using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

namespace TreeDesigner.Editor
{
    public class StackNodeView : UnityEditor.Experimental.GraphView.StackNode, IGroupable
    {
        StackNode m_StackNode;
        public StackNode StackNode => m_StackNode;

        BaseTreeView m_TreeView;
        public BaseTreeView TreeView => m_TreeView;

        List<BaseNodeView> m_NodeViews = new List<BaseNodeView>();

        NodeGroupView m_NodeGroupView;
        public NodeGroupView NodeGroupView { get => m_NodeGroupView; set => m_NodeGroupView = value; }

        public StackNodeView(StackNode stackNode, BaseTreeView treeView)
        {
            m_StackNode = stackNode;
            m_TreeView = treeView;
            viewDataKey = m_StackNode.GUID;

            SetPosition(new Rect(stackNode.Position, Vector2.zero));

            styleSheets.Add(Resources.Load<StyleSheet>("StyleSheet/StackNode"));

            capabilities = Capabilities.Selectable |
                           Capabilities.Movable |
                           Capabilities.Deletable |
                           Capabilities.Ascendable |
                           Capabilities.Copiable |
                           Capabilities.Snappable |
                           Capabilities.Groupable;

            List<string> toDelete = new List<string>();
            for (int i = 0, idx = 0; i < m_StackNode.NodeGUIDs.Count; i++)
            {
                BaseNodeView nodeView = m_TreeView.FindNodeView(m_StackNode.NodeGUIDs[i]);
                if (nodeView == null)
                {
                    toDelete.Add(m_StackNode.NodeGUIDs[i]);
                    continue;
                }
                InsertElement(idx++, nodeView);

                nodeView.RemoveFromClassList("stack-child-element");
                nodeView.StackNodeView = this;
                nodeView.RegisterCallback<DetachFromPanelEvent>(OnChildDetachedFromPanel);
                m_NodeViews.Add(nodeView);
            }

            for (int i = toDelete.Count - 1; i >= 0 ; i--)
            {
                Debug.Log($"m_VisualTreeAssetGUID:{toDelete[i]}");
                m_TreeView.Tree.ApplyModify("Delete InvalidNode", () =>
                {
                    m_StackNode.NodeGUIDs.Remove(toDelete[i]);
                });
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
        }
        public override void OnStartDragging(GraphElement ge)
        {
            BaseNodeView nodeView = ge as BaseNodeView;
            if (nodeView != null)
            {
                ge.RemoveFromHierarchy();
                m_TreeView.AddElement(ge);
                m_TreeView.AddToSelection(ge);
            }
        }
        public override bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            selection = selection.ToList().OrderBy(i => 
            {
                if (i is BaseNodeView nodeView)
                    return nodeView.Node.Position.y;
                else
                    return 0;
            });
            foreach (var elem in selection)
            {
                if (elem is BaseNodeView nodeView)
                {
                    nodeView.RegisterCallback<DetachFromPanelEvent>(OnChildDetachedFromPanel);
                }
            }
            return base.DragEnter(evt, selection, enteredTarget, dragSource);
        }
        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            return base.AcceptsElement(element, ref proposedIndex, maxIndex) && (element.capabilities | Capabilities.Groupable) == element.capabilities;
        }


        public void OnMoved(Vector2 position)
        {
            if (m_StackNode.Position != position)
            {
                m_TreeView.Tree.ApplyModify("Move Stack", () =>
                {
                    m_StackNode.Position = position;
                    UpdateChildPosition();
                    m_NodeGroupView?.OnMoved();
                });
            }
        }
        void OnChildDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (m_TreeView.TreeWindow.Docking) return;

            BaseNodeView nodeView = evt.target as BaseNodeView;
            if (base.panel != null)
            {
                if (!m_NodeViews.Contains(nodeView))
                {
                    nodeView.StackNodeView = this;
                    nodeView.capabilities = nodeView.capabilities | Capabilities.Snappable;
                    m_NodeViews.Add(nodeView);
                }
                else
                {
                    nodeView.StackNodeView = null;
                    nodeView.RefreshCapabilities();
                    nodeView.UnregisterCallback<DetachFromPanelEvent>(OnChildDetachedFromPanel);
                    m_NodeViews.Remove(nodeView);
                }
            }

            EditorApplication.delayCall += () =>
            {
                if (base.panel != null)
                {
                    nodeView.RemoveFromClassList("stack-child-element");
                    if (!m_StackNode.NodeGUIDs.Contains(nodeView.Node.GUID))
                    {
                        int index = 0;
                        foreach (var child in contentContainer.Children())
                        {
                            if (child is BaseNodeView childNodeView && childNodeView.worldBound.position.y < nodeView.worldBound.position.y)
                                index++;
                        }
                        index = Mathf.Clamp(index, 0, m_StackNode.NodeGUIDs.Count);
                        m_TreeView.Tree.ApplyModify("Add To Stack", () =>
                        {
                            m_StackNode.NodeGUIDs.Insert(index, nodeView.Node.GUID);
                        });
                    }
                    else
                    {
                        m_TreeView.Tree.ApplyModify("RemoveTagWithChildren From Stack", () =>
                        {
                            m_StackNode.NodeGUIDs.Remove(nodeView.Node.GUID);
                        });
                    }
                    UpdateChildPosition();
                }
            };
        }
        void UpdateChildPosition()
        {
            foreach (var nodeView in m_NodeViews)
            {
                nodeView.OnMoved(m_StackNode.Position + new Vector2(12, nodeView.layout.position.y));
            }
        }
    }
}