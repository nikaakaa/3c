using System;
using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    [DiagnosticTable(CharacterFootIkDiagnosticIdentity.CapabilityId, "ground-geometry", 1, 68)]
    internal static class CharacterFootIkGroundGeometryDiagnosticTable
    {
        const string Capability = CharacterFootIkDiagnosticIdentity.CapabilityId;
        const string Table = "ground-geometry";
        const string Group = "ground-geometry";

        [DiagnosticTableCount(Capability, Table)]
        internal static int Count(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata)
        {
            CharacterFootGroundPathDiagnostics ground = Ground(in view, in metadata);
            return Math.Max(ground.SurfaceCoverage.Count, Math.Max(ground.ContactCount, ground.EnvelopeVertexCount));
        }

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/sample-identity", 1, DiagnosticValueKind.Identity, "identity", Table, Group)]
        internal static string SampleIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => metadata.SampleIdentity;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/frame-sequence", 1, DiagnosticValueKind.Int32, "frame", Table, Group)]
        internal static int FrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => checked((int)view.Lineage.PresentationFrame);

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Table, Group)]
        internal static ulong CompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => view.Lineage.CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/side", 1, DiagnosticValueKind.Int32, "category", Table, Group)]
        internal static int Side(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => (int)metadata.Side;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-path-input-identity", 1, DiagnosticValueKind.UInt64, "identity", Table, Group)]
        internal static ulong GroundPathInputIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Ground(in view, in metadata).InputIdentity;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-contact-index", 1, DiagnosticValueKind.Int32, "count", Table, Group)]
        internal static int GroundContactIndex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => row < Ground(in view, in metadata).ContactCount ? row : -1;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-contact-segment-index", 1, DiagnosticValueKind.Int32, "count", Table, Group)]
        internal static int GroundContactSegmentIndex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Contact(in view, in metadata, row).SegmentIndex;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-contact-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Table, Group)]
        internal static int GroundContactSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Contact(in view, in metadata, row).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-contact-candidate-identity", 1, DiagnosticValueKind.UInt64, "identity", Table, Group)]
        internal static ulong GroundContactCandidateIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Contact(in view, in metadata, row).CandidateIdentity;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-contact-position", 1, DiagnosticValueKind.Vector3, "metres", Table, Group)]
        internal static DiagnosticVector3 GroundContactPosition(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Vector3(Contact(in view, in metadata, row).Position);

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-contact-normal", 1, DiagnosticValueKind.Vector3, "direction", Table, Group)]
        internal static DiagnosticVector3 GroundContactNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Vector3(Contact(in view, in metadata, row).Normal);

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-contact-query-distance", 1, DiagnosticValueKind.Float32, "metres", Table, Group)]
        internal static float GroundContactQueryDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Contact(in view, in metadata, row).QueryDistance;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-envelope-vertex-index", 1, DiagnosticValueKind.Int32, "count", Table, Group)]
        internal static int GroundEnvelopeVertexIndex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => row < Ground(in view, in metadata).EnvelopeVertexCount ? row : -1;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-envelope-vertex", 1, DiagnosticValueKind.Vector3, "metres", Table, Group)]
        internal static DiagnosticVector3 GroundEnvelopeVertex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Vector3(Envelope(in view, in metadata, row).Position);

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-surface-segment-index", 1, DiagnosticValueKind.Int32, "count", Table, Group)]
        internal static int GroundSurfaceSegmentIndex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => row < Ground(in view, in metadata).SurfaceCoverage.Count ? row : -1;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Table, Group)]
        internal static int GroundSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Surface(in view, in metadata, row).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-surface-face-index", 1, DiagnosticValueKind.Int32, "count", Table, Group)]
        internal static int GroundSurfaceFaceIndex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => row < Ground(in view, in metadata).SurfaceCoverage.Count ? Surface(in view, in metadata, row).FaceIdentity : -1;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-surface-start-distance", 1, DiagnosticValueKind.Float32, "metres", Table, Group)]
        internal static float GroundSurfaceStartDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Surface(in view, in metadata, row).Start.x;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-surface-start-height", 1, DiagnosticValueKind.Float32, "metres", Table, Group)]
        internal static float GroundSurfaceStartHeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Surface(in view, in metadata, row).Start.y;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-surface-end-distance", 1, DiagnosticValueKind.Float32, "metres", Table, Group)]
        internal static float GroundSurfaceEndDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Surface(in view, in metadata, row).End.x;

        [DiagnosticField(Capability, "character-foot-ik/ground-geometry/ground-surface-end-height", 1, DiagnosticValueKind.Float32, "metres", Table, Group)]
        internal static float GroundSurfaceEndHeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row) => Surface(in view, in metadata, row).End.y;

        static CharacterFootGroundPathDiagnostics Ground(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata)
        {
            ref readonly CharacterFootLandingPredictionDiagnostics frame = ref view.LandingPrediction;
            return metadata.Side == CharacterFootSide.Left ? frame.Left.GroundPath : frame.Right.GroundPath;
        }

        static CharacterFootGroundContact Contact(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row)
        {
            CharacterFootGroundPathDiagnostics ground = Ground(in view, in metadata);
            return row < ground.ContactCount ? ground.ContactAt(row) : default;
        }

        static CharacterFootGroundEnvelopeVertex Envelope(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row)
        {
            CharacterFootGroundPathDiagnostics ground = Ground(in view, in metadata);
            return row < ground.EnvelopeVertexCount ? ground.EnvelopeVertexAt(row) : default;
        }

        static CharacterFootGroundSurfaceSegment Surface(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata, int row)
        {
            CharacterFootGroundSurfaceDiagnostics surfaces = Ground(in view, in metadata).SurfaceCoverage;
            return row < surfaces.Count ? surfaces.SegmentAt(row) : default;
        }

        static DiagnosticVector3 Vector3(UnityEngine.Vector3 value) => new DiagnosticVector3(value.x, value.y, value.z);
    }
}
