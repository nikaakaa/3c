#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using NodeCanvas.Editor;
using ParadoxNotion;
using ParadoxNotion.Design;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
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
            if (node is BtsmtlSkillMoveFacingAngleFlowNode moveFacing)
                DrawMoveFacing(graph, moveFacing);
            if (node is BtsmtlSkillGameplayTagFlowNode tag)
                DrawGameplayTag(graph, tag);
            if (node is BtsmtlSkillGameplayTagQueryFlowNode tagQuery)
                DrawGameplayTagQuery(graph, tagQuery);
            if (node is BtsmtlSkillGameplayAttributeFlowNode attribute)
                DrawGameplayAttribute(graph, attribute);
            if (node is BtsmtlSkillApplyGameplayEffectFlowNode applyEffect)
                DrawApplyGameplayEffect(graph, applyEffect);
            if (node is BtsmtlSkillRemoveGameplayEffectFlowNode removeEffect)
                DrawRemoveGameplayEffect(graph, removeEffect);
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

        static void DrawMoveFacing(FlowGraph graph, BtsmtlSkillMoveFacingAngleFlowNode node)
        {
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", node.ProviderOwnerId);
            if (!string.Equals(owner, node.ProviderOwnerId, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(owner))
                Change(graph, "修改Character State引用", () => node.Configure(owner));
        }

        static void DrawGameplayTag(FlowGraph graph, BtsmtlSkillGameplayTagFlowNode node)
        {
            string id = EditorGUILayout.DelayedTextField("Tag ID", node.Tag.Value);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", node.ProviderOwnerId);
            if (!string.Equals(id, node.Tag.Value, StringComparison.Ordinal) ||
                !string.Equals(owner, node.ProviderOwnerId, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Tag引用", () => node.Configure(new GameplayTagId(id), owner));
        }

        static void DrawGameplayTagQuery(FlowGraph graph, BtsmtlSkillGameplayTagQueryFlowNode node)
        {
            string all = EditorGUILayout.DelayedTextField("All Tags", JoinTags(node.Query.All));
            string any = EditorGUILayout.DelayedTextField("Any Tags", JoinTags(node.Query.Any));
            string none = EditorGUILayout.DelayedTextField("None Tags", JoinTags(node.Query.None));
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", node.ProviderOwnerId);
            if (!string.Equals(all, JoinTags(node.Query.All), StringComparison.Ordinal) ||
                !string.Equals(any, JoinTags(node.Query.Any), StringComparison.Ordinal) ||
                !string.Equals(none, JoinTags(node.Query.None), StringComparison.Ordinal) ||
                !string.Equals(owner, node.ProviderOwnerId, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Tag Query", () => node.Configure(
                    new GameplayTagQuery(ParseTags(all), ParseTags(any), ParseTags(none)), owner));
        }

        static void DrawGameplayAttribute(FlowGraph graph, BtsmtlSkillGameplayAttributeFlowNode node)
        {
            string id = EditorGUILayout.DelayedTextField("Attribute ID", node.Attribute.Value);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", node.ProviderOwnerId);
            if (!string.Equals(id, node.Attribute.Value, StringComparison.Ordinal) ||
                !string.Equals(owner, node.ProviderOwnerId, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Attribute引用", () => node.Configure(new GameplayAttributeId(id), owner));
        }

        static void DrawApplyGameplayEffect(FlowGraph graph, BtsmtlSkillApplyGameplayEffectFlowNode node)
        {
            GameplayEffectDefinition effect = ObjectField("Gameplay Effect", node.Effect, typeof(GameplayEffectDefinition));
            ActionContextSlot context = ObjectField("Action Context", node.ActionContext, typeof(ActionContextSlot));
            bool predicted = EditorGUILayout.Toggle("Predicted", node.Predicted);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", node.ProviderOwnerId);
            if (effect != node.Effect || context != node.ActionContext || predicted != node.Predicted ||
                !string.Equals(owner, node.ProviderOwnerId, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Effect应用", () => node.Configure(effect, context, predicted, owner));
        }

        static void DrawRemoveGameplayEffect(FlowGraph graph, BtsmtlSkillRemoveGameplayEffectFlowNode node)
        {
            GameplayEffectRemoveSelector selector = (GameplayEffectRemoveSelector)EditorGUILayout.EnumPopup("Selector", node.Selector);
            long handleValue = EditorGUILayout.LongField("Handle", (long)node.Handle);
            ulong handle = handleValue < 0 ? 0UL : (ulong)handleValue;
            GameplayEffectDefinition effect = ObjectField("Gameplay Effect", node.Effect, typeof(GameplayEffectDefinition));
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", node.ProviderOwnerId);
            if (selector != node.Selector || handle != node.Handle || effect != node.Effect ||
                !string.Equals(owner, node.ProviderOwnerId, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Effect移除", () => node.Configure(selector, handle, effect, node.EffectTagQuery, owner));
        }

        static string JoinTags(IReadOnlyList<GameplayTagId> tags) =>
            string.Join(",", tags?.Select(value => value.Value) ?? Array.Empty<string>());

        static IReadOnlyList<GameplayTagId> ParseTags(string value) =>
            (value ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => new GameplayTagId(item.Trim()))
                .Where(item => item.IsValid)
                .ToArray();

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
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", node.ProviderOwnerId);
            if (string.Equals(value, node.InputId, StringComparison.Ordinal) &&
                string.Equals(owner, node.ProviderOwnerId, StringComparison.Ordinal))
                return;
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(owner))
                return;
            Change(graph, "修改技能输入", () =>
            {
                switch (node)
                {
                    case BtsmtlSkillBooleanInputFlowNode boolean:
                        boolean.SetInputId(value, owner);
                        break;
                    case BtsmtlSkillScalarInputFlowNode scalar:
                        scalar.SetInputId(value, owner);
                        break;
                    case BtsmtlSkillVector2InputFlowNode vector:
                        vector.SetInputId(value, owner);
                        break;
                    case BtsmtlSkillInputMagnitudeFlowNode magnitude:
                        magnitude.SetInputId(value, owner);
                        break;
                    case BtsmtlSkillActionRequestFlowNode request:
                        request.SetInputId(value, owner);
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

        internal static void DrawValueInputs(FlowGraph graph, FlowNode node)
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

    [Name("读取Gameplay Tag"), Category("BTSMTL/Ability")]
    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.GameplayEffectProfile)]
    [BtsmtlSkillNodeKind("gameplay-tag-has")]
    [BtsmtlSkillAuthoringField("tagId", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String, NonEmpty = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillGameplayTagFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IGameplayTagAuthoring
    {
        [SerializeField] GameplayTagId m_Tag;
        [SerializeField] string m_ProviderOwnerId;

        public GameplayTagId Tag => m_Tag;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(GameplayTagId tag, string providerOwnerId)
        {
            if (!tag.IsValid || string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("Gameplay Tag provider reference is incomplete.");
            m_Tag = tag;
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts() =>
            AddValueOutput<bool>("Has Tag", RejectAuthoringValue<bool>, "m_Result");
    }

    [Name("查询Gameplay Tags"), Category("BTSMTL/Ability")]
    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.GameplayEffectProfile)]
    [BtsmtlSkillNodeKind("gameplay-tag-query")]
    [BtsmtlSkillAuthoringField("query", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Object,
        Optional = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillGameplayTagQueryFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IGameplayTagQueryAuthoring
    {
        [SerializeField] GameplayTagQuery m_Query = new GameplayTagQuery();
        [SerializeField] string m_ProviderOwnerId;

        public GameplayTagQuery Query => m_Query;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(GameplayTagQuery query, string providerOwnerId)
        {
            if (query == null || string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("Gameplay Tag Query provider reference is incomplete.");
            m_Query = query;
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts() =>
            AddValueOutput<bool>("Matches", RejectAuthoringValue<bool>, "m_Result");
    }

    [Name("读取Ability Attribute"), Category("BTSMTL/Ability")]
    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.GameplayEffectProfile)]
    [BtsmtlSkillNodeKind("gameplay-attribute-read")]
    [BtsmtlSkillAuthoringField("attributeId", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String, NonEmpty = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillGameplayAttributeFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IGameplayAttributeAuthoring
    {
        [SerializeField] GameplayAttributeId m_Attribute;
        [SerializeField] string m_ProviderOwnerId;

        public GameplayAttributeId Attribute => m_Attribute;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(GameplayAttributeId attribute, string providerOwnerId)
        {
            if (!attribute.IsValid || string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("Gameplay Attribute provider reference is incomplete.");
            m_Attribute = attribute;
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts()
        {
            AddValueOutput<bool>("Valid", RejectAuthoringValue<bool>, "m_Valid");
            AddValueOutput<float>("Base Value", RejectAuthoringValue<float>, "m_BaseValue");
            AddValueOutput<float>("Current Value", RejectAuthoringValue<float>, "m_CurrentValue");
        }
    }

    [Name("应用Gameplay Effect"), Category("BTSMTL/Ability")]
    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.GameplayEffectProfile)]
    [BtsmtlSkillNodeKind("gameplay-effect-apply")]
    [BtsmtlSkillNodeAuthoringReference(
        "effect",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_gameplay_effect_unresolved",
        "Skill Gameplay Effect引用无法解析。",
        typeof(GameplayEffectDefinition))]
    [BtsmtlSkillNodeAuthoringReference(
        "actionContext",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_action_context_unresolved",
        "Skill Action Context引用无法解析。",
        typeof(ActionContextSlot),
        Optional = true)]
    [BtsmtlSkillAuthoringField("effect", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference)]
    [BtsmtlSkillAuthoringField("actionContext", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField("predicted", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Boolean,
        Optional = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillApplyGameplayEffectFlowNode : BtsmtlSkillFlowNode, IGameplayEffectApplicationAuthoring
    {
        [SerializeField] GameplayEffectDefinition m_Effect;
        [SerializeField] ActionContextSlot m_ActionContext;
        [SerializeField] bool m_Predicted;
        [SerializeField] string m_ProviderOwnerId;

        public GameplayEffectDefinition Effect => m_Effect;
        public ActionContextSlot ActionContext => m_ActionContext;
        public bool Predicted => m_Predicted;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(GameplayEffectDefinition effect, ActionContextSlot actionContext, bool predicted, string providerOwnerId)
        {
            if (!effect || string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("Gameplay Effect provider reference is incomplete.");
            m_Effect = effect;
            m_ActionContext = actionContext;
            m_Predicted = predicted;
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddValueOutput<bool>("Applied", RejectAuthoringValue<bool>, "m_Applied");
        }
    }

    [Name("移除Gameplay Effect"), Category("BTSMTL/Ability")]
    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.GameplayEffectProfile)]
    [BtsmtlSkillNodeKind("gameplay-effect-remove")]
    [BtsmtlSkillNodeAuthoringReference(
        "effect",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_gameplay_effect_unresolved",
        "Skill Gameplay Effect引用无法解析。",
        typeof(GameplayEffectDefinition),
        Optional = true)]
    [BtsmtlSkillAuthoringField("selector", typeof(GameplayEffectRemoveSelector))]
    [BtsmtlSkillAuthoringField(
        "handle",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Integer,
        HasMinimum = true,
        Minimum = 0d,
        Finite = true,
        Optional = true)]
    [BtsmtlSkillAuthoringField("effect", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField("query", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Object,
        Optional = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillRemoveGameplayEffectFlowNode : BtsmtlSkillFlowNode, IGameplayEffectRemovalAuthoring
    {
        [SerializeField] GameplayEffectRemoveSelector m_Selector = GameplayEffectRemoveSelector.EffectId;
        [SerializeField] ulong m_Handle;
        [SerializeField] GameplayEffectDefinition m_Effect;
        [SerializeField] GameplayTagQuery m_EffectTagQuery = new GameplayTagQuery();
        [SerializeField] string m_ProviderOwnerId;

        public GameplayEffectRemoveSelector Selector => m_Selector;
        public ulong Handle => m_Handle;
        public GameplayEffectDefinition Effect => m_Effect;
        public GameplayTagQuery EffectTagQuery => m_EffectTagQuery;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(
            GameplayEffectRemoveSelector selector,
            ulong handle,
            GameplayEffectDefinition effect,
            GameplayTagQuery effectTagQuery,
            string providerOwnerId)
        {
            if (!Enum.IsDefined(typeof(GameplayEffectRemoveSelector), selector) ||
                string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("Gameplay Effect removal provider reference is incomplete.");
            if (selector == GameplayEffectRemoveSelector.EffectId && !effect)
                throw new ArgumentException("EffectId removal requires an effect definition.");
            if (selector == GameplayEffectRemoveSelector.EffectTagQuery && effectTagQuery == null)
                throw new ArgumentException("EffectTagQuery removal requires a tag query.");
            m_Selector = selector;
            m_Handle = handle;
            m_Effect = effect;
            m_EffectTagQuery = effectTagQuery ?? new GameplayTagQuery();
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddValueOutput<bool>("Removed", RejectAuthoringValue<bool>, "m_Removed");
        }
    }
}
#endif
