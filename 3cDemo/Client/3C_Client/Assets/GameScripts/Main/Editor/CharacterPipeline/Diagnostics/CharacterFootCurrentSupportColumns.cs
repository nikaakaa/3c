using System.Collections.Generic;
using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootCurrentSupportDiagnostics;
using ProbeSource = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootCurrentSupportProbeDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootCurrentSupportDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootCurrentSupportSample>;
using ProbeColumn = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootCurrentSupportProbeDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootCurrentSupportProbeSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootCurrentSupportSample
    {
        internal bool Specified;
        internal bool Available;
        internal string RejectReason;
        internal ulong Frame;
        internal ulong Completion;
        internal ulong WorldRevision;
        internal CharacterFootCurrentSupportProbeSample Heel = new CharacterFootCurrentSupportProbeSample();
        internal CharacterFootCurrentSupportProbeSample Toe = new CharacterFootCurrentSupportProbeSample();
        internal float HeelRequiredDisplacement;
        internal float ToeRequiredDisplacement;
        internal string SelectedProbe;
        internal string SelectionReason;
        internal float SelectionEpsilon;
        internal Vector3 SelectedNormalBeforeNormalization;
        internal CharacterFootSupportTargetSample Target = new CharacterFootSupportTargetSample();
    }

    internal sealed class CharacterFootCurrentSupportProbeSample
    {
        internal string Purpose;
        internal string Kind;
        internal string State;
        internal string RejectReason;
        internal Vector3 ProbePosition;
        internal Vector3 ComponentUp;
        internal Vector3 Origin;
        internal Vector3 Direction;
        internal float MaximumDistance;
        internal float Radius;
        internal int LayerMask;
        internal float MinimumGroundNormalDot;
        internal int HitCapacity;
        internal int CandidateCount;
        internal int Surface;
        internal Vector3 Point;
        internal Vector3 Normal;
        internal float Distance;
        internal ulong WorldRevision;
        internal bool SphereCastExecuted;
        internal bool Accepted;
    }

    internal static class CharacterFootCurrentSupportColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootCurrentSupportSample> Schema = Create();

        static CharacterFootCsvGroup<Source, CharacterFootCurrentSupportSample> Create()
        {
            var columns = new List<Column>();
            columns.Add(Column.Create("CurrentSupportFrameSequence", Codecs.UInt64, Unit.Frame,
                (in Source source) => source.FrameSequence, (target, value) => target.Frame = value));
            columns.Add(Column.Create("CurrentSupportCompletionIdentity", Codecs.UInt64, Unit.Identity,
                (in Source source) => source.CompletionIdentity, (target, value) => target.Completion = value));
            columns.Add(Column.Create("CurrentSupportWorldRevision", Codecs.UInt64, Unit.Identity,
                (in Source source) => source.WorldRevision, (target, value) => target.WorldRevision = value));
            columns.Add(Column.Create("CurrentSupportIsSpecified", Codecs.Boolean, Unit.None,
                (in Source source) => source.IsSpecified, (target, value) => target.Specified = value));
            columns.Add(Column.Create("CurrentSupportAvailable", Codecs.Boolean, Unit.None,
                (in Source source) => source.Available, (target, value) => target.Available = value));
            columns.Add(Column.Create("CurrentSupportRejectReason", Codecs.Text, Unit.Category,
                (in Source source) => source.RejectReason.ToString(), (target, value) => target.RejectReason = value));
            columns.AddRange(CreateProbe("CurrentSupportHeel")
                .Project<Source, CharacterFootCurrentSupportSample>(
                    (in Source source) => source.Heel, target => target.Heel));
            columns.AddRange(CreateProbe("CurrentSupportToe")
                .Project<Source, CharacterFootCurrentSupportSample>(
                    (in Source source) => source.Toe, target => target.Toe));
            columns.Add(Column.Create("CurrentSupportHeelRequiredDisplacement", Codecs.Float32, Unit.Metres,
                (in Source source) => source.HeelRequiredDisplacement, (target, value) => target.HeelRequiredDisplacement = value));
            columns.Add(Column.Create("CurrentSupportToeRequiredDisplacement", Codecs.Float32, Unit.Metres,
                (in Source source) => source.ToeRequiredDisplacement, (target, value) => target.ToeRequiredDisplacement = value));
            columns.Add(Column.Create("CurrentSupportSelectedProbe", Codecs.Text, Unit.Category,
                (in Source source) => source.SelectedProbe.ToString(), (target, value) => target.SelectedProbe = value));
            columns.Add(Column.Create("CurrentSupportSelectionReason", Codecs.Text, Unit.Category,
                (in Source source) => source.SelectionReason.ToString(), (target, value) => target.SelectionReason = value));
            columns.Add(Column.Create("CurrentSupportSelectionEpsilon", Codecs.Float32, Unit.Metres,
                (in Source source) => source.SelectionEpsilon, (target, value) => target.SelectionEpsilon = value));
            columns.Add(Column.Create("CurrentSupportSelectedSupportNormalBeforeNormalization", Codecs.Vector, Unit.Direction,
                (in Source source) => source.SelectedSupportNormalBeforeNormalization, (target, value) => target.SelectedNormalBeforeNormalization = value));
            columns.AddRange(CharacterFootSupportTargetColumns.Create("CurrentSupportTarget")
                .Project<Source, CharacterFootCurrentSupportSample>(
                    (in Source source) => source.Target, target => target.Target));
            return new CharacterFootCsvGroup<Source, CharacterFootCurrentSupportSample>(
                "CurrentSupport", () => new CharacterFootCurrentSupportSample(), columns.ToArray());
        }

        static CharacterFootCsvGroup<ProbeSource, CharacterFootCurrentSupportProbeSample> CreateProbe(string prefix) =>
            new CharacterFootCsvGroup<ProbeSource, CharacterFootCurrentSupportProbeSample>(
                prefix, () => new CharacterFootCurrentSupportProbeSample(),
                new ProbeColumn[]
                {
                    ProbeColumn.Create(prefix + "Purpose", Codecs.Text, Unit.Category,
                        (in ProbeSource source) => source.Purpose.ToString(), (target, value) => target.Purpose = value),
                    ProbeColumn.Create(prefix + "Kind", Codecs.Text, Unit.Category,
                        (in ProbeSource source) => source.Kind.ToString(), (target, value) => target.Kind = value),
                    ProbeColumn.Create(prefix + "State", Codecs.Text, Unit.Category,
                        (in ProbeSource source) => source.State.ToString(), (target, value) => target.State = value),
                    ProbeColumn.Create(prefix + "RejectReason", Codecs.Text, Unit.Category,
                        (in ProbeSource source) => source.RejectReason.ToString(), (target, value) => target.RejectReason = value),
                    ProbeColumn.Create(prefix + "ProbePosition", Codecs.Vector, Unit.Metres,
                        (in ProbeSource source) => source.ProbePosition, (target, value) => target.ProbePosition = value),
                    ProbeColumn.Create(prefix + "ComponentUp", Codecs.Vector, Unit.Direction,
                        (in ProbeSource source) => source.ComponentUp, (target, value) => target.ComponentUp = value),
                    ProbeColumn.Create(prefix + "Origin", Codecs.Vector, Unit.Metres,
                        (in ProbeSource source) => source.Origin, (target, value) => target.Origin = value),
                    ProbeColumn.Create(prefix + "Direction", Codecs.Vector, Unit.Direction,
                        (in ProbeSource source) => source.Direction, (target, value) => target.Direction = value),
                    ProbeColumn.Create(prefix + "MaximumDistance", Codecs.Float32, Unit.Metres,
                        (in ProbeSource source) => source.MaximumDistance, (target, value) => target.MaximumDistance = value),
                    ProbeColumn.Create(prefix + "Radius", Codecs.Float32, Unit.Metres,
                        (in ProbeSource source) => source.Radius, (target, value) => target.Radius = value),
                    ProbeColumn.Create(prefix + "LayerMask", Codecs.Int32, Unit.Bitmask,
                        (in ProbeSource source) => source.LayerMask, (target, value) => target.LayerMask = value),
                    ProbeColumn.Create(prefix + "MinimumGroundNormalDot", Codecs.Float32, Unit.Unitless,
                        (in ProbeSource source) => source.MinimumGroundNormalDot, (target, value) => target.MinimumGroundNormalDot = value),
                    ProbeColumn.Create(prefix + "HitCapacity", Codecs.Int32, Unit.Count,
                        (in ProbeSource source) => source.HitCapacity, (target, value) => target.HitCapacity = value),
                    ProbeColumn.Create(prefix + "CandidateCount", Codecs.Int32, Unit.Count,
                        (in ProbeSource source) => source.CandidateCount, (target, value) => target.CandidateCount = value),
                    ProbeColumn.Create(prefix + "SurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in ProbeSource source) => source.SurfaceIdentity, (target, value) => target.Surface = value, prefix + "Accepted"),
                    ProbeColumn.Create(prefix + "Point", Codecs.Vector, Unit.Metres,
                        (in ProbeSource source) => source.Point, (target, value) => target.Point = value, prefix + "Accepted"),
                    ProbeColumn.Create(prefix + "Normal", Codecs.Vector, Unit.Direction,
                        (in ProbeSource source) => source.Normal, (target, value) => target.Normal = value, prefix + "Accepted"),
                    ProbeColumn.Create(prefix + "Distance", Codecs.Float32, Unit.Metres,
                        (in ProbeSource source) => source.Distance, (target, value) => target.Distance = value, prefix + "Accepted"),
                    ProbeColumn.Create(prefix + "WorldRevision", Codecs.UInt64, Unit.Identity,
                        (in ProbeSource source) => source.WorldRevision, (target, value) => target.WorldRevision = value),
                    ProbeColumn.Create(prefix + "SphereCastExecuted", Codecs.Boolean, Unit.None,
                        (in ProbeSource source) => source.SphereCastExecuted, (target, value) => target.SphereCastExecuted = value),
                    ProbeColumn.Create(prefix + "Accepted", Codecs.Boolean, Unit.None,
                        (in ProbeSource source) => source.Accepted, (target, value) => target.Accepted = value),
                });
    }
}
