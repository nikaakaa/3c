using BTSMTL;

namespace TreeDesigner
{
    public interface IStateMachineRuntimeFacts
    {
        string ActiveStateGuid { get; }
        string ActiveStateName { get; }
        int StateElapsedTicks { get; }
        float StateElapsedSeconds { get; }
        State StateRootLastStatus { get; }
        bool StateRootCompleted { get; }
        string ExitingStateGuid { get; }
        string PendingTargetStateGuid { get; }
    }

    public sealed class StateMachineRuntimeFacts : IStateMachineRuntimeFacts
    {
        public string ActiveStateGuid { get; private set; }
        public string ActiveStateName { get; private set; }
        public int StateElapsedTicks { get; private set; }
        public float StateElapsedSeconds { get; private set; }
        public State StateRootLastStatus { get; private set; }
        public bool StateRootCompleted { get; private set; }
        public string ExitingStateGuid { get; private set; }
        public string PendingTargetStateGuid { get; private set; }

        internal void Enter(StateNode state)
        {
            ActiveStateGuid = state?.GUID ?? string.Empty;
            ActiveStateName = ResolveStateName(state);
            StateElapsedTicks = 0;
            StateElapsedSeconds = 0f;
            StateRootLastStatus = State.None;
            StateRootCompleted = false;
        }

        internal void Advance(float deltaTime)
        {
            StateElapsedTicks++;
            StateElapsedSeconds += deltaTime;
        }

        internal void SetRootStatus(State status)
        {
            StateRootLastStatus = status;
            StateRootCompleted = status == State.Success;
        }

        internal void BeginPending(StateNode exitingState, StateNode targetState)
        {
            ExitingStateGuid = exitingState?.GUID ?? string.Empty;
            PendingTargetStateGuid = targetState?.GUID ?? string.Empty;
        }

        internal void ClearPending()
        {
            ExitingStateGuid = string.Empty;
            PendingTargetStateGuid = string.Empty;
        }

        internal void Clear()
        {
            ActiveStateGuid = string.Empty;
            ActiveStateName = string.Empty;
            StateElapsedTicks = 0;
            StateElapsedSeconds = 0f;
            StateRootLastStatus = State.None;
            StateRootCompleted = false;
            ClearPending();
        }

        static string ResolveStateName(StateNode state)
        {
            if (state == null)
                return string.Empty;

#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(state.ResolvedDisplayName))
                return state.ResolvedDisplayName;
#endif

            NodeNameAttribute nodeNameAttribute = state.GetAttribute<NodeNameAttribute>();
            return nodeNameAttribute != null ? nodeNameAttribute.Name : state.GetType().Name;
        }
    }
}
