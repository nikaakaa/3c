using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class TreeModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        static TreeLocations s_TreeLocations;
        public static TreeLocations TreeLocations
        {
            get
            {
                if (s_TreeLocations == null)
                {
                    s_TreeLocations = Resources.Load<TreeLocations>("Default/TreeLocations");
                    if(s_TreeLocations == null)
                        s_TreeLocations = TreeLocations.CreateTreeLocations(AssetDatabase.GUIDToAssetPath(TreeDesignerUtility.DefaultFolderGUID));
                    s_TreeLocations.OnLoaded();
                }
                return s_TreeLocations;
            }
        }

        [MenuItem("Assets/TreeDesigner/ClearLocations")]
        public static void ClearLocations()
        {
            TreeLocations.Clear();
            TreeLocations.OnValueChanged?.Invoke();
            EditorUtility.SetDirty(TreeLocations);
        }

        [MenuItem("Assets/TreeDesigner/FindLocations")]
        public static void FindLocations()
        {
            string[] guids = Selection.assetGUIDs;
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(assetPath))
                    SearchDirectory(assetPath);
            }
            TreeLocations.OnValueChanged?.Invoke();
            EditorUtility.SetDirty(TreeLocations);
        }
        public static void FindLocations(string path)
        {
            if (Directory.Exists(path))
                SearchDirectory(path);

            TreeLocations.OnValueChanged?.Invoke();
            EditorUtility.SetDirty(TreeLocations);
        }

        static void SearchDirectory(string directory)
        {
            foreach (var treeAsset in GetTrees(directory))
            {
                string path = AssetDatabase.GetAssetPath(treeAsset);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (TreeLocations.Exist(guid))
                    TreeLocations.RemoveTree(guid);
                TreeLocations.AddTree(treeAsset.Tree, guid, path);
            }
            TreeLocations.FolderInfos = TreeLocations.FolderInfos.Where(i => Directory.Exists(i.path)).ToList();
        }
        static List<BaseTreeAsset> GetTrees(string directory)
        {
            List<BaseTreeAsset> trees = new List<BaseTreeAsset>();
            DirectoryInfo dInfo = new DirectoryInfo(directory);
            FileInfo[] fileInfoArr = dInfo.GetFiles("*.asset", SearchOption.AllDirectories);
            for (int i = 0; i < fileInfoArr.Length; ++i)
            {
                string fullName = fileInfoArr[i].FullName;
                fullName = fullName.Substring(fullName.IndexOf("Assets"));
                fullName = fullName.Replace('\\', '/');

                BaseTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(fullName);
                if (treeAsset)
                    trees.Add(treeAsset);
            }
            return trees;
        }

        static void OnWillCreateAsset(string path)
        {
            EditorApplication.delayCall += () =>
            {
                BaseTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(path);
                if (treeAsset != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    TreeLocations.AddTree(treeAsset.Tree, guid, path);
                    TreeLocations.OnValueChanged?.Invoke();
                    EditorUtility.SetDirty(TreeLocations);
                }             
            };
        }
        static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            if (TreeLocations.FolderInfos.Find(i=>i.path == oldPath) != null)
            {
                EditorApplication.delayCall += () =>
                {
                    FindLocations(newPath);
                    TreeLocations.FolderInfos = TreeLocations.FolderInfos.Where(i => Directory.Exists(i.path)).ToList();
                    TreeLocations.OnValueChanged?.Invoke();
                    EditorUtility.SetDirty(TreeLocations);
                };
            }
            else
            {
                BaseTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(oldPath);
                if (treeAsset != null)
                {
                    EditorApplication.delayCall += () =>
                    {
                        string guid = AssetDatabase.AssetPathToGUID (newPath);
                        TreeLocations.RemoveTree(guid);
                        TreeLocations.AddTree(treeAsset.Tree, guid, newPath);
                        TreeLocations.FolderInfos = TreeLocations.FolderInfos.Where(i => Directory.Exists(i.path)).ToList();
                        TreeLocations.OnValueChanged?.Invoke();
                        EditorUtility.SetDirty(TreeLocations);
                    };
                }
            }
            return AssetMoveResult.DidNotMove;
        }
        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions removeAssetOptions)
        {
            if (TreeLocations.FolderInfos.Find(i => i.path == path) != null)
            {
                foreach (var treeAsset in GetTrees(path))
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(treeAsset));
                    if (TreeLocations.Exist(guid))
                        TreeLocations.RemoveTree(guid);
                }

                EditorApplication.delayCall += () =>
                {
                    TreeLocations.FolderInfos = TreeLocations.FolderInfos.Where(i => Directory.Exists(i.path)).ToList();
                    TreeLocations.OnValueChanged?.Invoke();
                    EditorUtility.SetDirty(TreeLocations);
                };
            }
            else
            {
                BaseTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(path);
                if (treeAsset != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (TreeLocations.Exist(guid))
                        TreeLocations.RemoveTree(guid);

                    EditorApplication.delayCall += () =>
                    {
                        TreeLocations.FolderInfos = TreeLocations.FolderInfos.Where(i => Directory.Exists(i.path)).ToList();
                        TreeLocations.OnValueChanged?.Invoke();
                        EditorUtility.SetDirty(TreeLocations);
                    };
                }
            }
		    return AssetDeleteResult.DidNotDelete;
        }
    }
}
