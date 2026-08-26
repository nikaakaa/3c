using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterFootStepEventProjectionCompiler
    {
        const float ValueTolerance = 0.0001f;

        readonly struct StepFrame
        {
            internal StepFrame(
                bool available,
                int ordinal,
                int cycleOffset,
                float timeToLanding,
                float distance,
                Vector3 rootLocalLanding)
            {
                Available = available;
                Ordinal = ordinal;
                CycleOffset = cycleOffset;
                TimeToLanding = timeToLanding;
                Distance = distance;
                RootLocalLanding = rootLocalLanding;
            }

            internal bool Available { get; }
            internal int Ordinal { get; }
            internal int CycleOffset { get; }
            internal float TimeToLanding { get; }
            internal float Distance { get; }
            internal Vector3 RootLocalLanding { get; }
            internal long Token => Available
                ? ((long)CycleOffset << 32) | (uint)Ordinal
                : 0;
        }

        internal static AnimationFootFeaturePair Build(
            AnimationClip clip,
            AnimationFootAnalysisArtifact artifact)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            AnimationFootMotionDataDescriptor motion = artifact.MotionData;
            float duration = motion.Raw.DurationSeconds;
            bool loop = clip.isLooping;
            return new AnimationFootFeaturePair(
                BuildFoot(
                    clip,
                    artifact.Features.Left,
                    motion.Left,
                    duration,
                    motion.Raw.SampleRate,
                    loop,
                    true),
                BuildFoot(
                    clip,
                    artifact.Features.Right,
                    motion.Right,
                    duration,
                    motion.Raw.SampleRate,
                    loop,
                    false));
        }

        static AnimationFootFeatureCurveSet BuildFoot(
            AnimationClip clip,
            AnimationFootFeatureCurveSet legacy,
            AnimationFootMotionFootPage motion,
            float duration,
            float sampleRate,
            bool loop,
            bool left)
        {
            legacy.RequireValid();
            motion.RequireValid();
            string timeChannel = left
                ? CharacterAnimationClipRegisteredCurveChannels.LeftStepTime
                : CharacterAnimationClipRegisteredCurveChannels.RightStepTime;
            string distanceChannel = left
                ? CharacterAnimationClipRegisteredCurveChannels.LeftStepDistance
                : CharacterAnimationClipRegisteredCurveChannels.RightStepDistance;
            string contactChannel = left
                ? CharacterAnimationClipRegisteredCurveChannels.LeftContact
                : CharacterAnimationClipRegisteredCurveChannels.RightContact;
            AnimationCurve formalTime = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                clip,
                timeChannel);
            AnimationCurve formalDistance = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                clip,
                distanceChannel);
            AnimationCurve formalContact = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                clip,
                contactChannel);
            StepFrame[] current = BuildCurrentFrames(
                motion,
                formalTime,
                formalDistance,
                formalContact,
                duration,
                loop,
                left ? "Left" : "Right",
                clip.name);
            StepFrame[] incoming = BuildIncomingFrames(
                motion,
                current,
                sampleRate,
                loop);
            return new AnimationFootFeatureCurveSet(
                legacy.SoleLocalVelocityX,
                legacy.SoleLocalVelocityY,
                legacy.SoleLocalVelocityZ,
                legacy.SoleHeight,
                legacy.PlantConfidence,
                BuildStepCurves(legacy.PredictedStep, current),
                BuildStepCurves(legacy.IncomingPredictedStep, incoming),
                NormalizeSeconds(formalContact, duration));
        }

        static StepFrame[] BuildCurrentFrames(
            AnimationFootMotionFootPage page,
            AnimationCurve formalTime,
            AnimationCurve formalDistance,
            AnimationCurve formalContact,
            float duration,
            bool loop,
            string side,
            string clipName)
        {
            IReadOnlyList<AnimationFootMotionDerivedSample> samples = page.Samples;
            int intervals = samples.Count - 1;
            AnimationFootMotionEvent[] landings = page.Events
                .Where(value => value.Kind == AnimationFootMotionEventKind.Landing)
                .OrderBy(value => value.SampleIndex)
                .ToArray();
            var byOrdinal = landings.ToDictionary(value => value.Ordinal);
            var result = new StepFrame[samples.Count];
            for (int i = 0; i < result.Length; i++)
            {
                AnimationFootMotionStepEvidence step = samples[i].Step;
                float time = formalTime.Evaluate(samples[i].TimeSeconds);
                float distance = formalDistance.Evaluate(samples[i].TimeSeconds);
                float contact = formalContact.Evaluate(samples[i].TimeSeconds);
                RequireMatch(clipName, side, i, "Step Time", step.TimeSeconds, time);
                RequireMatch(clipName, side, i, "Step Distance", step.Distance, distance);
                RequireMatch(clipName, side, i, "Contact", samples[i].Filter.Contact, contact);
                if (!step.Available || !byOrdinal.TryGetValue(step.LandingOrdinal, out AnimationFootMotionEvent landing))
                {
                    result[i] = default;
                    continue;
                }
                if (!loop && i > landing.SampleIndex && time <= ValueTolerance)
                {
                    result[i] = default;
                    continue;
                }
                int target = ResolveLandingAbsolute(landing.SampleIndex, i, intervals, loop);
                result[i] = new StepFrame(
                    true,
                    landing.Ordinal,
                    loop ? Mathf.Max(0, target / intervals) : 0,
                    time,
                    distance,
                    landing.RootLocalSolePosition);
            }
            return result;
        }

        static StepFrame[] BuildIncomingFrames(
            AnimationFootMotionFootPage page,
            IReadOnlyList<StepFrame> current,
            float sampleRate,
            bool loop)
        {
            int intervals = page.Samples.Count - 1;
            float sampleStep = 1f / sampleRate;
            AnimationFootMotionEvent[] landings = page.Events
                .Where(value => value.Kind == AnimationFootMotionEventKind.Landing)
                .OrderBy(value => value.SampleIndex)
                .ToArray();
            var result = new StepFrame[current.Count];
            for (int i = 0; i < result.Length; i++)
            {
                StepFrame owned = current[i];
                if (!owned.Available)
                    continue;
                int ownedTarget = ResolveLandingAbsolute(
                    landings.First(value => value.Ordinal == owned.Ordinal).SampleIndex,
                    i,
                    intervals,
                    loop);
                if (!TryResolveNextLanding(
                        ownedTarget,
                        intervals,
                        landings,
                        loop,
                        out int nextTarget,
                        out AnimationFootMotionEvent nextLanding))
                    continue;
                int evidenceIndex = loop
                    ? Mod(ownedTarget + 1, intervals)
                    : Mathf.Min(intervals, ownedTarget + 1);
                AnimationFootMotionStepEvidence evidence = page.Samples[evidenceIndex].Step;
                float distance = evidence.Available && evidence.LandingOrdinal == nextLanding.Ordinal
                    ? evidence.Distance
                    : 0f;
                result[i] = new StepFrame(
                    true,
                    nextLanding.Ordinal,
                    loop ? Mathf.Max(0, nextTarget / intervals) : 0,
                    (nextTarget - i) * sampleStep,
                    distance,
                    nextLanding.RootLocalSolePosition);
            }
            return result;
        }

        static AnimationPredictedFootStepCurveSet BuildStepCurves(
            AnimationPredictedFootStepCurveSet legacy,
            IReadOnlyList<StepFrame> frames)
        {
            int count = frames.Count;
            float[] confidence = new float[count];
            float[] time = new float[count];
            float[] distance = new float[count];
            float[] ordinal = new float[count];
            float[] cycle = new float[count];
            float[] rootX = new float[count];
            float[] rootY = new float[count];
            float[] rootZ = new float[count];
            float[] constraint = new float[count];
            float[] support = new float[count];
            long[] tokens = new long[count];
            for (int i = 0; i < count; i++)
            {
                StepFrame frame = frames[i];
                float normalized = count > 1 ? i / (count - 1f) : 0f;
                AnimationPredictedFootStepSample legacySample = legacy.Sample(normalized);
                confidence[i] = frame.Available ? 1f : 0f;
                time[i] = frame.TimeToLanding;
                distance[i] = frame.Distance;
                ordinal[i] = frame.Ordinal;
                cycle[i] = frame.CycleOffset;
                rootX[i] = frame.RootLocalLanding.x;
                rootY[i] = frame.RootLocalLanding.y;
                rootZ[i] = frame.RootLocalLanding.z;
                constraint[i] = legacySample.CurrentConstraintWeight;
                support[i] = legacySample.CurrentSupportWeight;
                tokens[i] = frame.Token;
            }
            AnimationCurve zero = Constant(count, 0f);
            AnimationCurve[] routeX = CopyRoute(legacy, value => value.GetRootLocalFootRouteX);
            AnimationCurve[] routeY = CopyRoute(legacy, value => value.GetRootLocalFootRouteY);
            AnimationCurve[] routeZ = CopyRoute(legacy, value => value.GetRootLocalFootRouteZ);
            return new AnimationPredictedFootStepCurveSet(
                Curve(confidence, tokens, true),
                Curve(time, tokens, false),
                legacy.EventPhase,
                legacy.ReleasePhase,
                legacy.LiftOffPhase,
                legacy.ApproachContactPhase,
                legacy.ActionStepDurationSeconds,
                Curve(ordinal, tokens, true),
                Curve(cycle, tokens, true),
                zero,
                zero,
                zero,
                zero,
                zero,
                zero,
                routeX,
                routeY,
                routeZ,
                CopyRoute(legacy, value => value.GetRootLocalAnkleRouteX),
                CopyRoute(legacy, value => value.GetRootLocalAnkleRouteY),
                CopyRoute(legacy, value => value.GetRootLocalAnkleRouteZ),
                CopyRoute(legacy, value => value.GetRootLocalHipRouteX),
                CopyRoute(legacy, value => value.GetRootLocalHipRouteY),
                CopyRoute(legacy, value => value.GetRootLocalHipRouteZ),
                CopyRoute(legacy, value => value.GetAuthoredFootPlanarRouteX),
                CopyRoute(legacy, value => value.GetAuthoredFootPlanarRouteZ),
                CopyRoute(legacy, value => value.GetAnimationClearanceHeight),
                legacy.BiomechanicalStep,
                Curve(distance, tokens, true),
                Curve(rootX, tokens, true),
                Curve(rootY, tokens, true),
                Curve(rootZ, tokens, true),
                Curve(constraint, tokens, false),
                Curve(support, tokens, false));
        }

        static AnimationCurve[] CopyRoute(
            AnimationPredictedFootStepCurveSet source,
            Func<AnimationPredictedFootStepCurveSet, Func<int, AnimationCurve>> selector)
        {
            Func<int, AnimationCurve> get = selector(source);
            var result = new AnimationCurve[AnimationPredictedFootStepCurveSet.RouteSampleCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = AnimationPredictedFootStepCurveSet.Copy(get(i));
            return result;
        }

        static AnimationCurve Curve(
            IReadOnlyList<float> values,
            IReadOnlyList<long> tokens,
            bool discrete)
        {
            int count = values.Count;
            var keys = new Keyframe[count];
            for (int i = 0; i < count; i++)
            {
                float time = count > 1 ? i / (count - 1f) : 0f;
                float inTangent = float.PositiveInfinity;
                float outTangent = float.PositiveInfinity;
                if (!discrete && i > 0 && tokens[i] == tokens[i - 1])
                    inTangent = (values[i] - values[i - 1]) * (count - 1);
                if (!discrete && i + 1 < count && tokens[i] == tokens[i + 1])
                    outTangent = (values[i + 1] - values[i]) * (count - 1);
                keys[i] = new Keyframe(time, values[i], inTangent, outTangent);
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
        }

        static AnimationCurve Constant(int count, float value)
        {
            var values = Enumerable.Repeat(value, count).ToArray();
            var tokens = new long[count];
            return Curve(values, tokens, true);
        }

        static int ResolveLandingAbsolute(
            int landingSample,
            int currentSample,
            int intervals,
            bool loop)
        {
            int result = landingSample;
            if (loop)
            {
                while (result < currentSample)
                    result += intervals;
            }
            return result;
        }

        static bool TryResolveNextLanding(
            int currentTarget,
            int intervals,
            IReadOnlyList<AnimationFootMotionEvent> landings,
            bool loop,
            out int nextTarget,
            out AnimationFootMotionEvent nextLanding)
        {
            nextTarget = int.MaxValue;
            nextLanding = default;
            for (int cycle = 0; cycle <= (loop ? 2 : 0); cycle++)
            {
                for (int i = 0; i < landings.Count; i++)
                {
                    int candidate = landings[i].SampleIndex + cycle * intervals;
                    if (candidate <= currentTarget || candidate >= nextTarget)
                        continue;
                    nextTarget = candidate;
                    nextLanding = landings[i];
                }
            }
            return nextTarget != int.MaxValue;
        }

        static int Mod(int value, int modulus) =>
            ((value % modulus) + modulus) % modulus;

        static AnimationCurve NormalizeSeconds(AnimationCurve source, float duration)
        {
            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.time /= duration;
                key.inTangent *= duration;
                key.outTangent *= duration;
                keys[i] = key;
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        static void RequireMatch(
            string clip,
            string side,
            int sample,
            string field,
            float expected,
            float actual)
        {
            if (!float.IsFinite(actual) || Mathf.Abs(expected - actual) > ValueTolerance)
                throw new InvalidOperationException(
                    $"Foot Step Event '{clip}' {side} sample {sample} {field} mismatch: MotionData={expected:R}; Curve={actual:R}.");
        }
    }
}
