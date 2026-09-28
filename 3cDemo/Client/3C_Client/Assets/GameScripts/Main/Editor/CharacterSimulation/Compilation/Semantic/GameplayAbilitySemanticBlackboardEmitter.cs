using BTSMTL.Authoring.Blackboard;
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;
using UnityEngine;
using BlackboardDeclaration = ThirdPersonCharacter.Pipeline.Simulation.Editor.GameplayAbilityAuthoringBlackboardDeclaration;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class GameplayAbilitySemanticBlackboardEmitter : IBtsmtlSkillBlackboardCompilation
    {
        readonly IReadOnlyDictionary<string, BlackboardDeclaration> m_Declarations;
        readonly GameplayAbilitySemanticBuilder m_Builder;
        readonly SimulationCompileReport m_Report;
        readonly Dictionary<string, int> m_ValueSlots = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, ScopeRecord> m_Scopes = new Dictionary<string, ScopeRecord>(StringComparer.Ordinal);
        readonly List<GraphRoute> m_GraphStack = new List<GraphRoute>();

        public GameplayAbilitySemanticBlackboardEmitter(
            IReadOnlyDictionary<string, BlackboardDeclaration> declarations,
            GameplayAbilitySemanticBuilder builder,
            SimulationCompileReport report)
        {
            m_Declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void CompileDeclarations()
        {
            foreach (BlackboardDeclaration item in m_Declarations.Values)
            {
                GameplayAbilityBlackboardDeclarationSnapshot declaration = item.Declaration;
                SimulationSourceLocation source = DeclarationSource(item, item.Route);
                string identity = DeclarationIdentity(item.GraphId, declaration.DeclarationId);
                var fields = new List<ProgramCatalogField>
                {
                    m_Builder.ConstantField(source, "Key", declaration.BlackboardKey),
                    m_Builder.ConstantField(source, "ValueType", declaration.ValueType?.FullName),
                    m_Builder.ConstantField(source, "Scope", declaration.BlackboardScope),
                    m_Builder.ConstantField(source, "Lifetime", declaration.BlackboardLifetime),
                    m_Builder.ConstantField(source, "Category", declaration.BlackboardCategoryPath),
                    m_Builder.ConstantField(source, "Default", CompileBlackboardDefault(declaration.DefaultValue, source))
                };
                if (declaration.InputBinding != null)
                    fields.Add(m_Builder.ConstantField(source, "InputValueId", declaration.InputBinding.InputValueId));
                if (declaration.FactProjection != null)
                {
                    fields.Add(m_Builder.ConstantField(
                        source,
                        "Projection",
                        CompileBlackboardFactProjection(declaration.FactProjection.Kind)));
                    fields.Add(m_Builder.ConstantField(source, "ActionWindowType", declaration.FactProjection.ActionWindowType));
                    fields.Add(m_Builder.ConstantField(source, "ActionWindowId", declaration.FactProjection.ActionWindowId));
                    fields.Add(m_Builder.ConstantField(source, "ActionWindowDigest", declaration.FactProjection.ActionWindowDigest));
                }
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.BlackboardDeclaration,
                    identity,
                    2,
                    fields.ToArray(),
                    source);
                if (declaration.BlackboardScope == PipelineBlackboardVariableScope.Character ||
                    declaration.BlackboardScope == PipelineBlackboardVariableScope.Frame)
                    EnsureDeclarationState(item, item.Route, OperationHandle.Invalid);
            }
        }

        public void EndGraph()
        {
            m_GraphStack.RemoveAt(m_GraphStack.Count - 1);
        }

        public void BeginSkillGraph(BtsmtlSkillGraphOccurrence graph, OperationHandle stateOwner)
        {
            m_GraphStack.Add(new GraphRoute(graph.GraphId, graph.Route));
            foreach (BtsmtlSkillBlackboardDeclaration declaration in ((IBtsmtlSkillFlowGraph)graph.Graph).BlackboardDeclarations)
                EnsureDeclarationState(m_Declarations[DeclarationIdentity(graph.GraphId, declaration.VariableId)], graph.Route, stateOwner);
        }

        public void CompleteGraph(string route, OperationHandle graphEntry)
        {
            if (m_Scopes.TryGetValue(ScopeIdentity(PipelineBlackboardVariableScope.Graph, route), out ScopeRecord scope))
                scope.SetOwnerOperation(graphEntry);
        }

        public void DeclareScopes()
        {
            foreach (ScopeRecord scope in m_Scopes.Values.OrderBy(value => value.Identity, StringComparer.Ordinal))
            {
                m_Builder.DeclareScope(
                    scope.Identity,
                    scope.Kind,
                    scope.OwnerIdentity,
                    scope.OwnerOperation,
                    scope.StateSlots,
                    scope.Source);
            }
        }

        public void Bind(
            OperationHandle operation,
            string route,
            string declarationOwnerId,
            string declarationId,
            Type expectedValueType,
            SimulationSourceLocation source,
            bool requireWritable = false)
        {
            string declarationIdentity = DeclarationIdentity(declarationOwnerId, declarationId);
            if (string.IsNullOrEmpty(declarationOwnerId) || string.IsNullOrEmpty(declarationId) ||
                !m_Declarations.TryGetValue(declarationIdentity, out BlackboardDeclaration declaration))
            {
                m_Report.Error("blackboard_reference_invalid", source.Identity, $"Blackboard reference '{declarationIdentity}' does not resolve.");
                return;
            }

            if (expectedValueType != null && declaration.Declaration.ValueType != expectedValueType)
            {
                m_Report.Error("blackboard_value_type_mismatch", source.Identity,
                    $"Blackboard '{declarationIdentity}' is '{declaration.Declaration.ValueType}', expected '{expectedValueType}'.");
                return;
            }
            if (requireWritable && declaration.Declaration.BlackboardLifetime == PipelineBlackboardVariableLifetime.Config)
            {
                m_Report.Error("blackboard_config_read_only", source.Identity, $"Blackboard '{declarationIdentity}' is read-only configuration.");
                return;
            }

            string stateKey = ResolveDeclarationStateKey(declaration);
            if (!m_ValueSlots.TryGetValue(stateKey, out int stateSlot))
            {
                m_Report.Error("blackboard_state_address_missing", source.Identity, $"Blackboard state address '{stateKey}' was not declared.");
                return;
            }
            m_Builder.DeclareReference(
                $"{route}/node:{source.NodeId}/blackboard-state",
                operation,
                ProgramReferenceKind.StateSlot,
                stateSlot,
                declarationIdentity,
                source);
            if (m_Builder.TryGetCatalogEntry(ProgramCatalogEntryKind.BlackboardDeclaration, declarationIdentity, out int catalog))
            {
                m_Builder.DeclareReference(
                    $"{route}/node:{source.NodeId}/blackboard-catalog",
                    operation,
                    ProgramReferenceKind.CatalogEntry,
                    catalog,
                    declarationIdentity,
                    source);
            }
        }

        static ProgramBlackboardFactProjectionKind CompileBlackboardFactProjection(
            PipelineBlackboardFactProjectionKind projection)
        {
            return projection switch
            {
                PipelineBlackboardFactProjectionKind.ActionWindow => ProgramBlackboardFactProjectionKind.ActionWindow,
                _ => throw new ArgumentOutOfRangeException(nameof(projection), projection, "Unsupported Blackboard Fact Projection.")
            };
        }

        int EnsureDeclarationState(BlackboardDeclaration item, string route, OperationHandle stateScopeOwner)
        {
            string stateKey = item.Declaration.BlackboardScope == PipelineBlackboardVariableScope.Character
                ? DeclarationIdentity(item.GraphId, item.Declaration.DeclarationId)
                : $"{route}/declaration:{item.Declaration.DeclarationId}";
            if (m_ValueSlots.TryGetValue(stateKey, out int existing))
                return existing;
            if (!TryMapValueKind(item.Declaration.ValueType, out ProgramStateValueKind valueKind))
                return -1;

            SimulationSourceLocation source = DeclarationSource(item, route);
            int value = m_Builder.DeclareStandaloneStateSlot(
                source,
                valueKind,
                ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardValue,
                stateKey,
                CompileBlackboardDefault(item.Declaration.DefaultValue, source));
            int owner = m_Builder.DeclareStandaloneStateSlot(
                source,
                ProgramStateValueKind.BlackboardOwnerToken,
                ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardOwnerToken,
                stateKey);
            int lifetime = m_Builder.DeclareStandaloneStateSlot(
                source,
                ProgramStateValueKind.Int32,
                ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardLifetime,
                stateKey,
                (int)item.Declaration.BlackboardLifetime);
            int provenance = m_Builder.DeclareStandaloneStateSlot(
                source,
                ProgramStateValueKind.BlackboardWriteStamp,
                ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardWriteStamp,
                stateKey);
            m_ValueSlots.Add(stateKey, value);

            string scopeIdentity = ScopeIdentity(item.Declaration.BlackboardScope, route);
            if (!m_Scopes.TryGetValue(scopeIdentity, out ScopeRecord scope))
            {
                ProgramScopeKind kind = MapScope(item.Declaration.BlackboardScope);
                if (kind == ProgramScopeKind.State && !stateScopeOwner.IsValid)
                    m_Report.Error("blackboard_state_owner_missing", source.Identity, "State Blackboard declaration is not inside a compiled State activation owner.");
                scope = new ScopeRecord(scopeIdentity, kind, route, kind == ProgramScopeKind.State ? stateScopeOwner : OperationHandle.Invalid, source);
                m_Scopes.Add(scopeIdentity, scope);
            }
            scope.StateSlots.Add(value);
            scope.StateSlots.Add(owner);
            scope.StateSlots.Add(lifetime);
            scope.StateSlots.Add(provenance);
            return value;
        }

        string ResolveDeclarationStateKey(BlackboardDeclaration declaration)
        {
            if (declaration.Declaration.BlackboardScope == PipelineBlackboardVariableScope.Character)
                return DeclarationIdentity(declaration.GraphId, declaration.Declaration.DeclarationId);
            if (declaration.Declaration.BlackboardScope == PipelineBlackboardVariableScope.Frame)
                return $"{declaration.Route}/declaration:{declaration.Declaration.DeclarationId}";
            for (int i = m_GraphStack.Count - 1; i >= 0; i--)
            {
                if (string.Equals(m_GraphStack[i].GraphId, declaration.GraphId, StringComparison.Ordinal))
                    return $"{m_GraphStack[i].Route}/declaration:{declaration.Declaration.DeclarationId}";
            }
            return $"{declaration.Route}/declaration:{declaration.Declaration.DeclarationId}";
        }

        static bool TryMapValueKind(Type type, out ProgramStateValueKind kind)
        {
            if (type == typeof(bool)) kind = ProgramStateValueKind.Boolean;
            else if (type == typeof(int)) kind = ProgramStateValueKind.Int32;
            else if (type == typeof(ulong)) kind = ProgramStateValueKind.UInt64;
            else if (type == typeof(float)) kind = ProgramStateValueKind.Scalar;
            else if (type == typeof(string)) kind = ProgramStateValueKind.Identity;
            else if (type == typeof(Vector2)) kind = ProgramStateValueKind.Vector2;
            else if (type == typeof(Vector3)) kind = ProgramStateValueKind.Vector3;
            else if (type == typeof(ActionTargetSnapshot)) kind = ProgramStateValueKind.ActionTargetSnapshot;
            else
            {
                kind = default;
                return false;
            }
            return true;
        }

        static object CompileBlackboardDefault(object value, SimulationSourceLocation source)
        {
            if (value is not ActionTargetSnapshot snapshot)
                return value;
            float yaw = snapshot.Rotation.eulerAngles.y;
            if (yaw >= 180f)
                yaw -= 360f;
            var writer = new SemanticDataWriter();
            writer.WriteUInt32(0x504E5354);
            writer.WriteInt32(1);
            writer.WriteString(snapshot.TargetId);
            writer.WriteNumber(snapshot.Position.x, $"{source.Identity}/TargetSnapshot.Position.x");
            writer.WriteNumber(snapshot.Position.y, $"{source.Identity}/TargetSnapshot.Position.y");
            writer.WriteNumber(snapshot.Position.z, $"{source.Identity}/TargetSnapshot.Position.z");
            writer.WriteNumber(yaw, $"{source.Identity}/TargetSnapshot.Yaw");
            return writer.Build();
        }

        static ProgramScopeKind MapScope(PipelineBlackboardVariableScope scope)
        {
            return scope switch
            {
                PipelineBlackboardVariableScope.Character => ProgramScopeKind.Character,
                PipelineBlackboardVariableScope.Graph => ProgramScopeKind.Graph,
                PipelineBlackboardVariableScope.State => ProgramScopeKind.State,
                PipelineBlackboardVariableScope.ActionInstance => ProgramScopeKind.ActionInstance,
                PipelineBlackboardVariableScope.Frame => ProgramScopeKind.Frame,
                _ => throw new ArgumentOutOfRangeException(nameof(scope))
            };
        }

        static string ScopeIdentity(PipelineBlackboardVariableScope scope, string route) =>
            scope == PipelineBlackboardVariableScope.Character ? "scope:character" : $"scope:{scope}:{route}";

        static string DeclarationIdentity(string ownerId, string declarationId) => $"blackboard:{ownerId}:{declarationId}";

        static SimulationSourceLocation DeclarationSource(BlackboardDeclaration item, string route)
        {
            return new SimulationSourceLocation(
                item.SourceType,
                item.GraphId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/blackboard:{item.Declaration.DeclarationId}",
                declarationId: item.Declaration.DeclarationId,
                contentHash: item.ContentHash);
        }

        sealed class ScopeRecord
        {
            public ScopeRecord(
                string identity,
                ProgramScopeKind kind,
                string ownerIdentity,
                OperationHandle ownerOperation,
                SimulationSourceLocation source)
            {
                Identity = identity;
                Kind = kind;
                OwnerIdentity = ownerIdentity;
                OwnerOperation = ownerOperation;
                Source = source;
            }

            public string Identity { get; }
            public ProgramScopeKind Kind { get; }
            public string OwnerIdentity { get; }
            public OperationHandle OwnerOperation { get; private set; }
            public SimulationSourceLocation Source { get; }
            public List<int> StateSlots { get; } = new List<int>();

            public void SetOwnerOperation(OperationHandle ownerOperation)
            {
                OwnerOperation = ownerOperation;
            }
        }

        readonly struct GraphRoute
        {
            public GraphRoute(string graphId, string route)
            {
                GraphId = graphId;
                Route = route;
            }

            public string GraphId { get; }
            public string Route { get; }
        }
    }
}
