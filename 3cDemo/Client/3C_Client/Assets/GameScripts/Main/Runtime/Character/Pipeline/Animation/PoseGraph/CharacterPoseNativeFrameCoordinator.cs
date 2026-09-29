using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeFrameCoordinator : IDisposable
    {
        readonly CharacterPoseNativeRoleRuntime m_Role;
        Exception m_Failure;
        bool m_Running;
        bool m_Disposed;

        internal CharacterPoseNativeFrameCoordinator(CharacterPoseNativeRoleRuntime role)
        {
            m_Role = role ?? throw new ArgumentNullException(nameof(role));
        }

        internal void RequireAvailable()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseNativeFrameCoordinator));
            if (m_Failure != null)
                throw new InvalidOperationException("Pose runtime is faulted and cannot execute another frame.", m_Failure);
            if (m_Running)
                throw new InvalidOperationException("Pose frame is already running.");
        }

        internal CharacterPoseNativePublicationResult RunFrame(
            in CharacterPoseNativeFrameInput input,
            IActionPresentationClockCoordinator clock,
            ICharacterPoseNativeActionCommandSource commands,
            bool captureFootIkDiagnostics)
        {
            RequireAvailable();
            m_Running = true;
            CharacterPoseNativeFrameLease lease = default;
            CharacterPoseNativeFailureCode failureCode = CharacterPoseNativeFailureCode.FrameInvalid;
            AnimationPresentationFramePhase phase = AnimationPresentationFramePhase.Begin;
            bool enteredBarrier = false;
            try
            {
                lease = m_Role.BeginFrame(in input);
                phase = AnimationPresentationFramePhase.Prepare;
                CharacterPoseNativePreparationResult preparation = m_Role.PrepareFrame(lease);
                if (!preparation.IsValid || preparation.Status != CharacterPoseNativeFrameStatus.Prepared)
                {
                    failureCode = preparation.FailureCode;
                    throw new InvalidOperationException(
                        $"Pose frame preparation failed ({preparation.Source}): {preparation.Message}");
                }
                CharacterPoseNativeSourceDemand demand = preparation.Demand;
                m_Role.PrepareEvaluation(lease, in demand, input.PresentationFrame);
                phase = AnimationPresentationFramePhase.EvaluateBarrier;
                enteredBarrier = true;
                m_Role.EvaluateAnimationGraph();
                CharacterPoseNativeEvaluationResult evaluation =
                    m_Role.Evaluate(lease, in demand, input.PresentationFrame);
                if (evaluation.Status != CharacterPoseNativeFrameStatus.Evaluated)
                {
                    failureCode = evaluation.FailureCode;
                    throw new InvalidOperationException(
                        $"Pose frame evaluation failed ({evaluation.Source}): {evaluation.Message}");
                }
                CharacterPoseNativeValidationResult validation = m_Role.ValidatePending(lease, in evaluation);
                if (!validation.IsValidated)
                {
                    failureCode = validation.FailureCode;
                    throw new InvalidOperationException(
                        $"Pose frame validation failed ({validation.Source}): {validation.Message}");
                }
                clock?.ValidateFrame();
                CharacterPoseNativePublicationResult publication =
                    m_Role.Commit(lease, in evaluation, captureFootIkDiagnostics);
                if (publication.Status != CharacterPoseNativeFrameStatus.Committed)
                {
                    failureCode = publication.FailureCode;
                    throw new InvalidOperationException(
                        $"Pose frame publication failed ({publication.Source}): {publication.Message}");
                }
                phase = AnimationPresentationFramePhase.Sealed;
                commands.CommitFrame();
                clock?.CommitFrame();
                return publication;
            }
            catch (Exception exception)
            {
                Exception failure = exception;
                try
                {
                    if (lease.IsValid)
                        m_Role.Discard(lease, failureCode == CharacterPoseNativeFailureCode.None
                            ? CharacterPoseNativeFailureCode.FrameInvalid
                            : failureCode);
                }
                catch (Exception cleanup)
                {
                    failure = new AggregateException("Pose frame cleanup failed.", failure, cleanup);
                }
                if (enteredBarrier)
                {
                    var fault = new AnimationPresentationFault(input.ActorId, input.PresentationFrame,
                        input.BodyTick, phase, m_Role.CurrentLineage.CompletionIdentity);
                    throw RecordFault(in fault, failure);
                }
                if (!ReferenceEquals(failure, exception))
                    throw failure;
                throw;
            }
            finally
            {
                m_Running = false;
            }
        }

        internal Exception RecordFault(in AnimationPresentationFault fault, Exception failure)
        {
            failure.Data[nameof(AnimationPresentationFault)] = fault;
            m_Failure = new InvalidOperationException(
                $"Actor presentation faulted: actor={fault.ActorId}, frame={fault.PresentationFrame}, " +
                $"bodyTick={fault.BodyTick}, completion={fault.CompletionIdentity}, phase={fault.Phase}.", failure);
            return m_Failure;
        }

        internal void Stop() => m_Role.Stop();

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Role.Dispose();
        }
    }
}
