using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed partial class CharacterPresentationPoseGraphEditorWindow : EditorWindow
    {
        const string WorkspaceVisualTreePath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.uxml";
        const string WorkspaceStylePath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.uss";

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
        Label m_Title;
        Label m_Status;
        ToolbarToggle m_LiveDebugToggle;
        CharacterPoseGraphAssetMutationOwner m_Owner;
        CharacterTypedPoseGraphDocument m_Document;
        CharacterPoseGraphEditorMutationAdapter m_Mutation;
        CharacterPoseRuntimeTraceProjection m_RuntimeTrace;
        CharacterPosePreviewViewport m_PreviewPanel;
        CharacterPoseStateMachineDocument m_StateMachineDocument;
        CharacterPoseStateMachineEditorMutationAdapter m_StateMachineMutation;
        CharacterPoseTransitionRuleDocument m_RuleDocument;
        CharacterPoseTransitionRuleMutationAdapter m_RuleMutation;
        CharacterLinkedPoseAuthoringWorkspacePresenter m_LinkedPoseWorkspace;
        VisualElement m_LinkedPoseDetails;
        VisualElement m_SelectionTuningHost;
        string m_LinkedPoseSelectionId = string.Empty;
        string m_LinkedPoseWorkspaceStatus = "Unavailable";
        readonly GraphAuthoringPageStack m_PageStack =
            new GraphAuthoringPageStack();
        bool m_ShowingStateMachine;
        bool m_ShowingTransitionRule;
        GraphAuthoringSelectionBinding m_SelectionBinding;
        GraphAuthoringSelection? m_LastSelection;
        string m_LastContentRevision = string.Empty;
        readonly Guid m_DiagnosticsInterestOwnerId = Guid.NewGuid();
        AnimationPresentationRuntimeTarget m_DiagnosticsInterestTarget;

        internal CharacterPipelineDefinition DefinitionContext => m_Definition;
        internal CharacterAnimationPresentationProfile ProfileContext => m_Profile;
        internal CharacterPresentationProjectionAsset ProjectionContext => m_Projection;
        internal CharacterPresentationPoseGraphAsset AssetContext => m_Asset;
        internal string CurrentStateMachineId => m_StateMachineDocument?.DocumentId ?? string.Empty;
        internal CharacterPipelineDefinition DefinitionContextValue => m_Definition;
        internal bool IsLinkedPoseReadOnly => m_LiveDebugToggle != null && m_LiveDebugToggle.value;
        internal string LinkedPoseWorkspaceStatus => m_LinkedPoseWorkspaceStatus;

        public static CharacterPresentationPoseGraphEditorWindow Open(
            CharacterAnimationPresentationProfile profile)
        {
            if (!profile || !profile.PoseGraph || !profile.RigDefinition)
                throw new ArgumentException(
                    "Animation Presentation Profile requires one Pose Graph and Rig Definition.",
                    nameof(profile));
            CharacterAnimationPreviewFixture[] fixtures =
                CharacterAnimationPreviewFixtureCatalog.Load()
                    .Where(value =>
                        value &&
                        value.Profile == profile &&
                        value.Definition &&
                        value.Definition.AnimationPresentationProfile ==
                            profile)
                    .ToArray();
            if (fixtures.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Animation Presentation Profile '{profile.name}' requires exactly one formal Preview Fixture; found {fixtures.Length}.");
            }
            CharacterPipelineDefinition definition =
                fixtures[0].Definition;
            if (!definition.PresentationProjection)
                throw new InvalidOperationException(
                    $"Character Definition '{definition.name}' has no published Presentation Projection.");
            return Open(
                profile.PoseGraph,
                profile,
                definition.PresentationProjection,
                definition);
        }

        public static CharacterPresentationPoseGraphEditorWindow Open(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationProjectionAsset projection,
            CharacterPipelineDefinition definition)
        {
            if (!asset || asset.Graph == null || !asset.Graph.GraphId.IsValid)
                throw new ArgumentException("Presentation Pose Graph is missing typed authoring data.", nameof(asset));
            if (!profile || !projection || !definition || !profile.RigDefinition)
                throw new InvalidOperationException(
                    "Pose Graph requires one exact Definition, Presentation Profile, Rig Definition and published Projection context.");
            if (definition.AnimationPresentationProfile != profile)
                throw new InvalidOperationException("Character Definition does not own the selected Presentation Profile.");
            if (definition.PresentationProjection != projection)
                throw new InvalidOperationException("Character Definition does not own the selected Presentation Projection.");
            CharacterPresentationPoseGraphEditorWindow window = GetWindow<CharacterPresentationPoseGraphEditorWindow>();
            window.minSize = new Vector2(1280f, 760f);
            window.titleContent = new GUIContent("Presentation Pose Graph");
            window.SetDocument(asset, profile, projection, definition);
            window.Show();
            window.Focus();
            return window;
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
            VisualElement previewHost = Require("pose-preview-content");
            VisualElement navigatorHost = Require("pose-navigator-content");
            VisualElement canvasHost = Require("pose-graph-content");
            VisualElement detailsHost = Require("pose-details-content");

            m_Canvas = new GraphAuthoringCanvasView
            {
                name = "tree-view"
            };
            m_StateMachineSurface = new GraphAuthoringCanvasView();
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
            m_PreviewPanel = new CharacterPosePreviewViewport(this);

            canvasHost.Add(m_Canvas);
            canvasHost.Add(m_StateMachineSurface);
            previewHost.Add(m_PreviewPanel.View);
            navigatorHost.Add(m_Navigator);
            detailsHost.Add(m_Details);
            detailsHost.Add(m_SelectionTuningHost);
            detailsHost.Add(m_LinkedPoseDetails);

            m_Title = rootVisualElement.Q<Label>("pose-document-title");
            m_Status = rootVisualElement.Q<Label>("pose-authoring-status");
            toolbar.Add(new Button(ValidateAuthoring) { text = "Validate" });
            toolbar.Add(new Button(CompileSemanticIr) { text = "Compile" });
            toolbar.Add(new Button(BuildDefinition) { text = "Build" });
            m_LiveDebugToggle = new ToolbarToggle
            {
                text = "Live"
            };
            m_LiveDebugToggle.RegisterValueChangedCallback(evt => SetLiveDebug(evt.newValue));
            toolbar.Add(m_LiveDebugToggle);
            toolbar.Add(m_PreviewPanel.TargetField);

            m_BreadcrumbHost = new GraphAuthoringBreadcrumbHost(
                rootVisualElement.Q<Button>("pose-navigation-back-button"),
                rootVisualElement.Q("pose-navigation-breadcrumb"));
            m_BreadcrumbHost.BindBack(() =>
                NavigateToPage(m_PageStack.Pages.Count - 2));
            m_Canvas.NodeCreationRequested += ShowCreateMenu;
            m_Canvas.ChildSurfaceRequested += OpenChildSurface;
            m_StateMachineSurface.StateMachineNodeCreationRequested +=
                ShowStateMachineCreateMenu;
            m_SelectionBinding = new GraphAuthoringSelectionBinding(
                rootVisualElement,
                PublishSelection);
            m_UndoBinding = new GraphAuthoringUndoBinding(ReloadAfterUndoRedo);
            RuntimeDebugSession.Shared.Changed += OnRuntimeDebugChanged;
            BindCurrentGraph(true);
        }

        void OnDisable()
        {
            m_UndoBinding?.Dispose();
            m_UndoBinding = null;
            m_SelectionBinding?.Dispose();
            m_SelectionBinding = null;
            RuntimeDebugSession.Shared.Changed -= OnRuntimeDebugChanged;
            RuntimeDebugSession.Shared.ReleaseLiveInterest(this);
            ReleaseDiagnosticsInterest();
            m_PreviewPanel?.Unbind();
            m_BreadcrumbHost?.Dispose();
            m_BreadcrumbHost = null;
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
            CharacterPresentationProjectionAsset projection,
            CharacterPipelineDefinition definition)
        {
            m_Asset = asset;
            m_Profile = profile;
            m_Projection = projection;
            m_Definition = definition;
            ResetPoseTuningAuthoringState();
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

        public void FocusGraph(PoseGraphId graphId)
        {
            if (!graphId.IsValid || !m_Asset || !m_Asset.TryGetGraph(graphId, out _))
                return;
            OpenGraph(graphId);
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
            m_Mutation = new CharacterPoseGraphEditorMutationAdapter(
                new CharacterTypedPoseGraphMutationAdapter(),
                m_PreviewPanel.TryApplySelectionTuning);
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
                    m_Profile,
                    m_PreviewPanel.GetAppliedValues),

                OpenDetailsCommand,
                true));
            m_Details.style.display = DisplayStyle.Flex;
            m_LinkedPoseDetails.style.display = DisplayStyle.None;
            m_Navigator.Bind(m_Document, new NavigatorDataSource(this));
            m_PreviewPanel.Rebind(m_Document);
            m_Title.text = $"{m_Asset.name} / {graphDisplayName}";
            RefreshLinkedPoseWorkspaceStatus();
            m_LastContentRevision = graph.ContentRevision;
            CapturePublishedPoseGraphRevision(graph);
            RefreshPublishedStatus();
            m_LastSelection = null;
            RefreshSelectionTuning(null);
            if (resetPages || m_PageStack.Pages.Count == 0)
            {
                m_PageStack.Reset(new GraphAuthoringPageProjection(
                    new GraphAuthoringElementId(graph.GraphId.Value),
                    graphDisplayName,
                    ResolveRole(graph).Value));
            }
            RenderBreadcrumb();
            if (!string.IsNullOrEmpty(m_LinkedPoseSelectionId))
                ShowLinkedPoseSelection(m_LinkedPoseSelectionId);
            RefreshRuntimeHighlight();
            if (resetPages)
            {
                rootVisualElement.schedule.Execute(() =>
                {
                    if (m_Canvas != null && !m_ShowingStateMachine)
                        m_Canvas.FrameAll();
                });
            }
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
            if (kind == CharacterPoseNodeKind.LinkedPoseCall)
            {
                ShowLinkedPoseSelection("linked-root");
                m_Status.text = "Linked Pose Call is created from the typed Group/Entry authoring page.";
                return;
            }
            CharacterPoseNodePayload payload =
                (CharacterPoseNodePayload)Activator.CreateInstance(
                    CharacterPoseGraphAuthoringCapabilities
                        .RequirePayloadType(kind));
            var node = new CharacterTypedPoseNode(new PoseNodeId(Guid.NewGuid().ToString("N")), capability.DisplayName, payload);
            Vector2 graphPosition = m_Canvas.contentViewContainer.WorldToLocal(screenPosition - position.position);
            m_Canvas.CreateNode(capability.CapabilityId, node, graphPosition);
            m_Status.text =
                "Authoring changed · published Projection is Stale until explicit Build.";
            RefreshSelectedDetails();
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
                RefreshSelectedDetails();
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
            {
                if (!m_ShowingTransitionRule && m_Document != null &&
                    current.Value.Kind == GraphAuthoringSelectionKind.Node &&
                    m_Document.Graph.Nodes.FirstOrDefault(value =>
                        value.NodeId.Value == current.Value.ElementId.Value)?.Kind ==
                    CharacterPoseNodeKind.LinkedPoseCall)
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
            bool hasInlineTuning =
                m_PreviewPanel?.PopulateSelectionTuning(
                    selection,
                    m_SelectionTuningHost) ?? false;
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
                CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                    node.CapabilityId,
                    m_Document.DomainId,
                    m_Document.DocumentRoleId);
            GraphAuthoringChildSurfaceDescriptor child =
                capability.ChildSurfaces.Single(value =>
                    value.CommandId.Equals(request.CommandId));
            OpenChildSurface(node, child);
        }

        bool TryOpenFullBodyIkProfile(
            GraphAuthoringDetailsCommandRequest request,
            GraphAuthoringElementId nodeId)
        {
            if (request.Kind != GraphAuthoringMutationKind.ExecuteCommand ||
                !request.CommandId.Equals(
                    CharacterPoseGraphAuthoringCapabilities.OpenFullBodyIkProfile))
                return false;
            CharacterTypedPoseNode typed =
                m_Document.Graph.Nodes.Single(value =>
                    value.NodeId.Value == nodeId.Value);
            CharacterFullBodyIkPosePayload payload =
                typed.Payload as CharacterFullBodyIkPosePayload ??
                throw new InvalidOperationException(
                    $"Pose node '{typed.NodeId}' is not a Full Body IK node.");
            CharacterFullBodyIkProfile profile = payload.Profile;
            if (!profile)
                throw new InvalidOperationException(
                    $"Full Body IK node '{typed.NodeId}' has no Profile.");
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

            if (openSource && binding is CharacterClipPoseSourceBinding clipBinding && clipBinding.Clip)
                CharacterAnimationClipAuthoringService.Open(new CharacterAnimationClipOpenRequest(
                    m_Definition,
                    m_Profile,
                    clipBinding.Clip,
                    m_PreviewPanel?.PreviewTarget));
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
                new CharacterPoseStateMachineEditorMutationAdapter(
                    new CharacterPoseStateMachineMutationAdapter(),
                    m_PreviewPanel.TryApplySelectionTuning)
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
                new CharacterPoseStateMachineDetailsDataSource(),
                m_PreviewPanel.GetAppliedValues);
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
            if (pushPage)
            {
                m_PageStack.Push(
                    m_StateMachineDocument.Pages[0]);
            }
            RenderBreadcrumb();
            RefreshRuntimeHighlight();
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
            m_Title.text =
                $"{m_Asset.name} / Transition Rule / " +
                m_RuleDocument.DisplayName;
            RefreshPublishedStatus();
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
                RefreshSelectedDetails();
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
            if (!ValidateAuthoringAndLocate())
                return;
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
            ValidateAuthoringAndLocate();
        }

        bool ValidateAuthoringAndLocate()
        {
            if (!m_Asset)
                return false;
            ClearValidationHighlights();
            IReadOnlyList<string> capabilityErrors =
                CharacterPoseGraphCapabilityValidator.Validate(m_Asset);
            CharacterPoseGraphValidationReport report =
                CharacterPresentationPoseGraphValidator.Validate(
                    m_Asset,
                    m_Profile ? m_Profile.RigDefinition : null,
                    CharacterPoseAuthoringPortProjection.Get);
            int issueCount = capabilityErrors.Count + report.Issues.Count;
            if (TryFindStateMachineValidationIssue(
                    out CharacterTypedPoseGraph ownerGraph,
                    out CharacterTypedPoseNode ownerNode,
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
            out CharacterTypedPoseGraph ownerGraph,
            out CharacterTypedPoseNode ownerNode,
            out CharacterPoseStateMachineDefinition machine,
            out CharacterPoseStateMachineValidationIssue issue)
        {
            foreach (CharacterTypedPoseGraph graph in
                     m_Asset.EnumerateGraphs())
            {
                if (graph == null)
                    continue;
                foreach (CharacterTypedPoseNode node in graph.Nodes)
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
            CharacterTypedPoseGraph ownerGraph,
            CharacterTypedPoseNode ownerNode,
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
            bool pushPage = !m_ShowingStateMachine ||
                            m_StateMachineDocument?.Definition != machine;
            OpenStateMachine(machine, pushPage);
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
                    out CharacterTypedPoseGraph graph))
                return string.Empty;
            CharacterTypedPoseNode node = graph.Nodes
                .SingleOrDefault(value =>
                    value != null && value.NodeId == nodeId);
            if (node == null)
                return string.Empty;
            return CharacterPoseAuthoringPortProjection.Get(node)
                       .SingleOrDefault(value =>
                           value.PortId.Equals(portId))
                       ?.Name ?? string.Empty;
        }

        static void AddValidationHighlight(
            GraphAuthoringCanvasView canvas,
            GraphAuthoringElementId elementId,
            string portName = "")
        {
            if (canvas == null)
                return;
            foreach (GraphElement element in canvas.graphElements)
            {
                if (string.Equals(
                        element.viewDataKey,
                        elementId.Value,
                        StringComparison.Ordinal))
                    continue;
                element.AddToClassList("pose-validation-error");
                if (string.IsNullOrEmpty(portName))
                    continue;
                foreach (Port port in element.Query<Port>().ToList())
                {
                    if (string.Equals(
                            port.portName,
                            portName,
                            StringComparison.Ordinal))
                        port.AddToClassList("pose-validation-error");
                }
            }
        }

        void ClearValidationHighlights()
        {
            ClearValidationHighlights(m_Canvas);
            ClearValidationHighlights(m_StateMachineSurface);
        }

        static void ClearValidationHighlights(
            GraphAuthoringCanvasView canvas)
        {
            if (canvas == null)
                return;
            foreach (VisualElement element in
                     canvas.Query<VisualElement>(
                         className:
                         "pose-validation-error").ToList())
                element.RemoveFromClassList("pose-validation-error");
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
                bool built = CharacterSimulationProgramBuildService.Build(m_Definition, true);
                RefreshLinkedPoseWorkspaceStatus();
                m_Status.text = built
                    ? $"Build completed and published. · {m_LinkedPoseWorkspaceStatus}"
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

        void ReloadAfterUndoRedo()
        {
            Reload();
            m_PreviewPanel?.RebuildCandidateAfterUndoRedo();
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
            RefreshLinkedPoseWorkspaceStatus();
            m_Status.text = enabled
                ? "Live Debug · authoring mutation disabled."
                : CurrentPublishedStatus();
            if (m_LastSelection.HasValue)
                m_Details?.Inspect(m_LastSelection.Value);
            RefreshSelectedDetails();
            RefreshRuntimeHighlight();
        }

        void OnRuntimeDebugChanged()
        {
            if (m_LiveDebugToggle == null ||
                !m_LiveDebugToggle.value)
                return;
            SynchronizeDiagnosticsInterest();
            if (m_LastSelection.HasValue)
                m_Details?.Inspect(m_LastSelection.Value);
            RefreshRuntimeHighlight();
        }

        internal void RefreshRuntimeHighlight()
        {
            GraphAuthoringCanvasView canvas = m_ShowingStateMachine
                ? m_StateMachineSurface
                : m_Canvas;
            if (canvas == null)
                return;
            var active = new HashSet<string>(StringComparer.Ordinal);
            if (TryGetRuntimeSnapshot(
                    out AnimationPresentationRuntimeSnapshot snapshot,
                    out _))
            {
                if (m_ShowingStateMachine &&
                    m_StateMachineDocument != null)
                {
                    for (int i = 0;
                         i < snapshot.PoseStateMachines.Count;
                         i++)
                    {
                        PoseStateMachineRuntimeSnapshot stateMachine =
                            snapshot.PoseStateMachines[i];
                        if (!stateMachine.StateMachineId.Equals(
                                m_StateMachineDocument.Definition
                                    .StateMachineId))
                            continue;
                        if (stateMachine.ActiveStateId.IsValid)
                            active.Add(
                                stateMachine.ActiveStateId.Value);
                        if (stateMachine.TargetStateId.IsValid)
                            active.Add(
                                stateMachine.TargetStateId.Value);
                        if (stateMachine.ActiveTransitionId.IsValid)
                            active.Add(
                                stateMachine.ActiveTransitionId.Value);
                    }
                }
                else
                {
                    for (int i = 0; i < snapshot.Operations.Count; i++)
                    {
                        AnimationPoseOperationSnapshot operation =
                            snapshot.Operations[i];
                        if (operation.NodeId.IsValid &&
                            string.Equals(
                                operation.GraphId,
                                m_CurrentGraphId,
                                StringComparison.Ordinal))
                        {
                            active.Add(operation.NodeId.Value);
                        }
                    }
                }
            }
            foreach (GraphElement element in canvas.graphElements)
            {
                bool isActive = !string.IsNullOrEmpty(
                                    element.viewDataKey) &&
                                active.Contains(element.viewDataKey);
                element.EnableInClassList(
                    "pose-runtime-active",
                    isActive);
            }
        }

        internal bool TryGetPublishedPosePlan(
            out CharacterPresentationPosePlan plan,
            out string status)
        {
            plan = null;
            if (!TryGetPublishedProjection(
                    out CharacterPresentationProjection projection,
                    out status))
                return false;
            plan = projection.PosePlan;
            if (!m_Asset || m_Asset.Graph == null ||
                !string.Equals(
                    plan.PoseGraphId,
                    m_Asset.Graph.GraphId.Value,
                    StringComparison.Ordinal) ||
                (!string.Equals(
                     plan.ContentRevision,
                     m_Asset.Graph.ContentRevision,
                     StringComparison.Ordinal) &&
                 (!IsTuningOnlyAuthoringChange() ||
                  !string.Equals(
                      plan.ContentRevision,
                      m_LastPublishedPoseGraphRevision,
                      StringComparison.Ordinal))))
            {
                plan = null;
                status =
                    "Stale: published Pose Plan does not match current authoring. Run explicit Build.";
                return false;
            }
            status = IsTuningOnlyAuthoringChange()
                ? "Unpublished Parameter · published Projection remains active."
                : "Ready";
            return true;
        }

        internal bool TryGetPublishedProjection(
            out CharacterPresentationProjection projection,
            out string status)
        {
            projection = null;
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
                projection = m_Projection.Load(contract);
            }
            catch (Exception exception)
            {
                status =
                    $"Unavailable: published Pose Plan cannot be loaded: {exception.Message}";
                return false;
            }
            status = "Ready";
            return true;
        }

        void RefreshPublishedStatus()
        {
            if (m_Status != null)
                m_Status.text = CurrentPublishedStatus();
        }

        string CurrentPublishedStatus()
        {
            TryGetPublishedPosePlan(out _, out string status);
            return status;
        }

        internal bool MatchesCurrentPublishedRevision(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            if (!m_Asset || m_Asset.Graph == null || !m_Projection ||
                !string.Equals(
                    snapshot.PoseGraphId,
                    m_Asset.Graph.GraphId.Value,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    snapshot.ProjectionRevision,
                    m_Projection.ProjectionRevision,
                    StringComparison.Ordinal))
                return false;
            return string.Equals(
                       snapshot.PoseGraphRevision,
                       m_Asset.Graph.ContentRevision,
                       StringComparison.Ordinal) ||
                   (IsTuningOnlyAuthoringChange() &&
                    string.Equals(
                        snapshot.PoseGraphRevision,
                        m_LastPublishedPoseGraphRevision,
                        StringComparison.Ordinal));
        }

        internal bool TryGetCompiledLinkedPosePreviewCatalog(
            out IReadOnlyList<CharacterLinkedPosePreviewGroupOption> options,
            out string status)
        {
            options = Array.Empty<CharacterLinkedPosePreviewGroupOption>();
            if (!TryGetPublishedPosePlan(out _, out status))
                return false;
            return CharacterLinkedPoseAuthoringService.TryGetCompiledPreviewCatalog(
                m_Definition,
                m_Profile,
                m_Projection,
                out options,
                out status);
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
            if (!TryGetPublishedPosePlan(out _, out string status))
            {
                bool stale = status.StartsWith("Stale", StringComparison.Ordinal);
                m_LinkedPoseWorkspaceStatus = stale
                    ? dirty ? "Dirty · Stale" : "Stale"
                    : dirty ? "Dirty · Invalid" : "Invalid";
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
            CharacterPresentationPoseGraphEditorWindow window =
                CharacterPresentationPoseGraphEditorWindow.Open(
                    graphOwner,
                    m_Profile,
                    m_Projection,
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

        internal bool TryGetRuntimeSnapshot(
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

        internal void RefreshRuntimeDetails() =>
            RefreshSelectedDetails();

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
                    foreach (CharacterTypedPoseNode call in (m_Window.m_Asset.Graph?.Nodes ?? Array.Empty<CharacterTypedPoseNode>())
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
                            int callCount = (m_Window.m_Asset.Graph?.Nodes ?? Array.Empty<CharacterTypedPoseNode>())
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
                const string stateMachinePrefix = "state-machine:";
                if (item.ItemId.Value.StartsWith(
                        stateMachinePrefix,
                        StringComparison.Ordinal))
                {
                    string stateMachineId = item.ItemId.Value.Substring(
                        stateMachinePrefix.Length);
                    CharacterPoseStateMachineDefinition machine =
                        m_Window.m_Asset.EnumerateStateMachines()
                            .Single(value =>
                                value.StateMachineId.Value ==
                                stateMachineId);
                    m_Window.OpenStateMachine(machine, true);
                    return;
                }
                var graphId = new PoseGraphId(item.ItemId.Value);
                if (m_Window.m_Asset.TryGetGraph(graphId, out _))
                {
                    m_Window.OpenGraph(graphId);
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
