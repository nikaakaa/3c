#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillGraphClosureIndex
    {
        public readonly Dictionary<string, FlowGraph> Graphs =
            new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        public readonly Dictionary<string, BtsmtlSkillNativeStateMachine> StateMachines =
            new Dictionary<string, BtsmtlSkillNativeStateMachine>(StringComparer.Ordinal);
        public readonly Dictionary<string, TimelineAsset> Timelines =
            new Dictionary<string, TimelineAsset>(StringComparer.Ordinal);
        public readonly Dictionary<RuntimeSourceElementKey, IGraphElement> Elements = new();
        readonly HashSet<FlowGraph> m_Visited = new HashSet<FlowGraph>();
        readonly HashSet<BtsmtlSkillNativeStateMachine> m_VisitedStateMachines =
            new HashSet<BtsmtlSkillNativeStateMachine>();

        public void Build(CharacterPipelineDefinition definition)
        {
            Graphs.Clear();
            StateMachines.Clear();
            Timelines.Clear();
            Elements.Clear();
            m_Visited.Clear();
            m_VisitedStateMachines.Clear();
            IReadOnlyList<BtsmtlSkillFlowGraph> roots = definition.AbilityGraphs;
            foreach (BtsmtlSkillFlowGraph root in roots)
                Visit(root);
        }

        void Visit(FlowGraph graph)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring || !m_Visited.Add(graph))
                return;
            Graphs.Add(authoring.AuthoringId, graph);
            foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
            {
                Elements.Add(RuntimeSourceElementKey.Node(authoring.AuthoringId, node.UID), node);
                for (int i = 0; i < node.outConnections.Count; i++)
                {
                    Connection edge = node.outConnections[i];
                    Elements.Add(RuntimeSourceElementKey.Edge(authoring.AuthoringId, edge.UID), edge);
                }
                if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                    Visit(macroGraph);
                if (node is BtsmtlSkillStateMachineFlowNode machine)
                    VisitStateMachine(machine.StateMachine);
                if (node is BtsmtlSkillStateFlowNode state)
                    Visit(state.Body);
                if (node is BtsmtlSkillCompositeFlowNode composite)
                    foreach (BtsmtlSkillStepPort step in composite.Steps)
                        Visit(step.Condition);
                foreach (BtsmtlSkillFlowConnection transfer in node.outConnections.OfType<BtsmtlSkillFlowConnection>())
                    Visit(transfer.Condition);
                if (node is BtsmtlSkillTimelineFlowNode timeline && timeline.TimelineAsset)
                {
                    string timelineId = timeline.Timeline.AuthoringId;
                    Timelines[timelineId] = timeline.TimelineAsset;
                    foreach (Track track in timeline.Timeline.Tracks)
                    {
                        foreach (Clip clip in track.Clips)
                            if (clip is BTSMTL.Timeline.TreeClip tree && tree.AssetTree is BtsmtlSkillFlowGraph child)
                                Visit(child);
                        foreach (TimelineMarker marker in track.Markers)
                            if (marker.Graph is BtsmtlSkillFlowGraph trigger)
                                Visit(trigger);
                    }
                }
            }
        }

        void VisitStateMachine(BtsmtlSkillNativeStateMachine machine)
        {
            if (machine == null || !m_VisitedStateMachines.Add(machine))
                return;
            StateMachines[machine.AuthoringId] = machine;
            foreach (BtsmtlSkillFlowGraph child in BtsmtlSkillNativeStateMachineContract.References(machine))
                Visit(child);
        }
    }
}
#endif
