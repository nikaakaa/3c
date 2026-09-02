using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal readonly struct CharacterPoseSourceTuningView
    {
        readonly CharacterPoseSourceTuningSnapshot m_Snapshot;

        internal CharacterPoseSourceTuningView(
            CharacterPoseSourceTuningSnapshot snapshot)
        {
            m_Snapshot = snapshot ??
                throw new ArgumentNullException(nameof(snapshot));
        }

        internal ulong Generation => m_Snapshot?.Generation ?? 0;
        internal bool IsValid => m_Snapshot != null;

        internal float RequireClipPlayRate(int clipPlayerIndex) =>
            m_Snapshot.RequireClipPlayRate(clipPlayerIndex);
    }

    internal sealed class CharacterPoseSourceTuningSnapshot
    {
        readonly float[] m_ClipPlayRates;

        internal CharacterPoseSourceTuningSnapshot(
            ulong generation,
            float[] clipPlayRates)
        {
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            Generation = generation;
            m_ClipPlayRates = clipPlayRates ??
                throw new ArgumentNullException(nameof(clipPlayRates));
        }

        internal ulong Generation { get; }

        internal float RequireClipPlayRate(int clipPlayerIndex)
        {
            if ((uint)clipPlayerIndex >= (uint)m_ClipPlayRates.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clipPlayerIndex));
            }
            return m_ClipPlayRates[clipPlayerIndex];
        }
    }

    internal sealed class CharacterPoseSourceTuningState
    {
        readonly CharacterPoseTuningLayout m_Layout;
        readonly CharacterPoseTuningLayoutEntry[] m_ClipPlayRateEntries;
        CharacterPoseSourceTuningSnapshot m_Committed;
        CharacterPoseSourceTuningSnapshot m_Candidate;

        internal CharacterPoseSourceTuningState(
            CharacterPresentationProjection projection,
            ulong initialGeneration)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireTuningPayload();
            m_Layout = projection.TuningLayout;
            m_ClipPlayRateEntries = BuildClipPlayRateEntries(projection);
            m_Committed = BuildSnapshot(
                m_Layout,
                projection.TuningDefaultBlock,
                initialGeneration);
            for (int i = 0; i < m_ClipPlayRateEntries.Length; i++)
            {
                float expected = projection.PosePlan.ClipPlayers[i].PlayRate;
                float actual = m_Committed.RequireClipPlayRate(i);
                if (actual != expected)
                {
                    throw new InvalidOperationException(
                        $"Clip Player #{i} tuning default differs from its compiled descriptor.");
                }
            }
        }

        internal CharacterPoseSourceTuningView RequireCommitted(
            ulong generation)
        {
            if (m_Committed.Generation != generation)
            {
                throw new InvalidOperationException(
                    "Pose Source tuning generation differs from the root frame.");
            }
            return new CharacterPoseSourceTuningView(m_Committed);
        }

        internal void PrepareCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation)
        {
            if (m_Candidate != null)
            {
                throw new InvalidOperationException(
                    "Pose Source tuning candidate is already prepared.");
            }
            if (generation != checked(m_Committed.Generation + 1))
            {
                throw new InvalidOperationException(
                    "Pose Source tuning candidate generation is not consecutive.");
            }
            m_Candidate = BuildSnapshot(layout, block, generation);
        }

        internal void CommitCandidate(ulong generation)
        {
            if (m_Candidate == null ||
                m_Candidate.Generation != generation)
            {
                throw new InvalidOperationException(
                    "Pose Source tuning candidate is not prepared.");
            }
            m_Committed = m_Candidate;
            m_Candidate = null;
        }

        internal void DiscardCandidate() => m_Candidate = null;

        CharacterPoseSourceTuningSnapshot BuildSnapshot(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation)
        {
            if (layout == null || block == null)
                throw new ArgumentNullException();
            if (!string.Equals(
                    layout.LayoutHash,
                    m_Layout.LayoutHash,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    layout.ProgramId,
                    m_Layout.ProgramId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    layout.ProjectionRevision,
                    m_Layout.ProjectionRevision,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    layout.PosePlanHash,
                    m_Layout.PosePlanHash,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    layout.RigId,
                    m_Layout.RigId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    layout.RigRevision,
                    m_Layout.RigRevision,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pose Source tuning layout identity is stale.");
            }
            block.RequireValid(layout);
            var clipPlayRates = new float[m_ClipPlayRateEntries.Length];
            for (int i = 0; i < clipPlayRates.Length; i++)
            {
                float value = block.GetValue(
                    m_ClipPlayRateEntries[i]).FloatValue;
                if (!float.IsFinite(value) || value <= 0f || value > 8f)
                {
                    throw new InvalidOperationException(
                        $"Clip Player #{i} play rate is outside its published range.");
                }
                clipPlayRates[i] = value;
            }
            return new CharacterPoseSourceTuningSnapshot(
                generation,
                clipPlayRates);
        }

        static CharacterPoseTuningLayoutEntry[] BuildClipPlayRateEntries(
            CharacterPresentationProjection projection)
        {
            CharacterPoseTuningLayout layout = projection.TuningLayout;
            var entries = new CharacterPoseTuningLayoutEntry[
                projection.PosePlan.ClipPlayers.Count];
            for (int i = 0; i < entries.Length; i++)
            {
                CharacterPresentationClipPlayerDescriptor player =
                    projection.PosePlan.ClipPlayers[i];
                string ownerId = $"pose-node:{player.NodeId.Value}";
                string fieldId = $"{ownerId}/play-rate";
                CharacterPoseTuningLayoutEntry found = null;
                for (int entryIndex = 0;
                     entryIndex < layout.Entries.Count;
                     entryIndex++)
                {
                    CharacterPoseTuningLayoutEntry entry =
                        layout.Entries[entryIndex];
                    if (string.Equals(
                            entry.FieldId,
                            fieldId,
                            StringComparison.Ordinal))
                    {
                        found = entry;
                        break;
                    }
                }
                if (found == null ||
                    !string.Equals(
                        found.OwnerId,
                        ownerId,
                        StringComparison.Ordinal) ||
                    found.Interaction !=
                        CharacterPoseTuningInteractionPolicy.TunableDefault ||
                    found.ValueKind != CharacterPoseTuningValueKind.Float ||
                    found.ApplyTiming !=
                        CharacterPoseTuningApplyTiming.NextFrame ||
                    found.StatePolicy !=
                        CharacterPoseTuningStatePolicy.PreserveState)
                {
                    throw new InvalidOperationException(
                        $"Clip Player '{player.NodeId}' has no exact Source tuning field.");
                }
                entries[i] = found;
            }
            return entries;
        }
    }
}
