using UnityEngine;
using ThirdPersonCharacter.Pipeline.Presentation;
using static ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvValues;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootGroundPathDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootGroundPathDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootGroundPathSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootGroundPathSample
    {
        internal string State;
        internal string RejectReason;
        internal ulong InputIdentity;
        internal bool QueryExecuted;
        internal bool TargetAvailable;
        internal ulong LastLandingEventIdentity;
        internal ulong NextSwingLandingEventIdentity;
        internal ulong TrajectoryGeneration;
        internal ulong AuthorityTick;
        internal string LastFutureBodyTranslationSourceIdentity;
        internal string NextSwingFutureBodyTranslationSourceIdentity;
        internal Vector3 LastLanding;
        internal Vector3 NextSwingLanding;
        internal Vector3 LastLandingNormal;
        internal Vector3 NextSwingLandingNormal;
        internal int LastLandingSurfaceIdentity;
        internal int NextSwingLandingSurfaceIdentity;
        internal Vector3 ComponentUp;
        internal Vector3 AxisStart;
        internal Vector3 AxisEnd;
        internal float Radius;
        internal float MaximumAxisSegmentLength;
        internal Vector3 Direction;
        internal float MaximumDistance;
        internal int LayerMask;
        internal int SegmentHitCapacity;
        internal int ContactCapacity;
        internal int SegmentCount;
        internal int ContactCount;
        internal CharacterFootGroundSurfaceState SurfaceState;
        internal ulong SurfaceWorldRevision;
        internal int SurfaceSegmentCount;
        internal int EdgeCount;
        internal bool HasInvalidSegment;
        internal int FirstInvalidSegmentIndex;
        internal ulong FirstInvalidSegmentIdentity;
        internal Vector3 FirstInvalidSegmentBottom;
        internal Vector3 FirstInvalidSegmentTop;
        internal float FirstInvalidSegmentVerticalDistance;
        internal float MaximumReachableVerticalEdge;
        internal int EnvelopeVertexCount;
    }

    internal static class CharacterFootGroundPathColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootGroundPathSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootGroundPathSample>(
                "GroundPath", () => new CharacterFootGroundPathSample(), new Column[]
                {
                    Column.Create("GroundPathState", Codecs.Text, Unit.Category,
                        (in Source source) => source.State.ToString(), (target, value) => target.State = value),
                    Column.Create("GroundPathRejectReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.RejectReason.ToString(), (target, value) => target.RejectReason = value),
                    Column.Create("GroundPathInputIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.InputIdentity, (target, value) => target.InputIdentity = value),
                    Column.Create("GroundPathQueryExecuted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.QueryExecuted, (target, value) => target.QueryExecuted = value),
                    Column.Create("GroundPathTargetAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.NextSwingLandingEventIdentity != 0, (target, value) => target.TargetAvailable = value),
                    Column.Create("GroundPathLastLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.LastLandingEventIdentity, (target, value) => target.LastLandingEventIdentity = value),
                    Column.Create("GroundPathNextSwingLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.NextSwingLandingEventIdentity, (target, value) => target.NextSwingLandingEventIdentity = value),
                    Column.Create("GroundPathTrajectoryGeneration", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.TrajectoryGeneration, (target, value) => target.TrajectoryGeneration = value),
                    Column.Create("GroundPathAuthorityTick", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.AuthorityTick, (target, value) => target.AuthorityTick = value),
                    Column.Create("GroundPathLastFutureBodyTranslationSourceIdentity", Codecs.Text, Unit.Identity,
                        (in Source source) => source.LastFutureBodyTranslationSourceIdentity, (target, value) => target.LastFutureBodyTranslationSourceIdentity = value),
                    Column.Create("GroundPathNextSwingFutureBodyTranslationSourceIdentity", Codecs.Text, Unit.Identity,
                        (in Source source) => source.NextSwingFutureBodyTranslationSourceIdentity, (target, value) => target.NextSwingFutureBodyTranslationSourceIdentity = value),
                    Column.Create("GroundPathLastLanding", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.LastLanding, (target, value) => target.LastLanding = value),
                    Column.Create("GroundPathNextSwingLanding", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.NextSwingLanding, (target, value) => target.NextSwingLanding = value),
                    Column.Create("GroundPathLastLandingNormal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.LastLandingNormal, (target, value) => target.LastLandingNormal = value),
                    Column.Create("GroundPathNextSwingLandingNormal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.NextSwingLandingNormal, (target, value) => target.NextSwingLandingNormal = value),
                    Column.Create("GroundPathLastLandingSurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.LastLandingSurfaceIdentity, (target, value) => target.LastLandingSurfaceIdentity = value),
                    Column.Create("GroundPathNextSwingLandingSurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.NextSwingLandingSurfaceIdentity, (target, value) => target.NextSwingLandingSurfaceIdentity = value),
                    Column.Create("GroundPathComponentUp", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.ComponentUp, (target, value) => target.ComponentUp = value),
                    Column.Create("GroundPathAxisStart", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Query.AxisStart, (target, value) => target.AxisStart = value),
                    Column.Create("GroundPathAxisEnd", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Query.AxisEnd, (target, value) => target.AxisEnd = value),
                    Column.Create("GroundPathRadius", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Query.Radius, (target, value) => target.Radius = value),
                    Column.Create("GroundPathMaximumAxisSegmentLength", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Query.MaximumAxisSegmentLength, (target, value) => target.MaximumAxisSegmentLength = value),
                    Column.Create("GroundPathDirection", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Query.Direction, (target, value) => target.Direction = value),
                    Column.Create("GroundPathMaximumDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Query.MaximumDistance, (target, value) => target.MaximumDistance = value),
                    Column.Create("GroundPathLayerMask", Codecs.Int32, Unit.Bitmask,
                        (in Source source) => source.Query.LayerMask, (target, value) => target.LayerMask = value),
                    Column.Create("GroundPathSegmentHitCapacity", Codecs.Int32, Unit.Count,
                        (in Source source) => source.Query.SegmentHitCapacity, (target, value) => target.SegmentHitCapacity = value),
                    Column.Create("GroundPathContactCapacity", Codecs.Int32, Unit.Count,
                        (in Source source) => source.Query.ContactCapacity, (target, value) => target.ContactCapacity = value),
                    Column.Create("GroundPathSegmentCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.SegmentCount, (target, value) => target.SegmentCount = value),
                    Column.Create("GroundPathContactCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.ContactCount, (target, value) => target.ContactCount = value),
                    Column.Create("GroundSurfaceState", Codecs.Text, Unit.Category,
                        (in Source source) => source.SurfaceCoverage.State.ToString(), (target, value) => target.SurfaceState = ParseEnumValue<CharacterFootGroundSurfaceState>(value, "GroundSurfaceState")),
                    Column.Create("GroundSurfaceWorldRevision", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.SurfaceCoverage.WorldRevision, (target, value) => target.SurfaceWorldRevision = value),
                    Column.Create("GroundSurfaceSegmentCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.SurfaceCoverage.Count, (target, value) => target.SurfaceSegmentCount = value),
                    Column.Create("GroundPathEdgeCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.EdgeCount, (target, value) => target.EdgeCount = value),
                    Column.Create("GroundPathHasInvalidSegment", Codecs.Boolean, Unit.None,
                        (in Source source) => source.HasInvalidSegment, (target, value) => target.HasInvalidSegment = value),
                    Column.Create("GroundPathFirstInvalidSegmentIndex", Codecs.Int32, Unit.Count,
                        (in Source source) => source.FirstInvalidSegmentIndex, (target, value) => target.FirstInvalidSegmentIndex = value, "GroundPathHasInvalidSegment"),
                    Column.Create("GroundPathFirstInvalidSegmentIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.FirstInvalidSegmentIdentity, (target, value) => target.FirstInvalidSegmentIdentity = value, "GroundPathHasInvalidSegment"),
                    Column.Create("GroundPathFirstInvalidSegmentBottom", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.FirstInvalidSegmentBottom, (target, value) => target.FirstInvalidSegmentBottom = value, "GroundPathHasInvalidSegment"),
                    Column.Create("GroundPathFirstInvalidSegmentTop", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.FirstInvalidSegmentTop, (target, value) => target.FirstInvalidSegmentTop = value, "GroundPathHasInvalidSegment"),
                    Column.Create("GroundPathFirstInvalidSegmentVerticalDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.FirstInvalidSegmentVerticalDistance, (target, value) => target.FirstInvalidSegmentVerticalDistance = value, "GroundPathHasInvalidSegment"),
                    Column.Create("GroundPathMaximumReachableVerticalEdge", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.MaximumReachableVerticalEdge, (target, value) => target.MaximumReachableVerticalEdge = value),
                    Column.Create("GroundEnvelopeVertexCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.EnvelopeVertexCount, (target, value) => target.EnvelopeVertexCount = value),
                });
    }
}
