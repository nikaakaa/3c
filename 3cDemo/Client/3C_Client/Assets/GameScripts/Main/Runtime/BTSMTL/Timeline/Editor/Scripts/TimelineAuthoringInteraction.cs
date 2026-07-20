using System;
using System.Collections.Generic;

namespace BTSMTL.Timeline.Editor
{
    internal readonly struct TimelineAuthoringElementIdentity : IEquatable<TimelineAuthoringElementIdentity>
    {
        public TimelineAuthoringElementIdentity(TimelineAuthoringContentKind kind, string ownerAuthoringId, string elementId)
        {
            Kind = kind;
            OwnerAuthoringId = ownerAuthoringId ?? string.Empty;
            ElementId = elementId ?? string.Empty;
        }

        public TimelineAuthoringContentKind Kind { get; }
        public string OwnerAuthoringId { get; }
        public string ElementId { get; }
        public bool IsValid => !string.IsNullOrEmpty(OwnerAuthoringId) && !string.IsNullOrEmpty(ElementId);
        public bool Equals(TimelineAuthoringElementIdentity other) =>
            Kind == other.Kind &&
            string.Equals(OwnerAuthoringId, other.OwnerAuthoringId, StringComparison.Ordinal) &&
            string.Equals(ElementId, other.ElementId, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TimelineAuthoringElementIdentity other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(OwnerAuthoringId);
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(ElementId);
                return hash;
            }
        }
    }

    internal sealed class TimelineDraftTransaction<T>
    {
        readonly Func<T, T> m_Clone;

        public TimelineDraftTransaction(Func<T, T> clone)
        {
            m_Clone = clone ?? throw new ArgumentNullException(nameof(clone));
        }

        public bool IsActive { get; private set; }
        public T Original { get; private set; }
        public T Draft { get; private set; }
        public ulong SourceRevision { get; private set; }

        public void Begin(T source, ulong revision)
        {
            if (IsActive)
                throw new InvalidOperationException("Timeline draft transaction is already active.");
            Original = m_Clone(source);
            Draft = m_Clone(source);
            SourceRevision = revision;
            IsActive = true;
        }

        public void Update(T draft)
        {
            RequireActive();
            Draft = m_Clone(draft);
        }

        public T Complete()
        {
            RequireActive();
            T result = m_Clone(Draft);
            Reset();
            return result;
        }

        public void Cancel()
        {
            if (IsActive)
                Reset();
        }

        void Reset()
        {
            Original = default;
            Draft = default;
            SourceRevision = 0UL;
            IsActive = false;
        }

        void RequireActive()
        {
            if (!IsActive)
                throw new InvalidOperationException("Timeline draft transaction is not active.");
        }
    }

    internal static class TimelineCurveEditorSession
    {
        static readonly HashSet<string> ExpandedTracks = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> ExpandedMarkerTracks = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> HiddenChannels = new HashSet<string>(StringComparer.Ordinal);
        static readonly Dictionary<string, TimelineCurveVerticalView> VerticalViews =
            new Dictionary<string, TimelineCurveVerticalView>(StringComparer.Ordinal);

        public static bool CurvesExpanded(Track track) => ExpandedTracks.Contains(RequireTrackIdentity(track));
        public static bool MarkersExpanded(Track track) => ExpandedMarkerTracks.Contains(RequireTrackIdentity(track));
        public static void ToggleCurves(Track track) => Toggle(ExpandedTracks, RequireTrackIdentity(track));
        public static void ToggleMarkers(Track track) => Toggle(ExpandedMarkerTracks, RequireTrackIdentity(track));

        public static bool IsChannelVisible(Track track, TimelineCurveChannelId channelId) =>
            !HiddenChannels.Contains(ChannelKey(track, channelId));

        public static void ToggleChannel(Track track, TimelineCurveChannelId channelId)
        {
            string key = ChannelKey(track, channelId);
            if (!HiddenChannels.Add(key))
                HiddenChannels.Remove(key);
        }

        public static TimelineCurveVerticalView GetVerticalView(Track track, TimelineCurveChannelDescriptor descriptor)
        {
            string key = ChannelKey(track, descriptor.ChannelId);
            if (!VerticalViews.TryGetValue(key, out TimelineCurveVerticalView view))
            {
                view = descriptor.ValueDomain.IsBounded
                    ? new TimelineCurveVerticalView(descriptor.ValueDomain.Minimum, descriptor.ValueDomain.Maximum)
                    : new TimelineCurveVerticalView(-1f, 1f);
                VerticalViews.Add(key, view);
            }
            return view;
        }

        public static void SetVerticalView(Track track, TimelineCurveChannelId channelId, TimelineCurveVerticalView view) =>
            VerticalViews[ChannelKey(track, channelId)] = view;

        static void Toggle(HashSet<string> values, string identity)
        {
            if (!values.Add(identity))
                values.Remove(identity);
        }

        static string ChannelKey(Track track, TimelineCurveChannelId channelId) =>
            $"{RequireTrackIdentity(track)}/{channelId.Value}";

        static string RequireTrackIdentity(Track track)
        {
            if (track == null || string.IsNullOrWhiteSpace(track.AuthoringId))
                throw new InvalidOperationException("Timeline editor session state requires a stable Track identity.");
            return track.AuthoringId;
        }
    }

    internal readonly struct TimelineCurveVerticalView
    {
        public TimelineCurveVerticalView(float minimum, float maximum)
        {
            if (!TimelineCurveAuthoring.IsFinite(minimum) || !TimelineCurveAuthoring.IsFinite(maximum) || maximum <= minimum)
                throw new ArgumentException("Timeline curve vertical view requires finite minimum < maximum.");
            Minimum = minimum;
            Maximum = maximum;
        }

        public float Minimum { get; }
        public float Maximum { get; }
        public float Span => Maximum - Minimum;

        public TimelineCurveVerticalView Pan(float normalizedDelta) =>
            new TimelineCurveVerticalView(Minimum + Span * normalizedDelta, Maximum + Span * normalizedDelta);

        public TimelineCurveVerticalView Zoom(float factor, float pivot)
        {
            float scale = Math.Max(0.05f, Math.Min(20f, factor));
            float minimum = pivot + (Minimum - pivot) * scale;
            float maximum = pivot + (Maximum - pivot) * scale;
            return new TimelineCurveVerticalView(minimum, maximum);
        }
    }
}
