using System;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public static class StairTraversalRampEditorOperations
    {
        const float RampThickness = 0.12f;

        public static bool CanCreate(StairTraversalSurfaceAuthoring authoring) =>
            HasCompleteInputs(authoring) && !authoring.TraversalRampCollider;

        public static bool CanUpdate(StairTraversalSurfaceAuthoring authoring) =>
            HasCompleteInputs(authoring) && authoring.TraversalRampCollider;

        public static BoxCollider Create(StairTraversalSurfaceAuthoring authoring)
        {
            RequireCompleteInputs(authoring);
            if (authoring.TraversalRampCollider)
                throw new InvalidOperationException($"Stair '{authoring.StairIdentity}' already has a Traversal Ramp reference.");
            var rampObject = new GameObject($"{authoring.StairIdentity.Trim()}_TraversalRamp");
            Undo.RegisterCreatedObjectUndo(rampObject, "Create Traversal Ramp");
            Undo.SetTransformParent(rampObject.transform, authoring.transform, "Create Traversal Ramp");
            BoxCollider ramp = Undo.AddComponent<BoxCollider>(rampObject);
            Undo.RecordObject(authoring, "Bind Traversal Ramp");
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("m_TraversalRampCollider").objectReferenceValue = ramp;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Update(authoring);
            return ramp;
        }

        public static void Update(StairTraversalSurfaceAuthoring authoring)
        {
            RequireCompleteInputs(authoring);
            BoxCollider ramp = authoring.TraversalRampCollider ??
                               throw new InvalidOperationException($"Stair '{authoring.StairIdentity}' has no Traversal Ramp to update.");
            StairSurfaceLayerResolution layers = StairSurfaceLayerResolver.ResolveRequired();
            Vector3 parentScale = authoring.transform.lossyScale;
            if (!ApproximatelyOne(parentScale.x) || !ApproximatelyOne(parentScale.y) || !ApproximatelyOne(parentScale.z))
                throw new InvalidOperationException($"Stair '{authoring.StairIdentity}' authoring parent scale must be one; actual {parentScale}.");
            Vector3 lower = authoring.LowerTransition.position;
            Vector3 upper = authoring.UpperTransition.position;
            Vector3 direction = upper - lower;
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.y <= StairTraversalSurfaceValidator.EndpointTolerance || horizontal.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException($"Stair '{authoring.StairIdentity}' transitions require positive rise and non-zero horizontal run. Lower={lower}, Upper={upper}.");
            Vector3 forward = direction.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 normal = Vector3.Cross(forward, right).normalized;
            float width = StairTraversalSurfaceValidator.MeasureFootSurfaceWidth(authoring.FootSurfaceRoot, right);
            if (width <= 0f)
                throw new InvalidOperationException($"Stair '{authoring.StairIdentity}' Foot Surface has no positive walkable width.");

            Undo.RecordObjects(new UnityEngine.Object[] { ramp.gameObject, ramp.transform, ramp }, "Update Traversal Ramp");
            Renderer[] renderers = ramp.GetComponents<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
                Undo.DestroyObjectImmediate(renderers[i]);
            ramp.gameObject.name = $"{authoring.StairIdentity.Trim()}_TraversalRamp";
            ramp.gameObject.layer = layers.Traversal;
            ramp.gameObject.isStatic = true;
            ramp.transform.localScale = Vector3.one;
            ramp.transform.SetPositionAndRotation(
                (lower + upper) * 0.5f - normal * (RampThickness * 0.5f),
                Quaternion.LookRotation(forward, normal));
            ramp.center = Vector3.zero;
            ramp.size = new Vector3(width, RampThickness, direction.magnitude);
            ramp.enabled = true;
            ramp.isTrigger = false;
            MarkDirty(authoring, ramp);
        }

        static bool HasCompleteInputs(StairTraversalSurfaceAuthoring authoring)
        {
            return authoring &&
                   !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorUtility.IsPersistent(authoring) &&
                   !string.IsNullOrWhiteSpace(authoring.StairIdentity) &&
                   authoring.FootSurfaceRoot &&
                   authoring.LowerTransition &&
                   authoring.UpperTransition &&
                   StairSurfaceLayerResolver.TryResolve(out _, out _);
        }

        static void RequireCompleteInputs(StairTraversalSurfaceAuthoring authoring)
        {
            if (!HasCompleteInputs(authoring))
                throw new InvalidOperationException("Traversal Ramp operation requires a non-persistent authoring instance with identity, Foot Surface, Lower Transition, Upper Transition, and valid formal layers.");
        }

        static bool ApproximatelyOne(float value) => Mathf.Abs(value - 1f) <= 0.0001f;

        static void MarkDirty(StairTraversalSurfaceAuthoring authoring, BoxCollider ramp)
        {
            EditorUtility.SetDirty(authoring);
            EditorUtility.SetDirty(ramp);
            EditorUtility.SetDirty(ramp.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ramp);
            if (authoring.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
        }
    }

    [CustomEditor(typeof(StairTraversalSurfaceAuthoring))]
    public sealed class StairTraversalSurfaceAuthoringEditor : UnityEditor.Editor
    {
        StairTraversalSurfaceValidationReport m_Report;

        void OnEnable()
        {
            RefreshReport();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                RefreshReport();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }

            StairTraversalSurfaceAuthoring authoring = (StairTraversalSurfaceAuthoring)target;
            EditorGUILayout.Space();
            DrawReport();
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!StairTraversalRampEditorOperations.CanCreate(authoring)))
            {
                if (GUILayout.Button("Create Traversal Ramp"))
                {
                    StairTraversalRampEditorOperations.Create(authoring);
                    RefreshReport();
                }
            }
            using (new EditorGUI.DisabledScope(!StairTraversalRampEditorOperations.CanUpdate(authoring)))
            {
                if (GUILayout.Button("Update Traversal Ramp"))
                {
                    StairTraversalRampEditorOperations.Update(authoring);
                    RefreshReport();
                }
            }
            if (GUILayout.Button("Refresh Validation"))
                RefreshReport();
        }

        void DrawReport()
        {
            if (m_Report == null)
            {
                EditorGUILayout.HelpBox("No cached Stair Traversal validation report.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Stair Identity", m_Report.StairIdentity);
            EditorGUILayout.LabelField("Traversal Ramp", m_Report.RampColliderPath);
            EditorGUILayout.LabelField("Ramp Layer", $"{m_Report.RampLayer}:{m_Report.RampRole}");
            EditorGUILayout.LabelField("Ramp Fixed Owner", m_Report.RampOwnerPath);
            EditorGUILayout.LabelField("Foot Surface Colliders", m_Report.FootSurfaceColliderCount.ToString());
            EditorGUILayout.LabelField("Foot Surface Layers", m_Report.FootSurfaceLayers);
            EditorGUILayout.LabelField("Foot Fixed Owner Bindings", m_Report.FootSurfaceFixedOwnerBindings.ToString());
            EditorGUILayout.LabelField("Lower Endpoint Error", FormatMeters(m_Report.LowerTransitionError));
            EditorGUILayout.LabelField("Upper Endpoint Error", FormatMeters(m_Report.UpperTransitionError));
            EditorGUILayout.LabelField("Ramp / Foot Width", $"{FormatMeters(m_Report.RampWidth)} / {FormatMeters(m_Report.FootSurfaceWidth)}");
            EditorGUILayout.LabelField("Direction Error", float.IsNaN(m_Report.DirectionErrorDegrees) ? "n/a" : $"{m_Report.DirectionErrorDegrees:0.####} deg");
            if (!m_Report.HasErrors)
            {
                EditorGUILayout.HelpBox("Stair Traversal authoring is valid.", MessageType.Info);
                return;
            }
            foreach (StairTraversalDiagnostic diagnostic in m_Report.Diagnostics.Where(value => value.Severity == StairTraversalDiagnosticSeverity.Error))
                EditorGUILayout.HelpBox($"{diagnostic.Code}\n{diagnostic.ObjectPath}\n{diagnostic.Message}", MessageType.Error);
        }

        void RefreshReport()
        {
            StairTraversalSurfaceAuthoring authoring = target as StairTraversalSurfaceAuthoring;
            m_Report = authoring ? StairTraversalSurfaceValidator.ValidateSingle(authoring) : null;
            Repaint();
        }

        static string FormatMeters(float value) => float.IsNaN(value) ? "n/a" : $"{value:0.####} m";
    }
}
