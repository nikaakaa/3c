using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal enum AnimationPoseSourcePrepareKind : byte
    {
        CommittedUpdate = 1,
        PreparedResource = 2
    }

    internal readonly struct AnimationPoseSourceClipBinding
    {
        internal AnimationPoseSourceClipBinding(
            int clipBindingIndex,
            AnimationClip clip)
        {
            if (clipBindingIndex < 0 || !clip ||
                !float.IsFinite(clip.length) || clip.length <= 0f)
                throw new ArgumentException("Animation pose source clip binding is invalid.");
            ClipBindingIndex = clipBindingIndex;
            Clip = clip;
            ResourceCatalogIndex = -1;
            GroupClipIndex = -1;
        }

        internal AnimationPoseSourceClipBinding(
            int clipBindingIndex,
            int resourceCatalogIndex,
            int groupClipIndex)
        {
            if (clipBindingIndex < 0 || resourceCatalogIndex < 0 || groupClipIndex < 0)
                throw new ArgumentException("ACL animation pose source binding is invalid.");
            ClipBindingIndex = clipBindingIndex;
            Clip = null;
            ResourceCatalogIndex = resourceCatalogIndex;
            GroupClipIndex = groupClipIndex;
        }

        internal int ClipBindingIndex { get; }
        internal AnimationClip Clip { get; }
        internal int ResourceCatalogIndex { get; }
        internal int GroupClipIndex { get; }
        internal bool IsAcl => ResourceCatalogIndex >= 0;
        internal bool IsValid => ClipBindingIndex >= 0 &&
                                 (Clip && float.IsFinite(Clip.length) && Clip.length > 0f ||
                                  ResourceCatalogIndex >= 0 &&
                                   GroupClipIndex >= 0);
    }

    internal readonly struct AnimationPoseSourceReleaseToken
    {
        internal AnimationPoseSourceReleaseToken(
            int permissionIndex,
            ulong generation,
            CharacterAnimationSamplingBackendKind backend = CharacterAnimationSamplingBackendKind.NativeClip,
            AnimationPoseSourceId sourceId = default,
            PoseNodeId playerNodeId = default)
        {
            if (permissionIndex < 0 || generation == 0 ||
                !IsDefined(backend))
                throw new ArgumentException("Animation pose source release token is invalid.");
            PermissionIndex = permissionIndex;
            Generation = generation;
            Backend = backend;
            SourceId = sourceId;
            PlayerNodeId = playerNodeId;
        }

        internal int PermissionIndex { get; }
        internal ulong Generation { get; }
        internal CharacterAnimationSamplingBackendKind Backend { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal PoseNodeId PlayerNodeId { get; }
        internal bool IsValid => PermissionIndex >= 0 && Generation != 0 &&
                                 IsDefined(Backend);

        static bool IsDefined(CharacterAnimationSamplingBackendKind backend) =>
            backend == CharacterAnimationSamplingBackendKind.NativeClip ||
            backend == CharacterAnimationSamplingBackendKind.Acl;
    }

    internal readonly struct AnimationPoseSourcePrepareResult
    {
        internal AnimationPoseSourcePrepareResult(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId,
            ulong frameIdentity,
            ulong completionIdentity,
            AnimationScriptPlayable output,
            AnimationPoseSourcePrepareKind kind,
            in CharacterPoseSourceScalarReadView scalarReadView)
        {
            SourceId = sourceId;
            PlayerNodeId = playerNodeId;
            FrameIdentity = frameIdentity;
            CompletionIdentity = completionIdentity;
            Output = output;
            Kind = kind;
            ScalarReadView = scalarReadView;
            if (!IsValid)
                throw new ArgumentException("Animation pose source prepare result is invalid.");
        }

        internal AnimationPoseSourceId SourceId { get; }
        internal PoseNodeId PlayerNodeId { get; }
        internal ulong FrameIdentity { get; }
        internal ulong CompletionIdentity { get; }
        internal AnimationScriptPlayable Output { get; }
        internal AnimationPoseSourcePrepareKind Kind { get; }
        internal CharacterPoseSourceScalarReadView ScalarReadView { get; }
        internal bool IsPreparedResource => Kind == AnimationPoseSourcePrepareKind.PreparedResource;
            internal bool IsValid => SourceId.IsValid && PlayerNodeId.IsValid &&
                                  FrameIdentity != 0 && CompletionIdentity != 0 &&
                                  ((Playable)Output).IsValid() &&
                                  ((Playable)Output).GetInputCount() == 1 &&
                                  (Kind == AnimationPoseSourcePrepareKind.CommittedUpdate ||
                                  Kind == AnimationPoseSourcePrepareKind.PreparedResource) &&
                                  ScalarReadView.IsValid &&
                                  ScalarReadView.SourceId == SourceId &&
                                  ScalarReadView.CompletionIdentity == CompletionIdentity;
    }
}
