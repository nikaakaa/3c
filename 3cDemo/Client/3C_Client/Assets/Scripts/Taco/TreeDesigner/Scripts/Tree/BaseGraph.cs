using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    public abstract partial class BaseGraph : ScriptableObject
    {
        [SerializeReference]
        protected List<BaseNode> m_Nodes = new List<BaseNode>();
        public List<BaseNode> Nodes => m_Nodes;

        [SerializeField]
        protected List<BaseEdge> m_Edges = new List<BaseEdge>();
        public List<BaseEdge> Edges => m_Edges;

        [SerializeField]
        protected List<PropertyEdge> m_PropertyEdges = new List<PropertyEdge>();
        public List<PropertyEdge> PropertyEdges => m_PropertyEdges;

        [SerializeReference]
        protected List<BaseExposedProperty> m_ExposedProperties = new List<BaseExposedProperty>();
        public List<BaseExposedProperty> ExposedProperties => m_ExposedProperties;

        [NonSerialized]
        protected Dictionary<string, BaseNode> m_GUIDNodeMap = new Dictionary<string, BaseNode>();
        public Dictionary<string, BaseNode> GUIDNodeMap => m_GUIDNodeMap;

        [NonSerialized]
        protected Dictionary<string, BaseEdge> m_GUIDEdgeMap = new Dictionary<string, BaseEdge>();
        public Dictionary<string, BaseEdge> GUIDEdgeMap => m_GUIDEdgeMap;

        [NonSerialized]
        protected Dictionary<string, PropertyEdge> m_GUIDPropertyEdgeMap = new Dictionary<string, PropertyEdge>();
        public Dictionary<string, PropertyEdge> GUIDPropertyEdgeMap => m_GUIDPropertyEdgeMap;

        [NonSerialized]
        protected Dictionary<string, BaseExposedProperty> m_GUIDExposedPropertyMap = new Dictionary<string, BaseExposedProperty>();
        public Dictionary<string, BaseExposedProperty> GUIDExposedPropertyMap => m_GUIDExposedPropertyMap;

        [NonSerialized]
        protected Dictionary<string, BaseExposedProperty> m_NameExposedPropertyMap = new Dictionary<string, BaseExposedProperty>();

        [NonSerialized]
        protected Dictionary<BaseExposedProperty, object> m_ExposedPropertyOriginalValueMap = new Dictionary<BaseExposedProperty, object>();

        [NonSerialized]
        protected float m_DeltaTime;

        public int ID { get; private set; }
        public bool IsValid { get; private set; }
        public object User { get; private set; }
        public float DeltaTime => m_DeltaTime;

        public virtual void InitTree(object user)
        {
            ID = GetInstanceID();
            IsValid = true;
            User = user;
            m_DeltaTime = 0f;

            m_GUIDNodeMap.Clear();
            m_GUIDEdgeMap.Clear();
            m_GUIDPropertyEdgeMap.Clear();
            m_GUIDExposedPropertyMap.Clear();
            m_NameExposedPropertyMap.Clear();

            m_Nodes.ForEach(i =>
            {
                m_GUIDNodeMap.Add(i.GUID, i);
                i.BeforeInit();
            });
            m_Edges.ForEach(i => m_GUIDEdgeMap.Add(i.GUID, i));
            m_PropertyEdges.ForEach(i => m_GUIDPropertyEdgeMap.Add(i.GUID, i));
            m_ExposedProperties.ForEach(i =>
            {
                m_GUIDExposedPropertyMap.Add(i.GUID, i);
                m_NameExposedPropertyMap.Add(i.Name, i);
            });

            m_Edges.ForEach(i => i.Init(this));
            m_PropertyEdges.ForEach(i => i.Init(this));
            m_Nodes.ForEach(i => i.Init(this));
            m_Nodes.ForEach(i => i.AfterInit());
            m_ExposedProperties.ForEach(i => i.Init(this));
        }

        public virtual void DisposeTree()
        {
            m_Nodes.ForEach(i => i.Dispose());
            m_Edges.ForEach(i => i.Dispose());
            m_PropertyEdges.ForEach(i => i.Dispose());
            m_ExposedProperties.ForEach(i => i.Dispose());

            m_GUIDNodeMap.Clear();
            m_GUIDEdgeMap.Clear();
            m_GUIDPropertyEdgeMap.Clear();
            m_GUIDExposedPropertyMap.Clear();

            IsValid = false;
            User = null;
            m_DeltaTime = 0f;
        }

        public void SetDeltaTime(float deltaTime)
        {
            m_DeltaTime = deltaTime;
        }

        public bool TryGetUser<T>(out T user) where T : class
        {
            if (User is T directUser)
            {
                user = directUser;
                return true;
            }

            GameObject gameObject = User switch
            {
                GameObject go => go,
                Component component => component.gameObject,
                _ => null
            };

            if (gameObject)
            {
                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is T componentUser)
                    {
                        user = componentUser;
                        return true;
                    }
                }
            }

            user = null;
            return false;
        }

        public virtual void OnSpawn()
        {
            m_ExposedPropertyOriginalValueMap.Clear();
            m_ExposedProperties.ForEach(i => m_ExposedPropertyOriginalValueMap.Add(i, i.GetValue()));
            m_Nodes.ForEach(i => i.OnSpawn());
        }

        public virtual void OnUnspawn()
        {
            m_ExposedProperties.ForEach(i => i.SetValue(m_ExposedPropertyOriginalValueMap[i]));
            m_Nodes.ForEach(i => i.OnUnspawn());
        }

        public BaseExposedProperty GetExposedProperty(string name)
        {
            if (m_NameExposedPropertyMap.TryGetValue(name, out BaseExposedProperty exposedProperty))
                return exposedProperty;
            return null;
        }

        public T GetExposedProperty<T>(string name) where T : BaseExposedProperty
        {
            if (m_NameExposedPropertyMap.TryGetValue(name, out BaseExposedProperty exposedProperty))
                return exposedProperty as T;
            return null;
        }

        public IEnumerable<BaseEdge> GetInputEdges(BaseNode node, string endPortName = null)
        {
            if (node == null || m_Edges == null)
                yield break;

            for (int i = 0; i < m_Edges.Count; i++)
            {
                BaseEdge edge = m_Edges[i];
                if (edge == null)
                    continue;

                bool nodeMatches = edge.EndNode == node || edge.EndNodeGUID == node.GUID;
                bool portMatches = string.IsNullOrEmpty(endPortName) || edge.EndPortName == endPortName;
                if (nodeMatches && portMatches)
                    yield return edge;
            }
        }

        public IEnumerable<BaseEdge> GetOutputEdges(BaseNode node, string startPortName = null)
        {
            if (node == null || m_Edges == null)
                yield break;

            for (int i = 0; i < m_Edges.Count; i++)
            {
                BaseEdge edge = m_Edges[i];
                if (edge == null)
                    continue;

                bool nodeMatches = edge.StartNode == node || edge.StartNodeGUID == node.GUID;
                bool portMatches = string.IsNullOrEmpty(startPortName) || edge.StartPortName == startPortName;
                if (nodeMatches && portMatches)
                    yield return edge;
            }
        }

        public virtual bool CanCreateNodeType(Type type)
        {
            return type != null &&
                   typeof(BaseNode).IsAssignableFrom(type) &&
                   !typeof(StateMachineControlNode).IsAssignableFrom(type) &&
                   !typeof(StateNode).IsAssignableFrom(type) &&
                   !typeof(StateLifecycleNode).IsAssignableFrom(type) &&
                   !typeof(TransitionRuleResultNode).IsAssignableFrom(type);
        }
    }
}
