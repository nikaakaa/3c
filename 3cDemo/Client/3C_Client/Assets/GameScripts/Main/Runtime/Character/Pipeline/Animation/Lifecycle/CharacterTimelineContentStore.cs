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
        readonly Dictionary<AnimationProducerId, string> m_AnimationProducerIdentities = new();
        readonly Guid m_ContentSessionIdentity = Guid.NewGuid();
        TimelineAsset[] m_AuthoringTimelineContent = Array.Empty<TimelineAsset>();
        ulong m_ContentGeneration;
        readonly CharacterTimelineDependencyResolver m_DependencyResolver;
        TimelineRuntimeNumericTarget m_NumericTarget;
        bool IsInitialized { get; set; }

        public string AuthoringContentRevision { get; private set; } = string.Empty;
        public string ContentRevision { get; private set; } = string.Empty;
        public ulong ContentGeneration => m_ContentGeneration;

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

        internal bool TryGetTimeline(string identity, out TimelineData timeline) =>
            m_TimelineContent.TryGetValue(identity, out timeline);

        public void SetTimelineContent(IReadOnlyList<TimelineAsset> timelines)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline content requires an initialized Timeline content store.");
            if (timelines == null)
                throw new ArgumentNullException(nameof(timelines));
            if (!TryComputeContentRevisions(timelines, out string authoringRevision, out string contentRevision, out List<string> errors))
                throw new InvalidOperationException(string.Join(" | ", errors));
            if (!TryFreezeContent(timelines, out List<CharacterTimelineContentSnapshot> snapshots, out errors))
                throw new InvalidOperationException(string.Join(" | ", errors));
            m_AuthoringTimelineContent = timelines.ToArray();
            InstallTimelineContent(snapshots, authoringRevision, contentRevision);
        }

        void InstallTimelineContent(
            IReadOnlyList<CharacterTimelineContentSnapshot> snapshots,
            string authoringRevision,
            string contentRevision)
        {
            if (snapshots == null || snapshots.Count == 0)
                throw new ArgumentException("Timeline content snapshots are required.", nameof(snapshots));
            m_TimelineContent.Clear();
            m_AnimationProducerIdentities.Clear();
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterTimelineContentSnapshot snapshot = snapshots[i];
                TimelineData timeline = snapshot?.CloneData();
                if (timeline == null)
                    throw new InvalidOperationException("Timeline content snapshot is invalid.");
                if (!m_TimelineContent.TryAdd(timeline.AuthoringId, timeline))
                    throw new InvalidOperationException($"Timeline content identity '{timeline.AuthoringId}' is duplicated.");
                PrepareAnimationProducerIdentities(timeline);
            }
            AuthoringContentRevision = authoringRevision ?? string.Empty;
            m_DependencyResolver.InstallContent(m_TimelineContent.Values, m_NumericTarget);
            ContentRevision = contentRevision ?? string.Empty;
            m_ContentGeneration = checked(m_ContentGeneration + 1);
        }

        void PrepareAnimationProducerIdentities(TimelineData timeline)
        {
            for (int i = 0; i < timeline.Tracks.Count; i++)
            {
                if (timeline.Tracks[i] is not AnimationTrack track)
                    continue;
                var producerId = new AnimationProducerId(timeline.AuthoringId, track.AuthoringId);
                m_AnimationProducerIdentities.Add(producerId, producerId.ProgramProducerIdentity);
            }
        }

        internal string RequireAnimationProducerIdentity(AnimationProducerId producerId) =>
            m_AnimationProducerIdentities.TryGetValue(producerId, out string identity)
                ? identity : throw new InvalidOperationException($"Timeline animation producer '{producerId}' is not prepared.");

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
            if (!TryGetCurrentContentRevisions(
                    out string authoringRevision,
                    out string contentRevision,
                    out List<string> errors))
            {
                error = string.Join(" | ", errors);
                return false;
            }
            if (!TryFreezeContent(m_AuthoringTimelineContent, out List<CharacterTimelineContentSnapshot> snapshots, out errors))
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
            if (!TryValidateCurrentExport(export, out error))
                return false;
            if (!HasCompatibleContentTopology(export.Snapshots))
            {
                error = "Timeline content topology or contract changed; a new Session is required.";
                return false;
            }
            plan = new CharacterTimelineContentAdoptionPlan(
                export,
                m_ContentSessionIdentity,
                m_ContentGeneration,
                true,
                string.Equals(ContentRevision, export.ContentRevision, StringComparison.Ordinal)
                    ? "Timeline content is already adopted."
                    : "Timeline content is prepared for adoption at the next formal playback boundary.");
            return true;
        }

        public bool TryAdoptContent(
            CharacterTimelineContentPublication publication,
            out CharacterTimelineContentAdoptionReport report)
        {
            CharacterTimelineContentAdoptionPlan plan = publication?.Plan;
            if (!IsInitialized)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    publication?.AuthoringRevision,
                    publication?.ContentRevision,
                    "Timeline content adoption requires an initialized Timeline content store.");
                return false;
            }
            if (publication == null || !publication.IsValid)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    publication?.AuthoringRevision,
                    publication?.ContentRevision,
                    "Timeline content publication is invalid.");
                return false;
            }
            if (plan == null || !plan.IsValid || !plan.IsCompatible)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    plan?.AuthoringRevision,
                    plan?.ContentRevision,
                    "Timeline content adoption plan is invalid or incompatible.");
                return false;
            }
            if (!TryValidateCurrentPlan(plan, out string validationError))
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    plan.AuthoringRevision,
                    plan.ContentRevision,
                    validationError);
                return false;
            }
            if (!HasCompatibleContentTopology(plan.Snapshots))
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    plan.AuthoringRevision,
                    plan.ContentRevision,
                    "Timeline content topology or contract changed after preparation; a new Session is required.");
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
            if (plan == null || !plan.IsValid || !plan.IsCompatible)
            {
                error = "Timeline content adoption plan is invalid or incompatible.";
                return false;
            }
            if (!TryValidateCurrentPlan(plan, out error))
                return false;
            if (!HasCompatibleContentTopology(plan.Snapshots))
            {
                error = "Timeline content topology or contract changed; a new Session is required.";
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
            if (plan == null || !plan.IsValid || !plan.IsCompatible)
            {
                error = "Timeline content adoption plan is invalid or incompatible.";
                return false;
            }
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
            if (export == null || !export.IsValid)
            {
                error = "Timeline content export is invalid.";
                return false;
            }
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

        bool HasCompatibleContentTopology(IReadOnlyList<CharacterTimelineContentSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count != m_TimelineContent.Count)
                return false;
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterTimelineContentSnapshot snapshot = snapshots[i];
                if (snapshot == null ||
                    !m_TimelineContent.TryGetValue(snapshot.TimelineAuthoringId, out TimelineData installed) ||
                    !HasSameTopology(installed, snapshot.CloneData()))
                    return false;
            }
            return true;
        }

        static bool HasSameTopology(TimelineData installed, TimelineData candidate)
        {
            if (installed == null || candidate == null || installed.Tracks.Count != candidate.Tracks.Count)
                return false;
            for (int trackIndex = 0; trackIndex < installed.Tracks.Count; trackIndex++)
            {
                Track installedTrack = installed.Tracks[trackIndex];
                Track candidateTrack = candidate.Tracks[trackIndex];
                if (installedTrack == null || candidateTrack == null ||
                    !string.Equals(installedTrack.AuthoringId, candidateTrack.AuthoringId, StringComparison.Ordinal) ||
                    !string.Equals(installedTrack.ContractKind, candidateTrack.ContractKind, StringComparison.Ordinal) ||
                    installedTrack.Clips.Count != candidateTrack.Clips.Count)
                    return false;
                for (int clipIndex = 0; clipIndex < installedTrack.Clips.Count; clipIndex++)
                {
                    Clip installedClip = installedTrack.Clips[clipIndex];
                    Clip candidateClip = candidateTrack.Clips[clipIndex];
                    if (installedClip == null || candidateClip == null ||
                        !string.Equals(installedClip.AuthoringId, candidateClip.AuthoringId, StringComparison.Ordinal) ||
                        !string.Equals(installedClip.ContractKind, candidateClip.ContractKind, StringComparison.Ordinal))
                        return false;
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
            return TryComputeContentRevisions(
                m_AuthoringTimelineContent,
                out authoringRevision,
                out contentRevision,
                out errors);
        }

        static bool TryFreezeContent(
            IReadOnlyList<TimelineAsset> timelines,
            out List<CharacterTimelineContentSnapshot> snapshots,
            out List<string> errors)
        {
            snapshots = new List<CharacterTimelineContentSnapshot>();
            errors = new List<string>();
            if (timelines == null || timelines.Count == 0)
            {
                errors.Add("Timeline content list is empty.");
                return false;
            }
            var identities = new HashSet<string>(StringComparer.Ordinal);
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
                snapshots.Add(new CharacterTimelineContentSnapshot(asset.Data));
            }
            return errors.Count == 0;
        }

        static bool TryComputeContentRevisions(
            IReadOnlyList<TimelineAsset> timelines,
            out string authoringRevision,
            out string contentRevision,
            out List<string> errors)
        {
            authoringRevision = string.Empty;
            contentRevision = string.Empty;
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
                TimelineContentDiscoveryResult discovery = TimelineContentDiscovery.Discover(asset, catalog);
                if (!discovery.IsValid)
                {
                    for (int errorIndex = 0; errorIndex < discovery.Errors.Count; errorIndex++)
                        errors.Add($"{asset.name}: {discovery.Errors[errorIndex]}");
                    continue;
                }
                authoringParts.Add($"{asset.Data.AuthoringId}:{TimelineAuthoringFingerprint.Compute(asset.Data)}");
                contentParts.Add($"{asset.Data.AuthoringId}:{discovery.Content.ContentHash}");
            }
            if (errors.Count != 0)
                return false;
            authoringRevision = SourceContentHasher.Hash(authoringParts.ToArray());
            contentRevision = SourceContentHasher.Hash(contentParts.ToArray());
            return true;
        }

    }
}
