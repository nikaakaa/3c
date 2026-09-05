using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterControlOutputPort : ICharacterControlOutputPort
    {
        readonly CharacterControlModuleContract m_Contract;
        readonly Float32InputRuntime m_Input;
        readonly Float32LocomotionRuntime m_Locomotion;
        readonly Float32ActionRuntime m_Actions;

        public Float32CharacterControlOutputPort(
            CharacterControlModuleContract contract,
            Float32InputRuntime input,
            Float32LocomotionRuntime locomotion,
            Float32ActionRuntime actions)
        {
            m_Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Locomotion = locomotion ?? throw new ArgumentNullException(nameof(locomotion));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        public void SubmitMotion(CharacterControlMotionRequest request)
        {
            CharacterControlMotionDescriptor descriptor = RequireMotion(request.Binding);
            m_Locomotion.SubmitControl(m_Input, request, descriptor);
        }

        public bool SubmitSkill(CharacterControlSkillRequest request) => m_Actions.ActivateFromControl(request);

        public void SubmitSkillStop(CharacterControlSkillStopRequest request) => m_Actions.StopFromControl(request);

        CharacterControlMotionDescriptor RequireMotion(string binding)
        {
            for (int i = 0; i < m_Contract.Motions.Count; i++)
            {
                if (string.Equals(m_Contract.Motions[i].Binding, binding, StringComparison.Ordinal))
                    return m_Contract.Motions[i];
            }
            throw new InvalidOperationException($"Character control motion '{binding}' is absent from module '{m_Contract.ModuleId}'.");
        }
    }
}
