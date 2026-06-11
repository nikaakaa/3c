using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [CreateAssetMenu(fileName = "LocomotionMotionProfile", menuName = "3C/Animation/LocomotionMotionProfile")]
    public sealed class LocomotionMotionProfileSO : ScriptableObject
    {
        [SerializeField] BasicMovementPhase phase = BasicMovementPhase.MoveStop;
        [SerializeField] BasicMovementGait gait = BasicMovementGait.Run;
        [SerializeField] string aliasKey = "RunEnd";
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField] AnimationCurve cumulativeLocalX = ConstantZeroCurve();
        [SerializeField] AnimationCurve cumulativeLocalZ = ConstantZeroCurve();
        [SerializeField] AnimationCurve cumulativeYaw = ConstantZeroCurve();
        [SerializeField] string sourceClipName = string.Empty;
        [SerializeField] string sourceClipGuid = string.Empty;

        public BasicMovementPhase Phase => phase;
        public BasicMovementGait Gait => gait;
        public string AliasKey => aliasKey;
        public float Duration => duration;
        public string SourceClipName => sourceClipName;
        public string SourceClipGuid => sourceClipGuid;
        public bool HasValidCurves => HasCurve(cumulativeLocalX) && HasCurve(cumulativeLocalZ) && HasCurve(cumulativeYaw);
        public bool IsValid => duration > 0f && !string.IsNullOrWhiteSpace(aliasKey) && HasValidCurves;

        public Vector3 EvaluateCumulativeLocalPlanarDelta(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return new Vector3(
                cumulativeLocalX != null ? cumulativeLocalX.Evaluate(t) : 0f,
                0f,
                cumulativeLocalZ != null ? cumulativeLocalZ.Evaluate(t) : 0f);
        }

        public float EvaluateCumulativeYaw(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return cumulativeYaw != null ? cumulativeYaw.Evaluate(t) : 0f;
        }

        public void SetBakedData(
            BasicMovementPhase phase,
            string aliasKey,
            float duration,
            AnimationCurve cumulativeLocalX,
            AnimationCurve cumulativeLocalZ,
            AnimationCurve cumulativeYaw,
            string sourceClipName,
            string sourceClipGuid)
        {
            SetBakedData(phase, BasicMovementGait.Run, aliasKey, duration, cumulativeLocalX, cumulativeLocalZ, cumulativeYaw, sourceClipName, sourceClipGuid);
        }

        public void SetBakedData(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            float duration,
            AnimationCurve cumulativeLocalX,
            AnimationCurve cumulativeLocalZ,
            AnimationCurve cumulativeYaw,
            string sourceClipName,
            string sourceClipGuid)
        {
            this.phase = phase;
            this.gait = gait;
            this.aliasKey = aliasKey ?? string.Empty;
            this.duration = Mathf.Max(0f, duration);
            this.cumulativeLocalX = CloneCumulativeOrZero(cumulativeLocalX);
            this.cumulativeLocalZ = CloneCumulativeOrZero(cumulativeLocalZ);
            this.cumulativeYaw = CloneCumulativeOrZero(cumulativeYaw);
            this.sourceClipName = sourceClipName ?? string.Empty;
            this.sourceClipGuid = sourceClipGuid ?? string.Empty;
        }

        static bool HasCurve(AnimationCurve curve)
        {
            return curve != null && curve.length > 0;
        }

        static AnimationCurve CloneCumulativeOrZero(AnimationCurve curve)
        {
            if (curve == null || curve.length <= 0)
                return ConstantZeroCurve();

            Keyframe[] keys = curve.keys;
            if (keys.Length > 1)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    float inSlope = i > 0 ? CalculateSlope(keys[i - 1], keys[i]) : CalculateSlope(keys[i], keys[i + 1]);
                    float outSlope = i < keys.Length - 1 ? CalculateSlope(keys[i], keys[i + 1]) : CalculateSlope(keys[i - 1], keys[i]);
                    Keyframe key = keys[i];
                    key.inTangent = inSlope;
                    key.outTangent = outSlope;
                    key.weightedMode = WeightedMode.None;
                    keys[i] = key;
                }
            }

            return new AnimationCurve(keys);
        }

        static float CalculateSlope(Keyframe from, Keyframe to)
        {
            float timeDelta = to.time - from.time;
            return Mathf.Abs(timeDelta) > 0.000001f ? (to.value - from.value) / timeDelta : 0f;
        }

        static AnimationCurve ConstantZeroCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(1f, 0f, 0f, 0f));
        }
    }
}
