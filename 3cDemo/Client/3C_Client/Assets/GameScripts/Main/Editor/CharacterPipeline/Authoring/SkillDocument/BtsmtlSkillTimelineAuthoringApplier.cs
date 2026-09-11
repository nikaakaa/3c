using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill
{
    internal sealed class BtsmtlSkillTimelineAuthoringApplier : ITimelineAuthoringClipResolver
    {
        readonly AgentMutationSession m_Session;
        readonly AgentPackageSkillFlowDocument m_Document;
        readonly BtsmtlSkillGraphClosureIndex m_Current;
        readonly IDictionary<string, TimelineAsset> m_Timelines =
            new Dictionary<string, TimelineAsset>(StringComparer.Ordinal);
        readonly IDictionary<string, TimelineAsset> m_LocalTimelines =
            new Dictionary<string, TimelineAsset>(StringComparer.Ordinal);
        readonly IDictionary<string, Clip> m_LocalClips =
            new Dictionary<string, Clip>(StringComparer.Ordinal);
        readonly Func<string, FlowGraph> m_ResolveGraph;

        public BtsmtlSkillTimelineAuthoringApplier(
            AgentMutationSession session,
            AgentPackageSkillFlowDocument document,
            BtsmtlSkillGraphClosureIndex current,
            Func<string, FlowGraph> resolveGraph)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            m_Current = current ?? throw new ArgumentNullException(nameof(current));
            m_ResolveGraph = resolveGraph ?? throw new ArgumentNullException(nameof(resolveGraph));
        }

        public void Resolve()
        {
            m_LocalClips.Clear();
            ResolveTimelines();
            RehomeTimelineBodyGraphs();
        }

        public void Sync()
        {
            SyncTimelines();
        }

        public void DeleteRemoved()
        {
            DeleteRemovedTimelines();
        }

        public TimelineAsset ResolveTimeline(string identity)
        {
            if (m_Timelines.TryGetValue(identity, out TimelineAsset asset))
                return asset;
            if (m_LocalTimelines.TryGetValue(identity, out asset))
                return asset;
            throw new InvalidOperationException($"Skill Timeline local identity无法解析：{identity}");
        }

        void ResolveTimelines()
        {
            foreach (AgentPackageSkillTimelineFile target in m_Document.timelines.OrderBy(value => value.id, StringComparer.Ordinal))
            {
                TimelineAsset existing = ResolveExistingTimeline(target.id);
                if (existing)
                {
                    m_Timelines[target.id] = existing;
                    m_Session.Touch(existing);
                    continue;
                }
                TimelineAsset asset = target.asset != null &&
                    m_Session.Resolver.TryResolveSkillObject(target.asset, out TimelineAsset resolved)
                        ? resolved
                        : null;
                if (!asset)
                {
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"Skill Timeline '{target.id}'的正式资产引用无法解析。");
                    asset = ScriptableObject.CreateInstance<TimelineAsset>();
                    asset.name = string.IsNullOrWhiteSpace(target.name) ? "Skill Timeline" : target.name;
                    UnityEngine.Object owner = m_ResolveGraph(target.ownerGraphId);
                    string ownerPath = AssetDatabase.GetAssetPath(owner);
                    if (string.IsNullOrEmpty(ownerPath) ||
                        AssetDatabase.LoadMainAssetAtPath(ownerPath) is not IBtsmtlSkillFlowGraph)
                        throw new InvalidOperationException($"Skill Timeline '{target.id}' owner资产无法解析。");
                    AssetDatabase.AddObjectToAsset(asset, ownerPath);
                    Undo.RegisterCreatedObjectUndo(asset, "创建技能Timeline");
                    asset.SetData(TimelineData.CreateDefault(asset.name));
                }
                m_Timelines[target.id] = asset;
                if (target.id.StartsWith("local:", StringComparison.Ordinal))
                    m_LocalTimelines[target.id] = asset;
                m_Session.Touch(asset);
            }
        }

        void RehomeTimelineBodyGraphs()
        {
            foreach (AgentPackageSkillFlowGraphFile target in m_Document.graphs)
            {
                if (target.role != BtsmtlSkillFlowGraphRole.TimelineBody.ToString() ||
                    target.owner?.kind != "timeline-clip" ||
                    m_ResolveGraph(target.id) is not FlowGraph graph ||
                    !m_Timelines.TryGetValue(target.owner.timelineId, out TimelineAsset timeline))
                    continue;
                string timelinePath = AssetDatabase.GetAssetPath(timeline);
                if (string.IsNullOrEmpty(timelinePath))
                    throw new InvalidOperationException($"TimelineBody '{target.id}'的Timeline owner没有正式资产路径。");
                string sourcePath = AssetDatabase.GetAssetPath(graph);
                if (sourcePath == timelinePath || AssetDatabase.IsMainAsset(graph))
                    continue;
                if (AssetDatabase.IsSubAsset(graph))
                    AssetDatabase.RemoveObjectFromAsset(graph);
                AssetDatabase.AddObjectToAsset(graph, timelinePath);
                m_Session.RegisterRollback(() => RestoreAssetOwner(graph, sourcePath));
            }
        }

        static void RestoreAssetOwner(FlowGraph graph, string assetPath)
        {
            if (!graph || AssetDatabase.GetAssetPath(graph) == assetPath)
                return;
            if (!File.Exists(assetPath))
                throw new InvalidOperationException($"无法恢复技能Graph原owner：{assetPath}");
            AssetDatabase.RemoveObjectFromAsset(graph);
            AssetDatabase.AddObjectToAsset(graph, assetPath);
            EditorUtility.SetDirty(graph);
        }

        void SyncTimelines()
        {
            foreach (AgentPackageSkillTimelineFile target in m_Document.timelines)
            {
                TimelineAsset asset = ResolveTimeline(target.id);
                TimelineData timeline = asset.Data;
                timeline.Name = target.name ?? timeline.Name;
                SyncTimelineBindings(timeline, target.externalBindings);
                TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
                var targetTrackIds = (target.tracks ?? new List<AgentPackageSkillTimelineTrack>()).Select(value => value.id).ToHashSet(StringComparer.Ordinal);
                foreach (Track existing in timeline.Tracks.ToArray())
                    if (!targetTrackIds.Contains(existing.AuthoringId))
                        timeline.RemoveTrack(existing);
                var resolvedTracks = new List<Track>();
                foreach (AgentPackageSkillTimelineTrack targetTrack in target.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                {
                    Track track = timeline.Tracks.FirstOrDefault(value => value.AuthoringId == targetTrack.id);
                    if (track == null)
                    {
                        if (!targetTrack.id.StartsWith("local:", StringComparison.Ordinal))
                            throw new InvalidOperationException($"Timeline Track '{targetTrack.id}'无法创建新的formal identity。");
                        Type type = TimelineAuthoringTypeCatalog.RequireTrackType(targetTrack.kind);
                        timeline.AddTrack(type, TimelineTreeContractComposition.Create());
                        track = timeline.Tracks.Last();
                    }
                    else if (track.ContractKind != targetTrack.kind)
                        throw new InvalidOperationException($"Timeline Track '{targetTrack.id}'不能原位改变kind。");
                    track.Name = targetTrack.name ?? track.Name;
                    TimelineAuthoringTrackBinding.Apply(
                        track,
                        targetTrack.animationChannelId,
                        targetTrack.animationSlotId);
                    SyncTimelineClips(timeline, catalog, track, targetTrack.clips);
                    resolvedTracks.Add(track);
                }
                timeline.Tracks.Clear();
                timeline.Tracks.AddRange(resolvedTracks);
                SyncTimelineSections(timeline, target.sections);
                SyncTimelineMotionWarpSources(timeline, target.tracks);
                timeline.Init();
                var errors = new List<string>();
                if (!asset.ValidateContent(catalog, errors))
                    throw new InvalidOperationException($"Skill Timeline '{target.id}'内容无效：{string.Join(" ", errors)}");
                m_Session.Touch(asset);
            }
        }

        void SyncTimelineBindings(
            TimelineData timeline,
            IReadOnlyList<AgentPackageSkillTimelineExternalBinding> targets)
        {
            var targetIds = (targets ?? new List<AgentPackageSkillTimelineExternalBinding>())
                .Select(value => value.id)
                .ToHashSet(StringComparer.Ordinal);
            foreach (TimelineExternalBindingDeclaration existing in timeline.ExternalBindings.ToArray())
                if (!targetIds.Contains(existing.AuthoringId))
                    timeline.RemoveExternalBinding(existing);
            foreach (AgentPackageSkillTimelineExternalBinding target in targets ?? new List<AgentPackageSkillTimelineExternalBinding>())
            {
                TimelineExternalBindingDeclaration binding = timeline.ExternalBindings.FirstOrDefault(value => value.AuthoringId == target.id);
                if (binding == null)
                {
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline ExternalBinding '{target.id}'无法保持稳定identity。");
                    binding = timeline.AddExternalBinding(
                        target.bindingId,
                        target.displayName,
                        target.domain,
                        Enum.Parse<TimelineBindingValueKind>(target.valueKind, false),
                        Enum.Parse<TimelineBindingAccess>(target.access, false),
                        Enum.Parse<TimelineBindingLifetime>(target.lifetime, false),
                        target.parameterId);
                }
                else
                    binding.Configure(
                        target.bindingId,
                        target.displayName,
                        target.domain,
                        Enum.Parse<TimelineBindingValueKind>(target.valueKind, false),
                        Enum.Parse<TimelineBindingAccess>(target.access, false),
                        Enum.Parse<TimelineBindingLifetime>(target.lifetime, false),
                        target.parameterId);
                if (!binding.Validate(out string error))
                    throw new InvalidOperationException($"Timeline ExternalBinding '{target.id}'无效：{error}");
            }
        }

        void SyncTimelineSections(TimelineData timeline, IReadOnlyList<AgentPackageSkillTimelineSection> targets)
        {
            var targetIds = (targets ?? new List<AgentPackageSkillTimelineSection>())
                .Select(value => value.id)
                .ToHashSet(StringComparer.Ordinal);
            foreach (TimelineSection existing in timeline.Sections.ToArray())
                if (!targetIds.Contains(existing.AuthoringId))
                    timeline.RemoveSection(existing);
            foreach (AgentPackageSkillTimelineSection target in targets ?? new List<AgentPackageSkillTimelineSection>())
            {
                TimelineSection section = timeline.Sections.FirstOrDefault(value => value.AuthoringId == target.id);
                if (section == null)
                {
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline Section '{target.id}'无法保持稳定identity。");
                    section = timeline.AddSection(target.name ?? string.Empty, target.frame);
                }
                else
                    timeline.ConfigureSection(section, target.name ?? string.Empty, target.frame);
            }
            foreach (AgentPackageSkillTimelineSection target in targets ?? new List<AgentPackageSkillTimelineSection>())
            {
                TimelineSection section = timeline.Sections.FirstOrDefault(value => value.AuthoringId == target.id);
                if (section == null)
                    throw new InvalidOperationException($"Timeline Section '{target.id}'无法解析。");
                timeline.ConfigureSectionNext(section, target.nextSectionId);
            }
        }

        void SyncTimelineClips(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Track track,
            IReadOnlyList<AgentPackageSkillTimelineClip> targets)
        {
            var targetIds = (targets ?? new List<AgentPackageSkillTimelineClip>())
                .Select(value => value.id)
                .ToHashSet(StringComparer.Ordinal);
            foreach (Clip existing in track.Clips.ToArray())
                if (!targetIds.Contains(existing.AuthoringId))
                    track.RemoveClip(existing);
            foreach (AgentPackageSkillTimelineClip target in targets ?? new List<AgentPackageSkillTimelineClip>())
            {
                Clip clip = track.Clips.FirstOrDefault(value => value.AuthoringId == target.id);
                if (clip == null)
                {
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline Clip '{target.id}'无法创建新的formal identity。");
                    clip = CreateClip(timeline, catalog, track, target);
                    m_LocalClips[TimelineKey(timeline.AuthoringId, target.id)] = clip;
                }
                else if (clip.ContractKind != target.kind)
                    throw new InvalidOperationException($"Timeline Clip '{target.id}'不能原位改变kind。");
                clip.StartFrame = target.startFrame;
                clip.EndFrame = target.endFrame;
                clip.OtherEaseInFrame = target.otherEaseInFrame;
                clip.OtherEaseOutFrame = target.otherEaseOutFrame;
                clip.SelfEaseInFrame = target.selfEaseInFrame;
                clip.SelfEaseOutFrame = target.selfEaseOutFrame;
                clip.ClipInFrame = target.clipInFrame;
                ConfigureClip(timeline, clip, target);
            }
        }

        void SyncTimelineMotionWarpSources(
            TimelineData timeline,
            IReadOnlyList<AgentPackageSkillTimelineTrack> tracks)
        {
            foreach (AgentPackageSkillTimelineTrack targetTrack in tracks ?? new List<AgentPackageSkillTimelineTrack>())
                foreach (AgentPackageSkillTimelineClip target in targetTrack?.clips ?? new List<AgentPackageSkillTimelineClip>())
                {
                    if (target?.kind != TimelineContractKinds.MotionWarpClip)
                        continue;
                    Clip clip = target.id.StartsWith("local:", StringComparison.Ordinal) &&
                                 m_LocalClips.TryGetValue(TimelineKey(timeline.AuthoringId, target.id), out Clip local)
                        ? local
                        : timeline.Tracks
                            .SelectMany(value => value.Clips)
                            .FirstOrDefault(value => value.AuthoringId == target.id);
                    string sourceId = target.properties?.Value<string>("sourceMotionClipId");
                    if (clip is not MotionWarpClip warp || string.IsNullOrEmpty(sourceId) ||
                        !TryResolveMotionClip(timeline, sourceId, out MotionCurveClip source))
                        throw new InvalidOperationException($"MotionWarp Clip '{target.id}'的source MotionCurve无法解析。");
                    MotionWarpAuthoring.BindSource(timeline, warp, source);
                }
        }

        void DeleteRemovedTimelines()
        {
            foreach (TimelineAsset asset in m_Current.Timelines.Values.Distinct().ToArray())
            {
                if (!asset)
                    continue;
                if (asset.Data != null && m_Document.timelines.Any(value => value.id == asset.Data.AuthoringId))
                    continue;
                if (AssetDatabase.IsSubAsset(asset))
                    Undo.DestroyObjectImmediate(asset);
            }
        }

        TimelineAsset ResolveExistingTimeline(string identity)
        {
            foreach (AgentPackageSkillTimelineFile target in m_Document.timelines ?? new List<AgentPackageSkillTimelineFile>())
            {
                if (target.id != identity)
                    continue;
                TimelineAsset asset = target.asset == null ? null :
                    m_Session.Resolver.TryResolveSkillObject(target.asset, out TimelineAsset resolved)
                        ? resolved
                        : null;
                if (asset)
                    return asset;
            }
            foreach (TimelineAsset asset in m_Current.Timelines.Values)
                if (asset && asset.Data != null && asset.Data.AuthoringId == identity)
                    return asset;
            return null;
        }

        Clip CreateClip(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Track track,
            AgentPackageSkillTimelineClip target)
        {
            ScriptableObject treeAsset = m_ResolveGraph(target.treeGraphId) as ScriptableObject;
            if (target.kind == TimelineContractKinds.TreeClip)
                return TimelineTreeAuthoringClipBinding.CreateClip(
                    timeline,
                    catalog,
                    track,
                    treeAsset,
                    target.startFrame);
            return TimelineAuthoringTrackBinding.CreateClip(
                timeline,
                catalog,
                track,
                m_Session.Resolver.TryResolveSkillObject(
                    target.animationClip,
                    out UnityEngine.AnimationClip animationAsset)
                    ? animationAsset
                    : null,
                target.startFrame);
        }

        void ConfigureClip(TimelineData timeline, Clip clip, AgentPackageSkillTimelineClip target)
        {
            JObject properties = target.properties?.DeepClone() as JObject ?? new JObject();
            if (!string.IsNullOrEmpty(target.treeGraphId))
                properties["treeGraphId"] = target.treeGraphId;
            if (!string.IsNullOrEmpty(target.treePhase))
                properties["treePhase"] = target.treePhase;
            TimelineAuthoringClipBinding.Apply(
                timeline,
                clip,
                properties,
                AgentAuthoringDocumentCodec.ToToken(target.curves ?? new List<AgentPackageCurve>()),
                this);
            if (target.kind == TimelineContractKinds.TreeClip)
                TimelineTreeAuthoringClipBinding.Apply(
                    clip,
                    m_ResolveGraph(target.treeGraphId) as ScriptableObject,
                    target.treePhase);
        }

        public bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip source)
        {
            source = null;
            string localKey = TimelineKey(timeline.AuthoringId, identity);
            if (m_LocalClips.TryGetValue(localKey, out Clip local))
            {
                source = local as MotionCurveClip;
                return source != null;
            }
            if (!MotionWarpAuthoring.TryResolveClip(timeline, identity, out Clip resolved))
                return false;
            source = resolved as MotionCurveClip;
            return source != null;
        }

        static string TimelineKey(string timelineId, string itemId) => timelineId + "\0" + itemId;
    }
}
