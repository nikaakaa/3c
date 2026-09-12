#if UNITY_EDITOR
using System;
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

    public static class BtsmtlSkillGraphPreviewToolbarRegistry
    {
        static Action<FlowGraph> s_Draw;

        public static void Register(Action<FlowGraph> draw)
        {
            if (draw == null)
                throw new ArgumentNullException(nameof(draw));
            if (s_Draw != null && !ReferenceEquals(s_Draw, draw))
                throw new InvalidOperationException("Skill graph preview toolbar is already registered.");
            s_Draw = draw;
        }

        public static void Unregister(Action<FlowGraph> draw)
        {
            if (ReferenceEquals(s_Draw, draw))
                s_Draw = null;
        }

        internal static void Draw(FlowGraph graph) => s_Draw?.Invoke(graph);
    }

    public static class BtsmtlSkillObservationToolbar
    {
        public static void Draw(FlowGraph graph)
        {
            BtsmtlSkillGraphPreviewToolbarRegistry.Draw(graph);
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
