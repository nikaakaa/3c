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
            int matchCount)
        {
            Directory = directory ?? string.Empty;
            DiagnosticCount = diagnosticCount;
            TargetCount = targetCount;
            MatchCount = matchCount;
        }

        internal string Directory { get; }
        internal int DiagnosticCount { get; }
        internal int TargetCount { get; }
        internal int MatchCount { get; }
    }

    internal static class CharacterFootDiagnosisPublisher
    {
        const string FactsSchema = "character-foot-motion-facts/53";
        static readonly ICharacterFootDiagnosis[] s_Diagnoses =
        {
            new CharacterFootLandingLegExtensionDiagnosis(),
            new CharacterFootLandingStateConsistencyDiagnosis(),
            new CharacterFootLandingPathContinuityDiagnosis(),
            new CharacterFootLockedSoleMotionDiagnosis(),
            new CharacterFootSwingPathJitterDiagnosis(),
            new CharacterFootLandingObservationReuseDiagnosis(),
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
            try
            {
                string factsHash = ComputeSha256(fullFactsPath);
                var documents = new SortedDictionary<string, CharacterFootDiagnosisDocument>(
                    StringComparer.Ordinal);
                for (int i = 0; i < s_Diagnoses.Length; i++)
                {
                    ICharacterFootDiagnosis diagnosis = s_Diagnoses[i];
                    CharacterFootDiagnosisDocument document = diagnosis.Build(context);
                    document.facts.sha256 = factsHash;
                    documents.Add(diagnosis.FileName, document);
                    targetCount += document.summary.targetCount;
                    matchCount += document.summary.matchedEventCount;
                    PublishFile(
                        Path.Combine(staging, diagnosis.FileName),
                        document);
                }
                CharacterFootQualityScorecard quality =
                    CharacterFootDiagnosisScoring.BuildQualityScorecard(
                        documents, documents[s_Diagnoses[0].FileName].facts);
                PublishFile(Path.Combine(staging, "quality-score.json"), quality);
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
                s_Diagnoses.Length + 1,
                targetCount,
                matchCount);
        }

        static void PublishFile(
            string path,
            object document)
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
