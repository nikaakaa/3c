using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
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
            EditorApplication.projectChanged += s_Controller.Refresh;
            RuntimeDebugSession.Shared.Changed += s_Controller.Refresh;
            TimelineEditorWindow.AuthoringRevisionChanged += s_Controller.OnAuthoringRevisionChanged;
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
            TimelineWorkspaceMode m_Mode = TimelineWorkspaceMode.Authoring;
            BtsmtlScenePlayProfile m_Profile;
            CharacterTimelineContentExport m_ExportedContent;
            CharacterTimelineContentAdoptionPlan m_PendingPlan;
            CharacterTimelineContentPublication m_PublishedContent;
            CharacterTimelineContentAdoptionState m_ContentState;
            string m_Status = "Authoring";
            bool m_RuntimeInterest;

            public Controller()
            {
                int mode = SessionState.GetInt(ModeStateKey, (int)TimelineWorkspaceMode.Authoring);
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
                return container;
            }

            public void ApplyToWindow(TimelineEditorWindow window)
            {
                if (!window)
                    return;
                window.SetRuntimeObservationReadOnly(m_Mode == TimelineWorkspaceMode.RuntimeDebug);
                window.SetRuntimeObservationStatus(m_Status);
            }

            public void OnWindowClosed(TimelineEditorWindow window)
            {
                for (int i = m_Controls.Count - 1; i >= 0; i--)
                    if (m_Controls[i].Window == window)
                        m_Controls.RemoveAt(i);
            }

            internal void OnPlayModeChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    if (m_Mode == TimelineWorkspaceMode.RuntimeDebug)
                        EditorApplication.delayCall += AttachRuntimeDebug;
                    else if (m_Mode == TimelineWorkspaceMode.Preview)
                        SetStatus(FormatPreviewContentStatus("ScenePlay 已连接。"));
                }
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    ReleaseRuntimeInterest();
                    ClearContentWorkflow();
                    m_Mode = TimelineWorkspaceMode.Authoring;
                    SessionState.SetInt(ModeStateKey, (int)m_Mode);
                    TimelineWorkspaceModeBridge.SetActiveMode(m_Mode);
                    SetStatus("Authoring");
                }
                Refresh();
            }

            internal void OnAuthoringRevisionChanged(TimelineEditorWindow window)
            {
                if (!IsPreviewContentWindow(window))
                    return;
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
                        "作者内容已修改，尚未导出。"));
            }

            internal void Refresh()
            {
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
                    _ => HasSessionHost() || EditorApplication.isPlaying
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
                        SetStatus(FormatPreviewContentStatus("ScenePlay 已连接。"));
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
                if (resolved)
                    sessionHost.Stop();
                if (EditorApplication.isPlaying)
                    EditorApplication.ExitPlaymode();
                SetStatus(resolved ? "ScenePlay 已提交停止。" : error);
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
                    if (!composition || !string.Equals(composition.SessionId, m_Profile.ContextId, StringComparison.Ordinal))
                        continue;
                    if (sessionHost != null)
                    {
                        error = $"ScenePlay Context '{m_Profile.ContextId}' 匹配到多个 Session。";
                        return false;
                    }
                    sessionHost = hosts[i];
                }
                if (sessionHost != null)
                    return true;
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

            bool IsPreviewContentWindow(TimelineEditorWindow window)
            {
                TimelineAsset source = window?.SourceAsset;
                CharacterPipelineDefinition definition = ResolveActorHost()?.CharacterDefinition;
                return source != null && definition != null && definition.ControlMotionTimelines.Any(asset => asset == source);
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
                CharacterTimelineHost timelineHost = ResolveActorHost()?.TimelineHost;
                string authoring = ResolveCurrentAuthoringRevision();
                string adopted = timelineHost?.AuthoringContentRevision;
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
                string sessionGeneration = TryResolveSessionHost(out SimulationSessionHost sessionHost, out _)
                    ? sessionHost.SessionGeneration.ToString()
                    : "-";
                return $"Preview | {state} | 作者 {ShortRevision(authoring)} | 已采用 {ShortRevision(adopted)} | {stagedLabel} {ShortRevision(staged)} | Session {sessionGeneration} | RuntimeDebug {RuntimeDebugSession.Shared.TargetRevision} | {message}";
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
