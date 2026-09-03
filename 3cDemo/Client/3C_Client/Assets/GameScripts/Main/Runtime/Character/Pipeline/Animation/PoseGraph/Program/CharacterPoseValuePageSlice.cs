using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal unsafe struct CharacterPoseValuePageSlice
    {
        const float ScaleEpsilon = 0.000001f;

        [NativeDisableUnsafePtrRestriction] void* m_Poses;
        [NativeDisableUnsafePtrRestriction] void* m_Velocities;
        [NativeDisableUnsafePtrRestriction] void* m_Parameters;
        [NativeDisableUnsafePtrRestriction] void* m_ParameterAvailability;
        [NativeDisableUnsafePtrRestriction] void* m_Contributions;
        [NativeDisableUnsafePtrRestriction] void* m_ContributionWeights;
        [NativeDisableUnsafePtrRestriction] void* m_ContributionCounts;
        [NativeDisableUnsafePtrRestriction] void* m_OutputWeights;
        [NativeDisableUnsafePtrRestriction] void* m_LeftFeet;
        [NativeDisableUnsafePtrRestriction] void* m_RightFeet;
        [NativeDisableUnsafePtrRestriction] void* m_HasFeet;
        [NativeDisableUnsafePtrRestriction] void* m_Availability;
        [NativeDisableUnsafePtrRestriction] void* m_Continuity;
        [NativeDisableUnsafePtrRestriction] void* m_Discontinuities;
        [NativeDisableUnsafePtrRestriction] void* m_InvalidReasons;
        [NativeDisableUnsafePtrRestriction] void* m_Completions;
        [NativeDisableUnsafePtrRestriction] void* m_GraphInvalidReason;
        [NativeDisableUnsafePtrRestriction] void* m_GraphInvalidOperation;
        [NativeDisableUnsafePtrRestriction] void* m_ValueProducers;

        int m_PoseValueCount;
        int m_BoneCount;
        int m_ParameterCount;
        int m_ContributionStride;
        int m_OperationCount;
        int m_LeftFootBoneIndex;
        int m_RightFootBoneIndex;
        ulong m_CompletionIdentity;
        byte m_RecordGraphInvalid;

        internal static CharacterPoseValuePageSlice Create(
            in CharacterPoseGraphNativeBinding frame,
            NativeArray<int> valueProducers,
            int leftFootBoneIndex,
            int rightFootBoneIndex,
            bool recordGraphInvalid)
        {
            frame.RequireValid();
            if (!valueProducers.IsCreated ||
                valueProducers.Length != frame.Layout.PoseValueWorkspaceCount ||
                leftFootBoneIndex < 0 ||
                leftFootBoneIndex >= frame.Layout.BoneCount ||
                rightFootBoneIndex < 0 ||
                rightFootBoneIndex >= frame.Layout.BoneCount)
            {
                throw new System.ArgumentException(
                    "Pose Value Page slice layout is invalid.");
            }
            return new CharacterPoseValuePageSlice
            {
                m_Poses = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueDenseLocalPoses),
                m_Velocities = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueDenseVelocities),
                m_Parameters = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValuePoseParameters),
                m_ParameterAvailability = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValuePoseParameterAvailability),
                m_Contributions = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueContributions),
                m_ContributionWeights = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueDenseContributionWeights),
                m_ContributionCounts = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueContributionCounts),
                m_OutputWeights = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueOutputWeights),
                m_LeftFeet = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueLeftFootFeatures),
                m_RightFeet = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueRightFootFeatures),
                m_HasFeet = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueHasFootFeatures),
                m_Availability = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueAvailability),
                m_Continuity = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueContinuityIdentities),
                m_Discontinuities = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueDiscontinuities),
                m_InvalidReasons = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.ValueInvalidReasons),
                m_Completions = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.OperationCompletions.Entries),
                m_GraphInvalidReason = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.PoseGraphInvalidReason),
                m_GraphInvalidOperation = NativeArrayUnsafeUtility.GetUnsafePtr(
                    frame.PoseGraphInvalidOperationIndex),
                m_ValueProducers = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(
                    valueProducers),
                m_PoseValueCount = frame.Layout.PoseValueWorkspaceCount,
                m_BoneCount = frame.Layout.BoneCount,
                m_ParameterCount = frame.Layout.ParameterCount,
                m_ContributionStride = frame.Layout.PoseValueContributionStride,
                m_OperationCount = frame.Layout.OperationCount,
                m_LeftFootBoneIndex = leftFootBoneIndex,
                m_RightFootBoneIndex = rightFootBoneIndex,
                m_CompletionIdentity = frame.CompletionIdentity,
                m_RecordGraphInvalid = recordGraphInvalid ? (byte)1 : (byte)0
            };
        }

        internal bool IsValid =>
            m_Poses != null && m_Velocities != null &&
            m_Parameters != null && m_ParameterAvailability != null &&
            m_Contributions != null && m_ContributionWeights != null &&
            m_ContributionCounts != null && m_OutputWeights != null &&
            m_LeftFeet != null && m_RightFeet != null && m_HasFeet != null &&
            m_Availability != null && m_Continuity != null &&
            m_Discontinuities != null && m_InvalidReasons != null &&
            m_Completions != null && m_GraphInvalidReason != null &&
            m_GraphInvalidOperation != null && m_ValueProducers != null &&
            m_PoseValueCount > 0 && m_BoneCount > 0 &&
            m_ParameterCount > 0 && m_ContributionStride > 0 &&
            m_OperationCount > 0 && m_CompletionIdentity != 0;
        internal int BoneCount => m_BoneCount;
        internal int ParameterCount => m_ParameterCount;
        internal int ContributionStride => m_ContributionStride;
        internal int LeftFootBoneIndex => m_LeftFootBoneIndex;
        internal int RightFootBoneIndex => m_RightFootBoneIndex;
        internal ulong CompletionIdentity => m_CompletionIdentity;
        internal ulong WritePageIdentity => (ulong)m_Poses;

        internal AnimationPoseAvailability Availability(int value) =>
            Read<AnimationPoseAvailability>(m_Availability, ValueIndex(value));
        internal float OutputWeight(int value) =>
            Read<float>(m_OutputWeights, ValueIndex(value));
        internal ulong Continuity(int value) =>
            Read<ulong>(m_Continuity, ValueIndex(value));
        internal AnimationPoseNativeInvalidReason InvalidReason(int value) =>
            Read<AnimationPoseNativeInvalidReason>(
                m_InvalidReasons,
                ValueIndex(value));
        internal AnimationLocalBonePose Pose(int value, int bone) =>
            Read<AnimationLocalBonePose>(m_Poses, PoseOffset(value) + bone);
        internal AnimationBlendBoneVelocity Velocity(int value, int bone) =>
            Read<AnimationBlendBoneVelocity>(
                m_Velocities,
                PoseOffset(value) + bone);
        internal float Parameter(int value, int parameter) =>
            Read<float>(m_Parameters, ParameterOffset(value) + parameter);
        internal byte ParameterAvailable(int value, int parameter) =>
            Read<byte>(
                m_ParameterAvailability,
                ParameterOffset(value) + parameter);
        internal int ContributionCount(int value) =>
            Read<int>(m_ContributionCounts, ValueIndex(value));
        internal AnimationPrimitivePoseContribution Contribution(
            int value,
            int contribution) =>
            Read<AnimationPrimitivePoseContribution>(
                m_Contributions,
                ContributionOffset(value) + contribution);
        internal AnimationFootFeatureSample LeftFoot(int value) =>
            Read<AnimationFootFeatureSample>(m_LeftFeet, ValueIndex(value));
        internal AnimationFootFeatureSample RightFoot(int value) =>
            Read<AnimationFootFeatureSample>(m_RightFeet, ValueIndex(value));
        internal byte HasFootFeatures(int value) =>
            Read<byte>(m_HasFeet, ValueIndex(value));
        internal PoseDiscontinuityNative Discontinuity(int value) =>
            Read<PoseDiscontinuityNative>(m_Discontinuities, ValueIndex(value));

        internal void SetPose(
            int value,
            int bone,
            in AnimationLocalBonePose pose) =>
            Write(m_Poses, PoseOffset(value) + bone, in pose);
        internal void SetVelocity(
            int value,
            int bone,
            in AnimationBlendBoneVelocity velocity) =>
            Write(m_Velocities, PoseOffset(value) + bone, in velocity);
        internal void SetAvailability(
            int value,
            AnimationPoseAvailability availability) =>
            Write(m_Availability, ValueIndex(value), in availability);
        internal void SetOutputWeight(int value, float weight) =>
            Write(m_OutputWeights, ValueIndex(value), in weight);
        internal void SetContinuity(int value, ulong continuity) =>
            Write(m_Continuity, ValueIndex(value), in continuity);
        internal void SetDiscontinuity(
            int value,
            in PoseDiscontinuityNative discontinuity) =>
            Write(
                m_Discontinuities,
                ValueIndex(value),
                in discontinuity);
        internal void SetInvalidReason(
            int value,
            AnimationPoseNativeInvalidReason reason) =>
            Write(m_InvalidReasons, ValueIndex(value), in reason);
        internal void SetContributionCount(int value, int count) =>
            Write(m_ContributionCounts, ValueIndex(value), in count);
        internal void SetContribution(
            int value,
            int contribution,
            in AnimationPrimitivePoseContribution entry) =>
            Write(
                m_Contributions,
                ContributionOffset(value) + contribution,
                in entry);
        internal void SetParameter(
            int value,
            int parameter,
            float result,
            byte available)
        {
            int offset = ParameterOffset(value) + parameter;
            Write(m_Parameters, offset, in result);
            Write(m_ParameterAvailability, offset, in available);
        }

        internal void SetFootFeatures(
            int value,
            in AnimationFootFeatureSample left,
            in AnimationFootFeatureSample right,
            byte hasFeatures)
        {
            int index = ValueIndex(value);
            Write(m_LeftFeet, index, in left);
            Write(m_RightFeet, index, in right);
            Write(m_HasFeet, index, in hasFeatures);
        }

        internal bool IsInputReady(int value, int operationIndex)
        {
            if ((uint)value >= (uint)m_PoseValueCount)
                return false;
            int producer = Read<int>(m_ValueProducers, value);
            return producer >= 0 && producer < operationIndex &&
                   Read<CharacterPoseOperationCompletion>(
                       m_Completions,
                       producer).Matches(m_CompletionIdentity);
        }

        internal bool HasCompletion(int operationIndex) =>
            !Read<CharacterPoseOperationCompletion>(
                m_Completions,
                operationIndex).IsEmpty;

        internal bool HasGraphFailure =>
            Read<AnimationPoseNativeInvalidReason>(m_GraphInvalidReason, 0) !=
            AnimationPoseNativeInvalidReason.None;

        internal bool TryComplete(
            int operationIndex,
            CharacterPoseOperationOutcome outcome)
        {
            if ((uint)operationIndex >= (uint)m_OperationCount ||
                HasCompletion(operationIndex))
            {
                RecordGraphInvalid(
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operationIndex);
                return false;
            }
            var completion = new CharacterPoseOperationCompletion(
                m_CompletionIdentity,
                outcome);
            Write(m_Completions, operationIndex, in completion);
            return true;
        }

        internal bool TryRequireInputs(
            in CharacterPoseNativeOperationHeader header,
            int output,
            int inputA,
            int inputB)
        {
            if (!IsInputReady(inputA, header.Index) ||
                !IsInputReady(inputB, header.Index))
            {
                SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return false;
            }
            AnimationPoseAvailability availabilityA = Availability(inputA);
            AnimationPoseAvailability availabilityB = Availability(inputB);
            if (!CharacterPosePureMath.IsAvailability(availabilityA) ||
                !CharacterPosePureMath.IsAvailability(availabilityB))
            {
                SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return false;
            }
            if (availabilityA == AnimationPoseAvailability.Invalid ||
                availabilityB == AnimationPoseAvailability.Invalid)
            {
                AnimationPoseNativeInvalidReason reason =
                    availabilityA == AnimationPoseAvailability.Invalid
                        ? CharacterPosePureMath.NormalizeInvalidReason(
                            InvalidReason(inputA))
                        : CharacterPosePureMath.NormalizeInvalidReason(
                            InvalidReason(inputB));
                SetInvalid(
                    output,
                    CharacterPosePureMath.CombineContinuity(
                        Continuity(inputA),
                        Continuity(inputB),
                        output),
                    reason,
                    header.Index);
                return false;
            }
            return true;
        }

        internal void ResetValue(
            int value,
            NativeArray<float> parameterDefaults)
        {
            int index = ValueIndex(value);
            int zero = 0;
            float zeroWeight = 0f;
            byte zeroByte = 0;
            ulong identity = 1;
            AnimationFootFeatureSample foot = default;
            AnimationPoseAvailability availability =
                AnimationPoseAvailability.Invalid;
            PoseDiscontinuityNative discontinuity = default;
            AnimationPoseNativeInvalidReason reason =
                AnimationPoseNativeInvalidReason.None;
            Write(m_ContributionCounts, index, in zero);
            Write(m_OutputWeights, index, in zeroWeight);
            Write(m_LeftFeet, index, in foot);
            Write(m_RightFeet, index, in foot);
            Write(m_HasFeet, index, in zeroByte);
            Write(m_Availability, index, in availability);
            Write(m_Continuity, index, in identity);
            Write(m_Discontinuities, index, in discontinuity);
            Write(m_InvalidReasons, index, in reason);
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
                SetParameter(value, parameter, parameterDefaults[parameter], 0);
        }

        internal void SetInvalid(
            int value,
            ulong continuity,
            AnimationPoseNativeInvalidReason reason,
            int operationIndex)
        {
            reason = CharacterPosePureMath.NormalizeInvalidReason(reason);
            int index = ValueIndex(value);
            int zero = 0;
            float zeroWeight = 0f;
            byte zeroByte = 0;
            AnimationFootFeatureSample foot = default;
            AnimationPoseAvailability availability =
                AnimationPoseAvailability.Invalid;
            continuity = CharacterPosePureMath.RequireIdentity(continuity);
            PoseDiscontinuityNative discontinuity = default;
            Write(m_ContributionCounts, index, in zero);
            Write(m_OutputWeights, index, in zeroWeight);
            Write(m_LeftFeet, index, in foot);
            Write(m_RightFeet, index, in foot);
            Write(m_HasFeet, index, in zeroByte);
            Write(m_Availability, index, in availability);
            Write(m_Continuity, index, in continuity);
            Write(m_Discontinuities, index, in discontinuity);
            Write(m_InvalidReasons, index, in reason);
            RecordGraphInvalid(reason, operationIndex);
        }

        internal void RecordGraphInvalid(
            AnimationPoseNativeInvalidReason reason,
            int operationIndex)
        {
            if (m_RecordGraphInvalid == 0 || HasGraphFailure)
                return;
            reason = CharacterPosePureMath.NormalizeInvalidReason(reason);
            Write(m_GraphInvalidReason, 0, in reason);
            Write(m_GraphInvalidOperation, 0, in operationIndex);
        }

        internal bool TryCopyValue(
            int source,
            int destination,
            int operationIndex,
            bool copyPose = true)
        {
            if ((uint)source >= (uint)m_PoseValueCount ||
                (uint)destination >= (uint)m_PoseValueCount)
                return false;
            int count = ContributionCount(source);
            if (count < 0 || count > m_ContributionStride)
                return false;
            SetAvailability(destination, Availability(source));
            SetOutputWeight(destination, OutputWeight(source));
            SetContinuity(
                destination,
                CharacterPosePureMath.CombineContinuity(
                    Continuity(source),
                    (ulong)operationIndex + 1UL,
                    operationIndex));
            PoseDiscontinuityNative discontinuity = default;
            Write(m_Discontinuities, ValueIndex(destination), in discontinuity);
            SetInvalidReason(destination, InvalidReason(source));
            if (Availability(source) == AnimationPoseAvailability.Pose)
            {
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    if (copyPose)
                    {
                        AnimationLocalBonePose pose = Pose(source, bone);
                        SetPose(destination, bone, in pose);
                    }
                    AnimationBlendBoneVelocity velocity =
                        Velocity(source, bone);
                    SetVelocity(destination, bone, in velocity);
                }
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                SetParameter(
                    destination,
                    parameter,
                    Parameter(source, parameter),
                    ParameterAvailable(source, parameter));
            }
            SetContributionCount(destination, count);
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution value =
                    Contribution(source, contribution);
                Write(
                    m_Contributions,
                    ContributionOffset(destination) + contribution,
                    in value);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    SetContributionBoneWeight(
                        destination,
                        contribution,
                        bone,
                        GetContributionBoneWeight(
                            source,
                            contribution,
                            bone));
                }
            }
            AnimationFootFeatureSample left = LeftFoot(source);
            AnimationFootFeatureSample right = RightFoot(source);
            SetFootFeatures(
                destination,
                in left,
                in right,
                HasFootFeatures(source));
            return true;
        }

        internal bool TryCopyParameters(int source, int output)
        {
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = Parameter(source, parameter);
                if (!float.IsFinite(value))
                    return false;
                SetParameter(
                    output,
                    parameter,
                    value,
                    ParameterAvailable(source, parameter));
            }
            PoseDiscontinuityNative discontinuity = Discontinuity(source);
            Write(m_Discontinuities, ValueIndex(output), in discontinuity);
            return true;
        }

        internal bool TryResolveParameters(
            float weight,
            int policyOffset,
            int baseValue,
            int overlayValue,
            int output,
            NativeArray<PoseParameterResolvePolicy> policies,
            NativeArray<float> defaults)
        {
            float baseWeight = OutputWeight(baseValue);
            float overlayWeight = OutputWeight(overlayValue) * weight;
            if (!CharacterPosePureMath.IsWeight(baseWeight) ||
                !CharacterPosePureMath.IsWeight(overlayWeight))
                return false;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float baseParameter = Parameter(baseValue, parameter);
                float overlayParameter = Parameter(overlayValue, parameter);
                if (!float.IsFinite(baseParameter) ||
                    !float.IsFinite(overlayParameter))
                    return false;
                bool baseAvailable = ParameterAvailable(baseValue, parameter) != 0;
                bool overlayAvailable =
                    ParameterAvailable(overlayValue, parameter) != 0;
                bool available;
                float value;
                switch (policies[policyOffset + parameter])
                {
                    case PoseParameterResolvePolicy.Base:
                        available = baseAvailable;
                        value = available ? baseParameter : defaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Overlay:
                        available = overlayWeight > 0f && overlayAvailable ||
                                    baseAvailable;
                        value = overlayWeight > 0f && overlayAvailable
                            ? overlayParameter
                            : baseAvailable ? baseParameter : defaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Weighted:
                        float resolvedBaseWeight = baseAvailable ? baseWeight : 0f;
                        float resolvedOverlayWeight = overlayAvailable
                            ? overlayWeight
                            : 0f;
                        float total = resolvedBaseWeight + resolvedOverlayWeight;
                        available = total > 0f;
                        value = total > 0f
                            ? (baseParameter * resolvedBaseWeight +
                               overlayParameter * resolvedOverlayWeight) / total
                            : defaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Max:
                        available = baseAvailable || overlayAvailable;
                        value = baseAvailable && overlayAvailable
                            ? Mathf.Max(baseParameter, overlayParameter)
                            : baseAvailable
                                ? baseParameter
                                : overlayAvailable
                                    ? overlayParameter
                                    : defaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Min:
                        available = baseAvailable || overlayAvailable;
                        value = baseAvailable && overlayAvailable
                            ? Mathf.Min(baseParameter, overlayParameter)
                            : baseAvailable
                                ? baseParameter
                                : overlayAvailable
                                    ? overlayParameter
                                    : defaults[parameter];
                        break;
                    default:
                        return false;
                }
                if (!float.IsFinite(value))
                    return false;
                SetParameter(
                    output,
                    parameter,
                    value,
                    available ? (byte)1 : (byte)0);
            }
            return true;
        }

        internal bool TryScaleValue(
            int value,
            float weight,
            int maskOffset,
            NativeArray<float> masks)
        {
            if (Availability(value) != AnimationPoseAvailability.Pose)
                return true;
            float outputWeight = OutputWeight(value) * weight;
            if (!CharacterPosePureMath.IsWeight(outputWeight))
                return false;
            SetOutputWeight(value, outputWeight);
            int count = ContributionCount(value);
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution source =
                    Contribution(value, contribution);
                float scalarWeight = source.Weight * weight;
                float leftWeight = source.LeftFootWeight *
                    MaskWeight(masks, maskOffset, m_LeftFootBoneIndex) * weight;
                float rightWeight = source.RightFootWeight *
                    MaskWeight(masks, maskOffset, m_RightFootBoneIndex) * weight;
                if (!CharacterPosePureMath.IsWeight(scalarWeight) ||
                    !CharacterPosePureMath.IsWeight(leftWeight) ||
                    !CharacterPosePureMath.IsWeight(rightWeight))
                    return false;
                AnimationPrimitivePoseContribution scaled =
                    CharacterPosePureMath.CopyContribution(
                        in source,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
                Write(
                    m_Contributions,
                    ContributionOffset(value) + contribution,
                    in scaled);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float boneWeight = GetContributionBoneWeight(
                        value,
                        contribution,
                        bone) * MaskWeight(masks, maskOffset, bone) * weight;
                    if (!CharacterPosePureMath.IsWeight(boneWeight))
                        return false;
                    SetContributionBoneWeight(
                        value,
                        contribution,
                        bone,
                        boneWeight);
                }
            }
            return true;
        }

        internal bool TryMergeContributions(
            float weight,
            int maskOffset,
            int baseValue,
            int overlayValue,
            int output,
            bool additive,
            NativeArray<float> masks)
        {
            for (int contribution = 0;
                 contribution < ContributionCount(baseValue);
                 contribution++)
            {
                if (!TryAddContribution(
                        weight,
                        maskOffset,
                        baseValue,
                        contribution,
                        overlayValue,
                        output,
                        false,
                        additive,
                        masks))
                    return false;
            }
            for (int contribution = 0;
                 contribution < ContributionCount(overlayValue);
                 contribution++)
            {
                if (!TryAddContribution(
                        weight,
                        maskOffset,
                        overlayValue,
                        contribution,
                        overlayValue,
                        output,
                        true,
                        additive,
                        masks))
                    return false;
            }
            return true;
        }

        internal bool TryAddContribution(
            float weight,
            int maskOffset,
            int sourceValue,
            int sourceIndex,
            int overlayValue,
            int output,
            bool overlay,
            bool additive,
            NativeArray<float> masks)
        {
            AnimationPrimitivePoseContribution source =
                Contribution(sourceValue, sourceIndex);
            if (!CharacterPosePureMath.IsValidPrimitiveContribution(in source))
                return false;
            float scalarFactor;
            float leftFactor;
            float rightFactor;
            if (overlay)
            {
                scalarFactor = weight;
                leftFactor = MaskWeight(
                    masks,
                    maskOffset,
                    m_LeftFootBoneIndex) * weight;
                rightFactor = MaskWeight(
                    masks,
                    maskOffset,
                    m_RightFootBoneIndex) * weight;
            }
            else if (additive)
            {
                scalarFactor = 1f;
                leftFactor = 1f;
                rightFactor = 1f;
            }
            else
            {
                if (!TryGetBoneOutputWeight(
                        overlayValue,
                        m_LeftFootBoneIndex,
                        out float leftOverlay) ||
                    !TryGetBoneOutputWeight(
                        overlayValue,
                        m_RightFootBoneIndex,
                        out float rightOverlay))
                    return false;
                scalarFactor = 1f - OutputWeight(overlayValue) * weight;
                leftFactor = 1f - leftOverlay * MaskWeight(
                    masks,
                    maskOffset,
                    m_LeftFootBoneIndex) * weight;
                rightFactor = 1f - rightOverlay * MaskWeight(
                    masks,
                    maskOffset,
                    m_RightFootBoneIndex) * weight;
            }
            float scalarWeight = source.Weight * Mathf.Clamp01(scalarFactor);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(leftFactor);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(rightFactor);
            if (!CharacterPosePureMath.IsWeight(scalarWeight) ||
                !CharacterPosePureMath.IsWeight(leftWeight) ||
                !CharacterPosePureMath.IsWeight(rightWeight))
                return false;
            int target = FindContribution(output, in source);
            if (target < 0)
            {
                target = ContributionCount(output);
                if (target >= m_ContributionStride)
                    return false;
                SetContributionCount(output, target + 1);
                ClearContributionWeights(output, target);
                AnimationPrimitivePoseContribution added =
                    CharacterPosePureMath.CopyContribution(
                        in source,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
                Write(
                    m_Contributions,
                    ContributionOffset(output) + target,
                    in added);
            }
            else
            {
                AnimationPrimitivePoseContribution current =
                    Contribution(output, target);
                AnimationPrimitivePoseContribution combined =
                    CharacterPosePureMath.CopyContribution(
                        in current,
                        Mathf.Clamp01(current.Weight + scalarWeight),
                        Mathf.Clamp01(current.LeftFootWeight + leftWeight),
                        Mathf.Clamp01(current.RightFootWeight + rightWeight));
                Write(
                    m_Contributions,
                    ContributionOffset(output) + target,
                    in combined);
            }
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float factor;
                if (overlay)
                {
                    factor = MaskWeight(masks, maskOffset, bone) * weight;
                }
                else if (additive)
                {
                    factor = 1f;
                }
                else
                {
                    if (!TryGetBoneOutputWeight(
                            overlayValue,
                            bone,
                            out float overlayOutput))
                        return false;
                    factor = 1f - overlayOutput *
                        MaskWeight(masks, maskOffset, bone) * weight;
                }
                float boneWeight = GetContributionBoneWeight(
                    sourceValue,
                    sourceIndex,
                    bone) * Mathf.Clamp01(factor);
                float combined = Mathf.Clamp01(
                    GetContributionBoneWeight(output, target, bone) +
                    boneWeight);
                if (!CharacterPosePureMath.IsWeight(combined))
                    return false;
                SetContributionBoneWeight(
                    output,
                    target,
                    bone,
                    combined);
            }
            return true;
        }

        internal bool TryResolveFootFeatures(
            float weight,
            int maskOffset,
            int baseValue,
            int overlayValue,
            int output,
            bool additive,
            NativeArray<float> masks)
        {
            bool hasBase = HasFootFeatures(baseValue) == 1;
            bool hasOverlay = HasFootFeatures(overlayValue) == 1;
            if (!hasBase && !hasOverlay)
                return true;
            if (!TryGetBoneOutputWeight(
                    overlayValue,
                    m_LeftFootBoneIndex,
                    out float leftOutput) ||
                !TryGetBoneOutputWeight(
                    overlayValue,
                    m_RightFootBoneIndex,
                    out float rightOutput))
                return false;
            float left = leftOutput * MaskWeight(
                masks,
                maskOffset,
                m_LeftFootBoneIndex) * weight;
            float right = rightOutput * MaskWeight(
                masks,
                maskOffset,
                m_RightFootBoneIndex) * weight;
            if (additive)
            {
                left /= 1f + left;
                right /= 1f + right;
            }
            AnimationFootFeatureSample baseLeft = LeftFoot(baseValue);
            AnimationFootFeatureSample overlayLeft = LeftFoot(overlayValue);
            AnimationFootFeatureSample baseRight = RightFoot(baseValue);
            AnimationFootFeatureSample overlayRight = RightFoot(overlayValue);
            if (!CharacterPosePureMath.TryResolveFeature(
                    hasBase,
                    in baseLeft,
                    hasOverlay,
                    in overlayLeft,
                    left,
                    hasOverlay && left > 0f,
                    out AnimationFootFeatureSample resolvedLeft) ||
                !CharacterPosePureMath.TryResolveFeature(
                    hasBase,
                    in baseRight,
                    hasOverlay,
                    in overlayRight,
                    right,
                    hasOverlay && right > 0f,
                    out AnimationFootFeatureSample resolvedRight))
                return false;
            SetFootFeatures(
                output,
                in resolvedLeft,
                in resolvedRight,
                resolvedLeft.IsValid && resolvedRight.IsValid
                    ? (byte)1
                    : (byte)0);
            return true;
        }

        internal bool TryValidateValueEnvelope(
            int value,
            out AnimationPoseNativeInvalidReason reason)
        {
            reason = AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
            AnimationPoseAvailability availability = Availability(value);
            AnimationPoseNativeInvalidReason invalidReason = InvalidReason(value);
            int contributionCount = ContributionCount(value);
            byte hasFeet = HasFootFeatures(value);
            if (!CharacterPosePureMath.IsAvailability(availability) ||
                !CharacterPosePureMath.IsWeight(OutputWeight(value)) ||
                Continuity(value) == 0 || contributionCount < 0 ||
                contributionCount > m_ContributionStride || hasFeet > 1)
                return false;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                if (!float.IsFinite(Parameter(value, parameter)) ||
                    ParameterAvailable(value, parameter) > 1)
                    return false;
            }
            if (availability == AnimationPoseAvailability.Invalid)
            {
                reason = CharacterPosePureMath.NormalizeInvalidReason(
                    invalidReason);
                return invalidReason != AnimationPoseNativeInvalidReason.None &&
                       contributionCount == 0 && OutputWeight(value) == 0f &&
                       hasFeet == 0;
            }
            if (invalidReason != AnimationPoseNativeInvalidReason.None)
                return false;
            if (availability == AnimationPoseAvailability.NoPose)
            {
                return contributionCount == 0 && OutputWeight(value) == 0f &&
                       hasFeet == 0;
            }
            if (contributionCount <= 0)
                return false;
            if (hasFeet == 1 &&
                (!CharacterPosePureMath.IsValidFootFeature(LeftFoot(value)) ||
                 !CharacterPosePureMath.IsValidFootFeature(RightFoot(value))))
                return false;
            reason = AnimationPoseNativeInvalidReason.None;
            return true;
        }

        internal bool TryValidateValueDeep(
            int value,
            out AnimationPoseNativeInvalidReason reason)
        {
            if (!TryValidateValueEnvelope(value, out reason))
                return false;
            if (Availability(value) != AnimationPoseAvailability.Pose)
                return true;
            int count = ContributionCount(value);
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!Pose(value, bone).IsValid)
                    return false;
            }
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution valueContribution =
                    Contribution(value, contribution);
                if (!CharacterPosePureMath.IsValidPrimitiveContribution(
                        in valueContribution))
                    return false;
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    if (!CharacterPosePureMath.IsWeight(
                            GetContributionBoneWeight(
                                value,
                                contribution,
                                bone)))
                        return false;
                }
            }
            reason = AnimationPoseNativeInvalidReason.None;
            return true;
        }

        internal bool TryGetBoneOutputWeight(
            int value,
            int bone,
            out float result)
        {
            result = 0f;
            int count = ContributionCount(value);
            if (count < 0 || count > m_ContributionStride ||
                (uint)bone >= (uint)m_BoneCount)
                return false;
            for (int contribution = 0; contribution < count; contribution++)
            {
                float weight = GetContributionBoneWeight(
                    value,
                    contribution,
                    bone);
                if (!CharacterPosePureMath.IsWeight(weight))
                    return false;
                result += weight;
                if (!float.IsFinite(result))
                    return false;
            }
            result = Mathf.Clamp01(result);
            return true;
        }

        internal float GetContributionBoneWeight(
            int value,
            int contribution,
            int bone) =>
            Read<float>(
                m_ContributionWeights,
                ContributionBoneOffset(value) +
                contribution * m_BoneCount + bone);

        internal void SetContributionBoneWeight(
            int value,
            int contribution,
            int bone,
            float weight) =>
            Write(
                m_ContributionWeights,
                ContributionBoneOffset(value) +
                contribution * m_BoneCount + bone,
                in weight);

        internal void ClearContributionWeights(int value, int contribution)
        {
            for (int bone = 0; bone < m_BoneCount; bone++)
                SetContributionBoneWeight(value, contribution, bone, 0f);
        }

        internal int FindContribution(
            int value,
            in AnimationPrimitivePoseContribution source)
        {
            int count = ContributionCount(value);
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution candidate =
                    Contribution(value, contribution);
                if (candidate.PhysicalPlayerIndex == source.PhysicalPlayerIndex &&
                    candidate.PhysicalSourceIndex == source.PhysicalSourceIndex &&
                    candidate.PhysicalSourceGeneration ==
                    source.PhysicalSourceGeneration &&
                    candidate.Kind == source.Kind &&
                    candidate.SourceOwnerIndex == source.SourceOwnerIndex &&
                    candidate.ContributionContinuityIdentity ==
                    source.ContributionContinuityIdentity)
                    return contribution;
            }
            return -1;
        }

        internal static float MaskWeight(
            NativeArray<float> masks,
            int maskOffset,
            int bone) =>
            maskOffset < 0 ? 1f : masks[maskOffset + bone];

        internal bool TryAddMeshPose(
            int baseValue,
            int additiveValue,
            int outputValue,
            in CharacterPoseNativeCompositionOperation operation,
            int bone,
            float weight,
            NativeArray<AnimationLocalBonePose> additiveReferences,
            NativeArray<int> parentIndices,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!TryResolveModelPose(
                    baseValue,
                    bone,
                    parentIndices,
                    out AnimationLocalBonePose basePose) ||
                !TryResolveModelPose(
                    additiveValue,
                    bone,
                    parentIndices,
                    out AnimationLocalBonePose additivePose))
                return false;
            AnimationLocalBonePose reference =
                additiveReferences[operation.AdditiveReferenceOffset + bone];
            if (!CharacterPosePureMath.TryAddPose(
                    in basePose,
                    in additivePose,
                    in reference,
                    operation.AdditiveScalePolicy,
                    weight,
                    out AnimationLocalBonePose modelResult))
                return false;
            int parentIndex = parentIndices[bone];
            if (parentIndex < 0)
            {
                result = modelResult;
                return true;
            }
            return TryResolveModelPose(
                       outputValue,
                       parentIndex,
                       parentIndices,
                       out AnimationLocalBonePose outputParent) &&
                   CharacterPosePureMath.TryToLocal(
                       in outputParent,
                       in modelResult,
                       out result);
        }

        internal bool TryResolveModelPose(
            int value,
            int bone,
            NativeArray<int> parentIndices,
            out AnimationLocalBonePose result)
        {
            result = Pose(value, bone);
            if (!result.IsValid)
                return false;
            int parentIndex = parentIndices[bone];
            while (parentIndex >= 0)
            {
                AnimationLocalBonePose parent = Pose(value, parentIndex);
                if (!CharacterPosePureMath.TryToModel(
                        in parent,
                        in result,
                        out result))
                    return false;
                parentIndex = parentIndices[parentIndex];
            }
            return true;
        }

        int ValueIndex(int value) => value;
        int PoseOffset(int value) => value * m_BoneCount;
        int ParameterOffset(int value) => value * m_ParameterCount;
        int ContributionOffset(int value) => value * m_ContributionStride;
        int ContributionBoneOffset(int value) =>
            value * m_ContributionStride * m_BoneCount;

        static T Read<T>(void* pointer, int index) where T : struct =>
            UnsafeUtility.ReadArrayElement<T>(pointer, index);

        static void Write<T>(void* pointer, int index, in T value)
            where T : struct =>
            UnsafeUtility.WriteArrayElement(pointer, index, value);
    }
}
