using System;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillTimelineCompiler
    {
        readonly TimelineSemanticEmitter m_Emitter;
        readonly GameplayAbilitySemanticBuilder m_Builder;
        readonly Func<BtsmtlSkillGraphOccurrence, OperationHandle, BtsmtlSkillInvocationContext, BtsmtlSkillGraphCompilation> m_Compile;

        public BtsmtlSkillTimelineCompiler(TimelineSemanticEmitterRegistry registry, GameplayAbilitySemanticBuilder builder,
            Func<BtsmtlSkillGraphOccurrence, OperationHandle, BtsmtlSkillInvocationContext, BtsmtlSkillGraphCompilation> compile)
        {
            m_Emitter = new TimelineSemanticEmitter(registry);
            m_Builder = builder;
            m_Compile = compile;
        }

        public void Emit(BtsmtlSkillGraphOccurrence graph, BtsmtlSkillTimelineOccurrence timeline,
            OperationHandle operation, OperationHandle stateOwner)
        {
            TimelineSemanticInvocation invocation = TimelineSemanticInvocation.ForCharacterGraph(timeline.Content.Route,
                graph.GraphId, timeline.Node.UID, timeline.Node.PlaybackMode);
            m_Emitter.Emit(new TimelineSemanticEmissionRequest(timeline.Content, m_Builder, invocation, operation, stateOwner,
                CharacterSimulationNodeEmitterContext.AssetIdentity(timeline.Node.ActionContext), (clip, owner) =>
                {
                    if (!timeline.Trees.TryGetValue(clip.Clip.AuthoringId, out BtsmtlSkillGraphOccurrence tree))
                        throw new InvalidOperationException($"{clip.Route}: 技能TreeClip没有原生节点图编译记录。");
                    BtsmtlSkillGraphCompilation compiled = m_Compile(tree, owner,
                        BtsmtlSkillInvocationContext.TreeClip(timeline.Node.UID, clip.Clip.AuthoringId,
                            ((TreeClip)clip.Clip).ExecutionPhase == TimelineTreeExecutionPhase.Commit));
                    return new TimelineSemanticTreeCompilation(tree.Route, compiled.Entry, port =>
                    {
                        BtsmtlSkillTimelineHookFlowNode hook = tree.Nodes.OfType<BtsmtlSkillTimelineHookFlowNode>()
                            .SingleOrDefault(node => string.Equals(node.Hook.ToString(), port, StringComparison.Ordinal));
                        return hook != null ? compiled.Operations.Node(hook.UID) : OperationHandle.Invalid;
                    });
                }));
        }
    }
}
