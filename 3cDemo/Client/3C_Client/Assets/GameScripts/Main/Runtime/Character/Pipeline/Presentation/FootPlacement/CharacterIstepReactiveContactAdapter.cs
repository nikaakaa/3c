using System;
using HoaxGames;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    readonly struct CharacterFootReactiveContactObservation
    {
        internal CharacterFootReactiveContactObservation(
            CharacterFootReactiveContactRejectReason rejectReason,
            int surfaceIdentity,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            Vector3 targetAnkle,
            Vector3 targetSole,
            float correctionAlongUp,
            float clearanceAlongUp,
            Vector3 footprintOrigin,
            Vector3 footprintHalfExtents,
            Quaternion footprintRotation,
            float castDistance,
            float queryDistance,
            bool usedNormalRepair)
        {
            RejectReason = rejectReason;
            SurfaceIdentity = surfaceIdentity;
            SurfacePoint = surfacePoint;
            SurfaceNormal = surfaceNormal;
            TargetAnkle = targetAnkle;
            TargetSole = targetSole;
            CorrectionAlongUp = correctionAlongUp;
            ClearanceAlongUp = clearanceAlongUp;
            FootprintOrigin = footprintOrigin;
            FootprintHalfExtents = footprintHalfExtents;
            FootprintRotation = footprintRotation;
            CastDistance = castDistance;
            QueryDistance = queryDistance;
            UsedNormalRepair = usedNormalRepair;
        }

        internal CharacterFootReactiveContactRejectReason RejectReason { get; }
        internal bool Accepted => RejectReason == CharacterFootReactiveContactRejectReason.None;
        internal int SurfaceIdentity { get; }
        internal Vector3 SurfacePoint { get; }
        internal Vector3 SurfaceNormal { get; }
        internal Vector3 TargetAnkle { get; }
        internal Vector3 TargetSole { get; }
        internal float CorrectionAlongUp { get; }
        internal float ClearanceAlongUp { get; }
        internal Vector3 FootprintOrigin { get; }
        internal Vector3 FootprintHalfExtents { get; }
        internal Quaternion FootprintRotation { get; }
        internal float CastDistance { get; }
        internal float QueryDistance { get; }
        internal bool UsedNormalRepair { get; }
    }

    sealed class CharacterIstepReactiveContactAdapter
    {
        readonly PhysicsScene m_PhysicsScene;
        readonly Func<Collider, bool> m_IsSelfCollider;

        internal CharacterIstepReactiveContactAdapter(
            PhysicsScene physicsScene,
            Func<Collider, bool> isSelfCollider)
        {
            if (!physicsScene.IsValid())
                throw new ArgumentException("Reactive Foot Contact requires a valid PhysicsScene.", nameof(physicsScene));
            m_PhysicsScene = physicsScene;
            m_IsSelfCollider = isSelfCollider;
        }

        internal CharacterFootReactiveContactObservation Evaluate(
            in CharacterFootReactiveContactRequest request,
            in CharacterFootReactiveContactSettings settings)
        {
            if (!request.ActivePhase)
                return Rejected(CharacterFootReactiveContactRejectReason.InactivePhase, in request);
            if (!request.IsValid)
                return Rejected(CharacterFootReactiveContactRejectReason.InvalidRequest, in request);
            settings.RequireValid();

            Vector3 up = request.ComponentUp.normalized;
            Vector3 forward = Vector3.ProjectOnPlane(
                request.PoseRootRotation * Vector3.forward,
                up);
            if (forward.sqrMagnitude <= 0.000001f)
                return Rejected(CharacterFootReactiveContactRejectReason.InvalidRigGeometry, in request);
            forward.Normalize();

            Vector3 ikFromOrigin = request.OriginalAnklePosition - request.PoseRootPosition;
            Vector3 projectedOnCastAxis = Vector3.Project(ikFromOrigin, -up);
            Vector3 ikBottomPoint = request.OriginalAnklePosition - projectedOnCastAxis;
            var query = new FootContactQueryRequest(
                m_PhysicsScene,
                request.OriginalAnklePosition,
                ikBottomPoint,
                settings.FootHeight,
                settings.ForwardBias,
                forward,
                request.PoseRootRotation,
                -up,
                settings.MaximumIkCorrection,
                settings.CorrectionCastMargin,
                settings.MaximumFootCorrection,
                settings.MaximumBodyCorrection,
                settings.FootprintLength,
                settings.FootprintWidth,
                settings.LayerMask,
                settings.TriggerInteraction,
                settings.MaximumGroundDetectionAngleDegrees,
                settings.MaximumFootAdaptationAngleDegrees,
                settings.NormalRepairRadius,
                settings.MaximumRepairSeparation,
                m_IsSelfCollider,
                false);
            FootContactResult result = FootContactSolver.Solve(in query);
            CharacterFootReactiveContactRejectReason rejectReason = Map(result.RejectReason);
            if (rejectReason != CharacterFootReactiveContactRejectReason.None)
            {
                return new CharacterFootReactiveContactObservation(
                    rejectReason,
                    0,
                    default,
                    default,
                    request.OriginalAnklePosition,
                    request.OriginalSolePosition,
                    0f,
                    0f,
                    result.FootprintOrigin,
                    result.FootprintHalfExtents,
                    result.FootprintRotation,
                    result.CastDistance,
                    result.QueryDistance,
                    false);
            }

            float correctionAlongUp = Vector3.Dot(
                result.SurfacePoint - request.OriginalSolePosition,
                up);
            if (!float.IsFinite(correctionAlongUp) ||
                Mathf.Abs(correctionAlongUp) > settings.MaximumAcquisitionDistance)
            {
                return new CharacterFootReactiveContactObservation(
                    CharacterFootReactiveContactRejectReason.ContactOutsideAcquisitionRange,
                    result.SurfaceIdentity,
                    result.SurfacePoint,
                    result.Normal,
                    request.OriginalAnklePosition,
                    request.OriginalSolePosition,
                    0f,
                    -correctionAlongUp,
                    result.FootprintOrigin,
                    result.FootprintHalfExtents,
                    result.FootprintRotation,
                    result.CastDistance,
                    result.QueryDistance,
                    result.UsedNormalRepair);
            }

            Vector3 targetAnkle = request.OriginalAnklePosition + up * correctionAlongUp;
            Vector3 targetSole = request.OriginalSolePosition + up * correctionAlongUp;
            return new CharacterFootReactiveContactObservation(
                CharacterFootReactiveContactRejectReason.None,
                result.SurfaceIdentity,
                result.SurfacePoint,
                result.Normal,
                targetAnkle,
                targetSole,
                correctionAlongUp,
                -correctionAlongUp,
                result.FootprintOrigin,
                result.FootprintHalfExtents,
                result.FootprintRotation,
                result.CastDistance,
                result.QueryDistance,
                result.UsedNormalRepair);
        }

        static CharacterFootReactiveContactObservation Rejected(
            CharacterFootReactiveContactRejectReason reason,
            in CharacterFootReactiveContactRequest request) =>
            new CharacterFootReactiveContactObservation(
                reason,
                0,
                default,
                default,
                request.OriginalAnklePosition,
                request.OriginalSolePosition,
                0f,
                0f,
                default,
                default,
                Quaternion.identity,
                0f,
                0f,
                false);

        static CharacterFootReactiveContactRejectReason Map(
            FootContactRejectReason reason) => reason switch
        {
            FootContactRejectReason.None => CharacterFootReactiveContactRejectReason.None,
            FootContactRejectReason.InvalidRequest => CharacterFootReactiveContactRejectReason.InvalidRequest,
            FootContactRejectReason.NoFootprintHit => CharacterFootReactiveContactRejectReason.NoFootprintHit,
            FootContactRejectReason.SelfColliderOnly => CharacterFootReactiveContactRejectReason.SelfColliderOnly,
            FootContactRejectReason.InitialOverlapOnly => CharacterFootReactiveContactRejectReason.InitialOverlapOnly,
            FootContactRejectReason.InvalidSurfaceGeometry => CharacterFootReactiveContactRejectReason.InvalidSurfaceGeometry,
            FootContactRejectReason.GroundAngleExceeded => CharacterFootReactiveContactRejectReason.GroundAngleExceeded,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }
}
