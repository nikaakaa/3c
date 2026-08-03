using System;
using System.Collections.Generic;
using System.Linq;
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
        Marker = 4,
        Curve = 5
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
            ElementAuthoringId =
                elementAuthoringId ?? string.Empty;
            SubElementId = subElementId ?? string.Empty;
            KeyIndices = keyIndices ?? Array.Empty<int>();
            Revision = revision;
        }

        public TimelineEditorSelectionKind Kind { get; }
        public Track Track { get; }
        public Clip Clip { get; }
        public string ElementAuthoringId { get; }
        public string SubElementId { get; }
        public IReadOnlyList<int> KeyIndices { get; }
        public ulong Revision { get; }
        public bool HasTrack => Track != null;
        public bool HasClip => Clip != null;
        public bool IsTreeClip =>
            Kind == TimelineEditorSelectionKind.TreeClip;
        public bool HasMarker =>
            Kind == TimelineEditorSelectionKind.Marker;
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
            ITimelineAnimationMarkerSyncAuthoringContext markerTopologyContext,
            ITimelineEditorRuntimeDebugBinding runtimeDebugBinding,
            TimelineEditorToolCatalog toolCatalog)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            SerializedOwner = serializedOwner ? serializedOwner : throw new ArgumentNullException(nameof(serializedOwner));
            if (string.IsNullOrWhiteSpace(serializedPropertyPath))
                throw new ArgumentException("Timeline serialized property path is invalid.", nameof(serializedPropertyPath));
            SerializedPropertyPath = serializedPropertyPath;
            OwnershipLabel = ownershipLabel ?? string.Empty;
            MarkerTopologyContext = markerTopologyContext;
            RuntimeDebugBinding = runtimeDebugBinding;
            ToolCatalog = toolCatalog ?? TimelineEditorToolCatalog.Empty;
        }

        public TimelineData Timeline { get; }
        public UnityEngine.Object SerializedOwner { get; }
        public string SerializedPropertyPath { get; }
        public string OwnershipLabel { get; }
        public ITimelineAnimationMarkerSyncAuthoringContext MarkerTopologyContext { get; }
        public ITimelineEditorRuntimeDebugBinding RuntimeDebugBinding { get; }
        public TimelineEditorToolCatalog ToolCatalog { get; }
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
        public ITimelineAnimationMarkerSyncAuthoringContext MarkerTopologyContext => m_Request.MarkerTopologyContext;
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
                _ => default
            };
            if (selection.Kind == m_Selection.Kind &&
                ReferenceEquals(
                    selection.Track,
                    m_Selection.Track) &&
                ReferenceEquals(
                    selection.Clip,
                    m_Selection.Clip) &&
                string.Equals(
                    selection.ElementAuthoringId,
                    m_Selection.ElementAuthoringId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    selection.SubElementId,
                    m_Selection.SubElementId,
                    StringComparison.Ordinal) &&
                selection.Revision == m_Selection.Revision &&
                selection.KeyIndices.SequenceEqual(
                    m_Selection.KeyIndices ??
                    Array.Empty<int>()))
                return;
            m_Selection = selection;
            SelectionChanged?.Invoke(m_Selection);
        }

        public void Apply(Action mutation, string undoName)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Live Debug Timeline is read-only.");
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
}
