using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterEquipmentVisualRuntime : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly Dictionary<EquipmentVisualBindingId, ResolvedBinding> m_Bindings =
            new Dictionary<EquipmentVisualBindingId, ResolvedBinding>();
        readonly Dictionary<EquipmentSlotId, ActiveVisual> m_ActiveVisuals =
            new Dictionary<EquipmentSlotId, ActiveVisual>();
        readonly Dictionary<EquipmentSlotId, EquipmentVisualSelection> m_LatestSelections =
            new Dictionary<EquipmentSlotId, EquipmentVisualSelection>();
        EquipmentVisualSelection[] m_PendingSelections = Array.Empty<EquipmentVisualSelection>();
        bool m_HasPendingSelections;
        bool m_Invalid;
        string m_InvalidReason = string.Empty;
        bool m_Disposed;

        public CharacterEquipmentVisualRuntime(
            ActorId actorId,
            CharacterPresentationProjection projection,
            CharacterEquipmentRigBindingCatalog rigCatalog,
            RuntimeDiagnosticsContext diagnostics)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Equipment Visual Runtime Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (projection.EquipmentVisualBindings.Count == 0)
                return;
            if (!rigCatalog)
                throw new InvalidOperationException("Equipment Presentation Projection requires an explicit Rig Binding Catalog.");
            rigCatalog.RequireValid();

            for (int i = 0; i < projection.EquipmentVisualBindings.Count; i++)
            {
                EquipmentVisualProjectionBinding binding = projection.EquipmentVisualBindings[i] ??
                    throw new InvalidOperationException($"Equipment visual binding #{i} is missing.");
                if (!m_Bindings.TryAdd(binding.VisualBindingId, Resolve(binding, rigCatalog)))
                    throw new InvalidOperationException($"Equipment visual binding '{binding.VisualBindingId}' is duplicated.");
            }
            foreach (ResolvedBinding binding in m_Bindings.Values)
                for (int rendererIndex = 0; rendererIndex < binding.Renderers.Length; rendererIndex++)
                    binding.Renderers[rendererIndex].enabled = false;
        }

        public bool IsValid => !m_Invalid;
        public string InvalidReason => m_InvalidReason;

        public void Capture(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            RequireAlive();
            if (selections == null)
                throw new ArgumentNullException(nameof(selections));
            var slots = new HashSet<EquipmentSlotId>();
            var copy = new EquipmentVisualSelection[selections.Count];
            for (int i = 0; i < selections.Count; i++)
            {
                EquipmentVisualSelection selection = selections[i];
                if (selection.ActorId != m_ActorId || !selection.SlotId.IsValid || selection.EquipmentRevision == 0 ||
                    !slots.Add(selection.SlotId))
                {
                    throw new InvalidOperationException("Equipment visual selection transaction is invalid or duplicated.");
                }
                copy[i] = selection;
            }
            Array.Sort(copy, (left, right) => left.SlotId.CompareTo(right.SlotId));
            m_PendingSelections = copy;
            m_HasPendingSelections = true;
        }

        public void Present()
        {
            RequireAlive();
            if (m_Invalid)
                throw new InvalidOperationException(m_InvalidReason);
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
            m_PendingSelections = m_LatestSelections.Values
                .OrderBy(value => value.SlotId.Value, StringComparer.Ordinal)
                .ToArray();
            m_LatestSelections.Clear();
            m_HasPendingSelections = m_PendingSelections.Length != 0;
            m_Invalid = false;
            m_InvalidReason = string.Empty;
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
                {
                    Publish("Rejected", selection, "stale_revision");
                    return;
                }
                if (selection.EquipmentRevision == current.EquipmentRevision)
                {
                    if (!SameIdentity(selection, current))
                        Fail(selection, "same revision contains a different Equipment or Visual Binding identity");
                    if (selection.SourceTick < current.SourceTick)
                    {
                        Publish("Rejected", selection, "stale_source_tick");
                        return;
                    }
                    m_LatestSelections[selection.SlotId] = selection;
                    return;
                }
            }

            ResolvedBinding nextBinding = null;
            GameObject nextInstance = null;
            if (selection.IsEquipped)
            {
                if (!m_Bindings.TryGetValue(selection.VisualBindingId, out nextBinding))
                    Fail(selection, $"visual binding '{selection.VisualBindingId}' is absent from Projection");
                if (nextBinding.Source.Kind == EquipmentVisualBindingKind.SpawnedVisualAsset)
                {
                    nextInstance = Object.Instantiate(nextBinding.Source.VisualPrefab, nextBinding.Socket, false);
                    if (!nextInstance)
                        Fail(selection, $"visual prefab for '{selection.VisualBindingId}' could not be instantiated");
                    Transform transform = nextInstance.transform;
                    transform.localPosition = nextBinding.Source.LocalPosition;
                    transform.localRotation = nextBinding.Source.LocalRotation;
                    transform.localScale = nextBinding.Source.LocalScale;
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
            Publish("Applied", selection, selection.IsEquipped ? selection.VisualBindingId.Value : "unequipped");
        }

        ResolvedBinding Resolve(
            EquipmentVisualProjectionBinding source,
            CharacterEquipmentRigBindingCatalog catalog)
        {
            if (source.Kind == EquipmentVisualBindingKind.ExistingRigObject)
            {
                EquipmentRigObjectBinding rig = catalog.RequireRigObject(source.RigBindingId);
                if (rig.RendererBindingIds.Count != rig.Renderers.Count)
                    throw new InvalidOperationException($"Equipment Rig Binding '{source.RigBindingId}' Renderer identities and references do not align.");
                var renderers = new Renderer[source.RendererBindingIds.Count];
                for (int i = 0; i < source.RendererBindingIds.Count; i++)
                {
                    string expected = source.RendererBindingIds[i];
                    int match = -1;
                    for (int candidate = 0; candidate < rig.RendererBindingIds.Count; candidate++)
                    {
                        if (!string.Equals(rig.RendererBindingIds[candidate], expected, StringComparison.Ordinal))
                            continue;
                        if (match >= 0)
                            throw new InvalidOperationException($"Equipment Renderer Binding '{expected}' is duplicated in Rig '{source.RigBindingId}'.");
                        match = candidate;
                    }
                    if (match < 0 || !rig.Renderers[match])
                        throw new InvalidOperationException($"Equipment Renderer Binding '{expected}' is absent from Rig '{source.RigBindingId}'.");
                    renderers[i] = rig.Renderers[match];
                }
                return new ResolvedBinding(source, renderers, null);
            }
            if (source.Kind == EquipmentVisualBindingKind.SpawnedVisualAsset)
            {
                if (!source.VisualPrefab)
                    throw new InvalidOperationException($"Equipment visual binding '{source.VisualBindingId}' has no Prefab.");
                return new ResolvedBinding(source, Array.Empty<Renderer>(), catalog.RequireSocket(source.SocketBindingId));
            }
            throw new InvalidOperationException($"Equipment visual binding '{source.VisualBindingId}' has unsupported kind '{source.Kind}'.");
        }

        void Fail(EquipmentVisualSelection selection, string reason)
        {
            m_Invalid = true;
            m_InvalidReason = $"Equipment visual selection '{selection.SlotId}@{selection.EquipmentRevision}' failed: {reason}.";
            Publish("Invalid", selection, reason);
            throw new InvalidOperationException(m_InvalidReason);
        }

        void Publish(string status, EquipmentVisualSelection selection, string detail)
        {
            if (!m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Equipment, RuntimeTraceEventKind.EquipmentVisual))
                return;
            m_Diagnostics.Publish(
                RuntimeTraceChannel.Equipment,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.EquipmentVisual,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = status,
                    Name = selection.VisualBindingId.Value,
                    OwnerId = selection.SlotId.Value,
                    RelatedElementId = selection.EquipmentId.Value,
                    Cause = selection.EquipmentRevision.ToString(),
                    Time = selection.SourceTick,
                    Detail = detail
                });
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
                throw new ObjectDisposedException(nameof(CharacterEquipmentVisualRuntime));
        }

        static bool SameIdentity(EquipmentVisualSelection left, EquipmentVisualSelection right) =>
            left.ActorId == right.ActorId && left.SlotId == right.SlotId && left.EquipmentId == right.EquipmentId &&
            left.VisualBindingId == right.VisualBindingId && left.EquipmentRevision == right.EquipmentRevision;

        sealed class ResolvedBinding
        {
            public ResolvedBinding(EquipmentVisualProjectionBinding source, Renderer[] renderers, Transform socket)
            {
                Source = source;
                Renderers = renderers;
                Socket = socket;
            }

            public EquipmentVisualProjectionBinding Source { get; }
            public Renderer[] Renderers { get; }
            public Transform Socket { get; }
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
