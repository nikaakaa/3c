using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFixedInputScheduledReplayProofResult
    {
        internal CharacterFixedInputScheduledReplayProofResult(
            string path,
            bool matched,
            string summary)
        {
            Path = path;
            Matched = matched;
            Summary = summary;
        }

        internal string Path { get; }
        internal bool Matched { get; }
        internal string Summary { get; }
    }

    internal static class CharacterFixedInputScheduledReplayProof
    {
        const string Schema = "character-fixed-input-replay-proof/4";

        internal static CharacterFixedInputScheduledReplayProofResult Publish(
            in CharacterFixedInputPresentationScheduleBinding binding,
            in CharacterFixedInputReplayRuntimeIdentity runtimeIdentity,
            CharacterFixedInputPresentationSchedule schedule,
            FixedCharacterInputReplayEvidence fixedEvidence,
            IReadOnlyList<GameplayPresentationScheduleFrame> scheduleFrames,
            in CharacterFixedInputPresentationScheduleFootCoverage footCoverage,
            string samplesPath,
            string factsPath)
        {
            if (schedule == null || fixedEvidence == null ||
                scheduleFrames == null ||
                scheduleFrames.Count != schedule.FrameCount ||
                fixedEvidence.Frames.Count != binding.TraceFrameCount)
            {
                throw new ArgumentException(
                    "Scheduled replay proof evidence is incomplete.");
            }
            ProofDocument document = Build(
                in binding,
                in runtimeIdentity,
                schedule,
                fixedEvidence,
                scheduleFrames,
                in footCoverage,
                samplesPath,
                factsPath);
            string directory = ResolveDirectory(
                binding.TraceId,
                schedule.ContentHash);
            Directory.CreateDirectory(directory);
            string baselinePath = FindLatest(directory);
            if (string.IsNullOrEmpty(baselinePath))
            {
                document.comparison = EmptyComparison();
            }
            else
            {
                document.comparison = Compare(
                    Read(baselinePath),
                    document,
                    baselinePath);
            }
            document.proof_hash = ComputeProofHash(document);
            string path = System.IO.Path.Combine(
                directory,
                $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{document.run_id}.json");
            Write(path, document);
            string summary = Describe(document.comparison, path);
            return new CharacterFixedInputScheduledReplayProofResult(
                path,
                document.comparison.matched,
                summary);
        }

        static ProofDocument Build(
            in CharacterFixedInputPresentationScheduleBinding binding,
            in CharacterFixedInputReplayRuntimeIdentity runtimeIdentity,
            CharacterFixedInputPresentationSchedule schedule,
            FixedCharacterInputReplayEvidence fixedEvidence,
            IReadOnlyList<GameplayPresentationScheduleFrame> scheduleFrames,
            in CharacterFixedInputPresentationScheduleFootCoverage footCoverage,
            string samplesPath,
            string factsPath)
        {
            var fixedFrames = new FixedFrameDocument[fixedEvidence.Frames.Count];
            for (int i = 0; i < fixedFrames.Length; i++)
            {
                FixedCharacterInputReplayFrameEvidence frame =
                    fixedEvidence.Frames[i];
                fixedFrames[i] = new FixedFrameDocument
                {
                    relative_frame = frame.RelativeFrame,
                    recorded_tick = frame.RecordedTick,
                    input_hash = frame.InputHash.ToString(),
                    body_hash = frame.BodyHash.ToString()
                };
            }
            var presentationFrames = new ScheduleFrameDocument[
                scheduleFrames.Count];
            for (int i = 0; i < presentationFrames.Length; i++)
            {
                GameplayPresentationScheduleFrame frame = scheduleFrames[i];
                presentationFrames[i] = new ScheduleFrameDocument
                {
                    frame_index = frame.FrameIndex,
                    render_frame = frame.RenderFrame,
                    relative_start_local_logic_tick =
                        frame.RelativeStartLocalLogicTick,
                    relative_end_local_logic_tick =
                        frame.RelativeEndLocalLogicTick,
                    logic_tick_count = frame.LogicTickCount,
                    scaled_delta_seconds = frame.ScaledDeltaSeconds,
                    unscaled_delta_seconds = frame.UnscaledDeltaSeconds,
                    presentation_delta_seconds = frame.PresentationDeltaSeconds,
                    interpolation_alpha = frame.InterpolationAlpha,
                    presentation_clock_mode =
                        frame.PresentationClockMode.ToString()
                };
            }
            return new ProofDocument
            {
                schema = Schema,
                run_id = Guid.NewGuid().ToString("N"),
                created_utc = DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                trace_id = binding.TraceId,
                trace_content_hash = binding.TraceContentHash,
                runtime_identity = new RuntimeIdentityDocument
                {
                    program_id = runtimeIdentity.ProgramId,
                    program_hash = runtimeIdentity.ProgramHash,
                    source_revision = runtimeIdentity.SourceRevision,
                    semantic_hash = runtimeIdentity.SemanticHash,
                    tick_rate = runtimeIdentity.TickRate,
                    projection_revision = runtimeIdentity.ProjectionRevision,
                    projection_source_revision = runtimeIdentity.ProjectionSourceRevision,
                    projection_semantic_hash = runtimeIdentity.ProjectionSemanticHash,
                    projection_contract_hash = runtimeIdentity.ProjectionContractHash,
                    world_revision = runtimeIdentity.WorldRevision,
                    launcher_variant_index = runtimeIdentity.LauncherVariantIndex
                },
                start_body_hash = fixedEvidence.StartBodyHash.ToString(),
                fixed_frame_count = fixedFrames.Length,
                input_sequence_hash =
                    fixedEvidence.InputSequenceHash.ToString(),
                body_trajectory_hash =
                    fixedEvidence.BodyTrajectoryHash.ToString(),
                presentation_schedule_id = schedule.ScheduleId,
                presentation_schedule_hash = schedule.ContentHash,
                presentation_schedule_path = schedule.Path,
                presentation_schedule_frame_count =
                    presentationFrames.Length,
                presentation_schedule_sequence_hash =
                    ComputeScheduleSequenceHash(presentationFrames),
                foot = new FootDocument
                {
                    samples_path = samplesPath,
                    facts_path = factsPath,
                    samples_sha256 = ComputeFileHash(samplesPath),
                    row_count = footCoverage.RowCount,
                    distinct_schedule_frame_count =
                        footCoverage.DistinctFrameCount,
                    first_schedule_frame_index =
                        footCoverage.FirstScheduleFrameIndex,
                    last_schedule_frame_index =
                        footCoverage.LastScheduleFrameIndex,
                    outside_schedule_row_count = 0
                },
                fixed_frames = fixedFrames,
                presentation_frames = presentationFrames
            };
        }

        static ComparisonDocument Compare(
            ProofDocument baseline,
            ProofDocument candidate,
            string baselinePath)
        {
            var aggregate = new List<MismatchDocument>();
            Add(aggregate, "trace_id", baseline.trace_id, candidate.trace_id);
            Add(
                aggregate,
                "trace_content_hash",
                baseline.trace_content_hash,
                candidate.trace_content_hash);
            AddRuntimeIdentity(
                aggregate,
                baseline.runtime_identity,
                candidate.runtime_identity);
            Add(
                aggregate,
                "start_body_hash",
                baseline.start_body_hash,
                candidate.start_body_hash);
            Add(
                aggregate,
                "fixed_frame_count",
                baseline.fixed_frame_count,
                candidate.fixed_frame_count);
            Add(
                aggregate,
                "input_sequence_hash",
                baseline.input_sequence_hash,
                candidate.input_sequence_hash);
            Add(
                aggregate,
                "body_trajectory_hash",
                baseline.body_trajectory_hash,
                candidate.body_trajectory_hash);
            Add(
                aggregate,
                "presentation_schedule_hash",
                baseline.presentation_schedule_hash,
                candidate.presentation_schedule_hash);
            Add(
                aggregate,
                "presentation_schedule_frame_count",
                baseline.presentation_schedule_frame_count,
                candidate.presentation_schedule_frame_count);
            Add(
                aggregate,
                "presentation_schedule_sequence_hash",
                baseline.presentation_schedule_sequence_hash,
                candidate.presentation_schedule_sequence_hash);
            Add(
                aggregate,
                "foot_row_count",
                baseline.foot.row_count,
                candidate.foot.row_count);
            Add(
                aggregate,
                "foot_distinct_schedule_frame_count",
                baseline.foot.distinct_schedule_frame_count,
                candidate.foot.distinct_schedule_frame_count);
            Add(
                aggregate,
                "foot_first_schedule_frame_index",
                baseline.foot.first_schedule_frame_index,
                candidate.foot.first_schedule_frame_index);
            Add(
                aggregate,
                "foot_last_schedule_frame_index",
                baseline.foot.last_schedule_frame_index,
                candidate.foot.last_schedule_frame_index);
            Add(
                aggregate,
                "foot_outside_schedule_row_count",
                baseline.foot.outside_schedule_row_count,
                candidate.foot.outside_schedule_row_count);

            FrameComparison fixedComparison = CompareFixedFrames(
                baseline.fixed_frames,
                candidate.fixed_frames);
            FrameComparison scheduleComparison = CompareScheduleFrames(
                baseline.presentation_frames,
                candidate.presentation_frames);
            return new ComparisonDocument
            {
                baseline_available = true,
                matched = aggregate.Count == 0 &&
                          fixedComparison.DivergentCount == 0 &&
                          scheduleComparison.DivergentCount == 0,
                baseline_path = baselinePath,
                aggregate_mismatches = aggregate.ToArray(),
                fixed_divergent_frame_count =
                    fixedComparison.DivergentCount,
                first_fixed_divergent_frame =
                    fixedComparison.FirstFrame,
                first_fixed_frame_mismatches =
                    fixedComparison.Mismatches,
                schedule_divergent_frame_count =
                    scheduleComparison.DivergentCount,
                first_schedule_divergent_frame =
                    scheduleComparison.FirstFrame,
                first_schedule_frame_mismatches =
                    scheduleComparison.Mismatches
            };
        }

        static FrameComparison CompareFixedFrames(
            FixedFrameDocument[] baseline,
            FixedFrameDocument[] candidate)
        {
            int count = Math.Min(baseline.Length, candidate.Length);
            int divergent = 0;
            int first = -1;
            var firstMismatches = new List<MismatchDocument>();
            for (int i = 0; i < count; i++)
            {
                var current = new List<MismatchDocument>();
                Add(current, "relative_frame", baseline[i].relative_frame, candidate[i].relative_frame);
                Add(current, "recorded_tick", baseline[i].recorded_tick, candidate[i].recorded_tick);
                Add(current, "input_hash", baseline[i].input_hash, candidate[i].input_hash);
                Add(current, "body_hash", baseline[i].body_hash, candidate[i].body_hash);
                if (current.Count == 0)
                    continue;
                divergent++;
                if (first >= 0)
                    continue;
                first = i;
                firstMismatches.AddRange(current);
            }
            divergent += Math.Abs(baseline.Length - candidate.Length);
            return new FrameComparison(
                divergent,
                first,
                firstMismatches.ToArray());
        }

        static FrameComparison CompareScheduleFrames(
            ScheduleFrameDocument[] baseline,
            ScheduleFrameDocument[] candidate)
        {
            int count = Math.Min(baseline.Length, candidate.Length);
            int divergent = 0;
            int first = -1;
            var firstMismatches = new List<MismatchDocument>();
            for (int i = 0; i < count; i++)
            {
                var current = new List<MismatchDocument>();
                Add(current, "frame_index", baseline[i].frame_index, candidate[i].frame_index);
                Add(current, "relative_start_local_logic_tick", baseline[i].relative_start_local_logic_tick, candidate[i].relative_start_local_logic_tick);
                Add(current, "relative_end_local_logic_tick", baseline[i].relative_end_local_logic_tick, candidate[i].relative_end_local_logic_tick);
                Add(current, "logic_tick_count", baseline[i].logic_tick_count, candidate[i].logic_tick_count);
                Add(current, "scaled_delta_seconds", baseline[i].scaled_delta_seconds, candidate[i].scaled_delta_seconds);
                Add(current, "unscaled_delta_seconds", baseline[i].unscaled_delta_seconds, candidate[i].unscaled_delta_seconds);
                Add(current, "presentation_delta_seconds", baseline[i].presentation_delta_seconds, candidate[i].presentation_delta_seconds);
                Add(current, "interpolation_alpha", baseline[i].interpolation_alpha, candidate[i].interpolation_alpha);
                Add(current, "presentation_clock_mode", baseline[i].presentation_clock_mode, candidate[i].presentation_clock_mode);
                if (current.Count == 0)
                    continue;
                divergent++;
                if (first >= 0)
                    continue;
                first = i;
                firstMismatches.AddRange(current);
            }
            divergent += Math.Abs(baseline.Length - candidate.Length);
            return new FrameComparison(
                divergent,
                first,
                firstMismatches.ToArray());
        }

        static ComparisonDocument EmptyComparison() =>
            new ComparisonDocument
            {
                baseline_available = false,
                matched = true,
                baseline_path = string.Empty,
                aggregate_mismatches = Array.Empty<MismatchDocument>(),
                first_fixed_divergent_frame = -1,
                first_fixed_frame_mismatches =
                    Array.Empty<MismatchDocument>(),
                first_schedule_divergent_frame = -1,
                first_schedule_frame_mismatches =
                    Array.Empty<MismatchDocument>()
            };

        static void Add(
            ICollection<MismatchDocument> target,
            string field,
            object baseline,
            object candidate)
        {
            string left = Format(baseline);
            string right = Format(candidate);
            if (string.Equals(left, right, StringComparison.Ordinal))
                return;
            target.Add(new MismatchDocument
            {
                field = field,
                baseline = left,
                candidate = right
            });
        }

        static void AddRuntimeIdentity(
            ICollection<MismatchDocument> target,
            RuntimeIdentityDocument baseline,
            RuntimeIdentityDocument candidate)
        {
            Add(target, "program_id", baseline.program_id, candidate.program_id);
            Add(target, "program_hash", baseline.program_hash, candidate.program_hash);
            Add(target, "source_revision", baseline.source_revision, candidate.source_revision);
            Add(target, "semantic_hash", baseline.semantic_hash, candidate.semantic_hash);
            Add(target, "runtime_tick_rate", baseline.tick_rate, candidate.tick_rate);
            Add(target, "projection_revision", baseline.projection_revision, candidate.projection_revision);
            Add(target, "projection_source_revision", baseline.projection_source_revision, candidate.projection_source_revision);
            Add(target, "projection_semantic_hash", baseline.projection_semantic_hash, candidate.projection_semantic_hash);
            Add(target, "projection_contract_hash", baseline.projection_contract_hash, candidate.projection_contract_hash);
            Add(target, "world_revision", baseline.world_revision, candidate.world_revision);
            Add(target, "launcher_variant_index", baseline.launcher_variant_index, candidate.launcher_variant_index);
        }

        static string Format(object value) => value switch
        {
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            IFormattable formattable =>
                formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value?.ToString() ?? string.Empty
        };

        static string ComputeScheduleSequenceHash(
            IReadOnlyList<ScheduleFrameDocument> frames)
        {
            using IncrementalHash hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (int i = 0; i < frames.Count; i++)
            {
                ScheduleFrameDocument frame = frames[i];
                Append(hash, frame.frame_index);
                Append(hash, frame.relative_start_local_logic_tick);
                Append(hash, frame.relative_end_local_logic_tick);
                Append(hash, frame.logic_tick_count);
                Append(hash, frame.scaled_delta_seconds);
                Append(hash, frame.unscaled_delta_seconds);
                Append(hash, frame.presentation_delta_seconds);
                Append(hash, frame.interpolation_alpha);
                Append(hash, frame.presentation_clock_mode);
            }
            return ToHex(hash.GetHashAndReset());
        }

        static string ComputeProofHash(ProofDocument document)
        {
            using IncrementalHash hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Append(hash, document.schema);
            Append(hash, document.run_id);
            Append(hash, document.created_utc);
            Append(hash, document.trace_id);
            Append(hash, document.trace_content_hash);
            AppendRuntimeIdentity(hash, document.runtime_identity);
            Append(hash, document.start_body_hash);
            Append(hash, document.input_sequence_hash);
            Append(hash, document.body_trajectory_hash);
            Append(hash, document.presentation_schedule_hash);
            Append(hash, document.presentation_schedule_sequence_hash);
            Append(hash, document.foot.row_count);
            Append(hash, document.foot.distinct_schedule_frame_count);
            Append(hash, document.foot.first_schedule_frame_index);
            Append(hash, document.foot.last_schedule_frame_index);
            for (int i = 0; i < document.fixed_frames.Length; i++)
            {
                Append(hash, document.fixed_frames[i].input_hash);
                Append(hash, document.fixed_frames[i].body_hash);
            }
            return ToHex(hash.GetHashAndReset());
        }

        static void AppendRuntimeIdentity(
            IncrementalHash hash,
            RuntimeIdentityDocument identity)
        {
            Append(hash, identity.program_id);
            Append(hash, identity.program_hash);
            Append(hash, identity.source_revision);
            Append(hash, identity.semantic_hash);
            Append(hash, identity.tick_rate);
            Append(hash, identity.projection_revision);
            Append(hash, identity.projection_source_revision);
            Append(hash, identity.projection_semantic_hash);
            Append(hash, identity.projection_contract_hash);
            Append(hash, identity.world_revision);
            Append(hash, identity.launcher_variant_index);
        }

        static void Append(IncrementalHash hash, object value)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Format(value)));
            hash.AppendData(new byte[] { 0x1f });
        }

        static string ComputeFileHash(string path)
        {
            using SHA256 hash = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return ToHex(hash.ComputeHash(stream));
        }

        static string ToHex(byte[] bytes) => string.Concat(
            bytes.Select(value =>
                value.ToString("x2", CultureInfo.InvariantCulture)));

        static string ResolveDirectory(string traceId, string scheduleHash) =>
            System.IO.Path.GetFullPath(System.IO.Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                "CharacterInputReplayProofs",
                "v4",
                traceId,
                scheduleHash));

        static string FindLatest(string directory) =>
            Directory.EnumerateFiles(directory, "*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? string.Empty;

        static ProofDocument Read(string path)
        {
            ProofDocument document = JsonConvert.DeserializeObject<ProofDocument>(
                File.ReadAllText(path, Encoding.UTF8));
            if (document == null || document.schema != Schema ||
                document.fixed_frames == null ||
                document.presentation_frames == null ||
                document.foot == null ||
                !IsRuntimeIdentityValid(document.runtime_identity))
            {
                throw new InvalidDataException(
                    "Scheduled replay baseline proof is invalid.");
            }
            return document;
        }

        static bool IsRuntimeIdentityValid(RuntimeIdentityDocument identity) =>
            identity != null &&
            !string.IsNullOrWhiteSpace(identity.program_id) &&
            !string.IsNullOrWhiteSpace(identity.program_hash) &&
            !string.IsNullOrWhiteSpace(identity.source_revision) &&
            !string.IsNullOrWhiteSpace(identity.semantic_hash) &&
            identity.tick_rate > 0 &&
            !string.IsNullOrWhiteSpace(identity.projection_revision) &&
            !string.IsNullOrWhiteSpace(identity.projection_source_revision) &&
            !string.IsNullOrWhiteSpace(identity.projection_semantic_hash) &&
            !string.IsNullOrWhiteSpace(identity.projection_contract_hash) &&
            !string.IsNullOrWhiteSpace(identity.world_revision) &&
            identity.launcher_variant_index >= 0;

        static void Write(string path, ProofDocument document)
        {
            string part = path + ".part";
            File.WriteAllText(
                part,
                JsonConvert.SerializeObject(document, Formatting.Indented),
                new UTF8Encoding(false));
            File.Move(part, path);
        }

        static string Describe(ComparisonDocument comparison, string path)
        {
            string aggregate = string.Join(
                ",",
                comparison.aggregate_mismatches.Select(value => value.field));
            string fixedFields = string.Join(
                ",",
                comparison.first_fixed_frame_mismatches.Select(value => value.field));
            string scheduleFields = string.Join(
                ",",
                comparison.first_schedule_frame_mismatches.Select(value => value.field));
            return
                $"Scheduled replay proof {(comparison.matched ? "matched" : "mismatch")}. " +
                $"Candidate={path}, AggregateFields=[{aggregate}], " +
                $"FixedDivergent={comparison.fixed_divergent_frame_count}, " +
                $"FirstFixedFrame={comparison.first_fixed_divergent_frame}, " +
                $"FixedFields=[{fixedFields}], " +
                $"ScheduleDivergent={comparison.schedule_divergent_frame_count}, " +
                $"FirstScheduleFrame={comparison.first_schedule_divergent_frame}, " +
                $"ScheduleFields=[{scheduleFields}].";
        }

        readonly struct FrameComparison
        {
            internal FrameComparison(
                int divergentCount,
                int firstFrame,
                MismatchDocument[] mismatches)
            {
                DivergentCount = divergentCount;
                FirstFrame = firstFrame;
                Mismatches = mismatches;
            }

            internal int DivergentCount { get; }
            internal int FirstFrame { get; }
            internal MismatchDocument[] Mismatches { get; }
        }

        [Serializable]
        sealed class ProofDocument
        {
            public string schema;
            public string run_id;
            public string created_utc;
            public string trace_id;
            public string trace_content_hash;
            public RuntimeIdentityDocument runtime_identity;
            public string start_body_hash;
            public int fixed_frame_count;
            public string input_sequence_hash;
            public string body_trajectory_hash;
            public string presentation_schedule_id;
            public string presentation_schedule_hash;
            public string presentation_schedule_path;
            public int presentation_schedule_frame_count;
            public string presentation_schedule_sequence_hash;
            public FootDocument foot;
            public FixedFrameDocument[] fixed_frames;
            public ScheduleFrameDocument[] presentation_frames;
            public ComparisonDocument comparison;
            public string proof_hash;
        }

        [Serializable]
        sealed class RuntimeIdentityDocument
        {
            public string program_id;
            public string program_hash;
            public string source_revision;
            public string semantic_hash;
            public int tick_rate;
            public string projection_revision;
            public string projection_source_revision;
            public string projection_semantic_hash;
            public string projection_contract_hash;
            public string world_revision;
            public int launcher_variant_index;
        }

        [Serializable]
        sealed class FootDocument
        {
            public string samples_path;
            public string facts_path;
            public string samples_sha256;
            public int row_count;
            public int distinct_schedule_frame_count;
            public int first_schedule_frame_index;
            public int last_schedule_frame_index;
            public int outside_schedule_row_count;
        }

        [Serializable]
        sealed class FixedFrameDocument
        {
            public int relative_frame;
            public ulong recorded_tick;
            public string input_hash;
            public string body_hash;
        }

        [Serializable]
        sealed class ScheduleFrameDocument
        {
            public int frame_index;
            public ulong render_frame;
            public ulong relative_start_local_logic_tick;
            public ulong relative_end_local_logic_tick;
            public int logic_tick_count;
            public float scaled_delta_seconds;
            public float unscaled_delta_seconds;
            public float presentation_delta_seconds;
            public float interpolation_alpha;
            public string presentation_clock_mode;
        }

        [Serializable]
        sealed class ComparisonDocument
        {
            public bool baseline_available;
            public bool matched;
            public string baseline_path;
            public MismatchDocument[] aggregate_mismatches;
            public int fixed_divergent_frame_count;
            public int first_fixed_divergent_frame;
            public MismatchDocument[] first_fixed_frame_mismatches;
            public int schedule_divergent_frame_count;
            public int first_schedule_divergent_frame;
            public MismatchDocument[] first_schedule_frame_mismatches;
        }

        [Serializable]
        sealed class MismatchDocument
        {
            public string field;
            public string baseline;
            public string candidate;
        }
    }
}
