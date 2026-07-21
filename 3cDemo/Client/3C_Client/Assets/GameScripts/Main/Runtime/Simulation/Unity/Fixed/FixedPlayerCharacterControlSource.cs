using System;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Input;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [DisallowMultipleComponent]
    public sealed class FixedPlayerCharacterControlSource : FixedCharacterControlSource
    {
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] string m_ActionTargetInputValueId;
        [SerializeField] CharacterActionTargetInputProvider m_ActionTargetProvider;

        public string ActionTargetInputValueId => string.IsNullOrWhiteSpace(m_ActionTargetInputValueId)
            ? string.Empty
            : m_ActionTargetInputValueId.Trim();
        public CharacterActionTargetInputProvider ActionTargetProvider => m_ActionTargetProvider;
        public override string SourceIdentity => m_InputProfile
            ? $"unity-fixed-player/{m_InputProfile.name}/{ActionTargetInputValueId}/{(m_ActionTargetProvider ? m_ActionTargetProvider.ProviderIdentity : "none")}"
            : "unity-fixed-player/unconfigured";

        public override IUnityFixedCharacterControlSourceRuntime Create(FixedCharacterControlSourceContext context)
        {
            CharacterInputProfile inputProfile = m_InputProfile ? m_InputProfile :
                throw new InvalidOperationException($"Fixed Player Control Source '{name}' requires a Character Input Profile.");
            ThirdPersonCameraController cameraRig = context.Owner.CameraRig ? context.Owner.CameraRig :
                throw new InvalidOperationException($"Fixed Player Control Source '{name}' requires the Fixed Character Host Camera Rig.");
            return new UnityFixedCharacterInputAdapter(
                inputProfile,
                context.Program,
                cameraRig,
                context.Owner,
                ActionTargetInputValueId,
                m_ActionTargetProvider);
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            CharacterInputProfile inputProfile,
            string actionTargetInputValueId,
            CharacterActionTargetInputProvider actionTargetProvider)
        {
            m_InputProfile = inputProfile ? inputProfile : throw new ArgumentNullException(nameof(inputProfile));
            m_ActionTargetInputValueId = string.IsNullOrWhiteSpace(actionTargetInputValueId)
                ? string.Empty
                : actionTargetInputValueId.Trim();
            m_ActionTargetProvider = actionTargetProvider;
        }
#endif
    }
}
