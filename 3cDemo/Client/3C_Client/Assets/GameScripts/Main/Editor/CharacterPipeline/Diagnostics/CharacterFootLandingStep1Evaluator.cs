using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootLandingStep1Report
    {
        internal CharacterFootLandingStep1Report(
            bool passed,
            string csvPath,
            int uniqueFrames,
            int dualWeightFrames,
            int singleSwingFrames,
            int visibleLiftFrames,
            float maxPelvisWeight,
            float maxNegativeCorrection,
            string summary)
        {
            Passed = passed;
            CsvPath = csvPath;
            UniqueFrames = uniqueFrames;
            DualWeightFrames = dualWeightFrames;
            SingleSwingFrames = singleSwingFrames;
            VisibleLiftFrames = visibleLiftFrames;
            MaxPelvisWeight = maxPelvisWeight;
            MaxNegativeCorrection = maxNegativeCorrection;
            Summary = summary;
        }

        internal bool Passed { get; }
        internal string CsvPath { get; }
        internal int UniqueFrames { get; }
        internal int DualWeightFrames { get; }
        internal int SingleSwingFrames { get; }
        internal int VisibleLiftFrames { get; }
        internal float MaxPelvisWeight { get; }
        internal float MaxNegativeCorrection { get; }
        internal string Summary { get; }
    }

    internal static class CharacterFootLandingStep1Evaluator
    {
        const float ResidualTolerance = 0.02f;
        const float MaximumPelvisGoalStep = 0.15f;
        const float MaximumPhysicalPelvisStep = 0.3f;

        internal static CharacterFootLandingStep1Report Evaluate(string csvPath)
        {
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
                return Fail(csvPath, "CSV 不存在。");

            var frames = new Dictionary<(int frame, ulong completion, string root), FramePair>();
            var roots = new HashSet<string>(StringComparer.Ordinal);
            var targetRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
            var targetHostIds = new HashSet<int>();
            int expandedConsistencyFailures = 0;
            bool hasTargetIdentity;
            using (var reader = new StreamReader(csvPath))
            {
                string header = reader.ReadLine();
                if (string.IsNullOrEmpty(header))
                    return Fail(csvPath, "CSV 为空。");
                string[] names = header.Split(',');
                int iFrame = Index(names, "FrameSequence");
                int iCompletion = Index(names, "CompletionIdentity");
                int iTargetRuntime = Index(names, "TargetRuntimeInstanceId");
                int iTargetHost = Index(names, "TargetHostInstanceId");
                int iRoot = Index(names, "RootInstanceId");
                int iSide = Index(names, "Side");
                int iWeight = Index(names, "FinalGoalPositionWeight");
                int iPelvisWeight = Index(names, "PelvisPositionWeight");
                int iVertical = Index(names, "FootMotionVerticalCorrection");
                int iIkResidual = Index(names, "FinalIkPositionResidual");
                int iIkSucceeded = Index(names, "FinalIkSucceeded");
                int iAppliedGoalCount = Index(names, "FinalIkAppliedGoalCount");
                int iIkPelvisAvailable = Index(names, "FinalIkPelvisAvailable");
                int iPhysicalWriteAvailable = Index(names, "FinalPhysicalWriteAvailable");
                int iPhysicalWriteCompletion = Index(names, "FinalPhysicalWriteCompletionIdentity");
                int iPelvisResidual = Index(names, "FinalPhysicalPelvisGoalResidual");
                int iAnkleResidual = Index(names, "FinalPhysicalAnkleGoalResidual");
                int iStrideState = Index(names, "StrideState");
                int iStrideReject = Index(names, "StrideRejectReason");
                int iPelvisGoalY = Index(names, "FinalPelvisGoalY");
                int iPhysicalPelvisY = Index(names, "FinalPhysicalPelvisComponentPositionY");
                int iBodyReset = Index(names, "BodyResetSequence");
                int iLandingEvent = Index(names, "LandingEventIdentity");
                int iLastLandingEvent = Index(names, "GroundPathLastLandingEventIdentity");
                int iNextLandingEvent = Index(names, "GroundPathNextSwingLandingEventIdentity");
                int requiredMaximum = Maximum(
                    iFrame, iCompletion, iRoot, iSide, iWeight, iPelvisWeight, iVertical,
                    iIkSucceeded, iAppliedGoalCount, iIkPelvisAvailable,
                    iPhysicalWriteAvailable, iPhysicalWriteCompletion,
                    iPelvisResidual, iAnkleResidual, iStrideState, iStrideReject,
                    iPelvisGoalY, iPhysicalPelvisY, iBodyReset,
                    iLandingEvent, iLastLandingEvent, iNextLandingEvent);
                if (requiredMaximum < 0)
                    return Fail(csvPath, "CSV 缺第 2 步所需列。");
                hasTargetIdentity = iTargetRuntime >= 0 && iTargetHost >= 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] cells = line.Split(',');
                    if (cells.Length <= requiredMaximum)
                        continue;
                    int frame = ParseInt(cells[iFrame]);
                    ulong completion = ParseUlong(cells[iCompletion]);
                    string root = cells[iRoot];
                    string side = cells[iSide];
                    roots.Add(root);
                    if (hasTargetIdentity)
                    {
                        targetRuntimeIds.Add(cells[iTargetRuntime]);
                        targetHostIds.Add(ParseInt(cells[iTargetHost]));
                    }
                    var row = new FootRow(
                        ParseFloat(cells[iWeight]),
                        ParseFloat(cells[iVertical]),
                        iIkResidual >= 0 ? ParseFloat(cells[iIkResidual]) : 0f,
                        ParseFloat(cells[iPelvisWeight]),
                        ParseInt(cells[iIkSucceeded]) != 0,
                        ParseInt(cells[iAppliedGoalCount]),
                        ParseInt(cells[iIkPelvisAvailable]) != 0,
                        ParseInt(cells[iPhysicalWriteAvailable]) != 0,
                        completion,
                        ParseUlong(cells[iPhysicalWriteCompletion]),
                        ParseFloat(cells[iPelvisResidual]),
                        ParseFloat(cells[iAnkleResidual]),
                        cells[iStrideState],
                        cells[iStrideReject],
                        ParseFloat(cells[iPelvisGoalY]),
                        ParseFloat(cells[iPhysicalPelvisY]),
                        ParseInt(cells[iBodyReset]),
                        ParseUlong(cells[iLandingEvent]),
                        ParseUlong(cells[iLastLandingEvent]),
                        ParseUlong(cells[iNextLandingEvent]));
                    var key = (frame, completion, root);
                    if (!frames.TryGetValue(key, out FramePair pair))
                        pair = new FramePair();
                    if (side == "Left")
                    {
                        if (pair.Left.HasValue && !pair.Left.Value.SameSample(in row))
                            expandedConsistencyFailures++;
                        pair.Left = row;
                    }
                    else if (side == "Right")
                    {
                        if (pair.Right.HasValue && !pair.Right.Value.SameSample(in row))
                            expandedConsistencyFailures++;
                        pair.Right = row;
                    }
                    frames[key] = pair;
                }
            }

            string selectedRoot = string.Empty;
            int activeRootCount = 0;
            foreach (string root in roots)
            {
                bool active = false;
                foreach (KeyValuePair<(int frame, ulong completion, string root), FramePair> entry in frames)
                {
                    if (!string.Equals(entry.Key.root, root, StringComparison.Ordinal) ||
                        !entry.Value.Left.HasValue || !entry.Value.Right.HasValue)
                        continue;
                    FootRow left = entry.Value.Left.Value;
                    FootRow right = entry.Value.Right.Value;
                    if (left.Weight > 0.01f || right.Weight > 0.01f ||
                        left.PelvisWeight > 0.01f || right.PelvisWeight > 0.01f)
                    {
                        active = true;
                        break;
                    }
                }
                if (!active)
                    continue;
                selectedRoot = root;
                activeRootCount++;
            }
            if (activeRootCount != 1)
                return Fail(csvPath, $"有效采样目标数量错误：{activeRootCount}。");

            var ordered = new List<FrameSample>();
            foreach (KeyValuePair<(int frame, ulong completion, string root), FramePair> entry in frames)
            {
                if (!string.Equals(entry.Key.root, selectedRoot, StringComparison.Ordinal) ||
                    !entry.Value.Left.HasValue || !entry.Value.Right.HasValue)
                    continue;
                ordered.Add(new FrameSample(
                    entry.Key.frame,
                    entry.Key.completion,
                    entry.Value.Left.Value,
                    entry.Value.Right.Value));
            }
            ordered.Sort((left, right) =>
            {
                int frame = left.Frame.CompareTo(right.Frame);
                return frame != 0 ? frame : left.Completion.CompareTo(right.Completion);
            });

            int dual = 0;
            int single = 0;
            int visible = 0;
            float maxPelvis = 0f;
            float maxNeg = 0f;
            int activeGoalRows = 0;
            int closedGoalRows = 0;
            int pelvisGoalFrames = 0;
            int closedPelvisFrames = 0;
            int closureFailures = 0;
            int dualStrideRejects = 0;
            int pelvisGoalHardCuts = 0;
            int physicalPelvisHardCuts = 0;
            float maxPelvisGoalStep = 0f;
            float maxPhysicalPelvisStep = 0f;
            float maxPelvisResidual = 0f;
            float maxAnkleResidual = 0f;
            for (int i = 0; i < ordered.Count; i++)
            {
                FrameSample frame = ordered[i];
                FootRow left = frame.Left;
                FootRow right = frame.Right;
                maxPelvis = Math.Max(maxPelvis, Math.Max(left.PelvisWeight, right.PelvisWeight));
                maxNeg = Math.Min(maxNeg, Math.Min(left.Vertical, right.Vertical));
                bool leftSwing = left.Weight > 0.01f && left.Vertical > 0.0001f;
                bool rightSwing = right.Weight > 0.01f && right.Vertical > 0.0001f;
                if (leftSwing && rightSwing)
                    dual++;
                else if (leftSwing || rightSwing)
                    single++;
                if (leftSwing && left.Vertical > 0.03f && left.Residual < 0.01f)
                    visible++;
                if (rightSwing && right.Vertical > 0.03f && right.Residual < 0.01f)
                    visible++;
                CountFootClosure(in left, ref activeGoalRows, ref closedGoalRows,
                    ref closureFailures, ref maxAnkleResidual);
                CountFootClosure(in right, ref activeGoalRows, ref closedGoalRows,
                    ref closureFailures, ref maxAnkleResidual);
                float pelvisWeight = Math.Max(left.PelvisWeight, right.PelvisWeight);
                if (pelvisWeight > 0.01f)
                {
                    pelvisGoalFrames++;
                    float pelvisResidual = Math.Max(left.PelvisResidual, right.PelvisResidual);
                    maxPelvisResidual = Math.Max(maxPelvisResidual, pelvisResidual);
                    bool closed = left.HasClosedWriter && right.HasClosedWriter &&
                                  left.PelvisAvailable && right.PelvisAvailable &&
                                  pelvisResidual <= ResidualTolerance;
                    if (closed)
                        closedPelvisFrames++;
                    else
                        closureFailures++;
                }
                if (string.Equals(left.StrideRejectReason, "DualSwing", StringComparison.Ordinal))
                    dualStrideRejects++;
                if (i == 0)
                    continue;
                FrameSample previous = ordered[i - 1];
                if (frame.Frame != previous.Frame + 1 ||
                    frame.Left.BodyResetSequence != previous.Left.BodyResetSequence)
                    continue;
                float previousContribution = previous.Left.PelvisGoalY *
                    Math.Max(previous.Left.PelvisWeight, previous.Right.PelvisWeight);
                float currentContribution = left.PelvisGoalY * pelvisWeight;
                float goalStep = Math.Abs(currentContribution - previousContribution);
                float physicalStep = Math.Abs(
                    left.PhysicalPelvisY - previous.Left.PhysicalPelvisY);
                maxPelvisGoalStep = Math.Max(maxPelvisGoalStep, goalStep);
                maxPhysicalPelvisStep = Math.Max(maxPhysicalPelvisStep, physicalStep);
                if (goalStep > MaximumPelvisGoalStep)
                    pelvisGoalHardCuts++;
                if (physicalStep > MaximumPhysicalPelvisStep)
                    physicalPelvisHardCuts++;
            }

            int missedPromotions = CountMissedPromotions(ordered, true) +
                                   CountMissedPromotions(ordered, false);
            int contaminatingRoots = Math.Max(0, roots.Count - 1);
            bool targetIdentityValid = hasTargetIdentity &&
                                       targetRuntimeIds.Count == 1 &&
                                       targetHostIds.Count == 1 &&
                                       !targetRuntimeIds.Contains(string.Empty) &&
                                       !targetHostIds.Contains(0);
            bool passed = ordered.Count > 0 &&
                          targetIdentityValid &&
                          contaminatingRoots == 0 &&
                          expandedConsistencyFailures == 0 &&
                          dual == 0 &&
                          dualStrideRejects == 0 &&
                          missedPromotions == 0 &&
                          pelvisGoalHardCuts == 0 &&
                          physicalPelvisHardCuts == 0 &&
                          single > 0 &&
                          visible >= 8 &&
                          maxNeg > -0.02f &&
                          activeGoalRows > 0 &&
                          closedGoalRows == activeGoalRows &&
                          pelvisGoalFrames > 0 &&
                          closedPelvisFrames == pelvisGoalFrames &&
                          closureFailures == 0;
            string summary =
                $"frames={ordered.Count} roots={roots.Count} target={(targetIdentityValid ? "bound" : "invalid")} " +
                $"expandedMismatch={expandedConsistencyFailures} dualGoal={dual} dualStride={dualStrideRejects} " +
                $"missedPromotion={missedPromotions} pelvisCuts={pelvisGoalHardCuts}/{physicalPelvisHardCuts} " +
                $"maxPelvisStep={maxPelvisGoalStep:0.###}/{maxPhysicalPelvisStep:0.###} " +
                $"singleSwing={single} visibleLift={visible} goals={closedGoalRows}/{activeGoalRows} " +
                $"pelvis={closedPelvisFrames}/{pelvisGoalFrames} closureFailures={closureFailures} " +
                $"maxPelvisW={maxPelvis:0.###} maxPelvisResidual={maxPelvisResidual:0.###} " +
                $"maxAnkleResidual={maxAnkleResidual:0.###} maxNegCorr={maxNeg:0.###} " +
                $"=> {(passed ? "PASS" : "FAIL")}";
            return new CharacterFootLandingStep1Report(
                passed, csvPath, ordered.Count, dual, single, visible, maxPelvis, maxNeg, summary);
        }

        static int CountMissedPromotions(List<FrameSample> frames, bool leftSide)
        {
            ulong trackedEvent = 0;
            bool hadAcceptedNext = false;
            int failures = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                FootRow row = leftSide ? frames[i].Left : frames[i].Right;
                if (row.LandingEventIdentity == 0)
                    continue;
                if (trackedEvent != 0 && row.LandingEventIdentity != trackedEvent)
                {
                    if (hadAcceptedNext && row.LastLandingEventIdentity != trackedEvent)
                        failures++;
                    trackedEvent = row.LandingEventIdentity;
                    hadAcceptedNext = false;
                }
                else if (trackedEvent == 0)
                {
                    trackedEvent = row.LandingEventIdentity;
                }
                if (row.NextLandingEventIdentity == trackedEvent)
                    hadAcceptedNext = true;
            }
            return failures;
        }

        static void CountFootClosure(
            in FootRow row,
            ref int activeGoalRows,
            ref int closedGoalRows,
            ref int closureFailures,
            ref float maxAnkleResidual)
        {
            if (row.Weight <= 0.01f)
                return;
            activeGoalRows++;
            maxAnkleResidual = Math.Max(maxAnkleResidual, row.AnkleResidual);
            if (row.HasClosedWriter && row.AnkleResidual <= ResidualTolerance)
                closedGoalRows++;
            else
                closureFailures++;
        }

        static CharacterFootLandingStep1Report Fail(string path, string reason) =>
            new CharacterFootLandingStep1Report(false, path ?? string.Empty, 0, 0, 0, 0, 0f, 0f, reason);

        static int Index(string[] names, string name)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                    return i;
            }
            return -1;
        }

        static int Maximum(params int[] values)
        {
            int maximum = -1;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < 0)
                    return -1;
                maximum = Math.Max(maximum, values[i]);
            }
            return maximum;
        }

        static int ParseInt(string value) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : 0;

        static ulong ParseUlong(string value) =>
            ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed)
                ? parsed
                : 0;

        static float ParseFloat(string value) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : 0f;

        struct FramePair
        {
            internal FootRow? Left;
            internal FootRow? Right;
        }

        readonly struct FrameSample
        {
            internal FrameSample(int frame, ulong completion, FootRow left, FootRow right)
            {
                Frame = frame;
                Completion = completion;
                Left = left;
                Right = right;
            }

            internal int Frame { get; }
            internal ulong Completion { get; }
            internal FootRow Left { get; }
            internal FootRow Right { get; }
        }

        readonly struct FootRow
        {
            internal FootRow(
                float weight,
                float vertical,
                float residual,
                float pelvisWeight,
                bool ikSucceeded,
                int appliedGoalCount,
                bool pelvisAvailable,
                bool physicalWriteAvailable,
                ulong completionIdentity,
                ulong physicalWriteCompletionIdentity,
                float pelvisResidual,
                float ankleResidual,
                string strideState,
                string strideRejectReason,
                float pelvisGoalY,
                float physicalPelvisY,
                int bodyResetSequence,
                ulong landingEventIdentity,
                ulong lastLandingEventIdentity,
                ulong nextLandingEventIdentity)
            {
                Weight = weight;
                Vertical = vertical;
                Residual = residual;
                PelvisWeight = pelvisWeight;
                IkSucceeded = ikSucceeded;
                AppliedGoalCount = appliedGoalCount;
                PelvisAvailable = pelvisAvailable;
                PhysicalWriteAvailable = physicalWriteAvailable;
                CompletionIdentity = completionIdentity;
                PhysicalWriteCompletionIdentity = physicalWriteCompletionIdentity;
                PelvisResidual = pelvisResidual;
                AnkleResidual = ankleResidual;
                StrideState = strideState;
                StrideRejectReason = strideRejectReason;
                PelvisGoalY = pelvisGoalY;
                PhysicalPelvisY = physicalPelvisY;
                BodyResetSequence = bodyResetSequence;
                LandingEventIdentity = landingEventIdentity;
                LastLandingEventIdentity = lastLandingEventIdentity;
                NextLandingEventIdentity = nextLandingEventIdentity;
            }

            internal float Weight { get; }
            internal float Vertical { get; }
            internal float Residual { get; }
            internal float PelvisWeight { get; }
            internal bool IkSucceeded { get; }
            internal int AppliedGoalCount { get; }
            internal bool PelvisAvailable { get; }
            internal bool PhysicalWriteAvailable { get; }
            internal ulong CompletionIdentity { get; }
            internal ulong PhysicalWriteCompletionIdentity { get; }
            internal float PelvisResidual { get; }
            internal float AnkleResidual { get; }
            internal string StrideState { get; }
            internal string StrideRejectReason { get; }
            internal float PelvisGoalY { get; }
            internal float PhysicalPelvisY { get; }
            internal int BodyResetSequence { get; }
            internal ulong LandingEventIdentity { get; }
            internal ulong LastLandingEventIdentity { get; }
            internal ulong NextLandingEventIdentity { get; }
            internal bool HasClosedWriter =>
                IkSucceeded &&
                AppliedGoalCount > 0 &&
                PhysicalWriteAvailable &&
                CompletionIdentity != 0 &&
                PhysicalWriteCompletionIdentity == CompletionIdentity;

            internal bool SameSample(in FootRow other) =>
                Weight.Equals(other.Weight) &&
                Vertical.Equals(other.Vertical) &&
                PelvisWeight.Equals(other.PelvisWeight) &&
                string.Equals(StrideState, other.StrideState, StringComparison.Ordinal) &&
                string.Equals(StrideRejectReason, other.StrideRejectReason, StringComparison.Ordinal) &&
                PelvisGoalY.Equals(other.PelvisGoalY) &&
                PhysicalPelvisY.Equals(other.PhysicalPelvisY) &&
                BodyResetSequence == other.BodyResetSequence &&
                LandingEventIdentity == other.LandingEventIdentity &&
                LastLandingEventIdentity == other.LastLandingEventIdentity &&
                NextLandingEventIdentity == other.NextLandingEventIdentity;
        }
    }
}
