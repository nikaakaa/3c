using BTSMTL.Timeline.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    sealed class BtsmtlScenePlayPreviewWindow : EditorWindow
    {
        static BtsmtlScenePlayPreviewWindow s_Window;
        BtsmtlExecutionTimelineView m_Execution;
        Label m_Status;

        internal static bool IsOpen => s_Window != null;

        [MenuItem("Window/BTSMTL/Preview")]
        internal static void Open()
        {
            bool alreadyOpen = IsOpen;
            var window = GetWindow<BtsmtlScenePlayPreviewWindow>();
            window.titleContent = new GUIContent("角色预览");
            window.minSize = new Vector2(780, 560);
            window.Show();
            window.Focus();
            if (alreadyOpen)
                BtsmtlScenePlayTimelineController.OnPreviewWindowOpened();
        }

        void OnEnable()
        {
            s_Window = this;
            titleContent = new GUIContent("角色预览");
            minSize = new Vector2(780, 560);
        }

        void CreateGUI()
        {
            m_Execution?.Dispose();
            rootVisualElement.Clear();
            m_Status = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            m_Status.style.paddingLeft = 8f;
            m_Status.style.paddingTop = 4f;
            m_Status.style.paddingBottom = 4f;
            rootVisualElement.Add(BtsmtlScenePlayTimelineController.CreatePreviewControls(this));
            rootVisualElement.Add(m_Status);
            var split = new TwoPaneSplitView(0, 290, TwoPaneSplitViewOrientation.Vertical)
            {
                viewDataKey = "ability-preview-vertical-split"
            };
            split.style.flexGrow = 1;
            var viewport = new BtsmtlScenePlayPreviewView();
            var upper = new TwoPaneSplitView(1, 280, TwoPaneSplitViewOrientation.Horizontal)
            {
                viewDataKey = "ability-preview-blackboard-split"
            };
            upper.Add(viewport);
            upper.Add(new BtsmtlScenePlayBlackboardView());
            split.Add(upper);
            m_Execution = new BtsmtlExecutionTimelineView(
                BeginWindows, EndWindows,
                BtsmtlScenePlayTimelineController.OpenExecutionSource,
                BtsmtlScenePlayTimelineController.SeekExecutionHistory);
            m_Execution.style.minHeight = 180;
            split.Add(m_Execution);
            rootVisualElement.Add(split);
            viewport.SetVisible(true);
            m_Execution.SetVisible(true);
            BtsmtlScenePlayTimelineController.OnPreviewWindowOpened();
        }

        internal void SetStatus(string status) => m_Status.text = status;

        void OnDisable()
        {
            m_Execution?.Dispose();
            m_Execution = null;
            rootVisualElement.Clear();
            s_Window = null;
            BtsmtlScenePlayTimelineController.OnPreviewWindowClosed(this);
        }
    }
}
