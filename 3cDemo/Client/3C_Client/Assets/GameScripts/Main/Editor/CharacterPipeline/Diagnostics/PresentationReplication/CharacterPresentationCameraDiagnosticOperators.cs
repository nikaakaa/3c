using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    internal sealed class CharacterPresentationCameraPlanContinuityOperator :
        IDiagnosticAnalysisOperator
    {
        const string HasCamera = "has-camera";
        const string Frame = "presentation-frame";
        const string Reset = "reset-sequence";
        const string Delta = "delta-seconds";
        const string ResetTracking = "reset-tracking";
        const string PlanValid = "plan-valid";
        const string TargetValid = "target-valid";
        const string FinalOutput = "final-output-available";
        const string FinalPosition = "final-position";
        const string FinalRotation = "final-rotation";
        const string FieldOfView = "field-of-view";
        const string BasisValid = "basis-valid";
        const string MinimumFieldOfView = "minimum-field-of-view";
        const string MaximumFieldOfView = "maximum-field-of-view";
        const string MaximumPositionJump = "maximum-position-jump";
        const string MaximumAngleJump = "maximum-angle-jump";
        const string MaximumFieldOfViewJump = "maximum-field-of-view-jump";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "camera/plan-continuity",
                "camera",
                new[]
                {
                    Main(HasCamera, DiagnosticValueKind.Boolean),
                    Main(Frame, DiagnosticValueKind.UInt64),
                    Main(Reset, DiagnosticValueKind.UInt64),
                    Main(Delta, DiagnosticValueKind.Float32),
                    Main(ResetTracking, DiagnosticValueKind.Boolean),
                    Main(PlanValid, DiagnosticValueKind.Boolean),
                    Main(TargetValid, DiagnosticValueKind.Boolean),
                    Main(FinalOutput, DiagnosticValueKind.Boolean),
                    Main(FinalPosition, DiagnosticValueKind.Vector3),
                    Main(FinalRotation, DiagnosticValueKind.Quaternion),
                    Main(FieldOfView, DiagnosticValueKind.Float32),
                    Main(BasisValid, DiagnosticValueKind.Boolean)
                },
                new[]
                {
                    Number(MinimumFieldOfView, 1d),
                    Number(MaximumFieldOfView, 1d),
                    Number(MaximumPositionJump, 0d),
                    Number(MaximumAngleJump, 0d),
                    Number(MaximumFieldOfViewJump, 0d)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            double minimumFov = Number(context, MinimumFieldOfView);
            double maximumFov = Number(context, MaximumFieldOfView);
            double maximumPositionJump = Number(context, MaximumPositionJump);
            double maximumAngleJump = Number(context, MaximumAngleJump);
            double maximumFovJump = Number(context, MaximumFieldOfViewJump);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            var previous = new Dictionary<string, CameraSample>(StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            int eligible = 0;
            int failed = 0;
            int findingIndex = 0;
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !context.MatchesFilters(row))
                {
                    continue;
                }
                if (!CharacterPresentationReplicationDiagnosticOperatorSupport
                        .Require(
                            in row,
                            new[]
                            {
                                inputs.HasCamera,
                                inputs.Frame,
                                inputs.Reset,
                                inputs.Delta,
                                inputs.ResetTracking,
                                inputs.PlanValid,
                                inputs.TargetValid,
                                inputs.FinalOutput,
                                inputs.FinalPosition,
                                inputs.FinalRotation,
                                inputs.FieldOfView,
                                inputs.BasisValid
                            },
                            missing))
                {
                    continue;
                }
                if (!row.GetBoolean(inputs.HasCamera.Handle))
                    continue;
                eligible++;
                CameraSample current = new CameraSample(
                    row.GetUInt64(inputs.Frame.Handle),
                    row.GetUInt64(inputs.Reset.Handle),
                    row.GetBoolean(inputs.ResetTracking.Handle),
                    row.GetBoolean(inputs.PlanValid.Handle),
                    row.GetBoolean(inputs.TargetValid.Handle),
                    row.GetBoolean(inputs.FinalOutput.Handle),
                    row.GetVector3(inputs.FinalPosition.Handle),
                    row.GetQuaternion(inputs.FinalRotation.Handle),
                    row.GetFloat32(inputs.FieldOfView.Handle),
                    row.GetBoolean(inputs.BasisValid.Handle));
                var reasons = new List<string>();
                if (current.Frame == 0)
                    reasons.Add("presentation-frame-missing");
                if (!current.PlanValid)
                    reasons.Add("plan-invalid");
                if (!current.TargetValid)
                    reasons.Add("target-invalid");
                if (!current.FinalOutput)
                    reasons.Add("final-output-unavailable");
                if (!current.BasisValid && current.FinalOutput)
                    reasons.Add("camera-basis-invalid");
                if (current.FieldOfView < minimumFov ||
                    current.FieldOfView > maximumFov)
                {
                    reasons.Add("field-of-view-out-of-range");
                }
                if (previous.TryGetValue(
                        row.SampleKey.DimensionId,
                        out CameraSample prior) &&
                    current.ResetSequence == prior.ResetSequence &&
                    !current.ResetTracking &&
                    !prior.ResetTracking &&
                    current.FinalOutput &&
                    prior.FinalOutput)
                {
                    double positionJump =
                        CharacterPresentationReplicationDiagnosticOperatorSupport.Distance(
                            current.FinalPosition,
                            prior.FinalPosition);
                    double angleJump =
                        CharacterPresentationReplicationDiagnosticOperatorSupport.AngleDegrees(
                            current.FinalRotation,
                            prior.FinalRotation);
                    double fovJump = Math.Abs(current.FieldOfView - prior.FieldOfView);
                    if (positionJump > maximumPositionJump)
                        reasons.Add("position-jump");
                    if (angleJump > maximumAngleJump)
                        reasons.Add("rotation-jump");
                    if (fovJump > maximumFovJump)
                        reasons.Add("field-of-view-jump");
                }
                if (reasons.Count != 0)
                {
                    failed++;
                    findings.Add(
                        CharacterPresentationReplicationDiagnosticOperatorSupport.Finding(
                            context,
                            reasons.Contains("plan-invalid") ||
                            reasons.Contains("target-invalid") ||
                            reasons.Contains("final-output-unavailable") ||
                            reasons.Contains("field-of-view-out-of-range")
                                ? DiagnosticSeverity.Error
                                : DiagnosticSeverity.Warning,
                            in row,
                            "Camera presentation frame failed continuity: " +
                            string.Join(", ", reasons) + ".",
                            findingIndex++,
                            new[]
                            {
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "camera-plan",
                                    inputs.PlanValid,
                                    in row,
                                    current.PlanValid.ToString()),
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "camera-target",
                                    inputs.TargetValid,
                                    in row,
                                    current.TargetValid.ToString()),
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "camera-position",
                                    inputs.FinalPosition,
                                    in row,
                                    CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                        current.FinalPosition)),
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "camera-rotation",
                                    inputs.FinalRotation,
                                    in row,
                                    CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                        current.FinalRotation)),
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "camera-field-of-view",
                                    inputs.FieldOfView,
                                    in row,
                                    CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                        current.FieldOfView))
                            }));
                }
                previous[row.SampleKey.DimensionId] = current;
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
                    "No camera presentation frame was captured.");
            }
            double score = Math.Max(0d, 1d - (double)failed / eligible);
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{failed.ToString(CultureInfo.InvariantCulture)} of {eligible.ToString(CultureInfo.InvariantCulture)} camera frames failed continuity.",
                score,
                Array.Empty<DiagnosticEvidence>(),
                findings);
        }

        static DiagnosticOperatorInputSlot Main(
            string id,
            DiagnosticValueKind kind) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.Main(id, kind);

        static DiagnosticOperatorParameter Number(
            string id,
            double minimum) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.Number(
                id,
                minimum);

        static double Number(
            DiagnosticOperatorExecutionContext context,
            string id) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.RequireNumber(
                context,
                id);

        readonly struct CameraSample
        {
            public CameraSample(
                ulong frame,
                ulong resetSequence,
                bool resetTracking,
                bool planValid,
                bool targetValid,
                bool finalOutput,
                in DiagnosticVector3 finalPosition,
                in DiagnosticQuaternion finalRotation,
                float fieldOfView,
                bool basisValid)
            {
                Frame = frame;
                ResetSequence = resetSequence;
                ResetTracking = resetTracking;
                PlanValid = planValid;
                TargetValid = targetValid;
                FinalOutput = finalOutput;
                FinalPosition = finalPosition;
                FinalRotation = finalRotation;
                FieldOfView = fieldOfView;
                BasisValid = basisValid;
            }

            public ulong Frame { get; }
            public ulong ResetSequence { get; }
            public bool ResetTracking { get; }
            public bool PlanValid { get; }
            public bool TargetValid { get; }
            public bool FinalOutput { get; }
            public DiagnosticVector3 FinalPosition { get; }
            public DiagnosticQuaternion FinalRotation { get; }
            public float FieldOfView { get; }
            public bool BasisValid { get; }
        }

        sealed class Inputs
        {
            public Inputs(DiagnosticOperatorExecutionContext context)
            {
                HasCamera = context.Input("has-camera");
                Frame = context.Input("presentation-frame");
                Reset = context.Input("reset-sequence");
                Delta = context.Input("delta-seconds");
                ResetTracking = context.Input("reset-tracking");
                PlanValid = context.Input("plan-valid");
                TargetValid = context.Input("target-valid");
                FinalOutput = context.Input("final-output-available");
                FinalPosition = context.Input("final-position");
                FinalRotation = context.Input("final-rotation");
                FieldOfView = context.Input("field-of-view");
                BasisValid = context.Input("basis-valid");
            }

            public DiagnosticBoundInput HasCamera { get; }
            public DiagnosticBoundInput Frame { get; }
            public DiagnosticBoundInput Reset { get; }
            public DiagnosticBoundInput Delta { get; }
            public DiagnosticBoundInput ResetTracking { get; }
            public DiagnosticBoundInput PlanValid { get; }
            public DiagnosticBoundInput TargetValid { get; }
            public DiagnosticBoundInput FinalOutput { get; }
            public DiagnosticBoundInput FinalPosition { get; }
            public DiagnosticBoundInput FinalRotation { get; }
            public DiagnosticBoundInput FieldOfView { get; }
            public DiagnosticBoundInput BasisValid { get; }
        }
    }

    internal sealed class CharacterPresentationCameraCueLifecycleOperator :
        IDiagnosticAnalysisOperator
    {
        const string CueId = "cue-id";
        const string CueKind = "cue-kind";
        const string CueType = "cue-type";
        const string Intensity = "intensity";
        const string Duration = "duration-seconds";
        const string Priority = "priority";
        const string SourceId = "source-id";
        const string Active = "active";
        const string MaximumDuration = "maximum-duration-seconds";
        const string MaximumKind = "maximum-cue-kind";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "camera/cue-lifecycle",
                "camera",
                new[]
                {
                    Table(CueId, DiagnosticValueKind.Identity),
                    Table(CueKind, DiagnosticValueKind.Int32),
                    Table(CueType, DiagnosticValueKind.Identity),
                    Table(Intensity, DiagnosticValueKind.Float32),
                    Table(Duration, DiagnosticValueKind.Float32),
                    Table(Priority, DiagnosticValueKind.Int32),
                    Table(SourceId, DiagnosticValueKind.Identity),
                    Table(Active, DiagnosticValueKind.Boolean)
                },
                new[]
                {
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Number(
                        MaximumDuration,
                        0d),
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Integer(
                        MaximumKind,
                        0d)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            double maximumDuration =
                CharacterPresentationReplicationDiagnosticOperatorSupport.RequireNumber(
                    context,
                    MaximumDuration);
            uint maximumKind =
                CharacterPresentationReplicationDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    MaximumKind);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
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
                        .Require(
                            in row,
                            new[]
                            {
                                inputs.CueId,
                                inputs.CueKind,
                                inputs.CueType,
                                inputs.Intensity,
                                inputs.Duration,
                                inputs.Priority,
                                inputs.SourceId,
                                inputs.Active
                            },
                            missing))
                {
                    continue;
                }
                string cueId = row.GetIdentity(inputs.CueId.Handle);
                if (string.IsNullOrEmpty(cueId))
                {
                    missing.Add("camera-cue-id");
                    continue;
                }
                eligible++;
                int cueKind = row.GetInt32(inputs.CueKind.Handle);
                float intensity = row.GetFloat32(inputs.Intensity.Handle);
                float duration = row.GetFloat32(inputs.Duration.Handle);
                bool active = row.GetBoolean(inputs.Active.Handle);
                bool valid = cueKind >= 0 &&
                    (uint)cueKind <= maximumKind &&
                    !float.IsNaN(intensity) &&
                    !float.IsInfinity(intensity) &&
                    intensity >= 0f &&
                    !float.IsNaN(duration) &&
                    !float.IsInfinity(duration) &&
                    duration >= 0f &&
                    duration <= maximumDuration &&
                    (!active || !string.IsNullOrEmpty(
                        row.GetIdentity(inputs.SourceId.Handle)));
                if (valid)
                    continue;
                failed++;
                findings.Add(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Finding(
                        context,
                        DiagnosticSeverity.Error,
                        in row,
                        $"Camera cue '{cueId}' failed lifecycle contract.",
                        findingIndex++,
                        new[]
                        {
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "cue-id",
                                inputs.CueId,
                                in row,
                                cueId),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "cue-kind",
                                inputs.CueKind,
                                in row,
                                cueKind.ToString(CultureInfo.InvariantCulture)),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "intensity",
                                inputs.Intensity,
                                in row,
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                    intensity)),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "duration-seconds",
                                inputs.Duration,
                                in row,
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                    duration)),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "source-id",
                                inputs.SourceId,
                                in row,
                                row.GetIdentity(inputs.SourceId.Handle))
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
                    "No camera cue rows were captured.");
            }
            double score = Math.Max(0d, 1d - (double)failed / eligible);
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{failed.ToString(CultureInfo.InvariantCulture)} of {eligible.ToString(CultureInfo.InvariantCulture)} camera cue rows failed lifecycle validation.",
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
                CueId = context.Input("cue-id");
                CueKind = context.Input("cue-kind");
                CueType = context.Input("cue-type");
                Intensity = context.Input("intensity");
                Duration = context.Input("duration-seconds");
                Priority = context.Input("priority");
                SourceId = context.Input("source-id");
                Active = context.Input("active");
            }

            public DiagnosticBoundInput CueId { get; }
            public DiagnosticBoundInput CueKind { get; }
            public DiagnosticBoundInput CueType { get; }
            public DiagnosticBoundInput Intensity { get; }
            public DiagnosticBoundInput Duration { get; }
            public DiagnosticBoundInput Priority { get; }
            public DiagnosticBoundInput SourceId { get; }
            public DiagnosticBoundInput Active { get; }
        }
    }

    internal sealed class CharacterPresentationCameraCueRoutingOperator :
        IDiagnosticAnalysisOperator
    {
        const string EventIdentity = "event-identity";
        const string CommandKind = "kind";
        const string CommandWeight = "weight";
        const string CueSourceId = "source-id";
        const string CueId = "cue-id";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "camera/cue-routing",
                "camera",
                new[]
                {
                    Table(EventIdentity, DiagnosticValueKind.Identity),
                    Table(CommandKind, DiagnosticValueKind.UInt32),
                    Table(CommandWeight, DiagnosticValueKind.Float32),
                    Table(CueSourceId, DiagnosticValueKind.Identity),
                    Table(CueId, DiagnosticValueKind.Identity)
                },
                Array.Empty<DiagnosticOperatorParameter>());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var commandsBySample = new Dictionary<string, List<CommandSample>>(
                StringComparer.Ordinal);
            var cuesBySample = new Dictionary<string, HashSet<string>>(
                StringComparer.Ordinal);
            DiagnosticDatasetCursor commandCursor = context.CreateCursor(0);
            while (commandCursor.MoveNext())
            {
                DiagnosticDatasetRow row = commandCursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !CharacterPresentationReplicationDiagnosticOperatorSupport
                        .Require(
                            in row,
                            new[]
                            {
                                inputs.EventIdentity,
                                inputs.CommandKind,
                                inputs.CommandWeight
                            },
                            missing))
                {
                    continue;
                }
                string eventIdentity = row.GetIdentity(inputs.EventIdentity.Handle);
                if (string.IsNullOrEmpty(eventIdentity))
                {
                    missing.Add("presentation-command-event-identity");
                    continue;
                }
                if (row.GetUInt32(inputs.CommandKind.Handle) != 6 ||
                    row.GetFloat32(inputs.CommandWeight.Handle) <= 0f)
                {
                    continue;
                }
                string key = SampleKey(row);
                if (!commandsBySample.TryGetValue(
                        key,
                        out List<CommandSample> commands))
                {
                    commands = new List<CommandSample>();
                    commandsBySample.Add(key, commands);
                }
                commands.Add(
                    new CommandSample(
                        in row,
                        eventIdentity,
                        row.GetFloat32(inputs.CommandWeight.Handle)));
            }

            DiagnosticDatasetCursor cueCursor = context.CreateCursor(3);
            while (cueCursor.MoveNext())
            {
                DiagnosticDatasetRow row = cueCursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !CharacterPresentationReplicationDiagnosticOperatorSupport
                        .Require(
                            in row,
                            new[] { inputs.CueSourceId, inputs.CueId },
                            missing))
                {
                    continue;
                }
                string sourceId = row.GetIdentity(inputs.CueSourceId.Handle);
                string cueId = row.GetIdentity(inputs.CueId.Handle);
                if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(cueId))
                {
                    missing.Add("camera-cue-source-identity");
                    continue;
                }
                string key = SampleKey(row);
                if (!cuesBySample.TryGetValue(
                        key,
                        out HashSet<string> sources))
                {
                    sources = new HashSet<string>(StringComparer.Ordinal);
                    cuesBySample.Add(key, sources);
                }
                sources.Add(sourceId);
            }

            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            int failed = 0;
            int findingIndex = 0;
            foreach (KeyValuePair<string, List<CommandSample>> pair in commandsBySample)
            {
                foreach (CommandSample command in pair.Value)
                {
                    DiagnosticDatasetRow commandRow = command.Row;
                    eligible++;
                    if (cuesBySample.TryGetValue(
                            pair.Key,
                            out HashSet<string> sources) &&
                        sources.Contains(command.EventIdentity))
                    {
                        continue;
                    }
                    failed++;
                    findings.Add(
                        CharacterPresentationReplicationDiagnosticOperatorSupport.Finding(
                            context,
                            DiagnosticSeverity.Error,
                            in commandRow,
                            $"Camera Cue command '{command.EventIdentity}' did not produce an active cue in the same presentation frame.",
                            findingIndex++,
                            new[]
                            {
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "event-identity",
                                    inputs.EventIdentity,
                                    in commandRow,
                                    command.EventIdentity),
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "kind",
                                    inputs.CommandKind,
                                    in commandRow,
                                    "6"),
                                CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                    "weight",
                                    inputs.CommandWeight,
                                    in commandRow,
                                    CharacterPresentationReplicationDiagnosticOperatorSupport.Format(
                                        command.Weight))
                            }));
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
                    "No positive camera Cue command was captured.");
            }
            double score = Math.Max(0d, 1d - (double)failed / eligible);
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{failed.ToString(CultureInfo.InvariantCulture)} of {eligible.ToString(CultureInfo.InvariantCulture)} camera Cue commands failed routing.",
                score,
                Array.Empty<DiagnosticEvidence>(),
                findings);
        }

        static string SampleKey(in DiagnosticDatasetRow row) =>
            row.SampleKey.DimensionId + "|" +
            row.SampleKey.Sequence.ToString(CultureInfo.InvariantCulture);

        static DiagnosticOperatorInputSlot Table(
            string id,
            DiagnosticValueKind kind) =>
            CharacterPresentationReplicationDiagnosticOperatorSupport.Table(id, kind);

        readonly struct CommandSample
        {
            public CommandSample(
                in DiagnosticDatasetRow row,
                string eventIdentity,
                float weight)
            {
                Row = row;
                EventIdentity = eventIdentity;
                Weight = weight;
            }

            public DiagnosticDatasetRow Row { get; }
            public string EventIdentity { get; }
            public float Weight { get; }
        }

        sealed class Inputs
        {
            public Inputs(DiagnosticOperatorExecutionContext context)
            {
                EventIdentity = context.Input("event-identity");
                CommandKind = context.Input("kind");
                CommandWeight = context.Input("weight");
                CueSourceId = context.Input("source-id");
                CueId = context.Input("cue-id");
            }

            public DiagnosticBoundInput EventIdentity { get; }
            public DiagnosticBoundInput CommandKind { get; }
            public DiagnosticBoundInput CommandWeight { get; }
            public DiagnosticBoundInput CueSourceId { get; }
            public DiagnosticBoundInput CueId { get; }
        }
    }
}
