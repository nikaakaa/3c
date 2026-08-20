using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootGoalTransitionMode : byte
    {
        Smooth = 0,
        LandingPreparation = 1,
        DirectSupport = 2
    }

    public readonly struct CharacterFootGoalTransitionDiagnostics
    {
        internal CharacterFootGoalTransitionDiagnostics(
            in CharacterFootGoalTransitionSnapshot committed,
            in CharacterFootGoalTransitionSnapshot pending,
            float halfLifeSeconds)
        {
            Mode = pending.Mode;
            HasCommittedOutput = committed.HasOutput;
            HasPendingOutput = pending.HasOutput;
            CommittedSourceGroundPathIdentity = committed.SourceGroundPathIdentity;
            PendingSourceGroundPathIdentity = pending.SourceGroundPathIdentity;
            OriginalComponentPosition = pending.OriginalComponentPosition;
            RawPositionCorrection = pending.TargetPositionCorrection;
            RawRotationCorrection = pending.TargetRotationCorrection;
            RawPositionWeight = pending.TargetPositionWeight;
            RawRotationWeight = pending.TargetRotationWeight;
            CommittedPositionCorrection = committed.OutputPositionCorrection;
            CommittedRotationCorrection = committed.OutputRotationCorrection;
            CommittedPositionWeight = committed.OutputPositionWeight;
            CommittedRotationWeight = committed.OutputRotationWeight;
            PendingPositionCorrection = pending.OutputPositionCorrection;
            PendingRotationCorrection = pending.OutputRotationCorrection;
            PendingPositionWeight = pending.OutputPositionWeight;
            PendingRotationWeight = pending.OutputRotationWeight;
            HalfLifeSeconds = halfLifeSeconds;
        }

        public CharacterFootGoalTransitionMode Mode { get; }
        public bool HasCommittedOutput { get; }
        public bool HasPendingOutput { get; }
        public ulong CommittedSourceGroundPathIdentity { get; }
        public ulong PendingSourceGroundPathIdentity { get; }
        public Vector3 OriginalComponentPosition { get; }
        public Vector3 RawPositionCorrection { get; }
        public Quaternion RawRotationCorrection { get; }
        public float RawPositionWeight { get; }
        public float RawRotationWeight { get; }
        public Vector3 CommittedPositionCorrection { get; }
        public Quaternion CommittedRotationCorrection { get; }
        public float CommittedPositionWeight { get; }
        public float CommittedRotationWeight { get; }
        public Vector3 PendingPositionCorrection { get; }
        public Quaternion PendingRotationCorrection { get; }
        public float PendingPositionWeight { get; }
        public float PendingRotationWeight { get; }
        public float HalfLifeSeconds { get; }
    }

    readonly struct CharacterFootGoalTransitionSnapshot
    {
        internal CharacterFootGoalTransitionSnapshot(
            bool hasOutput,
            CharacterFootGoalTransitionMode mode,
            ulong sourceGroundPathIdentity,
            Vector3 originalComponentPosition,
            Vector3 targetPositionCorrection,
            Quaternion targetRotationCorrection,
            float targetPositionWeight,
            float targetRotationWeight,
            Vector3 outputPositionCorrection,
            Quaternion outputRotationCorrection,
            float outputPositionWeight,
            float outputRotationWeight)
        {
            HasOutput = hasOutput;
            Mode = mode;
            SourceGroundPathIdentity = sourceGroundPathIdentity;
            OriginalComponentPosition = originalComponentPosition;
            TargetPositionCorrection = targetPositionCorrection;
            TargetRotationCorrection = targetRotationCorrection;
            TargetPositionWeight = targetPositionWeight;
            TargetRotationWeight = targetRotationWeight;
            OutputPositionCorrection = outputPositionCorrection;
            OutputRotationCorrection = outputRotationCorrection;
            OutputPositionWeight = outputPositionWeight;
            OutputRotationWeight = outputRotationWeight;
        }

        internal bool HasOutput { get; }
        internal CharacterFootGoalTransitionMode Mode { get; }
        internal ulong SourceGroundPathIdentity { get; }
        internal Vector3 OriginalComponentPosition { get; }
        internal Vector3 TargetPositionCorrection { get; }
        internal Quaternion TargetRotationCorrection { get; }
        internal float TargetPositionWeight { get; }
        internal float TargetRotationWeight { get; }
        internal Vector3 OutputPositionCorrection { get; }
        internal Quaternion OutputRotationCorrection { get; }
        internal float OutputPositionWeight { get; }
        internal float OutputRotationWeight { get; }
    }

    struct CharacterFootGoalTransitionFrame
    {
        internal bool HasOutput;
        internal CharacterFootGoalTransitionMode Mode;
        internal ulong SourceGroundPathIdentity;
        internal Vector3 OriginalComponentPosition;
        internal Vector3 TargetPositionCorrection;
        internal Quaternion TargetRotationCorrection;
        internal float TargetPositionWeight;
        internal float TargetRotationWeight;
        internal Vector3 OutputPositionCorrection;
        internal Quaternion OutputRotationCorrection;
        internal float OutputPositionWeight;
        internal float OutputRotationWeight;

        internal CharacterFootGoalTransitionSnapshot Snapshot =>
            new CharacterFootGoalTransitionSnapshot(
                HasOutput,
                Mode,
                SourceGroundPathIdentity,
                OriginalComponentPosition,
                TargetPositionCorrection,
                HasOutput ? TargetRotationCorrection : Quaternion.identity,
                TargetPositionWeight,
                TargetRotationWeight,
                OutputPositionCorrection,
                HasOutput ? OutputRotationCorrection : Quaternion.identity,
                OutputPositionWeight,
                OutputRotationWeight);
    }

    sealed class CharacterFootGoalTransition
    {
        CharacterFootGoalTransitionFrame m_Committed;
        CharacterFootGoalTransitionFrame m_Pending;
        bool m_HasPending;

        internal CharacterFootGoalTransitionSnapshot Pending
        {
            get
            {
                RequirePending();
                return m_Pending.Snapshot;
            }
        }

        internal CharacterFootGoalTransitionDiagnostics CaptureDiagnostics(
            float halfLifeSeconds)
        {
            RequirePending();
            return new CharacterFootGoalTransitionDiagnostics(
                m_Committed.Snapshot,
                m_Pending.Snapshot,
                halfLifeSeconds);
        }

        internal void BeginPending()
        {
            m_Pending = m_Committed;
            m_HasPending = true;
        }

        internal CharacterFullBodyIkGoal Resolve(
            in CharacterFullBodyIkGoal target,
            Vector3 originalComponentPosition,
            Quaternion originalComponentRotation,
            Vector3 componentUp,
            ulong sourceGroundPathIdentity,
            float deltaSeconds,
            float halfLifeSeconds,
            CharacterFootGoalTransitionMode mode,
            bool hardOwnershipLoss)
        {
            RequirePending();
            if (!target.IsValid ||
                target.Application != CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget ||
                !Finite(originalComponentPosition) ||
                !Finite(componentUp) || componentUp.sqrMagnitude <= 0.0001f ||
                !Unit(originalComponentRotation) ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f ||
                !float.IsFinite(halfLifeSeconds) || halfLifeSeconds <= 0f ||
                mode != CharacterFootGoalTransitionMode.Smooth &&
                mode != CharacterFootGoalTransitionMode.LandingPreparation &&
                mode != CharacterFootGoalTransitionMode.DirectSupport)
            {
                throw new ArgumentException("Foot Goal transition input is invalid.");
            }

            if (hardOwnershipLoss)
            {
                m_Pending = default;
                return Rebuild(
                    in target,
                    originalComponentPosition,
                    originalComponentRotation,
                    0f,
                    0f);
            }

            Vector3 targetPositionCorrection =
                target.ComponentPosition - originalComponentPosition;
            Quaternion targetRotationCorrection =
                (Quaternion.Inverse(originalComponentRotation) * target.ComponentRotation).normalized;
            Vector3 committedPositionCorrection = m_Committed.HasOutput
                ? m_Committed.OutputPositionCorrection
                : default;
            Quaternion committedRotationCorrection = m_Committed.HasOutput
                ? m_Committed.OutputRotationCorrection
                : Quaternion.identity;
            float committedPositionWeight = m_Committed.HasOutput
                ? m_Committed.OutputPositionWeight
                : 0f;
            float committedRotationWeight = m_Committed.HasOutput
                ? m_Committed.OutputRotationWeight
                : 0f;
            float alpha = deltaSeconds <= 0f
                ? 0f
                : 1f - Mathf.Pow(0.5f, deltaSeconds / halfLifeSeconds);
            Vector3 smoothedPositionCorrection = Vector3.LerpUnclamped(
                committedPositionCorrection,
                targetPositionCorrection,
                alpha);
            Quaternion smoothedRotationCorrection = Quaternion.SlerpUnclamped(
                committedRotationCorrection,
                targetRotationCorrection,
                alpha).normalized;
            float smoothedPositionWeight = Mathf.LerpUnclamped(
                committedPositionWeight,
                target.PositionWeight,
                alpha);
            float smoothedRotationWeight = Mathf.LerpUnclamped(
                committedRotationWeight,
                target.RotationWeight,
                alpha);

            m_Pending.HasOutput = true;
            m_Pending.Mode = mode;
            m_Pending.SourceGroundPathIdentity = sourceGroundPathIdentity;
            m_Pending.OriginalComponentPosition = originalComponentPosition;
            m_Pending.TargetPositionCorrection = targetPositionCorrection;
            m_Pending.TargetRotationCorrection = targetRotationCorrection;
            m_Pending.TargetPositionWeight = target.PositionWeight;
            m_Pending.TargetRotationWeight = target.RotationWeight;
            if (mode == CharacterFootGoalTransitionMode.DirectSupport)
            {
                m_Pending.OutputPositionCorrection = targetPositionCorrection;
                m_Pending.OutputRotationCorrection = targetRotationCorrection;
                m_Pending.OutputPositionWeight = target.PositionWeight;
                m_Pending.OutputRotationWeight = target.RotationWeight;
            }
            else if (mode == CharacterFootGoalTransitionMode.LandingPreparation)
            {
                Vector3 up = componentUp.normalized;
                float missingUpwardCorrection = Vector3.Dot(
                    targetPositionCorrection - smoothedPositionCorrection,
                    up);
                m_Pending.OutputPositionCorrection = missingUpwardCorrection > 0f
                    ? smoothedPositionCorrection + up * missingUpwardCorrection
                    : smoothedPositionCorrection;
                m_Pending.OutputRotationCorrection = smoothedRotationCorrection;
                m_Pending.OutputPositionWeight = target.PositionWeight;
                m_Pending.OutputRotationWeight = target.RotationWeight;
            }
            else
            {
                m_Pending.OutputPositionCorrection = smoothedPositionCorrection;
                m_Pending.OutputRotationCorrection = smoothedRotationCorrection;
                m_Pending.OutputPositionWeight = smoothedPositionWeight;
                m_Pending.OutputRotationWeight = smoothedRotationWeight;
            }

            Vector3 outputPosition = originalComponentPosition +
                                     m_Pending.OutputPositionCorrection;
            Quaternion outputRotation = (originalComponentRotation *
                                           m_Pending.OutputRotationCorrection).normalized;
            return Rebuild(
                in target,
                outputPosition,
                outputRotation,
                m_Pending.OutputPositionWeight,
                m_Pending.OutputRotationWeight);
        }

        internal void Seal()
        {
            RequirePending();
            m_Committed = m_Pending;
            ClearPending();
        }

        internal void Discard()
        {
            ClearPending();
        }

        internal void Reset()
        {
            m_Committed = default;
            ClearPending();
        }

        static CharacterFullBodyIkGoal Rebuild(
            in CharacterFullBodyIkGoal source,
            Vector3 componentPosition,
            Quaternion componentRotation,
            float positionWeight,
            float rotationWeight) =>
            new CharacterFullBodyIkGoal(
                source.Slot,
                componentPosition,
                componentRotation,
                Mathf.Clamp01(positionWeight),
                Mathf.Clamp01(rotationWeight),
                source.Application,
                source.SourceKind,
                source.DiagnosticMetadataIndex);

        void ClearPending()
        {
            m_Pending = default;
            m_HasPending = false;
        }

        void RequirePending()
        {
            if (!m_HasPending)
                throw new InvalidOperationException("Foot Goal transition has no pending frame.");
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool Unit(Quaternion value)
        {
            float squareMagnitude = value.x * value.x + value.y * value.y +
                                    value.z * value.z + value.w * value.w;
            return float.IsFinite(squareMagnitude) &&
                   Mathf.Abs(squareMagnitude - 1f) <= 0.01f;
        }
    }
}
