using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using Taco;

namespace TreeDesigner.Editor
{
    public static partial class TreeDesignerUtility
    {
        static bool s_Builted;
        static BiDictionary<string, Type> s_NodeTypeMap = new BiDictionary<string, Type>();
        static Dictionary<Type, MonoScript> s_NodeScriptMap = new Dictionary<Type, MonoScript>();
        static Dictionary<Type, MonoScript> s_NodeViewScriptMap = new Dictionary<Type, MonoScript>();
        static Dictionary<BaseGraph, SerializedObject> s_SerializedTreeMap = new Dictionary<BaseGraph, SerializedObject>();

        static Dictionary<Type, string> s_NodePathMap = new Dictionary<Type, string>();
        static Dictionary<string, List<(Type, string)>> s_StartPathMap = new Dictionary<string, List<(Type, string)>>();

        public const string DefaultFolderGUID = "320778c47f0f2104fa68e3102f51659e";

        static TreeDesignerUtility()
        {
            if (!s_Builted)
                BuildScriptCache();
        }

        public static bool IsReadOnly(this object target, string fieldName)
        {
            ReadOnlyAttribute readOnlyAttribute = ReflectionUtility.GetFieldAttribute<ReadOnlyAttribute>(target, fieldName);
            return readOnlyAttribute != null;
        }
        public static bool IsShow(this object target, string fieldName)
        {
            var showIfAttributes = ReflectionUtility.GetFieldAttributes<ShowIfAttribute>(target, fieldName);
            if (showIfAttributes != null && showIfAttributes.Length > 0)
            {
                foreach (var showIfAttribute in showIfAttributes)
                {
                    if (target.GetField(showIfAttribute.Name) is FieldInfo fieldInfo)
                    {
                        bool show = false;
                        foreach (var condition in showIfAttribute.Conditions)
                        {
                            if (fieldInfo.GetValue(target).Equals(condition))
                                show = true;
                        }
                        if (!show)
                            return false;
                    }
                    else if (target.GetProperty(showIfAttribute.Name) is PropertyInfo propertyInfo)
                    {
                        bool show = false;
                        foreach (var condition in showIfAttribute.Conditions)
                        {
                            if (propertyInfo.GetValue(target).Equals(condition))
                                show = true;
                        }
                        if (!show)
                            return false;
                    }
                    else if (target.GetMethod(showIfAttribute.Name) is MethodInfo methodInfo)
                    {
                        bool show = false;
                        foreach (var condition in showIfAttribute.Conditions)
                        {
                            if (methodInfo.Invoke(target, null).Equals(condition))
                                show = true;
                        }
                        if (!show)
                            return false;
                    }
                }
            }
            return true;
        }

        public static Type GetNodeType(string typeName)
        {
            if(s_NodeTypeMap.TryGetValue(typeName,out Type type))
                return type;
            else
                return null;
        }

        #region Script
        public static void BuildScriptCache()
        {
            foreach (var nodeType in TypeCache.GetTypesDerivedFrom<BaseNode>())
            {
                AddNodeType(nodeType);
                AddNodeScriptAsset(nodeType);
                AddNodePath(nodeType);
            }
            AddNodeViewScriptAsset(typeof(BaseNodeView));
            foreach (var nodeViewType in TypeCache.GetTypesDerivedFrom<BaseNodeView>())
            {
                AddNodeViewScriptAsset(nodeViewType);
            }
            s_Builted = true;
        }
        public static MonoScriptInfo GetNodeScript(Type type)
        {
            if (s_NodeScriptMap.TryGetValue(type, out MonoScript monoScript))
                return new MonoScriptInfo(monoScript);
            return FindNodeScriptByClassName(type.Name);
        }
        public static MonoScript GetNodeViewScript(Type type)
        {
            if (s_NodeViewScriptMap.TryGetValue(type, out MonoScript monoScript))
                return monoScript;
            return null;
        }
        public static Type GetNodeViewType(string nodeViewTypeName)
        {
            foreach (var nodeViewScriptPair in s_NodeViewScriptMap)
            {
                if (nodeViewScriptPair.Key.Name == nodeViewTypeName)
                    return nodeViewScriptPair.Key;
            }
            return null;
        }
        public static string GetNodePath(Type type)
        {
            if (s_NodePathMap.TryGetValue(type, out string path))
                return path;
            return string.Empty;
        }
        public static List<string> GetNodePaths(string startPath)
        {
            List<string> nodePaths = new List<string>();
            if (s_StartPathMap.TryGetValue(startPath, out List<(Type, string)> pathPairs))
                pathPairs.ForEach(i => nodePaths.Add(i.Item2));
            return nodePaths;
        }
        public static List<(Type, string)> GetNodePathPairs(string startPath)
        {
            List<(Type, string)> nodePathPairs = new List<(Type, string)>();
            if (s_StartPathMap.TryGetValue(startPath, out List<(Type, string)> pathPairs))
                nodePathPairs.AddRange(pathPairs);
            return nodePathPairs;
        }
        static void AddNodeType(Type type)
        {
            if (!s_NodeTypeMap.Contains(type.Name))
                s_NodeTypeMap.Add(type.Name, type);
        }
        static void AddNodeScriptAsset(Type type)
        {
            var nodeScriptAsset = FindScriptFromClassName(type.Name);
            if (nodeScriptAsset != null)
                s_NodeScriptMap[type] = nodeScriptAsset;
        }
        static void AddNodeViewScriptAsset(Type type)
        {
            var nodeScriptAsset = FindScriptFromClassName(type.Name);
            if (nodeScriptAsset != null)
                s_NodeViewScriptMap[type] = nodeScriptAsset;
        }
        static void AddNodePath(Type type)
        {
            if (type.IsAbstract) return;

            NodePathAttribute nodePathAttribute = type.GetAttribute<NodePathAttribute>();
            if (nodePathAttribute == null) return;

            if (!s_NodePathMap.ContainsKey(type))
                s_NodePathMap.Add(type, nodePathAttribute.Path);

            var pathSplitStrs = nodePathAttribute.Path.Split(new char[] { '/' });
            if (pathSplitStrs.Length == 1) return;

            string startPath = pathSplitStrs[0];
            if (!s_StartPathMap.ContainsKey(startPath))
                s_StartPathMap.Add(startPath, new List<(Type, string)>());
            s_StartPathMap[startPath].Add((type, nodePathAttribute.Path));
        }
        static MonoScript FindScriptFromClassName(string className)
        {
            var scriptGUIDs = AssetDatabase.FindAssets($"t:script {className}");

            if (scriptGUIDs.Length == 0)
                return null;

            foreach (var scriptGUID in scriptGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script != null && string.Equals(className, Path.GetFileNameWithoutExtension(assetPath), StringComparison.OrdinalIgnoreCase))
                    return script;
            }

            return null;
        }
        static MonoScriptInfo FindNodeScriptByClassName(string className)
        {
            string[] guids = AssetDatabase.FindAssets("t:script", new string[] { "Assets/Scripts/Tree/Node" });

            foreach (string guid in guids)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);
                string[] lines = File.ReadAllLines(filePath);
                int lineIdx = 0;
                foreach (string line in lines)
                {
                    lineIdx++;
                    if (line.Contains("class") && line.Contains(className))
                    {
                        MonoScriptInfo ret = new MonoScriptInfo();
                        ret.Mono = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
                        ret.LineNumber = lineIdx;
                        ret.ColumnNumber = line.IndexOf(className) + 1;
                        return ret;
                    }
                }
            }

            return null;
        }
        public class MonoScriptInfo
        {
            public MonoScript Mono;
            public int LineNumber;
            public int ColumnNumber;
            public MonoScriptInfo(MonoScript mono, int ln = 0, int cn = 0)
            {
                Mono = mono;
                LineNumber = ln;
                ColumnNumber = cn;
            }
            public MonoScriptInfo() { }
        }
        #endregion

        #region SerializedProperty
        public static void ApplyModify(this BaseGraph tree, string name, Action action)
        {
            bool isInstance = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tree));
            if (isInstance)
            {
                tree.GetSerializedTree().Update();
                action?.Invoke();
                tree.OnModified?.Invoke();
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(tree, $"Tree ({name})");
                tree.GetSerializedTree().Update();
                action?.Invoke();
                tree.OnModified?.Invoke();
                EditorUtility.SetDirty(tree);
            }
        }
        public static void ApplyModify(this BaseNode node, string name, Action action)
        {
            ApplyModify(node.Owner, name, action);
        }

        public static SerializedObject GetNewSerializedTree(this BaseGraph tree)
        {
            if (!s_SerializedTreeMap.ContainsKey(tree))
                s_SerializedTreeMap.Add(tree, new SerializedObject(tree));
            else
                s_SerializedTreeMap[tree] = new SerializedObject(tree);

            return s_SerializedTreeMap[tree];
        }
        public static SerializedObject GetNewSerializedTree(this BaseNode node)
        {
            return GetNewSerializedTree(node.Owner);
        }
        public static SerializedObject GetSerializedTree(this BaseGraph tree)
        {
            if (!s_SerializedTreeMap.ContainsKey(tree))
                s_SerializedTreeMap.Add(tree, new SerializedObject(tree));

            //s_SerializedTreeMap[tree].Update();
            return s_SerializedTreeMap[tree];
        }
        public static SerializedObject GetSerializedTree(this BaseNode node)
        {
            return node.Owner.GetSerializedTree();
        }
        public static SerializedObject GetSerializedTree(this BaseExposedProperty exposedProperty)
        {
            return exposedProperty.Owner.GetSerializedTree();
        }
        public static SerializedProperty GetSerializedNode(this BaseNode node)
        {
            return node.GetSerializedTree().FindProperty("m_Nodes").GetArrayElementAtIndex(node.Owner.Nodes.IndexOf(node));
        }
        public static SerializedProperty GetSerializedExposedProperty(this BaseExposedProperty exposedProperty)
        {
            return exposedProperty.GetSerializedTree().FindProperty("m_ExposedProperties").GetArrayElementAtIndex(exposedProperty.Owner.ExposedProperties.IndexOf(exposedProperty));
        }
        public static SerializedProperty GetNodeSerializedProperty(this BaseNode node, string propertyName)
        {
            return node.GetSerializedNode().FindPropertyRelative(propertyName);
        }
        public static SerializedProperty GetSerializedProperty(this NodeFieldAccessor accessor)
        {
            return accessor.OwnerNode.GetSerializedNode().FindPropertyRelative(accessor.SerializedPropertyPath);
        }
        public static SerializedProperty GetExposedPropertySerializedProperty(this BaseExposedProperty exposedProperty, string propertyName)
        {
            return exposedProperty.GetSerializedExposedProperty().FindPropertyRelative(propertyName);
        }
        #endregion

    }
}
