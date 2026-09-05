using System;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticTimelineEmitter
    {
        readonly CharacterSimulationTimelineEmitterRegistry m_Emitters;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly Func<CharacterAuthoringGraphOccurrence, OperationHandle, OperationHandle> m_CompileGraph;
        readonly TryGetCompiledOperation m_TryGetCompiledOperation;

        public CharacterSemanticTimelineEmitter(
            CharacterSimulationTimelineEmitterRegistry emitters,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            Func<CharacterAuthoringGraphOccurrence, OperationHandle, OperationHandle> compileGraph,
            TryGetCompiledOperation tryGetCompiledOperation)
        {
            m_Emitters = emitters ?? throw new ArgumentNullException(nameof(emitters));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_CompileGraph = compileGraph ?? throw new ArgumentNullException(nameof(compileGraph));
            m_TryGetCompiledOperation = tryGetCompiledOperation ?? throw new ArgumentNullException(nameof(tryGetCompiledOperation));
        }

        public void Emit(
            CharacterAuthoringTimelineRecord record,
            OperationHandle owner,
            OperationHandle stateScopeOwner)
        {
            TimelineNode node = record.Node;
            TimelineData timeline = record.Timeline;
            string route = record.Route;
            string ownerGraphId = node.Owner?.GraphAuthoringId ?? string.Empty;
            CharacterSimulationSourceLocation timelineSource = new CharacterSimulationSourceLocation(
                timeline.GetType().FullName,
                ownerGraphId,
                node.GUID,
                string.Empty,
                timeline.AuthoringId,
                string.Empty,
                route,
                contentHash: TimelineAuthoringFingerprint.Compute(timeline));
            int timelineCatalog = m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.Timeline,
                $"timeline:{timeline.AuthoringId}",
                1,
                Fields(
                    m_Builder.ConstantField(timelineSource, "Name", timeline.Name),
                    m_Builder.ConstantField(timelineSource, "Scale", timeline.Scale),
                    m_Builder.ConstantField(timelineSource, "MaxFrame", timeline.MaxFrame),
                    m_Builder.ConstantField(timelineSource, "FrameRate", TimelineUtility.FrameRate)),
                timelineSource);
            if (timelineCatalog >= 0)
            {
                m_Builder.DeclareReference(
                    $"{record.GraphRoute}/node:{node.GUID}/timeline-catalog",
                    owner,
                    ProgramReferenceKind.CatalogEntry,
                    timelineCatalog,
                    $"timeline:{timeline.AuthoringId}",
                    NodeSource(node.Owner as BaseTree, node, record.GraphRoute));
            }

            var timelineEmission = new CharacterSimulationTimelineEmissionSession(timeline, m_Builder);
            foreach (CharacterAuthoringTrackRecord trackRecord in record.Tracks)
            {
                Track track = trackRecord.Track;
                var context = new CharacterSimulationTimelineEmitterContext(
                    timeline,
                    track,
                    trackRecord.AuthoringIndex,
                    ownerGraphId,
                    node.GUID,
                    route,
                    m_Builder,
                    owner,
                    CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext),
                    timelineEmission);
                if (m_Emitters.TryGetTrack(track.GetType(), out ICharacterSimulationTimelineTrackEmitter trackEmitter))
                    trackEmitter.Emit(track, context);
                else
                    throw new InvalidOperationException($"Discovered Track '{track.AuthoringId}' has no emitter.");
                foreach (CharacterAuthoringClipRecord clipRecord in trackRecord.Clips)
                {
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
                        m_Report.EmissionError("timeline_clip_emit_failed", context.ClipSource(clip).Identity, exception.Message);
                        continue;
                    }
                    m_Builder.DeclareControlFlow(
                        $"{context.ClipSource(clip).Identity}/segment",
                        owner,
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
                    if (clip is not TreeClip || clipRecord.TreeGraph == null)
                        continue;
                    string treeRoute = clipRecord.TreeGraph.Route;
                    OperationHandle treeEntry = m_CompileGraph(clipRecord.TreeGraph, stateScopeOwner);
                    if (!treeEntry.IsValid)
                        continue;
                    m_Builder.DeclareControlFlow(
                        $"{treeRoute}/entry",
                        clipOperation,
                        treeEntry,
                        "TreeClip",
                        "Entry",
                        ProgramControlFlowKind.Enter,
                        0,
                        0,
                        ProgramAbortPolicy.None,
                        false,
                        OperationHandle.Invalid,
                        context.ClipSource(clip));
                    DeclareTreeClipLifecycle((TimelineRunningTree)clipRecord.TreeGraph.Graph, clipOperation, treeRoute, context.ClipSource(clip));
                }
            }
            timelineEmission.Complete();
        }

        void DeclareTreeClipLifecycle(
            TimelineRunningTree tree,
            OperationHandle clipOperation,
            string treeRoute,
            CharacterSimulationSourceLocation source)
        {
            DeclareTreeClipLifecycleEdge(tree.OnEnableGUID, "OnEnable", ProgramControlFlowKind.Enter, 1);
            DeclareTreeClipLifecycleEdge(tree.OnDisableGUID, "OnDisable", ProgramControlFlowKind.Exit, 0);
            DeclareTreeClipLifecycleEdge(tree.OnDestroyGUID, "OnDestroy", ProgramControlFlowKind.Exit, 1);

            void DeclareTreeClipLifecycleEdge(string nodeId, string port, ProgramControlFlowKind kind, int order)
            {
                if (!m_TryGetCompiledOperation(treeRoute, nodeId, out OperationHandle target))
                {
                    m_Report.Error("tree_clip_lifecycle_missing", treeRoute, $"TreeClip graph is missing compiled '{port}' lifecycle operation.");
                    return;
                }
                m_Builder.DeclareControlFlow(
                    $"{treeRoute}/{port}",
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

        static CharacterSimulationSourceLocation NodeSource(BaseTree graph, BaseNode node, string route)
        {
            return new CharacterSimulationSourceLocation(
                node.GetType().FullName,
                graph?.GraphAuthoringId ?? node.Owner?.GraphAuthoringId ?? string.Empty,
                node.GUID,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/node:{node.GUID}",
                contentHash: GraphAuthoringFingerprint.Compute(graph ?? node.Owner));
        }

        static ProgramCatalogField[] Fields(params ProgramCatalogField[] values) =>
            Array.FindAll(values, value => value != null);

        internal delegate bool TryGetCompiledOperation(string route, string nodeId, out OperationHandle operation);
    }
}
