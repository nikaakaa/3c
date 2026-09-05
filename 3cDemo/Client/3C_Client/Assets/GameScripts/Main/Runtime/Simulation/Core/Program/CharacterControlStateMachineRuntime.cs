using System;

namespace ThirdPersonSimulation
{
    public sealed class CharacterControlStateMachineRuntime
    {
        readonly ICharacterControlModule m_Module;
        readonly CharacterControlStateFieldDescriptor m_ActiveStateField;
        readonly CharacterControlStateFieldDescriptor m_EnteredTickField;
        readonly CharacterControlStateFieldDescriptor m_TransitionField;
        readonly CharacterControlStateFieldDescriptor m_TransitionProgressField;

        public CharacterControlStateMachineRuntime(ICharacterControlModule module)
        {
            m_Module = module ?? throw new ArgumentNullException(nameof(module));
            if (module.Contract == null)
                throw new ArgumentException("Character control module has no contract.", nameof(module));
            m_ActiveStateField = module.Contract.RequireStateField(ProgramStateSemantic.ControlActiveState);
            m_EnteredTickField = module.Contract.RequireStateField(ProgramStateSemantic.ControlEnteredTick);
            m_TransitionField = module.Contract.Transitions.Count == 0
                ? null
                : module.Contract.RequireStateField(ProgramStateSemantic.ControlTransition);
            m_TransitionProgressField = module.Contract.RequireStateField(ProgramStateSemantic.ControlTransitionProgress);
        }

        public CharacterControlModuleContract Contract => m_Module.Contract;

        public void Tick(
            in CharacterControlTickContext context,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output)
        {
            if (read == null)
                throw new ArgumentNullException(nameof(read));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            CharacterControlStateId current = state.ReadState(m_ActiveStateField.Id);
            if (!current.IsValid)
            {
                current = Contract.InitialState;
                state.WriteState(m_ActiveStateField.Id, current);
                state.WriteUInt64(m_EnteredTickField.Id, context.Tick.Value);
                state.WriteInt32(m_TransitionProgressField.Id, 0);
                m_Module.Enter(in context, current, read, state, output);
                state.WriteInt32(m_TransitionProgressField.Id, 1);
                Trace(
                    output,
                    current,
                    default,
                    "control_state_entered",
                    $"state={current.Value}:tick={context.Tick.Value}");
            }
            RequireState(current);
            if (state.ReadInt32(m_TransitionProgressField.Id) == 0)
            {
                m_Module.Enter(in context, current, read, state, output);
                state.WriteInt32(m_TransitionProgressField.Id, 1);
                Trace(
                    output,
                    current,
                    default,
                    "control_state_entered",
                    $"state={current.Value}:tick={context.Tick.Value}");
            }
            m_Module.Tick(in context, current, read, state, output);

            CharacterControlTransitionDescriptor selected = null;
            for (int i = 0; i < Contract.Transitions.Count; i++)
            {
                CharacterControlTransitionDescriptor candidate = Contract.Transitions[i];
                if (candidate.Source != current)
                    continue;
                bool evaluated = m_Module.EvaluateTransition(in context, candidate.Id, read, state);
                Trace(
                    output,
                    current,
                    candidate.Id,
                    "control_transition_evaluated",
                    $"source={candidate.Source.Value}:target={candidate.Target.Value}:result={evaluated}");
                if (!evaluated)
                    continue;
                if (selected == null ||
                    candidate.Priority < selected.Priority ||
                    candidate.Priority == selected.Priority &&
                    (candidate.EvaluationOrder < selected.EvaluationOrder ||
                     candidate.EvaluationOrder == selected.EvaluationOrder && candidate.Id.CompareTo(selected.Id) < 0))
                    selected = candidate;
            }
            if (selected == null)
                return;

            m_Module.Exit(in context, current, read, state, output);
            Trace(
                output,
                current,
                default,
                "control_state_exited",
                $"state={current.Value}:tick={context.Tick.Value}");
            if (m_TransitionField != null)
                state.WriteTransition(m_TransitionField.Id, selected.Id);
            state.WriteState(m_ActiveStateField.Id, selected.Target);
            state.WriteUInt64(m_EnteredTickField.Id, context.Tick.Value);
            state.WriteInt32(m_TransitionProgressField.Id, 0);
            Trace(
                output,
                current,
                selected.Id,
                "control_transition_selected",
                $"source={current.Value}:target={selected.Target.Value}:tick={context.Tick.Value}");
        }

        void Trace(
            ICharacterControlOutputPort output,
            CharacterControlStateId stateId,
            CharacterControlTransitionId transitionId,
            string code,
            string detail)
        {
            output.Trace(
                SimulationExecutionSource.FromCharacterControl(
                    Contract.ModuleId,
                    stateId,
                    transitionId),
                code,
                detail);
        }

        void RequireState(CharacterControlStateId stateId)
        {
            for (int i = 0; i < Contract.States.Count; i++)
            {
                if (Contract.States[i].Id == stateId)
                    return;
            }
            throw new InvalidOperationException($"Character control state '{stateId}' is not declared by module '{Contract.ModuleId}'.");
        }
    }
}
