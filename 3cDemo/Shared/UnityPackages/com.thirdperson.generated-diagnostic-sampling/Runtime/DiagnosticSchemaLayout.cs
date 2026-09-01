using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public readonly struct DiagnosticFieldAvailability
    {
        public DiagnosticFieldAvailability(
            DiagnosticValueKind valueKind,
            int denseIndex,
            long expectedValue)
        {
            if (valueKind < DiagnosticValueKind.Boolean ||
                valueKind > DiagnosticValueKind.UInt64)
            {
                throw new ArgumentOutOfRangeException(nameof(valueKind));
            }
            if (denseIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(denseIndex));
            if (valueKind == DiagnosticValueKind.Boolean &&
                expectedValue != 0 && expectedValue != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedValue));
            }
            if (valueKind == DiagnosticValueKind.Int32 &&
                (expectedValue < int.MinValue || expectedValue > int.MaxValue))
            {
                throw new ArgumentOutOfRangeException(nameof(expectedValue));
            }
            if (valueKind == DiagnosticValueKind.UInt32 &&
                (expectedValue < 0 || expectedValue > uint.MaxValue))
            {
                throw new ArgumentOutOfRangeException(nameof(expectedValue));
            }
            if (valueKind == DiagnosticValueKind.UInt64 && expectedValue < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedValue));
            ValueKind = valueKind;
            DenseIndex = denseIndex;
            ExpectedValue = expectedValue;
        }

        public DiagnosticValueKind ValueKind { get; }
        public int DenseIndex { get; }
        public long ExpectedValue { get; }
        public bool IsDeclared => ValueKind != 0;
    }

    public sealed class DiagnosticFieldHandle
    {
        public DiagnosticFieldHandle(
            string id,
            int revision,
            DiagnosticValueKind valueKind,
            string unit,
            int denseIndex,
            DiagnosticFieldAvailability availability,
            bool derived)
        {
            Id = DiagnosticIdentity.RequireId(id, nameof(id));
            Revision = DiagnosticIdentity.RequireRevision(revision, nameof(revision));
            if (!Enum.IsDefined(typeof(DiagnosticValueKind), valueKind))
                throw new ArgumentOutOfRangeException(nameof(valueKind));
            ValueKind = valueKind;
            Unit = DiagnosticIdentity.RequireText(unit, nameof(unit));
            if (denseIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(denseIndex));
            DenseIndex = denseIndex;
            Availability = availability;
            Derived = derived;
        }

        public string Id { get; }
        public int Revision { get; }
        public DiagnosticValueKind ValueKind { get; }
        public string Unit { get; }
        public int DenseIndex { get; }
        public DiagnosticFieldAvailability Availability { get; }
        public bool Derived { get; }
    }

    public sealed class DiagnosticTableSchema
    {
        public DiagnosticTableSchema(
            string id,
            int revision,
            int denseIndex,
            int capacity,
            IEnumerable<DiagnosticFieldHandle> fields)
        {
            Id = DiagnosticIdentity.RequireId(id, nameof(id));
            Revision = DiagnosticIdentity.RequireRevision(revision, nameof(revision));
            if (denseIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(denseIndex));
            DenseIndex = denseIndex;
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            Fields = NormalizeFields(fields, nameof(fields));
            if (Fields.Count == 0)
                throw new ArgumentException("Diagnostic table schema requires fields.", nameof(fields));
        }

        public string Id { get; }
        public int Revision { get; }
        public int DenseIndex { get; }
        public int Capacity { get; }
        public IReadOnlyList<DiagnosticFieldHandle> Fields { get; }

        internal static IReadOnlyList<DiagnosticFieldHandle> NormalizeFields(
            IEnumerable<DiagnosticFieldHandle> fields,
            string parameterName) => DiagnosticIdentity.NormalizeDescriptors(
                fields,
                value => value.Id,
                parameterName);
    }

    public sealed class DiagnosticSamplerLayout
    {
        public DiagnosticSamplerLayout(
            string id,
            int revision,
            string hostAdapterId,
            string outputSchema,
            IEnumerable<DiagnosticFieldHandle> fields,
            IEnumerable<DiagnosticTableSchema> tables)
        {
            Id = DiagnosticIdentity.RequireId(id, nameof(id));
            Revision = DiagnosticIdentity.RequireRevision(revision, nameof(revision));
            HostAdapterId = DiagnosticIdentity.RequireId(hostAdapterId, nameof(hostAdapterId));
            OutputSchema = DiagnosticIdentity.RequireId(outputSchema, nameof(outputSchema));
            Fields = DiagnosticTableSchema.NormalizeFields(fields, nameof(fields));
            Tables = DiagnosticIdentity.NormalizeDescriptors(
                tables,
                value => value.Id,
                nameof(tables));
            if (Fields.Count == 0 && Tables.Count == 0)
                throw new ArgumentException("Diagnostic sampler layout is empty.");
        }

        public string Id { get; }
        public int Revision { get; }
        public string HostAdapterId { get; }
        public string OutputSchema { get; }
        public IReadOnlyList<DiagnosticFieldHandle> Fields { get; }
        public IReadOnlyList<DiagnosticTableSchema> Tables { get; }
    }

    public sealed class DiagnosticSchemaLayout
    {
        public DiagnosticSchemaLayout(
            string capabilityId,
            int capabilityRevision,
            string programId,
            string samplerSetIdentity,
            string schemaIdentity,
            DiagnosticPacketLayout packetLayout,
            IEnumerable<DiagnosticFieldHandle> fields,
            IEnumerable<DiagnosticTableSchema> tables,
            IEnumerable<DiagnosticSamplerLayout> samplers)
        {
            CapabilityId = DiagnosticIdentity.RequireId(capabilityId, nameof(capabilityId));
            CapabilityRevision = DiagnosticIdentity.RequireRevision(
                capabilityRevision,
                nameof(capabilityRevision));
            ProgramId = DiagnosticIdentity.RequireId(programId, nameof(programId));
            SamplerSetIdentity = DiagnosticIdentity.RequireId(
                samplerSetIdentity,
                nameof(samplerSetIdentity));
            Identity = DiagnosticIdentity.RequireId(schemaIdentity, nameof(schemaIdentity));
            PacketLayout = packetLayout ?? throw new ArgumentNullException(nameof(packetLayout));
            Fields = DiagnosticTableSchema.NormalizeFields(fields, nameof(fields));
            Tables = DiagnosticIdentity.NormalizeDescriptors(
                tables,
                value => value.Id,
                nameof(tables));
            Samplers = DiagnosticIdentity.NormalizeDescriptors(
                samplers,
                value => value.Id,
                nameof(samplers));
            if (Samplers.Count == 0)
                throw new ArgumentException("Diagnostic schema requires samplers.", nameof(samplers));
            ValidateHandles();
        }

        public string CapabilityId { get; }
        public int CapabilityRevision { get; }
        public string ProgramId { get; }
        public string SamplerSetIdentity { get; }
        public string Identity { get; }
        public DiagnosticPacketLayout PacketLayout { get; }
        public IReadOnlyList<DiagnosticFieldHandle> Fields { get; }
        public IReadOnlyList<DiagnosticTableSchema> Tables { get; }
        public IReadOnlyList<DiagnosticSamplerLayout> Samplers { get; }

        public DiagnosticSamplerLayout RequireSampler(string samplerId)
        {
            samplerId = DiagnosticIdentity.RequireId(samplerId, nameof(samplerId));
            DiagnosticSamplerLayout sampler = Samplers.SingleOrDefault(value => string.Equals(
                value.Id,
                samplerId,
                StringComparison.Ordinal));
            return sampler ?? throw new ArgumentException(
                $"Diagnostic sampler '{samplerId}' is not in the schema.",
                nameof(samplerId));
        }

        public void Require(DiagnosticCapabilityBuildDescriptor capability)
        {
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));
            if (!string.Equals(CapabilityId, capability.CapabilityId, StringComparison.Ordinal) ||
                CapabilityRevision != capability.CapabilityRevision ||
                !string.Equals(ProgramId, capability.ProgramId, StringComparison.Ordinal) ||
                !string.Equals(
                    SamplerSetIdentity,
                    capability.SamplerSetIdentity,
                    StringComparison.Ordinal) ||
                !string.Equals(Identity, capability.SchemaIdentity, StringComparison.Ordinal) ||
                !string.Equals(
                    PacketLayout.Identity,
                    capability.PacketLayoutIdentity,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Diagnostic schema does not match the capability.");
            }
        }

        void ValidateHandles()
        {
            RequireDenseHandles(Fields, PacketLayout);
            foreach (DiagnosticFieldHandle field in Fields)
                RequireHandle(field, PacketLayout);
            if (Tables.Count != PacketLayout.Tables.Count)
                throw new ArgumentException("Diagnostic schema table count does not match packet layout.");
            for (int i = 0; i < Tables.Count; i++)
            {
                DiagnosticTableSchema table = Tables[i];
                DiagnosticTableLayout layout = PacketLayout.Tables[i];
                if (table.DenseIndex != i ||
                    !string.Equals(table.Id, layout.Id, StringComparison.Ordinal) ||
                    table.Capacity != layout.Capacity)
                {
                    throw new ArgumentException("Diagnostic table schema does not match packet layout.");
                }
                foreach (DiagnosticFieldHandle field in table.Fields)
                    RequireHandle(field, layout.RowLayout);
                RequireDenseHandles(table.Fields, layout.RowLayout);
            }
            foreach (DiagnosticSamplerLayout sampler in Samplers)
            {
                if (sampler.Fields.Any(field => !Fields.Contains(field)) ||
                    sampler.Tables.Any(table => !Tables.Contains(table)))
                {
                    throw new ArgumentException("Diagnostic sampler must reference the canonical schema handles.");
                }
            }
        }

        static void RequireHandle(
            DiagnosticFieldHandle field,
            DiagnosticPacketLayout layout)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            int count = Count(layout, field.ValueKind);
            if (field.DenseIndex >= count)
                throw new ArgumentException("Diagnostic field handle is outside the packet layout.");
            if (field.Availability.IsDeclared)
            {
                int availabilityCount = Count(layout, field.Availability.ValueKind);
                if (field.Availability.DenseIndex >= availabilityCount)
                    throw new ArgumentException("Diagnostic availability handle is outside the packet layout.");
            }
        }

        static void RequireDenseHandles(
            IReadOnlyList<DiagnosticFieldHandle> fields,
            DiagnosticPacketLayout layout)
        {
            foreach (DiagnosticValueKind kind in Enum.GetValues(typeof(DiagnosticValueKind)))
            {
                int[] indices = fields
                    .Where(value => value.ValueKind == kind)
                    .Select(value => value.DenseIndex)
                    .OrderBy(value => value)
                    .ToArray();
                if (indices.Length != Count(layout, kind))
                    throw new ArgumentException("Diagnostic field handles do not close the packet layout.");
                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] != i)
                        throw new ArgumentException("Diagnostic field handles are not dense.");
                }
            }
        }

        internal static int Count(DiagnosticPacketLayout layout, DiagnosticValueKind kind)
        {
            switch (kind)
            {
                case DiagnosticValueKind.Boolean: return layout.BooleanCount;
                case DiagnosticValueKind.Int32: return layout.Int32Count;
                case DiagnosticValueKind.UInt32: return layout.UInt32Count;
                case DiagnosticValueKind.Int64: return layout.Int64Count;
                case DiagnosticValueKind.UInt64: return layout.UInt64Count;
                case DiagnosticValueKind.Float32: return layout.Float32Count;
                case DiagnosticValueKind.Float64: return layout.Float64Count;
                case DiagnosticValueKind.Identity: return layout.IdentityCount;
                case DiagnosticValueKind.Vector2: return layout.Vector2Count;
                case DiagnosticValueKind.Vector3: return layout.Vector3Count;
                case DiagnosticValueKind.Vector4: return layout.Vector4Count;
                case DiagnosticValueKind.Quaternion: return layout.QuaternionCount;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
