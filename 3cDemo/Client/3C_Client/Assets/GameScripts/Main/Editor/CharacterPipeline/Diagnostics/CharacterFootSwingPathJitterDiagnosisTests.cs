using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootSwingPathJitterDiagnosisTests
    {
        [Test]
        public void UnanchoredCorrectionStepMatchesSwingPathDiagnosis()
        {
            var facts = new JObject
            {
                ["events"] = new JArray(new JObject
                {
                    ["kind"] = "PathChange",
                    ["metrics"] = new JObject
                    {
                        ["correctionStepMaximumMeters"] = 0.03d,
                        ["nextLandingEndpointDeltaMeters"] = 0.03d,
                        ["correctionExcursionMeters"] = 0.03d,
                        ["correctionJerkMetersPerSecondCubed"] = 10d
                    },
                    ["evidence"] = new JObject()
                })
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootSwingPathJitterDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            Assert.That(document.diagnosticId, Is.EqualTo("swing-path-jitter"));
            Assert.That(document.targets[0].matchedEventCount, Is.EqualTo(1));
        }
    }
}
