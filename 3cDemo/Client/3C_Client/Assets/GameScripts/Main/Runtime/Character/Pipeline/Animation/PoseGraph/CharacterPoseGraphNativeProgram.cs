using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct AnimationPoseGraphNativeOperation
    {
        internal AnimationPoseGraphNativeOperation(
            int index,
            CharacterPoseOperationCode code,
            int outputPoseValueIndex,
            int inputPoseValueIndexA,
            int inputPoseValueIndexB,
            int physicalSlotIndex,
            PoseSlotOutputPolicy poseSlotOutputPolicy,
            int boneMaskOffset,
            int additiveReferenceOffset,
            AdditiveReferenceSpace additiveReferenceSpace,
            AdditiveScalePolicy additiveScalePolicy,
            int parameterPolicyOffset,
            int frameCacheIndex,
            float weight)
        {
            if (index < 0 || !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) ||
                outputPoseValueIndex < 0 || !float.IsFinite(weight) || weight < 0f || weight > 1f)
            {
                throw new ArgumentException("Animation Pose Graph Native operation header is invalid.");
            }

            bool inputA = inputPoseValueIndexA >= 0 && inputPoseValueIndexA < outputPoseValueIndex;
            bool inputB = inputPoseValueIndexB >= 0 && inputPoseValueIndexB < outputPoseValueIndex;
            bool noInputA = inputPoseValueIndexA == -1;
            bool noInputB = inputPoseValueIndexB == -1;
            bool slot = physicalSlotIndex >= 0;
            bool noSlot = physicalSlotIndex == -1;
            bool slotPolicy = Enum.IsDefined(typeof(PoseSlotOutputPolicy), poseSlotOutputPolicy);
            bool noSlotPolicy = (int)poseSlotOutputPolicy == 0;
            bool mask = boneMaskOffset >= 0;
            bool noMask = boneMaskOffset == -1;
            bool additive = additiveReferenceOffset >= 0 &&
                            Enum.IsDefined(typeof(AdditiveReferenceSpace), additiveReferenceSpace) &&
                            Enum.IsDefined(typeof(AdditiveScalePolicy), additiveScalePolicy);
            bool noAdditive = additiveReferenceOffset == -1 &&
                              (int)additiveReferenceSpace == 0 &&
                              (int)additiveScalePolicy == 0;
            bool policies = parameterPolicyOffset >= 0;
            bool noPolicies = parameterPolicyOffset == -1;
            bool frameCache = frameCacheIndex == index;
            bool valid = code switch
            {
                CharacterPoseOperationCode.PoseSlotInput =>
                    noInputA && noInputB && slot && slotPolicy && noMask && noAdditive && noPolicies,
                CharacterPoseOperationCode.LayeredBoneBlend =>
                    inputA && inputB && noSlot && noSlotPolicy && mask && noAdditive && policies,
                CharacterPoseOperationCode.AdditivePose =>
                    inputA && inputB && noSlot && noSlotPolicy && mask && additive && policies,
                CharacterPoseOperationCode.PoseCurveResolve =>
                    inputA && inputB && noSlot && noSlotPolicy && noMask && noAdditive && policies,
                CharacterPoseOperationCode.OutputPose =>
                    inputA && noInputB && noSlot && noSlotPolicy && noMask && noAdditive && noPolicies,
                _ => false
            };
            if (!valid || !frameCache)
                throw new ArgumentException($"Animation Pose Graph Native operation #{index} layout is invalid.");

            Index = index;
            Code = code;
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndexA = inputPoseValueIndexA;
            InputPoseValueIndexB = inputPoseValueIndexB;
            PhysicalSlotIndex = physicalSlotIndex;
            PoseSlotOutputPolicy = poseSlotOutputPolicy;
            BoneMaskOffset = boneMaskOffset;
            AdditiveReferenceOffset = additiveReferenceOffset;
            AdditiveReferenceSpace = additiveReferenceSpace;
            AdditiveScalePolicy = additiveScalePolicy;
            ParameterPolicyOffset = parameterPolicyOffset;
            FrameCacheIndex = frameCacheIndex;
            Weight = weight;
        }

        internal int Index { get; }
        internal CharacterPoseOperationCode Code { get; }
        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndexA { get; }
        internal int InputPoseValueIndexB { get; }
        internal int PhysicalSlotIndex { get; }
        internal PoseSlotOutputPolicy PoseSlotOutputPolicy { get; }
        internal int BoneMaskOffset { get; }
        internal int AdditiveReferenceOffset { get; }
        internal AdditiveReferenceSpace AdditiveReferenceSpace { get; }
        internal AdditiveScalePolicy AdditiveScalePolicy { get; }
        internal int ParameterPolicyOffset { get; }
        internal int FrameCacheIndex { get; }
        internal float Weight { get; }
    }

    internal sealed class CharacterPoseGraphNativeProgram : IDisposable
    {
        NativeArray<AnimationPoseGraphNativeOperation> m_Operations;
        NativeArray<float> m_DenseBoneMasks;
        NativeArray<AnimationLocalBonePose> m_AdditiveReferences;
        NativeArray<PoseParameterResolvePolicy> m_ParameterPolicies;
        NativeArray<float> m_ParameterDefaults;
        NativeArray<int> m_ParentIndices;
        int m_BoneCount;
        int m_ParameterCount;
        int m_PoseValueCount;
        int m_ContributionStride;
        int m_FrameCacheCount;
        int m_OutputOperationIndex;
        int m_OutputPoseValueIndex;
        int m_LeftFootBoneIndex;
        int m_RightFootBoneIndex;
        bool m_Disposed;

        internal CharacterPoseGraphNativeProgram(
            CharacterPresentationPoseProgram program,
            CharacterAnimationRigPayload rig)
        {
            try
            {
                if (program == null)
                    throw new ArgumentNullException(nameof(program));
                if (rig == null)
                    throw new ArgumentNullException(nameof(rig));
                program.RequireValid();
                rig.RequireValid();
                if (!string.Equals(program.RigId, rig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(program.RigRevision, rig.RigRevision, StringComparison.Ordinal) ||
                    program.BoneCount != rig.Bones.Count ||
                    program.LeftFootBoneIndex != rig.LeftFootBoneIndex ||
                    program.RightFootBoneIndex != rig.RightFootBoneIndex ||
                    program.Parameters.Count <= 0 ||
                    program.ContributionWorkspaceCount % program.PoseValueWorkspaceCount != 0)
                {
                    throw new InvalidOperationException("Animation Pose Graph Program and Rig payload do not match.");
                }

                m_BoneCount = program.BoneCount;
                m_ParameterCount = program.Parameters.Count;
                m_PoseValueCount = program.PoseValueWorkspaceCount;
                m_ContributionStride = program.ContributionWorkspaceCount / program.PoseValueWorkspaceCount;
                m_FrameCacheCount = program.FrameCacheCount;
                m_OutputOperationIndex = program.OutputOperationIndex;
                m_LeftFootBoneIndex = rig.LeftFootBoneIndex;
                m_RightFootBoneIndex = rig.RightFootBoneIndex;
                if (m_ContributionStride <= 0 || m_FrameCacheCount <= 0)
                    throw new InvalidOperationException("Animation Pose Graph Native workspace layout is invalid.");

                int operationCount = program.Operations.Count;
                if (m_FrameCacheCount != operationCount)
                    throw new InvalidOperationException("Animation Pose Graph requires one stable frame cache per operation.");
                int maskValueCount = checked(program.BoneMasks.Count * m_BoneCount);
                int additiveValueCount = checked(program.AdditiveReferences.Count * m_BoneCount);
                int parameterPolicyCount = 0;
                for (int i = 0; i < operationCount; i++)
                {
                    if (program.Operations[i].ParameterPolicies.Count > 0)
                        parameterPolicyCount = checked(parameterPolicyCount + m_ParameterCount);
                }

                m_Operations = Allocate<AnimationPoseGraphNativeOperation>(operationCount);
                m_DenseBoneMasks = Allocate<float>(maskValueCount);
                m_AdditiveReferences = Allocate<AnimationLocalBonePose>(additiveValueCount);
                m_ParameterPolicies = Allocate<PoseParameterResolvePolicy>(parameterPolicyCount);
                m_ParameterDefaults = Allocate<float>(m_ParameterCount);
                m_ParentIndices = Allocate<int>(m_BoneCount);

                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    int parentIndex = rig.Bones[bone].ParentIndex;
                    if (parentIndex < -1 || parentIndex >= bone)
                        throw new InvalidOperationException($"Animation Pose Graph Rig Bone #{bone} parent is invalid.");
                    m_ParentIndices[bone] = parentIndex;
                }

                for (int i = 0; i < m_ParameterCount; i++)
                {
                    CharacterPresentationPoseParameterProgramEntry parameter = program.Parameters[i];
                    if (parameter.Index != i || !float.IsFinite(parameter.DefaultValue))
                        throw new InvalidOperationException($"Animation Pose Graph Parameter #{i} is invalid.");
                    m_ParameterDefaults[i] = parameter.DefaultValue;
                }

                for (int maskIndex = 0; maskIndex < program.BoneMasks.Count; maskIndex++)
                {
                    CharacterPresentationDenseBoneMask mask = program.BoneMasks[maskIndex];
                    int offset = checked(maskIndex * m_BoneCount);
                    for (int bone = 0; bone < m_BoneCount; bone++)
                    {
                        float value = mask.Weights[bone];
                        if (!float.IsFinite(value) || value < 0f || value > 1f)
                            throw new InvalidOperationException($"Animation Pose Graph Bone Mask #{maskIndex} is invalid.");
                        m_DenseBoneMasks[offset + bone] = value;
                    }
                }

                for (int referenceIndex = 0; referenceIndex < program.AdditiveReferences.Count; referenceIndex++)
                {
                    CharacterPresentationAdditiveReferenceDescriptor reference = program.AdditiveReferences[referenceIndex];
                    int offset = checked(referenceIndex * m_BoneCount);
                    for (int bone = 0; bone < m_BoneCount; bone++)
                    {
                        m_AdditiveReferences[offset + bone] = new AnimationLocalBonePose(
                            reference.Positions[bone],
                            reference.Rotations[bone],
                            reference.Scales[bone]);
                    }
                }

                int nextPolicyOffset = 0;
                for (int i = 0; i < operationCount; i++)
                {
                    CharacterPresentationPoseOperation operation = program.Operations[i];
                    int maskOffset = operation.BoneMaskIndex >= 0
                        ? checked(operation.BoneMaskIndex * m_BoneCount)
                        : -1;
                    PoseSlotOutputPolicy outputPolicy = default;
                    if (operation.PoseSlotIndex >= 0)
                    {
                        CharacterPresentationPoseSlotProgramEntry slot = program.Slots[operation.PoseSlotIndex];
                        if (slot.Index != operation.PoseSlotIndex)
                            throw new InvalidOperationException($"Animation Pose Graph operation #{i} Slot is invalid.");
                        outputPolicy = slot.OutputPolicy;
                    }
                    int additiveOffset = operation.AdditiveReferenceIndex >= 0
                        ? checked(operation.AdditiveReferenceIndex * m_BoneCount)
                        : -1;
                    AdditiveReferenceSpace referenceSpace = default;
                    AdditiveScalePolicy scalePolicy = default;
                    if (operation.AdditiveReferenceIndex >= 0)
                    {
                        CharacterPresentationAdditiveReferenceDescriptor reference =
                            program.AdditiveReferences[operation.AdditiveReferenceIndex];
                        referenceSpace = reference.Space;
                        scalePolicy = reference.ScalePolicy;
                    }

                    int parameterPolicyOffset = -1;
                    if (operation.ParameterPolicies.Count > 0)
                    {
                        if (operation.ParameterPolicies.Count != m_ParameterCount)
                            throw new InvalidOperationException($"Animation Pose Graph operation #{i} parameter policy is incomplete.");
                        parameterPolicyOffset = nextPolicyOffset;
                        for (int parameter = 0; parameter < m_ParameterCount; parameter++)
                        {
                            PoseParameterResolvePolicy policy = operation.ParameterPolicies[parameter];
                            if (!Enum.IsDefined(typeof(PoseParameterResolvePolicy), policy))
                                throw new InvalidOperationException($"Animation Pose Graph operation #{i} parameter policy is invalid.");
                            m_ParameterPolicies[nextPolicyOffset + parameter] = policy;
                        }
                        nextPolicyOffset = checked(nextPolicyOffset + m_ParameterCount);
                    }

                    m_Operations[i] = new AnimationPoseGraphNativeOperation(
                        operation.Index,
                        operation.Code,
                        operation.OutputPoseValueIndex,
                        operation.InputPoseValueIndexA,
                        operation.InputPoseValueIndexB,
                        operation.PoseSlotIndex,
                        outputPolicy,
                        maskOffset,
                        additiveOffset,
                        referenceSpace,
                        scalePolicy,
                        parameterPolicyOffset,
                        operation.Index,
                        operation.Weight);
                }
                if (nextPolicyOffset != parameterPolicyCount)
                    throw new InvalidOperationException("Animation Pose Graph parameter policy layout is inconsistent.");

                AnimationPoseGraphNativeOperation output = m_Operations[m_OutputOperationIndex];
                if (output.Code != CharacterPoseOperationCode.OutputPose)
                    throw new InvalidOperationException("Animation Pose Graph output operation is invalid.");
                m_OutputPoseValueIndex = output.OutputPoseValueIndex;
                RequireValid();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal int BoneCount => m_BoneCount;
        internal int ParameterCount => m_ParameterCount;
        internal int PoseValueCount => m_PoseValueCount;
        internal int ContributionStride => m_ContributionStride;
        internal int FrameCacheCount => m_FrameCacheCount;
        internal int OutputOperationIndex => m_OutputOperationIndex;
        internal int OutputPoseValueIndex => m_OutputPoseValueIndex;
        internal int LeftFootBoneIndex => m_LeftFootBoneIndex;
        internal int RightFootBoneIndex => m_RightFootBoneIndex;
        internal NativeArray<AnimationPoseGraphNativeOperation> Operations => m_Operations;
        internal NativeArray<float> DenseBoneMasks => m_DenseBoneMasks;
        internal NativeArray<AnimationLocalBonePose> AdditiveReferences => m_AdditiveReferences;
        internal NativeArray<PoseParameterResolvePolicy> ParameterPolicies => m_ParameterPolicies;
        internal NativeArray<float> ParameterDefaults => m_ParameterDefaults;
        internal NativeArray<int> ParentIndices => m_ParentIndices;

        internal void RequireValid()
        {
            RequireAlive();
            if (m_BoneCount <= 0 || m_ParameterCount <= 0 || m_PoseValueCount <= 0 ||
                m_ContributionStride <= 0 || m_FrameCacheCount <= 0 ||
                m_LeftFootBoneIndex < 0 || m_LeftFootBoneIndex >= m_BoneCount ||
                m_RightFootBoneIndex < 0 || m_RightFootBoneIndex >= m_BoneCount ||
                !m_Operations.IsCreated || m_Operations.Length <= 0 ||
                !m_DenseBoneMasks.IsCreated || !m_AdditiveReferences.IsCreated ||
                !m_ParameterPolicies.IsCreated || !m_ParameterDefaults.IsCreated || !m_ParentIndices.IsCreated ||
                m_ParameterDefaults.Length != m_ParameterCount ||
                m_ParentIndices.Length != m_BoneCount || m_FrameCacheCount != m_Operations.Length ||
                m_OutputOperationIndex < 0 || m_OutputOperationIndex >= m_Operations.Length ||
                m_OutputPoseValueIndex < 0 || m_OutputPoseValueIndex >= m_PoseValueCount)
            {
                throw new InvalidOperationException("Animation Pose Graph Native Program is invalid.");
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            DisposeArray(ref m_ParentIndices);
            DisposeArray(ref m_ParameterDefaults);
            DisposeArray(ref m_ParameterPolicies);
            DisposeArray(ref m_AdditiveReferences);
            DisposeArray(ref m_DenseBoneMasks);
            DisposeArray(ref m_Operations);
            m_BoneCount = 0;
            m_ParameterCount = 0;
            m_PoseValueCount = 0;
            m_ContributionStride = 0;
            m_FrameCacheCount = 0;
            m_OutputOperationIndex = -1;
            m_OutputPoseValueIndex = -1;
            m_LeftFootBoneIndex = -1;
            m_RightFootBoneIndex = -1;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseGraphNativeProgram));
        }

        static NativeArray<T> Allocate<T>(int length) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        static void DisposeArray<T>(ref NativeArray<T> values) where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
