using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public static class CharacterMotionMatchingDatabaseArtifactCodec
    {
        const int Magic = 0x42444d4d;
        const int FormatVersion = 16;

        enum SectionId
        {
            Identity = 1,
            Segments = 2,
            Samples = 3,
            Features = 4,
            Normalization = 5,
            SearchIndex = 6,
            Runtime = 7,
            Coverage = 8
        }

        public static StableHash ComputeContentHash(CharacterMotionMatchingDatabaseArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            byte[] identityCore = WriteIdentityCore(artifact.Identity);
            byte[][] dataSections = WriteDataSections(artifact);
            return SimulationCanonicalPayloadHash.Compute(Join(identityCore, dataSections));
        }

        public static byte[] Write(CharacterMotionMatchingDatabaseArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            StableHash computed = ComputeContentHash(artifact);
            if (!computed.Equals(artifact.Identity.ContentHash))
                throw new InvalidDataException("Motion Matching Artifact ContentHash does not match its canonical sections.");
            byte[] identityCore = WriteIdentityCore(artifact.Identity);
            byte[] identity = WriteBuffer(writer =>
            {
                writer.Write(identityCore.Length);
                writer.Write(identityCore);
                writer.Write(artifact.Identity.ContentHash.Value);
            });
            byte[][] data = WriteDataSections(artifact);
            var sections = new List<KeyValuePair<SectionId, byte[]>>(8)
            {
                Pair(SectionId.Identity, identity),
                Pair(SectionId.Segments, data[0]),
                Pair(SectionId.Samples, data[1]),
                Pair(SectionId.Features, data[2]),
                Pair(SectionId.Normalization, data[3]),
                Pair(SectionId.SearchIndex, data[4]),
                Pair(SectionId.Runtime, data[5]),
                Pair(SectionId.Coverage, data[6])
            };
            int headerSize = 12 + sections.Count * 12;
            int offset = headerSize;
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(sections.Count);
            for (int i = 0; i < sections.Count; i++)
            {
                writer.Write((int)sections[i].Key);
                writer.Write(offset);
                writer.Write(sections[i].Value.Length);
                offset += sections[i].Value.Length;
            }
            for (int i = 0; i < sections.Count; i++)
                writer.Write(sections[i].Value);
            writer.Flush();
            return stream.ToArray();
        }

        public static CharacterMotionMatchingDatabaseArtifact Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 12)
                throw new InvalidDataException("Motion Matching Artifact is empty or truncated.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (reader.ReadInt32() != Magic)
                throw new InvalidDataException("Motion Matching Artifact magic is invalid.");
            if (reader.ReadInt32() != FormatVersion)
                throw new InvalidDataException("Motion Matching Artifact schema version is unknown.");
            int sectionCount = reader.ReadInt32();
            if (sectionCount != 8)
                throw new InvalidDataException("Motion Matching Artifact section table is incomplete.");
            int expectedOffset = 12 + sectionCount * 12;
            var sections = new Dictionary<SectionId, ArraySegment<byte>>();
            for (int i = 0; i < sectionCount; i++)
            {
                SectionId id = (SectionId)reader.ReadInt32();
                int offset = reader.ReadInt32();
                int length = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(SectionId), id) || offset != expectedOffset || length < 0 || offset + length > bytes.Length || !sections.TryAdd(id, new ArraySegment<byte>(bytes, offset, length)))
                    throw new InvalidDataException("Motion Matching Artifact section table is non-canonical.");
                expectedOffset += length;
            }
            if (expectedOffset != bytes.Length)
                throw new InvalidDataException("Motion Matching Artifact contains trailing bytes.");
            IdentityRead identityRead = ReadIdentity(sections[SectionId.Identity]);
            MotionMatchingSegmentPayload[] segments = ReadSegments(sections[SectionId.Segments]);
            MotionMatchingSamplePayload[] samples = ReadSamples(sections[SectionId.Samples]);
            float[] features = ReadFloatArray(sections[SectionId.Features], "feature");
            ReadNormalization(sections[SectionId.Normalization], out float[] median, out float[] scale, out bool[] active);
            ReadIndex(sections[SectionId.SearchIndex], out MotionMatchingSearchIndexNodePayload[] nodes, out int[] orderedSamples);
            RuntimeRead runtime = ReadRuntime(sections[SectionId.Runtime]);
            ReadCoverage(
                sections[SectionId.Coverage],
                out MotionMatchingDatabaseCoverageDiagnosticsPayload coverageDiagnostics,
                out MotionMatchingCoverageSummaryPayload[] coverage);
            var identity = new CharacterMotionMatchingDatabaseArtifactIdentity(
                identityRead.ArtifactSchemaVersion,
                identityRead.AlgorithmVersion,
                identityRead.DatabaseId,
                identityRead.DatabaseRevision,
                identityRead.FeatureSchemaId,
                identityRead.FeatureSchemaRevision,
                identityRead.RigId,
                identityRead.RigRevision,
                identityRead.Dependencies,
                identityRead.AnalysisInputHash,
                identityRead.OrderedDependencyHash,
                identityRead.ContentHash);
            var artifact = new CharacterMotionMatchingDatabaseArtifact(
                identity,
                runtime.SearchDomainId,
                runtime.SampleRate,
                runtime.Capacities,
                segments,
                samples,
                features,
                median,
                scale,
                active,
                nodes,
                orderedSamples,
                coverageDiagnostics,
                coverage);
            if (!ComputeContentHash(artifact).Equals(identity.ContentHash))
                throw new InvalidDataException("Motion Matching Artifact canonical ContentHash is invalid.");
            byte[] canonical = Write(artifact);
            if (!canonical.SequenceEqual(bytes))
                throw new InvalidDataException("Motion Matching Artifact encoding is not canonical.");
            return artifact;
        }

        static byte[] WriteIdentityCore(CharacterMotionMatchingDatabaseArtifactIdentity identity)
        {
            return WriteBuffer(writer =>
            {
                writer.Write(identity.ArtifactSchemaVersion);
                writer.Write(identity.AnalysisAlgorithmVersion);
                writer.Write(identity.DatabaseId.Value);
                writer.Write(identity.DatabaseRevision);
                writer.Write(identity.FeatureSchemaId.Value);
                writer.Write(identity.FeatureSchemaRevision);
                writer.Write(identity.RigId);
                writer.Write(identity.RigRevision);
                writer.Write(identity.ClipDependencyCount);
                for (int i = 0; i < identity.ClipDependencyCount; i++)
                {
                    MotionMatchingClipDependencyIdentity dependency = identity.GetClipDependency(i);
                    writer.Write(dependency.SourceSetId.Value);
                    writer.Write(dependency.SourceSetRevision);
                    writer.Write(dependency.SourceClipId.Value);
                    writer.Write(dependency.AssetGuid);
                    writer.Write(dependency.LocalFileId);
                    writer.Write(dependency.ImportDependencyHash);
                    writer.Write(dependency.SamplingRigSignature);
                    writer.Write(dependency.MotionRootBoneId.Value);
                    writer.Write(dependency.FootArtifactHash.Value);
                }
                writer.Write(identity.AnalysisInputHash.Value);
                writer.Write(identity.OrderedClipDependencyHash.Value);
            });
        }

        static byte[][] WriteDataSections(CharacterMotionMatchingDatabaseArtifact artifact)
        {
            return new[]
            {
                WriteSegments(artifact),
                WriteSamples(artifact),
                WriteBuffer(writer => WriteFloatArray(writer, artifact.NormalizedFeatureCount, artifact.GetNormalizedFeature)),
                WriteNormalization(artifact),
                WriteIndex(artifact),
                WriteRuntime(artifact),
                WriteCoverage(artifact)
            };
        }

        static byte[] WriteSegments(CharacterMotionMatchingDatabaseArtifact artifact) => WriteBuffer(writer =>
        {
            writer.Write(artifact.SegmentCount);
            for (int i = 0; i < artifact.SegmentCount; i++)
            {
                MotionMatchingSegmentPayload segment = artifact.GetSegment(i);
                writer.Write(segment.SegmentId.Value);
                writer.Write(segment.SourceClipId.Value);
                writer.Write(segment.FirstSampleIndex);
                writer.Write(segment.SampleCount);
                writer.Write(segment.StartTime);
                writer.Write(segment.EndTime);
                writer.Write((byte)segment.LoopMode);
                writer.Write(segment.Terminal);
                writer.Write(segment.ContinuationEntrySampleIndex);
            }
        });

        static MotionMatchingSegmentPayload[] ReadSegments(ArraySegment<byte> section) => ReadSection(section, reader =>
        {
            int count = ReadPositiveCount(reader, "Segment");
            var values = new MotionMatchingSegmentPayload[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = new MotionMatchingSegmentPayload(
                    new CharacterMotionMatchingSegmentId(reader.ReadString()),
                    new CharacterMotionMatchingSourceClipId(reader.ReadString()),
                    reader.ReadInt32(), reader.ReadInt32(),
                    reader.ReadSingle(), reader.ReadSingle(),
                    (MotionMatchingSegmentLoopMode)reader.ReadByte(),
                    reader.ReadBoolean(), reader.ReadInt32());
            }
            return values;
        });

        static byte[] WriteSamples(CharacterMotionMatchingDatabaseArtifact artifact) => WriteBuffer(writer =>
        {
            writer.Write(artifact.SampleCount);
            for (int i = 0; i < artifact.SampleCount; i++)
            {
                MotionMatchingSamplePayload sample = artifact.GetSample(i);
                writer.Write(sample.SampleId.Value);
                writer.Write(sample.SegmentId.Value);
                writer.Write(sample.SearchDomainId.Value);
                writer.Write(sample.ClipBindingIndex);
                writer.Write(sample.SampleTime);
                writer.Write(sample.CanInitialize);
                writer.Write(sample.CanJumpInto);
                writer.Write(sample.EntryExcluded);
                writer.Write(sample.ExitExcluded);
                writer.Write(sample.Terminal);
                writer.Write(sample.NextSampleIndex);
                writer.Write((byte)sample.ContactMask);
                WriteVector2(writer, sample.RootPlanarVelocity);
                writer.Write(sample.RootYawVelocityDegrees);
                WriteVector3(writer, sample.LeftFootRootPosition);
                WriteVector3(writer, sample.RightFootRootPosition);
                WriteFoot(writer, sample.LeftFoot);
                WriteFoot(writer, sample.RightFoot);
            }
        });

        static MotionMatchingSamplePayload[] ReadSamples(ArraySegment<byte> section) => ReadSection(section, reader =>
        {
            int count = ReadPositiveCount(reader, "Sample");
            var values = new MotionMatchingSamplePayload[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = new MotionMatchingSamplePayload(
                    new CharacterMotionMatchingSampleId(reader.ReadUInt32()),
                    new CharacterMotionMatchingSegmentId(reader.ReadString()),
                    new CharacterMotionMatchingSearchDomainId(reader.ReadString()),
                    reader.ReadInt32(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadBoolean(),
                    reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadInt32(),
                    (MotionMatchingFootContactMask)reader.ReadByte(), ReadVector2(reader), reader.ReadSingle(),
                    ReadVector3(reader), ReadVector3(reader), ReadFoot(reader), ReadFoot(reader));
            }
            return values;
        });

        static byte[] WriteNormalization(CharacterMotionMatchingDatabaseArtifact artifact) => WriteBuffer(writer =>
        {
            writer.Write(artifact.Capacities.DenseFeatureCount);
            for (int i = 0; i < artifact.Capacities.DenseFeatureCount; i++)
            {
                writer.Write(artifact.GetNormalizationMedian(i));
                writer.Write(artifact.GetNormalizationScale(i));
                writer.Write(artifact.IsFeatureActive(i));
            }
        });

        static void ReadNormalization(ArraySegment<byte> section, out float[] median, out float[] scale, out bool[] active)
        {
            (median, scale, active) = ReadSection(section, reader =>
            {
                int count = ReadPositiveCount(reader, "Normalization");
                var medians = new float[count];
                var scales = new float[count];
                var flags = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    medians[i] = reader.ReadSingle();
                    scales[i] = reader.ReadSingle();
                    flags[i] = reader.ReadBoolean();
                }
                return (medians, scales, flags);
            });
        }

        static byte[] WriteIndex(CharacterMotionMatchingDatabaseArtifact artifact) => WriteBuffer(writer =>
        {
            writer.Write(artifact.SearchNodeCount);
            for (int i = 0; i < artifact.SearchNodeCount; i++)
            {
                MotionMatchingSearchIndexNodePayload node = artifact.GetSearchNode(i);
                writer.Write(node.NodeId.Value);
                writer.Write(node.LeftChildIndex);
                writer.Write(node.RightChildIndex);
                writer.Write(node.OrderedSampleOffset);
                writer.Write(node.OrderedSampleCount);
                writer.Write(node.SearchDomainId.Value);
                writer.Write((byte)node.ContactMaskUnion);
                writer.Write(node.FeatureCount);
                for (int feature = 0; feature < node.FeatureCount; feature++)
                {
                    writer.Write(node.GetMinimum(feature));
                    writer.Write(node.GetMaximum(feature));
                }
            }
            writer.Write(artifact.OrderedSampleIndexCount);
            for (int i = 0; i < artifact.OrderedSampleIndexCount; i++)
                writer.Write(artifact.GetOrderedSampleIndex(i));
        });

        static void ReadIndex(ArraySegment<byte> section, out MotionMatchingSearchIndexNodePayload[] nodes, out int[] orderedSamples)
        {
            (nodes, orderedSamples) = ReadSection(section, reader =>
            {
                int nodeCount = ReadPositiveCount(reader, "Search node");
                var readNodes = new MotionMatchingSearchIndexNodePayload[nodeCount];
                for (int i = 0; i < nodeCount; i++)
                {
                    var nodeId = new CharacterMotionMatchingIndexNodeId(reader.ReadInt32());
                    int left = reader.ReadInt32();
                    int right = reader.ReadInt32();
                    int offset = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    var domain = new CharacterMotionMatchingSearchDomainId(reader.ReadString());
                    var contact = (MotionMatchingFootContactMask)reader.ReadByte();
                    int featureCount = ReadPositiveCount(reader, "Node feature");
                    var min = new float[featureCount];
                    var max = new float[featureCount];
                    for (int feature = 0; feature < featureCount; feature++)
                    {
                        min[feature] = reader.ReadSingle();
                        max[feature] = reader.ReadSingle();
                    }
                    readNodes[i] = new MotionMatchingSearchIndexNodePayload(nodeId, left, right, offset, count, domain, contact, min, max);
                }
                int orderedCount = ReadPositiveCount(reader, "Ordered sample");
                var ordered = new int[orderedCount];
                for (int i = 0; i < orderedCount; i++)
                    ordered[i] = reader.ReadInt32();
                return (readNodes, ordered);
            });
        }

        static byte[] WriteRuntime(CharacterMotionMatchingDatabaseArtifact artifact) => WriteBuffer(writer =>
        {
            writer.Write(artifact.SearchDomainId.Value);
            writer.Write(artifact.SampleRate);
            MotionMatchingRuntimeCapacityPayload capacity = artifact.Capacities;
            writer.Write(capacity.DenseFeatureCount);
            writer.Write(capacity.SampleCount);
            writer.Write(capacity.TreeNodeCount);
            writer.Write(capacity.TraversalCapacity);
            writer.Write(capacity.TopK);
            writer.Write(capacity.PlanSampleCount);
            writer.Write(capacity.HistoryCapacity);
            writer.Write(capacity.DiagnosticDetailCapacity);
        });

        static RuntimeRead ReadRuntime(ArraySegment<byte> section) => ReadSection(section, reader =>
        {
            var domain = new CharacterMotionMatchingSearchDomainId(reader.ReadString());
            float sampleRate = reader.ReadSingle();
            var capacity = new MotionMatchingRuntimeCapacityPayload(
                reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
                reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            return new RuntimeRead(domain, sampleRate, capacity);
        });

        static byte[] WriteCoverage(CharacterMotionMatchingDatabaseArtifact artifact) => WriteBuffer(writer =>
        {
            MotionMatchingDatabaseCoverageDiagnosticsPayload diagnostics = artifact.CoverageDiagnostics;
            writer.Write(diagnostics.TotalSampleCount);
            writer.Write(diagnostics.ReachableSampleCount);
            writer.Write(diagnostics.UnreachableSampleCount);
            writer.Write(diagnostics.TotalSegmentCount);
            writer.Write(diagnostics.ReachableSegmentCount);
            writer.Write(diagnostics.UnreachableSegmentCount);
            writer.Write(diagnostics.ExactDuplicateSampleCount);
            writer.Write(diagnostics.ExactDuplicateSampleRatio);
            writer.Write(diagnostics.NearDuplicatePairCount);
            writer.Write(diagnostics.TotalUnorderedNonExactPairCount);
            writer.Write(diagnostics.NearDuplicatePairRatio);
            writer.Write(diagnostics.ProtectedContactEmptyRegionCount);
            writer.Write(diagnostics.EvaluatedNonEmptyRawProtectedContactRegionCount);
            writer.Write(diagnostics.ProtectedContactEmptyRegionRatio);
            writer.Write(diagnostics.MaximumAdmittedCandidateSetUpperBound);
            writer.Write(diagnostics.SearchIndexMaximumDepth);
            writer.Write(artifact.CoverageCount);
            for (int i = 0; i < artifact.CoverageCount; i++)
            {
                MotionMatchingCoverageSummaryPayload coverage = artifact.GetCoverage(i);
                writer.Write(coverage.RequirementId);
                writer.Write(coverage.Satisfied);
                writer.Write(coverage.SampleCount);
                writer.Write(coverage.MinimumObservedSpeed);
                writer.Write(coverage.MaximumObservedSpeed);
                writer.Write(coverage.MaximumObservedFacingChange);
                writer.Write(coverage.MinimumObservedPlanHorizon);
            }
        });

        static void ReadCoverage(
            ArraySegment<byte> section,
            out MotionMatchingDatabaseCoverageDiagnosticsPayload diagnostics,
            out MotionMatchingCoverageSummaryPayload[] coverage)
        {
            CoverageRead value = ReadSection(section, reader =>
            {
                var databaseDiagnostics = new MotionMatchingDatabaseCoverageDiagnosticsPayload(
                    reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
                    reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
                    reader.ReadInt32(), reader.ReadSingle(), reader.ReadInt64(), reader.ReadInt64(), reader.ReadSingle(),
                    reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadInt32(), reader.ReadInt32());
                int count = ReadPositiveCount(reader, "Coverage");
                var summaries = new MotionMatchingCoverageSummaryPayload[count];
                for (int i = 0; i < count; i++)
                    summaries[i] = new MotionMatchingCoverageSummaryPayload(reader.ReadString(), reader.ReadBoolean(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                return new CoverageRead(databaseDiagnostics, summaries);
            });
            diagnostics = value.Diagnostics;
            coverage = value.Summaries;
        }

        static IdentityRead ReadIdentity(ArraySegment<byte> section) => ReadSection(section, reader =>
        {
            int coreLength = reader.ReadInt32();
            if (coreLength <= 0 || coreLength > section.Count - 4)
                throw new InvalidDataException("Motion Matching Artifact identity core length is invalid.");
            byte[] core = reader.ReadBytes(coreLength);
            if (core.Length != coreLength)
                throw new EndOfStreamException();
            string contentHash = reader.ReadString();
            using var coreStream = new MemoryStream(core, false);
            using var coreReader = new BinaryReader(coreStream, Encoding.UTF8, true);
            int schema = coreReader.ReadInt32();
            string algorithm = coreReader.ReadString();
            var databaseId = new CharacterMotionMatchingDatabaseId(coreReader.ReadString());
            int databaseRevision = coreReader.ReadInt32();
            var featureSchemaId = new CharacterMotionMatchingFeatureSchemaId(coreReader.ReadString());
            int featureRevision = coreReader.ReadInt32();
            string rigId = coreReader.ReadString();
            string rigRevision = coreReader.ReadString();
            int dependencyCount = ReadPositiveCount(coreReader, "Clip dependency");
            var dependencies = new MotionMatchingClipDependencyIdentity[dependencyCount];
            for (int i = 0; i < dependencyCount; i++)
            {
                dependencies[i] = new MotionMatchingClipDependencyIdentity(
                    new CharacterMotionMatchingSourceSetId(coreReader.ReadString()), coreReader.ReadInt32(),
                    new CharacterMotionMatchingSourceClipId(coreReader.ReadString()), coreReader.ReadString(), coreReader.ReadInt64(),
                    coreReader.ReadString(), coreReader.ReadString(), new AnimationBoneId(coreReader.ReadString()), new StableHash(coreReader.ReadString()));
            }
            var analysisInputHash = new StableHash(coreReader.ReadString());
            var orderedHash = new StableHash(coreReader.ReadString());
            if (coreStream.Position != coreStream.Length)
                throw new InvalidDataException("Motion Matching Artifact identity core contains trailing bytes.");
            return new IdentityRead(schema, algorithm, databaseId, databaseRevision, featureSchemaId, featureRevision, rigId, rigRevision, dependencies, analysisInputHash, orderedHash, new StableHash(contentHash));
        });

        static float[] ReadFloatArray(ArraySegment<byte> section, string label) => ReadSection(section, reader =>
        {
            int count = ReadPositiveCount(reader, label);
            var values = new float[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadSingle();
            return values;
        });

        static void WriteFloatArray(BinaryWriter writer, int count, Func<int, float> get)
        {
            writer.Write(count);
            for (int i = 0; i < count; i++)
                writer.Write(get(i));
        }

        static void WriteFoot(BinaryWriter writer, AnimationFootFeatureSample value)
        {
            WriteVector3(writer, value.SoleLocalVelocity);
            writer.Write(value.SoleHeight);
            writer.Write(value.PlantConfidence);
            AnimationPredictedFootStepSample predicted = value.PredictedStep;
            writer.Write(predicted.IsValid);
            if (!predicted.IsValid)
                return;
            writer.Write(predicted.EventOrdinal);
            writer.Write(predicted.SourceLandingCycleOffset);
            writer.Write(predicted.Confidence);
            writer.Write(predicted.TimeToLandingSeconds);
            writer.Write(predicted.EventPhase);
            writer.Write(predicted.ReleasePhase);
            writer.Write(predicted.LiftOffPhase);
            writer.Write(predicted.ApproachContactPhase);
            writer.Write(predicted.ActionStepClock.DurationSeconds);
            writer.Write(predicted.OpposingEventOrdinal);
            writer.Write(predicted.OpposingLandingDelaySeconds);
            writer.Write(predicted.OpposingLandingCycleOffset);
            WriteVector3(writer, predicted.OpposingRootLocalLanding);
            for (int i = 0; i < predicted.RootLocalFootRoute.Length; i++)
                WriteVector3(writer, predicted.RootLocalFootRoute[i]);
            for (int i = 0; i < predicted.RootLocalAnkleRoute.Length; i++)
                WriteVector3(writer, predicted.RootLocalAnkleRoute[i]);
            for (int i = 0; i < predicted.RootLocalHipRoute.Length; i++)
                WriteVector3(writer, predicted.RootLocalHipRoute[i]);
            for (int i = 0; i < predicted.AuthoredFootPlanarRoute.Length; i++)
                WriteVector3(writer, predicted.AuthoredFootPlanarRoute[i]);
            for (int i = 0; i < predicted.AnimationClearanceHeights.Length; i++)
                writer.Write(predicted.AnimationClearanceHeights[i]);
            writer.Write(predicted.LandingPhase);
            WriteQuaternion(writer, predicted.OpposingRootLocalSoleRotation);
            WriteBiomechanicalSample(writer, predicted.BiomechanicalSample);
        }

        static AnimationFootFeatureSample ReadFoot(BinaryReader reader)
        {
            Vector3 velocity = ReadVector3(reader);
            float soleHeight = reader.ReadSingle();
            float plantConfidence = reader.ReadSingle();
            AnimationPredictedFootStepSample predicted = reader.ReadBoolean()
                ? new AnimationPredictedFootStepSample(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadInt32(),
                reader.ReadSingle(),
                reader.ReadInt32(),
                ReadVector3(reader),
                ReadVector3Route(reader),
                ReadVector3Route(reader),
                ReadVector3Route(reader),
                ReadVector3Route(reader),
                ReadFloatRoute(reader),
                reader.ReadSingle(),
                ReadQuaternion(reader),
                ReadBiomechanicalSample(reader))
                : default;
            return new AnimationFootFeatureSample(
                velocity,
                soleHeight,
                plantConfidence,
                predicted,
                default);
        }
        static FixedList512Bytes<Vector3> ReadVector3Route(BinaryReader reader)
        {
            var result = new FixedList512Bytes<Vector3>();
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                result.Add(ReadVector3(reader));
            return result;
        }

        static void WriteBiomechanicalSample(
            BinaryWriter writer,
            AnimationFootBiomechanicalRouteSample value)
        {
            if (!value.IsValid)
                throw new InvalidDataException("Motion Matching biomechanical Foot sample is invalid.");
            WriteVector3(writer, value.RootLocalHeelPosition);
            WriteVector3(writer, value.RootLocalToePosition);
            WriteVector3(writer, value.RootLocalKneePosition);
            WriteQuaternion(writer, value.RootLocalSoleRotation);
            WriteQuaternion(writer, value.RootLocalAnkleRotation);
            writer.Write(value.ConstraintWeight);
            writer.Write(value.SupportWeight);
            writer.Write(value.SupportLegLength);
            writer.Write(value.SupportLegCompressionReserve);
            WriteVector3(writer, value.SupportKneeBendPlane);
            WriteVector3(writer, value.SupportFootPivotPosition);
            writer.Write(value.SupportFootPivotWeight);
        }

        static AnimationFootBiomechanicalRouteSample ReadBiomechanicalSample(BinaryReader reader) =>
            new AnimationFootBiomechanicalRouteSample(
                ReadVector3(reader),
                ReadVector3(reader),
                ReadVector3(reader),
                ReadQuaternion(reader),
                ReadQuaternion(reader),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                ReadVector3(reader),
                ReadVector3(reader),
                reader.ReadSingle());
        static FixedList128Bytes<float> ReadFloatRoute(BinaryReader reader)
        {
            var result = new FixedList128Bytes<float>();
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                result.Add(reader.ReadSingle());
            return result;
        }
        static void WriteVector2(BinaryWriter writer, Vector2 value) { writer.Write(value.x); writer.Write(value.y); }
        static void WriteVector3(BinaryWriter writer, Vector3 value) { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
        static Vector2 ReadVector2(BinaryReader reader) => new Vector2(reader.ReadSingle(), reader.ReadSingle());
        static Vector3 ReadVector3(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        static void WriteQuaternion(BinaryWriter writer, Quaternion value) { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); writer.Write(value.w); }
        static Quaternion ReadQuaternion(BinaryReader reader) => new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        static T ReadSection<T>(ArraySegment<byte> section, Func<BinaryReader, T> read)
        {
            using var stream = new MemoryStream(section.Array, section.Offset, section.Count, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            T result = read(reader);
            if (stream.Position != stream.Length)
                throw new InvalidDataException("Motion Matching Artifact section contains trailing bytes.");
            return result;
        }

        static byte[] WriteBuffer(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            write(writer);
            writer.Flush();
            return stream.ToArray();
        }

        static int ReadPositiveCount(BinaryReader reader, string label)
        {
            int value = reader.ReadInt32();
            if (value <= 0)
                throw new InvalidDataException($"Motion Matching Artifact {label} count is invalid.");
            return value;
        }

        static byte[] Join(byte[] identityCore, byte[][] data)
        {
            int length = identityCore.Length;
            for (int i = 0; i < data.Length; i++)
                length += data[i].Length;
            var result = new byte[length];
            int offset = 0;
            Buffer.BlockCopy(identityCore, 0, result, offset, identityCore.Length);
            offset += identityCore.Length;
            for (int i = 0; i < data.Length; i++)
            {
                Buffer.BlockCopy(data[i], 0, result, offset, data[i].Length);
                offset += data[i].Length;
            }
            return result;
        }

        static KeyValuePair<SectionId, byte[]> Pair(SectionId id, byte[] bytes) => new KeyValuePair<SectionId, byte[]>(id, bytes);

        readonly struct RuntimeRead
        {
            public RuntimeRead(CharacterMotionMatchingSearchDomainId searchDomainId, float sampleRate, MotionMatchingRuntimeCapacityPayload capacities)
            {
                SearchDomainId = searchDomainId;
                SampleRate = sampleRate;
                Capacities = capacities;
            }
            public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
            public float SampleRate { get; }
            public MotionMatchingRuntimeCapacityPayload Capacities { get; }
        }

        readonly struct CoverageRead
        {
            public CoverageRead(
                MotionMatchingDatabaseCoverageDiagnosticsPayload diagnostics,
                MotionMatchingCoverageSummaryPayload[] summaries)
            {
                Diagnostics = diagnostics;
                Summaries = summaries;
            }

            public MotionMatchingDatabaseCoverageDiagnosticsPayload Diagnostics { get; }
            public MotionMatchingCoverageSummaryPayload[] Summaries { get; }
        }

        readonly struct IdentityRead
        {
            public IdentityRead(int schema, string algorithm, CharacterMotionMatchingDatabaseId databaseId, int databaseRevision,
                CharacterMotionMatchingFeatureSchemaId featureSchemaId, int featureSchemaRevision, string rigId, string rigRevision,
                MotionMatchingClipDependencyIdentity[] dependencies, StableHash analysisInputHash,
                StableHash orderedDependencyHash, StableHash contentHash)
            {
                ArtifactSchemaVersion = schema;
                AlgorithmVersion = algorithm;
                DatabaseId = databaseId;
                DatabaseRevision = databaseRevision;
                FeatureSchemaId = featureSchemaId;
                FeatureSchemaRevision = featureSchemaRevision;
                RigId = rigId;
                RigRevision = rigRevision;
                Dependencies = dependencies;
                AnalysisInputHash = analysisInputHash;
                OrderedDependencyHash = orderedDependencyHash;
                ContentHash = contentHash;
            }
            public int ArtifactSchemaVersion { get; }
            public string AlgorithmVersion { get; }
            public CharacterMotionMatchingDatabaseId DatabaseId { get; }
            public int DatabaseRevision { get; }
            public CharacterMotionMatchingFeatureSchemaId FeatureSchemaId { get; }
            public int FeatureSchemaRevision { get; }
            public string RigId { get; }
            public string RigRevision { get; }
            public MotionMatchingClipDependencyIdentity[] Dependencies { get; }
            public StableHash AnalysisInputHash { get; }
            public StableHash OrderedDependencyHash { get; }
            public StableHash ContentHash { get; }
        }
    }
}
