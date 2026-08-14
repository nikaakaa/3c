using System;
using System.Collections.Generic;
using System.Globalization;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
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
            AnimationFootAnalysisArtifact artifact,
            bool requireLoopingClip = true)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (requireLoopingClip && !clip.isLooping)
                throw new InvalidOperationException("Foot contact marker candidates require a looping AnimationClip.");
            if (!float.IsFinite(clip.length) || clip.length <= 0f)
                throw new InvalidOperationException("Foot contact marker candidates require a finite positive AnimationClip duration.");
            RequireArtifactMatchesClip(clip, artifact);

            int intervals = Mathf.Max(2, Mathf.CeilToInt(clip.length * artifact.Identity.SampleRate));
            int minimumSamples = Mathf.Max(
                1,
                Mathf.CeilToInt(artifact.Identity.MinimumLandingSegmentSeconds / (clip.length / intervals)));
            var candidates = new List<AnimationFootContactCandidate>();
            Collect(TimelineFootContactSide.Left, artifact.Features.Left.PlantConfidence, intervals, minimumSamples, candidates);
            Collect(TimelineFootContactSide.Right, artifact.Features.Right.PlantConfidence, intervals, minimumSamples, candidates);
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

        static void RequireArtifactMatchesClip(
            UnityEngine.AnimationClip clip,
            AnimationFootAnalysisArtifact artifact)
        {
            string clipPath = AssetDatabase.GetAssetPath(clip);
            string clipGuid = string.IsNullOrEmpty(clipPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(clipPath);
            string dependencyHash = string.IsNullOrEmpty(clipPath)
                ? string.Empty
                : AssetDatabase.GetAssetDependencyHash(clipPath).ToString();
            if (!string.Equals(artifact.Identity.ClipAssetGuid, clipGuid, StringComparison.Ordinal) ||
                !string.Equals(artifact.Identity.ClipDependencyHash, dependencyHash, StringComparison.Ordinal) ||
                !string.Equals(
                    artifact.Identity.AlgorithmVersion,
                    CharacterFootPlacementAnalysisSource.AlgorithmVersion,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Foot contact marker candidates require the exact current AnimationClip dependency and Foot Analysis algorithm.");
            }
        }

        static void Collect(
            TimelineFootContactSide side,
            AnimationCurve plantConfidence,
            int intervals,
            int minimumSamples,
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
                if (previous >= 0.5f || current < 0.5f)
                    continue;
                int plantedSamples = 0;
                while (plantedSamples < intervals)
                {
                    float confidence = plantConfidence.Evaluate(((i + plantedSamples) % intervals) / (float)intervals);
                    if (!float.IsFinite(confidence))
                        throw new InvalidOperationException($"{side} foot PlantConfidence contains a non-finite sample.");
                    if (confidence < 0.5f)
                        break;
                    plantedSamples++;
                }
                if (plantedSamples >= minimumSamples)
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
        public TimelineFootContactMarkerCandidate(AnimationFootContactCandidate source, int sequenceFrame)
        {
            if (sequenceFrame < 0)
                throw new ArgumentOutOfRangeException(nameof(sequenceFrame));
            Source = source;
            SequenceFrame = sequenceFrame;
        }

        public AnimationFootContactCandidate Source { get; }
        public string MarkerId => Source.MarkerId;
        public int SequenceFrame { get; }
    }

    sealed class TimelineFootContactMarkerProposal
    {
        public const string LeftMarkerId = "LeftFootContact";
        public const string RightMarkerId = "RightFootContact";

        TimelineFootContactMarkerProposal(
            string revision,
            string sequenceAuthoringId,
            AnimationFootContactCandidateSet source,
            TimelineFootContactMarkerCandidate[] candidates)
        {
            Revision = revision;
            SequenceAuthoringId = sequenceAuthoringId;
            Source = source;
            Candidates = candidates;
        }

        public string Revision { get; }
        public string SequenceAuthoringId { get; }
        public AnimationFootContactCandidateSet Source { get; }
        public IReadOnlyList<TimelineFootContactMarkerCandidate> Candidates { get; }

        public static TimelineFootContactMarkerProposal Build(
            CharacterAnimationSequenceAsset sequence,
            AnimationFootAnalysisArtifact artifact)
        {
            if (!sequence || !sequence.Clip)
                throw new ArgumentNullException(nameof(sequence));
            if (!AuthoringIdentity.IsValid(sequence.AuthoringId))
                throw new InvalidOperationException("Foot contact marker proposal requires a stable Sequence identity.");
            if (!sequence.Loop || sequence.SyncMode != AnimationSyncMode.MarkerGroup ||
                sequence.SequenceTopology != AnimationMarkerSequenceTopology.Cyclic)
                throw new InvalidOperationException("Foot contact marker proposal requires a looping MarkerGroup/Cyclic Sequence.");

            AnimationFootContactCandidateSet source = AnimationFootContactCandidateSet.Build(sequence.Clip, artifact);
            int sourceCycleFrames = sequence.DurationFrame;
            var mapped = new List<TimelineFootContactMarkerCandidate>();
            var occupiedFrames = new HashSet<int>();
            for (int i = 0; i < source.Candidates.Count; i++)
            {
                AnimationFootContactCandidate candidate = source.Candidates[i];
                int frame = Mathf.Clamp(
                    Mathf.RoundToInt(candidate.SourceNormalizedTime * sourceCycleFrames),
                    0,
                    sourceCycleFrames - 1);
                if (!occupiedFrames.Add(frame))
                    throw new InvalidOperationException($"Foot contact candidates collide at Sequence frame {frame}.");
                mapped.Add(new TimelineFootContactMarkerCandidate(candidate, frame));
            }
            mapped.Sort((left, right) =>
            {
                int frame = left.SequenceFrame.CompareTo(right.SequenceFrame);
                return frame != 0 ? frame : string.CompareOrdinal(left.MarkerId, right.MarkerId);
            });
            if (mapped.Count < 2)
                throw new InvalidOperationException("Foot contact marker proposal does not cover both feet on the Sequence.");

            for (int i = 0; i < sequence.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = sequence.SyncMarkers[i];
                if (marker != null && !IsFootMarker(marker.MarkerId) && occupiedFrames.Contains(marker.Frame))
                    throw new InvalidOperationException(
                        $"Foot contact candidate frame {marker.Frame} conflicts with material marker '{marker.MarkerId}'.");
            }

            var revisionParts = new List<string>
            {
                "animation-sequence-foot-contact-marker-proposal/v2",
                sequence.AuthoringId,
                sequence.ContentRevision,
                source.Revision,
                sourceCycleFrames.ToString(CultureInfo.InvariantCulture),
                sequence.SyncGroupId,
                sequence.TimeMapping.ToString(),
                sequence.SequenceTopology.ToString(),
                sequence.SyncRole.ToString()
            };
            for (int i = 0; i < mapped.Count; i++)
            {
                revisionParts.Add(mapped[i].MarkerId);
                revisionParts.Add(mapped[i].SequenceFrame.ToString(CultureInfo.InvariantCulture));
            }
            return new TimelineFootContactMarkerProposal(
                StableHash.Compute(revisionParts.ToArray()).Value,
                sequence.AuthoringId,
                source,
                mapped.ToArray());
        }

        public void Apply(CharacterAnimationSequenceAsset sequence)
        {
            if (!sequence || !string.Equals(sequence.AuthoringId, SequenceAuthoringId, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot contact marker proposal target Sequence changed.");
            sequence.ApplyModify(() => ApplyMarkers(sequence), "Apply Foot Contact Markers");
        }

        void ApplyMarkers(CharacterAnimationSequenceAsset sequence)
        {
            var reusable = new Dictionary<string, List<AnimationSyncMarker>>(StringComparer.Ordinal)
            {
                [LeftMarkerId] = new List<AnimationSyncMarker>(),
                [RightMarkerId] = new List<AnimationSyncMarker>()
            };
            var existingFootMarkers = new List<AnimationSyncMarker>();
            for (int i = 0; i < sequence.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = sequence.SyncMarkers[i];
                if (marker == null || !IsFootMarker(marker.MarkerId))
                    continue;
                reusable[marker.MarkerId].Add(marker);
                existingFootMarkers.Add(marker);
            }

            var reusedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Candidates.Count; i++)
            {
                TimelineFootContactMarkerCandidate candidate = Candidates[i];
                AnimationSyncMarker marker = FindReusableMarker(
                    reusable[candidate.MarkerId],
                    candidate.SequenceFrame,
                    reusedIds);
                string identity = marker?.AuthoringId ?? AuthoringIdentity.Create();
                sequence.EnsureMarker(identity, candidate.MarkerId, candidate.SequenceFrame);
                reusedIds.Add(identity);
            }
            for (int i = 0; i < existingFootMarkers.Count; i++)
            {
                AnimationSyncMarker marker = existingFootMarkers[i];
                if (!reusedIds.Contains(marker.AuthoringId))
                    sequence.DeleteMarker(marker.AuthoringId);
            }
        }

        static AnimationSyncMarker FindReusableMarker(
            IReadOnlyList<AnimationSyncMarker> markers,
            int targetFrame,
            ISet<string> reusedIds)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                AnimationSyncMarker marker = markers[i];
                if (marker.Frame == targetFrame && !reusedIds.Contains(marker.AuthoringId))
                    return marker;
            }
            for (int i = 0; i < markers.Count; i++)
            {
                AnimationSyncMarker marker = markers[i];
                if (!reusedIds.Contains(marker.AuthoringId))
                    return marker;
            }
            return null;
        }

        static bool IsFootMarker(string markerId) =>
            string.Equals(markerId, LeftMarkerId, StringComparison.Ordinal) ||
            string.Equals(markerId, RightMarkerId, StringComparison.Ordinal);
    }
}
