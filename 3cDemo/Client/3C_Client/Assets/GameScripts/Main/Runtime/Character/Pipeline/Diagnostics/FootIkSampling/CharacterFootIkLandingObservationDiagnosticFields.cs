using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string LandingObservationGroup = "landing-observation";
        const string FutureBodyTranslationAvailableField =
            "character-foot-ik/main/future-body-translation-available";
        const string RawLandingAvailableField =
            "character-foot-ik/main/raw-landing-available";
        const string QuerySelectedCandidateAvailableField =
            "character-foot-ik/main/query-selected-candidate-available";
        const string LandingAcceptedField = "character-foot-ik/main/accepted";

        [DiagnosticField(Capability, FutureBodyTranslationAvailableField, 1,
            DiagnosticValueKind.Boolean, "none", Main, LandingObservationGroup)]
        internal static bool FutureBodyTranslationAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).FutureBodyTranslationAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/future-body-relative-translation", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup, AvailabilityFieldId = FutureBodyTranslationAvailableField)]
        internal static DiagnosticVector3 FutureBodyRelativeTranslation(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).FutureBodyRelativeTranslation);

        [DiagnosticField(Capability, "character-foot-ik/main/future-body-translation-velocity", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup, AvailabilityFieldId = FutureBodyTranslationAvailableField)]
        internal static DiagnosticVector3 FutureBodyTranslationVelocity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).FutureBodyTranslationVelocity);

        [DiagnosticField(Capability, "character-foot-ik/main/current-animated-sole", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup)]
        internal static DiagnosticVector3 CurrentAnimatedSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).CurrentAnimatedSole);

        [DiagnosticField(Capability, RawLandingAvailableField, 1,
            DiagnosticValueKind.Boolean, "none", Main, LandingObservationGroup)]
        internal static bool RawLandingAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).RawLandingAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/raw-landing-candidate", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup, AvailabilityFieldId = RawLandingAvailableField)]
        internal static DiagnosticVector3 RawLandingCandidate(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).RawLandingCandidate);

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, LandingObservationGroup)]
        internal static ulong LandingObservationIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).Identity;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-world-revision", 1,
            DiagnosticValueKind.UInt64, "identity", Main, LandingObservationGroup)]
        internal static ulong LandingObservationWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-source-sample-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, LandingObservationGroup)]
        internal static ulong LandingObservationSourceSampleIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).SourceSampleIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-source-sample-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, LandingObservationGroup)]
        internal static int LandingObservationSourceSampleCycle(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).SourceSampleCycle;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-cache-state", 1,
            DiagnosticValueKind.Int32, "category", Main, LandingObservationGroup)]
        internal static int LandingObservationCacheState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Observation(in view, in metadata).CacheState;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-query-executed", 1,
            DiagnosticValueKind.Boolean, "none", Main, LandingObservationGroup)]
        internal static bool LandingObservationQueryExecuted(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).QueryExecutedThisFrame;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-query-purpose", 1,
            DiagnosticValueKind.Int32, "category", Main, LandingObservationGroup)]
        internal static int LandingObservationQueryPurpose(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Observation(in view, in metadata).QueryPurpose;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-refresh-mode", 1,
            DiagnosticValueKind.Int32, "category", Main, LandingObservationGroup)]
        internal static int LandingObservationRefreshMode(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Observation(in view, in metadata).RefreshMode;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-query-reason", 1,
            DiagnosticValueKind.Int32, "category", Main, LandingObservationGroup)]
        internal static int LandingObservationQueryReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Observation(in view, in metadata).QueryReason;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-canonical-raw", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup)]
        internal static DiagnosticVector3 LandingObservationCanonicalRaw(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Observation(in view, in metadata).CanonicalRawLanding);

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-canonical-component-up", 1,
            DiagnosticValueKind.Vector3, "direction", Main, LandingObservationGroup)]
        internal static DiagnosticVector3 LandingObservationCanonicalComponentUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Observation(in view, in metadata).CanonicalComponentUp);

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-candidate-raw", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup)]
        internal static DiagnosticVector3 LandingObservationCandidateRaw(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Observation(in view, in metadata).CandidateRawLanding);

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-candidate-component-up", 1,
            DiagnosticValueKind.Vector3, "direction", Main, LandingObservationGroup)]
        internal static DiagnosticVector3 LandingObservationCandidateComponentUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Observation(in view, in metadata).CandidateComponentUp);

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-query-input-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, LandingObservationGroup)]
        internal static float LandingObservationQueryInputDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).QueryInputDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-query-component-up-angle-degrees", 1,
            DiagnosticValueKind.Float32, "degrees", Main, LandingObservationGroup)]
        internal static float LandingObservationQueryComponentUpAngleDegrees(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).QueryComponentUpAngleDegrees;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-prediction-input-accumulation-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, LandingObservationGroup)]
        internal static float LandingObservationPredictionInputAccumulationDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).PredictionInputAccumulationDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-observation-component-up-change-angle-degrees", 1,
            DiagnosticValueKind.Float32, "degrees", Main, LandingObservationGroup)]
        internal static float LandingObservationComponentUpChangeAngleDegrees(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Observation(in view, in metadata).ComponentUpChangeAngleDegrees;

        [DiagnosticField(Capability, "character-foot-ik/main/query-shape", 1,
            DiagnosticValueKind.Int32, "category", Main, LandingObservationGroup)]
        internal static int QueryShape(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Query(in view, in metadata).Shape;

        [DiagnosticField(Capability, "character-foot-ik/main/query-purpose", 1,
            DiagnosticValueKind.Int32, "category", Main, LandingObservationGroup)]
        internal static int QueryPurpose(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Query(in view, in metadata).Purpose;

        [DiagnosticField(Capability, "character-foot-ik/main/query-foot-index", 1,
            DiagnosticValueKind.Int32, "count", Main, LandingObservationGroup)]
        internal static int QueryFootIndex(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Query(in view, in metadata).FootIndex;

        [DiagnosticField(Capability, "character-foot-ik/main/query-origin", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup)]
        internal static DiagnosticVector3 QueryOrigin(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Query(in view, in metadata).Origin);

        [DiagnosticField(Capability, "character-foot-ik/main/query-direction", 1,
            DiagnosticValueKind.Vector3, "direction", Main, LandingObservationGroup)]
        internal static DiagnosticVector3 QueryDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Query(in view, in metadata).Direction);

        [DiagnosticField(Capability, "character-foot-ik/main/query-maximum-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, LandingObservationGroup)]
        internal static float QueryMaximumDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Query(in view, in metadata).MaximumDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/query-radius", 1,
            DiagnosticValueKind.Float32, "metres", Main, LandingObservationGroup)]
        internal static float QueryRadius(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Query(in view, in metadata).Radius;

        [DiagnosticField(Capability, "character-foot-ik/main/query-layer-mask", 1,
            DiagnosticValueKind.Int32, "bitmask", Main, LandingObservationGroup)]
        internal static int QueryLayerMask(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Query(in view, in metadata).LayerMask;

        [DiagnosticField(Capability, "character-foot-ik/main/query-minimum-ground-normal-dot", 1,
            DiagnosticValueKind.Float32, "unitless", Main, LandingObservationGroup)]
        internal static float QueryMinimumGroundNormalDot(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Query(in view, in metadata).MinimumGroundNormalDot;

        [DiagnosticField(Capability, "character-foot-ik/main/query-candidate-selection-state", 1,
            DiagnosticValueKind.Int32, "category", Main, LandingObservationGroup)]
        internal static int QueryCandidateSelectionState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)QuerySelection(in view, in metadata).State;

        [DiagnosticField(Capability, "character-foot-ik/main/query-valid-candidate-count", 1,
            DiagnosticValueKind.Int32, "count", Main, LandingObservationGroup)]
        internal static int QueryValidCandidateCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            QuerySelection(in view, in metadata).ValidCandidateCount;

        [DiagnosticField(Capability, QuerySelectedCandidateAvailableField, 1,
            DiagnosticValueKind.Boolean, "none", Main, LandingObservationGroup)]
        internal static bool QuerySelectedCandidateAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            QuerySelection(in view, in metadata).Selected.IsAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/query-selected-surface-identity", 1,
            DiagnosticValueKind.Int32, "identity", Main, LandingObservationGroup, AvailabilityFieldId = QuerySelectedCandidateAvailableField)]
        internal static int QuerySelectedSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            QuerySelection(in view, in metadata).Selected.SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/query-selected-point", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup, AvailabilityFieldId = QuerySelectedCandidateAvailableField)]
        internal static DiagnosticVector3 QuerySelectedPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(QuerySelection(in view, in metadata).Selected.Point);

        [DiagnosticField(Capability, "character-foot-ik/main/query-selected-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, LandingObservationGroup, AvailabilityFieldId = QuerySelectedCandidateAvailableField)]
        internal static float QuerySelectedDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            QuerySelection(in view, in metadata).Selected.Distance;

        [DiagnosticField(Capability, LandingAcceptedField, 1,
            DiagnosticValueKind.Boolean, "none", Main, LandingObservationGroup)]
        internal static bool Accepted(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).Accepted;

        [DiagnosticField(Capability, "character-foot-ik/main/surface-identity", 1,
            DiagnosticValueKind.Int32, "identity", Main, LandingObservationGroup, AvailabilityFieldId = LandingAcceptedField)]
        internal static int SurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/landing-point", 1,
            DiagnosticValueKind.Vector3, "metres", Main, LandingObservationGroup, AvailabilityFieldId = LandingAcceptedField)]
        internal static DiagnosticVector3 LandingPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).LandingPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/landing-normal", 1,
            DiagnosticValueKind.Vector3, "direction", Main, LandingObservationGroup, AvailabilityFieldId = LandingAcceptedField)]
        internal static DiagnosticVector3 LandingNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).LandingNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/query-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, LandingObservationGroup, AvailabilityFieldId = LandingAcceptedField)]
        internal static float QueryDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).QueryDistance;

        static CharacterFootLandingObservationDiagnostics Observation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).Observation;

        static CharacterFootPlacementQueryRequest Query(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).Query;

        static CharacterFootLandingQuerySelectionDiagnostics QuerySelection(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).QuerySelection;
    }
}
