using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using BlackboardDeclaration = ThirdPersonCharacter.Pipeline.Simulation.Editor.CharacterAuthoringBlackboardDeclaration;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticBlackboardEmitter
    {
        readonly IReadOnlyDictionary<string, BlackboardDeclaration> m_Declarations;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly Dictionary<string, int> m_ValueSlots = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, ScopeRecord> m_Scopes = new Dictionary<string, ScopeRecord>(StringComparer.Ordinal);
        readonly List<GraphRoute> m_GraphStack = new List<GraphRoute>();

        public CharacterSemanticBlackboardEmitter(
            IReadOnlyDictionary<string, BlackboardDeclaration> declarations,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report)
        {
            m_Declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void CompileDeclarations()
        {
            foreach (BlackboardDeclaration item in m_Declarations.Values)
            {
                BaseExposedProperty declaration = item.Declaration;
                CharacterSimulationSourceLocation source = DeclarationSource(item, item.Route);
                string identity = DeclarationIdentity(item.Graph.GraphAuthoringId, declaration.DeclarationId);
                var fields = new List<ProgramCatalogField>
                {
                    m_Builder.ConstantField(source, "Key", declaration.BlackboardKey),
                    m_Builder.ConstantField(source, "ValueType", declaration.ValueType?.FullName),
                    m_Builder.ConstantField(source, "Scope", declaration.BlackboardScope),
                    m_Builder.ConstantField(source, "Lifetime", declaration.BlackboardLifetime),
                    m_Builder.ConstantField(source, "Category", declaration.BlackboardCategoryPath),
                    m_Builder.ConstantField(source, "Default", CompileBlackboardDefault(declaration.GetValue(), source))
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

        public void BeginGraph(
            BaseTree graph,
            string route,
            IReadOnlyList<BaseExposedProperty> declarations,
            OperationHandle stateScopeOwner)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (route == null)
                throw new ArgumentNullException(nameof(route));
            if (declarations == null)
                throw new ArgumentNullException(nameof(declarations));
            m_GraphStack.Add(new GraphRoute(graph.GraphAuthoringId, route));
            for (int i = 0; i < declarations.Count; i++)
            {
                BaseExposedProperty declaration = declarations[i];
                BlackboardDeclaration item = m_Declarations[DeclarationIdentity(graph.GraphAuthoringId, declaration.DeclarationId)];
                EnsureDeclarationState(item, route, stateScopeOwner);
            }
        }

        public void EndGraph()
        {
            m_GraphStack.RemoveAt(m_GraphStack.Count - 1);
        }

        public void CompleteGraph(string route, OperationHandle graphEntry)
        {
            if (m_Scopes.TryGetValue(ScopeIdentity(PipelineBlackboardVariableScope.Graph, route), out ScopeRecord scope))
                scope.SetOwnerOperation(graphEntry);
        }

        public bool TryGetValueSlot(
            BaseTree graph,
            string route,
            string declarationId,
            out int slot)
        {
            slot = -1;
            if (graph == null || string.IsNullOrEmpty(route) || string.IsNullOrEmpty(declarationId))
                return false;
            BaseExposedProperty declaration = graph.ExposedProperties
                .FirstOrDefault(value => value != null && string.Equals(value.DeclarationId, declarationId, StringComparison.Ordinal));
            if (declaration == null)
                return false;
            string key = declaration.BlackboardScope == PipelineBlackboardVariableScope.Character
                ? DeclarationIdentity(graph.GraphAuthoringId, declaration.DeclarationId)
                : $"{route}/declaration:{declaration.DeclarationId}";
            return m_ValueSlots.TryGetValue(key, out slot);
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
            PipelineBlackboardVariableReference reference,
            CharacterSimulationSourceLocation source)
        {
            string declarationIdentity = DeclarationIdentity(reference.DeclarationOwnerId, reference.DeclarationId);
            if (!reference.IsValid || !m_Declarations.TryGetValue(declarationIdentity, out BlackboardDeclaration declaration))
            {
                m_Report.Error("blackboard_reference_invalid", source.Identity, $"Blackboard reference '{declarationIdentity}' does not resolve.");
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
                ? DeclarationIdentity(item.Graph.GraphAuthoringId, item.Declaration.DeclarationId)
                : $"{route}/declaration:{item.Declaration.DeclarationId}";
            if (m_ValueSlots.TryGetValue(stateKey, out int existing))
                return existing;
            if (!TryMapValueKind(item.Declaration.ValueType, out ProgramStateValueKind valueKind))
                return -1;

            CharacterSimulationSourceLocation source = DeclarationSource(item, route);
            int value = m_Builder.DeclareStandaloneStateSlot(
                source,
                valueKind,
                ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardValue,
                stateKey,
                CompileBlackboardDefault(item.Declaration.GetValue(), source));
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
                return DeclarationIdentity(declaration.Graph.GraphAuthoringId, declaration.Declaration.DeclarationId);
            if (declaration.Declaration.BlackboardScope == PipelineBlackboardVariableScope.Frame)
                return $"{declaration.Route}/declaration:{declaration.Declaration.DeclarationId}";
            for (int i = m_GraphStack.Count - 1; i >= 0; i--)
            {
                if (string.Equals(m_GraphStack[i].GraphId, declaration.Graph.GraphAuthoringId, StringComparison.Ordinal))
                    return $"{m_GraphStack[i].Route}/declaration:{declaration.Declaration.DeclarationId}";
            }
            return $"{declaration.Route}/declaration:{declaration.Declaration.DeclarationId}";
        }

        static bool TryMapValueKind(Type type, out ProgramStateValueKind kind)
        {
            if (type == typeof(bool)) kind = ProgramStateValueKind.Boolean;
            else if (type == typeof(int)) kind = ProgramStateValueKind.Int32;
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

        static object CompileBlackboardDefault(object value, CharacterSimulationSourceLocation source)
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

        static CharacterSimulationSourceLocation DeclarationSource(BlackboardDeclaration item, string route)
        {
            return new CharacterSimulationSourceLocation(
                item.Declaration.GetType().FullName,
                item.Graph.GraphAuthoringId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/blackboard:{item.Declaration.DeclarationId}",
                declarationId: item.Declaration.DeclarationId,
                contentHash: GraphAuthoringFingerprint.Compute(item.Graph));
        }

        sealed class ScopeRecord
        {
            public ScopeRecord(
                string identity,
                ProgramScopeKind kind,
                string ownerIdentity,
                OperationHandle ownerOperation,
                CharacterSimulationSourceLocation source)
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
            public CharacterSimulationSourceLocation Source { get; }
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
