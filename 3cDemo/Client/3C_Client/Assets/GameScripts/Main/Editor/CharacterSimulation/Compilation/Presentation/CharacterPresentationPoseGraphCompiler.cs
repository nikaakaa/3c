using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public static class CharacterPresentationPoseGraphCompiler
    {
        public static CharacterPresentationPosePlan Compile(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableAnimationChannels,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationPoseSourcePlan[] poseSources,
            IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> sourceIndices,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndicesByIdentity,
            CharacterAnimationPresentationProfile profile,
            CharacterLinkedPoseProjectionPayload linkedPose,
            List<string> errors) =>
            CharacterPoseNativePlanBuilder.Build(
                asset,
                rig,
                reachableAnimationChannels,
                blendNodes,
                poseSources,
                sourceIndices,
                curveIndices,
                profileIndicesByIdentity,
                profile,
                linkedPose,
                errors);
    }

    internal static class CharacterPoseNativePlanBuilder
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

        sealed class CompilationState
        {
            public CompilationState(
                CharacterPresentationPoseGraphAsset graphAsset,
                CharacterAnimationRigDefinition rig,
                CharacterPresentationPoseParameterEntry[] parameters,
                Dictionary<PoseParameterId, int> parameterIndices,
                AnimationBlendNodePayload[] blendNodes,
                CharacterPresentationPoseSourcePlan[] poseSources,
                IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> sourceIndices,
                IReadOnlyDictionary<string, int> curveIndices,
                IReadOnlyDictionary<string, int> profileIndicesByIdentity,
                CharacterAnimationPresentationProfile profile,
                CharacterLinkedPoseProjectionPayload linkedPose)
            {
                GraphAsset = graphAsset;
                Rig = rig;
                Parameters = parameters;
                ParameterIndices = parameterIndices;
                BlendNodes = blendNodes;
                BlendNodeIndices = blendNodes
                    .Select((value, index) => new KeyValuePair<PoseNodeId, int>(value.NodeId, index))
                    .ToDictionary(value => value.Key, value => value.Value);
                PoseSources = poseSources.ToDictionary(value => value.SourceIndex);
                SourceIndices = sourceIndices ?? throw new ArgumentNullException(nameof(sourceIndices));
                CurveIndices = curveIndices ?? throw new ArgumentNullException(nameof(curveIndices));
                ProfileIndicesByIdentity = profileIndicesByIdentity ??
                    throw new ArgumentNullException(nameof(profileIndicesByIdentity));
                Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
                LinkedPose = linkedPose ?? throw new ArgumentNullException(nameof(linkedPose));
                LinkedGroups = profile.LinkedPoseGroups.ToDictionary(value => value.GroupId);
                LinkedImplementations = profile.LinkedPoseImplementations.ToDictionary(value => value.ImplementationId);
            }

            public CharacterPresentationPoseGraphAsset GraphAsset { get; }
            public CharacterAnimationRigDefinition Rig { get; }
            public CharacterPresentationPoseParameterEntry[] Parameters { get; }
            public Dictionary<PoseParameterId, int> ParameterIndices { get; }
            public AnimationBlendNodePayload[] BlendNodes { get; }
            public Dictionary<PoseNodeId, int> BlendNodeIndices { get; }
            public Dictionary<PresentationPoseSourceIndex, CharacterPresentationPoseSourcePlan> PoseSources { get; }
            public IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> SourceIndices { get; }
            public IReadOnlyDictionary<string, int> CurveIndices { get; }
            public IReadOnlyDictionary<string, int> ProfileIndicesByIdentity { get; }
            public CharacterAnimationPresentationProfile Profile { get; }
            public CharacterLinkedPoseProjectionPayload LinkedPose { get; }
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
            public List<CharacterPresentationPredictiveFootPlacementDescriptor> PredictiveFootPlacements { get; } = new List<CharacterPresentationPredictiveFootPlacementDescriptor>();
            public List<CharacterPresentationFullBodyIkDescriptor> FullBodyIks { get; } = new List<CharacterPresentationFullBodyIkDescriptor>();
            public List<int> FullBodyIkGoalInputValueIndices { get; } = new List<int>();
            public List<CharacterPresentationSequencePlayerDescriptor> SequencePlayers { get; } = new List<CharacterPresentationSequencePlayerDescriptor>();
            public List<CharacterPoseStateMachineDescriptor> StateMachines { get; } =
                new List<CharacterPoseStateMachineDescriptor>();
            public List<CharacterAnimationSlotDescriptor> AnimationSlots { get; } =
                new List<CharacterAnimationSlotDescriptor>();
            public List<ActionPlaybackInputPlan> ActionPlaybackInputs { get; } =
                new List<ActionPlaybackInputPlan>();
            public List<CharacterPresentationPoseOperation> Operations { get; } = new List<CharacterPresentationPoseOperation>();
            public List<CharacterPresentationPoseSourceMapEntry> SourceMap { get; } = new List<CharacterPresentationPoseSourceMapEntry>();
            public List<string> GraphDependencies { get; } = new List<string>();
            public HashSet<string> GraphCallStack { get; } =
                new HashSet<string>(StringComparer.Ordinal);
            public int PoseValueCount { get; set; }
            public int FullBodyIkGoalSetValueCount { get; set; }
            public int FullBodyIkGoalWorkspaceCount { get; set; }
            public int PlayerCount { get; set; }
            public int OutputOperationIndex { get; set; } = -1;
        }

        readonly struct NativeWorkspacePlan
        {
            public NativeWorkspacePlan(
                int poseValueCapacity,
                int parameterValueCapacity,
                int contributionCapacity,
                int frameCacheCapacity,
                int[] poseValueLastUse)
            {
                PoseValueCapacity = poseValueCapacity;
                ParameterValueCapacity = parameterValueCapacity;
                ContributionCapacity = contributionCapacity;
                FrameCacheCapacity = frameCacheCapacity;
                PoseValueLastUse = poseValueLastUse ?? throw new ArgumentNullException(nameof(poseValueLastUse));
            }

            public int PoseValueCapacity { get; }
            public int ParameterValueCapacity { get; }
            public int ContributionCapacity { get; }
            public int FrameCacheCapacity { get; }
            public IReadOnlyList<int> PoseValueLastUse { get; }
        }

        public static CharacterPresentationPosePlan Build(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableAnimationChannels,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationPoseSourcePlan[] poseSources,
            IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> sourceIndices,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndicesByIdentity,
            CharacterAnimationPresentationProfile profile,
            CharacterLinkedPoseProjectionPayload linkedPose,
            List<string> errors)
        {
            IReadOnlyList<string> capabilityErrors =
                CharacterPoseGraphCapabilityValidator.Validate(asset);
            if (capabilityErrors.Count != 0)
            {
                errors?.AddRange(capabilityErrors);
                return null;
            }
            CharacterPoseGraphValidationReport report = CharacterPresentationPoseGraphValidator.Validate(
                asset,
                rig,
                CharacterPoseAuthoringPortProjection.Get,
                reachableAnimationChannels,
                sourceIndices?.Keys.ToArray());
            if (!report.IsValid)
            {
                report.CopyMessagesTo(errors);
                return null;
            }
            try
            {
                return CompileValidated(
                    asset,
                    rig,
                    blendNodes ?? Array.Empty<AnimationBlendNodePayload>(),
                    poseSources ?? Array.Empty<CharacterPresentationPoseSourcePlan>(),
                    sourceIndices ?? new Dictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex>(),
                    curveIndices,
                    profileIndicesByIdentity,
                    profile,
                    linkedPose);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }

        static CharacterPresentationPosePlan CompileValidated(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationPoseSourcePlan[] poseSources,
            IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> sourceIndices,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndicesByIdentity,
            CharacterAnimationPresentationProfile profile,
            CharacterLinkedPoseProjectionPayload linkedPose)
        {
            CharacterTypedPoseGraph graph = asset.Graph;
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
            var state = new CompilationState(
                asset,
                rig,
                parameters,
                parameterIndices,
                blendNodes,
                poseSources,
                sourceIndices,
                curveIndices,
                profileIndicesByIdentity,
                profile,
                linkedPose);
            CompileGraph(
                state,
                asset,
                graph,
                new Dictionary<PoseInterfacePortId, CompiledValue>(),
                string.Empty,
                string.Empty,
                true);
            if (state.OutputOperationIndex < 0 || state.PoseValueCount <= 0)
                throw new InvalidOperationException("Pose Plan has no complete Pose and Output boundary.");
            if (state.BlendNodeIndices.Count != state.BlendNodes.Length)
                throw new InvalidOperationException("Pose Plan Blend Stack payload identities are not unique.");

            NativeWorkspacePlan workspace = PlanNativeWorkspace(state);
            CharacterPresentationPoseStage[] stages = CompileStages(state.Operations);
            BindLinkedPoseStageRanges(state.LinkedFragments, stages);
            string hash = ComputeHash(
                graph,
                rig,
                state,
                stages,
                workspace.PoseValueCapacity,
                workspace.ParameterValueCapacity,
                workspace.ContributionCapacity,
                workspace.FrameCacheCapacity);
            return new CharacterPresentationPosePlan(
                graph.GraphId.Value,
                graph.ContentRevision,
                hash,
                rig,
                parameters,
                blendNodes,
                Array.Empty<CharacterPresentationInertializationDescriptor>(),
                state.Masks.ToArray(),
                state.AdditiveReferences.ToArray(),
                state.ModifyBones.ToArray(),
                state.RootOrientationWarps.ToArray(),
                state.PoseBoneIkGoalSources.ToArray(),
                state.PredictiveFootPlacements.ToArray(),
                state.FullBodyIks.ToArray(),
                state.FullBodyIkGoalInputValueIndices.ToArray(),
                state.SequencePlayers.ToArray(),
                state.StateMachines.ToArray(),
                state.AnimationSlots.ToArray(),
                state.ActionPlaybackInputs.ToArray(),
                state.LinkedFragments.ToArray(),
                state.LinkedCalls.ToArray(),
                state.Operations.ToArray(),
                state.SourceMap.ToArray(),
                stages,
                workspace.PoseValueCapacity,
                state.FullBodyIkGoalSetValueCount,
                state.FullBodyIkGoalWorkspaceCount,
                workspace.ParameterValueCapacity,
                workspace.ContributionCapacity,
                workspace.FrameCacheCapacity,
                state.OutputOperationIndex);
        }

        static CharacterPresentationPoseStage[] CompileStages(
            IReadOnlyList<CharacterPresentationPoseOperation> operations)
        {
            if (operations == null || operations.Count == 0)
                throw new InvalidOperationException("Pose Plan has no operations to stage.");

            var stages = new List<CharacterPresentationPoseStage>();
            int operationStart = 0;
            int nativeOperationStart = 0;
            while (operationStart < operations.Count)
            {
                CharacterPresentationPoseOperation first = operations[operationStart];
                CharacterPoseExecutionDomain domain = first.ExecutionDomain;
                CharacterPoseSpace outputSpace = first.OutputPoseSpace;
                int operationEnd = operationStart + 1;
                while (operationEnd < operations.Count &&
                       operations[operationEnd].ExecutionDomain == domain &&
                       operations[operationEnd].OutputPoseSpace == outputSpace &&
                       operations[operationEnd].LinkedPoseFragmentIndex == first.LinkedPoseFragmentIndex)
                {
                    operationEnd++;
                }

                CharacterPoseSpace inputSpace = CharacterPoseSpace.None;
                int nativeOperationCount = 0;
                int minPoseValue = int.MaxValue;
                int maxPoseValue = -1;
                for (int operationIndex = operationStart; operationIndex < operationEnd; operationIndex++)
                {
                    CharacterPresentationPoseOperation operation = operations[operationIndex];
                    if (inputSpace == CharacterPoseSpace.None && operation.InputPoseSpace != CharacterPoseSpace.None)
                        inputSpace = operation.InputPoseSpace;
                    if (IsNativePoseOperation(operation.Code))
                        nativeOperationCount++;
                    if (operation.OutputValueIndex < 0)
                        continue;
                    minPoseValue = Math.Min(minPoseValue, operation.OutputValueIndex);
                    maxPoseValue = Math.Max(maxPoseValue, operation.OutputValueIndex);
                }

                stages.Add(new CharacterPresentationPoseStage(
                    stages.Count,
                    domain,
                    inputSpace,
                    outputSpace,
                    operationStart,
                    operationEnd - operationStart,
                    nativeOperationStart,
                    nativeOperationCount,
                    maxPoseValue < 0 ? 0 : minPoseValue,
                    maxPoseValue < 0 ? 0 : maxPoseValue - minPoseValue + 1));
                nativeOperationStart += nativeOperationCount;
                operationStart = operationEnd;
            }
            return stages.ToArray();
        }

        static void BindLinkedPoseStageRanges(
            IReadOnlyList<CharacterLinkedPoseEntryFragmentPlanDescriptor> fragments,
            IReadOnlyList<CharacterPresentationPoseStage> stages)
        {
            int stageIndex = 0;
            for (int fragmentIndex = 0; fragmentIndex < fragments.Count; fragmentIndex++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = fragments[fragmentIndex];
                int operationEnd = checked(fragment.OperationStart + fragment.OperationCount);
                while (stageIndex < stages.Count &&
                       stages[stageIndex].OperationStart + stages[stageIndex].OperationCount <= fragment.OperationStart)
                {
                    stageIndex++;
                }
                int stageStart = stageIndex;
                while (stageIndex < stages.Count && stages[stageIndex].OperationStart < operationEnd)
                {
                    CharacterPresentationPoseStage stage = stages[stageIndex];
                    if (stage.OperationStart < fragment.OperationStart ||
                        stage.OperationStart + stage.OperationCount > operationEnd)
                    {
                        throw new InvalidOperationException($"Linked Pose fragment #{fragment.Index} does not own an isolated stage range.");
                    }
                    stageIndex++;
                }
                fragment.BindStageRange(stageStart, stageIndex - stageStart);
            }
        }

        static CharacterPoseSpace ResolveInputPoseSpace(CharacterTypedPoseNode node) =>
            ResolvePoseSpace(node, CharacterPosePortDirection.Input);

        static CharacterPoseSpace ResolveOutputPoseSpace(
            CharacterTypedPoseNode node,
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
            CharacterTypedPoseNode node,
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

        static bool IsNativePoseOperation(CharacterPoseOperationCode code) => code switch
        {
            CharacterPoseOperationCode.SelectedPosePlayer or
            CharacterPoseOperationCode.BlendSpacePlayer or
            CharacterPoseOperationCode.SequencePlayer or
            CharacterPoseOperationCode.BlendStack or
            CharacterPoseOperationCode.AnimationSlot or
            CharacterPoseOperationCode.Inertialization or
            CharacterPoseOperationCode.BlendPose or
            CharacterPoseOperationCode.LayeredBoneBlend or
            CharacterPoseOperationCode.AdditivePose or
            CharacterPoseOperationCode.PoseParameterResolve or
            CharacterPoseOperationCode.ModifyBone or
            CharacterPoseOperationCode.RootOrientationWarp or
            CharacterPoseOperationCode.PredictiveFootPlacement or
            CharacterPoseOperationCode.PoseBoneIKGoals or
            CharacterPoseOperationCode.FullBodyIK or
            CharacterPoseOperationCode.LocalToComponentPose or
            CharacterPoseOperationCode.ComponentToLocalPose or
            CharacterPoseOperationCode.StatePoseOutput or
            CharacterPoseOperationCode.PoseStateMachine or
            CharacterPoseOperationCode.LinkedPoseCall or
            CharacterPoseOperationCode.EmptyFullBodyIkGoals or
            CharacterPoseOperationCode.MotionMatchingPose or
            CharacterPoseOperationCode.PoseHistoryRead or
            CharacterPoseOperationCode.OutputPose => true,
            _ => false
        };

        static NativeWorkspacePlan PlanNativeWorkspace(CompilationState state)
        {
            if (state.PoseValueCount <= 0 || state.Operations.Count <= 0)
                throw new InvalidOperationException("Pose Native plan requires operations and Pose values.");
            var producer = Enumerable.Repeat(-1, state.PoseValueCount).ToArray();
            var lastUse = Enumerable.Repeat(-1, state.PoseValueCount).ToArray();
            var goalSetProducer = Enumerable.Repeat(-1, state.FullBodyIkGoalSetValueCount).ToArray();
            var goalSetLastUse = Enumerable.Repeat(-1, state.FullBodyIkGoalSetValueCount).ToArray();
            int contributionCapacityPerValue = 0;
            for (int i = 0; i < state.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = state.Operations[i];
                if (operation.Index != i)
                    throw new InvalidOperationException(
                        $"Pose Native plan operation '{operation.NodeId}' has non-linear index '{operation.Index}'.");
                RegisterPoseOutput(operation.OutputValueIndex, i, producer, lastUse, operation.NodeId);
                RegisterPoseInput(operation.InputValueIndexA, i, producer, lastUse, operation.NodeId);
                RegisterPoseInput(operation.InputValueIndexB, i, producer, lastUse, operation.NodeId);
                RegisterFullBodyIkGoalSetOutput(
                    operation.OutputFullBodyIkGoalSetValueIndex,
                    i,
                    goalSetProducer,
                    goalSetLastUse,
                    operation.NodeId);
                for (int inputIndex = 0; inputIndex < operation.FullBodyIkGoalInputCount; inputIndex++)
                    RegisterFullBodyIkGoalSetInput(
                        state.FullBodyIkGoalInputValueIndices[operation.FullBodyIkGoalInputStart + inputIndex],
                        i,
                        goalSetProducer,
                        goalSetLastUse,
                        operation.NodeId);
                if (operation.Code == CharacterPoseOperationCode.LinkedPoseCall)
                {
                    CharacterLinkedPoseCallPlanDescriptor call = state.LinkedCalls[operation.LinkedPoseCallIndex];
                    for (int fragmentOffset = 0; fragmentOffset < call.FragmentIndices.Count; fragmentOffset++)
                    {
                        CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = state.LinkedFragments[call.FragmentIndices[fragmentOffset]];
                        for (int outputIndex = 0; outputIndex < fragment.Outputs.Count; outputIndex++)
                        {
                            CharacterLinkedPosePortValueBinding binding = fragment.Outputs[outputIndex];
                            if (binding.Kind == CharacterPosePortKind.FullBodyIkGoals)
                                RegisterFullBodyIkGoalSetInput(binding.ValueIndex, i, goalSetProducer, goalSetLastUse, operation.NodeId);
                            else if (binding.Kind == CharacterPosePortKind.LocalPose || binding.Kind == CharacterPosePortKind.ComponentPose)
                                RegisterPoseInput(binding.ValueIndex, i, producer, lastUse, operation.NodeId);
                        }
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer ||
                    operation.Code == CharacterPoseOperationCode.BlendSpacePlayer ||
                    operation.Code == CharacterPoseOperationCode.SequencePlayer)
                {
                    contributionCapacityPerValue = checked(contributionCapacityPerValue + 1);
                    continue;
                }
                if (operation.Code != CharacterPoseOperationCode.BlendStack &&
                    operation.Code != CharacterPoseOperationCode.AnimationSlot ||
                    operation.BlendNodeIndex < 0 || operation.BlendNodeIndex >= state.BlendNodes.Length)
                    continue;
                AnimationBlendNodePayload blendNode = state.BlendNodes[operation.BlendNodeIndex];
                if (blendNode?.StackPolicy == null || blendNode.StackPolicy.MaxActiveSourceEntries <= 0)
                    throw new InvalidOperationException($"Pose Plan Blend Stack '{operation.NodeId}' has an invalid contribution capacity.");
                contributionCapacityPerValue = checked(
                    contributionCapacityPerValue +
                    blendNode.StackPolicy.MaxActiveSourceEntries +
                    1);
            }
            if ((uint)state.OutputOperationIndex >= (uint)state.Operations.Count)
                throw new InvalidOperationException("Pose Native plan output operation is outside the linear operation list.");
            CharacterPresentationPoseOperation output = state.Operations[state.OutputOperationIndex];
            if ((uint)output.OutputValueIndex >= (uint)lastUse.Length)
                throw new InvalidOperationException("Pose Native plan output operation does not publish a Pose value.");
            lastUse[output.OutputValueIndex] = state.Operations.Count;
            for (int i = 0; i < producer.Length; i++)
            {
                if (producer[i] < 0 || lastUse[i] < producer[i])
                    throw new InvalidOperationException($"Pose Native plan value '{i}' has an invalid lifetime.");
            }
            for (int i = 0; i < goalSetProducer.Length; i++)
            {
                if (goalSetProducer[i] < 0 || goalSetLastUse[i] <= goalSetProducer[i])
                    throw new InvalidOperationException($"Pose Native plan Full Body IK Goal Set value '{i}' has an invalid lifetime.");
            }
            if (contributionCapacityPerValue <= 0)
                throw new InvalidOperationException("Pose Plan requires at least one Player contribution capacity.");
            return new NativeWorkspacePlan(
                state.PoseValueCount,
                state.Parameters.Length,
                checked(state.PoseValueCount * contributionCapacityPerValue),
                state.Operations.Count,
                lastUse);
        }

        static void RegisterPoseOutput(
            int valueIndex,
            int operationIndex,
            int[] producer,
            int[] lastUse,
            PoseNodeId nodeId)
        {
            if (valueIndex < 0)
                return;
            if ((uint)valueIndex >= (uint)producer.Length || producer[valueIndex] >= 0)
                throw new InvalidOperationException(
                    $"Pose Native plan node '{nodeId}' publishes invalid or duplicate value '{valueIndex}'.");
            producer[valueIndex] = operationIndex;
            lastUse[valueIndex] = operationIndex;
        }

        static void RegisterPoseInput(
            int valueIndex,
            int operationIndex,
            int[] producer,
            int[] lastUse,
            PoseNodeId nodeId)
        {
            if (valueIndex < 0)
                return;
            if ((uint)valueIndex >= (uint)producer.Length || producer[valueIndex] < 0 ||
                producer[valueIndex] >= operationIndex)
            {
                throw new InvalidOperationException(
                    $"Pose Native plan node '{nodeId}' consumes Pose value '{valueIndex}' before it is published.");
            }
            lastUse[valueIndex] = Math.Max(lastUse[valueIndex], operationIndex);
        }

        static void RegisterFullBodyIkGoalSetOutput(
            int valueIndex,
            int operationIndex,
            int[] producer,
            int[] lastUse,
            PoseNodeId nodeId)
        {
            if (valueIndex < 0)
                return;
            if ((uint)valueIndex >= (uint)producer.Length || producer[valueIndex] >= 0)
                throw new InvalidOperationException(
                    $"Pose Native plan node '{nodeId}' publishes invalid or duplicate Full Body IK Goal Set value '{valueIndex}'.");
            producer[valueIndex] = operationIndex;
            lastUse[valueIndex] = operationIndex;
        }

        static void RegisterFullBodyIkGoalSetInput(
            int valueIndex,
            int operationIndex,
            int[] producer,
            int[] lastUse,
            PoseNodeId nodeId)
        {
            if (valueIndex < 0)
                return;
            if ((uint)valueIndex >= (uint)producer.Length || producer[valueIndex] < 0 ||
                producer[valueIndex] >= operationIndex)
            {
                throw new InvalidOperationException(
                    $"Pose Native plan node '{nodeId}' consumes Full Body IK Goal Set value '{valueIndex}' before it is published.");
            }
            lastUse[valueIndex] = Math.Max(lastUse[valueIndex], operationIndex);
        }

        static Dictionary<PoseInterfacePortId, CompiledValue> CompileGraph(
            CompilationState state,
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterTypedPoseGraph graph,
            IReadOnlyDictionary<PoseInterfacePortId, CompiledValue> imports,
            string scope,
            string callChain,
            bool root,
            Action<int> stateOutput = null,
            int linkedPoseFragmentIndex = -1)
        {
            string graphStackKey = CharacterPresentationAssetObjectIdentity.Require(ownerAsset) + "\0" + graph.GraphId.Value;
            if (!state.GraphCallStack.Add(graphStackKey))
            {
                string path = string.IsNullOrEmpty(callChain)
                    ? graph.GraphId.Value
                    : callChain + " -> " + graph.GraphId.Value;
                throw new InvalidOperationException(
                    $"Pose Graph catalog contains a recursive call: {path}.");
            }
            state.GraphDependencies.Add($"{CharacterPresentationAssetObjectIdentity.Require(ownerAsset)}\0{callChain}\0{graph.GraphId}\0{graph.ContentRevision}");
            CharacterPoseIrGraphRole graphRole = linkedPoseFragmentIndex >= 0 && stateOutput == null
                ? CharacterPoseIrGraphRole.LinkedPoseEntry
                : root
                ? CharacterPoseIrGraphRole.Root
                : stateOutput != null
                    ? CharacterPoseIrGraphRole.StateLocal
                    : CharacterPoseIrGraphRole.Subgraph;
            CharacterPoseIrGraph ir = new CharacterPoseIrCompiler().Compile(graph, graphRole);
            Dictionary<PoseNodeId, CharacterTypedPoseNode> nodes = graph.Nodes.ToDictionary(value => value.NodeId);
            Dictionary<string, CharacterPoseEdge> incoming = BuildIncoming(graph);
            var values = new Dictionary<string, CompiledValue>(StringComparer.Ordinal);
            var exports = new Dictionary<PoseInterfacePortId, CompiledValue>();
            for (int nodeIndex = 0; nodeIndex < ir.Nodes.Count; nodeIndex++)
            {
                CharacterPoseIrNode irNode = ir.Nodes[nodeIndex];
                CharacterTypedPoseNode node = nodes[new PoseNodeId(irNode.NodeId.Value)];
                ICharacterPoseCompilerHandler handler =
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
                        linkedPoseFragmentIndex);
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
                int stateMachineIndex = handler.StateMachine
                    ? CompileStateMachine(
                        ownerAsset,
                        RequirePayload<CharacterPoseStateMachineNodePayload>(irNode),
                        scopedNodeId,
                        state,
                        scope,
                        callChain,
                        linkedPoseFragmentIndex)
                    : -1;
                int operationIndex = state.Operations.Count;
                CharacterPoseOperationCode code =
                    handler.NativeRole ==
                    CharacterPoseNativeNodeRole.PoseOutput &&
                    stateOutput != null
                        ? CharacterPoseOperationCode.StatePoseOutput
                        : handler.Code;
                int outputValueIndex = HasPoseOutput(node) ||
                                       handler.NativeRole ==
                                       CharacterPoseNativeNodeRole
                                           .PoseOutput
                    ? state.PoseValueCount++
                    : -1;
                int outputFullBodyIkGoalSetValueIndex = HasOutput(
                    node,
                    CharacterPosePortKind.FullBodyIkGoals)
                    ? state.FullBodyIkGoalSetValueCount++
                    : -1;
                int inputA = RequireOptionalPoseInput(node, 0, incoming, scope, values);
                int inputB = RequireOptionalPoseInput(node, 1, incoming, scope, values);
                int[] fullBodyIkGoalInputs = GetInputIndices(
                    node,
                    CharacterPosePortKind.FullBodyIkGoals,
                    incoming,
                    scope,
                    values);
                int fullBodyIkGoalInputStart = fullBodyIkGoalInputs.Length == 0
                    ? -1
                    : state.FullBodyIkGoalInputValueIndices.Count;
                state.FullBodyIkGoalInputValueIndices.AddRange(fullBodyIkGoalInputs);
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
                if (handler.ActionPlaybackControl)
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
                if (handler.Player)
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

                int blendNodeIndex = handler.BlendPolicy
                    ? state.BlendNodeIndices.TryGetValue(scopedNodeId, out int index)
                        ? index
                        : throw new InvalidOperationException($"Animation transition owner '{scopedNodeId}' has no compiled policy payload.")
                    : -1;
                int animationSlotIndex = handler.AnimationSlot
                    ? CompileAnimationSlot(
                        RequirePayload<CharacterAnimationSlotPosePayload>(irNode),
                        scopedNodeId,
                        inputA,
                        controlInputOperationIndex,
                        playerIndex,
                        blendNodeIndex,
                        state)
                    : -1;
                int inertializationIndex = handler.Inertialization
                    ? CompileInertialization(state)
                    : -1;
                CharacterAnimationBoneMaskAsset boneMask = handler.BoneMask(irNode.Payload);
                int maskIndex = boneMask
                    ? CompileMask(boneMask, state.Rig, state.Masks, state.MaskIndices)
                    : -1;
                int additiveIndex = handler.Additive
                    ? CompileAdditiveReference(
                        RequirePayload<CharacterAdditivePosePayload>(irNode),
                        state.Rig,
                        state.AdditiveReferences)
                    : -1;
                int modifyIndex = handler.ModifyBone
                    ? CompileModifyBone(
                        RequirePayload<CharacterModifyBonePosePayload>(irNode),
                        state)
                    : -1;
                int rootOrientationWarpIndex = handler.RootOrientationWarp
                    ? CompileRootOrientationWarp(
                        RequirePayload<CharacterRootOrientationWarpPosePayload>(irNode),
                        scopedNodeId,
                        inputA,
                        state)
                    : -1;
                int poseBoneIkGoalsIndex = handler.Kind == CharacterPoseNodeKind.PoseBoneIKGoals
                    ? CompilePoseBoneIkGoals(
                        RequirePayload<CharacterPoseBoneIkGoalsPayload>(irNode),
                        scopedNodeId,
                        state)
                    : -1;
                int predictiveFootPlacementIndex = handler.Kind == CharacterPoseNodeKind.PredictiveFootPlacement
                    ? CompilePredictiveFootPlacement(
                        RequirePayload<CharacterPredictiveFootPlacementPosePayload>(irNode),
                        scopedNodeId,
                        state)
                    : -1;
                int fullBodyIkIndex = handler.Kind == CharacterPoseNodeKind.FullBodyIK
                    ? CompileFullBodyIk(
                        RequirePayload<CharacterFullBodyIkPosePayload>(irNode),
                        scopedNodeId,
                        state)
                    : -1;
                int sequencePlayerIndex = handler.SequencePlayer
                    ? CompileSequencePlayer(
                        RequirePayload<CharacterSequencePlayerPosePayload>(irNode),
                        scopedNodeId,
                        playerIndex,
                        state)
                    : -1;
                PoseParameterResolvePolicy[] policies = CompilePolicies(
                    handler.ParameterPolicies(irNode.Payload),
                    state.Parameters,
                    state.ParameterIndices);
                PresentationPoseSourceProviderId provider = handler.Player
                    ? new PresentationPoseSourceProviderId($"pose-provider/{scopedNodeId}")
                    : default;
                CharacterPresentationPoseSourceSlot sourceSlot = handler.Source(irNode.Payload);
                PresentationPoseSourceIndex sourceIndex = default;
                if (sourceSlot && !state.SourceIndices.TryGetValue(sourceSlot, out sourceIndex))
                    throw new InvalidOperationException($"Pose Player '{scopedNodeId}' Source Slot is outside the compiled source catalog.");
                state.Operations.Add(new CharacterPresentationPoseOperation(
                    operationIndex,
                    handler.NativeRole == CharacterPoseNativeNodeRole.PoseOutput && stateOutput != null
                        ? CharacterPoseExecutionDomain.PurePose
                        : linkedPoseCall.ExecutionDomain,
                    ResolveInputPoseSpace(node),
                    ResolveOutputPoseSpace(node, code),
                    code,
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
                    inertializationIndex,
                    maskIndex,
                    additiveIndex,
                    modifyIndex,
                    rootOrientationWarpIndex,
                    poseBoneIkGoalsIndex,
                    predictiveFootPlacementIndex,
                    fullBodyIkIndex,
                    outputFullBodyIkGoalSetValueIndex,
                    fullBodyIkGoalInputStart,
                    fullBodyIkGoalInputs.Length,
                    sequencePlayerIndex,
                    stateMachineIndex,
                    animationSlotIndex,
                    linkedPoseCall.CallIndex,
                    linkedPoseFragmentIndex,
                    handler.Weight(irNode.Payload),
                    policies));
                state.SourceMap.Add(new CharacterPresentationPoseSourceMapEntry(operationIndex, graph.GraphId.Value, scopedNodeId, callChain));

                BindOperationOutputs(
                    node,
                    scope,
                    outputValueIndex,
                    outputFullBodyIkGoalSetValueIndex,
                    parameterIndex,
                    operationIndex,
                    values);
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
            state.GraphCallStack.Remove(graphStackKey);
            return exports;
        }

        static LinkedPoseCallCompilation CompileLinkedPoseCall(
            CompilationState state,
            CharacterTypedPoseNode call,
            Dictionary<string, CharacterPoseEdge> incoming,
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
                CharacterTypedPoseGraph entryGraph = entryBinding.RequireValid();
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
                    fragmentIndex);
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

        static int CompileAnimationSlot(
            CharacterAnimationSlotPosePayload payload,
            PoseNodeId scopedNodeId,
            int sourcePoseValueIndex,
            int actionPlaybackOperationIndex,
            int playerIndex,
            int blendNodeIndex,
            CompilationState state)
        {
            if (sourcePoseValueIndex < 0 || actionPlaybackOperationIndex < 0 ||
                playerIndex < 0 || blendNodeIndex < 0)
                throw new InvalidOperationException($"Animation Slot '{scopedNodeId}' has an incomplete compiled input.");
            if ((uint)actionPlaybackOperationIndex >= (uint)state.Operations.Count)
                throw new InvalidOperationException(
                    $"Animation Slot '{scopedNodeId}' Action Playback operation is outside the compiled graph.");
            CharacterPresentationPoseOperation actionPlayback =
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

        static int CompileModifyBone(
            CharacterModifyBonePosePayload payload,
            CompilationState state)
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

        static int CompilePoseBoneIkGoals(
            CharacterPoseBoneIkGoalsPayload payload,
            PoseNodeId scopedNodeId,
            CompilationState state)
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
            int goalOffset = state.FullBodyIkGoalWorkspaceCount;
            state.FullBodyIkGoalWorkspaceCount = checked(
                state.FullBodyIkGoalWorkspaceCount + bindings.Length);
            state.PoseBoneIkGoalSources.Add(
                new CharacterPresentationPoseBoneIkGoalsDescriptor(
                    index,
                    scopedNodeId,
                    goalOffset,
                    bindings));
            return index;
        }

        static int CompileInertialization(CompilationState state) =>
            state.InertializationCount++;

        static int CompilePredictiveFootPlacement(
            CharacterPredictiveFootPlacementPosePayload payload,
            PoseNodeId scopedNodeId,
            CompilationState state)
        {
            if (state.PredictiveFootPlacements.Count != 0)
                throw new InvalidOperationException("Pose Plan contains more than one Predictive Foot Placement node.");
            int index = state.PredictiveFootPlacements.Count;
            int goalOffset = state.FullBodyIkGoalWorkspaceCount;
            state.FullBodyIkGoalWorkspaceCount = checked(
                state.FullBodyIkGoalWorkspaceCount +
                CharacterPresentationPredictiveFootPlacementDescriptor.GoalCount);
            state.PredictiveFootPlacements.Add(
                new CharacterPresentationPredictiveFootPlacementDescriptor(
                    index,
                    scopedNodeId,
                    payload.Profile,
                    payload.Calibration,
                    goalOffset));
            return index;
        }

        static int CompileFullBodyIk(
            CharacterFullBodyIkPosePayload payload,
            PoseNodeId scopedNodeId,
            CompilationState state)
        {
            int index = state.FullBodyIks.Count;
            state.FullBodyIks.Add(
                new CharacterPresentationFullBodyIkDescriptor(
                    index,
                    scopedNodeId,
                    payload.Profile));
            return index;
        }

        static int CompileStateMachine(
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterPoseStateMachineNodePayload payload,
            PoseNodeId scopedNodeId,
            CompilationState state,
            string scope,
            string callChain,
            int linkedPoseFragmentIndex)
        {
            CharacterPoseStateMachineDefinition definition = payload.StateMachine;
            CharacterPoseStateMachineAuthoringValidator.RequireValid(
                definition,
                ownerAsset.RequireGraph);
            Dictionary<PoseStateAliasId, HashSet<PoseStateId>> aliases = ExpandAliases(definition);
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
                CharacterTypedPoseGraph stateGraph =
                    ownerAsset.RequireGraph(authored.PoseGraphId);
                ValidateStateParameters(authored, stateGraph, state.Parameters);
                int operationStart = state.Operations.Count;
                int outputValueIndex = -1;
                string stateScope = scopedNodeId.Value + "/state/" + authored.StateId.Value;
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
                    linkedPoseFragmentIndex);
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
                    CharacterPoseTransitionRuleCompiler.Compile(authored.Rule),
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

        static Dictionary<PoseStateAliasId, HashSet<PoseStateId>> ExpandAliases(
            CharacterPoseStateMachineDefinition definition)
        {
            var authored = definition.Aliases.ToDictionary(value => value.AliasId);
            var result = new Dictionary<PoseStateAliasId, HashSet<PoseStateId>>();
            var visiting = new HashSet<PoseStateAliasId>();
            foreach (CharacterPoseStateAlias alias in definition.Aliases.OrderBy(value => value.AliasId))
                ExpandAlias(alias.AliasId, authored, result, visiting);
            return result;
        }

        static HashSet<PoseStateId> ExpandAlias(
            PoseStateAliasId aliasId,
            IReadOnlyDictionary<PoseStateAliasId, CharacterPoseStateAlias> authored,
            Dictionary<PoseStateAliasId, HashSet<PoseStateId>> result,
            HashSet<PoseStateAliasId> visiting)
        {
            if (result.TryGetValue(aliasId, out HashSet<PoseStateId> existing))
                return existing;
            if (!visiting.Add(aliasId))
                throw new InvalidOperationException($"Pose State Alias cycle contains '{aliasId}'.");
            CharacterPoseStateAlias alias = authored[aliasId];
            var states = new HashSet<PoseStateId>();
            for (int i = 0; i < alias.Sources.Count; i++)
            {
                CharacterPoseStateTransitionSource source = alias.Sources[i];
                if (source.Kind == PoseStateTransitionSourceKind.State)
                    states.Add(source.StateId);
                else
                    states.UnionWith(ExpandAlias(source.AliasId, authored, result, visiting));
            }
            visiting.Remove(aliasId);
            if (states.Count == 0)
                throw new InvalidOperationException($"Pose State Alias '{aliasId}' expands to no State.");
            result.Add(aliasId, states);
            return states;
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
            CharacterTypedPoseGraph stateGraph,
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
            CompilationState state)
        {
            var result = new List<PoseStateSourceProviderPlan>();
            int end = checked(operationStart + operationCount);
            for (int operationIndex = operationStart; operationIndex < end; operationIndex++)
            {
                CharacterPresentationPoseOperation operation = state.Operations[operationIndex];
                AnimationPoseSourceKind sourceKind;
                PresentationPoseSourceIndex poseSourceIndex = default;
                if (operation.Code == CharacterPoseOperationCode.SequencePlayer)
                {
                    sourceKind = AnimationPoseSourceKind.Sequence;
                    poseSourceIndex = state.SequencePlayers[operation.SequencePlayerIndex].PresentationPoseSourceIndex;
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
            CompilationState state)
        {
            PoseStateSourceProviderPlan sourceUsage = FindSyncProvider(sourceState);
            PoseStateSourceProviderPlan targetUsage = FindSyncProvider(targetState);
            if (sourceUsage == null || targetUsage == null)
                return new CharacterPoseStateSourceSyncPlan(PoseStateSourceSyncMode.None);
            CharacterPresentationPoseSourcePlan source = state.PoseSources[sourceUsage.PresentationPoseSourceIndex];
            CharacterPresentationPoseSourcePlan target = state.PoseSources[targetUsage.PresentationPoseSourceIndex];
            AnimationMarkerSyncBinding sourceBinding = source.MarkerSync;
            AnimationMarkerSyncBinding targetBinding = target.MarkerSync;
            if (!sourceBinding.IsMarkerGroup || !targetBinding.IsMarkerGroup ||
                !string.Equals(
                    sourceBinding.CanonicalGroupId,
                    targetBinding.CanonicalGroupId,
                    StringComparison.Ordinal))
            {
                return new CharacterPoseStateSourceSyncPlan(PoseStateSourceSyncMode.None);
            }
            bool sourceIsLeader = ResolveStateSyncLeader(
                transition.TransitionId,
                sourceBinding.SyncRole,
                targetBinding.SyncRole);
            AnimationMarkerSyncBinding leader = sourceIsLeader ? sourceBinding : targetBinding;
            AnimationMarkerSyncBinding follower = sourceIsLeader ? targetBinding : sourceBinding;
            for (int i = 0; i < leader.Segments.Count; i++)
            {
                AnimationMarkerSyncSegmentOccurrence segment = leader.Segments[i];
                if (segment.Wraps)
                    continue;
                if (!follower.TryGetOccurrences(
                        segment.PreviousMarkerId,
                        segment.NextMarkerId,
                        out AnimationMarkerSyncSegmentOccurrence[] occurrences) ||
                    occurrences.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Pose State transition '{transition.TransitionId}' follower marker topology misses " +
                        $"'{segment.PreviousMarkerId}->{segment.NextMarkerId}'.");
                }
            }
            return new CharacterPoseStateSourceSyncPlan(
                $"pose-state-sync/{stateMachineId}/{transition.TransitionId}/{sourceState.StateId}",
                sourceUsage.PlayerIndex,
                targetUsage.PlayerIndex,
                source.SourceIndex,
                target.SourceIndex,
                sourceBinding.CanonicalGroupId,
                sourceIsLeader);
        }

        static PoseStateSourceProviderPlan FindSyncProvider(CharacterPoseStateDescriptor state)
        {
            PoseStateSourceProviderPlan result = null;
            for (int i = 0; i < state.SourceProviders.Count; i++)
            {
                PoseStateSourceProviderPlan candidate = state.SourceProviders[i];
                if (candidate.SourceKind != AnimationPoseSourceKind.Sequence &&
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

        static bool ResolveStateSyncLeader(
            PoseStateTransitionId transitionId,
            AnimationMarkerSyncRole source,
            AnimationMarkerSyncRole target)
        {
            if (source == AnimationMarkerSyncRole.AlwaysLeader && target == AnimationMarkerSyncRole.AlwaysLeader ||
                source == AnimationMarkerSyncRole.AlwaysFollower && target == AnimationMarkerSyncRole.AlwaysFollower)
            {
                throw new InvalidOperationException(
                    $"Pose State transition '{transitionId}' MarkerGroup roles do not resolve one leader.");
            }
            if (source == AnimationMarkerSyncRole.AlwaysLeader ||
                target == AnimationMarkerSyncRole.AlwaysFollower)
                return true;
            if (target == AnimationMarkerSyncRole.AlwaysLeader ||
                source == AnimationMarkerSyncRole.AlwaysFollower)
                return false;
            return true;
        }

        static TransitionEndpointId RoutingEndpoint(
            PoseStateMachineId stateMachineId,
            PoseStateId stateId) =>
            new TransitionEndpointId($"pose-state/{stateMachineId}/{stateId}");

        static TransitionRuleId RoutingRuleId(
            PoseStateTransitionId transitionId,
            PoseStateId sourceStateId) =>
            new TransitionRuleId($"pose-state/{transitionId}/{sourceStateId}");

        static int CompileSequencePlayer(
            CharacterSequencePlayerPosePayload payload,
            PoseNodeId scopedNodeId,
            int playerIndex,
            CompilationState state)
        {
            if (!payload.SourceSlot || !state.SourceIndices.TryGetValue(payload.SourceSlot, out PresentationPoseSourceIndex sourceIndex))
                throw new InvalidOperationException($"Sequence Player '{scopedNodeId}' Source Slot is outside the compiled source catalog.");
            int index = state.SequencePlayers.Count;
            state.SequencePlayers.Add(CharacterPresentationSequencePlayerCompiler.Compile(
                index,
                playerIndex,
                scopedNodeId,
                sourceIndex,
                payload));
            return index;
        }

        static int CompileRootOrientationWarp(
            CharacterRootOrientationWarpPosePayload payload,
            PoseNodeId scopedNodeId,
            int inputValueIndex,
            CompilationState state)
        {
            CharacterPresentationPoseOperation source = state.Operations
                .SingleOrDefault(value =>
                    value.OutputValueIndex == inputValueIndex);
            if (source == null ||
                source.Code != CharacterPoseOperationCode.SequencePlayer ||
                (uint)source.SequencePlayerIndex >=
                (uint)state.SequencePlayers.Count)
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{scopedNodeId}' must receive Pose directly from one Sequence Player.");
            }
            CharacterPresentationSequencePlayerDescriptor sequence =
                state.SequencePlayers[source.SequencePlayerIndex];
            CharacterPresentationPoseSourcePlan poseSource =
                state.PoseSources[sequence.PresentationPoseSourceIndex];
            if (sequence.Loop ||
                Math.Abs(poseSource.Clip.length - payload.YawCurve.Duration) >
                0.0001f)
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{scopedNodeId}' Yaw profile must match its finite Sequence duration.");
            }
            int index = state.RootOrientationWarps.Count;
            state.RootOrientationWarps.Add(
                new CharacterPresentationRootOrientationWarpDescriptor(
                    index,
                    scopedNodeId,
                    source.SequencePlayerIndex,
                    state.Rig.RequireRootBoneIndex(),
                    payload.YawCurve.Duration,
                    payload.YawCurve.TotalYaw,
                    payload.YawCurve.LocalYaw));
            return index;
        }

        static void BindGraphInputs(
            CharacterTypedPoseNode node,
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
            CharacterTypedPoseNode node,
            Dictionary<string, CharacterPoseEdge> incoming,
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
            CompilationState state,
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterTypedPoseGraph owner,
            CharacterTypedPoseNode callSite,
            Dictionary<string, CharacterPoseEdge> incoming,
            string scope,
            string callChain,
            Dictionary<string, CompiledValue> values,
            int linkedPoseFragmentIndex)
        {
            CharacterTypedPoseGraph child =
                ownerAsset.RequireGraph(callSite.Subgraph.PoseGraphId);
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
            string childScope = scopedCallSite.Value + "/" + child.GraphId;
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
                linkedPoseFragmentIndex);
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
            CharacterTypedPoseNode node,
            string scope,
            int poseValue,
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
                    _ => -1
                };
                if (index < 0)
                    throw new InvalidOperationException($"Pose Node '{node.NodeId}' output '{port.PortId}' has no compiled workspace value.");
                values.Add(EndpointKey(node.NodeId, port.PortId, scope), new CompiledValue(port.Kind, index, operationIndex));
            }
        }

        static CompiledValue RequireInput(
            CharacterTypedPoseNode node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseEdge> incoming,
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
            CharacterTypedPoseNode node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseEdge> incoming,
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
            CharacterTypedPoseNode node,
            CharacterPosePortKind kind,
            int ordinal,
            Dictionary<string, CharacterPoseEdge> incoming,
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
            CharacterTypedPoseNode node,
            CharacterPosePortKind kind,
            Dictionary<string, CharacterPoseEdge> incoming,
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
            CharacterTypedPoseNode node,
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

        static Dictionary<string, CharacterPoseEdge> BuildIncoming(CharacterTypedPoseGraph graph)
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
            CharacterTypedPoseNode node,
            int ordinal,
            Dictionary<string, CharacterPoseEdge> incoming,
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

        static bool HasPoseOutput(CharacterTypedPoseNode node) =>
            CharacterPoseAuthoringPortProjection.Get(node)
                .Any(value => value != null && IsPose(value.Kind) && value.Direction == CharacterPosePortDirection.Output);

        static bool HasOutput(
            CharacterTypedPoseNode node,
            CharacterPosePortKind kind) =>
            CharacterPoseAuthoringPortProjection.Get(node)
                .Any(value => value != null && value.Kind == kind &&
                              value.Direction == CharacterPosePortDirection.Output);

        static bool IsPose(CharacterPosePortKind kind) =>
            kind == CharacterPosePortKind.LocalPose ||
            kind == CharacterPosePortKind.ComponentPose;

        static ICharacterPoseCompilerHandler RequireNativeHandler(
            CharacterPoseIrNode node)
        {
            if (node == null)
                throw new InvalidOperationException(
                    "Pose IR node is missing.");
            ICharacterPoseCompilerHandler handler =
                CharacterPoseCompilerHandlerRegistry.Shared
                    .RequireCapability(node.CapabilityIdentity);
            handler.RequirePayload(node.Payload);
            return handler;
        }

        static TPayload RequirePayload<TPayload>(CharacterPoseIrNode node)
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
            string.IsNullOrEmpty(scope) ? nodeId : new PoseNodeId(scope + "/" + nodeId.Value);

        static PosePortId ScopePortId(PosePortId portId, string scope) =>
            string.IsNullOrEmpty(scope) ? portId : new PosePortId(scope + "/" + portId.Value);

        static string ComputeHash(
            CharacterTypedPoseGraph graph,
            CharacterAnimationRigDefinition rig,
            CompilationState state,
            IReadOnlyList<CharacterPresentationPoseStage> stages,
            int poseWorkspace,
            int parameterWorkspace,
            int contributionWorkspace,
            int frameCache)
        {
            var values = new List<string>
            {
                CharacterPresentationPosePlan.SchemaVersion,
                CharacterPresentationPosePlan.RuntimeAbi,
                graph.GraphId.Value,
                graph.ContentRevision,
                rig.RigId,
                rig.Revision
            };
            values.AddRange(state.GraphDependencies.Select(value => "graph:" + value));
            for (int i = 0; i < state.Parameters.Length; i++)
            {
                CharacterPresentationPoseParameterEntry parameter = state.Parameters[i];
                values.Add(FormattableString.Invariant($"parameter:{parameter.Index}:{parameter.ParameterId}:{(int)parameter.ValueType}:{parameter.Unit}:{parameter.DefaultValue:R}"));
            }
            for (int i = 0; i < state.BlendNodes.Length; i++)
            {
                AnimationBlendNodePayload blend = state.BlendNodes[i];
                values.Add(
                    $"blend:{blend.NodeId}:{blend.PolicyId}:{blend.PolicyRevision}:{blend.RoutingPlanId}:{blend.RoutingDefinitionRevision}:{blend.Transitions.Count}");
                for (int transitionIndex = 0; transitionIndex < blend.Transitions.Count; transitionIndex++)
                {
                    AnimationBlendTransitionPayload transition = blend.Transitions[transitionIndex];
                    values.Add(FormattableString.Invariant(
                        $"blend-transition:{i}:{transitionIndex}:{transition.SourceOwnerIndex}:{(int)transition.SourceEndpointKind}:{transition.SourceOwnerIdentity}:{transition.TargetOwnerIndex}:{(int)transition.TargetEndpointKind}:{transition.TargetOwnerIdentity}:{(int)transition.BlendLogic}:{transition.DurationSeconds:R}:{transition.CurveIndex}:{transition.BlendProfileIndex}"));
                }
            }
            for (int i = 0; i < rig.VirtualBoneCount; i++)
            {
                CharacterAnimationVirtualBoneDefinition bone = rig.VirtualBones[i];
                values.Add(
                    $"virtual-bone:{i}:{bone.VirtualBoneId}:{bone.DisplayName}:{bone.SourcePhysicalBoneId}:{bone.TargetPhysicalBoneId}");
            }
            for (int i = 0; i < state.PoseBoneIkGoalSources.Count; i++)
            {
                CharacterPresentationPoseBoneIkGoalsDescriptor descriptor =
                    state.PoseBoneIkGoalSources[i];
                values.Add($"pose-bone-ik-goals:{i}:{descriptor.NodeId}:{descriptor.GoalWorkspaceOffset}:{descriptor.GoalCount}");
                for (int bindingIndex = 0; bindingIndex < descriptor.Bindings.Count; bindingIndex++)
                {
                    CharacterPresentationPoseBoneIkGoalBindingDescriptor binding = descriptor.Bindings[bindingIndex];
                    values.Add(FormattableString.Invariant(
                        $"pose-bone-ik-goal:{i}:{bindingIndex}:{(int)binding.EffectorSlot}:{binding.TargetPoseBoneIndex}:{binding.PositionOffset.x:R}:{binding.PositionOffset.y:R}:{binding.PositionOffset.z:R}:{binding.RotationOffset.x:R}:{binding.RotationOffset.y:R}:{binding.RotationOffset.z:R}:{binding.RotationOffset.w:R}:{binding.PositionWeight:R}:{binding.RotationWeight:R}"));
                }
            }
            for (int i = 0; i < state.PredictiveFootPlacements.Count; i++)
            {
                CharacterPresentationPredictiveFootPlacementDescriptor descriptor =
                    state.PredictiveFootPlacements[i];
                values.Add(
                    $"predictive-foot-placement:{i}:{descriptor.NodeId}:{descriptor.Profile.ProfileId}:{descriptor.Profile.Revision}:{descriptor.CalibrationId}:{descriptor.CalibrationRevision}:{descriptor.BackendIdentity}:{descriptor.BackendSourceRevision}:{descriptor.GoalWorkspaceOffset}");
            }
            for (int i = 0; i < state.FullBodyIks.Count; i++)
            {
                CharacterPresentationFullBodyIkDescriptor descriptor = state.FullBodyIks[i];
                values.Add(
                    $"full-body-ik:{i}:{descriptor.NodeId}:{descriptor.ProfileId}:{descriptor.ProfileRevision}:{descriptor.BackendIdentity}:{descriptor.BackendSourceRevision}");
            }
            for (int i = 0; i < state.FullBodyIkGoalInputValueIndices.Count; i++)
                values.Add($"full-body-ik-goal-input:{i}:{state.FullBodyIkGoalInputValueIndices[i]}");
            for (int i = 0; i < state.SequencePlayers.Count; i++)
            {
                CharacterPresentationSequencePlayerDescriptor descriptor = state.SequencePlayers[i];
                values.Add(FormattableString.Invariant(
                    $"sequence-player:{descriptor.Index}:{descriptor.NodeId}:{descriptor.PresentationPoseSourceIndex.Value}:{descriptor.Loop}:{descriptor.PlayRate:R}:{descriptor.InitialTime:R}:{(int)descriptor.ClockSource}:{descriptor.PlayerIndex}"));
            }
            for (int i = 0; i < state.RootOrientationWarps.Count; i++)
            {
                CharacterPresentationRootOrientationWarpDescriptor descriptor =
                    state.RootOrientationWarps[i];
                Keyframe[] keys = descriptor.YawCurve.keys;
                values.Add(FormattableString.Invariant(
                    $"root-orientation-warp:{descriptor.Index}:{descriptor.NodeId}:{descriptor.SequencePlayerIndex}:{descriptor.RootPhysicalBoneIndex}:{descriptor.Duration:R}:{descriptor.TotalYaw:R}:{keys.Length}"));
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    Keyframe key = keys[keyIndex];
                    values.Add(FormattableString.Invariant(
                        $"root-orientation-warp-key:{i}:{keyIndex}:{key.time:R}:{key.value:R}:{key.inTangent:R}:{key.outTangent:R}:{key.inWeight:R}:{key.outWeight:R}:{(int)key.weightedMode}"));
                }
            }
            for (int i = 0; i < state.StateMachines.Count; i++)
            {
                CharacterPoseStateMachineDescriptor descriptor = state.StateMachines[i];
                values.Add(
                    $"state-machine:{descriptor.Index}:{descriptor.NodeId}:{descriptor.StateMachineId}:" +
                    $"{descriptor.ContentRevision}:{descriptor.EntryStateIndex}:{descriptor.MaxTransitionsPerFrame}:" +
                    $"{descriptor.StateWorkspaceCount}:{descriptor.TransitionWorkspaceCount}:" +
                    $"{descriptor.RoutingPlanId}:{descriptor.RoutingDefinitionRevision}");
                for (int stateIndex = 0; stateIndex < descriptor.States.Count; stateIndex++)
                {
                    CharacterPoseStateDescriptor poseState = descriptor.States[stateIndex];
                    values.Add(FormattableString.Invariant(
                        $"state:{i}:{poseState.Index}:{poseState.StateId}:{poseState.AlwaysResetOnEntry}"));
                }
                for (int transitionIndex = 0; transitionIndex < descriptor.Transitions.Count; transitionIndex++)
                {
                    CharacterPoseStateTransitionDescriptor transition = descriptor.Transitions[transitionIndex];
                    values.Add(FormattableString.Invariant(
                        $"state-transition:{i}:{transition.Index}:{transition.TransitionId}:{transition.SourceStateIndex}:{transition.TargetStateIndex}:{transition.Priority}:{transition.Rule.GraphId}:{transition.Rule.ContentRevision}:{(int)transition.BlendLogic}:{transition.DurationSeconds:R}:{transition.CompletionDurationSeconds:R}:{(int)transition.BlendMode}:{transition.CurveIndex}:{transition.BlendProfileIndex}:{transition.RoutingRuleId}:{(int)transition.SourceSync.Mode}:{transition.SourceSync.RelationId}"));
                }
            }
            for (int i = 0; i < state.AnimationSlots.Count; i++)
            {
                CharacterAnimationSlotDescriptor descriptor = state.AnimationSlots[i];
                values.Add(
                    $"animation-slot:{descriptor.Index}:{descriptor.NodeId}:{descriptor.SlotId}:{descriptor.AnimationChannelId}:" +
                    $"{descriptor.RoutingOwnerId}:{descriptor.RoutingPlanId}:{descriptor.RoutingDefinitionRevision}:" +
                    $"{descriptor.ActionPlayer.PlayerNodeId}:{descriptor.ActionPlayer.ActionPlaybackOperationIndex}:" +
                    $"{descriptor.ActionPlayer.PlayerIndex}:{descriptor.BlendStackWorkspace.BlendNodeIndex}:" +
                    $"{descriptor.BlendStackWorkspace.Capacity}:{descriptor.SourceUsage.SourcePoseValueIndex}");
                for (int routeIndex = 0; routeIndex < descriptor.RequestRoutes.Count; routeIndex++)
                {
                    CharacterAnimationSlotRequestRouteDescriptor route = descriptor.RequestRoutes[routeIndex];
                    values.Add(FormattableString.Invariant(
                        $"animation-slot-route:{i}:{routeIndex}:{route.RuleId}:{route.SourceEndpointId}:{route.TargetEndpointId}:{(int)route.BlendLogic}:{route.DurationSeconds:R}:{route.CurveIndex}:{route.BlendProfileIndex}:{route.RequiresTargetFirstSample}:{route.RequiresCaptureCompletion}"));
                }
            }
            values.Add($"inertial-count:{state.InertializationCount}");
            for (int i = 0; i < state.LinkedFragments.Count; i++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = state.LinkedFragments[i];
                values.Add($"linked-fragment:{fragment.Index}:{fragment.GroupId}:{fragment.InterfaceId}:{fragment.InterfaceSignature}:{fragment.ImplementationId}:{fragment.ImplementationRevision}:{fragment.EntryId}:{fragment.GraphId}:{fragment.GraphRevision}:{fragment.OperationStart}:{fragment.OperationCount}:{fragment.PoseValueStart}:{fragment.PoseValueCount}:{fragment.GoalSetValueStart}:{fragment.GoalSetValueCount}:{fragment.PlayerStart}:{fragment.PlayerCount}:{fragment.StateMachineStart}:{fragment.StateMachineCount}:{fragment.InertializationStart}:{fragment.InertializationCount}:{fragment.RootOrientationWarpStart}:{fragment.RootOrientationWarpCount}:{fragment.MotionMatchingProviderStart}:{fragment.MotionMatchingProviderCount}:{fragment.StageStart}:{fragment.StageCount}:{string.Join(",", fragment.SourceIndices)}");
                for (int bindingIndex = 0; bindingIndex < fragment.Inputs.Count; bindingIndex++)
                {
                    CharacterLinkedPosePortValueBinding binding = fragment.Inputs[bindingIndex];
                    values.Add($"linked-fragment-input:{i}:{bindingIndex}:{binding.PortId}:{(int)binding.Kind}:{binding.ValueIndex}");
                }
                for (int bindingIndex = 0; bindingIndex < fragment.Outputs.Count; bindingIndex++)
                {
                    CharacterLinkedPosePortValueBinding binding = fragment.Outputs[bindingIndex];
                    values.Add($"linked-fragment-output:{i}:{bindingIndex}:{binding.PortId}:{(int)binding.Kind}:{binding.ValueIndex}");
                }
            }
            for (int i = 0; i < state.LinkedCalls.Count; i++)
            {
                CharacterLinkedPoseCallPlanDescriptor call = state.LinkedCalls[i];
                values.Add($"linked-call:{call.Index}:{call.NodeId}:{call.GroupId}:{call.InterfaceId}:{call.InterfaceSignature}:{call.EntryId}:{(int)call.ExecutionDomain}:{string.Join(",", call.FragmentIndices)}");
            }
            for (int i = 0; i < state.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = state.Operations[i];
                values.Add(FormattableString.Invariant(
                    $"operation:{operation.Index}:{(int)operation.ExecutionDomain}:{(int)operation.InputPoseSpace}:{(int)operation.OutputPoseSpace}:{(int)operation.Code}:{operation.NodeId}:{operation.AnimationChannelId}:{(int)operation.SelectionAvailability}:{operation.OutputValueIndex}:{operation.InputValueIndexA}:{operation.InputValueIndexB}:{operation.OutputFullBodyIkGoalSetValueIndex}:{operation.FullBodyIkGoalInputStart}:{operation.FullBodyIkGoalInputCount}:{operation.ControlInputOperationIndex}:{operation.ParameterIndex}:{operation.ParameterIndexB}:{operation.PlayerIndex}:{operation.BlendNodeIndex}:{operation.InertializationIndex}:{operation.BoneMaskIndex}:{operation.AdditiveReferenceIndex}:{operation.ModifyBoneIndex}:{operation.RootOrientationWarpIndex}:{operation.PoseBoneIkGoalsIndex}:{operation.PredictiveFootPlacementIndex}:{operation.FullBodyIkIndex}:{operation.SequencePlayerIndex}:{operation.StateMachineIndex}:{operation.AnimationSlotIndex}:{operation.LinkedPoseCallIndex}:{operation.LinkedPoseFragmentIndex}:{operation.Weight:R}"));
            }
            for (int i = 0; i < stages.Count; i++)
            {
                CharacterPresentationPoseStage stage = stages[i];
                values.Add(
                    $"stage:{stage.Index}:{(int)stage.ExecutionDomain}:{(int)stage.InputPoseSpace}:{(int)stage.OutputPoseSpace}:{stage.OperationStart}:{stage.OperationCount}:{stage.NativeOperationStart}:{stage.NativeOperationCount}:{stage.PoseWorkspaceStart}:{stage.PoseWorkspaceCount}:{stage.CompletionIndex}:{stage.DiagnosticIndex}");
            }
            values.Add(FormattableString.Invariant($"workspace:{poseWorkspace}:{state.FullBodyIkGoalSetValueCount}:{state.FullBodyIkGoalWorkspaceCount}:{parameterWorkspace}:{contributionWorkspace}:{frameCache}:{state.OutputOperationIndex}"));
            return StableHash.Compute(values.ToArray()).ToString();
        }
    }
}
