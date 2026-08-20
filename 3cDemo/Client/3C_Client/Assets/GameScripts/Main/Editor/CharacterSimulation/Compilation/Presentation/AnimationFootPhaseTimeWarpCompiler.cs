using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    static class AnimationFootPhaseTimeWarpCompiler
    {
        const int LeaderIntervalCount = 32;
        const int FollowerIntervalCount = 64;
        const int MaximumFollowerStep = 4;

        readonly struct FootSample
        {
            public FootSample(
                Vector2 position,
                float height,
                Vector3 velocity,
                float plant)
            {
                Position = position;
                Height = height;
                Velocity = velocity;
                Plant = plant;
            }

            public Vector2 Position { get; }
            public float Height { get; }
            public Vector3 Velocity { get; }
            public float Plant { get; }
        }

        sealed class SegmentFeatures
        {
            public FootSample[] Left;
            public FootSample[] Right;
        }

        internal static AnimationFootPhaseTimeWarpPlan Compile(
            string relationIdentity,
            string leaderSourceIdentity,
            AnimationMarkerSyncBinding leaderBinding,
            AnimationFootAnalysisArtifact leaderArtifact,
            string followerSourceIdentity,
            AnimationMarkerSyncBinding followerBinding,
            AnimationFootAnalysisArtifact followerArtifact)
        {
            if (string.IsNullOrWhiteSpace(relationIdentity) ||
                string.IsNullOrWhiteSpace(leaderSourceIdentity) ||
                string.IsNullOrWhiteSpace(followerSourceIdentity) ||
                leaderBinding == null || followerBinding == null ||
                leaderArtifact == null || followerArtifact == null ||
                leaderBinding.TimeMapping != AnimationSyncTimeMapping.GeneratedFootPhase ||
                followerBinding.TimeMapping != AnimationSyncTimeMapping.GeneratedFootPhase)
                throw new ArgumentException("Foot phase warp compile input is invalid.");

            var segments = new List<AnimationFootPhaseWarpSegmentPlan>();
            for (int leaderIndex = 0; leaderIndex < leaderBinding.Segments.Count; leaderIndex++)
            {
                AnimationMarkerSyncSegmentOccurrence leader = leaderBinding.Segments[leaderIndex];
                if (!followerBinding.TryGetOccurrences(
                        leader.PreviousMarkerId,
                        leader.NextMarkerId,
                        out AnimationMarkerSyncSegmentOccurrence[] followers) ||
                    followers.Length == 0)
                    throw new InvalidOperationException(
                        $"Foot phase follower misses '{leader.PreviousMarkerId}->{leader.NextMarkerId}'.");
                for (int followerIndex = 0; followerIndex < followers.Length; followerIndex++)
                {
                    AnimationMarkerSyncSegmentOccurrence follower = followers[followerIndex];
                    segments.Add(new AnimationFootPhaseWarpSegmentPlan(
                        leader.OccurrenceIndex,
                        follower.OccurrenceIndex,
                        leader.PreviousMarkerId,
                        leader.NextMarkerId,
                        CompileKnots(
                            leaderArtifact.PhaseValidation,
                            leaderBinding.DurationSeconds,
                            leader,
                            followerArtifact.PhaseValidation,
                            followerBinding.DurationSeconds,
                            follower)));
                }
            }
            string planIdentity = StableHash.Compute(
                AnimationFootPhaseTimeWarpPlan.AlgorithmIdentity,
                relationIdentity,
                leaderSourceIdentity,
                followerSourceIdentity,
                leaderArtifact.ContentHash.Value,
                followerArtifact.ContentHash.Value).Value;
            return new AnimationFootPhaseTimeWarpPlan(
                planIdentity,
                leaderArtifact.ContentHash.Value,
                followerArtifact.ContentHash.Value,
                leaderSourceIdentity,
                followerSourceIdentity,
                segments.ToArray());
        }

        static AnimationFootPhaseWarpKnot[] CompileKnots(
            AnimationFootPhaseValidationDescriptor leaderDescriptor,
            float leaderDuration,
            AnimationMarkerSyncSegmentOccurrence leaderSegment,
            AnimationFootPhaseValidationDescriptor followerDescriptor,
            float followerDuration,
            AnimationMarkerSyncSegmentOccurrence followerSegment)
        {
            SegmentFeatures leader = BuildFeatures(
                leaderDescriptor,
                leaderDuration,
                leaderSegment,
                LeaderIntervalCount);
            SegmentFeatures follower = BuildFeatures(
                followerDescriptor,
                followerDuration,
                followerSegment,
                FollowerIntervalCount);
            var costs = new double[LeaderIntervalCount + 1, FollowerIntervalCount + 1];
            var previous = new int[LeaderIntervalCount + 1, FollowerIntervalCount + 1];
            for (int i = 0; i <= LeaderIntervalCount; i++)
                for (int j = 0; j <= FollowerIntervalCount; j++)
                {
                    costs[i, j] = double.PositiveInfinity;
                    previous[i, j] = -1;
                }
            costs[0, 0] = FeatureCost(leader, 0, follower, 0);
            for (int i = 1; i <= LeaderIntervalCount; i++)
            {
                int minimum = i;
                int maximum = Math.Min(FollowerIntervalCount, i * MaximumFollowerStep);
                for (int j = minimum; j <= maximum; j++)
                {
                    double local = FeatureCost(leader, i, follower, j);
                    for (int step = 1; step <= MaximumFollowerStep; step++)
                    {
                        int predecessor = j - step;
                        if (predecessor < 0 || double.IsPositiveInfinity(costs[i - 1, predecessor]))
                            continue;
                        double slopePenalty = Math.Abs(step - 2) * 0.04d;
                        double candidate = costs[i - 1, predecessor] + local + slopePenalty;
                        if (candidate < costs[i, j] - 0.000000001d ||
                            Math.Abs(candidate - costs[i, j]) <= 0.000000001d &&
                            (previous[i, j] < 0 || predecessor < previous[i, j]))
                        {
                            costs[i, j] = candidate;
                            previous[i, j] = predecessor;
                        }
                    }
                }
            }
            if (double.IsPositiveInfinity(costs[LeaderIntervalCount, FollowerIntervalCount]))
                throw new InvalidOperationException("Foot phase alignment could not cover both segment endpoints.");
            var followerIndices = new int[LeaderIntervalCount + 1];
            followerIndices[LeaderIntervalCount] = FollowerIntervalCount;
            for (int i = LeaderIntervalCount; i > 0; i--)
            {
                int value = previous[i, followerIndices[i]];
                if (value < 0)
                    throw new InvalidOperationException("Foot phase alignment path is incomplete.");
                followerIndices[i - 1] = value;
            }
            var knots = new AnimationFootPhaseWarpKnot[LeaderIntervalCount + 1];
            for (int i = 0; i <= LeaderIntervalCount; i++)
            {
                knots[i] = new AnimationFootPhaseWarpKnot(
                    i / (float)LeaderIntervalCount,
                    followerIndices[i] / (float)FollowerIntervalCount);
            }
            return knots;
        }

        static SegmentFeatures BuildFeatures(
            AnimationFootPhaseValidationDescriptor descriptor,
            float duration,
            AnimationMarkerSyncSegmentOccurrence segment,
            int intervals)
        {
            descriptor.RequireValid();
            if (!float.IsFinite(duration) || duration <= 0f ||
                segment == null || segment.DurationSeconds <= 0f)
                throw new InvalidOperationException("Foot phase segment timing is invalid.");
            var result = new SegmentFeatures
            {
                Left = new FootSample[intervals + 1],
                Right = new FootSample[intervals + 1]
            };
            for (int i = 0; i <= intervals; i++)
            {
                float fraction = i / (float)intervals;
                float time = segment.StartTimeSeconds + segment.DurationSeconds * fraction;
                float normalized = Mathf.Repeat(time / duration, 1f);
                if (i == intervals && !segment.Wraps)
                    normalized = Mathf.Clamp01(time / duration);
                result.Left[i] = Sample(descriptor.Left, normalized);
                result.Right[i] = Sample(descriptor.Right, normalized);
            }
            Normalize(result.Left, result.Right);
            return result;
        }

        static FootSample Sample(
            AnimationFootPhaseValidationFootDescriptor descriptor,
            float normalizedTime)
        {
            IReadOnlyList<AnimationFootPhaseValidationSample> samples = descriptor.Samples;
            for (int i = 1; i < samples.Count; i++)
            {
                AnimationFootPhaseValidationSample next = samples[i];
                if (normalizedTime > next.NormalizedTime)
                    continue;
                AnimationFootPhaseValidationSample previous = samples[i - 1];
                float width = next.NormalizedTime - previous.NormalizedTime;
                float fraction = width <= 0f
                    ? 0f
                    : (normalizedTime - previous.NormalizedTime) / width;
                return new FootSample(
                    Vector2.Lerp(
                        previous.RootLocalSolePlanarPosition,
                        next.RootLocalSolePlanarPosition,
                        fraction),
                    Mathf.Lerp(
                        previous.CalibratedSoleHeight,
                        next.CalibratedSoleHeight,
                        fraction),
                    Vector3.Lerp(
                        previous.SoleLocalVelocity,
                        next.SoleLocalVelocity,
                        fraction),
                    Mathf.Lerp(
                        previous.PlantConfidence,
                        next.PlantConfidence,
                        fraction));
            }
            AnimationFootPhaseValidationSample last = samples[samples.Count - 1];
            return new FootSample(
                last.RootLocalSolePlanarPosition,
                last.CalibratedSoleHeight,
                last.SoleLocalVelocity,
                last.PlantConfidence);
        }

        static void Normalize(FootSample[] left, FootSample[] right)
        {
            Vector2 leftOrigin = left[0].Position;
            Vector2 rightOrigin = right[0].Position;
            float leftHeight = left[0].Height;
            float rightHeight = right[0].Height;
            float planarScale = 0f;
            float heightScale = 0f;
            float velocityScale = 0f;
            for (int i = 0; i < left.Length; i++)
            {
                planarScale = Mathf.Max(
                    planarScale,
                    (left[i].Position - leftOrigin).magnitude,
                    (right[i].Position - rightOrigin).magnitude);
                heightScale = Mathf.Max(
                    heightScale,
                    Mathf.Abs(left[i].Height - leftHeight),
                    Mathf.Abs(right[i].Height - rightHeight));
                velocityScale = Mathf.Max(
                    velocityScale,
                    left[i].Velocity.magnitude,
                    right[i].Velocity.magnitude);
            }
            planarScale = Mathf.Max(planarScale, 0.00001f);
            heightScale = Mathf.Max(heightScale, 0.00001f);
            velocityScale = Mathf.Max(velocityScale, 0.00001f);
            for (int i = 0; i < left.Length; i++)
            {
                left[i] = Normalize(left[i], leftOrigin, leftHeight, planarScale, heightScale, velocityScale);
                right[i] = Normalize(right[i], rightOrigin, rightHeight, planarScale, heightScale, velocityScale);
            }
        }

        static FootSample Normalize(
            FootSample sample,
            Vector2 origin,
            float heightOrigin,
            float planarScale,
            float heightScale,
            float velocityScale) =>
            new FootSample(
                (sample.Position - origin) / planarScale,
                (sample.Height - heightOrigin) / heightScale,
                sample.Velocity / velocityScale,
                sample.Plant);

        static double FeatureCost(
            SegmentFeatures leader,
            int leaderIndex,
            SegmentFeatures follower,
            int followerIndex)
        {
            return FootCost(leader.Left[leaderIndex], follower.Left[followerIndex]) +
                   FootCost(leader.Right[leaderIndex], follower.Right[followerIndex]) +
                   Math.Abs(
                       leaderIndex / (double)LeaderIntervalCount -
                       followerIndex / (double)FollowerIntervalCount) * 0.1d;
        }

        static double FootCost(FootSample leader, FootSample follower)
        {
            double plant = Math.Abs(leader.Plant - follower.Plant) * 4d;
            double planar = (leader.Position - follower.Position).sqrMagnitude;
            double height = Math.Abs(leader.Height - follower.Height);
            float leaderSpeed = leader.Velocity.magnitude;
            float followerSpeed = follower.Velocity.magnitude;
            double direction = leaderSpeed <= 0.00001f || followerSpeed <= 0.00001f
                ? 0d
                : (1f - Mathf.Clamp(Vector3.Dot(
                    leader.Velocity / leaderSpeed,
                    follower.Velocity / followerSpeed), -1f, 1f)) * 0.75d;
            double speed = Math.Abs(leaderSpeed - followerSpeed) * 0.25d;
            return plant + planar + height + direction + speed;
        }
    }
}
