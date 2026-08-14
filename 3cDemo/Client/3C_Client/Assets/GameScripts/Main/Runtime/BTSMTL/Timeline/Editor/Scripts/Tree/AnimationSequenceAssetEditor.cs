using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    [CustomEditor(typeof(AnimationSequenceAsset), true)]
    public sealed class AnimationSequenceAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var sequence = (AnimationSequenceAsset)target;
            var root = new VisualElement();
            var open = new Button(() => TimelineEditorWindow.Open(sequence))
            {
                text = "Open Sequence"
            };
            root.Add(open);
            root.Add(new HelpBox(
                "Marker、Notify和Curve只在Timeline Editor的Sequence文档中编辑。Inspector只显示精确owner摘要。",
                HelpBoxMessageType.Info));
            AddObject(root, "Clip", sequence.Clip);
            string frameRate = sequence.Clip ? sequence.Clip.frameRate.ToString("0.###") : "Unavailable";
            root.Add(new Label($"{sequence.DurationFrame}F · {frameRate} fps · {(sequence.Loop ? "Loop" : "Finite")}"));
            root.Add(new Label($"Play Rate {sequence.DefaultPlayRate:0.###} · {sequence.SyncMode}"));
            root.Add(new Label($"{sequence.SyncGroupId} · {sequence.TimeMapping} · {sequence.SequenceTopology} · {sequence.SyncRole}"));
            root.Add(new Label($"Sync Markers: {sequence.SyncMarkers.Count}"));
            for (int i = 0; i < sequence.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = sequence.SyncMarkers[i];
                if (marker != null)
                    root.Add(new Label($"  {marker.Frame}F · {marker.MarkerId}"));
            }
            root.Add(new Label($"Curve Channels: {sequence.CurveChannels.Count}"));
            for (int i = 0; i < sequence.CurveChannels.Count; i++)
            {
                AnimationSequenceCurveChannel curve = sequence.CurveChannels[i];
                if (curve != null)
                    root.Add(new Label($"  {curve.ChannelId} · {curve.ValueDomain} · {curve.Curve.length} keys"));
            }
            root.Add(new Label($"Notifies: {sequence.Notifies.Count}"));
            if (sequence is IAnimationSequenceAnalysisReference analysis)
            {
                AddObject(root, "Analysis Source", analysis.AnalysisSource);
                var identity = new TextField("Analysis Identity") { value = analysis.AnalysisIdentity };
                identity.SetEnabled(false);
                root.Add(identity);
            }
            return root;
        }

        static void AddObject(VisualElement root, string label, UnityEngine.Object value)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false
            };
            field.SetValueWithoutNotify(value);
            field.SetEnabled(false);
            root.Add(field);
        }
    }
}
