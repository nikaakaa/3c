using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public interface IAgentMutationHandler
    {
        bool Preflight(AgentMutationSession session, AgentMutation command);
        void Apply(AgentMutationSession session, AgentMutation command);
    }

    public sealed class AgentMutationHandlerCatalog
    {
        readonly Dictionary<AgentMutationKind, IAgentMutationHandler> m_Handlers =
            new Dictionary<AgentMutationKind, IAgentMutationHandler>();

        public AgentMutationHandlerCatalog()
        {
            Register(new AgentActionEligibilityMutationHandler(),
                AgentMutationKind.EnsureGameplayTag,
                AgentMutationKind.SetActionProfileGrantedTags,
                AgentMutationKind.SetActionProfileCancelQuery,
                AgentMutationKind.SetActionProfileTargetRequirement,
                AgentMutationKind.SetActionRequestTimingClass);
            Register(new BtsmtlSkillAuthoringMutationAdapter(),
                AgentMutationKind.SetSkillFlowDocument);
            Register(new AgentControlConfigurationMutationHandler(),
                AgentMutationKind.ConfigureControlConfiguration);
        }

        public IAgentMutationHandler Get(AgentMutationKind kind)
        {
            if (m_Handlers.TryGetValue(kind, out IAgentMutationHandler handler))
                return handler;
            throw new InvalidOperationException($"Agent Mutation handler is not registered: {kind}");
        }

        void Register(IAgentMutationHandler handler, params AgentMutationKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
            {
                if (m_Handlers.ContainsKey(kinds[i]))
                    throw new InvalidOperationException($"Duplicate Agent Mutation handler: {kinds[i]}");
                m_Handlers.Add(kinds[i], handler);
            }
        }
    }
}
