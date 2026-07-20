using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    sealed class CharacterSimulationTimelineEmissionSession
    {
        readonly TimelineData m_Timeline;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly Dictionary<string, EmittedClip> m_Clips = new Dictionary<string, EmittedClip>(StringComparer.Ordinal);
        readonly List<PendingMotionSource> m_PendingMotionSources = new List<PendingMotionSource>();
        bool m_MotionWarpValidated;

        public CharacterSimulationTimelineEmissionSession(
            TimelineData timeline,
            CharacterSimulationProgramBuilder builder)
        {
            m_Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void RecordClip(
            Clip clip,
            OperationHandle operation,
            string identity,
            CharacterSimulationSourceLocation source)
        {
            if (!m_Clips.TryAdd(clip.AuthoringId, new EmittedClip(clip, operation, identity, source)))
                m_Builder.Report.Error("timeline_clip_identity_duplicate", source.Identity, $"Timeline clip identity '{clip.AuthoringId}' is duplicated.");
        }

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
                    $"timeline:{m_Timeline.AuthoringId}/clip:{pending.Warp.AuthoringId}/motion-source",
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

    public interface ICharacterSimulationTimelineTrackEmitter
    {
        Type SourceType { get; }
        void Emit(Track track, CharacterSimulationTimelineEmitterContext context);
    }

    public interface ICharacterSimulationTimelineClipEmitter
    {
        Type SourceType { get; }
        OperationHandle Emit(Clip clip, CharacterSimulationTimelineEmitterContext context);
    }

    public sealed class CharacterSimulationTimelineEmitterContext
    {
        readonly TimelineData m_Timeline;
        readonly Track m_Track;
        readonly int m_TrackIndex;
        readonly string m_OwnerGraphId;
        readonly string m_OwnerNodeId;
        readonly string m_Route;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationTimelineEmissionSession m_Session;

        internal CharacterSimulationTimelineEmitterContext(
            TimelineData timeline,
            Track track,
            int trackIndex,
            string ownerGraphId,
            string ownerNodeId,
            string route,
            CharacterSimulationProgramBuilder builder,
            OperationHandle timelineOperation,
            string actionContextIdentity,
            CharacterSimulationTimelineEmissionSession session)
        {
            m_Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            m_Track = track ?? throw new ArgumentNullException(nameof(track));
            m_TrackIndex = trackIndex;
            m_OwnerGraphId = string.IsNullOrEmpty(ownerGraphId)
                ? throw new ArgumentException("Timeline owner Graph identity is required.", nameof(ownerGraphId))
                : ownerGraphId;
            m_OwnerNodeId = string.IsNullOrEmpty(ownerNodeId)
                ? throw new ArgumentException("Timeline owner Node identity is required.", nameof(ownerNodeId))
                : ownerNodeId;
            m_Route = route ?? string.Empty;
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
            trackId: m_Track.AuthoringId);

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
                trackId: m_Track.AuthoringId);
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
                ClipIdentity(clip),
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
                    $"{ClipIdentity(clip)}/catalog",
                    operation,
                    ProgramReferenceKind.CatalogEntry,
                    catalog,
                    ClipIdentity(clip),
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
        public string ProducerIdentity(Clip clip) => $"producer:{m_Timeline.AuthoringId}:{m_Track.AuthoringId}:{clip.AuthoringId}";
        public string AnimationProducerIdentity => $"producer:{m_Timeline.AuthoringId}:{m_Track.AuthoringId}";

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

    public sealed class CharacterSimulationTimelineEmitterRegistry
    {
        readonly Dictionary<Type, ICharacterSimulationTimelineTrackEmitter> m_TrackEmitters = new Dictionary<Type, ICharacterSimulationTimelineTrackEmitter>();
        readonly Dictionary<Type, ICharacterSimulationTimelineClipEmitter> m_ClipEmitters = new Dictionary<Type, ICharacterSimulationTimelineClipEmitter>();

        public void Register(ICharacterSimulationTimelineTrackEmitter emitter)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));
            if (!m_TrackEmitters.TryAdd(emitter.SourceType, emitter))
                throw new InvalidOperationException($"Timeline Track emitter for '{emitter.SourceType.FullName}' is already registered.");
        }

        public void Register(ICharacterSimulationTimelineClipEmitter emitter)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));
            if (!m_ClipEmitters.TryAdd(emitter.SourceType, emitter))
                throw new InvalidOperationException($"Timeline Clip emitter for '{emitter.SourceType.FullName}' is already registered.");
        }

        public bool TryGetTrack(Type type, out ICharacterSimulationTimelineTrackEmitter emitter) => m_TrackEmitters.TryGetValue(type, out emitter);
        public bool TryGetClip(Type type, out ICharacterSimulationTimelineClipEmitter emitter) => m_ClipEmitters.TryGetValue(type, out emitter);

        public static CharacterSimulationTimelineEmitterRegistry CreateDefault()
        {
            var registry = new CharacterSimulationTimelineEmitterRegistry();
            registry.Register(new SimpleTrackEmitter<AnimationTrack>(context =>
            {
                var track = (AnimationTrack)context.Track;
                context.DeclareTrackCatalog(context.Builder.ConstantField(context.TrackSource, "LayerId", track.LayerId));
                context.Builder.DeclareProducer(
                    context.AnimationProducerIdentity,
                    track.LayerId,
                    context.TrackIdentity,
                    ProgramOutputChannelKind.Presentation,
                    context.TrackSource);
            }));
            registry.Register(new SimpleTrackEmitter<MotionCurveTrack>(context => context.DeclareTrackCatalog()));
            registry.Register(new SimpleTrackEmitter<MotionWarpTrack>(context =>
            {
                context.ValidateMotionWarp();
                context.DeclareTrackCatalog();
            }));
            registry.Register(new SimpleTrackEmitter<TreeTrack>(context => context.DeclareTrackCatalog()));
            registry.Register(new SimpleTrackEmitter<ActionCueTrack>(context => context.DeclareTrackCatalog()));
            registry.Register(new SimpleTrackEmitter<CameraStateTrack>(context => context.DeclareTrackCatalog()));
            registry.Register(new SimpleTrackEmitter<CameraCueTrack>(context => context.DeclareTrackCatalog()));
            registry.Register(new SimpleTrackEmitter<CameraResponseTrack>(context => context.DeclareTrackCatalog()));
            registry.Register(new SimpleClipEmitter<BTSMTL.Timeline.AnimationClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                string producer = context.AnimationProducerIdentity;
                int producerIndex = context.Builder.DeclareProducer(
                    producer,
                    ((AnimationTrack)context.Track).LayerId,
                    context.TrackIdentity,
                    ProgramOutputChannelKind.Presentation,
                    source);
                OperationHandle operation = context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineAnimation,
                    new[]
                    {
                        context.Builder.ConstantField(source, "Extrapolation", clip.ExtraPolationMode),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve)),
                        context.Builder.IdentityField("Producer", producer)
                    },
                    producer);
                if (producerIndex >= 0)
                {
                    context.Builder.DeclareReference(
                        $"{context.ClipIdentity(clip)}/producer",
                        operation,
                        ProgramReferenceKind.Producer,
                        producerIndex,
                        producer,
                        source);
                }
                return operation;
            }));
            registry.Register(new SimpleClipEmitter<MotionCurveClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                context.Builder.RequireGameplayCapability("TimelineMotionCurve");
                context.Builder.RequireWorldRequest("CharacterBodyMotion", WorldCapability.BodyMotion | WorldCapability.Grounding | WorldCapability.Collision);
                return context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineMotionCurve,
                    new[]
                    {
                        context.Builder.ConstantField(source, "CurveId", clip.CurveId),
                        context.Builder.ConstantField(source, "CurveEndFrame", clip.CurveEndFrame),
                        context.Builder.ConstantField(source, "Space", clip.Space),
                        context.Builder.ConstantField(source, "Channel", clip.Channel),
                        context.Builder.ConstantField(source, "BlendMode", clip.BlendMode),
                        context.Builder.ConstantField(source, "Priority", clip.Priority),
                        context.Builder.ConstantField(source, "ConsumeLowerChannels", clip.ConsumeLowerChannels),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "PositionX", context.BakeCurve(clip, "PositionX", clip.PositionX)),
                        context.Builder.ConstantField(source, "PositionY", context.BakeCurve(clip, "PositionY", clip.PositionY)),
                        context.Builder.ConstantField(source, "PositionZ", context.BakeCurve(clip, "PositionZ", clip.PositionZ)),
                        context.Builder.ConstantField(source, "Yaw", context.BakeCurve(clip, "Yaw", clip.Yaw)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                    },
                    clip.CurveId);
            }));
            registry.Register(new SimpleClipEmitter<MotionWarpClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                if (string.IsNullOrEmpty(context.ActionContextIdentity))
                    throw new InvalidOperationException($"MotionWarp '{clip.AuthoringId}' requires an explicit Timeline Action Context.");
                context.Builder.RequireGameplayCapability("TimelineMotionWarp");
                OperationHandle operation = context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineMotionWarp,
                    new[]
                    {
                        context.Builder.IdentityField("SourceMotionClip", $"timeline:{context.Timeline.AuthoringId}/clip:{clip.SourceMotionClipId}"),
                        context.Builder.IdentityField("TimelineOwner", $"timeline:{context.Timeline.AuthoringId}"),
                        context.Builder.IdentityField("ActionContext", context.ActionContextIdentity),
                        context.Builder.ConstantField(source, "TimelineOwnerOperation", context.TimelineOperation.Value),
                        context.Builder.ConstantField(source, "PositionMode", clip.PositionMode),
                        context.Builder.ConstantField(source, "RotationMode", clip.RotationMode),
                        context.Builder.ConstantField(source, "TargetLocalPlanarOffset", clip.TargetLocalPlanarOffset),
                        context.Builder.ConstantField(source, "TargetYawOffsetDegrees", clip.TargetYawOffsetDegrees),
                        context.Builder.ConstantField(source, "PositionWeight", clip.PositionWeight),
                        context.Builder.ConstantField(source, "YawWeight", clip.YawWeight),
                        context.Builder.ConstantField(source, "MaxTotalPositionCorrection", clip.MaxTotalPositionCorrection),
                        context.Builder.ConstantField(source, "MaxTotalYawCorrectionDegrees", clip.MaxTotalYawCorrectionDegrees),
                        context.Builder.ConstantField(source, "PositionProgressCurve", context.BakeCurve(clip, "PositionProgressCurve", clip.PositionProgressCurve)),
                        context.Builder.ConstantField(source, "YawProgressCurve", context.BakeCurve(clip, "YawProgressCurve", clip.YawProgressCurve))
                    },
                    clip.SourceMotionClipId,
                    integer0: (int)clip.PositionMode,
                    integer1: (int)clip.RotationMode);
                context.DeferMotionSourceReference(clip, operation);
                return operation;
            }));
            registry.Register(new SimpleClipEmitter<TreeClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineTreeClip,
                    new[]
                    {
                        context.Builder.ConstantField(source, "ExecutionPhase", clip.ExecutionPhase),
                        context.Builder.ConstantField(source, "Ownership", clip.Ownership),
                        context.Builder.IdentityField("Graph", clip.ResolvedTree?.GraphAuthoringId)
                    },
                    clip.ResolvedTree?.GraphAuthoringId,
                    integer0: (int)clip.ExecutionPhase,
                    integer1: (int)clip.Ownership);
            }));
            registry.Register(new SimpleClipEmitter<ActionCueClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                string producer = context.ProducerIdentity(clip);
                int producerIndex = context.Builder.DeclareProducer(
                    producer,
                    "Cue",
                    context.ClipIdentity(clip),
                    ProgramOutputChannelKind.Presentation,
                    source);
                OperationHandle operation = context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineCue,
                    new[]
                    {
                        context.Builder.ConstantField(source, "CueId", clip.CueId),
                        context.Builder.ConstantField(source, "CueType", clip.CueType),
                        context.Builder.IdentityField("Producer", producer)
                    },
                    producer);
                if (producerIndex >= 0)
                {
                    context.Builder.DeclareReference(
                        $"{context.ClipIdentity(clip)}/producer",
                        operation,
                        ProgramReferenceKind.Producer,
                        producerIndex,
                        producer,
                        source);
                }
                return operation;
            }));
            registry.Register(new SimpleClipEmitter<CameraStateClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return DeclarePresentationClip(
                    clip,
                    context,
                    SimulationOperationCode.TimelineCameraState,
                    new[]
                    {
                        context.Builder.ConstantField(source, "Mode", clip.Mode),
                        context.Builder.ConstantField(source, "Priority", clip.Priority),
                        context.Builder.ConstantField(source, "BlendInSeconds", clip.BlendInSeconds),
                        context.Builder.ConstantField(source, "BlendOutSeconds", clip.BlendOutSeconds),
                        context.Builder.ConstantField(source, "TargetKey", clip.TargetKey),
                        context.Builder.ConstantField(source, "InterruptPolicy", clip.InterruptPolicy),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                    });
            }));
            registry.Register(new SimpleClipEmitter<CameraCueClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return DeclarePresentationClip(
                    clip,
                    context,
                    SimulationOperationCode.TimelineCameraCue,
                    new[]
                    {
                        context.Builder.ConstantField(source, "CueId", clip.CueId),
                        context.Builder.ConstantField(source, "CueKind", clip.CueKind),
                        context.Builder.ConstantField(source, "CueType", clip.CueType),
                        context.Builder.ConstantField(source, "Intensity", clip.Intensity),
                        context.Builder.ConstantField(source, "DurationSeconds", clip.DurationSeconds),
                        context.Builder.ConstantField(source, "Priority", clip.Priority)
                    });
            }));
            registry.Register(new SimpleClipEmitter<CameraResponseClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return DeclarePresentationClip(
                    clip,
                    context,
                    SimulationOperationCode.TimelineCameraResponse,
                    new[]
                    {
                        context.Builder.ConstantField(source, "LookResponse", clip.LookResponse),
                        context.Builder.ConstantField(source, "ManualOrbitWeight", clip.ManualOrbitWeight),
                        context.Builder.ConstantField(source, "PitchResponseWeight", clip.PitchResponseWeight),
                        context.Builder.ConstantField(source, "YawResponseWeight", clip.YawResponseWeight),
                        context.Builder.ConstantField(source, "Priority", clip.Priority),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                    });
            }));
            return registry;
        }

        static OperationHandle DeclarePresentationClip(
            Clip clip,
            CharacterSimulationTimelineEmitterContext context,
            SimulationOperationCode code,
            IEnumerable<ProgramCatalogField> fields)
        {
            CharacterSimulationSourceLocation source = context.ClipSource(clip);
            string producer = context.ProducerIdentity(clip);
            int producerIndex = context.Builder.DeclareProducer(
                producer,
                "Camera",
                context.ClipIdentity(clip),
                ProgramOutputChannelKind.Presentation,
                source);
            OperationHandle operation = context.DeclareClipOperation(
                clip,
                code,
                fields.Concat(new[] { context.Builder.IdentityField("Producer", producer) }),
                producer);
            if (producerIndex >= 0)
            {
                context.Builder.DeclareReference(
                    $"{context.ClipIdentity(clip)}/producer",
                    operation,
                    ProgramReferenceKind.Producer,
                    producerIndex,
                    producer,
                    source);
            }
            return operation;
        }

        sealed class SimpleTrackEmitter<T> : ICharacterSimulationTimelineTrackEmitter where T : Track
        {
            readonly Action<CharacterSimulationTimelineEmitterContext> m_Emit;
            public SimpleTrackEmitter(Action<CharacterSimulationTimelineEmitterContext> emit) => m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
            public Type SourceType => typeof(T);
            public void Emit(Track track, CharacterSimulationTimelineEmitterContext context) => m_Emit(context);
        }

        sealed class SimpleClipEmitter<T> : ICharacterSimulationTimelineClipEmitter where T : Clip
        {
            readonly Func<T, CharacterSimulationTimelineEmitterContext, OperationHandle> m_Emit;
            public SimpleClipEmitter(Func<T, CharacterSimulationTimelineEmitterContext, OperationHandle> emit) => m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
            public Type SourceType => typeof(T);
            public OperationHandle Emit(Clip clip, CharacterSimulationTimelineEmitterContext context) => m_Emit((T)clip, context);
        }
    }
}
