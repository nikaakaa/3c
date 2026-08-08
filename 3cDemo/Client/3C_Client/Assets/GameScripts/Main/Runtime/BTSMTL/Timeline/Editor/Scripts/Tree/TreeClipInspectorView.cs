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
    public sealed class TreeClipInspectorView : TimelineClipInspectorView
    {
        readonly TreeClip m_TreeClip;
        readonly ToolbarToggle m_DecisionToggle;
        readonly ToolbarToggle m_CommitToggle;
        readonly Label m_OwnershipLabel;
        readonly Label m_OutputSummary;

        public TreeClipInspectorView(Clip clip)
        {
            m_TreeClip = clip as TreeClip;
            if (m_TreeClip == null)
                return;

            Add(new Label("Phase"));
            var phaseToolbar = new Toolbar();
            m_DecisionToggle = new ToolbarToggle { text = "Decision" };
            m_CommitToggle = new ToolbarToggle { text = "Commit" };
            m_DecisionToggle.RegisterValueChangedCallback(evt => SetPhase(evt, TimelineTreeExecutionPhase.Decision));
            m_CommitToggle.RegisterValueChangedCallback(evt => SetPhase(evt, TimelineTreeExecutionPhase.Commit));
            phaseToolbar.Add(m_DecisionToggle);
            phaseToolbar.Add(m_CommitToggle);
            Add(phaseToolbar);

            m_OwnershipLabel = new Label();
            Add(m_OwnershipLabel);

            ObjectField sharedTreeField = new ObjectField("Shared Tree")
            {
                objectType = typeof(BaseTreeAsset),
                allowSceneObjects = false,
                value = m_TreeClip.SharedTreeAsset
            };
            sharedTreeField.RegisterValueChangedCallback(evt =>
            {
                BaseTreeAsset asset = evt.newValue as BaseTreeAsset;
                if (asset && !(asset.Tree is TimelineRunningTree))
                {
                    sharedTreeField.SetValueWithoutNotify(m_TreeClip.SharedTreeAsset);
                    return;
                }

                Modify("Set TreeClip Shared Tree", () =>
                {
                    if (asset)
                        m_TreeClip.SetSharedTreeAsset(asset);
                    else
                        m_TreeClip.EnsureInlineTree();
                });
                Refresh();
            });
            Add(sharedTreeField);

            VisualElement actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            Button openButton = new Button(OpenTree) { text = "Open" };
            Button extractButton = new Button(ExtractShared) { text = "Extract Shared" };
            Button inlineButton = new Button(UseInline) { text = "Use Inline" };
            actions.Add(openButton);
            actions.Add(extractButton);
            actions.Add(inlineButton);
            Add(actions);

            m_OutputSummary = new Label();
            m_OutputSummary.style.whiteSpace = WhiteSpace.Normal;
            Add(m_OutputSummary);
            Label previewStatus = new Label("Preview execution requires a formal pipeline scheduler context.");
            previewStatus.style.whiteSpace = WhiteSpace.Normal;
            Add(previewStatus);
            Refresh();
        }

        void OpenTree()
        {
            EditorView?.OpenClip(m_TreeClip);
        }

        void ExtractShared()
        {
            if (m_TreeClip.Ownership != TimelineTreeOwnership.Inline)
                return;

            Modify("Extract TreeClip Shared Tree", () => TreeClipAuthoringService.ExtractShared(m_TreeClip));
            Refresh();
        }

        void UseInline()
        {
            Modify("Use Inline TreeClip Tree", () => TreeClipAuthoringService.UseInline(m_TreeClip));
            Refresh();
        }

        void Modify(string name, System.Action action)
        {
            TimelineData timeline = m_TreeClip.Timeline;
            timeline.ApplyModify(() =>
            {
                action();
                timeline.Init();
            }, name);
        }

        void SetPhase(ChangeEvent<bool> evt, TimelineTreeExecutionPhase phase)
        {
            if (!evt.newValue)
            {
                Refresh();
                return;
            }

            Modify("Set TreeClip Phase", () => m_TreeClip.SetExecutionPhase(phase));
            Refresh();
        }

        void Refresh()
        {
            m_DecisionToggle?.SetValueWithoutNotify(m_TreeClip.ExecutionPhase == TimelineTreeExecutionPhase.Decision);
            m_CommitToggle?.SetValueWithoutNotify(m_TreeClip.ExecutionPhase == TimelineTreeExecutionPhase.Commit);
            m_OwnershipLabel.text = $"Ownership: {m_TreeClip.Ownership}";
            TimelineRunningTree tree = m_TreeClip.ResolvedTree;
            if (tree == null)
            {
                m_OutputSummary.text = "Missing TimelineRunningTree";
                return;
            }

            string[] outputs = tree.Nodes
                .OfType<ExposedPropertyNode>()
                .Where(i => i.NodeType == ExposedPropertyNodeType.Set && i.BlackboardVariable.IsValid)
                .Select(i => OutputSummary(i.BlackboardVariable))
                .Distinct()
                .ToArray();
            m_OutputSummary.text = outputs.Length == 0
                ? "Decision outputs: None"
                : $"Decision outputs: {string.Join(", ", outputs)}";
        }

        static string OutputSummary(PipelineBlackboardVariableReference reference)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:BaseTreeAsset"))
            {
                BaseTreeAsset asset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(AssetDatabase.GUIDToAssetPath(guid));
                BaseExposedProperty declaration = asset?.Tree?.ExposedProperties.FirstOrDefault(i =>
                    i.DeclarationId == reference.DeclarationId && i.DeclarationOwnerId == reference.DeclarationOwnerId);
                if (declaration == null)
                    continue;

                return declaration.FactProjection?.Kind == PipelineBlackboardFactProjectionKind.ActionWindow
                    ? $"{reference.DisplayKey} -> ActionWindow({declaration.FactProjection.ActionWindowType}/{declaration.FactProjection.ActionWindowId}/{declaration.FactProjection.ActionWindowDigest})"
                    : $"{reference.DisplayKey} -> Local";
            }

            return $"{reference.DisplayKey} -> Missing declaration";
        }
    }

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
                timeline.AddTrack(typeof(TreeTrack));
                track = timeline.Tracks.OfType<TreeTrack>().First();
            }

            TreeClip clip = timeline.AddClip(track, startFrame) as TreeClip;
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
