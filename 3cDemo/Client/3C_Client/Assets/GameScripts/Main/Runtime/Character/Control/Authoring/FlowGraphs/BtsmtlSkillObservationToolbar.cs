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
    }

    public static class BtsmtlSkillObservationToolbar
    {
        public static void Draw(FlowGraph graph)
        {
            if (graph.editorObservation is not IBtsmtlSkillObservationControls controls)
                return;
            bool changed = GUI.changed;
            controls.CaptureValues = GUILayout.Toggle(controls.CaptureValues, "采集端口值", EditorStyles.toolbarButton);
            GUILayout.Label(controls.StatusMessage, EditorStyles.miniLabel);
            GUI.changed = changed;
        }
    }
}
#endif
