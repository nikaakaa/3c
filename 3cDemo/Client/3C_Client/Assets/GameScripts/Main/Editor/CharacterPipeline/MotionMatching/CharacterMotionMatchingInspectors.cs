using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    [CustomEditor(typeof(CharacterMotionMatchingProfile))]
    public sealed class CharacterMotionMatchingProfileInspector : UnityEditor.Editor
    {
        bool m_ShowDiagnostics;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawSection("Feature And Policies", "m_FeatureSchema", "m_TrajectoryPolicy", "m_CostProfile", "m_SearchPolicy");
            DrawSection("Databases", "m_Databases");
            m_ShowDiagnostics = EditorGUILayout.Foldout(m_ShowDiagnostics, "Diagnostics", true);
            if (m_ShowDiagnostics)
            {
                using (new EditorGUI.DisabledScope(true))
                    DrawSection("Machine Identity", "m_Schema", "m_ProfileId", "m_Revision");
            }
            serializedObject.ApplyModifiedProperties();
            MotionMatchingSourceClipInspectionGui.DrawProfile((CharacterMotionMatchingProfile)target);
            if (GUILayout.Button("Validate Motion Matching Profile"))
                Run(() => CharacterMotionMatchingAuthoringValidator.RequireProfile((CharacterMotionMatchingProfile)target));
        }

        void DrawSection(string title, params string[] properties)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            for (int i = 0; i < properties.Length; i++)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(properties[i]), true);
        }

        static void Run(Action action)
        {
            try
            {
                action();
                EditorUtility.DisplayDialog("Motion Matching", "Profile validation succeeded.", "OK");
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Motion Matching Validation Failed", exception.Message, "OK");
            }
        }
    }

    [CustomEditor(typeof(CharacterMotionMatchingSourceSet))]
    public sealed class CharacterMotionMatchingSourceSetInspector : UnityEditor.Editor
    {
        CharacterFootPlacementAnalysisSource m_AnalysisSource;
        AnimationClip m_ClipToRegister;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clip Registration", EditorStyles.boldLabel);
            m_ClipToRegister = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", m_ClipToRegister, typeof(AnimationClip), false);
            using (new EditorGUI.DisabledScope(!m_ClipToRegister))
            {
                if (GUILayout.Button("Register Animation Clip"))
                    RegisterClips(new[] { m_ClipToRegister });
            }
            using (new EditorGUI.DisabledScope(Selection.objects.OfType<AnimationClip>().Any() == false))
            {
                if (GUILayout.Button("Register Selected Animation Clips"))
                    RegisterClips(Selection.objects.OfType<AnimationClip>());
            }
            CharacterMotionMatchingSourceSet sourceSet = (CharacterMotionMatchingSourceSet)target;
            bool sourceClipsReady = MotionMatchingSourceClipInspectionGui.DrawSourceSet(sourceSet);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Foot Analysis", EditorStyles.boldLabel);
            m_AnalysisSource = (CharacterFootPlacementAnalysisSource)EditorGUILayout.ObjectField(
                "Analysis Source", m_AnalysisSource, typeof(CharacterFootPlacementAnalysisSource), false);
            if (!sourceClipsReady && MotionMatchingSourceClipInspectionGui.TryGetFirstFailure(sourceSet, out string sourceFailure))
                EditorGUILayout.HelpBox($"Formal Build disabled: {sourceFailure}", MessageType.Error);
            using (new EditorGUI.DisabledScope(!m_AnalysisSource || !sourceClipsReady || MotionMatchingSourceSetFootAnalysisBuildJob.Active != null))
            {
                if (GUILayout.Button("Build Source Set Foot Analysis"))
                    StartFootBuild();
            }
        }

        void RegisterClips(IEnumerable<AnimationClip> clips)
        {
            CharacterMotionMatchingSourceSet sourceSet = (CharacterMotionMatchingSourceSet)target;
            var existing = new HashSet<string>(sourceSet.SourceClips.Select(value =>
                value.AnimationClipAssetGuid + ":" + value.AnimationClipLocalFileId.ToString(CultureInfo.InvariantCulture)), StringComparer.Ordinal);
            serializedObject.Update();
            SerializedProperty entries = serializedObject.FindProperty("m_SourceClips");
            int added = 0;
            foreach (AnimationClip clip in clips.Where(value => value).Distinct().OrderBy(AssetDatabase.GetAssetPath).ThenBy(value => value.name, StringComparer.Ordinal))
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string guid, out long localId) || string.IsNullOrEmpty(guid) || localId == 0)
                    throw new InvalidOperationException($"Animation Clip '{clip.name}' is not a persisted asset with a stable local file id.");
                string assetKey = guid + ":" + localId.ToString(CultureInfo.InvariantCulture);
                if (!existing.Add(assetKey))
                    continue;
                int index = entries.arraySize++;
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("m_SourceClipId").stringValue = "clip." + guid + "." + localId.ToString(CultureInfo.InvariantCulture);
                entry.FindPropertyRelative("m_AnimationClipAssetGuid").stringValue = guid;
                entry.FindPropertyRelative("m_AnimationClipLocalFileId").longValue = localId;
                added++;
            }
            if (added > 0)
            {
                SerializedProperty revision = serializedObject.FindProperty("m_Revision");
                revision.intValue = Math.Max(1, revision.intValue + 1);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(sourceSet);
            }
        }

        void StartFootBuild()
        {
            try
            {
                MotionMatchingSourceSetFootAnalysisBuildRequest request = MotionMatchingSourceSetFootAnalysisBuildRequest.Create(
                    (CharacterMotionMatchingSourceSet)target, m_AnalysisSource);
                string rigPath = AssetDatabase.GUIDToAssetPath(m_AnalysisSource.SamplingRigAssetGuid);
                string summary =
                    $"Analysis Source: {m_AnalysisSource.name}\n" +
                    $"Sampling Rig: {rigPath}\n" +
                    $"Clip Count: {((CharacterMotionMatchingSourceSet)target).SourceClips.Count}\n" +
                    $"Ready: {request.ReadyCount}\nMissing: {request.MissingCount}\nStale: {request.StaleCount}\n" +
                    $"Estimated Samples: {request.EstimatedSampleCount}";
                if (!EditorUtility.DisplayDialog("Build Source Set Foot Analysis", summary, "Build", "Cancel"))
                    return;
                MotionMatchingSourceSetFootAnalysisBuildJob job = MotionMatchingSourceSetFootAnalysisBuildJob.Start(request);
                job.Finished += completed =>
                {
                    if (completed.Failure != null)
                        Debug.LogError($"Source Set Foot Analysis failed: {completed.Failure.Message}", target);
                    else if (!completed.IsCanceled)
                        Debug.Log($"Source Set Foot Analysis completed for '{target.name}'.", target);
                };
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Foot Analysis Preflight Failed", exception.Message, "OK");
            }
        }
    }

    [CustomEditor(typeof(CharacterMotionMatchingDatabaseDefinition))]
    public sealed class CharacterMotionMatchingDatabaseInspector : UnityEditor.Editor
    {
        CharacterMotionMatchingProfile m_Profile;
        CharacterFootPlacementAnalysisSource m_AnalysisSource;
        CharacterPipelineDefinition m_ReplayDefinition;
        TextAsset m_SearchReplayArtifact;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
            CharacterMotionMatchingDatabaseDefinition database = (CharacterMotionMatchingDatabaseDefinition)target;
            DrawSourceSetOwners(database);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Artifact Build Context", EditorStyles.boldLabel);
            m_Profile = (CharacterMotionMatchingProfile)EditorGUILayout.ObjectField(
                "Motion Matching Profile", m_Profile, typeof(CharacterMotionMatchingProfile), false);
            m_AnalysisSource = (CharacterFootPlacementAnalysisSource)EditorGUILayout.ObjectField(
                "Foot Analysis Source", m_AnalysisSource, typeof(CharacterFootPlacementAnalysisSource), false);
            bool sourceClipsReady = MotionMatchingSourceClipInspectionGui.AreAllReady(database.SourceSets, out string sourceFailure);
            if (!sourceClipsReady)
                EditorGUILayout.HelpBox($"Formal Build disabled: {sourceFailure}", MessageType.Error);
            using (new EditorGUI.DisabledScope(!m_Profile || !m_AnalysisSource || !sourceClipsReady || MotionMatchingDatabaseBuildJob.Active != null))
            {
                if (GUILayout.Button("Build Motion Matching Database"))
                    StartDatabaseBuild();
            }
            DrawArtifactStatus();
            DrawSearchReplay();
        }

        void DrawSearchReplay()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Search Replay", EditorStyles.boldLabel);
            m_ReplayDefinition = (CharacterPipelineDefinition)EditorGUILayout.ObjectField(
                "Character Definition", m_ReplayDefinition, typeof(CharacterPipelineDefinition), false);
            m_SearchReplayArtifact = (TextAsset)EditorGUILayout.ObjectField(
                "Search Replay Artifact", m_SearchReplayArtifact, typeof(TextAsset), false);
            EditorGUILayout.HelpBox(
                "Replay uses the selected Definition's compiled Projection and the formal Runtime Database/Search/Plan path. Exact Projection and Database identity are required.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(!m_ReplayDefinition || !m_SearchReplayArtifact))
            {
                if (GUILayout.Button("Replay Motion Matching Search"))
                    ReplaySearch();
            }
        }

        void ReplaySearch()
        {
            try
            {
                MotionMatchingSearchReplayArtifact artifact =
                    MotionMatchingSearchReplayArtifactCodec.Decode(m_SearchReplayArtifact.bytes);
                if (!m_ReplayDefinition.SimulationProgram || !m_ReplayDefinition.PresentationProjection)
                    throw new InvalidOperationException("Selected Character Definition has no compiled Program or Presentation Projection.");
                CharacterSimulationProgram program = m_ReplayDefinition.SimulationProgram.Load();
                CharacterPresentationProjection projection = m_ReplayDefinition.PresentationProjection.Load(
                    Float32CharacterPresentationContractAdapter.Create(program));
                string projectionIdentity = $"{projection.ProgramId}@{projection.SourceRevision}:{projection.ContractHash}";
                if (!string.Equals(projectionIdentity, artifact.ProjectionIdentity, StringComparison.Ordinal))
                    throw new InvalidOperationException("Search Replay Projection identity does not match the selected Character Definition.");
                MotionMatchingProjectionPayload motionMatching = projection.MotionMatching ??
                    throw new InvalidOperationException("Selected Character Definition has no Motion Matching payload.");
                int databaseIndex = -1;
                for (int i = 0; i < motionMatching.DatabaseCount; i++)
                {
                    if (!motionMatching.GetDatabase(i).ArtifactIdentity.EqualsExact(artifact.DatabaseIdentity))
                        continue;
                    databaseIndex = i;
                    break;
                }
                if (databaseIndex < 0)
                    throw new InvalidOperationException("Search Replay Database Artifact identity is absent from the selected Projection.");
                using var database = new CharacterMotionMatchingRuntimeDatabase(motionMatching, databaseIndex);
                var runner = new MotionMatchingSearchReplayRunner(
                    projectionIdentity,
                    database,
                    motionMatching.TrajectoryPolicy.PointCount);
                MotionMatchingSearchReplayResult result = runner.Replay(artifact);
                EditorUtility.DisplayDialog(
                    result.Matches ? "Motion Matching Search Replay Matched" : "Motion Matching Search Replay Mismatch",
                    $"Result: {result.Failure}\nExpected: {result.ExpectedDigest}\nActual: {result.ActualDigest}",
                    "OK");
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Motion Matching Search Replay Failed", exception.Message, "OK");
            }
        }

        static void DrawSourceSetOwners(CharacterMotionMatchingDatabaseDefinition database)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Source Set Owners", EditorStyles.boldLabel);
            for (int i = 0; i < database.SourceSets.Count; i++)
            {
                CharacterMotionMatchingSourceSet sourceSet = database.SourceSets[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField($"Source Set {i + 1}", sourceSet, typeof(CharacterMotionMatchingSourceSet), false);
                    using (new EditorGUI.DisabledScope(!sourceSet))
                    {
                        if (GUILayout.Button("Open Owner", GUILayout.Width(90f)))
                        {
                            Selection.activeObject = sourceSet;
                            EditorGUIUtility.PingObject(sourceSet);
                        }
                    }
                }
            }
        }

        void DrawArtifactStatus()
        {
            if (!m_Profile || !m_AnalysisSource)
            {
                EditorGUILayout.HelpBox("Select the formal Profile and Foot Analysis Source to inspect exact Artifact status.", MessageType.Info);
                return;
            }
            try
            {
                MotionMatchingDatabaseBuildRequest request = MotionMatchingDatabaseBuildRequestFactory.Create(
                    m_Profile, (CharacterMotionMatchingDatabaseDefinition)target, m_AnalysisSource);
                CharacterMotionMatchingArtifactInspection inspection = CharacterMotionMatchingDatabaseArtifactStore.Inspect(
                    (CharacterMotionMatchingDatabaseDefinition)target, request.ExpectedIdentity);
                MessageType type = inspection.Status == CharacterMotionMatchingArtifactStatus.Ready ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.HelpBox($"Artifact: {inspection.Status}\n{inspection.Path}\n{inspection.Diagnostic}", type);
                if (inspection.Artifact != null)
                    DrawCoverage(inspection.Artifact);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Warning);
            }
        }

        static void DrawCoverage(CharacterMotionMatchingDatabaseArtifact artifact)
        {
            EditorGUILayout.LabelField("Compiled Coverage", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Search Domain", artifact.SearchDomainId.Value);
            EditorGUILayout.LabelField("Samples", artifact.SampleCount.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Tree Nodes", artifact.SearchNodeCount.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Traversal Capacity", artifact.Capacities.TraversalCapacity.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Top-K Capacity", artifact.Capacities.TopK.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Plan Capacity", artifact.Capacities.PlanSampleCount.ToString(CultureInfo.InvariantCulture));
            MotionMatchingDatabaseCoverageDiagnosticsPayload diagnostics = artifact.CoverageDiagnostics;
            EditorGUILayout.LabelField(
                "Reachability",
                $"Samples {diagnostics.ReachableSampleCount}/{diagnostics.TotalSampleCount}, Segments {diagnostics.ReachableSegmentCount}/{diagnostics.TotalSegmentCount}");
            EditorGUILayout.LabelField(
                "Exact Duplicates",
                $"{diagnostics.ExactDuplicateSampleCount} samples ({diagnostics.ExactDuplicateSampleRatio:P2})");
            EditorGUILayout.LabelField(
                "Near Duplicates",
                $"{diagnostics.NearDuplicatePairCount}/{diagnostics.TotalUnorderedNonExactPairCount} pairs ({diagnostics.NearDuplicatePairRatio:P2})");
            EditorGUILayout.LabelField(
                "Protected Contact Empty Regions",
                $"{diagnostics.ProtectedContactEmptyRegionCount}/{diagnostics.EvaluatedNonEmptyRawProtectedContactRegionCount} ({diagnostics.ProtectedContactEmptyRegionRatio:P2})");
            EditorGUILayout.LabelField(
                "Candidate Upper Bound",
                diagnostics.MaximumAdmittedCandidateSetUpperBound.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField(
                "Search Index Maximum Depth",
                diagnostics.SearchIndexMaximumDepth.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < artifact.CoverageCount; i++)
            {
                MotionMatchingCoverageSummaryPayload coverage = artifact.GetCoverage(i);
                EditorGUILayout.HelpBox(
                    $"{coverage.RequirementId}: {(coverage.Satisfied ? "Satisfied" : "Missing")}\n" +
                    $"Samples {coverage.SampleCount}, Speed {coverage.MinimumObservedSpeed:R}..{coverage.MaximumObservedSpeed:R}, " +
                    $"Facing {coverage.MaximumObservedFacingChange:R}, Plan {coverage.MinimumObservedPlanHorizon:R}s",
                    coverage.Satisfied ? MessageType.Info : MessageType.Error);
            }
        }

        void StartDatabaseBuild()
        {
            try
            {
                CharacterMotionMatchingDatabaseDefinition database = (CharacterMotionMatchingDatabaseDefinition)target;
                int ready = 0;
                int missing = 0;
                int stale = 0;
                int clipCount = 0;
                for (int i = 0; i < database.SourceSets.Count; i++)
                {
                    MotionMatchingSourceSetFootAnalysisBuildRequest foot = MotionMatchingSourceSetFootAnalysisBuildRequest.Create(
                        database.SourceSets[i], m_AnalysisSource);
                    ready += foot.ReadyCount;
                    missing += foot.MissingCount;
                    stale += foot.StaleCount;
                    clipCount += database.SourceSets[i].SourceClips.Count;
                }
                if (missing > 0 || stale > 0)
                {
                    EditorUtility.DisplayDialog(
                        "Motion Matching Database Preflight Failed",
                        $"Clip Count: {clipCount}\nFoot Ready: {ready}\nFoot Missing: {missing}\nFoot Stale: {stale}\n\nRun Build Source Set Foot Analysis from each Source Set owner first.",
                        "OK");
                    return;
                }
                MotionMatchingDatabaseBuildRequest request = MotionMatchingDatabaseBuildRequestFactory.Create(
                    m_Profile, database, m_AnalysisSource);
                string sourceSets = string.Join(", ", database.SourceSets.Select(value => value.SourceSetId.Value));
                string summary =
                    $"Database: {database.DatabaseId}@{database.Revision}\n" +
                    $"Source Sets: {sourceSets}\nClip Count: {request.ClipCount}\n" +
                    $"Estimated Samples: {request.EstimatedSampleCount}\nFoot Ready: {ready}\n" +
                    $"Memory Upper Bound: {EditorUtility.FormatBytes(request.MemoryUpperBoundBytes)}";
                if (!EditorUtility.DisplayDialog("Build Motion Matching Database", summary, "Build", "Cancel"))
                    return;
                MotionMatchingDatabaseBuildJob job = MotionMatchingDatabaseBuildJob.Start(request);
                job.Finished += completed =>
                {
                    if (completed.Result.Succeeded)
                        Debug.Log($"Motion Matching Database '{database.DatabaseId}' published '{CharacterMotionMatchingDatabaseArtifactStore.GetPath(database)}'.", database);
                    else if (!completed.Result.Canceled)
                        Debug.LogError($"Motion Matching Database Build failed at {completed.Result.FinalStage}: {completed.Result.Diagnostic}", database);
                };
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Motion Matching Database Preflight Failed", exception.Message, "OK");
            }
        }
    }

    static class MotionMatchingSourceClipInspectionGui
    {
        static bool s_ShowDiagnostics;
        const string RegistrationNotice = "登记只建立正式配置，不自动Build。";
        const string ReimportNotice = "FBX导入或reimport只会使既有artifact变为Stale，必须由作者主动Build。";

        public static void DrawProfile(CharacterMotionMatchingProfile profile)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Source Clip Inspection", EditorStyles.boldLabel);
            DrawNotices();
            var drawnSourceSets = new HashSet<CharacterMotionMatchingSourceSet>();
            bool found = false;
            for (int databaseIndex = 0; databaseIndex < profile.Databases.Count; databaseIndex++)
            {
                CharacterMotionMatchingDatabaseDefinition database = profile.Databases[databaseIndex];
                if (!database)
                    continue;
                for (int sourceSetIndex = 0; sourceSetIndex < database.SourceSets.Count; sourceSetIndex++)
                {
                    CharacterMotionMatchingSourceSet sourceSet = database.SourceSets[sourceSetIndex];
                    if (!sourceSet || !drawnSourceSets.Add(sourceSet))
                        continue;
                    found = true;
                    DrawSourceSetContents(sourceSet);
                }
            }
            if (!found)
                EditorGUILayout.HelpBox("Profile database closure contains no Source Set to inspect.", MessageType.Info);
        }

        public static bool DrawSourceSet(CharacterMotionMatchingSourceSet sourceSet)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Source Clip Inspection", EditorStyles.boldLabel);
            DrawNotices();
            return DrawSourceSetContents(sourceSet);
        }

        public static bool TryGetFirstFailure(CharacterMotionMatchingSourceSet sourceSet, out string diagnostic)
        {
            return !AreAllReady(new[] { sourceSet }, out diagnostic);
        }

        public static bool AreAllReady(
            IReadOnlyList<CharacterMotionMatchingSourceSet> sourceSets,
            out string diagnostic)
        {
            if (sourceSets == null || sourceSets.Count == 0)
            {
                diagnostic = "No Source Set is configured.";
                return false;
            }
            for (int sourceSetIndex = 0; sourceSetIndex < sourceSets.Count; sourceSetIndex++)
            {
                CharacterMotionMatchingSourceSet sourceSet = sourceSets[sourceSetIndex];
                if (!sourceSet)
                {
                    diagnostic = $"Source Set #{sourceSetIndex} is missing.";
                    return false;
                }
                if (sourceSet.SourceClips.Count == 0)
                {
                    diagnostic = $"Source Set '{sourceSet.name}' contains no registered Source Clip.";
                    return false;
                }
                for (int clipIndex = 0; clipIndex < sourceSet.SourceClips.Count; clipIndex++)
                {
                    MotionMatchingSourceClipInspection inspection = MotionMatchingSourceClipResolver.Inspect(
                        sourceSet.SourceClips[clipIndex], sourceSet.SamplingCompatibilityMode);
                    if (inspection.HasFormalBuildPrerequisites)
                        continue;
                    diagnostic = Failure(sourceSet, clipIndex, inspection);
                    return false;
                }
            }
            diagnostic = string.Empty;
            return true;
        }

        static bool DrawSourceSetContents(CharacterMotionMatchingSourceSet sourceSet)
        {
            if (!sourceSet)
            {
                EditorGUILayout.HelpBox("Source Set is missing.", MessageType.Error);
                return false;
            }
            EditorGUILayout.LabelField($"Source Set: {sourceSet.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Compatibility Mode", sourceSet.SamplingCompatibilityMode.ToString());
            s_ShowDiagnostics = EditorGUILayout.Foldout(s_ShowDiagnostics, "Diagnostics", true);
            if (sourceSet.SourceClips.Count == 0)
            {
                EditorGUILayout.HelpBox("No Source Clip is registered.", MessageType.Error);
                return false;
            }

            bool allReady = true;
            for (int clipIndex = 0; clipIndex < sourceSet.SourceClips.Count; clipIndex++)
            {
                MotionMatchingSourceClipInspection inspection = MotionMatchingSourceClipResolver.Inspect(
                    sourceSet.SourceClips[clipIndex], sourceSet.SamplingCompatibilityMode);
                DrawClip(clipIndex, inspection, s_ShowDiagnostics);
                allReady &= inspection.HasFormalBuildPrerequisites;
            }
            return allReady;
        }

        static void DrawClip(int clipIndex, MotionMatchingSourceClipInspection inspection, bool showDiagnostics)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string sourceClipId = inspection.SourceClipId.IsValid ? inspection.SourceClipId.Value : $"Entry #{clipIndex}";
                EditorGUILayout.LabelField(sourceClipId, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("AnimationClip", inspection.AnimationClipExists ? inspection.Clip.name : "Missing");
                EditorGUILayout.LabelField("Importer", inspection.ImporterExists ? inspection.Importer.GetType().Name : "Missing");
                string declaredCompatibility = inspection.ModelImporter != null
                    ? inspection.DeclaredAnimationType.ToString()
                    : "Undeclared";
                EditorGUILayout.LabelField(
                    "Compatibility",
                    $"Required {inspection.CompatibilityMode} | Declared {declaredCompatibility} | {(inspection.CompatibilityDeclared ? "Valid" : "Invalid")}");
                if (inspection.CompatibilityMode == MotionMatchingSamplingCompatibilityMode.HumanoidRetargeted)
                {
                    EditorGUILayout.LabelField("Humanoid Avatar", inspection.SourceAvatar ? inspection.SourceAvatar.name : "Missing");
                }
                EditorGUILayout.LabelField("Ready", inspection.HasFormalBuildPrerequisites ? "Yes" : "No");
                if (!inspection.HasFormalBuildPrerequisites)
                    EditorGUILayout.HelpBox($"{inspection.Status}: {inspection.Diagnostic}", MessageType.Error);
                if (showDiagnostics)
                    DrawDiagnostics(inspection);
            }
        }

        static void DrawDiagnostics(MotionMatchingSourceClipInspection inspection)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Machine Identity", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                "Asset Identity",
                $"{inspection.AssetIdentityStatus} | {inspection.AssetGuid}:{inspection.LocalFileId.ToString(CultureInfo.InvariantCulture)}");
            if (inspection.CompatibilityMode == MotionMatchingSamplingCompatibilityMode.HumanoidRetargeted)
            {
                EditorGUILayout.LabelField(
                    "Avatar Identity",
                    inspection.SourceAvatarIdentityAvailable ? inspection.SourceAvatarIdentity : "Missing");
            }
            else if (inspection.CompatibilityMode == MotionMatchingSamplingCompatibilityMode.ExactGenericRig)
            {
                EditorGUILayout.LabelField(
                    "Generic Root",
                    string.IsNullOrEmpty(inspection.SourceRootIdentity) ? "Missing" : inspection.SourceRootIdentity);
                EditorGUILayout.LabelField(
                    "Hierarchy Identity",
                    inspection.SourceHierarchyIdentityAvailable
                        ? $"{inspection.SourceHierarchyIdentity} ({inspection.SourceHierarchyPathCount.ToString(CultureInfo.InvariantCulture)} paths)"
                        : "Missing");
            }
        }

        static string Failure(
            CharacterMotionMatchingSourceSet sourceSet,
            int clipIndex,
            MotionMatchingSourceClipInspection inspection)
        {
            string sourceClipId = inspection.SourceClipId.IsValid ? inspection.SourceClipId.Value : $"Entry #{clipIndex}";
            return $"Source Set '{sourceSet.name}', Clip '{sourceClipId}': {inspection.Status}. {inspection.Diagnostic}";
        }

        static void DrawNotices()
        {
            EditorGUILayout.HelpBox(RegistrationNotice, MessageType.Info);
            EditorGUILayout.HelpBox(ReimportNotice, MessageType.Info);
        }
    }
}
