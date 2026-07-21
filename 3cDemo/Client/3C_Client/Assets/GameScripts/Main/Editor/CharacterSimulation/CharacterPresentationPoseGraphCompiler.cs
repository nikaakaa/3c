using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public static class CharacterPresentationPoseGraphCompiler
    {
        sealed class CompilationState
        {
            public CompilationState(
                CharacterAnimationRigDefinition rig,
                CharacterPresentationPoseSlotProgramEntry[] slots,
                Dictionary<PoseSlotId, int> slotIndices,
                CharacterPresentationPoseParameterProgramEntry[] parameters,
                Dictionary<PoseParameterId, int> parameterIndices)
            {
                Rig = rig;
                Slots = slots;
                SlotIndices = slotIndices;
                Parameters = parameters;
                ParameterIndices = parameterIndices;
            }

            public CharacterAnimationRigDefinition Rig { get; }
            public CharacterPresentationPoseSlotProgramEntry[] Slots { get; }
            public Dictionary<PoseSlotId, int> SlotIndices { get; }
            public CharacterPresentationPoseParameterProgramEntry[] Parameters { get; }
            public Dictionary<PoseParameterId, int> ParameterIndices { get; }
            public List<CharacterPresentationDenseBoneMask> Masks { get; } = new List<CharacterPresentationDenseBoneMask>();
            public Dictionary<string, int> MaskIndices { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
            public List<CharacterPresentationAdditiveReferenceDescriptor> AdditiveReferences { get; } = new List<CharacterPresentationAdditiveReferenceDescriptor>();
            public List<CharacterPresentationPoseOperation> Operations { get; } = new List<CharacterPresentationPoseOperation>();
            public List<CharacterPresentationPoseSourceMapEntry> SourceMap { get; } = new List<CharacterPresentationPoseSourceMapEntry>();
            public List<string> GraphDependencies { get; } = new List<string>();
            public int OutputOperationIndex { get; set; } = -1;
        }

        public static CharacterPresentationPoseProgram Compile(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableAnimationChannels,
            int contributionCapacityPerValue,
            List<string> errors)
        {
            if (contributionCapacityPerValue <= 0)
            {
                errors?.Add("Pose Graph contribution capacity must be positive.");
                return null;
            }
            CharacterPoseGraphValidationReport report = CharacterPresentationPoseGraphValidator.Validate(
                asset,
                rig,
                reachableAnimationChannels);
            if (!report.IsValid)
            {
                report.CopyMessagesTo(errors);
                return null;
            }
            try
            {
                return CompileValidated(asset.Graph, rig, contributionCapacityPerValue);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }

        static CharacterPresentationPoseProgram CompileValidated(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            int contributionCapacityPerValue)
        {
            CharacterPoseSlotDeclaration[] authoredSlots = graph.PoseSlots.OrderBy(value => value.PoseSlotId).ToArray();
            var slots = new CharacterPresentationPoseSlotProgramEntry[authoredSlots.Length];
            var slotIndex = new Dictionary<PoseSlotId, int>();
            for (int i = 0; i < authoredSlots.Length; i++)
            {
                CharacterPoseSlotDeclaration slot = authoredSlots[i];
                slots[i] = new CharacterPresentationPoseSlotProgramEntry(
                    i,
                    slot.PoseSlotId,
                    slot.AnimationChannelId,
                    slot.OutputPolicy);
                slotIndex.Add(slot.PoseSlotId, i);
            }

            CharacterPoseParameterDeclaration[] authoredParameters = graph.Parameters.OrderBy(value => value.ParameterId).ToArray();
            var parameters = new CharacterPresentationPoseParameterProgramEntry[authoredParameters.Length];
            var parameterIndex = new Dictionary<PoseParameterId, int>();
            for (int i = 0; i < authoredParameters.Length; i++)
            {
                parameters[i] = new CharacterPresentationPoseParameterProgramEntry(i, authoredParameters[i].ParameterId, authoredParameters[i].DefaultValue);
                parameterIndex.Add(authoredParameters[i].ParameterId, i);
            }

            var state = new CompilationState(rig, slots, slotIndex, parameters, parameterIndex);
            CompileGraph(
                state,
                graph,
                new Dictionary<PoseInterfacePortId, int>(),
                string.Empty,
                string.Empty,
                true);
            CharacterPresentationPoseOperation[] operations = state.Operations.ToArray();
            CharacterPresentationPoseSourceMapEntry[] sourceMap = state.SourceMap.ToArray();

            string hash = ComputeHash(
                graph,
                rig,
                slots,
                parameters,
                state.Masks,
                state.AdditiveReferences,
                operations,
                sourceMap,
                state.GraphDependencies);
            int poseWorkspace = operations.Length;
            int parameterWorkspace = Math.Max(parameters.Length, poseWorkspace * parameters.Length);
            int contributionWorkspace = checked(poseWorkspace * contributionCapacityPerValue);
            return new CharacterPresentationPoseProgram(
                graph.GraphId,
                graph.ContentRevision,
                hash,
                rig,
                slots,
                parameters,
                state.Masks.ToArray(),
                state.AdditiveReferences.ToArray(),
                operations,
                sourceMap,
                poseWorkspace,
                parameterWorkspace,
                contributionWorkspace,
                poseWorkspace,
                state.OutputOperationIndex);
        }

        static Dictionary<PoseInterfacePortId, int> CompileGraph(
            CompilationState state,
            CharacterPoseGraphData graph,
            IReadOnlyDictionary<PoseInterfacePortId, int> imports,
            string scope,
            string callChain,
            bool root)
        {
            state.GraphDependencies.Add($"{callChain}\0{graph.GraphId}\0{graph.ContentRevision}");
            List<CharacterPoseNodeDefinition> orderedNodes = TopologicalOrder(graph);
            Dictionary<string, CharacterPoseEdge> incoming = BuildIncoming(graph);
            var values = new Dictionary<string, int>(StringComparer.Ordinal);
            var exports = new Dictionary<PoseInterfacePortId, int>();
            for (int nodeIndex = 0; nodeIndex < orderedNodes.Count; nodeIndex++)
            {
                CharacterPoseNodeDefinition node = orderedNodes[nodeIndex];
                if (node.Kind == CharacterPoseNodeKind.GraphInput)
                {
                    BindGraphInputs(node, imports, scope, values);
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.GraphOutput)
                {
                    BindGraphOutputs(node, incoming, scope, values, exports);
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
                {
                    CompileSubgraphCall(state, graph, node, incoming, scope, callChain, values);
                    continue;
                }

                CharacterPoseOperationCode code = OperationCode(node.Kind);
                List<CharacterPosePortDefinition> inputPorts = node.Ports
                    .Where(value => value != null && value.Kind == CharacterPosePortKind.Pose &&
                                    value.Direction == CharacterPosePortDirection.Input)
                    .ToList();
                int inputA = inputPorts.Count > 0
                    ? RequireInputValue(node, inputPorts[0], incoming, scope, values)
                    : -1;
                int inputB = inputPorts.Count > 1
                    ? RequireInputValue(node, inputPorts[1], incoming, scope, values)
                    : -1;
                int operationIndex = state.Operations.Count;
                PoseNodeId scopedNodeId = ScopeNodeId(node.NodeId, scope);
                int compiledSlot = node.Kind == CharacterPoseNodeKind.PoseSlotInput
                    ? state.SlotIndices[node.PoseSlotId]
                    : -1;
                int mask = node.BoneMask
                    ? CompileMask(node.BoneMask, state.Rig, state.Masks, state.MaskIndices)
                    : -1;
                int additiveReference = node.Kind == CharacterPoseNodeKind.AdditivePose
                    ? CompileAdditiveReference(node, state.Rig, state.AdditiveReferences)
                    : -1;
                PoseParameterResolvePolicy[] policies = CompilePolicies(
                    node,
                    state.Parameters,
                    state.ParameterIndices);
                state.Operations.Add(new CharacterPresentationPoseOperation(
                    operationIndex,
                    code,
                    scopedNodeId,
                    operationIndex,
                    inputA,
                    inputB,
                    compiledSlot,
                    mask,
                    additiveReference,
                    node.Weight,
                    policies));
                state.SourceMap.Add(new CharacterPresentationPoseSourceMapEntry(
                    operationIndex,
                    graph.GraphId,
                    scopedNodeId,
                    callChain));
                BindOperationOutputs(node, scope, operationIndex, values);
                if (code == CharacterPoseOperationCode.OutputPose)
                {
                    if (!root || state.OutputOperationIndex >= 0)
                        throw new InvalidOperationException("Pose Program contains an invalid OutputPose boundary.");
                    state.OutputOperationIndex = operationIndex;
                }
            }
            return exports;
        }

        static void BindGraphInputs(
            CharacterPoseNodeDefinition node,
            IReadOnlyDictionary<PoseInterfacePortId, int> imports,
            string scope,
            Dictionary<string, int> values)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                if (imports.TryGetValue(port.InterfacePortId, out int value))
                    values.Add(EndpointKey(node.NodeId, port.PortId, scope), value);
                else if (port.Required)
                    throw new InvalidOperationException($"GraphInput Interface Port '{port.InterfacePortId}' has no call-site source.");
            }
        }

        static void BindGraphOutputs(
            CharacterPoseNodeDefinition node,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, int> values,
            Dictionary<PoseInterfacePortId, int> exports)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Input)
                    continue;
                if (TryGetInputValue(node, port, incoming, scope, values, out int value))
                    exports.Add(port.InterfacePortId, value);
                else if (port.Required)
                    throw new InvalidOperationException($"GraphOutput Interface Port '{port.InterfacePortId}' has no internal source.");
            }
        }

        static void CompileSubgraphCall(
            CompilationState state,
            CharacterPoseGraphData owner,
            CharacterPoseNodeDefinition callSite,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            string callChain,
            Dictionary<string, int> values)
        {
            CharacterPoseSubgraphReference reference = callSite.Subgraph;
            CharacterPoseGraphData child = reference.HasInline ? reference.Inline : reference.Shared.Graph;
            var childImports = new Dictionary<PoseInterfacePortId, int>();
            for (int i = 0; i < callSite.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = callSite.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Input)
                    continue;
                if (TryGetInputValue(callSite, port, incoming, scope, values, out int value))
                    childImports.Add(port.InterfacePortId, value);
                else if (port.Required)
                    throw new InvalidOperationException($"PoseSubgraph '{callSite.NodeId}' Interface Port '{port.InterfacePortId}' has no source.");
            }

            PoseNodeId scopedCallSite = ScopeNodeId(callSite.NodeId, scope);
            string childScope = scopedCallSite.Value + "/" + child.GraphId;
            string childCallChain = string.IsNullOrEmpty(callChain)
                ? $"{owner.GraphId}/{scopedCallSite.Value}->{child.GraphId}"
                : $"{callChain}|{owner.GraphId}/{scopedCallSite.Value}->{child.GraphId}";
            Dictionary<PoseInterfacePortId, int> childExports = CompileGraph(
                state,
                child,
                childImports,
                childScope,
                childCallChain,
                false);
            for (int i = 0; i < callSite.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = callSite.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                if (childExports.TryGetValue(port.InterfacePortId, out int value))
                    values.Add(EndpointKey(callSite.NodeId, port.PortId, scope), value);
                else if (port.Required)
                    throw new InvalidOperationException($"PoseSubgraph '{callSite.NodeId}' Interface Port '{port.InterfacePortId}' has no compiled output.");
            }
        }

        static void BindOperationOutputs(
            CharacterPoseNodeDefinition node,
            string scope,
            int value,
            Dictionary<string, int> values)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port != null && port.Direction == CharacterPosePortDirection.Output)
                    values.Add(EndpointKey(node.NodeId, port.PortId, scope), value);
            }
        }

        static List<CharacterPoseNodeDefinition> TopologicalOrder(CharacterPoseGraphData graph)
        {
            var nodes = graph.Nodes.ToDictionary(value => value.NodeId);
            var indegree = nodes.Keys.ToDictionary(value => value, _ => 0);
            var outgoing = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseEdge edge = graph.Edges[i];
                indegree[edge.TargetNodeId]++;
                if (!outgoing.TryGetValue(edge.SourceNodeId, out List<PoseNodeId> values))
                {
                    values = new List<PoseNodeId>();
                    outgoing.Add(edge.SourceNodeId, values);
                }
                values.Add(edge.TargetNodeId);
            }
            var ready = new SortedSet<PoseNodeId>(indegree.Where(value => value.Value == 0).Select(value => value.Key));
            var result = new List<CharacterPoseNodeDefinition>(nodes.Count);
            while (ready.Count > 0)
            {
                PoseNodeId current = ready.Min;
                ready.Remove(current);
                result.Add(nodes[current]);
                if (!outgoing.TryGetValue(current, out List<PoseNodeId> targets))
                    continue;
                targets.Sort();
                for (int i = 0; i < targets.Count; i++)
                {
                    PoseNodeId target = targets[i];
                    indegree[target]--;
                    if (indegree[target] == 0)
                        ready.Add(target);
                }
            }
            if (result.Count != nodes.Count)
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' cannot produce a stable topological order.");
            return result;
        }

        static Dictionary<string, CharacterPoseEdge> BuildIncoming(CharacterPoseGraphData graph)
        {
            var result = new Dictionary<string, CharacterPoseEdge>(StringComparer.Ordinal);
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseEdge edge = graph.Edges[i];
                result.Add(edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value, edge);
            }
            return result;
        }

        static int RequireInputValue(
            CharacterPoseNodeDefinition node,
            CharacterPosePortDefinition port,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, int> values)
        {
            if (!TryGetInputValue(node, port, incoming, scope, values, out int value))
                throw new InvalidOperationException($"Pose Node '{node.NodeId}' input Port '{port.PortId}' has no compiled source.");
            return value;
        }

        static bool TryGetInputValue(
            CharacterPoseNodeDefinition node,
            CharacterPosePortDefinition port,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, int> values,
            out int value)
        {
            value = -1;
            return incoming.TryGetValue(node.NodeId.Value + "\0" + port.PortId.Value, out CharacterPoseEdge edge) &&
                   values.TryGetValue(EndpointKey(edge.SourceNodeId, edge.SourcePortId, scope), out value);
        }

        static string EndpointKey(PoseNodeId nodeId, PosePortId portId, string scope) =>
            ScopeNodeId(nodeId, scope).Value + "\0" + ScopePortId(portId, scope).Value;

        static PoseNodeId ScopeNodeId(PoseNodeId nodeId, string scope) =>
            string.IsNullOrEmpty(scope) ? nodeId : new PoseNodeId(scope + "/" + nodeId.Value);

        static PosePortId ScopePortId(PosePortId portId, string scope) =>
            string.IsNullOrEmpty(scope) ? portId : new PosePortId(scope + "/" + portId.Value);

        static int CompileMask(
            CharacterAnimationBoneMaskAsset mask,
            CharacterAnimationRigDefinition rig,
            List<CharacterPresentationDenseBoneMask> masks,
            Dictionary<string, int> indices)
        {
            string key = mask.MaskId + "\0" + mask.RigId + "\0" + mask.RigRevision;
            if (indices.TryGetValue(key, out int existing))
                return existing;
            int index = masks.Count;
            masks.Add(new CharacterPresentationDenseBoneMask(index, mask.MaskId, mask.BuildDense(rig)));
            indices.Add(key, index);
            return index;
        }

        static int CompileAdditiveReference(
            CharacterPoseNodeDefinition node,
            CharacterAnimationRigDefinition rig,
            List<CharacterPresentationAdditiveReferenceDescriptor> references)
        {
            int count = rig.Bones.Count;
            var positions = new UnityEngine.Vector3[count];
            var rotations = new UnityEngine.Quaternion[count];
            var scales = new UnityEngine.Vector3[count];
            for (int i = 0; i < count; i++)
            {
                CharacterAnimationBoneDefinition bone = rig.Bones[i];
                positions[i] = bone.ReferenceLocalPosition;
                rotations[i] = bone.ReferenceLocalRotation;
                scales[i] = bone.ReferenceLocalScale;
            }
            int index = references.Count;
            references.Add(new CharacterPresentationAdditiveReferenceDescriptor(
                index,
                node.AdditiveReferencePoseId,
                node.AdditiveReferenceSpace,
                node.AdditiveScalePolicy,
                positions,
                rotations,
                scales));
            return index;
        }

        static PoseParameterResolvePolicy[] CompilePolicies(
            CharacterPoseNodeDefinition node,
            CharacterPresentationPoseParameterProgramEntry[] parameters,
            Dictionary<PoseParameterId, int> indices)
        {
            if (node.Kind != CharacterPoseNodeKind.LayeredBoneBlend &&
                node.Kind != CharacterPoseNodeKind.AdditivePose &&
                node.Kind != CharacterPoseNodeKind.PoseCurveResolve)
                return Array.Empty<PoseParameterResolvePolicy>();
            var result = new PoseParameterResolvePolicy[parameters.Length];
            for (int i = 0; i < node.ParameterPolicies.Count; i++)
            {
                CharacterPoseParameterPolicy policy = node.ParameterPolicies[i];
                result[indices[policy.ParameterId]] = policy.Policy;
            }
            return result;
        }

        static CharacterPoseOperationCode OperationCode(CharacterPoseNodeKind kind)
        {
            return kind switch
            {
                CharacterPoseNodeKind.PoseSlotInput => CharacterPoseOperationCode.PoseSlotInput,
                CharacterPoseNodeKind.LayeredBoneBlend => CharacterPoseOperationCode.LayeredBoneBlend,
                CharacterPoseNodeKind.AdditivePose => CharacterPoseOperationCode.AdditivePose,
                CharacterPoseNodeKind.PoseCurveResolve => CharacterPoseOperationCode.PoseCurveResolve,
                CharacterPoseNodeKind.PoseSubgraph => throw new InvalidOperationException(
                    "PoseSubgraph must disappear during static expansion."),
                CharacterPoseNodeKind.OutputPose => CharacterPoseOperationCode.OutputPose,
                CharacterPoseNodeKind.GraphInput => throw new InvalidOperationException(
                    "GraphInput must disappear during static expansion."),
                CharacterPoseNodeKind.GraphOutput => throw new InvalidOperationException(
                    "GraphOutput must disappear during static expansion."),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        static string ComputeHash(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<CharacterPresentationPoseSlotProgramEntry> slots,
            IReadOnlyList<CharacterPresentationPoseParameterProgramEntry> parameters,
            IReadOnlyList<CharacterPresentationDenseBoneMask> masks,
            IReadOnlyList<CharacterPresentationAdditiveReferenceDescriptor> additiveReferences,
            IReadOnlyList<CharacterPresentationPoseOperation> operations,
            IReadOnlyList<CharacterPresentationPoseSourceMapEntry> sourceMap,
            IReadOnlyList<string> graphDependencies)
        {
            var values = new List<string>
            {
                CharacterPresentationPoseProgram.SchemaVersion,
                CharacterPresentationPoseProgram.RuntimeAbi,
                graph.GraphId,
                graph.ContentRevision,
                rig.RigId,
                rig.Revision,
                rig.Bones.Count.ToString(CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < graphDependencies.Count; i++)
                values.Add("graph:" + graphDependencies[i]);
            for (int i = 0; i < slots.Count; i++)
            {
                CharacterPresentationPoseSlotProgramEntry slot = slots[i];
                values.Add($"slot:{slot.Index}:{slot.PoseSlotId.Value}:{slot.AnimationChannelId.Value}:{(int)slot.OutputPolicy}");
            }
            for (int i = 0; i < parameters.Count; i++)
                values.Add($"parameter:{parameters[i].Index}:{parameters[i].ParameterId.Value}:{parameters[i].DefaultValue:R}");
            for (int i = 0; i < masks.Count; i++)
            {
                values.Add($"mask:{masks[i].Index}:{masks[i].MaskId}");
                for (int bone = 0; bone < masks[i].Weights.Count; bone++)
                    values.Add(masks[i].Weights[bone].ToString("R", CultureInfo.InvariantCulture));
            }
            for (int i = 0; i < additiveReferences.Count; i++)
            {
                CharacterPresentationAdditiveReferenceDescriptor reference = additiveReferences[i];
                values.Add($"additive:{reference.Index}:{reference.ReferencePoseId}:{(int)reference.Space}:{(int)reference.ScalePolicy}");
                for (int bone = 0; bone < reference.Positions.Count; bone++)
                {
                    UnityEngine.Vector3 position = reference.Positions[bone];
                    UnityEngine.Quaternion rotation = reference.Rotations[bone];
                    UnityEngine.Vector3 scale = reference.Scales[bone];
                    values.Add($"{position.x:R}:{position.y:R}:{position.z:R}");
                    values.Add($"{rotation.x:R}:{rotation.y:R}:{rotation.z:R}:{rotation.w:R}");
                    values.Add($"{scale.x:R}:{scale.y:R}:{scale.z:R}");
                }
            }
            for (int i = 0; i < operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = operations[i];
                values.Add($"operation:{operation.Index}:{(int)operation.Code}:{operation.Version}:{operation.NodeId.Value}:{operation.OutputPoseValueIndex}:{operation.InputPoseValueIndexA}:{operation.InputPoseValueIndexB}:{operation.PoseSlotIndex}:{operation.BoneMaskIndex}:{operation.AdditiveReferenceIndex}:{operation.Weight:R}");
                for (int parameter = 0; parameter < operation.ParameterPolicies.Count; parameter++)
                    values.Add(((int)operation.ParameterPolicies[parameter]).ToString(CultureInfo.InvariantCulture));
            }
            for (int i = 0; i < sourceMap.Count; i++)
                values.Add($"source:{sourceMap[i].OperationIndex}:{sourceMap[i].GraphId}:{sourceMap[i].NodeId.Value}:{sourceMap[i].CallSite}");
            return StableHash.Compute(values.ToArray()).ToString();
        }
    }
}
