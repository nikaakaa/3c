using System;
using System.Collections.Generic;
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
            int outputFullBodyIkGoalContributionValueIndex,
            int outputFullBodyIkGoalSetValueIndex,
            int inputFullBodyIkGoalSetValueIndex,
            int inputPoseValueIndexA,
            int inputPoseValueIndexB,
            int fullBodyIkGoalContributionInputStart,
            int fullBodyIkGoalContributionInputCount,
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
            CharacterPoseBoneContributionConstraintHandle
                poseBoneContribution,
            CharacterFootPlacementConstraintHandle
                footPlacementConstraint,
            CharacterFullBodyIkGoalAssemblerConstraintHandle
                goalAssemblerConstraint,
            CharacterFullBodyIkConstraintHandle fullBodyIkConstraint,
            int stateMachineIndex,
            int animationSlotIndex,
            int linkedPoseCallIndex,
            int linkedPoseFragmentIndex,
            int frameCacheIndex,
            float weight)
        {
            bool isPoseBoneContribution =
                code == CharacterPoseOperationCode.PoseBoneIKGoals;
            bool isFootPlacement =
                code == CharacterPoseOperationCode.FootPlacement;
            bool isGoalAssembler =
                code ==
                CharacterPoseOperationCode.FullBodyIkGoalAssembler;
            bool isFullBodyIk =
                code == CharacterPoseOperationCode.FullBodyIK;
            if (index < 0 ||
                !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) ||
                outputPoseValueIndex < -1 ||
                outputFullBodyIkGoalContributionValueIndex < -1 ||
                outputFullBodyIkGoalSetValueIndex < -1 ||
                inputFullBodyIkGoalSetValueIndex < -1 ||
                fullBodyIkGoalContributionInputStart < -1 ||
                fullBodyIkGoalContributionInputCount < 0 ||
                frameCacheIndex != index ||
                isPoseBoneContribution != poseBoneContribution.IsValid ||
                isPoseBoneContribution &&
                (poseBoneContribution.OperationIndex != index ||
                 poseBoneContribution.CallSiteIndex != frameCacheIndex ||
                 poseBoneContribution.InputPoseValueIndex !=
                 inputPoseValueIndexA ||
                 poseBoneContribution.ContributionValueIndex !=
                 outputFullBodyIkGoalContributionValueIndex) ||
                isFootPlacement != footPlacementConstraint.IsValid ||
                isFootPlacement &&
                (footPlacementConstraint.OperationIndex != index ||
                 footPlacementConstraint.CallSiteIndex != frameCacheIndex ||
                 footPlacementConstraint.ContributionValueIndex !=
                 outputFullBodyIkGoalContributionValueIndex) ||
                isGoalAssembler != goalAssemblerConstraint.IsValid ||
                isGoalAssembler &&
                (goalAssemblerConstraint.OperationIndex != index ||
                 goalAssemblerConstraint.CallSiteIndex != frameCacheIndex ||
                 goalAssemblerConstraint.GoalSetValueIndex !=
                 outputFullBodyIkGoalSetValueIndex ||
                 goalAssemblerConstraint.ContributionInputStart !=
                 fullBodyIkGoalContributionInputStart ||
                 goalAssemblerConstraint.ContributionInputCount !=
                 fullBodyIkGoalContributionInputCount) ||
                isFullBodyIk != fullBodyIkConstraint.IsValid ||
                isFullBodyIk &&
                (fullBodyIkConstraint.OperationIndex != index ||
                 fullBodyIkConstraint.CallSiteIndex != frameCacheIndex ||
                 fullBodyIkConstraint.InputPoseValueIndex !=
                 inputPoseValueIndexA ||
                 fullBodyIkConstraint.OutputPoseValueIndex !=
                 outputPoseValueIndex ||
                 fullBodyIkConstraint.InputGoalSetValueIndex !=
                 inputFullBodyIkGoalSetValueIndex) ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentException("Animation Pose Graph Native operation header is invalid.");
            Index = index;
            Code = code;
            OutputValueIndex = outputPoseValueIndex;
            m_OutputFullBodyIkGoalContributionValueIndex =
                isPoseBoneContribution || isFootPlacement
                    ? -1
                    : outputFullBodyIkGoalContributionValueIndex;
            m_OutputFullBodyIkGoalSetValueIndex =
                isGoalAssembler ? -1 : outputFullBodyIkGoalSetValueIndex;
            m_InputFullBodyIkGoalSetValueIndex = isFullBodyIk
                ? -1
                : inputFullBodyIkGoalSetValueIndex;
            InputValueIndexA = inputPoseValueIndexA;
            InputValueIndexB = inputPoseValueIndexB;
            m_FullBodyIkGoalContributionInputStart = isGoalAssembler
                ? -1
                : fullBodyIkGoalContributionInputStart;
            m_FullBodyIkGoalContributionInputCount = isGoalAssembler
                ? 0
                : fullBodyIkGoalContributionInputCount;
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
            PoseBoneContribution = poseBoneContribution;
            FootPlacementConstraint = footPlacementConstraint;
            GoalAssemblerConstraint = goalAssemblerConstraint;
            FullBodyIkConstraint = fullBodyIkConstraint;
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
        readonly int m_OutputFullBodyIkGoalContributionValueIndex;
        internal int OutputFullBodyIkGoalContributionValueIndex =>
            PoseBoneContribution.IsValid
                ? PoseBoneContribution.ContributionValueIndex
                : FootPlacementConstraint.IsValid
                ? FootPlacementConstraint.ContributionValueIndex
                : m_OutputFullBodyIkGoalContributionValueIndex;
        readonly int m_OutputFullBodyIkGoalSetValueIndex;
        internal int OutputFullBodyIkGoalSetValueIndex =>
            GoalAssemblerConstraint.IsValid
                ? GoalAssemblerConstraint.GoalSetValueIndex
                : m_OutputFullBodyIkGoalSetValueIndex;
        readonly int m_InputFullBodyIkGoalSetValueIndex;
        internal int InputFullBodyIkGoalSetValueIndex =>
            FullBodyIkConstraint.IsValid
                ? FullBodyIkConstraint.InputGoalSetValueIndex
                : m_InputFullBodyIkGoalSetValueIndex;
        internal int InputValueIndexA { get; }
        internal int InputValueIndexB { get; }
        readonly int m_FullBodyIkGoalContributionInputStart;
        internal int FullBodyIkGoalContributionInputStart =>
            GoalAssemblerConstraint.IsValid
                ? GoalAssemblerConstraint.ContributionInputStart
                : m_FullBodyIkGoalContributionInputStart;
        readonly int m_FullBodyIkGoalContributionInputCount;
        internal int FullBodyIkGoalContributionInputCount =>
            GoalAssemblerConstraint.IsValid
                ? GoalAssemblerConstraint.ContributionInputCount
                : m_FullBodyIkGoalContributionInputCount;
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
        internal CharacterPoseBoneContributionConstraintHandle
            PoseBoneContribution { get; }
        internal CharacterFootPlacementConstraintHandle
            FootPlacementConstraint { get; }
        internal CharacterFullBodyIkGoalAssemblerConstraintHandle
            GoalAssemblerConstraint { get; }
        internal CharacterFullBodyIkConstraintHandle
            FullBodyIkConstraint { get; }
        internal int FullBodyIkIndex =>
            FullBodyIkConstraint.IsValid
                ? FullBodyIkConstraint.FullBodyIkIndex
                : -1;
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
            OutputFullBodyIkGoalContributionValueIndex,
            OutputFullBodyIkGoalSetValueIndex,
            InputFullBodyIkGoalSetValueIndex,
            InputValueIndexA,
            InputValueIndexB,
            FullBodyIkGoalContributionInputStart,
            FullBodyIkGoalContributionInputCount,
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
            PoseBoneContribution,
            FootPlacementConstraint,
            GoalAssemblerConstraint,
            FullBodyIkConstraint,
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
            OutputFullBodyIkGoalContributionValueIndex,
            OutputFullBodyIkGoalSetValueIndex,
            InputFullBodyIkGoalSetValueIndex,
            sourcePoseValueIndex,
            targetPoseValueIndex,
            FullBodyIkGoalContributionInputStart,
            FullBodyIkGoalContributionInputCount,
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
            default,
            default,
            default,
            default,
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
            int outputPoseValueIndex)
        {
            if (fragmentIndex < 0 || outputPoseValueIndex < 0)
            {
                throw new ArgumentException("Linked Pose native candidate output is invalid.");
            }
            FragmentIndex = fragmentIndex;
            OutputPoseValueIndex = outputPoseValueIndex;
        }

        internal int FragmentIndex { get; }
        internal int OutputPoseValueIndex { get; }
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

    internal sealed class CharacterPoseProgramExecutionView : IDisposable
    {
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
        NativeArray<CharacterVirtualBoneDescriptor> m_VirtualBones;
        NativeArray<CharacterPoseBoneIkGoalDescriptor> m_PoseBoneIkGoalDescriptors;
        NativeArray<int> m_FullBodyIkGoalContributionInputValueIndices;
        NativeArray<AnimationPoseGraphNativeLinkedPoseCall> m_LinkedPoseCalls;
        NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate> m_LinkedPoseCandidates;
        LinkedPoseGroupId[] m_LinkedPoseCallGroupIds;
        LinkedPoseInterfaceId[] m_LinkedPoseCallInterfaceIds;
        LinkedPoseImplementationId[] m_LinkedPoseCandidateImplementationIds;
        CharacterPoseBoneCounts m_BoneCounts;
        int m_BoneCount;
        int m_ParameterCount;
        int m_PoseValueCount;
        int m_FootPlacementCount;
        int m_FullBodyIkCount;
        int m_FullBodyIkGoalContributionCount;
        int m_FullBodyIkContributionGoalCount;
        int m_FullBodyIkGoalSetValueCount;
        int m_ContributionStride;
        int m_FrameCacheCount;
        int m_OutputOperationIndex;
        int m_OutputNativeOperationIndex;
        int m_OutputValueIndex;
        int m_LeftFootBoneIndex;
        int m_RightFootBoneIndex;
        int m_PelvisBoneIndex;
        int m_LinkedPoseFragmentCount;
        AnimationPoseGraphNativeLegChain m_LeftLeg;
        AnimationPoseGraphNativeLegChain m_RightLeg;
        FixedString128Bytes m_ProgramId;
        FixedString128Bytes m_ProjectionRevision;
        FixedString128Bytes m_PoseProgramImageHash;
        FixedString64Bytes m_RigId;
        FixedString64Bytes m_RigRevision;
        bool m_Disposed;

        internal CharacterPoseProgramExecutionView(
            CharacterPresentationProjection projection,
            in AnimationPoseNativeAggregateLayout layout)
        {
            try
            {
                if (projection == null)
                    throw new ArgumentNullException(nameof(projection));
                projection.RequirePosePayload();
                CharacterPoseProgramImage program = projection.PosePlan;
                CharacterAnimationRigPayload rig = projection.Rig;
                AnimationBlendCurveCatalogPayload curves =
                    projection.BlendCurveCatalog;
                AnimationBlendProfileCatalogPayload profiles =
                    projection.BlendProfileCatalog;
                program.RequireValid();
                program.RequireProjectionIdentity(
                    projection.ProgramId,
                    projection.ProjectionRevision,
                    rig.RigId,
                    rig.RigRevision);
                rig.RequireValid();
                curves.RequireValid();
                profiles.RequireValid(program.PoseBoneCount, rig.RigId, rig.RigRevision);
                layout.RequireValid();
                if (!string.Equals(program.RigId, rig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(program.RigRevision, rig.RigRevision, StringComparison.Ordinal) ||
                    program.PoseBoneCount != rig.PoseBoneCount || program.Parameters.Count <= 0 ||
                    program.ContributionCapacity <= 0 ||
                    layout.BoneCount != program.PoseBoneCount ||
                    layout.ParameterCount != program.Parameters.Count ||
                    layout.PoseValueCount != program.PoseValueCount ||
                    layout.PoseValueWorkspaceCount !=
                    program.PoseValueWorkspaceCount ||
                    layout.PoseValueContributionStride !=
                    program.ContributionCapacity ||
                    layout.OperationCount != program.OperationHeaders.Count ||
                    layout.StageCount != program.Stages.Count)
                    throw new InvalidOperationException("Animation Pose Graph Program and Rig payload do not match.");

                m_BoneCount = program.PoseBoneCount;
                m_BoneCounts = rig.BoneCounts;
                m_ParameterCount = program.Parameters.Count;
                m_PoseValueCount = program.PoseValueCount;
                m_FootPlacementCount = program.FootPlacements.Count;
                m_FullBodyIkCount = program.FullBodyIks.Count;
                m_FullBodyIkGoalContributionCount =
                    program.FullBodyIkGoalContributionWorkspaceCount;
                m_FullBodyIkContributionGoalCount =
                    program.FullBodyIkGoalContributionGoalWorkspaceCount;
                m_FullBodyIkGoalSetValueCount =
                    program.FullBodyIkGoalSetWorkspaceCount;
                m_ContributionStride = program.ContributionCapacity;
                m_FrameCacheCount = program.FrameCacheCount;
                m_OutputOperationIndex = program.OutputOperationIndex;
                m_LeftFootBoneIndex = rig.LeftLeg.AnklePhysicalBoneIndex;
                m_RightFootBoneIndex = rig.RightLeg.AnklePhysicalBoneIndex;
                m_PelvisBoneIndex = rig.PelvisPhysicalBoneIndex;
                m_LinkedPoseFragmentCount =
                    program.LinkedPoseFragments.Count;
                m_LeftLeg = new AnimationPoseGraphNativeLegChain(rig.LeftLeg);
                m_RightLeg = new AnimationPoseGraphNativeLegChain(rig.RightLeg);
                m_ProgramId = new FixedString128Bytes(program.ProgramId);
                m_ProjectionRevision =
                    new FixedString128Bytes(program.ProjectionRevision);
                m_PoseProgramImageHash =
                    new FixedString128Bytes(program.PoseProgramImageHash);
                m_RigId = new FixedString64Bytes(rig.RigId);
                m_RigRevision = new FixedString64Bytes(rig.RigRevision);

                int nativeOperationCount = 0;
                int policyCount = 0;
                for (int i = 0; i < program.OperationHeaders.Count; i++)
                {
                    CharacterPoseOperationHeader operation =
                        program.OperationHeaders[i];
                    if (CharacterPoseProgramImage.IsExecutionViewOperation(
                            operation.Code))
                        nativeOperationCount++;
                    if (operation.Code == CharacterPoseOperationCode.PoseParameterResolve)
                        policyCount = checked(policyCount + m_ParameterCount);
                }
                if (nativeOperationCount <= 0 || m_ContributionStride <= 0 ||
                    m_FrameCacheCount != program.OperationHeaders.Count)
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
                m_VirtualBones = Allocate<CharacterVirtualBoneDescriptor>(rig.VirtualBoneCount);
                int poseBoneGoalCount = program.PoseBoneIkGoalSources.Sum(value => value.GoalCount);
                m_PoseBoneIkGoalDescriptors = Allocate<CharacterPoseBoneIkGoalDescriptor>(poseBoneGoalCount);
                m_FullBodyIkGoalContributionInputValueIndices = Allocate<int>(
                    program.OperationHeaders.Sum(value =>
                        program.OperationPages.CountInputValues(
                            value,
                            CharacterPoseValueReferenceKind.FullBodyIkGoalContribution)));
                int linkedPoseCandidateCount = program.LinkedPoseCalls.Sum(value => value.FragmentIndices.Count);
                m_LinkedPoseCalls = Allocate<AnimationPoseGraphNativeLinkedPoseCall>(program.LinkedPoseCalls.Count);
                m_LinkedPoseCandidates = Allocate<AnimationPoseGraphNativeLinkedPoseCandidate>(linkedPoseCandidateCount);
                m_LinkedPoseCallGroupIds = new LinkedPoseGroupId[program.LinkedPoseCalls.Count];
                m_LinkedPoseCallInterfaceIds = new LinkedPoseInterfaceId[program.LinkedPoseCalls.Count];
                m_LinkedPoseCandidateImplementationIds = new LinkedPoseImplementationId[linkedPoseCandidateCount];
                CompileRig(program, rig);
                CompileBlendCatalogs(curves, profiles);
                CompilePayloads(program);
                CompileLinkedPose(program);
                CompileOperations(program);
                MaterializeStages(program);
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
        internal int FootPlacementCount => m_FootPlacementCount;
        internal int FullBodyIkCount => m_FullBodyIkCount;
        internal int FullBodyIkGoalSetValueCount => m_FullBodyIkGoalSetValueCount;
        internal int ContributionStride => m_ContributionStride;
        internal int FrameCacheCount => m_FrameCacheCount;
        internal int OutputOperationIndex => m_OutputOperationIndex;
        internal int OutputNativeOperationIndex => m_OutputNativeOperationIndex;
        internal int OutputValueIndex => m_OutputValueIndex;
        internal int LeftFootBoneIndex => m_LeftFootBoneIndex;
        internal int RightFootBoneIndex => m_RightFootBoneIndex;
        internal int PelvisBoneIndex => m_PelvisBoneIndex;
        internal int LinkedPoseFragmentCount =>
            m_LinkedPoseFragmentCount;
        internal AnimationPoseGraphNativeLegChain LeftLeg => m_LeftLeg;
        internal AnimationPoseGraphNativeLegChain RightLeg => m_RightLeg;
        internal FixedString128Bytes ProgramId => m_ProgramId;
        internal FixedString128Bytes ProjectionRevision =>
            m_ProjectionRevision;
        internal FixedString128Bytes PoseProgramImageHash =>
            m_PoseProgramImageHash;
        internal FixedString64Bytes RigId => m_RigId;
        internal FixedString64Bytes RigRevision => m_RigRevision;
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
        internal NativeArray<CharacterVirtualBoneDescriptor> VirtualBones => m_VirtualBones;
        internal CharacterPoseBoneContributionCatalog PoseBoneContributions =>
            new CharacterPoseBoneContributionCatalog(
                m_PoseBoneIkGoalDescriptors);
        internal CharacterFullBodyIkGoalAssemblerCatalog GoalAssemblers =>
            new CharacterFullBodyIkGoalAssemblerCatalog(
                m_FullBodyIkGoalContributionInputValueIndices,
                m_FullBodyIkGoalSetValueCount);
        internal bool ContainsFullBodyIkConstraint(
            in CharacterFullBodyIkConstraintHandle handle) =>
            handle.IsValid &&
            (uint)handle.FullBodyIkIndex < (uint)m_FullBodyIkCount &&
            (uint)handle.InputGoalSetValueIndex <
            (uint)m_FullBodyIkGoalSetValueCount;
        internal int FullBodyIkGoalContributionCount =>
            m_FullBodyIkGoalContributionCount;
        internal int FullBodyIkContributionGoalCount =>
            m_FullBodyIkContributionGoalCount;
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCall> LinkedPoseCalls => m_LinkedPoseCalls;
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate> LinkedPoseCandidates => m_LinkedPoseCandidates;
        internal CharacterPoseBoneCounts BoneCounts => m_BoneCounts;

        internal LinkedPoseGroupId GetLinkedPoseCallGroupId(int callIndex)
        {
            RequireAlive();
            if ((uint)callIndex >= (uint)m_LinkedPoseCallGroupIds.Length)
                throw new ArgumentOutOfRangeException(nameof(callIndex));
            return m_LinkedPoseCallGroupIds[callIndex];
        }

        internal LinkedPoseInterfaceId GetLinkedPoseCallInterfaceId(
            int callIndex)
        {
            RequireAlive();
            if ((uint)callIndex >= (uint)m_LinkedPoseCallInterfaceIds.Length)
                throw new ArgumentOutOfRangeException(nameof(callIndex));
            return m_LinkedPoseCallInterfaceIds[callIndex];
        }

        void CompileRig(CharacterPoseProgramImage program, CharacterAnimationRigPayload rig)
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

        void CompilePayloads(CharacterPoseProgramImage program)
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
        }

        void CompileLinkedPose(CharacterPoseProgramImage program)
        {
            int candidateIndex = 0;
            for (int callIndex = 0; callIndex < program.LinkedPoseCalls.Count; callIndex++)
            {
                CharacterLinkedPoseCallPlanDescriptor call = program.LinkedPoseCalls[callIndex];
                if (call == null || call.Index != callIndex)
                    throw new InvalidOperationException($"Linked Pose native call #{callIndex} is invalid.");
                CharacterPoseOperationHeader callOperation =
                    program.OperationHeaders.Single(value =>
                        value.Code == CharacterPoseOperationCode.LinkedPoseCall &&
                        ((CharacterPoseIndexedOperationPayload)
                            program.OperationPages.RequirePayload(value))
                        .ValueIndex == callIndex);
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
                        else
                        {
                            throw new InvalidOperationException(
                                $"Linked Pose fragment #{fragmentIndex} has a non-Pose output.");
                        }
                    }
                    if (program.OperationPages.FindOutputValueIndex(
                            callOperation,
                            CharacterPoseValueReferenceKind.Pose) < 0 ||
                        program.OperationPages.FindOutputValueIndex(
                            callOperation,
                            CharacterPoseValueReferenceKind.FullBodyIkGoalSet) >= 0 ||
                        outputPoseValueIndex < 0)
                    {
                        throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} output ABI does not match call #{callIndex}.");
                    }
                    m_LinkedPoseCandidates[candidateIndex] = new AnimationPoseGraphNativeLinkedPoseCandidate(
                        fragmentIndex,
                        outputPoseValueIndex);
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

        internal int FindLinkedPoseCandidate(
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

        void CompileOperations(CharacterPoseProgramImage program)
        {
            int nativeIndex = 0;
            int policyOffset = 0;
            int contributionInputCursor = 0;
            m_OutputNativeOperationIndex = -1;
            for (int i = 0; i < program.OperationHeaders.Count; i++)
            {
                CharacterPoseOperationHeader operation =
                    program.OperationHeaders[i];
                if (!CharacterPoseProgramImage.IsExecutionViewOperation(
                        operation.Code))
                    continue;
                int outputPose = program.OperationPages.FindOutputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Pose);
                int outputGoalContribution =
                    program.OperationPages.FindOutputValueIndex(
                        operation,
                        CharacterPoseValueReferenceKind.FullBodyIkGoalContribution);
                int outputGoalSet = program.OperationPages.FindOutputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalSet);
                int inputGoalSet = program.OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalSet);
                int inputPoseA = program.OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Pose);
                int inputPoseB = program.OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Pose,
                    1);
                int parameterIndex = program.OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Parameter);
                int contributionInputStart = contributionInputCursor;
                int contributionInputCount =
                    program.OperationPages.CountInputValues(
                        operation,
                        CharacterPoseValueReferenceKind.FullBodyIkGoalContribution);
                for (int inputIndex = 0;
                     inputIndex < contributionInputCount;
                     inputIndex++)
                {
                    m_FullBodyIkGoalContributionInputValueIndices[
                            contributionInputCursor++] =
                        program.OperationPages.FindInputValueIndex(
                            operation,
                            CharacterPoseValueReferenceKind.FullBodyIkGoalContribution,
                            inputIndex);
                }
                int playerIndex = -1;
                int inertializationIndex = -1;
                int stateMachineIndex = -1;
                int animationSlotIndex = -1;
                int linkedPoseCallIndex = -1;
                int modifyBoneIndex = -1;
                int rootOrientationWarpIndex = -1;
                int boneMaskIndex = -1;
                int additiveReferenceIndex = -1;
                int poseBoneIkGoalsIndex = -1;
                int footPlacementIndex = -1;
                int fullBodyIkIndex = -1;
                AnimationSelectionAvailabilityPolicy outputPolicy = default;
                CharacterPoseOperationPayload payload =
                    program.OperationPages.RequirePayload(operation);
                if (payload is CharacterPosePlayerOperationPayload player)
                {
                    playerIndex = player.PlayerIndex;
                    outputPolicy = player.SelectionAvailability;
                }
                else if (payload is CharacterPoseBlendOperationPayload blend)
                {
                    playerIndex = blend.PlayerIndex;
                    outputPolicy = blend.SelectionAvailability;
                }
                else if (payload is CharacterPoseAnimationSlotOperationPayload slot)
                {
                    playerIndex = slot.PlayerIndex;
                    animationSlotIndex = slot.AnimationSlotIndex;
                    int controlIndex = program.OperationPages.FindInputValueIndex(
                        operation,
                        CharacterPoseValueReferenceKind.OperationControl);
                    if ((uint)controlIndex >= (uint)i)
                        throw new InvalidOperationException(
                            $"Animation Pose Graph Player operation #{i} has no compiled control input.");
                    outputPolicy =
                        ((CharacterPoseActionInputOperationPayload)
                            program.OperationPages.RequirePayload(
                                program.OperationHeaders[controlIndex]))
                        .SelectionAvailability;
                }
                else if (payload is CharacterPoseIndexedOperationPayload indexed)
                {
                    if (operation.Family == CharacterPoseOperationFamily.Inertialization)
                        inertializationIndex = indexed.ValueIndex;
                    else if (operation.Family == CharacterPoseOperationFamily.FullBodyIk)
                        fullBodyIkIndex = indexed.ValueIndex;
                    else if (operation.Family == CharacterPoseOperationFamily.LinkedPose)
                        linkedPoseCallIndex = indexed.ValueIndex;
                }
                else if (payload is CharacterPoseStateMachineOperationPayload machine)
                {
                    stateMachineIndex = machine.StateMachineIndex;
                }
                else if (payload is CharacterPoseCompositionOperationPayload composition)
                {
                    boneMaskIndex = composition.BoneMaskIndex;
                    additiveReferenceIndex = composition.AdditiveReferenceIndex;
                }
                else if (payload is CharacterPoseComponentControlOperationPayload control)
                {
                    modifyBoneIndex = control.ModifyBoneIndex;
                    rootOrientationWarpIndex = control.RootOrientationWarpIndex;
                }
                else if (payload is CharacterPoseGoalContributionOperationPayload goal)
                {
                    poseBoneIkGoalsIndex = goal.PoseBoneIkGoalsIndex;
                    footPlacementIndex = goal.FootPlacementIndex;
                }
                int operationPolicyOffset = -1;
                if (operation.Code == CharacterPoseOperationCode.PoseParameterResolve)
                {
                    IReadOnlyList<PoseParameterResolvePolicy> policies =
                        ((CharacterPoseParameterResolveOperationPayload)payload)
                        .Policies;
                    if (policies.Count != m_ParameterCount)
                        throw new InvalidOperationException($"Animation Pose Graph operation #{i} parameter policy is incomplete.");
                    operationPolicyOffset = policyOffset;
                    for (int parameter = 0; parameter < m_ParameterCount; parameter++)
                        m_ParameterPolicies[policyOffset++] = policies[parameter];
                }
                int maskOffset = boneMaskIndex >= 0
                    ? boneMaskIndex * m_BoneCount
                    : -1;
                int additiveOffset = additiveReferenceIndex >= 0
                    ? additiveReferenceIndex * m_BoneCount
                    : -1;
                AdditiveReferenceSpace referenceSpace = default;
                AdditiveScalePolicy scalePolicy = default;
                if (additiveReferenceIndex >= 0)
                {
                    CharacterPresentationAdditiveReferenceDescriptor reference =
                        program.AdditiveReferences[additiveReferenceIndex];
                    referenceSpace = reference.Space;
                    scalePolicy = reference.ScalePolicy;
                }
                CharacterPoseBoneContributionConstraintHandle
                    poseBoneContribution = default;
                if (operation.Code ==
                    CharacterPoseOperationCode.PoseBoneIKGoals)
                {
                    int sourceIndex = poseBoneIkGoalsIndex;
                    CharacterPresentationPoseBoneIkGoalsDescriptor source =
                        program.PoseBoneIkGoalSources[sourceIndex];
                    int descriptorOffset = 0;
                    for (int precedingSourceIndex = 0;
                         precedingSourceIndex < sourceIndex;
                         precedingSourceIndex++)
                    {
                        descriptorOffset = checked(
                            descriptorOffset +
                            program.PoseBoneIkGoalSources[
                                precedingSourceIndex].GoalCount);
                    }
                    poseBoneContribution =
                        new CharacterPoseBoneContributionConstraintHandle(
                            operation.Index,
                            operation.Index,
                            inputPoseA,
                            outputGoalContribution,
                            source.ContributionGoalWorkspaceOffset,
                            descriptorOffset,
                            source.GoalCount);
                }
                CharacterFootPlacementConstraintHandle
                    footPlacementConstraint = default;
                if (operation.Code ==
                    CharacterPoseOperationCode.FootPlacement)
                {
                    CharacterPresentationFootPlacementDescriptor descriptor =
                        program.FootPlacements[
                            footPlacementIndex];
                    footPlacementConstraint =
                        new CharacterFootPlacementConstraintHandle(
                            operation.Index,
                            operation.Index,
                            footPlacementIndex,
                            outputGoalContribution,
                            descriptor.ContributionGoalWorkspaceOffset);
                }
                CharacterFullBodyIkGoalAssemblerConstraintHandle
                    goalAssemblerConstraint = default;
                if (operation.Code ==
                    CharacterPoseOperationCode.FullBodyIkGoalAssembler)
                {
                    goalAssemblerConstraint =
                        new CharacterFullBodyIkGoalAssemblerConstraintHandle(
                            operation.Index,
                            operation.Index,
                            outputGoalSet,
                            contributionInputStart,
                            contributionInputCount);
                }
                CharacterFullBodyIkConstraintHandle
                    fullBodyIkConstraint = default;
                if (operation.Code == CharacterPoseOperationCode.FullBodyIK)
                {
                    fullBodyIkConstraint =
                        new CharacterFullBodyIkConstraintHandle(
                            operation.Index,
                            operation.Index,
                            fullBodyIkIndex,
                            inputPoseA,
                            outputPose,
                            inputGoalSet);
                }
                m_Operations[nativeIndex] = new AnimationPoseGraphNativeOperation(
                    operation.Index,
                    operation.Code,
                    outputPose,
                    outputGoalContribution,
                    outputGoalSet,
                    inputGoalSet,
                    inputPoseA,
                    inputPoseB,
                    contributionInputStart,
                    contributionInputCount,
                    playerIndex,
                    outputPolicy,
                    parameterIndex,
                    inertializationIndex,
                    maskOffset,
                    additiveOffset,
                    referenceSpace,
                    scalePolicy,
                    operationPolicyOffset,
                    modifyBoneIndex,
                    rootOrientationWarpIndex,
                    poseBoneContribution,
                    footPlacementConstraint,
                    goalAssemblerConstraint,
                    fullBodyIkConstraint,
                    stateMachineIndex,
                    animationSlotIndex,
                    linkedPoseCallIndex,
                    operation.LinkedPoseFragmentIndex,
                    operation.Index,
                    operation.Weight);
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    m_OutputNativeOperationIndex = nativeIndex;
                    m_OutputValueIndex = outputPose;
                }
                nativeIndex++;
            }
            if (nativeIndex != m_Operations.Length ||
                policyOffset != m_ParameterPolicies.Length ||
                contributionInputCursor !=
                m_FullBodyIkGoalContributionInputValueIndices.Length ||
                m_OutputNativeOperationIndex < 0)
                throw new InvalidOperationException("Animation Pose Graph Native operation layout is inconsistent.");
        }

        void MaterializeStages(CharacterPoseProgramImage program)
        {
            for (int stageIndex = 0; stageIndex < program.Stages.Count; stageIndex++)
                m_Stages[stageIndex] = new AnimationPoseGraphNativeStage(
                    program.Stages[stageIndex]);
        }

        internal void RequireValid()
        {
            RequireAlive();
            if (m_BoneCount <= 0 || m_ParameterCount <= 0 || m_PoseValueCount <= 0 ||
                m_FootPlacementCount < 0 ||
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
                !m_RootOrientationWarps.IsCreated ||
                !m_VirtualBones.IsCreated || m_VirtualBones.Length != m_BoneCounts.VirtualBoneCount ||
                !m_PoseBoneIkGoalDescriptors.IsCreated ||
                !m_FullBodyIkGoalContributionInputValueIndices.IsCreated ||
                m_FullBodyIkGoalContributionCount < 0 ||
                m_FullBodyIkContributionGoalCount < 0 ||
                !m_LinkedPoseCalls.IsCreated || !m_LinkedPoseCandidates.IsCreated ||
                m_LinkedPoseCallGroupIds == null ||
                m_LinkedPoseCallGroupIds.Length != m_LinkedPoseCalls.Length ||
                m_LinkedPoseCallInterfaceIds == null ||
                m_LinkedPoseCallInterfaceIds.Length != m_LinkedPoseCalls.Length ||
                m_LinkedPoseCandidateImplementationIds == null ||
                m_LinkedPoseCandidateImplementationIds.Length != m_LinkedPoseCandidates.Length ||
                m_LinkedPoseFragmentCount < 0 ||
                !m_BoneCounts.IsValid ||
                m_PelvisBoneIndex < 0 || m_PelvisBoneIndex >= m_BoneCount ||
                !m_LeftLeg.IsValid(m_BoneCounts.PhysicalBoneCount, m_PelvisBoneIndex) ||
                !m_RightLeg.IsValid(m_BoneCounts.PhysicalBoneCount, m_PelvisBoneIndex) ||
                m_ParameterDefaults.Length != m_ParameterCount || m_ParentIndices.Length != m_BoneCount ||
                m_ProgramId.Length == 0 ||
                m_ProjectionRevision.Length == 0 ||
                m_PoseProgramImageHash.Length == 0 ||
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
                if ((uint)candidate.FragmentIndex >=
                    (uint)m_LinkedPoseFragmentCount ||
                    candidate.OutputPoseValueIndex < 0 ||
                    candidate.OutputPoseValueIndex >= m_PoseValueCount ||
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

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_LinkedPoseCallGroupIds = null;
            m_LinkedPoseCallInterfaceIds = null;
            m_LinkedPoseCandidateImplementationIds = null;
            DisposeArray(ref m_LinkedPoseCandidates);
            DisposeArray(ref m_LinkedPoseCalls);
            DisposeArray(ref m_FullBodyIkGoalContributionInputValueIndices);
            DisposeArray(ref m_PoseBoneIkGoalDescriptors);
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
                throw new ObjectDisposedException(nameof(CharacterPoseProgramExecutionView));
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
