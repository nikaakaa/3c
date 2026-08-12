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
            int outputFullBodyIkGoalSetValueIndex,
            int inputPoseValueIndexA,
            int inputPoseValueIndexB,
            int fullBodyIkGoalInputStart,
            int fullBodyIkGoalInputCount,
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
            int poseBoneIkGoalsIndex,
            int footGroundingIndex,
            int fullBodyIkIndex,
            int stateMachineIndex,
            int animationSlotIndex,
            int linkedPoseCallIndex,
            int linkedPoseFragmentIndex,
            int frameCacheIndex,
            float weight)
        {
            if (index < 0 ||
                !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) ||
                outputPoseValueIndex < -1 || outputFullBodyIkGoalSetValueIndex < -1 ||
                fullBodyIkGoalInputStart < -1 || fullBodyIkGoalInputCount < 0 ||
                frameCacheIndex != index ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentException("Animation Pose Graph Native operation header is invalid.");
            Index = index;
            Code = code;
            OutputValueIndex = outputPoseValueIndex;
            OutputFullBodyIkGoalSetValueIndex = outputFullBodyIkGoalSetValueIndex;
            InputValueIndexA = inputPoseValueIndexA;
            InputValueIndexB = inputPoseValueIndexB;
            FullBodyIkGoalInputStart = fullBodyIkGoalInputStart;
            FullBodyIkGoalInputCount = fullBodyIkGoalInputCount;
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
            PoseBoneIkGoalsIndex = poseBoneIkGoalsIndex;
            FootGroundingIndex = footGroundingIndex;
            FullBodyIkIndex = fullBodyIkIndex;
            StateMachineIndex = stateMachineIndex;
            AnimationSlotIndex = animationSlotIndex;
            LinkedPoseCallIndex = linkedPoseCallIndex;
            LinkedPoseFragmentIndex = linkedPoseFragmentIndex;
            FrameCacheIndex = frameCacheIndex;
            Weight = weight;
        }

        internal int Index { get; }
        internal CharacterPoseOperationCode Code { get; }
        internal int OutputValueIndex { get; }
        internal int OutputFullBodyIkGoalSetValueIndex { get; }
        internal int InputValueIndexA { get; }
        internal int InputValueIndexB { get; }
        internal int FullBodyIkGoalInputStart { get; }
        internal int FullBodyIkGoalInputCount { get; }
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
        internal int PoseBoneIkGoalsIndex { get; }
        internal int FootGroundingIndex { get; }
        internal int FullBodyIkIndex { get; }
        internal int StateMachineIndex { get; }
        internal int AnimationSlotIndex { get; }
        internal int LinkedPoseCallIndex { get; }
        internal int LinkedPoseFragmentIndex { get; }
        internal int FrameCacheIndex { get; }
        internal float Weight { get; }

        internal AnimationPoseGraphNativeOperation WithWeight(float value) => new AnimationPoseGraphNativeOperation(
            Index,
            Code,
            OutputValueIndex,
            OutputFullBodyIkGoalSetValueIndex,
            InputValueIndexA,
            InputValueIndexB,
            FullBodyIkGoalInputStart,
            FullBodyIkGoalInputCount,
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
            PoseBoneIkGoalsIndex,
            FootGroundingIndex,
            FullBodyIkIndex,
            StateMachineIndex,
            AnimationSlotIndex,
            LinkedPoseCallIndex,
            LinkedPoseFragmentIndex,
            FrameCacheIndex,
            value);

        internal AnimationPoseGraphNativeOperation WithBlendInputs(
            int sourcePoseValueIndex,
            int targetPoseValueIndex,
            float targetWeight) => new AnimationPoseGraphNativeOperation(
            Index,
            CharacterPoseOperationCode.PoseStateMachine,
            OutputValueIndex,
            OutputFullBodyIkGoalSetValueIndex,
            sourcePoseValueIndex,
            targetPoseValueIndex,
            FullBodyIkGoalInputStart,
            FullBodyIkGoalInputCount,
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
            PoseBoneIkGoalsIndex,
            FootGroundingIndex,
            FullBodyIkIndex,
            StateMachineIndex,
            AnimationSlotIndex,
            LinkedPoseCallIndex,
            LinkedPoseFragmentIndex,
            FrameCacheIndex,
            targetWeight);
    }

    internal readonly struct AnimationPoseGraphNativeLinkedPoseCall
    {
        internal AnimationPoseGraphNativeLinkedPoseCall(int candidateStart, int candidateCount)
        {
            if (candidateStart < 0 || candidateCount <= 0)
                throw new ArgumentException("Linked Pose native call range is invalid.");
            CandidateStart = candidateStart;
            CandidateCount = candidateCount;
        }

        internal int CandidateStart { get; }
        internal int CandidateCount { get; }
    }

    internal readonly struct AnimationPoseGraphNativeLinkedPoseCandidate
    {
        internal AnimationPoseGraphNativeLinkedPoseCandidate(
            int fragmentIndex,
            int outputPoseValueIndex,
            int outputFullBodyIkGoalSetValueIndex)
        {
            if (fragmentIndex < 0 || outputPoseValueIndex < -1 ||
                outputFullBodyIkGoalSetValueIndex < -1 ||
                outputPoseValueIndex < 0 && outputFullBodyIkGoalSetValueIndex < 0)
            {
                throw new ArgumentException("Linked Pose native candidate output is invalid.");
            }
            FragmentIndex = fragmentIndex;
            OutputPoseValueIndex = outputPoseValueIndex;
            OutputFullBodyIkGoalSetValueIndex = outputFullBodyIkGoalSetValueIndex;
        }

        internal int FragmentIndex { get; }
        internal int OutputPoseValueIndex { get; }
        internal int OutputFullBodyIkGoalSetValueIndex { get; }
    }

    internal readonly struct AnimationPoseGraphNativeLinkedPoseCallControl
    {
        internal AnimationPoseGraphNativeLinkedPoseCallControl(
            int candidateIndex,
            ulong generation,
            bool poseDiscontinuity)
        {
            if (candidateIndex < 0 || generation == 0)
                throw new ArgumentException("Linked Pose native call control is invalid.");
            CandidateIndex = candidateIndex;
            Generation = generation;
            PoseDiscontinuity = poseDiscontinuity ? (byte)1 : (byte)0;
        }

        internal int CandidateIndex { get; }
        internal ulong Generation { get; }
        internal byte PoseDiscontinuity { get; }
        internal bool IsActive => CandidateIndex >= 0 && Generation != 0 && PoseDiscontinuity <= 1;
        internal static AnimationPoseGraphNativeLinkedPoseCallControl Inactive =>
            new AnimationPoseGraphNativeLinkedPoseCallControl(-1, 0, 0);

        AnimationPoseGraphNativeLinkedPoseCallControl(
            int candidateIndex,
            ulong generation,
            byte poseDiscontinuity)
        {
            CandidateIndex = candidateIndex;
            Generation = generation;
            PoseDiscontinuity = poseDiscontinuity;
        }
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

    internal readonly struct AnimationPoseGraphNativePoseBoneIkGoalRange
    {
        internal AnimationPoseGraphNativePoseBoneIkGoalRange(
            int descriptorOffset,
            int descriptorCount,
            int goalWorkspaceOffset)
        {
            if (descriptorOffset < 0 || descriptorCount <= 0 || goalWorkspaceOffset < 0)
                throw new ArgumentException("Pose Bone IK Goal native range is invalid.");
            DescriptorOffset = descriptorOffset;
            DescriptorCount = descriptorCount;
            GoalWorkspaceOffset = goalWorkspaceOffset;
        }

        internal int DescriptorOffset { get; }
        internal int DescriptorCount { get; }
        internal int GoalWorkspaceOffset { get; }
    }

    internal sealed class CharacterPoseGraphNativeProgram : IDisposable
    {
        sealed class Page
        {
            internal NativeArray<CharacterPoseStateMachineNativeControl>
                StateMachineControls;
            internal NativeArray<CharacterAnimationSlotNativeControl>
                AnimationSlotControls;
            internal NativeArray<CharacterRootOrientationWarpNativeControl>
                RootOrientationWarpControls;
            internal NativeArray<CharacterFullBodyIkGoalSetHeader>
                FullBodyIkGoalSets;
            internal NativeArray<CharacterFullBodyIkGoal>
                FullBodyIkGoals;
            internal NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
                LinkedPoseCallControls;
            internal NativeArray<byte>
                LinkedPoseActiveFragments;
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
        NativeArray<CharacterVirtualBoneDescriptor> m_VirtualBones;
        NativeArray<AnimationPoseGraphNativePoseBoneIkGoalRange> m_PoseBoneIkGoalRanges;
        NativeArray<CharacterPoseBoneIkGoalDescriptor> m_PoseBoneIkGoalDescriptors;
        NativeArray<int> m_FullBodyIkGoalInputValueIndices;
        NativeArray<CharacterFullBodyIkGoalSetHeader> m_FullBodyIkGoalSets;
        NativeArray<CharacterFullBodyIkGoal> m_FullBodyIkGoals;
        NativeArray<AnimationPoseGraphNativeLinkedPoseCall> m_LinkedPoseCalls;
        NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate> m_LinkedPoseCandidates;
        NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl> m_LinkedPoseCallControls;
        NativeArray<byte> m_LinkedPoseActiveFragments;
        LinkedPoseGroupId[] m_LinkedPoseCallGroupIds;
        LinkedPoseInterfaceId[] m_LinkedPoseCallInterfaceIds;
        LinkedPoseImplementationId[] m_LinkedPoseCandidateImplementationIds;
        NativeArray<CharacterPoseStateMachineNativeControl> m_StateMachineControls;
        NativeArray<CharacterAnimationSlotNativeControl> m_AnimationSlotControls;
        Page m_CommittedPage;
        Page m_PendingPage;
        CharacterPoseBoneCounts m_BoneCounts;
        int m_BoneCount;
        int m_ParameterCount;
        int m_PoseValueCount;
        int m_FootGroundingCount;
        int m_FullBodyIkCount;
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
        FixedString64Bytes m_RigId;
        FixedString64Bytes m_RigRevision;
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
                m_FootGroundingCount = program.FootGroundings.Count;
                m_FullBodyIkCount = program.FullBodyIks.Count;
                m_ContributionStride = program.ContributionWorkspaceCount / program.PoseValueWorkspaceCount;
                m_FrameCacheCount = program.FrameCacheCount;
                m_OutputOperationIndex = program.OutputOperationIndex;
                m_LeftFootBoneIndex = rig.LeftLeg.AnklePhysicalBoneIndex;
                m_RightFootBoneIndex = rig.RightLeg.AnklePhysicalBoneIndex;
                m_PelvisBoneIndex = rig.PelvisPhysicalBoneIndex;
                m_LeftLeg = new AnimationPoseGraphNativeLegChain(rig.LeftLeg);
                m_RightLeg = new AnimationPoseGraphNativeLegChain(rig.RightLeg);
                m_RigId = new FixedString64Bytes(rig.RigId);
                m_RigRevision = new FixedString64Bytes(rig.RigRevision);

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
                m_VirtualBones = Allocate<CharacterVirtualBoneDescriptor>(rig.VirtualBoneCount);
                m_PoseBoneIkGoalRanges = Allocate<AnimationPoseGraphNativePoseBoneIkGoalRange>(program.PoseBoneIkGoalSources.Count);
                int poseBoneGoalCount = program.PoseBoneIkGoalSources.Sum(value => value.GoalCount);
                m_PoseBoneIkGoalDescriptors = Allocate<CharacterPoseBoneIkGoalDescriptor>(poseBoneGoalCount);
                m_FullBodyIkGoalInputValueIndices = Allocate<int>(program.FullBodyIkGoalInputValueIndices.Count);
                m_FullBodyIkGoalSets = AllocateClear<CharacterFullBodyIkGoalSetHeader>(program.FullBodyIkGoalSetWorkspaceCount);
                m_FullBodyIkGoals = AllocateClear<CharacterFullBodyIkGoal>(program.FullBodyIkGoalWorkspaceCount);
                int linkedPoseCandidateCount = program.LinkedPoseCalls.Sum(value => value.FragmentIndices.Count);
                m_LinkedPoseCalls = Allocate<AnimationPoseGraphNativeLinkedPoseCall>(program.LinkedPoseCalls.Count);
                m_LinkedPoseCandidates = Allocate<AnimationPoseGraphNativeLinkedPoseCandidate>(linkedPoseCandidateCount);
                m_LinkedPoseCallControls = Allocate<AnimationPoseGraphNativeLinkedPoseCallControl>(program.LinkedPoseCalls.Count);
                m_LinkedPoseActiveFragments = AllocateClear<byte>(program.LinkedPoseFragments.Count);
                m_LinkedPoseCallGroupIds = new LinkedPoseGroupId[program.LinkedPoseCalls.Count];
                m_LinkedPoseCallInterfaceIds = new LinkedPoseInterfaceId[program.LinkedPoseCalls.Count];
                m_LinkedPoseCandidateImplementationIds = new LinkedPoseImplementationId[linkedPoseCandidateCount];
                for (int i = 0; i < m_LinkedPoseCallControls.Length; i++)
                    m_LinkedPoseCallControls[i] = AnimationPoseGraphNativeLinkedPoseCallControl.Inactive;
                m_StateMachineControls = Allocate<CharacterPoseStateMachineNativeControl>(program.StateMachines.Count);
                m_AnimationSlotControls = Allocate<CharacterAnimationSlotNativeControl>(program.AnimationSlots.Count);

                CompileRig(program, rig);
                CompileBlendCatalogs(curves, profiles);
                CompilePayloads(program);
                CompileLinkedPose(program);
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
        internal int FootGroundingCount => m_FootGroundingCount;
        internal int FullBodyIkCount => m_FullBodyIkCount;
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
        internal FixedString64Bytes RigId => m_RigId;
        internal FixedString64Bytes RigRevision => m_RigRevision;
        internal NativeArray<AnimationPoseGraphNativeOperation> Operations => m_Operations;

        internal void SetOperationWeight(int operationIndex, float weight)
        {
            RequireAlive();
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentOutOfRangeException(nameof(weight));
            for (int i = 0; i < m_Operations.Length; i++)
            {
                if (m_Operations[i].Index != operationIndex)
                    continue;
                m_Operations[i] = m_Operations[i].WithWeight(weight);
                return;
            }
            throw new InvalidOperationException(
                $"Pose tuning operation '{operationIndex}' has no native operation.");
        }

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
        internal NativeArray<CharacterVirtualBoneDescriptor> VirtualBones => m_VirtualBones;
        internal NativeArray<AnimationPoseGraphNativePoseBoneIkGoalRange> PoseBoneIkGoalRanges => m_PoseBoneIkGoalRanges;
        internal NativeArray<CharacterPoseBoneIkGoalDescriptor> PoseBoneIkGoalDescriptors => m_PoseBoneIkGoalDescriptors;
        internal NativeArray<int> FullBodyIkGoalInputValueIndices => m_FullBodyIkGoalInputValueIndices;
        internal NativeArray<CharacterFullBodyIkGoalSetHeader> FullBodyIkGoalSets => m_FullBodyIkGoalSets;
        internal NativeArray<CharacterFullBodyIkGoal> FullBodyIkGoals => m_FullBodyIkGoals;
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCall> LinkedPoseCalls => m_LinkedPoseCalls;
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate> LinkedPoseCandidates => m_LinkedPoseCandidates;
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl> LinkedPoseCallControls => m_LinkedPoseCallControls;
        internal NativeArray<byte> LinkedPoseActiveFragments => m_LinkedPoseActiveFragments;
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
            for (int i = 0; i < m_FullBodyIkGoalSets.Length; i++)
                m_FullBodyIkGoalSets[i] = default;
            for (int i = 0; i < m_FullBodyIkGoals.Length; i++)
                m_FullBodyIkGoals[i] = default;
            for (int i = 0; i < m_LinkedPoseCallControls.Length; i++)
                m_LinkedPoseCallControls[i] = AnimationPoseGraphNativeLinkedPoseCallControl.Inactive;
            for (int i = 0; i < m_LinkedPoseActiveFragments.Length; i++)
                m_LinkedPoseActiveFragments[i] = 0;
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

        internal void SetLinkedPoseGroupSelection(
            in CharacterLinkedPoseGenerationHandle selection)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!selection.IsValid)
                throw new ArgumentException("Linked Pose generation selection is invalid.", nameof(selection));
            int matchingCallCount = 0;
            for (int callIndex = 0; callIndex < m_LinkedPoseCalls.Length; callIndex++)
            {
                if (m_LinkedPoseCallGroupIds[callIndex] != selection.GroupId)
                    continue;
                matchingCallCount++;
                if (m_LinkedPoseCallInterfaceIds[callIndex] != selection.InterfaceId ||
                    m_LinkedPoseCallControls[callIndex].IsActive ||
                    FindLinkedPoseCandidate(callIndex, selection.ImplementationId) < 0)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose Group '{selection.GroupId}' selection does not match call #{callIndex}.");
                }
            }
            if (matchingCallCount == 0)
                throw new InvalidOperationException($"Linked Pose Group '{selection.GroupId}' has no compiled calls.");
            for (int callIndex = 0; callIndex < m_LinkedPoseCalls.Length; callIndex++)
            {
                if (m_LinkedPoseCallGroupIds[callIndex] != selection.GroupId)
                    continue;
                int candidateIndex = FindLinkedPoseCandidate(callIndex, selection.ImplementationId);
                AnimationPoseGraphNativeLinkedPoseCandidate candidate = m_LinkedPoseCandidates[candidateIndex];
                m_LinkedPoseCallControls[callIndex] = new AnimationPoseGraphNativeLinkedPoseCallControl(
                    candidateIndex,
                    selection.Generation,
                    selection.PoseDiscontinuity);
                m_LinkedPoseActiveFragments[candidate.FragmentIndex] = 1;
            }
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
            for (int virtualIndex = 0; virtualIndex < rig.VirtualBoneCount; virtualIndex++)
            {
                CharacterAnimationVirtualBonePayload bone = rig.VirtualBones[virtualIndex];
                m_VirtualBones[virtualIndex] = new CharacterVirtualBoneDescriptor(
                    new CharacterPoseBoneRuntimeId(bone.VirtualBoneId),
                    bone.SourcePhysicalBoneIndex,
                    bone.TargetPhysicalBoneIndex,
                    bone.PoseBoneIndex);
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
            int poseBoneGoalOffset = 0;
            for (int sourceIndex = 0; sourceIndex < program.PoseBoneIkGoalSources.Count; sourceIndex++)
            {
                CharacterPresentationPoseBoneIkGoalsDescriptor source = program.PoseBoneIkGoalSources[sourceIndex];
                m_PoseBoneIkGoalRanges[sourceIndex] = new AnimationPoseGraphNativePoseBoneIkGoalRange(
                    poseBoneGoalOffset,
                    source.GoalCount,
                    source.GoalWorkspaceOffset);
                for (int goalIndex = 0; goalIndex < source.GoalCount; goalIndex++)
                {
                    CharacterPresentationPoseBoneIkGoalBindingDescriptor binding = source.Bindings[goalIndex];
                    m_PoseBoneIkGoalDescriptors[poseBoneGoalOffset++] = new CharacterPoseBoneIkGoalDescriptor(
                        binding.EffectorSlot,
                        binding.TargetPoseBoneIndex,
                        binding.PositionOffset,
                        binding.RotationOffset,
                        binding.PositionWeight,
                        binding.RotationWeight);
                }
            }
            if (poseBoneGoalOffset != m_PoseBoneIkGoalDescriptors.Length)
                throw new InvalidOperationException("Pose Bone IK Goal native descriptor layout is inconsistent.");
            for (int inputIndex = 0; inputIndex < m_FullBodyIkGoalInputValueIndices.Length; inputIndex++)
                m_FullBodyIkGoalInputValueIndices[inputIndex] = program.FullBodyIkGoalInputValueIndices[inputIndex];
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

        void CompileLinkedPose(CharacterPresentationPosePlan program)
        {
            int candidateIndex = 0;
            for (int callIndex = 0; callIndex < program.LinkedPoseCalls.Count; callIndex++)
            {
                CharacterLinkedPoseCallPlanDescriptor call = program.LinkedPoseCalls[callIndex];
                if (call == null || call.Index != callIndex)
                    throw new InvalidOperationException($"Linked Pose native call #{callIndex} is invalid.");
                CharacterPresentationPoseOperation callOperation = program.Operations
                    .Single(value => value.Code == CharacterPoseOperationCode.LinkedPoseCall &&
                                     value.LinkedPoseCallIndex == callIndex);
                int candidateStart = candidateIndex;
                m_LinkedPoseCallGroupIds[callIndex] = call.GroupId;
                m_LinkedPoseCallInterfaceIds[callIndex] = call.InterfaceId;
                for (int offset = 0; offset < call.FragmentIndices.Count; offset++)
                {
                    int fragmentIndex = call.FragmentIndices[offset];
                    if ((uint)fragmentIndex >= (uint)program.LinkedPoseFragments.Count)
                        throw new InvalidOperationException($"Linked Pose native call #{callIndex} references an invalid fragment.");
                    CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = program.LinkedPoseFragments[fragmentIndex];
                    int outputPoseValueIndex = -1;
                    int outputGoalSetValueIndex = -1;
                    for (int outputIndex = 0; outputIndex < fragment.Outputs.Count; outputIndex++)
                    {
                        CharacterLinkedPosePortValueBinding output = fragment.Outputs[outputIndex];
                        if (output.Kind == CharacterPosePortKind.LocalPose ||
                            output.Kind == CharacterPosePortKind.ComponentPose)
                        {
                            if (outputPoseValueIndex >= 0 && outputPoseValueIndex != output.ValueIndex)
                                throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} has multiple Pose outputs.");
                            outputPoseValueIndex = output.ValueIndex;
                        }
                        else if (output.Kind == CharacterPosePortKind.FullBodyIkGoals)
                        {
                            if (outputGoalSetValueIndex >= 0 && outputGoalSetValueIndex != output.ValueIndex)
                                throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} has multiple Goal Set outputs.");
                            outputGoalSetValueIndex = output.ValueIndex;
                        }
                    }
                    if ((callOperation.OutputValueIndex >= 0) != (outputPoseValueIndex >= 0) ||
                        (callOperation.OutputFullBodyIkGoalSetValueIndex >= 0) != (outputGoalSetValueIndex >= 0))
                    {
                        throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} output ABI does not match call #{callIndex}.");
                    }
                    m_LinkedPoseCandidates[candidateIndex] = new AnimationPoseGraphNativeLinkedPoseCandidate(
                        fragmentIndex,
                        outputPoseValueIndex,
                        outputGoalSetValueIndex);
                    m_LinkedPoseCandidateImplementationIds[candidateIndex] = fragment.ImplementationId;
                    candidateIndex++;
                }
                m_LinkedPoseCalls[callIndex] = new AnimationPoseGraphNativeLinkedPoseCall(
                    candidateStart,
                    candidateIndex - candidateStart);
            }
            if (candidateIndex != m_LinkedPoseCandidates.Length)
                throw new InvalidOperationException("Linked Pose native candidate layout is incomplete.");
        }

        int FindLinkedPoseCandidate(
            int callIndex,
            LinkedPoseImplementationId implementationId)
        {
            if ((uint)callIndex >= (uint)m_LinkedPoseCalls.Length || !implementationId.IsValid)
                return -1;
            AnimationPoseGraphNativeLinkedPoseCall call = m_LinkedPoseCalls[callIndex];
            for (int candidateIndex = call.CandidateStart;
                 candidateIndex < call.CandidateStart + call.CandidateCount;
                 candidateIndex++)
            {
                if (m_LinkedPoseCandidateImplementationIds[candidateIndex] == implementationId)
                    return candidateIndex;
            }
            return -1;
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
                    operation.OutputFullBodyIkGoalSetValueIndex,
                    operation.InputValueIndexA,
                    operation.InputValueIndexB,
                    operation.FullBodyIkGoalInputStart,
                    operation.FullBodyIkGoalInputCount,
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
                    operation.PoseBoneIkGoalsIndex,
                    operation.FootGroundingIndex,
                    operation.FullBodyIkIndex,
                    operation.StateMachineIndex,
                    operation.AnimationSlotIndex,
                    operation.LinkedPoseCallIndex,
                    operation.LinkedPoseFragmentIndex,
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
            if (m_BoneCount <= 0 || m_ParameterCount <= 0 || m_PoseValueCount <= 0 ||
                m_FootGroundingCount < 0 ||
                m_FullBodyIkCount < 0 || m_ContributionStride <= 0 ||
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
                !m_RootOrientationWarps.IsCreated || !m_RootOrientationWarpControls.IsCreated ||
                m_RootOrientationWarps.Length != m_RootOrientationWarpControls.Length ||
                !m_VirtualBones.IsCreated || m_VirtualBones.Length != m_BoneCounts.VirtualBoneCount ||
                !m_PoseBoneIkGoalRanges.IsCreated || !m_PoseBoneIkGoalDescriptors.IsCreated ||
                !m_FullBodyIkGoalInputValueIndices.IsCreated ||
                !m_FullBodyIkGoalSets.IsCreated || !m_FullBodyIkGoals.IsCreated ||
                !m_LinkedPoseCalls.IsCreated || !m_LinkedPoseCandidates.IsCreated ||
                !m_LinkedPoseCallControls.IsCreated || !m_LinkedPoseActiveFragments.IsCreated ||
                m_LinkedPoseCallGroupIds == null ||
                m_LinkedPoseCallGroupIds.Length != m_LinkedPoseCalls.Length ||
                m_LinkedPoseCallInterfaceIds == null ||
                m_LinkedPoseCallInterfaceIds.Length != m_LinkedPoseCalls.Length ||
                m_LinkedPoseCandidateImplementationIds == null ||
                m_LinkedPoseCandidateImplementationIds.Length != m_LinkedPoseCandidates.Length ||
                !m_StateMachineControls.IsCreated ||
                !m_AnimationSlotControls.IsCreated ||
                !m_BoneCounts.IsValid ||
                m_PelvisBoneIndex < 0 || m_PelvisBoneIndex >= m_BoneCount ||
                !m_LeftLeg.IsValid(m_BoneCounts.PhysicalBoneCount, m_PelvisBoneIndex) ||
                !m_RightLeg.IsValid(m_BoneCounts.PhysicalBoneCount, m_PelvisBoneIndex) ||
                m_ParameterDefaults.Length != m_ParameterCount || m_ParentIndices.Length != m_BoneCount ||
                m_RigId.Length == 0 || m_RigRevision.Length == 0 ||
                m_OutputNativeOperationIndex < 0 || m_OutputNativeOperationIndex >= m_Operations.Length ||
                m_OutputOperationIndex < 0 || m_OutputOperationIndex >= m_FrameCacheCount ||
                m_OutputValueIndex < 0 || m_OutputValueIndex >= m_PoseValueCount)
                throw new InvalidOperationException("Animation Pose Graph Native Program is invalid.");

            int linkedCandidateStart = 0;
            for (int callIndex = 0; callIndex < m_LinkedPoseCalls.Length; callIndex++)
            {
                AnimationPoseGraphNativeLinkedPoseCall call = m_LinkedPoseCalls[callIndex];
                if (call.CandidateStart != linkedCandidateStart || call.CandidateCount <= 0 ||
                    !m_LinkedPoseCallGroupIds[callIndex].IsValid ||
                    !m_LinkedPoseCallInterfaceIds[callIndex].IsValid)
                {
                    throw new InvalidOperationException($"Linked Pose native call #{callIndex} is invalid.");
                }
                linkedCandidateStart += call.CandidateCount;
            }
            if (linkedCandidateStart != m_LinkedPoseCandidates.Length)
                throw new InvalidOperationException("Linked Pose native candidate ranges are incomplete.");
            for (int candidateIndex = 0; candidateIndex < m_LinkedPoseCandidates.Length; candidateIndex++)
            {
                AnimationPoseGraphNativeLinkedPoseCandidate candidate = m_LinkedPoseCandidates[candidateIndex];
                if ((uint)candidate.FragmentIndex >= (uint)m_LinkedPoseActiveFragments.Length ||
                    candidate.OutputPoseValueIndex < -1 || candidate.OutputPoseValueIndex >= m_PoseValueCount ||
                    candidate.OutputFullBodyIkGoalSetValueIndex < -1 ||
                    candidate.OutputFullBodyIkGoalSetValueIndex >= m_FullBodyIkGoalSets.Length ||
                    candidate.OutputPoseValueIndex < 0 && candidate.OutputFullBodyIkGoalSetValueIndex < 0 ||
                    !m_LinkedPoseCandidateImplementationIds[candidateIndex].IsValid)
                {
                    throw new InvalidOperationException($"Linked Pose native candidate #{candidateIndex} is invalid.");
                }
            }

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
            CharacterPoseOperationCode.FootPlacement => true,
            CharacterPoseOperationCode.PoseBoneIKGoals => true,
            CharacterPoseOperationCode.FullBodyIK => true,
            CharacterPoseOperationCode.LinkedPoseCall => true,
            CharacterPoseOperationCode.EmptyFullBodyIkGoals => true,
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
            m_LinkedPoseCallGroupIds = null;
            m_LinkedPoseCallInterfaceIds = null;
            m_LinkedPoseCandidateImplementationIds = null;
            DisposeArray(ref m_LinkedPoseCandidates);
            DisposeArray(ref m_LinkedPoseCalls);
            DisposeArray(ref m_FullBodyIkGoalInputValueIndices);
            DisposeArray(ref m_PoseBoneIkGoalDescriptors);
            DisposeArray(ref m_PoseBoneIkGoalRanges);
            DisposeArray(ref m_VirtualBones);
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
            StateMachineControls = m_StateMachineControls,
            AnimationSlotControls = m_AnimationSlotControls,
            RootOrientationWarpControls = m_RootOrientationWarpControls,
            FullBodyIkGoalSets = m_FullBodyIkGoalSets,
            FullBodyIkGoals = m_FullBodyIkGoals,
            LinkedPoseCallControls = m_LinkedPoseCallControls,
            LinkedPoseActiveFragments = m_LinkedPoseActiveFragments
        };

        Page AllocatePage()
        {
            var page = new Page();
            try
            {
                page.StateMachineControls = Allocate<CharacterPoseStateMachineNativeControl>(m_StateMachineControls.Length);
                page.AnimationSlotControls = Allocate<CharacterAnimationSlotNativeControl>(m_AnimationSlotControls.Length);
                page.RootOrientationWarpControls = AllocateClear<CharacterRootOrientationWarpNativeControl>(m_RootOrientationWarpControls.Length);
                page.FullBodyIkGoalSets = AllocateClear<CharacterFullBodyIkGoalSetHeader>(m_FullBodyIkGoalSets.Length);
                page.FullBodyIkGoals = AllocateClear<CharacterFullBodyIkGoal>(m_FullBodyIkGoals.Length);
                page.LinkedPoseCallControls = Allocate<AnimationPoseGraphNativeLinkedPoseCallControl>(m_LinkedPoseCallControls.Length);
                for (int i = 0; i < page.LinkedPoseCallControls.Length; i++)
                    page.LinkedPoseCallControls[i] = AnimationPoseGraphNativeLinkedPoseCallControl.Inactive;
                page.LinkedPoseActiveFragments = AllocateClear<byte>(m_LinkedPoseActiveFragments.Length);
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
            m_StateMachineControls = page.StateMachineControls;
            m_AnimationSlotControls = page.AnimationSlotControls;
            m_RootOrientationWarpControls = page.RootOrientationWarpControls;
            m_FullBodyIkGoalSets = page.FullBodyIkGoalSets;
            m_FullBodyIkGoals = page.FullBodyIkGoals;
            m_LinkedPoseCallControls = page.LinkedPoseCallControls;
            m_LinkedPoseActiveFragments = page.LinkedPoseActiveFragments;
        }

        static void DisposePage(Page page)
        {
            if (page == null)
                return;
            DisposeArray(ref page.LinkedPoseActiveFragments);
            DisposeArray(ref page.LinkedPoseCallControls);
            DisposeArray(ref page.FullBodyIkGoals);
            DisposeArray(ref page.FullBodyIkGoalSets);
            DisposeArray(ref page.RootOrientationWarpControls);
            DisposeArray(ref page.AnimationSlotControls);
            DisposeArray(ref page.StateMachineControls);
        }

        static void DisposeArray<T>(ref NativeArray<T> values) where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
