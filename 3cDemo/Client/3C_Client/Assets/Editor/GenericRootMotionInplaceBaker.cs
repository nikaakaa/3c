using System;
using System.IO;
using ThirdPersonAnimation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonAnimation.EditorTools
{
    public enum GenericRootTransformBakeMode
    {
        PreserveRootTransform = 0,
        CompensateRootTransform = 1,
    }

    public readonly struct GenericRootMotionInplaceBakeRequest
    {
        public GenericRootMotionInplaceBakeRequest(
            AnimationClip sourceClip,
            LocomotionMotionProfileSO motionProfile,
            string rootPath,
            int sampleRate,
            float duration = 0f,
            string clipName = null,
            GenericRootTransformBakeMode rootTransformMode = GenericRootTransformBakeMode.PreserveRootTransform)
        {
            SourceClip = sourceClip;
            MotionProfile = motionProfile;
            RootPath = rootPath ?? string.Empty;
            SampleRate = sampleRate;
            Duration = duration;
            ClipName = clipName ?? string.Empty;
            RootTransformMode = rootTransformMode;
        }

        public AnimationClip SourceClip { get; }
        public LocomotionMotionProfileSO MotionProfile { get; }
        public string RootPath { get; }
        public int SampleRate { get; }
        public float Duration { get; }
        public string ClipName { get; }
        public GenericRootTransformBakeMode RootTransformMode { get; }
    }

    public static class GenericRootMotionInplaceBaker
    {
        public static AnimationClip CreateOrUpdateClipAsset(string assetPath, in GenericRootMotionInplaceBakeRequest request)
        {
            string normalizedPath = NormalizeAssetPath(assetPath);
            EnsureAssetFolder(normalizedPath);

            AnimationClip generated = BuildInplaceClip(in request);
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(normalizedPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, normalizedPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(normalizedPath);
            }

            EditorUtility.CopySerialized(generated, existing);
            existing.name = generated.name;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return existing;
        }

        public static AnimationClip BuildInplaceClip(in GenericRootMotionInplaceBakeRequest request)
        {
            if (request.SourceClip == null)
                throw new ArgumentNullException(nameof(request.SourceClip));

            if (request.MotionProfile == null || !request.MotionProfile.IsValid)
                throw new ArgumentException("Motion profile is missing or invalid.", nameof(request));

            if (string.IsNullOrWhiteSpace(request.RootPath))
                throw new ArgumentException("Root path is missing.", nameof(request));

            AnimationClip source = request.SourceClip;
            float duration = ResolveDuration(source, request.MotionProfile, request.Duration);
            int sampleRate = ResolveSampleRate(source, request.SampleRate);
            int steps = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
            string clipName = string.IsNullOrWhiteSpace(request.ClipName)
                ? source.name + "_ProfileCompensatedInplace"
                : request.ClipName;

            var output = new AnimationClip
            {
                name = clipName,
                frameRate = sampleRate,
                wrapMode = source.wrapMode,
                legacy = source.legacy,
            };

            AnimationUtility.SetAnimationClipSettings(output, AnimationUtility.GetAnimationClipSettings(source));
            CopyNonCompensatedCurves(source, output, request.RootPath, request.RootTransformMode);
            CopyObjectReferenceCurves(source, output);
            AnimationUtility.SetAnimationEvents(output, AnimationUtility.GetAnimationEvents(source));
            WriteNeutralAnimatorRootMotion(output, duration);
            if (request.RootTransformMode == GenericRootTransformBakeMode.CompensateRootTransform)
                WriteCompensatedRootCurves(source, output, request.MotionProfile, request.RootPath, duration, steps);

            output.EnsureQuaternionContinuity();
            return output;
        }

        static void CopyNonCompensatedCurves(
            AnimationClip source,
            AnimationClip output,
            string rootPath,
            GenericRootTransformBakeMode rootTransformMode)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                if (IsAnimatorRootMotionBinding(binding))
                    continue;

                if (rootTransformMode == GenericRootTransformBakeMode.CompensateRootTransform &&
                    IsRootTransformBinding(binding, rootPath))
                    continue;

                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationUtility.SetEditorCurve(output, binding, CopyCurve(curve));
            }
        }

        static void CopyObjectReferenceCurves(AnimationClip source, AnimationClip output)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(source, binding);
                AnimationUtility.SetObjectReferenceCurve(output, binding, curve);
            }
        }

        static void WriteCompensatedRootCurves(
            AnimationClip source,
            AnimationClip output,
            LocomotionMotionProfileSO motionProfile,
            string rootPath,
            float duration,
            int steps)
        {
            RootTransformCurves sourceRoot = RootTransformCurves.Load(source, rootPath);
            var posX = new Keyframe[steps + 1];
            var posY = new Keyframe[steps + 1];
            var posZ = new Keyframe[steps + 1];
            var rotX = new Keyframe[steps + 1];
            var rotY = new Keyframe[steps + 1];
            var rotZ = new Keyframe[steps + 1];
            var rotW = new Keyframe[steps + 1];
            Vector3 initialSourcePosition = sourceRoot.EvaluatePosition(0f);
            Quaternion previousSourceRotation = Quaternion.identity;
            Quaternion previousOutputRotation = Quaternion.identity;
            bool hasPreviousSourceRotation = false;
            bool hasPreviousOutputRotation = false;

            for (int i = 0; i <= steps; i++)
            {
                float normalizedTime = i / (float)steps;
                float time = normalizedTime * duration;
                Vector3 sourcePosition = sourceRoot.EvaluatePosition(time);
                Quaternion sourceRotation = sourceRoot.EvaluateRotation(time);
                sourceRotation = NormalizeQuaternion(sourceRotation);
                if (hasPreviousSourceRotation && Quaternion.Dot(previousSourceRotation, sourceRotation) < 0f)
                    sourceRotation = Negate(sourceRotation);

                previousSourceRotation = sourceRotation;
                hasPreviousSourceRotation = true;

                Vector3 profilePosition = motionProfile.EvaluateCumulativeLocalPlanarDelta(normalizedTime);
                Quaternion inverseProfileRotation = Quaternion.Inverse(Quaternion.Euler(0f, motionProfile.EvaluateCumulativeYaw(normalizedTime), 0f));
                Vector3 outputPosition = initialSourcePosition + inverseProfileRotation * (sourcePosition - initialSourcePosition - profilePosition);
                Quaternion outputRotation = NormalizeQuaternion(inverseProfileRotation * sourceRotation);
                if (hasPreviousOutputRotation && Quaternion.Dot(previousOutputRotation, outputRotation) < 0f)
                    outputRotation = Negate(outputRotation);

                previousOutputRotation = outputRotation;
                hasPreviousOutputRotation = true;

                posX[i] = new Keyframe(time, outputPosition.x);
                posY[i] = new Keyframe(time, outputPosition.y);
                posZ[i] = new Keyframe(time, outputPosition.z);
                rotX[i] = new Keyframe(time, outputRotation.x);
                rotY[i] = new Keyframe(time, outputRotation.y);
                rotZ[i] = new Keyframe(time, outputRotation.z);
                rotW[i] = new Keyframe(time, outputRotation.w);
            }

            SetTransformCurve(output, rootPath, "m_LocalPosition.x", CreateLinearCurve(posX));
            SetTransformCurve(output, rootPath, "m_LocalPosition.y", CreateLinearCurve(posY));
            SetTransformCurve(output, rootPath, "m_LocalPosition.z", CreateLinearCurve(posZ));
            SetTransformCurve(output, rootPath, "m_LocalRotation.x", CreateLinearCurve(rotX));
            SetTransformCurve(output, rootPath, "m_LocalRotation.y", CreateLinearCurve(rotY));
            SetTransformCurve(output, rootPath, "m_LocalRotation.z", CreateLinearCurve(rotZ));
            SetTransformCurve(output, rootPath, "m_LocalRotation.w", CreateLinearCurve(rotW));
        }

        static void WriteNeutralAnimatorRootMotion(AnimationClip output, float duration)
        {
            SetAnimatorCurve(output, "RootT.x", ConstantCurve(duration, 0f));
            SetAnimatorCurve(output, "RootT.y", ConstantCurve(duration, 0f));
            SetAnimatorCurve(output, "RootT.z", ConstantCurve(duration, 0f));
            SetAnimatorCurve(output, "RootQ.x", ConstantCurve(duration, 0f));
            SetAnimatorCurve(output, "RootQ.y", ConstantCurve(duration, 0f));
            SetAnimatorCurve(output, "RootQ.z", ConstantCurve(duration, 0f));
            SetAnimatorCurve(output, "RootQ.w", ConstantCurve(duration, 1f));
        }

        static bool IsAnimatorRootMotionBinding(EditorCurveBinding binding)
        {
            if (binding.path != string.Empty || binding.type != typeof(Animator))
                return false;

            return binding.propertyName == "RootT.x" ||
                   binding.propertyName == "RootT.y" ||
                   binding.propertyName == "RootT.z" ||
                   binding.propertyName == "RootQ.x" ||
                   binding.propertyName == "RootQ.y" ||
                   binding.propertyName == "RootQ.z" ||
                   binding.propertyName == "RootQ.w";
        }

        static bool IsRootTransformBinding(EditorCurveBinding binding, string rootPath)
        {
            if (binding.type != typeof(Transform) || !string.Equals(NormalizePath(binding.path), NormalizePath(rootPath), StringComparison.Ordinal))
                return false;

            string propertyName = binding.propertyName ?? string.Empty;
            return propertyName == "m_LocalPosition.x" ||
                   propertyName == "m_LocalPosition.y" ||
                   propertyName == "m_LocalPosition.z" ||
                   propertyName == "m_LocalRotation.x" ||
                   propertyName == "m_LocalRotation.y" ||
                   propertyName == "m_LocalRotation.z" ||
                   propertyName == "m_LocalRotation.w" ||
                   propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal);
        }

        static AnimationCurve CreateLinearCurve(Keyframe[] keys)
        {
            if (keys == null || keys.Length == 0)
                return new AnimationCurve();

            if (keys.Length > 1)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    float inSlope = i > 0 ? CalculateSlope(keys[i - 1], keys[i]) : CalculateSlope(keys[i], keys[i + 1]);
                    float outSlope = i < keys.Length - 1 ? CalculateSlope(keys[i], keys[i + 1]) : CalculateSlope(keys[i - 1], keys[i]);
                    keys[i].inTangent = inSlope;
                    keys[i].outTangent = outSlope;
                    keys[i].weightedMode = WeightedMode.None;
                }
            }

            return new AnimationCurve(keys);
        }

        static float CalculateSlope(Keyframe from, Keyframe to)
        {
            float timeDelta = to.time - from.time;
            return Mathf.Abs(timeDelta) > 0.000001f ? (to.value - from.value) / timeDelta : 0f;
        }

        static AnimationCurve ConstantCurve(float duration, float value)
        {
            return new AnimationCurve(
                new Keyframe(0f, value, 0f, 0f),
                new Keyframe(Mathf.Max(0.001f, duration), value, 0f, 0f));
        }

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                return null;

            var output = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode,
            };
            return output;
        }

        static Quaternion NormalizeQuaternion(Quaternion value)
        {
            float magnitude = Mathf.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            if (magnitude <= 0.000001f)
                return Quaternion.identity;

            float inverse = 1f / magnitude;
            return new Quaternion(value.x * inverse, value.y * inverse, value.z * inverse, value.w * inverse);
        }

        static Quaternion Negate(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        static void SetAnimatorCurve(AnimationClip clip, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName), curve);
        }

        static void SetTransformCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName), curve);
        }

        static float ResolveDuration(AnimationClip source, LocomotionMotionProfileSO motionProfile, float requestedDuration)
        {
            if (requestedDuration > 0f)
                return Mathf.Clamp(requestedDuration, 0.001f, Mathf.Max(0.001f, source.length));

            if (motionProfile != null && motionProfile.Duration > 0f)
                return Mathf.Clamp(motionProfile.Duration, 0.001f, Mathf.Max(0.001f, source.length));

            return Mathf.Max(0.001f, source.length);
        }

        static int ResolveSampleRate(AnimationClip source, int requestedSampleRate)
        {
            if (requestedSampleRate > 0)
                return Mathf.Clamp(requestedSampleRate, 1, 120);

            return Mathf.Clamp(Mathf.RoundToInt(source.frameRate > 0f ? source.frameRate : 60f), 1, 120);
        }

        static string NormalizeAssetPath(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is missing.", nameof(outputPath));

            string normalized = outputPath.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Output path must be under Assets/.", nameof(outputPath));

            return normalized.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : normalized + ".anim";
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

        static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim('/');
        }

        readonly struct RootTransformCurves
        {
            readonly AnimationCurve posX;
            readonly AnimationCurve posY;
            readonly AnimationCurve posZ;
            readonly AnimationCurve rotX;
            readonly AnimationCurve rotY;
            readonly AnimationCurve rotZ;
            readonly AnimationCurve rotW;

            RootTransformCurves(
                AnimationCurve posX,
                AnimationCurve posY,
                AnimationCurve posZ,
                AnimationCurve rotX,
                AnimationCurve rotY,
                AnimationCurve rotZ,
                AnimationCurve rotW)
            {
                this.posX = posX;
                this.posY = posY;
                this.posZ = posZ;
                this.rotX = rotX;
                this.rotY = rotY;
                this.rotZ = rotZ;
                this.rotW = rotW;
            }

            public static RootTransformCurves Load(AnimationClip clip, string rootPath)
            {
                return new RootTransformCurves(
                    GetCurve(clip, rootPath, "m_LocalPosition.x"),
                    GetCurve(clip, rootPath, "m_LocalPosition.y"),
                    GetCurve(clip, rootPath, "m_LocalPosition.z"),
                    GetCurve(clip, rootPath, "m_LocalRotation.x"),
                    GetCurve(clip, rootPath, "m_LocalRotation.y"),
                    GetCurve(clip, rootPath, "m_LocalRotation.z"),
                    GetCurve(clip, rootPath, "m_LocalRotation.w"));
            }

            public Vector3 EvaluatePosition(float time)
            {
                return new Vector3(
                    EvaluateOrDefault(posX, time, 0f),
                    EvaluateOrDefault(posY, time, 0f),
                    EvaluateOrDefault(posZ, time, 0f));
            }

            public Quaternion EvaluateRotation(float time)
            {
                return new Quaternion(
                    EvaluateOrDefault(rotX, time, 0f),
                    EvaluateOrDefault(rotY, time, 0f),
                    EvaluateOrDefault(rotZ, time, 0f),
                    EvaluateOrDefault(rotW, time, 1f));
            }

            static AnimationCurve GetCurve(AnimationClip clip, string path, string propertyName)
            {
                return AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            }

            static float EvaluateOrDefault(AnimationCurve curve, float time, float fallback)
            {
                return curve != null ? curve.Evaluate(time) : fallback;
            }
        }
    }

    public static class CorinTurnBackProfileCompensatedInplaceBaker
    {
        public const string SourceClipPath = "Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponRootmotion/Corin_TurnBack_WithWeaponRootmotion.anim";
        public const string MotionProfilePath = "Assets/Configs/3C/Animation/Locomotion/Corin/Bake/TestTurnback614.asset";
        public const string OutputClipPath = "Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponInplace/Corin_TurnBack_NoRootTurn_WithWeaponInplace.anim";
        public const string RootPath = "Bip001";

        [MenuItem("Tools/3C/Corin/Build TurnBack Profile Compensated Inplace Clip")]
        public static void BuildDefaultTurnBack()
        {
            AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceClipPath);
            LocomotionMotionProfileSO profile = AssetDatabase.LoadAssetAtPath<LocomotionMotionProfileSO>(MotionProfilePath);
            var request = new GenericRootMotionInplaceBakeRequest(
                source,
                profile,
                RootPath,
                0,
                0f,
                Path.GetFileNameWithoutExtension(OutputClipPath),
                GenericRootTransformBakeMode.CompensateRootTransform);

            AnimationClip output = GenericRootMotionInplaceBaker.CreateOrUpdateClipAsset(OutputClipPath, in request);
            Selection.activeObject = output;
            EditorGUIUtility.PingObject(output);
            Debug.Log($"[CorinTurnBackProfileCompensatedInplaceBaker] Built {OutputClipPath} from {SourceClipPath} profile={MotionProfilePath} rootPath={RootPath}.");
        }
    }
}
