namespace ThirdPersonDiagnostics
{
    public interface ICharacterDiagnosticSink
    {
        void Submit(in RuntimeDiagnosticLogEvent diagnosticEvent);
    }
}
