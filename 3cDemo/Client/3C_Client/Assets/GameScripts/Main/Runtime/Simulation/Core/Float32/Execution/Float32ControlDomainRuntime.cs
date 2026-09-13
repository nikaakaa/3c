using System;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonSimulation
{
    internal sealed class Float32ControlDomainRuntime
    {
        readonly Float32EvaluationFrame m_Frame;
        readonly ICharacterControlModule m_Control;
        readonly Float32ActionStateStore m_ActionStore;
        readonly Float32CharacterControlReadPort m_Read;
        readonly ICharacterControlStatePort m_State;
        readonly Float32CharacterControlOutputPort m_Output;

        public Float32ControlDomainRuntime(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            Float32EvaluationFrame frame,
            Float32InputRuntime input,
            Float32ActionRuntime actions,
            Float32ActionStateStore actionStore,
            Float32BlackboardRuntime blackboard,
            Float32EquipmentRuntime equipment,
            Float32LocomotionRuntime locomotion,
            Float32StatePort state,
            CharacterControlModuleCatalog controlModules,
            CharacterControlRuntimeBinding controlRuntimeBinding)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            input = input ?? throw new ArgumentNullException(nameof(input));
            actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_ActionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            locomotion = locomotion ?? throw new ArgumentNullException(nameof(locomotion));
            state = state ?? throw new ArgumentNullException(nameof(state));
            controlModules = controlModules ?? throw new ArgumentNullException(nameof(controlModules));
            if (!program.Manifest.Root.IsCharacter)
            {
                if (controlRuntimeBinding != null)
                    throw new ArgumentException("Non-Character Program cannot carry a Control runtime binding.", nameof(controlRuntimeBinding));
                return;
            }
            if (controlRuntimeBinding == null)
                throw new ArgumentNullException(nameof(controlRuntimeBinding));
            m_Control = controlModules.Require(controlRuntimeBinding.ModuleId);
            if (m_Control.Contract.SemanticVersion != controlRuntimeBinding.SemanticVersion)
                throw new InvalidOperationException($"Character control runtime binding '{controlRuntimeBinding.ModuleId}/{controlRuntimeBinding.SemanticVersion}' does not match installed module version '{m_Control.Contract.SemanticVersion}'.");
            controlRuntimeBinding.RequireContract(m_Control.Contract);
            CharacterControlStateLayout controlLayout = layout.CreateControlStateLayout(m_Control.Contract);
            m_Read = new Float32CharacterControlReadPort(
                input,
                m_Frame,
                parameter => ReadControlParameter(controlRuntimeBinding.Parameters, parameter),
                skill => actions.IsAbilityActive(skill),
                skill => (m_ActionStore.TryGetActiveAbilityInstanceId(skill, out ulong instanceId), instanceId),
                skill => actions.IsAbilityCompleted(skill),
                skill => actions.CompletedAbilityInstanceId(skill),
                (skill, window) => blackboard.IsActionWindowActive(skill, window),
                route =>
                {
                    bool found = equipment.TryReadActionContext(route, out EquipmentActionContext context);
                    return (found, context);
                });
            m_State = new Float32CharacterControlStatePort(state, controlLayout);
            m_Output = new Float32CharacterControlOutputPort(
                m_Control.Contract,
                input,
                locomotion,
                actions,
                m_Frame.Trace);
        }

        public bool IsInstalled => m_Control != null;

        [PerformanceProbe("simulation.operation.character-control-tick")]
        public void Tick()
        {
            if (m_Control == null)
                return;
            var context = new CharacterControlTickContext(
                m_Frame.ActorId,
                m_Frame.Tick,
                m_Frame.Program.Manifest.TickRate);
            m_Control.Tick(in context, m_Read, m_State, m_Output);
        }

        static Float32Scalar ReadControlParameter(
            CharacterControlParameterSet parameters,
            CharacterControlParameterId parameter)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            return Float32Scalar.FromDouble(parameters.ReadNumeric(parameter));
        }
    }
}
