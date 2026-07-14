using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public class NodeGroupView : Group
    {
        NodeGroup m_NodeGroup;
        public NodeGroup NodeGroup => m_NodeGroup;

        BaseTreeView m_TreeView;
        public BaseTreeView TreeView => m_TreeView;

        List<BaseNodeView> m_NodeViews = new List<BaseNodeView>();
        List<StackNodeView> m_StackNodeViews = new List<StackNodeView>();

        public NodeGroupView(NodeGroup nodeGroup, BaseTreeView treeView)
        {
            m_NodeGroup = nodeGroup;
            m_TreeView = treeView;

            title = nodeGroup.Title;
            SetPosition(new Rect(nodeGroup.Position,Vector2.zero));

            headerContainer.Q<TextField>().RegisterCallback<ChangeEvent<string>>(TitleChangedCallback);

            var titleLabel = headerContainer.Q<Label>();
            titleLabel.style.paddingLeft = titleLabel.style.paddingRight = 0;
            titleLabel.style.fontSize = 20;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            var titleField = headerContainer.Q("titleField");
            titleField.style.marginTop = titleField.style.marginBottom = titleField.style.marginLeft = titleField.style.marginRight = 0;
            titleField.style.top = titleField.style.bottom = titleField.style.left = titleField.style.right = 0;

            var textInput = headerContainer.Q("unity-text-input");
            textInput.style.paddingTop = textInput.style.paddingBottom = textInput.style.paddingLeft = textInput.style.paddingRight = 0;
            textInput.style.fontSize = 18;
            textInput.style.unityTextAlign = TextAnchor.MiddleCenter;

            styleSheets.Add(Resources.Load<StyleSheet>("StyleSheet/NodeGroup"));

            capabilities = Capabilities.Selectable |
                           Capabilities.Movable |
                           Capabilities.Deletable |
                           Capabilities.Ascendable |
                           Capabilities.Copiable |
                           Capabilities.Snappable;

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            m_NodeGroup.NodeGUIDs.ForEach(i =>
            {
                BaseNodeView nodeView = m_TreeView.FindNodeView(i);
                AddElement(nodeView);
            });
            m_NodeGroup.StackGUIDs.ForEach(i =>
            {
                AddElement(m_TreeView.GetElementByGuid(i));
            });
        }

        public void OnMoved(Vector2 position)
        {
            if (m_NodeGroup.Position != position)
            {
                m_TreeView.Tree.ApplyModify("Move Group", () =>
                {
                    m_NodeGroup.Position = position;
                    m_NodeViews.ForEach(i => i.OnMoved(i.GetPosition().position));
                    m_StackNodeViews.ForEach(i => i.OnMoved(i.GetPosition().position));
                });
            }
        }
        public void OnMoved()
        {
            if (m_NodeGroup.Position != GetPosition().position)
            {
                m_TreeView.Tree.ApplyModify("Move Group", () =>
                {
                    m_NodeGroup.Position = GetPosition().position;
                });
            }
        }
        public void RemoveFromGroup(IGroupable groupable)
        {
            m_TreeView.Tree.ApplyModify("RemoveTagWithChildren From Group", () =>
            {
                RemoveElement(groupable as GraphElement);
            });
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            foreach (var element in elements)
            {
                if (element is BaseNodeView nodeView)
                {
                    if (!m_NodeGroup.NodeGUIDs.Contains(nodeView.Node.GUID))
                    {
                        m_TreeView.Tree.ApplyModify("AddToGroup", () => m_NodeGroup.NodeGUIDs.Add(nodeView.Node.GUID));
                        OnMoved();
                    }
                    nodeView.NodeGroupView = this;
                    m_NodeViews.Add(nodeView);
                }
                if (element is StackNodeView stackNodeView)
                {
                    if (!m_NodeGroup.StackGUIDs.Contains(stackNodeView.StackNode.GUID))
                    {
                        m_TreeView.Tree.ApplyModify("AddToGroup", () => m_NodeGroup.StackGUIDs.Add(stackNodeView.StackNode.GUID));
                        OnMoved();
                    }
                    stackNodeView.NodeGroupView = this;
                    m_StackNodeViews.Add(stackNodeView);
                }
            }
            base.OnElementsAdded(elements);
        }
        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            if (parent != null)
            {
                foreach (var element in elements)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        if (m_NodeGroup.NodeGUIDs.Contains(nodeView.Node.GUID))
                        {
                            m_TreeView.Tree.ApplyModify("RemoveFromGroup", () => m_NodeGroup.NodeGUIDs.Remove(nodeView.Node.GUID));
                            OnMoved();
                        }
                        nodeView.NodeGroupView = null;
                        m_NodeViews.Remove(nodeView);
                    }
                    if (element is StackNodeView stackNodeView)
                    {
                        if (m_NodeGroup.StackGUIDs.Contains(stackNodeView.StackNode.GUID))
                        {
                            m_TreeView.Tree.ApplyModify("RemoveFromGroup", () => m_NodeGroup.StackGUIDs.Remove(stackNodeView.StackNode.GUID));
                            OnMoved();
                        }
                        stackNodeView.NodeGroupView = null;
                        m_StackNodeViews.Remove(stackNodeView);
                    }
                }
            }

            base.OnElementsRemoved(elements);
        }
        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }
        void TitleChangedCallback(ChangeEvent<string> e)
        {
            m_TreeView.Tree.ApplyModify("Change Group Title", () =>
            {
                m_NodeGroup.Title = e.newValue;
            });
        }
    }
}