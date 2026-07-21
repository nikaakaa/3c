using System;
using System.Collections.Generic;
using System.Globalization;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEngine;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    enum TimelineFootContactSide : byte
    {
        Left = 0,
        Right = 1
    }

    readonly struct AnimationFootContactCandidate
    {
        public AnimationFootContactCandidate(
            TimelineFootContactSide side,
            float sourceNormalizedTime,
            float plantConfidence)
        {
            if (!float.IsFinite(sourceNormalizedTime) || sourceNormalizedTime < 0f || sourceNormalizedTime >= 1f)
                throw new ArgumentOutOfRangeException(nameof(sourceNormalizedTime));
            if (!float.IsFinite(plantConfidence) || plantConfidence < 0.5f || plantConfidence > 1f)
                throw new ArgumentOutOfRangeException(nameof(plantConfidence));
            Side = side;
            SourceNormalizedTime = sourceNormalizedTime;
            PlantConfidence = plantConfidence;
        }

        public TimelineFootContactSide Side { get; }
        public string MarkerId => Side == TimelineFootContactSide.Left
            ? TimelineFootContactMarkerProposal.LeftMarkerId
            : TimelineFootContactMarkerProposal.RightMarkerId;
        public float SourceNormalizedTime { get; }
        public float PlantConfidence { get; }
    }

    sealed class AnimationFootContactCandidateSet
    {
        AnimationFootContactCandidateSet(
            string artifactIdentityHash,
            string artifactContentHash,
            string revision,
            AnimationFootContactCandidate[] candidates)
        {
            ArtifactIdentityHash = artifactIdentityHash;
            ArtifactContentHash = artifactContentHash;
            Revision = revision;
            Candidates = candidates;
        }

        public string ArtifactIdentityHash { get; }
        public string ArtifactContentHash { get; }
        public string Revision { get; }
        public IReadOnlyList<AnimationFootContactCandidate> Candidates { get; }

        public static AnimationFootContactCandidateSet Build(
            UnityEngine.AnimationClip clip,
            AnimationFootAnalysisArtifact artifact)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (!clip.isLooping)
                throw new InvalidOperationException("Foot contact marker candidates require a looping AnimationClip.");
            if (!float.IsFinite(clip.length) || clip.length <= 0f)
                throw new InvalidOperationException("Foot contact marker candidates require a finite positive AnimationClip duration.");

            int intervals = Mathf.Max(2, Mathf.CeilToInt(clip.length * artifact.Identity.SampleRate));
            var candidates = new List<AnimationFootContactCandidate>();
            Collect(TimelineFootContactSide.Left, artifact.Features.Left.PlantConfidence, intervals, candidates);
            Collect(TimelineFootContactSide.Right, artifact.Features.Right.PlantConfidence, intervals, candidates);
            candidates.Sort(Compare);
            bool hasLeft = false;
            bool hasRight = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                hasLeft |= candidates[i].Side == TimelineFootContactSide.Left;
                hasRight |= candidates[i].Side == TimelineFootContactSide.Right;
            }
            if (!hasLeft || !hasRight)
                throw new InvalidOperationException("Foot contact marker candidates require at least one stable contact onset for each foot.");

            string identityHash = artifact.Identity.IdentityHash.Value;
            string contentHash = artifact.ContentHash.Value;
            var revisionParts = new List<string>
            {
                "animation-foot-contact-candidates/v1",
                identityHash,
                contentHash,
                intervals.ToString(CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < candidates.Count; i++)
            {
                AnimationFootContactCandidate candidate = candidates[i];
                revisionParts.Add(candidate.MarkerId);
                revisionParts.Add(BitConverter.SingleToInt32Bits(candidate.SourceNormalizedTime).ToString("x8", CultureInfo.InvariantCulture));
                revisionParts.Add(BitConverter.SingleToInt32Bits(candidate.PlantConfidence).ToString("x8", CultureInfo.InvariantCulture));
            }
            return new AnimationFootContactCandidateSet(
                identityHash,
                contentHash,
                StableHash.Compute(revisionParts.ToArray()).Value,
                candidates.ToArray());
        }

        static void Collect(
            TimelineFootContactSide side,
            AnimationCurve plantConfidence,
            int intervals,
            List<AnimationFootContactCandidate> destination)
        {
            if (plantConfidence == null)
                throw new InvalidOperationException($"{side} foot PlantConfidence is missing.");
            for (int i = 0; i < intervals; i++)
            {
                int previousIndex = i == 0 ? intervals - 1 : i - 1;
                float previous = plantConfidence.Evaluate(previousIndex / (float)intervals);
                float current = plantConfidence.Evaluate(i / (float)intervals);
                if (!float.IsFinite(previous) || !float.IsFinite(current))
                    throw new InvalidOperationException($"{side} foot PlantConfidence contains a non-finite sample.");
                if (previous < 0.5f && current >= 0.5f)
                    destination.Add(new AnimationFootContactCandidate(side, i / (float)intervals, current));
            }
        }

        static int Compare(AnimationFootContactCandidate left, AnimationFootContactCandidate right)
        {
            int time = left.SourceNormalizedTime.CompareTo(right.SourceNormalizedTime);
            return time != 0 ? time : left.Side.CompareTo(right.Side);
        }
    }

    readonly struct TimelineFootContactMarkerCandidate
    {
        public TimelineFootContactMarkerCandidate(AnimationFootContactCandidate source, int timelineFrame)
        {
            if (timelineFrame < 0)
                throw new ArgumentOutOfRangeException(nameof(timelineFrame));
            Source = source;
            TimelineFrame = timelineFrame;
        }

        public AnimationFootContactCandidate Source { get; }
        public string MarkerId => Source.MarkerId;
        public int TimelineFrame { get; }
    }

    sealed class TimelineFootContactMarkerProposal
    {
        public const string LeftMarkerId = "LeftFootContact";
        public const string RightMarkerId = "RightFootContact";

        TimelineFootContactMarkerProposal(
            string revision,
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId,
            AnimationFootContactCandidateSet source,
            TimelineFootContactMarkerCandidate[] candidates)
        {
            Revision = revision;
            TimelineAuthoringId = timelineAuthoringId;
            TrackAuthoringId = trackAuthoringId;
            ClipAuthoringId = clipAuthoringId;
            Source = source;
            Candidates = candidates;
        }

        public string Revision { get; }
        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public AnimationFootContactCandidateSet Source { get; }
        public IReadOnlyList<TimelineFootContactMarkerCandidate> Candidates { get; }

        public static TimelineFootContactMarkerProposal Build(
            TimelineData timeline,
            AnimationTrack track,
            TimelineAnimationClip clip,
            AnimationFootAnalysisArtifact artifact)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (track == null)
                throw new ArgumentNullException(nameof(track));
            if (clip == null || !clip.Clip)
                throw new ArgumentNullException(nameof(clip));
            if (!ReferenceEquals(clip.Track, track))
                throw new InvalidOperationException("Foot contact marker proposal Clip does not belong to the target AnimationTrack.");
            if (track.SyncMode != AnimationSyncMode.MarkerGroup ||
                track.SequenceTopology != AnimationMarkerSequenceTopology.Cyclic)
                throw new InvalidOperationException("Foot contact marker proposal requires a MarkerGroup/Cyclic AnimationTrack.");
            if (timeline.MaxFrame <= 0)
                throw new InvalidOperationException("Foot contact marker proposal requires a positive Timeline duration.");

            int animationClipCount = 0;
            TimelineAnimationClip onlyClip = null;
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is not TimelineAnimationClip value || !value.Clip)
                    continue;
                animationClipCount++;
                onlyClip = value;
            }
            if (animationClipCount != 1 || !ReferenceEquals(onlyClip, clip))
                throw new InvalidOperationException("Foot contact marker proposal requires exactly one AnimationClip on the target track.");
            if (clip.StartFrame != 0 || clip.EndFrame != timeline.MaxFrame)
                throw new InvalidOperationException("Foot contact marker proposal requires the selected AnimationClip to exactly cover the Timeline.");

            AnimationFootContactCandidateSet source = AnimationFootContactCandidateSet.Build(clip.Clip, artifact);
            int sourceCycleFrames = clip.Length;
            if (sourceCycleFrames <= 0)
                throw new InvalidOperationException("Foot contact marker proposal source cycle has no frames.");
            var mapped = new List<TimelineFootContactMarkerCandidate>();
            var occupiedFrames = new HashSet<int>();
            for (int i = 0; i < source.Candidates.Count; i++)
            {
                AnimationFootContactCandidate candidate = source.Candidates[i];
                int sourceFrame = Mathf.Clamp(
                    Mathf.RoundToInt(candidate.SourceNormalizedTime * sourceCycleFrames),
                    0,
                    sourceCycleFrames - 1);
                int firstFrame = PositiveModulo(sourceFrame - clip.ClipInFrame, sourceCycleFrames);
                for (int frame = firstFrame; frame < timeline.MaxFrame; frame += sourceCycleFrames)
                {
                    if (!occupiedFrames.Add(frame))
                        throw new InvalidOperationException($"Foot contact candidates collide at Timeline frame {frame}.");
                    mapped.Add(new TimelineFootContactMarkerCandidate(candidate, frame));
                }
            }
            mapped.Sort((left, right) =>
            {
                int frame = left.TimelineFrame.CompareTo(right.TimelineFrame);
                return frame != 0 ? frame : string.CompareOrdinal(left.MarkerId, right.MarkerId);
            });
            if (mapped.Count < 2)
                throw new InvalidOperationException("Foot contact marker proposal does not cover both feet on the target Timeline.");

            for (int i = 0; i < track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = track.SyncMarkers[i];
                if (marker == null || IsFootMarker(marker.MarkerId))
                    continue;
                if (occupiedFrames.Contains(marker.Frame))
                    throw new InvalidOperationException(
                        $"Foot contact candidate frame {marker.Frame} conflicts with business marker '{marker.MarkerId}'.");
            }

            var revisionParts = new List<string>
            {
                "timeline-foot-contact-marker-proposal/v1",
                timeline.AuthoringId,
                track.AuthoringId,
                clip.AuthoringId,
                source.Revision,
                timeline.MaxFrame.ToString(CultureInfo.InvariantCulture),
                clip.StartFrame.ToString(CultureInfo.InvariantCulture),
                clip.EndFrame.ToString(CultureInfo.InvariantCulture),
                clip.ClipInFrame.ToString(CultureInfo.InvariantCulture),
                sourceCycleFrames.ToString(CultureInfo.InvariantCulture),
                clip.ExtraPolationMode.ToString(),
                track.AnimationChannelId.ToString(),
                track.SyncGroupId ?? string.Empty,
                track.SyncMode.ToString(),
                track.SequenceTopology.ToString(),
                track.SyncRole.ToString()
            };
            for (int i = 0; i < track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = track.SyncMarkers[i];
                revisionParts.Add(marker?.AuthoringId ?? string.Empty);
                revisionParts.Add(marker?.MarkerId ?? string.Empty);
                revisionParts.Add((marker?.Frame ?? -1).ToString(CultureInfo.InvariantCulture));
            }
            for (int i = 0; i < mapped.Count; i++)
            {
                revisionParts.Add(mapped[i].MarkerId);
                revisionParts.Add(mapped[i].TimelineFrame.ToString(CultureInfo.InvariantCulture));
            }
            return new TimelineFootContactMarkerProposal(
                StableHash.Compute(revisionParts.ToArray()).Value,
                timeline.AuthoringId,
                track.AuthoringId,
                clip.AuthoringId,
                source,
                mapped.ToArray());
        }

        public void Apply(AnimationTrack track)
        {
            if (track == null || !string.Equals(track.AuthoringId, TrackAuthoringId, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot contact marker proposal target track changed.");
            var reusable = new Dictionary<string, Queue<AnimationSyncMarker>>(StringComparer.Ordinal)
            {
                [LeftMarkerId] = new Queue<AnimationSyncMarker>(),
                [RightMarkerId] = new Queue<AnimationSyncMarker>()
            };
            var existingFootMarkers = new List<AnimationSyncMarker>();
            for (int i = 0; i < track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = track.SyncMarkers[i];
                if (marker == null || !IsFootMarker(marker.MarkerId))
                    continue;
                reusable[marker.MarkerId].Enqueue(marker);
                existingFootMarkers.Add(marker);
            }

            var reusedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Candidates.Count; i++)
            {
                TimelineFootContactMarkerCandidate candidate = Candidates[i];
                Queue<AnimationSyncMarker> queue = reusable[candidate.MarkerId];
                if (queue.Count == 0)
                {
                    track.AddMarker(candidate.MarkerId, candidate.TimelineFrame);
                    continue;
                }
                AnimationSyncMarker marker = queue.Dequeue();
                reusedIds.Add(marker.AuthoringId);
                track.EnsureMarker(marker.AuthoringId, candidate.MarkerId, candidate.TimelineFrame);
            }
            for (int i = 0; i < existingFootMarkers.Count; i++)
            {
                AnimationSyncMarker marker = existingFootMarkers[i];
                if (!reusedIds.Contains(marker.AuthoringId))
                    track.DeleteMarker(marker.AuthoringId);
            }
        }

        static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        static bool IsFootMarker(string markerId) =>
            string.Equals(markerId, LeftMarkerId, StringComparison.Ordinal) ||
            string.Equals(markerId, RightMarkerId, StringComparison.Ordinal);
    }
}
