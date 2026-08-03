using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPresentationPoseGraphEditorWindow : EditorWindow
    {
        [SerializeField] CharacterPresentationPoseGraphAsset m_Asset;
        [SerializeField] CharacterAnimationPresentationProfile m_Profile;
        [SerializeField] CharacterPresentationProjectionAsset m_Projection;
        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] string m_CurrentGraphId = string.Empty;

        GraphAuthoringCanvasView m_Canvas;
        GraphAuthoringCanvasView m_StateMachineSurface;
        GraphAuthoringDetailsRegion m_Details;
        GraphAuthoringNavigatorPresenter m_Navigator;
        GraphAuthoringBreadcrumbHost m_BreadcrumbHost;
        GraphAuthoringUndoBinding m_UndoBinding;
        GraphAuthoringBottomDockPresenter m_BottomDock;
        Label m_Title;
        Label m_Status;
        ToolbarToggle m_LiveDebugToggle;
        CharacterPoseGraphAssetMutationOwner m_Owner;
        CharacterTypedPoseGraphDocument m_Document;
        CharacterTypedPoseGraphMutationAdapter m_Mutation;
        CharacterPoseRuntimeTraceProjection m_RuntimeTrace;
        CharacterPosePreviewPanel m_PreviewPanel;
        CharacterPoseStateMachineDocument m_StateMachineDocument;
        CharacterPoseStateMachineMutationAdapter m_StateMachineMutation;
        CharacterPoseTransitionRuleDocument m_RuleDocument;
        CharacterPoseTransitionRuleMutationAdapter m_RuleMutation;
        readonly GraphAuthoringPageStack m_PageStack =
            new GraphAuthoringPageStack();
        bool m_ShowingStateMachine;
        bool m_ShowingTransitionRule;
        GraphAuthoringSelectionBinding m_SelectionBinding;
        GraphAuthoringSelection? m_LastSelection;
        string m_LastContentRevision = string.Empty;
        readonly Guid m_PoseWatchOwnerId = Guid.NewGuid();
        readonly Guid m_DiagnosticsInterestOwnerId = Guid.NewGuid();
        readonly List<AnimationPoseWatchIdentity> m_PoseWatchIdentities =
            new List<AnimationPoseWatchIdentity>();
        AnimationPresentationRuntimeTarget m_PoseWatchRuntimeTarget;
        AnimationPresentationRuntimeTarget m_DiagnosticsInterestTarget;

        internal CharacterPipelineDefinition DefinitionContext => m_Definition;
        internal CharacterAnimationPresentationProfile ProfileContext => m_Profile;
        internal Guid PoseWatchOwnerId => m_PoseWatchOwnerId;
        internal IReadOnlyList<AnimationPoseWatchIdentity>
            PoseWatchIdentities => m_PoseWatchIdentities;

        public static CharacterPresentationPoseGraphEditorWindow Open(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile = null,
            CharacterPresentationProjectionAsset projection = null,
            CharacterPipelineDefinition definition = null)
        {
            if (!asset || asset.Graph == null || !asset.Graph.GraphId.IsValid)
                throw new ArgumentException("Presentation Pose Graph is missing typed authoring data.", nameof(asset));
            if (definition && (!profile || definition.AnimationPresentationProfile != profile))
                throw new InvalidOperationException("Character Definition does not own the selected Presentation Profile.");
            CharacterPresentationPoseGraphEditorWindow window = GetWindow<CharacterPresentationPoseGraphEditorWindow>();
            window.titleContent = new GUIContent("Presentation Pose Graph");
            window.SetDocument(asset, profile, projection, definition);
            window.Show();
            window.Focus();
            return window;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeWindow");
            if (!visualTree)
                throw new InvalidOperationException("Graph Authoring workspace visual tree is missing.");
            visualTree.CloneTree(rootVisualElement);

            VisualElement toolbar = Require("workspace-toolbar-content");
            VisualElement navigatorHost = Require("workspace-navigator-content");
            VisualElement canvasHost = Require("workspace-graph-content");
            VisualElement detailsHost = Require("workspace-details-content");
            VisualElement bottomHost = Require("workspace-bottom-content");
            rootVisualElement.Q<Label>("workspace-navigator-title").text = "Navigator";
            rootVisualElement.Q<Label>("workspace-details-title").text = "Details";
            rootVisualElement.Q<Label>("workspace-bottom-title").text =
                "Preview / Pose Watch / Live Debug";

            m_Title = new Label { name = "tree-title" };
            rootVisualElement.Add(m_Title);
            m_Canvas = new GraphAuthoringCanvasView
            {
                name = "tree-view"
            };
            m_StateMachineSurface =
                new GraphAuthoringCanvasView();
            m_StateMachineSurface.style.display = DisplayStyle.None;
            m_Details = new GraphAuthoringDetailsRegion
            {
                name = "tree-inspector"
            };
            m_Navigator = new GraphAuthoringNavigatorPresenter();
            m_BreadcrumbHost =
                new GraphAuthoringBreadcrumbHost(
                    rootVisualElement.Q<Button>(
                        "tree-navigation-back-button"),
                    rootVisualElement.Q(
                        "tree-navigation-breadcrumb"));
            m_BreadcrumbHost.BindBack(() =>
                NavigateToPage(m_PageStack.Pages.Count - 2));
            m_BottomDock =
                new GraphAuthoringBottomDockPresenter();
            canvasHost.Add(m_Canvas);
            canvasHost.Add(m_StateMachineSurface);
            detailsHost.Add(m_Details);
            navigatorHost.Add(m_Navigator);
            bottomHost.Add(m_BottomDock);

            toolbar.Add(new Button(ValidateAuthoring) { text = "Validate" });
            toolbar.Add(new Button(CompileSemanticIr) { text = "Compile" });
            toolbar.Add(new Button(BuildDefinition) { text = "Build" });
            m_LiveDebugToggle = new ToolbarToggle { text = "Live Debug" };
            m_LiveDebugToggle.RegisterValueChangedCallback(evt =>
                SetLiveDebug(evt.newValue));
            toolbar.Add(m_LiveDebugToggle);
            toolbar.Add(new Button(() => Selection.activeObject = m_Asset) { text = "Asset" });
            if (m_Projection)
                toolbar.Add(new Button(() => Selection.activeObject = m_Projection) { text = "Projection" });

            m_Status = new Label();
            toolbar.Add(m_Status);

            m_Canvas.NodeCreationRequested += ShowCreateMenu;
            m_Canvas.ChildSurfaceRequested += OpenChildSurface;
            m_StateMachineSurface
                .StateMachineNodeCreationRequested +=
                ShowStateMachineCreateMenu;
            m_SelectionBinding =
                new GraphAuthoringSelectionBinding(
                    rootVisualElement,
                    PublishSelection);
            m_UndoBinding =
                new GraphAuthoringUndoBinding(Reload);
            RuntimeDebugSession.Shared.Changed += OnRuntimeDebugChanged;
            BindCurrentGraph();
        }

        void OnDisable()
        {
            m_UndoBinding?.Dispose();
            m_UndoBinding = null;
            RuntimeDebugSession.Shared.Changed -= OnRuntimeDebugChanged;
            RuntimeDebugSession.Shared.ReleaseLiveInterest(this);
            ReleaseDiagnosticsInterest();
            ReleasePoseWatchInterests();
            m_BreadcrumbHost?.Dispose();
            m_BreadcrumbHost = null;
            m_BottomDock?.Unbind();
            m_SelectionBinding?.Dispose();
            m_SelectionBinding = null;
            if (m_Canvas != null)
            {
                m_Canvas.NodeCreationRequested -= ShowCreateMenu;
                m_Canvas.ChildSurfaceRequested -= OpenChildSurface;
            }
            if (m_StateMachineSurface != null)
            {
                m_StateMachineSurface
                    .StateMachineNodeCreationRequested -=
                    ShowStateMachineCreateMenu;
            }
        }

        public void SetDocument(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationProjectionAsset projection,
            CharacterPipelineDefinition definition = null)
        {
            m_Asset = asset;
            m_Profile = profile;
            m_Projection = projection;
            m_Definition = definition;
            if (asset && asset.Graph != null)
                m_CurrentGraphId = asset.Graph.GraphId.Value;
            BindCurrentGraph(true);
        }

        public void FocusStateSequence(
            CharacterTypedPoseNode machine,
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

        void BindCurrentGraph(bool resetPages = false)
        {
            if (m_Canvas == null || !m_Asset || m_Asset.Graph == null)
                return;
            PoseGraphId graphId = string.IsNullOrWhiteSpace(m_CurrentGraphId)
                ? m_Asset.Graph.GraphId
                : new PoseGraphId(m_CurrentGraphId);
            CharacterTypedPoseGraph graph = m_Asset.RequireGraph(graphId);
            m_ShowingStateMachine = false;
            m_ShowingTransitionRule = false;
            m_RuleDocument = null;
            m_RuleMutation = null;
            m_Canvas.style.display = DisplayStyle.Flex;
            m_StateMachineSurface.style.display = DisplayStyle.None;
            m_Details.style.display = DisplayStyle.Flex;
            m_Owner = new CharacterPoseGraphAssetMutationOwner(m_Asset, m_Profile);
            string graphDisplayName = ResolveGraphDisplayName(graph);
            m_Document = new CharacterTypedPoseGraphDocument(
                m_Owner,
                graph.GraphId.Value,
                ResolveRole(graph),
                graphDisplayName);
            m_Mutation = new CharacterTypedPoseGraphMutationAdapter();
            m_Mutation.ReadOnly =
                m_LiveDebugToggle != null && m_LiveDebugToggle.value;
            m_RuntimeTrace =
                new CharacterPoseRuntimeTraceProjection(m_Asset, m_Projection);
            GraphAuthoringCapabilityCatalog catalog = CharacterPoseGraphAuthoringCapabilities.Catalog;
            m_Canvas.BindProjection(
                new GraphAuthoringProjectionCanvasBinding(
                m_Document,
                catalog,
                m_Mutation,
                new CharacterTypedPoseConnectionPolicy(),
                new CharacterTypedPoseGraphClipboardCodec(
                    m_Mutation)));
            m_Details.Bind(new GraphAuthoringDetailsBinding(
                m_Document,
                catalog,
                m_Mutation,
                new CharacterTypedPoseDetailsDataSource(
                    m_RuntimeTrace,
                    m_Profile?.RigDefinition,
                    m_Profile),
                OpenDetailsCommand,
                true));
            m_Navigator.Bind(m_Document, new NavigatorDataSource(this));
            m_BottomDock.Bind(m_Document, CreateBottomDockCatalog());
            m_Title.text = $"{m_Asset.name} / {graphDisplayName}";
            m_Status.text = "Authoring";
            m_LastContentRevision = graph.ContentRevision;
            m_LastSelection = null;
            if (resetPages || m_PageStack.Pages.Count == 0)
            {
                m_PageStack.Reset(new GraphAuthoringPageProjection(
                    new GraphAuthoringElementId(graph.GraphId.Value),
                    graphDisplayName,
                    ResolveRole(graph).Value));
            }
            RenderBreadcrumb();
        }

        GraphAuthoringBottomDockCatalog CreateBottomDockCatalog()
        {
            var catalog = new GraphAuthoringBottomDockCatalog();
            GraphAuthoringDocumentRoleId[] roles =
            {
                CharacterPoseGraphAuthoringCapabilities.RootGraph,
                CharacterPoseGraphAuthoringCapabilities.Subgraph,
                CharacterPoseGraphAuthoringCapabilities.StatePoseGraph,
                CharacterPoseGraphAuthoringCapabilities.StateMachine
            };
            catalog.Register(new GraphAuthoringBottomDockTabDescriptor(
                "pose.preview",
                CharacterPoseGraphAuthoringCapabilities.Domain,
                roles,
                "Preview",
                () => m_PreviewPanel =
                    new CharacterPosePreviewPanel(this),
                true));
            catalog.Register(new GraphAuthoringBottomDockTabDescriptor(
                "pose.watch",
                CharacterPoseGraphAuthoringCapabilities.Domain,
                roles,
                "Pose Watch",
                () => new CharacterPoseWatchPanel(this)));
            catalog.Register(new GraphAuthoringBottomDockTabDescriptor(
                "pose.live-debug",
                CharacterPoseGraphAuthoringCapabilities.Domain,
                roles,
                "Live Debug",
                () => new CharacterPoseLiveDebugPanel(m_RuntimeTrace)));
            return catalog;
        }

        GraphAuthoringDocumentRoleId ResolveRole(CharacterTypedPoseGraph graph)
        {
            if (ReferenceEquals(graph, m_Asset.Graph))
                return CharacterPoseGraphAuthoringCapabilities.RootGraph;
            bool stateOwned = m_Asset.EnumerateGraphs()
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Any(value => value.StateMachine != null && value.StateMachine.States.Any(state => state.PoseGraphId == graph.GraphId));
            return stateOwned
                ? CharacterPoseGraphAuthoringCapabilities.StatePoseGraph
                : CharacterPoseGraphAuthoringCapabilities.Subgraph;
        }

        string ResolveGraphDisplayName(CharacterTypedPoseGraph graph)
        {
            if (ReferenceEquals(graph, m_Asset.Graph))
                return "Root Pose Graph";
            string[] stateNames = m_Asset.EnumerateStateMachines()
                .SelectMany(value => value.States)
                .Where(value => value.PoseGraphId == graph.GraphId)
                .Select(value => value.DisplayName)
                .Where(value => !string.IsNullOrWhiteSpace(value))
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
                .Select(value => value.DisplayName)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (subgraphOwners.Length == 1)
                return $"{subgraphOwners[0]} Subgraph";
            if (subgraphOwners.Length > 1)
                return "Shared Pose Subgraph";
            CharacterTypedPoseGraph[] graphs = m_Asset.EnumerateGraphs()
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
                    m_Canvas.contentViewContainer.WorldToLocal(
                        screenPosition - position.position);
                m_Canvas.CreateNode(
                    capability.CapabilityId,
                    operation,
                    rulePosition);
                m_Status.text =
                    "Transition Rule changed · published Projection is Stale until explicit Build.";
                return;
            }
            CharacterPoseNodeKind kind = Enum.GetValues(typeof(CharacterPoseNodeKind))
                .Cast<CharacterPoseNodeKind>()
                .Single(value => CharacterPoseGraphAuthoringCapabilities.Get(value).Equals(capability.CapabilityId));
            CharacterPoseNodePayload payload =
                (CharacterPoseNodePayload)Activator.CreateInstance(
                    CharacterPoseGraphAuthoringCapabilities
                        .RequirePayloadType(kind));
            var node = new CharacterTypedPoseNode(new PoseNodeId(Guid.NewGuid().ToString("N")), capability.DisplayName, payload);
            Vector2 graphPosition = m_Canvas.contentViewContainer.WorldToLocal(screenPosition - position.position);
            m_Canvas.CreateNode(capability.CapabilityId, node, graphPosition);
            m_Status.text =
                "Authoring changed · published Projection is Stale until explicit Build.";
            m_BottomDock?.Refresh();
        }

        void PublishSelection()
        {
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
            if (!string.Equals(
                    revision,
                    m_LastContentRevision,
                    StringComparison.Ordinal))
            {
                m_LastContentRevision = revision;
                m_Status.text =
                    "Authoring changed · published Projection is Stale until explicit Build.";
                m_BottomDock?.Refresh();
            }
            GraphAuthoringSelection? current = m_Canvas.GetStableSelection().Count == 1
                ? m_Canvas.GetStableSelection()[0]
                : null;
            if (Nullable.Equals(current, m_LastSelection) &&
                (m_LiveDebugToggle == null ||
                 !m_LiveDebugToggle.value))
                return;
            m_LastSelection = current;
            if (current.HasValue)
                m_Details.Inspect(current.Value);
            else
                m_Details.ClearSelection();
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
            if (request.Kind ==
                    GraphAuthoringMutationKind.ExecuteCommand &&
                request.CommandId.Equals(
                    ActionAnimationWorkspaceCommands.Open))
            {
                CharacterTypedPoseNode typed =
                    m_Document.Graph.Nodes.Single(value =>
                        value.NodeId.Value == nodeId.Value);
                ActionAnimationAuthoringWorkspaceEntryPoints
                    .OpenFromPoseSlot(
                        m_Definition,
                        m_Asset,
                        m_Document.Graph,
                        typed);
                return;
            }
            if (request.Kind !=
                GraphAuthoringMutationKind.OpenChildSurface)
                throw new InvalidOperationException(
                    $"Details command '{request.Kind}' is not a navigation command.");
            GraphAuthoringNodeProjection node =
                m_Document.Nodes.Single(value =>
                    value.NodeId.Equals(nodeId));
            GraphAuthoringCapabilityDescriptor capability =
                CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                    node.CapabilityId,
                    m_Document.DomainId,
                    m_Document.DocumentRoleId);
            GraphAuthoringChildSurfaceDescriptor child =
                capability.ChildSurfaces.Single(value =>
                    value.CommandId.Equals(request.CommandId));
            OpenChildSurface(node, child);
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

            if (!m_Profile)
                throw new InvalidOperationException(
                    "Pose Source command requires an exact Presentation Profile context.");
            if (openProfile)
            {
                Selection.activeObject = m_Profile;
                EditorGUIUtility.PingObject(m_Profile);
                AssetDatabase.OpenAsset(m_Profile);
                return true;
            }

            CharacterTypedPoseNode typed =
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

            if (openSource)
                CharacterPoseSourceEditorWindow.Open(m_Profile, binding);
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
            CharacterTypedPoseNode typed =
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
                OpenStateMachine(payload.StateMachine, true);
                return;
            }
            if (typed.Payload is CharacterPoseSubgraphPayload subgraph &&
                subgraph.Subgraph != null &&
                subgraph.Subgraph.PoseGraphId.IsValid)
            {
                CharacterTypedPoseGraph graph =
                    m_Asset.RequireGraph(
                        subgraph.Subgraph.PoseGraphId);
                m_PageStack.Push(new GraphAuthoringPageProjection(
                    new GraphAuthoringElementId(graph.GraphId.Value),
                    ResolveGraphDisplayName(graph),
                    ResolveRole(graph).Value));
                m_CurrentGraphId = graph.GraphId.Value;
                BindCurrentGraph(false);
                return;
            }
            throw new InvalidOperationException(
                $"Pose node '{typed.NodeId}' does not own child surface '{child.CommandId}'.");
        }

        void OpenStateMachine(
            CharacterPoseStateMachineDefinition machine,
            bool pushPage)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            m_ShowingStateMachine = true;
            m_ShowingTransitionRule = false;
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
                new CharacterPoseStateMachineMutationAdapter
                {
                    ReadOnly =
                        m_LiveDebugToggle != null &&
                        m_LiveDebugToggle.value
                };
            var policy = new CharacterPoseStateMachinePolicy(
                OpenStateGraph,
                OpenTransitionRule,
                CharacterPoseTransitionCreationDialog.Show);
            var binding = new GraphAuthoringStateMachineBinding(
                m_StateMachineDocument,
                CharacterPoseGraphAuthoringCapabilities.Catalog,
                m_StateMachineMutation,
                policy);
            m_StateMachineSurface.BindStateMachine(binding);
            m_Details.BindStateMachine(
                binding,
                new CharacterPoseStateMachineDetailsDataSource());
            m_Navigator.Bind(
                m_StateMachineDocument,
                new NavigatorDataSource(this));
            m_BottomDock.Bind(
                m_StateMachineDocument,
                CreateBottomDockCatalog());
            m_Title.text =
                $"{m_Asset.name} / " +
                CharacterPoseAuthoringDisplayNames.StateMachine(machine);
            m_Status.text = "Authoring";
            m_LastContentRevision = machine.ContentRevision;
            m_LastSelection = null;
            if (pushPage)
            {
                m_PageStack.Push(
                    m_StateMachineDocument.Pages[0]);
            }
            RenderBreadcrumb();
        }

        void OpenStateGraph(CharacterPoseStateDefinition state)
        {
            CharacterTypedPoseGraph graph =
                m_Asset.RequireGraph(state.PoseGraphId);
            m_PageStack.Push(new GraphAuthoringPageProjection(
                new GraphAuthoringElementId(graph.GraphId.Value),
                state.DisplayName,
                CharacterPoseGraphAuthoringCapabilities
                    .StatePoseGraph.Value));
            m_CurrentGraphId = graph.GraphId.Value;
            BindCurrentGraph(false);
        }

        void OpenTransitionRule(
            CharacterPoseStateTransition transition)
        {
            BindTransitionRule(
                m_StateMachineDocument.Definition,
                transition.TransitionId,
                true);
        }

        void BindTransitionRule(
            CharacterPoseStateMachineDefinition machine,
            PoseStateTransitionId transitionId,
            bool pushPage)
        {
            m_ShowingStateMachine = false;
            m_ShowingTransitionRule = true;
            m_Canvas.style.display = DisplayStyle.Flex;
            m_StateMachineSurface.style.display =
                DisplayStyle.None;
            m_Details.style.display = DisplayStyle.Flex;
            m_RuleDocument =
                new CharacterPoseTransitionRuleDocument(
                    m_Asset,
                    machine,
                    transitionId);
            m_RuleMutation =
                new CharacterPoseTransitionRuleMutationAdapter
                {
                    ReadOnly =
                        m_LiveDebugToggle != null &&
                        m_LiveDebugToggle.value
                };
            m_Canvas.BindProjection(
                new GraphAuthoringProjectionCanvasBinding(
                m_RuleDocument,
                CharacterPoseGraphAuthoringCapabilities.Catalog,
                m_RuleMutation,
                new CharacterPoseTransitionRuleConnectionPolicy(),
                persistsLayout: false));
            m_Details.Bind(new GraphAuthoringDetailsBinding(
                m_RuleDocument,
                CharacterPoseGraphAuthoringCapabilities.Catalog,
                m_RuleMutation,
                new CharacterPoseTransitionRuleDetailsDataSource(),
                ExecuteRuleDetailsCommand));
            m_Navigator.Bind(
                m_RuleDocument,
                new NavigatorDataSource(this));
            m_BottomDock.Bind(
                m_RuleDocument,
                CreateBottomDockCatalog());
            m_Title.text =
                $"{m_Asset.name} / Transition Rule / " +
                m_RuleDocument.DisplayName;
            m_Status.text = "Authoring";
            m_LastContentRevision =
                m_RuleDocument.ContentRevision;
            m_LastSelection = null;
            if (pushPage)
                m_PageStack.Push(m_RuleDocument.Pages[0]);
            RenderBreadcrumb();
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
                transitionId,
                false);
        }

        void ShowStateMachineCreateMenu(Vector2 screenPosition)
        {
            if (m_StateMachineMutation == null ||
                m_StateMachineMutation.ReadOnly)
                return;
            var menu = new GenericMenu();
            Vector2 graphPosition =
                m_StateMachineSurface.contentViewContainer.WorldToLocal(
                    screenPosition - position.position);
            menu.AddItem(
                new GUIContent("State"),
                false,
                () => CreateState(graphPosition));
            IReadOnlyList<GraphAuthoringSelection> selection =
                m_StateMachineSurface.GetStableSelection();
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
            string suffix = Guid.NewGuid().ToString("N");
            var graphId = new PoseGraphId(suffix);
            var outputNodeId = new PoseNodeId(
                Guid.NewGuid().ToString("N"));
            var outputNode = new CharacterTypedPoseNode(
                outputNodeId,
                "Output Pose",
                new CharacterOutputPosePayload());
            var graph = new CharacterTypedPoseGraph(
                graphId,
                Guid.NewGuid().ToString("N"),
                Array.Empty<CharacterPoseParameterDeclaration>(),
                new[] { outputNode },
                Array.Empty<CharacterPoseEdge>(),
                new[]
                {
                    new CharacterPoseGraphLayoutEntry(
                        outputNodeId,
                        new Vector2(420f, 0f))
                });
            var state = new CharacterPoseStateDefinition(
                new PoseStateId(suffix),
                $"State {m_StateMachineDocument.Definition.States.Count + 1}",
                graphId,
                outputNodeId,
                true);
            m_StateMachineMutation.Apply(
                m_StateMachineDocument,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateState,
                    position: position,
                    value: new CharacterPoseStateCreation(
                        state,
                        graph)));
            OpenStateMachine(
                m_StateMachineDocument.Definition,
                false);
        }

        void CreateStateAlias(Vector2 position)
        {
            CharacterPoseStateTransitionSource[] sources =
                m_StateMachineSurface.GetStableSelection()
                    .Where(value =>
                        value.Kind ==
                        GraphAuthoringSelectionKind.State)
                    .Select(value =>
                        m_StateMachineDocument.Definition.States.Any(
                            state =>
                                state.StateId.Value ==
                                value.ElementId.Value)
                            ? CharacterPoseStateTransitionSource
                                .FromState(
                                    new PoseStateId(
                                        value.ElementId.Value))
                            : CharacterPoseStateTransitionSource
                                .FromAlias(
                                    new PoseStateAliasId(
                                        value.ElementId.Value)))
                    .Distinct()
                    .ToArray();
            if (sources.Length == 0)
                return;
            var alias = new CharacterPoseStateAlias(
                new PoseStateAliasId(
                    Guid.NewGuid().ToString("N")),
                $"Alias {m_StateMachineDocument.Definition.Aliases.Count + 1}",
                sources);
            m_StateMachineMutation.Apply(
                m_StateMachineDocument,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateStateAlias,
                    position: position,
                    value: alias));
            OpenStateMachine(
                m_StateMachineDocument.Definition,
                false);
        }

        void PublishStateMachineSelection()
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
                m_BottomDock?.Refresh();
            }
            IReadOnlyList<GraphAuthoringSelection> selection =
                m_StateMachineSurface.GetStableSelection();
            GraphAuthoringSelection? current =
                selection.Count == 1 ? selection[0] : null;
            if (Nullable.Equals(current, m_LastSelection))
                return;
            m_LastSelection = current;
            if (current.HasValue &&
                current.Value.Kind == GraphAuthoringSelectionKind.State)
            {
                m_Details.InspectState(current.Value.ElementId);
                return;
            }
            if (current.HasValue &&
                current.Value.Kind ==
                GraphAuthoringSelectionKind.Transition)
            {
                m_Details.InspectTransition(
                    current.Value.ElementId);
                return;
            }
            m_Details.ClearStateMachineSelection();
        }

        void NavigateToPage(int index)
        {
            m_PageStack.NavigateTo(index);
            NavigateToCurrentPage();
        }

        void NavigateToCurrentPage()
        {
            GraphAuthoringPageProjection page =
                m_PageStack.Current;
            if (string.Equals(
                    page.Tooltip,
                    CharacterPoseGraphAuthoringCapabilities
                        .StateMachine.Value,
                    StringComparison.Ordinal))
            {
                CharacterPoseStateMachineDefinition machine =
                    m_Asset.EnumerateGraphs()
                        .SelectMany(value => value.Nodes)
                        .Select(value => value?.Payload)
                        .OfType<
                            CharacterPoseStateMachineNodePayload>()
                        .Select(value => value.StateMachine)
                        .Single(value =>
                            value.StateMachineId.Value ==
                            page.PageId.Value);
                OpenStateMachine(machine, false);
                return;
            }
            if (string.Equals(
                    page.Tooltip,
                    CharacterPoseGraphAuthoringCapabilities
                        .TransitionRule.Value,
                    StringComparison.Ordinal))
            {
                (
                    CharacterPoseStateMachineDefinition machine,
                    CharacterPoseStateTransition transition) =
                    FindTransitionRuleOwner(
                        page.PageId.Value);
                BindTransitionRule(
                    machine,
                    transition.TransitionId,
                    false);
                return;
            }
            m_CurrentGraphId = page.PageId.Value;
            BindCurrentGraph(false);
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
            CharacterTypedPoseGraph root = m_Asset.Graph ??
                throw new InvalidOperationException(
                    "Presentation Pose Graph root is missing.");
            m_PageStack.Reset(
                new GraphAuthoringPageProjection(
                    new GraphAuthoringElementId(
                        root.GraphId.Value),
                    ResolveGraphDisplayName(root),
                    CharacterPoseGraphAuthoringCapabilities
                        .RootGraph.Value));
            m_PageStack.Push(
                new GraphAuthoringPageProjection(
                    new GraphAuthoringElementId(
                        machine.StateMachineId.Value),
                    CharacterPoseAuthoringDisplayNames.StateMachine(
                        machine),
                    CharacterPoseGraphAuthoringCapabilities
                        .StateMachine.Value));
            m_PageStack.Push(
                new GraphAuthoringPageProjection(
                    new GraphAuthoringElementId(
                        transition.Rule.GraphId.Value),
                    CharacterPoseAuthoringDisplayNames.Transition(
                        machine,
                        transition),
                    CharacterPoseGraphAuthoringCapabilities
                        .TransitionRule.Value));
            BindTransitionRule(
                machine,
                transition.TransitionId,
                false);
        }

        void RenderBreadcrumb()
        {
            m_BreadcrumbHost?.Render(
                m_PageStack.Pages.Select(page =>
                    new GraphAuthoringBreadcrumbEntry(
                        page.DisplayName,
                        page.Tooltip)).ToArray(),
                NavigateToPage);
        }

        void CompileSemanticIr()
        {
            if (!m_Definition)
            {
                m_Status.text = "Compile unavailable: no Character Definition context.";
                return;
            }
            try
            {
                CharacterSemanticFrontendResult result = CharacterSimulationBuildOrchestrator.CompileSemanticIr(m_Definition, true);
                m_Status.text = result.IsValid ? "Compile completed." : "Compile failed. Inspect the formal report.";
            }
            catch (Exception exception)
            {
                m_Status.text = $"Compile failed: {exception.Message}";
            }
        }

        void ValidateAuthoring()
        {
            if (!m_Asset)
                return;
            IReadOnlyList<string> capabilityErrors =
                CharacterPoseGraphCapabilityValidator.Validate(m_Asset);
            CharacterPoseGraphValidationReport report =
                CharacterPresentationPoseGraphValidator.Validate(
                    m_Asset,
                    m_Profile ? m_Profile.RigDefinition : null,
                    CharacterPoseAuthoringPortProjection.Get);
            int issueCount = capabilityErrors.Count + report.Issues.Count;
            m_Status.text = issueCount == 0
                ? "Authoring valid"
                : $"Authoring invalid · {issueCount} issue(s)";
        }

        void BuildDefinition()
        {
            if (!m_Definition)
            {
                m_Status.text = "Build unavailable: no Character Definition context.";
                return;
            }
            try
            {
                m_Status.text = CharacterSimulationProgramBuildService.Build(m_Definition, true)
                    ? "Build completed and published."
                    : "Build failed. Inspect the formal report.";
            }
            catch (Exception exception)
            {
                m_Status.text = $"Build failed: {exception.Message}";
            }
        }

        void Reload()
        {
            if (!m_Asset)
                return;
            if (m_ShowingTransitionRule &&
                m_RuleDocument != null)
            {
                BindTransitionRule(
                    m_RuleDocument.Machine,
                    m_RuleDocument.TransitionId,
                    false);
            }
            else if (m_ShowingStateMachine &&
                m_StateMachineDocument != null)
            {
                OpenStateMachine(
                    m_StateMachineDocument.Definition,
                    false);
            }
            else
            {
                BindCurrentGraph(false);
            }
        }

        void SetLiveDebug(bool enabled)
        {
            if (enabled)
            {
                RuntimeDebugSession.Shared.EnsureLiveInterest(
                    this,
                    RuntimeTraceChannel.Animation |
                    RuntimeTraceChannel.StateMachine);
                SynchronizeDiagnosticsInterest();
            }
            else
            {
                RuntimeDebugSession.Shared.ReleaseLiveInterest(this);
                ReleaseDiagnosticsInterest();
            }
            if (m_Mutation != null)
                m_Mutation.ReadOnly = enabled;
            if (m_StateMachineMutation != null)
                m_StateMachineMutation.ReadOnly = enabled;
            if (m_RuleMutation != null)
                m_RuleMutation.ReadOnly = enabled;
            m_Status.text = enabled
                ? "Live Debug · authoring mutation disabled."
                : "Authoring";
            if (m_LastSelection.HasValue)
                m_Details?.Inspect(m_LastSelection.Value);
            m_BottomDock?.Refresh();
        }

        void OnRuntimeDebugChanged()
        {
            if (m_LiveDebugToggle == null ||
                !m_LiveDebugToggle.value)
                return;
            SynchronizeDiagnosticsInterest();
            m_BottomDock?.Refresh();
            if (m_LastSelection.HasValue)
                m_Details?.Inspect(m_LastSelection.Value);
        }

        internal bool TryGetPublishedPosePlan(
            out CharacterPresentationPosePlan plan,
            out string status)
        {
            plan = null;
            if (!m_Definition || !m_Profile || !m_Projection ||
                !m_Definition.SimulationProgram ||
                m_Definition.AnimationPresentationProfile != m_Profile ||
                m_Definition.PresentationProjection != m_Projection)
            {
                status =
                    "Unavailable: one exact Definition, Profile, Simulation Program and Presentation Projection context is required.";
                return false;
            }
            try
            {
                var program = m_Definition.SimulationProgram.Load();
                CharacterPresentationSemanticContract contract =
                    Float32CharacterPresentationContractAdapter.Create(program);
                CharacterPresentationProjection projection =
                    m_Projection.Load(contract);
                plan = projection.PosePlan;
            }
            catch (Exception exception)
            {
                status =
                    $"Unavailable: published Pose Plan cannot be loaded: {exception.Message}";
                return false;
            }
            if (!m_Asset || m_Asset.Graph == null ||
                !string.Equals(
                    plan.PoseGraphId,
                    m_Asset.Graph.GraphId.Value,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    plan.ContentRevision,
                    m_Asset.Graph.ContentRevision,
                    StringComparison.Ordinal))
            {
                plan = null;
                status =
                    "Stale: published Pose Plan does not match current authoring. Run explicit Build.";
                return false;
            }
            status = "Ready";
            return true;
        }

        internal bool MatchesCurrentPublishedRevision(
            AnimationPresentationRuntimeSnapshot snapshot) =>
            m_Asset && m_Asset.Graph != null && m_Projection &&
            string.Equals(
                snapshot.PoseGraphId,
                m_Asset.Graph.GraphId.Value,
                StringComparison.Ordinal) &&
            string.Equals(
                snapshot.PoseGraphRevision,
                m_Asset.Graph.ContentRevision,
                StringComparison.Ordinal) &&
            string.Equals(
                snapshot.ProjectionRevision,
                m_Projection.ProjectionRevision,
                StringComparison.Ordinal);

        internal void WatchSelectedNode()
        {
            IReadOnlyList<GraphAuthoringSelection> selection =
                m_Canvas?.GetStableSelection() ??
                Array.Empty<GraphAuthoringSelection>();
            if (selection.Count != 1 ||
                selection[0].Kind != GraphAuthoringSelectionKind.Node)
            {
                m_Status.text =
                    "Pose Watch unavailable: select exactly one Pose node.";
                return;
            }
            if (!TryGetPublishedPosePlan(
                    out CharacterPresentationPosePlan plan,
                    out string status))
            {
                m_Status.text = status;
                return;
            }
            string nodeId = selection[0].ElementId.Value;
            var identities = new List<AnimationPoseWatchIdentity>();
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    plan.Operations[i];
                CharacterPresentationPoseSourceMapEntry source =
                    plan.SourceMap[i];
                if (operation.OutputValueIndex < 0 ||
                    !string.Equals(
                        source.GraphId,
                        m_Document.DocumentId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        source.NodeId.Value,
                        nodeId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                identities.Add(new AnimationPoseWatchIdentity(
                    source.GraphId,
                    plan.ContentRevision,
                    source.NodeId,
                    source.CallSite));
            }
            foreach (AnimationPoseWatchIdentity identity in
                     identities.Distinct())
            {
                if (m_PoseWatchIdentities.Contains(identity))
                    continue;
                if (m_PoseWatchIdentities.Count >=
                    AnimationPoseWatchCapacity.PerWindow)
                {
                    m_Status.text =
                        $"Pose Watch capacity exceeded: {AnimationPoseWatchCapacity.PerWindow}.";
                    break;
                }
                m_PoseWatchIdentities.Add(identity);
            }
            if (identities.Count == 0)
            {
                m_Status.text =
                    "Pose Watch unavailable: selected node has no compiled Pose output.";
                return;
            }
            SynchronizePoseWatchInterests();
            m_BottomDock?.Refresh();
        }

        internal void RemovePoseWatch(int index)
        {
            if ((uint)index >= (uint)m_PoseWatchIdentities.Count)
                return;
            m_PoseWatchIdentities.RemoveAt(index);
            SynchronizePoseWatchInterests();
            m_BottomDock?.Refresh();
        }

        internal void ClearPoseWatches()
        {
            m_PoseWatchIdentities.Clear();
            SynchronizePoseWatchInterests();
            m_BottomDock?.Refresh();
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

        internal void SynchronizePoseWatchInterests()
        {
            RuntimeDebugViewModel viewModel =
                RuntimeDebugSession.Shared.ViewModel;
            AnimationPresentationRuntimeTarget target =
                viewModel.Attached &&
                AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget resolved)
                    ? resolved
                    : null;
            if (!ReferenceEquals(target, m_PoseWatchRuntimeTarget))
            {
                m_PoseWatchRuntimeTarget?.RemovePoseWatchInterests(
                    m_PoseWatchOwnerId);
                m_PoseWatchRuntimeTarget = target;
            }
            m_PoseWatchRuntimeTarget?.SetPoseWatchInterests(
                m_PoseWatchOwnerId,
                m_PoseWatchIdentities);
        }

        void SynchronizeDiagnosticsInterest()
        {
            RuntimeDebugViewModel viewModel =
                RuntimeDebugSession.Shared.ViewModel;
            AnimationPresentationRuntimeTarget target =
                viewModel.Attached &&
                AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget resolved)
                    ? resolved
                    : null;
            if (!ReferenceEquals(target, m_DiagnosticsInterestTarget))
            {
                m_DiagnosticsInterestTarget?.RemoveDiagnosticsInterest(
                    m_DiagnosticsInterestOwnerId);
                m_DiagnosticsInterestTarget = target;
            }
            m_DiagnosticsInterestTarget?.SetDiagnosticsInterest(
                m_DiagnosticsInterestOwnerId,
                AnimationPresentationDiagnosticsInterest.LiveState |
                AnimationPresentationDiagnosticsInterest.OperationDetail);
        }

        void ReleaseDiagnosticsInterest()
        {
            m_DiagnosticsInterestTarget?.RemoveDiagnosticsInterest(
                m_DiagnosticsInterestOwnerId);
            m_DiagnosticsInterestTarget = null;
        }

        internal void ReleasePoseWatchInterests()
        {
            m_PoseWatchRuntimeTarget?.RemovePoseWatchInterests(
                m_PoseWatchOwnerId);
            m_PoseWatchRuntimeTarget = null;
        }

        internal bool TryGetPoseWatchSnapshot(
            out AnimationPresentationRuntimeSnapshot snapshot,
            out string status)
        {
            if (m_PreviewPanel != null &&
                m_PreviewPanel.TryGetSnapshot(out snapshot, out status))
            {
                return true;
            }
            if (m_RuntimeTrace != null)
                return m_RuntimeTrace.TryGetSnapshot(
                    out snapshot,
                    out status);
            snapshot = default;
            status = "Unavailable: Pose runtime trace is not bound.";
            return false;
        }

        internal void RefreshBottomDock() =>
            m_BottomDock?.Refresh();

        VisualElement Require(string name) =>
            rootVisualElement.Q(name) ?? throw new InvalidOperationException($"Graph Authoring workspace host '{name}' is missing.");

        sealed class NavigatorDataSource : IGraphAuthoringNavigatorDataSource
        {
            readonly CharacterPresentationPoseGraphEditorWindow m_Window;
            public NavigatorDataSource(CharacterPresentationPoseGraphEditorWindow window) => m_Window = window;

            public IReadOnlyList<GraphAuthoringNavigatorItem> GetItems(
                IGraphAuthoringDocumentProjection document)
            {
                var items = m_Window.m_Asset.EnumerateGraphs()
                    .Select(graph => new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(graph.GraphId.Value),
                        ReferenceEquals(graph, m_Window.m_Asset.Graph)
                            ? "Root"
                            : "Owned Graphs",
                        m_Window.ResolveGraphDisplayName(graph),
                        m_Window.m_Asset.name,
                        graph.ContentRevision,
                        new GraphAuthoringCommandId("open-owner"),
                        string.Join(
                            " ",
                            graph.Nodes.Select(node => node.DisplayName))))
                    .ToList();
                foreach ((
                             CharacterPoseStateMachineDefinition machine,
                             CharacterPoseStateTransition transition) in
                         m_Window.m_Asset.EnumerateGraphs()
                             .Where(value => value != null)
                             .SelectMany(value => value.Nodes)
                             .Select(value => value?.Payload)
                             .OfType<
                                 CharacterPoseStateMachineNodePayload>()
                             .Select(value => value.StateMachine)
                             .Where(value => value != null)
                             .SelectMany(machine =>
                                 machine.Transitions.Select(
                                     transition =>
                                         (
                                             Machine: machine,
                                             Transition:
                                             transition))))
                {
                    items.Add(
                        new GraphAuthoringNavigatorItem(
                            new GraphAuthoringElementId(
                                transition.Rule.GraphId.Value),
                            "Transition Rules",
                            CharacterPoseAuthoringDisplayNames.Transition(
                                machine,
                                transition) +
                            $" · Priority {transition.Priority}",
                            machine.StateMachineId.Value,
                            transition.TransitionId.Value,
                            new GraphAuthoringCommandId(
                                "open-owner"),
                            $"{transition.Rule.GraphId.Value} {transition.TransitionId.Value} {transition.Source.Kind} {transition.TargetStateId.Value}"));
                }
                if (!m_Window.m_Definition ||
                    !m_Window.m_Profile ||
                    m_Window.m_Definition.AnimationPresentationProfile !=
                    m_Window.m_Profile)
                {
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(
                            "unavailable:definition-context"),
                        "Data Catalog",
                        "Unavailable: exact Definition context required",
                        string.Empty,
                        string.Empty,
                        default,
                        "Definition Profile Pose Source Action Producer"));
                    return items;
                }
                foreach (CharacterPresentationPoseSourceBinding source in
                         m_Window.m_Profile.PoseSourceBindings
                             .Where(value => value && value.Slot)
                             .OrderBy(value => value.Slot.name, StringComparer.Ordinal))
                {
                    UnityEngine.Object asset = source.SourceAsset;
                    string slotName = source.Slot.name;
                    string label = asset
                        ? $"{slotName} → {asset.name}"
                        : $"{slotName} → Missing Resource";
                    string detail = source is CharacterSequencePoseSourceBinding sequence
                        ? $"{slotName} {source.SourceKind} {sequence.MarkerGroupId}"
                        : $"{slotName} {source.SourceKind}";
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(
                            "pose-source:" + GlobalObjectId.GetGlobalObjectIdSlow(source.Slot)),
                        "Locomotion / Pose Sources",
                        label,
                        m_Window.m_Profile.name,
                        string.Empty,
                        default,
                        detail));
                }
                IReadOnlyList<AnimationProducerAuthoringEntry> producers;
                try
                {
                    producers =
                        CharacterAnimationPresentationAuthoringService
                            .DiscoverProducers(
                                m_Window.m_Profile,
                                m_Window.m_Definition);
                }
                catch (Exception exception)
                {
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(
                            "unavailable:producer-catalog"),
                        "Action / Producers",
                        "Unavailable: producer composition is invalid",
                        m_Window.m_Definition.name,
                        string.Empty,
                        default,
                        exception.Message));
                    return items;
                }
                foreach (AnimationProducerAuthoringEntry producer in
                         producers.OrderBy(
                             value => value.ProgramProducerIdentity,
                             StringComparer.Ordinal))
                {
                    string identity =
                        producer.ProducerId.TimelineAuthoringId +
                        "/" +
                        producer.ProducerId.TrackAuthoringId;
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(
                            "producer:" + identity),
                        "Action / Producers",
                        producer.DisplayName,
                        producer.ProducerId.TimelineAuthoringId,
                        producer.AnimationChannelId.ToString(),
                        default,
                        $"{identity} {producer.AnimationChannelId}"));
                }
                return items;
            }

            public void Open(
                IGraphAuthoringDocumentProjection document,
                GraphAuthoringNavigatorItem item)
            {
                if (m_Window.m_Asset.TryGetGraph(
                        new PoseGraphId(item.ItemId.Value),
                        out _))
                {
                    m_Window.OpenGraph(
                        new PoseGraphId(item.ItemId.Value));
                    return;
                }
                m_Window.OpenTransitionRuleFromNavigator(
                    item.ItemId.Value);
            }
        }
    }
}
