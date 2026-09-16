using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    public sealed class ScenePlayCoordinatorWindow : EditorWindow
    {
        SimulationSessionHost m_SessionHost;
        FixedCharacterHost m_CharacterHost;
        CharacterTimelineHost m_TimelineHost;
        readonly List<CharacterTimelinePlaybackObservation> m_TimelinePlaybacks = new List<CharacterTimelinePlaybackObservation>();
        Vector2 m_Scroll;
        string m_StatusText = "Ready";

        [MenuItem("3C/ScenePlay/Coordinator", priority = 10)]
        public static void Open()
        {
            var window = GetWindow<ScenePlayCoordinatorWindow>("ScenePlay");
            window.minSize = new Vector2(420f, 520f);
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            RefreshReferences();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        void OnPlayModeChanged(PlayModeStateChange state)
        {
            RefreshReferences();
            Repaint();
        }

        void RefreshReferences()
        {
            m_SessionHost = FindObjectOfType<SimulationSessionHost>();
            m_CharacterHost = FindObjectOfType<FixedCharacterHost>();
            m_TimelineHost = m_CharacterHost
                ? m_CharacterHost.GetComponent<CharacterTimelineHost>()
                : FindObjectOfType<CharacterTimelineHost>();
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("ScenePlay Coordinator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ScenePlay 只读取领域运行事实。Timeline 播放由 Ability 启动，" +
                "Pose 姿态由表现域发布。", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Setup Corin GameplayLab Fixed Scene"))
                CorinGameplayLabSceneSetup.SetupScene();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                    RefreshReferences();
                GUI.enabled = EditorApplication.isPlaying;
                if (GUILayout.Button("Refresh Play State"))
                    RefreshReferences();
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("SessionHost", m_SessionHost, typeof(SimulationSessionHost), true);
            if (m_SessionHost != null)
            {
                EditorGUILayout.LabelField("Lifecycle", m_SessionHost.LifecycleState.ToString());
                EditorGUILayout.LabelField("Composition", m_SessionHost.Composition ? m_SessionHost.Composition.name : "None");
                EditorGUILayout.LabelField("Registrations", m_SessionHost.RegistrationCount.ToString());
                EditorGUILayout.LabelField("Diagnostics", m_SessionHost.Diagnostics != default ? "Available" : "None");
            }
            else
            {
                EditorGUILayout.HelpBox("SessionHost 不存在，先执行场景装配。", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("CharacterHost", m_CharacterHost, typeof(FixedCharacterHost), true);
            if (m_CharacterHost != null)
            {
                EditorGUILayout.LabelField("ActorId", m_CharacterHost.ActorId.ToString());
                EditorGUILayout.LabelField("Definition", m_CharacterHost.CharacterDefinition ? m_CharacterHost.CharacterDefinition.name : "None");
                var profile = m_CharacterHost.AnimationPresentationProfile;
                EditorGUILayout.LabelField("AnimationProfile", profile ? profile.name : "None");
                DrawPresentationObservation();
            }
            else
            {
                EditorGUILayout.HelpBox("CharacterHost 不存在，先执行场景装配。", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("TimelineHost", m_TimelineHost, typeof(CharacterTimelineHost), true);
            DrawTimelineLengths();
            DrawTimelinePlaybacks();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(m_StatusText, MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        void DrawPresentationObservation()
        {
            ICharacterPresentationDomainRuntime runtime = m_CharacterHost.PresentationRuntime;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("表现域尚未装配，进入 Play 后由 FixedCharacterHost 创建。", MessageType.None);
                return;
            }
            CharacterPresentationDomainObservation observation = runtime.CaptureObservation();
            EditorGUILayout.LabelField("PoseComposed", observation.PoseComposed ? "Yes" : "No");
            EditorGUILayout.LabelField("PoseAvailability", string.IsNullOrEmpty(observation.PoseAvailability) ? "None" : observation.PoseAvailability);
            EditorGUILayout.LabelField("PoseGraphRevision", string.IsNullOrEmpty(observation.PoseGraphRevision) ? "None" : observation.PoseGraphRevision);
            EditorGUILayout.LabelField("PoseInstanceId", observation.PoseInstanceId.ToString());
            EditorGUILayout.LabelField("PoseResetGeneration", observation.PoseResetGeneration.ToString());
            EditorGUILayout.LabelField("PoseBones", observation.PoseBoneCount.ToString());
            EditorGUILayout.LabelField("PoseContributions", observation.PoseContributionCount.ToString());
        }

        void DrawTimelineLengths()
        {
            IReadOnlyList<TimelineAsset> timelines = m_CharacterHost && m_CharacterHost.CharacterDefinition
                ? m_CharacterHost.CharacterDefinition.ControlMotionTimelines
                : null;
            if (timelines == null || timelines.Count == 0)
            {
                EditorGUILayout.HelpBox("Character Definition 没有配置 Control Motion Timeline。", MessageType.None);
                return;
            }
            for (int i = 0; i < timelines.Count; i++)
            {
                TimelineAsset asset = timelines[i];
                if (!asset || asset.Data == null)
                    continue;
                EditorGUILayout.ObjectField(asset.Name, asset, typeof(TimelineAsset), false);
                EditorGUILayout.LabelField($"    MaxFrame", asset.Data.MaxFrame.ToString());
                EditorGUILayout.LabelField($"    Duration", $"{asset.Data.Duration:0.###}s");
            }
        }

        void DrawTimelinePlaybacks()
        {
            if (m_TimelineHost == null)
            {
                EditorGUILayout.HelpBox("CharacterTimelineHost 不存在，先执行场景装配。", MessageType.Warning);
                return;
            }
            m_TimelineHost.CollectActivePlaybacks(m_TimelinePlaybacks);
            EditorGUILayout.LabelField("ActivePlaybacks", m_TimelinePlaybacks.Count.ToString());
            if (m_TimelinePlaybacks.Count == 0)
            {
                EditorGUILayout.HelpBox("没有活动 Timeline 播放。", MessageType.None);
                return;
            }
            for (int i = 0; i < m_TimelinePlaybacks.Count; i++)
            {
                CharacterTimelinePlaybackObservation playback = m_TimelinePlaybacks[i];
                EditorGUILayout.LabelField($"#{playback.Handle.Value}", playback.Status.ToString());
                EditorGUILayout.LabelField("    Source", string.IsNullOrEmpty(playback.SourceName) ? "None" : playback.SourceName);
                if (playback.Timeline != null)
                {
                    EditorGUILayout.LabelField("    Timeline", playback.Timeline.Name);
                    EditorGUILayout.LabelField("    ClipTime", $"{playback.ClipTime:0.###}s / {playback.Timeline.Duration:0.###}s");
                    EditorGUILayout.LabelField("    NormalizedTime", $"{playback.NormalizedTime:0.###}");
                    EditorGUILayout.LabelField("    Weight", $"{playback.Weight:0.###}");
                    EditorGUILayout.LabelField("    Frame", $"{playback.Timeline.Frame} / {playback.Timeline.MaxFrame}");
                }
            }
        }

        void SetStatus(string text)
        {
            m_StatusText = text;
            Repaint();
        }
    }
}
