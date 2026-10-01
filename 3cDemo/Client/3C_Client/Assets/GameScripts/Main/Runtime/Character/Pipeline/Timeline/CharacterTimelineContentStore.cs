using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public sealed class CharacterTimelineContentStore
    {
        readonly Dictionary<string, TimelineData> m_TimelineContent = new Dictionary<string, TimelineData>(StringComparer.Ordinal);
        readonly Dictionary<AnimationProducerId, (string Identity, AnimationChannelId Channel, string Slot)> m_AnimationProducers = new();
        readonly HashSet<(string Timeline, string Clip)> m_MotionWarpBindings = new();
        readonly Guid m_ContentSessionIdentity = Guid.NewGuid();
        TimelineAsset[] m_AuthoringTimelineContent = Array.Empty<TimelineAsset>();
        ulong m_ContentGeneration;
        readonly CharacterTimelineDependencyResolver m_DependencyResolver;
        CharacterPoseNativeSourceResourceCatalog m_AnimationResources;
        TimelineRuntimeNumericTarget m_NumericTarget;
        bool IsInitialized { get; set; }

        public string AuthoringContentRevision { get; private set; } = string.Empty;
        public string ContentRevision { get; private set; } = string.Empty;
        public ulong ContentGeneration => m_ContentGeneration;
        internal event Action<TimelineData, TimelineContentUnit> TimelineInstalled;

        internal CharacterTimelineContentStore(CharacterTimelineDependencyResolver dependencyResolver)
        {
            m_DependencyResolver = dependencyResolver;
        }

        internal void Initialize(TimelineRuntimeNumericTarget numericTarget)
        {
            m_NumericTarget = numericTarget;
            IsInitialized = true;
        }

        internal void Stop() => IsInitialized = false;

        internal void BindAnimationResources(CharacterPoseNativeSourceResourceCatalog resources) =>
            m_AnimationResources = resources;

        internal bool TryGetTimeline(string identity, out TimelineData timeline) =>
            m_TimelineContent.TryGetValue(identity, out timeline);

        public void SetTimelineContent(IReadOnlyList<TimelineAsset> timelines)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline content requires an initialized Timeline content store.");
            if (timelines == null)
                throw new ArgumentNullException(nameof(timelines));
            if (!TryFreezeContent(timelines, out List<CharacterTimelineContentSnapshot> snapshots,
                    out string authoringRevision, out string contentRevision, out List<string> errors))
                throw new InvalidOperationException(string.Join(" | ", errors));
            m_AuthoringTimelineContent = timelines.ToArray();
            InstallTimelineContent(snapshots, authoringRevision, contentRevision);
        }

        void InstallTimelineContent(
            IReadOnlyList<CharacterTimelineContentSnapshot> snapshots,
            string authoringRevision,
            string contentRevision)
        {
            m_TimelineContent.Clear();
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterTimelineContentSnapshot snapshot = snapshots[i];
                TimelineData timeline = snapshot.CloneData();
                m_TimelineContent.Add(timeline.AuthoringId, timeline);
                timeline.Init();
            }
            if (m_ContentGeneration == 0)
                foreach (TimelineData timeline in m_TimelineContent.Values)
                    PrepareCompiledBindings(timeline);
            AuthoringContentRevision = authoringRevision;
            m_DependencyResolver.InstallContent(m_TimelineContent.Values, m_NumericTarget);
            for (int index = 0; index < snapshots.Count; index++)
            {
                CharacterTimelineContentSnapshot snapshot = snapshots[index];
                TimelineInstalled(m_TimelineContent[snapshot.TimelineAuthoringId], snapshot.Content);
            }
            ContentRevision = contentRevision;
            m_ContentGeneration = checked(m_ContentGeneration + 1);
        }

        void PrepareCompiledBindings(TimelineData timeline)
        {
            for (int i = 0; i < timeline.Tracks.Count; i++)
            {
                Track track = timeline.Tracks[i];
                if (track is AnimationTrack animation)
                {
                    var producerId = new AnimationProducerId(timeline.AuthoringId, track.AuthoringId);
                    m_AnimationProducers.Add(producerId,
                        (producerId.ProgramProducerIdentity, animation.AnimationChannelId, animation.AnimationSlotId));
                }
                if (track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int j = 0; j < track.Clips.Count; j++)
                    if (track.Clips[j] is MotionWarpClip warp && warp.ExecutionDomain == TimelineExecutionDomain.Logic)
                        m_MotionWarpBindings.Add((timeline.AuthoringId, warp.AuthoringId));
            }
        }

        internal string RequireAnimationProducerIdentity(AnimationProducerId producerId) =>
            m_AnimationProducers.TryGetValue(producerId, out var binding)
                ? binding.Identity : throw new InvalidOperationException($"Timeline animation producer '{producerId}' is not prepared.");

        public bool TryExportContent(
            out CharacterTimelineContentExport export,
            out string error)
        {
            export = null;
            error = string.Empty;
            if (!IsInitialized)
            {
                error = "Timeline content export requires an initialized Timeline content store.";
                return false;
            }
            if (!TryFreezeContent(
                    m_AuthoringTimelineContent,
                    out List<CharacterTimelineContentSnapshot> snapshots,
                    out string authoringRevision,
                    out string contentRevision,
                    out List<string> errors))
            {
                error = string.Join(" | ", errors);
                return false;
            }
            export = new CharacterTimelineContentExport(snapshots, authoringRevision, contentRevision);
            return true;
        }

        public bool TryPrepareContentAdoption(
            CharacterTimelineContentExport export,
            out CharacterTimelineContentAdoptionPlan plan,
            out string error)
        {
            plan = null;
            error = string.Empty;
            if (!IsInitialized)
            {
                error = "Timeline content adoption requires an initialized Timeline content store.";
                return false;
            }
            if (export == null)
            {
                error = "请先导出 Timeline 内容。";
                return false;
            }
            if (!TryValidateCompiledBindings(export.Snapshots, out error))
                return false;
            plan = new CharacterTimelineContentAdoptionPlan(
                export,
                m_ContentSessionIdentity,
                m_ContentGeneration,
                string.Equals(ContentRevision, export.ContentRevision, StringComparison.Ordinal)
                    ? "Timeline content is already adopted."
                    : "Timeline content is prepared for adoption at the next formal playback boundary.");
            return true;
        }

        public bool TryAdoptContent(
            CharacterTimelineContentPublication publication,
            out CharacterTimelineContentAdoptionReport report)
        {
            if (!IsInitialized)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    publication?.AuthoringRevision,
                    publication?.ContentRevision,
                    "Timeline content adoption requires an initialized Timeline content store.");
                return false;
            }
            if (publication == null)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    publication?.AuthoringRevision,
                    publication?.ContentRevision,
                    "Timeline content publication is invalid.");
                return false;
            }
            CharacterTimelineContentAdoptionPlan plan = publication.Plan;
            if (!TryValidateCurrentPlan(plan, out string validationError))
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    plan.AuthoringRevision,
                    plan.ContentRevision,
                    validationError);
                return false;
            }
            InstallTimelineContent(plan.Snapshots, plan.AuthoringRevision, plan.ContentRevision);
            report = new CharacterTimelineContentAdoptionReport(
                CharacterTimelineContentAdoptionState.Adopted,
                AuthoringContentRevision,
                ContentRevision,
                "Timeline content adopted for future formal playback activations.");
            return true;
        }

        public bool TryPublishContentAdoption(
            CharacterTimelineContentAdoptionPlan plan,
            out CharacterTimelineContentPublication publication,
            out string error)
        {
            publication = null;
            error = string.Empty;
            if (!IsInitialized)
            {
                error = "Timeline content publication requires an initialized Timeline content store.";
                return false;
            }
            if (plan == null)
            {
                error = "请先准备 Timeline 内容。";
                return false;
            }
            publication = new CharacterTimelineContentPublication(
                plan,
                "Timeline content publication is sealed for the next formal adoption boundary.");
            return true;
        }

        bool TryValidateCurrentPlan(
            CharacterTimelineContentAdoptionPlan plan,
            out string error)
        {
            if (plan.SessionIdentity != m_ContentSessionIdentity)
            {
                error = "Timeline content adoption plan belongs to another Session.";
                return false;
            }
            if (plan.SessionContentGeneration != m_ContentGeneration)
            {
                error = "Timeline content changed after preparation; export again before adoption.";
                return false;
            }
            return TryValidateCurrentExport(plan.Export, out error);
        }

        bool TryValidateCurrentExport(
            CharacterTimelineContentExport export,
            out string error)
        {
            if (!TryGetCurrentContentRevisions(
                    out string authoringRevision,
                    out string contentRevision,
                    out List<string> errors))
            {
                error = string.Join(" | ", errors);
                return false;
            }
            if (!string.Equals(export.AuthoringRevision, authoringRevision, StringComparison.Ordinal) ||
                !string.Equals(export.ContentRevision, contentRevision, StringComparison.Ordinal))
            {
                error = "Timeline authoring changed after export; export again before preparing or adopting.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        bool TryValidateCompiledBindings(IReadOnlyList<CharacterTimelineContentSnapshot> snapshots, out string error)
        {
            error = string.Empty;
            if (snapshots.Count != m_TimelineContent.Count)
            {
                error = "Timeline 集合发生变化，需要重新 Build 其正式调用图。";
                return false;
            }
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterTimelineContentSnapshot snapshot = snapshots[i];
                if (!m_TimelineContent.ContainsKey(snapshot.TimelineAuthoringId))
                {
                    error = $"Timeline '{snapshot.TimelineAuthoringId}' 缺少已编译的调用入口，需要重新 Build。";
                    return false;
                }
                if (!m_DependencyResolver.TryValidateGraphBindings(snapshot.Content, out error))
                    return false;
                TimelineData candidate = snapshot.CloneData();
                for (int trackIndex = 0; trackIndex < candidate.Tracks.Count; trackIndex++)
                {
                    Track track = candidate.Tracks[trackIndex];
                    if (track.PersistentMuted)
                        continue;
                    if (track is AnimationTrack animation && track.Clips.Count != 0)
                    {
                        var producerId = new AnimationProducerId(candidate.AuthoringId, track.AuthoringId);
                        if (!m_AnimationProducers.TryGetValue(producerId, out var binding) ||
                            !binding.Channel.Equals(animation.AnimationChannelId) || binding.Slot != animation.AnimationSlotId)
                        {
                            error = $"动画 Track '{track.AuthoringId}' 的 producer、Channel 或 Slot 未在当前 Pose 装配中准备，需要重新 Build Pose 绑定。";
                            return false;
                        }
                        for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                        {
                            var clip = (BTSMTL.Timeline.AnimationClip)track.Clips[clipIndex];
                            if (m_AnimationResources.HasActionPlan(clip.Clip))
                                continue;
                            error = $"动画资源 '{clip.Clip.name}' 未在当前 Pose 资源目录中准备，需要重新 Build 动画资源。";
                            return false;
                        }
                    }
                    if (track.ExecutionDomain != TimelineExecutionDomain.Logic)
                        continue;
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        if (track.Clips[clipIndex] is not MotionWarpClip warp || warp.ExecutionDomain != TimelineExecutionDomain.Logic ||
                            m_MotionWarpBindings.Contains((candidate.AuthoringId, warp.AuthoringId)))
                            continue;
                        error = $"MotionWarp '{warp.AuthoringId}' 缺少当前技能的状态绑定，需要重新 Build 技能运行数据。";
                        return false;
                    }
                }
            }
            return true;
        }

        public bool TryGetCurrentAuthoringContentRevision(
            out string authoringRevision,
            out string error)
        {
            if (TryGetCurrentContentRevisions(out authoringRevision, out _, out List<string> errors))
            {
                error = string.Empty;
                return true;
            }
            error = string.Join(" | ", errors);
            return false;
        }

        bool TryGetCurrentContentRevisions(
            out string authoringRevision,
            out string contentRevision,
            out List<string> errors)
        {
            return TryFreezeContent(
                m_AuthoringTimelineContent,
                out _,
                out authoringRevision,
                out contentRevision,
                out errors);
        }

        bool TryFreezeContent(
            IReadOnlyList<TimelineAsset> timelines,
            out List<CharacterTimelineContentSnapshot> snapshots,
            out string authoringRevision,
            out string contentRevision,
            out List<string> errors)
        {
            authoringRevision = string.Empty;
            contentRevision = string.Empty;
            snapshots = new List<CharacterTimelineContentSnapshot>();
            errors = new List<string>();
            if (timelines == null || timelines.Count == 0)
            {
                errors.Add("Timeline content list is empty.");
                return false;
            }
            TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var authoringParts = new List<string>(timelines.Count);
            var contentParts = new List<string>(timelines.Count);
            for (int i = 0; i < timelines.Count; i++)
            {
                TimelineAsset asset = timelines[i];
                if (!asset || asset.Data == null)
                {
                    errors.Add($"Timeline content #{i} is missing.");
                    continue;
                }
                if (!identities.Add(asset.Data.AuthoringId))
                {
                    errors.Add($"Timeline content identity '{asset.Data.AuthoringId}' is duplicated.");
                    continue;
                }
                TimelineData frozen = asset.Data.Clone();
                frozen.Init();
#if UNITY_EDITOR
                TimelineContentDiscoveryResult discovery = TimelineContentDiscovery.Discover(frozen, catalog);
#else
                TimelineContentDiscoveryResult discovery = TimelineContentDiscovery.Discover(frozen, catalog, m_DependencyResolver);
#endif
                if (!discovery.IsValid)
                {
                    for (int errorIndex = 0; errorIndex < discovery.Errors.Count; errorIndex++)
                        errors.Add($"{asset.name}: {discovery.Errors[errorIndex]}");
                    continue;
                }
                authoringParts.Add($"{asset.Data.AuthoringId}:{TimelineAuthoringFingerprint.Compute(asset.Data)}");
                contentParts.Add($"{asset.Data.AuthoringId}:{discovery.Content.ContentHash}");
                snapshots.Add(new CharacterTimelineContentSnapshot(frozen, discovery.Content));
            }
            if (errors.Count != 0)
                return false;
            authoringRevision = SourceContentHasher.Hash(authoringParts.ToArray());
            contentRevision = SourceContentHasher.Hash(contentParts.ToArray());
            return true;
        }

    }
}
