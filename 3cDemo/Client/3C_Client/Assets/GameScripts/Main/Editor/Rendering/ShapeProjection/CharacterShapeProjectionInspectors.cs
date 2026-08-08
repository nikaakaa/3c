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
            EditorGUILayout.LabelField("Revision", profile.Revision.ToString());
            EditorGUILayout.LabelField("Published Content Hash", profile.ContentHash.ToString());
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script", "profileId", "revision", "contentHash");
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed)
            {
                profile.InvalidatePublishedContent();
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
            ShapeProjectionDiagnosticsSnapshot diagnostics = source.Diagnostics;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime State", source.RuntimeState.ToString());
            if (!string.IsNullOrEmpty(source.Fault))
                EditorGUILayout.HelpBox(source.Fault, MessageType.Error);
            EditorGUILayout.LabelField("Camera / Slot", $"{diagnostics.CameraInstanceId} / {diagnostics.SlotGeneration}");
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
