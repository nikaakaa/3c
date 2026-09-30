using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Tick;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    sealed class BtsmtlScenePlayPreviewView : VisualElement
    {
        readonly BtsmtlScenePlayPreviewHost m_Host = BtsmtlScenePlayPreviewHost.Shared;
        readonly Image m_Image = new Image { scaleMode = ScaleMode.ScaleToFit };
        readonly Label m_Status = new Label();
        readonly ToolbarButton m_Play;
        readonly ToolbarButton m_Pause;
        readonly ToolbarButton m_Step;
        readonly PopupField<float> m_Rate = new PopupField<float>(new List<float> { 0.1f, 0.25f, 0.5f, 1f, 2f }, 3,
            value => value.ToString("0.##") + "×", value => value.ToString("0.##") + "×");
        readonly PopupField<GameplayPresentationDebugClockMode> m_Clock = new PopupField<GameplayPresentationDebugClockMode>(
            new List<GameplayPresentationDebugClockMode> { GameplayPresentationDebugClockMode.LogicLockedPresentation,
                GameplayPresentationDebugClockMode.LivePresentation }, 0, ClockLabel, ClockLabel);
        readonly ToolbarMenu m_Abilities = new ToolbarMenu { text = "技能图" };
        readonly PopupField<string> m_InputRequest = new PopupField<string>();
        readonly ToolbarButton m_SubmitInput;

        internal BtsmtlScenePlayPreviewView()
        {
            name = "workbench-preview-viewport";
            style.flexGrow = 1;
            style.minHeight = 160;
            style.minWidth = 300;
            var toolbar = new Toolbar();
            m_Play = new ToolbarButton(() => m_Host.Enqueue(GameplayTickDriveCommand.SetRatePlayback(m_Rate.value))) { text = "播放" };
            m_Pause = new ToolbarButton(() => m_Host.Enqueue(GameplayTickDriveCommand.Pause())) { text = "暂停" };
            m_Step = new ToolbarButton(() => m_Host.Enqueue(GameplayTickDriveCommand.Step(1))) { text = "单步" };
            toolbar.Add(m_Play);
            toolbar.Add(m_Pause);
            toolbar.Add(m_Step);
            m_Rate.tooltip = "播放速度；暂停时修改将在下次播放采用";
            m_Rate.style.width = 64;
            m_Rate.RegisterValueChangedCallback(evt =>
            {
                if (m_Host.IsReady && m_Host.DriveStatus.Mode != GameplayTickDriveMode.Paused)
                    m_Host.Enqueue(GameplayTickDriveCommand.SetRatePlayback(evt.newValue));
            });
            m_Clock.style.width = 120;
            m_Clock.RegisterValueChangedCallback(evt =>
                m_Host.Enqueue(GameplayTickDriveCommand.SetPresentationClock(evt.newValue)));
            toolbar.Add(m_Rate);
            toolbar.Add(m_Clock);
            toolbar.Add(m_Status);
            Add(toolbar);
            var inputToolbar = new Toolbar();
            m_InputRequest.tooltip = "当前角色输入配置中的正式动作请求；下一个逻辑 Tick 接收";
            m_InputRequest.style.width = 140;
            m_SubmitInput = new ToolbarButton(() => m_Host.Actor.EnqueueAbilityInputRequest(m_InputRequest.value))
                { text = "提交输入" };
            inputToolbar.Add(m_Abilities);
            inputToolbar.Add(m_InputRequest);
            inputToolbar.Add(m_SubmitInput);
            Add(inputToolbar);
            m_Image.style.flexGrow = 1;
            m_Image.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
            Add(m_Image);
            m_Image.RegisterCallback<GeometryChangedEvent>(_ => UpdateViewport());
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Host.Changed += OnHostChanged;
                m_Host.Renderer.Changed += OnImageChanged;
                OnHostChanged();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Host.Changed -= OnHostChanged;
                m_Host.Renderer.Changed -= OnImageChanged;
                ReleaseViewport();
            });
        }

        internal void SetVisible(bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (style.display.value == display)
                return;
            style.display = display;
            if (visible)
                OnHostChanged();
            else
                ReleaseViewport();
        }

        void OnHostChanged()
        {
            bool ready = m_Host.IsReady;
            m_Play.SetEnabled(ready);
            m_Pause.SetEnabled(ready);
            m_Step.SetEnabled(ready);
            m_Rate.SetEnabled(ready);
            m_Clock.SetEnabled(ready);
            m_Abilities.SetEnabled(ready);
            m_Abilities.menu.MenuItems().Clear();
            m_InputRequest.choices.Clear();
            if (ready)
            {
                m_Clock.SetValueWithoutNotify(m_Host.DriveStatus.PresentationClockMode);
                var definition = m_Host.Actor.CharacterDefinition;
                foreach (var graph in definition.AbilityGraphs)
                    m_Abilities.menu.AppendAction(graph.name.Replace('/', '→'), _ =>
                    {
                        BtsmtlScenePlayTimelineController.EnableRuntimeDebug();
                        RuntimeDebugSourceNavigator.Open(definition, RuntimeSourceElementKey.Graph(graph.AuthoringId));
                    });
                if (m_Host.Actor.ControlSource is FixedPlayerCharacterControlSource playerInput)
                    foreach (var request in playerInput.InputProfile.ActionRequests)
                        m_InputRequest.choices.Add(request.RequestId);
            }
            bool hasRequests = m_InputRequest.choices.Count != 0;
            m_InputRequest.SetValueWithoutNotify(hasRequests ? m_InputRequest.choices[0] : string.Empty);
            m_InputRequest.SetEnabled(hasRequests);
            m_SubmitInput.SetEnabled(hasRequests);
            m_Status.text = m_Host.Error.Length != 0 ? m_Host.Error : ready ? "" : "预览未运行";
            if (ready)
                UpdateViewport();
            else
                ReleaseViewport();
        }

        static string ClockLabel(GameplayPresentationDebugClockMode mode) =>
            mode == GameplayPresentationDebugClockMode.LogicLockedPresentation ? "表现随逻辑步进" : "表现随播放帧";

        void UpdateViewport()
        {
            if (panel == null || resolvedStyle.display == DisplayStyle.None || !m_Host.IsReady)
                return;
            int width = Mathf.CeilToInt(m_Image.contentRect.width * EditorGUIUtility.pixelsPerPoint);
            int height = Mathf.CeilToInt(m_Image.contentRect.height * EditorGUIUtility.pixelsPerPoint);
            if (width <= 0 || height <= 0)
                return;
            m_Host.Renderer.SetViewport(this, new Vector2Int(width, height));
            m_Host.RefreshPreviewImage();
            m_Image.image = m_Host.Renderer.Texture;
        }

        void OnImageChanged()
        {
            if (panel == null || resolvedStyle.display == DisplayStyle.None)
                return;
            m_Image.image = m_Host.Renderer.Texture;
            m_Image.MarkDirtyRepaint();
        }

        void ReleaseViewport()
        {
            m_Host.Renderer.RemoveViewport(this);
            m_Image.image = null;
        }
    }
}
