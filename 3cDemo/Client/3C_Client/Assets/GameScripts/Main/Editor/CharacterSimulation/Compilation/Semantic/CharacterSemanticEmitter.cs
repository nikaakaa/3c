using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSemanticEmitter
    {
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationNodeEmitterRegistry m_NodeEmitters;
        readonly CharacterSemanticTimelineEmitter m_Timelines;
        readonly CharacterSemanticBlackboardEmitter m_Blackboard;
        readonly CharacterSemanticDomainBindingEmitter m_DomainBindings;
        readonly CharacterSemanticAbilityProgramEmitter m_AbilityPrograms;
        readonly CharacterSemanticGraphFlowEmitter m_Flow;
        readonly BtsmtlSkillGraphCompiler m_NativeSkills;
        readonly Dictionary<string, Dictionary<string, OperationHandle>> m_CompiledGraphOperations = new Dictionary<string, Dictionary<string, OperationHandle>>(StringComparer.Ordinal);
        int m_AbilityCompilationDepth;

        public CharacterSemanticEmitter(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationCatalogIndex catalogIndex)
        {
            model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Blackboard = new CharacterSemanticBlackboardEmitter(model.Declarations, builder, report);
            m_DomainBindings = new CharacterSemanticDomainBindingEmitter(catalogIndex, builder, report, m_Blackboard);
            m_NativeSkills = new BtsmtlSkillGraphCompiler(
                builder,
                m_DomainBindings.Bind,
                model.TimelineEmitters,
                m_Blackboard,
                model.Definition.ControlModuleId,
                AssetProviderOwner(model, model.InputProfile),
                AssetProviderOwner(model, model.GameplayEffectProfile));
            m_AbilityPrograms = new CharacterSemanticAbilityProgramEmitter(model.AbilityRecords, builder, report);
            m_NodeEmitters = model.NodeEmitters;
            m_Timelines = new CharacterSemanticTimelineEmitter(
                model.TimelineEmitters,
                builder,
                report,
                CompileGraph,
                TryGetCompiledOperation);
            m_Flow = new CharacterSemanticGraphFlowEmitter(builder, report, CompileGraph, TryGetCompiledOperation);
        }

        public OperationHandle EmitControlAbilityPrograms(
            CharacterControlModuleContract contract,
            CharacterSimulationSourceLocation controlSource,
            IReadOnlyList<CharacterControlMotionCompilationRecord> motions)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (motions == null)
                throw new ArgumentNullException(nameof(motions));
            m_Blackboard.CompileDeclarations();
            for (int motionIndex = 0; motionIndex < motions.Count; motionIndex++)
            {
                if (motions[motionIndex].Graph == null)
                    continue;
                m_AbilityCompilationDepth++;
                try
                {
                    CompileGraph(motions[motionIndex].Graph, OperationHandle.Invalid);
                }
                finally
                {
                    m_AbilityCompilationDepth--;
                }
            }
            if (!m_AbilityPrograms.Emit(contract, CompileAbilityEntry))
                return OperationHandle.Invalid;
            m_Blackboard.DeclareScopes();
            CharacterSimulationSourceLocation rootSource = new CharacterSimulationSourceLocation(
                typeof(ICharacterControlModule).FullName,
                $"control:{contract.ModuleId.Value}",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"control:{contract.ModuleId.Value}/root",
                contentHash: controlSource.ContentHash);
            return m_Builder.DeclareOperation(rootSource, SimulationOperationCode.Root, Array.Empty<int>());
        }

        OperationHandle CompileAbilityEntry(GameplayAbilityCompilationRecord record)
        {
            return m_NativeSkills.Compile(record.EntryGraph, OperationHandle.Invalid).Entry;
        }

        OperationHandle CompileGraph(CharacterAuthoringGraphOccurrence occurrence, OperationHandle stateScopeOwner)
        {
            if (occurrence == null)
                throw new ArgumentNullException(nameof(occurrence));
            BaseTree graph = occurrence.Graph;
            string route = occurrence.Route;
            try
            {
                m_Blackboard.BeginGraph(graph, route, occurrence.Declarations, stateScopeOwner);
                var operations = new Dictionary<string, OperationHandle>(StringComparer.Ordinal);
                foreach (BaseNode node in occurrence.Nodes)
                {
                    if (m_AbilityCompilationDepth != 0 && node is ActivateActionInstanceNode)
                        continue;
                    if (!m_NodeEmitters.TryGet(node.GetType(), out ICharacterSimulationNodeEmitter emitter))
                        throw new InvalidOperationException($"Discovered Node '{node.GUID}' has no emitter.");
                    var context = new CharacterSimulationNodeEmitterContext(graph, route, m_Builder);
                    OperationHandle operation;
                    try
                    {
                        operation = emitter.Emit(node, context);
                    }
                    catch (Exception exception)
                    {
                        m_Report.EmissionError("node_emit_failed", $"{route}/node:{node.GUID}", exception.Message);
                        continue;
                    }
                    operations.Add(node.GUID, operation);
                    m_DomainBindings.Bind(node, operation, route, context.Source(node));
                }
                m_CompiledGraphOperations[route] = operations;

                foreach (CharacterAuthoringEdgeRecord edge in occurrence.Edges)
                    m_Flow.EmitEdge(occurrence, edge, operations, stateScopeOwner, m_AbilityCompilationDepth != 0);
                foreach (CharacterAuthoringEdgeRecord edge in occurrence.PropertyEdges)
                    m_Flow.EmitEdge(occurrence, edge, operations, stateScopeOwner, m_AbilityCompilationDepth != 0);

                foreach (CharacterAuthoringTimelineRecord timeline in occurrence.Timelines)
                {
                    if (!operations.TryGetValue(timeline.Node.GUID, out OperationHandle owner))
                        throw new InvalidOperationException($"Discovered Timeline Node '{timeline.Node.GUID}' has no emitted operation.");
                    m_Timelines.Emit(timeline, owner, stateScopeOwner);
                }
                foreach (CharacterAuthoringGraphReferenceRecord reference in occurrence.GraphReferences)
                {
                    if (!operations.TryGetValue(reference.Owner.GUID, out OperationHandle owner))
                        throw new InvalidOperationException($"Discovered graph owner Node '{reference.Owner.GUID}' has no emitted operation.");
                    OperationHandle childStateOwner = reference.Owner is StateNode && reference.Child.Graph is StateBehaviorSubTree
                        ? owner
                        : stateScopeOwner;
                    OperationHandle entry = CompileGraph(reference.Child, childStateOwner);
                    if (!entry.IsValid)
                        continue;
                    if (reference.Owner is SubTreeNode)
                        EmitGraphCallFrame(reference, owner, entry);
                    if (reference.Owner is StateNode && reference.Child.Graph is StateBehaviorSubTree stateBehavior)
                    {
                        m_Flow.EmitStateBehavior(graph, reference.Owner, owner, stateBehavior, reference.Route, entry);
                        continue;
                    }
                    m_Builder.DeclareControlFlow(
                        $"{reference.Route}/entry",
                        owner,
                        entry,
                        reference.Reference.Key,
                        "Entry",
                        ProgramControlFlowKind.Enter,
                        0,
                        0,
                        ProgramAbortPolicy.None,
                        false,
                        OperationHandle.Invalid,
                        CharacterSemanticSourceFactory.Node(graph, reference.Owner, route));
                    if (reference.Owner is StateMachineNode)
                    {
                        foreach (BaseNode stateNode in reference.Child.Nodes)
                        {
                            if (stateNode is not StateNode)
                                continue;
                            if (!TryGetCompiledOperation(reference.Route, stateNode.GUID, out OperationHandle stateOperation))
                                throw new InvalidOperationException($"State '{stateNode.GUID}' has no compiled operation.");
                            m_Builder.DeclareReference(
                                $"{reference.Route}/node:{stateNode.GUID}/state-machine-owner",
                                stateOperation,
                                ProgramReferenceKind.Operation,
                                owner.Value,
                                reference.Route,
                                CharacterSemanticSourceFactory.Node(reference.Child.Graph, stateNode, reference.Route));
                        }
                    }
                    if (reference.Owner is StateMachineNode && reference.Child.Graph is StateMachineGraph stateMachine &&
                        stateMachine.AnyStateNode != null &&
                        TryGetCompiledOperation(reference.Route, stateMachine.AnyStateNode.GUID, out OperationHandle anyState))
                    {
                        m_Builder.DeclareControlFlow(
                            $"{reference.Route}/any-state",
                            owner,
                            anyState,
                            "AnyState",
                            "Entry",
                            ProgramControlFlowKind.Enter,
                            1,
                            0,
                            ProgramAbortPolicy.None,
                            false,
                            OperationHandle.Invalid,
                            CharacterSemanticSourceFactory.Node(graph, reference.Owner, route));
                    }
                }
                OperationHandle graphEntry = FindEntry(occurrence, operations);
                m_Blackboard.CompleteGraph(route, graphEntry);
                return graphEntry;
            }
            finally
            {
                m_Blackboard.EndGraph();
            }
        }

        void EmitGraphCallFrame(
            CharacterAuthoringGraphReferenceRecord reference,
            OperationHandle owner,
            OperationHandle entry)
        {
            var inputs = new List<ProgramGraphParameterBinding>();
            var outputs = new List<ProgramGraphParameterBinding>();
            BindGraphParameters(reference, reference.CallFrame.Inputs, inputs);
            BindGraphParameters(reference, reference.CallFrame.Outputs, outputs);
            EmitGraphCallInputDefaults(reference, owner);
            m_Builder.DeclareGraphCallFrame(
                reference.CallFrame.Identity,
                owner,
                entry,
                reference.Child.Graph.GraphAuthoringId,
                inputs,
                outputs,
                CharacterSemanticSourceFactory.Node(reference.Owner.Owner as BaseTree, reference.Owner, reference.Route));
        }

        void EmitGraphCallInputDefaults(
            CharacterAuthoringGraphReferenceRecord reference,
            OperationHandle owner)
        {
            if (reference.Owner is not SubTreeNode subTreeNode)
                return;
            BaseGraph graph = reference.Owner.Owner;
            for (int i = 0; i < reference.CallFrame.Inputs.Count; i++)
            {
                CharacterAuthoringGraphParameterBinding binding = reference.CallFrame.Inputs[i];
                PropertyPort port = null;
                for (int portIndex = 0; portIndex < subTreeNode.InputPropertyPorts.Count; portIndex++)
                {
                    PropertyPort candidate = subTreeNode.InputPropertyPorts[portIndex];
                    if (candidate != null && string.Equals(candidate.PortId, binding.PortId, StringComparison.Ordinal))
                    {
                        port = candidate;
                        break;
                    }
                }
                if (port == null || IsGraphCallInputLinked(graph, subTreeNode, binding.PortId))
                    continue;
                if (!TryMapValueKind(binding.ValueType, out SemanticValueKind kind))
                {
                    m_Report.Error(
                        "graph_call_parameter_default_type_unsupported",
                        reference.Route,
                        $"Graph call parameter '{binding.ParameterName}' default uses unsupported type '{binding.ValueType?.FullName}'.");
                    continue;
                }
                CharacterSimulationSourceLocation source = CharacterSemanticSourceFactory.Node(
                    graph as BaseTree,
                    subTreeNode,
                    reference.Route + "/input:" + binding.PortId);
                int constant = m_Builder.DeclareConstant(source, "default-value", port.GetValue());
                if (constant >= 0)
                    m_Builder.DeclareConstantInputBinding(owner, binding.PortId, constant, kind, source);
            }
        }

        static bool IsGraphCallInputLinked(BaseGraph graph, SubTreeNode node, string portId)
        {
            if (graph == null)
                return false;
            for (int i = 0; i < graph.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = graph.PropertyEdges[i];
                if (edge != null &&
                    string.Equals(edge.EndNodeGUID, node.GUID, StringComparison.Ordinal) &&
                    string.Equals(edge.EndPortName, portId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        void BindGraphParameters(
            CharacterAuthoringGraphReferenceRecord reference,
            IReadOnlyList<CharacterAuthoringGraphParameterBinding> source,
            List<ProgramGraphParameterBinding> destination)
        {
            for (int i = 0; i < source.Count; i++)
            {
                CharacterAuthoringGraphParameterBinding binding = source[i];
                if (!m_Blackboard.TryGetValueSlot(
                        reference.Child.Graph,
                        reference.Child.Route,
                        binding.DeclarationId,
                        out int stateSlot))
                {
                    m_Report.Error(
                        "graph_call_parameter_state_missing",
                        reference.Route,
                        $"Graph call parameter '{binding.ParameterName}' has no compiled child state address.");
                    continue;
                }
                if (!TryMapValueKind(binding.ValueType, out SemanticValueKind valueKind))
                {
                    m_Report.Error(
                        "graph_call_parameter_type_unsupported",
                        reference.Route,
                        $"Graph call parameter '{binding.ParameterName}' uses unsupported type '{binding.ValueType?.FullName}'.");
                    continue;
                }
                destination.Add(new ProgramGraphParameterBinding(
                    binding.Direction == CharacterAuthoringGraphParameterDirection.Input
                        ? ProgramGraphParameterDirection.Input
                        : ProgramGraphParameterDirection.Output,
                    binding.ParameterName,
                    $"blackboard:{reference.Child.Graph.GraphAuthoringId}:{binding.DeclarationId}",
                    stateSlot,
                    binding.PortId,
                    valueKind));
            }
        }

        static bool TryMapValueKind(Type type, out SemanticValueKind kind)
        {
            if (type == typeof(bool)) kind = SemanticValueKind.Boolean;
            else if (type == typeof(int)) kind = SemanticValueKind.Int32;
            else if (type == typeof(uint) || type == typeof(ulong)) kind = SemanticValueKind.UInt64;
            else if (type == typeof(float) || type == typeof(double)) kind = SemanticValueKind.Number;
            else if (type == typeof(UnityEngine.Vector2)) kind = SemanticValueKind.Vector2;
            else if (type == typeof(UnityEngine.Vector3)) kind = SemanticValueKind.Vector3;
            else if (type == typeof(string)) kind = SemanticValueKind.Identity;
            else
            {
                kind = default;
                return false;
            }
            return true;
        }

        bool TryGetCompiledOperation(string route, string nodeId, out OperationHandle operation)
        {
            operation = OperationHandle.Invalid;
            return !string.IsNullOrEmpty(nodeId) &&
                   m_CompiledGraphOperations.TryGetValue(route, out Dictionary<string, OperationHandle> operations) &&
                   operations.TryGetValue(nodeId, out operation);
        }

        OperationHandle FindEntry(CharacterAuthoringGraphOccurrence occurrence, Dictionary<string, OperationHandle> operations)
        {
            if (!string.IsNullOrEmpty(occurrence.EntryNodeId) && operations.TryGetValue(occurrence.EntryNodeId, out OperationHandle operation))
                return operation;
            m_Report.EmissionError("graph_entry_missing", occurrence.Route, $"Discovered entry Node '{occurrence.EntryNodeId}' was not emitted.");
            return OperationHandle.Invalid;
        }

        static string AssetProviderOwner(CharacterAuthoringCompilationModel model, UnityEngine.Object asset)
        {
            return asset
                ? CharacterSkillProviderOwners.Asset(model.GetAssetGuid(asset))
                : string.Empty;
        }
    }
}
