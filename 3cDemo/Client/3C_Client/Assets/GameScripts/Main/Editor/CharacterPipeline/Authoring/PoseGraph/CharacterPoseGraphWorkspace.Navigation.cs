using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed partial class CharacterPoseGraphWorkspace
    {
        [Serializable]
        sealed class SavedNavigation
        {
            public string rootRole;
            public string rootId;
            public List<SavedNavigationStep> steps = new List<SavedNavigationStep>();
            public List<SavedNavigationStep> selection = new List<SavedNavigationStep>();
        }

        [Serializable]
        sealed class SavedNavigationStep
        {
            public string ownerId;
            public bool connection;
        }

        void NavigateFromCatalog(string role, string id)
        {
            CharacterPoseCanvasInteraction.Apply(() =>
            {
                var routes = new List<(string Label, SavedNavigation Navigation)>();
                Visit(m_Asset.Graph, new List<SavedNavigationStep>(), "Root Pose Graph", new HashSet<PoseGraphId>());
                if (routes.Count == 0)
                {
                    if (role == "graph") OpenGraph(new PoseGraphId(id));
                    else
                    {
                        var owner = CharacterPoseGraphAssetMutationOwner.ResolveStateMachineOwner(m_Asset, new PoseStateMachineId(id));
                        OpenGraph(owner.Item1);
                        m_Asset.RequireGraph(owner.Item1).RequireNode(owner.Item2).TryOpenEditorChild();
                    }
                    return;
                }
                if (routes.Count == 1)
                {
                    RestoreNavigation(JsonUtility.ToJson(routes[0].Navigation));
                    return;
                }
                var menu = new GenericMenu();
                foreach (var route in routes)
                {
                    string navigation = JsonUtility.ToJson(route.Navigation);
                    menu.AddItem(new GUIContent(route.Label), false, () => RestoreNavigation(navigation));
                }
                menu.ShowAsContext();

                void AddRoute(List<SavedNavigationStep> steps, string label) => routes.Add((label, new SavedNavigation
                {
                    rootRole = "graph",
                    rootId = m_Asset.Graph.GraphId.Value,
                    steps = new List<SavedNavigationStep>(steps)
                }));

                void Visit(CharacterPoseCanvasGraph graph, List<SavedNavigationStep> steps, string label, HashSet<PoseGraphId> ancestors)
                {
                    if (!ancestors.Add(graph.GraphId)) return;
                    if (role == "graph" && graph.GraphId.Value == id) AddRoute(steps, label);
                    foreach (CharacterPoseCanvasNode node in graph.Nodes)
                    {
                        var childSteps = new List<SavedNavigationStep>(steps)
                        {
                            new SavedNavigationStep { ownerId = node.NodeId.Value }
                        };
                        string childLabel = label + " › " + node.DisplayName;
                        if (node.Payload is CharacterPoseStateMachineNodePayload machine && machine.StateMachine != null)
                        {
                            if (role == "state" && machine.StateMachine.StateMachineId.Value == id) AddRoute(childSteps, childLabel);
                            foreach (CharacterPoseStateDefinition state in machine.StateMachine.States)
                            {
                                var stateSteps = new List<SavedNavigationStep>(childSteps)
                                {
                                    new SavedNavigationStep { ownerId = state.StateId.Value }
                                };
                                Visit(m_Asset.RequireGraph(state.PoseGraphId), stateSteps, childLabel + " › " + state.DisplayName, ancestors);
                            }
                        }
                        else if (node.Payload is CharacterPoseSubgraphPayload subgraph && subgraph.Subgraph != null)
                            Visit(m_Asset.RequireGraph(subgraph.Subgraph.PoseGraphId), childSteps, childLabel, ancestors);
                    }
                    ancestors.Remove(graph.GraphId);
                }
            });
        }

        string CaptureNavigation()
        {
            NodeCanvas.Framework.Graph root = GraphEditor.rootGraph;
            var saved = new SavedNavigation();
            if (root is CharacterPoseCanvasGraph pose)
            {
                saved.rootRole = "graph";
                saved.rootId = pose.GraphId.Value;
            }
            else if (root is CharacterPoseDocumentCanvas view && view.StateMachineBinding?.Document is CharacterPoseStateMachineDocument machine)
            {
                saved.rootRole = "state";
                saved.rootId = machine.DocumentId;
            }
            else if (root is CharacterPoseDocumentCanvas ruleView && ruleView.ProjectionBinding?.Document is CharacterPoseTransitionRuleDocument rule)
            {
                saved.rootRole = "rule";
                saved.rootId = rule.DocumentId;
            }
            else return string.Empty;
            for (NodeCanvas.Framework.Graph graph = root; graph.GetCurrentChildGraph() != null; graph = graph.GetCurrentChildGraph())
            {
                IGraphElement owner = graph.GetCurrentChildGraphSource();
                saved.steps.Add(new SavedNavigationStep { ownerId = owner.UID, connection = owner is Connection });
            }
            IEnumerable<IGraphElement> selected = GraphEditorUtility.activeElements.Count != 0
                ? GraphEditorUtility.activeElements
                : GraphEditorUtility.activeElement != null ? new[] { GraphEditorUtility.activeElement } : Array.Empty<IGraphElement>();
            foreach (IGraphElement element in selected)
                if (element.graph == GraphEditor.currentGraph)
                    saved.selection.Add(new SavedNavigationStep { ownerId = element.UID, connection = element is Connection });
            return JsonUtility.ToJson(saved);
        }

        void RestoreNavigation(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            SavedNavigation saved = JsonUtility.FromJson<SavedNavigation>(json);
            try
            {
                switch (saved.rootRole)
                {
                    case "graph":
                        OpenGraph(new PoseGraphId(saved.rootId));
                        break;
                    case "state":
                        OpenStateMachine(m_Asset.EnumerateStateMachines().Single(value => value.StateMachineId.Value == saved.rootId));
                        break;
                    case "rule":
                        var owner = CharacterPoseAuthoringCatalog.FindTransitionRuleOwner(m_Asset, saved.rootId);
                        BindTransitionRule(owner.Machine, owner.Transition.TransitionId);
                        break;
                    default:
                        throw new InvalidOperationException("保存的图页面类型已失效。");
                }
                foreach (SavedNavigationStep step in saved.steps)
                {
                    NodeCanvas.Framework.Graph graph = GraphEditor.currentGraph;
                    IGraphElement owner = step.connection
                        ? graph.allNodes.SelectMany(node => node.outConnections).SingleOrDefault(edge => edge.UID == step.ownerId)
                        : graph.allNodes.SingleOrDefault(node => node.UID == step.ownerId);
                    bool opened = owner is Node node ? node.TryOpenEditorChild() : owner is Connection edge && edge.TryOpenEditorChild();
                    if (!opened) throw new InvalidOperationException("保存的子图入口已改变，导航停留在最后一个有效页面。");
                }
                var selected = new List<IGraphElement>();
                NodeCanvas.Framework.Graph current = GraphEditor.currentGraph;
                foreach (SavedNavigationStep step in saved.selection ?? new List<SavedNavigationStep>())
                {
                    IGraphElement element = step.connection
                        ? current.allNodes.SelectMany(node => node.outConnections).SingleOrDefault(edge => edge.UID == step.ownerId)
                        : current.allNodes.SingleOrDefault(node => node.UID == step.ownerId);
                    if (element != null) selected.Add(element);
                }
                GraphEditorUtility.activeElements = selected;
            }
            catch (InvalidOperationException exception)
            {
                ShowNotification(new GUIContent(exception.Message));
            }
        }
    }
}
