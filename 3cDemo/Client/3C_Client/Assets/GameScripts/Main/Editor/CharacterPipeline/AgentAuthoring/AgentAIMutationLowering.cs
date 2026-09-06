using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentMutationLoweringSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentAIMutationLowering
    {
        internal static AgentMutation LowerEnsureAIControllerDefinition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string controllerId = context.RequiredText(operation.controllerId, string.Empty, "controllerId", "ensure_ai_controller_definition 缺少 ControllerId。");
            if (!string.Equals(controllerId, controllerId.Trim(), StringComparison.Ordinal))
                context.Error("controllerId", "ai_controller_id_invalid", "ControllerId 不能包含首尾空白。");
            return context.IsValid ? new AgentEnsureAIControllerDefinitionMutation(operation.id, context.Path, controllerId) : null;
        }

        internal static AgentMutation LowerEnsureAIControllerTree(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string path = context.RequiredText(operation.rootTreeAssetPath, string.Empty, "rootTreeAssetPath", "ensure_ai_controller_tree 缺少精确的 RootTree 资产路径。");
            return context.IsValid ? new AgentEnsureAIControllerTreeMutation(operation.id, context.Path, path) : null;
        }

        internal static AgentMutation LowerBindAIControllerAssets(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            if (string.IsNullOrEmpty(operation.controlledCharacterAssetPath) && string.IsNullOrEmpty(operation.controlledCharacterAssetGuid))
                context.Error("controlledCharacter", "controlled_character_reference_missing", "bind_ai_controller_assets 缺少 Character Definition 资产引用。");
            if (string.IsNullOrEmpty(operation.perceptionProfileAssetPath) && string.IsNullOrEmpty(operation.perceptionProfileAssetGuid))
                context.Error("perceptionProfile", "perception_profile_reference_missing", "bind_ai_controller_assets 缺少 Perception Profile 资产引用。");
            return context.IsValid
                ? new AgentBindAIControllerAssetsMutation(
                    operation.id,
                    context.Path,
                    new AgentAssetReference(string.Empty, operation.controlledCharacterAssetPath, operation.controlledCharacterAssetGuid),
                    new AgentAssetReference(string.Empty, operation.perceptionProfileAssetPath, operation.perceptionProfileAssetGuid))
                : null;
        }

        internal static AgentMutation LowerConfigureAICandidates(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            TryParseEnum(context, operation.candidateOrdering, "candidateOrdering", out AICandidateOrdering ordering);
            var values = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (operation.candidateActorIds == null)
            {
                context.Error("candidateActorIds", "candidate_actor_ids_missing", "configure_ai_candidates 缺少显式候选 ActorId 列表。");
            }
            else
            {
                for (int i = 0; i < operation.candidateActorIds.Count; i++)
                {
                    string actorId = operation.candidateActorIds[i];
                    if (string.IsNullOrWhiteSpace(actorId) || !string.Equals(actorId, actorId.Trim(), StringComparison.Ordinal) || !unique.Add(actorId))
                        context.Error($"candidateActorIds[{i}]", "candidate_actor_id_invalid", $"候选 ActorId 缺失、重复或包含首尾空白：{actorId}");
                    else
                        values.Add(actorId);
                }
            }
            return context.IsValid ? new AgentConfigureAICandidatesMutation(operation.id, context.Path, ordering, values) : null;
        }

        internal static AgentMutation LowerEnsureAIBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration", true);
            string key = context.RequiredText(operation.blackboardKey, operation.displayName, "blackboardKey", "ensure_ai_blackboard_declaration 缺少 Blackboard key。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope);
            if (scope != PipelineBlackboardVariableScope.AIController && scope != PipelineBlackboardVariableScope.AITick && scope != PipelineBlackboardVariableScope.Graph)
                context.Error("blackboardScope", "ai_blackboard_scope_invalid", $"AI Blackboard 不允许 scope：{scope}");
            return context.IsValid
                ? new AgentEnsureAIBlackboardDeclarationMutation(operation.id, context.Path, graph, existing, key, valueType, scope, AIBlackboardDefault(operation, valueType))
                : null;
        }

        internal static AgentMutation LowerEnsureAISharedNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAISharedNodeKind nodeKind);
            LoopNode.StopType loopStopType = LoopNode.StopType.None;
            CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
            if (nodeKind == AgentAISharedNodeKind.Loop)
                TryParseEnum(context, operation.loopStopType, "loopStopType", out loopStopType);
            if (nodeKind == AgentAISharedNodeKind.Compare)
                TryParseEnum(context, operation.compareType, "compareType", out compareType);
            return context.IsValid
                ? new AgentEnsureAISharedNodeMutation(operation.id, context.Path, graph, existing, nodeKind, loopStopType, compareType, operation.position)
                : null;
        }

        internal static AgentMutation LowerEnsureAIObservationNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAIObservationNodeKind kind);
            return context.IsValid ? new AgentEnsureAIObservationNodeMutation(operation.id, context.Path, graph, existing, kind, operation.position) : null;
        }

        internal static AgentMutation LowerEnsureAIMemoryNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration");
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAIMemoryNodeKind nodeKind);
            TryParseEnum(context, operation.aiMemoryValueKind, "aiMemoryValueKind", out AIMemoryValueKind valueKind);
            return context.IsValid ? new AgentEnsureAIMemoryNodeMutation(operation.id, context.Path, graph, existing, declaration, nodeKind, valueKind, operation.position) : null;
        }

        internal static AgentMutation LowerEnsureAIContinuousInput(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, string.Empty, "inputId", "ensure_ai_continuous_input 缺少 InputId。");
            return context.IsValid ? new AgentEnsureAIContinuousInputMutation(operation.id, context.Path, graph, existing, inputId, operation.position) : null;
        }

        internal static AgentMutation LowerEnsureAIActionTarget(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, string.Empty, "inputId", "ensure_ai_action_target 缺少 InputId。");
            return context.IsValid ? new AgentEnsureAIActionTargetMutation(operation.id, context.Path, graph, existing, inputId, operation.position) : null;
        }

        internal static AgentMutation LowerEnsureAIActionRequest(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string requestId = context.RequiredText(operation.request, string.Empty, "request", "ensure_ai_action_request 缺少 RequestId。");
            TryParseEnum(context, operation.aiRequestRepeatPolicy, "aiRequestRepeatPolicy", out AIRequestRepeatPolicy repeatPolicy);
            if (operation.requestBufferSeconds < 0f)
                context.Error("requestBufferSeconds", "ai_request_buffer_invalid", "Action Request buffer seconds 不能小于 0。");
            return context.IsValid
                ? new AgentEnsureAIActionRequestMutation(operation.id, context.Path, graph, existing, requestId, operation.requestBufferSeconds, operation.requestPriority, repeatPolicy, operation.position)
                : null;
        }
    }
}

