using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    sealed class TimelineSemanticEmissionSession
    {
        readonly TimelineData m_Timeline;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly string m_InvocationIdentity;
        readonly Dictionary<string, EmittedClip> m_Clips = new Dictionary<string, EmittedClip>(StringComparer.Ordinal);
        readonly List<OperationHandle> m_ClipOperations = new List<OperationHandle>();
        readonly List<PendingMotionSource> m_PendingMotionSources = new List<PendingMotionSource>();
        bool m_MotionWarpValidated;

        public TimelineSemanticEmissionSession(
            TimelineData timeline,
            CharacterSimulationProgramBuilder builder,
            TimelineSemanticInvocation invocation)
        {
            m_Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_InvocationIdentity = invocation.IsValid
                ? invocation.Identity
                : throw new ArgumentException("Timeline invocation is invalid.", nameof(invocation));
        }

        public void RecordClip(
            Clip clip,
            OperationHandle operation,
            string identity,
            CharacterSimulationSourceLocation source)
        {
            if (!m_Clips.TryAdd(clip.AuthoringId, new EmittedClip(clip, operation, identity, source)))
                m_Builder.Report.Error("timeline_clip_identity_duplicate", source.Identity, $"Timeline clip identity '{clip.AuthoringId}' is duplicated.");
            else
                m_ClipOperations.Add(operation);
        }

        public IReadOnlyList<OperationHandle> ClipOperations => m_ClipOperations;

        public void DeferMotionSource(
            MotionWarpClip warp,
            OperationHandle operation,
            CharacterSimulationSourceLocation source)
        {
            m_PendingMotionSources.Add(new PendingMotionSource(warp, operation, source));
        }

        public void ValidateMotionWarp()
        {
            if (m_MotionWarpValidated)
                return;
            m_MotionWarpValidated = true;
            var issues = new List<MotionWarpAuthoringIssue>();
            MotionWarpAuthoring.Validate(m_Timeline, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                MotionWarpAuthoringIssue issue = issues[i];
                string identity = issue.Clip == null
                    ? $"timeline:{m_Timeline.AuthoringId}"
                    : $"timeline:{m_Timeline.AuthoringId}/clip:{issue.Clip.AuthoringId}";
                m_Builder.Report.Error(issue.Code, identity, issue.Message);
            }
        }

        public void Complete()
        {
            for (int i = 0; i < m_PendingMotionSources.Count; i++)
            {
                PendingMotionSource pending = m_PendingMotionSources[i];
                if (!m_Clips.TryGetValue(pending.Warp.SourceMotionClipId, out EmittedClip source))
                {
                    m_Builder.Report.Error(
                        "motion_warp_source_operation_missing",
                        pending.Source.Identity,
                        $"MotionWarp source '{pending.Warp.SourceMotionClipId}' was not emitted by the same Timeline.");
                    continue;
                }
                if (source.Clip is not MotionCurveClip || source.Operation.Value < 0)
                {
                    m_Builder.Report.Error(
                        "motion_warp_source_operation_invalid",
                        pending.Source.Identity,
                        $"MotionWarp source '{source.Identity}' is not a MotionCurve operation.");
                    continue;
                }
                m_Builder.DeclareReference(
                    $"{m_InvocationIdentity}/timeline:{m_Timeline.AuthoringId}/clip:{pending.Warp.AuthoringId}/motion-source",
                    pending.Operation,
                    ProgramReferenceKind.MotionSourceOperation,
                    source.Operation.Value,
                    source.Identity,
                    pending.Source);
            }
        }

        readonly struct EmittedClip
        {
            public EmittedClip(
                Clip clip,
                OperationHandle operation,
                string identity,
                CharacterSimulationSourceLocation source)
            {
                Clip = clip;
                Operation = operation;
                Identity = identity;
                Source = source;
            }

            public Clip Clip { get; }
            public OperationHandle Operation { get; }
            public string Identity { get; }
            public CharacterSimulationSourceLocation Source { get; }
        }

        readonly struct PendingMotionSource
        {
            public PendingMotionSource(
                MotionWarpClip warp,
                OperationHandle operation,
                CharacterSimulationSourceLocation source)
            {
                Warp = warp;
                Operation = operation;
                Source = source;
            }

            public MotionWarpClip Warp { get; }
            public OperationHandle Operation { get; }
            public CharacterSimulationSourceLocation Source { get; }
        }
    }

    public interface ITimelineSemanticTrackEmitter
    {
        Type SourceType { get; }
        void Emit(Track track, TimelineSemanticEmitterContext context);
    }

    public interface ITimelineSemanticClipEmitter
    {
        Type SourceType { get; }
        OperationHandle Emit(Clip clip, TimelineSemanticEmitterContext context);
    }

    public sealed class TimelineSemanticEmitterContext
    {
        readonly TimelineData m_Timeline;
        readonly string m_TimelineContentHash;
        readonly Track m_Track;
        readonly int m_TrackIndex;
        readonly string m_OwnerGraphId;
        readonly string m_OwnerNodeId;
        readonly string m_Route;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly TimelineSemanticEmissionSession m_Session;

        internal TimelineSemanticEmitterContext(
            TimelineData timeline,
            Track track,
            int trackIndex,
            TimelineSemanticInvocation invocation,
            CharacterSimulationProgramBuilder builder,
            OperationHandle timelineOperation,
            string actionContextIdentity,
            TimelineSemanticEmissionSession session,
            string timelineContentHash)
        {
            m_Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            m_TimelineContentHash = string.IsNullOrWhiteSpace(timelineContentHash)
                ? throw new ArgumentException("Timeline content hash is required.", nameof(timelineContentHash))
                : timelineContentHash;
            m_Track = track ?? throw new ArgumentNullException(nameof(track));
            m_TrackIndex = trackIndex;
            Invocation = invocation;
            m_OwnerGraphId = invocation.GraphId;
            m_OwnerNodeId = invocation.NodeId;
            m_Route = invocation.Route;
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            TimelineOperation = timelineOperation.IsValid
                ? timelineOperation
                : throw new ArgumentException("Timeline operation is required.", nameof(timelineOperation));
            ActionContextIdentity = actionContextIdentity ?? string.Empty;
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public TimelineData Timeline => m_Timeline;
        public Track Track => m_Track;
        public CharacterSimulationProgramBuilder Builder => m_Builder;
        public TimelineSemanticInvocation Invocation { get; }
        public OperationHandle TimelineOperation { get; }
        public string ActionContextIdentity { get; }
        public CharacterSimulationSourceLocation TrackSource => new CharacterSimulationSourceLocation(
            m_Track.GetType().FullName,
            m_OwnerGraphId,
            m_OwnerNodeId,
            string.Empty,
            m_Timeline.AuthoringId,
            string.Empty,
            $"{m_Route}/track:{m_Track.AuthoringId}",
            trackId: m_Track.AuthoringId,
            contentHash: m_TimelineContentHash);

        public CharacterSimulationSourceLocation ClipSource(Clip clip)
        {
            return new CharacterSimulationSourceLocation(
                clip.GetType().FullName,
                m_OwnerGraphId,
                m_OwnerNodeId,
                string.Empty,
                m_Timeline.AuthoringId,
                clip.AuthoringId,
                $"{m_Route}/track:{m_Track.AuthoringId}/clip:{clip.AuthoringId}",
                trackId: m_Track.AuthoringId,
                contentHash: m_TimelineContentHash);
        }

        public void DeclareTrackCatalog(params ProgramCatalogField[] fields)
        {
            CharacterSimulationSourceLocation source = TrackSource;
            var values = CommonTrackFields(source).Concat(Valid(fields));
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.TimelineTrack,
                TrackIdentity,
                1,
                values,
                source);
        }

        public OperationHandle DeclareClipOperation(
            Clip clip,
            SimulationOperationCode code,
            IEnumerable<ProgramCatalogField> fields,
            string text = null,
            int integer0 = 0,
            int integer1 = 0,
            uint flags = 0)
        {
            CharacterSimulationSourceLocation source = ClipSource(clip);
            ProgramCatalogField[] values = CommonClipFields(clip, source).Concat(Valid(fields)).ToArray();
            int catalog = m_Builder.DeclareCatalogEntry(
                clip is MotionCurveClip ? ProgramCatalogEntryKind.MotionCurve : ProgramCatalogEntryKind.TimelineClip,
                CatalogIdentity(clip),
                1,
                values,
                source);
            var constants = values
                .Where(value => value.Kind == ProgramCatalogFieldKind.Constant)
                .Select(value => value.ConstantIndex)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            OperationHandle operation = m_Builder.DeclareOperation(
                source,
                code,
                constants,
                integer0,
                integer1,
                catalog >= 0 ? (ulong)catalog : 0UL,
                default,
                text,
                flags);
            if (catalog >= 0)
            {
                m_Builder.DeclareReference(
                    ReferenceIdentity($"{ClipIdentity(clip)}/catalog"),
                    operation,
                    ProgramReferenceKind.CatalogEntry,
                    catalog,
                    CatalogIdentity(clip),
                    source);
            }
            m_Session.RecordClip(clip, operation, ClipIdentity(clip), source);
            return operation;
        }

        public void DeferMotionSourceReference(MotionWarpClip warp, OperationHandle operation)
        {
            m_Session.DeferMotionSource(warp, operation, ClipSource(warp));
        }

        public void ValidateMotionWarp()
        {
            m_Session.ValidateMotionWarp();
        }

        public SemanticDataDocument BakeCurve(Clip clip, string fieldName, AnimationCurve curve)
        {
            CharacterSimulationSourceLocation source = ClipSource(clip);
            try
            {
                if (curve == null)
                    throw new InvalidOperationException($"Curve '{fieldName}' is missing.");
                var writer = new SemanticDataWriter();
                writer.WriteUInt32(0x56525543);
                writer.WriteInt32(1);
                writer.WriteInt32((int)curve.preWrapMode);
                writer.WriteInt32((int)curve.postWrapMode);
                writer.WriteInt32(curve.length);
                for (int i = 0; i < curve.length; i++)
                {
                    Keyframe key = curve.keys[i];
                    if (key.weightedMode != WeightedMode.None)
                        throw new InvalidOperationException($"Curve '{fieldName}' key #{i} uses unsupported weighted tangents.");
                    writer.WriteNumber(key.time, $"{source.Identity}/{fieldName}[{i}].time");
                    writer.WriteNumber(key.value, $"{source.Identity}/{fieldName}[{i}].value");
                    writer.WriteNumber(key.inTangent, $"{source.Identity}/{fieldName}[{i}].inTangent");
                    writer.WriteNumber(key.outTangent, $"{source.Identity}/{fieldName}[{i}].outTangent");
                    writer.WriteNumber(key.inWeight, $"{source.Identity}/{fieldName}[{i}].inWeight");
                    writer.WriteNumber(key.outWeight, $"{source.Identity}/{fieldName}[{i}].outWeight");
                    writer.WriteInt32((int)key.weightedMode);
                }
                return writer.Build();
            }
            catch (Exception exception)
            {
                m_Builder.Report.Error("timeline_curve_invalid", source.Identity, exception.Message);
                return SemanticDataDocument.Empty;
            }
        }

        public string TrackIdentity => $"timeline:{m_Timeline.AuthoringId}/track:{m_Track.AuthoringId}";
        public string ClipIdentity(Clip clip) => $"{TrackIdentity}/clip:{clip.AuthoringId}";
        public string InvocationIdentity => Invocation.Identity;
        public string ReferenceIdentity(string identity) => Invocation.ReferenceIdentity(identity);
        public string CatalogIdentity(Clip clip)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            return $"{ClipIdentity(clip)}/invocation:{Invocation.Identity}";
        }

        public CharacterSimulationSourceLocation InvocationConstantSource(Clip clip, string fieldName)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Timeline invocation constant field is required.", nameof(fieldName));
            return new CharacterSimulationSourceLocation(
                clip.GetType().FullName,
                string.Empty,
                string.Empty,
                string.Empty,
                m_Timeline.AuthoringId,
                string.Empty,
                displayPath: $"{Invocation.Route}/timeline:{m_Timeline.AuthoringId}/track:{m_Track.AuthoringId}/clip:{clip.AuthoringId}/invocation:{Invocation.Identity}/constant:{fieldName.Trim()}",
                trackId: m_Track.AuthoringId,
                contentHash: m_TimelineContentHash);
        }

        string ProducerPrefix => Invocation.Kind == TimelineSemanticInvocationKind.CharacterGraph
            ? string.Empty
            : Invocation.Identity + "/";

        public string ProducerIdentity(Clip clip) => $"{ProducerPrefix}producer:{m_Timeline.AuthoringId}:{m_Track.AuthoringId}:{clip.AuthoringId}";
        public string AnimationProducerIdentity => $"{ProducerPrefix}producer:{m_Timeline.AuthoringId}:{m_Track.AuthoringId}";

        IEnumerable<ProgramCatalogField> CommonTrackFields(CharacterSimulationSourceLocation source)
        {
            yield return m_Builder.ConstantField(source, "TrackIndex", m_TrackIndex);
            yield return m_Builder.ConstantField(source, "Name", m_Track.Name ?? string.Empty);
            yield return m_Builder.ConstantField(source, "Muted", m_Track.PersistentMuted);
            yield return m_Builder.IdentityField("Timeline", $"timeline:{m_Timeline.AuthoringId}");
        }

        IEnumerable<ProgramCatalogField> CommonClipFields(Clip clip, CharacterSimulationSourceLocation source)
        {
            yield return m_Builder.ConstantField(source, "StartFrame", clip.StartFrame);
            yield return m_Builder.ConstantField(source, "EndFrame", clip.EndFrame);
            yield return m_Builder.ConstantField(source, "EaseInFrame", clip.EaseInFrame);
            yield return m_Builder.ConstantField(source, "EaseOutFrame", clip.EaseOutFrame);
            yield return m_Builder.ConstantField(source, "ClipInFrame", clip.ClipInFrame);
            yield return m_Builder.IdentityField("Track", TrackIdentity);
        }

        static IEnumerable<ProgramCatalogField> Valid(IEnumerable<ProgramCatalogField> fields)
        {
            return fields == null ? Enumerable.Empty<ProgramCatalogField>() : fields.Where(value => value != null);
        }

    }

    public sealed class TimelineSemanticEmitterRegistry
    {
        readonly Dictionary<Type, ITimelineSemanticTrackEmitter> m_TrackEmitters = new Dictionary<Type, ITimelineSemanticTrackEmitter>();
        readonly Dictionary<Type, ITimelineSemanticClipEmitter> m_ClipEmitters = new Dictionary<Type, ITimelineSemanticClipEmitter>();

        public void Register(ITimelineSemanticTrackEmitter emitter)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));
            if (!m_TrackEmitters.TryAdd(emitter.SourceType, emitter))
                throw new InvalidOperationException($"Timeline Track emitter for '{emitter.SourceType.FullName}' is already registered.");
        }

        public void Register(ITimelineSemanticClipEmitter emitter)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));
            if (!m_ClipEmitters.TryAdd(emitter.SourceType, emitter))
                throw new InvalidOperationException($"Timeline Clip emitter for '{emitter.SourceType.FullName}' is already registered.");
        }

        public bool TryGetTrack(Type type, out ITimelineSemanticTrackEmitter emitter) => m_TrackEmitters.TryGetValue(type, out emitter);
        public bool TryGetClip(Type type, out ITimelineSemanticClipEmitter emitter) => m_ClipEmitters.TryGetValue(type, out emitter);

        public static TimelineSemanticEmitterRegistry CreateDefault()
        {
            var registry = new TimelineSemanticEmitterRegistry();
            TimelineAnimationEmitterRegistration.Register(registry);
            TimelineMotionEmitterRegistration.Register(registry);
            TimelineTreeEmitterRegistration.Register(registry);
            TimelineCueEmitterRegistration.Register(registry);
            TimelineCameraEmitterRegistration.Register(registry);
            TimelineSceneParameterEmitterRegistration.Register(registry);
            return registry;
        }

        internal void RegisterTrack<T>(Action<TimelineSemanticEmitterContext> emit) where T : Track =>
            Register(new SimpleTrackEmitter<T>(emit));

        internal void RegisterClip<T>(Func<T, TimelineSemanticEmitterContext, OperationHandle> emit) where T : Clip =>
            Register(new SimpleClipEmitter<T>(emit));

        sealed class SimpleTrackEmitter<T> : ITimelineSemanticTrackEmitter where T : Track
        {
            readonly Action<TimelineSemanticEmitterContext> m_Emit;
            public SimpleTrackEmitter(Action<TimelineSemanticEmitterContext> emit) => m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
            public Type SourceType => typeof(T);
            public void Emit(Track track, TimelineSemanticEmitterContext context) => m_Emit(context);
        }

        sealed class SimpleClipEmitter<T> : ITimelineSemanticClipEmitter where T : Clip
        {
            readonly Func<T, TimelineSemanticEmitterContext, OperationHandle> m_Emit;
            public SimpleClipEmitter(Func<T, TimelineSemanticEmitterContext, OperationHandle> emit) => m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
            public Type SourceType => typeof(T);
            public OperationHandle Emit(Clip clip, TimelineSemanticEmitterContext context) => m_Emit((T)clip, context);
        }
    }

    public readonly struct TimelineSemanticTreeCompilation
    {
        readonly Func<string, OperationHandle> m_LifecycleResolver;

        public TimelineSemanticTreeCompilation(
            string route,
            OperationHandle entry,
            Func<string, OperationHandle> lifecycleResolver)
        {
            Route = route ?? string.Empty;
            Entry = entry;
            m_LifecycleResolver = lifecycleResolver;
        }

        public string Route { get; }
        public OperationHandle Entry { get; }
        public bool IsValid => Entry.IsValid;

        public OperationHandle Lifecycle(string port)
        {
            return m_LifecycleResolver == null
                ? OperationHandle.Invalid
                : m_LifecycleResolver(port);
        }
    }

    public sealed class TimelineSemanticEmitter
    {
        readonly TimelineSemanticEmitterRegistry m_Registry;

        public TimelineSemanticEmitter(TimelineSemanticEmitterRegistry registry)
        {
            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public TimelineSemanticEmissionResult Emit(TimelineSemanticEmissionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            TimelineSemanticContentRecord content = request.Content;
            CharacterSimulationProgramBuilder builder = request.Builder;
            TimelineSemanticInvocation invocation = request.Invocation;
            OperationHandle timelineOperation = request.TimelineOperation;
            OperationHandle treeStateScopeOwner = request.TreeStateScopeOwner;
            string actionContextIdentity = request.ActionContextIdentity;
            Func<TimelineSemanticClipRecord, OperationHandle, TimelineSemanticTreeCompilation> treeCompiler = request.TreeCompiler;
            TimelineData timeline = content.Timeline;
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (!invocation.IsValid)
                throw new ArgumentException("Timeline invocation is invalid.", nameof(request));

            string timelineRoute = invocation.Route;
            var timelineSource = new CharacterSimulationSourceLocation(
                timeline.GetType().FullName,
                invocation.GraphId,
                invocation.NodeId,
                string.Empty,
                timeline.AuthoringId,
                string.Empty,
                timelineRoute,
                contentHash: content.ContentUnit.ContentHash);
            int timelineCatalog = builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.Timeline,
                $"timeline:{timeline.AuthoringId}",
                1,
                new[]
                {
                    builder.ConstantField(timelineSource, "Name", timeline.Name),
                    builder.ConstantField(timelineSource, "Scale", timeline.Scale),
                    builder.ConstantField(timelineSource, "MaxFrame", content.MaxFrame),
                    builder.ConstantField(timelineSource, "FrameRate", content.ContentUnit.FrameRate)
                },
                timelineSource);
            if (timelineCatalog >= 0)
            {
                builder.DeclareReference(
                    invocation.ReferenceIdentity($"{timelineRoute}/timeline-catalog"),
                    timelineOperation,
                    ProgramReferenceKind.CatalogEntry,
                    timelineCatalog,
                    $"timeline:{timeline.AuthoringId}",
                    timelineSource);
            }

            var emission = new TimelineSemanticEmissionSession(timeline, builder, invocation);
            for (int trackIndex = 0; trackIndex < content.Tracks.Count; trackIndex++)
            {
                TimelineSemanticTrackRecord trackRecord = content.Tracks[trackIndex];
                Track track = trackRecord.Track;
                var context = new TimelineSemanticEmitterContext(
                    timeline,
                    track,
                    trackRecord.AuthoringIndex,
                    invocation,
                    builder,
                    timelineOperation,
                    actionContextIdentity,
                    emission,
                    content.ContentUnit.ContentHash);
                if (!m_Registry.TryGetTrack(track.GetType(), out ITimelineSemanticTrackEmitter trackEmitter))
                    throw new InvalidOperationException($"Discovered Track '{track.AuthoringId}' has no emitter.");
                trackEmitter.Emit(track, context);

                for (int clipIndex = 0; clipIndex < trackRecord.Clips.Count; clipIndex++)
                {
                    TimelineSemanticClipRecord clipRecord = trackRecord.Clips[clipIndex];
                    Clip clip = clipRecord.Clip;
                    if (!m_Registry.TryGetClip(clip.GetType(), out ITimelineSemanticClipEmitter clipEmitter))
                        throw new InvalidOperationException($"Discovered Clip '{clip.AuthoringId}' has no emitter.");
                    OperationHandle clipOperation;
                    try
                    {
                        clipOperation = clipEmitter.Emit(clip, context);
                    }
                    catch (Exception exception)
                    {
                        builder.Report.EmissionError("timeline_clip_emit_failed", context.ClipSource(clip).Identity, exception.Message);
                        continue;
                    }
                    builder.DeclareControlFlow(
                        $"{context.ClipSource(clip).Identity}/segment",
                        timelineOperation,
                        clipOperation,
                        track.AuthoringId,
                        clip.AuthoringId,
                        ProgramControlFlowKind.Child,
                        clipRecord.AuthoringIndex,
                        0,
                        ProgramAbortPolicy.None,
                        false,
                        OperationHandle.Invalid,
                        context.ClipSource(clip));
                    if (!(clip is TreeClip))
                        continue;
                    if (treeCompiler == null)
                    {
                        builder.Report.Error("tree_clip_compiler_missing", context.ClipSource(clip).Identity, "TreeClip has no shared semantic tree compiler.");
                        continue;
                    }
                    TimelineSemanticTreeCompilation tree;
                    try
                    {
                        tree = treeCompiler(clipRecord, treeStateScopeOwner);
                    }
                    catch (Exception exception)
                    {
                        builder.Report.Error("tree_clip_compile_failed", context.ClipSource(clip).Identity, exception.Message);
                        continue;
                    }
                    if (!tree.IsValid)
                    {
                        builder.Report.Error("tree_clip_compile_failed", context.ClipSource(clip).Identity, "TreeClip did not produce a compiled tree entry.");
                        continue;
                    }
                    builder.DeclareControlFlow(
                        $"{tree.Route}/entry",
                        clipOperation,
                        tree.Entry,
                        "TreeClip",
                        "Entry",
                        ProgramControlFlowKind.Enter,
                        0,
                        0,
                        ProgramAbortPolicy.None,
                        false,
                        OperationHandle.Invalid,
                        context.ClipSource(clip));
                    DeclareLifecycle(builder, tree, clipOperation, context.ClipSource(clip), "OnEnable", 1, ProgramControlFlowKind.Enter);
                    DeclareLifecycle(builder, tree, clipOperation, context.ClipSource(clip), "OnDisable", 0, ProgramControlFlowKind.Exit);
                    DeclareLifecycle(builder, tree, clipOperation, context.ClipSource(clip), "OnDestroy", 1, ProgramControlFlowKind.Exit);
                }
            }
            emission.Complete();
            return new TimelineSemanticEmissionResult(timelineOperation, emission.ClipOperations, builder.Report);
        }

        static void DeclareLifecycle(
            CharacterSimulationProgramBuilder builder,
            TimelineSemanticTreeCompilation tree,
            OperationHandle clipOperation,
            CharacterSimulationSourceLocation source,
            string port,
            int order,
            ProgramControlFlowKind kind)
        {
            OperationHandle target = tree.Lifecycle(port);
            if (!target.IsValid)
            {
                builder.Report.Error("tree_clip_lifecycle_missing", tree.Route, $"TreeClip graph is missing compiled '{port}' lifecycle operation.");
                return;
            }
            builder.DeclareControlFlow(
                $"{tree.Route}/{port}",
                clipOperation,
                target,
                port,
                "Entry",
                kind,
                order,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
        }
    }
}
