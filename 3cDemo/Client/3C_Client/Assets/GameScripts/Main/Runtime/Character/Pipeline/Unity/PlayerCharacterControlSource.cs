using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [DisallowMultipleComponent]
    public sealed class PlayerCharacterControlSource : CharacterControlSource
    {
        [SerializeField] string m_ActionTargetInputValueId;
        [SerializeField] CharacterActionTargetInputProvider m_ActionTargetProvider;

        public string ActionTargetInputValueId => string.IsNullOrWhiteSpace(m_ActionTargetInputValueId)
            ? string.Empty
            : m_ActionTargetInputValueId.Trim();
        public CharacterActionTargetInputProvider ActionTargetProvider => m_ActionTargetProvider;
        public override string SourceIdentity => $"unity-player/{ActionTargetInputValueId}/{(m_ActionTargetProvider ? m_ActionTargetProvider.ProviderIdentity : "none")}";

        public override IUnityCharacterControlSourceRuntime Create(CharacterControlSourceContext context)
        {
            if (!context.Owner.CameraRig)
                throw new InvalidOperationException("Player control source requires an explicit camera rig.");
            if (string.IsNullOrEmpty(ActionTargetInputValueId))
                throw new InvalidOperationException("Player control source requires an explicit Action Target input value id.");
            return new UnityCharacterSimulationInputAdapter(
                context.Definition.InputProfile,
                context.Program,
                context.Owner.CameraRig,
                context.Owner,
                ActionTargetInputValueId,
                m_ActionTargetProvider);
        }
    }
}
