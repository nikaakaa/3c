using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootPrimarySupportDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootPrimarySupportDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootPrimarySupportSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootPrimarySupportSample
    {
        internal bool HasValue;
        internal string Side;
        internal ulong LandingEventIdentity;
        internal bool Retained;
    }

    internal static class CharacterFootPrimarySupportColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootPrimarySupportSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootPrimarySupportSample>(
                "PrimarySupport", () => new CharacterFootPrimarySupportSample(), new Column[]
                {
                    Column.Create("PrimarySupportHasValue", Codecs.Boolean, Unit.None,
                        (in Source source) => source.HasValue, (target, value) => target.HasValue = value),
                    Column.Create("PrimarySupportSide", Codecs.Text, Unit.Category,
                        (in Source source) => source.Side.ToString(), (target, value) => target.Side = value, "PrimarySupportHasValue"),
                    Column.Create("PrimarySupportLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.LandingEventIdentity, (target, value) => target.LandingEventIdentity = value, "PrimarySupportHasValue"),
                    Column.Create("PrimarySupportRetained", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Retained, (target, value) => target.Retained = value),
                });
    }
}
