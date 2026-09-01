using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling.Host
{
    public interface IDiagnosticHostAdapter
    {
        string Id { get; }
        string SamplerId { get; }
        DiagnosticSamplerManifest Finalize(DiagnosticHostContext context);
    }

    public sealed class DiagnosticHostContext
    {
        public DiagnosticHostContext(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticSchemaLayout schema,
            IReadOnlyList<DiagnosticCapturePacket> packets)
        {
            Capability = capability ?? throw new ArgumentNullException(nameof(capability));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            m_Packets = packets ?? throw new ArgumentNullException(nameof(packets));
        }

        readonly IReadOnlyList<DiagnosticCapturePacket> m_Packets;
        public DiagnosticCapabilityBuildDescriptor Capability { get; }
        public DiagnosticSchemaLayout Schema { get; }

        public DiagnosticSamplerPacketSeries GetSampler(string samplerId) =>
            new DiagnosticSamplerPacketSeries(Schema.RequireSampler(samplerId), m_Packets);
    }

    public sealed class DiagnosticHostFinalizationResult
    {
        public DiagnosticHostFinalizationResult(
            DiagnosticCapabilityManifest manifest,
            DiagnosticEncodedDocument encodedManifest)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            EncodedManifest = encodedManifest ?? throw new ArgumentNullException(nameof(encodedManifest));
        }

        public DiagnosticCapabilityManifest Manifest { get; }
        public DiagnosticEncodedDocument EncodedManifest { get; }
    }

    public sealed class DiagnosticHostFinalizer
    {
        public DiagnosticHostFinalizationResult Finalize(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticEncodedDocument encodedRuntimeManifest,
            DiagnosticSchemaLayout schema,
            IEnumerable<IDiagnosticHostAdapter> adapters)
        {
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));
            if (encodedRuntimeManifest == null)
                throw new ArgumentNullException(nameof(encodedRuntimeManifest));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            DiagnosticRuntimeManifest runtime;
            try
            {
                runtime = DiagnosticCapabilityCodec.DecodeRuntimeManifest(encodedRuntimeManifest);
                if (!string.Equals(
                        runtime.Capability.Identity,
                        capability.Identity,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Diagnostic runtime capability does not match the request.");
                }
                schema.Require(capability);
            }
            catch (Exception exception)
            {
                return Fault(
                    capability,
                    null,
                    null,
                    DiagnosticCaptureFailureStage.Preflight,
                    string.Empty,
                    exception.Message);
            }

            if (runtime.Status == DiagnosticCaptureStatus.Faulted ||
                runtime.Status == DiagnosticCaptureStatus.Cancelled)
            {
                return FinalizeManifest(new DiagnosticCapabilityManifest(
                    capability,
                    runtime.Status,
                    runtime.SampleCount,
                    runtime.Schema,
                    runtime.Packet,
                    Array.Empty<DiagnosticSamplerManifest>(),
                    runtime.Failure));
            }

            try
            {
                RequireSchemaArtifact(schema, runtime.Schema);
            }
            catch (Exception exception)
            {
                return Fault(
                    capability,
                    runtime.Schema,
                    runtime.Packet,
                    DiagnosticCaptureFailureStage.Reader,
                    string.Empty,
                    exception.Message);
            }

            IDiagnosticHostAdapter[] orderedAdapters;
            try
            {
                orderedAdapters = RequireAdapters(schema, adapters);
            }
            catch (Exception exception)
            {
                return Fault(
                    capability,
                    runtime.Schema,
                    runtime.Packet,
                    DiagnosticCaptureFailureStage.Preflight,
                    string.Empty,
                    exception.Message);
            }

            DiagnosticCapturePacket[] packets;
            try
            {
                using (var reader = new DiagnosticSealedPacketReader(
                    runtime.Packet,
                    capability,
                    schema.PacketLayout))
                {
                    packets = reader.ReadAll().ToArray();
                }
                if ((ulong)packets.Length != runtime.SampleCount)
                    throw new InvalidOperationException("Diagnostic packet count does not match the runtime manifest.");
            }
            catch (Exception exception)
            {
                return Fault(
                    capability,
                    runtime.Schema,
                    runtime.Packet,
                    DiagnosticCaptureFailureStage.Reader,
                    string.Empty,
                    exception.Message);
            }

            var context = new DiagnosticHostContext(capability, schema, packets);
            var samplers = new List<DiagnosticSamplerManifest>(orderedAdapters.Length);
            foreach (IDiagnosticHostAdapter adapter in orderedAdapters)
            {
                try
                {
                    DiagnosticSamplerManifest manifest = adapter.Finalize(context) ??
                        throw new InvalidOperationException("Diagnostic host adapter returned no manifest.");
                    if (!string.Equals(manifest.SamplerId, adapter.SamplerId, StringComparison.Ordinal) ||
                        !string.Equals(
                            manifest.SchemaIdentity,
                            capability.SchemaIdentity,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Diagnostic sampler manifest identity is invalid.");
                    }
                    foreach (DiagnosticSealedArtifact artifact in manifest.Artifacts)
                        DiagnosticArtifactStore.Require(artifact);
                    samplers.Add(manifest);
                }
                catch (Exception exception)
                {
                    return Fault(
                        capability,
                        runtime.Schema,
                        runtime.Packet,
                        DiagnosticCaptureFailureStage.Host,
                        adapter.SamplerId,
                        exception.Message);
                }
            }
            return FinalizeManifest(new DiagnosticCapabilityManifest(
                capability,
                DiagnosticCaptureStatus.Completed,
                runtime.SampleCount,
                runtime.Schema,
                runtime.Packet,
                samplers,
                null));
        }

        static IDiagnosticHostAdapter[] RequireAdapters(
            DiagnosticSchemaLayout schema,
            IEnumerable<IDiagnosticHostAdapter> adapters)
        {
            IDiagnosticHostAdapter[] ordered = (adapters ??
                    throw new ArgumentNullException(nameof(adapters)))
                .OrderBy(value => value.SamplerId, StringComparer.Ordinal)
                .ToArray();
            if (ordered.Length != schema.Samplers.Count)
                throw new ArgumentException("Diagnostic host adapter set does not match the schema.", nameof(adapters));
            for (int i = 0; i < ordered.Length; i++)
            {
                DiagnosticSamplerLayout sampler = schema.Samplers[i];
                IDiagnosticHostAdapter adapter = ordered[i];
                if (!string.Equals(adapter.SamplerId, sampler.Id, StringComparison.Ordinal) ||
                    !string.Equals(adapter.Id, sampler.HostAdapterId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Diagnostic host adapter set does not match the schema.", nameof(adapters));
                }
            }
            return ordered;
        }

        static void RequireSchemaArtifact(
            DiagnosticSchemaLayout schema,
            DiagnosticSealedArtifact artifact)
        {
            if (artifact == null)
                throw new InvalidOperationException("Diagnostic runtime manifest has no schema artifact.");
            DiagnosticEncodedDocument expected = DiagnosticCapabilityCodec.EncodeSchema(schema);
            if (artifact.Size != expected.Length ||
                !string.Equals(artifact.Sha256, expected.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Diagnostic schema artifact does not match the generated schema.");
            }
            DiagnosticArtifactStore.Require(artifact);
        }

        static DiagnosticHostFinalizationResult Fault(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticSealedArtifact schema,
            DiagnosticSealedArtifact packet,
            DiagnosticCaptureFailureStage stage,
            string samplerId,
            string message)
        {
            var failure = new DiagnosticCaptureFailure(
                stage,
                capability.CapabilityId,
                capability.ProgramId,
                samplerId,
                string.Empty,
                message);
            return FinalizeManifest(new DiagnosticCapabilityManifest(
                capability,
                DiagnosticCaptureStatus.Faulted,
                0,
                schema,
                packet,
                Array.Empty<DiagnosticSamplerManifest>(),
                failure));
        }

        static DiagnosticHostFinalizationResult FinalizeManifest(
            DiagnosticCapabilityManifest manifest) =>
            new DiagnosticHostFinalizationResult(
                manifest,
                DiagnosticCapabilityCodec.EncodeCapabilityManifest(manifest));
    }
}
