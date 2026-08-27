using System;
using System.Collections.Generic;
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
