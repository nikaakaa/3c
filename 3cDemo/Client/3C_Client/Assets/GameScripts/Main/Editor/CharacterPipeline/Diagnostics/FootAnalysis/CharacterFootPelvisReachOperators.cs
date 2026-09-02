using System;
using System.Collections.Generic;
using System.Linq;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootLandingLegInputs
    {
        internal const string FrameSequence = "frame-sequence";
        internal const string ResetSequence = "reset-sequence";
        internal const string ConstraintState = "constraint-state";
        internal const string LandingEventIdentity = "landing-event-identity";
        internal const string OriginalHip = "original-hip";
        internal const string OriginalKnee = "original-knee";
        internal const string OriginalAnkle = "original-ankle";
        internal const string TargetAnkle = "target-ankle";
        internal const string SolvedHip = "solved-hip";
        internal const string SolvedKnee = "solved-knee";
        internal const string SolvedAnkle = "solved-ankle";
        internal const string OriginalExtensionRatio = "original-extension-ratio";
        internal const string TargetExtensionRatio = "target-extension-ratio";
        internal const string SolvedExtensionRatio = "solved-extension-ratio";
        internal const string SolvedBendDegrees = "solved-bend-degrees";
        internal const string BendDirectionPreviousDot = "bend-direction-previous-dot";
        internal const string OriginalCompressionReserve = "original-compression-reserve";
        internal const string TargetCompressionReserve = "target-compression-reserve";
        internal const string SolvedCompressionReserve = "solved-compression-reserve";
        internal const string ComponentUp = "component-up";
        internal const string LandingReachAvailable = "landing-reach-available";
        internal const string LandingReachHip = "landing-reach-hip";
        internal const string LandingReachTargetAnkle = "landing-reach-target-ankle";
        internal const string LandingReachLegLength = "landing-reach-leg-length";
        internal const string LandingReachReserve = "landing-reach-reserve";
        internal const string PelvisReachEvaluated = "pelvis-reach-evaluated";
        internal const string PelvisReachStatus = "pelvis-reach-status";
        internal const string PelvisReachMinimum = "pelvis-reach-minimum";
        internal const string PelvisReachMaximum = "pelvis-reach-maximum";

        internal CharacterFootLandingLegInputs(DiagnosticOperatorExecutionContext context)
        {
            Frame = context.Input(FrameSequence);
            Reset = context.Input(ResetSequence);
            State = context.Input(ConstraintState);
            Event = context.Input(LandingEventIdentity);
            OriginalHipPoint = context.Input(OriginalHip);
            OriginalKneePoint = context.Input(OriginalKnee);
            OriginalAnklePoint = context.Input(OriginalAnkle);
            TargetAnklePoint = context.Input(TargetAnkle);
            SolvedHipPoint = context.Input(SolvedHip);
            SolvedKneePoint = context.Input(SolvedKnee);
            SolvedAnklePoint = context.Input(SolvedAnkle);
            OriginalExtension = context.Input(OriginalExtensionRatio);
            TargetExtension = context.Input(TargetExtensionRatio);
            SolvedExtension = context.Input(SolvedExtensionRatio);
            SolvedBend = context.Input(SolvedBendDegrees);
            BendDirectionDot = context.Input(BendDirectionPreviousDot);
            OriginalCompression = context.Input(OriginalCompressionReserve);
            TargetCompression = context.Input(TargetCompressionReserve);
            SolvedCompression = context.Input(SolvedCompressionReserve);
            Up = context.Input(ComponentUp);
            ReachAvailable = context.Input(LandingReachAvailable);
            ReachHip = context.Input(LandingReachHip);
            ReachTarget = context.Input(LandingReachTargetAnkle);
            ReachLegLength = context.Input(LandingReachLegLength);
            ReachReserve = context.Input(LandingReachReserve);
            PelvisEvaluated = context.Input(PelvisReachEvaluated);
            PelvisStatus = context.Input(PelvisReachStatus);
            PelvisMinimum = context.Input(PelvisReachMinimum);
            PelvisMaximum = context.Input(PelvisReachMaximum);
            All = new[]
            {
                Frame,
                Reset,
                State,
                Event,
                OriginalHipPoint,
                OriginalKneePoint,
                OriginalAnklePoint,
                TargetAnklePoint,
                SolvedHipPoint,
                SolvedKneePoint,
                SolvedAnklePoint,
                OriginalExtension,
                TargetExtension,
                SolvedExtension,
                SolvedBend,
                BendDirectionDot,
                OriginalCompression,
                TargetCompression,
                SolvedCompression,
                Up,
                ReachAvailable,
                PelvisEvaluated,
                PelvisStatus
            };
        }

        internal DiagnosticBoundInput Frame { get; }
        internal DiagnosticBoundInput Reset { get; }
        internal DiagnosticBoundInput State { get; }
        internal DiagnosticBoundInput Event { get; }
        internal DiagnosticBoundInput OriginalHipPoint { get; }
        internal DiagnosticBoundInput OriginalKneePoint { get; }
        internal DiagnosticBoundInput OriginalAnklePoint { get; }
        internal DiagnosticBoundInput TargetAnklePoint { get; }
        internal DiagnosticBoundInput SolvedHipPoint { get; }
        internal DiagnosticBoundInput SolvedKneePoint { get; }
        internal DiagnosticBoundInput SolvedAnklePoint { get; }
        internal DiagnosticBoundInput OriginalExtension { get; }
        internal DiagnosticBoundInput TargetExtension { get; }
        internal DiagnosticBoundInput SolvedExtension { get; }
        internal DiagnosticBoundInput SolvedBend { get; }
        internal DiagnosticBoundInput BendDirectionDot { get; }
        internal DiagnosticBoundInput OriginalCompression { get; }
        internal DiagnosticBoundInput TargetCompression { get; }
        internal DiagnosticBoundInput SolvedCompression { get; }
        internal DiagnosticBoundInput Up { get; }
        internal DiagnosticBoundInput ReachAvailable { get; }
        internal DiagnosticBoundInput ReachHip { get; }
        internal DiagnosticBoundInput ReachTarget { get; }
        internal DiagnosticBoundInput ReachLegLength { get; }
        internal DiagnosticBoundInput ReachReserve { get; }
        internal DiagnosticBoundInput PelvisEvaluated { get; }
        internal DiagnosticBoundInput PelvisStatus { get; }
        internal DiagnosticBoundInput PelvisMinimum { get; }
        internal DiagnosticBoundInput PelvisMaximum { get; }
        internal IReadOnlyList<DiagnosticBoundInput> All { get; }

        internal static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            Slot(FrameSequence, DiagnosticValueKind.UInt64),
            Slot(ResetSequence, DiagnosticValueKind.UInt64),
            Slot(ConstraintState, DiagnosticValueKind.UInt32),
            Slot(LandingEventIdentity, DiagnosticValueKind.UInt64),
            Slot(OriginalHip, DiagnosticValueKind.Vector3),
            Slot(OriginalKnee, DiagnosticValueKind.Vector3),
            Slot(OriginalAnkle, DiagnosticValueKind.Vector3),
            Slot(TargetAnkle, DiagnosticValueKind.Vector3),
            Slot(SolvedHip, DiagnosticValueKind.Vector3),
            Slot(SolvedKnee, DiagnosticValueKind.Vector3),
            Slot(SolvedAnkle, DiagnosticValueKind.Vector3),
            Slot(OriginalExtensionRatio, DiagnosticValueKind.Float32),
            Slot(TargetExtensionRatio, DiagnosticValueKind.Float32),
            Slot(SolvedExtensionRatio, DiagnosticValueKind.Float32),
            Slot(SolvedBendDegrees, DiagnosticValueKind.Float32),
            Slot(BendDirectionPreviousDot, DiagnosticValueKind.Float32),
            Slot(OriginalCompressionReserve, DiagnosticValueKind.Float32),
            Slot(TargetCompressionReserve, DiagnosticValueKind.Float32),
            Slot(SolvedCompressionReserve, DiagnosticValueKind.Float32),
            Slot(ComponentUp, DiagnosticValueKind.Vector3),
            Slot(LandingReachAvailable, DiagnosticValueKind.Boolean),
            Slot(LandingReachHip, DiagnosticValueKind.Vector3),
            Slot(LandingReachTargetAnkle, DiagnosticValueKind.Vector3),
            Slot(LandingReachLegLength, DiagnosticValueKind.Float32),
            Slot(LandingReachReserve, DiagnosticValueKind.Float32),
            Slot(PelvisReachEvaluated, DiagnosticValueKind.Boolean),
            Slot(PelvisReachStatus, DiagnosticValueKind.UInt32),
            Slot(PelvisReachMinimum, DiagnosticValueKind.Float32),
            Slot(PelvisReachMaximum, DiagnosticValueKind.Float32)
        };

        static DiagnosticOperatorInputSlot Slot(string id, DiagnosticValueKind kind) =>
            new DiagnosticOperatorInputSlot(id, kind, DiagnosticDatasetCardinality.Main, true);
    }

    internal sealed class CharacterFootLandingLegExtensionOperator :
        IDiagnosticAnalysisOperator
    {
        const string LandingState = "landing-state";
        const string LockedState = "locked-state";
        const string ReleasingState = "releasing-state";
        const string ExtensionThreshold = "extension-ratio-delta";
        const string BendDropThreshold = "bend-drop-degrees";
        const string ReachReserve = "candidate-compression-reserve-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/landing-leg-extension",
                "pelvis",
                CharacterFootLandingLegInputs.Slots(),
                Parameters());

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
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
            double extensionThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                ExtensionThreshold);
            double bendThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                BendDropThreshold);
            double reachReserve = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                ReachReserve);
            var inputs = new CharacterFootLandingLegInputs(context);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (IGrouping<string, DiagnosticDatasetRow> group in Rows(context, inputs.Frame)
                .GroupBy(value => value.SampleKey.DimensionId, StringComparer.Ordinal))
            {
                DiagnosticDatasetRow[] rows = group
                    .OrderBy(value => value.SampleKey.Sequence)
                    .ToArray();
                for (int i = 1; i < rows.Length; i++)
                {
                    DiagnosticDatasetRow previous = rows[i - 1];
                    DiagnosticDatasetRow current = rows[i];
                    if (!Available(previous, inputs.Frame, inputs.Reset, inputs.State, inputs.Event) ||
                        !Available(current, inputs.Frame, inputs.Reset, inputs.State, inputs.Event))
                    {
                        AddMissing(missing, previous, inputs.Frame, inputs.Reset, inputs.State, inputs.Event);
                        AddMissing(missing, current, inputs.Frame, inputs.Reset, inputs.State, inputs.Event);
                        continue;
                    }
                    uint currentState = current.GetUInt32(inputs.State.Handle);
                    ulong eventIdentity = current.GetUInt64(inputs.Event.Handle);
                    if (currentState != landingState ||
                        !Continuous(previous, current, inputs) ||
                        previous.GetUInt32(inputs.State.Handle) == landingState &&
                        previous.GetUInt64(inputs.Event.Handle) == eventIdentity)
                    {
                        continue;
                    }
                    int end = i;
                    while (end + 1 < rows.Length &&
                        Available(rows[end + 1], inputs.Frame, inputs.Reset, inputs.State, inputs.Event) &&
                        Continuous(rows[end], rows[end + 1], inputs) &&
                        rows[end + 1].GetUInt32(inputs.State.Handle) == landingState &&
                        rows[end + 1].GetUInt64(inputs.Event.Handle) == eventIdentity)
                    {
                        end++;
                    }
                    int terminal = end;
                    if (terminal + 1 < rows.Length &&
                        Available(rows[terminal + 1], inputs.Frame, inputs.Reset, inputs.State, inputs.Event) &&
                        Continuous(rows[terminal], rows[terminal + 1], inputs) &&
                        rows[terminal + 1].GetUInt64(inputs.Event.Handle) == eventIdentity)
                    {
                        uint terminalState = rows[terminal + 1].GetUInt32(inputs.State.Handle);
                        if (terminalState == lockedState || terminalState == releasingState)
                            terminal++;
                    }
                    DiagnosticDatasetRow[] window = rows.Skip(i).Take(terminal - i + 1).ToArray();
                    if (!Available(previous, inputs.All) || window.Any(value => !Available(value, inputs.All)))
                    {
                        AddMissing(missing, previous, inputs.All);
                        for (int rowIndex = 0; rowIndex < window.Length; rowIndex++)
                            AddMissing(missing, window[rowIndex], inputs.All);
                        i = end;
                        continue;
                    }
                    bool conditionalMissing = false;
                    for (int rowIndex = 0; rowIndex < window.Length; rowIndex++)
                    {
                        DiagnosticDatasetRow value = window[rowIndex];
                        if (value.GetBoolean(inputs.ReachAvailable.Handle) &&
                            !Available(
                                value,
                                inputs.ReachHip,
                                inputs.ReachTarget,
                                inputs.ReachLegLength,
                                inputs.ReachReserve))
                        {
                            AddMissing(
                                missing,
                                value,
                                inputs.ReachHip,
                                inputs.ReachTarget,
                                inputs.ReachLegLength,
                                inputs.ReachReserve);
                            conditionalMissing = true;
                        }
                        else if (value.GetBoolean(inputs.ReachAvailable.Handle) &&
                                 !TryReachInterval(
                                     value,
                                     inputs,
                                     out _,
                                     out _,
                                     out _))
                        {
                            missing.Add(
                                CharacterFootDiagnosticOperatorSupport.Identity(
                                    inputs.Up));
                            conditionalMissing = true;
                        }
                        if (value.GetBoolean(inputs.PelvisEvaluated.Handle) &&
                            !Available(
                                value,
                                inputs.PelvisMinimum,
                                inputs.PelvisMaximum))
                        {
                            AddMissing(
                                missing,
                                value,
                                inputs.PelvisMinimum,
                                inputs.PelvisMaximum);
                            conditionalMissing = true;
                        }
                    }
                    if (conditionalMissing)
                    {
                        i = end;
                        continue;
                    }
                    eligible++;
                    double targetPeak = window.Max(value => value.GetFloat32(
                        inputs.TargetExtension.Handle));
                    double solvedExtensionPeak = window.Max(value => value.GetFloat32(
                        inputs.SolvedExtension.Handle));
                    double solvedBendMinimum = window.Min(value => value.GetFloat32(
                        inputs.SolvedBend.Handle));
                    double bendDirectionMinimum = window.Min(value => value.GetFloat32(
                        inputs.BendDirectionDot.Handle));
                    double targetDelta = targetPeak - previous.GetFloat32(
                        inputs.TargetExtension.Handle);
                    double bendDrop = previous.GetFloat32(inputs.SolvedBend.Handle) -
                        solvedBendMinimum;
                    bool reversed = bendDirectionMinimum < 0d;
                    if (targetDelta > extensionThreshold || bendDrop > bendThreshold || reversed)
                    {
                        DiagnosticDatasetRow peak = window
                            .OrderByDescending(value => value.GetFloat32(
                                inputs.TargetExtension.Handle))
                            .First();
                        ulong sequenceStart = current.SampleKey.Sequence;
                        ulong sequenceEnd = rows[terminal].SampleKey.Sequence;
                        var evidence = new List<DiagnosticEvidence>
                        {
                            Evidence("target-extension-ratio-delta", inputs.TargetExtension, current, sequenceStart, sequenceEnd, targetDelta),
                            Evidence("solved-bend-drop-degrees", inputs.SolvedBend, current, sequenceStart, sequenceEnd, bendDrop),
                            Evidence("bend-direction-previous-dot-minimum", inputs.BendDirectionDot, current, sequenceStart, sequenceEnd, bendDirectionMinimum),
                            Evidence("solved-extension-ratio-peak", inputs.SolvedExtension, current, sequenceStart, sequenceEnd, solvedExtensionPeak),
                            Evidence("original-hip", inputs.OriginalHipPoint, peak, sequenceStart, sequenceEnd, peak.GetVector3(inputs.OriginalHipPoint.Handle)),
                            Evidence("target-ankle", inputs.TargetAnklePoint, peak, sequenceStart, sequenceEnd, peak.GetVector3(inputs.TargetAnklePoint.Handle)),
                            Evidence("solved-ankle", inputs.SolvedAnklePoint, peak, sequenceStart, sequenceEnd, peak.GetVector3(inputs.SolvedAnklePoint.Handle)),
                            Evidence("target-compression-reserve-minimum", inputs.TargetCompression, peak, sequenceStart, sequenceEnd, window.Min(value => value.GetFloat32(inputs.TargetCompression.Handle)))
                        };
                        if (peak.GetBoolean(inputs.ReachAvailable.Handle))
                        {
                            TryReachInterval(
                                peak,
                                inputs,
                                out double reachMinimum,
                                out double reachMaximum,
                                out double minimumCorrection);
                            evidence.Add(Evidence(
                                "landing-reach-minimum-correction",
                                inputs.ReachReserve,
                                peak,
                                sequenceStart,
                                sequenceEnd,
                                minimumCorrection));
                            evidence.Add(Evidence(
                                "landing-reach-minimum",
                                inputs.ReachHip,
                                peak,
                                sequenceStart,
                                sequenceEnd,
                                reachMinimum));
                            evidence.Add(Evidence(
                                "landing-reach-maximum",
                                inputs.ReachTarget,
                                peak,
                                sequenceStart,
                                sequenceEnd,
                                reachMaximum));
                        }
                        if (peak.GetBoolean(inputs.PelvisEvaluated.Handle))
                        {
                            double pelvisMinimum = peak.GetFloat32(
                                inputs.PelvisMinimum.Handle);
                            double pelvisMaximum = peak.GetFloat32(
                                inputs.PelvisMaximum.Handle);
                            evidence.Add(Evidence(
                                "pelvis-reach-conflict-gap",
                                inputs.PelvisStatus,
                                peak,
                                sequenceStart,
                                sequenceEnd,
                                Math.Max(0d, pelvisMinimum - pelvisMaximum)));
                        }
                        findings.Add(new DiagnosticFinding(
                            CharacterFootDiagnosticOperatorSupport.FindingId(
                                context.RuleId,
                                peak.SampleKey.Sequence),
                            reversed ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                            current.SampleKey.DimensionId,
                            sequenceStart,
                            sequenceEnd,
                            $"Landing event target extension changed {CharacterFootDiagnosticOperatorSupport.Format(targetDelta)}, solved bend dropped {CharacterFootDiagnosticOperatorSupport.Format(bendDrop)} degrees, bend direction reversal={reversed}, candidate compression reserve={CharacterFootDiagnosticOperatorSupport.Format(reachReserve)} m.",
                            evidence,
                            CharacterFootDiagnosticOperatorSupport.FrameRange(
                                inputs.Frame,
                                current.GetUInt64(inputs.Frame.Handle),
                                rows[terminal].GetUInt64(inputs.Frame.Handle))));
                    }
                    i = end;
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete Landing event with a preceding committed frame was observed.");
            double score = Math.Max(0d, 1d - (double)findings.Count / eligible);
            string summary =
                $"{findings.Count} of {eligible} Landing events exceeded extension or bend continuity limits.";
            return findings.Count == 0
                ? DiagnosticOperatorResult.Passed(summary, score: score)
                : DiagnosticOperatorResult.Failed(summary, null, findings, score);
        }

        static bool TryReachInterval(
            in DiagnosticDatasetRow row,
            CharacterFootLandingLegInputs inputs,
            out double minimum,
            out double maximum,
            out double correction)
        {
            DiagnosticVector3 up = row.GetVector3(inputs.Up.Handle);
            if (!CharacterFootDiagnosticOperatorSupport.TryNormalize(
                    up,
                    out double upX,
                    out double upY,
                    out double upZ))
            {
                minimum = 0d;
                maximum = 0d;
                correction = 0d;
                return false;
            }
            DiagnosticVector3 hip = row.GetVector3(inputs.ReachHip.Handle);
            DiagnosticVector3 ankle = row.GetVector3(inputs.ReachTarget.Handle);
            double legLength = row.GetFloat32(inputs.ReachLegLength.Handle);
            double reserve = row.GetFloat32(inputs.ReachReserve.Handle);
            if (legLength <= reserve || reserve <= 0d)
            {
                minimum = 0d;
                maximum = 0d;
                correction = 0d;
                return false;
            }
            double x = hip.X - ankle.X;
            double y = hip.Y - ankle.Y;
            double z = hip.Z - ankle.Z;
            double vertical = x * upX + y * upY + z * upZ;
            double horizontalX = x - upX * vertical;
            double horizontalY = y - upY * vertical;
            double horizontalZ = z - upZ * vertical;
            double usable = legLength - reserve;
            double verticalReachSquare = usable * usable -
                (horizontalX * horizontalX +
                 horizontalY * horizontalY +
                 horizontalZ * horizontalZ);
            if (verticalReachSquare < 0d)
            {
                minimum = 0d;
                maximum = 0d;
                correction = 0d;
                return true;
            }
            double verticalReach = Math.Sqrt(verticalReachSquare);
            minimum = -vertical - verticalReach;
            maximum = -vertical + verticalReach;
            correction = minimum > 0d
                ? minimum
                : maximum < 0d
                    ? -maximum
                    : 0d;
            return double.IsFinite(minimum) &&
                double.IsFinite(maximum) &&
                double.IsFinite(correction);
        }

        static DiagnosticOperatorParameter[] Parameters() => new[]
        {
            Integer(LandingState),
            Integer(LockedState),
            Integer(ReleasingState),
            Number(ExtensionThreshold),
            Number(BendDropThreshold),
            Number(ReachReserve)
        };

        static DiagnosticOperatorParameter Integer(string id) =>
            new DiagnosticOperatorParameter(id, DiagnosticOperatorParameterKind.Integer, true);

        static DiagnosticOperatorParameter Number(string id) =>
            new DiagnosticOperatorParameter(id, DiagnosticOperatorParameterKind.Number, true, 0d);

        static IEnumerable<DiagnosticDatasetRow> Rows(
            DiagnosticOperatorExecutionContext context,
            DiagnosticBoundInput input)
        {
            DiagnosticDatasetCursor cursor = input.Dataset.Main.CreateCursor();
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (context.IncludesDimension(row.SampleKey.DimensionId) &&
                    context.MatchesFilters(row))
                {
                    yield return row;
                }
            }
        }

        static bool Continuous(
            in DiagnosticDatasetRow previous,
            in DiagnosticDatasetRow current,
            CharacterFootLandingLegInputs inputs) =>
            current.GetUInt64(inputs.Frame.Handle) ==
                previous.GetUInt64(inputs.Frame.Handle) + 1 &&
            current.GetUInt64(inputs.Reset.Handle) ==
                previous.GetUInt64(inputs.Reset.Handle);

        static bool Available(
            in DiagnosticDatasetRow row,
            params DiagnosticBoundInput[] inputs) => Available(
                row,
                (IReadOnlyList<DiagnosticBoundInput>)inputs);

        static bool Available(
            in DiagnosticDatasetRow row,
            IReadOnlyList<DiagnosticBoundInput> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
                if (!row.IsAvailable(inputs[i].Handle))
                    return false;
            return true;
        }

        static void AddMissing(
            ISet<string> missing,
            in DiagnosticDatasetRow row,
            params DiagnosticBoundInput[] inputs) => AddMissing(
                missing,
                row,
                (IReadOnlyList<DiagnosticBoundInput>)inputs);

        static void AddMissing(
            ISet<string> missing,
            in DiagnosticDatasetRow row,
            IReadOnlyList<DiagnosticBoundInput> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
                if (!row.IsAvailable(inputs[i].Handle))
                    missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
        }

        static DiagnosticEvidence Evidence(
            string id,
            in DiagnosticBoundInput input,
            in DiagnosticDatasetRow row,
            ulong start,
            ulong end,
            double value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                row.SampleKey.DimensionId,
                start,
                end,
                CharacterFootDiagnosticOperatorSupport.Format(value));

        static DiagnosticEvidence Evidence(
            string id,
            in DiagnosticBoundInput input,
            in DiagnosticDatasetRow row,
            ulong start,
            ulong end,
            in DiagnosticVector3 value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                row.SampleKey.DimensionId,
                start,
                end,
                CharacterFootDiagnosticOperatorSupport.Format(value));
    }

    internal sealed class CharacterFootSameLevelPelvisInputs
    {
        internal const string FrameSequence = "frame-sequence";
        internal const string ResetSequence = "reset-sequence";
        internal const string DeltaSeconds = "delta-seconds";
        internal const string ComponentUp = "component-up";
        internal const string SourceAnklePosition = "source-ankle-position";
        internal const string SourceAnkleRotation = "source-ankle-rotation";
        internal const string SourceSole = "source-sole";
        internal const string PhysicalWriteAvailable = "physical-write-available";
        internal const string PhysicalAnklePosition = "physical-ankle-position";
        internal const string PhysicalAnkleRotation = "physical-ankle-rotation";
        internal const string PhysicalPelvisAvailable = "physical-pelvis-available";
        internal const string PhysicalPelvisPosition = "physical-pelvis-position";
        internal const string PoseInputAvailable = "pose-input-available";
        internal const string AnimatedPelvisPosition = "animated-pelvis-position";
        internal const string PoseRootPosition = "pose-root-position";
        internal const string ResolvedTargetSole = "resolved-target-sole";
        internal const string AnimatedSole = "animated-sole";
        internal const string ResponseEvaluated = "response-evaluated";
        internal const string ResponsePreviousOutput = "response-previous-output";
        internal const string ResponseTarget = "response-target";
        internal const string ResponseOutput = "response-output";
        internal const string HeightTargetAvailable = "height-target-available";
        internal const string RequestedHeightOffset = "requested-height-offset";
        internal const string PostureAvailable = "posture-available";
        internal const string PostureOffset = "posture-offset";
        internal const string ConstraintState = "constraint-state";
        internal const string SameLevelLimitApplied = "same-level-limit-applied";
        internal const string SupportChanged = "support-changed";
        internal const string ReachIntersectionEvaluated = "reach-intersection-evaluated";
        internal const string ReachIntersectionMinimum = "reach-intersection-minimum";
        internal const string ReachIntersectionMaximum = "reach-intersection-maximum";

        internal CharacterFootSameLevelPelvisInputs(DiagnosticOperatorExecutionContext context)
        {
            Frame = context.Input(FrameSequence);
            Reset = context.Input(ResetSequence);
            Delta = context.Input(DeltaSeconds);
            Up = context.Input(ComponentUp);
            SourceAnkle = context.Input(SourceAnklePosition);
            SourceRotation = context.Input(SourceAnkleRotation);
            SourceSolePoint = context.Input(SourceSole);
            PhysicalAvailable = context.Input(PhysicalWriteAvailable);
            PhysicalAnkle = context.Input(PhysicalAnklePosition);
            PhysicalRotation = context.Input(PhysicalAnkleRotation);
            PhysicalPelvisIsAvailable = context.Input(PhysicalPelvisAvailable);
            PhysicalPelvis = context.Input(PhysicalPelvisPosition);
            PoseAvailable = context.Input(PoseInputAvailable);
            AnimatedPelvis = context.Input(AnimatedPelvisPosition);
            PoseRoot = context.Input(PoseRootPosition);
            ResolvedSole = context.Input(ResolvedTargetSole);
            OriginalSole = context.Input(AnimatedSole);
            ResponseIsEvaluated = context.Input(ResponseEvaluated);
            PreviousOutput = context.Input(ResponsePreviousOutput);
            Target = context.Input(ResponseTarget);
            Output = context.Input(ResponseOutput);
            HeightAvailable = context.Input(HeightTargetAvailable);
            HeightOffset = context.Input(RequestedHeightOffset);
            PostureIsAvailable = context.Input(PostureAvailable);
            Posture = context.Input(PostureOffset);
            State = context.Input(ConstraintState);
            LimitApplied = context.Input(SameLevelLimitApplied);
            ChangedSupport = context.Input(SupportChanged);
            ReachEvaluated = context.Input(ReachIntersectionEvaluated);
            ReachMinimum = context.Input(ReachIntersectionMinimum);
            ReachMaximum = context.Input(ReachIntersectionMaximum);
            All = new[]
            {
                Frame,
                Reset,
                PhysicalAvailable,
                PhysicalPelvisIsAvailable,
                PoseAvailable,
                ResponseIsEvaluated
            };
            Measurements = new[]
            {
                Delta,
                Up,
                SourceAnkle,
                SourceRotation,
                SourceSolePoint,
                PhysicalAnkle,
                PhysicalRotation,
                PhysicalPelvis,
                AnimatedPelvis,
                PoseRoot,
                ResolvedSole,
                OriginalSole,
                PreviousOutput,
                Target,
                Output,
                HeightAvailable,
                PostureIsAvailable,
                State,
                LimitApplied,
                ChangedSupport,
                ReachEvaluated
            };
        }

        internal DiagnosticBoundInput Frame { get; }
        internal DiagnosticBoundInput Reset { get; }
        internal DiagnosticBoundInput Delta { get; }
        internal DiagnosticBoundInput Up { get; }
        internal DiagnosticBoundInput SourceAnkle { get; }
        internal DiagnosticBoundInput SourceRotation { get; }
        internal DiagnosticBoundInput SourceSolePoint { get; }
        internal DiagnosticBoundInput PhysicalAvailable { get; }
        internal DiagnosticBoundInput PhysicalAnkle { get; }
        internal DiagnosticBoundInput PhysicalRotation { get; }
        internal DiagnosticBoundInput PhysicalPelvisIsAvailable { get; }
        internal DiagnosticBoundInput PhysicalPelvis { get; }
        internal DiagnosticBoundInput PoseAvailable { get; }
        internal DiagnosticBoundInput AnimatedPelvis { get; }
        internal DiagnosticBoundInput PoseRoot { get; }
        internal DiagnosticBoundInput ResolvedSole { get; }
        internal DiagnosticBoundInput OriginalSole { get; }
        internal DiagnosticBoundInput ResponseIsEvaluated { get; }
        internal DiagnosticBoundInput PreviousOutput { get; }
        internal DiagnosticBoundInput Target { get; }
        internal DiagnosticBoundInput Output { get; }
        internal DiagnosticBoundInput HeightAvailable { get; }
        internal DiagnosticBoundInput HeightOffset { get; }
        internal DiagnosticBoundInput PostureIsAvailable { get; }
        internal DiagnosticBoundInput Posture { get; }
        internal DiagnosticBoundInput State { get; }
        internal DiagnosticBoundInput LimitApplied { get; }
        internal DiagnosticBoundInput ChangedSupport { get; }
        internal DiagnosticBoundInput ReachEvaluated { get; }
        internal DiagnosticBoundInput ReachMinimum { get; }
        internal DiagnosticBoundInput ReachMaximum { get; }
        internal IReadOnlyList<DiagnosticBoundInput> All { get; }
        internal IReadOnlyList<DiagnosticBoundInput> Measurements { get; }

        internal static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            Slot(FrameSequence, DiagnosticValueKind.UInt64),
            Slot(ResetSequence, DiagnosticValueKind.UInt64),
            Slot(DeltaSeconds, DiagnosticValueKind.Float32),
            Slot(ComponentUp, DiagnosticValueKind.Vector3),
            Slot(SourceAnklePosition, DiagnosticValueKind.Vector3),
            Slot(SourceAnkleRotation, DiagnosticValueKind.Quaternion),
            Slot(SourceSole, DiagnosticValueKind.Vector3),
            Slot(PhysicalWriteAvailable, DiagnosticValueKind.Boolean),
            Slot(PhysicalAnklePosition, DiagnosticValueKind.Vector3),
            Slot(PhysicalAnkleRotation, DiagnosticValueKind.Quaternion),
            Slot(PhysicalPelvisAvailable, DiagnosticValueKind.Boolean),
            Slot(PhysicalPelvisPosition, DiagnosticValueKind.Vector3),
            Slot(PoseInputAvailable, DiagnosticValueKind.Boolean),
            Slot(AnimatedPelvisPosition, DiagnosticValueKind.Vector3),
            Slot(PoseRootPosition, DiagnosticValueKind.Vector3),
            Slot(ResolvedTargetSole, DiagnosticValueKind.Vector3),
            Slot(AnimatedSole, DiagnosticValueKind.Vector3),
            Slot(ResponseEvaluated, DiagnosticValueKind.Boolean),
            Slot(ResponsePreviousOutput, DiagnosticValueKind.Float32),
            Slot(ResponseTarget, DiagnosticValueKind.Float32),
            Slot(ResponseOutput, DiagnosticValueKind.Float32),
            Slot(HeightTargetAvailable, DiagnosticValueKind.Boolean),
            Slot(RequestedHeightOffset, DiagnosticValueKind.Float32),
            Slot(PostureAvailable, DiagnosticValueKind.Boolean),
            Slot(PostureOffset, DiagnosticValueKind.Float32),
            Slot(ConstraintState, DiagnosticValueKind.UInt32),
            Slot(SameLevelLimitApplied, DiagnosticValueKind.Boolean),
            Slot(SupportChanged, DiagnosticValueKind.Boolean),
            Slot(ReachIntersectionEvaluated, DiagnosticValueKind.Boolean),
            Slot(ReachIntersectionMinimum, DiagnosticValueKind.Float32),
            Slot(ReachIntersectionMaximum, DiagnosticValueKind.Float32)
        };

        static DiagnosticOperatorInputSlot Slot(string id, DiagnosticValueKind kind) =>
            new DiagnosticOperatorInputSlot(id, kind, DiagnosticDatasetCardinality.Main, true);
    }

    internal sealed class CharacterFootSameLevelFeetPelvisDescentOperator :
        IDiagnosticAnalysisOperator
    {
        const string SameLevelThreshold = "same-level-height-tolerance-meters";
        const string DownwardThreshold = "downward-step-threshold-meters";
        const string NoiseFloor = "position-noise-floor-meters";
        const string ReleasingState = "releasing-state";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/same-level-feet-pelvis-descent",
                "pelvis",
                CharacterFootSameLevelPelvisInputs.Slots(),
                Parameters());

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            double sameLevelThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                SameLevelThreshold);
            double downwardThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                DownwardThreshold);
            double noiseFloor = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                NoiseFloor);
            uint releasingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                ReleasingState);
            var inputs = new CharacterFootSameLevelPelvisInputs(context);
            string[] dimensions = context.Dimensions.Count == 2
                ? context.Dimensions.ToArray()
                : inputs.Frame.Dataset.Schema.Dimensions
                    .Select(value => value.Id)
                    .Take(2)
                    .ToArray();
            if (dimensions.Length != 2)
                return DiagnosticOperatorResult.NotApplicable(
                    "Same-level pelvis analysis requires exactly two dimensions.");
            Dictionary<ulong, Dictionary<string, DiagnosticDatasetRow>> frames =
                ReadFrames(context, inputs, dimensions);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            ulong[] frameIds = frames.Keys.OrderBy(value => value).ToArray();
            int eligible = 0;
            for (int i = 1; i < frameIds.Length; i++)
            {
                if (frameIds[i] != frameIds[i - 1] + 1 ||
                    !TryPair(frames[frameIds[i - 1]], dimensions, out DiagnosticDatasetRow previousLeft, out DiagnosticDatasetRow previousRight) ||
                    !TryPair(frames[frameIds[i]], dimensions, out DiagnosticDatasetRow currentLeft, out DiagnosticDatasetRow currentRight))
                {
                    continue;
                }
                if (!Available(previousLeft, inputs.All) ||
                    !Available(previousRight, inputs.All) ||
                    !Available(currentLeft, inputs.All) ||
                    !Available(currentRight, inputs.All))
                {
                    AddMissing(missing, previousLeft, inputs.All);
                    AddMissing(missing, previousRight, inputs.All);
                    AddMissing(missing, currentLeft, inputs.All);
                    AddMissing(missing, currentRight, inputs.All);
                    continue;
                }
                if (currentLeft.GetUInt64(inputs.Reset.Handle) !=
                        previousLeft.GetUInt64(inputs.Reset.Handle) ||
                    currentRight.GetUInt64(inputs.Reset.Handle) !=
                        previousRight.GetUInt64(inputs.Reset.Handle) ||
                    !previousLeft.GetBoolean(inputs.PhysicalAvailable.Handle) ||
                    !previousRight.GetBoolean(inputs.PhysicalAvailable.Handle) ||
                    !currentLeft.GetBoolean(inputs.PhysicalAvailable.Handle) ||
                    !currentRight.GetBoolean(inputs.PhysicalAvailable.Handle) ||
                    !previousLeft.GetBoolean(inputs.PhysicalPelvisIsAvailable.Handle) ||
                    !currentLeft.GetBoolean(inputs.PhysicalPelvisIsAvailable.Handle) ||
                    !previousLeft.GetBoolean(inputs.PoseAvailable.Handle) ||
                    !currentLeft.GetBoolean(inputs.PoseAvailable.Handle) ||
                    !previousLeft.GetBoolean(inputs.ResponseIsEvaluated.Handle) ||
                    !currentLeft.GetBoolean(inputs.ResponseIsEvaluated.Handle))
                {
                    continue;
                }
                DiagnosticDatasetRow[] evidenceRows =
                {
                    previousLeft,
                    previousRight,
                    currentLeft,
                    currentRight
                };
                bool measurementMissing = false;
                for (int rowIndex = 0; rowIndex < evidenceRows.Length; rowIndex++)
                {
                    DiagnosticDatasetRow row = evidenceRows[rowIndex];
                    if (!Available(row, inputs.Measurements))
                    {
                        AddMissing(missing, row, inputs.Measurements);
                        measurementMissing = true;
                    }
                    if (row.IsAvailable(inputs.HeightAvailable.Handle) &&
                        row.GetBoolean(inputs.HeightAvailable.Handle) &&
                        !row.IsAvailable(inputs.HeightOffset.Handle))
                    {
                        AddMissing(missing, row, inputs.HeightOffset);
                        measurementMissing = true;
                    }
                    if (row.IsAvailable(inputs.PostureIsAvailable.Handle) &&
                        row.GetBoolean(inputs.PostureIsAvailable.Handle) &&
                        !row.IsAvailable(inputs.Posture.Handle))
                    {
                        AddMissing(missing, row, inputs.Posture);
                        measurementMissing = true;
                    }
                    if (row.IsAvailable(inputs.ReachEvaluated.Handle) &&
                        row.GetBoolean(inputs.ReachEvaluated.Handle) &&
                        !Available(row, inputs.ReachMinimum, inputs.ReachMaximum))
                    {
                        AddMissing(missing, row, inputs.ReachMinimum, inputs.ReachMaximum);
                        measurementMissing = true;
                    }
                }
                if (measurementMissing)
                    continue;
                DiagnosticVector3 up = currentLeft.GetVector3(inputs.Up.Handle);
                if (!CharacterFootDiagnosticOperatorSupport.TryNormalize(
                        up,
                        out double upX,
                        out double upY,
                        out double upZ))
                {
                    missing.Add("invalid-component-up");
                    continue;
                }
                if (!TryPhysicalSole(currentLeft, inputs, out DiagnosticVector3 leftSole) ||
                    !TryPhysicalSole(currentRight, inputs, out DiagnosticVector3 rightSole))
                {
                    missing.Add("invalid-physical-sole-rigid-transform");
                    continue;
                }
                double physicalSpread = Math.Abs(Project(leftSole, upX, upY, upZ) -
                    Project(rightSole, upX, upY, upZ));
                if (physicalSpread > sameLevelThreshold)
                    continue;
                eligible++;
                DiagnosticVector3 previousPhysicalPelvis = previousLeft.GetVector3(
                    inputs.PhysicalPelvis.Handle);
                DiagnosticVector3 currentPhysicalPelvis = currentLeft.GetVector3(
                    inputs.PhysicalPelvis.Handle);
                double physicalStep = ProjectDelta(
                    currentPhysicalPelvis,
                    previousPhysicalPelvis,
                    upX,
                    upY,
                    upZ);
                double downwardStep = Math.Max(0d, -physicalStep);
                if (downwardStep <= downwardThreshold)
                    continue;
                double responseStep = currentLeft.GetFloat32(inputs.Output.Handle) -
                    previousLeft.GetFloat32(inputs.Output.Handle);
                double animatedStep = ProjectDelta(
                    currentLeft.GetVector3(inputs.AnimatedPelvis.Handle),
                    previousLeft.GetVector3(inputs.AnimatedPelvis.Handle),
                    upX,
                    upY,
                    upZ);
                double rootStep = ProjectDelta(
                    currentLeft.GetVector3(inputs.PoseRoot.Handle),
                    previousLeft.GetVector3(inputs.PoseRoot.Handle),
                    upX,
                    upY,
                    upZ);
                string cause = Cause(
                    currentLeft,
                    inputs,
                    releasingState,
                    noiseFloor,
                    responseStep,
                    animatedStep);
                ulong sequenceStart = Math.Min(
                    previousLeft.SampleKey.Sequence,
                    previousRight.SampleKey.Sequence);
                ulong sequenceEnd = Math.Max(
                    currentLeft.SampleKey.Sequence,
                    currentRight.SampleKey.Sequence);
                var evidence = new List<DiagnosticEvidence>
                {
                    Evidence("physical-foot-height-spread-meters", inputs.PhysicalAnkle, currentLeft, sequenceStart, sequenceEnd, physicalSpread),
                    Evidence("physical-pelvis-downward-step-meters", inputs.PhysicalPelvis, currentLeft, sequenceStart, sequenceEnd, downwardStep),
                    Evidence("pelvis-response-step-meters", inputs.Output, currentLeft, sequenceStart, sequenceEnd, responseStep),
                    Evidence("animated-pelvis-step-meters", inputs.AnimatedPelvis, currentLeft, sequenceStart, sequenceEnd, animatedStep),
                    Evidence("pose-root-step-meters", inputs.PoseRoot, currentLeft, sequenceStart, sequenceEnd, rootStep),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "primary-cause",
                        inputs.Output,
                        "both",
                        sequenceStart,
                        sequenceEnd,
                        cause)
                };
                if (currentLeft.GetBoolean(inputs.ReachEvaluated.Handle))
                {
                    evidence.Add(Evidence(
                        "reach-intersection-minimum",
                        inputs.ReachMinimum,
                        currentLeft,
                        sequenceStart,
                        sequenceEnd,
                        currentLeft.GetFloat32(inputs.ReachMinimum.Handle)));
                    evidence.Add(Evidence(
                        "reach-intersection-maximum",
                        inputs.ReachMaximum,
                        currentLeft,
                        sequenceStart,
                        sequenceEnd,
                        currentLeft.GetFloat32(inputs.ReachMaximum.Handle)));
                }
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        currentLeft.SampleKey.Sequence),
                    DiagnosticSeverity.Information,
                    "both",
                    sequenceStart,
                    sequenceEnd,
                    $"Physical pelvis descended {CharacterFootDiagnosticOperatorSupport.Format(downwardStep)} m while final physical feet differed by {CharacterFootDiagnosticOperatorSupport.Format(physicalSpread)} m; primary cause={cause}.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        inputs.Frame,
                        frameIds[i - 1],
                        frameIds[i])));
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No continuous frame pair with complete same-level physical feet and pelvis evidence was observed.");
            string summary =
                $"{findings.Count} of {eligible} same-level physical-foot frame pairs exceeded the pelvis downward-step limit.";
            return new DiagnosticOperatorResult(
                findings.Count == 0 ? DiagnosticRuleState.Passed : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static DiagnosticOperatorParameter[] Parameters() => new[]
        {
            Number(SameLevelThreshold),
            Number(DownwardThreshold),
            Number(NoiseFloor),
            new DiagnosticOperatorParameter(
                ReleasingState,
                DiagnosticOperatorParameterKind.Integer,
                true)
        };

        static DiagnosticOperatorParameter Number(string id) =>
            new DiagnosticOperatorParameter(id, DiagnosticOperatorParameterKind.Number, true, 0d);

        static Dictionary<ulong, Dictionary<string, DiagnosticDatasetRow>> ReadFrames(
            DiagnosticOperatorExecutionContext context,
            CharacterFootSameLevelPelvisInputs inputs,
            IReadOnlyList<string> dimensions)
        {
            var selected = new HashSet<string>(dimensions, StringComparer.Ordinal);
            var result = new Dictionary<ulong, Dictionary<string, DiagnosticDatasetRow>>();
            DiagnosticDatasetCursor cursor = inputs.Frame.Dataset.Main.CreateCursor();
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (!selected.Contains(row.SampleKey.DimensionId) ||
                    !context.IncludesDimension(row.SampleKey.DimensionId) ||
                    !context.MatchesFilters(row) ||
                    !row.IsAvailable(inputs.Frame.Handle))
                {
                    continue;
                }
                ulong frame = row.GetUInt64(inputs.Frame.Handle);
                if (!result.TryGetValue(frame, out Dictionary<string, DiagnosticDatasetRow> pair))
                {
                    pair = new Dictionary<string, DiagnosticDatasetRow>(StringComparer.Ordinal);
                    result.Add(frame, pair);
                }
                pair[row.SampleKey.DimensionId] = row;
            }
            return result;
        }

        static bool TryPair(
            IReadOnlyDictionary<string, DiagnosticDatasetRow> pair,
            IReadOnlyList<string> dimensions,
            out DiagnosticDatasetRow left,
            out DiagnosticDatasetRow right)
        {
            bool hasLeft = pair.TryGetValue(dimensions[0], out left);
            bool hasRight = pair.TryGetValue(dimensions[1], out right);
            return hasLeft && hasRight;
        }

        static bool TryPhysicalSole(
            in DiagnosticDatasetRow row,
            CharacterFootSameLevelPelvisInputs inputs,
            out DiagnosticVector3 sole) =>
            CharacterFootDiagnosticOperatorSupport.TryRebuildPhysicalProbe(
                row.GetVector3(inputs.SourceAnkle.Handle),
                row.GetQuaternion(inputs.SourceRotation.Handle),
                row.GetVector3(inputs.SourceSolePoint.Handle),
                row.GetVector3(inputs.PhysicalAnkle.Handle),
                row.GetQuaternion(inputs.PhysicalRotation.Handle),
                out sole);

        static string Cause(
            in DiagnosticDatasetRow row,
            CharacterFootSameLevelPelvisInputs inputs,
            uint releasingState,
            double noiseFloor,
            double responseStep,
            double animatedStep)
        {
            bool animation = animatedStep < -noiseFloor;
            if (row.GetBoolean(inputs.LimitApplied.Handle))
                return animation
                    ? "same-level-world-down-limit-and-animation-motion"
                    : "same-level-world-down-limit";
            if (row.GetBoolean(inputs.HeightAvailable.Handle) &&
                row.GetFloat32(inputs.HeightOffset.Handle) < -noiseFloor)
            {
                return animation
                    ? "common-height-request-and-animation-motion"
                    : "common-height-request";
            }
            if (row.GetUInt32(inputs.State.Handle) == releasingState &&
                Math.Abs(row.GetFloat32(inputs.Target.Handle)) <= noiseFloor &&
                responseStep < -noiseFloor)
            {
                return animation
                    ? "release-to-animation-and-animation-motion"
                    : "release-to-animation";
            }
            if (responseStep < -noiseFloor &&
                row.GetFloat32(inputs.Target.Handle) >=
                    row.GetFloat32(inputs.PreviousOutput.Handle) - noiseFloor)
            {
                return animation
                    ? "response-history-and-animation-motion"
                    : "response-history";
            }
            if (animation)
                return "animation-or-root-motion";
            if (responseStep < -noiseFloor)
                return "pelvis-response";
            return "unavailable";
        }

        static double Project(
            in DiagnosticVector3 value,
            double x,
            double y,
            double z) => value.X * x + value.Y * y + value.Z * z;

        static double ProjectDelta(
            in DiagnosticVector3 current,
            in DiagnosticVector3 previous,
            double x,
            double y,
            double z) =>
            (current.X - previous.X) * x +
            (current.Y - previous.Y) * y +
            (current.Z - previous.Z) * z;

        static bool Available(
            in DiagnosticDatasetRow row,
            IReadOnlyList<DiagnosticBoundInput> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
                if (!row.IsAvailable(inputs[i].Handle))
                    return false;
            return true;
        }

        static bool Available(
            in DiagnosticDatasetRow row,
            params DiagnosticBoundInput[] inputs) => Available(
                row,
                (IReadOnlyList<DiagnosticBoundInput>)inputs);

        static void AddMissing(
            ISet<string> missing,
            in DiagnosticDatasetRow row,
            IReadOnlyList<DiagnosticBoundInput> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
                if (!row.IsAvailable(inputs[i].Handle))
                    missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
        }

        static void AddMissing(
            ISet<string> missing,
            in DiagnosticDatasetRow row,
            params DiagnosticBoundInput[] inputs) => AddMissing(
                missing,
                row,
                (IReadOnlyList<DiagnosticBoundInput>)inputs);

        static DiagnosticEvidence Evidence(
            string id,
            in DiagnosticBoundInput input,
            in DiagnosticDatasetRow row,
            ulong start,
            ulong end,
            double value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                row.SampleKey.DimensionId,
                start,
                end,
                CharacterFootDiagnosticOperatorSupport.Format(value));
    }
}
