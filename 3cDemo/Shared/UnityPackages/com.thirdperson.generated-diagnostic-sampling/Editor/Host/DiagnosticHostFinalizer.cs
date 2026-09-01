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
            DiagnosticSealedPacketArtifact artifact,
            IEnumerable<IDiagnosticHostAdapter> adapters)
        {
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            IDiagnosticHostAdapter[] orderedAdapters = (adapters ??
                    throw new ArgumentNullException(nameof(adapters)))
                .OrderBy(value => value.SamplerId, StringComparer.Ordinal)
                .ToArray();
            try
            {
                DiagnosticCapturePacket[] packets;
                DiagnosticPacketLayout layout;
                using (var reader = new DiagnosticSealedPacketReader(artifact.Path))
                {
                    reader.Require(
                        capability.CapabilityId,
                        capability.ProgramId,
                        capability.SchemaIdentity,
                        capability.GeneratedProgramHash,
                        capability.PacketLayoutIdentity,
                        capability.LineageTypeIdentity);
                    layout = reader.Layout;
                    packets = reader.ReadAll().ToArray();
                }
                var context = new DiagnosticHostContext(capability, layout, packets);
                DiagnosticSamplerManifest[] samplers = orderedAdapters
                    .Select(value => value.Finalize(context))
                    .ToArray();
                return new DiagnosticCapabilityManifest(
                    capability,
                    DiagnosticCaptureStatus.Completed,
                    (ulong)packets.Length,
                    artifact,
                    samplers,
                    null);
            }
            catch (Exception exception)
            {
                var failure = new DiagnosticCaptureFailure(
                    DiagnosticCaptureFailureStage.Host,
                    capability.CapabilityId,
                    capability.ProgramId,
                    string.Empty,
                    string.Empty,
                    exception.Message);
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
}
