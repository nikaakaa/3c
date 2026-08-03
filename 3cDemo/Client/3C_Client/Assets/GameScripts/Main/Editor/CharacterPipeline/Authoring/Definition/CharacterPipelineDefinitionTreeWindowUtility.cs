using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterPipelineDefinitionTreeWindowUtility
    {
        public static BaseTreeWindow OpenRootTree(CharacterPipelineDefinition definition)
        {
            if (!definition || !definition.RootTreeAsset)
                return null;

            return TreeWindowUtility.OpenTree(definition.RootTreeAsset, new CharacterPipelineAuthoringContext(definition));
        }
    }
}
