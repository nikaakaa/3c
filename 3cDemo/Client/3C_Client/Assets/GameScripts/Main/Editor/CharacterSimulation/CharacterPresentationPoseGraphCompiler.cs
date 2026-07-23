using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public static class CharacterPresentationPoseGraphCompiler
    {
        readonly struct CompiledValue
        {
            public CompiledValue(CharacterPosePortKind kind, int index, int producerOperationIndex = -1)
            {
                Kind = kind;
                Index = index;
                ProducerOperationIndex = producerOperationIndex;
            }

            public CharacterPosePortKind Kind { get; }
            public int Index { get; }
            public int ProducerOperationIndex { get; }
        }

        sealed class CompilationState
        {
            public CompilationState(
                CharacterAnimationRigDefinition rig,
                CharacterPresentationPoseParameterEntry[] parameters,
                Dictionary<PoseParameterId, int> parameterIndices,
                AnimationBlendNodePayload[] blendNodes)
            {
                Rig = rig;
                Parameters = parameters;
                ParameterIndices = parameterIndices;
                BlendNodes = blendNodes;
                BlendNodeIndices = blendNodes
                    .Select((value, index) => new KeyValuePair<PoseNodeId, int>(value.NodeId, index))
                    .ToDictionary(value => value.Key, value => value.Value);
            }

            public CharacterAnimationRigDefinition Rig { get; }
            public CharacterPresentationPoseParameterEntry[] Parameters { get; }
            public Dictionary<PoseParameterId, int> ParameterIndices { get; }
            public AnimationBlendNodePayload[] BlendNodes { get; }
            public Dictionary<PoseNodeId, int> BlendNodeIndices { get; }
            public List<CharacterPresentationSelectionInputEntry> SelectionInputs { get; } = new List<CharacterPresentationSelectionInputEntry>();
            public List<CharacterPresentationDenseBoneMask> Masks { get; } = new List<CharacterPresentationDenseBoneMask>();
            public Dictionary<string, int> MaskIndices { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
            public List<CharacterPresentationAdditiveReferenceDescriptor> AdditiveReferences { get; } = new List<CharacterPresentationAdditiveReferenceDescriptor>();
            public int InertializationCount { get; set; }
            public List<CharacterPresentationModifyBoneDescriptor> ModifyBones { get; } = new List<CharacterPresentationModifyBoneDescriptor>();
            public List<CharacterPresentationFootPlacementNodeDescriptor> FootPlacementNodes { get; } = new List<CharacterPresentationFootPlacementNodeDescriptor>();
            public List<CharacterPresentationPoseOperation> Operations { get; } = new List<CharacterPresentationPoseOperation>();
            public List<CharacterPresentationPoseSourceMapEntry> SourceMap { get; } = new List<CharacterPresentationPoseSourceMapEntry>();
            public List<string> GraphDependencies { get; } = new List<string>();
            public int PoseValueCount { get; set; }
            public int PlayerCount { get; set; }
            public int OutputOperationIndex { get; set; } = -1;
        }

        public static CharacterPresentationPosePlan Compile(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableAnimationChannels,
            AnimationBlendNodePayload[] blendNodes,
            List<string> errors)
        {
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
                return CompileValidated(asset.Graph, rig, blendNodes ?? Array.Empty<AnimationBlendNodePayload>());
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }

        static CharacterPresentationPosePlan CompileValidated(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            AnimationBlendNodePayload[] blendNodes)
        {
            CharacterPoseParameterDeclaration[] authoredParameters = graph.Parameters.OrderBy(value => value.ParameterId).ToArray();
            var parameters = new CharacterPresentationPoseParameterEntry[authoredParameters.Length];
            var parameterIndices = new Dictionary<PoseParameterId, int>();
            for (int i = 0; i < authoredParameters.Length; i++)
            {
                CharacterPoseParameterDeclaration parameter = authoredParameters[i];
                parameters[i] = new CharacterPresentationPoseParameterEntry(
                    i,
                    parameter.ParameterId,
                    parameter.ValueType,
                    parameter.DefaultValue,
                    parameter.Unit);
                parameterIndices.Add(parameter.ParameterId, i);
            }
            if (!parameterIndices.ContainsKey(AnimationPoseParameterIds.FootPlacementWeight))
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' requires Parameter '{AnimationPoseParameterIds.FootPlacementWeight}'.");

            var state = new CompilationState(rig, parameters, parameterIndices, blendNodes);
            CompileGraph(
                state,
                graph,
                new Dictionary<PoseInterfacePortId, CompiledValue>(),
                string.Empty,
                string.Empty,
                true);
            if (state.OutputOperationIndex < 0 || state.PoseValueCount <= 0 || state.SelectionInputs.Count == 0)
                throw new InvalidOperationException("Pose Plan has no complete Selection, Pose and Output boundary.");
            if (state.BlendNodeIndices.Count != state.BlendNodes.Length)
                throw new InvalidOperationException("Pose Plan Blend Stack payload identities are not unique.");

            int selectionWorkspace = state.SelectionInputs.Count;
            int poseWorkspace = state.PoseValueCount;
            int parameterWorkspace = parameters.Length;
            int contributionCapacityPerValue = ComputeContributionCapacityPerValue(state);
            int contributionWorkspace = checked(poseWorkspace * contributionCapacityPerValue);
            int frameCache = state.Operations.Count;
            string hash = ComputeHash(graph, rig, state, selectionWorkspace, poseWorkspace, parameterWorkspace, contributionWorkspace, frameCache);
            return new CharacterPresentationPosePlan(
                graph.GraphId,
                graph.ContentRevision,
                hash,
                rig,
                state.SelectionInputs.ToArray(),
                parameters,
                blendNodes,
                Array.Empty<CharacterPresentationInertializationDescriptor>(),
                state.Masks.ToArray(),
                state.AdditiveReferences.ToArray(),
                state.ModifyBones.ToArray(),
                state.FootPlacementNodes.ToArray(),
                state.Operations.ToArray(),
                state.SourceMap.ToArray(),
                selectionWorkspace,
                poseWorkspace,
                parameterWorkspace,
                contributionWorkspace,
                frameCache,
                state.OutputOperationIndex);
        }

        static int ComputeContributionCapacityPerValue(CompilationState state)
        {
            int capacity = 0;
            for (int i = 0; i < state.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = state.Operations[i];
                if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer ||
                    operation.Code == CharacterPoseOperationCode.BlendSpacePlayer)
                {
                    capacity = checked(capacity + 1);
                    continue;
                }
                if (operation.Code != CharacterPoseOperationCode.BlendStack ||
                    operation.BlendNodeIndex < 0 || operation.BlendNodeIndex >= state.BlendNodes.Length)
                    continue;
                AnimationBlendNodePayload blendNode = state.BlendNodes[operation.BlendNodeIndex];
                if (blendNode?.StackPolicy == null || blendNode.StackPolicy.MaxActiveSourceEntries <= 0)
                    throw new InvalidOperationException($"Pose Plan Blend Stack '{operation.NodeId}' has an invalid contribution capacity.");
                capacity = checked(capacity + blendNode.StackPolicy.MaxActiveSourceEntries + 1);
            }
            if (capacity <= 0)
                throw new InvalidOperationException("Pose Plan requires at least one Player contribution capacity.");
            return capacity;
        }

        static Dictionary<PoseInterfacePortId, CompiledValue> CompileGraph(
            CompilationState state,
            CharacterPoseGraphData graph,
            IReadOnlyDictionary<PoseInterfacePortId, CompiledValue> imports,
            string scope,
            string callChain,
            bool root)
        {
            state.GraphDependencies.Add($"{callChain}\0{graph.GraphId}\0{graph.ContentRevision}");
            List<CharacterPoseNodeDefinition> orderedNodes = TopologicalOrder(graph);
            Dictionary<string, CharacterPoseEdge> incoming = BuildIncoming(graph);
            var values = new Dictionary<string, CompiledValue>(StringComparer.Ordinal);
            var exports = new Dictionary<PoseInterfacePortId, CompiledValue>();
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

                PoseNodeId scopedNodeId = ScopeNodeId(node.NodeId, scope);
                CharacterPoseOperationCode code = OperationCode(node.Kind);
                int operationIndex = state.Operations.Count;
                int outputValueIndex = HasPoseOutput(node) || node.Kind == CharacterPoseNodeKind.OutputPose
                    ? state.PoseValueCount++
                    : -1;
                int inputA = RequireOptionalInput(node, CharacterPosePortKind.Pose, 0, incoming, scope, values);
                int inputB = RequireOptionalInput(node, CharacterPosePortKind.Pose, 1, incoming, scope, values);
                int selectionInputIndex = -1;
                int markerSyncOperationIndex = -1;
                int parameterIndex = -1;
                int parameterIndexB = -1;
                int playerIndex = -1;
                if (node.Kind == CharacterPoseNodeKind.AnimationSelectionInput || node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput)
                {
                    selectionInputIndex = state.SelectionInputs.Count;
                    state.SelectionInputs.Add(new CharacterPresentationSelectionInputEntry(
                        selectionInputIndex,
                        scopedNodeId,
                        node.AnimationChannelId,
                        node.ProgramProducerId,
                        node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput,
                        node.SelectionAvailability));
                }
                else if (node.Kind == CharacterPoseNodeKind.MarkerSync ||
                         node.Kind == CharacterPoseNodeKind.SelectedPosePlayer ||
                         node.Kind == CharacterPoseNodeKind.BlendStack ||
                         node.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
                {
                    CompiledValue selection = RequireInput(node, CharacterPosePortKind.AnimationSelection, 0, incoming, scope, values);
                    selectionInputIndex = selection.Index;
                    if ((node.Kind == CharacterPoseNodeKind.SelectedPosePlayer || node.Kind == CharacterPoseNodeKind.BlendStack ||
                         node.Kind == CharacterPoseNodeKind.BlendSpacePlayer) &&
                        selection.ProducerOperationIndex >= 0 &&
                        state.Operations[selection.ProducerOperationIndex].Code == CharacterPoseOperationCode.MarkerSync)
                        markerSyncOperationIndex = selection.ProducerOperationIndex;
                }
                if (node.Kind == CharacterPoseNodeKind.SelectedPosePlayer || node.Kind == CharacterPoseNodeKind.BlendStack ||
                    node.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
                    playerIndex = state.PlayerCount++;
                if (node.Kind == CharacterPoseNodeKind.ProgramParameterInput)
                    parameterIndex = state.ParameterIndices[node.ParameterId];
                else if (node.Ports.Any(port => port != null &&
                    port.Kind == CharacterPosePortKind.Parameter &&
                    port.Direction == CharacterPosePortDirection.Input))
                {
                    CompiledValue parameterInput = RequireInput(node, CharacterPosePortKind.Parameter, 0, incoming, scope, values, false);
                    parameterIndex = parameterInput.Index;
                }
                if (node.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
                    parameterIndexB = TryGetInputIndex(node, CharacterPosePortKind.Parameter, 1, incoming, scope, values);

                int blendNodeIndex = node.Kind == CharacterPoseNodeKind.BlendStack
                    ? state.BlendNodeIndices.TryGetValue(scopedNodeId, out int index)
                        ? index
                        : throw new InvalidOperationException($"Blend Stack '{scopedNodeId}' has no compiled policy payload.")
                    : -1;
                int inertializationIndex = node.Kind == CharacterPoseNodeKind.Inertialization
                    ? CompileInertialization(node, scopedNodeId, state)
                    : -1;
                int maskIndex = node.BoneMask ? CompileMask(node.BoneMask, state.Rig, state.Masks, state.MaskIndices) : -1;
                int additiveIndex = node.Kind == CharacterPoseNodeKind.AdditivePose
                    ? CompileAdditiveReference(node, state.Rig, state.AdditiveReferences)
                    : -1;
                int modifyIndex = node.Kind == CharacterPoseNodeKind.ModifyBone
                    ? CompileModifyBone(node, state)
                    : -1;
                int footIndex = node.Kind == CharacterPoseNodeKind.FootPlacement
                    ? CompileFootPlacement(node, scopedNodeId, state)
                    : -1;
                PoseParameterResolvePolicy[] policies = CompilePolicies(node, state.Parameters, state.ParameterIndices);
                state.Operations.Add(new CharacterPresentationPoseOperation(
                    operationIndex,
                    Phase(node.Kind),
                    code,
                    scopedNodeId,
                    outputValueIndex,
                    inputA,
                    inputB,
                    selectionInputIndex,
                    markerSyncOperationIndex,
                    parameterIndex,
                    parameterIndexB,
                    node.BlendSpaceInputRangePolicy,
                    playerIndex,
                    blendNodeIndex,
                    inertializationIndex,
                    maskIndex,
                    additiveIndex,
                    modifyIndex,
                    footIndex,
                    node.Weight,
                    policies));
                state.SourceMap.Add(new CharacterPresentationPoseSourceMapEntry(operationIndex, graph.GraphId, scopedNodeId, callChain));

                BindOperationOutputs(node, scope, outputValueIndex, selectionInputIndex, parameterIndex, operationIndex, values);
                if (node.Kind == CharacterPoseNodeKind.OutputPose)
                {
                    if (!root || state.OutputOperationIndex >= 0)
                        throw new InvalidOperationException("Pose Plan contains an invalid OutputPose boundary.");
                    state.OutputOperationIndex = operationIndex;
                }
            }
            return exports;
        }

        static int CompileModifyBone(CharacterPoseNodeDefinition node, CompilationState state)
        {
            int boneIndex = state.Rig.RequireBoneIndex(node.BoneId);
            int index = state.ModifyBones.Count;
            state.ModifyBones.Add(new CharacterPresentationModifyBoneDescriptor(
                index,
                boneIndex,
                state.Rig.Bones[boneIndex].ParentIndex,
                node));
            return index;
        }

        static int CompileInertialization(
            CharacterPoseNodeDefinition node,
            PoseNodeId scopedNodeId,
            CompilationState state)
        {
            return state.InertializationCount++;
        }

        static int CompileFootPlacement(CharacterPoseNodeDefinition node, PoseNodeId scopedNodeId, CompilationState state)
        {
            if (state.FootPlacementNodes.Count != 0)
                throw new InvalidOperationException("Pose Plan contains more than one FootPlacement node.");
            int index = state.FootPlacementNodes.Count;
            state.FootPlacementNodes.Add(new CharacterPresentationFootPlacementNodeDescriptor(
                index,
                scopedNodeId,
                node.FootPlacementCalibration.CalibrationId.Value,
                node.FootPlacementCalibration.ContentRevision));
            return index;
        }

        static void BindGraphInputs(
            CharacterPoseNodeDefinition node,
            IReadOnlyDictionary<PoseInterfacePortId, CompiledValue> imports,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                if (imports.TryGetValue(port.InterfacePortId, out CompiledValue value))
                    values.Add(EndpointKey(node.NodeId, port.PortId, scope), value);
                else if (port.Required)
                    throw new InvalidOperationException($"GraphInput Interface Port '{port.InterfacePortId}' has no call-site source.");
            }
        }

        static void BindGraphOutputs(
            CharacterPoseNodeDefinition node,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, CompiledValue> values,
            Dictionary<PoseInterfacePortId, CompiledValue> exports)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Input)
                    continue;
                if (TryGetInputValue(node, port, incoming, scope, values, out CompiledValue value))
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
            Dictionary<string, CompiledValue> values)
        {
            CharacterPoseGraphData child = callSite.Subgraph.HasInline ? callSite.Subgraph.Inline : callSite.Subgraph.Shared.Graph;
            var imports = new Dictionary<PoseInterfacePortId, CompiledValue>();
            for (int i = 0; i < callSite.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = callSite.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Input)
                    continue;
                if (TryGetInputValue(callSite, port, incoming, scope, values, out CompiledValue value))
                    imports.Add(port.InterfacePortId, value);
                else if (port.Required)
                    throw new InvalidOperationException($"PoseSubgraph '{callSite.NodeId}' Interface Port '{port.InterfacePortId}' has no source.");
            }
            PoseNodeId scopedCallSite = ScopeNodeId(callSite.NodeId, scope);
            string childScope = scopedCallSite.Value + "/" + child.GraphId;
            string childCallChain = string.IsNullOrEmpty(callChain)
                ? $"{owner.GraphId}/{scopedCallSite.Value}->{child.GraphId}"
                : $"{callChain}|{owner.GraphId}/{scopedCallSite.Value}->{child.GraphId}";
            Dictionary<PoseInterfacePortId, CompiledValue> exports = CompileGraph(state, child, imports, childScope, childCallChain, false);
            for (int i = 0; i < callSite.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = callSite.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                if (exports.TryGetValue(port.InterfacePortId, out CompiledValue value))
                    values.Add(EndpointKey(callSite.NodeId, port.PortId, scope), value);
                else if (port.Required)
                    throw new InvalidOperationException($"PoseSubgraph '{callSite.NodeId}' Interface Port '{port.InterfacePortId}' has no output.");
            }
        }

        static void BindOperationOutputs(
            CharacterPoseNodeDefinition node,
            string scope,
            int poseValue,
            int selectionValue,
            int parameterValue,
            int operationIndex,
            Dictionary<string, CompiledValue> values)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                int index = port.Kind switch
                {
                    CharacterPosePortKind.AnimationSelection => selectionValue,
                    CharacterPosePortKind.Parameter => parameterValue,
                    CharacterPosePortKind.Pose => poseValue,
                    CharacterPosePortKind.PoseDiscontinuity => poseValue,
                    _ => -1
                };
                if (index < 0)
                    throw new InvalidOperationException($"Pose Node '{node.NodeId}' output '{port.PortId}' has no compiled workspace value.");
                values.Add(EndpointKey(node.NodeId, port.PortId, scope), new CompiledValue(port.Kind, index, operationIndex));
            }
        }

        static CompiledValue RequireInput(
            CharacterPoseNodeDefinition node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, CompiledValue> values,
            bool required = true)
        {
            CharacterPosePortDefinition[] ports = node.Ports
                .Where(value => value != null && value.Kind == kind && value.Direction == CharacterPosePortDirection.Input)
                .ToArray();
            if ((uint)ordinal >= (uint)ports.Length)
                return default;
            if (TryGetInputValue(node, ports[ordinal], incoming, scope, values, out CompiledValue value))
                return value;
            if (required || ports[ordinal].Required)
                throw new InvalidOperationException($"Pose Node '{node.NodeId}' input '{ports[ordinal].PortId}' has no compiled source.");
            return default;
        }

        static int RequireOptionalInput(
            CharacterPoseNodeDefinition node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            CharacterPosePortDefinition[] ports = node.Ports
                .Where(value => value != null && value.Kind == kind && value.Direction == CharacterPosePortDirection.Input)
                .ToArray();
            if ((uint)ordinal >= (uint)ports.Length)
                return -1;
            return RequireInput(node, kind, ordinal, incoming, scope, values).Index;
        }

        static int TryGetInputIndex(
            CharacterPoseNodeDefinition node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            CharacterPosePortDefinition[] ports = node.Ports
                .Where(value => value != null && value.Kind == kind && value.Direction == CharacterPosePortDirection.Input)
                .ToArray();
            if ((uint)ordinal >= (uint)ports.Length)
                return -1;
            return TryGetInputValue(node, ports[ordinal], incoming, scope, values, out CompiledValue value)
                ? value.Index
                : -1;
        }

        static bool TryGetInputValue(
            CharacterPoseNodeDefinition node,
            CharacterPosePortDefinition port,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            Dictionary<string, CompiledValue> values,
            out CompiledValue value)
        {
            value = default;
            return incoming.TryGetValue(node.NodeId.Value + "\0" + port.PortId.Value, out CharacterPoseEdge edge) &&
                   values.TryGetValue(EndpointKey(edge.SourceNodeId, edge.SourcePortId, scope), out value) &&
                   value.Kind == port.Kind;
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
                if (!outgoing.TryGetValue(edge.SourceNodeId, out List<PoseNodeId> targets))
                {
                    targets = new List<PoseNodeId>();
                    outgoing.Add(edge.SourceNodeId, targets);
                }
                targets.Add(edge.TargetNodeId);
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
                    if (--indegree[targets[i]] == 0)
                        ready.Add(targets[i]);
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

        static int CompileMask(
            CharacterAnimationBoneMaskAsset mask,
            CharacterAnimationRigDefinition rig,
            List<CharacterPresentationDenseBoneMask> masks,
            Dictionary<string, int> indices)
        {
            float[] dense = mask.BuildDense(rig);
            string key = mask.MaskId + "\0" + string.Join("|", dense.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
            if (indices.TryGetValue(key, out int existing))
                return existing;
            int index = masks.Count;
            masks.Add(new CharacterPresentationDenseBoneMask(index, mask.MaskId, dense));
            indices.Add(key, index);
            return index;
        }

        static int CompileAdditiveReference(
            CharacterPoseNodeDefinition node,
            CharacterAnimationRigDefinition rig,
            List<CharacterPresentationAdditiveReferenceDescriptor> references)
        {
            int count = rig.Bones.Count;
            var positions = new Vector3[count];
            var rotations = new Quaternion[count];
            var scales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                CharacterAnimationBoneDefinition bone = rig.Bones[i];
                if (bone.ParentIndex < 0)
                {
                    positions[i] = bone.ReferenceLocalPosition;
                    rotations[i] = bone.ReferenceLocalRotation;
                    scales[i] = bone.ReferenceLocalScale;
                    continue;
                }
                int parent = bone.ParentIndex;
                positions[i] = positions[parent] + rotations[parent] * Vector3.Scale(scales[parent], bone.ReferenceLocalPosition);
                rotations[i] = (rotations[parent] * bone.ReferenceLocalRotation).normalized;
                scales[i] = Vector3.Scale(scales[parent], bone.ReferenceLocalScale);
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
            CharacterPresentationPoseParameterEntry[] parameters,
            Dictionary<PoseParameterId, int> indices)
        {
            if (node.ParameterPolicies.Count == 0)
                return Array.Empty<PoseParameterResolvePolicy>();
            var result = new PoseParameterResolvePolicy[parameters.Length];
            for (int i = 0; i < node.ParameterPolicies.Count; i++)
            {
                CharacterPoseParameterPolicy policy = node.ParameterPolicies[i];
                result[indices[policy.ParameterId]] = policy.Policy;
            }
            return result;
        }

        static bool HasPoseOutput(CharacterPoseNodeDefinition node) =>
            node.Ports.Any(value => value != null && value.Kind == CharacterPosePortKind.Pose && value.Direction == CharacterPosePortDirection.Output);

        static CharacterPosePlanPhase Phase(CharacterPoseNodeKind kind) => kind switch
        {
            CharacterPoseNodeKind.AnimationSelectionInput => CharacterPosePlanPhase.Selection,
            CharacterPoseNodeKind.MotionMatchingSelectionInput => CharacterPosePlanPhase.Selection,
            CharacterPoseNodeKind.ProgramParameterInput => CharacterPosePlanPhase.Selection,
            CharacterPoseNodeKind.MarkerSync => CharacterPosePlanPhase.Selection,
            CharacterPoseNodeKind.FootPlacement => CharacterPosePlanPhase.WorldAwarePostProcess,
            CharacterPoseNodeKind.OutputPose => CharacterPosePlanPhase.FinalPublication,
            _ => CharacterPosePlanPhase.SourceAndNativePose
        };

        static CharacterPoseOperationCode OperationCode(CharacterPoseNodeKind kind) => kind switch
        {
            CharacterPoseNodeKind.AnimationSelectionInput => CharacterPoseOperationCode.AnimationSelectionInput,
            CharacterPoseNodeKind.MotionMatchingSelectionInput => CharacterPoseOperationCode.MotionMatchingSelectionInput,
            CharacterPoseNodeKind.ProgramParameterInput => CharacterPoseOperationCode.ProgramParameterInput,
            CharacterPoseNodeKind.MarkerSync => CharacterPoseOperationCode.MarkerSync,
            CharacterPoseNodeKind.SelectedPosePlayer => CharacterPoseOperationCode.SelectedPosePlayer,
            CharacterPoseNodeKind.BlendSpacePlayer => CharacterPoseOperationCode.BlendSpacePlayer,
            CharacterPoseNodeKind.BlendStack => CharacterPoseOperationCode.BlendStack,
            CharacterPoseNodeKind.Inertialization => CharacterPoseOperationCode.Inertialization,
            CharacterPoseNodeKind.BlendPose => CharacterPoseOperationCode.BlendPose,
            CharacterPoseNodeKind.LayeredBoneBlend => CharacterPoseOperationCode.LayeredBoneBlend,
            CharacterPoseNodeKind.AdditivePose => CharacterPoseOperationCode.AdditivePose,
            CharacterPoseNodeKind.PoseParameterResolve => CharacterPoseOperationCode.PoseParameterResolve,
            CharacterPoseNodeKind.ModifyBone => CharacterPoseOperationCode.ModifyBone,
            CharacterPoseNodeKind.FootPlacement => CharacterPoseOperationCode.FootPlacement,
            CharacterPoseNodeKind.OutputPose => CharacterPoseOperationCode.OutputPose,
            _ => throw new InvalidOperationException($"Node '{kind}' must disappear during static subgraph expansion.")
        };

        static string EndpointKey(PoseNodeId nodeId, PosePortId portId, string scope) =>
            ScopeNodeId(nodeId, scope).Value + "\0" + ScopePortId(portId, scope).Value;

        static PoseNodeId ScopeNodeId(PoseNodeId nodeId, string scope) =>
            string.IsNullOrEmpty(scope) ? nodeId : new PoseNodeId(scope + "/" + nodeId.Value);

        static PosePortId ScopePortId(PosePortId portId, string scope) =>
            string.IsNullOrEmpty(scope) ? portId : new PosePortId(scope + "/" + portId.Value);

        static string ComputeHash(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            CompilationState state,
            int selectionWorkspace,
            int poseWorkspace,
            int parameterWorkspace,
            int contributionWorkspace,
            int frameCache)
        {
            var values = new List<string>
            {
                CharacterPresentationPosePlan.SchemaVersion,
                CharacterPresentationPosePlan.RuntimeAbi,
                graph.GraphId,
                graph.ContentRevision,
                rig.RigId,
                rig.Revision
            };
            values.AddRange(state.GraphDependencies.Select(value => "graph:" + value));
            for (int i = 0; i < state.SelectionInputs.Count; i++)
            {
                CharacterPresentationSelectionInputEntry input = state.SelectionInputs[i];
                values.Add(FormattableString.Invariant($"selection:{input.Index}:{input.NodeId}:{input.AnimationChannelId}:{input.ProgramProducerId}:{input.MotionMatching}:{(int)input.Availability}"));
            }
            for (int i = 0; i < state.Parameters.Length; i++)
            {
                CharacterPresentationPoseParameterEntry parameter = state.Parameters[i];
                values.Add(FormattableString.Invariant($"parameter:{parameter.Index}:{parameter.ParameterId}:{(int)parameter.ValueType}:{parameter.Unit}:{parameter.DefaultValue:R}"));
            }
            for (int i = 0; i < state.BlendNodes.Length; i++)
            {
                AnimationBlendNodePayload blend = state.BlendNodes[i];
                values.Add($"blend:{blend.NodeId}:{blend.PolicyId}:{blend.PolicyRevision}:{blend.Transitions.Count}");
            }
            values.Add($"inertial-count:{state.InertializationCount}");
            for (int i = 0; i < state.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = state.Operations[i];
                values.Add(FormattableString.Invariant(
                    $"operation:{operation.Index}:{(int)operation.Phase}:{(int)operation.Code}:{operation.NodeId}:{operation.OutputValueIndex}:{operation.InputValueIndexA}:{operation.InputValueIndexB}:{operation.SelectionInputIndex}:{operation.MarkerSyncOperationIndex}:{operation.ParameterIndex}:{operation.PlayerIndex}:{operation.BlendNodeIndex}:{operation.InertializationIndex}:{operation.BoneMaskIndex}:{operation.AdditiveReferenceIndex}:{operation.ModifyBoneIndex}:{operation.FootPlacementNodeIndex}:{operation.Weight:R}"));
            }
            values.Add(FormattableString.Invariant($"workspace:{selectionWorkspace}:{poseWorkspace}:{parameterWorkspace}:{contributionWorkspace}:{frameCache}:{state.OutputOperationIndex}"));
            return StableHash.Compute(values.ToArray()).ToString();
        }
    }
}
