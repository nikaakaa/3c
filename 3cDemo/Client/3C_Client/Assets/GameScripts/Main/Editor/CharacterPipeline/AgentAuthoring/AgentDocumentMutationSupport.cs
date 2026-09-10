using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentDocumentMutationSupport
    {
        internal static void Add(
            AgentMutationDraftSet mutations,
            string path,
            AgentMutationKind kind,
            Action<AgentMutationDraft> configure)
        {
            var operation = new AgentMutationDraft
            {
                id = "mutation-" + mutations.mutations.Count.ToString("D4"),
                sourcePath = path,
                kind = kind
            };
            configure(operation);
            mutations.mutations.Add(operation);
        }

        internal static Dictionary<string, T> Index<T>(
            IEnumerable<T> values,
            Func<T, string> identity,
            string path,
            AgentCompileReport report)
            where T : class
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            int index = 0;
            foreach (T value in values ?? Array.Empty<T>())
            {
                string key = value == null ? string.Empty : identity(value);
                if (string.IsNullOrWhiteSpace(key))
                    report.Error($"{path}[{index}]", "entity_identity_missing", "Document entity缺少identity。");
                else if (!result.TryAdd(key, value))
                    report.Error($"{path}[{index}]", "entity_identity_duplicate", $"Document entity identity重复：{key}");
                index++;
            }
            return result;
        }

        internal static bool Same(object left, object right)
        {
            return string.Equals(
                AgentAuthoringDocumentCodec.Hash(left),
                AgentAuthoringDocumentCodec.Hash(right),
                StringComparison.Ordinal);
        }

        internal static bool SameList(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            return (left ?? Array.Empty<string>()).SequenceEqual(
                right ?? Array.Empty<string>(),
                StringComparer.Ordinal);
        }

        internal static string Escape(string identity)
        {
            return "'" + (identity ?? string.Empty).Replace("'", "\\'") + "'";
        }
    }
}
