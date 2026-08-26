using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootLandingLegExtensionDiagnosisTests
    {
        [Test]
        public void TargetExtensionDeltaMatchesOnlyLandingDiagnosis()
        {
            CharacterFootDiagnosisDocument document =
                new CharacterFootLandingLegExtensionDiagnosis().Build(
                    Context(
                        "Landing",
                        new JObject
                        {
                            ["targetExtensionRatioDelta"] = 0.03d,
                            ["solvedBendDropDegrees"] = 0d,
                            ["solvedExtensionRatioPeak"] = 1d,
                            ["solvedBendDegreesMinimum"] = 0d
                        },
                        new JObject { ["bendDirectionReversed"] = false }));
            Assert.That(document.diagnosticId, Is.EqualTo("landing-leg-extension"));
            Assert.That(document.targets[0].matchedEventCount, Is.EqualTo(1));
        }

        static CharacterFootDiagnosisContext Context(
            string kind,
            JObject metrics,
            JObject evidence) =>
            new CharacterFootDiagnosisContext(new JObject
            {
                ["events"] = new JArray(new JObject
                {
                    ["kind"] = kind,
                    ["metrics"] = metrics,
                    ["evidence"] = evidence
                })
            });
    }
}
