using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public interface IAgentMutationHandler
    {
        bool Preflight(AgentMutationSession session, AgentMutation command);
        void Apply(AgentMutationSession session, AgentMutation command);
    }

    public sealed class AgentMutationHandlerCatalog
    {
        readonly Dictionary<AgentMutationKind, IAgentMutationHandler> m_Handlers =
            new Dictionary<AgentMutationKind, IAgentMutationHandler>();

        public AgentMutationHandlerCatalog()
        {
            var emitters = new BtsmtlGraphAuthoringCapabilities();
            var conditionBuilder = new AgentConditionRuleBuilder();
            Register(new AgentStateMachineMutationHandler(emitters, conditionBuilder),
                AgentMutationKind.EnsureStateMachine,
                AgentMutationKind.EnsureState,
                AgentMutationKind.DeleteState,
                AgentMutationKind.EnsureTransition,
                AgentMutationKind.RewireTransition,
                AgentMutationKind.EnsureConditionRule);
            Register(new AgentStateBehaviorMutationHandler(emitters, conditionBuilder),
                AgentMutationKind.EnsureActionExitLifecycle,
                AgentMutationKind.DeleteStateBehaviorNode,
                AgentMutationKind.EnsureStateBehaviorNode,
                AgentMutationKind.EnsureTimelineNode,
                AgentMutationKind.EnsureActionActivation,
                AgentMutationKind.EnsureActionLifecycleTransition);
            Register(new AgentNodeAssetMutationHandler(emitters),
                AgentMutationKind.EnsureInputNode,
                AgentMutationKind.EnsureConditionValueNode,
                AgentMutationKind.ConfigureActionAdmission);
            Register(new AgentActionEligibilityMutationHandler(),
                AgentMutationKind.EnsureBlackboardDeclaration,
                AgentMutationKind.MoveBlackboardDeclaration,
                AgentMutationKind.DeleteBlackboardDeclaration,
                AgentMutationKind.SetBlackboardSchemaRevision,
                AgentMutationKind.EnsureExposedPropertyNode,
                AgentMutationKind.EnsureTimelineTreeClip,
                AgentMutationKind.EnsureInlineTimeline,
                AgentMutationKind.EnsureMotionCurveTrack,
                AgentMutationKind.EnsureMotionCurveClip,
                AgentMutationKind.ConfigureMotionCurveClip,
                AgentMutationKind.EnsureMotionWarpTrack,
                AgentMutationKind.DeleteTimelineTrack,
                AgentMutationKind.EnsureMotionWarpClip,
                AgentMutationKind.ConfigureMotionWarpSource,
                AgentMutationKind.ConfigureMotionWarpParameters,
                AgentMutationKind.MoveTimelineClip,
                AgentMutationKind.ConfigureTimelineClipEase,
                AgentMutationKind.ConfigureTimelineCurveChannel,
                AgentMutationKind.ConfigureAnimationTrackChannel,
                AgentMutationKind.ConfigureAnimationTrackMarkerSync,
                AgentMutationKind.EnsureAnimationSyncMarker,
                AgentMutationKind.MoveAnimationSyncMarker,
                AgentMutationKind.DeleteAnimationSyncMarker,
                AgentMutationKind.DeleteTimelineClip,
                AgentMutationKind.EnsureTreeClipBlackboardWrite,
                AgentMutationKind.DeleteTransition,
                AgentMutationKind.EnsureGameplayTag,
                AgentMutationKind.SetActionProfileGrantedTags,
                AgentMutationKind.SetActionProfileCancelQuery,
                AgentMutationKind.SetActionProfileTargetRequirement,
                AgentMutationKind.SetActionRequestTimingClass);
            Register(new AgentAIControllerMutationHandler(),
                AgentMutationKind.EnsureAIControllerDefinition,
                AgentMutationKind.EnsureAIControllerTree,
                AgentMutationKind.BindAIControllerAssets,
                AgentMutationKind.ConfigureAICandidates,
                AgentMutationKind.EnsureAIBlackboardDeclaration,
                AgentMutationKind.EnsureAISharedNode,
                AgentMutationKind.EnsureAIObservationNode,
                AgentMutationKind.EnsureAIMemoryNode,
                AgentMutationKind.EnsureAIContinuousInput,
                AgentMutationKind.EnsureAIActionTarget,
                AgentMutationKind.EnsureAIActionRequest);
            Register(new AgentBTConditionRuleMutationHandler(conditionBuilder), AgentMutationKind.EnsureBTConditionRule);
            Register(new AgentGraphNodeMutationHandler(emitters), AgentMutationKind.EnsureGraphNode, AgentMutationKind.DeleteGraphNode);
            Register(new AgentGraphLinkMutationHandler(), AgentMutationKind.DeleteFlowEdge, AgentMutationKind.DeletePropertyEdge, AgentMutationKind.LinkFlow, AgentMutationKind.LinkProperty);
        }

        public IAgentMutationHandler Get(AgentMutationKind kind)
        {
            if (m_Handlers.TryGetValue(kind, out IAgentMutationHandler handler))
                return handler;
            throw new InvalidOperationException($"Agent Mutation handler is not registered: {kind}");
        }

        void Register(IAgentMutationHandler handler, params AgentMutationKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
            {
                if (m_Handlers.ContainsKey(kinds[i]))
                    throw new InvalidOperationException($"Duplicate Agent Mutation handler: {kinds[i]}");
                m_Handlers.Add(kinds[i], handler);
            }
        }
    }

    public sealed class AgentAIControllerMutationHandler : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (!session.AIDefinition)
            {
                session.Report.Error(command.Path, "ai_definition_missing", "AIController domain 缺少 AIControllerDefinition。");
                return false;
            }

            bool valid = command switch
            {
                AgentEnsureAIControllerDefinitionMutation value => PreflightDefinition(session, value),
                AgentEnsureAIControllerTreeMutation value => PreflightTree(session, value),
                AgentBindAIControllerAssetsMutation value => PreflightBindings(session, value),
                AgentConfigureAICandidatesMutation value => PreflightCandidates(session, value),
                AgentEnsureAIBlackboardDeclarationMutation value => PreflightDeclaration(session, value),
                AgentEnsureAISharedNodeMutation value => PreflightNode(session, value.Graph, value.ExistingNode, SharedType(value.NodeKind), value),
                AgentEnsureAIObservationNodeMutation value => PreflightNode(session, value.Graph, value.ExistingNode, ObservationType(value.NodeKind), value),
                AgentEnsureAIMemoryNodeMutation value => PreflightMemory(session, value),
                AgentEnsureAIContinuousInputMutation value => PreflightContinuousInput(session, value),
                AgentEnsureAIActionTargetMutation value => PreflightActionTarget(session, value),
                AgentEnsureAIActionRequestMutation value => PreflightActionRequest(session, value),
                _ => throw new InvalidOperationException($"Unsupported AI Controller command: {command.Kind}")
            };
            return valid;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureAIControllerDefinitionMutation value:
                    session.AIDefinition.ConfigureAuthoring(value.ControllerId, session.AIDefinition.RootTreeAsset, session.AIDefinition.ControlledCharacter, session.AIDefinition.PerceptionProfile);
                    session.AddAppliedAuthoring(value, session.AIDefinition, session.AIDefinition, value.ControllerId, "AI Controller Definition");
                    break;
                case AgentEnsureAIControllerTreeMutation value:
                    session.AddAppliedAuthoring(value, session.AIDefinition.RootTreeAsset, session.RootTree, session.RootTree.GraphAuthoringId, value.TreeAssetPath);
                    break;
                case AgentBindAIControllerAssetsMutation value:
                    ApplyBindings(session, value);
                    break;
                case AgentConfigureAICandidatesMutation value:
                    session.AIDefinition.PerceptionProfile.ConfigureAuthoring(value.CandidateActorIds, value.Ordering);
                    session.AddAppliedAuthoring(value, session.AIDefinition.PerceptionProfile, session.AIDefinition.PerceptionProfile, session.AIDefinition.PerceptionProfile.name, "Configured candidates");
                    break;
                case AgentEnsureAIBlackboardDeclarationMutation value:
                    ApplyDeclaration(session, value);
                    break;
                case AgentEnsureAISharedNodeMutation value:
                    ApplySharedNode(session, value);
                    break;
                case AgentEnsureAIObservationNodeMutation value:
                    ApplyObservation(session, value);
                    break;
                case AgentEnsureAIMemoryNodeMutation value:
                    ApplyMemory(session, value);
                    break;
                case AgentEnsureAIContinuousInputMutation value:
                    ApplyContinuousInput(session, value);
                    break;
                case AgentEnsureAIActionTargetMutation value:
                    ApplyActionTarget(session, value);
                    break;
                case AgentEnsureAIActionRequestMutation value:
                    ApplyActionRequest(session, value);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported AI Controller command: {command.Kind}");
            }
        }

        static bool PreflightDefinition(AgentMutationSession session, AgentEnsureAIControllerDefinitionMutation command)
        {
            if (!session.AIDefinition.RootTreeAsset || !session.AIDefinition.ControlledCharacter || !session.AIDefinition.PerceptionProfile)
            {
                session.Report.Error(command.Path, "ai_definition_incomplete", "ensure_ai_controller_definition 要求 RootTree、ControlledCharacter 与 PerceptionProfile 已正式绑定。");
                return false;
            }
            session.AddPlanned(command, null, command.ControllerId, "AI Controller Definition");
            return true;
        }

        static bool PreflightTree(AgentMutationSession session, AgentEnsureAIControllerTreeMutation command)
        {
            string path = AssetDatabase.GetAssetPath(session.AIDefinition.RootTreeAsset);
            if (!IsExactAssetPath(command.TreeAssetPath) || !string.Equals(path, command.TreeAssetPath, StringComparison.Ordinal))
            {
                session.Report.Error(command.Path, "ai_root_tree_path_mismatch", $"RootTree 资产路径与当前 Definition 不一致：{command.TreeAssetPath}");
                return false;
            }
            session.AddPlanned(command, session.RootTree, session.RootTree.GraphAuthoringId, path);
            return true;
        }

        static bool PreflightBindings(AgentMutationSession session, AgentBindAIControllerAssetsMutation command)
        {
            bool characterResolved = ResolveAsset(command.ControlledCharacter, out CharacterPipelineDefinition character);
            bool perceptionResolved = ResolveAsset(command.PerceptionProfile, out AIPerceptionProfile perception);
            bool valid = characterResolved && perceptionResolved;
            if (!valid)
            {
                session.Report.Error(command.Path, "ai_binding_asset_unresolved", "Controlled Character 或 Perception Profile 资产引用无法精确解析。");
                return false;
            }
            if (!character.SimulationProgram)
            {
                session.Report.Error(command.Path, "controlled_character_program_missing", "Controlled Character 缺少正式 Simulation Program。");
                return false;
            }
            session.AddPlanned(command, null, character.name, perception.name);
            return true;
        }

        static bool PreflightCandidates(AgentMutationSession session, AgentConfigureAICandidatesMutation command)
        {
            if (!session.AIDefinition.PerceptionProfile)
            {
                session.Report.Error(command.Path, "perception_profile_missing", "AIControllerDefinition 缺少 Perception Profile。");
                return false;
            }
            session.AddPlanned(command, null, session.AIDefinition.PerceptionProfile.name, $"{command.Ordering} / {command.CandidateActorIds.Count}");
            return true;
        }

        static bool PreflightDeclaration(AgentMutationSession session, AgentEnsureAIBlackboardDeclarationMutation command)
        {
            if (!TryResolveAIGraph(session, command.Graph, command.Path, out BaseTree graph))
                return false;
            if (command.ExistingDeclaration.IsValid && !session.TryResolveDeclaration(graph, command.ExistingDeclaration.Value, command.Path, out _))
                return false;
            if (graph != null && graph.ExposedProperties.Any(value => value != null &&
                    string.Equals(value.BlackboardKey, command.Key, StringComparison.Ordinal) &&
                    (!command.ExistingDeclaration.IsValid || !string.Equals(value.DeclarationId, command.ExistingDeclaration.Identity, StringComparison.Ordinal))))
            {
                session.Report.Error(command.Path, "ai_blackboard_key_duplicate", $"AI Blackboard key 已存在：{command.Key}");
                return false;
            }
            session.PlanBlackboardDeclaration(command.Id, command.Graph.Identity, command.ValueType, command.Key);
            session.AddPlanned(command, graph, command.Key, $"{command.Scope}/{command.ValueType.Name}");
            return true;
        }

        static bool PreflightMemory(AgentMutationSession session, AgentEnsureAIMemoryNodeMutation command)
        {
            if (!PreflightNode(session, command.Graph, command.ExistingNode, command.NodeKind == AgentAIMemoryNodeKind.Read ? typeof(ReadAIMemoryNode) : typeof(WriteAIMemoryNode), command, false, out BaseTree graph))
                return false;
            BaseExposedProperty declaration = null;
            if (graph != null && !session.TryResolveDeclaration(graph, command.Declaration, command.Path, out declaration))
                return false;
            if (graph != null && declaration != null && declaration.ValueType != MemoryValueType(command.ValueKind))
            {
                session.Report.Error(command.Path, "ai_memory_type_mismatch", $"Memory declaration 类型与 {command.ValueKind} 不一致。");
                return false;
            }
            session.AddPlanned(command, graph, command.ExistingNode.Identity, command.ValueKind.ToString());
            return true;
        }

        static bool PreflightContinuousInput(AgentMutationSession session, AgentEnsureAIContinuousInputMutation command)
        {
            if (!TryGetInput(session, command.InputId, out string valueType) || string.Equals(valueType, "ActionTargetSnapshot", StringComparison.OrdinalIgnoreCase))
            {
                session.Report.Error(command.Path, "ai_input_binding_invalid", $"Continuous InputId 不在受控 Character catalog 中或类型不允许：{command.InputId}");
                return false;
            }
            return PreflightNode(session, command.Graph, command.ExistingNode, typeof(WriteContinuousInputNode), command);
        }

        static bool PreflightActionTarget(AgentMutationSession session, AgentEnsureAIActionTargetMutation command)
        {
            if (!TryGetInput(session, command.InputId, out string valueType) || !string.Equals(valueType, "ActionTargetSnapshot", StringComparison.OrdinalIgnoreCase))
            {
                session.Report.Error(command.Path, "ai_action_target_binding_invalid", $"Action Target InputId 不是 ActionTargetSnapshot：{command.InputId}");
                return false;
            }
            return PreflightNode(session, command.Graph, command.ExistingNode, typeof(WriteActionTargetSnapshotNode), command);
        }

        static bool PreflightActionRequest(AgentMutationSession session, AgentEnsureAIActionRequestMutation command)
        {
            if (session.Snapshot.aiController?.actionRequests == null || !session.Snapshot.aiController.actionRequests.Any(value => string.Equals(value.requestId, command.RequestId, StringComparison.Ordinal)))
            {
                session.Report.Error(command.Path, "ai_action_request_binding_invalid", $"RequestId 不在受控 Character catalog 中：{command.RequestId}");
                return false;
            }
            return PreflightNode(session, command.Graph, command.ExistingNode, typeof(SubmitActionRequestNode), command);
        }

        static bool PreflightNode(AgentMutationSession session, AgentGraphTargetReference graphReference, AgentElementTargetReference existingReference, Type expectedType, AgentMutation command)
        {
            return PreflightNode(session, graphReference, existingReference, expectedType, command, true, out _);
        }

        static bool PreflightNode(AgentMutationSession session, AgentGraphTargetReference graphReference, AgentElementTargetReference existingReference, Type expectedType, AgentMutation command, bool addPlan, out BaseTree graph)
        {
            if (!TryResolveAIGraph(session, graphReference, command.Path, out graph))
                return false;
            if (graph != null && !graph.CanCreateNodeType(expectedType))
            {
                session.Report.Error(command.Path, "ai_node_capability_forbidden", $"AI Graph policy 禁止节点：{expectedType.Name}");
                return false;
            }
            if (existingReference.IsValid && graph != null)
            {
                if (!session.TryResolveNode(graph, existingReference, command.Path, out BaseNode existing))
                    return false;
                if (existing != null && existing.GetType() != expectedType)
                {
                    session.Report.Error(command.Path, "ai_node_type_mismatch", $"现有节点类型不是 {expectedType.Name}。");
                    return false;
                }
            }
            if (addPlan)
                session.AddPlanned(command, graph, existingReference.Identity, expectedType.Name);
            return true;
        }

        static bool TryResolveAIGraph(AgentMutationSession session, AgentGraphTargetReference reference, string path, out BaseTree graph)
        {
            if (!session.TryResolveGraph(reference, path, out graph))
                return false;
            if (graph != null && graph.AuthoringRole != GraphAuthoringRole.AIController)
            {
                session.Report.Error(path, "ai_graph_role_invalid", $"目标 Graph role 不是 AIController：{graph.AuthoringRole}");
                return false;
            }
            return true;
        }

        static void ApplyBindings(AgentMutationSession session, AgentBindAIControllerAssetsMutation command)
        {
            if (!ResolveAsset(command.ControlledCharacter, out CharacterPipelineDefinition character) ||
                !ResolveAsset(command.PerceptionProfile, out AIPerceptionProfile perception))
                throw new InvalidOperationException("AI Controller binding assets changed after preflight.");
            session.AIDefinition.ConfigureAuthoring(session.AIDefinition.ControllerId, session.AIDefinition.RootTreeAsset, character, perception);
            session.AddAppliedAuthoring(command, session.AIDefinition, session.AIDefinition, character.name, perception.name);
        }

        static void ApplyDeclaration(AgentMutationSession session, AgentEnsureAIBlackboardDeclarationMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph))
                return;
            BaseExposedProperty declaration = null;
            if (command.ExistingDeclaration.IsValid)
                session.TryResolveDeclaration(graph, command.ExistingDeclaration.Value, command.Path, out declaration);
            declaration ??= graph.CreateExposedProperty(ExposedPropertyType(command.ValueType));
            declaration.Name = command.Key;
            declaration.ConfigureDeclaration(
                command.Key,
                command.Scope,
                PipelineBlackboardVariablePolicy.DefaultLifetime(command.Scope),
                "AI");
            declaration.ClearInputBinding();
            declaration.ClearFactProjection();
            declaration.SetValue(command.DefaultValue ?? DefaultValue(command.ValueType));
            session.AddAppliedAuthoring(command, graph.SerializedOwner, declaration, declaration.DeclarationId, command.Key);
            session.RefreshIndex(command.Path);
        }

        static void ApplyObservation(AgentMutationSession session, AgentEnsureAIObservationNodeMutation command)
        {
            BaseNode node = ResolveOrCreateNode(session, command.Graph, command.ExistingNode, ObservationType(command.NodeKind), command.Position, command.Path, out BaseTree graph);
            if (node != null)
                session.AddApplied(command, graph, node, command.NodeKind.ToString());
        }

        static void ApplySharedNode(AgentMutationSession session, AgentEnsureAISharedNodeMutation command)
        {
            BaseNode node = ResolveOrCreateNode(session, command.Graph, command.ExistingNode, SharedType(command.NodeKind), command.Position, command.Path, out BaseTree graph);
            if (node == null)
                return;
            if (node is LoopNode loop)
                loop.ConfigureAuthoring(command.LoopStopType);
            else if (node is CompareNode compare)
                compare.ConfigureAuthoring(command.CompareType);
            session.AddApplied(command, graph, node, command.NodeKind.ToString());
        }

        static void ApplyMemory(AgentMutationSession session, AgentEnsureAIMemoryNodeMutation command)
        {
            Type type = command.NodeKind == AgentAIMemoryNodeKind.Read ? typeof(ReadAIMemoryNode) : typeof(WriteAIMemoryNode);
            BaseNode node = ResolveOrCreateNode(session, command.Graph, command.ExistingNode, type, command.Position, command.Path, out BaseTree graph);
            if (node == null || !session.TryResolveDeclaration(graph, command.Declaration, command.Path, out BaseExposedProperty declaration))
                return;
            if (node is ReadAIMemoryNode read)
                read.ConfigureAuthoring(declaration, command.ValueKind);
            else
                ((WriteAIMemoryNode)node).ConfigureAuthoring(declaration, command.ValueKind);
            node.RebindReadOnlyViewReferences(graph);
            session.AddApplied(command, graph, node, command.ValueKind.ToString());
        }

        static void ApplyContinuousInput(AgentMutationSession session, AgentEnsureAIContinuousInputMutation command)
        {
            BaseNode node = ResolveOrCreateNode(session, command.Graph, command.ExistingNode, typeof(WriteContinuousInputNode), command.Position, command.Path, out BaseTree graph);
            if (node == null || !TryGetInput(session, command.InputId, out string valueType))
                return;
            ((WriteContinuousInputNode)node).ConfigureInput(command.InputId, InputPortType(valueType));
            node.RebindReadOnlyViewReferences(graph);
            session.AddApplied(command, graph, node, command.InputId);
        }

        static void ApplyActionTarget(AgentMutationSession session, AgentEnsureAIActionTargetMutation command)
        {
            BaseNode node = ResolveOrCreateNode(session, command.Graph, command.ExistingNode, typeof(WriteActionTargetSnapshotNode), command.Position, command.Path, out BaseTree graph);
            if (node == null)
                return;
            ((WriteActionTargetSnapshotNode)node).ConfigureInput(command.InputId);
            session.AddApplied(command, graph, node, command.InputId);
        }

        static void ApplyActionRequest(AgentMutationSession session, AgentEnsureAIActionRequestMutation command)
        {
            BaseNode node = ResolveOrCreateNode(session, command.Graph, command.ExistingNode, typeof(SubmitActionRequestNode), command.Position, command.Path, out BaseTree graph);
            if (node == null)
                return;
            ((SubmitActionRequestNode)node).ConfigureRequest(command.RequestId, command.BufferSeconds, command.Priority, command.RepeatPolicy);
            session.AddApplied(command, graph, node, command.RequestId);
        }

        static BaseNode ResolveOrCreateNode(AgentMutationSession session, AgentGraphTargetReference graphReference, AgentElementTargetReference existingReference, Type type, Vector2 position, string path, out BaseTree graph)
        {
            graph = null;
            if (!session.TryResolveGraph(graphReference, path, out graph))
                return null;
            BaseNode node = null;
            if (existingReference.IsValid)
                session.TryResolveNode(graph, existingReference, path, out node);
            if (node == null)
            {
                node = graph.CreateNode(type);
                node.Position = position;
            }
            return node;
        }

        static bool TryGetInput(AgentMutationSession session, string inputId, out string valueType)
        {
            valueType = string.Empty;
            AgentSnapshotInputValue entry = session.Snapshot.aiController?.inputValues?.FirstOrDefault(value => string.Equals(value.inputValueId, inputId, StringComparison.Ordinal));
            if (entry == null)
                return false;
            valueType = entry.valueType;
            return true;
        }

        static Type ObservationType(AgentAIObservationNodeKind kind)
        {
            return kind switch
            {
                AgentAIObservationNodeKind.ReadSelf => typeof(ReadSelfObservationNode),
                AgentAIObservationNodeKind.EnumerateConfiguredCandidates => typeof(EnumerateConfiguredCandidatesNode),
                AgentAIObservationNodeKind.SelectNearestCandidate => typeof(SelectNearestCandidateNode),
                AgentAIObservationNodeKind.ReadTargetDistance => typeof(ReadTargetDistanceNode),
                AgentAIObservationNodeKind.ReadTargetDirection => typeof(ReadTargetDirectionNode),
                AgentAIObservationNodeKind.ReadSelectedTargetSnapshot => typeof(ReadSelectedTargetSnapshotNode),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        static Type SharedType(AgentAISharedNodeKind kind)
        {
            return kind switch
            {
                AgentAISharedNodeKind.Loop => typeof(LoopNode),
                AgentAISharedNodeKind.Sequence => typeof(SequenceNode),
                AgentAISharedNodeKind.Selector => typeof(SelectorNode),
                AgentAISharedNodeKind.Compare => typeof(CompareNode),
                AgentAISharedNodeKind.WaitTicks => typeof(AIWaitTicksNode),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        static Type MemoryValueType(AIMemoryValueKind kind)
        {
            return kind switch
            {
                AIMemoryValueKind.Boolean => typeof(bool),
                AIMemoryValueKind.Integer => typeof(int),
                AIMemoryValueKind.Scalar => typeof(float),
                AIMemoryValueKind.Vector2 => typeof(Vector2),
                AIMemoryValueKind.Vector3 => typeof(Vector3),
                AIMemoryValueKind.ActorId => typeof(AIActorIdValue),
                AIMemoryValueKind.ActionTargetSnapshot => typeof(AIActionTargetSnapshotValue),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        static Type ExposedPropertyType(Type valueType)
        {
            if (valueType == typeof(bool)) return typeof(BoolExposedProperty);
            if (valueType == typeof(int)) return typeof(IntExposedProperty);
            if (valueType == typeof(float)) return typeof(FloatExposedProperty);
            if (valueType == typeof(Vector2)) return typeof(Vector2ExposedProperty);
            if (valueType == typeof(Vector3)) return typeof(Vector3ExposedProperty);
            if (valueType == typeof(AIActorIdValue)) return typeof(AIActorIdExposedProperty);
            if (valueType == typeof(AIActionTargetSnapshotValue)) return typeof(AIActionTargetSnapshotExposedProperty);
            throw new InvalidOperationException($"Unsupported AI Blackboard value type: {valueType?.FullName}");
        }

        static Type InputPortType(string valueType)
        {
            return valueType switch
            {
                "Bool" or "Boolean" => typeof(BoolPropertyPort),
                "Float" or "Scalar" or "Yaw" => typeof(FloatPropertyPort),
                "Vector2" => typeof(Vector2PropertyPort),
                "Vector3" => typeof(Vector3PropertyPort),
                _ => throw new InvalidOperationException($"Unsupported AI continuous input type: {valueType}")
            };
        }

        static object DefaultValue(Type valueType)
        {
            return valueType != null && valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
        }

        static bool ResolveAsset<T>(AgentAssetReference reference, out T asset) where T : UnityEngine.Object
        {
            asset = null;
            string path = reference.AssetPath;
            if (!string.IsNullOrEmpty(reference.AssetGuid))
            {
                string guidPath = AssetDatabase.GUIDToAssetPath(reference.AssetGuid);
                if (string.IsNullOrEmpty(path))
                    path = guidPath;
                else if (!string.Equals(path, guidPath, StringComparison.Ordinal))
                    return false;
            }
            if (!IsExactAssetPath(path))
                return false;
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset && string.Equals(AssetDatabase.GetAssetPath(asset), path, StringComparison.Ordinal);
        }

        static bool IsExactAssetPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal) && !path.Contains("\\") && !path.Contains("/../");
        }
    }

    public static class AgentMutationGraphAuthoringUtility
    {
        public static bool TryLinkLifecycleSlot(
            AgentMutationSession session,
            StateBehaviorSubTree graph,
            string lifecycleSlot,
            BaseNode child,
            string path)
        {
            if (!child)
                return false;
            BaseNode anchor = ResolveLifecycleAnchor(graph, lifecycleSlot);
            if (!anchor)
            {
                session.Report.Error(path, "lifecycle_anchor_not_found", $"生命周期入口无法解析：{lifecycleSlot}");
                return false;
            }

            BaseEdge existing = FindAnyOutputEdge(graph, anchor, "Output");
            if (existing != null)
            {
                if (existing.EndNode == child)
                    return true;
                session.Report.Warning(path, "lifecycle_slot_occupied", $"{lifecycleSlot} 已连接到 {existing.EndNode?.ResolvedDisplayName}，未覆盖作者已有结构。");
                return false;
            }

            BaseEdge edge = graph.Link(anchor, child, "Output", "Input");
            if (edge != null)
                return true;
            session.Report.Error(path, "lifecycle_link_failed", $"无法连接 {lifecycleSlot} -> {child.ResolvedDisplayName}");
            return false;
        }

        public static BaseNode ResolveLifecycleAnchor(StateBehaviorSubTree graph, string lifecycleSlot)
        {
            string slot = string.IsNullOrEmpty(lifecycleSlot) ? "Root" : lifecycleSlot;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                if (slot == "Root" && node is RootNode)
                    return node;
                if (slot == "OnEnter" && node is StateOnEnterNode)
                    return node;
                if (slot == "OnExit" && node is StateOnExitNode)
                    return node;
            }
            return null;
        }

        public static BaseEdge EnsureSingleTransition(StateMachineGraph graph, BaseNode source, BaseNode target)
        {
            List<BaseEdge> matches = graph.Edges.Where(edge =>
                edge != null &&
                (edge.StartNode == source || edge.StartNodeGUID == source.GUID) &&
                (edge.EndNode == target || edge.EndNodeGUID == target.GUID) &&
                edge.StartPortName == StateMachinePorts.StateOut &&
                edge.EndPortName == StateMachinePorts.StateIn).ToList();
            BaseEdge transition = matches.FirstOrDefault();
            for (int i = 1; i < matches.Count; i++)
                graph.UnLink(matches[i]);
            return transition ?? graph.Link(source, target, StateMachinePorts.StateOut, StateMachinePorts.StateIn);
        }

        public static BaseEdge FindFlowEdge(BaseGraph graph, BaseNode source, BaseNode target, string startPort, string endPort)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge != null && edge.StartNode == source && edge.EndNode == target &&
                    edge.StartPortName == startPort && edge.EndPortName == endPort)
                    return edge;
            }
            return null;
        }

        public static BaseEdge FindAnyOutputEdge(BaseGraph graph, BaseNode source, string startPort)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge != null && edge.StartNode == source && edge.StartPortName == startPort)
                    return edge;
            }
            return null;
        }

        public static void RemoveOrphanLinks(BaseGraph graph)
        {
            HashSet<string> nodeGuids = graph.Nodes.Where(node => node != null).Select(node => node.GUID).ToHashSet();
            graph.PropertyEdges.RemoveAll(edge => edge == null);
            graph.Edges.RemoveAll(edge => edge == null);
            foreach (PropertyEdge edge in graph.PropertyEdges.Where(edge =>
                         !nodeGuids.Contains(edge.StartNodeGUID) || !nodeGuids.Contains(edge.EndNodeGUID)).ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseEdge edge in graph.Edges.Where(edge =>
                         !nodeGuids.Contains(edge.StartNodeGUID) || !nodeGuids.Contains(edge.EndNodeGUID)).ToList())
                graph.UnLink(edge);
        }
    }
}
