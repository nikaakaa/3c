using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using BlackboardDeclaration = ThirdPersonCharacter.Pipeline.Simulation.Editor.CharacterAuthoringBlackboardDeclaration;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSemanticEmitter
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationNodeEmitterRegistry m_NodeEmitters;
        readonly CharacterSimulationTimelineEmitterRegistry m_TimelineEmitters;
        readonly CharacterSimulationCatalogIndex m_CatalogIndex;
        readonly Dictionary<string, int> m_BlackboardValueSlots = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, ScopeRecord> m_Scopes = new Dictionary<string, ScopeRecord>(StringComparer.Ordinal);
        readonly Dictionary<string, Dictionary<string, OperationHandle>> m_CompiledGraphOperations = new Dictionary<string, Dictionary<string, OperationHandle>>(StringComparer.Ordinal);
        readonly List<GraphRoute> m_CompileStack = new List<GraphRoute>();

        public CharacterSemanticEmitter(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationCatalogIndex catalogIndex)
        {
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_CatalogIndex = catalogIndex ?? throw new ArgumentNullException(nameof(catalogIndex));
            m_NodeEmitters = model.NodeEmitters;
            m_TimelineEmitters = model.TimelineEmitters;
        }

        public OperationHandle Emit()
        {
            CompileDeclarationCatalogs();
            OperationHandle entry = CompileGraph(m_Model.Root, OperationHandle.Invalid);
            foreach (ScopeRecord scope in m_Scopes.Values.OrderBy(value => value.Identity, StringComparer.Ordinal))
                m_Builder.DeclareScope(scope.Identity, scope.Kind, scope.OwnerIdentity, scope.OwnerOperation, scope.StateSlots, scope.Source);
            return entry;
        }

        void CompileDeclarationCatalogs()
        {
            foreach (BlackboardDeclaration item in m_Model.Declarations.Values)
            {
                BaseExposedProperty declaration = item.Declaration;
                CharacterSimulationSourceLocation source = DeclarationSource(item);
                string identity = DeclarationIdentity(item.Graph.GraphAuthoringId, declaration.DeclarationId);
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.BlackboardDeclaration,
                    identity,
                    2,
                    Fields(
                        m_Builder.ConstantField(source, "Key", declaration.BlackboardKey),
                        m_Builder.ConstantField(source, "ValueType", declaration.ValueType?.FullName),
                        m_Builder.ConstantField(source, "Scope", declaration.BlackboardScope),
                        m_Builder.ConstantField(source, "Lifetime", declaration.BlackboardLifetime),
                        m_Builder.ConstantField(source, "Authority", declaration.BlackboardAuthority),
                        m_Builder.ConstantField(source, "SyncPolicy", declaration.BlackboardSyncPolicy),
                        m_Builder.ConstantField(source, "InputValueId", declaration.InputValueId),
                        m_Builder.ConstantField(source, "Projection", declaration.BlackboardFactProjection),
                        m_Builder.ConstantField(source, "ActionWindowType", declaration.ActionWindowType),
                        m_Builder.ConstantField(source, "ActionWindowId", declaration.ActionWindowId),
                        m_Builder.ConstantField(source, "ActionWindowDigest", declaration.ActionWindowDigest),
                        m_Builder.ConstantField(source, "Category", declaration.BlackboardCategoryPath),
                        m_Builder.ConstantField(source, "Default", CompileBlackboardDefault(declaration.GetValue(), source))),
                    source);
                if (declaration.BlackboardScope == PipelineBlackboardVariableScope.Character)
                    EnsureDeclarationState(item, item.Route);
            }
        }

        OperationHandle CompileGraph(CharacterAuthoringGraphOccurrence occurrence, OperationHandle stateScopeOwner)
        {
            if (occurrence == null)
                throw new ArgumentNullException(nameof(occurrence));
            BaseTree graph = occurrence.Graph;
            string route = occurrence.Route;
            m_CompileStack.Add(new GraphRoute(graph.GraphAuthoringId, route, stateScopeOwner));
            try
            {
                foreach (BaseExposedProperty declaration in occurrence.Declarations)
                {
                    BlackboardDeclaration item = m_Model.Declarations[DeclarationIdentity(graph.GraphAuthoringId, declaration.DeclarationId)];
                    EnsureDeclarationState(item, route);
                }

                var operations = new Dictionary<string, OperationHandle>(StringComparer.Ordinal);
                foreach (BaseNode node in occurrence.Nodes)
                {
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
                    BindNodeCatalogs(node, operation, route);
                }
                m_CompiledGraphOperations[route] = operations;

                foreach (CharacterAuthoringEdgeRecord edge in occurrence.Edges)
                    CompileEdge(occurrence, edge, operations, stateScopeOwner);
                foreach (CharacterAuthoringEdgeRecord edge in occurrence.PropertyEdges)
                    CompileEdge(occurrence, edge, operations, stateScopeOwner);

                foreach (CharacterAuthoringTimelineRecord timeline in occurrence.Timelines)
                {
                    if (!operations.TryGetValue(timeline.Node.GUID, out OperationHandle owner))
                        throw new InvalidOperationException($"Discovered Timeline Node '{timeline.Node.GUID}' has no emitted operation.");
                    CompileTimeline(timeline, owner, stateScopeOwner);
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
                    if (reference.Owner is StateNode && reference.Child.Graph is StateBehaviorSubTree stateBehavior)
                    {
                        DeclareStateBehaviorControlFlow(graph, reference.Owner, owner, stateBehavior, reference.Route, entry);
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
                        NodeSource(graph, reference.Owner, route));
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
                            NodeSource(graph, reference.Owner, route));
                    }
                }
                OperationHandle graphEntry = FindEntry(occurrence, operations);
                foreach (ScopeRecord scope in m_Scopes.Values)
                {
                    if (scope.Kind == ProgramScopeKind.Graph && string.Equals(scope.OwnerIdentity, route, StringComparison.Ordinal))
                        scope.SetOwnerOperation(graphEntry);
                }
                return graphEntry;
            }
            finally
            {
                m_CompileStack.RemoveAt(m_CompileStack.Count - 1);
            }
        }

        void DeclareStateBehaviorControlFlow(
            BaseTree graph,
            BaseNode node,
            OperationHandle owner,
            StateBehaviorSubTree stateBehavior,
            string childRoute,
            OperationHandle root)
        {
            CharacterSimulationSourceLocation source = NodeSource(graph, node, childRoute);
            if (!TryGetCompiledOperation(childRoute, stateBehavior.OnEnterGUID, out OperationHandle onEnter) ||
                !TryGetCompiledOperation(childRoute, stateBehavior.OnExitGUID, out OperationHandle onExit))
            {
                m_Report.Error("state_lifecycle_operation_missing", childRoute, "State behavior requires compiled OnEnter and OnExit operations.");
                return;
            }
            m_Builder.DeclareControlFlow(
                $"{childRoute}/state-on-enter",
                owner,
                onEnter,
                "OnEnter",
                "Entry",
                ProgramControlFlowKind.Enter,
                0,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
            m_Builder.DeclareControlFlow(
                $"{childRoute}/state-root",
                owner,
                root,
                "Root",
                "Entry",
                ProgramControlFlowKind.Enter,
                1,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
            m_Builder.DeclareControlFlow(
                $"{childRoute}/state-on-exit",
                owner,
                onExit,
                "OnExit",
                "Entry",
                ProgramControlFlowKind.Exit,
                2,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
        }

        bool TryGetCompiledOperation(string route, string nodeId, out OperationHandle operation)
        {
            operation = OperationHandle.Invalid;
            return !string.IsNullOrEmpty(nodeId) &&
                   m_CompiledGraphOperations.TryGetValue(route, out Dictionary<string, OperationHandle> operations) &&
                   operations.TryGetValue(nodeId, out operation);
        }

        void CompileEdge(
            CharacterAuthoringGraphOccurrence occurrence,
            CharacterAuthoringEdgeRecord record,
            Dictionary<string, OperationHandle> operations,
            OperationHandle stateScopeOwner)
        {
            BaseTree graph = occurrence.Graph;
            BaseEdge edge = record.Edge;
            string edgeRoute = record.Route;
            if (!operations.TryGetValue(edge.StartNodeGUID, out OperationHandle source) || !operations.TryGetValue(edge.EndNodeGUID, out OperationHandle target))
                throw new InvalidOperationException($"Discovered Edge '{edge.GUID}' has an operation endpoint mismatch.");
            bool hasCondition = false;
            OperationHandle condition = OperationHandle.Invalid;
            if (record.ConditionGraph != null)
            {
                OperationHandle conditionStateOwner = occurrence.Nodes.Any(value => value is StateNode && value.GUID == edge.StartNodeGUID)
                    ? source
                    : stateScopeOwner;
                condition = CompileGraph(record.ConditionGraph, conditionStateOwner);
                hasCondition = condition.IsValid;
            }
            ProgramControlFlowKind kind = edge is PropertyEdge
                ? ProgramControlFlowKind.Value
                : graph is StateMachineGraph stateMachine && stateMachine.IsTransitionEdge(edge)
                    ? ProgramControlFlowKind.Transition
                    : ProgramControlFlowKind.Child;
            m_Builder.DeclareControlFlow(
                edgeRoute,
                source,
                target,
                edge.StartPortName,
                edge.EndPortName,
                kind,
                edge.FlowOrder,
                edge.TransitionPriority,
                (ProgramAbortPolicy)(int)edge.AbortPolicy,
                hasCondition,
                condition,
                new CharacterSimulationSourceLocation(
                    edge.GetType().FullName,
                    graph.GraphAuthoringId,
                    string.Empty,
                    edge.GUID,
                    string.Empty,
                    string.Empty,
                    edgeRoute));
        }

        void CompileTimeline(CharacterAuthoringTimelineRecord record, OperationHandle owner, OperationHandle stateScopeOwner)
        {
            TimelineNode node = record.Node;
            TimelineData timeline = record.Timeline;
            string route = record.Route;
            string ownerGraphId = node.Owner?.GraphAuthoringId ?? string.Empty;
            CharacterSimulationSourceLocation timelineSource = new CharacterSimulationSourceLocation(
                timeline.GetType().FullName,
                ownerGraphId,
                node.GUID,
                string.Empty,
                timeline.AuthoringId,
                string.Empty,
                route);
            int timelineCatalog = m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.Timeline,
                $"timeline:{timeline.AuthoringId}",
                1,
                Fields(
                    m_Builder.ConstantField(timelineSource, "Name", timeline.Name),
                    m_Builder.ConstantField(timelineSource, "Scale", timeline.Scale),
                    m_Builder.ConstantField(timelineSource, "MaxFrame", timeline.MaxFrame),
                    m_Builder.ConstantField(timelineSource, "FrameRate", TimelineUtility.FrameRate)),
                timelineSource);
            if (timelineCatalog >= 0)
            {
                m_Builder.DeclareReference(
                    $"{record.GraphRoute}/node:{node.GUID}/timeline-catalog",
                    owner,
                    ProgramReferenceKind.CatalogEntry,
                    timelineCatalog,
                    $"timeline:{timeline.AuthoringId}",
                    NodeSource(node.Owner as BaseTree, node, record.GraphRoute));
            }

            var timelineEmission = new CharacterSimulationTimelineEmissionSession(timeline, m_Builder);

            foreach (CharacterAuthoringTrackRecord trackRecord in record.Tracks)
            {
                Track track = trackRecord.Track;
                var context = new CharacterSimulationTimelineEmitterContext(
                    timeline,
                    track,
                    trackRecord.AuthoringIndex,
                    ownerGraphId,
                    node.GUID,
                    route,
                    m_Builder,
                    owner,
                    CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext),
                    timelineEmission);
                if (m_TimelineEmitters.TryGetTrack(track.GetType(), out ICharacterSimulationTimelineTrackEmitter trackEmitter))
                    trackEmitter.Emit(track, context);
                else
                    throw new InvalidOperationException($"Discovered Track '{track.AuthoringId}' has no emitter.");
                foreach (CharacterAuthoringClipRecord clipRecord in trackRecord.Clips)
                {
                    Clip clip = clipRecord.Clip;
                    if (!m_TimelineEmitters.TryGetClip(clip.GetType(), out ICharacterSimulationTimelineClipEmitter clipEmitter))
                        throw new InvalidOperationException($"Discovered Clip '{clip.AuthoringId}' has no emitter.");
                    OperationHandle clipOperation;
                    try
                    {
                        clipOperation = clipEmitter.Emit(clip, context);
                    }
                    catch (Exception exception)
                    {
                        m_Report.EmissionError("timeline_clip_emit_failed", context.ClipSource(clip).Identity, exception.Message);
                        continue;
                    }
                    m_Builder.DeclareControlFlow(
                        $"{context.ClipSource(clip).Identity}/segment",
                        owner,
                        clipOperation,
                        track.AuthoringId,
                        clip.AuthoringId,
                        ProgramControlFlowKind.Child,
                        clipRecord.AuthoringIndex,
                        0,
                        ProgramAbortPolicy.None,
                        false,
                        OperationHandle.Invalid,
                        context.ClipSource(clip));
                    if (clip is not TreeClip || clipRecord.TreeGraph == null)
                        continue;
                    string treeRoute = clipRecord.TreeGraph.Route;
                    OperationHandle treeEntry = CompileGraph(clipRecord.TreeGraph, stateScopeOwner);
                    if (!treeEntry.IsValid)
                        continue;
                    m_Builder.DeclareControlFlow(
                        $"{treeRoute}/entry",
                        clipOperation,
                        treeEntry,
                        "TreeClip",
                        "Entry",
                        ProgramControlFlowKind.Enter,
                        0,
                        0,
                        ProgramAbortPolicy.None,
                        false,
                        OperationHandle.Invalid,
                        context.ClipSource(clip));
                    DeclareTreeClipLifecycle((TimelineRunningTree)clipRecord.TreeGraph.Graph, clipOperation, treeRoute, context.ClipSource(clip));
                }
            }
            timelineEmission.Complete();
        }

        void DeclareTreeClipLifecycle(
            TimelineRunningTree tree,
            OperationHandle clipOperation,
            string treeRoute,
            CharacterSimulationSourceLocation source)
        {
            DeclareTreeClipLifecycleEdge(tree.OnEnableGUID, "OnEnable", ProgramControlFlowKind.Enter, 1);
            DeclareTreeClipLifecycleEdge(tree.OnDisableGUID, "OnDisable", ProgramControlFlowKind.Exit, 0);
            DeclareTreeClipLifecycleEdge(tree.OnDestroyGUID, "OnDestroy", ProgramControlFlowKind.Exit, 1);

            void DeclareTreeClipLifecycleEdge(string nodeId, string port, ProgramControlFlowKind kind, int order)
            {
                if (!TryGetCompiledOperation(treeRoute, nodeId, out OperationHandle target))
                {
                    m_Report.Error("tree_clip_lifecycle_missing", treeRoute, $"TreeClip graph is missing compiled '{port}' lifecycle operation.");
                    return;
                }
                m_Builder.DeclareControlFlow(
                    $"{treeRoute}/{port}",
                    clipOperation,
                    target,
                    port,
                    "Entry",
                    kind,
                    order,
                    0,
                    ProgramAbortPolicy.None,
                    false,
                    OperationHandle.Invalid,
                    source);
            }
        }

        void BindNodeCatalogs(BaseNode node, OperationHandle operation, string route)
        {
            if (TryGetBlackboardReference(node, out PipelineBlackboardVariableReference blackboard))
            {
                BindBlackboard(node, operation, route, blackboard);
                return;
            }
            if (node is CharacterInputValueInfoNode input)
            {
                BindCatalog(node, operation, route, ProgramCatalogEntryKind.InputValue, $"input:value:{input.InputValueId}", m_CatalogIndex.InputValues.Contains(input.InputValueId));
                return;
            }
            if (node is CharacterActionRequestInfoNode request)
            {
                BindCatalog(node, operation, route, ProgramCatalogEntryKind.InputRequest, $"input:request:{request.RequestId}", m_CatalogIndex.InputRequests.Contains(request.RequestId));
                return;
            }
            if (node is ActivateActionInstanceNode activate)
            {
                string actionId = activate.ActionProfile ? activate.ActionProfile.ActionId : string.Empty;
                BindCatalog(node, operation, route, ProgramCatalogEntryKind.Action, $"action:{actionId}", m_CatalogIndex.Actions.Contains(actionId));
                if (!string.IsNullOrEmpty(activate.SourceInputRequestId))
                    BindCatalog(node, operation, route, ProgramCatalogEntryKind.InputRequest, $"input:request:{activate.SourceInputRequestId}", m_CatalogIndex.InputRequests.Contains(activate.SourceInputRequestId), "source-request");
                if (!activate.ActionContext)
                    m_Report.Error("action_context_missing", $"{route}/node:{node.GUID}", "Action activation requires a formal Action Context asset.");
                if (activate.TargetSnapshotVariable.IsValid)
                    BindBlackboard(node, operation, route, activate.TargetSnapshotVariable);
                return;
            }
            if (node is CanActivateActionInfoNode admission)
            {
                string actionId = admission.ActionProfile ? admission.ActionProfile.ActionId : string.Empty;
                BindCatalog(node, operation, route, ProgramCatalogEntryKind.Action, $"action:{actionId}", m_CatalogIndex.Actions.Contains(actionId));
                if (admission.TargetSnapshotVariable.IsValid)
                    BindBlackboard(node, operation, route, admission.TargetSnapshotVariable);
                return;
            }
            if (node is HasGameplayTagNode hasTag)
            {
                BindCatalog(node, operation, route, ProgramCatalogEntryKind.GameplayTag, $"tag:{hasTag.Tag.Value}", m_CatalogIndex.GameplayTags.Contains(hasTag.Tag.Value));
                return;
            }
            if (node is MatchGameplayTagQueryNode matchTags)
            {
                BindTagQuery(node, operation, route, matchTags.Query);
                return;
            }
            if (node is ReadGameplayAttributeNode readAttribute)
            {
                BindCatalog(node, operation, route, ProgramCatalogEntryKind.Attribute, $"attribute:{readAttribute.Attribute.Value}", m_CatalogIndex.Attributes.Contains(readAttribute.Attribute.Value));
                return;
            }
            if (node is ApplyGameplayEffectNode applyEffect)
            {
                string effectId = applyEffect.Effect ? applyEffect.Effect.EffectId.Value : string.Empty;
                BindCatalog(node, operation, route, ProgramCatalogEntryKind.GameplayEffect, $"effect:{effectId}", m_CatalogIndex.GameplayEffects.Contains(effectId));
                if (applyEffect.Predicted && !applyEffect.ActionContext)
                    m_Report.Error("gameplay_effect_prediction_context_missing", $"{route}/node:{node.GUID}", "Predicted Gameplay Effect application requires a formal Action Context.");
                return;
            }
            if (node is RemoveGameplayEffectNode removeEffect)
            {
                if (removeEffect.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectId)
                {
                    string effectId = removeEffect.Effect ? removeEffect.Effect.EffectId.Value : string.Empty;
                    BindCatalog(node, operation, route, ProgramCatalogEntryKind.GameplayEffect, $"effect:{effectId}", m_CatalogIndex.GameplayEffects.Contains(effectId));
                }
                else if (removeEffect.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectTagQuery)
                {
                    BindTagQuery(node, operation, route, removeEffect.EffectTagQuery);
                }
            }
        }

        void BindTagQuery(BaseNode node, OperationHandle operation, string route, ThirdPersonGameplay.Tags.GameplayTagQuery query)
        {
            if (query == null)
            {
                m_Report.Error("gameplay_tag_query_missing", $"{route}/node:{node.GUID}", "Gameplay Tag query is missing.");
                return;
            }
            int suffix = 0;
            Bind(query.All);
            Bind(query.Any);
            Bind(query.None);

            void Bind(IReadOnlyList<ThirdPersonGameplay.Tags.GameplayTagId> values)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    string tagId = values[i].Value;
                    BindCatalog(node, operation, route, ProgramCatalogEntryKind.GameplayTag, $"tag:{tagId}", m_CatalogIndex.GameplayTags.Contains(tagId), $"tag-{suffix++:D4}");
                }
            }
        }

        void BindBlackboard(BaseNode node, OperationHandle operation, string route, PipelineBlackboardVariableReference reference)
        {
            string declarationIdentity = DeclarationIdentity(reference.DeclarationOwnerId, reference.DeclarationId);
            if (!reference.IsValid || !m_Model.Declarations.TryGetValue(declarationIdentity, out BlackboardDeclaration declaration))
            {
                m_Report.Error("blackboard_reference_invalid", $"{route}/node:{node.GUID}", $"Blackboard reference '{declarationIdentity}' does not resolve.");
                return;
            }
            string stateKey = ResolveDeclarationStateKey(declaration, route);
            if (!m_BlackboardValueSlots.TryGetValue(stateKey, out int stateSlot))
            {
                m_Report.Error("blackboard_state_address_missing", $"{route}/node:{node.GUID}", $"Blackboard state address '{stateKey}' was not declared.");
                return;
            }
            CharacterSimulationSourceLocation source = NodeSource(node.Owner as BaseTree, node, route);
            m_Builder.DeclareReference(
                $"{route}/node:{node.GUID}/blackboard-state",
                operation,
                ProgramReferenceKind.StateSlot,
                stateSlot,
                declarationIdentity,
                source);
            if (m_Builder.TryGetCatalogEntry(ProgramCatalogEntryKind.BlackboardDeclaration, declarationIdentity, out int catalog))
            {
                m_Builder.DeclareReference(
                    $"{route}/node:{node.GUID}/blackboard-catalog",
                    operation,
                    ProgramReferenceKind.CatalogEntry,
                    catalog,
                    declarationIdentity,
                    source);
            }
        }

        void BindCatalog(BaseNode node, OperationHandle operation, string route, ProgramCatalogEntryKind kind, string identity, bool known, string suffix = "catalog")
        {
            CharacterSimulationSourceLocation source = NodeSource(node.Owner as BaseTree, node, route);
            if (!known || !m_Builder.TryGetCatalogEntry(kind, identity, out int catalog))
            {
                m_Report.Error("catalog_reference_invalid", source.Identity, $"Node references unknown catalog entry '{identity}'.");
                return;
            }
            m_Builder.DeclareReference($"{route}/node:{node.GUID}/{suffix}", operation, ProgramReferenceKind.CatalogEntry, catalog, identity, source);
        }

        int EnsureDeclarationState(BlackboardDeclaration item, string route)
        {
            string stateKey = item.Declaration.BlackboardScope == PipelineBlackboardVariableScope.Character
                ? DeclarationIdentity(item.Graph.GraphAuthoringId, item.Declaration.DeclarationId)
                : $"{route}/declaration:{item.Declaration.DeclarationId}";
            if (m_BlackboardValueSlots.TryGetValue(stateKey, out int existing))
                return existing;
            if (!TryMapValueKind(item.Declaration.ValueType, out ProgramStateValueKind valueKind))
                return -1;
            CharacterSimulationSourceLocation source = new CharacterSimulationSourceLocation(
                item.Declaration.GetType().FullName,
                item.Graph.GraphAuthoringId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/blackboard:{item.Declaration.DeclarationId}");
            int value = m_Builder.DeclareStandaloneStateSlot(
                source,
                valueKind,
                ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardValue,
                stateKey,
                CompileBlackboardDefault(item.Declaration.GetValue(), source));
            int owner = m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.BlackboardOwnerToken, ProgramStateOwnerKind.Blackboard, ProgramStateSemantic.BlackboardOwnerToken, stateKey);
            int lifetime = m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Blackboard, ProgramStateSemantic.BlackboardLifetime, stateKey, (int)item.Declaration.BlackboardLifetime);
            int provenance = m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.BlackboardWriteStamp, ProgramStateOwnerKind.Blackboard, ProgramStateSemantic.BlackboardWriteStamp, stateKey);
            m_BlackboardValueSlots.Add(stateKey, value);
            string scopeIdentity = ScopeIdentity(item.Declaration.BlackboardScope, route);
            if (!m_Scopes.TryGetValue(scopeIdentity, out ScopeRecord scope))
            {
                ProgramScopeKind kind = MapScope(item.Declaration.BlackboardScope);
                OperationHandle ownerOperation = kind == ProgramScopeKind.State && m_CompileStack.Count > 0
                    ? m_CompileStack[m_CompileStack.Count - 1].StateScopeOwner
                    : OperationHandle.Invalid;
                if (kind == ProgramScopeKind.State && !ownerOperation.IsValid)
                    m_Report.Error("blackboard_state_owner_missing", source.Identity, "State Blackboard declaration is not inside a compiled State activation owner.");
                scope = new ScopeRecord(scopeIdentity, kind, route, ownerOperation, source);
                m_Scopes.Add(scopeIdentity, scope);
            }
            scope.StateSlots.Add(value);
            scope.StateSlots.Add(owner);
            scope.StateSlots.Add(lifetime);
            scope.StateSlots.Add(provenance);
            return value;
        }

        string ResolveDeclarationStateKey(BlackboardDeclaration declaration, string accessRoute)
        {
            if (declaration.Declaration.BlackboardScope == PipelineBlackboardVariableScope.Character)
                return DeclarationIdentity(declaration.Graph.GraphAuthoringId, declaration.Declaration.DeclarationId);
            for (int i = m_CompileStack.Count - 1; i >= 0; i--)
            {
                if (string.Equals(m_CompileStack[i].GraphId, declaration.Graph.GraphAuthoringId, StringComparison.Ordinal))
                    return $"{m_CompileStack[i].Route}/declaration:{declaration.Declaration.DeclarationId}";
            }
            return $"{accessRoute}/declaration:{declaration.Declaration.DeclarationId}";
        }

        OperationHandle FindEntry(CharacterAuthoringGraphOccurrence occurrence, Dictionary<string, OperationHandle> operations)
        {
            if (!string.IsNullOrEmpty(occurrence.EntryNodeId) && operations.TryGetValue(occurrence.EntryNodeId, out OperationHandle operation))
                return operation;
            m_Report.EmissionError("graph_entry_missing", occurrence.Route, $"Discovered entry Node '{occurrence.EntryNodeId}' was not emitted.");
            return OperationHandle.Invalid;
        }

        static bool TryGetBlackboardReference(BaseNode node, out PipelineBlackboardVariableReference reference)
        {
            if (node is ExposedPropertyNode exposed)
            {
                reference = exposed.BlackboardVariable;
                return true;
            }
            if (node is PipelineBlackboardValueInfoNode value)
            {
                reference = value.BlackboardVariable;
                return true;
            }
            reference = default;
            return false;
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

        object CompileBlackboardDefault(object value, CharacterSimulationSourceLocation source)
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

        static string ScopeIdentity(PipelineBlackboardVariableScope scope, string route) => scope == PipelineBlackboardVariableScope.Character ? "scope:character" : $"scope:{scope}:{route}";
        static string DeclarationIdentity(string ownerId, string declarationId) => $"blackboard:{ownerId}:{declarationId}";
        static CharacterSimulationSourceLocation NodeSource(BaseTree graph, BaseNode node, string route)
        {
            return new CharacterSimulationSourceLocation(
                node.GetType().FullName,
                graph?.GraphAuthoringId ?? node.Owner?.GraphAuthoringId ?? string.Empty,
                node.GUID,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/node:{node.GUID}");
        }

        static CharacterSimulationSourceLocation DeclarationSource(BlackboardDeclaration item)
        {
            return new CharacterSimulationSourceLocation(
                item.Declaration.GetType().FullName,
                item.Graph.GraphAuthoringId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{item.Route}/blackboard:{item.Declaration.DeclarationId}",
                declarationId: item.Declaration.DeclarationId);
        }

        static ProgramCatalogField[] Fields(params ProgramCatalogField[] values) => values.Where(value => value != null).ToArray();

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
                if (!ownerOperation.IsValid)
                    throw new ArgumentException("Scope owner operation is invalid.", nameof(ownerOperation));
                if (OwnerOperation.IsValid && !OwnerOperation.Equals(ownerOperation))
                    throw new InvalidOperationException($"Scope '{Identity}' has multiple owner operations.");
                OwnerOperation = ownerOperation;
            }
        }

        readonly struct GraphRoute
        {
            public GraphRoute(string graphId, string route, OperationHandle stateScopeOwner)
            {
                GraphId = graphId;
                Route = route;
                StateScopeOwner = stateScopeOwner;
            }
            public string GraphId { get; }
            public string Route { get; }
            public OperationHandle StateScopeOwner { get; }
        }
    }
}
