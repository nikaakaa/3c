using System;
using System.IO;
using ThirdPersonAnimation;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonAnimation.EditorTools
{
    public readonly struct LocomotionMotionProfileBakeRequest
    {
        public LocomotionMotionProfileBakeRequest(
            GameObject targetPrefab,
            AnimationClip clip,
            BasicMovementPhase phase,
            string aliasKey,
            int sampleRate)
            : this(targetPrefab, clip, phase, BasicMovementGait.Run, aliasKey, "Bip001", sampleRate)
        {
        }

        public LocomotionMotionProfileBakeRequest(
            GameObject targetPrefab,
            AnimationClip clip,
            BasicMovementPhase phase,
            string aliasKey,
            string motionRootPath,
            int sampleRate)
            : this(targetPrefab, clip, phase, BasicMovementGait.Run, aliasKey, motionRootPath, sampleRate)
        {
        }

        public LocomotionMotionProfileBakeRequest(
            GameObject targetPrefab,
            AnimationClip clip,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            string motionRootPath,
            int sampleRate)
        {
            TargetPrefab = targetPrefab;
            Clip = clip;
            Phase = phase;
            Gait = gait;
            AliasKey = aliasKey ?? string.Empty;
            MotionRootPath = motionRootPath ?? string.Empty;
            SampleRate = Mathf.Max(1, sampleRate);
        }

        public GameObject TargetPrefab { get; }
        public AnimationClip Clip { get; }
        public BasicMovementPhase Phase { get; }
        public BasicMovementGait Gait { get; }
        public string AliasKey { get; }
        public string MotionRootPath { get; }
        public int SampleRate { get; }
    }

    public static class LocomotionMotionProfileBakeUtility
    {
        public static LocomotionMotionProfileSO CreateOrUpdateProfileAsset(
            string assetPath,
            in LocomotionMotionProfileBakeRequest request)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException("Output asset path is missing.", nameof(assetPath));

            string normalizedPath = NormalizeAssetPath(assetPath);
            EnsureAssetFolder(normalizedPath);

            LocomotionMotionProfileSO profile = AssetDatabase.LoadAssetAtPath<LocomotionMotionProfileSO>(normalizedPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();
                AssetDatabase.CreateAsset(profile, normalizedPath);
            }

            BakeIntoProfile(profile, in request);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        public static void BakeIntoProfile(
            LocomotionMotionProfileSO profile,
            in LocomotionMotionProfileBakeRequest request)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (request.TargetPrefab == null)
                throw new ArgumentException("Target prefab is missing.", nameof(request));

            if (request.Clip == null)
                throw new ArgumentException("Animation clip is missing.", nameof(request));

            if (string.IsNullOrWhiteSpace(request.AliasKey))
                throw new ArgumentException("Alias key is missing.", nameof(request));

            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(request.TargetPrefab);
                if (instance == null)
                    instance = UnityEngine.Object.Instantiate(request.TargetPrefab);

                instance.hideFlags = HideFlags.HideAndDontSave;
                GameObject sampleRoot = ResolveSampleRoot(instance);
                Transform root = ResolveMotionRoot(sampleRoot, request.MotionRootPath);
                request.Clip.SampleAnimation(sampleRoot, 0f);
                Transform basis = root.parent != null ? root.parent : root;
                Transform yawRoot = sampleRoot.transform;
                Vector3 initialPosition = root.position;
                Quaternion initialYawRotation = yawRoot.rotation;
                Quaternion inverseInitialBasisRotation = Quaternion.Inverse(basis.rotation);

                AnimationCurve cumulativeLocalX = new AnimationCurve();
                AnimationCurve cumulativeLocalZ = new AnimationCurve();
                AnimationCurve cumulativeYaw = new AnimationCurve();
                int steps = Mathf.Max(1, Mathf.CeilToInt(request.Clip.length * request.SampleRate));

                for (int i = 0; i <= steps; i++)
                {
                    float normalizedTime = i / (float)steps;
                    float time = Mathf.Clamp01(normalizedTime) * request.Clip.length;
                    request.Clip.SampleAnimation(sampleRoot, time);

                    Vector3 localOffset = inverseInitialBasisRotation * (root.position - initialPosition);
                    float yaw = Mathf.DeltaAngle(initialYawRotation.eulerAngles.y, yawRoot.eulerAngles.y);
                    cumulativeLocalX.AddKey(normalizedTime, localOffset.x);
                    cumulativeLocalZ.AddKey(normalizedTime, localOffset.z);
                    cumulativeYaw.AddKey(normalizedTime, yaw);
                }

                profile.SetBakedData(
                    request.Phase,
                    request.Gait,
                    request.AliasKey,
                    request.Clip.length,
                    cumulativeLocalX,
                    cumulativeLocalZ,
                    cumulativeYaw,
                    request.Clip.name,
                    ResolveClipGuid(request.Clip));

                EditorUtility.SetDirty(profile);
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static GameObject ResolveSampleRoot(GameObject instance)
        {
            Animator animator = instance.GetComponentInChildren<Animator>();
            return animator != null ? animator.gameObject : instance;
        }

        static Transform ResolveMotionRoot(GameObject sampleRoot, string motionRootPath)
        {
            Transform root = null;
            if (!string.IsNullOrWhiteSpace(motionRootPath))
            {
                root = sampleRoot.transform.Find(motionRootPath);
                if (root == null)
                    root = FindChildByName(sampleRoot.transform, motionRootPath);
            }

            if (root != null)
                return root;

            root = FindChildByName(sampleRoot.transform, "Bip001");
            if (root != null)
                return root;

            return sampleRoot.transform;
        }

        static Transform FindChildByName(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
                return null;

            string expectedName = childName.Contains("/") ? childName.Substring(childName.LastIndexOf('/') + 1) : childName;
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (string.Equals(children[i].name, expectedName, StringComparison.Ordinal))
                    return children[i];
            }

            return null;
        }

        static string ResolveClipGuid(AnimationClip clip)
        {
            string clipPath = AssetDatabase.GetAssetPath(clip);
            return string.IsNullOrWhiteSpace(clipPath) ? string.Empty : AssetDatabase.AssetPathToGUID(clipPath);
        }

        static string NormalizeAssetPath(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Output asset path must be under Assets/.", nameof(assetPath));

            return normalized.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ? normalized : normalized + ".asset";
        }

        static void EnsureAssetFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}
