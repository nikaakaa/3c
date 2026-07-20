using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterAnimationPresentationProfile))]
    public sealed class CharacterAnimationPresentationProfileEditor : UnityEditor.Editor
    {
        readonly List<CharacterPipelineDefinition> m_Contexts = new List<CharacterPipelineDefinition>();
        readonly List<CharacterPresentationProducerEntry> m_AnimationProducers = new List<CharacterPresentationProducerEntry>();
        readonly List<string> m_ConfigurationErrors = new List<string>();
        readonly Dictionary<AnimationProducerId, CharacterAuthoringTimelineEntry> m_ProducerSources =
            new Dictionary<AnimationProducerId, CharacterAuthoringTimelineEntry>();
        readonly Dictionary<AnimationProducerId, string> m_ProducerDisplayNames =
            new Dictionary<AnimationProducerId, string>();
        SerializedProperty m_Layers;
        SerializedProperty m_TransitionLibrary;
        CharacterPipelineDefinition m_InspectedContext;
        CharacterSimulationProgramAsset m_InspectedProgramAsset;
        CharacterPresentationProjectionAsset m_InspectedProjectionAsset;
        CharacterPresentationProjection m_InspectedProjection;
        string m_InspectedProgramHash = string.Empty;
        string m_InspectedProjectionRevision = string.Empty;
        string m_BindingError = string.Empty;
        int m_SelectedContextIndex = -1;
        bool m_ShowProducerBindings = true;

        CharacterAnimationPresentationProfile Profile => target as CharacterAnimationPresentationProfile;

        void OnEnable()
        {
            m_Layers = serializedObject.FindProperty("m_Layers");
            m_TransitionLibrary = serializedObject.FindProperty("m_TransitionLibrary");
            RefreshContexts();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Animation Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Layers, new GUIContent("Layers"), true);
            EditorGUILayout.PropertyField(m_TransitionLibrary, new GUIContent("Transition Library"));
            bool changed = serializedObject.ApplyModifiedProperties();
            if (changed)
                m_BindingError = string.Empty;

            DrawTransitionLibraryNavigation();
            DrawConfigurationErrors();
            DrawContext();
            DrawProducerBindings();
        }

        void DrawTransitionLibraryNavigation()
        {
            CharacterAnimationPresentationProfile profile = Profile;
            using (new EditorGUI.DisabledScope(!profile || !profile.TransitionLibrary))
            {
                if (GUILayout.Button("Open Transition Library"))
                    OpenAsset(profile.TransitionLibrary);
            }
            EditorGUILayout.Space(6f);
        }

        void DrawConfigurationErrors()
        {
            m_ConfigurationErrors.Clear();
            CharacterAnimationPresentationProfile profile = Profile;
            if (profile && profile.CollectConfigurationErrors(m_ConfigurationErrors))
                return;
            for (int i = 0; i < m_ConfigurationErrors.Count; i++)
                EditorGUILayout.HelpBox(m_ConfigurationErrors[i], MessageType.Error);
        }

        void DrawContext()
        {
            EditorGUILayout.LabelField("Definition Context", EditorStyles.boldLabel);
            if (m_Contexts.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No CharacterPipelineDefinition references this Profile. Producer projection and binding authoring are unavailable.",
                    MessageType.Error);
            }
            else if (m_Contexts.Count == 1)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Definition", m_Contexts[0], typeof(CharacterPipelineDefinition), false);
            }
            else
            {
                string[] options = new string[m_Contexts.Count + 1];
                options[0] = "Select Definition...";
                for (int i = 0; i < m_Contexts.Count; i++)
                    options[i + 1] = $"{m_Contexts[i].name}  ({AssetDatabase.GetAssetPath(m_Contexts[i])})";
                int next = EditorGUILayout.Popup("Definition", m_SelectedContextIndex + 1, options) - 1;
                if (next != m_SelectedContextIndex)
                {
                    m_SelectedContextIndex = next;
                    InvalidateProjection();
                }
                if (m_SelectedContextIndex < 0)
                {
                    EditorGUILayout.HelpBox(
                        "This Profile is shared. Select the Definition whose compiled producer projection you want to edit.",
                        MessageType.Warning);
                }
            }

            if (GUILayout.Button("Refresh Definition Contexts"))
                RefreshContexts();
            EditorGUILayout.Space(6f);
        }

        void DrawProducerBindings()
        {
            CharacterPipelineDefinition context = SelectedContext;
            if (!context)
                return;

            if (!TryInspectProjection(context, out string projectionError))
            {
                EditorGUILayout.HelpBox(projectionError, MessageType.Error);
                return;
            }

            m_ShowProducerBindings = EditorGUILayout.Foldout(
                m_ShowProducerBindings,
                $"Producer Bindings ({m_AnimationProducers.Count})",
                true);
            if (!m_ShowProducerBindings)
                return;

            if (!string.IsNullOrEmpty(m_BindingError))
                EditorGUILayout.HelpBox(m_BindingError, MessageType.Error);
            for (int i = 0; i < m_AnimationProducers.Count; i++)
                DrawProducerBinding(context, m_AnimationProducers[i]);
        }

        CharacterPipelineDefinition SelectedContext
        {
            get
            {
                if (m_Contexts.Count == 1)
                    return m_Contexts[0];
                return m_SelectedContextIndex >= 0 && m_SelectedContextIndex < m_Contexts.Count
                    ? m_Contexts[m_SelectedContextIndex]
                    : null;
            }
        }

        bool TryInspectProjection(CharacterPipelineDefinition context, out string error)
        {
            CharacterSimulationProgramAsset programAsset = context.SimulationProgram;
            CharacterPresentationProjectionAsset projectionAsset = context.PresentationProjection;
            if (!programAsset || !projectionAsset)
            {
                error = $"CharacterPipelineDefinition '{context.name}' requires compiled Program and Presentation Projection assets.";
                return false;
            }

            if (m_InspectedProjection != null && m_InspectedContext == context &&
                m_InspectedProgramAsset == programAsset && m_InspectedProjectionAsset == projectionAsset &&
                string.Equals(m_InspectedProgramHash, programAsset.ProgramHash, StringComparison.Ordinal) &&
                string.Equals(m_InspectedProjectionRevision, projectionAsset.SourceRevision, StringComparison.Ordinal))
            {
                error = string.Empty;
                return true;
            }

            InvalidateProjection();
            try
            {
                m_InspectedProjection = projectionAsset.Inspect(programAsset);
                var topologyErrors = new List<string>();
                CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                    context.RootTreeAsset ? context.RootTreeAsset.Tree : null,
                    topologyErrors);
                if (!topology.IsValid)
                    throw new InvalidOperationException(string.Join("\n", topologyErrors));
                IReadOnlyList<CharacterPresentationProducerEntry> producers = m_InspectedProjection.AnimationProducers;
                for (int i = 0; i < producers.Count; i++)
                {
                    CharacterPresentationProducerEntry producer = producers[i];
                    if (!TryResolveTimeline(topology, producer, out CharacterAuthoringTimelineEntry source, out string trackName))
                        throw new InvalidOperationException(
                            $"Animation producer on layer '{producer.LayerId}' no longer resolves to Timeline Track '{producer.Animation?.TrackName}'.");
                    m_AnimationProducers.Add(producers[i]);
                    m_ProducerSources[producer.ProducerId] = source;
                    m_ProducerDisplayNames[producer.ProducerId] = $"{source.Graph.name} / {source.Timeline.Name} / {trackName}";
                }
                m_InspectedContext = context;
                m_InspectedProgramAsset = programAsset;
                m_InspectedProjectionAsset = projectionAsset;
                m_InspectedProgramHash = programAsset.ProgramHash;
                m_InspectedProjectionRevision = projectionAsset.SourceRevision;
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        void DrawProducerBinding(
            CharacterPipelineDefinition context,
            CharacterPresentationProducerEntry producer)
        {
            CharacterAnimationPresentationProfile profile = Profile;
            CharacterAuthoringTimelineEntry source = m_ProducerSources[producer.ProducerId];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(m_ProducerDisplayNames[producer.ProducerId], EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Layer", producer.LayerId);

            AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producer.ProducerId);
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
                try
                {
                    if (transition)
                    {
                        CharacterAnimationPresentationAuthoringService.ConfigureProducerBinding(
                            profile,
                            context,
                            producer.ProducerId,
                            transition,
                            nextEasing);
                    }
                    else
                    {
                        CharacterAnimationPresentationAuthoringService.RemoveProducerBinding(
                            profile,
                            context,
                            producer.ProducerId);
                    }
                    m_BindingError = string.Empty;
                }
                catch (Exception exception)
                {
                    m_BindingError = exception.Message;
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Graph"))
                OpenGraph(context, source);
            if (GUILayout.Button("Open Timeline"))
                OpenTimeline(context, producer, source);
            using (new EditorGUI.DisabledScope(!transition))
            {
                if (GUILayout.Button("Open Transition"))
                    OpenAsset(transition);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void RefreshContexts()
        {
            CharacterPipelineDefinition previous = SelectedContext;
            m_Contexts.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CharacterPipelineDefinition");
            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (definition && definition.AnimationPresentationProfile == Profile)
                    m_Contexts.Add(definition);
            }

            m_SelectedContextIndex = m_Contexts.Count == 1 ? 0 : m_Contexts.IndexOf(previous);
            InvalidateProjection();
        }

        void InvalidateProjection()
        {
            m_InspectedContext = null;
            m_InspectedProgramAsset = null;
            m_InspectedProjectionAsset = null;
            m_InspectedProjection = null;
            m_InspectedProgramHash = string.Empty;
            m_InspectedProjectionRevision = string.Empty;
            m_AnimationProducers.Clear();
            m_ProducerSources.Clear();
            m_ProducerDisplayNames.Clear();
        }

        static BaseTreeWindow OpenGraph(
            CharacterPipelineDefinition definition,
            CharacterAuthoringTimelineEntry source)
        {
            BaseTreeWindow window = CharacterPipelineDefinitionTreeWindowUtility.OpenRootTree(definition);
            BaseTree rootTree = definition && definition.RootTreeAsset ? definition.RootTreeAsset.Tree : null;
            if (!window || ReferenceEquals(source.Graph, rootTree))
                return window;
            if (source.Graph is BaseTree tree)
                window.PushTreePage(tree, null, tree.name, source.Node.GUID, "animationPresentation");
            return window;
        }

        static void OpenTimeline(
            CharacterPipelineDefinition definition,
            CharacterPresentationProducerEntry producer,
            CharacterAuthoringTimelineEntry source)
        {
            BaseTreeWindow graphWindow = OpenGraph(definition, source);
            TimelineEditorWindow.Open(graphWindow, source.Node)?.FocusSource(
                producer.ProducerId.TrackAuthoringId,
                string.Empty);
        }

        static bool TryResolveTimeline(
            CharacterAuthoringTopologyProjection topology,
            CharacterPresentationProducerEntry producer,
            out CharacterAuthoringTimelineEntry source,
            out string trackName)
        {
            source = default;
            trackName = string.Empty;
            AnimationProducerId producerId = producer.ProducerId;
            for (int i = 0; i < topology.Timelines.Count; i++)
            {
                CharacterAuthoringTimelineEntry candidate = topology.Timelines[i];
                if (!string.Equals(candidate.Timeline.AuthoringId, producerId.TimelineAuthoringId, StringComparison.Ordinal))
                    continue;
                for (int trackIndex = 0; trackIndex < candidate.Timeline.Tracks.Count; trackIndex++)
                {
                    Track track = candidate.Timeline.Tracks[trackIndex];
                    if (track == null || !string.Equals(track.AuthoringId, producerId.TrackAuthoringId, StringComparison.Ordinal))
                        continue;
                    source = candidate;
                    trackName = string.IsNullOrEmpty(track.Name) ? track.GetType().Name : track.Name;
                    return true;
                }
            }
            return false;
        }

        static void OpenAsset(UnityEngine.Object asset)
        {
            if (!asset)
                return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }
    }
}
