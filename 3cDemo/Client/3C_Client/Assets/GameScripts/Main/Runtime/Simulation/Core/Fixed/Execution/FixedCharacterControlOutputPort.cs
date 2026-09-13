using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterControlOutputPort : ICharacterControlOutputPort
    {
        readonly CharacterControlModuleContract m_ControlModule;
        readonly FixedInputRuntime m_Input;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedActionRuntime m_Actions;
        readonly FixedTraceSink m_Trace;

        public FixedCharacterControlOutputPort(
            CharacterControlModuleContract controlModule,
            FixedInputRuntime input,
            FixedLocomotionRuntime locomotion,
            FixedActionRuntime actions,
            FixedTraceSink trace)
        {
            m_ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Locomotion = locomotion ?? throw new ArgumentNullException(nameof(locomotion));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public void SubmitMotion(CharacterControlMotionRequest request)
        {
            CharacterControlMotionDescriptor descriptor = RequireMotion(request.Binding);
            m_Locomotion.SubmitControl(m_Input, request, descriptor);
        }

        public bool SubmitAbility(CharacterControlAbilityRequest request) => m_Actions.ActivateFromControl(request);

        public void SubmitAbilityStop(CharacterControlAbilityStopRequest request) => m_Actions.StopFromControl(request);

        public void Trace(SimulationExecutionSource source, string code, string detail, ulong generation) =>
            m_Trace.Add(source, code, SimulationTraceSeverity.Information, detail, generation);

        CharacterControlMotionDescriptor RequireMotion(string binding)
        {
            for (int i = 0; i < m_ControlModule.Motions.Count; i++)
            {
                CharacterControlMotionDescriptor motion = m_ControlModule.Motions[i];
                if (string.Equals(motion.Binding, binding, StringComparison.Ordinal))
                    return motion;
            }
            throw new InvalidOperationException($"Control module '{m_ControlModule.ModuleId}' has no motion '{binding}'.");
        }
    }
}
