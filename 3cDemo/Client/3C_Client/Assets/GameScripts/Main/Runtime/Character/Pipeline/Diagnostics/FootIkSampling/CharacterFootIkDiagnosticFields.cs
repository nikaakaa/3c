using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string Capability =
            CharacterFootIkDiagnosticIdentity.CapabilityId;
        const string Main = "main";
        const string IdentityGroup = "identity";
        const string CaptureMetadataGroup = "capture-metadata";
        const string RootHierarchyGroup = "root-hierarchy";

        [DiagnosticField(Capability, "character-foot-ik/main/sample-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, CaptureMetadataGroup)]
        internal static string SampleIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.SampleIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/sample-started-utc-ticks", 1,
            DiagnosticValueKind.Int64, "utc-ticks", Main, CaptureMetadataGroup)]
        internal static long SampleStartedUtcTicks(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.StartedUtcTicks;

        [DiagnosticField(Capability, "character-foot-ik/main/program-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, CaptureMetadataGroup)]
        internal static string ProgramIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.ProgramIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/target-runtime-instance-id", 1,
            DiagnosticValueKind.Identity, "identity", Main, CaptureMetadataGroup)]
        internal static string TargetRuntimeInstanceId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.TargetRuntimeInstanceId;

        [DiagnosticField(Capability, "character-foot-ik/main/target-host-instance-id", 1,
            DiagnosticValueKind.Int32, "identity", Main, CaptureMetadataGroup)]
        internal static int TargetHostInstanceId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.TargetHostInstanceId;

        [DiagnosticField(Capability, "character-foot-ik/main/side", 1,
            DiagnosticValueKind.Int32, "category", Main, IdentityGroup)]
        internal static int Side(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)metadata.Side;

        [DiagnosticField(Capability, "character-foot-ik/main/projection-revision", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string ProjectionRevision(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.ProjectionRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/pose-graph-id", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string PoseGraphId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PoseGraphId;

        [DiagnosticField(Capability, "character-foot-ik/main/pose-graph-revision", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string PoseGraphRevision(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PoseGraphRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/pose-plan-hash", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string PosePlanHash(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PoseProgramIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/frame-sequence", 1,
            DiagnosticValueKind.Int32, "frame", Main, IdentityGroup)]
        internal static int FrameSequence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            checked((int)view.Lineage.PresentationFrame);

        [DiagnosticField(Capability, "character-foot-ik/main/completion-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong CompletionIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/root-instance-id", 1,
            DiagnosticValueKind.Int32, "identity", Main, IdentityGroup)]
        internal static int RootInstanceId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.RootInstanceId;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-profile-id", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string FootProfileId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.ProfileId;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-profile-revision", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string FootProfileRevision(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.ProfileRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/state", 1,
            DiagnosticValueKind.Int32, "category", Main, IdentityGroup)]
        internal static int State(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Foot(in view, in metadata).State;

        [DiagnosticField(Capability, "character-foot-ik/main/reject-reason", 1,
            DiagnosticValueKind.Int32, "category", Main, IdentityGroup)]
        internal static int RejectReason(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Foot(in view, in metadata).RejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/step-source", 1,
            DiagnosticValueKind.Int32, "category", Main, IdentityGroup)]
        internal static int StepSource(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Foot(in view, in metadata).StepSource;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong LandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).LandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/trajectory-generation", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong TrajectoryGeneration(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).TrajectoryGeneration;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-confidence", 1,
            DiagnosticValueKind.Float32, "unitless", Main, IdentityGroup)]
        internal static float LandingConfidence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).LandingConfidence;

        [DiagnosticField(Capability, "character-foot-ik/main/time-to-landing-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, IdentityGroup)]
        internal static float TimeToLandingSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).TimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/next-landing-tracking-state", 1,
            DiagnosticValueKind.Int32, "category", Main, IdentityGroup)]
        internal static int NextLandingTrackingState(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Foot(in view, in metadata).NextLandingTrackingState;

        [DiagnosticField(Capability, "character-foot-ik/main/next-landing-tracking-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong NextLandingTrackingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).NextLandingTrackingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/verified-last-landing-available", 1,
            DiagnosticValueKind.Boolean, "none", Main, IdentityGroup)]
        internal static bool VerifiedLastLandingAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).VerifiedLastLandingAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/verified-last-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong VerifiedLastLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).VerifiedLastLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-state", 1,
            DiagnosticValueKind.Int32, "category", Main, IdentityGroup)]
        internal static int PlantTargetState(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Foot(in view, in metadata).PlantTargetState;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-available", 1,
            DiagnosticValueKind.Boolean, "none", Main, IdentityGroup)]
        internal static bool PlantTargetAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantTargetAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup,
            AvailabilityFieldId = "character-foot-ik/main/plant-target-available")]
        internal static ulong PlantTargetEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantTargetEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-surface-identity", 1,
            DiagnosticValueKind.Int32, "count", Main, IdentityGroup,
            AvailabilityFieldId = "character-foot-ik/main/plant-target-available")]
        internal static int PlantTargetSurfaceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantTargetSurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-point", 1,
            DiagnosticValueKind.Vector3, "metres", Main, IdentityGroup,
            AvailabilityFieldId = "character-foot-ik/main/plant-target-available")]
        internal static DiagnosticVector3 PlantTargetPoint(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).PlantTargetPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-normal", 1,
            DiagnosticValueKind.Vector3, "direction", Main, IdentityGroup,
            AvailabilityFieldId = "character-foot-ik/main/plant-target-available")]
        internal static DiagnosticVector3 PlantTargetNormal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).PlantTargetNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-trajectory-generation", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup,
            AvailabilityFieldId = "character-foot-ik/main/plant-target-available")]
        internal static ulong PlantTargetTrajectoryGeneration(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantTargetTrajectoryGeneration;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-future-body-translation-source-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup,
            AvailabilityFieldId = "character-foot-ik/main/plant-target-available")]
        internal static string PlantTargetFutureBodyTranslationSourceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantTargetFutureBodyTranslationSourceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-target-updated", 1,
            DiagnosticValueKind.Boolean, "none", Main, IdentityGroup)]
        internal static bool PlantTargetUpdated(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantTargetUpdated;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-verification-attempted", 1,
            DiagnosticValueKind.Boolean, "none", Main, IdentityGroup)]
        internal static bool PlantVerificationAttempted(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantVerificationAttempted;

        [DiagnosticField(Capability, "character-foot-ik/main/plant-verification-unavailable", 1,
            DiagnosticValueKind.Boolean, "none", Main, IdentityGroup)]
        internal static bool PlantVerificationUnavailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).PlantVerificationUnavailable;

        [DiagnosticField(Capability, "character-foot-ik/main/approach-plant-target-prepared", 1,
            DiagnosticValueKind.Boolean, "none", Main, IdentityGroup)]
        internal static bool ApproachPlantTargetPrepared(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).ApproachPlantTargetPrepared;

        [DiagnosticField(Capability, "character-foot-ik/main/step-selection-maximum-prediction-time-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, IdentityGroup)]
        internal static float StepSelectionMaximumPredictionTimeSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).StepCandidateSelection.MaximumPredictionTimeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/step-selection-last-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong StepSelectionLastLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).StepCandidateSelection.LastLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/selected-step-source", 1,
            DiagnosticValueKind.Int32, "category", Main, IdentityGroup)]
        internal static int SelectedStepSource(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Foot(in view, in metadata).StepCandidateSelection.SelectedSource;

        [DiagnosticField(Capability, "character-foot-ik/main/selected-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong SelectedLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).StepCandidateSelection.SelectedLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/logic-root-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, RootHierarchyGroup)]
        internal static DiagnosticVector3 LogicRootPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(view.LogicRootWorldPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/logic-root-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, RootHierarchyGroup)]
        internal static DiagnosticQuaternion LogicRootRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(view.LogicRootWorldRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/visual-root-local-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, RootHierarchyGroup)]
        internal static DiagnosticVector3 VisualRootLocalPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(view.VisualRootLocalPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/visual-root-local-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, RootHierarchyGroup)]
        internal static DiagnosticQuaternion VisualRootLocalRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(view.VisualRootLocalRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/visual-root-world-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, RootHierarchyGroup)]
        internal static DiagnosticVector3 VisualRootWorldPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(view.VisualRootWorldPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/visual-root-world-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, RootHierarchyGroup)]
        internal static DiagnosticQuaternion VisualRootWorldRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(view.VisualRootWorldRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/pose-root-local-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, RootHierarchyGroup)]
        internal static DiagnosticVector3 PoseRootLocalPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(view.PoseRootLocalPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/pose-root-local-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, RootHierarchyGroup)]
        internal static DiagnosticQuaternion PoseRootLocalRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(view.PoseRootLocalRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/pose-root-world-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, RootHierarchyGroup)]
        internal static DiagnosticVector3 PoseRootWorldPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(view.PoseRootWorldPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/pose-root-world-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, RootHierarchyGroup)]
        internal static DiagnosticQuaternion PoseRootWorldRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(view.PoseRootWorldRotation);

        static DiagnosticVector3 Vector3(UnityEngine.Vector3 value) =>
            new DiagnosticVector3(value.x, value.y, value.z);

        static DiagnosticQuaternion Quaternion(UnityEngine.Quaternion value) =>
            new DiagnosticQuaternion(value.x, value.y, value.z, value.w);

        static CharacterFootLandingPredictionFootDiagnostics Foot(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            ref readonly CharacterFootLandingPredictionDiagnostics frame =
                ref view.LandingPrediction;
            return metadata.Side == CharacterFootSide.Left
                ? frame.Left
                : frame.Right;
        }
    }
}
