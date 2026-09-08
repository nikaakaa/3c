using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentSetSkillFlowDocumentMutation : AgentMutation
    {
        public AgentSetSkillFlowDocumentMutation(
            string id,
            string path,
            AgentPackageSkillFlowDocument document)
            : base(
                id,
                AgentMutationKind.SetSkillFlowDocument,
                "set_skill_flow_document",
                AgentMutationOutputKind.None,
                path,
                "skill-flow",
                Vector2.zero)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public AgentPackageSkillFlowDocument Document { get; }
    }

    internal static class AgentSkillFlowDocumentMutationLowering
    {
        public static AgentMutation LowerSetSkillFlowDocument(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            if (operation.skillFlowDocument == null)
            {
                context.Error(string.Empty, "skill_flow_document_missing", "Skill Flow Document mutation正文缺失。");
                return null;
            }
            return new AgentSetSkillFlowDocumentMutation(
                operation.id,
                context.Path,
                AgentSkillFlowDocumentClone.Clone(operation.skillFlowDocument));
        }
    }
}
