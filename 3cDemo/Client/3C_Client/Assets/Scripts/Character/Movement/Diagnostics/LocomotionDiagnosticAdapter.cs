using System;
using ThirdPersonDiagnostics;

namespace ThirdPersonMovement
{
    public sealed class LocomotionDiagnosticAdapter
    {
        readonly ICharacterDiagnosticSink sink;

        public LocomotionDiagnosticAdapter(ICharacterDiagnosticSink sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void Submit(in RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            sink.Submit(in diagnosticEvent);
        }
    }
}
