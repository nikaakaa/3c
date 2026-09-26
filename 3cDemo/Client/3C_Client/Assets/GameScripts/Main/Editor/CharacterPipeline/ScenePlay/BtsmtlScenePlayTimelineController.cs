using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Editor.ProductStartup;
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
            RuntimeDebugSession.Shared.Changed += s_Controller.Refresh;
            RuntimeDiagnosticsTargetRegistry.TargetRegistered += s_Controller.OnTargetChanged;
            RuntimeDiagnosticsTargetRegistry.TargetUnregistered += s_Controller.OnTargetChanged;
            EditorApplication.update += s_Controller.UpdateSessionState;
            TimelineEditorWindow.AuthoringRevisionChanged += s_Controller.OnAuthoringRevisionChanged;
            TimelineEditorWindow.AssetOpened += s_Controller.OnTimelineAssetOpened;
        }

        sealed class Controls
        {
            internal TimelineEditorWindow Window;
            internal ObjectField Profile;
            internal ToolbarMenu Mode;
            internal ToolbarMenu Session;
        }

        sealed class Controller : ITimelineWorkspaceModeController
        {
            const string ModeStateKey = "ThirdPersonCharacter.ScenePlay.Timeline.Mode";
            const string ProfileGuidStateKey = "ThirdPersonCharacter.ScenePlay.Timeline.ProfileGuid";
            const string DefaultProfileGuid = "f6a23791f8784a9f9ee35efd2ecbfcb7";
            const RuntimeTraceChannel TraceChannels =
                RuntimeTraceChannel.Graph |
                RuntimeTraceChannel.StateMachine |
                RuntimeTraceChannel.Timeline |
                RuntimeTraceChannel.Blackboard |
                RuntimeTraceChannel.Animation |
                RuntimeTraceChannel.Motion |
                RuntimeTraceChannel.Values;

            readonly object m_InterestOwner = new object();
            readonly List<Controls> m_Controls = new List<Controls>();
            readonly BtsmtlRuntimeFocusResolver m_RuntimeFocus = new();
            TimelineWorkspaceMode m_Mode = TimelineWorkspaceMode.RuntimeDebug;
            BtsmtlScenePlayProfile m_Profile;
            CharacterTimelineContentExport m_ExportedContent;
            CharacterTimelineContentAdoptionPlan m_PendingPlan;
            CharacterTimelineContentPublication m_PublishedContent;
            CharacterTimelineContentAdoptionState m_ContentState;
            string m_Status = "Authoring";
            bool m_RuntimeInterest;
            bool m_ConnectionRefreshQueued;
            SimulationSessionHost m_ObservedSession;
            SimulationSessionLifecycleState m_ObservedLifecycle;
            ulong m_ObservedGeneration;
            bool m_FollowRuntime = true;
            bool m_NavigationQueued;
            RuntimeInstanceKey m_LastFocusInstance;
            string m_LastFocusGraph;

            public Controller()
            {
                int mode = SessionState.GetInt(ModeStateKey, (int)TimelineWorkspaceMode.RuntimeDebug);
                TimelineWorkspaceMode persistedMode = (TimelineWorkspaceMode)mode;
                m_Mode = Enum.IsDefined(typeof(TimelineWorkspaceMode), persistedMode)
                    ? persistedMode
                    : TimelineWorkspaceMode.Authoring;
                string profileGuid = SessionState.GetString(ProfileGuidStateKey, DefaultProfileGuid);
                m_Profile = AssetDatabase.LoadAssetAtPath<BtsmtlScenePlayProfile>(
                    AssetDatabase.GUIDToAssetPath(profileGuid));
                TimelineWorkspaceModeBridge.SetActiveMode(m_Mode);
            }

            public VisualElement CreateControls(TimelineEditorWindow window)
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
                    Mode = new ToolbarMenu { text = ModeLabel(m_Mode) },
                    Session = new ToolbarMenu { text = "Session" }
                };
                controls.Profile.style.width = 170f;
                controls.Profile.tooltip = "唯一 ScenePlay Profile；详细 Scene / Context / Actor 配置在 Profile Inspector。";
                controls.Profile.RegisterValueChangedCallback(evt =>
                {
                    BtsmtlScenePlayProfile profile = evt.newValue as BtsmtlScenePlayProfile;
                    if (!ReferenceEquals(m_Profile, profile))
                    {
                        ReleaseRuntimeInterest();
                        ClearContentWorkflow();
                        m_Mode = TimelineWorkspaceMode.Authoring;
                        SessionState.SetInt(ModeStateKey, (int)m_Mode);
                        TimelineWorkspaceModeBridge.SetActiveMode(m_Mode);
                    }
                    m_Profile = profile;
                    string path = AssetDatabase.GetAssetPath(m_Profile);
                    SessionState.SetString(
                        ProfileGuidStateKey,
                        string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
                    SetStatus(m_Profile == null ? "未选择 ScenePlay Profile。" : m_Profile.IsValid ? "Profile 已选择。" : "ScenePlay Profile 配置无效。");
                });
                AddModeActions(controls.Mode);
                AddSessionActions(controls);
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.Add(controls.Profile);
                container.Add(controls.Mode);
                container.Add(controls.Session);
                m_Controls.Add(controls);
                RefreshControls();
                ApplyToWindow(window);
                QueueConnectionRefresh();
                return container;
            }

            public void ApplyToWindow(TimelineEditorWindow window)
            {
                if (!window)
                    return;
                window.SetRuntimeObservationReadOnly(
                    m_Mode == TimelineWorkspaceMode.RuntimeDebug && EditorApplication.isPlaying);
                window.SetRuntimeObservationStatus(m_Status);
            }

            public void OnWindowClosed(TimelineEditorWindow window)
            {
                for (int i = m_Controls.Count - 1; i >= 0; i--)
                    if (m_Controls[i].Window == window)
                        m_Controls.RemoveAt(i);
                if (m_Controls.Count == 0)
                    ReleaseRuntimeInterest();
            }

            internal void OnTargetChanged(RuntimeDiagnosticsTarget target) => QueueConnectionRefresh();

            void QueueConnectionRefresh()
            {
                if (m_ConnectionRefreshQueued)
                    return;
                m_ConnectionRefreshQueued = true;
                EditorApplication.delayCall += RefreshConnection;
            }

            void RefreshConnection()
            {
                m_ConnectionRefreshQueued = false;
                if (!EditorApplication.isPlaying)
                    return;
                if (m_Mode == TimelineWorkspaceMode.RuntimeDebug)
                    AttachRuntimeDebug();
                else if (m_Mode == TimelineWorkspaceMode.Preview)
                    SetStatus(FormatPreviewContentStatus(string.Empty));
            }

            internal void UpdateSessionState()
            {
                if (m_Mode == TimelineWorkspaceMode.Authoring || !m_ObservedSession)
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
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    if (m_Profile != null && m_Profile.IsValid)
                    {
                        m_Mode = TimelineWorkspaceMode.RuntimeDebug;
                        SessionState.SetInt(ModeStateKey, (int)m_Mode);
                        TimelineWorkspaceModeBridge.SetActiveMode(m_Mode);
                    }
                    QueueConnectionRefresh();
                }
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    m_ObservedSession = null;
                    ReleaseRuntimeInterest();
                    ClearContentWorkflow();
                    TimelineWorkspaceModeBridge.SetActiveMode(m_Mode);
                    SetStatus(m_Mode == TimelineWorkspaceMode.RuntimeDebug
                        ? FormatRuntimeDebugStatus("已退出 Play。")
                        : "Authoring");
                }
                Refresh();
            }

            internal void OnAuthoringRevisionChanged(TimelineEditorWindow window)
            {
                OnContentChanged();
            }

            internal void OnTimelineAssetOpened(TimelineAsset asset)
            {
                if (!EditorApplication.isPlaying || m_Mode != TimelineWorkspaceMode.RuntimeDebug)
                    return;
                TimelineEditorWindow window = TimelineEditorWindow.FindOpen(asset);
                if (window == null)
                    return;
                window.SetRuntimeObservationReadOnly(true);
                TimelineRuntimeObservationBridge.RefreshWindow(window);
            }

            internal void OnContentChanged()
            {
                if (m_Mode != TimelineWorkspaceMode.Preview || ResolveActorHost()?.TimelineHost == null)
                {
                    Refresh();
                    return;
                }
                if (m_ExportedContent != null &&
                    !string.Equals(
                        ResolveCurrentAuthoringRevision(),
                        m_ExportedContent.AuthoringRevision,
                        StringComparison.Ordinal))
                {
                    InvalidateContentWorkflow("作者内容已修改，之前的导出版本已失效，请重新导出。");
                    return;
                }
                if (m_Mode == TimelineWorkspaceMode.Preview)
                    SetStatus(FormatPreviewContentStatus(
                        string.Empty));
            }

            internal void Refresh()
            {
                if (m_Mode == TimelineWorkspaceMode.RuntimeDebug)
                {
                    m_RuntimeFocus.Refresh(RuntimeDebugSession.Shared.ViewModel);
                    QueueRuntimeNavigation();
                }
                RefreshControls();
                for (int i = 0; i < m_Controls.Count; i++)
                    ApplyToWindow(m_Controls[i].Window);
            }

            void AddModeActions(ToolbarMenu menu)
            {
                AppendModeAction(menu, TimelineWorkspaceMode.Authoring);
                AppendModeAction(menu, TimelineWorkspaceMode.Preview);
                AppendModeAction(menu, TimelineWorkspaceMode.RuntimeDebug);
            }

            void AppendModeAction(ToolbarMenu menu, TimelineWorkspaceMode mode)
            {
                menu.menu.AppendAction(
                    ModeLabel(mode),
                    _ => SetMode(mode),
                    _ => m_Mode == mode
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
            }

            void AddSessionActions(Controls controls)
            {
                ToolbarMenu menu = controls.Session;
                menu.menu.MenuItems().Clear();
                menu.menu.AppendAction(
                    "Start Preview",
                    _ => SetMode(TimelineWorkspaceMode.Preview),
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
                if (m_Mode == TimelineWorkspaceMode.Preview)
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
                if (m_Mode == TimelineWorkspaceMode.RuntimeDebug)
                {
                    AddRuntimePlaybackActions(menu, controls.Window);
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
                            if (window.SelectRuntimeObservationPlayback(playback))
                            {
                                m_FollowRuntime = false;
                                Refresh();
                            }
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
                        m_LastFocusInstance = default;
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
                EditorApplication.delayCall += FollowRuntime;
            }

            void FollowRuntime()
            {
                m_NavigationQueued = false;
                if (!m_FollowRuntime || m_Mode != TimelineWorkspaceMode.RuntimeDebug)
                    return;
                RuntimeDebugViewModel view = RuntimeDebugSession.Shared.ViewModel;
                if (!view.Attached)
                    return;
                if (!view.Valid)
                {
                    PublishNavigationStatus(view.Error);
                    return;
                }
                if (view.HasCoverageGap)
                {
                    PublishNavigationStatus("运行记录不完整，已停止自动跟随。");
                    return;
                }
                if (m_RuntimeFocus.Candidates.Count != 1)
                {
                    PublishNavigationStatus(m_RuntimeFocus.Candidates.Count == 0
                        ? "当前角色没有可导航的技能或 Timeline 运行事实。"
                        : "存在并行调用，请在 Session / RuntimeDebug / Pin 中选择具体实例。");
                    return;
                }
                RuntimeDebugEventView candidate = m_RuntimeFocus.Candidates[0];
                if (m_LastFocusInstance.Equals(candidate.Event.RuntimeInstance) &&
                    string.Equals(m_LastFocusGraph, candidate.Source.GraphAuthoringId, StringComparison.Ordinal))
                    return;
                NavigateRuntime(candidate);
            }

            void NavigateRuntime(RuntimeDebugEventView candidate)
            {
                if (!RuntimeDebugSourceNavigator.Open(candidate, followGraph: true))
                {
                    PublishNavigationStatus("当前调用缺少匹配版本的作者来源，无法导航。");
                    return;
                }
                m_LastFocusInstance = candidate.Event.RuntimeInstance;
                m_LastFocusGraph = candidate.Source.GraphAuthoringId;
                PublishNavigationStatus($"{(m_FollowRuntime ? "跟随" : "固定")} {candidate.SourceName} · 动作 {m_LastFocusInstance.ActionInstanceId}");
            }

            void PublishNavigationStatus(string message)
            {
                m_Status = FormatRuntimeDebugStatus(message);
                for (int i = 0; i < m_Controls.Count; i++)
                    ApplyToWindow(m_Controls[i].Window);
            }

            void SetMode(TimelineWorkspaceMode mode)
            {
                if (mode == TimelineWorkspaceMode.Authoring)
                {
                    ReleaseRuntimeInterest();
                    m_Mode = mode;
                    SessionState.SetInt(ModeStateKey, (int)m_Mode);
                    TimelineWorkspaceModeBridge.SetActiveMode(mode);
                    SetStatus("Authoring");
                    return;
                }
                if (m_Profile == null || !m_Profile.IsValid)
                {
                    SetStatus("请先选择有效的 ScenePlay Profile。");
                    return;
                }
                if (mode == TimelineWorkspaceMode.Preview)
                {
                    ReleaseRuntimeInterest();
                    m_Mode = mode;
                    SessionState.SetInt(ModeStateKey, (int)m_Mode);
                    TimelineWorkspaceModeBridge.SetActiveMode(mode);
                    if (!EditorApplication.isPlaying)
                        StartPreview();
                    else
                        SetStatus(FormatPreviewContentStatus(string.Empty));
                    return;
                }
                m_Mode = mode;
                SessionState.SetInt(ModeStateKey, (int)m_Mode);
                TimelineWorkspaceModeBridge.SetActiveMode(mode);
                if (!EditorApplication.isPlaying)
                    StartPreview();
                else
                    AttachRuntimeDebug();
            }

            void StartPreview()
            {
                ClearContentWorkflow();
                EditorPlayModeSceneLaunchResult result = EditorPlayModeSceneLauncher.Start(
                    new EditorPlayModeSceneLaunchRequest(
                        m_Profile.ScenePath,
                        m_Profile.ContextId,
                        ownerId: typeof(BtsmtlScenePlayTimelineController).FullName));
                SetStatus(result.Accepted
                    ? FormatPreviewContentStatus("ScenePlay 正在准备。")
                    : result.Message);
            }

            void StopSession()
            {
                bool resolved = TryResolveSessionHost(out SimulationSessionHost sessionHost, out string error);
                if (!resolved)
                {
                    SetStatus(error);
                    return;
                }
                sessionHost.Stop();
                if (EditorPlayModeSceneLauncher.TryGetPendingRequest(out EditorPlayModeSceneLaunchRequest request) &&
                    string.Equals(request.OwnerId, typeof(BtsmtlScenePlayTimelineController).FullName, StringComparison.Ordinal) &&
                    string.Equals(request.ScenePath, m_Profile.ScenePath, StringComparison.Ordinal) &&
                    string.Equals(request.ContextId, m_Profile.ContextId, StringComparison.Ordinal))
                    EditorApplication.ExitPlaymode();
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

            void AttachRuntimeDebug()
            {
                if (!EditorApplication.isPlaying)
                {
                    SetStatus("RuntimeDebug 需要先进入 Preview。");
                    return;
                }
                if (!TryResolveActorHost(out FixedCharacterHost host, out string resolveError))
                {
                    SetStatus(resolveError);
                    return;
                }
                if (!RuntimeDebugSession.Shared.AttachToHost(host.GetInstanceID()))
                {
                    SetStatus("Profile 对应 Actor 尚未注册 RuntimeDebug target。");
                    return;
                }
                RuntimeDebugSession.Shared.EnsureLiveInterest(m_InterestOwner, TraceChannels);
                m_RuntimeInterest = true;
                SetStatus(FormatRuntimeDebugStatus("当前 Timeline 只读。"));
            }

            void ReleaseRuntimeInterest()
            {
                BtsmtlSkillObservationSession.Close();
                m_LastFocusInstance = default;
                m_LastFocusGraph = null;
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
                SimulationSessionHost[] hosts = UnityEngine.Object.FindObjectsByType<SimulationSessionHost>(FindObjectsSortMode.None);
                for (int i = 0; i < hosts.Length; i++)
                {
                    SimulationSessionCompositionDefinition composition = hosts[i].Composition;
                    if (!composition ||
                        !string.Equals(hosts[i].gameObject.scene.path, m_Profile.ScenePath, StringComparison.Ordinal) ||
                        !string.Equals(composition.SessionId, m_Profile.ContextId, StringComparison.Ordinal))
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
                return m_Mode == TimelineWorkspaceMode.Preview && ResolveActorHost()?.TimelineHost != null;
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
                if (!timelineHost.TryExportContent(
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
                if (!timelineHost.TryPrepareContentAdoption(
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
                if (!timelineHost.TryPublishContentAdoption(
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
                if (!timelineHost.TryAdoptContent(
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
                if (!EditorApplication.isPlaying)
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
                if (!timelineHost.TryGetCurrentAuthoringContentRevision(out string authoring, out string revisionError))
                    return $"Preview | {target} | 作者内容不可用 | {revisionError}";
                string adopted = timelineHost.AuthoringContentRevision;
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

            string ResolveCurrentAuthoringRevision()
            {
                CharacterTimelineHost timelineHost = ResolveActorHost()?.TimelineHost;
                return timelineHost != null && timelineHost.TryGetCurrentAuthoringContentRevision(
                    out string revision,
                    out _)
                    ? revision
                    : string.Empty;
            }

            void BeginRuntimeDebugCapture()
            {
                RuntimeDebugSession debug = RuntimeDebugSession.Shared;
                bool started = debug.BeginCapture(TraceChannels, RuntimeDiagnosticsCaptureDetail.Continuous);
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
                    controls.Mode.text = ModeLabel(m_Mode);
                    controls.Profile.SetValueWithoutNotify(m_Profile);
                    AddSessionActions(controls);
                }
                if (m_Mode == TimelineWorkspaceMode.Authoring &&
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

            static string ModeLabel(TimelineWorkspaceMode mode) => mode switch
            {
                TimelineWorkspaceMode.Preview => "Preview",
                TimelineWorkspaceMode.RuntimeDebug => "RuntimeDebug",
                _ => "Authoring"
            };
        }
    }
}
