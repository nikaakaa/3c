#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using FlowCanvas;
using NodeCanvas.Editor;
using ParadoxNotion;
using ParadoxNotion.Design;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    static class BtsmtlSkillNodeInspector
    {
        public static void Draw(BtsmtlSkillFlowNode node)
        {
            var graph = (FlowGraph)node.graph;
            using var disabled = new EditorGUI.DisabledScope(graph.isEditorReadOnly);
            if (node is BtsmtlSkillLoopFlowNode loop)
                DrawLoop(graph, loop);
            if (node is BtsmtlSkillStateExitCauseFlowNode cause)
                DrawCause(graph, cause);
            if (node is BtsmtlSkillActionContextActiveFlowNode contextActive)
                DrawContextActive(graph, contextActive);
            if (node is BtsmtlSkillActionWindowActiveFlowNode window)
                DrawWindow(graph, window);
            if (node is BtsmtlSkillCanActivateActionFlowNode admission)
                DrawAdmission(graph, admission);
            if (node is BtsmtlSkillSubmitActionLifecycleFlowNode lifecycle)
                DrawLifecycle(graph, lifecycle);
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine)
                DrawStateMachine(graph, stateMachine);
            if (node is IBtsmtlSkillInputNode input)
                DrawInput(graph, input);
            if (node is BtsmtlSkillBlackboardReadFlowNode<bool> booleanRead)
                DrawBlackboardRead(graph, booleanRead);
            else if (node is BtsmtlSkillBlackboardReadFlowNode<float> scalarRead)
                DrawBlackboardRead(graph, scalarRead);
            if (node is BtsmtlSkillBlackboardAccessFlowNode blackboard)
                DrawBlackboardAccess(graph, blackboard);
            if (node is BtsmtlSkillTimelineFlowNode timeline)
                DrawTimeline(graph, timeline);
            if (node is BtsmtlSkillLocomotionFlowNode locomotion)
                DrawLocomotion(graph, locomotion);
            DrawValueInputs(graph, node);
        }

        static void DrawLoop(FlowGraph graph, BtsmtlSkillLoopFlowNode node)
        {
            var value = (BtsmtlSkillLoopStopType)EditorGUILayout.EnumPopup("停止方式", node.StopType);
            if (value != node.StopType)
                Change(graph, "修改技能循环", () => node.SetStopType(value));
        }

        static void DrawCause(FlowGraph graph, BtsmtlSkillStateExitCauseFlowNode node)
        {
            var value = (BtsmtlSkillStateExitCause)EditorGUILayout.EnumPopup("退出原因", node.Cause);
            if (value != node.Cause)
                Change(graph, "修改状态退出原因", () => node.SetCause(value));
        }

        static void DrawContextActive(FlowGraph graph, BtsmtlSkillActionContextActiveFlowNode node)
        {
            ActionContextSlot value = ObjectField("动作上下文", node.ActionContext, typeof(ActionContextSlot));
            if (value != node.ActionContext)
                Change(graph, "修改动作上下文", () => node.SetActionContext(value));
        }

        static void DrawWindow(FlowGraph graph, BtsmtlSkillActionWindowActiveFlowNode node)
        {
            string value = EditorGUILayout.DelayedTextField("窗口类型", node.WindowType);
            if (!string.Equals(value, node.WindowType, StringComparison.Ordinal))
                Change(graph, "修改动作窗口", () => node.SetWindowType(value));
        }

        static void DrawAdmission(FlowGraph graph, BtsmtlSkillCanActivateActionFlowNode node)
        {
            ActionProfile profile = ObjectField("动作配置", node.ActionProfile, typeof(ActionProfile));
            string declarationId = EditorGUILayout.DelayedTextField("目标声明", node.TargetSnapshotDeclarationId);
            string ownerId = EditorGUILayout.DelayedTextField("声明作用域", node.TargetSnapshotOwnerId);
            if (profile != node.ActionProfile ||
                !string.Equals(declarationId, node.TargetSnapshotDeclarationId, StringComparison.Ordinal) ||
                !string.Equals(ownerId, node.TargetSnapshotOwnerId, StringComparison.Ordinal))
                Change(graph, "修改动作准入", () => node.Configure(profile, declarationId, ownerId));
        }

        static void DrawLifecycle(FlowGraph graph, BtsmtlSkillSubmitActionLifecycleFlowNode node)
        {
            ActionContextSlot context = ObjectField("动作上下文", node.ActionContext, typeof(ActionContextSlot));
            var transition = (ActionLifecycleTransitionType)EditorGUILayout.EnumPopup("生命周期", node.TransitionType);
            string reason = EditorGUILayout.DelayedTextField("原因", node.Reason);
            if (context != node.ActionContext || transition != node.TransitionType ||
                !string.Equals(reason, node.Reason, StringComparison.Ordinal))
                Change(graph, "修改动作生命周期", () => node.Configure(context, transition, reason));
        }

        static void DrawStateMachine(FlowGraph graph, BtsmtlSkillStateMachineFlowNode node)
        {
            BtsmtlSkillFlowGraph value = (BtsmtlSkillFlowGraph)EditorGUILayout.ObjectField(
                "状态机页面",
                node.StateMachine,
                typeof(BtsmtlSkillFlowGraph),
                false);
            if (value != node.StateMachine)
                Change(graph, "修改技能状态机", () => node.SetStateMachine(value));
        }

        static void DrawInput(FlowGraph graph, IBtsmtlSkillInputNode node)
        {
            string value = EditorGUILayout.DelayedTextField("输入身份", node.InputId);
            if (string.Equals(value, node.InputId, StringComparison.Ordinal))
                return;
            Change(graph, "修改技能输入", () =>
            {
                switch (node)
                {
                    case BtsmtlSkillBooleanInputFlowNode boolean:
                        boolean.SetInputId(value);
                        break;
                    case BtsmtlSkillScalarInputFlowNode scalar:
                        scalar.SetInputId(value);
                        break;
                    case BtsmtlSkillVector2InputFlowNode vector:
                        vector.SetInputId(value);
                        break;
                    case BtsmtlSkillInputMagnitudeFlowNode magnitude:
                        magnitude.SetInputId(value);
                        break;
                    case BtsmtlSkillActionRequestFlowNode request:
                        request.SetInputId(value);
                        break;
                    default:
                        throw new InvalidOperationException("技能输入节点类型未登记。");
                }
            });
        }

        static void DrawBlackboardRead<T>(FlowGraph graph, BtsmtlSkillBlackboardReadFlowNode<T> node)
        {
            BtsmtlSkillBlackboardReference value = Reference(node.Variable);
            if (value.DeclarationId == node.Variable.DeclarationId &&
                value.OwnerId == node.Variable.OwnerId)
                return;
            if (!value.IsValid)
                return;
            Change(graph, "修改技能黑板读取", () => node.SetVariable(value));
        }

        static void DrawBlackboardAccess(FlowGraph graph, BtsmtlSkillBlackboardAccessFlowNode node)
        {
            BtsmtlSkillBlackboardReference variable = Reference(node.Variable);
            var valueType = (BtsmtlSkillBlackboardValueType)EditorGUILayout.EnumPopup("值类型", node.DeclaredType);
            UnityEngine.Object factContext = EditorGUILayout.ObjectField(
                "事实上下文",
                node.FactContext,
                typeof(UnityEngine.Object),
                false);
            if (variable.DeclarationId == node.Variable.DeclarationId &&
                variable.OwnerId == node.Variable.OwnerId &&
                valueType == node.DeclaredType &&
                factContext == node.FactContext)
                return;
            if (!variable.IsValid)
                return;
            Change(graph, "修改技能黑板访问", () => node.Configure(variable, valueType, factContext));
        }

        static void DrawTimeline(FlowGraph graph, BtsmtlSkillTimelineFlowNode node)
        {
            TimelineAsset timeline = ObjectField("Timeline", node.TimelineAsset, typeof(TimelineAsset));
            var ownership = (BtsmtlSkillTimelineOwnership)EditorGUILayout.EnumPopup("所有权", node.Ownership);
            ActionContextSlot context = ObjectField("动作上下文", node.ActionContext, typeof(ActionContextSlot));
            var playback = (TimelinePlaybackMode)EditorGUILayout.EnumPopup("播放模式", node.PlaybackMode);
            if (timeline != node.TimelineAsset || ownership != node.Ownership ||
                context != node.ActionContext || playback != node.PlaybackMode)
                Change(graph, "修改技能Timeline", () => node.Configure(timeline, ownership, context, playback));
            if (node.TimelineAsset && GUILayout.Button("打开Timeline编辑器"))
            {
                var observation = graph.editorObservation as IBtsmtlSkillObservationControls;
                observation?.NotifyTimelineOpening(node);
                try { AssetDatabase.OpenAsset(node.TimelineAsset); }
                finally { observation?.NotifyTimelineOpening(null); }
            }
        }

        static void DrawLocomotion(FlowGraph graph, BtsmtlSkillLocomotionFlowNode node)
        {
            float moveSpeed = EditorGUILayout.FloatField("移动速度", node.MoveSpeed);
            var displacement = (LocomotionInputMotionDisplacementMode)EditorGUILayout.EnumPopup("位移模式", node.DisplacementMode);
            RootMotionCurveAsset curve = ObjectField("动作曲线", node.ActionMotionCurve, typeof(RootMotionCurveAsset));
            float turnSpeed = EditorGUILayout.FloatField("转向速度", node.TurnSpeedDegrees);
            bool cameraRelative = EditorGUILayout.Toggle("相机相对", node.CameraRelative);
            var execution = (LocomotionInputMotionExecutionMode)EditorGUILayout.EnumPopup("执行模式", node.ExecutionMode);
            float duration = EditorGUILayout.FloatField("持续时间", node.DurationSeconds);
            if (!Mathf.Approximately(moveSpeed, node.MoveSpeed) ||
                displacement != node.DisplacementMode ||
                curve != node.ActionMotionCurve ||
                !Mathf.Approximately(turnSpeed, node.TurnSpeedDegrees) ||
                cameraRelative != node.CameraRelative ||
                execution != node.ExecutionMode ||
                !Mathf.Approximately(duration, node.DurationSeconds))
                Change(graph, "修改技能移动", () => node.Configure(
                    moveSpeed,
                    displacement,
                    curve,
                    turnSpeed,
                    cameraRelative,
                    execution,
                    duration));
        }

        static BtsmtlSkillBlackboardReference Reference(BtsmtlSkillBlackboardReference current)
        {
            string declarationId = EditorGUILayout.DelayedTextField("声明身份", current.DeclarationId);
            string ownerId = EditorGUILayout.DelayedTextField("作用域身份", current.OwnerId);
            return string.IsNullOrWhiteSpace(declarationId) || string.IsNullOrWhiteSpace(ownerId)
                ? default
                : new BtsmtlSkillBlackboardReference(declarationId, ownerId);
        }

        static void DrawValueInputs(FlowGraph graph, BtsmtlSkillFlowNode node)
        {
            foreach (ValueInput input in node.GetInputValuePorts())
            {
                if (input.isConnected)
                {
                    EditorGUILayout.LabelField(input.name, "[CONNECTED]");
                    continue;
                }
                object oldValue = input.serializedValue;
                object newValue = EditorUtils.ReflectedFieldInspector(
                    input.name,
                    oldValue,
                    input.type,
                    new InspectedFieldInfo(graph, null, null, null));
                if (Equals(oldValue, newValue))
                    continue;
                Change(graph, "修改技能默认输入", () =>
                {
                    if (input.isRequired && newValue == null)
                        throw new InvalidOperationException($"技能输入'{input.ID}'不能保存空默认值。");
                    if (newValue != null && !input.type.IsInstanceOfType(newValue))
                        throw new InvalidOperationException($"技能输入'{input.ID}'默认值类型不匹配。");
                    input.serializedValue = newValue;
                });
            }
        }

        static T ObjectField<T>(string label, T value, Type type) where T : UnityEngine.Object =>
            (T)EditorGUILayout.ObjectField(label, value, type, false);

        static void Change(FlowGraph graph, string title, Action mutation)
        {
            try { BtsmtlSkillFlowEditorMutation.Apply(graph, title, mutation); }
            catch (InvalidOperationException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
            catch (ArgumentException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
        }
    }
}
#endif
