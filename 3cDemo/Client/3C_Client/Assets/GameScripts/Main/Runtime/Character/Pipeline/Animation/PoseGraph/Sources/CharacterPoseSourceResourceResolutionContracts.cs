using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal enum CharacterPoseSourceReadinessTargetInput : byte
    {
        ClipSamples = 1,
        Resource = 2,
        BlendSpaceSamples = 3
    }

    internal readonly struct CharacterPoseSourceReadinessTarget
    {
        readonly AnimationPoseSourceId m_SourceId;
        readonly PoseNodeId m_PoseNodeId;
        readonly AnimationReadOnlyBuffer<ClipSamplePlan> m_Clips;
        CharacterPoseSourceReadinessTarget(
            CharacterPoseSourcePreparationKind kind,
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId,
            int bindingIndex,
            CharacterPoseSourceReadinessTargetInput input,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            CharacterAnimationSamplingBackendKind backend,
            int resourceIndex,
            int groupClipIndex,
            IReadOnlyList<CharacterAnimationBlendSpaceSamplePlan> samples)
        {
            if (!CharacterPoseSourceReadinessEnumValues.IsValid(kind) ||
                !poseNodeId.IsValid || bindingIndex < -1 ||
                (!sourceId.IsValid && bindingIndex < 0) ||
                input < CharacterPoseSourceReadinessTargetInput.ClipSamples ||
                input > CharacterPoseSourceReadinessTargetInput.BlendSpaceSamples)
            {
                throw new ArgumentException(
                    "Character Pose source readiness target is invalid.");
            }
            Kind = kind;
            m_SourceId = sourceId;
            m_PoseNodeId = poseNodeId;
            BindingIndex = bindingIndex;
            Input = input;
            m_Clips = clips;
            Backend = backend;
            ResourceIndex = resourceIndex;
            GroupClipIndex = groupClipIndex;
            Samples = samples;
            if (!IsValid)
                throw new ArgumentException(
                    "Character Pose source readiness target is invalid.");
        }

        internal CharacterPoseSourcePreparationKind Kind { get; }
        internal ref readonly AnimationPoseSourceId SourceId => ref m_SourceId;
        internal ref readonly PoseNodeId PoseNodeId => ref m_PoseNodeId;
        internal int BindingIndex { get; }
        internal CharacterPoseSourceReadinessTargetInput Input { get; }
        internal ref readonly AnimationReadOnlyBuffer<ClipSamplePlan> Clips =>
            ref m_Clips;
        internal CharacterAnimationSamplingBackendKind Backend { get; }
        internal int ResourceIndex { get; }
        internal int GroupClipIndex { get; }
        internal IReadOnlyList<CharacterAnimationBlendSpaceSamplePlan> Samples { get; }
        internal bool IsValid =>
            CharacterPoseSourceReadinessEnumValues.IsValid(Kind) &&
            PoseNodeId.IsValid &&
            BindingIndex >= -1 &&
            (SourceId.IsValid || BindingIndex >= 0) &&
            (Input == CharacterPoseSourceReadinessTargetInput.ClipSamples
                ? Clips.Count > 0
                : Input == CharacterPoseSourceReadinessTargetInput.Resource
                    ? (Backend == CharacterAnimationSamplingBackendKind.NativeClip ||
                       Backend == CharacterAnimationSamplingBackendKind.Acl) &&
                      (Backend == CharacterAnimationSamplingBackendKind.NativeClip
                          ? ResourceIndex == -1 && GroupClipIndex == -1
                          : ResourceIndex >= 0 && GroupClipIndex >= 0)
                    : Input == CharacterPoseSourceReadinessTargetInput.BlendSpaceSamples &&
                      Samples != null && Samples.Count > 0);

        internal static CharacterPoseSourceReadinessTarget FromClips(
            CharacterPoseSourcePreparationKind kind,
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId,
            int bindingIndex,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips) =>
            new CharacterPoseSourceReadinessTarget(
                kind,
                sourceId,
                poseNodeId,
                bindingIndex,
                CharacterPoseSourceReadinessTargetInput.ClipSamples,
                clips,
                CharacterAnimationSamplingBackendKind.NativeClip,
                -1,
                -1,
                null);

        internal static CharacterPoseSourceReadinessTarget FromResource(
            CharacterPoseSourcePreparationKind kind,
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId,
            int bindingIndex,
            CharacterAnimationSamplingBackendKind backend,
            int resourceIndex,
            int groupClipIndex) =>
            new CharacterPoseSourceReadinessTarget(
                kind,
                sourceId,
                poseNodeId,
                bindingIndex,
                CharacterPoseSourceReadinessTargetInput.Resource,
                default,
                backend,
                resourceIndex,
                groupClipIndex,
                null);

        internal static CharacterPoseSourceReadinessTarget FromBlendSpaceSamples(
            CharacterPoseSourcePreparationKind kind,
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId,
            int bindingIndex,
            IReadOnlyList<CharacterAnimationBlendSpaceSamplePlan> samples) =>
            new CharacterPoseSourceReadinessTarget(
                kind,
                sourceId,
                poseNodeId,
                bindingIndex,
                CharacterPoseSourceReadinessTargetInput.BlendSpaceSamples,
                default,
                CharacterAnimationSamplingBackendKind.NativeClip,
                -1,
                -1,
                samples);

        internal static CharacterPoseSourceReadinessTarget FromPreparation(
            in CharacterPoseSourcePreparation preparation)
        {
            if (!preparation.IsValid)
                throw new ArgumentException(
                    "Character Pose source preparation is invalid.",
                    nameof(preparation));
            bool useRequest =
                preparation.Kind == CharacterPoseSourcePreparationKind.Action ||
                preparation.Kind == CharacterPoseSourcePreparationKind.Provider;
            ref readonly AnimationPoseSourceId sourceId = ref useRequest
                ? ref preparation.Request.SourceId
                : ref preparation.SourceId;
            ref readonly AnimationReadOnlyBuffer<ClipSamplePlan> clips = ref useRequest
                ? ref preparation.Request.Clips
                : ref preparation.Clips;
            return FromClips(
                preparation.Kind,
                in sourceId,
                preparation.PoseNodeId,
                useRequest ? -1 : preparation.BindingIndex,
                in clips);
        }
    }

    internal readonly struct CharacterPoseSourceResourceResolution
    {
        internal CharacterPoseSourceResourceResolution(
            CharacterAclResourceReadinessResult resource,
            int resourceCatalogIndex,
            int groupClipIndex,
            ulong resourceGeneration)
        {
            Resource = resource;
            ResourceCatalogIndex = resourceCatalogIndex;
            GroupClipIndex = groupClipIndex;
            ResourceGeneration = resourceGeneration;
            if (!IsValid)
                throw new ArgumentException(
                    "Character Pose source resource resolution is invalid.");
        }

        internal CharacterAclResourceReadinessResult Resource { get; }
        internal int ResourceCatalogIndex { get; }
        internal int GroupClipIndex { get; }
        internal ulong ResourceGeneration { get; }
        internal bool IsReady => Resource.IsReady;
        internal bool IsPending => Resource.IsPending;
        internal bool IsInvalid => Resource.IsInvalid;
        internal bool IsValid =>
            (IsReady ? 1 : 0) +
            (IsPending ? 1 : 0) +
            (IsInvalid ? 1 : 0) == 1 &&
            (ResourceCatalogIndex == -1 &&
             GroupClipIndex == -1 &&
             ResourceGeneration == 0 ||
             ResourceCatalogIndex >= 0 &&
             GroupClipIndex >= 0 &&
             ResourceGeneration != 0);

        internal CharacterPoseSourceReadinessView ToReadiness(
            ulong completionIdentity)
        {
            if (!IsValid)
                throw new InvalidOperationException(
                    "Character Pose source resource resolution is invalid.");
            return IsInvalid
                ? CharacterPoseSourceReadinessView.Invalid(
                    completionIdentity,
                    ResourceCatalogIndex,
                    GroupClipIndex,
                    ResourceGeneration,
                    Resource.FailureCode,
                    Resource.Message)
                : IsPending
                    ? CharacterPoseSourceReadinessView.Pending(
                        completionIdentity,
                        ResourceCatalogIndex,
                        GroupClipIndex,
                        ResourceGeneration,
                        Resource.Message)
                    : ResourceCatalogIndex >= 0
                        ? CharacterPoseSourceReadinessView.Ready(
                            completionIdentity,
                            ResourceCatalogIndex,
                            GroupClipIndex,
                            ResourceGeneration)
                        : CharacterPoseSourceReadinessView.Ready(
                            completionIdentity);
        }
    }
}
