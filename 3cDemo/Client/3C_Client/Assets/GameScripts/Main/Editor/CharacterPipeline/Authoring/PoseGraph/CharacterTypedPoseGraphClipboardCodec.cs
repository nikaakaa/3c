using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterTypedPoseGraphClipboardCodec :
        IGraphAuthoringClipboardCodec
    {
        const string Schema = "character-pose-clipboard.v1";
        readonly IGraphAuthoringDomainMutation m_Mutation;

        public CharacterTypedPoseGraphClipboardCodec(
            IGraphAuthoringDomainMutation mutation)
        {
            m_Mutation = mutation ??
                         throw new ArgumentNullException(nameof(mutation));
        }

        public string Serialize(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringSelection> selection)
        {
            CharacterTypedPoseGraphDocument pose = RequireDocument(document);
            HashSet<string> selected = (selection ??
                                        throw new ArgumentNullException(
                                            nameof(selection)))
                .Where(value =>
                    value.Kind == GraphAuthoringSelectionKind.Node)
                .Select(value => value.ElementId.Value)
                .ToHashSet(StringComparer.Ordinal);
            if (selected.Count == 0)
                throw new InvalidOperationException(
                    "Pose clipboard selection contains no nodes.");

            var payload = new ClipboardPayload { schema = Schema };
            foreach (CharacterTypedPoseNode node in pose.Graph.Nodes
                         .Where(value => selected.Contains(value.NodeId.Value))
                         .OrderBy(value => value.NodeId))
            {
                RequireCopyable(node);
                GraphAuthoringCapabilityDescriptor capability =
                    CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                        CharacterPoseGraphAuthoringCapabilities.Get(node.Kind),
                        document.DomainId,
                        document.DocumentRoleId);
                var properties = new JObject();
                foreach (GraphAuthoringFieldDescriptor field in
                         capability.Fields.OrderBy(value => value.FieldId))
                {
                    properties[field.FieldId.Value] =
                        CharacterPoseAuthoringPayloadCodec.EncodeValue(
                            CharacterPoseAuthoringPayloadCodec.Read(
                                node.Payload,
                                field.FieldId.Value),
                            EncodeAsset);
                }
                Vector2 position = pose.Graph.Layout
                    .LastOrDefault(value => value.NodeId == node.NodeId)
                    ?.Position ?? Vector2.zero;
                payload.nodes.Add(new ClipboardNode
                {
                    id = node.NodeId.Value,
                    capability = capability.CapabilityId.Value,
                    name = node.DisplayName,
                    x = position.x,
                    y = position.y,
                    properties = properties,
                    dynamicPorts = node.DynamicPorts.Select(value =>
                        new ClipboardPort
                        {
                            id = value.PortId.Value,
                            name = value.DisplayName,
                            valueType =
                                CharacterTypedPoseGraphDocument.ValueType(
                                    value.Kind),
                            direction = value.Direction.ToString(),
                            required = value.Required,
                            order = value.Order,
                            interfacePortId =
                                value.InterfacePortId.Value ?? string.Empty
                        }).ToList()
                });
            }
            payload.edges.AddRange(pose.Graph.Edges
                .Where(value =>
                    selected.Contains(value.SourceNodeId.Value) &&
                    selected.Contains(value.TargetNodeId.Value))
                .OrderBy(value => value.EdgeId, StringComparer.Ordinal)
                .Select(value => new ClipboardEdge
                {
                    id = value.EdgeId,
                    sourceNode = value.SourceNodeId.Value,
                    sourcePort = value.SourcePortId.Value,
                    targetNode = value.TargetNodeId.Value,
                    targetPort = value.TargetPortId.Value
                }));
            return JObject.FromObject(payload)
                .ToString(Formatting.None);
        }

        public bool CanPaste(
            IGraphAuthoringDocumentProjection document,
            string payload)
        {
            try
            {
                Validate(RequireDocument(document), Parse(payload));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Paste(
            IGraphAuthoringDocumentProjection document,
            string operationName,
            string payload,
            Vector2 graphPosition)
        {
            CharacterTypedPoseGraphDocument pose = RequireDocument(document);
            ClipboardPayload value = Parse(payload);
            Validate(pose, value);
            Vector2 origin = new Vector2(
                value.nodes.Min(node => node.x),
                value.nodes.Min(node => node.y));
            Vector2 offset = graphPosition - origin;
            var nodeIds = value.nodes.ToDictionary(
                node => node.id,
                _ => Guid.NewGuid().ToString("N"),
                StringComparer.Ordinal);
            var portIds = new Dictionary<string, string>(
                StringComparer.Ordinal);
            var requests = new List<GraphAuthoringMutationRequest>();
            foreach (ClipboardNode node in value.nodes)
            {
                CharacterPoseNodeKind kind = ResolveKind(
                    node.capability,
                    pose.DocumentRoleId);
                CharacterPoseDynamicPort[] dynamicPorts =
                    node.dynamicPorts.Select(port =>
                    {
                        string newId = Guid.NewGuid().ToString("N");
                        portIds.Add(
                            PortKey(node.id, port.id),
                            newId);
                        return new CharacterPoseDynamicPort(
                            new PosePortId(newId),
                            port.name,
                            PortKind(port.valueType),
                            Enum.Parse<CharacterPosePortDirection>(
                                port.direction,
                                false),
                            port.required,
                            port.order,
                            string.IsNullOrWhiteSpace(port.interfacePortId)
                                ? default
                                : new PoseInterfacePortId(
                                    port.interfacePortId));
                    }).ToArray();
                CharacterPoseNodePayload typedPayload =
                    CharacterPoseAuthoringPayloadCodec.Create(
                        kind,
                        new CharacterPoseAuthoringPayloadInput(
                            (fieldId, expectedType) => DecodeField(
                                pose,
                                node,
                                fieldId,
                                expectedType)));
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateNode,
                    capabilityId: new GraphAuthoringCapabilityId(
                        node.capability),
                    value: new CharacterTypedPoseNode(
                        new PoseNodeId(nodeIds[node.id]),
                        node.name,
                        typedPayload,
                        dynamicPorts),
                    position: new Vector2(node.x, node.y) + offset));
            }
            foreach (ClipboardEdge edge in value.edges)
            {
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.ConnectPorts,
                    sourceNodeId: new GraphAuthoringElementId(
                        nodeIds[edge.sourceNode]),
                    sourcePortId: new GraphAuthoringPortId(
                        RemapPort(
                            edge.sourceNode,
                            edge.sourcePort,
                            portIds)),
                    targetNodeId: new GraphAuthoringElementId(
                        nodeIds[edge.targetNode]),
                    targetPortId: new GraphAuthoringPortId(
                        RemapPort(
                            edge.targetNode,
                            edge.targetPort,
                            portIds))));
            }
            m_Mutation.Apply(pose, requests);
        }

        static void Validate(
            CharacterTypedPoseGraphDocument document,
            ClipboardPayload payload)
        {
            if (payload == null ||
                !string.Equals(
                    payload.schema,
                    Schema,
                    StringComparison.Ordinal) ||
                payload.nodes == null ||
                payload.nodes.Count == 0 ||
                payload.edges == null)
            {
                throw new InvalidOperationException(
                    "Pose clipboard payload is incomplete.");
            }
            Dictionary<string, ClipboardNode> nodes = payload.nodes
                .ToDictionary(
                    node => Require(node.id, "node identity"),
                    StringComparer.Ordinal);
            foreach (ClipboardNode node in nodes.Values)
            {
                CharacterPoseNodeKind kind = ResolveKind(
                    node.capability,
                    document.DocumentRoleId);
                RequireCopyable(kind);
                GraphAuthoringCapabilityDescriptor capability =
                    CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                        new GraphAuthoringCapabilityId(node.capability),
                        document.DomainId,
                        document.DocumentRoleId);
                HashSet<string> fields = capability.Fields
                    .Select(value => value.FieldId.Value)
                    .ToHashSet(StringComparer.Ordinal);
                if (node.properties == null ||
                    !fields.SetEquals(
                        node.properties.Properties()
                            .Select(value => value.Name)))
                {
                    throw new InvalidOperationException(
                        $"Pose clipboard node '{node.id}' fields do not match capability '{node.capability}'.");
                }
                if (node.dynamicPorts == null ||
                    node.dynamicPorts
                        .Select(port =>
                            Require(port.id, "dynamic port identity"))
                        .Distinct(StringComparer.Ordinal)
                        .Count() != node.dynamicPorts.Count)
                {
                    throw new InvalidOperationException(
                        $"Pose clipboard node '{node.id}' has invalid dynamic ports.");
                }
                CharacterPoseAuthoringPayloadCodec.Create(
                    kind,
                    new CharacterPoseAuthoringPayloadInput(
                        (fieldId, expectedType) => DecodeField(
                            document,
                            node,
                            fieldId,
                            expectedType)));
            }
            if (payload.edges
                .Select(edge => Require(edge.id, "edge identity"))
                .Distinct(StringComparer.Ordinal)
                .Count() != payload.edges.Count)
            {
                throw new InvalidOperationException(
                    "Pose clipboard contains duplicate edge identities.");
            }
            foreach (ClipboardEdge edge in payload.edges)
            {
                if (!nodes.ContainsKey(edge.sourceNode) ||
                    !nodes.ContainsKey(edge.targetNode))
                {
                    throw new InvalidOperationException(
                        $"Pose clipboard edge '{edge.id}' leaves the copied node closure.");
                }
                Require(edge.sourcePort, "source port identity");
                Require(edge.targetPort, "target port identity");
            }
        }

        static object DecodeField(
            CharacterTypedPoseGraphDocument document,
            ClipboardNode node,
            string fieldId,
            Type expectedType)
        {
            GraphAuthoringCapabilityDescriptor capability =
                CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                    new GraphAuthoringCapabilityId(node.capability),
                    document.DomainId,
                    document.DocumentRoleId);
            if (!capability.TryGetField(
                    new GraphAuthoringFieldId(fieldId),
                    out GraphAuthoringFieldDescriptor field))
            {
                throw new InvalidOperationException(
                    $"Pose clipboard capability '{node.capability}' does not declare field '{fieldId}'.");
            }
            return CharacterPoseAuthoringPayloadCodec.DecodeValue(
                field,
                node.properties[fieldId],
                expectedType,
                (_, token, assetType) =>
                    DecodeAsset(token, assetType));
        }

        static JToken EncodeAsset(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(path) ||
                string.IsNullOrWhiteSpace(guid))
            {
                throw new InvalidOperationException(
                    $"Pose clipboard asset '{asset.name}' is not persistent.");
            }
            return new JObject
            {
                ["assetPath"] = path,
                ["assetGuid"] = guid
            };
        }

        static UnityEngine.Object DecodeAsset(
            JToken token,
            Type expectedType)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;
            string path = token.Value<string>("assetPath");
            string guid = token.Value<string>("assetGuid");
            if (string.IsNullOrWhiteSpace(path) ||
                string.IsNullOrWhiteSpace(guid) ||
                !string.Equals(
                    AssetDatabase.AssetPathToGUID(path),
                    guid,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pose clipboard asset reference is not exact.");
            }
            UnityEngine.Object asset =
                AssetDatabase.LoadAssetAtPath(path, expectedType);
            return asset
                ? asset
                : throw new InvalidOperationException(
                    $"Pose clipboard asset '{path}' is missing or has the wrong type.");
        }

        static ClipboardPayload Parse(string payload)
        {
            JObject root = JObject.Parse(payload ??
                                         throw new ArgumentNullException(
                                             nameof(payload)));
            RequireProperties(root, "schema", "nodes", "edges");
            foreach (JObject node in root["nodes"]?.OfType<JObject>() ??
                                     Enumerable.Empty<JObject>())
            {
                RequireProperties(
                    node,
                    "id",
                    "capability",
                    "name",
                    "x",
                    "y",
                    "properties",
                    "dynamicPorts");
                foreach (JObject port in
                         node["dynamicPorts"]?.OfType<JObject>() ??
                         Enumerable.Empty<JObject>())
                {
                    RequireProperties(
                        port,
                        "id",
                        "name",
                        "valueType",
                        "direction",
                        "required",
                        "order",
                        "interfacePortId");
                }
            }
            foreach (JObject edge in root["edges"]?.OfType<JObject>() ??
                                     Enumerable.Empty<JObject>())
            {
                RequireProperties(
                    edge,
                    "id",
                    "sourceNode",
                    "sourcePort",
                    "targetNode",
                    "targetPort");
            }
            return root.ToObject<ClipboardPayload>();
        }

        static void RequireProperties(
            JObject value,
            params string[] allowed)
        {
            HashSet<string> names = allowed.ToHashSet(
                StringComparer.Ordinal);
            string unknown = value.Properties()
                .Select(property => property.Name)
                .FirstOrDefault(name => !names.Contains(name));
            if (!string.IsNullOrEmpty(unknown))
            {
                throw new InvalidOperationException(
                    $"Pose clipboard contains unknown field '{unknown}'.");
            }
        }

        static CharacterTypedPoseGraphDocument RequireDocument(
            IGraphAuthoringDocumentProjection document) =>
            document as CharacterTypedPoseGraphDocument ??
            throw new ArgumentException(
                "Pose clipboard requires a typed Pose Graph document.",
                nameof(document));

        static CharacterPoseNodeKind ResolveKind(
            string capability,
            GraphAuthoringDocumentRoleId role)
        {
            foreach (CharacterPoseNodeKind kind in
                     Enum.GetValues(typeof(CharacterPoseNodeKind)))
            {
                if (!string.Equals(
                        CharacterPoseGraphAuthoringCapabilities.Get(kind)
                            .Value,
                        capability,
                        StringComparison.Ordinal))
                    continue;
                CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                    new GraphAuthoringCapabilityId(capability),
                    CharacterPoseGraphAuthoringCapabilities.Domain,
                    role);
                return kind;
            }
            throw new InvalidOperationException(
                $"Unknown Pose clipboard capability '{capability}'.");
        }

        static void RequireCopyable(CharacterTypedPoseNode node) =>
            RequireCopyable(node.Kind);

        static void RequireCopyable(CharacterPoseNodeKind kind)
        {
            if (kind == CharacterPoseNodeKind.GraphInput ||
                kind == CharacterPoseNodeKind.GraphOutput ||
                kind == CharacterPoseNodeKind.OutputPose ||
                kind == CharacterPoseNodeKind.PoseStateMachine)
            {
                throw new InvalidOperationException(
                    $"Pose capability '{kind}' owns a system or child-document boundary and cannot be copied.");
            }
        }

        static CharacterPosePortKind PortKind(string value) => value switch
        {
            "pose.local" => CharacterPosePortKind.LocalPose,
            "pose.component" => CharacterPosePortKind.ComponentPose,
            "pose.parameter" => CharacterPosePortKind.Parameter,
            "pose.discontinuity" =>
                CharacterPosePortKind.PoseDiscontinuity,
            "pose.action-playback" =>
                CharacterPosePortKind.ActionPlayback,
            _ => throw new InvalidOperationException(
                $"Unknown Pose clipboard port value type '{value}'.")
        };

        static string RemapPort(
            string node,
            string port,
            IReadOnlyDictionary<string, string> ports) =>
            ports.TryGetValue(PortKey(node, port), out string mapped)
                ? mapped
                : port;

        static string PortKey(string node, string port) =>
            node + "\0" + port;

        static string Require(string value, string label) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException(
                    $"Pose clipboard {label} is missing.")
                : value;

        [Serializable]
        sealed class ClipboardPayload
        {
            public string schema;
            public List<ClipboardNode> nodes = new List<ClipboardNode>();
            public List<ClipboardEdge> edges = new List<ClipboardEdge>();
        }

        [Serializable]
        sealed class ClipboardNode
        {
            public string id;
            public string capability;
            public string name;
            public float x;
            public float y;
            public JObject properties = new JObject();
            public List<ClipboardPort> dynamicPorts =
                new List<ClipboardPort>();
        }

        [Serializable]
        sealed class ClipboardPort
        {
            public string id;
            public string name;
            public string valueType;
            public string direction;
            public bool required;
            public int order;
            public string interfacePortId;
        }

        [Serializable]
        sealed class ClipboardEdge
        {
            public string id;
            public string sourceNode;
            public string sourcePort;
            public string targetNode;
            public string targetPort;
        }
    }
}
