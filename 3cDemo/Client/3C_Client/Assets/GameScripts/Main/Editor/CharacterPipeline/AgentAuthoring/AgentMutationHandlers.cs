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
            var emitters = new BtsmtlGraphAuthoringCapabilities();
            var conditionBuilder = new AgentConditionRuleBuilder();
            Register(new AgentStateMachineMutationHandler(emitters, conditionBuilder),
                AgentMutationKind.EnsureStateMachine,
                AgentMutationKind.EnsureState,
                AgentMutationKind.DeleteState,
                AgentMutationKind.EnsureTransition,
                AgentMutationKind.RewireTransition,
                AgentMutationKind.EnsureConditionRule);
            Register(new AgentStateBehaviorMutationHandler(emitters, conditionBuilder),
                AgentMutationKind.EnsureActionExitLifecycle,
                AgentMutationKind.DeleteStateBehaviorNode,
                AgentMutationKind.EnsureStateBehaviorNode,
                AgentMutationKind.EnsureTimelineNode,
                AgentMutationKind.EnsureActionActivation,
                AgentMutationKind.EnsureActionLifecycleTransition);
            Register(new AgentNodeAssetMutationHandler(emitters),
                AgentMutationKind.EnsureInputNode,
                AgentMutationKind.EnsureConditionValueNode,
                AgentMutationKind.ConfigureActionAdmission);
            Register(new AgentActionEligibilityMutationHandler(),
                AgentMutationKind.EnsureBlackboardDeclaration,
                AgentMutationKind.MoveBlackboardDeclaration,
                AgentMutationKind.DeleteBlackboardDeclaration,
                AgentMutationKind.SetBlackboardSchemaRevision,
                AgentMutationKind.EnsureExposedPropertyNode,
                AgentMutationKind.EnsureTimelineTreeClip,
                AgentMutationKind.EnsureInlineTimeline,
                AgentMutationKind.EnsureMotionCurveTrack,
                AgentMutationKind.EnsureMotionCurveClip,
                AgentMutationKind.ConfigureMotionCurveClip,
                AgentMutationKind.EnsureMotionWarpTrack,
                AgentMutationKind.DeleteTimelineTrack,
                AgentMutationKind.EnsureTimelineSection,
                AgentMutationKind.DeleteTimelineSection,
                AgentMutationKind.EnsureMotionWarpClip,
                AgentMutationKind.ConfigureMotionWarpSource,
                AgentMutationKind.ConfigureMotionWarpParameters,
                AgentMutationKind.MoveTimelineClip,
                AgentMutationKind.ConfigureTimelineClipEase,
                AgentMutationKind.ConfigureTimelineCurveChannel,
                AgentMutationKind.ConfigureAnimationTrackChannel,
                AgentMutationKind.ConfigureAnimationTrackSlot,
                AgentMutationKind.ConfigureAnimationClipBlendProfile,
                AgentMutationKind.EnsureAnimationClipSegment,
                AgentMutationKind.DeleteTimelineClip,
                AgentMutationKind.EnsureTreeClipBlackboardWrite,
                AgentMutationKind.DeleteTransition,
                AgentMutationKind.EnsureGameplayTag,
                AgentMutationKind.SetActionProfileGrantedTags,
                AgentMutationKind.SetActionProfileCancelQuery,
                AgentMutationKind.SetActionProfileTargetRequirement,
                AgentMutationKind.SetActionRequestTimingClass);
            Register(new AgentSkillFlowDocumentMutationHandler(),
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
