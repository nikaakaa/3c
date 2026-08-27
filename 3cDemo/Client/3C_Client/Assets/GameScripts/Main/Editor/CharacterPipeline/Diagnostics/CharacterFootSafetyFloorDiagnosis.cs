using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootSafetyFloorDiagnosis :
        ICharacterFootDiagnosis
    {
        const double HandoffJumpMeters = 0.01d;
        static readonly double[] s_HandoffOccurrenceThresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d
        };

        public string DiagnosticId => "safety-floor-current-ground";
        public string FileName => "safety-floor-current-ground.json";

        public CharacterFootDiagnosisDocument Build(
            CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("SafetyFloor");
            List<JObject> pathContinuity =
                context.Events("PathContinuity");
            List<JObject> handoffs =
                context.Events("SwingToLandingFloorHandoff");
            List<JObject> safetyFloorClamps = pathContinuity.FindAll(
                value => CharacterFootDiagnosisContext.Evidence(
                    value,
                    "safetyFloorClamped"));
            CharacterFootDiagnosisTarget clearance = context.Target(
                "accepted-current-floor-negative-clearance",
                "CurrentFloor Accepted后Safety Floor是否仍低于真实地面",
                new[] { "SafetyFloor" },
                new[] { "currentFloorAccepted&&clearanceAfterMeters<0" },
                events.FindAll(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "currentFloorAccepted")),
                value => CharacterFootDiagnosisContext.Evidence(
                             value,
                             "clearanceAfterNonNegative")
                    ? new List<string>()
                    : new List<string>
                    {
                        "currentFloorAccepted&&clearanceAfterMeters<0"
                    },
                value => Math.Abs(value.metrics["clearanceAfterMeters"]),
                "clearanceBeforeMeters",
                "clearanceAfterMeters",
                "clampMeters",
                "currentFloorDistanceMeters");
            CharacterFootDiagnosisTarget missingInput = context.Target(
                "clamp-without-current-floor-input",
                "Safety Floor Clamp是否缺少真实CurrentFloor输入",
                new[] { "SafetyFloor" },
                new[] { "safetyFloorClamped&&!currentFloorAccepted" },
                events,
                value => CharacterFootDiagnosisContext.Evidence(
                             value,
                             "clampHasCurrentFloorInput")
                    ? new List<string>()
                    : new List<string>
                    {
                        "safetyFloorClamped&&!currentFloorAccepted"
                    },
                value => value.metrics["clampMeters"],
                "clampMeters",
                "minimumCorrectionMeters",
                "currentFloorDistanceMeters");
            CharacterFootDiagnosisTarget largeClamp = context.Target(
                "large-clamp-without-current-floor-input",
                "大于10cm的Safety Floor Clamp是否缺少真实CurrentFloor输入",
                new[] { "SafetyFloor" },
                new[]
                {
                    "clampMeters>0.1&&!currentFloorAccepted"
                },
                events,
                value => CharacterFootDiagnosisContext.Evidence(
                             value,
                             "largeClampWithoutCurrentFloorInput")
                    ? new List<string>
                    {
                        "clampMeters>0.1&&!currentFloorAccepted"
                    }
                    : new List<string>(),
                value => value.metrics["clampMeters"],
                "clampMeters",
                "currentFloorDistanceMeters",
                "currentFloorSurfaceIdentity");
            CharacterFootDiagnosisTarget source = context.Target(
                "minimum-correction-source",
                "Safety Floor最小修正是否直接来自CurrentFloor Point",
                new[] { "SafetyFloor" },
                new[]
                {
                    "safetyFloorAvailable&&!minimumCorrectionMatchesCurrentFloor"
                },
                events.FindAll(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "safetyFloorAvailable")),
                value => CharacterFootDiagnosisContext.Evidence(
                             value,
                             "minimumCorrectionMatchesCurrentFloor")
                    ? new List<string>()
                    : new List<string>
                    {
                        "safetyFloorAvailable&&!minimumCorrectionMatchesCurrentFloor"
                    },
                value => value.metrics[
                    "minimumCorrectionSourceErrorMeters"],
                "minimumCorrectionSourceErrorMeters",
                "currentFloorVsSwingEnvelopeHeightDeltaMeters",
                "currentFloorPointHeight",
                "swingEnvelopeSampleHeight");
            CharacterFootDiagnosisTarget contract = context.Target(
                "current-floor-query-contract",
                "CurrentFloor查询是否使用正式CurrentSwingFloor合同",
                new[] { "SafetyFloor" },
                new[]
                {
                    "queryPurpose!=CurrentSwingFloor",
                    "safetyFloorAvailable&&!currentFloorAccepted"
                },
                events,
                MatchContract,
                value => value.matchedRules.Count,
                "queryMaximumDistanceMeters",
                "queryRadiusMeters",
                "queryMinimumNormalDot");
            CharacterFootDiagnosisTarget largePathClamp =
                context.Target(
                    "large-safety-floor-clamp",
                    "真实CurrentFloor Safety Floor是否在单帧产生超过LandingUpdateDistance的硬抬升",
                    new[] { "PathContinuity" },
                    new[]
                    {
                        "safetyFloorClampMeters>landingUpdateDistanceMeters"
                    },
                    safetyFloorClamps,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "safetyFloorClampMeters") >
                             CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "landingUpdateDistanceMeters")
                        ? new List<string>
                        {
                            "safetyFloorClampMeters>landingUpdateDistanceMeters"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "safetyFloorClampMeters"),
                    "safetyFloorClampMeters",
                    "landingUpdateDistanceMeters",
                    "safetyFloorClearanceBeforeMeters",
                    "safetyFloorClearanceAfterMeters",
                    "residualBeforeDecayMeters",
                    "residualAfterDecayMeters",
                    "correctionStepMeters");
            CharacterFootDiagnosisTarget handoff = context.Target(
                "swing-to-landing-floor-handoff-jump",
                "Swing进入Landing时Safety Floor补偿交接是否产生Correction或物理脚跳变",
                new[] { "SwingToLandingFloorHandoff" },
                new[] { "entryCorrectionStepMeters>0.01" },
                handoffs,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "entryCorrectionStepMeters") >
                         HandoffJumpMeters
                    ? new List<string>
                    {
                        "entryCorrectionStepMeters>0.01"
                    }
                    : new List<string>(),
                value => Math.Max(
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "entryCorrectionStepMeters"),
                    Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "entryPhysicalAnkleStepMeters"),
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "entryPhysicalSoleStepMeters"))),
                "entryCorrectionStepMeters",
                "entryCorrectionAlongUpMeters",
                "entryPhysicalAnkleStepMeters",
                "entryPhysicalAnkleAlongUpMeters",
                "entryPhysicalSoleStepMeters",
                "entryPhysicalSoleAlongUpMeters",
                "previousSafetyFloorClampMeters",
                "previousClearanceBeforeMeters",
                "previousClearanceAfterMeters",
                "previousResidualAfterDecayMeters",
                "landingUpdateDistanceMeters",
                "previousSafetyFloorCompensationMeters",
                "stepHeightMeters",
                "previousFormalFootHeightMeters",
                "formalFootHeightMeters",
                "previousProgress",
                "progress",
                "previousTimeToLandingSeconds",
                "timeToLandingSeconds");
            handoff.occurrence = context.Occurrence(
                "ContinuousSwingToLandingBoundary",
                "entryCorrectionStepMeters",
                "Meters",
                handoffs,
                HandoffJumpMeters,
                s_HandoffOccurrenceThresholds);
            return context.Document(
                DiagnosticId,
                clearance,
                missingInput,
                largeClamp,
                source,
                contract,
                largePathClamp,
                handoff);
        }

        static List<string> MatchContract(JObject value)
        {
            var result = new List<string>();
            if (!CharacterFootDiagnosisContext.Evidence(
                    value,
                    "queryPurposeCurrentSwingFloor"))
            {
                result.Add("queryPurpose!=CurrentSwingFloor");
            }
            if (!CharacterFootDiagnosisContext.Evidence(
                    value,
                    "safetyFloorAvailabilityHasCurrentFloorInput"))
            {
                result.Add("safetyFloorAvailable&&!currentFloorAccepted");
            }
            return result;
        }
    }

    internal sealed class CharacterFootSwingCurrentFloorCatchupDiagnosis :
        ICharacterFootDiagnosis
    {
        const double PrimaryThresholdMeters = 0.01d;
        static readonly double[] s_Thresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d
        };

        public string DiagnosticId => "swing-current-floor-catchup";
        public string FileName => "swing-current-floor-catchup.json";

        public CharacterFootDiagnosisDocument Build(
            CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events(
                "SwingCurrentFloorCatchup");
            CharacterFootDiagnosisTarget target = context.Target(
                "swing-current-floor-catchup",
                "同一Landing Event的无Anchor Swing中，CurrentSwingFloor从Builder目标或Residual滞后处补齐了多少高度",
                new[] { "SwingCurrentFloorCatchup" },
                new[] { "currentFloorCatchupMeters>0.01" },
                events,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "CurrentFloorCatchup") >
                         PrimaryThresholdMeters
                    ? new List<string>
                    {
                        "currentFloorCatchupMeters>0.01"
                    }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "CurrentFloorCatchup"),
                "BuilderSwingTargetAlongUp",
                "StateOutputBeforeFloorAlongUp",
                "CurrentSwingFloorMinimumAlongUp",
                "CurrentFloorAboveBuilderTarget",
                "ResidualLagBelowCurrentFloor",
                "CurrentFloorCatchup",
                "SafetyFloorClamp",
                "FinalOutputAlongUp",
                "PhysicalAnkleStep",
                "PhysicalSoleStep");
            target.occurrence = context.Occurrence(
                "ContinuousAcceptedUnanchoredSwingPairWithCurrentFloor",
                "CurrentFloorCatchup",
                "Meters",
                events,
                PrimaryThresholdMeters,
                s_Thresholds);
            target.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["Classification"] = events
                        .GroupBy(
                            value => value["swingCurrentFloorCatchup"]?
                                ["classification"]?.Value<string>() ??
                                "Unspecified",
                            StringComparer.Ordinal)
                        .OrderBy(
                            value => value.Key,
                            StringComparer.Ordinal)
                        .Select(value =>
                            new CharacterFootDiagnosisCategoryCount
                            {
                                value = value.Key,
                                count = value.Count()
                            })
                        .ToList()
                };
            List<JObject> counterfactualEvents = context.Events(
                "SwingActualFootEnvelopeCounterfactual");
            List<JObject> currentFloorCounterfactualEvents =
                counterfactualEvents.FindAll(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "currentFloorComparisonAvailable"));
            List<JObject> unambiguousCounterfactualEvents =
                counterfactualEvents.FindAll(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "actualEnvelopeCurrentFloorComparisonAvailable") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "ambiguousEnvelopeAtActualFootDistance"));
            Func<JObject, List<string>> matchCounterfactual = value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "currentFloorCatchupAbove1cm")
                    ? new List<string>
                    {
                        "currentFloorCatchupMeters>0.01"
                    }
                    : new List<string>();
            CharacterFootDiagnosisTarget counterfactual = context.Target(
                "swing-actual-foot-envelope-counterfactual",
                "真实脚水平距离处的GroundPath Envelope候选是否唯一，以及非歧义候选能否在CurrentSwingFloor硬Clamp前覆盖地面",
                new[] { "SwingActualFootEnvelopeCounterfactual" },
                new[] { "currentFloorCatchupMeters>0.01" },
                counterfactualEvents,
                matchCounterfactual,
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "CurrentFloorCatchup"),
                "ActualFootHorizontalDistance",
                "BaselineHorizontalDistance",
                "PhaseEnvelopeHorizontalDistance",
                "ActualMinusPhaseEnvelopeHorizontalDistance",
                "ActualFootClosestPathParameter",
                "ActualFootDistanceAlongAxis",
                "ActualFootCrossTrackDistance",
                "GroundPathCorridorRadius",
                "ActualEnvelopeCandidateCount",
                "ActualEnvelopeHeightSpan",
                "PhaseSampleHeightAlongUp",
                "BuilderSwingTargetAlongUp",
                "StateOutputBeforeFloorAlongUp",
                "CurrentSwingFloorMinimumAlongUp",
                "FinalOutputAlongUp",
                "CurrentFloorCatchup",
                "ActualProgressEnvelopeMinimumCorrection",
                "ActualProgressEnvelopeAdvanceAboveBuilderTarget",
                "ActualProgressEnvelopeRemainingBelowCurrentFloor",
                "ActualEnvelopeCoversCurrentFloor");
            counterfactual.occurrence = context.Occurrence(
                "ContinuousAcceptedUnanchoredSwingPairWithCurrentFloor",
                "CurrentFloorCatchup",
                "Meters",
                currentFloorCounterfactualEvents,
                PrimaryThresholdMeters,
                s_Thresholds);
            counterfactual.supplementalOccurrences =
                new List<CharacterFootDiagnosisOccurrenceProfile>
                {
                    context.Occurrence(
                        "UnambiguousActualFootEnvelopePairWithCurrentFloor",
                        "ActualEnvelopeCoversCurrentFloor",
                        "Boolean",
                        unambiguousCounterfactualEvents,
                        0.5d,
                        0.5d)
                };
            counterfactual.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["CounterfactualState"] = counterfactualEvents
                        .GroupBy(
                            value => value[
                                    "swingActualFootEnvelopeCounterfactual"]?
                                ["counterfactualState"]?.Value<string>() ??
                                "Unspecified",
                            StringComparer.Ordinal)
                        .OrderBy(
                            value => value.Key,
                            StringComparer.Ordinal)
                        .Select(value =>
                            new CharacterFootDiagnosisCategoryCount
                            {
                                value = value.Key,
                                count = value.Count()
                            })
                        .ToList()
                };
            counterfactual.representativeEvents = context.Representatives(
                counterfactualEvents,
                matchCounterfactual,
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "CurrentFloorCatchup"),
                16);
            counterfactual.representativeEventCount =
                counterfactual.representativeEvents.Count;
            return context.Document(
                DiagnosticId,
                target,
                counterfactual);
        }
    }

    [Serializable]
    internal sealed class CharacterFootSwingCurrentFloorCatchupAnalysis
    {
        public int previousFrame;
        public int frame;
        public string side;
        public string landingEventIdentity;
        public string sourceIdentity;
        public int sourceCycle;
        public CharacterFootVectorFact componentUp;
        public CharacterFootVectorFact builderSwingTarget;
        public double builderSwingTargetAlongUpMeters;
        public CharacterFootVectorFact stateOutputBeforeFloor;
        public double stateOutputBeforeFloorAlongUpMeters;
        public CharacterFootVectorFact currentSwingFloorMinimum;
        public double currentSwingFloorMinimumAlongUpMeters;
        public double safetyFloorClampMeters;
        public CharacterFootVectorFact finalOutput;
        public double finalOutputAlongUpMeters;
        public double currentFloorAboveBuilderTargetMeters;
        public double residualLagBelowCurrentFloorMeters;
        public double currentFloorCatchupMeters;
        public int currentFloorSurfaceIdentity;
        public CharacterFootVectorFact currentFloorPoint;
        public bool physicalAnkleAvailable;
        public CharacterFootVectorFact previousPhysicalAnkle;
        public CharacterFootVectorFact physicalAnkle;
        public double physicalAnkleStepMeters;
        public double physicalAnkleAlongUpDeltaMeters;
        public bool physicalSoleAvailable;
        public CharacterFootVectorFact previousPhysicalSole;
        public CharacterFootVectorFact physicalSole;
        public double physicalSoleStepMeters;
        public double physicalSoleAlongUpDeltaMeters;
        public string classification;
    }

    [Serializable]
    internal sealed class CharacterFootSwingActualFootEnvelopeCounterfactualAnalysis
    {
        public int previousFrame;
        public int frame;
        public string side;
        public string landingEventIdentity;
        public string sourceIdentity;
        public int sourceCycle;
        public string groundPathInputIdentity;
        public CharacterFootVectorFact componentUp;
        public double actualFootHorizontalDistanceMeters;
        public double baselineHorizontalDistanceMeters;
        public double phaseEnvelopeHorizontalDistanceMeters;
        public double actualMinusPhaseEnvelopeHorizontalDistanceMeters;
        public string actualFootAxisRegion;
        public double actualFootClosestPathParameter;
        public double actualFootDistanceAlongAxisMeters;
        public double actualFootCrossTrackDistanceMeters;
        public double groundPathCorridorRadiusMeters;
        public bool actualFootWithinGroundPathCorridor;
        public string intersectionState;
        public string counterfactualState;
        public int candidateCount;
        public double minimumCandidateHeightAlongUpMeters;
        public double maximumCandidateHeightAlongUpMeters;
        public double candidateHeightSpanMeters;
        public bool hasVerticalEdge;
        public bool hasMultipleHeights;
        public bool ambiguousEnvelopeAtActualFootDistance;
        public double phaseSampleHeightAlongUpMeters;
        public bool builderSwingTargetAvailable;
        public CharacterFootVectorFact builderSwingTarget;
        public double builderSwingTargetAlongUpMeters;
        public bool stateOutputBeforeFloorAvailable;
        public CharacterFootVectorFact stateOutputBeforeFloor;
        public double stateOutputBeforeFloorAlongUpMeters;
        public bool currentFloorComparisonAvailable;
        public CharacterFootVectorFact currentSwingFloorMinimum;
        public double currentSwingFloorMinimumAlongUpMeters;
        public CharacterFootVectorFact finalOutput;
        public double finalOutputAlongUpMeters;
        public bool actualProgressEnvelopeCorrectionAvailable;
        public double actualProgressEnvelopeMinimumCorrectionMeters;
        public double actualProgressEnvelopeAdvanceAboveBuilderTargetMeters;
        public double actualProgressEnvelopeRemainingBelowCurrentFloorMeters;
        public bool actualProgressEnvelopeCoversCurrentFloor;
        public double currentFloorCatchupMeters;
    }

    [Serializable]
    internal sealed class CharacterFootSwingToLandingFloorHandoffAnalysis
    {
        public int previousFrame;
        public int frame;
        public string side;
        public string eventIdentity;
        public string previousSourceIdentity;
        public string sourceIdentity;
        public int previousSourceCycle;
        public int sourceCycle;
        public string previousContributionContinuityIdentity;
        public string contributionContinuityIdentity;
        public string stateBefore;
        public string stateAfter;
        public double entryCorrectionStepMeters;
        public double entryCorrectionAlongUpMeters;
        public bool entryPhysicalAnkleAvailable;
        public double entryPhysicalAnkleStepMeters;
        public double entryPhysicalAnkleAlongUpMeters;
        public bool entryPhysicalSoleAvailable;
        public double entryPhysicalSoleStepMeters;
        public double entryPhysicalSoleAlongUpMeters;
        public double previousSafetyFloorClampMeters;
        public double previousSafetyFloorClearanceBeforeMeters;
        public double previousSafetyFloorClearanceAfterMeters;
        public double previousResidualAfterDecayMeters;
        public double landingUpdateDistanceMeters;
        public CharacterFootVectorFact previousFinalEffectiveCorrection;
        public CharacterFootVectorFact finalEffectiveCorrection;
        public CharacterFootVectorFact previousSafetyFloorMinimumCorrection;
        public CharacterFootVectorFact previousSafetyFloorOutputCorrection;
        public double previousSafetyFloorCompensationMeters;
        public double previousSafetyFloorCompensationAlongUpMeters;
        public bool currentSafetyFloorAvailable;
        public string currentFloorState;
        public bool currentFloorAccepted;
        public int currentFloorSurfaceIdentity;
        public double currentContactOwnership;
        public bool currentContactPlaneAvailable;
        public int currentContactSurfaceIdentity;
        public double stepHeightMeters;
        public string stepDirection;
        public double previousFormalFootHeightMeters;
        public double formalFootHeightMeters;
        public bool previousFormalFootHeightAvailable;
        public bool formalFootHeightAvailable;
        public double previousProgress;
        public double progress;
        public double previousTimeToLandingSeconds;
        public double timeToLandingSeconds;
        public bool previousSafetyFloorOwned;
        public bool residualWithinDeadline;
        public bool floorCompensationDroppedAtLanding;
    }
}
