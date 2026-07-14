using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using BTSMTL;

namespace TreeDesigner.Editor 
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        protected BaseTreeWindow m_TreeWindow;
        protected BaseTreeView m_TreeView;
        protected Texture2D m_IndentationIcon;
        protected Dictionary<string, List<(Type, string)>> m_AcceptableNodePathMap = new Dictionary<string, List<(Type, string)>>();

        public virtual void Init(BaseTreeWindow treeWindow, BaseTreeView treeView)
        {
            m_TreeWindow = treeWindow;
            m_TreeView = treeView;

            m_IndentationIcon = new Texture2D(1, 1);
            m_IndentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            m_IndentationIcon.Apply();
        }
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>();
            searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent("Create Nodes")));

            List<string> acceptableNodePaths = ListPool<string>.Get();
            var acceptableNodePathsAttributes = m_TreeWindow.Tree.GetAttributes<AcceptableNodePathsAttribute>();
            foreach (var acceptableNodePathsAttribute in acceptableNodePathsAttributes)
            {
                foreach (var acceptableNodePath in acceptableNodePathsAttribute.AcceptableNodePaths)
                {
                    if(!acceptableNodePaths.Contains(acceptableNodePath))
                        acceptableNodePaths.Add(acceptableNodePath);
                }
            }

            HashSet<string> addedGroupPaths = new HashSet<string>();
            foreach (var acceptableNodePath in acceptableNodePaths)
            {
                addedGroupPaths.Clear();
                searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent(acceptableNodePath), 1));

                var nodePathPairs = TreeDesignerUtility.GetNodePathPairs(acceptableNodePath);
                nodePathPairs = nodePathPairs
                    .Where(i => m_TreeWindow.Tree.CanCreateNodeType(i.Item1))
                    .OrderBy(i => i.Item2)
                    .ToList();

                foreach (var nodePath in nodePathPairs)
                {
                    var pathSplitStrs = nodePath.Item2.Split(new char[] { '/' });
                    string currentGroupPath = pathSplitStrs[0];
                    for (int i = 1; i < pathSplitStrs.Length; i++)
                    {
                        int level = i + 1;
                        string pathSplitStr = pathSplitStrs[i];
                        if (i == pathSplitStrs.Length - 1)
                        {
                            searchTreeEntries.Add(new SearchTreeEntry(new GUIContent(pathSplitStr, m_IndentationIcon))
                            {
                                userData = nodePath.Item1,
                                level = level
                            });
                        }
                        else
                        {
                            currentGroupPath = $"{currentGroupPath}/{pathSplitStr}";
                            if (addedGroupPaths.Add(currentGroupPath))
                            {
                                searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent(pathSplitStr), level));
                            }
                        }
                    }
                }
            }

            ListPool<string>.Release(acceptableNodePaths);

            return searchTreeEntries;
        }
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            VisualElement windowRoot = m_TreeWindow.rootVisualElement;
            Vector2 windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, context.screenMousePosition - m_TreeWindow.position.position);
            Vector2 graphMousePosition = m_TreeView.contentContainer.WorldToLocal(windowMousePosition);
            m_TreeView.CreateNode(SearchTreeEntry.userData as Type, graphMousePosition);
            return true;
        }
    }
}
