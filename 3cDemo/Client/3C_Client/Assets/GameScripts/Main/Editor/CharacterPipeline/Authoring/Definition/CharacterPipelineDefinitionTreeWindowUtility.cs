using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterPipelineDefinitionTreeWindowUtility
    {
        public static bool OpenSkillGraph(CharacterPipelineDefinition definition, string graphAuthoringId)
        {
            if (!definition || definition.SkillGraphs == null || definition.SkillGraphs.Count == 0)
                return false;

            for (int i = 0; i < definition.SkillGraphs.Count; i++)
            {
                BtsmtlSkillFlowGraph graph = definition.SkillGraphs[i];
                if (graph && string.Equals(graph.AuthoringId, graphAuthoringId, System.StringComparison.Ordinal))
                {
                    BtsmtlSkillGraphAuthoringPanel.SetDefinitionContext(definition);
                    return AssetDatabase.OpenAsset(graph);
                }
            }
            return false;
        }
    }
}
