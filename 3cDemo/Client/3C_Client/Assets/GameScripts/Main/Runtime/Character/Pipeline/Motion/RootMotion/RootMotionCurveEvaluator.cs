using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion.RootMotion
{
    public readonly struct RootMotionCurveSample
    {
        public RootMotionCurveSample(float time, Vector3 localPosition, float localYaw)
        {
            Time = time;
            LocalPosition = localPosition;
            LocalYaw = localYaw;
        }

        public float Time { get; }
        public Vector3 LocalPosition { get; }
        public float LocalYaw { get; }
    }

    public readonly struct RootMotionCurveDelta
    {
        public RootMotionCurveDelta(Vector3 localDisplacement, float deltaYaw)
        {
            LocalDisplacement = localDisplacement;
            DeltaYaw = deltaYaw;
        }

        public Vector3 LocalDisplacement { get; }
        public float DeltaYaw { get; }
        public bool HasMotion => LocalDisplacement.sqrMagnitude > 0.0000001f || Mathf.Abs(DeltaYaw) > 0.0001f;
    }

    public static class RootMotionCurveEvaluator
    {
        public static bool TryEvaluateSample(RootMotionCurveAsset asset, float time, out RootMotionCurveSample sample)
        {
            sample = default;
            if (!asset || !asset.TryValidate(out _))
                return false;

            float clampedTime = ClampTime(asset, time);
            Vector3 localPosition;
            switch (asset.EvaluationMode)
            {
                case RootMotionCurveEvaluationMode.FullLocalDelta:
                    localPosition = new Vector3(
                        Evaluate(asset.LocalPositionX, clampedTime),
                        Evaluate(asset.LocalPositionY, clampedTime),
                        Evaluate(asset.LocalPositionZ, clampedTime));
                    break;
                case RootMotionCurveEvaluationMode.ForwardDistanceYaw:
                    localPosition = Vector3.forward * Evaluate(asset.ForwardDistance, clampedTime);
                    break;
                default:
                    return false;
            }

            sample = new RootMotionCurveSample(
                clampedTime,
                localPosition,
                Evaluate(asset.LocalYaw, clampedTime));
            return true;
        }

        public static bool TryEvaluateDelta(RootMotionCurveAsset asset, float previousTime, float currentTime, out RootMotionCurveDelta delta)
        {
            delta = default;
            if (!TryEvaluateSample(asset, previousTime, out RootMotionCurveSample previousSample) ||
                !TryEvaluateSample(asset, currentTime, out RootMotionCurveSample currentSample))
                return false;

            delta = new RootMotionCurveDelta(
                currentSample.LocalPosition - previousSample.LocalPosition,
                currentSample.LocalYaw - previousSample.LocalYaw);
            return true;
        }

        public static Vector3 ToWorldDisplacement(RootMotionCurveDelta delta, Quaternion actorRotation)
        {
            return actorRotation * delta.LocalDisplacement;
        }

        static float ClampTime(RootMotionCurveAsset asset, float time)
        {
            return Mathf.Clamp(time, 0f, Mathf.Max(0f, asset.Duration));
        }

        static float Evaluate(AnimationCurve curve, float time)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(time) : 0f;
        }
    }
}
