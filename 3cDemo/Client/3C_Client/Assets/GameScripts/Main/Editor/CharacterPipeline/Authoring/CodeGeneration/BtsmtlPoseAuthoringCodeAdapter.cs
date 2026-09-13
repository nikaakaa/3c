#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class BtsmtlPoseAuthoringCodeAdapter : IBtsmtlAuthoringCodeDomainAdapter
    {
        public string DomainId => "pose";

        public bool CanHandle(object root) => root is CharacterPresentationPoseGraphAsset;

        public void Emit(BtsmtlAuthoringCodeExportContext context, object root)
        {
            CharacterPresentationPoseGraphAsset asset =
                root as CharacterPresentationPoseGraphAsset;
            if (!asset)
            {
                context.ReportError("pose_root_invalid", string.Empty, "Pose 导出根对象不是正式 Pose Graph 资产。");
                return;
            }
            CharacterPoseCanvasGraph rootGraph = asset.Graph;
            if (!rootGraph)
            {
                context.ReportError("pose_root_graph_missing", asset.name, "Pose Graph 资产缺少根图。");
                return;
            }

            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(context.DefinitionAssetPath);
            CharacterAnimationPresentationProfile profile = definition?.AnimationPresentationProfile;
            if (!definition || !profile)
            {
                context.ReportError(
                    "pose_definition_profile_missing",
                    context.DefinitionAssetPath,
                    "Pose 导出需要 Definition 上的 Animation Presentation Profile。");
                return;
            }
            if (profile.PoseGraph != asset)
            {
                context.ReportError(
                    "pose_root_not_bound",
                    asset.name,
                    "Pose Graph 不是 Definition 当前 Presentation Profile 的正式根引用。");
                return;
            }

            CharacterPoseCanvasGraph[] graphs = asset.EnumerateGraphs()
                .Where(value => value != null)
                .ToArray();
            if (graphs.Length == 0 || graphs[0] != rootGraph)
            {
                context.ReportError("pose_graph_catalog_invalid", asset.name, "Pose Graph 闭包缺少第一个根图。");
                return;
            }
            if (graphs.Select(value => value.GraphId).Distinct().Count() != graphs.Length)
            {
                context.ReportError("pose_graph_identity_duplicate", asset.name, "Pose Graph 闭包包含重复图身份。");
                return;
            }

            string rootVariable = context.RegisterObject(
                asset,
                $"pose-asset:{rootGraph.GraphId.Value}",
                "poseAsset",
                true);
            CharacterPresentationPoseSourceSlot[] sourceSlots = asset.SourceSlots
                .Where(value => value)
                .ToArray();
            CharacterPoseResourceSlot[] resourceSlots = asset.ResourceSlots
                .Where(value => value)
                .ToArray();
            for (int i = 0; i < sourceSlots.Length; i++)
            {
                CharacterPresentationPoseSourceSlot slot = sourceSlots[i];
                context.RegisterObject(slot, $"pose.source.{slot.name}", "sourceSlot");
                if (slot.GetType().IsAbstract)
                    context.ReportError("pose_source_slot_type_invalid", slot.name, "Pose Source Slot 类型不能是抽象类型。");
            }
            for (int i = 0; i < resourceSlots.Length; i++)
            {
                CharacterPoseResourceSlot slot = resourceSlots[i];
                context.RegisterObject(slot, $"pose.resource.{slot.name}", "resourceSlot");
            }
            for (int i = 0; i < graphs.Length; i++)
            {
                CharacterPoseCanvasGraph graph = graphs[i];
                graph.RequireValid();
                context.RegisterObject(graph, graph.GraphId.Value, "poseGraph");
            }

            int[] sourceBindingIndices = ResolveSourceBindingIndices(context, profile, sourceSlots);
            int[] resourceBindingIndices = ResolveResourceBindingIndices(context, profile, resourceSlots);
            context.AddUsing(typeof(CharacterPoseCanvasGraph).Namespace);
            context.AddUsing("ThirdPersonCharacter.Animation.TransitionRouting");
            context.AddUsing("ThirdPersonCharacter.Pipeline");
            context.AddUsing("ThirdPersonCharacter.Pipeline.Animation");
            context.AddUsing("ThirdPersonSimulation");
            context.AddUsing("UnityEngine");
            string definitionExpression =
                BtsmtlAuthoringCodeValues.ExternalAsset(
                    context,
                    definition,
                    typeof(CharacterPipelineDefinition));
            string profileExpression =
                BtsmtlAuthoringCodeValues.ExternalAsset(
                    context,
                    profile,
                    typeof(CharacterAnimationPresentationProfile));

            foreach (CharacterPresentationPoseSourceSlot slot in sourceSlots)
            {
                string variable = context.RequireVariable(slot, slot.name);
                string type = BtsmtlAuthoringCodeSyntax.TypeName(slot.GetType());
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {variable} = BtsmtlPoseAuthoringCode.CreateSourceSlot<{type}>({BtsmtlAuthoringCodeSyntax.StringLiteral(slot.name)});");
            }
            foreach (CharacterPoseResourceSlot slot in resourceSlots)
            {
                string variable = context.RequireVariable(slot, slot.name);
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {variable} = BtsmtlPoseAuthoringCode.CreateResourceSlot({EnumLiteral(typeof(CharacterPoseResourceKind), slot.Kind)}, {BtsmtlAuthoringCodeSyntax.StringLiteral(slot.name)});");
            }
            foreach (CharacterPoseCanvasGraph graph in graphs)
            {
                string variable = context.RequireVariable(graph, graph.GraphId.Value);
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {variable} = {GraphExpression(context, graph, sourceSlots, resourceSlots)};");
            }

            string[] graphVariables = graphs
                .Select(graph => context.RequireVariable(graph, graph.GraphId.Value))
                .ToArray();
            string[] sourceVariables = sourceSlots
                .Select(slot => context.RequireVariable(slot, slot.name))
                .ToArray();
            string[] resourceVariables = resourceSlots
                .Select(slot => context.RequireVariable(slot, slot.name))
                .ToArray();
            string[] layoutExpressions = asset.StateMachineLayouts
                .Select(LayoutExpression)
                .ToArray();
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                $"var {rootVariable} = BtsmtlPoseAuthoringCode.EnsureRoot(context, {definitionExpression}, {profileExpression}, " +
                $"{ArrayExpression(typeof(CharacterPoseCanvasGraph), graphVariables)}, " +
                $"{ArrayExpression(typeof(CharacterPresentationPoseSourceSlot), sourceVariables)}, " +
                $"{ArrayExpression(typeof(CharacterPoseResourceSlot), resourceVariables)}, " +
                $"{ArrayExpression(typeof(int), sourceBindingIndices.Select(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture)))}, " +
                $"{ArrayExpression(typeof(int), resourceBindingIndices.Select(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture)))}, " +
                $"{ArrayExpression(typeof(CharacterPoseStateMachineLayout), layoutExpressions)});");
        }

        static int[] ResolveSourceBindingIndices(
            BtsmtlAuthoringCodeExportContext context,
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterPresentationPoseSourceSlot> slots)
        {
            var bindings = profile.PoseSourceBindings.ToArray();
            var result = new int[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                int index = -1;
                for (int j = 0; j < bindings.Length; j++)
                {
                    if (bindings[j] != null && bindings[j].Slot == slots[i])
                    {
                        if (index >= 0)
                        {
                            context.ReportError(
                                "pose_source_binding_duplicate",
                                slots[i].name,
                                "Pose Source Slot 对应了多个 Profile binding。");
                            break;
                        }
                        index = j;
                    }
                }
                if (index < 0)
                    context.ReportError("pose_source_binding_missing", slots[i].name, "Pose Source Slot 缺少 Profile binding。");
                result[i] = index;
            }
            return result;
        }

        static int[] ResolveResourceBindingIndices(
            BtsmtlAuthoringCodeExportContext context,
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterPoseResourceSlot> slots)
        {
            var bindings = profile.PoseResourceBindings.ToArray();
            var result = new int[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                int index = -1;
                for (int j = 0; j < bindings.Length; j++)
                {
                    if (bindings[j]?.Slot == slots[i])
                    {
                        if (index >= 0)
                        {
                            context.ReportError(
                                "pose_resource_binding_duplicate",
                                slots[i].name,
                                "Pose Resource Slot 对应了多个 Profile binding。");
                            break;
                        }
                        index = j;
                    }
                }
                if (index < 0)
                    context.ReportError("pose_resource_binding_missing", slots[i].name, "Pose Resource Slot 缺少 Profile binding。");
                result[i] = index;
            }
            return result;
        }

        static string GraphExpression(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPoseCanvasGraph graph,
            IReadOnlyList<CharacterPresentationPoseSourceSlot> sourceSlots,
            IReadOnlyList<CharacterPoseResourceSlot> resourceSlots)
        {
            string parameters = ArrayExpression(
                typeof(CharacterPoseParameterDeclaration),
                graph.Parameters.Select(ParameterExpression));
            string nodes = ArrayExpression(
                typeof(CharacterPoseCanvasNode),
                graph.Nodes.Select(node => NodeExpression(context, node, sourceSlots, resourceSlots)));
            string connections = ArrayExpression(
                typeof(CharacterPoseCanvasConnection),
                graph.Connections.Select(ConnectionExpression));
            string layout = ArrayExpression(
                typeof(CharacterPoseGraphLayoutEntry),
                graph.Layout.Select(LayoutEntryExpression));
            return $"CharacterPoseCanvasGraph.CreateAuthoring(" +
                $"{Id(typeof(PoseGraphId), graph.GraphId.Value)}, " +
                $"{BtsmtlAuthoringCodeSyntax.StringLiteral(graph.ContentRevision)}, " +
                $"{parameters}, {nodes}, {connections}, {layout}, " +
                $"{EnumLiteral(typeof(CharacterPoseAuthoringGraphRole), graph.Role)} )";
        }

        static string NodeExpression(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPoseCanvasNode node,
            IReadOnlyList<CharacterPresentationPoseSourceSlot> sourceSlots,
            IReadOnlyList<CharacterPoseResourceSlot> resourceSlots)
        {
            string payload = PayloadExpression(context, node.Payload, sourceSlots, resourceSlots, node.NodeId.Value);
            string dynamicPorts = ArrayExpression(
                typeof(CharacterPoseDynamicPort),
                node.DynamicPorts.Select(DynamicPortExpression));
            return $"new {Type(typeof(CharacterPoseCanvasNode))}(" +
                $"{Id(typeof(PoseNodeId), node.NodeId.Value)}, " +
                $"{BtsmtlAuthoringCodeSyntax.StringLiteral(node.DisplayName)}, {payload}, {dynamicPorts}, " +
                $"{BtsmtlAuthoringCodeValues.Vector2(node.position)})";
        }

        static string PayloadExpression(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPoseNodePayload payload,
            IReadOnlyList<CharacterPresentationPoseSourceSlot> sourceSlots,
            IReadOnlyList<CharacterPoseResourceSlot> resourceSlots,
            string subject)
        {
            if (payload == null)
            {
                context.ReportError("pose_payload_missing", subject, "Pose 节点缺少正式 payload。");
                return "null";
            }
            if (payload is CharacterGraphInputPosePayload)
                return New(typeof(CharacterGraphInputPosePayload));
            if (payload is CharacterGraphOutputPosePayload)
                return New(typeof(CharacterGraphOutputPosePayload));
            if (payload is CharacterOutputPosePayload)
                return New(typeof(CharacterOutputPosePayload));
            if (payload is CharacterLocalToComponentPosePayload)
                return New(typeof(CharacterLocalToComponentPosePayload));
            if (payload is CharacterComponentToLocalPosePayload)
                return New(typeof(CharacterComponentToLocalPosePayload));
            if (payload is CharacterFullBodyIkGoalAssemblerPayload)
                return New(typeof(CharacterFullBodyIkGoalAssemblerPayload));
            if (payload is CharacterFullBodyIkPosePayload)
                return New(typeof(CharacterFullBodyIkPosePayload));
            if (payload is CharacterLinkedPoseCallPayload linked)
                return New(
                    typeof(CharacterLinkedPoseCallPayload),
                    Id(typeof(LinkedPoseGroupId), linked.GroupId.Value),
                    Id(typeof(LinkedPoseInterfaceId), linked.InterfaceId.Value),
                    Id(typeof(LinkedPoseEntryId), linked.EntryId.Value));
            if (payload is CharacterActionPlaybackInputPosePayload action)
                return New(
                    typeof(CharacterActionPlaybackInputPosePayload),
                    Id(typeof(AnimationChannelId), action.AnimationChannelId.Value));
            if (payload is CharacterProgramParameterInputPosePayload parameter)
                return New(
                    typeof(CharacterProgramParameterInputPosePayload),
                    Id(typeof(PoseParameterId), parameter.ParameterId.Value));
            if (payload is CharacterSelectedPosePlayerPayload selected)
                return New(
                    typeof(CharacterSelectedPosePlayerPayload),
                    RequireSourceVariable(context, selected.SourceSlot, sourceSlots, subject));
            if (payload is CharacterBlendSpacePlayerPosePayload blendSpace)
                return New(
                    typeof(CharacterBlendSpacePlayerPosePayload),
                    RequireSourceVariable(context, blendSpace.SourceSlot, sourceSlots, subject),
                    EnumLiteral(typeof(CharacterAnimationBlendSpaceInputRangePolicy), blendSpace.InputRangePolicy));
            if (payload is CharacterClipPlayerPosePayload clip)
                return New(
                    typeof(CharacterClipPlayerPosePayload),
                    RequireSourceVariable(context, clip.SourceSlot, sourceSlots, subject),
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(clip.PlayRate),
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(clip.InitialTime),
                    clip.LoopAnimation ? "true" : "false",
                    EnumLiteral(typeof(CharacterClipPlayerClockSource), clip.ClockSource));
            if (payload is CharacterPoseStateMachineNodePayload stateMachine)
                return New(
                    typeof(CharacterPoseStateMachineNodePayload),
                    StateMachineExpression(context, stateMachine.StateMachine, resourceSlots, subject));
            if (payload is CharacterAnimationSlotPosePayload animationSlot)
                return New(
                    typeof(CharacterAnimationSlotPosePayload),
                    Id(typeof(AnimationSlotId), animationSlot.SlotId.Value),
                    Id(typeof(AnimationChannelId), animationSlot.AnimationChannelId.Value),
                    EnumLiteral(typeof(AnimationSelectionAvailabilityPolicy), animationSlot.SelectionAvailability),
                    RequireResourceVariable(context, animationSlot.BlendPolicySlot, resourceSlots, subject));
            if (payload is CharacterBlendStackPosePayload blendStack)
                return New(
                    typeof(CharacterBlendStackPosePayload),
                    RequireSourceVariable(context, blendStack.SourceSlot, sourceSlots, subject),
                    RequireResourceVariable(context, blendStack.BlendPolicySlot, resourceSlots, subject));
            if (payload is CharacterInertializationPosePayload inertialization)
                return New(
                    typeof(CharacterInertializationPosePayload),
                    RequireResourceVariable(context, inertialization.PolicySlot, resourceSlots, subject));
            if (payload is CharacterBlendPosePayload blend)
                return New(
                    typeof(CharacterBlendPosePayload),
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(blend.Weight));
            if (payload is CharacterLayeredBoneBlendPosePayload layered)
                return New(
                    typeof(CharacterLayeredBoneBlendPosePayload),
                    RequireResourceVariable(context, layered.BoneMaskSlot, resourceSlots, subject),
                    EnumLiteral(typeof(CharacterLayeredBoneBlendSpace), layered.BlendSpace),
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(layered.Weight));
            if (payload is CharacterAdditivePosePayload additive)
                return New(
                    typeof(CharacterAdditivePosePayload),
                    BtsmtlAuthoringCodeSyntax.StringLiteral(additive.ReferencePoseId),
                    EnumLiteral(typeof(AdditiveReferenceSpace), additive.ReferenceSpace),
                    EnumLiteral(typeof(AdditiveScalePolicy), additive.ScalePolicy),
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(additive.Weight));
            if (payload is CharacterPoseParameterResolvePayload resolve)
                return New(
                    typeof(CharacterPoseParameterResolvePayload),
                    ArrayExpression(typeof(CharacterPoseParameterPolicy), resolve.Policies.Select(ParameterPolicyExpression)));
            if (payload is CharacterRootOrientationWarpPosePayload warp)
                return New(
                    typeof(CharacterRootOrientationWarpPosePayload),
                    RequireResourceVariable(context, warp.YawCurveSlot, resourceSlots, subject));
            if (payload is CharacterFootPlacementPosePayload foot)
                return New(
                    typeof(CharacterFootPlacementPosePayload),
                    RequireResourceVariable(context, foot.ProfileSlot, resourceSlots, subject),
                    RequireResourceVariable(context, foot.CalibrationSlot, resourceSlots, subject));
            if (payload is CharacterPoseSubgraphPayload subgraph)
            {
                if (subgraph.Subgraph == null || !subgraph.Subgraph.PoseGraphId.IsValid)
                {
                    context.ReportError("pose_subgraph_reference_invalid", subject, "Pose Subgraph 引用缺少正式图身份。");
                    return "null";
                }
                return New(
                    typeof(CharacterPoseSubgraphPayload),
                    $"BtsmtlPoseAuthoringCode.CreateSubgraphReference({BtsmtlAuthoringCodeSyntax.StringLiteral(subgraph.Subgraph.PoseGraphId.Value)})");
            }
            if (payload is CharacterModifyBonePosePayload || payload is CharacterPoseBoneIkGoalsPayload)
            {
                context.ReportError(
                    "pose_payload_readback_incomplete",
                    subject,
                    $"Pose payload '{payload.GetType().Name}' 当前正式读取合同没有保留可逆的 Euler 字段，拒绝不完整导出。");
                return "null";
            }

            context.ReportError(
                "pose_payload_unsupported",
                subject,
                $"Pose payload '{payload.GetType().FullName}' 没有正式 C# 输出适配。");
            return "null";
        }

        static string StateMachineExpression(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPoseStateMachineDefinition machine,
            IReadOnlyList<CharacterPoseResourceSlot> resourceSlots,
            string subject)
        {
            if (machine == null)
            {
                context.ReportError("pose_state_machine_missing", subject, "Pose StateMachine payload 缺少正式定义。");
                return "null";
            }
            return New(
                typeof(CharacterPoseStateMachineDefinition),
                Id(typeof(PoseStateMachineId), machine.StateMachineId.Value),
                BtsmtlAuthoringCodeSyntax.StringLiteral(machine.ContentRevision),
                New(
                    typeof(CharacterPoseStateEntry),
                    Id(typeof(PoseStateEntryId), machine.Entry.EntryId.Value),
                    Id(typeof(PoseStateId), machine.Entry.TargetStateId.Value)),
                ArrayExpression(typeof(CharacterPoseStateDefinition), machine.States.Select(StateExpression)),
                ArrayExpression(
                    typeof(CharacterPoseStateTransition),
                    machine.Transitions.Select(value => TransitionExpression(context, value, resourceSlots, subject))),
                ArrayExpression(typeof(CharacterPoseStateAlias), machine.Aliases.Select(AliasExpression)),
                machine.MaxTransitionsPerFrame.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        static string StateExpression(CharacterPoseStateDefinition state) => New(
            typeof(CharacterPoseStateDefinition),
            Id(typeof(PoseStateId), state.StateId.Value),
            BtsmtlAuthoringCodeSyntax.StringLiteral(state.DisplayName),
            Id(typeof(PoseGraphId), state.PoseGraphId.Value),
            Id(typeof(PoseNodeId), state.OutputPoseNodeId.Value),
            state.AlwaysResetOnEntry ? "true" : "false");

        static string AliasExpression(CharacterPoseStateAlias alias) => New(
            typeof(CharacterPoseStateAlias),
            Id(typeof(PoseStateAliasId), alias.AliasId.Value),
            BtsmtlAuthoringCodeSyntax.StringLiteral(alias.DisplayName),
            ArrayExpression(typeof(CharacterPoseStateTransitionSource), alias.Sources.Select(SourceExpression)));

        static string SourceExpression(CharacterPoseStateTransitionSource source) =>
            source.Kind == PoseStateTransitionSourceKind.State
                ? $"CharacterPoseStateTransitionSource.FromState({Id(typeof(PoseStateId), source.StateId.Value)})"
                : $"CharacterPoseStateTransitionSource.FromAlias({Id(typeof(PoseStateAliasId), source.AliasId.Value)})";

        static string TransitionExpression(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPoseStateTransition transition,
            IReadOnlyList<CharacterPoseResourceSlot> resourceSlots,
            string subject)
        {
            if (transition.Rule == null)
                context.ReportError("pose_transition_rule_missing", transition.TransitionId.Value, "Pose Transition 缺少正式 Rule。");
            return New(
                typeof(CharacterPoseStateTransition),
                Id(typeof(PoseStateTransitionId), transition.TransitionId.Value),
                SourceExpression(transition.Source),
                Id(typeof(PoseStateId), transition.TargetStateId.Value),
                transition.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
                RuleExpression(context, transition.Rule, transition.TransitionId.Value),
                EnumLiteral(typeof(AnimationTransitionBlendLogic), transition.BlendLogic),
                BtsmtlAuthoringCodeSyntax.FloatLiteral(transition.DurationSeconds),
                EnumLiteral(typeof(CharacterAnimationBlendMode), transition.BlendMode),
                transition.CustomBlendCurveSlot == null
                    ? "null"
                    : RequireResourceVariable(context, transition.CustomBlendCurveSlot, resourceSlots, subject),
                transition.BlendProfileSlot == null
                    ? "null"
                    : RequireResourceVariable(context, transition.BlendProfileSlot, resourceSlots, subject));
        }

        static string RuleExpression(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPoseTransitionRuleGraph rule,
            string subject)
        {
            if (rule == null)
                return "null";
            return New(
                typeof(CharacterPoseTransitionRuleGraph),
                Id(typeof(PoseTransitionRuleGraphId), rule.GraphId.Value),
                BtsmtlAuthoringCodeSyntax.StringLiteral(rule.ContentRevision),
                ArrayExpression(typeof(CharacterPoseTransitionRuleOperation), rule.Operations.Select(OperationExpression)),
                Id(typeof(PoseTransitionRuleOperationId), rule.OutputOperationId.Value));
        }

        static string OperationExpression(CharacterPoseTransitionRuleOperation operation) => New(
            typeof(CharacterPoseTransitionRuleOperation),
            Id(typeof(PoseTransitionRuleOperationId), operation.OperationId.Value),
            EnumLiteral(typeof(PoseTransitionRuleOperationKind), operation.Kind),
            operation.InputA.IsValid ? Id(typeof(PoseTransitionRuleOperationId), operation.InputA.Value) : "default",
            operation.InputB.IsValid ? Id(typeof(PoseTransitionRuleOperationId), operation.InputB.Value) : "default",
            operation.FactId.IsValid ? Id(typeof(PresentationFactId), operation.FactId.Value) : "default",
            $"boolLiteral: {(operation.BoolLiteral ? "true" : "false")}",
            $"floatLiteral: {BtsmtlAuthoringCodeSyntax.FloatLiteral(operation.FloatLiteral)}",
            $"enumTypeId: {BtsmtlAuthoringCodeSyntax.StringLiteral(operation.EnumTypeId)}",
            $"enumLiteral: {operation.EnumLiteral.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"identityLiteral: {BtsmtlAuthoringCodeSyntax.StringLiteral(operation.IdentityLiteral)}",
            operation.ParameterId.IsValid ? $"parameterId: {Id(typeof(PoseParameterId), operation.ParameterId.Value)}" : "parameterId: default");

        static string ParameterExpression(CharacterPoseParameterDeclaration parameter) => New(
            typeof(CharacterPoseParameterDeclaration),
            Id(typeof(PoseParameterId), parameter.ParameterId.Value),
            EnumLiteral(typeof(PoseParameterValueType), parameter.ValueType),
            BtsmtlAuthoringCodeSyntax.FloatLiteral(parameter.DefaultValue),
            BtsmtlAuthoringCodeSyntax.StringLiteral(parameter.Unit),
            EnumLiteral(typeof(CharacterPoseParameterUsage), parameter.Usage),
            BtsmtlAuthoringCodeSyntax.StringLiteral(parameter.DisplayName));

        static string ParameterPolicyExpression(CharacterPoseParameterPolicy policy) => New(
            typeof(CharacterPoseParameterPolicy),
            Id(typeof(PoseParameterId), policy.ParameterId.Value),
            EnumLiteral(typeof(PoseParameterResolvePolicy), policy.Policy));

        static string DynamicPortExpression(CharacterPoseDynamicPort port) => New(
            typeof(CharacterPoseDynamicPort),
            Id(typeof(PosePortId), port.PortId.Value),
            BtsmtlAuthoringCodeSyntax.StringLiteral(port.DisplayName),
            EnumLiteral(typeof(CharacterPosePortKind), port.Kind),
            EnumLiteral(typeof(CharacterPosePortDirection), port.Direction),
            port.Required ? "true" : "false",
            port.Order.ToString(System.Globalization.CultureInfo.InvariantCulture),
            port.InterfacePortId.IsValid
                ? Id(typeof(PoseInterfacePortId), port.InterfacePortId.Value)
                : "default");

        static string ConnectionExpression(CharacterPoseCanvasConnection connection) => New(
            typeof(CharacterPoseCanvasConnection),
            BtsmtlAuthoringCodeSyntax.StringLiteral(connection.EdgeId),
            Id(typeof(PoseNodeId), connection.SourceNodeId.Value),
            Id(typeof(PosePortId), connection.SourcePortId.Value),
            Id(typeof(PoseNodeId), connection.TargetNodeId.Value),
            Id(typeof(PosePortId), connection.TargetPortId.Value));

        static string LayoutEntryExpression(CharacterPoseGraphLayoutEntry entry) => New(
            typeof(CharacterPoseGraphLayoutEntry),
            Id(typeof(PoseNodeId), entry.NodeId.Value),
            BtsmtlAuthoringCodeValues.Vector2(entry.Position));

        static string LayoutExpression(CharacterPoseStateMachineLayout layout) => New(
            typeof(CharacterPoseStateMachineLayout),
            Id(typeof(PoseStateMachineId), layout.StateMachineId.Value),
            ArrayExpression(
                typeof(CharacterPoseStateMachineLayoutElement),
                layout.Elements.Select(element => New(
                    typeof(CharacterPoseStateMachineLayoutElement),
                    BtsmtlAuthoringCodeSyntax.StringLiteral(element.ElementId),
                    BtsmtlAuthoringCodeValues.Vector2(element.Position)))));

        static string RequireSourceVariable(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPresentationPoseSourceSlot slot,
            IReadOnlyList<CharacterPresentationPoseSourceSlot> slots,
            string subject)
        {
            if (slot == null || !slots.Contains(slot))
            {
                context.ReportError("pose_source_reference_invalid", subject, "Pose 节点引用了不属于当前闭包的 Source Slot。");
                return "null";
            }
            return context.RequireVariable(slot, subject);
        }

        static string RequireResourceVariable(
            BtsmtlAuthoringCodeExportContext context,
            CharacterPoseResourceSlot slot,
            IReadOnlyList<CharacterPoseResourceSlot> slots,
            string subject)
        {
            if (slot == null || !slots.Contains(slot))
            {
                context.ReportError("pose_resource_reference_invalid", subject, "Pose 节点引用了不属于当前闭包的 Resource Slot。");
                return "null";
            }
            return context.RequireVariable(slot, subject);
        }

        static string ArrayExpression(Type elementType, IEnumerable<string> values)
        {
            string[] items = (values ?? Enumerable.Empty<string>()).ToArray();
            string type = Type(elementType);
            return items.Length == 0
                ? $"new {type}[0]"
                : $"new {type}[] {{ {string.Join(", ", items)} }}";
        }

        static string New(Type type, params string[] arguments) =>
            $"new {Type(type)}({string.Join(", ", arguments)})";

        static string Id(Type type, string value) =>
            $"new {Type(type)}({BtsmtlAuthoringCodeSyntax.StringLiteral(value)})";

        static string EnumLiteral(Type type, object value) =>
            BtsmtlAuthoringCodeSyntax.EnumLiteral(Type(type), value.ToString());

        static string Type(Type type) => BtsmtlAuthoringCodeSyntax.TypeName(type);
    }
}
#endif
