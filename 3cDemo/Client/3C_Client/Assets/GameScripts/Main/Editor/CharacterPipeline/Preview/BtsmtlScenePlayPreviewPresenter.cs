#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using FlowCanvas;
using NodeCanvas.Editor;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.ScenePlay;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.Preview
{
    [InitializeOnLoad]
    static class BtsmtlScenePlayPreviewPresenterRegistration
    {
        static BtsmtlScenePlayPreviewPresenterRegistration()
        {
            BtsmtlSkillGraphPreviewToolbarRegistry.Register(DrawSkillGraphToolbar);
            GraphEditor.onEditorClosed += ClearSkillGraphPresenter;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static BtsmtlScenePlayPreviewPresenter s_SkillGraphPresenter;
        static FlowGraph s_SkillGraph;

        static void DrawSkillGraphToolbar(FlowGraph graph)
        {
            if (graph == null)
                return;
            if (!ReferenceEquals(s_SkillGraph, graph))
            {
                s_SkillGraphPresenter?.Dispose();
                s_SkillGraph = graph;
                s_SkillGraphPresenter = new BtsmtlScenePlayPreviewPresenter();
            }
            s_SkillGraphPresenter.DrawImmediateGUI();
        }

        static void ClearSkillGraphPresenter()
        {
            s_SkillGraphPresenter?.Dispose();
            s_SkillGraphPresenter = null;
            s_SkillGraph = null;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ClearSkillGraphPresenter();
        }
    }

    sealed class BtsmtlScenePlayPreviewPresenter : IDisposable
    {
        IBtsmtlScenePlayPreviewOperations m_Operations;
        BtsmtlScenePlayContext m_Context;
        bool m_StartPaused;
        bool m_ImmediateExpanded = true;
        bool m_Disposed;
        bool m_HistoryInputsInitialized;
        Guid m_HistoryCaptureId;
        long m_RestoreTick = -1;
        int m_HistoryOffset;
        long m_ReplayFromTick = -1;
        long m_ReplayToTick = -1;
        RuntimeExecutionHistory m_History;
        string m_StatusMessage = string.Empty;

        public BtsmtlScenePlayPreviewPresenter()
        {
            BtsmtlScenePlayPreviewOperationsRegistry.Changed += OnRegistryChanged;
            RuntimeDebugSession.Shared.Changed += OnRuntimeChanged;
            BindOperations(BtsmtlScenePlayPreviewOperationsRegistry.Current);
        }

        public event Action Changed;

        public BtsmtlScenePlayContext Context
        {
            get => m_Context;
            set
            {
                if (ReferenceEquals(m_Context, value))
                    return;
                m_Context = value;
                NotifyChanged();
            }
        }

        public bool StartPaused
        {
            get => m_StartPaused;
            set
            {
                if (m_StartPaused == value)
                    return;
                m_StartPaused = value;
                NotifyChanged();
            }
        }

        public BtsmtlScenePlayStatus Status => m_Operations?.Status ?? BtsmtlScenePlayStatus.Idle;
        public BtsmtlScenePlayBuildStatus BuildStatus => m_Operations?.BuildStatus ?? BtsmtlScenePlayBuildStatus.Idle;
        public string BuildStatusDescription => DescribeBuildStatus(BuildStatus);
        public bool HasOperations => m_Operations != null;
        public bool IsInputRecording => m_Operations?.IsInputRecording == true;
        public bool SupportsInputReplay => m_Operations?.SupportsInputReplay == true;
        public bool SupportsPresentationCheckpointRestore => m_Operations?.SupportsPresentationCheckpointRestore == true;
        public IReadOnlyList<string> ActorIds => m_Operations?.ActorIds ?? Array.Empty<string>();
        public IReadOnlyList<BtsmtlScenePlaySkillOption> SkillOptions =>
            m_Operations?.SkillOptions ?? Array.Empty<BtsmtlScenePlaySkillOption>();
        public string StatusMessage => string.IsNullOrEmpty(Status.FailureMessage)
            ? m_StatusMessage
            : Status.FailureMessage;
        public bool HasHistory => m_History != null && m_History.Checkpoints.Count != 0;
        public long RestoreTick
        {
            get => m_RestoreTick;
            set => m_RestoreTick = value;
        }
        public int HistoryOffset => m_HistoryOffset;
        public long ReplayFromTick
        {
            get => m_ReplayFromTick;
            set => m_ReplayFromTick = value;
        }
        public long ReplayToTick
        {
            get => m_ReplayToTick;
            set => m_ReplayToTick = value;
        }
        public bool CanRestore => HasHistory &&
            IsRunningOrPaused &&
            SupportsPresentationCheckpointRestore &&
            FindRestoreCheckpoint().CanRestore &&
            m_RestoreTick >= 0;
        public bool CanReplay => HasHistory &&
            IsRunningOrPaused &&
            SupportsInputReplay &&
            !m_History.HasExternalResults &&
            m_ReplayFromTick >= 0 &&
            m_ReplayToTick >= 0;

        public void Refresh()
        {
            RefreshState();
            NotifyChanged();
        }

        public void Start()
        {
            if (m_Operations == null)
                return;
            if (!m_Context)
            {
                SetStatus("Scene Play 需要明确的场景上下文。");
                return;
            }
            if (string.IsNullOrWhiteSpace(m_Context.ScenePath) || string.IsNullOrWhiteSpace(m_Context.ContextId))
            {
                SetStatus("Scene Play 上下文必须属于已保存场景，并具有 ContextId。");
                return;
            }
            Execute(m_Operations.Start(new BtsmtlScenePlayRequest(
                m_Context.ScenePath,
                m_Context.ContextId,
                startPaused: m_StartPaused)));
        }

        public void Pause() => Execute(m_Operations?.Pause());
        public void Resume() => Execute(m_Operations?.Resume());
        public void Reset() => Execute(m_Operations?.Reset());
        public void Stop() => Execute(m_Operations?.Stop());

        public void Build(string actorId)
        {
            if (m_Operations != null)
                Execute(m_Operations.Build(actorId));
        }

        public void RequestSkill(string actorId, string skillId)
        {
            if (m_Operations == null)
                return;
            BtsmtlScenePlaySkillRequestResult result = m_Operations.RequestSkill(actorId, skillId);
            SetStatus(result.Message);
            RefreshState();
            NotifyChanged();
        }

        public void ToggleInputRecording()
        {
            if (m_Operations == null)
                return;
            Execute(m_Operations.IsInputRecording
                ? m_Operations.StopInputRecording()
                : m_Operations.StartInputRecording());
        }

        public void SetHistoryOffset(int offset)
        {
            m_HistoryOffset = Mathf.Max(0, offset);
            RuntimeDebugSession.Shared.SetHistoryOffset(m_HistoryOffset);
        }

        public void Restore()
        {
            if (m_Operations == null || m_RestoreTick < 0)
                return;
            BtsmtlScenePlayCommandResult result = m_Operations.ResumeFromTick(checked((ulong)m_RestoreTick));
            if (result.Accepted && RuntimeDebugSession.Shared.CanResumeLiveTarget)
                RuntimeDebugSession.Shared.ResumeLive();
            Execute(result);
        }

        public void Replay()
        {
            if (m_Operations == null || m_ReplayFromTick < 0 || m_ReplayToTick < 0)
                return;
            BtsmtlScenePlayCommandResult result = m_Operations.ReplayInputRange(
                checked((ulong)m_ReplayFromTick),
                checked((ulong)m_ReplayToTick));
            if (result.Accepted && RuntimeDebugSession.Shared.CanResumeLiveTarget)
                RuntimeDebugSession.Shared.ResumeLive();
            Execute(result);
        }

        public void DrawImmediateGUI()
        {
            bool changed = GUI.changed;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            m_ImmediateExpanded = EditorGUILayout.Foldout(m_ImmediateExpanded, "预览", true);
            if (m_ImmediateExpanded)
            {
                m_Context = (BtsmtlScenePlayContext)EditorGUILayout.ObjectField(
                    "场景上下文",
                    m_Context,
                    typeof(BtsmtlScenePlayContext),
                    true);
                m_StartPaused = EditorGUILayout.Toggle("启动后暂停", m_StartPaused);
                DrawSceneControls();
                DrawExperimentControls();
                DrawObservationControls();
                DrawHistoryControls();
                if (!string.IsNullOrEmpty(StatusMessage))
                    EditorGUILayout.HelpBox(StatusMessage, Status.HasFailure ? MessageType.Error : MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            GUI.changed = changed;
        }

        void DrawSceneControls()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("开始", EditorStyles.toolbarButton))
                Start();
            if (GUILayout.Button("暂停", EditorStyles.toolbarButton))
                Pause();
            if (GUILayout.Button("继续", EditorStyles.toolbarButton))
                Resume();
            if (GUILayout.Button("重建", EditorStyles.toolbarButton))
                Reset();
            if (GUILayout.Button("结束", EditorStyles.toolbarButton))
                Stop();
            EditorGUILayout.EndHorizontal();
        }

        void DrawExperimentControls()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build", EditorStyles.toolbarButton))
                ShowBuildMenu();
            if (GUILayout.Button("Skill", EditorStyles.toolbarButton))
                ShowSkillMenu();
            EditorGUILayout.LabelField(BuildStatusDescription, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        void DrawObservationControls()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(IsInputRecording ? "停止输入录制" : "开始输入录制", EditorStyles.toolbarButton))
                ToggleInputRecording();
            EditorGUILayout.LabelField(
                HasOperations ? DescribeStatus(Status) : "预览协调器未注册",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        void DrawHistoryControls()
        {
            if (!HasHistory)
                return;
            EditorGUILayout.BeginHorizontal();
            m_HistoryOffset = EditorGUILayout.IntField("历史段", m_HistoryOffset);
            if (m_HistoryOffset != RuntimeDebugSession.Shared.HistoryOffset)
                SetHistoryOffset(m_HistoryOffset);
            m_RestoreTick = EditorGUILayout.LongField("恢复 Tick", m_RestoreTick);
            if (GUILayout.Button("恢复并继续", EditorStyles.toolbarButton))
                Restore();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_ReplayFromTick = EditorGUILayout.LongField("回放起点", m_ReplayFromTick);
            m_ReplayToTick = EditorGUILayout.LongField("回放终点", m_ReplayToTick);
            if (GUILayout.Button("输入回放", EditorStyles.toolbarButton))
                Replay();
            EditorGUILayout.EndHorizontal();
        }

        void ShowBuildMenu()
        {
            var menu = new GenericMenu();
            for (int index = 0; index < ActorIds.Count; index++)
            {
                string actorId = ActorIds[index];
                menu.AddItem(new GUIContent(actorId), false, () => Build(actorId));
            }
            if (ActorIds.Count == 0)
                menu.AddDisabledItem(new GUIContent("没有可构建的角色"));
            menu.ShowAsContext();
        }

        void ShowSkillMenu()
        {
            var menu = new GenericMenu();
            for (int index = 0; index < SkillOptions.Count; index++)
            {
                BtsmtlScenePlaySkillOption option = SkillOptions[index];
                string actorId = option.ActorId;
                string skillId = option.SkillId;
                menu.AddItem(new GUIContent($"{actorId} / {skillId}"), false, () => RequestSkill(actorId, skillId));
            }
            if (SkillOptions.Count == 0)
                menu.AddDisabledItem(new GUIContent("没有可请求的技能"));
            menu.ShowAsContext();
        }

        void Execute(BtsmtlScenePlayCommandResult? result)
        {
            if (!result.HasValue)
                return;
            SetStatus(result.Value.Message);
            RefreshState();
            NotifyChanged();
        }

        void SetStatus(string message)
        {
            m_StatusMessage = message ?? string.Empty;
            NotifyChanged();
        }

        void OnRegistryChanged()
        {
            BindOperations(BtsmtlScenePlayPreviewOperationsRegistry.Current);
        }

        void OnRuntimeChanged()
        {
            RefreshState();
            NotifyChanged();
        }

        void OnStatusChanged(BtsmtlScenePlayStatus _)
        {
            RefreshState();
            NotifyChanged();
        }

        void BindOperations(IBtsmtlScenePlayPreviewOperations operations)
        {
            if (ReferenceEquals(m_Operations, operations))
            {
                RefreshState();
                NotifyChanged();
                return;
            }
            if (m_Operations != null)
                m_Operations.StatusChanged -= OnStatusChanged;
            m_Operations = operations;
            if (m_Operations != null)
                m_Operations.StatusChanged += OnStatusChanged;
            RefreshState();
            NotifyChanged();
        }

        void RefreshState()
        {
            m_History = RuntimeDebugSession.Shared.BuildExecutionHistory();
            if (!HasHistory)
            {
                m_HistoryCaptureId = Guid.Empty;
                m_HistoryInputsInitialized = false;
                m_HistoryOffset = RuntimeDebugSession.Shared.HistoryOffset;
                return;
            }
            RuntimeExecutionCheckpoint checkpoint = FindRestoreCheckpoint();
            ulong latestTick = m_History.Ticks.Count == 0
                ? checkpoint.Tick
                : m_History.Ticks[m_History.Ticks.Count - 1].Tick;
            if (!m_HistoryInputsInitialized || m_HistoryCaptureId != m_History.CaptureId)
            {
                m_HistoryCaptureId = m_History.CaptureId;
                m_HistoryInputsInitialized = true;
                m_RestoreTick = checked((long)checkpoint.Tick);
                m_ReplayFromTick = checked((long)checkpoint.Tick);
                m_ReplayToTick = checked((long)latestTick);
            }
            m_HistoryOffset = RuntimeDebugSession.Shared.HistoryOffset;
        }

        RuntimeExecutionCheckpoint FindRestoreCheckpoint()
        {
            if (!HasHistory)
                return default;
            RuntimeExecutionCheckpoint checkpoint = m_History.Checkpoints[m_History.Checkpoints.Count - 1];
            for (int index = m_History.Checkpoints.Count - 1; index >= 0; index--)
            {
                if (m_History.Checkpoints[index].CanRestore)
                {
                    checkpoint = m_History.Checkpoints[index];
                    break;
                }
            }
            return checkpoint;
        }

        bool IsRunningOrPaused => Status.State == BtsmtlScenePlayState.Running ||
                                  Status.State == BtsmtlScenePlayState.Paused;

        static string DescribeStatus(BtsmtlScenePlayStatus status)
        {
            return status.State switch
            {
                BtsmtlScenePlayState.Idle => "未开始",
                BtsmtlScenePlayState.Checking => "检查中",
                BtsmtlScenePlayState.EnteringPlay => "进入运行",
                BtsmtlScenePlayState.Preparing => "准备中",
                BtsmtlScenePlayState.Running => "运行中",
                BtsmtlScenePlayState.Paused => "已暂停",
                BtsmtlScenePlayState.Resetting => "重建中",
                BtsmtlScenePlayState.Stopping => "结束中",
                BtsmtlScenePlayState.Building => "构建中",
                BtsmtlScenePlayState.NeedsBuild => "需要构建",
                BtsmtlScenePlayState.Faulted => "失败",
                _ => status.State.ToString()
            };
        }

        static string DescribeBuildStatus(BtsmtlScenePlayBuildStatus status)
        {
            return status.State switch
            {
                BtsmtlScenePlayBuildState.Building => "构建中",
                BtsmtlScenePlayBuildState.Published =>
                    $"构建完成，等待采用 Epoch {status.RequestedProgramEpoch}",
                BtsmtlScenePlayBuildState.Adopted =>
                    $"已采用 Epoch {status.AdoptedProgramEpoch}",
                BtsmtlScenePlayBuildState.Failed =>
                    string.IsNullOrEmpty(status.Message) ? "构建失败" : $"构建失败：{status.Message}",
                _ => "未构建"
            };
        }

        void NotifyChanged() => Changed?.Invoke();

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            BtsmtlScenePlayPreviewOperationsRegistry.Changed -= OnRegistryChanged;
            RuntimeDebugSession.Shared.Changed -= OnRuntimeChanged;
            if (m_Operations != null)
                m_Operations.StatusChanged -= OnStatusChanged;
            Changed = null;
        }
    }
}
#endif
