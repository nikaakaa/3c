using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner.Editor
{
    public enum GraphAuthoringStateMachineSemanticKind : byte
    {
        Gameplay = 1,
        Pose = 2
    }

    public interface IGraphAuthoringTransitionPayload
    {
        GraphAuthoringStateMachineSemanticKind SemanticKind { get; }
    }

    public interface IGraphAuthoringStatePayload
    {
        GraphAuthoringStateMachineSemanticKind SemanticKind { get; }
    }

    public sealed class GraphAuthoringStateMachineEntryProjection
    {
        public GraphAuthoringStateMachineEntryProjection(GraphAuthoringElementId elementId, GraphAuthoringElementId targetStateId, Vector2 position)
        {
            ElementId = elementId.IsValid ? elementId : throw new ArgumentException("StateMachine entry identity is missing.", nameof(elementId));
            TargetStateId = targetStateId;
            Position = position;
        }

        public GraphAuthoringElementId ElementId { get; }
        public GraphAuthoringElementId TargetStateId { get; }
        public Vector2 Position { get; }
    }

    public sealed class GraphAuthoringStateProjection
    {
        public GraphAuthoringStateProjection(
            GraphAuthoringElementId stateId,
            GraphAuthoringCapabilityId capabilityId,
            string displayName,
            Vector2 position,
            IGraphAuthoringStatePayload payload,
            GraphAuthoringElementId childGraphId = default,
            string status = "")
        {
            StateId = stateId.IsValid ? stateId : throw new ArgumentException("State identity is missing.", nameof(stateId));
            CapabilityId = capabilityId.IsValid ? capabilityId : throw new ArgumentException("State capability is missing.", nameof(capabilityId));
            DisplayName = displayName ?? string.Empty;
            Position = position;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            ChildGraphId = childGraphId;
            Status = status ?? string.Empty;
        }

        public GraphAuthoringElementId StateId { get; }
        public GraphAuthoringCapabilityId CapabilityId { get; }
        public string DisplayName { get; }
        public Vector2 Position { get; }
        public IGraphAuthoringStatePayload Payload { get; }
        public GraphAuthoringElementId ChildGraphId { get; }
        public string Status { get; }
    }

    public sealed class GraphAuthoringStateAliasProjection
    {
        public GraphAuthoringStateAliasProjection(
            GraphAuthoringElementId aliasId,
            IReadOnlyList<GraphAuthoringElementId> sourceIds,
            string displayName,
            Vector2 position)
        {
            AliasId = aliasId.IsValid ? aliasId : throw new ArgumentException("State alias identity is missing.", nameof(aliasId));
            if (sourceIds == null || sourceIds.Count == 0)
                throw new ArgumentException("State alias sources are missing.", nameof(sourceIds));
            var unique = new HashSet<GraphAuthoringElementId>();
            for (int i = 0; i < sourceIds.Count; i++)
            {
                if (!sourceIds[i].IsValid || !unique.Add(sourceIds[i]))
                    throw new ArgumentException("State alias source identity is missing or duplicated.", nameof(sourceIds));
            }
            SourceIds = new List<GraphAuthoringElementId>(sourceIds);
            DisplayName = displayName ?? string.Empty;
            Position = position;
        }

        public GraphAuthoringElementId AliasId { get; }
        public IReadOnlyList<GraphAuthoringElementId> SourceIds { get; }
        public string DisplayName { get; }
        public Vector2 Position { get; }
    }

    public sealed class GraphAuthoringTransitionProjection
    {
        public GraphAuthoringTransitionProjection(
            GraphAuthoringElementId transitionId,
            GraphAuthoringElementId sourceStateId,
            GraphAuthoringElementId targetStateId,
            GraphAuthoringCapabilityId capabilityId,
            int priority,
            IGraphAuthoringTransitionPayload payload,
            GraphAuthoringElementId ruleOwnerId = default,
            string displayName = "")
        {
            TransitionId = transitionId.IsValid ? transitionId : throw new ArgumentException("Transition identity is missing.", nameof(transitionId));
            SourceStateId = sourceStateId.IsValid ? sourceStateId : throw new ArgumentException("Transition source is missing.", nameof(sourceStateId));
            TargetStateId = targetStateId.IsValid ? targetStateId : throw new ArgumentException("Transition target is missing.", nameof(targetStateId));
            CapabilityId = capabilityId.IsValid ? capabilityId : throw new ArgumentException("Transition capability is missing.", nameof(capabilityId));
            Priority = priority >= 0 ? priority : throw new ArgumentOutOfRangeException(nameof(priority));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            RuleOwnerId = ruleOwnerId;
            DisplayName = displayName ?? string.Empty;
        }

        public GraphAuthoringElementId TransitionId { get; }
        public GraphAuthoringElementId SourceStateId { get; }
        public GraphAuthoringElementId TargetStateId { get; }
        public GraphAuthoringCapabilityId CapabilityId { get; }
        public int Priority { get; }
        public IGraphAuthoringTransitionPayload Payload { get; }
        public GraphAuthoringElementId RuleOwnerId { get; }
        public string DisplayName { get; }
    }

    public interface IGraphAuthoringStateMachineProjection : IGraphAuthoringDocumentProjection
    {
        GraphAuthoringStateMachineSemanticKind SemanticKind { get; }
        GraphAuthoringStateMachineEntryProjection Entry { get; }
        IReadOnlyList<GraphAuthoringStateProjection> States { get; }
        IReadOnlyList<GraphAuthoringStateAliasProjection> Aliases { get; }
        IReadOnlyList<GraphAuthoringTransitionProjection> Transitions { get; }
    }

    public interface IGraphAuthoringStateMachinePolicy
    {
        GraphAuthoringStateMachineSemanticKind SemanticKind { get; }
        bool PersistsLayout { get; }
        void ValidateDocument(IGraphAuthoringStateMachineProjection document);
        bool CanCreateTransition(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId sourceStateId,
            GraphAuthoringElementId targetStateId);
        object CreateTransitionPayload(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringElementId sourceStateId,
            GraphAuthoringElementId targetStateId);
        IReadOnlyList<GraphAuthoringFieldDescriptor> GetStateFields(GraphAuthoringStateProjection state);
        IReadOnlyList<GraphAuthoringFieldDescriptor> GetTransitionFields(GraphAuthoringTransitionProjection transition);
        void OpenStateChildGraph(IGraphAuthoringStateMachineProjection document, GraphAuthoringElementId stateId);
        void OpenTransitionRule(IGraphAuthoringStateMachineProjection document, GraphAuthoringElementId transitionId);
    }

    public static class GraphAuthoringStateMachineProjectionValidator
    {
        public static void RequireValid(IGraphAuthoringStateMachineProjection document)
        {
            if (document == null || document.Entry == null)
                throw new InvalidOperationException("StateMachine projection or Entry is missing.");
            var elements = new HashSet<GraphAuthoringElementId> { document.Entry.ElementId };
            var states = new HashSet<GraphAuthoringElementId>();
            foreach (GraphAuthoringStateProjection state in document.States ?? Array.Empty<GraphAuthoringStateProjection>())
            {
                if (state == null || !states.Add(state.StateId) || !elements.Add(state.StateId))
                    throw new InvalidOperationException("StateMachine contains a missing or duplicate State identity.");
                if (state.Payload == null || state.Payload.SemanticKind != document.SemanticKind)
                    throw new InvalidOperationException(
                        $"State '{state.StateId}' payload cannot enter '{document.SemanticKind}' StateMachine.");
            }
            if (document.Entry.TargetStateId.IsValid && !states.Contains(document.Entry.TargetStateId))
                throw new InvalidOperationException("StateMachine Entry target is not a State.");

            var aliases = new HashSet<GraphAuthoringElementId>();
            foreach (GraphAuthoringStateAliasProjection alias in document.Aliases ?? Array.Empty<GraphAuthoringStateAliasProjection>())
            {
                if (alias == null || !aliases.Add(alias.AliasId) || !elements.Add(alias.AliasId))
                    throw new InvalidOperationException("StateMachine contains a missing or duplicate State Alias identity.");
            }
            foreach (GraphAuthoringStateAliasProjection alias in document.Aliases ?? Array.Empty<GraphAuthoringStateAliasProjection>())
            {
                for (int i = 0; i < alias.SourceIds.Count; i++)
                {
                    GraphAuthoringElementId sourceId = alias.SourceIds[i];
                    if (!states.Contains(sourceId) && !aliases.Contains(sourceId))
                        throw new InvalidOperationException($"State Alias '{alias.AliasId}' source '{sourceId}' is missing.");
                    if (sourceId.Equals(alias.AliasId))
                        throw new InvalidOperationException($"State Alias '{alias.AliasId}' cannot directly reference itself.");
                }
            }

            var transitions = new HashSet<GraphAuthoringElementId>();
            foreach (GraphAuthoringTransitionProjection transition in document.Transitions ?? Array.Empty<GraphAuthoringTransitionProjection>())
            {
                if (transition == null || !transitions.Add(transition.TransitionId) || !elements.Add(transition.TransitionId))
                    throw new InvalidOperationException("StateMachine contains a missing or duplicate Transition identity.");
                if (transition.Payload == null || transition.Payload.SemanticKind != document.SemanticKind)
                    throw new InvalidOperationException(
                        $"Transition '{transition.TransitionId}' payload cannot enter '{document.SemanticKind}' StateMachine.");
                if (!states.Contains(transition.SourceStateId) && !aliases.Contains(transition.SourceStateId) ||
                    !states.Contains(transition.TargetStateId))
                {
                    throw new InvalidOperationException($"Transition '{transition.TransitionId}' endpoint is missing.");
                }
            }
        }
    }
}
