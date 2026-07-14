using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Behavior;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterPipelineDefinition))]
    public sealed class CharacterPipelineDefinitionEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();
        readonly List<string> m_ProjectionErrors = new List<string>();
        SerializedProperty m_RootTreeAsset;
        SerializedProperty m_InputProfile;
        SerializedProperty m_GameplayEffectProfile;
        SerializedProperty m_ActionProfiles;
        SerializedProperty m_BehaviorProfiles;
        SerializedProperty m_AnimationPresentation;
        bool m_ShowAnimationPresentation = true;
        bool m_ShowProducerBindings = true;

        void OnEnable()
        {
            m_RootTreeAsset = serializedObject.FindProperty("m_RootTreeAsset");
            m_InputProfile = serializedObject.FindProperty("m_InputProfile");
            m_GameplayEffectProfile = serializedObject.FindProperty("m_GameplayEffectProfile");
            m_ActionProfiles = serializedObject.FindProperty("m_ActionProfiles");
            m_BehaviorProfiles = serializedObject.FindProperty("m_BehaviorProfiles");
            m_AnimationPresentation = serializedObject.FindProperty("m_AnimationPresentation");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_RootTreeAsset);
            EditorGUILayout.PropertyField(m_InputProfile);
            EditorGUILayout.PropertyField(m_GameplayEffectProfile);
            EditorGUILayout.LabelField("Action Profiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ActionProfiles, true);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Behavior Registry", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_BehaviorProfiles, true);
            DrawBehaviorSummary();
            DrawAnimationPresentationFields();
            serializedObject.ApplyModifiedProperties();
            DrawAnimationProducerBindings();
            DrawOpenRootTreeButton();
            DrawAgentAuthoringButton();
            DrawConfigurationErrors();
        }

        void DrawAnimationPresentationFields()
        {
            EditorGUILayout.Space(6f);
            m_ShowAnimationPresentation = EditorGUILayout.Foldout(
                m_ShowAnimationPresentation,
                "Animation Presentation",
                true);
            if (!m_ShowAnimationPresentation)
                return;
            if (m_AnimationPresentation == null)
            {
                EditorGUILayout.HelpBox("Animation Presentation Definition is missing.", MessageType.Error);
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_AnimationPresentation.FindPropertyRelative("m_Layers"), true);
                EditorGUILayout.PropertyField(m_AnimationPresentation.FindPropertyRelative("m_TransitionLibrary"));
            }
        }

        void DrawAnimationProducerBindings()
        {
            if (!m_ShowAnimationPresentation)
                return;

            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            CharacterAnimationPresentationDefinition presentation = definition?.AnimationPresentation;
            if (presentation == null)
                return;

            using (new EditorGUI.DisabledScope(!presentation.TransitionLibrary))
            {
                if (GUILayout.Button("Open Transition Library"))
                    OpenAsset(presentation.TransitionLibrary);
            }

            m_ProjectionErrors.Clear();
            AnimationPresentationProjection projection = AnimationPresentationProjection.Build(
                definition.RootTree,
                m_ProjectionErrors);
            for (int i = 0; i < m_ProjectionErrors.Count; i++)
                EditorGUILayout.HelpBox(m_ProjectionErrors[i], MessageType.Error);
            if (!projection.IsValid)
                return;

            m_ShowProducerBindings = EditorGUILayout.Foldout(
                m_ShowProducerBindings,
                $"Producer Bindings ({projection.Producers.Count})",
                true);
            if (!m_ShowProducerBindings)
                return;

            for (int i = 0; i < projection.Producers.Count; i++)
                DrawProducerBinding(definition, projection.Producers[i]);
        }

        static void DrawProducerBinding(
            CharacterPipelineDefinition definition,
            AnimationPresentationProducerEntry producer)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(producer.Timeline.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Layer", producer.LayerId);
            EditorGUILayout.LabelField("Graph", producer.Graph.name);
            EditorGUILayout.SelectableLabel(
                producer.ProducerId.ToString(),
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            AnimationProducerPresentationBinding binding =
                definition.AnimationPresentation.FindProducerBinding(producer.ProducerId);
            TransitionAssetBase currentTransition = binding?.Transition;
            TransitionAssetBase transition = (TransitionAssetBase)EditorGUILayout.ObjectField(
                "Animancer Transition",
                currentTransition,
                typeof(TransitionAssetBase),
                false);
            Easing.Function easing = binding?.Easing ?? Easing.Function.CubicInOut;
            Easing.Function nextEasing = (Easing.Function)EditorGUILayout.EnumPopup("Fade Easing", easing);
            if (transition != currentTransition || binding != null && nextEasing != easing)
            {
                if (transition)
                {
                    CharacterAnimationPresentationAuthoringService.ConfigureProducerBinding(
                        definition,
                        producer.ProducerId,
                        transition,
                        nextEasing);
                }
                else
                {
                    CharacterAnimationPresentationAuthoringService.RemoveProducerBinding(
                        definition,
                        producer.ProducerId);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Graph"))
                OpenGraph(definition, producer);
            if (GUILayout.Button("Open Timeline"))
                OpenTimeline(definition, producer);
            using (new EditorGUI.DisabledScope(!transition))
            {
                if (GUILayout.Button("Open Transition"))
                    OpenAsset(transition);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        static BaseTreeWindow OpenGraph(
            CharacterPipelineDefinition definition,
            AnimationPresentationProducerEntry producer)
        {
            BaseTreeWindow window = CharacterPipelineDefinitionTreeWindowUtility.OpenRootTree(definition);
            if (!window || ReferenceEquals(producer.Graph, definition.RootTree))
                return window;
            if (producer.Graph is BaseTree tree)
                window.PushTreePage(tree, null, tree.name, producer.Node.GUID, "animationPresentation");
            return window;
        }

        static void OpenTimeline(
            CharacterPipelineDefinition definition,
            AnimationPresentationProducerEntry producer)
        {
            BaseTreeWindow graphWindow = OpenGraph(definition, producer);
            TimelineEditorWindow.Open(graphWindow, producer.Node)?.FocusSource(
                producer.Track.AuthoringId,
                string.Empty);
        }

        static void OpenAsset(Object asset)
        {
            if (!asset)
                return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }

        void DrawBehaviorSummary()
        {
            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            if (!definition)
                return;

            EditorGUILayout.LabelField("Transaction Behaviors", definition.ActionProfiles.Count.ToString());
            EditorGUILayout.LabelField("Non-Transaction Behaviors", definition.BehaviorProfiles.Count.ToString());
            int effectCount = definition.GameplayEffectProfile
                ? definition.GameplayEffectProfile.EffectDefinitions.Count
                : 0;
            EditorGUILayout.LabelField("Effect Behaviors", effectCount.ToString());
            if (definition.GameplayEffectProfile)
            {
                for (int i = 0; i < definition.GameplayEffectProfile.EffectDefinitions.Count; i++)
                {
                    var effect = definition.GameplayEffectProfile.EffectDefinitions[i];
                    if (effect)
                        EditorGUILayout.ObjectField(effect.BehaviorId, effect, typeof(ThirdPersonGameplay.Effects.GameplayEffectDefinition), false);
                }
            }
        }

        void DrawOpenRootTreeButton()
        {
            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            using (new EditorGUI.DisabledScope(!definition || !definition.RootTreeAsset))
            {
                if (GUILayout.Button("Open Root Tree"))
                    CharacterPipelineDefinitionTreeWindowUtility.OpenRootTree(definition);
            }
        }

        void DrawAgentAuthoringButton()
        {
            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            using (new EditorGUI.DisabledScope(!definition))
            {
                if (GUILayout.Button("Open Agent Controller"))
                    AgentCharacterControllerSynthesisWindow.Open(definition);
            }
        }

        void DrawConfigurationErrors()
        {
            m_Errors.Clear();
            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            if (definition != null && definition.CollectConfigurationErrors(m_Errors))
            {
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
                return;
            }

            for (int i = 0; i < m_Errors.Count; i++)
                EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
        }
    }
}
