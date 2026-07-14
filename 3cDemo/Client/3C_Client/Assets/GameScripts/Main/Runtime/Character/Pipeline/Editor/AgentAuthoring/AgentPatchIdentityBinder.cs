using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentPatchIdentityBinder
    {
        readonly Dictionary<string, AgentSnapshotGraph> m_Graphs = new Dictionary<string, AgentSnapshotGraph>(StringComparer.Ordinal);
        readonly Dictionary<string, IdentityReference> m_StateMachines = new Dictionary<string, IdentityReference>(StringComparer.Ordinal);
        readonly Dictionary<string, IdentityReference> m_States = new Dictionary<string, IdentityReference>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_GraphOperations = new Dictionary<string, string>(StringComparer.Ordinal);

        AgentGraphSnapshot m_Snapshot;
        AgentCompileReport m_Report;

        public bool TryBind(AgentPatchIR patch, AgentGraphSnapshot snapshot, AgentCompileReport report)
        {
            m_Snapshot = snapshot;
            m_Report = report;
            m_Graphs.Clear();
            m_StateMachines.Clear();
            m_States.Clear();
            m_GraphOperations.Clear();

            if (patch == null || snapshot == null)
            {
                report?.Error("identity-binding", "binding_input_missing", "Identity binding 缺少 Patch 或 Full Snapshot。");
                return false;
            }
            if (!string.Equals(patch.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal) ||
                !string.Equals(snapshot.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                report?.Error("identity-binding", "unsupported_schema_version", $"Identity binding 只接受 {AgentAuthoringSchema.Version}。");
                return false;
            }
            if (!string.Equals(snapshot.exportMode, AgentSnapshotExportMode.Full.ToString(), StringComparison.Ordinal))
            {
                report?.Error("identity-binding", "full_snapshot_required", "Macro identity binding 必须使用 Full Snapshot。");
                return false;
            }

            for (int i = 0; i < snapshot.graphs.Count; i++)
            {
                AgentSnapshotGraph graph = snapshot.graphs[i];
                if (graph != null && !string.IsNullOrEmpty(graph.graphAuthoringId))
                    m_Graphs.Add(graph.graphAuthoringId, graph);
            }

            RegisterExistingStateMachines();
            for (int i = 0; i < patch.operations.Count; i++)
                BindOperation(patch.operations[i], $"patch.operations[{i}]");
            return report == null || !report.HasErrors();
        }

        void RegisterExistingStateMachines()
        {
            for (int i = 0; i < m_Snapshot.stateMachines.Count; i++)
            {
                AgentSnapshotStateMachineSummary stateMachine = m_Snapshot.stateMachines[i];
                if (stateMachine == null)
                    continue;

                RegisterUnique(m_StateMachines, stateMachine.name, new IdentityReference(stateMachine.graphAuthoringId, string.Empty), "state_machine_ambiguous");
                for (int stateIndex = 0; stateIndex < stateMachine.states.Count; stateIndex++)
                {
                    AgentSnapshotStateSummary state = stateMachine.states[stateIndex];
                    if (state == null)
                        continue;
                    RegisterUnique(
                        m_States,
                        StateKey(stateMachine.name, state.state),
                        new IdentityReference(state.stateAuthoringId, string.Empty),
                        "state_ambiguous");
                }
            }
        }

        void BindOperation(AgentPatchOperation operation, string path)
        {
            if (operation == null)
                return;

            switch (operation.op)
            {
                case "ensure_state_machine":
                    BindEnsureStateMachine(operation, path);
                    return;
                case "ensure_state":
                    BindEnsureState(operation, path);
                    return;
            }

            BindGraph(operation, path);
            BindTargetGraph(operation, path);
            BindStateMachine(operation, path);
            BindState(operation, path);
            BindTransitionEndpoints(operation, path);
            BindNodeReference(operation, true, path);
            BindNodeReference(operation, false, path);
            BindExistingEnsureTarget(operation, path);
        }

        void BindEnsureStateMachine(AgentPatchOperation operation, string path)
        {
            BindGraph(operation, path, true);
            string selector = Required(operation.stateMachine, operation.displayName);
            IdentityReference existing = ResolveStateMachine(selector, path, false);
            if (existing.IsValid)
            {
                operation.stateMachineGraphAuthoringId = existing.AuthoringId;
                if (m_Graphs.TryGetValue(operation.graphAuthoringId, out AgentSnapshotGraph parent))
                {
                    AgentSnapshotNode owner = FindUniqueNode(parent, operation.displayName, "StateMachineNode", path, false);
                    operation.targetElementAuthoringId = owner?.elementAuthoringId ?? string.Empty;
                }
            }
            else
            {
                operation.stateMachineOperationId = operation.id;
                RegisterUnique(m_StateMachines, selector, new IdentityReference(string.Empty, operation.id), "state_machine_ambiguous");
            }
            RegisterGraphOperation(selector, operation.id);
        }

        void BindEnsureState(AgentPatchOperation operation, string path)
        {
            BindStateMachine(operation, path, true);
            string selector = Required(operation.state, operation.displayName);
            IdentityReference existing = ResolveState(operation.stateMachine, selector, path, false);
            if (existing.IsValid)
                operation.stateAuthoringId = existing.AuthoringId;
            else
            {
                operation.stateOperationId = operation.id;
                RegisterUnique(m_States, StateKey(operation.stateMachine, selector), new IdentityReference(string.Empty, operation.id), "state_ambiguous");
            }
            RegisterGraphOperation($"{selector} State Body", operation.id);
        }

        void BindGraph(AgentPatchOperation operation, string path, bool required = false)
        {
            if (!string.IsNullOrEmpty(operation.graphAuthoringId) || !string.IsNullOrEmpty(operation.graphOperationId))
                return;
            if (string.IsNullOrEmpty(operation.graph))
            {
                if (required)
                    m_Report?.Error(path, "graph_identity_missing", "Operation 缺少 graphAuthoringId/graphOperationId。");
                return;
            }

            if (m_GraphOperations.TryGetValue(operation.graph, out string operationId))
            {
                operation.graphOperationId = operationId;
                return;
            }

            AgentSnapshotGraph graph = ResolveGraphSelector(operation.graph, path, required);
            if (graph != null)
                operation.graphAuthoringId = graph.graphAuthoringId;
        }

        void BindTargetGraph(AgentPatchOperation operation, string path)
        {
            if (!string.IsNullOrEmpty(operation.targetGraphAuthoringId) || !string.IsNullOrEmpty(operation.targetGraphOperationId) || string.IsNullOrEmpty(operation.targetGraph))
                return;
            if (m_GraphOperations.TryGetValue(operation.targetGraph, out string operationId))
                operation.targetGraphOperationId = operationId;
            else
                operation.targetGraphAuthoringId = ResolveGraphSelector(operation.targetGraph, path, true)?.graphAuthoringId ?? string.Empty;
        }

        void BindStateMachine(AgentPatchOperation operation, string path, bool required = false)
        {
            if (!string.IsNullOrEmpty(operation.stateMachineGraphAuthoringId) || !string.IsNullOrEmpty(operation.stateMachineOperationId))
                return;
            if (string.IsNullOrEmpty(operation.stateMachine))
            {
                if (required)
                    m_Report?.Error(path, "state_machine_identity_missing", "Operation 缺少 stateMachineGraphAuthoringId/stateMachineOperationId。");
                return;
            }

            IdentityReference reference = ResolveStateMachine(operation.stateMachine, path, required);
            operation.stateMachineGraphAuthoringId = reference.AuthoringId;
            operation.stateMachineOperationId = reference.OperationId;
        }

        void BindState(AgentPatchOperation operation, string path)
        {
            if (!string.IsNullOrEmpty(operation.stateAuthoringId) || !string.IsNullOrEmpty(operation.stateOperationId) || string.IsNullOrEmpty(operation.state))
                return;
            IdentityReference reference = ResolveState(operation.stateMachine, operation.state, path, true);
            operation.stateAuthoringId = reference.AuthoringId;
            operation.stateOperationId = reference.OperationId;
        }

        void BindTransitionEndpoints(AgentPatchOperation operation, string path)
        {
            if (string.IsNullOrEmpty(operation.from) && string.IsNullOrEmpty(operation.to))
                return;

            IdentityReference from = ResolveEndpoint(operation, operation.from, path);
            operation.fromElementAuthoringId = from.AuthoringId;
            operation.fromOperationId = from.OperationId;
            IdentityReference to = ResolveEndpoint(operation, operation.to, path);
            operation.toElementAuthoringId = to.AuthoringId;
            operation.toOperationId = to.OperationId;
        }

        IdentityReference ResolveEndpoint(AgentPatchOperation operation, string selector, string path)
        {
            IdentityReference state = ResolveState(operation.stateMachine, selector, path, false);
            if (state.IsValid)
                return state;

            if (!string.IsNullOrEmpty(operation.stateMachineGraphAuthoringId) && m_Graphs.TryGetValue(operation.stateMachineGraphAuthoringId, out AgentSnapshotGraph graph))
            {
                AgentSnapshotNode control = FindUniqueNode(graph, selector, ControlType(selector), path, false);
                if (control != null)
                    return new IdentityReference(control.elementAuthoringId, string.Empty);
            }
            if (!string.IsNullOrEmpty(operation.stateMachineOperationId) && !string.IsNullOrEmpty(ControlType(selector)))
                return new IdentityReference(string.Empty, $"{operation.stateMachineOperationId}#{ControlType(selector)}");

            m_Report?.Error(path, "transition_endpoint_identity_missing", $"Transition endpoint 无法绑定稳定 identity：{selector}");
            return default;
        }

        void BindNodeReference(AgentPatchOperation operation, bool source, string path)
        {
            string selector = source ? operation.sourceNode : operation.targetNode;
            if (string.IsNullOrEmpty(selector))
                return;
            if (source && (!string.IsNullOrEmpty(operation.sourceElementAuthoringId) || !string.IsNullOrEmpty(operation.sourceOperationId)) ||
                !source && (!string.IsNullOrEmpty(operation.targetElementAuthoringId) || !string.IsNullOrEmpty(operation.targetOperationId)))
                return;

            string graphId = !string.IsNullOrEmpty(operation.targetGraphAuthoringId)
                ? operation.targetGraphAuthoringId
                : operation.graphAuthoringId;
            if (string.IsNullOrEmpty(graphId) && !string.IsNullOrEmpty(operation.stateAuthoringId))
            {
                AgentSnapshotStateSummary state = m_Snapshot.stateMachines.SelectMany(i => i.states).FirstOrDefault(i => i.stateAuthoringId == operation.stateAuthoringId);
                graphId = state?.behaviorGraphAuthoringId;
            }
            if (string.IsNullOrEmpty(graphId) || !m_Graphs.TryGetValue(graphId, out AgentSnapshotGraph graph))
                return;

            AgentSnapshotNode node = FindUniqueNode(graph, selector, string.Empty, path, true);
            if (source)
                operation.sourceElementAuthoringId = node?.elementAuthoringId ?? string.Empty;
            else
                operation.targetElementAuthoringId = node?.elementAuthoringId ?? string.Empty;
        }

        void BindExistingEnsureTarget(AgentPatchOperation operation, string path)
        {
            if (!string.IsNullOrEmpty(operation.targetElementAuthoringId) || !string.IsNullOrEmpty(operation.targetOperationId))
                return;

            bool supported = operation.op is "ensure_state_behavior_node" or "ensure_timeline_node" or "ensure_action_activation" or "ensure_action_lifecycle_transition" or "ensure_input_node";
            if (!supported)
                return;
            string typeSuffix = operation.op switch
            {
                "ensure_state_behavior_node" => string.IsNullOrEmpty(operation.nodeType) ? "SequenceNode" : operation.nodeType,
                "ensure_timeline_node" => "TimelineNode",
                "ensure_action_activation" => "ActivateActionInstanceNode",
                "ensure_action_lifecycle_transition" => "SubmitActionLifecycleTransitionNode",
                "ensure_input_node" => operation.nodeType,
                _ => string.Empty
            };

            string graphId = operation.graphAuthoringId;
            if (string.IsNullOrEmpty(graphId) && !string.IsNullOrEmpty(operation.targetGraphAuthoringId))
                graphId = operation.targetGraphAuthoringId;
            if (string.IsNullOrEmpty(graphId) && !string.IsNullOrEmpty(operation.stateAuthoringId))
            {
                AgentSnapshotStateSummary state = m_Snapshot.stateMachines.SelectMany(i => i.states)
                    .SingleOrDefault(i => string.Equals(i.stateAuthoringId, operation.stateAuthoringId, StringComparison.Ordinal));
                graphId = state?.behaviorGraphAuthoringId;
            }
            if (string.IsNullOrEmpty(graphId) || !m_Graphs.TryGetValue(graphId, out AgentSnapshotGraph graph))
                return;

            string displayName = !string.IsNullOrEmpty(operation.displayName) ? operation.displayName : operation.inputId;
            List<AgentSnapshotNode> matches = graph.nodes.Where(i =>
                i != null &&
                (string.IsNullOrEmpty(typeSuffix) || i.typeName.EndsWith(typeSuffix, StringComparison.Ordinal)) &&
                string.Equals(i.displayName, displayName, StringComparison.Ordinal)).ToList();
            if (matches.Count == 1)
                operation.targetElementAuthoringId = matches[0].elementAuthoringId;
            else if (matches.Count > 1)
                m_Report?.Error(path, "element_identity_ambiguous", $"已有 element 无法按语义唯一绑定：{displayName}");
        }

        AgentSnapshotGraph ResolveGraphSelector(string selector, string path, bool required)
        {
            if (string.Equals(selector, "root", StringComparison.Ordinal))
                return m_Graphs.TryGetValue(m_Snapshot.rootGraphAuthoringId, out AgentSnapshotGraph root) ? root : null;

            List<AgentSnapshotGraph> matches = m_Snapshot.graphs
                .Where(i => i != null && (string.Equals(i.path, selector, StringComparison.Ordinal) || string.Equals(i.name, selector, StringComparison.Ordinal)))
                .ToList();
            if (matches.Count == 1)
                return matches[0];
            if (required)
                m_Report?.Error(path, matches.Count == 0 ? "graph_identity_missing" : "graph_identity_ambiguous", $"Graph selector 无法唯一绑定：{selector}");
            return null;
        }

        IdentityReference ResolveStateMachine(string selector, string path, bool required)
        {
            if (!string.IsNullOrEmpty(selector) && m_StateMachines.TryGetValue(selector, out IdentityReference reference))
                return reference;
            if (required)
                m_Report?.Error(path, "state_machine_identity_missing", $"StateMachine 无法绑定稳定 identity：{selector}");
            return default;
        }

        IdentityReference ResolveState(string stateMachine, string state, string path, bool required)
        {
            if (!string.IsNullOrEmpty(state) && m_States.TryGetValue(StateKey(stateMachine, state), out IdentityReference reference))
                return reference;
            if (required)
                m_Report?.Error(path, "state_identity_missing", $"State 无法绑定稳定 identity：{stateMachine}/{state}");
            return default;
        }

        AgentSnapshotNode FindUniqueNode(AgentSnapshotGraph graph, string selector, string typeSuffix, string path, bool required)
        {
            if (graph == null)
                return null;
            List<AgentSnapshotNode> matches = graph.nodes.Where(node =>
                node != null &&
                (string.IsNullOrEmpty(typeSuffix) || node.typeName.EndsWith(typeSuffix, StringComparison.Ordinal)) &&
                (string.IsNullOrEmpty(selector) || string.Equals(node.displayName, selector, StringComparison.Ordinal) || string.Equals(NodeRole(node), selector, StringComparison.Ordinal))).ToList();
            if (matches.Count == 1)
                return matches[0];
            if (required)
                m_Report?.Error(path, matches.Count == 0 ? "element_identity_missing" : "element_identity_ambiguous", $"Element selector 无法唯一绑定：{selector}");
            return null;
        }

        void RegisterGraphOperation(string selector, string operationId)
        {
            if (!string.IsNullOrEmpty(selector) && !string.IsNullOrEmpty(operationId))
                m_GraphOperations[selector] = operationId;
        }

        void RegisterUnique(Dictionary<string, IdentityReference> map, string key, IdentityReference value, string code)
        {
            if (string.IsNullOrEmpty(key) || !value.IsValid)
                return;
            if (map.TryGetValue(key, out IdentityReference existing) && !existing.Equals(value))
            {
                m_Report?.Error("identity-binding", code, $"语义 selector 无法唯一绑定：{key}");
                return;
            }
            map[key] = value;
        }

        static string StateKey(string stateMachine, string state) => $"{stateMachine}\n{state}";
        static string Required(string primary, string secondary) => !string.IsNullOrEmpty(primary) ? primary : secondary ?? string.Empty;

        static string ControlType(string selector)
        {
            return selector switch
            {
                "Enter" or "StateMachineEnterNode" => "StateMachineEnterNode",
                "AnyState" or "StateMachineAnyStateNode" => "StateMachineAnyStateNode",
                "Exit" or "StateMachineExitNode" => "StateMachineExitNode",
                _ => string.Empty
            };
        }

        static string NodeRole(AgentSnapshotNode node)
        {
            if (node.typeName.EndsWith("StateMachineEnterNode", StringComparison.Ordinal))
                return "Enter";
            if (node.typeName.EndsWith("StateMachineAnyStateNode", StringComparison.Ordinal))
                return "AnyState";
            if (node.typeName.EndsWith("StateMachineExitNode", StringComparison.Ordinal))
                return "Exit";
            return node.displayName;
        }

        readonly struct IdentityReference : IEquatable<IdentityReference>
        {
            public IdentityReference(string authoringId, string operationId)
            {
                AuthoringId = authoringId ?? string.Empty;
                OperationId = operationId ?? string.Empty;
            }

            public string AuthoringId { get; }
            public string OperationId { get; }
            public bool IsValid => !string.IsNullOrEmpty(AuthoringId) || !string.IsNullOrEmpty(OperationId);
            public bool Equals(IdentityReference other) => string.Equals(AuthoringId, other.AuthoringId, StringComparison.Ordinal) && string.Equals(OperationId, other.OperationId, StringComparison.Ordinal);
        }
    }
}
