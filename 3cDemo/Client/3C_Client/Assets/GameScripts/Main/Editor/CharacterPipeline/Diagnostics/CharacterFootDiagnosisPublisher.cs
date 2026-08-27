using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootDiagnosisPublication
    {
        internal CharacterFootDiagnosisPublication(
            string directory,
            int diagnosticCount,
            int targetCount,
            int matchCount,
            CharacterFootDiagnosisPrimaryResult primaryResult)
        {
            Directory = directory ?? string.Empty;
            DiagnosticCount = diagnosticCount;
            TargetCount = targetCount;
            MatchCount = matchCount;
            PrimaryResult = primaryResult;
        }

        internal string Directory { get; }
        internal int DiagnosticCount { get; }
        internal int TargetCount { get; }
        internal int MatchCount { get; }
        internal CharacterFootDiagnosisPrimaryResult PrimaryResult { get; }

        internal string FormatPrimarySummary()
        {
            if (PrimaryResult?.occurrence == null)
                return string.Empty;
            CharacterFootDiagnosisOccurrenceProfile occurrence =
                PrimaryResult.occurrence;
            if (!occurrence.available)
            {
                return $"{occurrence.metric}=unavailable " +
                       $"eligible{occurrence.sampleUnit}=0 ";
            }
            CharacterFootDiagnosisOccurrenceRate primary =
                occurrence.primaryRate ?? throw new InvalidOperationException(
                    "Foot diagnosis primary occurrence rate is unavailable.");
            CharacterFootDiagnosisDistribution amplitude =
                PrimaryResult.amplitudeDistribution ??
                throw new InvalidOperationException(
                    "Foot diagnosis amplitude distribution is unavailable.");
            if (!amplitude.available ||
                !amplitude.median.HasValue ||
                !amplitude.p90.HasValue ||
                !amplitude.p99.HasValue ||
                !amplitude.maximum.HasValue)
            {
                throw new InvalidOperationException(
                    "Foot diagnosis amplitude distribution is incomplete.");
            }
            string thresholdLabel = string.Equals(
                    occurrence.thresholdUnit,
                    "Meters",
                    StringComparison.Ordinal)
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.##}cm",
                    primary.threshold * 100d)
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.####}{1}",
                    primary.threshold,
                    occurrence.thresholdUnit);
            string occurrenceSummary = string.Format(
                CultureInfo.InvariantCulture,
                "{0}>{1}={2}/{3}={4:0.0}% " +
                "amplitudeMedian={5:0.####}m amplitudeP90={6:0.####}m " +
                "amplitudeP99={7:0.####}m amplitudeMax={8:0.####}m ",
                occurrence.metric,
                thresholdLabel,
                primary.matchedEventCount,
                primary.eligibleEventCount,
                primary.matchedEventRate * 100d,
                amplitude.median.Value,
                amplitude.p90.Value,
                amplitude.p99.Value,
                amplitude.maximum.Value);
            CharacterFootPathStageAnalysisCoverage stages =
                PrimaryResult.pathStageAnalysis;
            if (stages == null)
                return occurrenceSummary;
            if (stages.availableEventCount == 0)
            {
                return occurrenceSummary +
                       $"stageAnalysis=unavailable " +
                       $"stageEvents=0/{stages.eligibleEventCount} ";
            }
            string firstStages = string.Join(
                "|",
                stages.firstAmplificationStageCounts.Select(
                    value => $"{value.Key}:{value.Value}"));
            return occurrenceSummary +
                   $"stageEvents={stages.availableEventCount}/" +
                   $"{stages.eligibleEventCount} " +
                   $"firstAmplificationStages={firstStages} ";
        }
    }

    internal static class CharacterFootDiagnosisPublisher
    {
        const string FactsSchema = "character-foot-motion-facts/18";
        static readonly ICharacterFootDiagnosis[] s_Diagnoses =
        {
            new CharacterFootLandingLegExtensionDiagnosis(),
            new CharacterFootLandingStateConsistencyDiagnosis(),
            new CharacterFootLandingPathContinuityDiagnosis(),
            new CharacterFootLockedSoleMotionDiagnosis(),
            new CharacterFootSwingPathJitterDiagnosis(),
            new CharacterFootSafetyFloorDiagnosis(),
            new CharacterFootSwingCurrentFloorCatchupDiagnosis(),
            new CharacterFootContactPlanePenetrationDiagnosis(),
            new CharacterFootStepTimeCandidateSelectionDiagnosis()
        };

        internal static CharacterFootDiagnosisPublication Publish(string factsPath)
        {
            if (string.IsNullOrWhiteSpace(factsPath) || !File.Exists(factsPath))
                throw new FileNotFoundException("Foot Motion facts file is unavailable.", factsPath);
            string fullFactsPath = Path.GetFullPath(factsPath);
            JObject facts = JObject.Parse(File.ReadAllText(fullFactsPath, Encoding.UTF8));
            string factsSchema = facts.Value<string>("schema") ?? string.Empty;
            if (factsSchema != FactsSchema)
            {
                throw new InvalidDataException(
                    $"Foot diagnosis facts schema '{factsSchema}' is invalid; " +
                    $"expected '{FactsSchema}'.");
            }
            var context = new CharacterFootDiagnosisContext(facts);
            string parent = Path.GetDirectoryName(fullFactsPath) ?? string.Empty;
            string directory = Path.Combine(parent, "diagnoses");
            string staging = Path.Combine(
                parent,
                $"diagnoses.part-{Guid.NewGuid():N}");
            Directory.CreateDirectory(staging);
            int targetCount = 0;
            int matchCount = 0;
            CharacterFootDiagnosisPrimaryResult primaryResult = null;
            try
            {
                string factsHash = ComputeSha256(fullFactsPath);
                for (int i = 0; i < s_Diagnoses.Length; i++)
                {
                    ICharacterFootDiagnosis diagnosis = s_Diagnoses[i];
                    CharacterFootDiagnosisDocument document = diagnosis.Build(context);
                    document.facts.sha256 = factsHash;
                    targetCount += document.summary.targetCount;
                    matchCount += document.summary.matchedEventCount;
                    if (document.summary.primaryResult != null)
                    {
                        if (primaryResult != null)
                        {
                            throw new InvalidOperationException(
                                "Foot diagnosis publication has multiple primary occurrence results.");
                        }
                        primaryResult = document.summary.primaryResult;
                    }
                    PublishFile(
                        Path.Combine(staging, diagnosis.FileName),
                        document);
                }
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
                Directory.Move(staging, directory);
            }
            catch
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, true);
                throw;
            }
            return new CharacterFootDiagnosisPublication(
                directory,
                s_Diagnoses.Length,
                targetCount,
                matchCount,
                primaryResult);
        }

        static void PublishFile(
            string path,
            CharacterFootDiagnosisDocument document)
        {
            using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                65536,
                FileOptions.SequentialScan);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            using var json = new JsonTextWriter(writer)
            {
                Formatting = Formatting.Indented,
                Culture = CultureInfo.InvariantCulture
            };
            JsonSerializer serializer = JsonSerializer.Create(
                new JsonSerializerSettings
                {
                    Culture = CultureInfo.InvariantCulture,
                    NullValueHandling = NullValueHandling.Ignore
                });
            serializer.Serialize(json, document);
            json.Flush();
            writer.Flush();
            stream.Flush(true);
        }

        static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return string.Concat(
                sha.ComputeHash(stream)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
