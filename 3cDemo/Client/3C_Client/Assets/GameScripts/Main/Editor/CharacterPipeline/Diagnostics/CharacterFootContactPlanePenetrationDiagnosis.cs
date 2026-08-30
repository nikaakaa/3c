using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootContactPlanePenetrationDiagnosis : ICharacterFootDiagnosis
    {
        public string DiagnosticId => "contact-plane-penetration";
        public string FileName => "contact-plane-penetration.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("ContactPlanePenetration");
            CharacterFootDiagnosisTarget final = context.Target(
                "final-contact-plane-penetration",
                "最终物理Heel/Toe是否穿入正式接触平面；同一接触段取最大深度只计一次，原动画及Foot贡献另作证据",
                new[] { "ContactPlanePenetration" }, new[] { "finalDepthMaximumMeters>0.01" },
                events,
                value => CharacterFootDiagnosisContext.Metric(value,
                    "finalDepthMaximumMeters") > 0.01d
                    ? new List<string> { "finalDepthMaximumMeters>0.01" } : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(value, "finalDepthMaximumMeters"),
                "finalDepthMaximumMeters", "finalHeelDepthMaximumMeters",
                "finalToeDepthMaximumMeters", "finalDepthTimeIntegralMeterSeconds",
                "finalPenetratingFrameRatio", "finalLengthCoefficientMaximum",
                "sourceDepthMaximumMeters", "introducedDepthMaximumMeters");
            final.scorePolicy = "Health";
            final.occurrence = context.Occurrence(
                "ContactPlanePenetrationInterval", "finalDepthMaximumMeters", "Meters",
                events, 0.01d, 0.01d, 0.02d, 0.05d, 0.1d);
            CharacterFootDiagnosisTarget contribution = context.Target(
                "contact-plane-penetration-contribution",
                "穿透来自原动画残留、Foot新增还是Foot加重；这些证据不与最终穿透重复扣分",
                new[] { "ContactPlanePenetration" },
                new[] { "sourcePenetrated=true", "introduced=true", "amplified=true" },
                events,
                value =>
                {
                    var rules = new List<string>();
                    foreach (string evidence in new[] { "sourcePenetrated", "introduced", "amplified" })
                        if (CharacterFootDiagnosisContext.Evidence(value, evidence))
                            rules.Add(evidence + "=true");
                    return rules;
                },
                value => CharacterFootDiagnosisContext.Metric(value, "introducedDepthMaximumMeters"),
                "sourceDepthMaximumMeters", "sourceHeelDepthMaximumMeters",
                "sourceToeDepthMaximumMeters", "introducedDepthMaximumMeters",
                "introducedFrameCount", "amplifiedFrameCount", "baselineResidualFrameCount",
                "resolvedFrameCount", "finalDepthMaximumMeters", "finalHeelDepthMaximumMeters",
                "finalToeDepthMaximumMeters");
            return context.Document(DiagnosticId, final, contribution);
        }
    }
}
