#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using UnityEditor;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillAuthoringClosure
    {
        BtsmtlSkillAuthoringClosure(
            IReadOnlyList<FlowGraph> graphs,
            IReadOnlyList<BtsmtlSkillNativeStateMachine> machines,
            IReadOnlyDictionary<BtsmtlSkillNativeStateMachine, FlowGraph> machineOwners,
            IReadOnlyList<TimelineAsset> timelines,
            IReadOnlyDictionary<TimelineAsset, FlowGraph> timelineOwners,
            IReadOnlyDictionary<FlowGraph, FlowGraph> graphOwners,
            IReadOnlyDictionary<FlowGraph, FlowGraph> graphPlacementOwners)
        {
            Graphs = graphs;
            Machines = machines;
            MachineOwners = machineOwners;
            Timelines = timelines;
            TimelineOwners = timelineOwners;
            GraphOwners = graphOwners;
            GraphPlacementOwners = graphPlacementOwners;
        }

        public IReadOnlyList<FlowGraph> Graphs { get; }
        public IReadOnlyList<BtsmtlSkillNativeStateMachine> Machines { get; }
        public IReadOnlyDictionary<BtsmtlSkillNativeStateMachine, FlowGraph> MachineOwners { get; }
        public IReadOnlyList<TimelineAsset> Timelines { get; }
        public IReadOnlyDictionary<TimelineAsset, FlowGraph> TimelineOwners { get; }
        public IReadOnlyDictionary<FlowGraph, FlowGraph> GraphOwners { get; }
        public IReadOnlyDictionary<FlowGraph, FlowGraph> GraphPlacementOwners { get; }

        public static bool IsPrivateSubAsset(UnityEngine.Object child, UnityEngine.Object owner)
        {
            string childPath = AssetDatabase.GetAssetPath(child);
            string ownerPath = AssetDatabase.GetAssetPath(owner);
            return AssetDatabase.IsSubAsset(child) &&
                !string.IsNullOrEmpty(childPath) &&
                string.Equals(childPath, ownerPath, StringComparison.Ordinal);
        }

        public static BtsmtlSkillAuthoringClosure Create(
            BtsmtlSkillFlowGraph root,
            bool requireComplete)
        {
            IReadOnlyList<FlowGraph> graphs = BtsmtlSkillGraphClosure.Validate(root, requireComplete);
            var graphOwners = new Dictionary<FlowGraph, FlowGraph>();
            var graphPlacementOwners = new Dictionary<FlowGraph, FlowGraph>();
            var machines = new List<BtsmtlSkillNativeStateMachine>();
            var machineOwners = new Dictionary<BtsmtlSkillNativeStateMachine, FlowGraph>();
            var timelines = new List<TimelineAsset>();
            var timelineOwners = new Dictionary<TimelineAsset, FlowGraph>();

            foreach (FlowGraph graph in graphs)
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
                {
                    if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                    {
                        AddGraphOwner(graphOwners, macroGraph, graph);
                        AddPlacementOwner(graphPlacementOwners, macroGraph, graph);
                    }
                    if (node is BtsmtlSkillStateMachineFlowNode stateMachine && stateMachine.StateMachine != null)
                    {
                        AddUnique(machines, stateMachine.StateMachine);
                        AddMachineOwner(machineOwners, stateMachine.StateMachine, graph);
                        foreach (BtsmtlSkillFlowGraph child in BtsmtlSkillNativeStateMachineContract.References(stateMachine.StateMachine))
                            AddGraphOwner(graphOwners, child, graph);
                    }
                    if (node is BtsmtlSkillStateFlowNode state && state.Body != null)
                    {
                        AddGraphOwner(graphOwners, state.Body, graph);
                        AddPlacementOwner(graphPlacementOwners, state.Body, graph);
                    }
                    if (node is BtsmtlSkillTimelineFlowNode timelineNode && timelineNode.TimelineAsset != null)
                    {
                        AddUnique(timelines, timelineNode.TimelineAsset);
                        AddTimelineOwner(timelineOwners, timelineNode.TimelineAsset, graph);
                        foreach (TreeClip tree in timelineNode.Timeline?.Tracks
                                     .SelectMany(value => value.Clips)
                                     .OfType<TreeClip>() ?? Enumerable.Empty<TreeClip>())
                            if (tree.AssetTree is BtsmtlSkillFlowGraph child)
                            {
                                AddGraphOwner(graphOwners, child, graph);
                                AddPlacementOwner(graphPlacementOwners, child, graph);
                            }
                        foreach (TimelineMarker marker in timelineNode.Timeline.Tracks.SelectMany(track => track.Markers))
                            if (marker.Graph is BtsmtlSkillFlowGraph trigger)
                            {
                                AddGraphOwner(graphOwners, trigger, graph);
                                AddPlacementOwner(graphPlacementOwners, trigger, graph);
                            }
                    }
                    if (node is BtsmtlSkillCompositeFlowNode composite)
                        foreach (BtsmtlSkillStepPort step in composite.Steps)
                            if (step?.Condition != null)
                            {
                                AddGraphOwner(graphOwners, step.Condition, graph);
                                AddPlacementOwner(graphPlacementOwners, step.Condition, graph);
                            }
                    foreach (BtsmtlSkillFlowConnection transfer in node.outConnections.OfType<BtsmtlSkillFlowConnection>())
                        if (transfer.Condition != null)
                        {
                            AddGraphOwner(graphOwners, transfer.Condition, graph);
                            AddPlacementOwner(graphPlacementOwners, transfer.Condition, graph);
                        }
                }

            foreach (BtsmtlSkillNativeStateMachine machine in machines.ToArray())
            {
                FlowGraph owner = machineOwners[machine];
                foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
                {
                    if (state.Body != null)
                    {
                        AddGraphOwner(graphOwners, state.Body, owner);
                        AddPlacementOwner(graphPlacementOwners, state.Body, owner);
                    }
                    foreach (BtsmtlSkillNativeConnection connection in state.outConnections.OfType<BtsmtlSkillNativeConnection>())
                        if (connection.Condition != null)
                        {
                            AddGraphOwner(graphOwners, connection.Condition, owner);
                            AddPlacementOwner(
                                graphPlacementOwners,
                                connection.Condition,
                                (connection.targetNode as BtsmtlSkillNativeState)?.Body ??
                                state.Body ??
                                owner);
                        }
                }
            }
            return new BtsmtlSkillAuthoringClosure(
                graphs,
                machines,
                machineOwners,
                timelines,
                timelineOwners,
                graphOwners,
                graphPlacementOwners);
        }

        static void AddUnique<T>(ICollection<T> values, T value) where T : class
        {
            if (value != null && !values.Contains(value))
                values.Add(value);
        }

        static void AddGraphOwner(
            IDictionary<FlowGraph, FlowGraph> owners,
            FlowGraph child,
            FlowGraph owner)
        {
            if (child == null || owner == null)
                return;
            if (owners.TryGetValue(child, out FlowGraph existing) && !ReferenceEquals(existing, owner))
                throw new InvalidOperationException($"Skill graph '{child.name}' has multiple formal owners.");
            owners[child] = owner;
        }

        static void AddPlacementOwner(
            IDictionary<FlowGraph, FlowGraph> owners,
            FlowGraph child,
            FlowGraph owner)
        {
            if (child == null || owner == null)
                return;
            if (owners.TryGetValue(child, out FlowGraph existing) && !ReferenceEquals(existing, owner))
                throw new InvalidOperationException($"Skill graph '{child.name}' has multiple formal placement owners.");
            owners[child] = owner;
        }

        static void AddMachineOwner(
            IDictionary<BtsmtlSkillNativeStateMachine, FlowGraph> owners,
            BtsmtlSkillNativeStateMachine machine,
            FlowGraph owner)
        {
            if (machine == null || owner == null)
                return;
            if (owners.TryGetValue(machine, out FlowGraph existing) && !ReferenceEquals(existing, owner))
                throw new InvalidOperationException($"Skill state machine '{machine.AuthoringId}' has multiple formal owners.");
            owners[machine] = owner;
        }

        static void AddTimelineOwner(
            IDictionary<TimelineAsset, FlowGraph> owners,
            TimelineAsset timeline,
            FlowGraph owner)
        {
            if (timeline == null || owner == null)
                return;
            if (owners.TryGetValue(timeline, out FlowGraph existing) && !ReferenceEquals(existing, owner))
                throw new InvalidOperationException($"Skill Timeline '{timeline.name}' has multiple formal owners.");
            owners[timeline] = owner;
        }
    }
}
#endif
