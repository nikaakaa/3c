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
    public static class TreeClipAuthoringService
    {
        public static void EnsureInline(TreeClip clip)
        {
            if (clip == null)
                return;
            clip.EnsureInlineTree();
        }

        public static void UseInline(TreeClip clip)
        {
            if (clip == null)
                return;
            TimelineRunningTree source = clip.ResolvedTree;
            clip.SetInlineTree(source != null ? source.CloneForAuthoring() : TimelineRunningTree.CreateDefault("Timeline Tree"));
        }

        public static TreeClip CreateDecisionGate(
            TimelineData timeline,
            BaseExposedProperty declaration,
            int startFrame,
            int endFrame)
        {
            if (timeline == null || declaration == null ||
                declaration.BlackboardScope != PipelineBlackboardVariableScope.Frame ||
                declaration.BlackboardLifetime != PipelineBlackboardVariableLifetime.Frame)
                throw new System.InvalidOperationException("Decision gate requires a Frame/Frame blackboard declaration.");

            TreeTrack track = timeline.Tracks.OfType<TreeTrack>().FirstOrDefault();
            if (track == null)
            {
                timeline.AddTrack(typeof(TreeTrack), TimelineTreeContractComposition.Create());
                track = timeline.Tracks.OfType<TreeTrack>().First();
            }

            TreeClip clip = timeline.AddClip(TimelineTreeContractComposition.Create(), track, startFrame) as TreeClip;
            clip.StartFrame = startFrame;
            clip.EndFrame = System.Math.Max(startFrame + 1, endFrame);
            clip.SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            TimelineRunningTree tree = clip.InlineTree;
            tree.name = $"Decision {declaration.BlackboardKey}";
            ExposedPropertyNode setter = tree.CreateNode(typeof(ExposedPropertyNode)) as ExposedPropertyNode;
            setter.SetNodeType(ExposedPropertyNodeType.Set);
            setter.SetExposedProperty(declaration);
            setter.Value.SetValue(true);
            setter.DisplayName = $"Set {declaration.BlackboardKey}";
            setter.Position = new Vector2(320f, 0f);
            RootNode root = tree.Nodes.OfType<RootNode>().First();
            tree.Link(root, setter, "Output", "Input");
            tree.CheckInit();
            timeline.Init();
            return clip;
        }

        public static BaseTreeAsset ExtractShared(TreeClip clip)
        {
            if (clip?.InlineTree == null || clip.Timeline == null)
                return null;

            string timelinePath = AssetDatabase.GetAssetPath(clip.Timeline.SerializedOwner);
            string directory = System.IO.Path.GetDirectoryName(timelinePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
                return null;

            string folder = $"{directory}/SharedTrees";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(directory, "SharedTrees");

            TimelineRunningTree sharedTree = clip.InlineTree.Clone();
            BaseTreeAsset asset = ScriptableObject.CreateInstance<BaseTreeAsset>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Sanitize(sharedTree.name)}.asset");
            AssetDatabase.CreateAsset(asset, path);
            asset.SetTree(sharedTree);
            EditorUtility.SetDirty(asset);
            clip.SetSharedTreeAsset(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static string Sanitize(string value)
        {
            string result = string.IsNullOrEmpty(value) ? "TimelineTree" : value;
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            return result;
        }
    }

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
