using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    public readonly struct ActionTargetAuthoringIssue
    {
        public ActionTargetAuthoringIssue(string path, string code, string message)
        {
            Path = path ?? string.Empty;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Path { get; }
        public string Code { get; }
        public string Message { get; }
    }

    public static class ActionTargetAuthoringValidation
    {
        public static void Collect(
            CharacterAuthoringTopologyProjection topology,
            List<ActionTargetAuthoringIssue> issues)
        {
            if (topology == null)
                throw new ArgumentNullException(nameof(topology));
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));
            if (!topology.IsValid)
                return;

            for (int graphIndex = 0; graphIndex < topology.Graphs.Count; graphIndex++)
            {
                CharacterAuthoringGraphEntry entry = topology.Graphs[graphIndex];
                if (!entry.FirstOccurrence)
                    continue;
                string path = entry.Route.Count == 0 ? "root" : entry.Route.ToString();
                ValidateRequiredSnapshots(entry.Graph, path, issues);
                if (entry.Graph is StateBehaviorSubTree stateBody)
                    ValidateMotionWarpCallSites(stateBody, path, issues);
                if (entry.Graph is StateMachineGraph stateMachine)
                    ValidateAdmissionChains(stateMachine, path, issues);
            }
        }

        public static bool ContainsMotionWarp(TimelineData timeline)
        {
            if (timeline == null)
                return false;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is MotionWarpClip)
                        return true;
                }
            }
            return false;
        }

        static void ValidateRequiredSnapshots(
            BaseTree graph,
            string path,
            List<ActionTargetAuthoringIssue> issues)
        {
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                BaseNode node = graph.Nodes[nodeIndex];
                if (node is ActivateActionInstanceNode activation &&
                    activation.ActionProfile)
                {
                    ValidateTargetReference(
                        activation.ActionProfile,
                        activation.TargetSnapshotVariable,
                        path,
                        node,
                        "activation",
                        issues);
                }
                else if (node is CanActivateActionInfoNode admission &&
                         admission.ActionProfile)
                {
                    ValidateTargetReference(
                        admission.ActionProfile,
                        admission.TargetSnapshotVariable,
                        path,
                        node,
                        "admission",
                        issues);
                }
            }
        }

        static void ValidateMotionWarpCallSites(
            StateBehaviorSubTree graph,
            string path,
            List<ActionTargetAuthoringIssue> issues)
        {
            var activations = new List<ActivateActionInstanceNode>();
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                if (graph.Nodes[nodeIndex] is ActivateActionInstanceNode activation)
                    activations.Add(activation);
            }

            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                if (graph.Nodes[nodeIndex] is not TimelineNode timeline || !ContainsMotionWarp(timeline.Timeline))
                    continue;
                if (!timeline.ActionContext)
                {
                    Add(issues, path, timeline, "motion_warp_action_context_missing", "A Timeline containing MotionWarp requires an explicit Action Context.");
                    continue;
                }

                ActivateActionInstanceNode match = null;
                int matchCount = 0;
                for (int activationIndex = 0; activationIndex < activations.Count; activationIndex++)
                {
                    if (activations[activationIndex].ActionContext != timeline.ActionContext)
                        continue;
                    match = activations[activationIndex];
                    matchCount++;
                }
                if (matchCount != 1)
                {
                    Add(issues, path, timeline, "motion_warp_action_activation_ambiguous", $"A MotionWarp Timeline must match exactly one Action activation; found {matchCount}.");
                    continue;
                }
                if (!match.ActionProfile || match.ActionProfile.TargetRequirement == ActionTargetRequirement.None)
                {
                    Add(issues, path, timeline, "motion_warp_target_requirement_invalid", "A MotionWarp Timeline requires its ActionProfile to declare OptionalSnapshot or SnapshotRequired.");
                }
                else if (!match.TargetSnapshotVariable.IsValid)
                {
                    Add(issues, path, timeline, "motion_warp_target_declaration_missing", "A MotionWarp Timeline requires its Action activation to reference a TargetSnapshot declaration.");
                }
            }
        }

        static void ValidateAdmissionChains(
            StateMachineGraph graph,
            string path,
            List<ActionTargetAuthoringIssue> issues)
        {
            for (int edgeIndex = 0; edgeIndex < graph.Edges.Count; edgeIndex++)
            {
                BaseEdge edge = graph.Edges[edgeIndex];
                ConditionRuleGraph condition = edge?.ConditionRuleGraph;
                StateNode targetState = ResolveNode(graph, edge?.EndNode, edge?.EndNodeGUID) as StateNode;
                StateBehaviorSubTree targetBody = targetState?.SubTree as StateBehaviorSubTree;
                if (condition == null || targetBody == null)
                    continue;

                for (int conditionNodeIndex = 0; conditionNodeIndex < condition.Nodes.Count; conditionNodeIndex++)
                {
                    if (condition.Nodes[conditionNodeIndex] is not CanActivateActionInfoNode admission || !admission.ActionProfile)
                        continue;
                    var matches = new List<ActivateActionInstanceNode>();
                    CollectMatchingActivations(
                        targetBody,
                        admission.ActionProfile,
                        new HashSet<BaseTree>(),
                        matches);
                    string edgePath = $"{path}/edge:{edge.GUID}";
                    if (matches.Count == 0)
                    {
                        issues.Add(new ActionTargetAuthoringIssue(
                            edgePath,
                            "action_admission_activation_ambiguous",
                            $"CanActivate '{admission.ActionProfile.ActionId}' must reach at least one matching activation below the target State body."));
                        continue;
                    }
                    PipelineBlackboardVariableReference query = admission.TargetSnapshotVariable;
                    if (admission.ActionProfile.TargetRequirement != ActionTargetRequirement.None && !query.IsValid)
                    {
                        issues.Add(new ActionTargetAuthoringIssue(
                            edgePath,
                            "action_target_snapshot_required",
                            $"CanActivate '{admission.ActionProfile.ActionId}' requires a TargetSnapshot declaration."));
                        continue;
                    }
                    for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
                    {
                        PipelineBlackboardVariableReference activationTarget = matches[matchIndex].TargetSnapshotVariable;
                        if (string.Equals(query.DeclarationId, activationTarget.DeclarationId, StringComparison.Ordinal) &&
                            string.Equals(query.DeclarationOwnerId, activationTarget.DeclarationOwnerId, StringComparison.Ordinal))
                        {
                            continue;
                        }
                        issues.Add(new ActionTargetAuthoringIssue(
                            edgePath,
                            "action_target_snapshot_declaration_mismatch",
                            $"CanActivate and every reachable Activate '{admission.ActionProfile.ActionId}' must reference the same TargetSnapshot declaration."));
                        break;
                    }
                }
            }
        }

        static void CollectMatchingActivations(
            BaseTree graph,
            ActionProfile profile,
            HashSet<BaseTree> visited,
            List<ActivateActionInstanceNode> output)
        {
            if (graph == null || !visited.Add(graph))
                return;
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                BaseNode node = graph.Nodes[nodeIndex];
                if (node is ActivateActionInstanceNode activation && activation.ActionProfile == profile)
                    output.Add(activation);
                if (node == null)
                    continue;
                foreach (NodeGraphReference reference in node.GetGraphReferences())
                {
                    if (reference.Tree != null)
                        CollectMatchingActivations(reference.Tree, profile, visited, output);
                }
            }
        }

        static BaseNode ResolveNode(StateMachineGraph graph, BaseNode node, string guid)
        {
            if (node != null)
                return node;
            if (string.IsNullOrEmpty(guid))
                return null;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (string.Equals(graph.Nodes[i]?.GUID, guid, StringComparison.Ordinal))
                    return graph.Nodes[i];
            }
            return null;
        }

        static void Add(
            List<ActionTargetAuthoringIssue> issues,
            string path,
            BaseNode node,
            string code,
            string message)
        {
            issues.Add(new ActionTargetAuthoringIssue($"{path}/node:{node.GUID}", code, message));
        }

        static void ValidateTargetReference(
            ActionProfile profile,
            PipelineBlackboardVariableReference reference,
            string path,
            BaseNode node,
            string callSite,
            List<ActionTargetAuthoringIssue> issues)
        {
            if (profile.TargetRequirement == ActionTargetRequirement.None)
            {
                if (reference.IsValid)
                    Add(issues, path, node, "action_target_snapshot_forbidden", $"Action '{profile.ActionId}' {callSite} must not reference a target while TargetRequirement=None.");
                return;
            }
            if (!reference.IsValid)
                Add(issues, path, node, "action_target_snapshot_required", $"Action '{profile.ActionId}' {callSite} requires an explicit TargetSnapshot declaration.");
        }
    }
}
