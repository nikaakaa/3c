using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.RootMotion
{
    public static class RootMotionCurveBakingService
    {
        public static void Bake(
            AnimationClip clip,
            GameObject sampleObject,
            RuntimeAnimatorController controller,
            RootMotionCurveAsset target,
            float sampleRate,
            RootMotionCurveEvaluationMode evaluationMode)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!sampleObject)
                throw new ArgumentNullException(nameof(sampleObject));
            if (!target)
                throw new ArgumentNullException(nameof(target));
            if (sampleRate <= 0f)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (!RootMotionCurveAsset.TryValidateEvaluationMode(evaluationMode, out string error))
                throw new ArgumentException(error, nameof(evaluationMode));

            BakeCurves(
                clip,
                sampleObject,
                controller,
                sampleRate,
                evaluationMode,
                out AnimationCurve x,
                out AnimationCurve y,
                out AnimationCurve z,
                out AnimationCurve forwardDistance,
                out AnimationCurve yaw,
                out Vector3 totalPosition,
                out float totalForwardDistance,
                out float totalYaw);

            target.SetBakedData(
                clip,
                Mathf.Max(0f, clip.length),
                sampleRate,
                evaluationMode,
                x,
                y,
                z,
                forwardDistance,
                yaw,
                totalPosition,
                totalForwardDistance,
                totalYaw);
            EditorUtility.SetDirty(target);
        }

        static void BakeCurves(
            AnimationClip clip,
            GameObject sampleObject,
            RuntimeAnimatorController controller,
            float sampleRate,
            RootMotionCurveEvaluationMode evaluationMode,
            out AnimationCurve x,
            out AnimationCurve y,
            out AnimationCurve z,
            out AnimationCurve forwardDistance,
            out AnimationCurve yaw,
            out Vector3 totalPosition,
            out float totalForwardDistance,
            out float totalYaw)
        {
            x = new AnimationCurve();
            y = new AnimationCurve();
            z = new AnimationCurve();
            forwardDistance = new AnimationCurve();
            yaw = new AnimationCurve();
            totalPosition = Vector3.zero;
            totalForwardDistance = 0f;
            totalYaw = 0f;

            GameObject instance = null;
            AnimatorOverrideController overrideController = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(sampleObject, Vector3.zero, Quaternion.identity);
                instance.hideFlags = HideFlags.HideAndDontSave;

                Animator animator = instance.GetComponentInChildren<Animator>(true);
                if (!animator)
                    throw new InvalidOperationException("Sample object requires an Animator.");

                RuntimeAnimatorController sourceController = controller ? controller : animator.runtimeAnimatorController;
                if (!sourceController)
                    throw new InvalidOperationException("Root motion baking requires an explicit RuntimeAnimatorController.");

                overrideController = new AnimatorOverrideController(sourceController)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                AnimationClip[] controllerClips = overrideController.animationClips;
                if (controllerClips == null || controllerClips.Length == 0)
                    throw new InvalidOperationException("RuntimeAnimatorController contains no replaceable AnimationClip.");

                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(controllerClips.Length);
                for (int i = 0; i < controllerClips.Length; i++)
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(controllerClips[i], clip));

                overrideController.ApplyOverrides(overrides);
                animator.runtimeAnimatorController = overrideController;
                animator.applyRootMotion = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Update(0f);

                float duration = Mathf.Max(0f, clip.length);
                int frameCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
                float defaultDeltaTime = 1f / sampleRate;

                AddKey(x, y, z, forwardDistance, yaw, 0f, totalPosition, totalForwardDistance, totalYaw);
                for (int i = 1; i <= frameCount; i++)
                {
                    float previousTime = Mathf.Min((i - 1) * defaultDeltaTime, duration);
                    float currentTime = Mathf.Min(i * defaultDeltaTime, duration);
                    float deltaTime = Mathf.Max(0f, currentTime - previousTime);
                    if (deltaTime <= 0f)
                        continue;

                    Quaternion previousRotation = instance.transform.rotation;
                    animator.Update(deltaTime);

                    Vector3 localDelta = Quaternion.Inverse(previousRotation) * animator.deltaPosition;
                    float deltaYaw = ExtractYaw(animator.deltaRotation);
                    if (evaluationMode == RootMotionCurveEvaluationMode.ForwardDistanceYaw)
                    {
                        totalForwardDistance += new Vector2(localDelta.x, localDelta.z).magnitude;
                        totalPosition = Vector3.forward * totalForwardDistance;
                    }
                    else
                    {
                        totalPosition += localDelta;
                    }

                    totalYaw += deltaYaw;
                    AddKey(x, y, z, forwardDistance, yaw, currentTime, totalPosition, totalForwardDistance, totalYaw);
                }
            }
            finally
            {
                if (overrideController)
                    UnityEngine.Object.DestroyImmediate(overrideController);
                if (instance)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static void AddKey(
            AnimationCurve x,
            AnimationCurve y,
            AnimationCurve z,
            AnimationCurve forwardDistance,
            AnimationCurve yaw,
            float time,
            Vector3 position,
            float distance,
            float localYaw)
        {
            x.AddKey(time, position.x);
            y.AddKey(time, position.y);
            z.AddKey(time, position.z);
            forwardDistance.AddKey(time, distance);
            yaw.AddKey(time, localYaw);
        }

        static float ExtractYaw(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            return forward.sqrMagnitude <= 0.0000001f
                ? 0f
                : Vector3.SignedAngle(Vector3.forward, forward.normalized, Vector3.up);
        }
    }
}
