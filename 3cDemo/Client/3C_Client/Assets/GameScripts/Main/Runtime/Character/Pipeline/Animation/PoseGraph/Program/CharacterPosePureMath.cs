using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPosePureMath
    {
        const float ScaleEpsilon = 0.000001f;

        internal static AnimationPrimitivePoseContribution CopyContribution(
            in AnimationPrimitivePoseContribution source,
            float weight,
            float leftWeight,
            float rightWeight) =>
            new AnimationPrimitivePoseContribution(
                source.PhysicalPlayerIndex,
                source.PhysicalSourceIndex,
                source.PhysicalSourceGeneration,
                source.Kind,
                source.SourceOwnerIndex,
                source.ContributionContinuityIdentity,
                weight,
                leftWeight,
                rightWeight);

        internal static bool TryBlendPose(
            in AnimationLocalBonePose from,
            in AnimationLocalBonePose to,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!from.IsValid || !to.IsValid || !IsWeight(weight))
                return false;
            Quaternion target = to.Rotation;
            if (Quaternion.Dot(from.Rotation, target) < 0f)
            {
                target = new Quaternion(
                    -target.x,
                    -target.y,
                    -target.z,
                    -target.w);
            }
            return TryCreatePose(
                Vector3.LerpUnclamped(from.Position, to.Position, weight),
                Quaternion.SlerpUnclamped(from.Rotation, target, weight),
                Vector3.LerpUnclamped(from.Scale, to.Scale, weight),
                out result);
        }

        internal static bool TryAddPose(
            in AnimationLocalBonePose basePose,
            in AnimationLocalBonePose additivePose,
            in AnimationLocalBonePose referencePose,
            AdditiveScalePolicy scalePolicy,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!basePose.IsValid || !additivePose.IsValid ||
                !referencePose.IsValid || !IsWeight(weight))
                return false;
            Quaternion delta = additivePose.Rotation *
                               Quaternion.Inverse(referencePose.Rotation);
            if (delta.w < 0f)
            {
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            }
            Quaternion rotation = basePose.Rotation *
                Quaternion.SlerpUnclamped(Quaternion.identity, delta, weight);
            Vector3 scale;
            switch (scalePolicy)
            {
                case AdditiveScalePolicy.Multiply:
                    if (!TryDivide(
                            additivePose.Scale,
                            referencePose.Scale,
                            out Vector3 ratio))
                        return false;
                    scale = Vector3.Scale(
                        basePose.Scale,
                        Vector3.LerpUnclamped(Vector3.one, ratio, weight));
                    break;
                case AdditiveScalePolicy.AddDelta:
                    scale = basePose.Scale +
                            (additivePose.Scale - referencePose.Scale) * weight;
                    break;
                case AdditiveScalePolicy.Ignore:
                    scale = basePose.Scale;
                    break;
                default:
                    return false;
            }
            return TryCreatePose(
                basePose.Position +
                (additivePose.Position - referencePose.Position) * weight,
                rotation,
                scale,
                out result);
        }

        internal static bool TryToModel(
            in AnimationLocalBonePose parent,
            in AnimationLocalBonePose local,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!parent.IsValid || !local.IsValid)
                return false;
            return TryCreatePose(
                parent.Position + parent.Rotation *
                Vector3.Scale(parent.Scale, local.Position),
                parent.Rotation * local.Rotation,
                Vector3.Scale(parent.Scale, local.Scale),
                out result);
        }

        internal static bool TryToLocal(
            in AnimationLocalBonePose parent,
            in AnimationLocalBonePose model,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!parent.IsValid || !model.IsValid)
                return false;
            Quaternion inverse = Quaternion.Inverse(parent.Rotation);
            if (!TryDivide(
                    inverse * (model.Position - parent.Position),
                    parent.Scale,
                    out Vector3 position) ||
                !TryDivide(model.Scale, parent.Scale, out Vector3 scale))
                return false;
            return TryCreatePose(
                position,
                inverse * model.Rotation,
                scale,
                out result);
        }

        internal static bool TryResolveFeature(
            bool hasBase,
            in AnimationFootFeatureSample baseValue,
            bool hasOverlay,
            in AnimationFootFeatureSample overlayValue,
            float weight,
            bool overlayPredictionAuthoritative,
            out AnimationFootFeatureSample result)
        {
            result = default;
            if (!hasBase)
            {
                if (!hasOverlay || !IsValidFootFeature(overlayValue))
                    return false;
                result = overlayValue;
                return true;
            }
            if (!IsValidFootFeature(baseValue))
                return false;
            if (!hasOverlay)
            {
                result = baseValue;
                return true;
            }
            if (!IsValidFootFeature(overlayValue) || !float.IsFinite(weight))
                return false;
            float t = Mathf.Clamp01(weight);
            Vector3 velocity = Vector3.LerpUnclamped(
                baseValue.SoleLocalVelocity,
                overlayValue.SoleLocalVelocity,
                t);
            float height = Mathf.LerpUnclamped(
                baseValue.SoleHeight,
                overlayValue.SoleHeight,
                t);
            float plant = Mathf.LerpUnclamped(
                baseValue.PlantConfidence,
                overlayValue.PlantConfidence,
                t);
            AnimationPredictedFootStepSample predicted =
                overlayPredictionAuthoritative
                    ? overlayValue.PredictedStep
                    : baseValue.PredictedStep;
            AnimationPredictedFootStepSample incoming =
                overlayPredictionAuthoritative
                    ? overlayValue.IncomingPredictedStep
                    : baseValue.IncomingPredictedStep;
            if (!IsFinite(velocity) || !float.IsFinite(height) ||
                !IsWeight(plant))
                return false;
            result = new AnimationFootFeatureSample(
                velocity,
                height,
                plant,
                predicted,
                incoming);
            return result.IsValid;
        }

        internal static bool TryResolveStateMachineFeature(
            bool hasSource,
            in AnimationFootFeatureSample source,
            bool hasTarget,
            in AnimationFootFeatureSample target,
            float weight,
            out AnimationFootFeatureSample result) =>
            TryResolveFeature(
                hasSource,
                in source,
                hasTarget,
                in target,
                weight,
                true,
                out result) && result.IsValid;

        internal static bool IsValidPrimitiveContribution(
            in AnimationPrimitivePoseContribution contribution)
        {
            int kind = (int)contribution.Kind;
            bool live = contribution.Kind == AnimationPoseContributionKind.Live;
            return contribution.PhysicalPlayerIndex >= 0 &&
                   kind >= (int)AnimationPoseContributionKind.Live &&
                   kind <= (int)AnimationPoseContributionKind.Stored &&
                   (live
                       ? contribution.PhysicalSourceIndex >= 0 &&
                         contribution.PhysicalSourceGeneration != 0 &&
                         contribution.SourceOwnerIndex >= 0
                       : contribution.PhysicalSourceIndex == -1 &&
                         contribution.PhysicalSourceGeneration == 0 &&
                         contribution.SourceOwnerIndex == -1) &&
                   contribution.ContributionContinuityIdentity != 0 &&
                   IsWeight(contribution.Weight) &&
                   IsWeight(contribution.LeftFootWeight) &&
                   IsWeight(contribution.RightFootWeight);
        }

        internal static bool IsValidFootFeature(
            in AnimationFootFeatureSample sample) =>
            sample.IsValid && IsFinite(sample.SoleLocalVelocity) &&
            float.IsFinite(sample.SoleHeight) &&
            IsWeight(sample.PlantConfidence) &&
            (!sample.PredictedStep.IsValid ||
             IsWeight(sample.PredictedStep.Confidence) &&
             float.IsFinite(sample.PredictedStep.TimeToLandingSeconds) &&
             sample.PredictedStep.TimeToLandingSeconds >= 0f &&
             IsWeight(sample.PredictedStep.EventPhase) &&
             IsWeight(sample.PredictedStep.LiftOffPhase) &&
             IsValidRootLocalFootRoute(sample.PredictedStep)) &&
            (!sample.IncomingPredictedStep.IsValid ||
             IsWeight(sample.IncomingPredictedStep.Confidence) &&
             float.IsFinite(
                 sample.IncomingPredictedStep.TimeToLandingSeconds) &&
             sample.IncomingPredictedStep.TimeToLandingSeconds >= 0f &&
             IsWeight(sample.IncomingPredictedStep.EventPhase) &&
             IsWeight(sample.IncomingPredictedStep.LiftOffPhase) &&
             IsValidRootLocalFootRoute(sample.IncomingPredictedStep));

        static bool IsValidRootLocalFootRoute(
            in AnimationPredictedFootStepSample value)
        {
            if (value.Route.RootLocalFoot.Length !=
                    AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.RootLocalAnkle.Length !=
                    AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.RootLocalHip.Length !=
                    AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.AuthoredFootPlanar.Length !=
                    AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.AnimationClearance.Length !=
                    AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                !IsWeight(value.LandingPhase) ||
                !IsFinite(value.OpposingRootLocalSoleRotation) ||
                Quaternion.Dot(
                    value.OpposingRootLocalSoleRotation,
                    value.OpposingRootLocalSoleRotation) <= 0.000001f)
                return false;
            for (int i = 0; i < value.Route.RootLocalFoot.Length; i++)
            {
                if (!IsFinite(value.Route.RootLocalFoot[i]) ||
                    !IsFinite(value.Route.RootLocalAnkle[i]) ||
                    !IsFinite(value.Route.RootLocalHip[i]) ||
                    !IsFinite(value.Route.AuthoredFootPlanar[i]) ||
                    !float.IsFinite(value.Route.AnimationClearance[i]) ||
                    value.Route.AnimationClearance[i] < 0f)
                    return false;
            }
            return true;
        }

        internal static AnimationPoseNativeInvalidReason NormalizeInvalidReason(
            AnimationPoseNativeInvalidReason reason) =>
            AnimationPoseNativeInvalidReasonContract.NormalizeFailure(reason);

        internal static bool IsAvailability(
            AnimationPoseAvailability availability) =>
            availability >= AnimationPoseAvailability.Pose &&
            availability <= AnimationPoseAvailability.Invalid;

        internal static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        internal static float UnionWeight(float a, float b) =>
            Mathf.Clamp01(
                1f - (1f - Mathf.Clamp01(a)) * (1f - Mathf.Clamp01(b)));

        internal static ulong CombineContinuity(ulong a, ulong b, int operation)
        {
            unchecked
            {
                ulong value = 1469598103934665603UL;
                value = (value ^ RequireIdentity(a)) * 1099511628211UL;
                value = (value ^ RequireIdentity(b)) * 1099511628211UL;
                value = (value ^ (ulong)(operation + 1)) * 1099511628211UL;
                return RequireIdentity(value);
            }
        }

        internal static ulong RequireIdentity(ulong value) =>
            value == 0 ? 1UL : value;

        static bool TryDivide(
            in Vector3 value,
            in Vector3 divisor,
            out Vector3 result)
        {
            result = default;
            if (!IsFinite(value) || !IsFinite(divisor) ||
                Mathf.Abs(divisor.x) <= ScaleEpsilon ||
                Mathf.Abs(divisor.y) <= ScaleEpsilon ||
                Mathf.Abs(divisor.z) <= ScaleEpsilon)
                return false;
            result = new Vector3(
                value.x / divisor.x,
                value.y / divisor.y,
                value.z / divisor.z);
            return IsFinite(result);
        }

        static bool TryCreatePose(
            in Vector3 position,
            in Quaternion rotation,
            in Vector3 scale,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!IsFinite(position) || !IsFinite(rotation) ||
                !IsFinite(scale) || Quaternion.Dot(rotation, rotation) <= 0f)
                return false;
            result = new AnimationLocalBonePose(position, rotation, scale);
            return result.IsValid;
        }

        internal static bool IsFinite(in Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z);

        internal static bool IsFinite(in Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}
