using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public sealed class DiagnosticCapabilityBuildDescriptor
    {
        public DiagnosticCapabilityBuildDescriptor(
            string capabilityId,
            int capabilityRevision,
            DiagnosticCapabilityMode mode,
            string programId,
            string samplerSetIdentity,
            string schemaIdentity,
            string generatedProgramHash,
            string generatorIdentity,
            string packetLayoutIdentity,
            string lineageTypeIdentity,
            int packetCapacity,
            string writerTransportIdentity)
        {
            CapabilityId = DiagnosticIdentity.RequireId(capabilityId, nameof(capabilityId));
            CapabilityRevision = DiagnosticIdentity.RequireRevision(
                capabilityRevision,
                nameof(capabilityRevision));
            if (!Enum.IsDefined(typeof(DiagnosticCapabilityMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            Mode = mode;
            if (mode == DiagnosticCapabilityMode.Disabled)
            {
                ProgramId = string.Empty;
                SamplerSetIdentity = string.Empty;
                SchemaIdentity = string.Empty;
                GeneratedProgramHash = string.Empty;
                GeneratorIdentity = string.Empty;
                PacketLayoutIdentity = string.Empty;
                LineageTypeIdentity = string.Empty;
                PacketCapacity = 0;
                WriterTransportIdentity = string.Empty;
                Identity = DiagnosticIdentity.Hash(new[] { CanonicalIdentity });
                return;
            }
            ProgramId = DiagnosticIdentity.RequireId(programId, nameof(programId));
            SamplerSetIdentity = DiagnosticIdentity.RequireId(
                samplerSetIdentity,
                nameof(samplerSetIdentity));
            SchemaIdentity = DiagnosticIdentity.RequireId(schemaIdentity, nameof(schemaIdentity));
            GeneratedProgramHash = DiagnosticIdentity.RequireId(
                generatedProgramHash,
                nameof(generatedProgramHash));
            GeneratorIdentity = DiagnosticIdentity.RequireId(
                generatorIdentity,
                nameof(generatorIdentity));
            PacketLayoutIdentity = DiagnosticIdentity.RequireId(
                packetLayoutIdentity,
                nameof(packetLayoutIdentity));
            LineageTypeIdentity = DiagnosticIdentity.RequireId(
                lineageTypeIdentity,
                nameof(lineageTypeIdentity));
            if (packetCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(packetCapacity));
            PacketCapacity = packetCapacity;
            WriterTransportIdentity = DiagnosticIdentity.RequireId(
                writerTransportIdentity,
                nameof(writerTransportIdentity));
            Identity = DiagnosticIdentity.Hash(new[] { CanonicalIdentity });
        }

        public string CapabilityId { get; }
        public int CapabilityRevision { get; }
        public DiagnosticCapabilityMode Mode { get; }
        public string ProgramId { get; }
        public string SamplerSetIdentity { get; }
        public string SchemaIdentity { get; }
        public string GeneratedProgramHash { get; }
        public string GeneratorIdentity { get; }
        public string PacketLayoutIdentity { get; }
        public string LineageTypeIdentity { get; }
        public int PacketCapacity { get; }
        public string WriterTransportIdentity { get; }
        public string Identity { get; }

        internal string CanonicalIdentity => string.Join("|", new[]
        {
            CapabilityId,
            CapabilityRevision.ToString(),
            Mode.ToString(),
            ProgramId,
            SamplerSetIdentity,
            SchemaIdentity,
            GeneratedProgramHash,
            GeneratorIdentity,
            PacketLayoutIdentity,
            LineageTypeIdentity,
            PacketCapacity.ToString(),
            WriterTransportIdentity
        });
    }

    public sealed class DiagnosticCapabilitySet
    {
        public DiagnosticCapabilitySet(IEnumerable<DiagnosticCapabilityBuildDescriptor> capabilities)
        {
            Capabilities = DiagnosticIdentity.NormalizeDescriptors(
                capabilities,
                value => value.CapabilityId,
                nameof(capabilities));
            Identity = DiagnosticIdentity.Hash(
                Capabilities.Select(value => value.CanonicalIdentity));
        }

        public IReadOnlyList<DiagnosticCapabilityBuildDescriptor> Capabilities { get; }
        public string Identity { get; }
    }
}
