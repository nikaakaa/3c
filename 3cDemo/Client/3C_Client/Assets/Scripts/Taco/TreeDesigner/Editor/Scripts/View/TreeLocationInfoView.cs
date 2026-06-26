using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace TreeDesigner.Editor 
{ 
    public class TreeLocationInfoView : VisualElement
    {
        TreeLocations.TreeLocationInfo m_TreeLocationInfo;
        Label m_TreeName;
        Label m_TreePath;
        VisualElement m_LockButton;
        VisualElement m_OpenButton;

        public bool Locked
        {
            get => m_TreeLocationInfo.locked;
            set
            {
                m_TreeLocationInfo.locked = value;
                TreeModificationProcessor.TreeLocations.OnValueChanged?.Invoke();
            }
        }

        public TreeLocationInfoView(TreeLocations.TreeLocationInfo treeLocationInfo)
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>($"VisualTree/TreeLocationInfo");
            template.CloneTree(this);

            m_TreeLocationInfo = treeLocationInfo;
            this.AddManipulator(new Clickable(() =>
            {
                BaseTree tree = AssetDatabase.LoadAssetAtPath<BaseTree>(treeLocationInfo.path);
                Selection.activeObject = tree;
            }));

            m_TreeName = this.Q<Label>("tree-name");
            m_TreeName.text = m_TreeLocationInfo.name;

            m_TreePath = this.Q<Label>("tree-path");
            m_TreePath.text = m_TreeLocationInfo.path;
            m_TreePath.SetEnabled(false);

            m_LockButton = this.Q("lock-button");
            m_LockButton.AddManipulator(new Clickable(() =>
            {
                Locked = !Locked;
                EditorUtility.SetDirty(TreeModificationProcessor.TreeLocations);
            }));

            m_OpenButton = this.Q("open-button");
            m_OpenButton.AddManipulator(new Clickable(() =>
            {
                BaseTree tree = AssetDatabase.LoadAssetAtPath<BaseTree>(m_TreeLocationInfo.path);
                if(tree)
                    TreeWindowUtility.OpenTree(tree);
            }));

            RefreshLockState();
        }

        void RefreshLockState()
        {
            RemoveFromClassList("locked");
            if (m_TreeLocationInfo.locked)
                AddToClassList("locked");
        }
    }
}