using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLockedSoleMotionDiagnosis : ICharacterFootDiagnosis
    {
        public string DiagnosticId => "locked-sole-motion";
        public string FileName => "locked-sole-motion.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> fullAnchor = context.Events("LockedFullAnchor");
            List<JObject> physical = fullAnchor.Where(value =>
                CharacterFootDiagnosisContext.Evidence(value, "physicalAnchorAvailable") &&
                CharacterFootDiagnosisContext.Evidence(value, "anchorStable")).ToList();
            const string metric = "physicalSoleAnchorHorizontalDistanceMaximumMeters";
            CharacterFootDiagnosisTarget horizontal = context.Target(
                "locked-horizontal-drift",
                "FullAnchor子段最终物理Sole是否相对稳定Anchor水平漂移；不把Sliding正常移动或垂直穿透重复计入",
                new[] { "LockedFullAnchor" }, new[] { metric + ">0.01" }, physical,
                value => CharacterFootDiagnosisContext.Metric(value, metric) > 0.01d
                    ? new List<string> { metric + ">0.01" } : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(value, metric),
                metric, "correctedSoleAnchorHorizontalDistanceMaximumMeters",
                "visibleSoleStepMaximumMeters", "anchorDisplacementMeters");
            horizontal.scorePolicy = "Health";
            horizontal.occurrence = context.Occurrence(
                "ContinuousFullAnchorPhysicalSoleInterval", metric, "Meters", physical,
                0.01d, 0.01d, 0.02d, 0.05d, 0.1d);
            var locked = new List<JObject>(fullAnchor);
            locked.AddRange(context.Events("LockedSliding"));
            CharacterFootDiagnosisTarget vertical = context.Target(
                "locked-vertical-anchor-evidence",
                "FullAnchor与Sliding沿Up的Anchor下陷及响应类型证据；最终穿透由统一穿透Target计分",
                new[] { "LockedFullAnchor", "LockedSliding" },
                new[] { "soleDownwardExcursionMeters>0.005" }, locked,
                value => CharacterFootDiagnosisContext.Metric(value,
                    "soleDownwardExcursionMeters") > 0.005d
                    ? new List<string> { "soleDownwardExcursionMeters>0.005" }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(value, "soleDownwardExcursionMeters"),
                "soleDownwardExcursionMeters", "soleAlongUpAbsoluteMaximumMeters",
                "correctedSoleAnchorHorizontalDistanceMaximumMeters",
                "physicalSoleAnchorHorizontalDistanceMaximumMeters",
                "visibleSoleStepMaximumMeters", "anchorDisplacementMeters");
            vertical.categoricalMeasurements =
                new SortedDictionary<string, List<CharacterFootDiagnosisCategoryCount>>
                {
                    ["LockResponse"] = locked.GroupBy(value => value.Value<string>("kind"))
                        .OrderBy(group => group.Key)
                        .Select(group => new CharacterFootDiagnosisCategoryCount
                            { value = group.Key, count = group.Count() }).ToList()
                };
            return context.Document(DiagnosticId, horizontal, vertical);
        }
    }
}
