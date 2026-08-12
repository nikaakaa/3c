using UnityEditor;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection.Editor
{
    [CustomEditor(typeof(CharacterShapeProjectionProfile))]
    sealed class CharacterShapeProjectionProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CharacterShapeProjectionProfile profile = (CharacterShapeProjectionProfile)target;
            serializedObject.Update();
            EditorGUILayout.LabelField("Profile ID", profile.ProfileId.ToString());
            EditorGUILayout.LabelField("Bake Revision", profile.Revision.ToString());
            EditorGUILayout.LabelField("Runtime Tuning Revision", profile.RuntimeTuningRevision.ToString());
            EditorGUILayout.LabelField("Bake Content Hash", profile.ContentHash.ToString());
            EditorGUILayout.Space();
            bool runtimeChanged = DrawRuntimeTuningFields(serializedObject);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("烘焙参数", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这些参数决定Region拓扑、材质归属和固定容量，修改后必须重新Bake Artifact。", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorClusterThreshold"), new GUIContent("颜色聚类阈值"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smallRegionMergeThreshold"), new GUIContent("小区域合并阈值"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smallRegionTriangleLimit"), new GUIContent("小区域三角上限"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumProjectedRegionTriangles"), new GUIContent("发布Region最少三角数"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("capacity"), new GUIContent("固定容量"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("submeshRules"), new GUIContent("材质与Submesh规则"), true);
            bool bakeChanged = EditorGUI.EndChangeCheck();
            if (runtimeChanged || bakeChanged)
                Undo.RecordObject(profile, "Change Shape Projection Profile");
            serializedObject.ApplyModifiedProperties();
            if (bakeChanged)
            {
                profile.InvalidatePublishedContent();
                EditorUtility.SetDirty(profile);
            }
            else if (runtimeChanged)
            {
                profile.RecordRuntimeTuningChange();
                EditorUtility.SetDirty(profile);
            }
            if (GUILayout.Button("生成缺失Profile Identity"))
            {
                Undo.RecordObject(profile, "Generate Shape Projection Profile Identity");
                profile.EnsureIdentity();
                EditorUtility.SetDirty(profile);
            }
            if (GUILayout.Button("显式校验Profile"))
            {
                ShapeProjectionValidationResult result = profile.ValidateProfile();
                EditorUtility.DisplayDialog("Shape Projection Profile", result.IsValid ? "Profile有效" : result.Error, "确定");
            }
        }

        internal static void DrawRuntimeTuningEditor(CharacterShapeProjectionProfile profile)
        {
            SerializedObject profileObject = new SerializedObject(profile);
            profileObject.Update();
            bool changed = DrawRuntimeTuningFields(profileObject);
            if (changed)
                Undo.RecordObject(profile, "Tune Shape Projection Appearance");
            profileObject.ApplyModifiedProperties();
            if (!changed)
                return;
            profile.RecordRuntimeTuningChange();
            EditorUtility.SetDirty(profile);
        }

        static bool DrawRuntimeTuningFields(SerializedObject profileObject)
        {
            EditorGUILayout.LabelField("运行时效果参数", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这些参数不改变Bake拓扑，Play Mode中也可以直接调节；新参数会使旧的异步结果失效并重新发布。每个Region的主体环始终保留。", MessageType.None);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(profileObject.FindProperty("maximumSimplifyEpsilonPixels"), new GUIContent("最大直线化误差（像素）", "值越大越接近大块直线色面；运行时会按当前环尺寸自动收紧，避免主体退化。"));
            EditorGUILayout.PropertyField(profileObject.FindProperty("outlineWidthPixels"), new GUIContent("描边宽度（像素）"));
            EditorGUILayout.PropertyField(profileObject.FindProperty("minimumSecondaryLoopAreaPixels"), new GUIContent("次要环过滤面积（像素²）", "只过滤同一Region的次要碎屑环或小孔，不删除最大主体环。"));
            EditorGUILayout.PropertyField(profileObject.FindProperty("minimumSharedEdgePixels"), new GUIContent("共享边最短长度（像素）"));
            EditorGUILayout.PropertyField(profileObject.FindProperty("outlineColor"), new GUIContent("描边颜色"));
            return EditorGUI.EndChangeCheck();
        }
    }

    [CustomEditor(typeof(CharacterShapeProjectionArtifact))]
    sealed class CharacterShapeProjectionArtifactInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CharacterShapeProjectionArtifact artifact = (CharacterShapeProjectionArtifact)target;
            EditorGUILayout.LabelField("Artifact ID", artifact.ArtifactId.ToString());
            EditorGUILayout.LabelField("Profile ID", artifact.ProfileId.ToString());
            EditorGUILayout.LabelField("Profile Revision", artifact.ProfileRevision.ToString());
            EditorGUILayout.LabelField("Content Hash", artifact.ContentHash.ToString());
            EditorGUILayout.LabelField("Renderer", artifact.Renderers.Length.ToString());
            EditorGUILayout.LabelField("Dependency", artifact.Dependencies.Length.ToString());
            EditorGUILayout.LabelField("Triangle", artifact.BakeTriangleCount.ToString());
            EditorGUILayout.LabelField("Excluded Triangle", artifact.ExcludedTriangleCount.ToString());
            EditorGUILayout.LabelField("Region", artifact.Regions.Length.ToString());
            EditorGUILayout.LabelField("Shared Chain", artifact.SharedChains.Length.ToString());
            EditorGUILayout.LabelField("Baked UTC", artifact.BakedUtc);
            if (GUILayout.Button("显式校验Artifact"))
            {
                ShapeProjectionValidationResult result = ShapeProjectionDependencyValidator.Validate(artifact);
                EditorUtility.DisplayDialog("Shape Projection Artifact", result.IsValid ? "Artifact有效" : result.Error, "确定");
            }
        }
    }

    [CustomEditor(typeof(CharacterShapeProjectionSource))]
    sealed class CharacterShapeProjectionSourceInspector : UnityEditor.Editor
    {
        bool showRuntimeTuning = true;

        public override void OnInspectorGUI()
        {
            CharacterShapeProjectionSource source = (CharacterShapeProjectionSource)target;
            serializedObject.Update();
            SerializedProperty projectionEnabled = serializedObject.FindProperty("projectionEnabled");
            EditorGUI.BeginChangeCheck();
            bool nextProjectionEnabled = EditorGUILayout.Toggle("启用形状投影", projectionEnabled.boolValue);
            bool projectionChanged = EditorGUI.EndChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script", "projectionEnabled", "runtimeState", "fault", "diagnostics");
            serializedObject.ApplyModifiedProperties();
            if (projectionChanged)
            {
                CharacterShapeProjectionSource.RendererBinding[] bindings = source.RendererBindings;
                Object[] changedObjects = new Object[bindings.Length + 1];
                changedObjects[0] = source;
                for (int i = 0; i < bindings.Length; i++)
                    changedObjects[i + 1] = bindings[i].Renderer;
                Undo.RecordObjects(changedObjects, "Toggle Character Shape Projection");
                source.SetProjectionEnabled(nextProjectionEnabled);
                for (int i = 0; i < changedObjects.Length; i++)
                {
                    if (changedObjects[i] != null)
                        EditorUtility.SetDirty(changedObjects[i]);
                }
                serializedObject.Update();
            }
            if (source.Profile != null)
            {
                EditorGUILayout.Space();
                showRuntimeTuning = EditorGUILayout.Foldout(showRuntimeTuning, "效果参数", true);
                if (showRuntimeTuning)
                {
                    EditorGUI.indentLevel++;
                    CharacterShapeProjectionProfileInspector.DrawRuntimeTuningEditor(source.Profile);
                    if (GUILayout.Button("定位完整Profile与烘焙参数"))
                    {
                        Selection.activeObject = source.Profile;
                        EditorGUIUtility.PingObject(source.Profile);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            ShapeProjectionDiagnosticsSnapshot diagnostics = source.Diagnostics;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime State", source.RuntimeState.ToString());
            if (!string.IsNullOrEmpty(source.Fault))
                EditorGUILayout.HelpBox(source.Fault, MessageType.Error);
            EditorGUILayout.LabelField("Camera / Slot", $"{diagnostics.CameraInstanceId} / {diagnostics.SlotGeneration}");
            EditorGUILayout.LabelField("Bake / Tuning Revision", $"{diagnostics.ProfileRevision} / {diagnostics.ProfileRuntimeTuningRevision}");
            EditorGUILayout.LabelField("Submission / Age", $"{diagnostics.LastSubmissionSequence} / {diagnostics.ResultAgeFrames} frame");
            EditorGUILayout.LabelField("Slots / Skipped", $"{diagnostics.OccupiedSlots} / {diagnostics.SkippedSubmissions}");
            EditorGUILayout.LabelField("Renderer", $"{diagnostics.RendererCount}");
            EditorGUILayout.LabelField("Vertex", $"{diagnostics.VertexCount} / {diagnostics.VertexCapacity}");
            EditorGUILayout.LabelField("Triangle", $"{diagnostics.TriangleCount} / {diagnostics.TriangleCapacity}");
            EditorGUILayout.LabelField("Region", $"{diagnostics.ActiveRegions} active, {diagnostics.FilteredRegions} filtered / {diagnostics.RegionCapacity}");
            EditorGUILayout.LabelField("Shared Chain", $"{diagnostics.SharedChainCount} / {diagnostics.SharedChainCapacity}");
            EditorGUILayout.LabelField("Atlas", $"{diagnostics.AtlasUsedPixels} px / {diagnostics.AtlasWidth * diagnostics.AtlasHeight} px");
            EditorGUILayout.LabelField("Point / Loop / Instance", $"{diagnostics.ContourPointCount} / {diagnostics.LoopCount} / {diagnostics.IndirectInstanceCount}");
            EditorGUILayout.LabelField("Capture / Projection", $"{diagnostics.DeformationCaptureMilliseconds:F3} / {diagnostics.ProjectionMilliseconds:F3} ms");
            EditorGUILayout.LabelField("Mask Cmd / GPU", $"{diagnostics.MaskCommandMilliseconds:F3} / {diagnostics.MaskGpuMilliseconds:F3} ms");
            EditorGUILayout.LabelField("Readback / Contour", $"{diagnostics.ReadbackMilliseconds:F3} / {diagnostics.ContourMilliseconds:F3} ms");
            EditorGUILayout.LabelField("Composite Cmd / GPU", $"{diagnostics.CompositeCommandMilliseconds:F3} / {diagnostics.CompositeGpuMilliseconds:F3} ms");
            if (GUILayout.Button("生成缺失Source Identity"))
            {
                Undo.RecordObject(source, "Generate Shape Projection Source Identity");
                source.EnsureIdentity();
                EditorUtility.SetDirty(source);
            }
            if (GUILayout.Button("显式校验Source"))
            {
                ShapeProjectionValidationResult result = source.ValidateSource();
                EditorUtility.DisplayDialog("Shape Projection Source", result.IsValid ? "Source有效" : result.Error, "确定");
            }
        }
    }
}
