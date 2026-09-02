using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseProgramTuningView
    {
        readonly CharacterPoseProgramTuningSnapshot m_Snapshot;

        internal CharacterPoseProgramTuningView(
            CharacterPoseProgramTuningSnapshot snapshot)
        {
            m_Snapshot = snapshot ??
                throw new ArgumentNullException(nameof(snapshot));
        }

        internal ulong Generation => m_Snapshot?.Generation ?? 0;
        internal NativeArray<float> OperationWeights =>
            m_Snapshot?.OperationWeights ?? default;
        internal bool IsValid =>
            m_Snapshot != null && m_Snapshot.IsValid;
    }

    internal sealed class CharacterPoseProgramTuningSnapshot : IDisposable
    {
        NativeArray<float> m_OperationWeights;
        bool m_Disposed;

        internal CharacterPoseProgramTuningSnapshot(
            ulong generation,
            NativeArray<float> operationWeights)
        {
            if (generation == 0 || !operationWeights.IsCreated)
                throw new ArgumentException();
            Generation = generation;
            m_OperationWeights = operationWeights;
        }

        internal ulong Generation { get; }
        internal NativeArray<float> OperationWeights => m_OperationWeights;
        internal bool IsValid =>
            !m_Disposed && m_OperationWeights.IsCreated;

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_OperationWeights.IsCreated)
                m_OperationWeights.Dispose();
            m_OperationWeights = default;
        }
    }

    internal sealed class CharacterPoseProgramTuningState : IDisposable
    {
        readonly CharacterPoseTuningLayout m_Layout;
        readonly CharacterPoseTuningLayoutEntry[] m_OperationWeightEntries;
        readonly float[] m_DefaultOperationWeights;
        CharacterPoseProgramTuningSnapshot m_Committed;
        CharacterPoseProgramTuningSnapshot m_Candidate;
        bool m_Disposed;

        internal CharacterPoseProgramTuningState(
            CharacterPresentationProjection projection,
            NativeArray<AnimationPoseGraphNativeOperation> operations,
            ulong initialGeneration)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (!operations.IsCreated || operations.Length == 0)
                throw new ArgumentException(nameof(operations));
            projection.RequireTuningPayload();
            m_Layout = projection.TuningLayout;
            m_OperationWeightEntries =
                new CharacterPoseTuningLayoutEntry[operations.Length];
            m_DefaultOperationWeights = new float[operations.Length];
            BuildOperationSchema(projection, operations);
            m_Committed = BuildSnapshot(
                m_Layout,
                projection.TuningDefaultBlock,
                initialGeneration);
            for (int i = 0; i < m_DefaultOperationWeights.Length; i++)
            {
                if (m_Committed.OperationWeights[i] !=
                    m_DefaultOperationWeights[i])
                {
                    Dispose();
                    throw new InvalidOperationException(
                        $"Pose operation #{i} tuning default differs from its compiled weight.");
                }
            }
        }

        internal CharacterPoseProgramTuningView RequireCommitted(
            ulong generation)
        {
            RequireAlive();
            if (!m_Committed.IsValid ||
                m_Committed.Generation != generation)
            {
                throw new InvalidOperationException(
                    "Pose Program tuning generation differs from the root frame.");
            }
            return new CharacterPoseProgramTuningView(m_Committed);
        }

        internal void PrepareCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation)
        {
            RequireAlive();
            if (m_Candidate != null)
            {
                throw new InvalidOperationException(
                    "Pose Program tuning candidate is already prepared.");
            }
            if (generation != checked(m_Committed.Generation + 1))
            {
                throw new InvalidOperationException(
                    "Pose Program tuning candidate generation is not consecutive.");
            }
            m_Candidate = BuildSnapshot(layout, block, generation);
        }

        internal void CommitCandidate(ulong generation)
        {
            RequireAlive();
            if (m_Candidate == null ||
                !m_Candidate.IsValid ||
                m_Candidate.Generation != generation)
            {
                throw new InvalidOperationException(
                    "Pose Program tuning candidate is not prepared.");
            }
            CharacterPoseProgramTuningSnapshot previous = m_Committed;
            m_Committed = m_Candidate;
            m_Candidate = null;
            previous.Dispose();
        }

        internal void DiscardCandidate()
        {
            m_Candidate?.Dispose();
            m_Candidate = null;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Candidate?.Dispose();
            m_Committed?.Dispose();
            m_Candidate = null;
            m_Committed = null;
        }

        CharacterPoseProgramTuningSnapshot BuildSnapshot(
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
                    "Pose Program tuning layout identity is stale.");
            }
            block.RequireValid(layout);
            var weights = new NativeArray<float>(
                m_DefaultOperationWeights.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            try
            {
                for (int i = 0; i < weights.Length; i++)
                {
                    CharacterPoseTuningLayoutEntry entry =
                        m_OperationWeightEntries[i];
                    float value = entry == null
                        ? m_DefaultOperationWeights[i]
                        : block.GetValue(entry).FloatValue;
                    if (!float.IsFinite(value) || value < 0f || value > 1f)
                    {
                        throw new InvalidOperationException(
                            $"Pose operation #{i} weight is outside its published range.");
                    }
                    weights[i] = value;
                }
                return new CharacterPoseProgramTuningSnapshot(
                    generation,
                    weights);
            }
            catch
            {
                weights.Dispose();
                throw;
            }
        }

        void BuildOperationSchema(
            CharacterPresentationProjection projection,
            NativeArray<AnimationPoseGraphNativeOperation> operations)
        {
            for (int nativeIndex = 0;
                 nativeIndex < operations.Length;
                 nativeIndex++)
            {
                AnimationPoseGraphNativeOperation native =
                    operations[nativeIndex];
                CharacterPoseOperationHeader operation =
                    projection.PosePlan.OperationHeaders[native.Index];
                if (operation.Code != native.Code)
                {
                    throw new InvalidOperationException(
                        $"Pose operation #{native.Index} differs from its native execution entry.");
                }
                m_DefaultOperationWeights[nativeIndex] = native.Weight;
                if (!IsTunableWeight(operation.Code))
                    continue;
                string ownerId = $"pose-node:{operation.NodeId.Value}";
                string fieldId = $"{ownerId}/weight";
                CharacterPoseTuningLayoutEntry found = null;
                for (int entryIndex = 0;
                     entryIndex < m_Layout.Entries.Count;
                     entryIndex++)
                {
                    CharacterPoseTuningLayoutEntry entry =
                        m_Layout.Entries[entryIndex];
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
                        $"Pose operation '{operation.NodeId}' has no exact Program tuning field.");
                }
                m_OperationWeightEntries[nativeIndex] = found;
            }
        }

        static bool IsTunableWeight(CharacterPoseOperationCode code) =>
            code == CharacterPoseOperationCode.BlendPose ||
            code == CharacterPoseOperationCode.LayeredBoneBlend ||
            code == CharacterPoseOperationCode.AdditivePose;

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseProgramTuningState));
            }
        }
    }
}
