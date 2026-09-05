using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class TimelineSemanticRootEmitter
    {
        readonly CharacterSimulationTimelineEmitterRegistry m_Emitters;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;

        public TimelineSemanticRootEmitter(
            CharacterSimulationTimelineEmitterRegistry emitters,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report)
        {
            m_Emitters = emitters ?? throw new ArgumentNullException(nameof(emitters));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public OperationHandle Emit(TimelineSemanticContentRecord content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            TimelineData timeline = content.Timeline;
            var source = new CharacterSimulationSourceLocation(
                typeof(TimelineAsset).FullName,
                string.Empty,
                string.Empty,
                string.Empty,
                timeline.AuthoringId,
                string.Empty,
                content.Route,
                contentHash: content.ContentHash);
            int timelineCatalog = m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.Timeline,
                $"timeline:{timeline.AuthoringId}",
                1,
                new[]
                {
                    m_Builder.ConstantField(source, "Name", timeline.Name),
                    m_Builder.ConstantField(source, "Scale", timeline.Scale),
                    m_Builder.ConstantField(source, "MaxFrame", timeline.MaxFrame),
                    m_Builder.ConstantField(source, "FrameRate", TimelineUtility.FrameRate)
                },
                source);
            OperationHandle timelineOperation = m_Builder.DeclareOperation(
                source,
                SimulationOperationCode.Timeline,
                Array.Empty<int>(),
                integer0: (int)TimelinePlaybackMode.Once,
                text0: timeline.AuthoringId);
            if (timelineCatalog >= 0)
            {
                m_Builder.DeclareReference(
                    $"{content.Route}/timeline-catalog",
                    timelineOperation,
                    ProgramReferenceKind.CatalogEntry,
                    timelineCatalog,
                    $"timeline:{timeline.AuthoringId}",
                    source);
            }

            var emission = new CharacterSimulationTimelineEmissionSession(timeline, m_Builder);
            for (int trackIndex = 0; trackIndex < content.Tracks.Count; trackIndex++)
            {
                CharacterAuthoringTrackRecord trackRecord = content.Tracks[trackIndex];
                Track track = trackRecord.Track;
                var context = new CharacterSimulationTimelineEmitterContext(
                    timeline,
                    track,
                    trackRecord.AuthoringIndex,
                    string.Empty,
                    string.Empty,
                    content.Route,
                    m_Builder,
                    timelineOperation,
                    string.Empty,
                    emission,
                    false);
                if (!m_Emitters.TryGetTrack(track.GetType(), out ICharacterSimulationTimelineTrackEmitter trackEmitter))
                    throw new InvalidOperationException($"Discovered Track '{track.AuthoringId}' has no emitter.");
                trackEmitter.Emit(track, context);
                for (int clipIndex = 0; clipIndex < trackRecord.Clips.Count; clipIndex++)
                {
                    CharacterAuthoringClipRecord clipRecord = trackRecord.Clips[clipIndex];
                    Clip clip = clipRecord.Clip;
                    if (!m_Emitters.TryGetClip(clip.GetType(), out ICharacterSimulationTimelineClipEmitter clipEmitter))
                        throw new InvalidOperationException($"Discovered Clip '{clip.AuthoringId}' has no emitter.");
                    OperationHandle clipOperation;
                    try
                    {
                        clipOperation = clipEmitter.Emit(clip, context);
                    }
                    catch (Exception exception)
                    {
                        m_Report.EmissionError(
                            "timeline_clip_emit_failed",
                            context.ClipSource(clip).Identity,
                            exception.Message);
                        continue;
                    }
                    m_Builder.DeclareControlFlow(
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
                }
            }
            emission.Complete();
            return timelineOperation;
        }
    }
}
