using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct AnimationPresentationIdentity : IEquatable<AnimationPresentationIdentity>
    {
        public AnimationPresentationIdentity(
            string projectionRevision,
            string poseGraphId,
            string poseGraphRevision,
            string posePlanHash)
        {
            if (string.IsNullOrWhiteSpace(projectionRevision) ||
                string.IsNullOrWhiteSpace(poseGraphId) ||
                string.IsNullOrWhiteSpace(poseGraphRevision) ||
                string.IsNullOrWhiteSpace(posePlanHash))
            {
                throw new ArgumentException("Animation Presentation identity is incomplete.");
            }
            ProjectionRevision = projectionRevision.Trim();
            PoseGraphId = poseGraphId.Trim();
            PoseGraphRevision = poseGraphRevision.Trim();
            PosePlanHash = posePlanHash.Trim();
        }

        public AnimationPresentationIdentity(CharacterPresentationProjection projection)
            : this(
                projection?.ProjectionRevision,
                projection?.PosePlan?.PoseGraphId,
                projection?.PosePlan?.ContentRevision,
                projection?.PosePlan?.PlanHash)
        {
            projection.RequirePosePayload();
        }

        public string ProjectionRevision { get; }
        public string PoseGraphId { get; }
        public string PoseGraphRevision { get; }
        public string PosePlanHash { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProjectionRevision) &&
            !string.IsNullOrWhiteSpace(PoseGraphId) &&
            !string.IsNullOrWhiteSpace(PoseGraphRevision) &&
            !string.IsNullOrWhiteSpace(PosePlanHash);

        public bool Equals(AnimationPresentationIdentity other) =>
            string.Equals(ProjectionRevision, other.ProjectionRevision, StringComparison.Ordinal) &&
            string.Equals(PoseGraphId, other.PoseGraphId, StringComparison.Ordinal) &&
            string.Equals(PoseGraphRevision, other.PoseGraphRevision, StringComparison.Ordinal) &&
            string.Equals(PosePlanHash, other.PosePlanHash, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is AnimationPresentationIdentity other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            ProjectionRevision,
            PoseGraphId,
            PoseGraphRevision,
            PosePlanHash);
    }

    [Flags]
    public enum AnimationPresentationDiagnosticsInterest : byte
    {
        None = 0,
        LiveState = 1 << 0,
        Capture = 1 << 1,
        OperationDetail = 1 << 2,
        FinalPoseDetail = 1 << 3,
        PoseWatch = 1 << 4
    }

    public interface IAnimationPresentationRuntimeSnapshotProvider
    {
        bool MotionMatchingRuntimeEnabled { get; }
        AnimationPresentationDiagnosticsInterest DiagnosticsInterest { get; }
        bool TryGetAnimationPresentationDebugView(
            out AnimationPresentationDebugView debugView);
        bool TryGetPosePlanStages(out CharacterPosePlanStageSnapshot snapshot);
        bool TryCaptureMotionMatchingSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact);
        void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest);
        void RemoveDiagnosticsInterest(Guid ownerId);
        void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests);
        void RemovePoseWatchInterests(Guid ownerId);
    }

    public interface IAnimationPresentationRuntimeResetController
    {
        void Reset();
    }

    public sealed class AnimationPresentationRuntimeTarget
    {
        IAnimationPresentationRuntimeSnapshotProvider m_Provider;

        public AnimationPresentationRuntimeTarget(
            Guid runtimeInstanceId,
            ActorId actorId,
            int hostInstanceId,
            string displayName,
            AnimationPresentationIdentity presentationIdentity,
            IAnimationPresentationRuntimeSnapshotProvider provider)
        {
            if (runtimeInstanceId == Guid.Empty || !actorId.IsValid || hostInstanceId == 0 ||
                string.IsNullOrWhiteSpace(displayName) || !presentationIdentity.IsValid)
            {
                throw new ArgumentException("Animation Presentation runtime target identity is incomplete.");
            }
            RuntimeInstanceId = runtimeInstanceId;
            ActorId = actorId;
            HostInstanceId = hostInstanceId;
            DisplayName = displayName.Trim();
            PresentationIdentity = presentationIdentity;
            m_Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public void Replace(
            AnimationPresentationIdentity presentationIdentity,
            IAnimationPresentationRuntimeSnapshotProvider provider)
        {
            if (!presentationIdentity.IsValid)
                throw new ArgumentException("Animation Presentation replacement identity is invalid.", nameof(presentationIdentity));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            m_Provider = provider;
            PresentationIdentity = presentationIdentity;
        }

        public Guid RuntimeInstanceId { get; }
        public ActorId ActorId { get; }
        public int HostInstanceId { get; }
        public string DisplayName { get; }
        public AnimationPresentationIdentity PresentationIdentity { get; private set; }
        public string ProjectionRevision => PresentationIdentity.ProjectionRevision;
        public bool MotionMatchingRuntimeEnabled => m_Provider.MotionMatchingRuntimeEnabled;
        public AnimationPresentationDiagnosticsInterest DiagnosticsInterest =>
            m_Provider.DiagnosticsInterest;
        public bool CanReset => m_Provider is IAnimationPresentationRuntimeResetController;
        public bool TryGetDebugView(
            out AnimationPresentationDebugView debugView)
        {
            if (!m_Provider
                .TryGetAnimationPresentationDebugView(
                    out debugView))
            {
                return false;
            }
            AnimationPresentationRuntimeSnapshot snapshot = debugView.PosePlan;
            var liveIdentity = new AnimationPresentationIdentity(
                snapshot.ProjectionRevision,
                snapshot.PoseGraphId,
                snapshot.PoseGraphRevision,
                snapshot.PosePlanHash);
            if (!PresentationIdentity.Equals(liveIdentity))
            {
                throw new InvalidOperationException(
                    "Animation Presentation live identity changed after target binding.");
            }
            return true;
        }

        public bool TryGetPosePlanStages(out CharacterPosePlanStageSnapshot snapshot) =>
            m_Provider.TryGetPosePlanStages(out snapshot);

        public bool TryCaptureMotionMatchingSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact) =>
            m_Provider.TryCaptureMotionMatchingSearchReplay(providerId, out artifact);

        public void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest) =>
            m_Provider.SetDiagnosticsInterest(ownerId, interest);

        public void RemoveDiagnosticsInterest(Guid ownerId) =>
            m_Provider.RemoveDiagnosticsInterest(ownerId);

        public void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_Provider.SetPoseWatchInterests(ownerId, interests);

        public void RemovePoseWatchInterests(Guid ownerId) => m_Provider.RemovePoseWatchInterests(ownerId);

        public void Reset()
        {
            if (m_Provider is not IAnimationPresentationRuntimeResetController resetController)
                throw new InvalidOperationException(
                    "Animation Presentation runtime target does not expose a reset controller.");
            resetController.Reset();
        }
    }

    public static class AnimationPresentationRuntimeTargetRegistry
    {
        static readonly List<AnimationPresentationRuntimeTarget> s_Targets =
            new List<AnimationPresentationRuntimeTarget>();

        public static event Action<AnimationPresentationRuntimeTarget> TargetRegistered;
        public static event Action<AnimationPresentationRuntimeTarget> TargetUnregistered;
        public static IReadOnlyList<AnimationPresentationRuntimeTarget> Targets => s_Targets;

        public static void Register(AnimationPresentationRuntimeTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            for (int i = 0; i < s_Targets.Count; i++)
            {
                if (s_Targets[i].RuntimeInstanceId == target.RuntimeInstanceId)
                    throw new InvalidOperationException($"Animation Presentation runtime target is already registered: {target.RuntimeInstanceId:N}.");
                if (s_Targets[i].ActorId == target.ActorId)
                    throw new InvalidOperationException($"Animation Presentation Actor target is already registered: {target.ActorId}.");
            }
            s_Targets.Add(target);
            try
            {
                TargetRegistered?.Invoke(target);
            }
            catch
            {
                s_Targets.Remove(target);
                throw;
            }
        }

        public static void Unregister(AnimationPresentationRuntimeTarget target)
        {
            if (target == null || !s_Targets.Remove(target))
                return;
            TargetUnregistered?.Invoke(target);
        }

        public static bool TryGet(Guid runtimeInstanceId, out AnimationPresentationRuntimeTarget target)
        {
            for (int i = 0; i < s_Targets.Count; i++)
            {
                if (s_Targets[i].RuntimeInstanceId != runtimeInstanceId)
                    continue;
                target = s_Targets[i];
                return true;
            }
            target = null;
            return false;
        }

        public static bool TryGet(ActorId actorId, out AnimationPresentationRuntimeTarget target)
        {
            for (int i = 0; i < s_Targets.Count; i++)
            {
                if (s_Targets[i].ActorId != actorId)
                    continue;
                target = s_Targets[i];
                return true;
            }
            target = null;
            return false;
        }
    }
}
