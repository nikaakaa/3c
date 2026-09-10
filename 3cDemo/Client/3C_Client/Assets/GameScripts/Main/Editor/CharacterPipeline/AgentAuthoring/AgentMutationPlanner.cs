using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMutationPlanner
    {
        public bool TryCreatePlan(
            AgentMutationDraftSet drafts,
            AgentCompileReport report,
            out AgentMutationPlan plan)
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
            report.domain = drafts.domain ?? string.Empty;
            report.rootIdentity = drafts.rootIdentity ?? string.Empty;
            if (!string.Equals(drafts.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                report.Error(
                    "document.schemaVersion",
                    "unsupported_schema_version",
                    $"Mutation Draft schema必须是{AgentAuthoringSchema.Version}，当前为{drafts.schemaVersion}。");
                report.metrics.schemaInvalidCount++;
            }
            if (!AgentAuthoringSchema.IsDomain(drafts.domain))
            {
                report.Error("document.domain", "unsupported_domain", $"Mutation domain无效：{drafts.domain}");
                report.metrics.schemaInvalidCount++;
            }
            if (string.IsNullOrWhiteSpace(drafts.rootIdentity) || string.IsNullOrWhiteSpace(drafts.sourceRevision))
            {
                report.Error("document", "document_source_identity_missing", "Mutation Draft缺少rootIdentity或sourceRevision。");
                report.metrics.schemaInvalidCount++;
            }
            if (drafts.mutations == null)
            {
                report.Error("document.editable", "editable_missing", "Document editable正文缺失。");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            var commands = new List<AgentMutation>(drafts.mutations.Count);
            var mutationIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < drafts.mutations.Count; index++)
            {
                AgentMutationDraft operation = drafts.mutations[index];
                string path = string.IsNullOrEmpty(operation?.sourcePath)
                    ? $"document.mutations[{index}]"
                    : operation.sourcePath;
                if (operation == null)
                {
                    report.Error(path, "mutation_missing", "内部Mutation Draft为空。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(operation.id) || !mutationIds.Add(operation.id))
                {
                    report.Error(path, "mutation_id_invalid", "每个内部Mutation必须使用非空唯一id。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!AgentTypedMutationLoweringCatalog.TryGet(operation.kind, out AgentMutationDraftDescriptor descriptor))
                {
                    report.Error(path, "unknown_mutation", $"内部Mutation kind没有当前Character typed lowering：{operation.kind}");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!descriptor.Allows(drafts.domain))
                {
                    report.Error(path, "mutation_domain_mismatch", $"Mutation '{operation.kind}'不允许用于{drafts.domain} domain。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }

                var context = new AgentMutationPlanningContext(report, path);
                AgentMutation command = descriptor.Lower(context, operation);
                if (command == null || context.HasErrors)
                {
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                commands.Add(command);
                report.metrics.schemaValidCount++;
            }

            if (report.HasErrors())
                return false;
            plan = new AgentMutationPlan(commands, drafts.domain, drafts.rootIdentity, drafts.sourceRevision);
            return true;
        }
    }

    public static class AgentTypedMutationLoweringCatalog
    {
        static readonly Dictionary<AgentMutationKind, AgentMutationDraftDescriptor> s_Descriptors =
            new Dictionary<AgentMutationKind, AgentMutationDraftDescriptor>
            {
                [AgentMutationKind.EnsureGameplayTag] = new AgentMutationDraftDescriptor(
                    AgentMutationKind.EnsureGameplayTag,
                    AgentActionMutationLowering.LowerEnsureGameplayTag),
                [AgentMutationKind.SetActionProfileGrantedTags] = new AgentMutationDraftDescriptor(
                    AgentMutationKind.SetActionProfileGrantedTags,
                    AgentActionMutationLowering.LowerSetActionProfileGrantedTags),
                [AgentMutationKind.SetActionProfileCancelQuery] = new AgentMutationDraftDescriptor(
                    AgentMutationKind.SetActionProfileCancelQuery,
                    AgentActionMutationLowering.LowerSetActionProfileCancelQuery),
                [AgentMutationKind.SetActionProfileTargetRequirement] = new AgentMutationDraftDescriptor(
                    AgentMutationKind.SetActionProfileTargetRequirement,
                    AgentActionMutationLowering.LowerSetActionProfileTargetRequirement),
                [AgentMutationKind.SetActionRequestTimingClass] = new AgentMutationDraftDescriptor(
                    AgentMutationKind.SetActionRequestTimingClass,
                    AgentActionMutationLowering.LowerSetActionRequestTimingClass),
                [AgentMutationKind.ConfigureControlConfiguration] = new AgentMutationDraftDescriptor(
                    AgentMutationKind.ConfigureControlConfiguration,
                    AgentControlMutationLowering.LowerConfigureControlConfiguration),
                [AgentMutationKind.SetSkillFlowDocument] = new AgentMutationDraftDescriptor(
                    AgentMutationKind.SetSkillFlowDocument,
                    AgentSkillFlowDocumentMutationLowering.LowerSetSkillFlowDocument)
            };

        public static bool TryGet(AgentMutationKind kind, out AgentMutationDraftDescriptor descriptor)
        {
            return s_Descriptors.TryGetValue(kind, out descriptor);
        }

        public static IReadOnlyCollection<AgentMutationKind> Kinds => s_Descriptors.Keys;
    }

    [Flags]
    public enum AgentMutationDomainMask
    {
        CharacterController = 1
    }

    public sealed class AgentMutationDraftDescriptor
    {
        readonly Func<AgentMutationPlanningContext, AgentMutationDraft, AgentMutation> m_Lower;

        internal AgentMutationDraftDescriptor(
            AgentMutationKind kind,
            Func<AgentMutationPlanningContext, AgentMutationDraft, AgentMutation> lower,
            AgentMutationDomainMask domains = AgentMutationDomainMask.CharacterController)
        {
            Kind = kind;
            m_Lower = lower ?? throw new ArgumentNullException(nameof(lower));
            Domains = domains;
        }

        public AgentMutationKind Kind { get; }
        public AgentMutationDomainMask Domains { get; }

        public bool Allows(string domain)
        {
            return string.Equals(domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal) &&
                   (Domains & AgentMutationDomainMask.CharacterController) != 0;
        }

        internal AgentMutation Lower(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            return m_Lower(context, operation);
        }
    }

    public sealed class AgentMutationPlanningContext
    {
        readonly AgentCompileReport m_Report;
        int m_ErrorCount;

        internal AgentMutationPlanningContext(AgentCompileReport report, string path)
        {
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            Path = path ?? string.Empty;
        }

        public string Path { get; }
        public bool HasErrors => m_ErrorCount != 0;
        public bool IsValid => !HasErrors;

        public string RequiredText(string primary, string secondary, string field, string message)
        {
            string value = !string.IsNullOrWhiteSpace(primary) ? primary : secondary;
            if (string.IsNullOrWhiteSpace(value))
                Error(field, $"{field}_missing", message);
            return value ?? string.Empty;
        }

        public void Error(string field, string code, string message, string suggestion = "")
        {
            m_ErrorCount++;
            m_Report.Error(
                string.IsNullOrEmpty(field) ? Path : $"{Path}.{field}",
                code,
                message,
                suggestion);
        }
    }
}
