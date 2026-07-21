using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal readonly struct AnimationBlendBoneVelocity
    {
        public AnimationBlendBoneVelocity(Vector3 linear, Vector3 angular, Vector3 scale)
        {
            if (!AnimationBlendPoseMath.IsFinite(linear) ||
                !AnimationBlendPoseMath.IsFinite(angular) ||
                !AnimationBlendPoseMath.IsFinite(scale))
                throw new ArgumentException("Animation Bone velocity is non-finite.");
            Linear = linear;
            Angular = angular;
            Scale = scale;
        }

        public Vector3 Linear { get; }
        public Vector3 Angular { get; }
        public Vector3 Scale { get; }
        public bool IsValid => AnimationBlendPoseMath.IsFinite(Linear) &&
                               AnimationBlendPoseMath.IsFinite(Angular) &&
                               AnimationBlendPoseMath.IsFinite(Scale);
    }

    internal readonly struct AnimationBlendSourcePoseFrame
    {
        public AnimationBlendSourcePoseFrame(
            AnimationPlaybackId playbackId,
            int programProducerIndex,
            AnimationReadOnlyBuffer<AnimationLocalBonePose> denseLocalPose,
            AnimationReadOnlyBuffer<AnimationBlendBoneVelocity> denseVelocity,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            float visualTimeScale)
        {
            if (!playbackId.IsValid || programProducerIndex < 0 ||
                denseLocalPose.Count == 0 || denseVelocity.Count != denseLocalPose.Count ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f ||
                hasFootFeatures && (!leftFootFeatures.IsValid || !rightFootFeatures.IsValid))
                throw new ArgumentException("Animation source pose frame is invalid.");
            PlaybackId = playbackId;
            ProgramProducerIndex = programProducerIndex;
            DenseLocalPose = denseLocalPose;
            DenseVelocity = denseVelocity;
            PoseParameters = poseParameters;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            VisualTimeScale = visualTimeScale;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public int ProgramProducerIndex { get; }
        public AnimationReadOnlyBuffer<AnimationLocalBonePose> DenseLocalPose { get; }
        public AnimationReadOnlyBuffer<AnimationBlendBoneVelocity> DenseVelocity { get; }
        public AnimationReadOnlyBuffer<float> PoseParameters { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
        public bool HasFootFeatures { get; }
        public float VisualTimeScale { get; }
    }

    internal static class AnimationBlendPoseMath
    {
        const float QuaternionTolerance = 0.0000001f;

        public static AnimationLocalBonePose BlendWeighted(
            Vector3 positionSum,
            Vector4 rotationSum,
            Vector3 scaleSum,
            float weight,
            AnimationLocalBonePose referencePose)
        {
            if (weight <= 0f)
                return referencePose;
            Quaternion rotation = new Quaternion(rotationSum.x, rotationSum.y, rotationSum.z, rotationSum.w);
            if (Quaternion.Dot(rotation, rotation) <= QuaternionTolerance)
                throw new InvalidOperationException("Animation Blend produced a degenerate rotation.");
            return new AnimationLocalBonePose(positionSum / weight, rotation.normalized, scaleSum / weight);
        }

        public static Vector4 AlignAndScale(Quaternion rotation, Quaternion reference, float weight)
        {
            float sign = Quaternion.Dot(rotation, reference) < 0f ? -1f : 1f;
            float scale = sign * weight;
            return new Vector4(rotation.x * scale, rotation.y * scale, rotation.z * scale, rotation.w * scale);
        }

        public static AnimationBlendBoneVelocity Differentiate(
            AnimationLocalBonePose previous,
            AnimationLocalBonePose current,
            float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0f)
                return default;
            Quaternion delta = current.Rotation * Quaternion.Inverse(previous.Rotation);
            Vector3 angular = QuaternionLog(delta) / deltaSeconds;
            return new AnimationBlendBoneVelocity(
                (current.Position - previous.Position) / deltaSeconds,
                angular,
                (current.Scale - previous.Scale) / deltaSeconds);
        }

        public static Vector3 QuaternionLog(Quaternion value)
        {
            Quaternion normalized = value.normalized;
            if (normalized.w < 0f)
                normalized = new Quaternion(-normalized.x, -normalized.y, -normalized.z, -normalized.w);
            Vector3 vector = new Vector3(normalized.x, normalized.y, normalized.z);
            float magnitude = vector.magnitude;
            if (magnitude <= QuaternionTolerance)
                return Vector3.zero;
            float angle = 2f * Mathf.Atan2(magnitude, Mathf.Clamp(normalized.w, -1f, 1f));
            return vector * (angle / magnitude);
        }

        public static Quaternion QuaternionExp(Vector3 value)
        {
            float angle = value.magnitude;
            if (angle <= QuaternionTolerance)
                return Quaternion.identity;
            float half = angle * 0.5f;
            float scale = Mathf.Sin(half) / angle;
            return new Quaternion(value.x * scale, value.y * scale, value.z * scale, Mathf.Cos(half)).normalized;
        }

        public static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
