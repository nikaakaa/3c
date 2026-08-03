using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMutationSession
    {
        readonly Dictionary<string, AgentResolvedIdentity> m_ResolvedIdentities =
            new Dictionary<string, AgentResolvedIdentity>(StringComparer.Ordinal);
        readonly HashSet<UnityEngine.Object> m_TouchedOwners = new HashSet<UnityEngine.Object>();
        readonly Dictionary<string, AgentPlannedBlackboardDeclaration> m_PlannedBlackboardDeclarations =
            new Dictionary<string, AgentPlannedBlackboardDeclaration>(StringComparer.Ordinal);
        readonly HashSet<string> m_PlannedGameplayTags = new HashSet<string>(StringComparer.Ordinal);

        public AgentMutationSession(
            CharacterPipelineDefinition definition,
            AgentGraphSnapshot snapshot,
            AgentMutationPlan plan,
            AgentCompileReport report,
            bool apply)
        {
            Definition = definition;
            Domain = AgentAuthoringSchema.CharacterControllerDomain;
            Snapshot = snapshot;
            Plan = plan;
            Report = report;
            IsApply = apply;
            Resolver = new AgentAssetResolver(definition, snapshot);
            Index = new AgentGraphAuthoringIndex();
        }

        public AgentMutationSession(
            AIControllerDefinition definition,
            AgentGraphSnapshot snapshot,
            AgentMutationPlan plan,
            AgentCompileReport report,
            bool apply)
        {
            AIDefinition = definition;
            Domain = AgentAuthoringSchema.AIControllerDomain;
            Snapshot = snapshot;
            Plan = plan;
            Report = report;
            IsApply = apply;
            Resolver = new AgentAssetResolver(definition ? definition.ControlledCharacter : null, snapshot);
            Index = new AgentGraphAuthoringIndex();
        }

        public CharacterPipelineDefinition Definition { get; }
        public AIControllerDefinition AIDefinition { get; }
        public string Domain { get; }
        public AgentGraphSnapshot Snapshot { get; }
        public AgentMutationPlan Plan { get; }
        public AgentCompileReport Report { get; }
        public bool IsApply { get; }
        public AgentAssetResolver Resolver { get; }
        public AgentGraphAuthoringIndex Index { get; }
        public BaseTree RootTree { get; private set; }
        public IReadOnlyCollection<UnityEngine.Object> TouchedOwners => m_TouchedOwners;

        public bool Initialize()
        {
            if (!Definition && !AIDefinition)
            {
                Report.Error("definition", "missing_definition", $"{Domain} root definition 缺失。");
                return false;
            }
            if (Snapshot == null)
            {
                Report.Error("snapshot", "missing_snapshot", "Agent Graph Snapshot 缺失。");
                return false;
            }
            if (!string.Equals(Snapshot.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                Report.Error(
                    "snapshot.schemaVersion",
                    "unsupported_schema_version",
                    $"Snapshot schema 必须是 {AgentAuthoringSchema.Version}，当前为 {Snapshot.schemaVersion}。");
                return false;
            }
            if (!string.Equals(Snapshot.domain, Domain, StringComparison.Ordinal) ||
                !string.Equals(Plan.Domain, Domain, StringComparison.Ordinal))
            {
                Report.Error("snapshot.domain", "domain_root_mismatch", "Snapshot、Mutation Plan与加载的root domain不一致。");
                return false;
            }
            BaseTree rootTree;
            if (Definition)
            {
                if (!Resolver.TryGetRootTree(out rootTree, Report, "definition"))
                    return false;
            }
            else if (AIDefinition.RootTreeAsset?.Tree is AIControllerTree aiRoot)
            {
                rootTree = aiRoot;
            }
            else
            {
                Report.Error("definition", "missing_ai_root_tree", "AIControllerDefinition 缺少正式 AIControllerTree。");
                return false;
            }

            RootTree = rootTree;
            UnityEngine.Object rootAsset = Definition ? Definition.RootTreeAsset : AIDefinition.RootTreeAsset;
            UnityEngine.Object rootDefinition = Definition ? Definition : AIDefinition;
            string definitionPath = AssetDatabase.GetAssetPath(rootDefinition);
            string rootPath = AssetDatabase.GetAssetPath(rootAsset);
            string rootIdentity = Definition ? AssetDatabase.AssetPathToGUID(definitionPath) : AIDefinition.ControllerId;
            if (!string.Equals(Snapshot.rootAssetPath, definitionPath, StringComparison.Ordinal) ||
                !string.Equals(Snapshot.rootIdentity, rootIdentity, StringComparison.Ordinal) ||
                !string.Equals(Plan.RootIdentity, rootIdentity, StringComparison.Ordinal) ||
                !string.Equals(Plan.SourceRevision, Snapshot.sourceRevision, StringComparison.Ordinal) ||
                !string.Equals(Snapshot.rootTreeAssetPath, rootPath, StringComparison.Ordinal) ||
                !string.Equals(Snapshot.rootGraphAuthoringId, RootTree.GraphAuthoringId, StringComparison.Ordinal))
            {
                Report.Error("snapshot", "snapshot_source_changed", "Snapshot与Mutation Plan的root identity或source revision与当前Definition不一致，请重新执行Document dry-run。");
                return false;
            }
            return RefreshIndex("definition");
        }

        public bool RefreshIndex(string path)
        {
            try
            {
                Index.Rebuild(RootTree);
                return true;
            }
            catch (Exception exception)
            {
                Report.Error(path, "authoring_identity_index_invalid", exception.Message);
                return false;
            }
        }

        public bool TryResolveGraph(AgentGraphTargetReference reference, string path, out BaseTree graph)
        {
            return TryResolveGraph(reference.Value, path, out graph);
        }

        public bool TryResolveFlowEdge(
            BaseTree graph,
            AgentFlowEdgeTargetReference reference,
            string path,
            out BaseEdge edge)
        {
            edge = null;
            AgentAuthoringReference value = reference.Value;
            if (!string.IsNullOrEmpty(value.AuthoringId))
            {
                edge = graph?.Edges.FirstOrDefault(candidate => candidate != null && string.Equals(candidate.GUID, value.AuthoringId, StringComparison.Ordinal));
                if (edge != null)
                    return true;
                Report.Error(path, "flow_edge_not_found", $"Flow edge authoring identity 无法解析：{value.AuthoringId}");
                return false;
            }
            if (!value.PlannedIdentity.IsValid)
            {
                Report.Error(path, "flow_edge_not_found", "Flow edge reference 缺失。");
                return false;
            }
            if (!IsApply)
                return true;
            if (m_ResolvedIdentities.TryGetValue(value.PlannedIdentity.Identity, out AgentResolvedIdentity output) &&
                output.Edge != null && graph != null && graph.Edges.Contains(output.Edge))
            {
                edge = output.Edge;
                return true;
            }
            Report.Error(path, "flow_edge_local_identity_unresolved", $"Flow edge local identity无法解析：{value.PlannedIdentity.Value}");
            return false;
        }

        public bool TryResolveStateMachine(
            AgentStateMachineTargetReference reference,
            string path,
            out StateMachineGraph graph)
        {
            graph = null;
            AgentAuthoringReference value = reference.Value;
            if (!string.IsNullOrEmpty(value.AuthoringId))
            {
                if (Index.TryFindStateMachineGraph(value.AuthoringId, out graph))
                    return true;
                Report.Error(path, "state_machine_not_found", $"StateMachineGraph authoring identity 无法解析：{value.AuthoringId}");
                return false;
            }
            if (!value.PlannedIdentity.IsValid)
            {
                Report.Error(path, "state_machine_not_found", "StateMachineGraph reference 缺失。");
                return false;
            }
            if (!IsApply)
                return true;
            if (m_ResolvedIdentities.TryGetValue(value.PlannedIdentity.Identity, out AgentResolvedIdentity output) && output.Graph is StateMachineGraph stateMachine)
            {
                graph = stateMachine;
                return true;
            }
            Report.Error(path, "state_machine_local_identity_unresolved", $"StateMachine local identity无法解析：{value.PlannedIdentity.Value}");
            return false;
        }

        public bool TryResolveStateBehavior(
            AgentStateBehaviorTargetReference target,
            string path,
            out StateBehaviorSubTree graph)
        {
            graph = null;
            if (target.IsDirect)
            {
                if (!TryResolveGraph(target.DirectGraph, path, out BaseTree direct))
                    return false;
                if (direct == null && !IsApply)
                    return true;
                graph = direct as StateBehaviorSubTree;
                if (graph != null)
                    return true;
                Report.Error(path, "target_graph_wrong_type", "target graph identity 不是 StateBehaviorSubTree。");
                return false;
            }

            if (!TryResolveStateMachine(target.StateMachine, path, out StateMachineGraph stateMachine))
                return false;
            if (stateMachine == null && !IsApply)
                return true;
            if (!TryResolveNode(stateMachine, target.State.Value, path, out BaseNode resolved))
                return false;
            if (resolved == null && !IsApply)
                return true;
            if (resolved is not StateNode state)
            {
                Report.Error(path, "state_not_found", "State identity 没有指向 StateNode。");
                return false;
            }
            graph = Index.GetStateBehaviorTree(state);
            if (graph)
                return true;
            Report.Error(path, "state_behavior_missing", $"状态缺少 StateBehaviorSubTree：{state.ResolvedDisplayName}");
            return false;
        }

        public bool TryResolveNode(
            BaseGraph graph,
            AgentElementTargetReference reference,
            string path,
            out BaseNode node)
        {
            return TryResolveNode(graph, reference.Value, path, out node);
        }

        public bool TryResolveOptionalNode(
            BaseGraph graph,
            AgentElementTargetReference reference,
            string path,
            out BaseNode node)
        {
            node = null;
            return !reference.IsValid || TryResolveNode(graph, reference.Value, path, out node);
        }

        public bool TryResolveTransitionEndpoints(
            AgentEnsureTransitionMutation command,
            out StateMachineGraph graph,
            out BaseNode from,
            out BaseNode to)
        {
            graph = null;
            from = null;
            to = null;
            if (!TryResolveStateMachine(command.StateMachine, command.Path, out graph))
                return false;
            if (graph == null && !IsApply)
                return true;
            return TryResolveNode(graph, command.From.Value, command.Path, out from) &&
                   TryResolveNode(graph, command.To.Value, command.Path, out to);
        }

        public void AddPlanned(AgentMutation command, BaseGraph graph, string target, string detail)
        {
            Report.plannedDiff.Add(new AgentCompileDiffEntry
            {
                mutationId = command.Id,
                action = command.OperationName,
                graph = graph != null ? Index.GetGraphPath(graph) : command.OwnerScope,
                target = target ?? string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public void PlanBlackboardDeclaration(string declarationId, string ownerGraphAuthoringId, Type valueType, string key = null)
        {
            if (string.IsNullOrEmpty(declarationId))
                return;
            m_PlannedBlackboardDeclarations[declarationId] = new AgentPlannedBlackboardDeclaration(ownerGraphAuthoringId, valueType, key);
        }

        public bool TryGetPlannedBlackboardDeclaration(string declarationId, out string ownerGraphAuthoringId, out Type valueType)
        {
            ownerGraphAuthoringId = string.Empty;
            valueType = null;
            if (!m_PlannedBlackboardDeclarations.TryGetValue(declarationId, out AgentPlannedBlackboardDeclaration value))
                return false;
            ownerGraphAuthoringId = value.OwnerGraphAuthoringId;
            valueType = value.ValueType;
            return true;
        }

        public void PlanGameplayTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                m_PlannedGameplayTags.Add(tag);
        }

        public bool IsGameplayTagPlanned(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && m_PlannedGameplayTags.Contains(tag);
        }

        public ThirdPersonSimulation.ActionTargetRequirement ResolveEffectiveTargetRequirement(
            ThirdPersonCharacter.ActionSystem.ActionProfile profile)
        {
            if (!profile)
                return ThirdPersonSimulation.ActionTargetRequirement.None;
            ThirdPersonSimulation.ActionTargetRequirement requirement = profile.TargetRequirement;
            for (int i = 0; i < Plan.Commands.Count; i++)
            {
                if (Plan.Commands[i] is AgentSetActionProfileTargetRequirementMutation command &&
                    string.Equals(command.ActionProfile.LogicalId, profile.ActionId, StringComparison.Ordinal))
                {
                    requirement = command.TargetRequirement;
                }
            }
            return requirement;
        }

        public bool HasEffectiveTargetSnapshot(ActivateActionInstanceNode node)
        {
            if (!node)
                return false;
            for (int i = 0; i < Plan.Commands.Count; i++)
            {
                if (Plan.Commands[i] is AgentEnsureActionActivationMutation command &&
                    string.Equals(command.ExistingElement.Identity, node.GUID, StringComparison.Ordinal))
                {
                    return !string.IsNullOrWhiteSpace(command.TargetSnapshotBlackboardKey);
                }
            }
            return node.TargetSnapshotVariable.IsValid;
        }

        public void AddApplied(
            AgentMutation command,
            BaseGraph graph,
            BaseNode node,
            string detail)
        {
            Touch(graph);
            m_ResolvedIdentities[command.Id] = new AgentResolvedIdentity(
                node is StateMachineNode stateMachine ? stateMachine.Graph : node is StateNode state ? state.SubTree : null,
                node,
                null,
                null);
            Report.appliedDiff.Add(new AgentCompileDiffEntry
            {
                mutationId = command.Id,
                action = command.OperationName,
                graph = Index.GetGraphPath(graph),
                target = node != null ? node.ResolvedDisplayName : string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public void AddApplied(
            AgentMutation command,
            BaseGraph graph,
            BaseEdge edge,
            string detail)
        {
            Touch(graph);
            m_ResolvedIdentities[command.Id] = new AgentResolvedIdentity(null, null, edge, null);
            Report.appliedDiff.Add(new AgentCompileDiffEntry
            {
                mutationId = command.Id,
                action = command.OperationName,
                graph = Index.GetGraphPath(graph),
                target = edge != null ? $"{edge.StartNode?.ResolvedDisplayName}->{edge.EndNode?.ResolvedDisplayName}" : string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public void AddAppliedWithoutIdentity(AgentMutation command, BaseGraph graph, string target, string detail)
        {
            Touch(graph);
            m_ResolvedIdentities[command.Id] = default;
            Report.appliedDiff.Add(new AgentCompileDiffEntry
            {
                mutationId = command.Id,
                action = command.OperationName,
                graph = Index.GetGraphPath(graph),
                target = target ?? string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public void AddAppliedAuthoring(
            AgentMutation command,
            UnityEngine.Object owner,
            object value,
            string target,
            string detail)
        {
            Touch(owner);
            m_ResolvedIdentities[command.Id] = new AgentResolvedIdentity(null, null, null, value);
            Report.appliedDiff.Add(new AgentCompileDiffEntry
            {
                mutationId = command.Id,
                action = command.OperationName,
                graph = command.OwnerScope,
                target = target ?? string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public bool TryResolvePlannedIdentity<T>(AgentPlannedIdentityReference reference, string path, out T value) where T : class
        {
            value = null;
            if (!reference.IsValid)
                return false;
            if (!IsApply)
                return true;
            if (m_ResolvedIdentities.TryGetValue(reference.Identity, out AgentResolvedIdentity output) &&
                TryGetResolvedValue(output, out value))
            {
                return true;
            }
            Report.Error(path, "authoring_local_identity_unresolved", $"Authoring local identity无法解析：{reference.Value}");
            return false;
        }

        static bool TryGetResolvedValue<T>(AgentResolvedIdentity output, out T value) where T : class
        {
            if (output.AuthoringValue is T authoringValue)
            {
                value = authoringValue;
                return true;
            }
            if (output.Node is T node)
            {
                value = node;
                return true;
            }
            if (output.Edge is T edge)
            {
                value = edge;
                return true;
            }
            if (output.Graph is T graph)
            {
                value = graph;
                return true;
            }
            value = null;
            return false;
        }

        public bool TryResolveDeclaration(BaseGraph graph, AgentAuthoringReference reference, string path, out BaseExposedProperty declaration)
        {
            declaration = null;
            if (!string.IsNullOrEmpty(reference.AuthoringId))
            {
                if (graph != null)
                {
                    for (int i = 0; i < graph.ExposedProperties.Count; i++)
                    {
                        BaseExposedProperty candidate = graph.ExposedProperties[i];
                        if (candidate != null && string.Equals(candidate.DeclarationId, reference.AuthoringId, StringComparison.Ordinal))
                        {
                            declaration = candidate;
                            return true;
                        }
                    }
                }
                Report.Error(path, "blackboard_declaration_not_found", $"Blackboard declaration identity 无法解析：{reference.AuthoringId}");
                return false;
            }
            if (!reference.PlannedIdentity.IsValid)
            {
                Report.Error(path, "blackboard_declaration_not_found", "Blackboard declaration reference 缺失。");
                return false;
            }
            if (!IsApply)
                return true;
            if (m_ResolvedIdentities.TryGetValue(reference.PlannedIdentity.Identity, out AgentResolvedIdentity output) && output.AuthoringValue is BaseExposedProperty created)
            {
                declaration = created;
                return true;
            }
            Report.Error(path, "blackboard_declaration_local_identity_unresolved", $"Blackboard declaration local identity无法解析：{reference.PlannedIdentity.Value}");
            return false;
        }

        public void Touch(BaseGraph graph)
        {
            if (graph?.SerializedOwner != null)
                m_TouchedOwners.Add(graph.SerializedOwner);
        }

        public void Touch(UnityEngine.Object owner)
        {
            if (owner != null)
                m_TouchedOwners.Add(owner);
        }

        public bool TryResolveBlackboardDeclaration(
            string key,
            Type expectedType,
            string path,
            out PipelineBlackboardVariableReference reference,
            out BaseExposedProperty declaration)
        {
            reference = default;
            declaration = null;
            List<BaseExposedProperty> matches = RootTree.ExposedProperties
                .Where(value => string.Equals(value.BlackboardKey, key, StringComparison.Ordinal))
                .ToList();
            if (matches.Count == 0 && !IsApply)
            {
                AgentPlannedBlackboardDeclaration[] planned = m_PlannedBlackboardDeclarations.Values
                    .Where(value => string.Equals(value.Key, key, StringComparison.Ordinal))
                    .ToArray();
                if (planned.Length == 1 && planned[0].ValueType == expectedType)
                    return true;
            }
            if (matches.Count != 1)
            {
                Report.Error(path, matches.Count == 0 ? "blackboard_declaration_missing" : "blackboard_declaration_ambiguous", $"Pipeline Blackboard declaration 必须唯一：{key}");
                return false;
            }
            declaration = matches[0];
            if (declaration.ValueType != expectedType)
            {
                Report.Error(path, "blackboard_type_mismatch", $"Pipeline Blackboard declaration {key} 需要 {expectedType.Name}，当前为 {declaration.ValueType?.Name ?? "Unknown"}。");
                declaration = null;
                return false;
            }
            reference = declaration.CreateBlackboardReference();
            return true;
        }

        bool TryResolveGraph(AgentAuthoringReference reference, string path, out BaseTree graph)
        {
            graph = null;
            if (!string.IsNullOrEmpty(reference.AuthoringId))
            {
                if (Index.TryGetGraph(reference.AuthoringId, out graph))
                    return true;
                Report.Error(path, "graph_not_found", $"Graph authoring identity 无法解析：{reference.AuthoringId}");
                return false;
            }
            if (!reference.PlannedIdentity.IsValid)
            {
                Report.Error(path, "graph_not_found", "Graph reference 缺失。");
                return false;
            }
            if (!IsApply)
                return true;
            if (m_ResolvedIdentities.TryGetValue(reference.PlannedIdentity.Identity, out AgentResolvedIdentity output) && output.Graph is BaseTree generated)
            {
                graph = generated;
                return true;
            }
            Report.Error(path, "graph_local_identity_unresolved", $"Graph local identity无法解析：{reference.PlannedIdentity.Value}");
            return false;
        }

        bool TryResolveNode(BaseGraph graph, AgentAuthoringReference reference, string path, out BaseNode node)
        {
            node = null;
            if (!string.IsNullOrEmpty(reference.AuthoringId))
            {
                if (graph != null && reference.AuthoringId.StartsWith("@", StringComparison.Ordinal))
                {
                    var catalog =
                        new BtsmtlGraphAuthoringCapabilities();
                    node = graph.Nodes.FirstOrDefault(candidate =>
                        candidate != null &&
                        catalog.TryGetAnchor(candidate.GetType().FullName, out string anchor) &&
                        string.Equals(anchor, reference.AuthoringId, StringComparison.Ordinal));
                    if (node != null)
                        return true;
                }
                if (graph != null && Index.TryFindNode(graph, reference.AuthoringId, out node))
                    return true;
                if (graph == null && !IsApply)
                    return true;
                Report.Error(path, "element_identity_not_found", $"Element authoring identity 无法在目标 Graph 中解析：{reference.AuthoringId}");
                return false;
            }
            if (!reference.PlannedIdentity.IsValid)
            {
                Report.Error(path, "element_identity_not_found", "Element reference 缺失。");
                return false;
            }
            if (!IsApply)
                return true;
            if (!m_ResolvedIdentities.TryGetValue(reference.PlannedIdentity.Identity, out AgentResolvedIdentity output))
            {
                Report.Error(path, "element_local_identity_unresolved", $"Element local identity 无法解析：{reference.PlannedIdentity.Value}");
                return false;
            }
            if (!string.IsNullOrEmpty(reference.PlannedIdentity.Role))
                node = ResolveStateMachineControl(output.Graph as StateMachineGraph, reference.PlannedIdentity.Role);
            else
                node = output.Node;
            if (node != null && graph != null && graph.Nodes.Contains(node))
                return true;
            Report.Error(path, "element_local_identity_unresolved", $"Element local identity 不属于目标 Graph：{reference.PlannedIdentity.Value}");
            node = null;
            return false;
        }

        static BaseNode ResolveStateMachineControl(StateMachineGraph graph, string role)
        {
            if (graph == null)
                return null;
            switch (role)
            {
                case "StateMachineEnterNode": return graph.EnterNode;
                case "StateMachineAnyStateNode": return graph.AnyStateNode;
                case "StateMachineExitNode": return graph.ExitNode;
                default: return null;
            }
        }
    }

    readonly struct AgentPlannedBlackboardDeclaration
    {
        public AgentPlannedBlackboardDeclaration(string ownerGraphAuthoringId, Type valueType, string key)
        {
            OwnerGraphAuthoringId = ownerGraphAuthoringId ?? string.Empty;
            ValueType = valueType;
            Key = key ?? string.Empty;
        }

        public string OwnerGraphAuthoringId { get; }
        public Type ValueType { get; }
        public string Key { get; }
    }

    public readonly struct AgentResolvedIdentity
    {
        public AgentResolvedIdentity(BaseGraph graph, BaseNode node, BaseEdge edge, object authoringValue)
        {
            Graph = graph;
            Node = node;
            Edge = edge;
            AuthoringValue = authoringValue;
        }

        public BaseGraph Graph { get; }
        public BaseNode Node { get; }
        public BaseEdge Edge { get; }
        public object AuthoringValue { get; }
    }

    public sealed class AgentDocumentPreparation
    {
        internal AgentDocumentPreparation(
            AgentMutationPlan plan,
            AgentGraphSnapshot snapshot,
            AgentDocumentBoundaryIdentity boundary,
            AgentCompileReport report,
            AgentPresentationMutationPlan presentationPlan = null)
        {
            Plan = plan;
            Snapshot = snapshot;
            Boundary = boundary;
            Report = report;
            PresentationPlan = presentationPlan;
        }

        public AgentMutationPlan Plan { get; }
        public AgentCompileReport Report { get; }
        internal AgentGraphSnapshot Snapshot { get; }
        internal AgentDocumentBoundaryIdentity Boundary { get; }
        public AgentPresentationMutationPlan PresentationPlan { get; }
        public bool IsValid => Plan != null && Report != null && !Report.HasErrors();
    }

    public sealed class AgentDocumentBoundaryIdentity
    {
        readonly Dictionary<string, BaseTree> m_Graphs;

        AgentDocumentBoundaryIdentity(
            UnityEngine.Object definition,
            UnityEngine.Object rootTreeAsset,
            BaseTree rootTree,
            string definitionPath,
            string definitionGuid,
            string rootPath,
            string rootGuid,
            IDictionary<string, BaseTree> graphs)
        {
            Definition = definition;
            RootTreeAsset = rootTreeAsset;
            RootTree = rootTree;
            DefinitionPath = definitionPath;
            DefinitionGuid = definitionGuid;
            RootPath = rootPath;
            RootGuid = rootGuid;
            RootGraphAuthoringId = rootTree?.GraphAuthoringId ?? string.Empty;
            m_Graphs = new Dictionary<string, BaseTree>(graphs, StringComparer.Ordinal);
        }

        UnityEngine.Object Definition { get; }
        UnityEngine.Object RootTreeAsset { get; }
        BaseTree RootTree { get; }
        string DefinitionPath { get; }
        string DefinitionGuid { get; }
        string RootPath { get; }
        string RootGuid { get; }
        string RootGraphAuthoringId { get; }

        public static AgentDocumentBoundaryIdentity Capture(AgentMutationSession session)
        {
            UnityEngine.Object definition = session.Definition ? session.Definition : session.AIDefinition;
            UnityEngine.Object rootTreeAsset = session.Definition ? session.Definition.RootTreeAsset : session.AIDefinition.RootTreeAsset;
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string rootPath = AssetDatabase.GetAssetPath(rootTreeAsset);
            var graphs = new Dictionary<string, BaseTree>(StringComparer.Ordinal);
            for (int i = 0; i < session.Plan.Commands.Count; i++)
            {
                string owner = session.Plan.Commands[i].OwnerScope;
                if (AuthoringIdentity.IsValid(owner) && session.Index.TryGetGraph(owner, out BaseTree graph))
                    graphs[owner] = graph;
            }
            return new AgentDocumentBoundaryIdentity(
                definition,
                rootTreeAsset,
                session.RootTree,
                definitionPath,
                AssetDatabase.AssetPathToGUID(definitionPath),
                rootPath,
                AssetDatabase.AssetPathToGUID(rootPath),
                graphs);
        }

        public bool Validate(UnityEngine.Object definition, AgentMutationSession session, AgentCompileReport report)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            UnityEngine.Object rootTreeAsset = definition switch
            {
                CharacterPipelineDefinition character => character.RootTreeAsset,
                ThirdPersonCharacter.AI.AIControllerDefinition ai => ai.RootTreeAsset,
                _ => null
            };
            string rootPath = rootTreeAsset ? AssetDatabase.GetAssetPath(rootTreeAsset) : string.Empty;
            if (definition != Definition ||
                rootTreeAsset != RootTreeAsset ||
                session.RootTree != RootTree ||
                !string.Equals(definitionPath, DefinitionPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.AssetPathToGUID(definitionPath), DefinitionGuid, StringComparison.Ordinal) ||
                !string.Equals(rootPath, RootPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.AssetPathToGUID(rootPath), RootGuid, StringComparison.Ordinal) ||
                !string.Equals(session.RootTree?.GraphAuthoringId, RootGraphAuthoringId, StringComparison.Ordinal))
            {
                report.Error("transaction", "authoring_source_changed", "Definition 或 RootTree identity 在 dry-run 与 apply 之间发生变化。");
                return false;
            }

            foreach (KeyValuePair<string, BaseTree> pair in m_Graphs)
            {
                if (!session.Index.TryGetGraph(pair.Key, out BaseTree current) || !ReferenceEquals(current, pair.Value))
                {
                    report.Error("transaction", "authoring_graph_changed", $"Graph identity 在 dry-run 与 apply 之间发生变化：{pair.Key}");
                    return false;
                }
            }
            return true;
        }
    }
}
