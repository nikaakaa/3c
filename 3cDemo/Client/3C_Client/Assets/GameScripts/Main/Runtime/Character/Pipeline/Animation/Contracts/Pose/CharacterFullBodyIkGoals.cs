using System;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterFullBodyIkEffectorSlot : byte
    {
        PelvisPreSolveTranslation = 1,
        Body = 2,
        LeftShoulder = 3,
        RightShoulder = 4,
        LeftThigh = 5,
        RightThigh = 6,
        LeftHand = 7,
        RightHand = 8,
        LeftFoot = 9,
        RightFoot = 10
    }

    [Flags]
    public enum CharacterFullBodyIkGoalSourceKind : byte
    {
        None = 0,
        FootGrounding = 1,
        PredictiveExtension = 2,
        PoseBone = 4
    }

    public enum CharacterFullBodyIkGoalSetAvailability : byte
    {
        Invalid = 0,
        Ready = 1,
        WorldContextUnavailable = 2
    }

    public enum CharacterFullBodyIkGoalApplication : byte
    {
        AbsoluteEffectorTarget = 1,
        FootPlacementEffectorTarget = 2,
        PelvisPreSolveTranslation = 3
    }

    public readonly struct CharacterFullBodyIkGoal
    {
        public CharacterFullBodyIkGoal(
            CharacterFullBodyIkEffectorSlot slot,
            Vector3 componentPosition,
            Quaternion componentRotation,
            float positionWeight,
            float rotationWeight,
            CharacterFullBodyIkGoalApplication application,
            CharacterFullBodyIkGoalSourceKind sourceKind,
            int diagnosticMetadataIndex)
        {
            Slot = slot;
            ComponentPosition = componentPosition;
            ComponentRotation = componentRotation;
            PositionWeight = positionWeight;
            RotationWeight = rotationWeight;
            Application = application;
            SourceKind = sourceKind;
            DiagnosticMetadataIndex = diagnosticMetadataIndex;
            if (!IsValid)
                throw new ArgumentException("Full Body IK Goal is invalid.");
        }

        public CharacterFullBodyIkEffectorSlot Slot { get; }
        public Vector3 ComponentPosition { get; }
        public Quaternion ComponentRotation { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public CharacterFullBodyIkGoalApplication Application { get; }
        public CharacterFullBodyIkGoalSourceKind SourceKind { get; }
        public int DiagnosticMetadataIndex { get; }

        public bool IsValid =>
            Slot >= CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation &&
            Slot <= CharacterFullBodyIkEffectorSlot.RightFoot &&
            CharacterPoseConstraintMath.IsFinite(ComponentPosition) &&
            IsUnit(ComponentRotation) &&
            IsWeight(PositionWeight) &&
            IsWeight(RotationWeight) &&
            IsApplicationValid() &&
            SourceKind != CharacterFullBodyIkGoalSourceKind.None &&
            (SourceKind & ~(CharacterFullBodyIkGoalSourceKind.FootGrounding |
                            CharacterFullBodyIkGoalSourceKind.PredictiveExtension |
                            CharacterFullBodyIkGoalSourceKind.PoseBone)) == 0 &&
            DiagnosticMetadataIndex >= -1;

        bool IsApplicationValid()
        {
            switch (Application)
            {
                case CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation:
                    return Slot == CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation &&
                           ComponentRotation == Quaternion.identity &&
                           RotationWeight == 0f &&
                           (SourceKind & CharacterFullBodyIkGoalSourceKind.FootGrounding) != 0;
                case CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget:
                    return (Slot == CharacterFullBodyIkEffectorSlot.LeftFoot ||
                            Slot == CharacterFullBodyIkEffectorSlot.RightFoot) &&
                           (SourceKind & CharacterFullBodyIkGoalSourceKind.FootGrounding) != 0;
                case CharacterFullBodyIkGoalApplication.AbsoluteEffectorTarget:
                    return Slot != CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation;
                default:
                    return false;
            }
        }

        static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        static bool IsUnit(Quaternion value)
        {
            if (!CharacterPoseConstraintMath.IsFinite(value))
                return false;
            float squareMagnitude = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            return Mathf.Abs(squareMagnitude - 1f) <= 0.01f;
        }
    }

    public readonly struct CharacterFullBodyIkGoalSetHeader
    {
        public const int MaximumGoalCount = 10;

        public CharacterFullBodyIkGoalSetHeader(
            ulong frameSequence,
            ulong completionIdentity,
            string rigId,
            string rigRevision,
            int producerOperationIndex,
            int producerCallSiteIndex,
            int goalOffset,
            int goalCount,
            CharacterFullBodyIkGoalSetAvailability availability)
            : this(
                frameSequence,
                completionIdentity,
                new FixedString64Bytes(rigId ?? string.Empty),
                new FixedString64Bytes(rigRevision ?? string.Empty),
                producerOperationIndex,
                producerCallSiteIndex,
                goalOffset,
                goalCount,
                availability)
        {
        }

        public CharacterFullBodyIkGoalSetHeader(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            int producerOperationIndex,
            int producerCallSiteIndex,
            int goalOffset,
            int goalCount,
            CharacterFullBodyIkGoalSetAvailability availability)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            ProducerOperationIndex = producerOperationIndex;
            ProducerCallSiteIndex = producerCallSiteIndex;
            GoalOffset = goalOffset;
            GoalCount = goalCount;
            Availability = availability;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public FixedString64Bytes RigId { get; }
        public FixedString64Bytes RigRevision { get; }
        public int ProducerOperationIndex { get; }
        public int ProducerCallSiteIndex { get; }
        public int GoalOffset { get; }
        public int GoalCount { get; }
        public CharacterFullBodyIkGoalSetAvailability Availability { get; }

        public bool IsValid =>
            FrameSequence != 0 &&
            CompletionIdentity != 0 &&
            RigId.Length > 0 &&
            RigRevision.Length > 0 &&
            ProducerOperationIndex >= 0 &&
            ProducerCallSiteIndex >= 0 &&
            GoalOffset >= 0 &&
            GoalCount >= 0 &&
            GoalCount <= MaximumGoalCount &&
            (Availability == CharacterFullBodyIkGoalSetAvailability.Ready ||
             Availability == CharacterFullBodyIkGoalSetAvailability.WorldContextUnavailable && GoalCount == 0);
    }

    public readonly struct CharacterFullBodyIkGoalSet
    {
        public CharacterFullBodyIkGoalSet(
            in CharacterFullBodyIkGoalSetHeader header,
            NativeSlice<CharacterFullBodyIkGoal> goals)
        {
            Header = header;
            Goals = goals;
            if (!IsValid)
                throw new ArgumentException("Full Body IK Goal Set is invalid.");
        }

        public CharacterFullBodyIkGoalSetHeader Header { get; }
        public NativeSlice<CharacterFullBodyIkGoal> Goals { get; }

        public bool IsValid
        {
            get
            {
                if (!Header.IsValid || Goals.Length != Header.GoalCount)
                    return false;
                ushort slots = 0;
                for (int i = 0; i < Goals.Length; i++)
                {
                    CharacterFullBodyIkGoal goal = Goals[i];
                    if (!goal.IsValid)
                        return false;
                    int bit = 1 << ((int)goal.Slot - 1);
                    if ((slots & bit) != 0)
                        return false;
                    slots = (ushort)(slots | bit);
                }
                return true;
            }
        }
    }
}
