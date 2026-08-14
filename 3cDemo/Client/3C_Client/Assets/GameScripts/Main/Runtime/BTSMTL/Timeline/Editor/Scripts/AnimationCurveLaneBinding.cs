using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    internal interface IAnimationCurveLaneBinding
    {
        TimelineFieldView FieldView { get; }
        string Identity { get; }
        string DisplayName { get; }
        Color Color { get; }
        TimelineCurveTimeDomain TimeDomain { get; }
        TimelineCurveValueDomain ValueDomain { get; }
        IReadOnlyList<object> Owners { get; }
        bool RuntimeReadOnly { get; }
        bool Supports(object owner);
        string OwnerIdentity(object owner);
        string OwnerDisplayName(object owner);
        int StartFrame(object owner);
        int EndFrame(object owner);
        int NormalizedTimeToFrame(object owner, float normalizedTime);
        float FrameToNormalizedTime(object owner, int frame);
        float NormalizedTimeToPosition(object owner, float normalizedTime);
        float PositionToNormalizedTime(object owner, float position);
        bool TryGetCurrentNormalizedTime(object owner, out float normalizedTime);
        AnimationCurve Read(object owner);
        void Validate(object owner, AnimationCurve curve);
        TimelineCurveVerticalView GetVerticalView();
        void SetVerticalView(TimelineCurveVerticalView view);
        void Commit(
            IReadOnlyDictionary<object, AnimationCurve> curves,
            string undoName,
            AnimationCurveSelection selectionAfter = null);
    }

    internal sealed class TimelineCurveLaneBinding : IAnimationCurveLaneBinding
    {
        readonly TimelineTrackView m_TrackView;
        readonly object[] m_Owners;

        public TimelineCurveLaneBinding(TimelineTrackView trackView, TimelineCurveChannelDescriptor descriptor)
        {
            m_TrackView = trackView ?? throw new ArgumentNullException(nameof(trackView));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Owners = trackView.Track.Clips.Where(descriptor.Supports).Cast<object>().ToArray();
        }

        public TimelineCurveChannelDescriptor Descriptor { get; }
        public TimelineFieldView FieldView => m_TrackView.FieldView;
        public string Identity => Descriptor.ChannelId.Value;
        public string DisplayName => Descriptor.DisplayName;
        public Color Color => Descriptor.Color;
        public TimelineCurveTimeDomain TimeDomain => Descriptor.TimeDomain;
        public TimelineCurveValueDomain ValueDomain => Descriptor.ValueDomain;
        public IReadOnlyList<object> Owners => m_Owners;
        public bool RuntimeReadOnly => m_TrackView.RuntimeReadOnly;
        public bool Supports(object owner) => owner is Clip clip && Descriptor.Supports(clip);
        public string OwnerIdentity(object owner) => ((Clip)owner).AuthoringId;
        public string OwnerDisplayName(object owner) => ((Clip)owner).Name;
        public int StartFrame(object owner) => ((Clip)owner).StartFrame;
        public int EndFrame(object owner) => ((Clip)owner).EndFrame;
        public int NormalizedTimeToFrame(object owner, float normalizedTime) =>
            FieldView.Geometry.ClipNormalizedTimeToFrame((Clip)owner, normalizedTime);
        public float FrameToNormalizedTime(object owner, int frame) =>
            FieldView.Geometry.FrameToClipNormalizedTime((Clip)owner, frame);
        public float NormalizedTimeToPosition(object owner, float normalizedTime) =>
            FieldView.Geometry.ClipNormalizedTimeToPosition((Clip)owner, normalizedTime);
        public float PositionToNormalizedTime(object owner, float position) =>
            FieldView.Geometry.PositionToClipNormalizedTime((Clip)owner, position);

        public bool TryGetCurrentNormalizedTime(object owner, out float normalizedTime)
        {
            var clip = (Clip)owner;
            float time = m_TrackView.EditorWindow.PreviewSession.Time;
            if (time < clip.StartTime || time > clip.EndTime)
            {
                normalizedTime = 0f;
                return false;
            }
            normalizedTime = Mathf.InverseLerp(clip.StartTime, clip.EndTime, time);
            return true;
        }

        public AnimationCurve Read(object owner) => Descriptor.Read((Clip)owner);
        public void Validate(object owner, AnimationCurve curve) => Descriptor.Validate((Clip)owner, curve);
        public TimelineCurveVerticalView GetVerticalView() =>
            ValueDomain.IsBounded
                ? new TimelineCurveVerticalView(ValueDomain.Minimum, ValueDomain.Maximum)
                : TimelineCurveEditorSession.GetVerticalView(m_TrackView.Track, Descriptor);
        public void SetVerticalView(TimelineCurveVerticalView view) =>
            TimelineCurveEditorSession.SetVerticalView(m_TrackView.Track, Descriptor.ChannelId, view);

        public void Commit(
            IReadOnlyDictionary<object, AnimationCurve> curves,
            string undoName,
            AnimationCurveSelection selectionAfter = null) =>
            FieldView.CommitAuthoringMutation(
                () =>
                {
                    foreach (KeyValuePair<object, AnimationCurve> pair in curves)
                        Descriptor.Replace((Clip)pair.Key, pair.Value);
                },
                undoName,
                selectionAfter);
    }

    internal sealed class AnimationSequenceCurveLaneBinding : IAnimationCurveLaneBinding
    {
        readonly IAnimationTimeDocumentAdapter m_Document;
        readonly AnimationSequenceAsset m_Sequence;
        readonly string m_LaneIdentity;
        readonly string m_ChannelIdentity;
        readonly object[] m_Owners;

        public AnimationSequenceCurveLaneBinding(
            TimelineFieldView fieldView,
            IAnimationTimeDocumentAdapter document,
            AnimationTimeLaneDescriptor lane)
        {
            FieldView = fieldView ?? throw new ArgumentNullException(nameof(fieldView));
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            m_Sequence = document.Document as AnimationSequenceAsset ??
                         throw new ArgumentException("Sequence curve binding requires AnimationSequenceAsset.", nameof(document));
            if (lane == null || lane.Kind != AnimationTimeLaneKind.Curve)
                throw new ArgumentException("Sequence curve binding requires a Curve lane.", nameof(lane));
            m_LaneIdentity = lane.Identity;
            m_ChannelIdentity = lane.Curve.Identity;
            DisplayName = lane.Curve.Label;
            Color = lane.Curve.Color;
            ValueDomain = ConvertDomain(lane.Curve.ValueDomain);
            m_Owners = new object[] { m_Sequence };
        }

        public TimelineFieldView FieldView { get; }
        public string Identity => m_LaneIdentity;
        public string DisplayName { get; }
        public Color Color { get; }
        public TimelineCurveTimeDomain TimeDomain => TimelineCurveTimeDomain.ClipNormalized;
        public TimelineCurveValueDomain ValueDomain { get; }
        public IReadOnlyList<object> Owners => m_Owners;
        public bool RuntimeReadOnly => FieldView.RuntimeReadOnly;
        public bool Supports(object owner) => ReferenceEquals(owner, m_Sequence);
        public string OwnerIdentity(object owner) => m_Sequence.AuthoringId;
        public string OwnerDisplayName(object owner) => m_Sequence.name;
        public int StartFrame(object owner) => 0;
        public int EndFrame(object owner) => m_Document.DurationFrame;
        public int NormalizedTimeToFrame(object owner, float normalizedTime) =>
            Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(normalizedTime) * EndFrame(owner)), 0, EndFrame(owner));
        public float FrameToNormalizedTime(object owner, int frame) =>
            EndFrame(owner) <= 0 ? 0f : Mathf.Clamp(frame, 0, EndFrame(owner)) / (float)EndFrame(owner);
        public float NormalizedTimeToPosition(object owner, float normalizedTime) =>
            FieldView.Geometry.FrameToPosition(NormalizedTimeToFrame(owner, normalizedTime));
        public float PositionToNormalizedTime(object owner, float position) =>
            FrameToNormalizedTime(owner, FieldView.Geometry.PositionToClosestFrame(position));

        public bool TryGetCurrentNormalizedTime(object owner, out float normalizedTime)
        {
            normalizedTime = FrameToNormalizedTime(owner, FieldView.EditorWindow.AuthoringFrame);
            return true;
        }

        public AnimationCurve Read(object owner) => FindChannel().Curve;

        public void Validate(object owner, AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"Animation Sequence curve '{m_ChannelIdentity}' requires at least one key.");
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!float.IsFinite(key.time) || !float.IsFinite(key.value) || key.time < 0f || key.time > 1f ||
                    i > 0 && key.time <= keys[i - 1].time ||
                    ValueDomain.IsBounded && (key.value < ValueDomain.Minimum || key.value > ValueDomain.Maximum))
                    throw new InvalidOperationException($"Animation Sequence curve '{m_ChannelIdentity}' key #{i} is invalid.");
            }
        }

        public TimelineCurveVerticalView GetVerticalView() =>
            AnimationTimeEditorSession.GetVerticalView(m_Document, m_LaneIdentity, ValueDomain);
        public void SetVerticalView(TimelineCurveVerticalView view) =>
            AnimationTimeEditorSession.SetVerticalView(m_Document, m_LaneIdentity, view);

        public void Commit(
            IReadOnlyDictionary<object, AnimationCurve> curves,
            string undoName,
            AnimationCurveSelection selectionAfter = null)
        {
            if (curves.Count != 1 || !curves.TryGetValue(m_Sequence, out AnimationCurve curve))
                throw new InvalidOperationException("Animation Sequence curve mutation must target its single material span.");
            FieldView.CommitTimeDocumentCurveMutation(m_LaneIdentity, curve, undoName, selectionAfter);
        }

        AnimationSequenceCurveChannel FindChannel()
        {
            for (int i = 0; i < m_Sequence.CurveChannels.Count; i++)
            {
                AnimationSequenceCurveChannel channel = m_Sequence.CurveChannels[i];
                if (channel != null && string.Equals(channel.ChannelId, m_ChannelIdentity, StringComparison.Ordinal))
                    return channel;
            }
            throw new KeyNotFoundException($"Animation Sequence curve '{m_ChannelIdentity}' was not found.");
        }

        static TimelineCurveValueDomain ConvertDomain(AnimationSequenceCurveValueDomain domain) => domain switch
        {
            AnimationSequenceCurveValueDomain.Normalized01 => TimelineCurveValueDomain.Bounded(0f, 1f),
            AnimationSequenceCurveValueDomain.SignedNormalized => TimelineCurveValueDomain.Bounded(-1f, 1f),
            AnimationSequenceCurveValueDomain.Unbounded => TimelineCurveValueDomain.Unbounded(0f, string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
    }
}
