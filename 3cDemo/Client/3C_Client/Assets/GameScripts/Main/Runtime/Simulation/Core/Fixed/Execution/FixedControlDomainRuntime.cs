using System;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedControlDomainRuntime
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly ICharacterControlModule m_Control;
        readonly FixedActionStateStore m_ActionStore;
        readonly FixedCharacterControlReadPort m_Read;
        readonly ICharacterControlStatePort m_State;
        readonly FixedCharacterControlOutputPort m_Output;

        public FixedControlDomainRuntime(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            FixedEvaluationFrame frame,
            FixedInputRuntime input,
            FixedActionRuntime actions,
            FixedActionStateStore actionStore,
            FixedBlackboardRuntime blackboard,
            FixedEquipmentRuntime equipment,
            FixedLocomotionRuntime locomotion,
            FixedStatePort state,
            CharacterControlModuleCatalog controlModules)
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
            if (!program.ControlModuleBinding.IsValid)
                return;
            m_Control = controlModules.Require(program.ControlModuleBinding);
            ProgramCatalogEntry controlCatalog =
                program.CatalogEntries[program.ControlModuleBinding.CatalogEntryIndex];
            CharacterControlStateLayout controlLayout = layout.CreateControlStateLayout(m_Control.Contract);
            m_Read = new FixedCharacterControlReadPort(
                input,
                m_Frame,
                parameter => ReadControlParameter(program, controlCatalog, parameter),
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
            m_State = new FixedCharacterControlStatePort(state, controlLayout);
            m_Output = new FixedCharacterControlOutputPort(
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

        static FixedScalar ReadControlParameter(
            CharacterSimulationProgram program,
            ProgramCatalogEntry controlCatalog,
            CharacterControlParameterId parameter)
        {
            string name = $"Parameter:{parameter.Value}:NumericValue";
            for (int i = 0; i < controlCatalog.Fields.Count; i++)
            {
                ProgramCatalogField field = controlCatalog.Fields[i];
                if (!string.Equals(field.Name, name, StringComparison.Ordinal))
                    continue;
                if (field.Kind != ProgramCatalogFieldKind.Constant || field.ConstantIndex < 0 ||
                    field.ConstantIndex >= program.Constants.Count)
                    throw new InvalidOperationException($"Control module field '{name}' is not a valid constant.");
                ProgramConstant value = program.Constants[field.ConstantIndex];
                if (value.Kind != ProgramConstantKind.Scalar)
                    throw new InvalidOperationException($"Control module field '{name}' is not Scalar.");
                return value.Scalar;
            }
            throw new InvalidOperationException($"Control module '{controlCatalog.Identity}' has no '{name}' field.");
        }
    }
}
