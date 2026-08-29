using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLandingObservationReuseDiagnosis :
        ICharacterFootDiagnosis
    {
        public string DiagnosticId => "landing-observation-reuse";
        public string FileName => "landing-observation-reuse.json";

        public CharacterFootDiagnosisDocument Build(
            CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("LandingObservation");
            List<JObject> currentSupport = context.Events(
                "CurrentSupportQuery");
            Func<JObject, List<string>> match = value =>
            {
                var rules = new List<string>();
                if (!CharacterFootDiagnosisContext.Evidence(
                        value,
                        "cacheStateConsistent"))
                    rules.Add("cacheStateConsistent=false");
                if (CharacterFootDiagnosisContext.Evidence(
                        value,
                        "duplicateQuery"))
                    rules.Add("duplicateQuery=true");
                if (CharacterFootDiagnosisContext.Evidence(
                        value,
                        "identitySeenBefore") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "resultMatchesPrevious"))
                    rules.Add("sameKeyResultChanged=true");
                if (!CharacterFootDiagnosisContext.Evidence(
                        value,
                        "queryThresholdContractConsistent"))
                {
                    rules.Add("queryThresholdContractConsistent=false");
                }
                return rules;
            };
            CharacterFootDiagnosisTarget target = context.Target(
                "landing-observation-reuse-contract",
                "FutureLanding按阈值复用且首次Current Contact Verification允许同Key强制查询一次，后续不得重复",
                new[] { "LandingObservation" },
                new[]
                {
                    "cacheStateConsistent=false",
                    "duplicateQuery=true",
                    "sameKeyResultChanged=true",
                    "queryThresholdContractConsistent=false"
                },
                events,
                match,
                _ => 0d,
                "ValidCandidateCount",
                "QueryInputDistance",
                "PredictionInputAccumulationDistance",
                "QueryComponentUpAngleDegrees",
                "ComponentUpChangeAngleDegrees");
            target.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["CacheState"] = events
                        .GroupBy(
                            value => value["landingObservation"]?
                                         ["cacheState"]?.Value<string>() ??
                                     "Unavailable",
                            StringComparer.Ordinal)
                        .OrderBy(value => value.Key, StringComparer.Ordinal)
                        .Select(value => new CharacterFootDiagnosisCategoryCount
                        {
                            value = value.Key,
                            count = value.Count()
                        })
                        .ToList(),
                    ["QueryReason"] = events
                        .GroupBy(
                            value => value["landingObservation"]?
                                         ["queryReason"]?.Value<string>() ??
                                     "None",
                            StringComparer.Ordinal)
                        .OrderBy(value => value.Key, StringComparer.Ordinal)
                        .Select(value => new CharacterFootDiagnosisCategoryCount
                        {
                            value = value.Key,
                            count = value.Count()
                        })
                        .ToList()
                };
            target.representativeEvents = context.Representatives(
                events,
                match,
                _ => 0d,
                16);
            target.representativeEventCount =
                target.representativeEvents.Count;
            CharacterFootDiagnosisTarget currentSupportTarget =
                context.Target(
                    "current-support-five-probe-resolution",
                    "Current Support五点事务是否按固定候选合同解析同一Position与Direction记录",
                    new[] { "CurrentSupportQuery" },
                    new[] { "available=false" },
                    currentSupport,
                    value => CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "available")
                        ? new List<string>()
                        : new List<string> { "available=false" },
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "AvailableSoleCandidateCount"),
                    "BaseHitCandidateCount",
                    "RearHitCandidateCount",
                    "PositiveLateralHitCandidateCount",
                    "NegativeLateralHitCandidateCount",
                    "ToeCandidateCount",
                    "AvailableSoleCandidateCount",
                    "BaseCandidateHeightAlongUp",
                    "RearCandidateHeightAlongUp",
                    "PositiveLateralCandidateHeightAlongUp",
                    "NegativeLateralCandidateHeightAlongUp",
                    "ToeCandidateHeightAlongUp",
                    "RearProbeExtension",
                    "LateralProbeExtent",
                    "ToeProbeExtension");
            currentSupportTarget.scorePolicy = "Informational";
            currentSupportTarget.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["SelectedProbe"] = currentSupport
                        .GroupBy(CurrentSupportSelectedProbe)
                        .OrderBy(value => value.Key, StringComparer.Ordinal)
                        .Select(value => new CharacterFootDiagnosisCategoryCount
                        {
                            value = value.Key,
                            count = value.Count()
                        })
                        .ToList(),
                    ["RejectReason"] = currentSupport
                        .GroupBy(CurrentSupportRejectReason)
                        .OrderBy(value => value.Key, StringComparer.Ordinal)
                        .Select(value => new CharacterFootDiagnosisCategoryCount
                        {
                            value = value.Key,
                            count = value.Count()
                        })
                        .ToList()
                };
            return context.Document(
                DiagnosticId,
                target,
                currentSupportTarget);
        }

        static string CurrentSupportSelectedProbe(JObject value)
        {
            if (!CharacterFootDiagnosisContext.Evidence(value, "available"))
                return "Unavailable";
            if (CharacterFootDiagnosisContext.Evidence(value, "selectedBase"))
                return "Base";
            if (CharacterFootDiagnosisContext.Evidence(value, "selectedRear"))
                return "Rear";
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "selectedPositiveLateral"))
                return "PositiveLateral";
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "selectedNegativeLateral"))
                return "NegativeLateral";
            if (CharacterFootDiagnosisContext.Evidence(value, "selectedToe"))
                return "Toe";
            return "Invalid";
        }

        static string CurrentSupportRejectReason(JObject value)
        {
            if (CharacterFootDiagnosisContext.Evidence(value, "available"))
                return "None";
            string[] reasons =
            {
                "BaseUnavailable",
                "ToeUnavailable",
                "BaseAndToeUnavailable",
                "InvalidSupportNormal",
                "NotGrounded",
                "WorldRevisionMismatch",
                "CapacityExceeded"
            };
            for (int i = 0; i < reasons.Length; i++)
            {
                if (CharacterFootDiagnosisContext.Evidence(
                        value,
                        "reject" + reasons[i]))
                    return reasons[i];
            }
            return "Invalid";
        }
    }

    [Serializable]
    internal sealed class CharacterFootLandingQueryCandidateFact
    {
        public bool available;
        public int surfaceIdentity;
        public CharacterFootVectorFact point;
        public double distanceMeters;
    }

    [Serializable]
    internal sealed class CharacterFootLandingObservationAnalysis
    {
        public int previousFrame;
        public int frame;
        public string side;
        public string landingEventIdentity;
        public string sourceIdentity;
        public int sourceCycle;
        public string observationIdentity;
        public string worldRevision;
        public string sourceSampleIdentity;
        public int sourceSampleCycle;
        public string cacheState;
        public bool queryExecutedThisFrame;
        public string queryPurpose;
        public string refreshMode;
        public string queryReason;
        public CharacterFootVectorFact canonicalRawLanding;
        public CharacterFootVectorFact canonicalComponentUp;
        public CharacterFootVectorFact candidateRawLanding;
        public CharacterFootVectorFact candidateComponentUp;
        public double queryInputDistanceMeters;
        public double queryComponentUpAngleDegrees;
        public double predictionInputAccumulationDistanceMeters;
        public double componentUpChangeAngleDegrees;
        public string selectionState;
        public int validCandidateCount;
        public CharacterFootLandingQueryCandidateFact selected;
        public bool identitySeenBefore;
        public bool forcedPlantVerification;
        public bool firstForcedPlantVerification;
        public bool duplicateQuery;
        public bool resultMatchesPrevious;
        public bool cacheStateConsistent;
        public bool queryThresholdContractConsistent;
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
        public double entryCorrectionReexpressionStepMeters;
        public double entryCorrectionReexpressionAlongUpMeters;
        public double entryCorrectedSoleStepMeters;
        public double entryAnimatedSoleStepMeters;
        public double entryStateAdditionalOutputStepMeters;
        public double entryOutputBlendParameter;
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
        public double swingResidualToleranceMeters;
        public CharacterFootVectorFact previousFinalEffectiveCorrection;
        public CharacterFootVectorFact finalEffectiveCorrection;
        public CharacterFootVectorFact previousSafetyFloorMinimumCorrection;
        public CharacterFootVectorFact previousSafetyFloorOutputCorrection;
        public double previousSafetyFloorCompensationMeters;
        public double previousSafetyFloorCompensationAlongUpMeters;
        public string previousSafetyFloorOwner;
        public int previousSafetyFloorOwnerSurfaceIdentity;
        public string previousSafetyFloorOwnerPathIdentity;
        public string safetyFloorOwner;
        public int safetyFloorOwnerSurfaceIdentity;
        public string safetyFloorOwnerPathIdentity;
        public bool currentSafetyFloorAvailable;
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
