#if !UNITY_6000_3_OR_NEWER

using UnityEditor;
using UnityToolbarExtender;

namespace TEngine
{
    [InitializeOnLoad]
    public partial class UnityToolbarExtenderRight
    {
        static UnityToolbarExtenderRight()
        {
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI_SceneSwitch);
            EditorApplication.projectChanged += UpdateScenes;
            UpdateScenes();
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI_EditorPlayMode);
            _resourceModeIndex = EditorPrefs.GetInt("EditorPlayMode", 0);
        }
    }
}

#endif
