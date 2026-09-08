using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPoseFamilyPayloadPlanPass
    {
        static readonly CharacterPoseFamilyPayloadAdapterCatalog s_FamilyPayloadAdapters =
            new CharacterPoseFamilyPayloadAdapterCatalog();

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

        readonly struct LinkedPoseCallCompilation
        {
            public LinkedPoseCallCompilation(int callIndex, CharacterPoseExecutionDomain executionDomain)
            {
                CallIndex = callIndex;
                ExecutionDomain = executionDomain;
            }

            public int CallIndex { get; }
            public CharacterPoseExecutionDomain ExecutionDomain { get; }
        }

        sealed class ExpandedStateTransition
        {
            public ExpandedStateTransition(
                CharacterPoseStateTransition authored,
                PoseStateId sourceStateId)
            {
                Authored = authored;
                SourceStateId = sourceStateId;
            }

            public CharacterPoseStateTransition Authored { get; }
            public PoseStateId SourceStateId { get; }
        }

        internal sealed class BindingBuilder
        {
            public BindingBuilder(
                CharacterPresentationPoseGraphAsset graphAsset,
                CharacterPoseGraphClosure graphClosure,
                CharacterPoseTopologyCatalog topology,
                CharacterPoseSymbolicProgram symbolicProgram,
                CharacterAnimationRigDefinition rig,
                CharacterPresentationPoseParameterEntry[] parameters,
                Dictionary<PoseParameterId, int> parameterIndices,
                AnimationBlendNodePayload[] blendNodes,
                CharacterPresentationPoseSourcePlan[] poseSources,
                AnimationClipPhasePlan[] clipPhasePlans,
                AnimationSourcePhasePlan[] sourcePhasePlans,
                AnimationFootPhaseValidationDescriptor[] clipPhaseValidations,
                IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> sourceIndices,
                IReadOnlyDictionary<string, int> curveIndices,
                IReadOnlyDictionary<string, int> profileIndicesByIdentity,
                CharacterAnimationPresentationProfile profile,
                CharacterLinkedPoseProjectionPayload linkedPose,
                CharacterFootPlacementAnalysisCompilation footAnalysis,
                IReadOnlyList<string> movementModeStateIdentities)
            {
                GraphAsset = graphAsset;
                GraphClosure = graphClosure ??
                    throw new ArgumentNullException(nameof(graphClosure));
                Topology = topology ??
                    throw new ArgumentNullException(nameof(topology));
                SymbolicProgram = symbolicProgram ??
                    throw new ArgumentNullException(nameof(symbolicProgram));
                Rig = rig;
                Parameters = parameters;
                ParameterIndices = parameterIndices;
                BlendNodes = blendNodes;
                BlendNodeIndices = blendNodes
                    .Select((value, index) => new KeyValuePair<PoseNodeId, int>(value.NodeId, index))
                    .ToDictionary(value => value.Key, value => value.Value);
                PoseSources = poseSources.ToDictionary(value => value.SourceIndex);
                ClipPhasePlans = clipPhasePlans ?? Array.Empty<AnimationClipPhasePlan>();
                SourcePhasePlans = sourcePhasePlans ?? Array.Empty<AnimationSourcePhasePlan>();
                ClipPhaseValidations = clipPhaseValidations ??
                    Array.Empty<AnimationFootPhaseValidationDescriptor>();
                if (ClipPhaseValidations.Count != ClipPhasePlans.Count)
                    throw new InvalidOperationException("Animation Clip Phase validation catalog does not match the compiled plan catalog.");
                SourcePhasePlanIndices = SourcePhasePlans
                    .Select((value, index) => new KeyValuePair<PresentationPoseSourceIndex, int>(value.SourceIndex, index))
                    .ToDictionary(value => value.Key, value => value.Value);
                SourceIndices = sourceIndices ?? throw new ArgumentNullException(nameof(sourceIndices));
                CurveIndices = curveIndices ?? throw new ArgumentNullException(nameof(curveIndices));
                ProfileIndicesByIdentity = profileIndicesByIdentity ??
                    throw new ArgumentNullException(nameof(profileIndicesByIdentity));
                Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
                LinkedPose = linkedPose ?? throw new ArgumentNullException(nameof(linkedPose));
                FootAnalysis = footAnalysis ?? throw new ArgumentNullException(nameof(footAnalysis));
                MovementModeStateIdentities = movementModeStateIdentities ??
                    throw new ArgumentNullException(nameof(movementModeStateIdentities));
                LinkedGroups = profile.LinkedPoseGroups.ToDictionary(value => value.GroupId);
                LinkedImplementations = profile.LinkedPoseImplementations.ToDictionary(value => value.ImplementationId);
            }

            public CharacterPresentationPoseGraphAsset GraphAsset { get; }
            public CharacterPoseGraphClosure GraphClosure { get; }
            public CharacterPoseTopologyCatalog Topology { get; }
            public CharacterPoseSymbolicProgram SymbolicProgram { get; }
            public CharacterAnimationRigDefinition Rig { get; }
            public CharacterPresentationPoseParameterEntry[] Parameters { get; }
            public Dictionary<PoseParameterId, int> ParameterIndices { get; }
            public AnimationBlendNodePayload[] BlendNodes { get; }
            public Dictionary<PoseNodeId, int> BlendNodeIndices { get; }
            public Dictionary<PresentationPoseSourceIndex, CharacterPresentationPoseSourcePlan> PoseSources { get; }
            public IReadOnlyList<AnimationClipPhasePlan> ClipPhasePlans { get; }
            public IReadOnlyList<AnimationSourcePhasePlan> SourcePhasePlans { get; }
            public IReadOnlyList<AnimationFootPhaseValidationDescriptor> ClipPhaseValidations { get; }
            public Dictionary<PresentationPoseSourceIndex, int> SourcePhasePlanIndices { get; }
            public IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> SourceIndices { get; }
            public IReadOnlyDictionary<string, int> CurveIndices { get; }
            public IReadOnlyDictionary<string, int> ProfileIndicesByIdentity { get; }
            public CharacterAnimationPresentationProfile Profile { get; }
            public CharacterLinkedPoseProjectionPayload LinkedPose { get; }
            public CharacterFootPlacementAnalysisCompilation FootAnalysis { get; }
            public IReadOnlyList<string> MovementModeStateIdentities { get; }
            public Dictionary<LinkedPoseGroupId, CharacterLinkedPoseGroupBinding> LinkedGroups { get; }
            public Dictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationAsset> LinkedImplementations { get; }
            public List<CharacterLinkedPoseEntryFragmentPlanDescriptor> LinkedFragments { get; } = new List<CharacterLinkedPoseEntryFragmentPlanDescriptor>();
            public List<CharacterLinkedPoseCallPlanDescriptor> LinkedCalls { get; } = new List<CharacterLinkedPoseCallPlanDescriptor>();
            public List<CharacterPresentationDenseBoneMask> Masks { get; } = new List<CharacterPresentationDenseBoneMask>();
            public Dictionary<string, int> MaskIndices { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
            public List<CharacterPresentationAdditiveReferenceDescriptor> AdditiveReferences { get; } = new List<CharacterPresentationAdditiveReferenceDescriptor>();
            public int InertializationCount { get; set; }
            public List<CharacterPresentationModifyBoneDescriptor> ModifyBones { get; } = new List<CharacterPresentationModifyBoneDescriptor>();
            public List<CharacterPresentationRootOrientationWarpDescriptor> RootOrientationWarps { get; } = new List<CharacterPresentationRootOrientationWarpDescriptor>();
            public List<CharacterPresentationPoseBoneIkGoalsDescriptor> PoseBoneIkGoalSources { get; } = new List<CharacterPresentationPoseBoneIkGoalsDescriptor>();
            public List<CharacterPresentationFootPlacementDescriptor> FootPlacements { get; } = new List<CharacterPresentationFootPlacementDescriptor>();
            public List<CharacterPresentationFullBodyIkDescriptor> FullBodyIks { get; } = new List<CharacterPresentationFullBodyIkDescriptor>();
            public List<int> FullBodyIkGoalContributionInputValueIndices { get; } =
                new List<int>();
            public List<CharacterPresentationClipPlayerDescriptor> ClipPlayers { get; } = new List<CharacterPresentationClipPlayerDescriptor>();
            public List<CharacterPoseStateMachineDescriptor> StateMachines { get; } =
                new List<CharacterPoseStateMachineDescriptor>();
            public List<CharacterAnimationSlotDescriptor> AnimationSlots { get; } =
                new List<CharacterAnimationSlotDescriptor>();
            public List<ActionPlaybackInputPlan> ActionPlaybackInputs { get; } =
                new List<ActionPlaybackInputPlan>();
            public List<CharacterPoseBoundOperation> Operations { get; } = new List<CharacterPoseBoundOperation>();
            public List<CharacterPresentationPoseSourceMapEntry> SourceMap { get; } = new List<CharacterPresentationPoseSourceMapEntry>();
            public List<string> GraphDependencies { get; } = new List<string>();
            public int PoseValueCount { get; set; }
            public int FullBodyIkGoalContributionValueCount { get; set; }
            public int FullBodyIkGoalSetValueCount { get; set; }
            public int FullBodyIkGoalContributionGoalWorkspaceCount { get; set; }
            public int PlayerCount { get; set; }
            public int OutputOperationIndex { get; set; } = -1;
            public int SymbolicOperationCursor { get; set; }
        }

        internal static CharacterPoseFamilyPayloadPlan Run(
            CharacterPoseCompilationRequest request,
            CharacterPoseGraphClosure graphClosure,
            CharacterPoseTopologyCatalog topology,
            CharacterPoseSymbolicProgram symbolicProgram)
        {
            CharacterPresentationPoseGraphAsset asset = request.AuthoringView.OwnerAsset;
            CharacterAnimationRigDefinition rig = request.Rig;
            AnimationBlendNodePayload[] blendNodes = request.BlendNodes;
            CharacterPoseCanvasGraph graph = asset.Graph;
            CharacterAnimationParameterLayout parameterLayout =
                CharacterAnimationParameterLayoutCompiler.Build(graph);
            CharacterPoseParameterDeclaration[] authoredParameters = parameterLayout.Declarations;
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
                    parameter.Unit,
                    parameter.Usage);
                parameterIndices.Add(parameter.ParameterId, i);
            }
            var state = new BindingBuilder(
                asset,
                graphClosure,
                topology,
                symbolicProgram,
                rig,
                parameters,
                parameterIndices,
                blendNodes,
                request.PoseSources,
                request.ClipPhasePlans,
                request.SourcePhasePlans,
                request.ClipPhaseValidations,
                request.SourceIndices,
                request.CurveIndices,
                request.ProfileIndicesByIdentity,
                request.Profile,
                request.LinkedPose,
                request.FootAnalysis,
                request.MovementModeStateIdentities);
            CompileGraph(
                state,
                asset,
                graph,
                new Dictionary<PoseInterfacePortId, CompiledValue>(),
                string.Empty,
                string.Empty,
                true);
            if (state.SymbolicOperationCursor !=
                state.SymbolicProgram.Operations.Count)
            {
                throw new InvalidOperationException(
                    "Pose compiler did not consume the complete Symbolic Program.");
            }
            if (state.OutputOperationIndex < 0 || state.PoseValueCount <= 0)
                throw new InvalidOperationException("Pose Plan has no complete Pose and Output boundary.");
            if (state.BlendNodeIndices.Count != state.BlendNodes.Length)
                throw new InvalidOperationException("Pose Plan Blend Stack payload identities are not unique.");

            var payloads = new CharacterPoseBoundFamilyPayloads(
                state.Parameters,
                state.BlendNodes,
                state.Masks.ToArray(),
                state.AdditiveReferences.ToArray(),
                state.ModifyBones.ToArray(),
                state.RootOrientationWarps.ToArray(),
                state.PoseBoneIkGoalSources.ToArray(),
                state.FootPlacements.ToArray(),
                state.FullBodyIks.ToArray(),
                state.FullBodyIkGoalContributionInputValueIndices.ToArray(),
                state.ClipPlayers.ToArray(),
                state.StateMachines.ToArray(),
                state.AnimationSlots.ToArray(),
                state.ActionPlaybackInputs.ToArray(),
                state.LinkedFragments.ToArray(),
                state.LinkedCalls.ToArray());
            var layout = new CharacterPoseBoundProgramLayout(
                state.PoseSources.Count,
                state.PoseValueCount,
                state.FullBodyIkGoalContributionValueCount,
                state.FullBodyIkGoalSetValueCount,
                state.FullBodyIkGoalContributionGoalWorkspaceCount,
                state.PlayerCount,
                state.InertializationCount,
                state.OutputOperationIndex);
            return new CharacterPoseFamilyPayloadPlan(
                payloads,
                state.Operations.ToArray(),
                state.SourceMap.ToArray(),
                state.GraphDependencies.ToArray(),
                in layout);
        }

        static CharacterPoseSpace ResolveInputPoseSpace(CharacterPoseCanvasNode node) =>
            ResolvePoseSpace(node, CharacterPosePortDirection.Input);

        static CharacterPoseSpace ResolveOutputPoseSpace(
            CharacterPoseCanvasNode node,
            CharacterPoseOperationCode code)
        {
            CharacterPoseSpace result = ResolvePoseSpace(node, CharacterPosePortDirection.Output);
            if (result != CharacterPoseSpace.None)
                return result;
            return code == CharacterPoseOperationCode.StatePoseOutput ||
                   code == CharacterPoseOperationCode.OutputPose
                ? CharacterPoseSpace.Local
                : CharacterPoseSpace.None;
        }

        static CharacterPoseSpace ResolvePoseSpace(
            CharacterPoseCanvasNode node,
            CharacterPosePortDirection direction)
        {
            CharacterPoseSpace result = CharacterPoseSpace.None;
            foreach (CharacterPosePortDefinition port in CharacterPoseAuthoringPortProjection.Get(node))
            {
                if (port.Direction != direction ||
                    port.Kind != CharacterPosePortKind.LocalPose &&
                    port.Kind != CharacterPosePortKind.ComponentPose)
                {
                    continue;
                }
                CharacterPoseSpace candidate = port.Kind == CharacterPosePortKind.LocalPose
                    ? CharacterPoseSpace.Local
                    : CharacterPoseSpace.Component;
                if (result != CharacterPoseSpace.None && result != candidate)
                    throw new InvalidOperationException($"Pose node '{node.NodeId}' mixes input or output Pose spaces.");
                result = candidate;
            }
            return result;
        }

        static Dictionary<PoseInterfacePortId, CompiledValue> CompileGraph(
            BindingBuilder state,
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterPoseCanvasGraph graph,
            IReadOnlyDictionary<PoseInterfacePortId, CompiledValue> imports,
            string scope,
            string callChain,
            bool root,
            Action<int> stateOutput = null,
            int linkedPoseFragmentIndex = -1,
            string linkedPoseFragmentIdentity = "")
        {
            CharacterPoseCanvasGraph closedGraph =
                state.GraphClosure.RequireGraph(
                    ownerAsset,
                    graph.GraphId);
            if (!ReferenceEquals(closedGraph, graph))
            {
                throw new InvalidOperationException(
                    $"Pose Graph '{graph.GraphId}' does not match its Graph Closure entry.");
            }
            state.GraphDependencies.Add($"{CharacterPresentationAssetObjectIdentity.Require(ownerAsset)}\0{callChain}\0{graph.GraphId}\0{graph.ContentRevision}");
            CharacterPoseIrGraphRole graphRole = linkedPoseFragmentIndex >= 0 && stateOutput == null
                ? CharacterPoseIrGraphRole.LinkedPoseEntry
                : root
                ? CharacterPoseIrGraphRole.Root
                : stateOutput != null
                    ? CharacterPoseIrGraphRole.StateLocal
                    : CharacterPoseIrGraphRole.Subgraph;
            CharacterPoseIrGraph ir = state.Topology.RequireGraph(
                ownerAsset,
                graph.GraphId,
                graphRole);
            Dictionary<PoseNodeId, CharacterPoseCanvasNode> nodes = graph.Nodes.ToDictionary(value => value.NodeId);
            Dictionary<string, CharacterPoseCanvasConnection> incoming = BuildIncoming(graph);
            var values = new Dictionary<string, CompiledValue>(StringComparer.Ordinal);
            var exports = new Dictionary<PoseInterfacePortId, CompiledValue>();
            for (int nodeIndex = 0; nodeIndex < ir.Nodes.Count; nodeIndex++)
            {
                CharacterPoseIrNode irNode = ir.Nodes[nodeIndex];
                CharacterPoseCanvasNode node = nodes[new PoseNodeId(irNode.NodeId.Value)];
                CharacterPoseNodeDefinition handler =
                    RequireNativeHandler(irNode);
                if (handler.NativeRole ==
                    CharacterPoseNativeNodeRole.GraphInput)
                {
                    BindGraphInputs(node, imports, scope, values);
                    continue;
                }
                if (handler.NativeRole ==
                    CharacterPoseNativeNodeRole.GraphOutput)
                {
                    BindGraphOutputs(node, incoming, scope, values, exports);
                    continue;
                }
                if (handler.NativeRole ==
                    CharacterPoseNativeNodeRole.Subgraph)
                {
                    CompileSubgraphCall(
                        state,
                        ownerAsset,
                        graph,
                        node,
                        incoming,
                        scope,
                        callChain,
                        values,
                        linkedPoseFragmentIndex,
                        linkedPoseFragmentIdentity);
                    continue;
                }

                PoseNodeId scopedNodeId = ScopeNodeId(node.NodeId, scope);
                LinkedPoseCallCompilation linkedPoseCall = handler.Kind == CharacterPoseNodeKind.LinkedPoseCall
                    ? CompileLinkedPoseCall(
                        state,
                        node,
                        incoming,
                        scope,
                        callChain,
                        values)
                    : new LinkedPoseCallCompilation(-1, handler.ExecutionDomain);
                CharacterPoseFamilyPayloadBindingResult preboundFamilyPayload =
                    handler.Requires(
                        CharacterPoseNodeRuntimeRequirement.StateMachine)
                        ? s_FamilyPayloadAdapters.Bind(
                            CharacterPoseOperationFamily.StateMachine,
                            new CharacterPoseFamilyPayloadBindingRequest(
                                state,
                                handler,
                                irNode,
                                node,
                                scopedNodeId,
                                scope,
                                callChain,
                                linkedPoseFragmentIndex,
                                linkedPoseFragmentIdentity,
                                -1,
                                -1,
                                -1,
                                -1))
                        : default;
                CharacterPoseOperationCode expectedCode =
                    handler.NativeRole ==
                    CharacterPoseNativeNodeRole.PoseOutput &&
                    stateOutput != null
                        ? CharacterPoseOperationCode.StatePoseOutput
                        : handler.OperationCode;
                CharacterPoseExecutionDomain expectedDomain =
                    expectedCode == CharacterPoseOperationCode.StatePoseOutput
                        ? CharacterPoseExecutionDomain.ManagedControl
                        : linkedPoseCall.ExecutionDomain;
                CharacterPoseSymbolicOperation symbolic =
                    RequireNextSymbolicOperation(
                        state,
                        scopedNodeId,
                        handler,
                        expectedCode,
                        expectedDomain,
                        ResolveInputPoseSpace(node),
                        ResolveOutputPoseSpace(node, expectedCode),
                        linkedPoseFragmentIdentity);
                int operationIndex = state.Operations.Count;
                CharacterPoseOperationCode code = symbolic.OperationCode;
                int outputValueIndex = HasPoseOutput(node) ||
                                       handler.NativeRole ==
                                       CharacterPoseNativeNodeRole
                                           .PoseOutput
                    ? state.PoseValueCount++
                    : -1;
                int outputFullBodyIkGoalContributionValueIndex = HasOutput(
                    node,
                    CharacterPosePortKind.FullBodyIkGoalContribution)
                    ? state.FullBodyIkGoalContributionValueCount++
                    : -1;
                int outputFullBodyIkGoalSetValueIndex = HasOutput(
                    node,
                    CharacterPosePortKind.FullBodyIkGoals)
                    ? state.FullBodyIkGoalSetValueCount++
                    : -1;
                int inputA = RequireOptionalPoseInput(node, 0, incoming, scope, values);
                int inputB = RequireOptionalPoseInput(node, 1, incoming, scope, values);
                int[] fullBodyIkGoalContributionInputs = GetInputIndices(
                    node,
                    CharacterPosePortKind.FullBodyIkGoalContribution,
                    incoming,
                    scope,
                    values);
                int fullBodyIkGoalContributionInputStart =
                    fullBodyIkGoalContributionInputs.Length == 0
                    ? -1
                    : state.FullBodyIkGoalContributionInputValueIndices.Count;
                state.FullBodyIkGoalContributionInputValueIndices.AddRange(
                    fullBodyIkGoalContributionInputs);
                int[] fullBodyIkGoalSetInputs = GetInputIndices(
                    node,
                    CharacterPosePortKind.FullBodyIkGoals,
                    incoming,
                    scope,
                    values);
                if (fullBodyIkGoalSetInputs.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Pose Node '{scopedNodeId}' has more than one Full Body IK Goal Set input.");
                }
                int inputFullBodyIkGoalSetValueIndex =
                    fullBodyIkGoalSetInputs.Length == 1
                        ? fullBodyIkGoalSetInputs[0]
                        : -1;
                int controlInputOperationIndex = -1;
                int parameterIndex = -1;
                int parameterIndexB = TryGetInputIndex(
                    node,
                    CharacterPosePortKind.Parameter,
                    1,
                    incoming,
                    scope,
                    values);
                int playerIndex = -1;
                if (handler.Requires(
                        CharacterPoseNodeRuntimeRequirement.ActionPlaybackControl))
                {
                    CompiledValue selection = RequireInput(
                        node,
                        CharacterPosePortKind.ActionPlayback,
                        0,
                        incoming,
                        scope,
                        values);
                    controlInputOperationIndex = selection.ProducerOperationIndex;
                }
                if (handler.Requires(
                        CharacterPoseNodeRuntimeRequirement.Player))
                    playerIndex = state.PlayerCount++;
                PoseParameterId declaredParameter = handler.Parameter(irNode.Payload);
                if (declaredParameter.IsValid)
                    parameterIndex = state.ParameterIndices[declaredParameter];
                else
                {
                    CharacterPosePortDefinition parameterPort =
                        CharacterPoseAuthoringPortProjection.Get(node)
                            .FirstOrDefault(port =>
                                port != null &&
                                port.Kind == CharacterPosePortKind.Parameter &&
                                port.Direction == CharacterPosePortDirection.Input);
                    if (parameterPort != null)
                    {
                        parameterIndex = parameterPort.Required
                            ? RequireInput(
                                node,
                                CharacterPosePortKind.Parameter,
                                0,
                                incoming,
                                scope,
                                values).Index
                            : TryGetInputIndex(
                                node,
                                CharacterPosePortKind.Parameter,
                                0,
                                incoming,
                                scope,
                                values);
                    }
                }

                int blendNodeIndex = handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.BlendPolicy)
                    ? state.BlendNodeIndices.TryGetValue(scopedNodeId, out int index)
                        ? index
                        : throw new InvalidOperationException($"Animation transition owner '{scopedNodeId}' has no compiled policy payload.")
                    : -1;
                CharacterPoseFamilyPayloadBindingResult familyPayload =
                    handler.Requires(
                        CharacterPoseNodeRuntimeRequirement.StateMachine)
                        ? preboundFamilyPayload
                        : s_FamilyPayloadAdapters.Bind(
                            symbolic.Family,
                            new CharacterPoseFamilyPayloadBindingRequest(
                                state,
                                handler,
                                irNode,
                                node,
                                scopedNodeId,
                                scope,
                                callChain,
                                linkedPoseFragmentIndex,
                                linkedPoseFragmentIdentity,
                                inputA,
                                controlInputOperationIndex,
                                playerIndex,
                                blendNodeIndex));
                PoseParameterResolvePolicy[] policies = CompilePolicies(
                    handler.ParameterPolicies(irNode.Payload),
                    state.Parameters,
                    state.ParameterIndices);
                PresentationPoseSourceProviderId provider = handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.Player)
                    ? new PresentationPoseSourceProviderId($"pose-provider/{scopedNodeId}")
                    : default;
                CharacterPresentationPoseSourceSlot sourceSlot = handler.Source(irNode.Payload);
                PresentationPoseSourceIndex sourceIndex = default;
                if (sourceSlot && !state.SourceIndices.TryGetValue(sourceSlot, out sourceIndex))
                    throw new InvalidOperationException($"Pose Player '{scopedNodeId}' Source Slot is outside the compiled source catalog.");
                state.Operations.Add(new CharacterPoseBoundOperation(
                    operationIndex,
                    symbolic.ExecutionDomain,
                    symbolic.InputPoseSpace,
                    symbolic.OutputPoseSpace,
                    code,
                    symbolic.Family,
                    scopedNodeId,
                    provider,
                    sourceIndex,
                    outputValueIndex,
                    inputA,
                    inputB,
                    controlInputOperationIndex,
                    handler.Channel(irNode.Payload),
                    handler.Availability(irNode.Payload, stateOutput != null),
                    parameterIndex,
                    parameterIndexB,
                    handler.InputRange(irNode.Payload),
                    playerIndex,
                    blendNodeIndex,
                    familyPayload.InertializationIndex,
                    familyPayload.BoneMaskIndex,
                    familyPayload.AdditiveReferenceIndex,
                    familyPayload.ModifyBoneIndex,
                    familyPayload.RootOrientationWarpIndex,
                    familyPayload.PoseBoneIkGoalsIndex,
                    familyPayload.FootPlacementIndex,
                    familyPayload.FullBodyIkIndex,
                    outputFullBodyIkGoalContributionValueIndex,
                    outputFullBodyIkGoalSetValueIndex,
                    inputFullBodyIkGoalSetValueIndex,
                    fullBodyIkGoalContributionInputStart,
                    fullBodyIkGoalContributionInputs.Length,
                    familyPayload.ClipPlayerIndex,
                    familyPayload.StateMachineIndex,
                    familyPayload.AnimationSlotIndex,
                    linkedPoseCall.CallIndex,
                    linkedPoseFragmentIndex,
                    handler.Weight(irNode.Payload),
                    policies));

                BindOperationOutputs(
                    node,
                    scope,
                    outputValueIndex,
                    outputFullBodyIkGoalContributionValueIndex,
                    outputFullBodyIkGoalSetValueIndex,
                    parameterIndex,
                    operationIndex,
                    values);
                CharacterPoseOutputPortSource[] outputSources = CharacterPoseAuthoringPortProjection.Get(node)
                    .Where(port => port.Direction == CharacterPosePortDirection.Output)
                    .Select(port => new CharacterPoseOutputPortSource(port.PortId.Value, port.Kind,
                        values[EndpointKey(node.NodeId, port.PortId, scope)].Index)).ToArray();
                state.SourceMap.Add(new CharacterPresentationPoseSourceMapEntry(operationIndex, graph.GraphId.Value, graph.ContentRevision,
                    scopedNodeId, node.NodeId, callChain, scope, outputSources));
                if (handler.NativeRole ==
                    CharacterPoseNativeNodeRole.PoseOutput)
                {
                    if (stateOutput != null)
                    {
                        stateOutput(outputValueIndex);
                    }
                    else if (!root || state.OutputOperationIndex >= 0)
                        throw new InvalidOperationException("Pose Plan contains an invalid OutputPose boundary.");
                    else
                        state.OutputOperationIndex = operationIndex;
                }
            }
            return exports;
        }

        static LinkedPoseCallCompilation CompileLinkedPoseCall(
            BindingBuilder state,
            CharacterPoseCanvasNode call,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            string callChain,
            Dictionary<string, CompiledValue> values)
        {
            if (call?.Payload is not CharacterLinkedPoseCallPayload payload ||
                !state.LinkedGroups.TryGetValue(payload.GroupId, out CharacterLinkedPoseGroupBinding group) ||
                payload.InterfaceId != group.Interface.InterfaceId)
            {
                throw new InvalidOperationException($"Linked Pose Call '{call?.NodeId}' has no exact compiled Group and Interface.");
            }
            CharacterLinkedPosePortProjection.RequireCallMatch(call, group.Interface);
            CharacterLinkedPoseInterfaceEntryDescriptor entry = group.Interface.RequireEntry(payload.EntryId);
            CharacterLinkedPoseCompiledSelectorDescriptor selector = state.LinkedPose.Selectors.Single(value => value.GroupId == payload.GroupId);
            var imports = new Dictionary<PoseInterfacePortId, CompiledValue>();
            var inputBindings = new List<CharacterLinkedPosePortValueBinding>();
            IReadOnlyList<CharacterPosePortDefinition> callPorts = CharacterPoseAuthoringPortProjection.Get(call);
            for (int portIndex = 0; portIndex < callPorts.Count; portIndex++)
            {
                CharacterPosePortDefinition port = callPorts[portIndex];
                if (port == null || port.Direction != CharacterPosePortDirection.Input)
                    continue;
                if (!TryGetInputValue(call, port, incoming, scope, values, out CompiledValue value))
                {
                    if (port.Required)
                        throw new InvalidOperationException($"Linked Pose Call '{call.NodeId}' Interface Port '{port.InterfacePortId}' has no source.");
                    continue;
                }
                imports.Add(port.InterfacePortId, value);
                inputBindings.Add(new CharacterLinkedPosePortValueBinding(port.InterfacePortId, port.Kind, value.Index));
            }

            int callIndex = state.LinkedCalls.Count;
            var fragmentIndices = new int[selector.CandidateImplementationIds.Count];
            for (int candidateIndex = 0; candidateIndex < selector.CandidateImplementationIds.Count; candidateIndex++)
            {
                var implementationId = new LinkedPoseImplementationId(selector.CandidateImplementationIds[candidateIndex]);
                if (!state.LinkedImplementations.TryGetValue(implementationId, out CharacterLinkedPoseImplementationAsset implementation))
                    throw new InvalidOperationException($"Linked Pose Call '{call.NodeId}' candidate '{implementationId}' is absent from authoring.");
                CharacterLinkedPoseImplementationEntryBinding entryBinding = implementation.RequireEntry(payload.EntryId);
                CharacterPoseCanvasGraph entryGraph =
                    state.GraphClosure.RequireGraph(
                        entryBinding.GraphOwner,
                        entryBinding.GraphId);
                CharacterLinkedPosePortProjection.RequireEntryGraphMatch(entryGraph, group.Interface, payload.EntryId);

                int fragmentIndex = state.LinkedFragments.Count;
                fragmentIndices[candidateIndex] = fragmentIndex;
                int operationStart = state.Operations.Count;
                int poseValueStart = state.PoseValueCount;
                int goalSetValueStart = state.FullBodyIkGoalSetValueCount;
                int playerStart = state.PlayerCount;
                int stateMachineStart = state.StateMachines.Count;
                int inertializationStart = state.InertializationCount;
                int rootOrientationWarpStart = state.RootOrientationWarps.Count;
                int motionMatchingProviderStart = CountMotionMatchingProviders(state.StateMachines, 0, stateMachineStart);
                string callNodeScope = ScopeNodeId(call.NodeId, scope).Value;
                string fragmentScope = $"{callNodeScope}/linked/{payload.GroupId.Value}/{implementationId.Value}/{payload.EntryId.Value}";
                string fragmentCallChain = string.IsNullOrEmpty(callChain)
                    ? $"{callNodeScope}->{entryBinding.GraphOwnerIdentity}/{entryGraph.GraphId.Value}"
                    : $"{callChain}|{callNodeScope}->{entryBinding.GraphOwnerIdentity}/{entryGraph.GraphId.Value}";
                Dictionary<PoseInterfacePortId, CompiledValue> exports = CompileGraph(
                    state,
                    entryBinding.GraphOwner,
                    entryGraph,
                    imports,
                    fragmentScope,
                    fragmentCallChain,
                    false,
                    null,
                    fragmentIndex,
                    fragmentScope);
                int operationCount = state.Operations.Count - operationStart;
                var outputBindings = new List<CharacterLinkedPosePortValueBinding>();
                for (int portIndex = 0; portIndex < entry.Ports.Count; portIndex++)
                {
                    CharacterLinkedPoseInterfacePortDescriptor port = entry.Ports[portIndex];
                    if (port.Direction != CharacterPosePortDirection.Output)
                        continue;
                    if (!exports.TryGetValue(port.PortId, out CompiledValue output))
                    {
                        if (port.Required)
                            throw new InvalidOperationException($"Linked Pose Implementation '{implementationId}' Entry '{payload.EntryId}' has no output '{port.PortId}'.");
                        continue;
                    }
                    outputBindings.Add(new CharacterLinkedPosePortValueBinding(port.PortId, port.Kind, output.Index));
                }
                int[] sourceIndices = state.Operations
                    .Skip(operationStart)
                    .Take(operationCount)
                    .Where(value => value.PresentationPoseSourceIndex.IsValid)
                    .Select(value => value.PresentationPoseSourceIndex.Value)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray();
                state.LinkedFragments.Add(new CharacterLinkedPoseEntryFragmentPlanDescriptor(
                    fragmentIndex,
                    payload.GroupId,
                    group.Interface,
                    implementation,
                    payload.EntryId,
                    entryGraph,
                    operationStart,
                    operationCount,
                    poseValueStart,
                    state.PoseValueCount - poseValueStart,
                    goalSetValueStart,
                    state.FullBodyIkGoalSetValueCount - goalSetValueStart,
                    playerStart,
                    state.PlayerCount - playerStart,
                    stateMachineStart,
                    state.StateMachines.Count - stateMachineStart,
                    inertializationStart,
                    state.InertializationCount - inertializationStart,
                    rootOrientationWarpStart,
                    state.RootOrientationWarps.Count - rootOrientationWarpStart,
                    motionMatchingProviderStart,
                    CountMotionMatchingProviders(
                        state.StateMachines,
                        stateMachineStart,
                        state.StateMachines.Count - stateMachineStart),
                    inputBindings.ToArray(),
                    outputBindings.ToArray(),
                    sourceIndices));
            }
            state.LinkedCalls.Add(new CharacterLinkedPoseCallPlanDescriptor(
                callIndex,
                ScopeNodeId(call.NodeId, scope),
                payload.GroupId,
                group.Interface,
                payload.EntryId,
                entry.ExecutionDomain,
                fragmentIndices));
            return new LinkedPoseCallCompilation(callIndex, entry.ExecutionDomain);
        }

        static int CountMotionMatchingProviders(
            IReadOnlyList<CharacterPoseStateMachineDescriptor> stateMachines,
            int start,
            int count)
        {
            if (stateMachines == null || start < 0 || count < 0 || start + count > stateMachines.Count)
                throw new ArgumentOutOfRangeException(nameof(start));
            int result = 0;
            for (int machineIndex = start; machineIndex < start + count; machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine = stateMachines[machineIndex];
                for (int stateIndex = 0; stateIndex < machine.States.Count; stateIndex++)
                {
                    CharacterPoseStateDescriptor poseState = machine.States[stateIndex];
                    for (int providerIndex = 0; providerIndex < poseState.SourceProviders.Count; providerIndex++)
                    {
                        if (poseState.SourceProviders[providerIndex].SourceKind == AnimationPoseSourceKind.MotionMatching)
                            result = checked(result + 1);
                    }
                }
            }
            return result;
        }

        internal static int CompileAnimationSlot(
            CharacterAnimationSlotPosePayload payload,
            PoseNodeId scopedNodeId,
            int sourcePoseValueIndex,
            int actionPlaybackOperationIndex,
            int playerIndex,
            int blendNodeIndex,
            BindingBuilder state)
        {
            if (sourcePoseValueIndex < 0 || actionPlaybackOperationIndex < 0 ||
                playerIndex < 0 || blendNodeIndex < 0)
                throw new InvalidOperationException($"Animation Slot '{scopedNodeId}' has an incomplete compiled input.");
            if ((uint)actionPlaybackOperationIndex >= (uint)state.Operations.Count)
                throw new InvalidOperationException(
                    $"Animation Slot '{scopedNodeId}' Action Playback operation is outside the compiled graph.");
            CharacterPoseBoundOperation actionPlayback =
                state.Operations[actionPlaybackOperationIndex];
            if (actionPlayback.Code != CharacterPoseOperationCode.ActionPlaybackInput ||
                actionPlayback.AnimationChannelId != payload.AnimationChannelId ||
                actionPlayback.SelectionAvailability != AnimationSelectionAvailabilityPolicy.AllowEmpty)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{scopedNodeId}' requires one exact AllowEmpty Action Playback binding on channel '{payload.AnimationChannelId}'.");
            }
            AnimationBlendNodePayload blendNode = state.BlendNodes[blendNodeIndex];
            if (blendNode == null || blendNode.NodeId != scopedNodeId || blendNode.StackPolicy == null ||
                !payload.BlendPolicy ||
                blendNode.StackPolicy.MaxActiveSourceEntries != payload.BlendPolicy.StackPolicy.MaxActiveSourceEntries)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{scopedNodeId}' has no exact BlendStack workspace payload.");
            }

            var producerIdentities = new Dictionary<int, string>();
            for (int i = 0; i < blendNode.Transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition = blendNode.Transitions[i];
                if (transition == null)
                    throw new InvalidOperationException($"Animation Slot '{scopedNodeId}' transition #{i} is missing.");
                CollectSlotProducerIdentity(
                    scopedNodeId,
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind,
                    transition.SourceOwnerIdentity,
                    producerIdentities);
                CollectSlotProducerIdentity(
                    scopedNodeId,
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind,
                    transition.TargetOwnerIdentity,
                    producerIdentities);
            }
            if (producerIdentities.Count == 0)
                throw new InvalidOperationException($"Animation Slot '{scopedNodeId}' has no reachable Action producer.");

            var endpoints = new List<CharacterAnimationSlotEndpointDescriptor>
            {
                new CharacterAnimationSlotEndpointDescriptor(
                    TransitionEndpointId.SourcePose,
                    -1,
                    string.Empty,
                    true)
            };
            var endpointByProducer = new Dictionary<int, TransitionEndpointId>();
            foreach (KeyValuePair<int, string> producer in producerIdentities
                         .OrderBy(value => value.Value, StringComparer.Ordinal)
                         .ThenBy(value => value.Key))
            {
                var endpointId = new TransitionEndpointId(
                    $"animation-slot/{payload.SlotId}/producer/{producer.Value}");
                endpoints.Add(new CharacterAnimationSlotEndpointDescriptor(
                    endpointId,
                    producer.Key,
                    producer.Value,
                    false));
                endpointByProducer.Add(producer.Key, endpointId);
            }

            TransitionEndpointId ResolveEndpoint(
                int producerIndex,
                AnimationBlendTransitionEndpointKind endpointKind) =>
                endpointKind switch
                {
                    AnimationBlendTransitionEndpointKind.SourcePose =>
                        TransitionEndpointId.SourcePose,
                    AnimationBlendTransitionEndpointKind.SourceOwner
                        when endpointByProducer.TryGetValue(
                            producerIndex,
                            out TransitionEndpointId endpoint) =>
                        endpoint,
                    AnimationBlendTransitionEndpointKind.SourceOwner =>
                        throw new InvalidOperationException(
                            $"Animation Slot '{scopedNodeId}' transition references unknown Action producer index '{producerIndex}'."),
                    _ => throw new InvalidOperationException(
                        $"Animation Slot '{scopedNodeId}' transition uses invalid endpoint kind '{endpointKind}'.")
                };

            var routingRules = new AnimationTransitionRule[blendNode.Transitions.Count];
            var requestRoutes = new CharacterAnimationSlotRequestRouteDescriptor[blendNode.Transitions.Count];
            var routeTokens = new List<string>
            {
                CharacterAnimationSlotDescriptor.SchemaVersion,
                payload.SlotId.Value,
                payload.AnimationChannelId.Value,
                blendNode.PolicyId,
                blendNode.PolicyRevision
            };
            for (int i = 0; i < blendNode.Transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition = blendNode.Transitions[i];
                TransitionEndpointId source = ResolveEndpoint(
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind);
                TransitionEndpointId target = ResolveEndpoint(
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind);
                var ruleId = new TransitionRuleId(
                    $"animation-slot/{payload.SlotId}/route/{StableHash.Compute(source.Value, target.Value)}");
                var curveId = new TransitionBlendCurveId($"curve/{transition.CurveIndex}");
                var profileId = new TransitionBlendProfileId($"profile/{transition.BlendProfileIndex}");
                routingRules[i] = new AnimationTransitionRule(
                    ruleId,
                    source,
                    target,
                    transition.BlendLogic,
                    transition.DurationSeconds,
                    curveId,
                    profileId);
                requestRoutes[i] = new CharacterAnimationSlotRequestRouteDescriptor(
                    ruleId,
                    source,
                    target,
                    transition.BlendLogic,
                    transition.DurationSeconds,
                    transition.CurveIndex,
                    transition.BlendProfileIndex,
                    !target.IsSourcePose,
                    transition.BlendLogic == AnimationTransitionBlendLogic.Inertialization);
                routeTokens.Add(FormattableString.Invariant(
                    $"{ruleId}:{source}:{target}:{(int)transition.BlendLogic}:{transition.DurationSeconds:R}:{transition.CurveIndex}:{transition.BlendProfileIndex}"));
            }

            var routingRevision = new TransitionDefinitionRevision(
                StableHash.Compute(routeTokens.ToArray()).ToString());
            TransitionRoutingCompileResult routing = TransitionRoutingCompiler.Compile(
                new TransitionRoutingDefinition(
                    TransitionRoutingCompiler.CurrentSchemaVersion,
                    routingRevision,
                    TransitionRoutingCoveragePolicy.CompleteMatrix,
                    endpoints.Select(value => value.EndpointId).ToArray(),
                    routingRules,
                    true));
            if (!routing.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{scopedNodeId}' Transition Routing compile failed: " +
                    string.Join(
                        " | ",
                        routing.Diagnostics.Select(value => $"[{value.Code}] {value.Message}")));
            }

            int descriptorIndex = state.AnimationSlots.Count;
            state.AnimationSlots.Add(new CharacterAnimationSlotDescriptor(
                descriptorIndex,
                scopedNodeId,
                payload.SlotId,
                payload.AnimationChannelId,
                new TransitionRouteOwnerId($"animation-slot/{payload.SlotId}"),
                new CompiledTransitionRoutingPlanPayload(
                    routing.Plan),
                endpoints.ToArray(),
                requestRoutes,
                new CharacterAnimationSlotActionPlayerDescriptor(
                    new PoseNodeId(scopedNodeId.Value + "/action-player"),
                    actionPlaybackOperationIndex,
                    playerIndex,
                    payload.AnimationChannelId,
                    true),
                new CharacterAnimationSlotBlendStackWorkspaceDescriptor(
                    blendNodeIndex,
                    blendNode.StackPolicy.MaxActiveSourceEntries),
                new CharacterAnimationSlotSourceUsagePlan(
                    sourcePoseValueIndex,
                    actionPlaybackOperationIndex,
                    playerIndex,
                    true),
                new CharacterAnimationSlotReleasePlan(
                    TransitionEndpointId.SourcePose,
                    true,
                    true,
                    true)));
            PoseNodeId actionPlayerNodeId =
                new PoseNodeId(scopedNodeId.Value + "/action-player");
            for (int i = 0; i < endpoints.Count; i++)
            {
                CharacterAnimationSlotEndpointDescriptor endpoint = endpoints[i];
                if (endpoint.SourcePose)
                    continue;
                state.ActionPlaybackInputs.Add(new ActionPlaybackInputPlan(
                    state.ActionPlaybackInputs.Count,
                    endpoint.ProgramProducerIndex,
                    endpoint.ProgramProducerIdentity,
                    payload.AnimationChannelId,
                    descriptorIndex,
                    payload.SlotId,
                    scopedNodeId,
                    playerIndex,
                    actionPlayerNodeId,
                    endpoint.EndpointId));
            }
            return descriptorIndex;
        }

        static void CollectSlotProducerIdentity(
            PoseNodeId slotNodeId,
            int producerIndex,
            AnimationBlendTransitionEndpointKind endpointKind,
            string producerIdentity,
            Dictionary<int, string> identities)
        {
            if (endpointKind == AnimationBlendTransitionEndpointKind.SourcePose)
            {
                if (producerIndex != -1 || !string.IsNullOrEmpty(producerIdentity))
                    throw new InvalidOperationException($"Animation Slot '{slotNodeId}' has an invalid Source Pose endpoint.");
                return;
            }
            if (endpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{slotNodeId}' cannot contain endpoint kind '{endpointKind}'.");
            }
            if (producerIndex < 0 || string.IsNullOrWhiteSpace(producerIdentity))
                throw new InvalidOperationException($"Animation Slot '{slotNodeId}' has an invalid Action producer endpoint.");
            if (identities.TryGetValue(producerIndex, out string existing) &&
                !string.Equals(existing, producerIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{slotNodeId}' Action producer index '{producerIndex}' resolves to multiple identities.");
            }
            identities[producerIndex] = producerIdentity;
        }

        internal static int CompileModifyBone(
            CharacterModifyBonePosePayload payload,
            BindingBuilder state)
        {
            int boneIndex = state.Rig.RequirePhysicalBoneIndex(payload.BoneId);
            int index = state.ModifyBones.Count;
            state.ModifyBones.Add(new CharacterPresentationModifyBoneDescriptor(
                index,
                boneIndex,
                state.Rig.PhysicalBones[boneIndex].ParentIndex,
                payload));
            return index;
        }

        internal static int CompilePoseBoneIkGoals(
            CharacterPoseBoneIkGoalsPayload payload,
            PoseNodeId scopedNodeId,
            BindingBuilder state)
        {
            var bindings = new CharacterPresentationPoseBoneIkGoalBindingDescriptor[
                payload.Bindings.Count];
            for (int i = 0; i < bindings.Length; i++)
            {
                CharacterPoseBoneIkGoalBinding binding = payload.Bindings[i];
                bindings[i] = new CharacterPresentationPoseBoneIkGoalBindingDescriptor(
                    binding.EffectorSlot,
                    state.Rig.RequirePoseBoneIndex(binding.TargetPoseBoneId),
                    binding.PositionOffset,
                    binding.RotationOffset,
                    binding.PositionWeight,
                    binding.RotationWeight);
            }
            int index = state.PoseBoneIkGoalSources.Count;
            int goalOffset = state.FullBodyIkGoalContributionGoalWorkspaceCount;
            state.FullBodyIkGoalContributionGoalWorkspaceCount = checked(
                state.FullBodyIkGoalContributionGoalWorkspaceCount + bindings.Length);
            state.PoseBoneIkGoalSources.Add(
                new CharacterPresentationPoseBoneIkGoalsDescriptor(
                    index,
                    scopedNodeId,
                    goalOffset,
                    bindings));
            return index;
        }

        internal static int CompileInertialization(BindingBuilder state) =>
            state.InertializationCount++;

        internal static int CompileFootPlacement(
            CharacterFootPlacementPosePayload payload,
            PoseNodeId scopedNodeId,
            BindingBuilder state)
        {
            if (state.FootPlacements.Count != 0)
                throw new InvalidOperationException("Pose Plan contains more than one Foot Placement node.");
            int index = state.FootPlacements.Count;
            int goalOffset = state.FullBodyIkGoalContributionGoalWorkspaceCount;
            state.FullBodyIkGoalContributionGoalWorkspaceCount = checked(
                state.FullBodyIkGoalContributionGoalWorkspaceCount +
                CharacterPresentationFootPlacementDescriptor.GoalCount);
            state.FootPlacements.Add(
                new CharacterPresentationFootPlacementDescriptor(
                    index,
                    scopedNodeId,
                    payload.Profile,
                    payload.Calibration,
                    goalOffset));
            return index;
        }

        internal static int CompileFullBodyIk(
            PoseNodeId scopedNodeId,
            BindingBuilder state)
        {
            CharacterFullBodyIkProfile profile = state.Profile.FullBodyIkProfile;
            if (!profile)
                throw new InvalidOperationException("Animation Presentation Profile has no Full Body IK Profile.");
            profile.RequireValid();
            int index = state.FullBodyIks.Count;
            state.FullBodyIks.Add(
                new CharacterPresentationFullBodyIkDescriptor(
                    index,
                    scopedNodeId,
                    profile));
            return index;
        }

        internal static int CompileStateMachine(
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterPoseStateMachineNodePayload payload,
            PoseNodeId scopedNodeId,
            BindingBuilder state,
            string scope,
            string callChain,
            int linkedPoseFragmentIndex,
            string linkedPoseFragmentIdentity)
        {
            CharacterPoseStateMachineDefinition definition = payload.StateMachine;
            CharacterPoseStateMachineAuthoringValidator.RequireValid(
                definition,
                graphId => state.GraphClosure.RequireGraph(
                    ownerAsset,
                    graphId));
            Dictionary<PoseStateAliasId, HashSet<PoseStateId>> aliases = CharacterPoseStateAliasResolver.Expand(definition.Aliases);
            List<ExpandedStateTransition> expanded = ExpandTransitions(definition, aliases);
            HashSet<PoseStateId> reachable = CollectReachableStates(definition.Entry.TargetStateId, expanded);
            if (reachable.Count != definition.States.Count)
            {
                string hidden = string.Join(
                    ", ",
                    definition.States
                        .Where(value => !reachable.Contains(value.StateId))
                        .Select(value => value.StateId.ToString()));
                throw new InvalidOperationException(
                    $"Pose StateMachine '{definition.StateMachineId}' contains unreachable States: {hidden}.");
            }

            CharacterPoseStateDefinition[] orderedStates = definition.States
                .OrderBy(value => value.StateId)
                .ToArray();
            var stateIndices = new Dictionary<PoseStateId, int>();
            for (int i = 0; i < orderedStates.Length; i++)
                stateIndices.Add(orderedStates[i].StateId, i);

            var stateDescriptors = new CharacterPoseStateDescriptor[orderedStates.Length];
            for (int stateIndex = 0; stateIndex < orderedStates.Length; stateIndex++)
            {
                CharacterPoseStateDefinition authored = orderedStates[stateIndex];
                CharacterPoseCanvasGraph stateGraph =
                    state.GraphClosure.RequireGraph(
                        ownerAsset,
                        authored.PoseGraphId);
                ValidateStateParameters(authored, stateGraph, state.Parameters);
                int operationStart = state.Operations.Count;
                int outputValueIndex = -1;
                string stateScope = CharacterPoseCallScope.State(string.Empty, scopedNodeId, authored.StateId);
                string stateCallChain = string.IsNullOrEmpty(callChain)
                    ? scopedNodeId.Value + "/" + authored.StateId.Value
                    : callChain + "/" + scopedNodeId.Value + "/" + authored.StateId.Value;
                CompileGraph(
                    state,
                    ownerAsset,
                    stateGraph,
                    new Dictionary<PoseInterfacePortId, CompiledValue>(),
                    stateScope,
                    stateCallChain,
                    false,
                    value =>
                    {
                        if (outputValueIndex >= 0)
                        {
                            throw new InvalidOperationException(
                                $"Pose State '{authored.StateId}' compiled more than one Pose output.");
                        }
                        outputValueIndex = value;
                    },
                    linkedPoseFragmentIndex,
                    linkedPoseFragmentIdentity);
                int operationCount = state.Operations.Count - operationStart;
                if (outputValueIndex < 0 || operationCount <= 0)
                    throw new InvalidOperationException($"Pose State '{authored.StateId}' has no compiled Pose output.");
                PoseStateSourceProviderPlan[] sourceProviders = BuildStateSourceProviders(
                    stateIndex,
                    operationStart,
                    operationCount,
                    state);
                stateDescriptors[stateIndex] = new CharacterPoseStateDescriptor(
                    stateIndex,
                    authored.StateId,
                    authored.DisplayName,
                    outputValueIndex,
                    operationStart,
                    operationCount,
                    authored.AlwaysResetOnEntry,
                    sourceProviders);
            }

            ExpandedStateTransition[] orderedTransitions = expanded
                .Where(value => reachable.Contains(value.SourceStateId) &&
                                reachable.Contains(value.Authored.TargetStateId))
                .OrderBy(value => stateIndices[value.SourceStateId])
                .ThenBy(value => value.Authored.Priority)
                .ThenBy(value => value.Authored.TransitionId)
                .ThenBy(value => stateIndices[value.Authored.TargetStateId])
                .ToArray();
            var transitionDescriptors = new CharacterPoseStateTransitionDescriptor[orderedTransitions.Length];
            var routingRules = new AnimationTransitionRule[orderedTransitions.Length];
            for (int i = 0; i < orderedTransitions.Length; i++)
            {
                ExpandedStateTransition expandedTransition = orderedTransitions[i];
                CharacterPoseStateTransition authored = expandedTransition.Authored;
                int sourceStateIndex = stateIndices[expandedTransition.SourceStateId];
                int targetStateIndex = stateIndices[authored.TargetStateId];
                if (sourceStateIndex == targetStateIndex)
                    throw new InvalidOperationException(
                        $"Pose State transition '{authored.TransitionId}' cannot target its source State.");
                AnimationBlendCurvePayload curve =
                    CharacterAnimationBlendCurveCompiler.Compile(
                        authored.BlendMode,
                        authored.CustomBlendCurve);
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(curve);
                if (!state.CurveIndices.TryGetValue(curveKey, out int curveIndex))
                {
                    throw new InvalidOperationException(
                        $"Pose State transition '{authored.TransitionId}' canonical Blend Curve is missing from the Projection catalog.");
                }
                int blendProfileIndex = -1;
                float completionDurationSeconds = authored.DurationSeconds;
                if (authored.BlendProfile &&
                    !state.ProfileIndicesByIdentity.TryGetValue(
                        authored.BlendProfile.ProfileId,
                        out blendProfileIndex))
                {
                    throw new InvalidOperationException(
                        $"Pose State transition '{authored.TransitionId}' Blend Profile '{authored.BlendProfile.ProfileId}' is missing from the Projection catalog.");
                }
                if (authored.BlendProfile)
                {
                    float maxMultiplier = Math.Max(
                        1f,
                        authored.BlendProfile.BuildDense(state.Rig).Max());
                    completionDurationSeconds = authored.DurationSeconds *
                                                authored.BlendProfile.GlobalDurationMultiplier *
                                                maxMultiplier;
                }
                TransitionRuleId routingRuleId = RoutingRuleId(authored.TransitionId, expandedTransition.SourceStateId);
                CharacterPoseStateSourceSyncPlan sync = CompileStateSourceSync(
                    definition.StateMachineId,
                    authored,
                    stateDescriptors[sourceStateIndex],
                    stateDescriptors[targetStateIndex],
                    state);
                transitionDescriptors[i] = new CharacterPoseStateTransitionDescriptor(
                    i,
                    authored.TransitionId,
                    sourceStateIndex,
                    targetStateIndex,
                    authored.Priority,
                    CharacterPoseTransitionRuleCompiler.Compile(
                        authored.Rule,
                        state.MovementModeStateIdentities),
                    authored.BlendLogic,
                    authored.DurationSeconds,
                    completionDurationSeconds,
                    authored.BlendMode,
                    curveIndex,
                    blendProfileIndex,
                    routingRuleId,
                    sync);
                routingRules[i] = new AnimationTransitionRule(
                    routingRuleId,
                    RoutingEndpoint(definition.StateMachineId, expandedTransition.SourceStateId),
                    RoutingEndpoint(definition.StateMachineId, authored.TargetStateId),
                    authored.BlendLogic,
                    authored.DurationSeconds,
                    new TransitionBlendCurveId($"curve/{curveIndex}"),
                    new TransitionBlendProfileId($"profile/{blendProfileIndex}"));
            }
            if (transitionDescriptors.Length == 0)
                throw new InvalidOperationException($"Pose StateMachine '{definition.StateMachineId}' has no reachable Transition.");

            TransitionEndpointId[] endpoints = orderedStates
                .Select(value => RoutingEndpoint(definition.StateMachineId, value.StateId))
                .ToArray();
            var routingRevision = new TransitionDefinitionRevision(definition.ContentRevision);
            TransitionRoutingCompileResult routing = TransitionRoutingCompiler.Compile(
                new TransitionRoutingDefinition(
                    TransitionRoutingCompiler.CurrentSchemaVersion,
                    routingRevision,
                    TransitionRoutingCoveragePolicy.DeclaredRules,
                    endpoints,
                    routingRules));
            if (!routing.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Pose StateMachine '{definition.StateMachineId}' Transition Routing compile failed: " +
                    string.Join(
                        " | ",
                        routing.Diagnostics.Select(value => $"[{value.Code}] {value.Message}")));
            }

            int descriptorIndex = state.StateMachines.Count;
            state.StateMachines.Add(new CharacterPoseStateMachineDescriptor(
                descriptorIndex,
                scopedNodeId,
                definition.StateMachineId,
                definition.ContentRevision,
                stateIndices[definition.Entry.TargetStateId],
                definition.MaxTransitionsPerFrame,
                stateDescriptors,
                transitionDescriptors,
                new CompiledTransitionRoutingPlanPayload(
                    routing.Plan)));
            return descriptorIndex;
        }

        static List<ExpandedStateTransition> ExpandTransitions(
            CharacterPoseStateMachineDefinition definition,
            IReadOnlyDictionary<PoseStateAliasId, HashSet<PoseStateId>> aliases)
        {
            var result = new List<ExpandedStateTransition>();
            for (int i = 0; i < definition.Transitions.Count; i++)
            {
                CharacterPoseStateTransition transition = definition.Transitions[i];
                IEnumerable<PoseStateId> sources = transition.Source.Kind == PoseStateTransitionSourceKind.State
                    ? new[] { transition.Source.StateId }
                    : aliases[transition.Source.AliasId];
                foreach (PoseStateId source in sources.OrderBy(value => value))
                {
                    if (source == transition.TargetStateId)
                        continue;
                    result.Add(new ExpandedStateTransition(transition, source));
                }
            }
            return result;
        }

        static HashSet<PoseStateId> CollectReachableStates(
            PoseStateId entry,
            IReadOnlyList<ExpandedStateTransition> transitions)
        {
            var result = new HashSet<PoseStateId> { entry };
            var queue = new Queue<PoseStateId>();
            queue.Enqueue(entry);
            while (queue.Count > 0)
            {
                PoseStateId source = queue.Dequeue();
                for (int i = 0; i < transitions.Count; i++)
                {
                    ExpandedStateTransition transition = transitions[i];
                    if (transition.SourceStateId != source || !result.Add(transition.Authored.TargetStateId))
                        continue;
                    queue.Enqueue(transition.Authored.TargetStateId);
                }
            }
            return result;
        }

        static void ValidateStateParameters(
            CharacterPoseStateDefinition state,
            CharacterPoseCanvasGraph stateGraph,
            IReadOnlyList<CharacterPresentationPoseParameterEntry> parameters)
        {
            if (stateGraph.Parameters.Count != parameters.Count)
                throw new InvalidOperationException($"Pose State '{state.StateId}' Parameter contract is incomplete.");
            var authored = stateGraph.Parameters.ToDictionary(value => value.ParameterId);
            for (int i = 0; i < parameters.Count; i++)
            {
                CharacterPresentationPoseParameterEntry expected = parameters[i];
                if (!authored.TryGetValue(expected.ParameterId, out CharacterPoseParameterDeclaration actual) ||
                    actual.ValueType != expected.ValueType ||
                    actual.Usage != expected.Usage ||
                    !string.Equals(actual.Unit, expected.Unit, StringComparison.Ordinal) ||
                    actual.DefaultValue != expected.DefaultValue)
                {
                    throw new InvalidOperationException(
                        $"Pose State '{state.StateId}' Parameter '{expected.ParameterId}' does not match the root Pose Graph.");
                }
            }
        }

        static PoseStateSourceProviderPlan[] BuildStateSourceProviders(
            int stateIndex,
            int operationStart,
            int operationCount,
            BindingBuilder state)
        {
            var result = new List<PoseStateSourceProviderPlan>();
            int end = checked(operationStart + operationCount);
            for (int operationIndex = operationStart; operationIndex < end; operationIndex++)
            {
                CharacterPoseBoundOperation operation = state.Operations[operationIndex];
                AnimationPoseSourceKind sourceKind;
                PresentationPoseSourceIndex poseSourceIndex = default;
                if (operation.Code == CharacterPoseOperationCode.ClipPlayer)
                {
                    sourceKind = AnimationPoseSourceKind.Clip;
                    poseSourceIndex = state.ClipPlayers[operation.ClipPlayerIndex].PresentationPoseSourceIndex;
                }
                else if (operation.Code == CharacterPoseOperationCode.BlendSpacePlayer)
                {
                    sourceKind = AnimationPoseSourceKind.BlendSpace;
                    poseSourceIndex = operation.PresentationPoseSourceIndex;
                }
                else if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer ||
                         operation.Code == CharacterPoseOperationCode.BlendStack)
                {
                    if (operation.ControlInputOperationIndex >= 0 ||
                        !operation.PresentationPoseSourceProviderId.IsValid ||
                        !operation.PresentationPoseSourceIndex.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Pose State Player '{operation.NodeId}' requires one direct provider identity.");
                    }
                    sourceKind = AnimationPoseSourceKind.MotionMatching;
                    poseSourceIndex = operation.PresentationPoseSourceIndex;
                }
                else
                {
                    continue;
                }
                result.Add(new PoseStateSourceProviderPlan(
                    stateIndex,
                    operationIndex,
                    operation.PlayerIndex,
                    operation.PresentationPoseSourceProviderId,
                    operation.NodeId,
                    sourceKind,
                    poseSourceIndex));
            }
            return result.ToArray();
        }

        static CharacterPoseStateSourceSyncPlan CompileStateSourceSync(
            PoseStateMachineId stateMachineId,
            CharacterPoseStateTransition transition,
            CharacterPoseStateDescriptor sourceState,
            CharacterPoseStateDescriptor targetState,
            BindingBuilder state)
        {
            PoseStateSourceProviderPlan sourceUsage = FindSyncProvider(sourceState);
            PoseStateSourceProviderPlan targetUsage = FindSyncProvider(targetState);
            if (sourceUsage == null || targetUsage == null)
                return new CharacterPoseStateSourceSyncPlan(PoseStateSourceSyncMode.None);
            PresentationPoseSourceIndex sourceIndex = sourceUsage.PresentationPoseSourceIndex;
            PresentationPoseSourceIndex targetIndex = targetUsage.PresentationPoseSourceIndex;
            string relationIdentity =
                $"pose-state-phase/{stateMachineId}/{transition.TransitionId}/{sourceState.StateId}";
            CharacterLocomotionSyncGroup sourceGroup = ResolveLocomotionSyncGroup(state, sourceIndex);
            CharacterLocomotionSyncGroup targetGroup = ResolveLocomotionSyncGroup(state, targetIndex);
            bool sourceHasPhase = state.SourcePhasePlanIndices.TryGetValue(
                sourceIndex,
                out int sourcePhasePlanIndex);
            bool targetHasPhase = state.SourcePhasePlanIndices.TryGetValue(
                targetIndex,
                out int targetPhasePlanIndex);
            if (sourceGroup == null || targetGroup == null ||
                !string.Equals(sourceGroup.GroupId, targetGroup.GroupId, StringComparison.Ordinal))
            {
                return new CharacterPoseStateSourceSyncPlan(PoseStateSourceSyncMode.None);
            }
            if (sourceHasPhase || targetHasPhase)
            {
                if (!sourceHasPhase || !targetHasPhase)
                {
                    throw new InvalidOperationException(
                        $"Pose State transition '{transition.TransitionId}' has an incomplete Locomotion Phase plan inside Group '{sourceGroup.GroupId}'.");
                }
                AnimationSourcePhasePlan sourcePhase = state.SourcePhasePlans[sourcePhasePlanIndex];
                AnimationSourcePhasePlan targetPhase = state.SourcePhasePlans[targetPhasePlanIndex];
                CharacterClipPlayerClockSource sourceClock = RequireClipClock(state, sourceUsage.PlayerIndex);
                CharacterClipPlayerClockSource targetClock = RequireClipClock(state, targetUsage.PlayerIndex);
                bool sourceCovers = sourcePhase.ActualCoverage.EndSeconds - sourcePhase.ActualCoverage.StartSeconds >=
                                    transition.DurationSeconds;
                bool targetCovers = targetPhase.ActualCoverage.EndSeconds - targetPhase.ActualCoverage.StartSeconds >=
                                    transition.DurationSeconds;
                bool sourceIsLeader = sourceClock == targetClock
                    ? sourceCovers
                    : sourceClock == CharacterClipPlayerClockSource.CommittedMovement
                        ? sourceCovers
                        : !targetCovers;
                if (sourceIsLeader && !sourceCovers || !sourceIsLeader && !targetCovers)
                    throw new InvalidOperationException(
                        $"Pose State transition '{transition.TransitionId}' has no Phase leader covering the full Blend window.");
                AnimationClipPhasePlan sourceClipPhase =
                    state.ClipPhasePlans[sourcePhase.ClockCarrierClipPlanIndex];
                AnimationClipPhasePlan targetClipPhase =
                    state.ClipPhasePlans[targetPhase.ClockCarrierClipPlanIndex];
                string qualityValidationIdentity = AnimationPhaseRelationQualityCompiler.Validate(
                    transition.TransitionId.Value,
                    sourceClipPhase,
                    state.ClipPhaseValidations[sourcePhase.ClockCarrierClipPlanIndex],
                    sourcePhase.ActualCoverage,
                    targetClipPhase,
                    state.ClipPhaseValidations[targetPhase.ClockCarrierClipPlanIndex],
                    targetPhase.ActualCoverage,
                    transition.DurationSeconds);
                var phaseRelation = new AnimationPhaseRelationPlan(
                    relationIdentity,
                    transition.TransitionId,
                    sourcePhasePlanIndex,
                    targetPhasePlanIndex,
                    sourceIsLeader,
                    sourceIsLeader ? sourceClock : targetClock,
                    StableHash.Compute(
                        "animation-phase-relation-validation/v1",
                        sourceClipPhase.ValidationIdentity,
                        targetClipPhase.ValidationIdentity,
                        qualityValidationIdentity).Value);
                return new CharacterPoseStateSourceSyncPlan(
                    sourceUsage.PlayerIndex,
                    targetUsage.PlayerIndex,
                    sourceIndex,
                    targetIndex,
                    sourceGroup.GroupId,
                    phaseRelation);
            }
            return new CharacterPoseStateSourceSyncPlan(PoseStateSourceSyncMode.None);
        }

        static CharacterLocomotionSyncGroup ResolveLocomotionSyncGroup(
            BindingBuilder state,
            PresentationPoseSourceIndex sourceIndex)
        {
            CharacterPresentationPoseSourceSlot slot = state.SourceIndices
                .FirstOrDefault(pair => pair.Value == sourceIndex).Key;
            CharacterPresentationPoseSourceBinding binding = slot
                ? state.Profile.FindPoseSourceBinding(slot)
                : null;
            if (binding is CharacterClipPoseSourceBinding clipBinding)
                return state.Profile.FindLocomotionSyncGroup(clipBinding.Clip);
            if (binding is CharacterBlendSpacePoseSourceBinding blendBinding && blendBinding.BlendSpace)
            {
                CharacterAnimationBlendSpaceSample reference =
                    blendBinding.BlendSpace.FindSample(blendBinding.BlendSpace.PhaseReferenceSampleId);
                return reference == null ? null : state.Profile.FindLocomotionSyncGroup(reference.Clip);
            }
            return null;
        }

        static CharacterClipPlayerClockSource RequireClipClock(BindingBuilder state, int playerIndex)
        {
            for (int i = 0; i < state.ClipPlayers.Count; i++)
            {
                if (state.ClipPlayers[i].PlayerIndex == playerIndex)
                    return state.ClipPlayers[i].ClockSource;
            }
            for (int i = 0; i < state.Operations.Count; i++)
            {
                if (state.Operations[i].PlayerIndex == playerIndex &&
                    state.Operations[i].Code == CharacterPoseOperationCode.BlendSpacePlayer)
                    return CharacterClipPlayerClockSource.PresentationDelta;
            }
            throw new InvalidOperationException(
                $"Locomotion Phase source Player #{playerIndex} is not a compiled Clip or Blend Space Player.");
        }

        static PoseStateSourceProviderPlan FindSyncProvider(CharacterPoseStateDescriptor state)
        {
            PoseStateSourceProviderPlan result = null;
            for (int i = 0; i < state.SourceProviders.Count; i++)
            {
                PoseStateSourceProviderPlan candidate = state.SourceProviders[i];
                if (candidate.SourceKind != AnimationPoseSourceKind.Clip &&
                    candidate.SourceKind != AnimationPoseSourceKind.BlendSpace)
                    continue;
                if (result != null)
                {
                    throw new InvalidOperationException(
                        $"Pose State '{state.StateId}' has more than one sync-capable source.");
                }
                result = candidate;
            }
            return result;
        }

        static TransitionEndpointId RoutingEndpoint(
            PoseStateMachineId stateMachineId,
            PoseStateId stateId) =>
            new TransitionEndpointId($"pose-state/{stateMachineId}/{stateId}");

        static TransitionRuleId RoutingRuleId(
            PoseStateTransitionId transitionId,
            PoseStateId sourceStateId) =>
            new TransitionRuleId($"pose-state/{transitionId}/{sourceStateId}");

        internal static int CompileClipPlayer(
            CharacterClipPlayerPosePayload payload,
            PoseNodeId scopedNodeId,
            int playerIndex,
            BindingBuilder state)
        {
            if (!payload.SourceSlot || !state.SourceIndices.TryGetValue(payload.SourceSlot, out PresentationPoseSourceIndex sourceIndex))
                throw new InvalidOperationException($"Clip Player '{scopedNodeId}' Source Slot is outside the compiled source catalog.");
            int index = state.ClipPlayers.Count;
            state.ClipPlayers.Add(CharacterPresentationClipPlayerCompiler.Compile(
                index,
                playerIndex,
                scopedNodeId,
                sourceIndex,
                payload));
            return index;
        }

        internal static int CompileRootOrientationWarp(
            CharacterRootOrientationWarpPosePayload payload,
            PoseNodeId scopedNodeId,
            int inputValueIndex,
            BindingBuilder state)
        {
            CharacterPoseBoundOperation source = state.Operations
                .SingleOrDefault(value =>
                    value.OutputValueIndex == inputValueIndex);
            if (source == null ||
                source.Code != CharacterPoseOperationCode.ClipPlayer ||
                (uint)source.ClipPlayerIndex >=
                (uint)state.ClipPlayers.Count)
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{scopedNodeId}' must receive Pose directly from one Clip Player.");
            }
            CharacterPresentationClipPlayerDescriptor clipPlayer =
                state.ClipPlayers[source.ClipPlayerIndex];
            CharacterPresentationPoseSourcePlan poseSource =
                state.PoseSources[clipPlayer.PresentationPoseSourceIndex];
            if (poseSource.Clip.isLooping ||
                Math.Abs(poseSource.Clip.length - payload.YawCurve.Duration) >
                0.0001f)
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{scopedNodeId}' Yaw profile must match its finite Clip duration.");
            }
            int index = state.RootOrientationWarps.Count;
            state.RootOrientationWarps.Add(
                new CharacterPresentationRootOrientationWarpDescriptor(
                    index,
                    scopedNodeId,
                    source.ClipPlayerIndex,
                    state.Rig.RequireRootBoneIndex(),
                    payload.YawCurve.Duration,
                    payload.YawCurve.TotalYaw,
                    payload.YawCurve.LocalYaw));
            return index;
        }

        static void BindGraphInputs(
            CharacterPoseCanvasNode node,
            IReadOnlyDictionary<PoseInterfacePortId, CompiledValue> imports,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node);
            for (int i = 0; i < ports.Count; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                if (imports.TryGetValue(port.InterfacePortId, out CompiledValue value))
                    values.Add(EndpointKey(node.NodeId, port.PortId, scope), value);
                else if (port.Required)
                    throw new InvalidOperationException($"GraphInput Interface Port '{port.InterfacePortId}' has no call-site source.");
            }
        }

        static void BindGraphOutputs(
            CharacterPoseCanvasNode node,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            Dictionary<string, CompiledValue> values,
            Dictionary<PoseInterfacePortId, CompiledValue> exports)
        {
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node);
            for (int i = 0; i < ports.Count; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Input)
                    continue;
                if (TryGetInputValue(node, port, incoming, scope, values, out CompiledValue value))
                    exports.Add(port.InterfacePortId, value);
                else if (port.Required)
                    throw new InvalidOperationException($"GraphOutput Interface Port '{port.InterfacePortId}' has no internal source.");
            }
        }

        static void CompileSubgraphCall(
            BindingBuilder state,
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterPoseCanvasGraph owner,
            CharacterPoseCanvasNode callSite,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            string callChain,
            Dictionary<string, CompiledValue> values,
            int linkedPoseFragmentIndex,
            string linkedPoseFragmentIdentity)
        {
            CharacterPoseCanvasGraph child =
                state.GraphClosure.RequireGraph(
                    ownerAsset,
                    callSite.Subgraph.PoseGraphId);
            CharacterPoseSubgraphSignatureValidator.RequireMatch(callSite, child);
            var imports = new Dictionary<PoseInterfacePortId, CompiledValue>();
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(callSite);
            for (int i = 0; i < ports.Count; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Input)
                    continue;
                if (TryGetInputValue(callSite, port, incoming, scope, values, out CompiledValue value))
                    imports.Add(port.InterfacePortId, value);
                else if (port.Required)
                    throw new InvalidOperationException($"PoseSubgraph '{callSite.NodeId}' Interface Port '{port.InterfacePortId}' has no source.");
            }
            PoseNodeId scopedCallSite = ScopeNodeId(callSite.NodeId, scope);
            string childScope = CharacterPoseCallScope.Subgraph(scope, callSite.NodeId, child.GraphId);
            string childCallChain = string.IsNullOrEmpty(callChain)
                ? $"{owner.GraphId}/{scopedCallSite.Value}->{child.GraphId}"
                : $"{callChain}|{owner.GraphId}/{scopedCallSite.Value}->{child.GraphId}";
            Dictionary<PoseInterfacePortId, CompiledValue> exports = CompileGraph(
                state,
                ownerAsset,
                child,
                imports,
                childScope,
                childCallChain,
                false,
                null,
                linkedPoseFragmentIndex,
                linkedPoseFragmentIdentity);
            for (int i = 0; i < ports.Count; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                if (exports.TryGetValue(port.InterfacePortId, out CompiledValue value))
                    values.Add(EndpointKey(callSite.NodeId, port.PortId, scope), value);
                else if (port.Required)
                    throw new InvalidOperationException($"PoseSubgraph '{callSite.NodeId}' Interface Port '{port.InterfacePortId}' has no output.");
            }
        }

        static void BindOperationOutputs(
            CharacterPoseCanvasNode node,
            string scope,
            int poseValue,
            int fullBodyIkGoalContributionValue,
            int fullBodyIkGoalSetValue,
            int parameterValue,
            int operationIndex,
            Dictionary<string, CompiledValue> values)
        {
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node);
            for (int i = 0; i < ports.Count; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (port == null || port.Direction != CharacterPosePortDirection.Output)
                    continue;
                int index = port.Kind switch
                {
                    CharacterPosePortKind.ActionPlayback => operationIndex,
                    CharacterPosePortKind.Parameter => parameterValue,
                    CharacterPosePortKind.LocalPose => poseValue,
                    CharacterPosePortKind.ComponentPose => poseValue,
                    CharacterPosePortKind.PoseDiscontinuity => poseValue,
                    CharacterPosePortKind.FullBodyIkGoals => fullBodyIkGoalSetValue,
                    CharacterPosePortKind.FullBodyIkGoalContribution =>
                        fullBodyIkGoalContributionValue,
                    _ => -1
                };
                if (index < 0)
                    throw new InvalidOperationException($"Pose Node '{node.NodeId}' output '{port.PortId}' has no compiled workspace value.");
                values.Add(EndpointKey(node.NodeId, port.PortId, scope), new CompiledValue(port.Kind, index, operationIndex));
            }
        }

        static CompiledValue RequireInput(
            CharacterPoseCanvasNode node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            Dictionary<string, CompiledValue> values,
            bool required = true)
        {
            CharacterPosePortDefinition[] ports =
                CharacterPoseAuthoringPortProjection.Get(node)
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
            CharacterPoseCanvasNode node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            CharacterPosePortDefinition[] ports =
                CharacterPoseAuthoringPortProjection.Get(node)
                .Where(value => value != null && value.Kind == kind && value.Direction == CharacterPosePortDirection.Input)
                .ToArray();
            if ((uint)ordinal >= (uint)ports.Length)
                return -1;
            return RequireInput(node, kind, ordinal, incoming, scope, values).Index;
        }

        static int TryGetInputIndex(
            CharacterPoseCanvasNode node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            CharacterPosePortDefinition[] ports =
                CharacterPoseAuthoringPortProjection.Get(node)
                .Where(value => value != null && value.Kind == kind && value.Direction == CharacterPosePortDirection.Input)
                .ToArray();
            if ((uint)ordinal >= (uint)ports.Length)
                return -1;
            return TryGetInputValue(node, ports[ordinal], incoming, scope, values, out CompiledValue value)
                ? value.Index
                : -1;
        }

        static int[] GetInputIndices(
            CharacterPoseCanvasNode node,
            CharacterPosePortKind kind,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            CharacterPosePortDefinition[] ports =
                CharacterPoseAuthoringPortProjection.Get(node)
                    .Where(value => value != null &&
                                    value.Kind == kind &&
                                    value.Direction == CharacterPosePortDirection.Input)
                    .ToArray();
            var result = new List<int>(ports.Length);
            for (int i = 0; i < ports.Length; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (TryGetInputValue(node, port, incoming, scope, values, out CompiledValue value))
                {
                    result.Add(value.Index);
                    continue;
                }
                if (port.Required)
                    throw new InvalidOperationException(
                        $"Pose Node '{node.NodeId}' input '{port.PortId}' has no compiled source.");
            }
            return result.ToArray();
        }

        static bool TryGetInputValue(
            CharacterPoseCanvasNode node,
            CharacterPosePortDefinition port,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            Dictionary<string, CompiledValue> values,
            out CompiledValue value)
        {
            value = default;
            return incoming.TryGetValue(node.NodeId.Value + "\0" + port.PortId.Value, out CharacterPoseCanvasConnection edge) &&
                   values.TryGetValue(EndpointKey(edge.SourceNodeId, edge.SourcePortId, scope), out value) &&
                   value.Kind == port.Kind;
        }

        static Dictionary<string, CharacterPoseCanvasConnection> BuildIncoming(CharacterPoseCanvasGraph graph)
        {
            var result = new Dictionary<string, CharacterPoseCanvasConnection>(StringComparer.Ordinal);
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseCanvasConnection edge = graph.Edges[i];
                result.Add(edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value, edge);
            }
            return result;
        }

        internal static int CompileMask(
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

        internal static int CompileAdditiveReference(
            CharacterAdditivePosePayload payload,
            CharacterAnimationRigDefinition rig,
            List<CharacterPresentationAdditiveReferenceDescriptor> references)
        {
            var rigPayload = new CharacterAnimationRigPayload(rig);
            int count = rigPayload.PoseBoneCount;
            var positions = new Vector3[count];
            var rotations = new Quaternion[count];
            var scales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                AnimationLocalBonePose bone = rigPayload.GetReferenceLocalPose(i);
                int parentIndex = rigPayload.GetPoseParentIndex(i);
                if (parentIndex < 0)
                {
                    positions[i] = bone.Position;
                    rotations[i] = bone.Rotation;
                    scales[i] = bone.Scale;
                    continue;
                }
                positions[i] = positions[parentIndex] + rotations[parentIndex] * Vector3.Scale(scales[parentIndex], bone.Position);
                rotations[i] = (rotations[parentIndex] * bone.Rotation).normalized;
                scales[i] = Vector3.Scale(scales[parentIndex], bone.Scale);
            }
            int index = references.Count;
            references.Add(new CharacterPresentationAdditiveReferenceDescriptor(
                index,
                payload.ReferencePoseId,
                payload.ReferenceSpace,
                payload.ScalePolicy,
                positions,
                rotations,
                scales));
            return index;
        }

        static PoseParameterResolvePolicy[] CompilePolicies(
            IReadOnlyList<CharacterPoseParameterPolicy> authoredPolicies,
            CharacterPresentationPoseParameterEntry[] parameters,
            Dictionary<PoseParameterId, int> indices)
        {
            if (authoredPolicies == null || authoredPolicies.Count == 0)
                return Array.Empty<PoseParameterResolvePolicy>();
            var result = new PoseParameterResolvePolicy[parameters.Length];
            for (int i = 0; i < authoredPolicies.Count; i++)
            {
                CharacterPoseParameterPolicy policy = authoredPolicies[i];
                result[indices[policy.ParameterId]] = policy.Policy;
            }
            return result;
        }

        static int RequireOptionalPoseInput(
            CharacterPoseCanvasNode node,
            int ordinal,
            Dictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            Dictionary<string, CompiledValue> values)
        {
            CharacterPosePortDefinition[] ports =
                CharacterPoseAuthoringPortProjection.Get(node)
                    .Where(value => value != null &&
                                    IsPose(value.Kind) &&
                                    value.Direction == CharacterPosePortDirection.Input)
                    .ToArray();
            if ((uint)ordinal >= (uint)ports.Length)
                return -1;
            if (TryGetInputValue(node, ports[ordinal], incoming, scope, values, out CompiledValue value))
                return value.Index;
            if (ports[ordinal].Required)
                throw new InvalidOperationException($"Pose Node '{node.NodeId}' input '{ports[ordinal].PortId}' has no compiled source.");
            return -1;
        }

        static bool HasPoseOutput(CharacterPoseCanvasNode node) =>
            CharacterPoseAuthoringPortProjection.Get(node)
                .Any(value => value != null && IsPose(value.Kind) && value.Direction == CharacterPosePortDirection.Output);

        static bool HasOutput(
            CharacterPoseCanvasNode node,
            CharacterPosePortKind kind) =>
            CharacterPoseAuthoringPortProjection.Get(node)
                .Any(value => value != null && value.Kind == kind &&
                              value.Direction == CharacterPosePortDirection.Output);

        static bool IsPose(CharacterPosePortKind kind) =>
            kind == CharacterPosePortKind.LocalPose ||
            kind == CharacterPosePortKind.ComponentPose;

        static CharacterPoseNodeDefinition RequireNativeHandler(
            CharacterPoseIrNode node)
        {
            if (node == null)
                throw new InvalidOperationException(
                    "Pose IR node is missing.");
            CharacterPoseNodeDefinition handler =
                CharacterPoseNodeDefinitionModule.Shared
                    .RequireCapability(node.CapabilityIdentity);
            handler.RequirePayload(node.Payload);
            return handler;
        }

        static CharacterPoseSymbolicOperation RequireNextSymbolicOperation(
            BindingBuilder state,
            PoseNodeId nodeId,
            CharacterPoseNodeDefinition definition,
            CharacterPoseOperationCode operationCode,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            string fragmentIdentity)
        {
            int sequence = state.SymbolicOperationCursor;
            if ((uint)sequence >=
                (uint)state.SymbolicProgram.Operations.Count)
            {
                throw new InvalidOperationException(
                    $"Pose binding produced an unexpected Operation '{nodeId}'.");
            }
            CharacterPoseSymbolicOperation operation =
                state.SymbolicProgram.Operations[sequence];
            if (operation.Sequence != sequence ||
                operation.NodeId != nodeId ||
                operation.NodeKind != definition.Kind ||
                operation.OperationCode != operationCode ||
                operation.Family !=
                (operationCode == CharacterPoseOperationCode.StatePoseOutput
                    ? CharacterPoseOperationFamily.StateMachine
                    : definition.OperationFamily) ||
                operation.ExecutionDomain != executionDomain ||
                operation.InputPoseSpace != inputPoseSpace ||
                operation.OutputPoseSpace != outputPoseSpace ||
                !string.Equals(
                    operation.FragmentIdentity,
                    fragmentIdentity ?? string.Empty,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Pose binding Operation '{nodeId}' does not match Symbolic Family Lowering sequence #{sequence}.");
            }
            state.SymbolicOperationCursor++;
            return operation;
        }

        internal static TPayload RequirePayload<TPayload>(CharacterPoseIrNode node)
            where TPayload : CharacterPoseNodePayload
        {
            if (!(node?.Payload is TPayload payload))
            {
                throw new InvalidOperationException(
                    $"Pose IR node '{(node == null ? "<null>" : node.NodeId.Value)}' does not own payload '{typeof(TPayload).Name}'.");
            }
            return payload;
        }

        static string EndpointKey(PoseNodeId nodeId, PosePortId portId, string scope) =>
            ScopeNodeId(nodeId, scope).Value + "\0" + ScopePortId(portId, scope).Value;

        static PoseNodeId ScopeNodeId(PoseNodeId nodeId, string scope) =>
            CharacterPoseCallScope.Node(nodeId, scope);

        static PosePortId ScopePortId(PosePortId portId, string scope) =>
            string.IsNullOrEmpty(scope) ? portId : new PosePortId(scope + "/" + portId.Value);

    }
}
