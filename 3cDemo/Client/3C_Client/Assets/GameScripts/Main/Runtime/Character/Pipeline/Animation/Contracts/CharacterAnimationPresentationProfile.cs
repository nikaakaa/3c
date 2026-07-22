using System;
using System.Collections.Generic;
using Animancer;
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

        public void Configure(
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
        [SerializeField] CharacterAnimationBlendLibrary m_BlendLibrary;
        [SerializeField] CharacterAnimationRigDefinition m_RigDefinition;
        [SerializeField] AnimationProducerPresentationBinding[] m_ProducerBindings =
            Array.Empty<AnimationProducerPresentationBinding>();
        [SerializeField] CharacterFootPlacementAnalysisMode m_FootPlacementAnalysisMode;
        [SerializeField] string m_FootPlacementAnalysisSourceAssetGuid = string.Empty;

        public CharacterPresentationPoseGraphAsset PoseGraph => m_PoseGraph;
        public CharacterAnimationBlendLibrary BlendLibrary => m_BlendLibrary;
        public CharacterAnimationRigDefinition RigDefinition => m_RigDefinition;
        public IReadOnlyList<AnimationProducerPresentationBinding> ProducerBindings =>
            m_ProducerBindings ?? Array.Empty<AnimationProducerPresentationBinding>();
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

        public void SetPresentationGraph(
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterAnimationBlendLibrary blendLibrary,
            CharacterAnimationRigDefinition rigDefinition)
        {
            m_PoseGraph = poseGraph ? poseGraph : throw new ArgumentNullException(nameof(poseGraph));
            m_BlendLibrary = blendLibrary ? blendLibrary : throw new ArgumentNullException(nameof(blendLibrary));
            m_RigDefinition = rigDefinition ? rigDefinition : throw new ArgumentNullException(nameof(rigDefinition));
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
            if (!m_BlendLibrary)
            {
                errors?.Add($"{name}: Animation Blend Library is missing.");
                valid = false;
            }
            if (!m_RigDefinition)
            {
                errors?.Add($"{name}: Animation Rig Definition is missing.");
                valid = false;
            }
            if (m_PoseGraph && m_RigDefinition)
            {
                CharacterPoseGraphValidationReport poseReport = CharacterPresentationPoseGraphValidator.Validate(m_PoseGraph, m_RigDefinition);
                poseReport.CopyMessagesTo(errors);
                valid &= poseReport.IsValid;
            }
            if (m_BlendLibrary && m_PoseGraph && m_RigDefinition)
            {
                try
                {
                    m_BlendLibrary.RequireValid(m_PoseGraph, m_RigDefinition);
                }
                catch (Exception exception)
                {
                    errors?.Add(exception.Message);
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
