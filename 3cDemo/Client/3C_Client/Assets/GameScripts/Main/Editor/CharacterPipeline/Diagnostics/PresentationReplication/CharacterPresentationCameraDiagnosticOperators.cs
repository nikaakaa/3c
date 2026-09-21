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
        const string ResetReason = "reset-reason";
        const string SequenceRetiring = "sequence-retiring";
        const string SequenceStopReason = "sequence-stop-reason";
        const string PlanValid = "plan-valid";
        const string TargetValid = "target-valid";
        const string TargetRetired = "target-retired";
        const string TargetStopReason = "target-stop-reason";
        const string TargetRetiredKey = "target-retired-key";
        const string FinalOutput = "final-output-available";
        const string FinalPosition = "final-position";
        const string FinalRotation = "final-rotation";
        const string FieldOfView = "field-of-view";
        const string BasisValid = "basis-valid";
        const string CollisionStatus = "collision-status";
        const string CollisionCorrection = "collision-correction-distance";
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
                    Main(ResetReason, DiagnosticValueKind.Int32),
                    Main(SequenceRetiring, DiagnosticValueKind.Boolean),
                    Main(SequenceStopReason, DiagnosticValueKind.Int32),
                    Main(PlanValid, DiagnosticValueKind.Boolean),
                    Main(TargetValid, DiagnosticValueKind.Boolean),
                    Main(TargetRetired, DiagnosticValueKind.Boolean),
                    Main(TargetStopReason, DiagnosticValueKind.Int32),
                    Main(TargetRetiredKey, DiagnosticValueKind.Identity),
                    Main(FinalOutput, DiagnosticValueKind.Boolean),
                    Main(FinalPosition, DiagnosticValueKind.Vector3),
                    Main(FinalRotation, DiagnosticValueKind.Quaternion),
                    Main(FieldOfView, DiagnosticValueKind.Float32),
                    Main(BasisValid, DiagnosticValueKind.Boolean),
                    Main(CollisionStatus, DiagnosticValueKind.Int32),
                    Main(CollisionCorrection, DiagnosticValueKind.Float32)
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
                                inputs.ResetReason,
                                inputs.SequenceRetiring,
                                inputs.SequenceStopReason,
                                inputs.PlanValid,
                                inputs.TargetValid,
                                inputs.TargetRetired,
                                inputs.TargetStopReason,
                                inputs.TargetRetiredKey,
                                inputs.FinalOutput,
                                inputs.FinalPosition,
                                inputs.FinalRotation,
                                inputs.FieldOfView,
                                inputs.BasisValid,
                                inputs.CollisionStatus,
                                inputs.CollisionCorrection
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
                    row.GetInt32(inputs.ResetReason.Handle),
                    row.GetBoolean(inputs.SequenceRetiring.Handle),
                    row.GetInt32(inputs.SequenceStopReason.Handle),
                    row.GetBoolean(inputs.PlanValid.Handle),
                    row.GetBoolean(inputs.TargetValid.Handle),
                    row.GetBoolean(inputs.TargetRetired.Handle),
                    row.GetInt32(inputs.TargetStopReason.Handle),
                    row.GetIdentity(inputs.TargetRetiredKey.Handle),
                    row.GetBoolean(inputs.FinalOutput.Handle),
                    row.GetVector3(inputs.FinalPosition.Handle),
                    row.GetQuaternion(inputs.FinalRotation.Handle),
                    row.GetFloat32(inputs.FieldOfView.Handle),
                    row.GetBoolean(inputs.BasisValid.Handle),
                    row.GetInt32(inputs.CollisionStatus.Handle),
                    row.GetFloat32(inputs.CollisionCorrection.Handle));
                var reasons = new List<string>();
                if (current.Frame == 0)
                    reasons.Add("presentation-frame-missing");
                if (current.ResetReason < 0 || current.ResetReason > 4)
                    reasons.Add("reset-reason-invalid");
                if (current.SequenceRetiring &&
                    (current.SequenceStopReason < 1 || current.SequenceStopReason > 4))
                    reasons.Add("sequence-stop-reason-invalid");
                if (!current.PlanValid)
                    reasons.Add("plan-invalid");
                if (!current.TargetValid)
                    reasons.Add("target-invalid");
                if (current.TargetRetired &&
                    (current.TargetStopReason != 4 || string.IsNullOrEmpty(current.TargetRetiredKey)))
                    reasons.Add("target-retirement-invalid");
                if (!current.FinalOutput)
                    reasons.Add("final-output-unavailable");
                if (!current.BasisValid && current.FinalOutput)
                    reasons.Add("camera-basis-invalid");
                if (current.CollisionStatus < 0 || current.CollisionStatus > 4 ||
                    float.IsNaN(current.CollisionCorrection) ||
                    float.IsInfinity(current.CollisionCorrection) ||
                    current.CollisionCorrection < 0f)
                    reasons.Add("collision-result-invalid");
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
                            reasons.Contains("sequence-stop-reason-invalid") ||
                            reasons.Contains("target-retirement-invalid") ||
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
                int resetReason,
                bool sequenceRetiring,
                int sequenceStopReason,
                bool planValid,
                bool targetValid,
                bool targetRetired,
                int targetStopReason,
                string targetRetiredKey,
                bool finalOutput,
                in DiagnosticVector3 finalPosition,
                in DiagnosticQuaternion finalRotation,
                float fieldOfView,
                bool basisValid,
                int collisionStatus,
                float collisionCorrection)
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
                ResetReason = resetReason;
                SequenceRetiring = sequenceRetiring;
                SequenceStopReason = sequenceStopReason;
                CollisionStatus = collisionStatus;
                CollisionCorrection = collisionCorrection;
                TargetRetired = targetRetired;
                TargetStopReason = targetStopReason;
                TargetRetiredKey = targetRetiredKey ?? string.Empty;
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
            public int ResetReason { get; }
            public bool SequenceRetiring { get; }
            public int SequenceStopReason { get; }
            public int CollisionStatus { get; }
            public float CollisionCorrection { get; }
            public bool TargetRetired { get; }
            public int TargetStopReason { get; }
            public string TargetRetiredKey { get; }
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
                ResetReason = context.Input("reset-reason");
                SequenceRetiring = context.Input("sequence-retiring");
                SequenceStopReason = context.Input("sequence-stop-reason");
                PlanValid = context.Input("plan-valid");
                TargetValid = context.Input("target-valid");
                TargetRetired = context.Input("target-retired");
                TargetStopReason = context.Input("target-stop-reason");
                TargetRetiredKey = context.Input("target-retired-key");
                FinalOutput = context.Input("final-output-available");
                FinalPosition = context.Input("final-position");
                FinalRotation = context.Input("final-rotation");
                FieldOfView = context.Input("field-of-view");
                BasisValid = context.Input("basis-valid");
                CollisionStatus = context.Input("collision-status");
                CollisionCorrection = context.Input("collision-correction-distance");
            }

            public DiagnosticBoundInput HasCamera { get; }
            public DiagnosticBoundInput Frame { get; }
            public DiagnosticBoundInput Reset { get; }
            public DiagnosticBoundInput Delta { get; }
            public DiagnosticBoundInput ResetTracking { get; }
            public DiagnosticBoundInput ResetReason { get; }
            public DiagnosticBoundInput SequenceRetiring { get; }
            public DiagnosticBoundInput SequenceStopReason { get; }
            public DiagnosticBoundInput PlanValid { get; }
            public DiagnosticBoundInput TargetValid { get; }
            public DiagnosticBoundInput TargetRetired { get; }
            public DiagnosticBoundInput TargetStopReason { get; }
            public DiagnosticBoundInput TargetRetiredKey { get; }
            public DiagnosticBoundInput FinalOutput { get; }
            public DiagnosticBoundInput FinalPosition { get; }
            public DiagnosticBoundInput FinalRotation { get; }
            public DiagnosticBoundInput FieldOfView { get; }
            public DiagnosticBoundInput BasisValid { get; }
            public DiagnosticBoundInput CollisionStatus { get; }
            public DiagnosticBoundInput CollisionCorrection { get; }
        }
    }

    internal sealed class CharacterPresentationCameraEffectLifecycleOperator :
        IDiagnosticAnalysisOperator
    {
        const string Stage = "stage";
        const string ResourceId = "resource-id";
        const string Weight = "weight";
        const string Remaining = "remaining-seconds";
        const string Priority = "priority";
        const string Active = "active";
        const string SourceId = "source-id";
        const string Generation = "generation";
        const string SourceActionInstanceId = "source-action-instance-id";
        const string Cycle = "cycle";
        const string EventId = "event-id";
        const string StopReason = "stop-reason";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "camera/effect-lifecycle",
                "camera",
                new[]
                {
                    Table(Stage, DiagnosticValueKind.Int32),
                    Table(ResourceId, DiagnosticValueKind.Identity),
                    Table(Weight, DiagnosticValueKind.Float32),
                    Table(Remaining, DiagnosticValueKind.Float32),
                    Table(Priority, DiagnosticValueKind.Int32),
                    Table(Active, DiagnosticValueKind.Boolean),
                    Table(SourceId, DiagnosticValueKind.Identity),
                    Table(Generation, DiagnosticValueKind.UInt64),
                    Table(SourceActionInstanceId, DiagnosticValueKind.UInt64),
                    Table(Cycle, DiagnosticValueKind.Int32),
                    Table(EventId, DiagnosticValueKind.Identity),
                    Table(StopReason, DiagnosticValueKind.Int32)
                },
                Array.Empty<DiagnosticOperatorParameter>());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            DiagnosticDatasetCursor cursor = context.CreateCursor(1);
            int eligible = 0;
            int failed = 0;
            int findingIndex = 0;
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (!context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !CharacterPresentationReplicationDiagnosticOperatorSupport.Require(
                        in row,
                        new[]
                        {
                            inputs.Stage,
                            inputs.ResourceId,
                            inputs.Weight,
                            inputs.Remaining,
                            inputs.Priority,
                            inputs.Active,
                            inputs.SourceId,
                            inputs.Generation,
                            inputs.SourceActionInstanceId,
                            inputs.Cycle,
                            inputs.EventId,
                            inputs.StopReason
                        },
                        missing))
                    continue;
                eligible++;
                int stage = row.GetInt32(inputs.Stage.Handle);
                string resourceId = row.GetIdentity(inputs.ResourceId.Handle);
                float weight = row.GetFloat32(inputs.Weight.Handle);
                float remaining = row.GetFloat32(inputs.Remaining.Handle);
                bool active = row.GetBoolean(inputs.Active.Handle);
                string sourceId = row.GetIdentity(inputs.SourceId.Handle);
                int cycle = row.GetInt32(inputs.Cycle.Handle);
                string eventId = row.GetIdentity(inputs.EventId.Handle);
                int stopReason = row.GetInt32(inputs.StopReason.Handle);
                bool valid = stage >= 1 && stage <= 6 &&
                    !string.IsNullOrEmpty(resourceId) &&
                    !float.IsNaN(weight) && !float.IsInfinity(weight) && weight >= 0f &&
                    !float.IsNaN(remaining) && remaining >= 0f &&
                    cycle >= 0 && stopReason >= 1 && stopReason <= 4 &&
                    (!active || !string.IsNullOrEmpty(sourceId) || !string.IsNullOrEmpty(eventId));
                if (valid)
                    continue;
                failed++;
                findings.Add(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.Finding(
                        context,
                        DiagnosticSeverity.Error,
                        in row,
                        $"Camera effect '{resourceId}' failed lifecycle contract.",
                        findingIndex++,
                        new[]
                        {
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "resource-id",
                                inputs.ResourceId,
                                in row,
                                resourceId),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "stage",
                                inputs.Stage,
                                in row,
                                stage.ToString(CultureInfo.InvariantCulture)),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "source-id",
                                inputs.SourceId,
                                in row,
                                sourceId),
                            CharacterPresentationReplicationDiagnosticOperatorSupport.Evidence(
                                "event-id",
                                inputs.EventId,
                                in row,
                                eventId)
                        }));
            }
            if (missing.Count != 0)
            {
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterPresentationReplicationDiagnosticOperatorSupport.MissingSummary(
                        missing));
            }
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No camera effect rows were captured.");
            double score = Math.Max(0d, 1d - (double)failed / eligible);
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{failed.ToString(CultureInfo.InvariantCulture)} of {eligible.ToString(CultureInfo.InvariantCulture)} camera effect rows failed lifecycle validation.",
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
                Stage = context.Input(StageId);
                ResourceId = context.Input(ResourceIdId);
                Weight = context.Input(WeightId);
                Remaining = context.Input(RemainingId);
                Priority = context.Input(PriorityId);
                Active = context.Input(ActiveId);
                SourceId = context.Input(SourceIdId);
                Generation = context.Input(GenerationId);
                SourceActionInstanceId = context.Input(SourceActionInstanceIdId);
                Cycle = context.Input(CycleId);
                EventId = context.Input(EventIdId);
                StopReason = context.Input(StopReasonId);
            }

            const string StageId = "stage";
            const string ResourceIdId = "resource-id";
            const string WeightId = "weight";
            const string RemainingId = "remaining-seconds";
            const string PriorityId = "priority";
            const string ActiveId = "active";
            const string SourceIdId = "source-id";
            const string GenerationId = "generation";
            const string SourceActionInstanceIdId = "source-action-instance-id";
            const string CycleId = "cycle";
            const string EventIdId = "event-id";
            const string StopReasonId = "stop-reason";

            public DiagnosticBoundInput Stage { get; }
            public DiagnosticBoundInput ResourceId { get; }
            public DiagnosticBoundInput Weight { get; }
            public DiagnosticBoundInput Remaining { get; }
            public DiagnosticBoundInput Priority { get; }
            public DiagnosticBoundInput Active { get; }
            public DiagnosticBoundInput SourceId { get; }
            public DiagnosticBoundInput Generation { get; }
            public DiagnosticBoundInput SourceActionInstanceId { get; }
            public DiagnosticBoundInput Cycle { get; }
            public DiagnosticBoundInput EventId { get; }
            public DiagnosticBoundInput StopReason { get; }
        }
    }
}
