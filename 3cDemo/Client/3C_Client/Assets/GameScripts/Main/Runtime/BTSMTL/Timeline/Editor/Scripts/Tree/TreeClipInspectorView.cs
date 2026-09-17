using TreeDesigner.Authoring;
using System.Linq;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline
{
    public static class TimelineAuthoringCommands
    {
        public static readonly GraphAuthoringCommandId UseInline =
            new GraphAuthoringCommandId(
                "btsmtl.timeline.use-inline");
        public static readonly GraphAuthoringCommandId UseShared =
            new GraphAuthoringCommandId(
                "btsmtl.timeline.use-shared");
    }
}

namespace BTSMTL.Timeline.Editor
{
    [InitializeOnLoad]
    public static class TimelineNodeAuthoringBridge
    {
        static TimelineNodeAuthoringBridge()
        {
            AuthoringPageOpenRegistry.Register<TimelineNode>(OpenNode, PopulateNodeInspector);
        }

        static void OpenNode(BaseTreeWindow window, TimelineNode node)
        {
            if (window == null || node?.Timeline == null)
                return;
            TimelineEditorWindow.Open(window, node);
        }

        static void PopulateNodeInspector(BaseTreeInspectorView inspector, BaseNodeView nodeView, TimelineNode node)
        {
            VisualElement container = inspector.SelectionInspectorContainer;
            container.Add(new Label($"Ownership: {node.TimelineOwnership}"));
            container.Add(new Label($"Timeline: {(node.Timeline != null ? node.Timeline.Name : "Missing")}"));

            if (node.TimelineOwnership == TimelineOwnership.Missing)
            {
                Label error = new Label("Timeline ownership is missing. Choose a formal Inline or Shared source.");
                error.style.color = new Color(0.9f, 0.25f, 0.2f);
                container.Add(error);
                return;
            }

            VisualElement actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.Add(new Button(() => OpenNode(nodeView.TreeWindow, node)) { text = "Open" });

            if (node.TimelineOwnership == TimelineOwnership.Inline)
            {
                actions.Add(new Button(() =>
                {
                    TimelineAsset asset = CreateSharedTimelineAsset(node);
                    nodeView.ExecuteAuthoringCommand(TimelineAuthoringCommands.UseShared, asset);
                    TimelineEditorWindow.RebindIfOpen(node);
                    RefreshNodeInspector(inspector, nodeView);
                }) { text = "Extract Shared" });

                ObjectField sharedPicker = CreateSharedPicker(node, inspector, nodeView);
                sharedPicker.style.display = DisplayStyle.None;
                actions.Add(new Button(() => sharedPicker.style.display = DisplayStyle.Flex) { text = "Use Shared" });
                container.Add(actions);
                container.Add(sharedPicker);
                return;
            }

            actions.Add(new Button(() =>
            {
                nodeView.ExecuteAuthoringCommand(TimelineAuthoringCommands.UseInline);
                TimelineEditorWindow.RebindIfOpen(node);
                RefreshNodeInspector(inspector, nodeView);
            }) { text = "Use Inline" });
            container.Add(actions);
            ObjectField sharedAsset = new ObjectField("Shared Timeline")
            {
                objectType = typeof(TimelineAsset),
                allowSceneObjects = false,
                value = node.SharedTimelineAsset
            };
            sharedAsset.RegisterValueChangedCallback(evt =>
            {
                TimelineAsset asset = evt.newValue as TimelineAsset;
                if (!asset)
                {
                    sharedAsset.SetValueWithoutNotify(node.SharedTimelineAsset);
                    return;
                }
                nodeView.ExecuteAuthoringCommand(
                    TimelineAuthoringCommands.UseShared,
                    asset);
                TimelineEditorWindow.RebindIfOpen(node);
                RefreshNodeInspector(inspector, nodeView);
            });
            container.Add(sharedAsset);
        }

        static ObjectField CreateSharedPicker(TimelineNode node, BaseTreeInspectorView inspector, BaseNodeView nodeView)
        {
            ObjectField picker = new ObjectField("Shared Timeline")
            {
                objectType = typeof(TimelineAsset),
                allowSceneObjects = false
            };
            picker.RegisterValueChangedCallback(evt =>
            {
                TimelineAsset asset = evt.newValue as TimelineAsset;
                if (!asset)
                    return;
                nodeView.ExecuteAuthoringCommand(
                    TimelineAuthoringCommands.UseShared,
                    asset);
                TimelineEditorWindow.RebindIfOpen(node);
                RefreshNodeInspector(inspector, nodeView);
            });
            return picker;
        }

        static TimelineAsset CreateSharedTimelineAsset(
            TimelineNode node)
        {
            TimelineData timeline = node?.InlineTimeline;
            UnityEngine.Object owner = timeline?.SerializedOwner;
            string ownerPath = AssetDatabase.GetAssetPath(owner);
            string directory = System.IO.Path.GetDirectoryName(ownerPath)?.Replace('\\', '/');
            if (timeline == null || string.IsNullOrEmpty(directory))
                throw new System.InvalidOperationException("Inline Timeline requires a persistent serialized owner before extraction.");

            string folder = $"{directory}/SharedTimelines";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(directory, "SharedTimelines");

            TimelineAsset asset = ScriptableObject.CreateInstance<TimelineAsset>();
            asset.SetData(timeline.Clone());
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Sanitize(timeline.Name)}.asset");
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static void RefreshNodeInspector(BaseTreeInspectorView inspector, BaseNodeView nodeView)
        {
            nodeView.Refresh();
            inspector.RefreshNodeSelection(nodeView);
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OpenTimelineAsset(int instanceId, int line)
        {
            TimelineAsset asset = EditorUtility.InstanceIDToObject(instanceId) as TimelineAsset;
            if (!asset)
                return false;

            return TimelineEditorWindow.Open(asset) != null;
        }

        static string Sanitize(string value)
        {
            string result = string.IsNullOrEmpty(value) ? "Timeline" : value;
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            return result;
        }
    }

}
