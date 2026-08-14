using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    internal sealed class AnimationCurveSelection
    {
        public AnimationCurveSelection(
            IAnimationCurveLaneBinding binding,
            object owner,
            IEnumerable<int> keyIndices)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (!binding.Supports(owner))
                throw new ArgumentException("Curve selection owner is not supported by its binding.", nameof(owner));
            OwnerAuthoringId = binding.OwnerIdentity(owner);
            KeyIndices = keyIndices?.Distinct().OrderBy(value => value).ToArray() ?? Array.Empty<int>();
            Revision = TimelineCurveAuthoring.Revision(binding.Read(owner));
        }

        public IAnimationCurveLaneBinding Binding { get; }
        public object Owner { get; }
        public string OwnerAuthoringId { get; }
        public IReadOnlyList<int> KeyIndices { get; }
        public ulong Revision { get; }
    }

    internal readonly struct AnimationCurveKeyAddress : IEquatable<AnimationCurveKeyAddress>
    {
        public AnimationCurveKeyAddress(object owner, int keyIndex)
        {
            Owner = owner;
            KeyIndex = keyIndex;
        }

        public object Owner { get; }
        public int KeyIndex { get; }
        public bool Equals(AnimationCurveKeyAddress other) =>
            ReferenceEquals(Owner, other.Owner) && KeyIndex == other.KeyIndex;
        public override bool Equals(object obj) => obj is AnimationCurveKeyAddress other && Equals(other);
        public override int GetHashCode() => (Owner?.GetHashCode() ?? 0) * 397 ^ KeyIndex;
    }

    internal sealed class AnimationCurveClipboard
    {
        public TimelineCurveTimeDomain TimeDomain;
        public TimelineCurveValueDomain ValueDomain;
        public readonly List<Keyframe> Keys = new List<Keyframe>();
    }

    internal class AnimationCurveChannelLaneView : VisualElement
    {
        enum Gesture
        {
            None,
            Keys,
            Box,
            InTangent,
            OutTangent
        }

        const float VerticalPadding = 6f;
        const float SamplePixelStep = 4f;
        const int MaximumSamples = 512;
        const float KeyRadius = 4f;
        const float KeyHitRadius = 9f;
        const float TangentHitRadius = 8f;
        static AnimationCurveClipboard s_Clipboard;

        readonly IAnimationCurveLaneBinding m_Binding;
        readonly List<AnimationCurveKeyAddress> m_Selection = new List<AnimationCurveKeyAddress>();
        readonly Dictionary<object, AnimationCurve> m_OriginalCurves = new Dictionary<object, AnimationCurve>();
        readonly Dictionary<object, AnimationCurve> m_DraftCurves = new Dictionary<object, AnimationCurve>();
        readonly Dictionary<object, ulong> m_SourceRevisions = new Dictionary<object, ulong>();
        readonly List<Vector2> m_Samples = new List<Vector2>(MaximumSamples + 1);
        Gesture m_Gesture;
        int m_PointerId = -1;
        Vector2 m_StartPointer;
        Vector2 m_CurrentPointer;
        Rect m_Box;
        AnimationCurveKeyAddress m_TangentKey;
        bool m_Changed;
        bool m_HasAutoFit;
        float m_LastContextX;

        public AnimationCurveChannelLaneView(IAnimationCurveLaneBinding binding, float top)
        {
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            name = $"animation-curve-{binding.Identity}";
            AddToClassList("timelineCurveChannelLane");
            style.top = top;
            tooltip = $"{binding.DisplayName} · {binding.ValueDomain.Summary}";
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

        public IAnimationCurveLaneBinding Binding => m_Binding;

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
            for (int i = 0; i < m_Binding.Owners.Count; i++)
            {
                object owner = m_Binding.Owners[i];
                if (!m_Binding.Supports(owner) || m_Binding.EndFrame(owner) <= m_Binding.StartFrame(owner))
                    continue;
                AnimationCurve curve = CurveForDraw(owner);
                DrawOwnerBackground(painter, owner);
                DrawCurve(painter, owner, curve);
                DrawKeys(painter, owner, curve);
                DrawCursorSample(painter, owner, curve);
            }
            if (m_Gesture == Gesture.Box)
                DrawSelectionBox(painter);
            DrawSelectedTangents(painter);
        }

        void DrawGrid(Painter2D painter, MeshGenerationContext context)
        {
            TimelineCurveVerticalView view = VerticalView();
            float[] values = m_Binding.ValueDomain.IsBounded
                ? new[] { m_Binding.ValueDomain.Maximum, (m_Binding.ValueDomain.Minimum + m_Binding.ValueDomain.Maximum) * 0.5f, m_Binding.ValueDomain.Minimum }
                : view.Minimum <= m_Binding.ValueDomain.Zero && view.Maximum >= m_Binding.ValueDomain.Zero
                    ? new[] { view.Maximum, m_Binding.ValueDomain.Zero, view.Minimum }
                    : new[] { view.Maximum, view.Minimum };
            for (int i = 0; i < values.Length; i++)
            {
                float y = ValueToY(values[i]);
                painter.strokeColor = new Color(1f, 1f, 1f, Mathf.Approximately(values[i], 0f) ? 0.22f : 0.11f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(contentRect.width, y));
                painter.Stroke();
                context.DrawText($"{values[i]:0.###}{m_Binding.ValueDomain.Unit}", new Vector2(3f, y - 7f), 8, new Color(1f, 1f, 1f, 0.46f));
            }
        }

        void DrawOwnerBackground(Painter2D painter, object owner)
        {
            OwnerBounds(owner, out float left, out float right);
            bool selected = m_Selection.Any(value => ReferenceEquals(value.Owner, owner));
            Color color = m_Binding.Color;
            painter.fillColor = new Color(color.r, color.g, color.b, selected ? 0.1f : 0.035f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, 0f));
            painter.LineTo(new Vector2(right, 0f));
            painter.LineTo(new Vector2(right, TimelineTrackLayout.CurveLaneHeight));
            painter.LineTo(new Vector2(left, TimelineTrackLayout.CurveLaneHeight));
            painter.ClosePath();
            painter.Fill();
        }

        void DrawCurve(Painter2D painter, object owner, AnimationCurve curve)
        {
            OwnerBounds(owner, out float left, out float right);
            int samples = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1f, right - left) / SamplePixelStep), 2, MaximumSamples);
            m_Samples.Clear();
            for (int sample = 0; sample <= samples; sample++)
            {
                float time = sample / (float)samples;
                m_Samples.Add(new Vector2(Mathf.Lerp(left, right, time), ValueToY(curve.Evaluate(time))));
            }
            painter.strokeColor = m_Binding.Color;
            painter.lineWidth = 2f;
            painter.BeginPath();
            for (int i = 0; i < m_Samples.Count; i++)
            {
                if (i == 0) painter.MoveTo(m_Samples[i]);
                else painter.LineTo(m_Samples[i]);
            }
            painter.Stroke();
        }

        void DrawKeys(Painter2D painter, object owner, AnimationCurve curve)
        {
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Vector2 position = KeyPosition(owner, keys[i]);
                bool selected = m_Selection.Contains(new AnimationCurveKeyAddress(owner, i));
                painter.fillColor = selected ? Color.white : m_Binding.Color;
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

        void DrawCursorSample(Painter2D painter, object owner, AnimationCurve curve)
        {
            if (!m_Binding.TryGetCurrentNormalizedTime(owner, out float normalized))
                return;
            Vector2 point = new Vector2(
                m_Binding.NormalizedTimeToPosition(owner, normalized),
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
            AnimationCurveKeyAddress address = m_Selection[0];
            AnimationCurve curve = CurveForDraw(address.Owner);
            if (address.KeyIndex < 0 || address.KeyIndex >= curve.length)
                return;
            Vector2 key = KeyPosition(address.Owner, curve.keys[address.KeyIndex]);
            DrawTangent(painter, key, TangentPosition(address.Owner, curve, address.KeyIndex, false), false);
            DrawTangent(painter, key, TangentPosition(address.Owner, curve, address.KeyIndex, true), true);
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
            if (m_Binding.RuntimeReadOnly)
                return;
            Focus();
            m_LastContextX = evt.localPosition.x;
            if (evt.button == 1)
            {
                ShowContextMenu(evt.localPosition);
                evt.StopImmediatePropagation();
                return;
            }
            if (evt.button != 0)
                return;
            if (TryFindTangent(evt.localPosition, out AnimationCurveKeyAddress tangentKey, out bool outgoing))
            {
                m_TangentKey = tangentKey;
                BeginCurveDraft(new[] { tangentKey.Owner });
                BeginGesture(evt, outgoing ? Gesture.OutTangent : Gesture.InTangent);
                return;
            }
            if (TryFindKey(evt.localPosition, out AnimationCurveKeyAddress address))
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
            if (evt.clickCount >= 2 && TryFindOwner(evt.localPosition.x, out object owner))
            {
                AddKey(owner, evt.localPosition);
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
            if (m_Gesture == Gesture.Keys)
                UpdateKeyDrag();
            else if (m_Gesture == Gesture.Box)
            {
                m_Box = Rect.MinMaxRect(
                    Mathf.Min(m_StartPointer.x, m_CurrentPointer.x),
                    Mathf.Min(m_StartPointer.y, m_CurrentPointer.y),
                    Mathf.Max(m_StartPointer.x, m_CurrentPointer.x),
                    Mathf.Max(m_StartPointer.y, m_CurrentPointer.y));
                UpdateBoxSelection(evt.shiftKey);
            }
            else
                UpdateTangent(m_Gesture == Gesture.OutTangent);
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
            if (!evt.altKey || m_Binding.ValueDomain.IsBounded)
                return;
            TimelineCurveVerticalView view = VerticalView();
            float pivot = YToValue(evt.localMousePosition.y);
            view = evt.shiftKey
                ? view.Pan(evt.delta.y * 0.02f)
                : view.Zoom(Mathf.Pow(1.1f, evt.delta.y), pivot);
            m_Binding.SetVerticalView(view);
            MarkDirtyRepaint();
            evt.StopImmediatePropagation();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete)
                DeleteSelectedKeys();
            else if (evt.actionKey && evt.keyCode == KeyCode.C)
                CopySelectedKeys();
            else if (evt.actionKey && evt.keyCode == KeyCode.V)
                PasteKeysAt(m_LastContextX);
            else if (evt.keyCode == KeyCode.F)
                FrameSelected();
            else
                return;
            evt.StopImmediatePropagation();
        }

        void ShowContextMenu(Vector2 position)
        {
            var menu = new GenericMenu();
            bool hasOwner = TryFindOwner(position.x, out object owner);
            AddMenuItem(menu, "Add Key", hasOwner, () => AddKey(owner, position));
            AddMenuItem(menu, "Delete Selected", m_Selection.Count > 0, DeleteSelectedKeys);
            AddMenuItem(menu, "Copy Selected", m_Selection.Count > 0, CopySelectedKeys);
            AddMenuItem(menu, "Paste", hasOwner && ClipboardCompatible(), () => PasteKeysAt(position.x));
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
            if (commit && m_Changed && gesture != Gesture.Box)
                CommitDraft(gesture == Gesture.Keys ? "Move Curve Keys" : "Edit Curve Tangent");
            else
                ClearDraft();
            m_Box = default;
            MarkDirtyRepaint();
        }

        void BeginCurveDraft(IEnumerable<object> owners)
        {
            ClearDraft();
            foreach (object owner in owners.Distinct())
            {
                AnimationCurve curve = m_Binding.Read(owner);
                m_OriginalCurves.Add(owner, curve);
                m_DraftCurves.Add(owner, TimelineCurveAuthoring.CopyCurve(curve));
                m_SourceRevisions.Add(owner, TimelineCurveAuthoring.Revision(curve));
            }
        }

        void UpdateKeyDrag()
        {
            if (m_Selection.Count == 0)
                return;
            int rawDeltaFrame = m_Binding.FieldView.Geometry.PositionToClosestFrame(m_CurrentPointer.x) -
                                m_Binding.FieldView.Geometry.PositionToClosestFrame(m_StartPointer.x);
            int minimumDelta = int.MinValue;
            int maximumDelta = int.MaxValue;
            float rawValueDelta = YToValue(m_CurrentPointer.y) - YToValue(m_StartPointer.y);
            float minimumValueDelta = float.NegativeInfinity;
            float maximumValueDelta = float.PositiveInfinity;
            Dictionary<object, HashSet<int>> selected = m_Selection.GroupBy(value => value.Owner)
                .ToDictionary(group => group.Key, group => new HashSet<int>(group.Select(value => value.KeyIndex)));
            foreach (KeyValuePair<object, HashSet<int>> pair in selected)
            {
                Keyframe[] keys = m_OriginalCurves[pair.Key].keys;
                foreach (int index in pair.Value)
                {
                    int frame = m_Binding.NormalizedTimeToFrame(pair.Key, keys[index].time);
                    minimumDelta = Mathf.Max(minimumDelta, m_Binding.StartFrame(pair.Key) - frame);
                    maximumDelta = Mathf.Min(maximumDelta, m_Binding.EndFrame(pair.Key) - frame);
                    if (index > 0 && !pair.Value.Contains(index - 1))
                        minimumDelta = Mathf.Max(minimumDelta,
                            m_Binding.NormalizedTimeToFrame(pair.Key, keys[index - 1].time) + 1 - frame);
                    if (index + 1 < keys.Length && !pair.Value.Contains(index + 1))
                        maximumDelta = Mathf.Min(maximumDelta,
                            m_Binding.NormalizedTimeToFrame(pair.Key, keys[index + 1].time) - 1 - frame);
                    if (m_Binding.ValueDomain.IsBounded)
                    {
                        minimumValueDelta = Mathf.Max(minimumValueDelta, m_Binding.ValueDomain.Minimum - keys[index].value);
                        maximumValueDelta = Mathf.Min(maximumValueDelta, m_Binding.ValueDomain.Maximum - keys[index].value);
                    }
                }
            }
            int deltaFrame = Mathf.Clamp(rawDeltaFrame, minimumDelta, maximumDelta);
            float deltaValue = Mathf.Clamp(rawValueDelta, minimumValueDelta, maximumValueDelta);
            foreach (KeyValuePair<object, HashSet<int>> pair in selected)
            {
                AnimationCurve draft = TimelineCurveAuthoring.CopyCurve(m_OriginalCurves[pair.Key]);
                Keyframe[] keys = draft.keys;
                foreach (int index in pair.Value)
                {
                    Keyframe key = keys[index];
                    int frame = m_Binding.NormalizedTimeToFrame(pair.Key, key.time) + deltaFrame;
                    key.time = m_Binding.FrameToNormalizedTime(pair.Key, frame);
                    key.value += deltaValue;
                    keys[index] = key;
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
            for (int ownerIndex = 0; ownerIndex < m_Binding.Owners.Count; ownerIndex++)
            {
                object owner = m_Binding.Owners[ownerIndex];
                if (!m_Binding.Supports(owner))
                    continue;
                Keyframe[] keys = m_Binding.Read(owner).keys;
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    var address = new AnimationCurveKeyAddress(owner, keyIndex);
                    if (m_Box.Contains(KeyPosition(owner, keys[keyIndex])) && !m_Selection.Contains(address))
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
            float time = m_Binding.PositionToNormalizedTime(m_TangentKey.Owner, m_CurrentPointer.x);
            float deltaTime = time - key.time;
            if (outgoing && deltaTime <= 0.0001f || !outgoing && deltaTime >= -0.0001f)
                return;
            float tangent = (YToValue(m_CurrentPointer.y) - key.value) / deltaTime;
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

        void CommitDraft(string undoName)
        {
            foreach (KeyValuePair<object, AnimationCurve> pair in m_DraftCurves)
            {
                if (TimelineCurveAuthoring.Revision(m_Binding.Read(pair.Key)) != m_SourceRevisions[pair.Key])
                {
                    m_Selection.Clear();
                    ClearDraft();
                    Debug.LogError($"Animation curve '{m_Binding.Identity}' changed during the active gesture; stale key indices were discarded.");
                    return;
                }
                m_Binding.Validate(pair.Key, pair.Value);
            }
            Dictionary<object, AnimationCurve> final = m_DraftCurves.ToDictionary(
                pair => pair.Key,
                pair => TimelineCurveAuthoring.CopyCurve(pair.Value));
            object focus = m_Selection.Count > 0 ? m_Selection[0].Owner : final.Keys.First();
            m_Binding.Commit(
                final,
                undoName,
                new AnimationCurveSelection(
                    m_Binding,
                    focus,
                    m_Selection.Where(value => ReferenceEquals(value.Owner, focus)).Select(value => value.KeyIndex)));
            ClearDraft();
        }

        void AddKey(object owner, Vector2 position)
        {
            AnimationCurve curve = m_Binding.Read(owner);
            int index = curve.AddKey(new Keyframe(
                m_Binding.PositionToNormalizedTime(owner, position.x),
                YToValue(position.y)));
            if (index < 0)
                return;
            m_Binding.Validate(owner, curve);
            m_Binding.Commit(
                new Dictionary<object, AnimationCurve> { [owner] = curve },
                $"Add {m_Binding.DisplayName} Key",
                new AnimationCurveSelection(m_Binding, owner, new[] { index }));
        }

        void DeleteSelectedKeys()
        {
            if (m_Selection.Count == 0)
                return;
            var curves = new Dictionary<object, AnimationCurve>();
            foreach (IGrouping<object, AnimationCurveKeyAddress> group in m_Selection.GroupBy(value => value.Owner))
            {
                AnimationCurve curve = m_Binding.Read(group.Key);
                int[] indices = group.Select(value => value.KeyIndex).Distinct().OrderByDescending(value => value).ToArray();
                if (curve.length - indices.Length <= 0)
                    continue;
                for (int i = 0; i < indices.Length; i++)
                    curve.RemoveKey(indices[i]);
                m_Binding.Validate(group.Key, curve);
                curves.Add(group.Key, curve);
            }
            if (curves.Count > 0)
                m_Binding.Commit(curves, $"Delete {m_Binding.DisplayName} Keys");
            m_Selection.Clear();
        }

        void CopySelectedKeys()
        {
            if (m_Selection.Count == 0)
                return;
            s_Clipboard = new AnimationCurveClipboard
            {
                TimeDomain = m_Binding.TimeDomain,
                ValueDomain = m_Binding.ValueDomain
            };
            AnimationCurveKeyAddress[] ordered = m_Selection
                .OrderBy(value => m_Binding.StartFrame(value.Owner))
                .ThenBy(value => m_Binding.Read(value.Owner).keys[value.KeyIndex].time)
                .ToArray();
            for (int i = 0; i < ordered.Length; i++)
                s_Clipboard.Keys.Add(m_Binding.Read(ordered[i].Owner).keys[ordered[i].KeyIndex]);
        }

        void PasteKeysAt(float x)
        {
            if (!ClipboardCompatible() || !TryFindOwner(x, out object owner))
                return;
            AnimationCurve curve = m_Binding.Read(owner);
            float anchor = m_Binding.PositionToNormalizedTime(owner, x);
            float source = s_Clipboard.Keys.Min(value => value.time);
            var indices = new List<int>();
            for (int i = 0; i < s_Clipboard.Keys.Count; i++)
            {
                Keyframe key = s_Clipboard.Keys[i];
                key.time = Mathf.Clamp01(anchor + key.time - source);
                int index = curve.AddKey(key);
                if (index < 0)
                    throw new InvalidOperationException("Pasted curve keys collide with an existing key time.");
                indices.Add(index);
            }
            m_Binding.Validate(owner, curve);
            m_Binding.Commit(
                new Dictionary<object, AnimationCurve> { [owner] = curve },
                $"Paste {m_Binding.DisplayName} Keys",
                new AnimationCurveSelection(m_Binding, owner, indices));
        }

        bool ClipboardCompatible()
        {
            if (s_Clipboard == null || s_Clipboard.Keys.Count == 0 || s_Clipboard.TimeDomain != m_Binding.TimeDomain)
                return false;
            TimelineCurveValueDomain source = s_Clipboard.ValueDomain;
            TimelineCurveValueDomain target = m_Binding.ValueDomain;
            return source.IsBounded == target.IsBounded &&
                   string.Equals(source.Unit, target.Unit, StringComparison.Ordinal) &&
                   (!source.IsBounded || Mathf.Approximately(source.Minimum, target.Minimum) &&
                   Mathf.Approximately(source.Maximum, target.Maximum));
        }

        void SetSelectedTangentMode(AnimationUtility.TangentMode mode)
        {
            if (m_Selection.Count == 0)
                return;
            var curves = new Dictionary<object, AnimationCurve>();
            foreach (IGrouping<object, AnimationCurveKeyAddress> group in m_Selection.GroupBy(value => value.Owner))
            {
                AnimationCurve curve = m_Binding.Read(group.Key);
                foreach (int index in group.Select(value => value.KeyIndex).Distinct())
                {
                    AnimationUtility.SetKeyBroken(curve, index, mode == AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetKeyLeftTangentMode(curve, index, mode);
                    AnimationUtility.SetKeyRightTangentMode(curve, index, mode);
                }
                m_Binding.Validate(group.Key, curve);
                curves.Add(group.Key, curve);
            }
            object owner = m_Selection[0].Owner;
            m_Binding.Commit(
                curves,
                $"Set {m_Binding.DisplayName} Tangent",
                new AnimationCurveSelection(
                    m_Binding,
                    owner,
                    m_Selection.Where(value => ReferenceEquals(value.Owner, owner)).Select(value => value.KeyIndex)));
        }

        void SetSelectedWeightedMode(WeightedMode mode)
        {
            if (m_Selection.Count == 0)
                return;
            var curves = new Dictionary<object, AnimationCurve>();
            foreach (IGrouping<object, AnimationCurveKeyAddress> group in m_Selection.GroupBy(value => value.Owner))
            {
                AnimationCurve curve = m_Binding.Read(group.Key);
                foreach (int index in group.Select(value => value.KeyIndex).Distinct())
                {
                    Keyframe key = curve.keys[index];
                    key.weightedMode = mode;
                    curve.MoveKey(index, key);
                }
                m_Binding.Validate(group.Key, curve);
                curves.Add(group.Key, curve);
            }
            object owner = m_Selection[0].Owner;
            m_Binding.Commit(
                curves,
                $"Set {m_Binding.DisplayName} Weighted Mode",
                new AnimationCurveSelection(
                    m_Binding,
                    owner,
                    m_Selection.Where(value => ReferenceEquals(value.Owner, owner)).Select(value => value.KeyIndex)));
        }

        public void FrameSelected()
        {
            if (m_Binding.ValueDomain.IsBounded)
                return;
            IEnumerable<float> values = m_Selection.Count > 0
                ? m_Selection.Select(address => m_Binding.Read(address.Owner).keys[address.KeyIndex].value)
                : m_Binding.Owners.Where(m_Binding.Supports)
                    .SelectMany(owner => m_Binding.Read(owner).keys)
                    .Select(key => key.value);
            float[] array = values.ToArray();
            float minimum = array.Length == 0 ? -1f : array.Min();
            float maximum = array.Length == 0 ? 1f : array.Max();
            float padding = Mathf.Max(0.1f, (maximum - minimum) * 0.12f);
            if (Mathf.Approximately(minimum, maximum))
                padding = Mathf.Max(0.5f, Mathf.Abs(minimum) * 0.2f);
            m_Binding.SetVerticalView(new TimelineCurveVerticalView(minimum - padding, maximum + padding));
            m_HasAutoFit = true;
            MarkDirtyRepaint();
        }

        void AutoFitIfNeeded()
        {
            if (!m_HasAutoFit && !m_Binding.ValueDomain.IsBounded)
                FrameSelected();
        }

        void PresentSelection(object owner) =>
            m_Binding.FieldView.PresentCurveSelection(new AnimationCurveSelection(
                m_Binding,
                owner,
                m_Selection.Where(value => ReferenceEquals(value.Owner, owner)).Select(value => value.KeyIndex)));

        void ClearStaleSelection()
        {
            for (int i = m_Selection.Count - 1; i >= 0; i--)
            {
                AnimationCurveKeyAddress address = m_Selection[i];
                if (!m_Binding.Supports(address.Owner) || address.KeyIndex < 0 ||
                    address.KeyIndex >= m_Binding.Read(address.Owner).length)
                    m_Selection.RemoveAt(i);
            }
        }

        bool TryFindOwner(float x, out object selected)
        {
            int frame = m_Binding.FieldView.Geometry.PositionToFloorFrame(x);
            selected = null;
            for (int i = 0; i < m_Binding.Owners.Count; i++)
            {
                object owner = m_Binding.Owners[i];
                if (!m_Binding.Supports(owner) || frame < m_Binding.StartFrame(owner) || frame > m_Binding.EndFrame(owner))
                    continue;
                if (selected == null || m_Binding.StartFrame(owner) >= m_Binding.StartFrame(selected))
                    selected = owner;
            }
            return selected != null;
        }

        bool TryFindKey(Vector2 position, out AnimationCurveKeyAddress result)
        {
            result = default;
            float nearest = KeyHitRadius * KeyHitRadius;
            bool found = false;
            for (int ownerIndex = 0; ownerIndex < m_Binding.Owners.Count; ownerIndex++)
            {
                object owner = m_Binding.Owners[ownerIndex];
                if (!m_Binding.Supports(owner))
                    continue;
                Keyframe[] keys = CurveForDraw(owner).keys;
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    float distance = (KeyPosition(owner, keys[keyIndex]) - position).sqrMagnitude;
                    if (distance > nearest)
                        continue;
                    nearest = distance;
                    result = new AnimationCurveKeyAddress(owner, keyIndex);
                    found = true;
                }
            }
            return found;
        }

        bool TryFindTangent(Vector2 position, out AnimationCurveKeyAddress address, out bool outgoing)
        {
            address = default;
            outgoing = false;
            if (m_Selection.Count != 1)
                return false;
            address = m_Selection[0];
            AnimationCurve curve = CurveForDraw(address.Owner);
            if (address.KeyIndex < 0 || address.KeyIndex >= curve.length)
                return false;
            float incoming = (TangentPosition(address.Owner, curve, address.KeyIndex, false) - position).sqrMagnitude;
            float outgoingDistance = (TangentPosition(address.Owner, curve, address.KeyIndex, true) - position).sqrMagnitude;
            if (Mathf.Min(incoming, outgoingDistance) > TangentHitRadius * TangentHitRadius)
                return false;
            outgoing = outgoingDistance < incoming;
            return true;
        }

        Vector2 TangentPosition(object owner, AnimationCurve curve, int keyIndex, bool outgoing)
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
            return new Vector2(m_Binding.NormalizedTimeToPosition(owner, targetTime), ValueToY(targetValue));
        }

        AnimationCurve CurveForDraw(object owner) =>
            m_DraftCurves.TryGetValue(owner, out AnimationCurve curve) ? curve : m_Binding.Read(owner);

        Vector2 KeyPosition(object owner, Keyframe key) =>
            new Vector2(m_Binding.NormalizedTimeToPosition(owner, key.time), ValueToY(key.value));

        void OwnerBounds(object owner, out float left, out float right)
        {
            left = m_Binding.FieldView.Geometry.FrameToPosition(m_Binding.StartFrame(owner));
            right = m_Binding.FieldView.Geometry.FrameToPosition(m_Binding.EndFrame(owner));
        }

        TimelineCurveVerticalView VerticalView() => m_Binding.GetVerticalView();

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
            return m_Binding.ValueDomain.IsBounded
                ? Mathf.Clamp(value, m_Binding.ValueDomain.Minimum, m_Binding.ValueDomain.Maximum)
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

    internal sealed class TimelineCurveChannelLaneView : AnimationCurveChannelLaneView
    {
        public TimelineCurveChannelLaneView(
            TimelineTrackView trackView,
            TimelineCurveChannelDescriptor descriptor,
            int visibleChannelIndex)
            : base(
                new TimelineCurveLaneBinding(trackView, descriptor),
                TimelineTrackLayout.CurveLaneTop(trackView.Track, visibleChannelIndex))
        {
        }
    }

    internal sealed class AnimationCurveInspectorView : VisualElement
    {
        readonly AnimationCurveSelection m_Selection;

        public AnimationCurveInspectorView(AnimationCurveSelection selection)
        {
            m_Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            Build();
        }

        void Build()
        {
            IAnimationCurveLaneBinding binding = m_Selection.Binding;
            Add(new Label(binding.DisplayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            Add(new Label($"{binding.Identity} · {binding.OwnerDisplayName(m_Selection.Owner)}"));
            Add(new Label($"Revision {m_Selection.Revision:X16} · {binding.ValueDomain.Summary}"));
            AnimationCurve curve = binding.Read(m_Selection.Owner);
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
            var frame = new IntegerField("Timeline Frame")
            {
                value = binding.NormalizedTimeToFrame(m_Selection.Owner, key.time),
                isDelayed = true
            };
            var time = new FloatField("Normalized Time") { value = key.time, isDelayed = true };
            var value = new FloatField($"Value {binding.ValueDomain.Unit}") { value = key.value, isDelayed = true };
            var inTangent = new FloatField("In Tangent") { value = key.inTangent, isDelayed = true };
            var outTangent = new FloatField("Out Tangent") { value = key.outTangent, isDelayed = true };
            var inWeight = new FloatField("In Weight") { value = key.inWeight, isDelayed = true };
            var outWeight = new FloatField("Out Weight") { value = key.outWeight, isDelayed = true };
            var weighted = new EnumField("Weighted Mode", key.weightedMode);
            frame.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.time = binding.FrameToNormalizedTime(
                m_Selection.Owner,
                Mathf.Clamp(evt.newValue, binding.StartFrame(m_Selection.Owner), binding.EndFrame(m_Selection.Owner))), "Edit Curve Key Frame"));
            time.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.time = Mathf.Clamp01(evt.newValue), "Edit Curve Key Time"));
            value.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.value = evt.newValue, "Edit Curve Key Value"));
            inTangent.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.inTangent = evt.newValue, "Edit Curve In Tangent"));
            outTangent.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.outTangent = evt.newValue, "Edit Curve Out Tangent"));
            inWeight.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.inWeight = Mathf.Clamp01(evt.newValue), "Edit Curve In Weight"));
            outWeight.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.outWeight = Mathf.Clamp01(evt.newValue), "Edit Curve Out Weight"));
            weighted.RegisterValueChangedCallback(evt => ReplaceKey(keyIndex, item => item.weightedMode = (WeightedMode)evt.newValue, "Edit Curve Weighted Mode"));
            Add(frame);
            Add(time);
            Add(value);
            Add(inTangent);
            Add(outTangent);
            Add(inWeight);
            Add(outWeight);
            Add(weighted);
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
                AnimationCurve curve = m_Selection.Binding.Read(m_Selection.Owner);
                AnimationUtility.SetKeyBroken(curve, keyIndex, mode == AnimationUtility.TangentMode.Free);
                AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, mode);
                AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, mode);
                Commit(curve, $"Set Curve Tangent {text}", keyIndex);
            }) { text = text });
        }

        void ReplaceCurve(Action<AnimationCurve> mutation, string undoName)
        {
            AnimationCurve curve = m_Selection.Binding.Read(m_Selection.Owner);
            mutation(curve);
            Commit(curve, undoName, m_Selection.KeyIndices.Count > 0 ? m_Selection.KeyIndices[0] : -1);
        }

        void ReplaceKey(int keyIndex, Action<Keyframe> mutation, string undoName)
        {
            AnimationCurve curve = m_Selection.Binding.Read(m_Selection.Owner);
            Keyframe key = curve.keys[keyIndex];
            mutation(key);
            Commit(curve, undoName, curve.MoveKey(keyIndex, key));
        }

        void Commit(AnimationCurve curve, string undoName, int keyIndex)
        {
            m_Selection.Binding.Validate(m_Selection.Owner, curve);
            m_Selection.Binding.Commit(
                new Dictionary<object, AnimationCurve> { [m_Selection.Owner] = curve },
                undoName,
                new AnimationCurveSelection(
                    m_Selection.Binding,
                    m_Selection.Owner,
                    keyIndex >= 0 ? new[] { keyIndex } : Array.Empty<int>()));
        }
    }
}
