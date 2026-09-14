#if UNITY_EDITOR
using System;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
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
                ? new BtsmtlScenePlayGraphShellToolbar(
                    (shell as BaseTreeWindow).Tree?.GraphAuthoringId)
                : null;
        }
    }

    sealed class BtsmtlScenePlayGraphShellToolbar : VisualElement
    {
        readonly BtsmtlScenePlayPreviewPresenter m_Presenter;
        readonly ObjectField m_SceneField;
        readonly TextField m_ContextIdField;
        readonly Toggle m_StartPaused;
        readonly ToolbarButton m_StartButton;
        readonly ToolbarButton m_PauseButton;
        readonly ToolbarButton m_ResumeButton;
        readonly ToolbarButton m_ResetButton;
        readonly ToolbarButton m_StopButton;
        readonly ToolbarMenu m_SkillMenu;
        readonly Label m_TimelineAuthoringStatus;
        readonly ToolbarButton m_CaptureButton;
        readonly ToolbarButton m_InputRecordButton;
        readonly LongField m_RestoreTickField;
        readonly IntegerField m_HistoryOffsetField;
        readonly ToolbarButton m_RestoreButton;
        readonly LongField m_ReplayFromTickField;
        readonly LongField m_ReplayToTickField;
        readonly ToolbarButton m_ReplayButton;
        readonly Label m_Status;
        bool m_Disposed;

        public BtsmtlScenePlayGraphShellToolbar(string graphAuthoringId)
        {
            m_Presenter = new BtsmtlScenePlayPreviewPresenter(graphAuthoringId);
            m_Presenter.Changed += Refresh;
            AddToClassList("btsmtl-scene-play-graph-shell-toolbar");
            style.flexGrow = 1f;

            var sceneControls = new Foldout { text = "场景控制", value = true };
            var experimentControls = new Foldout { text = "试验与采用", value = true };
            var observationControls = new Foldout { text = "观察", value = true };
            var historyControls = new Foldout { text = "历史与录制", value = false };
            Add(sceneControls);
            Add(experimentControls);
            Add(observationControls);
            Add(historyControls);

            m_SceneField = new ObjectField("Scene")
            {
                objectType = typeof(SceneAsset),
                allowSceneObjects = false
            };
            m_SceneField.style.width = 250f;
            m_SceneField.RegisterValueChangedCallback(evt => m_Presenter.SceneAsset = evt.newValue as SceneAsset);
            sceneControls.Add(m_SceneField);

            m_ContextIdField = new TextField("ContextId");
            m_ContextIdField.style.width = 180f;
            m_ContextIdField.RegisterValueChangedCallback(evt => m_Presenter.ContextId = evt.newValue);
            sceneControls.Add(m_ContextIdField);

            m_StartPaused = new Toggle("Paused") { value = m_Presenter.StartPaused };
            m_StartPaused.style.width = 70f;
            m_StartPaused.RegisterValueChangedCallback(evt => m_Presenter.StartPaused = evt.newValue);
            sceneControls.Add(m_StartPaused);

            m_StartButton = new ToolbarButton(() => m_Presenter.Start()) { text = "Start" };
            m_PauseButton = new ToolbarButton(() => m_Presenter.Pause()) { text = "Pause" };
            m_ResumeButton = new ToolbarButton(() => m_Presenter.Resume()) { text = "Resume" };
            m_ResetButton = new ToolbarButton(() => m_Presenter.Reset()) { text = "Reset" };
            m_StopButton = new ToolbarButton(() => m_Presenter.Stop()) { text = "Stop" };
            sceneControls.Add(m_StartButton);
            sceneControls.Add(m_PauseButton);
            sceneControls.Add(m_ResumeButton);
            sceneControls.Add(m_ResetButton);
            sceneControls.Add(m_StopButton);

            m_SkillMenu = new ToolbarMenu { text = "Skill" };
            m_TimelineAuthoringStatus = new Label();
            m_TimelineAuthoringStatus.style.marginLeft = 6f;
            experimentControls.Add(m_SkillMenu);
            experimentControls.Add(m_TimelineAuthoringStatus);

            m_CaptureButton = new ToolbarButton(() => m_Presenter.ToggleDiagnosticCapture()) { text = "Capture" };
            observationControls.Add(m_CaptureButton);
            m_InputRecordButton = new ToolbarButton(() => m_Presenter.ToggleInputRecording()) { text = "Record Input" };
            observationControls.Add(m_InputRecordButton);

            m_RestoreTickField = new LongField("Restore") { value = -1 };
            m_RestoreTickField.style.width = 115f;
            m_RestoreTickField.RegisterValueChangedCallback(evt => m_Presenter.RestoreTick = evt.newValue);
            m_HistoryOffsetField = new IntegerField("Segment") { value = 0 };
            m_HistoryOffsetField.style.width = 95f;
            m_HistoryOffsetField.RegisterValueChangedCallback(evt => m_Presenter.SetHistoryOffset(evt.newValue));
            m_RestoreButton = new ToolbarButton(() => m_Presenter.Restore()) { text = "Restore" };
            m_ReplayFromTickField = new LongField("Replay From") { value = -1 };
            m_ReplayFromTickField.style.width = 125f;
            m_ReplayFromTickField.RegisterValueChangedCallback(evt => m_Presenter.ReplayFromTick = evt.newValue);
            m_ReplayToTickField = new LongField("To") { value = -1 };
            m_ReplayToTickField.style.width = 90f;
            m_ReplayToTickField.RegisterValueChangedCallback(evt => m_Presenter.ReplayToTick = evt.newValue);
            m_ReplayButton = new ToolbarButton(() => m_Presenter.Replay()) { text = "Replay" };
            historyControls.Add(m_RestoreTickField);
            historyControls.Add(m_HistoryOffsetField);
            historyControls.Add(m_RestoreButton);
            historyControls.Add(m_ReplayFromTickField);
            historyControls.Add(m_ReplayToTickField);
            historyControls.Add(m_ReplayButton);

            m_Status = new Label();
            m_Status.style.marginLeft = 6f;
            m_Status.style.flexGrow = 1f;
            Add(m_Status);

            RegisterCallback<DetachFromPanelEvent>(_ => Dispose());
            Refresh();
        }

        void Refresh()
        {
            if (m_Disposed)
                return;
            BtsmtlScenePlayStatus status = m_Presenter.Status;
            bool active = status.IsActive;
            m_StartButton.SetEnabled(m_Presenter.HasOperations && !active && status.State != BtsmtlScenePlayState.Checking);
            m_PauseButton.SetEnabled(m_Presenter.HasOperations && status.State == BtsmtlScenePlayState.Running);
            m_ResumeButton.SetEnabled(m_Presenter.HasOperations && status.State == BtsmtlScenePlayState.Paused);
            m_ResetButton.SetEnabled(m_Presenter.HasOperations &&
                                     (status.State == BtsmtlScenePlayState.Running ||
                                      status.State == BtsmtlScenePlayState.Paused));
            m_StopButton.SetEnabled(m_Presenter.HasOperations && active);
            RefreshSkillMenu(status);
            m_TimelineAuthoringStatus.text = m_Presenter.TimelineAuthoringDescription;
            m_TimelineAuthoringStatus.tooltip = "Timeline作者版本只表示当前作者内容，不代表运行时已经采用。";
            m_InputRecordButton.SetEnabled(m_Presenter.HasOperations &&
                                           m_Presenter.SupportsInputReplay &&
                                           (status.State == BtsmtlScenePlayState.Running ||
                                            status.State == BtsmtlScenePlayState.Paused));
            m_InputRecordButton.text = m_Presenter.IsInputRecording ? "Stop Input" : "Record Input";
            m_CaptureButton.SetEnabled(
                m_Presenter.IsDiagnosticCaptureRecording
                    ? m_Presenter.CanStopDiagnosticCapture
                    : m_Presenter.CanStartDiagnosticCapture);
            m_CaptureButton.text = m_Presenter.IsDiagnosticCaptureRecording
                ? "Stop Capture"
                : "Start Capture";
            RefreshHistoryControls();
            string message = m_Presenter.StatusMessage;
            m_Status.text = string.IsNullOrEmpty(message) ? DescribeStatus(status) : message;
            m_Status.tooltip = m_Status.text;
        }

        void RefreshSkillMenu(BtsmtlScenePlayStatus status)
        {
            m_SkillMenu.menu.MenuItems().Clear();
            if (!m_Presenter.HasOperations)
            {
                m_SkillMenu.text = "Skill";
                m_SkillMenu.SetEnabled(false);
                return;
            }
            for (int index = 0; index < m_Presenter.SkillOptions.Count; index++)
            {
                BtsmtlScenePlaySkillOption option = m_Presenter.SkillOptions[index];
                string actorId = option.ActorId;
                string skillId = option.SkillId;
                m_SkillMenu.menu.AppendAction(
                    $"{actorId} / {skillId}",
                    _ => m_Presenter.RequestSkill(actorId, skillId));
            }
            m_SkillMenu.SetEnabled(
                status.State == BtsmtlScenePlayState.Running &&
                m_Presenter.SkillOptions.Count != 0);
        }

        void RefreshHistoryControls()
        {
            bool hasHistory = m_Presenter.HasHistory;
            m_RestoreTickField.SetDisplay(hasHistory);
            m_HistoryOffsetField.SetDisplay(hasHistory);
            m_RestoreButton.SetDisplay(hasHistory);
            m_ReplayFromTickField.SetDisplay(hasHistory);
            m_ReplayToTickField.SetDisplay(hasHistory);
            m_ReplayButton.SetDisplay(hasHistory);
            if (!hasHistory)
            {
                m_RestoreButton.SetEnabled(false);
                m_HistoryOffsetField.SetEnabled(false);
                m_ReplayButton.SetEnabled(false);
                return;
            }
            m_RestoreTickField.SetValueWithoutNotify(m_Presenter.RestoreTick);
            m_HistoryOffsetField.SetValueWithoutNotify(m_Presenter.HistoryOffset);
            m_ReplayFromTickField.SetValueWithoutNotify(m_Presenter.ReplayFromTick);
            m_ReplayToTickField.SetValueWithoutNotify(m_Presenter.ReplayToTick);
            m_HistoryOffsetField.SetEnabled(RuntimeDebugSession.Shared.HasCaptureHistory);
            m_RestoreButton.SetEnabled(m_Presenter.CanRestore);
            m_ReplayButton.SetEnabled(m_Presenter.CanReplay);
        }

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
                BtsmtlScenePlayState.Faulted => "失败",
                _ => status.State.ToString()
            };
        }

        void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Presenter.Changed -= Refresh;
            m_Presenter.Dispose();
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
