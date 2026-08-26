using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseWorldAwareStageInput
    {
        internal CharacterPoseWorldAwareStageInput(
            int operationIndex,
            int contributionGoalOffset,
            in CharacterFootPlacementFrameInput footPlacement)
        {
            if (operationIndex < 0 || contributionGoalOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            OperationIndex = operationIndex;
            ContributionGoalOffset = contributionGoalOffset;
            FootPlacement = footPlacement;
            HasFootPlacement = true;
            WorldContextAvailable = true;
        }

        internal CharacterPoseWorldAwareStageInput(
            int operationIndex,
            int contributionGoalOffset)
        {
            if (operationIndex < 0 || contributionGoalOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            OperationIndex = operationIndex;
            ContributionGoalOffset = contributionGoalOffset;
            FootPlacement = default;
            HasFootPlacement = true;
            WorldContextAvailable = false;
        }

        internal int OperationIndex { get; }
        internal int ContributionGoalOffset { get; }
        internal CharacterFootPlacementFrameInput FootPlacement { get; }
        internal bool HasFootPlacement { get; }
        internal bool WorldContextAvailable { get; }
    }

    internal struct CharacterPoseGraphStagedExecutor
    {
        const float ScaleEpsilon = 0.000001f;

        static readonly ProfilerMarker ValueResetMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.ValueReset");
        static readonly ProfilerMarker PlayerInputMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.PlayerInput");
        static readonly ProfilerMarker SlotMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.Slot");
        static readonly ProfilerMarker StateMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.State");
        static readonly ProfilerMarker InertializationMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.Inertialization");
        static readonly ProfilerMarker BlendMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.Blend");
        static readonly ProfilerMarker ConstraintMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.Constraint");
        static readonly ProfilerMarker IkGoalMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.IKGoal");
        static readonly ProfilerMarker LinkedPoseMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.LinkedPose");
        static readonly ProfilerMarker FullBodyIkMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.FinalIKFullBody");
        static readonly ProfilerMarker SpaceConversionMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.SpaceConversion");
        static readonly ProfilerMarker OutputMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.Output");
        static readonly ProfilerMarker ValueValidationMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraph.ValueValidation");

        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeOperation> m_Operations;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeStage> m_Stages;
        [ReadOnly]
        readonly NativeArray<float> m_DenseBoneMasks;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_AdditiveReferences;
        [ReadOnly]
        readonly NativeArray<PoseParameterResolvePolicy> m_ParameterPolicies;
        [ReadOnly]
        readonly NativeArray<float> m_ParameterDefaults;
        [ReadOnly]
        readonly NativeArray<int> m_ParentIndices;
        [ReadOnly]
        readonly NativeArray<AnimationBlendCurveNativeEntry> m_BlendCurves;
        [ReadOnly]
        readonly NativeArray<AnimationBlendCurveSegment> m_BlendCurveSegments;
        [ReadOnly]
        readonly NativeArray<AnimationBlendProfileNativeEntry> m_BlendProfiles;
        [ReadOnly]
        readonly NativeArray<float> m_BlendDenseProfiles;
        [ReadOnly]
        readonly NativeArray<PoseInertializationNativeNode> m_Inertializations;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeModifyBone> m_ModifyBones;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeRootOrientationWarp> m_RootOrientationWarps;
        [ReadOnly]
        readonly NativeArray<CharacterRootOrientationWarpNativeControl> m_RootOrientationWarpControls;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativePoseBoneIkGoalRange> m_PoseBoneIkGoalRanges;
        [ReadOnly]
        readonly NativeArray<CharacterPoseBoneIkGoalDescriptor> m_PoseBoneIkGoalDescriptors;
        [ReadOnly]
        readonly NativeArray<int> m_FullBodyIkGoalContributionInputValueIndices;
        readonly int m_FullBodyIkGoalSetValueCount;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeLinkedPoseCall> m_LinkedPoseCalls;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate> m_LinkedPoseCandidates;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl> m_LinkedPoseCallControls;
        [ReadOnly]
        readonly NativeArray<byte> m_LinkedPoseActiveFragments;
        [ReadOnly]
        readonly NativeArray<CharacterPoseStateMachineNativeControl> m_StateMachineControls;
        [ReadOnly]
        readonly NativeArray<CharacterAnimationSlotNativeControl> m_AnimationSlotControls;
        [ReadOnly]
        readonly NativeArray<PoseInertializationNativeRule> m_InertialRules;
        [ReadOnly]
        readonly NativeArray<AnimationBlendCurveSegment> m_InertialCurveSegments;
        [ReadOnly]
        readonly NativeArray<float> m_InertialDenseProfiles;
        [ReadOnly]
        readonly NativeArray<PoseParameterInertializationMode> m_InertialParameterModes;
        NativeArray<PoseInertializationNativeState> m_InertialStates;
        NativeArray<AnimationLocalBonePose> m_InertialHistory;
        NativeArray<AnimationBlendBoneVelocity> m_InertialHistoryVelocities;
        NativeArray<float> m_InertialHistoryParameters;
        NativeArray<byte> m_InertialHistoryParameterAvailability;
        NativeArray<AnimationFootFeatureSample> m_InertialHistoryLeftFeet;
        NativeArray<AnimationFootFeatureSample> m_InertialHistoryRightFeet;
        NativeArray<byte> m_InertialHistoryHasFeet;
        NativeArray<AnimationFootFeatureSample> m_InertialAccumulatorLeftFeet;
        NativeArray<AnimationFootFeatureSample> m_InertialAccumulatorRightFeet;
        NativeArray<byte> m_InertialAccumulatorHasFeet;
        NativeArray<Vector3> m_InertialPositionResiduals;
        NativeArray<Vector3> m_InertialRotationResiduals;
        NativeArray<Vector3> m_InertialScaleResiduals;
        NativeArray<Vector3> m_InertialLinearVelocityResiduals;
        NativeArray<Vector3> m_InertialAngularVelocityResiduals;
        NativeArray<Vector3> m_InertialScaleVelocityResiduals;
        NativeArray<float> m_InertialParameterResiduals;
        readonly NativeArray<byte> m_InertialResetRequests;
        [ReadOnly]
        readonly NativeArray<PoseInertializationNativeState> m_CommittedInertialStates;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_CommittedInertialHistory;
        [ReadOnly]
        readonly NativeArray<AnimationBlendBoneVelocity> m_CommittedInertialHistoryVelocities;
        [ReadOnly]
        readonly NativeArray<float> m_CommittedInertialHistoryParameters;
        [ReadOnly]
        readonly NativeArray<byte> m_CommittedInertialHistoryParameterAvailability;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_CommittedInertialHistoryLeftFeet;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_CommittedInertialHistoryRightFeet;
        [ReadOnly]
        readonly NativeArray<byte> m_CommittedInertialHistoryHasFeet;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_CommittedInertialAccumulatorLeftFeet;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_CommittedInertialAccumulatorRightFeet;
        [ReadOnly]
        readonly NativeArray<byte> m_CommittedInertialAccumulatorHasFeet;
        [ReadOnly]
        readonly NativeArray<Vector3> m_CommittedInertialPositionResiduals;
        [ReadOnly]
        readonly NativeArray<Vector3> m_CommittedInertialRotationResiduals;
        [ReadOnly]
        readonly NativeArray<Vector3> m_CommittedInertialScaleResiduals;
        [ReadOnly]
        readonly NativeArray<Vector3> m_CommittedInertialLinearVelocityResiduals;
        [ReadOnly]
        readonly NativeArray<Vector3> m_CommittedInertialAngularVelocityResiduals;
        [ReadOnly]
        readonly NativeArray<Vector3> m_CommittedInertialScaleVelocityResiduals;
        [ReadOnly]
        readonly NativeArray<float> m_CommittedInertialParameterResiduals;

        [ReadOnly]
        readonly NativeArray<AnimationPlayerPoseNativeRange> m_SlotRanges;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_SlotDenseLocalPoses;
        [ReadOnly]
        readonly NativeArray<AnimationBlendBoneVelocity> m_SlotDenseVelocities;
        [ReadOnly]
        readonly NativeArray<float> m_SlotPoseParameters;
        [ReadOnly]
        readonly NativeArray<byte> m_SlotPoseParameterAvailability;
        [ReadOnly]
        readonly NativeArray<AnimationPrimitivePoseContribution> m_SlotContributions;
        [ReadOnly]
        readonly NativeArray<float> m_SlotDenseContributionWeights;
        [ReadOnly]
        readonly NativeArray<int> m_SlotContributionCounts;
        [ReadOnly]
        readonly NativeArray<float> m_SlotOutputWeights;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_SlotLeftFootFeatures;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_SlotRightFootFeatures;
        [ReadOnly]
        readonly NativeArray<byte> m_SlotHasFootFeatures;
        [ReadOnly]
        readonly NativeArray<AnimationPoseAvailability> m_SlotAvailability;
        [ReadOnly]
        readonly NativeArray<ulong> m_SlotContinuityIdentities;
        [ReadOnly]
        readonly NativeArray<PoseDiscontinuityNative> m_SlotDiscontinuities;
        [ReadOnly]
        readonly NativeArray<AnimationPoseNativeInvalidReason> m_SlotInvalidReasons;
        [ReadOnly]
        readonly NativeArray<ulong> m_SlotCompletedAt;

        NativeArray<AnimationLocalBonePose> m_ValueDenseLocalPoses;
        NativeArray<AnimationBlendBoneVelocity> m_ValueDenseVelocities;
        NativeArray<float> m_ValuePoseParameters;
        NativeArray<byte> m_ValuePoseParameterAvailability;
        NativeArray<AnimationPrimitivePoseContribution> m_ValueContributions;
        NativeArray<float> m_ValueDenseContributionWeights;
        NativeArray<int> m_ValueContributionCounts;
        NativeArray<float> m_ValueOutputWeights;
        NativeArray<AnimationFootFeatureSample> m_ValueLeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_ValueRightFootFeatures;
        NativeArray<byte> m_ValueHasFootFeatures;
        NativeArray<AnimationPoseAvailability> m_ValueAvailability;
        NativeArray<ulong> m_ValueContinuityIdentities;
        NativeArray<PoseDiscontinuityNative> m_ValueDiscontinuities;
        NativeArray<AnimationPoseNativeInvalidReason> m_ValueInvalidReasons;
        NativeArray<ulong> m_FrameCacheCompletedAt;
        NativeArray<ulong> m_StageCompletedAt;
        NativeArray<int> m_StageInvalidOperationIndex;
        NativeArray<AnimationPoseNativeInvalidReason> m_PoseGraphInvalidReason;
        NativeArray<int> m_PoseGraphInvalidOperationIndex;
        NativeArray<ulong> m_PoseGraphCompletedAt;

        readonly int m_PlayerCount;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_PoseValueCount;
        readonly int m_FootPlacementCount;
        readonly int m_ContributionStride;
        readonly int m_OutputOperationIndex;
        readonly int m_OutputValueIndex;
        readonly int m_LeftFootBoneIndex;
        readonly int m_RightFootBoneIndex;
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        readonly int m_AnimationSlotNodeOffset;
        readonly ulong m_CompletionIdentity;
        readonly CharacterPoseConstraintRuntime m_PoseConstraints;
        readonly bool m_RecordDiagnostics;
        ulong m_FrameSequence;

        internal CharacterPoseGraphStagedExecutor(
            CharacterPoseGraphNativeProgram program,
            PoseInertializationNativeProgram inertializationProgram,
            CharacterPoseGraphNativeBinding binding,
            CharacterPoseConstraintRuntime poseConstraints,
            bool recordDiagnostics)
        {
            RequireValidConfiguration(program, inertializationProgram, binding, poseConstraints);

            m_Operations = program.Operations;
            m_Stages = program.Stages;
            m_DenseBoneMasks = program.DenseBoneMasks;
            m_AdditiveReferences = program.AdditiveReferences;
            m_ParameterPolicies = program.ParameterPolicies;
            m_ParameterDefaults = program.ParameterDefaults;
            m_ParentIndices = program.ParentIndices;
            m_BlendCurves = program.BlendCurves;
            m_BlendCurveSegments = program.BlendCurveSegments;
            m_BlendProfiles = program.BlendProfiles;
            m_BlendDenseProfiles = program.BlendDenseProfiles;
            m_Inertializations = inertializationProgram.Nodes;
            m_ModifyBones = program.ModifyBones;
            m_RootOrientationWarps = program.RootOrientationWarps;
            m_RootOrientationWarpControls = program.RootOrientationWarpControls;
            m_PoseBoneIkGoalRanges = program.PoseBoneIkGoalRanges;
            m_PoseBoneIkGoalDescriptors = program.PoseBoneIkGoalDescriptors;
            m_FullBodyIkGoalContributionInputValueIndices =
                program.FullBodyIkGoalContributionInputValueIndices;
            m_FullBodyIkGoalSetValueCount = program.FullBodyIkGoalSetValueCount;
            m_FootPlacementCount = program.FootPlacementCount;
            m_LinkedPoseCalls = program.LinkedPoseCalls;
            m_LinkedPoseCandidates = program.LinkedPoseCandidates;
            m_LinkedPoseCallControls = program.LinkedPoseCallControls;
            m_LinkedPoseActiveFragments = program.LinkedPoseActiveFragments;
            m_StateMachineControls = program.StateMachineControls;
            m_AnimationSlotControls = program.AnimationSlotControls;
            m_InertialRules = inertializationProgram.Rules;
            m_InertialCurveSegments = inertializationProgram.CurveSegments;
            m_InertialDenseProfiles = inertializationProgram.DenseProfiles;
            m_InertialParameterModes = inertializationProgram.ParameterModes;
            m_InertialStates = inertializationProgram.States;
            m_InertialHistory = inertializationProgram.HistoryPoses;
            m_InertialHistoryVelocities = inertializationProgram.HistoryVelocities;
            m_InertialHistoryParameters = inertializationProgram.HistoryParameters;
            m_InertialHistoryParameterAvailability = inertializationProgram.HistoryParameterAvailability;
            m_InertialHistoryLeftFeet = inertializationProgram.HistoryLeftFeet;
            m_InertialHistoryRightFeet = inertializationProgram.HistoryRightFeet;
            m_InertialHistoryHasFeet = inertializationProgram.HistoryHasFeet;
            m_InertialAccumulatorLeftFeet = inertializationProgram.AccumulatorLeftFeet;
            m_InertialAccumulatorRightFeet = inertializationProgram.AccumulatorRightFeet;
            m_InertialAccumulatorHasFeet = inertializationProgram.AccumulatorHasFeet;
            m_InertialPositionResiduals = inertializationProgram.PositionResiduals;
            m_InertialRotationResiduals = inertializationProgram.RotationResiduals;
            m_InertialScaleResiduals = inertializationProgram.ScaleResiduals;
            m_InertialLinearVelocityResiduals = inertializationProgram.LinearVelocityResiduals;
            m_InertialAngularVelocityResiduals = inertializationProgram.AngularVelocityResiduals;
            m_InertialScaleVelocityResiduals = inertializationProgram.ScaleVelocityResiduals;
            m_InertialParameterResiduals = inertializationProgram.ParameterResiduals;
            m_InertialResetRequests = inertializationProgram.ResetRequests;
            m_CommittedInertialStates = inertializationProgram.CommittedStates;
            m_CommittedInertialHistory = inertializationProgram.CommittedHistoryPoses;
            m_CommittedInertialHistoryVelocities = inertializationProgram.CommittedHistoryVelocities;
            m_CommittedInertialHistoryParameters = inertializationProgram.CommittedHistoryParameters;
            m_CommittedInertialHistoryParameterAvailability = inertializationProgram.CommittedHistoryParameterAvailability;
            m_CommittedInertialHistoryLeftFeet = inertializationProgram.CommittedHistoryLeftFeet;
            m_CommittedInertialHistoryRightFeet = inertializationProgram.CommittedHistoryRightFeet;
            m_CommittedInertialHistoryHasFeet = inertializationProgram.CommittedHistoryHasFeet;
            m_CommittedInertialAccumulatorLeftFeet = inertializationProgram.CommittedAccumulatorLeftFeet;
            m_CommittedInertialAccumulatorRightFeet = inertializationProgram.CommittedAccumulatorRightFeet;
            m_CommittedInertialAccumulatorHasFeet = inertializationProgram.CommittedAccumulatorHasFeet;
            m_CommittedInertialPositionResiduals = inertializationProgram.CommittedPositionResiduals;
            m_CommittedInertialRotationResiduals = inertializationProgram.CommittedRotationResiduals;
            m_CommittedInertialScaleResiduals = inertializationProgram.CommittedScaleResiduals;
            m_CommittedInertialLinearVelocityResiduals = inertializationProgram.CommittedLinearVelocityResiduals;
            m_CommittedInertialAngularVelocityResiduals = inertializationProgram.CommittedAngularVelocityResiduals;
            m_CommittedInertialScaleVelocityResiduals = inertializationProgram.CommittedScaleVelocityResiduals;
            m_CommittedInertialParameterResiduals = inertializationProgram.CommittedParameterResiduals;
            m_AnimationSlotNodeOffset = inertializationProgram.SlotNodeOffset;

            m_SlotRanges = binding.SlotRanges;
            m_SlotDenseLocalPoses = binding.SlotDenseLocalPoses;
            m_SlotDenseVelocities = binding.SlotDenseVelocities;
            m_SlotPoseParameters = binding.SlotPoseParameters;
            m_SlotPoseParameterAvailability = binding.SlotPoseParameterAvailability;
            m_SlotContributions = binding.SlotContributions;
            m_SlotDenseContributionWeights = binding.SlotDenseContributionWeights;
            m_SlotContributionCounts = binding.SlotContributionCounts;
            m_SlotOutputWeights = binding.SlotOutputWeights;
            m_SlotLeftFootFeatures = binding.SlotLeftFootFeatures;
            m_SlotRightFootFeatures = binding.SlotRightFootFeatures;
            m_SlotHasFootFeatures = binding.SlotHasFootFeatures;
            m_SlotAvailability = binding.SlotAvailability;
            m_SlotContinuityIdentities = binding.SlotContinuityIdentities;
            m_SlotDiscontinuities = binding.SlotDiscontinuities;
            m_SlotInvalidReasons = binding.SlotInvalidReasons;
            m_SlotCompletedAt = binding.SlotCompletedAt;

            m_ValueDenseLocalPoses = binding.ValueDenseLocalPoses;
            m_ValueDenseVelocities = binding.ValueDenseVelocities;
            m_ValuePoseParameters = binding.ValuePoseParameters;
            m_ValuePoseParameterAvailability = binding.ValuePoseParameterAvailability;
            m_ValueContributions = binding.ValueContributions;
            m_ValueDenseContributionWeights = binding.ValueDenseContributionWeights;
            m_ValueContributionCounts = binding.ValueContributionCounts;
            m_ValueOutputWeights = binding.ValueOutputWeights;
            m_ValueLeftFootFeatures = binding.ValueLeftFootFeatures;
            m_ValueRightFootFeatures = binding.ValueRightFootFeatures;
            m_ValueHasFootFeatures = binding.ValueHasFootFeatures;
            m_ValueAvailability = binding.ValueAvailability;
            m_ValueContinuityIdentities = binding.ValueContinuityIdentities;
            m_ValueDiscontinuities = binding.ValueDiscontinuities;
            m_ValueInvalidReasons = binding.ValueInvalidReasons;
            m_FrameCacheCompletedAt = binding.FrameCacheCompletedAt;
            m_StageCompletedAt = binding.StageCompletedAt;
            m_StageInvalidOperationIndex = binding.StageInvalidOperationIndex;
            m_PoseGraphInvalidReason = binding.PoseGraphInvalidReason;
            m_PoseGraphInvalidOperationIndex = binding.PoseGraphInvalidOperationIndex;
            m_PoseGraphCompletedAt = binding.PoseGraphCompletedAt;

            m_PlayerCount = binding.Layout.PlayerCount;
            m_BoneCount = binding.Layout.BoneCount;
            m_ParameterCount = binding.Layout.ParameterCount;
            m_PoseValueCount = binding.Layout.PoseValueCount;
            m_ContributionStride = binding.Layout.PoseValueContributionStride;
            m_OutputOperationIndex = program.OutputOperationIndex;
            m_OutputValueIndex = program.OutputValueIndex;
            m_LeftFootBoneIndex = program.LeftFootBoneIndex;
            m_RightFootBoneIndex = program.RightFootBoneIndex;
            m_RigId = program.RigId;
            m_RigRevision = program.RigRevision;
            m_CompletionIdentity = binding.CompletionIdentity;
            m_PoseConstraints = poseConstraints;
            m_RecordDiagnostics = recordDiagnostics;
            m_FrameSequence = 0;
        }

        internal void BeginStagedEvaluation(ulong frameSequence)
        {
            if (frameSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(frameSequence));
            m_FrameSequence = frameSequence;
            for (int i = 0; i < m_FrameCacheCompletedAt.Length; i++)
                m_FrameCacheCompletedAt[i] = 0;
            m_PoseGraphInvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_PoseGraphInvalidOperationIndex[0] = -1;
            m_PoseGraphCompletedAt[0] = 0;
        }

        internal bool ExecuteStage(
            int stageIndex,
            float deltaSeconds,
            in CharacterPoseWorldAwareStageInput worldInput)
        {
            if ((uint)stageIndex >= (uint)m_Stages.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f ||
                stageIndex > 0 &&
                m_StageCompletedAt[m_Stages[stageIndex - 1].CompletionIndex] !=
                m_CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Pose stage #{stageIndex} cannot execute outside compiled order.");
            }

            AnimationPoseGraphNativeStage stage = m_Stages[stageIndex];
            bool stop = false;
            for (int operationIndex = stage.OperationStart;
                 operationIndex < stage.OperationStart + stage.OperationCount;
                operationIndex++)
            {
                AnimationPoseGraphNativeOperation operation = m_Operations[operationIndex];
                bool producesPose = operation.OutputValueIndex >= 0;
                if (operation.LinkedPoseFragmentIndex >= 0 &&
                    !IsLinkedPoseFragmentActive(operation.LinkedPoseFragmentIndex))
                {
                    if (producesPose)
                    {
                        using (ValueResetMarker.Auto())
                            ResetValue(operation.OutputValueIndex);
                    }
                    m_FrameCacheCompletedAt[operation.FrameCacheIndex] = m_CompletionIdentity;
                    continue;
                }
                if (producesPose)
                {
                    using (ValueResetMarker.Auto())
                        ResetValue(operation.OutputValueIndex);
                }
                bool valueOperationValid = true;
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.SelectedPosePlayer:
                    case CharacterPoseOperationCode.BlendSpacePlayer:
                    case CharacterPoseOperationCode.ClipPlayer:
                    case CharacterPoseOperationCode.BlendStack:
                        using (PlayerInputMarker.Auto())
                            EvaluatePlayerInput(operation);
                        break;
                    case CharacterPoseOperationCode.AnimationSlot:
                        using (SlotMarker.Auto())
                            EvaluateAnimationSlot(operation, deltaSeconds);
                        break;
                    case CharacterPoseOperationCode.StatePoseOutput:
                        using (StateMarker.Auto())
                            EvaluateStatePoseOutput(operation);
                        break;
                    case CharacterPoseOperationCode.PoseStateMachine:
                        using (StateMarker.Auto())
                            EvaluatePoseStateMachine(operation);
                        break;
                    case CharacterPoseOperationCode.Inertialization:
                        using (InertializationMarker.Auto())
                            EvaluateInertialization(operation, deltaSeconds);
                        break;
                    case CharacterPoseOperationCode.BlendPose:
                        using (BlendMarker.Auto())
                            EvaluateBlendPose(operation);
                        break;
                    case CharacterPoseOperationCode.LayeredBoneBlend:
                        using (BlendMarker.Auto())
                            EvaluateLayeredBoneBlend(operation);
                        break;
                    case CharacterPoseOperationCode.AdditivePose:
                        using (BlendMarker.Auto())
                            EvaluateAdditivePose(operation);
                        break;
                    case CharacterPoseOperationCode.PoseParameterResolve:
                        using (BlendMarker.Auto())
                            EvaluatePoseParameterResolve(operation);
                        break;
                    case CharacterPoseOperationCode.ModifyBone:
                        using (ConstraintMarker.Auto())
                            EvaluateModifyBone(operation);
                        break;
                    case CharacterPoseOperationCode.RootOrientationWarp:
                        using (ConstraintMarker.Auto())
                            EvaluateRootOrientationWarp(operation);
                        break;
                    case CharacterPoseOperationCode.PoseBoneIKGoals:
                        using (IkGoalMarker.Auto())
                            valueOperationValid = EvaluatePoseBoneIkGoals(operation);
                        break;
                    case CharacterPoseOperationCode.FootPlacement:
                        using (IkGoalMarker.Auto())
                            valueOperationValid = EvaluateWorldAwareFootGoal(
                                operation,
                                in worldInput);
                        break;
                    case CharacterPoseOperationCode.FullBodyIkGoalAssembler:
                        using (IkGoalMarker.Auto())
                            valueOperationValid = EvaluateGoalAssembler(operation);
                        break;
                    case CharacterPoseOperationCode.FullBodyIK:
                        using (FullBodyIkMarker.Auto())
                            EvaluateFullBodyIk(operation);
                        break;
                    case CharacterPoseOperationCode.LinkedPoseCall:
                        using (LinkedPoseMarker.Auto())
                            valueOperationValid = EvaluateLinkedPoseCall(operation);
                        break;
                    case CharacterPoseOperationCode.LocalToComponentPose:
                        using (SpaceConversionMarker.Auto())
                            EvaluateLocalToComponentPose(operation);
                        break;
                    case CharacterPoseOperationCode.ComponentToLocalPose:
                        using (SpaceConversionMarker.Auto())
                            EvaluateComponentToLocalPose(operation);
                        break;
                    case CharacterPoseOperationCode.OutputPose:
                        using (OutputMarker.Auto())
                            EvaluateOutputPose(operation);
                        break;
                    default:
                        if (producesPose)
                        {
                            SetInvalid(
                                operation.OutputValueIndex,
                                (ulong)operation.Index + 1UL,
                                AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                                operation.Index);
                        }
                        else
                        {
                            valueOperationValid = false;
                        }
                        break;
                }

                bool valueValid = valueOperationValid;
                AnimationPoseNativeInvalidReason reason = valueOperationValid
                    ? AnimationPoseNativeInvalidReason.None
                    : ValueOperationInvalidReason(operation);
                if (producesPose)
                {
                    using (ValueValidationMarker.Auto())
                    {
                        valueValid = TryValidateValueEnvelope(
                            operation.OutputValueIndex,
                            out reason);
                    }
                }
                if (!valueValid)
                {
                    if (producesPose)
                    {
                        SetInvalid(
                            operation.OutputValueIndex,
                            m_ValueContinuityIdentities[operation.OutputValueIndex],
                            reason,
                            operation.Index);
                    }
                    else
                    {
                        m_PoseGraphInvalidReason[0] = reason;
                        m_PoseGraphInvalidOperationIndex[0] = operation.Index;
                    }
                    m_StageInvalidOperationIndex[stage.DiagnosticIndex] = operation.Index;
                    stop = true;
                }
                m_FrameCacheCompletedAt[operation.FrameCacheIndex] = m_CompletionIdentity;
                if (stop)
                    break;
            }
            if (!stop)
                m_StageCompletedAt[stage.CompletionIndex] = m_CompletionIdentity;
            return !stop;
        }

        internal void ExecuteSequencePreview(int sourceOperationIndex)
        {
            AnimationPoseGraphNativeOperation sourceOperation = default;
            bool found = false;
            for (int i = 0; i < m_Operations.Length; i++)
            {
                if (m_Operations[i].Index != sourceOperationIndex)
                    continue;
                sourceOperation = m_Operations[i];
                found = true;
                break;
            }
            if (!found || sourceOperation.Code != CharacterPoseOperationCode.ClipPlayer)
                throw new InvalidOperationException(
                    $"Clip Preview source operation #{sourceOperationIndex} is not a compiled Clip Player.");

            ResetValue(sourceOperation.OutputValueIndex);
            EvaluatePlayerInput(sourceOperation);
            m_FrameCacheCompletedAt[sourceOperation.FrameCacheIndex] = m_CompletionIdentity;
            if (m_ValueAvailability[sourceOperation.OutputValueIndex] == AnimationPoseAvailability.Pose &&
                sourceOperation.OutputValueIndex != m_OutputValueIndex)
            {
                ResetValue(m_OutputValueIndex);
                if (!TryCopyValue(
                        sourceOperation.OutputValueIndex,
                        m_OutputValueIndex,
                        sourceOperation.Index))
                {
                    SetInvalid(
                        m_OutputValueIndex,
                        m_ValueContinuityIdentities[sourceOperation.OutputValueIndex],
                        AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid,
                        sourceOperation.Index);
                }
            }
            for (int i = 0; i < m_FrameCacheCompletedAt.Length; i++)
                m_FrameCacheCompletedAt[i] = m_CompletionIdentity;
            for (int i = 0; i < m_StageCompletedAt.Length; i++)
                m_StageCompletedAt[i] = m_CompletionIdentity;
            CompleteStagedEvaluation();
        }

        internal void CompleteStagedEvaluation()
        {
            if (m_ValueAvailability[m_OutputValueIndex] != AnimationPoseAvailability.Pose)
            {
                if (m_ValueAvailability[m_OutputValueIndex] != AnimationPoseAvailability.Invalid)
                {
                    SetInvalid(
                        m_OutputValueIndex,
                        m_ValueContinuityIdentities[m_OutputValueIndex],
                        AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid,
                        m_OutputOperationIndex);
                }
                else if (m_PoseGraphInvalidReason[0] == AnimationPoseNativeInvalidReason.None)
                {
                    RecordGraphInvalid(
                        NormalizeInvalidReason(m_ValueInvalidReasons[m_OutputValueIndex]),
                        m_OutputOperationIndex);
                }
            }
            else if (m_ValueContributionCounts[m_OutputValueIndex] <= 0 ||
                     m_ValueInvalidReasons[m_OutputValueIndex] != AnimationPoseNativeInvalidReason.None)
            {
                SetInvalid(
                    m_OutputValueIndex,
                    m_ValueContinuityIdentities[m_OutputValueIndex],
                    AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid,
                    m_OutputOperationIndex);
            }
            else if (!TryValidateValueDeep(
                         m_OutputValueIndex,
                         out AnimationPoseNativeInvalidReason reason))
            {
                SetInvalid(
                    m_OutputValueIndex,
                    m_ValueContinuityIdentities[m_OutputValueIndex],
                    reason,
                    m_OutputOperationIndex);
            }

            m_PoseGraphCompletedAt[0] = m_CompletionIdentity;
        }

        void EvaluatePlayerInput(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int slotIndex = operation.PhysicalPlayerIndex;
            ulong continuity = slotIndex >= 0 && slotIndex < m_PlayerCount
                ? m_SlotContinuityIdentities[slotIndex]
                : 0UL;
            if (slotIndex < 0 || slotIndex >= m_PlayerCount ||
                m_SlotCompletedAt[slotIndex] != m_CompletionIdentity)
            {
                SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }

            AnimationPoseAvailability availability = m_SlotAvailability[slotIndex];
            AnimationPoseNativeInvalidReason slotReason = m_SlotInvalidReasons[slotIndex];
            PoseDiscontinuityNative discontinuity = m_SlotDiscontinuities[slotIndex];
            if (availability == AnimationPoseAvailability.Invalid)
            {
                SetInvalid(output, continuity, NormalizeInvalidReason(slotReason), operation.Index);
                return;
            }
            if (!IsAvailability(availability) || slotReason != AnimationPoseNativeInvalidReason.None || continuity == 0 ||
                !discontinuity.IsValid || discontinuity.IsPresent && discontinuity.CompletionIdentity != m_CompletionIdentity)
            {
                SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (availability == AnimationPoseAvailability.NoPose &&
                operation.AnimationSelectionAvailabilityPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.RequiredPoseMissing, operation.Index);
                return;
            }

            AnimationPlayerPoseNativeRange range = m_SlotRanges[slotIndex];
            int contributionCount = m_SlotContributionCounts[slotIndex];
            float outputWeight = m_SlotOutputWeights[slotIndex];
            byte hasFootFeatures = m_SlotHasFootFeatures[slotIndex];
            if (range.PhysicalPlayerIndex != slotIndex || contributionCount < 0 ||
                contributionCount > range.ContributionCapacity || contributionCount > m_ContributionStride ||
                !IsWeight(outputWeight) || hasFootFeatures > 1)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                return;
            }

            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = m_SlotPoseParameters[range.ParameterOffset + parameter];
                byte parameterAvailable = m_SlotPoseParameterAvailability[range.ParameterOffset + parameter];
                if (!float.IsFinite(value) || parameterAvailable > 1)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotParameterInvalid, operation.Index);
                    return;
                }
                m_ValuePoseParameters[ParameterOffset(output) + parameter] = value;
                m_ValuePoseParameterAvailability[ParameterOffset(output) + parameter] = parameterAvailable;
            }

            if (availability == AnimationPoseAvailability.NoPose)
            {
                if (contributionCount != 0 || outputWeight != 0f || hasFootFeatures != 0)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                    return;
                }
                m_ValueAvailability[output] = AnimationPoseAvailability.NoPose;
                m_ValueContinuityIdentities[output] = continuity;
                m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
                return;
            }

            if (contributionCount <= 0)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                return;
            }
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                AnimationLocalBonePose pose = m_SlotDenseLocalPoses[range.PoseOffset + bone];
                AnimationBlendBoneVelocity velocity = m_SlotDenseVelocities[range.VelocityOffset + bone];
                if (!pose.IsValid || !velocity.IsValid)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPoseInvalid, operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = pose;
                m_ValueDenseVelocities[PoseOffset(output) + bone] = velocity;
            }

            if (hasFootFeatures == 1 &&
                (!IsValidFootFeature(m_SlotLeftFootFeatures[slotIndex]) ||
                 !IsValidFootFeature(m_SlotRightFootFeatures[slotIndex])))
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid, operation.Index);
                return;
            }

            int destinationContributionOffset = ContributionOffset(output);
            int destinationDenseOffset = ContributionBoneOffset(output);
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                AnimationPrimitivePoseContribution primitive =
                    m_SlotContributions[range.ContributionOffset + contribution];
                if (!IsValidPrimitiveContribution(primitive) || primitive.PhysicalPlayerIndex != slotIndex)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, operation.Index);
                    return;
                }
                m_ValueContributions[destinationContributionOffset + contribution] = primitive;
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float weight = m_SlotDenseContributionWeights[
                        range.DenseContributionWeightOffset + contribution * m_BoneCount + bone];
                    if (!IsWeight(weight))
                    {
                        SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, operation.Index);
                        return;
                    }
                    m_ValueDenseContributionWeights[
                        destinationDenseOffset + contribution * m_BoneCount + bone] = weight;
                }
            }

            m_ValueContributionCounts[output] = contributionCount;
            m_ValueOutputWeights[output] = outputWeight;
            m_ValueLeftFootFeatures[output] = hasFootFeatures == 1
                ? m_SlotLeftFootFeatures[slotIndex]
                : default;
            m_ValueRightFootFeatures[output] = hasFootFeatures == 1
                ? m_SlotRightFootFeatures[slotIndex]
                : default;
            m_ValueHasFootFeatures[output] = hasFootFeatures;
            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_ValueContinuityIdentities[output] = continuity;
            m_ValueDiscontinuities[output] = m_SlotDiscontinuities[slotIndex];
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
        }

        void EvaluateAnimationSlot(
            AnimationPoseGraphNativeOperation operation,
            float deltaSeconds)
        {
            int source = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!IsInputReady(source, operation.Index) ||
                m_ValueAvailability[source] != AnimationPoseAvailability.Pose)
            {
                SetInvalid(
                    output,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }

            EvaluatePlayerInput(operation);
            if (m_ValueAvailability[output] == AnimationPoseAvailability.Invalid)
                return;
            if (m_ValueAvailability[output] == AnimationPoseAvailability.NoPose)
            {
                if (!TryCopyValue(source, output, operation.Index))
                {
                    SetInvalid(
                        output,
                        m_ValueContinuityIdentities[source],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        operation.Index);
                }
                else
                {
                    EvaluateAnimationSlotInertialization(operation, deltaSeconds);
                }
                return;
            }

            int actionContributionCount = m_ValueContributionCounts[output];
            int actionContributionStart = m_ContributionStride - actionContributionCount;
            if (actionContributionCount <= 0 || actionContributionStart < 0)
            {
                SetInvalid(
                    output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                    operation.Index);
                return;
            }
            for (int contribution = actionContributionCount - 1; contribution >= 0; contribution--)
            {
                int target = actionContributionStart + contribution;
                m_ValueContributions[ContributionOffset(output) + target] =
                    m_ValueContributions[ContributionOffset(output) + contribution];
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    SetContributionBoneWeight(
                        output,
                        target,
                        bone,
                        GetContributionBoneWeight(output, contribution, bone));
                }
            }

            ulong actionContinuity = m_ValueContinuityIdentities[output];
            float actionOutputWeight = m_ValueOutputWeights[output];
            byte actionHasFootFeatures = m_ValueHasFootFeatures[output];
            AnimationFootFeatureSample actionLeftFoot = m_ValueLeftFootFeatures[output];
            AnimationFootFeatureSample actionRightFoot = m_ValueRightFootFeatures[output];
            if (!TryGetBoneOutputWeight(output, m_LeftFootBoneIndex, out float actionLeftFootWeight) ||
                !TryGetBoneOutputWeight(output, m_RightFootBoneIndex, out float actionRightFootWeight))
            {
                SetInvalid(
                    output,
                    actionContinuity,
                    AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                    operation.Index);
                return;
            }
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!TryGetBoneOutputWeight(output, bone, out float actionBoneWeight))
                {
                    SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        operation.Index);
                    return;
                }
                AnimationLocalBonePose actionPose = m_ValueDenseLocalPoses[PoseOffset(output) + bone];
                AnimationBlendBoneVelocity actionVelocity = m_ValueDenseVelocities[PoseOffset(output) + bone];
                AnimationBlendBoneVelocity sourceVelocity = m_ValueDenseVelocities[PoseOffset(source) + bone];
                if (!TryBlendPose(
                        m_ValueDenseLocalPoses[PoseOffset(source) + bone],
                        actionPose,
                        actionBoneWeight,
                        out AnimationLocalBonePose pose))
                {
                    SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotPoseInvalid,
                        operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = pose;
                m_ValueDenseVelocities[PoseOffset(output) + bone] = new AnimationBlendBoneVelocity(
                    Vector3.LerpUnclamped(sourceVelocity.Linear, actionVelocity.Linear, actionBoneWeight),
                    Vector3.LerpUnclamped(sourceVelocity.Angular, actionVelocity.Angular, actionBoneWeight),
                    Vector3.LerpUnclamped(sourceVelocity.Scale, actionVelocity.Scale, actionBoneWeight));
            }

            m_ValueContributionCounts[output] = 0;
            m_ValueOutputWeights[output] = actionOutputWeight;
            for (int contribution = 0; contribution < actionContributionCount; contribution++)
            {
                if (!TryAddContribution(
                        operation,
                        output,
                        actionContributionStart + contribution,
                        output,
                        output,
                        true,
                        false))
                {
                    SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        operation.Index);
                    return;
                }
            }
            for (int contribution = 0; contribution < m_ValueContributionCounts[source]; contribution++)
            {
                if (!TryAddAnimationSlotBaseContribution(
                        source,
                        contribution,
                        output,
                        actionContributionStart,
                        actionContributionCount,
                        actionOutputWeight,
                        actionLeftFootWeight,
                        actionRightFootWeight))
                {
                    SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        operation.Index);
                    return;
                }
            }

            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_ValueOutputWeights[output] = UnionWeight(
                m_ValueOutputWeights[source],
                actionOutputWeight);
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[source],
                actionContinuity,
                operation.Index);
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            if (!TryCopyParameters(source, output) ||
                !TryResolveAnimationSlotFootFeatures(
                    source,
                    output,
                    actionHasFootFeatures,
                    actionLeftFoot,
                    actionRightFoot,
                    actionLeftFootWeight,
                    actionRightFootWeight))
            {
                SetInvalid(
                    output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
            }
            if (m_ValueAvailability[output] == AnimationPoseAvailability.Pose)
                EvaluateAnimationSlotInertialization(operation, deltaSeconds);
        }

        void EvaluateAnimationSlotInertialization(
            AnimationPoseGraphNativeOperation operation,
            float deltaSeconds)
        {
            int output = operation.OutputValueIndex;
            if ((uint)operation.AnimationSlotIndex >= (uint)m_AnimationSlotControls.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                SetInvalid(
                    output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            int stateIndex = m_AnimationSlotNodeOffset + operation.AnimationSlotIndex;
            if ((uint)stateIndex >= (uint)m_InertialStates.Length)
            {
                SetInvalid(
                    output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            CharacterAnimationSlotNativeControl control =
                m_AnimationSlotControls[operation.AnimationSlotIndex];
            PoseInertializationNativeState state = CommittedInertialState(stateIndex);
            PrepareInertialNode(stateIndex, in state);
            if (control.Generation == 0 || control.Generation < state.LastEventIdentity)
            {
                SetInvalid(
                    output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (control.Generation > state.LastEventIdentity)
            {
                state.LastEventIdentity = control.Generation;
                state.Active = 0;
                state.ElapsedSeconds = 0f;
                if (control.Mode == CharacterAnimationSlotNativeTransitionMode.Inertialization)
                {
                    int ruleIndex = RequireInertialRule(
                        stateIndex,
                        control.SourceProducerIndex,
                        control.TargetProducerIndex);
                    if (ruleIndex < 0 ||
                        m_InertialRules[ruleIndex].Mode != PoseInertializationMode.Inertialize)
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Invalid;
                        m_InertialStates[stateIndex] = state;
                        SetInvalid(
                            output,
                            m_ValueContinuityIdentities[output],
                            AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                            operation.Index);
                        return;
                    }
                    if (state.HasHistory != 0)
                    {
                        CaptureInertialResidual(stateIndex, output, ruleIndex, ref state);
                    }
                    else
                    {
                        state.ActiveRuleIndex = ruleIndex;
                        state.RuntimeState = PoseInertializationRuntimeState.Anchor;
                    }
                }
                else
                {
                    state.RuntimeState =
                        control.Mode == CharacterAnimationSlotNativeTransitionMode.StandardBlend
                            ? PoseInertializationRuntimeState.HardCut
                            : PoseInertializationRuntimeState.Anchor;
                }
            }
            else if (state.Active != 0)
            {
                state.RuntimeState = PoseInertializationRuntimeState.Continue;
            }

            if (state.Active != 0)
            {
                PoseInertializationNativeRule rule = m_InertialRules[state.ActiveRuleIndex];
                bool anyActive = false;
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    int residualIndex = stateIndex * m_BoneCount + bone;
                    float duration = state.ActiveDurationSeconds *
                                     m_InertialDenseProfiles[rule.ProfileOffset + bone];
                    EvaluateInertialEnvelope(
                        rule,
                        state.ElapsedSeconds,
                        duration,
                        out _,
                        out float weight,
                        out float derivative);
                    anyActive |= state.ElapsedSeconds < duration;
                    AnimationLocalBonePose target = m_ValueDenseLocalPoses[PoseOffset(output) + bone];
                    AnimationBlendBoneVelocity targetVelocity = m_ValueDenseVelocities[PoseOffset(output) + bone];
                    Vector3 positionBase = m_InertialPositionResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 rotationBase = m_InertialRotationResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleBase = m_InertialScaleResiduals[residualIndex] +
                                        state.ElapsedSeconds * m_InertialScaleVelocityResiduals[residualIndex];
                    Vector3 linear = targetVelocity.Linear + derivative * positionBase +
                                     weight * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 angular = targetVelocity.Angular + derivative * rotationBase +
                                      weight * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleVelocity = targetVelocity.Scale + derivative * scaleBase +
                                            weight * m_InertialScaleVelocityResiduals[residualIndex];
                    if (!IsFinite(linear) || !IsFinite(angular) || !IsFinite(scaleVelocity))
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Invalid;
                        m_InertialStates[stateIndex] = state;
                        SetInvalid(
                            output,
                            m_ValueContinuityIdentities[output],
                            AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                            operation.Index);
                        return;
                    }
                    m_ValueDenseLocalPoses[PoseOffset(output) + bone] = new AnimationLocalBonePose(
                        target.Position + weight * positionBase,
                        AnimationPoseMath.QuaternionExp(weight * rotationBase) * target.Rotation,
                        target.Scale + weight * scaleBase);
                    m_ValueDenseVelocities[PoseOffset(output) + bone] =
                        new AnimationBlendBoneVelocity(linear, angular, scaleVelocity);
                }
                ApplyInertialParameters(stateIndex, output, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                ApplyInertialFootFeatures(stateIndex, output, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                state.ElapsedSeconds += deltaSeconds;
                state.LastDeltaSeconds = deltaSeconds;
                if (!anyActive)
                {
                    state.Active = 0;
                    state.RuntimeState = PoseInertializationRuntimeState.Complete;
                }
            }
            CommitInertialHistory(stateIndex, output, ref state);
            if (state.RuntimeState == 0)
                state.RuntimeState = PoseInertializationRuntimeState.Anchor;
            state.OutputCompletionIdentity = m_CompletionIdentity;
            m_InertialStates[stateIndex] = state;
        }

        bool TryAddAnimationSlotBaseContribution(
            int sourceValue,
            int sourceIndex,
            int output,
            int actionContributionStart,
            int actionContributionCount,
            float actionOutputWeight,
            float actionLeftFootWeight,
            float actionRightFootWeight)
        {
            AnimationPrimitivePoseContribution source =
                m_ValueContributions[ContributionOffset(sourceValue) + sourceIndex];
            if (!IsValidPrimitiveContribution(source))
                return false;
            float scalarWeight = source.Weight * Mathf.Clamp01(1f - actionOutputWeight);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(1f - actionLeftFootWeight);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(1f - actionRightFootWeight);
            if (!IsWeight(scalarWeight) || !IsWeight(leftWeight) || !IsWeight(rightWeight))
                return false;

            int targetIndex = FindContribution(output, source);
            if (targetIndex < 0)
            {
                targetIndex = m_ValueContributionCounts[output];
                if (targetIndex >= m_ContributionStride)
                    return false;
                m_ValueContributionCounts[output] = targetIndex + 1;
                ClearContributionWeights(output, targetIndex);
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
            }
            else
            {
                AnimationPrimitivePoseContribution current =
                    m_ValueContributions[ContributionOffset(output) + targetIndex];
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        current.PhysicalPlayerIndex,
                        current.PhysicalSourceIndex,
                        current.PhysicalSourceGeneration,
                        current.Kind,
                        current.SourceOwnerIndex,
                        current.ContributionContinuityIdentity,
                        Mathf.Clamp01(current.Weight + scalarWeight),
                        Mathf.Clamp01(current.LeftFootWeight + leftWeight),
                        Mathf.Clamp01(current.RightFootWeight + rightWeight));
            }

            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float actionWeight = 0f;
                for (int action = 0; action < actionContributionCount; action++)
                {
                    actionWeight += GetContributionBoneWeight(
                        output,
                        actionContributionStart + action,
                        bone);
                }
                if (!float.IsFinite(actionWeight))
                    return false;
                float weight = GetContributionBoneWeight(sourceValue, sourceIndex, bone) *
                               Mathf.Clamp01(1f - actionWeight);
                float combined = Mathf.Clamp01(
                    GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!IsWeight(combined))
                    return false;
                SetContributionBoneWeight(output, targetIndex, bone, combined);
            }
            return true;
        }

        bool TryResolveAnimationSlotFootFeatures(
            int source,
            int output,
            byte actionHasFootFeatures,
            AnimationFootFeatureSample actionLeftFoot,
            AnimationFootFeatureSample actionRightFoot,
            float actionLeftFootWeight,
            float actionRightFootWeight)
        {
            bool hasSource = m_ValueHasFootFeatures[source] == 1;
            bool hasAction = actionHasFootFeatures == 1;
            if (!hasSource && !hasAction)
            {
                m_ValueHasFootFeatures[output] = 0;
                m_ValueLeftFootFeatures[output] = default;
                m_ValueRightFootFeatures[output] = default;
                return true;
            }
            if (!TryResolveFeature(
                    hasSource,
                    m_ValueLeftFootFeatures[source],
                    hasAction,
                    actionLeftFoot,
                    actionLeftFootWeight,
                    hasAction && actionLeftFootWeight > 0f,
                    out AnimationFootFeatureSample left) ||
                !TryResolveFeature(
                    hasSource,
                    m_ValueRightFootFeatures[source],
                    hasAction,
                    actionRightFoot,
                    actionRightFootWeight,
                    hasAction && actionRightFootWeight > 0f,
                    out AnimationFootFeatureSample right))
            {
                return false;
            }
            m_ValueLeftFootFeatures[output] = left;
            m_ValueRightFootFeatures[output] = right;
            m_ValueHasFootFeatures[output] = left.IsValid && right.IsValid ? (byte)1 : (byte)0;
            return true;
        }

        void EvaluateInertialization(AnimationPoseGraphNativeOperation operation, float deltaSeconds)
        {
            int output = operation.OutputValueIndex;
            int input = operation.InputValueIndexA;
            if (!IsInputReady(input, operation.Index) ||
                (uint)operation.InertializationIndex >= (uint)m_Inertializations.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            int stateIndex = operation.InertializationIndex;
            PoseInertializationNativeNode node = m_Inertializations[stateIndex];
            bool stateMachineOwner = node.TemporalOwnerKind ==
                                     PoseInertializationTemporalOwnerKind.StateMachineTransition;
            bool directPlayerOwner = node.TemporalOwnerKind ==
                                     PoseInertializationTemporalOwnerKind.DirectPlayerPolicy;
            if (!stateMachineOwner && !directPlayerOwner ||
                stateMachineOwner && (uint)node.ControlIndex >= (uint)m_StateMachineControls.Length ||
                directPlayerOwner && node.ControlIndex != -1)
            {
                ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                SetInvalid(
                    output,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            CharacterPoseStateMachineNativeControl control = stateMachineOwner
                ? m_StateMachineControls[node.ControlIndex]
                : default;
            PoseDiscontinuityNative discontinuity = m_ValueDiscontinuities[input];
            if (!TryCopyValue(input, output, operation.Index))
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            PoseInertializationNativeState state = CommittedInertialState(stateIndex);
            PrepareInertialNode(stateIndex, in state);
            if (m_ValueAvailability[input] != AnimationPoseAvailability.Pose)
            {
                ClearInertialState(stateIndex, PoseInertializationRuntimeState.Reset);
                return;
            }

            if (discontinuity.IsReset)
            {
                state = default;
                state.LastEventIdentity = stateMachineOwner
                    ? control.Generation
                    : discontinuity.EventIdentity;
                state.RuntimeState = PoseInertializationRuntimeState.Reset;
                state.LastResetReason = discontinuity.ResetReason;
                state.LastResetSequence = discontinuity.ResetSequence;
                state.OutputCompletionIdentity = m_CompletionIdentity;
            }
            else
            {
                ulong eventIdentity = stateMachineOwner
                    ? control.Generation
                    : discontinuity.EventIdentity;
                if (stateMachineOwner && eventIdentity == 0 ||
                    eventIdentity != 0 && eventIdentity < state.LastEventIdentity)
                {
                    ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                    SetInvalid(
                        output,
                        m_ValueContinuityIdentities[input],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        operation.Index);
                    return;
                }
                if (eventIdentity > state.LastEventIdentity)
                {
                    int sourceEndpointIndex;
                    int targetEndpointIndex;
                    bool inertialize;
                    if (stateMachineOwner)
                    {
                        sourceEndpointIndex = control.SourceStateIndex;
                        targetEndpointIndex = control.TargetStateIndex;
                        inertialize = control.BlendMode == CharacterPoseStateMachineBlendMode.Inertialization;
                    }
                    else
                    {
                        if (!discontinuity.IsPresent || discontinuity.HasPreviousEndpoint == 0 ||
                            discontinuity.HasCurrentEndpoint == 0 ||
                            discontinuity.PreviousEndpoint.PresentationPoseSourceIndex < 0 ||
                            discontinuity.CurrentEndpoint.PresentationPoseSourceIndex < 0)
                        {
                            ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                            SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                            return;
                        }
                        sourceEndpointIndex = discontinuity.PreviousEndpoint.PresentationPoseSourceIndex;
                        targetEndpointIndex = discontinuity.CurrentEndpoint.PresentationPoseSourceIndex;
                        inertialize = true;
                        state.LastReason = discontinuity.Reason;
                        state.PreviousEndpoint = discontinuity.PreviousEndpoint;
                        state.CurrentEndpoint = discontinuity.CurrentEndpoint;
                        state.PreviousContinuityIdentity = discontinuity.PreviousContinuityIdentity;
                        state.CurrentContinuityIdentity = discontinuity.CurrentContinuityIdentity;
                    }
                    bool rebase = state.Active != 0;
                    state.LastEventIdentity = eventIdentity;
                    state.Active = 0;
                    state.ElapsedSeconds = 0f;
                    if (!inertialize)
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Anchor;
                    }
                    else
                    {
                        int ruleIndex = RequireInertialRule(
                            stateIndex,
                            sourceEndpointIndex,
                            targetEndpointIndex);
                        if (ruleIndex < 0 ||
                            m_InertialRules[ruleIndex].Mode != PoseInertializationMode.Inertialize)
                        {
                            ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                            SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                            return;
                        }
                        if (state.HasHistory != 0)
                        {
                            CaptureInertialResidual(stateIndex, input, ruleIndex, ref state);
                            state.RuntimeState = rebase
                                ? PoseInertializationRuntimeState.Rebase
                                : PoseInertializationRuntimeState.Capture;
                        }
                        else
                        {
                            state.Active = 0;
                            state.ActiveRuleIndex = ruleIndex;
                            state.RuntimeState = PoseInertializationRuntimeState.Anchor;
                        }
                    }
                }
                else if (state.Active != 0)
                {
                    state.RuntimeState = PoseInertializationRuntimeState.Continue;
                }
            }

            if (state.Active != 0)
            {
                PoseInertializationNativeRule rule = m_InertialRules[state.ActiveRuleIndex];
                bool anyActive = false;
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    int residualIndex = stateIndex * m_BoneCount + bone;
                    float duration = state.ActiveDurationSeconds *
                                     m_InertialDenseProfiles[rule.ProfileOffset + bone];
                    EvaluateInertialEnvelope(rule, state.ElapsedSeconds, duration, out _, out float weight, out float derivative);
                    anyActive |= state.ElapsedSeconds < duration;
                    AnimationLocalBonePose target = m_ValueDenseLocalPoses[PoseOffset(input) + bone];
                    AnimationBlendBoneVelocity targetVelocity = m_ValueDenseVelocities[PoseOffset(input) + bone];
                    Vector3 positionBase = m_InertialPositionResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 rotationBase = m_InertialRotationResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleBase = m_InertialScaleResiduals[residualIndex] +
                                        state.ElapsedSeconds * m_InertialScaleVelocityResiduals[residualIndex];
                    Vector3 linear = targetVelocity.Linear + derivative * positionBase +
                                     weight * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 angular = targetVelocity.Angular + derivative * rotationBase +
                                      weight * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleVelocity = targetVelocity.Scale + derivative * scaleBase +
                                            weight * m_InertialScaleVelocityResiduals[residualIndex];
                    if (!IsFinite(linear) || !IsFinite(angular) || !IsFinite(scaleVelocity))
                    {
                        ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                        SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                        return;
                    }
                    m_ValueDenseLocalPoses[PoseOffset(output) + bone] = new AnimationLocalBonePose(
                        target.Position + weight * positionBase,
                        AnimationPoseMath.QuaternionExp(weight * rotationBase) * target.Rotation,
                        target.Scale + weight * scaleBase);
                    m_ValueDenseVelocities[PoseOffset(output) + bone] =
                        new AnimationBlendBoneVelocity(linear, angular, scaleVelocity);
                }
                ApplyInertialParameters(stateIndex, input, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                ApplyInertialFootFeatures(stateIndex, input, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                state.ElapsedSeconds += deltaSeconds;
                state.LastDeltaSeconds = deltaSeconds;
                if (!anyActive)
                {
                    state.Active = 0;
                    state.RuntimeState = PoseInertializationRuntimeState.Complete;
                }
            }

            CommitInertialHistory(stateIndex, output, ref state);
            if (state.RuntimeState == 0)
                state.RuntimeState = PoseInertializationRuntimeState.Anchor;
            state.OutputCompletionIdentity = m_CompletionIdentity;
            m_InertialStates[stateIndex] = state;
        }

        void EvaluateStatePoseOutput(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            if (!IsInputReady(input, operation.Index) ||
                !TryCopyValue(input, operation.OutputValueIndex, operation.Index))
            {
                SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
            }
        }

        void EvaluatePoseStateMachine(AnimationPoseGraphNativeOperation operation)
        {
            if ((uint)operation.StateMachineIndex >= (uint)m_StateMachineControls.Length)
            {
                SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            CharacterPoseStateMachineNativeControl control =
                m_StateMachineControls[operation.StateMachineIndex];
            if (control.Generation == 0 ||
                control.SourcePoseValueIndex < 0 ||
                control.SourcePoseValueIndex >= operation.OutputValueIndex ||
                control.TargetPoseValueIndex < 0 ||
                control.TargetPoseValueIndex >= operation.OutputValueIndex ||
                control.PredictionPoseValueIndex < -1 ||
                control.PredictionPoseValueIndex >= operation.OutputValueIndex)
            {
                SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (control.BlendMode == CharacterPoseStateMachineBlendMode.Single ||
                control.BlendMode == CharacterPoseStateMachineBlendMode.Inertialization)
            {
                if (!IsInputReady(control.TargetPoseValueIndex, operation.Index) ||
                    !TryCopyValue(
                        control.TargetPoseValueIndex,
                        operation.OutputValueIndex,
                        operation.Index) ||
                    !TryApplyStateMachinePrediction(
                        operation.OutputValueIndex,
                        control.PredictionPoseValueIndex,
                        operation.Index))
                {
                    SetInvalid(
                        operation.OutputValueIndex,
                        (ulong)operation.Index + 1UL,
                        AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                        operation.Index);
                }
                return;
            }
            if (control.BlendMode != CharacterPoseStateMachineBlendMode.Standard)
            {
                SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            EvaluateStateMachineStandardBlend(operation, control);
        }

        void EvaluateStateMachineStandardBlend(
            AnimationPoseGraphNativeOperation operation,
            CharacterPoseStateMachineNativeControl control)
        {
            int output = operation.OutputValueIndex;
            int source = control.SourcePoseValueIndex;
            int target = control.TargetPoseValueIndex;
            if (!TryRequireInputs(operation, source, target) ||
                (uint)control.CurveIndex >= (uint)m_BlendCurves.Length ||
                (control.DurationSeconds > 0f &&
                 (uint)control.BlendProfileIndex >= (uint)m_BlendProfiles.Length))
            {
                SetInvalid(
                    output,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (control.DurationSeconds <= 0f)
            {
                if (!TryCopyValue(target, output, operation.Index))
                {
                    SetInvalid(
                        output,
                        m_ValueContinuityIdentities[target],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        operation.Index);
                }
                return;
            }
            if (m_ValueAvailability[source] != AnimationPoseAvailability.Pose ||
                m_ValueAvailability[target] != AnimationPoseAvailability.Pose)
            {
                SetInvalid(
                    output,
                    CombineContinuity(
                        m_ValueContinuityIdentities[source],
                        m_ValueContinuityIdentities[target],
                        operation.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }

            AnimationBlendProfileNativeEntry profile =
                m_BlendProfiles[control.BlendProfileIndex];
            float globalDuration = control.DurationSeconds *
                                   profile.GlobalDurationMultiplier;
            float globalWeight = EvaluateStandardBlendCurve(
                control.CurveIndex,
                control.ElapsedSeconds,
                globalDuration);
            float leftWeight = EvaluateStandardBlendBoneWeight(
                control,
                profile,
                m_LeftFootBoneIndex);
            float rightWeight = EvaluateStandardBlendBoneWeight(
                control,
                profile,
                m_RightFootBoneIndex);

            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_ValueOutputWeights[output] = UnionWeight(
                m_ValueOutputWeights[source],
                m_ValueOutputWeights[target] * globalWeight);
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[source],
                m_ValueContinuityIdentities[target],
                operation.Index);
            m_ValueDiscontinuities[output] = m_ValueDiscontinuities[target];
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float weight = EvaluateStandardBlendBoneWeight(
                    control,
                    profile,
                    bone);
                if (!TryBlendPose(
                        m_ValueDenseLocalPoses[PoseOffset(source) + bone],
                        m_ValueDenseLocalPoses[PoseOffset(target) + bone],
                        weight,
                        out AnimationLocalBonePose pose))
                {
                    SetInvalid(
                        output,
                        m_ValueContinuityIdentities[output],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = pose;
                AnimationBlendBoneVelocity sourceVelocity =
                    m_ValueDenseVelocities[PoseOffset(source) + bone];
                AnimationBlendBoneVelocity targetVelocity =
                    m_ValueDenseVelocities[PoseOffset(target) + bone];
                m_ValueDenseVelocities[PoseOffset(output) + bone] =
                    new AnimationBlendBoneVelocity(
                        Vector3.LerpUnclamped(sourceVelocity.Linear, targetVelocity.Linear, weight),
                        Vector3.LerpUnclamped(sourceVelocity.Angular, targetVelocity.Angular, weight),
                        Vector3.LerpUnclamped(sourceVelocity.Scale, targetVelocity.Scale, weight));
            }
            if (!TryBlendStateMachineParameters(source, target, output, globalWeight) ||
                !TryMergeStateMachineContributions(
                    source,
                    target,
                    output,
                    control,
                    profile,
                    globalWeight,
                    leftWeight,
                    rightWeight) ||
                !TryBlendStateMachineFootFeatures(
                    source,
                    target,
                    output,
                    leftWeight,
                    rightWeight) ||
                !TryApplyStateMachinePrediction(
                    output,
                    control.PredictionPoseValueIndex,
                    operation.Index))
            {
                SetInvalid(
                    output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
            }
        }

        float EvaluateStandardBlendBoneWeight(
            CharacterPoseStateMachineNativeControl control,
            AnimationBlendProfileNativeEntry profile,
            int bone)
        {
            float duration = control.DurationSeconds *
                             profile.GlobalDurationMultiplier *
                             m_BlendDenseProfiles[profile.DenseOffset + bone];
            return EvaluateStandardBlendCurve(
                control.CurveIndex,
                control.ElapsedSeconds,
                duration);
        }

        float EvaluateStandardBlendCurve(
            int curveIndex,
            float elapsedSeconds,
            float durationSeconds)
        {
            if (durationSeconds <= 0f || elapsedSeconds >= durationSeconds)
                return 1f;
            float time = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            AnimationBlendCurveNativeEntry curve = m_BlendCurves[curveIndex];
            AnimationBlendCurveSegment segment =
                m_BlendCurveSegments[curve.SegmentOffset + curve.SegmentCount - 1];
            for (int i = 0; i < curve.SegmentCount; i++)
            {
                AnimationBlendCurveSegment candidate =
                    m_BlendCurveSegments[curve.SegmentOffset + i];
                if (time <= candidate.EndTime)
                {
                    segment = candidate;
                    break;
                }
            }
            float u = (time - segment.StartTime) /
                      (segment.EndTime - segment.StartTime);
            return Mathf.Clamp01(
                ((segment.A * u + segment.B) * u + segment.C) * u + segment.D);
        }

        void PrepareInertialNode(
            int stateIndex,
            in PoseInertializationNativeState state)
        {
            m_InertialStates[stateIndex] = state;
            if (state.Active == 0)
                return;
            int residualOffset = stateIndex * m_BoneCount;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                int index = residualOffset + bone;
                m_InertialPositionResiduals[index] = m_CommittedInertialPositionResiduals[index];
                m_InertialRotationResiduals[index] = m_CommittedInertialRotationResiduals[index];
                m_InertialScaleResiduals[index] = m_CommittedInertialScaleResiduals[index];
                m_InertialLinearVelocityResiduals[index] = m_CommittedInertialLinearVelocityResiduals[index];
                m_InertialAngularVelocityResiduals[index] = m_CommittedInertialAngularVelocityResiduals[index];
                m_InertialScaleVelocityResiduals[index] = m_CommittedInertialScaleVelocityResiduals[index];
            }
            int parameterOffset = stateIndex * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                int index = parameterOffset + parameter;
                m_InertialParameterResiduals[index] = m_CommittedInertialParameterResiduals[index];
            }
            m_InertialAccumulatorLeftFeet[stateIndex] = m_CommittedInertialAccumulatorLeftFeet[stateIndex];
            m_InertialAccumulatorRightFeet[stateIndex] = m_CommittedInertialAccumulatorRightFeet[stateIndex];
            m_InertialAccumulatorHasFeet[stateIndex] = m_CommittedInertialAccumulatorHasFeet[stateIndex];
        }

        PoseInertializationNativeState CommittedInertialState(int stateIndex) =>
            m_InertialResetRequests[stateIndex] != 0
                ? default
                : m_CommittedInertialStates[stateIndex];

        void CaptureInertialResidual(
            int stateIndex,
            int input,
            int ruleIndex,
            ref PoseInertializationNativeState state)
        {
            int historyPoseOffset = (stateIndex * 2 + state.HistoryPage) * m_BoneCount;
            int residualOffset = stateIndex * m_BoneCount;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                AnimationLocalBonePose previous = m_CommittedInertialHistory[historyPoseOffset + bone];
                AnimationBlendBoneVelocity previousVelocity = m_CommittedInertialHistoryVelocities[historyPoseOffset + bone];
                AnimationLocalBonePose target = m_ValueDenseLocalPoses[PoseOffset(input) + bone];
                AnimationBlendBoneVelocity targetVelocity = m_ValueDenseVelocities[PoseOffset(input) + bone];
                m_InertialPositionResiduals[residualOffset + bone] = previous.Position - target.Position;
                m_InertialRotationResiduals[residualOffset + bone] =
                    AnimationPoseMath.QuaternionLog(previous.Rotation * Quaternion.Inverse(target.Rotation));
                m_InertialScaleResiduals[residualOffset + bone] = previous.Scale - target.Scale;
                m_InertialLinearVelocityResiduals[residualOffset + bone] = previousVelocity.Linear - targetVelocity.Linear;
                m_InertialAngularVelocityResiduals[residualOffset + bone] = previousVelocity.Angular - targetVelocity.Angular;
                m_InertialScaleVelocityResiduals[residualOffset + bone] = previousVelocity.Scale - targetVelocity.Scale;
            }
            PoseInertializationNativeRule rule = m_InertialRules[ruleIndex];
            int historyParameterOffset = (stateIndex * 2 + state.HistoryPage) * m_ParameterCount;
            int residualParameterOffset = stateIndex * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_InertialParameterResiduals[residualParameterOffset + parameter] =
                    m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_CommittedInertialHistoryParameterAvailability[historyParameterOffset + parameter] != 0 &&
                    m_ValuePoseParameterAvailability[ParameterOffset(input) + parameter] != 0
                        ? m_CommittedInertialHistoryParameters[historyParameterOffset + parameter] -
                          m_ValuePoseParameters[ParameterOffset(input) + parameter]
                        : 0f;
            }
            int historyFootIndex = stateIndex * 2 + state.HistoryPage;
            m_InertialAccumulatorLeftFeet[stateIndex] = m_CommittedInertialHistoryLeftFeet[historyFootIndex];
            m_InertialAccumulatorRightFeet[stateIndex] = m_CommittedInertialHistoryRightFeet[historyFootIndex];
            m_InertialAccumulatorHasFeet[stateIndex] = m_CommittedInertialHistoryHasFeet[historyFootIndex];
            state.ActiveRuleIndex = ruleIndex;
            state.ActiveDurationSeconds = rule.DurationSeconds;
            state.ElapsedSeconds = 0f;
            state.AccumulatorGeneration++;
            state.Active = 1;
        }

        void ApplyInertialParameters(
            int stateIndex,
            int input,
            int output,
            PoseInertializationNativeRule rule,
            float durationSeconds,
            float elapsedSeconds)
        {
            EvaluateInertialEnvelope(rule, elapsedSeconds, durationSeconds, out _, out float weight, out _);
            int residualOffset = stateIndex * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                if (m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_ValuePoseParameterAvailability[ParameterOffset(input) + parameter] != 0)
                {
                    m_ValuePoseParameters[ParameterOffset(output) + parameter] =
                        m_ValuePoseParameters[ParameterOffset(input) + parameter] +
                        weight * m_InertialParameterResiduals[residualOffset + parameter];
                }
            }
        }

        void ApplyInertialFootFeatures(
            int stateIndex,
            int input,
            int output,
            PoseInertializationNativeRule rule,
            float durationSeconds,
            float elapsedSeconds)
        {
            if (m_InertialAccumulatorHasFeet[stateIndex] == 0 || m_ValueHasFootFeatures[input] == 0)
                return;
            float leftDuration = durationSeconds *
                                 m_InertialDenseProfiles[rule.ProfileOffset + m_LeftFootBoneIndex];
            float rightDuration = durationSeconds *
                                  m_InertialDenseProfiles[rule.ProfileOffset + m_RightFootBoneIndex];
            EvaluateInertialEnvelope(rule, elapsedSeconds, leftDuration, out float leftEnvelope, out _, out _);
            EvaluateInertialEnvelope(rule, elapsedSeconds, rightDuration, out float rightEnvelope, out _, out _);
            if (TryResolveFeature(
                    true,
                    m_InertialAccumulatorLeftFeet[stateIndex],
                    true,
                    m_ValueLeftFootFeatures[input],
                    leftEnvelope,
                    true,
                    out AnimationFootFeatureSample left) &&
                TryResolveFeature(
                    true,
                    m_InertialAccumulatorRightFeet[stateIndex],
                    true,
                    m_ValueRightFootFeatures[input],
                    rightEnvelope,
                    true,
                    out AnimationFootFeatureSample right))
            {
                m_ValueLeftFootFeatures[output] = left;
                m_ValueRightFootFeatures[output] = right;
                m_ValueHasFootFeatures[output] = 1;
                ScaleContributionFootWeights(output, leftEnvelope, rightEnvelope);
            }
        }

        void ScaleContributionFootWeights(int value, float leftEnvelope, float rightEnvelope)
        {
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                int index = ContributionOffset(value) + contribution;
                AnimationPrimitivePoseContribution source = m_ValueContributions[index];
                m_ValueContributions[index] = new AnimationPrimitivePoseContribution(
                    source.PhysicalPlayerIndex,
                    source.PhysicalSourceIndex,
                    source.PhysicalSourceGeneration,
                    source.Kind,
                    source.SourceOwnerIndex,
                    source.ContributionContinuityIdentity,
                    source.Weight,
                    source.LeftFootWeight * leftEnvelope,
                    source.RightFootWeight * rightEnvelope);
            }
        }

        void CommitInertialHistory(int stateIndex, int output, ref PoseInertializationNativeState state)
        {
            int page = state.HasHistory == 0 ? 0 : 1 - state.HistoryPage;
            int poseOffset = (stateIndex * 2 + page) * m_BoneCount;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                m_InertialHistory[poseOffset + bone] = m_ValueDenseLocalPoses[PoseOffset(output) + bone];
                m_InertialHistoryVelocities[poseOffset + bone] = m_ValueDenseVelocities[PoseOffset(output) + bone];
            }
            int parameterOffset = (stateIndex * 2 + page) * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_InertialHistoryParameters[parameterOffset + parameter] = m_ValuePoseParameters[ParameterOffset(output) + parameter];
                m_InertialHistoryParameterAvailability[parameterOffset + parameter] =
                    m_ValuePoseParameterAvailability[ParameterOffset(output) + parameter];
            }
            int footIndex = stateIndex * 2 + page;
            m_InertialHistoryLeftFeet[footIndex] = m_ValueLeftFootFeatures[output];
            m_InertialHistoryRightFeet[footIndex] = m_ValueRightFootFeatures[output];
            m_InertialHistoryHasFeet[footIndex] = m_ValueHasFootFeatures[output];
            state.HistoryPage = page;
            state.HasHistory = 1;
            state.HistoryCompletionIdentity = m_CompletionIdentity;
        }

        void ClearInertialState(int stateIndex, PoseInertializationRuntimeState runtimeState)
        {
            if ((uint)stateIndex < (uint)m_InertialStates.Length)
            {
                m_InertialStates[stateIndex] = new PoseInertializationNativeState
                {
                    RuntimeState = runtimeState,
                    OutputCompletionIdentity = m_CompletionIdentity
                };
            }
        }

        int RequireInertialRule(int stateIndex, int sourceProducerIndex, int targetProducerIndex)
        {
            PoseInertializationNativeNode node = m_Inertializations[stateIndex];
            int match = -1;
            for (int i = 0; i < node.RuleCount; i++)
            {
                int index = node.RuleOffset + i;
                PoseInertializationNativeRule rule = m_InertialRules[index];
                if (rule.SourceEndpointIndex != sourceProducerIndex || rule.TargetEndpointIndex != targetProducerIndex)
                    continue;
                if (match >= 0)
                    return -1;
                match = index;
            }
            return match;
        }

        void EvaluateInertialEnvelope(
            PoseInertializationNativeRule rule,
            float elapsedSeconds,
            float durationSeconds,
            out float envelope,
            out float residualWeight,
            out float residualDerivativePerSecond)
        {
            if (durationSeconds <= 0f || elapsedSeconds >= durationSeconds)
            {
                envelope = 1f;
                residualWeight = 0f;
                residualDerivativePerSecond = 0f;
                return;
            }
            float normalized = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            EvaluateInertialCurve(rule, normalized, out float curve, out float derivative);
            EvaluateInertialCurve(rule, 0f, out _, out float startDerivative);
            EvaluateInertialCurve(rule, 1f, out _, out float endDerivative);
            float s2 = normalized * normalized;
            float s3 = s2 * normalized;
            float h10 = s3 - 2f * s2 + normalized;
            float h11 = s3 - s2;
            float h10Derivative = 3f * s2 - 4f * normalized + 1f;
            float h11Derivative = 3f * s2 - 2f * normalized;
            envelope = Mathf.Clamp01(curve - startDerivative * h10 - endDerivative * h11);
            float envelopeDerivative = derivative - startDerivative * h10Derivative - endDerivative * h11Derivative;
            residualWeight = 1f - envelope;
            residualDerivativePerSecond = -envelopeDerivative / durationSeconds;
        }

        void EvaluateInertialCurve(
            PoseInertializationNativeRule rule,
            float normalizedTime,
            out float value,
            out float derivative)
        {
            float time = Mathf.Clamp01(normalizedTime);
            AnimationBlendCurveSegment segment = m_InertialCurveSegments[rule.CurveOffset + rule.CurveCount - 1];
            for (int i = 0; i < rule.CurveCount; i++)
            {
                AnimationBlendCurveSegment candidate = m_InertialCurveSegments[rule.CurveOffset + i];
                if (time <= candidate.EndTime)
                {
                    segment = candidate;
                    break;
                }
            }
            float u = (time - segment.StartTime) / (segment.EndTime - segment.StartTime);
            value = Mathf.Clamp01(((segment.A * u + segment.B) * u + segment.C) * u + segment.D);
            derivative = ((3f * segment.A * u + 2f * segment.B) * u + segment.C) /
                         (segment.EndTime - segment.StartTime);
        }

        void EvaluateBlendPose(AnimationPoseGraphNativeOperation operation)
        {
            float weight = operation.Weight;
            if (operation.ParameterIndex >= 0)
            {
                int input = operation.InputValueIndexA;
                if (!IsInputReady(input, operation.Index) || operation.ParameterIndex >= m_ParameterCount)
                {
                    SetInvalid(operation.OutputValueIndex, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                    return;
                }
                if (m_ValuePoseParameterAvailability[ParameterOffset(input) + operation.ParameterIndex] == 0)
                {
                    SetInvalid(operation.OutputValueIndex, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.SlotParameterInvalid, operation.Index);
                    return;
                }
                weight = Mathf.Clamp01(m_ValuePoseParameters[ParameterOffset(input) + operation.ParameterIndex]);
            }
            EvaluateLayeredBoneBlend(operation.WithWeight(weight));
        }

        void EvaluateModifyBone(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!IsInputReady(input, operation.Index) ||
                (uint)operation.ModifyBoneIndex >= (uint)m_ModifyBones.Length ||
                !TryCopyValue(input, output, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            if (m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            AnimationPoseGraphNativeModifyBone modify = m_ModifyBones[operation.ModifyBoneIndex];
            int inputOffset = PoseOffset(input);
            int outputOffset = PoseOffset(output);
            AnimationLocalBonePose current = m_ValueDenseLocalPoses[outputOffset + modify.BoneIndex];
            CharacterComponentBonePose parentComponent = default;
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Local && modify.ParentBoneIndex >= 0)
            {
                parentComponent = AsComponent(m_ValueDenseLocalPoses[outputOffset + modify.ParentBoneIndex]);
                if (!CharacterPoseConstraintMath.TryCreateLocal(AsComponent(current), parentComponent, out current))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
            }
            Vector3 position = (modify.Operations & ModifyBoneOperationMask.Position) != 0
                ? current.Position + modify.Position * operation.Weight
                : current.Position;
            Quaternion rotation = (modify.Operations & ModifyBoneOperationMask.Rotation) != 0
                ? Quaternion.SlerpUnclamped(Quaternion.identity, modify.Rotation, operation.Weight) * current.Rotation
                : current.Rotation;
            Vector3 scale = (modify.Operations & ModifyBoneOperationMask.Scale) != 0
                ? Vector3.Scale(current.Scale, Vector3.LerpUnclamped(Vector3.one, modify.Scale, operation.Weight))
                : current.Scale;
            var modified = new AnimationLocalBonePose(position, rotation, scale);
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Local && modify.ParentBoneIndex >= 0)
            {
                if (!CharacterPoseConstraintMath.TryCreateComponent(
                        modified,
                        parentComponent,
                        out CharacterComponentBonePose component))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                modified = ToPose(component);
            }
            m_ValueDenseLocalPoses[outputOffset + modify.BoneIndex] = modified;
            if (!RebuildComponentDescendants(inputOffset, outputOffset, modify.BoneIndex))
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
        }

        void EvaluateRootOrientationWarp(
            AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!IsInputReady(input, operation.Index) ||
                (uint)operation.RootOrientationWarpIndex >=
                (uint)m_RootOrientationWarps.Length ||
                (uint)operation.RootOrientationWarpIndex >=
                (uint)m_RootOrientationWarpControls.Length ||
                !TryCopyValue(input, output, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }
            if (m_ValueAvailability[output] !=
                AnimationPoseAvailability.Pose)
                return;
            CharacterRootOrientationWarpNativeControl control =
                m_RootOrientationWarpControls[
                    operation.RootOrientationWarpIndex];
            if (!control.IsValid)
            {
                SetInvalid(output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (control.Active == 0 ||
                Math.Abs(control.YawOffsetDegrees) <= 0.0001f)
                return;
            int root = m_RootOrientationWarps[
                operation.RootOrientationWarpIndex]
                .RootPhysicalBoneIndex;
            if ((uint)root >= (uint)m_BoneCount)
            {
                SetInvalid(output,
                    m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            int offset = PoseOffset(output) + root;
            AnimationLocalBonePose current =
                m_ValueDenseLocalPoses[offset];
            m_ValueDenseLocalPoses[offset] =
                new AnimationLocalBonePose(
                    current.Position,
                    Quaternion.AngleAxis(
                        control.YawOffsetDegrees,
                        Vector3.up) * current.Rotation,
                    current.Scale);
        }

        bool EvaluatePoseBoneIkGoals(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputFullBodyIkGoalContributionValueIndex;
            if (!IsInputReady(input, operation.Index) ||
                (uint)operation.PoseBoneIkGoalsIndex >= (uint)m_PoseBoneIkGoalRanges.Length ||
                (uint)output >=
                (uint)m_PoseConstraints.FullBodyIkGoalContributionCount ||
                m_ValueAvailability[input] != AnimationPoseAvailability.Pose)
            {
                return false;
            }
            AnimationPoseGraphNativePoseBoneIkGoalRange range =
                m_PoseBoneIkGoalRanges[operation.PoseBoneIkGoalsIndex];
            if (range.DescriptorOffset > m_PoseBoneIkGoalDescriptors.Length - range.DescriptorCount ||
                range.ContributionGoalWorkspaceOffset >
                m_PoseConstraints.FullBodyIkContributionGoalCount -
                range.DescriptorCount)
            {
                return false;
            }
            NativeSlice<AnimationLocalBonePose> componentPose = new NativeSlice<AnimationLocalBonePose>(
                m_ValueDenseLocalPoses,
                PoseOffset(input),
                m_BoneCount);
            NativeSlice<CharacterPoseBoneIkGoalDescriptor> descriptors =
                new NativeSlice<CharacterPoseBoneIkGoalDescriptor>(
                    m_PoseBoneIkGoalDescriptors,
                    range.DescriptorOffset,
                    range.DescriptorCount);
            CharacterFullBodyIkGoalContributionHeader contribution =
                m_PoseConstraints.ProducePoseBoneIkGoals(
                    output,
                    range.ContributionGoalWorkspaceOffset,
                    componentPose,
                    descriptors,
                    operation.Index,
                    operation.FrameCacheIndex,
                    m_FrameSequence,
                    m_CompletionIdentity);
            return contribution.IsValid;
        }

        bool RebuildComponentDescendants(int inputOffset, int outputOffset, int rootIndex)
        {
            for (int bone = rootIndex + 1; bone < m_BoneCount; bone++)
            {
                if (!IsDescendant(bone, rootIndex))
                    continue;
                int parent = m_ParentIndices[bone];
                CharacterComponentBonePose inputBone = AsComponent(m_ValueDenseLocalPoses[inputOffset + bone]);
                CharacterComponentBonePose inputParent = AsComponent(m_ValueDenseLocalPoses[inputOffset + parent]);
                CharacterComponentBonePose outputParent = AsComponent(m_ValueDenseLocalPoses[outputOffset + parent]);
                if (!CharacterPoseConstraintMath.TryCreateLocal(inputBone, inputParent, out AnimationLocalBonePose local) ||
                    !CharacterPoseConstraintMath.TryCreateComponent(local, outputParent, out CharacterComponentBonePose rebuilt))
                    return false;
                m_ValueDenseLocalPoses[outputOffset + bone] = ToPose(rebuilt);
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
                cursor = m_ParentIndices[cursor];
            }
            return false;
        }

        static CharacterComponentBonePose AsComponent(AnimationLocalBonePose value) =>
            new CharacterComponentBonePose(value.Position, value.Rotation, value.Scale);

        static AnimationLocalBonePose ToPose(CharacterComponentBonePose value) =>
            new AnimationLocalBonePose(value.Position, value.Rotation, value.Scale);

        bool EvaluateWorldAwareFootGoal(
            AnimationPoseGraphNativeOperation operation,
            in CharacterPoseWorldAwareStageInput worldInput)
        {
            int output = operation.OutputFullBodyIkGoalContributionValueIndex;
            bool descriptorValid = operation.Code == CharacterPoseOperationCode.FootPlacement &&
                                   (uint)operation.FootPlacementIndex < (uint)m_FootPlacementCount;
            if (!descriptorValid ||
                !worldInput.HasFootPlacement ||
                worldInput.OperationIndex != operation.Index ||
                (uint)output >=
                (uint)m_PoseConstraints.FullBodyIkGoalContributionCount)
            {
                return false;
            }
            CharacterFullBodyIkGoalContributionHeader header;
            if (worldInput.WorldContextAvailable)
            {
                CharacterFootPlacementFrameInput footPlacement =
                    worldInput.FootPlacement;
                header = m_PoseConstraints.PrepareFootPlacement(
                    in footPlacement,
                    output,
                    worldInput.ContributionGoalOffset,
                    operation.Index,
                    operation.FrameCacheIndex);
            }
            else
            {
                header = new CharacterFullBodyIkGoalContributionHeader(
                    m_FrameSequence,
                    m_CompletionIdentity,
                    m_RigId,
                    m_RigRevision,
                    operation.Index,
                    operation.FrameCacheIndex,
                    worldInput.ContributionGoalOffset,
                    0,
                    CharacterFullBodyIkGoalContributionAvailability
                        .WorldContextUnavailable);
                m_PoseConstraints.RecordUnavailableGoalContribution(
                    output,
                    in header);
            }
            return header.IsValid &&
                   header.FrameSequence == m_FrameSequence &&
                   header.CompletionIdentity == m_CompletionIdentity &&
                   header.ProducerOperationIndex == operation.Index &&
                   header.ProducerCallSiteIndex == operation.FrameCacheIndex &&
                   header.RigId.Equals(m_RigId) &&
                   header.RigRevision.Equals(m_RigRevision) &&
                   header.GoalOffset <=
                       m_PoseConstraints.FullBodyIkContributionGoalCount -
                       header.GoalCount &&
                   header.Availability ==
                       CharacterFullBodyIkGoalContributionAvailability.Ready;
        }

        bool EvaluateGoalAssembler(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputFullBodyIkGoalSetValueIndex;
            if ((uint)output >= (uint)m_FullBodyIkGoalSetValueCount ||
                operation.FullBodyIkGoalContributionInputStart < -1 ||
                operation.FullBodyIkGoalContributionInputCount < 0 ||
                operation.FullBodyIkGoalContributionInputCount > 0 &&
                operation.FullBodyIkGoalContributionInputStart >
                m_FullBodyIkGoalContributionInputValueIndices.Length -
                operation.FullBodyIkGoalContributionInputCount)
            {
                return false;
            }
            var contributionInputs = new NativeSlice<int>(
                m_FullBodyIkGoalContributionInputValueIndices,
                operation.FullBodyIkGoalContributionInputCount == 0
                    ? 0
                    : operation.FullBodyIkGoalContributionInputStart,
                operation.FullBodyIkGoalContributionInputCount);
            CharacterFullBodyIkResult result =
                m_PoseConstraints.AssembleFullBodyIkGoals(
                    contributionInputs,
                    operation.Index,
                    operation.FrameCacheIndex,
                    m_FrameSequence,
                    m_CompletionIdentity,
                    out CharacterFullBodyIkGoalSetHeader goalSet);
            if (!result.Succeeded)
                return false;
            return IsGoalSetReady(goalSet) &&
                   goalSet.ProducerOperationIndex == operation.Index &&
                   goalSet.ProducerCallSiteIndex == operation.FrameCacheIndex;
        }

        void EvaluateFullBodyIk(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            int goalSetIndex = operation.InputFullBodyIkGoalSetValueIndex;
            if (!IsInputReady(input, operation.Index) ||
                operation.FullBodyIkIndex != 0 ||
                (uint)goalSetIndex >= (uint)m_FullBodyIkGoalSetValueCount ||
                !m_PoseConstraints.HasPendingAssembledGoalSet ||
                !TryCopyValue(input, output, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }
            if (m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            NativeSlice<AnimationLocalBonePose> outputPose = new NativeSlice<AnimationLocalBonePose>(
                m_ValueDenseLocalPoses,
                PoseOffset(output),
                m_BoneCount);
            CharacterFullBodyIkResult result = m_PoseConstraints.SolveFullBodyIk(
                outputPose,
                operation.Index,
                operation.FrameCacheIndex,
                m_FrameSequence,
                m_CompletionIdentity);
            if (!result.Succeeded)
            {
                SetInvalid(output, m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.FullBodyIkSolverInvalid,
                    operation.Index);
            }
        }

        bool IsGoalSetReady(CharacterFullBodyIkGoalSetHeader header) =>
            header.IsValid &&
            header.FrameSequence == m_FrameSequence &&
            header.CompletionIdentity == m_CompletionIdentity &&
            header.RigId.Equals(m_RigId) &&
            header.RigRevision.Equals(m_RigRevision) &&
            header.Availability == CharacterFullBodyIkGoalSetAvailability.Ready;

        bool EvaluateLinkedPoseCall(AnimationPoseGraphNativeOperation operation)
        {
            if ((uint)operation.LinkedPoseCallIndex >= (uint)m_LinkedPoseCalls.Length)
                return false;
            AnimationPoseGraphNativeLinkedPoseCall call =
                m_LinkedPoseCalls[operation.LinkedPoseCallIndex];
            AnimationPoseGraphNativeLinkedPoseCallControl control =
                m_LinkedPoseCallControls[operation.LinkedPoseCallIndex];
            if (!control.IsActive || control.CandidateIndex < call.CandidateStart ||
                control.CandidateIndex >= call.CandidateStart + call.CandidateCount ||
                (uint)control.CandidateIndex >= (uint)m_LinkedPoseCandidates.Length)
            {
                return false;
            }
            AnimationPoseGraphNativeLinkedPoseCandidate candidate =
                m_LinkedPoseCandidates[control.CandidateIndex];
            if (!IsLinkedPoseFragmentActive(candidate.FragmentIndex))
                return false;

            if (operation.OutputValueIndex >= 0)
            {
                if (candidate.OutputPoseValueIndex < 0 ||
                    !IsInputReady(candidate.OutputPoseValueIndex, operation.Index) ||
                    !TryCopyValue(
                        candidate.OutputPoseValueIndex,
                        operation.OutputValueIndex,
                        operation.Index))
                {
                    SetInvalid(
                        operation.OutputValueIndex,
                        control.Generation,
                        AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                        operation.Index);
                    return false;
                }
                m_ValueContinuityIdentities[operation.OutputValueIndex] = CombineContinuity(
                    m_ValueContinuityIdentities[operation.OutputValueIndex],
                    control.Generation,
                    operation.Index);
                if (control.PoseDiscontinuity != 0)
                {
                    ulong continuity = m_ValueContinuityIdentities[operation.OutputValueIndex];
                    PoseDiscontinuity discontinuity = PoseDiscontinuity.Reset(
                        CombineContinuity(control.Generation, continuity, operation.Index),
                        m_CompletionIdentity,
                        default,
                        continuity,
                        PoseDiscontinuityResetReason.BranchReplacement,
                        control.Generation,
                        false);
                    m_ValueDiscontinuities[operation.OutputValueIndex] =
                        PoseDiscontinuityNative.From(in discontinuity);
                }
            }

            return operation.OutputValueIndex >= 0;
        }

        bool IsLinkedPoseFragmentActive(int fragmentIndex) =>
            (uint)fragmentIndex < (uint)m_LinkedPoseActiveFragments.Length &&
            m_LinkedPoseActiveFragments[fragmentIndex] == 1;

        AnimationPoseNativeInvalidReason ValueOperationInvalidReason(
            AnimationPoseGraphNativeOperation operation)
        {
            if (operation.Code == CharacterPoseOperationCode.FootPlacement &&
                (uint)operation.OutputFullBodyIkGoalContributionValueIndex <
                (uint)m_PoseConstraints.FullBodyIkGoalContributionCount &&
                m_PoseConstraints.GetPendingGoalContribution(
                        operation.OutputFullBodyIkGoalContributionValueIndex)
                    .Availability ==
                CharacterFullBodyIkGoalContributionAvailability.WorldContextUnavailable)
            {
                return AnimationPoseNativeInvalidReason.WorldContextUnavailable;
            }
            return operation.Code == CharacterPoseOperationCode.FootPlacement
                ? AnimationPoseNativeInvalidReason.FootPlacementInvalid
                : operation.Code == CharacterPoseOperationCode.PoseBoneIKGoals ||
                  operation.Code == CharacterPoseOperationCode.FullBodyIkGoalAssembler
                    ? AnimationPoseNativeInvalidReason.FullBodyIkGoalSetInvalid
                    : AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
        }

        void EvaluateLocalToComponentPose(
            AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!IsInputReady(input, operation.Index) ||
                !TryCopyValueWithoutPose(
                    input,
                    output,
                    operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }
            if (m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                int offset = PoseOffset(output) + bone;
                int parent = m_ParentIndices[bone];
                AnimationLocalBonePose local =
                    m_ValueDenseLocalPoses[PoseOffset(input) + bone];
                if (parent >= 0 &&
                    !TryToModel(
                        m_ValueDenseLocalPoses[PoseOffset(output) + parent],
                        local,
                        out local))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output],
                        AnimationPoseNativeInvalidReason.PoseSpaceConversionInvalid,
                        operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[offset] = local;
            }
        }

        void EvaluateComponentToLocalPose(
            AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!IsInputReady(input, operation.Index) ||
                !TryCopyValueWithoutPose(
                    input,
                    output,
                    operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }
            if (m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                int parent = m_ParentIndices[bone];
                AnimationLocalBonePose component =
                    m_ValueDenseLocalPoses[PoseOffset(input) + bone];
                if (parent >= 0 &&
                    !TryToLocal(
                        m_ValueDenseLocalPoses[PoseOffset(input) + parent],
                        component,
                        out component))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output],
                        AnimationPoseNativeInvalidReason.PoseSpaceConversionInvalid,
                        operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = component;
            }
        }

        bool TryBlendStateMachineParameters(
            int source,
            int target,
            int output,
            float targetWeight)
        {
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                int sourceOffset = ParameterOffset(source) + parameter;
                int targetOffset = ParameterOffset(target) + parameter;
                int outputOffset = ParameterOffset(output) + parameter;
                float sourceValue = m_ValuePoseParameters[sourceOffset];
                float targetValue = m_ValuePoseParameters[targetOffset];
                if (!float.IsFinite(sourceValue) || !float.IsFinite(targetValue))
                    return false;
                bool sourceAvailable = m_ValuePoseParameterAvailability[sourceOffset] != 0;
                bool targetAvailable = m_ValuePoseParameterAvailability[targetOffset] != 0;
                bool available = sourceAvailable && targetWeight < 1f ||
                                 targetAvailable && targetWeight > 0f;
                float value = sourceAvailable && targetAvailable
                    ? Mathf.LerpUnclamped(sourceValue, targetValue, targetWeight)
                    : targetAvailable && targetWeight > 0f
                        ? targetValue
                        : sourceAvailable && targetWeight < 1f
                            ? sourceValue
                            : m_ParameterDefaults[parameter];
                if (!float.IsFinite(value))
                    return false;
                m_ValuePoseParameters[outputOffset] = value;
                m_ValuePoseParameterAvailability[outputOffset] =
                    available ? (byte)1 : (byte)0;
            }
            return true;
        }

        bool TryMergeStateMachineContributions(
            int source,
            int target,
            int output,
            CharacterPoseStateMachineNativeControl control,
            AnimationBlendProfileNativeEntry profile,
            float globalWeight,
            float leftWeight,
            float rightWeight)
        {
            m_ValueContributionCounts[output] = 0;
            for (int contribution = 0;
                 contribution < m_ValueContributionCounts[source];
                 contribution++)
            {
                if (!TryAddStateMachineContribution(
                        source,
                        contribution,
                        output,
                        control,
                        profile,
                        1f - globalWeight,
                        1f - leftWeight,
                        1f - rightWeight,
                        false))
                    return false;
            }
            for (int contribution = 0;
                 contribution < m_ValueContributionCounts[target];
                 contribution++)
            {
                if (!TryAddStateMachineContribution(
                        target,
                        contribution,
                        output,
                        control,
                        profile,
                        globalWeight,
                        leftWeight,
                        rightWeight,
                        true))
                    return false;
            }
            return true;
        }

        bool TryAddStateMachineContribution(
            int sourceValue,
            int sourceIndex,
            int output,
            CharacterPoseStateMachineNativeControl control,
            AnimationBlendProfileNativeEntry profile,
            float scalarFactor,
            float leftFactor,
            float rightFactor,
            bool target)
        {
            AnimationPrimitivePoseContribution source =
                m_ValueContributions[ContributionOffset(sourceValue) + sourceIndex];
            if (!IsValidPrimitiveContribution(source))
                return false;
            float scalarWeight = source.Weight * Mathf.Clamp01(scalarFactor);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(leftFactor);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(rightFactor);
            if (!IsWeight(scalarWeight) || !IsWeight(leftWeight) || !IsWeight(rightWeight))
                return false;

            int targetIndex = FindContribution(output, source);
            if (targetIndex < 0)
            {
                targetIndex = m_ValueContributionCounts[output];
                if (targetIndex >= m_ContributionStride)
                    return false;
                m_ValueContributionCounts[output] = targetIndex + 1;
                ClearContributionWeights(output, targetIndex);
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
            }
            else
            {
                AnimationPrimitivePoseContribution current =
                    m_ValueContributions[ContributionOffset(output) + targetIndex];
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        current.PhysicalPlayerIndex,
                        current.PhysicalSourceIndex,
                        current.PhysicalSourceGeneration,
                        current.Kind,
                        current.SourceOwnerIndex,
                        current.ContributionContinuityIdentity,
                        Mathf.Clamp01(current.Weight + scalarWeight),
                        Mathf.Clamp01(current.LeftFootWeight + leftWeight),
                        Mathf.Clamp01(current.RightFootWeight + rightWeight));
            }
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float blendWeight = EvaluateStandardBlendBoneWeight(
                    control,
                    profile,
                    bone);
                float factor = target ? blendWeight : 1f - blendWeight;
                float weight = GetContributionBoneWeight(
                                   sourceValue,
                                   sourceIndex,
                                   bone) * Mathf.Clamp01(factor);
                float combined = Mathf.Clamp01(
                    GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!IsWeight(combined))
                    return false;
                SetContributionBoneWeight(output, targetIndex, bone, combined);
            }
            return true;
        }

        bool TryBlendStateMachineFootFeatures(
            int source,
            int target,
            int output,
            float leftWeight,
            float rightWeight)
        {
            bool hasSource = m_ValueHasFootFeatures[source] != 0;
            bool hasTarget = m_ValueHasFootFeatures[target] != 0;
            if (!hasSource && !hasTarget)
            {
                m_ValueHasFootFeatures[output] = 0;
                return true;
            }
            if (!TryResolveStateMachineFeature(
                    hasSource,
                    m_ValueLeftFootFeatures[source],
                    hasTarget,
                    m_ValueLeftFootFeatures[target],
                    leftWeight,
                    out AnimationFootFeatureSample left) ||
                !TryResolveStateMachineFeature(
                    hasSource,
                    m_ValueRightFootFeatures[source],
                    hasTarget,
                    m_ValueRightFootFeatures[target],
                    rightWeight,
                    out AnimationFootFeatureSample right))
                return false;
            m_ValueLeftFootFeatures[output] = left;
            m_ValueRightFootFeatures[output] = right;
            m_ValueHasFootFeatures[output] =
                left.IsValid && right.IsValid ? (byte)1 : (byte)0;
            return true;
        }

        bool TryApplyStateMachinePrediction(
            int output,
            int prediction,
            int operationIndex)
        {
            if (prediction < 0)
                return true;
            if (!IsInputReady(prediction, operationIndex) ||
                m_ValueHasFootFeatures[output] == 0 ||
                m_ValueHasFootFeatures[prediction] == 0)
            {
                return false;
            }
            for (int contribution = 0;
                 contribution < m_ValueContributionCounts[prediction];
                 contribution++)
            {
                AnimationPrimitivePoseContribution source =
                    m_ValueContributions[
                        ContributionOffset(prediction) + contribution];
                if (!IsValidPrimitiveContribution(source))
                    return false;
                if (FindContribution(output, source) >= 0)
                    continue;
                int targetIndex = m_ValueContributionCounts[output];
                if (targetIndex >= m_ContributionStride)
                    return false;
                m_ValueContributionCounts[output] = targetIndex + 1;
                ClearContributionWeights(output, targetIndex);
                m_ValueContributions[
                    ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        0f,
                        0f,
                        0f);
            }
            AnimationFootFeatureSample left = ApplyStateMachinePrediction(
                m_ValueLeftFootFeatures[output],
                m_ValueLeftFootFeatures[prediction]);
            AnimationFootFeatureSample right = ApplyStateMachinePrediction(
                m_ValueRightFootFeatures[output],
                m_ValueRightFootFeatures[prediction]);
            if (!left.IsValid || !right.IsValid)
                return false;
            m_ValueLeftFootFeatures[output] = left;
            m_ValueRightFootFeatures[output] = right;
            m_ValueHasFootFeatures[output] = 1;
            return true;
        }

        static AnimationFootFeatureSample ApplyStateMachinePrediction(
            AnimationFootFeatureSample output,
            AnimationFootFeatureSample prediction) =>
            output.WithPredictionPair(
                prediction.PredictedStep,
                prediction.IncomingPredictedStep);

        void EvaluateLayeredBoneBlend(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int overlayValue = operation.InputValueIndexB;
            if (!TryRequireInputs(operation, baseValue, overlayValue))
                return;
            if (m_ValueAvailability[overlayValue] == AnimationPoseAvailability.NoPose)
            {
                if (!TryCopyValue(baseValue, output, operation.Index))
                    SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[baseValue] == AnimationPoseAvailability.NoPose)
            {
                if (!TryCopyValue(overlayValue, output, operation.Index) ||
                    !TryScaleValue(output, operation))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[overlayValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                }
                return;
            }

            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_ValueOutputWeights[output] = UnionWeight(
                m_ValueOutputWeights[baseValue],
                m_ValueOutputWeights[overlayValue] * operation.Weight);
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[baseValue],
                m_ValueContinuityIdentities[overlayValue],
                operation.Index);
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!TryGetBoneOutputWeight(overlayValue, bone, out float overlayOutputWeight))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                float overlay = Mathf.Clamp01(
                    overlayOutputWeight * GetMaskWeight(operation, bone) * operation.Weight);
                if (!TryBlendPose(
                        m_ValueDenseLocalPoses[PoseOffset(baseValue) + bone],
                        m_ValueDenseLocalPoses[PoseOffset(overlayValue) + bone],
                        overlay,
                        out AnimationLocalBonePose pose))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = pose;
            }
            if (!TryCopyParameters(baseValue, output) ||
                !TryMergeContributions(operation, baseValue, overlayValue, output, false) ||
                !TryResolveFootFeatures(operation, baseValue, overlayValue, output, false))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        void EvaluateAdditivePose(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int additiveValue = operation.InputValueIndexB;
            if (!TryRequireInputs(operation, baseValue, additiveValue))
                return;
            if (m_ValueAvailability[additiveValue] == AnimationPoseAvailability.NoPose)
            {
                if (!TryCopyValue(baseValue, output, operation.Index))
                    SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[baseValue] != AnimationPoseAvailability.Pose)
            {
                SetInvalid(
                    output,
                    CombineContinuity(
                        m_ValueContinuityIdentities[baseValue],
                        m_ValueContinuityIdentities[additiveValue],
                        operation.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }

            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_ValueOutputWeights[output] = UnionWeight(
                m_ValueOutputWeights[baseValue],
                m_ValueOutputWeights[additiveValue] * operation.Weight);
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[baseValue],
                m_ValueContinuityIdentities[additiveValue],
                operation.Index);
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!TryGetBoneOutputWeight(additiveValue, bone, out float additiveOutputWeight))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                float weight = Mathf.Clamp01(
                    additiveOutputWeight * GetMaskWeight(operation, bone) * operation.Weight);
                bool valid = operation.AdditiveReferenceSpace switch
                {
                    AdditiveReferenceSpace.Local => TryAddPose(
                        m_ValueDenseLocalPoses[PoseOffset(baseValue) + bone],
                        m_ValueDenseLocalPoses[PoseOffset(additiveValue) + bone],
                        m_AdditiveReferences[operation.AdditiveReferenceOffset + bone],
                        operation.AdditiveScalePolicy,
                        weight,
                        out AnimationLocalBonePose localPose) &&
                        AssignPose(output, bone, localPose),
                    AdditiveReferenceSpace.Mesh => TryAddMeshPose(
                        baseValue,
                        additiveValue,
                        output,
                        operation,
                        bone,
                        weight,
                        out AnimationLocalBonePose meshPose) &&
                        AssignPose(output, bone, meshPose),
                    _ => false
                };
                if (!valid)
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
            }
            if (!TryCopyParameters(baseValue, output) ||
                !TryMergeContributions(operation, baseValue, additiveValue, output, true) ||
                !TryResolveFootFeatures(operation, baseValue, additiveValue, output, true))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        void EvaluatePoseParameterResolve(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int parameterSourceValue = operation.InputValueIndexB;
            if (!TryRequireInputs(operation, baseValue, parameterSourceValue))
                return;
            if (!TryCopyValue(baseValue, output, operation.Index))
            {
                SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[parameterSourceValue] == AnimationPoseAvailability.NoPose)
                return;
            if (!TryResolveParameters(operation, baseValue, parameterSourceValue, output))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[baseValue],
                m_ValueContinuityIdentities[parameterSourceValue],
                operation.Index);
        }

        void EvaluateOutputPose(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int input = operation.InputValueIndexA;
            if (!IsInputReady(input, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            if (m_ValueAvailability[input] == AnimationPoseAvailability.NoPose)
            {
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid, operation.Index);
                return;
            }
            if (!TryCopyValue(input, output, operation.Index))
            {
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[output] == AnimationPoseAvailability.Invalid &&
                m_PoseGraphInvalidReason[0] == AnimationPoseNativeInvalidReason.None)
            {
                RecordGraphInvalid(NormalizeInvalidReason(m_ValueInvalidReasons[output]), operation.Index);
            }
        }

        bool TryRequireInputs(AnimationPoseGraphNativeOperation operation, int inputA, int inputB)
        {
            int output = operation.OutputValueIndex;
            if (!IsInputReady(inputA, operation.Index) || !IsInputReady(inputB, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return false;
            }
            AnimationPoseAvailability availabilityA = m_ValueAvailability[inputA];
            AnimationPoseAvailability availabilityB = m_ValueAvailability[inputB];
            if (!IsAvailability(availabilityA) || !IsAvailability(availabilityB))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return false;
            }
            if (availabilityA == AnimationPoseAvailability.Invalid ||
                availabilityB == AnimationPoseAvailability.Invalid)
            {
                AnimationPoseNativeInvalidReason reason = availabilityA == AnimationPoseAvailability.Invalid
                    ? NormalizeInvalidReason(m_ValueInvalidReasons[inputA])
                    : NormalizeInvalidReason(m_ValueInvalidReasons[inputB]);
                SetInvalid(
                    output,
                    CombineContinuity(
                        m_ValueContinuityIdentities[inputA],
                        m_ValueContinuityIdentities[inputB],
                        output),
                    reason,
                    operation.Index);
                return false;
            }
            return true;
        }

        bool TryCopyValue(int source, int destination, int operationIndex)
        {
            return TryCopyValue(
                source,
                destination,
                operationIndex,
                true);
        }

        bool TryCopyValueWithoutPose(
            int source,
            int destination,
            int operationIndex)
        {
            return TryCopyValue(
                source,
                destination,
                operationIndex,
                false);
        }

        bool TryCopyValue(
            int source,
            int destination,
            int operationIndex,
            bool copyPose)
        {
            if (source < 0 || source >= m_PoseValueCount || destination < 0 || destination >= m_PoseValueCount)
                return false;
            int contributionCount = m_ValueContributionCounts[source];
            if (contributionCount < 0 || contributionCount > m_ContributionStride)
                return false;

            AnimationPoseAvailability availability = m_ValueAvailability[source];
            m_ValueAvailability[destination] = availability;
            m_ValueOutputWeights[destination] = m_ValueOutputWeights[source];
            m_ValueContinuityIdentities[destination] = CombineContinuity(
                m_ValueContinuityIdentities[source],
                (ulong)operationIndex + 1UL,
                operationIndex);
            m_ValueDiscontinuities[destination] = default;
            m_ValueInvalidReasons[destination] = m_ValueInvalidReasons[source];
            if (availability == AnimationPoseAvailability.Pose)
            {
                if (copyPose)
                {
                    NativeArray<AnimationLocalBonePose>.Copy(
                        m_ValueDenseLocalPoses,
                        PoseOffset(source),
                        m_ValueDenseLocalPoses,
                        PoseOffset(destination),
                        m_BoneCount);
                }
                NativeArray<AnimationBlendBoneVelocity>.Copy(
                    m_ValueDenseVelocities,
                    PoseOffset(source),
                    m_ValueDenseVelocities,
                    PoseOffset(destination),
                    m_BoneCount);
            }
            NativeArray<float>.Copy(
                m_ValuePoseParameters,
                ParameterOffset(source),
                m_ValuePoseParameters,
                ParameterOffset(destination),
                m_ParameterCount);
            NativeArray<byte>.Copy(
                m_ValuePoseParameterAvailability,
                ParameterOffset(source),
                m_ValuePoseParameterAvailability,
                ParameterOffset(destination),
                m_ParameterCount);
            m_ValueContributionCounts[destination] = contributionCount;
            if (contributionCount > 0)
            {
                NativeArray<AnimationPrimitivePoseContribution>.Copy(
                    m_ValueContributions,
                    ContributionOffset(source),
                    m_ValueContributions,
                    ContributionOffset(destination),
                    contributionCount);
                NativeArray<float>.Copy(
                    m_ValueDenseContributionWeights,
                    ContributionBoneOffset(source),
                    m_ValueDenseContributionWeights,
                    ContributionBoneOffset(destination),
                    contributionCount * m_BoneCount);
            }
            m_ValueLeftFootFeatures[destination] = m_ValueLeftFootFeatures[source];
            m_ValueRightFootFeatures[destination] = m_ValueRightFootFeatures[source];
            m_ValueHasFootFeatures[destination] = m_ValueHasFootFeatures[source];
            return true;
        }

        bool TryScaleValue(int value, AnimationPoseGraphNativeOperation operation)
        {
            if (m_ValueAvailability[value] != AnimationPoseAvailability.Pose)
                return true;
            float outputWeight = m_ValueOutputWeights[value] * operation.Weight;
            if (!IsWeight(outputWeight))
                return false;
            m_ValueOutputWeights[value] = outputWeight;
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution source =
                    m_ValueContributions[ContributionOffset(value) + contribution];
                float scalarWeight = source.Weight * operation.Weight;
                float leftWeight = source.LeftFootWeight * GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
                float rightWeight = source.RightFootWeight * GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
                if (!IsWeight(scalarWeight) || !IsWeight(leftWeight) || !IsWeight(rightWeight))
                    return false;
                m_ValueContributions[ContributionOffset(value) + contribution] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float weight = GetContributionBoneWeight(value, contribution, bone) *
                                   GetMaskWeight(operation, bone) * operation.Weight;
                    if (!IsWeight(weight))
                        return false;
                    SetContributionBoneWeight(value, contribution, bone, weight);
                }
            }
            return true;
        }

        bool TryResolveParameters(
            AnimationPoseGraphNativeOperation operation,
            int baseValue,
            int overlayValue,
            int output)
        {
            float baseWeight = m_ValueOutputWeights[baseValue];
            float overlayWeight = m_ValueOutputWeights[overlayValue] * operation.Weight;
            if (!IsWeight(baseWeight) || !IsWeight(overlayWeight))
                return false;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                int baseOffset = ParameterOffset(baseValue) + parameter;
                int overlayOffset = ParameterOffset(overlayValue) + parameter;
                int outputOffset = ParameterOffset(output) + parameter;
                float baseParameter = m_ValuePoseParameters[baseOffset];
                float overlayParameter = m_ValuePoseParameters[overlayOffset];
                if (!float.IsFinite(baseParameter) || !float.IsFinite(overlayParameter))
                    return false;
                bool baseAvailable = m_ValuePoseParameterAvailability[baseOffset] != 0;
                bool overlayAvailable = m_ValuePoseParameterAvailability[overlayOffset] != 0;
                PoseParameterResolvePolicy policy =
                    m_ParameterPolicies[operation.ParameterPolicyOffset + parameter];
                float value;
                bool available;
                switch (policy)
                {
                    case PoseParameterResolvePolicy.Base:
                        available = baseAvailable;
                        value = available ? baseParameter : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Overlay:
                        available = overlayWeight > 0f && overlayAvailable || baseAvailable;
                        value = overlayWeight > 0f && overlayAvailable
                            ? overlayParameter
                            : baseAvailable ? baseParameter : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Weighted:
                        float resolvedBaseWeight = baseAvailable ? baseWeight : 0f;
                        float resolvedOverlayWeight = overlayAvailable ? overlayWeight : 0f;
                        float total = resolvedBaseWeight + resolvedOverlayWeight;
                        available = total > 0f;
                        value = total > 0f
                            ? (baseParameter * resolvedBaseWeight + overlayParameter * resolvedOverlayWeight) / total
                            : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Max:
                        available = baseAvailable || overlayAvailable;
                        value = baseAvailable && overlayAvailable
                            ? Mathf.Max(baseParameter, overlayParameter)
                            : baseAvailable ? baseParameter : overlayAvailable ? overlayParameter : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Min:
                        available = baseAvailable || overlayAvailable;
                        value = baseAvailable && overlayAvailable
                            ? Mathf.Min(baseParameter, overlayParameter)
                            : baseAvailable ? baseParameter : overlayAvailable ? overlayParameter : m_ParameterDefaults[parameter];
                        break;
                    default:
                        return false;
                }
                if (!float.IsFinite(value))
                    return false;
                m_ValuePoseParameters[outputOffset] = value;
                m_ValuePoseParameterAvailability[outputOffset] = available ? (byte)1 : (byte)0;
            }
            return true;
        }

        bool TryCopyParameters(int source, int output)
        {
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = m_ValuePoseParameters[ParameterOffset(source) + parameter];
                if (!float.IsFinite(value))
                    return false;
                m_ValuePoseParameters[ParameterOffset(output) + parameter] = value;
                m_ValuePoseParameterAvailability[ParameterOffset(output) + parameter] =
                    m_ValuePoseParameterAvailability[ParameterOffset(source) + parameter];
            }
            m_ValueDiscontinuities[output] = m_ValueDiscontinuities[source];
            return true;
        }

        bool TryMergeContributions(
            AnimationPoseGraphNativeOperation operation,
            int baseValue,
            int overlayValue,
            int output,
            bool additive)
        {
            for (int contribution = 0; contribution < m_ValueContributionCounts[baseValue]; contribution++)
            {
                if (!TryAddContribution(
                        operation,
                        baseValue,
                        contribution,
                        overlayValue,
                        output,
                        false,
                        additive))
                {
                    return false;
                }
            }
            for (int contribution = 0; contribution < m_ValueContributionCounts[overlayValue]; contribution++)
            {
                if (!TryAddContribution(
                        operation,
                        overlayValue,
                        contribution,
                        overlayValue,
                        output,
                        true,
                        additive))
                {
                    return false;
                }
            }
            return true;
        }

        bool TryAddContribution(
            AnimationPoseGraphNativeOperation operation,
            int sourceValue,
            int sourceIndex,
            int overlayValue,
            int output,
            bool overlay,
            bool additive)
        {
            AnimationPrimitivePoseContribution source =
                m_ValueContributions[ContributionOffset(sourceValue) + sourceIndex];
            if (!IsValidPrimitiveContribution(source))
                return false;

            float scalarFactor;
            float leftFactor;
            float rightFactor;
            if (overlay)
            {
                scalarFactor = operation.Weight;
                leftFactor = GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
                rightFactor = GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
            }
            else if (additive)
            {
                scalarFactor = 1f;
                leftFactor = 1f;
                rightFactor = 1f;
            }
            else
            {
                if (!TryGetBoneOutputWeight(overlayValue, m_LeftFootBoneIndex, out float leftOverlay) ||
                    !TryGetBoneOutputWeight(overlayValue, m_RightFootBoneIndex, out float rightOverlay))
                {
                    return false;
                }
                scalarFactor = 1f - m_ValueOutputWeights[overlayValue] * operation.Weight;
                leftFactor = 1f - leftOverlay * GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
                rightFactor = 1f - rightOverlay * GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
            }

            float scalarWeight = source.Weight * Mathf.Clamp01(scalarFactor);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(leftFactor);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(rightFactor);
            if (!IsWeight(scalarWeight) || !IsWeight(leftWeight) || !IsWeight(rightWeight))
                return false;

            int targetIndex = FindContribution(output, source);
            if (targetIndex < 0)
            {
                targetIndex = m_ValueContributionCounts[output];
                if (targetIndex >= m_ContributionStride)
                    return false;
                m_ValueContributionCounts[output] = targetIndex + 1;
                ClearContributionWeights(output, targetIndex);
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
            }
            else
            {
                AnimationPrimitivePoseContribution current =
                    m_ValueContributions[ContributionOffset(output) + targetIndex];
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        current.PhysicalPlayerIndex,
                        current.PhysicalSourceIndex,
                        current.PhysicalSourceGeneration,
                        current.Kind,
                        current.SourceOwnerIndex,
                        current.ContributionContinuityIdentity,
                        Mathf.Clamp01(current.Weight + scalarWeight),
                        Mathf.Clamp01(current.LeftFootWeight + leftWeight),
                        Mathf.Clamp01(current.RightFootWeight + rightWeight));
            }

            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float factor;
                if (overlay)
                {
                    factor = GetMaskWeight(operation, bone) * operation.Weight;
                }
                else if (additive)
                {
                    factor = 1f;
                }
                else
                {
                    if (!TryGetBoneOutputWeight(overlayValue, bone, out float overlayOutput))
                        return false;
                    factor = 1f - overlayOutput * GetMaskWeight(operation, bone) * operation.Weight;
                }
                float weight = GetContributionBoneWeight(sourceValue, sourceIndex, bone) * Mathf.Clamp01(factor);
                float combined = Mathf.Clamp01(GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!IsWeight(combined))
                    return false;
                SetContributionBoneWeight(output, targetIndex, bone, combined);
            }
            return true;
        }

        bool TryResolveFootFeatures(
            AnimationPoseGraphNativeOperation operation,
            int baseValue,
            int overlayValue,
            int output,
            bool additive)
        {
            bool hasBase = m_ValueHasFootFeatures[baseValue] == 1;
            bool hasOverlay = m_ValueHasFootFeatures[overlayValue] == 1;
            if (!hasBase && !hasOverlay)
                return true;
            if (!TryGetBoneOutputWeight(overlayValue, m_LeftFootBoneIndex, out float leftOutput) ||
                !TryGetBoneOutputWeight(overlayValue, m_RightFootBoneIndex, out float rightOutput))
            {
                return false;
            }
            float left = leftOutput * GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
            float right = rightOutput * GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
            if (additive)
            {
                left = left / (1f + left);
                right = right / (1f + right);
            }
            if (!TryResolveFeature(
                    hasBase,
                    m_ValueLeftFootFeatures[baseValue],
                    hasOverlay,
                    m_ValueLeftFootFeatures[overlayValue],
                    left,
                    hasOverlay && left > 0f,
                    out AnimationFootFeatureSample leftFeature) ||
                !TryResolveFeature(
                    hasBase,
                    m_ValueRightFootFeatures[baseValue],
                    hasOverlay,
                    m_ValueRightFootFeatures[overlayValue],
                    right,
                    hasOverlay && right > 0f,
                    out AnimationFootFeatureSample rightFeature))
            {
                return false;
            }
            m_ValueLeftFootFeatures[output] = leftFeature;
            m_ValueRightFootFeatures[output] = rightFeature;
            m_ValueHasFootFeatures[output] = leftFeature.IsValid && rightFeature.IsValid ? (byte)1 : (byte)0;
            return true;
        }

        bool TryValidateValueEnvelope(
            int value,
            out AnimationPoseNativeInvalidReason reason)
        {
            reason = AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
            AnimationPoseAvailability availability = m_ValueAvailability[value];
            AnimationPoseNativeInvalidReason invalidReason = m_ValueInvalidReasons[value];
            int contributionCount = m_ValueContributionCounts[value];
            byte hasFootFeatures = m_ValueHasFootFeatures[value];
            if (!IsAvailability(availability) ||
                !IsWeight(m_ValueOutputWeights[value]) ||
                m_ValueContinuityIdentities[value] == 0 ||
                contributionCount < 0 || contributionCount > m_ContributionStride ||
                hasFootFeatures > 1)
            {
                return false;
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                byte parameterAvailable = m_ValuePoseParameterAvailability[ParameterOffset(value) + parameter];
                if (!float.IsFinite(m_ValuePoseParameters[ParameterOffset(value) + parameter]) || parameterAvailable > 1)
                    return false;
            }

            if (availability == AnimationPoseAvailability.Invalid)
            {
                reason = NormalizeInvalidReason(invalidReason);
                return invalidReason != AnimationPoseNativeInvalidReason.None &&
                       contributionCount == 0 && m_ValueOutputWeights[value] == 0f && hasFootFeatures == 0;
            }
            if (invalidReason != AnimationPoseNativeInvalidReason.None)
                return false;
            if (availability == AnimationPoseAvailability.NoPose)
            {
                return contributionCount == 0 && m_ValueOutputWeights[value] == 0f && hasFootFeatures == 0;
            }
            if (contributionCount <= 0)
                return false;
            if (hasFootFeatures == 1 &&
                (!IsValidFootFeature(m_ValueLeftFootFeatures[value]) ||
                 !IsValidFootFeature(m_ValueRightFootFeatures[value])))
            {
                return false;
            }
            reason = AnimationPoseNativeInvalidReason.None;
            return true;
        }

        bool TryValidateValueDeep(
            int value,
            out AnimationPoseNativeInvalidReason reason)
        {
            if (!TryValidateValueEnvelope(value, out reason))
                return false;
            if (m_ValueAvailability[value] != AnimationPoseAvailability.Pose)
                return true;
            int contributionCount = m_ValueContributionCounts[value];
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!m_ValueDenseLocalPoses[PoseOffset(value) + bone].IsValid)
                    return false;
            }
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                if (!IsValidPrimitiveContribution(
                        m_ValueContributions[ContributionOffset(value) + contribution]))
                {
                    return false;
                }
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    if (!IsWeight(GetContributionBoneWeight(value, contribution, bone)))
                        return false;
                }
            }
            reason = AnimationPoseNativeInvalidReason.None;
            return true;
        }

        bool IsInputReady(int value, int operationIndex)
        {
            if (value < 0 || value >= m_PoseValueCount)
                return false;
            for (int i = 0; i < m_Operations.Length; i++)
            {
                AnimationPoseGraphNativeOperation candidate = m_Operations[i];
                if (candidate.Index < operationIndex && candidate.OutputValueIndex == value)
                    return m_FrameCacheCompletedAt[candidate.FrameCacheIndex] == m_CompletionIdentity;
            }
            return false;
        }

        bool TryGetBoneOutputWeight(int value, int bone, out float result)
        {
            result = 0f;
            int count = m_ValueContributionCounts[value];
            if (count < 0 || count > m_ContributionStride || bone < 0 || bone >= m_BoneCount)
                return false;
            for (int contribution = 0; contribution < count; contribution++)
            {
                float weight = GetContributionBoneWeight(value, contribution, bone);
                if (!IsWeight(weight))
                    return false;
                result += weight;
                if (!float.IsFinite(result))
                    return false;
            }
            result = Mathf.Clamp01(result);
            return true;
        }

        int FindContribution(int value, AnimationPrimitivePoseContribution source)
        {
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution candidate =
                    m_ValueContributions[ContributionOffset(value) + contribution];
                if (candidate.PhysicalPlayerIndex == source.PhysicalPlayerIndex &&
                    candidate.PhysicalSourceIndex == source.PhysicalSourceIndex &&
                    candidate.PhysicalSourceGeneration == source.PhysicalSourceGeneration &&
                    candidate.Kind == source.Kind &&
                    candidate.SourceOwnerIndex == source.SourceOwnerIndex &&
                    candidate.ContributionContinuityIdentity == source.ContributionContinuityIdentity)
                {
                    return contribution;
                }
            }
            return -1;
        }

        void ResetValue(int value)
        {
            m_ValueContributionCounts[value] = 0;
            m_ValueOutputWeights[value] = 0f;
            m_ValueLeftFootFeatures[value] = default;
            m_ValueRightFootFeatures[value] = default;
            m_ValueHasFootFeatures[value] = 0;
            m_ValueAvailability[value] = AnimationPoseAvailability.Invalid;
            m_ValueContinuityIdentities[value] = 1;
            m_ValueDiscontinuities[value] = default;
            m_ValueInvalidReasons[value] = AnimationPoseNativeInvalidReason.None;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_ValuePoseParameters[ParameterOffset(value) + parameter] = m_ParameterDefaults[parameter];
                m_ValuePoseParameterAvailability[ParameterOffset(value) + parameter] = 0;
            }
        }

        void ClearContributionWeights(int value, int contribution)
        {
            int offset = ContributionBoneOffset(value) + contribution * m_BoneCount;
            for (int bone = 0; bone < m_BoneCount; bone++)
                m_ValueDenseContributionWeights[offset + bone] = 0f;
        }

        void SetInvalid(
            int value,
            ulong continuity,
            AnimationPoseNativeInvalidReason reason,
            int operationIndex)
        {
            reason = NormalizeInvalidReason(reason);
            m_ValueContributionCounts[value] = 0;
            m_ValueOutputWeights[value] = 0f;
            m_ValueLeftFootFeatures[value] = default;
            m_ValueRightFootFeatures[value] = default;
            m_ValueHasFootFeatures[value] = 0;
            m_ValueAvailability[value] = AnimationPoseAvailability.Invalid;
            m_ValueContinuityIdentities[value] = RequireIdentity(continuity);
            m_ValueDiscontinuities[value] = default;
            m_ValueInvalidReasons[value] = reason;
            RecordGraphInvalid(reason, operationIndex);
        }

        void RecordGraphInvalid(AnimationPoseNativeInvalidReason reason, int operationIndex)
        {
            if (m_PoseGraphInvalidReason[0] != AnimationPoseNativeInvalidReason.None)
                return;
            m_PoseGraphInvalidReason[0] = NormalizeInvalidReason(reason);
            m_PoseGraphInvalidOperationIndex[0] = operationIndex;
        }

        bool TryAddMeshPose(
            int baseValue,
            int additiveValue,
            int outputValue,
            AnimationPoseGraphNativeOperation operation,
            int bone,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!TryResolveModelPose(baseValue, bone, out AnimationLocalBonePose basePose) ||
                !TryResolveModelPose(additiveValue, bone, out AnimationLocalBonePose additivePose) ||
                !TryAddPose(
                    basePose,
                    additivePose,
                    m_AdditiveReferences[operation.AdditiveReferenceOffset + bone],
                    operation.AdditiveScalePolicy,
                    weight,
                    out AnimationLocalBonePose modelResult))
            {
                return false;
            }
            int parentIndex = m_ParentIndices[bone];
            if (parentIndex < 0)
            {
                result = modelResult;
                return true;
            }
            return TryResolveModelPose(outputValue, parentIndex, out AnimationLocalBonePose outputParent) &&
                   TryToLocal(outputParent, modelResult, out result);
        }

        bool TryResolveModelPose(int value, int bone, out AnimationLocalBonePose result)
        {
            result = m_ValueDenseLocalPoses[PoseOffset(value) + bone];
            if (!result.IsValid)
                return false;
            int parentIndex = m_ParentIndices[bone];
            while (parentIndex >= 0)
            {
                AnimationLocalBonePose parent = m_ValueDenseLocalPoses[PoseOffset(value) + parentIndex];
                if (!TryToModel(parent, result, out result))
                    return false;
                parentIndex = m_ParentIndices[parentIndex];
            }
            return true;
        }

        bool AssignPose(int value, int bone, AnimationLocalBonePose pose)
        {
            if (!pose.IsValid)
                return false;
            m_ValueDenseLocalPoses[PoseOffset(value) + bone] = pose;
            return true;
        }

        float GetMaskWeight(AnimationPoseGraphNativeOperation operation, int bone) =>
            operation.BoneMaskOffset < 0 ? 1f : m_DenseBoneMasks[operation.BoneMaskOffset + bone];

        float GetContributionBoneWeight(int value, int contribution, int bone) =>
            m_ValueDenseContributionWeights[
                ContributionBoneOffset(value) + contribution * m_BoneCount + bone];

        void SetContributionBoneWeight(int value, int contribution, int bone, float weight)
        {
            m_ValueDenseContributionWeights[
                ContributionBoneOffset(value) + contribution * m_BoneCount + bone] = weight;
        }

        int PoseOffset(int value) => value * m_BoneCount;
        int ParameterOffset(int value) => value * m_ParameterCount;
        int ContributionOffset(int value) => value * m_ContributionStride;
        int ContributionBoneOffset(int value) => value * m_ContributionStride * m_BoneCount;

        static bool TryBlendPose(
            AnimationLocalBonePose from,
            AnimationLocalBonePose to,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!from.IsValid || !to.IsValid || !IsWeight(weight))
                return false;
            Quaternion target = to.Rotation;
            if (Quaternion.Dot(from.Rotation, target) < 0f)
                target = new Quaternion(-target.x, -target.y, -target.z, -target.w);
            return TryCreatePose(
                Vector3.LerpUnclamped(from.Position, to.Position, weight),
                Quaternion.SlerpUnclamped(from.Rotation, target, weight),
                Vector3.LerpUnclamped(from.Scale, to.Scale, weight),
                out result);
        }

        static bool TryAddPose(
            AnimationLocalBonePose basePose,
            AnimationLocalBonePose additivePose,
            AnimationLocalBonePose referencePose,
            AdditiveScalePolicy scalePolicy,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!basePose.IsValid || !additivePose.IsValid || !referencePose.IsValid || !IsWeight(weight))
                return false;
            Quaternion delta = additivePose.Rotation * Quaternion.Inverse(referencePose.Rotation);
            if (delta.w < 0f)
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            Quaternion rotation = basePose.Rotation *
                                  Quaternion.SlerpUnclamped(Quaternion.identity, delta, weight);
            Vector3 scale;
            switch (scalePolicy)
            {
                case AdditiveScalePolicy.Multiply:
                    if (!TryDivide(additivePose.Scale, referencePose.Scale, out Vector3 scaleRatio))
                        return false;
                    scale = Vector3.Scale(
                        basePose.Scale,
                        Vector3.LerpUnclamped(Vector3.one, scaleRatio, weight));
                    break;
                case AdditiveScalePolicy.AddDelta:
                    scale = basePose.Scale + (additivePose.Scale - referencePose.Scale) * weight;
                    break;
                case AdditiveScalePolicy.Ignore:
                    scale = basePose.Scale;
                    break;
                default:
                    return false;
            }
            return TryCreatePose(
                basePose.Position + (additivePose.Position - referencePose.Position) * weight,
                rotation,
                scale,
                out result);
        }

        static bool TryToModel(
            AnimationLocalBonePose parent,
            AnimationLocalBonePose local,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!parent.IsValid || !local.IsValid)
                return false;
            return TryCreatePose(
                parent.Position + parent.Rotation * Vector3.Scale(parent.Scale, local.Position),
                parent.Rotation * local.Rotation,
                Vector3.Scale(parent.Scale, local.Scale),
                out result);
        }

        static bool TryToLocal(
            AnimationLocalBonePose parent,
            AnimationLocalBonePose model,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!parent.IsValid || !model.IsValid)
                return false;
            Quaternion inverse = Quaternion.Inverse(parent.Rotation);
            if (!TryDivide(inverse * (model.Position - parent.Position), parent.Scale, out Vector3 position) ||
                !TryDivide(model.Scale, parent.Scale, out Vector3 scale))
            {
                return false;
            }
            return TryCreatePose(position, inverse * model.Rotation, scale, out result);
        }

        static bool TryDivide(Vector3 value, Vector3 divisor, out Vector3 result)
        {
            result = default;
            if (!IsFinite(value) || !IsFinite(divisor) ||
                Mathf.Abs(divisor.x) <= ScaleEpsilon ||
                Mathf.Abs(divisor.y) <= ScaleEpsilon ||
                Mathf.Abs(divisor.z) <= ScaleEpsilon)
            {
                return false;
            }
            result = new Vector3(
                value.x / divisor.x,
                value.y / divisor.y,
                value.z / divisor.z);
            return IsFinite(result);
        }

        static bool TryCreatePose(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) ||
                Quaternion.Dot(rotation, rotation) <= 0f)
            {
                return false;
            }
            result = new AnimationLocalBonePose(position, rotation, scale);
            return result.IsValid;
        }

        static bool TryResolveFeature(
            bool hasBase,
            AnimationFootFeatureSample baseValue,
            bool hasOverlay,
            AnimationFootFeatureSample overlayValue,
            float weight,
            bool overlayPredictionAuthoritative,
            out AnimationFootFeatureSample result)
        {
            result = default;
            if (!hasBase)
            {
                if (!hasOverlay || !IsValidFootFeature(overlayValue))
                    return false;
                result = overlayValue;
                return true;
            }
            if (!IsValidFootFeature(baseValue))
                return false;
            if (!hasOverlay)
            {
                result = baseValue;
                return true;
            }
            if (!IsValidFootFeature(overlayValue) || !float.IsFinite(weight))
                return false;
            float t = Mathf.Clamp01(weight);
            Vector3 velocity = Vector3.LerpUnclamped(
                baseValue.SoleLocalVelocity,
                overlayValue.SoleLocalVelocity,
                t);
            float height = Mathf.LerpUnclamped(baseValue.SoleHeight, overlayValue.SoleHeight, t);
            float plant = Mathf.LerpUnclamped(baseValue.PlantConfidence, overlayValue.PlantConfidence, t);
            float contact = Mathf.LerpUnclamped(baseValue.Contact, overlayValue.Contact, t);
            AnimationPredictedFootStepSample predicted = overlayPredictionAuthoritative
                ? overlayValue.PredictedStep
                : baseValue.PredictedStep;
            AnimationPredictedFootStepSample incomingPredicted = overlayPredictionAuthoritative
                ? overlayValue.IncomingPredictedStep
                : baseValue.IncomingPredictedStep;
            if (!IsFinite(velocity) || !float.IsFinite(height) || !IsWeight(plant) ||
                !IsWeight(contact))
            {
                return false;
            }
            result = new AnimationFootFeatureSample(
                velocity,
                height,
                plant,
                predicted,
                incomingPredicted,
                contact);
            return result.IsValid;
        }

        static bool TryResolveStateMachineFeature(
            bool hasSource,
            AnimationFootFeatureSample source,
            bool hasTarget,
            AnimationFootFeatureSample target,
            float weight,
            out AnimationFootFeatureSample result)
        {
            if (!TryResolveFeature(
                    hasSource,
                    source,
                    hasTarget,
                    target,
                    weight,
                    true,
                    out result))
            {
                return false;
            }
            return result.IsValid;
        }

        static bool IsValidPrimitiveContribution(AnimationPrimitivePoseContribution contribution)
        {
            int kind = (int)contribution.Kind;
            bool live = contribution.Kind == AnimationPoseContributionKind.Live;
            return contribution.PhysicalPlayerIndex >= 0 &&
                   kind >= (int)AnimationPoseContributionKind.Live &&
                   kind <= (int)AnimationPoseContributionKind.Stored &&
                   (live
                       ? contribution.PhysicalSourceIndex >= 0 &&
                         contribution.PhysicalSourceGeneration != 0 &&
                         contribution.SourceOwnerIndex >= 0
                       : contribution.PhysicalSourceIndex == -1 &&
                         contribution.PhysicalSourceGeneration == 0 &&
                         contribution.SourceOwnerIndex == -1) &&
                   contribution.ContributionContinuityIdentity != 0 &&
                   IsWeight(contribution.Weight) &&
                   IsWeight(contribution.LeftFootWeight) &&
                   IsWeight(contribution.RightFootWeight);
        }

        static bool IsValidFootFeature(AnimationFootFeatureSample sample) =>
            sample.IsValid &&
            IsFinite(sample.SoleLocalVelocity) &&
            float.IsFinite(sample.SoleHeight) &&
            IsWeight(sample.PlantConfidence) &&
            IsWeight(sample.Contact) &&
            (!sample.PredictedStep.IsValid ||
             IsWeight(sample.PredictedStep.Confidence) &&
             float.IsFinite(sample.PredictedStep.TimeToLandingSeconds) &&
             sample.PredictedStep.TimeToLandingSeconds >= 0f &&
             IsWeight(sample.PredictedStep.EventPhase) &&
             IsWeight(sample.PredictedStep.LiftOffPhase) &&
             IsValidRootLocalFootRoute(sample.PredictedStep)) &&
            (!sample.IncomingPredictedStep.IsValid ||
             IsWeight(sample.IncomingPredictedStep.Confidence) &&
             float.IsFinite(sample.IncomingPredictedStep.TimeToLandingSeconds) &&
             sample.IncomingPredictedStep.TimeToLandingSeconds >= 0f &&
             IsWeight(sample.IncomingPredictedStep.EventPhase) &&
             IsWeight(sample.IncomingPredictedStep.LiftOffPhase) &&
             IsValidRootLocalFootRoute(sample.IncomingPredictedStep));

        static bool IsValidRootLocalFootRoute(AnimationPredictedFootStepSample value)
        {
            if (value.Route.RootLocalFoot.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.RootLocalAnkle.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.RootLocalHip.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.AuthoredFootPlanar.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.AnimationClearance.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                !IsWeight(value.LandingPhase) ||
                !IsFinite(value.OpposingRootLocalSoleRotation) ||
                Quaternion.Dot(value.OpposingRootLocalSoleRotation, value.OpposingRootLocalSoleRotation) <= 0.000001f)
                return false;
            for (int i = 0; i < value.Route.RootLocalFoot.Length; i++)
            {
                if (!IsFinite(value.Route.RootLocalFoot[i]) ||
                    !IsFinite(value.Route.RootLocalAnkle[i]) ||
                    !IsFinite(value.Route.RootLocalHip[i]) ||
                    !IsFinite(value.Route.AuthoredFootPlanar[i]) ||
                    !float.IsFinite(value.Route.AnimationClearance[i]) ||
                    value.Route.AnimationClearance[i] < 0f)
                    return false;
            }
            return true;
        }

        static AnimationPoseNativeInvalidReason NormalizeInvalidReason(
            AnimationPoseNativeInvalidReason reason) =>
            AnimationPoseNativeInvalidReasonContract.NormalizeFailure(reason);

        static bool IsAvailability(AnimationPoseAvailability availability)
        {
            int value = (int)availability;
            return value >= (int)AnimationPoseAvailability.Pose &&
                   value <= (int)AnimationPoseAvailability.Invalid;
        }

        static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        static float UnionWeight(float a, float b) =>
            Mathf.Clamp01(1f - (1f - Mathf.Clamp01(a)) * (1f - Mathf.Clamp01(b)));

        static ulong CombineContinuity(ulong a, ulong b, int operation)
        {
            unchecked
            {
                ulong value = 1469598103934665603UL;
                value = (value ^ RequireIdentity(a)) * 1099511628211UL;
                value = (value ^ RequireIdentity(b)) * 1099511628211UL;
                value = (value ^ (ulong)(operation + 1)) * 1099511628211UL;
                return RequireIdentity(value);
            }
        }

        static ulong RequireIdentity(ulong value) => value == 0 ? 1UL : value;

        static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);

        static Vector3 ClampMagnitude(Vector3 value, float maximum) =>
            maximum <= 0f ? Vector3.zero : Vector3.ClampMagnitude(value, maximum);

        static Vector3 RotationResidual(Quaternion previous, Quaternion current, float maximumDegrees)
        {
            Quaternion delta = previous * Quaternion.Inverse(current);
            if (delta.w < 0f)
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            delta.ToAngleAxis(out float angleDegrees, out Vector3 axis);
            if (!float.IsFinite(angleDegrees) || !IsFinite(axis) || axis.sqrMagnitude <= 0.000001f)
                return Vector3.zero;
            if (angleDegrees > 180f)
                angleDegrees -= 360f;
            angleDegrees = Mathf.Clamp(angleDegrees, -maximumDegrees, maximumDegrees);
            return axis.normalized * (angleDegrees * Mathf.Deg2Rad);
        }

        static void RequireValidConfiguration(
            CharacterPoseGraphNativeProgram program,
            PoseInertializationNativeProgram inertializationProgram,
            CharacterPoseGraphNativeBinding binding,
            CharacterPoseConstraintRuntime poseConstraints)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            program.RequireValid();
            if (poseConstraints == null ||
                program.FullBodyIkCount != 1 ||
                poseConstraints.FullBodyIkGoalContributionCount !=
                program.FullBodyIkGoalContributionCount ||
                poseConstraints.FullBodyIkContributionGoalCount !=
                program.FullBodyIkContributionGoalCount)
            {
                throw new ArgumentException("FinalIK Full Body solver layout is invalid.", nameof(poseConstraints));
            }
            if (inertializationProgram == null || inertializationProgram.BoneCount != program.PoseBoneCount ||
                inertializationProgram.ParameterCount != program.ParameterCount ||
                inertializationProgram.ResetRequests.Length != inertializationProgram.Nodes.Length)
                throw new ArgumentException("Pose Inertialization Native Program is invalid.", nameof(inertializationProgram));
            binding.RequireValid();
            AnimationPoseNativeAggregateLayout layout = binding.Layout;
            if (layout.BoneCount != program.PoseBoneCount ||
                layout.ParameterCount != program.ParameterCount ||
                layout.PoseValueCount != program.PoseValueCount ||
                layout.PoseValueContributionStride != program.ContributionStride ||
                layout.OperationCount != program.FrameCacheCount ||
                layout.FrameCacheCount != program.FrameCacheCount ||
                layout.StageCount != program.Stages.Length ||
                layout.OutputValueIndex != program.OutputValueIndex ||
                program.OutputOperationIndex < 0 ||
                program.OutputOperationIndex >= program.FrameCacheCount ||
                program.OutputNativeOperationIndex < 0 ||
                program.OutputNativeOperationIndex >= program.Operations.Length ||
                program.LeftFootBoneIndex < 0 || program.LeftFootBoneIndex >= program.PoseBoneCount ||
                program.RightFootBoneIndex < 0 || program.RightFootBoneIndex >= program.PoseBoneCount)
            {
                throw new ArgumentException("Animation Pose Graph Native Job layout is invalid.", nameof(binding));
            }

            for (int bone = 0; bone < program.PoseBoneCount; bone++)
            {
                int parentIndex = program.ParentIndices[bone];
                if (parentIndex < -1 || parentIndex >= bone)
                    throw new ArgumentException($"Animation Pose Graph Native Job parent #{bone} is invalid.", nameof(program));
            }
            for (int parameter = 0; parameter < program.ParameterCount; parameter++)
            {
                if (!float.IsFinite(program.ParameterDefaults[parameter]))
                    throw new ArgumentException($"Animation Pose Graph Native Job parameter #{parameter} is invalid.", nameof(program));
            }

            int outputCount = 0;
            int nativeOperationStart = 0;
            for (int stageIndex = 0; stageIndex < program.Stages.Length; stageIndex++)
            {
                AnimationPoseGraphNativeStage stage = program.Stages[stageIndex];
                if (stage.Index != stageIndex || stage.OperationStart != nativeOperationStart ||
                    stage.OperationCount < 0 ||
                    stage.OperationStart > program.Operations.Length - stage.OperationCount ||
                    stage.CompletionIndex != stageIndex || stage.DiagnosticIndex != stageIndex)
                {
                    throw new ArgumentException(
                        $"Animation Pose Graph Native Job stage #{stageIndex} is invalid.", nameof(program));
                }
                nativeOperationStart += stage.OperationCount;
            }
            if (nativeOperationStart != program.Operations.Length)
                throw new ArgumentException("Animation Pose Graph Native Job stages are incomplete.", nameof(program));
            for (int i = 0; i < program.Operations.Length; i++)
            {
                AnimationPoseGraphNativeOperation operation = program.Operations[i];
                if (operation.Index < 0 || operation.Index >= program.FrameCacheCount ||
                    operation.FrameCacheIndex != operation.Index ||
                    operation.OutputValueIndex < -1 ||
                    operation.OutputValueIndex >= program.PoseValueCount ||
                    operation.OutputFullBodyIkGoalContributionValueIndex < -1 ||
                    operation.OutputFullBodyIkGoalContributionValueIndex >=
                    poseConstraints.FullBodyIkGoalContributionCount ||
                    operation.OutputFullBodyIkGoalSetValueIndex < -1 ||
                    operation.OutputFullBodyIkGoalSetValueIndex >=
                    program.FullBodyIkGoalSetValueCount ||
                    operation.InputFullBodyIkGoalSetValueIndex < -1 ||
                    operation.InputFullBodyIkGoalSetValueIndex >=
                    program.FullBodyIkGoalSetValueCount ||
                    operation.FullBodyIkGoalContributionInputStart < -1 ||
                    operation.FullBodyIkGoalContributionInputCount < 0 ||
                    operation.LinkedPoseCallIndex < -1 ||
                    operation.LinkedPoseFragmentIndex < -1 ||
                    operation.LinkedPoseFragmentIndex >= program.LinkedPoseActiveFragments.Length ||
                    !float.IsFinite(operation.Weight) || operation.Weight < 0f || operation.Weight > 1f)
                {
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} is invalid.", nameof(program));
                }
                bool validPoseInputA = operation.InputValueIndexA >= 0 &&
                                       operation.InputValueIndexA < program.PoseValueCount;
                bool validPoseInputB = operation.InputValueIndexB >= 0 &&
                                       operation.InputValueIndexB < program.PoseValueCount;
                bool inputA = validPoseInputA &&
                              operation.OutputValueIndex >= 0 &&
                              operation.InputValueIndexA < operation.OutputValueIndex;
                bool inputB = validPoseInputB &&
                              operation.OutputValueIndex >= 0 &&
                              operation.InputValueIndexB < operation.OutputValueIndex;
                bool valid = operation.Code switch
                {
                    CharacterPoseOperationCode.SelectedPosePlayer or CharacterPoseOperationCode.BlendSpacePlayer or
                        CharacterPoseOperationCode.ClipPlayer or CharacterPoseOperationCode.BlendStack =>
                        operation.InputValueIndexA == -1 && operation.InputValueIndexB == -1 &&
                        operation.PhysicalPlayerIndex >= 0 && operation.PhysicalPlayerIndex < layout.PlayerCount &&
                        IsOutputPolicy(operation.AnimationSelectionAvailabilityPolicy) &&
                        operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.AnimationSlot =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.PhysicalPlayerIndex >= 0 && operation.PhysicalPlayerIndex < layout.PlayerCount &&
                        operation.AnimationSlotIndex >= 0 &&
                        operation.AnimationSlotIndex < program.AnimationSlotControls.Length &&
                        inertializationProgram.SlotNodeOffset + operation.AnimationSlotIndex <
                        inertializationProgram.Nodes.Length &&
                        operation.AnimationSelectionAvailabilityPolicy == AnimationSelectionAvailabilityPolicy.AllowEmpty &&
                        operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.Inertialization =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.InertializationIndex >= 0 && operation.InertializationIndex < inertializationProgram.Nodes.Length,
                    CharacterPoseOperationCode.BlendPose =>
                        inputA && inputB && operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 && operation.ParameterPolicyOffset == -1 &&
                        operation.ParameterIndex < program.ParameterCount,
                    CharacterPoseOperationCode.LayeredBoneBlend =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.PoseBoneCount) &&
                        operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.AdditivePose =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.PoseBoneCount) &&
                        HasSpan(program.AdditiveReferences, operation.AdditiveReferenceOffset, program.PoseBoneCount) &&
                        IsAdditiveReferenceSpace(operation.AdditiveReferenceSpace) &&
                        IsAdditiveScalePolicy(operation.AdditiveScalePolicy) &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.PoseParameterResolve =>
                        inputA && inputB && operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        HasSpan(program.ParameterPolicies, operation.ParameterPolicyOffset, program.ParameterCount),
                    CharacterPoseOperationCode.ModifyBone =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.ModifyBoneIndex >= 0 && operation.ModifyBoneIndex < program.ModifyBones.Length,
                    CharacterPoseOperationCode.RootOrientationWarp =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.RootOrientationWarpIndex >= 0 &&
                        operation.RootOrientationWarpIndex < program.RootOrientationWarps.Length,
                    CharacterPoseOperationCode.PoseBoneIKGoals =>
                        operation.OutputValueIndex == -1 && validPoseInputA &&
                        operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex >= 0 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.FullBodyIkGoalContributionInputCount == 0 &&
                        operation.PoseBoneIkGoalsIndex >= 0 &&
                        operation.PoseBoneIkGoalsIndex < program.PoseBoneIkGoalRanges.Length,
                    CharacterPoseOperationCode.FootPlacement =>
                        operation.OutputValueIndex == -1 && validPoseInputA &&
                        operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex >= 0 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.FullBodyIkGoalContributionInputCount == 0 &&
                        operation.FootPlacementIndex >= 0 &&
                        operation.FootPlacementIndex < program.FootPlacementCount,
                    CharacterPoseOperationCode.FullBodyIkGoalAssembler =>
                        operation.OutputValueIndex == -1 &&
                        operation.InputValueIndexA == -1 &&
                        operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex == -1 &&
                        operation.OutputFullBodyIkGoalSetValueIndex >= 0 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        (operation.FullBodyIkGoalContributionInputCount == 0 &&
                         operation.FullBodyIkGoalContributionInputStart == -1 ||
                         operation.FullBodyIkGoalContributionInputCount > 0 &&
                         operation.FullBodyIkGoalContributionInputStart >= 0 &&
                         operation.FullBodyIkGoalContributionInputStart <=
                         program.FullBodyIkGoalContributionInputValueIndices.Length -
                         operation.FullBodyIkGoalContributionInputCount),
                    CharacterPoseOperationCode.FullBodyIK =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex == -1 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex >= 0 &&
                        operation.FullBodyIkIndex >= 0 &&
                        operation.FullBodyIkIndex < program.FullBodyIkCount &&
                        operation.FullBodyIkGoalContributionInputCount == 0 &&
                        poseConstraints.IsFullBodyIkPrepared,
                    CharacterPoseOperationCode.LinkedPoseCall =>
                        validPoseInputA && operation.InputValueIndexB == -1 &&
                        operation.LinkedPoseCallIndex >= 0 &&
                        operation.LinkedPoseCallIndex < program.LinkedPoseCalls.Length &&
                        operation.LinkedPoseFragmentIndex == -1 &&
                        operation.OutputValueIndex >= 0 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex == -1 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.FullBodyIkGoalContributionInputCount == 0,
                    CharacterPoseOperationCode.LocalToComponentPose or
                        CharacterPoseOperationCode.ComponentToLocalPose =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.StatePoseOutput =>
                        inputA && operation.InputValueIndexB == -1,
                    CharacterPoseOperationCode.PoseStateMachine =>
                        operation.InputValueIndexA == -1 && operation.InputValueIndexB == -1 &&
                        operation.StateMachineIndex >= 0 &&
                        operation.StateMachineIndex < program.StateMachineControls.Length,
                    CharacterPoseOperationCode.OutputPose =>
                        inputA && operation.InputValueIndexB == -1 && operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 && operation.ParameterPolicyOffset == -1,
                    _ => false
                };
                if (!valid)
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} layout is invalid.", nameof(program));
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != program.OutputNativeOperationIndex ||
                        operation.Index != program.OutputOperationIndex ||
                        operation.OutputValueIndex != program.OutputValueIndex)
                    {
                        throw new ArgumentException("Animation Pose Graph Native Job output identity is invalid.", nameof(program));
                    }
                }
            }
            if (outputCount != 1)
                throw new ArgumentException("Animation Pose Graph Native Job requires one output operation.", nameof(program));
        }

        static bool HasSpan<T>(NativeArray<T> values, int offset, int count) where T : struct =>
            offset >= 0 && count > 0 && offset <= values.Length - count;

        static bool IsOutputPolicy(AnimationSelectionAvailabilityPolicy value) =>
            (int)value >= (int)AnimationSelectionAvailabilityPolicy.RequireSelection &&
            (int)value <= (int)AnimationSelectionAvailabilityPolicy.AllowEmpty;

        static bool IsAdditiveReferenceSpace(AdditiveReferenceSpace value) =>
            (int)value >= (int)AdditiveReferenceSpace.Local &&
            (int)value <= (int)AdditiveReferenceSpace.Mesh;

        static bool IsAdditiveScalePolicy(AdditiveScalePolicy value) =>
            (int)value >= (int)AdditiveScalePolicy.Multiply &&
            (int)value <= (int)AdditiveScalePolicy.Ignore;
    }
}
