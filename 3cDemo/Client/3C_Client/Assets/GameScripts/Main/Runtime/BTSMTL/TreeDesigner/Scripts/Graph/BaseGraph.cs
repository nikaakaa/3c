using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public partial class BaseGraph
    {
        [SerializeField]
        string m_Name;
        public string name { get => string.IsNullOrEmpty(m_Name) ? GetType().Name : m_Name; set => m_Name = value; }

        [SerializeField]
        string m_GraphAuthoringId;
        public string GraphAuthoringId => m_GraphAuthoringId ?? string.Empty;

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

        [NonSerialized]
        object m_EvaluationContext;

        [NonSerialized]
        UnityEngine.Object m_SerializedOwner;

        [NonSerialized]
        string m_SerializedPropertyPath;

        [NonSerialized]
        Guid m_RuntimeId;

        [NonSerialized]
        BaseGraph m_ParentRuntimeGraph;

        [NonSerialized]
        TreeAuthoringRouteId m_AuthoringRoute;

        public int ID { get; private set; }
        public bool IsValid { get; private set; }
        public object User { get; private set; }
        public object EvaluationContext => m_EvaluationContext;
        public float DeltaTime => m_DeltaTime;
        public UnityEngine.Object SerializedOwner => m_SerializedOwner;
        public string SerializedPropertyPath => m_SerializedPropertyPath;
        public Guid RuntimeId => m_RuntimeId;
        public BaseGraph ParentRuntimeGraph => m_ParentRuntimeGraph;
        public TreeAuthoringRouteId AuthoringRoute => m_AuthoringRoute;
        public virtual GraphAuthoringRole AuthoringRole => GraphAuthoringRole.Character;

        public virtual void BindSerializedOwner(UnityEngine.Object owner, string propertyPath)
        {
            m_SerializedOwner = owner;
            m_SerializedPropertyPath = propertyPath ?? string.Empty;
        }

        public string GetSerializedPropertyPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return m_SerializedPropertyPath;

            return string.IsNullOrEmpty(m_SerializedPropertyPath)
                ? relativePath
                : $"{m_SerializedPropertyPath}.{relativePath}";
        }

        public virtual void OnAfterDeserializeGraph()
        {
            m_GUIDNodeMap.Clear();
            m_GUIDEdgeMap.Clear();
            m_GUIDPropertyEdgeMap.Clear();
            m_GUIDExposedPropertyMap.Clear();
            m_NameExposedPropertyMap.Clear();

            m_Nodes?.ForEach(i => i?.OnAfterDeserialize());
        }

        public void InitTree(object user)
        {
            InitializeTree(user, null, null);
        }

        public void InitTree(object user, BaseGraph parentRuntimeGraph, TreeAuthoringRouteId authoringRoute)
        {
            if (parentRuntimeGraph == null)
                throw new ArgumentNullException(nameof(parentRuntimeGraph));
            if (authoringRoute == null || !authoringRoute.IsValid ||
                !string.Equals(authoringRoute.LeafGraphAuthoringId, GraphAuthoringId, StringComparison.Ordinal))
                throw new ArgumentException($"Nested Graph '{name}/{GraphAuthoringId}' received an invalid authoring route.", nameof(authoringRoute));

            InitializeTree(user, parentRuntimeGraph, authoringRoute);
        }

        protected virtual void ValidateInitializationContext(
            object user,
            BaseGraph parentRuntimeGraph,
            TreeAuthoringRouteId authoringRoute)
        {
        }

        protected virtual void OnTreeInitialized()
        {
        }

        void InitializeTree(object user, BaseGraph parentRuntimeGraph, TreeAuthoringRouteId authoringRoute)
        {
            if (!AuthoringIdentity.IsValid(m_GraphAuthoringId))
                throw new InvalidOperationException($"Graph '{name}' has an invalid GraphAuthoringId.");

            TreeAuthoringRouteId resolvedRoute = parentRuntimeGraph == null
                ? TreeAuthoringRouteId.Root(m_GraphAuthoringId)
                : authoringRoute;
            if (parentRuntimeGraph != null &&
                (resolvedRoute == null || !resolvedRoute.IsValid ||
                 !string.Equals(resolvedRoute.LeafGraphAuthoringId, m_GraphAuthoringId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Nested Graph '{name}/{m_GraphAuthoringId}' requires an explicit valid authoring route.");

            ValidateInitializationContext(user, parentRuntimeGraph, resolvedRoute);
            m_ParentRuntimeGraph = parentRuntimeGraph;
            m_AuthoringRoute = resolvedRoute;

            ID = RuntimeHelpers.GetHashCode(this);
            m_RuntimeId = Guid.NewGuid();
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
            if (User is IPipelineBlackboardRuntimeAccess blackboardRuntime)
                blackboardRuntime.RegisterPipelineBlackboardVariables(this, m_ExposedProperties);
            TreeRuntimeDiagnostics.PublishGraph(this, RuntimeTraceEventKind.GraphCreated);
            OnTreeInitialized();
        }

        public virtual void DisposeTree()
        {
            TreeRuntimeDiagnostics.PublishGraph(this, RuntimeTraceEventKind.GraphDestroyed);
            if (User is IPipelineBlackboardRuntimeAccess blackboardRuntime)
                blackboardRuntime.UnregisterPipelineBlackboardGraph(this);

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
            m_EvaluationContext = null;
            m_DeltaTime = 0f;
            m_RuntimeId = Guid.Empty;
            m_ParentRuntimeGraph = null;
            m_AuthoringRoute = null;
        }

        public BaseGraph ResolveRuntimeGraph(string graphAuthoringId)
        {
            if (string.IsNullOrEmpty(graphAuthoringId))
                return null;

            for (BaseGraph graph = this; graph != null; graph = graph.m_ParentRuntimeGraph)
            {
                if (string.Equals(graph.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal))
                    return graph;
            }

            return null;
        }

#if UNITY_EDITOR
        public bool EnsureGraphAuthoringId()
        {
            if (AuthoringIdentity.IsValid(m_GraphAuthoringId))
                return false;

            m_GraphAuthoringId = AuthoringIdentity.Create();
            return true;
        }
#endif

        public void SetDeltaTime(float deltaTime)
        {
            m_DeltaTime = deltaTime;
        }

        public void SetEvaluationContext(object evaluationContext)
        {
            m_EvaluationContext = evaluationContext;
        }

        public bool TryGetEvaluationContext<T>(out T evaluationContext) where T : class
        {
            if (m_EvaluationContext is T typedContext)
            {
                evaluationContext = typedContext;
                return true;
            }

            evaluationContext = null;
            return false;
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
            if (type == null || !typeof(BaseNode).IsAssignableFrom(type) ||
                typeof(StateMachineControlNode).IsAssignableFrom(type) ||
                typeof(StateNode).IsAssignableFrom(type) ||
                typeof(StateLifecycleNode).IsAssignableFrom(type) ||
                typeof(StateMachineRuntimeFactNode).IsAssignableFrom(type) ||
                typeof(ConditionRuleResultNode).IsAssignableFrom(type))
                return false;

            return !NodeAuthoringCapabilityPolicy.TryGetCapability(type, out NodeAuthoringCapability capability) ||
                   NodeAuthoringCapabilityPolicy.Allows(AuthoringRole, capability);
        }

        public string GetNodeSerializedPropertyPath(BaseNode node)
        {
            int index = m_Nodes.IndexOf(node);
            if (index < 0)
                return string.Empty;

            return GetSerializedPropertyPath($"m_Nodes.Array.data[{index}]");
        }

        public string GetEdgeSerializedPropertyPath(BaseEdge edge)
        {
            int index = m_Edges.IndexOf(edge);
            if (index < 0)
                return string.Empty;

            return GetSerializedPropertyPath($"m_Edges.Array.data[{index}]");
        }

        public string GetExposedPropertySerializedPropertyPath(BaseExposedProperty exposedProperty)
        {
            int index = m_ExposedProperties.IndexOf(exposedProperty);
            if (index < 0)
                return string.Empty;

            return GetSerializedPropertyPath($"m_ExposedProperties.Array.data[{index}]");
        }

        public static implicit operator bool(BaseGraph exists) => exists != null;
    }
}
