using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootLandingPredictionState : byte
    {
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootLandingPredictionRejectReason : byte
    {
        None = 0,
        StepUnavailable = 1,
        StepIdentityMismatch = 2,
        LandingTimeInvalid = 3,
        MotionTimelineUnavailable = 4,
        FutureBodyUnavailable = 5,
        FutureBodyRangeInvalid = 6,
        GroundQueryMissed = 7
    }

    internal readonly struct CharacterFootLandingSupport
    {
        internal CharacterFootLandingSupport(
            int surfaceIdentity,
            Vector3 point,
            Vector3 normal,
            float distance)
        {
            if (surfaceIdentity == 0 || !Finite(point) || !Finite(normal) ||
                normal.sqrMagnitude <= 0.000001f ||
                !float.IsFinite(distance) || distance < 0f)
            {
                throw new ArgumentException("Foot Landing support is invalid.");
            }
            SurfaceIdentity = surfaceIdentity;
            Point = point;
            Normal = normal.normalized;
            Distance = distance;
        }

        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
        internal float Distance { get; }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal interface ICharacterFootLandingWorldQuery
    {
        bool TryQuery(
            in CharacterFootPlacementQueryRequest request,
            out CharacterFootLandingSupport support);
    }

    internal sealed class CharacterFootLandingWorldQuery : ICharacterFootLandingWorldQuery
    {
        readonly CharacterFootPlacementWorldQueryBackend m_Backend;

        internal CharacterFootLandingWorldQuery(
            CharacterFootPlacementWorldQueryBackend backend)
        {
            m_Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public bool TryQuery(
            in CharacterFootPlacementQueryRequest request,
            out CharacterFootLandingSupport support)
        {
            if (!m_Backend.Query(in request, out CharacterFootPlacementQueryHit hit))
            {
                support = default;
                return false;
            }
            support = new CharacterFootLandingSupport(
                hit.SurfaceIdentity,
                hit.Point,
                hit.Normal,
                hit.Distance);
            return true;
        }
    }

    public readonly struct CharacterFootLandingPredictionFootDiagnostics
    {
        internal CharacterFootLandingPredictionFootDiagnostics(
            CharacterFootSide side,
            CharacterFootLandingPredictionState state,
            CharacterFootLandingPredictionRejectReason rejectReason,
            ulong landingEventIdentity,
            ulong trajectoryGeneration,
            float landingConfidence,
            float timeToLandingSeconds,
            Vector3 rootLocalLanding,
            Vector3 currentAnimatedSole,
            Vector3 rawLandingCandidate,
            CharacterFootPlacementQueryRequest query,
            CharacterFootLandingSupport support,
            CharacterFullBodyIkGoal goal)
        {
            Side = side;
            State = state;
            RejectReason = rejectReason;
            LandingEventIdentity = landingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            LandingConfidence = landingConfidence;
            TimeToLandingSeconds = timeToLandingSeconds;
            RootLocalLanding = rootLocalLanding;
            CurrentAnimatedSole = currentAnimatedSole;
            RawLandingCandidate = rawLandingCandidate;
            Query = query;
            SurfaceIdentity = support.SurfaceIdentity;
            LandingPoint = support.Point;
            LandingNormal = support.Normal;
            QueryDistance = support.Distance;
            Goal = goal;
        }

        public CharacterFootSide Side { get; }
        public CharacterFootLandingPredictionState State { get; }
        public CharacterFootLandingPredictionRejectReason RejectReason { get; }
        public ulong LandingEventIdentity { get; }
        public ulong TrajectoryGeneration { get; }
        public float LandingConfidence { get; }
        public float TimeToLandingSeconds { get; }
        public Vector3 RootLocalLanding { get; }
        public Vector3 CurrentAnimatedSole { get; }
        public Vector3 RawLandingCandidate { get; }
        public CharacterFootPlacementQueryRequest Query { get; }
        public int SurfaceIdentity { get; }
        public Vector3 LandingPoint { get; }
        public Vector3 LandingNormal { get; }
        public float QueryDistance { get; }
        public CharacterFullBodyIkGoal Goal { get; }
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;
    }

    public readonly struct CharacterFootLandingPredictionDiagnostics
    {
        internal CharacterFootLandingPredictionDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            int rootInstanceId,
            CharacterFullBodyIkGoal pelvisGoal,
            CharacterFootLandingPredictionFootDiagnostics left,
            CharacterFootLandingPredictionFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RootInstanceId = rootInstanceId;
            PelvisGoal = pelvisGoal;
            Left = left;
            Right = right;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public int RootInstanceId { get; }
        public CharacterFullBodyIkGoal PelvisGoal { get; }
        public CharacterFootLandingPredictionFootDiagnostics Left { get; }
        public CharacterFootLandingPredictionFootDiagnostics Right { get; }
        public bool IsCompleted =>
            FrameSequence != 0 && CompletionIdentity != 0 && RootInstanceId != 0 &&
            PelvisGoal.IsValid && Left.Goal.IsValid && Right.Goal.IsValid;
    }

    internal static class CharacterFootLandingPredictionDebugRegistry
    {
        static readonly Dictionary<int, CharacterFootLandingPredictionDiagnostics> s_ByRoot =
            new Dictionary<int, CharacterFootLandingPredictionDiagnostics>();

        internal static void Publish(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (diagnostics.IsCompleted)
                s_ByRoot[diagnostics.RootInstanceId] = diagnostics;
        }

        internal static bool TryGet(
            int rootInstanceId,
            out CharacterFootLandingPredictionDiagnostics diagnostics) =>
            s_ByRoot.TryGetValue(rootInstanceId, out diagnostics);

        internal static void Remove(int rootInstanceId) => s_ByRoot.Remove(rootInstanceId);
    }

    internal static class CharacterFootLandingPredictor
    {
        internal static Vector3 ProjectRawLanding(
            Vector3 rootPosition,
            Quaternion rootRotation,
            Vector3 componentUp,
            in ThirdPersonSimulation.CharacterFutureBodyTrajectorySample bodySample,
            Vector3 rootLocalLanding)
        {
            if (!Finite(rootPosition) || !Finite(componentUp) ||
                componentUp.sqrMagnitude <= 0.000001f || !Finite(rootLocalLanding))
            {
                throw new ArgumentException("Foot Landing projection input is invalid.");
            }
            Vector3 futureRootPosition = rootPosition + new Vector3(
                bodySample.RelativePositionX,
                bodySample.RelativePositionY,
                bodySample.RelativePositionZ);
            Quaternion futureRootRotation = (
                Quaternion.AngleAxis(
                    bodySample.RelativeYawDegrees,
                    componentUp.normalized) * rootRotation).normalized;
            return futureRootPosition + futureRootRotation * rootLocalLanding;
        }

        internal static CharacterFootPlacementQueryRequest BuildQuery(
            CharacterFootSide side,
            Vector3 rawLandingCandidate,
            Vector3 componentUp,
            in CharacterFootLandingPredictionSettings settings)
        {
            Vector3 up = componentUp.normalized;
            return new CharacterFootPlacementQueryRequest(
                CharacterFootPlacementQueryShape.Sphere,
                CharacterFootPlacementQueryPurpose.FutureLanding,
                side == CharacterFootSide.Left ? 0 : 1,
                rawLandingCandidate + up * settings.CastAbove,
                Vector3.zero,
                -up,
                settings.CastAbove + settings.CastBelow,
                settings.SphereRadius,
                settings.GroundLayerMask,
                settings.MinimumGroundNormalDot);
        }

        internal static bool TryResolve(
            CharacterFootSide side,
            Vector3 rawLandingCandidate,
            Vector3 componentUp,
            in CharacterFootLandingPredictionSettings settings,
            ICharacterFootLandingWorldQuery world,
            out CharacterFootPlacementQueryRequest query,
            out CharacterFootLandingSupport support)
        {
            query = BuildQuery(
                side,
                rawLandingCandidate,
                componentUp,
                in settings);
            return world.TryQuery(in query, out support);
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
