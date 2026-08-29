using System;
using System.Collections.Generic;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterFootPlacementRigCalibrationAuthoringSession
    {
        enum AuthoringPage : byte
        {
            RigMapping = 0,
            Calibration = 1
        }

        enum RigSemanticSlot : byte
        {
            Pelvis = 0,
            LeftHip = 1,
            LeftKnee = 2,
            LeftAnkle = 3,
            LeftToe = 4,
            RightHip = 5,
            RightKnee = 6,
            RightAnkle = 7,
            RightToe = 8
        }

        enum CalibrationEditMode : byte
        {
            HeelContact = 0,
            ToeContact = 1
        }

        static CharacterFootPlacementAnalysisSource s_Source;
        static CharacterFootPlacementPoseRig s_Rig;
        static CharacterAnimationRigBinding s_RigBinding;
        static CharacterWorldAwarePresentationBinding s_WorldBinding;
        static PrefabStage s_Stage;
        static CharacterFootPlacementFootCalibration s_Left;
        static CharacterFootPlacementFootCalibration s_Right;
        static CharacterFootSide s_Side = CharacterFootSide.Left;
        static CalibrationEditMode s_EditMode;
        static CharacterFootPlacementRigGeometryReport s_Report;
        static string s_Error = string.Empty;
        static readonly Dictionary<int, string> s_LastValidation = new Dictionary<int, string>();
        static bool s_PreviousToolsHidden;
        static bool s_HasToolsHiddenState;
        static AnimationModeDriver s_AnimationModeDriver;
        static PlayableGraph s_PreviewGraph;
        static Animator s_PreviewAnimator;
        static AuthoringPage s_Page;
        static RigSemanticSlot s_SelectedMappingSlot;
        static string s_PelvisBoneId = string.Empty;
        static string s_LeftHipBoneId = string.Empty;
        static string s_LeftKneeBoneId = string.Empty;
        static string s_LeftAnkleBoneId = string.Empty;
        static string s_LeftToeBoneId = string.Empty;
        static string s_RightHipBoneId = string.Empty;
        static string s_RightKneeBoneId = string.Empty;
        static string s_RightAnkleBoneId = string.Empty;
        static string s_RightToeBoneId = string.Empty;
        static string s_MappingError = string.Empty;

        public static void Open(CharacterFootPlacementAnalysisSource source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            source.RequireCalibrationAuthoringInput();
            CharacterFootPlacementSamplingRigAuthoringService.SynchronizeBinding(source);
            string path = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Sampling Rig GUID does not resolve to a Prefab asset.");
            PrefabStage stage = PrefabStageUtility.OpenPrefab(path);
            if (stage == null)
                throw new InvalidOperationException("Sampling Rig Prefab Stage could not be opened.");
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Bind(source, stage);
                }
                catch (Exception exception)
                {
                    Detach();
                    s_LastValidation[source.RigCalibration.GetInstanceID()] = exception.Message;
                    Debug.LogException(exception);
                }
            };
        }

        public static void RebuildGeometryValidation(CharacterFootPlacementAnalysisSource source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            try
            {
                CharacterFootPlacementRigGeometryValidationIdentity identity =
                    CharacterFootPlacementSamplingRigAuthoringService.RebuildGeometryValidation(source);
                s_LastValidation[source.RigCalibration.GetInstanceID()] =
                    $"Rig Calibration geometry is valid and published as {identity.GeometryContentHash}.";
            }
            catch (Exception exception)
            {
                s_LastValidation[source.RigCalibration.GetInstanceID()] = exception.Message;
                Debug.LogException(exception);
            }
        }

        public static bool IsEditing(CharacterWorldAwarePresentationBinding binding) =>
            binding && s_Rig != null && binding == s_Rig.World &&
            s_Stage != null && PrefabStageUtility.GetCurrentPrefabStage() == s_Stage;

        public static string GetLastValidation(CharacterFootPlacementRigCalibration calibration)
        {
            if (!calibration)
                return "No Sampling Rig geometry validation is available.";
            return s_LastValidation.TryGetValue(calibration.GetInstanceID(), out string summary)
                ? summary
                : "Open a referencing Analysis Source to evaluate Sampling Rig geometry.";
        }

        public static void DrawInspector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sampling Rig Authoring", EditorStyles.boldLabel);
            s_Page = (AuthoringPage)GUILayout.Toolbar(
                (int)s_Page,
                new[] { "Rig Mapping", "Calibration" });
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Preview Pose",
                    s_Source.CalibrationPreviewClip,
                    typeof(AnimationClip),
                    false);
                EditorGUILayout.FloatField(
                    "Preview Normalized Time",
                    s_Source.CalibrationPreviewNormalizedTime);
            }
            if (GUILayout.Button("Refresh Calibration Preview Pose"))
                RefreshPreviewPose();
            if (s_Page == AuthoringPage.RigMapping)
            {
                DrawRigMappingPage();
                return;
            }
            DrawCalibrationPage();
        }

        static void DrawCalibrationPage()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int leftIssueCount = CountDiagnostics(CharacterFootSide.Left);
                int rightIssueCount = CountDiagnostics(CharacterFootSide.Right);
                string leftLabel = leftIssueCount > 0 ? $"Left Foot ({leftIssueCount})" : "Left Foot";
                string rightLabel = rightIssueCount > 0 ? $"Right Foot ({rightIssueCount})" : "Right Foot";
                if (GUILayout.Toggle(s_Side == CharacterFootSide.Left, leftLabel, EditorStyles.miniButtonLeft))
                    s_Side = CharacterFootSide.Left;
                if (GUILayout.Toggle(s_Side == CharacterFootSide.Right, rightLabel, EditorStyles.miniButtonRight))
                    s_Side = CharacterFootSide.Right;
            }
            s_EditMode = (CalibrationEditMode)GUILayout.Toolbar(
                (int)s_EditMode,
                new[] { "Heel", "Toe" });
            if (GUILayout.Button("Frame Active Calibration Control"))
                FrameActiveControl();
            CharacterFootPlacementFootRigGeometry geometry = s_Side == CharacterFootSide.Left
                ? s_Report?.Left ?? default
                : s_Report?.Right ?? default;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Sole Frame", "Automatic: Z Heel to Toe / Y Visual Up / X Right");
                EditorGUILayout.TextField("Leg Extension Basis", geometry.LegLength.ToString("F4"));
                EditorGUILayout.TextField("Sole Length", geometry.SoleLength.ToString("F4"));
                EditorGUILayout.TextField("Heel / Toe Height", $"{geometry.ContactGroundError:F4}");
                EditorGUILayout.TextField("Forward Z Error", $"{geometry.SoleForwardErrorDegrees:F2} degrees / 15 max");
                EditorGUILayout.TextField("Up Y Error", $"{geometry.SoleUpErrorDegrees:F2} degrees / 15 max");
            }
            if (!string.IsNullOrEmpty(s_Error))
                EditorGUILayout.HelpBox(s_Error, MessageType.Error);
            else if (s_Report != null)
                DrawDraftDiagnostics();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Discard Unapplied Changes"))
                    LoadDraft();
                using (new EditorGUI.DisabledScope(s_Report == null || !s_Report.IsValid))
                {
                    if (GUILayout.Button("Apply Calibration Asset"))
                        Apply();
                }
            }
            EditorGUILayout.HelpBox(
                "Apply writes Calibration v4 Heel, Toe and Sole geometry only. Foot-analysis artifacts and Presentation Projection are rebuilt by their explicit Build commands.",
                MessageType.None);
        }

        static void Bind(CharacterFootPlacementAnalysisSource source, PrefabStage stage)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != stage || !stage.prefabContentsRoot)
                return;
            CharacterAnimationRigBinding[] rigBindings = stage.prefabContentsRoot.GetComponentsInChildren<CharacterAnimationRigBinding>(true);
            CharacterWorldAwarePresentationBinding[] worldBindings = stage.prefabContentsRoot.GetComponentsInChildren<CharacterWorldAwarePresentationBinding>(true);
            if (rigBindings.Length != 1 || worldBindings.Length != 1)
                throw new InvalidOperationException(
                    $"Sampling Rig requires exactly one Animation Rig Binding and World-Aware Binding; found {rigBindings.Length}/{worldBindings.Length}.");
            Detach();
            s_Source = source;
            s_RigBinding = rigBindings[0];
            s_WorldBinding = worldBindings[0];
            RebuildRig();
            s_Stage = stage;
            try
            {
                StartPreviewPose();
                LoadMappingDraft();
                LoadDraft();
                SceneView.duringSceneGui += OnSceneGUI;
                PrefabStage.prefabStageClosing += OnPrefabStageClosing;
                AssemblyReloadEvents.beforeAssemblyReload += Detach;
                s_PreviousToolsHidden = Tools.hidden;
                s_HasToolsHiddenState = true;
                Tools.hidden = true;
                Selection.activeObject = s_Rig.World.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            catch
            {
                Detach();
                throw;
            }
        }

        static void RebuildRig()
        {
            s_Rig = CharacterFootPlacementPoseRig.CreateCalibrationAuthoringRig(
                s_Source.RigCalibration,
                s_Source.RigDefinition,
                s_RigBinding,
                s_WorldBinding);
        }

        static void LoadMappingDraft()
        {
            CharacterAnimationRigDefinition definition = s_Source.RigDefinition;
            s_PelvisBoneId = definition.PelvisBoneId.Value;
            s_LeftHipBoneId = definition.LeftLeg.HipBoneId.Value;
            s_LeftKneeBoneId = definition.LeftLeg.KneeBoneId.Value;
            s_LeftAnkleBoneId = definition.LeftLeg.AnkleBoneId.Value;
            s_LeftToeBoneId = definition.LeftLeg.ToeBoneId.Value;
            s_RightHipBoneId = definition.RightLeg.HipBoneId.Value;
            s_RightKneeBoneId = definition.RightLeg.KneeBoneId.Value;
            s_RightAnkleBoneId = definition.RightLeg.AnkleBoneId.Value;
            s_RightToeBoneId = definition.RightLeg.ToeBoneId.Value;
            ValidateMappingDraft();
        }

        static void DrawRigMappingPage()
        {
            CharacterAnimationRigDefinition definition = s_Source.RigDefinition;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Rig v3 Semantic Mapping", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select the pelvis and both Physical leg chains from this Sampling Rig's exact catalog. Selecting a row highlights the same Transform in Scene view.",
                MessageType.Info);
            string[] ids = PhysicalBoneIds(definition);
            DrawMappingPicker("Pelvis", RigSemanticSlot.Pelvis, ref s_PelvisBoneId, ids);
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Left Leg", EditorStyles.boldLabel);
            DrawMappingPicker("Hip", RigSemanticSlot.LeftHip, ref s_LeftHipBoneId, ids);
            DrawMappingPicker("Knee", RigSemanticSlot.LeftKnee, ref s_LeftKneeBoneId, ids);
            DrawMappingPicker("Ankle", RigSemanticSlot.LeftAnkle, ref s_LeftAnkleBoneId, ids);
            DrawMappingPicker("Toe", RigSemanticSlot.LeftToe, ref s_LeftToeBoneId, ids);
            DrawLegMappingSummary("Left", s_LeftHipBoneId, s_LeftKneeBoneId, s_LeftAnkleBoneId, s_LeftToeBoneId);
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Right Leg", EditorStyles.boldLabel);
            DrawMappingPicker("Hip", RigSemanticSlot.RightHip, ref s_RightHipBoneId, ids);
            DrawMappingPicker("Knee", RigSemanticSlot.RightKnee, ref s_RightKneeBoneId, ids);
            DrawMappingPicker("Ankle", RigSemanticSlot.RightAnkle, ref s_RightAnkleBoneId, ids);
            DrawMappingPicker("Toe", RigSemanticSlot.RightToe, ref s_RightToeBoneId, ids);
            DrawLegMappingSummary("Right", s_RightHipBoneId, s_RightKneeBoneId, s_RightAnkleBoneId, s_RightToeBoneId);
            EditorGUILayout.Space(4f);
            if (!string.IsNullOrEmpty(s_MappingError))
                EditorGUILayout.HelpBox(s_MappingError, MessageType.Error);
            else
                EditorGUILayout.HelpBox("Rig Mapping draft is valid.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Discard Mapping Changes"))
                    LoadMappingDraft();
                if (GUILayout.Button("Frame Selected Bone"))
                    FrameSelectedMappingBone();
                using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(s_MappingError)))
                {
                    if (GUILayout.Button("Apply Rig Mapping"))
                        ApplyRigMapping();
                }
            }
            EditorGUILayout.HelpBox(
                "Apply updates Rig v3 and Calibration identity in one Undo group. Foot-analysis artifacts and Presentation Projection remain stale until their explicit Build commands run.",
                MessageType.None);
        }

        static void DrawMappingPicker(
            string label,
            RigSemanticSlot slot,
            ref string value,
            string[] ids)
        {
            string[] labels = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                labels[i] = ShortBoneName(ids[i]);
            int current = Array.IndexOf(ids, value);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool selected = GUILayout.Toggle(
                    s_SelectedMappingSlot == slot,
                    GUIContent.none,
                    GUILayout.Width(18f));
                if (selected)
                    s_SelectedMappingSlot = slot;
                EditorGUI.BeginChangeCheck();
                int next = EditorGUILayout.Popup(label, Mathf.Max(0, current), labels);
                if (EditorGUI.EndChangeCheck() && (uint)next < (uint)ids.Length)
                {
                    value = ids[next];
                    s_SelectedMappingSlot = slot;
                    ValidateMappingDraft();
                    SceneView.RepaintAll();
                }
            }
        }

        static void DrawLegMappingSummary(
            string label,
            string hip,
            string knee,
            string ankle,
            string toe)
        {
            Transform hipTransform = MappingTransform(hip);
            Transform kneeTransform = MappingTransform(knee);
            Transform ankleTransform = MappingTransform(ankle);
            Transform toeTransform = MappingTransform(toe);
            using (new EditorGUI.DisabledScope(true))
            {
                string chain = $"{ShortBoneName(hip)} → {ShortBoneName(knee)} → {ShortBoneName(ankle)} → {ShortBoneName(toe)}";
                EditorGUILayout.TextField($"{label} Parent Chain", chain);
                float upper = hipTransform && kneeTransform ? Vector3.Distance(hipTransform.position, kneeTransform.position) : 0f;
                float lower = kneeTransform && ankleTransform ? Vector3.Distance(kneeTransform.position, ankleTransform.position) : 0f;
                float foot = ankleTransform && toeTransform ? Vector3.Distance(ankleTransform.position, toeTransform.position) : 0f;
                EditorGUILayout.TextField($"{label} Length", $"leg {upper + lower:F4} · upper {upper:F4} · lower {lower:F4} · foot {foot:F4}");
            }
        }

        static void ValidateMappingDraft()
        {
            try
            {
                CharacterAnimationRigDefinition definition = s_Source.RigDefinition;
                var ids = new HashSet<string>(StringComparer.Ordinal);
                RequireUniquePhysical(definition, ids, "Pelvis", s_PelvisBoneId);
                RequireUniquePhysical(definition, ids, "Left Hip", s_LeftHipBoneId);
                RequireUniquePhysical(definition, ids, "Left Knee", s_LeftKneeBoneId);
                RequireUniquePhysical(definition, ids, "Left Ankle", s_LeftAnkleBoneId);
                RequireUniquePhysical(definition, ids, "Left Toe", s_LeftToeBoneId);
                RequireUniquePhysical(definition, ids, "Right Hip", s_RightHipBoneId);
                RequireUniquePhysical(definition, ids, "Right Knee", s_RightKneeBoneId);
                RequireUniquePhysical(definition, ids, "Right Ankle", s_RightAnkleBoneId);
                RequireUniquePhysical(definition, ids, "Right Toe", s_RightToeBoneId);
                RequireDirectParent(definition, "Left Hip", s_LeftHipBoneId, s_PelvisBoneId);
                RequireDirectParent(definition, "Left Knee", s_LeftKneeBoneId, s_LeftHipBoneId);
                RequireDirectParent(definition, "Left Ankle", s_LeftAnkleBoneId, s_LeftKneeBoneId);
                RequireDirectParent(definition, "Left Toe", s_LeftToeBoneId, s_LeftAnkleBoneId);
                RequireDirectParent(definition, "Right Hip", s_RightHipBoneId, s_PelvisBoneId);
                RequireDirectParent(definition, "Right Knee", s_RightKneeBoneId, s_RightHipBoneId);
                RequireDirectParent(definition, "Right Ankle", s_RightAnkleBoneId, s_RightKneeBoneId);
                RequireDirectParent(definition, "Right Toe", s_RightToeBoneId, s_RightAnkleBoneId);
                RequireSegmentLength("Left upper leg", s_LeftHipBoneId, s_LeftKneeBoneId);
                RequireSegmentLength("Left lower leg", s_LeftKneeBoneId, s_LeftAnkleBoneId);
                RequireSegmentLength("Left foot", s_LeftAnkleBoneId, s_LeftToeBoneId);
                RequireSegmentLength("Right upper leg", s_RightHipBoneId, s_RightKneeBoneId);
                RequireSegmentLength("Right lower leg", s_RightKneeBoneId, s_RightAnkleBoneId);
                RequireSegmentLength("Right foot", s_RightAnkleBoneId, s_RightToeBoneId);
                s_MappingError = string.Empty;
            }
            catch (Exception exception)
            {
                s_MappingError = exception.Message;
            }
        }

        static void RequireUniquePhysical(
            CharacterAnimationRigDefinition definition,
            HashSet<string> assigned,
            string label,
            string boneId)
        {
            if (FindPhysicalBoneIndex(definition, boneId) < 0)
                throw new InvalidOperationException($"{label} must reference a Physical Bone in the exact Rig catalog.");
            if (!assigned.Add(boneId))
                throw new InvalidOperationException($"{label} duplicates Physical Bone '{ShortBoneName(boneId)}'.");
        }

        static void RequireDirectParent(
            CharacterAnimationRigDefinition definition,
            string label,
            string childId,
            string parentId)
        {
            int childIndex = FindPhysicalBoneIndex(definition, childId);
            int parentIndex = FindPhysicalBoneIndex(definition, parentId);
            if (childIndex < 0 || parentIndex < 0 || definition.PhysicalBones[childIndex].ParentIndex != parentIndex)
                throw new InvalidOperationException($"{label} must be a direct child of '{ShortBoneName(parentId)}'.");
        }

        static void RequireSegmentLength(string label, string fromId, string toId)
        {
            Transform from = MappingTransform(fromId);
            Transform to = MappingTransform(toId);
            if (!from || !to || Vector3.Distance(from.position, to.position) <= 0.0001f)
                throw new InvalidOperationException($"{label} length is degenerate in the current preview pose.");
        }

        static void ApplyRigMapping()
        {
            ValidateMappingDraft();
            if (!string.IsNullOrEmpty(s_MappingError))
                return;
            DeriveSoleFrames();
            EvaluateDraft();
            if (s_Report == null || !s_Report.IsValid)
                return;
            CharacterAnimationRigDefinition definition = s_Source.RigDefinition;
            CharacterFootPlacementRigCalibration calibration = s_Source.RigCalibration;
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Sampling Rig Mapping");
            Undo.RecordObjects(new UnityEngine.Object[] { definition, calibration, s_RigBinding }, "Apply Sampling Rig Mapping");
            try
            {
                var physical = new CharacterAnimationPhysicalBoneDefinition[definition.PhysicalBones.Count];
                var virtualBones = new CharacterAnimationVirtualBoneDefinition[definition.VirtualBones.Count];
                for (int i = 0; i < physical.Length; i++)
                    physical[i] = definition.PhysicalBones[i];
                for (int i = 0; i < virtualBones.Length; i++)
                    virtualBones[i] = definition.VirtualBones[i];
                var spine = new AnimationBoneId[definition.SpineBoneCount];
                for (int i = 0; i < spine.Length; i++)
                    spine[i] = definition.GetSpineBoneId(i);
                definition.Configure(
                    definition.RigId,
                    Guid.NewGuid().ToString("N"),
                    physical,
                    virtualBones,
                    definition.RootBonePolicy,
                    definition.ScalePolicy,
                    definition.SolverRootBoneId,
                    new AnimationBoneId(s_PelvisBoneId),
                    spine,
                    definition.LeftArm,
                    definition.RightArm,
                    new CharacterAnimationLegChainDefinition(
                        new AnimationBoneId(s_LeftHipBoneId),
                        new AnimationBoneId(s_LeftKneeBoneId),
                        new AnimationBoneId(s_LeftAnkleBoneId),
                        new AnimationBoneId(s_LeftToeBoneId)),
                    new CharacterAnimationLegChainDefinition(
                        new AnimationBoneId(s_RightHipBoneId),
                        new AnimationBoneId(s_RightKneeBoneId),
                        new AnimationBoneId(s_RightAnkleBoneId),
                        new AnimationBoneId(s_RightToeBoneId)),
                    definition.HeadBoneId);
                calibration.Configure(calibration.CalibrationId, definition, s_Left, s_Right);
                var physicalTransforms = new Transform[s_RigBinding.PhysicalBones.Count];
                for (int i = 0; i < physicalTransforms.Length; i++)
                    physicalTransforms[i] = s_RigBinding.PhysicalBones[i];
                s_RigBinding.Configure(
                    s_RigBinding.Animator,
                    new CharacterAnimationRigPayload(definition),
                    physicalTransforms);
                CharacterFootPlacementRigGeometryValidationPublisher.Publish(s_Source, s_Report);
                RebuildRig();
                DeriveSoleFrames();
                EvaluateDraft();
                if (s_Report == null || !s_Report.IsValid)
                    throw new InvalidOperationException("Updated Rig mapping does not produce valid Foot Placement geometry.");
                calibration.Configure(calibration.CalibrationId, definition, s_Left, s_Right);
                CharacterFootPlacementRigGeometryValidationPublisher.Publish(s_Source, s_Report);
                EditorUtility.SetDirty(definition);
                EditorUtility.SetDirty(calibration);
                EditorUtility.SetDirty(s_RigBinding);
                AssetDatabase.SaveAssetIfDirty(definition);
                AssetDatabase.SaveAssetIfDirty(calibration);
                RebuildRig();
                LoadMappingDraft();
                LoadDraft();
                s_Error = string.Empty;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                s_MappingError = exception.Message;
                LoadMappingDraft();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        static void DrawRigMappingScene()
        {
            DrawMappingChain(
                new[] { s_PelvisBoneId, s_LeftHipBoneId, s_LeftKneeBoneId, s_LeftAnkleBoneId, s_LeftToeBoneId },
                new Color(0.2f, 0.75f, 1f));
            DrawMappingChain(
                new[] { s_PelvisBoneId, s_RightHipBoneId, s_RightKneeBoneId, s_RightAnkleBoneId, s_RightToeBoneId },
                new Color(1f, 0.55f, 0.2f));
            Transform selected = MappingTransform(SelectedMappingBoneId());
            if (!selected)
                return;
            Handles.color = Color.white;
            float size = HandleUtility.GetHandleSize(selected.position) * 0.09f;
            Handles.SphereHandleCap(0, selected.position, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(selected.position, $"{s_SelectedMappingSlot} · {selected.name}");
        }

        static void DrawMappingChain(string[] ids, Color color)
        {
            var points = new List<Vector3>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                Transform bone = MappingTransform(ids[i]);
                if (!bone)
                    return;
                points.Add(bone.position);
                Handles.color = new Color(color.r, color.g, color.b, 0.75f);
                Handles.SphereHandleCap(
                    0,
                    bone.position,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(bone.position) * 0.035f,
                    EventType.Repaint);
            }
            Handles.color = color;
            Handles.DrawAAPolyLine(4f, points.ToArray());
        }

        static void FrameSelectedMappingBone()
        {
            Transform selected = MappingTransform(SelectedMappingBoneId());
            if (!selected || SceneView.lastActiveSceneView == null)
                return;
            Selection.activeTransform = selected;
            SceneView.lastActiveSceneView.LookAt(
                selected.position,
                SceneView.lastActiveSceneView.rotation,
                Mathf.Max(0.2f, HandleUtility.GetHandleSize(selected.position) * 0.6f));
        }

        static string SelectedMappingBoneId()
        {
            return s_SelectedMappingSlot switch
            {
                RigSemanticSlot.Pelvis => s_PelvisBoneId,
                RigSemanticSlot.LeftHip => s_LeftHipBoneId,
                RigSemanticSlot.LeftKnee => s_LeftKneeBoneId,
                RigSemanticSlot.LeftAnkle => s_LeftAnkleBoneId,
                RigSemanticSlot.LeftToe => s_LeftToeBoneId,
                RigSemanticSlot.RightHip => s_RightHipBoneId,
                RigSemanticSlot.RightKnee => s_RightKneeBoneId,
                RigSemanticSlot.RightAnkle => s_RightAnkleBoneId,
                RigSemanticSlot.RightToe => s_RightToeBoneId,
                _ => string.Empty
            };
        }

        static Transform MappingTransform(string boneId)
        {
            if (s_Source == null || s_RigBinding == null || string.IsNullOrWhiteSpace(boneId))
                return null;
            int index = FindPhysicalBoneIndex(s_Source.RigDefinition, boneId);
            return index >= 0 && index < s_RigBinding.PhysicalBones.Count
                ? s_RigBinding.PhysicalBones[index]
                : null;
        }

        static string[] PhysicalBoneIds(CharacterAnimationRigDefinition definition)
        {
            var result = new string[definition.PhysicalBones.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = definition.PhysicalBones[i].BoneId.Value;
            return result;
        }

        static int FindPhysicalBoneIndex(CharacterAnimationRigDefinition definition, string boneId)
        {
            for (int i = 0; i < definition.PhysicalBones.Count; i++)
            {
                if (string.Equals(definition.PhysicalBones[i].BoneId.Value, boneId, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        static string ShortBoneName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "(Missing)";
            int slash = value.LastIndexOf('/');
            return slash >= 0 ? value.Substring(slash + 1) : value;
        }

        static void LoadDraft()
        {
            if (!s_Source || s_Rig == null)
                return;
            s_Left = s_Source.RigCalibration.Left;
            s_Right = s_Source.RigCalibration.Right;
            DeriveSoleFrames();
            EvaluateDraft();
            SceneView.RepaintAll();
        }

        static void Apply()
        {
            DeriveSoleFrames();
            EvaluateDraft();
            if (s_Report == null || !s_Report.IsValid)
                return;
            CharacterFootPlacementRigCalibration calibration = s_Source.RigCalibration;
            Undo.RecordObject(calibration, "Apply Foot Placement Rig Calibration");
            calibration.Configure(calibration.CalibrationId, s_Source.RigDefinition, s_Left, s_Right);
            CharacterFootPlacementRigGeometryValidationPublisher.Publish(s_Source, s_Report);
            EditorUtility.SetDirty(calibration);
            AssetDatabase.SaveAssetIfDirty(calibration);
            LoadDraft();
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (s_Rig == null || PrefabStageUtility.GetCurrentPrefabStage() != s_Stage)
            {
                Detach();
                return;
            }
            if (s_Page == AuthoringPage.RigMapping)
            {
                DrawRigMappingScene();
                return;
            }
            CharacterFootPlacementFootCalibration draft = s_Side == CharacterFootSide.Left ? s_Left : s_Right;
            Transform ankle = s_Side == CharacterFootSide.Left ? s_Rig.LeftAnkle : s_Rig.RightAnkle;
            Transform toe = s_Side == CharacterFootSide.Left ? s_Rig.LeftToe : s_Rig.RightToe;
            Color color = s_Side == CharacterFootSide.Left ? new Color(0.2f, 0.75f, 1f) : new Color(1f, 0.55f, 0.2f);
            Vector3 heelPosition = ankle.TransformPoint(draft.HeelContactLocalOffset);
            Vector3 toePosition = toe.TransformPoint(draft.ToeContactLocalOffset);
            Quaternion soleRotation = ankle.rotation * draft.SoleFrameLocalRotation;

            Handles.color = color;
            Handles.DrawAAPolyLine(4f, heelPosition, toePosition);
            DrawContact(heelPosition, s_EditMode == CalibrationEditMode.HeelContact, color);
            DrawContact(toePosition, s_EditMode == CalibrationEditMode.ToeContact, color);
            Vector3 soleCenter = (heelPosition + toePosition) * 0.5f;
            DrawReferenceGround(soleCenter);
            DrawAxis(soleCenter, soleRotation * Vector3.forward, Color.blue, "Z");
            DrawAxis(soleCenter, soleRotation * Vector3.up, Color.green, "Y");
            DrawAxis(soleCenter, soleRotation * Vector3.right, Color.red, "X");

            EditorGUI.BeginChangeCheck();
            Vector3 nextHeel = heelPosition;
            Vector3 nextToe = toePosition;
            string sideLabel = s_Side == CharacterFootSide.Left ? "Left" : "Right";
            switch (s_EditMode)
            {
                case CalibrationEditMode.HeelContact:
                    Handles.Label(heelPosition, $"{sideLabel} Heel Contact");
                    nextHeel = Handles.PositionHandle(heelPosition, Quaternion.identity);
                    break;
                case CalibrationEditMode.ToeContact:
                    Handles.Label(toePosition, $"{sideLabel} Toe Contact");
                    nextToe = Handles.PositionHandle(toePosition, Quaternion.identity);
                    break;
            }
            if (EditorGUI.EndChangeCheck())
            {
                draft = new CharacterFootPlacementFootCalibration(
                    ankle.InverseTransformPoint(nextHeel),
                    toe.InverseTransformPoint(nextToe),
                    draft.SoleFrameLocalRotation);
                draft = DeriveSoleFrame(draft, ankle, toe);
                if (s_Side == CharacterFootSide.Left)
                    s_Left = draft;
                else
                    s_Right = draft;
                EvaluateDraft();
                SceneView.RepaintAll();
            }
        }

        static void DrawAxis(Vector3 origin, Vector3 direction, Color color, string label)
        {
            if (direction.sqrMagnitude <= 0.000001f)
                return;
            direction.Normalize();
            float length = HandleUtility.GetHandleSize(origin) * 0.2f;
            Handles.color = color;
            Handles.ArrowHandleCap(0, origin, Quaternion.LookRotation(direction), length, EventType.Repaint);
            if (!string.IsNullOrEmpty(label))
                Handles.Label(origin + direction * length, label);
        }

        static void DrawContact(Vector3 position, bool active, Color color)
        {
            Handles.color = active ? Color.white : new Color(color.r, color.g, color.b, 0.55f);
            float size = HandleUtility.GetHandleSize(position) * (active ? 0.055f : 0.035f);
            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
        }

        static void DrawReferenceGround(Vector3 center)
        {
            if (s_Report == null)
                return;
            Vector3 up = s_Rig.VisualRoot.up;
            Vector3 groundCenter = center + up * (s_Report.ReferenceGroundHeight - Vector3.Dot(center, up));
            float size = Mathf.Max(0.25f, Mathf.Max(s_Report.Left.LegLength, s_Report.Right.LegLength) * 0.45f);
            Vector3 right = s_Rig.VisualRoot.right * size;
            Vector3 forward = Vector3.ProjectOnPlane(s_Rig.VisualRoot.forward, up).normalized * size;
            Handles.color = new Color(0.45f, 0.85f, 0.45f, 0.7f);
            Handles.DrawDottedLine(groundCenter - right, groundCenter + right, 4f);
            Handles.DrawDottedLine(groundCenter - forward, groundCenter + forward, 4f);
        }

        static void FrameActiveControl()
        {
            if (s_Rig == null || SceneView.lastActiveSceneView == null)
                return;
            CharacterFootPlacementFootCalibration draft = s_Side == CharacterFootSide.Left ? s_Left : s_Right;
            Transform hip = s_Side == CharacterFootSide.Left ? s_Rig.LeftHip : s_Rig.RightHip;
            Transform ankle = s_Side == CharacterFootSide.Left ? s_Rig.LeftAnkle : s_Rig.RightAnkle;
            Transform toe = s_Side == CharacterFootSide.Left ? s_Rig.LeftToe : s_Rig.RightToe;
            Vector3 heelPosition = ankle.TransformPoint(draft.HeelContactLocalOffset);
            Vector3 toePosition = toe.TransformPoint(draft.ToeContactLocalOffset);
            float legLength = Vector3.Distance(hip.position, ankle.position);
            Vector3 position = s_EditMode == CalibrationEditMode.HeelContact
                ? heelPosition
                : toePosition;
            SceneView.lastActiveSceneView.LookAt(position, SceneView.lastActiveSceneView.rotation, Mathf.Max(0.25f, legLength * 0.65f));
        }

        static CharacterFootPlacementFootCalibration DeriveSoleFrame(
            CharacterFootPlacementFootCalibration source,
            Transform ankle,
            Transform toe)
        {
            Vector3 heelPosition = ankle.TransformPoint(source.HeelContactLocalOffset);
            Vector3 toePosition = toe.TransformPoint(source.ToeContactLocalOffset);
            Vector3 up = s_Rig.VisualRoot.up;
            Vector3 forward = Vector3.ProjectOnPlane(toePosition - heelPosition, up);
            if (forward.sqrMagnitude <= 0.000001f)
                return source;
            Quaternion worldRotation = Quaternion.LookRotation(forward.normalized, up);
            return new CharacterFootPlacementFootCalibration(
                source.HeelContactLocalOffset,
                source.ToeContactLocalOffset,
                Quaternion.Inverse(ankle.rotation) * worldRotation);
        }

        static void DeriveSoleFrames()
        {
            s_Left = DeriveSoleFrame(s_Left, s_Rig.LeftAnkle, s_Rig.LeftToe);
            s_Right = DeriveSoleFrame(s_Right, s_Rig.RightAnkle, s_Rig.RightToe);
        }

        static int CountDiagnostics(CharacterFootSide side)
        {
            if (s_Report == null)
                return 0;
            int count = 0;
            for (int i = 0; i < s_Report.Diagnostics.Length; i++)
            {
                CharacterFootSide diagnosticSide = s_Report.Diagnostics[i].Side;
                if (diagnosticSide == side || diagnosticSide == 0)
                    count++;
            }
            return count;
        }

        static void DrawDraftDiagnostics()
        {
            if (s_Report.IsValid)
            {
                EditorGUILayout.HelpBox("Draft geometry is valid and ready to apply.", MessageType.Info);
                return;
            }
            bool selectedHasIssues = false;
            CharacterFootSide otherSide = s_Side == CharacterFootSide.Left
                ? CharacterFootSide.Right
                : CharacterFootSide.Left;
            int otherIssueCount = 0;
            for (int i = 0; i < s_Report.Diagnostics.Length; i++)
            {
                CharacterFootPlacementRigCalibrationDiagnostic diagnostic = s_Report.Diagnostics[i];
                if (diagnostic.Side == s_Side || diagnostic.Side == 0)
                {
                    selectedHasIssues = true;
                    EditorGUILayout.HelpBox(FormatDiagnostic(diagnostic), MessageType.Error);
                }
                else if (diagnostic.Side == otherSide)
                {
                    otherIssueCount++;
                }
            }
            if (!selectedHasIssues)
                EditorGUILayout.HelpBox("Selected foot is valid.", MessageType.Info);
            if (otherIssueCount > 0)
            {
                string otherLabel = otherSide == CharacterFootSide.Left ? "Left Foot" : "Right Foot";
                EditorGUILayout.HelpBox($"{otherLabel} has {otherIssueCount} remaining issue(s). Select it to inspect.", MessageType.Warning);
            }
        }

        static string FormatAuthoringDiagnostics(CharacterFootPlacementRigGeometryReport report)
        {
            if (report == null)
                return "No Sampling Rig geometry validation is available.";
            if (report.IsValid)
                return "Rig Calibration geometry is valid.";
            var builder = new StringBuilder();
            for (int i = 0; i < report.Diagnostics.Length; i++)
            {
                if (i > 0)
                    builder.AppendLine();
                builder.Append(FormatDiagnostic(report.Diagnostics[i]));
            }
            return builder.ToString();
        }

        static string FormatDiagnostic(CharacterFootPlacementRigCalibrationDiagnostic diagnostic)
        {
            string side = diagnostic.Side == CharacterFootSide.Left
                ? "Left"
                : diagnostic.Side == CharacterFootSide.Right ? "Right" : "Both";
            switch (diagnostic.Code)
            {
                case CharacterFootPlacementRigCalibrationDiagnosticCode.DegenerateSoleBaseline:
                    return $"{side} · Heel to Toe length {diagnostic.Actual:F4}; minimum {diagnostic.Limit:F4}.";
                case CharacterFootPlacementRigCalibrationDiagnosticCode.ContactGroundMismatch:
                    return $"{side} · Heel / Toe height difference {diagnostic.Actual:F4}; maximum {diagnostic.Limit:F4}.";
                case CharacterFootPlacementRigCalibrationDiagnosticCode.SoleForwardMismatch:
                    return $"{side} · Automatic forward Z error {diagnostic.Actual:F2} degrees; maximum {diagnostic.Limit:F2}.";
                case CharacterFootPlacementRigCalibrationDiagnosticCode.SoleUpMismatch:
                    return $"{side} · Automatic up Y error {diagnostic.Actual:F2} degrees; maximum {diagnostic.Limit:F2}.";
                case CharacterFootPlacementRigCalibrationDiagnosticCode.FlatGroundCorrectionExceeded:
                    return $"{side} · Flat-ground correction {diagnostic.Actual:F2} degrees; maximum {diagnostic.Limit:F2}.";
                case CharacterFootPlacementRigCalibrationDiagnosticCode.FeetGroundMismatch:
                    return $"Both · Left / Right ground height difference {diagnostic.Actual:F4}; maximum {diagnostic.Limit:F4}.";
                case CharacterFootPlacementRigCalibrationDiagnosticCode.FeetForwardOpposed:
                    return $"Both · Left / Right automatic forward axes oppose each other.";
                case CharacterFootPlacementRigCalibrationDiagnosticCode.SoleHandednessMismatch:
                    return "Both · Automatic sole frames do not share the required handedness.";
                default:
                    return $"{side} · {diagnostic.Code}: {diagnostic.Actual:F4}, limit {diagnostic.Limit:F4}.";
            }
        }

        static void EvaluateDraft()
        {
            try
            {
                CharacterFootPlacementRigCalibration.RequireValidDraft(s_Left, s_Right);
                s_Report = CharacterFootPlacementRigGeometryValidator.Evaluate(s_Rig, s_Left, s_Right);
                s_Error = string.Empty;
                s_LastValidation[s_Source.RigCalibration.GetInstanceID()] = FormatAuthoringDiagnostics(s_Report);
            }
            catch (Exception exception)
            {
                s_Report = null;
                s_Error = exception.Message;
                if (s_Source && s_Source.RigCalibration)
                    s_LastValidation[s_Source.RigCalibration.GetInstanceID()] = s_Error;
            }
        }

        static void StartPreviewPose()
        {
            if (AnimationMode.InAnimationMode())
                throw new InvalidOperationException("Close the active Animation or Timeline preview before editing Foot Placement calibration.");
            Animator[] animators = s_Stage.prefabContentsRoot.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException($"Sampling Rig requires exactly one Animator for calibration preview; found {animators.Length}.");
            s_PreviewAnimator = animators[0];
            s_AnimationModeDriver = ScriptableObject.CreateInstance<AnimationModeDriver>();
            s_AnimationModeDriver.hideFlags = HideFlags.HideAndDontSave;
            s_PreviewGraph = PlayableGraph.Create("Foot Placement Calibration Preview");
            s_PreviewGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(
                s_PreviewGraph,
                s_Source.CalibrationPreviewClip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                s_PreviewGraph,
                "Calibration Preview",
                s_PreviewAnimator);
            output.SetSourcePlayable(clipPlayable);
            s_PreviewGraph.Play();
            AnimationMode.StartAnimationMode(s_AnimationModeDriver);
            SamplePreviewPose();
        }

        static void RefreshPreviewPose()
        {
            try
            {
                SamplePreviewPose();
                DeriveSoleFrames();
                ValidateMappingDraft();
                EvaluateDraft();
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                StopPreviewPose();
                s_Error = exception.Message;
            }
        }

        static void SamplePreviewPose()
        {
            if (!s_PreviewGraph.IsValid() || !s_PreviewAnimator || !s_AnimationModeDriver)
                throw new InvalidOperationException("Calibration preview session is unavailable.");
            AnimationMode.BeginSampling();
            try
            {
                AnimationMode.SamplePlayableGraph(
                    s_PreviewGraph,
                    0,
                    s_Source.CalibrationPreviewTimeSeconds);
            }
            finally
            {
                AnimationMode.EndSampling();
            }
        }

        static void OnPrefabStageClosing(PrefabStage stage)
        {
            if (stage == s_Stage)
                Detach();
        }

        static void StopPreviewPose()
        {
            if (s_AnimationModeDriver && AnimationMode.InAnimationMode(s_AnimationModeDriver))
                AnimationMode.StopAnimationMode(s_AnimationModeDriver);
            if (s_PreviewGraph.IsValid())
                s_PreviewGraph.Destroy();
            if (s_AnimationModeDriver)
                UnityEngine.Object.DestroyImmediate(s_AnimationModeDriver);
            s_AnimationModeDriver = null;
            s_PreviewAnimator = null;
            s_PreviewGraph = default;
        }

        static void Detach()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            AssemblyReloadEvents.beforeAssemblyReload -= Detach;
            StopPreviewPose();
            if (s_HasToolsHiddenState)
            {
                Tools.hidden = s_PreviousToolsHidden;
                s_HasToolsHiddenState = false;
            }
            s_Source = null;
            s_Rig = null;
            s_RigBinding = null;
            s_WorldBinding = null;
            s_Stage = null;
            s_Report = null;
            s_Error = string.Empty;
            s_MappingError = string.Empty;
        }
    }

    [CustomEditor(typeof(CharacterFootPlacementRigCalibration))]
    public sealed class CharacterFootPlacementRigCalibrationEditor : UnityEditor.Editor
    {
        readonly List<CharacterFootPlacementAnalysisSource> m_References = new List<CharacterFootPlacementAnalysisSource>();

        public override void OnInspectorGUI()
        {
            CharacterFootPlacementRigCalibration calibration = (CharacterFootPlacementRigCalibration)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Calibration Id", calibration.CalibrationId.Value);
                EditorGUILayout.IntField("Schema Version", calibration.SchemaVersion);
                EditorGUILayout.TextField("Content Revision", calibration.ContentRevision);
            }
            CharacterFootPlacementRigGeometryValidationIdentity geometry = calibration.GeometryValidation;
            if (geometry == null)
            {
                EditorGUILayout.HelpBox(
                    "Geometry Validation is not published. Open a referencing Analysis Source, validate the preview pose, then Apply Calibration Asset.",
                    MessageType.Error);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Geometry Identity", geometry.IdentityHash);
                    EditorGUILayout.TextField("Geometry Content", geometry.GeometryContentHash);
                    EditorGUILayout.TextField("Validated Rig", $"{geometry.RigId}@{geometry.RigRevision}");
                    EditorGUILayout.TextField("Sampling Rig GUID", geometry.SamplingRigAssetGuid);
                    EditorGUILayout.TextField("Preview Clip GUID", geometry.PreviewClipAssetGuid);
                    EditorGUILayout.FloatField("Preview Normalized Time", geometry.PreviewNormalizedTime);
                }
            }
            EditorGUILayout.HelpBox(
                "Geometry is authored from an Analysis Source inside its exact Sampling Rig Prefab Stage.",
                MessageType.Info);
            EditorGUILayout.HelpBox(
                CharacterFootPlacementRigCalibrationAuthoringSession.GetLastValidation(calibration),
                MessageType.None);
            if (GUILayout.Button("Find Referencing Analysis Sources"))
                FindReferences(calibration);
            for (int i = 0; i < m_References.Count; i++)
            {
                CharacterFootPlacementAnalysisSource source = m_References[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(source, typeof(CharacterFootPlacementAnalysisSource), false);
                    string rigPath = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
                    EditorGUILayout.ObjectField(
                        AssetDatabase.LoadAssetAtPath<GameObject>(rigPath),
                        typeof(GameObject),
                        false,
                        GUILayout.MinWidth(120f));
                    if (GUILayout.Button("Edit", GUILayout.Width(52f)))
                        CharacterFootPlacementRigCalibrationAuthoringSession.Open(source);
                }
            }
        }

        void FindReferences(CharacterFootPlacementRigCalibration calibration)
        {
            m_References.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CharacterFootPlacementAnalysisSource");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterFootPlacementAnalysisSource source = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
                if (source && source.RigCalibration == calibration)
                    m_References.Add(source);
            }
        }
    }
}
