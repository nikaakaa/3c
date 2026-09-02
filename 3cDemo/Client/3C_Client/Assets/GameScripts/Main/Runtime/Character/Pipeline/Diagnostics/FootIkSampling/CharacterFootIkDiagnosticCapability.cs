using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static class CharacterFootIkDiagnosticIdentity
    {
        internal const string CapabilityId = "character-foot-ik";
        internal const int CapabilityRevision = 1;
        internal const string LineageTypeIdentity =
            "character-foot-ik-lineage/1";
        internal const string LeftDimensionId = "character-foot-ik/left";
        internal const string RightDimensionId = "character-foot-ik/right";
    }

    public readonly struct CharacterFootIkCaptureMetadata
    {
        public CharacterFootIkCaptureMetadata(
            Guid sampleIdentity,
            DateTime startedUtc,
            in AnimationPresentationProgramIdentity program,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId)
        {
            if (sampleIdentity == Guid.Empty ||
                startedUtc.Kind != DateTimeKind.Utc ||
                !program.IsValid ||
                targetRuntimeInstanceId == Guid.Empty ||
                targetHostInstanceId == 0)
            {
                throw new ArgumentException("Foot IK capture metadata is invalid.");
            }
            SampleIdentity = sampleIdentity.ToString("N");
            StartedUtcTicks = startedUtc.Ticks;
            ProgramIdentity =
                $"{program.ProjectionRevision}|{program.PosePlanHash}";
            TargetRuntimeInstanceId = targetRuntimeInstanceId.ToString("N");
            TargetHostInstanceId = targetHostInstanceId;
        }

        [DiagnosticField(1, "identity", "capture-metadata")]
        public string SampleIdentity { get; }

        [DiagnosticField(1, "utc-ticks", "capture-metadata")]
        public long StartedUtcTicks { get; }

        [DiagnosticField(1, "identity", "capture-metadata")]
        public string ProgramIdentity { get; }

        [DiagnosticField(1, "identity", "capture-metadata")]
        public string TargetRuntimeInstanceId { get; }

        [DiagnosticField(1, "identity", "capture-metadata")]
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
    [DiagnosticFactRoot("frame", typeof(CharacterPoseFrameLineage))]
    [DiagnosticFactRoot("input", typeof(CharacterFootLandingPredictionInputDiagnostics))]
    [DiagnosticFactRoot("leg", typeof(CharacterFullBodyIkLimbDiagnostics))]
    [DiagnosticFactRoot("pelvis", typeof(CharacterFullBodyIkEffectorDiagnostics))]
    [DiagnosticFactRoot("pelvis-goal", typeof(CharacterFullBodyIkGoal))]
    [DiagnosticFactRoot("primary-support", typeof(CharacterFootPrimarySupportDiagnostics))]
    [DiagnosticFactRoot("solver", typeof(CharacterFullBodyIkSolverDiagnostics))]
    [DiagnosticFactRoot("stride", typeof(CharacterFootStrideHipsDiagnostics))]
    internal static class CharacterFootIkDiagnosticCapability
    {
    }
}
