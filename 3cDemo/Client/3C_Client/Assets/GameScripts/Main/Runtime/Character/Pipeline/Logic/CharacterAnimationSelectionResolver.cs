using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Logic
{
    readonly struct CharacterAnimationSelectionCandidate
    {
        public CharacterAnimationSelectionCandidate(
            AnimationPlaybackId playbackId,
            string layerId,
            string sourceId,
            string sourceName,
            ulong actionInstanceId)
        {
            PlaybackId = playbackId;
            LayerId = layerId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            ActionInstanceId = actionInstanceId;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public string LayerId { get; }
        public string SourceId { get; }
        public string SourceName { get; }
        public ulong ActionInstanceId { get; }
    }

    readonly struct CharacterAnimationLayerDecision
    {
        public CharacterAnimationLayerDecision(
            string layerId,
            CharacterAnimationSelectionCandidate candidate,
            bool submit,
            bool hasPlayback)
        {
            LayerId = layerId ?? string.Empty;
            Candidate = candidate;
            Submit = submit;
            HasPlayback = hasPlayback;
        }

        public string LayerId { get; }
        public CharacterAnimationSelectionCandidate Candidate { get; }
        public bool Submit { get; }
        public bool HasPlayback { get; }
    }

    sealed class CharacterAnimationSelectionResolver
    {
        readonly Dictionary<string, List<CharacterAnimationSelectionCandidate>> m_LocomotionCandidates =
            new Dictionary<string, List<CharacterAnimationSelectionCandidate>>(StringComparer.Ordinal);
        readonly Dictionary<string, List<CharacterAnimationSelectionCandidate>> m_ActionCandidates =
            new Dictionary<string, List<CharacterAnimationSelectionCandidate>>(StringComparer.Ordinal);
        readonly Dictionary<string, ulong> m_ActionLayerOwners =
            new Dictionary<string, ulong>(StringComparer.Ordinal);
        readonly List<string> m_RemoveLayerOwners = new List<string>();
        readonly List<string> m_Errors = new List<string>();

        public IReadOnlyList<string> Errors => m_Errors;

        public bool Resolve(
            IReadOnlyList<ResolvedAnimationLayer> layers,
            IReadOnlyList<CharacterAnimationSelectionCandidate> candidates,
            ulong activeActionInstanceId,
            List<CharacterAnimationLayerDecision> decisions)
        {
            ClearCandidateMaps();
            m_Errors.Clear();
            decisions.Clear();

            for (int i = 0; i < candidates.Count; i++)
            {
                CharacterAnimationSelectionCandidate candidate = candidates[i];
                if (candidate.ActionInstanceId == 0)
                {
                    AddCandidate(m_LocomotionCandidates, candidate);
                    continue;
                }

                if (candidate.ActionInstanceId == activeActionInstanceId)
                    AddCandidate(m_ActionCandidates, candidate);
            }

            RemoveInactiveActionOwners(activeActionInstanceId);
            for (int i = 0; i < layers.Count; i++)
                ResolveLayer(layers[i], activeActionInstanceId, decisions);

            return m_Errors.Count == 0;
        }

        public void Reset()
        {
            ClearCandidateMaps();
            m_ActionLayerOwners.Clear();
            m_RemoveLayerOwners.Clear();
            m_Errors.Clear();
        }

        void ResolveLayer(
            ResolvedAnimationLayer layer,
            ulong activeActionInstanceId,
            List<CharacterAnimationLayerDecision> decisions)
        {
            m_ActionCandidates.TryGetValue(layer.Id, out List<CharacterAnimationSelectionCandidate> actionCandidates);
            m_LocomotionCandidates.TryGetValue(layer.Id, out List<CharacterAnimationSelectionCandidate> locomotionCandidates);

            if (HasConflict(actionCandidates))
            {
                AddConflict(layer.Id, "active action", actionCandidates);
                return;
            }

            if (HasConflict(locomotionCandidates))
            {
                AddConflict(layer.Id, "locomotion", locomotionCandidates);
                return;
            }

            if (actionCandidates != null && actionCandidates.Count == 1)
            {
                CharacterAnimationSelectionCandidate selected = actionCandidates[0];
                m_ActionLayerOwners[layer.Id] = selected.ActionInstanceId;
                decisions.Add(new CharacterAnimationLayerDecision(layer.Id, selected, true, true));
                return;
            }

            if (activeActionInstanceId != 0 &&
                m_ActionLayerOwners.TryGetValue(layer.Id, out ulong owner) &&
                owner == activeActionInstanceId)
            {
                decisions.Add(new CharacterAnimationLayerDecision(layer.Id, default, false, false));
                return;
            }

            if (locomotionCandidates != null && locomotionCandidates.Count == 1)
            {
                decisions.Add(new CharacterAnimationLayerDecision(layer.Id, locomotionCandidates[0], true, true));
                return;
            }

            bool submitEmpty = layer.OutputPolicy == AnimationLayerOutputPolicy.AllowEmpty;
            decisions.Add(new CharacterAnimationLayerDecision(layer.Id, default, submitEmpty, false));
        }

        void RemoveInactiveActionOwners(ulong activeActionInstanceId)
        {
            m_RemoveLayerOwners.Clear();
            foreach (KeyValuePair<string, ulong> pair in m_ActionLayerOwners)
            {
                if (pair.Value != activeActionInstanceId)
                    m_RemoveLayerOwners.Add(pair.Key);
            }

            for (int i = 0; i < m_RemoveLayerOwners.Count; i++)
                m_ActionLayerOwners.Remove(m_RemoveLayerOwners[i]);
            m_RemoveLayerOwners.Clear();
        }

        void AddConflict(
            string layerId,
            string ownership,
            IReadOnlyList<CharacterAnimationSelectionCandidate> candidates)
        {
            string sources = string.Empty;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i > 0)
                    sources += ", ";
                sources += string.IsNullOrEmpty(candidates[i].SourceName)
                    ? candidates[i].SourceId
                    : candidates[i].SourceName;
            }
            m_Errors.Add($"Animation layer '{layerId}' received multiple {ownership} producers: {sources}.");
        }

        static void AddCandidate(
            Dictionary<string, List<CharacterAnimationSelectionCandidate>> map,
            CharacterAnimationSelectionCandidate candidate)
        {
            if (!map.TryGetValue(candidate.LayerId, out List<CharacterAnimationSelectionCandidate> values))
            {
                values = new List<CharacterAnimationSelectionCandidate>();
                map.Add(candidate.LayerId, values);
            }
            values.Add(candidate);
        }

        static bool HasConflict(IReadOnlyCollection<CharacterAnimationSelectionCandidate> candidates)
        {
            return candidates != null && candidates.Count > 1;
        }

        void ClearCandidateMaps()
        {
            ClearCandidateMap(m_LocomotionCandidates);
            ClearCandidateMap(m_ActionCandidates);
        }

        static void ClearCandidateMap(Dictionary<string, List<CharacterAnimationSelectionCandidate>> map)
        {
            foreach (List<CharacterAnimationSelectionCandidate> candidates in map.Values)
                candidates.Clear();
            map.Clear();
        }
    }
}
