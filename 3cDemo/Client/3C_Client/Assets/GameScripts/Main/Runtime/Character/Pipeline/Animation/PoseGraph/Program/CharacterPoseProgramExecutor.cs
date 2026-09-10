using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseWorldFrameInput
    {
        readonly CharacterPoseWorldContextAdapter m_Adapter;

        internal CharacterPoseWorldFrameInput(
            CharacterPoseWorldContextAdapter adapter,
            ActorId actorId,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            ulong completionIdentity)
        {
            m_Adapter = adapter ??
                throw new ArgumentNullException(nameof(adapter));
            ActorId = actorId;
            PresentationFrame = presentationFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            BodyFrame = bodyFrame;
            FactFrame = factFrame;
            CompletionIdentity = completionIdentity;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Character Pose world frame input is invalid.");
            }
        }

        internal ActorId ActorId { get; }
        internal ulong PresentationFrame { get; }
        internal float PresentationDeltaSeconds { get; }
        internal CharacterBodyPresentationFrame BodyFrame { get; }
        internal CharacterPresentationFactFrame FactFrame { get; }
        internal ulong CompletionIdentity { get; }
        internal bool IsValid =>
            m_Adapter != null &&
            ActorId.IsValid &&
            PresentationFrame != 0 &&
            float.IsFinite(PresentationDeltaSeconds) &&
            PresentationDeltaSeconds >= 0f &&
            BodyFrame.IsValid &&
            FactFrame.IsValid &&
            CompletionIdentity != 0;

        internal CharacterFootPlacementFrameInput BuildFootPlacement(
            in AnimationPoseValueNativeReadBinding inputBinding,
            int parameterIndex)
        {
            CharacterBodyPresentationFrame bodyFrame = BodyFrame;
            CharacterPresentationFactFrame factFrame = FactFrame;
            return m_Adapter.BuildFootPlacement(
                ActorId,
                PresentationFrame,
                PresentationDeltaSeconds,
                in bodyFrame,
                in factFrame,
                CompletionIdentity,
                in inputBinding,
                parameterIndex);
        }
    }

    internal sealed class CharacterPoseProgramExecutor :
        CharacterPoseManagedValuePage
    {
        static readonly ProfilerMarker ValueResetMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.ValueResetName);
        static readonly ProfilerMarker PlayerInputMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.PlayerInputName);
        static readonly ProfilerMarker SlotMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.SlotName);
        static readonly ProfilerMarker StateMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.StateName);
        static readonly ProfilerMarker InertializationMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.InertializationName);
        static readonly ProfilerMarker IkGoalMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.IkGoalName);
        static readonly ProfilerMarker LinkedPoseMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.LinkedPoseName);
        static readonly ProfilerMarker FullBodyIkMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.FullBodyIkName);
        static readonly ProfilerMarker OutputMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.OutputName);
        static readonly ProfilerMarker ValueValidationMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.ValueValidationName);
        readonly CharacterPoseConstraintOperationModule
            m_ConstraintOperations;
        readonly CharacterPoseLinkedOperationModule m_LinkedOperations;
        readonly CharacterPoseOutputOperationModule m_OutputOperations;
        readonly CharacterPoseInertializationOperationModule
            m_InertializationOperations;
        readonly CharacterPosePlayerOperationModule m_PlayerOperations;
        readonly CharacterPoseStateOperationModule m_StateOperations;
        readonly CharacterPoseProgramExecutionView m_Program;
        readonly CharacterPoseProgramFramePages m_FramePages;
        readonly PoseInertializationNativeProgram m_InertializationProgram;
        readonly CharacterPoseConstraintRuntime m_PoseConstraints;
        readonly NativeArray<CharacterPoseNativeOperationHeader>
            m_OperationHeaders;
        readonly NativeArray<float> m_ParameterDefaults;
        readonly NativeArray<CharacterPoseNativePlayerOperation>
            m_PlayerFamilyOperations;
        readonly NativeArray<CharacterPoseNativeStateMachineOperation>
            m_StateMachineFamilyOperations;
        readonly NativeArray<CharacterPoseNativeAnimationSlotOperation>
            m_AnimationSlotFamilyOperations;
        readonly NativeArray<CharacterPoseNativeBlendOperation>
            m_BlendFamilyOperations;
        readonly NativeArray<CharacterPoseNativeInertializationOperation>
            m_InertializationFamilyOperations;
        readonly NativeArray<CharacterPoseNativeGoalContributionOperation>
            m_GoalContributionOperations;
        readonly NativeArray<CharacterPoseNativeGoalAssemblerOperation>
            m_GoalAssemblerOperations;
        readonly NativeArray<CharacterPoseNativeFullBodyIkOperation>
            m_FullBodyIkOperations;
        readonly NativeArray<CharacterPoseNativeLinkedPoseOperation>
            m_LinkedPoseOperations;
        readonly NativeArray<CharacterPoseNativeOutputOperation>
            m_OutputFamilyOperations;
        NativeArray<float> m_OperationWeights;
        readonly NativeArray<AnimationPoseGraphNativeStage> m_Stages;
        NativeArray<CharacterRootOrientationWarpNativeControl>
            m_RootOrientationWarpControls;
        NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
            m_LinkedPoseCallControls;
        NativeArray<byte> m_LinkedPoseActiveFragments;
        NativeArray<CharacterPoseStateMachineNativeControl>
            m_StateMachineControls;
        NativeArray<CharacterAnimationSlotNativeControl>
            m_AnimationSlotControls;
        CharacterPoseOperationCompletionPage m_OperationCompletions;
        NativeArray<ulong> m_StageCompletedAt;
        NativeArray<int> m_StageInvalidOperationIndex;
        NativeArray<AnimationPoseNativeInvalidReason>
            m_PoseGraphInvalidReason;
        NativeArray<int> m_PoseGraphInvalidOperationIndex;
        NativeArray<ulong> m_PoseGraphCompletedAt;
        CharacterPoseGraphNativeBinding m_FrameBinding;
        CharacterFinalPosePublicationOutputBinding m_FinalOutput;
        readonly int m_OutputOperationIndex;
        ulong m_FrameSequence;

        internal CharacterPoseProgramExecutor(
            CharacterPoseProgramExecutionView program,
            CharacterPoseProgramFramePages framePages,
            PoseInertializationNativeProgram inertializationProgram,
            CharacterPoseConstraintRuntime poseConstraints)
        {
            m_Program = program ??
                throw new ArgumentNullException(nameof(program));
            m_FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            m_InertializationProgram = inertializationProgram ??
                throw new ArgumentNullException(nameof(inertializationProgram));
            m_PoseConstraints = poseConstraints ??
                throw new ArgumentNullException(nameof(poseConstraints));
            program.RequireValid();
            framePages.RequireValid();

            m_OperationHeaders = program.OperationHeaders;
            m_PlayerFamilyOperations = program.PlayerOperations;
            m_StateMachineFamilyOperations = program.StateMachineOperations;
            m_AnimationSlotFamilyOperations = program.AnimationSlotOperations;
            m_BlendFamilyOperations = program.BlendOperations;
            m_InertializationFamilyOperations = program.InertializationOperations;
            m_GoalContributionOperations = program.GoalContributionOperations;
            m_GoalAssemblerOperations = program.GoalAssemblerOperations;
            m_FullBodyIkOperations = program.FullBodyIkOperations;
            m_LinkedPoseOperations = program.LinkedPoseOperations;
            m_OutputFamilyOperations = program.OutputOperations;
            m_Stages = program.Stages;
            m_ParameterDefaults = program.ParameterDefaults;
            AnimationPoseNativeAggregateLayout layout =
                framePages.EvaluationLayout;
            m_BoneCount = layout.BoneCount;
            m_ParameterCount = layout.ParameterCount;
            m_PoseValueCount = layout.PoseValueCount;
            m_ContributionStride = layout.PoseValueContributionStride;
            m_OutputOperationIndex = program.OutputOperationIndex;
            m_LeftFootBoneIndex = program.LeftFootBoneIndex;
            m_RightFootBoneIndex = program.RightFootBoneIndex;
            m_FrameSequence = 0;
            m_ConstraintOperations =
                new CharacterPoseConstraintOperationModule(
                    m_PoseConstraints);
            m_LinkedOperations =
                new CharacterPoseLinkedOperationModule(
                    program.LinkedPoseCalls,
                    program.LinkedPoseCandidates);
            m_OutputOperations =
                new CharacterPoseOutputOperationModule();
            m_InertializationOperations =
                new CharacterPoseInertializationOperationModule(
                    this,
                    m_InertializationProgram);
            m_PlayerOperations =
                new CharacterPosePlayerOperationModule(
                    this,
                    m_InertializationOperations);
            m_StateOperations =
                new CharacterPoseStateOperationModule(
                    program.BlendCurves,
                    program.BlendCurveSegments,
                    program.BlendProfiles,
                    program.BlendDenseProfiles,
                    m_ParameterDefaults);
        }

        internal CharacterPoseProgramExecutor BindFrame(
            in CharacterPoseProgramTuningView tuning,
            CharacterPoseGraphNativeBinding binding,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            bool recordDiagnostics)
        {
            CharacterPoseProgramExecutorConfiguration.Require(
                m_Program,
                m_FramePages,
                in tuning,
                m_InertializationProgram,
                binding,
                in finalOutput,
                m_PoseConstraints);
            m_OperationWeights = tuning.OperationWeights;
            m_RootOrientationWarpControls =
                m_FramePages.RootOrientationWarpControls;
            m_LinkedPoseCallControls =
                m_FramePages.LinkedPoseCallControls;
            m_LinkedPoseActiveFragments =
                m_FramePages.LinkedPoseActiveFragments;
            m_StateMachineControls = m_FramePages.StateMachineControls;
            m_AnimationSlotControls = m_FramePages.AnimationSlotControls;

            m_ValueDenseLocalPoses = binding.ValueDenseLocalPoses;
            m_ValueDenseVelocities = binding.ValueDenseVelocities;
            m_ValuePoseParameters = binding.ValuePoseParameters;
            m_ValuePoseParameterAvailability =
                binding.ValuePoseParameterAvailability;
            m_ValueContributions = binding.ValueContributions;
            m_ValueDenseContributionWeights =
                binding.ValueDenseContributionWeights;
            m_ValueContributionCounts = binding.ValueContributionCounts;
            m_ValueOutputWeights = binding.ValueOutputWeights;
            m_ValueLeftFootFeatures = binding.ValueLeftFootFeatures;
            m_ValueRightFootFeatures = binding.ValueRightFootFeatures;
            m_ValueHasFootFeatures = binding.ValueHasFootFeatures;
            m_ValueAvailability = binding.ValueAvailability;
            m_ValueContinuityIdentities =
                binding.ValueContinuityIdentities;
            m_ValueDiscontinuities = binding.ValueDiscontinuities;
            m_ValueInvalidReasons = binding.ValueInvalidReasons;
            m_OperationCompletions = binding.OperationCompletions;
            m_StageCompletedAt = binding.StageCompletedAt;
            m_StageInvalidOperationIndex =
                binding.StageInvalidOperationIndex;
            m_PoseGraphInvalidReason = binding.PoseGraphInvalidReason;
            m_PoseGraphInvalidOperationIndex =
                binding.PoseGraphInvalidOperationIndex;
            m_PoseGraphCompletedAt = binding.PoseGraphCompletedAt;
            m_FrameBinding = binding;
            m_ValuePage = CharacterPoseValuePageSlice.Create(
                in binding,
                m_Program.ValueProducerOperationIndices,
                m_LeftFootBoneIndex,
                m_RightFootBoneIndex,
                true);
            m_FinalOutput = finalOutput;
            m_CompletionIdentity = binding.CompletionIdentity;
            m_FrameSequence = 0;
            m_ConstraintOperations.BindFrame(
                in m_ValuePage,
                binding);
            m_LinkedOperations.BindFrame(
                in m_ValuePage,
                m_LinkedPoseCallControls,
                m_LinkedPoseActiveFragments);
            m_StateOperations.BindFrame(
                in m_ValuePage,
                m_StateMachineControls);
            m_InertializationOperations.BindFrame(
                m_StateMachineControls,
                m_AnimationSlotControls);
            m_PlayerOperations.BindFrame(
                binding,
                m_AnimationSlotControls);
            m_OutputOperations.BindFrame(
                in m_ValuePage,
                binding,
                in finalOutput);
            return this;
        }

        internal void BeginEvaluation(ulong frameSequence)
        {
            if (frameSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(frameSequence));
            m_FrameSequence = frameSequence;
            m_ConstraintOperations.BeginEvaluation(frameSequence);
            m_OperationCompletions.Clear();
            m_PoseGraphInvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_PoseGraphInvalidOperationIndex[0] = -1;
            m_PoseGraphCompletedAt[0] = 0;
        }

        internal CharacterPoseWorkerStageLease CreateWorkerStageLease(
            CharacterPoseWorkerActorRegistration registration,
            ActorId actorId,
            int stageIndex)
        {
            if (registration?.IsValid != true || !actorId.IsValid ||
                (uint)stageIndex >= (uint)m_Stages.Length)
            {
                throw new ArgumentException(
                    "Pose Worker Stage lease input is invalid.");
            }
            AnimationPoseGraphNativeStage stage = m_Stages[stageIndex];
            if (!CharacterPoseWorkerKernels.IsWorkerDomain(
                    stage.ExecutionDomain) ||
                stageIndex > 0 &&
                m_StageCompletedAt[m_Stages[stageIndex - 1].CompletionIndex] !=
                m_CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Pose Worker Stage #{stageIndex} cannot be submitted outside compiled order.");
            }
            return new CharacterPoseWorkerStageLease(
                registration,
                m_Program,
                this,
                actorId,
                stageIndex,
                in m_FrameBinding,
                m_OperationWeights,
                m_RootOrientationWarpControls,
                m_LinkedPoseActiveFragments);
        }

        internal void CompleteWorkerStage(
            int stageIndex,
            ulong completionIdentity)
        {
            if ((uint)stageIndex >= (uint)m_Stages.Length ||
                completionIdentity != m_CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Pose Worker Stage completion is stale.");
            }
            AnimationPoseGraphNativeStage stage = m_Stages[stageIndex];
            if (!CharacterPoseWorkerKernels.IsWorkerDomain(
                    stage.ExecutionDomain) ||
                m_StageCompletedAt[stage.CompletionIndex] != 0)
            {
                throw new InvalidOperationException(
                    $"Pose Worker Stage #{stageIndex} completion is invalid.");
            }
            int failedOperation = -1;
            AnimationPoseNativeInvalidReason failureReason =
                AnimationPoseNativeInvalidReason.None;
            for (int nativeIndex = stage.OperationStart;
                 nativeIndex < stage.OperationStart + stage.OperationCount;
                 nativeIndex++)
            {
                CharacterPoseNativeOperationHeader operation =
                    m_OperationHeaders[nativeIndex];
                CharacterPoseOperationCompletion completion =
                    m_OperationCompletions[operation.FrameCacheIndex];
                if (!completion.Matches(completionIdentity))
                {
                    failedOperation = operation.Index;
                    failureReason = AnimationPoseNativeInvalidReason
                        .PoseGraphOperationInvalid;
                    break;
                }
                if (completion.Outcome ==
                    CharacterPoseOperationOutcome.TypedInvalid)
                {
                    failedOperation = operation.Index;
                    failureReason = operation.OutputPoseValueIndex >= 0
                        ? CharacterPosePureMath.NormalizeInvalidReason(
                            m_ValueInvalidReasons[
                                operation.OutputPoseValueIndex])
                        : AnimationPoseNativeInvalidReason
                            .PoseGraphOperationInvalid;
                    break;
                }
                if (completion.Outcome == CharacterPoseOperationOutcome.Skipped ||
                    operation.OutputPoseValueIndex < 0 ||
                    m_ValueAvailability[operation.OutputPoseValueIndex] !=
                    AnimationPoseAvailability.Invalid)
                {
                    continue;
                }
                RecordGraphInvalid(
                    CharacterPosePureMath.NormalizeInvalidReason(
                        m_ValueInvalidReasons[
                            operation.OutputPoseValueIndex]),
                    operation.Index);
            }
            if (failedOperation >= 0)
            {
                RecordGraphInvalid(failureReason, failedOperation);
                m_StageInvalidOperationIndex[stage.DiagnosticIndex] =
                    failedOperation;
                return;
            }
            m_StageCompletedAt[stage.CompletionIndex] = completionIdentity;
        }

        internal bool ExecuteStage(
            int stageIndex,
            float deltaSeconds,
            in CharacterPoseWorldFrameInput worldInput)
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
            if (CharacterPoseWorkerKernels.IsWorkerDomain(
                    stage.ExecutionDomain))
            {
                throw new InvalidOperationException(
                    $"Pose Worker Stage #{stageIndex} cannot execute on the Managed path.");
            }
            bool stop = false;
            for (int operationIndex = stage.OperationStart;
                 operationIndex < stage.OperationStart + stage.OperationCount;
                operationIndex++)
            {
                CharacterPoseNativeOperationHeader operation =
                    m_OperationHeaders[operationIndex].WithWeight(
                        m_OperationWeights[operationIndex]);
                if (!m_OperationCompletions[operation.FrameCacheIndex].IsEmpty)
                {
                    RecordDuplicateOperation(
                        operation.Index,
                        stage.DiagnosticIndex);
                    return false;
                }
                bool producesPose = operation.OutputPoseValueIndex >= 0;
                bool publishesFinalPose =
                    operation.Code == CharacterPoseOperationCode.OutputPose;
                if (operation.LinkedPoseFragmentIndex >= 0 &&
                    !m_LinkedOperations.IsFragmentActive(
                        operation.LinkedPoseFragmentIndex))
                {
                    if (producesPose && !publishesFinalPose)
                    {
                        using (ValueResetMarker.Auto())
                            m_ValuePage.ResetValue(
                                operation.OutputPoseValueIndex,
                                m_ParameterDefaults);
                    }
                    if (!TryCompleteOperation(
                            in operation,
                            CharacterPoseOperationOutcome.Skipped,
                            stage.DiagnosticIndex))
                    {
                        return false;
                    }
                    continue;
                }
                if (producesPose && !publishesFinalPose)
                {
                    using (ValueResetMarker.Auto())
                    m_ValuePage.ResetValue(
                        operation.OutputPoseValueIndex,
                        m_ParameterDefaults);
                }
                bool valueOperationValid = true;
                AnimationPoseNativeInvalidReason typedInvalidReason =
                    AnimationPoseNativeInvalidReason.None;
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.SelectedPosePlayer:
                    case CharacterPoseOperationCode.BlendSpacePlayer:
                    case CharacterPoseOperationCode.ClipPlayer:
                        using (PlayerInputMarker.Auto())
                            m_PlayerOperations.EvaluatePlayerInput(
                                in operation,
                                m_PlayerFamilyOperations[
                                    operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.BlendStack:
                        using (PlayerInputMarker.Auto())
                            m_PlayerOperations.EvaluatePlayerInput(
                                in operation,
                                m_BlendFamilyOperations[
                                    operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.AnimationSlot:
                        using (SlotMarker.Auto())
                            m_PlayerOperations.EvaluateAnimationSlot(
                                in operation,
                                m_AnimationSlotFamilyOperations[
                                    operation.FamilyPayloadIndex],
                                deltaSeconds);
                        break;
                    case CharacterPoseOperationCode.StatePoseOutput:
                        using (StateMarker.Auto())
                            m_StateOperations.EvaluateStatePoseOutput(
                                in operation,
                                m_StateMachineFamilyOperations[
                                    operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.PoseStateMachine:
                        using (StateMarker.Auto())
                            m_StateOperations.EvaluatePoseStateMachine(
                                in operation,
                                m_StateMachineFamilyOperations[
                                    operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.Inertialization:
                        using (InertializationMarker.Auto())
                            m_InertializationOperations
                                .EvaluateInertialization(
                                    in operation,
                                    m_InertializationFamilyOperations[
                                        operation.FamilyPayloadIndex],
                                    deltaSeconds);
                        break;
                    case CharacterPoseOperationCode.PoseBoneIKGoals:
                        using (IkGoalMarker.Auto())
                            valueOperationValid = m_ConstraintOperations
                                .EvaluatePoseBoneIkGoals(
                                    in operation,
                                    m_GoalContributionOperations[
                                        operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.FootPlacement:
                        using (IkGoalMarker.Auto())
                            valueOperationValid = m_ConstraintOperations
                                .EvaluateWorldAwareFootGoal(
                                    in operation,
                                    m_GoalContributionOperations[
                                        operation.FamilyPayloadIndex],
                                    in worldInput,
                                    out typedInvalidReason);
                        break;
                    case CharacterPoseOperationCode.FullBodyIkGoalAssembler:
                        using (IkGoalMarker.Auto())
                            valueOperationValid = m_ConstraintOperations
                                .EvaluateGoalAssembler(
                                    m_GoalAssemblerOperations[
                                        operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.FullBodyIK:
                        using (FullBodyIkMarker.Auto())
                            m_ConstraintOperations.EvaluateFullBodyIk(
                                in operation,
                                m_FullBodyIkOperations[
                                    operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.LinkedPoseCall:
                        using (LinkedPoseMarker.Auto())
                            valueOperationValid = m_LinkedOperations
                                .EvaluateLinkedPoseCall(
                                    in operation,
                                    m_LinkedPoseOperations[
                                        operation.FamilyPayloadIndex]);
                        break;
                    case CharacterPoseOperationCode.OutputPose:
                        using (OutputMarker.Auto())
                            m_OutputOperations.EvaluateOutputPose(
                                in operation,
                                m_OutputFamilyOperations[
                                    operation.FamilyPayloadIndex]);
                        break;
                    default:
                        if (producesPose)
                        {
                            SetInvalid(
                                operation.OutputPoseValueIndex,
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
                    : typedInvalidReason !=
                      AnimationPoseNativeInvalidReason.None
                        ? typedInvalidReason
                        : ValueOperationInvalidReason(operation);
                if (producesPose)
                {
                    using (ValueValidationMarker.Auto())
                    {
                        valueValid = publishesFinalPose
                            ? m_FinalOutput.HasOutput
                            : m_ValuePage.TryValidateValueEnvelope(
                                operation.OutputPoseValueIndex,
                                out reason);
                    }
                }
                if (!valueValid)
                {
                    if (producesPose)
                    {
                        if (publishesFinalPose)
                        {
                            throw new InvalidOperationException(
                                "Final Pose Output operation did not publish its actor-local result.");
                        }
                        SetInvalid(
                            operation.OutputPoseValueIndex,
                            m_ValueContinuityIdentities[operation.OutputPoseValueIndex],
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
                if (!TryCompleteOperation(
                        in operation,
                        valueValid
                            ? CharacterPoseOperationOutcome.Completed
                            : CharacterPoseOperationOutcome.TypedInvalid,
                        stage.DiagnosticIndex))
                {
                    return false;
                }
                if (stop)
                    break;
            }
            if (!stop)
                m_StageCompletedAt[stage.CompletionIndex] = m_CompletionIdentity;
            return !stop;
        }

        bool TryCompleteOperation(
            in CharacterPoseNativeOperationHeader operation,
            CharacterPoseOperationOutcome outcome,
            int stageDiagnosticIndex)
        {
            var completion = new CharacterPoseOperationCompletion(
                m_CompletionIdentity,
                outcome);
            if (m_OperationCompletions.TryComplete(
                    operation.FrameCacheIndex,
                    in completion))
            {
                return true;
            }

            RecordDuplicateOperation(operation.Index, stageDiagnosticIndex);
            return false;
        }

        void RecordDuplicateOperation(
            int operationIndex,
            int stageDiagnosticIndex)
        {
            m_PoseGraphInvalidReason[0] =
                AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
            m_PoseGraphInvalidOperationIndex[0] = operationIndex;
            if ((uint)stageDiagnosticIndex <
                (uint)m_StageInvalidOperationIndex.Length)
            {
                m_StageInvalidOperationIndex[stageDiagnosticIndex] =
                    operationIndex;
            }
        }

        internal CharacterPoseProgramOutputResult ExecuteSequencePreview(
            int sourceOperationIndex)
        {
            CharacterPoseNativeOperationHeader sourceOperation = default;
            bool found = false;
            for (int i = 0; i < m_OperationHeaders.Length; i++)
            {
                if (m_OperationHeaders[i].Index != sourceOperationIndex)
                    continue;
                sourceOperation = m_OperationHeaders[i];
                found = true;
                break;
            }
            if (!found || sourceOperation.Code != CharacterPoseOperationCode.ClipPlayer)
                throw new InvalidOperationException(
                    $"Clip Preview source operation #{sourceOperationIndex} is not a compiled Clip Player.");

            m_ValuePage.ResetValue(
                sourceOperation.OutputPoseValueIndex,
                m_ParameterDefaults);
            m_PlayerOperations.EvaluatePlayerInput(
                in sourceOperation,
                m_PlayerFamilyOperations[sourceOperation.FamilyPayloadIndex]);
            if (!TryCompleteOperation(
                    in sourceOperation,
                    m_ValueAvailability[sourceOperation.OutputPoseValueIndex] ==
                    AnimationPoseAvailability.Pose
                        ? CharacterPoseOperationOutcome.Completed
                        : CharacterPoseOperationOutcome.TypedInvalid,
                    -1))
            {
                return CompleteEvaluation();
            }
            int sourceValue = sourceOperation.OutputPoseValueIndex;
            ulong continuity = CharacterPosePureMath.CombineContinuity(
                m_ValueContinuityIdentities[sourceValue],
                (ulong)sourceOperation.Index + 1UL,
                sourceOperation.Index);
            AnimationPoseNativeInvalidReason previewReason =
                AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid;
            if (m_ValueAvailability[sourceValue] ==
                    AnimationPoseAvailability.Pose &&
                TryValidateValueDeep(
                    sourceValue,
                    out previewReason))
            {
                var input = new AnimationPoseValueNativeReadBinding(
                    in m_FrameBinding,
                    sourceValue);
                m_FinalOutput.WritePose(
                    in input,
                    m_ValueOutputWeights[sourceValue],
                    continuity);
            }
            else
            {
                AnimationPoseNativeInvalidReason reason =
                    m_ValueAvailability[sourceValue] ==
                    AnimationPoseAvailability.Invalid
                        ? CharacterPosePureMath.NormalizeInvalidReason(
                            m_ValueInvalidReasons[sourceValue])
                        : m_ValueAvailability[sourceValue] ==
                          AnimationPoseAvailability.NoPose
                            ? AnimationPoseNativeInvalidReason
                                .PoseGraphOutputInvalid
                            : CharacterPosePureMath.NormalizeInvalidReason(previewReason);
                RecordGraphInvalid(reason, sourceOperation.Index);
                m_FinalOutput.WriteInvalid(reason, continuity);
            }
            for (int i = 0; i < m_OperationCompletions.Count; i++)
            {
                if (!m_OperationCompletions[i].IsEmpty)
                    continue;
                CharacterPoseNativeOperationHeader operation =
                    m_OperationHeaders[i];
                if (!TryCompleteOperation(
                        in operation,
                        CharacterPoseOperationOutcome.Skipped,
                        -1))
                {
                    return CompleteEvaluation();
                }
            }
            for (int i = 0; i < m_StageCompletedAt.Length; i++)
                m_StageCompletedAt[i] = m_CompletionIdentity;
            return CompleteEvaluation();
        }

        internal CharacterPoseProgramOutputResult CompleteEvaluation()
        {
            if (!m_FinalOutput.HasOutput)
            {
                AnimationPoseNativeInvalidReason reason =
                    m_PoseGraphInvalidReason[0] ==
                    AnimationPoseNativeInvalidReason.None
                        ? AnimationPoseNativeInvalidReason
                            .PoseGraphOutputInvalid
                        : CharacterPosePureMath.NormalizeInvalidReason(
                            m_PoseGraphInvalidReason[0]);
                int operationIndex =
                    m_PoseGraphInvalidOperationIndex[0] >= 0
                        ? m_PoseGraphInvalidOperationIndex[0]
                        : m_OutputOperationIndex;
                RecordGraphInvalid(reason, operationIndex);
                m_FinalOutput.WriteInvalid(
                    reason,
                    (ulong)m_OutputOperationIndex + 1UL);
            }
            m_PoseGraphCompletedAt[0] = m_CompletionIdentity;
            return m_FinalOutput.Complete(
                m_PoseGraphInvalidReason[0],
                m_PoseGraphInvalidOperationIndex[0]);
        }

        AnimationPoseNativeInvalidReason ValueOperationInvalidReason(
            CharacterPoseNativeOperationHeader operation)
        {
            return operation.Code == CharacterPoseOperationCode.FootPlacement
                ? AnimationPoseNativeInvalidReason.FootPlacementInvalid
                : operation.Code == CharacterPoseOperationCode.PoseBoneIKGoals ||
                  operation.Code == CharacterPoseOperationCode.FullBodyIkGoalAssembler
                    ? AnimationPoseNativeInvalidReason.FullBodyIkGoalSetInvalid
                    : AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
        }

    }
}
