using System.Collections.Generic;
using UnityEngine;
using ThirdPersonCharacter.Pipeline.Animation;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootEventCsvSource;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootEventCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootEventSample>;
using EventColumn = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Animation.AnimationFootMotionEventOccurrence, ThirdPersonCharacter.Pipeline.Editor.CharacterFootEventOccurrenceSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootEventCsvSource
    {
        internal CharacterFootEventCsvSource(bool available, AnimationFootMotionEventFrame events)
        {
            Valid = available && events.IsValid;
            Events = events;
            Current = Valid ? events.CurrentContact : default;
            Next = Valid ? events.NextLanding : default;
        }

        internal bool Valid { get; }
        internal AnimationFootMotionEventFrame Events { get; }
        internal AnimationFootMotionEventOccurrence Current { get; }
        internal AnimationFootMotionEventOccurrence Next { get; }
    }

    internal sealed class CharacterFootEventOccurrenceSample
    {
        internal bool Available;
        internal ulong Identity;
        internal int Ordinal;
        internal int Cycle;
        internal float Distance;
        internal Vector3 RootLocalLanding;
    }

    internal sealed class CharacterFootEventSample
    {
        internal string Phase;
        internal float ApproachProgress;
        internal float TimeToLandingSeconds;
        internal bool InApproach;
        internal CharacterFootEventOccurrenceSample Current = new CharacterFootEventOccurrenceSample();
        internal CharacterFootEventOccurrenceSample Next = new CharacterFootEventOccurrenceSample();
    }

    internal static class CharacterFootEventColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootEventSample> Output = Create("Formal");
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootEventSample> Input = Create("InputFormal");

        static CharacterFootCsvGroup<Source, CharacterFootEventSample> Create(string prefix)
        {
            var columns = new List<Column>
            {
                Column.Create(prefix + "EventPhase", Codecs.Text, Unit.Category,
                    (in Source source) => source.Valid ? source.Events.Phase.ToString() : string.Empty,
                    (target, value) => target.Phase = value),
                Column.Create(prefix + "EventApproachContactToLandingProgress", Codecs.Float32, Unit.Unitless,
                    (in Source source) => source.Valid ? source.Events.ApproachContactToLandingProgress : 0f,
                    (target, value) => target.ApproachProgress = value),
                Column.Create(prefix + "EventTimeToLandingSeconds", Codecs.Float32, Unit.Seconds,
                    (in Source source) => source.Valid ? source.Events.TimeToLandingSeconds : 0f,
                    (target, value) => target.TimeToLandingSeconds = value),
                Column.Create(prefix + "InApproachContactToLanding", Codecs.Boolean, Unit.None,
                    (in Source source) => source.Valid && source.Events.InApproachContactToLanding,
                    (target, value) => target.InApproach = value)
            };
            columns.AddRange(CreateOccurrence(prefix + "CurrentContact", prefix + "CurrentContact")
                .Project<Source, CharacterFootEventSample>((in Source source) => source.Current, target => target.Current));
            columns.AddRange(CreateOccurrence(prefix + "NextLanding", prefix + "Next")
                .Project<Source, CharacterFootEventSample>((in Source source) => source.Next, target => target.Next));
            return new CharacterFootCsvGroup<Source, CharacterFootEventSample>(
                prefix + "Events", () => new CharacterFootEventSample(), columns.ToArray());
        }

        static CharacterFootCsvGroup<AnimationFootMotionEventOccurrence, CharacterFootEventOccurrenceSample> CreateOccurrence(
            string prefix, string pointPrefix) =>
            new CharacterFootCsvGroup<AnimationFootMotionEventOccurrence, CharacterFootEventOccurrenceSample>(
                prefix, () => new CharacterFootEventOccurrenceSample(),
                new EventColumn[]
                {
                    EventColumn.Create(prefix + "EventAvailable", Codecs.Boolean, Unit.None,
                        (in AnimationFootMotionEventOccurrence source) => source.IsValid, (target, value) => target.Available = value),
                    EventColumn.Create(prefix + "EventIdentity", Codecs.UInt64, Unit.Identity,
                        (in AnimationFootMotionEventOccurrence source) => source.IsBound ? source.Identity : 0UL, (target, value) => target.Identity = value),
                    EventColumn.Create(prefix + "EventOrdinal", Codecs.Int32, Unit.Count,
                        (in AnimationFootMotionEventOccurrence source) => source.IsValid ? source.Ordinal : 0, (target, value) => target.Ordinal = value),
                    EventColumn.Create(prefix + "EventCycle", Codecs.Int32, Unit.Count,
                        (in AnimationFootMotionEventOccurrence source) => source.IsValid ? source.LandingCycle : 0, (target, value) => target.Cycle = value),
                    EventColumn.Create(prefix + "EventDistance", Codecs.Float32, Unit.Metres,
                        (in AnimationFootMotionEventOccurrence source) => source.IsValid ? source.Distance : 0f, (target, value) => target.Distance = value,
                        prefix + "EventAvailable"),
                    EventColumn.Create(pointPrefix + "RootLocalLanding", Codecs.Vector, Unit.Metres,
                        (in AnimationFootMotionEventOccurrence source) => source.IsValid ? source.RootLocalLanding : Vector3.zero,
                        (target, value) => target.RootLocalLanding = value, prefix + "EventAvailable")
                });
    }
}
