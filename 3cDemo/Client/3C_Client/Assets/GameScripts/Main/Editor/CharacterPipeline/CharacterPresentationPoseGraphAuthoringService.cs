using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterPresentationPoseGraphAuthoringService
    {
        public static void ConfigureParameters(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseParameterDeclaration[] parameters)
        {
            if (!owner)
                throw new ArgumentNullException(nameof(owner));
            Undo.RecordObject(owner, "Configure Pose Graph Parameters");
            CharacterPoseGraphData graph = owner.Graph;
            graph.Configure(
                parameters ?? Array.Empty<CharacterPoseParameterDeclaration>(),
                graph.Nodes.ToArray(),
                graph.Edges.ToArray());
            EditorUtility.SetDirty(owner);
        }

        public static CharacterPoseNodeDefinition CreateNode(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            CharacterPoseNodeKind kind,
            Vector2 position)
        {
            RequireOwner(owner, graph);
            Undo.RecordObject(owner, "Create Pose Graph Node");
            CharacterPoseNodeDefinition node = CreateNodeDefinition(graph, kind, position);
            graph.Configure(
                graph.Parameters.ToArray(),
                graph.Nodes.Concat(new[] { node }).ToArray(),
                graph.Edges.ToArray());
            EditorUtility.SetDirty(owner);
            return node;
        }

        public static void DeleteSelection(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            IReadOnlyCollection<PoseNodeId> nodeIds,
            IReadOnlyCollection<string> edgeIds)
        {
            RequireOwner(owner, graph);
            var removedNodes = new HashSet<PoseNodeId>(nodeIds ?? Array.Empty<PoseNodeId>());
            var removedEdges = new HashSet<string>(edgeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (removedNodes.Count == 0 && removedEdges.Count == 0)
                return;
            Undo.RecordObject(owner, "Delete Pose Graph Selection");
            graph.Configure(
                graph.Parameters.ToArray(),
                graph.Nodes.Where(node => node != null && !removedNodes.Contains(node.NodeId)).ToArray(),
                graph.Edges.Where(edge => edge != null &&
                    !removedEdges.Contains(edge.EdgeId) &&
                    !removedNodes.Contains(edge.SourceNodeId) &&
                    !removedNodes.Contains(edge.TargetNodeId)).ToArray());
            EditorUtility.SetDirty(owner);
        }

        public static CharacterPoseEdge Connect(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            PoseNodeId sourceNodeId,
            PosePortId sourcePortId,
            PoseNodeId targetNodeId,
            PosePortId targetPortId)
        {
            RequireOwner(owner, graph);
            CharacterPoseNodeDefinition sourceNode = RequireNode(graph, sourceNodeId);
            CharacterPoseNodeDefinition targetNode = RequireNode(graph, targetNodeId);
            CharacterPosePortDefinition sourcePort = RequirePort(sourceNode, sourcePortId);
            CharacterPosePortDefinition targetPort = RequirePort(targetNode, targetPortId);
            if (sourceNodeId.Equals(targetNodeId) ||
                sourcePort.Direction != CharacterPosePortDirection.Output ||
                targetPort.Direction != CharacterPosePortDirection.Input ||
                sourcePort.Kind != targetPort.Kind)
                throw new InvalidOperationException("Pose Graph ports are not compatible.");
            if (graph.Edges.Any(edge => edge != null && edge.TargetNodeId.Equals(targetNodeId) && edge.TargetPortId.Equals(targetPortId)))
                throw new InvalidOperationException($"Pose input '{targetNodeId}/{targetPortId}' already has an incoming edge.");
            if (WouldCreateCycle(graph, sourceNodeId, targetNodeId))
                throw new InvalidOperationException($"Pose edge '{sourceNodeId}' -> '{targetNodeId}' would create a graph cycle.");
            Undo.RecordObject(owner, "Connect Pose Graph Ports");
            var edge = new CharacterPoseEdge(Guid.NewGuid().ToString("N"), sourceNodeId, sourcePortId, targetNodeId, targetPortId);
            graph.Configure(
                graph.Parameters.ToArray(),
                graph.Nodes.ToArray(),
                graph.Edges.Concat(new[] { edge }).ToArray());
            EditorUtility.SetDirty(owner);
            return edge;
        }

        public static void MoveNodes(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            IReadOnlyDictionary<PoseNodeId, Vector2> positions)
        {
            RequireOwner(owner, graph);
            if (positions == null || positions.Count == 0)
                return;
            Undo.RecordObject(owner, "Move Pose Graph Nodes");
            CharacterPoseNodeDefinition[] nodes = graph.Nodes.Select(node =>
            {
                if (node == null || !positions.TryGetValue(node.NodeId, out Vector2 position))
                    return node;
                return CloneNode(node, node.NodeId, node.Ports.ToArray(), position, node.Subgraph);
            }).ToArray();
            graph.Configure(graph.Parameters.ToArray(), nodes, graph.Edges.ToArray());
            EditorUtility.SetDirty(owner);
        }

        public static void RenameNode(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            PoseNodeId nodeId,
            string displayName)
        {
            RequireOwner(owner, graph);
            CharacterPoseNodeDefinition current = RequireNode(graph, nodeId);
            Undo.RecordObject(owner, "Rename Pose Graph Node");
            ReplaceNode(graph, current, CloneNode(current, current.NodeId, current.Ports.ToArray(), current.Position, current.Subgraph, displayName));
            EditorUtility.SetDirty(owner);
        }

        public static void ConfigureNode(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            PoseNodeId nodeId,
            ThirdPersonSimulation.AnimationChannelId animationChannelId,
            string programProducerId,
            PoseParameterId parameterId,
            AnimationSelectionAvailabilityPolicy selectionAvailability,
            CharacterAnimationBlendSpaceInputRangePolicy blendSpaceInputRangePolicy,
            CharacterAnimationBlendPolicy blendPolicy,
            CharacterPoseInertializationPolicy inertializationPolicy,
            CharacterAnimationBoneMaskAsset boneMask,
            float weight,
            CharacterPoseParameterPolicy[] parameterPolicies,
            string additiveReferencePoseId,
            AdditiveReferenceSpace additiveReferenceSpace,
            AdditiveScalePolicy additiveScalePolicy,
            AnimationBoneId boneId,
            ModifyBoneReferenceSpace modifyBoneReferenceSpace,
            ModifyBoneOperationMask modifyBoneOperations,
            Vector3 modifyPosition,
            Vector3 modifyRotationEuler,
            Vector3 modifyScale,
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRigCalibration footPlacementCalibration)
        {
            RequireOwner(owner, graph);
            CharacterPoseNodeDefinition current = RequireNode(graph, nodeId);
            Undo.RecordObject(owner, "Configure Pose Graph Node");
            CharacterPoseNodeDefinition replacement = new CharacterPoseNodeDefinition(
                nodeId: current.NodeId,
                kind: current.Kind,
                displayName: current.DisplayName,
                position: current.Position,
                ports: current.Ports.ToArray(),
                animationChannelId: animationChannelId,
                programProducerId: programProducerId,
                parameterId: parameterId,
                selectionAvailability: selectionAvailability,
                blendSpaceInputRangePolicy: blendSpaceInputRangePolicy,
                blendPolicy: blendPolicy,
                inertializationPolicy: inertializationPolicy,
                boneMask: boneMask,
                weight: weight,
                parameterPolicies: parameterPolicies ?? Array.Empty<CharacterPoseParameterPolicy>(),
                additiveReferencePoseId: additiveReferencePoseId,
                additiveReferenceSpace: additiveReferenceSpace,
                additiveScalePolicy: additiveScalePolicy,
                boneId: boneId,
                modifyBoneReferenceSpace: modifyBoneReferenceSpace,
                modifyBoneOperations: modifyBoneOperations,
                modifyPosition: modifyPosition,
                modifyRotationEuler: modifyRotationEuler,
                modifyScale: modifyScale,
                footPlacementProfile: footPlacementProfile,
                footPlacementCalibration: footPlacementCalibration,
                subgraph: current.Subgraph);
            ReplaceNode(graph, current, replacement);
            EditorUtility.SetDirty(owner);
        }

        public static void CreateInline(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            PoseNodeId nodeId)
        {
            RequireOwner(owner, graph);
            CharacterPoseNodeDefinition current = RequireSubgraphNode(graph, nodeId);
            if (current.Subgraph != null && current.Subgraph.IsExclusive)
                throw new InvalidOperationException($"PoseSubgraph '{nodeId}' already owns a graph reference.");
            Undo.RecordObject(owner, "Create Inline Pose Subgraph");
            CharacterPoseSubgraphReference reference = CreateDefaultSubgraphReference(graph.Parameters);
            CharacterPoseNodeDefinition replacement = CloneNode(
                current,
                current.NodeId,
                CreateCallSitePorts(reference.Inline),
                current.Position,
                reference);
            ReplaceNode(graph, current, replacement);
            EditorUtility.SetDirty(owner);
        }

        public static CharacterPresentationPoseGraphAsset ExtractShared(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            PoseNodeId nodeId,
            string assetPath)
        {
            RequireOwner(owner, graph);
            CharacterPoseNodeDefinition current = RequireSubgraphNode(graph, nodeId);
            if (current.Subgraph == null || !current.Subgraph.HasInline || current.Subgraph.HasShared)
                throw new InvalidOperationException($"PoseSubgraph '{nodeId}' does not own an exclusive inline graph.");
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !string.Equals(Path.GetExtension(assetPath), ".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Shared Pose Graph path must be an Assets/... .asset path.", nameof(assetPath));
            if (AssetDatabase.LoadMainAssetAtPath(assetPath))
                throw new InvalidOperationException($"Asset '{assetPath}' already exists.");

            Undo.RecordObject(owner, "Extract Shared Pose Subgraph");
            CharacterPresentationPoseGraphAsset shared = ScriptableObject.CreateInstance<CharacterPresentationPoseGraphAsset>();
            shared.SetGraph(current.Subgraph.Inline);
            AssetDatabase.CreateAsset(shared, assetPath);
            Undo.RegisterCreatedObjectUndo(shared, "Extract Shared Pose Subgraph");
            current.Subgraph.UseShared(shared);
            graph.Touch();
            EditorUtility.SetDirty(owner);
            EditorUtility.SetDirty(shared);
            return shared;
        }

        public static void UseShared(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            PoseNodeId nodeId,
            CharacterPresentationPoseGraphAsset shared)
        {
            RequireOwner(owner, graph);
            if (!shared)
                throw new ArgumentNullException(nameof(shared));
            if (ReferenceEquals(shared, owner) || ReferenceEquals(shared.Graph, graph))
                throw new InvalidOperationException("A PoseSubgraph cannot reference its owning Pose Graph asset.");
            CharacterPoseNodeDefinition current = RequireSubgraphNode(graph, nodeId);
            Undo.RecordObject(owner, "Use Shared Pose Subgraph");
            var reference = new CharacterPoseSubgraphReference();
            reference.UseShared(shared);
            CharacterPoseNodeDefinition replacement = CloneNode(
                current,
                current.NodeId,
                CreateCallSitePorts(shared.Graph),
                current.Position,
                reference);
            ReplaceNode(graph, current, replacement);
            EditorUtility.SetDirty(owner);
        }

        public static void ClearShared(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            PoseNodeId nodeId)
        {
            RequireOwner(owner, graph);
            CharacterPoseNodeDefinition current = RequireSubgraphNode(graph, nodeId);
            if (current.Subgraph == null || !current.Subgraph.HasShared || current.Subgraph.HasInline)
                throw new InvalidOperationException($"PoseSubgraph '{nodeId}' does not own an exclusive shared graph reference.");
            Undo.RecordObject(owner, "Clear Shared Pose Subgraph");
            CharacterPoseNodeDefinition replacement = CloneNode(
                current,
                current.NodeId,
                Array.Empty<CharacterPosePortDefinition>(),
                current.Position,
                new CharacterPoseSubgraphReference());
            ReplaceNode(graph, current, replacement);
            EditorUtility.SetDirty(owner);
        }

        public static CharacterPoseNodeDefinition[] CloneNodesWithNewIdentities(
            IReadOnlyList<CharacterPoseNodeDefinition> source,
            Vector2 offset,
            out Dictionary<PoseNodeId, PoseNodeId> nodeMap,
            out Dictionary<string, PosePortId> portMap)
        {
            return CloneNodesWithNewIdentities(
                source,
                offset,
                new Dictionary<PoseInterfacePortId, PoseInterfacePortId>(),
                out nodeMap,
                out portMap);
        }

        static CharacterPoseNodeDefinition[] CloneNodesWithNewIdentities(
            IReadOnlyList<CharacterPoseNodeDefinition> source,
            Vector2 offset,
            IReadOnlyDictionary<PoseInterfacePortId, PoseInterfacePortId> graphInterfaceMap,
            out Dictionary<PoseNodeId, PoseNodeId> nodeMap,
            out Dictionary<string, PosePortId> portMap)
        {
            nodeMap = new Dictionary<PoseNodeId, PoseNodeId>();
            portMap = new Dictionary<string, PosePortId>(StringComparer.Ordinal);
            var result = new CharacterPoseNodeDefinition[source?.Count ?? 0];
            for (int nodeIndex = 0; nodeIndex < result.Length; nodeIndex++)
            {
                CharacterPoseNodeDefinition node = source[nodeIndex] ?? throw new InvalidOperationException("Pose Graph clipboard contains an empty node.");
                PoseNodeId clonedNodeId = new PoseNodeId(Guid.NewGuid().ToString("N"));
                nodeMap.Add(node.NodeId, clonedNodeId);
                CharacterPoseSubgraphReference subgraph = CloneSubgraphReference(
                    node.Subgraph,
                    out Dictionary<PoseInterfacePortId, PoseInterfacePortId> ownedInterfaceMap);
                CharacterPosePortDefinition[] ports = new CharacterPosePortDefinition[node.Ports.Count];
                for (int portIndex = 0; portIndex < ports.Length; portIndex++)
                {
                    CharacterPosePortDefinition port = node.Ports[portIndex];
                    PosePortId clonedPortId = new PosePortId(Guid.NewGuid().ToString("N"));
                    portMap.Add(PortKey(node.NodeId, port.PortId), clonedPortId);
                    PoseInterfacePortId interfacePortId = port.InterfacePortId;
                    if (interfacePortId.IsValid && ownedInterfaceMap.TryGetValue(interfacePortId, out PoseInterfacePortId ownedRemap))
                        interfacePortId = ownedRemap;
                    else if (interfacePortId.IsValid && graphInterfaceMap.TryGetValue(interfacePortId, out PoseInterfacePortId graphRemap))
                        interfacePortId = graphRemap;
                    ports[portIndex] = new CharacterPosePortDefinition(
                        clonedPortId,
                        port.Name,
                        port.Kind,
                        port.Direction,
                        port.Required,
                        interfacePortId);
                }
                result[nodeIndex] = CloneNode(node, clonedNodeId, ports, node.Position + offset, subgraph);
            }
            return result;
        }

        public static CharacterPoseEdge[] CloneInternalEdges(
            IReadOnlyList<CharacterPoseEdge> source,
            IReadOnlyDictionary<PoseNodeId, PoseNodeId> nodeMap,
            IReadOnlyDictionary<string, PosePortId> portMap)
        {
            var result = new List<CharacterPoseEdge>();
            for (int i = 0; i < (source?.Count ?? 0); i++)
            {
                CharacterPoseEdge edge = source[i];
                if (edge == null || !nodeMap.TryGetValue(edge.SourceNodeId, out PoseNodeId sourceNode) ||
                    !nodeMap.TryGetValue(edge.TargetNodeId, out PoseNodeId targetNode) ||
                    !portMap.TryGetValue(PortKey(edge.SourceNodeId, edge.SourcePortId), out PosePortId sourcePort) ||
                    !portMap.TryGetValue(PortKey(edge.TargetNodeId, edge.TargetPortId), out PosePortId targetPort))
                    continue;
                result.Add(new CharacterPoseEdge(Guid.NewGuid().ToString("N"), sourceNode, sourcePort, targetNode, targetPort));
            }
            return result.ToArray();
        }

        public static void AppendClonedSelection(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            IReadOnlyList<CharacterPoseNodeDefinition> nodes,
            IReadOnlyList<CharacterPoseEdge> edges)
        {
            RequireOwner(owner, graph);
            Undo.RecordObject(owner, "Paste Pose Graph Elements");
            graph.Configure(
                graph.Parameters.ToArray(),
                graph.Nodes.Concat(nodes ?? Array.Empty<CharacterPoseNodeDefinition>()).ToArray(),
                graph.Edges.Concat(edges ?? Array.Empty<CharacterPoseEdge>()).ToArray());
            EditorUtility.SetDirty(owner);
        }

        static CharacterPoseNodeDefinition CreateNodeDefinition(
            CharacterPoseGraphData graph,
            CharacterPoseNodeKind kind,
            Vector2 position)
        {
            PoseNodeId nodeId = new PoseNodeId(Guid.NewGuid().ToString("N"));
            CharacterPosePortDefinition[] ports;
            CharacterPoseSubgraphReference subgraph = null;
            switch (kind)
            {
                case CharacterPoseNodeKind.AnimationSelectionInput:
                case CharacterPoseNodeKind.MotionMatchingSelectionInput:
                    ports = new[] { Port("Selection", CharacterPosePortKind.AnimationSelection, CharacterPosePortDirection.Output) };
                    break;
                case CharacterPoseNodeKind.ProgramParameterInput:
                    ports = new[] { Port("Parameter", CharacterPosePortKind.Parameter, CharacterPosePortDirection.Output) };
                    break;
                case CharacterPoseNodeKind.MarkerSync:
                    ports = new[]
                    {
                        Port("Selection", CharacterPosePortKind.AnimationSelection, CharacterPosePortDirection.Input),
                        Port("Selection", CharacterPosePortKind.AnimationSelection, CharacterPosePortDirection.Output)
                    };
                    break;
                case CharacterPoseNodeKind.SelectedPosePlayer:
                case CharacterPoseNodeKind.BlendStack:
                    ports = new[]
                    {
                        Port("Selection", CharacterPosePortKind.AnimationSelection, CharacterPosePortDirection.Input),
                        Port("Pose", CharacterPosePortKind.Pose, CharacterPosePortDirection.Output)
                    };
                    break;
                case CharacterPoseNodeKind.BlendSpacePlayer:
                    ports = new[]
                    {
                        Port("Selection", CharacterPosePortKind.AnimationSelection, CharacterPosePortDirection.Input),
                        Port("X", CharacterPosePortKind.Parameter, CharacterPosePortDirection.Input),
                        OptionalPort("Y", CharacterPosePortKind.Parameter, CharacterPosePortDirection.Input),
                        Port("Pose", CharacterPosePortKind.Pose, CharacterPosePortDirection.Output),
                        Port("Discontinuity", CharacterPosePortKind.PoseDiscontinuity, CharacterPosePortDirection.Output)
                    };
                    break;
                case CharacterPoseNodeKind.Inertialization:
                    ports = PoseUnaryPorts();
                    break;
                case CharacterPoseNodeKind.BlendPose:
                case CharacterPoseNodeKind.LayeredBoneBlend:
                case CharacterPoseNodeKind.AdditivePose:
                    ports = PoseBinaryWithParameterPorts("Base", kind == CharacterPoseNodeKind.AdditivePose ? "Additive" : "Overlay");
                    break;
                case CharacterPoseNodeKind.PoseParameterResolve:
                    ports = PoseBinaryPorts("Base Pose", "Parameter Source Pose");
                    break;
                case CharacterPoseNodeKind.ModifyBone:
                case CharacterPoseNodeKind.FootPlacement:
                    ports = PoseUnaryWithParameterPorts();
                    break;
                case CharacterPoseNodeKind.PoseSubgraph:
                    subgraph = CreateDefaultSubgraphReference(graph.Parameters);
                    ports = CreateCallSitePorts(subgraph.Inline);
                    break;
                case CharacterPoseNodeKind.OutputPose:
                    ports = new[] { Port("Pose", CharacterPosePortDirection.Input) };
                    break;
                default:
                    throw new InvalidOperationException($"Pose node kind '{kind}' is a graph boundary and cannot be created directly.");
            }
            return new CharacterPoseNodeDefinition(
                nodeId: nodeId,
                kind: kind,
                displayName: kind.ToString(),
                position: position,
                ports: ports,
                parameterId: graph.Parameters.FirstOrDefault()?.ParameterId ?? default,
                parameterPolicies: CreatePolicies(graph.Parameters),
                subgraph: subgraph);
        }

        static CharacterPoseSubgraphReference CreateDefaultSubgraphReference(
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters)
        {
            PoseInterfacePortId inputInterface = new PoseInterfacePortId(Guid.NewGuid().ToString("N"));
            PoseInterfacePortId outputInterface = new PoseInterfacePortId(Guid.NewGuid().ToString("N"));
            CharacterPosePortDefinition inputBoundaryPort = Port("Input Pose", CharacterPosePortDirection.Output, inputInterface);
            CharacterPosePortDefinition outputBoundaryPort = Port("Output Pose", CharacterPosePortDirection.Input, outputInterface);
            CharacterPoseNodeDefinition input = new CharacterPoseNodeDefinition(
                new PoseNodeId(Guid.NewGuid().ToString("N")),
                CharacterPoseNodeKind.GraphInput,
                "Graph Input",
                new Vector2(-240f, 0f),
                new[] { inputBoundaryPort });
            CharacterPoseNodeDefinition output = new CharacterPoseNodeDefinition(
                new PoseNodeId(Guid.NewGuid().ToString("N")),
                CharacterPoseNodeKind.GraphOutput,
                "Graph Output",
                new Vector2(240f, 0f),
                new[] { outputBoundaryPort });
            var graph = new CharacterPoseGraphData();
            graph.Configure(
                parameters?.ToArray() ?? Array.Empty<CharacterPoseParameterDeclaration>(),
                new[] { input, output },
                new[]
                {
                    new CharacterPoseEdge(
                        Guid.NewGuid().ToString("N"),
                        input.NodeId,
                        inputBoundaryPort.PortId,
                        output.NodeId,
                        outputBoundaryPort.PortId)
                });
            var reference = new CharacterPoseSubgraphReference();
            reference.CreateInline(graph);
            return reference;
        }

        static CharacterPosePortDefinition[] CreateCallSitePorts(CharacterPoseGraphData graph)
        {
            CharacterPoseNodeDefinition input = graph.Nodes.Single(node => node.Kind == CharacterPoseNodeKind.GraphInput);
            CharacterPoseNodeDefinition output = graph.Nodes.Single(node => node.Kind == CharacterPoseNodeKind.GraphOutput);
            return input.Ports.Select(port => new CharacterPosePortDefinition(
                    new PosePortId(Guid.NewGuid().ToString("N")),
                    port.Name,
                    port.Kind,
                    CharacterPosePortDirection.Input,
                    port.Required,
                    port.InterfacePortId))
                .Concat(output.Ports.Select(port => new CharacterPosePortDefinition(
                    new PosePortId(Guid.NewGuid().ToString("N")),
                    port.Name,
                    port.Kind,
                    CharacterPosePortDirection.Output,
                    port.Required,
                    port.InterfacePortId)))
                .ToArray();
        }

        static CharacterPoseSubgraphReference CloneSubgraphReference(
            CharacterPoseSubgraphReference source,
            out Dictionary<PoseInterfacePortId, PoseInterfacePortId> interfaceMap)
        {
            interfaceMap = new Dictionary<PoseInterfacePortId, PoseInterfacePortId>();
            if (source == null)
                return null;
            var clone = new CharacterPoseSubgraphReference();
            if (source.HasShared)
            {
                clone.UseShared(source.Shared);
                return clone;
            }
            if (!source.HasInline)
                return clone;
            CharacterPoseGraphData graph = source.Inline;
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[nodeIndex];
                if (node == null || (node.Kind != CharacterPoseNodeKind.GraphInput && node.Kind != CharacterPoseNodeKind.GraphOutput))
                    continue;
                for (int portIndex = 0; portIndex < node.Ports.Count; portIndex++)
                {
                    PoseInterfacePortId interfacePortId = node.Ports[portIndex].InterfacePortId;
                    if (interfacePortId.IsValid && !interfaceMap.ContainsKey(interfacePortId))
                        interfaceMap.Add(interfacePortId, new PoseInterfacePortId(Guid.NewGuid().ToString("N")));
                }
            }
            CharacterPoseNodeDefinition[] nodes = CloneNodesWithNewIdentities(
                graph.Nodes.ToArray(),
                Vector2.zero,
                interfaceMap,
                out Dictionary<PoseNodeId, PoseNodeId> nodeMap,
                out Dictionary<string, PosePortId> portMap);
            CharacterPoseEdge[] edges = CloneInternalEdges(graph.Edges.ToArray(), nodeMap, portMap);
            var inline = new CharacterPoseGraphData();
            inline.Configure(graph.Parameters.ToArray(), nodes, edges);
            clone.CreateInline(inline);
            return clone;
        }

        static CharacterPoseNodeDefinition CloneNode(
            CharacterPoseNodeDefinition source,
            PoseNodeId nodeId,
            CharacterPosePortDefinition[] ports,
            Vector2 position,
            CharacterPoseSubgraphReference subgraph,
            string displayName = null)
        {
            return new CharacterPoseNodeDefinition(
                nodeId: nodeId,
                kind: source.Kind,
                displayName: displayName ?? source.DisplayName,
                position: position,
                ports: ports,
                animationChannelId: source.AnimationChannelId,
                programProducerId: source.ProgramProducerId,
                parameterId: source.ParameterId,
                selectionAvailability: source.SelectionAvailability,
                blendSpaceInputRangePolicy: source.BlendSpaceInputRangePolicy,
                blendPolicy: source.BlendPolicy,
                inertializationPolicy: source.InertializationPolicy,
                boneMask: source.BoneMask,
                weight: source.Weight,
                parameterPolicies: source.ParameterPolicies.ToArray(),
                additiveReferencePoseId: source.AdditiveReferencePoseId,
                additiveReferenceSpace: source.AdditiveReferenceSpace,
                additiveScalePolicy: source.AdditiveScalePolicy,
                boneId: source.BoneId,
                modifyBoneReferenceSpace: source.ModifyBoneReferenceSpace,
                modifyBoneOperations: source.ModifyBoneOperations,
                modifyPosition: source.ModifyPosition,
                modifyRotationEuler: source.ModifyRotation.eulerAngles,
                modifyScale: source.ModifyScale,
                footPlacementProfile: source.FootPlacementProfile,
                footPlacementCalibration: source.FootPlacementCalibration,
                subgraph: subgraph);
        }

        static CharacterPosePortDefinition[] PoseBinaryPorts(string first, string second)
        {
            return new[]
            {
                Port(first, CharacterPosePortDirection.Input),
                Port(second, CharacterPosePortDirection.Input),
                Port("Pose", CharacterPosePortDirection.Output)
            };
        }

        static CharacterPosePortDefinition[] PoseBinaryWithParameterPorts(string first, string second)
        {
            return PoseBinaryPorts(first, second)
                .Concat(new[] { Port("Weight", CharacterPosePortKind.Parameter, CharacterPosePortDirection.Input) })
                .ToArray();
        }

        static CharacterPosePortDefinition[] PoseUnaryPorts()
        {
            return new[]
            {
                Port("Pose", CharacterPosePortKind.Pose, CharacterPosePortDirection.Input),
                Port("Pose", CharacterPosePortKind.Pose, CharacterPosePortDirection.Output)
            };
        }

        static CharacterPosePortDefinition[] PoseUnaryWithParameterPorts()
        {
            return PoseUnaryPorts()
                .Concat(new[] { Port("Weight", CharacterPosePortKind.Parameter, CharacterPosePortDirection.Input) })
                .ToArray();
        }

        static CharacterPosePortDefinition Port(
            string name,
            CharacterPosePortDirection direction,
            PoseInterfacePortId interfacePortId = default)
        {
            return Port(name, CharacterPosePortKind.Pose, direction, interfacePortId);
        }

        static CharacterPosePortDefinition Port(
            string name,
            CharacterPosePortKind kind,
            CharacterPosePortDirection direction,
            PoseInterfacePortId interfacePortId = default)
        {
            return new CharacterPosePortDefinition(
                new PosePortId(Guid.NewGuid().ToString("N")),
                name,
                kind,
                direction,
                true,
                interfacePortId);
        }

        static CharacterPosePortDefinition OptionalPort(
            string name,
            CharacterPosePortKind kind,
            CharacterPosePortDirection direction)
        {
            return new CharacterPosePortDefinition(
                new PosePortId(Guid.NewGuid().ToString("N")),
                name,
                kind,
                direction,
                false);
        }

        static CharacterPoseParameterPolicy[] CreatePolicies(IReadOnlyList<CharacterPoseParameterDeclaration> parameters)
        {
            return parameters?.Select(parameter => new CharacterPoseParameterPolicy(
                    parameter.ParameterId,
                    PoseParameterResolvePolicy.Weighted))
                .ToArray() ?? Array.Empty<CharacterPoseParameterPolicy>();
        }

        static void ReplaceNode(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition current,
            CharacterPoseNodeDefinition replacement)
        {
            CharacterPoseNodeDefinition[] nodes = graph.Nodes.ToArray();
            int index = Array.IndexOf(nodes, current);
            if (index < 0)
                throw new InvalidOperationException($"Pose node '{current.NodeId}' is not owned by graph '{graph.GraphId}'.");
            nodes[index] = replacement;
            graph.Configure(graph.Parameters.ToArray(), nodes, graph.Edges.ToArray());
        }

        static CharacterPoseNodeDefinition RequireNode(CharacterPoseGraphData graph, PoseNodeId nodeId)
        {
            return graph.Nodes.SingleOrDefault(node => node != null && node.NodeId.Equals(nodeId))
                ?? throw new InvalidOperationException($"Pose node '{nodeId}' does not exist in graph '{graph.GraphId}'.");
        }

        static CharacterPoseNodeDefinition RequireSubgraphNode(CharacterPoseGraphData graph, PoseNodeId nodeId)
        {
            CharacterPoseNodeDefinition node = RequireNode(graph, nodeId);
            if (node.Kind != CharacterPoseNodeKind.PoseSubgraph)
                throw new InvalidOperationException($"Pose node '{nodeId}' is not a PoseSubgraph.");
            return node;
        }

        static CharacterPosePortDefinition RequirePort(CharacterPoseNodeDefinition node, PosePortId portId)
        {
            return node.Ports.SingleOrDefault(port => port != null && port.PortId.Equals(portId))
                ?? throw new InvalidOperationException($"Pose port '{node.NodeId}/{portId}' does not exist.");
        }

        static bool WouldCreateCycle(CharacterPoseGraphData graph, PoseNodeId source, PoseNodeId target)
        {
            var stack = new Stack<PoseNodeId>();
            var visited = new HashSet<PoseNodeId>();
            stack.Push(target);
            while (stack.Count > 0)
            {
                PoseNodeId current = stack.Pop();
                if (!visited.Add(current))
                    continue;
                if (current.Equals(source))
                    return true;
                for (int edgeIndex = 0; edgeIndex < graph.Edges.Count; edgeIndex++)
                {
                    CharacterPoseEdge edge = graph.Edges[edgeIndex];
                    if (edge != null && edge.SourceNodeId.Equals(current))
                        stack.Push(edge.TargetNodeId);
                }
            }
            return false;
        }

        static void RequireOwner(CharacterPresentationPoseGraphAsset owner, CharacterPoseGraphData graph)
        {
            if (!owner)
                throw new ArgumentNullException(nameof(owner));
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (!OwnsGraph(owner.Graph, graph, new HashSet<CharacterPoseGraphData>()))
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' is not owned by asset '{owner.name}'.");
        }

        static bool OwnsGraph(
            CharacterPoseGraphData current,
            CharacterPoseGraphData target,
            HashSet<CharacterPoseGraphData> visited)
        {
            if (current == null || !visited.Add(current))
                return false;
            if (ReferenceEquals(current, target))
                return true;
            for (int nodeIndex = 0; nodeIndex < current.Nodes.Count; nodeIndex++)
            {
                CharacterPoseSubgraphReference reference = current.Nodes[nodeIndex]?.Subgraph;
                if (reference != null && reference.HasInline && OwnsGraph(reference.Inline, target, visited))
                    return true;
            }
            return false;
        }

        static string PortKey(PoseNodeId nodeId, PosePortId portId) => nodeId.Value + ":" + portId.Value;
    }
}
