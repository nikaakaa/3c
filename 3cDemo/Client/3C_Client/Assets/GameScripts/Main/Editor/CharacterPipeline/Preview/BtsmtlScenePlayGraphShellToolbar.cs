#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.ScenePlay;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Editor.Preview
{
    [InitializeOnLoad]
    static class BtsmtlScenePlayGraphShellToolbarRegistration
    {
        static BtsmtlScenePlayGraphShellToolbarRegistration()
        {
            GraphAuthoringShellToolbarExtensionRegistry.Register(
                new BtsmtlScenePlayGraphShellToolbarExtension());
        }
    }

    sealed class BtsmtlScenePlayGraphShellToolbarExtension : IGraphAuthoringShellToolbarExtension
    {
        public VisualElement Create(GraphAuthoringEditorShell shell)
        {
            return shell is BaseTreeWindow
                ? new BtsmtlScenePlayGraphShellToolbar()
                : null;
        }
    }

    sealed class BtsmtlScenePlayGraphShellToolbar : VisualElement
    {
        readonly ObjectField m_ContextField;
        readonly Toggle m_StartPaused;
        readonly ToolbarButton m_StartButton;
        readonly ToolbarButton m_PauseButton;
        readonly ToolbarButton m_ResumeButton;
        readonly ToolbarButton m_ResetButton;
        readonly ToolbarButton m_StopButton;
        readonly ToolbarMenu m_BuildMenu;
        readonly ToolbarMenu m_SkillMenu;
        readonly ToolbarButton m_InputRecordButton;
        readonly LongField m_RestoreTickField;
        readonly ToolbarButton m_RestoreButton;
        readonly LongField m_ReplayFromTickField;
        readonly LongField m_ReplayToTickField;
        readonly ToolbarButton m_ReplayButton;
        readonly Label m_Status;
        IBtsmtlScenePlayPreviewOperations m_Operations;
        bool m_Disposed;
        Guid m_HistoryCaptureId;
        bool m_HistoryInputsInitialized;

        public BtsmtlScenePlayGraphShellToolbar()
        {
            AddToClassList("btsmtl-scene-play-graph-shell-toolbar");
            style.flexGrow = 1f;

            m_ContextField = new ObjectField("Scene Play")
            {
                objectType = typeof(BtsmtlScenePlayContext),
                allowSceneObjects = true
            };
            m_ContextField.style.width = 250f;
            m_ContextField.RegisterValueChangedCallback(_ => Refresh());
            Add(m_ContextField);

            m_StartPaused = new Toggle("Paused") { value = false };
            m_StartPaused.style.width = 70f;
            Add(m_StartPaused);

            m_StartButton = new ToolbarButton(Start) { text = "Start" };
            m_PauseButton = new ToolbarButton(Pause) { text = "Pause" };
            m_ResumeButton = new ToolbarButton(Resume) { text = "Resume" };
            m_ResetButton = new ToolbarButton(Reset) { text = "Reset" };
            m_StopButton = new ToolbarButton(Stop) { text = "Stop" };
            Add(m_StartButton);
            Add(m_PauseButton);
            Add(m_ResumeButton);
            Add(m_ResetButton);
            Add(m_StopButton);

            m_BuildMenu = new ToolbarMenu { text = "Build" };
            m_SkillMenu = new ToolbarMenu { text = "Skill" };
            Add(m_BuildMenu);
            Add(m_SkillMenu);

            m_InputRecordButton = new ToolbarButton(ToggleInputRecording) { text = "Record Input" };
            Add(m_InputRecordButton);

            m_RestoreTickField = new LongField("Restore") { value = -1 };
            m_RestoreTickField.style.width = 115f;
            m_RestoreButton = new ToolbarButton(Restore) { text = "Restore" };
            m_ReplayFromTickField = new LongField("Replay From") { value = -1 };
            m_ReplayFromTickField.style.width = 125f;
            m_ReplayToTickField = new LongField("To") { value = -1 };
            m_ReplayToTickField.style.width = 90f;
            m_ReplayButton = new ToolbarButton(Replay) { text = "Replay" };
            Add(m_RestoreTickField);
            Add(m_RestoreButton);
            Add(m_ReplayFromTickField);
            Add(m_ReplayToTickField);
            Add(m_ReplayButton);

            m_Status = new Label();
            m_Status.style.marginLeft = 6f;
            m_Status.style.flexGrow = 1f;
            Add(m_Status);

            BtsmtlScenePlayPreviewOperationsRegistry.Changed += OnRegistryChanged;
            RuntimeDebugSession.Shared.Changed += OnRuntimeChanged;
            RegisterCallback<DetachFromPanelEvent>(_ => Dispose());
            BindOperations(BtsmtlScenePlayPreviewOperationsRegistry.Current);
        }

        void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            BtsmtlScenePlayPreviewOperationsRegistry.Changed -= OnRegistryChanged;
            RuntimeDebugSession.Shared.Changed -= OnRuntimeChanged;
            BindOperations(null);
        }

        void OnRegistryChanged()
        {
            BindOperations(BtsmtlScenePlayPreviewOperationsRegistry.Current);
        }

        void OnRuntimeChanged()
        {
            RefreshHistoryControls();
        }

        void BindOperations(IBtsmtlScenePlayPreviewOperations operations)
        {
            if (ReferenceEquals(m_Operations, operations))
            {
                Refresh();
                return;
            }
            if (m_Operations != null)
                m_Operations.StatusChanged -= OnStatusChanged;
            m_Operations = operations;
            if (m_Operations != null)
                m_Operations.StatusChanged += OnStatusChanged;
            Refresh();
        }

        void OnStatusChanged(BtsmtlScenePlayStatus _)
        {
            Refresh();
        }

        void Start()
        {
            if (m_Operations == null)
                return;
            BtsmtlScenePlayContext context = m_ContextField.value as BtsmtlScenePlayContext;
            if (!context)
            {
                SetStatus("Scene Play requires an explicit context.");
                return;
            }
            if (string.IsNullOrWhiteSpace(context.ScenePath) || string.IsNullOrWhiteSpace(context.ContextId))
            {
                SetStatus("Scene Play context must have a saved Scene and ContextId.");
                return;
            }
            BtsmtlScenePlayCommandResult result = m_Operations.Start(
                new BtsmtlScenePlayRequest(
                    context.ScenePath,
                    context.ContextId,
                    startPaused: m_StartPaused.value));
            SetStatus(result.Message);
            Refresh();
        }

        void Pause()
        {
            Execute(m_Operations?.Pause());
        }

        void Resume()
        {
            Execute(m_Operations?.Resume());
        }

        void Reset()
        {
            Execute(m_Operations?.Reset());
        }

        void Stop()
        {
            Execute(m_Operations?.Stop());
        }

        void Execute(BtsmtlScenePlayCommandResult? result)
        {
            if (!result.HasValue)
                return;
            SetStatus(result.Value.Message);
            Refresh();
        }

        void BuildActor(string actorId)
        {
            if (m_Operations == null)
                return;
            Execute(m_Operations.Build(actorId));
        }

        void RequestSkill(string actorId, string skillId)
        {
            if (m_Operations == null)
                return;
            BtsmtlScenePlaySkillRequestResult result = m_Operations.RequestSkill(actorId, skillId);
            SetStatus(result.Message);
            Refresh();
        }

        void ToggleInputRecording()
        {
            if (m_Operations == null)
                return;
            Execute(m_Operations.IsInputRecording
                ? m_Operations.StopInputRecording()
                : m_Operations.StartInputRecording());
        }

        void Restore()
        {
            if (m_Operations == null || m_RestoreTickField.value < 0)
                return;
            BtsmtlScenePlayCommandResult result = m_Operations.ResumeFromTick(
                checked((ulong)m_RestoreTickField.value));
            if (result.Accepted && RuntimeDebugSession.Shared.CanResumeLiveTarget)
                RuntimeDebugSession.Shared.ResumeLive();
            Execute(result);
        }

        void Replay()
        {
            if (m_Operations == null || m_ReplayFromTickField.value < 0 || m_ReplayToTickField.value < 0)
                return;
            BtsmtlScenePlayCommandResult result = m_Operations.ReplayInputRange(
                checked((ulong)m_ReplayFromTickField.value),
                checked((ulong)m_ReplayToTickField.value));
            if (result.Accepted && RuntimeDebugSession.Shared.CanResumeLiveTarget)
                RuntimeDebugSession.Shared.ResumeLive();
            Execute(result);
        }

        void Refresh()
        {
            if (m_Disposed)
                return;
            BtsmtlScenePlayStatus status = m_Operations != null
                ? m_Operations.Status
                : BtsmtlScenePlayStatus.Idle;
            bool active = m_Operations != null && status.IsActive;
            m_StartButton.SetEnabled(m_Operations != null && !active && status.State != BtsmtlScenePlayState.Checking);
            m_PauseButton.SetEnabled(m_Operations != null && status.State == BtsmtlScenePlayState.Running);
            m_ResumeButton.SetEnabled(m_Operations != null && status.State == BtsmtlScenePlayState.Paused);
            m_ResetButton.SetEnabled(m_Operations != null &&
                                     (status.State == BtsmtlScenePlayState.Running ||
                                      status.State == BtsmtlScenePlayState.Paused));
            m_StopButton.SetEnabled(m_Operations != null && active);
            RefreshBuildMenu(status);
            RefreshSkillMenu(status);
            m_InputRecordButton.SetEnabled(m_Operations != null &&
                                           m_Operations.SupportsInputReplay &&
                                           (status.State == BtsmtlScenePlayState.Running ||
                                            status.State == BtsmtlScenePlayState.Paused));
            m_InputRecordButton.text = m_Operations != null && m_Operations.IsInputRecording
                ? "Stop Input"
                : "Record Input";
            RefreshHistoryControls();
            if (m_Operations != null && !string.IsNullOrEmpty(status.FailureMessage))
                SetStatus(status.FailureMessage);
        }

        void RefreshBuildMenu(BtsmtlScenePlayStatus status)
        {
            m_BuildMenu.menu.MenuItems().Clear();
            if (m_Operations == null)
            {
                m_BuildMenu.text = "Build";
                m_BuildMenu.SetEnabled(false);
                return;
            }
            IReadOnlyList<string> actorIds = m_Operations.ActorIds;
            for (int index = 0; index < actorIds.Count; index++)
            {
                string actorId = actorIds[index];
                m_BuildMenu.menu.AppendAction(actorId, _ => BuildActor(actorId));
            }
            BtsmtlScenePlayBuildStatus build = m_Operations.BuildStatus;
            m_BuildMenu.text = build.IsActive ? "Building" : "Build";
            m_BuildMenu.tooltip = build.Message;
            m_BuildMenu.SetEnabled(
                (status.State == BtsmtlScenePlayState.Running || status.State == BtsmtlScenePlayState.Paused) &&
                actorIds.Count != 0 &&
                !build.IsActive &&
                !build.IsPublished);
        }

        void RefreshSkillMenu(BtsmtlScenePlayStatus status)
        {
            m_SkillMenu.menu.MenuItems().Clear();
            if (m_Operations == null)
            {
                m_SkillMenu.text = "Skill";
                m_SkillMenu.SetEnabled(false);
                return;
            }
            IReadOnlyList<BtsmtlScenePlaySkillOption> options = m_Operations.SkillOptions;
            for (int index = 0; index < options.Count; index++)
            {
                BtsmtlScenePlaySkillOption option = options[index];
                string actorId = option.ActorId;
                string skillId = option.SkillId;
                m_SkillMenu.menu.AppendAction(
                    $"{actorId} / {skillId}",
                    _ => RequestSkill(actorId, skillId));
            }
            BtsmtlScenePlayBuildStatus build = m_Operations.BuildStatus;
            m_SkillMenu.tooltip = build.Message;
            m_SkillMenu.SetEnabled(
                status.State == BtsmtlScenePlayState.Running &&
                !build.IsActive &&
                !build.IsPublished &&
                options.Count != 0);
        }

        void RefreshHistoryControls()
        {
            RuntimeExecutionHistory history = RuntimeDebugSession.Shared.BuildExecutionHistory();
            bool hasHistory = history != null && history.Checkpoints.Count != 0;
            m_RestoreTickField.SetDisplay(hasHistory);
            m_RestoreButton.SetDisplay(hasHistory);
            m_ReplayFromTickField.SetDisplay(hasHistory);
            m_ReplayToTickField.SetDisplay(hasHistory);
            m_ReplayButton.SetDisplay(hasHistory);
            if (!hasHistory || m_Operations == null)
            {
                m_HistoryCaptureId = Guid.Empty;
                m_HistoryInputsInitialized = false;
                m_RestoreButton.SetEnabled(false);
                m_ReplayButton.SetEnabled(false);
                return;
            }
            RuntimeExecutionCheckpoint checkpoint = history.Checkpoints[history.Checkpoints.Count - 1];
            for (int index = history.Checkpoints.Count - 1; index >= 0; index--)
            {
                if (history.Checkpoints[index].CanRestore)
                {
                    checkpoint = history.Checkpoints[index];
                    break;
                }
            }
            ulong latestTick = history.Ticks.Count == 0
                ? checkpoint.Tick
                : history.Ticks[history.Ticks.Count - 1].Tick;
            if (!m_HistoryInputsInitialized || m_HistoryCaptureId != history.CaptureId)
            {
                m_HistoryCaptureId = history.CaptureId;
                m_HistoryInputsInitialized = true;
                m_RestoreTickField.SetValueWithoutNotify(checked((long)checkpoint.Tick));
                m_ReplayFromTickField.SetValueWithoutNotify(checked((long)checkpoint.Tick));
                m_ReplayToTickField.SetValueWithoutNotify(checked((long)latestTick));
            }
            bool running = m_Operations.Status.State == BtsmtlScenePlayState.Running ||
                           m_Operations.Status.State == BtsmtlScenePlayState.Paused;
            m_RestoreButton.SetEnabled(running && m_Operations.SupportsPresentationCheckpointRestore && checkpoint.CanRestore);
            m_ReplayButton.SetEnabled(running && m_Operations.SupportsInputReplay && !history.HasExternalResults);
        }

        void SetStatus(string value)
        {
            m_Status.text = value ?? string.Empty;
            m_Status.tooltip = value ?? string.Empty;
        }
    }

    static class BtsmtlScenePlayGraphShellToolbarExtensions
    {
        public static void SetDisplay(this VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
#endif
