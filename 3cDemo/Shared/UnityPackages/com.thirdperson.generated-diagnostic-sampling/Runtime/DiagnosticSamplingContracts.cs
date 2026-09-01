using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public enum DiagnosticValueKind
    {
        Boolean = 1,
        Int32 = 2,
        UInt32 = 3,
        Int64 = 4,
        UInt64 = 5,
        Float32 = 6,
        Float64 = 7,
        Identity = 8,
        Vector2 = 9,
        Vector3 = 10,
        Vector4 = 11,
        Quaternion = 12
    }

    public enum DiagnosticCapabilityMode
    {
        Disabled = 0,
        Capture = 1
    }

    public enum DiagnosticCaptureStatus
    {
        Prepared = 0,
        Capturing = 1,
        Finalizing = 2,
        Completed = 3,
        Faulted = 4,
        Cancelled = 5
    }

    public enum DiagnosticCaptureFailureStage
    {
        None = 0,
        Compile = 1,
        Preflight = 2,
        Capture = 3,
        Queue = 4,
        Writer = 5,
        Reader = 6,
        Host = 7,
        Publish = 8
    }

    public readonly struct DiagnosticCaptureFailure
    {
        public DiagnosticCaptureFailure(
            DiagnosticCaptureFailureStage stage,
            string capabilityId,
            string programId,
            string samplerId,
            string memberId,
            string message)
        {
            if (stage == DiagnosticCaptureFailureStage.None)
                throw new ArgumentOutOfRangeException(nameof(stage));
            Stage = stage;
            CapabilityId = capabilityId ?? string.Empty;
            ProgramId = programId ?? string.Empty;
            SamplerId = samplerId ?? string.Empty;
            MemberId = memberId ?? string.Empty;
            Message = DiagnosticIdentity.RequireText(message, nameof(message));
        }

        public DiagnosticCaptureFailureStage Stage { get; }
        public string CapabilityId { get; }
        public string ProgramId { get; }
        public string SamplerId { get; }
        public string MemberId { get; }
        public string Message { get; }
    }

    public sealed class DiagnosticFieldDescriptor
    {
        public DiagnosticFieldDescriptor(
            string capabilityId,
            string id,
            int revision,
            DiagnosticValueKind valueKind,
            string unit,
            string tableId,
            IEnumerable<string> groups,
            string availabilityFieldId,
            long availabilityValue,
            bool derived)
        {
            CapabilityId = DiagnosticIdentity.RequireId(capabilityId, nameof(capabilityId));
            Id = DiagnosticIdentity.RequireId(id, nameof(id));
            Revision = DiagnosticIdentity.RequireRevision(revision, nameof(revision));
            if (!Enum.IsDefined(typeof(DiagnosticValueKind), valueKind))
                throw new ArgumentOutOfRangeException(nameof(valueKind));
            ValueKind = valueKind;
            Unit = DiagnosticIdentity.RequireText(unit, nameof(unit));
            TableId = DiagnosticIdentity.RequireId(tableId, nameof(tableId));
            Groups = DiagnosticIdentity.NormalizeIds(groups, nameof(groups));
            AvailabilityFieldId = string.IsNullOrWhiteSpace(availabilityFieldId)
                ? string.Empty
                : DiagnosticIdentity.RequireId(availabilityFieldId, nameof(availabilityFieldId));
            AvailabilityValue = availabilityValue;
            Derived = derived;
        }

        public string CapabilityId { get; }
        public string Id { get; }
        public int Revision { get; }
        public DiagnosticValueKind ValueKind { get; }
        public string Unit { get; }
        public string TableId { get; }
        public IReadOnlyList<string> Groups { get; }
        public string AvailabilityFieldId { get; }
        public long AvailabilityValue { get; }
        public bool Derived { get; }
    }

    public sealed class DiagnosticTableDescriptor
    {
        public DiagnosticTableDescriptor(
            string capabilityId,
            string id,
            int revision,
            int capacity)
        {
            CapabilityId = DiagnosticIdentity.RequireId(capabilityId, nameof(capabilityId));
            Id = DiagnosticIdentity.RequireId(id, nameof(id));
            Revision = DiagnosticIdentity.RequireRevision(revision, nameof(revision));
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        public string CapabilityId { get; }
        public string Id { get; }
        public int Revision { get; }
        public int Capacity { get; }
    }

    public sealed class DiagnosticSamplerDescriptor
    {
        public DiagnosticSamplerDescriptor(
            string capabilityId,
            string id,
            int revision,
            string hostAdapterId,
            string outputSchema,
            IEnumerable<string> groups,
            IEnumerable<string> fields,
            IEnumerable<string> tables)
        {
            CapabilityId = DiagnosticIdentity.RequireId(capabilityId, nameof(capabilityId));
            Id = DiagnosticIdentity.RequireId(id, nameof(id));
            Revision = DiagnosticIdentity.RequireRevision(revision, nameof(revision));
            HostAdapterId = DiagnosticIdentity.RequireId(hostAdapterId, nameof(hostAdapterId));
            OutputSchema = DiagnosticIdentity.RequireId(outputSchema, nameof(outputSchema));
            Groups = DiagnosticIdentity.NormalizeIds(groups, nameof(groups));
            Fields = DiagnosticIdentity.NormalizeIds(fields, nameof(fields));
            Tables = DiagnosticIdentity.NormalizeIds(tables, nameof(tables));
            if (Groups.Count == 0 && Fields.Count == 0 && Tables.Count == 0)
                throw new ArgumentException("Diagnostic sampler must select at least one field or table.");
        }

        public string CapabilityId { get; }
        public string Id { get; }
        public int Revision { get; }
        public string HostAdapterId { get; }
        public string OutputSchema { get; }
        public IReadOnlyList<string> Groups { get; }
        public IReadOnlyList<string> Fields { get; }
        public IReadOnlyList<string> Tables { get; }
    }

    public sealed class DiagnosticProgramDescriptor
    {
        public DiagnosticProgramDescriptor(
            string capabilityId,
            int capabilityRevision,
            string programId,
            IEnumerable<DiagnosticSamplerDescriptor> samplers)
        {
            CapabilityId = DiagnosticIdentity.RequireId(capabilityId, nameof(capabilityId));
            CapabilityRevision = DiagnosticIdentity.RequireRevision(
                capabilityRevision,
                nameof(capabilityRevision));
            ProgramId = DiagnosticIdentity.RequireId(programId, nameof(programId));
            Samplers = DiagnosticIdentity.NormalizeDescriptors(
                samplers,
                value => value.Id,
                nameof(samplers));
            if (Samplers.Count == 0)
                throw new ArgumentException("Diagnostic program must include at least one sampler.");
            if (Samplers.Any(value => !string.Equals(
                    value.CapabilityId,
                    CapabilityId,
                    StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    "Diagnostic program samplers must belong to the same capability.",
                    nameof(samplers));
            }
        }

        public string CapabilityId { get; }
        public int CapabilityRevision { get; }
        public string ProgramId { get; }
        public IReadOnlyList<DiagnosticSamplerDescriptor> Samplers { get; }
    }
}
