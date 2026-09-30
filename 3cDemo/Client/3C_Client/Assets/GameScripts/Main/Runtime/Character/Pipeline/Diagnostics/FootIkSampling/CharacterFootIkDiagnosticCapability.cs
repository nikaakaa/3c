using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    public static class CharacterFootIkDiagnosticIdentity
    {
        public const string CapabilityId = "character-foot-ik";
        public const int CapabilityRevision = 5;
        public const string LeftDimensionId = "character-foot-ik/left";
        public const string RightDimensionId = "character-foot-ik/right";
        public const string CoreSamplerId = "character-foot-ik/core";
        public const string FullSamplerId = "character-foot-ik/full";
        public const string CoreProgramId = "character-foot-ik/core-program";
        public const string FullProgramId = "character-foot-ik/full-program";
    }

    public readonly struct CharacterFootIkCaptureMetadata
    {
        public CharacterFootIkCaptureMetadata(
            Guid sampleIdentity,
            DateTime startedUtc,
            in CharacterPoseDiagnosticTarget presentation,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId)
        {
            if (sampleIdentity == Guid.Empty ||
                startedUtc.Kind != DateTimeKind.Utc ||
                !presentation.IsValid ||
                targetRuntimeInstanceId == Guid.Empty ||
                targetHostInstanceId == 0)
            {
                throw new ArgumentException("Foot IK capture metadata is invalid.");
            }
            SampleIdentity = sampleIdentity.ToString("N");
            StartedUtcTicks = startedUtc.Ticks;
            PresentationIdentity =
                $"{presentation.GraphRevision}|{presentation.ResourceRevision}";
            TargetRuntimeInstanceId = targetRuntimeInstanceId.ToString("N");
            TargetHostInstanceId = targetHostInstanceId;
        }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public string SampleIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public long StartedUtcTicks { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public string PresentationIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public string TargetRuntimeInstanceId { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public int TargetHostInstanceId { get; }
    }

    [DiagnosticCapability(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkDiagnosticIdentity.CapabilityRevision,
        typeof(CharacterFootIkCaptureMetadata))]
    [DiagnosticFactRoot("effector", typeof(CharacterFullBodyIkEffectorDiagnostics))]
    [DiagnosticFactRoot("foot", typeof(CharacterFootLandingPredictionFootDiagnostics))]
    [DiagnosticFactRoot("formal-input", typeof(AnimationFootMotionRuntimeSample))]
    [DiagnosticFactRoot("formal-output", typeof(AnimationFootMotionRuntimeSample))]
    [DiagnosticFactRoot("frame", typeof(CharacterPoseDiagnosticFrame))]
    [DiagnosticFactRoot("input", typeof(CharacterFootLandingPredictionInputDiagnostics))]
    [DiagnosticFactRoot("leg", typeof(CharacterFullBodyIkLimbDiagnostics))]
    [DiagnosticFactRoot("pelvis", typeof(CharacterFullBodyIkEffectorDiagnostics))]
    [DiagnosticFactRoot("pelvis-goal", typeof(CharacterFullBodyIkGoal))]
    [DiagnosticFactRoot("physical", typeof(CharacterPhysicalFootPose))]
    [DiagnosticFactRoot("physical-body", typeof(CharacterPhysicalBodyPose))]
    [DiagnosticFactRoot("primary-support", typeof(CharacterFootPrimarySupportDiagnostics))]
    [DiagnosticFactRoot("solver", typeof(CharacterFullBodyIkSolverDiagnostics))]
    [DiagnosticFactRoot("stride", typeof(CharacterFootStrideHipsDiagnostics))]
    internal static class CharacterFootIkDiagnosticCapability
    {
    }
}
