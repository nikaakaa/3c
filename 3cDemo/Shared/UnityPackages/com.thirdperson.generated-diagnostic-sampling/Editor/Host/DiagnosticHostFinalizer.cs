using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling.Host
{
    public interface IDiagnosticHostAdapter
    {
        string SamplerId { get; }
        DiagnosticSamplerManifest Finalize(DiagnosticHostContext context);
    }

    public sealed class DiagnosticHostContext
    {
        public DiagnosticHostContext(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticPacketLayout layout,
            IReadOnlyList<DiagnosticCapturePacket> packets)
        {
            Capability = capability;
            Layout = layout;
            Packets = packets;
        }

        public DiagnosticCapabilityBuildDescriptor Capability { get; }
        public DiagnosticPacketLayout Layout { get; }
        public IReadOnlyList<DiagnosticCapturePacket> Packets { get; }
    }

    public sealed class DiagnosticHostFinalizer
    {
        public DiagnosticCapabilityManifest Finalize(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticPacketLayout layout,
            DiagnosticSealedPacketArtifact artifact,
            IEnumerable<IDiagnosticHostAdapter> adapters)
        {
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            IDiagnosticHostAdapter[] orderedAdapters = (adapters ??
                    throw new ArgumentNullException(nameof(adapters)))
                .OrderBy(value => value.SamplerId, StringComparer.Ordinal)
                .ToArray();
            if (orderedAdapters.Length == 0)
                throw new ArgumentException("Diagnostic host adapters are required.", nameof(adapters));
            for (int i = 1; i < orderedAdapters.Length; i++)
            {
                if (string.Equals(
                        orderedAdapters[i - 1].SamplerId,
                        orderedAdapters[i].SamplerId,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException("Diagnostic host adapter identity is duplicated.", nameof(adapters));
                }
            }

            DiagnosticCapturePacket[] packets;
            try
            {
                using (var reader = new DiagnosticSealedPacketReader(
                    artifact,
                    capability,
                    layout))
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

            var context = new DiagnosticHostContext(capability, layout, packets);
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
