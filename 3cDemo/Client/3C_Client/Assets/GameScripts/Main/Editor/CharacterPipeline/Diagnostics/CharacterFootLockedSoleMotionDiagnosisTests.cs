using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootLockedSoleMotionDiagnosisTests
    {
        [Test]
        public void StableAnchorDistanceGrowthMatchesLockedDiagnosis()
        {
            var facts = new JObject
            {
                ["events"] = new JArray(new JObject
                {
                    ["kind"] = "Locked",
                    ["metrics"] = new JObject
                    {
                        ["soleDownwardExcursionMeters"] = 0d,
                        ["correctedSoleAnchorDistanceChangeMeters"] = 0.02d,
                        ["visibleSoleStepMaximumMeters"] = 0.02d,
                        ["anchorDisplacementMeters"] = 0d
                    },
                    ["evidence"] = new JObject { ["anchorStable"] = true }
                })
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootLockedSoleMotionDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            Assert.That(document.diagnosticId, Is.EqualTo("locked-sole-motion"));
            Assert.That(document.targets[0].matchedEventCount, Is.EqualTo(1));
        }
    }
}
