using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Editor.MotionMatching;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
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
        readonly List<AnimationProducerAuthoringEntry> m_AnimationProducers = new List<AnimationProducerAuthoringEntry>();
        readonly List<string> m_ConfigurationErrors = new List<string>();
        readonly List<CharacterMotionMatchingAuthoringDiagnostic> m_MotionMatchingDiagnostics =
            new List<CharacterMotionMatchingAuthoringDiagnostic>();
        SerializedProperty m_PoseGraph;
        SerializedProperty m_RigDefinition;
        SerializedProperty m_MotionMatchingProfile;
        SerializedProperty m_FootAnalysisMode;
        SerializedProperty m_FootAnalysisSourceAssetGuid;
        CharacterPipelineDefinition m_InspectedContext;
        string m_BindingError = string.Empty;
        int m_SelectedContextIndex = -1;
        bool m_ShowProducerBindings = true;

        CharacterAnimationPresentationProfile Profile => target as CharacterAnimationPresentationProfile;

        void OnEnable()
        {
            m_PoseGraph = serializedObject.FindProperty("m_PoseGraph");
            m_RigDefinition = serializedObject.FindProperty("m_RigDefinition");
            m_MotionMatchingProfile = serializedObject.FindProperty("m_MotionMatchingProfile");
            m_FootAnalysisMode = serializedObject.FindProperty("m_FootPlacementAnalysisMode");
            m_FootAnalysisSourceAssetGuid = serializedObject.FindProperty("m_FootPlacementAnalysisSourceAssetGuid");
            RefreshContexts();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Animation Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PoseGraph, new GUIContent("Pose Graph"));
            EditorGUILayout.PropertyField(m_RigDefinition, new GUIContent("Rig Definition"));
            EditorGUILayout.PropertyField(m_MotionMatchingProfile, new GUIContent("Motion Matching Profile"));
            EditorGUILayout.HelpBox(
                "Motion Matching is optional per Character Definition. Configure one Motion Matching Profile only when at least one producer uses Motion Matching; Timeline-only Definitions leave it empty.",
                MessageType.Info);
            DrawFootAnalysis();
            bool changed = serializedObject.ApplyModifiedProperties();
            if (changed)
            {
                m_BindingError = string.Empty;
                InvalidateProjection();
            }

            DrawPresentationAssetSummary();
            DrawConfigurationErrors();
            DrawContext();
            DrawProducerBindings();
        }

        void DrawFootAnalysis()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Foot Analysis", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_FootAnalysisMode, new GUIContent("Mode"));
            CharacterFootPlacementAnalysisMode mode =
                (CharacterFootPlacementAnalysisMode)m_FootAnalysisMode.enumValueIndex;
            if (mode == CharacterFootPlacementAnalysisMode.Disabled)
            {
                m_FootAnalysisSourceAssetGuid.stringValue = string.Empty;
                return;
            }

            string guid = m_FootAnalysisSourceAssetGuid.stringValue;
            CharacterFootPlacementAnalysisSource current = CharacterFootPlacementAnalysisSource.IsAssetGuid(guid)
                ? AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(AssetDatabase.GUIDToAssetPath(guid))
                : null;
            CharacterFootPlacementAnalysisSource next = EditorGUILayout.ObjectField(
                "Analysis Source",
                current,
                typeof(CharacterFootPlacementAnalysisSource),
                false) as CharacterFootPlacementAnalysisSource;
            if (next != current)
            {
                string path = next ? AssetDatabase.GetAssetPath(next) : string.Empty;
                m_FootAnalysisSourceAssetGuid.stringValue = next ? AssetDatabase.AssetPathToGUID(path) : string.Empty;
            }
            if (!next)
            {
                EditorGUILayout.HelpBox("Generated Foot Analysis requires an explicit Analysis Source asset.", MessageType.Error);
                return;
            }
            try
            {
                next.RequireValid();
                EditorGUILayout.HelpBox(
                    $"Source Ready: {next.AnalysisSourceId.Value} / v{next.AnalysisVersion} / {CharacterFootPlacementAnalysisSource.AlgorithmVersion}",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox($"Analysis Source Invalid: {exception.Message}", MessageType.Error);
            }
        }

        void DrawPresentationAssetSummary()
        {
            CharacterAnimationPresentationProfile profile = Profile;
            if (!profile)
                return;

            CharacterPresentationPoseGraphAsset poseGraph = profile.PoseGraph;
            CharacterAnimationRigDefinition rig = profile.RigDefinition;
            CharacterMotionMatchingProfile motionMatching = profile.MotionMatchingProfile;
            if (poseGraph && poseGraph.Graph != null)
                EditorGUILayout.LabelField("Pose Graph Identity", $"{poseGraph.Graph.GraphId} @ {poseGraph.Graph.ContentRevision}");
            if (rig)
                EditorGUILayout.LabelField("Rig Identity", $"{rig.RigId} @ {rig.Revision}");
            if (motionMatching)
                EditorGUILayout.LabelField("Motion Matching Identity", $"{motionMatching.ProfileId} @ {motionMatching.Revision}");

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!poseGraph))
            {
                if (GUILayout.Button("Open Pose Graph"))
                    CharacterPresentationPoseGraphEditorWindow.Open(
                        poseGraph,
                        profile,
                        SelectedContext ? SelectedContext.PresentationProjection : null,
                        SelectedContext);
            }
            using (new EditorGUI.DisabledScope(!rig))
            {
                if (GUILayout.Button("Open Rig"))
                    OpenAsset(rig);
            }
            EditorGUILayout.EndHorizontal();
            using (new EditorGUI.DisabledScope(!motionMatching))
            {
                if (GUILayout.Button("Open Motion Matching Profile"))
                    OpenAsset(motionMatching);
            }
            EditorGUILayout.Space(6f);
        }

        void DrawConfigurationErrors()
        {
            m_ConfigurationErrors.Clear();
            CharacterAnimationPresentationProfile profile = Profile;
            profile?.CollectConfigurationErrors(m_ConfigurationErrors);
            for (int i = 0; i < m_ConfigurationErrors.Count; i++)
                EditorGUILayout.HelpBox(m_ConfigurationErrors[i], MessageType.Error);
            DrawMotionMatchingOwnershipDiagnostics();
        }

        void DrawMotionMatchingOwnershipDiagnostics()
        {
            m_MotionMatchingDiagnostics.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationPresentationProfile");
            var profiles = new List<CharacterAnimationPresentationProfile>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterAnimationPresentationProfile profile =
                    AssetDatabase.LoadAssetAtPath<CharacterAnimationPresentationProfile>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (profile)
                    profiles.Add(profile);
            }
            CharacterMotionMatchingAuthoringValidator.CollectPresentationOwnershipDiagnostics(
                Profile,
                profiles,
                m_MotionMatchingDiagnostics);
            for (int i = 0; i < m_MotionMatchingDiagnostics.Count; i++)
            {
                CharacterMotionMatchingAuthoringDiagnostic diagnostic = m_MotionMatchingDiagnostics[i];
                EditorGUILayout.HelpBox($"{diagnostic.Code}: {diagnostic.Message}", MessageType.Error);
            }
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

            if (!TryInspectAuthoring(context, out string projectionError))
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

        bool TryInspectAuthoring(CharacterPipelineDefinition context, out string error)
        {
            if (m_InspectedContext == context && m_AnimationProducers.Count > 0)
            {
                error = string.Empty;
                return true;
            }

            InvalidateProjection();
            try
            {
                IReadOnlyList<AnimationProducerAuthoringEntry> producers =
                    CharacterAnimationPresentationAuthoringService.DiscoverProducers(Profile, context);
                for (int i = 0; i < producers.Count; i++)
                    m_AnimationProducers.Add(producers[i]);
                m_InspectedContext = context;
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
            AnimationProducerAuthoringEntry producer)
        {
            CharacterAnimationPresentationProfile profile = Profile;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(producer.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Producer", producer.ProgramProducerIdentity);
            EditorGUILayout.LabelField("Animation Channel", producer.AnimationChannelId.Value);
            EditorGUILayout.LabelField("Source Clips", producer.SourceClips.Count.ToString());
            for (int clipIndex = 0; clipIndex < producer.SourceClips.Count; clipIndex++)
            {
                AnimationProducerSourceClipAuthoringEntry sourceClip = producer.SourceClips[clipIndex];
                EditorGUILayout.LabelField("Source Clip Identity", sourceClip.StableIdentity);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Animation Clip", sourceClip.Clip, typeof(UnityEngine.AnimationClip), false);
            }

            AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producer.ProducerId);
            int sourceKind = binding == null ? 0 : (int)binding.SourceKind;
            int nextSourceKind = EditorGUILayout.Popup("Pose Source", sourceKind, new[] { "Unbound", "Timeline", "Motion Matching", "Blend Space" });
            if (nextSourceKind != sourceKind)
            {
                try
                {
                    if (nextSourceKind == 0)
                        CharacterAnimationPresentationAuthoringService.RemoveProducerBinding(profile, context, producer.ProducerId);
                    else if (nextSourceKind == (int)AnimationPoseSourceKind.MotionMatching)
                        CharacterAnimationPresentationAuthoringService.ConfigureMotionMatchingProducerBinding(profile, context, producer.ProducerId);
                    m_BindingError = string.Empty;
                    binding = profile.FindProducerBinding(producer.ProducerId);
                }
                catch (Exception exception)
                {
                    m_BindingError = exception.Message;
                }
            }

            TransitionAssetBase currentSource = binding?.Source;
            TransitionAssetBase sourceAsset = (TransitionAssetBase)EditorGUILayout.ObjectField(
                "Timeline Source",
                currentSource,
                typeof(TransitionAssetBase),
                false);
            if (sourceAsset != currentSource)
            {
                try
                {
                    if (sourceAsset)
                        CharacterAnimationPresentationAuthoringService.ConfigureTimelineProducerBinding(profile, context, producer.ProducerId, sourceAsset);
                    else if (binding?.SourceKind == AnimationPoseSourceKind.Timeline)
                        CharacterAnimationPresentationAuthoringService.RemoveProducerBinding(profile, context, producer.ProducerId);
                    m_BindingError = string.Empty;
                    binding = profile.FindProducerBinding(producer.ProducerId);
                }
                catch (Exception exception)
                {
                    m_BindingError = exception.Message;
                }
            }

            CharacterAnimationBlendSpaceAsset currentBlendSpace = binding?.BlendSpaceSource;
            CharacterAnimationBlendSpaceAsset blendSpaceAsset = (CharacterAnimationBlendSpaceAsset)EditorGUILayout.ObjectField(
                "Blend Space Source",
                currentBlendSpace,
                typeof(CharacterAnimationBlendSpaceAsset),
                false);
            if (blendSpaceAsset != currentBlendSpace)
            {
                try
                {
                    if (blendSpaceAsset)
                        CharacterAnimationPresentationAuthoringService.ConfigureBlendSpaceProducerBinding(profile, context, producer.ProducerId, blendSpaceAsset);
                    else if (binding?.SourceKind == AnimationPoseSourceKind.BlendSpace)
                        CharacterAnimationPresentationAuthoringService.RemoveProducerBinding(profile, context, producer.ProducerId);
                    m_BindingError = string.Empty;
                    binding = profile.FindProducerBinding(producer.ProducerId);
                }
                catch (Exception exception)
                {
                    m_BindingError = exception.Message;
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Graph"))
                OpenGraph(context, producer.Timeline);
            if (GUILayout.Button("Open Timeline"))
                OpenTimeline(context, producer);
            UnityEngine.Object sourceObject = binding?.SourceKind == AnimationPoseSourceKind.BlendSpace
                ? binding.BlendSpaceSource
                : binding?.Source;
            using (new EditorGUI.DisabledScope(!sourceObject))
            {
                if (GUILayout.Button("Open Source"))
                {
                    if (sourceObject is CharacterAnimationBlendSpaceAsset blendSpace)
                        CharacterAnimationBlendSpaceEditorWindow.Open(blendSpace, context);
                    else
                        OpenAsset(sourceObject);
                }
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
            m_AnimationProducers.Clear();
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
            AnimationProducerAuthoringEntry producer)
        {
            BaseTreeWindow graphWindow = OpenGraph(definition, producer.Timeline);
            TimelineEditorWindow.Open(graphWindow, producer.Timeline.Node)?.FocusSource(
                producer.ProducerId.TrackAuthoringId,
                string.Empty);
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
