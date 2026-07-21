using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    public sealed class AnimancerPlaybackAdapter : IAnimationPlaybackAdapter, IDisposable
    {
        readonly AnimancerComponent m_Animancer;
        readonly CharacterAnimationPresentationBindingIndex m_Bindings;
        readonly TransitionLibrary m_TransitionLibrary;
        readonly Dictionary<AnimationProducerId, ProducerVisual> m_ProducerVisuals =
            new Dictionary<AnimationProducerId, ProducerVisual>();
        readonly Dictionary<AnimationPlaybackId, ProducerVisual> m_PlaybackVisuals =
            new Dictionary<AnimationPlaybackId, ProducerVisual>();
        readonly Dictionary<AnimancerState, ProducerVisual> m_StateVisuals =
            new Dictionary<AnimancerState, ProducerVisual>();
        readonly HashSet<AnimationPlaybackId> m_SupersededPlaybacks =
            new HashSet<AnimationPlaybackId>();
        readonly HashSet<AnimationPlaybackId> m_EmptyFadePlaybacks =
            new HashSet<AnimationPlaybackId>();
        readonly bool m_ManageGraphClock;
        readonly AnimationTransitionEvaluationMode m_TransitionEvaluationMode;

        bool m_Disposed;

        public AnimancerPlaybackAdapter(
            AnimancerComponent animancer,
            CharacterAnimationPresentationBindingIndex bindings,
            bool manageGraphClock,
            AnimationTransitionEvaluationMode transitionEvaluationMode)
        {
            m_Animancer = animancer ? animancer : throw new ArgumentNullException(nameof(animancer));
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            if (!bindings.IsValid || !bindings.TransitionLibrary || bindings.TransitionLibrary.Library == null)
                throw new ArgumentException("Animation Presentation bindings are invalid.", nameof(bindings));
            if (!Enum.IsDefined(typeof(AnimationTransitionEvaluationMode), transitionEvaluationMode))
                throw new ArgumentOutOfRangeException(nameof(transitionEvaluationMode), transitionEvaluationMode, null);

            m_TransitionLibrary = bindings.TransitionLibrary.Library;
            m_ManageGraphClock = manageGraphClock;
            m_TransitionEvaluationMode = transitionEvaluationMode;
            m_Animancer.Graph.Transitions = m_TransitionLibrary;
            if (m_ManageGraphClock)
                m_Animancer.Graph.PauseGraph();
            foreach (ResolvedAnimationLayer layer in bindings.Layers.Values)
                ConfigureLayer(layer);
        }

        public void Play(
            ResolvedAnimationLayer layer,
            ResolvedAnimationProducerBinding binding,
            AnimationProducerSample sample)
        {
            if (!layer.Id.Equals(sample.LayerId, StringComparison.Ordinal) ||
                !binding.ProducerId.Equals(sample.PlaybackId.ProducerId) ||
                !sample.HasOutput)
                throw new InvalidOperationException($"Animation sample '{sample.PlaybackId}' does not match its presentation binding.");

            ProducerVisual visual = GetOrCreateVisual(layer, binding, sample);
            BindPlayback(visual, sample.PlaybackId);
            ApplySample(visual, sample);

            AnimancerLayer animancerLayer = m_Animancer.Layers[layer.AnimancerLayerIndex];
            object sourceTransitionKey = null;
            if (animancerLayer.CurrentState != null &&
                m_StateVisuals.TryGetValue(animancerLayer.CurrentState, out ProducerVisual sourceVisual))
            {
                sourceTransitionKey = sourceVisual.Binding.Transition.Key;
                if (sourceVisual.BoundPlayback.IsValid)
                    m_EmptyFadePlaybacks.Remove(sourceVisual.BoundPlayback);
            }

            float duration = m_TransitionEvaluationMode == AnimationTransitionEvaluationMode.Timed
                ? m_TransitionLibrary.GetFadeDuration(sourceTransitionKey, binding.Transition)
                : 0f;
            AnimancerState played = animancerLayer.Play(visual.State, duration, binding.Transition.FadeMode);
            if (!ReferenceEquals(played, visual.State))
                throw new InvalidOperationException(
                    $"Animation producer '{binding.ProducerId}' created a second Animancer state for one Timeline playback authority.");
            visual.State.Speed = 0f;
            visual.State.FadeGroup?.SetEasing(binding.Easing);
            ApplySample(visual, sample);
        }

        public void UpdateSample(AnimationProducerSample sample)
        {
            if (sample == null || !sample.HasOutput ||
                !m_PlaybackVisuals.TryGetValue(sample.PlaybackId, out ProducerVisual visual) ||
                !visual.BoundPlayback.Equals(sample.PlaybackId))
                return;
            ApplySample(visual, sample);
        }

        public void FadeToEmpty(ResolvedAnimationLayer layer, Easing.Function easing)
        {
            AnimancerLayer animancerLayer = m_Animancer.Layers[layer.AnimancerLayerIndex];
            float duration = 0f;
            if (animancerLayer.CurrentState != null &&
                m_StateVisuals.TryGetValue(animancerLayer.CurrentState, out ProducerVisual current))
            {
                if (m_TransitionEvaluationMode == AnimationTransitionEvaluationMode.Timed)
                    duration = current.Binding.Transition.FadeDuration;
                if (current.BoundPlayback.IsValid)
                    m_EmptyFadePlaybacks.Add(current.BoundPlayback);
            }
            animancerLayer.StartFade(0f, Mathf.Max(0f, duration));
            animancerLayer.FadeGroup?.SetEasing(easing);
        }

        public void Evaluate(float presentationDeltaSeconds)
        {
            m_Animancer.Evaluate(Mathf.Max(0f, presentationDeltaSeconds));
        }

        public bool IsRetired(AnimationPlaybackId playbackId)
        {
            if (m_SupersededPlaybacks.Contains(playbackId))
                return true;
            if (!m_PlaybackVisuals.TryGetValue(playbackId, out ProducerVisual visual))
                return true;
            if (!visual.BoundPlayback.Equals(playbackId))
                return true;
            if (m_EmptyFadePlaybacks.Contains(playbackId))
            {
                AnimancerLayer layer = m_Animancer.Layers[visual.Layer.AnimancerLayerIndex];
                return layer.Weight <= 0.0001f && layer.FadeGroup == null;
            }
            return !visual.State.IsPlaying &&
                   visual.State.Weight <= 0.0001f &&
                   visual.State.FadeGroup == null;
        }

        public float GetWeight(AnimationPlaybackId playbackId)
        {
            if (!m_PlaybackVisuals.TryGetValue(playbackId, out ProducerVisual visual) ||
                !visual.BoundPlayback.Equals(playbackId))
                return 0f;
            AnimancerLayer layer = m_Animancer.Layers[visual.Layer.AnimancerLayerIndex];
            return visual.State.Weight * layer.Weight;
        }

        public float GetFadeProgress(AnimationPlaybackId playbackId)
        {
            if (!m_PlaybackVisuals.TryGetValue(playbackId, out ProducerVisual visual) ||
                !visual.BoundPlayback.Equals(playbackId))
                return 1f;
            if (m_EmptyFadePlaybacks.Contains(playbackId))
            {
                AnimancerLayer layer = m_Animancer.Layers[visual.Layer.AnimancerLayerIndex];
                return layer.FadeGroup != null
                    ? Mathf.Clamp01(layer.FadeGroup.NormalizedTime)
                    : 1f;
            }
            return visual.State.FadeGroup != null
                ? Mathf.Clamp01(visual.State.FadeGroup.NormalizedTime)
                : 1f;
        }

        public bool TryGetVisualSnapshot(
            AnimationPlaybackId playbackId,
            out AnimationPlaybackVisualSnapshot snapshot)
        {
            if (m_PlaybackVisuals.TryGetValue(playbackId, out ProducerVisual visual) &&
                visual.BoundPlayback.Equals(playbackId) &&
                visual.HasSample)
            {
                snapshot = new AnimationPlaybackVisualSnapshot(visual.StateKey, visual.SampleTime);
                return snapshot.IsValid;
            }

            snapshot = default;
            return false;
        }

        public void CollectPoseContributions(
            string layerId,
            List<AnimationPoseContribution> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (KeyValuePair<AnimationPlaybackId, ProducerVisual> pair in m_PlaybackVisuals)
            {
                ProducerVisual visual = pair.Value;
                if (!visual.HasSample ||
                    !visual.BoundPlayback.Equals(pair.Key) ||
                    !string.Equals(visual.Layer.Id, layerId, StringComparison.Ordinal))
                    continue;
                float weight = GetWeight(pair.Key);
                if (weight <= 0.0001f)
                    continue;
                destination.Add(new AnimationPoseContribution(
                    visual.Layer.Id,
                    visual.ProgramProducerIndex,
                    pair.Key,
                    visual.SampleTime,
                    visual.NormalizedTime,
                    visual.Cycle,
                    visual.VisualTimeScale,
                    Mathf.Clamp01(weight)));
            }
            destination.Sort(AnimationPoseContributionComparer.Instance);
        }

        public void Release(AnimationPlaybackId playbackId)
        {
            m_PlaybackVisuals.Remove(playbackId);
            m_SupersededPlaybacks.Remove(playbackId);
            m_EmptyFadePlaybacks.Remove(playbackId);
        }

        public void Clear()
        {
            if (m_Animancer)
            {
                foreach (ProducerVisual visual in m_ProducerVisuals.Values)
                {
                    if (visual.State.IsValid())
                        visual.State.Stop();
                    visual.BoundPlayback = default;
                }
                foreach (ResolvedAnimationLayer layer in m_Bindings.Layers.Values)
                {
                    AnimancerLayer animancerLayer = m_Animancer.Layers[layer.AnimancerLayerIndex];
                    animancerLayer.CancelFade();
                    animancerLayer.Weight = layer.OutputPolicy == AnimationLayerOutputPolicy.AllowEmpty ? 0f : 1f;
                }
                m_Animancer.Evaluate(0f);
            }
            m_PlaybackVisuals.Clear();
            m_SupersededPlaybacks.Clear();
            m_EmptyFadePlaybacks.Clear();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            foreach (ProducerVisual visual in m_ProducerVisuals.Values)
            {
                if (visual.State.IsValid())
                    visual.State.Destroy();
            }
            m_ProducerVisuals.Clear();
            m_PlaybackVisuals.Clear();
            m_StateVisuals.Clear();
            m_SupersededPlaybacks.Clear();
            m_EmptyFadePlaybacks.Clear();
            if (m_ManageGraphClock && m_Animancer && m_Animancer.IsGraphInitialized)
                m_Animancer.Graph.UnpauseGraph();
        }

        ProducerVisual GetOrCreateVisual(
            ResolvedAnimationLayer layer,
            ResolvedAnimationProducerBinding binding,
            AnimationProducerSample sample)
        {
            if (m_ProducerVisuals.TryGetValue(binding.ProducerId, out ProducerVisual visual))
                return visual;

            var key = new AnimationProducerStateKey(binding.ProducerId);
            AnimancerState state;
            ManualMixerState mixer = null;
            if (binding.UsesMixer)
            {
                mixer = new ManualMixerState { Key = key };
                mixer.SetParent(m_Animancer.Layers[layer.AnimancerLayerIndex]);
                state = mixer;
            }
            else
            {
                AnimationClip clip = sample.Clips[0].Clip;
                state = m_Animancer.Layers[layer.AnimancerLayerIndex].GetOrCreateState(key, clip);
            }

            visual = new ProducerVisual(
                layer,
                binding,
                ResolveProgramProducerIndex(binding.ProducerId),
                state,
                mixer);
            m_ProducerVisuals.Add(binding.ProducerId, visual);
            m_StateVisuals[state] = visual;
            return visual;
        }

        void BindPlayback(ProducerVisual visual, AnimationPlaybackId playbackId)
        {
            if (visual.BoundPlayback.IsValid && !visual.BoundPlayback.Equals(playbackId))
            {
                m_SupersededPlaybacks.Add(visual.BoundPlayback);
                m_PlaybackVisuals.Remove(visual.BoundPlayback);
            }
            visual.BoundPlayback = playbackId;
            m_PlaybackVisuals[playbackId] = visual;
        }

        static void ApplySample(ProducerVisual visual, AnimationProducerSample sample)
        {
            visual.SampleTime = sample.SampleTime;
            visual.Cycle = sample.Cycle;
            visual.VisualTimeScale = sample.VisualTimeScale;
            visual.HasSample = true;
            float normalizedWeight = 0f;
            float normalizedTime = 0f;
            for (int i = 0; i < sample.Clips.Count; i++)
            {
                normalizedTime += sample.Clips[i].NormalizedTime * sample.Clips[i].Weight;
                normalizedWeight += sample.Clips[i].Weight;
            }
            visual.NormalizedTime = normalizedWeight > 0f
                ? Mathf.Clamp01(normalizedTime / normalizedWeight)
                : 0f;
            if (visual.Mixer == null)
            {
                if (sample.Clips.Count != 1 || !ReferenceEquals(visual.State.Clip, sample.Clips[0].Clip))
                    throw new InvalidOperationException($"Single-clip animation producer '{sample.PlaybackId.ProducerId}' changed its authored clip.");
                AnimationClipSample clip = sample.Clips[0];
                visual.State.Speed = 0f;
                visual.State.Time = clip.ClipTime;
                return;
            }

            foreach (ClipState child in visual.Children.Values)
                child.Weight = 0f;
            for (int i = 0; i < sample.Clips.Count; i++)
            {
                AnimationClipSample clip = sample.Clips[i];
                if (!visual.Children.TryGetValue(clip.ClipAuthoringId, out ClipState child))
                {
                    child = visual.Mixer.Add(clip.Clip);
                    child.Key = new AnimationProducerClipKey(visual.Binding.ProducerId, clip.ClipAuthoringId);
                    visual.Mixer.DontSynchronize(child);
                    visual.Children.Add(clip.ClipAuthoringId, child);
                }
                else if (!ReferenceEquals(child.Clip, clip.Clip))
                {
                    throw new InvalidOperationException(
                        $"Animation clip identity '{clip.ClipAuthoringId}' changed its clip reference.");
                }
                child.IsPlaying = true;
                child.Speed = 0f;
                child.Time = clip.ClipTime;
                child.Weight = clip.Weight;
            }
            visual.Mixer.Speed = 0f;
        }

        void ConfigureLayer(ResolvedAnimationLayer layer)
        {
            AnimancerLayer animancerLayer = m_Animancer.Layers[layer.AnimancerLayerIndex];
            animancerLayer.SetLayerWeightOnPlay = true;
            animancerLayer.IsAdditive = layer.BlendMode ==
                                        ThirdPersonCharacter.Pipeline.Animation.AnimationBlendMode.Additive;
            animancerLayer.Mask = layer.AvatarMask;
            if (layer.OutputPolicy == AnimationLayerOutputPolicy.AllowEmpty)
                animancerLayer.Weight = 0f;
        }

        int ResolveProgramProducerIndex(AnimationProducerId producerId)
        {
            IReadOnlyList<CharacterPresentationProducerEntry> producers = m_Bindings.Projection.Producers;
            for (int i = 0; i < producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = producers[i];
                if (producer.Kind == CharacterPresentationProducerKind.Animation &&
                    producer.ProducerId.Equals(producerId))
                    return producer.ProgramProducerIndex;
            }
            throw new InvalidOperationException(
                $"Animation producer '{producerId}' is absent from the compiled Projection.");
        }

        sealed class ProducerVisual
        {
            public ProducerVisual(
                ResolvedAnimationLayer layer,
                ResolvedAnimationProducerBinding binding,
                int programProducerIndex,
                AnimancerState state,
                ManualMixerState mixer)
            {
                Layer = layer;
                Binding = binding;
                ProgramProducerIndex = programProducerIndex;
                State = state;
                Mixer = mixer;
                StateKey = state.Key?.ToString() ?? binding.ProducerId.ToString();
            }

            public ResolvedAnimationLayer Layer { get; }
            public ResolvedAnimationProducerBinding Binding { get; }
            public int ProgramProducerIndex { get; }
            public AnimancerState State { get; set; }
            public ManualMixerState Mixer { get; }
            public string StateKey { get; }
            public AnimationPlaybackId BoundPlayback { get; set; }
            public float SampleTime { get; set; }
            public float NormalizedTime { get; set; }
            public int Cycle { get; set; }
            public float VisualTimeScale { get; set; }
            public bool HasSample { get; set; }
            public Dictionary<string, ClipState> Children { get; } =
                new Dictionary<string, ClipState>(StringComparer.Ordinal);
        }

        sealed class AnimationPoseContributionComparer : IComparer<AnimationPoseContribution>
        {
            public static readonly AnimationPoseContributionComparer Instance =
                new AnimationPoseContributionComparer();

            public int Compare(AnimationPoseContribution left, AnimationPoseContribution right)
            {
                int producer = left.ProgramProducerIndex.CompareTo(right.ProgramProducerIndex);
                return producer != 0
                    ? producer
                    : left.PlaybackId.Generation.CompareTo(right.PlaybackId.Generation);
            }
        }

        sealed class AnimationProducerStateKey
        {
            readonly AnimationProducerId m_ProducerId;

            public AnimationProducerStateKey(AnimationProducerId producerId)
            {
                m_ProducerId = producerId;
            }

            public override string ToString() => m_ProducerId.ToString();
        }

        sealed class AnimationProducerClipKey
        {
            readonly AnimationProducerId m_ProducerId;
            readonly string m_ClipAuthoringId;

            public AnimationProducerClipKey(AnimationProducerId producerId, string clipAuthoringId)
            {
                m_ProducerId = producerId;
                m_ClipAuthoringId = clipAuthoringId ?? string.Empty;
            }

            public override string ToString() => $"{m_ProducerId}/{m_ClipAuthoringId}";
        }
    }
}
