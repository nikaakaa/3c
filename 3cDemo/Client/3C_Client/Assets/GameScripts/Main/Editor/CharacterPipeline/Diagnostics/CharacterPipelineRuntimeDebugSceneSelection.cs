using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
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

            FixedCharacterHost fixedHost = selected.GetComponentInParent<FixedCharacterHost>(true);
            if (fixedHost)
                return new RuntimeDebugSceneSelection(fixedHost.GetInstanceID());

            DeterministicRollbackCharacterHost rollbackHost = selected.GetComponentInParent<DeterministicRollbackCharacterHost>(true);
            return rollbackHost ? new RuntimeDebugSceneSelection(rollbackHost.GetInstanceID()) : default;
        }
    }
}
