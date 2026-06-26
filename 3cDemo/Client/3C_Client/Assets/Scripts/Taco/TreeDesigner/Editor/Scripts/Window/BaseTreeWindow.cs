using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Taco;

namespace TreeDesigner.Editor
{
    public class BaseTreeWindow : EditorWindow
    {
        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;
        
        protected BaseTreeView m_TreeView;
        public BaseTreeView TreeView => m_TreeView;

        protected VisualElement m_LeftPanel;
        protected VisualElement m_RightPanel;
        protected VisualElement m_NavigationToolbar;
        protected Button m_BackButton;
        protected VisualElement m_BreadcrumbContainer;
        protected Label m_TreeTitle;
        protected BaseTreeInspectorView m_TreeInspectorView;
        protected List<BaseTree> m_OpenedTrees = new List<BaseTree>();
        protected List<GraphNavigationEntry> m_NavigationStack = new List<GraphNavigationEntry>();

        protected virtual Type m_TreeViewType => typeof(BaseTreeView);
        protected virtual Type m_TreeInspectorViewType => typeof(BaseTreeInspectorView);

        public Action OnClosedCallback;
        public Action OnFocusCallback;
        public Action OnLostFocusCallback;

        protected bool m_Docking;
        public bool Docking => m_Docking;

        protected readonly struct GraphNavigationEntry
        {
            public readonly BaseTree Tree;
            public readonly string DisplayName;
            public readonly BaseTree SourceTree;
            public readonly string SourceNodeGuid;
            public readonly string ReferenceKey;

            public GraphNavigationEntry(BaseTree tree, string displayName, BaseTree sourceTree, string sourceNodeGuid, string referenceKey)
            {
                Tree = tree;
                DisplayName = displayName;
                SourceTree = sourceTree;
                SourceNodeGuid = sourceNodeGuid;
                ReferenceKey = referenceKey;
            }
        }

        public virtual void CreateGUI()
        {
            m_Tree = null;

            VisualElement root = rootVisualElement;
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeWindow");
            visualTree.CloneTree(root);

            m_LeftPanel = root.Q("left-panel");
            m_RightPanel = root.Q("right-panel");
            m_NavigationToolbar = root.Q("tree-navigation-toolbar");
            m_BackButton = root.Q<Button>("tree-navigation-back-button");
            m_BreadcrumbContainer = root.Q("tree-navigation-breadcrumb");

            m_BackButton.clicked += PopNavigationPage;

            m_TreeView = Activator.CreateInstance(m_TreeViewType) as BaseTreeView;
            m_TreeView.Init(this);
            m_TreeView.name = "tree-view";
            m_RightPanel.Add(m_TreeView);

            m_TreeTitle = new Label();
            m_TreeTitle.name = "tree-title";
            m_RightPanel.Add(m_TreeTitle);

            m_TreeInspectorView = Activator.CreateInstance(m_TreeInspectorViewType) as BaseTreeInspectorView;
            m_TreeInspectorView.name = "tree-inspector";
            m_LeftPanel.Add(m_TreeInspectorView);

            Undo.undoRedoPerformed += OnUndoRedo;
            OnClosedCallback?.Invoke();
            RefreshNavigationToolbar();
        }

        public virtual void OnFocus()
        {
            TreeWindowUtility.SelectTree(m_Tree);
            OnFocusCallback?.Invoke();

            //if (Application.isPlaying && m_Tree)
            //{
            //    foreach (var propertyField in rootVisualElement.Query<PropertyField>().ToList())
            //    {
            //        propertyField.Bind(m_Tree.GetSerializedTree());
            //    }
            //}
        }
        public virtual void OnLostFocus()
        {
            OnLostFocusCallback?.Invoke();

            //if (Application.isPlaying && m_Tree)
            //{
            //    foreach (var propertyField in rootVisualElement.Query<PropertyField>().ToList())
            //    {
            //        propertyField.Unbind();
            //    }
            //}
        }
        public virtual void OnDisable()
        {
            m_TreeView?.ClearView();
            m_TreeInspectorView?.ClearView();

            m_OpenedTrees.ForEach(i => 
            {
                Undo.ClearUndo(i);

                if (i.User == null)
                    i.DisposeTree();
            });
            m_OpenedTrees.Clear();
            m_NavigationStack.Clear();

            Undo.undoRedoPerformed -= OnUndoRedo;
            TreeWindowUtility.OnWindowClosed(this);
            
            OnClosedCallback?.Invoke();
            OnClosedCallback = null;
        }
        public virtual void Update()
        {
            if (m_Tree)
            {
                if (!Application.isPlaying)
                {
                    m_TreeTitle.text = m_Tree.name;
                    //m_TreeView.NodeViews.ForEach(i => i.Update());
                }
                m_TreeView.NodeViews.ForEach(i => i.Animation());
            }
            else if(m_Tree == null && !m_TreeView.Empty)
            {
                m_TreeView.ClearView();
                m_TreeInspectorView.ClearView();
            }

            if (EditorApplication.isCompiling)
                Close();
        }
        public void ReplaceNavigationRoot(BaseTree tree)
        {
            if (!tree)
                return;

            m_NavigationStack.Clear();
            m_NavigationStack.Add(new GraphNavigationEntry(tree, tree.name, null, string.Empty, string.Empty));
            SelectNavigationPage(m_NavigationStack[0], false);
        }
        public void PushReferencedTree(BaseNode sourceNode, NodeGraphReference reference)
        {
            if (!reference.Tree)
                return;

            string displayName = GetReferenceDisplayName(sourceNode, reference);
            string sourceNodeGuid = sourceNode != null ? sourceNode.GUID : string.Empty;
            m_NavigationStack.Add(new GraphNavigationEntry(reference.Tree, displayName, m_Tree, sourceNodeGuid, reference.Key));
            SelectNavigationPage(m_NavigationStack[m_NavigationStack.Count - 1], true);
        }
        public void PushReferencedTree(BaseEdge sourceEdge, BaseTree tree, string label)
        {
            if (!tree)
                return;

            string edgeGuid = sourceEdge != null ? sourceEdge.GUID : string.Empty;
            string displayName = string.IsNullOrEmpty(label) ? tree.name : label;
            m_NavigationStack.Add(new GraphNavigationEntry(tree, displayName, m_Tree, edgeGuid, "TransitionRuleGraph"));
            SelectNavigationPage(m_NavigationStack[m_NavigationStack.Count - 1], true);
        }
        public void PopNavigationPage()
        {
            if (m_NavigationStack.Count <= 1)
                return;

            m_NavigationStack.RemoveAt(m_NavigationStack.Count - 1);
            SelectNavigationPage(m_NavigationStack[m_NavigationStack.Count - 1], true);
        }
        public void PopNavigationTo(int index)
        {
            if (index < 0 || index >= m_NavigationStack.Count - 1)
                return;

            m_NavigationStack.RemoveRange(index + 1, m_NavigationStack.Count - index - 1);
            SelectNavigationPage(m_NavigationStack[index], true);
        }
        protected void SelectNavigationPage(GraphNavigationEntry entry, bool notifyOpened)
        {
            SelectTree(entry.Tree);
            RefreshNavigationToolbar();

            if (notifyOpened)
                TreeWindowUtility.NotifyOpened(this, entry.Tree);
        }
        protected string GetReferenceDisplayName(BaseNode sourceNode, NodeGraphReference reference)
        {
            string nodeName = GetNodeDisplayName(sourceNode);
            if (!string.IsNullOrEmpty(nodeName))
                return nodeName;

            if (!string.IsNullOrEmpty(reference.Label))
                return reference.Label;

            return reference.Tree ? reference.Tree.name : "Graph";
        }
        protected string GetNodeDisplayName(BaseNode sourceNode)
        {
            if (sourceNode == null)
                return string.Empty;

            NodeNameAttribute nodeNameAttribute = sourceNode.GetAttribute<NodeNameAttribute>();
            if (nodeNameAttribute != null)
            {
                MethodInfo methodInfo = sourceNode.GetMethod(nodeNameAttribute.Name);
                if (methodInfo != null)
                    return methodInfo.Invoke(sourceNode, null) as string;

                return nodeNameAttribute.Name;
            }

            return sourceNode.GetType().Name;
        }
        protected void RefreshNavigationToolbar()
        {
            if (m_BackButton == null || m_BreadcrumbContainer == null)
                return;

            m_BackButton.SetEnabled(m_NavigationStack.Count > 1);
            m_BreadcrumbContainer.Clear();

            if (m_NavigationStack.Count == 0)
            {
                if (m_Tree)
                    AddCurrentBreadcrumb(m_Tree.name);
                return;
            }

            for (int i = 0; i < m_NavigationStack.Count; i++)
            {
                if (i > 0)
                    AddBreadcrumbSeparator();

                GraphNavigationEntry entry = m_NavigationStack[i];
                string displayName = string.IsNullOrEmpty(entry.DisplayName) && entry.Tree ? entry.Tree.name : entry.DisplayName;
                if (i == m_NavigationStack.Count - 1)
                    AddCurrentBreadcrumb(displayName);
                else
                    AddBreadcrumbButton(displayName, i);
            }
        }
        void AddBreadcrumbButton(string text, int index)
        {
            Button button = new Button(() => PopNavigationTo(index));
            button.text = text;
            button.AddToClassList("tree-navigation-segment");
            m_BreadcrumbContainer.Add(button);
        }
        void AddCurrentBreadcrumb(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("tree-navigation-current-segment");
            m_BreadcrumbContainer.Add(label);
        }
        void AddBreadcrumbSeparator()
        {
            Label separator = new Label("/");
            separator.AddToClassList("tree-navigation-separator");
            m_BreadcrumbContainer.Add(separator);
        }
        public void SelectTree(BaseTree tree)
        {
            if (!tree)
                return;

            if (tree != m_Tree)
            {
                //m_Tree?.Dispose();
                m_Tree = tree;
                m_TreeTitle.text = m_Tree.name;

                if (!m_OpenedTrees.Contains(tree))
                    m_OpenedTrees.Add(tree);
                if (m_Tree.Refresh())
                    EditorUtility.SetDirty(m_Tree);
                if (m_Tree.CheckInit())
                    EditorUtility.SetDirty(m_Tree);
                m_TreeView.PopulateView(m_Tree);
                m_TreeInspectorView.PopulateView(m_Tree);
                TreeWindowUtility.SelectTree(m_Tree);
            }

            if (m_TreeTitle != null)
                m_TreeTitle.text = m_Tree.name;

            RefreshNavigationToolbar();
        }
        void OnUndoRedo()
        {
            if (m_Tree)
            {
                //m_Tree.DisposeTree();

                m_Tree.GetNewSerializedTree();
                if (m_Tree.Refresh())
                    EditorUtility.SetDirty(m_Tree);
                if (m_Tree.CheckInit())
                    EditorUtility.SetDirty(m_Tree);
                m_TreeView.PopulateView(m_Tree);
                m_TreeInspectorView.PopulateView(m_Tree);
            }
        }

        void OnBeforeRemovedAsTab()
        {
            m_Docking = true;
        }
        void OnAddedAsTab()
        {
            m_Docking = false;
        }
    }
}
