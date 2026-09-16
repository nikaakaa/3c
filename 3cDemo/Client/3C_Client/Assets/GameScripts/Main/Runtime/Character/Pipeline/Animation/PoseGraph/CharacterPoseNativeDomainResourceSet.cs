using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(
        fileName = "CharacterPoseNativeDomainResourceSet",
        menuName = "3C/Character/Pose Native Domain Resource Set")]
    public sealed class CharacterPoseNativeDomainResourceSet : ScriptableObject
    {
        [SerializeField] CharacterPresentationPoseSourcePlan[] m_SourcePlans =
            Array.Empty<CharacterPresentationPoseSourcePlan>();
        [SerializeField] CharacterAnimationCompiledResourceDescriptor[] m_ResourceDescriptors =
            Array.Empty<CharacterAnimationCompiledResourceDescriptor>();
        [SerializeField] CharacterPoseBoneIkGoalBinding[] m_PoseBoneIkGoalBindings =
            Array.Empty<CharacterPoseBoneIkGoalBinding>();
        [SerializeField] int[] m_GoalAssemblerContributions = Array.Empty<int>();
        [SerializeField, Min(0)] int m_GoalSetValueCount;
        [SerializeField, Min(1)] int m_ContributionCount = 1;
        [SerializeField, Min(1)] int m_ContributionGoalCount = 1;
        [SerializeField] CharacterFootPlacementProfile m_FootPlacementProfile;
        [SerializeField] CharacterFootPlacementRigCalibration m_FootPlacementCalibration;
        [SerializeField] CharacterLinkedPoseImplementationAsset[] m_LinkedPoseImplementations =
            Array.Empty<CharacterLinkedPoseImplementationAsset>();
        [SerializeField] RootMotionCurveAsset[] m_RootOrientationCurves =
            Array.Empty<RootMotionCurveAsset>();

        public IReadOnlyList<CharacterPresentationPoseSourcePlan> SourcePlans =>
            m_SourcePlans ?? Array.Empty<CharacterPresentationPoseSourcePlan>();
        public IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> ResourceDescriptors =>
            m_ResourceDescriptors ?? Array.Empty<CharacterAnimationCompiledResourceDescriptor>();
        public IReadOnlyList<CharacterPoseBoneIkGoalBinding> PoseBoneIkGoalBindings =>
            m_PoseBoneIkGoalBindings ?? Array.Empty<CharacterPoseBoneIkGoalBinding>();
        public IReadOnlyList<int> GoalAssemblerContributions =>
            m_GoalAssemblerContributions ?? Array.Empty<int>();
        public int GoalSetValueCount => m_GoalSetValueCount;
        public int ContributionCount => m_ContributionCount;
        public int ContributionGoalCount => m_ContributionGoalCount;
        public CharacterFootPlacementProfile FootPlacementProfile => m_FootPlacementProfile;
        public CharacterFootPlacementRigCalibration FootPlacementCalibration => m_FootPlacementCalibration;
        public IReadOnlyList<CharacterLinkedPoseImplementationAsset> LinkedPoseImplementations =>
            m_LinkedPoseImplementations ?? Array.Empty<CharacterLinkedPoseImplementationAsset>();
        public IReadOnlyList<RootMotionCurveAsset> RootOrientationCurves =>
            m_RootOrientationCurves ?? Array.Empty<RootMotionCurveAsset>();

        internal CharacterPoseNativeSourceResourceCatalog CreateSourceCatalog(
            CharacterAnimationRigPayload rig,
            CharacterAnimationResourceScope resourceScope)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if (resourceScope == null)
                throw new ArgumentNullException(nameof(resourceScope));
            return new CharacterPoseNativeSourceResourceCatalog(
                rig,
                resourceScope,
                SourcePlans,
                ResourceDescriptors);
        }

        internal CharacterPoseNativeConstraintResourceCatalog CreateConstraintCatalog(
            CharacterAnimationRigPayload rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            var descriptors = new CharacterPoseBoneIkGoalDescriptor[PoseBoneIkGoalBindings.Count];
            for (int i = 0; i < descriptors.Length; i++)
            {
                CharacterPoseBoneIkGoalBinding binding = PoseBoneIkGoalBindings[i] ??
                    throw new InvalidOperationException($"Pose Bone IK Goal binding #{i} is missing.");
                descriptors[i] = new CharacterPoseBoneIkGoalDescriptor(
                    binding.EffectorSlot,
                    rig.RequirePoseBoneIndex(binding.TargetPoseBoneId),
                    binding.PositionOffset,
                    binding.RotationOffset,
                    binding.PositionWeight,
                    binding.RotationWeight);
            }
            return new CharacterPoseNativeConstraintResourceCatalog(
                descriptors,
                GoalAssemblerContributions,
                GoalSetValueCount);
        }

        internal CharacterPoseNativeFootPlacementResource CreateFootPlacementResource(
            CharacterWorldAwarePresentationBinding world,
            ICharacterFutureBodyTranslationSource futureBodyTranslationSource,
            ICharacterFootPlacementWorldQuery worldQuery)
        {
            if (!FootPlacementProfile || !FootPlacementCalibration)
                throw new InvalidOperationException("Pose Native Foot Placement resource is incomplete.");
            return new CharacterPoseNativeFootPlacementResource(
                FootPlacementProfile,
                FootPlacementCalibration,
                world,
                futureBodyTranslationSource,
                worldQuery);
        }
        internal CharacterPoseNativeManagedSourceResourceCatalog CreateManagedCatalog(
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            return new CharacterPoseNativeManagedSourceResourceCatalog(
                rig,
                BuildLinkedImplementations(profile),
                Array.Empty<CharacterMotionMatchingRuntimeDatabase>(),
                RootOrientationCurves.Where(value => value).ToArray());
        }

        static CharacterPoseNativeLinkedPoseImplementation[] BuildLinkedImplementations(
            CharacterAnimationPresentationProfile profile)
        {
            var implementations = new List<CharacterPoseNativeLinkedPoseImplementation>();
            foreach (CharacterLinkedPoseGroupBinding group in profile.LinkedPoseGroups)
            {
                if (group == null || !group.Interface)
                    continue;
                CharacterLinkedPoseImplementationAsset implementation =
                    profile.LinkedPoseImplementations.FirstOrDefault(value =>
                        value && value.Interface == group.Interface) ??
                    throw new InvalidOperationException(
                        $"Linked Pose group '{group.GroupId}' has no implementation.");
                implementations.Add(new CharacterPoseNativeLinkedPoseImplementation(
                    group.GroupId,
                    implementation));
            }
            return implementations.ToArray();
        }

        internal void RequireSourceCatalogComplete(CharacterAnimationPresentationProfile profile)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            var reachableSlots = new HashSet<CharacterPresentationPoseSourceSlot>();
            foreach (CharacterPoseCanvasGraph graph in profile.PoseGraph.EnumerateGraphs())
            {
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node?.PresentationPoseSourceSlot)
                        reachableSlots.Add(node.PresentationPoseSourceSlot);
                }
            }
            int bindingCount = 0;
            foreach (CharacterPresentationPoseSourceBinding binding in profile.PoseSourceBindings)
            {
                if (!binding || !binding.Slot)
                    continue;
                bindingCount++;
                if (reachableSlots.Contains(binding.Slot) &&
                    !SourcePlans.Any(plan => plan != null && plan.SourceIndex.Value == bindingCount - 1))
                {
                    throw new InvalidOperationException(
                        $"Pose Native Source plan #{bindingCount - 1} is missing for '{binding.Slot.name}'.");
                }
            }
        }
    }
}



