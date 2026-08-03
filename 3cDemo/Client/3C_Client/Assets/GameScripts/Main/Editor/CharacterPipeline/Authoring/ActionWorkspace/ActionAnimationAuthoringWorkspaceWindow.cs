using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class ActionAnimationAuthoringWorkspaceWindow : EditorWindow
    {
        enum DetailsPage
        {
            Identity,
            Gameplay,
            Animation,
            SlotBlend,
            References
        }

        enum BottomPage
        {
            Preview,
            Live,
            Diagnostics
        }

        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] string m_ActionId = string.Empty;
        [SerializeField] string m_TimelineAuthoringId = string.Empty;
        [SerializeField] string m_TrackAuthoringId = string.Empty;
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] DetailsPage m_DetailsPage;
        [SerializeField] BottomPage m_BottomPage;
        [SerializeField] bool m_DetailsExpanded = true;
        [SerializeField] bool m_BottomExpanded = true;
        [SerializeField] bool m_LiveMode;

        ObjectField m_DefinitionField;
        ToolbarMenu m_ActionMenu;
        Label m_TimelineTitle;
        ToolbarToggle m_PreviewToggle;
        ToolbarToggle m_LiveToggle;
        VisualElement m_StatusHost;
        VisualElement m_TimelineHost;
        VisualElement m_DetailsHost;
        VisualElement m_BottomHost;
        GraphAuthoringBreadcrumbHost m_BreadcrumbHost;
        TimelineEditorView m_TimelineView;
        ActionAnimationWorkspaceResolution m_Resolution;
        TimelineEditorSelection m_TimelineSelection;
        RuntimeDebugViewBinding m_RuntimeBinding;
        RuntimeDebugTargetResolution m_RuntimeResolution;
        ActionAnimationWorkspaceLiveView m_LiveView;
        string m_LiveFailure = string.Empty;
        ActionAnimationWorkspacePreviewView m_PreviewView;
        string m_PreviewFailure = string.Empty;
        CharacterSimulationCompileReport m_DryRunReport;
        string m_ExplicitBuildStatus = string.Empty;
        readonly Guid m_AnimationDiagnosticsOwnerId = Guid.NewGuid();
        AnimationPresentationRuntimeTarget m_AnimationDiagnosticsTarget;

        [MenuItem("Tools/3C/Character/Action Animation Workspace")]
        public static void OpenStandalone()
        {
            ActionAnimationAuthoringWorkspaceWindow window =
                GetWindow<ActionAnimationAuthoringWorkspaceWindow>();
            window.titleContent = new GUIContent("Action Animation");
            window.Show();
            window.Focus();
        }

        public static ActionAnimationAuthoringWorkspaceWindow Open(
            ActionAnimationWorkspaceOpenRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            ActionAnimationAuthoringWorkspaceWindow window =
                GetWindow<ActionAnimationAuthoringWorkspaceWindow>();
            window.m_Definition = request.Definition;
            window.m_ActionId = request.ActionId;
            window.m_TimelineAuthoringId = request.TimelineAuthoringId;
            window.m_TrackAuthoringId = request.TrackAuthoringId;
            window.m_SlotId = request.SlotId;
            window.titleContent = new GUIContent("Action Animation");
            window.Rebuild();
            window.Show();
            window.Focus();
            return window;
        }

        public void CreateGUI()
        {
            titleContent = new GUIContent("Action Animation");
            Rebuild();
        }

        void OnEnable()
        {
            RuntimeDebugSession.Shared.Changed +=
                OnRuntimeDebugSessionChanged;
        }

        void OnDisable()
        {
            RuntimeDebugSession.Shared.Changed -=
                OnRuntimeDebugSessionChanged;
            m_RuntimeBinding?.Dispose(
                RuntimeDebugSession.Shared);
            m_RuntimeBinding = null;
            ReleaseAnimationDiagnosticsInterest();
            m_BreadcrumbHost?.Dispose();
            m_BreadcrumbHost = null;
            DisposeTimeline();
        }

        void Rebuild()
        {
            if (rootVisualElement == null)
                return;
            DisposeTimeline();
            m_BreadcrumbHost?.Dispose();
            m_BreadcrumbHost = null;
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(
                "VisualTree/BaseTreeWindow");
            if (!visualTree)
                throw new InvalidOperationException(
                    "Graph Authoring workspace visual tree is missing.");
            visualTree.CloneTree(rootVisualElement);

            VisualElement toolbarHost =
                RequireHost("workspace-toolbar-content");
            VisualElement navigatorRegion =
                RequireHost("workspace-navigator");
            VisualElement graphHost =
                RequireHost("workspace-graph-content");
            VisualElement detailsHost =
                RequireHost("workspace-details-content");
            VisualElement bottomHost =
                RequireHost("workspace-bottom-content");
            navigatorRegion.style.display = DisplayStyle.None;
            rootVisualElement.Q<Label>("workspace-details-title").text =
                "Action Details";
            rootVisualElement.Q<Label>("workspace-bottom-title").text =
                "Preview / Live / Diagnostics";

            toolbarHost.Add(CreateTopBar());
            m_StatusHost = new VisualElement();
            graphHost.Add(m_StatusHost);
            m_TimelineHost = new VisualElement();
            m_TimelineHost.style.flexGrow = 1f;
            m_TimelineHost.style.minWidth = 520f;
            graphHost.Add(m_TimelineHost);

            var actionDetails = new ActionAnimationDetailsHost();
            detailsHost.Add(actionDetails);
            actionDetails.Content.Add(CreateDetailsHeader());
            m_DetailsHost = new VisualElement();
            m_DetailsHost.style.flexGrow = 1f;
            actionDetails.Content.Add(m_DetailsHost);

            bottomHost.Add(CreateBottomHeader());
            m_BottomHost = new ScrollView();
            m_BottomHost.style.flexGrow = 1f;
            m_BottomHost.style.display = m_BottomExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            bottomHost.Add(m_BottomHost);

            m_BreadcrumbHost = new GraphAuthoringBreadcrumbHost(
                rootVisualElement.Q<Button>(
                    "tree-navigation-back-button"),
                rootVisualElement.Q(
                    "tree-navigation-breadcrumb"));
            m_BreadcrumbHost.BindBack(() =>
                NavigateBreadcrumb(BreadcrumbCount() - 2));

            ResolveAndBind();
        }

        VisualElement CreateTopBar()
        {
            var toolbar = new Toolbar();
            m_DefinitionField = new ObjectField
            {
                objectType = typeof(CharacterPipelineDefinition),
                allowSceneObjects = false,
                value = m_Definition
            };
            m_DefinitionField.style.width = 240f;
            m_DefinitionField.RegisterValueChangedCallback(evt =>
            {
                CharacterPipelineDefinition definition =
                    evt.newValue as CharacterPipelineDefinition;
                if (ReferenceEquals(definition, m_Definition))
                    return;
                m_Definition = definition;
                m_ActionId = string.Empty;
                ClearExactSelectors();
                ResolveAndBind();
            });
            toolbar.Add(new Label("Definition"));
            toolbar.Add(m_DefinitionField);

            m_ActionMenu = new ToolbarMenu { text = "Action" };
            m_ActionMenu.style.minWidth = 180f;
            toolbar.Add(m_ActionMenu);

            m_TimelineTitle = new Label("Timeline: —");
            m_TimelineTitle.style.flexGrow = 1f;
            m_TimelineTitle.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(m_TimelineTitle);

            m_PreviewToggle = new ToolbarToggle
            {
                text = "Preview",
                value = !m_LiveMode
            };
            m_PreviewToggle.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue)
                {
                    if (!m_LiveMode)
                        m_PreviewToggle.SetValueWithoutNotify(true);
                    return;
                }
                m_LiveMode = false;
                ReleaseAnimationDiagnosticsInterest();
                m_LiveToggle?.SetValueWithoutNotify(false);
                m_TimelineView?.SetLiveDebug(false);
                m_BottomPage = BottomPage.Preview;
                RefreshPreviewState(false);
                RefreshBottom();
            });
            toolbar.Add(m_PreviewToggle);

            m_LiveToggle = new ToolbarToggle
            {
                text = "Live",
                value = m_LiveMode
            };
            m_LiveToggle.RegisterValueChangedCallback(evt =>
            {
                m_LiveMode = evt.newValue;
                m_PreviewToggle?.SetValueWithoutNotify(!m_LiveMode);
                m_TimelineView?.SetLiveDebug(m_LiveMode);
                if (m_LiveMode)
                    m_BottomPage = BottomPage.Live;
                RefreshLiveState();
                RefreshBottom();
            });
            toolbar.Add(m_LiveToggle);
            return toolbar;
        }

        VisualElement CreateDetailsHeader()
        {
            var column = new VisualElement();
            var title = new Toolbar();
            var expanded = new ToolbarToggle
            {
                text = "Details",
                value = m_DetailsExpanded
            };
            expanded.RegisterValueChangedCallback(evt =>
            {
                m_DetailsExpanded = evt.newValue;
                if (m_DetailsHost != null)
                    m_DetailsHost.style.display =
                        m_DetailsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            });
            title.Add(expanded);
            column.Add(title);
            var pages = new Toolbar();
            AddDetailsButton(pages, "Identity", DetailsPage.Identity);
            AddDetailsButton(pages, "Gameplay", DetailsPage.Gameplay);
            AddDetailsButton(pages, "Animation", DetailsPage.Animation);
            AddDetailsButton(pages, "Slot", DetailsPage.SlotBlend);
            AddDetailsButton(pages, "Refs", DetailsPage.References);
            column.Add(pages);
            return column;
        }

        void AddDetailsButton(
            VisualElement parent,
            string label,
            DetailsPage page)
        {
            parent.Add(new ToolbarButton(() =>
            {
                m_DetailsPage = page;
                RefreshDetails();
            }) { text = label });
        }

        VisualElement CreateBottomHeader()
        {
            var toolbar = new Toolbar();
            var expanded = new ToolbarToggle
            {
                text = "Dock",
                value = m_BottomExpanded
            };
            expanded.RegisterValueChangedCallback(evt =>
            {
                m_BottomExpanded = evt.newValue;
                if (m_BottomHost != null)
                    m_BottomHost.style.display =
                        m_BottomExpanded
                            ? DisplayStyle.Flex
                            : DisplayStyle.None;
            });
            toolbar.Add(expanded);
            AddBottomButton(toolbar, "Preview", BottomPage.Preview);
            AddBottomButton(toolbar, "Live", BottomPage.Live);
            AddBottomButton(toolbar, "Diagnostics", BottomPage.Diagnostics);
            return toolbar;
        }

        void AddBottomButton(
            VisualElement parent,
            string label,
            BottomPage page)
        {
            parent.Add(new ToolbarButton(() =>
            {
                m_BottomPage = page;
                RefreshBottom();
            }) { text = label });
        }

        void ResolveAndBind()
        {
            DisposeTimeline();
            InvalidateLiveState();
            m_Resolution = null;
            m_TimelineSelection = default;
            m_StatusHost?.Clear();
            m_TimelineHost?.Clear();
            ConfigureActionMenu();
            if (!m_Definition || string.IsNullOrWhiteSpace(m_ActionId))
            {
                AddStatus(
                    !m_Definition
                        ? "选择精确 Character Definition。"
                        : "选择 Definition 已登记的 Action。",
                    false);
                m_TimelineTitle.text = "Timeline: —";
                RenderBreadcrumb();
                RefreshDetails();
                RefreshBottom();
                return;
            }

            try
            {
                var request = new ActionAnimationWorkspaceOpenRequest(
                    m_Definition,
                    m_ActionId,
                    m_TimelineAuthoringId,
                    m_TrackAuthoringId,
                    m_SlotId);
                m_Resolution =
                    ActionAnimationAuthoringWorkspaceResolver.Resolve(request);
            }
            catch (Exception exception)
            {
                AddStatus(exception.Message, true);
                RenderBreadcrumb();
                RefreshDetails();
                RefreshBottom();
                return;
            }

            for (int i = 0; i < m_Resolution.Failures.Count; i++)
                AddFailure(m_Resolution.Failures[i]);
            if (m_Resolution.Failures.Count == 0)
                AddStatus("Typed session 已完整解析，所有关系均指向正式 owner。", false);

            if (m_Resolution.Timeline != null)
            {
                BindTimeline(m_Resolution.Timeline);
                m_TimelineTitle.text =
                    $"Timeline: {m_Resolution.Timeline.Timeline.AuthoringId}";
            }
            else
            {
                m_TimelineTitle.text = "Timeline: unresolved";
                AddTimelinePlaceholder(
                    "有限 Action Timeline 未唯一解析，Timeline Core 未绑定。");
            }
            RenderBreadcrumb();
            RefreshLiveState();
            RefreshPreviewState(false);
            RefreshDetails();
            RefreshBottom();
        }

        void ConfigureActionMenu()
        {
            if (m_ActionMenu == null)
                return;
            m_ActionMenu.menu.MenuItems().Clear();
            m_ActionMenu.text = string.IsNullOrWhiteSpace(m_ActionId)
                ? "Action"
                : m_ActionId;
            if (!m_Definition)
                return;
            foreach (ActionProfile profile in m_Definition.ActionProfiles
                         .Where(value => value)
                         .OrderBy(value => value.ActionId, StringComparer.Ordinal))
            {
                ActionProfile captured = profile;
                m_ActionMenu.menu.AppendAction(
                    string.IsNullOrWhiteSpace(captured.DisplayName)
                        ? captured.ActionId
                        : $"{captured.DisplayName} ({captured.ActionId})",
                    _ =>
                    {
                        m_ActionId = captured.ActionId;
                        ClearExactSelectors();
                        ResolveAndBind();
                    },
                    _ => string.Equals(
                        m_ActionId,
                        captured.ActionId,
                        StringComparison.Ordinal)
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
            }
        }

        void ClearExactSelectors()
        {
            m_TimelineAuthoringId = string.Empty;
            m_TrackAuthoringId = string.Empty;
            m_SlotId = string.Empty;
        }

        void BindTimeline(ActionAnimationTimelineContext timeline)
        {
            var markerContext =
                new CharacterPipelineAuthoringContext(m_Definition);
            var request = new TimelineEditorOpenRequest(
                timeline.Timeline,
                timeline.SerializedOwner,
                timeline.SerializedPropertyPath,
                timeline.Ownership.ToString(),
                markerContext,
                m_Resolution.RuntimeDebug,
                TimelineEditorToolComposition.Catalog);
            m_TimelineView = new TimelineEditorView();
            m_TimelineView.Init(request);
            m_TimelineView.SetLiveDebug(m_LiveMode);
            m_TimelineView.SessionContext.SelectionChanged +=
                OnTimelineSelectionChanged;
            m_TimelineView.PreviewSession.Evaluated +=
                OnTimelinePreviewEvaluated;
            m_TimelineView.OpenClipRequested +=
                OpenTimelineClip;
            m_TimelineHost.Add(m_TimelineView);
            if (m_Resolution.Producer != null)
                m_TimelineView.FocusSource(
                    m_Resolution.Producer.Track.AuthoringId,
                    string.Empty);
        }

        void DisposeTimeline()
        {
            if (m_TimelineView?.SessionContext != null)
                m_TimelineView.SessionContext.SelectionChanged -=
                    OnTimelineSelectionChanged;
            if (m_TimelineView != null)
            {
                m_TimelineView.PreviewSession.Evaluated -=
                    OnTimelinePreviewEvaluated;
                m_TimelineView.OpenClipRequested -=
                    OpenTimelineClip;
            }
            m_TimelineView?.Dispose();
            m_TimelineView = null;
            m_PreviewView = null;
            m_PreviewFailure = string.Empty;
        }

        void OnTimelineSelectionChanged(TimelineEditorSelection selection)
        {
            m_TimelineSelection = selection;
            RefreshDetails();
        }

        void OpenTimelineClip(Clip clip)
        {
            if (clip is not TreeClip treeClip ||
                treeClip.ResolvedTree == null)
                return;
            BaseTreeWindow window =
                CharacterPipelineDefinitionTreeWindowUtility
                    .OpenRootTree(m_Definition);
            if (!window)
                return;
            string identity =
                $"{treeClip.Track?.AuthoringId}:{treeClip.AuthoringId}";
            window.PushTreePage(
                treeClip.ResolvedTree,
                treeClip.SharedTreeAsset,
                treeClip.Name,
                identity,
                "TreeClip",
                AuthoringPageKind.TreeClip);
            window.Show();
            window.Focus();
        }

        void AddTimelinePlaceholder(string message)
        {
            var label = new Label(message);
            label.style.flexGrow = 1f;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_TimelineHost.Add(label);
        }

        void AddStatus(string message, bool error)
        {
            var label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.paddingLeft = 8f;
            label.style.paddingRight = 8f;
            label.style.paddingTop = 3f;
            label.style.paddingBottom = 3f;
            label.style.color = error
                ? new Color(1f, 0.45f, 0.35f)
                : new Color(0.62f, 0.82f, 0.62f);
            m_StatusHost?.Add(label);
        }

        void AddFailure(ActionAnimationWorkspaceFailure failure)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingLeft = 8f;
            row.style.paddingRight = 8f;
            var label = new Label($"{failure.Code}: {failure.Message}");
            label.style.flexGrow = 1f;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new Color(1f, 0.45f, 0.35f);
            row.Add(label);
            if (failure.Owner)
                row.Add(new Button(() => SelectOwner(failure.Owner))
                {
                    text = "Owner"
                });
            if (!string.IsNullOrEmpty(failure.ElementAuthoringId))
                row.Add(new Button(() => NavigateFailure(failure))
                {
                    text = "Source"
                });
            m_StatusHost.Add(row);
        }

        void RefreshDetails()
        {
            if (m_DetailsHost == null)
                return;
            m_DetailsHost.Clear();
            m_DetailsHost.style.display =
                m_DetailsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_Resolution == null)
            {
                AddDetailText("尚未建立 typed session。");
                return;
            }
            switch (m_DetailsPage)
            {
                case DetailsPage.Identity:
                    DrawIdentityDetails();
                    break;
                case DetailsPage.Gameplay:
                    DrawGameplayDetails();
                    break;
                case DetailsPage.Animation:
                    DrawAnimationDetails();
                    break;
                case DetailsPage.SlotBlend:
                    DrawSlotDetails();
                    break;
                case DetailsPage.References:
                    DrawReferenceDetails();
                    break;
            }
        }

        void DrawIdentityDetails()
        {
            AddDetailHeader("Identity");
            AddDetailText($"Action: {m_Resolution.Action?.ActionId ?? "unresolved"}");
            AddDetailText(
                $"Timeline: {m_Resolution.Timeline?.Timeline.AuthoringId ?? "unresolved"}");
            AddDetailText(
                $"Producer: {m_Resolution.Producer?.ProducerId.ToString() ?? "unresolved"}");
            AddDetailText(
                $"Slot: {m_Resolution.Slot?.SlotId.ToString() ?? "unresolved"}");
            AddDetailText(
                $"Workspace: {m_Resolution.Session?.WorkspaceId.ToString() ?? "incomplete"}");
        }

        void DrawGameplayDetails()
        {
            AddDetailHeader("Gameplay");
            AddOwnerRow(
                "ActionProfile",
                m_Resolution.Action?.Profile,
                () => SelectOwner(m_Resolution.Action?.Profile));
            AddOwnerRow(
                "Call Site Graph",
                m_Resolution.CallSite?.Graph?.SerializedOwner,
                NavigateCallSite);
            AddDetailText(
                $"Call Site: {m_Resolution.CallSite?.Node?.GUID ?? "unresolved"}");
            AddOwnerRow(
                "Action Context",
                m_Resolution.CallSite?.ActionContext,
                () => SelectOwner(m_Resolution.CallSite?.ActionContext));
        }

        void DrawAnimationDetails()
        {
            AddDetailHeader("Animation");
            AddOwnerRow(
                "Timeline Owner",
                m_Resolution.Timeline?.SerializedOwner,
                NavigateTimelineNode);
            AddDetailText(
                $"Track: {m_Resolution.Producer?.Track.AuthoringId ?? "unresolved"}");
            AddDetailText(
                $"Channel: {m_Resolution.Producer?.AnimationChannelId.ToString() ?? "unresolved"}");
            AddOwnerRow(
                "Presentation Profile",
                m_Resolution.Presentation?.Profile,
                () => SelectOwner(m_Resolution.Presentation?.Profile));
            AddOwnerRow(
                "Animation Source",
                m_Resolution.Presentation?.Binding.Source,
                () => SelectOwner(m_Resolution.Presentation?.Binding.Source));
            if (m_TimelineSelection.HasTrack)
                AddDetailText(
                    $"Selected Track: {m_TimelineSelection.Track.AuthoringId}");
            if (m_TimelineSelection.HasClip)
                AddDetailText(
                    $"Selected Clip: {m_TimelineSelection.Clip.AuthoringId}");
            if (m_TimelineSelection.IsTreeClip)
                AddDetailText(
                    $"Selected TreeClip: {m_TimelineSelection.ElementAuthoringId}");
            if (m_TimelineSelection.HasMarker)
                AddDetailText(
                    $"Selected Marker: {m_TimelineSelection.ElementAuthoringId}");
            if (m_TimelineSelection.HasCurve)
                AddDetailText(
                    $"Selected Curve: {m_TimelineSelection.ElementAuthoringId}/{m_TimelineSelection.SubElementId} · keys {string.Join(",", m_TimelineSelection.KeyIndices)} · revision {m_TimelineSelection.Revision}");
        }

        void DrawSlotDetails()
        {
            AddDetailHeader("Slot / Blend");
            AddOwnerRow(
                "Pose Graph",
                m_Resolution.Slot?.Asset,
                NavigateSlot);
            AddDetailText(
                $"Graph: {m_Resolution.Slot?.Graph.GraphId.ToString() ?? "unresolved"}");
            AddDetailText(
                $"Node: {m_Resolution.Slot?.Node.NodeId.ToString() ?? "unresolved"}");
            AddDetailText(
                $"Slot: {m_Resolution.Slot?.SlotId.ToString() ?? "unresolved"}");
            AddOwnerRow(
                "Blend Policy",
                m_Resolution.Slot?.BlendPolicy,
                () => SelectOwner(m_Resolution.Slot?.BlendPolicy));
        }

        void DrawReferenceDetails()
        {
            AddDetailHeader("Formal Owners");
            AddOwnerRow(
                "Definition",
                m_Resolution.Definition?.Definition,
                () => SelectOwner(m_Resolution.Definition?.Definition));
            AddOwnerRow(
                "ActionProfile",
                m_Resolution.Action?.Profile,
                () => SelectOwner(m_Resolution.Action?.Profile));
            AddOwnerRow(
                "Gameplay Graph",
                m_Resolution.CallSite?.Graph?.SerializedOwner,
                NavigateCallSite);
            AddOwnerRow(
                "Timeline",
                m_Resolution.Timeline?.SerializedOwner,
                NavigateTimelineNode);
            AddOwnerRow(
                "Presentation",
                m_Resolution.Presentation?.Profile,
                () => SelectOwner(m_Resolution.Presentation?.Profile));
            AddOwnerRow(
                "Pose Graph",
                m_Resolution.Slot?.Asset,
                NavigateSlot);
        }

        void AddDetailHeader(string value)
        {
            var label = new Label(value);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 6f;
            label.style.marginBottom = 4f;
            m_DetailsHost.Add(label);
        }

        void AddDetailText(string value)
        {
            var label = new Label(value ?? string.Empty);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 3f;
            m_DetailsHost.Add(label);
        }

        void AddOwnerRow(
            string label,
            UnityEngine.Object owner,
            Action navigate)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            var field = new ObjectField(label)
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = true,
                value = owner
            };
            field.SetEnabled(false);
            field.style.flexGrow = 1f;
            row.Add(field);
            var button = new Button(navigate) { text = "Open" };
            button.SetEnabled(owner && navigate != null);
            row.Add(button);
            m_DetailsHost.Add(row);
        }

        void RefreshBottom()
        {
            if (m_BottomHost == null)
                return;
            m_BottomHost.Clear();
            m_BottomHost.style.display = m_BottomExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (m_Resolution == null)
            {
                m_BottomHost.Add(new Label("尚未建立 typed session。"));
                return;
            }
            switch (m_BottomPage)
            {
                case BottomPage.Preview:
                    DrawPreviewBottom();
                    break;
                case BottomPage.Live:
                    DrawLiveBottom();
                    break;
                case BottomPage.Diagnostics:
                    DrawDiagnosticsBottom();
                    break;
            }
        }

        void DrawPreviewBottom()
        {
            ActionAnimationPreviewTargetContext preview =
                m_Resolution.PreviewTarget;
            AddBottomText(
                preview?.IsReady == true
                    ? "Presentation Preview 输入已精确闭合；Timeline Core 只执行正式表现 Preview，不推进 Gameplay。"
                    : "Presentation Preview 未闭合；不会临时编译 Projection 或创建播放器。");
            AddBottomObject("Projection", preview?.Projection);
            AddBottomObject("Rig", preview?.Rig);
            AddBottomObject("Pose Graph", preview?.PoseGraph);
            TimelinePreviewSession session =
                m_TimelineView?.PreviewSession;
            AddBottomObject("Preview Target", session?.Target);
            AddBottomText(
                session == null
                    ? "Timeline Preview session unavailable."
                    : $"Preview Time: {session.Time:0.000}s / frame {session.Frame} / {(session.IsPlaying ? "Playing" : "Paused")}");
            if (!string.IsNullOrWhiteSpace(m_PreviewFailure))
                AddBottomText(m_PreviewFailure);
            if (m_PreviewView == null)
                return;

            ActionPresentationTimeSnapshot time =
                m_PreviewView.Time;
            ActionCommittedRawSample previous =
                time.CommittedWindow.Previous;
            AddBottomText(
                $"Action Instance: {m_PreviewView.Playback.ActionInstanceId} / {m_PreviewView.Playback.Phase} / terminal {m_PreviewView.Playback.LogicTerminal}");
            AddBottomText(
                $"Action Logic Time: tick {previous.LocalLogicTick}, time {previous.VisualTime:0.000}, cycle {previous.Cycle}");
            AddBottomText(
                $"Presentation Time: {time.ProjectedRawSample.ContinuousTime:0.000} / {time.ProjectionKind} / marker {time.MarkerEffectiveSample.ContinuousTime:0.000}");
            AddBottomText(
                $"AnimationSlot: {m_PreviewView.Slot.SlotId} / {m_PreviewView.Slot.ActionAvailability} / weight {m_PreviewView.Slot.ActionOutputWeight:0.###} / {m_PreviewView.Slot.TransitionExecution}");
            AddBottomText(
                $"Transition Routing: {m_PreviewView.Slot.Routing.Lifecycle} / rule {m_PreviewView.Slot.Routing.ActiveRuleId} / capture {m_PreviewView.Slot.Routing.CaptureCompleted} / release {m_PreviewView.Slot.ReleasePermission} / pending {m_PreviewView.Slot.PendingReleaseCompletion} / {m_PreviewView.Slot.Routing.ReasonCode}");
            AddBottomText(
                m_PreviewView.HasStack
                    ? $"Blend Stack: entries {m_PreviewView.Stack.EntryCount}, weight {m_PreviewView.Stack.OutputWeight:0.###}, stored {m_PreviewView.Stack.HasStoredPose}"
                    : "Blend Stack: no Slot-owned stack snapshot");
            AddBottomText(
                $"Inertialization nodes: {m_PreviewView.InertializationCount}");
            for (int i = 0;
                 i < m_PreviewView.Inertializations.Count;
                 i++)
            {
                PoseInertializationSnapshot inertialization =
                    m_PreviewView.Inertializations[i];
                AddBottomText(
                    $"Inertialization {inertialization.NodeId}: {inertialization.State} / owner {inertialization.TemporalOwnerKind} / edge {inertialization.SourceEndpointIndex}->{inertialization.TargetEndpointIndex} / curve {inertialization.CurveIndex} / profile {inertialization.ProfileIndex} / {inertialization.ElapsedSeconds:0.000}/{inertialization.DurationSeconds:0.000}s / {inertialization.Reason} / history {inertialization.HistoryCompletionIdentity} -> output {inertialization.OutputCompletionIdentity}");
            }
            AnimationPresentationRuntimeSnapshot posePlan =
                m_PreviewView.PosePlan;
            AddBottomText(
                $"Final Pose: {posePlan.FinalAvailability} / {posePlan.FinalInvalidReason} / contributions {posePlan.FinalContributions.Count} / continuity {posePlan.ContinuityIdentity}");
            CharacterPosePlanStageSnapshot stages =
                m_PreviewView.Stages;
            for (int i = 0; i < stages.Stages.Count; i++)
            {
                CharacterPoseExecutionStageSnapshot stage = stages.Stages[i];
                AddBottomText(
                    $"Pose Stage {stage.StageIndex}: {stage.ExecutionDomain} / {stage.InputPoseSpace}->{stage.OutputPoseSpace} / {stage.Status} / {stage.UnavailableReason}");
            }
        }

        void DrawLiveBottom()
        {
            ActionAnimationRuntimeDebugBinding binding =
                m_Resolution.RuntimeDebug;
            AddBottomText(
                m_LiveMode
                    ? "Live 模式只读；Timeline mutation 已禁用。"
                    : "启用顶部 Live 后读取正式 Runtime Debug。");
            AddBottomText($"Binding: {binding?.BindingId ?? "unresolved"}");
            AddBottomText(
                $"Source Revision: {binding?.SourceRevision ?? "missing"}");
            AddBottomText(
                $"Projection Revision: {binding?.ProjectionRevision ?? "missing"}");
            if (binding != null && !binding.HasExactRevision)
                AddBottomText("Revision 不完整，Trace 关联必须保持停止。");
            if (!string.IsNullOrWhiteSpace(m_RuntimeResolution.Message))
                AddBottomText(m_RuntimeResolution.Message);
            if (!string.IsNullOrWhiteSpace(m_LiveFailure))
                AddBottomText(m_LiveFailure);
            AddLiveTargetChoices();
            if (m_LiveView == null)
                return;

            AddBottomText(
                $"Target: {m_LiveView.Target.DisplayName} / {m_LiveView.NumericTarget}");
            AddBottomText(
                $"Action Instance: {m_LiveView.Playback.ActionInstanceId} / {m_LiveView.Playback.Phase} / terminal {m_LiveView.Playback.LogicTerminal}");
            ActionPresentationTimeSnapshot time =
                m_LiveView.Time;
            ActionCommittedRawSample previous =
                time.CommittedWindow.Previous;
            AddBottomText(
                $"Action Logic Time: tick {previous.LocalLogicTick}, time {previous.VisualTime:0.000}, cycle {previous.Cycle}");
            AddBottomText(
                $"Committed Raw Previous: event {previous.EventId}, sequence {previous.CommittedSequence}, continuous {previous.ContinuousVisualTime:0.000}");
            if (time.CommittedWindow.HasNext)
            {
                ActionCommittedRawSample next =
                    time.CommittedWindow.Next;
                AddBottomText(
                    $"Committed Raw Next: event {next.EventId}, tick {next.LocalLogicTick}, sequence {next.CommittedSequence}, continuous {next.ContinuousVisualTime:0.000}");
            }
            else
            {
                AddBottomText("Committed Raw Next: none");
            }
            AddBottomText(
                $"Projected Presentation Time: {time.ProjectedRawSample.ContinuousTime:0.000} / {time.ProjectionKind} / frame {time.PresentationFrame}");
            AddBottomText(
                $"Marker Effective Time: {time.MarkerEffectiveSample.ContinuousTime:0.000} / {time.PreviousMarkerId}->{time.NextMarkerId} / fraction {time.MarkerSegmentFraction:0.###} / mapped {time.MarkerMapped} / rebased {time.MarkerRebased}");
            AddBottomText(
                $"AnimationSlot: {m_LiveView.Slot.SlotId} / action {m_LiveView.Slot.SourceActionInstanceId} / {m_LiveView.Slot.ActionAvailability} / weight {m_LiveView.Slot.ActionOutputWeight:0.###} / {m_LiveView.Slot.TransitionExecution}");
            AddBottomText(
                $"Transition Routing: {m_LiveView.Slot.Routing.Lifecycle} / rule {m_LiveView.Slot.Routing.ActiveRuleId} / capture {m_LiveView.Slot.Routing.CaptureCompleted} / release {m_LiveView.Slot.ReleasePermission} / pending {m_LiveView.Slot.PendingReleaseCompletion} / {m_LiveView.Slot.Routing.ReasonCode}");
            AddBottomText(
                m_LiveView.HasStack
                    ? $"Blend Stack: entries {m_LiveView.Stack.EntryCount}, weight {m_LiveView.Stack.OutputWeight:0.###}, stored {m_LiveView.Stack.HasStoredPose}"
                    : "Blend Stack: no Slot-owned stack snapshot");
            AddBottomText(
                $"Inertialization nodes: {m_LiveView.InertializationCount}");
            for (int i = 0;
                 i < m_LiveView.Inertializations.Count;
                 i++)
            {
                PoseInertializationSnapshot inertialization =
                    m_LiveView.Inertializations[i];
                AddBottomText(
                    $"Inertialization {inertialization.NodeId}: {inertialization.State} / owner {inertialization.TemporalOwnerKind} / edge {inertialization.SourceEndpointIndex}->{inertialization.TargetEndpointIndex} / curve {inertialization.CurveIndex} / profile {inertialization.ProfileIndex} / residual {inertialization.ElapsedSeconds:0.000}/{inertialization.DurationSeconds:0.000}s / {inertialization.Reason} / continuity {inertialization.PreviousContinuityIdentity}->{inertialization.CurrentContinuityIdentity}");
            }
            AddBottomText(
                $"Final Pose: {m_LiveView.FinalAvailability} / {m_LiveView.FinalInvalidReason} / contributions {m_LiveView.FinalContributionCount} / continuity {m_LiveView.FinalContinuityIdentity}");
        }

        void AddLiveTargetChoices()
        {
            if (m_RuntimeBinding == null ||
                m_RuntimeResolution.CanReadSnapshot)
                return;
            IReadOnlyList<RuntimeDebugTargetCandidate> candidates =
                RuntimeDebugSession.Shared.GetTargetCandidates(
                    m_RuntimeBinding.Request);
            for (int i = 0; i < candidates.Count; i++)
            {
                RuntimeDebugTargetCandidate candidate =
                    candidates[i];
                var button = new Button(() =>
                {
                    if (candidate.IsExact)
                    {
                        RuntimeDebugSession.Shared.AttachToTarget(
                            candidate.Target.CharacterRuntimeId);
                        RefreshLiveState();
                        RefreshBottom();
                    }
                })
                {
                    text =
                        $"{candidate.Target.DisplayName} ({candidate.Match})"
                };
                button.SetEnabled(candidate.IsExact);
                m_BottomHost.Add(button);
            }
        }

        void DrawDiagnosticsBottom()
        {
            AddBottomText(
                $"Definition: {m_Resolution.Definition?.AssetGuid ?? "unresolved"}");
            AddBottomText(
                "Requested Numeric Target: Float32 Program + Presentation Projection");
            var commands = new VisualElement();
            commands.style.flexDirection = FlexDirection.Row;
            commands.Add(new Button(RunExplicitDryRun)
            {
                text = "Dry Run Float32"
            });
            commands.Add(new Button(RunExplicitBuild)
            {
                text = "Build Float32"
            });
            m_BottomHost.Add(commands);
            AddBottomText(
                m_Resolution.Failures.Count == 0
                    ? "Typed session diagnostics: Ready"
                    : $"Typed session diagnostics: {m_Resolution.Failures.Count} failure(s)");
            for (int i = 0;
                 i < m_Resolution.Failures.Count;
                 i++)
            {
                ActionAnimationWorkspaceFailure failure =
                    m_Resolution.Failures[i];
                AddBottomText($"{failure.Code}: {failure.Message}");
            }
            if (!string.IsNullOrWhiteSpace(
                    m_ExplicitBuildStatus))
                AddBottomText(m_ExplicitBuildStatus);
            if (m_DryRunReport == null)
                return;
            for (int i = 0;
                 i < m_DryRunReport.Messages.Count;
                 i++)
            {
                CharacterSimulationCompileMessage message =
                    m_DryRunReport.Messages[i];
                AddBottomText(
                    $"{message.Stage}/{message.Severity}/{message.Code}: {message.SourceIdentity} · {message.Message}");
            }
        }

        void RunExplicitDryRun()
        {
            m_DryRunReport = null;
            m_ExplicitBuildStatus = string.Empty;
            if (!m_Definition)
            {
                m_ExplicitBuildStatus =
                    "Dry Run unavailable: Definition unresolved.";
                RefreshBottom();
                return;
            }
            try
            {
                CharacterSimulationBuildResult result =
                    CharacterSimulationBuildOrchestrator.DryRun(
                        m_Definition);
                m_DryRunReport = result.Report;
                m_ExplicitBuildStatus = result.IsValid
                    ? "Dry Run Float32: Ready"
                    : "Dry Run Float32: Failed";
            }
            catch (Exception exception)
            {
                m_ExplicitBuildStatus =
                    $"Dry Run Float32 failed: {exception.Message}";
            }
            RefreshBottom();
        }

        void RunExplicitBuild()
        {
            m_DryRunReport = null;
            m_ExplicitBuildStatus = string.Empty;
            if (!m_Definition)
            {
                m_ExplicitBuildStatus =
                    "Build unavailable: Definition unresolved.";
                RefreshBottom();
                return;
            }
            try
            {
                m_ExplicitBuildStatus =
                    CharacterSimulationProgramBuildService.Build(
                        m_Definition,
                        true)
                        ? "Build Float32: Published Program and Projection."
                        : "Build Float32: Failed.";
            }
            catch (Exception exception)
            {
                m_ExplicitBuildStatus =
                    $"Build Float32 failed: {exception.Message}";
            }
            RefreshBottom();
        }

        void AddBottomText(string value)
        {
            var label = new Label(value ?? string.Empty);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginLeft = 8f;
            label.style.marginTop = 3f;
            m_BottomHost.Add(label);
        }

        void AddBottomObject(string label, UnityEngine.Object value)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = true,
                value = value
            };
            field.SetEnabled(false);
            m_BottomHost.Add(field);
        }

        void NavigateCallSite()
        {
            if (m_Resolution?.CallSite == null)
                return;
            NavigateGraphNode(
                m_Resolution.CallSite.Graph,
                m_Resolution.CallSite.Node.GUID);
        }

        void NavigateTimelineNode()
        {
            if (m_Resolution?.Timeline == null)
                return;
            NavigateGraphNode(
                m_Resolution.Timeline.Graph,
                m_Resolution.Timeline.Node.GUID);
        }

        void NavigateGraphNode(
            TreeDesigner.BaseTree graph,
            string nodeGuid)
        {
            BaseTreeWindow window =
                CharacterPipelineDefinitionTreeWindowUtility.OpenRootTree(
                    m_Definition);
            if (!window || graph == null || string.IsNullOrWhiteSpace(nodeGuid))
                return;
            window.ReplaceNavigationRoot(
                graph,
                new CharacterPipelineAuthoringContext(m_Definition));
            window.FocusSharedElement(new GraphAuthoringElementId(nodeGuid));
        }

        void NavigateSlot()
        {
            if (m_Resolution?.Slot == null)
                return;
            CharacterPresentationPoseGraphEditorWindow window =
                CharacterPresentationPoseGraphEditorWindow.Open(
                    m_Resolution.Slot.Asset,
                    m_Definition.AnimationPresentationProfile,
                    m_Definition.PresentationProjection,
                    m_Definition);
            window.FocusNode(
                m_Resolution.Slot.Graph.GraphId,
                m_Resolution.Slot.Node.NodeId);
        }

        void NavigateFailure(ActionAnimationWorkspaceFailure failure)
        {
            if (m_Definition?.RootTreeAsset?.Tree == null ||
                string.IsNullOrWhiteSpace(failure.ElementAuthoringId))
                return;
            ActionAnimationCallSiteContext call = m_Resolution?.CallSite;
            if (call != null &&
                string.Equals(
                    call.Graph.GraphAuthoringId,
                    failure.GraphAuthoringId,
                    StringComparison.Ordinal))
            {
                NavigateGraphNode(call.Graph, failure.ElementAuthoringId);
                return;
            }
            ActionAnimationTimelineContext timeline =
                m_Resolution?.Timeline;
            if (timeline != null &&
                string.Equals(
                    timeline.Graph.GraphAuthoringId,
                    failure.GraphAuthoringId,
                    StringComparison.Ordinal))
                NavigateGraphNode(
                    timeline.Graph,
                    failure.ElementAuthoringId);
        }

        static void SelectOwner(UnityEngine.Object owner)
        {
            if (!owner)
                return;
            Selection.activeObject = owner;
            EditorGUIUtility.PingObject(owner);
        }

        int BreadcrumbCount()
        {
            int count = m_Definition ? 1 : 0;
            if (m_Resolution?.Action != null)
                count++;
            if (m_Resolution?.Timeline != null)
                count++;
            return count;
        }

        void RenderBreadcrumb()
        {
            var entries = new List<GraphAuthoringBreadcrumbEntry>();
            if (m_Definition)
            {
                entries.Add(new GraphAuthoringBreadcrumbEntry(
                    m_Definition.name,
                    AssetDatabase.GetAssetPath(m_Definition)));
            }
            if (m_Resolution?.Action != null)
            {
                entries.Add(new GraphAuthoringBreadcrumbEntry(
                    m_Resolution.Action.ActionId,
                    "Action Profile"));
            }
            if (m_Resolution?.Timeline != null)
            {
                entries.Add(new GraphAuthoringBreadcrumbEntry(
                    m_Resolution.Timeline.Timeline.AuthoringId,
                    "Action Timeline"));
            }
            m_BreadcrumbHost?.Render(entries, NavigateBreadcrumb);
        }

        void NavigateBreadcrumb(int index)
        {
            if (index < 0)
                return;
            if (m_Definition)
            {
                if (index == 0)
                {
                    SelectOwner(m_Definition);
                    return;
                }
                index--;
            }
            if (m_Resolution?.Action != null)
            {
                if (index == 0)
                {
                    SelectOwner(m_Resolution.Action.Profile);
                    return;
                }
                index--;
            }
            if (index == 0 && m_Resolution?.Timeline != null)
                NavigateTimelineNode();
        }

        VisualElement RequireHost(string name) =>
            rootVisualElement.Q(name) ??
            throw new InvalidOperationException(
                $"Action Animation workspace host '{name}' is missing.");

        sealed class ActionAnimationDetailsHost :
            GraphAuthoringDetailsHostView
        {
            public ActionAnimationDetailsHost() :
                base(false)
            {
            }

            public VisualElement Content => DetailsContent;
        }

        void RefreshLiveState()
        {
            m_LiveView = null;
            m_LiveFailure = string.Empty;
            m_RuntimeResolution = default;
            if (!m_LiveMode ||
                m_Resolution?.Timeline == null ||
                m_TimelineView == null)
            {
                ReleaseAnimationDiagnosticsInterest();
                m_TimelineView?.ClearRuntimeOverlay();
                return;
            }
            if (m_RuntimeBinding == null)
                m_RuntimeBinding =
                    new RuntimeDebugViewBinding(
                        RuntimeDebugViewKind.Timeline);
            m_RuntimeBinding.Configure(
                new RuntimeDebugTargetRequest(
                    RuntimeSourceElementKey.Timeline(
                        m_Resolution.Timeline.Timeline.AuthoringId),
                    TimelineAuthoringFingerprint.Compute(
                        m_Resolution.Timeline.Timeline)));
            m_RuntimeResolution =
                m_RuntimeBinding.Refresh(
                    RuntimeDebugSession.Shared,
                    RuntimeTraceChannel.Timeline |
                    RuntimeTraceChannel.Animation |
                    RuntimeTraceChannel.Motion);
            if (!m_RuntimeResolution.CanReadSnapshot)
            {
                ReleaseAnimationDiagnosticsInterest();
                m_TimelineView.ClearRuntimeOverlay();
                m_LiveFailure = m_RuntimeResolution.Message;
                return;
            }
            RuntimeDebugViewModel runtimeView =
                RuntimeDebugSession.Shared.ViewModel;
            SynchronizeAnimationDiagnosticsInterest(runtimeView);
            if (!ActionAnimationAuthoringWorkspaceLiveProjection
                    .TryResolve(
                        m_Resolution,
                        runtimeView,
                        out m_LiveView,
                        out m_LiveFailure))
            {
                m_TimelineView.ClearRuntimeOverlay();
                return;
            }
            ApplyTimelineRuntimeOverlay(runtimeView);
        }

        void SynchronizeAnimationDiagnosticsInterest(
            RuntimeDebugViewModel runtimeView)
        {
            AnimationPresentationRuntimeTarget target =
                runtimeView?.Attached == true &&
                AnimationPresentationRuntimeTargetRegistry.TryGet(
                    runtimeView.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget resolved)
                    ? resolved
                    : null;
            if (!ReferenceEquals(target, m_AnimationDiagnosticsTarget))
            {
                m_AnimationDiagnosticsTarget?.RemoveDiagnosticsInterest(
                    m_AnimationDiagnosticsOwnerId);
                m_AnimationDiagnosticsTarget = target;
            }
            m_AnimationDiagnosticsTarget?.SetDiagnosticsInterest(
                m_AnimationDiagnosticsOwnerId,
                AnimationPresentationDiagnosticsInterest.LiveState |
                AnimationPresentationDiagnosticsInterest.FinalPoseDetail);
        }

        void ReleaseAnimationDiagnosticsInterest()
        {
            m_AnimationDiagnosticsTarget?.RemoveDiagnosticsInterest(
                m_AnimationDiagnosticsOwnerId);
            m_AnimationDiagnosticsTarget = null;
        }

        void ApplyTimelineRuntimeOverlay(
            RuntimeDebugViewModel runtimeView)
        {
            RuntimeInstanceKey playback =
                m_RuntimeBinding.SelectedInstance;
            string timelineId =
                m_Resolution.Timeline.Timeline.AuthoringId;
            if (playback.Kind !=
                    RuntimeInstanceKind.TimelinePlayback ||
                !runtimeView.TryGetTimelinePlaybackSummary(
                    timelineId,
                    playback,
                    out _))
            {
                m_TimelineView.ClearRuntimeOverlay();
                return;
            }
            IReadOnlyList<RuntimeDebugEventView> events =
                runtimeView.GetTimelineCurrentEvents(
                    timelineId,
                    playback);
            ulong latestLogic = 0;
            ulong latestPresentation = 0;
            for (int i = 0; i < events.Count; i++)
            {
                RuntimeTraceEvent traceEvent =
                    events[i].Event;
                if (traceEvent.Domain ==
                    RuntimeTraceDomain.Logic)
                {
                    latestLogic = Math.Max(
                        latestLogic,
                        traceEvent.Position);
                }
                else if (traceEvent.Domain ==
                         RuntimeTraceDomain.Presentation)
                {
                    latestPresentation = Math.Max(
                        latestPresentation,
                        traceEvent.Position);
                }
            }
            float visualTime = 0f;
            var tracks =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);
            var clips =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);
            for (int i = 0; i < events.Count; i++)
            {
                RuntimeDebugEventView eventView =
                    events[i];
                if (eventView.Event.Domain ==
                        RuntimeTraceDomain.Presentation &&
                    eventView.Event.Position ==
                        latestPresentation &&
                    eventView.Event.Kind ==
                        RuntimeTraceEventKind.TimelineVisualTime)
                {
                    visualTime =
                        eventView.Event.Payload.Time;
                }
                if (eventView.Event.Domain !=
                        RuntimeTraceDomain.Logic ||
                    eventView.Event.Position != latestLogic)
                    continue;
                if (eventView.Event.Kind ==
                    RuntimeTraceEventKind.TrackActive)
                {
                    tracks[eventView.Source.TrackAuthoringId] =
                        eventView.Event.Payload.Status;
                }
                if (eventView.Event.Kind ==
                        RuntimeTraceEventKind.ClipActive ||
                    eventView.Event.Kind is
                        RuntimeTraceEventKind.TreeClipEntered or
                        RuntimeTraceEventKind.TreeClipUpdated)
                {
                    clips[eventView.Source.ClipAuthoringId] =
                        $"{eventView.Event.Kind}: {eventView.Event.Payload.Status}";
                }
            }
            m_TimelineView.ApplyRuntimeOverlay(
                visualTime,
                tracks,
                clips);
        }

        void InvalidateLiveState()
        {
            m_RuntimeBinding?.Dispose(
                RuntimeDebugSession.Shared);
            m_RuntimeBinding = null;
            m_RuntimeResolution = default;
            m_LiveView = null;
            m_LiveFailure = string.Empty;
        }

        void OnTimelinePreviewEvaluated()
        {
            if (m_LiveMode)
                return;
            RefreshPreviewState(true);
            if (m_BottomPage == BottomPage.Preview)
                RefreshBottom();
            Repaint();
        }

        void RefreshPreviewState(bool stopOnFailure)
        {
            m_PreviewView = null;
            m_PreviewFailure = string.Empty;
            if (m_LiveMode ||
                m_Resolution?.Timeline == null ||
                m_TimelineView == null)
                return;
            TimelinePreviewSession session =
                m_TimelineView.PreviewSession;
            if (!ReferenceEquals(
                    session.Timeline,
                    m_Resolution.Timeline.Timeline))
            {
                m_PreviewFailure =
                    "Preview session Timeline 与 typed session 不一致。";
                return;
            }
            if (!session.Target)
            {
                m_PreviewFailure =
                    "选择一个使用当前精确 Definition 的 CharacterPipelineHost。";
                return;
            }
            if (session.Target is not CharacterPipelineHost host)
            {
                StopPreview(
                    session,
                    null,
                    "Preview target 不是正式 CharacterPipelineHost。",
                    stopOnFailure);
                return;
            }
            if (host.Definition != m_Definition)
            {
                StopPreview(
                    session,
                    host,
                    "Preview target 没有使用当前精确 Character Definition。",
                    stopOnFailure);
                return;
            }
            if (m_Resolution.PreviewTarget?.IsReady != true ||
                !host.CanPreviewTimeline)
            {
                StopPreview(
                    session,
                    host,
                    "Preview target 缺少正式 Definition、Projection、Rig、Animancer 或 VisualRoot。",
                    stopOnFailure);
                return;
            }
            if (!string.IsNullOrWhiteSpace(session.Error))
            {
                m_PreviewFailure = session.Error;
                return;
            }
            if (!host.HasPreviewAnimationDebugView)
            {
                m_PreviewFailure =
                    "正式 Timeline Preview 尚未提交完整 Animation frame。";
                return;
            }
            if (!ActionAnimationAuthoringWorkspaceLiveProjection
                    .TryResolvePreview(
                        m_Resolution,
                        host.PreviewAnimationDebugView,
                        host.PreviewPosePlanStages,
                        out m_PreviewView,
                        out m_PreviewFailure))
            {
                StopPreview(
                    session,
                    host,
                    m_PreviewFailure,
                    stopOnFailure);
            }
        }

        void StopPreview(
            TimelinePreviewSession session,
            CharacterPipelineHost host,
            string failure,
            bool stop)
        {
            m_PreviewFailure = failure;
            if (!stop)
                return;
            session.Pause();
            host?.ClearTimelinePreview(session.SessionId);
        }

        void OnRuntimeDebugSessionChanged()
        {
            if (!m_LiveMode)
                return;
            RefreshLiveState();
            if (m_BottomPage == BottomPage.Live)
                RefreshBottom();
            Repaint();
        }
    }
}
