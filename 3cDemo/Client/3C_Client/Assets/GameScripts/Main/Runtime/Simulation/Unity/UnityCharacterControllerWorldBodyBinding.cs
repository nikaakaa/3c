using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [DisallowMultipleComponent]
    public sealed class UnityCharacterControllerWorldBodyBinding : Float32WorldBodyBinding
    {
        [SerializeField] CharacterController m_CharacterController;
        [SerializeField] Transform m_LogicRoot;

        public CharacterController CharacterController => m_CharacterController;
        public Transform LogicRoot => m_LogicRoot;

        public void ConfigurePreview(
            string bindingId,
            ActorId actorId,
            CharacterController characterController,
            Transform logicRoot)
        {
            if (string.IsNullOrWhiteSpace(bindingId) || !actorId.IsValid ||
                !characterController || !logicRoot || characterController.transform != logicRoot)
                throw new System.ArgumentException("Preview World body binding configuration is incomplete.");
            ConfigureIdentity(bindingId, actorId);
            m_CharacterController = characterController;
            m_LogicRoot = logicRoot;
        }

        protected override void RequireImplementationValid()
        {
            if (!m_CharacterController || !m_LogicRoot)
                throw new System.InvalidOperationException($"World body binding '{BindingId}' requires an explicit CharacterController and LogicRoot.");
            if (m_CharacterController.transform != m_LogicRoot)
                throw new System.InvalidOperationException($"World body binding '{BindingId}' requires CharacterController and LogicRoot on the same Transform.");
            if (!m_CharacterController.enabled || !m_CharacterController.gameObject.activeInHierarchy)
                throw new System.InvalidOperationException($"World body binding '{BindingId}' requires an active CharacterController.");
        }

        protected override WorldBodyState BuildInitialBody(ActorId actorId)
        {
            return UnityCharacterSimulationWorldBody.CreateInitial(actorId, this);
        }
    }
}
