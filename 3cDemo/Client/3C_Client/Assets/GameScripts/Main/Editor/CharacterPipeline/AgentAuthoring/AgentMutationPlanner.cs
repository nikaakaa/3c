using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMutationPlanner
    {
        public bool TryCreatePlan(AgentMutationDraftSet drafts, AgentCompileReport report, out AgentMutationPlan plan)
        {
            plan = null;
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (drafts == null)
            {
                report.Error("document.editable", "mutation_draft_missing", "Reconciler内部Mutation Draft缺失。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (!string.Equals(drafts.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                report.Error(
                    "document.schemaVersion",
                    "unsupported_schema_version",
                    $"Mutation Draft schema必须是{AgentAuthoringSchema.Version}，当前为{drafts.schemaVersion}。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            report.domain = drafts.domain ?? string.Empty;
            report.rootIdentity = drafts.rootIdentity ?? string.Empty;
            if (!AgentAuthoringSchema.IsDomain(drafts.domain))
            {
                report.Error("document.domain", "unsupported_domain", $"Mutation domain无效：{drafts.domain}");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (string.IsNullOrWhiteSpace(drafts.rootIdentity) || string.IsNullOrWhiteSpace(drafts.sourceRevision))
            {
                report.Error("document", "document_source_identity_missing", "Mutation Draft缺少rootIdentity或sourceRevision。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (drafts.mutations == null)
            {
                report.Error("document.editable", "editable_missing", "Document editable正文缺失。");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            var commands = new List<AgentMutation>(drafts.mutations.Count);
            var plannedIdentities = new Dictionary<string, AgentPlannedIdentitySymbol>(StringComparer.Ordinal);
            var mutationIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < drafts.mutations.Count; i++)
            {
                AgentMutationDraft operation = drafts.mutations[i];
                if (operation == null || string.IsNullOrWhiteSpace(operation.id) ||
                    !operation.id.StartsWith("local:", StringComparison.Ordinal) ||
                    !AgentTypedMutationLoweringCatalog.TryGet(operation.kind, out AgentMutationDraftDescriptor descriptor) ||
                    descriptor.OutputKind == AgentMutationOutputKind.None ||
                    plannedIdentities.ContainsKey(operation.id))
                    continue;
                plannedIdentities.Add(
                    operation.id,
                    new AgentPlannedIdentitySymbol(operation.id, descriptor.OutputKind, DraftOwnerScope(operation)));
            }
            for (int i = 0; i < drafts.mutations.Count; i++)
            {
                AgentMutationDraft operation = drafts.mutations[i];
                string path = string.IsNullOrEmpty(operation?.sourcePath)
                    ? $"document.mutations[{i}]"
                    : operation.sourcePath;
                if (operation == null)
                {
                    report.Error(path, "mutation_missing", "内部Mutation Draft为空。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(operation.id))
                {
                    report.Error(path, "mutation_id_missing", "每个内部Mutation必须使用唯一id。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!mutationIds.Add(operation.id))
                {
                    report.Error(path, "mutation_id_duplicate", $"Mutation id重复：{operation.id}");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!AgentTypedMutationLoweringCatalog.TryGet(operation.kind, out AgentMutationDraftDescriptor descriptor))
                {
                    report.Error(path, "unknown_mutation", $"内部Mutation kind没有typed lowering：{operation.kind}");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!descriptor.Allows(drafts.domain))
                {
                    report.Error(path, "mutation_domain_mismatch", $"Mutation '{operation.kind}'不允许用于{drafts.domain} domain。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }

                var context = new AgentMutationPlanningContext(report, path, operation.id, plannedIdentities);
                AgentMutation command = descriptor.Lower(context, operation);
                if (command == null || context.HasErrors)
                {
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                context.ValidateOwnedReferences(command);
                if (context.HasErrors)
                {
                    report.metrics.schemaInvalidCount++;
                    continue;
                }

                commands.Add(command);
                plannedIdentities[operation.id] = new AgentPlannedIdentitySymbol(operation.id, descriptor.OutputKind, command.OwnerScope);
                report.metrics.schemaValidCount++;
            }

            if (report.HasErrors())
                return false;

            if (!TryOrderCommands(commands, report, out IReadOnlyList<AgentMutation> orderedCommands))
                return false;
            plan = new AgentMutationPlan(orderedCommands.ToList(), drafts.domain, drafts.rootIdentity, drafts.sourceRevision);
            return true;
        }

        static string DraftOwnerScope(AgentMutationDraft operation)
        {
            return FirstIdentity(
                operation.graphPlannedIdentity,
                operation.graphAuthoringId,
                operation.targetGraphPlannedIdentity,
                operation.targetGraphAuthoringId,
                operation.stateMachinePlannedIdentity,
                operation.stateMachineGraphAuthoringId);
        }

        static string FirstIdentity(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                    return AgentPlannedIdentityReference.Parse(values[i]).Identity;
            }
            return string.Empty;
        }

        static bool TryOrderCommands(
            IReadOnlyList<AgentMutation> commands,
            AgentCompileReport report,
            out IReadOnlyList<AgentMutation> ordered)
        {
            var producers = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].Id.StartsWith("local:", StringComparison.Ordinal))
                    producers[commands[i].Id] = i;
            }
            var dependencies = new List<HashSet<int>>(commands.Count);
            var outgoing = new List<List<int>>(commands.Count);
            var indegree = new int[commands.Count];
            for (int i = 0; i < commands.Count; i++)
            {
                dependencies.Add(new HashSet<int>());
                outgoing.Add(new List<int>());
            }
            bool hasMissingDependency = false;
            for (int i = 0; i < commands.Count; i++)
            {
                foreach (string dependency in CommandDependencies(commands[i]))
                {
                    if (!producers.TryGetValue(dependency, out int producer))
                    {
                        report.Error(
                            commands[i].Path,
                            "mutation_planned_dependency_unresolved",
                            $"Mutation planned identity没有对应的producer：{dependency}");
                        hasMissingDependency = true;
                        continue;
                    }
                    if (!dependencies[i].Add(producer))
                        continue;
                    outgoing[producer].Add(i);
                    indegree[i]++;
                }
            }
            if (hasMissingDependency)
            {
                ordered = commands;
                return false;
            }
            var ready = new SortedSet<int>();
            for (int i = 0; i < indegree.Length; i++)
            {
                if (indegree[i] == 0)
                    ready.Add(i);
            }
            var result = new List<AgentMutation>(commands.Count);
            while (ready.Count > 0)
            {
                int index = ready.Min;
                ready.Remove(index);
                result.Add(commands[index]);
                foreach (int dependent in outgoing[index])
                {
                    indegree[dependent]--;
                    if (indegree[dependent] == 0)
                        ready.Add(dependent);
                }
            }
            if (result.Count == commands.Count)
            {
                ordered = result;
                return true;
            }
            report.Error("document.mutations", "mutation_dependency_cycle", "Mutation planned identity依赖存在循环，无法形成有序事务计划。");
            ordered = commands;
            return false;
        }

        static IEnumerable<string> CommandDependencies(AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureGraphMutation graph:
                    foreach (string dependency in Planned(graph.ParentGraph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(graph.OwnerNode.Value))
                        yield return dependency;
                    break;
                case AgentConfigureGraphReferenceMutation reference:
                    foreach (string dependency in Planned(reference.OwnerGraph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(reference.OwnerNode.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(reference.ChildGraph.Value))
                        yield return dependency;
                    break;
                case AgentEnsureGraphNodeMutation node:
                    foreach (string dependency in Planned(node.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(node.Existing.Value))
                        yield return dependency;
                    break;
                case AgentEnsureInputNodeMutation input:
                    foreach (string dependency in Planned(input.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(input.ExistingElement.Value))
                        yield return dependency;
                    break;
                case AgentEnsureConditionValueNodeMutation condition:
                    foreach (string dependency in Planned(condition.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(condition.ExistingElement.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(condition.BlackboardDeclaration))
                        yield return dependency;
                    foreach (string dependency in Planned(condition.TargetSnapshotDeclaration))
                        yield return dependency;
                    break;
                case AgentGraphLinkMutation link:
                    foreach (string dependency in Planned(link.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(link.Source.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(link.Target.Value))
                        yield return dependency;
                    break;
                case AgentDeleteGraphNodeMutation deleteNode:
                    foreach (string dependency in Planned(deleteNode.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(deleteNode.Element.Value))
                        yield return dependency;
                    break;
                case AgentDeleteFlowEdgeMutation deleteFlow:
                    foreach (string dependency in Planned(deleteFlow.Graph.Value))
                        yield return dependency;
                    break;
                case AgentDeletePropertyEdgeMutation deleteProperty:
                    foreach (string dependency in Planned(deleteProperty.Graph.Value))
                        yield return dependency;
                    break;
                case AgentEnsureBlackboardDeclarationMutation declaration:
                    foreach (string dependency in Planned(declaration.Graph.Value))
                        yield return dependency;
                    break;
                case AgentDeleteBlackboardDeclarationMutation deleteDeclaration:
                    foreach (string dependency in Planned(deleteDeclaration.Graph.Value))
                        yield return dependency;
                    break;
                case AgentSetBlackboardSchemaRevisionMutation revision:
                    foreach (string dependency in Planned(revision.Graph.Value))
                        yield return dependency;
                    break;
                case AgentMoveBlackboardDeclarationMutation moveDeclaration:
                    foreach (string dependency in Planned(moveDeclaration.SourceGraph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(moveDeclaration.TargetGraph.Value))
                        yield return dependency;
                    break;
                case AgentEnsureExposedPropertyNodeMutation exposed:
                    foreach (string dependency in Planned(exposed.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(exposed.Declaration))
                        yield return dependency;
                    break;
                case AgentEnsureStateMachineMutation stateMachine:
                    foreach (string dependency in Planned(stateMachine.ParentGraph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(stateMachine.ExistingOwner.Value))
                        yield return dependency;
                    break;
                case AgentEnsureStateMutation state:
                    foreach (string dependency in Planned(state.StateMachine.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(state.ExistingState.Value))
                        yield return dependency;
                    break;
                case AgentDeleteStateMutation deleteState:
                    foreach (string dependency in Planned(deleteState.StateMachine.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(deleteState.State.Value))
                        yield return dependency;
                    break;
                case AgentEnsureTransitionMutation transition:
                    foreach (string dependency in Planned(transition.StateMachine.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(transition.From.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(transition.To.Value))
                        yield return dependency;
                    break;
                case AgentRewireTransitionMutation rewire:
                    foreach (string dependency in Planned(rewire.StateMachine.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(rewire.From.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(rewire.To.Value))
                        yield return dependency;
                    break;
                case AgentConfigureActionAdmissionMutation admission:
                    foreach (string dependency in Planned(admission.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(admission.Element.Value))
                        yield return dependency;
                    break;
                case AgentStateBehaviorMutation behavior:
                    foreach (string dependency in Planned(behavior.Target.DirectGraph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(behavior.Target.StateMachine.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(behavior.Target.State.Value))
                        yield return dependency;
                    switch (behavior)
                    {
                        case AgentEnsureActionExitLifecycleMutation exit:
                            foreach (string dependency in Planned(exit.Source.Value))
                                yield return dependency;
                            foreach (string dependency in Planned(exit.ExistingElement.Value))
                                yield return dependency;
                            break;
                        case AgentDeleteStateBehaviorNodeMutation delete:
                            foreach (string dependency in Planned(delete.Element.Value))
                                yield return dependency;
                            break;
                        case AgentEnsureStateBehaviorNodeMutation node:
                            foreach (string dependency in Planned(node.ExistingElement.Value))
                                yield return dependency;
                            break;
                        case AgentEnsureTimelineNodeMutation timelineNode:
                            foreach (string dependency in Planned(timelineNode.ExistingElement.Value))
                                yield return dependency;
                            break;
                        case AgentEnsureActionActivationMutation activation:
                            foreach (string dependency in Planned(activation.ExistingElement.Value))
                                yield return dependency;
                            break;
                        case AgentEnsureActionLifecycleTransitionMutation lifecycle:
                            foreach (string dependency in Planned(lifecycle.ExistingElement.Value))
                                yield return dependency;
                            break;
                    }
                    break;
                case AgentEnsureInlineTimelineMutation inlineTimeline:
                    foreach (string dependency in Planned(inlineTimeline.TimelineNode.Value))
                        yield return dependency;
                    break;
                case AgentEnsureTimelineTreeClipMutation treeClip:
                    foreach (string dependency in Planned(treeClip.Target.TimelinePlannedIdentity))
                        yield return dependency;
                    foreach (string dependency in Planned(treeClip.Target.TrackPlannedIdentity))
                        yield return dependency;
                    foreach (string dependency in Planned(treeClip.Target.ClipPlannedIdentity))
                        yield return dependency;
                    break;
                case AgentEnsureMotionCurveTrackMutation curveTrack:
                    foreach (string dependency in Planned(curveTrack.Target.TimelinePlannedIdentity))
                        yield return dependency;
                    foreach (string dependency in Planned(curveTrack.Target.TrackPlannedIdentity))
                        yield return dependency;
                    break;
                case AgentTimelineClipMutation timelineClip:
                    foreach (string dependency in Planned(timelineClip.Target.TimelinePlannedIdentity))
                        yield return dependency;
                    foreach (string dependency in Planned(timelineClip.Target.TrackPlannedIdentity))
                        yield return dependency;
                    foreach (string dependency in Planned(timelineClip.Target.ClipPlannedIdentity))
                        yield return dependency;
                    break;
                case AgentAnimationTrackMutation animationTrack:
                    foreach (string dependency in Planned(animationTrack.Target.TimelinePlannedIdentity))
                        yield return dependency;
                    foreach (string dependency in Planned(animationTrack.Target.TrackPlannedIdentity))
                        yield return dependency;
                    break;
                case AgentEnsureTimelineSectionMutation section:
                    foreach (string dependency in Planned(section.Target.TimelinePlannedIdentity))
                        yield return dependency;
                    break;
                case AgentDeleteTimelineSectionMutation deleteSection:
                    foreach (string dependency in Planned(deleteSection.Target.TimelinePlannedIdentity))
                        yield return dependency;
                    break;
                case AgentEnsureAIObservationNodeMutation aiObservation:
                    foreach (string dependency in Planned(aiObservation.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiObservation.ExistingNode.Value))
                        yield return dependency;
                    break;
                case AgentEnsureAISharedNodeMutation aiShared:
                    foreach (string dependency in Planned(aiShared.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiShared.ExistingNode.Value))
                        yield return dependency;
                    break;
                case AgentEnsureAIMemoryNodeMutation aiMemory:
                    foreach (string dependency in Planned(aiMemory.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiMemory.ExistingNode.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiMemory.Declaration))
                        yield return dependency;
                    break;
                case AgentEnsureAIContinuousInputMutation aiInput:
                    foreach (string dependency in Planned(aiInput.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiInput.ExistingNode.Value))
                        yield return dependency;
                    break;
                case AgentEnsureAIActionTargetMutation aiTarget:
                    foreach (string dependency in Planned(aiTarget.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiTarget.ExistingNode.Value))
                        yield return dependency;
                    break;
                case AgentEnsureAIActionRequestMutation aiRequest:
                    foreach (string dependency in Planned(aiRequest.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiRequest.ExistingNode.Value))
                        yield return dependency;
                    break;
                case AgentEnsureAIBlackboardDeclarationMutation aiDeclaration:
                    foreach (string dependency in Planned(aiDeclaration.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(aiDeclaration.ExistingDeclaration.Value))
                        yield return dependency;
                    break;
                case AgentEnsureBTConditionRuleMutation btCondition:
                    foreach (string dependency in Planned(btCondition.Graph.Value))
                        yield return dependency;
                    foreach (string dependency in Planned(btCondition.Edge.Value))
                        yield return dependency;
                    break;
            }
        }

        static IEnumerable<string> Planned(AgentAuthoringReference reference)
        {
            if (reference.PlannedIdentity.IsValid)
                yield return reference.PlannedIdentity.Identity;
        }

        static IEnumerable<string> Planned(AgentGraphTargetReference reference) => Planned(reference.Value);
        static IEnumerable<string> Planned(AgentStateMachineTargetReference reference) => Planned(reference.Value);
        static IEnumerable<string> Planned(AgentStateTargetReference reference) => Planned(reference.Value);
        static IEnumerable<string> Planned(AgentElementTargetReference reference) => Planned(reference.Value);
        static IEnumerable<string> Planned(AgentPlannedIdentityReference reference)
        {
            if (reference.IsValid)
                yield return reference.Identity;
        }
    }

    public static class AgentTypedMutationLoweringCatalog
    {
        static readonly Dictionary<AgentMutationKind, AgentMutationDraftDescriptor> s_Descriptors =
            new Dictionary<AgentMutationKind, AgentMutationDraftDescriptor>()
            {
                [AgentMutationKind.EnsureStateMachine] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureStateMachine, AgentMutationOutputKind.StateMachine, AgentStateMachineMutationLowering.LowerEnsureStateMachine),
                [AgentMutationKind.EnsureState] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureState, AgentMutationOutputKind.State, AgentStateMachineMutationLowering.LowerEnsureState),
                [AgentMutationKind.DeleteState] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteState, AgentMutationOutputKind.None, AgentStateMachineMutationLowering.LowerDeleteState),
                [AgentMutationKind.EnsureTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTransition, AgentMutationOutputKind.Transition, AgentStateMachineMutationLowering.LowerEnsureTransition),
                [AgentMutationKind.RewireTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.RewireTransition, AgentMutationOutputKind.None, AgentStateMachineMutationLowering.LowerRewireTransition),
                [AgentMutationKind.EnsureConditionRule] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureConditionRule, AgentMutationOutputKind.Transition, AgentStateMachineMutationLowering.LowerEnsureConditionRule),
                [AgentMutationKind.EnsureActionExitLifecycle] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureActionExitLifecycle, AgentMutationOutputKind.Node, AgentStateBehaviorMutationLowering.LowerEnsureActionExitLifecycle),
                [AgentMutationKind.DeleteStateBehaviorNode] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteStateBehaviorNode, AgentMutationOutputKind.None, AgentStateBehaviorMutationLowering.LowerDeleteStateBehaviorNode),
                [AgentMutationKind.EnsureStateBehaviorNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureStateBehaviorNode, AgentMutationOutputKind.Node, AgentStateBehaviorMutationLowering.LowerEnsureStateBehaviorNode),
                [AgentMutationKind.EnsureTimelineNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTimelineNode, AgentMutationOutputKind.Node, AgentTimelineMutationLowering.LowerEnsureTimelineNode),
                [AgentMutationKind.EnsureInlineTimeline] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureInlineTimeline, AgentMutationOutputKind.Timeline, AgentTimelineMutationLowering.LowerEnsureInlineTimeline),
                [AgentMutationKind.EnsureActionActivation] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureActionActivation, AgentMutationOutputKind.Node, AgentStateBehaviorMutationLowering.LowerEnsureActionActivation),
                [AgentMutationKind.EnsureActionLifecycleTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureActionLifecycleTransition, AgentMutationOutputKind.Node, AgentStateBehaviorMutationLowering.LowerEnsureActionLifecycleTransition),
                [AgentMutationKind.EnsureInputNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureInputNode, AgentMutationOutputKind.Node, AgentGraphMutationLowering.LowerEnsureInputNode),
                [AgentMutationKind.EnsureConditionValueNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureConditionValueNode, AgentMutationOutputKind.Node, AgentGraphMutationLowering.LowerEnsureConditionValueNode),
                [AgentMutationKind.ConfigureActionAdmission] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureActionAdmission, AgentMutationOutputKind.None, AgentActionMutationLowering.LowerConfigureActionAdmission),
                [AgentMutationKind.EnsureBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureBlackboardDeclaration, AgentMutationOutputKind.BlackboardDeclaration, AgentBlackboardMutationLowering.LowerEnsureBlackboardDeclaration),
                [AgentMutationKind.MoveBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.MoveBlackboardDeclaration, AgentMutationOutputKind.BlackboardDeclaration, AgentBlackboardMutationLowering.LowerMoveBlackboardDeclaration),
                [AgentMutationKind.DeleteBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteBlackboardDeclaration, AgentMutationOutputKind.None, AgentBlackboardMutationLowering.LowerDeleteBlackboardDeclaration, AgentMutationDomainMask.AIController),
                [AgentMutationKind.SetBlackboardSchemaRevision] = new AgentMutationDraftDescriptor(AgentMutationKind.SetBlackboardSchemaRevision, AgentMutationOutputKind.None, AgentBlackboardMutationLowering.LowerSetBlackboardSchemaRevision, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureExposedPropertyNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureExposedPropertyNode, AgentMutationOutputKind.Node, AgentBlackboardMutationLowering.LowerEnsureExposedPropertyNode),
                [AgentMutationKind.EnsureTimelineTreeClip] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTimelineTreeClip, AgentMutationOutputKind.TimelineClip, AgentTimelineMutationLowering.LowerEnsureTimelineTreeClip),
                [AgentMutationKind.EnsureMotionCurveTrack] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionCurveTrack, AgentMutationOutputKind.TimelineTrack, AgentTimelineMutationLowering.LowerEnsureMotionCurveTrack),
                [AgentMutationKind.EnsureMotionCurveClip] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionCurveClip, AgentMutationOutputKind.TimelineClip, AgentTimelineMutationLowering.LowerEnsureMotionCurveClip),
                [AgentMutationKind.ConfigureMotionCurveClip] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureMotionCurveClip, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureMotionCurveClip),
                [AgentMutationKind.EnsureMotionWarpTrack] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionWarpTrack, AgentMutationOutputKind.TimelineTrack, AgentTimelineMutationLowering.LowerEnsureMotionWarpTrack),
                [AgentMutationKind.DeleteTimelineTrack] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteTimelineTrack, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerDeleteTimelineTrack),
                [AgentMutationKind.EnsureTimelineSection] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTimelineSection, AgentMutationOutputKind.TimelineSection, AgentTimelineMutationLowering.LowerEnsureTimelineSection),
                [AgentMutationKind.DeleteTimelineSection] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteTimelineSection, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerDeleteTimelineSection),
                [AgentMutationKind.EnsureMotionWarpClip] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionWarpClip, AgentMutationOutputKind.TimelineClip, AgentTimelineMutationLowering.LowerEnsureMotionWarpClip),
                [AgentMutationKind.ConfigureMotionWarpSource] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureMotionWarpSource, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureMotionWarpSource),
                [AgentMutationKind.ConfigureMotionWarpParameters] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureMotionWarpParameters, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureMotionWarpParameters),
                [AgentMutationKind.MoveTimelineClip] = new AgentMutationDraftDescriptor(AgentMutationKind.MoveTimelineClip, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerMoveTimelineClip),
                [AgentMutationKind.ConfigureTimelineClipEase] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureTimelineClipEase, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureTimelineClipEase),
                [AgentMutationKind.ConfigureTimelineCurveChannel] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureTimelineCurveChannel, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureTimelineCurveChannel),
                [AgentMutationKind.ConfigureAnimationTrackChannel] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureAnimationTrackChannel, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureAnimationTrackChannel),
                [AgentMutationKind.ConfigureAnimationTrackSlot] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureAnimationTrackSlot, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureAnimationTrackSlot),
                [AgentMutationKind.ConfigureAnimationClipBlendProfile] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureAnimationClipBlendProfile, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerConfigureAnimationClipBlendProfile),
                [AgentMutationKind.EnsureAnimationClipSegment] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAnimationClipSegment, AgentMutationOutputKind.TimelineClip, AgentTimelineMutationLowering.LowerEnsureAnimationClipSegment),
                [AgentMutationKind.DeleteTimelineClip] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteTimelineClip, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerDeleteTimelineClip),
                [AgentMutationKind.EnsureTreeClipBlackboardWrite] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTreeClipBlackboardWrite, AgentMutationOutputKind.None, AgentTimelineMutationLowering.LowerEnsureTreeClipBlackboardWrite),
                [AgentMutationKind.DeleteTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteTransition, AgentMutationOutputKind.None, AgentStateMachineMutationLowering.LowerDeleteTransition),
                [AgentMutationKind.EnsureGameplayTag] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureGameplayTag, AgentMutationOutputKind.None, AgentActionMutationLowering.LowerEnsureGameplayTag),
                [AgentMutationKind.SetActionProfileGrantedTags] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionProfileGrantedTags, AgentMutationOutputKind.None, AgentActionMutationLowering.LowerSetActionProfileGrantedTags),
                [AgentMutationKind.SetActionProfileCancelQuery] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionProfileCancelQuery, AgentMutationOutputKind.None, AgentActionMutationLowering.LowerSetActionProfileCancelQuery),
                [AgentMutationKind.SetActionProfileTargetRequirement] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionProfileTargetRequirement, AgentMutationOutputKind.None, AgentActionMutationLowering.LowerSetActionProfileTargetRequirement),
                [AgentMutationKind.SetActionRequestTimingClass] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionRequestTimingClass, AgentMutationOutputKind.None, AgentActionMutationLowering.LowerSetActionRequestTimingClass),
                [AgentMutationKind.ConfigureControlConfiguration] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureControlConfiguration, AgentMutationOutputKind.None, AgentControlMutationLowering.LowerConfigureControlConfiguration, AgentMutationDomainMask.CharacterController),
                [AgentMutationKind.SetSkillFlowDocument] = new AgentMutationDraftDescriptor(AgentMutationKind.SetSkillFlowDocument, AgentMutationOutputKind.None, AgentSkillFlowDocumentMutationLowering.LowerSetSkillFlowDocument, AgentMutationDomainMask.CharacterController),
                [AgentMutationKind.EnsureAIControllerDefinition] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIControllerDefinition, AgentMutationOutputKind.None, AgentAIMutationLowering.LowerEnsureAIControllerDefinition, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIControllerTree] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIControllerTree, AgentMutationOutputKind.None, AgentAIMutationLowering.LowerEnsureAIControllerTree, AgentMutationDomainMask.AIController),
                [AgentMutationKind.BindAIControllerAssets] = new AgentMutationDraftDescriptor(AgentMutationKind.BindAIControllerAssets, AgentMutationOutputKind.None, AgentAIMutationLowering.LowerBindAIControllerAssets, AgentMutationDomainMask.AIController),
                [AgentMutationKind.ConfigureAICandidates] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureAICandidates, AgentMutationOutputKind.None, AgentAIMutationLowering.LowerConfigureAICandidates, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIBlackboardDeclaration, AgentMutationOutputKind.BlackboardDeclaration, AgentAIMutationLowering.LowerEnsureAIBlackboardDeclaration, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAISharedNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAISharedNode, AgentMutationOutputKind.Node, AgentAIMutationLowering.LowerEnsureAISharedNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIObservationNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIObservationNode, AgentMutationOutputKind.Node, AgentAIMutationLowering.LowerEnsureAIObservationNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIMemoryNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIMemoryNode, AgentMutationOutputKind.Node, AgentAIMutationLowering.LowerEnsureAIMemoryNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIContinuousInput] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIContinuousInput, AgentMutationOutputKind.Node, AgentAIMutationLowering.LowerEnsureAIContinuousInput, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIActionTarget] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIActionTarget, AgentMutationOutputKind.Node, AgentAIMutationLowering.LowerEnsureAIActionTarget, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIActionRequest] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIActionRequest, AgentMutationOutputKind.Node, AgentAIMutationLowering.LowerEnsureAIActionRequest, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureBTConditionRule] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureBTConditionRule, AgentMutationOutputKind.FlowEdge, AgentGraphMutationLowering.LowerEnsureBTConditionRule, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureGraphNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureGraphNode, AgentMutationOutputKind.Node, AgentGraphMutationLowering.LowerEnsureGraphNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.DeleteGraphNode] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteGraphNode, AgentMutationOutputKind.None, AgentGraphMutationLowering.LowerDeleteGraphNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.DeleteFlowEdge] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteFlowEdge, AgentMutationOutputKind.None, AgentGraphMutationLowering.LowerDeleteFlowEdge, AgentMutationDomainMask.AIController),
                [AgentMutationKind.DeletePropertyEdge] = new AgentMutationDraftDescriptor(AgentMutationKind.DeletePropertyEdge, AgentMutationOutputKind.None, AgentGraphMutationLowering.LowerDeletePropertyEdge, AgentMutationDomainMask.AIController),
                [AgentMutationKind.LinkFlow] = new AgentMutationDraftDescriptor(AgentMutationKind.LinkFlow, AgentMutationOutputKind.FlowEdge, AgentGraphMutationLowering.LowerLinkFlow, AgentMutationDomainMask.AIController),
                [AgentMutationKind.LinkProperty] = new AgentMutationDraftDescriptor(AgentMutationKind.LinkProperty, AgentMutationOutputKind.PropertyEdge, AgentGraphMutationLowering.LowerLinkProperty, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureGraph] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureGraph, AgentMutationOutputKind.Graph, AgentGraphMutationLowering.LowerEnsureGraph, AgentMutationDomainMask.AIController),
                [AgentMutationKind.ConfigureGraphReference] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureGraphReference, AgentMutationOutputKind.None, AgentGraphMutationLowering.LowerConfigureGraphReference, AgentMutationDomainMask.AIController)
            };

        public static bool TryGet(AgentMutationKind kind, out AgentMutationDraftDescriptor descriptor)
        {
            descriptor = null;
            return s_Descriptors.TryGetValue(kind, out descriptor);
        }

        public static IReadOnlyCollection<AgentMutationKind> Kinds => s_Descriptors.Keys;









    }

    [Flags]
    public enum AgentMutationDomainMask
    {
        CharacterController = 1,
        AIController = 2,
        Both = CharacterController | AIController
    }

    public sealed class AgentMutationDraftDescriptor
    {
        readonly Func<AgentMutationPlanningContext, AgentMutationDraft, AgentMutation> m_Lower;

        internal AgentMutationDraftDescriptor(
            AgentMutationKind kind,
            AgentMutationOutputKind outputKind,
            Func<AgentMutationPlanningContext, AgentMutationDraft, AgentMutation> lower,
            AgentMutationDomainMask domains = AgentMutationDomainMask.AIController)
        {
            Kind = kind;
            OutputKind = outputKind;
            m_Lower = lower;
            Domains = domains;
        }

        public AgentMutationKind Kind { get; }
        public AgentMutationOutputKind OutputKind { get; }
        public AgentMutationDomainMask Domains { get; }
        public bool Allows(string domain)
        {
            return string.Equals(domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal)
                ? (Domains & AgentMutationDomainMask.CharacterController) != 0
                : string.Equals(domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal) &&
                  (Domains & AgentMutationDomainMask.AIController) != 0;
        }
        internal AgentMutation Lower(AgentMutationPlanningContext context, AgentMutationDraft operation) => m_Lower(context, operation);
    }

    public sealed class AgentMutationPlanningContext
    {
        static readonly IReadOnlyDictionary<string, AgentConditionTermKind> s_TermKinds =
            new Dictionary<string, AgentConditionTermKind>(StringComparer.OrdinalIgnoreCase)
            {
                ["move_stop"] = AgentConditionTermKind.MoveStop,
                ["move_has"] = AgentConditionTermKind.MoveHas,
                ["move_run"] = AgentConditionTermKind.MoveRun,
                ["move_walk"] = AgentConditionTermKind.MoveWalk,
                ["turn_facing_angle"] = AgentConditionTermKind.TurnFacingAngle,
                ["blackboard_bool"] = AgentConditionTermKind.BlackboardBool,
                ["state_root_completed"] = AgentConditionTermKind.StateRootCompleted,
                ["action_request"] = AgentConditionTermKind.ActionRequest,
                ["action_window_active"] = AgentConditionTermKind.ActionWindowActive,
                ["action_can_activate"] = AgentConditionTermKind.CanActivateAction,
                ["ai_target_distance_compare_blackboard"] = AgentConditionTermKind.AITargetDistanceCompareBlackboard
            };

        readonly AgentCompileReport m_Report;
        readonly string m_MutationId;
        readonly IReadOnlyDictionary<string, AgentPlannedIdentitySymbol> m_PlannedIdentities;
        int m_ErrorCount;

        internal AgentMutationPlanningContext(
            AgentCompileReport report,
            string path,
            string plannedIdentity,
            IReadOnlyDictionary<string, AgentPlannedIdentitySymbol> plannedIdentities)
        {
            m_Report = report;
            Path = path;
            m_MutationId = plannedIdentity;
            m_PlannedIdentities = plannedIdentities;
        }

        public string Path { get; }
        public bool HasErrors => m_ErrorCount > 0;
        public bool IsValid => !HasErrors;

        public void ReadTransition(
            AgentMutationDraft operation,
            out AgentStateMachineTargetReference stateMachine,
            out AgentElementTargetReference from,
            out AgentElementTargetReference to)
        {
            stateMachine = RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine");
            from = RequiredElement(operation.fromElementAuthoringId, operation.fromPlannedIdentity, "fromElement");
            to = RequiredElement(operation.toElementAuthoringId, operation.toPlannedIdentity, "toElement");
        }

        public AgentStateBehaviorTargetReference RequiredStateBehaviorTarget(AgentMutationDraft operation)
        {
            bool hasDirect = HasValue(operation.targetGraphAuthoringId) || HasValue(operation.targetGraphPlannedIdentity);
            bool hasState = HasValue(operation.stateMachineGraphAuthoringId) || HasValue(operation.stateMachinePlannedIdentity) ||
                            HasValue(operation.stateAuthoringId) || HasValue(operation.statePlannedIdentity);
            if (hasDirect && hasState)
            {
                Error(string.Empty, "state_behavior_target_ambiguous", "State behavior target 不能同时使用 direct graph 与 StateMachine/State reference。");
                return default;
            }
            if (hasDirect)
                return new AgentStateBehaviorTargetReference(RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphPlannedIdentity, "targetGraph"), default, default);
            if (!hasState)
            {
                Error(string.Empty, "state_behavior_identity_missing", "Operation 必须用 target graph identity，或 StateMachine + State identity 指定 State body。");
                return default;
            }
            return new AgentStateBehaviorTargetReference(
                default,
                RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine"),
                RequiredState(operation.stateAuthoringId, operation.statePlannedIdentity, "state"));
        }

        public AgentGraphTargetReference RequiredGraph(string authoringId, string plannedIdentity, string label)
        {
            return new AgentGraphTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Graph));
        }

        public AgentGraphTargetReference OptionalGraph(string authoringId, string plannedIdentity, string label)
        {
            return new AgentGraphTargetReference(OptionalReference(authoringId, plannedIdentity, label, false, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Graph));
        }

        public AgentStateMachineTargetReference RequiredStateMachine(string authoringId, string plannedIdentity, string label, bool allowSelf = false)
        {
            return new AgentStateMachineTargetReference(RequiredReference(authoringId, plannedIdentity, label, allowSelf, AgentMutationOutputKind.StateMachine));
        }

        public AgentStateTargetReference RequiredState(string authoringId, string plannedIdentity, string label)
        {
            return new AgentStateTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.State));
        }

        public AgentStateTargetReference OptionalState(string authoringId, string plannedIdentity, string label, bool allowSelf = false)
        {
            return new AgentStateTargetReference(OptionalReference(authoringId, plannedIdentity, label, allowSelf, AgentMutationOutputKind.State));
        }

        public AgentElementTargetReference RequiredElement(string authoringId, string plannedIdentity, string label)
        {
            return new AgentElementTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Node));
        }

        public AgentElementTargetReference OptionalElement(string authoringId, string plannedIdentity, string label, bool allowSelf = false)
        {
            return new AgentElementTargetReference(OptionalReference(authoringId, plannedIdentity, label, allowSelf, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Node));
        }

        public AgentFlowEdgeTargetReference RequiredFlowEdge(string authoringId, string plannedIdentity, string label)
        {
            return new AgentFlowEdgeTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.FlowEdge));
        }

        public AgentAuthoringReference RequiredDeclaration(string authoringId, string plannedIdentity, string label)
        {
            return RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.BlackboardDeclaration);
        }

        public AgentPlannedIdentityReference OptionalPlannedIdentity(string plannedIdentity, string label, AgentMutationOutputKind expectedKind)
        {
            AgentAuthoringReference reference = OptionalReference(string.Empty, plannedIdentity, label, false, expectedKind);
            return reference.PlannedIdentity;
        }

        public string OptionalAuthoringId(string authoringId, string label)
        {
            if (!HasValue(authoringId))
                return string.Empty;
            if (!AuthoringIdentity.IsValid(authoringId))
                Error(label, "authoring_identity_invalid", $"Authoring identity 格式无效：{authoringId}");
            return authoringId;
        }

        public string RequiredText(string primary, string secondary, string field, string message)
        {
            string value = HasValue(primary) ? primary : secondary;
            if (!HasValue(value))
                Error(field, $"{field}_missing", message);
            return value ?? string.Empty;
        }

        public List<AgentConditionGroupMutation> RequiredConditionGroups(
            List<AgentConditionGroup> groups,
            AgentMutationDraft operation)
        {
            return RequiredConditionGroups(groups, operation, "conditionGroups");
        }

        public List<AgentConditionGroupMutation> RequiredConditionGroups(
            List<AgentConditionGroup> groups,
            AgentMutationDraft operation,
            string field)
        {
            var result = new List<AgentConditionGroupMutation>();
            if (groups == null || groups.Count == 0)
            {
                Error(field, "condition_groups_empty", $"{operation.kind} 必须包含至少一个条件组。");
                return result;
            }
            for (int i = 0; i < groups.Count; i++)
            {
                AgentConditionGroup group = groups[i];
                if (group == null)
                {
                    Error($"{field}[{i}]", "condition_group_missing", "Condition group 为空。");
                    continue;
                }
                List<AgentConditionTermMutation> terms = ConditionTerms(group.terms, operation, $"{field}[{i}].terms", false);
                if (terms.Count > 0)
                    result.Add(new AgentConditionGroupMutation(terms));
            }
            return result;
        }

        public List<AgentConditionTermMutation> ConditionTerms(
            List<AgentConditionTerm> terms,
            AgentMutationDraft operation,
            string field,
            bool allowEmpty)
        {
            var result = new List<AgentConditionTermMutation>();
            if (terms == null || terms.Count == 0)
            {
                if (!allowEmpty)
                    Error(field, "condition_group_terms_empty", "Condition group 必须包含至少一个 term。");
                return result;
            }
            for (int i = 0; i < terms.Count; i++)
            {
                AgentConditionTerm source = terms[i];
                string termField = $"{field}[{i}]";
                if (source == null || !TryParseTermKind(source.kind, out AgentConditionTermKind kind))
                {
                    Error(termField, "condition_term_unsupported", $"不支持的 Condition term：{source?.kind}");
                    continue;
                }
                string blackboardKey = source.blackboardKey ?? string.Empty;
                string request = HasValue(source.request) ? source.request : HasValue(operation.request) ? operation.request : operation.inputId;
                if ((kind == AgentConditionTermKind.BlackboardBool ||
                     kind == AgentConditionTermKind.TurnFacingAngle) &&
                    !HasValue(blackboardKey))
                {
                    Error($"{termField}.blackboardKey", "blackboard_key_missing", $"{source.kind} condition 缺少 blackboardKey。");
                    continue;
                }
                if (kind == AgentConditionTermKind.ActionRequest && !HasValue(request))
                {
                    Error($"{termField}.request", "request_missing", "action_request condition 缺少 request。");
                    continue;
                }
                if (kind == AgentConditionTermKind.ActionWindowActive && !HasValue(source.windowType))
                {
                    Error($"{termField}.windowType", "window_type_missing", "action_window_active condition 缺少 windowType。");
                    continue;
                }
                if (kind == AgentConditionTermKind.CanActivateAction && !HasValue(source.actionProfile))
                {
                    Error($"{termField}.actionProfile", "action_profile_missing", "action_can_activate condition 缺少 ActionProfile identity。");
                    continue;
                }
                CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
                if (kind == AgentConditionTermKind.AITargetDistanceCompareBlackboard)
                {
                    if (!HasValue(blackboardKey))
                    {
                        Error($"{termField}.blackboardKey", "blackboard_key_missing", "ai_target_distance_compare_blackboard condition 缺少 blackboardKey。");
                        continue;
                    }
                    if (!Enum.TryParse(source.compareType, true, out compareType))
                    {
                        Error($"{termField}.compareType", "compare_type_invalid", $"CompareType 无效：{source.compareType}");
                        continue;
                    }
                }
                result.Add(new AgentConditionTermMutation(
                    kind,
                    blackboardKey,
                    source.negate,
                    request,
                    source.windowType,
                    new AgentAssetReference(source.actionProfile, source.actionProfileAssetPath, source.actionProfileAssetGuid),
                    source.targetSnapshotBlackboardKey,
                    compareType));
            }
            return result;
        }

        public void Error(string field, string code, string message, string suggestion = "")
        {
            m_ErrorCount++;
            m_Report.Error(string.IsNullOrEmpty(field) ? Path : $"{Path}.{field}", code, message, suggestion);
        }

        public void ValidateOwnedReferences(AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureStateMachineMutation stateMachine:
                    ValidateOwnedReference(stateMachine.ExistingOwner.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureGraphMutation graph:
                    ValidateOwnedReference(graph.OwnerNode.Value, command.OwnerScope, "ownerNode");
                    break;
                case AgentConfigureGraphReferenceMutation reference:
                    ValidateOwnedReference(reference.OwnerNode.Value, command.OwnerScope, "ownerNode");
                    break;
                case AgentEnsureStateMutation state:
                    ValidateOwnedReference(state.ExistingState.Value, command.OwnerScope, "state");
                    break;
                case AgentDeleteStateMutation deleteState:
                    ValidateOwnedReference(deleteState.State.Value, command.OwnerScope, "state");
                    break;
                case AgentEnsureTransitionMutation transition:
                    ValidateOwnedReference(transition.From.Value, command.OwnerScope, "fromElement");
                    ValidateOwnedReference(transition.To.Value, command.OwnerScope, "toElement");
                    break;
                case AgentEnsureActionExitLifecycleMutation actionExit:
                    ValidateOwnedReference(actionExit.Source.Value, command.OwnerScope, "sourceElement");
                    break;
                case AgentDeleteStateBehaviorNodeMutation delete:
                    ValidateOwnedReference(delete.Element.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureStateBehaviorNodeMutation node:
                    ValidateOwnedReference(node.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureTimelineNodeMutation timeline:
                    ValidateOwnedReference(timeline.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureActionActivationMutation activation:
                    ValidateOwnedReference(activation.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureActionLifecycleTransitionMutation lifecycle:
                    ValidateOwnedReference(lifecycle.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureInputNodeMutation input:
                    ValidateOwnedReference(input.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureConditionValueNodeMutation conditionValue:
                    ValidateOwnedReference(conditionValue.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentGraphLinkMutation link:
                    ValidateOwnedReference(link.Source.Value, command.OwnerScope, "sourceElement");
                    ValidateOwnedReference(link.Target.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentConfigureActionAdmissionMutation admission:
                    ValidateOwnedReference(admission.Element.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIBlackboardDeclarationMutation declaration:
                    ValidateOwnedReference(declaration.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(declaration.ExistingDeclaration.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAISharedNodeMutation shared:
                    ValidateOwnedReference(shared.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(shared.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIObservationNodeMutation observation:
                    ValidateOwnedReference(observation.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(observation.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIMemoryNodeMutation memory:
                    ValidateOwnedReference(memory.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(memory.ExistingNode.Value, command.OwnerScope, "targetElement");
                    ValidateOwnedReference(memory.Declaration, command.OwnerScope, "declaration");
                    break;
                case AgentEnsureAIContinuousInputMutation continuousInput:
                    ValidateOwnedReference(continuousInput.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(continuousInput.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIActionTargetMutation actionTarget:
                    ValidateOwnedReference(actionTarget.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(actionTarget.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIActionRequestMutation actionRequest:
                    ValidateOwnedReference(actionRequest.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(actionRequest.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
            }
        }

        AgentAuthoringReference RequiredReference(
            string authoringId,
            string plannedIdentity,
            string label,
            params AgentMutationOutputKind[] expectedKinds)
        {
            return RequiredReference(authoringId, plannedIdentity, label, false, expectedKinds);
        }

        AgentAuthoringReference RequiredReference(
            string authoringId,
            string plannedIdentity,
            string label,
            bool allowSelf,
            params AgentMutationOutputKind[] expectedKinds)
        {
            AgentAuthoringReference reference = OptionalReference(authoringId, plannedIdentity, label, allowSelf, expectedKinds);
            if (!reference.IsValid && !(allowSelf && IsSelfIdentity(plannedIdentity)))
                Error(label, $"{label}_identity_missing", $"内部Mutation缺少{label} stable/local identity。");
            return reference;
        }

        AgentAuthoringReference OptionalReference(
            string authoringId,
            string plannedIdentity,
            string label,
            bool allowSelf,
            params AgentMutationOutputKind[] expectedKinds)
        {
            bool hasAuthoring = HasValue(authoringId);
            bool hasPlannedIdentity = HasValue(plannedIdentity);
            if (!hasAuthoring && !hasPlannedIdentity)
                return default;
            if (hasAuthoring && hasPlannedIdentity)
            {
                Error(label, $"{label}_reference_ambiguous", $"{label}不能同时包含stable identity与local identity。");
                return default;
            }
            if (hasAuthoring)
            {
                if (!AuthoringIdentity.IsValid(authoringId))
                {
                    Error(label, "authoring_identity_invalid", $"Authoring identity 格式无效：{authoringId}");
                    return default;
                }
                return new AgentAuthoringReference(authoringId, default);
            }

            AgentPlannedIdentityReference reference = AgentPlannedIdentityReference.Parse(plannedIdentity);
            if (!IsLocalIdentity(reference.Identity))
            {
                Error(label, $"{label}_planned_identity_invalid", $"计划内引用必须使用Document local identity：{plannedIdentity}");
                return default;
            }
            if (allowSelf && IsSelfIdentity(reference.Identity) && string.IsNullOrEmpty(reference.Role))
                return default;
            if (!m_PlannedIdentities.TryGetValue(reference.Identity, out AgentPlannedIdentitySymbol symbol))
            {
                Error(label, $"{label}_planned_identity_unresolved", $"Local identity必须由更早的typed Mutation创建：{plannedIdentity}");
                return default;
            }
            if (!Contains(expectedKinds, symbol.Kind))
            {
                Error(label, $"{label}_planned_identity_kind_invalid", $"Local identity {reference.Identity} 的typed kind是{symbol.Kind}，不能作为{label}。");
                return default;
            }
            if (!string.IsNullOrEmpty(reference.Role) &&
                (symbol.Kind != AgentMutationOutputKind.StateMachine || !IsStateMachineControlRole(reference.Role)))
            {
                Error(label, $"{label}_planned_identity_role_invalid", $"Local identity role无效：{plannedIdentity}");
                return default;
            }
            return new AgentAuthoringReference(string.Empty, reference);
        }

        void ValidateOwnedReference(AgentAuthoringReference reference, string expectedOwnerScope, string label)
        {
            AgentPlannedIdentityReference plannedIdentity = reference.PlannedIdentity;
            if (!plannedIdentity.IsValid || !m_PlannedIdentities.TryGetValue(plannedIdentity.Identity, out AgentPlannedIdentitySymbol symbol))
                return;

            string actualOwnerScope = !string.IsNullOrEmpty(plannedIdentity.Role) && symbol.Kind == AgentMutationOutputKind.StateMachine
                ? plannedIdentity.Identity
                : symbol.OwnerScope;
            if (!string.Equals(actualOwnerScope, expectedOwnerScope, StringComparison.Ordinal))
            {
                Error(
                    label,
                    $"{label}_planned_identity_owner_mismatch",
                    $"Local identity {plannedIdentity.Value}属于{actualOwnerScope}，不能用于owner {expectedOwnerScope}。");
            }
        }

        bool IsSelfIdentity(string plannedIdentity)
        {
            return string.Equals(plannedIdentity, m_MutationId, StringComparison.Ordinal);
        }

        static bool Contains(AgentMutationOutputKind[] values, AgentMutationOutputKind value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == value)
                    return true;
            }
            return false;
        }

        static bool IsStateMachineControlRole(string role)
        {
            return string.Equals(role, "StateMachineEnterNode", StringComparison.Ordinal) ||
                   string.Equals(role, "StateMachineAnyStateNode", StringComparison.Ordinal) ||
                   string.Equals(role, "StateMachineExitNode", StringComparison.Ordinal);
        }

        static bool IsLocalIdentity(string identity)
        {
            return !string.IsNullOrEmpty(identity) && identity.StartsWith("local:", StringComparison.Ordinal);
        }

        static bool TryParseTermKind(string value, out AgentConditionTermKind kind)
        {
            kind = default;
            return value != null && s_TermKinds.TryGetValue(value, out kind);
        }

        static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);
    }
}
