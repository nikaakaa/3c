using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public sealed class DiagnosticSamplerManifest
    {
        public DiagnosticSamplerManifest(
            string samplerId,
            string schemaIdentity,
            IReadOnlyList<DiagnosticSealedArtifact> artifacts)
        {
            SamplerId = DiagnosticIdentity.RequireId(samplerId, nameof(samplerId));
            SchemaIdentity = DiagnosticIdentity.RequireId(
                schemaIdentity,
                nameof(schemaIdentity));
            Artifacts = (artifacts ?? throw new ArgumentNullException(nameof(artifacts)))
                .OrderBy(value => value.Path, StringComparer.Ordinal)
                .ToArray();
            if (Artifacts.Count == 0)
                throw new ArgumentException("Diagnostic sampler manifest requires artifacts.", nameof(artifacts));
            for (int i = 1; i < Artifacts.Count; i++)
            {
                if (string.Equals(
                        Artifacts[i - 1].Path,
                        Artifacts[i].Path,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException("Diagnostic sampler artifact path is duplicated.", nameof(artifacts));
                }
            }
        }

        public string SamplerId { get; }
        public string SchemaIdentity { get; }
        public IReadOnlyList<DiagnosticSealedArtifact> Artifacts { get; }
    }

    public sealed class DiagnosticRuntimeManifest
    {
        public DiagnosticRuntimeManifest(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticCaptureStatus status,
            ulong sampleCount,
            DiagnosticSealedArtifact schema,
            DiagnosticSealedArtifact packet,
            DiagnosticCaptureFailure? failure)
        {
            Capability = capability ?? throw new ArgumentNullException(nameof(capability));
            if (status != DiagnosticCaptureStatus.Finalizing &&
                status != DiagnosticCaptureStatus.Faulted &&
                status != DiagnosticCaptureStatus.Cancelled)
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }
            Status = status;
            SampleCount = sampleCount;
            Schema = schema;
            Packet = packet;
            Failure = failure;
            if (status == DiagnosticCaptureStatus.Finalizing &&
                (sampleCount == 0 || schema == null || packet == null || failure.HasValue))
            {
                throw new ArgumentException("Finalizing runtime manifest is incomplete.");
            }
            if (status == DiagnosticCaptureStatus.Faulted && !failure.HasValue)
                throw new ArgumentException("Faulted runtime manifest requires a failure.");
            if (failure.HasValue)
                RequireFailure(capability, failure.Value);
        }

        public DiagnosticCapabilityBuildDescriptor Capability { get; }
        public DiagnosticCaptureStatus Status { get; }
        public ulong SampleCount { get; }
        public DiagnosticSealedArtifact Schema { get; }
        public DiagnosticSealedArtifact Packet { get; }
        public DiagnosticCaptureFailure? Failure { get; }

        internal static void RequireFailure(
            DiagnosticCapabilityBuildDescriptor capability,
            in DiagnosticCaptureFailure failure)
        {
            if (!string.Equals(
                    failure.CapabilityId,
                    capability.CapabilityId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    failure.ProgramId,
                    capability.ProgramId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException("Diagnostic failure identity does not match the capability.");
            }
        }
    }

    public sealed class DiagnosticCapabilityManifest
    {
        public DiagnosticCapabilityManifest(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticCaptureStatus status,
            ulong sampleCount,
            DiagnosticSealedArtifact schema,
            DiagnosticSealedArtifact packet,
            IReadOnlyList<DiagnosticSamplerManifest> samplers,
            DiagnosticCaptureFailure? failure)
        {
            Capability = capability ?? throw new ArgumentNullException(nameof(capability));
            Status = status;
            SampleCount = sampleCount;
            Schema = schema;
            Packet = packet;
            Samplers = DiagnosticIdentity.NormalizeDescriptors(
                samplers,
                value => value.SamplerId,
                nameof(samplers));
            Failure = failure;
            if (status == DiagnosticCaptureStatus.Completed &&
                (sampleCount == 0 ||
                    schema == null ||
                    packet == null ||
                    Samplers.Count == 0 ||
                    failure.HasValue))
            {
                throw new ArgumentException("Completed capability manifest is incomplete.");
            }
            if (status == DiagnosticCaptureStatus.Faulted && !failure.HasValue)
                throw new ArgumentException("Faulted capability manifest requires a failure.");
            if (failure.HasValue)
                DiagnosticRuntimeManifest.RequireFailure(capability, failure.Value);
        }

        public DiagnosticCapabilityBuildDescriptor Capability { get; }
        public DiagnosticCaptureStatus Status { get; }
        public ulong SampleCount { get; }
        public DiagnosticSealedArtifact Schema { get; }
        public DiagnosticSealedArtifact Packet { get; }
        public IReadOnlyList<DiagnosticSamplerManifest> Samplers { get; }
        public DiagnosticCaptureFailure? Failure { get; }
    }
}
