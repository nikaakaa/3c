using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    internal enum AnimationTimeLaneKind
    {
        Span,
        Point,
        Curve
    }

    internal enum AnimationTimePointKind
    {
        SyncMarker,
        Notify
    }

    internal readonly struct AnimationTimeSpanDescriptor
    {
        public AnimationTimeSpanDescriptor(string identity, string label, int startFrame, int endFrame)
        {
            Identity = identity ?? string.Empty;
            Label = label ?? string.Empty;
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        public string Identity { get; }
        public string Label { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
    }

    internal readonly struct AnimationTimePointDescriptor
    {
        public AnimationTimePointDescriptor(
            string identity,
            string label,
            int frame,
            AnimationTimePointKind kind)
        {
            Identity = identity ?? string.Empty;
            Label = label ?? string.Empty;
            Frame = frame;
            Kind = kind;
        }

        public string Identity { get; }
        public string Label { get; }
        public int Frame { get; }
        public AnimationTimePointKind Kind { get; }
    }

    internal readonly struct AnimationTimeCurveDescriptor
    {
        public AnimationTimeCurveDescriptor(
            string identity,
            string label,
            AnimationSequenceCurveValueDomain valueDomain,
            Color color,
            AnimationCurve curve)
        {
            Identity = identity ?? string.Empty;
            Label = label ?? string.Empty;
            ValueDomain = valueDomain;
            Color = color;
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
        }

        public string Identity { get; }
        public string Label { get; }
        public AnimationSequenceCurveValueDomain ValueDomain { get; }
        public Color Color { get; }
        public AnimationCurve Curve { get; }
    }

    internal sealed class AnimationTimeLaneDescriptor
    {
        AnimationTimeLaneDescriptor(
            string identity,
            string name,
            AnimationTimeLaneKind kind,
            float height)
        {
            Identity = identity ?? string.Empty;
            Name = name ?? string.Empty;
            Kind = kind;
            Height = height;
        }

        public string Identity { get; }
        public string Name { get; }
        public AnimationTimeLaneKind Kind { get; }
        public float Height { get; }
        public AnimationTimeSpanDescriptor Span { get; private set; }
        public IReadOnlyList<AnimationTimePointDescriptor> Points { get; private set; } =
            Array.Empty<AnimationTimePointDescriptor>();
        public AnimationTimeCurveDescriptor Curve { get; private set; }

        public static AnimationTimeLaneDescriptor CreateSpan(
            string identity,
            string name,
            AnimationTimeSpanDescriptor span) =>
            new AnimationTimeLaneDescriptor(identity, name, AnimationTimeLaneKind.Span, TimelineTrackLayout.ClipRowHeight)
            {
                Span = span
            };

        public static AnimationTimeLaneDescriptor CreatePoints(
            string identity,
            string name,
            IReadOnlyList<AnimationTimePointDescriptor> points) =>
            new AnimationTimeLaneDescriptor(identity, name, AnimationTimeLaneKind.Point, AnimationTimeDocumentLayout.PointLaneHeight)
            {
                Points = points ?? Array.Empty<AnimationTimePointDescriptor>()
            };

        public static AnimationTimeLaneDescriptor CreateCurve(
            string identity,
            string name,
            AnimationTimeCurveDescriptor curve) =>
            new AnimationTimeLaneDescriptor(identity, name, AnimationTimeLaneKind.Curve, TimelineTrackLayout.CurveLaneHeight)
            {
                Curve = curve
            };
    }

    internal sealed class AnimationTimeSelection
    {
        public AnimationTimeSelection(
            string laneIdentity,
            string elementIdentity,
            AnimationTimePointKind pointKind)
        {
            LaneIdentity = laneIdentity ?? string.Empty;
            ElementIdentity = elementIdentity ?? string.Empty;
            PointKind = pointKind;
        }

        public string LaneIdentity { get; }
        public string ElementIdentity { get; }
        public AnimationTimePointKind PointKind { get; }
    }

    internal interface IAnimationTimeDocumentAdapter
    {
        UnityEngine.Object Document { get; }
        string DocumentIdentity { get; }
        string DisplayName { get; }
        int DurationFrame { get; }
        float FrameRate { get; }
        float DefaultPlayRate { get; }
        bool Loop { get; }
        IReadOnlyList<AnimationTimeLaneDescriptor> Lanes { get; }
        void Refresh();
        void RequireValid();
        void MovePoint(AnimationTimeSelection selection, int frame);
        void DeletePoint(AnimationTimeSelection selection);
        void BuildInspector(VisualElement container, AnimationTimeSelection selection, Action refresh);
        void SetCurve(string laneIdentity, AnimationCurve curve, string undoName);
    }

    internal sealed class AnimationSequenceTimeDocumentAdapter : IAnimationTimeDocumentAdapter
    {
        const string SpanLaneId = "sequence.span";
        const string MarkerLaneId = "sequence.markers";
        const string NotifyLaneId = "sequence.notifies";

        readonly AnimationSequenceAsset m_Sequence;
        readonly List<AnimationTimeLaneDescriptor> m_Lanes = new List<AnimationTimeLaneDescriptor>();

        public AnimationSequenceTimeDocumentAdapter(AnimationSequenceAsset sequence)
        {
            m_Sequence = sequence ? sequence : throw new ArgumentNullException(nameof(sequence));
            Refresh();
        }

        public UnityEngine.Object Document => m_Sequence;
        public string DocumentIdentity => m_Sequence.AuthoringId;
        public string DisplayName => m_Sequence.name;
        public int DurationFrame => m_Sequence.DurationFrame;
        public float FrameRate => m_Sequence.Clip ? m_Sequence.Clip.frameRate : TimelineUtility.FrameRate;
        public float DefaultPlayRate => m_Sequence.DefaultPlayRate;
        public bool Loop => m_Sequence.Loop;
        public IReadOnlyList<AnimationTimeLaneDescriptor> Lanes => m_Lanes;

        public void Refresh()
        {
            m_Lanes.Clear();
            m_Lanes.Add(AnimationTimeLaneDescriptor.CreateSpan(
                SpanLaneId,
                "SEQUENCE",
                new AnimationTimeSpanDescriptor(
                    m_Sequence.AuthoringId,
                    m_Sequence.Clip ? m_Sequence.Clip.name : "Missing Clip",
                    0,
                    m_Sequence.DurationFrame)));

            var markers = new List<AnimationTimePointDescriptor>(m_Sequence.SyncMarkers.Count);
            for (int i = 0; i < m_Sequence.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = m_Sequence.SyncMarkers[i];
                if (marker != null)
                {
                    markers.Add(new AnimationTimePointDescriptor(
                        marker.AuthoringId,
                        marker.MarkerId,
                        marker.Frame,
                        AnimationTimePointKind.SyncMarker));
                }
            }
            m_Lanes.Add(AnimationTimeLaneDescriptor.CreatePoints(MarkerLaneId, "SYNC MARKERS", markers));

            var notifies = new List<AnimationTimePointDescriptor>(m_Sequence.Notifies.Count);
            for (int i = 0; i < m_Sequence.Notifies.Count; i++)
            {
                AnimationSequenceNotify notify = m_Sequence.Notifies[i];
                if (notify != null)
                {
                    notifies.Add(new AnimationTimePointDescriptor(
                        notify.AuthoringId,
                        notify.Kind.ToString(),
                        notify.Frame,
                        AnimationTimePointKind.Notify));
                }
            }
            m_Lanes.Add(AnimationTimeLaneDescriptor.CreatePoints(NotifyLaneId, "NOTIFIES", notifies));

            for (int i = 0; i < m_Sequence.CurveChannels.Count; i++)
            {
                AnimationSequenceCurveChannel channel = m_Sequence.CurveChannels[i];
                if (channel == null)
                    continue;
                string laneId = $"sequence.curve.{channel.ChannelId}";
                m_Lanes.Add(AnimationTimeLaneDescriptor.CreateCurve(
                    laneId,
                    channel.ChannelId,
                    new AnimationTimeCurveDescriptor(
                        channel.ChannelId,
                        channel.ChannelId,
                        channel.ValueDomain,
                        CurveColor(channel.ChannelId),
                        channel.Curve)));
            }
        }

        public void RequireValid() => m_Sequence.RequireValid();

        public void MovePoint(AnimationTimeSelection selection, int frame)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));
            int clamped = Mathf.Clamp(frame, 0, DurationFrame);
            Apply(selection.PointKind == AnimationTimePointKind.SyncMarker ? "Move Marker" : "Move Notify", () =>
            {
                if (selection.PointKind == AnimationTimePointKind.SyncMarker)
                    m_Sequence.MoveMarker(selection.ElementIdentity, clamped);
                else
                    m_Sequence.MoveNotify(selection.ElementIdentity, clamped);
            });
        }

        public void DeletePoint(AnimationTimeSelection selection)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));
            Apply(selection.PointKind == AnimationTimePointKind.SyncMarker ? "Delete Marker" : "Delete Notify", () =>
            {
                if (selection.PointKind == AnimationTimePointKind.SyncMarker)
                    m_Sequence.DeleteMarker(selection.ElementIdentity);
                else
                    m_Sequence.DeleteNotify(selection.ElementIdentity);
            });
        }

        public void BuildInspector(VisualElement container, AnimationTimeSelection selection, Action refresh)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            container.Clear();
            var title = new Label(m_Sequence.name)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14f,
                    marginBottom = 8f
                }
            };
            container.Add(title);
            var clip = new ObjectField("Clip")
            {
                objectType = typeof(UnityEngine.AnimationClip),
                allowSceneObjects = false
            };
            clip.SetValueWithoutNotify(m_Sequence.Clip);
            clip.SetEnabled(false);
            container.Add(clip);
            container.Add(new Label(
                $"{DurationFrame}F · {FrameRate:0.###} fps · {(Loop ? "Loop" : "Finite")} · {m_Sequence.SyncMode}"));
            container.Add(new Label(
                $"{m_Sequence.SyncGroupId} · {m_Sequence.TimeMapping} · {m_Sequence.SequenceTopology}"));
            container.Add(new Label("空间关系在中央时间画布编辑；Details只编辑精确值。"));

            if (selection == null)
                return;
            if (selection.PointKind == AnimationTimePointKind.SyncMarker)
                BuildMarkerInspector(container, selection.ElementIdentity, refresh);
            else
                BuildNotifyInspector(container, selection.ElementIdentity, refresh);
        }

        public void SetCurve(string laneIdentity, AnimationCurve curve, string undoName)
        {
            AnimationTimeLaneDescriptor lane = FindLane(laneIdentity);
            if (lane.Kind != AnimationTimeLaneKind.Curve)
                throw new InvalidOperationException($"Animation time lane '{laneIdentity}' is not a Curve lane.");
            Apply(string.IsNullOrWhiteSpace(undoName) ? "Edit Curve" : undoName, () => m_Sequence.SetCurve(
                lane.Curve.Identity,
                lane.Curve.ValueDomain,
                curve));
        }

        static Color CurveColor(string channelId)
        {
            Color[] palette =
            {
                new Color32(92, 205, 235, 255),
                new Color32(91, 187, 137, 255),
                new Color32(226, 165, 79, 255),
                new Color32(184, 161, 252, 255),
                new Color32(235, 100, 91, 255)
            };
            uint hash = 2166136261;
            for (int i = 0; i < (channelId?.Length ?? 0); i++)
                hash = (hash ^ channelId[i]) * 16777619;
            return palette[(int)(hash % palette.Length)];
        }

        void BuildMarkerInspector(VisualElement container, string identity, Action refresh)
        {
            AnimationSyncMarker marker = FindMarker(identity);
            if (marker == null)
                return;
            container.Add(new Label("Sync Marker")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 12f }
            });
            var name = new TextField("Marker Id") { value = marker.MarkerId };
            name.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (!string.Equals(name.value, marker.MarkerId, StringComparison.Ordinal))
                {
                    Apply("Rename Marker", () => m_Sequence.RenameMarker(marker.AuthoringId, name.value));
                    refresh?.Invoke();
                }
            });
            var frame = new IntegerField("Frame") { value = marker.Frame };
            frame.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (frame.value != marker.Frame)
                {
                    MovePoint(new AnimationTimeSelection(MarkerLaneId, marker.AuthoringId, AnimationTimePointKind.SyncMarker), frame.value);
                    refresh?.Invoke();
                }
            });
            var remove = new Button(() =>
            {
                Apply("Delete Marker", () => m_Sequence.DeleteMarker(marker.AuthoringId));
                refresh?.Invoke();
            }) { text = "Delete Marker" };
            container.Add(name);
            container.Add(frame);
            container.Add(remove);
        }

        void BuildNotifyInspector(VisualElement container, string identity, Action refresh)
        {
            AnimationSequenceNotify notify = FindNotify(identity);
            if (notify == null)
                return;
            container.Add(new Label("Presentation Notify")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 12f }
            });
            container.Add(new Label(notify.Kind.ToString()));
            var frame = new IntegerField("Frame") { value = notify.Frame };
            frame.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (frame.value != notify.Frame)
                {
                    MovePoint(new AnimationTimeSelection(NotifyLaneId, notify.AuthoringId, AnimationTimePointKind.Notify), frame.value);
                    refresh?.Invoke();
                }
            });
            container.Add(frame);
            AddNotifyPayload(container, notify, refresh);
            container.Add(new Button(() =>
            {
                Apply("Delete Notify", () => m_Sequence.DeleteNotify(notify.AuthoringId));
                refresh?.Invoke();
            }) { text = "Delete Notify" });
        }

        void AddNotifyPayload(VisualElement container, AnimationSequenceNotify notify, Action refresh)
        {
            switch (notify.Payload)
            {
                case AnimationSequenceFootstepAudioPayload footstep:
                {
                    var cue = new TextField("Cue Id") { value = footstep.CueId };
                    var foot = new TextField("Foot Id") { value = footstep.FootId };
                    void Commit()
                    {
                        if (string.Equals(cue.value, footstep.CueId, StringComparison.Ordinal) &&
                            string.Equals(foot.value, footstep.FootId, StringComparison.Ordinal))
                            return;
                        Apply("Configure Notify", () => m_Sequence.EnsureNotify(
                            notify.AuthoringId,
                            notify.Kind,
                            notify.Frame,
                            new AnimationSequenceFootstepAudioPayload(cue.value, foot.value)));
                        refresh?.Invoke();
                    }
                    cue.RegisterCallback<FocusOutEvent>(_ => Commit());
                    foot.RegisterCallback<FocusOutEvent>(_ => Commit());
                    container.Add(cue);
                    container.Add(foot);
                    break;
                }
                case AnimationSequenceVisualEffectPayload effect:
                {
                    var effectId = new TextField("Effect Id") { value = effect.EffectId };
                    var socket = new TextField("Socket Id") { value = effect.SocketId };
                    void Commit()
                    {
                        if (string.Equals(effectId.value, effect.EffectId, StringComparison.Ordinal) &&
                            string.Equals(socket.value, effect.SocketId, StringComparison.Ordinal))
                            return;
                        Apply("Configure Notify", () => m_Sequence.EnsureNotify(
                            notify.AuthoringId,
                            notify.Kind,
                            notify.Frame,
                            new AnimationSequenceVisualEffectPayload(effectId.value, socket.value)));
                        refresh?.Invoke();
                    }
                    effectId.RegisterCallback<FocusOutEvent>(_ => Commit());
                    socket.RegisterCallback<FocusOutEvent>(_ => Commit());
                    container.Add(effectId);
                    container.Add(socket);
                    break;
                }
                case AnimationSequenceEditorAnnotationPayload annotation:
                {
                    var text = new TextField("Text") { value = annotation.Text };
                    text.RegisterCallback<FocusOutEvent>(_ =>
                    {
                        if (string.Equals(text.value, annotation.Text, StringComparison.Ordinal))
                            return;
                        Apply("Configure Notify", () => m_Sequence.EnsureNotify(
                            notify.AuthoringId,
                            notify.Kind,
                            notify.Frame,
                            new AnimationSequenceEditorAnnotationPayload(text.value)));
                        refresh?.Invoke();
                    });
                    container.Add(text);
                    break;
                }
            }
        }

        void Apply(string name, Action mutation)
        {
            m_Sequence.ApplyModify(mutation, name);
            Refresh();
        }

        AnimationTimeLaneDescriptor FindLane(string identity)
        {
            for (int i = 0; i < m_Lanes.Count; i++)
                if (string.Equals(m_Lanes[i].Identity, identity, StringComparison.Ordinal))
                    return m_Lanes[i];
            throw new KeyNotFoundException($"Animation time lane '{identity}' was not found.");
        }

        AnimationSyncMarker FindMarker(string identity)
        {
            for (int i = 0; i < m_Sequence.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = m_Sequence.SyncMarkers[i];
                if (marker != null && string.Equals(marker.AuthoringId, identity, StringComparison.Ordinal))
                    return marker;
            }
            return null;
        }

        AnimationSequenceNotify FindNotify(string identity)
        {
            for (int i = 0; i < m_Sequence.Notifies.Count; i++)
            {
                AnimationSequenceNotify notify = m_Sequence.Notifies[i];
                if (notify != null && string.Equals(notify.AuthoringId, identity, StringComparison.Ordinal))
                    return notify;
            }
            return null;
        }
    }

    internal static class AnimationTimeDocumentLayout
    {
        public const float PointLaneHeight = 28f;

        public static float CurveHeaderTop =>
            TimelineTrackLayout.ClipRowHeight + PointLaneHeight * 2f;

        public static float CurveLaneTop(int visibleChannelIndex) =>
            CurveHeaderTop + TimelineTrackLayout.CurveHeaderHeight +
            visibleChannelIndex * TimelineTrackLayout.CurveLaneHeight;

        public static float ContentHeight(IAnimationTimeDocumentAdapter document)
        {
            int visibleCurves = 0;
            for (int i = 0; i < document.Lanes.Count; i++)
            {
                AnimationTimeLaneDescriptor lane = document.Lanes[i];
                if (lane.Kind == AnimationTimeLaneKind.Curve &&
                    AnimationTimeEditorSession.IsChannelVisible(document, lane.Identity))
                    visibleCurves++;
            }
            float height = CurveHeaderTop;
            if (CurveCount(document) > 0)
                height += TimelineTrackLayout.CurveHeaderHeight +
                          (AnimationTimeEditorSession.CurvesExpanded(document)
                              ? visibleCurves * TimelineTrackLayout.CurveLaneHeight
                              : 0f);
            return height;
        }

        public static int CurveCount(IAnimationTimeDocumentAdapter document)
        {
            int count = 0;
            for (int i = 0; i < document.Lanes.Count; i++)
                if (document.Lanes[i].Kind == AnimationTimeLaneKind.Curve)
                    count++;
            return count;
        }
    }

    internal static class AnimationTimeEditorSession
    {
        static string Key(IAnimationTimeDocumentAdapter document, string suffix) =>
            $"BTSMTL.AnimationTime.{document.DocumentIdentity}.{suffix}";

        public static bool CurvesExpanded(IAnimationTimeDocumentAdapter document) =>
            SessionState.GetBool(Key(document, "CurvesExpanded"), true);

        public static void ToggleCurves(IAnimationTimeDocumentAdapter document) =>
            SessionState.SetBool(Key(document, "CurvesExpanded"), !CurvesExpanded(document));

        public static bool IsChannelVisible(IAnimationTimeDocumentAdapter document, string laneIdentity) =>
            SessionState.GetBool(Key(document, $"CurveVisible.{laneIdentity}"), true);

        public static void ToggleChannel(IAnimationTimeDocumentAdapter document, string laneIdentity) =>
            SessionState.SetBool(
                Key(document, $"CurveVisible.{laneIdentity}"),
                !IsChannelVisible(document, laneIdentity));

        public static TimelineCurveVerticalView GetVerticalView(
            IAnimationTimeDocumentAdapter document,
            string laneIdentity,
            TimelineCurveValueDomain domain)
        {
            if (domain.IsBounded)
                return new TimelineCurveVerticalView(domain.Minimum, domain.Maximum);
            string prefix = Key(document, $"CurveView.{laneIdentity}");
            return new TimelineCurveVerticalView(
                SessionState.GetFloat($"{prefix}.Min", -1f),
                SessionState.GetFloat($"{prefix}.Max", 1f));
        }

        public static void SetVerticalView(
            IAnimationTimeDocumentAdapter document,
            string laneIdentity,
            TimelineCurveVerticalView view)
        {
            string prefix = Key(document, $"CurveView.{laneIdentity}");
            SessionState.SetFloat($"{prefix}.Min", view.Minimum);
            SessionState.SetFloat($"{prefix}.Max", view.Maximum);
        }
    }
}
