using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterFootReactiveContactMeasurementSnapshot
    {
        internal CharacterFootReactiveContactMeasurementSnapshot(
            bool hasHistory,
            bool currentAccepted,
            ulong revision,
            int surfaceIdentity,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            float queryDistance)
        {
            HasHistory = hasHistory;
            CurrentAccepted = currentAccepted;
            Revision = revision;
            SurfaceIdentity = surfaceIdentity;
            SurfacePoint = surfacePoint;
            SurfaceNormal = surfaceNormal;
            QueryDistance = queryDistance;
        }

        public bool HasHistory { get; }
        public bool CurrentAccepted { get; }
        public ulong Revision { get; }
        public int SurfaceIdentity { get; }
        public Vector3 SurfacePoint { get; }
        public Vector3 SurfaceNormal { get; }
        public float QueryDistance { get; }
    }

    public readonly struct CharacterFootReactiveContactDiagnostics
    {
        internal CharacterFootReactiveContactDiagnostics(
            CharacterFootSide side,
            in CharacterFootReactiveContactMeasurementSnapshot committed,
            in CharacterFootReactiveContactMeasurementSnapshot pending,
            in CharacterFootReactiveContactProposal proposal)
        {
            Side = side;
            Committed = committed;
            Pending = pending;
            Proposal = proposal;
        }

        public CharacterFootSide Side { get; }
        public CharacterFootReactiveContactMeasurementSnapshot Committed { get; }
        public CharacterFootReactiveContactMeasurementSnapshot Pending { get; }
        public CharacterFootReactiveContactProposal Proposal { get; }
    }

    struct CharacterFootReactiveContactMeasurementFrame
    {
        internal bool HasHistory;
        internal bool CurrentAccepted;
        internal ulong Revision;
        internal int SurfaceIdentity;
        internal Vector3 SurfacePoint;
        internal Vector3 SurfaceNormal;
        internal float QueryDistance;

        internal CharacterFootReactiveContactMeasurementSnapshot Snapshot =>
            new CharacterFootReactiveContactMeasurementSnapshot(
                HasHistory,
                CurrentAccepted,
                Revision,
                SurfaceIdentity,
                SurfacePoint,
                SurfaceNormal,
                QueryDistance);
    }

    sealed class CharacterFootReactiveContactModule : IDisposable
    {
        readonly CharacterFootSide m_Side;
        readonly CharacterIstepReactiveContactAdapter m_Adapter;
        readonly CharacterFootReactiveContactSettings m_Settings;
        CharacterFootReactiveContactMeasurementFrame m_Committed;
        CharacterFootReactiveContactMeasurementFrame m_Pending;
        CharacterFootReactiveContactProposal m_PendingProposal;
        bool m_HasPending;
        bool m_HasEvaluated;
        bool m_Disposed;

        internal CharacterFootReactiveContactModule(
            CharacterFootSide side,
            CharacterIstepReactiveContactAdapter adapter,
            in CharacterFootReactiveContactSettings settings)
        {
            if (side != CharacterFootSide.Left && side != CharacterFootSide.Right)
                throw new ArgumentOutOfRangeException(nameof(side));
            m_Side = side;
            m_Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            settings.RequireValid();
            m_Settings = settings;
        }

        internal CharacterFootReactiveContactProposal PendingProposal
        {
            get
            {
                RequireEvaluated();
                return m_PendingProposal;
            }
        }

        internal void BeginPending()
        {
            RequireAlive();
            if (m_HasPending)
                throw new InvalidOperationException("Reactive Foot Contact already has a pending frame.");
            m_Pending = m_Committed;
            m_Pending.CurrentAccepted = false;
            m_PendingProposal = default;
            m_HasPending = true;
            m_HasEvaluated = false;
        }

        internal CharacterFootReactiveContactProposal Evaluate(
            in CharacterFootReactiveContactRequest request)
        {
            RequirePending();
            if (m_HasEvaluated)
                throw new InvalidOperationException("Reactive Foot Contact already evaluated this pending frame.");
            if (request.Side != m_Side)
                throw new ArgumentException("Reactive Foot Contact request side is inconsistent.", nameof(request));
            m_HasEvaluated = true;

            CharacterFootReactiveContactObservation observation =
                m_Adapter.Evaluate(in request, in m_Settings);
            if (!observation.Accepted)
            {
                m_Pending.CurrentAccepted = false;
                m_PendingProposal = Reject(in request, in observation);
                return m_PendingProposal;
            }

            bool reuseMeasurement = m_Pending.HasHistory &&
                                    m_Pending.SurfaceIdentity == observation.SurfaceIdentity &&
                                    Vector3.Distance(
                                        m_Pending.SurfacePoint,
                                        observation.SurfacePoint) <= m_Settings.ContactPointDeadZone &&
                                    Vector3.Angle(
                                        m_Pending.SurfaceNormal,
                                        observation.SurfaceNormal) <=
                                    m_Settings.ContactNormalDeadZoneDegrees;
            if (!reuseMeasurement)
            {
                m_Pending.HasHistory = true;
                m_Pending.Revision = NextRevision(m_Pending.Revision);
                m_Pending.SurfaceIdentity = observation.SurfaceIdentity;
                m_Pending.SurfacePoint = observation.SurfacePoint;
                m_Pending.SurfaceNormal = observation.SurfaceNormal.normalized;
                m_Pending.QueryDistance = observation.QueryDistance;
            }
            m_Pending.CurrentAccepted = true;

            Vector3 up = request.ComponentUp.normalized;
            float correctionAlongUp = Vector3.Dot(
                m_Pending.SurfacePoint - request.OriginalSolePosition,
                up);
            if (!float.IsFinite(correctionAlongUp) ||
                Mathf.Abs(correctionAlongUp) > m_Settings.MaximumAcquisitionDistance)
            {
                m_Pending.CurrentAccepted = false;
                var rejectedObservation = new CharacterFootReactiveContactObservation(
                    CharacterFootReactiveContactRejectReason.ContactOutsideAcquisitionRange,
                    observation.SurfaceIdentity,
                    observation.SurfacePoint,
                    observation.SurfaceNormal,
                    request.OriginalAnklePosition,
                    request.OriginalSolePosition,
                    0f,
                    -correctionAlongUp,
                    observation.FootprintOrigin,
                    observation.FootprintHalfExtents,
                    observation.FootprintRotation,
                    observation.CastDistance,
                    observation.QueryDistance,
                    observation.UsedNormalRepair);
                m_PendingProposal = Reject(in request, in rejectedObservation);
                return m_PendingProposal;
            }

            Vector3 targetAnkle = request.OriginalAnklePosition + up * correctionAlongUp;
            Vector3 targetSole = request.OriginalSolePosition + up * correctionAlongUp;
            var goal = new CharacterFootGoalProposal(
                CharacterFootGoalProposalSourceKind.Reactive,
                true,
                request.FrameSequence,
                request.CompletionIdentity,
                request.RigId,
                request.RigRevision,
                request.Side,
                request.LandingEventIdentity,
                m_Pending.Revision,
                m_Pending.SurfaceIdentity,
                m_Pending.SurfacePoint,
                m_Pending.SurfaceNormal,
                request.OriginalAnklePosition,
                request.OriginalAnkleRotation,
                request.OriginalSolePosition,
                targetAnkle,
                request.OriginalAnkleRotation,
                targetSole,
                false,
                0);
            m_PendingProposal = new CharacterFootReactiveContactProposal(
                CharacterFootReactiveContactRejectReason.None,
                in goal,
                correctionAlongUp,
                -correctionAlongUp,
                observation.FootprintOrigin,
                observation.FootprintHalfExtents,
                observation.FootprintRotation,
                observation.CastDistance,
                observation.QueryDistance,
                observation.UsedNormalRepair);
            return m_PendingProposal;
        }

        internal CharacterFootReactiveContactDiagnostics CaptureDiagnostics()
        {
            RequireEvaluated();
            CharacterFootReactiveContactMeasurementSnapshot committed = m_Committed.Snapshot;
            CharacterFootReactiveContactMeasurementSnapshot pending = m_Pending.Snapshot;
            return new CharacterFootReactiveContactDiagnostics(
                m_Side,
                in committed,
                in pending,
                in m_PendingProposal);
        }

        internal void Seal()
        {
            RequireEvaluated();
            m_Committed = m_Pending;
            ClearPending();
        }

        internal void Discard()
        {
            RequirePending();
            ClearPending();
        }

        internal void Reset()
        {
            RequireAlive();
            m_Committed = default;
            ClearPending();
        }

        internal void Retarget() => Reset();

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Committed = default;
            ClearPending();
        }

        CharacterFootReactiveContactProposal Reject(
            in CharacterFootReactiveContactRequest request,
            in CharacterFootReactiveContactObservation observation)
        {
            var goal = new CharacterFootGoalProposal(
                CharacterFootGoalProposalSourceKind.Reactive,
                false,
                request.FrameSequence,
                request.CompletionIdentity,
                request.RigId,
                request.RigRevision,
                request.Side,
                request.LandingEventIdentity,
                0,
                0,
                default,
                default,
                request.OriginalAnklePosition,
                request.OriginalAnkleRotation,
                request.OriginalSolePosition,
                request.OriginalAnklePosition,
                request.OriginalAnkleRotation,
                request.OriginalSolePosition,
                false,
                (int)observation.RejectReason);
            return new CharacterFootReactiveContactProposal(
                observation.RejectReason,
                in goal,
                0f,
                observation.ClearanceAlongUp,
                observation.FootprintOrigin,
                observation.FootprintHalfExtents,
                observation.FootprintRotation,
                observation.CastDistance,
                observation.QueryDistance,
                observation.UsedNormalRepair);
        }

        static ulong NextRevision(ulong current)
        {
            if (current == ulong.MaxValue)
                throw new OverflowException("Reactive Foot Contact measurement revision overflowed.");
            return current + 1;
        }

        void ClearPending()
        {
            m_Pending = default;
            m_PendingProposal = default;
            m_HasPending = false;
            m_HasEvaluated = false;
        }

        void RequirePending()
        {
            RequireAlive();
            if (!m_HasPending)
                throw new InvalidOperationException("Reactive Foot Contact has no pending frame.");
        }

        void RequireEvaluated()
        {
            RequirePending();
            if (!m_HasEvaluated)
                throw new InvalidOperationException("Reactive Foot Contact pending frame has not evaluated.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterFootReactiveContactModule));
        }
    }
}
