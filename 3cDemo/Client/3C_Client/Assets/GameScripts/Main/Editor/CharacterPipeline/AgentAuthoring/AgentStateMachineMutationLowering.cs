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
    internal static class AgentStateMachineMutationLowering
    {
        internal static AgentMutation LowerEnsureStateMachine(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference parent = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existingOwner = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string existingGraphId = context.OptionalAuthoringId(operation.stateMachineGraphAuthoringId, "stateMachineGraphAuthoringId");
            string displayName = context.RequiredText(operation.displayName, operation.stateMachine, "displayName", "ensure_state_machine 缺少 displayName/stateMachine。");
            return context.IsValid
                ? new AgentEnsureStateMachineMutation(operation.id, context.Path, parent, existingOwner, existingGraphId, displayName, operation.lifecycleSlot, operation.position)
                : null;
        }

        internal static AgentMutation LowerEnsureState(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine", true);
            AgentStateTargetReference existingState = context.OptionalState(operation.stateAuthoringId, operation.statePlannedIdentity, "state", true);
            string stateName = context.RequiredText(operation.state, operation.displayName, "state", "ensure_state 缺少 state/displayName。");
            return context.IsValid
                ? new AgentEnsureStateMutation(operation.id, context.Path, stateMachine, existingState, stateName, operation.position)
                : null;
        }

        internal static AgentMutation LowerDeleteState(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine");
            AgentStateTargetReference state = context.RequiredState(operation.stateAuthoringId, operation.statePlannedIdentity, "state");
            return context.IsValid
                ? new AgentDeleteStateMutation(operation.id, context.Path, stateMachine, state)
                : null;
        }

        internal static AgentMutation LowerEnsureTransition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            context.ReadTransition(operation, out AgentStateMachineTargetReference stateMachine, out AgentElementTargetReference from, out AgentElementTargetReference to);
            string edge = ReadEnsureTransitionIdentity(context, operation, "ensure_transition 缺少 stable edge identity。");
            return context.IsValid
                ? new AgentEnsureTransitionMutation(operation.id, AgentMutationKind.EnsureTransition, "ensure_transition", context.Path, stateMachine, from, to, edge, operation.transitionPriority, operation.position)
                : null;
        }

        internal static AgentMutation LowerRewireTransition(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            context.ReadTransition(
                operation,
                out AgentStateMachineTargetReference stateMachine,
                out AgentElementTargetReference from,
                out AgentElementTargetReference to);
            string edge = context.OptionalAuthoringId(
                context.RequiredText(
                    operation.targetElementAuthoringId,
                    string.Empty,
                    "targetElementAuthoringId",
                    "rewire_transition 缺少 stable edge identity。"),
                "targetElementAuthoringId");
            return context.IsValid
                ? new AgentRewireTransitionMutation(
                    operation.id,
                    context.Path,
                    stateMachine,
                    from,
                    to,
                    edge,
                    operation.transitionPriority)
                : null;
        }

        internal static AgentMutation LowerEnsureConditionRule(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            context.ReadTransition(operation, out AgentStateMachineTargetReference stateMachine, out AgentElementTargetReference from, out AgentElementTargetReference to);
            string edge = ReadEnsureTransitionIdentity(context, operation, "ensure_condition_rule 缺少 stable edge identity。");
            List<AgentConditionGroupMutation> groups = context.RequiredConditionGroups(operation.conditionGroups, operation);
            return context.IsValid
                ? new AgentEnsureConditionRuleMutation(operation.id, context.Path, stateMachine, from, to, edge, operation.transitionPriority, groups, operation.position)
                : null;
        }

        internal static string ReadEnsureTransitionIdentity(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation,
            string missingMessage)
        {
            string identity = context.RequiredText(
                operation.targetElementAuthoringId,
                string.Empty,
                "targetElementAuthoringId",
                missingMessage);
            return identity.StartsWith("local:", StringComparison.Ordinal)
                ? identity
                : context.OptionalAuthoringId(identity, "targetElementAuthoringId");
        }

        internal static AgentMutation LowerDeleteTransition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_transition 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeleteTransitionMutation(operation.id, context.Path, stateMachine, edge) : null;
        }
    }
}

