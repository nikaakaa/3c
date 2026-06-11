using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionInterruptContext
    {
        public ActionInterruptContext(
            ActionStateId currentState,
            float currentStateElapsedSeconds,
            int currentStateResistance = 0,
            int currentTick = 0)
        {
            CurrentState = currentState;
            CurrentStateElapsedSeconds = currentStateElapsedSeconds < 0f ? 0f : currentStateElapsedSeconds;
            CurrentStateResistance = currentStateResistance < 0 ? 0 : currentStateResistance;
            CurrentTick = currentTick < 0 ? 0 : currentTick;
        }

        public ActionStateId CurrentState { get; }
        public float CurrentStateElapsedSeconds { get; }
        public int CurrentStateResistance { get; }
        public int CurrentTick { get; }
    }
}
