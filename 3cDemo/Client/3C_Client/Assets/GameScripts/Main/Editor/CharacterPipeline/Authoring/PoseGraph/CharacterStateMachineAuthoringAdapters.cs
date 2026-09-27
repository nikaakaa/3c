using BTSMTL.Authoring.Editor;
using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{

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
            CustomBlendCurveSlot = transition.CustomBlendCurveSlot;
            BlendProfileSlot = transition.BlendProfileSlot;
        }

        public GraphAuthoringStateMachineSemanticKind SemanticKind =>
            GraphAuthoringStateMachineSemanticKind.Pose;
        public CharacterPoseTransitionRuleGraph Rule { get; }
        public AnimationTransitionBlendLogic BlendLogic { get; }
        public float DurationSeconds { get; }
        public CharacterAnimationBlendMode BlendMode { get; }
        public CharacterPoseResourceSlot CustomBlendCurveSlot { get; }
        public CharacterPoseResourceSlot BlendProfileSlot { get; }
        public CharacterPoseTransitionReadinessRequirement Readiness =>
            CharacterPoseTransitionReadinessRequirement.TargetPoseSourceReady;
    }

    internal static class CharacterPoseAuthoringDisplayNames
    {
        public static string ForParameter(PoseParameterId parameterId) =>
            HumanizeIdentity(parameterId.Value, "Parameter");

        public static string ForIdentity(string identity) =>
            HumanizeIdentity(identity, "Unnamed");

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
        readonly PoseGraphId m_OwnerGraphId;
        readonly PoseNodeId m_OwnerNodeId;
        CharacterPoseStateMachineDefinition m_Definition => m_Asset.RequireGraph(m_OwnerGraphId)
            .RequireNode(m_OwnerNodeId).RequirePayload<CharacterPoseStateMachineNodePayload>().StateMachine;

        public CharacterPoseStateMachineDocument(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseStateMachineDefinition definition)
        {
            m_Asset = asset ? asset : throw new ArgumentNullException(nameof(asset));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            (m_OwnerGraphId, m_OwnerNodeId) = CharacterPoseGraphAssetMutationOwner.ResolveStateMachineOwner(asset, definition.StateMachineId);
            CharacterPoseGraphCapabilityProjector.EnsureRegistered();
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

    public sealed class CharacterPoseStateCreation
    {
        public CharacterPoseStateCreation(
            CharacterPoseStateDefinition state,
            CharacterPoseCanvasGraph graph)
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
        public CharacterPoseCanvasGraph Graph { get; }
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
                case GraphAuthoringMutationKind.ConfigureStateAlias:
                {
                    var edit = request.Value as GraphAuthoringStateAliasProjection ??
                        throw new InvalidOperationException("状态别名编辑需要完整的名称与成员。");
                    if (!edit.AliasId.Equals(request.TargetId))
                        throw new InvalidOperationException("状态别名编辑目标不一致。");
                    CharacterPoseStateAlias current = pose.Definition.Aliases.Single(value => value.AliasId.Value == request.TargetId.Value);
                    CharacterPoseStateTransitionSource[] sources = edit.SourceIds.Select(id =>
                    {
                        if (pose.Definition.States.Any(state => state.StateId.Value == id.Value))
                            return CharacterPoseStateTransitionSource.FromState(new PoseStateId(id.Value));
                        CharacterPoseStateAlias source = pose.Definition.Aliases.Single(value => value.AliasId.Value == id.Value);
                        return CharacterPoseStateTransitionSource.FromAlias(source.AliasId);
                    }).ToArray();
                    var replacement = new CharacterPoseStateAlias(current.AliasId, edit.DisplayName, sources);
                    transaction.Add(new ConfigurePoseStateMachineMutation(machineId, pose.Definition.Entry,
                        pose.Definition.Aliases.Select(value => value.AliasId == current.AliasId ? replacement : value).ToArray(),
                        pose.Definition.MaxTransitionsPerFrame));
                    return;
                }
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
            CharacterPoseGraphCapabilityProjector.EnsureRegistered();
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
            return CharacterPoseGraphCapabilityProjector.Catalog
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
            return CharacterPoseGraphCapabilityProjector.Catalog
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
                "display-name" => state.DisplayName,
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
                "custom-blend-curve" => payload.CustomBlendCurveSlot,
                "blend-profile" => payload.BlendProfileSlot,
                "source-readiness" => payload.Readiness.ToString(),
                "pose-rule-id" => "Configured Transition Rule",
                _ => throw new InvalidOperationException(
                    $"Pose Transition does not declare field '{field.FieldId}'.")
            };
        }
    }
}
