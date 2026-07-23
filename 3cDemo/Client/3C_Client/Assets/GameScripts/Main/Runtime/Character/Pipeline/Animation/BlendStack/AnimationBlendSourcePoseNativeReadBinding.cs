using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal readonly struct AnimationBlendSourcePoseNativeReadBinding
    {
        internal AnimationBlendSourcePoseNativeReadBinding(
            int boneCount,
            int parameterCount,
            int sourceCapacity,
            ulong completionIdentity,
            NativeArray<AnimationLocalBonePose> currentPose,
            NativeArray<AnimationBlendBoneVelocity> velocity,
            NativeArray<float> poseParameters,
            NativeArray<byte> poseParameterAvailability,
            NativeArray<AnimationFootFeatureSample> leftFootFeatures,
            NativeArray<AnimationFootFeatureSample> rightFootFeatures,
            NativeArray<float> visualTimeScales,
            NativeArray<byte> hasFootFeatures,
            NativeArray<ulong> completedAt,
            NativeArray<int> programProducerIndices)
        {
            if (boneCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(boneCount));
            if (parameterCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(parameterCount));
            if (sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCapacity));
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));

            int poseCapacity = checked(sourceCapacity * boneCount);
            int parameterCapacity = checked(sourceCapacity * parameterCount);
            RequireLength(currentPose, poseCapacity);
            RequireLength(velocity, poseCapacity);
            RequireLength(poseParameters, parameterCapacity);
            RequireLength(poseParameterAvailability, parameterCapacity);
            RequireLength(leftFootFeatures, sourceCapacity);
            RequireLength(rightFootFeatures, sourceCapacity);
            RequireLength(visualTimeScales, sourceCapacity);
            RequireLength(hasFootFeatures, sourceCapacity);
            RequireLength(completedAt, sourceCapacity);
            RequireLength(programProducerIndices, sourceCapacity);

            BoneCount = boneCount;
            ParameterCount = parameterCount;
            SourceCapacity = sourceCapacity;
            CompletionIdentity = completionIdentity;
            CurrentPose = currentPose;
            Velocity = velocity;
            PoseParameters = poseParameters;
            PoseParameterAvailability = poseParameterAvailability;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            VisualTimeScales = visualTimeScales;
            HasFootFeatures = hasFootFeatures;
            CompletedAt = completedAt;
            ProgramProducerIndices = programProducerIndices;
        }

        internal int BoneCount { get; }
        internal int ParameterCount { get; }
        internal int SourceCapacity { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeArray<AnimationLocalBonePose> CurrentPose { get; }
        internal NativeArray<AnimationBlendBoneVelocity> Velocity { get; }
        internal NativeArray<float> PoseParameters { get; }
        internal NativeArray<byte> PoseParameterAvailability { get; }
        internal NativeArray<AnimationFootFeatureSample> LeftFootFeatures { get; }
        internal NativeArray<AnimationFootFeatureSample> RightFootFeatures { get; }
        internal NativeArray<float> VisualTimeScales { get; }
        internal NativeArray<byte> HasFootFeatures { get; }
        internal NativeArray<ulong> CompletedAt { get; }
        internal NativeArray<int> ProgramProducerIndices { get; }

        static void RequireLength<T>(NativeArray<T> values, int expectedLength) where T : struct
        {
            if (!values.IsCreated || values.Length != expectedLength)
                throw new ArgumentException("Animation Blend source pose Native read container length is invalid.");
        }
    }
}
