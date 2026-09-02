using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;

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
        readonly CharacterPoseProgramEvaluationState m_State =
            new CharacterPoseProgramEvaluationState();

        internal CharacterPoseProgramEvaluationRuntime(
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramExecutor executor,
            CharacterPoseWorldContextAdapter worldContext,
            CharacterPoseProgramSourcePreparationRuntime sourcePreparation,
            CharacterPoseProgramCommittedDiagnosticsProjector diagnostics)
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

        internal CharacterPoseProgramOutputResult Complete(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame)
        {
            if (!prepared.IsValid ||
                !lease.Matches(prepared.Lineage) ||
                !bodyFrame.IsValid ||
                !factFrame.IsValid ||
                !m_FramePages.HasPendingEvaluationFrame ||
                m_FramePages.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program evaluation input is invalid.",
                    nameof(prepared));
            }
            CharacterPosePreparedEvaluationState state =
                m_State.Consume(in prepared);
            m_Executor.BeginEvaluation(state.Lineage.PresentationFrame);
            CharacterPoseProgramOutputResult output;
            if (m_SourcePreparation.HasSequencePreview)
            {
                output = m_Executor.ExecuteSequencePreview(
                    m_SourcePreparation.SequencePreviewOperationIndex);
            }
            else
            {
                var worldInput = new CharacterPoseWorldFrameInput(
                    m_WorldContext,
                    state.Lineage.ActorId,
                    state.Lineage.PresentationFrame,
                    state.PresentationDeltaSeconds,
                    in bodyFrame,
                    in factFrame,
                    state.Lineage.CompletionIdentity);
                for (int stageIndex = 0;
                     stageIndex < m_ExecutionView.Stages.Length;
                     stageIndex++)
                {
                    AnimationPoseGraphNativeStage stage =
                        m_ExecutionView.Stages[stageIndex];
                    if (!m_Executor.ExecuteStage(
                            stageIndex,
                            state.PresentationDeltaSeconds,
                            in worldInput))
                    {
                        break;
                    }
                }
                output = m_Executor.CompleteEvaluation();
            }
            m_FramePages.RequireEvaluationStagesCompleted(
                state.Lineage.CompletionIdentity);
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

        internal void DiscardPending() => m_State.DiscardPending();

        internal void Reset() => m_State.Reset();

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
