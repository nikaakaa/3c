using System;
using ThirdPerson.GeneratedDiagnosticSampling;

namespace ThirdPerson.GeneratedDiagnosticSampling.Probe
{
    internal readonly struct ProbeCommittedView
    {
        internal ProbeCommittedView(int value)
        {
            Value = value;
        }

        internal int Value { get; }
    }

    internal readonly struct ProbeCaptureMetadata
    {
        internal ProbeCaptureMetadata(bool enabled, int offset, int tableCount)
        {
            Enabled = enabled;
            Offset = offset;
            TableCount = tableCount;
        }

        internal bool Enabled { get; }
        internal int Offset { get; }
        internal int TableCount { get; }
    }

    [DiagnosticCapability(
        "probe-capability",
        1,
        typeof(ProbeCommittedView),
        typeof(ProbeCaptureMetadata))]
    internal static class ProbeCapabilityDefinition
    {
    }

    internal static class ProbeFields
    {
        [DiagnosticField(
            "probe-capability",
            "probe.available",
            1,
            DiagnosticValueKind.Boolean,
            "flag",
            "main",
            "probe.core")]
        internal static bool Available(
            in ProbeCommittedView view,
            in ProbeCaptureMetadata metadata) => metadata.Enabled;

        [DiagnosticField(
            "probe-capability",
            "probe.value",
            1,
            DiagnosticValueKind.Int32,
            "count",
            "main",
            "probe.core",
            AvailabilityFieldId = "probe.available",
            Dependencies = new[] { "probe.available" })]
        internal static int Value(
            in ProbeCommittedView view,
            in ProbeCaptureMetadata metadata) => view.Value + metadata.Offset;
    }

    [DiagnosticTable("probe-capability", "probe.table", 1, 2)]
    internal static class ProbeTable
    {
        [DiagnosticTableCount("probe-capability", "probe.table")]
        internal static int Count(
            in ProbeCommittedView view,
            in ProbeCaptureMetadata metadata) => metadata.TableCount;

        [DiagnosticField(
            "probe-capability",
            "probe.table.value",
            1,
            DiagnosticValueKind.Int32,
            "count",
            "probe.table",
            "probe.table")]
        internal static int Value(
            in ProbeCommittedView view,
            in ProbeCaptureMetadata metadata,
            int row) => view.Value + metadata.Offset + row;
    }

    [DiagnosticSampler(
        "probe-capability",
        "probe-sampler",
        1,
        "probe-host",
        "probe-output/1",
        "probe.core",
        Tables = new[] { "probe.table" })]
    internal static class ProbeSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        "probe-program",
        "probe-capability",
        typeof(ProbeSamplerDefinition))]
    internal static partial class ProbeProgramDefinition
    {
    }

    internal static class ProbeProgramConsumer
    {
        internal static int Capture(int value)
        {
            DiagnosticSchemaLayout schema =
                ProbeProgramDefinition.CreateDiagnosticSchemaLayout();
            DiagnosticCapabilityBuildDescriptor capability =
                ProbeProgramDefinition.CreateDiagnosticCapabilityBuildDescriptor(
                    "probe-cadence/1",
                    "probe-lineage/1",
                    4,
                    "probe-binary/1");
            schema.Require(capability);
            DiagnosticEncodedDocument schemaDocument =
                DiagnosticCapabilityCodec.EncodeSchema(schema);
            DiagnosticSchemaLayout decodedSchema =
                DiagnosticCapabilityCodec.DecodeSchema(schemaDocument);
            decodedSchema.Require(capability);
            var capabilitySet = new DiagnosticCapabilitySet(new[] { capability });
            var closure = new DiagnosticCapabilityCompilationClosureDescriptor(
                "probe-capability",
                1,
                new[] { "Probe.Capture" },
                new[] { "PROBE_CAPTURE" });
            var proof = new DiagnosticCompilationClosureProof(
                capabilitySet,
                new[] { closure });
            if (proof.AssemblyNames.Count != 1)
                throw new InvalidOperationException("Probe compilation closure is invalid.");
            DiagnosticCapabilityCodec.DecodeCapabilitySet(
                DiagnosticCapabilityCodec.EncodeCapabilitySet(capabilitySet));
            var schemaArtifact = new DiagnosticSealedArtifact(
                "probe.schema",
                schemaDocument.Length,
                schemaDocument.Sha256);
            var packetArtifact = new DiagnosticSealedArtifact(
                "probe.packet",
                1,
                schemaDocument.Sha256);
            var runtimeManifest = new DiagnosticRuntimeManifest(
                capability,
                DiagnosticCaptureStatus.Finalizing,
                1,
                schemaArtifact,
                packetArtifact,
                null);
            DiagnosticCapabilityCodec.DecodeRuntimeManifest(
                DiagnosticCapabilityCodec.EncodeRuntimeManifest(runtimeManifest));
            DiagnosticPacketLayout layout = schema.PacketLayout;
            var packet = new DiagnosticCapturePacket(layout);
            var lineage = new DiagnosticLineageKey("probe-lineage/1", 0, 1);
            var sampleKey = new DiagnosticSampleKey(1, lineage);
            packet.Begin(sampleKey);
            var view = new ProbeCommittedView(value);
            var metadata = new ProbeCaptureMetadata(true, 0, 1);
            ProbeProgramDefinition.Capture(in view, in metadata, ref packet);
            return packet.Int32Values[0];
        }
    }
}
