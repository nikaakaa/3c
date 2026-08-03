using System;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct AnimationBlendCurveNativeEntry
    {
        internal AnimationBlendCurveNativeEntry(int segmentOffset, int segmentCount)
        {
            if (segmentOffset < 0 || segmentCount <= 0)
                throw new ArgumentException("Animation Blend Curve native entry is invalid.");
            SegmentOffset = segmentOffset;
            SegmentCount = segmentCount;
        }

        internal int SegmentOffset { get; }
        internal int SegmentCount { get; }
    }

    internal readonly struct AnimationBlendProfileNativeEntry
    {
        internal AnimationBlendProfileNativeEntry(int denseOffset, float globalDurationMultiplier)
        {
            if (denseOffset < 0 || !float.IsFinite(globalDurationMultiplier) || globalDurationMultiplier <= 0f)
                throw new ArgumentException("Animation Blend Profile native entry is invalid.");
            DenseOffset = denseOffset;
            GlobalDurationMultiplier = globalDurationMultiplier;
        }

        internal int DenseOffset { get; }
        internal float GlobalDurationMultiplier { get; }
    }

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
            int rootOrientationWarpIndex,
            int twoBoneIkIndex,
            int footPlacementIndex,
            int stateMachineIndex,
            int animationSlotIndex,
            int frameCacheIndex,
            float weight)
        {
            byte codeValue = (byte)code;
            if (index < 0 ||
                codeValue < (byte)CharacterPoseOperationCode.ProgramParameterInput ||
                codeValue > (byte)CharacterPoseOperationCode.ComponentToLocalPose ||
                codeValue == 14 ||
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
            RootOrientationWarpIndex = rootOrientationWarpIndex;
            TwoBoneIkIndex = twoBoneIkIndex;
            FootPlacementIndex = footPlacementIndex;
            StateMachineIndex = stateMachineIndex;
            AnimationSlotIndex = animationSlotIndex;
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
        internal int RootOrientationWarpIndex { get; }
        internal int TwoBoneIkIndex { get; }
        internal int FootPlacementIndex { get; }
        internal int StateMachineIndex { get; }
        internal int AnimationSlotIndex { get; }
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
            RootOrientationWarpIndex,
            TwoBoneIkIndex,
            FootPlacementIndex,
            StateMachineIndex,
            AnimationSlotIndex,
            FrameCacheIndex,
            value);

        internal AnimationPoseGraphNativeOperation WithBlendInputs(
            int sourcePoseValueIndex,
            int targetPoseValueIndex,
            float targetWeight) => new AnimationPoseGraphNativeOperation(
            Index,
            CharacterPoseOperationCode.PoseStateMachine,
            OutputValueIndex,
            sourcePoseValueIndex,
            targetPoseValueIndex,
            PhysicalPlayerIndex,
            AnimationSelectionAvailabilityPolicy,
            ParameterIndex,
            InertializationIndex,
            -1,
            AdditiveReferenceOffset,
            AdditiveReferenceSpace,
            AdditiveScalePolicy,
            ParameterPolicyOffset,
            ModifyBoneIndex,
            RootOrientationWarpIndex,
            TwoBoneIkIndex,
            FootPlacementIndex,
            StateMachineIndex,
            AnimationSlotIndex,
            FrameCacheIndex,
            targetWeight);
    }

    internal readonly struct AnimationPoseGraphNativeStage
    {
        internal AnimationPoseGraphNativeStage(CharacterPresentationPoseStage source)
        {
            if (source == null || source.Index < 0 || source.NativeOperationStart < 0 ||
                source.NativeOperationCount < 0 || source.CompletionIndex != source.Index ||
                source.DiagnosticIndex != source.Index)
            {
                throw new ArgumentException("Animation Pose Graph native stage is invalid.", nameof(source));
            }
            Index = source.Index;
            ExecutionDomain = source.ExecutionDomain;
            InputPoseSpace = source.InputPoseSpace;
            OutputPoseSpace = source.OutputPoseSpace;
            OperationStart = source.NativeOperationStart;
            OperationCount = source.NativeOperationCount;
            CompletionIndex = source.CompletionIndex;
            DiagnosticIndex = source.DiagnosticIndex;
        }

        internal int Index { get; }
        internal CharacterPoseExecutionDomain ExecutionDomain { get; }
        internal CharacterPoseSpace InputPoseSpace { get; }
        internal CharacterPoseSpace OutputPoseSpace { get; }
        internal int OperationStart { get; }
        internal int OperationCount { get; }
        internal int CompletionIndex { get; }
        internal int DiagnosticIndex { get; }
    }

    internal readonly struct AnimationPoseGraphNativeLegChain
    {
        internal AnimationPoseGraphNativeLegChain(CharacterAnimationLegChainPayload source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            HipPhysicalBoneIndex = source.HipPhysicalBoneIndex;
            KneePhysicalBoneIndex = source.KneePhysicalBoneIndex;
            AnklePhysicalBoneIndex = source.AnklePhysicalBoneIndex;
            ToePhysicalBoneIndex = source.ToePhysicalBoneIndex;
            UpperLegLength = source.UpperLegLength;
            LowerLegLength = source.LowerLegLength;
            FootLength = source.FootLength;
        }

        internal int HipPhysicalBoneIndex { get; }
        internal int KneePhysicalBoneIndex { get; }
        internal int AnklePhysicalBoneIndex { get; }
        internal int ToePhysicalBoneIndex { get; }
        internal float UpperLegLength { get; }
        internal float LowerLegLength { get; }
        internal float FootLength { get; }

        internal bool IsValid(int physicalBoneCount, int pelvisPhysicalBoneIndex) =>
            HipPhysicalBoneIndex >= 0 && HipPhysicalBoneIndex < physicalBoneCount &&
            KneePhysicalBoneIndex >= 0 && KneePhysicalBoneIndex < physicalBoneCount &&
            AnklePhysicalBoneIndex >= 0 && AnklePhysicalBoneIndex < physicalBoneCount &&
            ToePhysicalBoneIndex >= 0 && ToePhysicalBoneIndex < physicalBoneCount &&
            HipPhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            KneePhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            AnklePhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            ToePhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            HipPhysicalBoneIndex != KneePhysicalBoneIndex &&
            HipPhysicalBoneIndex != AnklePhysicalBoneIndex &&
            HipPhysicalBoneIndex != ToePhysicalBoneIndex &&
            KneePhysicalBoneIndex != AnklePhysicalBoneIndex &&
            KneePhysicalBoneIndex != ToePhysicalBoneIndex &&
            AnklePhysicalBoneIndex != ToePhysicalBoneIndex &&
            float.IsFinite(UpperLegLength) && UpperLegLength > 0.0001f &&
            float.IsFinite(LowerLegLength) && LowerLegLength > 0.0001f &&
            float.IsFinite(FootLength) && FootLength > 0.0001f;
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

    internal readonly struct AnimationPoseGraphNativeRootOrientationWarp
    {
        internal AnimationPoseGraphNativeRootOrientationWarp(
            CharacterPresentationRootOrientationWarpDescriptor source)
        {
            if (source == null || source.RootPhysicalBoneIndex < 0)
                throw new ArgumentException("Animation Pose Graph Root Orientation Warp payload is invalid.", nameof(source));
            RootPhysicalBoneIndex = source.RootPhysicalBoneIndex;
        }

        internal int RootPhysicalBoneIndex { get; }
    }

    internal readonly struct CharacterRootOrientationWarpNativeControl
    {
        internal CharacterRootOrientationWarpNativeControl(bool active, float yawOffsetDegrees)
        {
            if (!float.IsFinite(yawOffsetDegrees))
                throw new ArgumentOutOfRangeException(nameof(yawOffsetDegrees));
            Active = active ? (byte)1 : (byte)0;
            YawOffsetDegrees = active ? yawOffsetDegrees : 0f;
        }

        internal byte Active { get; }
        internal float YawOffsetDegrees { get; }
        internal bool IsValid => Active <= 1 && float.IsFinite(YawOffsetDegrees) &&
                                 (Active != 0 || YawOffsetDegrees == 0f);
    }

    internal readonly struct CharacterFootPlacementNativeControl
    {
        internal CharacterFootPlacementNativeControl(
            bool active,
            Vector3 pelvisComponentOffset,
            Vector3 leftTargetComponentPosition,
            Quaternion leftTargetComponentRotation,
            Vector3 leftBendComponentDirection,
            float leftPositionWeight,
            float leftRotationWeight,
            Vector3 rightTargetComponentPosition,
            Quaternion rightTargetComponentRotation,
            Vector3 rightBendComponentDirection,
            float rightPositionWeight,
            float rightRotationWeight,
            byte executionState = 0)
        {
            Active = executionState == 0
                ? active ? (byte)1 : (byte)0
                : executionState;
            PelvisComponentOffset = active ? pelvisComponentOffset : Vector3.zero;
            LeftTargetComponentPosition = active ? leftTargetComponentPosition : Vector3.zero;
            LeftTargetComponentRotation = active ? leftTargetComponentRotation.normalized : Quaternion.identity;
            LeftBendComponentDirection = active ? leftBendComponentDirection.normalized : Vector3.forward;
            LeftPositionWeight = active ? Mathf.Clamp01(leftPositionWeight) : 0f;
            LeftRotationWeight = active ? Mathf.Clamp01(leftRotationWeight) : 0f;
            RightTargetComponentPosition = active ? rightTargetComponentPosition : Vector3.zero;
            RightTargetComponentRotation = active ? rightTargetComponentRotation.normalized : Quaternion.identity;
            RightBendComponentDirection = active ? rightBendComponentDirection.normalized : Vector3.forward;
            RightPositionWeight = active ? Mathf.Clamp01(rightPositionWeight) : 0f;
            RightRotationWeight = active ? Mathf.Clamp01(rightRotationWeight) : 0f;
        }

        internal byte Active { get; }
        internal Vector3 PelvisComponentOffset { get; }
        internal Vector3 LeftTargetComponentPosition { get; }
        internal Quaternion LeftTargetComponentRotation { get; }
        internal Vector3 LeftBendComponentDirection { get; }
        internal float LeftPositionWeight { get; }
        internal float LeftRotationWeight { get; }
        internal Vector3 RightTargetComponentPosition { get; }
        internal Quaternion RightTargetComponentRotation { get; }
        internal Vector3 RightBendComponentDirection { get; }
        internal float RightPositionWeight { get; }
        internal float RightRotationWeight { get; }
        internal bool IsValid => Active <= 2 &&
                                 CharacterPoseConstraintMath.IsFinite(PelvisComponentOffset) &&
                                 CharacterPoseConstraintMath.IsFinite(LeftTargetComponentPosition) &&
                                 CharacterPoseConstraintMath.IsFinite(LeftTargetComponentRotation) &&
                                 CharacterPoseConstraintMath.IsFinite(LeftBendComponentDirection) &&
                                 float.IsFinite(LeftPositionWeight) && float.IsFinite(LeftRotationWeight) &&
                                 CharacterPoseConstraintMath.IsFinite(RightTargetComponentPosition) &&
                                 CharacterPoseConstraintMath.IsFinite(RightTargetComponentRotation) &&
                                 CharacterPoseConstraintMath.IsFinite(RightBendComponentDirection) &&
                                 float.IsFinite(RightPositionWeight) && float.IsFinite(RightRotationWeight);

        internal static CharacterFootPlacementNativeControl Inactive =>
            new CharacterFootPlacementNativeControl(
                false,
                Vector3.zero,
                Vector3.zero,
                Quaternion.identity,
                Vector3.forward,
                0f,
                0f,
                Vector3.zero,
                Quaternion.identity,
                Vector3.forward,
                0f,
                0f);

        internal static CharacterFootPlacementNativeControl WorldContextUnavailable =>
            new CharacterFootPlacementNativeControl(
                false,
                Vector3.zero,
                Vector3.zero,
                Quaternion.identity,
                Vector3.forward,
                0f,
                0f,
                Vector3.zero,
                Quaternion.identity,
                Vector3.forward,
                0f,
                0f,
                2);
    }

    internal sealed class CharacterPoseGraphNativeProgram : IDisposable
    {
        sealed class Page
        {
            internal NativeArray<CharacterTwoBoneIkRuntimeDiagnostic>
                TwoBoneIkDiagnostics;
            internal NativeArray<CharacterComponentBonePose>
                ConstraintComponentScratch;
            internal NativeArray<CharacterPoseStateMachineNativeControl>
                StateMachineControls;
            internal NativeArray<CharacterAnimationSlotNativeControl>
                AnimationSlotControls;
            internal NativeArray<CharacterRootOrientationWarpNativeControl>
                RootOrientationWarpControls;
            internal NativeArray<CharacterFootPlacementNativeControl>
                FootPlacementControls;
        }

        NativeArray<AnimationPoseGraphNativeOperation> m_Operations;
        NativeArray<AnimationPoseGraphNativeStage> m_Stages;
        NativeArray<float> m_DenseBoneMasks;
        NativeArray<AnimationLocalBonePose> m_AdditiveReferences;
        NativeArray<PoseParameterResolvePolicy> m_ParameterPolicies;
        NativeArray<float> m_ParameterDefaults;
        NativeArray<int> m_ParentIndices;
        NativeArray<AnimationBlendCurveNativeEntry> m_BlendCurves;
        NativeArray<AnimationBlendCurveSegment> m_BlendCurveSegments;
        NativeArray<AnimationBlendProfileNativeEntry> m_BlendProfiles;
        NativeArray<float> m_BlendDenseProfiles;
        NativeArray<AnimationPoseGraphNativeModifyBone> m_ModifyBones;
        NativeArray<AnimationPoseGraphNativeRootOrientationWarp> m_RootOrientationWarps;
        NativeArray<CharacterRootOrientationWarpNativeControl> m_RootOrientationWarpControls;
        NativeArray<CharacterFootPlacementNativeControl> m_FootPlacementControls;
        NativeArray<CharacterTwoBoneIkDescriptor> m_TwoBoneIks;
        NativeArray<CharacterTwoBoneIkRuntimeDiagnostic> m_TwoBoneIkDiagnostics;
        NativeArray<CharacterComponentBonePose> m_ConstraintComponentScratch;
        NativeArray<CharacterPoseStateMachineNativeControl> m_StateMachineControls;
        NativeArray<CharacterAnimationSlotNativeControl> m_AnimationSlotControls;
        Page m_CommittedPage;
        Page m_PendingPage;
        CharacterPoseBoneCounts m_BoneCounts;
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
        int m_PelvisBoneIndex;
        AnimationPoseGraphNativeLegChain m_LeftLeg;
        AnimationPoseGraphNativeLegChain m_RightLeg;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseGraphNativeProgram(
            CharacterPresentationPosePlan program,
            CharacterAnimationRigPayload rig,
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles)
        {
            try
            {
                if (program == null)
                    throw new ArgumentNullException(nameof(program));
                if (rig == null)
                    throw new ArgumentNullException(nameof(rig));
                if (curves == null)
                    throw new ArgumentNullException(nameof(curves));
                if (profiles == null)
                    throw new ArgumentNullException(nameof(profiles));
                program.RequireValid();
                rig.RequireValid();
                curves.RequireValid();
                profiles.RequireValid(program.PoseBoneCount, rig.RigId, rig.RigRevision);
                if (!string.Equals(program.RigId, rig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(program.RigRevision, rig.RigRevision, StringComparison.Ordinal) ||
                    program.PoseBoneCount != rig.PoseBoneCount || program.Parameters.Count <= 0 ||
                    program.ContributionWorkspaceCount % program.PoseValueWorkspaceCount != 0)
                    throw new InvalidOperationException("Animation Pose Graph Program and Rig payload do not match.");

                m_BoneCount = program.PoseBoneCount;
                m_BoneCounts = rig.BoneCounts;
                m_ParameterCount = program.Parameters.Count;
                m_PoseValueCount = program.PoseValueWorkspaceCount;
                m_ContributionStride = program.ContributionWorkspaceCount / program.PoseValueWorkspaceCount;
                m_FrameCacheCount = program.FrameCacheCount;
                m_OutputOperationIndex = program.OutputOperationIndex;
                m_LeftFootBoneIndex = rig.LeftLeg.AnklePhysicalBoneIndex;
                m_RightFootBoneIndex = rig.RightLeg.AnklePhysicalBoneIndex;
                m_PelvisBoneIndex = rig.PelvisPhysicalBoneIndex;
                m_LeftLeg = new AnimationPoseGraphNativeLegChain(rig.LeftLeg);
                m_RightLeg = new AnimationPoseGraphNativeLegChain(rig.RightLeg);

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
                m_Stages = Allocate<AnimationPoseGraphNativeStage>(program.Stages.Count);
                m_DenseBoneMasks = Allocate<float>(checked(program.BoneMasks.Count * m_BoneCount));
                m_AdditiveReferences = Allocate<AnimationLocalBonePose>(checked(program.AdditiveReferences.Count * m_BoneCount));
                m_ParameterPolicies = Allocate<PoseParameterResolvePolicy>(policyCount);
                m_ParameterDefaults = Allocate<float>(m_ParameterCount);
                m_ParentIndices = Allocate<int>(m_BoneCount);
                int curveSegmentCount = curves.Entries.Sum(value => value.Curve.Segments.Count);
                m_BlendCurves = Allocate<AnimationBlendCurveNativeEntry>(curves.Entries.Count);
                m_BlendCurveSegments = Allocate<AnimationBlendCurveSegment>(curveSegmentCount);
                m_BlendProfiles = Allocate<AnimationBlendProfileNativeEntry>(profiles.Entries.Count);
                m_BlendDenseProfiles = Allocate<float>(checked(profiles.Entries.Count * m_BoneCount));
                m_ModifyBones = Allocate<AnimationPoseGraphNativeModifyBone>(program.ModifyBones.Count);
                m_RootOrientationWarps = Allocate<AnimationPoseGraphNativeRootOrientationWarp>(program.RootOrientationWarps.Count);
                m_RootOrientationWarpControls = AllocateClear<CharacterRootOrientationWarpNativeControl>(program.RootOrientationWarps.Count);
                m_FootPlacementControls = AllocateClear<CharacterFootPlacementNativeControl>(program.FootPlacementNodes.Count);
                for (int i = 0; i < m_FootPlacementControls.Length; i++)
                    m_FootPlacementControls[i] = CharacterFootPlacementNativeControl.Inactive;
                m_TwoBoneIks = Allocate<CharacterTwoBoneIkDescriptor>(program.TwoBoneIks.Count);
                m_TwoBoneIkDiagnostics = AllocateClear<CharacterTwoBoneIkRuntimeDiagnostic>(program.TwoBoneIks.Count);
                m_ConstraintComponentScratch = Allocate<CharacterComponentBonePose>(m_BoneCount);
                m_StateMachineControls = Allocate<CharacterPoseStateMachineNativeControl>(program.StateMachines.Count);
                m_AnimationSlotControls = Allocate<CharacterAnimationSlotNativeControl>(program.AnimationSlots.Count);

                CompileRig(program, rig);
                CompileBlendCatalogs(curves, profiles);
                CompilePayloads(program);
                CompileOperations(program);
                CompileStages(program);
                m_CommittedPage = CaptureActivePage();
                m_PendingPage = AllocatePage();
                RequireValid();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal int PoseBoneCount => m_BoneCount;
        internal int ParameterCount => m_ParameterCount;
        internal int PoseValueCount => m_PoseValueCount;
        internal int ContributionStride => m_ContributionStride;
        internal int FrameCacheCount => m_FrameCacheCount;
        internal int OutputOperationIndex => m_OutputOperationIndex;
        internal int OutputNativeOperationIndex => m_OutputNativeOperationIndex;
        internal int OutputValueIndex => m_OutputValueIndex;
        internal int LeftFootBoneIndex => m_LeftFootBoneIndex;
        internal int RightFootBoneIndex => m_RightFootBoneIndex;
        internal int PelvisBoneIndex => m_PelvisBoneIndex;
        internal AnimationPoseGraphNativeLegChain LeftLeg => m_LeftLeg;
        internal AnimationPoseGraphNativeLegChain RightLeg => m_RightLeg;
        internal NativeArray<AnimationPoseGraphNativeOperation> Operations => m_Operations;
        internal NativeArray<AnimationPoseGraphNativeStage> Stages => m_Stages;
        internal NativeArray<float> DenseBoneMasks => m_DenseBoneMasks;
        internal NativeArray<AnimationLocalBonePose> AdditiveReferences => m_AdditiveReferences;
        internal NativeArray<PoseParameterResolvePolicy> ParameterPolicies => m_ParameterPolicies;
        internal NativeArray<float> ParameterDefaults => m_ParameterDefaults;
        internal NativeArray<int> ParentIndices => m_ParentIndices;
        internal NativeArray<AnimationBlendCurveNativeEntry> BlendCurves => m_BlendCurves;
        internal NativeArray<AnimationBlendCurveSegment> BlendCurveSegments => m_BlendCurveSegments;
        internal NativeArray<AnimationBlendProfileNativeEntry> BlendProfiles => m_BlendProfiles;
        internal NativeArray<float> BlendDenseProfiles => m_BlendDenseProfiles;
        internal NativeArray<AnimationPoseGraphNativeModifyBone> ModifyBones => m_ModifyBones;
        internal NativeArray<AnimationPoseGraphNativeRootOrientationWarp> RootOrientationWarps => m_RootOrientationWarps;
        internal NativeArray<CharacterRootOrientationWarpNativeControl> RootOrientationWarpControls => m_RootOrientationWarpControls;
        internal NativeArray<CharacterFootPlacementNativeControl> FootPlacementControls => m_FootPlacementControls;
        internal NativeArray<CharacterTwoBoneIkDescriptor> TwoBoneIks => m_TwoBoneIks;
        internal NativeArray<CharacterTwoBoneIkRuntimeDiagnostic> TwoBoneIkDiagnostics => m_TwoBoneIkDiagnostics;
        internal NativeArray<CharacterComponentBonePose> ConstraintComponentScratch => m_ConstraintComponentScratch;
        internal CharacterPoseBoneCounts BoneCounts => m_BoneCounts;
        internal NativeArray<CharacterPoseStateMachineNativeControl> StateMachineControls => m_StateMachineControls;
        internal NativeArray<CharacterAnimationSlotNativeControl> AnimationSlotControls =>
            m_AnimationSlotControls;
        internal bool HasOpenFrame => m_FrameOpen;

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Character Pose Graph frame is already open.");
            BindPage(m_PendingPage);
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            Page previousCommitted = m_CommittedPage;
            m_CommittedPage = m_PendingPage;
            m_PendingPage = previousCommitted;
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            BindPage(m_CommittedPage);
            m_FrameOpen = false;
        }

        internal void SetStateMachineControl(
            int stateMachineIndex,
            in CharacterPoseStateMachineNativeControl control)
        {
            RequireAlive();
            if ((uint)stateMachineIndex >= (uint)m_StateMachineControls.Length)
                throw new ArgumentOutOfRangeException(nameof(stateMachineIndex));
            m_StateMachineControls[stateMachineIndex] = control;
        }

        internal float GetStateMachineBoneWeight(int stateMachineIndex, int boneIndex)
        {
            RequireAlive();
            if ((uint)stateMachineIndex >= (uint)m_StateMachineControls.Length ||
                (uint)boneIndex >= (uint)m_BoneCount)
                throw new ArgumentOutOfRangeException();
            CharacterPoseStateMachineNativeControl control =
                m_StateMachineControls[stateMachineIndex];
            if (control.BlendMode != CharacterPoseStateMachineBlendMode.Standard ||
                control.DurationSeconds <= 0f)
                return 1f;
            if ((uint)control.CurveIndex >= (uint)m_BlendCurves.Length ||
                (uint)control.BlendProfileIndex >= (uint)m_BlendProfiles.Length)
            {
                throw new InvalidOperationException(
                    "Pose StateMachine diagnostic blend indices are invalid.");
            }
            AnimationBlendProfileNativeEntry profile =
                m_BlendProfiles[control.BlendProfileIndex];
            float duration = control.DurationSeconds *
                             profile.GlobalDurationMultiplier *
                             m_BlendDenseProfiles[profile.DenseOffset + boneIndex];
            if (duration <= 0f || control.ElapsedSeconds >= duration)
                return 1f;
            float time = Mathf.Clamp01(control.ElapsedSeconds / duration);
            AnimationBlendCurveNativeEntry curve = m_BlendCurves[control.CurveIndex];
            AnimationBlendCurveSegment segment =
                m_BlendCurveSegments[curve.SegmentOffset + curve.SegmentCount - 1];
            for (int i = 0; i < curve.SegmentCount; i++)
            {
                AnimationBlendCurveSegment candidate =
                    m_BlendCurveSegments[curve.SegmentOffset + i];
                if (time > candidate.EndTime)
                    continue;
                segment = candidate;
                break;
            }
            float normalized = (time - segment.StartTime) /
                               (segment.EndTime - segment.StartTime);
            return Mathf.Clamp01(
                ((segment.A * normalized + segment.B) * normalized + segment.C) *
                normalized + segment.D);
        }

        internal void SetAnimationSlotControl(
            int animationSlotIndex,
            in CharacterAnimationSlotNativeControl control)
        {
            RequireAlive();
            if ((uint)animationSlotIndex >= (uint)m_AnimationSlotControls.Length)
                throw new ArgumentOutOfRangeException(nameof(animationSlotIndex));
            m_AnimationSlotControls[animationSlotIndex] = control;
        }

        internal void SetRootOrientationWarpControl(
            int rootOrientationWarpIndex,
            in CharacterRootOrientationWarpNativeControl control)
        {
            RequireAlive();
            if ((uint)rootOrientationWarpIndex >=
                (uint)m_RootOrientationWarpControls.Length ||
                !control.IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rootOrientationWarpIndex));
            }
            m_RootOrientationWarpControls[rootOrientationWarpIndex] = control;
        }

        internal void SetFootPlacementControl(
            int footPlacementIndex,
            in CharacterFootPlacementNativeControl control)
        {
            RequireAlive();
            if ((uint)footPlacementIndex >= (uint)m_FootPlacementControls.Length || !control.IsValid)
                throw new ArgumentOutOfRangeException(nameof(footPlacementIndex));
            m_FootPlacementControls[footPlacementIndex] = control;
        }

        void CompileRig(CharacterPresentationPosePlan program, CharacterAnimationRigPayload rig)
        {
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                int parent = rig.GetPoseParentIndex(bone);
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

        void CompileBlendCatalogs(
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles)
        {
            int segmentOffset = 0;
            for (int curveIndex = 0; curveIndex < curves.Entries.Count; curveIndex++)
            {
                AnimationBlendCurvePayload curve = curves.Require(curveIndex);
                m_BlendCurves[curveIndex] = new AnimationBlendCurveNativeEntry(
                    segmentOffset,
                    curve.Segments.Count);
                for (int segment = 0; segment < curve.Segments.Count; segment++)
                    m_BlendCurveSegments[segmentOffset + segment] = curve.Segments[segment];
                segmentOffset += curve.Segments.Count;
            }
            if (segmentOffset != m_BlendCurveSegments.Length)
                throw new InvalidOperationException("Animation Blend Curve native catalog layout is inconsistent.");

            for (int profileIndex = 0; profileIndex < profiles.Entries.Count; profileIndex++)
            {
                AnimationBlendProfilePayload profile = profiles.Require(profileIndex);
                int denseOffset = profileIndex * m_BoneCount;
                m_BlendProfiles[profileIndex] = new AnimationBlendProfileNativeEntry(
                    denseOffset,
                    profile.GlobalDurationMultiplier);
                for (int bone = 0; bone < m_BoneCount; bone++)
                    m_BlendDenseProfiles[denseOffset + bone] = profile.DenseDurationMultipliers[bone];
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
            for (int i = 0; i < program.RootOrientationWarps.Count; i++)
            {
                if (program.RootOrientationWarps[i].Index != i)
                    throw new InvalidOperationException($"Animation Pose Graph Root Orientation Warp #{i} is invalid.");
                m_RootOrientationWarps[i] = new AnimationPoseGraphNativeRootOrientationWarp(program.RootOrientationWarps[i]);
            }
            for (int i = 0; i < program.TwoBoneIks.Count; i++)
                m_TwoBoneIks[i] = program.TwoBoneIks[i].ToRuntimeDescriptor();
            for (int i = 0; i < program.StateMachines.Count; i++)
            {
                CharacterPoseStateMachineDescriptor machine = program.StateMachines[i];
                int output = machine.States[machine.EntryStateIndex].OutputPoseValueIndex;
                m_StateMachineControls[i] = new CharacterPoseStateMachineNativeControl(
                    output,
                    output,
                    machine.EntryStateIndex,
                    machine.EntryStateIndex,
                    0f,
                    0f,
                    -1,
                    -1,
                    CharacterPoseStateMachineBlendMode.Single,
                    1);
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
                if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer ||
                    operation.Code == CharacterPoseOperationCode.BlendStack ||
                    operation.Code == CharacterPoseOperationCode.BlendSpacePlayer ||
                    operation.Code == CharacterPoseOperationCode.SequencePlayer ||
                    operation.Code == CharacterPoseOperationCode.AnimationSlot)
                {
                    if (operation.Code !=
                        CharacterPoseOperationCode.AnimationSlot)
                    {
                        outputPolicy = operation.SelectionAvailability;
                    }
                    else
                    {
                        int controlIndex = operation.ControlInputOperationIndex;
                        if ((uint)controlIndex >= (uint)i)
                            throw new InvalidOperationException(
                                $"Animation Pose Graph Player operation #{i} has no compiled control input.");
                        CharacterPresentationPoseOperation control =
                            program.Operations[controlIndex];
                        outputPolicy = control.SelectionAvailability;
                    }
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
                    operation.RootOrientationWarpIndex,
                    operation.TwoBoneIkIndex,
                    operation.FootPlacementNodeIndex,
                    operation.StateMachineIndex,
                    operation.AnimationSlotIndex,
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

        void CompileStages(CharacterPresentationPosePlan program)
        {
            int nativeOperationStart = 0;
            for (int stageIndex = 0; stageIndex < program.Stages.Count; stageIndex++)
            {
                CharacterPresentationPoseStage stage = program.Stages[stageIndex];
                if (stage == null || stage.Index != stageIndex ||
                    stage.NativeOperationStart != nativeOperationStart)
                {
                    throw new InvalidOperationException(
                        $"Animation Pose Graph stage #{stageIndex} is not compact.");
                }
                m_Stages[stageIndex] = new AnimationPoseGraphNativeStage(stage);
                nativeOperationStart += stage.NativeOperationCount;
            }
            if (nativeOperationStart != m_Operations.Length)
                throw new InvalidOperationException("Animation Pose Graph native stages do not close the operation range.");
        }

        internal void RequireValid()
        {
            RequireAlive();
            if (m_BoneCount <= 0 || m_ParameterCount <= 0 || m_PoseValueCount <= 0 || m_ContributionStride <= 0 ||
                m_FrameCacheCount <= 0 || m_LeftFootBoneIndex < 0 || m_LeftFootBoneIndex >= m_BoneCount ||
                m_RightFootBoneIndex < 0 || m_RightFootBoneIndex >= m_BoneCount ||
                !m_Operations.IsCreated || m_Operations.Length <= 0 ||
                !m_Stages.IsCreated || m_Stages.Length <= 0 || !m_DenseBoneMasks.IsCreated ||
                !m_AdditiveReferences.IsCreated || !m_ParameterPolicies.IsCreated || !m_ParameterDefaults.IsCreated ||
                !m_ParentIndices.IsCreated || !m_BlendCurves.IsCreated ||
                !m_BlendCurveSegments.IsCreated || !m_BlendProfiles.IsCreated ||
                !m_BlendDenseProfiles.IsCreated ||
                m_BlendCurves.Length <= 0 || m_BlendCurveSegments.Length <= 0 ||
                m_BlendProfiles.Length <= 0 ||
                m_BlendDenseProfiles.Length != m_BlendProfiles.Length * m_BoneCount ||
                !m_ModifyBones.IsCreated ||
                !m_RootOrientationWarps.IsCreated || !m_RootOrientationWarpControls.IsCreated || !m_FootPlacementControls.IsCreated ||
                m_RootOrientationWarps.Length != m_RootOrientationWarpControls.Length ||
                !m_TwoBoneIks.IsCreated || !m_TwoBoneIkDiagnostics.IsCreated ||
                m_TwoBoneIkDiagnostics.Length != m_TwoBoneIks.Length ||
                !m_ConstraintComponentScratch.IsCreated || !m_StateMachineControls.IsCreated ||
                !m_AnimationSlotControls.IsCreated ||
                m_ConstraintComponentScratch.Length != m_BoneCount || !m_BoneCounts.IsValid ||
                m_PelvisBoneIndex < 0 || m_PelvisBoneIndex >= m_BoneCount ||
                !m_LeftLeg.IsValid(m_BoneCounts.PhysicalBoneCount, m_PelvisBoneIndex) ||
                !m_RightLeg.IsValid(m_BoneCounts.PhysicalBoneCount, m_PelvisBoneIndex) ||
                m_ParameterDefaults.Length != m_ParameterCount || m_ParentIndices.Length != m_BoneCount ||
                m_OutputNativeOperationIndex < 0 || m_OutputNativeOperationIndex >= m_Operations.Length ||
                m_OutputOperationIndex < 0 || m_OutputOperationIndex >= m_FrameCacheCount ||
                m_OutputValueIndex < 0 || m_OutputValueIndex >= m_PoseValueCount)
                throw new InvalidOperationException("Animation Pose Graph Native Program is invalid.");

            int nativeOperationStart = 0;
            int finalStageCount = 0;
            for (int stageIndex = 0; stageIndex < m_Stages.Length; stageIndex++)
            {
                AnimationPoseGraphNativeStage stage = m_Stages[stageIndex];
                if (stage.Index != stageIndex || stage.OperationStart != nativeOperationStart ||
                    stage.OperationCount < 0 || stage.OperationStart > m_Operations.Length - stage.OperationCount ||
                    stage.CompletionIndex != stageIndex || stage.DiagnosticIndex != stageIndex)
                {
                    throw new InvalidOperationException($"Animation Pose Graph Native stage #{stageIndex} is invalid.");
                }
                if (stage.ExecutionDomain == CharacterPoseExecutionDomain.FinalPublication)
                    finalStageCount++;
                nativeOperationStart += stage.OperationCount;
            }
            if (nativeOperationStart != m_Operations.Length || finalStageCount != 1 ||
                m_Stages[m_Stages.Length - 1].ExecutionDomain != CharacterPoseExecutionDomain.FinalPublication)
            {
                throw new InvalidOperationException("Animation Pose Graph Native stage table is incomplete.");
            }
        }

        internal static bool IsNativePoseOperation(CharacterPoseOperationCode code) => code switch
        {
            CharacterPoseOperationCode.SelectedPosePlayer => true,
            CharacterPoseOperationCode.BlendSpacePlayer => true,
            CharacterPoseOperationCode.SequencePlayer => true,
            CharacterPoseOperationCode.BlendStack => true,
            CharacterPoseOperationCode.AnimationSlot => true,
            CharacterPoseOperationCode.Inertialization => true,
            CharacterPoseOperationCode.BlendPose => true,
            CharacterPoseOperationCode.LayeredBoneBlend => true,
            CharacterPoseOperationCode.AdditivePose => true,
            CharacterPoseOperationCode.PoseParameterResolve => true,
            CharacterPoseOperationCode.ModifyBone => true,
            CharacterPoseOperationCode.RootOrientationWarp => true,
            CharacterPoseOperationCode.TwoBoneIK => true,
            CharacterPoseOperationCode.FootPlacement => true,
            CharacterPoseOperationCode.LocalToComponentPose => true,
            CharacterPoseOperationCode.ComponentToLocalPose => true,
            CharacterPoseOperationCode.StatePoseOutput => true,
            CharacterPoseOperationCode.PoseStateMachine => true,
            CharacterPoseOperationCode.OutputPose => true,
            _ => false
        };

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_CommittedPage != null)
                DisposePage(m_CommittedPage);
            else
                DisposePage(CaptureActivePage());
            DisposePage(m_PendingPage);
            m_CommittedPage = null;
            m_PendingPage = null;
            DisposeArray(ref m_TwoBoneIks);
            DisposeArray(ref m_RootOrientationWarps);
            DisposeArray(ref m_ModifyBones);
            DisposeArray(ref m_BlendDenseProfiles);
            DisposeArray(ref m_BlendProfiles);
            DisposeArray(ref m_BlendCurveSegments);
            DisposeArray(ref m_BlendCurves);
            DisposeArray(ref m_ParentIndices);
            DisposeArray(ref m_ParameterDefaults);
            DisposeArray(ref m_ParameterPolicies);
            DisposeArray(ref m_AdditiveReferences);
            DisposeArray(ref m_DenseBoneMasks);
            DisposeArray(ref m_Stages);
            DisposeArray(ref m_Operations);
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseGraphNativeProgram));
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Character Pose Graph frame is not open.");
        }

        static NativeArray<T> Allocate<T>(int length) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        static NativeArray<T> AllocateClear<T>(int length) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Page CaptureActivePage() => new Page
        {
            TwoBoneIkDiagnostics = m_TwoBoneIkDiagnostics,
            ConstraintComponentScratch = m_ConstraintComponentScratch,
            StateMachineControls = m_StateMachineControls,
            AnimationSlotControls = m_AnimationSlotControls,
            RootOrientationWarpControls = m_RootOrientationWarpControls,
            FootPlacementControls = m_FootPlacementControls
        };

        Page AllocatePage()
        {
            var page = new Page();
            try
            {
                page.TwoBoneIkDiagnostics = AllocateClear<CharacterTwoBoneIkRuntimeDiagnostic>(m_TwoBoneIkDiagnostics.Length);
                page.ConstraintComponentScratch = Allocate<CharacterComponentBonePose>(m_ConstraintComponentScratch.Length);
                page.StateMachineControls = Allocate<CharacterPoseStateMachineNativeControl>(m_StateMachineControls.Length);
                page.AnimationSlotControls = Allocate<CharacterAnimationSlotNativeControl>(m_AnimationSlotControls.Length);
                page.RootOrientationWarpControls = AllocateClear<CharacterRootOrientationWarpNativeControl>(m_RootOrientationWarpControls.Length);
                page.FootPlacementControls = AllocateClear<CharacterFootPlacementNativeControl>(m_FootPlacementControls.Length);
                for (int i = 0; i < page.FootPlacementControls.Length; i++)
                    page.FootPlacementControls[i] = CharacterFootPlacementNativeControl.Inactive;
                return page;
            }
            catch
            {
                DisposePage(page);
                throw;
            }
        }

        void BindPage(Page page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            m_TwoBoneIkDiagnostics = page.TwoBoneIkDiagnostics;
            m_ConstraintComponentScratch = page.ConstraintComponentScratch;
            m_StateMachineControls = page.StateMachineControls;
            m_AnimationSlotControls = page.AnimationSlotControls;
            m_RootOrientationWarpControls = page.RootOrientationWarpControls;
            m_FootPlacementControls = page.FootPlacementControls;
        }

        static void DisposePage(Page page)
        {
            if (page == null)
                return;
            DisposeArray(ref page.FootPlacementControls);
            DisposeArray(ref page.RootOrientationWarpControls);
            DisposeArray(ref page.AnimationSlotControls);
            DisposeArray(ref page.StateMachineControls);
            DisposeArray(ref page.ConstraintComponentScratch);
            DisposeArray(ref page.TwoBoneIkDiagnostics);
        }

        static void DisposeArray<T>(ref NativeArray<T> values) where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
