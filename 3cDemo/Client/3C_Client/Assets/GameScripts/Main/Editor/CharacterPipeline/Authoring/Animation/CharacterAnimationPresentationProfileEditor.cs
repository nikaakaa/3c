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
        readonly struct PoseSourceConsumer
        {
            public PoseSourceConsumer(
                CharacterTypedPoseNode machine,
                CharacterPoseStateDefinition state,
                CharacterTypedPoseNode sequence)
            {
                Machine = machine;
                State = state;
                Sequence = sequence;
            }

            public CharacterTypedPoseNode Machine { get; }
            public CharacterPoseStateDefinition State { get; }
            public CharacterTypedPoseNode Sequence { get; }
        }

        readonly List<CharacterPipelineDefinition> m_Contexts = new List<CharacterPipelineDefinition>();
        readonly List<AnimationProducerAuthoringEntry> m_AnimationProducers = new List<AnimationProducerAuthoringEntry>();
        readonly List<string> m_ConfigurationErrors = new List<string>();
        readonly List<CharacterMotionMatchingAuthoringDiagnostic> m_MotionMatchingDiagnostics =
            new List<CharacterMotionMatchingAuthoringDiagnostic>();
        readonly Dictionary<CharacterPresentationPoseSourceSlot, List<PoseSourceConsumer>> m_PoseSourceConsumers =
            new Dictionary<CharacterPresentationPoseSourceSlot, List<PoseSourceConsumer>>();
        CharacterPipelineDefinition m_InspectedContext;
        string m_BindingError = string.Empty;
        string m_ProducerInspectionError = string.Empty;
        string m_PoseSourceError = string.Empty;
        string m_BuildMessage = string.Empty;
        string m_NewPoseSourceName = string.Empty;
        PresentationPoseSourceKind m_NewPoseSourceKind =
            PresentationPoseSourceKind.Sequence;
        CharacterAnimationSequenceAsset m_NewPoseSourceSequence;
        CharacterAnimationBlendSpaceAsset m_NewPoseSourceBlendSpace;
        CharacterMotionMatchingProfile m_NewPoseSourceMotionMatching;
        int m_NewMotionMatchingDomainIndex;
        int m_SelectedContextIndex = -1;
        bool m_ContextsLoaded;
        bool m_ConfigurationDiagnosticsReady;
        bool m_ShowLinkedPoseBindings = true;
        bool m_ShowPoseSourceBindings = true;
        bool m_ShowProducerBindings = true;

        CharacterAnimationPresentationProfile Profile => target as CharacterAnimationPresentationProfile;

        void OnEnable()
        {
            m_PoseSourceConsumers.Clear();
            m_Contexts.Clear();
            m_ContextsLoaded = false;
            m_ConfigurationDiagnosticsReady = false;
            InvalidateProjection();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Animation Presentation", EditorStyles.boldLabel);
            DrawProfileSettings();
            EditorGUILayout.HelpBox(
                "Motion Matching is optional per Character Definition. Configure one Motion Matching Profile only when at least one producer uses Motion Matching; Timeline-only Definitions leave it empty.",
                MessageType.Info);

            DrawPresentationAssetSummary();
            DrawConfigurationErrors();
            DrawLinkedPoseBindings();
            DrawPoseSourceBindings();
            DrawContext();
            DrawProducerBindings();
        }

        void DrawProfileSettings()
        {
            CharacterAnimationPresentationProfile profile = Profile;
            if (!profile)
                return;
            CharacterPresentationPoseGraphAsset poseGraph = EditorGUILayout.ObjectField(
                "Pose Graph",
                profile.PoseGraph,
                typeof(CharacterPresentationPoseGraphAsset),
                false) as CharacterPresentationPoseGraphAsset;
            CharacterAnimationRigDefinition rig = EditorGUILayout.ObjectField(
                "Rig Definition",
                profile.RigDefinition,
                typeof(CharacterAnimationRigDefinition),
                false) as CharacterAnimationRigDefinition;
            CharacterMotionMatchingProfile motionMatching = EditorGUILayout.ObjectField(
                "Motion Matching Profile",
                profile.MotionMatchingProfile,
                typeof(CharacterMotionMatchingProfile),
                false) as CharacterMotionMatchingProfile;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Foot Analysis", EditorStyles.boldLabel);
            CharacterFootPlacementAnalysisMode mode =
                (CharacterFootPlacementAnalysisMode)EditorGUILayout.EnumPopup(
                    "Mode",
                    profile.FootPlacementAnalysisMode);
            CharacterFootPlacementAnalysisSource analysisSource = null;
            if (mode == CharacterFootPlacementAnalysisMode.Disabled)
            {
                analysisSource = null;
            }
            else
            {
                string currentGuid = profile.FootPlacementAnalysisSourceAssetGuid;
                CharacterFootPlacementAnalysisSource current =
                    CharacterFootPlacementAnalysisSource.IsAssetGuid(currentGuid)
                        ? AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(
                            AssetDatabase.GUIDToAssetPath(currentGuid))
                        : null;
                analysisSource = EditorGUILayout.ObjectField(
                    "Analysis Source",
                    current,
                    typeof(CharacterFootPlacementAnalysisSource),
                    false) as CharacterFootPlacementAnalysisSource;
                if (!analysisSource)
                    EditorGUILayout.HelpBox(
                        "Generated Foot Analysis requires an explicit Analysis Source asset.",
                        MessageType.Error);
            }
            EditorGUILayout.HelpBox(
                "Source validation only runs from the explicit profile validation command or a formal build.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(!analysisSource))
            {
                if (GUILayout.Button("Open Foot Analysis Source"))
                    OpenAsset(analysisSource);
            }

            string nextAnalysisGuid = analysisSource
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(analysisSource))
                : string.Empty;
            bool graphChanged =
                poseGraph != profile.PoseGraph || rig != profile.RigDefinition;
            bool motionMatchingChanged =
                motionMatching != profile.MotionMatchingProfile;
            bool footPlacementChanged =
                mode != profile.FootPlacementAnalysisMode ||
                !string.Equals(
                    nextAnalysisGuid,
                    profile.FootPlacementAnalysisSourceAssetGuid,
                    StringComparison.Ordinal);
            if (!graphChanged && !motionMatchingChanged && !footPlacementChanged)
                return;
            string path = AssetDatabase.GetAssetPath(profile);
            string profileId = string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(profileId))
            {
                m_BindingError =
                    "Presentation Profile must be a saved asset before formal mutation.";
                return;
            }
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Edit Animation Presentation Profile");
            if (graphChanged)
            {
                if (poseGraph && rig)
                    transaction.Add(new SetPresentationGraphMutation(profileId, poseGraph, rig));
                else
                    m_BindingError = "Pose Graph and Rig Definition must be assigned together.";
            }
            if (motionMatchingChanged)
                transaction.Add(new SetMotionMatchingProfileMutation(profileId, motionMatching));
            if (footPlacementChanged)
            {
                if (mode == CharacterFootPlacementAnalysisMode.Disabled ||
                    analysisSource)
                {
                    transaction.Add(new SetFootPlacementAnalysisMutation(
                        profileId,
                        mode,
                        nextAnalysisGuid));
                }
            }
            if (transaction.Mutations.Count == 0)
                return;
            try
            {
                new CharacterPresentationMutationService().Apply(
                    new CharacterPresentationProfileMutationOwner(
                        profile,
                        profileId),
                    transaction);
                m_BindingError = string.Empty;
                InvalidateDiagnostics();
                InvalidateProjection();
            }
            catch (Exception exception)
            {
                m_BindingError = exception.Message;
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
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!poseGraph))
            {
                if (GUILayout.Button("Open Pose Graph"))
                    CharacterPresentationPoseGraphEditorWindow.Open(profile);
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
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate Presentation Profile"))
                RefreshConfigurationDiagnostics();
            if (!m_ConfigurationDiagnosticsReady)
            {
                EditorGUILayout.HelpBox(
                    "Validation has not been run. Asset selection and Inspector repaint never validate the Profile.",
                    MessageType.Info);
                return;
            }

            if (m_ConfigurationErrors.Count == 0 &&
                m_MotionMatchingDiagnostics.Count == 0)
            {
                EditorGUILayout.HelpBox("Profile validation completed without errors.", MessageType.Info);
                return;
            }
            for (int i = 0; i < m_ConfigurationErrors.Count; i++)
                EditorGUILayout.HelpBox(m_ConfigurationErrors[i], MessageType.Error);
            for (int i = 0; i < m_MotionMatchingDiagnostics.Count; i++)
            {
                CharacterMotionMatchingAuthoringDiagnostic diagnostic =
                    m_MotionMatchingDiagnostics[i];
                EditorGUILayout.HelpBox(
                    $"{diagnostic.Code}: {diagnostic.Message}",
                    MessageType.Error);
            }
        }

        void RefreshConfigurationDiagnostics()
        {
            m_ConfigurationErrors.Clear();
            m_MotionMatchingDiagnostics.Clear();
            CharacterAnimationPresentationProfile profile = Profile;
            profile?.CollectConfigurationErrors(m_ConfigurationErrors);
            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationPresentationProfile");
            var profiles = new List<CharacterAnimationPresentationProfile>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterAnimationPresentationProfile candidate =
                    AssetDatabase.LoadAssetAtPath<CharacterAnimationPresentationProfile>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (candidate)
                    profiles.Add(candidate);
            }
            CharacterMotionMatchingAuthoringValidator.CollectPresentationOwnershipDiagnostics(
                Profile,
                profiles,
                m_MotionMatchingDiagnostics);
            m_ConfigurationDiagnosticsReady = true;
        }

        void DrawLinkedPoseBindings()
        {
            CharacterAnimationPresentationProfile profile = Profile;
            if (!profile)
                return;

            EditorGUILayout.Space(4f);
            m_ShowLinkedPoseBindings = EditorGUILayout.Foldout(
                m_ShowLinkedPoseBindings,
                $"Linked Pose Groups ({profile.LinkedPoseGroups.Count})",
                true);
            if (!m_ShowLinkedPoseBindings)
                return;

            EditorGUILayout.HelpBox(
                "Groups select one precompiled Implementation through one selector. Candidate closure is derived from the selector mappings and is not separately editable.",
                MessageType.Info);
            if (GUILayout.Button("Open Linked Pose in Animation Workspace"))
                CharacterLinkedPoseAuthoringService.OpenWorkspace(profile);
            if (profile.LinkedPoseGroups.Count == 0)
            {
                EditorGUILayout.HelpBox("This Profile has no Linked Pose Groups.", MessageType.Info);
                return;
            }

            for (int groupIndex = 0; groupIndex < profile.LinkedPoseGroups.Count; groupIndex++)
            {
                CharacterLinkedPoseGroupBinding group = profile.LinkedPoseGroups[groupIndex];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    group != null && group.Interface
                        ? group.Interface.name
                        : "Missing Linked Pose Group",
                    EditorStyles.boldLabel);
                if (group == null)
                {
                    EditorGUILayout.HelpBox("Group binding is missing.", MessageType.Error);
                    EditorGUILayout.EndVertical();
                    continue;
                }

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Interface", group.Interface, typeof(CharacterLinkedPoseInterfaceAsset), false);

                CharacterLinkedPoseSelectorBindingAsset selector =
                    FindLinkedPoseSelector(profile, group.GroupId, out int selectorCount);
                if (selectorCount != 1)
                {
                    EditorGUILayout.HelpBox(
                        selectorCount == 0
                            ? "This Group has no selector."
                            : $"This Group has {selectorCount} selectors; exactly one is required.",
                        MessageType.Error);
                    selector = null;
                }
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Selector", selector, typeof(CharacterLinkedPoseSelectorBindingAsset), false);

                if (selector is CharacterEquipmentLinkedPoseSelectionBinding equipment)
                    DrawEquipmentLinkedPoseSelector(profile, equipment);
                else if (selector)
                    DrawLinkedPoseCandidateClosure(profile, selector.CandidateImplementationIds);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Group in Animation Workspace"))
                    CharacterLinkedPoseAuthoringService.OpenWorkspace(profile);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space(6f);
        }

        static CharacterLinkedPoseSelectorBindingAsset FindLinkedPoseSelector(
            CharacterAnimationPresentationProfile profile,
            LinkedPoseGroupId groupId,
            out int count)
        {
            CharacterLinkedPoseSelectorBindingAsset result = null;
            count = 0;
            for (int i = 0; i < profile.LinkedPoseSelectors.Count; i++)
            {
                CharacterLinkedPoseSelectorBindingAsset selector = profile.LinkedPoseSelectors[i];
                if (!selector || selector.GroupId != groupId)
                    continue;
                count++;
                result = selector;
            }
            return result;
        }

        static CharacterLinkedPoseImplementationAsset FindLinkedPoseImplementation(
            CharacterAnimationPresentationProfile profile,
            LinkedPoseImplementationId implementationId)
        {
            for (int i = 0; i < profile.LinkedPoseImplementations.Count; i++)
            {
                CharacterLinkedPoseImplementationAsset implementation =
                    profile.LinkedPoseImplementations[i];
                if (implementation && implementation.ImplementationId == implementationId)
                    return implementation;
            }
            return null;
        }

        void DrawEquipmentLinkedPoseSelector(
            CharacterAnimationPresentationProfile profile,
            CharacterEquipmentLinkedPoseSelectionBinding selector)
        {
            EditorGUILayout.LabelField("Equipment Slot", selector.SlotId.ToString());
            CharacterLinkedPoseImplementationAsset empty =
                FindLinkedPoseImplementation(profile, selector.EmptyImplementationId);
            DrawLinkedPoseMapping("Empty Equipment", selector.EmptyImplementationId, empty);

            EditorGUILayout.LabelField($"Exact Equipment Mappings ({selector.Mappings.Count})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < selector.Mappings.Count; i++)
            {
                CharacterEquipmentLinkedPoseMapping mapping = selector.Mappings[i];
                if (mapping == null)
                {
                    EditorGUILayout.HelpBox($"Mapping #{i} is missing.", MessageType.Error);
                    continue;
                }
                DrawLinkedPoseMapping(
                    mapping.EquipmentId.ToString(),
                    mapping.ImplementationId,
                    FindLinkedPoseImplementation(profile, mapping.ImplementationId));
            }
            DrawLinkedPoseCandidateClosure(profile, selector.CandidateImplementationIds);
        }

        void DrawLinkedPoseMapping(
            string label,
            LinkedPoseImplementationId implementationId,
            CharacterLinkedPoseImplementationAsset implementation)
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(label, implementation, typeof(CharacterLinkedPoseImplementationAsset), false);
            using (new EditorGUI.DisabledScope(!implementation))
            {
                if (GUILayout.Button("Workspace", GUILayout.Width(78f)))
                    CharacterLinkedPoseAuthoringService.OpenWorkspace(implementation);
            }
            EditorGUILayout.EndHorizontal();
            if (!implementation)
                EditorGUILayout.HelpBox($"Implementation '{implementationId}' is not in this Profile.", MessageType.Error);
        }

        void DrawLinkedPoseCandidateClosure(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<LinkedPoseImplementationId> candidates)
        {
            EditorGUILayout.LabelField($"Derived Candidate Closure ({candidates.Count})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < candidates.Count; i++)
            {
                LinkedPoseImplementationId candidateId = candidates[i];
                CharacterLinkedPoseImplementationAsset implementation =
                    FindLinkedPoseImplementation(profile, candidateId);
                DrawLinkedPoseMapping($"Candidate {i + 1}", candidateId, implementation);
            }
        }

        void DrawPoseSourceBindings()
        {
            CharacterAnimationPresentationProfile profile = Profile;
            if (!profile)
                return;

            EditorGUILayout.Space(4f);
            m_ShowPoseSourceBindings = EditorGUILayout.Foldout(
                m_ShowPoseSourceBindings,
                $"Continuous Pose Sources ({profile.PoseSourceBindings.Count})",
                true);
            if (!m_ShowPoseSourceBindings)
                return;

            EditorGUILayout.HelpBox(
                "Pose Graph owns readable Source Slots. This Profile owns one typed resource binding for every Slot.",
                MessageType.Info);
            if (!string.IsNullOrEmpty(m_PoseSourceError))
                EditorGUILayout.HelpBox(m_PoseSourceError, MessageType.Error);

            if (!profile.PoseGraph)
            {
                EditorGUILayout.HelpBox("Assign a Pose Graph before creating Pose sources.", MessageType.Warning);
                return;
            }
            for (int i = 0; i < profile.PoseGraph.SourceSlots.Count; i++)
                DrawPoseSourceBinding(profile, profile.PoseGraph.SourceSlots[i]);
            DrawNewPoseSource(profile);
            EditorGUILayout.Space(6f);
        }

        void DrawPoseSourceBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceSlot slot)
        {
            CharacterPresentationPoseSourceBinding binding =
                profile.FindPoseSourceBinding(slot);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(slot ? slot.name : "Missing Source Slot", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!slot))
            {
                string renamed = EditorGUILayout.DelayedTextField("Display Name", slot ? slot.name : string.Empty);
                if (slot && !string.Equals(renamed, slot.name, StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(renamed))
                    CharacterAnimationPresentationAuthoringService.RenamePoseSourceSlot(profile.PoseGraph, slot, renamed);
            }
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Source Slot", slot, slot ? slot.GetType() : typeof(CharacterPresentationPoseSourceSlot), false);
                EditorGUILayout.EnumPopup("Source Type", slot ? slot.SourceKind : default);
                EditorGUILayout.ObjectField("Profile Binding", binding, binding ? binding.GetType() : typeof(CharacterPresentationPoseSourceBinding), false);
                EditorGUILayout.ObjectField("Resource", binding ? binding.SourceAsset : null, typeof(UnityEngine.Object), false);
                EditorGUILayout.ObjectField("Rig", binding ? binding.Rig : null, typeof(CharacterAnimationRigDefinition), false);
            }
            if (!binding)
                EditorGUILayout.HelpBox("This Source Slot has no Profile binding.", MessageType.Error);
            else if (binding is CharacterSequencePoseSourceBinding sequence)
            {
                EditorGUILayout.LabelField("Duration", sequence.Clip ? $"{sequence.Clip.length:0.###} s" : "Unavailable");
                EditorGUILayout.LabelField("Loop", sequence.Loop ? "Yes" : "No");
                EditorGUILayout.LabelField("Markers", sequence.Sequence.SyncMarkers.Count.ToString());
                EditorGUILayout.LabelField("Time Mapping", sequence.Sequence.TimeMapping.ToString());
            }
            DrawPoseSourceConsumers(profile, slot);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!binding || !binding.SourceAsset))
            {
                if (GUILayout.Button("Ping Source"))
                    EditorGUIUtility.PingObject(binding.SourceAsset);
                if (GUILayout.Button("Open Source"))
                {
                    if (binding is CharacterSequencePoseSourceBinding sequence && sequence.Sequence)
                        TimelineEditorWindow.Open(sequence.Sequence);
                    else if (binding is CharacterBlendSpacePoseSourceBinding blendSpace && blendSpace.BlendSpace)
                        CharacterAnimationBlendSpaceEditorWindow.Open(blendSpace.BlendSpace);
                }
            }
            if (GUILayout.Button("Open Binding"))
                OpenAsset(binding);
            if (GUILayout.Button("Delete"))
                TryDeletePoseSource(profile, slot);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void DrawPoseSourceConsumers(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceSlot slot)
        {
            if (GUILayout.Button("Refresh PoseState Consumers"))
            {
                var consumers = new List<PoseSourceConsumer>();
                CharacterPresentationPoseGraphAsset owner = profile.PoseGraph;
                if (owner && owner.Graph != null)
                {
                    CollectPoseSourceConsumers(
                        owner,
                        owner.Graph,
                        slot,
                        consumers,
                        new HashSet<PoseGraphId>());
                }
                m_PoseSourceConsumers[slot] = consumers;
            }
            if (!m_PoseSourceConsumers.TryGetValue(
                    slot,
                    out List<PoseSourceConsumer> cached))
            {
                EditorGUILayout.LabelField("PoseState Consumers", "Not inspected");
                return;
            }
            EditorGUILayout.LabelField("PoseState Consumers", cached.Count.ToString());
            for (int i = 0; i < cached.Count; i++)
            {
                PoseSourceConsumer consumer = cached[i];
                string machineName = string.IsNullOrWhiteSpace(consumer.Machine.DisplayName)
                    ? consumer.Machine.PoseStateMachine.StateMachineId.Value
                    : consumer.Machine.DisplayName;
                string stateName = string.IsNullOrWhiteSpace(consumer.State.DisplayName)
                    ? consumer.State.StateId.Value
                    : consumer.State.DisplayName;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{machineName} / {stateName} / {consumer.Sequence.NodeId}");
                if (GUILayout.Button("Open PoseState", GUILayout.Width(110f)))
                    OpenPoseSourceConsumer(profile, consumer);
                EditorGUILayout.EndHorizontal();
            }
            if (cached.Count == 0)
                EditorGUILayout.HelpBox("No PoseState SequencePlayer currently consumes this source.", MessageType.Warning);
        }

        void DrawNewPoseSource(CharacterAnimationPresentationProfile profile)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Create Pose Source", EditorStyles.boldLabel);
            m_NewPoseSourceName = EditorGUILayout.TextField("Display Name", m_NewPoseSourceName);
            m_NewPoseSourceKind =
                (PresentationPoseSourceKind)EditorGUILayout.EnumPopup(
                    "Source Type",
                    m_NewPoseSourceKind);
            bool canCreate = !string.IsNullOrWhiteSpace(m_NewPoseSourceName);
            if (m_NewPoseSourceKind == PresentationPoseSourceKind.Sequence)
            {
                m_NewPoseSourceSequence = EditorGUILayout.ObjectField(
                    "Sequence",
                    m_NewPoseSourceSequence,
                    typeof(CharacterAnimationSequenceAsset),
                    false) as CharacterAnimationSequenceAsset;
                canCreate &= m_NewPoseSourceSequence;
            }
            else if (m_NewPoseSourceKind == PresentationPoseSourceKind.BlendSpace)
            {
                m_NewPoseSourceBlendSpace = EditorGUILayout.ObjectField(
                    "Blend Space",
                    m_NewPoseSourceBlendSpace,
                    typeof(CharacterAnimationBlendSpaceAsset),
                    false) as CharacterAnimationBlendSpaceAsset;
                canCreate &= m_NewPoseSourceBlendSpace;
            }
            else
            {
                m_NewPoseSourceMotionMatching = EditorGUILayout.ObjectField(
                    "Motion Matching Profile",
                    m_NewPoseSourceMotionMatching,
                    typeof(CharacterMotionMatchingProfile),
                    false) as CharacterMotionMatchingProfile;
                List<CharacterMotionMatchingSearchDomainId> domains =
                    CollectSearchDomains(m_NewPoseSourceMotionMatching);
                string[] labels = new string[domains.Count];
                for (int i = 0; i < domains.Count; i++)
                    labels[i] = domains[i].Value;
                m_NewMotionMatchingDomainIndex = domains.Count == 0
                    ? 0
                    : EditorGUILayout.Popup(
                        "Search Domain",
                        Mathf.Clamp(m_NewMotionMatchingDomainIndex, 0, domains.Count - 1),
                        labels);
                canCreate &= m_NewPoseSourceMotionMatching && domains.Count > 0;
            }
            using (new EditorGUI.DisabledScope(!canCreate))
            {
                if (GUILayout.Button("Add Pose Source"))
                {
                    try
                    {
                        if (m_NewPoseSourceKind == PresentationPoseSourceKind.Sequence)
                        {
                            CharacterAnimationPresentationAuthoringService
                                .CreateSequencePoseSource(
                                    profile,
                                    m_NewPoseSourceName,
                                    m_NewPoseSourceSequence);
                        }
                        else if (m_NewPoseSourceKind == PresentationPoseSourceKind.BlendSpace)
                        {
                            CharacterAnimationPresentationAuthoringService
                                .CreateBlendSpacePoseSource(
                                    profile,
                                    m_NewPoseSourceName,
                                    m_NewPoseSourceBlendSpace);
                        }
                        else
                        {
                            List<CharacterMotionMatchingSearchDomainId> domains =
                                CollectSearchDomains(m_NewPoseSourceMotionMatching);
                            CharacterMotionMatchingSearchDomainId domain =
                                domains[m_NewMotionMatchingDomainIndex];
                            var databases = new List<CharacterMotionMatchingDatabaseDefinition>();
                            for (int i = 0; i < m_NewPoseSourceMotionMatching.Databases.Count; i++)
                            {
                                CharacterMotionMatchingDatabaseDefinition database =
                                    m_NewPoseSourceMotionMatching.Databases[i];
                                if (database && database.SearchDomainId.Equals(domain))
                                    databases.Add(database);
                            }
                            CharacterAnimationPresentationAuthoringService
                                .CreateMotionMatchingPoseSource(
                                    profile,
                                    m_NewPoseSourceName,
                                    m_NewPoseSourceMotionMatching,
                                    domain,
                                    databases.ToArray());
                        }
                        m_NewPoseSourceName = string.Empty;
                        m_NewPoseSourceSequence = null;
                        m_NewPoseSourceBlendSpace = null;
                        m_NewPoseSourceMotionMatching = null;
                        m_PoseSourceError = string.Empty;
                        InvalidateDiagnostics();
                        InvalidateProjection();
                    }
                    catch (Exception exception)
                    {
                        m_PoseSourceError = exception.Message;
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        static List<CharacterMotionMatchingSearchDomainId> CollectSearchDomains(
            CharacterMotionMatchingProfile profile)
        {
            var result = new List<CharacterMotionMatchingSearchDomainId>();
            if (!profile)
                return result;
            for (int i = 0; i < profile.Databases.Count; i++)
            {
                CharacterMotionMatchingDatabaseDefinition database = profile.Databases[i];
                if (!database || !database.SearchDomainId.IsValid || result.Contains(database.SearchDomainId))
                    continue;
                result.Add(database.SearchDomainId);
            }
            result.Sort();
            return result;
        }

        void TryDeletePoseSource(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceSlot slot)
        {
            try
            {
                CharacterAnimationPresentationAuthoringService.DeletePoseSource(profile, slot);
                m_PoseSourceConsumers.Remove(slot);
                m_PoseSourceError = string.Empty;
                InvalidateDiagnostics();
                InvalidateProjection();
            }
            catch (Exception exception)
            {
                m_PoseSourceError = exception.Message;
            }
        }

        static void CollectPoseSourceConsumers(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            CharacterPresentationPoseSourceSlot slot,
            List<PoseSourceConsumer> consumers,
            HashSet<PoseGraphId> visited)
        {
            if (graph == null || !visited.Add(graph.GraphId))
                return;
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                CharacterTypedPoseNode machine = graph.Nodes[nodeIndex];
                if (machine?.Kind == CharacterPoseNodeKind.PoseSubgraph &&
                    machine.Subgraph?.PoseGraphId.IsValid == true)
                {
                    CollectPoseSourceConsumers(
                        owner,
                        owner.RequireGraph(machine.Subgraph.PoseGraphId),
                        slot,
                        consumers,
                        visited);
                    continue;
                }
                if (machine?.Kind != CharacterPoseNodeKind.PoseStateMachine ||
                    machine.PoseStateMachine == null)
                {
                    continue;
                }
                for (int stateIndex = 0; stateIndex < machine.PoseStateMachine.States.Count; stateIndex++)
                {
                    CharacterPoseStateDefinition state = machine.PoseStateMachine.States[stateIndex];
                    if (state == null ||
                        !owner.TryGetGraph(
                            state.PoseGraphId,
                            out CharacterTypedPoseGraph stateGraph))
                        continue;
                    for (int sequenceIndex = 0; sequenceIndex < stateGraph.Nodes.Count; sequenceIndex++)
                    {
                        CharacterTypedPoseNode sequence = stateGraph.Nodes[sequenceIndex];
                        if ((sequence?.Kind == CharacterPoseNodeKind.SequencePlayer ||
                             sequence?.Kind == CharacterPoseNodeKind.BlendSpacePlayer) &&
                            sequence.PresentationPoseSourceSlot == slot)
                        {
                            consumers.Add(new PoseSourceConsumer(machine, state, sequence));
                        }
                    }
                }
            }
        }

        void OpenPoseSourceConsumer(
            CharacterAnimationPresentationProfile profile,
            PoseSourceConsumer consumer)
        {
            CharacterPresentationPoseGraphEditorWindow window =
                CharacterPresentationPoseGraphEditorWindow.Open(profile);
            window.FocusStateSequence(consumer.Machine, consumer.State, consumer.Sequence.NodeId);
        }

        static CharacterFootPlacementAnalysisSource ResolveAnalysisSource(
            CharacterAnimationPresentationProfile profile)
        {
            if (!profile ||
                !CharacterFootPlacementAnalysisSource.IsAssetGuid(
                    profile.FootPlacementAnalysisSourceAssetGuid))
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(
                AssetDatabase.GUIDToAssetPath(profile.FootPlacementAnalysisSourceAssetGuid));
        }

        void DrawContext()
        {
            EditorGUILayout.LabelField("Definition Context", EditorStyles.boldLabel);
            if (!m_ContextsLoaded)
            {
                EditorGUILayout.HelpBox(
                    "Definition contexts have not been scanned. Asset selection never scans project assets.",
                    MessageType.Info);
            }
            else if (m_Contexts.Count == 0)
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
            CharacterPipelineDefinition context = SelectedContext;
            using (new EditorGUI.DisabledScope(!context))
            {
                if (GUILayout.Button("Build Presentation Projection"))
                {
                    try
                    {
                        bool success = CharacterSimulationProgramBuildService.Build(context, true);
                        m_BuildMessage = success
                            ? "Build completed and published."
                            : "Build failed. Inspect the formal compile report.";
                    }
                    catch (Exception exception)
                    {
                        m_BuildMessage = $"Build failed: {exception.Message}";
                    }
                }
            }
            if (!string.IsNullOrEmpty(m_BuildMessage))
                EditorGUILayout.HelpBox(m_BuildMessage, MessageType.Info);
            EditorGUILayout.Space(6f);
        }

        void DrawProducerBindings()
        {
            CharacterPipelineDefinition context = SelectedContext;
            if (!context)
                return;

            EditorGUILayout.LabelField("Finite Action Timeline Producers", EditorStyles.boldLabel);
            if (m_InspectedContext != context)
            {
                EditorGUILayout.HelpBox(
                    "Producer topology has not been inspected. Inspector repaint never traverses BTSMTL Graph or Timeline authoring.",
                    MessageType.Info);
                if (GUILayout.Button("Inspect Finite Action Timeline Producers"))
                    InspectAuthoring(context);
                return;
            }
            if (!string.IsNullOrEmpty(m_ProducerInspectionError))
            {
                EditorGUILayout.HelpBox(m_ProducerInspectionError, MessageType.Error);
                if (GUILayout.Button("Inspect Finite Action Timeline Producers Again"))
                    InspectAuthoring(context);
                return;
            }

            m_ShowProducerBindings = EditorGUILayout.Foldout(
                m_ShowProducerBindings,
                $"Finite Action Timeline Producers ({m_AnimationProducers.Count})",
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

        void InspectAuthoring(CharacterPipelineDefinition context)
        {
            InvalidateProjection();
            m_InspectedContext = context;
            try
            {
                IReadOnlyList<AnimationProducerAuthoringEntry> producers =
                    CharacterAnimationPresentationAuthoringService.DiscoverProducers(Profile, context);
                for (int i = 0; i < producers.Count; i++)
                    m_AnimationProducers.Add(producers[i]);
                m_ProducerInspectionError = string.Empty;
            }
            catch (Exception exception)
            {
                m_ProducerInspectionError = exception.Message;
            }
        }

        void DrawProducerBinding(
            CharacterPipelineDefinition context,
            AnimationProducerAuthoringEntry producer)
        {
            CharacterAnimationPresentationProfile profile = Profile;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(producer.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Animation Channel", producer.AnimationChannelId.Value);
            EditorGUILayout.LabelField("Source Clips", producer.SourceClips.Count.ToString());
            for (int clipIndex = 0; clipIndex < producer.SourceClips.Count; clipIndex++)
            {
                AnimationProducerSourceClipAuthoringEntry sourceClip = producer.SourceClips[clipIndex];
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Animation Clip", sourceClip.Clip, typeof(UnityEngine.AnimationClip), false);
                if (GUILayout.Button("Open Action Clip Analysis", GUILayout.Width(170f)))
                    OpenTimeline(context, producer, sourceClip.ClipAuthoringId);
                EditorGUILayout.EndHorizontal();
            }

            AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producer.ProducerId);
            EditorGUILayout.LabelField(
                "Presentation Binding",
                binding == null ? "Unbound" : "Action Timeline");

            TransitionAssetBase currentSource = binding?.Source;
            TransitionAssetBase sourceAsset = (TransitionAssetBase)EditorGUILayout.ObjectField(
                "Action Timeline Source",
                currentSource,
                typeof(TransitionAssetBase),
                false);
            if (sourceAsset != currentSource)
            {
                try
                {
                    if (sourceAsset)
                        CharacterAnimationPresentationAuthoringService.ConfigureTimelineProducerBinding(profile, context, producer.ProducerId, sourceAsset);
                    else if (binding != null)
                        CharacterAnimationPresentationAuthoringService.RemoveProducerBinding(profile, context, producer.ProducerId);
                    m_BindingError = string.Empty;
                    InvalidateDiagnostics();
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
            if (GUILayout.Button("Open Action Timeline"))
                OpenTimeline(context, producer);
            if (GUILayout.Button("Open Action Curves / Analysis"))
                OpenTimeline(context, producer);
            UnityEngine.Object sourceObject = binding?.Source;
            using (new EditorGUI.DisabledScope(!sourceObject))
            {
                if (GUILayout.Button("Open Source"))
                {
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

            m_ContextsLoaded = true;
            m_SelectedContextIndex = m_Contexts.Count == 1 ? 0 : m_Contexts.IndexOf(previous);
            InvalidateProjection();
        }

        void InvalidateDiagnostics()
        {
            m_ConfigurationDiagnosticsReady = false;
            m_ConfigurationErrors.Clear();
            m_MotionMatchingDiagnostics.Clear();
            m_PoseSourceConsumers.Clear();
        }

        void InvalidateProjection()
        {
            m_InspectedContext = null;
            m_AnimationProducers.Clear();
            m_ProducerInspectionError = string.Empty;
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
            AnimationProducerAuthoringEntry producer,
            string clipAuthoringId = "")
        {
            BaseTreeWindow graphWindow = OpenGraph(definition, producer.Timeline);
            TimelineEditorWindow.Open(graphWindow, producer.Timeline.Node)?.FocusSource(
                producer.ProducerId.TrackAuthoringId,
                clipAuthoringId);
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

    sealed class CharacterPoseSourceAnalysisWindow : EditorWindow
    {
        CharacterAnimationPresentationProfile m_Profile;
        CharacterPresentationPoseSourceSlot m_SourceSlot;
        CharacterFootAnalysisArtifactDiagnostic m_Diagnostic;
        bool m_HasDiagnostic;

        public static void Open(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceSlot sourceSlot)
        {
            if (!profile || !sourceSlot)
                return;
            CharacterPoseSourceAnalysisWindow window =
                GetWindow<CharacterPoseSourceAnalysisWindow>();
            window.titleContent = new GUIContent("Pose Source Analysis");
            window.m_Profile = profile;
            window.m_SourceSlot = sourceSlot;
            window.m_HasDiagnostic = false;
            window.Show();
            window.Focus();
        }

        void OnGUI()
        {
            if (!m_Profile || !m_SourceSlot)
            {
                EditorGUILayout.HelpBox("Pose source context is unavailable.", MessageType.Error);
                return;
            }
            CharacterPresentationPoseSourceBinding binding =
                m_Profile.FindPoseSourceBinding(m_SourceSlot);
            if (!binding)
            {
                EditorGUILayout.HelpBox($"Pose source '{m_SourceSlot.name}' no longer exists.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField(m_SourceSlot.name, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Source Kind", binding.SourceKind);
                EditorGUILayout.ObjectField(
                    "Resource",
                    binding.SourceAsset,
                    binding.SourceAsset ? binding.SourceAsset.GetType() : typeof(UnityEngine.Object),
                    false);
                EditorGUILayout.ObjectField(
                    "Rig",
                    binding.Rig,
                    typeof(CharacterAnimationRigDefinition),
                    false);
            }

            if (GUILayout.Button("Inspect Foot Analysis Artifact"))
            {
                m_Diagnostic =
                    CharacterProjectionFootAnalysisResolver.InspectPoseSource(
                        m_Profile,
                        binding);
                m_HasDiagnostic = true;
            }
            if (m_HasDiagnostic)
            {
                EditorGUILayout.LabelField("Artifact Key", m_Diagnostic.BindingKey);
                EditorGUILayout.HelpBox(
                    $"{m_Diagnostic.Status}: {m_Diagnostic.Message}",
                    m_Diagnostic.Status == AnimationFootAnalysisArtifactStatus.Ready
                        ? MessageType.Info
                        : m_Diagnostic.Status == AnimationFootAnalysisArtifactStatus.Stale
                            ? MessageType.Warning
                            : MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Artifact status has not been inspected.",
                    MessageType.Info);
            }

            CharacterFootPlacementAnalysisSource analysisSource = null;
            if (CharacterFootPlacementAnalysisSource.IsAssetGuid(
                    m_Profile.FootPlacementAnalysisSourceAssetGuid))
            {
                analysisSource = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(
                    AssetDatabase.GUIDToAssetPath(
                        m_Profile.FootPlacementAnalysisSourceAssetGuid));
            }
            EditorGUILayout.BeginHorizontal();
            UnityEngine.Object sourceAsset = binding.SourceAsset;
            using (new EditorGUI.DisabledScope(!sourceAsset))
            {
                if (GUILayout.Button("Locate Pose Source"))
                {
                    Selection.activeObject = sourceAsset;
                    EditorGUIUtility.PingObject(sourceAsset);
                }
            }
            using (new EditorGUI.DisabledScope(!analysisSource))
            {
                if (GUILayout.Button("Open Analysis Source"))
                {
                    Selection.activeObject = analysisSource;
                    EditorGUIUtility.PingObject(analysisSource);
                    AssetDatabase.OpenAsset(analysisSource);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
