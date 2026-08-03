using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseStateMachineLayoutElement
    {
        [SerializeField] string m_ElementId = string.Empty;
        [SerializeField] Vector2 m_Position;

        public string ElementId => m_ElementId ?? string.Empty;
        public Vector2 Position => m_Position;

        public CharacterPoseStateMachineLayoutElement() { }

        public CharacterPoseStateMachineLayoutElement(
            string elementId,
            Vector2 position)
        {
            m_ElementId = string.IsNullOrWhiteSpace(elementId)
                ? throw new ArgumentException(
                    "Pose StateMachine layout element identity is missing.",
                    nameof(elementId))
                : elementId.Trim();
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y))
                throw new ArgumentException(
                    "Pose StateMachine layout position must be finite.",
                    nameof(position));
            m_Position = position;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateMachineLayout
    {
        [SerializeField] string m_StateMachineId = string.Empty;
        [SerializeField] CharacterPoseStateMachineLayoutElement[] m_Elements =
            Array.Empty<CharacterPoseStateMachineLayoutElement>();

        public PoseStateMachineId StateMachineId =>
            string.IsNullOrWhiteSpace(m_StateMachineId)
                ? default
                : new PoseStateMachineId(m_StateMachineId);
        public IReadOnlyList<CharacterPoseStateMachineLayoutElement> Elements =>
            m_Elements ?? Array.Empty<CharacterPoseStateMachineLayoutElement>();

        public CharacterPoseStateMachineLayout() { }

        public CharacterPoseStateMachineLayout(
            PoseStateMachineId stateMachineId,
            CharacterPoseStateMachineLayoutElement[] elements)
        {
            m_StateMachineId = stateMachineId.IsValid
                ? stateMachineId.Value
                : throw new ArgumentException(
                    "Pose StateMachine layout owner identity is invalid.",
                    nameof(stateMachineId));
            m_Elements = (elements ??
                          Array.Empty<CharacterPoseStateMachineLayoutElement>())
                .OrderBy(value => value?.ElementId, StringComparer.Ordinal)
                .ToArray();
            RequireValidElements(m_Elements);
        }

        internal static void RequireValidElements(
            IReadOnlyList<CharacterPoseStateMachineLayoutElement> elements)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseStateMachineLayoutElement element in
                     elements ??
                     Array.Empty<CharacterPoseStateMachineLayoutElement>())
            {
                if (element == null ||
                    string.IsNullOrWhiteSpace(element.ElementId) ||
                    !identities.Add(element.ElementId) ||
                    !float.IsFinite(element.Position.x) ||
                    !float.IsFinite(element.Position.y))
                {
                    throw new InvalidOperationException(
                        "Pose StateMachine layout contains a missing, duplicate or non-finite element.");
                }
            }
        }
    }

    [CreateAssetMenu(fileName = "CharacterPresentationPoseGraph", menuName = "3C/Character/Presentation Pose Graph")]
    public sealed class CharacterPresentationPoseGraphAsset : ScriptableObject
    {
        [SerializeField] CharacterTypedPoseGraph m_TypedGraph;
        [SerializeField] CharacterTypedPoseGraph[] m_TypedGraphCatalog = Array.Empty<CharacterTypedPoseGraph>();
        [SerializeField] CharacterPoseStateMachineLayout[] m_StateMachineLayouts =
            Array.Empty<CharacterPoseStateMachineLayout>();
        [SerializeField] CharacterPresentationPoseSourceSlot[] m_SourceSlots =
            Array.Empty<CharacterPresentationPoseSourceSlot>();

        public CharacterTypedPoseGraph Graph => m_TypedGraph;
        public IReadOnlyList<CharacterTypedPoseGraph> GraphCatalog => m_TypedGraphCatalog ?? Array.Empty<CharacterTypedPoseGraph>();
        public IReadOnlyList<CharacterPoseStateMachineLayout> StateMachineLayouts =>
            m_StateMachineLayouts ?? Array.Empty<CharacterPoseStateMachineLayout>();
        public IReadOnlyList<CharacterPresentationPoseSourceSlot> SourceSlots =>
            m_SourceSlots ?? Array.Empty<CharacterPresentationPoseSourceSlot>();

        public void SetGraph(CharacterTypedPoseGraph graph)
        {
            m_TypedGraph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public void SetSourceSlots(CharacterPresentationPoseSourceSlot[] slots)
        {
            CharacterPresentationPoseSourceSlot[] values = slots ??
                Array.Empty<CharacterPresentationPoseSourceSlot>();
            var references = new HashSet<CharacterPresentationPoseSourceSlot>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < values.Length; i++)
            {
                CharacterPresentationPoseSourceSlot slot = values[i];
                if (!slot || !references.Add(slot))
                    throw new InvalidOperationException($"Pose Source Slot #{i} is missing or duplicated.");
                slot.RequireValid();
                if (!names.Add(slot.name.Trim()))
                    throw new InvalidOperationException($"Pose Source Slot name '{slot.name}' is duplicated.");
            }
            m_SourceSlots = values;
        }

        public CharacterTypedPoseGraph RequireGraph(PoseGraphId graphId)
        {
            if (!TryGetGraph(graphId, out CharacterTypedPoseGraph graph))
                throw new InvalidOperationException($"Pose Graph '{graphId}' does not exist in '{name}'.");
            return graph;
        }

        public bool TryGetGraph(PoseGraphId graphId, out CharacterTypedPoseGraph graph)
        {
            graph = null;
            if (!graphId.IsValid)
                return false;
            if (m_TypedGraph != null && m_TypedGraph.GraphId == graphId)
            {
                graph = m_TypedGraph;
                return true;
            }
            CharacterTypedPoseGraph[] catalog = m_TypedGraphCatalog ?? Array.Empty<CharacterTypedPoseGraph>();
            for (int i = 0; i < catalog.Length; i++)
            {
                CharacterTypedPoseGraph candidate = catalog[i];
                if (candidate != null && candidate.GraphId == graphId)
                {
                    graph = candidate;
                    return true;
                }
            }
            return false;
        }

        public void AddGraph(CharacterTypedPoseGraph graph)
        {
            if (graph == null || !graph.GraphId.IsValid)
                throw new ArgumentException("Pose Graph catalog record is invalid.", nameof(graph));
            if (TryGetGraph(graph.GraphId, out _))
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' already exists in '{name}'.");
            m_TypedGraphCatalog = (m_TypedGraphCatalog ?? Array.Empty<CharacterTypedPoseGraph>()).Concat(new[] { graph }).ToArray();
        }

        public void ReplaceGraph(CharacterTypedPoseGraph graph)
        {
            if (graph == null || !graph.GraphId.IsValid)
                throw new ArgumentException("Pose Graph replacement is invalid.", nameof(graph));
            if (m_TypedGraph != null && m_TypedGraph.GraphId == graph.GraphId)
            {
                m_TypedGraph = graph;
                return;
            }
            CharacterTypedPoseGraph[] catalog = m_TypedGraphCatalog ?? Array.Empty<CharacterTypedPoseGraph>();
            int index = Array.FindIndex(catalog, value => value != null && value.GraphId == graph.GraphId);
            if (index < 0)
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' does not exist in '{name}'.");
            catalog[index] = graph;
            m_TypedGraphCatalog = catalog;
        }

        public void RemoveGraph(PoseGraphId graphId)
        {
            if (!graphId.IsValid)
                throw new ArgumentException("Pose Graph identity is invalid.", nameof(graphId));
            if (m_TypedGraph != null && m_TypedGraph.GraphId == graphId)
                throw new InvalidOperationException("The root Pose Graph cannot be removed.");
            CharacterTypedPoseGraph[] catalog = m_TypedGraphCatalog ?? Array.Empty<CharacterTypedPoseGraph>();
            CharacterTypedPoseGraph[] next = catalog.Where(value => value != null && value.GraphId != graphId).ToArray();
            if (next.Length == catalog.Length)
                throw new InvalidOperationException($"Pose Graph '{graphId}' does not exist in '{name}'.");
            m_TypedGraphCatalog = next;
        }

        public IEnumerable<CharacterTypedPoseGraph> EnumerateGraphs()
        {
            if (m_TypedGraph != null)
                yield return m_TypedGraph;
            CharacterTypedPoseGraph[] catalog = m_TypedGraphCatalog ?? Array.Empty<CharacterTypedPoseGraph>();
            for (int i = 0; i < catalog.Length; i++)
                yield return catalog[i];
        }

        public IEnumerable<CharacterPoseStateMachineDefinition>
            EnumerateStateMachines() =>
            EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Select(value => value.StateMachine)
                .Where(value => value != null);

        public IReadOnlyList<CharacterPoseStateMachineLayoutElement>
            GetExplicitStateMachineLayout(PoseStateMachineId stateMachineId)
        {
            CharacterPoseStateMachineLayout layout =
                FindExplicitStateMachineLayout(stateMachineId);
            return layout?.Elements ??
                   Array.Empty<CharacterPoseStateMachineLayoutElement>();
        }

        public Vector2 ResolveStateMachineElementPosition(
            CharacterPoseStateMachineDefinition stateMachine,
            string elementId)
        {
            if (stateMachine == null ||
                !stateMachine.StateMachineId.IsValid ||
                string.IsNullOrWhiteSpace(elementId))
                throw new ArgumentException(
                    "Pose StateMachine layout lookup is invalid.");
            CharacterPoseStateMachineDefinition owned =
                RequireStateMachine(stateMachine.StateMachineId);
            if (!ReferenceEquals(owned, stateMachine))
                throw new InvalidOperationException(
                    $"Pose StateMachine '{stateMachine.StateMachineId}' is not owned by '{name}'.");
            CharacterPoseStateMachineLayoutElement explicitElement =
                GetExplicitStateMachineLayout(stateMachine.StateMachineId)
                    .SingleOrDefault(value => string.Equals(
                        value.ElementId,
                        elementId,
                        StringComparison.Ordinal));
            if (explicitElement != null)
                return explicitElement.Position;
            if (string.Equals(
                    stateMachine.Entry.EntryId.Value,
                    elementId,
                    StringComparison.Ordinal))
                return new Vector2(-360f, 0f);
            string[] states = stateMachine.States
                .Where(value => value != null)
                .Select(value => value.StateId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            int stateIndex = Array.IndexOf(states, elementId);
            if (stateIndex >= 0)
                return new Vector2(0f, stateIndex * 160f);
            string[] aliases = stateMachine.Aliases
                .Where(value => value != null)
                .Select(value => value.AliasId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            int aliasIndex = Array.IndexOf(aliases, elementId);
            if (aliasIndex >= 0)
                return new Vector2(-180f, (aliasIndex + 1) * 160f);
            throw new InvalidOperationException(
                $"Pose StateMachine '{stateMachine.StateMachineId}' has no layout element '{elementId}'.");
        }

        public void SetStateMachineLayoutElement(
            PoseStateMachineId stateMachineId,
            string elementId,
            Vector2 position)
        {
            CharacterPoseStateMachineDefinition machine =
                RequireStateMachine(stateMachineId);
            RequireKnownLayoutElement(machine, elementId);
            var elements = GetExplicitStateMachineLayout(stateMachineId)
                .Where(value => !string.Equals(
                    value.ElementId,
                    elementId,
                    StringComparison.Ordinal))
                .Concat(new[]
                {
                    new CharacterPoseStateMachineLayoutElement(
                        elementId,
                        position)
                })
                .ToArray();
            SetStateMachineLayout(stateMachineId, elements);
        }

        public void RemoveStateMachineLayoutElement(
            PoseStateMachineId stateMachineId,
            string elementId)
        {
            RequireStateMachine(stateMachineId);
            CharacterPoseStateMachineLayout current =
                FindExplicitStateMachineLayout(stateMachineId);
            if (current == null)
                return;
            CharacterPoseStateMachineLayoutElement[] elements = current.Elements
                .Where(value => !string.Equals(
                    value.ElementId,
                    elementId,
                    StringComparison.Ordinal))
                .ToArray();
            if (elements.Length == current.Elements.Count)
                return;
            SetStateMachineLayout(stateMachineId, elements);
        }

        public void SetStateMachineLayout(
            PoseStateMachineId stateMachineId,
            CharacterPoseStateMachineLayoutElement[] elements)
        {
            CharacterPoseStateMachineDefinition machine =
                RequireStateMachine(stateMachineId);
            CharacterPoseStateMachineLayoutElement[] values = elements ??
                Array.Empty<CharacterPoseStateMachineLayoutElement>();
            CharacterPoseStateMachineLayout.RequireValidElements(values);
            foreach (CharacterPoseStateMachineLayoutElement element in values)
                RequireKnownLayoutElement(machine, element.ElementId);
            var layouts = (m_StateMachineLayouts ??
                           Array.Empty<CharacterPoseStateMachineLayout>())
                .Where(value => value != null &&
                                !value.StateMachineId.Equals(stateMachineId))
                .ToList();
            if (values.Length > 0)
                layouts.Add(new CharacterPoseStateMachineLayout(
                    stateMachineId,
                    values));
            m_StateMachineLayouts = layouts
                .OrderBy(value => value.StateMachineId)
                .ToArray();
        }

        CharacterPoseStateMachineLayout FindExplicitStateMachineLayout(
            PoseStateMachineId stateMachineId)
        {
            if (!stateMachineId.IsValid)
                throw new ArgumentException(
                    "Pose StateMachine layout owner identity is invalid.",
                    nameof(stateMachineId));
            CharacterPoseStateMachineLayout[] matches =
                (m_StateMachineLayouts ??
                 Array.Empty<CharacterPoseStateMachineLayout>())
                .Where(value => value != null &&
                                value.StateMachineId.Equals(stateMachineId))
                .ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{stateMachineId}' has duplicate layout owners.");
            if (matches.Length == 1)
                CharacterPoseStateMachineLayout.RequireValidElements(
                    matches[0].Elements);
            return matches.SingleOrDefault();
        }

        CharacterPoseStateMachineDefinition RequireStateMachine(
            PoseStateMachineId stateMachineId)
        {
            CharacterPoseStateMachineDefinition[] matches =
                EnumerateStateMachines()
                    .Where(value => value.StateMachineId.Equals(stateMachineId))
                    .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Pose StateMachine '{stateMachineId}' must have exactly one root-owned node.");
        }

        static void RequireKnownLayoutElement(
            CharacterPoseStateMachineDefinition machine,
            string elementId)
        {
            bool known = string.Equals(
                             machine.Entry.EntryId.Value,
                             elementId,
                             StringComparison.Ordinal) ||
                         machine.States.Any(value => value != null &&
                             string.Equals(
                                 value.StateId.Value,
                                 elementId,
                                 StringComparison.Ordinal)) ||
                         machine.Aliases.Any(value => value != null &&
                             string.Equals(
                                 value.AliasId.Value,
                                 elementId,
                                 StringComparison.Ordinal));
            if (!known)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{machine.StateMachineId}' has no Entry, State or Alias '{elementId}'.");
        }
    }
}
