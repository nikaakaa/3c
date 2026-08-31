using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSupportTargetDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSupportTargetDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootSupportTargetSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootSupportTargetSample
    {
        internal bool Available;
        internal ulong Frame;
        internal ulong Completion;
        internal string Side;
        internal Vector3 Position;
        internal Vector3 Normal;
        internal int Surface;
        internal ulong WorldRevision;
        internal string Kind;
        internal string PositionSource;
        internal ulong PositionFrame;
        internal ulong PositionCompletion;
        internal ulong PositionEvent;
        internal ulong PositionPath;
        internal string NormalSource;
        internal ulong NormalFrame;
        internal ulong NormalCompletion;
        internal ulong NormalEvent;
    }

    internal static class CharacterFootSupportTargetColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootSupportTargetSample> Selected =
            Create("FootMotionSelectedSupportTarget");

        internal static CharacterFootCsvGroup<Source, CharacterFootSupportTargetSample> Create(string prefix) =>
            new CharacterFootCsvGroup<Source, CharacterFootSupportTargetSample>(
                prefix,
                () => new CharacterFootSupportTargetSample(),
                new Column[]
                {
                    Column.Create(prefix + "Available", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Available, (target, value) => target.Available = value),
                    Column.Create(prefix + "FrameSequence", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.FrameSequence, (target, value) => target.Frame = value),
                    Column.Create(prefix + "CompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CompletionIdentity, (target, value) => target.Completion = value),
                    Column.Create(prefix + "Side", Codecs.Text, Unit.Category,
                        (in Source source) => source.Side.ToString(), (target, value) => target.Side = value),
                    Column.Create(prefix + "Position", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Position, (target, value) => target.Position = value, prefix + "Available"),
                    Column.Create(prefix + "Normal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.SupportNormal, (target, value) => target.Normal = value, prefix + "Available"),
                    Column.Create(prefix + "SurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.SurfaceIdentity, (target, value) => target.Surface = value, prefix + "Available"),
                    Column.Create(prefix + "WorldRevision", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.WorldRevision, (target, value) => target.WorldRevision = value, prefix + "Available"),
                    Column.Create(prefix + "Kind", Codecs.Text, Unit.Category,
                        (in Source source) => source.Kind.ToString(), (target, value) => target.Kind = value, prefix + "Available"),
                    Column.Create(prefix + "PositionSource", Codecs.Text, Unit.Category,
                        (in Source source) => source.PositionSource.ToString(), (target, value) => target.PositionSource = value, prefix + "Available"),
                    Column.Create(prefix + "PositionFrameSequence", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.PositionFrameSequence, (target, value) => target.PositionFrame = value, prefix + "Available"),
                    Column.Create(prefix + "PositionCompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PositionCompletionIdentity, (target, value) => target.PositionCompletion = value, prefix + "Available"),
                    Column.Create(prefix + "PositionEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PositionEventIdentity, (target, value) => target.PositionEvent = value, prefix + "Available"),
                    Column.Create(prefix + "PositionPathIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PositionPathIdentity, (target, value) => target.PositionPath = value, prefix + "Available"),
                    Column.Create(prefix + "NormalSource", Codecs.Text, Unit.Category,
                        (in Source source) => source.NormalSource.ToString(), (target, value) => target.NormalSource = value, prefix + "Available"),
                    Column.Create(prefix + "NormalFrameSequence", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.NormalFrameSequence, (target, value) => target.NormalFrame = value, prefix + "Available"),
                    Column.Create(prefix + "NormalCompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.NormalCompletionIdentity, (target, value) => target.NormalCompletion = value, prefix + "Available"),
                    Column.Create(prefix + "NormalEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.NormalEventIdentity, (target, value) => target.NormalEvent = value, prefix + "Available"),
                });
    }
}
