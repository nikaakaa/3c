using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPosePreparedEvaluationState
    {
        internal CharacterPosePreparedEvaluationState(
            in CharacterPoseFrameLineage lineage,
            float presentationDeltaSeconds,
            in CharacterPoseGraphNativeBinding frame)
        {
            Lineage = lineage;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Frame = frame;
        }

        internal CharacterPoseFrameLineage Lineage { get; }
        internal float PresentationDeltaSeconds { get; }
        internal CharacterPoseGraphNativeBinding Frame { get; }
        internal bool IsValid =>
            Lineage.IsValid &&
            float.IsFinite(PresentationDeltaSeconds) &&
            PresentationDeltaSeconds >= 0f &&
            Frame.CompletionIdentity == Lineage.CompletionIdentity;
    }

    internal sealed class CharacterPoseProgramEvaluationState
    {
        CharacterPoseFrameLineage m_PreparedLineage;
        CharacterPoseGraphNativeBinding m_PreparedFrame;
        CharacterPoseGraphNativeBinding m_CommittedFrame;
        CharacterPoseGraphNativeBinding m_PendingCompletedFrame;
        float m_PreparedDeltaSeconds;
        bool m_HasPrepared;
        bool m_HasCommitted;
        bool m_HasPendingCompleted;

        internal bool HasPrepared => m_HasPrepared;
        internal bool HasCommitted => m_HasCommitted;
        internal bool HasPendingCompleted => m_HasPendingCompleted;
        internal ulong CommittedCompletionIdentity =>
            m_HasCommitted ? m_CommittedFrame.CompletionIdentity : 0;
        internal ulong PendingCompletedCompletionIdentity =>
            m_HasPendingCompleted
                ? m_PendingCompletedFrame.CompletionIdentity
                : 0;

        internal void Prepare(
            in CharacterPoseProgramPrepared prepared,
            float presentationDeltaSeconds,
            in CharacterPoseGraphNativeBinding frame)
        {
            if (m_HasPrepared)
            {
                throw new InvalidOperationException(
                    "Pose Program prepared page already contains a frame.");
            }
            if (!prepared.IsValid ||
                !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                frame.CompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Pose Program prepared page input is invalid.",
                    nameof(prepared));
            }
            m_PreparedLineage = prepared.Lineage;
            m_PreparedDeltaSeconds = presentationDeltaSeconds;
            m_PreparedFrame = frame;
            m_HasPrepared = true;
        }

        internal float RequireDeltaSeconds(
            in CharacterPoseProgramPrepared prepared)
        {
            RequirePrepared(in prepared);
            return m_PreparedDeltaSeconds;
        }

        internal CharacterPosePreparedEvaluationState Consume(
            in CharacterPoseProgramPrepared prepared)
        {
            RequirePrepared(in prepared);
            var state = new CharacterPosePreparedEvaluationState(
                in m_PreparedLineage,
                m_PreparedDeltaSeconds,
                in m_PreparedFrame);
            ClearPrepared();
            if (!state.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Program prepared page state is inconsistent.");
            }
            return state;
        }

        internal void MarkCompleted(
            in CharacterPoseGraphNativeBinding frame)
        {
            if (m_HasPrepared || m_HasPendingCompleted ||
                frame.CompletionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation completion is invalid.");
            }
            m_PendingCompletedFrame = frame;
            m_HasPendingCompleted = true;
        }

        internal void Commit(ulong completionIdentity)
        {
            if (!m_HasPendingCompleted ||
                m_PendingCompletedFrame.CompletionIdentity !=
                    completionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no completed evaluation to commit.");
            }
            m_CommittedFrame = m_PendingCompletedFrame;
            m_HasCommitted = true;
            m_PendingCompletedFrame = default;
            m_HasPendingCompleted = false;
        }

        internal CharacterPoseGraphNativeBinding RequirePendingCompleted()
        {
            if (!m_HasPendingCompleted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no Pending completed evaluation.");
            }
            return m_PendingCompletedFrame;
        }

        internal CharacterPoseGraphNativeBinding RequireCommitted()
        {
            if (!m_HasCommitted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no committed evaluation.");
            }
            return m_CommittedFrame;
        }

        internal void DiscardPending()
        {
            ClearPrepared();
            m_PendingCompletedFrame = default;
            m_HasPendingCompleted = false;
        }

        internal void Reset()
        {
            DiscardPending();
            m_CommittedFrame = default;
            m_HasCommitted = false;
        }

        void RequirePrepared(in CharacterPoseProgramPrepared prepared)
        {
            if (!m_HasPrepared || !prepared.IsValid ||
                prepared.Lineage != m_PreparedLineage)
            {
                throw new ArgumentException(
                    "Pose Program prepared page does not match the requested frame.",
                    nameof(prepared));
            }
        }

        void ClearPrepared()
        {
            m_PreparedLineage = default;
            m_PreparedFrame = default;
            m_PreparedDeltaSeconds = 0f;
            m_HasPrepared = false;
        }
    }
}
