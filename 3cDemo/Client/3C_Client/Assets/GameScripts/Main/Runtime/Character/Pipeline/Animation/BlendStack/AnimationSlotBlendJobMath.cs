using UnityEngine;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal static class AnimationSlotBlendJobMath
    {
        const float QuaternionTolerance = 0.0000001f;

        internal static bool TryResolveWeightedPose(
            Vector3 positionSum,
            Vector4 rotationSum,
            Vector3 scaleSum,
            float weight,
            out AnimationLocalBonePose pose)
        {
            if (!float.IsFinite(weight) || weight <= 0f ||
                !AnimationPoseMath.IsFinite(positionSum) ||
                !AnimationPoseMath.IsFinite(scaleSum) ||
                !IsFinite(rotationSum))
            {
                pose = default;
                return false;
            }
            Vector3 position = positionSum / weight;
            Vector3 scale = scaleSum / weight;
            Quaternion rotation = new Quaternion(
                rotationSum.x / weight,
                rotationSum.y / weight,
                rotationSum.z / weight,
                rotationSum.w / weight);
            float magnitude = Quaternion.Dot(rotation, rotation);
            if (!AnimationPoseMath.IsFinite(position) || !AnimationPoseMath.IsFinite(scale) ||
                !float.IsFinite(magnitude) || magnitude <= QuaternionTolerance)
            {
                pose = default;
                return false;
            }
            rotation = rotation.normalized;
            if (!IsFinite(rotation))
            {
                pose = default;
                return false;
            }
            pose = new AnimationLocalBonePose(position, rotation, scale);
            return true;
        }

        internal static bool TryCreateVelocity(
            Vector3 linear,
            Vector3 angular,
            Vector3 scale,
            out AnimationBlendBoneVelocity velocity)
        {
            if (!AnimationPoseMath.IsFinite(linear) ||
                !AnimationPoseMath.IsFinite(angular) ||
                !AnimationPoseMath.IsFinite(scale))
            {
                velocity = default;
                return false;
            }
            velocity = new AnimationBlendBoneVelocity(linear, angular, scale);
            return true;
        }

        internal static bool TryDifferentiate(
            AnimationLocalBonePose previous,
            AnimationLocalBonePose current,
            float deltaSeconds,
            out AnimationBlendBoneVelocity velocity)
        {
            Vector3 linear = (current.Position - previous.Position) / deltaSeconds;
            Vector3 angular = AnimationPoseMath.QuaternionLog(current.Rotation * Quaternion.Inverse(previous.Rotation)) / deltaSeconds;
            Vector3 scale = (current.Scale - previous.Scale) / deltaSeconds;
            return TryCreateVelocity(linear, angular, scale, out velocity);
        }

        internal static bool AccumulateFoot(
            AnimationFootFeatureSample sample,
            float weight,
            float visualTimeScale,
            ref float totalWeight,
            ref Vector3 velocity,
            ref float height,
            ref float plantConfidence)
        {
            if (!IsValidFoot(sample) || !float.IsFinite(weight) || weight <= 0f ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
            {
                return false;
            }
            totalWeight += weight;
            velocity += sample.SoleLocalVelocity * visualTimeScale * weight;
            height += sample.SoleHeight * weight;
            plantConfidence += sample.PlantConfidence * weight;
            return float.IsFinite(totalWeight) && AnimationPoseMath.IsFinite(velocity) &&
                   float.IsFinite(height) && float.IsFinite(plantConfidence);
        }

        internal static bool TryResolveAuthoritativePrediction(
            AnimationFootFeatureSample sample,
            float visualTimeScale,
            ulong contributionContinuityIdentity,
            CharacterFootSide side,
            out AnimationPredictedFootStepSample predictedStep,
            out AnimationPredictedFootStepSample incomingPredictedStep)
        {
            predictedStep = default;
            incomingPredictedStep = default;
            if (!IsValidFoot(sample) || !float.IsFinite(visualTimeScale) || visualTimeScale < 0f ||
                contributionContinuityIdentity == 0 ||
                side != CharacterFootSide.Left && side != CharacterFootSide.Right)
            {
                return false;
            }
            predictedStep = sample.PredictedStep
                .ApplyTimeScale(visualTimeScale)
                .BindContribution(contributionContinuityIdentity, side);
            incomingPredictedStep = sample.IncomingPredictedStep
                .ApplyTimeScale(visualTimeScale)
                .BindContribution(contributionContinuityIdentity, side);
            return IsValidPrediction(predictedStep) && IsValidPrediction(incomingPredictedStep);
        }

        internal static bool TryResolveFoot(
            float weight,
            Vector3 velocity,
            float height,
            float plantConfidence,
            AnimationPredictedFootStepSample predictedStep,
            AnimationPredictedFootStepSample incomingPredictedStep,
            out AnimationFootFeatureSample sample)
        {
            float inverseWeight = 1f / weight;
            float resolvedPlant = plantConfidence * inverseWeight;
            Vector3 resolvedVelocity = velocity * inverseWeight;
            float resolvedHeight = height * inverseWeight;
            if (!AnimationPoseMath.IsFinite(resolvedVelocity) || !float.IsFinite(resolvedHeight) ||
                !IsNormalized(resolvedPlant))
            {
                sample = default;
                return false;
            }
            sample = new AnimationFootFeatureSample(
                resolvedVelocity,
                resolvedHeight,
                resolvedPlant,
                predictedStep,
                incomingPredictedStep);
            return true;
        }

        internal static bool IsValidFoot(AnimationFootFeatureSample sample) =>
            sample.IsValid && AnimationPoseMath.IsFinite(sample.SoleLocalVelocity) &&
            float.IsFinite(sample.SoleHeight) && IsNormalized(sample.PlantConfidence) &&
            IsValidPrediction(sample.PredictedStep) &&
            IsValidPrediction(sample.IncomingPredictedStep);

        static bool IsValidPrediction(AnimationPredictedFootStepSample value) =>
            !value.IsValid ||
            IsNormalized(value.Confidence) &&
            float.IsFinite(value.TimeToLandingSeconds) && value.TimeToLandingSeconds >= 0f &&
            IsNormalized(value.EventPhase) && IsNormalized(value.LiftOffPhase) &&
            IsValidRootLocalFootRoute(value);

        static bool IsValidRootLocalFootRoute(AnimationPredictedFootStepSample value)
        {
            if (value.RootLocalFootRoute.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.RootLocalAnkleRoute.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.RootLocalHipRoute.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.AuthoredFootPlanarRoute.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.AnimationClearanceHeights.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                !IsNormalized(value.LandingPhase) ||
                !IsFinite(value.OpposingRootLocalSoleRotation) ||
                Quaternion.Dot(value.OpposingRootLocalSoleRotation, value.OpposingRootLocalSoleRotation) <= 0.000001f)
                return false;
            for (int i = 0; i < value.RootLocalFootRoute.Length; i++)
            {
                if (!AnimationPoseMath.IsFinite(value.RootLocalFootRoute[i]) ||
                    !AnimationPoseMath.IsFinite(value.RootLocalAnkleRoute[i]) ||
                    !AnimationPoseMath.IsFinite(value.RootLocalHipRoute[i]) ||
                    !AnimationPoseMath.IsFinite(value.AuthoredFootPlanarRoute[i]) ||
                    !float.IsFinite(value.AnimationClearanceHeights[i]) ||
                    value.AnimationClearanceHeights[i] < 0f)
                    return false;
            }
            return true;
        }

        internal static bool IsNormalized(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        internal static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        internal static bool IsFinite(Vector4 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);

        internal static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}
