using System;
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
                CharacterLyraCurrentGroundingSettings current = settings.CurrentGrounding;
                CharacterStanceStabilizationSettings stance = settings.StanceStabilization;
                CharacterPredictiveFootPlacementRuntimeSettings predictive = settings.PredictiveExtension;

                float traceAbove = current.TraceAbove;
                float traceBelow = current.TraceBelow;
                float traceRadius = current.TraceRadius;
                float hitNormalSpringStrength = current.HitNormalSpringStrength;
                float hitNormalCriticalDamping = current.HitNormalCriticalDamping;
                float footOffsetSpringStrength = current.FootOffsetSpringStrength;
                float footOffsetCriticalDamping = current.FootOffsetCriticalDamping;
                float footOffsetTargetVelocityAmount = current.FootOffsetTargetVelocityAmount;
                float pelvisOffsetSpringStrength = current.PelvisOffsetSpringStrength;
                float pelvisOffsetCriticalDamping = current.PelvisOffsetCriticalDamping;

                float maximumSurfaceSlopeDegrees = stance.MaximumSurfaceSlopeDegrees;
                float maximumContactSurfaceDistance = stance.MaximumContactSurfaceDistance;
                float plantSpeedThreshold = stance.PlantSpeedThreshold;
                float unalignmentSpeedThreshold = stance.UnalignmentSpeedThreshold;
                float plantConfidenceEnter = stance.PlantConfidenceEnter;
                float plantConfidenceExit = stance.PlantConfidenceExit;
                float anchorBlendSpeed = stance.AnchorBlendSpeed;
                float maximumAnchorDistance = stance.MaximumAnchorDistance;
                float maximumPelvisLowering = stance.MaximumPelvisLowering;
                float maximumPelvisRaising = stance.MaximumPelvisRaising;

                float pathSphereRadius = predictive.PathSphereRadius;
                float swingCapsuleRadius = predictive.SwingCapsuleRadius;
                float castAbove = predictive.CastAbove;
                float castBelow = predictive.CastBelow;
                float maximumSlopeDegrees = predictive.MaximumSlopeDegrees;
                float maximumStepUp = predictive.MaximumStepUp;
                float maximumStepDown = predictive.MaximumStepDown;
                float maximumHeightDiscontinuity = predictive.MaximumHeightDiscontinuity;
                float maximumEdgeGap = predictive.MaximumEdgeGap;
                float minimumLandingConfidence = predictive.MinimumLandingConfidence;
                float maximumPredictionReachRatio = predictive.MaximumPredictionReachRatio;

                string ownerId = $"foot-placement-profile:{settings.ProfileId}";
                for (int i = 0; i < layout.Entries.Count; i++)
                {
                    CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                    if (!string.Equals(entry.OwnerId, ownerId, StringComparison.Ordinal) ||
                        entry.Interaction != CharacterPoseTuningInteractionPolicy.TunableDefault)
                        continue;
                    CharacterPoseTuningValue value = block.GetValue(entry);
                    string fieldId = entry.FieldId;
                    if (fieldId.EndsWith("/lyra-current-grounding/hit-capacity", StringComparison.Ordinal))
                        return "Foot Placement tuning cannot change published workspace capacity.";
                    if (fieldId.EndsWith("/lyra-current-grounding/trace-above", StringComparison.Ordinal)) traceAbove = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/trace-below", StringComparison.Ordinal)) traceBelow = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/trace-radius", StringComparison.Ordinal)) traceRadius = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/hit-normal-spring-strength", StringComparison.Ordinal)) hitNormalSpringStrength = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/hit-normal-critical-damping", StringComparison.Ordinal)) hitNormalCriticalDamping = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/foot-offset-spring-strength", StringComparison.Ordinal)) footOffsetSpringStrength = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/foot-offset-critical-damping", StringComparison.Ordinal)) footOffsetCriticalDamping = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/foot-offset-target-velocity-amount", StringComparison.Ordinal)) footOffsetTargetVelocityAmount = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/pelvis-offset-spring-strength", StringComparison.Ordinal)) pelvisOffsetSpringStrength = value.FloatValue;
                    else if (fieldId.EndsWith("/lyra-current-grounding/pelvis-offset-critical-damping", StringComparison.Ordinal)) pelvisOffsetCriticalDamping = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/maximum-surface-slope-degrees", StringComparison.Ordinal)) maximumSurfaceSlopeDegrees = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/maximum-contact-surface-distance", StringComparison.Ordinal)) maximumContactSurfaceDistance = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/plant-speed-threshold", StringComparison.Ordinal)) plantSpeedThreshold = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/unalignment-speed-threshold", StringComparison.Ordinal)) unalignmentSpeedThreshold = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/plant-confidence-enter", StringComparison.Ordinal)) plantConfidenceEnter = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/plant-confidence-exit", StringComparison.Ordinal)) plantConfidenceExit = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/anchor-blend-speed", StringComparison.Ordinal)) anchorBlendSpeed = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/maximum-anchor-distance", StringComparison.Ordinal)) maximumAnchorDistance = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/maximum-pelvis-lowering", StringComparison.Ordinal)) maximumPelvisLowering = value.FloatValue;
                    else if (fieldId.EndsWith("/stance-stabilization/maximum-pelvis-raising", StringComparison.Ordinal)) maximumPelvisRaising = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/path-sphere-radius", StringComparison.Ordinal)) pathSphereRadius = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/swing-capsule-radius", StringComparison.Ordinal)) swingCapsuleRadius = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/cast-above", StringComparison.Ordinal)) castAbove = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/cast-below", StringComparison.Ordinal)) castBelow = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-slope-degrees", StringComparison.Ordinal)) maximumSlopeDegrees = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-step-up", StringComparison.Ordinal)) maximumStepUp = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-step-down", StringComparison.Ordinal)) maximumStepDown = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-height-discontinuity", StringComparison.Ordinal)) maximumHeightDiscontinuity = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-edge-gap", StringComparison.Ordinal)) maximumEdgeGap = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/minimum-landing-confidence", StringComparison.Ordinal)) minimumLandingConfidence = value.FloatValue;
                    else if (fieldId.EndsWith("/predictive/maximum-prediction-reach-ratio", StringComparison.Ordinal)) maximumPredictionReachRatio = value.FloatValue;
                }

                current = new CharacterLyraCurrentGroundingSettings(
                    current.GroundLayerMask,
                    current.HitCapacity,
                    traceAbove,
                    traceBelow,
                    traceRadius,
                    hitNormalSpringStrength,
                    hitNormalCriticalDamping,
                    footOffsetSpringStrength,
                    footOffsetCriticalDamping,
                    footOffsetTargetVelocityAmount,
                    pelvisOffsetSpringStrength,
                    pelvisOffsetCriticalDamping);
                stance = new CharacterStanceStabilizationSettings(
                    maximumSurfaceSlopeDegrees,
                    maximumContactSurfaceDistance,
                    plantSpeedThreshold,
                    unalignmentSpeedThreshold,
                    plantConfidenceEnter,
                    plantConfidenceExit,
                    anchorBlendSpeed,
                    maximumAnchorDistance,
                    maximumPelvisLowering,
                    maximumPelvisRaising);
                predictive = new CharacterPredictiveFootPlacementRuntimeSettings(
                    pathSphereRadius,
                    swingCapsuleRadius,
                    castAbove,
                    castBelow,
                    maximumSlopeDegrees,
                    maximumStepUp,
                    maximumStepDown,
                    maximumHeightDiscontinuity,
                    maximumEdgeGap,
                    minimumLandingConfidence,
                    maximumPredictionReachRatio);
                settings.ApplyTuning(current, stance, predictive);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }
    }
}
