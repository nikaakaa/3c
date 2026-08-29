using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootLandingStateConsistencyDiagnosisTests
    {
        [Test]
        public void FormalBoundaryWithoutRuntimeLandingMatchesMissedEntry()
        {
            var facts = new JObject
            {
                ["events"] = new JArray(new JObject
                {
                    ["kind"] = "LandingStateBoundary",
                    ["metrics"] = new JObject
                    {
                        ["formalStepTimeSeconds"] = 0d,
                        ["correctionStepMeters"] = 0.02d,
                        ["finalSoleStepMeters"] = 0.02d
                    },
                    ["evidence"] = new JObject
                    {
                        ["runtimeLandingAtBoundary"] = false,
                        ["runtimeLockedAtBoundary"] = false
                    }
                })
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootLandingStateConsistencyDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            Assert.That(document.diagnosticId, Is.EqualTo("landing-state-consistency"));
            Assert.That(document.targets[0].matchedEventCount, Is.EqualTo(1));
        }

        [Test]
        public void ReversedReleaseExcursionMatchesReleaseFlyback()
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
                new CharacterFootLandingStateConsistencyDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            CharacterFootDiagnosisTarget release =
                document.targets.Find(value =>
                    value.id == "release-flyback");
            Assert.That(
                release.id,
                Is.EqualTo("release-flyback"));
            Assert.That(
                release.matchedEventCount,
                Is.EqualTo(1));
        }
    }
}
