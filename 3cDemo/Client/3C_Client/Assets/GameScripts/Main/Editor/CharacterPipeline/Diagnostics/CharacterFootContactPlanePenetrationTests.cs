using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootContactPlanePenetrationTests
    {
        [TestCase(0.01d, 0.02d, 0d, 0d)]
        [TestCase(-0.01d, -0.01d, 1d, 0.01d)]
        [TestCase(-0.01d, 0.01d, 0.5d, 0.0025d)]
        [TestCase(0.01d, -0.01d, 0.5d, 0.0025d)]
        [TestCase(-0.005d, 0.015d, 0.25d, 0.000625d)]
        public void ResolveLineUsesExactPenetratingLengthAndDepth(
            double heelClearance,
            double toeClearance,
            double expectedCoefficient,
            double expectedMeanDepth)
        {
            CharacterFootContactLinePenetration result =
                CharacterFootContactPlanePenetration.ResolveLine(
                    heelClearance,
                    toeClearance);
            Assert.That(result.LengthCoefficient, Is.EqualTo(expectedCoefficient).Within(1e-12d));
            Assert.That(result.MeanDepth, Is.EqualTo(expectedMeanDepth).Within(1e-12d));
        }

        [TestCase(0d, 0d, 0)]
        [TestCase(0d, 0.01d, 1)]
        [TestCase(0.005d, 0.01d, 2)]
        [TestCase(0.01d, 0.005d, 3)]
        [TestCase(0.01d, 0d, 4)]
        [TestCase(0.01d, 0.01d, 5)]
        public void ResolveResponsibilitySeparatesSourceAndFinalDepth(
            double sourceDepth,
            double finalDepth,
            int expected)
        {
            CharacterFootContactPlanePenetrationResponsibility result =
                CharacterFootContactPlanePenetration.ResolveResponsibility(
                    sourceDepth,
                    finalDepth);
            Assert.That((int)result, Is.EqualTo(expected));
        }

        [Test]
        public void SealedSamplesPublishFactsAndTargetedDiagnosis()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"character-foot-penetration-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            try
            {
                string samplesPath = Path.Combine(directory, "samples.csv");
                string csvHeader = ResolveCsvHeader();
                string[] headers = csvHeader.Split(',');
                var indices = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < headers.Length; i++)
                    indices[headers[i]] = i;
                string[] row = new string[headers.Length];
                Array.Fill(row, "0");
                Set(row, indices, "SampleIdentity", "fixture");
                Set(row, indices, "ProgramIdentity", "program");
                Set(row, indices, "ProjectionRevision", "projection");
                Set(row, indices, "PoseGraphId", "graph");
                Set(row, indices, "PoseGraphRevision", "graph-revision");
                Set(row, indices, "PosePlanHash", "pose-plan");
                Set(row, indices, "FrameSequence", "1");
                Set(row, indices, "CompletionIdentity", "1");
                Set(row, indices, "Side", "Left");
                Set(row, indices, "PresentationDeltaSeconds", "0.016666667");
                Set(row, indices, "Grounded", "1");
                Set(row, indices, "InputFormalStepSourceIdentity", "source");
                Set(row, indices, "InputFormalLockMode", "Unlocked");
                Set(row, indices, "GroundPathState", "Accepted");
                Set(row, indices, "GroundPathComponentUpY", "1");
                Set(row, indices, "FootMotionState", "Accepted");
                Set(row, indices, "FootMotionConstraintState", "Swing");
                Set(
                    row,
                    indices,
                    "FootContactPlanePenetrationAvailability",
                    CharacterFootContactPlanePenetrationAvailability
                        .ContactLifecycleUnavailable.ToString());
                File.WriteAllLines(
                    samplesPath,
                    new[]
                    {
                        csvHeader,
                        string.Join(",", row)
                    });
                CharacterFootMotionDiagnosticAnalysis result =
                    CharacterFootMotionDiagnosticAnalyzer.Analyze(samplesPath);
                Assert.That(File.Exists(result.FactsPath), Is.True);
                Assert.That(File.Exists(result.DiagnosisPath), Is.True);
                JObject facts = JObject.Parse(File.ReadAllText(result.FactsPath));
                JObject diagnosis = JObject.Parse(File.ReadAllText(result.DiagnosisPath));
                Assert.That(
                    facts.Value<string>("schema"),
                    Is.EqualTo("character-foot-motion-facts/4"));
                Assert.That(
                    diagnosis.Value<string>("schema"),
                    Is.EqualTo("character-foot-motion-diagnosis/3"));
                Assert.That(
                    diagnosis["penetrationCoverage"]?
                        .Value<int>("availableFootRowCount"),
                    Is.EqualTo(0));
                Assert.That(
                    diagnosis["penetrationCoverage"]?["availabilityReasons"]?
                        .Value<int>("ContactLifecycleUnavailable"),
                    Is.EqualTo(1));
                string firstFacts = File.ReadAllText(result.FactsPath);
                string firstDiagnosis = File.ReadAllText(result.DiagnosisPath);
                CharacterFootMotionDiagnosticAnalyzer.Analyze(samplesPath);
                Assert.That(File.ReadAllText(result.FactsPath), Is.EqualTo(firstFacts));
                Assert.That(
                    File.ReadAllText(result.DiagnosisPath),
                    Is.EqualTo(firstDiagnosis));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        static void Set(
            string[] row,
            Dictionary<string, int> indices,
            string name,
            string value) => row[indices[name]] = value;

        static string ResolveCsvHeader() =>
            (string)typeof(CharacterFootLandingPredictionSampler)
                .GetField(
                    "Header",
                    BindingFlags.Static | BindingFlags.NonPublic)
                .GetRawConstantValue();
    }
}
