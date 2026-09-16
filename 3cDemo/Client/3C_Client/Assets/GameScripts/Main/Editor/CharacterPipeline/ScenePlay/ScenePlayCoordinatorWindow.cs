using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    public sealed class ScenePlayCoordinatorWindow : EditorWindow
    {
        SimulationSessionHost m_SessionHost;
        FixedCharacterHost m_CharacterHost;
        Vector2 m_Scroll;
        string m_StatusText = "Ready";

        [MenuItem("3C/ScenePlay/Coordinator", priority = 10)]
        public static void Open()
        {
            var window = GetWindow<ScenePlayCoordinatorWindow>("ScenePlay");
            window.minSize = new Vector2(360f, 400f);
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
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("ScenePlay Coordinator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ScenePlay 拥有预览生命周期。Timeline UI 通过正式 Runtime 消费事实，" +
                "不拥有独立时钟、求值器或播放器。", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Setup Corin GameplayLab Fixed Scene"))
                CorinGameplayLabSceneSetup.SetupScene();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Lifecycle", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = EditorApplication.isPlaying;
                if (GUILayout.Button("Refresh"))
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

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("CharacterHost", m_CharacterHost, typeof(FixedCharacterHost), true);
            if (m_CharacterHost != null)
            {
                EditorGUILayout.LabelField("ActorId", m_CharacterHost.ActorId.ToString());
                EditorGUILayout.LabelField("Definition", m_CharacterHost.CharacterDefinition ? m_CharacterHost.CharacterDefinition.name : "None");
                var profile = m_CharacterHost.AnimationPresentationProfile;
                EditorGUILayout.LabelField("AnimationProfile", profile ? profile.name : "None");
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(m_StatusText, MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        void SetStatus(string text)
        {
            m_StatusText = text;
            Repaint();
        }
    }
}
