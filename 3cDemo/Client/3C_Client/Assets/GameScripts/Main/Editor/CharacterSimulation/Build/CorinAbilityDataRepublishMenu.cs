using System;
using UnityEngine;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CorinAbilityDataRepublishMenu
    {
        const string DefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string OutputFolder = "Assets/Configs/Character/Corin/Pipeline/Abilities";

        [MenuItem("Tools/3C/Internal/Republish Corin Ability Data")]
        static void Republish()
        {
            Debug.Log("Republish Corin Ability Data: start");
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (definition == null)
                throw new InvalidOperationException($"Corin Character Pipeline Definition '{DefinitionPath}' is missing.");
            GameplayAbilityExecutionDataAssetPublisher.PublishDefinition(definition, OutputFolder);
        }
    }
}