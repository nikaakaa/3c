using System;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticTimelineEmitter
    {
        readonly TimelineSemanticEmitter m_Emitter;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly Func<CharacterAuthoringGraphOccurrence, OperationHandle, OperationHandle> m_CompileGraph;
        readonly TryGetCompiledOperation m_TryGetCompiledOperation;

        public CharacterSemanticTimelineEmitter(
            TimelineSemanticEmitterRegistry emitters,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            Func<CharacterAuthoringGraphOccurrence, OperationHandle, OperationHandle> compileGraph,
            TryGetCompiledOperation tryGetCompiledOperation)
        {
            m_Emitter = new TimelineSemanticEmitter(emitters);
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
            TimelineSemanticInvocation invocation;
            try
            {
                invocation = TimelineSemanticInvocation.ForCharacterGraph(
                    record.Route,
                    node.Owner?.GraphAuthoringId ?? string.Empty,
                    node.GUID,
                    node.PlaybackMode);
            }
            catch (Exception exception)
            {
                m_Report.Error("timeline_call_source_invalid", record.Route, exception.Message);
                return;
            }
            m_Emitter.Emit(new TimelineSemanticEmissionRequest(
                record.Content,
                m_Builder,
                invocation,
                owner,
                stateScopeOwner,
                CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext),
                (clip, treeOwner) => CompileTree(record, clip, treeOwner)));
        }

        TimelineSemanticTreeCompilation CompileTree(
            CharacterAuthoringTimelineRecord record,
            TimelineSemanticClipRecord clip,
            OperationHandle owner)
        {
            if (clip.Clip is not TreeClip ||
                !record.TreeGraphs.TryGetValue(clip.Clip.AuthoringId, out CharacterAuthoringGraphOccurrence graph))
            {
                return default;
            }
            OperationHandle entry = m_CompileGraph(graph, owner);
            var tree = (TimelineRunningTree)graph.Graph;
            return new TimelineSemanticTreeCompilation(graph.Route, entry, port =>
            {
                string nodeId = port switch
                {
                    "OnEnable" => tree.OnEnableGUID,
                    "OnDisable" => tree.OnDisableGUID,
                    "OnDestroy" => tree.OnDestroyGUID,
                    _ => string.Empty
                };
                return m_TryGetCompiledOperation(graph.Route, nodeId, out OperationHandle target)
                    ? target
                    : OperationHandle.Invalid;
            });
        }

        internal delegate bool TryGetCompiledOperation(string route, string nodeId, out OperationHandle operation);
    }
}
