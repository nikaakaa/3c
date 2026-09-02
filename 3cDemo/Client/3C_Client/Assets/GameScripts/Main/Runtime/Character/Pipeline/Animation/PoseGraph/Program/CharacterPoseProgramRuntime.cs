using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramRuntime : IDisposable
    {
        readonly struct PreparedEvaluationState
        {
            internal PreparedEvaluationState(
                in CharacterPoseFrameLineage lineage,
                float presentationDeltaSeconds,
                in CharacterPoseGraphNativeBinding frame)
            {
                Lineage = lineage;
                PresentationDeltaSeconds = presentationDeltaSeconds;
                Frame = frame;
            }

            internal CharacterPoseFrameLineage Lineage { get; }
            internal float PresentationDeltaSeconds { get; }
            internal CharacterPoseGraphNativeBinding Frame { get; }
            internal bool IsValid =>
                Lineage.IsValid &&
                float.IsFinite(PresentationDeltaSeconds) &&
                PresentationDeltaSeconds >= 0f &&
                Frame.CompletionIdentity == Lineage.CompletionIdentity;
        }

        struct PreparedEvaluationPage
        {
            CharacterPoseFrameLineage m_Lineage;
            CharacterPoseGraphNativeBinding m_Frame;
            float m_PresentationDeltaSeconds;
            bool m_HasValue;

            internal bool HasValue => m_HasValue;

            internal float RequireDeltaSeconds(
                in CharacterPoseProgramPrepared prepared)
            {
                if (!m_HasValue ||
                    !prepared.IsValid ||
                    prepared.Lineage != m_Lineage)
                {
                    throw new ArgumentException(
                        "Pose Program prepared page does not match the requested frame.",
                        nameof(prepared));
                }
                return m_PresentationDeltaSeconds;
            }

            internal void Prepare(
                in CharacterPoseProgramPrepared prepared,
                float presentationDeltaSeconds,
                in CharacterPoseGraphNativeBinding frame)
            {
                if (m_HasValue)
                {
                    throw new InvalidOperationException(
                        "Pose Program prepared page already contains a frame.");
                }
                if (!prepared.IsValid ||
                    !float.IsFinite(presentationDeltaSeconds) ||
                    presentationDeltaSeconds < 0f ||
                    frame.CompletionIdentity !=
                        prepared.Lineage.CompletionIdentity)
                {
                    throw new ArgumentException(
                        "Pose Program prepared page input is invalid.",
                        nameof(prepared));
                }
                m_Lineage = prepared.Lineage;
                m_PresentationDeltaSeconds = presentationDeltaSeconds;
                m_Frame = frame;
                m_HasValue = true;
            }

            internal PreparedEvaluationState Consume(
                in CharacterPoseProgramPrepared prepared)
            {
                if (!m_HasValue ||
                    !prepared.IsValid ||
                    prepared.Lineage != m_Lineage)
                {
                    throw new ArgumentException(
                        "Pose Program prepared page does not match the requested frame.",
                        nameof(prepared));
                }
                var state = new PreparedEvaluationState(
                    in m_Lineage,
                    m_PresentationDeltaSeconds,
                    in m_Frame);
                Clear();
                if (!state.IsValid)
                {
                    throw new InvalidOperationException(
                        "Pose Program prepared page state is inconsistent.");
                }
                return state;
            }

            internal void Clear()
            {
                m_Lineage = default;
                m_Frame = default;
                m_PresentationDeltaSeconds = 0f;
                m_HasValue = false;
            }
        }

        readonly CharacterPoseProgramImage m_Image;
        readonly CharacterPoseConstraintRuntime m_PoseConstraints;
        readonly CharacterPoseWorldContextAdapter m_WorldContext;
        readonly int m_FootPlacementWeightParameterIndex;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        PreparedEvaluationPage m_PreparedEvaluation;
        CharacterPoseGraphNativeBinding m_CommittedEvaluationFrame;
        CharacterPoseGraphNativeBinding m_PendingCompletedEvaluationFrame;
        bool m_HasCommittedEvaluationFrame;
        bool m_HasPendingCompletedEvaluationFrame;
        bool m_Disposed;

        internal CharacterPoseProgramRuntime(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramTuningState tuning,
            CharacterPoseWorldContextAdapter worldContext,
            CharacterPoseConstraintRuntime poseConstraints)
        {
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            ExecutionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            m_WorldContext = worldContext ??
                throw new ArgumentNullException(nameof(worldContext));
            m_PoseConstraints = poseConstraints ??
                throw new ArgumentNullException(nameof(poseConstraints));
            m_FootPlacementWeightParameterIndex =
                image.RequireParameterIndex(
                    AnimationPoseParameterIds.FootPlacementWeight);
            Executor = new CharacterPoseGraphStagedExecutor(
                ExecutionView,
                FramePages,
                ActorState.Inertialization,
                m_PoseConstraints);
            if (!string.Equals(
                    m_Image.ProgramId,
                    ExecutionView.ProgramId.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.ProjectionRevision,
                    ExecutionView.ProjectionRevision.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.PoseProgramImageHash,
                    ExecutionView.PoseProgramImageHash.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.RigId,
                    ExecutionView.RigId.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.RigRevision,
                    ExecutionView.RigRevision.ToString(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pose Program Runtime inputs do not share one Image identity.");
            }
        }

        internal CharacterPoseProgramImage Image => m_Image;
        internal CharacterPoseProgramExecutionView ExecutionView { get; }
        internal CharacterPoseActorState ActorState { get; }
        CharacterPoseProgramFramePages FramePages { get; }
        CharacterPoseProgramTuningState Tuning { get; }
        CharacterPoseGraphStagedExecutor Executor { get; }
        internal bool HasOpenFrame => m_ActiveFrameLease.IsValid;
        internal bool HasPreparedEvaluation => m_PreparedEvaluation.HasValue;
        internal bool HasPendingEvaluationFrame =>
            FramePages.HasPendingEvaluationFrame;
        internal ulong PendingEvaluationCompletionIdentity =>
            FramePages.PendingEvaluationCompletionIdentity;
        internal long DenseDoublePageResidentPayloadBytes =>
            FramePages.DenseDoublePageResidentPayloadBytes;
        internal bool HasCommittedEvaluationFrame =>
            m_HasCommittedEvaluationFrame;
        internal bool HasPendingCompletedEvaluationFrame =>
            m_HasPendingCompletedEvaluationFrame;
        internal ulong CommittedEvaluationCompletionIdentity =>
            m_HasCommittedEvaluationFrame
                ? m_CommittedEvaluationFrame.CompletionIdentity
                : 0;
        internal ulong PendingCompletedEvaluationCompletionIdentity =>
            m_HasPendingCompletedEvaluationFrame
                ? m_PendingCompletedEvaluationFrame.CompletionIdentity
                : 0;

        internal void BeginFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid || m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame cannot begin.");
            }
            FramePages.BeginFrame();
            m_ActiveFrameLease = lease;
        }

        internal void CommitFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            FramePages.CommitFrame();
            m_ActiveFrameLease = default;
        }

        internal void DiscardFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            FramePages.DiscardFrame();
            m_ActiveFrameLease = default;
        }

        internal void BeginEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (m_PreparedEvaluation.HasValue ||
                m_HasPendingCompletedEvaluationFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has an unfinished evaluation.");
            }
            FramePages.BeginEvaluationFrame(completionIdentity);
        }

        internal void CommitEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (!m_HasPendingCompletedEvaluationFrame ||
                m_PendingCompletedEvaluationFrame.CompletionIdentity !=
                    completionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no completed evaluation to commit.");
            }
            FramePages.CommitEvaluationFrame(completionIdentity);
            m_CommittedEvaluationFrame = m_PendingCompletedEvaluationFrame;
            m_HasCommittedEvaluationFrame = true;
            m_PendingCompletedEvaluationFrame = default;
            m_HasPendingCompletedEvaluationFrame = false;
        }

        internal void DiscardEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            FramePages.DiscardEvaluationFrame(completionIdentity);
            m_PreparedEvaluation.Clear();
            m_PendingCompletedEvaluationFrame = default;
            m_HasPendingCompletedEvaluationFrame = false;
        }

        internal CharacterPoseSourcePreparationView BeginSourceDemand(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            return FramePages.BeginSourceDemand(completionIdentity);
        }

        internal void BindSourceDemand(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourceDemand demand)
        {
            RequireFrame(lease);
            if (!lease.Matches(demand.Lineage))
            {
                throw new ArgumentException(
                    "Character Pose Program source demand lineage is invalid.",
                    nameof(demand));
            }
            FramePages.BindSourceDemand(in demand);
        }

        internal CharacterPoseSourceDemand RequireSourceDemand(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourceDemand demand)
        {
            RequireFrame(lease);
            return FramePages.RequireSourceDemand(in demand);
        }

        internal int AddSourcePreparation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourcePreparation preparation)
        {
            RequireFrame(lease);
            return FramePages.AddSourcePreparation(in preparation);
        }

        internal void ClearSourceDemand() => FramePages.ClearSourceDemand();

        internal void SetAnimationSlotControl(
            CharacterPoseProgramFrameLease lease,
            int animationSlotIndex,
            in CharacterAnimationSlotNativeControl control)
        {
            RequireFrame(lease);
            FramePages.SetAnimationSlotControl(
                animationSlotIndex,
                in control);
        }

        internal void SetRootOrientationWarpControl(
            CharacterPoseProgramFrameLease lease,
            int rootOrientationWarpIndex,
            in CharacterRootOrientationWarpNativeControl control)
        {
            RequireFrame(lease);
            FramePages.SetRootOrientationWarpControl(
                rootOrientationWarpIndex,
                in control);
        }

        internal void ResetRootOrientationWarpControl(
            int rootOrientationWarpIndex,
            in CharacterRootOrientationWarpNativeControl control)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Root Orientation Warp cannot reset during a frame.");
            }
            FramePages.SetRootOrientationWarpControl(
                rootOrientationWarpIndex,
                in control);
        }

        internal void EvaluateTransitions(
            CharacterPoseProgramFrameLease lease,
            in CharacterPresentationFactFrame factFrame,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireFrame(lease);
            ActorState.PoseStateSources.EvaluateTransitions(
                in factFrame,
                FramePages,
                workspace,
                workspaceLease);
        }

        internal AnimationPlayerPoseNativeWriteBinding
            RequirePlayerWriteBinding(
                CharacterPoseProgramFrameLease lease,
                int physicalSlotIndex,
                ulong completionIdentity)
        {
            RequireFrame(lease);
            return FramePages.RequirePlayerWriteBinding(
                physicalSlotIndex,
                completionIdentity);
        }

        internal void BindEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            ulong tuningGeneration,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            bool recordDiagnostics)
        {
            RequireFrame(lease);
            if (!lineage.IsValid ||
                !lease.Matches(lineage) ||
                !FramePages.HasPendingEvaluationFrame ||
                FramePages.PendingEvaluationCompletionIdentity !=
                    lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program evaluation binding is invalid.",
                    nameof(lineage));
            }
            CharacterPoseGraphNativeBinding frame =
                FramePages.RequirePoseGraphBinding(
                    lineage.CompletionIdentity);
            CharacterPoseProgramTuningView tuning =
                Tuning.RequireCommitted(tuningGeneration);
            Executor.BindFrame(
                in tuning,
                frame,
                in finalOutput,
                recordDiagnostics);
        }

        internal void PrepareEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            float presentationDeltaSeconds)
        {
            RequireFrame(lease);
            if (!prepared.IsValid ||
                !lease.Matches(prepared.Lineage) ||
                !FramePages.HasPendingEvaluationFrame ||
                FramePages.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program prepared evaluation is invalid.",
                    nameof(prepared));
            }
            CharacterPoseGraphNativeBinding frame =
                FramePages.RequirePoseGraphBinding(
                    prepared.Lineage.CompletionIdentity);
            m_PreparedEvaluation.Prepare(
                in prepared,
                presentationDeltaSeconds,
                in frame);
        }

        internal CharacterPoseProgramOutputResult CompleteEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            bool sequencePreview,
            int sequencePreviewOperationIndex)
        {
            RequireFrame(lease);
            if (!prepared.IsValid ||
                !lease.Matches(prepared.Lineage) ||
                !bodyFrame.IsValid ||
                !factFrame.IsValid ||
                !FramePages.HasPendingEvaluationFrame ||
                FramePages.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program evaluation input is invalid.",
                    nameof(prepared));
            }
            PreparedEvaluationState state =
                m_PreparedEvaluation.Consume(in prepared);
            Executor.BeginStagedEvaluation(
                state.Lineage.PresentationFrame);
            CharacterPoseProgramOutputResult output;
            if (sequencePreview)
            {
                output = Executor.ExecuteSequencePreview(
                    sequencePreviewOperationIndex);
            }
            else
            {
                for (int stageIndex = 0;
                     stageIndex < ExecutionView.Stages.Length;
                     stageIndex++)
                {
                    AnimationPoseGraphNativeStage stage =
                        ExecutionView.Stages[stageIndex];
                    CharacterPoseWorldAwareStageInput worldInput = default;
                    if (stage.ExecutionDomain ==
                        CharacterPoseExecutionDomain.WorldAwareValue)
                    {
                        worldInput = BuildWorldAwareStageInput(
                            in state,
                            in bodyFrame,
                            in factFrame,
                            in stage);
                    }
                    if (!Executor.ExecuteStage(
                            stageIndex,
                            state.PresentationDeltaSeconds,
                            in worldInput))
                    {
                        break;
                    }
                }
                output = Executor.CompleteStagedEvaluation();
            }
            FramePages.RequireEvaluationStagesCompleted(
                state.Lineage.CompletionIdentity);
            return output;
        }

        internal float RequirePreparedEvaluationDeltaSeconds(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared)
        {
            RequireFrame(lease);
            return m_PreparedEvaluation.RequireDeltaSeconds(in prepared);
        }

        internal void MarkEvaluationCompleted(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (m_PreparedEvaluation.HasValue ||
                m_HasPendingCompletedEvaluationFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation completion is invalid.");
            }
            FramePages.RequireEvaluationStagesCompleted(completionIdentity);
            m_PendingCompletedEvaluationFrame =
                FramePages.RequirePoseGraphBinding(completionIdentity);
            m_HasPendingCompletedEvaluationFrame = true;
        }

        internal bool TryCopyPendingPlayerPose(
            int playerIndex,
            int[] rigBoneIndices,
            UnityEngine.Vector3[] positions,
            out AnimationFootPlacementSample footPlacement)
        {
            RequireAlive();
            if (playerIndex < 0 ||
                rigBoneIndices == null || positions == null ||
                rigBoneIndices.Length == 0 ||
                positions.Length != rigBoneIndices.Length)
            {
                throw new ArgumentException(
                    "Animation Player history copy input is invalid.");
            }
            if (!m_HasPendingCompletedEvaluationFrame)
            {
                footPlacement = default;
                return false;
            }
            var read = new AnimationPlayerPoseNativeWriteBinding(
                in m_PendingCompletedEvaluationFrame,
                playerIndex);
            if (read.CompletedAt[0] !=
                    m_PendingCompletedEvaluationFrame.CompletionIdentity ||
                read.Availability[0] != AnimationPoseAvailability.Pose ||
                read.HasFootFeatures[0] == 0 ||
                read.PoseParameterAvailability[
                    m_FootPlacementWeightParameterIndex] == 0)
            {
                footPlacement = default;
                return false;
            }
            for (int i = 0; i < rigBoneIndices.Length; i++)
            {
                int boneIndex = rigBoneIndices[i];
                if ((uint)boneIndex >= (uint)read.DenseLocalPoses.Length)
                {
                    throw new InvalidOperationException(
                        "Motion Matching history Bone index is outside the completed Player pose.");
                }
                positions[i] = read.DenseLocalPoses[boneIndex].Position;
            }
            footPlacement = new AnimationFootPlacementSample(
                read.PoseParameters[m_FootPlacementWeightParameterIndex],
                read.LeftFootFeatures[0],
                read.RightFootFeatures[0]);
            return true;
        }

        internal void ResetEvaluation()
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation cannot reset during a frame.");
            }
            m_PreparedEvaluation.Clear();
            m_CommittedEvaluationFrame = default;
            m_PendingCompletedEvaluationFrame = default;
            m_HasCommittedEvaluationFrame = false;
            m_HasPendingCompletedEvaluationFrame = false;
        }

        internal CharacterPoseProgramTuningView RequireTuning(
            ulong generation) => Tuning.RequireCommitted(generation);

        internal CharacterPoseProgramCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
                CharacterPoseProgramCommittedDiagnosticsProjector projector,
                in CharacterPoseProgramResult result,
                in CharacterFinalPoseCommittedDiagnosticsView finalOutput,
                AnimationPresentationDiagnosticsInterest interest)
        {
            if (projector == null)
                throw new ArgumentNullException(nameof(projector));
            if (!m_HasCommittedEvaluationFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no committed evaluation diagnostics.");
            }
            return projector.Capture(
                FramePages,
                in result,
                in m_CommittedEvaluationFrame,
                in finalOutput,
                interest);
        }

        CharacterPoseWorldAwareStageInput BuildWorldAwareStageInput(
            in PreparedEvaluationState state,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            in AnimationPoseGraphNativeStage stage)
        {
            CharacterPoseWorldAwareStageInput result = default;
            for (int operationIndex = stage.OperationStart;
                 operationIndex < stage.OperationStart + stage.OperationCount;
                 operationIndex++)
            {
                AnimationPoseGraphNativeOperation operation =
                    ExecutionView.Operations[operationIndex];
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.FootPlacement:
                        if (result.HasFootPlacement)
                        {
                            throw new InvalidOperationException(
                                "World-Aware Pose stage contains multiple Foot Placement operations.");
                        }
                        CharacterFootPlacementConstraintHandle constraint =
                            operation.FootPlacementConstraint;
                        if (!m_PoseConstraints.HasFootPlacement)
                        {
                            result = new CharacterPoseWorldAwareStageInput(
                                constraint);
                            break;
                        }
                        AnimationPoseValueNativeReadBinding inputBinding =
                            FramePages.RequirePoseValueReadBinding(
                                operation.InputValueIndexA,
                                state.Lineage.CompletionIdentity);
                        result = m_WorldContext.BuildFootPlacement(
                            in constraint,
                            state.Lineage.ActorId,
                            state.Lineage.PresentationFrame,
                            state.PresentationDeltaSeconds,
                            in bodyFrame,
                            in factFrame,
                            state.Lineage.CompletionIdentity,
                            in inputBinding,
                            operation.ParameterIndex);
                        break;
                }
            }
            if (!result.HasFootPlacement)
            {
                throw new InvalidOperationException(
                    "World-Aware Pose stage has no supported planner operation.");
            }
            return result;
        }

        internal string PrepareTuningCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation)
        {
            try
            {
                Tuning.PrepareCandidate(layout, block, generation);
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                string error = ActorState.PoseStateSources.StateMachines[i]
                    .PrepareTuningCandidate(layout, block);
                if (!string.IsNullOrEmpty(error))
                {
                    DiscardTuningCandidate();
                    return error;
                }
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
            {
                string error = ActorState.Stacks[i].PrepareTuningCandidate(
                    layout,
                    block);
                if (!string.IsNullOrEmpty(error))
                {
                    DiscardTuningCandidate();
                    return error;
                }
            }
            string inertializationError = ActorState.Inertialization
                .PrepareTuningCandidate(layout, block);
            if (!string.IsNullOrEmpty(inertializationError))
            {
                DiscardTuningCandidate();
                return inertializationError;
            }
            return string.Empty;
        }

        internal void CommitTuningCandidate(ulong generation)
        {
            for (int i = 0;
                 i < ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                ActorState.PoseStateSources.StateMachines[i]
                    .CommitTuningCandidate();
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].CommitTuningCandidate();
            ActorState.Inertialization.CommitTuningCandidate();
            Tuning.CommitCandidate(generation);
        }

        internal void DiscardTuningCandidate()
        {
            for (int i = 0;
                 i < ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                ActorState.PoseStateSources.StateMachines[i]
                    .DiscardTuningCandidate();
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].DiscardTuningCandidate();
            ActorState.Inertialization.DiscardTuningCandidate();
            Tuning.DiscardCandidate();
        }

        internal void SetLinkedPoseGroupSelection(
            in CharacterLinkedPoseGenerationHandle selection)
        {
            if (!FramePages.HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame pages are not open.");
            }
            if (!selection.IsValid)
            {
                throw new ArgumentException(
                    "Linked Pose generation selection is invalid.",
                    nameof(selection));
            }
            NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
                controls = FramePages.LinkedPoseCallControls;
            NativeArray<byte> activeFragments =
                FramePages.LinkedPoseActiveFragments;
            int matchingCallCount = 0;
            for (int callIndex = 0;
                 callIndex < ExecutionView.LinkedPoseCalls.Length;
                 callIndex++)
            {
                if (ExecutionView.GetLinkedPoseCallGroupId(callIndex) !=
                    selection.GroupId)
                {
                    continue;
                }
                matchingCallCount++;
                if (ExecutionView.GetLinkedPoseCallInterfaceId(callIndex) !=
                        selection.InterfaceId ||
                    controls[callIndex].IsActive ||
                    ExecutionView.FindLinkedPoseCandidate(
                        callIndex,
                        selection.ImplementationId) < 0)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose Group '{selection.GroupId}' selection does not match call #{callIndex}.");
                }
            }
            if (matchingCallCount == 0)
            {
                throw new InvalidOperationException(
                    $"Linked Pose Group '{selection.GroupId}' has no compiled calls.");
            }
            for (int callIndex = 0;
                 callIndex < ExecutionView.LinkedPoseCalls.Length;
                 callIndex++)
            {
                if (ExecutionView.GetLinkedPoseCallGroupId(callIndex) !=
                    selection.GroupId)
                {
                    continue;
                }
                int candidateIndex =
                    ExecutionView.FindLinkedPoseCandidate(
                        callIndex,
                        selection.ImplementationId);
                AnimationPoseGraphNativeLinkedPoseCandidate candidate =
                    ExecutionView.LinkedPoseCandidates[candidateIndex];
                controls[callIndex] =
                    new AnimationPoseGraphNativeLinkedPoseCallControl(
                        candidateIndex,
                        selection.Generation,
                        selection.PoseDiscontinuity);
                activeFragments[candidate.FragmentIndex] = 1;
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_PreparedEvaluation.Clear();
            m_CommittedEvaluationFrame = default;
            m_PendingCompletedEvaluationFrame = default;
            m_HasCommittedEvaluationFrame = false;
            m_HasPendingCompletedEvaluationFrame = false;
            Exception failure = null;
            DisposeStep(ActorState.Dispose, ref failure);
            DisposeStep(Tuning.Dispose, ref failure);
            DisposeStep(ExecutionView.Dispose, ref failure);
            DisposeStep(FramePages.Dispose, ref failure);
            if (failure != null)
                throw failure;
        }

        void RequireFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                !m_ActiveFrameLease.IsValid ||
                lease.Lineage != m_ActiveFrameLease.Lineage)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame lease is stale.");
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseProgramRuntime));
            }
        }

        static void DisposeStep(Action dispose, ref Exception failure)
        {
            try
            {
                dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }
    }
}
