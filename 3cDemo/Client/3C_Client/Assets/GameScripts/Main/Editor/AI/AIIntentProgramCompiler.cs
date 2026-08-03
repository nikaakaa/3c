using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.AI.Editor
{
    public enum AIIntentProgramPublishedStatus : byte
    {
        Missing = 1,
        Invalid = 2,
        Unchecked = 3
    }

    public static class AIIntentProgramBuildService
    {
        public static AIIntentProgramPublishedStatus InspectPublishedHeader(
            AIControllerDefinition definition,
            out string status)
        {
            if (!definition || !definition.IntentProgram)
            {
                status = "Missing generated AI Intent Program.";
                return AIIntentProgramPublishedStatus.Missing;
            }

            AIIntentProgramAsset program = definition.IntentProgram;
            if (!program.HasCanonicalArtifact ||
                string.IsNullOrWhiteSpace(program.ProgramId) ||
                string.IsNullOrWhiteSpace(program.ProgramHash) ||
                string.IsNullOrWhiteSpace(program.LayoutHash) ||
                string.IsNullOrWhiteSpace(program.SourceRevision) ||
                string.IsNullOrWhiteSpace(program.CharacterProgramId) ||
                string.IsNullOrWhiteSpace(program.CharacterProgramHash) ||
                string.IsNullOrWhiteSpace(program.PerceptionSchemaHash))
            {
                status = "Generated AI Intent Program header is invalid.";
                return AIIntentProgramPublishedStatus.Invalid;
            }

            status = $"Unchecked: {program.ProgramId} / {program.ProgramHash}";
            return AIIntentProgramPublishedStatus.Unchecked;
        }

        public static AIIntentProgramAsset CompileAndPublish(AIControllerDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            var errors = new List<string>();
            if (!definition.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
            CharacterSimulationProgram characterProgram = definition.ControlledCharacter.SimulationProgram
                ? definition.ControlledCharacter.SimulationProgram.Load()
                : throw new InvalidOperationException("Controlled Character Definition has no compiled Float32 Program.");
            var candidates = new ActorId[definition.PerceptionProfile.CandidateActorIds.Count];
            for (int i = 0; i < candidates.Length; i++)
                candidates[i] = new ActorId(definition.PerceptionProfile.CandidateActorIds[i]);
            var perception = new AIPerceptionDescriptor(
                candidates,
                definition.PerceptionProfile.Ordering == AICandidateOrdering.DistanceThenActorId);
            PrepareSourceGraph(definition);
            string sourceRevision = ComputeSourceRevision(definition, characterProgram, perception);
            AIIntentSemanticIr firstIr = AIIntentSemanticFrontend.Compile(definition, characterProgram, perception, sourceRevision);
            AIIntentSemanticIr secondIr = AIIntentSemanticFrontend.Compile(definition, characterProgram, perception, sourceRevision);
            byte[] firstIrBytes = AIIntentSemanticIrCodec.Write(firstIr);
            byte[] secondIrBytes = AIIntentSemanticIrCodec.Write(secondIr);
            RequireExact(firstIrBytes, secondIrBytes, "AI Semantic IR");
            AIIntentSemanticIr roundTripIr = AIIntentSemanticIrCodec.Read(firstIrBytes);
            if (!roundTripIr.SemanticHash.Equals(firstIr.SemanticHash))
                throw new InvalidOperationException("AI Semantic IR round trip changed its identity.");
            AIIntentProgram firstProgram = AIIntentProgramLowerer.Lower(firstIr);
            AIIntentProgram secondProgram = AIIntentProgramLowerer.Lower(secondIr);
            byte[] firstProgramBytes = AIIntentProgramCodec.WriteArtifact(firstProgram);
            byte[] secondProgramBytes = AIIntentProgramCodec.WriteArtifact(secondProgram);
            RequireExact(firstProgramBytes, secondProgramBytes, "AI Intent Program");
            AIIntentProgram roundTripProgram = AIIntentProgramCodec.ReadArtifact(firstProgramBytes);
            if (!roundTripProgram.ProgramHash.Equals(firstProgram.ProgramHash) ||
                !roundTripProgram.LayoutHash.Equals(firstProgram.LayoutHash))
            {
                throw new InvalidOperationException("AI Intent Program round trip changed its Program or Layout identity.");
            }
            return Publish(definition, firstProgram, firstProgramBytes);
        }

        public static void Validate(AIControllerDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            var errors = new List<string>();
            if (!definition.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
            CharacterSimulationProgram characterProgram = definition.ControlledCharacter.SimulationProgram
                ? definition.ControlledCharacter.SimulationProgram.Load()
                : throw new InvalidOperationException("Controlled Character Definition has no compiled Float32 Program.");
            var candidates = new ActorId[definition.PerceptionProfile.CandidateActorIds.Count];
            for (int i = 0; i < candidates.Length; i++)
                candidates[i] = new ActorId(definition.PerceptionProfile.CandidateActorIds[i]);
            var perception = new AIPerceptionDescriptor(candidates, definition.PerceptionProfile.Ordering == AICandidateOrdering.DistanceThenActorId);
            PrepareSourceGraph(definition);
            string sourceRevision = ComputeSourceRevision(definition, characterProgram, perception);
            AIIntentSemanticIr firstIr = AIIntentSemanticFrontend.Compile(definition, characterProgram, perception, sourceRevision);
            AIIntentSemanticIr secondIr = AIIntentSemanticFrontend.Compile(definition, characterProgram, perception, sourceRevision);
            byte[] firstIrBytes = AIIntentSemanticIrCodec.Write(firstIr);
            byte[] secondIrBytes = AIIntentSemanticIrCodec.Write(secondIr);
            RequireExact(firstIrBytes, secondIrBytes, "AI Semantic IR");
            AIIntentSemanticIr roundTripIr = AIIntentSemanticIrCodec.Read(firstIrBytes);
            if (!roundTripIr.SemanticHash.Equals(firstIr.SemanticHash))
                throw new InvalidOperationException("AI Semantic IR round trip changed its identity.");
            AIIntentProgram firstProgram = AIIntentProgramLowerer.Lower(firstIr);
            AIIntentProgram secondProgram = AIIntentProgramLowerer.Lower(secondIr);
            byte[] firstProgramBytes = AIIntentProgramCodec.WriteArtifact(firstProgram);
            byte[] secondProgramBytes = AIIntentProgramCodec.WriteArtifact(secondProgram);
            RequireExact(firstProgramBytes, secondProgramBytes, "AI Intent Program");
            AIIntentProgram roundTripProgram = AIIntentProgramCodec.ReadArtifact(firstProgramBytes);
            if (!roundTripProgram.ProgramHash.Equals(firstProgram.ProgramHash) || !roundTripProgram.LayoutHash.Equals(firstProgram.LayoutHash))
                throw new InvalidOperationException("AI Intent Program round trip changed its Program or Layout identity.");
        }

        public static bool IsCurrent(AIControllerDefinition definition, out string status)
        {
            status = string.Empty;
            if (!definition || !definition.IntentProgram || !definition.ControlledCharacter ||
                !definition.ControlledCharacter.SimulationProgram || !definition.PerceptionProfile)
            {
                status = "Missing generated AI Intent Program.";
                return false;
            }
            try
            {
                CharacterSimulationProgram characterProgram = definition.ControlledCharacter.SimulationProgram.Load();
                var candidates = definition.PerceptionProfile.CandidateActorIds.Select(value => new ActorId(value)).ToArray();
                var perception = new AIPerceptionDescriptor(
                    candidates,
                    definition.PerceptionProfile.Ordering == AICandidateOrdering.DistanceThenActorId);
                PrepareSourceGraph(definition);
                string expected = ComputeSourceRevision(definition, characterProgram, perception);
                AIIntentProgram program = definition.IntentProgram.Load();
                bool current = string.Equals(program.SemanticIr.SourceRevision, expected, StringComparison.Ordinal);
                status = current
                    ? $"Current: {program.ProgramId} / {program.ProgramHash}"
                    : "Generated AI Intent Program is stale.";
                return current;
            }
            catch (Exception exception)
            {
                status = exception.Message;
                return false;
            }
        }

        static AIIntentProgramAsset Publish(
            AIControllerDefinition definition,
            AIIntentProgram program,
            byte[] bytes)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string directory = Path.GetDirectoryName(definitionPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("AI Controller Definition must be a saved asset before compilation.");
            string generatedDirectory = directory + "/Generated";
            if (!AssetDatabase.IsValidFolder(generatedDirectory))
                AssetDatabase.CreateFolder(directory, "Generated");
            string path = generatedDirectory + "/" + definition.name + ".AIIntentProgram.asset";
            AIIntentProgramAsset asset = AssetDatabase.LoadAssetAtPath<AIIntentProgramAsset>(path);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<AIIntentProgramAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }
            Undo.RecordObject(asset, "Compile AI Intent Program");
            asset.SetProgram(program, bytes);
            EditorUtility.SetDirty(asset);
            Undo.RecordObject(definition, "Bind AI Intent Program");
            definition.SetIntentProgram(asset);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static string ComputeSourceRevision(
            AIControllerDefinition definition,
            CharacterSimulationProgram characterProgram,
            AIPerceptionDescriptor perception)
        {
            return AIControllerSourceRevision.Compute(
                definition,
                characterProgram.Manifest.ProgramId,
                characterProgram.ProgramHash,
                perception.SchemaHash);
        }

        static void PrepareSourceGraph(AIControllerDefinition definition)
        {
            if (definition.RootTreeAsset?.Tree is not AIControllerTree root)
                throw new InvalidOperationException("AI Controller RootTree asset does not contain AIControllerTree.");
            root.RebindReadOnlyViewReferences();
        }

        static void RequireExact(byte[] left, byte[] right, string label)
        {
            if (!left.SequenceEqual(right))
                throw new InvalidOperationException($"Two unchanged compiler passes produced different {label} canonical bytes.");
        }
    }

    static class AIIntentSemanticFrontend
    {
        sealed class GraphRecord
        {
            public GraphRecord(BaseTree graph)
            {
                Graph = graph;
                Route = graph.GraphAuthoringId;
            }

            public BaseTree Graph { get; }
            public string Route { get; }
        }

        public static AIIntentSemanticIr Compile(
            AIControllerDefinition definition,
            CharacterSimulationProgram characterProgram,
            AIPerceptionDescriptor perception,
            string sourceRevision)
        {
            if (definition.RootTreeAsset.Tree is not AIControllerTree root)
                throw new InvalidOperationException("AI Controller RootTree asset does not contain AIControllerTree.");
            var graphs = DiscoverGraphs(root);
            var memory = CompileMemory(graphs);
            var memoryByReference = memory.ToDictionary(
                item => item.Identity,
                item => item,
                StringComparer.Ordinal);
            BaseNode[] nodes = graphs
                .SelectMany(graph => graph.Graph.Nodes.Where(node => node != null))
                .OrderBy(node => NodeIdentity(node.Owner, node), StringComparer.Ordinal)
                .ToArray();
            var handles = new Dictionary<BaseNode, OperationHandle>();
            for (int i = 0; i < nodes.Length; i++)
                handles.Add(nodes[i], new OperationHandle(i));
            var inputCatalog = new CharacterInputCatalogRuntime(characterProgram);
            var operations = new AIIntentSemanticOperation[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
                operations[i] = CompileNode(nodes[i], handles[nodes[i]], memoryByReference, inputCatalog);
            var edges = new List<ProgramControlFlowEdge>();
            for (int graphIndex = 0; graphIndex < graphs.Count; graphIndex++)
                CompileEdges(graphs[graphIndex].Graph, handles, edges);
            if (!root.GUIDNodeMap.TryGetValue(root.RootGUID, out BaseNode rootNode) || !handles.TryGetValue(rootNode, out OperationHandle rootHandle))
                throw new InvalidOperationException("AI Controller Tree has no resolved RootNode.");
            return new AIIntentSemanticIr(
                definition.ControllerId,
                sourceRevision,
                characterProgram.Manifest.ProgramId,
                characterProgram.ProgramHash,
                perception.SchemaHash,
                rootHandle,
                operations,
                edges,
                memory);
        }

        static List<GraphRecord> DiscoverGraphs(AIControllerTree root)
        {
            var result = new List<GraphRecord>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<BaseTree>();
            pending.Enqueue(root);
            while (pending.Count != 0)
            {
                BaseTree graph = pending.Dequeue();
                if (graph == null || !visited.Add(graph.GraphAuthoringId))
                    continue;
                graph.RebindReadOnlyViewReferences();
                if (graph.AuthoringRole != GraphAuthoringRole.AIController)
                    throw new InvalidOperationException($"AI graph '{graph.GraphAuthoringId}' has role '{graph.AuthoringRole}'.");
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    BaseNode node = graph.Nodes[i] ?? throw new InvalidOperationException($"AI graph '{graph.GraphAuthoringId}' contains a missing node.");
                    if (node is not ConditionRuleResultNode &&
                        (!NodeAuthoringCapabilityPolicy.TryGetCapability(node.GetType(), out NodeAuthoringCapability capability) ||
                         !NodeAuthoringCapabilityPolicy.Allows(GraphAuthoringRole.AIController, capability)))
                    {
                        throw new InvalidOperationException($"AI graph node '{NodeIdentity(graph, node)}' has no allowed authoring capability.");
                    }
                    RequireSupportedNode(node);
                }
                result.Add(new GraphRecord(graph));
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    BaseEdge edge = graph.Edges[i] ?? throw new InvalidOperationException($"AI graph '{graph.GraphAuthoringId}' contains a missing edge.");
                    if (!edge.HasConditionRuleGraphConfiguration)
                        continue;
                    if (!edge.TryResolveConditionRuleGraph(out ConditionRuleGraph condition, out string error))
                        throw new InvalidOperationException($"AI edge '{edge.GUID}' ConditionRuleGraph is invalid: {error}");
                    pending.Enqueue(condition);
                }
            }
            result.Sort((left, right) => string.CompareOrdinal(left.Route, right.Route));
            return result;
        }

        static List<AIIntentMemoryDeclaration> CompileMemory(IReadOnlyList<GraphRecord> graphs)
        {
            var declarations = graphs
                .SelectMany(graph => graph.Graph.ExposedProperties.Where(value => value != null))
                .OrderBy(value => MemoryIdentity(value.DeclarationOwnerId, value.DeclarationId), StringComparer.Ordinal)
                .ToArray();
            var result = new List<AIIntentMemoryDeclaration>(declarations.Length);
            for (int i = 0; i < declarations.Length; i++)
            {
                BaseExposedProperty declaration = declarations[i];
                AIMemoryScope scope = declaration.BlackboardScope switch
                {
                    PipelineBlackboardVariableScope.AIController => AIMemoryScope.Controller,
                    PipelineBlackboardVariableScope.Graph => AIMemoryScope.Controller,
                    PipelineBlackboardVariableScope.AITick => AIMemoryScope.Tick,
                    _ => throw new InvalidOperationException($"AI declaration '{declaration.DeclarationId}' uses forbidden scope '{declaration.BlackboardScope}'.")
                };
                if (declaration.BlackboardLifetime != PipelineBlackboardVariablePolicy.DefaultLifetime(declaration.BlackboardScope) ||
                    declaration.BlackboardAuthority != PipelineBlackboardVariableAuthority.LocalOnly ||
                    declaration.BlackboardSyncPolicy != PipelineBlackboardVariableSyncPolicy.None)
                {
                    throw new InvalidOperationException($"AI declaration '{declaration.DeclarationId}' has incompatible lifetime, authority or sync policy.");
                }
                ReadDefault(declaration.GetValue(), out AIIntentValueKind kind, out int integer0, out double x, out double y, out double z, out double w, out string text);
                result.Add(new AIIntentMemoryDeclaration(
                    i,
                    MemoryIdentity(declaration.DeclarationOwnerId, declaration.DeclarationId),
                    scope,
                    kind,
                    integer0,
                    x,
                    y,
                    z,
                    w,
                    text));
            }
            return result;
        }

        static AIIntentSemanticOperation CompileNode(
            BaseNode node,
            OperationHandle handle,
            IReadOnlyDictionary<string, AIIntentMemoryDeclaration> memory,
            CharacterInputCatalogRuntime inputCatalog)
        {
            SimulationOperationCode code = Code(node);
            string binding = string.Empty;
            string memoryIdentity = string.Empty;
            AIIntentValueKind valueKind = AIIntentValueKind.Boolean;
            int integer0 = 0;
            int integer1 = 0;
            ulong unsigned0 = 0;
            double scalar0 = 0d;
            double scalar1 = 0d;
            double scalar2 = 0d;
            double scalar3 = 0d;
            switch (node)
            {
                case LoopNode loop:
                    integer0 = (int)loop.LoopStopType;
                    break;
                case ParallelNode parallel:
                    integer0 = (int)parallel.Mode;
                    break;
                case CompareNode compare:
                    integer0 = (int)compare.Comparison;
                    valueKind = AIIntentValueKind.Scalar;
                    scalar0 = ReadNumericPort(node, "Value1");
                    scalar1 = ReadNumericPort(node, "Value2");
                    break;
                case AndNode:
                    integer0 = ReadBooleanPort(node, "Input1") ? 1 : 0;
                    integer1 = ReadBooleanPort(node, "Input2") ? 1 : 0;
                    break;
                case OrNode:
                    integer0 = ReadBooleanPort(node, "Input1") ? 1 : 0;
                    integer1 = ReadBooleanPort(node, "Input2") ? 1 : 0;
                    break;
                case NotNode:
                    integer0 = ReadBooleanPort(node, "Input") ? 1 : 0;
                    break;
                case ConditionRuleResultNode:
                    integer0 = ReadBooleanPort(node, "Result") ? 1 : 0;
                    break;
                case ReadSelfObservationNode:
                    valueKind = AIIntentValueKind.Vector3;
                    break;
                case EnumerateConfiguredCandidatesNode:
                    valueKind = AIIntentValueKind.Integer;
                    break;
                case ReadTargetDistanceNode:
                    valueKind = AIIntentValueKind.Scalar;
                    break;
                case ReadTargetDirectionNode:
                    valueKind = AIIntentValueKind.Vector2;
                    break;
                case ReadSelectedTargetSnapshotNode:
                    valueKind = AIIntentValueKind.ActionTargetSnapshot;
                    break;
                case ReadAIMemoryNode read:
                    memoryIdentity = RequireMemory(read.BlackboardVariable, memory).Identity;
                    valueKind = memory[memoryIdentity].ValueKind;
                    break;
                case WriteAIMemoryNode write:
                    memoryIdentity = RequireMemory(write.BlackboardVariable, memory).Identity;
                    valueKind = memory[memoryIdentity].ValueKind;
                    ReadPortDefault(write.ValuePort, valueKind, out integer0, out scalar0, out scalar1, out scalar2, out scalar3, out binding);
                    break;
                case ExposedPropertyNode exposed:
                    memoryIdentity = RequireMemory(exposed.BlackboardVariable, memory).Identity;
                    valueKind = memory[memoryIdentity].ValueKind;
                    code = exposed.NodeType == ExposedPropertyNodeType.Get
                        ? SimulationOperationCode.AIReadMemory
                        : SimulationOperationCode.AIWriteMemory;
                    if (exposed.NodeType == ExposedPropertyNodeType.Set)
                        ReadPortDefault(exposed.Value, valueKind, out integer0, out scalar0, out scalar1, out scalar2, out scalar3, out binding);
                    break;
                case WriteContinuousInputNode continuous:
                    binding = continuous.InputId;
                    SimulationInputValueKind inputKind = inputCatalog.RequireValueKind(binding);
                    valueKind = ToAIValueKind(inputKind);
                    ReadPortDefault(continuous.ValuePort, valueKind, out integer0, out scalar0, out scalar1, out scalar2, out scalar3, out _);
                    break;
                case WriteActionTargetSnapshotNode target:
                    binding = target.InputId;
                    if (inputCatalog.RequireValueKind(binding) != SimulationInputValueKind.ActionTargetSnapshot)
                        throw new InvalidOperationException($"AI target node '{node.GUID}' binding is not ActionTargetSnapshot.");
                    valueKind = AIIntentValueKind.ActionTargetSnapshot;
                    break;
                case SubmitActionRequestNode request:
                    unsigned0 = checked((ulong)inputCatalog.RequireRequestTimingClass(request.RequestId));
                    binding = request.RequestId;
                    integer0 = request.Priority;
                    integer1 = request.RepeatPolicy == AIRequestRepeatPolicy.OncePerActivation ? 0 : 1;
                    scalar0 = request.BufferSeconds;
                    break;
                case AIWaitTicksNode:
                    valueKind = AIIntentValueKind.Integer;
                    integer0 = ReadIntegerPort(node, "Ticks");
                    break;
            }
            return new AIIntentSemanticOperation(
                handle,
                code,
                NodeIdentity(node.Owner, node),
                NodePath(node),
                binding,
                memoryIdentity,
                valueKind,
                integer0,
                integer1,
                unsigned0,
                scalar0,
                scalar1,
                scalar2,
                scalar3);
        }

        static void CompileEdges(
            BaseTree graph,
            IReadOnlyDictionary<BaseNode, OperationHandle> handles,
            List<ProgramControlFlowEdge> output)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge.StartNode == null || edge.EndNode == null ||
                    !handles.TryGetValue(edge.StartNode, out OperationHandle source) ||
                    !handles.TryGetValue(edge.EndNode, out OperationHandle target))
                {
                    throw new InvalidOperationException($"AI flow edge '{edge?.GUID}' has unresolved endpoints.");
                }
                OperationHandle condition = OperationHandle.Invalid;
                if (edge.HasConditionRuleGraphConfiguration)
                {
                    if (!edge.TryResolveConditionRuleGraph(out ConditionRuleGraph conditionGraph, out string error) ||
                        conditionGraph.ResultNode == null || !handles.TryGetValue(conditionGraph.ResultNode, out condition))
                    {
                        throw new InvalidOperationException($"AI flow edge '{edge.GUID}' has unresolved ConditionRuleGraph: {error}");
                    }
                }
                output.Add(new ProgramControlFlowEdge(
                    $"{graph.GraphAuthoringId}/flow:{edge.GUID}",
                    source,
                    target,
                    edge.StartPortName,
                    edge.EndPortName,
                    ProgramControlFlowKind.Child,
                    edge.FlowOrder,
                    edge.TransitionPriority,
                    ToAbort(edge.AbortPolicy),
                    condition.IsValid,
                    condition));
            }
            for (int i = 0; i < graph.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = graph.PropertyEdges[i];
                if (edge.StartNode == null || edge.EndNode == null || edge.StartPort == null || edge.EndPort == null ||
                    !handles.TryGetValue(edge.StartNode, out OperationHandle source) ||
                    !handles.TryGetValue(edge.EndNode, out OperationHandle target))
                {
                    throw new InvalidOperationException($"AI value edge '{edge?.GUID}' has unresolved endpoints or ports.");
                }
                if (edge.StartPort.ValueType == null || edge.EndPort.ValueType == null ||
                    edge.StartPort.ValueType != edge.EndPort.ValueType)
                {
                    throw new InvalidOperationException(
                        $"AI value edge '{edge.GUID}' connects incompatible port types '{edge.StartPort.ValueType}' and '{edge.EndPort.ValueType}'.");
                }
                output.Add(new ProgramControlFlowEdge(
                    $"{graph.GraphAuthoringId}/value:{edge.GUID}",
                    source,
                    target,
                    edge.StartPort.DisplayName,
                    edge.EndPort.DisplayName,
                    ProgramControlFlowKind.Value,
                    edge.FlowOrder,
                    0,
                    ProgramAbortPolicy.None,
                    false,
                    OperationHandle.Invalid));
            }
        }

        static SimulationOperationCode Code(BaseNode node)
        {
            return node switch
            {
                RootNode => SimulationOperationCode.Root,
                LoopNode => SimulationOperationCode.Loop,
                ParallelNode => SimulationOperationCode.Parallel,
                SequenceNode => SimulationOperationCode.Sequence,
                SelectorNode => SimulationOperationCode.Selector,
                SucceedNode => SimulationOperationCode.Succeed,
                DebugNode => SimulationOperationCode.Succeed,
                ConditionRuleResultNode => SimulationOperationCode.Constant,
                CompareNode => SimulationOperationCode.Compare,
                AndNode => SimulationOperationCode.And,
                OrNode => SimulationOperationCode.Or,
                NotNode => SimulationOperationCode.Not,
                ReadSelfObservationNode => SimulationOperationCode.AIReadSelfObservation,
                EnumerateConfiguredCandidatesNode => SimulationOperationCode.AIEnumerateConfiguredCandidates,
                SelectNearestCandidateNode => SimulationOperationCode.AISelectNearestCandidate,
                ReadTargetDistanceNode => SimulationOperationCode.AIReadTargetDistance,
                ReadTargetDirectionNode => SimulationOperationCode.AIReadTargetDirection,
                ReadSelectedTargetSnapshotNode => SimulationOperationCode.AIReadSelectedTargetSnapshot,
                ReadAIMemoryNode => SimulationOperationCode.AIReadMemory,
                WriteAIMemoryNode => SimulationOperationCode.AIWriteMemory,
                ExposedPropertyNode => SimulationOperationCode.AIReadMemory,
                WriteContinuousInputNode => SimulationOperationCode.AIWriteContinuousInput,
                WriteActionTargetSnapshotNode => SimulationOperationCode.AIWriteActionTargetSnapshot,
                SubmitActionRequestNode => SimulationOperationCode.AISubmitActionRequest,
                AIWaitTicksNode => SimulationOperationCode.AIWaitTicks,
                _ => throw new InvalidOperationException($"AI node type '{node.GetType().FullName}' has no Semantic operation.")
            };
        }

        static void RequireSupportedNode(BaseNode node)
        {
            _ = Code(node);
        }

        static AIIntentMemoryDeclaration RequireMemory(
            PipelineBlackboardVariableReference reference,
            IReadOnlyDictionary<string, AIIntentMemoryDeclaration> memory)
        {
            if (!reference.IsValid || !memory.TryGetValue(MemoryIdentity(reference.DeclarationOwnerId, reference.DeclarationId), out AIIntentMemoryDeclaration declaration))
                throw new InvalidOperationException($"AI memory reference '{reference.DisplayKey}' is unresolved.");
            return declaration;
        }

        static string MemoryIdentity(string owner, string declaration) =>
            RequireIdentity(owner, nameof(owner)) + "/" + RequireIdentity(declaration, nameof(declaration));

        static string RequireIdentity(string value, string parameter)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException("AI authoring identity cannot be empty.", parameter);
            return normalized;
        }

        static string NodeIdentity(BaseGraph graph, BaseNode node) =>
            $"{graph.GraphAuthoringId}/{node.GUID}";

        static string NodePath(BaseNode node)
        {
            NodePathAttribute attribute = node.GetType().GetCustomAttribute<NodePathAttribute>();
            return attribute == null
                ? $"AI/{node.GetType().Name}/{node.GUID}"
                : attribute.Path + "/" + node.GUID;
        }

        static ProgramAbortPolicy ToAbort(BTAbortPolicy value) => value switch
        {
            BTAbortPolicy.None => ProgramAbortPolicy.None,
            BTAbortPolicy.Self => ProgramAbortPolicy.Self,
            BTAbortPolicy.LowerPriority => ProgramAbortPolicy.LowerPriority,
            BTAbortPolicy.Both => ProgramAbortPolicy.Both,
            _ => throw new InvalidOperationException($"AI edge abort policy '{value}' is invalid.")
        };

        static PropertyPort RequirePort(BaseNode node, string displayName)
        {
            PropertyPort port = node.PropertyPortMap.Values.FirstOrDefault(value =>
                string.Equals(value.DisplayName, displayName, StringComparison.Ordinal));
            return port ?? throw new InvalidOperationException($"AI node '{node.GUID}' has no port '{displayName}'.");
        }

        static bool ReadBooleanPort(BaseNode node, string displayName) =>
            ReadPortValue(RequirePort(node, displayName)) is bool value && value;

        static int ReadIntegerPort(BaseNode node, string displayName) =>
            ReadPortValue(RequirePort(node, displayName)) is int value ? value : 0;

        static double ReadNumericPort(BaseNode node, string displayName)
        {
            PropertyPort port = RequirePort(node, displayName);
            object value = ReadPortValue(port);
            if (value == null && node.IsConnected(port.PortId))
                return 0d;
            return value switch
            {
                int integer => integer,
                float scalar => scalar,
                _ => throw new InvalidOperationException(
                    $"AI node '{node.GUID}' port '{displayName}' has non-numeric value type '{value?.GetType().FullName ?? "null"}'.")
            };
        }

        static object ReadPortValue(PropertyPort port) =>
            port.SourcePort != null ? port.SourcePort.GetValue() : port.GetValue();

        static void ReadPortDefault(
            PropertyPort port,
            AIIntentValueKind kind,
            out int integer0,
            out double scalar0,
            out double scalar1,
            out double scalar2,
            out double scalar3,
            out string text0)
        {
            integer0 = 0;
            scalar0 = 0d;
            scalar1 = 0d;
            scalar2 = 0d;
            scalar3 = 0d;
            text0 = string.Empty;
            if (port == null)
                throw new InvalidOperationException($"AI value kind '{kind}' has no property port.");
            object value = port.GetValue();
            switch (kind)
            {
                case AIIntentValueKind.Boolean when value is bool boolean: integer0 = boolean ? 1 : 0; break;
                case AIIntentValueKind.Integer when value is int integer: integer0 = integer; break;
                case AIIntentValueKind.Scalar when value is float scalar: scalar0 = scalar; break;
                case AIIntentValueKind.Vector2:
                    if (value is not Vector2 vector2)
                        throw InvalidPortValue(kind, value);
                    scalar0 = vector2.x;
                    scalar1 = vector2.y;
                    break;
                case AIIntentValueKind.Vector3:
                    if (value is not Vector3 vector3)
                        throw InvalidPortValue(kind, value);
                    scalar0 = vector3.x;
                    scalar1 = vector3.y;
                    scalar2 = vector3.z;
                    break;
                case AIIntentValueKind.ActorId when value is AIActorIdValue actorId: text0 = actorId.Value; break;
                case AIIntentValueKind.ActionTargetSnapshot when value is AIActionTargetSnapshotValue target:
                    text0 = target.Target.Value;
                    scalar0 = target.Position.x;
                    scalar1 = target.Position.y;
                    scalar2 = target.Position.z;
                    scalar3 = target.Yaw;
                    break;
                default: throw InvalidPortValue(kind, value);
            }
        }

        static InvalidOperationException InvalidPortValue(AIIntentValueKind kind, object value) =>
            new InvalidOperationException(
                $"AI value kind '{kind}' cannot use property value type '{value?.GetType().FullName ?? "null"}'.");

        static void ReadDefault(
            object value,
            out AIIntentValueKind kind,
            out int integer0,
            out double scalar0,
            out double scalar1,
            out double scalar2,
            out double scalar3,
            out string text0)
        {
            if (value is AIActionTargetSnapshotValue actionTarget)
            {
                kind = AIIntentValueKind.ActionTargetSnapshot;
                integer0 = 0;
                scalar0 = actionTarget.Position.x;
                scalar1 = actionTarget.Position.y;
                scalar2 = actionTarget.Position.z;
                scalar3 = actionTarget.Yaw;
                text0 = actionTarget.Target.Value;
                return;
            }
            kind = value switch
            {
                bool => AIIntentValueKind.Boolean,
                int => AIIntentValueKind.Integer,
                float => AIIntentValueKind.Scalar,
                Vector2 => AIIntentValueKind.Vector2,
                Vector3 => AIIntentValueKind.Vector3,
                AIActorIdValue => AIIntentValueKind.ActorId,
                _ => throw new InvalidOperationException($"AI Blackboard value type '{value?.GetType().FullName}' is unsupported.")
            };
            ReadPortDefault(new ConstantPropertyPort(value), kind, out integer0, out scalar0, out scalar1, out scalar2, out scalar3, out text0);
        }

        static AIIntentValueKind ToAIValueKind(SimulationInputValueKind kind) => kind switch
        {
            SimulationInputValueKind.Boolean => AIIntentValueKind.Boolean,
            SimulationInputValueKind.Scalar => AIIntentValueKind.Scalar,
            SimulationInputValueKind.Vector2 => AIIntentValueKind.Vector2,
            SimulationInputValueKind.Vector3 => AIIntentValueKind.Vector3,
            SimulationInputValueKind.Yaw => AIIntentValueKind.Scalar,
            SimulationInputValueKind.ActionTargetSnapshot => AIIntentValueKind.ActionTargetSnapshot,
            _ => throw new InvalidOperationException($"Character input kind '{kind}' is unsupported by AI Intent.")
        };

        sealed class ConstantPropertyPort : PropertyPort
        {
            readonly object m_Value;
            public ConstantPropertyPort(object value) { m_Value = value; }
            public override object GetValue() => m_Value;
        }
    }
}
