using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootSwingPathJitterDiagnosisTests
    {
        [Test]
        public void VisibleOutputOffsetStepDrivesOccurrence()
        {
            var facts = new JObject
            {
                ["events"] = new JArray(
                    Event("StableSwingOutputJump", 10, 0.03d))
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootSwingPathJitterDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            CharacterFootDiagnosisTarget target = document.targets[0];
            Assert.That(target.id, Is.EqualTo(
                "stable-swing-output-jump"));
            Assert.That(target.eligibleEventCount, Is.EqualTo(1));
            Assert.That(target.matchedEventCount, Is.EqualTo(1));
            Assert.That(target.occurrence.rates.Count, Is.EqualTo(4));
            Assert.That(
                target.occurrence.rates[1].matchedEventCount,
                Is.EqualTo(1));
            Assert.That(
                target.measurements["ObservedSwingTargetDelta"].maximum,
                Is.EqualTo(0.2d));
        }

        [Test]
        public void OutputCategoriesRemainIndependent()
        {
            var facts = new JObject
            {
                ["events"] = new JArray(
                    Event("StableSwingOutputJump", 10, 0.015d),
                    Event("PathRevisionOutputJump", 11, 0.08d),
                    Event("SwingToLandingOutputJump", 12, 0.025d))
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootSwingPathJitterDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            Assert.That(document.targets.Count, Is.EqualTo(4));
            Assert.That(document.targets[0].eligibleEventCount, Is.EqualTo(1));
            Assert.That(document.targets[1].eligibleEventCount, Is.EqualTo(1));
            Assert.That(document.targets[2].eligibleEventCount, Is.EqualTo(1));
            Assert.That(document.targets[0].matchedEventCount, Is.EqualTo(0));
            Assert.That(document.targets[1].matchedEventCount, Is.EqualTo(1));
            Assert.That(document.targets[2].matchedEventCount, Is.EqualTo(1));
            Assert.That(
                document.targets[3].id,
                Is.EqualTo("swing-actual-foot-envelope-counterfactual"));
        }

        [Test]
        public void EmptyCategoryPublishesUnavailableOccurrence()
        {
            var facts = new JObject
            {
                ["events"] = new JArray()
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootSwingPathJitterDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            foreach (CharacterFootDiagnosisTarget target in document.targets)
            {
                Assert.That(target.eligibleEventCount, Is.EqualTo(0));
                Assert.That(target.matchedEventRateAvailable, Is.False);
                Assert.That(target.matchedEventRate, Is.Null);
                Assert.That(target.occurrence.available, Is.False);
                Assert.That(target.occurrence.rates, Is.Empty);
            }
        }

        static JObject Event(string kind, int frame, double outputStep) =>
            new JObject
            {
                ["kind"] = kind,
                ["side"] = "Left",
                ["startFrame"] = frame - 1,
                ["endFrame"] = frame,
                ["peakFrame"] = frame,
                ["metrics"] = new JObject
                {
                    ["FootPlacementOutputOffsetStep"] = outputStep,
                    ["FootPlacementOutputOffsetSpeed"] = outputStep / 0.02d,
                    ["FootPlacementOutputOffsetAcceleration"] = 0d,
                    ["FootPlacementOutputOffsetJerk"] = 0d,
                    ["ObservedSwingTargetDelta"] = 0.2d
                },
                ["evidence"] = new JObject
                {
                    ["accelerationAvailable"] = false,
                    ["jerkAvailable"] = false
                },
                ["visibleOutputJump"] = new JObject
                {
                    ["primaryProbe"] = "Ankle",
                    ["safetyFloorOwner"] = "GroundPathEnvelope",
                    ["pathRevisionReason"] = "None"
                }
            };
    }
}
