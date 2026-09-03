using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal abstract class CharacterPoseManagedValuePage
    {
        internal const float ScaleEpsilon = 0.000001f;
        internal CharacterPoseValuePageSlice m_ValuePage;

        internal NativeArray<AnimationLocalBonePose> m_ValueDenseLocalPoses;
        internal NativeArray<AnimationBlendBoneVelocity> m_ValueDenseVelocities;
        internal NativeArray<float> m_ValuePoseParameters;
        internal NativeArray<byte> m_ValuePoseParameterAvailability;
        internal NativeArray<AnimationPrimitivePoseContribution> m_ValueContributions;
        internal NativeArray<float> m_ValueDenseContributionWeights;
        internal NativeArray<int> m_ValueContributionCounts;
        internal NativeArray<float> m_ValueOutputWeights;
        internal NativeArray<AnimationFootFeatureSample> m_ValueLeftFootFeatures;
        internal NativeArray<AnimationFootFeatureSample> m_ValueRightFootFeatures;
        internal NativeArray<byte> m_ValueHasFootFeatures;
        internal NativeArray<AnimationPoseAvailability> m_ValueAvailability;
        internal NativeArray<ulong> m_ValueContinuityIdentities;
        internal NativeArray<PoseDiscontinuityNative> m_ValueDiscontinuities;
        internal NativeArray<AnimationPoseNativeInvalidReason> m_ValueInvalidReasons;
        internal int m_BoneCount;
        internal int m_ParameterCount;
        internal int m_PoseValueCount;
        internal int m_ContributionStride;
        internal int m_LeftFootBoneIndex;
        internal int m_RightFootBoneIndex;
        internal ulong m_CompletionIdentity;

        internal bool TryCopyValue(
            int source,
            int destination,
            int operationIndex) =>
            m_ValuePage.TryCopyValue(
                source,
                destination,
                operationIndex);

        internal bool TryCopyParameters(int source, int output) =>
            m_ValuePage.TryCopyParameters(source, output);

        internal bool TryAddUnmaskedContribution(
            float weight,
            int sourceValue,
            int sourceIndex,
            int overlayValue,
            int output,
            bool overlay,
            bool additive) =>
            m_ValuePage.TryAddContribution(
                weight,
                -1,
                sourceValue,
                sourceIndex,
                overlayValue,
                output,
                overlay,
                additive,
                default);

        internal bool TryValidateValueDeep(
            int value,
            out AnimationPoseNativeInvalidReason reason) =>
            m_ValuePage.TryValidateValueDeep(value, out reason);

        internal bool IsInputReady(int value, int operationIndex) =>
            m_ValuePage.IsInputReady(value, operationIndex);

        internal bool TryGetBoneOutputWeight(
            int value,
            int bone,
            out float result) =>
            m_ValuePage.TryGetBoneOutputWeight(value, bone, out result);

        internal int FindContribution(
            int value,
            AnimationPrimitivePoseContribution source) =>
            m_ValuePage.FindContribution(value, in source);

        internal void ClearContributionWeights(
            int value,
            int contribution) =>
            m_ValuePage.ClearContributionWeights(value, contribution);

        internal void SetInvalid(
            int value,
            ulong continuity,
            AnimationPoseNativeInvalidReason reason,
            int operationIndex) =>
            m_ValuePage.SetInvalid(
                value,
                continuity,
                reason,
                operationIndex);

        internal void RecordGraphInvalid(
            AnimationPoseNativeInvalidReason reason,
            int operationIndex) =>
            m_ValuePage.RecordGraphInvalid(reason, operationIndex);

        internal float GetContributionBoneWeight(
            int value,
            int contribution,
            int bone) =>
            m_ValuePage.GetContributionBoneWeight(
                value,
                contribution,
                bone);

        internal void SetContributionBoneWeight(
            int value,
            int contribution,
            int bone,
            float weight) =>
            m_ValuePage.SetContributionBoneWeight(
                value,
                contribution,
                bone,
                weight);

        internal int PoseOffset(int value) => value * m_BoneCount;
        internal int ParameterOffset(int value) => value * m_ParameterCount;
        internal int ContributionOffset(int value) =>
            value * m_ContributionStride;
        internal int ContributionBoneOffset(int value) =>
            value * m_ContributionStride * m_BoneCount;
    }
}
