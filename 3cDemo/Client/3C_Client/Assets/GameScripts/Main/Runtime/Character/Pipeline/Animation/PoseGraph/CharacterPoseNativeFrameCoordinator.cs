using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeFrameCoordinator : IDisposable
    {
        readonly CharacterPoseNativeRoleRuntime m_Role;
        CharacterPoseNativeFrameLease m_Lease;
        CharacterPoseNativePreparationResult m_Preparation;
        CharacterPoseNativeEvaluationResult m_Evaluation;
        CharacterPoseNativeValidationResult m_Validation;
        bool m_Open;
        bool m_Disposed;

        internal CharacterPoseNativeFrameCoordinator(
            CharacterPoseNativeRoleRuntime role)
        {
            m_Role = role ?? throw new ArgumentNullException(nameof(role));
        }

        internal bool IsOpen => m_Open;
        internal CharacterPoseNativeFrameLease Lease => m_Lease;
        internal CharacterPoseNativePreparationResult Preparation => m_Preparation;
        internal CharacterPoseNativeSourceDemand Demand => m_Preparation.Demand;
        internal CharacterPoseNativeEvaluationResult Evaluation => m_Evaluation;
        internal CharacterPoseNativeValidationResult Validation => m_Validation;

        internal CharacterPoseNativePreparationResult BeginFrame(
            in CharacterPoseNativeFrameInput input)
        {
            RequireAlive();
            if (m_Open)
                throw new InvalidOperationException(
                    "Pose native frame coordinator already has an open frame.");
            m_Lease = m_Role.BeginFrame(in input);
            m_Preparation = m_Role.PrepareFrame(m_Lease);
            m_Evaluation = default;
            m_Validation = default;
            if (!m_Preparation.IsValid ||
                m_Preparation.Status != CharacterPoseNativeFrameStatus.Prepared)
            {
                DiscardOpenedFrame(
                    m_Preparation.FailureCode == CharacterPoseNativeFailureCode.None
                        ? CharacterPoseNativeFailureCode.FrameInvalid
                        : m_Preparation.FailureCode);
                return m_Preparation;
            }
            m_Open = true;
            return m_Preparation;
        }

        internal void PrepareEvaluation(ulong barrierIdentity)
        {
            RequireOpen();
            CharacterPoseNativeSourceDemand demand = m_Preparation.Demand;
            m_Role.PrepareEvaluation(
                m_Lease,
                in demand,
                barrierIdentity);
        }

        internal CharacterPoseNativeEvaluationResult Evaluate(
            ulong barrierIdentity)
        {
            RequireOpen();
            CharacterPoseNativeSourceDemand demand = m_Preparation.Demand;
            m_Evaluation = m_Role.Evaluate(
                m_Lease,
                in demand,
                barrierIdentity);
            return m_Evaluation;
        }

        internal CharacterPoseNativeValidationResult ValidatePending()
        {
            RequireOpen();
            m_Validation = m_Role.ValidatePending(
                m_Lease,
                in m_Evaluation);
            return m_Validation;
        }

        internal CharacterPoseNativePublicationResult Commit(
            bool captureFootIkDiagnostics)
        {
            RequireOpen();
            CharacterPoseNativePublicationResult result = m_Role.Commit(
                m_Lease,
                in m_Evaluation,
                captureFootIkDiagnostics);
            ClearFrame();
            return result;
        }

        internal void Discard(CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            if (!m_Open)
                return;
            if (reason == CharacterPoseNativeFailureCode.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            try
            {
                m_Role.Discard(m_Lease, reason);
            }
            finally
            {
                ClearFrame();
            }
        }

        internal void Stop()
        {
            RequireAlive();
            if (m_Open)
                Discard(CharacterPoseNativeFailureCode.Disposed);
            m_Role.Stop();
        }

        internal bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation) =>
            m_Role.TryObserve(nodeId, portId, out observation);

        void DiscardOpenedFrame(CharacterPoseNativeFailureCode reason)
        {
            try
            {
                m_Role.Discard(m_Lease, reason);
            }
            finally
            {
                ClearFrame();
            }
        }

        void ClearFrame()
        {
            m_Lease = default;
            m_Preparation = default;
            m_Evaluation = default;
            m_Validation = default;
            m_Open = false;
        }

        void RequireOpen()
        {
            RequireAlive();
            if (!m_Open)
                throw new InvalidOperationException(
                    "Pose native frame coordinator has no open frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeFrameCoordinator));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            try
            {
                if (m_Open)
                    m_Role.Discard(
                        m_Lease,
                        CharacterPoseNativeFailureCode.Disposed);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                ClearFrame();
            }
            try
            {
                m_Role.Dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            if (failure != null)
                throw failure;
        }
    }
}
