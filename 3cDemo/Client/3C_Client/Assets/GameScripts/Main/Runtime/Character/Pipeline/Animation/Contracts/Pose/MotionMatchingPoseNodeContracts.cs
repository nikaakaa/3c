using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterMotionMatchingPoseNodeKinds
    {
        public const CharacterPoseNodeKind MotionMatchingPose = CharacterPoseNodeKind.MotionMatchingPose;
        public const CharacterPoseNodeKind EntryPoseInput = CharacterPoseNodeKind.EntryPoseInput;
    }

    public static class CharacterMotionMatchingPosePorts
    {
        public static readonly PosePortId Facts = new PosePortId("presentation.facts");
        public static readonly PosePortId Binding = new PosePortId("motion-matching.binding");
        public static readonly PosePortId LocalPoseInput = new PosePortId("pose.local.input");
        public static readonly PosePortId LocalPoseOutput = new PosePortId("pose.local");
    }

    public enum CharacterMotionMatchingRelevanceResetPolicy : byte
    {
        ResetOnRelevanceLoss = 1,
        PreserveUntilPresentationReset = 2
    }

    public enum CharacterMotionMatchingSearchCadencePolicy : byte
    {
        ProfileSearchInterval = 1,
        EveryPresentationFrame = 2
    }

    [Serializable]
    public sealed class CharacterMotionMatchingPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseResourceSlot m_BindingSlot;
        [SerializeField] CharacterPoseResourceSlot m_JumpBlendPolicySlot;
        [SerializeField] CharacterPoseSubgraphReference m_EntryGraph = new CharacterPoseSubgraphReference();
        [SerializeField] CharacterMotionMatchingRelevanceResetPolicy m_RelevanceResetPolicy;
        [SerializeField] CharacterMotionMatchingSearchCadencePolicy m_SearchCadencePolicy;

        public override CharacterPoseNodeKind Kind => CharacterMotionMatchingPoseNodeKinds.MotionMatchingPose;
        public CharacterPoseResourceSlot BindingSlot => m_BindingSlot;
        public CharacterPoseResourceSlot JumpBlendPolicySlot => m_JumpBlendPolicySlot;
        public CharacterPoseSubgraphReference EntryGraph => m_EntryGraph;
        public CharacterMotionMatchingRelevanceResetPolicy RelevanceResetPolicy => m_RelevanceResetPolicy;
        public CharacterMotionMatchingSearchCadencePolicy SearchCadencePolicy => m_SearchCadencePolicy;

        public CharacterMotionMatchingPosePayload() { }

        public CharacterMotionMatchingPosePayload(
            CharacterPoseResourceSlot bindingSlot,
            CharacterPoseResourceSlot jumpBlendPolicySlot,
            PoseGraphId entryGraphId,
            CharacterMotionMatchingRelevanceResetPolicy relevanceResetPolicy,
            CharacterMotionMatchingSearchCadencePolicy searchCadencePolicy)
        {
            m_BindingSlot = bindingSlot ? bindingSlot : throw new ArgumentNullException(nameof(bindingSlot));
            m_JumpBlendPolicySlot = jumpBlendPolicySlot ? jumpBlendPolicySlot : throw new ArgumentNullException(nameof(jumpBlendPolicySlot));
            m_EntryGraph = new CharacterPoseSubgraphReference();
            m_EntryGraph.Assign(entryGraphId);
            m_RelevanceResetPolicy = RequireDefined(relevanceResetPolicy, nameof(relevanceResetPolicy));
            m_SearchCadencePolicy = RequireDefined(searchCadencePolicy, nameof(searchCadencePolicy));
        }

        public void RequireValid()
        {
            if (!m_BindingSlot || m_BindingSlot.Kind != CharacterPoseResourceKind.MotionMatchingBinding ||
                !m_JumpBlendPolicySlot || m_JumpBlendPolicySlot.Kind != CharacterPoseResourceKind.BlendPolicy ||
                m_EntryGraph == null || !m_EntryGraph.PoseGraphId.IsValid)
                throw new InvalidOperationException("Motion Matching Pose payload is incomplete.");
            RequireDefined(m_RelevanceResetPolicy, nameof(RelevanceResetPolicy));
            RequireDefined(m_SearchCadencePolicy, nameof(SearchCadencePolicy));
        }

        static T RequireDefined<T>(T value, string parameterName) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }

    [Serializable]
    public sealed class CharacterEntryPoseInputPayload : CharacterPoseNodePayload
    {
        public override CharacterPoseNodeKind Kind => CharacterMotionMatchingPoseNodeKinds.EntryPoseInput;
    }
}
