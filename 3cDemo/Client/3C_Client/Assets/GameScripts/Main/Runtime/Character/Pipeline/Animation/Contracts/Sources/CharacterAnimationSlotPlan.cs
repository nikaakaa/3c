using System;
using System.Collections.Generic;
using UnityEngine;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class ActionPlaybackInputPlan
    {
        [SerializeField] int m_Index = -1;
        [SerializeField] int m_ProgramProducerIndex = -1;
        [SerializeField] string m_ProgramProducerId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] int m_SlotIndex = -1;
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] string m_SlotNodeId = string.Empty;
        [SerializeField] int m_ActionPlayerIndex = -1;
        [SerializeField] string m_ActionPlayerNodeId = string.Empty;
        [SerializeField] string m_EndpointId = string.Empty;

        public ActionPlaybackInputPlan(
            int index,
            int programProducerIndex,
            string programProducerId,
            AnimationChannelId animationChannelId,
            int slotIndex,
            AnimationSlotId slotId,
            PoseNodeId slotNodeId,
            int actionPlayerIndex,
            PoseNodeId actionPlayerNodeId,
            TransitionEndpointId endpointId)
        {
            m_Index = index;
            m_ProgramProducerIndex = programProducerIndex;
            m_ProgramProducerId = programProducerId?.Trim() ?? string.Empty;
            m_AnimationChannelId = animationChannelId.Value ?? string.Empty;
            m_SlotIndex = slotIndex;
            m_SlotId = slotId.Value ?? string.Empty;
            m_SlotNodeId = slotNodeId.Value ?? string.Empty;
            m_ActionPlayerIndex = actionPlayerIndex;
            m_ActionPlayerNodeId = actionPlayerNodeId.Value ?? string.Empty;
            m_EndpointId = endpointId.Value ?? string.Empty;
            RequireValid();
        }

        public int Index => m_Index;
        public int ProgramProducerIndex => m_ProgramProducerIndex;
        public string ProgramProducerId => m_ProgramProducerId ?? string.Empty;
        public AnimationChannelId AnimationChannelId =>
            string.IsNullOrWhiteSpace(m_AnimationChannelId)
                ? default
                : new AnimationChannelId(m_AnimationChannelId);
        public int SlotIndex => m_SlotIndex;
        public AnimationSlotId SlotId => string.IsNullOrWhiteSpace(m_SlotId)
            ? default
            : new AnimationSlotId(m_SlotId);
        public PoseNodeId SlotNodeId => string.IsNullOrWhiteSpace(m_SlotNodeId)
            ? default
            : new PoseNodeId(m_SlotNodeId);
        public int ActionPlayerIndex => m_ActionPlayerIndex;
        public PoseNodeId ActionPlayerNodeId =>
            string.IsNullOrWhiteSpace(m_ActionPlayerNodeId)
                ? default
                : new PoseNodeId(m_ActionPlayerNodeId);
        public TransitionEndpointId EndpointId =>
            string.IsNullOrWhiteSpace(m_EndpointId)
                ? default
                : new TransitionEndpointId(m_EndpointId);

        public void RequireValid()
        {
            if (Index < 0 ||
                ProgramProducerIndex < 0 ||
                string.IsNullOrWhiteSpace(ProgramProducerId) ||
                !AnimationChannelId.IsValid ||
                SlotIndex < 0 ||
                !SlotId.IsValid ||
                !SlotNodeId.IsValid ||
                ActionPlayerIndex < 0 ||
                !ActionPlayerNodeId.IsValid ||
                !EndpointId.IsValid ||
                EndpointId.IsSourcePose)
            {
                throw new InvalidOperationException("Compiled Action Playback input plan is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationSlotEndpointDescriptor
    {
        [SerializeField] string m_EndpointId = string.Empty;
        [SerializeField] int m_ProgramProducerIndex = -1;
        [SerializeField] string m_ProgramProducerIdentity = string.Empty;
        [SerializeField] bool m_SourcePose;

        public CharacterAnimationSlotEndpointDescriptor(
            TransitionEndpointId endpointId,
            int programProducerIndex,
            string programProducerIdentity,
            bool sourcePose)
        {
            if (!endpointId.IsValid ||
                sourcePose != endpointId.IsSourcePose ||
                (sourcePose
                    ? programProducerIndex != -1 || !string.IsNullOrEmpty(programProducerIdentity)
                    : programProducerIndex < 0 || string.IsNullOrWhiteSpace(programProducerIdentity)))
            {
                throw new ArgumentException("Compiled Animation Slot endpoint is invalid.");
            }
            m_EndpointId = endpointId.Value;
            m_ProgramProducerIndex = programProducerIndex;
            m_ProgramProducerIdentity = programProducerIdentity ?? string.Empty;
            m_SourcePose = sourcePose;
        }

        public TransitionEndpointId EndpointId => new TransitionEndpointId(m_EndpointId);
        public int ProgramProducerIndex => m_ProgramProducerIndex;
        public string ProgramProducerIdentity => m_ProgramProducerIdentity ?? string.Empty;
        public bool SourcePose => m_SourcePose;
    }

    [Serializable]
    public sealed class CharacterAnimationSlotActionPlayerDescriptor
    {
        [SerializeField] string m_PlayerNodeId = string.Empty;
        [SerializeField] int m_ActionPlaybackOperationIndex = -1;
        [SerializeField] int m_PlayerIndex = -1;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] bool m_AllowNoAction;

        public CharacterAnimationSlotActionPlayerDescriptor(
            PoseNodeId playerNodeId,
            int actionPlaybackOperationIndex,
            int playerIndex,
            AnimationChannelId animationChannelId,
            bool allowNoAction)
        {
            m_PlayerNodeId = playerNodeId.Value ?? string.Empty;
            m_ActionPlaybackOperationIndex = actionPlaybackOperationIndex;
            m_PlayerIndex = playerIndex;
            m_AnimationChannelId = animationChannelId.Value ?? string.Empty;
            m_AllowNoAction = allowNoAction;
            RequireValid();
        }

        public PoseNodeId PlayerNodeId => string.IsNullOrWhiteSpace(m_PlayerNodeId)
            ? default
            : new PoseNodeId(m_PlayerNodeId);
        public int ActionPlaybackOperationIndex => m_ActionPlaybackOperationIndex;
        public int PlayerIndex => m_PlayerIndex;
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId)
            ? default
            : new AnimationChannelId(m_AnimationChannelId);
        public bool AllowNoAction => m_AllowNoAction;

        public void RequireValid()
        {
            if (!PlayerNodeId.IsValid || ActionPlaybackOperationIndex < 0 || PlayerIndex < 0 ||
                !AnimationChannelId.IsValid || !AllowNoAction)
            {
                throw new InvalidOperationException("Compiled Animation Slot Action player is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationSlotBlendStackWorkspaceDescriptor
    {
        [SerializeField] int m_BlendNodeIndex = -1;
        [SerializeField] int m_Capacity;

        public CharacterAnimationSlotBlendStackWorkspaceDescriptor(int blendNodeIndex, int capacity)
        {
            m_BlendNodeIndex = blendNodeIndex;
            m_Capacity = capacity;
            RequireValid();
        }

        public int BlendNodeIndex => m_BlendNodeIndex;
        public int Capacity => m_Capacity;

        public void RequireValid()
        {
            if (BlendNodeIndex < 0 || Capacity < 2)
                throw new InvalidOperationException("Compiled Animation Slot BlendStack workspace is invalid.");
        }
    }

    [Serializable]
    public sealed class CharacterAnimationSlotSourceUsagePlan
    {
        [SerializeField] int m_SourcePoseValueIndex = -1;
        [SerializeField] int m_ActionPlaybackOperationIndex = -1;
        [SerializeField] int m_ActionPlayerIndex = -1;
        [SerializeField] bool m_KeepSourcePoseUpdating;

        public CharacterAnimationSlotSourceUsagePlan(
            int sourcePoseValueIndex,
            int actionPlaybackOperationIndex,
            int actionPlayerIndex,
            bool keepSourcePoseUpdating)
        {
            m_SourcePoseValueIndex = sourcePoseValueIndex;
            m_ActionPlaybackOperationIndex = actionPlaybackOperationIndex;
            m_ActionPlayerIndex = actionPlayerIndex;
            m_KeepSourcePoseUpdating = keepSourcePoseUpdating;
            RequireValid();
        }

        public int SourcePoseValueIndex => m_SourcePoseValueIndex;
        public int ActionPlaybackOperationIndex => m_ActionPlaybackOperationIndex;
        public int ActionPlayerIndex => m_ActionPlayerIndex;
        public bool KeepSourcePoseUpdating => m_KeepSourcePoseUpdating;

        public void RequireValid()
        {
            if (SourcePoseValueIndex < 0 || ActionPlaybackOperationIndex < 0 ||
                ActionPlayerIndex < 0 || !KeepSourcePoseUpdating)
            {
                throw new InvalidOperationException("Compiled Animation Slot source usage is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationSlotReleasePlan
    {
        [SerializeField] string m_SourcePoseEndpointId = string.Empty;
        [SerializeField] bool m_ReturnToCurrentSourcePose;
        [SerializeField] bool m_RequiresRoutingPermission;
        [SerializeField] bool m_ReleaseActionSourceAfterPermission;

        public CharacterAnimationSlotReleasePlan(
            TransitionEndpointId sourcePoseEndpointId,
            bool returnToCurrentSourcePose,
            bool requiresRoutingPermission,
            bool releaseActionSourceAfterPermission)
        {
            m_SourcePoseEndpointId = sourcePoseEndpointId.Value ?? string.Empty;
            m_ReturnToCurrentSourcePose = returnToCurrentSourcePose;
            m_RequiresRoutingPermission = requiresRoutingPermission;
            m_ReleaseActionSourceAfterPermission = releaseActionSourceAfterPermission;
            RequireValid();
        }

        public TransitionEndpointId SourcePoseEndpointId => new TransitionEndpointId(m_SourcePoseEndpointId);
        public bool ReturnToCurrentSourcePose => m_ReturnToCurrentSourcePose;
        public bool RequiresRoutingPermission => m_RequiresRoutingPermission;
        public bool ReleaseActionSourceAfterPermission => m_ReleaseActionSourceAfterPermission;

        public void RequireValid()
        {
            if (!SourcePoseEndpointId.IsSourcePose || !ReturnToCurrentSourcePose ||
                !RequiresRoutingPermission || !ReleaseActionSourceAfterPermission)
            {
                throw new InvalidOperationException("Compiled Animation Slot release plan is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationSlotRequestRouteDescriptor
    {
        [SerializeField] string m_RuleId = string.Empty;
        [SerializeField] string m_SourceEndpointId = string.Empty;
        [SerializeField] string m_TargetEndpointId = string.Empty;
        [SerializeField] AnimationTransitionBlendLogic m_BlendLogic;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] int m_CurveIndex = -1;
        [SerializeField] int m_BlendProfileIndex = -1;
        [SerializeField] bool m_RequiresTargetFirstSample;
        [SerializeField] bool m_RequiresCaptureCompletion;

        public CharacterAnimationSlotRequestRouteDescriptor(
            TransitionRuleId ruleId,
            TransitionEndpointId sourceEndpointId,
            TransitionEndpointId targetEndpointId,
            AnimationTransitionBlendLogic blendLogic,
            float durationSeconds,
            int curveIndex,
            int blendProfileIndex,
            bool requiresTargetFirstSample,
            bool requiresCaptureCompletion)
        {
            m_RuleId = ruleId.Value ?? string.Empty;
            m_SourceEndpointId = sourceEndpointId.Value ?? string.Empty;
            m_TargetEndpointId = targetEndpointId.Value ?? string.Empty;
            m_BlendLogic = blendLogic;
            m_DurationSeconds = durationSeconds;
            m_CurveIndex = curveIndex;
            m_BlendProfileIndex = blendProfileIndex;
            m_RequiresTargetFirstSample = requiresTargetFirstSample;
            m_RequiresCaptureCompletion = requiresCaptureCompletion;
            RequireValid();
        }

        public TransitionRuleId RuleId => new TransitionRuleId(m_RuleId);
        public TransitionEndpointId SourceEndpointId => new TransitionEndpointId(m_SourceEndpointId);
        public TransitionEndpointId TargetEndpointId => new TransitionEndpointId(m_TargetEndpointId);
        public AnimationTransitionBlendLogic BlendLogic => m_BlendLogic;
        public float DurationSeconds => m_DurationSeconds;
        public int CurveIndex => m_CurveIndex;
        public int BlendProfileIndex => m_BlendProfileIndex;
        public bool RequiresTargetFirstSample => m_RequiresTargetFirstSample;
        public bool RequiresCaptureCompletion => m_RequiresCaptureCompletion;

        public void RequireValid()
        {
            if (!RuleId.IsValid || !SourceEndpointId.IsValid || !TargetEndpointId.IsValid ||
                !Enum.IsDefined(typeof(AnimationTransitionBlendLogic), BlendLogic) ||
                !float.IsFinite(DurationSeconds) || DurationSeconds < 0f ||
                CurveIndex < 0 || BlendProfileIndex < 0 ||
                RequiresCaptureCompletion != (BlendLogic == AnimationTransitionBlendLogic.Inertialization))
            {
                throw new InvalidOperationException("Compiled Animation Slot request route is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationSlotDescriptor
    {
        public const string SchemaVersion = "character-animation-slot/v3";

        [SerializeField] int m_Index = -1;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] string m_RoutingOwnerId = string.Empty;
        [SerializeField] CompiledTransitionRoutingPlanPayload m_RoutingPlan;
        [SerializeField] CharacterAnimationSlotEndpointDescriptor[] m_Endpoints =
            Array.Empty<CharacterAnimationSlotEndpointDescriptor>();
        [SerializeField] CharacterAnimationSlotRequestRouteDescriptor[] m_RequestRoutes =
            Array.Empty<CharacterAnimationSlotRequestRouteDescriptor>();
        [SerializeField] CharacterAnimationSlotActionPlayerDescriptor m_ActionPlayer;
        [SerializeField] CharacterAnimationSlotBlendStackWorkspaceDescriptor m_BlendStackWorkspace;
        [SerializeField] CharacterAnimationSlotSourceUsagePlan m_SourceUsage;
        [SerializeField] CharacterAnimationSlotReleasePlan m_ReleasePlan;

        public CharacterAnimationSlotDescriptor(
            int index,
            PoseNodeId nodeId,
            AnimationSlotId slotId,
            AnimationChannelId animationChannelId,
            TransitionRouteOwnerId routingOwnerId,
            CompiledTransitionRoutingPlanPayload routingPlan,
            CharacterAnimationSlotEndpointDescriptor[] endpoints,
            CharacterAnimationSlotRequestRouteDescriptor[] requestRoutes,
            CharacterAnimationSlotActionPlayerDescriptor actionPlayer,
            CharacterAnimationSlotBlendStackWorkspaceDescriptor blendStackWorkspace,
            CharacterAnimationSlotSourceUsagePlan sourceUsage,
            CharacterAnimationSlotReleasePlan releasePlan)
        {
            m_Index = index;
            m_NodeId = nodeId.Value ?? string.Empty;
            m_SlotId = slotId.Value ?? string.Empty;
            m_AnimationChannelId = animationChannelId.Value ?? string.Empty;
            m_RoutingOwnerId = routingOwnerId.Value ?? string.Empty;
            m_RoutingPlan = routingPlan ??
                throw new ArgumentNullException(nameof(routingPlan));
            m_Endpoints = endpoints ?? Array.Empty<CharacterAnimationSlotEndpointDescriptor>();
            m_RequestRoutes = requestRoutes ?? Array.Empty<CharacterAnimationSlotRequestRouteDescriptor>();
            m_ActionPlayer = actionPlayer;
            m_BlendStackWorkspace = blendStackWorkspace;
            m_SourceUsage = sourceUsage;
            m_ReleasePlan = releasePlan;
            RequireValid();
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public AnimationSlotId SlotId => string.IsNullOrWhiteSpace(m_SlotId) ? default : new AnimationSlotId(m_SlotId);
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId)
            ? default
            : new AnimationChannelId(m_AnimationChannelId);
        public TransitionRouteOwnerId RoutingOwnerId => new TransitionRouteOwnerId(m_RoutingOwnerId);
        public string RoutingPlanId =>
            m_RoutingPlan?.PlanId ?? string.Empty;
        public string RoutingDefinitionRevision =>
            m_RoutingPlan?.DefinitionRevision ?? string.Empty;
        public IReadOnlyList<CharacterAnimationSlotEndpointDescriptor> Endpoints =>
            m_Endpoints ?? Array.Empty<CharacterAnimationSlotEndpointDescriptor>();
        public IReadOnlyList<CharacterAnimationSlotRequestRouteDescriptor> RequestRoutes =>
            m_RequestRoutes ?? Array.Empty<CharacterAnimationSlotRequestRouteDescriptor>();
        public CharacterAnimationSlotActionPlayerDescriptor ActionPlayer => m_ActionPlayer;
        public CharacterAnimationSlotBlendStackWorkspaceDescriptor BlendStackWorkspace => m_BlendStackWorkspace;
        public CharacterAnimationSlotSourceUsagePlan SourceUsage => m_SourceUsage;
        public CharacterAnimationSlotReleasePlan ReleasePlan => m_ReleasePlan;

        public CompiledTransitionRoutingPlan LoadRoutingPlan()
        {
            CompiledTransitionRoutingPlan plan =
                m_RoutingPlan?.Load() ??
                throw new InvalidOperationException(
                    "Compiled Animation Slot Routing Plan is missing.");
            if (!string.Equals(
                    plan.PlanId.ToString(),
                    RoutingPlanId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    plan.DefinitionRevision.ToString(),
                    RoutingDefinitionRevision,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Compiled Animation Slot Routing Plan identity is inconsistent.");
            }
            if (plan.CoveragePolicy !=
                    TransitionRoutingCoveragePolicy.CompleteMatrix ||
                plan.Endpoints.Count != Endpoints.Count ||
                plan.Rules.Count != RequestRoutes.Count)
            {
                throw new InvalidOperationException(
                    "Compiled Animation Slot Routing Plan shape is inconsistent.");
            }
            var endpointSet =
                new HashSet<TransitionEndpointId>();
            for (int i = 0; i < Endpoints.Count; i++)
                endpointSet.Add(Endpoints[i].EndpointId);
            for (int i = 0; i < plan.Endpoints.Count; i++)
            {
                if (!endpointSet.Contains(plan.Endpoints[i]))
                {
                    throw new InvalidOperationException(
                        "Compiled Animation Slot Routing Plan contains an unknown endpoint.");
                }
            }
            for (int i = 0; i < RequestRoutes.Count; i++)
            {
                CharacterAnimationSlotRequestRouteDescriptor route =
                    RequestRoutes[i];
                if (!plan.TryGetRule(
                        route.SourceEndpointId,
                        route.TargetEndpointId,
                        out AnimationTransitionRule rule) ||
                    rule.RuleId != route.RuleId ||
                    rule.BlendLogic != route.BlendLogic ||
                    rule.DurationSeconds !=
                        route.DurationSeconds ||
                    !rule.BlendCurveId.Equals(
                        new TransitionBlendCurveId(
                            $"curve/{route.CurveIndex}")) ||
                    !rule.BlendProfileId.Equals(
                        new TransitionBlendProfileId(
                            $"profile/{route.BlendProfileIndex}")))
                {
                    throw new InvalidOperationException(
                        $"Compiled Animation Slot route '{route.RuleId}' does not match its Routing Plan.");
                }
            }
            return plan;
        }

        public void RequireValid()
        {
            if (Index < 0 || !NodeId.IsValid || !SlotId.IsValid || !AnimationChannelId.IsValid ||
                !RoutingOwnerId.IsValid || m_RoutingPlan == null ||
                string.IsNullOrWhiteSpace(RoutingPlanId) ||
                string.IsNullOrWhiteSpace(RoutingDefinitionRevision) ||
                ActionPlayer == null || BlendStackWorkspace == null ||
                SourceUsage == null || ReleasePlan == null)
            {
                throw new InvalidOperationException("Compiled Animation Slot descriptor is invalid.");
            }
            ActionPlayer.RequireValid();
            BlendStackWorkspace.RequireValid();
            SourceUsage.RequireValid();
            ReleasePlan.RequireValid();
            if (ActionPlayer.AnimationChannelId != AnimationChannelId ||
                ActionPlayer.ActionPlaybackOperationIndex != SourceUsage.ActionPlaybackOperationIndex ||
                ActionPlayer.PlayerIndex != SourceUsage.ActionPlayerIndex)
            {
                throw new InvalidOperationException("Compiled Animation Slot Action player ownership is inconsistent.");
            }

            var endpointIds = new HashSet<TransitionEndpointId>();
            var producerIndices = new HashSet<int>();
            var producerIdentities = new HashSet<string>(StringComparer.Ordinal);
            int sourcePoseCount = 0;
            for (int i = 0; i < Endpoints.Count; i++)
            {
                CharacterAnimationSlotEndpointDescriptor endpoint = Endpoints[i];
                if (endpoint == null || !endpoint.EndpointId.IsValid || !endpointIds.Add(endpoint.EndpointId))
                    throw new InvalidOperationException($"Compiled Animation Slot endpoint #{i} is invalid or duplicated.");
                if (endpoint.SourcePose)
                    sourcePoseCount++;
                else if (!producerIndices.Add(endpoint.ProgramProducerIndex) ||
                         !producerIdentities.Add(endpoint.ProgramProducerIdentity))
                    throw new InvalidOperationException($"Compiled Animation Slot producer endpoint #{i} is duplicated.");
            }
            if (sourcePoseCount != 1 || Endpoints.Count < 2)
                throw new InvalidOperationException("Compiled Animation Slot requires Source Pose and at least one Action endpoint.");

            var routeIds = new HashSet<TransitionRuleId>();
            var routePairs = new HashSet<(TransitionEndpointId Source, TransitionEndpointId Target)>();
            for (int i = 0; i < RequestRoutes.Count; i++)
            {
                CharacterAnimationSlotRequestRouteDescriptor route = RequestRoutes[i];
                route?.RequireValid();
                if (route == null || !routeIds.Add(route.RuleId) ||
                    !endpointIds.Contains(route.SourceEndpointId) ||
                    !endpointIds.Contains(route.TargetEndpointId) ||
                    !routePairs.Add((route.SourceEndpointId, route.TargetEndpointId)))
                {
                    throw new InvalidOperationException($"Compiled Animation Slot request route #{i} is invalid or duplicated.");
                }
            }
            LoadRoutingPlan();
            if (RequestRoutes.Count != checked(Endpoints.Count * Endpoints.Count))
                throw new InvalidOperationException("Compiled Animation Slot request routes do not form a complete exact matrix.");
            for (int source = 0; source < Endpoints.Count; source++)
            {
                for (int target = 0; target < Endpoints.Count; target++)
                {
                    if (!routePairs.Contains((Endpoints[source].EndpointId, Endpoints[target].EndpointId)))
                        throw new InvalidOperationException("Compiled Animation Slot request routes are missing an exact pair.");
                }
            }
        }
    }
}
