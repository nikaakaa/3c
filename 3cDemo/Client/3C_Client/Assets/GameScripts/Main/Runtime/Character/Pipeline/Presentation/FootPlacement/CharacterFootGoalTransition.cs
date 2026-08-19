using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    readonly struct CharacterFootGoalTransitionSnapshot
    {
        internal CharacterFootGoalTransitionSnapshot(
            bool hasOutput,
            ulong sourceGroundPathIdentity,
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
            SourceGroundPathIdentity = sourceGroundPathIdentity;
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
        internal ulong SourceGroundPathIdentity { get; }
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
        internal ulong SourceGroundPathIdentity;
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
                SourceGroundPathIdentity,
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

        internal void BeginPending()
        {
            m_Pending = m_Committed;
            m_HasPending = true;
        }

        internal CharacterFullBodyIkGoal Resolve(
            in CharacterFullBodyIkGoal target,
            Vector3 originalComponentPosition,
            Quaternion originalComponentRotation,
            ulong sourceGroundPathIdentity,
            float deltaSeconds,
            float halfLifeSeconds,
            bool hardOwnershipLoss)
        {
            RequirePending();
            if (!target.IsValid ||
                target.Application != CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget ||
                !Finite(originalComponentPosition) ||
                !Unit(originalComponentRotation) ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f ||
                !float.IsFinite(halfLifeSeconds) || halfLifeSeconds <= 0f)
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

            m_Pending.HasOutput = true;
            m_Pending.SourceGroundPathIdentity = sourceGroundPathIdentity;
            m_Pending.TargetPositionCorrection = targetPositionCorrection;
            m_Pending.TargetRotationCorrection = targetRotationCorrection;
            m_Pending.TargetPositionWeight = target.PositionWeight;
            m_Pending.TargetRotationWeight = target.RotationWeight;
            m_Pending.OutputPositionCorrection = Vector3.LerpUnclamped(
                committedPositionCorrection,
                targetPositionCorrection,
                alpha);
            m_Pending.OutputRotationCorrection = Quaternion.SlerpUnclamped(
                committedRotationCorrection,
                targetRotationCorrection,
                alpha).normalized;
            m_Pending.OutputPositionWeight = Mathf.LerpUnclamped(
                committedPositionWeight,
                target.PositionWeight,
                alpha);
            m_Pending.OutputRotationWeight = Mathf.LerpUnclamped(
                committedRotationWeight,
                target.RotationWeight,
                alpha);

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
