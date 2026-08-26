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
            return context.Document(
                DiagnosticId,
                BuildTarget(
                    context,
                    events,
                    "source-contact-plane-penetration",
                    "Foot Placement处理前的Heel-Toe接触线是否已进入正式接触平面",
                    "sourceDepthMaximumMeters",
                    "sourceDepthMaximumMeters>0.00001",
                    "sourceHeelDepthMaximumMeters",
                    "sourceToeDepthMaximumMeters",
                    "sourceLengthCoefficientMaximum"),
                BuildTarget(
                    context,
                    events,
                    "foot-placement-introduced-contact-plane-penetration",
                    "当前Foot Placement与最终IK是否新增接触平面侵入",
                    "introducedDepthMaximumMeters",
                    "introducedDepthMaximumMeters>0.00001",
                    "sourceDepthMaximumMeters",
                    "finalDepthMaximumMeters",
                    "introducedFrameCount"),
                context.Target(
                    "foot-placement-amplified-contact-plane-penetration",
                    "当前Foot Placement与最终IK是否加重动画源已有侵入",
                    new[] { "ContactPlanePenetration" },
                    new[] { "amplifiedFrameCount>0" },
                    events,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "amplifiedFrameCount") > 0d
                        ? new List<string> { "amplifiedFrameCount>0" }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "introducedDepthMaximumMeters"),
                    "amplifiedFrameCount",
                    "introducedDepthMaximumMeters",
                    "sourceDepthMaximumMeters",
                    "finalDepthMaximumMeters"),
                BuildTarget(
                    context,
                    events,
                    "unresolved-toe-contact-plane-penetration",
                    "最终Toe接触探针是否仍进入接触平面，仅记录视觉残留不自动归责",
                    "finalToeDepthMaximumMeters",
                    "finalToeDepthMaximumMeters>0.00001",
                    "sourceToeDepthMaximumMeters",
                    "introducedDepthMaximumMeters",
                    "baselineResidualFrameCount"),
                BuildTarget(
                    context,
                    events,
                    "final-heel-contact-plane-penetration",
                    "最终Heel接触探针是否进入正式接触平面",
                    "finalHeelDepthMaximumMeters",
                    "finalHeelDepthMaximumMeters>0.00001",
                    "sourceHeelDepthMaximumMeters",
                    "finalLengthCoefficientMaximum",
                    "finalDepthTimeIntegralMeterSeconds"));
        }

        static CharacterFootDiagnosisTarget BuildTarget(
            CharacterFootDiagnosisContext context,
            List<JObject> events,
            string id,
            string question,
            string metric,
            string rule,
            params string[] additionalMetrics)
        {
            var measurements = new List<string> { metric };
            measurements.AddRange(additionalMetrics);
            return context.Target(
                id,
                question,
                new[] { "ContactPlanePenetration" },
                new[] { rule },
                events,
                value => CharacterFootDiagnosisContext.Metric(value, metric) >
                         CharacterFootContactPlanePenetration.GeometryEpsilonMeters
                    ? new List<string> { rule }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(value, metric),
                measurements.ToArray());
        }
    }
}
