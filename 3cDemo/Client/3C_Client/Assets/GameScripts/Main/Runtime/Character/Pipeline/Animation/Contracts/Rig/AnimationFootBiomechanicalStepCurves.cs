using System;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationFootBiomechanicalRouteSample
    {
        public AnimationFootBiomechanicalRouteSample(
            Vector3 rootLocalHeelPosition,
            Vector3 rootLocalToePosition,
            Vector3 rootLocalKneePosition,
            Quaternion rootLocalSoleRotation,
            Quaternion rootLocalAnkleRotation,
            float constraintWeight,
            float supportWeight,
            float supportLegLength,
            float supportLegCompressionReserve,
            Vector3 supportKneeBendPlane,
            Vector3 supportFootPivotPosition,
            float supportFootPivotWeight)
        {
            RootLocalHeelPosition = RequireFinite(rootLocalHeelPosition, nameof(rootLocalHeelPosition));
            RootLocalToePosition = RequireFinite(rootLocalToePosition, nameof(rootLocalToePosition));
            RootLocalKneePosition = RequireFinite(rootLocalKneePosition, nameof(rootLocalKneePosition));
            RootLocalSoleRotation = RequireRotation(rootLocalSoleRotation, nameof(rootLocalSoleRotation));
            RootLocalAnkleRotation = RequireRotation(rootLocalAnkleRotation, nameof(rootLocalAnkleRotation));
            ConstraintWeight = RequireWeight(constraintWeight, nameof(constraintWeight));
            SupportWeight = RequireWeight(supportWeight, nameof(supportWeight));
            SupportLegLength = RequireNonNegative(supportLegLength, nameof(supportLegLength));
            SupportLegCompressionReserve = RequireNonNegative(
                supportLegCompressionReserve,
                nameof(supportLegCompressionReserve));
            SupportKneeBendPlane = RequireFinite(supportKneeBendPlane, nameof(supportKneeBendPlane));
            SupportFootPivotPosition = RequireFinite(
                supportFootPivotPosition,
                nameof(supportFootPivotPosition));
            SupportFootPivotWeight = RequireWeight(
                supportFootPivotWeight,
                nameof(supportFootPivotWeight));
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public Vector3 RootLocalHeelPosition { get; }
        public Vector3 RootLocalToePosition { get; }
        public Vector3 RootLocalKneePosition { get; }
        public Quaternion RootLocalSoleRotation { get; }
        public Quaternion RootLocalAnkleRotation { get; }
        public float ConstraintWeight { get; }
        public float SupportWeight { get; }
        public float SupportLegLength { get; }
        public float SupportLegCompressionReserve { get; }
        public Vector3 SupportKneeBendPlane { get; }
        public Vector3 SupportFootPivotPosition { get; }
        public float SupportFootPivotWeight { get; }
        public bool IsValid => m_IsSpecified != 0;

        public static AnimationFootBiomechanicalRouteSample Interpolate(
            AnimationFootBiomechanicalRouteSample first,
            AnimationFootBiomechanicalRouteSample second,
            float t)
        {
            if (!first.IsValid || !second.IsValid || !float.IsFinite(t))
                throw new ArgumentException("Biomechanical Foot route interpolation is invalid.");
            float value = Mathf.Clamp01(t);
            return new AnimationFootBiomechanicalRouteSample(
                Vector3.Lerp(first.RootLocalHeelPosition, second.RootLocalHeelPosition, value),
                Vector3.Lerp(first.RootLocalToePosition, second.RootLocalToePosition, value),
                Vector3.Lerp(first.RootLocalKneePosition, second.RootLocalKneePosition, value),
                Quaternion.Slerp(first.RootLocalSoleRotation, second.RootLocalSoleRotation, value),
                Quaternion.Slerp(first.RootLocalAnkleRotation, second.RootLocalAnkleRotation, value),
                Mathf.Lerp(first.ConstraintWeight, second.ConstraintWeight, value),
                Mathf.Lerp(first.SupportWeight, second.SupportWeight, value),
                Mathf.Lerp(first.SupportLegLength, second.SupportLegLength, value),
                Mathf.Lerp(
                    first.SupportLegCompressionReserve,
                    second.SupportLegCompressionReserve,
                    value),
                Vector3.Lerp(first.SupportKneeBendPlane, second.SupportKneeBendPlane, value),
                Vector3.Lerp(first.SupportFootPivotPosition, second.SupportFootPivotPosition, value),
                Mathf.Lerp(first.SupportFootPivotWeight, second.SupportFootPivotWeight, value));
        }

        static Vector3 RequireFinite(Vector3 value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentException("Biomechanical Foot route vector is invalid.", field);
            return value;
        }

        static Quaternion RequireRotation(Quaternion value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w) ||
                Quaternion.Dot(value, value) <= 0.000001f)
            {
                throw new ArgumentException("Biomechanical Foot route rotation is invalid.", field);
            }
            return value.normalized;
        }

        static float RequireWeight(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(field);
            return value;
        }

        static float RequireNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(field);
            return value;
        }
    }

    [Serializable]
    public sealed class AnimationFootBiomechanicalStepCurveSet
    {
        [SerializeField] AnimationCurve m_LandingPhase;
        [SerializeField] AnimationCurve m_OpposingRootLocalSoleRotationX;
        [SerializeField] AnimationCurve m_OpposingRootLocalSoleRotationY;
        [SerializeField] AnimationCurve m_OpposingRootLocalSoleRotationZ;
        [SerializeField] AnimationCurve m_OpposingRootLocalSoleRotationW;
        [SerializeField] AnimationCurve[] m_RootLocalHeelRouteX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalHeelRouteY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalHeelRouteZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalToeRouteX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalToeRouteY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalToeRouteZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalKneeRouteX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalKneeRouteY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalKneeRouteZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalSoleRotationX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalSoleRotationY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalSoleRotationZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalSoleRotationW = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalAnkleRotationX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalAnkleRotationY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalAnkleRotationZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalAnkleRotationW = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_ConstraintWeight = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportWeight = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportLegLength = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportLegCompressionReserve = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportKneeBendPlaneX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportKneeBendPlaneY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportKneeBendPlaneZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportFootPivotPositionX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportFootPivotPositionY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportFootPivotPositionZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_SupportFootPivotWeight = Array.Empty<AnimationCurve>();

        public AnimationFootBiomechanicalStepCurveSet(
            AnimationCurve landingPhase,
            AnimationCurve opposingRootLocalSoleRotationX,
            AnimationCurve opposingRootLocalSoleRotationY,
            AnimationCurve opposingRootLocalSoleRotationZ,
            AnimationCurve opposingRootLocalSoleRotationW,
            AnimationCurve[][] vectorAndRotationRoutes,
            AnimationCurve[] constraintWeight,
            AnimationCurve[] supportWeight,
            AnimationCurve[] supportLegLength,
            AnimationCurve[] supportLegCompressionReserve,
            AnimationCurve[] supportFootPivotWeight)
        {
            if (vectorAndRotationRoutes == null || vectorAndRotationRoutes.Length != 23)
                throw new ArgumentException("Biomechanical Foot vector route set is incomplete.", nameof(vectorAndRotationRoutes));
            m_LandingPhase = Copy(landingPhase);
            m_OpposingRootLocalSoleRotationX = Copy(opposingRootLocalSoleRotationX);
            m_OpposingRootLocalSoleRotationY = Copy(opposingRootLocalSoleRotationY);
            m_OpposingRootLocalSoleRotationZ = Copy(opposingRootLocalSoleRotationZ);
            m_OpposingRootLocalSoleRotationW = Copy(opposingRootLocalSoleRotationW);
            m_RootLocalHeelRouteX = CopyRoute(vectorAndRotationRoutes[0]);
            m_RootLocalHeelRouteY = CopyRoute(vectorAndRotationRoutes[1]);
            m_RootLocalHeelRouteZ = CopyRoute(vectorAndRotationRoutes[2]);
            m_RootLocalToeRouteX = CopyRoute(vectorAndRotationRoutes[3]);
            m_RootLocalToeRouteY = CopyRoute(vectorAndRotationRoutes[4]);
            m_RootLocalToeRouteZ = CopyRoute(vectorAndRotationRoutes[5]);
            m_RootLocalKneeRouteX = CopyRoute(vectorAndRotationRoutes[6]);
            m_RootLocalKneeRouteY = CopyRoute(vectorAndRotationRoutes[7]);
            m_RootLocalKneeRouteZ = CopyRoute(vectorAndRotationRoutes[8]);
            m_RootLocalSoleRotationX = CopyRoute(vectorAndRotationRoutes[9]);
            m_RootLocalSoleRotationY = CopyRoute(vectorAndRotationRoutes[10]);
            m_RootLocalSoleRotationZ = CopyRoute(vectorAndRotationRoutes[11]);
            m_RootLocalSoleRotationW = CopyRoute(vectorAndRotationRoutes[12]);
            m_RootLocalAnkleRotationX = CopyRoute(vectorAndRotationRoutes[13]);
            m_RootLocalAnkleRotationY = CopyRoute(vectorAndRotationRoutes[14]);
            m_RootLocalAnkleRotationZ = CopyRoute(vectorAndRotationRoutes[15]);
            m_RootLocalAnkleRotationW = CopyRoute(vectorAndRotationRoutes[16]);
            m_SupportKneeBendPlaneX = CopyRoute(vectorAndRotationRoutes[17]);
            m_SupportKneeBendPlaneY = CopyRoute(vectorAndRotationRoutes[18]);
            m_SupportKneeBendPlaneZ = CopyRoute(vectorAndRotationRoutes[19]);
            m_SupportFootPivotPositionX = CopyRoute(vectorAndRotationRoutes[20]);
            m_SupportFootPivotPositionY = CopyRoute(vectorAndRotationRoutes[21]);
            m_SupportFootPivotPositionZ = CopyRoute(vectorAndRotationRoutes[22]);
            m_ConstraintWeight = CopyRoute(constraintWeight);
            m_SupportWeight = CopyRoute(supportWeight);
            m_SupportLegLength = CopyRoute(supportLegLength);
            m_SupportLegCompressionReserve = CopyRoute(supportLegCompressionReserve);
            m_SupportFootPivotWeight = CopyRoute(supportFootPivotWeight);
            RequireValid();
        }

        public AnimationCurve LandingPhase => m_LandingPhase;
        public AnimationCurve OpposingRootLocalSoleRotationX => m_OpposingRootLocalSoleRotationX;
        public AnimationCurve OpposingRootLocalSoleRotationY => m_OpposingRootLocalSoleRotationY;
        public AnimationCurve OpposingRootLocalSoleRotationZ => m_OpposingRootLocalSoleRotationZ;
        public AnimationCurve OpposingRootLocalSoleRotationW => m_OpposingRootLocalSoleRotationW;
        public AnimationCurve GetRootLocalHeelRoute(int axis, int index) => GetVectorRoute(axis, index, m_RootLocalHeelRouteX, m_RootLocalHeelRouteY, m_RootLocalHeelRouteZ);
        public AnimationCurve GetRootLocalToeRoute(int axis, int index) => GetVectorRoute(axis, index, m_RootLocalToeRouteX, m_RootLocalToeRouteY, m_RootLocalToeRouteZ);
        public AnimationCurve GetRootLocalKneeRoute(int axis, int index) => GetVectorRoute(axis, index, m_RootLocalKneeRouteX, m_RootLocalKneeRouteY, m_RootLocalKneeRouteZ);
        public AnimationCurve GetRootLocalSoleRotationRoute(int axis, int index) => GetQuaternionRoute(axis, index, m_RootLocalSoleRotationX, m_RootLocalSoleRotationY, m_RootLocalSoleRotationZ, m_RootLocalSoleRotationW);
        public AnimationCurve GetRootLocalAnkleRotationRoute(int axis, int index) => GetQuaternionRoute(axis, index, m_RootLocalAnkleRotationX, m_RootLocalAnkleRotationY, m_RootLocalAnkleRotationZ, m_RootLocalAnkleRotationW);
        public AnimationCurve GetConstraintWeight(int index) => GetRoute(m_ConstraintWeight, index);
        public AnimationCurve GetSupportWeight(int index) => GetRoute(m_SupportWeight, index);
        public AnimationCurve GetSupportLegLength(int index) => GetRoute(m_SupportLegLength, index);
        public AnimationCurve GetSupportLegCompressionReserve(int index) => GetRoute(m_SupportLegCompressionReserve, index);
        public AnimationCurve GetSupportKneeBendPlane(int axis, int index) => GetVectorRoute(axis, index, m_SupportKneeBendPlaneX, m_SupportKneeBendPlaneY, m_SupportKneeBendPlaneZ);
        public AnimationCurve GetSupportFootPivotPosition(int axis, int index) => GetVectorRoute(axis, index, m_SupportFootPivotPositionX, m_SupportFootPivotPositionY, m_SupportFootPivotPositionZ);
        public AnimationCurve GetSupportFootPivotWeight(int index) => GetRoute(m_SupportFootPivotWeight, index);

        public void Sample(
            float normalizedTime,
            out float landingPhase,
            out Quaternion opposingRootLocalSoleRotation,
            out FixedList4096Bytes<AnimationFootBiomechanicalRouteSample> route)
        {
            float time = Mathf.Clamp01(normalizedTime);
            landingPhase = m_LandingPhase.Evaluate(time);
            opposingRootLocalSoleRotation = Normalize(new Quaternion(
                m_OpposingRootLocalSoleRotationX.Evaluate(time),
                m_OpposingRootLocalSoleRotationY.Evaluate(time),
                m_OpposingRootLocalSoleRotationZ.Evaluate(time),
                m_OpposingRootLocalSoleRotationW.Evaluate(time)));
            route = default;
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
            {
                route.Add(new AnimationFootBiomechanicalRouteSample(
                    EvaluateVector(time, i, m_RootLocalHeelRouteX, m_RootLocalHeelRouteY, m_RootLocalHeelRouteZ),
                    EvaluateVector(time, i, m_RootLocalToeRouteX, m_RootLocalToeRouteY, m_RootLocalToeRouteZ),
                    EvaluateVector(time, i, m_RootLocalKneeRouteX, m_RootLocalKneeRouteY, m_RootLocalKneeRouteZ),
                    Normalize(EvaluateQuaternion(time, i, m_RootLocalSoleRotationX, m_RootLocalSoleRotationY, m_RootLocalSoleRotationZ, m_RootLocalSoleRotationW)),
                    Normalize(EvaluateQuaternion(time, i, m_RootLocalAnkleRotationX, m_RootLocalAnkleRotationY, m_RootLocalAnkleRotationZ, m_RootLocalAnkleRotationW)),
                    m_ConstraintWeight[i].Evaluate(time),
                    m_SupportWeight[i].Evaluate(time),
                    m_SupportLegLength[i].Evaluate(time),
                    m_SupportLegCompressionReserve[i].Evaluate(time),
                    EvaluateVector(time, i, m_SupportKneeBendPlaneX, m_SupportKneeBendPlaneY, m_SupportKneeBendPlaneZ),
                    EvaluateVector(time, i, m_SupportFootPivotPositionX, m_SupportFootPivotPositionY, m_SupportFootPivotPositionZ),
                    m_SupportFootPivotWeight[i].Evaluate(time)));
            }
        }

        public void RequireValid()
        {
            RequireCurve(m_LandingPhase, nameof(m_LandingPhase), true, false);
            RequireCurve(m_OpposingRootLocalSoleRotationX, nameof(m_OpposingRootLocalSoleRotationX), false, false);
            RequireCurve(m_OpposingRootLocalSoleRotationY, nameof(m_OpposingRootLocalSoleRotationY), false, false);
            RequireCurve(m_OpposingRootLocalSoleRotationZ, nameof(m_OpposingRootLocalSoleRotationZ), false, false);
            RequireCurve(m_OpposingRootLocalSoleRotationW, nameof(m_OpposingRootLocalSoleRotationW), false, false);
            RequireVectorRoute(m_RootLocalHeelRouteX, m_RootLocalHeelRouteY, m_RootLocalHeelRouteZ, "heel");
            RequireVectorRoute(m_RootLocalToeRouteX, m_RootLocalToeRouteY, m_RootLocalToeRouteZ, "toe");
            RequireVectorRoute(m_RootLocalKneeRouteX, m_RootLocalKneeRouteY, m_RootLocalKneeRouteZ, "knee");
            RequireQuaternionRoute(m_RootLocalSoleRotationX, m_RootLocalSoleRotationY, m_RootLocalSoleRotationZ, m_RootLocalSoleRotationW, "sole rotation");
            RequireQuaternionRoute(m_RootLocalAnkleRotationX, m_RootLocalAnkleRotationY, m_RootLocalAnkleRotationZ, m_RootLocalAnkleRotationW, "ankle rotation");
            RequireRoute(m_ConstraintWeight, nameof(m_ConstraintWeight), true, false);
            RequireRoute(m_SupportWeight, nameof(m_SupportWeight), true, false);
            RequireRoute(m_SupportLegLength, nameof(m_SupportLegLength), false, true);
            RequireRoute(m_SupportLegCompressionReserve, nameof(m_SupportLegCompressionReserve), false, true);
            RequireVectorRoute(m_SupportKneeBendPlaneX, m_SupportKneeBendPlaneY, m_SupportKneeBendPlaneZ, "knee bend plane");
            RequireVectorRoute(m_SupportFootPivotPositionX, m_SupportFootPivotPositionY, m_SupportFootPivotPositionZ, "support pivot");
            RequireRoute(m_SupportFootPivotWeight, nameof(m_SupportFootPivotWeight), true, false);
        }

        static void RequireVectorRoute(AnimationCurve[] x, AnimationCurve[] y, AnimationCurve[] z, string field)
        {
            RequireRoute(x, field + " x", false, false);
            RequireRoute(y, field + " y", false, false);
            RequireRoute(z, field + " z", false, false);
        }

        static void RequireQuaternionRoute(AnimationCurve[] x, AnimationCurve[] y, AnimationCurve[] z, AnimationCurve[] w, string field)
        {
            RequireVectorRoute(x, y, z, field);
            RequireRoute(w, field + " w", false, false);
        }

        static void RequireRoute(AnimationCurve[] route, string field, bool normalized, bool nonNegative)
        {
            if (route == null || route.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new InvalidOperationException($"Biomechanical Foot route '{field}' has invalid capacity.");
            for (int i = 0; i < route.Length; i++)
                RequireCurve(route[i], $"{field}[{i}]", normalized, nonNegative);
        }

        static void RequireCurve(AnimationCurve curve, string field, bool normalized, bool nonNegative) =>
            AnimationPredictedFootStepCurveSet.RequireCurve(curve, field, normalized, nonNegative);

        static AnimationCurve Copy(AnimationCurve curve) => AnimationPredictedFootStepCurveSet.Copy(curve);

        static AnimationCurve[] CopyRoute(AnimationCurve[] source)
        {
            if (source == null)
                return null;
            var result = new AnimationCurve[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = Copy(source[i]);
            return result;
        }

        static AnimationCurve GetVectorRoute(int axis, int index, AnimationCurve[] x, AnimationCurve[] y, AnimationCurve[] z) =>
            axis == 0 ? GetRoute(x, index) : axis == 1 ? GetRoute(y, index) : axis == 2 ? GetRoute(z, index) : throw new ArgumentOutOfRangeException(nameof(axis));

        static AnimationCurve GetQuaternionRoute(int axis, int index, AnimationCurve[] x, AnimationCurve[] y, AnimationCurve[] z, AnimationCurve[] w) =>
            axis == 3 ? GetRoute(w, index) : GetVectorRoute(axis, index, x, y, z);

        static AnimationCurve GetRoute(AnimationCurve[] route, int index)
        {
            if (route == null || index < 0 || index >= route.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return route[index];
        }

        static Vector3 EvaluateVector(float time, int index, AnimationCurve[] x, AnimationCurve[] y, AnimationCurve[] z) =>
            new Vector3(x[index].Evaluate(time), y[index].Evaluate(time), z[index].Evaluate(time));

        static Quaternion EvaluateQuaternion(float time, int index, AnimationCurve[] x, AnimationCurve[] y, AnimationCurve[] z, AnimationCurve[] w) =>
            new Quaternion(x[index].Evaluate(time), y[index].Evaluate(time), z[index].Evaluate(time), w[index].Evaluate(time));

        static Quaternion Normalize(Quaternion value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w) ||
                Quaternion.Dot(value, value) <= 0.000001f)
            {
                throw new InvalidOperationException("Biomechanical Foot rotation curve produced an invalid sample.");
            }
            return value.normalized;
        }
    }
}
