namespace ThirdPersonDiagnostics
{
    public sealed class RuntimeDiagnosticLogCharacterSink : ICharacterDiagnosticSink
    {
        public static readonly RuntimeDiagnosticLogCharacterSink Instance = new RuntimeDiagnosticLogCharacterSink();

        RuntimeDiagnosticLogCharacterSink()
        {
        }

        public void Submit(in RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            RuntimeDiagnosticLog.Submit(diagnosticEvent);
        }
    }
}
