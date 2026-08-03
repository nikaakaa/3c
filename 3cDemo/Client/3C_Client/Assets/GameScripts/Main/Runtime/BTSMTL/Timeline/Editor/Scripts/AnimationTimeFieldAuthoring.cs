using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    public readonly struct AnimationTimeMarker
    {
        public AnimationTimeMarker(string authoringId, string markerId, int frame)
        {
            AuthoringId = authoringId ?? string.Empty;
            MarkerId = markerId ?? string.Empty;
            Frame = frame;
        }

        public string AuthoringId { get; }
        public string MarkerId { get; }
        public int Frame { get; }
    }

    public readonly struct AnimationTimeAnalysisCandidate
    {
        public AnimationTimeAnalysisCandidate(string candidateId, string displayName, int frame, float confidence, Color color)
        {
            CandidateId = candidateId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Frame = frame;
            Confidence = confidence;
            Color = color;
        }

        public string CandidateId { get; }
        public string DisplayName { get; }
        public int Frame { get; }
        public float Confidence { get; }
        public Color Color { get; }
    }

    public interface IAnimationTimeFieldGeometry
    {
        int DurationFrames { get; }
        float FrameToPosition(int frame);
        int PositionToClosestFrame(float position);
        float NormalizedTimeToPosition(float normalizedTime);
        float PositionToNormalizedTime(float position);
    }

    public interface IAnimationTimeFieldContextAdapter
    {
        string AuthoringIdentity { get; }
        int DurationFrames { get; }
        float FrameRate { get; }
        bool IsCyclic { get; }
        void Seek(int frame);
    }

    public interface IAnimationTimeMarkerAuthoringAdapter
    {
        bool CanEditMarkers { get; }
        IReadOnlyList<AnimationTimeMarker> Markers { get; }
        void ReplaceMarkers(AnimationTimeMarker[] markers, string undoName);
    }

    public interface IAnimationTimeCurveAuthoringAdapter
    {
        string CurveLabel { get; }
        int CurveStartFrame { get; }
        int CurveDurationFrames { get; }
        bool CanEditCurve { get; }
        AnimationCurve ReadCurve();
        void ReplaceCurve(AnimationCurve curve, string undoName);
    }

    public interface IAnimationTimeAnalysisAuthoringAdapter
    {
        IReadOnlyList<AnimationTimeAnalysisCandidate> AnalysisCandidates { get; }
        string AnalysisStatus { get; }
        bool CanRefreshAnalysis { get; }
        bool CanApplyAnalysisCandidates { get; }
        void RefreshAnalysis();
        void ApplyAnalysisCandidates(string undoName);
    }

    public interface IAnimationTimeFieldAuthoringAdapter :
        IAnimationTimeFieldContextAdapter,
        IAnimationTimeMarkerAuthoringAdapter,
        IAnimationTimeCurveAuthoringAdapter,
        IAnimationTimeAnalysisAuthoringAdapter
    {
    }

    public sealed class AnimationTimeFieldGeometry : IAnimationTimeFieldGeometry
    {
        float m_Left;
        float m_Width = 1f;
        float m_ViewStart;
        float m_PixelsPerFrame = 8f;
        int m_DurationFrames = 1;

        public int DurationFrames => m_DurationFrames;
        public float PixelsPerFrame => m_PixelsPerFrame;
        public float ViewStart => m_ViewStart;

        public void SetViewport(Rect rect, int durationFrames)
        {
            m_Left = rect.x;
            m_Width = Mathf.Max(1f, rect.width);
            m_DurationFrames = Mathf.Max(1, durationFrames);
            ClampView();
        }

        public void FrameAll()
        {
            m_ViewStart = 0f;
            m_PixelsPerFrame = Mathf.Clamp(m_Width / Mathf.Max(1, m_DurationFrames), 0.2f, 80f);
            ClampView();
        }

        public void Zoom(float mousePosition, float factor)
        {
            float frameAtMouse = PositionToFrame(mousePosition);
            m_PixelsPerFrame = Mathf.Clamp(m_PixelsPerFrame * factor, 0.2f, 80f);
            m_ViewStart = frameAtMouse - (mousePosition - m_Left) / m_PixelsPerFrame;
            ClampView();
        }

        public void Pan(float pixelDelta)
        {
            m_ViewStart -= pixelDelta / m_PixelsPerFrame;
            ClampView();
        }

        public float FrameToPosition(int frame) => m_Left + (frame - m_ViewStart) * m_PixelsPerFrame;
        public int PositionToClosestFrame(float position) => Mathf.Clamp(Mathf.RoundToInt(PositionToFrame(position)), 0, Mathf.Max(0, m_DurationFrames - 1));
        public float NormalizedTimeToPosition(float normalizedTime) => m_Left + (Mathf.Clamp01(normalizedTime) * m_DurationFrames - m_ViewStart) * m_PixelsPerFrame;
        public float PositionToNormalizedTime(float position) => Mathf.Clamp01(PositionToFrame(position) / m_DurationFrames);

        float PositionToFrame(float position) => m_ViewStart + (position - m_Left) / m_PixelsPerFrame;

        void ClampView()
        {
            float visibleFrames = m_Width / m_PixelsPerFrame;
            m_ViewStart = Mathf.Clamp(m_ViewStart, 0f, Mathf.Max(0f, m_DurationFrames - visibleFrames));
        }
    }

    public sealed class AnimationTimeField
    {
        enum Gesture : byte
        {
            None,
            Pan,
            Marker,
            Keys,
            Box,
            InTangent,
            OutTangent
        }

        const float RulerHeight = 30f;
        const float MarkerHeight = 48f;
        const float CurveHeight = 190f;
        const float AnalysisHeight = 34f;
        const float KeyRadius = 5f;
        const float HitRadius = 9f;
        static Keyframe[] s_CurveClipboard = Array.Empty<Keyframe>();

        readonly AnimationTimeFieldGeometry m_Geometry = new AnimationTimeFieldGeometry();
        readonly HashSet<string> m_SelectedMarkers = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<int> m_SelectedKeys = new HashSet<int>();
        Gesture m_Gesture;
        Vector2 m_GestureStart;
        Vector2 m_GestureCurrent;
        string m_DragMarkerId = string.Empty;
        AnimationTimeMarker[] m_MarkerDraft = Array.Empty<AnimationTimeMarker>();
        AnimationCurve m_CurveDraft;
        AnimationCurve m_CurveOriginal;
        int m_PlayheadFrame;
        bool m_Framed;

        public float RequiredHeight => RulerHeight + MarkerHeight + CurveHeight + AnalysisHeight;
        public int PlayheadFrame => m_PlayheadFrame;

        public void ResetView()
        {
            m_Framed = false;
            m_Gesture = Gesture.None;
            m_SelectedMarkers.Clear();
            m_SelectedKeys.Clear();
            m_CurveDraft = null;
            m_CurveOriginal = null;
            m_MarkerDraft = Array.Empty<AnimationTimeMarker>();
        }

        public void Draw(Rect rect, IAnimationTimeFieldAuthoringAdapter adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));
            Rect ruler = new Rect(rect.x, rect.y, rect.width, RulerHeight);
            Rect marker = new Rect(rect.x, ruler.yMax, rect.width, MarkerHeight);
            Rect curve = new Rect(rect.x, marker.yMax, rect.width, CurveHeight);
            Rect analysis = new Rect(rect.x, curve.yMax, rect.width, AnalysisHeight);
            m_Geometry.SetViewport(rect, adapter.DurationFrames);
            if (!m_Framed)
            {
                m_Geometry.FrameAll();
                m_Framed = true;
            }

            DrawBackground(rect, marker, curve, analysis);
            DrawRuler(ruler, adapter);
            DrawMarkers(marker, adapter);
            DrawCurve(curve, adapter);
            DrawAnalysis(analysis, adapter);
            DrawPlayhead(rect);
            HandleCommands(adapter, marker, curve);
            HandlePointer(adapter, ruler, marker, curve, rect);
        }

        public void DrawSelectionInspector(IAnimationTimeFieldAuthoringAdapter adapter)
        {
            if (adapter == null)
                return;
            AnimationTimeMarker[] markers = CurrentMarkers(adapter);
            if (m_SelectedMarkers.Count == 1)
            {
                int index = Array.FindIndex(markers, value => m_SelectedMarkers.Contains(value.AuthoringId));
                if (index >= 0)
                {
                    AnimationTimeMarker marker = markers[index];
                    EditorGUILayout.LabelField("Marker Selection", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    string markerId = EditorGUILayout.DelayedTextField("Marker Id", marker.MarkerId);
                    int frame = EditorGUILayout.DelayedIntField("Frame", marker.Frame);
                    if (EditorGUI.EndChangeCheck())
                    {
                        frame = ClampMarkerFrame(adapter, frame);
                        markers[index] = new AnimationTimeMarker(marker.AuthoringId, markerId.Trim(), frame);
                        CommitMarkers(adapter, markers, "Edit Animation Marker");
                    }
                }
            }

            AnimationCurve curve = CurrentCurve(adapter);
            int[] selected = m_SelectedKeys.Where(value => value >= 0 && value < curve.length).OrderBy(value => value).ToArray();
            if (!adapter.CanEditCurve || selected.Length == 0)
                return;
            EditorGUILayout.LabelField($"Curve Selection ({selected.Length})", EditorStyles.boldLabel);
            if (selected.Length == 1)
            {
                int index = selected[0];
                Keyframe key = curve[index];
                EditorGUI.BeginChangeCheck();
                float time = EditorGUILayout.DelayedFloatField("Normalized Time", key.time);
                float value = EditorGUILayout.DelayedFloatField("Value", key.value);
                float inTangent = EditorGUILayout.DelayedFloatField("In Tangent", key.inTangent);
                float outTangent = EditorGUILayout.DelayedFloatField("Out Tangent", key.outTangent);
                float inWeight = EditorGUILayout.DelayedFloatField("In Weight", key.inWeight);
                float outWeight = EditorGUILayout.DelayedFloatField("Out Weight", key.outWeight);
                WeightedMode weighted = (WeightedMode)EditorGUILayout.EnumPopup("Weighted", key.weightedMode);
                if (EditorGUI.EndChangeCheck())
                {
                    key.time = ClampKeyTime(curve, index, time);
                    key.value = Mathf.Clamp01(value);
                    key.inTangent = inTangent;
                    key.outTangent = outTangent;
                    key.inWeight = Mathf.Clamp01(inWeight);
                    key.outWeight = Mathf.Clamp01(outWeight);
                    key.weightedMode = weighted;
                    curve.MoveKey(index, key);
                    CommitCurve(adapter, curve, "Edit Animation Curve Key");
                }
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto")) SetTangentMode(adapter, AnimationUtility.TangentMode.Auto);
            if (GUILayout.Button("Clamped")) SetTangentMode(adapter, AnimationUtility.TangentMode.ClampedAuto);
            if (GUILayout.Button("Linear")) SetTangentMode(adapter, AnimationUtility.TangentMode.Linear);
            if (GUILayout.Button("Constant")) SetTangentMode(adapter, AnimationUtility.TangentMode.Constant);
            if (GUILayout.Button("Free")) SetTangentMode(adapter, AnimationUtility.TangentMode.Free);
            EditorGUILayout.EndHorizontal();
        }

        void DrawBackground(Rect rect, Rect marker, Rect curve, Rect analysis)
        {
            EditorGUI.DrawRect(rect, new Color(0.105f, 0.105f, 0.105f));
            EditorGUI.DrawRect(marker, new Color(0.13f, 0.15f, 0.16f));
            EditorGUI.DrawRect(curve, new Color(0.075f, 0.08f, 0.085f));
            EditorGUI.DrawRect(analysis, new Color(0.105f, 0.12f, 0.11f));
        }

        void DrawRuler(Rect rect, IAnimationTimeFieldAuthoringAdapter adapter)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            int step = TickStep();
            GUIStyle label = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperLeft };
            for (int frame = 0; frame <= adapter.DurationFrames; frame += step)
            {
                float x = m_Geometry.FrameToPosition(frame);
                if (x < rect.x - 1f || x > rect.xMax + 1f)
                    continue;
                Handles.color = new Color(1f, 1f, 1f, 0.28f);
                Handles.DrawLine(new Vector3(x, rect.yMax - 8f), new Vector3(x, rect.yMax));
                GUI.Label(new Rect(x + 3f, rect.y + 2f, 80f, 18f), $"{frame}F  {frame / adapter.FrameRate:0.##}s", label);
            }
        }

        void DrawMarkers(Rect rect, IAnimationTimeFieldAuthoringAdapter adapter)
        {
            GUI.Label(new Rect(rect.x + 5f, rect.y + 2f, 120f, 18f), "SYNC MARKERS", EditorStyles.miniBoldLabel);
            AnimationTimeMarker[] markers = CurrentMarkers(adapter);
            for (int i = 0; i < markers.Length; i++)
            {
                AnimationTimeMarker marker = markers[i];
                float x = m_Geometry.FrameToPosition(marker.Frame);
                if (x < rect.x - 30f || x > rect.xMax + 30f)
                    continue;
                bool selected = m_SelectedMarkers.Contains(marker.AuthoringId);
                Color color = selected ? new Color(0.3f, 1f, 1f) : new Color(0.15f, 0.8f, 0.9f);
                Handles.color = color;
                Handles.DrawAAPolyLine(2f, new Vector3(x, rect.y + 19f), new Vector3(x, rect.yMax - 3f));
                EditorGUI.DrawRect(new Rect(x - 4f, rect.y + 17f, 8f, 7f), color);
                GUI.Label(new Rect(x + 6f, rect.y + 16f, 140f, 18f), $"{marker.MarkerId} · {marker.Frame}F", EditorStyles.miniLabel);
            }
            if (adapter.IsCyclic && markers.Length > 1)
                GUI.Label(new Rect(rect.xMax - 130f, rect.y + 2f, 125f, 18f), "Cyclic closure", EditorStyles.miniLabel);
        }

        void DrawCurve(Rect rect, IAnimationTimeFieldAuthoringAdapter adapter)
        {
            GUI.Label(new Rect(rect.x + 5f, rect.y + 2f, 280f, 18f), adapter.CurveLabel, EditorStyles.miniBoldLabel);
            if (!adapter.CanEditCurve)
                GUI.Label(new Rect(rect.xMax - 75f, rect.y + 2f, 70f, 18f), "READ ONLY", EditorStyles.miniLabel);
            for (int i = 0; i <= 4; i++)
            {
                float value = i / 4f;
                float y = CurveY(rect, value);
                Handles.color = new Color(1f, 1f, 1f, i == 0 || i == 4 ? 0.18f : 0.08f);
                Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
                GUI.Label(new Rect(rect.x + 3f, y - 14f, 35f, 16f), value.ToString("0.##"), EditorStyles.miniLabel);
            }
            AnimationCurve curve = CurrentCurve(adapter);
            int samples = Mathf.Clamp(Mathf.CeilToInt(rect.width / 4f), 2, 512);
            var points = new Vector3[samples + 1];
            for (int i = 0; i <= samples; i++)
            {
                float time = i / (float)samples;
                points[i] = new Vector3(CurveTimeToPosition(adapter, time), CurveY(rect, curve.Evaluate(time)));
            }
            Handles.color = new Color(0.3f, 0.92f, 0.52f);
            Handles.DrawAAPolyLine(2f, points);
            for (int i = 0; i < curve.length; i++)
            {
                Vector2 point = KeyPoint(rect, adapter, curve[i]);
                Color color = m_SelectedKeys.Contains(i) ? new Color(1f, 0.75f, 0.2f) : new Color(0.4f, 1f, 0.62f);
                EditorGUI.DrawRect(new Rect(point.x - KeyRadius, point.y - KeyRadius, KeyRadius * 2f, KeyRadius * 2f), color);
            }
            DrawTangents(rect, adapter, curve);
            if (m_Gesture == Gesture.Box)
            {
                Rect box = Rect.MinMaxRect(Mathf.Min(m_GestureStart.x, m_GestureCurrent.x), Mathf.Min(m_GestureStart.y, m_GestureCurrent.y), Mathf.Max(m_GestureStart.x, m_GestureCurrent.x), Mathf.Max(m_GestureStart.y, m_GestureCurrent.y));
                EditorGUI.DrawRect(box, new Color(0.25f, 0.65f, 1f, 0.14f));
                Handles.color = new Color(0.35f, 0.75f, 1f, 0.9f);
                Handles.DrawAAPolyLine(1f, new Vector3(box.xMin, box.yMin), new Vector3(box.xMax, box.yMin), new Vector3(box.xMax, box.yMax), new Vector3(box.xMin, box.yMax), new Vector3(box.xMin, box.yMin));
            }
        }

        void DrawTangents(Rect rect, IAnimationTimeCurveAuthoringAdapter adapter, AnimationCurve curve)
        {
            if (m_SelectedKeys.Count != 1)
                return;
            int index = m_SelectedKeys.First();
            if (index < 0 || index >= curve.length)
                return;
            Keyframe key = curve[index];
            Vector2 origin = KeyPoint(rect, adapter, key);
            Vector2 incoming = TangentPoint(rect, adapter, key, false);
            Vector2 outgoing = TangentPoint(rect, adapter, key, true);
            Handles.color = new Color(1f, 0.72f, 0.3f, 0.8f);
            Handles.DrawAAPolyLine(1.5f, new Vector3(incoming.x, incoming.y), new Vector3(origin.x, origin.y), new Vector3(outgoing.x, outgoing.y));
            EditorGUI.DrawRect(new Rect(incoming.x - 3f, incoming.y - 3f, 6f, 6f), Handles.color);
            EditorGUI.DrawRect(new Rect(outgoing.x - 3f, outgoing.y - 3f, 6f, 6f), Handles.color);
        }

        void DrawAnalysis(Rect rect, IAnimationTimeFieldAuthoringAdapter adapter)
        {
            GUI.Label(new Rect(rect.x + 5f, rect.y + 2f, 115f, 18f), "ANALYSIS", EditorStyles.miniBoldLabel);
            float right = rect.xMax - 5f;
            if (adapter.CanApplyAnalysisCandidates)
            {
                if (GUI.Button(new Rect(right - 52f, rect.y + 3f, 52f, 20f), "Apply", EditorStyles.miniButton))
                    adapter.ApplyAnalysisCandidates("Apply Animation Analysis Candidates");
                right -= 57f;
            }
            if (adapter.CanRefreshAnalysis)
            {
                if (GUI.Button(new Rect(right - 58f, rect.y + 3f, 58f, 20f), "Refresh", EditorStyles.miniButton))
                    adapter.RefreshAnalysis();
                right -= 63f;
            }
            IReadOnlyList<AnimationTimeAnalysisCandidate> candidates = adapter.AnalysisCandidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                AnimationTimeAnalysisCandidate candidate = candidates[i];
                float x = m_Geometry.FrameToPosition(candidate.Frame);
                if (x < rect.x || x > rect.xMax)
                    continue;
                Color color = candidate.Color;
                color.a = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(candidate.Confidence));
                EditorGUI.DrawRect(new Rect(x - 4f, rect.y + 19f, 8f, 8f), color);
            }
            if (candidates.Count == 0)
                GUI.Label(new Rect(rect.x + 120f, rect.y + 2f, Mathf.Max(0f, right - rect.x - 120f), 18f), adapter.AnalysisStatus, EditorStyles.miniLabel);
        }

        void DrawPlayhead(Rect rect)
        {
            float x = m_Geometry.FrameToPosition(m_PlayheadFrame);
            Handles.color = new Color(1f, 0.35f, 0.25f, 0.95f);
            Handles.DrawAAPolyLine(1.5f, new Vector3(x, rect.y), new Vector3(x, rect.yMax));
        }

        void HandleCommands(IAnimationTimeFieldAuthoringAdapter adapter, Rect markerRect, Rect curveRect)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ValidateCommand && (evt.commandName == "Copy" || evt.commandName == "Paste"))
            {
                evt.Use();
                return;
            }
            if (adapter.CanEditCurve && evt.type == EventType.ExecuteCommand && evt.commandName == "Copy" && m_SelectedKeys.Count > 0)
            {
                AnimationCurve curve = CurrentCurve(adapter);
                int[] indices = m_SelectedKeys.Where(value => value >= 0 && value < curve.length).OrderBy(value => value).ToArray();
                if (indices.Length > 0)
                {
                    float origin = curve[indices[0]].time;
                    s_CurveClipboard = indices.Select(value => { Keyframe key = curve[value]; key.time -= origin; return key; }).ToArray();
                }
                evt.Use();
                return;
            }
            if (adapter.CanEditCurve && evt.type == EventType.ExecuteCommand && evt.commandName == "Paste" && s_CurveClipboard.Length > 0)
            {
                PasteKeys(adapter);
                evt.Use();
                return;
            }
            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace))
            {
                if (adapter.CanEditMarkers && m_SelectedMarkers.Count > 0)
                {
                    AnimationTimeMarker[] remaining = CurrentMarkers(adapter).Where(value => !m_SelectedMarkers.Contains(value.AuthoringId)).ToArray();
                    m_SelectedMarkers.Clear();
                    CommitMarkers(adapter, remaining, "Delete Animation Marker");
                    evt.Use();
                }
                else if (adapter.CanEditCurve && m_SelectedKeys.Count > 0)
                {
                    DeleteKeys(adapter);
                    evt.Use();
                }
            }
        }

        void HandlePointer(IAnimationTimeFieldAuthoringAdapter adapter, Rect ruler, Rect markerRect, Rect curveRect, Rect all)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ScrollWheel && all.Contains(evt.mousePosition))
            {
                m_Geometry.Zoom(evt.mousePosition.x, Mathf.Pow(1.12f, -evt.delta.y));
                evt.Use();
                return;
            }
            if (evt.type == EventType.MouseDown && (evt.button == 2 || evt.button == 0 && evt.alt) && all.Contains(evt.mousePosition))
            {
                m_Gesture = Gesture.Pan;
                m_GestureStart = evt.mousePosition;
                evt.Use();
                return;
            }
            if (evt.type == EventType.MouseDrag && m_Gesture == Gesture.Pan)
            {
                m_Geometry.Pan(evt.mousePosition.x - m_GestureStart.x);
                m_GestureStart = evt.mousePosition;
                evt.Use();
                return;
            }
            if (evt.type == EventType.MouseUp && m_Gesture == Gesture.Pan)
            {
                m_Gesture = Gesture.None;
                evt.Use();
                return;
            }
            if (evt.type == EventType.MouseDown && evt.button == 0 && ruler.Contains(evt.mousePosition))
            {
                Seek(adapter, evt.mousePosition.x);
                evt.Use();
                return;
            }
            if (adapter.CanEditMarkers && HandleMarkerPointer(adapter, markerRect, evt) ||
                adapter.CanEditCurve && HandleCurvePointer(adapter, curveRect, evt))
                evt.Use();
        }

        bool HandleMarkerPointer(IAnimationTimeFieldAuthoringAdapter adapter, Rect rect, Event evt)
        {
            if (!rect.Contains(evt.mousePosition) && m_Gesture != Gesture.Marker)
                return false;
            AnimationTimeMarker[] markers = CurrentMarkers(adapter);
            int hit = MarkerAt(markers, evt.mousePosition.x);
            if (evt.type == EventType.MouseDown && evt.button == 1)
            {
                if (hit >= 0)
                {
                    string id = markers[hit].AuthoringId;
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Delete Marker"), false, () =>
                    {
                        CommitMarkers(adapter, markers.Where(value => !string.Equals(value.AuthoringId, id, StringComparison.Ordinal)).ToArray(), "Delete Animation Marker");
                    });
                    menu.ShowAsContext();
                }
                return true;
            }
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (hit < 0)
                {
                    string id = Guid.NewGuid().ToString("N");
                    var added = markers.Concat(new[] { new AnimationTimeMarker(id, "Marker", ClampMarkerFrame(adapter, m_Geometry.PositionToClosestFrame(evt.mousePosition.x))) }).OrderBy(value => value.Frame).ToArray();
                    m_SelectedMarkers.Clear();
                    m_SelectedMarkers.Add(id);
                    CommitMarkers(adapter, added, "Add Animation Marker");
                    return true;
                }
                if (!evt.control && !evt.command && !evt.shift)
                    m_SelectedMarkers.Clear();
                string markerId = markers[hit].AuthoringId;
                if (!m_SelectedMarkers.Add(markerId) && (evt.control || evt.command))
                    m_SelectedMarkers.Remove(markerId);
                m_DragMarkerId = markerId;
                m_MarkerDraft = markers;
                m_Gesture = Gesture.Marker;
                return true;
            }
            if (evt.type == EventType.MouseDrag && m_Gesture == Gesture.Marker)
            {
                int index = Array.FindIndex(m_MarkerDraft, value => string.Equals(value.AuthoringId, m_DragMarkerId, StringComparison.Ordinal));
                if (index >= 0)
                {
                    AnimationTimeMarker source = m_MarkerDraft[index];
                    int frame = ClampMarkerFrame(adapter, m_Geometry.PositionToClosestFrame(evt.mousePosition.x));
                    m_MarkerDraft[index] = new AnimationTimeMarker(source.AuthoringId, source.MarkerId, frame);
                }
                return true;
            }
            if (evt.type == EventType.MouseUp && m_Gesture == Gesture.Marker)
            {
                m_Gesture = Gesture.None;
                CommitMarkers(adapter, m_MarkerDraft, "Move Animation Marker");
                m_MarkerDraft = Array.Empty<AnimationTimeMarker>();
                return true;
            }
            return false;
        }

        bool HandleCurvePointer(IAnimationTimeFieldAuthoringAdapter adapter, Rect rect, Event evt)
        {
            if (!rect.Contains(evt.mousePosition) && m_Gesture != Gesture.Keys && m_Gesture != Gesture.Box && m_Gesture != Gesture.InTangent && m_Gesture != Gesture.OutTangent)
                return false;
            AnimationCurve curve = CurrentCurve(adapter);
            if (evt.type == EventType.MouseDown && evt.button == 1)
            {
                int hit = KeyAt(rect, adapter, curve, evt.mousePosition);
                if (hit >= 0 && !m_SelectedKeys.Contains(hit))
                {
                    m_SelectedKeys.Clear();
                    m_SelectedKeys.Add(hit);
                }
                ShowCurveMenu(adapter);
                return true;
            }
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (TryHitTangent(rect, adapter, curve, evt.mousePosition, out Gesture tangent))
                {
                    m_CurveDraft = CopyCurve(curve);
                    m_CurveOriginal = CopyCurve(curve);
                    m_Gesture = tangent;
                    return true;
                }
                int hit = KeyAt(rect, adapter, curve, evt.mousePosition);
                if (hit < 0 && evt.clickCount > 1)
                {
                    Keyframe key = new Keyframe(PositionToCurveTime(adapter, evt.mousePosition.x), CurveValue(rect, evt.mousePosition.y));
                    int index = curve.AddKey(key);
                    m_SelectedKeys.Clear();
                    m_SelectedKeys.Add(index);
                    CommitCurve(adapter, curve, "Add Animation Curve Key");
                    return true;
                }
                if (hit >= 0)
                {
                    if (!evt.control && !evt.command && !evt.shift && !m_SelectedKeys.Contains(hit))
                        m_SelectedKeys.Clear();
                    if (!m_SelectedKeys.Add(hit) && (evt.control || evt.command))
                        m_SelectedKeys.Remove(hit);
                    m_CurveDraft = CopyCurve(curve);
                    m_CurveOriginal = CopyCurve(curve);
                    m_GestureStart = evt.mousePosition;
                    m_GestureCurrent = evt.mousePosition;
                    m_Gesture = Gesture.Keys;
                    return true;
                }
                if (!evt.shift)
                    m_SelectedKeys.Clear();
                m_GestureStart = evt.mousePosition;
                m_GestureCurrent = evt.mousePosition;
                m_Gesture = Gesture.Box;
                return true;
            }
            if (evt.type == EventType.MouseDrag && m_Gesture == Gesture.Keys)
            {
                DragKeys(adapter, rect, evt.mousePosition);
                return true;
            }
            if (evt.type == EventType.MouseDrag && m_Gesture == Gesture.Box)
            {
                m_GestureCurrent = evt.mousePosition;
                return true;
            }
            if (evt.type == EventType.MouseDrag && (m_Gesture == Gesture.InTangent || m_Gesture == Gesture.OutTangent))
            {
                DragTangent(adapter, rect, evt.mousePosition, m_Gesture == Gesture.OutTangent);
                return true;
            }
            if (evt.type == EventType.MouseUp && m_Gesture == Gesture.Keys)
            {
                m_Gesture = Gesture.None;
                CommitCurve(adapter, m_CurveDraft, "Move Animation Curve Keys");
                m_CurveDraft = null;
                m_CurveOriginal = null;
                return true;
            }
            if (evt.type == EventType.MouseUp && m_Gesture == Gesture.Box)
            {
                SelectBox(adapter, rect, curve, evt.shift);
                m_Gesture = Gesture.None;
                return true;
            }
            if (evt.type == EventType.MouseUp && (m_Gesture == Gesture.InTangent || m_Gesture == Gesture.OutTangent))
            {
                m_Gesture = Gesture.None;
                CommitCurve(adapter, m_CurveDraft, "Edit Animation Curve Tangent");
                m_CurveDraft = null;
                m_CurveOriginal = null;
                return true;
            }
            return false;
        }

        void DragKeys(IAnimationTimeCurveAuthoringAdapter adapter, Rect rect, Vector2 mouse)
        {
            float deltaTime = PositionToCurveTime(adapter, mouse.x) - PositionToCurveTime(adapter, m_GestureStart.x);
            float deltaValue = CurveValue(rect, mouse.y) - CurveValue(rect, m_GestureStart.y);
            AnimationCurve source = CopyCurve(m_CurveOriginal ?? m_CurveDraft);
            Keyframe[] keys = source.keys;
            float minDelta = -1f;
            float maxDelta = 1f;
            foreach (int index in m_SelectedKeys)
            {
                if (index < 0 || index >= keys.Length)
                    continue;
                minDelta = Mathf.Max(minDelta, -keys[index].time);
                maxDelta = Mathf.Min(maxDelta, 1f - keys[index].time);
            }
            deltaTime = Mathf.Clamp(deltaTime, minDelta, maxDelta);
            for (int i = 0; i < keys.Length; i++)
            {
                if (!m_SelectedKeys.Contains(i))
                    continue;
                keys[i].time += deltaTime;
                keys[i].value = Mathf.Clamp01(keys[i].value + deltaValue);
            }
            Array.Sort(keys, (left, right) => left.time.CompareTo(right.time));
            m_CurveDraft = new AnimationCurve(keys) { preWrapMode = source.preWrapMode, postWrapMode = source.postWrapMode };
            m_GestureCurrent = mouse;
        }

        void DragTangent(IAnimationTimeCurveAuthoringAdapter adapter, Rect rect, Vector2 mouse, bool outgoing)
        {
            if (m_SelectedKeys.Count != 1 || m_CurveDraft == null)
                return;
            int index = m_SelectedKeys.First();
            Keyframe key = m_CurveDraft[index];
            float time = PositionToCurveTime(adapter, mouse.x);
            float value = CurveValue(rect, mouse.y);
            float deltaTime = time - key.time;
            if (outgoing && deltaTime < 0.001f || !outgoing && deltaTime > -0.001f)
                return;
            float tangent = (value - key.value) / deltaTime;
            float weight = Mathf.Clamp01(Mathf.Abs(deltaTime) * 3f);
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
            m_CurveDraft.MoveKey(index, key);
        }

        void SelectBox(IAnimationTimeCurveAuthoringAdapter adapter, Rect rect, AnimationCurve curve, bool additive)
        {
            if (!additive)
                m_SelectedKeys.Clear();
            Rect box = Rect.MinMaxRect(Mathf.Min(m_GestureStart.x, m_GestureCurrent.x), Mathf.Min(m_GestureStart.y, m_GestureCurrent.y), Mathf.Max(m_GestureStart.x, m_GestureCurrent.x), Mathf.Max(m_GestureStart.y, m_GestureCurrent.y));
            for (int i = 0; i < curve.length; i++)
                if (box.Contains(KeyPoint(rect, adapter, curve[i]), true))
                    m_SelectedKeys.Add(i);
        }

        void ShowCurveMenu(IAnimationTimeFieldAuthoringAdapter adapter)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Key At Playhead"), false, () => AddKeyAtPlayhead(adapter));
            menu.AddItem(new GUIContent("Delete Selected"), false, () => DeleteKeys(adapter));
            menu.AddSeparator(string.Empty);
            AddTangentMenu(menu, adapter, "Tangent/Auto", AnimationUtility.TangentMode.Auto);
            AddTangentMenu(menu, adapter, "Tangent/Clamped Auto", AnimationUtility.TangentMode.ClampedAuto);
            AddTangentMenu(menu, adapter, "Tangent/Linear", AnimationUtility.TangentMode.Linear);
            AddTangentMenu(menu, adapter, "Tangent/Constant", AnimationUtility.TangentMode.Constant);
            AddTangentMenu(menu, adapter, "Tangent/Free", AnimationUtility.TangentMode.Free);
            menu.AddItem(new GUIContent("Weighted/None"), false, () => SetWeightedMode(adapter, WeightedMode.None));
            menu.AddItem(new GUIContent("Weighted/In"), false, () => SetWeightedMode(adapter, WeightedMode.In));
            menu.AddItem(new GUIContent("Weighted/Out"), false, () => SetWeightedMode(adapter, WeightedMode.Out));
            menu.AddItem(new GUIContent("Weighted/Both"), false, () => SetWeightedMode(adapter, WeightedMode.Both));
            menu.ShowAsContext();
        }

        void AddTangentMenu(GenericMenu menu, IAnimationTimeFieldAuthoringAdapter adapter, string path, AnimationUtility.TangentMode mode) =>
            menu.AddItem(new GUIContent(path), false, () => SetTangentMode(adapter, mode));

        void AddKeyAtPlayhead(IAnimationTimeFieldAuthoringAdapter adapter)
        {
            AnimationCurve curve = CurrentCurve(adapter);
            float time = m_PlayheadFrame / (float)Mathf.Max(1, adapter.DurationFrames);
            int index = curve.AddKey(new Keyframe(time, Mathf.Clamp01(curve.Evaluate(time))));
            m_SelectedKeys.Clear();
            m_SelectedKeys.Add(index);
            CommitCurve(adapter, curve, "Add Animation Curve Key");
        }

        void DeleteKeys(IAnimationTimeFieldAuthoringAdapter adapter)
        {
            AnimationCurve curve = CurrentCurve(adapter);
            int[] indices = m_SelectedKeys.Where(value => value >= 0 && value < curve.length).OrderByDescending(value => value).ToArray();
            if (curve.length - indices.Length < 1)
                return;
            for (int i = 0; i < indices.Length; i++)
                curve.RemoveKey(indices[i]);
            m_SelectedKeys.Clear();
            CommitCurve(adapter, curve, "Delete Animation Curve Keys");
        }

        void PasteKeys(IAnimationTimeFieldAuthoringAdapter adapter)
        {
            AnimationCurve curve = CurrentCurve(adapter);
            float origin = m_PlayheadFrame / (float)Mathf.Max(1, adapter.DurationFrames);
            m_SelectedKeys.Clear();
            for (int i = 0; i < s_CurveClipboard.Length; i++)
            {
                Keyframe key = s_CurveClipboard[i];
                key.time = Mathf.Clamp01(origin + key.time);
                int index = curve.AddKey(key);
                if (index >= 0)
                    m_SelectedKeys.Add(index);
            }
            CommitCurve(adapter, curve, "Paste Animation Curve Keys");
        }

        void SetTangentMode(IAnimationTimeFieldAuthoringAdapter adapter, AnimationUtility.TangentMode mode)
        {
            AnimationCurve curve = CurrentCurve(adapter);
            foreach (int index in m_SelectedKeys)
            {
                if (index < 0 || index >= curve.length)
                    continue;
                AnimationUtility.SetKeyBroken(curve, index, mode == AnimationUtility.TangentMode.Free);
                AnimationUtility.SetKeyLeftTangentMode(curve, index, mode);
                AnimationUtility.SetKeyRightTangentMode(curve, index, mode);
            }
            CommitCurve(adapter, curve, "Set Animation Curve Tangent");
        }

        void SetWeightedMode(IAnimationTimeFieldAuthoringAdapter adapter, WeightedMode mode)
        {
            AnimationCurve curve = CurrentCurve(adapter);
            foreach (int index in m_SelectedKeys)
            {
                if (index < 0 || index >= curve.length)
                    continue;
                Keyframe key = curve[index];
                key.weightedMode = mode;
                curve.MoveKey(index, key);
            }
            CommitCurve(adapter, curve, "Set Animation Curve Weighting");
        }

        void CommitMarkers(IAnimationTimeFieldAuthoringAdapter adapter, AnimationTimeMarker[] markers, string undoName)
        {
            AnimationTimeMarker[] ordered = markers
                .Select(value => new AnimationTimeMarker(
                    value.AuthoringId,
                    AnimationMarkerSyncAuthoring.NormalizeId(value.MarkerId),
                    value.Frame))
                .OrderBy(value => value.Frame)
                .ThenBy(value => value.MarkerId, StringComparer.Ordinal)
                .ToArray();
            if (ordered.Any(value => string.IsNullOrEmpty(value.MarkerId)))
                throw new InvalidOperationException("Animation Marker Id is required.");
            adapter.ReplaceMarkers(ordered, undoName);
            m_MarkerDraft = Array.Empty<AnimationTimeMarker>();
        }

        void CommitCurve(IAnimationTimeFieldAuthoringAdapter adapter, AnimationCurve curve, string undoName)
        {
            adapter.ReplaceCurve(CopyCurve(curve), undoName);
            m_CurveDraft = null;
            m_CurveOriginal = null;
        }

        void Seek(IAnimationTimeFieldAuthoringAdapter adapter, float x)
        {
            m_PlayheadFrame = m_Geometry.PositionToClosestFrame(x);
            adapter.Seek(m_PlayheadFrame);
        }

        AnimationTimeMarker[] CurrentMarkers(IAnimationTimeFieldAuthoringAdapter adapter) =>
            m_Gesture == Gesture.Marker && m_MarkerDraft.Length > 0 ? m_MarkerDraft : adapter.Markers.ToArray();

        AnimationCurve CurrentCurve(IAnimationTimeFieldAuthoringAdapter adapter) =>
            m_CurveDraft ?? CopyCurve(adapter.ReadCurve());

        int MarkerAt(IReadOnlyList<AnimationTimeMarker> markers, float x)
        {
            for (int i = markers.Count - 1; i >= 0; i--)
                if (Mathf.Abs(m_Geometry.FrameToPosition(markers[i].Frame) - x) <= HitRadius)
                    return i;
            return -1;
        }

        int KeyAt(Rect rect, IAnimationTimeCurveAuthoringAdapter adapter, AnimationCurve curve, Vector2 point)
        {
            for (int i = curve.length - 1; i >= 0; i--)
                if (Vector2.Distance(KeyPoint(rect, adapter, curve[i]), point) <= HitRadius)
                    return i;
            return -1;
        }

        bool TryHitTangent(Rect rect, IAnimationTimeCurveAuthoringAdapter adapter, AnimationCurve curve, Vector2 point, out Gesture gesture)
        {
            gesture = Gesture.None;
            if (m_SelectedKeys.Count != 1)
                return false;
            int index = m_SelectedKeys.First();
            if (index < 0 || index >= curve.length)
                return false;
            Keyframe key = curve[index];
            if (Vector2.Distance(TangentPoint(rect, adapter, key, false), point) <= HitRadius)
                gesture = Gesture.InTangent;
            else if (Vector2.Distance(TangentPoint(rect, adapter, key, true), point) <= HitRadius)
                gesture = Gesture.OutTangent;
            return gesture != Gesture.None;
        }

        Vector2 KeyPoint(Rect rect, IAnimationTimeCurveAuthoringAdapter adapter, Keyframe key) =>
            new Vector2(CurveTimeToPosition(adapter, key.time), CurveY(rect, key.value));

        Vector2 TangentPoint(Rect rect, IAnimationTimeCurveAuthoringAdapter adapter, Keyframe key, bool outgoing)
        {
            float timeDelta = Mathf.Max(0.035f, outgoing ? key.outWeight / 3f : key.inWeight / 3f);
            if (!outgoing)
                timeDelta = -timeDelta;
            float tangent = outgoing ? key.outTangent : key.inTangent;
            if (!float.IsFinite(tangent))
                tangent = 0f;
            return new Vector2(CurveTimeToPosition(adapter, Mathf.Clamp01(key.time + timeDelta)), CurveY(rect, Mathf.Clamp01(key.value + tangent * timeDelta)));
        }

        static float CurveY(Rect rect, float value) => Mathf.Lerp(rect.yMax - 12f, rect.y + 25f, Mathf.Clamp01(value));
        static float CurveValue(Rect rect, float y) => Mathf.InverseLerp(rect.yMax - 12f, rect.y + 25f, y);
        static int ClampMarkerFrame(IAnimationTimeFieldAuthoringAdapter adapter, int frame) => Mathf.Clamp(frame, 0, Mathf.Max(0, adapter.DurationFrames - 1));

        static float ClampKeyTime(AnimationCurve curve, int index, float time)
        {
            float minimum = index > 0 ? curve[index - 1].time + 0.0001f : 0f;
            float maximum = index + 1 < curve.length ? curve[index + 1].time - 0.0001f : 1f;
            return Mathf.Clamp(time, minimum, maximum);
        }

        static AnimationCurve CopyCurve(AnimationCurve source) => TimelineCurveAuthoring.CopyCurve(source);

        float CurveTimeToPosition(IAnimationTimeCurveAuthoringAdapter adapter, float normalizedTime)
        {
            int duration = Mathf.Max(1, adapter.CurveDurationFrames);
            int frame = adapter.CurveStartFrame + Mathf.RoundToInt(Mathf.Clamp01(normalizedTime) * duration);
            return m_Geometry.FrameToPosition(frame);
        }

        float PositionToCurveTime(IAnimationTimeCurveAuthoringAdapter adapter, float position)
        {
            int duration = Mathf.Max(1, adapter.CurveDurationFrames);
            float frame = (position - m_Geometry.FrameToPosition(0)) / m_Geometry.PixelsPerFrame;
            return Mathf.Clamp01((frame - adapter.CurveStartFrame) / duration);
        }

        int TickStep()
        {
            float target = 90f / m_Geometry.PixelsPerFrame;
            int[] steps = { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600 };
            for (int i = 0; i < steps.Length; i++)
                if (steps[i] >= target)
                    return steps[i];
            return 1200;
        }
    }
}

namespace BTSMTL.Timeline.Editor
{
    internal sealed class TimelineAnimationTimeAuthoringInspector : UnityEngine.UIElements.VisualElement
    {
        readonly TimelineFieldView m_FieldView;
        readonly AnimationTrack m_Track;
        readonly List<AnimationClip> m_Clips = new List<AnimationClip>();
        readonly List<TimelineCurveChannelDescriptor> m_Channels = new List<TimelineCurveChannelDescriptor>();
        readonly AnimationTimeField m_TimeField = new AnimationTimeField();
        readonly Adapter m_Adapter;
        int m_ClipIndex;
        int m_ChannelIndex;

        internal TimelineAnimationTimeAuthoringInspector(
            TimelineFieldView fieldView,
            AnimationTrack track,
            AnimationClip initialClip = null)
        {
            m_FieldView = fieldView ?? throw new ArgumentNullException(nameof(fieldView));
            m_Track = track ?? throw new ArgumentNullException(nameof(track));
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is AnimationClip clip)
                    m_Clips.Add(clip);
            }
            m_ClipIndex = initialClip == null ? 0 : Mathf.Max(0, m_Clips.IndexOf(initialClip));
            RefreshChannels();
            m_Adapter = new Adapter(this);
            var gui = new UnityEngine.UIElements.IMGUIContainer(Draw);
            gui.style.flexGrow = 1f;
            Add(gui);
        }

        AnimationClip SelectedClip => m_Clips.Count == 0
            ? null
            : m_Clips[Mathf.Clamp(m_ClipIndex, 0, m_Clips.Count - 1)];

        TimelineCurveChannelDescriptor SelectedChannel => m_Channels.Count == 0
            ? null
            : m_Channels[Mathf.Clamp(m_ChannelIndex, 0, m_Channels.Count - 1)];

        void Draw()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Source Time Authoring", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Timeline AnimationTrack and Pose Source use the same typed time, marker and curve module. Timeline markers remain Track-owned; curves remain Clip-owned.",
                MessageType.Info);
            if (m_Clips.Count == 0)
            {
                EditorGUILayout.HelpBox("Add an Animation Clip to edit typed curves. Track markers remain available when MarkerGroup is configured.", MessageType.Warning);
            }
            else
            {
                string[] clipLabels = m_Clips.Select(value => $"{value.Name} · {value.StartFrame}-{value.EndFrame}F").ToArray();
                int nextClip = EditorGUILayout.Popup("Curve Owner", m_ClipIndex, clipLabels);
                if (nextClip != m_ClipIndex)
                {
                    m_ClipIndex = nextClip;
                    m_ChannelIndex = 0;
                    RefreshChannels();
                    m_TimeField.ResetView();
                }
                string[] channelLabels = m_Channels.Select(value => value.DisplayName).ToArray();
                if (channelLabels.Length > 0)
                    m_ChannelIndex = EditorGUILayout.Popup("Typed Curve", Mathf.Clamp(m_ChannelIndex, 0, channelLabels.Length - 1), channelLabels);
            }
            using (new EditorGUI.DisabledScope(m_FieldView.RuntimeReadOnly))
            {
                Rect field = GUILayoutUtility.GetRect(260f, m_TimeField.RequiredHeight, GUILayout.ExpandWidth(true));
                m_TimeField.Draw(field, m_Adapter);
                EditorGUILayout.Space(5f);
                m_TimeField.DrawSelectionInspector(m_Adapter);
            }
        }

        void RefreshChannels()
        {
            m_Channels.Clear();
            AnimationClip clip = SelectedClip;
            if (clip == null)
                return;
            IReadOnlyList<TimelineCurveChannelDescriptor> all = TimelineCurveChannelCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Supports(clip))
                    m_Channels.Add(all[i]);
            }
            m_ChannelIndex = Mathf.Clamp(m_ChannelIndex, 0, Mathf.Max(0, m_Channels.Count - 1));
        }

        sealed class Adapter : IAnimationTimeFieldAuthoringAdapter
        {
            readonly TimelineAnimationTimeAuthoringInspector m_View;

            internal Adapter(TimelineAnimationTimeAuthoringInspector view) => m_View = view;

            AnimationTrack Track => m_View.m_Track;
            AnimationClip Clip => m_View.SelectedClip;
            TimelineCurveChannelDescriptor Channel => m_View.SelectedChannel;

            public string AuthoringIdentity => Track.AuthoringId;
            public int DurationFrames => Mathf.Max(1, Track.Timeline.MaxFrame + 1);
            public float FrameRate => TimelineUtility.FrameRate;
            public bool IsCyclic => Track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic;
            public bool CanEditMarkers => !m_View.m_FieldView.RuntimeReadOnly && Track.SyncMode == AnimationSyncMode.MarkerGroup;
            public string CurveLabel => Channel == null ? "NO TYPED CURVE" : Channel.DisplayName.ToUpperInvariant();
            public int CurveStartFrame => Clip?.StartFrame ?? 0;
            public int CurveDurationFrames => Clip == null ? DurationFrames : Mathf.Max(1, Clip.Duration);
            public bool CanEditCurve => !m_View.m_FieldView.RuntimeReadOnly && Clip != null && Channel != null;
            public IReadOnlyList<AnimationTimeMarker> Markers => Track.SyncMarkers
                .Where(value => value != null)
                .Select(value => new AnimationTimeMarker(value.AuthoringId, value.MarkerId, value.Frame))
                .ToArray();
            public IReadOnlyList<AnimationTimeAnalysisCandidate> AnalysisCandidates => Array.Empty<AnimationTimeAnalysisCandidate>();
            public string AnalysisStatus => "Timeline analysis is selected from the Animation Clip analysis page.";
            public bool CanRefreshAnalysis => false;
            public bool CanApplyAnalysisCandidates => false;

            public AnimationCurve ReadCurve() => Channel == null || Clip == null
                ? AnimationCurve.Constant(0f, 1f, 0f)
                : Channel.Read(Clip);

            public void ReplaceMarkers(AnimationTimeMarker[] markers, string undoName)
            {
                if (!CanEditMarkers)
                    throw new InvalidOperationException("Timeline marker authoring is unavailable.");
                AnimationTimeMarker[] ordered = markers
                    .OrderBy(value => value.Frame)
                    .ThenBy(value => value.MarkerId, StringComparer.Ordinal)
                    .ToArray();
                m_View.m_FieldView.CommitAuthoringMutation(
                    () =>
                    {
                        string[] existing = Track.SyncMarkers
                            .Where(value => value != null)
                            .Select(value => value.AuthoringId)
                            .ToArray();
                        for (int i = 0; i < existing.Length; i++)
                            Track.DeleteMarker(existing[i]);
                        for (int i = 0; i < ordered.Length; i++)
                            Track.EnsureMarker(ordered[i].AuthoringId, ordered[i].MarkerId, ordered[i].Frame);
                    },
                    undoName,
                    Track);
            }

            public void ReplaceCurve(AnimationCurve curve, string undoName)
            {
                if (!CanEditCurve)
                    throw new InvalidOperationException("Timeline curve authoring is unavailable.");
                Channel.Validate(Clip, curve);
                m_View.m_FieldView.CommitAuthoringMutation(
                    () => Channel.Replace(Clip, curve),
                    undoName,
                    Clip);
            }

            public void Seek(int frame) => m_View.m_FieldView.SetTimeLocator(frame);
            public void RefreshAnalysis() { }
            public void ApplyAnalysisCandidates(string undoName) { }
        }
    }
}
