using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootFormalObservationCsvSource;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootFormalObservationCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootFormalObservationSample>;
using InputSource = ThirdPersonCharacter.Pipeline.Editor.CharacterFootFormalInputCsvSource;
using InputColumn = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootFormalInputCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootFormalInputSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootFormalObservationCsvSource
    {
        internal CharacterFootFormalObservationCsvSource(
            bool available, string sourceIdentity, float weight,
            float normalizedTime, in AnimationFootMotionRuntimeSample sample)
        {
            Available = available;
            SourceIdentity = sourceIdentity;
            Weight = weight;
            NormalizedTime = normalizedTime;
            Sample = sample;
        }

        internal bool Available { get; }
        internal string SourceIdentity { get; }
        internal float Weight { get; }
        internal float NormalizedTime { get; }
        internal AnimationFootMotionRuntimeSample Sample { get; }
    }

    internal readonly struct CharacterFootFormalInputCsvSource
    {
        internal CharacterFootFormalInputCsvSource(
            in CharacterFootFormalObservationCsvSource observation,
            in CharacterFootStepObservationInputDiagnostics origin)
        {
            Observation = observation;
            Origin = origin;
        }

        internal CharacterFootFormalObservationCsvSource Observation { get; }
        internal CharacterFootStepObservationInputDiagnostics Origin { get; }
    }

    internal class CharacterFootFormalObservationSample
    {
        internal bool Available;
        internal string SourceIdentity;
        internal float SourceWeight;
        internal float NormalizedTime;
        internal float TimeToLandingSeconds;
        internal float Distance;
        internal float FootHeight;
        internal float ToeHeight;
        internal float ToeSpeed;
        internal float PositionError;
        internal float RotationError;
        internal float Contact;
        internal string LockMode;
        internal float LockWeight;
        internal float Support;
    }

    internal sealed class CharacterFootFormalInputSample : CharacterFootFormalObservationSample
    {
        internal string SourceId;
        internal int ClipBindingIndex;
        internal int SourceCycle;
        internal ulong ContributionContinuityIdentity;
        internal ulong CompletionIdentity;
    }

    internal static class CharacterFootFormalObservationColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootFormalObservationSample> Output = CreateOutput();
        internal static readonly CharacterFootCsvGroup<InputSource, CharacterFootFormalInputSample> Input = CreateInput();

        static CharacterFootCsvGroup<Source, CharacterFootFormalObservationSample> CreateOutput()
        {
            var columns = new List<Column>(CreateHeader("Formal"));
            columns.AddRange(CreateMotion("Formal"));
            return new CharacterFootCsvGroup<Source, CharacterFootFormalObservationSample>(
                "FormalOutput", () => new CharacterFootFormalObservationSample(), columns.ToArray());
        }

        static CharacterFootCsvGroup<InputSource, CharacterFootFormalInputSample> CreateInput()
        {
            Column[] header = CreateHeader("InputFormal");
            var columns = new List<InputColumn>
            {
                Project(header[0]),
                InputColumn.Create("InputFormalStepSourceId", Codecs.Text, Unit.Identity,
                    (in InputSource source) => source.Observation.Available ? source.Origin.SourceId : string.Empty,
                    (target, value) => target.SourceId = value, "InputFormalStepObservationAvailable")
            };
            for (int i = 1; i < header.Length; i++)
                columns.Add(Project(header[i]));
            columns.Add(InputColumn.Create("InputFormalStepClipBindingIndex", Codecs.Int32, Unit.Count,
                (in InputSource source) => source.Observation.Available ? source.Origin.ClipBindingIndex : -1,
                (target, value) => target.ClipBindingIndex = value, "InputFormalStepObservationAvailable"));
            columns.Add(InputColumn.Create("InputFormalStepSourceCycle", Codecs.Int32, Unit.Count,
                (in InputSource source) => source.Observation.Available ? source.Origin.Cycle : 0,
                (target, value) => target.SourceCycle = value, "InputFormalStepObservationAvailable"));
            columns.Add(InputColumn.Create("InputFormalStepContributionContinuityIdentity", Codecs.UInt64, Unit.Identity,
                (in InputSource source) => source.Observation.Available ? source.Origin.ContributionContinuityIdentity : 0UL,
                (target, value) => target.ContributionContinuityIdentity = value, "InputFormalStepObservationAvailable"));
            columns.Add(InputColumn.Create("InputFormalStepCompletionIdentity", Codecs.UInt64, Unit.Identity,
                (in InputSource source) => source.Observation.Available ? source.Origin.CompletionIdentity : 0UL,
                (target, value) => target.CompletionIdentity = value, "InputFormalStepObservationAvailable"));
            foreach (Column column in CreateMotion("InputFormal"))
                columns.Add(Project(column));
            return new CharacterFootCsvGroup<InputSource, CharacterFootFormalInputSample>(
                "FormalInput", () => new CharacterFootFormalInputSample(), columns.ToArray());
        }

        static InputColumn Project(Column column) =>
            column.Project<InputSource, CharacterFootFormalInputSample>(
                (in InputSource source) => source.Observation, target => target);

        static Column[] CreateHeader(string prefix) => new[]
        {
            Column.Create(prefix + "StepObservationAvailable", Codecs.Boolean, Unit.None,
                (in Source source) => source.Available, (target, value) => target.Available = value),
            Column.Create(prefix + "StepSourceIdentity", Codecs.Text, Unit.Identity,
                (in Source source) => source.Available ? source.SourceIdentity : string.Empty,
                (target, value) => target.SourceIdentity = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "StepSourceWeight", Codecs.Float32, Unit.Unitless,
                (in Source source) => source.Available ? source.Weight : 0f,
                (target, value) => target.SourceWeight = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "StepSourceNormalizedTime", Codecs.Float32, Unit.Unitless,
                (in Source source) => source.Available ? source.NormalizedTime : 0f,
                (target, value) => target.NormalizedTime = value, prefix + "StepObservationAvailable")
        };

        static Column[] CreateMotion(string prefix) => new[]
        {
            Column.Create(prefix + "StepTimeSeconds", Codecs.Float32, Unit.Seconds,
                (in Source source) => source.Available ? source.Sample.TimeToLandingSeconds : 0f,
                (target, value) => target.TimeToLandingSeconds = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "StepDistance", Codecs.Float32, Unit.Metres,
                (in Source source) => source.Available ? source.Sample.Distance : 0f,
                (target, value) => target.Distance = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "FootHeight", Codecs.Float32, Unit.Metres,
                (in Source source) => source.Available ? source.Sample.FootHeight : 0f,
                (target, value) => target.FootHeight = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "ToeHeight", Codecs.Float32, Unit.Metres,
                (in Source source) => source.Available ? source.Sample.ToeHeight : 0f,
                (target, value) => target.ToeHeight = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "ToeSpeed", Codecs.Float32, Unit.MetresPerSecond,
                (in Source source) => source.Available ? source.Sample.ToeSpeed : 0f,
                (target, value) => target.ToeSpeed = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "PositionError", Codecs.Float32, Unit.Metres,
                (in Source source) => source.Available ? source.Sample.PositionError : 0f,
                (target, value) => target.PositionError = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "RotationError", Codecs.Float32, Unit.Degrees,
                (in Source source) => source.Available ? source.Sample.RotationError : 0f,
                (target, value) => target.RotationError = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "Contact", Codecs.Float32, Unit.Unitless,
                (in Source source) => source.Available ? source.Sample.Contact : 0f,
                (target, value) => target.Contact = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "LockMode", Codecs.Text, Unit.Category,
                (in Source source) => source.Available ? source.Sample.LockMode.ToString() : string.Empty,
                (target, value) => target.LockMode = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "LockWeight", Codecs.Float32, Unit.Unitless,
                (in Source source) => source.Available ? source.Sample.LockWeight : 0f,
                (target, value) => target.LockWeight = value, prefix + "StepObservationAvailable"),
            Column.Create(prefix + "Support", Codecs.Float32, Unit.Unitless,
                (in Source source) => source.Available ? source.Sample.Support : 0f,
                (target, value) => target.Support = value, prefix + "StepObservationAvailable")
        };
    }
}
