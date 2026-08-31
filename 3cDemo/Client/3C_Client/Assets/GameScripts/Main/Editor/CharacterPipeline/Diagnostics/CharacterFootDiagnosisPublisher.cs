using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        internal static CharacterFootDiagnosisPublication Publish(
            string outputDirectory,
            CharacterFootDiagnosisContext context,
            CharacterFootDiagnosticPerformance performance)
        {
            if (context == null ||
                context.FactsSchema != CharacterFootDiagnosticFormatIdentity.FactsSchema)
            {
                throw new InvalidDataException(
                    $"Foot diagnosis facts schema '{context?.FactsSchema}' is invalid; " +
                    $"expected '{CharacterFootDiagnosticFormatIdentity.FactsSchema}'.");
            }
            if (context.SourceIndices == null || context.SourceIndices.Count != 2)
                throw new InvalidDataException("Foot diagnostic source indices are unavailable.");
            string directory = Path.GetFullPath(outputDirectory);
            if (Directory.Exists(directory))
                throw new IOException("Foot diagnostic destination already exists.");
            string parent = Path.GetDirectoryName(directory);
            string staging = Path.Combine(
                parent,
                $"{Path.GetFileName(directory)}.part-{Guid.NewGuid():N}");
            Directory.CreateDirectory(staging);
            var timer = Stopwatch.StartNew();
            int targetCount = 0;
            int matchCount = 0;
            try
            {
                var documents = new SortedDictionary<string, CharacterFootDiagnosisDocument>(
                    StringComparer.Ordinal);
                for (int i = 0; i < s_Diagnoses.Length; i++)
                {
                    ICharacterFootDiagnosis diagnosis = s_Diagnoses[i];
                    CharacterFootDiagnosisDocument document = diagnosis.Build(context);
                    documents.Add(diagnosis.FileName, document);
                    targetCount += document.summary.targetCount;
                    matchCount += document.summary.matchedEventCount;
                }
                using (var store = new CharacterFootDiagnosticStore(staging,
                           context.Metadata("sample").Value<string>("identity")))
                {
                    context.WriteDetails(store);
                    foreach (CharacterFootDiagnosticSourceIndex source in context.SourceIndices)
                        source.file = Path.GetRelativePath(directory, source.file).Replace('\\', '/');
                    store.Complete(context.TargetIndices, context.SourceIndices);
                }
                CharacterFootQualityScorecard quality =
                    CharacterFootDiagnosisScoring.BuildQualityScorecard(
                        documents, documents[s_Diagnoses[0].FileName].facts);
                CharacterFootDiagnosticArtifact details = CharacterFootDiagnosticStore.Artifact(
                    staging, CharacterFootDiagnosticStore.DetailFileName);
                CharacterFootDiagnosticArtifact index = CharacterFootDiagnosticStore.Artifact(
                    staging, CharacterFootDiagnosticStore.IndexFileName);
                performance.detailBytes = details.bytes;
                performance.indexBytes = index.bytes;
                var manifest = new CharacterFootDiagnosticManifest
                {
                    factsSchema = context.FactsSchema,
                    sample = context.Metadata("sample"),
                    analyzer = context.Metadata("analyzer"),
                    coverage = context.Metadata("coverage"),
                    details = details,
                    index = index,
                    performance = performance
                };
                foreach (CharacterFootDiagnosisDocument document in documents.Values)
                {
                    document.facts.indexSha256 = index.sha256;
                    document.facts.indexFile = CharacterFootDiagnosticStore.IndexFileName;
                    document.facts.schema = CharacterFootDiagnosticStore.ManifestSchema;
                    document.facts.factsSchema = context.FactsSchema;
                }
                foreach (KeyValuePair<string, CharacterFootDiagnosisDocument> entry in documents)
                    PublishFile(Path.Combine(staging, entry.Key), Compact(entry.Value, context));
                PublishFile(Path.Combine(staging, "quality-score.json"), quality);
                performance.reportBytes = Directory.EnumerateFiles(staging, "*.json")
                    .Where(value => Path.GetFileName(value) != CharacterFootDiagnosticStore.IndexFileName)
                    .Sum(value => new FileInfo(value).Length);
                performance.publishMilliseconds = timer.Elapsed.TotalMilliseconds;
                PublishFile(Path.Combine(staging, CharacterFootDiagnosticStore.ManifestFileName), manifest);
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

        static JObject Compact(CharacterFootDiagnosisDocument document, CharacterFootDiagnosisContext context)
        {
            JObject result = JObject.FromObject(document,
                JsonSerializer.Create(CharacterFootDiagnosticStore.SerializerSettings()));
            foreach (JObject target in ((JArray)result["targets"]).OfType<JObject>())
            {
                var keys = new HashSet<string>(new[] { "detailId", "eventKind", "side", "startFrame",
                    "endFrame", "peakFrame", "eventIdentity", "sourceIdentity", "sourceCycle",
                    "matchedRules", "metrics", "evidence", "contactSupportGap" }, StringComparer.Ordinal);
                var representatives = new JArray();
                foreach (JObject source in ((JArray)target["representativeEvents"])
                             .OfType<JObject>().Take(CharacterFootDiagnosisContext.RepresentativeEventLimit))
                {
                    JObject preview = new JObject(source.Properties().Where(value => keys.Contains(value.Name))
                        .Select(value => new JProperty(value.Name, value.Value.DeepClone())));
                    representatives.Add(preview);
                }
                target["representativeEvents"] = representatives;
                target["representativeEventCount"] = representatives.Count;
                target["detailsIndex"] = new JObject
                {
                    ["file"] = CharacterFootDiagnosticStore.IndexFileName,
                    ["targetId"] = target.Value<string>("id")
                };
            }
            CompactObservations(result["stepTimeCandidateSelection"] as JObject,
                "stepTimeCandidateSelections", context);
            CompactObservations(result["landingReach"] as JObject, "landingReaches", context);
            return result;
        }

        static void CompactObservations(JObject report, string family, CharacterFootDiagnosisContext context)
        {
            if (report == null)
                return;
            report["detailsFamily"] = family;
            report["detailsIndex"] = CharacterFootDiagnosticStore.IndexFileName;
            var representatives = report["representativeEvents"] as JArray;
            if (representatives == null)
                return;
            while (representatives.Count > CharacterFootDiagnosisContext.RepresentativeEventLimit)
                representatives.RemoveAt(representatives.Count - 1);
            foreach (JObject representative in representatives.OfType<JObject>())
            {
                JObject observation = representative["landingReach"] as JObject ?? representative;
                representative["detailId"] = context.ObservationRecordId(family,
                    observation.Value<int>("frame"), observation.Value<string>("side"));
                if (observation != representative)
                {
                    representative["classification"] = observation["classification"]?.DeepClone();
                    representative["minimumCorrectionMeters"] = observation["minimumCorrectionMeters"]?.DeepClone();
                    representative.Remove("landingReach");
                }
            }
        }

        static void PublishFile(
            string path,
            object document)
        {
            CharacterFootDiagnosticStore.WriteJson(path, document, true);
        }
    }
}
