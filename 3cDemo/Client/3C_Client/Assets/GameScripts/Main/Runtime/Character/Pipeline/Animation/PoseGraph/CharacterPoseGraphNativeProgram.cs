using System;
using Unity.Collections;
using UnityEngine;

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
            int playerIndex,
            AnimationSelectionAvailabilityPolicy playerOutputPolicy,
            int parameterIndex,
            int inertializationIndex,
            int boneMaskOffset,
            int additiveReferenceOffset,
            AdditiveReferenceSpace additiveReferenceSpace,
            AdditiveScalePolicy additiveScalePolicy,
            int parameterPolicyOffset,
            int modifyBoneIndex,
            int footPlacementIndex,
            int frameCacheIndex,
            float weight)
        {
            if (index < 0 || !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) ||
                outputPoseValueIndex < 0 || frameCacheIndex != index ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentException("Animation Pose Graph Native operation header is invalid.");
            Index = index;
            Code = code;
            OutputValueIndex = outputPoseValueIndex;
            InputValueIndexA = inputPoseValueIndexA;
            InputValueIndexB = inputPoseValueIndexB;
            PhysicalPlayerIndex = playerIndex;
            AnimationSelectionAvailabilityPolicy = playerOutputPolicy;
            ParameterIndex = parameterIndex;
            InertializationIndex = inertializationIndex;
            BoneMaskOffset = boneMaskOffset;
            AdditiveReferenceOffset = additiveReferenceOffset;
            AdditiveReferenceSpace = additiveReferenceSpace;
            AdditiveScalePolicy = additiveScalePolicy;
            ParameterPolicyOffset = parameterPolicyOffset;
            ModifyBoneIndex = modifyBoneIndex;
            FootPlacementIndex = footPlacementIndex;
            FrameCacheIndex = frameCacheIndex;
            Weight = weight;
        }

        internal int Index { get; }
        internal CharacterPoseOperationCode Code { get; }
        internal int OutputValueIndex { get; }
        internal int InputValueIndexA { get; }
        internal int InputValueIndexB { get; }
        internal int PhysicalPlayerIndex { get; }
        internal AnimationSelectionAvailabilityPolicy AnimationSelectionAvailabilityPolicy { get; }
        internal int ParameterIndex { get; }
        internal int InertializationIndex { get; }
        internal int BoneMaskOffset { get; }
        internal int AdditiveReferenceOffset { get; }
        internal AdditiveReferenceSpace AdditiveReferenceSpace { get; }
        internal AdditiveScalePolicy AdditiveScalePolicy { get; }
        internal int ParameterPolicyOffset { get; }
        internal int ModifyBoneIndex { get; }
        internal int FootPlacementIndex { get; }
        internal int FrameCacheIndex { get; }
        internal float Weight { get; }

        internal AnimationPoseGraphNativeOperation WithWeight(float value) => new AnimationPoseGraphNativeOperation(
            Index,
            Code,
            OutputValueIndex,
            InputValueIndexA,
            InputValueIndexB,
            PhysicalPlayerIndex,
            AnimationSelectionAvailabilityPolicy,
            ParameterIndex,
            InertializationIndex,
            BoneMaskOffset,
            AdditiveReferenceOffset,
            AdditiveReferenceSpace,
            AdditiveScalePolicy,
            ParameterPolicyOffset,
            ModifyBoneIndex,
            FootPlacementIndex,
            FrameCacheIndex,
            value);
    }

    internal readonly struct AnimationPoseGraphNativeModifyBone
    {
        internal AnimationPoseGraphNativeModifyBone(CharacterPresentationModifyBoneDescriptor source)
        {
            if (source == null || source.BoneIndex < 0 || source.ParentBoneIndex < -1 ||
                !Enum.IsDefined(typeof(ModifyBoneReferenceSpace), source.ReferenceSpace) ||
                source.Operations == ModifyBoneOperationMask.None)
                throw new ArgumentException("Animation Pose Graph Modify Bone payload is invalid.", nameof(source));
            BoneIndex = source.BoneIndex;
            ParentBoneIndex = source.ParentBoneIndex;
            ReferenceSpace = source.ReferenceSpace;
            Operations = source.Operations;
            Position = source.Position;
            Rotation = source.Rotation;
            Scale = source.Scale;
        }

        internal int BoneIndex { get; }
        internal int ParentBoneIndex { get; }
        internal ModifyBoneReferenceSpace ReferenceSpace { get; }
        internal ModifyBoneOperationMask Operations { get; }
        internal Vector3 Position { get; }
        internal Quaternion Rotation { get; }
        internal Vector3 Scale { get; }
    }

    internal sealed class CharacterPoseGraphNativeProgram : IDisposable
    {
        NativeArray<AnimationPoseGraphNativeOperation> m_Operations;
        NativeArray<float> m_DenseBoneMasks;
        NativeArray<AnimationLocalBonePose> m_AdditiveReferences;
        NativeArray<PoseParameterResolvePolicy> m_ParameterPolicies;
        NativeArray<float> m_ParameterDefaults;
        NativeArray<int> m_ParentIndices;
        NativeArray<AnimationPoseGraphNativeModifyBone> m_ModifyBones;
        int m_BoneCount;
        int m_ParameterCount;
        int m_PoseValueCount;
        int m_ContributionStride;
        int m_FrameCacheCount;
        int m_OutputOperationIndex;
        int m_OutputNativeOperationIndex;
        int m_OutputValueIndex;
        int m_LeftFootBoneIndex;
        int m_RightFootBoneIndex;
        bool m_Disposed;

        internal CharacterPoseGraphNativeProgram(CharacterPresentationPosePlan program, CharacterAnimationRigPayload rig)
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
                    program.BoneCount != rig.Bones.Count || program.Parameters.Count <= 0 ||
                    program.ContributionWorkspaceCount % program.PoseValueWorkspaceCount != 0)
                    throw new InvalidOperationException("Animation Pose Graph Program and Rig payload do not match.");

                m_BoneCount = program.BoneCount;
                m_ParameterCount = program.Parameters.Count;
                m_PoseValueCount = program.PoseValueWorkspaceCount;
                m_ContributionStride = program.ContributionWorkspaceCount / program.PoseValueWorkspaceCount;
                m_FrameCacheCount = program.FrameCacheCount;
                m_OutputOperationIndex = program.OutputOperationIndex;
                m_LeftFootBoneIndex = rig.LeftFootBoneIndex;
                m_RightFootBoneIndex = rig.RightFootBoneIndex;

                int nativeOperationCount = 0;
                int policyCount = 0;
                for (int i = 0; i < program.Operations.Count; i++)
                {
                    CharacterPresentationPoseOperation operation = program.Operations[i];
                    if (IsNativePoseOperation(operation.Code))
                        nativeOperationCount++;
                    if (operation.Code == CharacterPoseOperationCode.PoseParameterResolve)
                        policyCount = checked(policyCount + m_ParameterCount);
                }
                if (nativeOperationCount <= 0 || m_ContributionStride <= 0 || m_FrameCacheCount != program.Operations.Count)
                    throw new InvalidOperationException("Animation Pose Graph Native workspace layout is invalid.");

                m_Operations = Allocate<AnimationPoseGraphNativeOperation>(nativeOperationCount);
                m_DenseBoneMasks = Allocate<float>(checked(program.BoneMasks.Count * m_BoneCount));
                m_AdditiveReferences = Allocate<AnimationLocalBonePose>(checked(program.AdditiveReferences.Count * m_BoneCount));
                m_ParameterPolicies = Allocate<PoseParameterResolvePolicy>(policyCount);
                m_ParameterDefaults = Allocate<float>(m_ParameterCount);
                m_ParentIndices = Allocate<int>(m_BoneCount);
                m_ModifyBones = Allocate<AnimationPoseGraphNativeModifyBone>(program.ModifyBones.Count);

                CompileRig(program, rig);
                CompilePayloads(program);
                CompileOperations(program);
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
        internal int OutputNativeOperationIndex => m_OutputNativeOperationIndex;
        internal int OutputValueIndex => m_OutputValueIndex;
        internal int LeftFootBoneIndex => m_LeftFootBoneIndex;
        internal int RightFootBoneIndex => m_RightFootBoneIndex;
        internal NativeArray<AnimationPoseGraphNativeOperation> Operations => m_Operations;
        internal NativeArray<float> DenseBoneMasks => m_DenseBoneMasks;
        internal NativeArray<AnimationLocalBonePose> AdditiveReferences => m_AdditiveReferences;
        internal NativeArray<PoseParameterResolvePolicy> ParameterPolicies => m_ParameterPolicies;
        internal NativeArray<float> ParameterDefaults => m_ParameterDefaults;
        internal NativeArray<int> ParentIndices => m_ParentIndices;
        internal NativeArray<AnimationPoseGraphNativeModifyBone> ModifyBones => m_ModifyBones;

        void CompileRig(CharacterPresentationPosePlan program, CharacterAnimationRigPayload rig)
        {
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                int parent = rig.Bones[bone].ParentIndex;
                if (parent < -1 || parent >= bone)
                    throw new InvalidOperationException($"Animation Pose Graph Rig Bone #{bone} parent is invalid.");
                m_ParentIndices[bone] = parent;
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                CharacterPresentationPoseParameterEntry entry = program.Parameters[parameter];
                if (entry.Index != parameter || !float.IsFinite(entry.DefaultValue))
                    throw new InvalidOperationException($"Animation Pose Graph Parameter #{parameter} is invalid.");
                m_ParameterDefaults[parameter] = entry.DefaultValue;
            }
        }

        void CompilePayloads(CharacterPresentationPosePlan program)
        {
            for (int maskIndex = 0; maskIndex < program.BoneMasks.Count; maskIndex++)
            {
                CharacterPresentationDenseBoneMask mask = program.BoneMasks[maskIndex];
                for (int bone = 0; bone < m_BoneCount; bone++)
                    m_DenseBoneMasks[maskIndex * m_BoneCount + bone] = mask.Weights[bone];
            }
            for (int referenceIndex = 0; referenceIndex < program.AdditiveReferences.Count; referenceIndex++)
            {
                CharacterPresentationAdditiveReferenceDescriptor reference = program.AdditiveReferences[referenceIndex];
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    m_AdditiveReferences[referenceIndex * m_BoneCount + bone] = new AnimationLocalBonePose(
                        reference.Positions[bone], reference.Rotations[bone], reference.Scales[bone]);
                }
            }
            for (int i = 0; i < program.ModifyBones.Count; i++)
            {
                if (program.ModifyBones[i].Index != i)
                    throw new InvalidOperationException($"Animation Pose Graph Modify Bone #{i} is invalid.");
                m_ModifyBones[i] = new AnimationPoseGraphNativeModifyBone(program.ModifyBones[i]);
            }
        }

        void CompileOperations(CharacterPresentationPosePlan program)
        {
            int nativeIndex = 0;
            int policyOffset = 0;
            m_OutputNativeOperationIndex = -1;
            for (int i = 0; i < program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = program.Operations[i];
                if (!IsNativePoseOperation(operation.Code))
                    continue;
                AnimationSelectionAvailabilityPolicy outputPolicy = default;
                if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer || operation.Code == CharacterPoseOperationCode.BlendStack ||
                    operation.Code == CharacterPoseOperationCode.BlendSpacePlayer)
                {
                    if ((uint)operation.SelectionInputIndex >= (uint)program.SelectionInputs.Count)
                        throw new InvalidOperationException($"Animation Pose Graph Player operation #{i} has no Selection Input.");
                    outputPolicy = program.SelectionInputs[operation.SelectionInputIndex].Availability;
                }
                int operationPolicyOffset = -1;
                if (operation.Code == CharacterPoseOperationCode.PoseParameterResolve)
                {
                    if (operation.ParameterPolicies.Count != m_ParameterCount)
                        throw new InvalidOperationException($"Animation Pose Graph operation #{i} parameter policy is incomplete.");
                    operationPolicyOffset = policyOffset;
                    for (int parameter = 0; parameter < m_ParameterCount; parameter++)
                        m_ParameterPolicies[policyOffset++] = operation.ParameterPolicies[parameter];
                }
                int maskOffset = operation.BoneMaskIndex >= 0 ? operation.BoneMaskIndex * m_BoneCount : -1;
                int additiveOffset = operation.AdditiveReferenceIndex >= 0 ? operation.AdditiveReferenceIndex * m_BoneCount : -1;
                AdditiveReferenceSpace referenceSpace = default;
                AdditiveScalePolicy scalePolicy = default;
                if (operation.AdditiveReferenceIndex >= 0)
                {
                    CharacterPresentationAdditiveReferenceDescriptor reference = program.AdditiveReferences[operation.AdditiveReferenceIndex];
                    referenceSpace = reference.Space;
                    scalePolicy = reference.ScalePolicy;
                }
                m_Operations[nativeIndex] = new AnimationPoseGraphNativeOperation(
                    operation.Index,
                    operation.Code,
                    operation.OutputValueIndex,
                    operation.InputValueIndexA,
                    operation.InputValueIndexB,
                    operation.PlayerIndex,
                    outputPolicy,
                    operation.ParameterIndex,
                    operation.InertializationIndex,
                    maskOffset,
                    additiveOffset,
                    referenceSpace,
                    scalePolicy,
                    operationPolicyOffset,
                    operation.ModifyBoneIndex,
                    operation.FootPlacementNodeIndex,
                    operation.Index,
                    operation.Weight);
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    m_OutputNativeOperationIndex = nativeIndex;
                    m_OutputValueIndex = operation.OutputValueIndex;
                }
                nativeIndex++;
            }
            if (nativeIndex != m_Operations.Length || policyOffset != m_ParameterPolicies.Length ||
                m_OutputNativeOperationIndex < 0)
                throw new InvalidOperationException("Animation Pose Graph Native operation layout is inconsistent.");
        }

        internal void RequireValid()
        {
            RequireAlive();
            if (m_BoneCount <= 0 || m_ParameterCount <= 0 || m_PoseValueCount <= 0 || m_ContributionStride <= 0 ||
                m_FrameCacheCount <= 0 || m_LeftFootBoneIndex < 0 || m_LeftFootBoneIndex >= m_BoneCount ||
                m_RightFootBoneIndex < 0 || m_RightFootBoneIndex >= m_BoneCount ||
                !m_Operations.IsCreated || m_Operations.Length <= 0 || !m_DenseBoneMasks.IsCreated ||
                !m_AdditiveReferences.IsCreated || !m_ParameterPolicies.IsCreated || !m_ParameterDefaults.IsCreated ||
                !m_ParentIndices.IsCreated || !m_ModifyBones.IsCreated ||
                m_ParameterDefaults.Length != m_ParameterCount || m_ParentIndices.Length != m_BoneCount ||
                m_OutputNativeOperationIndex < 0 || m_OutputNativeOperationIndex >= m_Operations.Length ||
                m_OutputOperationIndex < 0 || m_OutputOperationIndex >= m_FrameCacheCount ||
                m_OutputValueIndex < 0 || m_OutputValueIndex >= m_PoseValueCount)
                throw new InvalidOperationException("Animation Pose Graph Native Program is invalid.");
        }

        internal static bool IsNativePoseOperation(CharacterPoseOperationCode code) => code switch
        {
            CharacterPoseOperationCode.SelectedPosePlayer => true,
            CharacterPoseOperationCode.BlendSpacePlayer => true,
            CharacterPoseOperationCode.BlendStack => true,
            CharacterPoseOperationCode.Inertialization => true,
            CharacterPoseOperationCode.BlendPose => true,
            CharacterPoseOperationCode.LayeredBoneBlend => true,
            CharacterPoseOperationCode.AdditivePose => true,
            CharacterPoseOperationCode.PoseParameterResolve => true,
            CharacterPoseOperationCode.ModifyBone => true,
            CharacterPoseOperationCode.FootPlacement => true,
            CharacterPoseOperationCode.OutputPose => true,
            _ => false
        };

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            DisposeArray(ref m_ModifyBones);
            DisposeArray(ref m_ParentIndices);
            DisposeArray(ref m_ParameterDefaults);
            DisposeArray(ref m_ParameterPolicies);
            DisposeArray(ref m_AdditiveReferences);
            DisposeArray(ref m_DenseBoneMasks);
            DisposeArray(ref m_Operations);
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseGraphNativeProgram));
        }

        static NativeArray<T> Allocate<T>(int length) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        static NativeArray<T> AllocateClear<T>(int length) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        static void DisposeArray<T>(ref NativeArray<T> values) where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
