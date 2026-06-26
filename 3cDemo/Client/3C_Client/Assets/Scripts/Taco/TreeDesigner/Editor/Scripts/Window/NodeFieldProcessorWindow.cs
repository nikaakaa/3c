using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Taco;
using Taco.Editor;

namespace TreeDesigner.Editor
{
    public class NodeFieldProcessorWindow : EditorWindow
    {
        bool m_Started;
        int m_CurrentIndex;
        int m_WaitFrame;
        MonoScript m_MonoScript;
        string m_FieldName;
        List<BaseTree> m_TargetTrees = new List<BaseTree>();
        List<NodeFieldInfo> m_NodeFieldInfos= new List<NodeFieldInfo>();

        public virtual void CreateGUI()
        {
            IMGUIContainer imguiContainer = new IMGUIContainer(() =>
            {
                m_MonoScript = EditorGUILayout.ObjectField("NodeType", m_MonoScript, typeof(MonoScript), false) as MonoScript;
                if(!m_Started && GUILayout.Button("Refresh"))
                {
                    if (m_TargetTrees.Count > 0)
                    {
                        m_TargetTrees.ForEach(i => Resources.UnloadAsset(i));
                        m_TargetTrees.Clear();
                    }
                    if (!m_MonoScript)
                    {
                        Debug.Log("NodeType Can't be null");
                        return;
                    }
                    foreach (var treeLocationInfo in TreeModificationProcessor.TreeLocations.TreeInfos)
                    {
                        BaseTree tree = AssetDatabase.LoadAssetAtPath<BaseTree>(treeLocationInfo.path);
                        m_TargetTrees.Add(tree);

                        bool dirty = false;
                        foreach (var node in tree.Nodes)
                        {
                            if (node.GetType() == m_MonoScript.GetClass())
                            {
                                if (node.Refresh())
                                    dirty = true;
                            }
                        }
                        if (dirty)
                        {
                            Debug.Log(treeLocationInfo.path);
                            EditorUtility.SetDirty(tree);
                            AssetDatabase.SaveAssetIfDirty(tree);
                        }
                    }

                    m_TargetTrees.ForEach(i => Resources.UnloadAsset(i));
                    m_TargetTrees.Clear();
                }

                GUILayout.Space(10);
                m_FieldName = EditorGUILayout.TextField("FieldName", m_FieldName);
                if (!m_Started && GUILayout.Button("SaveWithoutUndo"))
                {
                    if (m_TargetTrees.Count > 0)
                    {
                        m_TargetTrees.ForEach(i => Resources.UnloadAsset(i));
                        m_TargetTrees.Clear();
                    }
                    if (!m_MonoScript)
                    {
                        Debug.Log("NodeType Can't be null");
                        return;
                    }
                    if (string.IsNullOrEmpty(m_FieldName))
                    {
                        Debug.Log("FieldName Can't be null");
                        return;
                    }
                    if (m_MonoScript && m_MonoScript.GetClass().IsSubclassOf(typeof(BaseNode)) && !m_MonoScript.GetClass().IsAbstract)
                    {
                        m_Started = true;
                        m_CurrentIndex = 0;
                        m_WaitFrame = 1;
                        m_NodeFieldInfos.Clear();
                    }
                    else
                        Debug.Log("This Class Isn't Subclass Of BaseNode");
                }
                if (!m_Started && GUILayout.Button("Load"))
                {
                    if (m_TargetTrees.Count > 0)
                    {
                        m_TargetTrees.ForEach(i => Resources.UnloadAsset(i));
                        m_TargetTrees.Clear();
                    }
                    if (!m_MonoScript)
                    {
                        Debug.Log("NodeType Can't be null");
                        return;
                    }
                    if (string.IsNullOrEmpty(m_FieldName))
                    {
                        Debug.Log("FieldName Can't be null");
                        return;
                    }

                    var bytes = File.ReadAllBytes(AssetDatabase.GUIDToAssetPath(TreeDesignerUtility.DefaultFolderGUID) + "/NodeFields.data");
                    m_NodeFieldInfos = bytes.Deserialize<List<NodeFieldInfo>>();
                    foreach (var nodeFieldInfo in m_NodeFieldInfos)
                    {
                        BaseTree tree = AssetDatabase.LoadAssetAtPath<BaseTree>(nodeFieldInfo.treePath);
                        m_TargetTrees.Add(tree);

                        for (int i = 0; i < nodeFieldInfo.nodeGUIDs.Count; i++)
                        {
                            string nodeGUID = nodeFieldInfo.nodeGUIDs[i];
                            BaseNode node = tree.Nodes.Find(n => n.GUID == nodeGUID);
                            if (node != null)
                            {
                                var fieldInfo = node.GetField(m_FieldName);
                                if (fieldInfo != null)
                                {
                                    if(fieldInfo.FieldType == nodeFieldInfo.fieldType)
                                    {
                                        fieldInfo.SetValue(node, nodeFieldInfo.previousValues[i]);
                                        Debug.Log($"{tree.name}:{node.GetType()}:{m_FieldName}:{nodeFieldInfo.previousValues[i]}");
                                    }
                                    else if (fieldInfo.GetValue(node) is PropertyPort propertyPort && propertyPort.ValueType == nodeFieldInfo.fieldType)
                                    {
                                        propertyPort.SetValue(nodeFieldInfo.previousValues[i]);
                                        Debug.Log($"{tree.name}:{node.GetType()}:{m_FieldName}:{nodeFieldInfo.previousValues[i]}");
                                    }
                                }
                            }
                        }
                        EditorUtility.SetDirty(tree);
                        AssetDatabase.SaveAssetIfDirty(tree);
                    }

                    m_TargetTrees.ForEach(i => Resources.UnloadAsset(i));
                    m_TargetTrees.Clear();
                }
            });
            rootVisualElement.Add(imguiContainer);
        }
        private void Update()
        {
            if (m_WaitFrame > 0)
            {
                m_WaitFrame--;
                return;
            }
            if (m_Started)
            {
                if (m_CurrentIndex < TreeModificationProcessor.TreeLocations.TreeInfos.Count)
                {
                    TreeLocations.TreeLocationInfo treeLocationInfo = TreeModificationProcessor.TreeLocations.TreeInfos[m_CurrentIndex];
                    EditorUtility.DisplayProgressBar("FindNodeField", treeLocationInfo.name, (float)m_CurrentIndex / TreeModificationProcessor.TreeLocations.TreeInfos.Count);

                    BaseTree tree = AssetDatabase.LoadAssetAtPath<BaseTree>(treeLocationInfo.path);

                    List<string> nodeGUIDs = new List<string>();
                    List<object> previousValues = new List<object>();
                    System.Type fieldType = null;

                    foreach (var node in tree.Nodes)
                    {
                        if(node.GetType() == m_MonoScript.GetClass())
                        {
                            nodeGUIDs.Add(node.GUID);
                            var fieldInfo = node.GetField(m_FieldName);
                            fieldType = fieldInfo.FieldType;
                            previousValues.Add(fieldInfo.GetValue(node));
                        }
                    }
                    if (nodeGUIDs.Count > 0)
                        m_NodeFieldInfos.Add(new NodeFieldInfo(treeLocationInfo.path, m_FieldName, fieldType, nodeGUIDs, previousValues));

                    m_CurrentIndex++;
                    m_WaitFrame = 1;
                }
                else
                {
                    m_Started = false;
                    m_TargetTrees.ForEach(i => Resources.UnloadAsset(i));
                    m_TargetTrees.Clear();

                    var bytes = m_NodeFieldInfos.ToBinary();
                    File.WriteAllBytes(AssetDatabase.GUIDToAssetPath(TreeDesignerUtility.DefaultFolderGUID) + "/NodeFields.data", bytes);

                    EditorUtility.ClearProgressBar();
                    AssetDatabase.Refresh();
                }
            }
        }
        private void OnDisable()
        {
            m_TargetTrees.ForEach(i => Resources.UnloadAsset(i));
            m_TargetTrees.Clear();
        }

        [MenuItem("Tools/TreeDesigner/NodeFieldProcessorWindow", false, 0)]
        public static void OpenNodeFieldProcessorWindow()
        {
            GetWindow<NodeFieldProcessorWindow>();
        }

        [System.Serializable]
        public class NodeFieldInfo
        {
            public string treePath;
            public string fieldName;
            public System.Type fieldType;
            public List<string> nodeGUIDs = new List<string>();
            public List<object> previousValues = new List<object>();

            public NodeFieldInfo(string treePath, string fieldName,System.Type fieldType, List<string> nodeGUIDs, List<object> previousValues)
            {
                this.treePath = treePath;
                this.fieldName = fieldName;
                this.fieldType = fieldType;
                this.nodeGUIDs = nodeGUIDs;
                this.previousValues = previousValues;
            }
        }
    }
}