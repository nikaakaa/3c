using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootLockTransitionFlybackDiagnosisTests
    {
        [Test]
        public void ReversedReleaseExcursionMatchesOnlyFlybackDiagnosis()
        {
            var facts = new JObject
            {
                ["events"] = new JArray(new JObject
                {
                    ["kind"] = "Release",
                    ["metrics"] = new JObject
                    {
                        ["correctionStepMaximumMeters"] = 0.005d,
                        ["correctionExcursionMeters"] = 0.02d,
                        ["velocityDirectionReversalCount"] = 1d
                    },
                    ["evidence"] = new JObject()
                })
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootLockTransitionFlybackDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            Assert.That(document.diagnosticId, Is.EqualTo("lock-transition-flyback"));
            Assert.That(document.targets[0].matchedEventCount, Is.EqualTo(1));
        }
    }
}
