using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class AnimationProducerPresentationBinding
    {
        [SerializeField] string m_TimelineAuthoringId;
        [SerializeField] string m_TrackAuthoringId;
        [SerializeField] TransitionAssetBase m_Source;

        public AnimationProducerId ProducerId => new AnimationProducerId(m_TimelineAuthoringId, m_TrackAuthoringId);
        public TransitionAssetBase Source => m_Source;

        public void ConfigureTimeline(
            AnimationProducerId producerId,
            TransitionAssetBase source)
        {
            if (!producerId.IsValid)
                throw new ArgumentException("Animation producer id is invalid.", nameof(producerId));
            if (!source || !source.IsValid)
                throw new ArgumentException("Animation source is invalid.", nameof(source));

            m_TimelineAuthoringId = producerId.TimelineAuthoringId;
            m_TrackAuthoringId = producerId.TrackAuthoringId;
            m_Source = source;
        }

    }

    [CreateAssetMenu(
        fileName = "CharacterAnimationPresentationProfile",
        menuName = "3C/Character/Animation Presentation Profile")]
    public sealed class CharacterAnimationPresentationProfile : ScriptableObject
    {
        [SerializeField] CharacterPresentationPoseGraphAsset m_PoseGraph;
        [SerializeField] CharacterAnimationRigDefinition m_RigDefinition;
        [SerializeField] CharacterMotionMatchingProfile m_MotionMatchingProfile;
        [SerializeField] CharacterFullBodyIkProfile m_FullBodyIkProfile;
        [SerializeField] CharacterLinkedPoseGroupBinding[] m_LinkedPoseGroups =
            Array.Empty<CharacterLinkedPoseGroupBinding>();
        [SerializeField] CharacterLinkedPoseImplementationAsset[] m_LinkedPoseImplementations =
            Array.Empty<CharacterLinkedPoseImplementationAsset>();
        [SerializeField] CharacterLinkedPoseSelectorBindingAsset[] m_LinkedPoseSelectors =
            Array.Empty<CharacterLinkedPoseSelectorBindingAsset>();
        [SerializeField] AnimationProducerPresentationBinding[] m_ProducerBindings =
            Array.Empty<AnimationProducerPresentationBinding>();
        [SerializeField] CharacterPresentationPoseSourceBinding[] m_PoseSourceBindings =
            Array.Empty<CharacterPresentationPoseSourceBinding>();
        [SerializeField] CharacterLocomotionSyncGroup[] m_LocomotionSyncGroups =
            Array.Empty<CharacterLocomotionSyncGroup>();
        [SerializeField] CharacterFootPlacementAnalysisMode m_FootPlacementAnalysisMode;
        [SerializeField] string m_FootPlacementAnalysisSourceAssetGuid = string.Empty;

        public CharacterPresentationPoseGraphAsset PoseGraph => m_PoseGraph;
        public CharacterAnimationRigDefinition RigDefinition => m_RigDefinition;
        public CharacterMotionMatchingProfile MotionMatchingProfile => m_MotionMatchingProfile;
        public CharacterFullBodyIkProfile FullBodyIkProfile => m_FullBodyIkProfile;
        public IReadOnlyList<CharacterLinkedPoseGroupBinding> LinkedPoseGroups =>
            m_LinkedPoseGroups ?? Array.Empty<CharacterLinkedPoseGroupBinding>();
        public IReadOnlyList<CharacterLinkedPoseImplementationAsset> LinkedPoseImplementations =>
            m_LinkedPoseImplementations ?? Array.Empty<CharacterLinkedPoseImplementationAsset>();
        public IReadOnlyList<CharacterLinkedPoseSelectorBindingAsset> LinkedPoseSelectors =>
            m_LinkedPoseSelectors ?? Array.Empty<CharacterLinkedPoseSelectorBindingAsset>();
        public IReadOnlyList<AnimationProducerPresentationBinding> ProducerBindings =>
            m_ProducerBindings ?? Array.Empty<AnimationProducerPresentationBinding>();
        public IReadOnlyList<CharacterPresentationPoseSourceBinding> PoseSourceBindings =>
            m_PoseSourceBindings ?? Array.Empty<CharacterPresentationPoseSourceBinding>();
        public IReadOnlyList<CharacterLocomotionSyncGroup> LocomotionSyncGroups =>
            m_LocomotionSyncGroups ?? Array.Empty<CharacterLocomotionSyncGroup>();
        public CharacterFootPlacementAnalysisMode FootPlacementAnalysisMode => m_FootPlacementAnalysisMode;
        public string FootPlacementAnalysisSourceAssetGuid => m_FootPlacementAnalysisSourceAssetGuid ?? string.Empty;

        public AnimationProducerPresentationBinding FindProducerBinding(AnimationProducerId producerId)
        {
            for (int i = 0; i < ProducerBindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = ProducerBindings[i];
                if (binding != null && binding.ProducerId.Equals(producerId))
                    return binding;
            }
            return null;
        }

        public void SetProducerBindings(AnimationProducerPresentationBinding[] bindings)
        {
            m_ProducerBindings = bindings ?? Array.Empty<AnimationProducerPresentationBinding>();
        }

        public CharacterPresentationPoseSourceBinding FindPoseSourceBinding(
            CharacterPresentationPoseSourceSlot slot)
        {
            for (int i = 0; i < PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding = PoseSourceBindings[i];
                if (binding && binding.Slot == slot)
                    return binding;
            }
            return null;
        }

        public void SetPoseSourceBindings(CharacterPresentationPoseSourceBinding[] bindings)
        {
            m_PoseSourceBindings = bindings ?? Array.Empty<CharacterPresentationPoseSourceBinding>();
        }

        public void SetLocomotionSyncGroups(CharacterLocomotionSyncGroup[] groups)
        {
            m_LocomotionSyncGroups = groups ?? Array.Empty<CharacterLocomotionSyncGroup>();
        }

        public CharacterLocomotionSyncGroup FindLocomotionSyncGroup(AnimationClip clip)
        {
            CharacterLocomotionSyncGroup result = null;
            for (int i = 0; i < LocomotionSyncGroups.Count; i++)
            {
                CharacterLocomotionSyncGroup group = LocomotionSyncGroups[i];
                if (group == null || !group.Contains(clip))
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"AnimationClip '{clip?.name}' belongs to more than one Locomotion Sync Group.");
                result = group;
            }
            return result;
        }

        public void SetPresentationGraph(
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterAnimationRigDefinition rigDefinition)
        {
            m_PoseGraph = poseGraph ? poseGraph : throw new ArgumentNullException(nameof(poseGraph));
            m_RigDefinition = rigDefinition ? rigDefinition : throw new ArgumentNullException(nameof(rigDefinition));
        }

        public void SetMotionMatchingProfile(CharacterMotionMatchingProfile profile)
        {
            m_MotionMatchingProfile = profile;
        }

        public void SetFullBodyIkProfile(CharacterFullBodyIkProfile profile)
        {
            m_FullBodyIkProfile = profile ? profile : throw new ArgumentNullException(nameof(profile));
        }

        public void SetLinkedPoseBindings(
            CharacterLinkedPoseGroupBinding[] groups,
            CharacterLinkedPoseImplementationAsset[] implementations,
            CharacterLinkedPoseSelectorBindingAsset[] selectors)
        {
            m_LinkedPoseGroups = groups ?? Array.Empty<CharacterLinkedPoseGroupBinding>();
            m_LinkedPoseImplementations = implementations ?? Array.Empty<CharacterLinkedPoseImplementationAsset>();
            m_LinkedPoseSelectors = selectors ?? Array.Empty<CharacterLinkedPoseSelectorBindingAsset>();
        }

        public void SetFootPlacementAnalysis(
            CharacterFootPlacementAnalysisMode mode,
            string analysisSourceAssetGuid)
        {
            if (mode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures)
            {
                if (!IsAssetGuid(analysisSourceAssetGuid))
                    throw new ArgumentException("Foot Placement Analysis Source Asset GUID is invalid.", nameof(analysisSourceAssetGuid));
                m_FootPlacementAnalysisSourceAssetGuid = analysisSourceAssetGuid;
            }
            else if (mode == CharacterFootPlacementAnalysisMode.Disabled)
            {
                if (!string.IsNullOrEmpty(analysisSourceAssetGuid))
                    throw new ArgumentException("Disabled Foot Placement Analysis cannot retain a Source GUID.", nameof(analysisSourceAssetGuid));
                m_FootPlacementAnalysisSourceAssetGuid = string.Empty;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }
            m_FootPlacementAnalysisMode = mode;
        }

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (!m_PoseGraph)
            {
                errors?.Add($"{name}: Presentation Pose Graph is missing.");
                valid = false;
            }
            if (!m_RigDefinition)
            {
                errors?.Add($"{name}: Animation Rig Definition is missing.");
                valid = false;
            }
            if (!m_FullBodyIkProfile)
            {
                errors?.Add($"{name}: Full Body IK Profile is missing.");
                valid = false;
            }
            else
            {
                try
                {
                    m_FullBodyIkProfile.RequireValid();
                }
                catch (Exception exception)
                {
                    errors?.Add($"{name}: {exception.Message}");
                    valid = false;
                }
            }
            var producerIds = new HashSet<AnimationProducerId>();
            IReadOnlyList<AnimationProducerPresentationBinding> bindings = ProducerBindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = bindings[i];
                if (binding == null || !binding.ProducerId.IsValid ||
                    !producerIds.Add(binding.ProducerId) ||
                    !binding.Source || !binding.Source.IsValid)
                {
                    errors?.Add($"{name}: Animation producer binding #{i} is invalid or duplicated.");
                    valid = false;
                }
            }

            var locomotionGroupIds = new HashSet<string>(StringComparer.Ordinal);
            var locomotionGroupMembers = new HashSet<AnimationClip>();
            for (int i = 0; i < LocomotionSyncGroups.Count; i++)
            {
                CharacterLocomotionSyncGroup group = LocomotionSyncGroups[i];
                try
                {
                    group?.RequireValid();
                    if (group == null || !locomotionGroupIds.Add(group.GroupId))
                        throw new InvalidOperationException("group identity is missing or duplicated.");
                    for (int memberIndex = 0; memberIndex < group.Members.Count; memberIndex++)
                    {
                        if (!locomotionGroupMembers.Add(group.Members[memberIndex]))
                            throw new InvalidOperationException($"member '{group.Members[memberIndex].name}' belongs to more than one group.");
                    }
                }
                catch (Exception exception)
                {
                    errors?.Add($"{name}: Locomotion Sync Group #{i} is invalid: {exception.Message}");
                    valid = false;
                }
            }

            var poseSourceSlots = new HashSet<CharacterPresentationPoseSourceSlot>();
            for (int i = 0; i < PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding = PoseSourceBindings[i];
                try
                {
                    if (!binding || !binding.Slot || !poseSourceSlots.Add(binding.Slot))
                        throw new InvalidOperationException("binding Slot is missing or duplicated.");
                    if (!m_PoseGraph || !m_PoseGraph.SourceSlots.Contains(binding.Slot))
                        throw new InvalidOperationException("binding Slot is outside the assigned Pose Graph.");
                    binding.RequireValid(m_RigDefinition);
                }
                catch (Exception exception)
                {
                    errors?.Add($"{name}: Presentation Pose source binding #{i} is invalid: {exception.Message}");
                    valid = false;
                }
            }

            if (m_MotionMatchingProfile)
            {
                try
                {
                    m_MotionMatchingProfile.RequireValid();
                    if (!m_RigDefinition)
                        throw new InvalidOperationException("Motion Matching Profile requires the Presentation Rig.");
                    m_MotionMatchingProfile.RequireRigClosure(m_RigDefinition);
                }
                catch (Exception exception)
                {
                    errors?.Add($"{name}: {exception.Message}");
                    valid = false;
                }
            }

            if (!CollectLinkedPoseConfigurationErrors(errors))
                valid = false;

            if (m_FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures)
            {
                if (!IsAssetGuid(m_FootPlacementAnalysisSourceAssetGuid))
                {
                    errors?.Add($"{name}: Foot Placement Analysis Source Asset GUID is invalid.");
                    valid = false;
                }
            }
            else if (m_FootPlacementAnalysisMode != CharacterFootPlacementAnalysisMode.Disabled ||
                     !string.IsNullOrEmpty(m_FootPlacementAnalysisSourceAssetGuid))
            {
                errors?.Add($"{name}: Disabled Foot Placement Analysis retains an invalid mode or Source GUID.");
                valid = false;
            }

            return valid;
        }

        bool CollectLinkedPoseConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            var groups = new Dictionary<LinkedPoseGroupId, CharacterLinkedPoseGroupBinding>();
            for (int i = 0; i < LinkedPoseGroups.Count; i++)
            {
                CharacterLinkedPoseGroupBinding group = LinkedPoseGroups[i];
                try
                {
                    group?.RequireValid();
                    if (group == null || !groups.TryAdd(group.GroupId, group))
                        throw new InvalidOperationException("Group is missing or duplicated.");
                }
                catch (Exception exception)
                {
                    errors?.Add($"{name}: Linked Pose Group #{i} is invalid: {exception.Message}");
                    valid = false;
                }
            }

            var implementations = new Dictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationAsset>();
            for (int i = 0; i < LinkedPoseImplementations.Count; i++)
            {
                CharacterLinkedPoseImplementationAsset implementation = LinkedPoseImplementations[i];
                try
                {
                    implementation?.RequireValid();
                    if (!implementation || !implementations.TryAdd(implementation.ImplementationId, implementation))
                        throw new InvalidOperationException("Implementation is missing or duplicated.");
                }
                catch (Exception exception)
                {
                    errors?.Add($"{name}: Linked Pose Implementation #{i} is invalid: {exception.Message}");
                    valid = false;
                }
            }

            var selectorIds = new HashSet<LinkedPoseSelectorId>();
            var selectedGroups = new HashSet<LinkedPoseGroupId>();
            var candidates = new HashSet<LinkedPoseImplementationId>();
            for (int i = 0; i < LinkedPoseSelectors.Count; i++)
            {
                CharacterLinkedPoseSelectorBindingAsset selectorAsset = LinkedPoseSelectors[i];
                try
                {
                    if (!selectorAsset || !(selectorAsset is ICharacterLinkedPoseSelectorAuthoring selector) ||
                        !selectorIds.Add(selector.SelectorId) || !selectedGroups.Add(selector.GroupId) ||
                        !groups.TryGetValue(selector.GroupId, out CharacterLinkedPoseGroupBinding group))
                    {
                        throw new InvalidOperationException("Selector is missing, duplicated or targets an undeclared Group.");
                    }
                    selector.RequireValid(group, implementations);
                    for (int candidateIndex = 0; candidateIndex < selector.CandidateImplementationIds.Count; candidateIndex++)
                        candidates.Add(selector.CandidateImplementationIds[candidateIndex]);
                }
                catch (Exception exception)
                {
                    errors?.Add($"{name}: Linked Pose selector #{i} is invalid: {exception.Message}");
                    valid = false;
                }
            }

            foreach (LinkedPoseGroupId groupId in groups.Keys)
            {
                if (!selectedGroups.Contains(groupId))
                {
                    errors?.Add($"{name}: Linked Pose Group '{groupId}' does not have exactly one selector.");
                    valid = false;
                }
            }
            foreach (LinkedPoseImplementationId implementationId in implementations.Keys)
            {
                if (!candidates.Contains(implementationId))
                {
                    errors?.Add($"{name}: Linked Pose Implementation '{implementationId}' is outside every selector candidate closure.");
                    valid = false;
                }
            }
            return valid;
        }

        static bool IsAssetGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9' && c < 'a' || c > 'f')
                    return false;
            }
            return true;
        }
    }
}
