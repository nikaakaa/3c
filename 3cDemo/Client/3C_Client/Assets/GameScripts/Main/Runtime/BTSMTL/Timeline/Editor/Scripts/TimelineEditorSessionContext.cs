using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    public enum TimelineEditorSelectionKind : byte
    {
        None = 0,
        Track = 1,
        Clip = 2,
        TreeClip = 3,
        Curve = 4,
        Section = 5
    }

    public interface ITimelineEditorRuntimeDebugBinding
    {
        string BindingId { get; }
    }

    public readonly struct TimelineEditorSelection
    {
        public TimelineEditorSelection(Track track, Clip clip)
            : this(
                clip is ITimelineOwnedAuthoringIdentity
                    ? TimelineEditorSelectionKind.TreeClip
                    : clip != null
                        ? TimelineEditorSelectionKind.Clip
                        : track != null
                            ? TimelineEditorSelectionKind.Track
                            : TimelineEditorSelectionKind.None,
                track,
                clip,
                clip?.AuthoringId ?? track?.AuthoringId ??
                string.Empty,
                string.Empty,
                Array.Empty<int>(),
                0)
        {
        }

        public TimelineEditorSelection(TimelineSection section)
            : this(
                section != null ? TimelineEditorSelectionKind.Section : TimelineEditorSelectionKind.None,
                null,
                null,
                section?.AuthoringId ?? string.Empty,
                string.Empty,
                Array.Empty<int>(),
                0)
        {
            Section = section;
        }

        TimelineEditorSelection(
            TimelineEditorSelectionKind kind,
            Track track,
            Clip clip,
            string elementAuthoringId,
            string subElementId,
            IReadOnlyList<int> keyIndices,
            ulong revision)
        {
            Kind = kind;
            Track = track;
            Clip = clip;
            Section = null;
            ElementAuthoringId =
                elementAuthoringId ?? string.Empty;
            SubElementId = subElementId ?? string.Empty;
            KeyIndices = keyIndices ?? Array.Empty<int>();
            Revision = revision;
        }

        public TimelineEditorSelectionKind Kind { get; }
        public Track Track { get; }
        public Clip Clip { get; }
        public TimelineSection Section { get; }
        public string ElementAuthoringId { get; }
        public string SubElementId { get; }
        public IReadOnlyList<int> KeyIndices { get; }
        public ulong Revision { get; }
        public bool HasTrack => Track != null;
        public bool HasClip => Clip != null;
        public bool IsTreeClip =>
            Kind == TimelineEditorSelectionKind.TreeClip;
        public bool HasCurve =>
            Kind == TimelineEditorSelectionKind.Curve;

    }

    public interface ITimelineEditorSelectionPort
    {
        TimelineEditorSelection Selection { get; }
        event Action<TimelineEditorSelection> SelectionChanged;
    }

    public interface ITimelineEditorMutationPort
    {
        bool IsReadOnly { get; }
        void Apply(Action mutation, string undoName);
    }

    public interface ITimelineEditorFrameGeometryPort
    {
        int FrameRate { get; }
        float OneFrameWidth { get; }
        int PositionToClosestFrame(float position);
        float FrameToPosition(int frame);
    }

    public abstract class TimelineEditorToolPanel : VisualElement, IDisposable
    {
        public virtual void Dispose()
        {
        }
    }

    public interface ITimelineEditorToolProvider
    {
        string ToolId { get; }
        string DisplayName { get; }
        bool Supports(TimelineEditorSelection selection);
        TimelineEditorToolPanel CreatePanel(TimelineEditorSessionContext session);
    }

    public sealed class TimelineEditorToolCatalog
    {
        public static readonly TimelineEditorToolCatalog Empty = new TimelineEditorToolCatalog(Array.Empty<ITimelineEditorToolProvider>());

        readonly ITimelineEditorToolProvider[] m_Providers;

        public TimelineEditorToolCatalog(IEnumerable<ITimelineEditorToolProvider> providers)
        {
            var values = new List<ITimelineEditorToolProvider>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (providers != null)
            {
                foreach (ITimelineEditorToolProvider provider in providers)
                {
                    if (provider == null || string.IsNullOrWhiteSpace(provider.ToolId) ||
                        !string.Equals(provider.ToolId, provider.ToolId.Trim(), StringComparison.Ordinal))
                        throw new ArgumentException("Timeline Editor tool provider identity is invalid.", nameof(providers));
                    if (!ids.Add(provider.ToolId))
                        throw new ArgumentException($"Timeline Editor tool provider '{provider.ToolId}' is duplicated.", nameof(providers));
                    values.Add(provider);
                }
            }
            m_Providers = values.ToArray();
        }

        public IReadOnlyList<ITimelineEditorToolProvider> Providers => m_Providers;
    }

    public static class TimelineEditorToolComposition
    {
        static TimelineEditorToolCatalog s_Catalog = TimelineEditorToolCatalog.Empty;

        public static TimelineEditorToolCatalog Catalog => s_Catalog;

        public static void SetCatalog(TimelineEditorToolCatalog catalog)
        {
            s_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }
    }

    public sealed class TimelineEditorOpenRequest
    {
        public TimelineEditorOpenRequest(
            TimelineData timeline,
            UnityEngine.Object serializedOwner,
            string serializedPropertyPath,
            string ownershipLabel,
            ITimelineEditorRuntimeDebugBinding runtimeDebugBinding,
            TimelineEditorToolCatalog toolCatalog,
            TimelineContractCatalog contractCatalog)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            SerializedOwner = serializedOwner ? serializedOwner : throw new ArgumentNullException(nameof(serializedOwner));
            if (string.IsNullOrWhiteSpace(serializedPropertyPath))
                throw new ArgumentException("Timeline serialized property path is invalid.", nameof(serializedPropertyPath));
            SerializedPropertyPath = serializedPropertyPath;
            OwnershipLabel = ownershipLabel ?? string.Empty;
            RuntimeDebugBinding = runtimeDebugBinding;
            ToolCatalog = toolCatalog ?? TimelineEditorToolCatalog.Empty;
            ContractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
        }

        public TimelineData Timeline { get; }
        public UnityEngine.Object SerializedOwner { get; }
        public string SerializedPropertyPath { get; }
        public string OwnershipLabel { get; }
        public ITimelineEditorRuntimeDebugBinding RuntimeDebugBinding { get; }
        public TimelineEditorToolCatalog ToolCatalog { get; }
        public TimelineContractCatalog ContractCatalog { get; }
    }

    public sealed class TimelineEditorSessionContext :
        ITimelineEditorSelectionPort,
        ITimelineEditorMutationPort,
        ITimelineEditorFrameGeometryPort
    {
        readonly TimelineEditorOpenRequest m_Request;
        Func<bool> m_IsReadOnly;
        Func<float> m_OneFrameWidth;
        Func<float, int> m_PositionToClosestFrame;
        Func<int, float> m_FrameToPosition;
        TimelineEditorSelection m_Selection;

        internal TimelineEditorSessionContext(TimelineEditorOpenRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public TimelineData Timeline => m_Request.Timeline;
        public UnityEngine.Object SerializedOwner => m_Request.SerializedOwner;
        public string SerializedPropertyPath => m_Request.SerializedPropertyPath;
        public string OwnershipLabel => m_Request.OwnershipLabel;
        public ITimelineEditorRuntimeDebugBinding RuntimeDebugBinding => m_Request.RuntimeDebugBinding;
        public TimelineEditorToolCatalog ToolCatalog => m_Request.ToolCatalog;
        public TimelineEditorSelection Selection => m_Selection;
        public bool IsReadOnly => m_IsReadOnly != null && m_IsReadOnly();
        public int FrameRate => TimelineUtility.FrameRate;
        public float OneFrameWidth => m_OneFrameWidth?.Invoke() ?? 0f;
        public event Action<TimelineEditorSelection> SelectionChanged;

        internal void BindView(
            Func<bool> isReadOnly,
            Func<float> oneFrameWidth,
            Func<float, int> positionToClosestFrame,
            Func<int, float> frameToPosition)
        {
            m_IsReadOnly = isReadOnly ?? throw new ArgumentNullException(nameof(isReadOnly));
            m_OneFrameWidth = oneFrameWidth ?? throw new ArgumentNullException(nameof(oneFrameWidth));
            m_PositionToClosestFrame = positionToClosestFrame ?? throw new ArgumentNullException(nameof(positionToClosestFrame));
            m_FrameToPosition = frameToPosition ?? throw new ArgumentNullException(nameof(frameToPosition));
        }

        internal void SetSelection(object target)
        {
            TimelineEditorSelection selection = target switch
            {
                Clip clip => new TimelineEditorSelection(clip.Track, clip),
                Track track => new TimelineEditorSelection(track, null),
                TimelineSection section => new TimelineEditorSelection(section),
                _ => default
            };
            if (selection.Kind == m_Selection.Kind &&
                ReferenceEquals(
                    selection.Track,
                    m_Selection.Track) &&
                ReferenceEquals(
                    selection.Clip,
                    m_Selection.Clip) &&
                ReferenceEquals(
                    selection.Section,
                    m_Selection.Section) &&
                string.Equals(
                    selection.ElementAuthoringId,
                    m_Selection.ElementAuthoringId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    selection.SubElementId,
                    m_Selection.SubElementId,
                    StringComparison.Ordinal) &&
                selection.Revision == m_Selection.Revision &&
                (selection.KeyIndices ?? Array.Empty<int>()).SequenceEqual(
                    m_Selection.KeyIndices ??
                    Array.Empty<int>()))
                return;
            m_Selection = selection;
            SelectionChanged?.Invoke(m_Selection);
        }

        public void Apply(Action mutation, string undoName)
        {
            if (IsReadOnly)
                throw new InvalidOperationException(
                    "Timeline authoring mutation is unavailable during Scene Play or Live Debug.");
            Timeline.ApplyModify(mutation ?? throw new ArgumentNullException(nameof(mutation)), undoName);
        }

        public int PositionToClosestFrame(float position) =>
            m_PositionToClosestFrame != null ? m_PositionToClosestFrame(position) : 0;

        public float FrameToPosition(int frame) =>
            m_FrameToPosition != null ? m_FrameToPosition(frame) : 0f;

        internal void Dispose()
        {
            SelectionChanged = null;
            m_IsReadOnly = null;
            m_OneFrameWidth = null;
            m_PositionToClosestFrame = null;
            m_FrameToPosition = null;
            m_Selection = default;
        }
    }

    public sealed class TimelineBindingSurface : VisualElement
    {
        sealed class BindingDraft
        {
            public string BindingId;
            public string DisplayName;
            public string Domain;
            public string ParameterId;
            public TimelineBindingValueKind ValueKind;
            public TimelineBindingAccess Access;
            public TimelineBindingLifetime Lifetime;

            public static BindingDraft From(TimelineExternalBindingDeclaration binding)
            {
                return new BindingDraft
                {
                    BindingId = binding.BindingId,
                    DisplayName = binding.DisplayName,
                    Domain = binding.Domain,
                    ParameterId = binding.ParameterId,
                    ValueKind = binding.ValueKind,
                    Access = binding.Access,
                    Lifetime = binding.Lifetime
                };
            }
        }

        readonly VisualElement m_Content = new VisualElement();
        readonly Dictionary<string, BindingDraft> m_Drafts = new Dictionary<string, BindingDraft>(StringComparer.Ordinal);
        TimelineData m_Timeline;
        TimelineContractCatalog m_Catalog;
        TimelineEditorSessionContext m_Session;
        bool m_ReadOnly;

        public TimelineBindingSurface()
        {
            name = "timeline-binding-surface";
            style.flexShrink = 0f;
            style.maxHeight = 360f;
            style.paddingLeft = 8f;
            style.paddingRight = 8f;
            style.paddingTop = 4f;
            style.paddingBottom = 4f;
            style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            Add(m_Content);
        }

        public void Bind(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            TimelineEditorSessionContext session)
        {
            m_Timeline = timeline;
            m_Catalog = catalog;
            m_Session = session;
            Rebuild();
        }

        public void SetReadOnly(bool readOnly)
        {
            if (m_ReadOnly == readOnly)
                return;
            m_ReadOnly = readOnly;
            Rebuild();
        }

        public void Refresh()
        {
            if (m_Timeline != null)
                Rebuild();
        }

        void Rebuild()
        {
            m_Content.Clear();
            if (m_Timeline == null || m_Catalog == null || m_Session == null)
                return;

            Foldout catalog = new Foldout { text = "Track / Clip Contracts", value = false };
            for (int i = 0; i < m_Catalog.Tracks.Count; i++)
            {
                TimelineTrackContract track = m_Catalog.Tracks[i];
                catalog.Add(new Label(
                    $"Track {track.Kind} | {track.OverlapPolicy} | {track.Capabilities} | {string.Join(", ", track.AllowedClipKinds)}"));
            }
            for (int i = 0; i < m_Catalog.Clips.Count; i++)
            {
                TimelineClipContract clip = m_Catalog.Clips[i];
                string bindingSummary = clip.Bindings.Count == 0
                    ? "none"
                    : string.Join(", ", clip.Bindings.Select(value =>
                        $"{value.BindingId}:{value.ValueKind}/{value.Access}/{value.Lifetime}"));
                catalog.Add(new Label(
                    $"Clip {clip.Kind} -> {clip.TrackKind} | {clip.SupportedExecutionPhases} | {clip.Capabilities} | bindings {bindingSummary}"));
            }
            m_Content.Add(catalog);

            var bindingSurface = new Foldout
            {
                text = $"External Targets / Parameters ({m_Timeline.ExternalBindings.Count})",
                value = true
            };
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            Button addTarget = new Button(() => AddBinding(TimelineBindingValueKind.Target)) { text = "Add Target" };
            Button addScalar = new Button(() => AddBinding(TimelineBindingValueKind.Scalar)) { text = "Add Scalar" };
            Button addBoolean = new Button(() => AddBinding(TimelineBindingValueKind.Boolean)) { text = "Add Boolean" };
            addTarget.SetEnabled(!m_ReadOnly);
            addScalar.SetEnabled(!m_ReadOnly);
            addBoolean.SetEnabled(!m_ReadOnly);
            buttons.Add(addTarget);
            buttons.Add(addScalar);
            buttons.Add(addBoolean);
            bindingSurface.Add(buttons);
            for (int i = 0; i < m_Timeline.ExternalBindings.Count; i++)
            {
                TimelineExternalBindingDeclaration binding = m_Timeline.ExternalBindings[i];
                if (binding != null)
                    bindingSurface.Add(BuildBindingRow(binding));
            }
            m_Content.Add(bindingSurface);

            var errors = new List<string>();
            if (!m_Timeline.ValidateContent(m_Catalog, errors) && errors.Count > 0)
            {
                var validation = new Foldout { text = $"Validation ({errors.Count})", value = true };
                for (int i = 0; i < errors.Count; i++)
                    validation.Add(new HelpBox(errors[i], HelpBoxMessageType.Error));
                m_Content.Add(validation);
            }
        }

        VisualElement BuildBindingRow(TimelineExternalBindingDeclaration binding)
        {
            if (!m_Drafts.TryGetValue(binding.AuthoringId, out BindingDraft draft))
            {
                draft = BindingDraft.From(binding);
                m_Drafts.Add(binding.AuthoringId, draft);
            }

            var row = new VisualElement();
            row.style.marginTop = 4f;
            row.style.paddingTop = 4f;
            row.style.paddingBottom = 4f;
            row.style.borderTopWidth = 1f;
            row.style.borderTopColor = new Color(0.25f, 0.25f, 0.25f);
            row.Add(new Label($"{binding.BindingId} [{binding.AuthoringId}]"));

            var displayName = new TextField("Display Name") { value = draft.DisplayName };
            var domain = new TextField("Domain") { value = draft.Domain };
            var parameter = new TextField("Parameter") { value = draft.ParameterId };
            var valueKind = CreatePopup(
                "Value Kind",
                Enum.GetNames(typeof(TimelineBindingValueKind)),
                draft.ValueKind.ToString());
            var access = CreatePopup(
                "Access",
                Enum.GetNames(typeof(TimelineBindingAccess)),
                draft.Access.ToString());
            var lifetime = CreatePopup(
                "Lifetime",
                Enum.GetNames(typeof(TimelineBindingLifetime)),
                draft.Lifetime.ToString());

            displayName.RegisterValueChangedCallback(evt => draft.DisplayName = evt.newValue);
            domain.RegisterValueChangedCallback(evt => draft.Domain = evt.newValue);
            parameter.RegisterValueChangedCallback(evt => draft.ParameterId = evt.newValue);
            valueKind.RegisterValueChangedCallback(evt =>
            {
                draft.ValueKind = Parse<TimelineBindingValueKind>(evt.newValue);
                if (draft.ValueKind == TimelineBindingValueKind.Target)
                {
                    draft.ParameterId = string.Empty;
                    parameter.SetValueWithoutNotify(string.Empty);
                }
                parameter.SetEnabled(!m_ReadOnly && draft.ValueKind != TimelineBindingValueKind.Target);
            });
            access.RegisterValueChangedCallback(evt =>
            {
                draft.Access = Parse<TimelineBindingAccess>(evt.newValue);
                draft.Lifetime = draft.Access == TimelineBindingAccess.Input
                    ? TimelineBindingLifetime.Call
                    : TimelineBindingLifetime.Tick;
                lifetime.SetValueWithoutNotify(draft.Lifetime.ToString());
            });
            lifetime.SetEnabled(false);
            parameter.SetEnabled(!m_ReadOnly && draft.ValueKind != TimelineBindingValueKind.Target);
            displayName.SetEnabled(!m_ReadOnly);
            domain.SetEnabled(!m_ReadOnly);
            valueKind.SetEnabled(!m_ReadOnly);
            access.SetEnabled(!m_ReadOnly);

            row.Add(displayName);
            row.Add(domain);
            row.Add(parameter);
            row.Add(valueKind);
            row.Add(access);
            row.Add(lifetime);

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            Button apply = new Button(() => ApplyBinding(binding, draft, row)) { text = "Apply" };
            Button remove = new Button(() => RemoveBinding(binding, row)) { text = "Remove" };
            apply.SetEnabled(!m_ReadOnly);
            remove.SetEnabled(!m_ReadOnly);
            actions.Add(apply);
            actions.Add(remove);
            row.Add(actions);
            return row;
        }

        static PopupField<string> CreatePopup(string label, IReadOnlyList<string> choices, string value)
        {
            var values = choices.ToList();
            int index = Math.Max(0, values.IndexOf(value));
            return new PopupField<string>(label, values, index);
        }

        void AddBinding(TimelineBindingValueKind valueKind)
        {
            if (m_ReadOnly)
                return;
            string bindingId = NextBindingId(valueKind == TimelineBindingValueKind.Target ? "target" : "parameter");
            string parameterId = valueKind == TimelineBindingValueKind.Target ? string.Empty : bindingId;
            try
            {
                m_Session.Apply(
                    () =>
                    {
                        m_Timeline.AddExternalBinding(
                            bindingId,
                            bindingId,
                            "scene.presentation",
                            valueKind,
                            valueKind == TimelineBindingValueKind.Target ? TimelineBindingAccess.Input : TimelineBindingAccess.Write,
                            valueKind == TimelineBindingValueKind.Target ? TimelineBindingLifetime.Call : TimelineBindingLifetime.Tick,
                            parameterId);
                    },
                    "Add Timeline Binding");
                Rebuild();
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                ShowOperationError(exception.Message);
            }
        }

        void ApplyBinding(TimelineExternalBindingDeclaration binding, BindingDraft draft, VisualElement row)
        {
            if (m_ReadOnly)
                return;
            if (!TryValidateDraft(draft, out string error))
            {
                ShowRowError(row, error);
                return;
            }
            try
            {
                m_Session.Apply(
                    () =>
                    {
                        binding.Configure(
                            draft.BindingId,
                            draft.DisplayName,
                            draft.Domain,
                            draft.ValueKind,
                            draft.Access,
                            draft.Lifetime,
                            draft.ParameterId);
                        m_Timeline.Init();
                    },
                    "Configure Timeline Binding");
                m_Drafts.Remove(binding.AuthoringId);
                Rebuild();
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                ShowRowError(row, exception.Message);
            }
        }

        void RemoveBinding(TimelineExternalBindingDeclaration binding, VisualElement row)
        {
            if (m_ReadOnly)
                return;
            if (IsReferenced(binding.BindingId))
            {
                ShowRowError(row, $"Binding '{binding.BindingId}' 仍被片段使用，不能删除。");
                return;
            }
            try
            {
                m_Session.Apply(
                    () =>
                    {
                        m_Timeline.RemoveExternalBinding(binding);
                        m_Timeline.Init();
                    },
                    "Remove Timeline Binding");
                m_Drafts.Remove(binding.AuthoringId);
                Rebuild();
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                ShowRowError(row, exception.Message);
            }
        }

        bool IsReferenced(string bindingId)
        {
            for (int trackIndex = 0; trackIndex < m_Timeline.Tracks.Count; trackIndex++)
            {
                IReadOnlyList<Clip> clips = m_Timeline.Tracks[trackIndex].Clips;
                for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
                {
                    if (clips[clipIndex] is not ITimelineExternalBindingUseSource source)
                        continue;
                    if (source.ExternalBindingUses.Any(value => string.Equals(value.BindingId, bindingId, StringComparison.Ordinal) ||
                                                                 string.Equals(value.TargetBindingId, bindingId, StringComparison.Ordinal)))
                        return true;
                }
            }
            return false;
        }

        static bool TryValidateDraft(BindingDraft draft, out string error)
        {
            if (string.IsNullOrWhiteSpace(draft.BindingId) || !string.Equals(draft.BindingId, draft.BindingId.Trim(), StringComparison.Ordinal))
            {
                error = "Binding identity 不能为空且不能包含首尾空白。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(draft.DisplayName) || string.IsNullOrWhiteSpace(draft.Domain))
            {
                error = "Display Name 和 Domain 不能为空。";
                return false;
            }
            if (draft.ValueKind == TimelineBindingValueKind.Target && !string.IsNullOrEmpty(draft.ParameterId) ||
                draft.ValueKind != TimelineBindingValueKind.Target && string.IsNullOrWhiteSpace(draft.ParameterId))
            {
                error = "Target 不允许 Parameter；Scalar/Boolean 必须填写 Parameter。";
                return false;
            }
            if (draft.ValueKind == TimelineBindingValueKind.Target && draft.Access != TimelineBindingAccess.Input)
            {
                error = "Target binding 只能作为 Call Input。";
                return false;
            }
            TimelineBindingLifetime expectedLifetime = draft.Access == TimelineBindingAccess.Input
                ? TimelineBindingLifetime.Call
                : TimelineBindingLifetime.Tick;
            if (draft.Lifetime != expectedLifetime)
            {
                error = "Input 必须使用 Call；Read/Write 必须使用 Tick。";
                return false;
            }
            error = string.Empty;
            return true;
        }

        string NextBindingId(string prefix)
        {
            if (!m_Timeline.ExternalBindings.Any(value => value != null && string.Equals(value.BindingId, prefix, StringComparison.Ordinal)))
                return prefix;
            for (int index = 2; ; index++)
            {
                string candidate = $"{prefix}{index}";
                if (!m_Timeline.ExternalBindings.Any(value => value != null && string.Equals(value.BindingId, candidate, StringComparison.Ordinal)))
                    return candidate;
            }
        }

        void ShowOperationError(string message)
        {
            m_Content.Add(new HelpBox(message, HelpBoxMessageType.Error));
        }

        static void ShowRowError(VisualElement row, string message)
        {
            row.Add(new HelpBox(message, HelpBoxMessageType.Error));
        }

        static T Parse<T>(string value) where T : struct
        {
            return Enum.TryParse(value, true, out T result) ? result : default;
        }
    }
}
