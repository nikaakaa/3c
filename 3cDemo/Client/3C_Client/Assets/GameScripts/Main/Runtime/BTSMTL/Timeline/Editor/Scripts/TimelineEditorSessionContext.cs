using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    public static class TimelineInspectorSelection
    {
        static UnityEngine.Object s_Owner;
        static string s_PropertyPath = string.Empty;
        static object s_Element;

        public static event Action Changed;

        public static void Set(UnityEngine.Object owner, string propertyPath, object element = null)
        {
            if (ReferenceEquals(s_Owner, owner) && string.Equals(s_PropertyPath, propertyPath, StringComparison.Ordinal) &&
                ReferenceEquals(s_Element, element))
                return;
            s_Owner = owner;
            s_PropertyPath = propertyPath ?? string.Empty;
            s_Element = element;
            Changed?.Invoke();
        }

        public static void Clear(UnityEngine.Object owner)
        {
            if (!ReferenceEquals(s_Owner, owner))
                return;
            s_Owner = null;
            s_PropertyPath = string.Empty;
            s_Element = null;
            Changed?.Invoke();
        }

        public static bool TryGet(UnityEngine.Object owner, out string propertyPath)
        {
            if (ReferenceEquals(s_Owner, owner) && !string.IsNullOrEmpty(s_PropertyPath))
            {
                propertyPath = s_PropertyPath;
                return true;
            }
            propertyPath = string.Empty;
            return false;
        }

        public static bool TryGetElement(UnityEngine.Object owner, out object element)
        {
            if (ReferenceEquals(s_Owner, owner) && s_Element != null)
            {
                element = s_Element;
                return true;
            }
            element = null;
            return false;
        }
    }

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

    public sealed class TimelineEditorOpenRequest
    {
        public TimelineEditorOpenRequest(
            TimelineData timeline,
            UnityEngine.Object serializedOwner,
            string serializedPropertyPath,
            string ownershipLabel,
            ITimelineEditorRuntimeDebugBinding runtimeDebugBinding,
            TimelineContractCatalog contractCatalog)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            SerializedOwner = serializedOwner ? serializedOwner : throw new ArgumentNullException(nameof(serializedOwner));
            if (string.IsNullOrWhiteSpace(serializedPropertyPath))
                throw new ArgumentException("Timeline serialized property path is invalid.", nameof(serializedPropertyPath));
            SerializedPropertyPath = serializedPropertyPath;
            OwnershipLabel = ownershipLabel ?? string.Empty;
            RuntimeDebugBinding = runtimeDebugBinding;
            ContractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
        }

        public TimelineData Timeline { get; }
        public UnityEngine.Object SerializedOwner { get; }
        public string SerializedPropertyPath { get; }
        public string OwnershipLabel { get; }
        public ITimelineEditorRuntimeDebugBinding RuntimeDebugBinding { get; }
        public TimelineContractCatalog ContractCatalog { get; }
    }

    public sealed class TimelineEditorSessionContext :
        ITimelineEditorSelectionPort,
        ITimelineEditorMutationPort
    {
        readonly TimelineEditorOpenRequest m_Request;
        Func<bool> m_IsReadOnly;
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
        public TimelineEditorSelection Selection => m_Selection;
        public bool IsReadOnly => m_IsReadOnly != null && m_IsReadOnly();
        public int FrameRate => TimelineUtility.FrameRate;
        public event Action<TimelineEditorSelection> SelectionChanged;

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

        internal void Dispose()
        {
            SelectionChanged = null;
            m_IsReadOnly = null;
            m_Selection = default;
        }
    }
}
