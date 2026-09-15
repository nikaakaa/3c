using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramEvaluationRuntime
    {
        readonly CharacterPoseProgramExecutionView m_ExecutionView;
        readonly CharacterPoseActorState m_ActorState;
        readonly CharacterPoseProgramFramePages m_FramePages;
        readonly CharacterPoseProgramExecutor m_Executor;
        readonly CharacterPoseWorldContextAdapter m_WorldContext;
        readonly CharacterPoseProgramSourcePreparationRuntime
            m_SourcePreparation;
        readonly CharacterPoseProgramCommittedDiagnosticsProjector
            m_Diagnostics;
        readonly ActorId m_ActorId;
        readonly CharacterPoseWorkerActorRegistration m_WorkerRegistration;
        readonly CharacterPoseProgramEvaluationState m_State =
            new CharacterPoseProgramEvaluationState();
        CharacterPosePreparedEvaluationState m_ExecutingState;
        CharacterPoseWorldFrameInput m_ExecutingWorldInput;
        CharacterPoseProgramOutputResult m_ExecutingOutput;
        int m_NextStageIndex;
        int m_WaitingWorkerStageIndex = -1;
        bool m_ExecutionActive;
        bool m_HasExecutingOutput;

        internal CharacterPoseProgramEvaluationRuntime(
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramExecutor executor,
            CharacterPoseWorldContextAdapter worldContext,
            CharacterPoseProgramSourcePreparationRuntime sourcePreparation,
            CharacterPoseProgramCommittedDiagnosticsProjector diagnostics,
            ActorId actorId,
            CharacterPoseWorkerActorRegistration workerRegistration)
        {
            m_ExecutionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            m_ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            m_FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            m_Executor = executor ??
                throw new ArgumentNullException(nameof(executor));
            m_WorldContext = worldContext ??
                throw new ArgumentNullException(nameof(worldContext));
            m_SourcePreparation = sourcePreparation ??
                throw new ArgumentNullException(nameof(sourcePreparation));
            m_Diagnostics = diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics));
            m_ActorId = actorId.IsValid
                ? actorId
                : throw new ArgumentException(
                    "Pose Program Actor identity is invalid.",
                    nameof(actorId));
            m_WorkerRegistration = workerRegistration ??
                throw new ArgumentNullException(nameof(workerRegistration));
        }

        internal bool HasPrepared => m_State.HasPrepared;
        internal bool HasPendingCompleted => m_State.HasPendingCompleted;
        internal bool HasCommitted => m_State.HasCommitted;
        internal CharacterPoseProgramEvaluationState State => m_State;
        internal ulong CommittedCompletionIdentity =>
            m_State.CommittedCompletionIdentity;
        internal ulong PendingCompletedCompletionIdentity =>
            m_State.PendingCompletedCompletionIdentity;

        internal void BeginFrame(ulong completionIdentity)
        {
            if (m_State.HasPrepared || m_State.HasPendingCompleted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has an unfinished evaluation.");
            }
            m_FramePages.BeginEvaluationFrame(completionIdentity);
        }

        internal void Bind(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            in CharacterPoseProgramTuningView tuning,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            bool recordDiagnostics)
        {
            if (!lineage.IsValid ||
                !lease.Matches(lineage) ||
                !m_FramePages.HasPendingEvaluationFrame ||
                m_FramePages.PendingEvaluationCompletionIdentity !=
                    lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program evaluation binding is invalid.",
                    nameof(lineage));
            }
            CharacterPoseGraphNativeBinding frame =
                m_FramePages.RequirePoseGraphBinding(
                    lineage.CompletionIdentity);
            m_Executor.BindFrame(
                in tuning,
                frame,
                in finalOutput,
                recordDiagnostics);
        }

        internal void Prepare(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            float presentationDeltaSeconds)
        {
            if (!prepared.IsValid ||
                !lease.Matches(prepared.Lineage) ||
                !m_FramePages.HasPendingEvaluationFrame ||
                m_FramePages.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program prepared evaluation is invalid.",
                    nameof(prepared));
            }
            CharacterPoseGraphNativeBinding frame =
                m_FramePages.RequirePoseGraphBinding(
                    prepared.Lineage.CompletionIdentity);
            m_State.Prepare(
                in prepared,
                presentationDeltaSeconds,
                in frame);
        }

        internal void BeginCompletion(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame)
        {
            if (!prepared.IsValid ||
                !lease.Matches(prepared.Lineage) ||
                !bodyFrame.IsValid ||
                !factFrame.IsValid || !parameterFrame.IsValid ||
                !m_FramePages.HasPendingEvaluationFrame ||
                m_FramePages.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program evaluation input is invalid.",
                    nameof(prepared));
            }
            if (m_ExecutionActive || m_HasExecutingOutput)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation is already executing.");
            }
            m_ExecutingState =
                m_State.Consume(in prepared);
            m_Executor.BeginEvaluation(
                m_ExecutingState.Lineage.PresentationFrame);
            m_NextStageIndex = 0;
            m_WaitingWorkerStageIndex = -1;
            if (m_SourcePreparation.HasSequencePreview)
            {
                m_ExecutingOutput = m_Executor.ExecuteSequencePreview(
                    m_SourcePreparation.SequencePreviewOperationIndex);
                m_HasExecutingOutput = true;
                m_FramePages.RequireEvaluationStagesCompleted(
                    m_ExecutingState.Lineage.CompletionIdentity);
                return;
            }
            m_ExecutingWorldInput = new CharacterPoseWorldFrameInput(
                m_WorldContext,
                m_ExecutingState.Lineage.ActorId,
                m_ExecutingState.Lineage.PresentationFrame,
                m_ExecutingState.PresentationDeltaSeconds,
                in bodyFrame,
                in factFrame,
                in parameterFrame,
                m_ExecutingState.Lineage.CompletionIdentity);
            m_ExecutionActive = true;
        }

        internal bool TryAdvanceCompletion(
            out CharacterPoseWorkerStageLease workerLease)
        {
            workerLease = default;
            if (m_HasExecutingOutput)
                return false;
            if (!m_ExecutionActive)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation has not entered execution.");
            }
            if (m_WaitingWorkerStageIndex >= 0)
            {
                CharacterPoseGraphNativeBinding frame =
                    m_FramePages.RequirePoseGraphBinding(
                        m_ExecutingState.Lineage.CompletionIdentity);
                AnimationPoseGraphNativeStage completed =
                    m_ExecutionView.Stages[m_WaitingWorkerStageIndex];
                if (frame.StageCompletedAt[completed.CompletionIndex] !=
                    m_ExecutingState.Lineage.CompletionIdentity)
                {
                    throw new InvalidOperationException(
                        $"Pose Worker Stage #{m_WaitingWorkerStageIndex} did not publish its Completion.");
                }
                m_NextStageIndex = m_WaitingWorkerStageIndex + 1;
                m_WaitingWorkerStageIndex = -1;
            }
            while (m_NextStageIndex < m_ExecutionView.Stages.Length)
            {
                AnimationPoseGraphNativeStage stage =
                    m_ExecutionView.Stages[m_NextStageIndex];
                if (CharacterPoseWorkerKernels.IsWorkerDomain(
                        stage.ExecutionDomain))
                {
                    m_WaitingWorkerStageIndex = m_NextStageIndex;
                    workerLease = m_Executor.CreateWorkerStageLease(
                        m_WorkerRegistration,
                        m_ActorId,
                        m_NextStageIndex);
                    return true;
                }
                if (!m_Executor.ExecuteStage(
                        m_NextStageIndex,
                        m_ExecutingState.PresentationDeltaSeconds,
                        in m_ExecutingWorldInput))
                {
                    m_NextStageIndex = m_ExecutionView.Stages.Length;
                    break;
                }
                m_NextStageIndex++;
            }
            m_ExecutingOutput = m_Executor.CompleteEvaluation();
            m_FramePages.RequireEvaluationStagesCompleted(
                m_ExecutingState.Lineage.CompletionIdentity);
            m_HasExecutingOutput = true;
            m_ExecutionActive = false;
            return false;
        }

        internal CharacterPoseProgramOutputResult FinishCompletion()
        {
            if (!m_HasExecutingOutput)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation has no completed output.");
            }
            CharacterPoseProgramOutputResult output = m_ExecutingOutput;
            m_ExecutingState = default;
            m_ExecutingWorldInput = default;
            m_ExecutingOutput = default;
            m_NextStageIndex = 0;
            m_WaitingWorkerStageIndex = -1;
            m_ExecutionActive = false;
            m_HasExecutingOutput = false;
            return output;
        }

        internal float RequireDeltaSeconds(
            in CharacterPoseProgramPrepared prepared) =>
            m_State.RequireDeltaSeconds(in prepared);

        internal void CompleteNodes(ulong completionIdentity)
        {
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
                m_ActorState.Stacks[i].CompleteFrame(completionIdentity);
            for (int i = 0; i < m_ActorState.DirectPlayers.Length; i++)
                m_ActorState.DirectPlayers[i].CompleteFrame();
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                m_ActorState.PoseStateSources.ClipPlayers[i].CompleteFrame();
            }
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                m_ActorState.PoseStateSources.BlendSpacePlayers[i]
                    .CompleteFrame();
            }
            for (int i = 0; i < m_ActorState.Routes.Length; i++)
            {
                m_ActorState.Routes[i].NotifyNativeFrameCompleted(
                    m_ActorState.Inertialization,
                    completionIdentity);
            }
            m_ActorState.PoseStateSources.NotifyNativeFrameCompleted(
                m_ActorState.Inertialization,
                completionIdentity);
            MarkCompleted(completionIdentity);
        }

        internal void Commit(ulong completionIdentity)
        {
            if (!m_State.HasPendingCompleted ||
                m_State.PendingCompletedCompletionIdentity !=
                    completionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no completed evaluation to commit.");
            }
            m_FramePages.CommitEvaluationFrame(completionIdentity);
            m_State.Commit(completionIdentity);
        }

        internal CharacterPoseProgramCommittedDiagnosticsView
            CapturePendingDiagnostics(
                in CharacterPoseProgramResult result,
                in CharacterPoseProgramOutputResult output,
                in ComposedAnimationPoseFrame outputFrame,
                AnimationPresentationDiagnosticsInterest interest)
        {
            if (!m_State.HasPendingCompleted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no Pending completed evaluation diagnostics.");
            }
            CharacterPoseGraphNativeBinding pending =
                m_State.RequirePendingCompleted();
            return m_Diagnostics.Capture(
                m_FramePages,
                in result,
                in pending,
                in output,
                in outputFrame,
                interest);
        }

        internal void DiscardPending()
        {
            ClearExecution();
            m_State.DiscardPending();
        }

        internal void Reset()
        {
            ClearExecution();
            m_State.Reset();
        }

        void ClearExecution()
        {
            m_WorkerRegistration.Fence();
            m_ExecutingState = default;
            m_ExecutingWorldInput = default;
            m_ExecutingOutput = default;
            m_NextStageIndex = 0;
            m_WaitingWorkerStageIndex = -1;
            m_ExecutionActive = false;
            m_HasExecutingOutput = false;
        }

        void MarkCompleted(ulong completionIdentity)
        {
            if (m_State.HasPrepared || m_State.HasPendingCompleted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation completion is invalid.");
            }
            m_FramePages.RequireEvaluationStagesCompleted(completionIdentity);
            CharacterPoseGraphNativeBinding completed =
                m_FramePages.RequirePoseGraphBinding(completionIdentity);
            m_State.MarkCompleted(in completed);
        }
    }
}
