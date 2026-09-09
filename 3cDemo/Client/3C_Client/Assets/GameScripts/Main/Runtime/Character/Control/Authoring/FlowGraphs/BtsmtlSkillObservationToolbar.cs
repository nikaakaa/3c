#if UNITY_EDITOR
using FlowCanvas;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public interface IBtsmtlSkillObservationControls
    {
        bool CaptureValues { get; set; }
        string StatusMessage { get; }
        string InvocationLabel { get; }
        bool CanNavigateParent { get; }
        void SelectInstance();
        void NavigateParent();
        void NotifyTimelineOpening(BtsmtlSkillTimelineFlowNode node);
    }

    public static class BtsmtlSkillObservationToolbar
    {
        public static void Draw(FlowGraph graph)
        {
            if (graph.editorObservation is not IBtsmtlSkillObservationControls controls)
                return;
            bool changed = GUI.changed;
            controls.CaptureValues = GUILayout.Toggle(controls.CaptureValues, "采集端口值", EditorStyles.toolbarButton);
            if (GUILayout.Button("选择执行实例", EditorStyles.toolbarButton))
                controls.SelectInstance();
            if (controls.CanNavigateParent && GUILayout.Button("返回父调用", EditorStyles.toolbarButton))
                controls.NavigateParent();
            GUILayout.Label(controls.InvocationLabel, EditorStyles.miniLabel);
            GUILayout.Label(controls.StatusMessage, EditorStyles.miniLabel);
            GUI.changed = changed;
        }
    }
}
#endif
