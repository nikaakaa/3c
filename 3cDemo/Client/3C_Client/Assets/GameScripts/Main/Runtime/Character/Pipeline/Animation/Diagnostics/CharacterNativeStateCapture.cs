using KK.GeneratedDiagnosticSampling;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal interface ICharacterNativeStateCaptureSource
    {
        int StateCaptureCount { get; }
        CharacterNativeStateCaptureRow ReadStateCapture(int index);
    }

    public readonly struct CharacterNativeStateCapturePage
    {
        readonly ICharacterNativeStateCaptureSource m_Source;
        internal CharacterNativeStateCapturePage(ICharacterNativeStateCaptureSource source) { m_Source = source; }
        public int Count => m_Source.StateCaptureCount;
        public CharacterNativeStateCaptureRow this[int index] => m_Source.ReadStateCapture(index);
    }

    public readonly struct CharacterNativeStateCaptureRow
    {
        internal CharacterNativeStateCaptureRow(string nodeId, string stateId, string targetId,
            string transitionId, float time, float elapsed, float duration)
        {
            StateMachineId = nodeId;
            ActiveStateId = stateId;
            TargetStateId = targetId;
            ActiveTransitionId = transitionId;
            TimeInState = time;
            TransitionElapsed = elapsed;
            TransitionDuration = duration;
        }
        [DiagnosticField, DiagnosticKey("state-machine-id")] public string StateMachineId { get; }
        [DiagnosticField, DiagnosticKey("active-state-id")] public string ActiveStateId { get; }
        [DiagnosticField, DiagnosticKey("target-state-id")] public string TargetStateId { get; }
        [DiagnosticField, DiagnosticKey("active-transition-id")] public string ActiveTransitionId { get; }
        [DiagnosticField, DiagnosticKey("time-in-state")] public float TimeInState { get; }
        [DiagnosticField, DiagnosticKey("transition-elapsed")] public float TransitionElapsed { get; }
        [DiagnosticField, DiagnosticKey("transition-duration")] public float TransitionDuration { get; }
        [DiagnosticField, DiagnosticKey("transition-progress")]
        public float TransitionProgress => TransitionDuration > 0f ? UnityEngine.Mathf.Clamp01(TransitionElapsed / TransitionDuration) : 0f;
    }
}
