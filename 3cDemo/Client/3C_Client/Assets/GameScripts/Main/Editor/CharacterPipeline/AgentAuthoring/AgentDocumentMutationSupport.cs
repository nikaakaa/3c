using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonGameplay.Tags;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal sealed class AgentMutationPlanBuilder
    {
        readonly List<AgentMutation> m_Commands = new List<AgentMutation>();
        readonly HashSet<string> m_Ids = new HashSet<string>(StringComparer.Ordinal);
        readonly AgentCompileReport m_Report;
        int m_NextId;

        public AgentMutationPlanBuilder(
            AgentCompileReport report,
            string domain,
            string rootIdentity,
            string sourceRevision)
        {
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            Domain = domain ?? string.Empty;
            RootIdentity = rootIdentity ?? string.Empty;
            SourceRevision = sourceRevision ?? string.Empty;
        }

        public string Domain { get; }
        public string RootIdentity { get; }
        public string SourceRevision { get; }

        public void Add(string path, Func<string, AgentMutation> factory)
        {
            string mutationId = "mutation-" + m_NextId++.ToString("D4");
            AgentMutation command;
            try
            {
                command = factory?.Invoke(mutationId);
            }
            catch (Exception exception)
            {
                m_Report.Error(path, "mutation_create_failed", exception.Message);
                return;
            }
            if (command == null)
                return;
            if (!m_Ids.Add(command.Id))
            {
                m_Report.Error(path, "mutation_id_invalid", $"Mutation id重复：{command.Id}");
                return;
            }
            m_Commands.Add(command);
            m_Report.metrics.schemaValidCount++;
        }

        public AgentMutationPlan Build()
        {
            return m_Report.HasErrors()
                ? null
                : new AgentMutationPlan(m_Commands, Domain, RootIdentity, SourceRevision);
        }
    }

    internal sealed class AgentMutationValueReader
    {
        readonly AgentCompileReport m_Report;

        public AgentMutationValueReader(AgentCompileReport report, string path)
        {
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            Path = path ?? string.Empty;
        }

        public string Path { get; }
        public bool IsValid { get; private set; } = true;

        public string RequiredText(string value, string field, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Error(field, $"{field}_missing", message);
                return string.Empty;
            }
            return value;
        }

        public bool TryParseEnum<T>(string value, string field, out T result)
            where T : struct
        {
            if (Enum.TryParse(value, true, out result) && Enum.IsDefined(typeof(T), result))
                return true;
            Error(field, $"{field}_invalid", $"{field} 无效：{value}");
            return false;
        }

        public List<GameplayTagId> ReadTags(IList<string> values, string field)
        {
            var result = new List<GameplayTagId>();
            var unique = new HashSet<GameplayTagId>();
            foreach (string value in values ?? Array.Empty<string>())
            {
                var tag = new GameplayTagId(value);
                if (!tag.IsValid || !unique.Add(tag))
                {
                    Error(field, "gameplay_tag_invalid", $"GameplayTag 缺失或重复：{value}");
                    continue;
                }
                result.Add(tag);
            }
            return result;
        }

        public void Error(string field, string code, string message)
        {
            IsValid = false;
            m_Report.Error(
                string.IsNullOrEmpty(field) ? Path : $"{Path}.{field}",
                code,
                message);
        }
    }

    internal static class AgentDocumentMutationSupport
    {
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
