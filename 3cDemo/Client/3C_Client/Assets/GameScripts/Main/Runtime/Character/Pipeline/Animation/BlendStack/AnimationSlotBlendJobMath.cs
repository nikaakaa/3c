using UnityEngine;

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
            ref float plantConfidence,
            ref float landingConfidence,
            ref float landingWeight,
            ref float landingDelay,
            ref Vector2 landingOffset)
        {
            if (!IsValidFoot(sample) || !float.IsFinite(weight) || weight <= 0f ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
            {
                return false;
            }
            float effectiveLandingConfidence = visualTimeScale > 0.000001f
                ? sample.NextLandingConfidence
                : 0f;
            float nextLandingWeight = weight * effectiveLandingConfidence;
            totalWeight += weight;
            velocity += sample.SoleLocalVelocity * visualTimeScale * weight;
            height += sample.SoleHeight * weight;
            plantConfidence += sample.PlantConfidence * weight;
            landingConfidence += effectiveLandingConfidence * weight;
            landingWeight += nextLandingWeight;
            if (nextLandingWeight > 0f)
                landingDelay += sample.NextLandingDelaySeconds / visualTimeScale * nextLandingWeight;
            landingOffset += sample.NextLandingLocalOffset * nextLandingWeight;
            return float.IsFinite(totalWeight) && AnimationPoseMath.IsFinite(velocity) &&
                   float.IsFinite(height) && float.IsFinite(plantConfidence) &&
                   float.IsFinite(landingConfidence) && float.IsFinite(landingWeight) &&
                   float.IsFinite(landingDelay) && IsFinite(landingOffset);
        }

        internal static bool TryResolveFoot(
            float weight,
            Vector3 velocity,
            float height,
            float plantConfidence,
            float landingConfidence,
            float landingWeight,
            float landingDelay,
            Vector2 landingOffset,
            out AnimationFootFeatureSample sample)
        {
            float inverseWeight = 1f / weight;
            float resolvedPlant = plantConfidence * inverseWeight;
            float resolvedLandingConfidence = landingConfidence * inverseWeight;
            float resolvedLandingDelay = landingWeight > 0f ? landingDelay / landingWeight : 0f;
            Vector2 resolvedLandingOffset = landingWeight > 0f ? landingOffset / landingWeight : Vector2.zero;
            Vector3 resolvedVelocity = velocity * inverseWeight;
            float resolvedHeight = height * inverseWeight;
            if (!AnimationPoseMath.IsFinite(resolvedVelocity) || !float.IsFinite(resolvedHeight) ||
                !IsNormalized(resolvedPlant) || !IsNormalized(resolvedLandingConfidence) ||
                !float.IsFinite(resolvedLandingDelay) || resolvedLandingDelay < 0f ||
                !IsFinite(resolvedLandingOffset))
            {
                sample = default;
                return false;
            }
            sample = new AnimationFootFeatureSample(
                resolvedVelocity,
                resolvedHeight,
                resolvedPlant,
                resolvedLandingConfidence,
                resolvedLandingDelay,
                resolvedLandingOffset);
            return true;
        }

        internal static bool IsValidFoot(AnimationFootFeatureSample sample) =>
            sample.IsValid && AnimationPoseMath.IsFinite(sample.SoleLocalVelocity) &&
            float.IsFinite(sample.SoleHeight) && IsNormalized(sample.PlantConfidence) &&
            IsNormalized(sample.NextLandingConfidence) &&
            float.IsFinite(sample.NextLandingDelaySeconds) && sample.NextLandingDelaySeconds >= 0f &&
            IsFinite(sample.NextLandingLocalOffset);

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
