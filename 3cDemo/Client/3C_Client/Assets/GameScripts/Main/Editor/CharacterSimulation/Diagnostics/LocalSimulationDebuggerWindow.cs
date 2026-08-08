using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.Tick;
using UnityEditor;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Editor
{
    public sealed class LocalSimulationDebuggerWindow : EditorWindow
    {
        readonly List<SimulationSessionDebugStatusSnapshot> m_Targets = new List<SimulationSessionDebugStatusSnapshot>();
        readonly List<string> m_TargetLabels = new List<string>();

        VisualElement m_TargetContainer;
        PopupField<string> m_TargetPopup;
        Label m_StatusLabel;
        Label m_IdentityLabel;
        Label m_DriveLabel;
        Label m_HistoryLabel;
        Label m_ResultLabel;
        Label m_HotkeyLabel;
        IntegerField m_StepCountField;
        PopupField<string> m_RatePopup;
        EnumField m_ClockField;
        LongField m_ReplayFromField;
        LongField m_ReplayToField;
        Button m_PauseButton;
        Button m_ResumeButton;
        Button m_StepOneButton;
        Button m_StepNButton;
        Button m_ApplyRateButton;
        Button m_ApplyClockButton;
        Button m_StartRecordingButton;
        Button m_StopRecordingButton;
        Button m_ReplayButton;
        Button m_ResumeFromTickButton;
        string m_SelectedTargetKey;

        [MenuItem("Tools/3C/Local Simulation Debugger")]
        public static void Open()
        {
            GetWindow<LocalSimulationDebuggerWindow>("Local Simulation Debugger");
        }

        void OnEnable()
        {
            LocalSimulationDebugControlService.TargetsChanged += RebuildTargets;
        }

        void OnDisable()
        {
            LocalSimulationDebugControlService.TargetsChanged -= RebuildTargets;
        }

        public void CreateGUI()
        {
            rootVisualElement.style.paddingLeft = 8;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.paddingTop = 8;
            rootVisualElement.style.paddingBottom = 8;

            m_TargetContainer = new VisualElement();
            rootVisualElement.Add(m_TargetContainer);

            m_StatusLabel = new Label();
            m_IdentityLabel = new Label();
            m_DriveLabel = new Label();
            m_HistoryLabel = new Label();
            m_ResultLabel = new Label();
            m_HotkeyLabel = new Label("Hotkeys in GameView: F5 Pause+Lock/Resume, F6 Step 1, F7 Step 8, F8 Resume Live, O Toggle 0.25x Slow Motion.");
            rootVisualElement.Add(m_StatusLabel);
            rootVisualElement.Add(m_IdentityLabel);
            rootVisualElement.Add(m_DriveLabel);
            rootVisualElement.Add(m_HistoryLabel);
            rootVisualElement.Add(m_ResultLabel);
            rootVisualElement.Add(m_HotkeyLabel);

            VisualElement driveRow = Row();
            m_PauseButton = new Button(() => SubmitSequence(
                SimulationSessionDebugCommand.SetPresentationClock(SelectedTargetKey(), GameplayPresentationDebugClockMode.LogicLockedPresentation),
                SimulationSessionDebugCommand.Pause(SelectedTargetKey())))
            { text = "Pause + Lock" };
            m_ResumeButton = new Button(() => SubmitSequence(
                SimulationSessionDebugCommand.SetPresentationClock(SelectedTargetKey(), GameplayPresentationDebugClockMode.LivePresentation),
                SimulationSessionDebugCommand.SetRealtime(SelectedTargetKey())))
            { text = "Resume Live" };
            m_StepOneButton = new Button(() => SubmitSequence(
                SimulationSessionDebugCommand.SetPresentationClock(SelectedTargetKey(), GameplayPresentationDebugClockMode.LogicLockedPresentation),
                SimulationSessionDebugCommand.Step(SelectedTargetKey(), 1)))
            { text = "Step 1" };
            driveRow.Add(m_PauseButton);
            driveRow.Add(m_ResumeButton);
            driveRow.Add(m_StepOneButton);
            rootVisualElement.Add(driveRow);

            VisualElement stepRow = Row();
            m_StepCountField = new IntegerField("Step N") { value = 8 };
            m_StepNButton = new Button(() =>
            {
                ulong count = (ulong)Math.Max(1, m_StepCountField.value);
                SubmitSequence(
                    SimulationSessionDebugCommand.SetPresentationClock(SelectedTargetKey(), GameplayPresentationDebugClockMode.LogicLockedPresentation),
                    SimulationSessionDebugCommand.Step(SelectedTargetKey(), count));
            })
            { text = "Run" };
            stepRow.Add(m_StepCountField);
            stepRow.Add(m_StepNButton);
            rootVisualElement.Add(stepRow);

            VisualElement rateRow = Row();
            m_RatePopup = new PopupField<string>("Rate", new List<string> { "0.25x", "0.5x", "1x", "2x", "4x" }, 2);
            m_ApplyRateButton = new Button(() => SubmitSequence(
                SimulationSessionDebugCommand.SetPresentationClock(SelectedTargetKey(), GameplayPresentationDebugClockMode.LivePresentation),
                SimulationSessionDebugCommand.SetRatePlayback(SelectedTargetKey(), SelectedRate())))
            { text = "Apply Rate" };
            rateRow.Add(m_RatePopup);
            rateRow.Add(m_ApplyRateButton);
            rootVisualElement.Add(rateRow);

            VisualElement clockRow = Row();
            m_ClockField = new EnumField("Presentation", GameplayPresentationDebugClockMode.LivePresentation);
            m_ApplyClockButton = new Button(() =>
                Submit(SimulationSessionDebugCommand.SetPresentationClock(SelectedTargetKey(), (GameplayPresentationDebugClockMode)m_ClockField.value)))
            { text = "Apply Clock" };
            clockRow.Add(m_ClockField);
            clockRow.Add(m_ApplyClockButton);
            rootVisualElement.Add(clockRow);

            VisualElement recordRow = Row();
            m_StartRecordingButton = new Button(() => Submit(SimulationSessionDebugCommand.StartRecording(SelectedTargetKey()))) { text = "Start Recording" };
            m_StopRecordingButton = new Button(() => Submit(SimulationSessionDebugCommand.StopRecording(SelectedTargetKey()))) { text = "Stop Recording" };
            recordRow.Add(m_StartRecordingButton);
            recordRow.Add(m_StopRecordingButton);
            rootVisualElement.Add(recordRow);

            VisualElement replayRow = Row();
            m_ReplayFromField = new LongField("From Tick") { value = 0 };
            m_ReplayToField = new LongField("To Tick") { value = 0 };
            m_ReplayButton = new Button(() => Submit(SimulationSessionDebugCommand.ReplayRange(
                SelectedTargetKey(),
                ClampTick(m_ReplayFromField.value),
                ClampTick(m_ReplayToField.value))))
            { text = "Replay" };
            m_ResumeFromTickButton = new Button(() => Submit(SimulationSessionDebugCommand.ResumeFromTick(
                SelectedTargetKey(),
                ClampTick(m_ReplayFromField.value))))
            { text = "Resume From" };
            replayRow.Add(m_ReplayFromField);
            replayRow.Add(m_ReplayToField);
            replayRow.Add(m_ReplayButton);
            replayRow.Add(m_ResumeFromTickButton);
            rootVisualElement.Add(replayRow);

            RebuildTargets();
            rootVisualElement.schedule.Execute(RefreshStatus).Every(250);
        }

        static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 6;
            row.style.marginBottom = 2;
            return row;
        }

        void RebuildTargets()
        {
            if (m_TargetContainer == null)
                return;

            m_Targets.Clear();
            m_TargetLabels.Clear();
            IReadOnlyList<SimulationSessionDebugStatusSnapshot> snapshots =
                LocalSimulationDebugControlService.CaptureStatusSnapshots();
            for (int i = 0; i < snapshots.Count; i++)
            {
                SimulationSessionDebugStatusSnapshot snapshot = snapshots[i];
                m_Targets.Add(snapshot);
                m_TargetLabels.Add($"{snapshot.Identity.DisplayName} | Tick {snapshot.LatestOuterTick}");
            }

            int selectedIndex = FindSelectedIndex();
            if (selectedIndex < 0 && m_Targets.Count > 0)
            {
                selectedIndex = 0;
                m_SelectedTargetKey = m_Targets[0].Identity.TargetKey;
            }

            m_TargetContainer.Clear();
            if (m_TargetLabels.Count == 0)
            {
                m_TargetPopup = null;
                m_TargetContainer.Add(new Label("No active Simulation Session debug target."));
                RefreshStatus();
                return;
            }

            m_TargetPopup = new PopupField<string>("Session", m_TargetLabels, selectedIndex);
            m_TargetPopup.RegisterValueChangedCallback(_ =>
            {
                int index = m_TargetPopup.index;
                if (index >= 0 && index < m_Targets.Count)
                    m_SelectedTargetKey = m_Targets[index].Identity.TargetKey;
                RefreshStatus();
            });
            m_TargetContainer.Add(m_TargetPopup);
            RefreshStatus();
        }

        void RefreshStatus()
        {
            RefreshSnapshotCache();
            SimulationSessionDebugStatusSnapshot status;
            bool hasTarget = TryGetSelectedStatus(out status);
            if (!hasTarget)
            {
                SetButtons(false, false, false);
                if (m_StatusLabel != null)
                    m_StatusLabel.text = "Status: no active target";
                if (m_IdentityLabel != null)
                    m_IdentityLabel.text = string.Empty;
                if (m_DriveLabel != null)
                    m_DriveLabel.text = string.Empty;
                if (m_HistoryLabel != null)
                    m_HistoryLabel.text = string.Empty;
                return;
            }

            bool active = status.LifecycleState == ThirdPersonSimulation.SimulationSessionLifecycleState.Active &&
                status.TickSystemAvailable;
            SetButtons(active, status.Capability.SupportsRecording, status.Capability.SupportsReplay);
            m_StatusLabel.text = $"Status: {status.LifecycleState} | OuterTick {status.LatestOuterTick} | Failure {status.FailureSummary}";
            m_IdentityLabel.text = $"Identity: Pipeline {status.Identity.Pipeline.Hash} | Program {status.Identity.ProgramCatalogHash}";
            m_DriveLabel.text = $"Drive: {status.DriveStatus.Mode} | Clock {status.DriveStatus.PresentationClockMode} | LocalTick {status.DriveStatus.LocalLogicTick} | Alpha {status.DriveStatus.InterpolationAlpha:0.000}";
            m_HistoryLabel.text = $"History: Recording {status.Recording} | Window {status.HistoryOldestTick}->{status.HistoryLatestTick} | Checkpoint {status.LatestCheckpointTick} | Hash {status.LatestHash}";
        }

        void RefreshSnapshotCache()
        {
            IReadOnlyList<SimulationSessionDebugStatusSnapshot> snapshots =
                LocalSimulationDebugControlService.CaptureStatusSnapshots();
            m_Targets.Clear();
            for (int i = 0; i < snapshots.Count; i++)
                m_Targets.Add(snapshots[i]);
        }

        void SetButtons(bool tickDrive, bool recording, bool replay)
        {
            m_PauseButton?.SetEnabled(tickDrive);
            m_ResumeButton?.SetEnabled(tickDrive);
            m_StepOneButton?.SetEnabled(tickDrive);
            m_StepNButton?.SetEnabled(tickDrive);
            m_ApplyRateButton?.SetEnabled(tickDrive);
            m_ApplyClockButton?.SetEnabled(tickDrive);
            m_StartRecordingButton?.SetEnabled(recording);
            m_StopRecordingButton?.SetEnabled(recording);
            m_ReplayButton?.SetEnabled(replay);
            m_ResumeFromTickButton?.SetEnabled(replay);
        }

        void Submit(SimulationSessionDebugCommand command)
        {
            SubmitSequence(command);
        }

        void SubmitSequence(params SimulationSessionDebugCommand[] commands)
        {
            string targetKey = SelectedTargetKey();
            if (string.IsNullOrEmpty(targetKey))
            {
                m_ResultLabel.text = "Command rejected: no target";
                return;
            }

            SimulationSessionDebugCommandResult result = default;
            for (int i = 0; i < commands.Length; i++)
            {
                LocalSimulationDebugControlService.TrySubmit(targetKey, commands[i], out result);
                if (!result.Accepted)
                    break;
            }
            m_ResultLabel.text = result.Accepted
                ? $"Command accepted #{result.CommandSequence}"
                : $"Command rejected #{result.CommandSequence}: {result.Code} {result.Message}";
            RefreshStatus();
        }

        string SelectedTargetKey()
        {
            if (!string.IsNullOrEmpty(m_SelectedTargetKey))
                return m_SelectedTargetKey;
            if (m_Targets.Count == 0)
                return string.Empty;
            return m_Targets[0].Identity.TargetKey;
        }

        bool TryGetSelectedStatus(out SimulationSessionDebugStatusSnapshot status)
        {
            string targetKey = SelectedTargetKey();
            for (int i = 0; i < m_Targets.Count; i++)
            {
                if (string.Equals(m_Targets[i].Identity.TargetKey, targetKey, StringComparison.Ordinal))
                {
                    status = m_Targets[i];
                    return true;
                }
            }
            status = default;
            return false;
        }

        int FindSelectedIndex()
        {
            if (string.IsNullOrEmpty(m_SelectedTargetKey))
                return -1;
            for (int i = 0; i < m_Targets.Count; i++)
            {
                if (string.Equals(m_Targets[i].Identity.TargetKey, m_SelectedTargetKey, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        float SelectedRate()
        {
            switch (m_RatePopup?.value)
            {
                case "0.25x":
                    return 0.25f;
                case "0.5x":
                    return 0.5f;
                case "2x":
                    return 2f;
                case "4x":
                    return 4f;
                default:
                    return 1f;
            }
        }

        static ulong ClampTick(long value)
        {
            return value <= 0 ? 0UL : (ulong)value;
        }
    }
}
