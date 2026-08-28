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
            Func<JObject, List<string>> match = value =>
            {
                var rules = new List<string>();
                if (!CharacterFootDiagnosisContext.Evidence(
                        value,
                        "cacheStateConsistent"))
                    rules.Add("cacheStateConsistent=false");
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
                "FutureLanding是否只在累计输入或强制lineage变化时查询，并让复用Observation保持不可变结果",
                new[] { "LandingObservation" },
                new[]
                {
                    "cacheStateConsistent=false",
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
            return context.Document(DiagnosticId, target);
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
