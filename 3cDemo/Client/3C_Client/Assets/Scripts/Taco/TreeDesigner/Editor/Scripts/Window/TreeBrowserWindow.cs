using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Taco.Editor;

namespace TreeDesigner.Editor
{
    public class TreeBrowserWindow : EditorWindow
    {
        Label m_TypeFilter;
        ToolbarSearchField m_ToolbarSearchField;
        VisualElement m_LockedContainer;
        VisualElement m_CommonContainer;

        string m_CurrentNameFilter;

        Dictionary<string, FolderView> m_PathFoldMap = new Dictionary<string, FolderView>();
        public virtual void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TreeBrowserWindow");
            visualTree.CloneTree(root);
            ScrollView scrollView = root.Q<ScrollView>();

            m_CurrentNameFilter = string.Empty;

            m_TypeFilter = scrollView.Q<Label>("type-filter");
            m_TypeFilter.AddManipulator(new DropdownMenuManipulator((menu) =>
            {
                foreach (var showInfo in TreeModificationProcessor.TreeLocations.ShowInfos)
                {
                    menu.AppendAction(showInfo.type, (e) =>
                    {
                        showInfo.show = !showInfo.show;
                        PopulateView();
                    }, (DropdownMenuAction a) => showInfo.show ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }                
            }, MouseButton.LeftMouse));

            m_ToolbarSearchField = scrollView.Q<ToolbarSearchField>("tree-search-field");
            m_ToolbarSearchField.RegisterValueChangedCallback((s) =>
            {
                m_CurrentNameFilter = s.newValue.ToLower();
                PopulateView();
            });

            m_LockedContainer = scrollView.Q("locked-container");
            m_CommonContainer = scrollView.Q("common-container");

            TreeModificationProcessor.TreeLocations.OnValueChanged = PopulateView;

            PopulateView();
        }

        public virtual void PopulateView()
        {
            ClearView();
            List<TreeLocations.TreeLocationInfo> treeLocationInfos = new List<TreeLocations.TreeLocationInfo>();

            string typeFilterText = string.Empty;
            foreach (var showInfo in TreeModificationProcessor.TreeLocations.ShowInfos)
            {
                if (showInfo.show)
                {
                    treeLocationInfos.AddRange(TreeModificationProcessor.TreeLocations.TreeInfoMap[showInfo.type]);
                    typeFilterText += showInfo.type + " | ";
                }
            }
            if(!string.IsNullOrEmpty(typeFilterText))
                typeFilterText = typeFilterText.Substring(0, typeFilterText.Length - 3);
            m_TypeFilter.text = string.IsNullOrEmpty(typeFilterText)? "TypeFilter" : typeFilterText;

            foreach (var treeInfo in treeLocationInfos)
            {
                if (!string.IsNullOrEmpty(m_CurrentNameFilter) && !treeInfo.name.ToLower().Contains(m_CurrentNameFilter))
                    continue;
                
                if (treeInfo.locked)
                {
                    TreeLocationInfoView treeInfoView = new TreeLocationInfoView(treeInfo);
                    treeInfoView.name = treeInfo.name;
                    m_LockedContainer.Add(treeInfoView);
                }
                {
                    var pathSplits = treeInfo.path.Split('/');
                    string folderPath = string.Empty;
                    for (int i = 0; i < pathSplits.Length - 1; i++)
                    {
                        string parentFolderPath = folderPath;
                        if (i == 0)
                            folderPath += pathSplits[i];
                        else
                            folderPath += $"/{pathSplits[i]}";

                        if (!m_PathFoldMap.ContainsKey(folderPath))
                        {
                            FolderView foldView = new FolderView(pathSplits[i]);
                            foldView.name = pathSplits[i];

                            TreeLocations.TreeFolderInfo treeFolderInfo = TreeModificationProcessor.TreeLocations.FolderInfos.Find(i => i.path == folderPath);
                            foldView.Expanded = treeFolderInfo.expanded;
                            foldView.OnExpandedStateChanged += () =>
                            {
                                treeFolderInfo.expanded = foldView.Expanded;
                                EditorUtility.SetDirty(TreeModificationProcessor.TreeLocations);
                            };

                            if (i == 0)
                                m_CommonContainer.Add(foldView);
                            else
                                m_PathFoldMap[parentFolderPath].AddContent(foldView);

                            m_PathFoldMap.Add(folderPath, foldView);
                        }
                    }

                    TreeLocationInfoView treeInfoView = new TreeLocationInfoView(treeInfo);
                    treeInfoView.name = treeInfo.name;

                    FolderView parentFold = m_PathFoldMap[folderPath];
                    parentFold.AddContent(treeInfoView);
                }
            }

            foreach (var pathFoldPair in m_PathFoldMap)
            {
                if(pathFoldPair.Value is FolderView folderView)
                {
                    List<VisualElement> children = folderView.Content.Children().ToList();
                    children = children.OrderBy(i => i is FolderView ? $"1{i.name}": $"2{i.name}").ToList();
                    folderView.Content.Clear();
                    children.ForEach(i => folderView.Content.Add(i));
                }
            }


            m_LockedContainer.style.display = m_LockedContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
        public virtual void ClearView()
        {
            m_LockedContainer.Clear();
            m_CommonContainer.Clear();
            m_PathFoldMap.Clear();
        }
    }
}