using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineContentBindingUse
    {
        public TimelineContentBindingUse(
            string bindingId,
            TimelineBindingValueKind valueKind,
            TimelineBindingAccess access,
            TimelineBindingLifetime lifetime,
            string targetBindingId = null)
        {
            BindingId = string.IsNullOrWhiteSpace(bindingId)
                ? throw new ArgumentException("Timeline binding identity is required.", nameof(bindingId))
                : bindingId.Trim();
            ValueKind = valueKind;
            Access = access;
            Lifetime = lifetime;
            TargetBindingId = targetBindingId?.Trim() ?? string.Empty;
        }

        public string BindingId { get; }
        public TimelineBindingValueKind ValueKind { get; }
        public TimelineBindingAccess Access { get; }
        public TimelineBindingLifetime Lifetime { get; }
        public string TargetBindingId { get; }
    }

    public readonly struct TimelineContentDependency
    {
        public TimelineContentDependency(string identity, string kind, string sourcePath, string contentHash)
        {
            Identity = Require(identity, nameof(identity));
            Kind = Require(kind, nameof(kind));
            SourcePath = sourcePath ?? string.Empty;
            ContentHash = Require(contentHash, nameof(contentHash));
        }

        public string Identity { get; }
        public string Kind { get; }
        public string SourcePath { get; }
        public string ContentHash { get; }

        static string Require(string value, string name)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Timeline content dependency identity is required.", name)
                : value.Trim();
        }
    }

    public interface ITimelineContentClosureSource
    {
        void CollectContentClosure(TimelineContentClosureBuilder builder);
    }

    public readonly struct TimelineContentCurveKey
    {
        public TimelineContentCurveKey(
            float time,
            float value,
            float inTangent,
            float outTangent,
            float inWeight,
            float outWeight,
            int weightedMode)
        {
            Time = time;
            Value = value;
            InTangent = inTangent;
            OutTangent = outTangent;
            InWeight = inWeight;
            OutWeight = outWeight;
            WeightedMode = weightedMode;
        }

        public float Time { get; }
        public float Value { get; }
        public float InTangent { get; }
        public float OutTangent { get; }
        public float InWeight { get; }
        public float OutWeight { get; }
        public int WeightedMode { get; }
    }

    public readonly struct TimelineContentCurve
    {
        public TimelineContentCurve(
            string channelId,
            string displayName,
            TimelineCurveTimeDomain timeDomain,
            TimelineCurveValueDomain valueDomain,
            int preWrapMode,
            int postWrapMode,
            IReadOnlyList<TimelineContentCurveKey> keys)
        {
            ChannelId = string.IsNullOrWhiteSpace(channelId)
                ? throw new ArgumentException("Timeline curve channel identity is required.", nameof(channelId))
                : channelId.Trim();
            DisplayName = displayName ?? string.Empty;
            TimeDomain = timeDomain;
            ValueDomain = valueDomain;
            PreWrapMode = preWrapMode;
            PostWrapMode = postWrapMode;
            Keys = new ReadOnlyCollection<TimelineContentCurveKey>(
                new List<TimelineContentCurveKey>(keys ?? Array.Empty<TimelineContentCurveKey>()));
        }

        public string ChannelId { get; }
        public string DisplayName { get; }
        public TimelineCurveTimeDomain TimeDomain { get; }
        public TimelineCurveValueDomain ValueDomain { get; }
        public int PreWrapMode { get; }
        public int PostWrapMode { get; }
        public IReadOnlyList<TimelineContentCurveKey> Keys { get; }
    }

    public readonly struct TimelineContentPresentationMarker
    {
        public TimelineContentPresentationMarker(
            string authoringId,
            int frame,
            int endFrame,
            TimelinePresentationMarkerLifetime lifetime,
            TimelineContentBindingUse payloadBinding)
        {
            AuthoringId = string.IsNullOrWhiteSpace(authoringId)
                ? throw new ArgumentException("Timeline presentation marker identity is required.", nameof(authoringId))
                : authoringId.Trim();
            if (frame < 0 || endFrame < frame ||
                !Enum.IsDefined(typeof(TimelinePresentationMarkerLifetime), lifetime))
            {
                throw new ArgumentOutOfRangeException(nameof(frame));
            }
            if (lifetime == TimelinePresentationMarkerLifetime.Pulse && endFrame != frame)
                throw new ArgumentException("Timeline Pulse marker cannot have a duration.", nameof(endFrame));
            if (lifetime == TimelinePresentationMarkerLifetime.Stateful && endFrame <= frame)
                throw new ArgumentException("Timeline Stateful marker requires a positive duration.", nameof(endFrame));
            Frame = frame;
            EndFrame = endFrame;
            Lifetime = lifetime;
            PayloadBinding = payloadBinding;
        }

        public string AuthoringId { get; }
        public int Frame { get; }
        public int EndFrame { get; }
        public TimelinePresentationMarkerLifetime Lifetime { get; }
        public TimelineContentBindingUse PayloadBinding { get; }
    }

    public sealed class TimelineContentClosureBuilder
    {
        readonly List<TimelineContentDependency> m_Dependencies = new List<TimelineContentDependency>();
        readonly List<string> m_Errors = new List<string>();

        public IReadOnlyList<TimelineContentDependency> Dependencies => m_Dependencies;
        public IReadOnlyList<string> Errors => m_Errors;

        public void AddDependency(string identity, string kind, string sourcePath, string contentHash)
        {
            TimelineContentDependency dependency = new TimelineContentDependency(identity, kind, sourcePath, contentHash);
            for (int i = 0; i < m_Dependencies.Count; i++)
            {
                if (string.Equals(m_Dependencies[i].Identity, dependency.Identity, StringComparison.Ordinal))
                {
                    if (!string.Equals(m_Dependencies[i].ContentHash, dependency.ContentHash, StringComparison.Ordinal))
                        AddError("timeline_dependency_version_conflict", dependency.Identity, "The same dependency identity resolved to different content hashes.");
                    return;
                }
            }
            m_Dependencies.Add(dependency);
        }

        public void AddError(string code, string sourcePath, string message)
        {
            m_Errors.Add($"{code}:{sourcePath}:{message}");
        }
    }

    public readonly struct TimelineContentClip
    {
        public TimelineContentClip(
            string authoringId,
            string contractKind,
            string trackAuthoringId,
            TimelineClipExecutionPolicy executionPolicy,
            int startFrame,
            int endFrame,
            TimelineClipExecutionPhase executionPhase,
            TimelineClipExitSource exitSource,
            TimelineCapability capabilities,
            bool supportsStop,
            bool trackMuted,
            IReadOnlyList<TimelineContentBindingUse> bindings,
            IReadOnlyList<TimelineContentPresentationMarker> presentationMarkers = null,
            IReadOnlyList<TimelineContentCurve> curves = null)
        {
            AuthoringId = Require(authoringId, nameof(authoringId));
            ContractKind = Require(contractKind, nameof(contractKind));
            TrackAuthoringId = Require(trackAuthoringId, nameof(trackAuthoringId));
            ExecutionPolicy = executionPolicy;
            StartFrame = startFrame;
            EndFrame = endFrame;
            ExecutionPhase = executionPhase;
            ExitSource = exitSource;
            Capabilities = capabilities;
            SupportsStop = supportsStop;
            TrackMuted = trackMuted;
            Bindings = new ReadOnlyCollection<TimelineContentBindingUse>(new List<TimelineContentBindingUse>(bindings ?? Array.Empty<TimelineContentBindingUse>()));
            PresentationMarkers = new ReadOnlyCollection<TimelineContentPresentationMarker>(
                new List<TimelineContentPresentationMarker>(presentationMarkers ?? Array.Empty<TimelineContentPresentationMarker>()));
            Curves = new ReadOnlyCollection<TimelineContentCurve>(new List<TimelineContentCurve>(curves ?? Array.Empty<TimelineContentCurve>()));
        }

        public string AuthoringId { get; }
        public string ContractKind { get; }
        public string TrackAuthoringId { get; }
        public TimelineClipExecutionPolicy ExecutionPolicy { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
        public TimelineClipExecutionPhase ExecutionPhase { get; }
        public TimelineClipExitSource ExitSource { get; }
        public TimelineCapability Capabilities { get; }
        public bool SupportsStop { get; }
        public bool TrackMuted { get; }
        public IReadOnlyList<TimelineContentBindingUse> Bindings { get; }
        public IReadOnlyList<TimelineContentPresentationMarker> PresentationMarkers { get; }
        public IReadOnlyList<TimelineContentCurve> Curves { get; }

        static string Require(string value, string name)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Timeline content identity is required.", name)
                : value.Trim();
        }
    }

    public readonly struct TimelineContentSection
    {
        public TimelineContentSection(string authoringId, int frame, string nextSectionId)
        {
            AuthoringId = Require(authoringId, nameof(authoringId));
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));
            Frame = frame;
            NextSectionId = nextSectionId?.Trim() ?? string.Empty;
        }

        public string AuthoringId { get; }
        public int Frame { get; }
        public string NextSectionId { get; }

        static string Require(string value, string name)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Timeline section identity is required.", name)
                : value.Trim();
        }
    }

    public readonly struct TimelineContentTrack
    {
        public TimelineContentTrack(
            string authoringId,
            string contractKind,
            TimelineClipExecutionPolicy executionPolicy,
            int order,
            bool muted,
            IReadOnlyList<string> clipAuthoringIds)
        {
            AuthoringId = Require(authoringId, nameof(authoringId));
            ContractKind = Require(contractKind, nameof(contractKind));
            ExecutionPolicy = executionPolicy;
            Order = order;
            Muted = muted;
            ClipAuthoringIds = new ReadOnlyCollection<string>(
                new List<string>(clipAuthoringIds ?? Array.Empty<string>()));
        }

        public string AuthoringId { get; }
        public string ContractKind { get; }
        public TimelineClipExecutionPolicy ExecutionPolicy { get; }
        public int Order { get; }
        public bool Muted { get; }
        public IReadOnlyList<string> ClipAuthoringIds { get; }

        static string Require(string value, string name)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Timeline track identity is required.", name)
                : value.Trim();
        }
    }

    public sealed class TimelineContentUnit
    {
        internal TimelineContentUnit(
            string identity,
            string contentHash,
            string rootFingerprint,
            string name,
            int frameRate,
            int maxFrame,
            bool loop,
            IReadOnlyList<TimelineContentTrack> tracks,
            IReadOnlyList<TimelineContentClip> clips,
            IReadOnlyList<TimelineContentSection> sections,
            IReadOnlyList<TimelineBindingDeclaration> bindings,
            IReadOnlyList<TimelineContentDependency> dependencies)
        {
            Identity = identity ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            RootFingerprint = rootFingerprint ?? string.Empty;
            Name = name ?? string.Empty;
            FrameRate = frameRate;
            MaxFrame = maxFrame;
            Loop = loop;
            Tracks = new ReadOnlyCollection<TimelineContentTrack>(new List<TimelineContentTrack>(tracks ?? Array.Empty<TimelineContentTrack>()));
            Clips = new ReadOnlyCollection<TimelineContentClip>(new List<TimelineContentClip>(clips ?? Array.Empty<TimelineContentClip>()));
            Sections = new ReadOnlyCollection<TimelineContentSection>(new List<TimelineContentSection>(sections ?? Array.Empty<TimelineContentSection>()));
            Bindings = new ReadOnlyCollection<TimelineBindingDeclaration>(new List<TimelineBindingDeclaration>(bindings ?? Array.Empty<TimelineBindingDeclaration>()));
            Dependencies = new ReadOnlyCollection<TimelineContentDependency>(new List<TimelineContentDependency>(dependencies ?? Array.Empty<TimelineContentDependency>()));
        }

        public string Identity { get; }
        public string ContentHash { get; }
        public string RootFingerprint { get; }
        public string Name { get; }
        public int FrameRate { get; }
        public int MaxFrame { get; }
        public bool Loop { get; }
        public IReadOnlyList<TimelineContentTrack> Tracks { get; }
        public IReadOnlyList<TimelineContentClip> Clips { get; }
        public IReadOnlyList<TimelineContentSection> Sections { get; }
        public IReadOnlyList<TimelineBindingDeclaration> Bindings { get; }
        public IReadOnlyList<TimelineContentDependency> Dependencies { get; }
        public bool IsValid => !string.IsNullOrEmpty(Identity) &&
            !string.IsNullOrEmpty(ContentHash) &&
            !string.IsNullOrEmpty(RootFingerprint) &&
            FrameRate > 0 &&
            MaxFrame >= 0;
    }

    public sealed class TimelineContentDiscoveryResult
    {
        internal TimelineContentDiscoveryResult(TimelineContentUnit content, IReadOnlyList<string> errors)
        {
            Content = content;
            Errors = new ReadOnlyCollection<string>(new List<string>(errors ?? Array.Empty<string>()));
        }

        public TimelineContentUnit Content { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Content != null && Content.IsValid && Errors.Count == 0;
    }

    public static class TimelineContentDiscovery
    {
        public static TimelineContentDiscoveryResult Discover(TimelineAsset asset, TimelineContractCatalog catalog)
        {
            return Discover(asset?.Data, catalog);
        }

        public static TimelineContentDiscoveryResult Discover(TimelineData timeline, TimelineContractCatalog catalog)
        {
            var errors = new List<string>();
            if (timeline == null)
            {
                errors.Add("Timeline content root is missing.");
                return new TimelineContentDiscoveryResult(null, errors);
            }

            if (!timeline.ValidateContent(catalog, errors))
                return new TimelineContentDiscoveryResult(null, errors);

            var clips = new List<TimelineContentClip>();
            var tracks = new List<TimelineContentTrack>();
            var sections = new List<TimelineContentSection>();
            var closure = new TimelineContentClosureBuilder();
            int maxFrame = 0;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                var clipAuthoringIds = new List<string>();
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip != null)
                        clipAuthoringIds.Add(clip.AuthoringId);
                    if (clip != null && clip.EndFrame > maxFrame)
                        maxFrame = clip.EndFrame;
                }
                tracks.Add(new TimelineContentTrack(
                    track.AuthoringId,
                    track.ContractKind,
                    TimelineClipExecutionPolicy.FromDomain(track.ExecutionDomain),
                    trackIndex,
                    track.PersistentMuted,
                    clipAuthoringIds));
            }
            for (int sectionIndex = 0; sectionIndex < timeline.Sections.Count; sectionIndex++)
            {
                TimelineSection section = timeline.Sections[sectionIndex];
                if (section != null && section.Frame > maxFrame)
                    maxFrame = section.Frame;
                if (section != null)
                    sections.Add(new TimelineContentSection(
                        section.AuthoringId,
                        section.Frame,
                        section.NextSectionId));
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                TimelineTrackContract trackContract = catalog.RequireTrack(track.ContractKind);
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;
                    TimelineClipContract clipContract = catalog.RequireClip(clip.ContractKind);
                    if (!trackContract.AllowsClip(clipContract.Kind))
                        continue;
                    TimelineClipExecutionPhase executionPhase = clipContract.DefaultExecutionPhase;
                    if (clip is ITimelineClipExecutionPhaseSource phaseSource)
                        executionPhase = phaseSource.TimelineExecutionPhase;
                    TimelineClipExitSource exitSource = TimelineClipExitSource.FrameBoundary;
                    if (clip is ITimelineClipExitSource exitSourceSource)
                        exitSource = exitSourceSource.ClipExitSource;
                    if (!clipContract.SupportsExecutionPhase(executionPhase))
                    {
                        errors.Add($"timeline_clip_execution_phase:{clip.AuthoringId}:phase '{executionPhase}' is not supported by '{clipContract.Kind}'.");
                        continue;
                    }
                    var bindingUses = new List<TimelineContentBindingUse>();
                    var presentationMarkers = new List<TimelineContentPresentationMarker>();
                    var curves = new List<TimelineContentCurve>();
                    var curveDescriptors = new List<TimelineCurveChannelDescriptor>();
                    TimelineCurveChannelCatalog.CollectForTrack(track, curveDescriptors);
                    for (int curveIndex = 0; curveIndex < curveDescriptors.Count; curveIndex++)
                    {
                        TimelineCurveChannelDescriptor descriptor = curveDescriptors[curveIndex];
                        if (!descriptor.Supports(clip))
                            continue;
                        AnimationCurve sourceCurve = descriptor.Read(clip);
                        Keyframe[] keys = sourceCurve?.keys ?? Array.Empty<Keyframe>();
                        var curveKeys = new TimelineContentCurveKey[keys.Length];
                        for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                        {
                            Keyframe key = keys[keyIndex];
                            curveKeys[keyIndex] = new TimelineContentCurveKey(
                                key.time,
                                key.value,
                                key.inTangent,
                                key.outTangent,
                                key.inWeight,
                                key.outWeight,
                                (int)key.weightedMode);
                        }
                        curves.Add(new TimelineContentCurve(
                            descriptor.ChannelId.Value,
                            descriptor.DisplayName,
                            descriptor.TimeDomain,
                            descriptor.ValueDomain,
                            (int)(sourceCurve?.preWrapMode ?? WrapMode.ClampForever),
                            (int)(sourceCurve?.postWrapMode ?? WrapMode.ClampForever),
                            curveKeys));
                    }
                    if (clip is ITimelineExternalBindingUseSource bindingSource && bindingSource.ExternalBindingUses != null)
                    {
                        for (int useIndex = 0; useIndex < bindingSource.ExternalBindingUses.Count; useIndex++)
                        {
                            TimelineExternalBindingUse use = bindingSource.ExternalBindingUses[useIndex];
                            if (use != null)
                                bindingUses.Add(new TimelineContentBindingUse(use.BindingId, use.ValueKind, use.Access, use.Lifetime, use.TargetBindingId));
                        }
                    }
                    if (clip is ITimelinePresentationMarkerSource markerSource &&
                        markerSource.PresentationMarkers != null)
                    {
                        for (int markerIndex = 0; markerIndex < markerSource.PresentationMarkers.Count; markerIndex++)
                        {
                            TimelinePresentationMarker marker = markerSource.PresentationMarkers[markerIndex];
                            if (marker == null || marker.PayloadBinding == null)
                                continue;
                            TimelineExternalBindingUse payload = marker.PayloadBinding;
                            var payloadBinding = new TimelineContentBindingUse(
                                payload.BindingId,
                                payload.ValueKind,
                                payload.Access,
                                payload.Lifetime,
                                payload.TargetBindingId);
                            bindingUses.Add(payloadBinding);
                            presentationMarkers.Add(new TimelineContentPresentationMarker(
                                marker.AuthoringId,
                                marker.Frame,
                                marker.EndFrame,
                                marker.Lifetime,
                                payloadBinding));
                        }
                    }
                    clips.Add(new TimelineContentClip(
                        clip.AuthoringId,
                        clipContract.Kind,
                        track.AuthoringId,
                        TimelineClipExecutionPolicy.FromDomain(
                            clip.ResolveExecutionDomain(track.ExecutionDomain)),
                        clip.StartFrame,
                        clip.EndFrame,
                        executionPhase,
                        exitSource,
                        clipContract.Capabilities,
                        clipContract.SupportsStop,
                        track.PersistentMuted,
                        bindingUses,
                        presentationMarkers,
                        curves));
                    if (clip is ITimelineContentClosureSource closureSource)
                        closureSource.CollectContentClosure(closure);
                }
            }

            var bindings = new List<TimelineBindingDeclaration>();
            for (int i = 0; i < timeline.ExternalBindings.Count; i++)
            {
                TimelineExternalBindingDeclaration binding = timeline.ExternalBindings[i];
                if (binding == null)
                    continue;
                bindings.Add(new TimelineBindingDeclaration(
                    binding.BindingId,
                    binding.Domain,
                    binding.ParameterId,
                    binding.ValueKind,
                    binding.Access,
                    binding.Lifetime));
            }
            clips.Sort((left, right) => string.CompareOrdinal(left.AuthoringId, right.AuthoringId));
            sections.Sort((left, right) =>
            {
                int frame = left.Frame.CompareTo(right.Frame);
                return frame != 0 ? frame : string.CompareOrdinal(left.AuthoringId, right.AuthoringId);
            });
            bindings.Sort((left, right) => string.CompareOrdinal(left.BindingId, right.BindingId));
            errors.AddRange(closure.Errors);
            List<TimelineContentDependency> dependencies = new List<TimelineContentDependency>(closure.Dependencies);
            dependencies.Sort((left, right) => string.CompareOrdinal(left.Identity, right.Identity));
            if (errors.Count > 0)
                return new TimelineContentDiscoveryResult(null, errors);
            string rootFingerprint = TimelineAuthoringFingerprint.Compute(timeline);
            var hashParts = new List<string>
            {
                "timeline-content-closure/1",
                timeline.AuthoringId,
                rootFingerprint
            };
            for (int i = 0; i < dependencies.Count; i++)
            {
                hashParts.Add(dependencies[i].Identity);
                hashParts.Add(dependencies[i].Kind);
                hashParts.Add(dependencies[i].ContentHash);
            }
            for (int i = 0; i < tracks.Count; i++)
            {
                hashParts.Add(tracks[i].ContractKind);
                hashParts.Add(tracks[i].ExecutionPolicy.Domain.ToString("G"));
                hashParts.Add(tracks[i].ExecutionPolicy.OutputKind.ToString("G"));
            }
            for (int i = 0; i < clips.Count; i++)
            {
                hashParts.Add(clips[i].AuthoringId);
                hashParts.Add(clips[i].ExecutionPolicy.Domain.ToString("G"));
                hashParts.Add(clips[i].ExecutionPolicy.OutputKind.ToString("G"));
                for (int markerIndex = 0; markerIndex < clips[i].PresentationMarkers.Count; markerIndex++)
                {
                    TimelineContentPresentationMarker marker = clips[i].PresentationMarkers[markerIndex];
                    hashParts.Add(marker.AuthoringId);
                    hashParts.Add(marker.Frame.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    hashParts.Add(marker.EndFrame.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    hashParts.Add(marker.Lifetime.ToString("G"));
                    hashParts.Add(marker.PayloadBinding.BindingId);
                    hashParts.Add(marker.PayloadBinding.ValueKind.ToString("G"));
                    hashParts.Add(marker.PayloadBinding.Access.ToString("G"));
                    hashParts.Add(marker.PayloadBinding.Lifetime.ToString("G"));
                    hashParts.Add(marker.PayloadBinding.TargetBindingId);
                }
            }
            return new TimelineContentDiscoveryResult(
                new TimelineContentUnit(
                    $"timeline:{timeline.AuthoringId}",
                    SourceContentHasher.Hash(hashParts.ToArray()),
                    rootFingerprint,
                    timeline.Name,
                    TimelineUtility.FrameRate,
                    maxFrame,
                    timeline.Loop,
                    tracks,
                    clips,
                    sections,
                    bindings,
                    dependencies),
                errors);
        }
    }
}
