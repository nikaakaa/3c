using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramRuntime : IDisposable
    {
        readonly CharacterPoseProgramImage m_Image;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        bool m_Disposed;

        internal CharacterPoseProgramRuntime(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramTuningState tuning,
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
            Executor = new CharacterPoseGraphStagedExecutor(
                ExecutionView,
                FramePages,
                ActorState.Inertialization,
                poseConstraints);
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
        internal CharacterPoseGraphStagedExecutor Executor { get; }
        internal bool HasOpenFrame => m_ActiveFrameLease.IsValid;
        internal bool HasPendingEvaluationFrame =>
            FramePages.HasPendingEvaluationFrame;
        internal ulong PendingEvaluationCompletionIdentity =>
            FramePages.PendingEvaluationCompletionIdentity;
        internal long DenseDoublePageResidentPayloadBytes =>
            FramePages.DenseDoublePageResidentPayloadBytes;

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

        internal CharacterPoseGraphNativeBinding BeginEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            return FramePages.BeginEvaluationFrame(completionIdentity);
        }

        internal void CommitEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            FramePages.CommitEvaluationFrame(completionIdentity);
        }

        internal void DiscardEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            FramePages.DiscardEvaluationFrame(completionIdentity);
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

        internal CharacterPoseGraphNativeBinding RequirePoseGraphBinding(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            return FramePages.RequirePoseGraphBinding(completionIdentity);
        }

        internal AnimationPoseValueNativeReadBinding
            RequirePoseValueReadBinding(
                CharacterPoseProgramFrameLease lease,
                int valueIndex,
                ulong completionIdentity)
        {
            RequireFrame(lease);
            return FramePages.RequirePoseValueReadBinding(
                valueIndex,
                completionIdentity);
        }

        internal void RequireEvaluationStagesCompleted(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            FramePages.RequireEvaluationStagesCompleted(completionIdentity);
        }

        internal CharacterPoseProgramTuningView RequireTuning(
            ulong generation) => Tuning.RequireCommitted(generation);

        internal CharacterPoseProgramCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
                CharacterPoseProgramCommittedDiagnosticsProjector projector,
                in CharacterPoseProgramResult result,
                in CharacterPoseGraphNativeBinding frame,
                in CharacterFinalPoseCommittedDiagnosticsView finalOutput,
                AnimationPresentationDiagnosticsInterest interest)
        {
            if (projector == null)
                throw new ArgumentNullException(nameof(projector));
            return projector.Capture(
                FramePages,
                in result,
                in frame,
                in finalOutput,
                interest);
        }

        internal CharacterPoseGraphStagedExecutor BindExecutor(
            in CharacterPoseProgramTuningView tuning,
            CharacterPoseGraphNativeBinding binding,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            bool recordDiagnostics) =>
            Executor.BindFrame(
                in tuning,
                binding,
                in finalOutput,
                recordDiagnostics);

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
