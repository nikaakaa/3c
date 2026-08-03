using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPoseSourceEditorWindow : EditorWindow
    {
        enum Page : byte
        {
            Source,
            Time,
            Analysis,
            Preview
        }

        [SerializeField] CharacterAnimationPresentationProfile m_Profile;
        [SerializeField] CharacterPresentationPoseSourceSlot m_Slot;
        [SerializeField] Page m_Page = Page.Time;
        [SerializeField] Vector2 m_Scroll;
        CharacterSequencePoseSourceBinding m_Binding;
        AnimationClip m_Clip;
        bool m_Loop;
        float m_PlayRate;
        string m_MarkerGroup = string.Empty;
        AnimationMarkerSequenceTopology m_Topology;
        AnimationMarkerSyncRole m_SyncRole;
        AnimationTimeMarker[] m_Markers = Array.Empty<AnimationTimeMarker>();
        AnimationCurve m_Curve = AnimationCurve.Constant(0f, 1f, 1f);
        AnimationTimeAnalysisCandidate[] m_Candidates = Array.Empty<AnimationTimeAnalysisCandidate>();
        AnimationFootContactCandidateSet m_CandidateSet;
        readonly AnimationTimeField m_TimeField = new AnimationTimeField();
        SourceAdapter m_Adapter;
        string m_AnalysisStatus = "Analysis not inspected.";
        string m_Error = string.Empty;
        int m_PreviewFrame;

        public static void Open(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceBinding binding)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!binding || !binding.Slot)
                throw new ArgumentNullException(nameof(binding));
            switch (binding)
            {
                case CharacterSequencePoseSourceBinding:
                {
                    CharacterPoseSourceEditorWindow window = GetWindow<CharacterPoseSourceEditorWindow>();
                    window.titleContent = new GUIContent("Pose Source Editor");
                    window.m_Profile = profile;
                    window.m_Slot = binding.Slot;
                    window.Reload();
                    window.Show();
                    window.Focus();
                    break;
                }
                case CharacterBlendSpacePoseSourceBinding blendSpace:
                    CharacterAnimationBlendSpaceEditorWindow.Open(blendSpace.BlendSpace, FindDefinition(profile));
                    break;
                case CharacterMotionMatchingPoseSourceBinding motionMatching:
                    OpenMotionMatching(motionMatching);
                    break;
                default:
                    throw new InvalidOperationException($"Pose Source kind '{binding.SourceKind}' has no editor route.");
            }
        }

        [OnOpenAsset]
        static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not CharacterPresentationPoseSourceBinding binding)
                return false;
            CharacterAnimationPresentationProfile profile = FindOwner(binding);
            if (!profile)
                return false;
            Open(profile, binding);
            return true;
        }

        void OnEnable()
        {
            if (m_Profile && m_Slot)
                Reload();
        }

        void OnGUI()
        {
            if (!TryResolveBinding())
            {
                EditorGUILayout.HelpBox("Pose Source Editor requires an exact Presentation Profile and Sequence binding.", MessageType.Error);
                return;
            }
            DrawHeader();
            m_Page = (Page)GUILayout.Toolbar((int)m_Page, new[] { "Source", "Markers / Curve", "Analysis", "Preview" });
            EditorGUILayout.Space(5f);
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            switch (m_Page)
            {
                case Page.Source: DrawSource(); break;
                case Page.Time: DrawTime(); break;
                case Page.Analysis: DrawAnalysis(); break;
                case Page.Preview: DrawPreview(); break;
            }
            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(m_Error))
                EditorGUILayout.HelpBox(m_Error, MessageType.Error);
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(m_Slot.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Sequence · {m_Adapter.DurationFrames}F · {m_Adapter.FrameRate:0.##} fps", EditorStyles.miniLabel);
            if (GUILayout.Button("Ping Profile", EditorStyles.toolbarButton))
                EditorGUIUtility.PingObject(m_Profile);
            EditorGUILayout.EndHorizontal();
        }

        void DrawSource()
        {
            EditorGUILayout.LabelField("Sequence Source", EditorStyles.boldLabel);
            m_Clip = EditorGUILayout.ObjectField("Animation Clip", m_Clip, typeof(AnimationClip), false) as AnimationClip;
            m_Loop = EditorGUILayout.Toggle("Loop", m_Loop);
            m_PlayRate = EditorGUILayout.FloatField("Default Play Rate", m_PlayRate);
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
            m_MarkerGroup = EditorGUILayout.DelayedTextField("Marker Group", m_MarkerGroup);
            m_Topology = (AnimationMarkerSequenceTopology)EditorGUILayout.EnumPopup("Topology", m_Topology);
            m_SyncRole = (AnimationMarkerSyncRole)EditorGUILayout.EnumPopup("Sync Role", m_SyncRole);
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Apply Source And Sync Settings", GUILayout.Height(26f)))
                Configure(m_Markers, m_Curve);
            EditorGUILayout.HelpBox("Apply only changes the Pose Source binding. It does not run Foot Analysis or Character Build.", MessageType.Info);
        }

        void DrawTime()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Frame All", GUILayout.Width(90f)))
                m_TimeField.ResetView();
            GUILayout.Label("Wheel: zoom · Alt/Middle drag: pan · Double click curve: add key · Drag empty curve: box select", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            Rect field = GUILayoutUtility.GetRect(200f, m_TimeField.RequiredHeight, GUILayout.ExpandWidth(true));
            m_TimeField.Draw(field, m_Adapter);
            EditorGUILayout.Space(6f);
            m_TimeField.DrawSelectionInspector(m_Adapter);
        }

        void DrawAnalysis()
        {
            EditorGUILayout.LabelField("Foot Analysis", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(m_AnalysisStatus, AnalysisMessageType());
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Candidates"))
                m_Adapter.RefreshAnalysis();
            using (new EditorGUI.DisabledScope(!m_Adapter.CanApplyAnalysisCandidates))
                if (GUILayout.Button("Apply Left / Right Contact Candidates"))
                    m_Adapter.ApplyAnalysisCandidates("Apply Foot Contact Candidates");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5f);
            for (int i = 0; i < m_Candidates.Length; i++)
            {
                AnimationTimeAnalysisCandidate candidate = m_Candidates[i];
                EditorGUILayout.LabelField(candidate.DisplayName, $"{candidate.Frame}F · confidence {candidate.Confidence:0.###}");
            }
            EditorGUILayout.HelpBox("Refresh only reads the exact generated artifact. Apply is the explicit authoring step; stale or missing data is never substituted.", MessageType.Info);
        }

        void DrawPreview()
        {
            EditorGUILayout.LabelField("Sequence Preview Context", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Profile", m_Profile, typeof(CharacterAnimationPresentationProfile), false);
                EditorGUILayout.ObjectField("Rig", m_Binding.Rig, typeof(CharacterAnimationRigDefinition), false);
                EditorGUILayout.ObjectField("Clip", m_Binding.Clip, typeof(AnimationClip), false);
            }
            int maximum = Mathf.Max(0, m_Adapter.DurationFrames - 1);
            int frame = EditorGUILayout.IntSlider("Frame", Mathf.Clamp(m_PreviewFrame, 0, maximum), 0, maximum);
            if (frame != m_PreviewFrame)
                m_Adapter.Seek(frame);
            EditorGUILayout.LabelField("Normalized Time", maximum > 0 ? (m_PreviewFrame / (float)maximum).ToString("0.0000") : "0");
            EditorGUILayout.HelpBox("This page owns source seek only. Pose Graph staged preview is selected from the Pose Graph Preview target and never triggers Build.", MessageType.Info);
        }

        void Configure(AnimationTimeMarker[] markers, AnimationCurve curve)
        {
            try
            {
                if (!m_Clip)
                    throw new InvalidOperationException("Sequence Source requires an Animation Clip.");
                AnimationTimeMarker[] ordered = markers.OrderBy(value => value.Frame).ThenBy(value => value.MarkerId, StringComparer.Ordinal).ToArray();
                var payload = new PresentationPoseSourceMarker[ordered.Length];
                for (int i = 0; i < ordered.Length; i++)
                    payload[i] = new PresentationPoseSourceMarker(ordered[i].AuthoringId, ordered[i].MarkerId, ordered[i].Frame);
                string markerGroup = ordered.Length == 0 ? string.Empty : m_MarkerGroup;
                AnimationMarkerSequenceTopology topology = ordered.Length == 0 ? AnimationMarkerSequenceTopology.Unspecified : m_Topology;
                AnimationMarkerSyncRole syncRole = ordered.Length == 0 ? AnimationMarkerSyncRole.Unspecified : m_SyncRole;
                CharacterAnimationPresentationAuthoringService.ConfigureSequencePoseSourceBinding(
                    m_Profile,
                    (CharacterSequencePoseSourceSlot)m_Slot,
                    m_Clip,
                    m_Loop,
                    m_PlayRate,
                    markerGroup,
                    topology,
                    syncRole,
                    payload,
                    curve);
                Reload(false);
                m_Error = string.Empty;
            }
            catch (Exception exception)
            {
                m_Error = exception.Message;
            }
        }

        void RefreshAnalysis()
        {
            m_CandidateSet = null;
            m_Candidates = Array.Empty<AnimationTimeAnalysisCandidate>();
            try
            {
                string path = AssetDatabase.GUIDToAssetPath(m_Profile.FootPlacementAnalysisSourceAssetGuid);
                CharacterFootPlacementAnalysisSource source = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
                if (!source)
                    throw new InvalidOperationException("Profile Foot Analysis Source does not resolve to an exact asset.");
                AnimationFootAnalysisArtifactIdentity identity = AnimationFootAnalysisArtifactIdentityBuilder.Build(m_Clip, source);
                AnimationFootAnalysisArtifactInspection inspection = AnimationFootAnalysisArtifactStore.Inspect(identity);
                if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready || inspection.Artifact == null)
                    throw new InvalidOperationException($"Foot Analysis artifact is {inspection.Status}: {inspection.Error}");
                m_CandidateSet = AnimationFootContactCandidateSet.Build(m_Clip, inspection.Artifact, false);
                var candidates = new AnimationTimeAnalysisCandidate[m_CandidateSet.Candidates.Count];
                for (int i = 0; i < candidates.Length; i++)
                {
                    AnimationFootContactCandidate sourceCandidate = m_CandidateSet.Candidates[i];
                    int frame = Mathf.Clamp(Mathf.RoundToInt(sourceCandidate.SourceNormalizedTime * m_Adapter.DurationFrames), 0, m_Adapter.DurationFrames - 1);
                    Color color = sourceCandidate.Side == TimelineFootContactSide.Left
                        ? new Color(0.2f, 0.85f, 1f)
                        : new Color(1f, 0.52f, 0.25f);
                    candidates[i] = new AnimationTimeAnalysisCandidate(
                        $"{sourceCandidate.MarkerId}/{i}",
                        sourceCandidate.MarkerId,
                        frame,
                        sourceCandidate.PlantConfidence,
                        color);
                }
                m_Candidates = candidates;
                m_AnalysisStatus = $"Ready · {m_Candidates.Length} candidates · artifact {m_CandidateSet.ArtifactContentHash}";
                m_Error = string.Empty;
            }
            catch (Exception exception)
            {
                m_AnalysisStatus = exception.Message;
                m_Error = string.Empty;
            }
        }

        void ApplyCandidates()
        {
            var result = new List<AnimationTimeMarker>();
            var reusable = new Dictionary<string, Queue<string>>(StringComparer.Ordinal)
            {
                [TimelineFootContactMarkerProposal.LeftMarkerId] = new Queue<string>(),
                [TimelineFootContactMarkerProposal.RightMarkerId] = new Queue<string>()
            };
            for (int i = 0; i < m_Markers.Length; i++)
            {
                AnimationTimeMarker marker = m_Markers[i];
                if (reusable.TryGetValue(marker.MarkerId, out Queue<string> ids))
                    ids.Enqueue(marker.AuthoringId);
                else
                    result.Add(marker);
            }
            for (int i = 0; i < m_Candidates.Length; i++)
            {
                AnimationTimeAnalysisCandidate candidate = m_Candidates[i];
                Queue<string> ids = reusable[candidate.DisplayName];
                result.Add(new AnimationTimeMarker(ids.Count > 0 ? ids.Dequeue() : Guid.NewGuid().ToString("N"), candidate.DisplayName, candidate.Frame));
            }
            if (string.IsNullOrWhiteSpace(m_MarkerGroup))
                m_MarkerGroup = "LocomotionFeet";
            m_Topology = m_Loop ? AnimationMarkerSequenceTopology.Cyclic : AnimationMarkerSequenceTopology.Finite;
            if (m_SyncRole == AnimationMarkerSyncRole.Unspecified)
                m_SyncRole = AnimationMarkerSyncRole.CanBeLeader;
            Configure(result.ToArray(), m_Curve);
        }

        void Reload(bool resetView = true)
        {
            if (!TryResolveBinding())
                return;
            m_Clip = m_Binding.Clip;
            m_Loop = m_Binding.Loop;
            m_PlayRate = m_Binding.DefaultPlayRate;
            m_MarkerGroup = m_Binding.MarkerGroupId;
            m_Topology = m_Binding.MarkerTopology;
            m_SyncRole = m_Binding.SyncRole;
            m_Markers = m_Binding.Markers.Select(value => new AnimationTimeMarker(value.AuthoringId, value.MarkerId, value.Frame)).ToArray();
            m_Curve = m_Binding.FootPlacementWeightCurve;
            m_Adapter = new SourceAdapter(this);
            if (resetView)
                m_TimeField.ResetView();
            RefreshAnalysis();
            Repaint();
        }

        bool TryResolveBinding()
        {
            if (!m_Profile || !m_Slot || m_Slot is not CharacterSequencePoseSourceSlot)
                return false;
            m_Binding = m_Profile.FindPoseSourceBinding(m_Slot) as CharacterSequencePoseSourceBinding;
            return m_Binding;
        }

        MessageType AnalysisMessageType() => m_CandidateSet != null ? MessageType.Info : MessageType.Warning;

        static CharacterAnimationPresentationProfile FindOwner(CharacterPresentationPoseSourceBinding binding)
        {
            string path = AssetDatabase.GetAssetPath(binding);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadMainAssetAtPath(path) as CharacterAnimationPresentationProfile;
        }

        static CharacterPipelineDefinition FindDefinition(CharacterAnimationPresentationProfile profile)
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterPipelineDefinition");
            CharacterPipelineDefinition result = null;
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterPipelineDefinition value = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!value || value.AnimationPresentationProfile != profile)
                    continue;
                if (result)
                    return null;
                result = value;
            }
            return result;
        }

        static void OpenMotionMatching(CharacterMotionMatchingPoseSourceBinding binding)
        {
            if (!binding.Profile)
                throw new InvalidOperationException("Motion Matching Pose Source has no Profile.");
            Selection.activeObject = binding.Profile;
            EditorGUIUtility.PingObject(binding.Profile);
            AssetDatabase.OpenAsset(binding.Profile);
            if (binding.Databases.Count == 1 && binding.Databases[0])
                EditorGUIUtility.PingObject(binding.Databases[0]);
        }

        sealed class SourceAdapter : IAnimationTimeFieldAuthoringAdapter
        {
            readonly CharacterPoseSourceEditorWindow m_Window;

            public SourceAdapter(CharacterPoseSourceEditorWindow window) => m_Window = window;
            public string AuthoringIdentity => m_Window.m_Slot ? m_Window.m_Slot.name : string.Empty;
            public int DurationFrames => Mathf.Max(1, Mathf.RoundToInt((m_Window.m_Clip ? m_Window.m_Clip.length : 1f) * FrameRate));
            public float FrameRate => m_Window.m_Clip && float.IsFinite(m_Window.m_Clip.frameRate) && m_Window.m_Clip.frameRate > 0f ? m_Window.m_Clip.frameRate : TimelineUtility.FrameRate;
            public bool IsCyclic => m_Window.m_Loop;
            public bool CanEditMarkers => true;
            public string CurveLabel => "FOOT PLACEMENT WEIGHT";
            public int CurveStartFrame => 0;
            public int CurveDurationFrames => DurationFrames;
            public bool CanEditCurve => true;
            public IReadOnlyList<AnimationTimeMarker> Markers => m_Window.m_Markers;
            public IReadOnlyList<AnimationTimeAnalysisCandidate> AnalysisCandidates => m_Window.m_Candidates;
            public string AnalysisStatus => m_Window.m_AnalysisStatus;
            public bool CanRefreshAnalysis => true;
            public bool CanApplyAnalysisCandidates => m_Window.m_CandidateSet != null && m_Window.m_Candidates.Length > 0;
            public AnimationCurve ReadCurve() => new AnimationCurve(m_Window.m_Curve.keys) { preWrapMode = m_Window.m_Curve.preWrapMode, postWrapMode = m_Window.m_Curve.postWrapMode };

            public void ReplaceMarkers(AnimationTimeMarker[] markers, string undoName)
            {
                m_Window.Configure(markers, m_Window.m_Curve);
            }

            public void ReplaceCurve(AnimationCurve curve, string undoName)
            {
                m_Window.Configure(m_Window.m_Markers, curve);
            }

            public void Seek(int frame)
            {
                m_Window.m_PreviewFrame = Mathf.Clamp(frame, 0, DurationFrames - 1);
                m_Window.Repaint();
            }

            public void RefreshAnalysis() => m_Window.RefreshAnalysis();

            public void ApplyAnalysisCandidates(string undoName) => m_Window.ApplyCandidates();
        }
    }
}
