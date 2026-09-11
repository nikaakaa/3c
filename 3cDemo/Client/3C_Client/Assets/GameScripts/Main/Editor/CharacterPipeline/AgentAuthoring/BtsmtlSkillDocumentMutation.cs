using System;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{

    public sealed class BtsmtlSetSkillDocumentMutation : AgentMutation
    {
        public BtsmtlSetSkillDocumentMutation(
            string id,
            string path,
            AgentPackageSkillFlowDocument document)
            : base(
                id,
                AgentMutationKind.SetSkillFlowDocument,
                "set_skill_flow_document",
                path,
                "skill-flow")
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public AgentPackageSkillFlowDocument Document { get; }
    }

}
