using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterFullBodyIkLimbSlot : byte
    {
        LeftArm = 1,
        RightArm = 2,
        LeftLeg = 3,
        RightLeg = 4
    }

    public readonly struct CharacterFullBodyIkEffectorDiagnostics
    {
        internal CharacterFullBodyIkEffectorDiagnostics(
            CharacterFullBodyIkGoal goal,
            Vector3 solvedComponentPosition,
            Quaternion solvedComponentRotation,
            float positionResidual,
            float rotationResidualDegrees)
        {
            Slot = goal.Slot;
            SourceKind = goal.SourceKind;
            Application = goal.Application;
            TargetComponentPosition = goal.ComponentPosition;
            TargetComponentRotation = goal.ComponentRotation;
            PositionWeight = goal.PositionWeight;
            RotationWeight = goal.RotationWeight;
            SolvedComponentPosition = solvedComponentPosition;
            SolvedComponentRotation = solvedComponentRotation;
            PositionResidual = positionResidual;
            RotationResidualDegrees = rotationResidualDegrees;
            IsAvailable = true;
        }

        public CharacterFullBodyIkEffectorSlot Slot { get; }
        public CharacterFullBodyIkGoalSourceKind SourceKind { get; }
        public CharacterFullBodyIkGoalApplication Application { get; }
        public Vector3 TargetComponentPosition { get; }
        public Quaternion TargetComponentRotation { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public Vector3 SolvedComponentPosition { get; }
        public Quaternion SolvedComponentRotation { get; }
        public float PositionResidual { get; }
        public float RotationResidualDegrees { get; }
        public bool IsAvailable { get; }
    }

    public readonly struct CharacterFullBodyIkLimbDiagnostics
    {
        internal CharacterFullBodyIkLimbDiagnostics(
            CharacterFullBodyIkLimbSlot limb,
            float pull,
            float reach,
            float bendWeight,
            float bendClamp)
        {
            Limb = limb;
            Pull = pull;
            Reach = reach;
            BendWeight = bendWeight;
            BendClamp = bendClamp;
        }

        public CharacterFullBodyIkLimbSlot Limb { get; }
        public float Pull { get; }
        public float Reach { get; }
        public float BendWeight { get; }
        public float BendClamp { get; }
    }

    public readonly struct CharacterFullBodyIkSolverDiagnostics
    {
        internal CharacterFullBodyIkSolverDiagnostics(
            ulong frameSequence,
            ulong inputCompletionIdentity,
            ulong outputCompletionIdentity,
            string backendIdentity,
            string rigId,
            string rigRevision,
            string profileId,
            string profileRevision,
            int iterations,
            bool fabrikPass,
            Vector3 pelvisPreSolveTranslation,
            CharacterFullBodyIkResult result,
            int effectorCount,
            int limbCount)
        {
            FrameSequence = frameSequence;
            InputCompletionIdentity = inputCompletionIdentity;
            OutputCompletionIdentity = outputCompletionIdentity;
            BackendIdentity = backendIdentity ?? string.Empty;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            ProfileId = profileId ?? string.Empty;
            ProfileRevision = profileRevision ?? string.Empty;
            Iterations = iterations;
            FabrikPass = fabrikPass;
            PelvisPreSolveTranslation = pelvisPreSolveTranslation;
            Failure = result.Failure;
            FailedGoalSetIndex = result.FailedGoalSetIndex;
            FailedSlot = result.FailedSlot;
            AppliedGoalCount = result.AppliedGoalCount;
            EffectorCount = effectorCount;
            LimbCount = limbCount;
        }

        public ulong FrameSequence { get; }
        public ulong InputCompletionIdentity { get; }
        public ulong OutputCompletionIdentity { get; }
        public string BackendIdentity { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public string ProfileId { get; }
        public string ProfileRevision { get; }
        public int Iterations { get; }
        public bool FabrikPass { get; }
        public Vector3 PelvisPreSolveTranslation { get; }
        public CharacterFullBodyIkFailure Failure { get; }
        public int FailedGoalSetIndex { get; }
        public CharacterFullBodyIkEffectorSlot FailedSlot { get; }
        public int AppliedGoalCount { get; }
        public int EffectorCount { get; }
        public int LimbCount { get; }
        public bool IsCompleted => FrameSequence != 0 && InputCompletionIdentity != 0;
        public bool Succeeded => IsCompleted && Failure == CharacterFullBodyIkFailure.None && OutputCompletionIdentity != 0;
    }
}
