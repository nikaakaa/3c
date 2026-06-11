namespace ThirdPersonAction
{
    public sealed class ActionRuntimeStateTracker
    {
        public static readonly ActionStateId NoneState = ActionStateIds.None;

        ActionRuntimeStateSnapshot snapshot;

        public ActionRuntimeStateTracker()
        {
            Reset();
        }

        public ActionStateId CurrentState => snapshot.CurrentState;
        public float ElapsedSeconds => snapshot.ElapsedSeconds;
        public int CurrentResistance => snapshot.CurrentResistance;
        public int CurrentTick => snapshot.CurrentTick;
        public ActionRuntimeStateSnapshot Snapshot => snapshot;

        public void Reset()
        {
            snapshot = new ActionRuntimeStateSnapshot(NoneState, 0f, 0, 0);
        }

        public void EnterState(ActionStateId state, int resistance = 0)
        {
            ActionStateId nextState = state.IsValid ? state : NoneState;
            snapshot = new ActionRuntimeStateSnapshot(nextState, 0f, resistance, snapshot.CurrentTick);
        }

        public void Tick(float deltaSeconds, int currentTick)
        {
            float safeDelta = deltaSeconds < 0f ? 0f : deltaSeconds;
            snapshot = new ActionRuntimeStateSnapshot(
                snapshot.CurrentState,
                snapshot.ElapsedSeconds + safeDelta,
                snapshot.CurrentResistance,
                currentTick);
        }

        public ActionInterruptContext CreateInterruptContext()
        {
            return new ActionInterruptContext(
                snapshot.CurrentState,
                snapshot.ElapsedSeconds,
                snapshot.CurrentResistance,
                snapshot.CurrentTick);
        }

        public void ApplyDecision(ActionInterruptDecision decision, int targetResistance = 0)
        {
            if (!decision.Accepted)
                return;

            EnterState(decision.TargetState, targetResistance);
        }
    }
}
