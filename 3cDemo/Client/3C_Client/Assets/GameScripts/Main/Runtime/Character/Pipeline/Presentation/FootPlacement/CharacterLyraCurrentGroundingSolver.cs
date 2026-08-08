using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterLyraFootTraceResult
    {
        internal CharacterLyraFootTraceResult(
            CharacterFootSide side,
            CharacterFootPlacementQueryRequest request,
            CharacterFootPlacementQueryHit hit,
            Vector3 animatedAnklePosition,
            float targetOffset)
        {
            Side = side;
            Request = request;
            Hit = hit;
            AnimatedAnklePosition = animatedAnklePosition;
            TargetOffset = targetOffset;
        }

        internal CharacterFootSide Side { get; }
        internal CharacterFootPlacementQueryRequest Request { get; }
        internal CharacterFootPlacementQueryHit Hit { get; }
        internal Vector3 AnimatedAnklePosition { get; }
        internal float TargetOffset { get; }
        internal bool DidTraceHit => Hit.HasHit;
    }

    internal readonly struct CharacterLyraCurrentGroundingTrace
    {
        internal CharacterLyraCurrentGroundingTrace(
            CharacterLyraFootTraceResult left,
            CharacterLyraFootTraceResult right,
            float pelvisTargetOffset)
        {
            Left = left;
            Right = right;
            PelvisTargetOffset = pelvisTargetOffset;
        }

        internal CharacterLyraFootTraceResult Left { get; }
        internal CharacterLyraFootTraceResult Right { get; }
        internal float PelvisTargetOffset { get; }
    }

    internal readonly struct CharacterLyraCurrentGroundingFootResult
    {
        internal CharacterLyraCurrentGroundingFootResult(
            CharacterLyraFootTraceResult trace,
            Vector3 componentPosition,
            Quaternion componentRotation,
            float soleClearanceTarget,
            float offsetTarget,
            float unconstrainedOffset,
            float soleConstraintOffset,
            float currentOffset,
            float offsetVelocity,
            float previousOffsetTarget,
            bool offsetSpringInitialized,
            Vector3 currentHitNormal,
            Vector3 normalVelocity,
            Vector3 previousNormalTarget,
            bool normalSpringInitialized)
        {
            Trace = trace;
            ComponentPosition = componentPosition;
            ComponentRotation = componentRotation;
            SoleClearanceTarget = soleClearanceTarget;
            OffsetTarget = offsetTarget;
            UnconstrainedOffset = unconstrainedOffset;
            SoleConstraintOffset = soleConstraintOffset;
            CurrentOffset = currentOffset;
            OffsetVelocity = offsetVelocity;
            PreviousOffsetTarget = previousOffsetTarget;
            OffsetSpringInitialized = offsetSpringInitialized;
            CurrentHitNormal = currentHitNormal;
            NormalVelocity = normalVelocity;
            PreviousNormalTarget = previousNormalTarget;
            NormalSpringInitialized = normalSpringInitialized;
        }

        internal CharacterLyraFootTraceResult Trace { get; }
        internal Vector3 ComponentPosition { get; }
        internal Quaternion ComponentRotation { get; }
        internal float SoleClearanceTarget { get; }
        internal float OffsetTarget { get; }
        internal float UnconstrainedOffset { get; }
        internal float SoleConstraintOffset { get; }
        internal float CurrentOffset { get; }
        internal float OffsetVelocity { get; }
        internal float PreviousOffsetTarget { get; }
        internal bool OffsetSpringInitialized { get; }
        internal Vector3 CurrentHitNormal { get; }
        internal Vector3 NormalVelocity { get; }
        internal Vector3 PreviousNormalTarget { get; }
        internal bool NormalSpringInitialized { get; }
    }

    internal readonly struct CharacterLyraCurrentGroundingResult
    {
        internal CharacterLyraCurrentGroundingResult(
            CharacterLyraCurrentGroundingFootResult left,
            CharacterLyraCurrentGroundingFootResult right,
            float targetPelvisOffset,
            float currentPelvisOffset,
            float pelvisVelocity,
            float previousPelvisTarget,
            bool pelvisSpringInitialized)
        {
            Left = left;
            Right = right;
            TargetPelvisOffset = targetPelvisOffset;
            CurrentPelvisOffset = currentPelvisOffset;
            PelvisVelocity = pelvisVelocity;
            PreviousPelvisTarget = previousPelvisTarget;
            PelvisSpringInitialized = pelvisSpringInitialized;
        }

        internal CharacterLyraCurrentGroundingFootResult Left { get; }
        internal CharacterLyraCurrentGroundingFootResult Right { get; }
        internal float TargetPelvisOffset { get; }
        internal float CurrentPelvisOffset { get; }
        internal float PelvisVelocity { get; }
        internal float PreviousPelvisTarget { get; }
        internal bool PelvisSpringInitialized { get; }
    }

    internal sealed class CharacterLyraCurrentGroundingSolver
    {
        internal const string SourceIdentity = "lyra-5.7/ABP_Mannequin_Base/CR_Mannequin_FootPlant";
        internal const string SpringIdentity = "unreal-engine-5.7/SpringInterpV2/FMath.SpringDamper";
        sealed class FloatSpringState
        {
            internal float Value;
            internal float Velocity;
            internal float PreviousTarget;
            internal bool Initialized;

            internal void Reset()
            {
                Value = 0f;
                Velocity = 0f;
                PreviousTarget = 0f;
                Initialized = false;
            }
        }

        sealed class VectorSpringState
        {
            internal Vector3 Value;
            internal Vector3 Velocity;
            internal Vector3 PreviousTarget;
            internal bool Initialized;

            internal void Reset()
            {
                Value = Vector3.zero;
                Velocity = Vector3.zero;
                PreviousTarget = Vector3.zero;
                Initialized = false;
            }
        }

        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly FloatSpringState m_LeftOffset = new FloatSpringState();
        readonly FloatSpringState m_RightOffset = new FloatSpringState();
        readonly FloatSpringState m_PelvisOffset = new FloatSpringState();
        readonly VectorSpringState m_LeftNormal = new VectorSpringState();
        readonly VectorSpringState m_RightNormal = new VectorSpringState();
        CharacterLyraCurrentGroundingSettings m_Settings;

        internal CharacterLyraCurrentGroundingSolver(
            CharacterFootPlacementPoseRig rig,
            CharacterFootPlacementWorldQueryBackend world,
            CharacterLyraCurrentGroundingSettings settings)
        {
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            settings.RequireValid();
            m_Settings = settings;
        }

        internal CharacterLyraCurrentGroundingTrace Trace(
            in CharacterFootPlacementAnimatedPose pose,
            float minimumGroundNormalDot)
        {
            if (!float.IsFinite(minimumGroundNormalDot) ||
                minimumGroundNormalDot < -1f || minimumGroundNormalDot > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumGroundNormalDot));
            }
            m_World.BeginFrame();
            CharacterLyraFootTraceResult left = TraceFoot(
                CharacterFootSide.Left,
                pose.Left.AnklePosition,
                minimumGroundNormalDot);
            CharacterLyraFootTraceResult right = TraceFoot(
                CharacterFootSide.Right,
                pose.Right.AnklePosition,
                minimumGroundNormalDot);
            float pelvisTarget = Mathf.Min(
                left.DidTraceHit ? left.TargetOffset : 0f,
                right.DidTraceHit ? right.TargetOffset : 0f);
            return new CharacterLyraCurrentGroundingTrace(left, right, pelvisTarget);
        }

        internal CharacterLyraCurrentGroundingResult Resolve(
            in CharacterLyraCurrentGroundingTrace trace,
            in CharacterFootPlacementAnimatedPose pose,
            float resolvedPelvisTarget,
            float leftSoleClearanceTarget,
            float rightSoleClearanceTarget,
            float deltaSeconds)
        {
            RequireSoleClearanceTarget(leftSoleClearanceTarget, nameof(leftSoleClearanceTarget));
            RequireSoleClearanceTarget(rightSoleClearanceTarget, nameof(rightSoleClearanceTarget));
            float pelvis = Spring(
                m_PelvisOffset,
                resolvedPelvisTarget,
                m_Settings.PelvisOffsetSpringStrength,
                m_Settings.PelvisOffsetCriticalDamping,
                m_Settings.PelvisOffsetTargetVelocityAmount,
                deltaSeconds);
            CharacterLyraCurrentGroundingFootResult left = ResolveFoot(
                trace.Left,
                pose.Left,
                m_LeftOffset,
                m_LeftNormal,
                pelvis,
                leftSoleClearanceTarget,
                deltaSeconds);
            CharacterLyraCurrentGroundingFootResult right = ResolveFoot(
                trace.Right,
                pose.Right,
                m_RightOffset,
                m_RightNormal,
                pelvis,
                rightSoleClearanceTarget,
                deltaSeconds);
            return new CharacterLyraCurrentGroundingResult(
                left,
                right,
                resolvedPelvisTarget,
                pelvis,
                m_PelvisOffset.Velocity,
                m_PelvisOffset.PreviousTarget,
                m_PelvisOffset.Initialized);
        }

        internal CharacterLyraCurrentGroundingResult ApplySoleConstraints(
            in CharacterLyraCurrentGroundingResult result,
            float leftConstraintOffset,
            float rightConstraintOffset)
        {
            RequireSoleClearanceTarget(leftConstraintOffset, nameof(leftConstraintOffset));
            RequireSoleClearanceTarget(rightConstraintOffset, nameof(rightConstraintOffset));
            CharacterLyraCurrentGroundingFootResult left = ApplySoleConstraint(
                result.Left,
                m_LeftOffset,
                leftConstraintOffset);
            CharacterLyraCurrentGroundingFootResult right = ApplySoleConstraint(
                result.Right,
                m_RightOffset,
                rightConstraintOffset);
            return new CharacterLyraCurrentGroundingResult(
                left,
                right,
                result.TargetPelvisOffset,
                result.CurrentPelvisOffset,
                result.PelvisVelocity,
                result.PreviousPelvisTarget,
                result.PelvisSpringInitialized);
        }

        internal void ApplyTuning(CharacterLyraCurrentGroundingSettings settings)
        {
            settings.RequireValid();
            if (settings.HitCapacity != m_Settings.HitCapacity)
                throw new InvalidOperationException("Lyra Current Grounding tuning cannot change hit capacity.");
            m_Settings = settings;
        }

        internal void Reset()
        {
            m_LeftOffset.Reset();
            m_RightOffset.Reset();
            m_PelvisOffset.Reset();
            m_LeftNormal.Reset();
            m_RightNormal.Reset();
        }

        CharacterLyraFootTraceResult TraceFoot(
            CharacterFootSide side,
            Vector3 anklePosition,
            float minimumGroundNormalDot)
        {
            Transform root = m_Rig.PoseRoot;
            Vector3 up = root.up.normalized;
            int footIndex = side == CharacterFootSide.Left ? 0 : 1;
            var request = new CharacterFootPlacementQueryRequest(
                CharacterFootPlacementQueryShape.Sphere,
                CharacterFootPlacementQueryPurpose.CurrentGrounding,
                footIndex,
                anklePosition + up * m_Settings.TraceAbove,
                Vector3.zero,
                -up,
                m_Settings.TraceAbove + m_Settings.TraceBelow,
                m_Settings.TraceRadius,
                m_Settings.GroundLayerMask,
                minimumGroundNormalDot);
            m_World.Query(in request, out CharacterFootPlacementQueryHit hit);
            float targetOffset = hit.HasHit
                ? root.InverseTransformPoint(hit.Location).y
                : 0f;
            return new CharacterLyraFootTraceResult(side, request, hit, anklePosition, targetOffset);
        }

        CharacterLyraCurrentGroundingFootResult ResolveFoot(
            CharacterLyraFootTraceResult trace,
            CharacterFootPlacementAnimatedFootPose pose,
            FloatSpringState offsetState,
            VectorSpringState normalState,
            float pelvisOffset,
            float soleClearanceTarget,
            float deltaSeconds)
        {
            Vector3 up = m_Rig.PoseRoot.up.normalized;
            Vector3 targetNormal = trace.DidTraceHit ? trace.Hit.Normal.normalized : up;
            Vector3 currentNormal = Spring(
                normalState,
                targetNormal,
                m_Settings.HitNormalSpringStrength,
                m_Settings.HitNormalCriticalDamping,
                0f,
                deltaSeconds);
            if (currentNormal.sqrMagnitude <= 0.000001f)
                currentNormal = up;
            else
                currentNormal.Normalize();
            float relativeTarget =
                (trace.DidTraceHit ? trace.TargetOffset + soleClearanceTarget : 0f) - pelvisOffset;
            float currentOffset = Spring(
                offsetState,
                relativeTarget,
                m_Settings.FootOffsetSpringStrength,
                m_Settings.FootOffsetCriticalDamping,
                m_Settings.FootOffsetTargetVelocityAmount,
                deltaSeconds);
            Vector3 worldPosition = pose.AnklePosition + up * (pelvisOffset + currentOffset);
            Quaternion worldRotation = (
                Quaternion.FromToRotation(up, currentNormal) * pose.AnkleRotation).normalized;
            Transform root = m_Rig.PoseRoot;
            return new CharacterLyraCurrentGroundingFootResult(
                trace,
                Quaternion.Inverse(root.rotation) * (worldPosition - root.position),
                (Quaternion.Inverse(root.rotation) * worldRotation).normalized,
                soleClearanceTarget,
                relativeTarget,
                currentOffset,
                0f,
                currentOffset,
                offsetState.Velocity,
                offsetState.PreviousTarget,
                offsetState.Initialized,
                currentNormal,
                normalState.Velocity,
                normalState.PreviousTarget,
                normalState.Initialized);
        }

        static CharacterLyraCurrentGroundingFootResult ApplySoleConstraint(
            CharacterLyraCurrentGroundingFootResult result,
            FloatSpringState offsetState,
            float constraintOffset)
        {
            if (constraintOffset <= 0f)
                return result;
            offsetState.Value += constraintOffset;
            if (offsetState.Velocity < 0f)
                offsetState.Velocity = 0f;
            return new CharacterLyraCurrentGroundingFootResult(
                result.Trace,
                result.ComponentPosition + Vector3.up * constraintOffset,
                result.ComponentRotation,
                result.SoleClearanceTarget,
                result.OffsetTarget,
                result.UnconstrainedOffset,
                result.SoleConstraintOffset + constraintOffset,
                offsetState.Value,
                offsetState.Velocity,
                offsetState.PreviousTarget,
                offsetState.Initialized,
                result.CurrentHitNormal,
                result.NormalVelocity,
                result.PreviousNormalTarget,
                result.NormalSpringInitialized);
        }

        static void RequireSoleClearanceTarget(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(parameterName);
        }

        static float Spring(
            FloatSpringState state,
            float target,
            float strength,
            float damping,
            float targetVelocityAmount,
            float deltaSeconds)
        {
            if (!state.Initialized && deltaSeconds > 0.000001f)
            {
                state.Value = target;
                state.Velocity = 0f;
                state.PreviousTarget = target;
                state.Initialized = true;
                return target;
            }
            float targetVelocity = state.Initialized && deltaSeconds > 0.000001f
                ? (target - state.PreviousTarget) * (targetVelocityAmount / deltaSeconds)
                : 0f;
            SpringDamper(ref state.Value, ref state.Velocity, target, targetVelocity, deltaSeconds, strength, damping);
            if (deltaSeconds > 0.000001f)
            {
                state.PreviousTarget = target;
                state.Initialized = true;
            }
            return state.Value;
        }

        static Vector3 Spring(
            VectorSpringState state,
            Vector3 target,
            float strength,
            float damping,
            float targetVelocityAmount,
            float deltaSeconds)
        {
            if (!state.Initialized && deltaSeconds > 0.000001f)
            {
                state.Value = target;
                state.Velocity = Vector3.zero;
                state.PreviousTarget = target;
                state.Initialized = true;
                return target;
            }
            Vector3 targetVelocity = state.Initialized && deltaSeconds > 0.000001f
                ? (target - state.PreviousTarget) * (targetVelocityAmount / deltaSeconds)
                : Vector3.zero;
            SpringDamper(ref state.Value, ref state.Velocity, target, targetVelocity, deltaSeconds, strength, damping);
            if (deltaSeconds > 0.000001f)
            {
                state.PreviousTarget = target;
                state.Initialized = true;
            }
            return state.Value;
        }

        static void SpringDamper(
            ref float value,
            ref float velocity,
            float target,
            float targetVelocity,
            float deltaSeconds,
            float frequency,
            float damping)
        {
            if (deltaSeconds <= 0f)
                return;
            float omega = frequency * 2f * Mathf.PI;
            if (omega < 0.000001f)
            {
                value += velocity * deltaSeconds;
                return;
            }
            if (damping < 0.000001f)
            {
                float sine = Mathf.Sin(omega * deltaSeconds);
                float cosine = Mathf.Cos(omega * deltaSeconds);
                float error = value - target;
                float b = velocity / omega;
                value = target + error * cosine + b * sine;
                velocity = velocity * cosine - error * (omega * sine);
                return;
            }
            float smoothingTime = 2f / omega;
            float adjustedTarget = target + targetVelocity * (damping * smoothingTime);
            float errorToTarget = value - adjustedTarget;
            if (damping > 1f)
            {
                float dampedFrequency = omega * Mathf.Sqrt(damping * damping - 1f);
                float c2 = -(velocity + (omega * damping - dampedFrequency) * errorToTarget) /
                           (2f * dampedFrequency);
                float c1 = errorToTarget - c2;
                float a1 = dampedFrequency - damping * omega;
                float a2 = -(dampedFrequency + damping * omega);
                float e1 = InvExpApprox(-a1 * deltaSeconds);
                float e2 = InvExpApprox(-a2 * deltaSeconds);
                value = adjustedTarget + e1 * c1 + e2 * c2;
                velocity = e1 * c1 * a1 + e2 * c2 * a2;
                return;
            }
            if (damping < 1f)
            {
                float dampedFrequency = omega * Mathf.Sqrt(1f - damping * damping);
                float a = errorToTarget;
                float b = (velocity + errorToTarget * (damping * omega)) / dampedFrequency;
                float sine = Mathf.Sin(dampedFrequency * deltaSeconds);
                float cosine = Mathf.Cos(dampedFrequency * deltaSeconds);
                float e = InvExpApprox(damping * omega * deltaSeconds);
                float relativeValue = e * (a * cosine + b * sine);
                value = relativeValue + adjustedTarget;
                velocity = -relativeValue * damping * omega +
                           e * (b * (dampedFrequency * cosine) - a * (dampedFrequency * sine));
                return;
            }
            float criticalC2 = velocity + errorToTarget * omega;
            float criticalE = InvExpApprox(omega * deltaSeconds);
            value = adjustedTarget + (errorToTarget + criticalC2 * deltaSeconds) * criticalE;
            velocity = (criticalC2 - errorToTarget * omega - criticalC2 * (omega * deltaSeconds)) * criticalE;
        }

        static void SpringDamper(
            ref Vector3 value,
            ref Vector3 velocity,
            Vector3 target,
            Vector3 targetVelocity,
            float deltaSeconds,
            float frequency,
            float damping)
        {
            float x = value.x;
            float vx = velocity.x;
            SpringDamper(ref x, ref vx, target.x, targetVelocity.x, deltaSeconds, frequency, damping);
            float y = value.y;
            float vy = velocity.y;
            SpringDamper(ref y, ref vy, target.y, targetVelocity.y, deltaSeconds, frequency, damping);
            float z = value.z;
            float vz = velocity.z;
            SpringDamper(ref z, ref vz, target.z, targetVelocity.z, deltaSeconds, frequency, damping);
            value = new Vector3(x, y, z);
            velocity = new Vector3(vx, vy, vz);
        }

        static float InvExpApprox(float value)
        {
            const float a = 1.00746054f;
            const float b = 0.45053901f;
            const float c = 0.25724632f;
            return 1f / (1f + a * value + b * value * value + c * value * value * value);
        }
    }
}
