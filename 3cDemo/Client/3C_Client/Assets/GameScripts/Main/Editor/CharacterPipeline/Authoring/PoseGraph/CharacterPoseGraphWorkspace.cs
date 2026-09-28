using BTSMTL.Authoring.Editor;
using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed partial class CharacterPoseGraphWorkspace : IDisposable
    {
        static CharacterPoseGraphWorkspace s_Current;
        readonly Dictionary<string, string> m_ObservedPorts = new Dictionary<string, string>(StringComparer.Ordinal);

        internal static bool TryGetFieldOptions(
            CharacterPoseCanvasNode node,
            GraphAuthoringFieldDescriptor field,
            out IReadOnlyList<GraphAuthoringFieldOption> options)
        {
            options = Array.Empty<GraphAuthoringFieldOption>();
            CharacterPoseGraphWorkspace workspace = s_Current;
            if (workspace == null || workspace.m_Document == null ||
                workspace.m_DetailsDataSource == null || node == null ||
                workspace.m_Canvas?.Graph != node.graph)
                return false;
            return workspace.m_DetailsDataSource.TryGetFieldOptions(
                workspace.m_Document,
                new GraphAuthoringElementId(node.NodeId.Value),
                field,
                out options);
        }

        const string SessionKey = "3C.PoseCanvas.Workspace.";
        static bool s_Reloading;
        NodeCanvas.Editor.GraphEditor m_Editor;
        readonly VisualElement rootVisualElement = new VisualElement();
        Rect position => m_Editor.position;
        void Repaint() => m_Editor.Repaint();
        void ShowNotification(GUIContent message) => m_Editor.ShowNotification(message);

        [InitializeOnLoadMethod]
        static void RegisterWorkspace()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                s_Current?.SaveWorkspace();
                s_Reloading = true;
                s_Current?.Dispose();
            };
            EditorApplication.delayCall += RestoreWorkspace;
        }

        static void RestoreWorkspace()
        {
            string assetPath = SessionState.GetString(SessionKey + "Asset", string.Empty);
            if (string.IsNullOrEmpty(assetPath))
                return;
            var asset = AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(assetPath);
            var profile = AssetDatabase.LoadAssetAtPath<CharacterAnimationPresentationProfile>(SessionState.GetString(SessionKey + "Profile", string.Empty));
            var definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(SessionState.GetString(SessionKey + "Definition", string.Empty));
            string navigation = SessionState.GetString(SessionKey + "Navigation", string.Empty);
            bool authoringOnly = string.IsNullOrEmpty(SessionState.GetString(SessionKey + "Definition", string.Empty));
            if (!asset || !authoringOnly && (!profile || !definition || definition.AnimationPresentationProfile != profile))
            {
                SessionState.EraseString(SessionKey + "Asset");
                Debug.LogWarning("Pose Canvas workspace context changed; reopen it from the exact Character Definition.");
                return;
            }
            CharacterPoseGraphWorkspace workspace = authoringOnly ? OpenAuthoring(asset) : Open(asset, profile, definition);
            workspace.RestoreNavigation(navigation);
        }

        void SaveWorkspace()
        {
            if (!m_Editor || NodeCanvas.Editor.GraphEditor.current != m_Editor) return;
            SessionState.SetString(SessionKey + "Asset", AssetDatabase.GetAssetPath(m_Asset));
            SessionState.SetString(SessionKey + "Profile", AssetDatabase.GetAssetPath(m_Profile));
            SessionState.SetString(SessionKey + "Definition", AssetDatabase.GetAssetPath(m_Definition));
            SessionState.SetString(SessionKey + "Navigation", CaptureNavigation());
        }

        const string WorkspaceVisualTreePath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.uxml";
        const string WorkspaceStylePath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.uss";

        [SerializeField] CharacterPresentationPoseGraphAsset m_Asset;
        [SerializeField] CharacterAnimationPresentationProfile m_Profile;
        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] string m_CurrentGraphId = string.Empty;

        CharacterPoseCanvasBinding m_Canvas;
        CharacterPoseCanvasBinding m_StateMachineSurface;
        GraphAuthoringDetailsRegion m_Details;
        GraphAuthoringNavigatorPresenter m_Navigator;
        GraphAuthoringUndoBinding m_UndoBinding;
        Label m_Title;
        Label m_Status;
        CharacterPoseGraphAssetMutationOwner m_Owner;
        CharacterPoseCanvasGraphDocument m_Document;
        CharacterPoseCanvasMutationAdapter m_Mutation;
        CharacterPoseCanvasDetailsDataSource m_DetailsDataSource;
        Action m_ShowDetails;
        CharacterPoseStateMachineDocument m_StateMachineDocument;
        CharacterPoseStateMachineMutationAdapter m_StateMachineMutation;
        CharacterPoseTransitionRuleDocument m_RuleDocument;
        CharacterPoseTransitionRuleMutationAdapter m_RuleMutation;
        CharacterLinkedPoseAuthoringWorkspacePresenter m_LinkedPoseWorkspace;
        VisualElement m_LinkedPoseDetails;
        VisualElement m_SelectionTuningHost;
        string m_LinkedPoseSelectionId = string.Empty;
        string m_LinkedPoseWorkspaceStatus = "Unavailable";
        bool m_BindingPanel;
        NodeCanvas.Framework.Graph m_PanelGraph;
        bool m_ShowingStateMachine => NodeCanvas.Editor.GraphEditor.currentGraph is CharacterPoseDocumentCanvas stateView && stateView.StateMachineBinding != null;
        bool m_ShowingTransitionRule => NodeCanvas.Editor.GraphEditor.currentGraph is CharacterPoseDocumentCanvas ruleView && ruleView.ProjectionBinding?.Document is CharacterPoseTransitionRuleDocument;
        GraphAuthoringSelectionBinding m_SelectionBinding;
        GraphAuthoringSelection? m_LastSelection;
        string m_LastContentRevision = string.Empty;
        CharacterAnimationVariableContract m_EditorAnimationVariables;

        internal CharacterPipelineDefinition DefinitionContext => m_Definition;
        internal CharacterAnimationPresentationProfile ProfileContext => m_Profile;
        internal static CharacterAnimationRigDefinition CurrentRigDefinition =>
            s_Current?.m_Profile?.RigDefinition;
        internal static CharacterAnimationVariableContract CurrentAnimationVariables =>
            s_Current?.m_EditorAnimationVariables;

        internal static void DrawNativeToolbar(CharacterPoseCanvasGraph graph)
        {
            CharacterPoseGraphWorkspace workspace = s_Current;
            if (workspace == null || !workspace.m_Asset ||
                !workspace.m_Asset.EnumerateGraphs().Contains(graph))
                return;

            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                workspace.ValidateAuthoring();
            if (GUILayout.Button("保存", EditorStyles.toolbarButton))
                workspace.SaveAuthoring();
            using (new EditorGUI.DisabledScope(!workspace.m_Profile))
            {
                if (GUILayout.Button("Compile", EditorStyles.toolbarButton))
                    workspace.CompilePoseProjection();
            }
        }

        internal CharacterPresentationPoseGraphAsset AssetContext => m_Asset;
        internal string CurrentStateMachineId => m_StateMachineDocument?.DocumentId ?? string.Empty;
        internal CharacterPipelineDefinition DefinitionContextValue => m_Definition;
        internal bool IsLinkedPoseReadOnly => false;
        internal string LinkedPoseWorkspaceStatus => m_LinkedPoseWorkspaceStatus;

        public static CharacterPoseGraphWorkspace Open(
            CharacterAnimationPresentationProfile profile)
        {
            if (!profile || !profile.PoseGraph || !profile.RigDefinition)
                throw new ArgumentException(
                    "Animation Presentation Profile requires one Pose Graph and Rig Definition.",
                    nameof(profile));
            CharacterPipelineDefinition[] definitions = AssetDatabase.FindAssets("t:CharacterPipelineDefinition")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(definition => definition && definition.AnimationPresentationProfile == profile).ToArray();
            if (definitions.Length != 1)
                return CreateWorkspace(profile.PoseGraph, profile, null);
            CharacterPipelineDefinition definition = definitions[0];
            return Open(
                profile.PoseGraph,
                profile,
                definition);
        }

        public static CharacterPoseGraphWorkspace Open(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definition)
        {
            if (!asset || asset.Graph == null || !asset.Graph.GraphId.IsValid)
                throw new ArgumentException("Presentation Pose Graph is missing typed authoring data.", nameof(asset));
            if (!profile || !definition || !profile.RigDefinition)
                throw new InvalidOperationException(
                    "Pose Graph requires one exact Definition, Presentation Profile and Rig Definition context.");
            if (definition.AnimationPresentationProfile != profile)
                throw new InvalidOperationException("Character Definition does not own the selected Presentation Profile.");
            return CreateWorkspace(asset, profile, definition);
        }

        internal static CharacterPoseGraphWorkspace OpenAuthoring(CharacterPresentationPoseGraphAsset asset)
        {
            if (!asset || asset.Graph == null || !asset.Graph.GraphId.IsValid)
                throw new ArgumentException("Pose Graph has no valid authoring graph.", nameof(asset));
            CharacterAnimationPresentationProfile[] profiles = AssetDatabase
                .FindAssets("t:CharacterAnimationPresentationProfile")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterAnimationPresentationProfile>(
                    AssetDatabase.GUIDToAssetPath(guid)))
                .Where(profile => profile && profile.PoseGraph == asset)
                .ToArray();
            if (profiles.Length != 1)
                return CreateWorkspace(asset, null, null);

            CharacterAnimationPresentationProfile profile = profiles[0];
            CharacterPipelineDefinition[] definitions = AssetDatabase
                .FindAssets("t:CharacterPipelineDefinition")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid)))
                .Where(definition => definition && definition.AnimationPresentationProfile == profile)
                .ToArray();
            if (definitions.Length != 1)
                return CreateWorkspace(asset, profile, null);

            CharacterPipelineDefinition definition = definitions[0];
            return CreateWorkspace(asset, profile, definition);
        }

        static CharacterPoseGraphWorkspace CreateWorkspace(CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile, CharacterPipelineDefinition definition)
        {
            s_Current?.Dispose();
            asset.Graph.SetEditorAnimationVariables(
                CreateEditorAnimationVariables(profile));
            NodeCanvas.Editor.GraphEditor editor = NodeCanvas.Editor.GraphEditor.OpenWindow(asset.Graph);
            editor.minSize = new Vector2(1000f, 680f);
            var workspace = new CharacterPoseGraphWorkspace { m_Editor = editor };
            s_Current = workspace;
            workspace.SetDocument(asset, profile, definition);
            workspace.CreateGUI();
            NodeCanvas.Editor.GraphEditorUtility.onSelectionChanged += workspace.PublishSelection;
            NodeCanvas.Editor.GraphEditor.onEditorNavigationChanged += workspace.SaveWorkspace;
            NodeCanvas.Editor.GraphEditor.onEditorClosed += workspace.Dispose;
            EditorApplication.projectChanged += workspace.OnAuthoringAssetsChanged;
            return workspace;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    WorkspaceVisualTreePath);
            if (!visualTree)
                throw new InvalidOperationException(
                    "Pose Graph workspace visual tree is missing.");
            visualTree.CloneTree(rootVisualElement);
            StyleSheet styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    WorkspaceStylePath);
            if (styleSheet)
                rootVisualElement.styleSheets.Add(styleSheet);

            VisualElement toolbar = Require("pose-toolbar-content");
            VisualElement navigatorHost = Require("pose-navigator-content");
            VisualElement canvasHost = Require("pose-graph-content");
            VisualElement detailsHost = Require("pose-details-content");
            VisualElement tabs = Require("pose-panel-tabs");
            void SelectPanel(VisualElement selected)
            {
                foreach (VisualElement page in new[] { detailsHost, navigatorHost })
                    page.style.display = page == selected ? DisplayStyle.Flex : DisplayStyle.None;
            }
            tabs.Add(new Button(() => SelectPanel(detailsHost)) { text = "详情" });
            tabs.Add(new Button(() => SelectPanel(navigatorHost)) { text = "图目录" });
            SelectPanel(detailsHost);
            m_ShowDetails = () => SelectPanel(detailsHost);


            m_Canvas = new CharacterPoseCanvasBinding
            {
                name = "tree-view"
            };
            m_StateMachineSurface = new CharacterPoseCanvasBinding();
            m_StateMachineSurface.style.display = DisplayStyle.None;
            m_Details = new GraphAuthoringDetailsRegion
            {
                name = "pose-details"
            };
            m_LinkedPoseDetails = new VisualElement
            {
                name = "linked-pose-details"
            };
            m_LinkedPoseDetails.style.display = DisplayStyle.None;
            m_SelectionTuningHost = new ScrollView(
                ScrollViewMode.Vertical)
            {
                name = "pose-selection-tuning"
            };
            m_SelectionTuningHost.AddToClassList(
                "pose-selection-tuning");
            m_SelectionTuningHost.style.display = DisplayStyle.None;
            m_LinkedPoseWorkspace = new CharacterLinkedPoseAuthoringWorkspacePresenter(this);
            m_LinkedPoseWorkspace.Bind(m_LinkedPoseDetails);
            m_Navigator = new GraphAuthoringNavigatorPresenter();
            canvasHost.Add(m_Canvas);
            canvasHost.Add(m_StateMachineSurface);
            navigatorHost.Add(m_Navigator);
            detailsHost.Add(m_Details);
            detailsHost.Add(m_SelectionTuningHost);
            detailsHost.Add(m_LinkedPoseDetails);

            m_Title = rootVisualElement.Q<Label>("pose-document-title");
            m_Status = rootVisualElement.Q<Label>("pose-authoring-status");
            toolbar.Add(new Button(() => NodeCanvas.Editor.GraphEditor.FocusReadableGraph()) { text = "100%" });
            toolbar.Add(new Button(ValidateAuthoring) { text = "Validate" });
            toolbar.Add(new Button(SaveAuthoring) { text = "保存" });
            var compile = new Button(ValidateAuthoring) { text = "Compile" };
            compile.SetEnabled(m_Profile != null);
            toolbar.Add(compile);

            m_Canvas.NodeCreationRequested += ShowCreateMenu;
            m_Canvas.ChildSurfaceRequested += OpenChildSurface;
            m_StateMachineSurface.StateMachineNodeCreationRequested +=
                ShowStateMachineCreateMenu;
            m_SelectionBinding = new GraphAuthoringSelectionBinding(
                rootVisualElement,
                PublishSelection);
            m_UndoBinding = new GraphAuthoringUndoBinding(ReloadAfterUndoRedo);
            BindCurrentGraph(true);
        }

        public void Dispose()
        {
            if (!s_Reloading && s_Current == this)
                SessionState.EraseString(SessionKey + "Asset");
            NodeCanvas.Editor.GraphEditorUtility.onSelectionChanged -= PublishSelection;
            NodeCanvas.Editor.GraphEditor.onEditorNavigationChanged -= SaveWorkspace;
            NodeCanvas.Editor.GraphEditor.onEditorClosed -= Dispose;
            EditorApplication.projectChanged -= OnAuthoringAssetsChanged;
            if (m_Asset)
            {
                foreach (CharacterPoseCanvasGraph graph in m_Asset.EnumerateGraphs())
                    graph?.SetEditorAnimationVariables(null);
            }
            m_EditorAnimationVariables = null;
            m_Canvas?.Dispose();
            m_StateMachineSurface?.Dispose();
            rootVisualElement.RemoveFromHierarchy();
            if (s_Current == this)
                s_Current = null;
            m_UndoBinding?.Dispose();
            m_UndoBinding = null;
            m_SelectionBinding?.Dispose();
            m_SelectionBinding = null;
            if (m_Canvas != null)
            {
                m_Canvas.NodeCreationRequested -= ShowCreateMenu;
                m_Canvas.ChildSurfaceRequested -= OpenChildSurface;
            }
            if (m_StateMachineSurface != null)
            {
                m_StateMachineSurface.StateMachineNodeCreationRequested -= ShowStateMachineCreateMenu;
            }
        }

        void SetDocument(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definition)
        {
            m_Asset = asset;
            m_Profile = profile;
            m_Definition = definition;
            m_EditorAnimationVariables = CreateEditorAnimationVariables(profile);
            if (asset && asset.Graph != null)
                asset.Graph.SetEditorAnimationVariables(m_EditorAnimationVariables);
            ResetPoseTuningAuthoringState();
            if (asset && asset.Graph != null)
                m_CurrentGraphId = asset.Graph.GraphId.Value;
        }

        static CharacterAnimationVariableContract CreateEditorAnimationVariables(
            CharacterAnimationPresentationProfile profile)
        {
            if (!profile || !profile.EventGraph)
                return null;
            return new CharacterAnimationVariableContract(
                profile.EventGraph.BuildVariableContract());
        }

        public void FocusStatePlayer(
            CharacterPoseCanvasNode machine,
            CharacterPoseStateDefinition state,
            PoseNodeId sequenceNodeId)
        {
            if (machine?.Payload is not CharacterPoseStateMachineNodePayload machinePayload ||
                state == null || !sequenceNodeId.IsValid ||
                !machinePayload.StateMachine.States.Any(value => value.StateId == state.StateId))
                return;
            OpenGraph(state.PoseGraphId);
            m_Canvas?.FocusElement(new GraphAuthoringElementId(sequenceNodeId.Value));
        }

        public void FocusGraph(PoseGraphId graphId)
        {
            if (!graphId.IsValid || !m_Asset || !m_Asset.TryGetGraph(graphId, out _))
                return;
            OpenGraph(graphId);
        }

        internal static void OpenNodeChild(CharacterPoseCanvasNode node)
        {
            if (s_Current == null || s_Current.m_Document == null ||
                !s_Current.m_Document.Graph.Nodes.Contains(node))
                throw new InvalidOperationException("Open the Pose Graph with its authoring workspace to navigate child documents.");
            GraphAuthoringNodeProjection projection = s_Current.m_Document.Nodes.Single(value => value.NodeId.Value == node.NodeId.Value);
            GraphAuthoringCapabilityDescriptor capability = CharacterPoseNodeDefinitionModule.Shared.Require(node.Kind).Capability;
            if (capability.ChildSurfaces.Count != 0)
                s_Current.OpenChildSurface(projection, capability.ChildSurfaces[0]);
        }

        internal static void HandleGraphSelection(NodeCanvas.Framework.Graph graph)
        {
            CharacterPoseGraphWorkspace workspace = s_Current;
            if (workspace == null || workspace.m_Canvas == null || workspace.m_BindingPanel || workspace.m_PanelGraph == graph) return;
            if (graph is CharacterPoseCanvasGraph pose && workspace.m_Asset.TryGetGraph(pose.GraphId, out CharacterPoseCanvasGraph owned) && owned == pose)
                workspace.FocusGraph(pose.GraphId);
            else if (graph is CharacterPoseDocumentCanvas document)
            {
                if (document.StateMachineBinding?.Document is CharacterPoseStateMachineDocument machine)
                    workspace.OpenStateMachine(machine.Definition);
                else if (document.ProjectionBinding?.Document is CharacterPoseTransitionRuleDocument rule)
                    workspace.BindTransitionRule(rule.Machine, rule.TransitionId);
            }
            else workspace.Dispose();
        }

        void BindPanel(Action bind)
        {
            m_BindingPanel = true;
            try { bind(); }
            finally { m_BindingPanel = false; }
            if (m_PanelGraph == NodeCanvas.Editor.GraphEditor.currentGraph) PublishSelection();
        }

        void SaveAuthoring()
        {
            foreach (CharacterPoseCanvasGraph graph in m_Asset.EnumerateGraphs())
            {
                graph.SelfSerialize();
                AssetDatabase.SaveAssetIfDirty(graph);
            }
            AssetDatabase.SaveAssetIfDirty(m_Asset);
            if (m_Profile)
                AssetDatabase.SaveAssetIfDirty(m_Profile);
            CharacterPoseTuningAuthoringService.SaveReferencedOwners(m_Asset, m_Profile);
            RefreshPublishedStatus();
        }

        void BindCurrentGraph(bool resetPages = false) => BindPanel(() => BindCurrentGraphCore(resetPages));

        void BindCurrentGraphCore(bool resetPages)
        {
            if (m_Canvas == null || !m_Asset || m_Asset.Graph == null)
                return;
            PoseGraphId graphId = string.IsNullOrWhiteSpace(m_CurrentGraphId)
                ? m_Asset.Graph.GraphId
                : new PoseGraphId(m_CurrentGraphId);
            CharacterPoseCanvasGraph graph = m_Asset.RequireGraph(graphId);
            graph.SetEditorAnimationVariables(m_EditorAnimationVariables);
            m_RuleDocument = null;
            m_RuleMutation = null;
            m_Canvas.style.display = DisplayStyle.Flex;
            m_StateMachineSurface.style.display = DisplayStyle.None;
            m_Details.style.display = DisplayStyle.Flex;
            m_Owner = new CharacterPoseGraphAssetMutationOwner(m_Asset, m_Profile);
            string graphDisplayName = ResolveGraphDisplayName(graph);
            m_Document = new CharacterPoseCanvasGraphDocument(
                m_Owner,
                graph.GraphId.Value,
                ResolveRole(graph),
                graphDisplayName);
            m_Mutation = new CharacterPoseCanvasMutationAdapter();
            m_Mutation.ReadOnly =
                false;
            IGraphAuthoringDomainDiagnostics runtimeTrace = null;
            GraphAuthoringCapabilityCatalog catalog = CharacterPoseGraphCapabilityProjector.Catalog;
            m_Canvas.BindProjection(
                new GraphAuthoringProjectionCanvasBinding(
                m_Document,
                catalog,
                m_Mutation,
                new CharacterPoseCanvasConnectionPolicy(),
                new CharacterPoseCanvasGraphClipboardCodec(
                    m_Mutation)));
            m_DetailsDataSource = new CharacterPoseCanvasDetailsDataSource(
                runtimeTrace,
                m_Profile?.RigDefinition,
                m_Profile,
                null);
            m_Details.Bind(new GraphAuthoringDetailsBinding(
                m_Document,
                catalog,
                m_Mutation,
                m_DetailsDataSource,

                OpenDetailsCommand,
                true,
                authoringOnly: true));
            m_Details.style.display = DisplayStyle.Flex;
            m_LinkedPoseDetails.style.display = DisplayStyle.None;
            m_Navigator.Bind(m_Document, new NavigatorDataSource(this));
            m_Title.text = $"{m_Asset.name} / {graphDisplayName}";
            RefreshLinkedPoseWorkspaceStatus();
            m_LastContentRevision = graph.ContentRevision;
            graph.editorTitle = graphDisplayName;
            RefreshPublishedStatus();
            m_LastSelection = null;
            RefreshSelectionTuning(null);

            m_PanelGraph = m_Canvas.Graph;
            SaveWorkspace();
            if (!string.IsNullOrEmpty(m_LinkedPoseSelectionId))
                ShowLinkedPoseSelection(m_LinkedPoseSelectionId);
            RefreshSelectedDetails();
            if (resetPages)
            {
                rootVisualElement.schedule.Execute(() =>
                {
                    if (m_Canvas != null && !m_ShowingStateMachine)
                        NodeCanvas.Editor.GraphEditor.FocusReadableGraph();
                });
            }
        }

        GraphAuthoringDocumentRoleId ResolveRole(CharacterPoseCanvasGraph graph)
        {
            if (ReferenceEquals(graph, m_Asset.Graph))
                return CharacterPoseGraphAuthoringCapabilities.RootGraph;
            if (graph.Role != CharacterPoseAuthoringGraphRole.AnimGraph)
                return CharacterPoseGraphAuthoringCapabilities.GetRole(graph.Role);
            bool stateOwned = m_Asset.EnumerateGraphs()
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Any(value => value.StateMachine != null && value.StateMachine.States.Any(state => state.PoseGraphId == graph.GraphId));
            return stateOwned
                ? CharacterPoseGraphAuthoringCapabilities.StatePoseGraph
                : CharacterPoseGraphAuthoringCapabilities.Subgraph;
        }

        string ResolveGraphDisplayName(CharacterPoseCanvasGraph graph)
        {
            if (ReferenceEquals(graph, m_Asset.Graph))
                return "Root Pose Graph";
            if (graph.Role == CharacterPoseAuthoringGraphRole.AnimationLayer)
                return "Animation Layer";
            if (graph.Role == CharacterPoseAuthoringGraphRole.ControlRig)
                return "Control Rig";
            if (graph.Role == CharacterPoseAuthoringGraphRole.TransitionRule)
                return "Transition Rule";
            string[] stateNames = m_Asset.EnumerateStateMachines()
                .SelectMany(value => value.States)
                .Where(value => value.PoseGraphId == graph.GraphId)
                .Select(value => CharacterPoseAuthoringDisplayNames.ForIdentity(value.DisplayName))
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "Unnamed")
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (stateNames.Length == 1)
                return $"{stateNames[0]} Pose Graph";
            if (stateNames.Length > 1)
                return "Shared State Pose Graph";
            string[] subgraphOwners = m_Asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Where(value =>
                    value?.Payload is CharacterPoseSubgraphPayload payload &&
                    payload.Subgraph != null &&
                    payload.Subgraph.PoseGraphId == graph.GraphId)
                .Select(value => CharacterPoseAuthoringDisplayNames.ForIdentity(value.DisplayName))
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "Unnamed")
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (subgraphOwners.Length == 1)
                return $"{subgraphOwners[0]} Subgraph";
            if (subgraphOwners.Length > 1)
                return "Shared Pose Subgraph";
            CharacterPoseCanvasGraph[] graphs = m_Asset.EnumerateGraphs()
                .Where(value => value != null &&
                                !ReferenceEquals(value, m_Asset.Graph))
                .OrderBy(value => value.GraphId)
                .ToArray();
            int index = Array.IndexOf(graphs, graph);
            return $"Pose Graph {Math.Max(index, 0) + 1}";
        }

        void ShowCreateMenu(Vector2 screenPosition, IReadOnlyList<GraphAuthoringCapabilityDescriptor> capabilities)
        {
            var menu = new GenericMenu();
            foreach (GraphAuthoringCapabilityDescriptor capability in capabilities.OrderBy(value => value.Category).ThenBy(value => value.DisplayName))
            {
                if (CharacterPoseNodeDefinitionModule.Shared.TryGetCapability(
                        capability.CapabilityId.Value,
                        out CharacterPoseNodeDefinition definition) &&
                    definition.CanvasCreation == CharacterPoseCanvasCreationKind.BlackboardOnly)
                    continue;
                GraphAuthoringCapabilityDescriptor selected = capability;
                menu.AddItem(new GUIContent($"{selected.Category}/{selected.DisplayName}"), false, () => CreateNode(selected, screenPosition));
            }
            menu.DropDown(new Rect(screenPosition, Vector2.zero));
        }

        void CreateNode(GraphAuthoringCapabilityDescriptor capability, Vector2 screenPosition)
        {
            if (m_ShowingTransitionRule)
            {
                if (!CharacterPoseGraphAuthoringCapabilities
                        .TryGetRuleOperationKind(
                            capability.CapabilityId,
                            out PoseTransitionRuleOperationKind
                                ruleKind))
                {
                    throw new InvalidOperationException(
                        $"Capability '{capability.CapabilityId}' is not a Pose Transition Rule operation.");
                }
                CharacterPoseTransitionRuleOperation operation =
                    CharacterPoseTransitionRuleOperationFactory
                        .Create(ruleKind);
                Vector2 rulePosition =
                    screenPosition;
                m_Canvas.CreateNode(
                    capability.CapabilityId,
                    operation,
                    rulePosition);
                m_Status.text =
                    "Transition Rule changed · published Projection is Stale until explicit Build.";
                return;
            }
            CharacterPoseNodeDefinition definition =
                CharacterPoseNodeDefinitionModule.Shared.RequireCapability(
                    capability.CapabilityId.Value);
            if (definition.CanvasCreation ==
                CharacterPoseCanvasCreationKind.DedicatedSurface)
            {
                ShowLinkedPoseSelection("linked-root");
                m_Status.text = "Linked Pose Call is created from the typed Group/Entry authoring page.";
                return;
            }
            if (definition.CanvasCreation == CharacterPoseCanvasCreationKind.BlackboardOnly)
            {
                m_Status.text = "Pose 参数 Get 节点必须从 Blackboard 变量拖入。";
                return;
            }
            CharacterPoseNodePayload payload =
                definition.CreateDefaultPayload();
            var node = new CharacterPoseCanvasNode(new PoseNodeId(Guid.NewGuid().ToString("N")), capability.DisplayName, payload);
            Vector2 graphPosition = screenPosition;
            m_Canvas.CreateNode(capability.CapabilityId, node, graphPosition);
            m_Status.text =
                "Authoring changed · published Projection is Stale until explicit Build.";
            RefreshSelectedDetails();
        }

        void PublishSelection()
        {
            if (m_BindingPanel) return;
            if (m_PanelGraph != NodeCanvas.Editor.GraphEditor.currentGraph)
            {
                HandleGraphSelection(NodeCanvas.Editor.GraphEditor.currentGraph);
                return;
            }
            if (m_Canvas == null || m_Details == null)
                return;
            if (m_ShowingStateMachine)
            {
                PublishStateMachineSelection();
                return;
            }
            string revision = m_ShowingTransitionRule
                ? m_RuleDocument?.ContentRevision ??
                  string.Empty
                : m_Document?.ContentRevision ?? string.Empty;
            bool tuningOnly = IsTuningOnlyAuthoringChange();
            if (!tuningOnly && m_TuningOnlyAuthoringChange)
                ClearPoseTuningAuthoringChange();
            if (!string.Equals(
                    revision,
                    m_LastContentRevision,
                    StringComparison.Ordinal))
            {
                m_LastContentRevision = revision;
                m_Status.text = tuningOnly
                    ? "Unpublished Parameter · published Projection remains active."
                    : "Authoring changed · published Projection is Stale until explicit Build.";
                if (m_ShowingTransitionRule) m_Canvas.PopulateProjection();
                RefreshSelectedDetails();
            }
            IReadOnlyList<GraphAuthoringSelection> selected = CharacterPoseCanvasCommands.Selection(NodeCanvas.Editor.GraphEditor.currentGraph);
            GraphAuthoringSelection? current = selected.Count == 1 ? selected[0] : null;
            if (Nullable.Equals(current, m_LastSelection) &&
                true)                return;
            m_LastSelection = current;
            if (current.HasValue)
            {
                m_ShowDetails?.Invoke();
                if (!m_ShowingTransitionRule && m_Document != null &&
                    current.Value.Kind == GraphAuthoringSelectionKind.Node &&
                    m_Document.Graph.Nodes.FirstOrDefault(value =>
                        value.NodeId.Value == current.Value.ElementId.Value) is
                        CharacterPoseCanvasNode selectedNode &&
                    CharacterPoseNodeDefinitionModule.Shared
                        .Require(selectedNode.Kind).CanvasCreation ==
                    CharacterPoseCanvasCreationKind.DedicatedSurface)
                {
                    ShowLinkedPoseSelection(
                        $"linked-call:{m_Document.DocumentId}:{current.Value.ElementId.Value}");
                    return;
                }
                HideLinkedPoseSelection();
                m_Details.Inspect(current.Value);
                RefreshSelectionTuning(current);
            }
            else
            {
                if (!m_LinkedPoseWorkspace?.IsShowing ?? true)
                    m_Details.ClearSelection();
                RefreshSelectionTuning(null);
            }
        }

        internal void RefreshSelectedDetails()
        {
            if (m_BindingPanel || m_PanelGraph != NodeCanvas.Editor.GraphEditor.currentGraph) return;
            if (m_ShowingStateMachine) { PublishStateMachineSelection(true); return; }
            if (m_Canvas == null || m_Details == null)
                return;
            IReadOnlyList<GraphAuthoringSelection> selection =
                m_Canvas.GetStableSelection();
            if (selection.Count == 1)
            {
                m_Details.Inspect(selection[0]);
                RefreshSelectionTuning(selection[0]);
            }
        }

        void RefreshSelectionTuning(
            GraphAuthoringSelection? selection)
        {
            bool hasInlineTuning = false;
            selection = null;
            m_SelectionTuningHost.Clear();
            foreach (VisualElement row in m_Details.Query<VisualElement>(
                         className:
                         "graph-authoring-details-field-row").ToList())
            {
                Label policy = row.Q<Label>(
                    className:
                    "graph-authoring-details-field-policy");
                bool tunable = policy != null &&
                               (string.Equals(
                                    policy.text,
                                    "Live Now",
                                    StringComparison.Ordinal) ||
                                string.Equals(
                                    policy.text,
                                    "Next Activation",
                                    StringComparison.Ordinal));
                row.style.display = hasInlineTuning && tunable
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
            foreach (Foldout section in
                     m_Details.Query<Foldout>().ToList())
            {
                if (string.Equals(
                        section.text,
                        "Applied Values",
                        StringComparison.Ordinal))
                    section.style.display = hasInlineTuning
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
            }
        }

        internal void ShowSelectionTuningError(string message)
        {
            RefreshSelectedDetails();
            m_SelectionTuningHost.style.display = DisplayStyle.Flex;
            m_SelectionTuningHost.Insert(0, new HelpBox(message, HelpBoxMessageType.Error));
        }

        void OpenGraph(PoseGraphId graphId)
        {
            m_Asset.RequireGraph(graphId);
            m_CurrentGraphId = graphId.Value;
            BindCurrentGraph(true);
        }

        void OpenDetailsCommand(
            GraphAuthoringDetailsCommandRequest request)
        {
            GraphAuthoringElementId nodeId = request.ElementId;
            if (TryOpenPoseSource(request, nodeId))
                return;
            if (TryOpenFullBodyIkProfile(request, nodeId))
                return;
            if (request.Kind !=
                GraphAuthoringMutationKind.OpenChildSurface)
                throw new InvalidOperationException(
                    $"Details command '{request.Kind}' is not a navigation command.");
            GraphAuthoringNodeProjection node =
                m_Document.Nodes.Single(value =>
                    value.NodeId.Equals(nodeId));
            GraphAuthoringCapabilityDescriptor capability =
                CharacterPoseGraphCapabilityProjector.Catalog.Require(
                    node.CapabilityId,
                    m_Document.DomainId,
                    m_Document.DocumentRoleId);
            GraphAuthoringChildSurfaceDescriptor child =
                capability.ChildSurfaces.Single(value =>
                    value.CommandId.Equals(request.CommandId));
            CharacterPoseCanvasNode caller = m_Document.Graph.RequireNode(new PoseNodeId(node.NodeId.Value));
            NodeCanvas.Editor.GraphEditor.OpenEditorChild(caller, () => OpenChildSurface(node, child));
        }

        bool TryOpenFullBodyIkProfile(
            GraphAuthoringDetailsCommandRequest request,
            GraphAuthoringElementId nodeId)
        {
            if (request.Kind != GraphAuthoringMutationKind.ExecuteCommand ||
                !request.CommandId.Equals(
                    CharacterPoseGraphAuthoringCapabilities.OpenFullBodyIkProfile))
                return false;
            CharacterPoseCanvasNode typed =
                m_Document.Graph.Nodes.Single(value =>
                    value.NodeId.Value == nodeId.Value);
            _ = typed.Payload as CharacterFullBodyIkPosePayload ??
                throw new InvalidOperationException(
                    $"Pose node '{typed.NodeId}' is not a Full Body IK node.");
            CharacterFullBodyIkProfile profile = m_Profile?.FullBodyIkProfile;
            if (!profile)
                throw new InvalidOperationException(
                    "Animation Presentation Profile has no Full Body IK Profile.");
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            AssetDatabase.OpenAsset(profile);
            return true;
        }

        bool TryOpenPoseSource(
            GraphAuthoringDetailsCommandRequest request,
            GraphAuthoringElementId nodeId)
        {
            if (request.Kind != GraphAuthoringMutationKind.ExecuteCommand)
                return false;

            bool pingSource = request.CommandId.Equals(
                CharacterPoseGraphAuthoringCapabilities.PingPoseSource);
            bool openSource = request.CommandId.Equals(
                CharacterPoseGraphAuthoringCapabilities.OpenPoseSource);
            bool openProfile = request.CommandId.Equals(
                CharacterPoseGraphAuthoringCapabilities.OpenPoseSourceProfile);
            if (!pingSource && !openSource && !openProfile)
                return false;

            if (m_Profile == null)
                throw new InvalidOperationException(
                    "Pose Source command requires an exact Presentation Profile context.");
            if (openProfile)
            {
                Selection.activeObject = m_Profile;
                EditorGUIUtility.PingObject(m_Profile);
                AssetDatabase.OpenAsset(m_Profile);
                return true;
            }

            CharacterPoseCanvasNode typed =
                m_Document.Graph.Nodes.Single(value =>
                    value.NodeId.Value == nodeId.Value);
            CharacterPresentationPoseSourceSlot slot =
                typed.PresentationPoseSourceSlot;
            if (!slot)
                throw new InvalidOperationException(
                    $"Pose node '{typed.NodeId}' has no Source Slot.");
            CharacterPresentationPoseSourceBinding binding =
                m_Profile.FindPoseSourceBinding(slot);
            UnityEngine.Object source = binding?.SourceAsset;
            if (!binding || !source)
                throw new InvalidOperationException(
                    $"Pose Source Slot '{slot.name}' has no valid Binding in Profile '{m_Profile.name}'.");

            if (openSource && binding is CharacterClipPoseSourceBinding clipBinding && clipBinding.Clip)
                CharacterAnimationClipAuthoringService.Open(new CharacterAnimationClipOpenRequest(
                    m_Definition,
                    m_Profile,
                    clipBinding.Clip,
                    null));
            else if (openSource && binding is CharacterBlendSpacePoseSourceBinding blendSpace && blendSpace.BlendSpace)
                CharacterAnimationBlendSpaceEditorWindow.Open(blendSpace.BlendSpace);
            else
            {
                Selection.activeObject = source;
                EditorGUIUtility.PingObject(source);
            }
            return true;
        }

        void OpenChildSurface(
            GraphAuthoringNodeProjection node,
            GraphAuthoringChildSurfaceDescriptor child)
        {
            CharacterPoseCanvasNode typed =
                m_Document.Graph.Nodes.Single(value =>
                    value.NodeId.Value == node.NodeId.Value);
            if (child.DocumentRoleId.Equals(
                    CharacterPoseGraphAuthoringCapabilities.StateMachine))
            {
                CharacterPoseStateMachineNodePayload payload =
                    typed.Payload as
                        CharacterPoseStateMachineNodePayload ??
                    throw new InvalidOperationException(
                        $"Pose node '{typed.NodeId}' does not own a StateMachine.");
                OpenStateMachine(payload.StateMachine);
                return;
            }
            if (typed.Payload is CharacterPoseSubgraphPayload subgraph &&
                subgraph.Subgraph != null &&
                subgraph.Subgraph.PoseGraphId.IsValid)
            {
                CharacterPoseCanvasGraph graph =
                    m_Asset.RequireGraph(
                        subgraph.Subgraph.PoseGraphId);
                m_CurrentGraphId = graph.GraphId.Value;
                BindCurrentGraph(false);
                return;
            }
            throw new InvalidOperationException(
                $"Pose node '{typed.NodeId}' does not own child surface '{child.CommandId}'.");
        }

        void OpenStateMachine(CharacterPoseStateMachineDefinition machine) => BindPanel(() => OpenStateMachineCore(machine));

        void OpenStateMachineCore(CharacterPoseStateMachineDefinition machine)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            BindMachineOwnerContext(machine);
            m_RuleDocument = null;
            m_RuleMutation = null;
            m_Canvas.style.display = DisplayStyle.None;
            m_StateMachineSurface.style.display = DisplayStyle.Flex;
            m_Details.style.display = DisplayStyle.Flex;
            m_StateMachineDocument =
                new CharacterPoseStateMachineDocument(
                    m_Asset,
                    machine);
            m_StateMachineMutation =
                new CharacterPoseStateMachineMutationAdapter()
                {
                    ReadOnly = false
                };
            var policy = new CharacterPoseStateMachinePolicy(
                OpenStateGraph,
                OpenTransitionRule,
                CharacterPoseTransitionCreationDialog.Show);
            var binding = new GraphAuthoringStateMachineBinding(
                m_StateMachineDocument,
                CharacterPoseGraphCapabilityProjector.Catalog,
                m_StateMachineMutation,
                policy);
            m_StateMachineSurface.BindStateMachine(binding);
            m_Details.BindStateMachine(
                binding,
                new CharacterPoseStateMachineDetailsDataSource(),
                null, authoringOnly: true);
            m_Navigator.Bind(
                m_StateMachineDocument,
                new NavigatorDataSource(this));
            m_Title.text =
                $"{m_Asset.name} / " +
                CharacterPoseAuthoringDisplayNames.StateMachine(machine);
            RefreshPublishedStatus();
            m_LastContentRevision = machine.ContentRevision;
            m_LastSelection = null;
            RefreshSelectionTuning(null);
            m_PanelGraph = m_StateMachineSurface.Graph;
            SaveWorkspace();
            RefreshSelectedDetails();
        }

        void BindMachineOwnerContext(CharacterPoseStateMachineDefinition machine)
        {
            var owner = CharacterPoseGraphAssetMutationOwner.ResolveStateMachineOwner(m_Asset, machine.StateMachineId);
            CharacterPoseCanvasGraph graph = m_Asset.RequireGraph(owner.Item1);
            m_CurrentGraphId = graph.GraphId.Value;
            m_Owner = new CharacterPoseGraphAssetMutationOwner(m_Asset, m_Profile);
            m_Document = new CharacterPoseCanvasGraphDocument(m_Owner, graph.GraphId.Value, ResolveRole(graph), ResolveGraphDisplayName(graph));
        }

        void OpenStateGraph(CharacterPoseStateDefinition state)
        {
            CharacterPoseCanvasGraph graph =
                m_Asset.RequireGraph(state.PoseGraphId);
            var owner = CharacterPoseGraphAssetMutationOwner.ResolveStateMachineOwner(m_Asset, m_StateMachineDocument.Definition.StateMachineId);

            m_CurrentGraphId = graph.GraphId.Value;
            BindCurrentGraph(false);
        }

        void OpenTransitionRule(CharacterPoseStateTransition transition)
        {
            var edge = m_StateMachineSurface.Graph.allNodes
                .SelectMany(node => node.outConnections)
                .OfType<CharacterPoseDocumentCanvasConnection>()
                .Single(value => value.ElementId.Value == transition.TransitionId.Value);
            CharacterPoseStateMachineDefinition machine = m_StateMachineDocument.Definition;
            NodeCanvas.Editor.GraphEditor.OpenEditorChild(edge,
                () => BindTransitionRule(machine, transition.TransitionId));
        }

        void BindTransitionRule(CharacterPoseStateMachineDefinition machine, PoseStateTransitionId transitionId) =>
            BindPanel(() => BindTransitionRuleCore(machine, transitionId));

        void BindTransitionRuleCore(CharacterPoseStateMachineDefinition machine, PoseStateTransitionId transitionId)
        {
            BindMachineOwnerContext(machine);
            m_Canvas.style.display = DisplayStyle.Flex;
            m_StateMachineSurface.style.display =
                DisplayStyle.None;
            m_Details.style.display = DisplayStyle.Flex;
            m_RuleDocument =
                new CharacterPoseTransitionRuleDocument(
                    m_Asset,
                    machine,
                    transitionId,
                    m_Profile && m_Profile.EventGraph
                        ? new CharacterAnimationVariableContract(
                            m_Profile.EventGraph.BuildVariableContract())
                        : null);
            m_RuleMutation =
                new CharacterPoseTransitionRuleMutationAdapter
                {
                    ReadOnly = false
                };
            m_Canvas.BindProjection(
                new GraphAuthoringProjectionCanvasBinding(
                m_RuleDocument,
                CharacterPoseGraphCapabilityProjector.Catalog,
                m_RuleMutation,
                new CharacterPoseTransitionRuleConnectionPolicy(),
                persistsLayout: false));
            m_Details.Bind(new GraphAuthoringDetailsBinding(
                m_RuleDocument,
                CharacterPoseGraphCapabilityProjector.Catalog,
                m_RuleMutation,
                new CharacterPoseTransitionRuleDetailsDataSource(),
                ExecuteRuleDetailsCommand, authoringOnly: true));
            m_Navigator.Bind(
                m_RuleDocument,
                new NavigatorDataSource(this));
            m_Title.text =
                $"{m_Asset.name} / Transition Rule / " +
                m_RuleDocument.DisplayName;
            RefreshPublishedStatus();
            m_LastContentRevision =
                m_RuleDocument.ContentRevision;
            m_LastSelection = null;
            m_PanelGraph = m_Canvas.Graph;
            SaveWorkspace();
        }

        void ExecuteRuleDetailsCommand(
            GraphAuthoringDetailsCommandRequest request)
        {
            if (request.Kind !=
                GraphAuthoringMutationKind.ExecuteCommand)
            {
                throw new InvalidOperationException(
                    $"Transition Rule Details command '{request.Kind}' is not supported.");
            }
            CharacterPoseStateMachineDefinition machine =
                m_RuleDocument.Machine;
            PoseStateTransitionId transitionId =
                m_RuleDocument.TransitionId;
            m_RuleMutation.Apply(
                m_RuleDocument,
                new GraphAuthoringMutationRequest(
                    request.Kind,
                    request.ElementId,
                    commandId: request.CommandId,
                    value: request.Value));
            BindTransitionRule(
                machine,
                transitionId);
        }

        void ShowStateMachineCreateMenu(Vector2 screenPosition)
        {
            if (m_StateMachineMutation == null ||
                m_StateMachineMutation.ReadOnly)
                return;
            var menu = new GenericMenu();
            Vector2 graphPosition =
                screenPosition;
            menu.AddItem(
                new GUIContent("State"),
                false,
                () => CreateState(graphPosition));
            IReadOnlyList<GraphAuthoringSelection> selection =
                CharacterPoseCanvasCommands.Selection(NodeCanvas.Editor.GraphEditor.currentGraph);
            if (selection.Any(value =>
                    value.Kind ==
                    GraphAuthoringSelectionKind.State))
            {
                menu.AddItem(
                    new GUIContent("State Alias from Selection"),
                    false,
                    () => CreateStateAlias(graphPosition));
            }
            else
            {
                menu.AddDisabledItem(
                    new GUIContent(
                        "State Alias from Selection"));
            }
            menu.DropDown(new Rect(screenPosition, Vector2.zero));
        }

        void CreateState(Vector2 position)
        {
            m_StateMachineMutation.CreateState(m_StateMachineDocument, position);
            OpenStateMachine(m_StateMachineDocument.Definition);
        }

        void CreateStateAlias(Vector2 position)
        {
            m_StateMachineMutation.CreateStateAlias(
                m_StateMachineDocument, m_StateMachineSurface.GetStableSelection(), position);
            OpenStateMachine(m_StateMachineDocument.Definition);
        }

        void PublishStateMachineSelection(bool refresh = false)
        {
            string revision =
                m_StateMachineDocument?.ContentRevision ??
                string.Empty;
            if (!string.Equals(
                    revision,
                    m_LastContentRevision,
                    StringComparison.Ordinal))
            {
                m_LastContentRevision = revision;
                m_Status.text =
                    "Authoring changed · published Projection is Stale until explicit Build.";
                m_StateMachineSurface.PopulateStateMachine();
                RefreshSelectedDetails();
            }
            IReadOnlyList<GraphAuthoringSelection> selection =
                CharacterPoseCanvasCommands.Selection(NodeCanvas.Editor.GraphEditor.currentGraph);
            GraphAuthoringSelection? current =
                selection.Count == 1 ? selection[0] : null;
            if (!refresh && Nullable.Equals(current, m_LastSelection))
                return;
            m_LastSelection = current;
            if (current.HasValue) m_ShowDetails?.Invoke();
            if (current.HasValue &&
                current.Value.Kind == GraphAuthoringSelectionKind.State)
            {
                m_Details.InspectState(current.Value.ElementId);
                RefreshSelectionTuning(current);
                return;
            }
            if (current.HasValue &&
                current.Value.Kind ==
                GraphAuthoringSelectionKind.Transition)
            {
                m_Details.InspectTransition(
                    current.Value.ElementId);
                RefreshSelectionTuning(current);
                return;
            }
            m_Details.ClearStateMachineSelection();
            RefreshSelectionTuning(null);
        }





        (
            CharacterPoseStateMachineDefinition Machine,
            CharacterPoseStateTransition Transition)
            FindTransitionRuleOwner(string ruleGraphId)
        {
            var matches = m_Asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Select(value => value.StateMachine)
                .Where(value => value != null)
                .SelectMany(machine =>
                    machine.Transitions.Select(transition =>
                        (Machine: machine, Transition: transition)))
                .Where(value =>
                    string.Equals(
                        value.Transition.Rule.GraphId.Value,
                        ruleGraphId,
                        StringComparison.Ordinal))
                .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Transition Rule graph '{ruleGraphId}' must have exactly one owning Transition.");
        }

        void OpenTransitionRuleFromNavigator(
            string ruleGraphId)
        {
            (
                CharacterPoseStateMachineDefinition machine,
                CharacterPoseStateTransition transition) =
                FindTransitionRuleOwner(ruleGraphId);
            CharacterPoseCanvasGraph root = m_Asset.Graph ??
                throw new InvalidOperationException(
                    "Presentation Pose Graph root is missing.");



            BindTransitionRule(
                machine,
                transition.TransitionId);
        }



        void CompilePoseProjection()
        {
            if (!m_Profile)
            {
                m_Status.text = "Compile unavailable: no Animation Presentation Profile context.";
                return;
            }
            if (!ValidateAuthoringAndLocate())
                return;
            m_Status.text = "Pose graph data compile completed.";
        }

        void ValidateAuthoring()
        {
            ValidateAuthoringAndLocate();
        }

        bool ValidateAuthoringAndLocate()
        {
            if (!m_Asset)
                return false;
            ClearValidationHighlights();
            IReadOnlyList<string> capabilityErrors =
                CharacterPoseGraphCapabilityValidator.Validate(m_Asset);
            IReadOnlyList<CharacterPoseParameterDeclaration>
                animationInputParameters = m_Profile
                    ? CharacterAnimationInputContract.Create(m_Profile).Parameters
                    : null;
            CharacterPoseGraphValidationReport report =
            CharacterPoseTopologyValidator.Validate(
                    m_Asset,
                    m_Profile ? m_Profile.RigDefinition : null,
                    CharacterPoseAuthoringPortProjection.Get,
                    animationInputParameters: animationInputParameters);
            int issueCount = capabilityErrors.Count + report.Issues.Count;
            if (TryFindStateMachineValidationIssue(
                    out CharacterPoseCanvasGraph ownerGraph,
                    out CharacterPoseCanvasNode ownerNode,
                    out CharacterPoseStateMachineDefinition machine,
                    out CharacterPoseStateMachineValidationIssue
                        stateMachineIssue))
            {
                LocateStateMachineValidationIssue(
                    ownerGraph,
                    ownerNode,
                    machine,
                    stateMachineIssue);
                string target = string.IsNullOrEmpty(
                    stateMachineIssue.ElementId)
                    ? stateMachineIssue.TargetKind.ToString()
                    : $"{stateMachineIssue.TargetKind} " +
                      stateMachineIssue.ElementId;
                m_Status.text =
                    $"{stateMachineIssue.Code} · {stateMachineIssue.Message} · {target} · {Math.Max(1, issueCount)} issue(s)";
                return false;
            }
            if (issueCount == 0)
            {
                m_Status.text = "Authoring valid";
                return true;
            }
            if (report.Issues.Count > 0)
            {
                CharacterPoseGraphValidationIssue issue = report.Issues[0];
                LocateValidationIssue(issue);
                string port = issue.PortId.IsValid
                    ? $" · Port {issue.PortId.Value}"
                    : string.Empty;
                m_Status.text =
                    $"{issue.Code} · {issue.Message}{port} · {issueCount} issue(s)";
            }
            else
            {
                m_Status.text =
                    $"{capabilityErrors[0]} · {issueCount} issue(s)";
            }
            return false;
        }

        void LocateValidationIssue(
            CharacterPoseGraphValidationIssue issue)
        {
            PoseGraphId graphId =
                string.IsNullOrWhiteSpace(issue.GraphId)
                    ? default
                    : new PoseGraphId(issue.GraphId);
            LocateValidationElement(
                graphId,
                issue.NodeId,
                issue.PortId);
        }

        bool TryFindStateMachineValidationIssue(
            out CharacterPoseCanvasGraph ownerGraph,
            out CharacterPoseCanvasNode ownerNode,
            out CharacterPoseStateMachineDefinition machine,
            out CharacterPoseStateMachineValidationIssue issue)
        {
            foreach (CharacterPoseCanvasGraph graph in
                     m_Asset.EnumerateGraphs())
            {
                if (graph == null)
                    continue;
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node?.Payload is not
                        CharacterPoseStateMachineNodePayload payload)
                        continue;
                    CharacterPoseStateMachineValidationIssue?
                        candidate =
                            CharacterPoseStateMachineAuthoringValidator
                                .FindFirstIssue(
                                    payload.StateMachine,
                                    m_Asset.RequireGraph);
                    if (!candidate.HasValue)
                        continue;
                    ownerGraph = graph;
                    ownerNode = node;
                    machine = payload.StateMachine;
                    issue = candidate.Value;
                    return true;
                }
            }
            ownerGraph = null;
            ownerNode = null;
            machine = null;
            issue = default;
            return false;
        }

        void LocateStateMachineValidationIssue(
            CharacterPoseCanvasGraph ownerGraph,
            CharacterPoseCanvasNode ownerNode,
            CharacterPoseStateMachineDefinition machine,
            CharacterPoseStateMachineValidationIssue issue)
        {
            if (machine == null ||
                issue.TargetKind ==
                CharacterPoseStateMachineValidationTargetKind
                    .StateMachine ||
                string.IsNullOrEmpty(issue.ElementId))
            {
                LocateValidationElement(
                    ownerGraph.GraphId,
                    ownerNode.NodeId);
                return;
            }
            OpenStateMachine(machine);
            GraphAuthoringElementId elementId =
                new GraphAuthoringElementId(issue.ElementId);
            rootVisualElement.schedule.Execute(() =>
            {
                m_StateMachineSurface?.FocusElement(elementId);
                AddValidationHighlight(
                    m_StateMachineSurface,
                    elementId);
            });
        }

        void LocateValidationElement(
            PoseGraphId graphId,
            PoseNodeId nodeId,
            PosePortId portId = default)
        {
            if (graphId.IsValid &&
                m_Asset.TryGetGraph(graphId, out _))
                OpenGraph(graphId);
            if (!nodeId.IsValid)
                return;
            string portName = ResolveValidationPortName(
                graphId,
                nodeId,
                portId);
            GraphAuthoringElementId elementId =
                new GraphAuthoringElementId(nodeId.Value);
            rootVisualElement.schedule.Execute(() =>
            {
                m_Canvas?.FocusElement(elementId);
                AddValidationHighlight(
                    m_Canvas,
                    elementId,
                    portName);
            });
        }

        string ResolveValidationPortName(
            PoseGraphId graphId,
            PoseNodeId nodeId,
            PosePortId portId)
        {
            if (!graphId.IsValid || !nodeId.IsValid ||
                !portId.IsValid ||
                !m_Asset.TryGetGraph(
                    graphId,
                    out CharacterPoseCanvasGraph graph))
                return string.Empty;
            CharacterPoseCanvasNode node = graph.Nodes
                .SingleOrDefault(value =>
                    value != null && value.NodeId == nodeId);
            if (node == null)
                return string.Empty;
            return CharacterPoseAuthoringPortProjection.Get(node)
                       .SingleOrDefault(value =>
                           value.PortId.Equals(portId))
                       ?.Name ?? string.Empty;
        }

        static void AddValidationHighlight(CharacterPoseCanvasBinding canvas, GraphAuthoringElementId elementId, string portName = "") =>
            canvas?.Highlight(elementId, portName);

        void ClearValidationHighlights()
        {
            m_Canvas?.ClearHighlights();
            m_StateMachineSurface?.ClearHighlights();
        }

        void Reload()
        {
            if (!m_Asset)
                return;
            GraphAuthoringSelection[] selection = (m_ShowingStateMachine ? m_StateMachineSurface : m_Canvas).GetStableSelection().ToArray();
            if (m_ShowingTransitionRule &&
                m_RuleDocument != null)
            {
                BindTransitionRule(
                    m_RuleDocument.Machine,
                    m_RuleDocument.TransitionId);
            }
            else if (m_ShowingStateMachine &&
                m_StateMachineDocument != null)
            {
                OpenStateMachine(
                    m_StateMachineDocument.Definition);
            }
            else
            {
                BindCurrentGraph(false);
            }
            (m_ShowingStateMachine ? m_StateMachineSurface : m_Canvas).RestoreSelection(selection);
        }

        void OnAuthoringAssetsChanged()
        {
            if (m_BindingPanel) return;
            if (m_PanelGraph != NodeCanvas.Editor.GraphEditor.currentGraph)
            {
                HandleGraphSelection(NodeCanvas.Editor.GraphEditor.currentGraph);
                return;
            }
            if (!m_Asset)
                return;
            string revision = m_ShowingTransitionRule ? m_RuleDocument.ContentRevision
                : m_ShowingStateMachine ? m_StateMachineDocument.ContentRevision
                : m_Asset.RequireGraph(new PoseGraphId(m_CurrentGraphId)).ContentRevision;
            if (revision != m_LastContentRevision || !m_ShowingStateMachine && !m_ShowingTransitionRule &&
                m_Canvas.Graph != m_Asset.RequireGraph(new PoseGraphId(m_CurrentGraphId)))
                Reload();
            else if (m_ShowingStateMachine)
                m_StateMachineSurface.PopulateStateMachine();
        }

        void ReloadAfterUndoRedo()
        {
            Reload();
        }

        void RefreshPublishedStatus()
        {
            if (m_Status != null)
                m_Status.text = ValidateAuthoringQuietly();
        }

        internal string CurrentPublishedStatus() => ValidateAuthoringQuietly();

        string ValidateAuthoringQuietly()
        {
            if (!m_Asset)
                return "Unavailable: Pose Graph asset is missing.";
            IReadOnlyList<string> errors = CharacterPoseGraphCapabilityValidator.Validate(m_Asset);
            return errors.Count == 0 ? "Authoring valid" : errors[0];
        }

        internal void FocusNode(PoseNodeId nodeId) =>
            m_Canvas?.FocusElement(
                new GraphAuthoringElementId(nodeId.Value));

        internal void FocusNode(
            PoseGraphId graphId,
            PoseNodeId nodeId)
        {
            if (!graphId.IsValid || !nodeId.IsValid)
                return;
            OpenGraph(graphId);
            FocusNode(nodeId);
        }

        internal void ShowLinkedPoseAsset(UnityEngine.Object target)
        {
            if (m_Profile == null || target == null)
                return;
            string selectionId = target switch
            {
                CharacterAnimationPresentationProfile => "linked-root",
                CharacterLinkedPoseInterfaceAsset linkedInterface =>
                    "linked-interface:" + linkedInterface.InterfaceId.Value,
                CharacterLinkedPoseImplementationAsset implementation =>
                    "linked-implementation:" + implementation.ImplementationId.Value,
                CharacterLinkedPoseSelectorBindingAsset selector =>
                    "linked-selector:" + selector.SelectorId.Value,
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(selectionId))
                ShowLinkedPoseSelection(selectionId);
        }

        internal void ShowLinkedPoseSelection(string selectionId)
        {
            if (m_LinkedPoseWorkspace == null || string.IsNullOrWhiteSpace(selectionId))
                return;
            m_LinkedPoseSelectionId = selectionId;
            m_Details.style.display = DisplayStyle.None;
            m_LinkedPoseDetails.style.display = DisplayStyle.Flex;
            if (m_SelectionTuningHost != null)
                m_SelectionTuningHost.style.display = DisplayStyle.None;
            m_LinkedPoseWorkspace.Show(selectionId);
        }

        internal void HideLinkedPoseSelection()
        {
            m_LinkedPoseSelectionId = string.Empty;
            m_LinkedPoseWorkspace?.Hide();
            if (m_Details != null)
                m_Details.style.display = DisplayStyle.Flex;
            if (m_LinkedPoseDetails != null)
                m_LinkedPoseDetails.style.display = DisplayStyle.None;
            if (m_LastSelection.HasValue)
                RefreshSelectionTuning(m_LastSelection);
        }

        internal void ReloadLinkedPoseWorkspace()
        {
            MarkLinkedPoseChanged();
            Reload();
        }

        internal void MarkLinkedPoseChanged()
        {
            RefreshLinkedPoseWorkspaceStatus();
            m_Status.text = $"Linked Pose · {m_LinkedPoseWorkspaceStatus}";
            RefreshSelectedDetails();
        }

        internal void RefreshLinkedPoseWorkspaceStatus()
        {
            if (IsLinkedPoseReadOnly)
            {
                m_LinkedPoseWorkspaceStatus = "Live";
                return;
            }
            if (m_Profile == null || !m_Asset || m_Asset.Graph == null)
            {
                m_LinkedPoseWorkspaceStatus = "Unavailable";
                return;
            }
            bool dirty = EditorUtility.IsDirty(m_Profile) ||
                         CharacterLinkedPoseAuthoringService.EnumerateInterfaces(m_Profile)
                             .Any(EditorUtility.IsDirty);
            bool invalid = false;
            try
            {
                foreach (CharacterLinkedPoseGroupBinding group in m_Profile.LinkedPoseGroups)
                {
                    if (group == null)
                    {
                        invalid = true;
                        break;
                    }
                    group.RequireValid();
                }
                foreach (CharacterLinkedPoseImplementationAsset implementation in m_Profile.LinkedPoseImplementations)
                {
                    if (!implementation)
                    {
                        invalid = true;
                        break;
                    }
                    implementation.RequireValid();
                }
            }
            catch
            {
                invalid = true;
            }
            if (invalid)
            {
                m_LinkedPoseWorkspaceStatus = dirty ? "Dirty · Invalid" : "Invalid";
                return;
            }
            m_LinkedPoseWorkspaceStatus = dirty ? "Dirty" : "Ready";
        }

        internal void FocusLinkedPoseEntry(
            CharacterPresentationPoseGraphAsset graphOwner,
            PoseGraphId graphId)
        {
            if (!graphOwner || !graphId.IsValid)
                return;
            if (graphOwner == m_Asset)
            {
                OpenGraph(graphId);
                return;
            }
            CharacterPoseGraphWorkspace window =
                CharacterPoseGraphWorkspace.Open(
                    graphOwner,
                    m_Profile,
                    m_Definition);
            window.FocusGraph(graphId);
        }

        internal void FocusLinkedPoseCall(PoseGraphId graphId, PoseNodeId nodeId)
        {
            if (!graphId.IsValid || !nodeId.IsValid)
                return;
            OpenGraph(graphId);
            ShowLinkedPoseSelection($"linked-call:{graphId.Value}:{nodeId.Value}");
            m_Canvas?.FocusElement(new GraphAuthoringElementId(nodeId.Value));
        }

        internal string FindImplementationId(
            CharacterLinkedPoseImplementationEntryBinding entry) =>
            m_Profile?.LinkedPoseImplementations
                .FirstOrDefault(value => value && value.Entries.Contains(entry))
                ?.ImplementationId.Value ?? string.Empty;

        internal void RefreshRuntimeDetails() =>
            RefreshSelectedDetails();

        VisualElement Require(string name) =>
            rootVisualElement.Q(name) ?? throw new InvalidOperationException($"Graph Authoring workspace host '{name}' is missing.");

        sealed class NavigatorDataSource : IGraphAuthoringNavigatorDataSource
        {
            readonly CharacterPoseGraphWorkspace m_Window;
            public NavigatorDataSource(CharacterPoseGraphWorkspace window) => m_Window = window;

            public IReadOnlyList<GraphAuthoringNavigatorItem> GetItems(
                IGraphAuthoringDocumentProjection document)
            {
                var items = m_Window.m_Asset.EnumerateGraphs()
                    .Select(graph => new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(graph.GraphId.Value),
                        ReferenceEquals(graph, m_Window.m_Asset.Graph)
                            ? "Graphs"
                            : "Graphs / Pose Graphs",
                        m_Window.ResolveGraphDisplayName(graph),
                        m_Window.m_Asset.name,
                        graph.ContentRevision,
                        new GraphAuthoringCommandId("open-owner"),
                        string.Join(
                            " ",
                            graph.Nodes.Select(node => node.DisplayName))))
                    .ToList();
                foreach (CharacterPoseStateMachineDefinition machine in
                         m_Window.m_Asset.EnumerateStateMachines()
                             .Where(value => value != null)
                             .OrderBy(
                                 value => value.StateMachineId.Value,
                                 StringComparer.Ordinal))
                {
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(
                            "state-machine:" +
                            machine.StateMachineId.Value),
                        "State Machines",
                        CharacterPoseAuthoringDisplayNames.StateMachine(
                            machine),
                        m_Window.m_Asset.name,
                        machine.ContentRevision,
                        new GraphAuthoringCommandId("open-owner"),
                        string.Join(
                            " ",
                            machine.States.Select(value =>
                                value.DisplayName))));
                }
                AppendLinkedPoseItems(items);
                return items;
            }

            void AppendLinkedPoseItems(List<GraphAuthoringNavigatorItem> items)
            {
                CharacterAnimationPresentationProfile profile =
                    m_Window.m_Profile;
                if (!profile)
                    return;
                if (profile.LinkedPoseGroups.Count == 0 &&
                    profile.LinkedPoseImplementations.Count == 0 &&
                    profile.LinkedPoseSelectors.Count == 0 &&
                    CharacterLinkedPoseAuthoringService.EnumerateInterfaces(profile).Count == 0)
                {
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId("linked-empty"),
                        "Linked Pose",
                        "Empty · create Interface first · " + m_Window.LinkedPoseWorkspaceStatus,
                        profile.name,
                        string.Empty,
                        new GraphAuthoringCommandId("open-owner"),
                        "Interface → Group → Implementation → Call"));
                    return;
                }
                var boundInterfaces = new HashSet<CharacterLinkedPoseInterfaceAsset>(
                    profile.LinkedPoseGroups
                        .Where(value => value?.Interface)
                        .Select(value => value.Interface));
                foreach (CharacterLinkedPoseInterfaceAsset linkedInterface in
                         CharacterLinkedPoseAuthoringService.EnumerateInterfaces(profile)
                             .Where(value => !boundInterfaces.Contains(value)))
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId("linked-interface:" + linkedInterface.InterfaceId.Value),
                        "Linked Pose / Contracts",
                        linkedInterface.name + " · " + m_Window.LinkedPoseWorkspaceStatus,
                        profile.name,
                        string.Empty,
                        new GraphAuthoringCommandId("open-owner"),
                        "Unbound Interface · create Group to attach"));
                int groupIndex = 0;
                foreach (CharacterLinkedPoseGroupBinding group in profile.LinkedPoseGroups
                             .Where(value => value != null)
                             .OrderBy(value => value.GroupId))
                {
                    string groupId = group.GroupId.Value;
                    string groupLabel = group.Interface
                        ? group.Interface.name
                        : $"Group {++groupIndex}";
                    string groupStatus = group.Interface && group.Interface.IsStale
                        ? "Stale"
                        : m_Window.LinkedPoseWorkspaceStatus;
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId("linked-group:" + groupId),
                        "Linked Pose / Groups",
                        groupLabel + " · " + groupStatus,
                        profile.name,
                        string.Empty,
                        new GraphAuthoringCommandId("open-owner"),
                        group.Interface ? group.Interface.name : "Missing Interface"));
                    if (group.Interface)
                    {
                        CharacterLinkedPoseInterfaceAsset linkedInterface = group.Interface;
                        items.Add(new GraphAuthoringNavigatorItem(
                            new GraphAuthoringElementId("linked-interface:" + linkedInterface.InterfaceId.Value),
                            "Linked Pose / " + groupLabel + " / Contract",
                            linkedInterface.name,
                            groupId,
                            linkedInterface.InterfaceId.Value,
                            new GraphAuthoringCommandId("open-owner"),
                            $"{linkedInterface.InterfaceId} {linkedInterface.SignatureHash}"));
                    }
                    foreach (CharacterLinkedPoseSelectorBindingAsset selector in profile.LinkedPoseSelectors
                                 .Where(value => value && value.GroupId == group.GroupId))
                        items.Add(new GraphAuthoringNavigatorItem(
                            new GraphAuthoringElementId("linked-selector:" + selector.SelectorId.Value),
                            "Linked Pose / " + groupLabel + " / Selection",
                            selector.name,
                            groupId,
                            selector.SelectorId.Value,
                            new GraphAuthoringCommandId("open-owner"),
                            string.Join(" ", selector.CandidateImplementationIds.Select(value => value.Value))));
                    foreach (CharacterLinkedPoseImplementationAsset implementation in profile.LinkedPoseImplementations
                                 .Where(value => value && (!group.Interface || value.Interface == group.Interface)))
                    {
                        items.Add(new GraphAuthoringNavigatorItem(
                            new GraphAuthoringElementId("linked-implementation:" + implementation.ImplementationId.Value),
                            "Linked Pose / " + groupLabel + " / Implementations",
                            implementation.name + " · " + (implementation.IsStale ? "Stale" : m_Window.LinkedPoseWorkspaceStatus),
                            groupId,
                            implementation.ImplementationId.Value,
                            new GraphAuthoringCommandId("open-owner"),
                            $"{implementation.ImplementationId} {implementation.Interface?.name}"));
                        foreach (CharacterLinkedPoseInterfaceEntryDescriptor requiredEntry in (implementation.Interface?.Entries ?? Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>()).Where(value => value != null))
                        {
                            CharacterLinkedPoseImplementationEntryBinding entry = implementation.Entries
                                .FirstOrDefault(value => value != null && value.EntryId == requiredEntry.EntryId);
                            items.Add(new GraphAuthoringNavigatorItem(
                                new GraphAuthoringElementId("linked-entry:" + implementation.ImplementationId.Value + ":" + requiredEntry.EntryId.Value),
                                "Linked Pose / " + groupLabel + " / Implementations / Entry",
                                (entry == null ? "Missing · " : string.Empty) + EntryDisplayName(requiredEntry.EntryId),
                                implementation.ImplementationId.Value,
                                requiredEntry.EntryId.Value,
                                new GraphAuthoringCommandId("open-owner"),
                                entry == null
                                    ? "Required Entry binding is missing."
                                    : $"{entry.GraphOwner?.name} {entry.GraphId} {entry.GraphOwnerIdentity}"));
                        }
                    }
                    foreach (CharacterPoseCanvasNode call in (m_Window.m_Asset.Graph?.Nodes ?? Array.Empty<CharacterPoseCanvasNode>())
                                 .Where(value => value?.Payload is CharacterLinkedPoseCallPayload payload && payload.GroupId == group.GroupId))
                        items.Add(new GraphAuthoringNavigatorItem(
                            new GraphAuthoringElementId("linked-call:" + m_Window.m_Asset.Graph.GraphId.Value + ":" + call.NodeId.Value),
                            "Linked Pose / " + groupLabel + " / Host Calls",
                            call.DisplayName,
                            m_Window.m_Asset.Graph.GraphId.Value,
                            call.NodeId.Value,
                            new GraphAuthoringCommandId("open-owner"),
                            call.LinkedPoseEntryId.Value));
                    if (group.Interface)
                    {
                        foreach (CharacterLinkedPoseInterfaceEntryDescriptor requiredEntry in group.Interface.Entries.Where(value => value != null))
                        {
                            int callCount = (m_Window.m_Asset.Graph?.Nodes ?? Array.Empty<CharacterPoseCanvasNode>())
                                .Count(value => value?.Payload is CharacterLinkedPoseCallPayload payload &&
                                                payload.GroupId == group.GroupId &&
                                                payload.EntryId == requiredEntry.EntryId);
                            if (callCount == 1)
                                continue;
                            string coverage = callCount == 0 ? "Missing" : "Duplicate";
                            items.Add(new GraphAuthoringNavigatorItem(
                                new GraphAuthoringElementId("linked-call-missing:" + group.GroupId.Value + ":" + requiredEntry.EntryId.Value),
                                "Linked Pose / " + groupLabel + " / Host Calls",
                                coverage + " · " + EntryDisplayName(requiredEntry.EntryId),
                                group.GroupId.Value,
                                requiredEntry.EntryId.Value,
                                new GraphAuthoringCommandId("open-owner"),
                                $"Required Call coverage is {coverage.ToLowerInvariant()} ({callCount})."));
                        }
                    }
                }
            }

            static string EntryDisplayName(LinkedPoseEntryId entryId)
            {
                string value = entryId.Value ?? string.Empty;
                int separator = Math.Max(
                    value.LastIndexOf('.'),
                    Math.Max(value.LastIndexOf('/'), value.LastIndexOf(':')));
                string leaf = separator >= 0 && separator + 1 < value.Length
                    ? value.Substring(separator + 1)
                    : value;
                leaf = leaf.Replace('-', ' ').Replace('_', ' ').Trim();
                return string.IsNullOrEmpty(leaf)
                    ? "Entry"
                    : char.ToUpperInvariant(leaf[0]) + leaf.Substring(1);
            }

            public void Open(
                IGraphAuthoringDocumentProjection document,
                GraphAuthoringNavigatorItem item)
            {
                if (TryOpenLinkedPoseItem(item.ItemId.Value)) return;
                const string stateMachinePrefix = "state-machine:";
                if (item.ItemId.Value.StartsWith(
                        stateMachinePrefix,
                        StringComparison.Ordinal))
                {
                    string stateMachineId = item.ItemId.Value.Substring(
                        stateMachinePrefix.Length);
                    m_Window.NavigateFromCatalog("state", stateMachineId);
                    return;
                }
                var graphId = new PoseGraphId(item.ItemId.Value);
                if (m_Window.m_Asset.TryGetGraph(graphId, out _))
                {
                    m_Window.NavigateFromCatalog("graph", graphId.Value);
                    return;
                }
            }

            bool TryOpenLinkedPoseItem(string itemId)
            {
                CharacterAnimationPresentationProfile profile =
                    m_Window.m_Profile;
                if (!profile || string.IsNullOrEmpty(itemId))
                    return false;
                if (itemId == "linked-empty" ||
                    itemId.StartsWith("linked-group:", StringComparison.Ordinal) ||
                    itemId.StartsWith("linked-interface:", StringComparison.Ordinal) ||
                    itemId.StartsWith("linked-selector:", StringComparison.Ordinal) ||
                    itemId.StartsWith("linked-implementation:", StringComparison.Ordinal) ||
                    itemId.StartsWith("linked-entry:", StringComparison.Ordinal) ||
                    itemId.StartsWith("linked-call:", StringComparison.Ordinal) ||
                    itemId.StartsWith("linked-call-missing:", StringComparison.Ordinal))
                {
                    if (itemId.StartsWith("linked-call-missing:", StringComparison.Ordinal))
                    {
                        string[] parts = itemId.Substring("linked-call-missing:".Length).Split(':');
                        if (parts.Length == 2)
                            m_Window.ShowLinkedPoseSelection("linked-group:" + parts[0]);
                        return true;
                    }
                    if (itemId.StartsWith("linked-entry:", StringComparison.Ordinal))
                    {
                        string[] parts = itemId.Substring("linked-entry:".Length).Split(':');
                        CharacterLinkedPoseImplementationAsset implementation =
                            parts.Length == 2
                                ? profile.LinkedPoseImplementations.FirstOrDefault(value =>
                                    value && value.ImplementationId.Value == parts[0])
                                : null;
                        CharacterLinkedPoseImplementationEntryBinding entry =
                            implementation?.Entries.FirstOrDefault(value =>
                                value != null && value.EntryId.Value == parts[1]);
                        if (entry?.GraphOwner)
                            m_Window.FocusLinkedPoseEntry(entry.GraphOwner, entry.GraphId);
                    }
                    m_Window.ShowLinkedPoseSelection(itemId);
                    return true;
                }
                return false;
            }

        }
    }
}
