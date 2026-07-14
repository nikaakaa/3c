using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationClipSample
    {
        public AnimationClipSample(
            string clipAuthoringId,
            RuntimeSourceElementHandle sourceHandle,
            AnimationClip clip,
            float clipTime,
            float normalizedTime,
            float weight,
            bool isLooping,
            float clipLoopStartTime,
            float clipLoopDuration,
            float continuousClipTime)
        {
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            SourceHandle = sourceHandle;
            Clip = clip;
            ClipTime = Mathf.Max(0f, clipTime);
            NormalizedTime = Mathf.Clamp01(normalizedTime);
            Weight = Mathf.Clamp01(weight);
            IsLooping = isLooping && clipLoopDuration > 0f;
            ClipLoopStartTime = Mathf.Max(0f, clipLoopStartTime);
            ClipLoopDuration = Mathf.Max(0f, clipLoopDuration);
            ContinuousClipTime = IsLooping ? Mathf.Max(0f, continuousClipTime) : ClipTime;
        }

        public string ClipAuthoringId { get; }
        public RuntimeSourceElementHandle SourceHandle { get; }
        public AnimationClip Clip { get; }
        public float ClipTime { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public bool IsLooping { get; }
        public float ClipLoopStartTime { get; }
        public float ClipLoopDuration { get; }
        public float ContinuousClipTime { get; }
        public bool IsValid => !string.IsNullOrEmpty(ClipAuthoringId) && Clip && Weight > 0f;
    }

    public sealed class AnimationProducerSample
    {
        readonly AnimationClipSample[] m_Clips;

        public AnimationProducerSample(
            AnimationPlaybackId playbackId,
            string layerId,
            string sourceId,
            string sourceName,
            string trackName,
            float sampleTime,
            int cycle,
            IReadOnlyList<AnimationClipSample> clips)
        {
            PlaybackId = playbackId;
            LayerId = layerId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            SampleTime = Mathf.Max(0f, sampleTime);
            Cycle = Mathf.Max(0, cycle);
            m_Clips = clips == null || clips.Count == 0
                ? Array.Empty<AnimationClipSample>()
                : CopyClips(clips);
        }

        public AnimationPlaybackId PlaybackId { get; }
        public string LayerId { get; }
        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public float SampleTime { get; }
        public int Cycle { get; }
        public IReadOnlyList<AnimationClipSample> Clips => m_Clips;
        public bool HasOutput => m_Clips.Length > 0;
        public bool IsValid
        {
            get
            {
                if (!PlaybackId.IsValid || string.IsNullOrEmpty(LayerId))
                    return false;
                for (int i = 0; i < m_Clips.Length; i++)
                {
                    if (!m_Clips[i].IsValid)
                        return false;
                }
                return true;
            }
        }

        static AnimationClipSample[] CopyClips(IReadOnlyList<AnimationClipSample> clips)
        {
            var result = new AnimationClipSample[clips.Count];
            for (int i = 0; i < clips.Count; i++)
                result[i] = clips[i];
            return result;
        }
    }

    public readonly struct AnimationLayerSelection
    {
        public AnimationLayerSelection(
            string layerId,
            AnimationPlaybackId playbackId,
            bool hasPlayback,
            ulong localLogicTick,
            ulong sequence)
        {
            LayerId = layerId ?? string.Empty;
            PlaybackId = playbackId;
            HasPlayback = hasPlayback;
            LocalLogicTick = localLogicTick;
            Sequence = sequence;
        }

        public string LayerId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public bool HasPlayback { get; }
        public ulong LocalLogicTick { get; }
        public ulong Sequence { get; }
        public bool IsValid => !string.IsNullOrEmpty(LayerId) &&
                               LocalLogicTick != 0 &&
                               Sequence != 0 &&
                               (!HasPlayback || PlaybackId.IsValid);

        public static AnimationLayerSelection Select(
            string layerId,
            AnimationPlaybackId playbackId,
            ulong localLogicTick,
            ulong sequence)
        {
            return new AnimationLayerSelection(layerId, playbackId, true, localLogicTick, sequence);
        }

        public static AnimationLayerSelection Empty(string layerId, ulong localLogicTick, ulong sequence)
        {
            return new AnimationLayerSelection(layerId, default, false, localLogicTick, sequence);
        }
    }

    public readonly struct AnimationLayerSelectionSource
    {
        public AnimationLayerSelectionSource(
            AnimationLayerSelection selection,
            string sourceId,
            string sourceName)
        {
            Selection = selection;
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
        }

        public AnimationLayerSelection Selection { get; }
        public string SourceId { get; }
        public string SourceName { get; }
    }

    public sealed class AnimationLayerSelectionBatch
    {
        readonly List<AnimationLayerSelectionSource> m_Selections = new List<AnimationLayerSelectionSource>();
        readonly Dictionary<string, AnimationLayerSelectionSource> m_ByLayer =
            new Dictionary<string, AnimationLayerSelectionSource>(StringComparer.Ordinal);
        readonly List<string> m_Errors = new List<string>();

        public IReadOnlyList<AnimationLayerSelectionSource> Selections => m_Selections;
        public IReadOnlyList<string> Errors => m_Errors;
        public bool IsValid => m_Errors.Count == 0;

        public void Begin()
        {
            m_Selections.Clear();
            m_ByLayer.Clear();
            m_Errors.Clear();
        }

        public bool Submit(AnimationLayerSelection selection, string sourceId, string sourceName)
        {
            var submitted = new AnimationLayerSelectionSource(selection, sourceId, sourceName);
            if (!selection.IsValid)
            {
                m_Errors.Add($"Animation selection from '{SourceLabel(submitted)}' is invalid.");
                return false;
            }

            if (m_ByLayer.TryGetValue(selection.LayerId, out AnimationLayerSelectionSource previous))
            {
                m_Errors.Add(
                    $"Animation layer '{selection.LayerId}' received multiple logic selections in one batch: " +
                    $"'{SourceLabel(previous)}' and '{SourceLabel(submitted)}'.");
                return false;
            }

            m_ByLayer.Add(selection.LayerId, submitted);
            m_Selections.Add(submitted);
            return true;
        }

        public void ReportError(string message)
        {
            if (!string.IsNullOrEmpty(message))
                m_Errors.Add(message);
        }

        static string SourceLabel(AnimationLayerSelectionSource source)
        {
            if (!string.IsNullOrEmpty(source.SourceName))
                return source.SourceName;
            if (!string.IsNullOrEmpty(source.SourceId))
                return source.SourceId;
            return source.Selection.HasPlayback ? source.Selection.PlaybackId.ToString() : "None";
        }
    }
}
