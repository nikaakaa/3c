using System.Collections.Generic;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterPipelineDefinitionTreeWindowUtility
    {
        public static bool OpenSkillGraph(CharacterPipelineDefinition definition, string graphAuthoringId)
        {
            if (!definition)
                return false;

            IReadOnlyList<BtsmtlSkillFlowGraph> graphs = definition.AbilityGraphs;
            if (graphs == null || graphs.Count == 0)
                return false;
            for (int i = 0; i < graphs.Count; i++)
            {
                BtsmtlSkillFlowGraph graph = graphs[i];
                if (graph && string.Equals(graph.AuthoringId, graphAuthoringId, System.StringComparison.Ordinal))
                    return AssetDatabase.OpenAsset(graph);
            }
            return false;
        }
    }
}
