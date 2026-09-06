using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL;

namespace TreeDesigner.Editor
{
    public readonly struct PropertyPortCollectionDescriptor
    {
        public PropertyPortCollectionDescriptor(
            BaseNode owner,
            string collectionId,
            PortDirection direction,
            PortCapacity capacity,
            int count)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            CollectionId = string.IsNullOrWhiteSpace(collectionId) ? throw new ArgumentException("Property port collection identity is missing.", nameof(collectionId)) : collectionId;
            Direction = direction;
            Capacity = capacity;
            Count = count;
        }

        public BaseNode Owner { get; }
        public string CollectionId { get; }
        public PortDirection Direction { get; }
        public PortCapacity Capacity { get; }
        public int Count { get; }
    }

    public readonly struct PropertyPortAuthoringDescriptor
    {
        public PropertyPortAuthoringDescriptor(
            BaseNode owner,
            PropertyPort port,
            string collectionId,
            bool canRemove,
            IReadOnlyList<Type> acceptedValueTypes)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Port = port ?? throw new ArgumentNullException(nameof(port));
            CollectionId = collectionId ?? string.Empty;
            CanRemove = canRemove;
            AcceptedValueTypes = acceptedValueTypes ?? Array.Empty<Type>();
        }

        public BaseNode Owner { get; }
        public PropertyPort Port { get; }
        public string PortId => Port.PortId;
        public string DeclarationId => Port.DeclarationId;
        public string FieldKey => Port.FieldKey;
        public string DisplayName => Port.DisplayName;
        public Type PropertyPortType => Port.GetType();
        public Type ValueType => Port.ValueType;
        public PortDirection Direction => Port.Direction;
        public int Index => Port.Index;
        public string CollectionId { get; }
        public bool IsDynamic => !string.IsNullOrEmpty(CollectionId);
        public bool CanRemove { get; }
        public IReadOnlyList<Type> AcceptedValueTypes { get; }
    }

    public static class PropertyPortAuthoringService
    {
        public static IReadOnlyList<PropertyPortCollectionDescriptor> DescribeCollections(BaseNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var result = new List<PropertyPortCollectionDescriptor>();
            foreach (NodeFieldAccessor accessor in node.GetFieldAccessors())
            {
                if (!accessor.TryGetPropertyPortList(out List<PropertyPort> ports))
                    continue;
                PropertyPortCollectionAttribute declaration = accessor.GetAttribute<PropertyPortCollectionAttribute>();
                if (declaration == null)
                    continue;
                result.Add(new PropertyPortCollectionDescriptor(
                    node,
                    accessor.FieldKey,
                    declaration.Direction,
                    declaration.Capacity,
                    ports.Count));
            }
            return result;
        }

        public static IReadOnlyList<PropertyPortAuthoringDescriptor> DescribePorts(BaseNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var result = new List<PropertyPortAuthoringDescriptor>();
            foreach (NodeFieldAccessor accessor in node.GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPort(out PropertyPort port))
                {
                    result.Add(CreateDescriptor(node, accessor, port, string.Empty, false));
                    continue;
                }

                if (!accessor.TryGetPropertyPortList(out List<PropertyPort> ports))
                    continue;
                PropertyPortCollectionAttribute collection = accessor.GetAttribute<PropertyPortCollectionAttribute>();
                foreach (PropertyPort item in ports)
                {
                    if (item != null)
                        result.Add(CreateDescriptor(node, accessor, item, accessor.FieldKey, collection != null));
                }
            }
            return result;
        }

        public static bool TryGetByDeclaration(
            BaseNode node,
            string declarationId,
            PortDirection direction,
            out PropertyPortAuthoringDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(declarationId))
                throw new ArgumentException("Property port declaration identity is missing.", nameof(declarationId));

            foreach (PropertyPortAuthoringDescriptor candidate in DescribePorts(node))
            {
                if (candidate.Direction == direction &&
                    string.Equals(candidate.DeclarationId, declarationId, StringComparison.Ordinal))
                {
                    descriptor = candidate;
                    return true;
                }
            }

            descriptor = default;
            return false;
        }

        public static PropertyPortAuthoringDescriptor RequirePort(BaseNode node, string portId)
        {
            if (string.IsNullOrWhiteSpace(portId))
                throw new ArgumentException("Property port identity is missing.", nameof(portId));

            PropertyPortAuthoringDescriptor? match = null;
            foreach (PropertyPortAuthoringDescriptor candidate in DescribePorts(node))
            {
                if (!string.Equals(candidate.PortId, portId, StringComparison.Ordinal))
                    continue;
                if (match.HasValue)
                    throw new InvalidOperationException($"Node '{node.GUID}' contains duplicate property port identity '{portId}'.");
                match = candidate;
            }
            return match ?? throw new InvalidOperationException($"Node '{node?.GUID ?? "null"}' has no property port '{portId}'.");
        }

        public static bool TryGetPropertyPortType(Type valueType, out Type propertyPortType) =>
            PropertyPortUtility.TryGetPropertyPortType(valueType, out propertyPortType);

        public static string PortIdForDeclaration(string collectionId, string declarationId)
        {
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentException("Property port collection identity is missing.", nameof(collectionId));
            if (string.IsNullOrWhiteSpace(declarationId))
                throw new ArgumentException("Property port declaration identity is missing.", nameof(declarationId));
            return $"{collectionId}.{declarationId}";
        }

        public static IReadOnlyList<Type> GetAcceptedValueTypes(BaseNode node, string portId) =>
            RequirePort(node, portId).AcceptedValueTypes;

        public static PropertyPort Add(
            BaseNode node,
            string collectionId,
            string portId,
            string displayName,
            Type propertyPortType,
            PortDirection direction,
            int index = -1,
            string declarationId = "")
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(collectionId))
                throw new ArgumentException("Property port collection identity is missing.", nameof(collectionId));
            RequirePortId(portId);
            ValidatePropertyPortType(propertyPortType);

            NodeFieldAccessor accessor = RequireCollection(node, collectionId, out PropertyPortCollectionAttribute collection);
            List<PropertyPort> ports = accessor.GetValue() as List<PropertyPort>;
            if (ports == null)
                throw new InvalidOperationException($"Property port collection '{collectionId}' is not writable.");
            if (collection.Direction != direction)
                throw new InvalidOperationException("Property port collection direction is invalid.");
            if (DescribePorts(node).Any(value => string.Equals(value.PortId, portId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Node '{node.GUID}' already contains property port '{portId}'.");

            PropertyPort port = (PropertyPort)Activator.CreateInstance(propertyPortType);
            string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName) ? portId : displayName;
            port.Name = resolvedDisplayName;
            port.Direction = direction;
            port.Index = index < 0 ? ports.Count : index;
            port.ConfigureIdentity(
                portId,
                $"{collectionId}.{portId}",
                accessor.ModuleId,
                resolvedDisplayName,
                resolvedDisplayName);
            port.ConfigureDeclaration(declarationId);
            ports.Add(port);
            accessor.SetValue(ports);
            ReinitializeNode(node);
            return RequirePort(node, portId).Port;
        }

        public static PropertyPort Configure(
            BaseNode node,
            string portId,
            Type propertyPortType,
            PortDirection direction,
            string displayName = null)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            RequirePortId(portId);
            ValidatePropertyPortType(propertyPortType);

            PortLocation location = RequireLocation(node, portId);
            if (location.Port.Direction == direction && location.Port.GetType() == propertyPortType)
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    location.Port.Name = displayName;
                ReinitializeNode(node);
                return RequirePort(node, portId).Port;
            }

            RemoveConnections(node, location.Port.PortId);
            PropertyPort replacement = (PropertyPort)Activator.CreateInstance(propertyPortType);
            string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName) ? location.Port.Name : displayName;
            replacement.Name = resolvedDisplayName;
            replacement.Direction = direction;
            replacement.Index = location.Port.Index;
            replacement.Expanded = location.Port.Expanded;
            replacement.ConfigureIdentity(
                location.Port.PortId,
                location.Port.FieldKey,
                location.Port.OwnerModuleId,
                resolvedDisplayName,
                resolvedDisplayName);
            replacement.ConfigureDeclaration(location.Port.DeclarationId);
            location.Replace(replacement);
            ReinitializeNode(node);
            return RequirePort(node, portId).Port;
        }

        public static void Remove(BaseNode node, string portId)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            PortLocation location = RequireLocation(node, portId);
            if (!location.CanRemove)
                throw new InvalidOperationException($"Property port '{portId}' is a fixed port and cannot be removed.");

            RemoveConnections(node, location.Port.PortId);
            location.Remove();
            ReinitializeNode(node);
        }

        public static bool CanConnect(
            BaseNode sourceNode,
            string sourcePortId,
            BaseNode targetNode,
            string targetPortId)
        {
            return CanConnect(
                sourceNode,
                RequirePort(sourceNode, sourcePortId).Port,
                targetNode,
                RequirePort(targetNode, targetPortId).Port);
        }

        public static bool CanConnect(
            BaseNode sourceNode,
            PropertyPort sourcePort,
            BaseNode targetNode,
            PropertyPort targetPort)
        {
            if (sourceNode == null || targetNode == null ||
                sourcePort == null || targetPort == null ||
                ReferenceEquals(sourceNode, targetNode) ||
                sourcePort.Direction != PortDirection.Output ||
                targetPort.Direction != PortDirection.Input ||
                sourcePort.ValueType == null)
                return false;

            return GetAcceptedValueTypes(targetNode, targetPort.PortId)
                .Any(type => type == sourcePort.ValueType || type.IsAssignableFrom(sourcePort.ValueType));
        }

        public static PropertyEdge ReplaceBinding(
            BaseGraph graph,
            string sourceNodeId,
            string sourcePortId,
            string targetNodeId,
            string targetPortId)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            BaseNode sourceNode = RequireNode(graph, sourceNodeId);
            BaseNode targetNode = RequireNode(graph, targetNodeId);
            PropertyPort sourcePort = RequirePort(sourceNode, sourcePortId).Port;
            PropertyPort targetPort = RequirePort(targetNode, targetPortId).Port;
            if (!CanConnect(sourceNode, sourcePort, targetNode, targetPort))
                throw new InvalidOperationException($"Property binding '{sourceNodeId}/{sourcePortId}' → '{targetNodeId}/{targetPortId}' is not compatible.");

            PropertyEdge existing = graph.PropertyEdges.FirstOrDefault(edge =>
                edge != null &&
                edge.StartNodeGUID == sourceNodeId &&
                edge.StartPortName == sourcePortId &&
                edge.EndNodeGUID == targetNodeId &&
                edge.EndPortName == targetPortId);
            if (existing != null)
                return existing;

            foreach (PropertyEdge edge in graph.PropertyEdges.Where(edge =>
                         edge != null &&
                         edge.EndNodeGUID == targetNodeId &&
                         edge.EndPortName == targetPortId).ToList())
                graph.UnLinkProperty(edge);

            return graph.LinkProperty(sourceNode, targetNode, sourcePort, targetPort);
        }

        public static void RemoveBinding(BaseGraph graph, string edgeId)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (string.IsNullOrWhiteSpace(edgeId))
                throw new ArgumentException("Property edge identity is missing.", nameof(edgeId));
            PropertyEdge edge = graph.PropertyEdges.FirstOrDefault(value =>
                value != null && string.Equals(value.GUID, edgeId, StringComparison.Ordinal));
            if (edge == null)
                throw new InvalidOperationException($"Graph '{graph.GraphAuthoringId}' has no property edge '{edgeId}'.");
            graph.UnLinkProperty(edge);
        }

        static PropertyPortAuthoringDescriptor CreateDescriptor(
            BaseNode node,
            NodeFieldAccessor accessor,
            PropertyPort port,
            string collectionId,
            bool canRemove)
        {
            return new PropertyPortAuthoringDescriptor(
                node,
                port,
                collectionId,
                canRemove,
                ResolveAcceptedValueTypes(node, accessor, port));
        }

        static IReadOnlyList<Type> ResolveAcceptedValueTypes(
            BaseNode node,
            NodeFieldAccessor accessor,
            PropertyPort port)
        {
            var result = new List<Type>();
            if (port.ValueType != null)
                result.Add(port.ValueType);

            VariablePropertyPortAttribute variable = accessor.GetAttribute<VariablePropertyPortAttribute>();
            if (variable != null)
            {
                if (variable.AcceptableTypes != null)
                    result.AddRange(variable.AcceptableTypes);
                else if (!string.IsNullOrEmpty(variable.AcceptableTypesMethodName))
                {
                    object target = accessor.TargetObject;
                    var method = target.GetMethod(variable.AcceptableTypesMethodName);
                    if (method == null)
                        throw new InvalidOperationException($"Property port '{port.PortId}' cannot resolve acceptable value types method '{variable.AcceptableTypesMethodName}'.");
                    if (!(method.Invoke(target, new object[] { port.FieldKey }) is IEnumerable<Type> values))
                        throw new InvalidOperationException($"Property port '{port.PortId}' acceptable value types method returned an invalid value.");
                    result.AddRange(values);
                }
            }

            CompatiblePortsAttribute compatible = port.GetAttribute<CompatiblePortsAttribute>();
            if (compatible?.CompatibleTypes != null)
                result.AddRange(compatible.CompatibleTypes);
            return result.Where(type => type != null).Distinct().ToArray();
        }

        static NodeFieldAccessor RequireCollection(
            BaseNode node,
            string collectionId,
            out PropertyPortCollectionAttribute declaration)
        {
            foreach (NodeFieldAccessor accessor in node.GetFieldAccessors())
            {
                if (!string.Equals(accessor.FieldKey, collectionId, StringComparison.Ordinal))
                    continue;
                if (!accessor.TryGetPropertyPortList(out _))
                    throw new InvalidOperationException($"Field '{collectionId}' is not a property port collection.");
                declaration = accessor.GetAttribute<PropertyPortCollectionAttribute>();
                if (declaration == null)
                    throw new InvalidOperationException($"Field '{collectionId}' has no property port collection declaration.");
                return accessor;
            }
            throw new InvalidOperationException($"Node '{node.GUID}' has no property port collection '{collectionId}'.");
        }

        static PortLocation RequireLocation(BaseNode node, string portId)
        {
            RequirePortId(portId);
            PortLocation location = default;
            bool found = false;
            foreach (NodeFieldAccessor accessor in node.GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPort(out PropertyPort fieldPort))
                {
                    if (!string.Equals(fieldPort.PortId, portId, StringComparison.Ordinal))
                        continue;
                    if (found)
                        throw new InvalidOperationException($"Node '{node.GUID}' contains duplicate property port identity '{portId}'.");
                    location = new PortLocation(node, accessor, fieldPort, null, -1, false);
                    found = true;
                    continue;
                }

                if (!accessor.TryGetPropertyPortList(out List<PropertyPort> ports))
                    continue;
                PropertyPortCollectionAttribute collection = accessor.GetAttribute<PropertyPortCollectionAttribute>();
                for (int i = 0; i < ports.Count; i++)
                {
                    PropertyPort port = ports[i];
                    if (port == null || !string.Equals(port.PortId, portId, StringComparison.Ordinal))
                        continue;
                    if (found)
                        throw new InvalidOperationException($"Node '{node.GUID}' contains duplicate property port identity '{portId}'.");
                    location = new PortLocation(node, accessor, port, ports, i, collection != null);
                    found = true;
                }
            }
            if (!found)
                throw new InvalidOperationException($"Node '{node?.GUID ?? "null"}' has no property port '{portId}'.");
            return location;
        }

        static void RemoveConnections(BaseNode node, string portId)
        {
            if (node.Owner == null)
                return;
            foreach (PropertyEdge edge in node.Owner.PropertyEdges.Where(value =>
                         value != null &&
                         ((value.StartNode == node || value.StartNodeGUID == node.GUID) && value.StartPortName == portId ||
                          (value.EndNode == node || value.EndNodeGUID == node.GUID) && value.EndPortName == portId)).ToList())
                node.Owner.UnLinkProperty(edge);
        }

        static void ReinitializeNode(BaseNode node)
        {
            node.BeforeInit();
            foreach (PropertyPort port in node.PropertyPortMap.Values)
                port.Init(node);
            node.AfterInit();
            node.OnNodeChangedCallback();
        }

        static BaseNode RequireNode(BaseGraph graph, string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Node identity is missing.", nameof(nodeId));
            BaseNode node = graph.Nodes.FirstOrDefault(value =>
                value != null && string.Equals(value.GUID, nodeId, StringComparison.Ordinal));
            return node ?? throw new InvalidOperationException($"Graph '{graph.GraphAuthoringId}' has no node '{nodeId}'.");
        }

        static void RequirePortId(string portId)
        {
            if (string.IsNullOrWhiteSpace(portId))
                throw new ArgumentException("Property port identity is missing.", nameof(portId));
        }

        static void ValidatePropertyPortType(Type type)
        {
            if (type == null || !typeof(PropertyPort).IsAssignableFrom(type) || type.IsAbstract || type.IsGenericType || type.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException("Property port type is not a concrete parameterless PropertyPort.", nameof(type));
        }

        readonly struct PortLocation
        {
            readonly BaseNode m_Node;
            readonly NodeFieldAccessor m_Accessor;
            readonly List<PropertyPort> m_Collection;
            readonly int m_Index;
            readonly bool m_CanRemove;

            public PortLocation(
                BaseNode node,
                NodeFieldAccessor accessor,
                PropertyPort port,
                List<PropertyPort> collection,
                int index,
                bool canRemove)
            {
                m_Node = node;
                m_Accessor = accessor;
                Port = port;
                m_Collection = collection;
                m_Index = index;
                m_CanRemove = canRemove;
            }

            public PropertyPort Port { get; }
            public bool CanRemove => m_CanRemove;

            public void Replace(PropertyPort replacement)
            {
                if (m_Collection != null)
                {
                    m_Collection[m_Index] = replacement;
                    m_Accessor.SetValue(m_Collection);
                }
                else
                    m_Accessor.SetValue(replacement);
            }

            public void Remove()
            {
                if (m_Collection == null)
                    throw new InvalidOperationException($"Property port '{Port.PortId}' is not stored in a dynamic collection.");
                m_Collection.RemoveAt(m_Index);
                m_Accessor.SetValue(m_Collection);
            }
        }
    }
}
