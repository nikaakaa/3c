using System;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class DiagnosticCapabilityAttribute : Attribute
    {
        public DiagnosticCapabilityAttribute(
            string id,
            int revision,
            Type committedViewType)
        {
            Id = id;
            Revision = revision;
            CommittedViewType = committedViewType;
        }

        public string Id { get; }
        public int Revision { get; }
        public Type CommittedViewType { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class DiagnosticTableAttribute : Attribute
    {
        public DiagnosticTableAttribute(
            string capabilityId,
            string id,
            int revision,
            int capacity)
        {
            CapabilityId = capabilityId;
            Id = id;
            Revision = revision;
            Capacity = capacity;
        }

        public string CapabilityId { get; }
        public string Id { get; }
        public int Revision { get; }
        public int Capacity { get; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class DiagnosticTableCountAttribute : Attribute
    {
        public DiagnosticTableCountAttribute(string capabilityId, string tableId)
        {
            CapabilityId = capabilityId;
            TableId = tableId;
        }

        public string CapabilityId { get; }
        public string TableId { get; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class DiagnosticFieldAttribute : Attribute
    {
        public DiagnosticFieldAttribute(
            string capabilityId,
            string id,
            int revision,
            DiagnosticValueKind valueKind,
            string unit,
            string tableId,
            params string[] groups)
        {
            CapabilityId = capabilityId;
            Id = id;
            Revision = revision;
            ValueKind = valueKind;
            Unit = unit;
            TableId = tableId;
            Groups = groups ?? Array.Empty<string>();
        }

        public string CapabilityId { get; }
        public string Id { get; }
        public int Revision { get; }
        public DiagnosticValueKind ValueKind { get; }
        public string Unit { get; }
        public string TableId { get; }
        public string[] Groups { get; }
        public string AvailabilityFieldId { get; set; } = string.Empty;
        public long AvailabilityValue { get; set; } = 1;
        public bool Derived { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class DiagnosticSamplerAttribute : Attribute
    {
        public DiagnosticSamplerAttribute(
            string capabilityId,
            string id,
            int revision,
            string hostAdapterId,
            string outputSchema,
            params string[] groups)
        {
            CapabilityId = capabilityId;
            Id = id;
            Revision = revision;
            HostAdapterId = hostAdapterId;
            OutputSchema = outputSchema;
            Groups = groups ?? Array.Empty<string>();
        }

        public string CapabilityId { get; }
        public string Id { get; }
        public int Revision { get; }
        public string HostAdapterId { get; }
        public string OutputSchema { get; }
        public string[] Groups { get; }
        public string[] Fields { get; set; } = Array.Empty<string>();
        public string[] Tables { get; set; } = Array.Empty<string>();
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class DiagnosticCaptureProgramAttribute : Attribute
    {
        public DiagnosticCaptureProgramAttribute(
            string id,
            string capabilityId,
            params Type[] samplerTypes)
        {
            Id = id;
            CapabilityId = capabilityId;
            SamplerTypes = samplerTypes ?? Array.Empty<Type>();
        }

        public string Id { get; }
        public string CapabilityId { get; }
        public Type[] SamplerTypes { get; }
    }
}
