using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLandingLegExtensionDiagnosis : ICharacterFootDiagnosis
    {
        const double ExtensionDelta = 0.02d;
        const double BendDropDegrees = 5d;

        public string DiagnosticId => "landing-leg-extension";
        public string FileName => "landing-leg-extension.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("Landing");
            CharacterFootDiagnosisTarget target = context.Target(
                DiagnosticId,
                "Landing阶段是否出现腿继续伸直或弯曲方向反转",
                new[] { "Landing" },
                new[]
                {
                    "targetExtensionRatioDelta>0.02",
                    "solvedBendDropDegrees>5",
                    "bendDirectionReversed=true"
                },
                events,
                value =>
                {
                    var rules = new List<string>(3);
                    if (CharacterFootDiagnosisContext.Metric(
                            value,
                            "targetExtensionRatioDelta") > ExtensionDelta)
                    {
                        rules.Add("targetExtensionRatioDelta>0.02");
                    }
                    if (CharacterFootDiagnosisContext.Metric(
                            value,
                            "solvedBendDropDegrees") > BendDropDegrees)
                    {
                        rules.Add("solvedBendDropDegrees>5");
                    }
                    if (CharacterFootDiagnosisContext.Evidence(
                            value,
                            "bendDirectionReversed"))
                    {
                        rules.Add("bendDirectionReversed=true");
                    }
                    return rules;
                },
                value => Math.Max(
                    Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "targetExtensionRatioDelta") / ExtensionDelta,
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "solvedBendDropDegrees") / BendDropDegrees),
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "bendDirectionReversed")
                        ? 1d
                        : 0d),
                "targetExtensionRatioDelta",
                "solvedBendDropDegrees",
                "solvedExtensionRatioPeak",
                "solvedBendDegreesMinimum");
            return context.Document(DiagnosticId, target);
        }
    }
}
