using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal struct CharacterPoseWorkerKernelRange
    {
        internal int OperationStart;
        internal int OperationCount;

        internal CharacterPoseNativeOperationHeader Header(
            NativeArray<CharacterPoseNativeOperationHeader> headers,
            NativeArray<int> nativeOperationIndices,
            int index) =>
            headers[nativeOperationIndices[OperationStart + index]];

        internal int NativeOperationIndex(
            NativeArray<int> nativeOperationIndices,
            int index) =>
            nativeOperationIndices[OperationStart + index];
    }

    internal readonly struct CharacterPoseWorkerKernelFrame
    {
        readonly CharacterPoseWorkerActorSlice m_Actor;
        readonly NativeArray<float> m_ParameterDefaults;

        internal CharacterPoseWorkerKernelFrame(
            in CharacterPoseWorkerActorSlice actor,
            NativeArray<float> parameterDefaults)
        {
            m_Actor = actor;
            m_ParameterDefaults = parameterDefaults;
        }

        internal CharacterPoseValuePageSlice Values => m_Actor.Values;

        internal bool TryBegin(
            int nativeOperationIndex,
            in CharacterPoseNativeOperationHeader source,
            out CharacterPoseNativeOperationHeader operation)
        {
            CharacterPoseValuePageSlice values = m_Actor.Values;
            operation = m_Actor.ApplyWeight(
                in source,
                nativeOperationIndex);
            if (values.HasGraphFailure || values.HasCompletion(source.Index))
            {
                if (values.HasCompletion(source.Index))
                {
                    values.RecordGraphInvalid(
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        source.Index);
                }
                return false;
            }
            if (source.OutputPoseValueIndex < 0)
            {
                values.RecordGraphInvalid(
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    source.Index);
                return false;
            }
            values.ResetValue(source.OutputPoseValueIndex, m_ParameterDefaults);
            if (m_Actor.IsFragmentActive(source.LinkedPoseFragmentIndex))
                return true;
            values.TryComplete(
                source.Index,
                CharacterPoseOperationOutcome.Skipped);
            return false;
        }

        internal bool Complete(
            in CharacterPoseNativeOperationHeader operation)
        {
            CharacterPoseValuePageSlice values = m_Actor.Values;
            bool valid = values.TryValidateValueEnvelope(
                operation.OutputPoseValueIndex,
                out AnimationPoseNativeInvalidReason reason);
            if (!valid)
            {
                values.SetInvalid(
                    operation.OutputPoseValueIndex,
                    values.Continuity(operation.OutputPoseValueIndex),
                    reason,
                    operation.Index);
            }
            return values.TryComplete(
                operation.Index,
                valid
                    ? CharacterPoseOperationOutcome.Completed
                    : CharacterPoseOperationOutcome.TypedInvalid) && valid;
        }
    }

    [BurstCompile]
    internal struct CharacterPoseParameterResolveWorkerKernel :
        IJobParallelFor
    {
        internal CharacterPoseWorkerKernelRange Range;
        [ReadOnly] internal NativeArray<CharacterPoseWorkerActorSlice> Actors;
        [ReadOnly] internal NativeArray<int> NativeOperationIndices;
        [ReadOnly] internal NativeArray<CharacterPoseNativeOperationHeader>
            Headers;
        [ReadOnly] internal NativeArray<float> ParameterDefaults;
        [ReadOnly] internal NativeArray<CharacterPoseNativeParameterResolveOperation>
            Operations;
        [ReadOnly] internal NativeArray<PoseParameterResolvePolicy> Policies;

        public void Execute(int actorIndex)
        {
            CharacterPoseWorkerActorSlice actor = Actors[actorIndex];
            var frame = new CharacterPoseWorkerKernelFrame(
                in actor,
                ParameterDefaults);
            for (int index = 0; index < Range.OperationCount; index++)
            {
                int nativeOperationIndex =
                    Range.NativeOperationIndex(
                        NativeOperationIndices,
                        index);
                CharacterPoseNativeOperationHeader source = Range.Header(
                    Headers,
                    NativeOperationIndices,
                    index);
                if (!frame.TryBegin(
                        nativeOperationIndex,
                        in source,
                        out CharacterPoseNativeOperationHeader header))
                    continue;
                CharacterPoseNativeParameterResolveOperation operation =
                    Operations[header.FamilyPayloadIndex];
                CharacterPoseValuePageSlice values = frame.Values;
                int output = operation.OutputPoseValueIndex;
                int baseValue = operation.InputPoseValueIndexA;
                int parameterSource = operation.InputPoseValueIndexB;
                if (values.TryRequireInputs(
                        in header,
                        output,
                        baseValue,
                        parameterSource))
                {
                    if (!values.TryCopyValue(baseValue, output, header.Index))
                    {
                        values.SetInvalid(
                            output,
                            values.Continuity(baseValue),
                            AnimationPoseNativeInvalidReason
                                .PoseGraphOperationInvalid,
                            header.Index);
                    }
                    else if (values.Availability(parameterSource) !=
                             AnimationPoseAvailability.NoPose)
                    {
                        if (!values.TryResolveParameters(
                                header.Weight,
                                operation.ParameterPolicyOffset,
                                baseValue,
                                parameterSource,
                                output,
                                Policies,
                                ParameterDefaults))
                        {
                            values.SetInvalid(
                                output,
                                values.Continuity(output),
                                AnimationPoseNativeInvalidReason
                                    .PoseGraphOperationInvalid,
                                header.Index);
                        }
                        else
                        {
                            values.SetContinuity(
                                output,
                                CharacterPosePureMath.CombineContinuity(
                                    values.Continuity(baseValue),
                                    values.Continuity(parameterSource),
                                    header.Index));
                        }
                    }
                }
                if (!frame.Complete(in header))
                    break;
            }
        }
    }

    [BurstCompile]
    internal struct CharacterPoseBlendWorkerKernel : IJobParallelFor
    {
        internal CharacterPoseWorkerKernelRange Range;
        [ReadOnly] internal NativeArray<CharacterPoseWorkerActorSlice> Actors;
        [ReadOnly] internal NativeArray<int> NativeOperationIndices;
        [ReadOnly] internal NativeArray<CharacterPoseNativeOperationHeader>
            Headers;
        [ReadOnly] internal NativeArray<float> ParameterDefaults;
        [ReadOnly] internal NativeArray<CharacterPoseNativeBlendOperation>
            Operations;

        public void Execute(int actorIndex)
        {
            CharacterPoseWorkerActorSlice actor = Actors[actorIndex];
            var frame = new CharacterPoseWorkerKernelFrame(
                in actor,
                ParameterDefaults);
            for (int index = 0; index < Range.OperationCount; index++)
            {
                int nativeOperationIndex =
                    Range.NativeOperationIndex(
                        NativeOperationIndices,
                        index);
                CharacterPoseNativeOperationHeader source = Range.Header(
                    Headers,
                    NativeOperationIndices,
                    index);
                if (!frame.TryBegin(
                        nativeOperationIndex,
                        in source,
                        out CharacterPoseNativeOperationHeader header))
                    continue;
                CharacterPoseNativeBlendOperation operation =
                    Operations[header.FamilyPayloadIndex];
                CharacterPoseValuePageSlice values = frame.Values;
                float weight = header.Weight;
                if (operation.ParameterIndex >= 0)
                {
                    int input = operation.InputPoseValueIndexA;
                    if (!values.IsInputReady(input, header.Index) ||
                        operation.ParameterIndex >= values.ParameterCount)
                    {
                        values.SetInvalid(
                            operation.OutputPoseValueIndex,
                            (ulong)header.Index + 1UL,
                            AnimationPoseNativeInvalidReason
                                .PoseGraphInputIncomplete,
                            header.Index);
                    }
                    else if (values.ParameterAvailable(
                                 input,
                                 operation.ParameterIndex) == 0)
                    {
                        values.SetInvalid(
                            operation.OutputPoseValueIndex,
                            (ulong)header.Index + 1UL,
                            AnimationPoseNativeInvalidReason
                                .SlotParameterInvalid,
                            header.Index);
                    }
                    else
                    {
                        weight = Mathf.Clamp01(values.Parameter(
                            input,
                            operation.ParameterIndex));
                        CharacterPoseCompositionWorkerKernel.Blend(
                            values,
                            in header,
                            operation.OutputPoseValueIndex,
                            operation.InputPoseValueIndexA,
                            operation.InputPoseValueIndexB,
                            -1,
                            weight,
                            default);
                    }
                }
                else
                {
                    CharacterPoseCompositionWorkerKernel.Blend(
                        values,
                        in header,
                        operation.OutputPoseValueIndex,
                        operation.InputPoseValueIndexA,
                        operation.InputPoseValueIndexB,
                        -1,
                        weight,
                        default);
                }
                if (!frame.Complete(in header))
                    break;
            }
        }
    }

    [BurstCompile]
    internal struct CharacterPoseCompositionWorkerKernel : IJobParallelFor
    {
        internal CharacterPoseWorkerKernelRange Range;
        [ReadOnly] internal NativeArray<CharacterPoseWorkerActorSlice> Actors;
        [ReadOnly] internal NativeArray<int> NativeOperationIndices;
        [ReadOnly] internal NativeArray<CharacterPoseNativeOperationHeader>
            Headers;
        [ReadOnly] internal NativeArray<float> ParameterDefaults;
        [ReadOnly] internal NativeArray<CharacterPoseNativeCompositionOperation>
            Operations;
        [ReadOnly] internal NativeArray<float> BoneMasks;
        [ReadOnly] internal NativeArray<AnimationLocalBonePose>
            AdditiveReferences;
        [ReadOnly] internal NativeArray<int> ParentIndices;

        public void Execute(int actorIndex)
        {
            CharacterPoseWorkerActorSlice actor = Actors[actorIndex];
            var frame = new CharacterPoseWorkerKernelFrame(
                in actor,
                ParameterDefaults);
            for (int index = 0; index < Range.OperationCount; index++)
            {
                int nativeOperationIndex =
                    Range.NativeOperationIndex(
                        NativeOperationIndices,
                        index);
                CharacterPoseNativeOperationHeader source = Range.Header(
                    Headers,
                    NativeOperationIndices,
                    index);
                if (!frame.TryBegin(
                        nativeOperationIndex,
                        in source,
                        out CharacterPoseNativeOperationHeader header))
                    continue;
                CharacterPoseNativeCompositionOperation operation =
                    Operations[header.FamilyPayloadIndex];
                if (header.Code == CharacterPoseOperationCode.LayeredBoneBlend)
                {
                    Blend(
                        frame.Values,
                        in header,
                        operation.OutputPoseValueIndex,
                        operation.InputPoseValueIndexA,
                        operation.InputPoseValueIndexB,
                        operation.BoneMaskOffset,
                        header.Weight,
                        BoneMasks);
                }
                else
                {
                    Additive(
                        frame.Values,
                        in header,
                        in operation,
                        BoneMasks,
                        AdditiveReferences,
                        ParentIndices);
                }
                if (!frame.Complete(in header))
                    break;
            }
        }

        internal static void Blend(
            CharacterPoseValuePageSlice values,
            in CharacterPoseNativeOperationHeader header,
            int output,
            int baseValue,
            int overlayValue,
            int maskOffset,
            float weight,
            NativeArray<float> masks)
        {
            if (!values.TryRequireInputs(
                    in header,
                    output,
                    baseValue,
                    overlayValue))
                return;
            if (values.Availability(overlayValue) ==
                AnimationPoseAvailability.NoPose)
            {
                if (!values.TryCopyValue(baseValue, output, header.Index))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(baseValue),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                }
                return;
            }
            if (values.Availability(baseValue) ==
                AnimationPoseAvailability.NoPose)
            {
                if (!values.TryCopyValue(overlayValue, output, header.Index) ||
                    !values.TryScaleValue(
                        output,
                        weight,
                        maskOffset,
                        masks))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(overlayValue),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                }
                return;
            }
            values.SetAvailability(output, AnimationPoseAvailability.Pose);
            values.SetOutputWeight(
                output,
                CharacterPosePureMath.UnionWeight(
                    values.OutputWeight(baseValue),
                    values.OutputWeight(overlayValue) * weight));
            values.SetContinuity(
                output,
                CharacterPosePureMath.CombineContinuity(
                    values.Continuity(baseValue),
                    values.Continuity(overlayValue),
                    header.Index));
            values.SetInvalidReason(
                output,
                AnimationPoseNativeInvalidReason.None);
            for (int bone = 0; bone < values.BoneCount; bone++)
            {
                if (!values.TryGetBoneOutputWeight(
                        overlayValue,
                        bone,
                        out float overlayOutputWeight))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(output),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
                float overlay = Mathf.Clamp01(
                    overlayOutputWeight * CharacterPoseValuePageSlice.MaskWeight(
                        masks,
                        maskOffset,
                        bone) * weight);
                AnimationLocalBonePose from = values.Pose(baseValue, bone);
                AnimationLocalBonePose to = values.Pose(overlayValue, bone);
                if (!CharacterPosePureMath.TryBlendPose(
                        in from,
                        in to,
                        overlay,
                        out AnimationLocalBonePose pose))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(output),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
                values.SetPose(output, bone, in pose);
            }
            if (!values.TryCopyParameters(baseValue, output) ||
                !values.TryMergeContributions(
                    weight,
                    maskOffset,
                    baseValue,
                    overlayValue,
                    output,
                    false,
                    masks) ||
                !values.TryResolveFootFeatures(
                    weight,
                    maskOffset,
                    baseValue,
                    overlayValue,
                    output,
                    false,
                    masks))
            {
                values.SetInvalid(
                    output,
                    values.Continuity(output),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
            }
        }

        static void Additive(
            CharacterPoseValuePageSlice values,
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeCompositionOperation operation,
            NativeArray<float> masks,
            NativeArray<AnimationLocalBonePose> additiveReferences,
            NativeArray<int> parentIndices)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int additiveValue = operation.InputPoseValueIndexB;
            if (!values.TryRequireInputs(
                    in header,
                    output,
                    baseValue,
                    additiveValue))
                return;
            if (values.Availability(additiveValue) ==
                AnimationPoseAvailability.NoPose)
            {
                if (!values.TryCopyValue(baseValue, output, header.Index))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(baseValue),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                }
                return;
            }
            if (values.Availability(baseValue) != AnimationPoseAvailability.Pose)
            {
                values.SetInvalid(
                    output,
                    CharacterPosePureMath.CombineContinuity(
                        values.Continuity(baseValue),
                        values.Continuity(additiveValue),
                        header.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            values.SetAvailability(output, AnimationPoseAvailability.Pose);
            values.SetOutputWeight(
                output,
                CharacterPosePureMath.UnionWeight(
                    values.OutputWeight(baseValue),
                    values.OutputWeight(additiveValue) * header.Weight));
            values.SetContinuity(
                output,
                CharacterPosePureMath.CombineContinuity(
                    values.Continuity(baseValue),
                    values.Continuity(additiveValue),
                    header.Index));
            values.SetInvalidReason(
                output,
                AnimationPoseNativeInvalidReason.None);
            for (int bone = 0; bone < values.BoneCount; bone++)
            {
                if (!values.TryGetBoneOutputWeight(
                        additiveValue,
                        bone,
                        out float additiveOutputWeight))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(output),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
                float weight = Mathf.Clamp01(
                    additiveOutputWeight * CharacterPoseValuePageSlice.MaskWeight(
                        masks,
                        operation.BoneMaskOffset,
                        bone) * header.Weight);
                bool valid;
                AnimationLocalBonePose pose;
                if (operation.AdditiveReferenceSpace ==
                    AdditiveReferenceSpace.Local)
                {
                    AnimationLocalBonePose basePose = values.Pose(baseValue, bone);
                    AnimationLocalBonePose additivePose =
                        values.Pose(additiveValue, bone);
                    AnimationLocalBonePose reference =
                        additiveReferences[
                            operation.AdditiveReferenceOffset + bone];
                    valid = CharacterPosePureMath.TryAddPose(
                        in basePose,
                        in additivePose,
                        in reference,
                        operation.AdditiveScalePolicy,
                        weight,
                        out pose);
                }
                else
                {
                    valid = values.TryAddMeshPose(
                        baseValue,
                        additiveValue,
                        output,
                        in operation,
                        bone,
                        weight,
                        additiveReferences,
                        parentIndices,
                        out pose);
                }
                if (!valid)
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(output),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
                values.SetPose(output, bone, in pose);
            }
            if (!values.TryCopyParameters(baseValue, output) ||
                !values.TryMergeContributions(
                    header.Weight,
                    operation.BoneMaskOffset,
                    baseValue,
                    additiveValue,
                    output,
                    true,
                    masks) ||
                !values.TryResolveFootFeatures(
                    header.Weight,
                    operation.BoneMaskOffset,
                    baseValue,
                    additiveValue,
                    output,
                    true,
                    masks))
            {
                values.SetInvalid(
                    output,
                    values.Continuity(output),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
            }
        }
    }

    [BurstCompile]
    internal struct CharacterPoseSpaceConversionWorkerKernel : IJobParallelFor
    {
        internal CharacterPoseWorkerKernelRange Range;
        [ReadOnly] internal NativeArray<CharacterPoseWorkerActorSlice> Actors;
        [ReadOnly] internal NativeArray<int> NativeOperationIndices;
        [ReadOnly] internal NativeArray<CharacterPoseNativeOperationHeader>
            Headers;
        [ReadOnly] internal NativeArray<float> ParameterDefaults;
        [ReadOnly] internal NativeArray<CharacterPoseNativeSpaceConversionOperation>
            Operations;
        [ReadOnly] internal NativeArray<int> ParentIndices;

        public void Execute(int actorIndex)
        {
            CharacterPoseWorkerActorSlice actor = Actors[actorIndex];
            var frame = new CharacterPoseWorkerKernelFrame(
                in actor,
                ParameterDefaults);
            for (int index = 0; index < Range.OperationCount; index++)
            {
                int nativeOperationIndex =
                    Range.NativeOperationIndex(
                        NativeOperationIndices,
                        index);
                CharacterPoseNativeOperationHeader source = Range.Header(
                    Headers,
                    NativeOperationIndices,
                    index);
                if (!frame.TryBegin(
                        nativeOperationIndex,
                        in source,
                        out CharacterPoseNativeOperationHeader header))
                    continue;
                CharacterPoseNativeSpaceConversionOperation operation =
                    Operations[header.FamilyPayloadIndex];
                CharacterPoseValuePageSlice values = frame.Values;
                int input = operation.InputPoseValueIndex;
                int output = operation.OutputPoseValueIndex;
                if (!values.IsInputReady(input, header.Index) ||
                    !values.TryCopyValue(
                        input,
                        output,
                        header.Index,
                        false))
                {
                    values.SetInvalid(
                        output,
                        (ulong)header.Index + 1UL,
                        AnimationPoseNativeInvalidReason
                            .PoseGraphInputIncomplete,
                        header.Index);
                }
                else if (values.Availability(output) ==
                         AnimationPoseAvailability.Pose)
                {
                    for (int bone = 0; bone < values.BoneCount; bone++)
                    {
                        int parent = ParentIndices[bone];
                        AnimationLocalBonePose pose = values.Pose(input, bone);
                        bool valid = true;
                        if (parent >= 0)
                        {
                            AnimationLocalBonePose parentPose =
                                header.Code == CharacterPoseOperationCode
                                    .LocalToComponentPose
                                    ? values.Pose(output, parent)
                                    : values.Pose(input, parent);
                            valid = header.Code == CharacterPoseOperationCode
                                .LocalToComponentPose
                                    ? CharacterPosePureMath.TryToModel(
                                        in parentPose,
                                        in pose,
                                        out pose)
                                    : CharacterPosePureMath.TryToLocal(
                                        in parentPose,
                                        in pose,
                                        out pose);
                        }
                        if (!valid)
                        {
                            values.SetInvalid(
                                output,
                                values.Continuity(output),
                                AnimationPoseNativeInvalidReason
                                    .PoseSpaceConversionInvalid,
                                header.Index);
                            break;
                        }
                        values.SetPose(output, bone, in pose);
                    }
                }
                if (!frame.Complete(in header))
                    break;
            }
        }
    }

    [BurstCompile]
    internal struct CharacterPoseComponentControlWorkerKernel :
        IJobParallelFor
    {
        internal CharacterPoseWorkerKernelRange Range;
        [ReadOnly] internal NativeArray<CharacterPoseWorkerActorSlice> Actors;
        [ReadOnly] internal NativeArray<int> NativeOperationIndices;
        [ReadOnly] internal NativeArray<CharacterPoseNativeOperationHeader>
            Headers;
        [ReadOnly] internal NativeArray<float> ParameterDefaults;
        [ReadOnly] internal NativeArray<CharacterPoseNativeComponentControlOperation>
            Operations;
        [ReadOnly] internal NativeArray<AnimationPoseGraphNativeModifyBone>
            ModifyBones;
        [ReadOnly] internal NativeArray<AnimationPoseGraphNativeRootOrientationWarp>
            RootOrientationWarps;
        [ReadOnly] internal NativeArray<int> ParentIndices;

        public void Execute(int actorIndex)
        {
            CharacterPoseWorkerActorSlice actor = Actors[actorIndex];
            var frame = new CharacterPoseWorkerKernelFrame(
                in actor,
                ParameterDefaults);
            for (int index = 0; index < Range.OperationCount; index++)
            {
                int nativeOperationIndex =
                    Range.NativeOperationIndex(
                        NativeOperationIndices,
                        index);
                CharacterPoseNativeOperationHeader source = Range.Header(
                    Headers,
                    NativeOperationIndices,
                    index);
                if (!frame.TryBegin(
                        nativeOperationIndex,
                        in source,
                        out CharacterPoseNativeOperationHeader header))
                    continue;
                CharacterPoseNativeComponentControlOperation operation =
                    Operations[header.FamilyPayloadIndex];
                if (header.Code == CharacterPoseOperationCode.ModifyBone)
                    Modify(frame.Values, in header, in operation);
                else
                    Warp(actor, frame.Values, in header, in operation);
                if (!frame.Complete(in header))
                    break;
            }
        }

        void Modify(
            CharacterPoseValuePageSlice values,
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeComponentControlOperation operation)
        {
            int input = operation.InputPoseValueIndex;
            int output = operation.OutputPoseValueIndex;
            if (!values.IsInputReady(input, header.Index) ||
                (uint)operation.ModifyBoneIndex >= (uint)ModifyBones.Length ||
                !values.TryCopyValue(input, output, header.Index))
            {
                values.SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return;
            }
            if (values.Availability(output) != AnimationPoseAvailability.Pose)
                return;
            AnimationPoseGraphNativeModifyBone modify =
                ModifyBones[operation.ModifyBoneIndex];
            AnimationLocalBonePose current = values.Pose(
                output,
                modify.BoneIndex);
            CharacterComponentBonePose parent = default;
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Local &&
                modify.ParentBoneIndex >= 0)
            {
                parent = AsComponent(values.Pose(output, modify.ParentBoneIndex));
                if (!CharacterPoseConstraintMath.TryCreateLocal(
                        AsComponent(current),
                        parent,
                        out current))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(output),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
            }
            Vector3 position =
                (modify.Operations & ModifyBoneOperationMask.Position) != 0
                    ? current.Position + modify.Position * header.Weight
                    : current.Position;
            Quaternion rotation =
                (modify.Operations & ModifyBoneOperationMask.Rotation) != 0
                    ? Quaternion.SlerpUnclamped(
                        Quaternion.identity,
                        modify.Rotation,
                        header.Weight) * current.Rotation
                    : current.Rotation;
            Vector3 scale =
                (modify.Operations & ModifyBoneOperationMask.Scale) != 0
                    ? Vector3.Scale(
                        current.Scale,
                        Vector3.LerpUnclamped(
                            Vector3.one,
                            modify.Scale,
                            header.Weight))
                    : current.Scale;
            var changed = new AnimationLocalBonePose(position, rotation, scale);
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Local &&
                modify.ParentBoneIndex >= 0)
            {
                if (!CharacterPoseConstraintMath.TryCreateComponent(
                        changed,
                        parent,
                        out CharacterComponentBonePose component))
                {
                    values.SetInvalid(
                        output,
                        values.Continuity(output),
                        AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
                changed = ToPose(component);
            }
            values.SetPose(output, modify.BoneIndex, in changed);
            if (!RebuildDescendants(
                    values,
                    input,
                    output,
                    modify.BoneIndex))
            {
                values.SetInvalid(
                    output,
                    values.Continuity(output),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
            }
        }

        void Warp(
            in CharacterPoseWorkerActorSlice actor,
            CharacterPoseValuePageSlice values,
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeComponentControlOperation operation)
        {
            int input = operation.InputPoseValueIndex;
            int output = operation.OutputPoseValueIndex;
            int warpIndex = operation.RootOrientationWarpIndex;
            if (!values.IsInputReady(input, header.Index) || warpIndex < 0 ||
                warpIndex >= RootOrientationWarps.Length ||
                !values.TryCopyValue(input, output, header.Index))
            {
                values.SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return;
            }
            if (values.Availability(output) != AnimationPoseAvailability.Pose)
                return;
            CharacterRootOrientationWarpNativeControl control =
                actor.RootOrientationWarpControl(warpIndex);
            if (!control.IsValid)
            {
                values.SetInvalid(
                    output,
                    values.Continuity(output),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            if (control.Active == 0 ||
                Math.Abs(control.YawOffsetDegrees) <= 0.0001f)
                return;
            int root = RootOrientationWarps[warpIndex].RootPhysicalBoneIndex;
            if ((uint)root >= (uint)values.BoneCount)
            {
                values.SetInvalid(
                    output,
                    values.Continuity(output),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            AnimationLocalBonePose current = values.Pose(output, root);
            var changed = new AnimationLocalBonePose(
                current.Position,
                Quaternion.AngleAxis(
                    control.YawOffsetDegrees,
                    Vector3.up) * current.Rotation,
                current.Scale);
            values.SetPose(output, root, in changed);
        }

        bool RebuildDescendants(
            CharacterPoseValuePageSlice values,
            int input,
            int output,
            int root)
        {
            for (int bone = root + 1; bone < values.BoneCount; bone++)
            {
                if (!IsDescendant(bone, root))
                    continue;
                int parent = ParentIndices[bone];
                CharacterComponentBonePose inputBone =
                    AsComponent(values.Pose(input, bone));
                CharacterComponentBonePose inputParent =
                    AsComponent(values.Pose(input, parent));
                CharacterComponentBonePose outputParent =
                    AsComponent(values.Pose(output, parent));
                if (!CharacterPoseConstraintMath.TryCreateLocal(
                        inputBone,
                        inputParent,
                        out AnimationLocalBonePose local) ||
                    !CharacterPoseConstraintMath.TryCreateComponent(
                        local,
                        outputParent,
                        out CharacterComponentBonePose rebuilt))
                    return false;
                AnimationLocalBonePose changed = ToPose(rebuilt);
                values.SetPose(output, bone, in changed);
            }
            return true;
        }

        bool IsDescendant(int bone, int ancestor)
        {
            int cursor = bone;
            while (cursor >= 0)
            {
                if (cursor == ancestor)
                    return true;
                cursor = ParentIndices[cursor];
            }
            return false;
        }

        static CharacterComponentBonePose AsComponent(
            in AnimationLocalBonePose value) =>
            new CharacterComponentBonePose(
                value.Position,
                value.Rotation,
                value.Scale);

        static AnimationLocalBonePose ToPose(
            in CharacterComponentBonePose value) =>
            new AnimationLocalBonePose(
                value.Position,
                value.Rotation,
                value.Scale);
    }
}
