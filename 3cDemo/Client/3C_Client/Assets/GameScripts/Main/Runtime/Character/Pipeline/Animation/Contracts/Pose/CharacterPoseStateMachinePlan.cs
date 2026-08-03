using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PoseStateSourceSyncMode : byte
    {
        None = 1,
        MarkerGroup = 2
    }

    [Serializable]
    public sealed class PoseStateSourceProviderPlan
    {
        [SerializeField] int m_StateIndex = -1;
        [SerializeField] int m_OperationIndex = -1;
        [SerializeField] int m_PlayerIndex = -1;
        [SerializeField] string m_ProviderId = string.Empty;
        [SerializeField] string m_PlayerNodeId = string.Empty;
        [SerializeField] AnimationPoseSourceKind m_SourceKind;
        [SerializeField] int m_PresentationPoseSourceIndex = -1;

        public int StateIndex => m_StateIndex;
        public int OperationIndex => m_OperationIndex;
        public int PlayerIndex => m_PlayerIndex;
        public PresentationPoseSourceProviderId ProviderId =>
            string.IsNullOrWhiteSpace(m_ProviderId)
                ? default
                : new PresentationPoseSourceProviderId(m_ProviderId);
        public PoseNodeId PlayerNodeId => string.IsNullOrWhiteSpace(m_PlayerNodeId)
            ? default
            : new PoseNodeId(m_PlayerNodeId);
        public AnimationPoseSourceKind SourceKind => m_SourceKind;
        public PresentationPoseSourceIndex PresentationPoseSourceIndex =>
            m_PresentationPoseSourceIndex < 0
                ? default
                : new PresentationPoseSourceIndex(m_PresentationPoseSourceIndex);

        public PoseStateSourceProviderPlan(
            int stateIndex,
            int operationIndex,
            int playerIndex,
            PresentationPoseSourceProviderId providerId,
            PoseNodeId playerNodeId,
            AnimationPoseSourceKind sourceKind,
            PresentationPoseSourceIndex presentationPoseSourceIndex)
        {
            if (stateIndex < 0 || operationIndex < 0 || playerIndex < 0 ||
                !providerId.IsValid || !playerNodeId.IsValid ||
                sourceKind != AnimationPoseSourceKind.Sequence &&
                sourceKind != AnimationPoseSourceKind.BlendSpace &&
                sourceKind != AnimationPoseSourceKind.MotionMatching)
            {
                throw new ArgumentException("Pose State source usage plan is invalid.");
            }
            if (!presentationPoseSourceIndex.IsValid)
                throw new ArgumentException("Pose State source usage identity does not match its source kind.");
            m_StateIndex = stateIndex;
            m_OperationIndex = operationIndex;
            m_PlayerIndex = playerIndex;
            m_ProviderId = providerId.Value;
            m_PlayerNodeId = playerNodeId.Value;
            m_SourceKind = sourceKind;
            m_PresentationPoseSourceIndex = presentationPoseSourceIndex.Value;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateDescriptor
    {
        [SerializeField] int m_Index = -1;
        [SerializeField] string m_StateId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] int m_OutputPoseValueIndex = -1;
        [SerializeField] int m_OperationStart = -1;
        [SerializeField] int m_OperationCount;
        [SerializeField] bool m_AlwaysResetOnEntry;
        [SerializeField] PoseStateSourceProviderPlan[] m_SourceProviders =
            Array.Empty<PoseStateSourceProviderPlan>();
        [SerializeField] int[] m_RelevantPlayerIndices = Array.Empty<int>();

        public int Index => m_Index;
        public PoseStateId StateId => string.IsNullOrWhiteSpace(m_StateId)
            ? default
            : new PoseStateId(m_StateId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public int OutputPoseValueIndex => m_OutputPoseValueIndex;
        public int OperationStart => m_OperationStart;
        public int OperationCount => m_OperationCount;
        public bool AlwaysResetOnEntry => m_AlwaysResetOnEntry;
        public IReadOnlyList<PoseStateSourceProviderPlan> SourceProviders =>
            m_SourceProviders ?? Array.Empty<PoseStateSourceProviderPlan>();
        public IReadOnlyList<int> RelevantPlayerIndices =>
            m_RelevantPlayerIndices ?? Array.Empty<int>();

        public CharacterPoseStateDescriptor(
            int index,
            PoseStateId stateId,
            string displayName,
            int outputPoseValueIndex,
            int operationStart,
            int operationCount,
            bool alwaysResetOnEntry,
            PoseStateSourceProviderPlan[] sourceProviders)
        {
            if (index < 0 || !stateId.IsValid || outputPoseValueIndex < 0 ||
                operationStart < 0 || operationCount <= 0)
            {
                throw new ArgumentException("Compiled Pose State descriptor is invalid.");
            }
            m_Index = index;
            m_StateId = stateId.Value;
            m_DisplayName = displayName ?? string.Empty;
            m_OutputPoseValueIndex = outputPoseValueIndex;
            m_OperationStart = operationStart;
            m_OperationCount = operationCount;
            m_AlwaysResetOnEntry = alwaysResetOnEntry;
            m_SourceProviders = sourceProviders ?? Array.Empty<PoseStateSourceProviderPlan>();
            m_RelevantPlayerIndices = m_SourceProviders
                .Select(value => value.PlayerIndex)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateSourceSyncPlan
    {
        [SerializeField] PoseStateSourceSyncMode m_Mode;
        [SerializeField] string m_RelationId = string.Empty;
        [SerializeField] int m_SourcePlayerIndex = -1;
        [SerializeField] int m_TargetPlayerIndex = -1;
        [SerializeField] int m_SourcePoseSourceIndex = -1;
        [SerializeField] int m_TargetPoseSourceIndex = -1;
        [SerializeField] string m_CanonicalGroupId = string.Empty;
        [SerializeField] bool m_SourceIsLeader;

        public PoseStateSourceSyncMode Mode => m_Mode;
        public string RelationId => m_RelationId ?? string.Empty;
        public int SourcePlayerIndex => m_SourcePlayerIndex;
        public int TargetPlayerIndex => m_TargetPlayerIndex;
        public PresentationPoseSourceIndex SourcePoseSourceIndex => m_SourcePoseSourceIndex < 0
            ? default
            : new PresentationPoseSourceIndex(m_SourcePoseSourceIndex);
        public PresentationPoseSourceIndex TargetPoseSourceIndex => m_TargetPoseSourceIndex < 0
            ? default
            : new PresentationPoseSourceIndex(m_TargetPoseSourceIndex);
        public string CanonicalGroupId => m_CanonicalGroupId ?? string.Empty;
        public bool SourceIsLeader => m_SourceIsLeader;

        public CharacterPoseStateSourceSyncPlan(PoseStateSourceSyncMode mode)
        {
            if (mode != PoseStateSourceSyncMode.None)
                throw new ArgumentOutOfRangeException(nameof(mode));
            m_Mode = mode;
        }

        public CharacterPoseStateSourceSyncPlan(
            string relationId,
            int sourcePlayerIndex,
            int targetPlayerIndex,
            PresentationPoseSourceIndex sourcePoseSourceIndex,
            PresentationPoseSourceIndex targetPoseSourceIndex,
            string canonicalGroupId,
            bool sourceIsLeader)
        {
            if (string.IsNullOrWhiteSpace(relationId) || sourcePlayerIndex < 0 || targetPlayerIndex < 0 ||
                !sourcePoseSourceIndex.IsValid || !targetPoseSourceIndex.IsValid ||
                string.IsNullOrWhiteSpace(canonicalGroupId))
            {
                throw new ArgumentException("Pose State MarkerGroup sync plan is invalid.");
            }
            m_Mode = PoseStateSourceSyncMode.MarkerGroup;
            m_RelationId = relationId.Trim();
            m_SourcePlayerIndex = sourcePlayerIndex;
            m_TargetPlayerIndex = targetPlayerIndex;
            m_SourcePoseSourceIndex = sourcePoseSourceIndex.Value;
            m_TargetPoseSourceIndex = targetPoseSourceIndex.Value;
            m_CanonicalGroupId = canonicalGroupId.Trim();
            m_SourceIsLeader = sourceIsLeader;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateTransitionDescriptor
    {
        [SerializeField] int m_Index = -1;
        [SerializeField] string m_TransitionId = string.Empty;
        [SerializeField] int m_SourceStateIndex = -1;
        [SerializeField] int m_TargetStateIndex = -1;
        [SerializeField] int m_Priority;
        [SerializeField] CharacterPoseTransitionRuleProgram m_Rule;
        [SerializeField] AnimationTransitionBlendLogic m_BlendLogic;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] float m_CompletionDurationSeconds;
        [SerializeField] CharacterAnimationBlendMode m_BlendMode;
        [SerializeField] int m_CurveIndex = -1;
        [SerializeField] int m_BlendProfileIndex = -1;
        [SerializeField] string m_RoutingRuleId = string.Empty;
        [SerializeField] CharacterPoseStateSourceSyncPlan m_SourceSync;

        public int Index => m_Index;
        public PoseStateTransitionId TransitionId => string.IsNullOrWhiteSpace(m_TransitionId)
            ? default
            : new PoseStateTransitionId(m_TransitionId);
        public int SourceStateIndex => m_SourceStateIndex;
        public int TargetStateIndex => m_TargetStateIndex;
        public int Priority => m_Priority;
        public CharacterPoseTransitionRuleProgram Rule => m_Rule;
        public AnimationTransitionBlendLogic BlendLogic => m_BlendLogic;
        public float DurationSeconds => m_DurationSeconds;
        public float CompletionDurationSeconds => m_CompletionDurationSeconds;
        public CharacterAnimationBlendMode BlendMode => m_BlendMode;
        public int CurveIndex => m_CurveIndex;
        public int BlendProfileIndex => m_BlendProfileIndex;
        public TransitionRuleId RoutingRuleId => string.IsNullOrWhiteSpace(m_RoutingRuleId)
            ? default
            : new TransitionRuleId(m_RoutingRuleId);
        public CharacterPoseStateSourceSyncPlan SourceSync => m_SourceSync;

        public CharacterPoseStateTransitionDescriptor(
            int index,
            PoseStateTransitionId transitionId,
            int sourceStateIndex,
            int targetStateIndex,
            int priority,
            CharacterPoseTransitionRuleProgram rule,
            AnimationTransitionBlendLogic blendLogic,
            float durationSeconds,
            float completionDurationSeconds,
            CharacterAnimationBlendMode blendMode,
            int curveIndex,
            int blendProfileIndex,
            TransitionRuleId routingRuleId,
            CharacterPoseStateSourceSyncPlan sourceSync)
        {
            if (index < 0 || !transitionId.IsValid || sourceStateIndex < 0 || targetStateIndex < 0 ||
                priority < 0 || rule == null ||
                !Enum.IsDefined(typeof(AnimationTransitionBlendLogic), blendLogic) ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendMode), blendMode) ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f ||
                !float.IsFinite(completionDurationSeconds) ||
                completionDurationSeconds < 0f ||
                curveIndex < 0 ||
                (blendProfileIndex < 0 &&
                 !(blendLogic == AnimationTransitionBlendLogic.StandardBlend && durationSeconds == 0f)) ||
                !routingRuleId.IsValid || sourceSync == null)
            {
                throw new ArgumentException("Compiled Pose State transition descriptor is invalid.");
            }
            rule.RequireValid();
            m_Index = index;
            m_TransitionId = transitionId.Value;
            m_SourceStateIndex = sourceStateIndex;
            m_TargetStateIndex = targetStateIndex;
            m_Priority = priority;
            m_Rule = rule;
            m_BlendLogic = blendLogic;
            m_DurationSeconds = durationSeconds;
            m_CompletionDurationSeconds = completionDurationSeconds;
            m_BlendMode = blendMode;
            m_CurveIndex = curveIndex;
            m_BlendProfileIndex = blendProfileIndex;
            m_RoutingRuleId = routingRuleId.Value;
            m_SourceSync = sourceSync;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateMachineDescriptor
    {
        public const string SchemaVersion = "character-pose-state-machine/v4";

        [SerializeField] int m_Index = -1;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_StateMachineId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] int m_EntryStateIndex = -1;
        [SerializeField] int m_MaxTransitionsPerFrame;
        [SerializeField] CharacterPoseStateDescriptor[] m_States = Array.Empty<CharacterPoseStateDescriptor>();
        [SerializeField] CharacterPoseStateTransitionDescriptor[] m_Transitions =
            Array.Empty<CharacterPoseStateTransitionDescriptor>();
        [SerializeField] int m_StateWorkspaceCount;
        [SerializeField] int m_TransitionWorkspaceCount;
        [SerializeField] CompiledTransitionRoutingPlanPayload m_RoutingPlan;

        public int Index => m_Index;
        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public PoseStateMachineId StateMachineId => string.IsNullOrWhiteSpace(m_StateMachineId)
            ? default
            : new PoseStateMachineId(m_StateMachineId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public int EntryStateIndex => m_EntryStateIndex;
        public int MaxTransitionsPerFrame => m_MaxTransitionsPerFrame;
        public IReadOnlyList<CharacterPoseStateDescriptor> States =>
            m_States ?? Array.Empty<CharacterPoseStateDescriptor>();
        public IReadOnlyList<CharacterPoseStateTransitionDescriptor> Transitions =>
            m_Transitions ?? Array.Empty<CharacterPoseStateTransitionDescriptor>();
        public int StateWorkspaceCount => m_StateWorkspaceCount;
        public int TransitionWorkspaceCount => m_TransitionWorkspaceCount;
        public string RoutingPlanId =>
            m_RoutingPlan?.PlanId ?? string.Empty;
        public string RoutingDefinitionRevision =>
            m_RoutingPlan?.DefinitionRevision ?? string.Empty;

        public CharacterPoseStateMachineDescriptor(
            int index,
            PoseNodeId nodeId,
            PoseStateMachineId stateMachineId,
            string contentRevision,
            int entryStateIndex,
            int maxTransitionsPerFrame,
            CharacterPoseStateDescriptor[] states,
            CharacterPoseStateTransitionDescriptor[] transitions,
            CompiledTransitionRoutingPlanPayload routingPlan)
        {
            m_Index = index;
            m_NodeId = nodeId.Value ?? string.Empty;
            m_StateMachineId = stateMachineId.Value ?? string.Empty;
            m_ContentRevision = contentRevision ?? string.Empty;
            m_EntryStateIndex = entryStateIndex;
            m_MaxTransitionsPerFrame = maxTransitionsPerFrame;
            m_States = states ?? Array.Empty<CharacterPoseStateDescriptor>();
            m_Transitions = transitions ?? Array.Empty<CharacterPoseStateTransitionDescriptor>();
            m_StateWorkspaceCount = m_States.Length;
            m_TransitionWorkspaceCount = m_Transitions.Length;
            m_RoutingPlan = routingPlan ??
                throw new ArgumentNullException(nameof(routingPlan));
            RequireValid();
        }

        public CompiledTransitionRoutingPlan LoadRoutingPlan()
        {
            CompiledTransitionRoutingPlan plan =
                m_RoutingPlan?.Load() ??
                throw new InvalidOperationException(
                    "Compiled Pose StateMachine Routing Plan is missing.");
            if (!string.Equals(
                    plan.PlanId.ToString(),
                    RoutingPlanId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    plan.DefinitionRevision.ToString(),
                    RoutingDefinitionRevision,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    plan.DefinitionRevision.ToString(),
                    ContentRevision,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Compiled Pose StateMachine Routing Plan identity is inconsistent.");
            }
            if (plan.CoveragePolicy !=
                    TransitionRoutingCoveragePolicy.DeclaredRules ||
                plan.Endpoints.Count != States.Count ||
                plan.Rules.Count != Transitions.Count)
            {
                throw new InvalidOperationException(
                    "Compiled Pose StateMachine Routing Plan shape is inconsistent.");
            }
            var endpoints =
                new TransitionEndpointId[States.Count];
            var endpointSet =
                new HashSet<TransitionEndpointId>();
            for (int i = 0; i < States.Count; i++)
            {
                endpoints[i] = new TransitionEndpointId(
                    $"pose-state/{StateMachineId}/{States[i].StateId}");
                endpointSet.Add(endpoints[i]);
            }
            for (int i = 0; i < plan.Endpoints.Count; i++)
            {
                if (!endpointSet.Contains(plan.Endpoints[i]))
                {
                    throw new InvalidOperationException(
                        "Compiled Pose StateMachine Routing Plan contains an unknown endpoint.");
                }
            }
            for (int i = 0; i < Transitions.Count; i++)
            {
                CharacterPoseStateTransitionDescriptor transition =
                    Transitions[i];
                TransitionEndpointId source =
                    endpoints[transition.SourceStateIndex];
                TransitionEndpointId target =
                    endpoints[transition.TargetStateIndex];
                if (!plan.TryGetRule(
                        source,
                        target,
                        out AnimationTransitionRule rule) ||
                    rule.RuleId != transition.RoutingRuleId ||
                    rule.BlendLogic != transition.BlendLogic ||
                    rule.DurationSeconds !=
                        transition.DurationSeconds ||
                    !rule.BlendCurveId.Equals(
                        new TransitionBlendCurveId(
                            $"curve/{transition.CurveIndex}")) ||
                    !rule.BlendProfileId.Equals(
                        new TransitionBlendProfileId(
                            $"profile/{transition.BlendProfileIndex}")))
                {
                    throw new InvalidOperationException(
                        $"Compiled Pose StateMachine transition '{transition.TransitionId}' does not match its Routing Plan.");
                }
            }
            return plan;
        }

        public void RequireValid()
        {
            if (Index < 0 || !NodeId.IsValid || !StateMachineId.IsValid ||
                string.IsNullOrWhiteSpace(ContentRevision) ||
                States.Count == 0 || (uint)EntryStateIndex >= (uint)States.Count ||
                MaxTransitionsPerFrame <= 0 || StateWorkspaceCount != States.Count ||
                TransitionWorkspaceCount != Transitions.Count ||
                m_RoutingPlan == null ||
                string.IsNullOrWhiteSpace(RoutingPlanId) ||
                string.IsNullOrWhiteSpace(RoutingDefinitionRevision))
            {
                throw new InvalidOperationException("Compiled Pose StateMachine descriptor is invalid.");
            }
            for (int i = 0; i < States.Count; i++)
            {
                CharacterPoseStateDescriptor state = States[i];
                if (state == null || state.Index != i)
                    throw new InvalidOperationException($"Compiled Pose State #{i} is invalid.");
            }
            for (int i = 0; i < Transitions.Count; i++)
            {
                CharacterPoseStateTransitionDescriptor transition = Transitions[i];
                if (transition == null || transition.Index != i ||
                    (uint)transition.SourceStateIndex >= (uint)States.Count ||
                    (uint)transition.TargetStateIndex >= (uint)States.Count ||
                    !Enum.IsDefined(typeof(CharacterAnimationBlendMode), transition.BlendMode) ||
                    transition.CurveIndex < 0 ||
                    (transition.BlendProfileIndex < 0 &&
                     !(transition.BlendLogic == AnimationTransitionBlendLogic.StandardBlend &&
                       transition.DurationSeconds == 0f)))
                {
                    throw new InvalidOperationException($"Compiled Pose State transition #{i} is invalid.");
                }
            }
            LoadRoutingPlan();
        }
    }
}
