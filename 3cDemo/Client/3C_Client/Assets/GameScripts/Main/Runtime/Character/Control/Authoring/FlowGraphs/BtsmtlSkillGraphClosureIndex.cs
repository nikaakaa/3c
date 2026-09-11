#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.Pipeline;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillGraphClosureIndex
    {
        public readonly Dictionary<string, FlowGraph> Graphs =
            new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        public readonly Dictionary<string, TimelineAsset> Timelines =
            new Dictionary<string, TimelineAsset>(StringComparer.Ordinal);
        readonly HashSet<FlowGraph> m_Visited = new HashSet<FlowGraph>();

        public void Build(CharacterPipelineDefinition definition)
        {
            Graphs.Clear();
            Timelines.Clear();
            m_Visited.Clear();
            foreach (BtsmtlSkillFlowGraph root in definition.SkillGraphs ?? Array.Empty<BtsmtlSkillFlowGraph>())
                Visit(root);
        }

        void Visit(FlowGraph graph)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring || !m_Visited.Add(graph))
                return;
            if (Graphs.TryGetValue(authoring.AuthoringId, out FlowGraph existing) && existing != graph)
                throw new InvalidOperationException($"Skill Graph identity重复：{authoring.AuthoringId}");
            Graphs.Add(authoring.AuthoringId, graph);
            foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
            {
                if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                    Visit(macroGraph);
                if (node is BtsmtlSkillStateMachineFlowNode machine)
                    Visit(machine.StateMachine);
                if (node is BtsmtlSkillStateFlowNode state)
                    Visit(state.Body);
                if (node is BtsmtlSkillCompositeFlowNode composite)
                    foreach (BtsmtlSkillStepPort step in composite.Steps)
                        Visit(step.Condition);
                if (node is BtsmtlSkillTimelineFlowNode timeline && timeline.TimelineAsset)
                {
                    string timelineId = timeline.Timeline.AuthoringId;
                    if (Timelines.TryGetValue(timelineId, out TimelineAsset existingTimeline) &&
                        existingTimeline != timeline.TimelineAsset)
                        throw new InvalidOperationException($"Skill Timeline identity重复：{timelineId}");
                    Timelines[timelineId] = timeline.TimelineAsset;
                    foreach (Track track in timeline.Timeline.Tracks)
                        foreach (Clip clip in track.Clips)
                            if (clip is BTSMTL.Timeline.TreeClip tree && tree.AssetTree is BtsmtlSkillFlowGraph child)
                                Visit(child);
                }
            }
        }
    }
}
#endif
