using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    internal sealed class TimelineCurveSelection
    {
        public TimelineCurveSelection(Clip owner, TimelineCurveChannelDescriptor descriptor, IEnumerable<int> keyIndices)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            OwnerAuthoringId = owner.AuthoringId;
            KeyIndices = keyIndices?.Distinct().OrderBy(value => value).ToArray() ?? Array.Empty<int>();
            Revision = TimelineCurveAuthoring.Revision(descriptor.Read(owner));
        }

        public Clip Owner { get; }
        public string OwnerAuthoringId { get; }
        public TimelineCurveChannelDescriptor Descriptor { get; }
        public IReadOnlyList<int> KeyIndices { get; }
        public ulong Revision { get; }
        public TimelineAuthoringElementIdentity Identity => new TimelineAuthoringElementIdentity(
            TimelineAuthoringContentKind.ContinuousCurve,
            OwnerAuthoringId,
            Descriptor.ChannelId.Value);
    }

    internal readonly struct TimelineCurveKeyAddress : IEquatable<TimelineCurveKeyAddress>
    {
        public TimelineCurveKeyAddress(Clip owner, int keyIndex)
        {
            Owner = owner;
            KeyIndex = keyIndex;
        }

        public Clip Owner { get; }
        public int KeyIndex { get; }
        public bool Equals(TimelineCurveKeyAddress other) => ReferenceEquals(Owner, other.Owner) && KeyIndex == other.KeyIndex;
        public override bool Equals(object obj) => obj is TimelineCurveKeyAddress other && Equals(other);
        public override int GetHashCode() => (Owner != null ? Owner.GetHashCode() : 0) * 397 ^ KeyIndex;
    }

    internal sealed class TimelineCurveClipboard
    {
        public TimelineCurveTimeDomain TimeDomain;
        public TimelineCurveValueDomain ValueDomain;
        public readonly List<Keyframe> Keys = new List<Keyframe>();
    }

    internal sealed class TimelineCurveChannelLaneView : VisualElement
    {
        enum Gesture
        {
            None,
            Keys,
            Box,
            InTangent,
            OutTangent,
            VerticalPan
        }

        const float VerticalPadding = 6f;
        const float SamplePixelStep = 4f;
        const int MaximumSamples = 512;
        const float KeyRadius = 4f;
        const float KeyHitRadius = 9f;
        const float TangentHitRadius = 8f;
        static TimelineCurveClipboard s_Clipboard;

        readonly TimelineTrackView m_TrackView;
        readonly TimelineCurveChannelDescriptor m_Descriptor;
        readonly List<TimelineCurveKeyAddress> m_Selection = new List<TimelineCurveKeyAddress>();
        readonly Dictionary<Clip, AnimationCurve> m_OriginalCurves = new Dictionary<Clip, AnimationCurve>();
        readonly Dictionary<Clip, AnimationCurve> m_DraftCurves = new Dictionary<Clip, AnimationCurve>();
        readonly Dictionary<Clip, ulong> m_SourceRevisions = new Dictionary<Clip, ulong>();
        readonly List<Vector2> m_SampleBuffer = new List<Vector2>(MaximumSamples + 1);
        Gesture m_Gesture;
        int m_PointerId = -1;
        Vector2 m_StartPointer;
        Vector2 m_CurrentPointer;
        Rect m_Box;
        TimelineCurveKeyAddress m_TangentKey;
        bool m_Changed;
        bool m_HasAutoFit;
        float m_LastContextX;

        public TimelineCurveChannelLaneView(
            TimelineTrackView trackView,
            TimelineCurveChannelDescriptor descriptor,
            int visibleChannelIndex)
        {
            m_TrackView = trackView ?? throw new ArgumentNullException(nameof(trackView));
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            name = $"timeline-curve-{descriptor.ChannelId.Value}";
            AddToClassList("timelineCurveChannelLane");
            style.top = TimelineTrackLayout.CurveLaneTop(trackView.Track, visibleChannelIndex);
            tooltip = $"{descriptor.DisplayName} · {descriptor.ValueDomain.Summary}";
            focusable = true;
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            AutoFitIfNeeded();
        }

        public TimelineCurveChannelDescriptor Descriptor => m_Descriptor;

        public void Refresh()
        {
            ClearStaleSelection();
            AutoFitIfNeeded();
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            DrawGrid(painter, context);
            for (int clipIndex = 0; clipIndex < m_TrackView.Track.Clips.Count; clipIndex++)
            {
                Clip clip = m_TrackView.Track.Clips[clipIndex];
                if (!m_Descriptor.Supports(clip) || clip.EndFrame <= clip.StartFrame)
                    continue;
                AnimationCurve curve = CurveForDraw(clip);
                DrawClipBackground(painter, clip);
                DrawCurve(painter, clip, curve);
                DrawKeys(painter, clip, curve);
                DrawCursorSample(painter, clip, curve);
            }
            if (m_Gesture == Gesture.Box)
                DrawSelectionBox(painter);
            DrawSelectedTangents(painter);
        }

        void DrawGrid(Painter2D painter, MeshGenerationContext context)
        {
            TimelineCurveVerticalView view = VerticalView();
            var values = new List<float>();
            if (m_Descriptor.ValueDomain.IsBounded)
            {
                values.Add(m_Descriptor.ValueDomain.Maximum);
                values.Add((m_Descriptor.ValueDomain.Minimum + m_Descriptor.ValueDomain.Maximum) * 0.5f);
                values.Add(m_Descriptor.ValueDomain.Minimum);
            }
            else
            {
                values.Add(view.Maximum);
                if (view.Minimum <= m_Descriptor.ValueDomain.Zero && view.Maximum >= m_Descriptor.ValueDomain.Zero)
                    values.Add(m_Descriptor.ValueDomain.Zero);
                values.Add(view.Minimum);
            }
            for (int i = 0; i < values.Count; i++)
            {
                float y = ValueToY(values[i]);
                painter.strokeColor = new Color(1f, 1f, 1f, Mathf.Approximately(values[i], 0f) ? 0.22f : 0.11f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(contentRect.width, y));
                painter.Stroke();
                string unit = m_Descriptor.ValueDomain.Unit;
                context.DrawText($"{values[i]:0.###}{unit}", new Vector2(3f, y - 7f), 8, new Color(1f, 1f, 1f, 0.46f));
            }
        }

        void DrawClipBackground(Painter2D painter, Clip clip)
        {
            ClipBounds(clip, out float left, out float right);
            bool selected = m_Selection.Any(value => ReferenceEquals(value.Owner, clip));
            Color color = m_Descriptor.Color;
            painter.fillColor = new Color(color.r, color.g, color.b, selected ? 0.10f : 0.035f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, 0f));
            painter.LineTo(new Vector2(right, 0f));
            painter.LineTo(new Vector2(right, TimelineTrackLayout.CurveLaneHeight));
            painter.LineTo(new Vector2(left, TimelineTrackLayout.CurveLaneHeight));
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(color.r, color.g, color.b, 0.25f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, 0f));
            painter.LineTo(new Vector2(left, TimelineTrackLayout.CurveLaneHeight));
            painter.MoveTo(new Vector2(right, 0f));
            painter.LineTo(new Vector2(right, TimelineTrackLayout.CurveLaneHeight));
            painter.Stroke();
        }

        void DrawCurve(Painter2D painter, Clip clip, AnimationCurve curve)
        {
            ClipBounds(clip, out float left, out float right);
            float width = Mathf.Max(1f, right - left);
            int samples = Mathf.Clamp(Mathf.CeilToInt(width / SamplePixelStep), 2, MaximumSamples);
            m_SampleBuffer.Clear();
            for (int sample = 0; sample <= samples; sample++)
            {
                float time = sample / (float)samples;
                m_SampleBuffer.Add(new Vector2(Mathf.Lerp(left, right, time), ValueToY(curve.Evaluate(time))));
            }
            painter.strokeColor = m_Descriptor.Color;
            painter.lineWidth = 2f;
            painter.BeginPath();
            for (int i = 0; i < m_SampleBuffer.Count; i++)
            {
                if (i == 0) painter.MoveTo(m_SampleBuffer[i]);
                else painter.LineTo(m_SampleBuffer[i]);
            }
            painter.Stroke();
        }

        void DrawKeys(Painter2D painter, Clip clip, AnimationCurve curve)
        {
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Vector2 position = KeyPosition(clip, keys[i]);
                bool selected = m_Selection.Contains(new TimelineCurveKeyAddress(clip, i));
                painter.fillColor = selected ? Color.white : m_Descriptor.Color;
                painter.strokeColor = new Color(0.06f, 0.06f, 0.06f, 1f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(position.x, position.y - KeyRadius));
                painter.LineTo(new Vector2(position.x + KeyRadius, position.y));
                painter.LineTo(new Vector2(position.x, position.y + KeyRadius));
                painter.LineTo(new Vector2(position.x - KeyRadius, position.y));
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
            }
        }

        void DrawCursorSample(Painter2D painter, Clip clip, AnimationCurve curve)
        {
            float time = m_TrackView.EditorWindow.PreviewSession.Time;
            if (time < clip.StartTime || time > clip.EndTime)
                return;
            float normalized = Mathf.InverseLerp(clip.StartTime, clip.EndTime, time);
            Vector2 point = new Vector2(
                m_TrackView.FieldView.Geometry.ClipNormalizedTimeToPosition(clip, normalized),
                ValueToY(curve.Evaluate(normalized)));
            painter.fillColor = Color.white;
            painter.BeginPath();
            painter.Arc(point, 2.5f, 0f, 360f);
            painter.Fill();
        }

        void DrawSelectedTangents(Painter2D painter)
        {
            if (m_Selection.Count != 1)
                return;
            TimelineCurveKeyAddress address = m_Selection[0];
            AnimationCurve curve = CurveForDraw(address.Owner);
            if (address.KeyIndex < 0 || address.KeyIndex >= curve.length)
                return;
            Keyframe key = curve.keys[address.KeyIndex];
            Vector2 keyPosition = KeyPosition(address.Owner, key);
            DrawTangent(painter, keyPosition, TangentPosition(address.Owner, curve, address.KeyIndex, false), false);
            DrawTangent(painter, keyPosition, TangentPosition(address.Owner, curve, address.KeyIndex, true), true);
        }

        static void DrawTangent(Painter2D painter, Vector2 key, Vector2 handle, bool outgoing)
        {
            painter.strokeColor = new Color(1f, 1f, 1f, 0.5f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(key);
            painter.LineTo(handle);
            painter.Stroke();
            painter.fillColor = outgoing ? new Color(1f, 0.68f, 0.15f) : new Color(0.45f, 0.75f, 1f);
            painter.BeginPath();
            painter.Arc(handle, 3f, 0f, 360f);
            painter.Fill();
        }

        void DrawSelectionBox(Painter2D painter)
        {
            painter.fillColor = new Color(0.2f, 0.65f, 1f, 0.12f);
            painter.strokeColor = new Color(0.4f, 0.78f, 1f, 0.8f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(m_Box.min);
            painter.LineTo(new Vector2(m_Box.xMax, m_Box.yMin));
            painter.LineTo(m_Box.max);
            painter.LineTo(new Vector2(m_Box.xMin, m_Box.yMax));
            painter.ClosePath();
            painter.Fill();
            painter.Stroke();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (m_TrackView.RuntimeReadOnly)
                return;
            Focus();
            m_LastContextX = evt.localPosition.x;
            if (evt.button == 1)
            {
                ShowContextMenu(evt);
                evt.StopImmediatePropagation();
                return;
            }
            if (evt.button == 2)
            {
                BeginGesture(evt, Gesture.VerticalPan);
                return;
            }
            if (evt.button != 0)
                return;

            if (TryFindTangent(evt.localPosition, out TimelineCurveKeyAddress tangentKey, out bool outgoing))
            {
                m_TangentKey = tangentKey;
                BeginCurveDraft(new[] { tangentKey.Owner });
                BeginGesture(evt, outgoing ? Gesture.OutTangent : Gesture.InTangent);
                return;
            }

            if (TryFindKey(evt.localPosition, out TimelineCurveKeyAddress address))
            {
                if (evt.shiftKey)
                {
                    if (!m_Selection.Remove(address))
                        m_Selection.Add(address);
                }
                else if (!m_Selection.Contains(address))
                {
                    m_Selection.Clear();
                    m_Selection.Add(address);
                }
                PresentSelection(address.Owner);
                BeginCurveDraft(m_Selection.Select(value => value.Owner));
                BeginGesture(evt, Gesture.Keys);
                return;
            }

            if (evt.clickCount >= 2 && TryFindClip(evt.localPosition.x, out Clip clip))
            {
                AddKey(clip, evt.localPosition);
                evt.StopImmediatePropagation();
                return;
            }

            if (!evt.shiftKey)
                m_Selection.Clear();
            BeginGesture(evt, Gesture.Box);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (m_Gesture == Gesture.None || evt.pointerId != m_PointerId || !this.HasPointerCapture(evt.pointerId))
                return;
            m_CurrentPointer = this.WorldToLocal(evt.position);
            switch (m_Gesture)
            {
                case Gesture.Keys:
                    UpdateKeyDrag();
                    break;
                case Gesture.Box:
                    m_Box = Rect.MinMaxRect(
                        Mathf.Min(m_StartPointer.x, m_CurrentPointer.x),
                        Mathf.Min(m_StartPointer.y, m_CurrentPointer.y),
                        Mathf.Max(m_StartPointer.x, m_CurrentPointer.x),
                        Mathf.Max(m_StartPointer.y, m_CurrentPointer.y));
                    UpdateBoxSelection(evt.shiftKey);
                    break;
                case Gesture.InTangent:
                case Gesture.OutTangent:
                    UpdateTangent(m_Gesture == Gesture.OutTangent);
                    break;
                case Gesture.VerticalPan:
                    UpdateVerticalPan();
                    break;
            }
            MarkDirtyRepaint();
            evt.StopImmediatePropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (m_Gesture == Gesture.None || evt.pointerId != m_PointerId)
                return;
            FinishGesture(true);
            evt.StopImmediatePropagation();
        }

        void OnPointerCancel(PointerCancelEvent evt)
        {
            if (m_Gesture == Gesture.None || evt.pointerId != m_PointerId)
                return;
            FinishGesture(false);
            evt.StopImmediatePropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (m_Gesture != Gesture.None && evt.pointerId == m_PointerId)
                FinishGesture(true);
        }

        void OnWheel(WheelEvent evt)
        {
            if (m_Descriptor.ValueDomain.IsBounded)
                return;
            TimelineCurveVerticalView view = VerticalView();
            float pivot = YToValue(evt.localMousePosition.y);
            view = evt.shiftKey
                ? view.Pan(evt.delta.y * 0.02f)
                : view.Zoom(Mathf.Pow(1.1f, evt.delta.y), pivot);
            TimelineCurveEditorSession.SetVerticalView(m_TrackView.Track, m_Descriptor.ChannelId, view);
            MarkDirtyRepaint();
            evt.StopImmediatePropagation();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete)
            {
                DeleteSelectedKeys();
                evt.StopImmediatePropagation();
            }
            else if (evt.actionKey && evt.keyCode == KeyCode.C)
            {
                CopySelectedKeys();
                evt.StopImmediatePropagation();
            }
            else if (evt.actionKey && evt.keyCode == KeyCode.V)
            {
                PasteKeysAt(m_LastContextX);
                evt.StopImmediatePropagation();
            }
            else if (evt.keyCode == KeyCode.F)
            {
                FrameSelected();
                evt.StopImmediatePropagation();
            }
        }

        void ShowContextMenu(PointerDownEvent evt)
        {
            var menu = new GenericMenu();
            bool hasClip = TryFindClip(evt.localPosition.x, out Clip clip);
            menu.AddItem(new GUIContent("Add Key"), false, () =>
            {
                if (hasClip) AddKey(clip, evt.localPosition);
            });
            AddMenuItem(menu, "Delete Selected", m_Selection.Count > 0, DeleteSelectedKeys);
            AddMenuItem(menu, "Copy Selected", m_Selection.Count > 0, CopySelectedKeys);
            AddMenuItem(menu, "Paste", hasClip && ClipboardCompatible(), () => PasteKeysAt(evt.localPosition.x));
            menu.AddSeparator(string.Empty);
            AddTangentMenu(menu, "Tangent/Auto", AnimationUtility.TangentMode.Auto);
            AddTangentMenu(menu, "Tangent/Clamped Auto", AnimationUtility.TangentMode.ClampedAuto);
            AddTangentMenu(menu, "Tangent/Linear", AnimationUtility.TangentMode.Linear);
            AddTangentMenu(menu, "Tangent/Constant", AnimationUtility.TangentMode.Constant);
            AddTangentMenu(menu, "Tangent/Free", AnimationUtility.TangentMode.Free);
            menu.AddSeparator("Tangent/");
            AddWeightedMenu(menu, "Tangent/Weighted/None", WeightedMode.None);
            AddWeightedMenu(menu, "Tangent/Weighted/In", WeightedMode.In);
            AddWeightedMenu(menu, "Tangent/Weighted/Out", WeightedMode.Out);
            AddWeightedMenu(menu, "Tangent/Weighted/Both", WeightedMode.Both);
            menu.AddItem(new GUIContent("Frame Selected"), false, FrameSelected);
            menu.ShowAsContext();
        }

        void AddTangentMenu(GenericMenu menu, string path, AnimationUtility.TangentMode mode) =>
            AddMenuItem(menu, path, m_Selection.Count > 0, () => SetSelectedTangentMode(mode));

        void AddWeightedMenu(GenericMenu menu, string path, WeightedMode mode) =>
            AddMenuItem(menu, path, m_Selection.Count > 0, () => SetSelectedWeightedMode(mode));

        static void AddMenuItem(GenericMenu menu, string path, bool enabled, GenericMenu.MenuFunction action)
        {
            if (enabled) menu.AddItem(new GUIContent(path), false, action);
            else menu.AddDisabledItem(new GUIContent(path));
        }

        void BeginGesture(PointerDownEvent evt, Gesture gesture)
        {
            m_Gesture = gesture;
            m_PointerId = evt.pointerId;
            m_StartPointer = evt.localPosition;
            m_CurrentPointer = m_StartPointer;
            m_Box = new Rect(m_StartPointer, Vector2.zero);
            m_Changed = false;
            this.CapturePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        void FinishGesture(bool commit)
        {
            Gesture gesture = m_Gesture;
            int pointerId = m_PointerId;
            m_Gesture = Gesture.None;
            m_PointerId = -1;
            if (pointerId >= 0 && this.HasPointerCapture(pointerId))
                this.ReleasePointer(pointerId);
            if (commit && m_Changed && (gesture == Gesture.Keys || gesture == Gesture.InTangent || gesture == Gesture.OutTangent))
                CommitDraft(gesture == Gesture.Keys ? "Move Curve Keys" : "Edit Curve Tangent");
            else
                ClearDraft();
            m_Box = default;
            MarkDirtyRepaint();
        }

        void BeginCurveDraft(IEnumerable<Clip> owners)
        {
            ClearDraft();
            foreach (Clip owner in owners.Distinct())
            {
                AnimationCurve curve = m_Descriptor.Read(owner);
                m_OriginalCurves.Add(owner, curve);
                m_DraftCurves.Add(owner, TimelineCurveAuthoring.CopyCurve(curve));
                m_SourceRevisions.Add(owner, TimelineCurveAuthoring.Revision(curve));
            }
        }

        void UpdateKeyDrag()
        {
            if (m_Selection.Count == 0)
                return;
            int rawDeltaFrame = m_TrackView.FieldView.Geometry.PositionToClosestFrame(m_CurrentPointer.x) -
                                m_TrackView.FieldView.Geometry.PositionToClosestFrame(m_StartPointer.x);
            int minimumDelta = int.MinValue;
            int maximumDelta = int.MaxValue;
            float rawValueDelta = YToValue(m_CurrentPointer.y) - YToValue(m_StartPointer.y);
            float minimumValueDelta = float.NegativeInfinity;
            float maximumValueDelta = float.PositiveInfinity;
            var selectedByOwner = m_Selection.GroupBy(value => value.Owner).ToDictionary(group => group.Key, group => new HashSet<int>(group.Select(value => value.KeyIndex)));
            foreach (KeyValuePair<Clip, HashSet<int>> pair in selectedByOwner)
            {
                Keyframe[] keys = m_OriginalCurves[pair.Key].keys;
                foreach (int keyIndex in pair.Value)
                {
                    int frame = m_TrackView.FieldView.Geometry.ClipNormalizedTimeToFrame(pair.Key, keys[keyIndex].time);
                    minimumDelta = Mathf.Max(minimumDelta, pair.Key.StartFrame - frame);
                    maximumDelta = Mathf.Min(maximumDelta, pair.Key.EndFrame - frame);
                    if (keyIndex > 0 && !pair.Value.Contains(keyIndex - 1))
                    {
                        int leftFrame = m_TrackView.FieldView.Geometry.ClipNormalizedTimeToFrame(pair.Key, keys[keyIndex - 1].time);
                        minimumDelta = Mathf.Max(minimumDelta, leftFrame + 1 - frame);
                    }
                    if (keyIndex + 1 < keys.Length && !pair.Value.Contains(keyIndex + 1))
                    {
                        int rightFrame = m_TrackView.FieldView.Geometry.ClipNormalizedTimeToFrame(pair.Key, keys[keyIndex + 1].time);
                        maximumDelta = Mathf.Min(maximumDelta, rightFrame - 1 - frame);
                    }
                    if (m_Descriptor.ValueDomain.IsBounded)
                    {
                        minimumValueDelta = Mathf.Max(minimumValueDelta, m_Descriptor.ValueDomain.Minimum - keys[keyIndex].value);
                        maximumValueDelta = Mathf.Min(maximumValueDelta, m_Descriptor.ValueDomain.Maximum - keys[keyIndex].value);
                    }
                }
            }
            int deltaFrame = Mathf.Clamp(rawDeltaFrame, minimumDelta, maximumDelta);
            float deltaValue = Mathf.Clamp(rawValueDelta, minimumValueDelta, maximumValueDelta);
            foreach (KeyValuePair<Clip, HashSet<int>> pair in selectedByOwner)
            {
                AnimationCurve draft = TimelineCurveAuthoring.CopyCurve(m_OriginalCurves[pair.Key]);
                Keyframe[] keys = draft.keys;
                foreach (int keyIndex in pair.Value)
                {
                    Keyframe key = keys[keyIndex];
                    int frame = m_TrackView.FieldView.Geometry.ClipNormalizedTimeToFrame(pair.Key, key.time) + deltaFrame;
                    key.time = m_TrackView.FieldView.Geometry.FrameToClipNormalizedTime(pair.Key, frame);
                    key.value += deltaValue;
                    keys[keyIndex] = key;
                }
                draft.keys = keys;
                m_DraftCurves[pair.Key] = draft;
            }
            m_Changed = deltaFrame != 0 || !Mathf.Approximately(deltaValue, 0f);
        }

        void UpdateBoxSelection(bool additive)
        {
            if (!additive)
                m_Selection.Clear();
            for (int clipIndex = 0; clipIndex < m_TrackView.Track.Clips.Count; clipIndex++)
            {
                Clip clip = m_TrackView.Track.Clips[clipIndex];
                if (!m_Descriptor.Supports(clip))
                    continue;
                AnimationCurve curve = m_Descriptor.Read(clip);
                Keyframe[] keys = curve.keys;
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    TimelineCurveKeyAddress address = new TimelineCurveKeyAddress(clip, keyIndex);
                    if (m_Box.Contains(KeyPosition(clip, keys[keyIndex])) && !m_Selection.Contains(address))
                        m_Selection.Add(address);
                }
            }
            if (m_Selection.Count > 0)
                PresentSelection(m_Selection[0].Owner);
        }

        void UpdateTangent(bool outgoing)
        {
            if (!m_DraftCurves.TryGetValue(m_TangentKey.Owner, out AnimationCurve curve) ||
                m_TangentKey.KeyIndex < 0 || m_TangentKey.KeyIndex >= curve.length)
                return;
            Keyframe key = curve.keys[m_TangentKey.KeyIndex];
            float time = m_TrackView.FieldView.Geometry.PositionToClipNormalizedTime(m_TangentKey.Owner, m_CurrentPointer.x);
            float value = YToValue(m_CurrentPointer.y);
            float deltaTime = time - key.time;
            if (outgoing && deltaTime <= 0.0001f || !outgoing && deltaTime >= -0.0001f)
                return;
            float tangent = (value - key.value) / deltaTime;
            float interval = 1f;
            if (outgoing && m_TangentKey.KeyIndex + 1 < curve.length)
                interval = curve.keys[m_TangentKey.KeyIndex + 1].time - key.time;
            else if (!outgoing && m_TangentKey.KeyIndex > 0)
                interval = key.time - curve.keys[m_TangentKey.KeyIndex - 1].time;
            float weight = Mathf.Clamp01(Mathf.Abs(deltaTime) / Mathf.Max(0.0001f, interval));
            if (outgoing)
            {
                key.outTangent = tangent;
                key.outWeight = weight;
                key.weightedMode |= WeightedMode.Out;
            }
            else
            {
                key.inTangent = tangent;
                key.inWeight = weight;
                key.weightedMode |= WeightedMode.In;
            }
            curve.MoveKey(m_TangentKey.KeyIndex, key);
            m_DraftCurves[m_TangentKey.Owner] = curve;
            m_Changed = true;
        }

        void UpdateVerticalPan()
        {
            if (m_Descriptor.ValueDomain.IsBounded)
                return;
            TimelineCurveVerticalView view = VerticalView();
            float delta = (m_CurrentPointer.y - m_StartPointer.y) / Mathf.Max(1f, TimelineTrackLayout.CurveLaneHeight);
            TimelineCurveEditorSession.SetVerticalView(m_TrackView.Track, m_Descriptor.ChannelId, view.Pan(delta));
            m_StartPointer = m_CurrentPointer;
        }

        void CommitDraft(string undoName)
        {
            foreach (KeyValuePair<Clip, AnimationCurve> pair in m_DraftCurves)
            {
                ulong current = TimelineCurveAuthoring.Revision(m_Descriptor.Read(pair.Key));
                if (current != m_SourceRevisions[pair.Key])
                {
                    m_Selection.Clear();
                    ClearDraft();
                    Debug.LogError($"Timeline curve '{m_Descriptor.ChannelId}' changed outside the active gesture; stale key indices were discarded.");
                    return;
                }
                m_Descriptor.Validate(pair.Key, pair.Value);
            }
            var final = m_DraftCurves.ToDictionary(pair => pair.Key, pair => TimelineCurveAuthoring.CopyCurve(pair.Value));
            Clip focusOwner = m_Selection.Count > 0 ? m_Selection[0].Owner : final.Keys.First();
            m_TrackView.FieldView.CommitAuthoringMutation(
                () =>
                {
                    foreach (KeyValuePair<Clip, AnimationCurve> pair in final)
                        m_Descriptor.Replace(pair.Key, pair.Value);
                },
                undoName,
                new TimelineCurveSelection(focusOwner, m_Descriptor,
                    m_Selection.Where(value => ReferenceEquals(value.Owner, focusOwner)).Select(value => value.KeyIndex)));
            ClearDraft();
        }

        void AddKey(Clip clip, Vector2 position)
        {
            AnimationCurve curve = m_Descriptor.Read(clip);
            Keyframe key = new Keyframe(
                m_TrackView.FieldView.Geometry.PositionToClipNormalizedTime(clip, position.x),
                YToValue(position.y));
            int index = curve.AddKey(key);
            if (index < 0)
                return;
            m_Descriptor.Validate(clip, curve);
            m_TrackView.FieldView.CommitAuthoringMutation(
                () => m_Descriptor.Replace(clip, curve),
                $"Add {m_Descriptor.DisplayName} Key",
                new TimelineCurveSelection(clip, m_Descriptor, new[] { index }));
        }

        void DeleteSelectedKeys()
        {
            if (m_Selection.Count == 0)
                return;
            var curves = new Dictionary<Clip, AnimationCurve>();
            foreach (IGrouping<Clip, TimelineCurveKeyAddress> group in m_Selection.GroupBy(value => value.Owner))
            {
                AnimationCurve curve = m_Descriptor.Read(group.Key);
                foreach (int index in group.Select(value => value.KeyIndex).Distinct().OrderByDescending(value => value))
                    curve.RemoveKey(index);
                m_Descriptor.Validate(group.Key, curve);
                curves.Add(group.Key, curve);
            }
            m_TrackView.FieldView.CommitAuthoringMutation(
                () =>
                {
                    foreach (KeyValuePair<Clip, AnimationCurve> pair in curves)
                        m_Descriptor.Replace(pair.Key, pair.Value);
                },
                $"Delete {m_Descriptor.DisplayName} Keys");
            m_Selection.Clear();
        }

        void CopySelectedKeys()
        {
            if (m_Selection.Count == 0)
                return;
            s_Clipboard = new TimelineCurveClipboard
            {
                TimeDomain = m_Descriptor.TimeDomain,
                ValueDomain = m_Descriptor.ValueDomain
            };
            TimelineCurveKeyAddress[] ordered = m_Selection
                .OrderBy(value => value.Owner.StartFrame)
                .ThenBy(value => m_Descriptor.Read(value.Owner).keys[value.KeyIndex].time)
                .ToArray();
            for (int i = 0; i < ordered.Length; i++)
                s_Clipboard.Keys.Add(m_Descriptor.Read(ordered[i].Owner).keys[ordered[i].KeyIndex]);
        }

        void PasteKeysAt(float x)
        {
            if (!ClipboardCompatible() || !TryFindClip(x, out Clip clip))
                return;
            AnimationCurve curve = m_Descriptor.Read(clip);
            float anchor = m_TrackView.FieldView.Geometry.PositionToClipNormalizedTime(clip, x);
            float source = s_Clipboard.Keys.Min(value => value.time);
            var added = new List<int>();
            for (int i = 0; i < s_Clipboard.Keys.Count; i++)
            {
                Keyframe key = s_Clipboard.Keys[i];
                key.time = Mathf.Clamp01(anchor + key.time - source);
                int index = curve.AddKey(key);
                if (index < 0)
                    throw new InvalidOperationException("Pasted Timeline curve keys collide with an existing key time.");
                added.Add(index);
            }
            m_Descriptor.Validate(clip, curve);
            m_TrackView.FieldView.CommitAuthoringMutation(
                () => m_Descriptor.Replace(clip, curve),
                $"Paste {m_Descriptor.DisplayName} Keys",
                new TimelineCurveSelection(clip, m_Descriptor, added));
        }

        bool ClipboardCompatible()
        {
            if (s_Clipboard == null || s_Clipboard.Keys.Count == 0 || s_Clipboard.TimeDomain != m_Descriptor.TimeDomain)
                return false;
            TimelineCurveValueDomain source = s_Clipboard.ValueDomain;
            TimelineCurveValueDomain target = m_Descriptor.ValueDomain;
            return source.IsBounded == target.IsBounded &&
                   string.Equals(source.Unit, target.Unit, StringComparison.Ordinal) &&
                   (!source.IsBounded || Mathf.Approximately(source.Minimum, target.Minimum) && Mathf.Approximately(source.Maximum, target.Maximum));
        }

        void SetSelectedTangentMode(AnimationUtility.TangentMode mode)
        {
            if (m_Selection.Count == 0)
                return;
            var curves = new Dictionary<Clip, AnimationCurve>();
            foreach (IGrouping<Clip, TimelineCurveKeyAddress> group in m_Selection.GroupBy(value => value.Owner))
            {
                AnimationCurve curve = m_Descriptor.Read(group.Key);
                foreach (int index in group.Select(value => value.KeyIndex).Distinct())
                {
                    AnimationUtility.SetKeyBroken(curve, index, mode == AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetKeyLeftTangentMode(curve, index, mode);
                    AnimationUtility.SetKeyRightTangentMode(curve, index, mode);
                }
                m_Descriptor.Validate(group.Key, curve);
                curves.Add(group.Key, curve);
            }
            Clip owner = m_Selection[0].Owner;
            m_TrackView.FieldView.CommitAuthoringMutation(
                () =>
                {
                    foreach (KeyValuePair<Clip, AnimationCurve> pair in curves)
                        m_Descriptor.Replace(pair.Key, pair.Value);
                },
                $"Set {m_Descriptor.DisplayName} Tangent",
                new TimelineCurveSelection(owner, m_Descriptor,
                    m_Selection.Where(value => ReferenceEquals(value.Owner, owner)).Select(value => value.KeyIndex)));
        }

        void SetSelectedWeightedMode(WeightedMode mode)
        {
            if (m_Selection.Count == 0)
                return;
            var curves = new Dictionary<Clip, AnimationCurve>();
            foreach (IGrouping<Clip, TimelineCurveKeyAddress> group in m_Selection.GroupBy(value => value.Owner))
            {
                AnimationCurve curve = m_Descriptor.Read(group.Key);
                foreach (int index in group.Select(value => value.KeyIndex).Distinct())
                {
                    Keyframe key = curve.keys[index];
                    key.weightedMode = mode;
                    curve.MoveKey(index, key);
                }
                m_Descriptor.Validate(group.Key, curve);
                curves.Add(group.Key, curve);
            }
            Clip owner = m_Selection[0].Owner;
            m_TrackView.FieldView.CommitAuthoringMutation(
                () =>
                {
                    foreach (KeyValuePair<Clip, AnimationCurve> pair in curves)
                        m_Descriptor.Replace(pair.Key, pair.Value);
                },
                $"Set {m_Descriptor.DisplayName} Weighted Mode",
                new TimelineCurveSelection(owner, m_Descriptor,
                    m_Selection.Where(value => ReferenceEquals(value.Owner, owner)).Select(value => value.KeyIndex)));
        }

        public void FrameSelected()
        {
            if (m_Descriptor.ValueDomain.IsBounded)
                return;
            IEnumerable<float> values;
            if (m_Selection.Count > 0)
            {
                values = m_Selection.Select(address => m_Descriptor.Read(address.Owner).keys[address.KeyIndex].value);
            }
            else
            {
                values = m_TrackView.Track.Clips.Where(m_Descriptor.Supports)
                    .SelectMany(owner => m_Descriptor.Read(owner).keys)
                    .Select(key => key.value);
            }
            float[] array = values.ToArray();
            float minimum = array.Length == 0 ? -1f : array.Min();
            float maximum = array.Length == 0 ? 1f : array.Max();
            float padding = Mathf.Max(0.1f, (maximum - minimum) * 0.12f);
            if (Mathf.Approximately(minimum, maximum))
                padding = Mathf.Max(0.5f, Mathf.Abs(minimum) * 0.2f);
            TimelineCurveEditorSession.SetVerticalView(
                m_TrackView.Track,
                m_Descriptor.ChannelId,
                new TimelineCurveVerticalView(minimum - padding, maximum + padding));
            m_HasAutoFit = true;
            MarkDirtyRepaint();
        }

        void AutoFitIfNeeded()
        {
            if (!m_HasAutoFit && !m_Descriptor.ValueDomain.IsBounded)
                FrameSelected();
        }

        void PresentSelection(Clip owner)
        {
            m_TrackView.FieldView.PresentCurveSelection(new TimelineCurveSelection(
                owner,
                m_Descriptor,
                m_Selection.Where(value => ReferenceEquals(value.Owner, owner)).Select(value => value.KeyIndex)));
        }

        void ClearStaleSelection()
        {
            for (int i = m_Selection.Count - 1; i >= 0; i--)
            {
                TimelineCurveKeyAddress value = m_Selection[i];
                if (!m_Descriptor.Supports(value.Owner) || value.KeyIndex < 0 || value.KeyIndex >= m_Descriptor.Read(value.Owner).length)
                    m_Selection.RemoveAt(i);
            }
        }

        bool TryFindClip(float x, out Clip selected)
        {
            int frame = m_TrackView.FieldView.Geometry.PositionToFloorFrame(x);
            selected = null;
            for (int i = 0; i < m_TrackView.Track.Clips.Count; i++)
            {
                Clip clip = m_TrackView.Track.Clips[i];
                if (!m_Descriptor.Supports(clip) || frame < clip.StartFrame || frame > clip.EndFrame)
                    continue;
                if (selected == null || clip.StartFrame >= selected.StartFrame)
                    selected = clip;
            }
            return selected != null;
        }

        bool TryFindKey(Vector2 position, out TimelineCurveKeyAddress result)
        {
            result = default;
            float nearest = KeyHitRadius * KeyHitRadius;
            bool found = false;
            for (int clipIndex = 0; clipIndex < m_TrackView.Track.Clips.Count; clipIndex++)
            {
                Clip clip = m_TrackView.Track.Clips[clipIndex];
                if (!m_Descriptor.Supports(clip))
                    continue;
                Keyframe[] keys = CurveForDraw(clip).keys;
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    float distance = (KeyPosition(clip, keys[keyIndex]) - position).sqrMagnitude;
                    if (distance > nearest)
                        continue;
                    nearest = distance;
                    result = new TimelineCurveKeyAddress(clip, keyIndex);
                    found = true;
                }
            }
            return found;
        }

        bool TryFindTangent(Vector2 position, out TimelineCurveKeyAddress address, out bool outgoing)
        {
            address = default;
            outgoing = false;
            if (m_Selection.Count != 1)
                return false;
            address = m_Selection[0];
            AnimationCurve curve = CurveForDraw(address.Owner);
            if (address.KeyIndex < 0 || address.KeyIndex >= curve.length)
                return false;
            Vector2 incoming = TangentPosition(address.Owner, curve, address.KeyIndex, false);
            Vector2 outgoingPosition = TangentPosition(address.Owner, curve, address.KeyIndex, true);
            float inDistance = (incoming - position).sqrMagnitude;
            float outDistance = (outgoingPosition - position).sqrMagnitude;
            if (Mathf.Min(inDistance, outDistance) > TangentHitRadius * TangentHitRadius)
                return false;
            outgoing = outDistance < inDistance;
            return true;
        }

        Vector2 TangentPosition(Clip owner, AnimationCurve curve, int keyIndex, bool outgoing)
        {
            Keyframe key = curve.keys[keyIndex];
            float direction = outgoing ? 1f : -1f;
            float interval = 0.15f;
            if (outgoing && keyIndex + 1 < curve.length)
                interval = curve.keys[keyIndex + 1].time - key.time;
            else if (!outgoing && keyIndex > 0)
                interval = key.time - curve.keys[keyIndex - 1].time;
            float weight = outgoing ? key.outWeight : key.inWeight;
            bool weighted = (key.weightedMode & (outgoing ? WeightedMode.Out : WeightedMode.In)) != 0;
            float deltaTime = direction * Mathf.Max(0.02f, interval * (weighted ? Mathf.Clamp(weight, 0.05f, 1f) : 0.33f));
            float tangent = outgoing ? key.outTangent : key.inTangent;
            if (!TimelineCurveAuthoring.IsFinite(tangent))
                tangent = 0f;
            float targetTime = Mathf.Clamp01(key.time + deltaTime);
            float targetValue = key.value + tangent * (targetTime - key.time);
            return new Vector2(
                m_TrackView.FieldView.Geometry.ClipNormalizedTimeToPosition(owner, targetTime),
                ValueToY(targetValue));
        }

        AnimationCurve CurveForDraw(Clip owner) =>
            m_DraftCurves.TryGetValue(owner, out AnimationCurve curve) ? curve : m_Descriptor.Read(owner);

        Vector2 KeyPosition(Clip clip, Keyframe key) => new Vector2(
            m_TrackView.FieldView.Geometry.ClipNormalizedTimeToPosition(clip, key.time),
            ValueToY(key.value));

        void ClipBounds(Clip clip, out float left, out float right)
        {
            left = m_TrackView.FieldView.Geometry.FrameToPosition(clip.StartFrame);
            right = m_TrackView.FieldView.Geometry.FrameToPosition(clip.EndFrame);
        }

        TimelineCurveVerticalView VerticalView()
        {
            if (m_Descriptor.ValueDomain.IsBounded)
                return new TimelineCurveVerticalView(m_Descriptor.ValueDomain.Minimum, m_Descriptor.ValueDomain.Maximum);
            return TimelineCurveEditorSession.GetVerticalView(m_TrackView.Track, m_Descriptor);
        }

        float ValueToY(float value)
        {
            TimelineCurveVerticalView view = VerticalView();
            return Mathf.Lerp(
                TimelineTrackLayout.CurveLaneHeight - VerticalPadding,
                VerticalPadding,
                Mathf.InverseLerp(view.Minimum, view.Maximum, value));
        }

        float YToValue(float y)
        {
            TimelineCurveVerticalView view = VerticalView();
            float value = Mathf.Lerp(
                view.Minimum,
                view.Maximum,
                Mathf.InverseLerp(TimelineTrackLayout.CurveLaneHeight - VerticalPadding, VerticalPadding, y));
            return m_Descriptor.ValueDomain.IsBounded
                ? Mathf.Clamp(value, m_Descriptor.ValueDomain.Minimum, m_Descriptor.ValueDomain.Maximum)
                : value;
        }

        void ClearDraft()
        {
            m_OriginalCurves.Clear();
            m_DraftCurves.Clear();
            m_SourceRevisions.Clear();
            m_Changed = false;
        }
    }

    internal sealed class TimelineCurveInspectorView : VisualElement
    {
        readonly TimelineFieldView m_FieldView;
        readonly TimelineCurveSelection m_Selection;

        public TimelineCurveInspectorView(TimelineFieldView fieldView, TimelineCurveSelection selection)
        {
            m_FieldView = fieldView ?? throw new ArgumentNullException(nameof(fieldView));
            m_Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            Build();
        }

        void Build()
        {
            Add(new Label(m_Selection.Descriptor.DisplayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            Add(new Label($"{m_Selection.Descriptor.ChannelId.Value} · {m_Selection.Owner.Name}"));
            Add(new Label($"Revision {m_Selection.Revision:X16} · {m_Selection.Descriptor.ValueDomain.Summary}"));
            AnimationCurve curve = m_Selection.Descriptor.Read(m_Selection.Owner);
            if (TimelineCurveAuthoring.Revision(curve) != m_Selection.Revision)
            {
                Add(new HelpBox("Curve changed after selection. Select the key again before editing.", HelpBoxMessageType.Error));
                return;
            }
            var preWrap = new EnumField("Pre Wrap", curve.preWrapMode);
            var postWrap = new EnumField("Post Wrap", curve.postWrapMode);
            preWrap.RegisterValueChangedCallback(evt => ReplaceCurve(value => value.preWrapMode = (WrapMode)evt.newValue, "Edit Curve Pre Wrap"));
            postWrap.RegisterValueChangedCallback(evt => ReplaceCurve(value => value.postWrapMode = (WrapMode)evt.newValue, "Edit Curve Post Wrap"));
            Add(preWrap);
            Add(postWrap);
            if (m_Selection.KeyIndices.Count != 1)
            {
                Add(new Label($"{m_Selection.KeyIndices.Count} keys selected"));
                return;
            }
            int keyIndex = m_Selection.KeyIndices[0];
            if (keyIndex < 0 || keyIndex >= curve.length)
            {
                Add(new HelpBox("Selected key no longer exists.", HelpBoxMessageType.Error));
                return;
            }
            Keyframe key = curve.keys[keyIndex];
            int frame = m_FieldView.Geometry.ClipNormalizedTimeToFrame(m_Selection.Owner, key.time);
            var frameField = new IntegerField("Timeline Frame") { value = frame, isDelayed = true };
            var timeField = new FloatField("Normalized Time") { value = key.time, isDelayed = true };
            var valueField = new FloatField($"Value {m_Selection.Descriptor.ValueDomain.Unit}") { value = key.value, isDelayed = true };
            var inTangent = new FloatField("In Tangent") { value = key.inTangent, isDelayed = true };
            var outTangent = new FloatField("Out Tangent") { value = key.outTangent, isDelayed = true };
            var inWeight = new FloatField("In Weight") { value = key.inWeight, isDelayed = true };
            var outWeight = new FloatField("Out Weight") { value = key.outWeight, isDelayed = true };
            var weightedMode = new EnumField("Weighted Mode", key.weightedMode);
            frameField.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value =>
                value.time = m_FieldView.Geometry.FrameToClipNormalizedTime(m_Selection.Owner,
                    Mathf.Clamp(evt.newValue, m_Selection.Owner.StartFrame, m_Selection.Owner.EndFrame)), "Edit Curve Key Frame"));
            timeField.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value => value.time = Mathf.Clamp01(evt.newValue), "Edit Curve Key Time"));
            valueField.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value => value.value = evt.newValue, "Edit Curve Key Value"));
            inTangent.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value => value.inTangent = evt.newValue, "Edit Curve In Tangent"));
            outTangent.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value => value.outTangent = evt.newValue, "Edit Curve Out Tangent"));
            inWeight.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value => value.inWeight = Mathf.Clamp01(evt.newValue), "Edit Curve In Weight"));
            outWeight.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value => value.outWeight = Mathf.Clamp01(evt.newValue), "Edit Curve Out Weight"));
            weightedMode.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, value => value.weightedMode = (WeightedMode)evt.newValue, "Edit Curve Weighted Mode"));
            Add(frameField);
            Add(timeField);
            Add(valueField);
            Add(inTangent);
            Add(outTangent);
            Add(inWeight);
            Add(outWeight);
            Add(weightedMode);
            var tangents = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            AddTangentButton(tangents, "Auto", AnimationUtility.TangentMode.Auto, keyIndex);
            AddTangentButton(tangents, "Clamped", AnimationUtility.TangentMode.ClampedAuto, keyIndex);
            AddTangentButton(tangents, "Linear", AnimationUtility.TangentMode.Linear, keyIndex);
            AddTangentButton(tangents, "Constant", AnimationUtility.TangentMode.Constant, keyIndex);
            AddTangentButton(tangents, "Free", AnimationUtility.TangentMode.Free, keyIndex);
            Add(tangents);
        }

        void AddTangentButton(VisualElement row, string text, AnimationUtility.TangentMode mode, int keyIndex)
        {
            row.Add(new Button(() =>
            {
                AnimationCurve curve = m_Selection.Descriptor.Read(m_Selection.Owner);
                AnimationUtility.SetKeyBroken(curve, keyIndex, mode == AnimationUtility.TangentMode.Free);
                AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, mode);
                AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, mode);
                Commit(curve, $"Set Curve Tangent {text}", keyIndex);
            }) { text = text });
        }

        void ReplaceCurve(Action<AnimationCurve> mutation, string undoName)
        {
            AnimationCurve curve = m_Selection.Descriptor.Read(m_Selection.Owner);
            mutation(curve);
            Commit(curve, undoName, m_Selection.KeyIndices.Count > 0 ? m_Selection.KeyIndices[0] : -1);
        }

        void ReplaceKey(int keyIndex, Action<Keyframe> mutation, string undoName)
        {
            AnimationCurve curve = m_Selection.Descriptor.Read(m_Selection.Owner);
            Keyframe key = curve.keys[keyIndex];
            mutation(key);
            int newIndex = curve.MoveKey(keyIndex, key);
            Commit(curve, undoName, newIndex);
        }

        void Commit(AnimationCurve curve, string undoName, int keyIndex)
        {
            m_Selection.Descriptor.Validate(m_Selection.Owner, curve);
            m_FieldView.CommitAuthoringMutation(
                () => m_Selection.Descriptor.Replace(m_Selection.Owner, curve),
                undoName,
                new TimelineCurveSelection(
                    m_Selection.Owner,
                    m_Selection.Descriptor,
                    keyIndex >= 0 ? new[] { keyIndex } : Array.Empty<int>()));
        }
    }
}
