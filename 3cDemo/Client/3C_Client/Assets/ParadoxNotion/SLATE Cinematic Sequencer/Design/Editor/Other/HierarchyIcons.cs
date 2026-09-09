#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Slate
{

    [InitializeOnLoad]
    public static class HierarchyIcons
    {

        static HierarchyIcons() {
#if UNITY_6000_5_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= ShowIcons;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += ShowIcons;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= ShowIcons;
            EditorApplication.hierarchyWindowItemOnGUI += ShowIcons;
#endif
            Styles.cutsceneIcon = (Texture2D)Resources.Load("Cutscene Icon"); //ensure
        }

#if UNITY_6000_5_OR_NEWER
        static void ShowIcons(EntityId ID, Rect r) {
#else
        static void ShowIcons(int ID, Rect r) {
#endif

#if UNITY_6000_5_OR_NEWER
            var go = EditorUtility.EntityIdToObject(ID) as GameObject;
#else
            var go = EditorUtility.InstanceIDToObject(ID) as GameObject;
#endif

            if ( go == null ) {
                return;
            }

            if ( go.GetComponent<Cutscene>() != null ) {
                r.x = r.xMax - 16;
                r.width = 16;
                GUI.DrawTexture(r, Styles.cutsceneIcon);
            }

            if ( go.GetComponent(typeof(IDirectableCamera)) != null ) {
                r.x = r.xMax - 16;
                r.width = 16;
                GUI.DrawTexture(r, Styles.cameraIcon);
            }
        }
    }
}

#endif