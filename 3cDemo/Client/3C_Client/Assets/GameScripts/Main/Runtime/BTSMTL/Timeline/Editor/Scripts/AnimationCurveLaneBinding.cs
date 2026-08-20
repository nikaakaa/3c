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

}
