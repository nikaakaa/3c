using UnityEngine;
using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;

namespace TreeDesigner
{
    public static class TreeUtility
    {
        public static T Clone<T>(this T tree) where T : BaseTree
        {
            if (tree == null)
                return null;

            T cloneTree = System.Activator.CreateInstance(tree.GetType()) as T;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tree), cloneTree);
            cloneTree.name = tree.name;
            cloneTree.OnAfterDeserializeGraph();
            return cloneTree;
        }

#if UNITY_EDITOR
        public static T CloneForAuthoring<T>(this T tree) where T : BaseTree
        {
            if (tree == null)
                return null;

            var identities = new HashSet<string>(StringComparer.Ordinal);
            CollectDefinedIdentities(tree, identities, new HashSet<BaseGraph>());
            string json = JsonUtility.ToJson(tree);
            foreach (string identity in identities)
                json = json.Replace(identity, AuthoringIdentity.Create());

            T cloneTree = Activator.CreateInstance(tree.GetType()) as T;
            JsonUtility.FromJsonOverwrite(json, cloneTree);
            cloneTree.name = tree.name;
            cloneTree.OnAfterDeserializeGraph();
            return cloneTree;
        }

        static void CollectDefinedIdentities(BaseTree graph, HashSet<string> identities, HashSet<BaseGraph> visited)
        {
            if (graph == null || !visited.Add(graph))
                return;

            AddIdentity(graph.GraphAuthoringId, identities);
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                AddIdentity(node.GUID, identities);
                foreach (NodeGraphReference reference in node.GetGraphReferences())
                {
                    if (reference.Inline && reference.Tree != null)
                        CollectDefinedIdentities(reference.Tree, identities, visited);
                }
            }
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge == null)
                    continue;
                AddIdentity(edge.GUID, identities);
                if (edge.ConditionRuleGraphOwnership == ConditionRuleGraphOwnership.Inline &&
                    edge.TryResolveConditionRuleGraph(out ConditionRuleGraph ruleGraph, out _))
                {
                    CollectDefinedIdentities(ruleGraph, identities, visited);
                }
            }
            for (int i = 0; i < graph.PropertyEdges.Count; i++)
                AddIdentity(graph.PropertyEdges[i]?.GUID, identities);
            for (int i = 0; i < graph.ExposedProperties.Count; i++)
                AddIdentity(graph.ExposedProperties[i]?.DeclarationId, identities);
        }

        static void AddIdentity(string identity, HashSet<string> identities)
        {
            if (AuthoringIdentity.IsValid(identity))
                identities.Add(identity);
        }
#endif
    }
}
