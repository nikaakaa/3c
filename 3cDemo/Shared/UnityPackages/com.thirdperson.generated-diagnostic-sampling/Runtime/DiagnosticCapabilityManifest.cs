using System;
using System.Collections.Generic;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public sealed class DiagnosticSamplerManifest
    {
        public DiagnosticSamplerManifest(
            string samplerId,
            string schemaIdentity,
            IReadOnlyList<DiagnosticSealedPacketArtifact> artifacts)
        {
            SamplerId = DiagnosticIdentity.RequireId(samplerId, nameof(samplerId));
            SchemaIdentity = DiagnosticIdentity.RequireId(
                schemaIdentity,
                nameof(schemaIdentity));
            Artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        }

        public string SamplerId { get; }
        public string SchemaIdentity { get; }
        public IReadOnlyList<DiagnosticSealedPacketArtifact> Artifacts { get; }
    }

    public sealed class DiagnosticCapabilityManifest
    {
        public DiagnosticCapabilityManifest(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticCaptureStatus status,
            ulong sampleCount,
            DiagnosticSealedPacketArtifact packet,
            IReadOnlyList<DiagnosticSamplerManifest> samplers,
            DiagnosticCaptureFailure? failure)
        {
            Capability = capability ?? throw new ArgumentNullException(nameof(capability));
            Status = status;
            SampleCount = sampleCount;
            Packet = packet;
            Samplers = samplers ?? Array.Empty<DiagnosticSamplerManifest>();
            Failure = failure;
            if (status == DiagnosticCaptureStatus.Completed &&
                (packet == null || failure.HasValue))
            {
                throw new ArgumentException("Completed capability manifest is incomplete.");
            }
        }

        public DiagnosticCapabilityBuildDescriptor Capability { get; }
        public DiagnosticCaptureStatus Status { get; }
        public ulong SampleCount { get; }
        public DiagnosticSealedPacketArtifact Packet { get; }
        public IReadOnlyList<DiagnosticSamplerManifest> Samplers { get; }
        public DiagnosticCaptureFailure? Failure { get; }
    }
}
