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
                ["events"] = new JArray(Event(10, 12, false))
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootSwingPathJitterDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            Assert.That(document.diagnosticId, Is.EqualTo("swing-path-jitter"));
            Assert.That(document.targets[0].matchedEventCount, Is.EqualTo(1));
            Assert.That(document.schema, Is.EqualTo("character-foot-diagnosis-file/2"));
            CharacterFootDiagnosisOccurrenceProfile occurrence =
                document.summary.primaryResult.occurrence;
            Assert.That(occurrence.available, Is.True);
            Assert.That(occurrence.eligibleEventCount, Is.EqualTo(1));
            Assert.That(occurrence.rates.Count, Is.EqualTo(4));
            Assert.That(occurrence.rates[0].threshold, Is.EqualTo(0.01d));
            Assert.That(occurrence.rates[0].matchedEventCount, Is.EqualTo(1));
            Assert.That(occurrence.rates[0].matchedEventRate, Is.EqualTo(1d));
            Assert.That(occurrence.rates[1].threshold, Is.EqualTo(0.02d));
            Assert.That(occurrence.rates[1].matchedEventCount, Is.EqualTo(1));
            Assert.That(occurrence.rates[2].matchedEventCount, Is.EqualTo(0));
            Assert.That(occurrence.rates[3].matchedEventCount, Is.EqualTo(0));
            Assert.That(
                document.summary.primaryResult.amplitudeDistribution.p90,
                Is.EqualTo(0.03d));
            CharacterFootPathStageAnalysisCoverage stageCoverage =
                document.summary.primaryResult.pathStageAnalysis;
            Assert.That(stageCoverage.available, Is.False);
            Assert.That(stageCoverage.eligibleEventCount, Is.EqualTo(1));
            Assert.That(stageCoverage.availableEventCount, Is.EqualTo(0));
            Assert.That(stageCoverage.unavailableEventCount, Is.EqualTo(1));
            Assert.That(
                stageCoverage.missingStageCounts[
                    CharacterFootPathStageNames.RawLandingToPathTarget],
                Is.EqualTo(1));
            Assert.That(
                document.targets[0].representativeEvents[0]
                    .pathStageAnalysis.available,
                Is.False);
        }

        [Test]
        public void OverlappingPeakIsCountedOnceAndAnchoredEventIsExcluded()
        {
            var facts = new JObject
            {
                ["events"] = new JArray(
                    Event(10, 12, false),
                    Event(11, 12, false),
                    Event(20, 22, true))
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootSwingPathJitterDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            CharacterFootDiagnosisTarget target = document.targets[0];
            Assert.That(target.eligibleEventCount, Is.EqualTo(1));
            Assert.That(target.matchedEventCount, Is.EqualTo(1));
            Assert.That(
                target.measurements["semanticPathChangeCount"].maximum,
                Is.EqualTo(2d));
        }

        [Test]
        public void NoPathChangeEventsPublishesUnavailableOccurrence()
        {
            var facts = new JObject
            {
                ["events"] = new JArray()
            };
            CharacterFootDiagnosisDocument document =
                new CharacterFootSwingPathJitterDiagnosis().Build(
                    new CharacterFootDiagnosisContext(facts));
            CharacterFootDiagnosisTarget target = document.targets[0];
            CharacterFootDiagnosisOccurrenceProfile occurrence =
                document.summary.primaryResult.occurrence;
            Assert.That(target.eligibleEventCount, Is.EqualTo(0));
            Assert.That(target.matchedEventRateAvailable, Is.False);
            Assert.That(target.matchedEventRate, Is.Null);
            Assert.That(occurrence.available, Is.False);
            Assert.That(occurrence.eligibleEventCount, Is.EqualTo(0));
            Assert.That(occurrence.configuredThresholds.Count, Is.EqualTo(4));
            Assert.That(occurrence.rates, Is.Empty);
            Assert.That(
                document.summary.primaryResult.amplitudeDistribution.available,
                Is.False);
            Assert.That(
                document.summary.primaryResult.pathStageAnalysis
                    .eligibleEventCount,
                Is.EqualTo(0));
        }

        static JObject Event(int startFrame, int peakFrame, bool anchored) =>
            new JObject
            {
                ["kind"] = "PathChange",
                ["side"] = "Left",
                ["startFrame"] = startFrame,
                ["endFrame"] = startFrame + 1,
                ["peakFrame"] = peakFrame,
                ["metrics"] = new JObject
                {
                    ["correctionStepMaximumMeters"] = 0.03d,
                    ["nextLandingEndpointDeltaMeters"] = 0.03d,
                    ["correctionExcursionMeters"] = 0.03d,
                    ["correctionJerkMetersPerSecondCubed"] = 10d
                },
                ["evidence"] = new JObject
                {
                    ["unanchoredSwingEligible"] = true,
                    ["anchorAvailable"] = anchored
                }
            };
    }
}
