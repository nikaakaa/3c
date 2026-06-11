using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionRuntimeStateSnapshot
    {
        public ActionRuntimeStateSnapshot(
            ActionStateId currentState,
            float elapsedSeconds,
            int currentResistance = 0,
            int currentTick = 0)
        {
            CurrentState = currentState;
            ElapsedSeconds = elapsedSeconds < 0f ? 0f : elapsedSeconds;
            CurrentResistance = currentResistance < 0 ? 0 : currentResistance;
            CurrentTick = currentTick < 0 ? 0 : currentTick;
        }

        public ActionStateId CurrentState { get; }
        public float ElapsedSeconds { get; }
        public int CurrentResistance { get; }
        public int CurrentTick { get; }
    }
}
