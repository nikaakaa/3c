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
            : this(targetPrefab, clip, phase, BasicMovementGait.Run, aliasKey, "Bip001", sampleRate, 0f)
        {
        }

        public LocomotionMotionProfileBakeRequest(
            GameObject targetPrefab,
            AnimationClip clip,
            BasicMovementPhase phase,
            string aliasKey,
            string motionRootPath,
            int sampleRate)
            : this(targetPrefab, clip, phase, BasicMovementGait.Run, aliasKey, motionRootPath, sampleRate, 0f)
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
            : this(targetPrefab, clip, phase, gait, aliasKey, motionRootPath, sampleRate, 0f)
        {
        }

        public LocomotionMotionProfileBakeRequest(
            GameObject targetPrefab,
            AnimationClip clip,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            string motionRootPath,
            int sampleRate,
            float clipEndTime)
        {
            TargetPrefab = targetPrefab;
            Clip = clip;
            Phase = phase;
            Gait = gait;
            AliasKey = aliasKey ?? string.Empty;
            MotionRootPath = motionRootPath ?? string.Empty;
            SampleRate = Mathf.Max(1, sampleRate);
            ClipEndTime = Mathf.Max(0f, clipEndTime);
        }

        public GameObject TargetPrefab { get; }
        public AnimationClip Clip { get; }
        public BasicMovementPhase Phase { get; }
        public BasicMovementGait Gait { get; }
        public string AliasKey { get; }
        public string MotionRootPath { get; }
        public int SampleRate { get; }
        public float ClipEndTime { get; }
    }

    public static class LocomotionMotionProfileBakeUtility
    {
        public static LocomotionMotionProfileSO CreateOrUpdateProfileAsset(
            string assetPath,
            in LocomotionMotionProfileBakeRequest request,
            bool negateYaw = false)
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

            BakeIntoProfile(profile, in request, negateYaw);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        public static void BakeIntoProfile(
            LocomotionMotionProfileSO profile,
            in LocomotionMotionProfileBakeRequest request,
            bool negateYaw = false)
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
                instance = PrefabUtility.InstantiatePrefab(request.TargetPrefab) as GameObject;
                if (instance == null)
                    instance = UnityEngine.Object.Instantiate(request.TargetPrefab);

                instance.hideFlags = HideFlags.HideAndDontSave;
                GameObject sampleRoot = ResolveSampleRoot(instance);
                Transform root = ResolveMotionRoot(sampleRoot, request.MotionRootPath);
                request.Clip.SampleAnimation(sampleRoot, 0f);

                Transform basis = root.parent != null ? root.parent : root;
                Vector3 initialPosition = root.position;
                Quaternion inverseInitialBasisRotation = Quaternion.Inverse(basis.rotation);
                float previousYaw = NormalizeYaw(root.eulerAngles.y);
                float totalYaw = 0f;
                float duration = ResolveBakeDuration(request.Clip, request.ClipEndTime);
                int steps = Mathf.Max(1, Mathf.CeilToInt(duration * request.SampleRate));

                AnimationCurve cumulativeLocalX = new AnimationCurve();
                AnimationCurve cumulativeLocalZ = new AnimationCurve();
                AnimationCurve cumulativeYaw = new AnimationCurve();

                for (int i = 0; i <= steps; i++)
                {
                    float normalizedTime = i / (float)steps;
                    float time = normalizedTime * duration;
                    request.Clip.SampleAnimation(sampleRoot, time);

                    Vector3 localOffset = inverseInitialBasisRotation * (root.position - initialPosition);
                    float currentYaw = NormalizeYaw(root.eulerAngles.y);
                    if (i > 0)
                        totalYaw += Mathf.DeltaAngle(previousYaw, currentYaw);

                    previousYaw = currentYaw;
                    cumulativeLocalX.AddKey(normalizedTime, localOffset.x);
                    cumulativeLocalZ.AddKey(normalizedTime, localOffset.z);
                    cumulativeYaw.AddKey(normalizedTime, negateYaw ? -totalYaw : totalYaw);
                }

                if (TryBakeAnimatorRootCurves(
                        request.Clip,
                        duration,
                        steps,
                        negateYaw,
                        out AnimationCurve animatorRootX,
                        out AnimationCurve animatorRootZ,
                        out AnimationCurve animatorRootYaw))
                {
                    if (HasPlanarMotion(animatorRootX, animatorRootZ))
                    {
                        cumulativeLocalX = animatorRootX;
                        cumulativeLocalZ = animatorRootZ;
                    }

                    if (HasCurveMotion(animatorRootYaw, 0.0001f))
                        cumulativeYaw = animatorRootYaw;
                }

                profile.SetBakedData(
                    request.Phase,
                    request.Gait,
                    request.AliasKey,
                    duration,
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

        static float NormalizeYaw(float yaw)
        {
            return Mathf.Repeat(yaw + 180f, 360f) - 180f;
        }

        static float ResolveBakeDuration(AnimationClip clip, float clipEndTime)
        {
            if (clip == null)
                return 0f;

            if (clipEndTime > 0f)
                return Mathf.Clamp(clipEndTime, 0.001f, Mathf.Max(0.001f, clip.length));

            return Mathf.Max(0.001f, clip.length);
        }

        static bool HasMotion(AnimationCurve cumulativeLocalX, AnimationCurve cumulativeLocalZ, AnimationCurve cumulativeYaw)
        {
            return HasCurveMotion(cumulativeLocalX, 0.000001f) ||
                   HasCurveMotion(cumulativeLocalZ, 0.000001f) ||
                   HasCurveMotion(cumulativeYaw, 0.0001f);
        }

        static bool HasPlanarMotion(AnimationCurve cumulativeLocalX, AnimationCurve cumulativeLocalZ)
        {
            return HasCurveMotion(cumulativeLocalX, 0.000001f) ||
                   HasCurveMotion(cumulativeLocalZ, 0.000001f);
        }

        static bool HasCurveMotion(AnimationCurve curve, float epsilon)
        {
            if (curve == null || curve.length == 0)
                return false;

            float first = curve.keys[0].value;
            for (int i = 1; i < curve.length; i++)
            {
                if (Mathf.Abs(curve.keys[i].value - first) > epsilon)
                    return true;
            }

            return false;
        }

        static bool TryBakeAnimatorRootCurves(
            AnimationClip clip,
            float duration,
            int steps,
            bool negateYaw,
            out AnimationCurve cumulativeLocalX,
            out AnimationCurve cumulativeLocalZ,
            out AnimationCurve cumulativeYaw)
        {
            cumulativeLocalX = null;
            cumulativeLocalZ = null;
            cumulativeYaw = null;

            AnimationCurve rootTx = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootT.x"));
            AnimationCurve rootTz = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootT.z"));
            AnimationCurve rootQx = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.x"));
            AnimationCurve rootQy = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.y"));
            AnimationCurve rootQz = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.z"));
            AnimationCurve rootQw = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.w"));

            if (rootTx == null && rootTz == null && (rootQy == null || rootQw == null))
                return false;

            cumulativeLocalX = new AnimationCurve();
            cumulativeLocalZ = new AnimationCurve();
            cumulativeYaw = new AnimationCurve();

            float startTx = EvaluateOrDefault(rootTx, 0f, 0f);
            float startTz = EvaluateOrDefault(rootTz, 0f, 0f);
            float previousYaw = 0f;
            float totalYaw = 0f;

            for (int i = 0; i <= steps; i++)
            {
                float normalizedTime = i / (float)steps;
                float time = normalizedTime * duration;
                float yaw = EvaluateAnimatorRootYaw(rootQx, rootQy, rootQz, rootQw, time);

                if (i > 0)
                    totalYaw += Mathf.DeltaAngle(previousYaw, yaw);

                previousYaw = yaw;
                cumulativeLocalX.AddKey(normalizedTime, EvaluateOrDefault(rootTx, time, 0f) - startTx);
                cumulativeLocalZ.AddKey(normalizedTime, EvaluateOrDefault(rootTz, time, 0f) - startTz);
                cumulativeYaw.AddKey(normalizedTime, negateYaw ? -totalYaw : totalYaw);
            }

            return HasMotion(cumulativeLocalX, cumulativeLocalZ, cumulativeYaw);
        }

        static float EvaluateOrDefault(AnimationCurve curve, float time, float fallback)
        {
            return curve != null ? curve.Evaluate(time) : fallback;
        }

        static float EvaluateAnimatorRootYaw(
            AnimationCurve rootQx,
            AnimationCurve rootQy,
            AnimationCurve rootQz,
            AnimationCurve rootQw,
            float time)
        {
            float qx = EvaluateOrDefault(rootQx, time, 0f);
            float qy = EvaluateOrDefault(rootQy, time, 0f);
            float qz = EvaluateOrDefault(rootQz, time, 0f);
            float qw = EvaluateOrDefault(rootQw, time, 1f);
            return Mathf.Atan2(2f * (qw * qy + qx * qz), 1f - 2f * (qy * qy + qz * qz)) * Mathf.Rad2Deg;
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
