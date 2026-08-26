using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLockTransitionFlybackDiagnosis : ICharacterFootDiagnosis
    {
        const double AcquireStepMeters = 0.01d;
        const double ReleaseExcursionMeters = 0.01d;

        public string DiagnosticId => "lock-transition-flyback";
        public string FileName => "lock-transition-flyback.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("Landing", "Release");
            CharacterFootDiagnosisTarget target = context.Target(
                DiagnosticId,
                "进入或退出Lock时是否出现突跳后反向回拉",
                new[] { "Landing", "Release" },
                new[]
                {
                    "Landing.correctionStepMaximumMeters>0.01",
                    "Release.velocityDirectionReversalCount>0&&correctionExcursionMeters>0.01"
                },
                events,
                value =>
                {
                    var rules = new List<string>(2);
                    string kind = value.Value<string>("kind") ?? string.Empty;
                    if (kind == "Landing" &&
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "correctionStepMaximumMeters") > AcquireStepMeters)
                    {
                        rules.Add("Landing.correctionStepMaximumMeters>0.01");
                    }
                    if (kind == "Release" &&
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "velocityDirectionReversalCount") > 0d &&
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "correctionExcursionMeters") > ReleaseExcursionMeters)
                    {
                        rules.Add(
                            "Release.velocityDirectionReversalCount>0&&correctionExcursionMeters>0.01");
                    }
                    return rules;
                },
                value => value.eventKind == "Landing"
                    ? CharacterFootDiagnosisContext.Metric(
                          value,
                          "correctionStepMaximumMeters") / AcquireStepMeters
                    : Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "correctionExcursionMeters") /
                        ReleaseExcursionMeters,
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "velocityDirectionReversalCount")),
                "correctionStepMaximumMeters",
                "correctionExcursionMeters",
                "velocityDirectionReversalCount");
            return context.Document(DiagnosticId, target);
        }
    }
}
