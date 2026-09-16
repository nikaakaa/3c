using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Equipment;
using ThirdPersonSimulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterEquipmentDomainRuntime : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly CharacterEquipmentPresentationProfile m_Profile;
        readonly CharacterEquipmentRigBindingCatalog m_RigCatalog;
        readonly Dictionary<EquipmentVisualBindingId, EquipmentVisualBindingDefinition> m_Bindings =
            new Dictionary<EquipmentVisualBindingId, EquipmentVisualBindingDefinition>();
        readonly Dictionary<EquipmentVisualBindingId, ResolvedBinding> m_ResolvedBindings =
            new Dictionary<EquipmentVisualBindingId, ResolvedBinding>();
        readonly Dictionary<EquipmentSlotId, ActiveVisual> m_ActiveVisuals =
            new Dictionary<EquipmentSlotId, ActiveVisual>();
        readonly Dictionary<EquipmentSlotId, EquipmentVisualSelection> m_LatestSelections =
            new Dictionary<EquipmentSlotId, EquipmentVisualSelection>();
        EquipmentVisualSelection[] m_PendingSelections = Array.Empty<EquipmentVisualSelection>();
        bool m_HasPendingSelections;
        bool m_Disposed;

        internal CharacterEquipmentDomainRuntime(
            ActorId actorId,
            CharacterEquipmentPresentationProfile profile,
            CharacterEquipmentRigBindingCatalog rigCatalog,
            bool initializeExternalState)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Equipment domain Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            if (!rigCatalog)
                throw new InvalidOperationException("Equipment domain requires an explicit Rig Binding Catalog.");
            m_RigCatalog = rigCatalog;
            m_RigCatalog.RequireValid();
            IReadOnlyList<EquipmentVisualBindingDefinition> bindings = m_Profile.VisualBindings;
            if (bindings.Count == 0)
                throw new InvalidOperationException($"Equipment Presentation Profile '{m_Profile.name}' has no visual bindings.");
            for (int i = 0; i < bindings.Count; i++)
            {
                EquipmentVisualBindingDefinition binding = bindings[i] ??
                    throw new InvalidOperationException($"Equipment visual binding #{i} is missing.");
                if (!m_Bindings.TryAdd(binding.VisualBindingId, binding))
                    throw new InvalidOperationException($"Equipment visual binding '{binding.VisualBindingId}' is duplicated.");
                m_ResolvedBindings.Add(binding.VisualBindingId, Resolve(binding));
            }
            if (initializeExternalState)
            {
                foreach (ResolvedBinding binding in m_ResolvedBindings.Values)
                {
                    for (int i = 0; i < binding.Renderers.Length; i++)
                        binding.Renderers[i].enabled = false;
                }
            }
        }

        internal void Capture(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            RequireAlive();
            if (selections == null)
                throw new ArgumentNullException(nameof(selections));
            var slots = new HashSet<EquipmentSlotId>();
            var pending = new EquipmentVisualSelection[selections.Count];
            for (int i = 0; i < selections.Count; i++)
            {
                EquipmentVisualSelection selection = selections[i];
                if (selection.ActorId != m_ActorId || !selection.SlotId.IsValid || selection.EquipmentRevision == 0 ||
                    !slots.Add(selection.SlotId))
                {
                    throw new InvalidOperationException("Equipment visual selection transaction is invalid or duplicated.");
                }
                if (selection.IsEquipped && !m_Bindings.ContainsKey(selection.VisualBindingId))
                    throw new InvalidOperationException(
                        $"Equipment visual binding '{selection.VisualBindingId}' is absent from '{m_Profile.name}'.");
                pending[i] = selection;
            }
            Array.Sort(pending, (left, right) => left.SlotId.CompareTo(right.SlotId));
            m_PendingSelections = pending;
            m_HasPendingSelections = true;
        }

        internal void Present()
        {
            RequireAlive();
            if (!m_HasPendingSelections)
                return;
            m_HasPendingSelections = false;
            for (int i = 0; i < m_PendingSelections.Length; i++)
                Apply(m_PendingSelections[i]);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            ReleaseAll();
            m_PendingSelections = new EquipmentVisualSelection[m_LatestSelections.Count];
            m_LatestSelections.Values.CopyTo(m_PendingSelections, 0);
            Array.Sort(m_PendingSelections, (left, right) => left.SlotId.CompareTo(right.SlotId));
            m_LatestSelections.Clear();
            m_HasPendingSelections = m_PendingSelections.Length != 0;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            ReleaseAll();
            m_LatestSelections.Clear();
            m_PendingSelections = Array.Empty<EquipmentVisualSelection>();
            m_HasPendingSelections = false;
        }

        void Apply(EquipmentVisualSelection selection)
        {
            if (m_LatestSelections.TryGetValue(selection.SlotId, out EquipmentVisualSelection current))
            {
                if (selection.EquipmentRevision < current.EquipmentRevision)
                    return;
                if (selection.EquipmentRevision == current.EquipmentRevision)
                {
                    if (!SameIdentity(selection, current))
                        throw new InvalidOperationException(
                            $"Equipment revision {selection.EquipmentRevision} contains a different Equipment or Visual Binding identity.");
                    if (selection.SourceTick < current.SourceTick)
                        return;
                    m_LatestSelections[selection.SlotId] = selection;
                    return;
                }
            }

            ResolvedBinding nextBinding = default;
            GameObject nextInstance = null;
            if (selection.IsEquipped)
            {
                if (!m_ResolvedBindings.TryGetValue(selection.VisualBindingId, out nextBinding))
                    throw new InvalidOperationException(
                        $"Equipment visual binding '{selection.VisualBindingId}' is absent from '{m_Profile.name}'.");
                if (nextBinding.Definition.Kind == EquipmentVisualBindingKind.SpawnedVisualAsset)
                {
                    nextInstance = Object.Instantiate(
                        nextBinding.Definition.VisualPrefab,
                        nextBinding.Socket,
                        false);
                    Transform transform = nextInstance.transform;
                    transform.localPosition = nextBinding.Definition.LocalPosition;
                    transform.localRotation = nextBinding.Definition.LocalRotation;
                    transform.localScale = nextBinding.Definition.LocalScale;
                }
            }

            if (m_ActiveVisuals.TryGetValue(selection.SlotId, out ActiveVisual active))
            {
                active.Release();
                m_ActiveVisuals.Remove(selection.SlotId);
            }
            if (selection.IsEquipped)
            {
                var replacement = new ActiveVisual(nextBinding, nextInstance);
                replacement.Activate();
                m_ActiveVisuals.Add(selection.SlotId, replacement);
            }
            m_LatestSelections[selection.SlotId] = selection;
        }

        ResolvedBinding Resolve(EquipmentVisualBindingDefinition definition)
        {
            if (definition.Kind == EquipmentVisualBindingKind.ExistingRigObject)
            {
                EquipmentRigObjectBinding rig = m_RigCatalog.RequireRigObject(definition.RigBindingId);
                var renderers = new Renderer[definition.RendererBindingIds.Count];
                for (int i = 0; i < definition.RendererBindingIds.Count; i++)
                {
                    string expected = definition.RendererBindingIds[i];
                    int match = -1;
                    for (int candidate = 0; candidate < rig.RendererBindingIds.Count; candidate++)
                    {
                        if (!string.Equals(rig.RendererBindingIds[candidate], expected, StringComparison.Ordinal))
                            continue;
                        if (match >= 0)
                            throw new InvalidOperationException(
                                $"Equipment Renderer Binding '{expected}' is duplicated in Rig '{definition.RigBindingId}'.");
                        match = candidate;
                    }
                    if (match < 0 || !rig.Renderers[match])
                        throw new InvalidOperationException(
                            $"Equipment Renderer Binding '{expected}' is absent from Rig '{definition.RigBindingId}'.");
                    renderers[i] = rig.Renderers[match];
                }
                return new ResolvedBinding(definition, renderers, null);
            }
            if (definition.Kind == EquipmentVisualBindingKind.SpawnedVisualAsset)
            {
                return new ResolvedBinding(
                    definition,
                    Array.Empty<Renderer>(),
                    m_RigCatalog.RequireSocket(definition.SocketBindingId));
            }
            throw new InvalidOperationException(
                $"Equipment visual binding '{definition.VisualBindingId}' has unsupported kind '{definition.Kind}'.");
        }

        void ReleaseAll()
        {
            foreach (ActiveVisual active in m_ActiveVisuals.Values)
                active.Release();
            m_ActiveVisuals.Clear();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterEquipmentDomainRuntime));
        }

        static bool SameIdentity(EquipmentVisualSelection left, EquipmentVisualSelection right) =>
            left.ActorId == right.ActorId && left.SlotId == right.SlotId && left.EquipmentId == right.EquipmentId &&
            left.VisualBindingId == right.VisualBindingId && left.EquipmentRevision == right.EquipmentRevision;

        readonly struct ResolvedBinding
        {
            internal ResolvedBinding(
                EquipmentVisualBindingDefinition definition,
                Renderer[] renderers,
                Transform socket)
            {
                Definition = definition;
                Renderers = renderers;
                Socket = socket;
            }

            internal EquipmentVisualBindingDefinition Definition { get; }
            internal Renderer[] Renderers { get; }
            internal Transform Socket { get; }
        }

        sealed class ActiveVisual
        {
            readonly ResolvedBinding m_Binding;
            readonly GameObject m_Instance;

            public ActiveVisual(ResolvedBinding binding, GameObject instance)
            {
                m_Binding = binding;
                m_Instance = instance;
            }

            public void Activate()
            {
                for (int i = 0; i < m_Binding.Renderers.Length; i++)
                    m_Binding.Renderers[i].enabled = true;
            }

            public void Release()
            {
                for (int i = 0; i < m_Binding.Renderers.Length; i++)
                {
                    if (m_Binding.Renderers[i])
                        m_Binding.Renderers[i].enabled = false;
                }
                if (m_Instance)
                    Object.Destroy(m_Instance);
            }
        }
    }
}