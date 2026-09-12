using System;
using NodeCanvas.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEditor.Callbacks;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterAnimationEventGraphEditorEntryPoint
    {
        [OnOpenAsset(0)]
        static bool OpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not CharacterAnimationEventGraph graph)
                return false;
            GraphEditor.OpenWindow(graph);
            return true;
        }

        [MenuItem("Tools/3C/Animation/Open Selected Event Graph")]
        static void OpenSelected()
        {
            if (Selection.activeObject is not CharacterAnimationEventGraph graph)
                throw new InvalidOperationException(
                    "Select a Character Animation Event Graph before opening it.");
            GraphEditor.OpenWindow(graph);
        }

        [MenuItem("Tools/3C/Animation/Open Selected Event Graph", true)]
        static bool ValidateOpenSelected() =>
            Selection.activeObject is CharacterAnimationEventGraph;
    }
}
