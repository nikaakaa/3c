using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationProjectionDraft
    {
        readonly CharacterPresentationSemanticContract m_Contract;
        readonly CharacterPoseProgramImage m_PosePlan;
        readonly AnimationBlendCurveCatalogPayload m_BlendCurveCatalog;
        readonly AnimationBlendProfileCatalogPayload m_BlendProfileCatalog;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly MotionMatchingProjectionPayload m_MotionMatching;
        readonly CharacterPresentationPoseSourcePlan[] m_PoseSources;
        readonly CharacterAnimationBlendSpacePlan[] m_BlendSpaces;
        readonly CharacterAnimationBlendSpacePlayerPlan[] m_BlendSpacePlayers;
        readonly AnimationClipPhasePlan[] m_ClipPhasePlans;
        readonly AnimationSourcePhasePlan[] m_SourcePhasePlans;
        readonly CharacterPresentationProducerEntry[] m_Producers;
        readonly AnimationFootAnalysisProjectionIdentity m_FootAnalysis;
        readonly EquipmentVisualProjectionBinding[] m_EquipmentVisualBindings;
        readonly CharacterLinkedPoseProjectionPayload m_LinkedPose;
        readonly CharacterPresentationAnimationPropertyBinding[] m_AnimationProperties;

        internal CharacterPresentationProjectionDraft(
            CharacterPresentationSemanticContract contract,
            CharacterPoseProgramImage posePlan,
            AnimationBlendCurveCatalogPayload blendCurveCatalog,
            AnimationBlendProfileCatalogPayload blendProfileCatalog,
            CharacterAnimationRigPayload rig,
            MotionMatchingProjectionPayload motionMatching,
            CharacterPresentationPoseSourcePlan[] poseSources,
            CharacterAnimationBlendSpacePlan[] blendSpaces,
            CharacterAnimationBlendSpacePlayerPlan[] blendSpacePlayers,
            AnimationClipPhasePlan[] clipPhasePlans,
            AnimationSourcePhasePlan[] sourcePhasePlans,
            CharacterPresentationProducerEntry[] producers,
            AnimationFootAnalysisProjectionIdentity footAnalysis,
            EquipmentVisualProjectionBinding[] equipmentVisualBindings,
            CharacterLinkedPoseProjectionPayload linkedPose,
            CharacterPresentationAnimationPropertyBinding[] animationProperties)
        {
            m_Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            m_PosePlan = posePlan ?? throw new ArgumentNullException(nameof(posePlan));
            m_BlendCurveCatalog = blendCurveCatalog ??
                throw new ArgumentNullException(nameof(blendCurveCatalog));
            m_BlendProfileCatalog = blendProfileCatalog ??
                throw new ArgumentNullException(nameof(blendProfileCatalog));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_MotionMatching = motionMatching;
            m_PoseSources = Copy(poseSources);
            m_BlendSpaces = Copy(blendSpaces);
            m_BlendSpacePlayers = Copy(blendSpacePlayers);
            m_ClipPhasePlans = Copy(clipPhasePlans);
            m_SourcePhasePlans = Copy(sourcePhasePlans);
            m_Producers = Copy(producers);
            m_FootAnalysis = footAnalysis;
            m_EquipmentVisualBindings = Copy(equipmentVisualBindings);
            m_LinkedPose = linkedPose ?? throw new ArgumentNullException(nameof(linkedPose));
            m_AnimationProperties = Copy(animationProperties);
        }

        internal CharacterPresentationProjection Seal(
            IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> resources,
            string projectionRevision)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources));
            if (string.IsNullOrWhiteSpace(projectionRevision))
                throw new ArgumentException(
                    "Projection revision is required.",
                    nameof(projectionRevision));
            var animationResources =
                new CharacterAnimationCompiledResourceDescriptor[resources.Count];
            for (int i = 0; i < resources.Count; i++)
                animationResources[i] = resources[i] ??
                    throw new InvalidOperationException(
                        $"Projection animation resource #{i} is missing.");
            return CharacterPresentationProjection.Create(
                m_Contract,
                m_PosePlan,
                m_BlendCurveCatalog,
                m_BlendProfileCatalog,
                m_Rig,
                animationResources,
                m_MotionMatching,
                m_PoseSources,
                m_BlendSpaces,
                m_BlendSpacePlayers,
                m_ClipPhasePlans,
                m_SourcePhasePlans,
                m_Producers,
                m_FootAnalysis,
                projectionRevision,
                m_EquipmentVisualBindings,
                m_LinkedPose,
                null,
                null,
                null,
                string.Empty,
                m_AnimationProperties);
        }

        static T[] Copy<T>(T[] values)
        {
            if (values == null || values.Length == 0)
                return Array.Empty<T>();
            var result = new T[values.Length];
            Array.Copy(values, result, values.Length);
            return result;
        }
    }
}
