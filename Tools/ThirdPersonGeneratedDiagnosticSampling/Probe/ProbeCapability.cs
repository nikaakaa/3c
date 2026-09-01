using ThirdPerson.GeneratedDiagnosticSampling;

namespace ThirdPerson.GeneratedDiagnosticSampling.Probe
{
    internal readonly struct ProbeCommittedView
    {
        internal ProbeCommittedView(int value)
        {
            Value = value;
        }

        internal int Value { get; }
    }

    [DiagnosticCapability("probe-capability", 1, typeof(ProbeCommittedView))]
    internal static class ProbeCapabilityDefinition
    {
    }

    internal static class ProbeFields
    {
        [DiagnosticField(
            "probe-capability",
            "probe.available",
            1,
            DiagnosticValueKind.Boolean,
            "flag",
            "main",
            "probe.core")]
        internal static bool Available(in ProbeCommittedView view) => true;

        [DiagnosticField(
            "probe-capability",
            "probe.value",
            1,
            DiagnosticValueKind.Int32,
            "count",
            "main",
            "probe.core",
            AvailabilityFieldId = "probe.available",
            Dependencies = new[] { "probe.available" })]
        internal static int Value(in ProbeCommittedView view) => view.Value;
    }

    [DiagnosticTable("probe-capability", "probe.table", 1, 2)]
    internal static class ProbeTable
    {
        [DiagnosticTableCount("probe-capability", "probe.table")]
        internal static int Count(in ProbeCommittedView view) => 1;

        [DiagnosticField(
            "probe-capability",
            "probe.table.value",
            1,
            DiagnosticValueKind.Int32,
            "count",
            "probe.table",
            "probe.table")]
        internal static int Value(in ProbeCommittedView view, int row) => view.Value + row;
    }

    [DiagnosticSampler(
        "probe-capability",
        "probe-sampler",
        1,
        "probe-host",
        "probe-output/1",
        "probe.core",
        Tables = new[] { "probe.table" })]
    internal static class ProbeSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        "probe-program",
        "probe-capability",
        typeof(ProbeSamplerDefinition))]
    internal static partial class ProbeProgramDefinition
    {
    }

    internal static class ProbeProgramConsumer
    {
        internal static int Capture(int value)
        {
            DiagnosticSchemaLayout schema =
                ProbeProgramDefinition.CreateDiagnosticSchemaLayout();
            DiagnosticCapabilityBuildDescriptor capability =
                ProbeProgramDefinition.CreateDiagnosticCapabilityBuildDescriptor(
                    "probe-lineage/1",
                    4,
                    "probe-binary/1");
            schema.Require(capability);
            DiagnosticPacketLayout layout = schema.PacketLayout;
            var packet = new DiagnosticCapturePacket(layout);
            var lineage = new DiagnosticLineageKey("probe-lineage/1", 0, 1);
            var sampleKey = new DiagnosticSampleKey(1, lineage);
            packet.Begin(sampleKey);
            var view = new ProbeCommittedView(value);
            ProbeProgramDefinition.Capture(in view, ref packet);
            return packet.Int32Values[0];
        }
    }
}
