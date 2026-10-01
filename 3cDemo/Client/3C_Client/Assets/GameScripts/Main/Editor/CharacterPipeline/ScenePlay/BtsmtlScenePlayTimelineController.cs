using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonGameplay.Tick;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    [InitializeOnLoad]
    static class BtsmtlScenePlayTimelineController
    {
        static readonly Controller s_Controller = new Controller();

        static BtsmtlScenePlayTimelineController()
        {
            TimelineWorkspaceModeBridge.Register(s_Controller);
            EditorApplication.playModeStateChanged += s_Controller.OnPlayModeChanged;
            EditorApplication.projectChanged += s_Controller.OnContentChanged;
            Undo.undoRedoPerformed += s_Controller.OnContentChanged;
            RuntimeDebugSession.Shared.Changed += s_Controller.OnRuntimeDebugChanged;
            RuntimeDiagnosticsTargetRegistry.TargetRegistered += s_Controller.OnTargetChanged;
            RuntimeDiagnosticsTargetRegistry.TargetUnregistered += s_Controller.OnTargetChanged;
            EditorApplication.update += s_Controller.UpdateSessionState;
            TimelineEditorWindow.AuthoringRevisionChanged += s_Controller.OnAuthoringRevisionChanged;
            TimelineEditorWindow.AssetOpened += s_Controller.OnTimelineAssetOpened;
            BtsmtlScenePlayPreviewHost.Shared.Changed += s_Controller.OnPreviewHostChanged;
        }

        internal static void EnableRuntimeDebug() => s_Controller.EnableRuntimeDebug();
        internal static VisualElement CreatePreviewControls(BtsmtlScenePlayPreviewWindow window) =>
            s_Controller.CreateControls((EditorWindow)window);
        internal static void OnPreviewWindowOpened() => s_Controller.EnterPreview();
        internal static void OnPreviewWindowClosed(BtsmtlScenePlayPreviewWindow window) =>
            s_Controller.OnWindowClosed((EditorWindow)window);
        internal static void OpenExecutionSource(RuntimeExecutionSpan span) =>
            s_Controller.OnExecutionSourceOpenRequested(span);
        internal static void SeekExecutionHistory() => s_Controller.OnExecutionHistoryRequested();

        sealed class Controls
        {
            internal EditorWindow Window;
            internal ObjectField Profile;
            internal ToolbarToggle RuntimeDebug;
            internal ToolbarMenu Session;
        }

        sealed class Controller : ITimelineWorkspaceModeController
        {
            const string ProfileGuidStateKey = "ThirdPersonCharacter.ScenePlay.Timeline.ProfileGuid";
            const string DefaultProfileGuid = "f6a23791f8784a9f9ee35efd2ecbfcb7";
            const RuntimeTraceChannel LiveTraceChannels =
                RuntimeTraceChannel.Graph |
                RuntimeTraceChannel.StateMachine |
                RuntimeTraceChannel.Timeline;
            const RuntimeTraceChannel CaptureTraceChannels =
                LiveTraceChannels |
                RuntimeTraceChannel.Blackboard |
                RuntimeTraceChannel.Animation |
                RuntimeTraceChannel.Motion |
                RuntimeTraceChannel.Values;

            readonly object m_InterestOwner = new object();
            readonly List<Controls> m_Controls = new List<Controls>();
            readonly BtsmtlRuntimeFocusResolver m_RuntimeFocus = BtsmtlRuntimeFocusResolver.Shared;
            TimelineWorkspaceMode Mode => BtsmtlScenePlayPreviewWindow.IsOpen
                ? TimelineWorkspaceMode.Preview : TimelineWorkspaceMode.Authoring;
            BtsmtlScenePlayProfile m_Profile;
            CharacterTimelineContentExport m_ExportedContent;
            CharacterTimelineContentAdoptionPlan m_PendingPlan;
            CharacterTimelineContentPublication m_PublishedContent;
            CharacterTimelineContentAdoptionState m_ContentState;
            string m_Status = "Authoring";
            bool m_RuntimeInterest;
            bool m_ConnectionRefreshQueued;
            bool m_ContentRefreshQueued;
            SimulationSessionHost m_ObservedSession;
            SimulationSessionLifecycleState m_ObservedLifecycle;
            ulong m_ObservedGeneration;
            bool HasRuntime => EditorApplication.isPlaying || BtsmtlScenePlayPreviewHost.Shared.IsReady;
            bool m_FollowRuntime = true;
            bool m_NavigationQueued;
            RuntimeInstanceKey m_LastFocusInstance;
            RuntimeSourceElementKey m_LastFocusSource;

            public Controller()
            {
                string profileGuid = SessionState.GetString(ProfileGuidStateKey, DefaultProfileGuid);
                m_Profile = AssetDatabase.LoadAssetAtPath<BtsmtlScenePlayProfile>(
                    AssetDatabase.GUIDToAssetPath(profileGuid));
            }

            public VisualElement CreateControls(TimelineEditorWindow window) => CreateControls((EditorWindow)window);

            internal VisualElement CreateControls(EditorWindow window)
            {
                for (int i = m_Controls.Count - 1; i >= 0; i--)
                    if (m_Controls[i].Window == window)
                        m_Controls.RemoveAt(i);
                var controls = new Controls
                {
                    Window = window,
                    Profile = new ObjectField
                    {
                        objectType = typeof(BtsmtlScenePlayProfile),
                        allowSceneObjects = false,
                        value = m_Profile
                    },
                    RuntimeDebug = new ToolbarToggle { text = "RuntimeDebug" },
                    Session = new ToolbarMenu { text = "Session" }
                };
                controls.Profile.style.width = 170f;
                controls.Session.RegisterCallback<PointerDownEvent>(
                    _ => AddSessionActions(controls), TrickleDown.TrickleDown);
                controls.Session.RegisterCallback<NavigationSubmitEvent>(
                    _ => AddSessionActions(controls), TrickleDown.TrickleDown);
                controls.Profile.tooltip = "预览装配 Profile；场景与角色配置在 Profile Inspector 中编辑。";
                controls.Profile.RegisterValueChangedCallback(evt =>
                {
                    BtsmtlScenePlayProfile profile = evt.newValue as BtsmtlScenePlayProfile;
                    if (!ReferenceEquals(m_Profile, profile))
                    {
                        BtsmtlScenePlayPreviewHost.Shared.Close();
                        TimelineWorkspaceModeBridge.SetRuntimeDebugEnabled(false);
                        ReleaseRuntimeInterest();
                        ClearContentWorkflow();
                        m_ContentRefreshQueued = false;
                    }
                    m_Profile = profile;
                    string path = AssetDatabase.GetAssetPath(m_Profile);
                    SessionState.SetString(
                        ProfileGuidStateKey,
                        string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
                    if (BtsmtlScenePlayPreviewWindow.IsOpen)
                        EnterPreview();
                    else
                        SetStatus(m_Profile == null ? "未选择预览 Profile。" : m_Profile.IsValid ? "Profile 已选择。" : "预览 Profile 配置无效。");
                });
                controls.RuntimeDebug.tooltip = "开启运行时诊断、自动跳转和 Timeline 覆盖显示。";
                controls.RuntimeDebug.RegisterValueChangedCallback(evt => SetRuntimeDebug(evt.newValue));
                AddSessionActions(controls);
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.Add(controls.Profile);
                container.Add(controls.RuntimeDebug);
                if (window is TimelineEditorWindow)
                    container.Add(new ToolbarButton(BtsmtlScenePlayPreviewWindow.Open) { text = "打开 Preview" });
                container.Add(controls.Session);
                m_Controls.Add(controls);
                RefreshControls();
                ApplyToWindow(window);
                QueueConnectionRefresh();
                return container;
            }

            public void ApplyToWindow(TimelineEditorWindow window) => ApplyToWindow((EditorWindow)window);

            void ApplyToWindow(EditorWindow window)
            {
                if (window is TimelineEditorWindow timeline)
                {
                    timeline.SetRuntimeObservationReadOnly(
                        TimelineWorkspaceModeBridge.RuntimeDebugEnabled && HasRuntime);
                    timeline.SetRuntimeObservationStatus(m_Status);
                }
                else if (window is BtsmtlScenePlayPreviewWindow preview)
                    preview.SetStatus(m_Status);
            }

            internal void OnExecutionSourceOpenRequested(RuntimeExecutionSpan span)
            {
                var source = new RuntimeDebugEventView(span.Start, span.Source, span.Start.Payload.Name);
                if (!RuntimeDebugSourceNavigator.Open(source, true))
                    SetStatus("当前记录的来源不可定位；检查记录版本及来源映射。");
            }

            internal void OnExecutionHistoryRequested()
            {
                if (BtsmtlScenePlayPreviewHost.Shared.IsReady)
                    BtsmtlScenePlayPreviewHost.Shared.Enqueue(GameplayTickDriveCommand.Pause());
            }

            public void OnWindowClosed(TimelineEditorWindow window) => OnWindowClosed((EditorWindow)window);

            internal void OnWindowClosed(EditorWindow window)
            {
                for (int i = m_Controls.Count - 1; i >= 0; i--)
                    if (m_Controls[i].Window == window)
                        m_Controls.RemoveAt(i);
                bool previewClosed = window is BtsmtlScenePlayPreviewWindow;
                if (previewClosed)
                {
                    m_ContentRefreshQueued = false;
                    BtsmtlScenePlayPreviewHost.Shared.Close();
                }
                if (m_Controls.Count == 0 || previewClosed && !EditorApplication.isPlaying)
                {
                    TimelineWorkspaceModeBridge.SetRuntimeDebugEnabled(false);
                    ReleaseRuntimeInterest();
                }
                Refresh();
            }

            internal void OnTargetChanged(RuntimeDiagnosticsTarget target) => QueueConnectionRefresh();

            void QueueConnectionRefresh()
            {
                if (m_ConnectionRefreshQueued)
                    return;
                m_ConnectionRefreshQueued = true;
            }

            void RefreshConnection()
            {
                m_ConnectionRefreshQueued = false;
                if (!HasRuntime)
                    return;
                if ((TimelineWorkspaceModeBridge.RuntimeDebugEnabled || Mode == TimelineWorkspaceMode.Preview) &&
                    !AttachRuntimeDebug())
                    return;
                RuntimeDebugSession debug = RuntimeDebugSession.Shared;
                if (Mode == TimelineWorkspaceMode.Preview && BtsmtlScenePlayPreviewHost.Shared.IsReady &&
                    debug.CanStartCapture && !debug.IsCaptureRecording && !debug.HasCaptureHistory)
                    debug.BeginCapture(CaptureTraceChannels, RuntimeDiagnosticsCaptureDetail.Continuous);
            }

            internal void UpdateSessionState()
            {
                if (m_ContentRefreshQueued && BtsmtlScenePlayPreviewHost.Shared.IsReady &&
                    !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                    ApplyChangedTimelineContent();
                if (m_ConnectionRefreshQueued)
                    RefreshConnection();
                if (m_NavigationQueued)
                    FollowRuntime();
                if ((!TimelineWorkspaceModeBridge.RuntimeDebugEnabled &&
                     Mode == TimelineWorkspaceMode.Authoring) || !m_ObservedSession)
                    return;
                if (m_ObservedLifecycle == m_ObservedSession.LifecycleState &&
                    m_ObservedGeneration == m_ObservedSession.SessionGeneration)
                    return;
                m_ObservedLifecycle = m_ObservedSession.LifecycleState;
                m_ObservedGeneration = m_ObservedSession.SessionGeneration;
                QueueConnectionRefresh();
            }

            internal void OnPlayModeChanged(PlayModeStateChange state)
            {
                m_ContentRefreshQueued = false;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    m_FollowRuntime = true;
                    ResetNavigationFocus();
                    TimelineWorkspaceModeBridge.SetRuntimeDebugEnabled(false);
                    QueueConnectionRefresh();
                }
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    m_ObservedSession = null;
                    TimelineWorkspaceModeBridge.SetRuntimeDebugEnabled(false);
                    ReleaseRuntimeInterest();
                    ClearContentWorkflow();
                    SetStatus(Mode == TimelineWorkspaceMode.Authoring ? "Authoring" : "已退出 Play。");
                }
                Refresh();
            }

            internal void OnAuthoringRevisionChanged(TimelineEditorWindow window)
            {
                OnContentChanged();
            }

            internal void OnTimelineAssetOpened(TimelineAsset asset)
            {
                if (!HasRuntime || m_Profile == null || !m_Profile.IsValid)
                    return;
                if (!TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
                    SetRuntimeDebug(true);
                TimelineEditorWindow window = TimelineEditorWindow.FindOpen(asset);
                if (window == null)
                    return;
                window.SetRuntimeObservationReadOnly(true);
                TimelineRuntimeObservationBridge.RefreshWindow(window);
            }

            internal void OnContentChanged()
            {
                if (BtsmtlScenePlayPreviewWindow.IsOpen && BtsmtlScenePlayPreviewHost.Shared.IsOpen)
                    m_ContentRefreshQueued = true;
            }

            void ApplyChangedTimelineContent()
            {
                m_ContentRefreshQueued = false;
                CharacterTimelineContentStore content = BtsmtlScenePlayPreviewHost.Shared.Actor.TimelineHost.Content;
                if (!content.TryExportContent(out CharacterTimelineContentExport export, out string error))
                {
                    ClearContentWorkflow();
                    SetContentStatus(CharacterTimelineContentAdoptionState.Failed, error);
                    return;
                }
                if (string.Equals(export.AuthoringRevision, content.AuthoringContentRevision, StringComparison.Ordinal))
                    return;
                ClearContentWorkflow();
                m_ExportedContent = export;
                if (!content.TryPrepareContentAdoption(export, out CharacterTimelineContentAdoptionPlan plan, out error))
                {
                    SetContentStatus(CharacterTimelineContentAdoptionState.Rejected, error);
                    return;
                }
                m_PendingPlan = plan;
                if (!content.TryPublishContentAdoption(plan, out CharacterTimelineContentPublication publication, out error))
                {
                    SetContentStatus(CharacterTimelineContentAdoptionState.Rejected, error);
                    return;
                }
                m_PublishedContent = publication;
                if (!content.TryAdoptContent(publication, out CharacterTimelineContentAdoptionReport report))
                {
                    SetContentStatus(report.State, report.Message);
                    return;
                }
                ClearContentWorkflow();
                SetContentStatus(report.State, "Timeline 内容已应用；新的播放调用使用此版本，活动播放保持原内容。");
            }

            internal void Refresh()
            {
                if (TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
                {
                    m_RuntimeFocus.Refresh(RuntimeDebugSession.Shared.ViewModel);
                    QueueRuntimeNavigation();
                }
                RefreshControls();
                for (int i = 0; i < m_Controls.Count; i++)
                    ApplyToWindow(m_Controls[i].Window);
            }

            internal void OnRuntimeDebugChanged()
            {
                if (!TimelineWorkspaceModeBridge.RuntimeDebugEnabled || !m_FollowRuntime)
                    return;
                m_RuntimeFocus.Refresh(RuntimeDebugSession.Shared.ViewModel);
                QueueRuntimeNavigation();
            }

            internal void EnableRuntimeDebug()
            {
                if (HasRuntime && !TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
                    SetRuntimeDebug(true);
            }

            void AddSessionActions(Controls controls)
            {
                ToolbarMenu menu = controls.Session;
                menu.menu.MenuItems().Clear();
                menu.menu.AppendAction(
                    "打开预览",
                    _ => BtsmtlScenePlayPreviewWindow.Open(),
                    _ => m_Profile != null && m_Profile.IsValid && !EditorApplication.isPlaying
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction(
                    "Pause",
                    _ => SubmitSessionCommand(true),
                    _ => HasSessionHost()
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction(
                    "Resume",
                    _ => SubmitSessionCommand(false),
                    _ => HasSessionHost()
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction(
                    "Stop",
                    _ => StopSession(),
                    _ => HasSessionHost() && EditorApplication.isPlaying
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction("关闭预览", _ => BtsmtlScenePlayPreviewHost.Shared.Close(),
                    _ => BtsmtlScenePlayPreviewHost.Shared.IsOpen
                        ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction("单步", _ => BtsmtlScenePlayPreviewHost.Shared.Enqueue(GameplayTickDriveCommand.Step(1)),
                    _ => BtsmtlScenePlayPreviewHost.Shared.IsReady
                        ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                if (Mode == TimelineWorkspaceMode.Preview)
                {
                    menu.menu.AppendSeparator("Content/");
                    menu.menu.AppendAction(
                        "Content/Export",
                        _ => ExportContent(),
                        _ => CanExportContent()
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled);
                    menu.menu.AppendAction(
                        "Content/Prepare",
                        _ => PrepareContent(),
                        _ => CanPrepareContent()
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled);
                    menu.menu.AppendAction(
                        "Content/Publish",
                        _ => PublishContent(),
                        _ => m_PendingPlan != null && CanExportContent()
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled);
                    menu.menu.AppendAction(
                        "Content/Adopt",
                        _ => AdoptContent(),
                        _ => m_PublishedContent != null && CanExportContent()
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled);
                }
                if (TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
                {
                    if (controls.Window is TimelineEditorWindow timeline)
                        AddRuntimePlaybackActions(menu, timeline);
                    AddRuntimeDebugActions(menu);
                }
            }

            void AddRuntimePlaybackActions(ToolbarMenu menu, TimelineEditorWindow window)
            {
                IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries = window == null
                    ? Array.Empty<RuntimeTimelinePlaybackDebugSummary>()
                    : window.GetRuntimeObservationSummaries();
                menu.menu.AppendSeparator("Runtime/");
                if (summaries.Count == 0)
                {
                    menu.menu.AppendAction(
                        "Runtime/Playback/尚无当前调用路径的实例",
                        _ => { },
                        DropdownMenuAction.Status.Disabled);
                    return;
                }
                for (int index = 0; index < summaries.Count; index++)
                {
                    RuntimeTimelinePlaybackDebugSummary summary = summaries[index];
                    RuntimeInstanceKey playback = summary.Playback;
                    string label = $"Runtime/Playback/#{playback.TimelinePlaybackId} · 动作 {playback.ActionInstanceId} · {summary.Provenance.SourceInvocationPath} · 调用 {summary.Provenance.SourceActivationGeneration}";
                    menu.menu.AppendAction(
                        label,
                        _ =>
                        {
                            window.SelectRuntimeObservationPlayback(playback, pin: true);
                            m_FollowRuntime = false;
                            Refresh();
                        },
                        _ => window.RuntimeObservationPlayback.Equals(playback)
                            ? DropdownMenuAction.Status.Checked
                            : DropdownMenuAction.Status.Normal);
                }
            }

            void AddRuntimeDebugActions(ToolbarMenu menu)
            {
                RuntimeDebugSession debug = RuntimeDebugSession.Shared;
                menu.menu.AppendSeparator("RuntimeDebug/");
                menu.menu.AppendAction(
                    "RuntimeDebug/Follow",
                    _ =>
                    {
                        m_FollowRuntime = true;
                        ResetNavigationFocus();
                        Refresh();
                    },
                    _ => m_FollowRuntime ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                for (int i = 0; i < m_RuntimeFocus.Candidates.Count; i++)
                {
                    RuntimeDebugEventView candidate = m_RuntimeFocus.Candidates[i];
                    RuntimeInstanceKey instance = candidate.Event.RuntimeInstance;
                    string path = instance.Kind == RuntimeInstanceKind.TimelinePlayback
                        ? candidate.Event.Payload.TimelinePlayback.SourceInvocationPath
                        : instance.CallSiteId;
                    menu.menu.AppendAction(
                        $"RuntimeDebug/Pin/{candidate.SourceName} · 动作 {instance.ActionInstanceId} · {path} · 调用 {instance.InvocationGeneration} · 播放 {instance.TimelinePlaybackId}",
                        _ =>
                        {
                            m_FollowRuntime = false;
                            NavigateRuntime(candidate);
                        });
                }
                if (debug.IsCaptureRecording)
                {
                    menu.menu.AppendAction(
                        "RuntimeDebug/Capture/End",
                        _ => EndRuntimeDebugCapture(),
                        _ => debug.CanStopCapture
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled);
                }
                else
                {
                    menu.menu.AppendAction(
                        "RuntimeDebug/Capture/Begin",
                        _ => BeginRuntimeDebugCapture(),
                        _ => debug.CanStartCapture
                            ? DropdownMenuAction.Status.Normal
                            : DropdownMenuAction.Status.Disabled);
                }
                if (!debug.HasCaptureHistory)
                    return;
                menu.menu.AppendAction(
                    "RuntimeDebug/History/Resume Live",
                    _ => ResumeRuntimeDebugLive(),
                    _ => debug.CanResumeLiveTarget && !debug.IsCaptureRecording
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                int maximumOffset = Math.Max(0, debug.CaptureSnapshot.SegmentCount - 1);
                for (int index = 0; index <= maximumOffset; index++)
                {
                    int historyOffset = index;
                    menu.menu.AppendAction(
                        $"RuntimeDebug/History/Segment {historyOffset}",
                        _ => SetRuntimeDebugHistoryOffset(historyOffset),
                        _ => debug.HistoryOffset == historyOffset &&
                             (debug.AttachmentState == RuntimeDebugAttachmentState.CaptureHistory ||
                              debug.AttachmentState == RuntimeDebugAttachmentState.Ended)
                            ? DropdownMenuAction.Status.Checked
                            : DropdownMenuAction.Status.Normal);
                }
            }

            void QueueRuntimeNavigation()
            {
                if (!m_FollowRuntime || m_NavigationQueued)
                    return;
                m_NavigationQueued = true;
            }

            void ResetNavigationFocus()
            {
                m_LastFocusInstance = default;
                m_LastFocusSource = default;
            }

            void FollowRuntime()
            {
                m_NavigationQueued = false;
                if (!m_FollowRuntime || !TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
                    return;
                RuntimeDebugViewModel view = RuntimeDebugSession.Shared.ViewModel;
                if (!view.Attached)
                {
                    ResetNavigationFocus();
                    return;
                }
                if (view.HasCoverageGap)
                {
                    ResetNavigationFocus();
                    PublishNavigationStatus("运行记录不完整，已停止自动跟随。");
                    return;
                }
                if (m_RuntimeFocus.Candidates.Count != 1)
                {
                    ResetNavigationFocus();
                    PublishNavigationStatus(m_RuntimeFocus.Candidates.Count == 0
                        ? "当前角色没有可导航的技能或 Timeline 运行事实。"
                        : "存在并行调用，请在 Session / RuntimeDebug / Pin 中选择具体实例。");
                    return;
                }
                RuntimeDebugEventView candidate = m_RuntimeFocus.Candidates[0];
                if (m_LastFocusInstance.Equals(candidate.Event.RuntimeInstance) &&
                    m_LastFocusSource.Equals(candidate.Source))
                    return;
                NavigateRuntime(candidate);
            }

            void NavigateRuntime(RuntimeDebugEventView candidate)
            {
                if (!RuntimeDebugSourceNavigator.Open(
                        candidate,
                        pin: !m_FollowRuntime,
                        preserveFocus: true))
                {
                    PublishNavigationStatus("当前调用缺少匹配版本的作者来源，无法导航。");
                    return;
                }
                m_LastFocusInstance = candidate.Event.RuntimeInstance;
                m_LastFocusSource = candidate.Source;
                PublishNavigationStatus($"{(m_FollowRuntime ? "跟随" : "固定")} {candidate.SourceName} · 动作 {m_LastFocusInstance.ActionInstanceId}");
            }

            void PublishNavigationStatus(string message)
            {
                m_Status = FormatRuntimeDebugStatus(message);
                for (int i = 0; i < m_Controls.Count; i++)
                    ApplyToWindow(m_Controls[i].Window);
            }

            internal void EnterPreview()
            {
                if (m_Profile == null || !m_Profile.IsValid)
                {
                    SetStatus("请先选择有效的预览 Profile。");
                    return;
                }
                QueueConnectionRefresh();
                if (!EditorApplication.isPlaying && !BtsmtlScenePlayPreviewHost.Shared.IsOpen)
                    StartPreview();
                else
                    SetStatus(TimelineWorkspaceModeBridge.RuntimeDebugEnabled
                        ? FormatRuntimeDebugStatus("当前 Timeline 只读。")
                        : FormatPreviewContentStatus(string.Empty));
            }

            void SetRuntimeDebug(bool enabled)
            {
                if (!HasRuntime || m_Profile == null || !m_Profile.IsValid)
                {
                    RefreshControls();
                    return;
                }
                if (TimelineWorkspaceModeBridge.RuntimeDebugEnabled == enabled)
                    return;
                TimelineWorkspaceModeBridge.SetRuntimeDebugEnabled(enabled);
                if (enabled)
                {
                    m_FollowRuntime = true;
                    ResetNavigationFocus();
                    AttachRuntimeDebug();
                }
                else
                {
                    ReleaseRuntimeInterest();
                    SetStatus(Mode == TimelineWorkspaceMode.Preview
                        ? FormatPreviewContentStatus(string.Empty)
                        : "Authoring");
                }
            }

            async void StartPreview()
            {
                ClearContentWorkflow();
                await BtsmtlScenePlayPreviewHost.Shared.OpenAsync(m_Profile);
                OnPreviewHostChanged();
            }

            internal void OnPreviewHostChanged()
            {
                m_ObservedSession = BtsmtlScenePlayPreviewHost.Shared.Session;
                SetStatus(FormatPreviewContentStatus(BtsmtlScenePlayPreviewHost.Shared.Error));
                QueueConnectionRefresh();
            }

            void StopSession()
            {
                if (!TryResolveSessionHost(out SimulationSessionHost sessionHost, out string error))
                {
                    SetStatus(error);
                    return;
                }
                sessionHost.Stop();
                SetStatus("ScenePlay 已提交停止。");
            }

            void SubmitSessionCommand(bool pause)
            {
                if (!TryResolveSessionHost(out SimulationSessionHost host, out string resolveError))
                {
                    SetStatus(resolveError);
                    return;
                }
                IReadOnlyList<SimulationSessionDebugStatusSnapshot> snapshots =
                    LocalSimulationDebugControlService.CaptureStatusSnapshots();
                for (int i = 0; i < snapshots.Count; i++)
                {
                    SimulationSessionDebugStatusSnapshot snapshot = snapshots[i];
                    if (snapshot.Identity.HostInstanceId != host.GetInstanceID())
                        continue;
                    SimulationSessionDebugCommand command = pause
                        ? SimulationSessionDebugCommand.Pause(snapshot.Identity.TargetKey)
                        : SimulationSessionDebugCommand.SetRealtime(snapshot.Identity.TargetKey);
                    if (LocalSimulationDebugControlService.TrySubmit(
                            command.TargetKey,
                            command,
                            out SimulationSessionDebugCommandResult result))
                        SetStatus($"Session command accepted #{result.CommandSequence}.");
                    else
                        SetStatus(result.Message);
                    return;
                }
                SetStatus("当前 Session 尚未注册控制目标。");
            }

            bool AttachRuntimeDebug()
            {
                if (!HasRuntime)
                {
                    SetStatus("RuntimeDebug 需要先进入 Preview。");
                    return false;
                }
                if (!TryResolveActorHost(out FixedCharacterHost host, out string resolveError))
                {
                    SetStatus(resolveError);
                    return false;
                }
                if (!RuntimeDebugSession.Shared.AttachToHost(host.GetInstanceID()))
                {
                    SetStatus("Profile 对应 Actor 尚未注册 RuntimeDebug target。");
                    return false;
                }
                RuntimeDebugSession.Shared.EnsureLiveInterest(m_InterestOwner, LiveTraceChannels);
                m_RuntimeInterest = true;
                SetStatus(TimelineWorkspaceModeBridge.RuntimeDebugEnabled
                    ? FormatRuntimeDebugStatus("当前 Timeline 只读。")
                    : FormatPreviewContentStatus(string.Empty));
                return true;
            }

            void ReleaseRuntimeInterest()
            {
                BtsmtlSkillObservationSession.Close();
                ResetNavigationFocus();
                if (!m_RuntimeInterest)
                    return;
                RuntimeDebugSession.Shared.ReleaseLiveInterest(m_InterestOwner);
                m_RuntimeInterest = false;
            }

            bool TryResolveSessionHost(out SimulationSessionHost sessionHost, out string error)
            {
                sessionHost = null;
                error = string.Empty;
                if (m_Profile == null || !m_Profile.IsValid)
                {
                    error = "请先选择有效的 ScenePlay Profile。";
                    return false;
                }
                if (!EditorApplication.isPlaying)
                {
                    BtsmtlScenePlayPreviewHost preview = BtsmtlScenePlayPreviewHost.Shared;
                    sessionHost = preview.Profile == m_Profile ? preview.Session : null;
                    if (!sessionHost)
                        error = preview.Error.Length != 0 ? preview.Error : "当前 Profile 尚未打开隐藏预览。";
                    m_ObservedSession = sessionHost;
                    return sessionHost != null;
                }
                SimulationSessionHost[] hosts = UnityEngine.Object.FindObjectsByType<SimulationSessionHost>(FindObjectsSortMode.None);
                for (int i = 0; i < hosts.Length; i++)
                {
                    SimulationSessionCompositionDefinition composition = hosts[i].Composition;
                    if (!string.Equals(composition.SessionId, m_Profile.ContextId, StringComparison.Ordinal))
                        continue;
                    if (sessionHost != null)
                    {
                        error = $"ScenePlay Context '{m_Profile.ContextId}' 匹配到多个 Session。";
                        return false;
                    }
                    sessionHost = hosts[i];
                }
                if (sessionHost != null)
                {
                    m_ObservedSession = sessionHost;
                    return true;
                }
                error = $"未找到 Context '{m_Profile.ContextId}' 对应的正式 ScenePlay Session。";
                return false;
            }

            bool TryResolveActorHost(out FixedCharacterHost actorHost, out string error)
            {
                actorHost = null;
                if (!TryResolveSessionHost(out SimulationSessionHost sessionHost, out error))
                    return false;
                if (!EditorApplication.isPlaying)
                {
                    actorHost = BtsmtlScenePlayPreviewHost.Shared.Actor;
                    if (!actorHost)
                        error = "预览 Actor 尚未装配。";
                    return actorHost != null;
                }
                FixedCharacterHost[] hosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(FindObjectsSortMode.None);
                for (int i = 0; i < hosts.Length; i++)
                {
                    if (hosts[i].SessionHost != sessionHost ||
                        !string.Equals(hosts[i].ActorId.Value, m_Profile.DefaultActorId, StringComparison.Ordinal))
                        continue;
                    if (actorHost != null)
                    {
                        error = $"ScenePlay Context '{m_Profile.ContextId}' 中 Actor '{m_Profile.DefaultActorId}' 不唯一。";
                        return false;
                    }
                    actorHost = hosts[i];
                }
                if (actorHost != null)
                    return true;
                error = $"ScenePlay Context '{m_Profile.ContextId}' 中没有 Actor '{m_Profile.DefaultActorId}'。";
                return false;
            }

            FixedCharacterHost ResolveActorHost()
            {
                TryResolveActorHost(out FixedCharacterHost host, out _);
                return host;
            }

            bool HasSessionHost()
            {
                return TryResolveSessionHost(out _, out _);
            }

            bool CanExportContent()
            {
                return Mode == TimelineWorkspaceMode.Preview && ResolveActorHost()?.TimelineHost != null;
            }

            bool CanPrepareContent()
            {
                return CanExportContent() && m_ExportedContent != null;
            }

            void ExportContent()
            {
                FixedCharacterHost host = ResolveActorHost();
                CharacterTimelineHost timelineHost = host?.TimelineHost;
                if (timelineHost == null)
                {
                    SetStatus("当前 Actor 没有可用 Timeline Host。");
                    return;
                }
                ClearContentWorkflow();
                if (!timelineHost.Content.TryExportContent(
                        out CharacterTimelineContentExport export,
                        out string error))
                {
                    SetContentStatus(CharacterTimelineContentAdoptionState.Failed, error);
                    return;
                }
                m_ExportedContent = export;
                SetContentStatus(CharacterTimelineContentAdoptionState.Exported, "Timeline 内容已冻结导出。");
            }

            void PrepareContent()
            {
                FixedCharacterHost host = ResolveActorHost();
                CharacterTimelineHost timelineHost = host?.TimelineHost;
                if (timelineHost == null || host.CharacterDefinition == null)
                {
                    SetStatus("当前 Actor 没有可用 Timeline Host。");
                    return;
                }
                if (m_ExportedContent == null)
                {
                    SetContentStatus(CharacterTimelineContentAdoptionState.Failed, "请先导出 Timeline 内容。");
                    return;
                }
                if (!timelineHost.Content.TryPrepareContentAdoption(
                        m_ExportedContent,
                        out CharacterTimelineContentAdoptionPlan plan,
                        out string error))
                {
                    InvalidateContentWorkflow(error);
                    return;
                }
                m_PendingPlan = plan;
                m_PublishedContent = null;
                SetContentStatus(
                    CharacterTimelineContentAdoptionState.Prepared,
                    plan.Message);
            }

            void PublishContent()
            {
                FixedCharacterHost host = ResolveActorHost();
                CharacterTimelineHost timelineHost = host?.TimelineHost;
                if (timelineHost == null)
                {
                    SetStatus("当前 Actor 没有可用 Timeline Host。");
                    return;
                }
                if (!timelineHost.Content.TryPublishContentAdoption(
                        m_PendingPlan,
                        out CharacterTimelineContentPublication publication,
                        out string error))
                {
                    InvalidateContentWorkflow(error);
                    return;
                }
                m_PublishedContent = publication;
                SetContentStatus(
                    CharacterTimelineContentAdoptionState.Published,
                    publication.Message);
            }

            void AdoptContent()
            {
                FixedCharacterHost host = ResolveActorHost();
                CharacterTimelineHost timelineHost = host?.TimelineHost;
                if (timelineHost == null)
                {
                    SetStatus("当前 Actor 没有可用 Timeline Host。");
                    return;
                }
                if (!timelineHost.Content.TryAdoptContent(
                        m_PublishedContent,
                        out CharacterTimelineContentAdoptionReport report))
                {
                    InvalidateContentWorkflow(report.Message);
                    return;
                }
                ClearContentWorkflow();
                SetContentStatus(
                    report.State,
                    report.Message);
            }

            void SetContentStatus(
                CharacterTimelineContentAdoptionState state,
                string message)
            {
                m_ContentState = state;
                SetStatus(FormatPreviewContentStatus(message));
            }

            string FormatPreviewContentStatus(string message)
            {
                if (!EditorApplication.isPlaying && !BtsmtlScenePlayPreviewHost.Shared.IsOpen)
                    return $"Preview | 未运行 | {message}";
                if (!TryResolveSessionHost(out SimulationSessionHost sessionHost, out string sessionError))
                    return $"Preview | 未连接 | {sessionError} | {message}";
                string target = $"Session {m_Profile.ContextId} #{sessionHost.SessionGeneration} | Actor {m_Profile.DefaultActorId}";
                if (sessionHost.LifecycleState != SimulationSessionLifecycleState.Active)
                    return $"Preview | {target} | {sessionHost.LifecycleState} | {sessionHost.Failure?.ToString() ?? message}";
                if (!TryResolveActorHost(out FixedCharacterHost actorHost, out string actorError))
                    return $"Preview | {target} | 未连接 | {actorError}";
                CharacterTimelineHost timelineHost = actorHost.TimelineHost;
                if (timelineHost == null || !timelineHost.IsInitialized)
                    return $"Preview | {target} | Timeline 尚未准备 | {message}";
                if (!timelineHost.Content.TryGetCurrentAuthoringContentRevision(out string authoring, out string revisionError))
                    return $"Preview | {target} | 作者内容不可用 | {revisionError}";
                string adopted = timelineHost.Content.AuthoringContentRevision;
                if (string.IsNullOrEmpty(adopted))
                    return $"Preview | {target} | 尚未收到实际采用版本 | {message}";
                string staged = m_PublishedContent?.AuthoringRevision ??
                                m_PendingPlan?.AuthoringRevision ??
                                m_ExportedContent?.AuthoringRevision;
                string stagedLabel = m_PublishedContent != null
                    ? "已发布"
                    : m_PendingPlan != null
                        ? "已准备"
                        : m_ExportedContent != null
                            ? "已导出"
                        : "待版本";
                string state = m_ContentState switch
                {
                    CharacterTimelineContentAdoptionState.Exported => "已导出",
                    CharacterTimelineContentAdoptionState.Prepared => "已准备",
                    CharacterTimelineContentAdoptionState.Published => "待采用",
                    CharacterTimelineContentAdoptionState.Adopted when !string.Equals(authoring, adopted, StringComparison.Ordinal) => "作者已修改",
                    CharacterTimelineContentAdoptionState.Adopted => "已采用",
                    CharacterTimelineContentAdoptionState.Rejected => "已拒绝",
                    CharacterTimelineContentAdoptionState.Failed => "失败",
                    _ => string.Equals(authoring, adopted, StringComparison.Ordinal) ? "已采用" : "作者已修改"
                };
                return $"Preview | {target} | {state} | 作者 {ShortRevision(authoring)} | 已采用 {ShortRevision(adopted)} | {stagedLabel} {ShortRevision(staged)} | RuntimeDebug {RuntimeDebugSession.Shared.TargetRevision} | {message}";
            }

            void BeginRuntimeDebugCapture()
            {
                RuntimeDebugSession debug = RuntimeDebugSession.Shared;
                bool started = debug.BeginCapture(CaptureTraceChannels, RuntimeDiagnosticsCaptureDetail.Continuous);
                SetStatus(FormatRuntimeDebugStatus(started ? "Capture 已开始。" : "Capture 无法开始。"));
            }

            void EndRuntimeDebugCapture()
            {
                RuntimeDebugSession debug = RuntimeDebugSession.Shared;
                bool ended = debug.EndCapture();
                SetStatus(FormatRuntimeDebugStatus(ended ? "Capture 已结束。" : "Capture 无法结束。"));
            }

            void SetRuntimeDebugHistoryOffset(int offset)
            {
                RuntimeDebugSession.Shared.SetHistoryOffset(offset);
                SetStatus(FormatRuntimeDebugStatus($"正在查看 Capture Segment {offset}。"));
            }

            void ResumeRuntimeDebugLive()
            {
                RuntimeDebugSession.Shared.ResumeLive();
                SetStatus(FormatRuntimeDebugStatus("已恢复 Live。"));
            }

            string FormatRuntimeDebugStatus(string message)
            {
                RuntimeDebugSession debug = RuntimeDebugSession.Shared;
                string sessionGeneration = TryResolveSessionHost(out SimulationSessionHost sessionHost, out _)
                    ? sessionHost.SessionGeneration.ToString()
                    : "-";
                string capture = debug.IsCaptureRecording
                    ? $"录制 {debug.CaptureSegmentCount}/{debug.CaptureSegmentCapacity}"
                    : debug.HasCaptureHistory
                        ? $"历史 {debug.HistoryOffset}/{Math.Max(0, debug.CaptureSnapshot.SegmentCount - 1)}"
                        : "无";
                return $"RuntimeDebug | Session {sessionGeneration} | TargetRevision {debug.TargetRevision} | {debug.AttachmentState} | Capture {capture} | {message}";
            }

            void ClearContentWorkflow()
            {
                m_ExportedContent = null;
                m_PendingPlan = null;
                m_PublishedContent = null;
                m_ContentState = CharacterTimelineContentAdoptionState.None;
            }

            void InvalidateContentWorkflow(string message)
            {
                ClearContentWorkflow();
                SetContentStatus(CharacterTimelineContentAdoptionState.Rejected, message);
            }

            static string ShortRevision(string revision) =>
                string.IsNullOrEmpty(revision)
                    ? "-"
                    : revision.Length <= 10 ? revision : revision.Substring(0, 10);

            void RefreshControls()
            {
                for (int i = m_Controls.Count - 1; i >= 0; i--)
                {
                    Controls controls = m_Controls[i];
                    if (controls.Window == null)
                    {
                        m_Controls.RemoveAt(i);
                        continue;
                    }
                    controls.RuntimeDebug.SetValueWithoutNotify(TimelineWorkspaceModeBridge.RuntimeDebugEnabled);
                    controls.RuntimeDebug.SetEnabled(HasRuntime && m_Profile != null && m_Profile.IsValid);
                    controls.Profile.SetValueWithoutNotify(m_Profile);
                }
                if (Mode == TimelineWorkspaceMode.Authoring &&
                    m_Profile != null &&
                    m_Profile.IsValid &&
                    string.Equals(m_Status, "请先选择有效的 ScenePlay Profile。", StringComparison.Ordinal))
                    m_Status = "Profile 已选择。";
            }

            void SetStatus(string message)
            {
                m_Status = message ?? string.Empty;
                Refresh();
            }


        }
    }
}
