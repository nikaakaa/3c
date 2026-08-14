using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Editor;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    internal readonly struct TimelineClipEditState
    {
        public TimelineClipEditState(Clip clip)
        {
            StartFrame = clip.StartFrame;
            EndFrame = clip.EndFrame;
            ClipInFrame = clip.ClipInFrame;
            SelfEaseInFrame = clip.SelfEaseInFrame;
            SelfEaseOutFrame = clip.SelfEaseOutFrame;
        }

        public int StartFrame { get; }
        public int EndFrame { get; }
        public int ClipInFrame { get; }
        public int SelfEaseInFrame { get; }
        public int SelfEaseOutFrame { get; }

        public void Apply(Clip clip)
        {
            clip.StartFrame = StartFrame;
            clip.EndFrame = EndFrame;
            clip.ClipInFrame = ClipInFrame;
            clip.SelfEaseInFrame = SelfEaseInFrame;
            clip.SelfEaseOutFrame = SelfEaseOutFrame;
            clip.Invalid = false;
        }

        public bool Equals(Clip clip)
        {
            return StartFrame == clip.StartFrame &&
                   EndFrame == clip.EndFrame &&
                   ClipInFrame == clip.ClipInFrame &&
                   SelfEaseInFrame == clip.SelfEaseInFrame &&
                   SelfEaseOutFrame == clip.SelfEaseOutFrame;
        }
    }

    internal interface ITimelineInteractionHost
    {
        TimelineFrameGeometry Geometry { get; }
        TimelineData TimelineData { get; }
        int MinimumVisibleFrame { get; }
        int MaximumVisibleFrame { get; }
        void PresentSelection(object target);
        void SetEditFrames(params int[] frames);
        void RefreshPreview();
    }

    internal sealed class TimelineInteractionState
    {
        readonly ITimelineInteractionHost m_Host;
        readonly List<ISelectable> m_Elements = new List<ISelectable>();
        readonly List<ISelectable> m_Selections = new List<ISelectable>();
        readonly List<(TimelineClipView View, TimelineClipEditState State)> m_EditClips =
            new List<(TimelineClipView, TimelineClipEditState)>();

        TimelineClipView m_EditLeader;
        int m_EditBorder;
        bool m_IsPanning;
        float m_PanPointerX;

        public TimelineInteractionState(ITimelineInteractionHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public IReadOnlyList<ISelectable> Elements => m_Elements;
        public IReadOnlyList<ISelectable> Selections => m_Selections;
        public bool IsPanning => m_IsPanning;

        public void RegisterElement(ISelectable selectable)
        {
            if (selectable == null)
                throw new ArgumentNullException(nameof(selectable));
            if (!m_Elements.Contains(selectable))
                m_Elements.Add(selectable);
        }

        public IReadOnlyList<object> CaptureSelectedTargets()
        {
            return m_Selections.Select(selection =>
                {
                    if (selection is TimelineClipView clipView)
                        return (object)clipView.Clip;
                    if (selection is TimelineTrackView trackView)
                        return trackView.Track;
                    if (selection is TimelineSectionView sectionView)
                        return sectionView.Section;
                    if (selection is AnimationTimePointView pointView)
                        return pointView.Selection;
                    return null;
                })
                .Where(target => target != null)
                .ToArray();
        }

        public void ResetViewState()
        {
            m_Selections.Clear();
            m_Elements.Clear();
            CancelEdit();
        }

        public void AddToSelection(ISelectable selectable)
        {
            if (selectable == null || m_Selections.Contains(selectable))
                return;
            m_Selections.Add(selectable);
            selectable.Select();
            if (selectable is TimelineTrackView trackView)
                m_Host.PresentSelection(trackView.Track);
            else if (selectable is TimelineClipView clipView)
                m_Host.PresentSelection(clipView.Clip);
            else if (selectable is TimelineSectionView sectionView)
                m_Host.PresentSelection(sectionView.Section);
            else if (selectable is AnimationTimePointView pointView)
                m_Host.PresentSelection(pointView.Selection);
        }

        public void RemoveFromSelection(ISelectable selectable)
        {
            if (selectable == null || !m_Selections.Remove(selectable))
                return;
            selectable.Unselect();
            if (m_Selections.Count == 0)
                m_Host.PresentSelection(null);
        }

        public void ClearSelection()
        {
            for (int i = 0; i < m_Selections.Count; i++)
                m_Selections[i].Unselect();
            m_Selections.Clear();
            m_Host.PresentSelection(null);
        }

        public void BeginPan(float pointerX)
        {
            m_IsPanning = true;
            m_PanPointerX = pointerX;
        }

        public float UpdatePan(float pointerX)
        {
            float delta = m_PanPointerX - pointerX;
            m_PanPointerX = pointerX;
            return delta;
        }

        public void EndPan()
        {
            m_IsPanning = false;
        }

        public void BeginResize(TimelineClipView clipView, int border)
        {
            BeginSingleClipEdit(clipView, border);
        }

        public void UpdateResize(float deltaPosition)
        {
            RequireEditLeader();
            RestoreEditState();
            TimelineClipEditState original = m_EditClips[0].State;
            TimelineFrameGeometry geometry = m_Host.Geometry;
            TimelineClipView clipView = m_EditLeader;
            int targetFrame;
            if (m_EditBorder == 0)
            {
                targetFrame = geometry.PositionToClosestFrame(geometry.FrameToPosition(original.StartFrame) + deltaPosition);
                int minimum = clipView.Clip.IsClipInable()
                    ? Mathf.Max(m_Host.MinimumVisibleFrame, original.StartFrame - original.ClipInFrame)
                    : m_Host.MinimumVisibleFrame;
                targetFrame = Mathf.Clamp(targetFrame, minimum, Mathf.Min(original.EndFrame - 1, m_Host.MaximumVisibleFrame));
                if (!clipView.Clip.IsMixable())
                {
                    Clip left = geometry.GetClosestLeftClip(clipView.Clip);
                    if (left != null)
                        targetFrame = Mathf.Max(targetFrame, left.EndFrame);
                }
                else
                {
                    targetFrame = Mathf.Min(targetFrame, original.EndFrame - clipView.OtherEaseOutFrame);
                    Clip overlap = geometry.GetSameStartOverlap(clipView.Clip);
                    if (overlap != null && targetFrame <= overlap.StartFrame)
                        return;
                    Clip left = geometry.GetClosestLeftClip(clipView.Clip);
                    if (left != null)
                        targetFrame = Mathf.Max(targetFrame, left.StartFrame + left.OtherEaseInFrame);
                }
                clipView.Resize(targetFrame, original.EndFrame);
            }
            else
            {
                targetFrame = geometry.PositionToClosestFrame(geometry.FrameToPosition(original.EndFrame) + deltaPosition);
                targetFrame = Mathf.Clamp(targetFrame, Mathf.Max(original.StartFrame + 1, m_Host.MinimumVisibleFrame), m_Host.MaximumVisibleFrame);
                if (!clipView.Clip.IsMixable())
                {
                    Clip right = geometry.GetClosestRightClip(clipView.Clip);
                    if (right != null)
                        targetFrame = Mathf.Min(targetFrame, right.StartFrame);
                }
                else
                {
                    targetFrame = Mathf.Max(targetFrame, original.StartFrame + clipView.OtherEaseInFrame);
                    Clip right = geometry.GetClosestRightClip(clipView.Clip);
                    if (right != null)
                        targetFrame = Mathf.Min(targetFrame, right.EndFrame - right.OtherEaseOutFrame);
                }
                clipView.Resize(original.StartFrame, targetFrame);
            }
            clipView.Refresh();
            m_Host.SetEditFrames(targetFrame);
            m_Host.RefreshPreview();
        }

        public void BeginEase(TimelineClipView clipView, int border)
        {
            BeginSingleClipEdit(clipView, border);
        }

        public void UpdateEase(float deltaPosition)
        {
            RequireEditLeader();
            RestoreEditState();
            TimelineClipEditState original = m_EditClips[0].State;
            TimelineClipView clipView = m_EditLeader;
            TimelineFrameGeometry geometry = m_Host.Geometry;
            int targetFrame;
            int deltaFrame;
            if (m_EditBorder == 0)
            {
                int originalFrame = original.StartFrame + original.SelfEaseInFrame;
                targetFrame = geometry.PositionToClosestFrame(geometry.FrameToPosition(originalFrame) + deltaPosition);
                targetFrame = Mathf.Clamp(targetFrame,
                    Mathf.Max(original.StartFrame, m_Host.MinimumVisibleFrame),
                    Mathf.Min(original.EndFrame - clipView.EaseOutFrame, m_Host.MaximumVisibleFrame));
                deltaFrame = targetFrame - originalFrame;
            }
            else
            {
                int originalFrame = original.EndFrame - original.SelfEaseOutFrame;
                targetFrame = geometry.PositionToClosestFrame(geometry.FrameToPosition(originalFrame) + deltaPosition);
                targetFrame = Mathf.Clamp(targetFrame,
                    Mathf.Max(original.StartFrame + clipView.EaseInFrame, m_Host.MinimumVisibleFrame),
                    Mathf.Min(original.EndFrame, m_Host.MaximumVisibleFrame));
                deltaFrame = targetFrame - originalFrame;
            }
            clipView.AdjustSelfEase(m_EditBorder, deltaFrame);
            clipView.Refresh();
            m_Host.SetEditFrames(targetFrame);
            m_Host.RefreshPreview();
        }

        public void BeginMove(TimelineClipView moveLeader)
        {
            CancelEdit();
            m_EditLeader = moveLeader ?? throw new ArgumentNullException(nameof(moveLeader));
            for (int i = 0; i < m_Selections.Count; i++)
            {
                if (m_Selections[i] is TimelineClipView clipView)
                    m_EditClips.Add((clipView, new TimelineClipEditState(clipView.Clip)));
            }
            if (m_EditClips.Count == 0)
                throw new InvalidOperationException("Timeline move requires at least one selected clip.");
        }

        public void UpdateMove(float deltaPosition)
        {
            RequireEditLeader();
            RestoreEditState();
            int startFrame = m_EditClips.Min(item => item.State.StartFrame);
            int endFrame = m_EditClips.Max(item => item.State.EndFrame);
            int targetStartFrame = m_Host.Geometry.PositionToClosestFrame(
                m_Host.Geometry.FrameToPosition(startFrame) + deltaPosition);
            targetStartFrame = Mathf.Clamp(targetStartFrame, m_Host.MinimumVisibleFrame, m_Host.MaximumVisibleFrame);
            int deltaFrame = targetStartFrame - startFrame;
            if (deltaFrame == 0)
                return;
            m_Host.Geometry.EnsureFrameCapacity(endFrame + deltaFrame);
            for (int i = 0; i < m_EditClips.Count; i++)
                m_EditClips[i].View.Move(deltaFrame);
            UpdateEditedTracks();
            for (int i = 0; i < m_EditClips.Count; i++)
            {
                TimelineClipView view = m_EditClips[i].View;
                view.Clip.Invalid = !m_Host.Geometry.IsMoveValid(view.Clip);
                view.Refresh();
            }
            m_Host.SetEditFrames(startFrame + deltaFrame, endFrame + deltaFrame);
            m_Host.RefreshPreview();
        }

        public void CommitEdit(string undoName)
        {
            RequireEditLeader();
            TimelineClipEditState[] finalStates = m_EditClips.Select(item => new TimelineClipEditState(item.View.Clip)).ToArray();
            bool changed = false;
            bool valid = true;
            for (int i = 0; i < m_EditClips.Count; i++)
            {
                changed |= !m_EditClips[i].State.Equals(m_EditClips[i].View.Clip);
                valid &= m_Host.Geometry.IsMoveValid(m_EditClips[i].View.Clip);
            }
            RestoreEditState();
            if (changed && valid)
            {
                m_Host.TimelineData.ApplyModify(() =>
                {
                    for (int i = 0; i < m_EditClips.Count; i++)
                        finalStates[i].Apply(m_EditClips[i].View.Clip);
                    UpdateEditedTracks();
                }, undoName);
            }
            else
            {
                UpdateEditedTracks();
            }
            m_Host.RefreshPreview();
            m_Host.SetEditFrames();
            CancelEdit();
        }

        void BeginSingleClipEdit(TimelineClipView clipView, int border)
        {
            CancelEdit();
            m_EditLeader = clipView ?? throw new ArgumentNullException(nameof(clipView));
            m_EditBorder = border;
            m_EditClips.Add((clipView, new TimelineClipEditState(clipView.Clip)));
        }

        void RestoreEditState()
        {
            for (int i = 0; i < m_EditClips.Count; i++)
                m_EditClips[i].State.Apply(m_EditClips[i].View.Clip);
            UpdateEditedTracks();
        }

        void UpdateEditedTracks()
        {
            foreach (Track track in m_EditClips.Select(item => item.View.Clip.Track).Distinct())
                track.UpdateMix();
        }

        void RequireEditLeader()
        {
            if (m_EditLeader == null || m_EditClips.Count == 0)
                throw new InvalidOperationException("Timeline edit transaction is not active.");
        }

        void CancelEdit()
        {
            m_EditLeader = null;
            m_EditBorder = 0;
            m_EditClips.Clear();
        }
    }

}
