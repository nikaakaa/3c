using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootStepTimeSelectionOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string FormalAvailable = "formal-available";
        const string SourceIdentity = "formal-source-identity";
        const string SourceCycle = "formal-source-cycle";
        const string Continuity = "formal-continuity";
        const string NormalizedTime = "formal-normalized-time";
        const string FormalTime = "formal-time-to-landing";
        const string FormalEvent = "formal-landing-event";
        const string SelectedSource = "selected-source";
        const string SelectedEvent = "selected-event";
        const string SelectedTime = "selected-time-to-landing";
        const string MaximumPrediction = "maximum-prediction-time";
        const string LastLandingEvent = "last-landing-event";
        const string DifferenceThreshold = "difference-threshold-seconds";
        const string FormalNextLandingSource = "formal-next-landing-source";
        const string RepresentativeLimit = "representative-limit";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/step-time-selection-observations",
                "timing",
                Slots(),
                new[]
                {
                    new DiagnosticOperatorParameter(
                        DifferenceThreshold,
                        DiagnosticOperatorParameterKind.Number,
                        true,
                        0d),
                    new DiagnosticOperatorParameter(
                        FormalNextLandingSource,
                        DiagnosticOperatorParameterKind.Integer,
                        true),
                    new DiagnosticOperatorParameter(
                        RepresentativeLimit,
                        DiagnosticOperatorParameterKind.Integer,
                        true,
                        1d)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double threshold = CharacterFootDiagnosticOperatorSupport
                .RequireNumber(context, DifferenceThreshold);
            uint formalNextLanding = CharacterFootDiagnosticOperatorSupport
                .RequireUInt32(context, FormalNextLandingSource);
            int representativeLimit = checked((int)
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    RepresentativeLimit));
            var inputs = new Inputs(context);
            var previous = new Dictionary<string, Observation>(
                StringComparer.Ordinal);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var observations = new List<Observation>();
            DiagnosticDatasetCursor cursor = inputs.FrameValue.Dataset.Main
                .CreateCursor();
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                string dimension = row.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(row))
                {
                    previous.Remove(dimension);
                    continue;
                }
                if (!TryRead(row, inputs, missing, out Observation current))
                {
                    previous.Remove(dimension);
                    continue;
                }
                if (previous.TryGetValue(dimension, out Observation prior) &&
                    prior.Frame + 1 == current.Frame &&
                    prior.Reset == current.Reset)
                {
                    current.NormalizedWrapped =
                        current.SourceIdentity == prior.SourceIdentity &&
                        current.SourceCycle > prior.SourceCycle &&
                        current.NormalizedTime < prior.NormalizedTime;
                    current.SourceChanged =
                        current.SourceIdentity != prior.SourceIdentity ||
                        current.Continuity != prior.Continuity;
                    current.EventChanged =
                        current.SelectedEvent != prior.SelectedEvent;
                }
                if (current.SelectedSource == formalNextLanding &&
                    current.FormalAvailable)
                {
                    current.TimeDifference = Math.Abs(
                        current.FormalTime - current.SelectedTime);
                    current.EventMismatch =
                        current.SelectedEvent != current.FormalEvent;
                    current.SelectionOutOfRange =
                        current.SelectedTime > current.MaximumPrediction;
                    current.ReusesLastEvent =
                        current.SelectedEvent != 0 &&
                        current.SelectedEvent == current.LastLandingEvent;
                }
                observations.Add(current);
                previous[dimension] = current;
            }
            if (missing.Count != 0)
            {
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        missing));
            }
            if (observations.Count == 0)
            {
                return DiagnosticOperatorResult.NotApplicable(
                    "No current formal Foot Step selection observation was captured.");
            }
            var findings = new List<DiagnosticFinding>();
            for (int i = 0;
                 i < observations.Count && findings.Count < representativeLimit;
                 i++)
            {
                Observation value = observations[i];
                bool timeMismatch = value.TimeDifference.HasValue &&
                    value.TimeDifference.Value > threshold;
                if (!value.NormalizedWrapped &&
                    !value.SourceChanged &&
                    !value.EventChanged &&
                    !value.EventMismatch &&
                    !value.SelectionOutOfRange &&
                    !value.ReusesLastEvent &&
                    !timeMismatch)
                {
                    continue;
                }
                var reasons = new List<string>();
                if (value.NormalizedWrapped)
                    reasons.Add("normalized-time-wrap");
                if (value.SourceChanged)
                    reasons.Add("source-change");
                if (value.EventChanged)
                    reasons.Add("selected-event-change");
                if (value.EventMismatch)
                    reasons.Add("formal-selected-event-mismatch");
                if (value.SelectionOutOfRange)
                    reasons.Add("selected-time-out-of-range");
                if (value.ReusesLastEvent)
                    reasons.Add("selected-reuses-last-landing-event");
                if (timeMismatch)
                    reasons.Add("formal-selected-time-difference");
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        value.Sequence),
                    DiagnosticSeverity.Information,
                    value.Dimension,
                    value.Sequence,
                    value.Sequence,
                    $"Current Foot Step selection observation: {string.Join(", ", reasons)}.",
                    new[]
                    {
                        CharacterFootDiagnosticOperatorSupport.Evidence(
                            "formal-source",
                            inputs.Source,
                            value.Dimension,
                            value.Sequence,
                            value.Sequence,
                            value.SourceIdentity),
                        CharacterFootDiagnosticOperatorSupport.Evidence(
                            "selected-source",
                            inputs.SelectionSource,
                            value.Dimension,
                            value.Sequence,
                            value.Sequence,
                            value.SelectedSource.ToString(
                                CultureInfo.InvariantCulture)),
                        CharacterFootDiagnosticOperatorSupport.Evidence(
                            "selected-event",
                            inputs.SelectionEvent,
                            value.Dimension,
                            value.Sequence,
                            value.Sequence,
                            value.SelectedEvent.ToString(
                                CultureInfo.InvariantCulture)),
                        CharacterFootDiagnosticOperatorSupport.Evidence(
                            "formal-selected-time-difference",
                            inputs.SelectionTime,
                            value.Dimension,
                            value.Sequence,
                            value.Sequence,
                            value.TimeDifference?.ToString(
                                "R",
                                CultureInfo.InvariantCulture) ??
                            "not-applicable")
                    },
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        inputs.FrameValue,
                        value.Frame,
                        value.Frame)));
            }
            string summary =
                $"{observations.Count} current formal Step observations; " +
                $"selectedNext={observations.FindAll(value => value.SelectedSource == formalNextLanding).Count}, " +
                $"wrap={observations.FindAll(value => value.NormalizedWrapped).Count}, " +
                $"sourceChange={observations.FindAll(value => value.SourceChanged).Count}, " +
                $"eventChange={observations.FindAll(value => value.EventChanged).Count}, " +
                $"eventMismatch={observations.FindAll(value => value.EventMismatch).Count}, " +
                $"outOfRange={observations.FindAll(value => value.SelectionOutOfRange).Count}, " +
                $"reusedLast={observations.FindAll(value => value.ReusesLastEvent).Count}, " +
                $"timeDifference={observations.FindAll(value => value.TimeDifference.HasValue && value.TimeDifference.Value > threshold).Count}.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static bool TryRead(
            in DiagnosticDatasetRow row,
            Inputs inputs,
            ISet<string> missing,
            out Observation value)
        {
            bool complete = true;
            for (int i = 0; i < inputs.All.Count; i++)
            {
                if (row.IsAvailable(inputs.All[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(
                    inputs.All[i]));
                complete = false;
            }
            if (!complete)
            {
                value = null;
                return false;
            }
            value = new Observation
            {
                Dimension = row.SampleKey.DimensionId,
                Sequence = row.SampleKey.Sequence,
                Frame = row.GetUInt64(inputs.FrameValue.Handle),
                Reset = row.GetUInt64(inputs.ResetValue.Handle),
                FormalAvailable = row.GetBoolean(inputs.Formal.Handle),
                SourceIdentity = row.GetIdentity(inputs.Source.Handle),
                SourceCycle = row.GetInt32(inputs.Cycle.Handle),
                Continuity = row.GetUInt64(inputs.ContinuityValue.Handle),
                NormalizedTime = row.GetFloat32(inputs.Normalized.Handle),
                FormalTime = row.GetFloat32(inputs.FormalTimeValue.Handle),
                FormalEvent = row.GetUInt64(inputs.FormalEventValue.Handle),
                SelectedSource = row.GetUInt32(inputs.SelectionSource.Handle),
                SelectedEvent = row.GetUInt64(inputs.SelectionEvent.Handle),
                SelectedTime = row.GetFloat32(inputs.SelectionTime.Handle),
                MaximumPrediction = row.GetFloat32(inputs.Maximum.Handle),
                LastLandingEvent = row.GetUInt64(inputs.LastLanding.Handle)
            };
            return true;
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            Slot(Frame, DiagnosticValueKind.UInt64),
            Slot(Reset, DiagnosticValueKind.UInt64),
            Slot(FormalAvailable, DiagnosticValueKind.Boolean),
            Slot(SourceIdentity, DiagnosticValueKind.Identity),
            Slot(SourceCycle, DiagnosticValueKind.Int32),
            Slot(Continuity, DiagnosticValueKind.UInt64),
            Slot(NormalizedTime, DiagnosticValueKind.Float32),
            Slot(FormalTime, DiagnosticValueKind.Float32),
            Slot(FormalEvent, DiagnosticValueKind.UInt64),
            Slot(SelectedSource, DiagnosticValueKind.UInt32),
            Slot(SelectedEvent, DiagnosticValueKind.UInt64),
            Slot(SelectedTime, DiagnosticValueKind.Float32),
            Slot(MaximumPrediction, DiagnosticValueKind.Float32),
            Slot(LastLandingEvent, DiagnosticValueKind.UInt64)
        };

        static DiagnosticOperatorInputSlot Slot(
            string id,
            DiagnosticValueKind kind) =>
            new DiagnosticOperatorInputSlot(
                id,
                kind,
                DiagnosticDatasetCardinality.Main,
                true);

        sealed class Inputs
        {
            internal Inputs(DiagnosticOperatorExecutionContext context)
            {
                FrameValue = context.Input(Frame);
                ResetValue = context.Input(Reset);
                Formal = context.Input(FormalAvailable);
                Source = context.Input(SourceIdentity);
                Cycle = context.Input(SourceCycle);
                ContinuityValue = context.Input(Continuity);
                Normalized = context.Input(NormalizedTime);
                FormalTimeValue = context.Input(FormalTime);
                FormalEventValue = context.Input(FormalEvent);
                SelectionSource = context.Input(SelectedSource);
                SelectionEvent = context.Input(SelectedEvent);
                SelectionTime = context.Input(SelectedTime);
                Maximum = context.Input(MaximumPrediction);
                LastLanding = context.Input(LastLandingEvent);
                All = new[]
                {
                    FrameValue,
                    ResetValue,
                    Formal,
                    Source,
                    Cycle,
                    ContinuityValue,
                    Normalized,
                    FormalTimeValue,
                    FormalEventValue,
                    SelectionSource,
                    SelectionEvent,
                    SelectionTime,
                    Maximum,
                    LastLanding
                };
            }

            internal DiagnosticBoundInput FrameValue { get; }
            internal DiagnosticBoundInput ResetValue { get; }
            internal DiagnosticBoundInput Formal { get; }
            internal DiagnosticBoundInput Source { get; }
            internal DiagnosticBoundInput Cycle { get; }
            internal DiagnosticBoundInput ContinuityValue { get; }
            internal DiagnosticBoundInput Normalized { get; }
            internal DiagnosticBoundInput FormalTimeValue { get; }
            internal DiagnosticBoundInput FormalEventValue { get; }
            internal DiagnosticBoundInput SelectionSource { get; }
            internal DiagnosticBoundInput SelectionEvent { get; }
            internal DiagnosticBoundInput SelectionTime { get; }
            internal DiagnosticBoundInput Maximum { get; }
            internal DiagnosticBoundInput LastLanding { get; }
            internal IReadOnlyList<DiagnosticBoundInput> All { get; }
        }

        sealed class Observation
        {
            internal string Dimension;
            internal ulong Sequence;
            internal ulong Frame;
            internal ulong Reset;
            internal bool FormalAvailable;
            internal string SourceIdentity;
            internal int SourceCycle;
            internal ulong Continuity;
            internal double NormalizedTime;
            internal double FormalTime;
            internal ulong FormalEvent;
            internal uint SelectedSource;
            internal ulong SelectedEvent;
            internal double SelectedTime;
            internal double MaximumPrediction;
            internal ulong LastLandingEvent;
            internal double? TimeDifference;
            internal bool NormalizedWrapped;
            internal bool SourceChanged;
            internal bool EventChanged;
            internal bool EventMismatch;
            internal bool SelectionOutOfRange;
            internal bool ReusesLastEvent;
        }
    }
}
