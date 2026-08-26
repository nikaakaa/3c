using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootSafetyFloorDiagnosis :
        ICharacterFootDiagnosis
    {
        public string DiagnosticId => "safety-floor-current-ground";
        public string FileName => "safety-floor-current-ground.json";

        public CharacterFootDiagnosisDocument Build(
            CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("SafetyFloor");
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
            return context.Document(
                DiagnosticId,
                clearance,
                missingInput,
                largeClamp,
                source,
                contract);
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
}
