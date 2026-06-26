using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    [Serializable]
    public abstract partial class BaseNode
    {
        [SerializeField]
        protected string m_GUID;
        public string GUID { get => m_GUID; set => m_GUID = value; }

        [NonSerialized]
        protected BaseGraph m_Owner;
        public BaseGraph Owner { get => m_Owner; set => m_Owner = value; }

        [NonSerialized]
        protected Dictionary<string, PropertyPort> m_PropertyPortMap = new Dictionary<string, PropertyPort>();
        public Dictionary<string, PropertyPort> PropertyPortMap => m_PropertyPortMap;
        
        [NonSerialized]
        protected List<BaseNode> m_InputPropertyNodes = new List<BaseNode>();
        public List<BaseNode> InputPropertyNodes => m_InputPropertyNodes;

        [SerializeReference]
        protected List<NodeModule> m_Modules = new List<NodeModule>();
        public IReadOnlyList<NodeModule> Modules => m_Modules;

        public virtual void BeforeInit()
        {
            EnsureModules();
            m_PropertyPortMap.Clear();
            foreach (var accessor in GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPort(out PropertyPort propertyPort))
                {
                    ConfigurePropertyPort(accessor, propertyPort, null);
                    AddPropertyPortToMap(propertyPort);
                }
                else if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                    propertyPorts.ForEach(i =>
                    {
                        if (i == null)
                            return;
                        ConfigurePropertyPort(accessor, i, i.Name);
                        AddPropertyPortToMap(i);
                    });
            }
        }
        public virtual void Init(BaseGraph tree)
        {
            m_Owner = tree;
            EnsureModules();
            foreach (var propertyPair in m_PropertyPortMap)
            {
                propertyPair.Value.Init(this);
            }
        }
        public virtual void AfterInit()
        {
            m_InputPropertyNodes.Clear();
            foreach (var propertyPortPair in m_PropertyPortMap)
            {
                if (propertyPortPair.Value.Direction == PortDirection.Input &&
                    propertyPortPair.Value.SourcePort != null &&
                    !m_InputPropertyNodes.Contains(propertyPortPair.Value.SourcePort.Owner))
                    m_InputPropertyNodes.Add(propertyPortPair.Value.SourcePort.Owner);
            }
        }
        public virtual void Dispose()
        {
            EnsureModules();
            m_Owner = null;
            foreach (var module in m_Modules)
                module?.Dispose();
            foreach (var propertyPair in m_PropertyPortMap)
            {
                propertyPair.Value.Dispose();
            }
            m_PropertyPortMap.Clear();
            m_InputPropertyNodes.Clear();
        }

        protected virtual void InputValue()
        {
            m_InputPropertyNodes.ForEach(i => i.OutputValue());
            foreach (var propertyPortPair in m_PropertyPortMap)
            {
                if (propertyPortPair.Value.Direction == PortDirection.Input)
                    propertyPortPair.Value.GetSourceValue();
            }
        }
        protected virtual void OutputValue(){ }
        public void OutputValueImperatively()
        {
            OutputValue();
        }

        public virtual void OnBeforeSerialize() { }
        public virtual void OnAfterDeserialize() 
        {
            m_Owner = null;
            //m_State = State.None;
            m_PropertyPortMap.Clear();
            m_InputPropertyNodes.Clear();

            EnsureModules();
            foreach (var accessor in GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPort(out PropertyPort propertyPort))
                {
                    propertyPort.OnAfterDeserialize();
                }
                else if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                    propertyPorts.ForEach(i => i?.OnAfterDeserialize());
            }
        }
        public virtual void OnSpawn() { }
        public virtual void OnUnspawn() { }

        public virtual bool IsConnected(string name)
        {
            return m_PropertyPortMap.TryGetValue(name, out PropertyPort propertyPort) && (!string.IsNullOrEmpty(propertyPort.InputEdgeGUID) || propertyPort.OutputEdgeGUIDs.Count > 0);
        }

        public virtual IEnumerable<NodeFieldAccessor> GetFieldAccessors()
        {
            foreach (var fieldInfo in this.GetAllFields())
            {
                if (!IsAuthoringField(fieldInfo))
                    continue;
                yield return new NodeFieldAccessor(this, this, fieldInfo, fieldInfo.Name, fieldInfo.Name, string.Empty);
            }

            EnsureModules();
            for (int i = 0; i < m_Modules.Count; i++)
            {
                var module = m_Modules[i];
                if (module == null)
                    continue;

                string moduleId = module.ModuleId;
                foreach (var fieldInfo in module.GetAllFields())
                {
                    if (!IsAuthoringField(fieldInfo))
                        continue;

                    string fieldKey = $"{moduleId}.{fieldInfo.Name}";
                    string serializedPropertyPath = $"m_Modules.Array.data[{i}].{fieldInfo.Name}";
                    yield return new NodeFieldAccessor(this, module, fieldInfo, fieldKey, serializedPropertyPath, moduleId);
                }
            }
        }

        static bool IsAuthoringField(FieldInfo fieldInfo)
        {
            return !fieldInfo.IsStatic && !fieldInfo.IsNotSerialized;
        }

        public virtual NodeFieldAccessor? FindFieldAccessor(string fieldKey)
        {
            foreach (var accessor in GetFieldAccessors())
            {
                if (accessor.FieldKey == fieldKey || accessor.FieldName == fieldKey)
                    return accessor;
            }
            return null;
        }

        public T GetModule<T>() where T : NodeModule
        {
            EnsureModules();
            return m_Modules.OfType<T>().FirstOrDefault();
        }

        protected virtual IEnumerable<NodeModule> CreateDefaultModules()
        {
            yield break;
        }

        public virtual IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            EnsureModules();
            HashSet<string> referenceKeys = new HashSet<string>();

            foreach (var module in m_Modules)
            {
                if (module == null)
                    continue;

                foreach (var reference in module.GetGraphReferences())
                {
                    if (referenceKeys.Add(reference.Key))
                        yield return reference;
                }
            }

            foreach (var accessor in GetFieldAccessors())
            {
                if (!typeof(BaseTree).IsAssignableFrom(accessor.FieldInfo.FieldType))
                    continue;

                if (referenceKeys.Add(accessor.FieldKey))
                    yield return new NodeGraphReference(this, accessor.FieldKey, accessor.FieldName, accessor.GetValue() as BaseTree, string.Empty, false);
            }
        }

        public virtual IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            EnsureModules();
            HashSet<string> referenceKeys = new HashSet<string>();

            foreach (var module in m_Modules)
            {
                if (module == null)
                    continue;

                foreach (var reference in module.GetAssetReferences())
                {
                    if (referenceKeys.Add(reference.Key))
                        yield return reference;
                }
            }
        }

        public virtual IEnumerable<BaseTree> GetReferencedTrees()
        {
            foreach (var reference in GetGraphReferences())
            {
                if (reference.Tree)
                    yield return reference.Tree;
            }
        }

        protected virtual void EnsureModules()
        {
            if (m_Modules == null)
                m_Modules = new List<NodeModule>();

            if (m_Modules.Count == 0)
                m_Modules.AddRange(CreateDefaultModules().Where(i => i != null));

            for (int i = 0; i < m_Modules.Count; i++)
            {
                var module = m_Modules[i];
                if (module == null)
                    continue;
                module.Init(this, module.DefaultModuleId);
            }
        }

        protected virtual void ConfigurePropertyPort(NodeFieldAccessor accessor, PropertyPort propertyPort, string listPortName)
        {
            string legacyName = string.IsNullOrEmpty(listPortName) ? accessor.FieldName : listPortName;
            string fieldKey = string.IsNullOrEmpty(listPortName) ? accessor.FieldKey : $"{accessor.FieldKey}.{listPortName}";
            string portId = propertyPort.HasExplicitPortId ? propertyPort.PortId : fieldKey;
            string displayName = propertyPort.DisplayName;

            if (accessor.GetAttribute<PropertyPortAttribute>() is PropertyPortAttribute propertyPortAttribute)
            {
                displayName = propertyPortAttribute.Name;
                propertyPort.Direction = propertyPortAttribute.Direction;
                if (propertyPort.Index == -1)
                    propertyPort.Index = propertyPortAttribute.Priority;
            }
            else if (accessor.GetAttribute<VariablePropertyPortAttribute>() is VariablePropertyPortAttribute variablePropertyPortAttribute)
            {
                displayName = variablePropertyPortAttribute.Name;
                propertyPort.Direction = variablePropertyPortAttribute.Direction;
                if (propertyPort.Index == -1)
                    propertyPort.Index = variablePropertyPortAttribute.Priority;
            }
            else if (!string.IsNullOrEmpty(listPortName))
                displayName = listPortName;

            propertyPort.ConfigureIdentity(portId, fieldKey, accessor.ModuleId, displayName, legacyName);
        }

        protected virtual void AddPropertyPortToMap(PropertyPort propertyPort)
        {
            if (string.IsNullOrEmpty(propertyPort.PortId))
                return;

            if (m_PropertyPortMap.ContainsKey(propertyPort.PortId))
                Debug.LogError($"Duplicate property port id: {propertyPort.PortId}");
            else
                m_PropertyPortMap.Add(propertyPort.PortId, propertyPort);
        }

        public static implicit operator bool(BaseNode exists) => exists != null;
    }
}
