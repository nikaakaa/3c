using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class BtsmtlGameplayStatePayload : IGraphAuthoringStatePayload
    {
        public static BtsmtlGameplayStatePayload Instance { get; } = new BtsmtlGameplayStatePayload();

        BtsmtlGameplayStatePayload()
        {
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Gameplay;
    }

    public sealed class CharacterPoseStatePayload : IGraphAuthoringStatePayload
    {
        public CharacterPoseStatePayload(CharacterPoseStateDefinition state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            AlwaysResetOnEntry = state.AlwaysResetOnEntry;
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Pose;
        public bool AlwaysResetOnEntry { get; }
    }

    public enum CharacterPoseTransitionReadinessRequirement : byte
    {
        TargetPoseSourceReady = 1
    }

    public sealed class BtsmtlGameplayTransitionPayload : IGraphAuthoringTransitionPayload
    {
        public BtsmtlGameplayTransitionPayload(BaseEdge edge)
        {
            if (edge == null)
                throw new ArgumentNullException(nameof(edge));
            Interruption = edge.AbortPolicy;
            ConditionOwnership = edge.ConditionRuleGraphOwnership;
            ConditionStatus = edge.ConditionRuleGraphReferenceStatus;
            ConditionRuleId = edge.ConditionRuleGraph?.GraphAuthoringId ?? string.Empty;
            SharedConditionRuleAsset = edge.SharedConditionRuleGraphAsset;
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Gameplay;
        public BTAbortPolicy Interruption { get; }
        public ConditionRuleGraphOwnership ConditionOwnership { get; }
        public ConditionRuleGraphReferenceStatus ConditionStatus { get; }
        public string ConditionRuleId { get; }
        public BaseTreeAsset SharedConditionRuleAsset { get; }
    }

    public sealed class BtsmtlGameplayTransitionCreation :
        IGraphAuthoringTransitionPayload
    {
        public BtsmtlGameplayTransitionCreation(
            int priority = 0,
            BTAbortPolicy interruption = BTAbortPolicy.None)
        {
            Priority = priority >= 0
                ? priority
                : throw new ArgumentOutOfRangeException(nameof(priority));
            Interruption = interruption;
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Gameplay;
        public int Priority { get; }
        public BTAbortPolicy Interruption { get; }
    }

    public sealed class CharacterPoseTransitionPayload : IGraphAuthoringTransitionPayload
    {
        public CharacterPoseTransitionPayload(CharacterPoseStateTransition transition)
        {
            if (transition == null)
                throw new ArgumentNullException(nameof(transition));
            Rule = transition.Rule ?? throw new InvalidOperationException(
                $"Pose Transition '{transition.TransitionId}' has no Pose rule.");
            BlendLogic = transition.BlendLogic;
            DurationSeconds = transition.DurationSeconds;
            BlendMode = transition.BlendMode;
            CustomBlendCurve = transition.CustomBlendCurve;
            BlendProfile = transition.BlendProfile;
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Pose;
        public CharacterPoseTransitionRuleGraph Rule { get; }
        public AnimationTransitionBlendLogic BlendLogic { get; }
        public float DurationSeconds { get; }
        public CharacterAnimationBlendMode BlendMode { get; }
        public CharacterAnimationBlendCurveAsset CustomBlendCurve { get; }
        public CharacterAnimationBlendProfile BlendProfile { get; }
        public CharacterPoseTransitionReadinessRequirement Readiness =>
            CharacterPoseTransitionReadinessRequirement.TargetPoseSourceReady;
    }

    public static class BtsmtlStateMachineAuthoringCapabilities
    {
        public static readonly GraphAuthoringCapabilityId Transition =
            new GraphAuthoringCapabilityId("btsmtl.state-machine.transition");

        static bool s_Registered;

        public static void EnsureRegistered()
        {
            _ = new BtsmtlGraphAuthoringCapabilities();
            if (s_Registered)
                return;
            GraphAuthoringCapabilityRegistrationRoot.RegisterDomain(
                "btsmtl.state-machine",
                catalog => catalog.Register(new GraphAuthoringCapabilityDescriptor(
                    Transition,
                    BtsmtlGraphAuthoringCapabilities.SharedDomain,
                    new[]
                    {
                        BtsmtlGraphAuthoringCapabilities.SharedRoleId("StateMachineGraph")
                    },
                    "Transition",
                    "State Machine",
                    new Color32(74, 91, 126, 255),
                    new[]
                    {
                        Field(
                            "priority",
                            "Priority",
                            GraphAuthoringFieldValueKind.Integer,
                            true,
                            new GraphAuthoringFieldConstraint(minimum: 0)),
                        Field(
                            "condition-rule-ownership",
                            "Condition Ownership",
                            GraphAuthoringFieldValueKind.Enum,
                            true,
                            new GraphAuthoringFieldConstraint(
                                allowedValues: new[]
                                {
                                    ConditionRuleGraphOwnership.Inline.ToString(),
                                    ConditionRuleGraphOwnership.Shared.ToString()
                                })),
                        Field("condition-rule-id", "Condition Rule", GraphAuthoringFieldValueKind.IdentityReference, false),
                        Field("shared-condition-rule-asset", "Shared Condition Rule", GraphAuthoringFieldValueKind.AssetReference, true),
                        Field(
                            "interruption",
                            "Interruption",
                            GraphAuthoringFieldValueKind.Enum,
                            true,
                            new GraphAuthoringFieldConstraint(
                                allowedValues: Enum.GetNames(typeof(BTAbortPolicy))))
                    },
                    mutationBindingId: "btsmtl.state-machine-transition",
                    validationBindingId: "btsmtl.state-machine-transition",
                    compilerBindingId: "btsmtl.state-machine-transition",
                    documentCodecId: "btsmtl.state-machine-transition")));
            s_Registered = true;
        }

        static GraphAuthoringFieldDescriptor Field(
            string id,
            string displayName,
            GraphAuthoringFieldValueKind valueKind,
            bool writable,
            GraphAuthoringFieldConstraint constraint = null)
        {
            GraphAuthoringFieldAccess access = GraphAuthoringFieldAccess.AuthoringRead;
            if (writable)
                access |= GraphAuthoringFieldAccess.AuthoringWrite;
            return new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                displayName,
                valueKind,
                access,
                constraint: constraint);
        }
    }

    public sealed class BtsmtlStateMachineDocument : IGraphAuthoringStateMachineProjection
    {
        readonly StateMachineGraph m_Graph;
        readonly StateMachineNode m_OwnerNode;
        readonly string m_ContentRevision;
        readonly GraphAuthoringDocumentRoleId m_Role =
            BtsmtlGraphAuthoringCapabilities.SharedRoleId(
                "StateMachineGraph");

        public BtsmtlStateMachineDocument(StateMachineGraph graph, string contentRevision)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            m_ContentRevision = string.IsNullOrWhiteSpace(contentRevision)
                ? throw new ArgumentException("BTSMTL StateMachine revision is missing.", nameof(contentRevision))
                : contentRevision.Trim();
            if (string.IsNullOrWhiteSpace(graph.GraphAuthoringId))
                throw new InvalidOperationException("BTSMTL StateMachine authoring identity is missing.");
            BtsmtlStateMachineAuthoringCapabilities.EnsureRegistered();
        }

        public BtsmtlStateMachineDocument(
            StateMachineNode ownerNode,
            string contentRevision)
            : this(
                ownerNode?.Graph ?? throw new ArgumentNullException(nameof(ownerNode)),
                contentRevision)
        {
            m_OwnerNode = ownerNode;
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Gameplay;
        public GraphAuthoringDomainId DomainId =>
            BtsmtlGraphAuthoringCapabilities.SharedDomain;
        public GraphAuthoringDocumentRoleId DocumentRoleId => m_Role;
        public string DocumentId => m_Graph.GraphAuthoringId;
        public string DisplayName => m_Graph.name;
        public string ContentRevision => m_ContentRevision;
        public UnityEngine.Object SerializedOwner => m_Graph.SerializedOwner;
        public IReadOnlyList<GraphAuthoringPageProjection> Pages => new[]
        {
            new GraphAuthoringPageProjection(
                new GraphAuthoringElementId(DocumentId),
                DisplayName,
                DocumentRoleId.Value)
        };
        public IReadOnlyList<GraphAuthoringNodeProjection> Nodes =>
            Array.Empty<GraphAuthoringNodeProjection>();
        public IReadOnlyList<GraphAuthoringEdgeProjection> Edges =>
            Array.Empty<GraphAuthoringEdgeProjection>();
        public GraphAuthoringStateMachineEntryProjection Entry => ProjectEntry();
        public IReadOnlyList<GraphAuthoringStateProjection> States => ProjectStates();
        public IReadOnlyList<GraphAuthoringStateAliasProjection> Aliases =>
            Array.Empty<GraphAuthoringStateAliasProjection>();
        public IReadOnlyList<GraphAuthoringTransitionProjection> Transitions =>
            ProjectTransitions();
        internal StateMachineGraph Graph => m_Graph;
        internal StateMachineNode OwnerNode => m_OwnerNode;

        GraphAuthoringStateMachineEntryProjection ProjectEntry()
        {
            StateMachineEnterNode entry = m_Graph.EnterNode ??
                throw new InvalidOperationException("BTSMTL StateMachine Entry is missing.");
            BaseEdge[] targets = m_Graph.Edges
                .Where(value => m_Graph.IsTransitionEdge(value) &&
                                value.StartNodeGUID == entry.GUID)
                .ToArray();
            if (targets.Length != 1)
                throw new InvalidOperationException("BTSMTL StateMachine Entry requires exactly one target.");
            return new GraphAuthoringStateMachineEntryProjection(
                new GraphAuthoringElementId(entry.GUID),
                new GraphAuthoringElementId(targets[0].EndNodeGUID),
                entry.Position);
        }

        IReadOnlyList<GraphAuthoringStateProjection> ProjectStates()
        {
            var result = new List<GraphAuthoringStateProjection>();
            foreach (StateNode state in m_Graph.StateNodes.OrderBy(value => value.GUID, StringComparer.Ordinal))
            {
                StateBehaviorGraphReferenceModule module =
                    state.GetModule<StateBehaviorGraphReferenceModule>();
                string childGraphId = module?.SubTree?.GraphAuthoringId ?? string.Empty;
                result.Add(new GraphAuthoringStateProjection(
                    new GraphAuthoringElementId(state.GUID),
                    new GraphAuthoringCapabilityId("btsmtl.state"),
                    state.DisplayName,
                    state.Position,
                    BtsmtlGameplayStatePayload.Instance,
                    Element(childGraphId),
                    module?.SharedSubTreeAsset ? "Shared" : "Inline"));
            }
            if (m_Graph.AnyStateNode != null)
            {
                result.Add(new GraphAuthoringStateProjection(
                    new GraphAuthoringElementId(m_Graph.AnyStateNode.GUID),
                    new GraphAuthoringCapabilityId("btsmtl.anchor.any"),
                    "Any State",
                    m_Graph.AnyStateNode.Position,
                    BtsmtlGameplayStatePayload.Instance));
            }
            if (m_Graph.ExitNode != null)
            {
                result.Add(new GraphAuthoringStateProjection(
                    new GraphAuthoringElementId(m_Graph.ExitNode.GUID),
                    new GraphAuthoringCapabilityId("btsmtl.anchor.exit"),
                    "Exit",
                    m_Graph.ExitNode.Position,
                    BtsmtlGameplayStatePayload.Instance));
            }
            return result;
        }

        IReadOnlyList<GraphAuthoringTransitionProjection> ProjectTransitions()
        {
            string entryId = m_Graph.EnterNode?.GUID ?? string.Empty;
            return m_Graph.Edges
                .Where(value => m_Graph.IsTransitionEdge(value) &&
                                value.StartNodeGUID != entryId)
                .OrderBy(value => value.TransitionPriority)
                .ThenBy(value => value.FlowOrder)
                .ThenBy(value => value.GUID, StringComparer.Ordinal)
                .Select(value => new GraphAuthoringTransitionProjection(
                    new GraphAuthoringElementId(value.GUID),
                    new GraphAuthoringElementId(value.StartNodeGUID),
                    new GraphAuthoringElementId(value.EndNodeGUID),
                    BtsmtlStateMachineAuthoringCapabilities.Transition,
                    value.TransitionPriority,
                    new BtsmtlGameplayTransitionPayload(value),
                    Element(value.ConditionRuleGraph?.GraphAuthoringId),
                    $"{NodeName(value.StartNodeGUID)} → {NodeName(value.EndNodeGUID)}"))
                .ToArray();
        }

        string NodeName(string nodeId) =>
            m_Graph.Nodes.FirstOrDefault(value => value != null && value.GUID == nodeId)
                ?.DisplayName ?? nodeId;

        static GraphAuthoringElementId Element(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? default
                : new GraphAuthoringElementId(value);
    }

    internal static class CharacterPoseAuthoringDisplayNames
    {
        public static string StateMachine(
            CharacterPoseStateMachineDefinition machine) =>
            HumanizeIdentity(
                machine?.StateMachineId.Value,
                "Pose") +
            " State Machine";

        public static string Transition(
            CharacterPoseStateMachineDefinition machine,
            CharacterPoseStateTransition transition)
        {
            if (machine == null || transition == null)
                return "Transition Rule";
            return $"{Source(machine, transition.Source)} → " +
                   State(machine, transition.TargetStateId);
        }

        public static string Source(
            CharacterPoseStateMachineDefinition machine,
            CharacterPoseStateTransitionSource source)
        {
            if (source == null)
                return "Unknown Source";
            if (source.Kind == PoseStateTransitionSourceKind.State)
                return State(machine, source.StateId);
            CharacterPoseStateAlias alias = machine?.Aliases
                .FirstOrDefault(value => value.AliasId == source.AliasId);
            return string.IsNullOrWhiteSpace(alias?.DisplayName)
                ? "State Alias"
                : alias.DisplayName;
        }

        public static string State(
            CharacterPoseStateMachineDefinition machine,
            PoseStateId stateId)
        {
            CharacterPoseStateDefinition state = machine?.States
                .FirstOrDefault(value => value.StateId == stateId);
            return string.IsNullOrWhiteSpace(state?.DisplayName)
                ? "Unknown State"
                : state.DisplayName;
        }

        static string HumanizeIdentity(
            string identity,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(identity) ||
                identity.Length >= 24 &&
                identity.All(Uri.IsHexDigit))
            {
                return fallback;
            }
            int separator = Math.Max(
                identity.LastIndexOf('.'),
                Math.Max(
                    identity.LastIndexOf('/'),
                    identity.LastIndexOf(':')));
            string value = identity.Substring(separator + 1)
                .Replace('-', ' ')
                .Replace('_', ' ')
                .Trim();
            if (string.IsNullOrEmpty(value))
                return fallback;
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }

    public sealed class CharacterPoseStateMachineDocument : IGraphAuthoringStateMachineProjection
    {
        readonly CharacterPresentationPoseGraphAsset m_Asset;
        readonly CharacterPoseStateMachineDefinition m_Definition;

        public CharacterPoseStateMachineDocument(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseStateMachineDefinition definition)
        {
            m_Asset = asset ? asset : throw new ArgumentNullException(nameof(asset));
            m_Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Pose;
        public GraphAuthoringDomainId DomainId =>
            CharacterPoseGraphAuthoringCapabilities.Domain;
        public GraphAuthoringDocumentRoleId DocumentRoleId =>
            CharacterPoseGraphAuthoringCapabilities.StateMachine;
        public string DocumentId => m_Definition.StateMachineId.Value;
        public string DisplayName =>
            CharacterPoseAuthoringDisplayNames.StateMachine(m_Definition);
        public string ContentRevision => m_Definition.ContentRevision;
        public UnityEngine.Object SerializedOwner => m_Asset;
        public IReadOnlyList<GraphAuthoringPageProjection> Pages => new[]
        {
            new GraphAuthoringPageProjection(
                new GraphAuthoringElementId(DocumentId),
                DisplayName,
                DocumentRoleId.Value)
        };
        public IReadOnlyList<GraphAuthoringNodeProjection> Nodes =>
            Array.Empty<GraphAuthoringNodeProjection>();
        public IReadOnlyList<GraphAuthoringEdgeProjection> Edges =>
            Array.Empty<GraphAuthoringEdgeProjection>();
        public GraphAuthoringStateMachineEntryProjection Entry =>
            new GraphAuthoringStateMachineEntryProjection(
                new GraphAuthoringElementId(m_Definition.Entry.EntryId.Value),
                new GraphAuthoringElementId(m_Definition.Entry.TargetStateId.Value),
                m_Asset.ResolveStateMachineElementPosition(
                    m_Definition,
                    m_Definition.Entry.EntryId.Value));
        public IReadOnlyList<GraphAuthoringStateProjection> States =>
            m_Definition.States
                .OrderBy(value => value.StateId)
                .Select(value => new GraphAuthoringStateProjection(
                    new GraphAuthoringElementId(value.StateId.Value),
                    CharacterPoseGraphAuthoringCapabilities.StateMachineState,
                    value.DisplayName,
                    m_Asset.ResolveStateMachineElementPosition(
                        m_Definition,
                        value.StateId.Value),
                    new CharacterPoseStatePayload(value),
                    new GraphAuthoringElementId(value.PoseGraphId.Value)))
                .ToArray();
        public IReadOnlyList<GraphAuthoringStateAliasProjection> Aliases =>
            m_Definition.Aliases
                .OrderBy(value => value.AliasId)
                .Select(value => new GraphAuthoringStateAliasProjection(
                    new GraphAuthoringElementId(value.AliasId.Value),
                    value.Sources.Select(SourceId).ToArray(),
                    value.DisplayName,
                    m_Asset.ResolveStateMachineElementPosition(
                        m_Definition,
                        value.AliasId.Value)))
                .ToArray();
        public IReadOnlyList<GraphAuthoringTransitionProjection> Transitions =>
            m_Definition.Transitions
                .OrderBy(value => value.Priority)
                .ThenBy(value => value.TransitionId)
                .Select(value => new GraphAuthoringTransitionProjection(
                    new GraphAuthoringElementId(value.TransitionId.Value),
                    SourceId(value.Source),
                    new GraphAuthoringElementId(value.TargetStateId.Value),
                    CharacterPoseGraphAuthoringCapabilities.StateMachineTransition,
                    value.Priority,
                    new CharacterPoseTransitionPayload(value),
                    new GraphAuthoringElementId(value.Rule.GraphId.Value),
                    CharacterPoseAuthoringDisplayNames.Transition(
                        m_Definition,
                        value)))
                .ToArray();
        internal CharacterPresentationPoseGraphAsset Asset => m_Asset;
        internal CharacterPoseStateMachineDefinition Definition => m_Definition;

        static GraphAuthoringElementId SourceId(
            CharacterPoseStateTransitionSource source) =>
            source.Kind == PoseStateTransitionSourceKind.State
                ? new GraphAuthoringElementId(source.StateId.Value)
                : new GraphAuthoringElementId(source.AliasId.Value);
    }

    public sealed class BtsmtlStateMachineMutationAdapter :
        IGraphAuthoringDomainMutation
    {
        public bool ReadOnly { get; set; }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringMutationRequest request) =>
            Apply(document, new[] { request });

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringMutationRequest> requests)
        {
            if (ReadOnly)
                throw new InvalidOperationException(
                    "BTSMTL StateMachine document is read-only.");
            BtsmtlStateMachineDocument btsmtl =
                document as BtsmtlStateMachineDocument ??
                throw new ArgumentException(
                    "BTSMTL StateMachine mutation requires the Gameplay adapter.",
                    nameof(document));
            if (btsmtl.Graph.SerializedOwner == null)
                throw new InvalidOperationException(
                    "BTSMTL StateMachine has no writable serialized owner.");
            GraphAuthoringMutationRequest[] values =
                (requests ?? throw new ArgumentNullException(nameof(requests)))
                .ToArray();
            btsmtl.Graph.ApplyModify(
                values.Length == 1
                    ? "BTSMTL StateMachine " + values[0].Kind
                    : "BTSMTL StateMachine Edit",
                () =>
                {
                    for (int i = 0; i < values.Length; i++)
                        ApplyInsideTransaction(btsmtl.Graph, values[i]);
                });
        }

        static void ApplyInsideTransaction(
            StateMachineGraph graph,
            GraphAuthoringMutationRequest request)
        {
            switch (request.Kind)
            {
                case GraphAuthoringMutationKind.CreateState:
                {
                    StateNode state =
                        graph.CreateNode(typeof(StateNode)) as StateNode ??
                        throw new InvalidOperationException(
                            "BTSMTL State creation failed.");
                    state.Position = request.Position;
                    if (request.Value is string displayName &&
                        !string.IsNullOrWhiteSpace(displayName))
                        state.DisplayName = displayName.Trim();
                    return;
                }
                case GraphAuthoringMutationKind.DeleteState:
                    DeleteState(graph, State(graph, request.TargetId));
                    return;
                case GraphAuthoringMutationKind.MoveElement:
                {
                    BaseNode node = Node(graph, request.TargetId);
                    node.Position = request.Position;
                    node.OnMoved();
                    return;
                }
                case GraphAuthoringMutationKind.CreateTransition:
                    CreateTransition(graph, request);
                    return;
                case GraphAuthoringMutationKind.DeleteTransition:
                    DeleteTransition(graph, Transition(graph, request.TargetId));
                    return;
                case GraphAuthoringMutationKind.SetTransitionField:
                    SetTransitionField(
                        graph,
                        Transition(graph, request.TargetId),
                        request.FieldId,
                        request.Value);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Shared StateMachine command '{request.Kind}' is not valid for BTSMTL.");
            }
        }

        static void CreateTransition(
            StateMachineGraph graph,
            GraphAuthoringMutationRequest request)
        {
            BaseNode source = Node(graph, request.TargetId);
            BaseNode target = Node(graph, request.SecondaryTargetId);
            if (source is StateMachineEnterNode)
            {
                if (!(target is StateNode))
                    throw new InvalidOperationException(
                        "BTSMTL Entry can target only a State.");
                BaseEdge entry = graph.Edges.Single(value =>
                    graph.IsTransitionEdge(value) &&
                    value.StartNodeGUID == source.GUID);
                graph.RetargetTransition(entry, source, target);
                entry.ClearConditionRuleGraph();
                return;
            }
            BtsmtlGameplayTransitionCreation creation =
                request.Value as BtsmtlGameplayTransitionCreation ??
                throw new InvalidOperationException(
                    "BTSMTL transition creation requires a typed Gameplay payload.");
            BaseEdge edge = graph.Link(
                source,
                target,
                StateMachinePorts.StateOut,
                StateMachinePorts.StateIn) ??
                throw new InvalidOperationException(
                    "BTSMTL transition already exists.");
            edge.TransitionPriority = creation.Priority;
            edge.AbortPolicy = creation.Interruption;
        }

        static void DeleteState(
            StateMachineGraph graph,
            StateNode state)
        {
            BaseEdge entry = graph.Edges.Single(value =>
                graph.IsTransitionEdge(value) &&
                value.StartNodeGUID == graph.EnterNode.GUID);
            if (entry.EndNodeGUID == state.GUID)
                throw new InvalidOperationException(
                    $"BTSMTL State '{state.ResolvedDisplayName}' is the Entry target. Retarget Entry before deleting it.");
            foreach (BaseEdge transition in graph.Edges
                         .Where(value =>
                             graph.IsTransitionEdge(value) &&
                             (value.StartNodeGUID == state.GUID ||
                              value.EndNodeGUID == state.GUID))
                         .ToArray())
                DeleteTransition(graph, transition);
            graph.DeleteNode(state);
        }

        static void DeleteTransition(
            StateMachineGraph graph,
            BaseEdge edge)
        {
            if (edge.StartNode is StateMachineEnterNode)
                throw new InvalidOperationException(
                    "BTSMTL Entry transition cannot be deleted. Retarget it instead.");
            edge.ClearConditionRuleGraph();
            graph.UnLink(edge);
        }

        static void SetTransitionField(
            StateMachineGraph graph,
            BaseEdge edge,
            GraphAuthoringFieldId fieldId,
            object value)
        {
            BtsmtlTransitionMutation.Apply(
                graph,
                edge,
                fieldId,
                value);
        }

        static T ParseEnum<T>(
            object value,
            GraphAuthoringFieldId fieldId)
            where T : struct
        {
            if (value is T typed)
                return typed;
            if (Enum.TryParse(value?.ToString(), false, out T parsed))
                return parsed;
            throw new InvalidOperationException(
                $"BTSMTL field '{fieldId}' has invalid value '{value}'.");
        }

        static StateNode State(
            StateMachineGraph graph,
            GraphAuthoringElementId stateId) =>
            Node(graph, stateId) as StateNode ??
            throw new InvalidOperationException(
                $"BTSMTL State '{stateId}' is missing.");

        static BaseNode Node(
            StateMachineGraph graph,
            GraphAuthoringElementId nodeId) =>
            graph.Nodes.SingleOrDefault(value =>
                value != null && value.GUID == nodeId.Value) ??
            throw new InvalidOperationException(
                $"BTSMTL StateMachine element '{nodeId}' is missing.");

        static BaseEdge Transition(
            StateMachineGraph graph,
            GraphAuthoringElementId transitionId) =>
            graph.Edges.SingleOrDefault(value =>
                graph.IsTransitionEdge(value) &&
                value.GUID == transitionId.Value) ??
            throw new InvalidOperationException(
                $"BTSMTL Transition '{transitionId}' is missing.");
    }

    public sealed class CharacterPoseStateCreation
    {
        public CharacterPoseStateCreation(
            CharacterPoseStateDefinition state,
            CharacterTypedPoseGraph graph)
        {
            State = state ??
                throw new ArgumentNullException(nameof(state));
            Graph = graph ??
                throw new ArgumentNullException(nameof(graph));
            if (State.PoseGraphId != Graph.GraphId)
            {
                throw new ArgumentException(
                    "Pose State and its state-local Graph identity do not match.");
            }
        }

        public CharacterPoseStateDefinition State { get; }
        public CharacterTypedPoseGraph Graph { get; }
    }

    public sealed class CharacterPoseStateMachineMutationAdapter :
        IGraphAuthoringDomainMutation
    {
        readonly CharacterPresentationMutationService m_Service =
            new CharacterPresentationMutationService();

        public bool ReadOnly { get; set; }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringMutationRequest request) =>
            Apply(document, new[] { request });

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringMutationRequest> requests)
        {
            if (ReadOnly)
                throw new InvalidOperationException(
                    "Pose StateMachine document is read-only.");
            CharacterPoseStateMachineDocument pose =
                document as CharacterPoseStateMachineDocument ??
                throw new ArgumentException(
                    "Pose StateMachine mutation requires the Presentation adapter.",
                    nameof(document));
            var transaction =
                new CharacterPresentationMutationTransaction(
                    Guid.NewGuid().ToString("N"),
                    "Edit Pose StateMachine");
            foreach (GraphAuthoringMutationRequest request in
                     requests ??
                     throw new ArgumentNullException(nameof(requests)))
            {
                Add(pose, request, transaction);
            }
            m_Service.Apply(
                new CharacterPoseGraphAssetMutationOwner(pose.Asset),
                transaction);
        }

        static void Add(
            CharacterPoseStateMachineDocument pose,
            GraphAuthoringMutationRequest request,
            CharacterPresentationMutationTransaction transaction)
        {
            string machineId = pose.DocumentId;
            switch (request.Kind)
            {
                case GraphAuthoringMutationKind.CreateState:
                {
                    CharacterPoseStateCreation creation =
                        request.Value as CharacterPoseStateCreation ??
                        throw new InvalidOperationException(
                            "Create Pose State requires one typed State and state-local Graph.");
                    transaction.Add(new CreatePoseGraphMutation(
                        pose.Asset.name,
                        creation.Graph));
                    transaction.Add(new CreatePoseStateMutation(
                        machineId,
                        creation.State));
                    transaction.Add(
                        new SetPoseStateMachineLayoutElementMutation(
                            machineId,
                            creation.State.StateId.Value,
                            request.Position));
                    return;
                }
                case GraphAuthoringMutationKind.DeleteState:
                    AddDeleteState(
                        pose,
                        new PoseStateId(request.TargetId.Value),
                        transaction);
                    return;
                case GraphAuthoringMutationKind.CreateTransition:
                    if (request.TargetId.Equals(
                            pose.Entry.ElementId))
                    {
                        transaction.Add(
                            new ConfigurePoseStateMachineMutation(
                                machineId,
                                new CharacterPoseStateEntry(
                                    pose.Definition.Entry.EntryId,
                                    new PoseStateId(
                                        request.SecondaryTargetId.Value)),
                                pose.Definition.Aliases.ToArray(),
                                pose.Definition.MaxTransitionsPerFrame));
                        return;
                    }
                    transaction.Add(new CreatePoseTransitionMutation(
                        machineId,
                        request.Value as CharacterPoseStateTransition ??
                        throw new InvalidOperationException(
                            "Create Pose Transition requires a complete typed transition payload.")));
                    return;
                case GraphAuthoringMutationKind.DeleteTransition:
                    transaction.Add(new DeletePoseTransitionMutation(
                        machineId,
                        new PoseStateTransitionId(
                            request.TargetId.Value)));
                    return;
                case GraphAuthoringMutationKind.SetTransitionField:
                    transaction.Add(new SetPoseTransitionFieldMutation(
                        machineId,
                        new PoseStateTransitionId(
                            request.TargetId.Value),
                        request.FieldId.Value,
                        request.Value));
                    return;
                case GraphAuthoringMutationKind.SetStateField:
                    transaction.Add(new SetPoseStateFieldMutation(
                        machineId,
                        new PoseStateId(request.TargetId.Value),
                        request.FieldId.Value,
                        request.Value));
                    return;
                case GraphAuthoringMutationKind.CreateStateAlias:
                {
                    CharacterPoseStateAlias alias =
                        request.Value as CharacterPoseStateAlias ??
                        throw new InvalidOperationException(
                            "Create Pose State Alias requires a typed alias payload.");
                    transaction.Add(
                        new ConfigurePoseStateMachineMutation(
                            machineId,
                            pose.Definition.Entry,
                            pose.Definition.Aliases
                                .Concat(new[] { alias })
                                .OrderBy(value => value.AliasId)
                                .ToArray(),
                            pose.Definition.MaxTransitionsPerFrame));
                    transaction.Add(
                        new SetPoseStateMachineLayoutElementMutation(
                            machineId,
                            alias.AliasId.Value,
                            request.Position));
                    return;
                }
                case GraphAuthoringMutationKind.DeleteStateAlias:
                    AddDeleteAlias(
                        pose,
                        new PoseStateAliasId(request.TargetId.Value),
                        transaction);
                    return;
                case GraphAuthoringMutationKind.MoveElement:
                    transaction.Add(
                        new SetPoseStateMachineLayoutElementMutation(
                            machineId,
                            request.TargetId.Value,
                            request.Position));
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Shared StateMachine command '{request.Kind}' is not valid for a Pose StateMachine.");
            }
        }

        static void AddDeleteState(
            CharacterPoseStateMachineDocument pose,
            PoseStateId stateId,
            CharacterPresentationMutationTransaction transaction)
        {
            CharacterPoseStateDefinition state =
                pose.Definition.States.SingleOrDefault(
                    value => value.StateId == stateId) ??
                throw new InvalidOperationException(
                    $"Pose State '{stateId}' does not exist.");
            if (pose.Definition.Entry.TargetStateId == stateId)
            {
                throw new InvalidOperationException(
                    $"Pose State '{stateId}' is the Entry target. Connect Entry to another State before deleting it.");
            }

            var removedAliases = new HashSet<PoseStateAliasId>();
            bool changed;
            do
            {
                changed = false;
                foreach (CharacterPoseStateAlias alias in
                         pose.Definition.Aliases)
                {
                    if (removedAliases.Contains(alias.AliasId))
                        continue;
                    bool hasRemainingSource = alias.Sources.Any(source =>
                        source.Kind ==
                            PoseStateTransitionSourceKind.State
                            ? source.StateId != stateId
                            : !removedAliases.Contains(
                                source.AliasId));
                    if (!hasRemainingSource &&
                        removedAliases.Add(alias.AliasId))
                    {
                        changed = true;
                    }
                }
            } while (changed);
            foreach (CharacterPoseStateTransition transition in
                     pose.Definition.Transitions.Where(value =>
                         value.TargetStateId == stateId ||
                         value.Source.Kind ==
                         PoseStateTransitionSourceKind.State &&
                         value.Source.StateId == stateId ||
                         value.Source.Kind ==
                         PoseStateTransitionSourceKind.Alias &&
                         removedAliases.Contains(
                             value.Source.AliasId)))
            {
                transaction.Add(new DeletePoseTransitionMutation(
                    pose.DocumentId,
                    transition.TransitionId));
            }

            CharacterPoseStateAlias[] aliases =
                pose.Definition.Aliases
                    .Where(value =>
                        !removedAliases.Contains(value.AliasId))
                    .Select(value => new CharacterPoseStateAlias(
                        value.AliasId,
                        value.DisplayName,
                        value.Sources.Where(source =>
                                source.Kind !=
                                PoseStateTransitionSourceKind.State ||
                                source.StateId != stateId)
                            .Where(source =>
                                source.Kind !=
                                PoseStateTransitionSourceKind.Alias ||
                                !removedAliases.Contains(
                                    source.AliasId))
                            .ToArray()))
                    .ToArray();
            transaction.Add(new ConfigurePoseStateMachineMutation(
                pose.DocumentId,
                pose.Definition.Entry,
                aliases,
                pose.Definition.MaxTransitionsPerFrame));
            transaction.Add(new DeletePoseStateMutation(
                pose.DocumentId,
                stateId));
            transaction.Add(new DeletePoseGraphMutation(
                pose.Asset.name,
                state.PoseGraphId));
            transaction.Add(
                new RemovePoseStateMachineLayoutElementMutation(
                    pose.DocumentId,
                    stateId.Value));
            foreach (PoseStateAliasId aliasId in removedAliases)
            {
                transaction.Add(
                    new RemovePoseStateMachineLayoutElementMutation(
                        pose.DocumentId,
                        aliasId.Value));
            }
        }

        static void AddDeleteAlias(
            CharacterPoseStateMachineDocument pose,
            PoseStateAliasId aliasId,
            CharacterPresentationMutationTransaction transaction)
        {
            if (!pose.Definition.Aliases.Any(value =>
                    value.AliasId == aliasId))
                throw new InvalidOperationException(
                    $"Pose State Alias '{aliasId}' does not exist.");
            var removed = new HashSet<PoseStateAliasId> { aliasId };
            bool changed;
            do
            {
                changed = false;
                foreach (CharacterPoseStateAlias alias in
                         pose.Definition.Aliases)
                {
                    if (removed.Contains(alias.AliasId))
                        continue;
                    bool hasRemainingSource = alias.Sources.Any(source =>
                        source.Kind == PoseStateTransitionSourceKind.State ||
                        !removed.Contains(source.AliasId));
                    if (!hasRemainingSource && removed.Add(alias.AliasId))
                        changed = true;
                }
            } while (changed);
            foreach (CharacterPoseStateTransition transition in
                     pose.Definition.Transitions.Where(value =>
                         value.Source.Kind ==
                         PoseStateTransitionSourceKind.Alias &&
                         removed.Contains(value.Source.AliasId)))
            {
                transaction.Add(new DeletePoseTransitionMutation(
                    pose.DocumentId,
                    transition.TransitionId));
            }
            CharacterPoseStateAlias[] aliases = pose.Definition.Aliases
                .Where(value => !removed.Contains(value.AliasId))
                .Select(value => new CharacterPoseStateAlias(
                    value.AliasId,
                    value.DisplayName,
                    value.Sources.Where(source =>
                            source.Kind !=
                            PoseStateTransitionSourceKind.Alias ||
                            !removed.Contains(source.AliasId))
                        .ToArray()))
                .ToArray();
            transaction.Add(new ConfigurePoseStateMachineMutation(
                pose.DocumentId,
                pose.Definition.Entry,
                aliases,
                pose.Definition.MaxTransitionsPerFrame));
            foreach (PoseStateAliasId removedId in removed)
            {
                transaction.Add(
                    new RemovePoseStateMachineLayoutElementMutation(
                        pose.DocumentId,
                        removedId.Value));
            }
        }
    }

    public sealed class BtsmtlStateMachinePolicy : IGraphAuthoringStateMachinePolicy
    {
        readonly Action<StateNode> m_OpenState;
        readonly Action<BaseEdge> m_OpenTransition;

        public BtsmtlStateMachinePolicy(
            Action<StateNode> openState,
            Action<BaseEdge> openTransition)
        {
            m_OpenState = openState ?? throw new ArgumentNullException(nameof(openState));
            m_OpenTransition = openTransition ?? throw new ArgumentNullException(nameof(openTransition));
            BtsmtlStateMachineAuthoringCapabilities.EnsureRegistered();
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Gameplay;
        public bool PersistsLayout => true;

        public void ValidateDocument(IGraphAuthoringStateMachineProjection document)
        {
            BtsmtlStateMachineDocument gameplay = Require(document);
            if (gameplay.OwnerNode != null)
            {
                ScopedGraphReferenceModule owner =
                    gameplay.OwnerNode.GetModule<ScopedGraphReferenceModule>() ??
                    throw new InvalidOperationException(
                        $"BTSMTL StateMachine node '{gameplay.OwnerNode.GUID}' has no graph owner.");
                bool shared = owner.SharedGraphAsset;
                if (owner.Graph == null || shared && owner.InlineGraph != null ||
                    !shared && owner.InlineGraph == null ||
                    !ReferenceEquals(owner.Graph, gameplay.Graph))
                {
                    throw new InvalidOperationException(
                        $"BTSMTL StateMachine node '{gameplay.OwnerNode.GUID}' violates inline-first ownership.");
                }
            }
            foreach (StateNode state in gameplay.Graph.StateNodes)
            {
                StateBehaviorGraphReferenceModule module =
                    state.GetModule<StateBehaviorGraphReferenceModule>() ??
                    throw new InvalidOperationException(
                        $"BTSMTL State '{state.GUID}' has no State Behavior owner.");
                bool shared = module.SharedSubTreeAsset;
                if (module.SubTree == null || shared && module.InlineSubTree != null ||
                    !shared && module.InlineSubTree == null)
                {
                    throw new InvalidOperationException(
                        $"BTSMTL State '{state.GUID}' violates inline-first ownership.");
                }
            }
            foreach (GraphAuthoringTransitionProjection transition in gameplay.Transitions)
            {
                BtsmtlGameplayTransitionPayload payload =
                    transition.Payload as BtsmtlGameplayTransitionPayload ??
                    throw new InvalidOperationException(
                        $"BTSMTL Transition '{transition.TransitionId}' has a non-Gameplay payload.");
                if (payload.ConditionStatus != ConditionRuleGraphReferenceStatus.ResolvedInline &&
                    payload.ConditionStatus != ConditionRuleGraphReferenceStatus.ResolvedShared)
                {
                    throw new InvalidOperationException(
                        $"BTSMTL Transition '{transition.TransitionId}' Condition Rule is invalid: {payload.ConditionStatus}.");
                }
            }
        }

        public bool CanCreateTransition(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId sourceStateId,
            GraphAuthoringElementId targetStateId)
        {
            BtsmtlStateMachineDocument gameplay = Require(document);
            BaseNode source = Node(gameplay.Graph, sourceStateId);
            BaseNode target = Node(gameplay.Graph, targetStateId);
            return source is StateMachineEnterNode &&
                       target is StateNode ||
                   (source is StateNode ||
                    source is StateMachineAnyStateNode) &&
                   (target is StateNode ||
                    target is StateMachineExitNode);
        }

        public object CreateTransitionPayload(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId sourceStateId,
            GraphAuthoringElementId targetStateId)
        {
            BtsmtlStateMachineDocument gameplay = Require(document);
            BaseNode source = Node(gameplay.Graph, sourceStateId);
            if (source is StateMachineEnterNode)
                return null;
            return new BtsmtlGameplayTransitionCreation();
        }

        public IReadOnlyList<GraphAuthoringFieldDescriptor> GetStateFields(
            GraphAuthoringStateProjection state) =>
            Array.Empty<GraphAuthoringFieldDescriptor>();

        public IReadOnlyList<GraphAuthoringFieldDescriptor> GetTransitionFields(
            GraphAuthoringTransitionProjection transition)
        {
            if (!(transition?.Payload is BtsmtlGameplayTransitionPayload))
                throw new InvalidOperationException("BTSMTL transition details reject non-Gameplay payload.");
            return GraphAuthoringCapabilityRegistrationRoot.Catalog
                .Require(
                    BtsmtlStateMachineAuthoringCapabilities.Transition,
                    BtsmtlGraphAuthoringCapabilities.SharedDomain,
                    BtsmtlGraphAuthoringCapabilities.SharedRoleId(
                        "StateMachineGraph"))
                .Fields
                .ToArray();
        }

        public void OpenStateChildGraph(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId stateId)
        {
            StateNode state = Node(Require(document).Graph, stateId) as StateNode ??
                throw new InvalidOperationException($"BTSMTL State '{stateId}' cannot open a State Behavior graph.");
            m_OpenState(state);
        }

        public void OpenTransitionRule(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId transitionId)
        {
            BtsmtlStateMachineDocument gameplay = Require(document);
            BaseEdge edge = gameplay.Graph.Edges.SingleOrDefault(
                value => value != null && value.GUID == transitionId.Value) ??
                throw new InvalidOperationException($"BTSMTL Transition '{transitionId}' is missing.");
            m_OpenTransition(edge);
        }

        static BtsmtlStateMachineDocument Require(
            IGraphAuthoringStateMachineProjection document) =>
            document as BtsmtlStateMachineDocument ??
            throw new InvalidOperationException("BTSMTL StateMachine policy requires the Gameplay adapter.");

        static BaseNode Node(StateMachineGraph graph, GraphAuthoringElementId nodeId) =>
            graph.Nodes.SingleOrDefault(value => value != null && value.GUID == nodeId.Value);
    }

    public sealed class CharacterPoseStateMachinePolicy : IGraphAuthoringStateMachinePolicy
    {
        readonly Action<CharacterPoseStateDefinition> m_OpenState;
        readonly Action<CharacterPoseStateTransition> m_OpenTransition;
        readonly Func<
            CharacterPoseStateMachineDocument,
            GraphAuthoringElementId,
            GraphAuthoringElementId,
            CharacterPoseStateTransition> m_CreateTransition;

        public CharacterPoseStateMachinePolicy(
            Action<CharacterPoseStateDefinition> openState,
            Action<CharacterPoseStateTransition> openTransition,
            Func<
                CharacterPoseStateMachineDocument,
                GraphAuthoringElementId,
                GraphAuthoringElementId,
                CharacterPoseStateTransition> createTransition = null)
        {
            m_OpenState = openState ?? throw new ArgumentNullException(nameof(openState));
            m_OpenTransition = openTransition ?? throw new ArgumentNullException(nameof(openTransition));
            m_CreateTransition = createTransition;
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Pose;
        public bool PersistsLayout => true;

        public void ValidateDocument(IGraphAuthoringStateMachineProjection document)
        {
            CharacterPoseStateMachineDocument pose = Require(document);
            CharacterPoseStateMachineAuthoringValidator.RequireValid(
                pose.Definition,
                pose.Asset.RequireGraph);
            PoseGraphId[] graphIds = pose.Asset.EnumerateGraphs()
                .Where(value => value != null)
                .Select(value => value.GraphId)
                .ToArray();
            if (graphIds.Distinct().Count() != graphIds.Length)
                throw new InvalidOperationException(
                    "Pose StateMachine root-owned graph catalog contains duplicate Graph identities.");
            int ownerCount = pose.Asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Where(value =>
                    value?.Payload is CharacterPoseStateMachineNodePayload payload &&
                    payload.StateMachine != null &&
                    payload.StateMachine.StateMachineId.Equals(
                        pose.Definition.StateMachineId))
                .Count();
            if (ownerCount != 1)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{pose.Definition.StateMachineId}' must have exactly one root-owned node.");
            foreach (GraphAuthoringTransitionProjection transition in pose.Transitions)
            {
                if (!(transition.Payload is CharacterPoseTransitionPayload))
                    throw new InvalidOperationException(
                        $"Pose Transition '{transition.TransitionId}' has a non-Pose payload.");
            }
        }

        public bool CanCreateTransition(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId sourceStateId,
            GraphAuthoringElementId targetStateId)
        {
            CharacterPoseStateMachineDocument pose = Require(document);
            bool source = sourceStateId.Equals(
                              pose.Entry.ElementId) ||
                          pose.Definition.States.Any(
                              value => value.StateId.Value == sourceStateId.Value) ||
                          pose.Definition.Aliases.Any(
                              value => value.AliasId.Value == sourceStateId.Value);
            bool target = pose.Definition.States.Any(
                value => value.StateId.Value == targetStateId.Value);
            return source && target;
        }

        public object CreateTransitionPayload(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId sourceStateId,
            GraphAuthoringElementId targetStateId)
        {
            CharacterPoseStateMachineDocument pose = Require(document);
            if (sourceStateId.Equals(pose.Entry.ElementId))
                return null;
            return m_CreateTransition?.Invoke(
                pose,
                sourceStateId,
                targetStateId);
        }

        public IReadOnlyList<GraphAuthoringFieldDescriptor> GetStateFields(
            GraphAuthoringStateProjection state)
        {
            if (state == null ||
                !state.CapabilityId.Equals(CharacterPoseGraphAuthoringCapabilities.StateMachineState))
            {
                throw new InvalidOperationException("Pose State details reject a non-Pose State.");
            }
            return CharacterPoseGraphAuthoringCapabilities.Catalog
                .Require(
                    CharacterPoseGraphAuthoringCapabilities.StateMachineState,
                    CharacterPoseGraphAuthoringCapabilities.Domain,
                    CharacterPoseGraphAuthoringCapabilities.StateMachine)
                .Fields
                .ToArray();
        }

        public IReadOnlyList<GraphAuthoringFieldDescriptor> GetTransitionFields(
            GraphAuthoringTransitionProjection transition)
        {
            if (!(transition?.Payload is CharacterPoseTransitionPayload))
                throw new InvalidOperationException("Pose transition details reject non-Pose payload.");
            return CharacterPoseGraphAuthoringCapabilities.Catalog
                .Require(
                    CharacterPoseGraphAuthoringCapabilities.StateMachineTransition,
                    CharacterPoseGraphAuthoringCapabilities.Domain,
                    CharacterPoseGraphAuthoringCapabilities.StateMachine)
                .Fields
                .ToArray();
        }

        public void OpenStateChildGraph(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId stateId)
        {
            CharacterPoseStateDefinition state = Require(document).Definition.States
                .SingleOrDefault(value => value.StateId.Value == stateId.Value) ??
                throw new InvalidOperationException($"Pose State '{stateId}' is missing.");
            m_OpenState(state);
        }

        public void OpenTransitionRule(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId transitionId)
        {
            CharacterPoseStateTransition transition = Require(document).Definition.Transitions
                .SingleOrDefault(value => value.TransitionId.Value == transitionId.Value) ??
                throw new InvalidOperationException($"Pose Transition '{transitionId}' is missing.");
            m_OpenTransition(transition);
        }

        static CharacterPoseStateMachineDocument Require(
            IGraphAuthoringStateMachineProjection document) =>
            document as CharacterPoseStateMachineDocument ??
            throw new InvalidOperationException("Pose StateMachine policy requires the Presentation adapter.");
    }

    public sealed class BtsmtlStateMachineDetailsDataSource :
        IGraphAuthoringStateMachineDetailsDataSource
    {
        public object ReadStateField(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringStateProjection state,
            GraphAuthoringFieldDescriptor field) =>
            throw new InvalidOperationException("BTSMTL State does not declare authoring fields.");

        public object ReadTransitionField(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringTransitionProjection transition,
            GraphAuthoringFieldDescriptor field)
        {
            BtsmtlGameplayTransitionPayload payload =
                transition?.Payload as BtsmtlGameplayTransitionPayload ??
                throw new InvalidOperationException("BTSMTL details reject non-Gameplay transition payload.");
            return field.FieldId.Value switch
            {
                "priority" => transition.Priority,
                "condition-rule-ownership" => payload.ConditionOwnership.ToString(),
                "condition-rule-id" => payload.ConditionRuleId,
                "shared-condition-rule-asset" => payload.SharedConditionRuleAsset,
                "interruption" => payload.Interruption.ToString(),
                _ => throw new InvalidOperationException(
                    $"BTSMTL Transition does not declare field '{field.FieldId}'.")
            };
        }
    }

    public sealed class CharacterPoseStateMachineDetailsDataSource :
        IGraphAuthoringStateMachineDetailsDataSource
    {
        public object ReadStateField(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringStateProjection state,
            GraphAuthoringFieldDescriptor field)
        {
            if (!(document is CharacterPoseStateMachineDocument))
                throw new InvalidOperationException("Pose details require the Presentation StateMachine adapter.");
            CharacterPoseStatePayload payload =
                state?.Payload as CharacterPoseStatePayload ??
                throw new InvalidOperationException("Pose details reject non-Pose state payload.");
            return field.FieldId.Value switch
            {
                "always-reset-on-entry" => payload.AlwaysResetOnEntry,
                _ => throw new InvalidOperationException(
                    $"Pose State does not declare field '{field.FieldId}'.")
            };
        }

        public object ReadTransitionField(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringTransitionProjection transition,
            GraphAuthoringFieldDescriptor field)
        {
            CharacterPoseTransitionPayload payload =
                transition?.Payload as CharacterPoseTransitionPayload ??
                throw new InvalidOperationException("Pose details reject non-Pose transition payload.");
            return field.FieldId.Value switch
            {
                "priority" => transition.Priority,
                "blend-logic" => payload.BlendLogic.ToString(),
                "duration-seconds" => payload.DurationSeconds,
                "blend-mode" => payload.BlendMode.ToString(),
                "custom-blend-curve" => payload.CustomBlendCurve,
                "blend-profile" => payload.BlendProfile,
                "source-readiness" => payload.Readiness.ToString(),
                "pose-rule-id" => "Configured Transition Rule",
                _ => throw new InvalidOperationException(
                    $"Pose Transition does not declare field '{field.FieldId}'.")
            };
        }
    }
}
