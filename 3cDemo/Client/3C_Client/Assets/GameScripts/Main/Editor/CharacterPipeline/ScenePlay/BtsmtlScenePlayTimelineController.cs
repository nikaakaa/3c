using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
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
            RuntimeDebugSession.Shared.Changed += s_Controller.Refresh;
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
            CharacterTimelineContentAdoptionPlan m_PendingPlan;
            CharacterTimelineContentPublication m_PublishedContent;
            string m_Status = "Authoring";
            bool m_RuntimeInterest;

            public Controller()
            {
                int mode = SessionState.GetInt(ModeStateKey, (int)TimelineWorkspaceMode.Authoring);
                m_Mode = Enum.IsDefined(typeof(TimelineWorkspaceMode), mode)
                    ? (TimelineWorkspaceMode)mode
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
                    m_Profile = evt.newValue as BtsmtlScenePlayProfile;
                    string path = AssetDatabase.GetAssetPath(m_Profile);
                    SessionState.SetString(
                        ProfileGuidStateKey,
                        string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
                    SetStatus(m_Profile == null ? "未选择 ScenePlay Profile。" : m_Profile.IsValid ? "Profile 已选择。" : "ScenePlay Profile 配置无效。");
                });
                AddModeActions(controls.Mode);
                AddSessionActions(controls.Session);
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
                        SetStatus("Preview | ScenePlay 已连接。");
                }
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    ReleaseRuntimeInterest();
                    m_Mode = TimelineWorkspaceMode.Authoring;
                    SessionState.SetInt(ModeStateKey, (int)m_Mode);
                    TimelineWorkspaceModeBridge.SetActiveMode(m_Mode);
                    SetStatus("Authoring");
                }
                Refresh();
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

            void AddSessionActions(ToolbarMenu menu)
            {
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
                menu.menu.AppendSeparator("Content/");
                menu.menu.AppendAction(
                    "Content/Prepare",
                    _ => PrepareContent(),
                    _ => CanPrepareContent()
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction(
                    "Content/Publish",
                    _ => PublishContent(),
                    _ => m_PendingPlan != null
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction(
                    "Content/Adopt",
                    _ => AdoptContent(),
                    _ => m_PublishedContent != null
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
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
                        SetStatus("Preview | ScenePlay 已连接。");
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
                EditorPlayModeSceneLaunchResult result = EditorPlayModeSceneLauncher.Start(
                    new EditorPlayModeSceneLaunchRequest(
                        m_Profile.ScenePath,
                        m_Profile.ContextId,
                        ownerId: typeof(BtsmtlScenePlayTimelineController).FullName));
                SetStatus(result.Accepted ? "Preview | ScenePlay 正在准备。" : result.Message);
            }

            void StopSession()
            {
                SimulationSessionHost sessionHost = UnityEngine.Object.FindObjectOfType<SimulationSessionHost>();
                sessionHost?.Stop();
                if (EditorApplication.isPlaying)
                    EditorApplication.ExitPlaymode();
                SetStatus("ScenePlay 已提交停止。");
            }

            void SubmitSessionCommand(bool pause)
            {
                SimulationSessionHost host = UnityEngine.Object.FindObjectOfType<SimulationSessionHost>();
                if (!host)
                {
                    SetStatus("当前没有 ScenePlay Session。");
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
                FixedCharacterHost host = ResolveActorHost();
                if (!host || !RuntimeDebugSession.Shared.AttachToHost(host.GetInstanceID()))
                {
                    SetStatus("Profile 对应 Actor 尚未注册 RuntimeDebug target。");
                    return;
                }
                RuntimeDebugSession.Shared.EnsureLiveInterest(m_InterestOwner, TraceChannels);
                m_RuntimeInterest = true;
                SetStatus("RuntimeDebug | 当前 Timeline 只读。");
            }

            void ReleaseRuntimeInterest()
            {
                if (!m_RuntimeInterest)
                    return;
                RuntimeDebugSession.Shared.ReleaseLiveInterest(m_InterestOwner);
                m_RuntimeInterest = false;
            }

            FixedCharacterHost ResolveActorHost()
            {
                FixedCharacterHost[] hosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(FindObjectsSortMode.None);
                if (hosts.Length == 0)
                    return null;
                if (m_Profile == null)
                    return null;
                for (int i = 0; i < hosts.Length; i++)
                    if (string.Equals(hosts[i].ActorId.Value, m_Profile.DefaultActorId, StringComparison.Ordinal))
                        return hosts[i];
                return null;
            }

            bool HasSessionHost()
            {
                return UnityEngine.Object.FindObjectOfType<SimulationSessionHost>() != null;
            }

            bool CanPrepareContent()
            {
                return m_Mode == TimelineWorkspaceMode.Preview && ResolveActorHost()?.TimelineHost != null;
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
                if (!timelineHost.TryPrepareContentAdoption(
                        host.CharacterDefinition.ControlMotionTimelines,
                        out CharacterTimelineContentAdoptionPlan plan,
                        out string error))
                {
                    SetStatus(error);
                    return;
                }
                m_PendingPlan = plan;
                m_PublishedContent = null;
                SetStatus($"Prepared {plan.ContentRevision}。");
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
                    SetStatus(error);
                    return;
                }
                m_PublishedContent = publication;
                SetStatus($"Published {publication.ContentRevision}。");
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
                    SetStatus(report.Message);
                    return;
                }
                m_PendingPlan = null;
                m_PublishedContent = null;
                SetStatus($"Adopted {report.ContentRevision}。");
            }

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
                }
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
