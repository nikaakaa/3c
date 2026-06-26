using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace TreeDesigner.Editor
{
    public class BaseEdgeView : Edge
    {
        protected BaseEdge m_Edge;
        public BaseEdge Edge
        {
            get => m_Edge;
            set
            {
                m_Edge = value;
                UpdateTransitionSummary();
            }
        }

        readonly Label m_TransitionSummary;

        public BasePortView StartPortView => output as BasePortView;
        public BasePortView EndPortView => input as BasePortView;

        public BaseNodeView StartNodeView => StartPortView.NodeView;
        public BaseNodeView EndNodeView => EndPortView.NodeView;

        public BaseEdgeView()
        {
            m_TransitionSummary = new Label();
            m_TransitionSummary.pickingMode = PickingMode.Ignore;
            m_TransitionSummary.style.position = Position.Absolute;
            m_TransitionSummary.style.left = 12;
            m_TransitionSummary.style.top = -10;
            m_TransitionSummary.style.paddingLeft = 4;
            m_TransitionSummary.style.paddingRight = 4;
            m_TransitionSummary.style.paddingTop = 1;
            m_TransitionSummary.style.paddingBottom = 1;
            m_TransitionSummary.style.fontSize = 10;
            m_TransitionSummary.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.82f);
            m_TransitionSummary.style.color = new Color(0.86f, 0.9f, 0.95f, 1f);
            m_TransitionSummary.style.display = DisplayStyle.None;
            Add(m_TransitionSummary);

            this.AddManipulator(new ContextualMenuManipulator(BuildTransitionContextualMenu));
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        void BuildTransitionContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (!IsStateMachineTransition())
                return;

            evt.menu.AppendSeparator();
            for (int i = 0; i <= 5; i++)
            {
                int priority = i;
                evt.menu.AppendAction($"Transition/Priority/{priority}", (s) =>
                {
                    m_Edge.Owner.ApplyModify("Set Transition Priority", () =>
                    {
                        m_Edge.TransitionPriority = priority;
                    });
                    UpdateTransitionSummary();
                });
            }

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Transition Rule/Open", (s) =>
            {
                OpenTransitionRuleGraph();
            }, (DropdownMenuAction a) => m_Edge.HasTransitionRuleGraph ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Transition Rule/Create Missing", (s) =>
            {
                CreateTransitionRuleGraph();
                OpenTransitionRuleGraph();
            }, (DropdownMenuAction a) => m_Edge.HasTransitionRuleGraph ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("Transition Rule/Clear", (s) =>
            {
                m_Edge.Owner.ApplyModify("Clear Transition Rule Graph", () =>
                {
                    m_Edge.SetTransitionRuleGraph(null);
                });
                UpdateTransitionSummary();
            }, (DropdownMenuAction a) => m_Edge.HasTransitionRuleGraph ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        bool IsStateMachineTransition()
        {
            return m_Edge != null &&
                   !(m_Edge is PropertyEdge) &&
                   m_Edge.Owner != null &&
                   m_Edge.Owner is StateMachineGraph &&
                   IsValidTransitionStart(m_Edge.StartNode) &&
                   IsValidTransitionEnd(m_Edge.EndNode);
        }

        static bool IsValidTransitionStart(BaseNode node)
        {
            return node is StateMachineEnterNode ||
                   node is StateMachineAnyStateNode ||
                   node is StateNode;
        }

        static bool IsValidTransitionEnd(BaseNode node)
        {
            return node is StateNode ||
                   node is StateMachineExitNode;
        }

        void UpdateTransitionSummary()
        {
            if (m_TransitionSummary == null)
                return;

            if (!IsStateMachineTransition())
            {
                m_TransitionSummary.style.display = DisplayStyle.None;
                return;
            }

            string condition = ResolveConditionLabel();
            m_TransitionSummary.text = string.IsNullOrEmpty(condition)
                ? $"P{m_Edge.TransitionPriority}"
                : $"P{m_Edge.TransitionPriority} | {condition}";
            m_TransitionSummary.tooltip = m_TransitionSummary.text;
            m_TransitionSummary.style.display = DisplayStyle.Flex;
        }

        string ResolveConditionLabel()
        {
            if (!m_Edge.HasTransitionRuleGraph)
                return m_Edge.StartNode is StateMachineAnyStateNode ? "Missing Rule" : string.Empty;

            return m_Edge.TransitionRuleGraph ? m_Edge.TransitionRuleGraph.name : "Missing Rule";
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount != 2 || !IsStateMachineTransition())
                return;

            OpenTransitionRuleGraph();
            evt.StopPropagation();
        }

        void OpenTransitionRuleGraph()
        {
            if (!m_Edge.HasTransitionRuleGraph)
                CreateTransitionRuleGraph();

            if (!m_Edge.HasTransitionRuleGraph)
                return;

            StartNodeView.TreeView.TreeWindow.PushReferencedTree(m_Edge, m_Edge.TransitionRuleGraph, "Transition Rule");
        }

        void CreateTransitionRuleGraph()
        {
            StateMachineGraph owner = m_Edge.Owner as StateMachineGraph;
            if (!owner)
                return;

            TransitionRuleGraph graph = ScriptableObject.CreateInstance<TransitionRuleGraph>();
            BaseNode resultNode = graph.CreateNode(typeof(TransitionRuleResultNode));
            resultNode.Position = new Vector2(360f, 0f);
            resultNode.Refresh();

            string ownerPath = AssetDatabase.GetAssetPath(owner);
            string directory = string.IsNullOrEmpty(ownerPath) ? "Assets" : Path.GetDirectoryName(ownerPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directory))
                directory = "Assets";

            string fileName = $"{SanitizeFileName(owner.name)}_{SanitizeFileName(NodeLabel(m_Edge.StartNode))}_To_{SanitizeFileName(NodeLabel(m_Edge.EndNode))}_Rule.asset";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{fileName}");
            AssetDatabase.CreateAsset(graph, assetPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();

            owner.ApplyModify("Set Transition Rule Graph", () =>
            {
                m_Edge.SetTransitionRuleGraph(graph);
            });
            UpdateTransitionSummary();
        }

        static string NodeLabel(BaseNode node)
        {
            if (node == null)
                return "Node";

            NodeNameAttribute nodeNameAttribute = node.GetAttribute<NodeNameAttribute>();
            return nodeNameAttribute != null ? nodeNameAttribute.Name : node.GetType().Name;
        }

        static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Transition";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar, '_');
            return name.Replace(' ', '_');
        }
    }
}
