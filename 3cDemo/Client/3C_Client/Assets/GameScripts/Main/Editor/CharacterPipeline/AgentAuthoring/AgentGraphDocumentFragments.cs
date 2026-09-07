using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentGraphDocumentFragments
    {
        public static bool IsDefinitionFragment(string path)
        {
            return path.StartsWith("editable/graphs/", StringComparison.Ordinal) &&
                   (path.EndsWith("/graph.json", StringComparison.Ordinal) ||
                    path.EndsWith("/layout.json", StringComparison.Ordinal));
        }

        public static bool TryDiscoverNew(
            IReadOnlyDictionary<string, JToken> candidates,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string directory in candidates.Keys
                         .Select(path => path.Substring(0, path.LastIndexOf('/')))
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string graphPath = directory + "/graph.json";
                string layoutPath = directory + "/layout.json";
                if (!candidates.TryGetValue(graphPath, out JToken graphToken) ||
                    !candidates.TryGetValue(layoutPath, out JToken layoutToken))
                {
                    report.Error(directory, "graph_new_pair_incomplete", "新增Graph必须同时提供同目录graph.json与layout.json。");
                    valid = false;
                    continue;
                }
                if (!AgentAuthoringDocumentCodec.TryConvertToken(graphToken, graphPath, report, out AgentPackageGraphFile graph) ||
                    !AgentAuthoringDocumentCodec.TryConvertToken(layoutToken, layoutPath, report, out AgentPackageLayoutFile layout))
                {
                    valid = false;
                    continue;
                }
                string expectedDirectory = $"editable/graphs/{AgentAuthoringPackageMapper.Segment(graph.id)}";
                bool localEntities = graph.nodes != null &&
                                     graph.nodes.All(node => node != null && IsLocal(node.id)) &&
                                     graph.flowEdges != null &&
                                     graph.flowEdges.All(edge => edge != null && IsLocal(edge.id)) &&
                                     graph.propertyEdges != null &&
                                     graph.propertyEdges.All(edge => edge != null && IsLocal(edge.id));
                if (!IsLocal(graph.id) ||
                    !string.Equals(directory, expectedDirectory, StringComparison.Ordinal) ||
                    !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal) ||
                    graph.owner == null ||
                    !IsIdentity(graph.owner.entityId) ||
                    string.IsNullOrWhiteSpace(graph.owner.slot) ||
                    !localEntities)
                {
                    report.Error(
                        graphPath,
                        "graph_new_pair_invalid",
                        "新增Graph必须使用canonical local identity目录、匹配layout、明确owner，并让Graph/Node/Edge使用local:* identity。");
                    valid = false;
                    continue;
                }
                result.Add(graphPath);
                result.Add(layoutPath);
            }
            discovered = result;
            return valid;
        }

        static bool IsLocal(string identity)
        {
            return !string.IsNullOrWhiteSpace(identity) &&
                   identity.StartsWith("local:", StringComparison.Ordinal) &&
                   identity.Length > "local:".Length &&
                   identity.Substring("local:".Length).All(character =>
                       char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }

        static bool IsIdentity(string identity)
        {
            return !string.IsNullOrWhiteSpace(identity) &&
                   !identity.Any(char.IsWhiteSpace) &&
                   !identity.StartsWith("@", StringComparison.Ordinal);
        }
    }
}
