using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public interface IAnimationPresentationRuntimeSnapshotProvider
    {
        bool TryGetAnimationPresentationSnapshot(out AnimationPresentationRuntimeSnapshot snapshot);
    }

    public sealed class AnimationPresentationRuntimeTarget
    {
        readonly IAnimationPresentationRuntimeSnapshotProvider m_Provider;

        public AnimationPresentationRuntimeTarget(
            Guid runtimeInstanceId,
            int hostInstanceId,
            string displayName,
            string projectionRevision,
            IAnimationPresentationRuntimeSnapshotProvider provider)
        {
            if (runtimeInstanceId == Guid.Empty || hostInstanceId == 0 ||
                string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(projectionRevision))
            {
                throw new ArgumentException("Animation Presentation runtime target identity is incomplete.");
            }
            RuntimeInstanceId = runtimeInstanceId;
            HostInstanceId = hostInstanceId;
            DisplayName = displayName.Trim();
            ProjectionRevision = projectionRevision.Trim();
            m_Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Guid RuntimeInstanceId { get; }
        public int HostInstanceId { get; }
        public string DisplayName { get; }
        public string ProjectionRevision { get; }

        public bool TryGetSnapshot(out AnimationPresentationRuntimeSnapshot snapshot)
        {
            if (!m_Provider.TryGetAnimationPresentationSnapshot(out snapshot))
                return false;
            if (!string.Equals(snapshot.ProjectionRevision, ProjectionRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Animation Presentation runtime snapshot Projection revision changed after target binding.");
            return true;
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
    }
}
