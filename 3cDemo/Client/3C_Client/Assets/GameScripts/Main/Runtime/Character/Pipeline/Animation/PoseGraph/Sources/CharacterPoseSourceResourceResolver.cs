using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Resources;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal sealed class CharacterPoseSourceResourceResolver
    {
        readonly CharacterAclResourceStore m_Store;

        internal CharacterPoseSourceResourceResolver(
            CharacterAclResourceStore store)
        {
            m_Store = store ?? throw new ArgumentNullException(nameof(store));
        }

        internal int Resolve(
            in CharacterPoseSourceReadinessTarget target,
            CharacterPoseSourceResourceResolution[] destination,
            out CharacterPoseSourceResourceResolution aggregate)
        {
            if (!target.IsValid)
                throw new ArgumentException(
                    "Character Pose source readiness target is invalid.",
                    nameof(target));
            if (destination == null || destination.Length == 0)
                throw new ArgumentException(
                    "Character Pose source readiness resolution buffer is invalid.",
                    nameof(destination));
            int count = target.Input switch
            {
                CharacterPoseSourceReadinessTargetInput.ClipSamples =>
                    ResolveClips(in target.Clips, destination),
                CharacterPoseSourceReadinessTargetInput.Resource =>
                    ResolveSingleResource(
                        target.Backend,
                        target.ResourceIndex,
                        target.GroupClipIndex,
                        destination),
                CharacterPoseSourceReadinessTargetInput.BlendSpaceSamples =>
                    ResolveBlendSpaceSamples(target.Samples, destination),
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
            aggregate = Aggregate(destination, count);
            return count;
        }

        int ResolveClips(
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            CharacterPoseSourceResourceResolution[] destination)
        {
            int count = 0;
            bool hasInvalid = false;
            for (int i = 0; i < clips.Count; i++)
            {
                ref readonly ClipSamplePlan clip = ref clips.ElementAt(i);
                if (!clip.IsValid)
                {
                    if (!hasInvalid)
                    {
                        Add(
                            InvalidResolution(
                                CharacterAclResourceFailureCode.FormatMismatch,
                                "Animation pose source clip plan is invalid."),
                            destination,
                            ref count);
                        hasInvalid = true;
                    }
                    continue;
                }
                if (!clip.IsAcl)
                    continue;
                Add(
                    EvaluateResource(
                        CharacterAnimationSamplingBackendKind.Acl,
                        clip.ResourceCatalogIndex,
                        clip.GroupClipIndex),
                    destination,
                    ref count);
            }
            if (count == 0)
                Add(ReadyResolution(), destination, ref count);
            return count;
        }

        int ResolveBlendSpaceSamples(
            IReadOnlyList<CharacterAnimationBlendSpaceSamplePlan> samples,
            CharacterPoseSourceResourceResolution[] destination)
        {
            int count = 0;
            bool hasInvalid = false;
            for (int i = 0; i < samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSamplePlan sample = samples[i];
                if (sample == null ||
                    sample.IsAcl &&
                    (sample.ResourceCatalogIndex < 0 ||
                     sample.GroupClipIndex < 0))
                {
                    if (!hasInvalid)
                    {
                        Add(
                            InvalidResolution(
                                CharacterAclResourceFailureCode.FormatMismatch,
                                "Blend Space animation sample plan is invalid."),
                            destination,
                            ref count);
                        hasInvalid = true;
                    }
                    continue;
                }
                if (!sample.IsAcl)
                    continue;
                Add(
                    EvaluateResource(
                        CharacterAnimationSamplingBackendKind.Acl,
                        sample.ResourceCatalogIndex,
                        sample.GroupClipIndex),
                    destination,
                    ref count);
            }
            if (count == 0)
                Add(ReadyResolution(), destination, ref count);
            return count;
        }

        int ResolveSingleResource(
            CharacterAnimationSamplingBackendKind backend,
            int resourceIndex,
            int groupClipIndex,
            CharacterPoseSourceResourceResolution[] destination)
        {
            destination[0] = EvaluateResource(
                backend,
                resourceIndex,
                groupClipIndex);
            return 1;
        }

        CharacterPoseSourceResourceResolution EvaluateResource(
            CharacterAnimationSamplingBackendKind backend,
            int resourceIndex,
            int groupClipIndex)
        {
            if (backend == CharacterAnimationSamplingBackendKind.NativeClip)
                return ReadyResolution();
            int storeIndex = m_Store.RequireStoreIndex(resourceIndex);
            CharacterAclResourceReadinessResult resource =
                m_Store.GetReadiness(storeIndex, out ulong generation);
            return new CharacterPoseSourceResourceResolution(
                resource,
                resourceIndex,
                groupClipIndex,
                generation);
        }

        static void Add(
            in CharacterPoseSourceResourceResolution resolution,
            CharacterPoseSourceResourceResolution[] destination,
            ref int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (destination[i].ResourceCatalogIndex ==
                        resolution.ResourceCatalogIndex &&
                    destination[i].GroupClipIndex == resolution.GroupClipIndex)
                {
                    if (destination[i].IsInvalid || !resolution.IsInvalid)
                        return;
                    destination[i] = resolution;
                    return;
                }
            }
            if (count >= destination.Length)
                throw new InvalidOperationException(
                    "Character Pose source readiness resolution capacity was exceeded.");
            destination[count++] = resolution;
        }

        static CharacterPoseSourceResourceResolution Aggregate(
            CharacterPoseSourceResourceResolution[] resolutions,
            int count)
        {
            CharacterPoseSourceResourceResolution pending = default;
            CharacterPoseSourceResourceResolution ready = default;
            bool hasPending = false;
            bool hasReady = false;
            for (int i = 0; i < count; i++)
            {
                CharacterPoseSourceResourceResolution resolution = resolutions[i];
                if (!resolution.IsValid)
                    throw new InvalidOperationException(
                        "Character Pose source resource resolution buffer contains an invalid value.");
                if (resolution.IsInvalid)
                    return resolution;
                if (resolution.IsPending && !hasPending)
                {
                    pending = resolution;
                    hasPending = true;
                    continue;
                }
                if (resolution.IsReady &&
                    (!hasReady ||
                     ready.ResourceCatalogIndex < 0 &&
                     resolution.ResourceCatalogIndex >= 0))
                {
                    ready = resolution;
                    hasReady = true;
                }
            }
            return hasPending
                ? pending
                : hasReady
                    ? ready
                    : ReadyResolution();
        }

        static CharacterPoseSourceResourceResolution InvalidResolution(
            CharacterAclResourceFailureCode failureCode,
            string message) =>
            new CharacterPoseSourceResourceResolution(
                CharacterAclResourceReadinessResult.Invalid(
                    failureCode,
                    message),
                -1,
                -1,
                0);

        static CharacterPoseSourceResourceResolution ReadyResolution() =>
            new CharacterPoseSourceResourceResolution(
                CharacterAclResourceReadinessResult.Ready(),
                -1,
                -1,
                0);
    }
}
