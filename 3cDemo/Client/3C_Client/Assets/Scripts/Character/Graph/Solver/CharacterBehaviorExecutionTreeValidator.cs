using System;
using System.Collections.Generic;
using ThirdPersonCharacterGraph;

namespace ThirdPersonCharacterGraph
{
    public sealed class CharacterBehaviorExecutionTreeValidationResult
    {
        readonly List<string> errors = new List<string>();
        readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                warnings.Add(message);
        }
    }

    public static class CharacterBehaviorExecutionTreeValidator
    {
        public static CharacterBehaviorExecutionTreeValidationResult Validate(CharacterBehaviorExecutionTree tree)
        {
            CharacterBehaviorExecutionTreeValidationResult result = new CharacterBehaviorExecutionTreeValidationResult();
            if (tree == null || !tree.IsDefined)
                return result;

            Dictionary<CharacterExecutionNodeId, CharacterExecutionNodeDefinition> nodeMap =
                new Dictionary<CharacterExecutionNodeId, CharacterExecutionNodeDefinition>();
            Dictionary<CharacterExecutionNodeId, int> parentCounts =
                new Dictionary<CharacterExecutionNodeId, int>();

            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                CharacterExecutionNodeDefinition node = tree.Nodes[i];
                if (!node.IsValid)
                {
                    result.AddError("node-invalid");
                    continue;
                }

                if (nodeMap.ContainsKey(node.Id))
                    result.AddError($"node-duplicate:{node.Id.Value}");
                else
                    nodeMap.Add(node.Id, node);
            }

            if (!nodeMap.TryGetValue(tree.RootNodeId, out CharacterExecutionNodeDefinition root))
            {
                result.AddError("root-missing");
                return result;
            }

            if (root.Kind != CharacterExecutionNodeKind.Root)
                result.AddError("root-kind-invalid");

            foreach (CharacterExecutionNodeDefinition node in nodeMap.Values)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    CharacterExecutionNodeId childId = node.Children[i];
                    if (!childId.IsValid)
                    {
                        result.AddError($"child-invalid:{node.Id.Value}");
                        continue;
                    }

                    if (!nodeMap.ContainsKey(childId))
                    {
                        result.AddError($"child-missing:{node.Id.Value}->{childId.Value}");
                        continue;
                    }

                    parentCounts.TryGetValue(childId, out int count);
                    parentCounts[childId] = count + 1;
                    if (!node.IsComposite && node.Children.Count > 0)
                        result.AddError($"non-composite-has-children:{node.Id.Value}");
                }
            }

            foreach (KeyValuePair<CharacterExecutionNodeId, int> pair in parentCounts)
            {
                if (pair.Value > 1)
                    result.AddError($"node-multiple-parents:{pair.Key.Value}");
            }

            DetectCycles(tree.RootNodeId, nodeMap, new HashSet<CharacterExecutionNodeId>(), new HashSet<CharacterExecutionNodeId>(), result);
            return result;
        }

        static void DetectCycles(
            CharacterExecutionNodeId id,
            IReadOnlyDictionary<CharacterExecutionNodeId, CharacterExecutionNodeDefinition> nodeMap,
            HashSet<CharacterExecutionNodeId> visiting,
            HashSet<CharacterExecutionNodeId> visited,
            CharacterBehaviorExecutionTreeValidationResult result)
        {
            if (visited.Contains(id) || !nodeMap.TryGetValue(id, out CharacterExecutionNodeDefinition node))
                return;

            if (visiting.Contains(id))
            {
                result.AddError($"node-cycle:{id.Value}");
                return;
            }

            visiting.Add(id);
            for (int i = 0; i < node.Children.Count; i++)
            {
                DetectCycles(node.Children[i], nodeMap, visiting, visited, result);
            }

            visiting.Remove(id);
            visited.Add(id);
        }
    }
}
