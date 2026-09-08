using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentDocumentMutationSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentGraphDocumentMutationModule
    {
        static readonly BtsmtlGraphAuthoringCapabilities s_Capabilities = new BtsmtlGraphAuthoringCapabilities();

        internal static void BuildGraphCreationMutations(
            IReadOnlyList<AgentSnapshotGraph> current,
            IReadOnlyList<AgentSnapshotGraph> target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var currentGraphs = Index(current, value => value.graphAuthoringId, "document.editable.graphs", report);
            foreach (AgentSnapshotGraph graph in (target ?? Array.Empty<AgentSnapshotGraph>())
                         .OrderBy(value => GraphDepth(value, target, new HashSet<string>(StringComparer.Ordinal)))
                         .ThenBy(value => value?.graphAuthoringId, StringComparer.Ordinal))
            {
                if (!IsLocal(graph?.graphAuthoringId) ||
                    currentGraphs.ContainsKey(graph.graphAuthoringId) ||
                    string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal) ||
                    string.Equals(graph.kind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal) ||
                    string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                    continue;
                if (!TryFindGraphOwner(target, graph.graphAuthoringId, out AgentSnapshotGraph parent, out AgentSnapshotNode owner))
                {
                    report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "graph_owner_required", "新增Graph必须声明可解析的owner Node与referenceKey。");
                    continue;
                }
                        Add(mutations, $"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", AgentMutationKind.EnsureGraph, operation =>
                {
                    operation.id = LocalIdentity(graph.graphAuthoringId);
                    SetGraph(operation, parent.graphAuthoringId);
                    operation.graphKind = graph.kind;
                    operation.graphOwnership = graph.ownership;
                    operation.graphReferenceKey = graph.referenceKey;
                    operation.graphReferenceSharedAssetPath = graph.sharedAssetPath;
                    operation.displayName = string.IsNullOrEmpty(graph.name) ? graph.graphAuthoringId : graph.name;
                    if (IsLocal(owner.elementAuthoringId))
                        operation.graphOwnerElementPlannedIdentity = LocalIdentity(owner.elementAuthoringId);
                    else
                        operation.graphOwnerElementAuthoringId = owner.elementAuthoringId;
                });
            }
        }

        internal static void BuildStateMachineCreationMutations(
            IReadOnlyList<AgentSnapshotStateMachineSummary> current,
            IReadOnlyList<AgentSnapshotStateMachineSummary> target,
            IReadOnlyList<AgentSnapshotGraph> targetGraphs,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var currentMachines = Index(current, value => value.graphAuthoringId, "document.editable.stateMachines", report);
            foreach (AgentSnapshotStateMachineSummary machine in (target ?? Array.Empty<AgentSnapshotStateMachineSummary>())
                         .OrderBy(value => GraphDepth(
                             (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                                 .FirstOrDefault(graph => string.Equals(graph?.graphAuthoringId, value?.graphAuthoringId, StringComparison.Ordinal)),
                             targetGraphs,
                             new HashSet<string>(StringComparer.Ordinal)))
                         .ThenBy(value => value?.graphAuthoringId, StringComparer.Ordinal))
            {
                if (machine == null)
                    continue;
                string machinePath = $"document.editable.stateMachines[{Escape(machine.graphAuthoringId)}]";
                bool createMachine = IsLocal(machine.graphAuthoringId) && !currentMachines.ContainsKey(machine.graphAuthoringId);
                if (!createMachine && !currentMachines.ContainsKey(machine.graphAuthoringId))
                    continue;
                AgentSnapshotGraph machineGraph = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                    .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, machine.graphAuthoringId, StringComparison.Ordinal));
                AgentSnapshotGraph parent = null;
                AgentSnapshotNode owner = null;
                if (createMachine && !TryFindGraphOwner(targetGraphs, machine.graphAuthoringId, out parent, out owner))
                {
                    report.Error(machinePath, "state_machine_local_parent_missing", "新StateMachine需要由Graph中的StateMachineNode明确声明owner与parent。");
                    continue;
                }
                if (createMachine)
                {
                    Add(mutations, machinePath, AgentMutationKind.EnsureStateMachine, operation =>
                    {
                        operation.id = LocalIdentity(owner.elementAuthoringId);
                        SetGraph(operation, parent);
                        operation.displayName = machine.name;
                        operation.position = ToVector(owner.position);
                    });
                    Add(mutations, machinePath + ".graph", AgentMutationKind.EnsureGraph, operation =>
                    {
                        operation.id = LocalIdentity(machine.graphAuthoringId);
                        SetGraph(operation, parent.graphAuthoringId);
                        operation.graphKind = AgentGraphKind.StateMachineGraph.ToString();
                        operation.graphOwnership = AgentGraphOwnership.Inline.ToString();
                        operation.graphReferenceKey = "scopedGraph.m_InlineGraph";
                        operation.displayName = machine.name;
                        operation.graphOwnerElementPlannedIdentity = LocalIdentity(owner.elementAuthoringId);
                    });
                }
                foreach (AgentSnapshotStateSummary state in machine.states ?? new List<AgentSnapshotStateSummary>())
                {
                    if (!IsLocal(state?.stateAuthoringId))
                        continue;
                    string statePath = $"{machinePath}.states[{Escape(state.stateAuthoringId)}]";
                    Add(mutations, statePath, AgentMutationKind.EnsureState, operation =>
                    {
                        operation.id = LocalIdentity(state.stateAuthoringId);
                        if (machineGraph != null)
                            SetStateMachine(operation, machineGraph, machine.graphAuthoringId);
                        else if (createMachine && owner != null && IsLocal(owner.elementAuthoringId))
                            SetStateMachine(operation, owner.elementAuthoringId);
                        else
                            SetStateMachine(operation, machine.graphAuthoringId);
                        operation.state = state.state;
                        operation.position = FindNodePosition(targetGraphs, state.stateAuthoringId);
                    });
                    if (IsLocal(state.behaviorGraphAuthoringId))
                    {
                        Add(mutations, statePath + ".behaviorGraph", AgentMutationKind.EnsureGraph, operation =>
                        {
                            operation.id = LocalIdentity(state.behaviorGraphAuthoringId);
                            if (machineGraph != null)
                                SetGraph(operation, machineGraph);
                            else
                                SetGraph(operation, machine.graphAuthoringId);
                            operation.graphKind = AgentGraphKind.StateBehaviorSubTree.ToString();
                            operation.graphOwnership = AgentGraphOwnership.Inline.ToString();
                            operation.graphReferenceKey = "stateBehaviorGraph.m_InlineSubTree";
                            operation.displayName = string.IsNullOrEmpty(state.state) ? "State Behavior" : state.state + " State Body";
                            operation.graphOwnerElementPlannedIdentity = LocalIdentity(state.stateAuthoringId);
                        });
                    }
                }
            }
        }

        internal static void BuildStateMachineMutations(
            string rootGraphAuthoringId,
            IReadOnlyList<AgentSnapshotStateMachineSummary> current,
            IReadOnlyList<AgentSnapshotStateMachineSummary> target,
            IReadOnlyList<AgentSnapshotGraph> currentGraphs,
            IReadOnlyList<AgentSnapshotGraph> targetGraphs,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            bool includeCreation = true)
        {
            var currentMachines = Index(current, value => value.graphAuthoringId, "document.editable.stateMachines", report);
            var targetMachines = Index(target, value => value.graphAuthoringId, "document.editable.stateMachines", report);
            foreach (AgentSnapshotStateMachineSummary machine in target ?? Array.Empty<AgentSnapshotStateMachineSummary>())
            {
                string machinePath = $"document.editable.stateMachines[{Escape(machine.graphAuthoringId)}]";
                AgentSnapshotGraph machineGraph = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                    .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, machine.graphAuthoringId, StringComparison.Ordinal));
                AgentSnapshotGraph currentMachineGraph = (currentGraphs ?? Array.Empty<AgentSnapshotGraph>())
                    .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, machine.graphAuthoringId, StringComparison.Ordinal));
                currentMachines.TryGetValue(machine.graphAuthoringId, out AgentSnapshotStateMachineSummary oldMachine);
                if (IsLocal(machine.graphAuthoringId))
                {
                    AgentSnapshotGraph newParent = null;
                    AgentSnapshotNode newOwner = null;
                    if (includeCreation && !TryFindGraphOwner(targetGraphs, machine.graphAuthoringId, out newParent, out newOwner))
                    {
                        report.Error(machinePath, "state_machine_local_parent_missing", "新StateMachine需要由Graph中的StateMachineNode明确声明owner与parent。");
                        continue;
                    }
                    if (includeCreation)
                    {
                        Add(mutations, machinePath, AgentMutationKind.EnsureStateMachine, operation =>
                        {
                            operation.id = LocalIdentity(newOwner.elementAuthoringId);
                            SetGraph(operation, newParent);
                            operation.displayName = machine.name;
                            operation.position = ToVector(newOwner.position);
                        });
                    }
                }
                else if (oldMachine == null)
                {
                    report.Error(machinePath, "state_machine_identity_unknown", "StateMachine identity不在当前树中；新StateMachine必须使用document-local identity。");
                    continue;
                }
                else if (!string.Equals(oldMachine.name, machine.name, StringComparison.Ordinal) &&
                         TryFindGraphOwner(targetGraphs, machine.graphAuthoringId, out AgentSnapshotGraph existingParent, out AgentSnapshotNode existingOwner))
                {
                    Add(mutations, machinePath, AgentMutationKind.EnsureStateMachine, operation =>
                    {
                        SetGraph(operation, existingParent.graphAuthoringId);
                        operation.targetElementAuthoringId = existingOwner.elementAuthoringId;
                        operation.stateMachineGraphAuthoringId = machine.graphAuthoringId;
                        operation.displayName = machine.name;
                        operation.position = ToVector(existingOwner.position);
                    });
                }
                if (currentMachineGraph != null && machineGraph != null &&
                    (!string.Equals(currentMachineGraph.ownership, machineGraph.ownership, StringComparison.Ordinal) ||
                     !SameOptionalText(currentMachineGraph.sharedAssetPath, machineGraph.sharedAssetPath)) &&
                    TryFindGraphOwner(targetGraphs, machine.graphAuthoringId, out AgentSnapshotGraph machineParent, out AgentSnapshotNode machineOwner))
                {
                    AddGraphReferenceOwnershipMutation(
                        machinePath + ".ownership",
                        machineParent,
                        machineOwner,
                        machineGraph,
                        mutations,
                        report);
                }

                oldMachine ??= new AgentSnapshotStateMachineSummary();

                var oldStates = Index(oldMachine.states, value => value.stateAuthoringId, machinePath + ".states", report);
                var newStates = Index(machine.states, value => value.stateAuthoringId, machinePath + ".states", report);
                foreach (AgentSnapshotStateSummary state in machine.states ?? new List<AgentSnapshotStateSummary>())
                {
                    string path = $"{machinePath}.states[{Escape(state.stateAuthoringId)}]";
                    oldStates.TryGetValue(state.stateAuthoringId, out AgentSnapshotStateSummary oldState);
                    if (IsLocal(state.stateAuthoringId))
                    {
                        if (includeCreation)
                        {
                            Add(mutations, path, AgentMutationKind.EnsureState, operation =>
                            {
                                operation.id = LocalIdentity(state.stateAuthoringId);
                                SetStateMachine(
                                    operation,
                                    machineGraph,
                                    machine.graphAuthoringId);
                                operation.state = state.state;
                                operation.position = FindNodePosition(targetGraphs, state.stateAuthoringId);
                            });
                            if (IsLocal(state.behaviorGraphAuthoringId))
                            {
                                Add(mutations, path + ".behaviorGraph", AgentMutationKind.EnsureGraph, operation =>
                                {
                                    operation.id = LocalIdentity(state.behaviorGraphAuthoringId);
                                    if (machineGraph != null)
                                        SetGraph(operation, machineGraph);
                                    else
                                        SetGraph(operation, machine.graphAuthoringId);
                                    operation.graphKind = AgentGraphKind.StateBehaviorSubTree.ToString();
                                    operation.graphOwnership = AgentGraphOwnership.Inline.ToString();
                                    operation.graphReferenceKey = "stateBehaviorGraph.m_InlineSubTree";
                                    operation.displayName = string.IsNullOrEmpty(state.state) ? "State Behavior" : state.state + " State Body";
                                    operation.graphOwnerElementPlannedIdentity = LocalIdentity(state.stateAuthoringId);
                                });
                            }
                        }
                    }
                    else if (oldState == null)
                    {
                        report.Error(path, "state_identity_unknown", "State identity不在当前树中；新State必须使用local:前缀。");
                    }
                    else if (!string.Equals(oldState.state, state.state, StringComparison.Ordinal))
                    {
                        Add(mutations, path, AgentMutationKind.EnsureState, operation =>
                        {
                            SetStateMachine(
                                operation,
                                machineGraph,
                                machine.graphAuthoringId);
                            operation.stateAuthoringId = state.stateAuthoringId;
                            operation.state = state.state;
                            operation.position = FindNodePosition(targetGraphs, state.stateAuthoringId);
                        });
                    }
                            oldState ??= new AgentSnapshotStateSummary();
                            AgentSnapshotGraph behaviorGraph = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                                .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, state.behaviorGraphAuthoringId, StringComparison.Ordinal));
                            AgentSnapshotGraph currentBehaviorGraph = (currentGraphs ?? Array.Empty<AgentSnapshotGraph>())
                                .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, state.behaviorGraphAuthoringId, StringComparison.Ordinal));
                            if (currentBehaviorGraph != null && behaviorGraph != null &&
                                (!string.Equals(currentBehaviorGraph.ownership, behaviorGraph.ownership, StringComparison.Ordinal) ||
                                 !SameOptionalText(currentBehaviorGraph.sharedAssetPath, behaviorGraph.sharedAssetPath)) &&
                                TryFindGraphOwner(targetGraphs, state.behaviorGraphAuthoringId, out AgentSnapshotGraph behaviorParent, out AgentSnapshotNode behaviorOwner))
                            {
                                AddGraphReferenceOwnershipMutation(
                                    path + ".behaviorGraph.ownership",
                                    behaviorParent,
                                    behaviorOwner,
                                    behaviorGraph,
                                    mutations,
                                    report);
                            }
                    var oldActivations = Index(oldState.actionActivations, value => value.nodeAuthoringId, path + ".actionActivations", report);
                    foreach (AgentSnapshotActionActivationSummary activation in state.actionActivations ?? new List<AgentSnapshotActionActivationSummary>())
                    {
                        string activationPath = $"{path}.actionActivations[{Escape(activation.nodeAuthoringId)}]";
                        if (oldActivations.TryGetValue(activation.nodeAuthoringId, out AgentSnapshotActionActivationSummary oldActivation) &&
                            Same(oldActivation, activation))
                            continue;
                        Add(mutations, activationPath, AgentMutationKind.EnsureActionActivation, operation =>
                        {
                            if (IsLocal(activation.nodeAuthoringId))
                                operation.id = LocalIdentity(activation.nodeAuthoringId);
                            if (behaviorGraph != null)
                                SetTargetGraph(operation, behaviorGraph);
                            else
                                operation.targetGraphAuthoringId = state.behaviorGraphAuthoringId;
                            SetOptionalExisting(operation, activation.nodeAuthoringId, false);
                            operation.displayName = activation.displayName;
                            operation.lifecycleSlot = "OnEnter";
                            operation.actionProfile = activation.actionProfile;
                            operation.actionContext = activation.actionContext;
                            operation.sourceInputRequestId = activation.sourceRequest;
                            operation.consumeSourceInputRequest = true;
                            operation.targetKey = activation.targetKey;
                            operation.targetSnapshotBlackboardKey = activation.targetSnapshotBlackboardKey;
                            operation.position = FindNodePosition(targetGraphs, activation.nodeAuthoringId);
                        });
                    }
                    var oldLifecycleTransitions = Index(
                        oldState.lifecycleTransitions,
                        value => value.nodeAuthoringId,
                        path + ".lifecycleTransitions",
                        report);
                    foreach (AgentSnapshotLifecycleSummary lifecycle in state.lifecycleTransitions ?? new List<AgentSnapshotLifecycleSummary>())
                    {
                        string lifecyclePath = $"{path}.lifecycleTransitions[{Escape(lifecycle.nodeAuthoringId)}]";
                        if (oldLifecycleTransitions.TryGetValue(lifecycle.nodeAuthoringId, out AgentSnapshotLifecycleSummary oldLifecycle) &&
                            Same(oldLifecycle, lifecycle))
                            continue;
                        Add(mutations, lifecyclePath, AgentMutationKind.EnsureActionLifecycleTransition, operation =>
                        {
                            if (IsLocal(lifecycle.nodeAuthoringId))
                                operation.id = LocalIdentity(lifecycle.nodeAuthoringId);
                            if (behaviorGraph != null)
                                SetTargetGraph(operation, behaviorGraph);
                            else
                                operation.targetGraphAuthoringId = state.behaviorGraphAuthoringId;
                            SetOptionalExisting(operation, lifecycle.nodeAuthoringId, false);
                            operation.displayName = lifecycle.displayName;
                            operation.lifecycleType = lifecycle.transitionType;
                            operation.reason = lifecycle.reason;
                            operation.actionContext = lifecycle.actionContext;
                            operation.position = FindNodePosition(targetGraphs, lifecycle.nodeAuthoringId);
                        });
                    }
                    var oldTimelines = Index(oldState.timelines, value => value.nodeAuthoringId, path + ".timelines", report);
                    foreach (AgentSnapshotTimelineBindingSummary timeline in state.timelines ?? new List<AgentSnapshotTimelineBindingSummary>())
                    {
                        string timelinePath = $"{path}.timelines[{Escape(timeline.nodeAuthoringId)}]";
                        if (oldTimelines.TryGetValue(timeline.nodeAuthoringId, out AgentSnapshotTimelineBindingSummary oldTimeline) &&
                            Same(oldTimeline, timeline))
                            continue;
                        Add(mutations, timelinePath, AgentMutationKind.EnsureTimelineNode, operation =>
                        {
                            if (IsLocal(timeline.nodeAuthoringId))
                                operation.id = LocalIdentity(timeline.nodeAuthoringId);
                            if (behaviorGraph != null)
                                SetTargetGraph(operation, behaviorGraph);
                            else
                                operation.targetGraphAuthoringId = state.behaviorGraphAuthoringId;
                            SetOptionalExisting(operation, timeline.nodeAuthoringId, false);
                            operation.displayName = timeline.displayName;
                            operation.timeline = timeline.timeline;
                            operation.timelineAuthoringId = timeline.timelineAuthoringId;
                            operation.timelineOwnership = timeline.ownership;
                            operation.timelineAssetPath = timeline.timelineAssetPath;
                            operation.timelineAssetGuid = timeline.timelineAssetGuid;
                            operation.actionContext = timeline.actionContext;
                            operation.position = FindNodePosition(targetGraphs, timeline.nodeAuthoringId);
                        });
                    }
                }
                foreach (AgentSnapshotStateSummary state in oldMachine.states ?? new List<AgentSnapshotStateSummary>())
                {
                    if (newStates.ContainsKey(state.stateAuthoringId))
                        continue;
                    Add(mutations, $"{machinePath}.states[{Escape(state.stateAuthoringId)}]", AgentMutationKind.DeleteState, operation =>
                    {
                        SetStateMachine(
                            operation,
                            machineGraph,
                            machine.graphAuthoringId);
                        operation.stateAuthoringId = state.stateAuthoringId;
                    });
                }

                var removedStates = new HashSet<string>(oldStates.Keys.Except(newStates.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
                var oldTransitions = Index(oldMachine.transitions, value => value.edgeAuthoringId, machinePath + ".transitions", report);
                var newTransitions = Index(machine.transitions, value => value.edgeAuthoringId, machinePath + ".transitions", report);
                foreach (AgentSnapshotTransitionSummary transition in machine.transitions ?? new List<AgentSnapshotTransitionSummary>())
                {
                    string path = $"{machinePath}.transitions[{Escape(transition.edgeAuthoringId)}]";
                    bool changed = !oldTransitions.TryGetValue(transition.edgeAuthoringId, out AgentSnapshotTransitionSummary oldTransition) ||
                                   !Same(oldTransition, transition);
                    if (!changed)
                        continue;
                    if (oldTransition != null &&
                        !SameTransitionEndpoints(oldTransition, transition))
                    {
                        if (!SameTransitionExceptEndpoints(oldTransition, transition))
                        {
                            report.Error(
                                path,
                                "transition_rewire_mixed_change",
                                "已有Transition改接端点时只能迁移端点；条件与priority必须保持不变。请先完成端点迁移，再单独修改其它业务语义。");
                            continue;
                        }
                        Add(
                            mutations,
                            path + ".endpoints",
                            AgentMutationKind.RewireTransition,
                            operation =>
                            {
                                SetStateMachine(
                                    operation,
                                    machineGraph,
                                    machine.graphAuthoringId);
                                SetElement(operation, transition.fromElementAuthoringId, true);
                                SetElement(operation, transition.toElementAuthoringId, false);
                                operation.targetElementAuthoringId =
                                    transition.edgeAuthoringId;
                                operation.transitionPriority =
                                    transition.priority;
                            });
                        continue;
                    }
                    if (oldTransition != null && SameTransitionExceptActionAdmission(oldTransition, transition))
                    {
                        List<AgentSnapshotConditionTerm> admissionTerms = (transition.conditionTerms ?? new List<AgentSnapshotConditionTerm>())
                            .Where(term => string.Equals(term.kind, "action_can_activate", StringComparison.Ordinal))
                            .ToList();
                        List<AgentSnapshotGraph> conditionGraphs = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                            .Where(graph => string.Equals(graph.ownerElementAuthoringId, transition.edgeAuthoringId, StringComparison.Ordinal) &&
                                            string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                            .ToList();
                        List<AgentSnapshotNode> admissionNodes = conditionGraphs
                            .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                            .Where(node => node.typeName?.EndsWith(".CanActivateActionInfoNode", StringComparison.Ordinal) == true)
                            .ToList();
                        if (admissionTerms.Count != 1 || string.IsNullOrWhiteSpace(admissionTerms[0].actionProfile) ||
                            conditionGraphs.Count != 1 || admissionNodes.Count != 1)
                        {
                            report.Error(path, "transition_action_admission_ambiguous", "Transition的Action准入必须对应唯一Condition Rule Graph、CanActivateActionInfoNode和ActionProfile。");
                            continue;
                        }
                        Add(mutations, path + ".conditionTerms[action_can_activate]", AgentMutationKind.ConfigureActionAdmission, operation =>
                        {
                            SetGraph(operation, conditionGraphs[0].graphAuthoringId);
                            operation.targetElementAuthoringId = admissionNodes[0].elementAuthoringId;
                            operation.actionProfile = admissionTerms[0].actionProfile;
                        });
                        continue;
                    }
                    Add(mutations, path, transition.conditionTerms != null && transition.conditionTerms.Count > 0 ? AgentMutationKind.EnsureConditionRule : AgentMutationKind.EnsureTransition, operation =>
                    {
                        SetStateMachine(
                            operation,
                            machineGraph,
                            machine.graphAuthoringId);
                        SetElement(operation, transition.fromElementAuthoringId, true);
                        SetElement(operation, transition.toElementAuthoringId, false);
                        operation.targetElementAuthoringId = transition.edgeAuthoringId;
                        operation.transitionPriority = transition.priority;
                        operation.conditionGroups = ToConditionGroups(transition.conditionTerms);
                    });
                }
                foreach (AgentSnapshotTransitionSummary transition in oldMachine.transitions ?? new List<AgentSnapshotTransitionSummary>())
                {
                    if (newTransitions.ContainsKey(transition.edgeAuthoringId))
                        continue;
                    if (removedStates.Contains(transition.fromElementAuthoringId) ||
                        removedStates.Contains(transition.toElementAuthoringId))
                        continue;
                    Add(mutations, $"{machinePath}.transitions[{Escape(transition.edgeAuthoringId)}]", AgentMutationKind.DeleteTransition, operation =>
                    {
                        SetStateMachine(
                            operation,
                            machineGraph,
                            machine.graphAuthoringId);
                        operation.targetElementAuthoringId = transition.edgeAuthoringId;
                    });
                }
            }
            foreach (string removedMachine in currentMachines.Keys.Except(targetMachines.Keys, StringComparer.Ordinal))
            {
                if (AgentSkillDocumentMapper.IsRootCompositionStateMachine(
                        currentMachines[removedMachine],
                        rootGraphAuthoringId,
                        currentGraphs))
                    continue;
                report.Error($"document.editable.stateMachines[{Escape(removedMachine)}]", "state_machine_delete_unsupported", "当前正式authoring API不支持删除整个StateMachine；请删除其owner节点。");
            }
        }

        internal static void BuildGraphEdgeMutations(
            IReadOnlyList<AgentSnapshotGraph> current,
            IReadOnlyList<AgentSnapshotGraph> target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldGraphs = Index(current, value => value.graphAuthoringId, "document.editable.graphs", report);
            foreach (AgentSnapshotGraph graph in target ?? Array.Empty<AgentSnapshotGraph>())
            {
                string graphPath = $"document.editable.graphs[{Escape(graph.graphAuthoringId)}]";
                if (!oldGraphs.TryGetValue(graph.graphAuthoringId, out AgentSnapshotGraph oldGraph))
                {
                    report.Error(graphPath, "graph_create_unsupported", "当前Document版本不允许直接创建裸Graph；Graph必须由拥有它的State或节点创建。");
                    continue;
                }
                var oldFlow = Index(oldGraph.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
                var newFlow = Index(graph.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
                foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                {
                    string path = $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]";
                    if (oldFlow.TryGetValue(edge.elementAuthoringId, out AgentSnapshotFlowEdge oldEdge) && SameFlowEdge(oldEdge, edge))
                        continue;
                    if (oldFlow.ContainsKey(edge.elementAuthoringId))
                    {
                        Add(mutations, path, AgentMutationKind.DeleteFlowEdge, operation =>
                        {
                            SetGraph(operation, graph);
                            operation.targetElementAuthoringId = edge.elementAuthoringId;
                        });
                    }
                    Add(mutations, path, AgentMutationKind.LinkFlow, operation =>
                    {
                        SetGraph(operation, graph);
                        SetLinkElement(operation, edge.startElementAuthoringId, true);
                        SetLinkElement(operation, edge.endElementAuthoringId, false);
                        operation.startPort = edge.startPort;
                        operation.endPort = edge.endPort;
                        operation.flowEdgeAuthoringId = edge.elementAuthoringId;
                    });
                }
                foreach (AgentSnapshotFlowEdge edge in oldGraph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                {
                    if (newFlow.ContainsKey(edge.elementAuthoringId))
                        continue;
                    Add(mutations, $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeleteFlowEdge, operation =>
                    {
                        SetGraph(operation, graph);
                        operation.targetElementAuthoringId = edge.elementAuthoringId;
                    });
                }
                BuildPropertyEdgeMutations(oldGraph, graph, mutations, report);
            }
        }

        internal static void BuildCharacterGraphMutations(
            IReadOnlyList<AgentSnapshotGraph> current,
            IReadOnlyList<AgentSnapshotGraph> target,
            IReadOnlyList<AgentSnapshotStateMachineSummary> targetStateMachines,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldGraphs = Index(current, value => value.graphAuthoringId, "document.editable.graphs", report);
            var targetGraphs = Index(target, value => value.graphAuthoringId, "document.editable.graphs", report);
            var targetOwnerIds = new HashSet<string>(
                (target ?? Array.Empty<AgentSnapshotGraph>())
                    .SelectMany(value => value.nodes ?? new List<AgentSnapshotNode>())
                    .Where(value => value != null && !string.IsNullOrEmpty(value.elementAuthoringId))
                    .Select(value => value.elementAuthoringId),
                StringComparer.Ordinal);
            foreach (AgentSnapshotStateMachineSummary machine in targetStateMachines ?? Array.Empty<AgentSnapshotStateMachineSummary>())
            {
                foreach (AgentSnapshotStateSummary state in machine?.states ?? new List<AgentSnapshotStateSummary>())
                {
                    if (!string.IsNullOrEmpty(state?.stateAuthoringId))
                        targetOwnerIds.Add(state.stateAuthoringId);
                }
            }
            var specializedNodeIds = new HashSet<string>(
                (targetStateMachines ?? Array.Empty<AgentSnapshotStateMachineSummary>())
                    .SelectMany(machine => machine.states ?? new List<AgentSnapshotStateSummary>())
                    .SelectMany(state =>
                        (state.nestedStateMachines ?? new List<AgentSnapshotNestedStateMachineSummary>())
                            .Select(value => value.nodeAuthoringId)
                            .Concat((state.actionActivations ?? new List<AgentSnapshotActionActivationSummary>())
                                .Select(value => value.nodeAuthoringId))
                            .Concat((state.timelines ?? new List<AgentSnapshotTimelineBindingSummary>())
                                .Select(value => value.nodeAuthoringId))
                            .Concat((state.lifecycleTransitions ?? new List<AgentSnapshotLifecycleSummary>())
                                .Select(value => value.nodeAuthoringId)))
                    .Where(value => !string.IsNullOrEmpty(value)),
                StringComparer.Ordinal);
            IReadOnlyList<AgentSnapshotGraph> orderedTargetGraphs = (target ?? Array.Empty<AgentSnapshotGraph>())
                .OrderBy(graph => GraphDepth(graph, target, new HashSet<string>(StringComparer.Ordinal)))
                .ThenBy(graph => graph?.graphAuthoringId, StringComparer.Ordinal)
                .ToList();
            foreach (AgentSnapshotGraph graph in orderedTargetGraphs)
            {
                if (string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal))
                    continue;
                if (string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                {
                    if (!oldGraphs.TryGetValue(graph.graphAuthoringId, out AgentSnapshotGraph oldConditionGraph))
                    {
                        report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "condition_graph_identity_unknown", "Condition Rule Graph identity不在当前树中。");
                        continue;
                    }
                    BuildConditionRuleGraphMutations(oldConditionGraph, graph, mutations, report);
                    continue;
                }
                if (!oldGraphs.TryGetValue(graph.graphAuthoringId, out AgentSnapshotGraph oldGraph))
                {
                    if (IsLocal(graph.graphAuthoringId) &&
                        (string.Equals(graph.kind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal) ||
                         string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal) ||
                         string.Equals(graph.kind, AgentGraphKind.SubTree.ToString(), StringComparison.Ordinal) ||
                         string.Equals(graph.kind, AgentGraphKind.RunnableTree.ToString(), StringComparison.Ordinal) ||
                         string.Equals(graph.kind, AgentGraphKind.BaseTree.ToString(), StringComparison.Ordinal)))
                    {
                        oldGraph = AgentAuthoringDocumentCodec.Clone(graph);
                        oldGraph.nodes = new List<AgentSnapshotNode>();
                        oldGraph.flowEdges = new List<AgentSnapshotFlowEdge>();
                        oldGraph.propertyEdges = new List<AgentSnapshotPropertyEdge>();
                    }
                    else
                    {
                        report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "graph_create_unsupported", "裸Graph不能直接创建，必须由拥有它的StateMachine、State或正式节点创建。");
                        continue;
                    }
                }
                if (string.Equals(graph.kind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal))
                    BuildStateBehaviorGraphMutations(oldGraph, graph, specializedNodeIds, mutations, report);
                else
                    BuildGenericGraphMutations(oldGraph, graph, mutations, report);
            }
            foreach (AgentSnapshotGraph graph in current ?? Array.Empty<AgentSnapshotGraph>())
            {
                if (targetGraphs.ContainsKey(graph.graphAuthoringId) ||
                    string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                    continue;
                if (string.IsNullOrEmpty(graph.ownerElementAuthoringId))
                    continue;
                if (!string.IsNullOrEmpty(graph.ownerElementAuthoringId) &&
                    !targetOwnerIds.Contains(graph.ownerElementAuthoringId))
                    continue;
                report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "graph_delete_requires_owner", "Graph只能通过删除拥有它的State或节点级联删除。");
            }
            foreach (AgentSnapshotGraph graph in orderedTargetGraphs)
            {
                if (string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal) ||
                    string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                    continue;
                oldGraphs.TryGetValue(graph.graphAuthoringId, out AgentSnapshotGraph oldGraph);
                var oldNodes = (oldGraph?.nodes ?? new List<AgentSnapshotNode>())
                    .Where(value => value != null && !string.IsNullOrEmpty(value.elementAuthoringId))
                    .ToDictionary(value => value.elementAuthoringId, value => value, StringComparer.Ordinal);
                foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                {
                    oldNodes.TryGetValue(node.elementAuthoringId, out AgentSnapshotNode oldNode);
                    AddGraphReferenceMutations(
                        oldNode,
                        node,
                        graph,
                        mutations,
                        report,
                        $"document.editable.graphs[{Escape(graph.graphAuthoringId)}].nodes[{Escape(node.elementAuthoringId)}]");
                }
            }
        }

        static bool TryGetPortShapeSignature(
            AgentSnapshotNode node,
            AgentCompileReport report,
            string graphPath,
            out string signature)
        {
            signature = string.Empty;
            if (!s_Capabilities.TryProjectSnapshotPortShape(
                    node,
                    out _,
                    out IReadOnlyList<GraphAuthoringDynamicPortProjection> projected,
                    out GraphAuthoringPortShapeException error))
            {
                report.Error($"{graphPath}.nodes[{Escape(node.elementAuthoringId)}].properties", error.Code, error.Message);
                return false;
            }
            signature = string.Join("|", projected
                .Select(value => $"{value.PortId}:{value.Direction}:{value.Capacity}")
                .OrderBy(value => value, StringComparer.Ordinal));
            return true;
        }

        static AgentSnapshotGraph PreparePortShapeChangeEdgeDeletions(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            var currentNodes = Index(current.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var changedNodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentSnapshotNode targetNode in target.nodes ?? new List<AgentSnapshotNode>())
            {
                if (!currentNodes.TryGetValue(targetNode.elementAuthoringId, out AgentSnapshotNode currentNode))
                    continue;
                if (TryGetPortShapeSignature(currentNode, report, graphPath, out string currentShape) &&
                    TryGetPortShapeSignature(targetNode, report, graphPath, out string targetShape) &&
                    !string.Equals(currentShape, targetShape, StringComparison.Ordinal))
                    changedNodes.Add(targetNode.elementAuthoringId);
            }
            if (changedNodes.Count == 0)
                return current;

            var deletedFlow = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentSnapshotFlowEdge edge in current.flowEdges ?? new List<AgentSnapshotFlowEdge>())
            {
                if (!changedNodes.Contains(edge.startElementAuthoringId) &&
                    !changedNodes.Contains(edge.endElementAuthoringId))
                    continue;
                deletedFlow.Add(edge.elementAuthoringId);
                Add(mutations, $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeleteFlowEdge, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = edge.elementAuthoringId;
                });
            }
            var deletedProperty = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentSnapshotPropertyEdge edge in current.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
            {
                if (!changedNodes.Contains(edge.startElementAuthoringId) &&
                    !changedNodes.Contains(edge.endElementAuthoringId))
                    continue;
                deletedProperty.Add(edge.elementAuthoringId);
                Add(mutations, $"{graphPath}.propertyEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeletePropertyEdge, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = edge.elementAuthoringId;
                });
            }
            AgentSnapshotGraph baseline = AgentAuthoringDocumentCodec.Clone(current);
            baseline.flowEdges = (baseline.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                .Where(value => !deletedFlow.Contains(value.elementAuthoringId))
                .ToList();
            baseline.propertyEdges = (baseline.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                .Where(value => !deletedProperty.Contains(value.elementAuthoringId))
                .ToList();
            return baseline;
        }

        static void BuildPropertyEdgeMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            ISet<string> removedNodes = null)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            var oldEdges = Index(current.propertyEdges, value => value.elementAuthoringId, graphPath + ".propertyEdges", report);
            var newEdges = Index(target.propertyEdges, value => value.elementAuthoringId, graphPath + ".propertyEdges", report);
            foreach (AgentSnapshotPropertyEdge edge in target.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
            {
                string path = $"{graphPath}.propertyEdges[{Escape(edge.elementAuthoringId)}]";
                if (oldEdges.TryGetValue(edge.elementAuthoringId, out AgentSnapshotPropertyEdge oldEdge) && Same(oldEdge, edge))
                    continue;
                if (oldEdges.ContainsKey(edge.elementAuthoringId))
                {
                    Add(mutations, path, AgentMutationKind.DeletePropertyEdge, operation =>
                    {
                        SetGraph(operation, target);
                        operation.targetElementAuthoringId = edge.elementAuthoringId;
                    });
                }
                Add(mutations, path, AgentMutationKind.LinkProperty, operation =>
                {
                    SetGraph(operation, target);
                    SetLinkElement(operation, edge.startElementAuthoringId, true);
                    SetLinkElement(operation, edge.endElementAuthoringId, false);
                    operation.startPropertyPort = edge.startPortId;
                    operation.endPropertyPort = edge.endPortId;
                    operation.flowEdgeAuthoringId = edge.elementAuthoringId;
                });
            }
            foreach (AgentSnapshotPropertyEdge edge in current.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
            {
                if (newEdges.ContainsKey(edge.elementAuthoringId) ||
                    removedNodes?.Contains(edge.startElementAuthoringId) == true ||
                    removedNodes?.Contains(edge.endElementAuthoringId) == true)
                    continue;
                Add(mutations, $"{graphPath}.propertyEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeletePropertyEdge, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = edge.elementAuthoringId;
                });
            }
        }

        static void BuildConditionRuleGraphMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            if (!SameGraphMetadata(current, target))
            {
                report.Error(graphPath, "condition_graph_metadata_modified", "Condition Rule Graph元数据不能从底层镜像直接修改。");
                return;
            }
            var oldNodes = Index(current.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var newNodes = Index(target.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var removed = new HashSet<string>(oldNodes.Keys.Except(newNodes.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            AddIncidentEdgeDeletes(current, target, removed, mutations, graphPath);
            foreach (AgentSnapshotNode node in oldNodes.Values.Where(node => removed.Contains(node.elementAuthoringId)))
            {
                if (!s_Capabilities.TryGetKind(node.typeName, out string kind) ||
                    !IsInputNodeKind(kind) &&
                    !TryResolveConditionValueConfiguration(kind, out _) &&
                    !s_Capabilities.SupportsGenericMutation(node.typeName))
                {
                    report.Error(
                        $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]",
                        "condition_node_delete_unsupported",
                        "该Condition节点没有完整typed delete capability。");
                    continue;
                }
                Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.DeleteGraphNode, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                });
            }

            foreach (AgentSnapshotNode node in target.nodes ?? new List<AgentSnapshotNode>())
            {
                oldNodes.TryGetValue(node.elementAuthoringId, out AgentSnapshotNode oldNode);
                if (Same(oldNode, node))
                    continue;
                string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]";
                if (TryAddInputNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (TryAddConditionValueNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (oldNode == null)
                {
                    if (!ValidateGenericNodeChange(null, node, report, path))
                        continue;
                    Add(mutations, path, AgentMutationKind.EnsureGraphNode, operation =>
                    {
                        if (IsLocal(node.elementAuthoringId))
                            operation.id = LocalIdentity(node.elementAuthoringId);
                        SetGraph(operation, target);
                        SetOptionalExisting(operation, node.elementAuthoringId, false);
                        operation.nodeType = node.typeName;
                        operation.displayName = node.displayName;
                        operation.loopStopType = node.loopStopType;
                        operation.compareType = node.compareType;
                        SetMotionConfiguration(operation, node);
                        operation.position = ToVector(node.position);
                    });
                    continue;
                }
                report.Error(path, "condition_node_reconcile_unsupported", "该Condition节点变化没有对应的正式typed Mutation。");
            }
            AgentSnapshotGraph currentEdges = AgentAuthoringDocumentCodec.Clone(current);
            currentEdges.flowEdges = (currentEdges.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                .Where(edge => !removed.Contains(edge.startElementAuthoringId) && !removed.Contains(edge.endElementAuthoringId))
                .ToList();
            currentEdges.propertyEdges = (currentEdges.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                .Where(edge => !removed.Contains(edge.startElementAuthoringId) && !removed.Contains(edge.endElementAuthoringId))
                .ToList();
            BuildGraphEdgeMutations(new[] { currentEdges }, new[] { target }, mutations, report);
        }

        internal static void BuildGenericGraphMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            if (!SameGraphMetadata(current, target))
            {
                report.Error(graphPath, "graph_metadata_modified", "Graph kind、ownership与owner不能原地修改。");
                return;
            }

            AgentSnapshotGraph edgeBaseline = PreparePortShapeChangeEdgeDeletions(
                current,
                target,
                mutations,
                report);

            bool Ignored(AgentSnapshotNode node) =>
                node?.typeName?.EndsWith(".StateMachineNode", StringComparison.Ordinal) == true;
            var oldNodes = Index(
                (current.nodes ?? new List<AgentSnapshotNode>()).Where(node => !Ignored(node)),
                value => value.elementAuthoringId,
                graphPath + ".nodes",
                report);
            var newNodes = Index(
                (target.nodes ?? new List<AgentSnapshotNode>()).Where(node => !Ignored(node)),
                value => value.elementAuthoringId,
                graphPath + ".nodes",
                report);
            var removed = new HashSet<string>(oldNodes.Keys.Except(newNodes.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
                    AddIncidentEdgeDeletes(current, target, removed, mutations, graphPath);
                    foreach (AgentSnapshotNode node in oldNodes.Values.Where(node => removed.Contains(node.elementAuthoringId)))
            {
                Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.DeleteGraphNode, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                });
            }
            foreach (AgentSnapshotNode node in newNodes.Values)
            {
                if (oldNodes.TryGetValue(node.elementAuthoringId, out AgentSnapshotNode oldNode) && Same(oldNode, node))
                    continue;
                if (node.exposedProperty != null)
                {
                    AddExposedPropertyMutation(mutations, graphPath, target, node, report);
                    continue;
                }
                string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]";
                if (TryAddInputNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (TryAddConditionValueNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (!ValidateGenericNodeChange(oldNode, node, report, path))
                    continue;
                Add(mutations, path, AgentMutationKind.EnsureGraphNode, operation =>
                {
                    if (IsLocal(node.elementAuthoringId))
                        operation.id = LocalIdentity(node.elementAuthoringId);
                    SetGraph(operation, target);
                    SetOptionalExisting(operation, node.elementAuthoringId, false);
                    operation.nodeType = node.typeName;
                    operation.displayName = node.displayName;
                    operation.loopStopType = node.loopStopType;
                    operation.compareType = node.compareType;
                    SetMotionConfiguration(operation, node);
                    operation.position = ToVector(node.position);
                });
            }

            HashSet<string> ignoredIds = new HashSet<string>(
                (current.nodes ?? new List<AgentSnapshotNode>())
                    .Concat(target.nodes ?? new List<AgentSnapshotNode>())
                    .Where(Ignored)
                    .Select(node => node.elementAuthoringId),
                StringComparer.Ordinal);
            AgentSnapshotGraph Filter(AgentSnapshotGraph source, bool removeDeletedIncident)
            {
                AgentSnapshotGraph clone = AgentAuthoringDocumentCodec.Clone(source);
                clone.nodes = (clone.nodes ?? new List<AgentSnapshotNode>())
                    .Where(node => !ignoredIds.Contains(node.elementAuthoringId))
                    .ToList();
                clone.flowEdges = (clone.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                    .Where(edge =>
                        !ignoredIds.Contains(edge.startElementAuthoringId) &&
                        !ignoredIds.Contains(edge.endElementAuthoringId) &&
                        (!removeDeletedIncident ||
                         !removed.Contains(edge.startElementAuthoringId) &&
                         !removed.Contains(edge.endElementAuthoringId)))
                    .ToList();
                clone.propertyEdges = (clone.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                    .Where(edge =>
                        !ignoredIds.Contains(edge.startElementAuthoringId) &&
                        !ignoredIds.Contains(edge.endElementAuthoringId) &&
                        (!removeDeletedIncident ||
                         !removed.Contains(edge.startElementAuthoringId) &&
                         !removed.Contains(edge.endElementAuthoringId)))
                    .ToList();
                return clone;
            }
            BuildGraphEdgeMutations(
                new[] { Filter(edgeBaseline, true) },
                new[] { Filter(target, false) },
                mutations,
                report);
        }

        static bool SameTransitionExceptActionAdmission(
            AgentSnapshotTransitionSummary left,
            AgentSnapshotTransitionSummary right)
        {
            object Project(AgentSnapshotTransitionSummary transition) => new
            {
                transition.edgeAuthoringId,
                transition.fromElementAuthoringId,
                transition.toElementAuthoringId,
                transition.from,
                transition.to,
                transition.priority,
                transition.requests,
                conditionTerms = (transition.conditionTerms ?? new List<AgentSnapshotConditionTerm>())
                    .Select(term => new
                    {
                        term.kind,
                        term.negate,
                        term.request,
                        term.blackboardKey,
                        term.windowType,
                        term.targetSnapshotBlackboardKey,
                        term.compareType
                    })
                    .ToList()
            };
            return Same(Project(left), Project(right));
        }

        static bool SameTransitionEndpoints(
            AgentSnapshotTransitionSummary left,
            AgentSnapshotTransitionSummary right)
        {
            return string.Equals(
                       left?.fromElementAuthoringId,
                       right?.fromElementAuthoringId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       left?.toElementAuthoringId,
                       right?.toElementAuthoringId,
                       StringComparison.Ordinal);
        }

        static bool SameTransitionExceptEndpoints(
            AgentSnapshotTransitionSummary left,
            AgentSnapshotTransitionSummary right)
        {
            object Project(AgentSnapshotTransitionSummary transition) => new
            {
                transition.edgeAuthoringId,
                transition.priority,
                transition.requests,
                transition.conditionTerms
            };
            return Same(Project(left), Project(right));
        }

        static void BuildStateBehaviorGraphMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            ISet<string> specializedNodeIds,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            if (!SameGraphMetadata(current, target))
            {
                report.Error(graphPath, "state_behavior_metadata_modified", "State behavior Graph元数据不能从底层镜像直接修改。");
                return;
            }

            AgentSnapshotGraph edgeBaseline = PreparePortShapeChangeEdgeDeletions(
                current,
                target,
                mutations,
                report);

            var oldNodes = Index(current.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var newNodes = Index(target.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var removedNodes = new HashSet<string>(oldNodes.Keys.Except(newNodes.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            AddIncidentEdgeDeletes(current, target, removedNodes, mutations, graphPath);
            foreach (AgentSnapshotNode node in current.nodes ?? new List<AgentSnapshotNode>())
            {
                if (!removedNodes.Contains(node.elementAuthoringId))
                    continue;
                Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.DeleteStateBehaviorNode, operation =>
                {
                    SetTargetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                });
            }

            foreach (AgentSnapshotNode node in target.nodes ?? new List<AgentSnapshotNode>())
            {
                if (specializedNodeIds?.Contains(node.elementAuthoringId) == true)
                    continue;
                string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]";
                if (!oldNodes.TryGetValue(node.elementAuthoringId, out AgentSnapshotNode oldNode))
                {
                    if (node.exposedProperty != null)
                    {
                        AddExposedPropertyMutation(mutations, graphPath, target, node, report);
                        continue;
                    }
                    if (TryAddInputNodeMutation(null, node, target, mutations, report, path))
                        continue;
                    if (TryAddConditionValueNodeMutation(null, node, target, mutations, report, path))
                        continue;
                    if (!ValidateGenericNodeChange(null, node, report, path))
                        continue;
                    Add(mutations, path, AgentMutationKind.EnsureStateBehaviorNode, operation =>
                    {
                        operation.id = IsLocal(node.elementAuthoringId) ? LocalIdentity(node.elementAuthoringId) : null;
                        SetTargetGraph(operation, target);
                        operation.targetElementAuthoringId = IsLocal(node.elementAuthoringId) ? null : node.elementAuthoringId;
                        operation.nodeType = node.typeName;
                        operation.displayName = node.displayName;
                        operation.loopStopType = node.loopStopType;
                        operation.compareType = node.compareType;
                        SetMotionConfiguration(operation, node);
                        operation.position = ToVector(node.position);
                    });
                    continue;
                }
                if (Same(oldNode, node))
                    continue;
                if (node.exposedProperty != null)
                {
                    AddExposedPropertyMutation(mutations, graphPath, target, node, report);
                    continue;
                }
                if (TryAddInputNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (TryAddConditionValueNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (!ValidateGenericNodeChange(oldNode, node, report, path))
                    continue;
                Add(mutations, path, AgentMutationKind.EnsureStateBehaviorNode, operation =>
                {
                    SetTargetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                    operation.nodeType = node.typeName;
                    operation.displayName = node.displayName;
                    operation.loopStopType = node.loopStopType;
                    operation.compareType = node.compareType;
                    SetMotionConfiguration(operation, node);
                    operation.position = ToVector(node.position);
                });
            }

            var oldFlow = Index(edgeBaseline.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
            var newFlow = Index(target.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
            foreach (AgentSnapshotFlowEdge edge in target.flowEdges ?? new List<AgentSnapshotFlowEdge>())
            {
                string path = $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]";
                if (oldFlow.TryGetValue(edge.elementAuthoringId, out AgentSnapshotFlowEdge oldEdge) && SameFlowEdge(oldEdge, edge))
                    continue;
                if (oldFlow.ContainsKey(edge.elementAuthoringId))
                {
                    Add(mutations, path, AgentMutationKind.DeleteFlowEdge, operation =>
                    {
                        SetGraph(operation, target);
                        operation.targetElementAuthoringId = edge.elementAuthoringId;
                    });
                }
                Add(mutations, path, AgentMutationKind.LinkFlow, operation =>
                {
                    SetGraph(operation, target);
                    SetLinkElement(operation, edge.startElementAuthoringId, true);
                    SetLinkElement(operation, edge.endElementAuthoringId, false);
                    operation.startPort = edge.startPort;
                    operation.endPort = edge.endPort;
                    operation.flowEdgeAuthoringId = edge.elementAuthoringId;
                });
            }
            foreach (AgentSnapshotFlowEdge edge in edgeBaseline.flowEdges ?? new List<AgentSnapshotFlowEdge>())
            {
                if (newFlow.ContainsKey(edge.elementAuthoringId) ||
                    removedNodes.Contains(edge.startElementAuthoringId) ||
                    removedNodes.Contains(edge.endElementAuthoringId))
                    continue;
                Add(mutations, $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeleteFlowEdge, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = edge.elementAuthoringId;
                });
            }

            BuildPropertyEdgeMutations(edgeBaseline, target, mutations, report, removedNodes);
        }

                static bool ValidateGenericNodeChange(
            AgentSnapshotNode current,
            AgentSnapshotNode target,
            AgentCompileReport report,
            string path)
        {
            bool typedLocomotion = string.Equals(target?.typeName, typeof(LocomotionInputMotionNode).FullName, StringComparison.Ordinal);
            if (current == null)
            {
                bool graphReferencesValid = (target.graphReferences?.Count ?? 0) == 0 ||
                    s_Capabilities.TryGetKind(target.typeName, out string newKind) &&
                    s_Capabilities.CanEditProperty(newKind, "graphReferences");
                if (s_Capabilities.SupportsGenericMutation(target.typeName) &&
                    graphReferencesValid &&
                    ((target.assetReferences?.Count ?? 0) == 0 || typedLocomotion))
                    return true;
                report.Error(path, "authoring_capability_incomplete", "该Node没有完整typed create capability，或带有尚未闭合的Graph/Asset reference。");
                return false;
            }
            if (!string.Equals(current.typeName, target.typeName, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "node_kind_changed", "Node kind不能原地改变。");
                return false;
            }
            if (!typedLocomotion && !Same(current.assetReferences, target.assetReferences))
            {
                report.Error(path + ".properties", "authoring_capability_incomplete", "该Node的Asset reference发生变化，但没有对应的typed configure capability。");
                return false;
            }
            if (!Same(current.graphReferences, target.graphReferences) &&
                (!s_Capabilities.TryGetKind(target.typeName, out string kind) ||
                 !s_Capabilities.CanEditProperty(kind, "graphReferences")))
            {
                report.Error(path + ".properties.graphReferences", "graph_reference_capability_missing", "该Node没有登记Graph reference configure capability。");
                return false;
            }
            return true;
        }

                static void AddGraphReferenceMutations(
            AgentSnapshotNode current,
            AgentSnapshotNode target,
            AgentSnapshotGraph graph,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            var oldReferences = (current?.graphReferences ?? new List<AgentSnapshotGraphReference>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.key))
                .ToDictionary(value => value.key, value => value, StringComparer.Ordinal);
            var newReferences = (target?.graphReferences ?? new List<AgentSnapshotGraphReference>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.key))
                .ToDictionary(value => value.key, value => value, StringComparer.Ordinal);
            foreach (string key in oldReferences.Keys.Union(newReferences.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            {
                oldReferences.TryGetValue(key, out AgentSnapshotGraphReference oldReference);
                newReferences.TryGetValue(key, out AgentSnapshotGraphReference newReference);
                if (Same(oldReference, newReference))
                    continue;
                AgentSnapshotGraphReference reference = newReference ?? oldReference;
                string targetGraphIdentity = newReference?.graphAuthoringId ?? string.Empty;
                Add(mutations, path + $".graphReferences[{Escape(key)}]", AgentMutationKind.ConfigureGraphReference, operation =>
                {
                    SetGraph(operation, graph);
                    if (IsLocal(target.elementAuthoringId))
                        operation.graphOwnerElementPlannedIdentity = LocalIdentity(target.elementAuthoringId);
                    else
                        operation.graphOwnerElementAuthoringId = target.elementAuthoringId;
                    operation.graphReferenceKey = reference.key;
                    operation.graphOwnership = reference.ownership;
                    operation.graphReferenceSharedAssetPath = reference.sharedAssetPath;
                    if (IsLocal(targetGraphIdentity))
                        operation.graphReferenceGraphPlannedIdentity = LocalIdentity(targetGraphIdentity);
                    else
                        operation.graphReferenceGraphAuthoringId = targetGraphIdentity;
                    operation.graphReferenceInputBindings = AgentAuthoringDocumentCodec.Clone(reference.inputBindings) ?? new List<AgentSnapshotGraphParameterBinding>();
                    operation.graphReferenceOutputBindings = AgentAuthoringDocumentCodec.Clone(reference.outputBindings) ?? new List<AgentSnapshotGraphParameterBinding>();
                });
                    }
                }

                static void AddGraphReferenceOwnershipMutation(
                    string path,
                    AgentSnapshotGraph ownerGraph,
                    AgentSnapshotNode ownerNode,
                    AgentSnapshotGraph childGraph,
                    AgentMutationDraftSet mutations,
                    AgentCompileReport report)
                {
                    if (ownerGraph == null || ownerNode == null || childGraph == null || string.IsNullOrEmpty(childGraph.referenceKey))
                    {
                        report.Error(path, "graph_reference_owner_invalid", "Graph ownership变化缺少正式owner Graph、Node或referenceKey。");
                        return;
                    }
                    Add(mutations, path, AgentMutationKind.ConfigureGraphReference, operation =>
                    {
                        SetGraph(operation, ownerGraph.graphAuthoringId);
                        if (IsLocal(ownerNode.elementAuthoringId))
                            operation.graphOwnerElementPlannedIdentity = LocalIdentity(ownerNode.elementAuthoringId);
                        else
                            operation.graphOwnerElementAuthoringId = ownerNode.elementAuthoringId;
                        operation.graphReferenceKey = childGraph.referenceKey;
                        operation.graphOwnership = childGraph.ownership;
                        operation.graphReferenceGraphAuthoringId = IsLocal(childGraph.graphAuthoringId) ? string.Empty : childGraph.graphAuthoringId;
                        operation.graphReferenceGraphPlannedIdentity = IsLocal(childGraph.graphAuthoringId) ? LocalIdentity(childGraph.graphAuthoringId) : string.Empty;
                        operation.graphReferenceSharedAssetPath = childGraph.sharedAssetPath;
                    });
                }

                static void AddIncidentEdgeDeletes(
                    AgentSnapshotGraph current,
                    AgentSnapshotGraph target,
                    ISet<string> removedNodes,
                    AgentMutationDraftSet mutations,
                    string graphPath)
                {
                    foreach (AgentSnapshotFlowEdge edge in current?.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                    {
                        if (!removedNodes.Contains(edge.startElementAuthoringId) &&
                            !removedNodes.Contains(edge.endElementAuthoringId))
                            continue;
                        Add(mutations, $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeleteFlowEdge, operation =>
                        {
                            SetGraph(operation, target);
                            operation.targetElementAuthoringId = edge.elementAuthoringId;
                        });
                    }
                    foreach (AgentSnapshotPropertyEdge edge in current?.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                    {
                        if (!removedNodes.Contains(edge.startElementAuthoringId) &&
                            !removedNodes.Contains(edge.endElementAuthoringId))
                            continue;
                        Add(mutations, $"{graphPath}.propertyEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeletePropertyEdge, operation =>
                        {
                            SetGraph(operation, target);
                            operation.targetElementAuthoringId = edge.elementAuthoringId;
                        });
                    }
                }

                static bool TryAddInputNodeMutation(
            AgentSnapshotNode current,
            AgentSnapshotNode target,
            AgentSnapshotGraph graph,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (!s_Capabilities.TryGetKind(target.typeName, out string kind))
                return false;
            bool request = string.Equals(kind, "character-action-request", StringComparison.Ordinal);
            if (!IsInputNodeKind(kind))
                return false;
            if (current != null && !string.Equals(current.typeName, target.typeName, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "node_kind_changed", "Node kind不能原地改变。");
                return true;
            }
            if (current != null &&
                (!Same(current.graphReferences, target.graphReferences) ||
                 !Same(current.assetReferences, target.assetReferences)))
            {
                report.Error(path + ".properties", "input_node_reference_modified", "Input Node不能携带Graph或Asset reference变化。");
                return true;
            }
            string binding = request ? target.requestId : target.inputId;
            if (string.IsNullOrWhiteSpace(binding))
            {
                report.Error(path + ".properties", "input_binding_required", $"{kind}必须声明非空{(request ? "requestId" : "inputId")}。");
                return true;
            }
            Add(mutations, path, AgentMutationKind.EnsureInputNode, operation =>
            {
                if (IsLocal(target.elementAuthoringId))
                    operation.id = LocalIdentity(target.elementAuthoringId);
                SetGraph(operation, graph);
                SetOptionalExisting(operation, target.elementAuthoringId, false);
                operation.nodeType = target.typeName;
                operation.displayName = target.displayName;
                operation.inputId = binding;
                operation.position = ToVector(target.position);
            });
            return true;
        }

        static bool IsInputNodeKind(string kind)
        {
            return string.Equals(kind, "character-action-request", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-bool", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-float", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-vector2", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-vector2-magnitude", StringComparison.Ordinal);
        }

        static bool TryAddConditionValueNodeMutation(
            AgentSnapshotNode current,
            AgentSnapshotNode target,
            AgentSnapshotGraph graph,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (!s_Capabilities.TryGetKind(target.typeName, out string kind) ||
                !TryResolveConditionValueConfiguration(kind, out AgentConditionValueNodeConfigurationKind configuration))
                return false;
            if (current != null && !string.Equals(current.typeName, target.typeName, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "node_kind_changed", "Node kind不能原地改变。");
                return true;
            }
            if (current != null &&
                (!Same(current.graphReferences, target.graphReferences) ||
                 !Same(current.assetReferences, target.assetReferences)))
            {
                report.Error(path + ".properties", "condition_value_reference_modified", "Condition Value Node不能携带未登记的Graph或Asset reference变化。");
                return true;
            }
            Add(mutations, path, AgentMutationKind.EnsureConditionValueNode, operation =>
            {
                if (IsLocal(target.elementAuthoringId))
                    operation.id = LocalIdentity(target.elementAuthoringId);
                SetGraph(operation, graph);
                SetOptionalExisting(operation, target.elementAuthoringId, false);
                operation.nodeType = target.typeName;
                operation.displayName = target.displayName;
                operation.conditionValueConfiguration = configuration.ToString();
                operation.position = ToVector(target.position);
                switch (configuration)
                {
                    case AgentConditionValueNodeConfigurationKind.BlackboardDeclaration:
                        SetDeclarationReference(operation, target.blackboardDeclarationId, false);
                        break;
                    case AgentConditionValueNodeConfigurationKind.StateExitCause:
                        operation.stateExitCause = target.stateExitCause;
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionContext:
                        operation.actionContext = target.actionContextId;
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionWindow:
                        operation.windowType = target.windowType;
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionAdmission:
                        operation.actionProfile = target.actionProfileId;
                        SetTargetSnapshotDeclarationReference(operation, target.targetSnapshotBlackboardDeclarationId);
                        break;
                }
            });
            return true;
        }

        static bool TryResolveConditionValueConfiguration(
            string kind,
            out AgentConditionValueNodeConfigurationKind configuration)
        {
            configuration = kind switch
            {
                "character-move-facing-angle" => AgentConditionValueNodeConfigurationKind.None,
                "pipeline-blackboard-bool" => AgentConditionValueNodeConfigurationKind.BlackboardDeclaration,
                "pipeline-blackboard-float" => AgentConditionValueNodeConfigurationKind.BlackboardDeclaration,
                "state-exit-cause" => AgentConditionValueNodeConfigurationKind.StateExitCause,
                "action-context-active" => AgentConditionValueNodeConfigurationKind.ActionContext,
                "action-window-active" => AgentConditionValueNodeConfigurationKind.ActionWindow,
                "can-activate-action" => AgentConditionValueNodeConfigurationKind.ActionAdmission,
                _ => AgentConditionValueNodeConfigurationKind.None
            };
            return string.Equals(kind, "character-move-facing-angle", StringComparison.Ordinal) ||
                   string.Equals(kind, "pipeline-blackboard-bool", StringComparison.Ordinal) ||
                   string.Equals(kind, "pipeline-blackboard-float", StringComparison.Ordinal) ||
                   string.Equals(kind, "state-exit-cause", StringComparison.Ordinal) ||
                   string.Equals(kind, "action-context-active", StringComparison.Ordinal) ||
                   string.Equals(kind, "action-window-active", StringComparison.Ordinal) ||
                   string.Equals(kind, "can-activate-action", StringComparison.Ordinal);
        }

        static bool SameGraphMetadata(AgentSnapshotGraph left, AgentSnapshotGraph right)
        {
            return Same(
                new
                {
                    left.graphAuthoringId,
                    left.path,
                    left.name,
                    left.kind,
                    left.ownership,
                    left.ownerElementAuthoringId,
                    left.referenceKey,
                    left.sharedAssetPath,
                    left.routes
                },
                new
                {
                    right.graphAuthoringId,
                    right.path,
                    right.name,
                    right.kind,
                    right.ownership,
                    right.ownerElementAuthoringId,
                    right.referenceKey,
                    right.sharedAssetPath,
                    right.routes
                });
        }

        static List<AgentConditionGroup> ToConditionGroups(IReadOnlyList<AgentSnapshotConditionTerm> terms)
        {
            var group = new AgentConditionGroup();
            foreach (AgentSnapshotConditionTerm term in terms ?? Array.Empty<AgentSnapshotConditionTerm>())
            {
                group.terms.Add(new AgentConditionTerm
                {
                    kind = term.kind,
                    negate = term.negate,
                    request = term.request,
                    blackboardKey = term.blackboardKey,
                    windowType = term.windowType,
                    actionProfile = term.actionProfile,
                    actionProfileAssetPath = term.actionProfileAssetPath,
                    actionProfileAssetGuid = term.actionProfileAssetGuid,
                    targetSnapshotBlackboardKey = term.targetSnapshotBlackboardKey,
                    compareType = term.compareType
                });
            }
            return new List<AgentConditionGroup> { group };
        }

        static bool TryFindGraphOwner(
            IReadOnlyList<AgentSnapshotGraph> graphs,
            string childGraphIdentity,
            out AgentSnapshotGraph parent,
            out AgentSnapshotNode owner)
        {
            parent = null;
            owner = null;
            AgentSnapshotGraph child = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, childGraphIdentity, StringComparison.Ordinal));
            if (child == null || string.IsNullOrEmpty(child.ownerElementAuthoringId))
                return false;
            foreach (AgentSnapshotGraph candidate in graphs ?? Array.Empty<AgentSnapshotGraph>())
            {
                AgentSnapshotNode node = candidate.nodes?.FirstOrDefault(value =>
                    string.Equals(value.elementAuthoringId, child.ownerElementAuthoringId, StringComparison.Ordinal));
                if (node == null)
                    continue;
                parent = candidate;
                owner = node;
                return true;
            }
            return false;
        }

        static int GraphDepth(
            AgentSnapshotGraph graph,
            IReadOnlyList<AgentSnapshotGraph> graphs,
            ISet<string> visiting)
        {
            if (graph == null || string.IsNullOrEmpty(graph.ownerElementAuthoringId) || !visiting.Add(graph.graphAuthoringId))
                return 0;
            AgentSnapshotGraph parent = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .FirstOrDefault(candidate => candidate?.nodes?.Any(node =>
                    string.Equals(node?.elementAuthoringId, graph.ownerElementAuthoringId, StringComparison.Ordinal)) == true);
            return parent == null ? 0 : 1 + GraphDepth(parent, graphs, visiting);
        }

        static void AddExposedPropertyMutation(
            AgentMutationDraftSet mutations,
            string graphPath,
            AgentSnapshotGraph graph,
            AgentSnapshotNode node,
            AgentCompileReport report)
        {
            AgentSnapshotExposedProperty exposed = node.exposedProperty;
            string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}].exposedProperty";
            if (!Enum.TryParse(exposed.mode, false, out ExposedPropertyNodeType mode) ||
                string.IsNullOrWhiteSpace(exposed.valueType) ||
                mode == ExposedPropertyNodeType.Set &&
                (exposed.value == null || exposed.value.Type == Newtonsoft.Json.Linq.JTokenType.Null))
            {
                report.Error(
                    path,
                    "exposed_property_mutation_invalid",
                    "ExposedProperty Mutation 必须声明有效 mode、valueType，Set 还必须声明 value。");
                return;
            }
            Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.EnsureExposedPropertyNode, operation =>
            {
                if (IsLocal(node.elementAuthoringId))
                    operation.id = LocalIdentity(node.elementAuthoringId);
                SetTargetGraph(operation, graph);
                SetOptionalExisting(operation, node.elementAuthoringId, false);
                if (IsLocal(exposed.declarationAuthoringId))
                    operation.declarationPlannedIdentity = LocalIdentity(exposed.declarationAuthoringId);
                else
                    operation.declarationAuthoringId = exposed.declarationAuthoringId;
                operation.exposedPropertyMode = mode.ToString();
                operation.blackboardValueType = exposed.valueType;
                operation.blackboardDefaultValue = exposed.value?.DeepClone();
                operation.displayName = node.displayName;
                operation.position = ToVector(node.position);
            });
        }
    }
}
