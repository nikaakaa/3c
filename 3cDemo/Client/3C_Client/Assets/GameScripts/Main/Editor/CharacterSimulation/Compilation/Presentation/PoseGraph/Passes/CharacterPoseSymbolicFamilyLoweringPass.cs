using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [Flags]
    internal enum CharacterPoseSymbolicActorStateRequirement : ushort
    {
        None = 0,
        PoseState = 1 << 0,
        Player = 1 << 1,
        ActionPlayback = 1 << 2,
        AnimationSlot = 1 << 3,
        BlendStack = 1 << 4,
        Transition = 1 << 5,
        Inertialization = 1 << 6,
        RootOrientationWarp = 1 << 7,
        MotionMatching = 1 << 8,
        PoseHistory = 1 << 9,
        LinkedPose = 1 << 10
    }

    [Flags]
    internal enum CharacterPoseSymbolicFrameRequirement : ushort
    {
        None = 0,
        NodeControl = 1 << 0,
        SourceDemand = 1 << 1,
        PoseValue = 1 << 2,
        ParameterValue = 1 << 3,
        PoseDiscontinuity = 1 << 4,
        GoalContribution = 1 << 5,
        GoalSet = 1 << 6,
        Completion = 1 << 7,
        Diagnostics = 1 << 8
    }

    [Flags]
    internal enum CharacterPoseSymbolicWorkspaceRequirement : ushort
    {
        None = 0,
        Pose = 1 << 0,
        Parameter = 1 << 1,
        Contribution = 1 << 2,
        FrameCache = 1 << 3,
        Player = 1 << 4,
        Transition = 1 << 5,
        Inertialization = 1 << 6,
        Source = 1 << 7,
        Constraint = 1 << 8,
        Diagnostics = 1 << 9
    }

    internal readonly struct CharacterPoseSymbolicValueReference :
        IEquatable<CharacterPoseSymbolicValueReference>
    {
        internal CharacterPoseSymbolicValueReference(
            string identity,
            CharacterPosePortKind kind)
        {
            Identity = PoseIdentity.Require(identity, nameof(identity));
            if (!Enum.IsDefined(typeof(CharacterPosePortKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
        }

        internal string Identity { get; }
        internal CharacterPosePortKind Kind { get; }
        internal bool IsValid => !string.IsNullOrEmpty(Identity);

        public bool Equals(CharacterPoseSymbolicValueReference other) =>
            Kind == other.Kind &&
            string.Equals(
                Identity,
                other.Identity,
                StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CharacterPoseSymbolicValueReference other &&
            Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            Identity == null
                ? 0
                : StringComparer.Ordinal.GetHashCode(Identity),
            (int)Kind);
    }

    internal sealed class CharacterPoseSymbolicOperation
    {
        internal CharacterPoseSymbolicOperation(
            int sequence,
            PoseGraphId graphId,
            CharacterPoseIrGraphRole graphRole,
            PoseNodeId nodeId,
            CharacterPoseNodeKind nodeKind,
            CharacterPoseOperationCode operationCode,
            CharacterPoseOperationFamily family,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            IReadOnlyList<CharacterPoseSymbolicValueReference> inputs,
            IReadOnlyList<CharacterPoseSymbolicValueReference> outputs,
            CharacterPoseSymbolicActorStateRequirement actorState,
            CharacterPoseSymbolicFrameRequirement frame,
            CharacterPoseSymbolicWorkspaceRequirement workspace,
            string fragmentIdentity,
            string sourcePath)
        {
            if (sequence < 0 ||
                !graphId.IsValid ||
                !nodeId.IsValid ||
                !Enum.IsDefined(typeof(CharacterPoseIrGraphRole), graphRole) ||
                !Enum.IsDefined(typeof(CharacterPoseNodeKind), nodeKind) ||
                !Enum.IsDefined(
                    typeof(CharacterPoseOperationFamily),
                    family) ||
                family == CharacterPoseOperationFamily.None ||
                !Enum.IsDefined(
                    typeof(CharacterPoseExecutionDomain),
                    executionDomain))
            {
                throw new ArgumentException(
                    "Symbolic Pose Operation identity is invalid.");
            }
            Sequence = sequence;
            GraphId = graphId;
            GraphRole = graphRole;
            NodeId = nodeId;
            NodeKind = nodeKind;
            OperationCode = operationCode;
            Family = family;
            ExecutionDomain = executionDomain;
            InputPoseSpace = inputPoseSpace;
            OutputPoseSpace = outputPoseSpace;
            Inputs = inputs?.ToArray() ??
                Array.Empty<CharacterPoseSymbolicValueReference>();
            Outputs = outputs?.ToArray() ??
                Array.Empty<CharacterPoseSymbolicValueReference>();
            ActorState = actorState;
            Frame = frame;
            Workspace = workspace;
            FragmentIdentity = fragmentIdentity ?? string.Empty;
            SourcePath = PoseIdentity.Require(sourcePath, nameof(sourcePath));
            if (Outputs.GroupBy(value => value).Any(value => value.Count() > 1))
            {
                throw new InvalidOperationException(
                    $"Symbolic Pose Operation '{NodeId}' duplicates an output Value.");
            }
        }

        internal int Sequence { get; }
        internal PoseGraphId GraphId { get; }
        internal CharacterPoseIrGraphRole GraphRole { get; }
        internal PoseNodeId NodeId { get; }
        internal CharacterPoseNodeKind NodeKind { get; }
        internal CharacterPoseOperationCode OperationCode { get; }
        internal CharacterPoseOperationFamily Family { get; }
        internal CharacterPoseExecutionDomain ExecutionDomain { get; }
        internal CharacterPoseSpace InputPoseSpace { get; }
        internal CharacterPoseSpace OutputPoseSpace { get; }
        internal IReadOnlyList<CharacterPoseSymbolicValueReference> Inputs
        {
            get;
        }
        internal IReadOnlyList<CharacterPoseSymbolicValueReference> Outputs
        {
            get;
        }
        internal CharacterPoseSymbolicActorStateRequirement ActorState
        {
            get;
        }
        internal CharacterPoseSymbolicFrameRequirement Frame { get; }
        internal CharacterPoseSymbolicWorkspaceRequirement Workspace
        {
            get;
        }
        internal string FragmentIdentity { get; }
        internal string SourcePath { get; }
    }

    internal sealed class CharacterPoseSymbolicProgram
    {
        internal CharacterPoseSymbolicProgram(
            IReadOnlyList<CharacterPoseSymbolicOperation> operations)
        {
            Operations = operations?.ToArray() ??
                throw new ArgumentNullException(nameof(operations));
            if (Operations.Count == 0)
            {
                throw new InvalidOperationException(
                    "Symbolic Pose Program has no Operations.");
            }
            var outputOwners =
                new HashSet<CharacterPoseSymbolicValueReference>();
            for (int index = 0; index < Operations.Count; index++)
            {
                CharacterPoseSymbolicOperation operation = Operations[index];
                if (operation == null || operation.Sequence != index)
                {
                    throw new InvalidOperationException(
                        "Symbolic Pose Program Operation order is invalid.");
                }
                for (int outputIndex = 0;
                     outputIndex < operation.Outputs.Count;
                     outputIndex++)
                {
                    if (!outputOwners.Add(operation.Outputs[outputIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Symbolic Pose Value '{operation.Outputs[outputIndex].Identity}' has more than one writer.");
                    }
                }
            }
        }

        internal IReadOnlyList<CharacterPoseSymbolicOperation> Operations
        {
            get;
        }
    }

    internal sealed class CharacterPoseSymbolicFamilyLoweringPassResult
    {
        internal CharacterPoseSymbolicFamilyLoweringPassResult(
            CharacterPoseSymbolicProgram program,
            IReadOnlyList<CharacterPoseCompilationDiagnostic> diagnostics)
        {
            Program = program;
            Diagnostics = diagnostics?.ToArray() ??
                Array.Empty<CharacterPoseCompilationDiagnostic>();
            IsSuccess = program != null &&
                Diagnostics.All(value =>
                    value.Severity !=
                    CharacterPoseCompilationDiagnosticSeverity.Error);
        }

        internal bool IsSuccess { get; }
        internal CharacterPoseSymbolicProgram Program { get; }
        internal IReadOnlyList<CharacterPoseCompilationDiagnostic>
            Diagnostics { get; }
    }

    internal static class CharacterPoseSymbolicFamilyLoweringPass
    {
        sealed class GraphResult
        {
            internal GraphResult(
                IReadOnlyDictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference> exports,
                CharacterPoseSymbolicValueReference finalOutput)
            {
                Exports = exports ??
                    throw new ArgumentNullException(nameof(exports));
                FinalOutput = finalOutput;
            }

            internal IReadOnlyDictionary<PoseInterfacePortId,
                CharacterPoseSymbolicValueReference> Exports { get; }
            internal CharacterPoseSymbolicValueReference FinalOutput
            {
                get;
            }
        }

        sealed class LinkedResult
        {
            internal LinkedResult(
                CharacterPoseExecutionDomain domain,
                IReadOnlyList<CharacterPoseSymbolicValueReference> outputs)
            {
                Domain = domain;
                Outputs = outputs?.ToArray() ??
                    Array.Empty<CharacterPoseSymbolicValueReference>();
            }

            internal CharacterPoseExecutionDomain Domain { get; }
            internal IReadOnlyList<CharacterPoseSymbolicValueReference>
                Outputs { get; }
        }

        sealed class Builder
        {
            readonly CharacterPoseCompilationRequest m_Request;
            readonly CharacterPoseGraphClosure m_Closure;
            readonly CharacterPoseTopologyCatalog m_Topology;
            readonly Dictionary<LinkedPoseGroupId,
                CharacterLinkedPoseGroupBinding> m_Groups;
            readonly Dictionary<LinkedPoseImplementationId,
                CharacterLinkedPoseImplementationAsset> m_Implementations;
            readonly Dictionary<LinkedPoseGroupId,
                CharacterLinkedPoseCompiledSelectorDescriptor> m_Selectors;
            readonly List<CharacterPoseSymbolicOperation> m_Operations =
                new List<CharacterPoseSymbolicOperation>();

            internal Builder(
                CharacterPoseCompilationRequest request,
                CharacterPoseGraphClosure closure,
                CharacterPoseTopologyCatalog topology)
            {
                m_Request = request ??
                    throw new ArgumentNullException(nameof(request));
                m_Closure = closure ??
                    throw new ArgumentNullException(nameof(closure));
                m_Topology = topology ??
                    throw new ArgumentNullException(nameof(topology));
                m_Groups = request.Profile.LinkedPoseGroups.ToDictionary(
                    value => value.GroupId);
                m_Implementations =
                    request.Profile.LinkedPoseImplementations.ToDictionary(
                        value => value.ImplementationId);
                m_Selectors = request.LinkedPose.Selectors.ToDictionary(
                    value => value.GroupId);
            }

            internal CharacterPoseSymbolicProgram Build()
            {
                CompileGraph(
                    m_Request.Asset,
                    m_Request.Asset.Graph,
                    CharacterPoseIrGraphRole.Root,
                    new Dictionary<PoseInterfacePortId,
                        CharacterPoseSymbolicValueReference>(),
                    string.Empty,
                    string.Empty);
                return new CharacterPoseSymbolicProgram(m_Operations);
            }

            GraphResult CompileGraph(
                CharacterPresentationPoseGraphAsset owner,
                CharacterTypedPoseGraph graph,
                CharacterPoseIrGraphRole role,
                IReadOnlyDictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference> imports,
                string scope,
                string fragmentIdentity)
            {
                CharacterPoseIrGraph ordered = m_Topology.RequireGraph(
                    owner,
                    graph.GraphId,
                    role);
                Dictionary<PoseNodeId, CharacterTypedPoseNode> nodes =
                    graph.Nodes.ToDictionary(value => value.NodeId);
                var values = new Dictionary<string,
                    CharacterPoseSymbolicValueReference>(
                    StringComparer.Ordinal);
                var exports = new Dictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference>();
                CharacterPoseSymbolicValueReference finalOutput = default;
                for (int nodeIndex = 0;
                     nodeIndex < ordered.Nodes.Count;
                     nodeIndex++)
                {
                    CharacterPoseIrNode irNode = ordered.Nodes[nodeIndex];
                    CharacterTypedPoseNode node = nodes[
                        new PoseNodeId(irNode.NodeId.Value)];
                    CharacterPoseNodeDefinition definition =
                        CharacterPoseNodeDefinitionModule.Shared
                            .RequireCapability(irNode.CapabilityIdentity);
                    if (definition.NativeRole ==
                        CharacterPoseNativeNodeRole.GraphInput)
                    {
                        BindGraphInputs(node, imports, scope, values);
                        continue;
                    }
                    if (definition.NativeRole ==
                        CharacterPoseNativeNodeRole.GraphOutput)
                    {
                        BindGraphOutputs(
                            node,
                            irNode,
                            scope,
                            values,
                            exports);
                        continue;
                    }
                    if (definition.NativeRole ==
                        CharacterPoseNativeNodeRole.Subgraph)
                    {
                        CompileSubgraph(
                            owner,
                            graph,
                            node,
                            irNode,
                            scope,
                            fragmentIdentity,
                            values);
                        continue;
                    }

                    PoseNodeId scopedNodeId = ScopeNodeId(
                        node.NodeId,
                        scope);
                    var hiddenInputs =
                        new List<CharacterPoseSymbolicValueReference>();
                    CharacterPoseExecutionDomain domain =
                        definition.ExecutionDomain;
                    if (definition.Kind ==
                        CharacterPoseNodeKind.LinkedPoseCall)
                    {
                        LinkedResult linked = CompileLinkedEntries(
                            owner,
                            graph,
                            node,
                            irNode,
                            scope,
                            values);
                        domain = linked.Domain;
                        hiddenInputs.AddRange(linked.Outputs);
                    }
                    if (definition.StateMachine)
                    {
                        hiddenInputs.AddRange(CompileStateGraphs(
                            owner,
                            node,
                            scope,
                            fragmentIdentity));
                    }
                    CharacterPoseOperationCode code =
                        definition.NativeRole ==
                            CharacterPoseNativeNodeRole.PoseOutput &&
                        role == CharacterPoseIrGraphRole.StateLocal
                            ? CharacterPoseOperationCode.StatePoseOutput
                            : definition.OperationCode;
                    if (code == CharacterPoseOperationCode.StatePoseOutput)
                        domain = CharacterPoseExecutionDomain.PurePose;
                    var inputs = new List<
                        CharacterPoseSymbolicValueReference>(
                        irNode.Inputs.Count + hiddenInputs.Count);
                    for (int inputIndex = 0;
                         inputIndex < irNode.Inputs.Count;
                         inputIndex++)
                    {
                        CharacterPoseIrInput input = irNode.Inputs[inputIndex];
                        string key = EndpointKey(
                            new PoseNodeId(input.SourceNodeId.Value),
                            new PosePortId(input.SourcePortId),
                            scope);
                        inputs.Add(values.TryGetValue(
                                key,
                                out CharacterPoseSymbolicValueReference value)
                            ? value
                            : new CharacterPoseSymbolicValueReference(
                                key,
                                input.ValueKind));
                    }
                    inputs.AddRange(hiddenInputs);
                    IReadOnlyList<CharacterPoseSymbolicValueReference> outputs =
                        BindOperationOutputs(
                            node,
                            scope,
                            values,
                            definition.NativeRole ==
                            CharacterPoseNativeNodeRole.PoseOutput);
                    var operation = new CharacterPoseSymbolicOperation(
                        m_Operations.Count,
                        graph.GraphId,
                        role,
                        scopedNodeId,
                        definition.Kind,
                        code,
                        definition.OperationFamily,
                        domain,
                        ResolvePoseSpace(
                            node,
                            CharacterPosePortDirection.Input),
                        ResolveOutputPoseSpace(node, code),
                        inputs,
                        outputs,
                        ResolveActorState(definition),
                        ResolveFrame(definition, inputs, outputs),
                        ResolveWorkspace(definition),
                        fragmentIdentity,
                        irNode.SourcePath);
                    m_Operations.Add(operation);
                    if (definition.NativeRole ==
                        CharacterPoseNativeNodeRole.PoseOutput)
                    {
                        finalOutput = outputs.Single(value =>
                            value.Kind == CharacterPosePortKind.LocalPose ||
                            value.Kind ==
                            CharacterPosePortKind.ComponentPose);
                    }
                }
                return new GraphResult(exports, finalOutput);
            }

            IReadOnlyList<CharacterPoseSymbolicValueReference>
                CompileStateGraphs(
                    CharacterPresentationPoseGraphAsset owner,
                    CharacterTypedPoseNode node,
                    string scope,
                    string fragmentIdentity)
            {
                var payload =
                    (CharacterPoseStateMachineNodePayload)node.Payload;
                PoseNodeId scopedNodeId = ScopeNodeId(node.NodeId, scope);
                var outputs =
                    new List<CharacterPoseSymbolicValueReference>();
                foreach (CharacterPoseStateDefinition state in
                         payload.StateMachine.States.OrderBy(
                             value => value.StateId))
                {
                    CharacterTypedPoseGraph stateGraph =
                        m_Closure.RequireGraph(owner, state.PoseGraphId);
                    string stateScope = scopedNodeId.Value +
                        "/state/" + state.StateId.Value;
                    GraphResult result = CompileGraph(
                        owner,
                        stateGraph,
                        CharacterPoseIrGraphRole.StateLocal,
                        new Dictionary<PoseInterfacePortId,
                            CharacterPoseSymbolicValueReference>(),
                        stateScope,
                        fragmentIdentity);
                    if (!result.FinalOutput.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Pose State '{state.StateId}' has no symbolic output.");
                    }
                    outputs.Add(result.FinalOutput);
                }
                return outputs;
            }

            LinkedResult CompileLinkedEntries(
                CharacterPresentationPoseGraphAsset owner,
                CharacterTypedPoseGraph graph,
                CharacterTypedPoseNode node,
                CharacterPoseIrNode irNode,
                string scope,
                IReadOnlyDictionary<string,
                    CharacterPoseSymbolicValueReference> values)
            {
                var payload = (CharacterLinkedPoseCallPayload)node.Payload;
                CharacterLinkedPoseGroupBinding group =
                    m_Groups[payload.GroupId];
                CharacterLinkedPoseInterfaceEntryDescriptor entry =
                    group.Interface.RequireEntry(payload.EntryId);
                CharacterLinkedPoseCompiledSelectorDescriptor selector =
                    m_Selectors[payload.GroupId];
                Dictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference> imports =
                    BuildImports(node, irNode, scope, values);
                PoseNodeId callNode = ScopeNodeId(node.NodeId, scope);
                var outputs =
                    new List<CharacterPoseSymbolicValueReference>();
                for (int candidateIndex = 0;
                     candidateIndex <
                     selector.CandidateImplementationIds.Count;
                     candidateIndex++)
                {
                    var implementationId = new LinkedPoseImplementationId(
                        selector.CandidateImplementationIds[candidateIndex]);
                    CharacterLinkedPoseImplementationAsset implementation =
                        m_Implementations[implementationId];
                    CharacterLinkedPoseImplementationEntryBinding binding =
                        implementation.RequireEntry(payload.EntryId);
                    CharacterTypedPoseGraph entryGraph =
                        m_Closure.RequireGraph(
                            binding.GraphOwner,
                            binding.GraphId);
                    string fragment = string.Join(
                        "/",
                        callNode.Value,
                        "linked",
                        payload.GroupId.Value,
                        implementationId.Value,
                        payload.EntryId.Value);
                    GraphResult result = CompileGraph(
                        binding.GraphOwner,
                        entryGraph,
                        CharacterPoseIrGraphRole.LinkedPoseEntry,
                        imports,
                        fragment,
                        fragment);
                    for (int portIndex = 0;
                         portIndex < entry.Ports.Count;
                         portIndex++)
                    {
                        CharacterLinkedPoseInterfacePortDescriptor port =
                            entry.Ports[portIndex];
                        if (port.Direction !=
                            CharacterPosePortDirection.Output)
                        {
                            continue;
                        }
                        if (result.Exports.TryGetValue(
                                port.PortId,
                                out CharacterPoseSymbolicValueReference value))
                        {
                            outputs.Add(value);
                        }
                    }
                }
                return new LinkedResult(
                    entry.ExecutionDomain,
                    outputs);
            }

            void CompileSubgraph(
                CharacterPresentationPoseGraphAsset owner,
                CharacterTypedPoseGraph graph,
                CharacterTypedPoseNode node,
                CharacterPoseIrNode irNode,
                string scope,
                string fragmentIdentity,
                Dictionary<string,
                    CharacterPoseSymbolicValueReference> values)
            {
                CharacterTypedPoseGraph child = m_Closure.RequireGraph(
                    owner,
                    node.Subgraph.PoseGraphId);
                Dictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference> imports =
                    BuildImports(node, irNode, scope, values);
                PoseNodeId scopedCall = ScopeNodeId(node.NodeId, scope);
                GraphResult result = CompileGraph(
                    owner,
                    child,
                    CharacterPoseIrGraphRole.Subgraph,
                    imports,
                    scopedCall.Value + "/" + child.GraphId,
                    fragmentIdentity);
                foreach (CharacterPosePortDefinition port in
                         CharacterPoseAuthoringPortProjection.Get(node))
                {
                    if (port.Direction !=
                        CharacterPosePortDirection.Output)
                    {
                        continue;
                    }
                    if (!result.Exports.TryGetValue(
                            port.InterfacePortId,
                            out CharacterPoseSymbolicValueReference value))
                    {
                        throw new InvalidOperationException(
                            $"Pose Subgraph '{node.NodeId}' has no symbolic output '{port.InterfacePortId}'.");
                    }
                    values.Add(
                        EndpointKey(node.NodeId, port.PortId, scope),
                        value);
                }
            }

            static Dictionary<PoseInterfacePortId,
                CharacterPoseSymbolicValueReference> BuildImports(
                CharacterTypedPoseNode node,
                CharacterPoseIrNode irNode,
                string scope,
                IReadOnlyDictionary<string,
                    CharacterPoseSymbolicValueReference> values)
            {
                var imports = new Dictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference>();
                foreach (CharacterPosePortDefinition port in
                         CharacterPoseAuthoringPortProjection.Get(node))
                {
                    if (port.Direction != CharacterPosePortDirection.Input)
                        continue;
                    CharacterPoseIrInput input = irNode.Inputs.SingleOrDefault(
                        value => string.Equals(
                            value.TargetPortId,
                            port.PortId.Value,
                            StringComparison.Ordinal));
                    if (input == null)
                        continue;
                    string key = EndpointKey(
                        new PoseNodeId(input.SourceNodeId.Value),
                        new PosePortId(input.SourcePortId),
                        scope);
                    if (!values.TryGetValue(
                            key,
                            out CharacterPoseSymbolicValueReference value))
                    {
                        throw new InvalidOperationException(
                            $"Pose call input '{port.PortId}' has no symbolic source.");
                    }
                    imports.Add(port.InterfacePortId, value);
                }
                return imports;
            }

            static void BindGraphInputs(
                CharacterTypedPoseNode node,
                IReadOnlyDictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference> imports,
                string scope,
                IDictionary<string,
                    CharacterPoseSymbolicValueReference> values)
            {
                foreach (CharacterPosePortDefinition port in
                         CharacterPoseAuthoringPortProjection.Get(node))
                {
                    if (port.Direction != CharacterPosePortDirection.Output)
                        continue;
                    if (!imports.TryGetValue(
                            port.InterfacePortId,
                            out CharacterPoseSymbolicValueReference value))
                    {
                        if (port.Required)
                        {
                            throw new InvalidOperationException(
                                $"Graph Input '{port.InterfacePortId}' has no symbolic import.");
                        }
                        continue;
                    }
                    values.Add(
                        EndpointKey(node.NodeId, port.PortId, scope),
                        value);
                }
            }

            static void BindGraphOutputs(
                CharacterTypedPoseNode node,
                CharacterPoseIrNode irNode,
                string scope,
                IReadOnlyDictionary<string,
                    CharacterPoseSymbolicValueReference> values,
                IDictionary<PoseInterfacePortId,
                    CharacterPoseSymbolicValueReference> exports)
            {
                foreach (CharacterPosePortDefinition port in
                         CharacterPoseAuthoringPortProjection.Get(node))
                {
                    if (port.Direction != CharacterPosePortDirection.Input)
                        continue;
                    CharacterPoseIrInput input = irNode.Inputs.SingleOrDefault(
                        value => string.Equals(
                            value.TargetPortId,
                            port.PortId.Value,
                            StringComparison.Ordinal));
                    if (input == null)
                        continue;
                    string key = EndpointKey(
                        new PoseNodeId(input.SourceNodeId.Value),
                        new PosePortId(input.SourcePortId),
                        scope);
                    if (!values.TryGetValue(
                            key,
                            out CharacterPoseSymbolicValueReference value))
                    {
                        throw new InvalidOperationException(
                            $"Graph Output '{port.InterfacePortId}' has no symbolic source.");
                    }
                    exports.Add(port.InterfacePortId, value);
                }
            }

            static IReadOnlyList<CharacterPoseSymbolicValueReference>
                BindOperationOutputs(
                    CharacterTypedPoseNode node,
                    string scope,
                    IDictionary<string,
                        CharacterPoseSymbolicValueReference> values,
                    bool publishFinalPose)
            {
                var outputs =
                    new List<CharacterPoseSymbolicValueReference>();
                foreach (CharacterPosePortDefinition port in
                         CharacterPoseAuthoringPortProjection.Get(node))
                {
                    if (port.Direction != CharacterPosePortDirection.Output)
                        continue;
                    string identity = EndpointKey(
                        node.NodeId,
                        port.PortId,
                        scope);
                    var value = new CharacterPoseSymbolicValueReference(
                        identity,
                        port.Kind);
                    values.Add(identity, value);
                    outputs.Add(value);
                }
                if (publishFinalPose)
                {
                    string identity = ScopeNodeId(node.NodeId, scope).Value +
                        "\0__final-pose";
                    var value = new CharacterPoseSymbolicValueReference(
                        identity,
                        CharacterPosePortKind.LocalPose);
                    values.Add(identity, value);
                    outputs.Add(value);
                }
                return outputs;
            }

            static CharacterPoseSymbolicActorStateRequirement
                ResolveActorState(CharacterPoseNodeDefinition definition)
            {
                CharacterPoseSymbolicActorStateRequirement result =
                    CharacterPoseSymbolicActorStateRequirement.None;
                if (definition.StateMachine)
                    result |= CharacterPoseSymbolicActorStateRequirement.PoseState |
                              CharacterPoseSymbolicActorStateRequirement.Transition;
                if (definition.Player)
                    result |= CharacterPoseSymbolicActorStateRequirement.Player;
                if (definition.Kind ==
                    CharacterPoseNodeKind.ActionPlaybackInput)
                {
                    result |= CharacterPoseSymbolicActorStateRequirement
                        .ActionPlayback;
                }
                if (definition.AnimationSlot)
                {
                    result |= CharacterPoseSymbolicActorStateRequirement
                                  .AnimationSlot |
                              CharacterPoseSymbolicActorStateRequirement
                                  .Transition;
                }
                if (definition.BlendPolicy)
                    result |= CharacterPoseSymbolicActorStateRequirement.BlendStack;
                if (definition.Inertialization)
                    result |= CharacterPoseSymbolicActorStateRequirement.Inertialization;
                if (definition.RootOrientationWarp)
                    result |= CharacterPoseSymbolicActorStateRequirement.RootOrientationWarp;
                if (definition.Kind == CharacterPoseNodeKind.MotionMatchingPose)
                    result |= CharacterPoseSymbolicActorStateRequirement.MotionMatching;
                if (definition.Kind == CharacterPoseNodeKind.PoseHistoryCollector)
                    result |= CharacterPoseSymbolicActorStateRequirement.PoseHistory;
                if (definition.Kind == CharacterPoseNodeKind.LinkedPoseCall)
                    result |= CharacterPoseSymbolicActorStateRequirement.LinkedPose;
                return result;
            }

            static CharacterPoseSymbolicFrameRequirement ResolveFrame(
                CharacterPoseNodeDefinition definition,
                IReadOnlyList<CharacterPoseSymbolicValueReference> inputs,
                IReadOnlyList<CharacterPoseSymbolicValueReference> outputs)
            {
                CharacterPoseSymbolicFrameRequirement result =
                    CharacterPoseSymbolicFrameRequirement.Completion |
                    CharacterPoseSymbolicFrameRequirement.Diagnostics;
                if (definition.Player || definition.BlendPolicy)
                {
                    result |= CharacterPoseSymbolicFrameRequirement.NodeControl |
                              CharacterPoseSymbolicFrameRequirement.SourceDemand;
                }
                foreach (CharacterPoseSymbolicValueReference value in
                         inputs.Concat(outputs))
                {
                    result |= value.Kind switch
                    {
                        CharacterPosePortKind.LocalPose or
                        CharacterPosePortKind.ComponentPose =>
                            CharacterPoseSymbolicFrameRequirement.PoseValue,
                        CharacterPosePortKind.Parameter =>
                            CharacterPoseSymbolicFrameRequirement.ParameterValue,
                        CharacterPosePortKind.PoseDiscontinuity =>
                            CharacterPoseSymbolicFrameRequirement.PoseDiscontinuity,
                        CharacterPosePortKind.FullBodyIkGoalContribution =>
                            CharacterPoseSymbolicFrameRequirement.GoalContribution,
                        CharacterPosePortKind.FullBodyIkGoals =>
                            CharacterPoseSymbolicFrameRequirement.GoalSet,
                        _ => CharacterPoseSymbolicFrameRequirement.None
                    };
                }
                return result;
            }

            static CharacterPoseSymbolicWorkspaceRequirement ResolveWorkspace(
                CharacterPoseNodeDefinition definition)
            {
                CharacterPoseSymbolicWorkspaceRequirement result =
                    CharacterPoseSymbolicWorkspaceRequirement.FrameCache |
                    CharacterPoseSymbolicWorkspaceRequirement.Diagnostics;
                if (definition.Player || definition.BlendPolicy)
                {
                    result |= CharacterPoseSymbolicWorkspaceRequirement.Pose |
                              CharacterPoseSymbolicWorkspaceRequirement.Parameter |
                              CharacterPoseSymbolicWorkspaceRequirement.Contribution |
                              CharacterPoseSymbolicWorkspaceRequirement.Player |
                              CharacterPoseSymbolicWorkspaceRequirement.Source;
                }
                if (definition.StateMachine || definition.AnimationSlot)
                    result |= CharacterPoseSymbolicWorkspaceRequirement.Transition;
                if (definition.Inertialization)
                    result |= CharacterPoseSymbolicWorkspaceRequirement.Inertialization;
                if (definition.OperationFamily ==
                        CharacterPoseOperationFamily.GoalContribution ||
                    definition.OperationFamily ==
                        CharacterPoseOperationFamily.GoalAssembler ||
                    definition.OperationFamily ==
                        CharacterPoseOperationFamily.FullBodyIk)
                {
                    result |= CharacterPoseSymbolicWorkspaceRequirement.Constraint;
                }
                return result;
            }

            static CharacterPoseSpace ResolveOutputPoseSpace(
                CharacterTypedPoseNode node,
                CharacterPoseOperationCode code)
            {
                CharacterPoseSpace result = ResolvePoseSpace(
                    node,
                    CharacterPosePortDirection.Output);
                if (result != CharacterPoseSpace.None)
                    return result;
                return code == CharacterPoseOperationCode.StatePoseOutput ||
                       code == CharacterPoseOperationCode.OutputPose
                    ? CharacterPoseSpace.Local
                    : CharacterPoseSpace.None;
            }

            static CharacterPoseSpace ResolvePoseSpace(
                CharacterTypedPoseNode node,
                CharacterPosePortDirection direction)
            {
                CharacterPoseSpace result = CharacterPoseSpace.None;
                foreach (CharacterPosePortDefinition port in
                         CharacterPoseAuthoringPortProjection.Get(node))
                {
                    if (port.Direction != direction ||
                        port.Kind != CharacterPosePortKind.LocalPose &&
                        port.Kind != CharacterPosePortKind.ComponentPose)
                    {
                        continue;
                    }
                    CharacterPoseSpace candidate =
                        port.Kind == CharacterPosePortKind.LocalPose
                            ? CharacterPoseSpace.Local
                            : CharacterPoseSpace.Component;
                    if (result != CharacterPoseSpace.None &&
                        result != candidate)
                    {
                        throw new InvalidOperationException(
                            $"Pose Node '{node.NodeId}' mixes Pose spaces.");
                    }
                    result = candidate;
                }
                return result;
            }

            static string EndpointKey(
                PoseNodeId nodeId,
                PosePortId portId,
                string scope) =>
                ScopeNodeId(nodeId, scope).Value +
                "\0" +
                ScopePortId(portId, scope).Value;

            static PoseNodeId ScopeNodeId(
                PoseNodeId nodeId,
                string scope) =>
                string.IsNullOrEmpty(scope)
                    ? nodeId
                    : new PoseNodeId(scope + "/" + nodeId.Value);

            static PosePortId ScopePortId(
                PosePortId portId,
                string scope) =>
                string.IsNullOrEmpty(scope)
                    ? portId
                    : new PosePortId(scope + "/" + portId.Value);
        }

        internal static CharacterPoseSymbolicFamilyLoweringPassResult Run(
            CharacterPoseCompilationRequest request,
            CharacterPoseGraphClosure closure,
            CharacterPoseTopologyCatalog topology)
        {
            try
            {
                CharacterPoseSymbolicProgram program =
                    new Builder(request, closure, topology).Build();
                return new CharacterPoseSymbolicFamilyLoweringPassResult(
                    program,
                    Array.Empty<CharacterPoseCompilationDiagnostic>());
            }
            catch (Exception exception)
            {
                return new CharacterPoseSymbolicFamilyLoweringPassResult(
                    null,
                    new[]
                    {
                        new CharacterPoseCompilationDiagnostic(
                            CharacterPoseCompilationPass.SymbolicFamilyLowering,
                            CharacterPoseCompilationDiagnosticSeverity.Error,
                            "symbolic-family-lowering-invalid",
                            exception.Message,
                            request?.Asset?.Graph?.GraphId ?? default)
                    });
            }
        }
    }
}
