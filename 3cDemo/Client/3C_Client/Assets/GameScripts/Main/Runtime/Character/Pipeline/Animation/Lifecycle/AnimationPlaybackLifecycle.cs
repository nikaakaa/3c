using System;
using System.Collections.Generic;
using Animancer;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public enum AnimationPlaybackLifecyclePhase
    {
        PendingFirstSample,
        Current,
        Outgoing,
        Retired
    }

    public readonly struct AnimationPlaybackLifecycleSnapshot
    {
        public AnimationPlaybackLifecycleSnapshot(
            string layerId,
            AnimationPlaybackId playbackId,
            AnimationPlaybackLifecyclePhase phase,
            float weight,
            float fadeProgress,
            string stateKey,
            float sampleTime,
            bool hasVisualSample)
        {
            LayerId = layerId ?? string.Empty;
            PlaybackId = playbackId;
            Phase = phase;
            Weight = weight;
            FadeProgress = fadeProgress;
            StateKey = stateKey ?? string.Empty;
            SampleTime = sampleTime;
            HasVisualSample = hasVisualSample;
        }

        public string LayerId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public AnimationPlaybackLifecyclePhase Phase { get; }
        public float Weight { get; }
        public float FadeProgress { get; }
        public string StateKey { get; }
        public float SampleTime { get; }
        public bool HasVisualSample { get; }
    }

    public readonly struct AnimationLayerPlaybackVisibility
    {
        public AnimationLayerPlaybackVisibility(
            string layerId,
            AnimationPlaybackId current,
            AnimationPlaybackId pending,
            IReadOnlyCollection<AnimationPlaybackId> outgoing)
        {
            LayerId = layerId ?? string.Empty;
            Current = current;
            Pending = pending;
            Outgoing = outgoing;
        }

        public string LayerId { get; }
        public AnimationPlaybackId Current { get; }
        public AnimationPlaybackId Pending { get; }
        public IReadOnlyCollection<AnimationPlaybackId> Outgoing { get; }
    }

    public sealed class AnimationPlaybackLifecycle
    {
        readonly CharacterAnimationPresentationBindingIndex m_Bindings;
        readonly IAnimationPlaybackAdapter m_Adapter;
        readonly List<LayerState> m_Layers = new List<LayerState>();
        readonly Dictionary<string, LayerState> m_LayersById =
            new Dictionary<string, LayerState>(StringComparer.Ordinal);
        readonly Dictionary<string, AnimationChannelSelection> m_LatestSelections =
            new Dictionary<string, AnimationChannelSelection>(StringComparer.Ordinal);
        readonly Dictionary<AnimationPlaybackId, AnimationProducerSample> m_LatestSamples =
            new Dictionary<AnimationPlaybackId, AnimationProducerSample>();
        readonly HashSet<AnimationPlaybackId> m_TerminalPlaybacks =
            new HashSet<AnimationPlaybackId>();
        readonly HashSet<AnimationPlaybackId> m_ReferencedPlaybacks =
            new HashSet<AnimationPlaybackId>();
        readonly List<AnimationPlaybackId> m_RemoveOutgoing = new List<AnimationPlaybackId>();
        readonly List<AnimationPlaybackId> m_RemoveTerminal = new List<AnimationPlaybackId>();

        public AnimationPlaybackLifecycle(
            CharacterAnimationPresentationBindingIndex bindings,
            IAnimationPlaybackAdapter adapter)
        {
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            m_Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            foreach (ResolvedAnimationLayer layer in bindings.Layers.Values)
            {
                var state = new LayerState(layer);
                m_Layers.Add(state);
                m_LayersById.Add(layer.Id, state);
            }
            m_Layers.Sort((left, right) => left.Layer.Order.CompareTo(right.Layer.Order));
        }

        public bool TryGetCurrentPlayback(string layerId, out AnimationPlaybackId playbackId)
        {
            if (m_LayersById.TryGetValue(layerId ?? string.Empty, out LayerState layer) && layer.Current.IsValid)
            {
                playbackId = layer.Current;
                return true;
            }
            playbackId = default;
            return false;
        }

        public bool TryGetPendingPlayback(string layerId, out AnimationPlaybackId playbackId)
        {
            if (m_LayersById.TryGetValue(layerId ?? string.Empty, out LayerState layer) && layer.Pending.IsValid)
            {
                playbackId = layer.Pending;
                return true;
            }
            playbackId = default;
            return false;
        }

        public bool Retains(AnimationPlaybackId playbackId)
        {
            if (!playbackId.IsValid)
                return false;
            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                if (layer.Current.Equals(playbackId) ||
                    layer.Pending.Equals(playbackId) ||
                    layer.Selection.HasPlayback && layer.Selection.PlaybackId.Equals(playbackId) ||
                    layer.Outgoing.Contains(playbackId))
                {
                    return true;
                }
            }
            return false;
        }

        public void BuildVisibilitySnapshot(List<AnimationLayerPlaybackVisibility> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                var outgoing = new AnimationPlaybackId[layer.Outgoing.Count];
                layer.Outgoing.CopyTo(outgoing);
                destination.Add(new AnimationLayerPlaybackVisibility(
                    layer.Layer.Id,
                    layer.Current,
                    layer.Pending,
                    outgoing));
            }
        }

        public void CollectSampleDemand(
            IReadOnlyList<AnimationPlaybackCommand> commands,
            HashSet<AnimationPlaybackId> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            m_LatestSelections.Clear();
            if (commands != null)
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    AnimationPlaybackCommand command = commands[i];
                    if (command.Kind == AnimationPlaybackCommandKind.Selection)
                        m_LatestSelections[command.Selection.LayerId] = command.Selection;
                }
            }

            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                if (layer.Current.IsValid)
                    destination.Add(layer.Current);
                if (layer.Pending.IsValid)
                    destination.Add(layer.Pending);
                foreach (AnimationPlaybackId outgoing in layer.Outgoing)
                    destination.Add(outgoing);

                AnimationChannelSelection selection = m_LatestSelections.TryGetValue(layer.Layer.Id, out AnimationChannelSelection latest)
                    ? latest
                    : layer.Selection;
                if (selection.HasPlayback && selection.PlaybackId.IsValid)
                    destination.Add(selection.PlaybackId);
            }
        }

        public void Apply(
            IReadOnlyList<AnimationPlaybackCommand> commands,
            float presentationDeltaSeconds,
            List<AnimationPlaybackId> retiredPlaybacks)
        {
            if (retiredPlaybacks == null)
                throw new ArgumentNullException(nameof(retiredPlaybacks));

            retiredPlaybacks.Clear();
            PrepareBatch(commands);
            ValidateBatch();
            CommitSelections();
            UpdateVisibleSamples();
            m_Adapter.Evaluate(presentationDeltaSeconds);
            RetireOutgoing(retiredPlaybacks);
            RetireUnreferencedTerminalPlaybacks(retiredPlaybacks);
        }

        public void BuildSnapshot(List<AnimationPlaybackLifecycleSnapshot> destination)
        {
            destination.Clear();
            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                if (layer.Pending.IsValid)
                    AddSnapshot(destination, layer.Layer.Id, layer.Pending, AnimationPlaybackLifecyclePhase.PendingFirstSample);
                if (layer.Current.IsValid)
                    AddSnapshot(destination, layer.Layer.Id, layer.Current, AnimationPlaybackLifecyclePhase.Current);
                foreach (AnimationPlaybackId outgoing in layer.Outgoing)
                    AddSnapshot(destination, layer.Layer.Id, outgoing, AnimationPlaybackLifecyclePhase.Outgoing);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < m_Layers.Count; i++)
                m_Layers[i].Reset();
            m_LatestSelections.Clear();
            m_LatestSamples.Clear();
            m_TerminalPlaybacks.Clear();
            m_ReferencedPlaybacks.Clear();
            m_RemoveOutgoing.Clear();
            m_RemoveTerminal.Clear();
            m_Adapter.Clear();
        }

        void PrepareBatch(IReadOnlyList<AnimationPlaybackCommand> commands)
        {
            m_LatestSelections.Clear();
            m_LatestSamples.Clear();
            if (commands == null)
                return;

            for (int i = 0; i < commands.Count; i++)
            {
                AnimationPlaybackCommand command = commands[i];
                switch (command.Kind)
                {
                    case AnimationPlaybackCommandKind.Selection:
                        m_LatestSelections[command.Selection.LayerId] = command.Selection;
                        break;
                    case AnimationPlaybackCommandKind.Sample:
                        if (command.Sample == null || !command.Sample.IsValid)
                            throw new InvalidOperationException("Animation playback batch contains an invalid producer sample.");
                        m_LatestSamples[command.Sample.PlaybackId] = command.Sample;
                        break;
                    case AnimationPlaybackCommandKind.Complete:
                    case AnimationPlaybackCommandKind.Release:
                        if (!command.PlaybackId.IsValid)
                            throw new InvalidOperationException("Animation playback batch contains an invalid terminal playback id.");
                        m_TerminalPlaybacks.Add(command.PlaybackId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
                }
            }
        }

        void ValidateBatch()
        {
            foreach (KeyValuePair<string, AnimationChannelSelection> pair in m_LatestSelections)
            {
                if (!m_LayersById.ContainsKey(pair.Key) || !pair.Value.IsValid)
                    throw new InvalidOperationException($"Animation selection targets unknown layer '{pair.Key}'.");
            }

            foreach (AnimationProducerSample sample in m_LatestSamples.Values)
            {
                if (!m_Bindings.TryGetBinding(sample.PlaybackId.ProducerId, out ResolvedAnimationProducerBinding binding))
                    throw new InvalidOperationException($"Animation sample targets unknown producer '{sample.PlaybackId.ProducerId}'.");
                if (!string.Equals(binding.LayerId, sample.LayerId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Animation sample '{sample.PlaybackId}' targets layer '{sample.LayerId}' instead of '{binding.LayerId}'.");
            }

            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                AnimationChannelSelection selection = m_LatestSelections.TryGetValue(layer.Layer.Id, out AnimationChannelSelection latest)
                    ? latest
                    : layer.Selection;
                if (!selection.IsValid)
                {
                    if (!layer.Current.IsValid &&
                        layer.Layer.OutputPolicy == AnimationLayerOutputPolicy.RequireOutput)
                    {
                        throw new InvalidOperationException(
                            $"Animation layer '{layer.Layer.Id}' requires output but has no current playback or logic selection.");
                    }
                    continue;
                }
                if (!selection.HasPlayback)
                {
                    if (layer.Layer.OutputPolicy == AnimationLayerOutputPolicy.RequireOutput)
                        throw new InvalidOperationException(
                            $"Animation layer '{layer.Layer.Id}' requires output but logic selected None.");
                    continue;
                }

                if (!m_Bindings.TryGetBinding(selection.PlaybackId.ProducerId, out ResolvedAnimationProducerBinding binding) ||
                    !string.Equals(binding.LayerId, layer.Layer.Id, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Animation layer '{layer.Layer.Id}' selected unknown producer '{selection.PlaybackId.ProducerId}'.");

                bool current = layer.Current.Equals(selection.PlaybackId);
                bool hasSample = m_LatestSamples.TryGetValue(selection.PlaybackId, out AnimationProducerSample sample) &&
                                 sample.HasOutput;
                if (!current && !hasSample && !layer.Current.IsValid &&
                    layer.Layer.OutputPolicy == AnimationLayerOutputPolicy.RequireOutput)
                    throw new InvalidOperationException(
                        $"Animation layer '{layer.Layer.Id}' selected '{selection.PlaybackId}' without a first output sample.");
                if (!current && !hasSample && m_TerminalPlaybacks.Contains(selection.PlaybackId))
                    throw new InvalidOperationException(
                        $"Animation layer '{layer.Layer.Id}' selected '{selection.PlaybackId}', but it completed before producing a first sample.");
                if (current && m_LatestSamples.TryGetValue(selection.PlaybackId, out sample) && !sample.HasOutput &&
                    layer.Layer.OutputPolicy == AnimationLayerOutputPolicy.RequireOutput)
                    throw new InvalidOperationException(
                        $"Animation layer '{layer.Layer.Id}' current producer '{selection.PlaybackId}' produced no output.");
            }
        }

        void CommitSelections()
        {
            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                if (m_LatestSelections.TryGetValue(layer.Layer.Id, out AnimationChannelSelection latest))
                    layer.Selection = latest;
                if (!layer.Selection.IsValid)
                    continue;

                if (!layer.Selection.HasPlayback)
                {
                    layer.Pending = default;
                    if (layer.Current.IsValid)
                    {
                        AnimationPlaybackId outgoing = layer.Current;
                        layer.Outgoing.Add(outgoing);
                        layer.Current = default;
                        Easing.Function easing = ResolveEasing(outgoing);
                        m_Adapter.FadeToEmpty(layer.Layer, easing);
                    }
                    continue;
                }

                AnimationPlaybackId target = layer.Selection.PlaybackId;
                if (layer.Current.Equals(target))
                {
                    layer.Pending = default;
                    continue;
                }

                if (!m_LatestSamples.TryGetValue(target, out AnimationProducerSample sample) || !sample.HasOutput)
                {
                    layer.Pending = target;
                    continue;
                }

                if (layer.Current.IsValid)
                    layer.Outgoing.Add(layer.Current);
                layer.Outgoing.Remove(target);
                layer.Pending = default;
                layer.Current = target;
                ResolvedAnimationProducerBinding binding = RequireBinding(target.ProducerId);
                m_Adapter.Play(layer.Layer, binding, sample);
            }
        }

        void UpdateVisibleSamples()
        {
            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                if (layer.Current.IsValid &&
                    m_LatestSamples.TryGetValue(layer.Current, out AnimationProducerSample currentSample) &&
                    currentSample.HasOutput)
                    m_Adapter.UpdateSample(currentSample);
                foreach (AnimationPlaybackId outgoing in layer.Outgoing)
                {
                    if (m_LatestSamples.TryGetValue(outgoing, out AnimationProducerSample outgoingSample) &&
                        outgoingSample.HasOutput)
                        m_Adapter.UpdateSample(outgoingSample);
                }
            }
        }

        void RetireOutgoing(List<AnimationPlaybackId> retiredPlaybacks)
        {
            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                m_RemoveOutgoing.Clear();
                foreach (AnimationPlaybackId outgoing in layer.Outgoing)
                {
                    if (!m_Adapter.IsRetired(outgoing))
                        continue;
                    m_RemoveOutgoing.Add(outgoing);
                    AddRetired(retiredPlaybacks, outgoing);
                }
                for (int removeIndex = 0; removeIndex < m_RemoveOutgoing.Count; removeIndex++)
                {
                    AnimationPlaybackId playbackId = m_RemoveOutgoing[removeIndex];
                    layer.Outgoing.Remove(playbackId);
                    m_Adapter.Release(playbackId);
                }
            }
        }

        void RetireUnreferencedTerminalPlaybacks(List<AnimationPlaybackId> retiredPlaybacks)
        {
            m_ReferencedPlaybacks.Clear();
            for (int i = 0; i < m_Layers.Count; i++)
            {
                LayerState layer = m_Layers[i];
                if (layer.Selection.HasPlayback)
                    m_ReferencedPlaybacks.Add(layer.Selection.PlaybackId);
                if (layer.Pending.IsValid)
                    m_ReferencedPlaybacks.Add(layer.Pending);
                if (layer.Current.IsValid)
                    m_ReferencedPlaybacks.Add(layer.Current);
                foreach (AnimationPlaybackId outgoing in layer.Outgoing)
                    m_ReferencedPlaybacks.Add(outgoing);
            }

            m_RemoveTerminal.Clear();
            foreach (AnimationPlaybackId terminal in m_TerminalPlaybacks)
            {
                if (m_ReferencedPlaybacks.Contains(terminal))
                    continue;
                m_RemoveTerminal.Add(terminal);
                AddRetired(retiredPlaybacks, terminal);
            }
            for (int i = 0; i < m_RemoveTerminal.Count; i++)
            {
                AnimationPlaybackId playbackId = m_RemoveTerminal[i];
                m_TerminalPlaybacks.Remove(playbackId);
                m_Adapter.Release(playbackId);
            }
        }

        ResolvedAnimationProducerBinding RequireBinding(AnimationProducerId producerId)
        {
            if (!m_Bindings.TryGetBinding(producerId, out ResolvedAnimationProducerBinding binding))
                throw new InvalidOperationException($"Animation producer '{producerId}' has no presentation binding.");
            return binding;
        }

        Easing.Function ResolveEasing(AnimationPlaybackId playbackId)
        {
            return RequireBinding(playbackId.ProducerId).Easing;
        }

        void AddSnapshot(
            List<AnimationPlaybackLifecycleSnapshot> destination,
            string layerId,
            AnimationPlaybackId playbackId,
            AnimationPlaybackLifecyclePhase phase)
        {
            bool hasVisualSample = m_Adapter.TryGetVisualSnapshot(
                playbackId,
                out AnimationPlaybackVisualSnapshot visualSnapshot);
            destination.Add(new AnimationPlaybackLifecycleSnapshot(
                layerId,
                playbackId,
                phase,
                m_Adapter.GetWeight(playbackId),
                m_Adapter.GetFadeProgress(playbackId),
                hasVisualSample ? visualSnapshot.StateKey : string.Empty,
                hasVisualSample ? visualSnapshot.SampleTime : 0f,
                hasVisualSample));
        }

        static void AddRetired(List<AnimationPlaybackId> retiredPlaybacks, AnimationPlaybackId playbackId)
        {
            if (!retiredPlaybacks.Contains(playbackId))
                retiredPlaybacks.Add(playbackId);
        }

        sealed class LayerState
        {
            public LayerState(ResolvedAnimationLayer layer)
            {
                Layer = layer;
            }

            public ResolvedAnimationLayer Layer { get; }
            public AnimationChannelSelection Selection { get; set; }
            public AnimationPlaybackId Pending { get; set; }
            public AnimationPlaybackId Current { get; set; }
            public HashSet<AnimationPlaybackId> Outgoing { get; } = new HashSet<AnimationPlaybackId>();

            public void Reset()
            {
                Selection = default;
                Pending = default;
                Current = default;
                Outgoing.Clear();
            }
        }
    }
}
