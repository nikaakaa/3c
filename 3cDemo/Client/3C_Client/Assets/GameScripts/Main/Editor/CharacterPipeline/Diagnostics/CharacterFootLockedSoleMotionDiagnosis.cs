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
            List<JObject> events = context.Events("Locked");
            CharacterFootDiagnosisTarget target = context.Target(
                "locked-sole-sink-or-drift",
                "Locked阶段脚底是否相对稳定Anchor下陷或漂移",
                new[] { "Locked" },
                new[]
                {
                    "soleDownwardExcursionMeters>0.005",
                    "anchorStable=true&&correctedSoleAnchorDistanceChangeMeters>0.01"
                },
                events,
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
                            "correctedSoleAnchorDistanceChangeMeters") > DriftMeters)
                    {
                        rules.Add(
                            "anchorStable=true&&correctedSoleAnchorDistanceChangeMeters>0.01");
                    }
                    return rules;
                },
                value => Math.Max(
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "soleDownwardExcursionMeters") / SinkMeters,
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "correctedSoleAnchorDistanceChangeMeters") / DriftMeters),
                "soleDownwardExcursionMeters",
                "correctedSoleAnchorDistanceChangeMeters",
                "visibleSoleStepMaximumMeters",
                "anchorDisplacementMeters");
            return context.Document(DiagnosticId, target);
        }
    }
}
