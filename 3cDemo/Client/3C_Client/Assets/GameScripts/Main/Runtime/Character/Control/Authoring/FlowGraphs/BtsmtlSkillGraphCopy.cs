#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillGraphCopy
    {
        sealed class AssetSnapshot
        {
            internal ScriptableObject Source;
            internal Type Type;
            internal string Name;
            internal string GraphId;
            internal string Metadata;
            internal List<UnityEngine.Object> References;
            internal List<Node> Nodes;
            internal TimelineData Timeline;
        }

        sealed class Clipboard
        {
            internal List<Node> Nodes;
            internal List<AssetSnapshot> Assets;
        }

        static Clipboard s_Clipboard;

        public static void CaptureClipboard(List<Node> nodes)
        {
            var plan = new CopyPlan(null, nodes);
            s_Clipboard = new Clipboard { Nodes = Graph.CloneNodes(nodes), Assets = plan.Snapshots };
            CopyBuffer.SetCache<Node[]>(s_Clipboard.Nodes.ToArray());
        }

        public static List<Node> Copy(FlowGraph target, List<Node> nodes, Vector2 position)
        {
            if (nodes == null || nodes.Distinct().Count() != nodes.Count || nodes.Any(node => node == null || !target.CanAuthorNodeType(node.GetType()) ||
                node is IBtsmtlSkillSystemNode || node is MacroInputNode || node is MacroOutputNode))
                throw new InvalidOperationException("复制集合含有系统入口或不属于目标页面的节点。");
            var plan = s_Clipboard != null && nodes.SequenceEqual(s_Clipboard.Nodes)
                ? new CopyPlan(target, nodes, s_Clipboard.Assets)
                : new CopyPlan(target, nodes);
            return BtsmtlSkillFlowEditorMutation.Execute(target, "复制技能节点及私有内容", () => plan.Apply(position));
        }

        public static List<Node> CopyNative(
            BtsmtlSkillNativeStateMachine target,
            List<Node> nodes,
            Vector2 position)
        {
            if (nodes == null || nodes.Distinct().Count() != nodes.Count || nodes.Any(node =>
                node == null || !target.CanAuthorNodeType(node.GetType()) ||
                node is BtsmtlSkillNativeEntryState || node is BtsmtlSkillNativeAnyState ||
                node is BtsmtlSkillNativeExitState))
                throw new InvalidOperationException("复制集合含有FSM系统锚点或未登记状态。");
            var plan = s_Clipboard != null && nodes.SequenceEqual(s_Clipboard.Nodes)
                ? new CopyPlan(target, nodes, s_Clipboard.Assets)
                : new CopyPlan(target, nodes);
            return BtsmtlSkillFlowEditorMutation.Execute(target, "复制技能FSM状态及私有内容", () => plan.Apply(position));
        }

        sealed class CopyPlan
        {
            readonly Graph m_Target;
            readonly List<Node> m_Nodes;
            readonly string m_TargetPath;
            readonly List<AssetSnapshot> m_Sources = new();
            readonly HashSet<ScriptableObject> m_Seen = new();
            readonly Dictionary<ScriptableObject, ScriptableObject> m_Copies = new();
            readonly Dictionary<string, string> m_GraphIds = new(StringComparer.Ordinal);
            internal List<AssetSnapshot> Snapshots => m_Sources;

            internal CopyPlan(Graph target, List<Node> nodes)
            {
                m_Target = target;
                m_Nodes = nodes.ToList();
                m_TargetPath = target ? AssetDatabase.GetAssetPath(target) : string.Empty;
                foreach (Node node in nodes)
                    foreach (ScriptableObject reference in References(node))
                        Collect(reference);
                if (target && m_Sources.Count != 0 && string.IsNullOrEmpty(m_TargetPath))
                    throw new InvalidOperationException("复制私有内容前必须保存目标技能根。");
            }

            internal CopyPlan(Graph target, List<Node> nodes, List<AssetSnapshot> snapshots)
            {
                m_Target = target;
                m_Nodes = nodes.ToList();
                m_TargetPath = AssetDatabase.GetAssetPath(target);
                m_Sources.AddRange(snapshots);
                if (m_Sources.Count != 0 && string.IsNullOrEmpty(m_TargetPath))
                    throw new InvalidOperationException("粘贴私有内容前必须保存目标技能根。");
            }

            void Collect(ScriptableObject source)
            {
                if (!source || !AssetDatabase.IsSubAsset(source) || !m_Seen.Add(source))
                    return;
                var snapshot = new AssetSnapshot { Source = source, Type = source.GetType(), Name = source.name };
                m_Sources.Add(snapshot);
                if (source is FlowGraph graph && graph is IBtsmtlSkillFlowGraph)
                {
                    BtsmtlSkillGraphClosure.Validate(graph, false);
                    snapshot.GraphId = ((IBtsmtlSkillFlowGraph)graph).AuthoringId;
                    snapshot.Nodes = Graph.CloneNodes(graph.allNodes.ToList());
                    GraphSource metadata = graph.GetGraphSourceMetaDataCopy();
                    metadata.localBlackboard = graph.GetGraphSource().localBlackboard;
                    metadata.canvasGroups = graph.canvasGroups.ToList();
                    snapshot.References = new List<UnityEngine.Object>();
                    snapshot.Metadata = JSONSerializer.Serialize(typeof(GraphSource), metadata.Pack(graph), snapshot.References);
                    foreach (Node node in graph.allNodes)
                        foreach (ScriptableObject reference in References(node))
                            Collect(reference);
                }
                else if (source is BtsmtlSkillNativeStateMachine machine)
                {
                    BtsmtlSkillNativeStateMachineContract.Validate(machine, false);
                    snapshot.GraphId = machine.AuthoringId;
                    snapshot.Nodes = Graph.CloneNodes(machine.allNodes.ToList());
                    GraphSource metadata = machine.GetGraphSourceMetaDataCopy();
                    metadata.localBlackboard = machine.GetGraphSource().localBlackboard;
                    metadata.canvasGroups = machine.canvasGroups.ToList();
                    snapshot.References = new List<UnityEngine.Object>();
                    snapshot.Metadata = JSONSerializer.Serialize(typeof(GraphSource), metadata.Pack(machine), snapshot.References);
                    foreach (Node node in machine.allNodes)
                        foreach (ScriptableObject reference in References(node))
                            Collect(reference);
                }
                else if (source is TimelineAsset timeline)
                {
                    snapshot.Timeline = timeline.Data.Clone();
                    foreach (TreeClip clip in timeline.Data.Tracks.SelectMany(track => track.Clips).OfType<TreeClip>())
                        Collect(clip.AssetTree);
                    foreach (TimelineMarker marker in timeline.Data.Tracks.SelectMany(track => track.Markers))
                        Collect(marker.Graph);
                }
                else
                    throw new InvalidOperationException("复制遇到没有技能所有权合同的私有内容。");
            }

            internal List<Node> Apply(Vector2 position)
            {
                foreach (AssetSnapshot source in m_Sources)
                {
                    var copy = ScriptableObject.CreateInstance(source.Type);
                    copy.name = source.Name;
                    AssetDatabase.AddObjectToAsset(copy, m_TargetPath);
                    Undo.RegisterCreatedObjectUndo(copy, "复制技能私有内容");
                    m_Copies.Add(source.Source, copy);
                    if (source.Metadata != null)
                        CopyMetadata(source, (Graph)copy);
                }
                foreach (AssetSnapshot source in m_Sources.Where(value => value.Timeline != null))
                {
                    TimelineData data = source.Timeline.CloneForAuthoring();
                    foreach (TreeClip clip in data.Tracks.SelectMany(track => track.Clips).OfType<TreeClip>())
                        if (!ReferenceEquals(clip.AssetTree, null))
                            clip.SetAssetTree(Resolve(clip.AssetTree));
                    foreach (TimelineMarker marker in data.Tracks.SelectMany(track => track.Markers))
                        marker.Configure(marker.Frame, Resolve(marker.Graph));
                    ((TimelineAsset)m_Copies[source.Source]).SetData(data);
                    EditorUtility.SetDirty(m_Copies[source.Source]);
                }
                foreach (AssetSnapshot source in m_Sources.Where(value => value.Metadata != null))
                {
                    var copy = (Graph)m_Copies[source.Source];
                    void CopyGraph()
                    {
                        List<Node> copied = copy is FlowGraph flow
                            ? ((IBtsmtlSkillFlowGraph)flow).DuplicateStructure(source.Nodes, default)
                            : ((BtsmtlSkillNativeStateMachine)copy).DuplicateNodesDirect(source.Nodes, default);
                        foreach (Node node in copied)
                            Remap(node);
                    }
                    if (copy is BtsmtlSkillNativeStateMachine native)
                        BtsmtlSkillFlowEditorMutation.Apply(native, "复制私有FSM拓扑", CopyGraph, false);
                    else
                        BtsmtlSkillFlowEditorMutation.Apply((FlowGraph)copy, "复制私有图拓扑", CopyGraph, false);
                }
                List<Node> result = m_Target is FlowGraph targetFlow
                    ? ((IBtsmtlSkillFlowGraph)targetFlow).DuplicateStructure(m_Nodes, position)
                    : ((BtsmtlSkillNativeStateMachine)m_Target).DuplicateNodesDirect(m_Nodes, position);
                foreach (Node node in result)
                    Remap(node);
                return result;
            }

            void CopyMetadata(AssetSnapshot source, Graph target)
            {
                if (!target.Deserialize(source.Metadata, source.References, false))
                    throw new InvalidOperationException("复制技能图元数据失败。");
                string identity = Guid.NewGuid().ToString("N");
                m_GraphIds.Add(source.GraphId, identity);
                if (target is BtsmtlSkillFlowGraph graph)
                    graph.ConfigureIdentity(identity, graph.Role);
                else if (target is BtsmtlSkillNativeStateMachine machine)
                    machine.ConfigureIdentity(identity);
                else
                    ((BtsmtlSkillMacroGraph)target).ConfigureIdentity(identity);
            }

            T Resolve<T>(T source) where T : ScriptableObject =>
                !ReferenceEquals(source, null) && m_Copies.TryGetValue(source, out ScriptableObject copy) ? (T)copy : source;

            string OwnerId(string identity) => m_GraphIds.TryGetValue(identity, out string copy) ? copy : identity;

            BtsmtlSkillBlackboardReference Remap(BtsmtlSkillBlackboardReference reference) =>
                new(reference.DeclarationId, OwnerId(reference.OwnerId));

            void Remap(Node node)
            {
                switch (node)
                {
                    case MacroNodeWrapper call:
                        call.macro = Resolve(call.macro);
                        break;
                    case BtsmtlSkillStateMachineFlowNode machine:
                        machine.SetStateMachine(Resolve(machine.StateMachine));
                        if (machine.StateMachine != null && node.graph is IBtsmtlSkillFlowGraph owner)
                            machine.StateMachine.ConfigureOwner(owner.AuthoringId, machine.UID);
                        break;
                    case BtsmtlSkillNativeState native:
                        native.SetBody(Resolve(native.Body));
                        break;
                    case BtsmtlSkillStateFlowNode state:
                        state.SetBody(Resolve(state.Body));
                        break;
                    case BtsmtlSkillTimelineFlowNode timeline when !ReferenceEquals(timeline.TimelineAsset, null):
                        timeline.Configure(Resolve(timeline.TimelineAsset), timeline.Ownership, timeline.ActionContext, timeline.PlaybackMode);
                        break;
                    case BtsmtlSkillBlackboardBooleanFlowNode value when value.Variable.IsValid:
                        value.SetVariable(Remap(value.Variable));
                        break;
                    case BtsmtlSkillBlackboardScalarFlowNode value when value.Variable.IsValid:
                        value.SetVariable(Remap(value.Variable));
                        break;
                    case BtsmtlSkillBlackboardAccessFlowNode value when value.Variable.IsValid:
                        value.Configure(Remap(value.Variable), value.DeclaredType, value.FactContext);
                        break;
                    case BtsmtlSkillCanActivateActionFlowNode action:
                        action.Configure(action.AdmissionProfile, action.TargetSnapshotDeclarationId, OwnerId(action.TargetSnapshotOwnerId));
                        break;
                }
                if (node is BtsmtlSkillCompositeFlowNode composite)
                    foreach (BtsmtlSkillStepPort step in composite.Steps)
                        step.Configure(step.Name, Resolve(step.Condition), step.Priority, step.AbortPolicy);
                foreach (BtsmtlSkillFlowConnection transfer in node.outConnections.OfType<BtsmtlSkillFlowConnection>())
                    transfer.Configure(
                        Resolve(transfer.Condition),
                        transfer.Priority,
                        transfer.AbortPolicy,
                        transfer.Order);
                foreach (BtsmtlSkillNativeConnection transfer in node.outConnections.OfType<BtsmtlSkillNativeConnection>())
                    transfer.Configure(
                        Resolve(transfer.Condition),
                        transfer.Priority,
                        transfer.AbortPolicy,
                        transfer.Order);
            }
        }

        static IEnumerable<ScriptableObject> References(Node node)
        {
            if (node is MacroNodeWrapper macro) yield return macro.macro;
            if (node is BtsmtlSkillStateMachineFlowNode machine) yield return machine.StateMachine;
            if (node is BtsmtlSkillNativeState native) yield return native.Body;
            foreach (BtsmtlSkillNativeConnection transfer in node.outConnections.OfType<BtsmtlSkillNativeConnection>())
                yield return transfer.Condition;
            if (node is BtsmtlSkillStateFlowNode state) yield return state.Body;
            if (node is BtsmtlSkillTimelineFlowNode timeline) yield return timeline.TimelineAsset;
            if (node is BtsmtlSkillCompositeFlowNode composite)
                foreach (BtsmtlSkillStepPort step in composite.Steps)
                    yield return step.Condition;
            foreach (BtsmtlSkillFlowConnection transfer in node.outConnections.OfType<BtsmtlSkillFlowConnection>())
                yield return transfer.Condition;
        }
    }
}
#endif
