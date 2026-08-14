using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterPredictiveFootRoutePointSnapshot
    {
        public CharacterPredictiveFootRoutePointSnapshot(float fraction, Vector3 position)
        {
            Fraction = Mathf.Clamp01(fraction);
            Position = position;
        }

        public float Fraction { get; }
        public Vector3 Position { get; }
    }

    public readonly struct CharacterPredictiveFootRatePointSnapshot
    {
        public CharacterPredictiveFootRatePointSnapshot(float actionPhase, float groundPathProgress)
        {
            ActionPhase = Mathf.Clamp01(actionPhase);
            GroundPathProgress = Mathf.Clamp01(groundPathProgress);
        }

        public float ActionPhase { get; }
        public float GroundPathProgress { get; }
    }

    public readonly struct CharacterPredictiveFootEnvelopeSegmentSnapshot
    {
        internal CharacterPredictiveFootEnvelopeSegmentSnapshot(in FootPlacementGroundEnvelopeSegment segment)
        {
            StartFraction = segment.StartFraction;
            EndFraction = segment.EndFraction;
            SurfaceIdentity = segment.Surface.Identity;
            SurfaceNormal = segment.Surface.IsValid ? segment.Surface.Normal : Vector3.zero;
            EdgeStart = segment.EdgeStart;
            EdgeEnd = segment.EdgeEnd;
            StartSoleHeight = segment.StartSoleHeight;
            EndSoleHeight = segment.EndSoleHeight;
        }

        public float StartFraction { get; }
        public float EndFraction { get; }
        public int SurfaceIdentity { get; }
        public Vector3 SurfaceNormal { get; }
        public Vector3 EdgeStart { get; }
        public Vector3 EdgeEnd { get; }
        public float StartSoleHeight { get; }
        public float EndSoleHeight { get; }
    }

    public readonly struct CharacterPredictiveFootClearanceSegmentSnapshot
    {
        internal CharacterPredictiveFootClearanceSegmentSnapshot(
            float startFraction,
            float endFraction,
            Vector3 start,
            Vector3 end,
            FootPlacementSurface surface,
            Vector3 rootStart,
            Vector3 rootEnd,
            Vector3 hipStart,
            Vector3 hipEnd,
            float startHeight,
            float endHeight)
        {
            StartFraction = Mathf.Clamp01(startFraction);
            EndFraction = Mathf.Clamp01(endFraction);
            Start = start;
            End = end;
            SurfaceIdentity = surface.Identity;
            RootStart = rootStart;
            RootEnd = rootEnd;
            HipStart = hipStart;
            HipEnd = hipEnd;
            StartHeight = startHeight;
            EndHeight = endHeight;
        }

        public float StartFraction { get; }
        public float EndFraction { get; }
        public Vector3 Start { get; }
        public Vector3 End { get; }
        public int SurfaceIdentity { get; }
        public Vector3 RootStart { get; }
        public Vector3 RootEnd { get; }
        public Vector3 HipStart { get; }
        public Vector3 HipEnd { get; }
        public float StartHeight { get; }
        public float EndHeight { get; }
    }

    public readonly struct CharacterPredictiveFootQueryRequestSnapshot
    {
        internal CharacterPredictiveFootQueryRequestSnapshot(in CharacterFootPlacementQueryRequest request)
        {
            Shape = request.Shape.ToString();
            Purpose = request.Purpose.ToString();
            Origin = request.Origin;
            CapsuleEnd = request.CapsuleEnd;
            Direction = request.Direction;
            MaximumDistance = request.MaximumDistance;
            Radius = request.Radius;
            LayerMask = request.LayerMask;
            MinimumGroundNormalDot = request.MinimumGroundNormalDot;
        }

        public string Shape { get; }
        public string Purpose { get; }
        public Vector3 Origin { get; }
        public Vector3 CapsuleEnd { get; }
        public Vector3 Direction { get; }
        public float MaximumDistance { get; }
        public float Radius { get; }
        public int LayerMask { get; }
        public float MinimumGroundNormalDot { get; }
    }

    public readonly struct CharacterPredictiveFootQueryGeometrySnapshot
    {
        internal CharacterPredictiveFootQueryGeometrySnapshot(
            int queryIndex,
            Vector3 position,
            Vector3 normal,
            int surfaceIdentity,
            FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            QueryIndex = queryIndex;
            Position = position;
            Normal = normal;
            SurfaceIdentity = surfaceIdentity;
            RejectReason = rejectReason.ToString();
        }

        public int QueryIndex { get; }
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public int SurfaceIdentity { get; }
        public string RejectReason { get; }
    }

    public sealed class CharacterPredictiveFootPlanGeometrySnapshot
    {
        readonly CharacterPredictiveFootRoutePointSnapshot[] m_GroundProbeRoute;
        readonly CharacterPredictiveFootRoutePointSnapshot[] m_AnimationFootRoute;
        readonly CharacterPredictiveFootRatePointSnapshot[] m_FootRate;
        readonly CharacterPredictiveFootClearanceSegmentSnapshot[] m_ClearancePath;
        readonly CharacterPredictiveFootEnvelopeSegmentSnapshot[] m_GroundEnvelope;
        readonly CharacterPredictiveFootQueryRequestSnapshot[] m_QueryRequests;
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_AcceptedSupports;
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_RejectedGeometry;

        internal CharacterPredictiveFootPlanGeometrySnapshot(
            CharacterFootSide side,
            ulong planSequence,
            ulong generatedFrame,
            ulong landingEventIdentity,
            bool executable,
            bool landingValid,
            Vector3 landing,
            bool virtualGroundSplitValid,
            float virtualGroundSplitEventPhase,
            Vector3 virtualGroundOpposingLanding,
            Vector3 virtualGroundSplitRoutePoint,
            float virtualGroundSplitPlanarError,
            float virtualGroundSplitFraction,
            ulong virtualGroundSplitLandingEventIdentity,
            Vector3 virtualGroundSplit,
            Vector3 currentPlanarVelocity,
            Vector3 continuationPlanarVelocity,
            float currentSegmentSwitchDelaySeconds,
            bool hasContinuation,
            float yawVelocityDegreesPerSecond,
            float trajectoryYawRateDegreesPerSecond,
            CharacterPredictiveFootRoutePointSnapshot[] groundProbeRoute,
            CharacterPredictiveFootRoutePointSnapshot[] animationFootRoute,
            CharacterPredictiveFootRatePointSnapshot[] footRate,
            CharacterPredictiveFootClearanceSegmentSnapshot[] clearancePath,
            CharacterPredictiveFootEnvelopeSegmentSnapshot[] groundEnvelope,
            CharacterPredictiveFootQueryRequestSnapshot[] queryRequests,
            CharacterPredictiveFootQueryGeometrySnapshot[] acceptedSupports,
            CharacterPredictiveFootQueryGeometrySnapshot[] rejectedGeometry)
        {
            Side = side;
            PlanSequence = planSequence;
            GeneratedFrame = generatedFrame;
            LandingEventIdentity = landingEventIdentity;
            Executable = executable;
            LandingValid = landingValid;
            Landing = landing;
            VirtualGroundSplitValid = virtualGroundSplitValid;
            VirtualGroundSplitEventPhase = Mathf.Clamp01(virtualGroundSplitEventPhase);
            VirtualGroundOpposingLanding = virtualGroundOpposingLanding;
            VirtualGroundSplitRoutePoint = virtualGroundSplitRoutePoint;
            VirtualGroundSplitPlanarError = Mathf.Max(0f, virtualGroundSplitPlanarError);
            VirtualGroundSplitFraction = Mathf.Clamp01(virtualGroundSplitFraction);
            VirtualGroundSplitLandingEventIdentity = virtualGroundSplitLandingEventIdentity;
            VirtualGroundSplit = virtualGroundSplit;
            CurrentPlanarVelocity = currentPlanarVelocity;
            ContinuationPlanarVelocity = continuationPlanarVelocity;
            CurrentSegmentSwitchDelaySeconds = currentSegmentSwitchDelaySeconds;
            HasContinuation = hasContinuation;
            YawVelocityDegreesPerSecond = yawVelocityDegreesPerSecond;
            TrajectoryYawRateDegreesPerSecond = trajectoryYawRateDegreesPerSecond;
            m_GroundProbeRoute = groundProbeRoute ?? Array.Empty<CharacterPredictiveFootRoutePointSnapshot>();
            m_AnimationFootRoute = animationFootRoute ?? Array.Empty<CharacterPredictiveFootRoutePointSnapshot>();
            m_FootRate = footRate ?? Array.Empty<CharacterPredictiveFootRatePointSnapshot>();
            m_ClearancePath = clearancePath ?? Array.Empty<CharacterPredictiveFootClearanceSegmentSnapshot>();
            m_GroundEnvelope = groundEnvelope ?? Array.Empty<CharacterPredictiveFootEnvelopeSegmentSnapshot>();
            m_QueryRequests = queryRequests ?? Array.Empty<CharacterPredictiveFootQueryRequestSnapshot>();
            m_AcceptedSupports = acceptedSupports ?? Array.Empty<CharacterPredictiveFootQueryGeometrySnapshot>();
            m_RejectedGeometry = rejectedGeometry ?? Array.Empty<CharacterPredictiveFootQueryGeometrySnapshot>();
        }

        public CharacterFootSide Side { get; }
        public ulong PlanSequence { get; }
        public ulong GeneratedFrame { get; }
        public ulong LandingEventIdentity { get; }
        public bool Executable { get; }
        public bool LandingValid { get; }
        public Vector3 Landing { get; }
        public bool VirtualGroundSplitValid { get; }
        public float VirtualGroundSplitEventPhase { get; }
        public Vector3 VirtualGroundOpposingLanding { get; }
        public Vector3 VirtualGroundSplitRoutePoint { get; }
        public float VirtualGroundSplitPlanarError { get; }
        public float VirtualGroundSplitFraction { get; }
        public ulong VirtualGroundSplitLandingEventIdentity { get; }
        public Vector3 VirtualGroundSplit { get; }
        public Vector3 CurrentPlanarVelocity { get; }
        public Vector3 ContinuationPlanarVelocity { get; }
        public float CurrentSegmentSwitchDelaySeconds { get; }
        public bool HasContinuation { get; }
        public float YawVelocityDegreesPerSecond { get; }
        public float TrajectoryYawRateDegreesPerSecond { get; }
        public IReadOnlyList<CharacterPredictiveFootRoutePointSnapshot> GroundProbeRoute => m_GroundProbeRoute;
        public IReadOnlyList<CharacterPredictiveFootRoutePointSnapshot> AnimationFootRoute => m_AnimationFootRoute;
        public IReadOnlyList<CharacterPredictiveFootRatePointSnapshot> FootRate => m_FootRate;
        public IReadOnlyList<CharacterPredictiveFootClearanceSegmentSnapshot> ClearancePath => m_ClearancePath;
        public IReadOnlyList<CharacterPredictiveFootEnvelopeSegmentSnapshot> GroundEnvelope => m_GroundEnvelope;
        public IReadOnlyList<CharacterPredictiveFootQueryRequestSnapshot> QueryRequests => m_QueryRequests;
        public IReadOnlyList<CharacterPredictiveFootQueryGeometrySnapshot> AcceptedSupports => m_AcceptedSupports;
        public IReadOnlyList<CharacterPredictiveFootQueryGeometrySnapshot> RejectedGeometry => m_RejectedGeometry;
    }

    public readonly struct CharacterPredictiveFootLegFrameSnapshot
    {
        internal CharacterPredictiveFootLegFrameSnapshot(
            CharacterFootSide side,
            CharacterPredictiveFootPlanState planState,
            float actionProgress,
            float groundPathProgress,
            CharacterPredictiveFootPlanGeometrySnapshot plan,
            bool clearanceEvaluated,
            bool rewritten,
            float requiredLift,
            float appliedLift,
            Vector3 currentPath,
            Vector3 baselineAnkle,
            Vector3 baselineHeel,
            Vector3 baselineToe,
            Vector3 finalAnkle,
            Vector3 finalHeel,
            Vector3 finalToe)
        {
            Side = side;
            PlanState = planState;
            ActionProgress = actionProgress;
            GroundPathProgress = groundPathProgress;
            Plan = plan;
            ClearanceEvaluated = clearanceEvaluated;
            Rewritten = rewritten;
            RequiredLift = requiredLift;
            AppliedLift = appliedLift;
            CurrentPath = currentPath;
            BaselineAnkle = baselineAnkle;
            BaselineHeel = baselineHeel;
            BaselineToe = baselineToe;
            FinalAnkle = finalAnkle;
            FinalHeel = finalHeel;
            FinalToe = finalToe;
        }

        public CharacterFootSide Side { get; }
        public CharacterPredictiveFootPlanState PlanState { get; }
        public float ActionProgress { get; }
        public float GroundPathProgress { get; }
        public CharacterPredictiveFootPlanGeometrySnapshot Plan { get; }
        public bool ClearanceEvaluated { get; }
        public bool Rewritten { get; }
        public float RequiredLift { get; }
        public float AppliedLift { get; }
        public Vector3 CurrentPath { get; }
        public Vector3 BaselineAnkle { get; }
        public Vector3 BaselineHeel { get; }
        public Vector3 BaselineToe { get; }
        public Vector3 FinalAnkle { get; }
        public Vector3 FinalHeel { get; }
        public Vector3 FinalToe { get; }
    }

    public sealed class CharacterPredictiveFootFrameSnapshot
    {
        internal CharacterPredictiveFootFrameSnapshot(
            ActorId actorId,
            ulong frameSequence,
            ulong completionIdentity,
            in CharacterPredictiveFootLegFrameSnapshot left,
            in CharacterPredictiveFootLegFrameSnapshot right)
        {
            ActorId = actorId;
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            Left = left;
            Right = right;
        }

        public ActorId ActorId { get; }
        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public CharacterPredictiveFootLegFrameSnapshot Left { get; }
        public CharacterPredictiveFootLegFrameSnapshot Right { get; }
    }

    public static class CharacterPredictiveFootPlacementDebugSnapshotRegistry
    {
        static readonly Dictionary<ActorId, CharacterPredictiveFootFrameSnapshot> s_Snapshots =
            new Dictionary<ActorId, CharacterPredictiveFootFrameSnapshot>();

        public static bool TryGet(ActorId actorId, out CharacterPredictiveFootFrameSnapshot snapshot) =>
            s_Snapshots.TryGetValue(actorId, out snapshot);

        internal static bool TryFindFrame(ulong frameSequence, out CharacterPredictiveFootFrameSnapshot snapshot)
        {
            foreach (CharacterPredictiveFootFrameSnapshot value in s_Snapshots.Values)
            {
                if (value.FrameSequence != frameSequence)
                    continue;
                snapshot = value;
                return true;
            }
            snapshot = null;
            return false;
        }

        internal static void Publish(CharacterPredictiveFootFrameSnapshot snapshot)
        {
            s_Snapshots[snapshot.ActorId] = snapshot;
        }

        internal static void Remove(ActorId actorId)
        {
            s_Snapshots.Remove(actorId);
        }
    }

    public readonly struct CharacterFootIkCompletedFrameSnapshot
    {
        internal CharacterFootIkCompletedFrameSnapshot(
            ActorId actorId,
            RuntimeFootIkTraceSnapshot trace,
            CharacterPredictiveFootFrameSnapshot predictive)
        {
            ActorId = actorId;
            Trace = trace;
            Predictive = predictive;
        }

        public ActorId ActorId { get; }
        public RuntimeFootIkTraceSnapshot Trace { get; }
        public CharacterPredictiveFootFrameSnapshot Predictive { get; }
        public bool HasPredictiveSnapshot => Predictive != null;
    }

    public static class CharacterFootIkCompletedFrameStream
    {
        public static event Action<CharacterFootIkCompletedFrameSnapshot> Published;

        internal static void Publish(ActorId actorId, RuntimeFootIkTraceSnapshot trace)
        {
            CharacterPredictiveFootPlacementDebugSnapshotRegistry.TryGet(
                    actorId,
                    out CharacterPredictiveFootFrameSnapshot predictive);
            if (predictive != null && predictive.FrameSequence != trace.FrameSequence)
                predictive = null;
            Action<CharacterFootIkCompletedFrameSnapshot> published = Published;
            if (published == null)
                return;
            var completed = new CharacterFootIkCompletedFrameSnapshot(actorId, trace, predictive);
            Delegate[] subscribers = published.GetInvocationList();
            for (int i = 0; i < subscribers.Length; i++)
            {
                try
                {
                    ((Action<CharacterFootIkCompletedFrameSnapshot>)subscribers[i]).Invoke(completed);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
