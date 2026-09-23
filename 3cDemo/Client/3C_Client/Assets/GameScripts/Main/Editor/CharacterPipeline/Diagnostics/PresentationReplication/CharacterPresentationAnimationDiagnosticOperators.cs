using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    internal sealed class CharacterPresentationAnimationOutputValidityOperator :
        IDiagnosticAnalysisOperator
    {
        const string Snapshot = "has-snapshot";
        const string PoseGraphId = "pose-graph-id";
        const string PoseGraphRevision = "pose-graph-revision";
        const string InputContractHash = "input-contract-hash";
        const string Completion = "completion-identity";
        const string Availability = "final-availability";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "animation/output-validity",
                "animation",
                new[]
                {
                    Main(Snapshot, DiagnosticValueKind.Boolean),
                    Main(PoseGraphId, DiagnosticValueKind.Identity),
                    Main(PoseGraphRevision, DiagnosticValueKind.Identity),
                    Main(InputContractHash, DiagnosticValueKind.Identity),
                    Main(Completion, DiagnosticValueKind.UInt64),
                    Main(Availability, DiagnosticValueKind.UInt32)
                },
                Array.Empty<DiagnosticOperatorParameter>());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            var required = new[]
            {
                inputs.Snapshot,
                inputs.PoseGraphId,
                inputs.PoseGraphRevision,
                inputs.InputContractHash,
                inputs.Completion,
                inputs.Availability
            };
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            int eligible = 0;
            int failed = 0;
            int findingIndex = 0;
            bool observed = false;
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !context.MatchesFilters(row))
                {
                    continue;
                }
                if (!CharacterPresentationReplicationDiagnosticOperatorSupport
                        .Require(in row, required, missing))
                {
                    continue;
                }
                if (!row.GetBoolean(inputs.Snapshot.Handle))
                {
                    missing.Add("animation-snapshot-unavailable");
                    continue;
                }
                observed = true;
                eligible++;
                bool valid =
                    !string.IsNullOrEmpty(row.GetIdentity(inputs.PoseGraphId.Handle)) &&
                    !string.IsNullOrEmpty(row.GetIdentity(inputs.PoseGraphRevision.Handle)) &&
                    !string.IsNullOrEmpty(row.GetIdentity(inputs.InputContractHash.Handle)) &&
                    row.GetUInt64(inputs.Completion.Handle) != 0 &&
                    row.GetUInt32(inputs.Availability.Handle) == 1;
                if (valid)
                {
                    continue;
                }
                failed++;
                findings.Add(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Finding(
                        context,
                        DiagnosticSeverity.Error,
                        in row,
                        "Committed animation output identity or availability is invalid.",
                        findingIndex++,
                        new[]
                        {
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "pose-graph-id",
                                inputs.PoseGraphId,
                                in row,
                                row.GetIdentity(inputs.PoseGraphId.Handle)),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "input-contract-hash",
                                inputs.InputContractHash,
                                in row,
                                row.GetIdentity(inputs.InputContractHash.Handle)),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "final-availability",
                                inputs.Availability,
                                in row,
                                row.GetUInt32(inputs.Availability.Handle).ToString(CultureInfo.InvariantCulture))
                        }));
            }
            if (missing.Count != 0)
            {
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.MissingSummary(
                        missing));
            }
            if (!observed)
            {
                return DiagnosticOperatorResult.NotApplicable(
                    "No committed animation snapshot was captured.");
            }
            double score = eligible == 0
                ? 0d
                : Math.Max(0d, 1d - (double)failed / eligible);
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{failed.ToString(CultureInfo.InvariantCulture)} of {eligible.ToString(CultureInfo.InvariantCulture)} committed animation frames failed output validity.",
                score,
                Array.Empty<DiagnosticEvidence>(),
                findings);
        }

        static DiagnosticOperatorInputSlot Main(
            string id,
            DiagnosticValueKind kind) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.Main(id, kind);

        sealed class Inputs
        {
            public Inputs(DiagnosticOperatorExecutionContext context)
            {
                Snapshot = context.Input("has-snapshot");
                PoseGraphId = context.Input("pose-graph-id");
                PoseGraphRevision = context.Input("pose-graph-revision");
                InputContractHash = context.Input("input-contract-hash");
                Completion = context.Input("completion-identity");
                Availability = context.Input("final-availability");
            }

            public DiagnosticBoundInput Snapshot { get; }
            public DiagnosticBoundInput PoseGraphId { get; }
            public DiagnosticBoundInput PoseGraphRevision { get; }
            public DiagnosticBoundInput InputContractHash { get; }
            public DiagnosticBoundInput Completion { get; }
            public DiagnosticBoundInput Availability { get; }
        }
    }

    internal sealed class CharacterPresentationAnimationParameterAvailabilityOperator :
        IDiagnosticAnalysisOperator
    {
        const string ParameterId = "parameter-id";
        const string ParameterValue = "parameter-value";
        const string ParameterAvailable = "parameter-available";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "animation/parameter-availability",
                "animation",
                new[]
                {
                    Table(ParameterId, DiagnosticValueKind.Identity),
                    Table(ParameterValue, DiagnosticValueKind.Float32),
                    Table(ParameterAvailable, DiagnosticValueKind.Boolean)
                },
                Array.Empty<DiagnosticOperatorParameter>());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            var required = new[]
            {
                inputs.ParameterId,
                inputs.ParameterValue,
                inputs.ParameterAvailable
            };
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            int eligible = 0;
            int failed = 0;
            int findingIndex = 0;
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId))
                    continue;
                if (!CharacterPresentationReplicationDiagnosticOperatorSupport
                        .Require(in row, required, missing))
                {
                    continue;
                }
                string parameterId = row.GetIdentity(inputs.ParameterId.Handle);
                if (string.IsNullOrEmpty(parameterId))
                {
                    missing.Add("animation-parameter-id");
                    continue;
                }
                eligible++;
                bool duplicate = !identities.Add(
                    row.SampleKey.DimensionId + "|" +
                    row.SampleKey.Sequence.ToString(CultureInfo.InvariantCulture) + "|" +
                    parameterId);
                bool available = row.GetBoolean(inputs.ParameterAvailable.Handle);
                bool finite = !float.IsNaN(row.GetFloat32(inputs.ParameterValue.Handle)) &&
                              !float.IsInfinity(row.GetFloat32(inputs.ParameterValue.Handle));
                if (!duplicate && available && finite)
                    continue;
                failed++;
                string reason = duplicate
                    ? "duplicate-parameter"
                    : !available
                        ? "parameter-unavailable"
                        : "parameter-value-invalid";
                findings.Add(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Finding(
                        context,
                        available && finite
                            ? DiagnosticSeverity.Warning
                            : DiagnosticSeverity.Error,
                        in row,
                        $"Animation parameter '{parameterId}' failed availability contract: {reason}.",
                        findingIndex++,
                        new[]
                        {
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "parameter-id",
                                inputs.ParameterId,
                                in row,
                                parameterId),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "parameter-available",
                                inputs.ParameterAvailable,
                                in row,
                                available.ToString()),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "parameter-value",
                                inputs.ParameterValue,
                                in row,
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                    row.GetFloat32(inputs.ParameterValue.Handle)))
                        }));
            }
            if (missing.Count != 0)
            {
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.MissingSummary(
                        missing));
            }
            if (eligible == 0)
            {
                return DiagnosticOperatorResult.NotApplicable(
                    "No animation parameter rows were captured.");
            }
            double score = Math.Max(0d, 1d - (double)failed / eligible);
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{failed.ToString(CultureInfo.InvariantCulture)} of {eligible.ToString(CultureInfo.InvariantCulture)} animation parameter rows failed availability.",
                score,
                Array.Empty<DiagnosticEvidence>(),
                findings);
        }

        static DiagnosticOperatorInputSlot Table(
            string id,
            DiagnosticValueKind kind) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.Table(id, kind);

        sealed class Inputs
        {
            public Inputs(DiagnosticOperatorExecutionContext context)
            {
                ParameterId = context.Input("parameter-id");
                ParameterValue = context.Input("parameter-value");
                ParameterAvailable = context.Input("parameter-available");
            }

            public DiagnosticBoundInput ParameterId { get; }
            public DiagnosticBoundInput ParameterValue { get; }
            public DiagnosticBoundInput ParameterAvailable { get; }
        }
    }

    internal sealed class CharacterPresentationAnimationStateContinuityOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "presentation-frame";
        const string Reset = "reset-sequence";
        const string StateMachineId = "state-machine-id";
        const string ActiveStateId = "active-state-id";
        const string ActiveTransitionId = "active-transition-id";
        const string TimeInState = "time-in-state";
        const string TransitionProgress = "transition-progress";
        const string TimeEpsilon = "time-epsilon-seconds";
        const string ProgressEpsilon = "progress-epsilon";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "animation/state-continuity",
                "animation",
                new[]
                {
                    Main(Frame, DiagnosticValueKind.UInt64),
                    Main(Reset, DiagnosticValueKind.UInt64),
                    Table(StateMachineId, DiagnosticValueKind.Identity),
                    Table(ActiveStateId, DiagnosticValueKind.Identity),
                    Table(ActiveTransitionId, DiagnosticValueKind.Identity),
                    Table(TimeInState, DiagnosticValueKind.Float32),
                    Table(TransitionProgress, DiagnosticValueKind.Float32)
                },
                new[]
                {
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Number(
                        TimeEpsilon,
                        0d),
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Number(
                        ProgressEpsilon,
                        0d)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            double timeEpsilon =
                CharacterPresentationReplicationDiagnosticOperatorSupport.RequireNumber(
                    context,
                    TimeEpsilon);
            double progressEpsilon =
                CharacterPresentationReplicationDiagnosticOperatorSupport.RequireNumber(
                    context,
                    ProgressEpsilon);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var frames = new Dictionary<ulong, FrameSample>();
            DiagnosticDatasetCursor frameCursor = context.CreateCursor(0);
            while (frameCursor.MoveNext())
            {
                DiagnosticDatasetRow row = frameCursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !CharacterPresentationReplicationDiagnosticOperatorSupport.Require(
                        in row,
                        new[] { inputs.Frame, inputs.Reset },
                        missing))
                {
                    continue;
                }
                frames[row.SampleKey.Sequence] = new FrameSample(
                    row.GetUInt64(inputs.Frame.Handle),
                    row.GetUInt64(inputs.Reset.Handle));
            }

            var samples = new Dictionary<string, List<StateSample>>(
                StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = context.CreateCursor(2);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !CharacterPresentationReplicationDiagnosticOperatorSupport.Require(
                        in row,
                        new[]
                        {
                            inputs.StateMachineId,
                            inputs.ActiveStateId,
                            inputs.ActiveTransitionId,
                            inputs.TimeInState,
                            inputs.TransitionProgress
                        },
                        missing))
                {
                    continue;
                }
                if (!frames.TryGetValue(row.SampleKey.Sequence, out FrameSample frame))
                {
                    missing.Add("animation-frame-for-state-machine");
                    continue;
                }
                string stateMachineId = row.GetIdentity(inputs.StateMachineId.Handle);
                if (string.IsNullOrEmpty(stateMachineId))
                {
                    missing.Add("animation-state-machine-id");
                    continue;
                }
                string key = row.SampleKey.DimensionId + "|" + stateMachineId;
                if (!samples.TryGetValue(key, out List<StateSample> values))
                {
                    values = new List<StateSample>();
                    samples.Add(key, values);
                }
                values.Add(new StateSample(
                    row,
                    frame,
                    stateMachineId,
                    row.GetIdentity(inputs.ActiveStateId.Handle),
                    row.GetIdentity(inputs.ActiveTransitionId.Handle),
                    row.GetFloat32(inputs.TimeInState.Handle),
                    row.GetFloat32(inputs.TransitionProgress.Handle)));
            }

            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            int failed = 0;
            int findingIndex = 0;
            foreach (List<StateSample> values in samples.Values)
            {
                StateSample previous = default;
                bool hasPrevious = false;
                for (int i = 0; i < values.Count; i++)
                {
                    StateSample current = values[i];
                    DiagnosticDatasetRow currentRow = current.Row;
                    eligible++;
                    bool invalidTime =
                        float.IsNaN(current.TimeInState) ||
                        float.IsInfinity(current.TimeInState) ||
                        current.TimeInState < -timeEpsilon;
                    bool invalidProgress =
                        float.IsNaN(current.TransitionProgress) ||
                        float.IsInfinity(current.TransitionProgress) ||
                        current.TransitionProgress < -progressEpsilon ||
                        current.TransitionProgress > 1d + progressEpsilon;
                    bool timeRegressed = hasPrevious &&
                        current.Frame.ResetSequence == previous.Frame.ResetSequence &&
                        string.Equals(current.ActiveStateId, previous.ActiveStateId, StringComparison.Ordinal) &&
                        current.TimeInState + timeEpsilon < previous.TimeInState;
                    bool progressRegressed = hasPrevious &&
                        current.Frame.ResetSequence == previous.Frame.ResetSequence &&
                        string.Equals(current.ActiveTransitionId, previous.ActiveTransitionId, StringComparison.Ordinal) &&
                        current.TransitionProgress + progressEpsilon < previous.TransitionProgress;
                    if (!invalidTime && !invalidProgress && !timeRegressed && !progressRegressed)
                    {
                        previous = current;
                        hasPrevious = true;
                        continue;
                    }
                    failed++;
                    var evidence = new List<DiagnosticEvidence>
                    {
                        CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                            "state-machine-id",
                            inputs.StateMachineId,
                            in currentRow,
                            current.StateMachineId),
                        CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                            "time-in-state",
                            inputs.TimeInState,
                            in currentRow,
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                current.TimeInState)),
                        CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                            "transition-progress",
                            inputs.TransitionProgress,
                            in currentRow,
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                current.TransitionProgress))
                    };
                    string reason = invalidTime
                        ? "invalid-time-in-state"
                        : invalidProgress
                            ? "invalid-transition-progress"
                            : timeRegressed
                                ? "time-regressed"
                                : "transition-progress-regressed";
                    findings.Add(
                        CharacterPresentationReplicationDiagnosticOperatorSupport.Finding(
                            context,
                            invalidTime || invalidProgress
                                ? DiagnosticSeverity.Error
                                : DiagnosticSeverity.Warning,
                            currentRow.SampleKey.DimensionId,
                            currentRow.SampleKey.Sequence,
                            currentRow.SampleKey.Sequence,
                            $"Animation state machine '{current.StateMachineId}' failed continuity: {reason}.",
                            findingIndex++,
                            evidence));
                    previous = current;
                    hasPrevious = true;
                }
            }
            if (missing.Count != 0)
            {
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.MissingSummary(
                        missing));
            }
            if (eligible == 0)
            {
                return DiagnosticOperatorResult.NotApplicable(
                    "No animation state-machine rows were captured.");
            }
            double score = Math.Max(0d, 1d - (double)failed / eligible);
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{failed.ToString(CultureInfo.InvariantCulture)} of {eligible.ToString(CultureInfo.InvariantCulture)} animation state observations failed continuity.",
                score,
                Array.Empty<DiagnosticEvidence>(),
                findings);
        }

        static DiagnosticOperatorInputSlot Main(
            string id,
            DiagnosticValueKind kind) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.Main(id, kind);

        static DiagnosticOperatorInputSlot Table(
            string id,
            DiagnosticValueKind kind) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.Table(id, kind);

        readonly struct FrameSample
        {
            public FrameSample(ulong presentationFrame, ulong resetSequence)
            {
                PresentationFrame = presentationFrame;
                ResetSequence = resetSequence;
            }

            public ulong PresentationFrame { get; }
            public ulong ResetSequence { get; }
        }

        readonly struct StateSample
        {
            public StateSample(
                DiagnosticDatasetRow row,
                in FrameSample frame,
                string stateMachineId,
                string activeStateId,
                string activeTransitionId,
                float timeInState,
                float transitionProgress)
            {
                Row = row;
                Frame = frame;
                StateMachineId = stateMachineId;
                ActiveStateId = activeStateId;
                ActiveTransitionId = activeTransitionId;
                TimeInState = timeInState;
                TransitionProgress = transitionProgress;
            }

            public DiagnosticDatasetRow Row { get; }
            public FrameSample Frame { get; }
            public string StateMachineId { get; }
            public string ActiveStateId { get; }
            public string ActiveTransitionId { get; }
            public float TimeInState { get; }
            public float TransitionProgress { get; }
        }

        sealed class Inputs
        {
            public Inputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input("presentation-frame");
                Reset = context.Input("reset-sequence");
                StateMachineId = context.Input("state-machine-id");
                ActiveStateId = context.Input("active-state-id");
                ActiveTransitionId = context.Input("active-transition-id");
                TimeInState = context.Input("time-in-state");
                TransitionProgress = context.Input("transition-progress");
            }

            public DiagnosticBoundInput Frame { get; }
            public DiagnosticBoundInput Reset { get; }
            public DiagnosticBoundInput StateMachineId { get; }
            public DiagnosticBoundInput ActiveStateId { get; }
            public DiagnosticBoundInput ActiveTransitionId { get; }
            public DiagnosticBoundInput TimeInState { get; }
            public DiagnosticBoundInput TransitionProgress { get; }
        }
    }
}
