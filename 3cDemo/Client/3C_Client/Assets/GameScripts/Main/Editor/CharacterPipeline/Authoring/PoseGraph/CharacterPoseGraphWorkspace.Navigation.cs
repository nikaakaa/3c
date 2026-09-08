using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
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
                        var owner = FindTransitionRuleOwner(saved.rootId);
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
