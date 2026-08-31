using System.Collections.Generic;
using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootStepCandidateDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootStepCandidateDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootStepCandidateSample>;
using PhaseColumn = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootStepCandidateDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootStepPhaseSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal class CharacterFootStepPhaseSample
    {
        internal float EventPhase;
        internal float ApproachContactToLandingProgress;
        internal float LandingPhase;
        internal bool AtOrAfterApproachContact;
        internal bool InApproachContactToLanding;
    }

    internal sealed class CharacterFootStepCandidateSample : CharacterFootStepPhaseSample
    {
        internal bool IsValid;
        internal bool IsAuthoritative;
        internal bool HasConsistentLandingEventIdentity;
        internal bool IsPreSwing;
        internal bool IsSwing;
        internal int EventOrdinal;
        internal int SourceLandingCycleOffset;
        internal int SourceSampleCycle;
        internal ulong ContributionContinuityIdentity;
        internal ulong LandingEventIdentity;
        internal float TimeToLandingSeconds;
        internal Vector3 RootLocalLanding;
    }

    internal static class CharacterFootStepColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootStepCandidateSample> Current = Create("CurrentStep");
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootStepCandidateSample> Incoming = Create("IncomingStep");
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootStepPhaseSample> SelectedPhase = CreatePhase("SelectedStep");

        static CharacterFootCsvGroup<Source, CharacterFootStepCandidateSample> Create(string prefix)
        {
            var columns = new List<Column>
            {
                Column.Create(prefix + "IsValid", Codecs.Boolean, Unit.None,
                    (in Source source) => source.IsValid, (target, value) => target.IsValid = value),
                Column.Create(prefix + "IsAuthoritative", Codecs.Boolean, Unit.None,
                    (in Source source) => source.IsAuthoritative, (target, value) => target.IsAuthoritative = value),
                Column.Create(prefix + "HasConsistentLandingEventIdentity", Codecs.Boolean, Unit.None,
                    (in Source source) => source.HasConsistentLandingEventIdentity, (target, value) => target.HasConsistentLandingEventIdentity = value),
                Column.Create(prefix + "IsPreSwing", Codecs.Boolean, Unit.None,
                    (in Source source) => source.IsPreSwing, (target, value) => target.IsPreSwing = value),
                Column.Create(prefix + "IsSwing", Codecs.Boolean, Unit.None,
                    (in Source source) => source.IsSwing, (target, value) => target.IsSwing = value),
                Column.Create(prefix + "EventOrdinal", Codecs.Int32, Unit.Count,
                    (in Source source) => source.EventOrdinal, (target, value) => target.EventOrdinal = value),
                Column.Create(prefix + "SourceLandingCycleOffset", Codecs.Int32, Unit.Count,
                    (in Source source) => source.SourceLandingCycleOffset, (target, value) => target.SourceLandingCycleOffset = value),
                Column.Create(prefix + "SourceSampleCycle", Codecs.Int32, Unit.Count,
                    (in Source source) => source.SourceSampleCycle, (target, value) => target.SourceSampleCycle = value),
                Column.Create(prefix + "ContributionContinuityIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.ContributionContinuityIdentity, (target, value) => target.ContributionContinuityIdentity = value),
                Column.Create(prefix + "LandingEventIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.LandingEventIdentity, (target, value) => target.LandingEventIdentity = value),
                Column.Create(prefix + "TimeToLandingSeconds", Codecs.Float32, Unit.Seconds,
                    (in Source source) => source.TimeToLandingSeconds, (target, value) => target.TimeToLandingSeconds = value),
            };
            columns.AddRange(CreatePhase(prefix).Project<Source, CharacterFootStepCandidateSample>(
                (in Source source) => source, target => target));
            columns.Add(Column.Create(prefix + "RootLocalLanding", Codecs.Vector, Unit.Metres,
                (in Source source) => source.RootLocalLanding, (target, value) => target.RootLocalLanding = value));
            return new CharacterFootCsvGroup<Source, CharacterFootStepCandidateSample>(
                prefix, () => new CharacterFootStepCandidateSample(), columns.ToArray());
        }

        static CharacterFootCsvGroup<Source, CharacterFootStepPhaseSample> CreatePhase(string prefix) =>
            new CharacterFootCsvGroup<Source, CharacterFootStepPhaseSample>(
                prefix, () => new CharacterFootStepPhaseSample(),
                new PhaseColumn[]
                {
                    PhaseColumn.Create(prefix + "EventPhase", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.EventPhase, (target, value) => target.EventPhase = value),
                    PhaseColumn.Create(prefix + "ApproachContactToLandingProgress", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.ApproachContactToLandingProgress, (target, value) => target.ApproachContactToLandingProgress = value),
                    PhaseColumn.Create(prefix + "LandingPhase", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.LandingPhase, (target, value) => target.LandingPhase = value),
                    PhaseColumn.Create(prefix + "AtOrAfterApproachContact", Codecs.Boolean, Unit.None,
                        (in Source source) => source.AtOrAfterApproachContact, (target, value) => target.AtOrAfterApproachContact = value),
                    PhaseColumn.Create(prefix + "InApproachContactToLanding", Codecs.Boolean, Unit.None,
                        (in Source source) => source.InApproachContactToLanding, (target, value) => target.InApproachContactToLanding = value),
                });
    }
}
