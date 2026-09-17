using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using NodeCanvas.Editor;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [UnityEditor.InitializeOnLoad]
    static class BtsmtlSkillTimelineTreeClipEditorEntryPoint
    {
        static BtsmtlSkillTimelineTreeClipEditorEntryPoint() =>
            TimelineEditorWindow.TreeClipOpenRequested += Open;

        static bool Open(TreeClip clip)
        {
            if (clip?.AssetTree is not BtsmtlSkillFlowGraph graph)
                return false;
            GraphEditor editor = GraphEditor.OpenWindow(graph);
            editor.Show();
            editor.Focus();
            return true;
        }
    }
}
