using BTSMTL.Diagnostics.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterPipelineRuntimeDebugSceneSelection
    {
        static CharacterPipelineRuntimeDebugSceneSelection()
        {
            RuntimeDebugSceneSelectionRegistry.Register(Resolve);
        }

        static RuntimeDebugSceneSelection Resolve()
        {
            GameObject selected = Selection.activeGameObject;
            if (!selected || EditorUtility.IsPersistent(selected))
                return default;

            CharacterPipelineHost host = selected.GetComponentInParent<CharacterPipelineHost>(true);
            return host ? new RuntimeDebugSceneSelection(host.GetInstanceID()) : default;
        }
    }
}
