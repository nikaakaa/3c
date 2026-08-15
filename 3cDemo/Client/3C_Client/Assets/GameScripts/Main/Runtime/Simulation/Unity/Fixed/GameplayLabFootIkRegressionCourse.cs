using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public enum GameplayLabFootIkInputScenario : byte
    {
        Straight = 1,
        AlternatingLateral = 2,
        SmoothCurve = 3
    }

    public static class GameplayLabFootIkRegressionCourse
    {
        public const string RootName = "Foot IK Automatic Regression Course";
        public const string StartMarkerName = "teststart";
        public const string EndMarkerName = "testend";
        public const string AscentStepPrefix = "FootIkRegressionAscent_";
        public const string DescentStepPrefix = "FootIkRegressionDescent_";
        public const string AscentIdentity = "foot-ik-regression-ascent";
        public const string DescentIdentity = "foot-ik-regression-descent";
        public const int StepCountPerFlight = 24;
        public const float StepRise = 0.18f;
        public const float StepRun = 0.52f;
        public const float CourseWidth = 8f;
        public const float TopLength = 6f;
        public const float LateralAmplitude = 2f;
        public const float LateralSafetyMargin = 0.5f;
        public const float EndpointMargin = 3f;
        public const float AlignmentDistance = 10f;
        public const float CourseX = 40f;
        public const float CourseStartZ = 0f;

        public static float FlightRun => StepCountPerFlight * StepRun;
        public static float CourseHeight => StepCountPerFlight * StepRise;
        public static float DescentStartZ => CourseStartZ + FlightRun + TopLength;
        public static float CourseEndZ => DescentStartZ + FlightRun;
        public static Vector3 StartPosition => new Vector3(CourseX, 0f, CourseStartZ - EndpointMargin);
        public static Vector3 EndPosition => new Vector3(CourseX, 0f, CourseEndZ + EndpointMargin);
        public static Vector3 PlayerSpawnPosition => StartPosition - Vector3.forward * (AlignmentDistance - 0.75f);
        public static Vector3 TargetSpawnPosition => PlayerSpawnPosition + Vector3.right * 3.2f;

        public static string ScenarioIdentity(GameplayLabFootIkInputScenario scenario) => scenario switch
        {
            GameplayLabFootIkInputScenario.Straight => "straight",
            GameplayLabFootIkInputScenario.AlternatingLateral => "alternating-lateral",
            GameplayLabFootIkInputScenario.SmoothCurve => "smooth-curve",
            _ => throw new InvalidOperationException("GameplayLab Foot IK input scenario is invalid.")
        };

        public static GameplayLabFootIkInputScenario NextScenario(GameplayLabFootIkInputScenario scenario) => scenario switch
        {
            GameplayLabFootIkInputScenario.Straight => GameplayLabFootIkInputScenario.AlternatingLateral,
            GameplayLabFootIkInputScenario.AlternatingLateral => GameplayLabFootIkInputScenario.SmoothCurve,
            GameplayLabFootIkInputScenario.SmoothCurve => GameplayLabFootIkInputScenario.Straight,
            _ => throw new InvalidOperationException("GameplayLab Foot IK input scenario is invalid.")
        };

        public static void Resolve(Scene scene, out Vector3 start, out Vector3 end)
        {
            Transform root = FindUnique(scene, RootName);
            Transform startMarker = FindUnique(root, StartMarkerName);
            Transform endMarker = FindUnique(root, EndMarkerName);
            start = startMarker.position;
            end = endMarker.position;
            Validate(root, start, end);
        }

        static void Validate(Transform root, Vector3 start, Vector3 end)
        {
            if ((start - StartPosition).sqrMagnitude > 0.000001f ||
                (end - EndPosition).sqrMagnitude > 0.000001f)
            {
                throw new InvalidOperationException(
                    $"GameplayLab Foot IK course markers do not match the formal route. Start={start}/{StartPosition}, End={end}/{EndPosition}.");
            }
            Vector3 route = Vector3.ProjectOnPlane(end - start, Vector3.up);
            if (route.sqrMagnitude <= 1f)
                throw new InvalidOperationException("GameplayLab Foot IK regression route is too short.");
            Vector3 forward = route.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            var ascent = new List<BoxCollider>(StepCountPerFlight);
            var descent = new List<BoxCollider>(StepCountPerFlight);
            BoxCollider[] colliders = root.GetComponentsInChildren<BoxCollider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                BoxCollider collider = colliders[i];
                if (collider.name.StartsWith(AscentStepPrefix, StringComparison.Ordinal))
                    ascent.Add(collider);
                else if (collider.name.StartsWith(DescentStepPrefix, StringComparison.Ordinal))
                    descent.Add(collider);
            }
            if (ascent.Count != StepCountPerFlight || descent.Count != StepCountPerFlight)
            {
                throw new InvalidOperationException(
                    $"GameplayLab Foot IK regression course requires {StepCountPerFlight} ascent and descent treads, found {ascent.Count}/{descent.Count}.");
            }
            ValidateTreads(ascent, start, route.magnitude, forward, right);
            ValidateTreads(descent, start, route.magnitude, forward, right);
            StairTraversalSurfaceAuthoring[] stairs = root.GetComponentsInChildren<StairTraversalSurfaceAuthoring>(true);
            if (stairs.Length != 2 ||
                Array.Find(stairs, value => string.Equals(value.StairIdentity, AscentIdentity, StringComparison.Ordinal)) == null ||
                Array.Find(stairs, value => string.Equals(value.StairIdentity, DescentIdentity, StringComparison.Ordinal)) == null)
            {
                throw new InvalidOperationException("GameplayLab Foot IK regression course requires one ascent and one descent traversal ramp.");
            }
        }

        static void ValidateTreads(
            IReadOnlyList<BoxCollider> treads,
            Vector3 routeStart,
            float routeLength,
            Vector3 forward,
            Vector3 right)
        {
            float requiredHalfWidth = LateralAmplitude + LateralSafetyMargin;
            for (int i = 0; i < treads.Count; i++)
            {
                BoxCollider tread = treads[i];
                if (!tread.enabled || tread.isTrigger ||
                    tread.gameObject.layer != CharacterSurfaceLayerRoles.FootPlacementSurfaceLayer)
                {
                    throw new InvalidOperationException($"GameplayLab Foot IK tread '{tread.name}' has an invalid collision role.");
                }
                Bounds bounds = tread.bounds;
                Vector3 relative = bounds.center - routeStart;
                float along = Vector3.Dot(relative, forward);
                float lateral = Mathf.Abs(Vector3.Dot(relative, right));
                float lateralExtent =
                    Mathf.Abs(right.x) * bounds.extents.x +
                    Mathf.Abs(right.y) * bounds.extents.y +
                    Mathf.Abs(right.z) * bounds.extents.z;
                if (along <= 0f || along >= routeLength || lateral + requiredHalfWidth > lateralExtent)
                {
                    throw new InvalidOperationException(
                        $"GameplayLab Foot IK tread '{tread.name}' does not cover the formal route. Along={along}, Lateral={lateral}, Extent={lateralExtent}.");
                }
            }
        }

        static Transform FindUnique(Scene scene, string requiredName)
        {
            Transform found = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                Find(roots[i].transform, requiredName, ref found);
            return found ? found : throw new InvalidOperationException(
                $"GameplayLab Foot IK regression object '{requiredName}' was not found in scene '{scene.path}'.");
        }

        static Transform FindUnique(Transform root, string requiredName)
        {
            Transform found = null;
            Find(root, requiredName, ref found);
            return found ? found : throw new InvalidOperationException(
                $"GameplayLab Foot IK regression object '{requiredName}' was not found under '{root.name}'.");
        }

        static void Find(Transform value, string requiredName, ref Transform found)
        {
            if (string.Equals(value.name, requiredName, StringComparison.Ordinal))
            {
                if (found)
                    throw new InvalidOperationException($"GameplayLab Foot IK regression object '{requiredName}' is duplicated.");
                found = value;
            }
            for (int i = 0; i < value.childCount; i++)
                Find(value.GetChild(i), requiredName, ref found);
        }
    }
}
