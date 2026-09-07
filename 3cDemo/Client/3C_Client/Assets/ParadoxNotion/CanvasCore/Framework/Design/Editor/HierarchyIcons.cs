#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using NodeCanvas.Framework;

namespace NodeCanvas.Editor
{

    [InitializeOnLoad]
    static class HierarchyIcons
    {
        static HierarchyIcons() {
#if UNITY_6000_5_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= ShowIcon;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += ShowIcon;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= ShowIcon;
            EditorApplication.hierarchyWindowItemOnGUI += ShowIcon;
#endif
        }

#if UNITY_6000_5_OR_NEWER
        static void ShowIcon(EntityId ID, Rect r) {
#else
        static void ShowIcon(int ID, Rect r) {
#endif

            if ( !Prefs.showHierarchyIcons ) {
                return;
            }

#if UNITY_6000_5_OR_NEWER
            var go = EditorUtility.EntityIdToObject(ID) as GameObject;
#else
            var go = EditorUtility.InstanceIDToObject(ID) as GameObject;
#endif

            if ( go == null ) return;
            var owner = go.GetComponent<GraphOwner>();
            if ( owner == null ) return;
            r.xMin = r.xMax - 16;
            GUI.DrawTexture(r, StyleSheet.canvasIcon);
        }
    }
}

#endif