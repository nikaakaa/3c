using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLockedSoleMotionDiagnosis : ICharacterFootDiagnosis
    {
        const double SinkMeters = 0.005d;
        const double DriftMeters = 0.01d;

        public string DiagnosticId => "locked-sole-motion";
        public string FileName => "locked-sole-motion.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> fullAnchorEvents = context.Events("LockedFullAnchor");
            CharacterFootDiagnosisTarget fullAnchorTarget = context.Target(
                "locked-sole-sink-or-drift",
                "FullAnchor子段脚底是否相对稳定Anchor下陷或水平漂移",
                new[] { "LockedFullAnchor" },
                new[]
                {
                    "soleDownwardExcursionMeters>0.005",
                    "anchorStable=true&&correctedSoleAnchorHorizontalDistanceMaximumMeters>0.01"
                },
                fullAnchorEvents,
                value =>
                {
                    var rules = new List<string>(2);
                    if (CharacterFootDiagnosisContext.Metric(
                            value,
                            "soleDownwardExcursionMeters") > SinkMeters)
                    {
                        rules.Add("soleDownwardExcursionMeters>0.005");
                    }
                    if (CharacterFootDiagnosisContext.Evidence(
                            value,
                            "anchorStable") &&
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "correctedSoleAnchorHorizontalDistanceMaximumMeters") > DriftMeters)
                    {
                        rules.Add(
                            "anchorStable=true&&correctedSoleAnchorHorizontalDistanceMaximumMeters>0.01");
                    }
                    return rules;
                },
                value => Math.Max(
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "soleDownwardExcursionMeters") / SinkMeters,
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "correctedSoleAnchorHorizontalDistanceMaximumMeters") / DriftMeters),
                "soleDownwardExcursionMeters",
                "correctedSoleAnchorHorizontalDistanceMaximumMeters",
                "visibleSoleStepMaximumMeters",
                "anchorDisplacementMeters");
            List<JObject> slidingEvents = context.Events("LockedSliding");
            CharacterFootDiagnosisTarget slidingTarget = context.Target(
                "locked-sliding-vertical-anchor",
                "Sliding子段是否向垂直Anchor下方下陷；水平离锚距离与输出步长只发布事实，不在缺少正式距离政策时判定",
                new[] { "LockedSliding" },
                new[]
                {
                    "soleDownwardExcursionMeters>0.005"
                },
                slidingEvents,
                value =>
                {
                    var rules = new List<string>(1);
                    if (CharacterFootDiagnosisContext.Metric(
                            value,
                            "soleDownwardExcursionMeters") > SinkMeters)
                    {
                        rules.Add(
                            "soleDownwardExcursionMeters>0.005");
                    }
                    return rules;
                },
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "soleDownwardExcursionMeters") / SinkMeters,
                "soleAlongUpAbsoluteMaximumMeters",
                "soleDownwardExcursionMeters",
                "correctedSoleAnchorHorizontalDistanceMaximumMeters",
                "visibleSoleStepMaximumMeters",
                "anchorDisplacementMeters");
            return context.Document(
                DiagnosticId,
                fullAnchorTarget,
                slidingTarget);
        }
    }
}
