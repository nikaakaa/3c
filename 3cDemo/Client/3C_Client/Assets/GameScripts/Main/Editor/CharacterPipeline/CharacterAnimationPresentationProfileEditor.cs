using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
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
        readonly List<CharacterPresentationProducerEntry> m_AnimationProducers = new List<CharacterPresentationProducerEntry>();
        readonly List<string> m_ConfigurationErrors = new List<string>();
        readonly Dictionary<AnimationProducerId, CharacterAuthoringTimelineEntry> m_ProducerSources =
            new Dictionary<AnimationProducerId, CharacterAuthoringTimelineEntry>();
        readonly Dictionary<AnimationProducerId, string> m_ProducerDisplayNames =
            new Dictionary<AnimationProducerId, string>();
        SerializedProperty m_PoseGraph;
        SerializedProperty m_BlendLibrary;
        SerializedProperty m_RigDefinition;
        SerializedProperty m_FootAnalysisMode;
        SerializedProperty m_FootAnalysisSourceAssetGuid;
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
            m_PoseGraph = serializedObject.FindProperty("m_PoseGraph");
            m_BlendLibrary = serializedObject.FindProperty("m_BlendLibrary");
            m_RigDefinition = serializedObject.FindProperty("m_RigDefinition");
            m_FootAnalysisMode = serializedObject.FindProperty("m_FootPlacementAnalysisMode");
            m_FootAnalysisSourceAssetGuid = serializedObject.FindProperty("m_FootPlacementAnalysisSourceAssetGuid");
            RefreshContexts();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Animation Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PoseGraph, new GUIContent("Pose Graph"));
            EditorGUILayout.PropertyField(m_BlendLibrary, new GUIContent("Blend Library"));
            EditorGUILayout.PropertyField(m_RigDefinition, new GUIContent("Rig Definition"));
            DrawFootAnalysis();
            bool changed = serializedObject.ApplyModifiedProperties();
            if (changed)
                m_BindingError = string.Empty;

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
            CharacterAnimationBlendLibrary blendLibrary = profile.BlendLibrary;
            CharacterAnimationRigDefinition rig = profile.RigDefinition;
            if (poseGraph && poseGraph.Graph != null)
                EditorGUILayout.LabelField("Pose Graph Identity", $"{poseGraph.Graph.GraphId} @ {poseGraph.Graph.ContentRevision}");
            if (blendLibrary)
                EditorGUILayout.LabelField("Blend Library Identity", $"{blendLibrary.LibraryId} @ {blendLibrary.Revision}");
            if (rig)
                EditorGUILayout.LabelField("Rig Identity", $"{rig.RigId} @ {rig.Revision}");

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!poseGraph))
            {
                if (GUILayout.Button("Open Pose Graph"))
                    OpenAsset(poseGraph);
            }
            using (new EditorGUI.DisabledScope(!blendLibrary))
            {
                if (GUILayout.Button("Open Blend Library"))
                    OpenAsset(blendLibrary);
            }
            using (new EditorGUI.DisabledScope(!rig))
            {
                if (GUILayout.Button("Open Rig"))
                    OpenAsset(rig);
            }
            EditorGUILayout.EndHorizontal();
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
                string.Equals(m_InspectedProjectionRevision, projectionAsset.ProjectionRevision, StringComparison.Ordinal))
            {
                error = string.Empty;
                return true;
            }

            InvalidateProjection();
            try
            {
                m_InspectedProjection = projectionAsset.Load(
                    Float32CharacterPresentationContractAdapter.Create(programAsset.Load()));
                var topologyErrors = new List<string>();
                CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                    CollectCompositionRoots(context),
                    topologyErrors);
                if (!topology.IsValid)
                    throw new InvalidOperationException(string.Join("\n", topologyErrors));
                IReadOnlyList<CharacterPresentationProducerEntry> producers = m_InspectedProjection.AnimationProducers;
                for (int i = 0; i < producers.Count; i++)
                {
                    CharacterPresentationProducerEntry producer = producers[i];
                    if (!TryResolveTimeline(topology, producer, out CharacterAuthoringTimelineEntry source, out string trackName))
                        throw new InvalidOperationException(
                            $"Animation producer on channel '{producer.AnimationChannelId}' no longer resolves to its Timeline Track.");
                    m_AnimationProducers.Add(producers[i]);
                    m_ProducerSources[producer.ProducerId] = source;
                    m_ProducerDisplayNames[producer.ProducerId] = $"{source.Graph.name} / {source.Timeline.Name} / {trackName}";
                }
                m_InspectedContext = context;
                m_InspectedProgramAsset = programAsset;
                m_InspectedProjectionAsset = projectionAsset;
                m_InspectedProgramHash = programAsset.ProgramHash;
                m_InspectedProjectionRevision = projectionAsset.ProjectionRevision;
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
            EditorGUILayout.LabelField("Animation Channel", producer.AnimationChannelId.Value);
            PoseSlotId poseSlotId = m_InspectedProjection.PoseProgram
                .RequireSlot(producer.AnimationChannelId)
                .PoseSlotId;
            EditorGUILayout.LabelField("Pose Slot", poseSlotId.Value);

            AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producer.ProducerId);
            TransitionAssetBase currentSource = binding?.Source;
            TransitionAssetBase sourceAsset = (TransitionAssetBase)EditorGUILayout.ObjectField(
                "Source",
                currentSource,
                typeof(TransitionAssetBase),
                false);
            if (sourceAsset != currentSource)
            {
                try
                {
                    if (sourceAsset)
                    {
                        CharacterAnimationPresentationAuthoringService.ConfigureProducerBinding(
                            profile,
                            context,
                            producer.ProducerId,
                            sourceAsset);
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
            using (new EditorGUI.DisabledScope(!sourceAsset))
            {
                if (GUILayout.Button("Open Source"))
                    OpenAsset(sourceAsset);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        static IReadOnlyList<BaseTree> CollectCompositionRoots(CharacterPipelineDefinition definition)
        {
            var roots = new List<BaseTree>();
            if (definition.RootTreeAsset && definition.RootTreeAsset.Tree)
                roots.Add(definition.RootTreeAsset.Tree);
            if (!definition.EquipmentCapabilityEnabled || !definition.EquipmentProfile)
                return roots;
            for (int featureIndex = 0; featureIndex < definition.EquipmentProfile.Features.Count; featureIndex++)
            {
                CharacterEquipmentFeatureDefinition feature = definition.EquipmentProfile.Features[featureIndex];
                if (!feature)
                    continue;
                if (feature.PersistentGraph)
                    roots.Add(feature.PersistentGraph);
                for (int routeIndex = 0; routeIndex < feature.RouteImplementations.Count; routeIndex++)
                {
                    EquipmentFeatureRouteImplementation route = feature.RouteImplementations[routeIndex];
                    if (route?.InlineGraph)
                        roots.Add(route.InlineGraph);
                }
            }
            return roots;
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
