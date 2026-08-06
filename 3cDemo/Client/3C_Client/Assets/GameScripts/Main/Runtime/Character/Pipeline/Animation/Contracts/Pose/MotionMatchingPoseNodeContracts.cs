using System;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterMotionMatchingPoseNodeKinds
    {
        public const CharacterPoseNodeKind MotionMatchingPose = CharacterPoseNodeKind.MotionMatchingPose;
        public const CharacterPoseNodeKind PoseHistoryCollector = CharacterPoseNodeKind.PoseHistoryCollector;
        public const CharacterPoseNodeKind EntryPoseInput = CharacterPoseNodeKind.EntryPoseInput;
    }

    public static class CharacterMotionMatchingPosePorts
    {
        public static readonly PosePortId History = new PosePortId("history.pose");
        public static readonly PosePortId Trajectory = new PosePortId("trajectory.query");
        public static readonly PosePortId Facts = new PosePortId("presentation.facts");
        public static readonly PosePortId Binding = new PosePortId("motion-matching.binding");
        public static readonly PosePortId LocalPoseInput = new PosePortId("pose.local.input");
        public static readonly PosePortId LocalPoseOutput = new PosePortId("pose.local");
    }

    public readonly struct CharacterPoseHistoryId : IEquatable<CharacterPoseHistoryId>, IComparable<CharacterPoseHistoryId>
    {
        public CharacterPoseHistoryId(string value) => Value = PoseIdentity.Require(value, nameof(value));
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(CharacterPoseHistoryId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterPoseHistoryId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterPoseHistoryId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterPoseHistoryId left, CharacterPoseHistoryId right) => left.Equals(right);
        public static bool operator !=(CharacterPoseHistoryId left, CharacterPoseHistoryId right) => !left.Equals(right);
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
        [SerializeField] CharacterMotionMatchingBinding m_Binding;
        [SerializeField] CharacterAnimationBlendPolicy m_JumpBlendPolicy;
        [SerializeField] CharacterPoseSubgraphReference m_EntryGraph = new CharacterPoseSubgraphReference();
        [SerializeField] CharacterMotionMatchingRelevanceResetPolicy m_RelevanceResetPolicy;
        [SerializeField] CharacterMotionMatchingSearchCadencePolicy m_SearchCadencePolicy;

        public override CharacterPoseNodeKind Kind => CharacterMotionMatchingPoseNodeKinds.MotionMatchingPose;
        public CharacterMotionMatchingBinding Binding => m_Binding;
        public CharacterAnimationBlendPolicy JumpBlendPolicy => m_JumpBlendPolicy;
        public CharacterPoseSubgraphReference EntryGraph => m_EntryGraph;
        public CharacterMotionMatchingRelevanceResetPolicy RelevanceResetPolicy => m_RelevanceResetPolicy;
        public CharacterMotionMatchingSearchCadencePolicy SearchCadencePolicy => m_SearchCadencePolicy;

        public CharacterMotionMatchingPosePayload() { }

        public CharacterMotionMatchingPosePayload(
            CharacterMotionMatchingBinding binding,
            CharacterAnimationBlendPolicy jumpBlendPolicy,
            PoseGraphId entryGraphId,
            CharacterMotionMatchingRelevanceResetPolicy relevanceResetPolicy,
            CharacterMotionMatchingSearchCadencePolicy searchCadencePolicy)
        {
            m_Binding = binding;
            m_JumpBlendPolicy = jumpBlendPolicy;
            m_EntryGraph = new CharacterPoseSubgraphReference();
            m_EntryGraph.Assign(entryGraphId);
            m_RelevanceResetPolicy = RequireDefined(relevanceResetPolicy, nameof(relevanceResetPolicy));
            m_SearchCadencePolicy = RequireDefined(searchCadencePolicy, nameof(searchCadencePolicy));
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!m_Binding || !m_JumpBlendPolicy || m_EntryGraph == null || !m_EntryGraph.PoseGraphId.IsValid)
                throw new InvalidOperationException("Motion Matching Pose payload is incomplete.");
            RequireDefined(m_RelevanceResetPolicy, nameof(RelevanceResetPolicy));
            RequireDefined(m_SearchCadencePolicy, nameof(SearchCadencePolicy));
            m_Binding.RequireValid(rig);
            m_JumpBlendPolicy.RequireValid(rig);
        }

        static T RequireDefined<T>(T value, string parameterName) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }

    [Serializable]
    public sealed class CharacterPoseHistoryCollectorPayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_HistoryId = string.Empty;

        public override CharacterPoseNodeKind Kind => CharacterMotionMatchingPoseNodeKinds.PoseHistoryCollector;
        public CharacterPoseHistoryId HistoryId => string.IsNullOrWhiteSpace(m_HistoryId)
            ? default
            : new CharacterPoseHistoryId(m_HistoryId);

        public CharacterPoseHistoryCollectorPayload() { }

        public CharacterPoseHistoryCollectorPayload(CharacterPoseHistoryId historyId) =>
            m_HistoryId = historyId.IsValid
                ? historyId.Value
                : throw new ArgumentException("Pose History identity is invalid.", nameof(historyId));
    }

    [Serializable]
    public sealed class CharacterEntryPoseInputPayload : CharacterPoseNodePayload
    {
        public override CharacterPoseNodeKind Kind => CharacterMotionMatchingPoseNodeKinds.EntryPoseInput;
    }
}
