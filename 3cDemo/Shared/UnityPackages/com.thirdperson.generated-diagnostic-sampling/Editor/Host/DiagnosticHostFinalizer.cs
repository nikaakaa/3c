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

    public sealed class DiagnosticHostFinalizer
    {
        public DiagnosticCapabilityManifest Finalize(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticSchemaLayout schema,
            DiagnosticSealedPacketArtifact artifact,
            IEnumerable<IDiagnosticHostAdapter> adapters)
        {
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            schema.Require(capability);
            IDiagnosticHostAdapter[] orderedAdapters = (adapters ??
                    throw new ArgumentNullException(nameof(adapters)))
                .OrderBy(value => value.SamplerId, StringComparer.Ordinal)
                .ToArray();
            if (orderedAdapters.Length == 0)
                throw new ArgumentException("Diagnostic host adapters are required.", nameof(adapters));
            if (orderedAdapters.Length != schema.Samplers.Count)
                throw new ArgumentException("Diagnostic host adapter set does not match the schema.", nameof(adapters));
            for (int i = 0; i < orderedAdapters.Length; i++)
            {
                DiagnosticSamplerLayout sampler = schema.Samplers[i];
                IDiagnosticHostAdapter adapter = orderedAdapters[i];
                if (!string.Equals(adapter.SamplerId, sampler.Id, StringComparison.Ordinal) ||
                    !string.Equals(adapter.Id, sampler.HostAdapterId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Diagnostic host adapter set does not match the schema.", nameof(adapters));
                }
            }

            DiagnosticCapturePacket[] packets;
            try
            {
                using (var reader = new DiagnosticSealedPacketReader(
                    artifact,
                    capability,
                    schema.PacketLayout))
                {
                    packets = reader.ReadAll().ToArray();
                }
            }
            catch (Exception exception)
            {
                return Fault(
                    capability,
                    artifact,
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
                    samplers.Add(manifest);
                }
                catch (Exception exception)
                {
                    return Fault(
                        capability,
                        artifact,
                        DiagnosticCaptureFailureStage.Host,
                        adapter.SamplerId,
                        exception.Message);
                }
            }
            return new DiagnosticCapabilityManifest(
                capability,
                DiagnosticCaptureStatus.Completed,
                (ulong)packets.Length,
                artifact,
                samplers,
                null);
        }

        static DiagnosticCapabilityManifest Fault(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticSealedPacketArtifact artifact,
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
            return new DiagnosticCapabilityManifest(
                capability,
                DiagnosticCaptureStatus.Faulted,
                0,
                artifact,
                Array.Empty<DiagnosticSamplerManifest>(),
                failure);
        }
    }
}
