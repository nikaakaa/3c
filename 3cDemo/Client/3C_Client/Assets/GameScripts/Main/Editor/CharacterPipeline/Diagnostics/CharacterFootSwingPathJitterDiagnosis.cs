using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootSwingPathJitterDiagnosis : ICharacterFootDiagnosis
    {
        const double CorrectionStepMeters = 0.02d;

        public string DiagnosticId => "swing-path-jitter";
        public string FileName => "swing-path-jitter.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("PathChange");
            CharacterFootDiagnosisTarget target = context.Target(
                "path-change-correction-jump",
                "无Anchor Swing的Ground Path变化附近是否出现修正跳变",
                new[] { "PathChange" },
                new[] { "correctionStepMaximumMeters>0.02" },
                events,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "correctionStepMaximumMeters") > CorrectionStepMeters
                    ? new List<string> { "correctionStepMaximumMeters>0.02" }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "correctionStepMaximumMeters") /
                         CorrectionStepMeters,
                "nextLandingEndpointDeltaMeters",
                "correctionStepMaximumMeters",
                "correctionExcursionMeters",
                "correctionJerkMetersPerSecondCubed");
            return context.Document(DiagnosticId, target);
        }
    }
}
