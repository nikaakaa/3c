#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public interface IBtsmtlSkillNodeAuthoringResolver
    {
        FlowGraph ResolveGraph(string identity);
        TimelineAsset ResolveTimeline(string identity);
        ActionContextSlot ResolveActionContext(JToken value);
        ActionProfile ResolveActionProfile(JToken value);
        GameplayEffectDefinition ResolveGameplayEffect(JToken value);
        BtsmtlSkillBlackboardReference ResolveBlackboardReference(JObject properties);
        string ResolveDeclarationIdentity(string identity);
        T ResolveAsset<T>(JToken value) where T : UnityEngine.Object;
        string RequiredProviderOwner(JObject properties);
    }

    public static class BtsmtlSkillNodeAuthoringBinding
    {
        public static void Apply(
            FlowNode node,
            JObject properties,
            IBtsmtlSkillNodeAuthoringResolver resolver)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            properties ??= new JObject();
            if (node is BtsmtlSkillLoopFlowNode loop && properties.Value<string>("stopType") != null)
                loop.SetStopType(Enum.Parse<BtsmtlSkillLoopStopType>(properties.Value<string>("stopType"), false));
            if (node is BtsmtlSkillParallelFlowNode parallel && properties.Value<string>("mode") != null)
                parallel.SetMode(Enum.Parse<BtsmtlSkillParallelMode>(properties.Value<string>("mode"), false));
            if (node is BtsmtlSkillStateExitCauseFlowNode cause && properties.Value<string>("cause") != null)
                cause.SetCause(Enum.Parse<BtsmtlSkillStateExitCause>(properties.Value<string>("cause"), false));
            if (node is IBtsmtlSkillInputNode input && properties.Value<string>("inputId") != null)
                SetInput(input, properties.Value<string>("inputId"), resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillGameplayTagFlowNode tag)
                tag.Configure(
                    new GameplayTagId(properties.Value<string>("tagId")),
                    resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillMoveFacingAngleFlowNode moveFacing)
                moveFacing.Configure(resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillCharacterStateVector3FlowNode stateVector3)
                stateVector3.Configure(properties.Value<string>("fieldId"), resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillCharacterStateScalarFlowNode stateScalar)
                stateScalar.Configure(properties.Value<string>("fieldId"), resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillCharacterStateYawFlowNode stateYaw)
                stateYaw.Configure(properties.Value<string>("fieldId"), resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillCharacterStateBooleanFlowNode stateBoolean)
                stateBoolean.Configure(properties.Value<string>("fieldId"), resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillGameplayTagQueryFlowNode tagQuery)
                tagQuery.Configure(
                    ParseGameplayTagQuery(properties["query"]),
                    resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillGameplayAttributeFlowNode attribute)
                attribute.Configure(
                    new GameplayAttributeId(properties.Value<string>("attributeId")),
                    resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillApplyGameplayEffectFlowNode applyEffect)
                applyEffect.Configure(
                    resolver.ResolveGameplayEffect(properties["effect"]),
                    resolver.ResolveActionContext(properties["actionContext"]),
                    properties.Value<bool>("predicted"),
                    resolver.RequiredProviderOwner(properties));
            if (node is BtsmtlSkillRemoveGameplayEffectFlowNode removeEffect)
            {
                GameplayEffectRemoveSelector selector =
                    Enum.Parse<GameplayEffectRemoveSelector>(properties.Value<string>("selector"), false);
                removeEffect.Configure(
                    selector,
                    properties.Value<ulong>("handle"),
                    selector == GameplayEffectRemoveSelector.EffectId
                        ? resolver.ResolveGameplayEffect(properties["effect"])
                        : null,
                    ParseGameplayTagQuery(properties["query"]),
                    resolver.RequiredProviderOwner(properties));
            }
            if (node is BtsmtlSkillActionContextActiveFlowNode contextActive)
                contextActive.SetActionContext(resolver.ResolveActionContext(properties["actionContext"]));
            if (node is BtsmtlSkillActionWindowActiveFlowNode window)
                window.SetWindowType(properties.Value<string>("windowType"));
            if (node is BtsmtlSkillCanActivateActionFlowNode admission)
            {
                ActionProfile profile = resolver.ResolveActionProfile(properties["actionProfile"]);
                JObject targetSnapshot = properties["targetSnapshot"] as JObject;
                admission.Configure(
                    profile,
                    resolver.ResolveDeclarationIdentity(targetSnapshot?.Value<string>("id")),
                    resolver.ResolveGraph(targetSnapshot?.Value<string>("ownerId")) is IBtsmtlSkillFlowGraph owner
                        ? owner.AuthoringId
                        : targetSnapshot?.Value<string>("ownerId"));
            }
            if (node is BtsmtlSkillSubmitActionLifecycleFlowNode lifecycle)
                lifecycle.Configure(
                    resolver.ResolveActionContext(properties["actionContext"]),
                    Enum.Parse<ActionLifecycleTransitionType>(properties.Value<string>("transitionType"), false),
                    properties.Value<string>("reason"));
            if (node is BtsmtlSkillBlackboardReadFlowNode<bool> booleanRead)
                booleanRead.SetVariable(resolver.ResolveBlackboardReference(properties));
            if (node is BtsmtlSkillBlackboardReadFlowNode<float> scalarRead)
                scalarRead.SetVariable(resolver.ResolveBlackboardReference(properties));
            if (node is BtsmtlSkillBlackboardAccessFlowNode blackboard)
                blackboard.Configure(
                    resolver.ResolveBlackboardReference(properties),
                    Enum.Parse<BtsmtlSkillBlackboardValueType>(ValueTypeEnum(properties.Value<string>("valueType")), false),
                    resolver.ResolveAsset<UnityEngine.Object>(properties["factContext"]));
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine)
                stateMachine.SetStateMachine(resolver.ResolveGraph(properties.Value<string>("graphId")) as BtsmtlSkillFlowGraph);
            if (node is BtsmtlSkillStateFlowNode state)
                state.SetBody(resolver.ResolveGraph(properties.Value<string>("bodyGraphId")) as BtsmtlSkillFlowGraph);
            if (node is BtsmtlSkillTimelineFlowNode timeline)
                timeline.Configure(
                    resolver.ResolveTimeline(properties.Value<string>("timelineId")),
                    Enum.Parse<BtsmtlSkillTimelineOwnership>(properties.Value<string>("timelineOwnership"), false),
                    resolver.ResolveActionContext(properties["actionContext"]),
                    Enum.Parse<TimelinePlaybackMode>(properties.Value<string>("playbackMode"), false));
            if (node is BtsmtlSkillLocomotionFlowNode locomotion)
                locomotion.Configure(
                    properties.Value<float>("moveSpeed"),
                    Enum.Parse<LocomotionInputMotionDisplacementMode>(properties.Value<string>("displacementMode"), false),
                    resolver.ResolveAsset<RootMotionCurveAsset>(properties["actionMotionCurve"]),
                    properties.Value<float>("turnSpeedDegrees"),
                    properties.Value<bool>("cameraRelative"),
                    Enum.Parse<LocomotionInputMotionExecutionMode>(properties.Value<string>("executionMode"), false),
                    properties.Value<float>("durationSeconds"));
            if (node is MacroNodeWrapper macro)
                macro.macro = resolver.ResolveGraph(properties.Value<string>("graphId")) as BtsmtlSkillMacroGraph;
        }

        public static void ApplyValues(FlowNode node, JObject values)
        {
            foreach (ValueInput input in node.GetInputValuePorts())
            {
                if (input.isConnected)
                    continue;
                if (values?.TryGetValue(input.ID, out JToken value) == true)
                    input.serializedValue = ParseValue(value, input.type);
                else
                    input.serializedValue = input.defaultValue;
            }
        }

        public static JObject Export(
            FlowNode node,
            Func<UnityEngine.Object, JObject> logicalReference,
            Func<UnityEngine.Object, JToken> objectReference,
            Func<FlowGraph, string> graphIdentity,
            Func<TimelineAsset, string> timelineIdentity)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (logicalReference == null)
                throw new ArgumentNullException(nameof(logicalReference));
            if (objectReference == null)
                throw new ArgumentNullException(nameof(objectReference));
            if (graphIdentity == null)
                throw new ArgumentNullException(nameof(graphIdentity));
            if (timelineIdentity == null)
                throw new ArgumentNullException(nameof(timelineIdentity));
            var value = new JObject();
            if (node is BtsmtlSkillCompositeFlowNode composite)
                value["steps"] = new JArray(composite.Steps.Select(step => new JObject
                {
                    ["id"] = step.Id,
                    ["name"] = step.Name,
                    ["conditionGraphId"] = step.Condition ? graphIdentity(step.Condition) : null,
                    ["priority"] = step.Priority,
                    ["abortPolicy"] = step.AbortPolicy.ToString()
                }));
            if (node is BtsmtlSkillLoopFlowNode loop)
                value["stopType"] = loop.StopType.ToString();
            if (node is BtsmtlSkillParallelFlowNode parallel)
                value["mode"] = parallel.Mode.ToString();
            if (node is BtsmtlSkillStateExitCauseFlowNode cause)
                value["cause"] = cause.Cause.ToString();
            if (node is IBtsmtlSkillInputNode input)
            {
                value["inputId"] = input.InputId;
                value["providerOwnerId"] = input.ProviderOwnerId;
            }
            if (node is BtsmtlSkillGameplayTagFlowNode tag)
            {
                value["tagId"] = tag.Tag.Value;
                value["providerOwnerId"] = tag.ProviderOwnerId;
            }
            if (node is BtsmtlSkillMoveFacingAngleFlowNode moveFacing)
                value["providerOwnerId"] = moveFacing.ProviderOwnerId;
            if (node is IBtsmtlSkillCharacterStateNode characterState)
            {
                value["fieldId"] = characterState.FieldId;
                value["providerOwnerId"] = characterState.ProviderOwnerId;
            }
            if (node is BtsmtlSkillGameplayTagQueryFlowNode tagQuery)
            {
                value["providerOwnerId"] = tagQuery.ProviderOwnerId;
                value["query"] = GameplayTagQueryToken(tagQuery.Query);
            }
            if (node is BtsmtlSkillGameplayAttributeFlowNode attribute)
            {
                value["attributeId"] = attribute.Attribute.Value;
                value["providerOwnerId"] = attribute.ProviderOwnerId;
            }
            if (node is BtsmtlSkillApplyGameplayEffectFlowNode applyEffect)
            {
                value["effect"] = logicalReference(applyEffect.Effect);
                value["actionContext"] = logicalReference(applyEffect.ActionContext);
                value["predicted"] = applyEffect.Predicted;
                value["providerOwnerId"] = applyEffect.ProviderOwnerId;
            }
            if (node is BtsmtlSkillRemoveGameplayEffectFlowNode removeEffect)
            {
                value["selector"] = removeEffect.Selector.ToString();
                value["handle"] = removeEffect.Handle;
                value["effect"] = logicalReference(removeEffect.Effect);
                value["query"] = GameplayTagQueryToken(removeEffect.EffectTagQuery);
                value["providerOwnerId"] = removeEffect.ProviderOwnerId;
            }
            if (node is BtsmtlSkillActionContextActiveFlowNode contextActive)
                value["actionContext"] = logicalReference(contextActive.ActionContext);
            if (node is BtsmtlSkillActionWindowActiveFlowNode window)
                value["windowType"] = window.WindowType;
            if (node is BtsmtlSkillCanActivateActionFlowNode admission)
            {
                value["actionProfile"] = logicalReference(admission.ActionProfile);
                value["targetSnapshot"] = new JObject
                {
                    ["id"] = admission.TargetSnapshotDeclarationId,
                    ["ownerId"] = admission.TargetSnapshotOwnerId
                };
            }
            if (node is BtsmtlSkillSubmitActionLifecycleFlowNode lifecycle)
            {
                value["actionContext"] = logicalReference(lifecycle.ActionContext);
                value["transitionType"] = lifecycle.TransitionType.ToString();
                value["reason"] = lifecycle.Reason;
            }
            if (node is IBtsmtlSkillBlackboardAccessNode blackboard)
            {
                value["declarationId"] = blackboard.Variable.DeclarationId;
                value["ownerId"] = blackboard.Variable.OwnerId;
                value["valueType"] = BtsmtlSkillGraphAuthoringMetadata.ValueType(blackboard.ValueType);
                if (blackboard is BtsmtlSkillBlackboardAccessFlowNode access && access.FactContext)
                    value["factContext"] = objectReference(access.FactContext);
                if (blackboard is BtsmtlSkillBlackboardAccessFlowNode)
                    value["accessMode"] = blackboard.Writes ? "set" : "get";
            }
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine && stateMachine.StateMachine)
                value["graphId"] = graphIdentity(stateMachine.StateMachine);
            if (node is BtsmtlSkillStateFlowNode state && state.Body)
                value["bodyGraphId"] = graphIdentity(state.Body);
            if (node is BtsmtlSkillTimelineFlowNode timeline)
            {
                value["timelineId"] = timelineIdentity(timeline.TimelineAsset);
                value["timelineOwnership"] = timeline.Ownership.ToString();
                value["actionContext"] = logicalReference(timeline.ActionContext);
                value["playbackMode"] = timeline.PlaybackMode.ToString();
            }
            if (node is BtsmtlSkillLocomotionFlowNode locomotion)
            {
                value["moveSpeed"] = locomotion.MoveSpeed;
                value["displacementMode"] = locomotion.DisplacementMode.ToString();
                value["turnSpeedDegrees"] = locomotion.TurnSpeedDegrees;
                value["cameraRelative"] = locomotion.CameraRelative;
                value["executionMode"] = locomotion.ExecutionMode.ToString();
                value["durationSeconds"] = locomotion.DurationSeconds;
                if (locomotion.ActionMotionCurve)
                    value["actionMotionCurve"] = objectReference(locomotion.ActionMotionCurve);
            }
            if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                value["graphId"] = graphIdentity(macroGraph);
            return value;
        }

        static JObject GameplayTagQueryToken(GameplayTagQuery query) => new JObject
        {
            ["all"] = new JArray((query?.All ?? Array.Empty<GameplayTagId>()).Select(value => value.Value)),
            ["any"] = new JArray((query?.Any ?? Array.Empty<GameplayTagId>()).Select(value => value.Value)),
            ["none"] = new JArray((query?.None ?? Array.Empty<GameplayTagId>()).Select(value => value.Value))
        };

        static void SetInput(IBtsmtlSkillInputNode node, string value, string providerOwnerId)
        {
            if (string.IsNullOrWhiteSpace(providerOwnerId))
                throw new InvalidOperationException("Skill input Document缺少providerOwnerId。");
            node.SetInputId(value, providerOwnerId);
        }

        static string ValueTypeEnum(string value)
        {
            return value switch
            {
                "bool" => nameof(BtsmtlSkillBlackboardValueType.Boolean),
                "int" => nameof(BtsmtlSkillBlackboardValueType.Integer),
                "float" => nameof(BtsmtlSkillBlackboardValueType.Number),
                "string" => nameof(BtsmtlSkillBlackboardValueType.Identity),
                "vector2" => nameof(BtsmtlSkillBlackboardValueType.Vector2),
                "vector3" => nameof(BtsmtlSkillBlackboardValueType.Vector3),
                _ => value
            };
        }

        static GameplayTagQuery ParseGameplayTagQuery(JToken token)
        {
            JObject value = token as JObject ?? new JObject();
            return new GameplayTagQuery(
                Tags(value["all"]),
                Tags(value["any"]),
                Tags(value["none"]));

            static IReadOnlyList<GameplayTagId> Tags(JToken source) =>
                (source as JArray)?.Values<string>()
                    .Select(item => new GameplayTagId(item))
                    .ToArray() ?? Array.Empty<GameplayTagId>();
        }

        public static object ParseValue(JToken token, Type type)
        {
            if (token == null || token.Type == JTokenType.Null)
                return type == typeof(string) ? string.Empty : type.IsValueType ? Activator.CreateInstance(type) : null;
            if (type == typeof(Vector2))
                return new Vector2(token.Value<float>("x"), token.Value<float>("y"));
            if (type == typeof(Vector3))
                return new Vector3(token.Value<float>("x"), token.Value<float>("y"), token.Value<float>("z"));
            if (type == typeof(ActionTargetSnapshot))
            {
                string targetId = token.Value<string>("targetId") ?? string.Empty;
                Vector3 position = new Vector3(token.Value<float>("x"), token.Value<float>("y"), token.Value<float>("z"));
                Quaternion rotation = new Quaternion(
                    token.Value<float>("rx"),
                    token.Value<float>("ry"),
                    token.Value<float>("rz"),
                    token["rw"] == null ? 1f : token["rw"].Value<float>());
                return new ActionTargetSnapshot(targetId, position, rotation);
            }
            return token.ToObject(type);
        }
    }
}
#endif
