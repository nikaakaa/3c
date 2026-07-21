using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.AI
{
    [CreateAssetMenu(fileName = "AIControllerDefinition", menuName = "3C/AI/Controller Definition")]
    public sealed class AIControllerDefinition : ScriptableObject
    {
        [SerializeField] string m_ControllerId = string.Empty;
        [SerializeField] BaseTreeAsset m_RootTreeAsset;
        [SerializeField] CharacterPipelineDefinition m_ControlledCharacter;
        [SerializeField] AIPerceptionProfile m_PerceptionProfile;
        [SerializeField] AIIntentProgramAsset m_IntentProgram;

        public string ControllerId => m_ControllerId ?? string.Empty;
        public BaseTreeAsset RootTreeAsset => m_RootTreeAsset;
        public CharacterPipelineDefinition ControlledCharacter => m_ControlledCharacter;
        public AIPerceptionProfile PerceptionProfile => m_PerceptionProfile;
        public AIIntentProgramAsset IntentProgram => m_IntentProgram;

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (string.IsNullOrWhiteSpace(ControllerId) || !string.Equals(ControllerId, ControllerId.Trim(), StringComparison.Ordinal))
            {
                errors?.Add($"{name}: ControllerId is invalid.");
                valid = false;
            }
            if (!m_RootTreeAsset)
            {
                errors?.Add($"{name}: AI RootTree is missing.");
                valid = false;
            }
            else if (m_RootTreeAsset.Tree is not AIControllerTree)
            {
                errors?.Add($"{name}: RootTree asset must contain AIControllerTree.");
                valid = false;
            }
            if (!m_ControlledCharacter)
            {
                errors?.Add($"{name}: controlled Character Pipeline Definition is missing.");
                valid = false;
            }
            if (!m_PerceptionProfile)
            {
                errors?.Add($"{name}: Perception Profile is missing.");
                valid = false;
            }
            else
            {
                valid &= m_PerceptionProfile.CollectConfigurationErrors(errors);
            }
            return valid;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(m_ControllerId))
                m_ControllerId = Guid.NewGuid().ToString("N");
        }

        public void SetRootTreeAsset(BaseTreeAsset rootTreeAsset)
        {
            m_RootTreeAsset = rootTreeAsset;
        }

        public void ConfigureAuthoring(
            string controllerId,
            BaseTreeAsset rootTreeAsset,
            CharacterPipelineDefinition controlledCharacter,
            AIPerceptionProfile perceptionProfile)
        {
            m_ControllerId = controllerId ?? string.Empty;
            m_RootTreeAsset = rootTreeAsset;
            m_ControlledCharacter = controlledCharacter;
            m_PerceptionProfile = perceptionProfile;
            var errors = new List<string>();
            if (!CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
        }

        public void SetIntentProgram(AIIntentProgramAsset intentProgram)
        {
            m_IntentProgram = intentProgram;
        }
#endif
    }
}
