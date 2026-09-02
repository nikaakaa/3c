using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string GroundPathGroup = "ground-path";
        const string GroundPathHasInvalidSegment =
            "character-foot-ik/main/ground-path-has-invalid-segment";

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-state", 1,
            DiagnosticValueKind.Int32, "category", Main, GroundPathGroup)]
        internal static int GroundPathState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)GroundPath(in view, in metadata).State;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-reject-reason", 1,
            DiagnosticValueKind.Int32, "category", Main, GroundPathGroup)]
        internal static int GroundPathRejectReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)GroundPath(in view, in metadata).RejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-input-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, GroundPathGroup)]
        internal static ulong GroundPathInputIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).InputIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-query-executed", 1,
            DiagnosticValueKind.Boolean, "none", Main, GroundPathGroup)]
        internal static bool GroundPathQueryExecuted(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).QueryExecuted;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-target-available", 1,
            DiagnosticValueKind.Boolean, "none", Main, GroundPathGroup)]
        internal static bool GroundPathTargetAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).NextSwingLandingEventIdentity != 0;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-last-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, GroundPathGroup)]
        internal static ulong GroundPathLastLandingEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).LastLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-next-swing-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, GroundPathGroup)]
        internal static ulong GroundPathNextSwingLandingEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).NextSwingLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-trajectory-generation", 1,
            DiagnosticValueKind.UInt64, "identity", Main, GroundPathGroup)]
        internal static ulong GroundPathTrajectoryGeneration(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).TrajectoryGeneration;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-authority-tick", 1,
            DiagnosticValueKind.UInt64, "frame", Main, GroundPathGroup)]
        internal static ulong GroundPathAuthorityTick(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).AuthorityTick;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-last-future-body-translation-source-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, GroundPathGroup)]
        internal static string GroundPathLastFutureBodyTranslationSourceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).LastFutureBodyTranslationSourceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-next-swing-future-body-translation-source-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, GroundPathGroup)]
        internal static string GroundPathNextSwingFutureBodyTranslationSourceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).NextSwingFutureBodyTranslationSourceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-last-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathLastLanding(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).LastLanding);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-next-swing-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathNextSwingLanding(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).NextSwingLanding);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-last-landing-normal", 1,
            DiagnosticValueKind.Vector3, "direction", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathLastLandingNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).LastLandingNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-next-swing-landing-normal", 1,
            DiagnosticValueKind.Vector3, "direction", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathNextSwingLandingNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).NextSwingLandingNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-last-landing-surface-identity", 1,
            DiagnosticValueKind.Int32, "identity", Main, GroundPathGroup)]
        internal static int GroundPathLastLandingSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).LastLandingSurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-next-swing-landing-surface-identity", 1,
            DiagnosticValueKind.Int32, "identity", Main, GroundPathGroup)]
        internal static int GroundPathNextSwingLandingSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).NextSwingLandingSurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-component-up", 1,
            DiagnosticValueKind.Vector3, "direction", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathComponentUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).ComponentUp);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-axis-start", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathAxisStart(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).Query.AxisStart);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-axis-end", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathAxisEnd(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).Query.AxisEnd);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-radius", 1,
            DiagnosticValueKind.Float32, "metres", Main, GroundPathGroup)]
        internal static float GroundPathRadius(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).Query.Radius;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-maximum-axis-segment-length", 1,
            DiagnosticValueKind.Float32, "metres", Main, GroundPathGroup)]
        internal static float GroundPathMaximumAxisSegmentLength(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).Query.MaximumAxisSegmentLength;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-direction", 1,
            DiagnosticValueKind.Vector3, "direction", Main, GroundPathGroup)]
        internal static DiagnosticVector3 GroundPathDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).Query.Direction);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-maximum-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, GroundPathGroup)]
        internal static float GroundPathMaximumDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).Query.MaximumDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-layer-mask", 1,
            DiagnosticValueKind.Int32, "bitmask", Main, GroundPathGroup)]
        internal static int GroundPathLayerMask(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).Query.LayerMask;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-segment-hit-capacity", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup)]
        internal static int GroundPathSegmentHitCapacity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).Query.SegmentHitCapacity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-contact-capacity", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup)]
        internal static int GroundPathContactCapacity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).Query.ContactCapacity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-segment-count", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup)]
        internal static int GroundPathSegmentCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).SegmentCount;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-contact-count", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup)]
        internal static int GroundPathContactCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).ContactCount;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-surface-state", 1,
            DiagnosticValueKind.Int32, "category", Main, GroundPathGroup)]
        internal static int GroundSurfaceState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)GroundPath(in view, in metadata).SurfaceCoverage.State;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-surface-world-revision", 1,
            DiagnosticValueKind.UInt64, "identity", Main, GroundPathGroup)]
        internal static ulong GroundSurfaceWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).SurfaceCoverage.WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-surface-segment-count", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup)]
        internal static int GroundSurfaceSegmentCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).SurfaceCoverage.Count;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-edge-count", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup)]
        internal static int GroundPathEdgeCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).EdgeCount;

        [DiagnosticField(Capability, GroundPathHasInvalidSegment, 1,
            DiagnosticValueKind.Boolean, "none", Main, GroundPathGroup)]
        internal static bool GroundPathHasInvalidSegmentValue(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).HasInvalidSegment;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-first-invalid-segment-index", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup, AvailabilityFieldId = GroundPathHasInvalidSegment)]
        internal static int GroundPathFirstInvalidSegmentIndex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).FirstInvalidSegmentIndex;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-first-invalid-segment-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, GroundPathGroup, AvailabilityFieldId = GroundPathHasInvalidSegment)]
        internal static ulong GroundPathFirstInvalidSegmentIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).FirstInvalidSegmentIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-first-invalid-segment-bottom", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GroundPathGroup, AvailabilityFieldId = GroundPathHasInvalidSegment)]
        internal static DiagnosticVector3 GroundPathFirstInvalidSegmentBottom(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).FirstInvalidSegmentBottom);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-first-invalid-segment-top", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GroundPathGroup, AvailabilityFieldId = GroundPathHasInvalidSegment)]
        internal static DiagnosticVector3 GroundPathFirstInvalidSegmentTop(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(GroundPath(in view, in metadata).FirstInvalidSegmentTop);

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-first-invalid-segment-vertical-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, GroundPathGroup, AvailabilityFieldId = GroundPathHasInvalidSegment)]
        internal static float GroundPathFirstInvalidSegmentVerticalDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).FirstInvalidSegmentVerticalDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-path-maximum-reachable-vertical-edge", 1,
            DiagnosticValueKind.Float32, "metres", Main, GroundPathGroup)]
        internal static float GroundPathMaximumReachableVerticalEdge(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).MaximumReachableVerticalEdge;

        [DiagnosticField(Capability, "character-foot-ik/main/ground-envelope-vertex-count", 1,
            DiagnosticValueKind.Int32, "count", Main, GroundPathGroup)]
        internal static int GroundEnvelopeVertexCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            GroundPath(in view, in metadata).EnvelopeVertexCount;

        static CharacterFootGroundPathDiagnostics GroundPath(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).GroundPath;
    }
}
