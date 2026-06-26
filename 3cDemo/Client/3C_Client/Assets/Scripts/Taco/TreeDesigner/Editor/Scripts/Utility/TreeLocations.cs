using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class TreeLocations : ScriptableObject
    {
        public List<TreeLocationInfo> TreeInfos = new List<TreeLocationInfo>();
        public List<TreeFolderInfo> FolderInfos = new List<TreeFolderInfo>();
        public List<TreeTypeShowInfo> ShowInfos = new List<TreeTypeShowInfo>();
        
        public Dictionary<string, List<TreeLocationInfo>> TreeInfoMap = new Dictionary<string, List<TreeLocationInfo>>();
        public Action OnValueChanged;
        
        public void Clear()
        {
            TreeInfos.Clear();
            FolderInfos.Clear();
            ShowInfos.Clear();
            TreeInfoMap.Clear();
        }
        public void AddTree(BaseTree tree, string guid, string path)
        {
            string[] splitPaths = path.Split('/');
            string folderPath = string.Empty;
            for (int i = 0; i < splitPaths.Length - 1; i++)
            {
                if (i == 0)
                    folderPath += splitPaths[i];
                else
                    folderPath += $"/{splitPaths[i]}";

                TreeFolderInfo treeFolderInfo = FolderInfos.Find(i => i.path == folderPath);
                if (!FolderInfos.Contains(treeFolderInfo))
                {
                    treeFolderInfo = new TreeFolderInfo(folderPath);
                    FolderInfos.Add(treeFolderInfo);
                }
            }

            Type treeType = tree.GetType();
            while (treeType.GetCustomAttributes(typeof(TreeWindowAttribute), false).Length == 0)
            {
                treeType = treeType.BaseType;
            }

            TreeLocationInfo treeLocationInfo = new TreeLocationInfo(guid, path, tree.name, treeType.Name);
            TreeInfos.Add(treeLocationInfo);

            if (!TreeInfoMap.ContainsKey(treeLocationInfo.type))
                TreeInfoMap.Add(treeLocationInfo.type, new List<TreeLocationInfo>());
            TreeInfoMap[treeLocationInfo.type].Add(treeLocationInfo);

            if (ShowInfos.Find(i => i.type == treeLocationInfo.type) == null)
                ShowInfos.Add(new TreeTypeShowInfo(treeLocationInfo.type, true));
        }
        public void RemoveTree(string guid)
        {
            TreeLocationInfo treeLocationInfo = TreeInfos.Find(i => i.guid == guid);
            if (treeLocationInfo != null)
            {
                TreeInfos.Remove(treeLocationInfo);
                if (TreeInfoMap.ContainsKey(treeLocationInfo.type) && TreeInfoMap[treeLocationInfo.type].Contains(treeLocationInfo))
                {
                    TreeInfoMap[treeLocationInfo.type].Remove(treeLocationInfo);
                    if(ShowInfos.Find(i => i.type == treeLocationInfo.type) is TreeTypeShowInfo treeTypeShowInfo && TreeInfoMap[treeLocationInfo.type].Count == 0)
                        ShowInfos.Remove(treeTypeShowInfo);
                }
            }
        }
        public bool Exist(string guid)
        {
            return TreeInfos.Find(i => i.guid == guid) != null;
        }
        public void OnLoaded()
        {
            foreach (var treeInfo in TreeInfos)
            {
                if (!TreeInfoMap.ContainsKey(treeInfo.type))
                    TreeInfoMap.Add(treeInfo.type, new List<TreeLocationInfo>());
                TreeInfoMap[treeInfo.type].Add(treeInfo);
            }
        }

        public static void CreateTreeLocations()
        {
            TreeLocations treeLocations = CreateInstance<TreeLocations>();
            treeLocations.hideFlags = HideFlags.NotEditable;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/TreeLocations.asset");
            AssetDatabase.CreateAsset(treeLocations, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        public static TreeLocations CreateTreeLocations(string path)
        {
            TreeLocations treeLocations = CreateInstance<TreeLocations>();
            treeLocations.hideFlags = HideFlags.NotEditable;
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/TreeLocations.asset");
            AssetDatabase.CreateAsset(treeLocations, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return treeLocations;
        }

        [Serializable]
        public class TreeFolderInfo 
        {
            public string path;
            public bool expanded;

            public TreeFolderInfo(string path)
            {
                this.path = path;
            }
        }

        [Serializable]
        public class TreeLocationInfo
        {
            public string guid;
            public string path;
            public string name;
            public string type;
            public bool locked;

            public TreeLocationInfo(string guid, string path, string name, string type)
            {
                this.guid = guid;
                this.path = path;
                this.name = name;
                this.type = type;
            }
        }

        [Serializable]
        public class TreeTypeShowInfo
        {
            public string type;
            public bool show;

            public TreeTypeShowInfo(string type,bool show)
            {
                this.type = type;
                this.show = show;
            }
        }
    }
}