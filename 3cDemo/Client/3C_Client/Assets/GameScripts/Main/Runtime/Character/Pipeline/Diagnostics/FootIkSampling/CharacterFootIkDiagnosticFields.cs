using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static class CharacterFootIkDiagnosticFields
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
            DiagnosticValueKind.UInt64, "frame", Main, IdentityGroup)]
        internal static ulong FrameSequence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PresentationFrame;

        [DiagnosticField(Capability, "character-foot-ik/main/completion-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong CompletionIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.CompletionIdentity;

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
    }
}
