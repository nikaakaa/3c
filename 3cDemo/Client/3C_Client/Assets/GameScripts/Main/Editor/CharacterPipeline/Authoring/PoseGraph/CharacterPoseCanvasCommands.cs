using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using NodeCanvas.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseCanvasCommands
    {
        internal static IReadOnlyList<GraphAuthoringSelection> Selection(NodeCanvas.Framework.Graph graph)
        {
            IEnumerable<IGraphElement> elements = GraphEditorUtility.activeElements.Count != 0
                ? GraphEditorUtility.activeElements
                : GraphEditorUtility.activeElement == null ? Array.Empty<IGraphElement>() : new[] { GraphEditorUtility.activeElement };
            var result = new List<GraphAuthoringSelection>();
            foreach (IGraphElement element in elements)
            {
                if (element is CharacterPoseCanvasNode node && node.graph == graph)
                    result.Add(new GraphAuthoringSelection(GraphAuthoringSelectionKind.Node, new GraphAuthoringElementId(node.NodeId.Value)));
                else if (element is CharacterPoseCanvasConnection edge && edge.graph == graph)
                    result.Add(new GraphAuthoringSelection(GraphAuthoringSelectionKind.Edge, new GraphAuthoringElementId(edge.EdgeId)));
                else if (element is CharacterPoseDocumentCanvasNode projected && projected.graph == graph && !projected.IsEntry)
                    result.Add(new GraphAuthoringSelection(projected.SelectionKind, projected.ElementId));
                else if (element is CharacterPoseDocumentCanvasConnection projectedEdge && projectedEdge.graph == graph && !projectedEdge.Source.IsEntry)
                    result.Add(new GraphAuthoringSelection(((CharacterPoseDocumentCanvas)graph).StateMachineBinding != null
                        ? GraphAuthoringSelectionKind.Transition : GraphAuthoringSelectionKind.Edge, projectedEdge.ElementId));
            }
            return result;
        }

        internal static bool Handle(GraphAuthoringProjectionCanvasBinding binding, NodeCanvas.Framework.Graph graph,
            string command, Vector2 position)
        {
            if (command is not ("Copy" or "Cut" or "Paste" or "Duplicate" or "Delete" or "SoftDelete"))
                return false;
            CharacterPoseCanvasInteraction.Apply(() => Execute());
            return true;

            void Execute()
            {
                IReadOnlyList<GraphAuthoringSelection> selection = Selection(graph);
                if (command != "Copy" && binding.Mutation.ReadOnly)
                    throw new InvalidOperationException("The current document is read-only.");
                if (command is "Copy" or "Cut" or "Duplicate")
                {
                    if (binding.Clipboard == null)
                        throw new InvalidOperationException("This document has no registered clipboard operation.");
                    string payload = binding.Clipboard.Serialize(binding.Document, selection);
                    if (command == "Duplicate")
                    {
                        Vector2 origin = binding.Document.Nodes.Where(node => selection.Any(value => value.ElementId.Equals(node.NodeId)))
                            .Select(value => value.Position).Aggregate(new Vector2(float.MaxValue, float.MaxValue), Vector2.Min);
                        binding.Clipboard.Paste(binding.Document, "Duplicate Nodes", payload, origin + new Vector2(40f, 40f));
                    }
                    else
                        EditorGUIUtility.systemCopyBuffer = payload;
                }
                if (command == "Paste")
                {
                    if (binding.Clipboard == null || !binding.Clipboard.CanPaste(binding.Document, EditorGUIUtility.systemCopyBuffer))
                        throw new InvalidOperationException("The clipboard does not contain compatible document nodes.");
                    binding.Clipboard.Paste(binding.Document, "Paste Nodes", EditorGUIUtility.systemCopyBuffer, position);
                }
                if (command is "Cut" or "Delete" or "SoftDelete")
                {
                    HashSet<GraphAuthoringElementId> nodes = selection.Where(value => value.Kind == GraphAuthoringSelectionKind.Node)
                        .Select(value => value.ElementId).ToHashSet();
                    var requests = new List<GraphAuthoringMutationRequest>();
                    foreach (GraphAuthoringEdgeProjection edge in binding.Document.Edges)
                        if (!nodes.Contains(edge.SourceNodeId) && !nodes.Contains(edge.TargetNodeId) &&
                            selection.Any(value => value.Kind == GraphAuthoringSelectionKind.Edge && value.ElementId.Equals(edge.EdgeId)))
                            requests.Add(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DisconnectEdge, edge.EdgeId));
                    requests.AddRange(nodes.Select(id => new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DeleteElement, id)));
                    if (requests.Count != 0)
                        binding.Mutation.Apply(binding.Document, requests);
                    GraphEditorUtility.activeElements = null;
                    GraphEditorUtility.activeElement = null;
                }
                if (graph is CharacterPoseDocumentCanvas document)
                    document.RefreshDocument();
                GraphEditor.current?.Repaint();
            }
        }

        internal static bool CanExecute(GraphAuthoringProjectionCanvasBinding binding, NodeCanvas.Framework.Graph graph, string command)
        {
            IReadOnlyList<GraphAuthoringSelection> selection = Selection(graph);
            bool canCopy = binding.Clipboard != null && selection.Any(value => value.Kind == GraphAuthoringSelectionKind.Node);
            if (canCopy && graph is CharacterPoseCanvasGraph pose)
            {
                var selected = selection.Where(value => value.Kind == GraphAuthoringSelectionKind.Node).Select(value => value.ElementId.Value).ToHashSet();
                canCopy = pose.Nodes.Where(node => selected.Contains(node.NodeId.Value))
                    .All(node => ThirdPersonCharacter.Editor.CharacterSimulation.CharacterPoseNodeDefinitionModule.Shared.Require(node.Kind).Copyable);
            }
            if (command == "Copy") return canCopy;
            if (binding.Mutation.ReadOnly) return false;
            if (command == "Paste") return binding.Clipboard != null && binding.Clipboard.CanPaste(binding.Document, EditorGUIUtility.systemCopyBuffer);
            if (command is "Cut" or "Duplicate") return canCopy;
            return selection.Count != 0;
        }

        internal static GenericMenu Menu(Func<string, Vector2, bool> handle, Vector2 position, Func<string, bool> canExecute = null)
        {
            var menu = new GenericMenu();
            foreach (var entry in new[] { ("复制", "Copy"), ("剪切", "Cut"), ("粘贴", "Paste"), ("重复", "Duplicate"), ("删除", "Delete") })
                if (canExecute == null || canExecute(entry.Item2))
                    menu.AddItem(new GUIContent(entry.Item1), false, () => handle(entry.Item2, position));
                else menu.AddDisabledItem(new GUIContent(entry.Item1));
            return menu;
        }
    }
}
