using System;
using RootMotion.FinalIK;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootPlacementTuningDecoder
    {
        internal static string Apply(
            CharacterFootPlacementRuntimeSettings settings,
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block)
        {
            if (settings == null || layout == null || block == null)
                return "Foot Placement tuning payload is missing.";
            try
            {
                block.RequireValid(layout);
                CharacterFinalIkGroundingSettings grounding = settings.Grounding;
                CharacterPredictiveFootPlacementRuntimeSettings predictive = settings.Predictive;
                float maximumStep = grounding.MaximumStep;
                float heightOffset = grounding.HeightOffset;
                float footHeightSpeed = grounding.FootHeightSpeed;
                float footRadius = grounding.FootRadius;
                float velocityPrediction = grounding.VelocityPrediction;
                float footRotationWeight = grounding.FootRotationWeight;
                float footRotationSpeed = grounding.FootRotationSpeed;
                float maximumFootRotationAngle = grounding.MaximumFootRotationAngle;
                float pathSphereRadius = predictive.PathSphereRadius;
                float swingCapsuleRadius = predictive.SwingCapsuleRadius;
                float castAbove = predictive.CastAbove;
                float castBelow = predictive.CastBelow;
                float maximumSlopeDegrees = predictive.MaximumSlopeDegrees;
                float maximumStepUp = predictive.MaximumStepUp;
                float maximumStepDown = predictive.MaximumStepDown;
                float maximumHeightDiscontinuity = predictive.MaximumHeightDiscontinuity;
                float maximumEdgeGap = predictive.MaximumEdgeGap;
                float maximumSwingClearance = predictive.MaximumSwingClearance;
                float plantSpeedThreshold = predictive.PlantSpeedThreshold;
                float unalignmentSpeedThreshold = predictive.UnalignmentSpeedThreshold;
                float plantConfidenceEnter = predictive.PlantConfidenceEnter;
                float plantConfidenceExit = predictive.PlantConfidenceExit;
                float minimumLookAheadSeconds = predictive.MinimumLookAheadSeconds;
                float maximumLookAheadSeconds = predictive.MaximumLookAheadSeconds;
                float maximumYawVelocity = predictive.MaximumYawVelocityDegreesPerSecond;
                float maximumPredictionDistance = predictive.MaximumPredictionDistance;
                float maximumPredictionReachRatio = predictive.MaximumPredictionReachRatio;
                float slideStartDistance = predictive.SlideStartDistance;
                float slideStopDistance = predictive.SlideStopDistance;
                float maximumSlideDistance = predictive.MaximumSlideDistance;
                float slideSpeed = predictive.SlideSpeed;
                float replantDistance = predictive.ReplantDistance;
                float replantAngleDegrees = predictive.ReplantAngleDegrees;
                float minimumFootSeparation = predictive.MinimumFootSeparation;
                float maximumAnkleTwistDegrees = predictive.MaximumAnkleTwistDegrees;
                float heelLiftRatio = predictive.HeelLiftRatio;
                float minimumLegExtensionRatio = predictive.MinimumLegExtensionRatio;
                float maximumLegExtensionRatio = predictive.MaximumLegExtensionRatio;
                float maximumPelvisLowering = predictive.MaximumPelvisLowering;
                float maximumPelvisRaising = predictive.MaximumPelvisRaising;
                float pelvisInterpolationSpeed = predictive.PelvisInterpolationSpeed;
                float pelvisHeightDeadZone = predictive.PelvisHeightDeadZone;
                float maximumHorizontalFootAdjustment = predictive.MaximumHorizontalFootAdjustment;
                float minimumSourceContribution = predictive.MinimumSourceContribution;
                string ownerId = $"foot-placement-profile:{settings.ProfileId}";
                for (int i = 0; i < layout.Entries.Count; i++)
                {
                    CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                    if (!string.Equals(entry.OwnerId, ownerId, StringComparison.Ordinal) ||
                        entry.Interaction != CharacterPoseTuningInteractionPolicy.TunableDefault)
                        continue;
                    CharacterPoseTuningValue value = block.GetValue(entry);
                    string fieldId = entry.FieldId;
                    if (fieldId.EndsWith("/predictive/hit-capacity", StringComparison.Ordinal) ||
                        fieldId.EndsWith("/predictive/path-sample-count", StringComparison.Ordinal))
                        return "Foot Placement tuning cannot change published workspace capacity.";
                    else if (fieldId.EndsWith("/grounding/maximum-step", StringComparison.Ordinal)) maximumStep = value.FloatValue;
                    else if (fieldId.EndsWith("/grounding/height-offset", StringComparison.Ordinal)) heightOffset = value.FloatValue;
                    else if (fieldId.EndsWith("/grounding/foot-height-speed", StringComparison.Ordinal)) footHeightSpeed = value.FloatValue;
                    else if (fieldId.EndsWith("/grounding/foot-radius", StringComparison.Ordinal)) footRadius = value.FloatValue;
                    else if (fieldId.EndsWith("/grounding/velocity-prediction", StringComparison.Ordinal)) velocityPrediction = value.FloatValue;
                    else if (fieldId.EndsWith("/grounding/foot-rotation-weight", StringComparison.Ordinal)) footRotationWeight = value.FloatValue;
                    else if (fieldId.EndsWith("/grounding/foot-rotation-speed", StringComparison.Ordinal)) footRotationSpeed = value.FloatValue;
                    else if (fieldId.EndsWith("/grounding/maximum-foot-rotation-angle", StringComparison.Ordinal)) maximumFootRotationAngle = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/path-sphere-radius", StringComparison.Ordinal)) pathSphereRadius = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/swing-capsule-radius", StringComparison.Ordinal)) swingCapsuleRadius = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/cast-above", StringComparison.Ordinal)) castAbove = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/cast-below", StringComparison.Ordinal)) castBelow = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-slope-degrees", StringComparison.Ordinal)) maximumSlopeDegrees = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-step-up", StringComparison.Ordinal)) maximumStepUp = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-step-down", StringComparison.Ordinal)) maximumStepDown = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-height-discontinuity", StringComparison.Ordinal)) maximumHeightDiscontinuity = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-edge-gap", StringComparison.Ordinal)) maximumEdgeGap = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-swing-clearance", StringComparison.Ordinal)) maximumSwingClearance = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/plant-speed-threshold", StringComparison.Ordinal)) plantSpeedThreshold = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/unalignment-speed-threshold", StringComparison.Ordinal)) unalignmentSpeedThreshold = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/plant-confidence-enter", StringComparison.Ordinal)) plantConfidenceEnter = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/plant-confidence-exit", StringComparison.Ordinal)) plantConfidenceExit = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/minimum-look-ahead-seconds", StringComparison.Ordinal)) minimumLookAheadSeconds = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-look-ahead-seconds", StringComparison.Ordinal)) maximumLookAheadSeconds = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-yaw-velocity", StringComparison.Ordinal)) maximumYawVelocity = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-prediction-distance", StringComparison.Ordinal)) maximumPredictionDistance = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-prediction-reach-ratio", StringComparison.Ordinal)) maximumPredictionReachRatio = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/slide-start-distance", StringComparison.Ordinal)) slideStartDistance = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/slide-stop-distance", StringComparison.Ordinal)) slideStopDistance = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-slide-distance", StringComparison.Ordinal)) maximumSlideDistance = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/slide-speed", StringComparison.Ordinal)) slideSpeed = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/replant-distance", StringComparison.Ordinal)) replantDistance = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/replant-angle-degrees", StringComparison.Ordinal)) replantAngleDegrees = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/minimum-foot-separation", StringComparison.Ordinal)) minimumFootSeparation = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-ankle-twist-degrees", StringComparison.Ordinal)) maximumAnkleTwistDegrees = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/heel-lift-ratio", StringComparison.Ordinal)) heelLiftRatio = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/minimum-leg-extension-ratio", StringComparison.Ordinal)) minimumLegExtensionRatio = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-leg-extension-ratio", StringComparison.Ordinal)) maximumLegExtensionRatio = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-pelvis-lowering", StringComparison.Ordinal)) maximumPelvisLowering = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-pelvis-raising", StringComparison.Ordinal)) maximumPelvisRaising = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/pelvis-interpolation-speed", StringComparison.Ordinal)) pelvisInterpolationSpeed = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/pelvis-height-dead-zone", StringComparison.Ordinal)) pelvisHeightDeadZone = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-horizontal-foot-adjustment", StringComparison.Ordinal)) maximumHorizontalFootAdjustment = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/minimum-source-contribution", StringComparison.Ordinal)) minimumSourceContribution = value.FloatValue;
                }
                grounding = new CharacterFinalIkGroundingSettings(
                    grounding.Quality,
                    grounding.GroundLayerMask,
                    maximumStep,
                    heightOffset,
                    footHeightSpeed,
                    footRadius,
                    velocityPrediction,
                    footRotationWeight,
                    footRotationSpeed,
                    maximumFootRotationAngle,
                    grounding.RotateSolver,
                    grounding.RootCastRadius,
                    grounding.OverstepFallsDown);
                predictive = new CharacterPredictiveFootPlacementRuntimeSettings(
                    predictive.HitCapacity,
                    pathSphereRadius,
                    swingCapsuleRadius,
                    castAbove,
                    castBelow,
                    predictive.PathSampleCount,
                    maximumSlopeDegrees,
                    maximumStepUp,
                    maximumStepDown,
                    maximumHeightDiscontinuity,
                    maximumEdgeGap,
                    maximumSwingClearance,
                    plantSpeedThreshold,
                    unalignmentSpeedThreshold,
                    plantConfidenceEnter,
                    plantConfidenceExit,
                    minimumLookAheadSeconds,
                    maximumLookAheadSeconds,
                    maximumYawVelocity,
                    maximumPredictionDistance,
                    maximumPredictionReachRatio,
                    slideStartDistance,
                    slideStopDistance,
                    maximumSlideDistance,
                    slideSpeed,
                    replantDistance,
                    replantAngleDegrees,
                    minimumFootSeparation,
                    maximumAnkleTwistDegrees,
                    predictive.LockType,
                    predictive.AdjustHeelBeforePlanting,
                    heelLiftRatio,
                    minimumLegExtensionRatio,
                    maximumLegExtensionRatio,
                    predictive.PelvisHeightMode,
                    predictive.ActorMovementCompensationMode,
                    maximumPelvisLowering,
                    maximumPelvisRaising,
                    pelvisInterpolationSpeed,
                    pelvisHeightDeadZone,
                    maximumHorizontalFootAdjustment,
                    minimumSourceContribution);
                settings.ApplyTuning(grounding, predictive);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }
    }
}
