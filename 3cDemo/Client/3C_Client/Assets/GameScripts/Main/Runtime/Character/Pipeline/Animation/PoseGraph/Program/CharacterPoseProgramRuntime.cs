using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramRuntime : IDisposable
    {
        readonly CharacterPoseProgramImage m_Image;
        bool m_Disposed;

        internal CharacterPoseProgramRuntime(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramTuningState tuning)
        {
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            ExecutionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
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
        internal CharacterPoseProgramFramePages FramePages { get; }
        internal CharacterPoseProgramTuningState Tuning { get; }

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
