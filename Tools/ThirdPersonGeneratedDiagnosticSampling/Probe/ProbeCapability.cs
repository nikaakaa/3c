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
            "probe.value",
            1,
            DiagnosticValueKind.Int32,
            "count",
            "main",
            "probe.core")]
        internal static int Value(in ProbeCommittedView view) => view.Value;
    }

    [DiagnosticSampler(
        "probe-capability",
        "probe-sampler",
        1,
        "probe-host",
        "probe-output/1",
        "probe.core")]
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
            DiagnosticPacketLayout layout =
                ProbeProgramDefinition.CreateDiagnosticPacketLayout();
            var packet = new DiagnosticCapturePacket(layout);
            var lineage = new DiagnosticLineageKey("probe-lineage/1", 0, 1);
            var sampleKey = new DiagnosticSampleKey(1, lineage);
            packet.Begin(sampleKey);
            var view = new ProbeCommittedView(value);
            ProbeProgramDefinition.Capture(in view, packet);
            return packet.Int32Values[0];
        }
    }
}
