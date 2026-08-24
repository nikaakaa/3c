using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterFootPlacementAnalysisSource))]
    public sealed class CharacterFootPlacementAnalysisSourceEditor : UnityEditor.Editor
    {
        SerializedProperty m_SamplingRigGuid;
        AnimationClip m_FootMotionClip;
        AnimationFootAnalysisArtifact m_FootMotionArtifact;
        CharacterFootMotionBakePlan m_FootMotionPlan;
        string m_FootMotionMessage = string.Empty;
        bool m_ShowMotionReferences;

        void OnEnable()
        {
            m_SamplingRigGuid = serializedObject.FindProperty("m_SamplingRigAssetGuid");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AnalysisSourceId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AnalysisVersion"));
            DrawSamplingRig();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RigDefinition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RigCalibration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CalibrationPreviewClip"));
            EditorGUILayout.Slider(
                serializedObject.FindProperty("m_CalibrationPreviewNormalizedTime"),
                0f,
                1f,
                "Calibration Preview Time");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SampleRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Thresholds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Reduction"), true);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Algorithm", CharacterFootPlacementAnalysisSource.AlgorithmVersion);
            serializedObject.ApplyModifiedProperties();
            DrawStatus();
            CharacterFootPlacementAnalysisSource source = (CharacterFootPlacementAnalysisSource)target;
            DrawMotionReferences(source);
            EditorGUILayout.HelpBox(
                CharacterFootPlacementRigCalibrationAuthoringSession.GetLastValidation(source.RigCalibration),
                MessageType.None);
            if (GUILayout.Button("Rebuild Geometry Validation From Preview Pose"))
                CharacterFootPlacementRigCalibrationAuthoringSession.RebuildGeometryValidation(source);
            if (GUILayout.Button("Edit Rig Calibration In Sampling Rig"))
            {
                try
                {
                    CharacterFootPlacementRigCalibrationAuthoringSession.Open(source);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
            DrawFootMotionData(source);
        }

        void DrawMotionReferences(CharacterFootPlacementAnalysisSource source)
        {
            m_ShowMotionReferences = EditorGUILayout.Foldout(
                m_ShowMotionReferences,
                $"Motion References ({source.MotionReferences.Count})",
                true);
            if (!m_ShowMotionReferences)
                return;
            EditorGUILayout.TextField("Motion Root", source.MotionRootBoneId.Value ?? string.Empty);
            for (int i = 0; i < source.MotionReferences.Count; i++)
            {
                CharacterFootMotionReferenceBinding binding = source.MotionReferences[i];
                if (binding == null)
                    continue;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Target", binding.RequireTargetClip(), typeof(AnimationClip), false);
                    EditorGUILayout.ObjectField("Motion", binding.RequireMotionReferenceClip(), typeof(AnimationClip), false);
                }
            }
        }

        void DrawFootMotionData(CharacterFootPlacementAnalysisSource source)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Foot Motion Data", EditorStyles.boldLabel);
            AnimationClip next = EditorGUILayout.ObjectField(
                "Animation Clip",
                m_FootMotionClip,
                typeof(AnimationClip),
                false) as AnimationClip;
            if (next != m_FootMotionClip)
            {
                m_FootMotionClip = next;
                m_FootMotionArtifact = null;
                m_FootMotionPlan = null;
                m_FootMotionMessage = string.Empty;
            }
            DrawResolvedMotionReference(source);
            using (new EditorGUI.DisabledScope(!m_FootMotionClip))
            {
                if (GUILayout.Button("Analyze Single Clip"))
                {
                    RunFootMotionAction(() =>
                    {
                        m_FootMotionPlan = CharacterFootMotionBakeService.Analyze(
                            source,
                            m_FootMotionClip);
                        m_FootMotionArtifact = AnimationFootAnalysisArtifactBuilder.Inspect(
                            m_FootMotionClip,
                            source).Artifact;
                        m_FootMotionMessage =
                            $"单Clip分析完成：{m_FootMotionPlan.State}，变化 {m_FootMotionPlan.ChangedChannels.Count} 条。";
                    });
                }
                DrawApply(source);
            }
            if (!string.IsNullOrEmpty(m_FootMotionMessage))
                EditorGUILayout.HelpBox(m_FootMotionMessage, MessageType.Info);
            DrawArtifactSummary();
            DrawPlan();
        }

        void DrawResolvedMotionReference(CharacterFootPlacementAnalysisSource source)
        {
            if (!m_FootMotionClip)
                return;
            try
            {
                CharacterFootMotionReference motionReference = source.RequireMotionReference(m_FootMotionClip);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Motion Reference",
                        motionReference.MotionReference,
                        typeof(AnimationClip),
                        false);
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }

        void DrawApply(CharacterFootPlacementAnalysisSource source)
        {
            using (new EditorGUI.DisabledScope(m_FootMotionPlan == null || m_FootMotionPlan.IsNoChange))
            {
                string button = m_FootMotionPlan != null && m_FootMotionPlan.RequiresReplace
                    ? "Replace Existing 22 Curves"
                    : "Apply 22 Curves To Animation Clip";
                if (!GUILayout.Button(button))
                    return;
                bool replace = m_FootMotionPlan.RequiresReplace;
                if (replace && !EditorUtility.DisplayDialog(
                        "Replace Foot Motion Curves",
                        BuildReplaceConfirmation(m_FootMotionPlan),
                        "Replace 22 Curves",
                        "Cancel"))
                    return;
                RunFootMotionAction(() =>
                {
                    CharacterFootMotionBakeApplyResult result = CharacterFootMotionBakeService.Apply(
                        m_FootMotionPlan,
                        m_FootMotionPlan.PlanHash,
                        replace);
                    m_FootMotionPlan = result.Plan;
                    m_FootMotionArtifact = AnimationFootAnalysisArtifactBuilder.Inspect(
                        m_FootMotionClip,
                        source).Artifact;
                    m_FootMotionMessage = result.Applied
                        ? "22条曲线已原子写入并逐Key验证。"
                        : "当前22条曲线与Candidate完全相同，没有写入。";
                });
            }
        }

        void DrawArtifactSummary()
        {
            if (m_FootMotionArtifact == null)
                return;
            AnimationFootMotionDataDescriptor data = m_FootMotionArtifact.MotionData;
            EditorGUILayout.LabelField("Raw Samples", data.Raw.RootSamples.Count.ToString());
            EditorGUILayout.LabelField("Left Status", string.IsNullOrEmpty(data.Left.Diagnostic) ? "Ready" : data.Left.Diagnostic);
            EditorGUILayout.LabelField("Right Status", string.IsNullOrEmpty(data.Right.Diagnostic) ? "Ready" : data.Right.Diagnostic);
            DrawMotionFootSummary("Left", data.Left);
            DrawMotionFootSummary("Right", data.Right);
        }

        void DrawPlan()
        {
            if (m_FootMotionPlan == null)
                return;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Curve State", m_FootMotionPlan.State);
                EditorGUILayout.TextField("Plan Hash", m_FootMotionPlan.PlanHash);
            }
            for (int i = 0; i < m_FootMotionPlan.ChangedChannels.Count; i++)
            {
                CharacterFootMotionBakeChannelDiff diff = m_FootMotionPlan.ChangedChannels[i];
                EditorGUILayout.LabelField(diff.PropertyName, diff.Kind.ToString());
            }
            foreach (CharacterAnimationClipRegisteredCurveDescriptor descriptor in
                     CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels)
            {
                EditorGUILayout.LabelField(
                    descriptor.Binding.propertyName,
                    $"{m_FootMotionPlan.Candidate.Curves[descriptor.ChannelId].length} keys");
            }
        }

        static string BuildReplaceConfirmation(CharacterFootMotionBakePlan plan)
        {
            var lines = new string[plan.ChangedChannels.Count];
            for (int i = 0; i < lines.Length; i++)
            {
                CharacterFootMotionBakeChannelDiff diff = plan.ChangedChannels[i];
                lines[i] = $"{diff.PropertyName}: {diff.Kind}";
            }
            return $"Target: {plan.TargetAssetPath}\nMotion Reference: {plan.MotionReferenceAssetPath}\n\n" +
                   $"The following {lines.Length} channels will be replaced:\n" +
                   string.Join("\n", lines);
        }

        static void DrawMotionFootSummary(string side, AnimationFootMotionFootPage foot)
        {
            int landing = 0;
            int liftOff = 0;
            float height = 0f;
            float toeHeight = float.NegativeInfinity;
            float toeSpeed = 0f;
            float positionError = 0f;
            float rotationError = 0f;
            float contact = 0f;
            float lockWeight = 0f;
            float support = 0f;
            for (int i = 0; i < foot.Events.Count; i++)
            {
                if (foot.Events[i].Kind == AnimationFootMotionEventKind.Landing)
                    landing++;
                else
                    liftOff++;
            }
            for (int i = 0; i < foot.Samples.Count; i++)
            {
                AnimationFootMotionDerivedSample sample = foot.Samples[i];
                height = Mathf.Max(height, sample.Step.HeightAbovePath);
                toeHeight = Mathf.Max(toeHeight, sample.Filter.ToeHeight);
                toeSpeed = Mathf.Max(toeSpeed, sample.Filter.ToeSpeed);
                positionError = Mathf.Max(positionError, sample.Filter.PositionError);
                rotationError = Mathf.Max(rotationError, sample.Filter.RotationError);
                contact = Mathf.Max(contact, sample.Filter.Contact);
                lockWeight = Mathf.Max(lockWeight, sample.Constraint.LockWeight);
                support = Mathf.Max(support, sample.Constraint.Support);
            }
            EditorGUILayout.LabelField($"{side} Events", $"Landing {landing} / LiftOff {liftOff}");
            EditorGUILayout.LabelField(
                $"{side} Diagnostics",
                foot.Diagnostics.Count == 0
                    ? "None"
                    : string.Join(", ", System.Linq.Enumerable.Select(foot.Diagnostics, value => value.Code.ToString())));
            EditorGUILayout.LabelField($"{side} Step/Path", $"Height {height:0.###}m");
            EditorGUILayout.LabelField($"{side} Toe", $"Height {toeHeight:0.###}m / Speed {toeSpeed:0.###}m/s");
            EditorGUILayout.LabelField($"{side} Pose Error", $"Pos {positionError:0.###}m / Rot {rotationError:0.##}°");
            EditorGUILayout.LabelField($"{side} Contact/Lock/Support", $"{contact:0.###} / {lockWeight:0.###} / {support:0.###}");
        }

        void RunFootMotionAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                m_FootMotionMessage = exception.Message;
                Debug.LogException(exception);
            }
        }

        void DrawSamplingRig()
        {
            string guid = m_SamplingRigGuid.stringValue;
            GameObject current = CharacterFootPlacementAnalysisSource.IsAssetGuid(guid)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))
                : null;
            GameObject next = EditorGUILayout.ObjectField("Sampling Rig Prefab", current, typeof(GameObject), false) as GameObject;
            if (next == current)
                return;
            if (next && PrefabUtility.GetPrefabAssetType(next) == PrefabAssetType.NotAPrefab)
            {
                EditorUtility.DisplayDialog("Invalid Sampling Rig", "Sampling Rig must be a persisted Prefab asset.", "OK");
                return;
            }
            m_SamplingRigGuid.stringValue = next
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(next))
                : string.Empty;
        }

        void DrawStatus()
        {
            try
            {
                ((CharacterFootPlacementAnalysisSource)target).RequireValid();
                EditorGUILayout.HelpBox("Analysis Source is valid.", MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }
    }

    [CustomEditor(typeof(CharacterWorldAwarePresentationBinding))]
    public sealed class CharacterWorldAwarePresentationBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PresentationRoot"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SelfColliderRoot"));
            serializedObject.ApplyModifiedProperties();
            CharacterWorldAwarePresentationBinding binding = (CharacterWorldAwarePresentationBinding)target;
            if (CharacterFootPlacementRigCalibrationAuthoringSession.IsEditing(binding))
            {
                CharacterFootPlacementRigCalibrationAuthoringSession.DrawInspector();
                return;
            }
            try
            {
                binding.RequireValid();
                EditorGUILayout.HelpBox("World-Aware Presentation Binding is valid.", MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
            CharacterFootPlacementAnalysisSource source = FindAnalysisSource(binding);
            if (!source)
            {
                EditorGUILayout.HelpBox(
                    "This Prefab Stage has no exact Foot Placement Analysis Source for its asset GUID.",
                    MessageType.Error);
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("Open Foot Placement Calibration");
                return;
            }
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Foot Placement Source", source, typeof(CharacterFootPlacementAnalysisSource), false);
            if (GUILayout.Button("Open Foot Placement Calibration"))
                CharacterFootPlacementRigCalibrationAuthoringSession.Open(source);
        }

        static CharacterFootPlacementAnalysisSource FindAnalysisSource(CharacterWorldAwarePresentationBinding binding)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || !stage.prefabContentsRoot ||
                !binding.transform.IsChildOf(stage.prefabContentsRoot.transform))
                return null;
            string samplingRigGuid = AssetDatabase.AssetPathToGUID(stage.assetPath);
            if (string.IsNullOrEmpty(samplingRigGuid))
                return null;
            string[] guids = AssetDatabase.FindAssets("t:CharacterFootPlacementAnalysisSource");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterFootPlacementAnalysisSource source = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
                if (source && string.Equals(source.SamplingRigAssetGuid, samplingRigGuid, StringComparison.Ordinal))
                    return source;
            }
            return null;
        }
    }
}
