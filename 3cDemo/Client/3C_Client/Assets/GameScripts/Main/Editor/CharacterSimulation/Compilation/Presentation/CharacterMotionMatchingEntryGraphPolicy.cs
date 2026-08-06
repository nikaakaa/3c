using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterMotionMatchingEntryGraphPolicy
    {
        static readonly HashSet<CharacterPoseNodeKind> s_AllowedKinds = new HashSet<CharacterPoseNodeKind>
        {
            CharacterPoseNodeKind.EntryPoseInput,
            CharacterPoseNodeKind.GraphOutput,
            CharacterPoseNodeKind.ProgramParameterInput,
            CharacterPoseNodeKind.Inertialization,
            CharacterPoseNodeKind.BlendPose,
            CharacterPoseNodeKind.LayeredBoneBlend,
            CharacterPoseNodeKind.AdditivePose,
            CharacterPoseNodeKind.PoseParameterResolve,
            CharacterPoseNodeKind.RootOrientationWarp
        };

        internal static bool IsEntryGraph(CharacterTypedPoseGraph graph) =>
            graph != null && graph.Nodes.Any(value => value?.Kind == CharacterPoseNodeKind.EntryPoseInput);

        internal static void RequireValid(CharacterTypedPoseGraph graph)
        {
            if (graph == null || !graph.GraphId.IsValid)
                throw new ArgumentException("Motion Matching entry graph is missing.", nameof(graph));
            CharacterTypedPoseNode input = graph.Nodes.SingleOrDefault(value => value?.Kind == CharacterPoseNodeKind.EntryPoseInput) ??
                throw new InvalidOperationException($"Motion Matching entry graph '{graph.GraphId}' requires one Entry Pose Input.");
            CharacterTypedPoseNode output = graph.Nodes.SingleOrDefault(value => value?.Kind == CharacterPoseNodeKind.GraphOutput) ??
                throw new InvalidOperationException($"Motion Matching entry graph '{graph.GraphId}' requires one Graph Output.");
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i] ??
                    throw new InvalidOperationException($"Motion Matching entry graph '{graph.GraphId}' contains a missing node.");
                if (!s_AllowedKinds.Contains(node.Kind))
                    throw new InvalidOperationException($"Motion Matching entry graph '{graph.GraphId}' contains forbidden node '{node.Kind}'.");
                CharacterPoseExecutionDomain domain = CharacterPoseCompilerHandlerRegistry.Shared.Require(node.Kind).ExecutionDomain;
                if (domain == CharacterPoseExecutionDomain.WorldAwareValue ||
                    node.Kind == CharacterPoseNodeKind.LocalToComponentPose ||
                    node.Kind == CharacterPoseNodeKind.ComponentToLocalPose ||
                    node.Kind == CharacterPoseNodeKind.FullBodyIK)
                {
                    throw new InvalidOperationException($"Motion Matching entry graph '{graph.GraphId}' contains a world-aware or Component Pose node '{node.Kind}'.");
                }
            }

            var adjacency = graph.Nodes.ToDictionary(value => value.NodeId, _ => new List<PoseNodeId>());
            foreach (CharacterPoseEdge edge in graph.Edges)
            {
                if (edge == null || !adjacency.TryGetValue(edge.SourceNodeId, out List<PoseNodeId> targets) || !adjacency.ContainsKey(edge.TargetNodeId))
                    throw new InvalidOperationException($"Motion Matching entry graph '{graph.GraphId}' contains an invalid edge.");
                targets.Add(edge.TargetNodeId);
            }
            int pathCount = CountPaths(input.NodeId, output.NodeId, adjacency, new HashSet<PoseNodeId>(), new Dictionary<PoseNodeId, int>());
            if (pathCount != 1)
                throw new InvalidOperationException($"Motion Matching entry graph '{graph.GraphId}' requires exactly one Entry Pose Input to Graph Output path.");
        }

        static int CountPaths(
            PoseNodeId node,
            PoseNodeId output,
            IReadOnlyDictionary<PoseNodeId, List<PoseNodeId>> adjacency,
            ISet<PoseNodeId> visiting,
            IDictionary<PoseNodeId, int> memo)
        {
            if (node == output)
                return 1;
            if (memo.TryGetValue(node, out int cached))
                return cached;
            if (!visiting.Add(node))
                throw new InvalidOperationException("Motion Matching entry graph contains a cycle.");
            int count = 0;
            foreach (PoseNodeId target in adjacency[node])
            {
                count += CountPaths(target, output, adjacency, visiting, memo);
                if (count > 1)
                    break;
            }
            visiting.Remove(node);
            memo[node] = count;
            return count;
        }
    }
}
