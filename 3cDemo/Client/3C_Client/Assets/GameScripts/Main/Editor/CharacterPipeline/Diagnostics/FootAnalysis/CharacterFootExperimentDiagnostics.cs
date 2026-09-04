using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootPelvisResponseChainOperator :
        IDiagnosticAnalysisOperator
    {
        const string ResidualThreshold = "response-residual-threshold-meters";
        const string OutputStepThreshold = "response-output-step-threshold-meters";
        const string PhysicalStepThreshold = "physical-pelvis-step-threshold-meters";
        const string HistoryThreshold = "response-history-threshold-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/pelvis-response-chain",
                "pelvis",
                ResponseInputs.Slots(),
                new[]
                {
                    Number(ResidualThreshold),
                    Number(OutputStepThreshold),
                    Number(PhysicalStepThreshold),
                    Number(HistoryThreshold)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double residualThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                ResidualThreshold);
            double outputStepThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                OutputStepThreshold);
            double physicalStepThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                PhysicalStepThreshold);
            double historyThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                HistoryThreshold);
            var inputs = new ResponseInputs(context);
            var rowsByDimension = new Dictionary<string, List<DiagnosticDatasetRow>>(
                StringComparer.Ordinal);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = inputs.Frame.Dataset.Main.CreateCursor();
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                string dimension = row.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(row))
                {
                    continue;
                }
                if (!Available(
                        row,
                        inputs.Frame,
                        inputs.Reset,
                        inputs.HeightAvailable,
                        inputs.ResponseIsEvaluated,
                        inputs.PhysicalAvailable,
                        inputs.PhysicalPelvisIsAvailable))
                {
                    AddMissing(
                        missing,
                        row,
                        inputs.Frame,
                        inputs.Reset,
                        inputs.HeightAvailable,
                        inputs.ResponseIsEvaluated,
                        inputs.PhysicalAvailable,
                        inputs.PhysicalPelvisIsAvailable);
                    continue;
                }
                if (!row.GetBoolean(inputs.HeightAvailable.Handle) ||
                    !row.GetBoolean(inputs.ResponseIsEvaluated.Handle) ||
                    !row.GetBoolean(inputs.PhysicalAvailable.Handle) ||
                    !row.GetBoolean(inputs.PhysicalPelvisIsAvailable.Handle))
                {
                    continue;
                }
                if (!Available(
                        row,
                        inputs.PhysicalPelvis,
                        inputs.PreviousOutput,
                        inputs.Target,
                        inputs.Output,
                        inputs.HeightOffset))
                {
                    AddMissing(
                        missing,
                        row,
                        inputs.PhysicalPelvis,
                        inputs.PreviousOutput,
                        inputs.Target,
                        inputs.Output,
                        inputs.HeightOffset);
                    continue;
                }
                if (!rowsByDimension.TryGetValue(
                        dimension,
                        out List<DiagnosticDatasetRow> rows))
                {
                    rows = new List<DiagnosticDatasetRow>();
                    rowsByDimension.Add(dimension, rows);
                }
                rows.Add(row);
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));

            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            double maximumResidual = 0d;
            double maximumOutputStep = 0d;
            double maximumPhysicalStep = 0d;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rowsByDimension)
            {
                pair.Value.Sort((left, right) =>
                    left.SampleKey.Sequence.CompareTo(right.SampleKey.Sequence));
                for (int i = 1; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow previous = pair.Value[i - 1];
                    DiagnosticDatasetRow current = pair.Value[i];
                    if (!Continuous(previous, current, inputs))
                        continue;
                    eligible++;
                    double target = current.GetFloat32(inputs.Target.Handle);
                    double output = current.GetFloat32(inputs.Output.Handle);
                    double previousOutput = current.GetFloat32(inputs.PreviousOutput.Handle);
                    double previousFrameOutput = previous.GetFloat32(inputs.Output.Handle);
                    double residual = Math.Abs(target - output);
                    double outputStep = Math.Abs(output - previousFrameOutput);
                    double physicalStep = CharacterFootDiagnosticOperatorSupport.Distance(
                        current.GetVector3(inputs.PhysicalPelvis.Handle),
                        previous.GetVector3(inputs.PhysicalPelvis.Handle));
                    double historyError = Math.Abs(previousOutput - previousFrameOutput);
                    double targetStep = Math.Abs(
                        current.GetFloat32(inputs.Target.Handle) -
                        previous.GetFloat32(inputs.Target.Handle));
                    maximumResidual = Math.Max(maximumResidual, residual);
                    maximumOutputStep = Math.Max(maximumOutputStep, outputStep);
                    maximumPhysicalStep = Math.Max(maximumPhysicalStep, physicalStep);
                    bool residualExceeded = residual > residualThreshold;
                    bool outputStepExceeded = outputStep > outputStepThreshold;
                    bool physicalStepExceeded = physicalStep > physicalStepThreshold;
                    bool historyMismatch = historyError > historyThreshold;
                    if (!residualExceeded &&
                        !outputStepExceeded &&
                        !physicalStepExceeded &&
                        !historyMismatch)
                    {
                        continue;
                    }
                    var causes = new List<string>();
                    if (residualExceeded)
                        causes.Add("response-residual");
                    if (outputStepExceeded)
                        causes.Add("response-output-step");
                    if (physicalStepExceeded)
                        causes.Add("physical-pelvis-step");
                    if (historyMismatch)
                        causes.Add("response-history-mismatch");
                    ulong sequenceStart = previous.SampleKey.Sequence;
                    ulong sequenceEnd = current.SampleKey.Sequence;
                    string dimension = pair.Key;
                    var evidence = new[]
                    {
                        Evidence("requested-height-offset-meters", inputs.HeightOffset, dimension, sequenceStart, sequenceEnd, current.GetFloat32(inputs.HeightOffset.Handle)),
                        Evidence("response-target-meters", inputs.Target, dimension, sequenceStart, sequenceEnd, target),
                        Evidence("response-output-meters", inputs.Output, dimension, sequenceStart, sequenceEnd, output),
                        Evidence("response-residual-meters", inputs.Output, dimension, sequenceStart, sequenceEnd, residual),
                        Evidence("response-output-step-meters", inputs.Output, dimension, sequenceStart, sequenceEnd, outputStep),
                        Evidence("physical-pelvis-step-meters", inputs.PhysicalPelvis, dimension, sequenceStart, sequenceEnd, physicalStep),
                        Evidence("response-history-error-meters", inputs.PreviousOutput, dimension, sequenceStart, sequenceEnd, historyError),
                        Evidence("response-target-step-meters", inputs.Target, dimension, sequenceStart, sequenceEnd, targetStep),
                        CharacterFootDiagnosticOperatorSupport.Evidence(
                            "response-chain-cause",
                            inputs.Output,
                            dimension,
                            sequenceStart,
                            sequenceEnd,
                            string.Join("+", causes))
                    };
                    findings.Add(new DiagnosticFinding(
                        CharacterFootDiagnosticOperatorSupport.FindingId(
                            context.RuleId,
                            sequenceEnd),
                        DiagnosticSeverity.Warning,
                        dimension,
                        sequenceStart,
                        sequenceEnd,
                        $"Pelvis response chain exceeded the configured observation limit; cause={string.Join("+", causes)}.",
                        evidence,
                        CharacterFootDiagnosticOperatorSupport.FrameRange(
                            inputs.Frame,
                            previous.GetUInt64(inputs.Frame.Handle),
                            current.GetUInt64(inputs.Frame.Handle))));
                }
            }
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No continuous Pelvis response frame pair with complete height, response and physical evidence was observed.");
            findings.Sort((left, right) =>
            {
                int sequence = left.SequenceStart.CompareTo(right.SequenceStart);
                return sequence != 0
                    ? sequence
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            string summary =
                $"{findings.Count} of {eligible} Pelvis response frame pairs exceeded the response-chain limits; maxResidual={Format(maximumResidual)} m, maxOutputStep={Format(maximumOutputStep)} m, maxPhysicalStep={Format(maximumPhysicalStep)} m.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static DiagnosticOperatorParameter Number(string id) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d);

        static DiagnosticEvidence Evidence(
            string id,
            in DiagnosticBoundInput input,
            string dimension,
            ulong sequenceStart,
            ulong sequenceEnd,
            double value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                dimension,
                sequenceStart,
                sequenceEnd,
                Format(value));

        static string Format(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        static bool Continuous(
            in DiagnosticDatasetRow previous,
            in DiagnosticDatasetRow current,
            ResponseInputs inputs) =>
            current.GetUInt64(inputs.Frame.Handle) ==
                previous.GetUInt64(inputs.Frame.Handle) + 1 &&
            current.GetUInt64(inputs.Reset.Handle) ==
                previous.GetUInt64(inputs.Reset.Handle);

        static bool Available(
            in DiagnosticDatasetRow row,
            params DiagnosticBoundInput[] inputs)
        {
            for (int i = 0; i < inputs.Length; i++)
                if (!row.IsAvailable(inputs[i].Handle))
                    return false;
            return true;
        }

        static void AddMissing(
            ISet<string> missing,
            in DiagnosticDatasetRow row,
            params DiagnosticBoundInput[] inputs)
        {
            for (int i = 0; i < inputs.Length; i++)
                if (!row.IsAvailable(inputs[i].Handle))
                    missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
        }

        sealed class ResponseInputs
        {
            internal ResponseInputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input("frame-sequence");
                Reset = context.Input("reset-sequence");
                PhysicalAvailable = context.Input("physical-write-available");
                PhysicalPelvisIsAvailable = context.Input("physical-pelvis-available");
                PhysicalPelvis = context.Input("physical-pelvis-position");
                ResponseIsEvaluated = context.Input("response-evaluated");
                PreviousOutput = context.Input("response-previous-output");
                Target = context.Input("response-target");
                Output = context.Input("response-output");
                HeightAvailable = context.Input("height-target-available");
                HeightOffset = context.Input("requested-height-offset");
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput PhysicalAvailable { get; }
            internal DiagnosticBoundInput PhysicalPelvisIsAvailable { get; }
            internal DiagnosticBoundInput PhysicalPelvis { get; }
            internal DiagnosticBoundInput ResponseIsEvaluated { get; }
            internal DiagnosticBoundInput PreviousOutput { get; }
            internal DiagnosticBoundInput Target { get; }
            internal DiagnosticBoundInput Output { get; }
            internal DiagnosticBoundInput HeightAvailable { get; }
            internal DiagnosticBoundInput HeightOffset { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot("frame-sequence", DiagnosticValueKind.UInt64),
                Slot("reset-sequence", DiagnosticValueKind.UInt64),
                Slot("physical-write-available", DiagnosticValueKind.Boolean),
                Slot("physical-pelvis-available", DiagnosticValueKind.Boolean),
                Slot("physical-pelvis-position", DiagnosticValueKind.Vector3),
                Slot("response-evaluated", DiagnosticValueKind.Boolean),
                Slot("response-previous-output", DiagnosticValueKind.Float32),
                Slot("response-target", DiagnosticValueKind.Float32),
                Slot("response-output", DiagnosticValueKind.Float32),
                Slot("height-target-available", DiagnosticValueKind.Boolean),
                Slot("requested-height-offset", DiagnosticValueKind.Float32)
            };

            static DiagnosticOperatorInputSlot Slot(
                string id,
                DiagnosticValueKind kind) =>
                CharacterFootLandingPolicySupport.Slot(id, kind);
        }
    }

    internal sealed class CharacterFootContactLifecycleChatterOperator :
        IDiagnosticAnalysisOperator
    {
        const string LandingState = "landing-state";
        const string LockedState = "locked-state";
        const string ReleasingState = "releasing-state";
        const string ChatterWindowFrames = "chatter-window-frames";
        const string NoContactEdge = "no-contact-edge";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/contact-lifecycle-chatter",
                "contact",
                Inputs.Slots(),
                new[]
                {
                    Integer(LandingState),
                    Integer(LockedState),
                    Integer(ReleasingState),
                    Integer(ChatterWindowFrames, 6d),
                    Integer(NoContactEdge)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            uint landingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LandingState);
            uint lockedState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LockedState);
            uint releasingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                ReleasingState);
            uint chatterWindow = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                ChatterWindowFrames);
            uint noContactEdge = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                NoContactEdge);
            if (chatterWindow == 0)
                throw new InvalidOperationException(
                    "Contact chatter window must be positive.");
            var inputs = new Inputs(context);
            inputs.LandingState = landingState;
            inputs.LockedState = lockedState;
            inputs.ReleasingState = releasingState;
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rowsByDimension =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    inputs.Required,
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));

            var findings = new List<DiagnosticFinding>();
            var lastReportedSequence = new Dictionary<string, ulong>(StringComparer.Ordinal);
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rowsByDimension)
            {
                for (int i = 2; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow before = pair.Value[i - 2];
                    DiagnosticDatasetRow middle = pair.Value[i - 1];
                    DiagnosticDatasetRow after = pair.Value[i];
                    if (!CharacterFootLandingPolicySupport.Continuous(
                            before,
                            middle,
                            inputs.Frame,
                            inputs.Reset) ||
                        !CharacterFootLandingPolicySupport.Continuous(
                            middle,
                            after,
                            inputs.Frame,
                            inputs.Reset))
                    {
                        continue;
                    }
                    if (!Relevant(before, inputs, noContactEdge) &&
                        !Relevant(middle, inputs, noContactEdge) &&
                        !Relevant(after, inputs, noContactEdge))
                    {
                        continue;
                    }
                    eligible++;
                    uint beforeState = before.GetUInt32(inputs.State.Handle);
                    uint middleState = middle.GetUInt32(inputs.State.Handle);
                    uint afterState = after.GetUInt32(inputs.State.Handle);
                    bool stateBounce = beforeState == afterState &&
                        beforeState != middleState;
                    bool lockBounce = before.GetBoolean(inputs.LockRequested.Handle) ==
                                      after.GetBoolean(inputs.LockRequested.Handle) &&
                        before.GetBoolean(inputs.LockRequested.Handle) !=
                        middle.GetBoolean(inputs.LockRequested.Handle);
                    ulong frameSpan = after.GetUInt64(inputs.Frame.Handle) -
                        before.GetUInt64(inputs.Frame.Handle);
                    if ((!stateBounce && !lockBounce) || frameSpan > chatterWindow)
                        continue;
                    if (lastReportedSequence.TryGetValue(
                            pair.Key,
                            out ulong lastSequence) &&
                        after.SampleKey.Sequence <= lastSequence + 1)
                    {
                        continue;
                    }
                    lastReportedSequence[pair.Key] = after.SampleKey.Sequence;
                    string cause = stateBounce && lockBounce
                        ? "state-and-lock-request-bounce"
                        : stateBounce
                            ? "state-bounce"
                            : "lock-request-bounce";
                    ulong sequenceStart = before.SampleKey.Sequence;
                    ulong sequenceEnd = after.SampleKey.Sequence;
                    var evidence = new[]
                    {
                        StateEvidence("state-before", inputs.State, pair.Key, sequenceStart, sequenceEnd, beforeState),
                        StateEvidence("state-middle", inputs.State, pair.Key, sequenceStart, sequenceEnd, middleState),
                        StateEvidence("state-after", inputs.State, pair.Key, sequenceStart, sequenceEnd, afterState),
                        BoolEvidence("lock-requested-before", inputs.LockRequested, pair.Key, sequenceStart, sequenceEnd, before.GetBoolean(inputs.LockRequested.Handle)),
                        BoolEvidence("lock-requested-middle", inputs.LockRequested, pair.Key, sequenceStart, sequenceEnd, middle.GetBoolean(inputs.LockRequested.Handle)),
                        BoolEvidence("lock-requested-after", inputs.LockRequested, pair.Key, sequenceStart, sequenceEnd, after.GetBoolean(inputs.LockRequested.Handle)),
                        StateEvidence("landing-event-before", inputs.LandingEvent, pair.Key, sequenceStart, sequenceEnd, before.GetUInt64(inputs.LandingEvent.Handle)),
                        StateEvidence("landing-event-after", inputs.LandingEvent, pair.Key, sequenceStart, sequenceEnd, after.GetUInt64(inputs.LandingEvent.Handle)),
                        CharacterFootDiagnosticOperatorSupport.Evidence(
                            "chatter-cause",
                            inputs.State,
                            pair.Key,
                            sequenceStart,
                            sequenceEnd,
                            cause)
                    };
                    findings.Add(new DiagnosticFinding(
                        CharacterFootDiagnosticOperatorSupport.FindingId(
                            context.RuleId,
                            sequenceEnd),
                        DiagnosticSeverity.Warning,
                        pair.Key,
                        sequenceStart,
                        sequenceEnd,
                        $"Contact lifecycle changed and returned within {frameSpan.ToString(CultureInfo.InvariantCulture)} frames; cause={cause}.",
                        evidence,
                        CharacterFootDiagnosticOperatorSupport.FrameRange(
                            inputs.Frame,
                            before.GetUInt64(inputs.Frame.Handle),
                            after.GetUInt64(inputs.Frame.Handle))));
                }
            }
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No continuous Contact lifecycle window was observed.");
            findings.Sort((left, right) =>
            {
                int sequence = left.SequenceStart.CompareTo(right.SequenceStart);
                return sequence != 0
                    ? sequence
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            string summary =
                $"{findings.Count} of {eligible} Contact lifecycle windows showed a rapid state or lock-request bounce within {chatterWindow.ToString(CultureInfo.InvariantCulture)} frames.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static bool Relevant(
            in DiagnosticDatasetRow row,
            Inputs inputs,
            uint noContactEdge) =>
            row.GetUInt32(inputs.State.Handle) ==
                inputs.LandingState ||
            row.GetUInt32(inputs.State.Handle) ==
                inputs.LockedState ||
            row.GetUInt32(inputs.State.Handle) ==
                inputs.ReleasingState ||
            row.GetUInt64(inputs.LandingEvent.Handle) != 0 ||
            row.GetBoolean(inputs.LockRequested.Handle) ||
            row.GetBoolean(inputs.AnchorAvailable.Handle) ||
            row.GetUInt32(inputs.ContactEdge.Handle) != noContactEdge;

        static DiagnosticOperatorParameter Integer(
            string id,
            double defaultValue = 0d) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Integer,
                true,
                defaultValue);

        static DiagnosticEvidence StateEvidence(
            string id,
            in DiagnosticBoundInput input,
            string dimension,
            ulong sequenceStart,
            ulong sequenceEnd,
            ulong value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                dimension,
                sequenceStart,
                sequenceEnd,
                value.ToString(CultureInfo.InvariantCulture));

        static DiagnosticEvidence StateEvidence(
            string id,
            in DiagnosticBoundInput input,
            string dimension,
            ulong sequenceStart,
            ulong sequenceEnd,
            uint value) => StateEvidence(
                id,
                input,
                dimension,
                sequenceStart,
                sequenceEnd,
                (ulong)value);

        static DiagnosticEvidence BoolEvidence(
            string id,
            in DiagnosticBoundInput input,
            string dimension,
            ulong sequenceStart,
            ulong sequenceEnd,
            bool value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                dimension,
                sequenceStart,
                sequenceEnd,
                value ? "true" : "false");

        sealed class Inputs
        {
            internal const string FrameSequence = "frame-sequence";
            internal const string ResetSequence = "reset-sequence";
            internal const string StateId = "constraint-state";
            internal const string LandingEventId = "landing-event-identity";
            internal const string ContactEdgeId = "contact-edge";
            internal const string LockRequestedId = "current-lock-requested";
            internal const string LockEventId = "current-lock-request-event-identity";
            internal const string LockModeId = "current-lock-request-mode";
            internal const string LockWeightId = "current-lock-request-weight";
            internal const string AnchorAvailableId = "anchor-available";

            internal Inputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input(FrameSequence);
                Reset = context.Input(ResetSequence);
                State = context.Input(StateId);
                LandingEvent = context.Input(LandingEventId);
                ContactEdge = context.Input(ContactEdgeId);
                LockRequested = context.Input(LockRequestedId);
                LockEvent = context.Input(LockEventId);
                LockMode = context.Input(LockModeId);
                LockWeight = context.Input(LockWeightId);
                AnchorAvailable = context.Input(AnchorAvailableId);
                LandingState = 0;
                LockedState = 0;
                ReleasingState = 0;
                Required = new[]
                {
                    Frame,
                    Reset,
                    State,
                    LandingEvent,
                    ContactEdge,
                    LockRequested,
                    LockEvent,
                    LockMode,
                    LockWeight,
                    AnchorAvailable
                };
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput State { get; }
            internal DiagnosticBoundInput LandingEvent { get; }
            internal DiagnosticBoundInput ContactEdge { get; }
            internal DiagnosticBoundInput LockRequested { get; }
            internal DiagnosticBoundInput LockEvent { get; }
            internal DiagnosticBoundInput LockMode { get; }
            internal DiagnosticBoundInput LockWeight { get; }
            internal DiagnosticBoundInput AnchorAvailable { get; }
            internal uint LandingState { get; set; }
            internal uint LockedState { get; set; }
            internal uint ReleasingState { get; set; }
            internal IReadOnlyList<DiagnosticBoundInput> Required { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot(FrameSequence, DiagnosticValueKind.UInt64),
                Slot(ResetSequence, DiagnosticValueKind.UInt64),
                Slot(StateId, DiagnosticValueKind.UInt32),
                Slot(LandingEventId, DiagnosticValueKind.UInt64),
                Slot(ContactEdgeId, DiagnosticValueKind.UInt32),
                Slot(LockRequestedId, DiagnosticValueKind.Boolean),
                Slot(LockEventId, DiagnosticValueKind.UInt64),
                Slot(LockModeId, DiagnosticValueKind.UInt32),
                Slot(LockWeightId, DiagnosticValueKind.Float32),
                Slot(AnchorAvailableId, DiagnosticValueKind.Boolean)
            };

            static DiagnosticOperatorInputSlot Slot(
                string id,
                DiagnosticValueKind kind) =>
                CharacterFootLandingPolicySupport.Slot(id, kind);
        }
    }
}
