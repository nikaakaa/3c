using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterFootMotionSampleInput
    {
        public float RigLegLength;
        public Vector3[] HipPositions;
        public Quaternion[] HipRotations;
        public Vector3[] KneePositions;
        public Quaternion[] KneeRotations;
        public Vector3[] AnklePositions;
        public Quaternion[] AnkleRotations;
        public Vector3[] HeelPositions;
        public Vector3[] ToePositions;
        public Quaternion[] ToeRotations;
        public Vector3[] SolePositions;
        public Quaternion[] SoleRotations;
    }

    internal sealed class CharacterFootMotionDataInput
    {
        public float SampleRate;
        public float DurationSeconds;
        public float GroundReferenceHeight;
        public bool Loop;
        public Vector3[] RootPositions;
        public Quaternion[] RootRotations;
        public CharacterFootPlacementAnalysisThresholds Thresholds;
        public CharacterFootMotionSampleInput Left;
        public CharacterFootMotionSampleInput Right;
    }

    internal static class CharacterFootMotionDataBuilder
    {
        const float RotationEnterDegrees = 10f;
        const float RotationExitDegrees = 35f;
        const float GroundPoseReachBlendMeters = 0.03f;
        const float SupportMinimumExtensionRatio = 0.55f;
        const float SupportFullExtensionRatio = 0.8f;
        const float SupportMinimumPostureInfluence = 0.5f;
        const float GeometryEpsilon = 0.000001f;

        readonly struct GroundPoseEvidence
        {
            public GroundPoseEvidence(float positionError, float rotationError, float reach)
            {
                PositionError = positionError;
                RotationError = rotationError;
                Reach = reach;
            }

            public float PositionError { get; }
            public float RotationError { get; }
            public float Reach { get; }
        }

        sealed class FootWork
        {
            public AnimationFootMotionRawFootPage Raw;
            public Vector3[] SoleVelocity;
            public Vector3[] ToeVelocity;
            public float[] ToeHeight;
            public float[] ToeSpeed;
            public float[] PositionError;
            public float[] RotationError;
            public float[] Contact;
            public AnimationFootLockMode[] LockMode;
            public float[] LockWeight;
            public float[] SupportCandidate;
            public float[] Support;
            public float SupportGroundScoreMax;
            public float SupportEnvelopeScoreMax;
            public float SupportExtensionScoreMax;
            public float SupportDownwardScoreMax;
            public float SupportExtensionRatioMax;
            public AnimationFootMotionStepEvidence[] Step;
            public AnimationFootMotionEvent[] Events;
            public AnimationFootMotionDiagnostic[] Diagnostics;
            public string Diagnostic;
        }

        sealed class FootLockScenario
        {
            public AnimationFootLockMode[] Mode;
            public float[] Weight;
        }

        public static AnimationFootMotionDataDescriptor Build(CharacterFootMotionDataInput input)
        {
            RequireInput(input);
            int count = input.RootPositions.Length;
            float step = input.DurationSeconds / (count - 1);
            AnimationFootMotionRootSample[] roots = BuildRoots(input, step);
            FootWork left = BuildFoot(input, input.Left, step);
            FootWork right = BuildFoot(input, input.Right, step);
            ReconcileLoopSymmetry(input, left, right);
            BuildSupport(left, right);
            ValidateDerivedData(input, left, right);
            return new AnimationFootMotionDataDescriptor(
                new AnimationFootMotionRawPage(
                    input.SampleRate,
                    input.DurationSeconds,
                    input.GroundReferenceHeight,
                    roots,
                    left.Raw,
                    right.Raw),
                BuildPage(left, step),
                BuildPage(right, step));
        }

        static FootWork BuildFoot(
            CharacterFootMotionDataInput input,
            CharacterFootMotionSampleInput source,
            float step)
        {
            int count = input.RootPositions.Length;
            Vector3[] soleVelocity = Velocity(input, source.SolePositions, step);
            Vector3[] toeVelocity = Velocity(input, source.ToePositions, step);
            Vector3[] heelVelocity = Velocity(input, source.HeelPositions, step);
            Vector3[] angularVelocity = AngularVelocity(input, source.SoleRotations, step);
            float[] supportDownwardScores = BuildSupportDownwardScore(input, source);
            var rawSamples = new AnimationFootMotionRawSample[count];
            var toeHeight = new float[count];
            var toeSpeed = new float[count];
            var positionError = new float[count];
            var rotationError = new float[count];
            var contactRaw = new float[count];
            var reachable = new float[count];
            var supportCandidate = new float[count];
            float supportGroundScoreMax = 0f;
            float supportEnvelopeScoreMax = 0f;
            float supportExtensionScoreMax = 0f;
            float supportDownwardScoreMax = 0f;
            float supportExtensionRatioMax = 0f;
            for (int i = 0; i < count; i++)
            {
                rawSamples[i] = new AnimationFootMotionRawSample(
                    i * step,
                    Pose(input, i, source.HipPositions[i], source.HipRotations[i]),
                    Pose(input, i, source.KneePositions[i], source.KneeRotations[i]),
                    Pose(input, i, source.AnklePositions[i], source.AnkleRotations[i]),
                    Pose(input, i, source.HeelPositions[i], source.SoleRotations[i]),
                    Pose(input, i, source.ToePositions[i], source.ToeRotations[i]),
                    Pose(input, i, source.SolePositions[i], source.SoleRotations[i]),
                    soleVelocity[i],
                    toeVelocity[i],
                    angularVelocity[i]);
                toeHeight[i] = Vector3.Dot(
                    source.ToePositions[i] - Vector3.up * input.GroundReferenceHeight,
                    Vector3.up);
                toeSpeed[i] = toeVelocity[i].magnitude;
                GroundPoseEvidence groundPose = BuildGroundPoseEvidence(input, source, i);
                positionError[i] = groundPose.PositionError;
                rotationError[i] = groundPose.RotationError;
                reachable[i] = groundPose.Reach;
                float heelHeight = source.HeelPositions[i].y - input.GroundReferenceHeight;
                float heelScore = ContactPointScore(
                    heelHeight,
                    heelVelocity[i],
                    input.Thresholds);
                float toeScore = ContactPointScore(
                    toeHeight[i],
                    toeVelocity[i],
                    input.Thresholds);
                float positionScore = 1f - Mathf.InverseLerp(
                    input.Thresholds.PlantEnterHeight,
                    input.Thresholds.PlantExitHeight,
                    positionError[i]);
                float rotationScore = 1f - Mathf.InverseLerp(
                    RotationEnterDegrees,
                    RotationExitDegrees,
                    rotationError[i]);
                contactRaw[i] = Mathf.Clamp01(
                    Mathf.Max(heelScore, toeScore) *
                    Mathf.Lerp(0.85f, 1f, positionScore) *
                    Mathf.Lerp(0.9f, 1f, rotationScore));
            }
            float[] contact = ContactWithHysteresis(
                contactRaw,
                input.Loop,
                step,
                input.Thresholds.MinimumLandingSegmentSeconds);
            float[] supportEnvelope = BuildSupportContactEnvelope(input, contact, step);
            for (int i = 0; i < count; i++)
            {
                supportCandidate[i] = BuildSupportCandidate(
                    input,
                    source,
                    i,
                    supportEnvelope[i],
                    supportDownwardScores[i],
                    out float supportGroundScore,
                    out float supportEnvelopeEvidence,
                    out float supportExtensionScore,
                    out float supportDownwardScore,
                    out float supportExtensionRatio);
                supportGroundScoreMax = Mathf.Max(supportGroundScoreMax, supportGroundScore);
                supportEnvelopeScoreMax = Mathf.Max(supportEnvelopeScoreMax, supportEnvelopeEvidence);
                supportExtensionScoreMax = Mathf.Max(supportExtensionScoreMax, supportExtensionScore);
                supportDownwardScoreMax = Mathf.Max(supportDownwardScoreMax, supportDownwardScore);
                supportExtensionRatioMax = Mathf.Max(supportExtensionRatioMax, supportExtensionRatio);
            }
            AnimationFootMotionEvent[] events = BuildEvents(input, source, contact);
            if (MovingLoop(input) &&
                !events.Any(value => value.Kind == AnimationFootMotionEventKind.Landing))
            {
                throw new InvalidOperationException(
                    $"Cyclic Foot Motion has no Landing Event; ToeHeightMin={toeHeight.Min():R}; " +
                    $"ToeSpeedMin={toeSpeed.Min():R}; SoleSpeedMin={soleVelocity.Min(value => value.magnitude):R}; " +
                    $"SoleVerticalSpeedMin={soleVelocity.Min(value => Mathf.Abs(value.y)):R}; " +
                    $"PosErrorMin={positionError.Min():R}; RotErrorMin={rotationError.Min():R}; " +
                    $"ReachableMax={reachable.Max():R}; ContactRawMax={contactRaw.Max():R}; ContactMax={contact.Max():R}.");
            }
            AnimationFootMotionStepEvidence[] motionStep = BuildStepEvidence(
                input,
                source,
                events,
                step,
                out string diagnostic,
                out AnimationFootMotionDiagnostic[] diagnostics);
            FootLockScenario lockScenario = BuildLockScenario(
                input,
                source,
                soleVelocity,
                contact,
                positionError,
                rotationError,
                reachable,
                step);
            return new FootWork
            {
                Raw = new AnimationFootMotionRawFootPage(source.RigLegLength, rawSamples),
                SoleVelocity = soleVelocity,
                ToeVelocity = toeVelocity,
                ToeHeight = toeHeight,
                ToeSpeed = toeSpeed,
                PositionError = positionError,
                RotationError = rotationError,
                Contact = contact,
                LockMode = lockScenario.Mode,
                LockWeight = lockScenario.Weight,
                SupportCandidate = supportCandidate,
                Support = new float[count],
                SupportGroundScoreMax = supportGroundScoreMax,
                SupportEnvelopeScoreMax = supportEnvelopeScoreMax,
                SupportExtensionScoreMax = supportExtensionScoreMax,
                SupportDownwardScoreMax = supportDownwardScoreMax,
                SupportExtensionRatioMax = supportExtensionRatioMax,
                Step = motionStep,
                Events = events,
                Diagnostics = diagnostics,
                Diagnostic = diagnostic
            };
        }

        static void ReconcileLoopSymmetry(
            CharacterFootMotionDataInput input,
            FootWork left,
            FootWork right)
        {
            if (!MovingLoop(input))
                return;
            int activeCount = left.Contact.Length - 1;
            int shift = ResolveOpposingPhaseShift(
                left.Contact,
                right.Contact,
                activeCount);
            var visited = new bool[activeCount];
            for (int leftIndex = 0; leftIndex < activeCount; leftIndex++)
            {
                if (visited[leftIndex])
                    continue;
                int rightIndex = (leftIndex + shift) % activeCount;
                int pairedLeft = (rightIndex + activeCount - shift) % activeCount;
                visited[leftIndex] = true;
                visited[pairedLeft] = true;
                bool locked =
                    left.LockMode[leftIndex] == AnimationFootLockMode.Locked &&
                    right.LockMode[rightIndex] == AnimationFootLockMode.Locked;
                bool contacting = left.Contact[leftIndex] >= 0.5f ||
                                  right.Contact[rightIndex] >= 0.5f;
                AnimationFootLockMode mode = locked
                    ? AnimationFootLockMode.Locked
                    : contacting
                        ? AnimationFootLockMode.Sliding
                        : AnimationFootLockMode.Unlocked;
                float weight = locked
                    ? Mathf.Min(
                        left.LockWeight[leftIndex],
                        right.LockWeight[rightIndex])
                    : 0f;
                float supportCandidate =
                    (left.SupportCandidate[leftIndex] +
                     right.SupportCandidate[rightIndex]) * 0.5f;
                left.LockMode[leftIndex] = mode;
                right.LockMode[rightIndex] = mode;
                left.LockWeight[leftIndex] = weight;
                right.LockWeight[rightIndex] = weight;
                left.SupportCandidate[leftIndex] = supportCandidate;
                right.SupportCandidate[rightIndex] = supportCandidate;
            }
            left.LockMode[activeCount] = left.LockMode[0];
            right.LockMode[activeCount] = right.LockMode[0];
            left.LockWeight[activeCount] = left.LockWeight[0];
            right.LockWeight[activeCount] = right.LockWeight[0];
            left.SupportCandidate[activeCount] = left.SupportCandidate[0];
            right.SupportCandidate[activeCount] = right.SupportCandidate[0];
        }

        static int ResolveOpposingPhaseShift(
            float[] left,
            float[] right,
            int activeCount)
        {
            int bestShift = 0;
            float bestError = float.PositiveInfinity;
            for (int shift = 1; shift < activeCount; shift++)
            {
                float error = 0f;
                for (int i = 0; i < activeCount; i++)
                    error += Mathf.Abs(left[i] - right[(i + shift) % activeCount]);
                if (error < bestError)
                {
                    bestError = error;
                    bestShift = shift;
                }
            }
            return bestShift;
        }

        static AnimationFootMotionPose Pose(
            CharacterFootMotionDataInput input,
            int index,
            Vector3 motionPosition,
            Quaternion motionRotation)
        {
            Quaternion rootInverse = Quaternion.Inverse(input.RootRotations[index]);
            return new AnimationFootMotionPose(
                rootInverse * (motionPosition - input.RootPositions[index]),
                rootInverse * motionRotation,
                motionPosition,
                motionRotation);
        }

        static GroundPoseEvidence BuildGroundPoseEvidence(
            CharacterFootMotionDataInput input,
            CharacterFootMotionSampleInput foot,
            int index)
        {
            Vector3 up = Vector3.up;
            Vector3 solePosition = foot.SolePositions[index];
            Quaternion soleRotation = foot.SoleRotations[index];
            Vector3 targetSolePosition = solePosition +
                                         up * (input.GroundReferenceHeight - Vector3.Dot(solePosition, up));
            Vector3 targetForward = Vector3.ProjectOnPlane(soleRotation * Vector3.forward, up);
            if (targetForward.sqrMagnitude <= GeometryEpsilon)
            {
                Vector3 targetRight = Vector3.ProjectOnPlane(soleRotation * Vector3.right, up);
                if (targetRight.sqrMagnitude <= GeometryEpsilon)
                    throw new InvalidOperationException($"Foot Motion Ground Pose Sole frame is degenerate at sample {index}.");
                targetForward = Vector3.Cross(targetRight.normalized, up);
            }
            Quaternion targetSoleRotation = Quaternion.LookRotation(targetForward.normalized, up);
            Vector3 soleToAnkle = Quaternion.Inverse(soleRotation) *
                                  (foot.AnklePositions[index] - solePosition);
            Vector3 targetAnkle = targetSolePosition + targetSoleRotation * soleToAnkle;
            Vector3 hip = foot.HipPositions[index];
            Vector3 knee = foot.KneePositions[index];
            Vector3 ankle = foot.AnklePositions[index];
            float upperLength = Vector3.Distance(hip, knee);
            float lowerLength = Vector3.Distance(knee, ankle);
            if (upperLength <= GeometryEpsilon || lowerLength <= GeometryEpsilon)
                throw new InvalidOperationException($"Foot Motion Ground Pose leg chain is degenerate at sample {index}.");
            Vector3 targetLeg = targetAnkle - hip;
            float targetDistance = targetLeg.magnitude;
            Vector3 targetDirection = targetDistance > GeometryEpsilon
                ? targetLeg / targetDistance
                : ResolveLegDirection(hip, ankle, index);
            float minimumReach = Mathf.Abs(upperLength - lowerLength) + GeometryEpsilon;
            float maximumReach = upperLength + lowerLength - GeometryEpsilon;
            float solvedDistance = Mathf.Clamp(targetDistance, minimumReach, maximumReach);
            Vector3 solvedAnkle = hip + targetDirection * solvedDistance;
            float along = (upperLength * upperLength - lowerLength * lowerLength +
                           solvedDistance * solvedDistance) / (2f * solvedDistance);
            float bendHeight = Mathf.Sqrt(Mathf.Max(
                0f,
                upperLength * upperLength - along * along));
            Vector3 bendDirection = ResolveBendDirection(
                hip,
                knee,
                ankle,
                targetDirection,
                index);
            Vector3 solvedKnee = hip + targetDirection * along + bendDirection * bendHeight;
            float residual = Vector3.Distance(solvedAnkle, targetAnkle);
            float ankleCorrection = Vector3.Distance(ankle, solvedAnkle);
            float kneeCorrection = Vector3.Distance(knee, solvedKnee);
            float positionError = Mathf.Sqrt(
                (ankleCorrection * ankleCorrection + kneeCorrection * kneeCorrection) * 0.5f) + residual;
            float rotationError = Quaternion.Angle(soleRotation, targetSoleRotation);
            float reach = 1f - Mathf.InverseLerp(0f, GroundPoseReachBlendMeters, residual);
            if (!float.IsFinite(positionError) || !float.IsFinite(rotationError) || !float.IsFinite(reach))
                throw new InvalidOperationException($"Foot Motion Ground Pose result is invalid at sample {index}.");
            return new GroundPoseEvidence(positionError, rotationError, Mathf.Clamp01(reach));
        }

        static Vector3 ResolveLegDirection(
            Vector3 hip,
            Vector3 ankle,
            int index)
        {
            Vector3 direction = ankle - hip;
            if (direction.sqrMagnitude <= GeometryEpsilon)
                throw new InvalidOperationException($"Foot Motion Ground Pose leg direction is degenerate at sample {index}.");
            return direction.normalized;
        }

        static Vector3 ResolveBendDirection(
            Vector3 hip,
            Vector3 knee,
            Vector3 ankle,
            Vector3 targetDirection,
            int index)
        {
            Vector3 authoredLeg = ankle - hip;
            float denominator = authoredLeg.sqrMagnitude;
            Vector3 bend = denominator > GeometryEpsilon
                ? knee - (hip + authoredLeg * Mathf.Clamp01(
                    Vector3.Dot(knee - hip, authoredLeg) / denominator))
                : Vector3.zero;
            bend = Vector3.ProjectOnPlane(bend, targetDirection);
            if (bend.sqrMagnitude <= GeometryEpsilon)
                bend = Vector3.ProjectOnPlane(Vector3.up, targetDirection);
            if (bend.sqrMagnitude <= GeometryEpsilon)
                bend = Vector3.ProjectOnPlane(Vector3.forward, targetDirection);
            if (bend.sqrMagnitude <= GeometryEpsilon)
                bend = Vector3.ProjectOnPlane(Vector3.right, targetDirection);
            if (bend.sqrMagnitude <= GeometryEpsilon)
                throw new InvalidOperationException($"Foot Motion Ground Pose bend plane is degenerate at sample {index}.");
            return bend.normalized;
        }

        static float BuildSupportCandidate(
            CharacterFootMotionDataInput input,
            CharacterFootMotionSampleInput foot,
            int index,
            float supportEnvelope,
            float supportDownwardScore,
            out float groundScore,
            out float verticalScore,
            out float extensionScore,
            out float downwardScore,
            out float extensionRatio)
        {
            float supportHeight = Mathf.Min(
                foot.HeelPositions[index].y,
                foot.ToePositions[index].y) - input.GroundReferenceHeight;
            groundScore = 1f - Mathf.InverseLerp(
                input.Thresholds.PlantEnterHeight,
                input.Thresholds.PlantExitHeight,
                Mathf.Max(0f, supportHeight));
            verticalScore = supportEnvelope;
            downwardScore = supportDownwardScore;
            Vector3 rootLocalHip = RootLocal(input, index, foot.HipPositions[index]);
            Vector3 rootLocalAnkle = RootLocal(input, index, foot.AnklePositions[index]);
            extensionRatio = Vector3.Distance(rootLocalHip, rootLocalAnkle) / foot.RigLegLength;
            extensionScore = Mathf.InverseLerp(
                SupportMinimumExtensionRatio,
                SupportFullExtensionRatio,
                extensionRatio);
            float postureScore = Mathf.Sqrt(
                Mathf.Clamp01(extensionScore * downwardScore));
            return Mathf.Clamp01(
                groundScore * verticalScore *
                Mathf.Lerp(SupportMinimumPostureInfluence, 1f, postureScore));
        }

        static float[] BuildSupportDownwardScore(
            CharacterFootMotionDataInput input,
            CharacterFootMotionSampleInput foot)
        {
            var extent = new float[foot.SolePositions.Length];
            float maximum = float.NegativeInfinity;
            for (int i = 0; i < extent.Length; i++)
            {
                extent[i] = Vector3.Dot(
                    foot.HipPositions[i] - foot.SolePositions[i],
                    Vector3.up);
                maximum = Mathf.Max(maximum, extent[i]);
            }
            var result = new float[extent.Length];
            float range = Mathf.Max(input.Thresholds.PlantExitHeight, GeometryEpsilon);
            for (int i = 0; i < result.Length; i++)
                result[i] = 1f - Mathf.InverseLerp(0f, range, maximum - extent[i]);
            return result;
        }

        static float[] BuildSupportContactEnvelope(
            CharacterFootMotionDataInput input,
            float[] contact,
            float step)
        {
            int intervals = contact.Length - 1;
            int activeCount = input.Loop ? intervals : contact.Length;
            int rampRadius = Mathf.Max(
                1,
                Mathf.CeilToInt(input.Thresholds.MinimumLandingSegmentSeconds / step));
            var result = new float[contact.Length];
            for (int i = 0; i < activeCount; i++)
            {
                if (contact[i] <= 0.5f)
                    continue;
                int before = SupportContactDistance(
                    contact,
                    i,
                    -1,
                    activeCount,
                    input.Loop,
                    rampRadius);
                int after = SupportContactDistance(
                    contact,
                    i,
                    1,
                    activeCount,
                    input.Loop,
                    rampRadius);
                result[i] = Mathf.Clamp01(
                    (Mathf.Min(before, after) + 1f) /
                    (rampRadius + 1f));
            }
            if (input.Loop)
                result[intervals] = result[0];
            return result;
        }

        static int SupportContactDistance(
            float[] contact,
            int index,
            int direction,
            int activeCount,
            bool loop,
            int maximum)
        {
            int distance = 0;
            for (int offset = 1; offset <= maximum; offset++)
            {
                int sample = index + direction * offset;
                if (!loop && (sample < 0 || sample >= activeCount))
                    return maximum;
                sample = ((sample % activeCount) + activeCount) % activeCount;
                if (contact[sample] <= 0.5f)
                    break;
                distance++;
            }
            return distance;
        }

        static AnimationFootMotionRootSample[] BuildRoots(CharacterFootMotionDataInput input, float step)
        {
            var result = new AnimationFootMotionRootSample[input.RootPositions.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = new AnimationFootMotionRootSample(i * step, input.RootPositions[i], input.RootRotations[i]);
            return result;
        }

        static float ContactPointScore(
            float height,
            Vector3 velocity,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            float heightScore = 1f - Mathf.InverseLerp(
                thresholds.PlantEnterHeight,
                thresholds.PlantExitHeight,
                Mathf.Max(0f, height));
            float verticalScore = 1f - Mathf.InverseLerp(
                thresholds.PlantEnterContactSpeed,
                thresholds.PlantExitContactSpeed,
                Mathf.Abs(velocity.y));
            float motionScore = 1f - Mathf.InverseLerp(
                thresholds.PlantExitContactSpeed,
                thresholds.PlantExitContactSpeed * 4f,
                velocity.magnitude);
            return heightScore *
                   Mathf.Lerp(0.85f, 1f, verticalScore) *
                   Mathf.Lerp(0.9f, 1f, motionScore);
        }

        static float[] ContactWithHysteresis(
            float[] raw,
            bool loop,
            float step,
            float minimumDuration)
        {
            int intervals = raw.Length - 1;
            int activeCount = loop ? intervals : raw.Length;
            var states = new bool[activeCount];
            bool state = loop && raw.Take(intervals).All(value => value >= 0.5f);
            int start = 0;
            if (loop && !state)
            {
                for (int i = 0; i < intervals; i++)
                {
                    if (raw[i] <= 0.2f)
                    {
                        start = (i + 1) % intervals;
                        break;
                    }
                }
            }
            for (int offset = 0; offset < activeCount; offset++)
            {
                int index = loop ? (start + offset) % intervals : offset;
                if (!state && raw[index] >= 0.6f)
                    state = true;
                else if (state && raw[index] <= 0.2f)
                    state = false;
                states[index] = state;
            }
            Stabilize(states, loop, Mathf.Max(1, Mathf.CeilToInt(minimumDuration / step)));
            var result = new float[raw.Length];
            for (int i = 0; i < activeCount; i++)
                result[i] = states[i]
                    ? Mathf.Lerp(0.5f, 1f, raw[i])
                    : Mathf.Min(0.499f, raw[i] * 0.5f);
            if (loop)
                result[intervals] = result[0];
            return result;
        }

        static AnimationFootMotionEvent[] BuildEvents(
            CharacterFootMotionDataInput input,
            CharacterFootMotionSampleInput foot,
            float[] contact)
        {
            int intervals = contact.Length - 1;
            int activeCount = input.Loop ? intervals : contact.Length;
            var result = new List<AnimationFootMotionEvent>();
            int landingOrdinal = 0;
            int liftOrdinal = 0;
            for (int i = 0; i < activeCount; i++)
            {
                bool current = contact[i] >= 0.5f;
                bool previous = i > 0
                    ? contact[i - 1] >= 0.5f
                    : input.Loop && contact[intervals - 1] >= 0.5f;
                if (current == previous)
                    continue;
                AnimationFootMotionEventKind kind = current
                    ? AnimationFootMotionEventKind.Landing
                    : AnimationFootMotionEventKind.LiftOff;
                int ordinal = current ? ++landingOrdinal : ++liftOrdinal;
                result.Add(new AnimationFootMotionEvent(
                    kind,
                    i,
                    ordinal,
                    0,
                    RootLocal(input, i, foot.SolePositions[i]),
                    foot.SolePositions[i],
                    foot.SoleRotations[i]));
            }
            return result.OrderBy(value => value.SampleIndex).ThenBy(value => value.Kind).ToArray();
        }

        static AnimationFootMotionStepEvidence[] BuildStepEvidence(
            CharacterFootMotionDataInput input,
            CharacterFootMotionSampleInput foot,
            AnimationFootMotionEvent[] events,
            float step,
            out string diagnostic,
            out AnimationFootMotionDiagnostic[] diagnostics)
        {
            int intervals = foot.SolePositions.Length - 1;
            int[] landings = events
                .Where(value => value.Kind == AnimationFootMotionEventKind.Landing)
                .Select(value => value.SampleIndex)
                .ToArray();
            var result = new AnimationFootMotionStepEvidence[foot.SolePositions.Length];
            var notes = new List<AnimationFootMotionDiagnostic>();
            var zeroOrdinals = new HashSet<int>();
            int unavailable = 0;
            if (landings.Length == 0)
            {
                if (MovingLoop(input))
                    throw new InvalidOperationException("Cyclic Foot Motion data has no Landing Event.");
                diagnostic = string.Empty;
                diagnostics = new[]
                {
                    new AnimationFootMotionDiagnostic(AnimationFootMotionDiagnosticCode.NoStep, -1)
                };
                return result.Select((_, i) => NoStep(
                    i,
                    step,
                    foot.SolePositions[i].y,
                    input.GroundReferenceHeight)).ToArray();
            }
            for (int i = 0; i < result.Length; i++)
            {
                if (!TryLandingBounds(
                        i,
                        intervals,
                        input.Loop,
                        landings,
                        out int previous,
                        out int next,
                        out int ordinal,
                        out bool completed))
                {
                    result[i] = NoStep(
                        i,
                        step,
                        foot.SolePositions[i].y,
                        input.GroundReferenceHeight);
                    unavailable++;
                    continue;
                }
                float totalDistance = AccumulatePlanarDistance(input, foot.SolePositions, previous, next);
                if (totalDistance <= 0.000001f && zeroOrdinals.Add(ordinal))
                    notes.Add(new AnimationFootMotionDiagnostic(
                        AnimationFootMotionDiagnosticCode.ZeroLengthStep,
                        i));
                if (input.Loop && next > intervals &&
                    notes.All(value => value.Code != AnimationFootMotionDiagnosticCode.LoopCycleBoundary))
                    notes.Add(new AnimationFootMotionDiagnostic(
                        AnimationFootMotionDiagnosticCode.LoopCycleBoundary,
                        i));
                if (completed &&
                    notes.All(value => value.Code != AnimationFootMotionDiagnosticCode.FiniteTerminalSegment))
                    notes.Add(new AnimationFootMotionDiagnostic(
                        AnimationFootMotionDiagnosticCode.FiniteTerminalSegment,
                        i));
                float currentDistance = AccumulatePlanarDistance(input, foot.SolePositions, previous, i);
                float progress = totalDistance > 0.000001f
                    ? Mathf.Clamp01(currentDistance / totalDistance)
                    : next > previous ? Mathf.InverseLerp(previous, next, i) : 1f;
                Vector3 previousPosition = UnwrappedPosition(input, foot.SolePositions, previous);
                Vector3 nextPosition = UnwrappedPosition(input, foot.SolePositions, next);
                Vector3 currentPosition = UnwrappedPosition(input, foot.SolePositions, i);
                float baseline = Mathf.Lerp(previousPosition.y, nextPosition.y, progress);
                float height = currentPosition.y;
                result[i] = new AnimationFootMotionStepEvidence(
                    true,
                    ordinal,
                    completed ? 0f : Mathf.Max(0f, (next - i) * step),
                    Vector3.ProjectOnPlane(nextPosition - previousPosition, Vector3.up).magnitude,
                    progress,
                    baseline,
                    height,
                    Mathf.Max(0f, height - baseline));
            }
            diagnostic = unavailable == 0
                ? string.Empty
                : $"StepDataUnavailable:{unavailable}";
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Kind != AnimationFootMotionEventKind.Landing)
                    continue;
                int sampleIndex = events[i].SampleIndex;
                if (result[sampleIndex].Available &&
                    (result[sampleIndex].TimeSeconds > 0.0001f ||
                     result[sampleIndex].HeightAbovePath > 0.0001f))
                    throw new InvalidOperationException(
                        $"Foot Motion Step Time or Height Above Path does not return to zero at Landing sample {sampleIndex}.");
            }
            diagnostics = notes.ToArray();
            return result;
        }

        static void ValidateDerivedData(
            CharacterFootMotionDataInput input,
            FootWork left,
            FootWork right)
        {
            ValidateFoot(input, left, "Left");
            ValidateFoot(input, right, "Right");
            if (input.Loop)
            {
                if (!left.Support.Any(value => value > 0.0001f) ||
                    !right.Support.Any(value => value > 0.0001f))
                    throw new InvalidOperationException(
                        $"Foot Motion Support is missing on a Loop animation: " +
                        $"LeftCandidateMax={left.SupportCandidate.Max():R}; " +
                        $"RightCandidateMax={right.SupportCandidate.Max():R}; " +
                        $"LeftScores={left.SupportGroundScoreMax:R}/{left.SupportEnvelopeScoreMax:R}/" +
                        $"{left.SupportExtensionScoreMax:R}/{left.SupportDownwardScoreMax:R}; " +
                        $"RightScores={right.SupportGroundScoreMax:R}/{right.SupportEnvelopeScoreMax:R}/" +
                        $"{right.SupportExtensionScoreMax:R}/{right.SupportDownwardScoreMax:R}; " +
                        $"ExtensionRatios={left.SupportExtensionRatioMax:R}/{right.SupportExtensionRatioMax:R}.");
            }
            int sampleCount = left.Support.Length;
            for (int i = 0; i < sampleCount; i++)
            {
                float expectedPresence = Mathf.Max(
                    left.SupportCandidate[i],
                    right.SupportCandidate[i]);
                float supportSum = left.Support[i] + right.Support[i];
                if (!float.IsFinite(expectedPresence) || !float.IsFinite(supportSum) ||
                    supportSum < 0f || supportSum > 1f ||
                    Mathf.Abs(supportSum - expectedPresence) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        $"Foot Motion Support Presence mismatch at sample {i}: " +
                        $"LeftCandidate={left.SupportCandidate[i]:R}; " +
                        $"RightCandidate={right.SupportCandidate[i]:R}; " +
                        $"LeftSupport={left.Support[i]:R}; RightSupport={right.Support[i]:R}.");
                }
                ValidateLockWeight(left, i, "Left");
                ValidateLockWeight(right, i, "Right");
            }
        }

        static void ValidateLockWeight(FootWork foot, int sample, string side)
        {
            if (foot.LockMode[sample] != AnimationFootLockMode.Locked &&
                foot.LockWeight[sample] > 0.0001f)
            {
                throw new InvalidOperationException(
                    $"{side} Foot Motion Lock Weight remains active without a Locked Anchor at sample {sample}.");
            }
        }

        static void ValidateFoot(
            CharacterFootMotionDataInput input,
            FootWork foot,
            string side)
        {
            for (int i = 0; i < foot.Step.Length; i++)
            {
                AnimationFootMotionStepEvidence step = foot.Step[i];
                float expectedHeight = foot.Raw.Samples[i].Sole.MotionPosition.y;
                float expectedAbovePath = Mathf.Max(0f, step.AnimationHeight - step.BaselineHeight);
                if (!input.Loop && Mathf.Abs(step.AnimationHeight - expectedHeight) > 0.00001f)
                    throw new InvalidOperationException(
                        $"{side} Foot Motion finite Height source mismatch at sample {i}: " +
                        $"Raw={expectedHeight:R}; Derived={step.AnimationHeight:R}.");
                if (Mathf.Abs(step.HeightAbovePath - expectedAbovePath) > 0.00001f)
                    throw new InvalidOperationException(
                        $"{side} Foot Motion Height Above Path mismatch at sample {i}: " +
                        $"Expected={expectedAbovePath:R}; Actual={step.HeightAbovePath:R}.");
            }
            if (!MovingLoop(input))
                return;
            if (!foot.Contact.Any(value => value >= 0.5f))
                throw new InvalidOperationException($"{side} Cyclic Foot Motion Contact is always zero.");
            if (!foot.Step.Any(value => value.Available && value.TimeSeconds > 0.0001f))
                throw new InvalidOperationException($"{side} Cyclic Foot Motion Step Time is always zero.");
            if (!foot.Step.Any(value => value.Available && value.Distance > 0.01f))
                throw new InvalidOperationException($"{side} Cyclic Foot Motion Step Distance is always zero.");
            if (!foot.LockMode.Any(value => value != AnimationFootLockMode.Unlocked))
                throw new InvalidOperationException($"{side} Cyclic Foot Motion Lock Mode is always Unlocked.");
        }

        static AnimationFootMotionStepEvidence NoStep(
            int index,
            float step,
            float height,
            float groundReferenceHeight) =>
            new AnimationFootMotionStepEvidence(
                false,
                0,
                0f,
                0f,
                0f,
                groundReferenceHeight,
                height,
                Mathf.Max(0f, height - groundReferenceHeight));

        static bool TryLandingBounds(
            int sample,
            int intervals,
            bool loop,
            IReadOnlyList<int> landings,
            out int previous,
            out int next,
            out int ordinal,
            out bool completed)
        {
            previous = int.MinValue;
            next = int.MaxValue;
            ordinal = 0;
            completed = false;
            for (int i = 0; i < landings.Count; i++)
            {
                int landing = landings[i];
                if (landing <= sample && landing > previous)
                    previous = landing;
                if (landing >= sample && landing < next)
                {
                    next = landing;
                    ordinal = i + 1;
                }
            }
            if (loop)
            {
                if (previous == int.MinValue)
                    previous = landings[landings.Count - 1] - intervals;
                if (next == int.MaxValue)
                {
                    next = landings[0] + intervals;
                    ordinal = 1;
                }
                if (previous == next)
                    previous = PreviousLanding(next, intervals, landings);
                return previous < next;
            }
            if (next != int.MaxValue)
            {
                int targetIndex = ordinal - 1;
                previous = targetIndex > 0 ? landings[targetIndex - 1] : 0;
                return previous <= next;
            }
            int lastIndex = landings.Count - 1;
            next = landings[lastIndex];
            previous = lastIndex > 0 ? landings[lastIndex - 1] : 0;
            ordinal = lastIndex + 1;
            completed = true;
            return previous <= next;
        }

        static int PreviousLanding(int next, int intervals, IReadOnlyList<int> landings)
        {
            int local = Mod(next, intervals);
            int index = -1;
            for (int i = 0; i < landings.Count; i++)
            {
                if (landings[i] == local)
                {
                    index = i;
                    break;
                }
            }
            return index > 0 ? landings[index - 1] : landings[landings.Count - 1] - intervals;
        }

        static float AccumulatePlanarDistance(
            CharacterFootMotionDataInput input,
            Vector3[] positions,
            int from,
            int to)
        {
            if (to <= from)
                return 0f;
            float distance = 0f;
            Vector3 previous = UnwrappedPosition(input, positions, from);
            for (int sample = from + 1; sample <= to; sample++)
            {
                Vector3 current = UnwrappedPosition(input, positions, sample);
                distance += Vector3.ProjectOnPlane(current - previous, Vector3.up).magnitude;
                previous = current;
            }
            return distance;
        }

        static Vector3 UnwrappedPosition(
            CharacterFootMotionDataInput input,
            Vector3[] positions,
            int sample)
        {
            if (!input.Loop)
            {
                if ((uint)sample >= (uint)positions.Length)
                    throw new ArgumentOutOfRangeException(nameof(sample));
                return positions[sample];
            }
            int intervals = positions.Length - 1;
            int cycle = FloorDiv(sample, intervals);
            int local = Mod(sample, intervals);
            return ApplyCycle(input, positions[local], cycle);
        }

        static FootLockScenario BuildLockScenario(
            CharacterFootMotionDataInput input,
            CharacterFootMotionSampleInput foot,
            Vector3[] soleVelocity,
            float[] contact,
            float[] positionError,
            float[] rotationError,
            float[] reachable,
            float step)
        {
            int intervals = contact.Length - 1;
            int activeCount = input.Loop ? intervals : contact.Length;
            var modeResult = new AnimationFootLockMode[contact.Length];
            var anchorDistance = new float[contact.Length];
            var anchorValid = new bool[contact.Length];
            float enterDistance = input.Thresholds.PlantEnterContactSpeed *
                                  input.Thresholds.MinimumLandingSegmentSeconds;
            float exitDistance = input.Thresholds.PlantExitContactSpeed *
                                 input.Thresholds.MinimumLandingSegmentSeconds;
            AnimationFootLockMode mode = AnimationFootLockMode.Unlocked;
            bool hasAnchor = false;
            Vector3 anchor = Vector3.zero;
            float accumulatedDrift = 0f;
            for (int pass = 0; pass < (input.Loop ? 2 : 1); pass++)
            {
                for (int i = 0; i < activeCount; i++)
                {
                    int absoluteSample = pass * activeCount + i;
                    Vector3 solePosition = input.Loop
                        ? UnwrappedPosition(input, foot.SolePositions, absoluteSample)
                        : foot.SolePositions[i];
                    if (contact[i] < 0.5f || reachable[i] <= 0f)
                    {
                        mode = AnimationFootLockMode.Unlocked;
                        hasAnchor = false;
                        accumulatedDrift = 0f;
                    }
                    else
                    {
                        float speed = Vector3.ProjectOnPlane(soleVelocity[i], Vector3.up).magnitude;
                        bool enterLocked = speed <= input.Thresholds.PlantEnterContactSpeed &&
                                           positionError[i] <= input.Thresholds.PlantEnterHeight &&
                                           rotationError[i] <= RotationEnterDegrees;
                        float drift = hasAnchor
                            ? Vector3.ProjectOnPlane(solePosition - anchor, Vector3.up).magnitude
                            : float.PositiveInfinity;
                        if (mode == AnimationFootLockMode.Locked && hasAnchor)
                            accumulatedDrift = Mathf.Max(accumulatedDrift, drift);
                        bool leaveLocked = mode == AnimationFootLockMode.Locked &&
                                           (accumulatedDrift >= exitDistance ||
                                           positionError[i] >= input.Thresholds.PlantExitHeight ||
                                           rotationError[i] >= RotationExitDegrees);
                        if (leaveLocked)
                        {
                            mode = AnimationFootLockMode.Sliding;
                            hasAnchor = false;
                            accumulatedDrift = 0f;
                        }
                        else if (mode != AnimationFootLockMode.Locked && enterLocked)
                        {
                            mode = AnimationFootLockMode.Locked;
                            anchor = solePosition;
                            hasAnchor = true;
                            accumulatedDrift = 0f;
                        }
                        else if (mode == AnimationFootLockMode.Unlocked)
                            mode = AnimationFootLockMode.Sliding;
                    }
                    if (!input.Loop || pass == 1)
                    {
                        modeResult[i] = mode;
                        anchorValid[i] = mode == AnimationFootLockMode.Locked && hasAnchor;
                        anchorDistance[i] = anchorValid[i]
                            ? accumulatedDrift
                            : 0f;
                    }
                }
            }
            RemoveShortLockedSegments(
                modeResult,
                contact,
                input.Loop,
                Mathf.Max(1, Mathf.CeilToInt(input.Thresholds.MinimumLandingSegmentSeconds / step)));
            if (input.Loop)
            {
                modeResult[intervals] = modeResult[0];
                anchorValid[intervals] = anchorValid[0];
                anchorDistance[intervals] = anchorDistance[0];
            }
            var weight = new float[contact.Length];
            for (int i = 0; i < weight.Length; i++)
            {
                if (modeResult[i] != AnimationFootLockMode.Locked || !anchorValid[i])
                    continue;
                float distanceScore = 1f - Mathf.InverseLerp(
                    enterDistance,
                    exitDistance,
                    anchorDistance[i]);
                float positionScore = 1f - Mathf.InverseLerp(
                    input.Thresholds.PlantEnterHeight,
                    input.Thresholds.PlantExitHeight,
                    positionError[i]);
                float rotationScore = 1f - Mathf.InverseLerp(
                    RotationEnterDegrees,
                    RotationExitDegrees,
                    rotationError[i]);
                weight[i] = Mathf.Clamp01(
                    contact[i] * Mathf.Sqrt(
                        Mathf.Clamp01(distanceScore * positionScore * rotationScore * reachable[i])));
            }
            return new FootLockScenario
            {
                Mode = modeResult,
                Weight = weight
            };
        }

        static void RemoveShortLockedSegments(
            AnimationFootLockMode[] values,
            float[] contact,
            bool loop,
            int minimumSamples)
        {
            if (minimumSamples <= 1)
                return;
            int activeCount = loop ? values.Length - 1 : values.Length;
            int scanStart = 0;
            if (loop)
            {
                while (scanStart < activeCount && values[scanStart] == AnimationFootLockMode.Locked)
                    scanStart++;
                if (scanStart == activeCount)
                    return;
                scanStart++;
            }
            int scanned = 0;
            while (scanned < activeCount)
            {
                int index = (scanStart + scanned) % activeCount;
                if (values[index] != AnimationFootLockMode.Locked)
                {
                    scanned++;
                    continue;
                }
                int length = 0;
                while (length < activeCount - scanned &&
                       values[(index + length) % activeCount] == AnimationFootLockMode.Locked)
                    length++;
                if (length < minimumSamples)
                {
                    for (int offset = 0; offset < length; offset++)
                    {
                        int sample = (index + offset) % activeCount;
                        values[sample] = contact[sample] >= 0.5f
                            ? AnimationFootLockMode.Sliding
                            : AnimationFootLockMode.Unlocked;
                    }
                }
                scanned += length;
            }
        }

        static void BuildSupport(FootWork left, FootWork right)
        {
            int count = left.SupportCandidate.Length;
            for (int i = 0; i < count; i++)
            {
                bool leftValid = left.SupportCandidate[i] > 0.0001f;
                bool rightValid = right.SupportCandidate[i] > 0.0001f;
                float presence = Mathf.Max(left.SupportCandidate[i], right.SupportCandidate[i]);
                if (!leftValid && !rightValid)
                    continue;
                if (leftValid && !rightValid)
                {
                    left.Support[i] = presence;
                    continue;
                }
                if (!leftValid)
                {
                    right.Support[i] = presence;
                    continue;
                }
                Vector3 leftSole = left.Raw.Samples[i].Sole.MotionPosition;
                Vector3 rightSole = right.Raw.Samples[i].Sole.MotionPosition;
                Vector3 pelvis = (left.Raw.Samples[i].Hip.MotionPosition +
                                  right.Raw.Samples[i].Hip.MotionPosition) * 0.5f;
                Vector3 segment = Vector3.ProjectOnPlane(rightSole - leftSole, Vector3.up);
                float t = segment.sqrMagnitude > 0.0000001f
                    ? Mathf.Clamp01(Vector3.Dot(
                        Vector3.ProjectOnPlane(pelvis - leftSole, Vector3.up),
                        segment) / segment.sqrMagnitude)
                    : 0.5f;
                float leftWeight = (1f - t) * left.SupportCandidate[i];
                float rightWeight = t * right.SupportCandidate[i];
                float total = leftWeight + rightWeight;
                if (total > 0.000001f)
                {
                    left.Support[i] = presence * leftWeight / total;
                    right.Support[i] = presence * rightWeight / total;
                }
            }
        }

        static AnimationFootMotionFootPage BuildPage(FootWork work, float step)
        {
            var samples = new AnimationFootMotionDerivedSample[work.Contact.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = new AnimationFootMotionDerivedSample(
                    i * step,
                    work.Step[i],
                    new AnimationFootMotionFilterEvidence(
                        work.ToeHeight[i],
                        work.ToeSpeed[i],
                        work.PositionError[i],
                        work.RotationError[i],
                        work.Contact[i]),
                    new AnimationFootMotionConstraintEvidence(
                        work.LockMode[i],
                        work.LockWeight[i],
                        work.SupportCandidate[i],
                        work.Support[i]));
            }
            return new AnimationFootMotionFootPage(
                work.Events,
                work.Diagnostics,
                samples,
                work.Diagnostic);
        }

        static Vector3[] Velocity(
            CharacterFootMotionDataInput input,
            Vector3[] positions,
            float step)
        {
            int last = positions.Length - 1;
            var result = new Vector3[positions.Length];
            for (int i = 0; i <= last; i++)
            {
                if (input.Loop)
                    result[i] = (UnwrappedPosition(input, positions, i + 1) -
                                 UnwrappedPosition(input, positions, i - 1)) / (2f * step);
                else if (i == 0)
                    result[i] = (positions[1] - positions[0]) / step;
                else if (i == last)
                    result[i] = (positions[last] - positions[last - 1]) / step;
                else
                    result[i] = (positions[i + 1] - positions[i - 1]) / (2f * step);
            }
            return result;
        }

        static Vector3[] AngularVelocity(
            CharacterFootMotionDataInput input,
            Quaternion[] rotations,
            float step)
        {
            int last = rotations.Length - 1;
            var result = new Vector3[rotations.Length];
            for (int i = 0; i <= last; i++)
            {
                int previous;
                int next;
                float duration;
                Quaternion previousRotation;
                Quaternion nextRotation;
                if (input.Loop)
                {
                    previousRotation = UnwrappedRotation(input, rotations, i - 1);
                    nextRotation = UnwrappedRotation(input, rotations, i + 1);
                    duration = 2f * step;
                }
                else
                {
                    previous = Mathf.Max(0, i - 1);
                    next = Mathf.Min(last, i + 1);
                    previousRotation = rotations[previous];
                    nextRotation = rotations[next];
                    duration = (next - previous) * step;
                }
                Quaternion delta = (nextRotation * Quaternion.Inverse(previousRotation)).normalized;
                delta.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f)
                    angle -= 360f;
                result[i] = duration > 0f && axis.sqrMagnitude > 0f
                    ? axis.normalized * (angle * Mathf.Deg2Rad / duration)
                    : Vector3.zero;
            }
            return result;
        }

        static Quaternion UnwrappedRotation(
            CharacterFootMotionDataInput input,
            Quaternion[] rotations,
            int sample)
        {
            int intervals = rotations.Length - 1;
            int cycle = FloorDiv(sample, intervals);
            int local = Mod(sample, intervals);
            Quaternion cycleRotation = CycleRotation(input);
            Quaternion result = rotations[local];
            if (cycle > 0)
            {
                for (int i = 0; i < cycle; i++)
                    result = cycleRotation * result;
            }
            else
            {
                Quaternion inverse = Quaternion.Inverse(cycleRotation);
                for (int i = 0; i > cycle; i--)
                    result = inverse * result;
            }
            return result.normalized;
        }

        static Vector3 ApplyCycle(CharacterFootMotionDataInput input, Vector3 position, int cycle)
        {
            Quaternion rotation = CycleRotation(input);
            Vector3 translation = input.RootPositions[input.RootPositions.Length - 1] -
                                  rotation * input.RootPositions[0];
            Vector3 result = position;
            if (cycle > 0)
            {
                for (int i = 0; i < cycle; i++)
                    result = rotation * result + translation;
            }
            else
            {
                Quaternion inverse = Quaternion.Inverse(rotation);
                for (int i = 0; i > cycle; i--)
                    result = inverse * (result - translation);
            }
            return result;
        }

        static Quaternion CycleRotation(CharacterFootMotionDataInput input) =>
            (input.RootRotations[input.RootRotations.Length - 1] *
             Quaternion.Inverse(input.RootRotations[0])).normalized;

        static Vector3 RootLocal(CharacterFootMotionDataInput input, int index, Vector3 position) =>
            Quaternion.Inverse(input.RootRotations[index]) * (position - input.RootPositions[index]);

        static void Stabilize(bool[] values, bool loop, int minimumSamples)
        {
            if (minimumSamples <= 1)
                return;
            for (int pass = 0; pass < values.Length; pass++)
            {
                bool changed = false;
                int start = 0;
                while (start < values.Length)
                {
                    int end = start + 1;
                    while (end < values.Length && values[end] == values[start])
                        end++;
                    int length = end - start;
                    if (length < minimumSamples && (start > 0 || loop) && (end < values.Length || loop))
                    {
                        bool before = values[(start + values.Length - 1) % values.Length];
                        bool after = values[end % values.Length];
                        if (before == after)
                        {
                            for (int i = start; i < end; i++)
                                values[i] = before;
                            changed = true;
                        }
                    }
                    start = end;
                }
                if (!changed)
                    return;
            }
        }

        static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
        }

        static int Mod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        static bool MovingLoop(CharacterFootMotionDataInput input) =>
            input.Loop &&
            Vector3.ProjectOnPlane(
                input.RootPositions[input.RootPositions.Length - 1] - input.RootPositions[0],
                Vector3.up).magnitude > 0.01f;

        static void RequireInput(CharacterFootMotionDataInput input)
        {
            if (input == null || input.Left == null || input.Right == null || input.Thresholds == null ||
                !float.IsFinite(input.SampleRate) || input.SampleRate <= 0f ||
                !float.IsFinite(input.DurationSeconds) || input.DurationSeconds <= 0f ||
                !float.IsFinite(input.GroundReferenceHeight) || input.RootPositions == null ||
                input.RootRotations == null || input.RootPositions.Length < 3 ||
                input.RootPositions.Length != input.RootRotations.Length)
                throw new ArgumentException("Foot Motion data input is invalid.");
            input.Thresholds.RequireValid();
            RequireFoot(input.Left, input.RootPositions.Length);
            RequireFoot(input.Right, input.RootPositions.Length);
        }

        static void RequireFoot(CharacterFootMotionSampleInput foot, int count)
        {
            if (!float.IsFinite(foot.RigLegLength) || foot.RigLegLength <= 0f ||
                foot.HipPositions?.Length != count || foot.HipRotations?.Length != count ||
                foot.KneePositions?.Length != count || foot.KneeRotations?.Length != count ||
                foot.AnklePositions?.Length != count || foot.AnkleRotations?.Length != count ||
                foot.HeelPositions?.Length != count || foot.ToePositions?.Length != count ||
                foot.ToeRotations?.Length != count || foot.SolePositions?.Length != count ||
                foot.SoleRotations?.Length != count)
                throw new ArgumentException("Foot Motion foot input is invalid.");
        }
    }
}
