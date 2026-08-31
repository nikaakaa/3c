using UnityEngine;
using ThirdPersonCharacter.Pipeline.Presentation;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootLandingObservationCsvSource;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootLandingObservationCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootLandingObservationSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootLandingObservationCsvSource
    {
        internal CharacterFootLandingObservationCsvSource(in CharacterFootLandingPredictionFootDiagnostics foot)
        {
            Foot = foot;
            Observation = foot.Observation;
            Query = foot.Query;
            Selection = foot.QuerySelection;
            Selected = Selection.Selected;
        }

        internal CharacterFootLandingPredictionFootDiagnostics Foot { get; }
        internal CharacterFootLandingObservationDiagnostics Observation { get; }
        internal CharacterFootPlacementQueryRequest Query { get; }
        internal CharacterFootLandingQuerySelectionDiagnostics Selection { get; }
        internal CharacterFootLandingQueryCandidateDiagnostics Selected { get; }
    }

    internal sealed class CharacterFootLandingObservationSample
    {
        internal bool FutureBodyTranslationAvailable;
        internal Vector3 FutureBodyRelativeTranslation;
        internal Vector3 FutureBodyTranslationVelocity;
        internal Vector3 CurrentAnimatedSole;
        internal bool RawLandingAvailable;
        internal Vector3 RawLanding;
        internal ulong ObservationIdentity;
        internal ulong ObservationWorldRevision;
        internal ulong ObservationSourceSampleIdentity;
        internal int ObservationSourceSampleCycle;
        internal string ObservationCacheState;
        internal bool ObservationQueryExecuted;
        internal string ObservationQueryPurpose;
        internal string ObservationRefreshMode;
        internal string ObservationQueryReason;
        internal Vector3 ObservationCanonicalRaw;
        internal Vector3 ObservationCanonicalComponentUp;
        internal Vector3 ObservationCandidateRaw;
        internal Vector3 ObservationCandidateComponentUp;
        internal float ObservationQueryInputDistance;
        internal float ObservationQueryComponentUpAngleDegrees;
        internal float ObservationPredictionInputAccumulationDistance;
        internal float ObservationComponentUpChangeAngleDegrees;
        internal string QueryShape;
        internal string QueryPurpose;
        internal int QueryFootIndex;
        internal Vector3 QueryOrigin;
        internal Vector3 QueryDirection;
        internal float QueryMaximumDistance;
        internal float QueryRadius;
        internal int QueryLayerMask;
        internal float QueryMinimumGroundNormalDot;
        internal string SelectionState;
        internal int ValidCandidateCount;
        internal bool SelectedAvailable;
        internal int SelectedSurfaceIdentity;
        internal Vector3 SelectedPoint;
        internal float SelectedDistance;
        internal bool Accepted;
        internal int SurfaceIdentity;
        internal Vector3 Point;
        internal Vector3 LandingNormal;
        internal float QueryDistance;
    }

    internal static class CharacterFootLandingObservationColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootLandingObservationSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootLandingObservationSample>(
                "LandingObservation", () => new CharacterFootLandingObservationSample(), new Column[]
                {
                    Column.Create("FutureBodyTranslationAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.FutureBodyTranslationAvailable, (target, value) => target.FutureBodyTranslationAvailable = value),
                    Column.Create("FutureBodyRelativeTranslation", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.FutureBodyRelativeTranslation, (target, value) => target.FutureBodyRelativeTranslation = value, "FutureBodyTranslationAvailable"),
                    Column.Create("FutureBodyTranslationVelocity", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.FutureBodyTranslationVelocity, (target, value) => target.FutureBodyTranslationVelocity = value, "FutureBodyTranslationAvailable"),
                    Column.Create("CurrentAnimatedSole", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.CurrentAnimatedSole, (target, value) => target.CurrentAnimatedSole = value),
                    Column.Create("RawLandingAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.RawLandingAvailable, (target, value) => target.RawLandingAvailable = value),
                    Column.Create("RawLandingCandidate", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.RawLandingCandidate, (target, value) => target.RawLanding = value, "RawLandingAvailable"),
                    Column.Create("LandingObservationIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Observation.Identity, (target, value) => target.ObservationIdentity = value),
                    Column.Create("LandingObservationWorldRevision", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Observation.WorldRevision, (target, value) => target.ObservationWorldRevision = value),
                    Column.Create("LandingObservationSourceSampleIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Observation.SourceSampleIdentity, (target, value) => target.ObservationSourceSampleIdentity = value),
                    Column.Create("LandingObservationSourceSampleCycle", Codecs.Int32, Unit.Count,
                        (in Source source) => source.Observation.SourceSampleCycle, (target, value) => target.ObservationSourceSampleCycle = value),
                    Column.Create("LandingObservationCacheState", Codecs.Text, Unit.Category,
                        (in Source source) => source.Observation.CacheState.ToString(), (target, value) => target.ObservationCacheState = value),
                    Column.Create("LandingObservationQueryExecuted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Observation.QueryExecutedThisFrame, (target, value) => target.ObservationQueryExecuted = value),
                    Column.Create("LandingObservationQueryPurpose", Codecs.Text, Unit.Category,
                        (in Source source) => source.Observation.QueryPurpose.ToString(), (target, value) => target.ObservationQueryPurpose = value),
                    Column.Create("LandingObservationRefreshMode", Codecs.Text, Unit.Category,
                        (in Source source) => source.Observation.RefreshMode.ToString(), (target, value) => target.ObservationRefreshMode = value),
                    Column.Create("LandingObservationQueryReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.Observation.QueryReason.ToString(), (target, value) => target.ObservationQueryReason = value),
                    Column.Create("LandingObservationCanonicalRaw", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Observation.CanonicalRawLanding, (target, value) => target.ObservationCanonicalRaw = value),
                    Column.Create("LandingObservationCanonicalComponentUp", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Observation.CanonicalComponentUp, (target, value) => target.ObservationCanonicalComponentUp = value),
                    Column.Create("LandingObservationCandidateRaw", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Observation.CandidateRawLanding, (target, value) => target.ObservationCandidateRaw = value),
                    Column.Create("LandingObservationCandidateComponentUp", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Observation.CandidateComponentUp, (target, value) => target.ObservationCandidateComponentUp = value),
                    Column.Create("LandingObservationQueryInputDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Observation.QueryInputDistance, (target, value) => target.ObservationQueryInputDistance = value),
                    Column.Create("LandingObservationQueryComponentUpAngleDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.Observation.QueryComponentUpAngleDegrees, (target, value) => target.ObservationQueryComponentUpAngleDegrees = value),
                    Column.Create("LandingObservationPredictionInputAccumulationDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Observation.PredictionInputAccumulationDistance, (target, value) => target.ObservationPredictionInputAccumulationDistance = value),
                    Column.Create("LandingObservationComponentUpChangeAngleDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.Observation.ComponentUpChangeAngleDegrees, (target, value) => target.ObservationComponentUpChangeAngleDegrees = value),
                    Column.Create("QueryShape", Codecs.Text, Unit.Category,
                        (in Source source) => source.Query.Shape.ToString(), (target, value) => target.QueryShape = value),
                    Column.Create("QueryPurpose", Codecs.Text, Unit.Category,
                        (in Source source) => source.Query.Purpose.ToString(), (target, value) => target.QueryPurpose = value),
                    Column.Create("QueryFootIndex", Codecs.Int32, Unit.Count,
                        (in Source source) => source.Query.FootIndex, (target, value) => target.QueryFootIndex = value),
                    Column.Create("QueryOrigin", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Query.Origin, (target, value) => target.QueryOrigin = value),
                    Column.Create("QueryDirection", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Query.Direction, (target, value) => target.QueryDirection = value),
                    Column.Create("QueryMaximumDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Query.MaximumDistance, (target, value) => target.QueryMaximumDistance = value),
                    Column.Create("QueryRadius", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Query.Radius, (target, value) => target.QueryRadius = value),
                    Column.Create("QueryLayerMask", Codecs.Int32, Unit.Bitmask,
                        (in Source source) => source.Query.LayerMask, (target, value) => target.QueryLayerMask = value),
                    Column.Create("QueryMinimumGroundNormalDot", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Query.MinimumGroundNormalDot, (target, value) => target.QueryMinimumGroundNormalDot = value),
                    Column.Create("QueryCandidateSelectionState", Codecs.Text, Unit.Category,
                        (in Source source) => source.Selection.State.ToString(), (target, value) => target.SelectionState = value),
                    Column.Create("QueryValidCandidateCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.Selection.ValidCandidateCount, (target, value) => target.ValidCandidateCount = value),
                    Column.Create("QuerySelectedCandidateAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Selected.IsAvailable, (target, value) => target.SelectedAvailable = value),
                    Column.Create("QuerySelectedSurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.Selected.SurfaceIdentity, (target, value) => target.SelectedSurfaceIdentity = value, "QuerySelectedCandidateAvailable"),
                    Column.Create("QuerySelectedPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Selected.Point, (target, value) => target.SelectedPoint = value, "QuerySelectedCandidateAvailable"),
                    Column.Create("QuerySelectedDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Selected.Distance, (target, value) => target.SelectedDistance = value, "QuerySelectedCandidateAvailable"),
                    Column.Create("Accepted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.Accepted, (target, value) => target.Accepted = value),
                    Column.Create("SurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.Foot.SurfaceIdentity, (target, value) => target.SurfaceIdentity = value, "Accepted"),
                    Column.Create("LandingPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.LandingPoint, (target, value) => target.Point = value, "Accepted"),
                    Column.Create("LandingNormal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Foot.LandingNormal, (target, value) => target.LandingNormal = value, "Accepted"),
                    Column.Create("QueryDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Foot.QueryDistance, (target, value) => target.QueryDistance = value, "Accepted"),
                });
    }
}
