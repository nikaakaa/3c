using ThirdPersonAction;
using UnityEditor;

namespace ThirdPersonCharacterBehavior.Editor.RefImport
{
    public static class CharacterBehaviorRefImportAdapter
    {
        [MenuItem("Tools/3C/Character Behavior/Import Ref Timeline Shape")]
        public static void ShowBoundaryMessage()
        {
            EditorUtility.DisplayDialog(
                "Character Behavior Ref Import",
                "Ref/wly970123 editor UI can be converted into this project's authoring data. Runtime runners are intentionally excluded.",
                "OK");
        }

        public static ActionTimelineTrackKind MapTrackName(string trackName)
        {
            switch ((trackName ?? string.Empty).Trim())
            {
                case "Animation":
                    return ActionTimelineTrackKind.Animation;
                case "Motion":
                    return ActionTimelineTrackKind.Motion;
                case "Hitbox":
                    return ActionTimelineTrackKind.Hitbox;
                case "Cancel":
                    return ActionTimelineTrackKind.Cancel;
                case "Cue":
                    return ActionTimelineTrackKind.Cue;
                default:
                    return ActionTimelineTrackKind.None;
            }
        }
    }
}
