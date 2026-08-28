using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterFootPathStageNames
    {
        internal const string RawLandingToPathTarget =
            "RawLandingToPathTarget";
        internal const string PathTargetToSwingTarget =
            "PathTargetToSwingTarget";
        internal const string SwingTargetToCapturedResidual =
            "SwingTargetToCapturedResidual";
        internal const string CapturedResidualToDecayedResidual =
            "CapturedResidualToDecayedResidual";
        internal const string ResidualOutputToStateOutput =
            "ResidualOutputToStateOutput";
        internal const string StateOutputToSafetyFloorOutput =
            "StateOutputToSafetyFloorOutput";
        internal const string FinalCorrectionToEncodedGoal =
            "FinalCorrectionToEncodedGoal";
        internal const string EncodedGoalToSolvedFoot =
            "EncodedGoalToSolvedFoot";

        internal static readonly string[] All =
        {
            RawLandingToPathTarget,
            PathTargetToSwingTarget,
            SwingTargetToCapturedResidual,
            CapturedResidualToDecayedResidual,
            ResidualOutputToStateOutput,
            StateOutputToSafetyFloorOutput,
            FinalCorrectionToEncodedGoal,
            EncodedGoalToSolvedFoot
        };
    }

    [Serializable]
    internal sealed class CharacterFootPathStageVector3
    {
        public double x;
        public double y;
        public double z;

        internal void RequireValid()
        {
            if (!double.IsFinite(x) ||
                !double.IsFinite(y) ||
                !double.IsFinite(z))
            {
                throw new InvalidOperationException(
                    "Foot path stage vector is non-finite.");
            }
        }
    }

    [Serializable]
    internal sealed class CharacterFootPathStageLineage
    {
        public int previousFrame;
        public int frame;
        public string previousCompletionIdentity;
        public string completionIdentity;
        public string side;
        public string previousEventIdentity;
        public string eventIdentity;
        public string previousSourceIdentity;
        public string sourceIdentity;
        public int previousSourceCycle;
        public int sourceCycle;
        public string previousPathInputIdentity;
        public string pathInputIdentity;
    }

    [Serializable]
    internal sealed class CharacterFootPathStageStateEvidence
    {
        public string previousState;
        public string stateBefore;
        public string stateAfter;
        public string previousLockResponse;
        public string lockResponseBefore;
        public string lockResponseAfter;
        public string revisionReason;
        public bool residualRebuilt;
        public bool targetTrackingApplied;
        public bool safetyFloorClamped;

        internal void RequireValid(bool required)
        {
            RequireEnum<CharacterFootConstraintState>(
                previousState,
                "previousState",
                required);
            RequireEnum<CharacterFootConstraintState>(
                stateBefore,
                "stateBefore",
                required);
            RequireEnum<CharacterFootConstraintState>(
                stateAfter,
                "stateAfter",
                required);
            RequireEnum<CharacterFootLockResponse>(
                previousLockResponse,
                "previousLockResponse",
                required);
            RequireEnum<CharacterFootLockResponse>(
                lockResponseBefore,
                "lockResponseBefore",
                required);
            RequireEnum<CharacterFootLockResponse>(
                lockResponseAfter,
                "lockResponseAfter",
                required);
        }

        static void RequireEnum<T>(
            string value,
            string field,
            bool required)
            where T : struct, Enum
        {
            if (string.IsNullOrEmpty(value) && !required)
                return;
            if (!Enum.TryParse(value, false, out T parsed) ||
                !Enum.IsDefined(typeof(T), parsed))
            {
                throw new InvalidOperationException(
                    $"Foot path stage {field} '{value}' is invalid.");
            }
        }
    }

    [Serializable]
    internal sealed class CharacterFootPathStageDelta
    {
        public string stage;
        public bool applicable;
        public bool available;
        public string unavailableReason;
        public int previousFrame;
        public int frame;
        public CharacterFootPathStageVector3 inputBefore;
        public CharacterFootPathStageVector3 inputAfter;
        public CharacterFootPathStageVector3 outputBefore;
        public CharacterFootPathStageVector3 outputAfter;
        public double? inputDeltaMeters;
        public double? outputDeltaMeters;
        public double? amplificationMeters;
        public bool amplificationRatioAvailable;
        public double? amplificationRatio;

        internal void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(stage))
                throw new InvalidOperationException(
                    "Foot path stage identity is unavailable.");
            RequireFinite(inputDeltaMeters);
            RequireFinite(outputDeltaMeters);
            RequireFinite(amplificationMeters);
            RequireFinite(amplificationRatio);
            if (available && !applicable)
                throw new InvalidOperationException(
                    $"Foot path stage '{stage}' is available but not applicable.");
            if (available &&
                (inputBefore == null ||
                 inputAfter == null ||
                 outputBefore == null ||
                 outputAfter == null ||
                 !inputDeltaMeters.HasValue ||
                 !outputDeltaMeters.HasValue ||
                 !amplificationMeters.HasValue))
            {
                throw new InvalidOperationException(
                    $"Foot path stage '{stage}' is incomplete.");
            }
            inputBefore?.RequireValid();
            inputAfter?.RequireValid();
            outputBefore?.RequireValid();
            outputAfter?.RequireValid();
            if (amplificationRatioAvailable &&
                !amplificationRatio.HasValue)
            {
                throw new InvalidOperationException(
                    $"Foot path stage '{stage}' ratio is incomplete.");
            }
        }

        static void RequireFinite(double? value)
        {
            if (value.HasValue && !double.IsFinite(value.Value))
                throw new InvalidOperationException(
                    "Foot path stage contains a non-finite value.");
        }
    }

    [Serializable]
    internal sealed class CharacterFootPathStageFacts
    {
        public bool residualCaptureAvailable;
        public CharacterFootPathStageVector3 residualBeforeRevisionPrevious;
        public CharacterFootPathStageVector3 residualBeforeRevision;
        public CharacterFootPathStageVector3 capturedResidualPrevious;
        public CharacterFootPathStageVector3 capturedResidual;
        public bool groundEnvelopeSafetyCorrectionAvailable;
        public CharacterFootPathStageVector3 groundEnvelopeSafetyCorrectionPrevious;
        public CharacterFootPathStageVector3 groundEnvelopeSafetyCorrection;
        public bool physicalFootAvailable;
        public CharacterFootPathStageVector3 physicalFootPrevious;
        public CharacterFootPathStageVector3 physicalFoot;

        internal void RequireValid()
        {
            residualBeforeRevisionPrevious?.RequireValid();
            residualBeforeRevision?.RequireValid();
            capturedResidualPrevious?.RequireValid();
            capturedResidual?.RequireValid();
            groundEnvelopeSafetyCorrectionPrevious?.RequireValid();
            groundEnvelopeSafetyCorrection?.RequireValid();
            physicalFootPrevious?.RequireValid();
            physicalFoot?.RequireValid();
        }
    }

    [Serializable]
    internal sealed class CharacterFootPathFirstAmplification
    {
        public bool available;
        public string unavailableReason;
        public string stage;
        public int previousFrame;
        public int frame;
        public double? inputDeltaMeters;
        public double? outputDeltaMeters;
        public double? amplificationMeters;
        public bool amplificationRatioAvailable;
        public double? amplificationRatio;
        public CharacterFootPathStageStateEvidence stateEvidence;
    }

    [Serializable]
    internal sealed class CharacterFootSwingTargetCounterfactual
    {
        public bool available;
        public string unavailableReason;
        public string classification;
        public CharacterFootPathStageVector3 phaseOnlyTarget;
        public CharacterFootPathStageVector3 pathRevisedTarget;
        public CharacterFootPathStageVector3 actualSwingTarget;
        public double? actualReconstructionError;
        public double? phaseAdvanceDelta;
        public double? pathRevisionDelta;
        public double? observedSwingTargetDelta;
        public double? pathRevisionContribution;
        public double? phaseContribution;

        internal void RequireValid()
        {
            phaseOnlyTarget?.RequireValid();
            pathRevisedTarget?.RequireValid();
            actualSwingTarget?.RequireValid();
            RequireFinite(actualReconstructionError);
            RequireFinite(phaseAdvanceDelta);
            RequireFinite(pathRevisionDelta);
            RequireFinite(observedSwingTargetDelta);
            RequireFinite(pathRevisionContribution);
            RequireFinite(phaseContribution);
            if (available &&
                (string.IsNullOrWhiteSpace(classification) ||
                 phaseOnlyTarget == null ||
                 pathRevisedTarget == null ||
                 actualSwingTarget == null ||
                 !actualReconstructionError.HasValue ||
                 !phaseAdvanceDelta.HasValue ||
                 !pathRevisionDelta.HasValue ||
                 !observedSwingTargetDelta.HasValue ||
                 !pathRevisionContribution.HasValue ||
                 !phaseContribution.HasValue))
            {
                throw new InvalidOperationException(
                    "Swing target counterfactual is incomplete.");
            }
        }

        static void RequireFinite(double? value)
        {
            if (value.HasValue && !double.IsFinite(value.Value))
                throw new InvalidOperationException(
                    "Swing target counterfactual contains a non-finite value.");
        }
    }

    [Serializable]
    internal sealed class CharacterFootPathStageAnalysis
    {
        public bool available;
        public string unavailableReason;
        public double amplificationNoiseFloorMeters;
        public CharacterFootPathStageLineage lineage;
        public CharacterFootPathStageStateEvidence stateEvidence;
        public CharacterFootPathStageFacts stageFacts;
        public List<string> missingStages;
        public List<CharacterFootPathStageDelta> stages;
        public CharacterFootPathFirstAmplification firstAmplificationStage;
        public CharacterFootSwingTargetCounterfactual swingTargetCounterfactual;

        internal void RequireValid()
        {
            if (!double.IsFinite(amplificationNoiseFloorMeters) ||
                amplificationNoiseFloorMeters < 0d)
            {
                throw new InvalidOperationException(
                    "Foot path stage noise floor is invalid.");
            }
            missingStages ??= new List<string>();
            stages ??= new List<CharacterFootPathStageDelta>();
            for (int i = 0; i < stages.Count; i++)
                stages[i].RequireValid();
            stageFacts?.RequireValid();
            swingTargetCounterfactual?.RequireValid();
            stateEvidence?.RequireValid(available);
            if (available && missingStages.Count > 0)
                throw new InvalidOperationException(
                    "Available Foot path stage analysis has missing stages.");
        }

        internal static CharacterFootPathStageAnalysis Unavailable(
            string reason,
            int previousFrame,
            int frame,
            string side,
            string eventIdentity,
            string sourceIdentity)
        {
            var stages = new List<CharacterFootPathStageDelta>(
                CharacterFootPathStageNames.All.Length);
            for (int i = 0; i < CharacterFootPathStageNames.All.Length; i++)
            {
                stages.Add(new CharacterFootPathStageDelta
                {
                    stage = CharacterFootPathStageNames.All[i],
                    applicable = true,
                    available = false,
                    unavailableReason = reason,
                    previousFrame = previousFrame,
                    frame = frame
                });
            }
            return new CharacterFootPathStageAnalysis
            {
                available = false,
                unavailableReason = reason,
                amplificationNoiseFloorMeters = 0.001d,
                lineage = new CharacterFootPathStageLineage
                {
                    previousFrame = previousFrame,
                    frame = frame,
                    side = side ?? string.Empty,
                    eventIdentity = eventIdentity ?? string.Empty,
                    sourceIdentity = sourceIdentity ?? string.Empty
                },
                stateEvidence = new CharacterFootPathStageStateEvidence(),
                stageFacts = new CharacterFootPathStageFacts(),
                missingStages = new List<string>(CharacterFootPathStageNames.All),
                stages = stages,
                firstAmplificationStage = new CharacterFootPathFirstAmplification
                {
                    available = false,
                    unavailableReason = reason,
                    previousFrame = previousFrame,
                    frame = frame
                }
            };
        }
    }

    [Serializable]
    internal sealed class CharacterFootPathStageAnalysisCoverage
    {
        public bool available;
        public int eligibleEventCount;
        public int availableEventCount;
        public int unavailableEventCount;
        public SortedDictionary<string, int> missingStageCounts;
        public SortedDictionary<string, int> firstAmplificationStageCounts;
    }

    internal static class CharacterFootPathStageDiagnosisProjection
    {
        internal static void Apply(
            CharacterFootDiagnosisTarget target,
            List<JObject> events)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            var missing = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var firstStages = new SortedDictionary<string, int>(StringComparer.Ordinal);
            int availableCount = 0;
            for (int i = 0; i < events.Count; i++)
            {
                CharacterFootPathStageAnalysis analysis = Read(events[i]);
                if (analysis.available)
                    availableCount++;
                for (int stage = 0; stage < analysis.missingStages.Count; stage++)
                {
                    string name = analysis.missingStages[stage];
                    missing.TryGetValue(name, out int count);
                    missing[name] = count + 1;
                }
                if (analysis.firstAmplificationStage?.available == true)
                {
                    string name = analysis.firstAmplificationStage.stage;
                    firstStages.TryGetValue(name, out int count);
                    firstStages[name] = count + 1;
                }
            }
            target.pathStageAnalysis = new CharacterFootPathStageAnalysisCoverage
            {
                available = events.Count > 0 && availableCount == events.Count,
                eligibleEventCount = events.Count,
                availableEventCount = availableCount,
                unavailableEventCount = events.Count - availableCount,
                missingStageCounts = missing,
                firstAmplificationStageCounts = firstStages
            };
            for (int i = 0; i < target.representativeEvents.Count; i++)
            {
                CharacterFootDiagnosisEvidence representative =
                    target.representativeEvents[i];
                JObject source = events.Find(value => Matches(
                    representative,
                    value));
                representative.pathStageAnalysis = source != null
                    ? Read(source)
                    : CharacterFootPathStageAnalysis.Unavailable(
                        "RepresentativeEventFactsUnavailable",
                        representative.startFrame,
                        representative.endFrame,
                        representative.side,
                        representative.eventIdentity,
                        representative.sourceIdentity);
            }
        }

        static CharacterFootPathStageAnalysis Read(JObject value)
        {
            if (value["pathStageAnalysis"] is JObject token)
            {
                CharacterFootPathStageAnalysis analysis =
                    token.ToObject<CharacterFootPathStageAnalysis>();
                analysis.RequireValid();
                return analysis;
            }
            return CharacterFootPathStageAnalysis.Unavailable(
                "StageFactsUnavailable",
                value.Value<int?>("startFrame") ?? 0,
                value.Value<int?>("peakFrame") ??
                value.Value<int?>("endFrame") ?? 0,
                value.Value<string>("side") ?? string.Empty,
                value.Value<string>("eventIdentity") ?? string.Empty,
                value.Value<string>("sourceIdentity") ?? string.Empty);
        }

        static bool Matches(
            CharacterFootDiagnosisEvidence representative,
            JObject value) =>
            representative.startFrame ==
            (value.Value<int?>("startFrame") ?? 0) &&
            representative.endFrame ==
            (value.Value<int?>("endFrame") ?? 0) &&
            string.Equals(
                representative.side,
                value.Value<string>("side") ?? string.Empty,
                StringComparison.Ordinal) &&
            string.Equals(
                representative.eventIdentity,
                value.Value<string>("eventIdentity") ?? string.Empty,
                StringComparison.Ordinal);
    }
}
