using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal readonly struct AnimationPoseSourcePrepareResult
    {
        internal AnimationPoseSourcePrepareResult(
            AnimationPoseSourceId sourceId,
            ulong completionIdentity,
            AnimationScriptPlayable output)
        {
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
            Output = output;
            if (!IsValid)
                throw new ArgumentException("Animation pose source prepare result is invalid.");
        }

        internal AnimationPoseSourceId SourceId { get; }
        internal ulong CompletionIdentity { get; }
        internal AnimationScriptPlayable Output { get; }
        internal bool IsValid => SourceId.IsValid && CompletionIdentity != 0 &&
                                 Output.IsValid() && Output.GetInputCount() == 1;
    }

    internal sealed class AnimancerPoseSamplingBackend : IDisposable
    {
        readonly AnimancerComponent m_Animancer;
        readonly AnimancerGraph m_Graph;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly Transform m_RootTransform;
        readonly NativeArray<TransformStreamHandle> m_Handles;
        readonly NativeArray<AnimationLocalBonePose> m_ReferencePose;
        readonly Dictionary<AnimationPlayerSourceKey, SourceVisual> m_Sources =
            new Dictionary<AnimationPlayerSourceKey, SourceVisual>();

        bool m_Disposed;

        internal AnimancerPoseSamplingBackend(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig)
        {
            if (!animancer)
                throw new ArgumentNullException(nameof(animancer));
            if (!rigBinding)
                throw new ArgumentNullException(nameof(rigBinding));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            rigBinding.RequireValid(rig);
            if (!animancer.Animator || rigBinding.Animator != animancer.Animator)
                throw new ArgumentException("Animation Rig Binding does not belong to the Animancer Animator.", nameof(rigBinding));
            AnimancerGraph graph = animancer.Graph;
            if (!graph.PlayableGraph.IsValid())
                throw new InvalidOperationException("Animancer pose sampling graph is unavailable.");

            NativeArray<TransformStreamHandle> handles = default;
            NativeArray<AnimationLocalBonePose> referencePose = default;
            try
            {
                handles = new NativeArray<TransformStreamHandle>(
                    rig.Bones.Count,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                referencePose = new NativeArray<AnimationLocalBonePose>(
                    rig.Bones.Count,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                for (int boneIndex = 0; boneIndex < rig.Bones.Count; boneIndex++)
                {
                    handles[boneIndex] = animancer.Animator.BindStreamTransform(rigBinding.Bones[boneIndex]);
                    CharacterAnimationRigBonePayload bone = rig.Bones[boneIndex];
                    referencePose[boneIndex] = new AnimationLocalBonePose(
                        bone.ReferenceLocalPosition,
                        bone.ReferenceLocalRotation,
                        bone.ReferenceLocalScale);
                }
            }
            catch
            {
                if (referencePose.IsCreated)
                    referencePose.Dispose();
                if (handles.IsCreated)
                    handles.Dispose();
                throw;
            }

            m_Animancer = animancer;
            m_Graph = graph;
            m_Rig = rig;
            m_RootTransform = rigBinding.Bones[rig.RootBoneIndex];
            m_Handles = handles;
            m_ReferencePose = referencePose;
        }

        internal Transform RootTransform
        {
            get
            {
                RequireAvailable();
                return m_RootTransform;
            }
        }

        internal void ApplyRootPolicy()
        {
            RequireAvailable();
            if (m_Rig.RootBonePolicy != CharacterAnimationRootBonePolicy.ExcludeSourceRoot)
                return;
            CharacterAnimationRigBonePayload root = m_Rig.Bones[m_Rig.RootBoneIndex];
            m_RootTransform.SetLocalPositionAndRotation(
                root.ReferenceLocalPosition,
                root.ReferenceLocalRotation);
            m_RootTransform.localScale = root.ReferenceLocalScale;
        }

        internal NativeArray<TransformStreamHandle> Handles
        {
            get
            {
                RequireAvailable();
                return m_Handles;
            }
        }

        internal AnimationPoseSourcePrepareResult PrepareOrUpdate(
            in AnimationSelectionFrame request,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId playerNodeId)
        {
            RequireAvailable();
            if (!request.IsValid || !playerNodeId.IsValid)
                throw new ArgumentException("Resolved animation pose request is invalid.", nameof(request));
            if (!capture.SourceId.Equals(request.SourceId))
                throw new ArgumentException("Animation pose capture SourceId does not match the request.", nameof(capture));

            var job = new AnimationSourcePoseCaptureJob(
                capture,
                m_Handles,
                m_ReferencePose,
                m_Rig.RootBoneIndex,
                m_Rig.RootBonePolicy,
                m_Rig.ScalePolicy);

            bool created = false;
            var key = new AnimationPlayerSourceKey(request.SourceId, playerNodeId);
            if (!m_Sources.TryGetValue(key, out SourceVisual visual))
            {
                visual = CreateSource(key, job);
                m_Sources.Add(key, visual);
                created = true;
            }

            try
            {
                ValidateClipBindings(visual, request.Clips);
                PrepareClips(visual, request.Clips);
                visual.CapturePlayable.SetJobData(job);
                return new AnimationPoseSourcePrepareResult(
                    request.SourceId,
                    capture.CompletionIdentity,
                    visual.CapturePlayable);
            }
            catch
            {
                if (created)
                {
                    m_Sources.Remove(key);
                    visual.Destroy(m_Graph.PlayableGraph);
                }
                throw;
            }
        }

        internal void Release(AnimationPoseSourceId sourceId, PoseNodeId playerNodeId)
        {
            RequireAvailable();
            if (!sourceId.IsValid || !playerNodeId.IsValid)
                throw new ArgumentException("Animation pose SourceId is invalid.", nameof(sourceId));
            var key = new AnimationPlayerSourceKey(sourceId, playerNodeId);
            if (!m_Sources.Remove(key, out SourceVisual visual))
                throw new KeyNotFoundException($"Animation pose source '{sourceId}' for Player '{playerNodeId}' is not prepared.");
            visual.Destroy(m_Graph.PlayableGraph);
        }

        internal void Clear()
        {
            RequireNotDisposed();
            DestroySources();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            try
            {
                DestroySources();
            }
            finally
            {
                m_ReferencePose.Dispose();
                m_Handles.Dispose();
                m_Disposed = true;
            }
        }

        void DestroySources()
        {
            PlayableGraph playableGraph = m_Graph.PlayableGraph;
            foreach (SourceVisual visual in m_Sources.Values)
                visual.Destroy(playableGraph);
            m_Sources.Clear();
        }

        SourceVisual CreateSource(AnimationPlayerSourceKey key, AnimationSourcePoseCaptureJob job)
        {
            var mixer = new ManualMixerState
            {
                Key = new AnimationPoseSourceMixerKey(key),
                Speed = 0f,
                IsPlaying = true,
                Weight = 1f
            };
            AnimationScriptPlayable capturePlayable = default;
            try
            {
                mixer.SetGraph(m_Graph);
                capturePlayable = AnimationScriptPlayable.Create(m_Graph.PlayableGraph, job, 1);
                capturePlayable.SetProcessInputs(true);
                m_Graph.PlayableGraph.Connect(mixer.Playable, 0, capturePlayable, 0);
                capturePlayable.SetInputWeight(0, 1f);
                return new SourceVisual(key, mixer, capturePlayable);
            }
            catch
            {
                if (capturePlayable.IsValid() && m_Graph.PlayableGraph.IsValid())
                    m_Graph.PlayableGraph.DestroyPlayable(capturePlayable);
                mixer.Destroy();
                throw;
            }
        }

        static void ValidateClipBindings(
            SourceVisual visual,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips)
        {
            for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            {
                ClipSamplePlan plan = clips[clipIndex];
                if (visual.Clips.TryGetValue(plan.ClipBindingIndex, out ClipState child) &&
                    !ReferenceEquals(child.Clip, plan.Clip))
                {
                    throw new InvalidOperationException(
                        $"Animation source '{visual.Key}' ClipBindingIndex #{plan.ClipBindingIndex} changed its clip reference.");
                }
            }
        }

        static void PrepareClips(
            SourceVisual visual,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips)
        {
            foreach (ClipState child in visual.Clips.Values)
                child.Weight = 0f;

            for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            {
                ClipSamplePlan plan = clips[clipIndex];
                if (!visual.Clips.TryGetValue(plan.ClipBindingIndex, out ClipState child))
                {
                    child = visual.Mixer.Add(plan.Clip);
                    child.Key = new AnimationPoseSourceClipKey(visual.Key, plan.ClipBindingIndex);
                    visual.Mixer.DontSynchronize(child);
                    visual.Clips.Add(plan.ClipBindingIndex, child);
                }
                child.IsPlaying = true;
                child.Speed = 0f;
                child.Time = plan.IsLooping ? (float)plan.ContinuousClipTime : plan.ClipTime;
                child.Weight = plan.Weight;
            }

            visual.Mixer.Speed = 0f;
            visual.Mixer.IsPlaying = true;
            visual.Mixer.Weight = 1f;
        }

        void RequireNotDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimancerPoseSamplingBackend));
        }

        void RequireAvailable()
        {
            RequireNotDisposed();
            if (!m_Animancer || !m_Graph.PlayableGraph.IsValid())
                throw new InvalidOperationException("Animancer pose sampling graph is unavailable.");
        }

        sealed class SourceVisual
        {
            public SourceVisual(
                AnimationPlayerSourceKey key,
                ManualMixerState mixer,
                AnimationScriptPlayable capturePlayable)
            {
                Key = key;
                Mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
                CapturePlayable = capturePlayable;
                if (!key.IsValid || !capturePlayable.IsValid())
                    throw new ArgumentException("Animation pose source visual is invalid.");
            }

            public AnimationPlayerSourceKey Key { get; }
            public ManualMixerState Mixer { get; }
            public AnimationScriptPlayable CapturePlayable { get; }
            public Dictionary<int, ClipState> Clips { get; } = new Dictionary<int, ClipState>();

            public void Destroy(PlayableGraph graph)
            {
                if (CapturePlayable.IsValid() && graph.IsValid())
                    graph.DestroyPlayable(CapturePlayable);
                Mixer.Destroy();
                Clips.Clear();
            }
        }

        readonly struct AnimationPoseSourceMixerKey : IEquatable<AnimationPoseSourceMixerKey>
        {
            public AnimationPoseSourceMixerKey(AnimationPlayerSourceKey source)
            {
                if (!source.IsValid)
                    throw new ArgumentException("Animation pose source mixer key is invalid.", nameof(source));
                Source = source;
            }

            public AnimationPlayerSourceKey Source { get; }
            public bool Equals(AnimationPoseSourceMixerKey other) => Source.Equals(other.Source);
            public override bool Equals(object obj) => obj is AnimationPoseSourceMixerKey other && Equals(other);
            public override int GetHashCode() => Source.GetHashCode();
            public override string ToString() => $"PoseSource/{Source}";
        }

        readonly struct AnimationPoseSourceClipKey : IEquatable<AnimationPoseSourceClipKey>
        {
            public AnimationPoseSourceClipKey(AnimationPlayerSourceKey source, int clipBindingIndex)
            {
                if (!source.IsValid || clipBindingIndex < 0)
                    throw new ArgumentException("Animation pose source clip key is invalid.");
                Source = source;
                ClipBindingIndex = clipBindingIndex;
            }

            public AnimationPlayerSourceKey Source { get; }
            public int ClipBindingIndex { get; }

            public bool Equals(AnimationPoseSourceClipKey other) =>
                Source.Equals(other.Source) && ClipBindingIndex == other.ClipBindingIndex;

            public override bool Equals(object obj) => obj is AnimationPoseSourceClipKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return Source.GetHashCode() * 397 ^ ClipBindingIndex;
                }
            }

            public override string ToString() => $"PoseSource/{Source}/Clip/{ClipBindingIndex}";
        }

        readonly struct AnimationPlayerSourceKey : IEquatable<AnimationPlayerSourceKey>
        {
            public AnimationPlayerSourceKey(AnimationPoseSourceId sourceId, PoseNodeId playerNodeId)
            {
                if (!sourceId.IsValid || !playerNodeId.IsValid)
                    throw new ArgumentException("Animation Player source key is invalid.");
                SourceId = sourceId;
                PlayerNodeId = playerNodeId;
            }

            public AnimationPoseSourceId SourceId { get; }
            public PoseNodeId PlayerNodeId { get; }
            public bool IsValid => SourceId.IsValid && PlayerNodeId.IsValid;

            public bool Equals(AnimationPlayerSourceKey other) =>
                SourceId.Equals(other.SourceId) && PlayerNodeId.Equals(other.PlayerNodeId);

            public override bool Equals(object obj) => obj is AnimationPlayerSourceKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return SourceId.GetHashCode() * 397 ^ PlayerNodeId.GetHashCode();
                }
            }

            public override string ToString() => $"{SourceId}/Player/{PlayerNodeId}";
        }
    }
}
