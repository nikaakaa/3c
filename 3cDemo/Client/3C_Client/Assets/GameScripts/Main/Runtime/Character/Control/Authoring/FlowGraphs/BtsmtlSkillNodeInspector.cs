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
            if (node is BtsmtlSkillStateFlowNode state)
                DrawState(graph, state);
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
            string currentOwner = Read<string>(node, "providerOwnerId");
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", currentOwner);
            if (!string.Equals(owner, currentOwner, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(owner))
                Change(graph, "修改Character State引用", () => node.Configure(owner));
        }

        static void DrawGameplayTag(FlowGraph graph, BtsmtlSkillGameplayTagFlowNode node)
        {
            string currentId = Read<string>(node, "tagId");
            string currentOwner = Read<string>(node, "providerOwnerId");
            string id = EditorGUILayout.DelayedTextField("Tag ID", currentId);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", currentOwner);
            if (!string.Equals(id, currentId, StringComparison.Ordinal) ||
                !string.Equals(owner, currentOwner, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Tag引用", () => node.Configure(new GameplayTagId(id), owner));
        }

        static void DrawGameplayTagQuery(FlowGraph graph, BtsmtlSkillGameplayTagQueryFlowNode node)
        {
            GameplayTagQuery currentQuery = Read<GameplayTagQuery>(node, "query");
            string currentAll = JoinTags(currentQuery?.All);
            string currentAny = JoinTags(currentQuery?.Any);
            string currentNone = JoinTags(currentQuery?.None);
            string currentOwner = Read<string>(node, "providerOwnerId");
            string all = EditorGUILayout.DelayedTextField("All Tags", currentAll);
            string any = EditorGUILayout.DelayedTextField("Any Tags", currentAny);
            string none = EditorGUILayout.DelayedTextField("None Tags", currentNone);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", currentOwner);
            if (!string.Equals(all, currentAll, StringComparison.Ordinal) ||
                !string.Equals(any, currentAny, StringComparison.Ordinal) ||
                !string.Equals(none, currentNone, StringComparison.Ordinal) ||
                !string.Equals(owner, currentOwner, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Tag Query", () => node.Configure(
                    new GameplayTagQuery(ParseTags(all), ParseTags(any), ParseTags(none)), owner));
        }

        static void DrawGameplayAttribute(FlowGraph graph, BtsmtlSkillGameplayAttributeFlowNode node)
        {
            string currentId = Read<string>(node, "attributeId");
            string currentOwner = Read<string>(node, "providerOwnerId");
            string id = EditorGUILayout.DelayedTextField("Attribute ID", currentId);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", currentOwner);
            if (!string.Equals(id, currentId, StringComparison.Ordinal) ||
                !string.Equals(owner, currentOwner, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Attribute引用", () => node.Configure(new GameplayAttributeId(id), owner));
        }

        static void DrawApplyGameplayEffect(FlowGraph graph, BtsmtlSkillApplyGameplayEffectFlowNode node)
        {
            GameplayEffectDefinition currentEffect = Read<GameplayEffectDefinition>(node, "effect");
            ActionContextSlot currentContext = Read<ActionContextSlot>(node, "actionContext");
            bool currentPredicted = Read<bool>(node, "predicted");
            string currentOwner = Read<string>(node, "providerOwnerId");
            GameplayEffectDefinition effect = ObjectField("Gameplay Effect", currentEffect, typeof(GameplayEffectDefinition));
            ActionContextSlot context = ObjectField("Action Context", currentContext, typeof(ActionContextSlot));
            bool predicted = EditorGUILayout.Toggle("Predicted", currentPredicted);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", currentOwner);
            if (effect != currentEffect || context != currentContext || predicted != currentPredicted ||
                !string.Equals(owner, currentOwner, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Effect应用", () => node.Configure(effect, context, predicted, owner));
        }

        static void DrawRemoveGameplayEffect(FlowGraph graph, BtsmtlSkillRemoveGameplayEffectFlowNode node)
        {
            GameplayEffectRemoveSelector currentSelector = Read<GameplayEffectRemoveSelector>(node, "selector");
            GameplayEffectDefinition currentEffect = Read<GameplayEffectDefinition>(node, "effect");
            string currentOwner = Read<string>(node, "providerOwnerId");
            GameplayEffectRemoveSelector selector = (GameplayEffectRemoveSelector)EditorGUILayout.EnumPopup("Selector", currentSelector);
            GameplayEffectDefinition effect = ObjectField("Gameplay Effect", currentEffect, typeof(GameplayEffectDefinition));
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", currentOwner);
            if (selector != currentSelector || effect != currentEffect ||
                !string.Equals(owner, currentOwner, StringComparison.Ordinal))
                Change(graph, "修改Gameplay Effect移除", () => node.Configure(selector, effect, Read<GameplayTagQuery>(node, "query"), owner));
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
            BtsmtlSkillLoopStopType current = Read<BtsmtlSkillLoopStopType>(node, "stopType");
            var value = (BtsmtlSkillLoopStopType)EditorGUILayout.EnumPopup("停止方式", current);
            if (value != current)
                Change(graph, "修改技能循环", () => node.SetStopType(value));
        }

        static void DrawCause(FlowGraph graph, BtsmtlSkillStateExitCauseFlowNode node)
        {
            BtsmtlSkillStateExitCause current = Read<BtsmtlSkillStateExitCause>(node, "cause");
            var value = (BtsmtlSkillStateExitCause)EditorGUILayout.EnumPopup("退出原因", current);
            if (value != current)
                Change(graph, "修改状态退出原因", () => node.SetCause(value));
        }

        static void DrawContextActive(FlowGraph graph, BtsmtlSkillActionContextActiveFlowNode node)
        {
            ActionContextSlot current = Read<ActionContextSlot>(node, "actionContext");
            ActionContextSlot value = ObjectField("动作上下文", current, typeof(ActionContextSlot));
            if (value != current)
                Change(graph, "修改动作上下文", () => node.SetActionContext(value));
        }

        static void DrawWindow(FlowGraph graph, BtsmtlSkillActionWindowActiveFlowNode node)
        {
            string current = Read<string>(node, "windowType");
            string value = EditorGUILayout.DelayedTextField("窗口类型", current);
            if (!string.Equals(value, current, StringComparison.Ordinal))
                Change(graph, "修改动作窗口", () => node.SetWindowType(value));
        }

        static void DrawAdmission(FlowGraph graph, BtsmtlSkillCanActivateActionFlowNode node)
        {
            GameplayAbilityAdmissionProfile currentProfile = Read<GameplayAbilityAdmissionProfile>(node, "admissionProfile");
            BtsmtlSkillTargetSnapshotReference currentSnapshot = Read<BtsmtlSkillTargetSnapshotReference>(node, "targetSnapshot");
            GameplayAbilityAdmissionProfile profile = ObjectField("准入规则", currentProfile, typeof(GameplayAbilityAdmissionProfile));
            string declarationId = EditorGUILayout.DelayedTextField("目标声明", currentSnapshot.DeclarationId);
            string ownerId = EditorGUILayout.DelayedTextField("声明作用域", currentSnapshot.OwnerId);
            if (profile != currentProfile ||
                !string.Equals(declarationId, currentSnapshot.DeclarationId, StringComparison.Ordinal) ||
                !string.Equals(ownerId, currentSnapshot.OwnerId, StringComparison.Ordinal))
                Change(graph, "修改动作准入", () => node.Configure(profile, declarationId, ownerId));
        }

        static void DrawStateMachine(FlowGraph graph, BtsmtlSkillStateMachineFlowNode node)
        {
            BtsmtlSkillNativeStateMachine current = node.StateMachine;
            BtsmtlSkillNativeStateMachine value = (BtsmtlSkillNativeStateMachine)EditorGUILayout.ObjectField(
                "原生状态机",
                current,
                typeof(BtsmtlSkillNativeStateMachine),
                false);
            if (value != current)
                Change(graph, "修改技能状态机", () => node.SetStateMachine(value), true);
        }

        static void DrawState(FlowGraph graph, BtsmtlSkillStateFlowNode node)
        {
            BtsmtlSkillFlowGraph current = node.Body;
            BtsmtlSkillFlowGraph value = (BtsmtlSkillFlowGraph)EditorGUILayout.ObjectField(
                "状态内容",
                current,
                typeof(BtsmtlSkillFlowGraph),
                false);
            if (value != current)
                Change(graph, "修改状态内容", () => node.SetBody(value), true);
        }

        static void DrawInput(FlowGraph graph, IBtsmtlSkillInputNode node)
        {
            string currentInputId = Read<string>((FlowNode)node, "inputId");
            string currentOwner = Read<string>((FlowNode)node, "providerOwnerId");
            string value = EditorGUILayout.DelayedTextField("输入身份", currentInputId);
            string owner = EditorGUILayout.DelayedTextField("Provider Owner", currentOwner);
            if (string.Equals(value, currentInputId, StringComparison.Ordinal) &&
                string.Equals(owner, currentOwner, StringComparison.Ordinal))
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
            BtsmtlSkillBlackboardReference current = ReadBlackboardReference(node);
            BtsmtlSkillBlackboardReference value = Reference(current);
            if (value.DeclarationId == current.DeclarationId &&
                value.OwnerId == current.OwnerId)
                return;
            if (!value.IsValid)
                return;
            Change(graph, "修改技能黑板读取", () => node.SetVariable(value));
        }

        static void DrawBlackboardAccess(FlowGraph graph, BtsmtlSkillBlackboardAccessFlowNode node)
        {
            BtsmtlSkillBlackboardReference currentVariable = ReadBlackboardReference(node);
            BtsmtlSkillBlackboardReference variable = Reference(currentVariable);
            BtsmtlSkillBlackboardValueType currentType = Read<BtsmtlSkillBlackboardValueType>(node, "valueType");
            var valueType = (BtsmtlSkillBlackboardValueType)EditorGUILayout.EnumPopup("值类型", currentType);
            UnityEngine.Object currentFactContext = Read<UnityEngine.Object>(node, "factContext");
            UnityEngine.Object factContext = EditorGUILayout.ObjectField(
                "事实上下文",
                currentFactContext,
                typeof(UnityEngine.Object),
                false);
            if (variable.DeclarationId == currentVariable.DeclarationId &&
                variable.OwnerId == currentVariable.OwnerId &&
                valueType == currentType &&
                factContext == currentFactContext)
                return;
            if (!variable.IsValid)
                return;
            Change(graph, "修改技能黑板访问", () => node.Configure(variable, valueType, factContext));
        }

        static void DrawTimeline(FlowGraph graph, BtsmtlSkillTimelineFlowNode node)
        {
            TimelineAsset currentTimeline = node.TimelineAsset;
            TimelineAsset timeline = ObjectField("Timeline", currentTimeline, typeof(TimelineAsset));
            BtsmtlSkillTimelineOwnership currentOwnership = Read<BtsmtlSkillTimelineOwnership>(node, "timelineOwnership");
            ActionContextSlot currentContext = Read<ActionContextSlot>(node, "actionContext");
            TimelinePlaybackMode currentPlayback = Read<TimelinePlaybackMode>(node, "playbackMode");
            var ownership = (BtsmtlSkillTimelineOwnership)EditorGUILayout.EnumPopup("所有权", currentOwnership);
            ActionContextSlot context = ObjectField("动作上下文", currentContext, typeof(ActionContextSlot));
            var playback = (TimelinePlaybackMode)EditorGUILayout.EnumPopup("播放模式", currentPlayback);
            if (timeline != currentTimeline || ownership != currentOwnership ||
                context != currentContext || playback != currentPlayback)
                Change(graph, "修改技能Timeline", () => node.Configure(timeline, ownership, context, playback),
                    timeline != currentTimeline);
            if (currentTimeline && GUILayout.Button("打开Timeline编辑器"))
            {
                var observation = graph.editorObservation as IBtsmtlSkillObservationControls;
                observation?.NotifyTimelineOpening(node);
                try { AssetDatabase.OpenAsset(currentTimeline); }
                finally { observation?.NotifyTimelineOpening(null); }
            }
        }

        static void DrawLocomotion(FlowGraph graph, BtsmtlSkillLocomotionFlowNode node)
        {
            float currentMoveSpeed = Read<float>(node, "moveSpeed");
            LocomotionInputMotionDisplacementMode currentDisplacement = Read<LocomotionInputMotionDisplacementMode>(node, "displacementMode");
            RootMotionCurveAsset currentCurve = Read<RootMotionCurveAsset>(node, "actionMotionCurve");
            float currentTurnSpeed = Read<float>(node, "turnSpeedDegrees");
            bool currentCameraRelative = Read<bool>(node, "cameraRelative");
            LocomotionInputMotionExecutionMode currentExecution = Read<LocomotionInputMotionExecutionMode>(node, "executionMode");
            float currentDuration = Read<float>(node, "durationSeconds");
            float moveSpeed = EditorGUILayout.FloatField("移动速度", currentMoveSpeed);
            var displacement = (LocomotionInputMotionDisplacementMode)EditorGUILayout.EnumPopup("位移模式", currentDisplacement);
            RootMotionCurveAsset curve = ObjectField("动作曲线", currentCurve, typeof(RootMotionCurveAsset));
            float turnSpeed = EditorGUILayout.FloatField("转向速度", currentTurnSpeed);
            bool cameraRelative = EditorGUILayout.Toggle("相机相对", currentCameraRelative);
            var execution = (LocomotionInputMotionExecutionMode)EditorGUILayout.EnumPopup("执行模式", currentExecution);
            float duration = EditorGUILayout.FloatField("持续时间", currentDuration);
            if (!Mathf.Approximately(moveSpeed, currentMoveSpeed) ||
                displacement != currentDisplacement ||
                curve != currentCurve ||
                !Mathf.Approximately(turnSpeed, currentTurnSpeed) ||
                cameraRelative != currentCameraRelative ||
                execution != currentExecution ||
                !Mathf.Approximately(duration, currentDuration))
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

        static BtsmtlSkillBlackboardReference ReadBlackboardReference(FlowNode node)
        {
            string declarationId = Read<string>(node, "declarationId");
            string ownerId = Read<string>(node, "ownerId");
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

        static TValue Read<TValue>(FlowNode node, string fieldId) =>
            (TValue)BtsmtlSkillGraphAuthoringMetadata.ReadField(node, fieldId);

        static void Change(FlowGraph graph, string title, Action mutation, bool updateOwnedAssets = false)
        {
            try
            {
                BtsmtlSkillFlowEditorMutation.Apply(
                    graph, title, mutation, true, Array.Empty<UnityEngine.Object>(), true, updateOwnedAssets);
            }
            catch (InvalidOperationException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
            catch (ArgumentException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
        }
    }

    [Name("读取Gameplay Tag"), Category("BTSMTL/Ability")]
    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.GameplayEffectProfile)]
    [BtsmtlSkillNodeKind("gameplay-tag-has")]
    [BtsmtlSkillAuthoringField("tagId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String, NonEmpty = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillGameplayTagFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IGameplayTagAuthoring
    {
        [SerializeField] GameplayTagId m_Tag;
        [SerializeField] string m_ProviderOwnerId;

        public GameplayTagId Tag => m_Tag;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(GameplayTagId tag, string providerOwnerId)
        {
            GameplayAuthoringRules.RequireTag(tag);
            if (string.IsNullOrWhiteSpace(providerOwnerId))
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
    [BtsmtlSkillAuthoringField("query", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Object,
        Optional = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillGameplayTagQueryFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IGameplayTagQueryAuthoring
    {
        [SerializeField] GameplayTagQuery m_Query = new GameplayTagQuery();
        [SerializeField] string m_ProviderOwnerId;

        public GameplayTagQuery Query => m_Query;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(GameplayTagQuery query, string providerOwnerId)
        {
            GameplayAuthoringRules.RequireTagQuery(query);
            if (string.IsNullOrWhiteSpace(providerOwnerId))
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
    [BtsmtlSkillAuthoringField("attributeId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String, NonEmpty = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillGameplayAttributeFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IGameplayAttributeAuthoring
    {
        [SerializeField] GameplayAttributeId m_Attribute;
        [SerializeField] string m_ProviderOwnerId;

        public GameplayAttributeId Attribute => m_Attribute;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(GameplayAttributeId attribute, string providerOwnerId)
        {
            GameplayAuthoringRules.RequireAttribute(attribute);
            if (string.IsNullOrWhiteSpace(providerOwnerId))
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
    [BtsmtlSkillAuthoringField("effect", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.IdentityReference)]
    [BtsmtlSkillAuthoringField("actionContext", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField("predicted", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Boolean,
        Optional = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
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
            GameplayAuthoringRules.RequireEffect(effect);
            if (string.IsNullOrWhiteSpace(providerOwnerId))
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
            AddValueOutput<ulong>("效果句柄", RejectAuthoringValue<ulong>, "m_Handle");
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
    [BtsmtlSkillAuthoringField("effect", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField("query", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Object,
        Optional = true)]
    [BtsmtlSkillAuthoringField("providerOwnerId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillRemoveGameplayEffectFlowNode : BtsmtlSkillFlowNode, IGameplayEffectRemovalAuthoring
    {
        [SerializeField] GameplayEffectRemoveSelector m_Selector = GameplayEffectRemoveSelector.EffectId;
        [SerializeField] GameplayEffectDefinition m_Effect;
        [SerializeField] GameplayTagQuery m_EffectTagQuery = new GameplayTagQuery();
        [SerializeField] string m_ProviderOwnerId;

        public GameplayEffectRemoveSelector Selector => m_Selector;
        public GameplayEffectDefinition Effect => m_Effect;
        public GameplayTagQuery EffectTagQuery => m_EffectTagQuery;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(
            GameplayEffectRemoveSelector selector,
            GameplayEffectDefinition effect,
            GameplayTagQuery effectTagQuery,
            string providerOwnerId)
        {
            GameplayAuthoringRules.ValidateEffectRemoval(selector, effect, effectTagQuery);
            if (string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("Gameplay Effect removal provider reference is incomplete.");
            m_Selector = selector;
            m_Effect = effect;
            m_EffectTagQuery = effectTagQuery ?? new GameplayTagQuery();
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddValueInput<ulong>("效果句柄", "m_Handle").SetDefaultAndSerializedValue(0UL);
            AddValueOutput<bool>("Removed", RejectAuthoringValue<bool>, "m_Removed");
        }
    }
}
#endif
