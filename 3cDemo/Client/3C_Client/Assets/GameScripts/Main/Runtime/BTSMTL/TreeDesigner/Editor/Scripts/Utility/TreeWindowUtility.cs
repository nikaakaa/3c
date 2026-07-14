#if UNITY_EDITOR

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BTSMTL;

namespace TreeDesigner.Editor
{
    public partial class TreeWindowUtilityInstance 
    {
        public BaseTreeWindow OpenBaseTreeWindow(BaseTree tree = null)
        {
            return TreeWindowUtility.GetWindow<BaseTreeWindow>(tree);
        }
        public BaseTreeWindow OpenBaseTreeAssetWindow(BaseTreeAsset treeAsset)
        {
            return TreeWindowUtility.GetWindow<BaseTreeWindow>(treeAsset);
        }
        public BaseTreeWindow OpenSubTreeWindow(SubTree tree = null)
        {
            return TreeWindowUtility.GetWindow<SubTreeWindow>(tree);
        }
    }


    public static partial class TreeWindowUtility
    {
        static TreeWindowUtilityInstance s_TreeWindowUtilityInstance;
        public static TreeWindowUtilityInstance TreeWindowUtilityInstance => s_TreeWindowUtilityInstance;

        static Dictionary<Type, BaseTreeWindow> s_TreeWindowTypeMap = new Dictionary<Type, BaseTreeWindow>();
        static Type[] s_WindowTypes;
        static List<BaseTreeWindow> s_ActiveWindows = new List<BaseTreeWindow>();
        static CurrentSelectedTree s_CurrentSelectedTree;

        public static event Action<BaseTreeWindow, BaseTree> OnOpened;

        static TreeWindowUtility()
        {
            s_TreeWindowUtilityInstance = new TreeWindowUtilityInstance();
            GetExistWindows();
            s_CurrentSelectedTree = Resources.Load<CurrentSelectedTree>("Default/CurrentSelectedTree");
            if(s_CurrentSelectedTree == null)
                s_CurrentSelectedTree = CurrentSelectedTree.CreateTreeLocations(AssetDatabase.GUIDToAssetPath(TreeDesignerUtility.DefaultFolderGUID));
            s_CurrentSelectedTree.TreeAsset = null;
            Undo.undoRedoPerformed += OnUndoRedo;
        }
        public static T GetWindow<T>(BaseTreeAsset treeAsset) where T : BaseTreeWindow
        {
            T treeWindow = GetWindow<T>(treeAsset ? treeAsset.Tree : null);
            if (treeAsset)
                treeWindow.ReplaceNavigationRoot(treeAsset);
            return treeWindow;
        }
        public static T GetWindow<T>(BaseTree tree = null) where T : BaseTreeWindow
        {
            if (s_TreeWindowTypeMap.ContainsKey(typeof(T)))
            {
                BaseTreeWindow treeWindow = s_TreeWindowTypeMap[typeof(T)];
                if (treeWindow == null)
                {
                    treeWindow = EditorWindow.CreateWindow<T>(s_WindowTypes);
                    treeWindow.titleContent = new GUIContent(typeof(T).Name);
                    s_TreeWindowTypeMap[typeof(T)] = treeWindow;
                }
                treeWindow.Show();
                treeWindow.Focus();
                treeWindow.ReplaceNavigationRoot(tree);

                if (!s_ActiveWindows.Contains(treeWindow))
                    s_ActiveWindows.Add(treeWindow);
                return treeWindow as T;
            }
            else
            {
                BaseTreeWindow treeWindow = EditorWindow.CreateWindow<T>(s_WindowTypes);
                treeWindow.titleContent = new GUIContent(typeof(T).Name);
                s_TreeWindowTypeMap.Add(typeof(T), treeWindow);

                treeWindow.Show();
                treeWindow.Focus();
                treeWindow.ReplaceNavigationRoot(tree);

                if (!s_ActiveWindows.Contains(treeWindow))
                    s_ActiveWindows.Add(treeWindow);
                return treeWindow as T;
            }
        }
        static void GetExistWindows()
        {
            var types = TypeCache.GetTypesDerivedFrom<BaseTreeWindow>().ToList();
            types = types.OrderByDescending(i =>
            {
                int count = 0;
                Type baseType = i.BaseType;
                while (baseType != null)
                {
                    count++;
                    baseType = baseType.BaseType;
                }
                return count;
            }).ToList();

            types.Add(typeof(BaseTreeWindow));
            s_WindowTypes = types.ToArray();

            foreach (var item in types)
            {
                BaseTreeWindow treeWindow = GetExistWindow(item);
                if (treeWindow != null)
                    s_TreeWindowTypeMap.Add(item, treeWindow);
            }
        }
        static BaseTreeWindow GetExistWindow(Type type)
        {
            UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(type);
            foreach (var item in array)
            {
                if (item.GetType() == type)
                    return item as BaseTreeWindow;
            }
            return null;
        }

        public static void SelectTree(BaseTree tree)
        {
            Undo.RegisterCompleteObjectUndo(s_CurrentSelectedTree, $"Tree (Selected Changed)");
            s_CurrentSelectedTree.TreeAsset = tree?.SerializedOwner as BaseTreeAsset;
        }
        public static void OnWindowClosed(BaseTreeWindow treeWindow)
        {
            if (s_ActiveWindows.Contains(treeWindow))
                s_ActiveWindows.Remove(treeWindow);
            if (s_ActiveWindows.Count == 0)
            {
                //Undo.ClearUndo(s_CurrentSelectedTree);
                Undo.ClearAll();
                s_CurrentSelectedTree.TreeAsset = null;
            }
        }
        static void OnUndoRedo()
        {
            if (!s_CurrentSelectedTree.Tree)
                return;

            if (s_ActiveWindows.Any(i => i && i.Tree == s_CurrentSelectedTree.Tree))
                return;

            OpenTree(s_CurrentSelectedTree.Tree);
        }

        [MenuItem("Tools/TreeDesigner/TreeBrowserWindow", false, 0)]
        public static void OpenTreeBrowserWindow()
        {
            EditorWindow.GetWindow<TreeBrowserWindow>();
        }

        public static void OpenTree(this BaseTree tree)
        {
            OpenTree(tree, null);
        }

        public static BaseTreeWindow OpenTree(this BaseTree tree, object authoringContext)
        {
            if (tree == null)
                return null;

            if (tree.SerializedOwner is BaseTreeAsset treeAsset)
            {
                return OpenTree(treeAsset, authoringContext);
            }

            var treeWindowAttributes = tree.GetAttributes<TreeWindowAttribute>();
            TreeWindowAttribute treeWindowAttribute = treeWindowAttributes[treeWindowAttributes.Length - 1];

            MethodInfo methodInfo = ReflectionUtility.GetMethod(s_TreeWindowUtilityInstance, treeWindowAttribute.Label);
            BaseTreeWindow treeWindow = methodInfo.Invoke(s_TreeWindowUtilityInstance, new object[] { tree }) as BaseTreeWindow;
            treeWindow.ReplaceNavigationRoot(tree, authoringContext);
            NotifyOpened(treeWindow, tree);
            return treeWindow;
        }
        public static BaseTreeWindow OpenTree(this BaseTreeAsset treeAsset)
        {
            return OpenTree(treeAsset, null);
        }

        public static BaseTreeWindow OpenTree(this BaseTreeAsset treeAsset, object authoringContext)
        {
            if (!treeAsset)
                return null;

            BaseTree tree = treeAsset.Tree;
            var treeWindowAttributes = tree.GetAttributes<TreeWindowAttribute>();
            TreeWindowAttribute treeWindowAttribute = treeWindowAttributes[treeWindowAttributes.Length - 1];

            MethodInfo methodInfo = ReflectionUtility.GetMethod(s_TreeWindowUtilityInstance, treeWindowAttribute.Label);
            BaseTreeWindow treeWindow = methodInfo.Invoke(s_TreeWindowUtilityInstance, new object[] { tree }) as BaseTreeWindow;
            treeWindow.ReplaceNavigationRoot(treeAsset, authoringContext);
            NotifyOpened(treeWindow, tree);
            return treeWindow;
        }
        internal static void NotifyOpened(BaseTreeWindow treeWindow, BaseTree tree)
        {
            OnOpened?.Invoke(treeWindow, tree);
        }
        
        [MenuItem("Tools/TreeDesigner/BaseTreeWindow", false, 1)]
        public static void OpenBaseTreeWindow()
        {
            s_TreeWindowUtilityInstance.OpenBaseTreeWindow(null);
        }
       
        [MenuItem("Tools/TreeDesigner/SubTreeWindow", false, 2)]
        public static void OpenSubTreeWindow()
        {
            s_TreeWindowUtilityInstance.OpenSubTreeWindow(null);
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            var asset = obj as BaseTreeAsset;
            if(asset == null) return false;

            TreeWindowUtility.OpenTree(asset);
            return true;
        }
    }
}

#endif
