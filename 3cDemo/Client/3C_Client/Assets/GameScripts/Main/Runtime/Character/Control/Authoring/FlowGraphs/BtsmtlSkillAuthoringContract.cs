#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonCamera;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using BTSMTL.Authoring.Graph;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public readonly struct BtsmtlSkillAuthoringFieldValue
    {
        public BtsmtlSkillAuthoringFieldValue(string fieldId, object value)
        {
            FieldId = string.IsNullOrWhiteSpace(fieldId)
                ? throw new ArgumentException("技能字段身份缺失。", nameof(fieldId))
                : fieldId;
            Value = value;
        }

        public string FieldId { get; }
        public object Value { get; }
    }

    public readonly struct BtsmtlSkillConnectionAuthoringValue
    {
        public BtsmtlSkillConnectionAuthoringValue(
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy,
            int order,
            bool hasConfiguration)
        {
            Condition = condition;
            Priority = priority;
            AbortPolicy = abortPolicy;
            Order = order;
            HasConfiguration = hasConfiguration;
        }

        public BtsmtlSkillFlowGraph Condition { get; }
        public int Priority { get; }
        public ProgramAbortPolicy AbortPolicy { get; }
        public int Order { get; }
        public bool HasConfiguration { get; }
        public bool HasNonDefaultConfiguration =>
            HasConfiguration &&
            (Condition != null || Priority != 0 || AbortPolicy != ProgramAbortPolicy.None || Order != 0);
    }

    public static class BtsmtlSkillAuthoringContract
    {
        public static BtsmtlSkillConnectionAuthoringValue ReadConnection(object connection)
        {
            if (connection is BtsmtlSkillFlowConnection flow)
                return new BtsmtlSkillConnectionAuthoringValue(
                    flow.Condition,
                    flow.Priority,
                    flow.AbortPolicy,
                    flow.Order,
                    true);
            if (connection is BtsmtlSkillNativeConnection native)
                return new BtsmtlSkillConnectionAuthoringValue(
                    native.Condition,
                    native.Priority,
                    native.AbortPolicy,
                    native.Order,
                    true);
            return default;
        }

        public static void ConfigureConnection(
            object connection,
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy,
            int order)
        {
            if (connection is BtsmtlSkillFlowConnection flow)
            {
                flow.Configure(condition, priority, abortPolicy, order);
                return;
            }
            if (connection is BtsmtlSkillNativeConnection native)
            {
                native.Configure(condition, priority, abortPolicy, order);
                return;
            }
            throw new ArgumentException("技能Connection类型没有正式配置合同。", nameof(connection));
        }

        public static IReadOnlyList<GraphAuthoringFieldValue> ReadFields(FlowNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (!BtsmtlSkillGraphAuthoringMetadata.TryGetKind(node.GetType(), out string kind))
                throw new InvalidOperationException($"技能节点类型未登记：{node.GetType().FullName}");

            var values = BtsmtlSkillAuthoringValues.ReadFields(node)
                .ToDictionary(value => value.Field.FieldId.Value, StringComparer.Ordinal);
            foreach (BtsmtlSkillAuthoringGraphReferenceValue reference in
                     BtsmtlSkillAuthoringValues.ReadGraphReferences(node))
            {
                if (!values.ContainsKey(reference.Definition.FieldId))
                    continue;
                GraphAuthoringFieldDescriptor field =
                    BtsmtlSkillGraphAuthoringMetadata.RequireField(
                        kind,
                        reference.Definition.FieldId);
                values[reference.Definition.FieldId] = new GraphAuthoringFieldValue(field, reference.Target);
            }
            return BtsmtlSkillGraphAuthoringMetadata.Fields(kind)
                .Where(field => values.ContainsKey(field.FieldId.Value))
                .Select(field => values[field.FieldId.Value])
                .ToArray();
        }

        public static bool IsDefault(GraphAuthoringFieldValue value)
        {
            object current = Normalize(value.Value);
            object expected = Normalize(value.Field.DefaultValue);
            if (expected == null)
                return false;
            if (current == null)
                return false;
            if (current is Enum currentEnum && expected is Enum expectedEnum)
                return currentEnum.GetType() == expectedEnum.GetType() &&
                       currentEnum.Equals(expectedEnum);
            return Equals(current, expected);
        }

        public static BtsmtlSkillStepPort CreateStep(
            string id,
            string name,
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy)
        {
            var result = new BtsmtlSkillStepPort(id, name);
            result.Configure(name, condition, priority, abortPolicy);
            return result;
        }

        public static void Apply(
            FlowNode node,
            params BtsmtlSkillAuthoringFieldValue[] values) =>
            Apply(node, (IReadOnlyList<BtsmtlSkillAuthoringFieldValue>)values);

        public static void Apply(
            FlowNode node,
            IReadOnlyList<BtsmtlSkillAuthoringFieldValue> values)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (node.graph is not FlowGraph graph)
                throw new InvalidOperationException("技能节点没有正式图owner。");
            if (!BtsmtlSkillGraphAuthoringMetadata.TryGetKind(node.GetType(), out string kind))
                throw new InvalidOperationException($"技能节点类型未登记：{node.GetType().FullName}");

            var fields = BtsmtlSkillGraphAuthoringMetadata.Fields(kind)
                .ToDictionary(value => value.FieldId.Value, StringComparer.Ordinal);
            var resolved = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (BtsmtlSkillAuthoringFieldValue value in values ?? Array.Empty<BtsmtlSkillAuthoringFieldValue>())
            {
                if (!fields.TryGetValue(value.FieldId, out GraphAuthoringFieldDescriptor field))
                    throw new InvalidOperationException($"技能节点 '{node.GetType().Name}' 没有字段 '{value.FieldId}'。");
                if (!(new GraphAuthoringFieldValue(field, value.Value)).IsValid)
                    throw new ArgumentException($"技能节点字段 '{value.FieldId}' 的值不符合正式字段合同。");
                resolved[value.FieldId] = value.Value;
            }
            BtsmtlSkillFlowEditorMutation.Execute(
                graph,
                "配置技能节点",
                () => ApplyResolved(node, resolved));
        }

        static void ApplyResolved(FlowNode node, IReadOnlyDictionary<string, object> values)
        {
            if (node is BtsmtlSkillCompositeFlowNode composite && values.TryGetValue("steps", out object steps))
                composite.SetSteps((IEnumerable<BtsmtlSkillStepPort>)steps);

            if (node is BtsmtlSkillStateExitCauseFlowNode cause && values.TryGetValue("cause", out object causeValue))
                cause.SetCause((BtsmtlSkillStateExitCause)causeValue);
            if (node is BtsmtlSkillLoopFlowNode loop && values.TryGetValue("stopType", out object stopType))
                loop.SetStopType((BtsmtlSkillLoopStopType)stopType);
            if (node is BtsmtlSkillParallelFlowNode parallel && values.TryGetValue("mode", out object mode))
                parallel.SetMode((BtsmtlSkillParallelMode)mode);

            if (node is IBtsmtlSkillInputNode input && values.ContainsKey("inputId"))
                input.SetInputId(
                    (string)values["inputId"],
                    values.TryGetValue("providerOwnerId", out object inputOwner) ? (string)inputOwner : input.ProviderOwnerId);
            if (node is BtsmtlSkillMoveFacingAngleFlowNode moveFacing && values.ContainsKey("providerOwnerId"))
                moveFacing.Configure((string)values["providerOwnerId"]);
            if (node is IBtsmtlSkillCharacterStateNode characterState &&
                (values.ContainsKey("fieldId") || values.ContainsKey("providerOwnerId")))
                characterState.Configure(
                    values.TryGetValue("fieldId", out object fieldId) ? (string)fieldId : characterState.FieldId,
                    values.TryGetValue("providerOwnerId", out object ownerId) ? (string)ownerId : characterState.ProviderOwnerId);

            if (node is BtsmtlSkillGameplayTagFlowNode tag && values.ContainsKey("tagId"))
                tag.Configure(
                    new GameplayTagId((string)values["tagId"]),
                    values.TryGetValue("providerOwnerId", out object owner) ? (string)owner : tag.ProviderOwnerId);
            if (node is BtsmtlSkillGameplayTagQueryFlowNode tagQuery && values.ContainsKey("query"))
                tagQuery.Configure(
                    (GameplayTagQuery)values["query"],
                    values.TryGetValue("providerOwnerId", out object owner) ? (string)owner : tagQuery.ProviderOwnerId);
            if (node is BtsmtlSkillGameplayAttributeFlowNode attribute && values.ContainsKey("attributeId"))
                attribute.Configure(
                    new GameplayAttributeId((string)values["attributeId"]),
                    values.TryGetValue("providerOwnerId", out object owner) ? (string)owner : attribute.ProviderOwnerId);
            if (node is BtsmtlSkillApplyGameplayEffectFlowNode applyEffect && values.ContainsKey("effect"))
                applyEffect.Configure(
                    (GameplayEffectDefinition)values["effect"],
                    values.TryGetValue("actionContext", out object actionContext)
                        ? (ActionContextSlot)actionContext
                        : applyEffect.ActionContext,
                    values.TryGetValue("predicted", out object predicted) ? (bool)predicted : applyEffect.Predicted,
                    values.TryGetValue("providerOwnerId", out object owner) ? (string)owner : applyEffect.ProviderOwnerId);
            if (node is BtsmtlSkillRemoveGameplayEffectFlowNode removeEffect &&
                (values.ContainsKey("selector") || values.ContainsKey("effect") || values.ContainsKey("query")))
                removeEffect.Configure(
                    values.TryGetValue("selector", out object selector)
                        ? (GameplayEffectRemoveSelector)selector
                        : removeEffect.Selector,
                    values.TryGetValue("effect", out object effect) ? (GameplayEffectDefinition)effect : removeEffect.Effect,
                    values.TryGetValue("query", out object query) ? (GameplayTagQuery)query : removeEffect.EffectTagQuery,
                    values.TryGetValue("providerOwnerId", out object owner) ? (string)owner : removeEffect.ProviderOwnerId);

            if (node is BtsmtlSkillActionContextActiveFlowNode contextActive && values.ContainsKey("actionContext"))
                contextActive.SetActionContext((ActionContextSlot)values["actionContext"]);
            if (node is BtsmtlSkillActivationEntryFlowNode activationEntry && values.ContainsKey("activationEntryId"))
                activationEntry.SetActivationEntryId((string)values["activationEntryId"]);
            if (node is BtsmtlSkillActionWindowActiveFlowNode window && values.ContainsKey("windowType"))
                window.SetWindowType((string)values["windowType"]);
            if (node is BtsmtlSkillActionEventReceivedFlowNode actionEvent && values.ContainsKey("eventId"))
                actionEvent.SetEventId((string)values["eventId"]);
            if (node is RequestCameraStateNode cameraState)
                cameraState.Configure(
                    values.TryGetValue("mode", out object cameraMode) ? (CameraMode)cameraMode : cameraState.Mode,
                    values.TryGetValue("sequenceId", out object cameraSequence) ? (string)cameraSequence : cameraState.SequenceId,
                    values.TryGetValue("priority", out object cameraPriority) ? (int)cameraPriority : cameraState.Priority,
                    values.TryGetValue("weight", out object cameraWeight) ? (float)cameraWeight : cameraState.Weight,
                    values.TryGetValue("blendInSeconds", out object blendIn) ? (float)blendIn : cameraState.BlendInSeconds,
                    values.TryGetValue("blendOutSeconds", out object blendOut) ? (float)blendOut : cameraState.BlendOutSeconds,
                    values.TryGetValue("targetKey", out object targetKey) ? (string)targetKey : cameraState.TargetKey,
                    values.TryGetValue("actionContext", out object stateContext) ? (ActionContextSlot)stateContext : cameraState.ActionContext,
                    values.TryGetValue("interruptPolicy", out object interruptPolicy) ? (CameraInterruptPolicy)interruptPolicy : cameraState.InterruptPolicy);
            if (node is RequestCameraEffectNode cameraEffect)
                cameraEffect.Configure(
                    values.TryGetValue("requestId", out object effectRequest) ? (string)effectRequest : cameraEffect.RequestId,
                    values.TryGetValue("effectKind", out object effectKind) ? (CameraEffectKind)effectKind : cameraEffect.EffectKind,
                    values.TryGetValue("resourceId", out object effectResource) ? (string)effectResource : cameraEffect.ResourceId,
                    values.TryGetValue("weight", out object effectWeight) ? (float)effectWeight : cameraEffect.Weight,
                    values.TryGetValue("priority", out object effectPriority) ? (int)effectPriority : cameraEffect.Priority,
                    values.TryGetValue("actionContext", out object effectContext) ? (ActionContextSlot)effectContext : cameraEffect.ActionContext);
            if (node is BtsmtlSkillCanActivateActionFlowNode admission && values.ContainsKey("admissionProfile"))
            {
                BtsmtlSkillTargetSnapshotReference snapshot = values.TryGetValue("targetSnapshot", out object snapshotValue)
                    ? (BtsmtlSkillTargetSnapshotReference)snapshotValue
                    : new BtsmtlSkillTargetSnapshotReference(
                        admission.TargetSnapshotDeclarationId,
                        admission.TargetSnapshotOwnerId);
                admission.Configure(
                    (GameplayAbilityAdmissionProfile)values["admissionProfile"],
                    snapshot.DeclarationId,
                    snapshot.OwnerId);
            }

            if (node is IBtsmtlSkillBlackboardReadNode blackboardRead && values.ContainsKey("declarationId"))
                blackboardRead.SetVariable(new BtsmtlSkillBlackboardReference(
                    (string)values["declarationId"],
                    (string)values["ownerId"]));
            if (node is BtsmtlSkillBlackboardAccessFlowNode blackboardAccess &&
                (values.ContainsKey("declarationId") || values.ContainsKey("valueType") || values.ContainsKey("factContext")))
                blackboardAccess.Configure(
                    new BtsmtlSkillBlackboardReference(
                        values.TryGetValue("declarationId", out object declarationId)
                            ? (string)declarationId
                            : blackboardAccess.DeclarationId,
                        values.TryGetValue("ownerId", out object blackboardOwner)
                            ? (string)blackboardOwner
                            : blackboardAccess.OwnerId),
                    ResolveBlackboardValueType(
                        values.TryGetValue("valueType", out object valueType)
                            ? (string)valueType
                            : BtsmtlSkillGraphAuthoringMetadata.ValueType(blackboardAccess.ValueType)),
                    values.TryGetValue("factContext", out object factContext)
                        ? (UnityEngine.Object)factContext
                        : blackboardAccess.FactContext);

            if (node is BtsmtlSkillStateMachineFlowNode stateMachine && values.ContainsKey("graphId"))
                stateMachine.SetStateMachine((BtsmtlSkillNativeStateMachine)values["graphId"]);
            if (node is BtsmtlSkillStateFlowNode state && values.ContainsKey("bodyGraphId"))
                state.SetBody((BtsmtlSkillFlowGraph)values["bodyGraphId"]);
            if (node is BtsmtlSkillTimelineFlowNode timeline && values.ContainsKey("timelineId"))
                timeline.Configure(
                    (TimelineAsset)values["timelineId"],
                    values.TryGetValue("timelineOwnership", out object ownership)
                        ? (BtsmtlSkillTimelineOwnership)ownership
                        : timeline.Ownership,
                    values.TryGetValue("actionContext", out object actionContext)
                        ? (ActionContextSlot)actionContext
                        : timeline.ActionContext,
                    values.TryGetValue("playbackMode", out object playback)
                        ? (TimelinePlaybackMode)playback
                        : timeline.PlaybackMode);
            if (node is BtsmtlSkillLocomotionFlowNode locomotion && values.ContainsKey("moveSpeed"))
                locomotion.Configure(
                    (float)values["moveSpeed"],
                    (LocomotionInputMotionDisplacementMode)values["displacementMode"],
                    values.TryGetValue("actionMotionCurve", out object motionCurve)
                        ? (RootMotionCurveAsset)motionCurve
                        : locomotion.ActionMotionCurve,
                    (float)values["turnSpeedDegrees"],
                    (bool)values["cameraRelative"],
                    (LocomotionInputMotionExecutionMode)values["executionMode"],
                    (float)values["durationSeconds"]);

            if (node is MacroNodeWrapper macro && values.ContainsKey("graphId"))
                macro.macro = (BtsmtlSkillMacroGraph)values["graphId"];
        }

        static BtsmtlSkillBlackboardValueType ResolveBlackboardValueType(string value) =>
            value switch
            {
                "bool" => BtsmtlSkillBlackboardValueType.Boolean,
                "int" => BtsmtlSkillBlackboardValueType.Integer,
                "ulong" => BtsmtlSkillBlackboardValueType.UInt64,
                "float" => BtsmtlSkillBlackboardValueType.Number,
                "string" => BtsmtlSkillBlackboardValueType.Identity,
                "vector2" => BtsmtlSkillBlackboardValueType.Vector2,
                "vector3" => BtsmtlSkillBlackboardValueType.Vector3,
                _ => throw new ArgumentException($"不支持的技能Blackboard值类型：{value}")
            };

        static object Normalize(object value)
        {
            if (value is UnityEngine.Object asset && !asset)
                return null;
            return value;
        }
    }
}
#endif
