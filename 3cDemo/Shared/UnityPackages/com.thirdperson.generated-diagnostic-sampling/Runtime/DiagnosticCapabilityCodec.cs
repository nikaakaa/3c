using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public static class DiagnosticCapabilityCodec
    {
        const int Revision = 1;
        const int MaximumCollectionCount = 4096;
        const string CapabilitySetMagic = "diagnostic-capability-set/1";
        const string SchemaMagic = "diagnostic-schema/1";
        const string RuntimeManifestMagic = "diagnostic-runtime-manifest/1";
        const string CapabilityManifestMagic = "diagnostic-capability-manifest/1";

        public static DiagnosticEncodedDocument EncodeCapabilitySet(
            DiagnosticCapabilitySet set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            return Encode(writer =>
            {
                writer.Write(CapabilitySetMagic);
                writer.Write(Revision);
                writer.Write(set.Identity);
                writer.Write(set.Capabilities.Count);
                foreach (DiagnosticCapabilityBuildDescriptor capability in set.Capabilities)
                    WriteCapability(writer, capability);
            });
        }

        public static DiagnosticCapabilitySet DecodeCapabilitySet(
            DiagnosticEncodedDocument document) => Decode(
                document,
                CapabilitySetMagic,
                reader =>
                {
                    string identity = reader.ReadString();
                    int count = ReadCount(reader);
                    var capabilities = new DiagnosticCapabilityBuildDescriptor[count];
                    for (int i = 0; i < count; i++)
                        capabilities[i] = ReadCapability(reader);
                    var set = new DiagnosticCapabilitySet(capabilities);
                    if (!string.Equals(set.Identity, identity, StringComparison.Ordinal))
                        throw new InvalidDataException("Diagnostic capability set identity is invalid.");
                    return set;
                });

        public static DiagnosticEncodedDocument EncodeSchema(DiagnosticSchemaLayout schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            return Encode(writer =>
            {
                writer.Write(SchemaMagic);
                writer.Write(Revision);
                writer.Write(schema.CapabilityId);
                writer.Write(schema.CapabilityRevision);
                writer.Write(schema.ProgramId);
                writer.Write(schema.SamplerSetIdentity);
                writer.Write(schema.Identity);
                WriteLayout(writer, schema.PacketLayout);
                WriteFields(writer, schema.Fields);
                writer.Write(schema.Tables.Count);
                foreach (DiagnosticTableSchema table in schema.Tables)
                {
                    writer.Write(table.Id);
                    writer.Write(table.Revision);
                    writer.Write(table.DenseIndex);
                    writer.Write(table.Capacity);
                    WriteFields(writer, table.Fields);
                }
                writer.Write(schema.Samplers.Count);
                foreach (DiagnosticSamplerLayout sampler in schema.Samplers)
                {
                    writer.Write(sampler.Id);
                    writer.Write(sampler.Revision);
                    writer.Write(sampler.HostAdapterId);
                    writer.Write(sampler.OutputSchema);
                    writer.Write(sampler.Fields.Count);
                    foreach (DiagnosticFieldHandle field in sampler.Fields)
                        writer.Write(field.Id);
                    writer.Write(sampler.Tables.Count);
                    foreach (DiagnosticTableSchema table in sampler.Tables)
                        writer.Write(table.Id);
                }
            });
        }

        public static DiagnosticSchemaLayout DecodeSchema(
            DiagnosticEncodedDocument document) => Decode(
                document,
                SchemaMagic,
                reader =>
                {
                    string capabilityId = reader.ReadString();
                    int capabilityRevision = reader.ReadInt32();
                    string programId = reader.ReadString();
                    string samplerSetIdentity = reader.ReadString();
                    string schemaIdentity = reader.ReadString();
                    DiagnosticPacketLayout layout = ReadLayout(reader);
                    DiagnosticFieldHandle[] fields = ReadFields(reader);
                    int tableCount = ReadCount(reader);
                    var tables = new DiagnosticTableSchema[tableCount];
                    for (int i = 0; i < tableCount; i++)
                    {
                        tables[i] = new DiagnosticTableSchema(
                            reader.ReadString(),
                            reader.ReadInt32(),
                            reader.ReadInt32(),
                            reader.ReadInt32(),
                            ReadFields(reader));
                    }
                    int samplerCount = ReadCount(reader);
                    var samplers = new DiagnosticSamplerLayout[samplerCount];
                    for (int i = 0; i < samplerCount; i++)
                    {
                        string samplerId = reader.ReadString();
                        int samplerRevision = reader.ReadInt32();
                        string hostAdapterId = reader.ReadString();
                        string outputSchema = reader.ReadString();
                        DiagnosticFieldHandle[] selectedFields = ReadReferences(
                            reader,
                            fields,
                            value => value.Id);
                        DiagnosticTableSchema[] selectedTables = ReadReferences(
                            reader,
                            tables,
                            value => value.Id);
                        samplers[i] = new DiagnosticSamplerLayout(
                            samplerId,
                            samplerRevision,
                            hostAdapterId,
                            outputSchema,
                            selectedFields,
                            selectedTables);
                    }
                    return new DiagnosticSchemaLayout(
                        capabilityId,
                        capabilityRevision,
                        programId,
                        samplerSetIdentity,
                        schemaIdentity,
                        layout,
                        fields,
                        tables,
                        samplers);
                });

        public static DiagnosticEncodedDocument EncodeRuntimeManifest(
            DiagnosticRuntimeManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            return Encode(writer =>
            {
                writer.Write(RuntimeManifestMagic);
                writer.Write(Revision);
                WriteCapability(writer, manifest.Capability);
                writer.Write((int)manifest.Status);
                writer.Write(manifest.SampleCount);
                WriteOptionalArtifact(writer, manifest.Schema);
                WriteOptionalArtifact(writer, manifest.Packet);
                WriteFailure(writer, manifest.Failure);
            });
        }

        public static DiagnosticRuntimeManifest DecodeRuntimeManifest(
            DiagnosticEncodedDocument document) => Decode(
                document,
                RuntimeManifestMagic,
                reader => new DiagnosticRuntimeManifest(
                    ReadCapability(reader),
                    ReadStatus(reader),
                    reader.ReadUInt64(),
                    ReadOptionalArtifact(reader),
                    ReadOptionalArtifact(reader),
                    ReadFailure(reader)));

        public static DiagnosticEncodedDocument EncodeCapabilityManifest(
            DiagnosticCapabilityManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            return Encode(writer =>
            {
                writer.Write(CapabilityManifestMagic);
                writer.Write(Revision);
                WriteCapability(writer, manifest.Capability);
                writer.Write((int)manifest.Status);
                writer.Write(manifest.SampleCount);
                WriteOptionalArtifact(writer, manifest.Schema);
                WriteOptionalArtifact(writer, manifest.Packet);
                writer.Write(manifest.Samplers.Count);
                foreach (DiagnosticSamplerManifest sampler in manifest.Samplers)
                {
                    writer.Write(sampler.SamplerId);
                    writer.Write(sampler.SchemaIdentity);
                    writer.Write(sampler.Artifacts.Count);
                    foreach (DiagnosticSealedArtifact artifact in sampler.Artifacts)
                        WriteArtifact(writer, artifact);
                }
                WriteFailure(writer, manifest.Failure);
            });
        }

        public static DiagnosticCapabilityManifest DecodeCapabilityManifest(
            DiagnosticEncodedDocument document) => Decode(
                document,
                CapabilityManifestMagic,
                reader =>
                {
                    DiagnosticCapabilityBuildDescriptor capability = ReadCapability(reader);
                    DiagnosticCaptureStatus status = ReadStatus(reader);
                    ulong sampleCount = reader.ReadUInt64();
                    DiagnosticSealedArtifact schema = ReadOptionalArtifact(reader);
                    DiagnosticSealedArtifact packet = ReadOptionalArtifact(reader);
                    int samplerCount = ReadCount(reader);
                    var samplers = new DiagnosticSamplerManifest[samplerCount];
                    for (int i = 0; i < samplerCount; i++)
                    {
                        string samplerId = reader.ReadString();
                        string schemaIdentity = reader.ReadString();
                        int artifactCount = ReadCount(reader);
                        var artifacts = new DiagnosticSealedArtifact[artifactCount];
                        for (int artifactIndex = 0; artifactIndex < artifactCount; artifactIndex++)
                            artifacts[artifactIndex] = ReadArtifact(reader);
                        samplers[i] = new DiagnosticSamplerManifest(
                            samplerId,
                            schemaIdentity,
                            artifacts);
                    }
                    return new DiagnosticCapabilityManifest(
                        capability,
                        status,
                        sampleCount,
                        schema,
                        packet,
                        samplers,
                        ReadFailure(reader));
                });

        static DiagnosticEncodedDocument Encode(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, new UTF8Encoding(false), true))
                {
                    write(writer);
                    writer.Flush();
                }
                return DiagnosticEncodedDocument.Create(stream.ToArray());
            }
        }

        static T Decode<T>(
            DiagnosticEncodedDocument document,
            string expectedMagic,
            Func<BinaryReader, T> read)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            using (var stream = new MemoryStream(document.CopyContent(), false))
            using (var reader = new BinaryReader(stream, new UTF8Encoding(false), true))
            {
                string magic = reader.ReadString();
                int revision = reader.ReadInt32();
                if (!string.Equals(magic, expectedMagic, StringComparison.Ordinal) ||
                    revision != Revision)
                {
                    throw new InvalidDataException("Diagnostic document codec identity is invalid.");
                }
                T value = read(reader);
                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Diagnostic document has trailing data.");
                return value;
            }
        }

        static void WriteCapability(
            BinaryWriter writer,
            DiagnosticCapabilityBuildDescriptor capability)
        {
            writer.Write(capability.CapabilityId);
            writer.Write(capability.CapabilityRevision);
            writer.Write((int)capability.Mode);
            writer.Write(capability.ProgramId);
            writer.Write(capability.SamplerSetIdentity);
            writer.Write(capability.SchemaIdentity);
            writer.Write(capability.GeneratedProgramHash);
            writer.Write(capability.GeneratorIdentity);
            writer.Write(capability.PacketLayoutIdentity);
            writer.Write(capability.CadenceIdentity);
            writer.Write(capability.LineageTypeIdentity);
            writer.Write(capability.PacketCapacity);
            writer.Write(capability.WriterTransportIdentity);
        }

        static DiagnosticCapabilityBuildDescriptor ReadCapability(BinaryReader reader) =>
            new DiagnosticCapabilityBuildDescriptor(
                reader.ReadString(),
                reader.ReadInt32(),
                ReadEnum<DiagnosticCapabilityMode>(reader),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadInt32(),
                reader.ReadString());

        static void WriteLayout(BinaryWriter writer, DiagnosticPacketLayout layout)
        {
            writer.Write(layout.Identity);
            writer.Write(layout.BooleanCount);
            writer.Write(layout.Int32Count);
            writer.Write(layout.UInt32Count);
            writer.Write(layout.Int64Count);
            writer.Write(layout.UInt64Count);
            writer.Write(layout.Float32Count);
            writer.Write(layout.Float64Count);
            writer.Write(layout.IdentityCount);
            writer.Write(layout.Vector2Count);
            writer.Write(layout.Vector3Count);
            writer.Write(layout.Vector4Count);
            writer.Write(layout.QuaternionCount);
            writer.Write(layout.Tables.Count);
            foreach (DiagnosticTableLayout table in layout.Tables)
            {
                writer.Write(table.Id);
                writer.Write(table.Capacity);
                WriteLayout(writer, table.RowLayout);
            }
        }

        static DiagnosticPacketLayout ReadLayout(BinaryReader reader)
        {
            string identity = reader.ReadString();
            int booleanCount = reader.ReadInt32();
            int int32Count = reader.ReadInt32();
            int uint32Count = reader.ReadInt32();
            int int64Count = reader.ReadInt32();
            int uint64Count = reader.ReadInt32();
            int float32Count = reader.ReadInt32();
            int float64Count = reader.ReadInt32();
            int identityCount = reader.ReadInt32();
            int vector2Count = reader.ReadInt32();
            int vector3Count = reader.ReadInt32();
            int vector4Count = reader.ReadInt32();
            int quaternionCount = reader.ReadInt32();
            int tableCount = ReadCount(reader);
            var tables = new DiagnosticTableLayout[tableCount];
            for (int i = 0; i < tableCount; i++)
            {
                tables[i] = new DiagnosticTableLayout(
                    reader.ReadString(),
                    reader.ReadInt32(),
                    ReadLayout(reader));
            }
            return new DiagnosticPacketLayout(
                identity,
                booleanCount,
                int32Count,
                uint32Count,
                int64Count,
                uint64Count,
                float32Count,
                float64Count,
                identityCount,
                vector2Count,
                vector3Count,
                vector4Count,
                quaternionCount,
                tables);
        }

        static void WriteFields(
            BinaryWriter writer,
            IReadOnlyList<DiagnosticFieldHandle> fields)
        {
            writer.Write(fields.Count);
            foreach (DiagnosticFieldHandle field in fields)
            {
                writer.Write(field.Id);
                writer.Write(field.Revision);
                writer.Write((int)field.ValueKind);
                writer.Write(field.Unit);
                writer.Write(field.DenseIndex);
                writer.Write(field.Availability.IsDeclared);
                if (field.Availability.IsDeclared)
                {
                    writer.Write((int)field.Availability.ValueKind);
                    writer.Write(field.Availability.DenseIndex);
                    writer.Write(field.Availability.ExpectedValue);
                }
                writer.Write(field.Derived);
            }
        }

        static DiagnosticFieldHandle[] ReadFields(BinaryReader reader)
        {
            int count = ReadCount(reader);
            var fields = new DiagnosticFieldHandle[count];
            for (int i = 0; i < count; i++)
            {
                string id = reader.ReadString();
                int revision = reader.ReadInt32();
                DiagnosticValueKind valueKind = ReadEnum<DiagnosticValueKind>(reader);
                string unit = reader.ReadString();
                int denseIndex = reader.ReadInt32();
                DiagnosticFieldAvailability availability = reader.ReadBoolean()
                    ? new DiagnosticFieldAvailability(
                        ReadEnum<DiagnosticValueKind>(reader),
                        reader.ReadInt32(),
                        reader.ReadInt64())
                    : default;
                fields[i] = new DiagnosticFieldHandle(
                    id,
                    revision,
                    valueKind,
                    unit,
                    denseIndex,
                    availability,
                    reader.ReadBoolean());
            }
            return fields;
        }

        static T[] ReadReferences<T>(
            BinaryReader reader,
            IReadOnlyList<T> values,
            Func<T, string> identity) where T : class
        {
            int count = ReadCount(reader);
            var result = new T[count];
            for (int i = 0; i < count; i++)
            {
                string id = reader.ReadString();
                result[i] = values.SingleOrDefault(value => string.Equals(
                    identity(value),
                    id,
                    StringComparison.Ordinal));
                if (ReferenceEquals(result[i], null))
                    throw new InvalidDataException($"Diagnostic schema reference '{id}' is invalid.");
            }
            return result;
        }

        static void WriteOptionalArtifact(
            BinaryWriter writer,
            DiagnosticSealedArtifact artifact)
        {
            writer.Write(artifact != null);
            if (artifact != null)
                WriteArtifact(writer, artifact);
        }

        static DiagnosticSealedArtifact ReadOptionalArtifact(BinaryReader reader) =>
            reader.ReadBoolean() ? ReadArtifact(reader) : null;

        static void WriteArtifact(BinaryWriter writer, DiagnosticSealedArtifact artifact)
        {
            writer.Write(artifact.Path);
            writer.Write(artifact.Size);
            writer.Write(artifact.Sha256);
        }

        static DiagnosticSealedArtifact ReadArtifact(BinaryReader reader) =>
            new DiagnosticSealedArtifact(
                reader.ReadString(),
                reader.ReadInt64(),
                reader.ReadString());

        static void WriteFailure(
            BinaryWriter writer,
            DiagnosticCaptureFailure? failure)
        {
            writer.Write(failure.HasValue);
            if (!failure.HasValue)
                return;
            DiagnosticCaptureFailure value = failure.Value;
            writer.Write((int)value.Stage);
            writer.Write(value.CapabilityId);
            writer.Write(value.ProgramId);
            writer.Write(value.SamplerId);
            writer.Write(value.MemberId);
            writer.Write(value.Message);
        }

        static DiagnosticCaptureFailure? ReadFailure(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;
            return new DiagnosticCaptureFailure(
                ReadEnum<DiagnosticCaptureFailureStage>(reader),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString());
        }

        static DiagnosticCaptureStatus ReadStatus(BinaryReader reader) =>
            ReadEnum<DiagnosticCaptureStatus>(reader);

        static T ReadEnum<T>(BinaryReader reader) where T : struct
        {
            int raw = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(T), raw))
                throw new InvalidDataException($"Diagnostic enum '{typeof(T).Name}' is invalid.");
            return (T)Enum.ToObject(typeof(T), raw);
        }

        static int ReadCount(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > MaximumCollectionCount)
                throw new InvalidDataException("Diagnostic collection count is invalid.");
            return count;
        }
    }
}
