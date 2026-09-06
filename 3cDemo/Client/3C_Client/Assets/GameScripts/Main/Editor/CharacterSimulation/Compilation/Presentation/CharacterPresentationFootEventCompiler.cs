using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPresentationFootEventCompiler
    {
        internal static AnimationCurve NormalizeRegisteredCurve(AnimationCurve source, float sourceDurationSeconds)
        {
            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.time /= sourceDurationSeconds;
                key.inTangent *= sourceDurationSeconds;
                key.outTangent *= sourceDurationSeconds;
                keys[i] = key;
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        internal static AnimationFootStepObservationCurvePair CompileFootStepObservation(
            UnityEngine.AnimationClip clip,
            float sourceDurationSeconds,
            AnimationFootMotionDataDescriptor motionData) =>
            new AnimationFootStepObservationCurvePair(
                new AnimationFootStepObservationCurveSet(
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftFootHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftToeHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftToeSpeed),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftPositionError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftRotationError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftContact),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftLockMode),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftLockWeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftSupport),
                        sourceDurationSeconds),
                    CompileLandingEvents(
                        motionData,
                        motionData?.Left,
                        motionData?.Raw.Left,
                        clip.isLooping,
                        sourceDurationSeconds,
                        $"{clip.name}/Left")),
                new AnimationFootStepObservationCurveSet(
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightFootHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightToeHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightToeSpeed),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightPositionError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightRotationError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightContact),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightLockMode),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightLockWeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightSupport),
                        sourceDurationSeconds),
                    CompileLandingEvents(
                        motionData,
                        motionData?.Right,
                        motionData?.Raw.Right,
                        clip.isLooping,
                        sourceDurationSeconds,
                        $"{clip.name}/Right")));

        static AnimationFootStepLandingEventTable CompileLandingEvents(
            AnimationFootMotionDataDescriptor motionData,
            AnimationFootMotionFootPage foot,
            AnimationFootMotionRawFootPage rawFoot,
            bool looping,
            float sourceDurationSeconds,
            string sourceLabel)
        {
            if (motionData == null || foot == null || rawFoot == null ||
                !float.IsFinite(sourceDurationSeconds) ||
                sourceDurationSeconds <= 0f ||
                rawFoot.Samples.Count != foot.Samples.Count ||
                Mathf.Abs(
                    motionData.Raw.DurationSeconds -
                    sourceDurationSeconds) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Foot Step Landing Event source timing is invalid.");
            }
            var result = new List<AnimationFootStepLandingEvent>();
            int landingOrdinal = 0;
            for (int i = 0; i < foot.Events.Count; i++)
            {
                AnimationFootMotionEvent footEvent = foot.Events[i];
                if (footEvent.Kind != AnimationFootMotionEventKind.Landing)
                    continue;
                if ((uint)footEvent.SampleIndex >=
                    (uint)motionData.Raw.RootSamples.Count ||
                    (uint)footEvent.SampleIndex >=
                    (uint)foot.Samples.Count)
                {
                    throw new InvalidOperationException(
                        "Foot Step Landing Event sample is outside its artifact.");
                }
                float normalizedTime =
                    motionData.Raw.RootSamples[footEvent.SampleIndex].TimeSeconds /
                    sourceDurationSeconds;
                AnimationFootMotionStepEvidence step =
                    foot.Samples[footEvent.SampleIndex].Step;
                if (!step.Available ||
                    step.LandingOrdinal != footEvent.Ordinal ||
                    footEvent.Ordinal != ++landingOrdinal)
                {
                    throw new InvalidOperationException(
                        "Foot Step Landing Event has no matching Step evidence.");
                }
                RequireLandingStepDistanceConsistency(
                    motionData.Raw,
                    rawFoot,
                    foot,
                    i,
                    in footEvent,
                    in step,
                    looping,
                    sourceLabel);
                bool hasSwingBoundaries = ResolveLandingEventPhaseLeads(
                    motionData,
                    foot,
                    in footEvent,
                    looping,
                    sourceDurationSeconds,
                    sourceLabel,
                    out float preSwingLeadSeconds,
                    out float swingLeadSeconds,
                    out float approachContactLeadSeconds);
                result.Add(new AnimationFootStepLandingEvent(
                    normalizedTime,
                    footEvent.Ordinal,
                    footEvent.CycleOffset,
                    step.Distance,
                    footEvent.RootLocalSolePosition,
                    hasSwingBoundaries,
                    preSwingLeadSeconds,
                    swingLeadSeconds,
                    approachContactLeadSeconds));
            }
            return new AnimationFootStepLandingEventTable(result.ToArray());
        }

        static void RequireLandingStepDistanceConsistency(
            AnimationFootMotionRawPage raw,
            AnimationFootMotionRawFootPage rawFoot,
            AnimationFootMotionFootPage foot,
            int eventIndex,
            in AnimationFootMotionEvent landing,
            in AnimationFootMotionStepEvidence step,
            bool looping,
            string sourceLabel)
        {
            const float geometryToleranceMeters = 0.0001f;
            const float timeToleranceSeconds = 0.0001f;
            Vector3 currentPosition =
                rawFoot.Samples[landing.SampleIndex].Sole.MotionPosition;
            if (Vector3.Distance(currentPosition, landing.MotionSolePosition) >
                    geometryToleranceMeters ||
                step.TimeSeconds > timeToleranceSeconds)
            {
                throw new InvalidOperationException(
                    $"Foot Motion '{sourceLabel}' Landing #{landing.Ordinal} " +
                    "does not match its canonical motion sample or time boundary.");
            }
            int previousEventIndex = -1;
            for (int i = eventIndex - 1; i >= 0; i--)
            {
                if (foot.Events[i].Kind != AnimationFootMotionEventKind.Landing)
                    continue;
                previousEventIndex = i;
                break;
            }
            bool previousCycle = previousEventIndex < 0 && looping;
            if (previousCycle)
            {
                for (int i = foot.Events.Count - 1; i >= 0; i--)
                {
                    if (foot.Events[i].Kind != AnimationFootMotionEventKind.Landing)
                        continue;
                    previousEventIndex = i;
                    break;
                }
            }
            Vector3 previousPosition = previousEventIndex >= 0
                ? foot.Events[previousEventIndex].MotionSolePosition
                : rawFoot.Samples[0].Sole.MotionPosition;
            if (previousCycle)
            {
                AnimationFootMotionRootSample first = raw.RootSamples[0];
                AnimationFootMotionRootSample last =
                    raw.RootSamples[raw.RootSamples.Count - 1];
                Quaternion cycleRotation =
                    (last.Rotation * Quaternion.Inverse(first.Rotation)).normalized;
                Vector3 cycleTranslation =
                    last.Position - cycleRotation * first.Position;
                previousPosition = Quaternion.Inverse(cycleRotation) *
                                   (previousPosition - cycleTranslation);
            }
            float expectedDistance = Vector3.ProjectOnPlane(
                currentPosition - previousPosition,
                Vector3.up).magnitude;
            if (!float.IsFinite(expectedDistance) ||
                Mathf.Abs(step.Distance - expectedDistance) >
                    geometryToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Foot Motion '{sourceLabel}' Landing #{landing.Ordinal} " +
                    $"Step Distance mismatch: expected={expectedDistance:R}, " +
                    $"actual={step.Distance:R}.");
            }
        }

        static bool ResolveLandingEventPhaseLeads(
            AnimationFootMotionDataDescriptor motionData,
            AnimationFootMotionFootPage foot,
            in AnimationFootMotionEvent footEvent,
            bool looping,
            float sourceDurationSeconds,
            string sourceLabel,
            out float preSwingLeadSeconds,
            out float swingLeadSeconds,
            out float approachContactLeadSeconds)
        {
            int sampleCount = motionData.Raw.RootSamples.Count;
            int activeSampleCount = looping ? sampleCount - 1 : sampleCount;
            int landingSample = footEvent.SampleIndex;
            if (activeSampleCount <= 0 ||
                landingSample < 0 ||
                landingSample >= activeSampleCount)
            {
                throw new InvalidOperationException(
                    $"Foot Step Landing Event #{footEvent.Ordinal} sample is invalid.");
            }
            int previousLandingSample = FindPreviousEventSample(
                foot,
                AnimationFootMotionEventKind.Landing,
                landingSample,
                activeSampleCount,
                looping);
            int liftOffSample = FindPreviousEventSample(
                foot,
                AnimationFootMotionEventKind.LiftOff,
                landingSample,
                activeSampleCount,
                looping);
            if (liftOffSample < 0)
            {
                if (!looping && landingSample == 0)
                {
                    preSwingLeadSeconds = 0f;
                    swingLeadSeconds = 0f;
                    approachContactLeadSeconds = 0f;
                    return false;
                }
                const float contactEpsilon = 0.0001f;
                if (!looping &&
                    foot.Samples[0].Filter.Contact <= contactEpsilon)
                {
                    liftOffSample = 0;
                }
                else if (foot.Samples[0].Filter.Contact > contactEpsilon)
                {
                    preSwingLeadSeconds = 0f;
                    swingLeadSeconds = 0f;
                    approachContactLeadSeconds = 0f;
                    return false;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Foot Step Landing Event {sourceLabel} #{footEvent.Ordinal} has no preceding LiftOff boundary. LandingSample={landingSample}, InitialContact={foot.Samples[0].Filter.Contact}.");
                }
            }
            preSwingLeadSeconds = previousLandingSample >= 0
                ? SecondsBetweenSamples(
                    motionData,
                    previousLandingSample,
                    landingSample,
                    looping,
                    sourceDurationSeconds)
                : motionData.Raw.RootSamples[landingSample].TimeSeconds;
            swingLeadSeconds = SecondsBetweenSamples(
                motionData,
                liftOffSample,
                landingSample,
                looping,
                sourceDurationSeconds);
            int approachContactSample = FindApproachContactSample(
                foot,
                liftOffSample,
                landingSample,
                activeSampleCount,
                looping);
            if (approachContactSample < 0)
            {
                throw new InvalidOperationException(
                    $"Foot Step Landing Event {sourceLabel} #{footEvent.Ordinal} has no Approach Contact boundary. LiftOffSample={liftOffSample}, LandingSample={landingSample}.");
            }
            approachContactLeadSeconds = SecondsBetweenSamples(
                motionData,
                approachContactSample,
                landingSample,
                looping,
                sourceDurationSeconds);
            if (!float.IsFinite(preSwingLeadSeconds) ||
                !float.IsFinite(swingLeadSeconds) ||
                !float.IsFinite(approachContactLeadSeconds) ||
                preSwingLeadSeconds < 0f ||
                swingLeadSeconds < 0f ||
                swingLeadSeconds > preSwingLeadSeconds ||
                approachContactLeadSeconds < 0f ||
                approachContactLeadSeconds > swingLeadSeconds)
            {
                throw new InvalidOperationException(
                    $"Foot Step Landing Event #{footEvent.Ordinal} phase boundaries are invalid.");
            }
            return true;
        }

        static int FindPreviousEventSample(
            AnimationFootMotionFootPage foot,
            AnimationFootMotionEventKind kind,
            int targetSample,
            int activeSampleCount,
            bool looping)
        {
            int selectedSample = -1;
            int selectedDistance = int.MaxValue;
            for (int i = 0; i < foot.Events.Count; i++)
            {
                AnimationFootMotionEvent candidate = foot.Events[i];
                if (candidate.Kind != kind ||
                    candidate.SampleIndex < 0 ||
                    candidate.SampleIndex >= activeSampleCount)
                {
                    continue;
                }
                int distance = targetSample - candidate.SampleIndex;
                if (distance <= 0)
                {
                    if (!looping)
                        continue;
                    distance += activeSampleCount;
                }
                if (distance >= selectedDistance)
                    continue;
                selectedSample = candidate.SampleIndex;
                selectedDistance = distance;
            }
            return selectedSample;
        }

        static int FindApproachContactSample(
            AnimationFootMotionFootPage foot,
            int liftOffSample,
            int landingSample,
            int activeSampleCount,
            bool looping)
        {
            int distance = BackwardSampleDistance(
                liftOffSample,
                landingSample,
                activeSampleCount,
                looping);
            if (distance <= 0)
                return -1;
            const float contactEpsilon = 0.0001f;
            for (int offset = 1; offset <= distance; offset++)
            {
                int sample = landingSample - offset;
                if (looping)
                    sample = Mod(sample, activeSampleCount);
                int next = sample + 1;
                if (looping)
                    next %= activeSampleCount;
                if (foot.Samples[sample].Filter.Contact <= contactEpsilon &&
                    foot.Samples[next].Filter.Contact > contactEpsilon)
                {
                    return next;
                }
            }
            return -1;
        }

        static int BackwardSampleDistance(
            int fromSample,
            int toSample,
            int activeSampleCount,
            bool looping)
        {
            int distance = toSample - fromSample;
            if (distance < 0 && looping)
                distance += activeSampleCount;
            return distance;
        }

        static float SecondsBetweenSamples(
            AnimationFootMotionDataDescriptor motionData,
            int fromSample,
            int toSample,
            bool looping,
            float sourceDurationSeconds)
        {
            float seconds =
                motionData.Raw.RootSamples[toSample].TimeSeconds -
                motionData.Raw.RootSamples[fromSample].TimeSeconds;
            if (seconds <= 0f && looping)
                seconds += sourceDurationSeconds;
            if (!float.IsFinite(seconds) || seconds < 0f)
                throw new InvalidOperationException("Formal Foot Step Event interval is invalid.");
            return seconds;
        }

        static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
