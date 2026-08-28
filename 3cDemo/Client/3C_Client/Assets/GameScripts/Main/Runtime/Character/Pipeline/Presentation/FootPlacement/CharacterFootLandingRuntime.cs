using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootLandingRuntime
    {
        internal static CharacterFootLandingSnapshot ProjectBeforePrediction(
            in CharacterFootLifecycleContext context,
            in AnimationBiomechanicalStepHeader currentStep)
        {
            CharacterFootLandingContext projected = context.Landing;
            projected.BeginFrame();
            PromoteLanded(ref projected, in currentStep);
            return projected.Snapshot;
        }

        internal static CharacterFootLandingSnapshot ProjectAfterPrediction(
            in CharacterFootLifecycleContext context,
            in AnimationBiomechanicalStepHeader currentStep,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            CharacterFootLandingContext projected = context.Landing;
            projected.BeginFrame();
            PromoteLanded(ref projected, in currentStep);
            CaptureNextSwing(
                ref projected,
                in selectedStep,
                in landingPrediction,
                in settings);
            return projected.Snapshot;
        }

        internal static void Evaluate(
            ref CharacterFootLandingContext context,
            in AnimationBiomechanicalStepHeader currentStep,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            context.BeginFrame();
            PromoteLanded(ref context, in currentStep);
            CaptureNextSwing(
                ref context,
                in selectedStep,
                in landingPrediction,
                in settings);
        }

        static void PromoteLanded(
            ref CharacterFootLandingContext context,
            in AnimationBiomechanicalStepHeader step)
        {
            bool hasCurrentEvent = step.IsAuthoritative &&
                                   step.HasConsistentLandingEventIdentity &&
                                   step.LandingEventIdentity != 0;
            ulong currentEventIdentity = hasCurrentEvent
                ? step.LandingEventIdentity
                : 0;
            if (context.NextSwingLanding.HasValue)
            {
                ulong acceptedEventIdentity =
                    context.NextSwingLanding.LandingEventIdentity;
                bool completedInPlace = hasCurrentEvent &&
                                        currentEventIdentity == acceptedEventIdentity &&
                                        step.TimeToLandingSeconds <= 0.000001f;
                bool advancedToNextEvent = hasCurrentEvent &&
                                           context.ObservedCurrentEventIdentity == acceptedEventIdentity &&
                                           currentEventIdentity != acceptedEventIdentity;
                if (completedInPlace || advancedToNextEvent)
                {
                    context.LastLanding = context.TrackingState ==
                                          CharacterFootLandingTrackingState.Accepted
                        ? context.NextSwingLanding
                        : default;
                    context.PromotedLanding = context.LastLanding;
                    context.TrackedEventIdentity = 0;
                    context.ClearNextSwing();
                }
            }
            else if (hasCurrentEvent &&
                     step.TimeToLandingSeconds <= 0.000001f &&
                     context.TrackedEventIdentity == currentEventIdentity)
            {
                context.LastLanding = default;
                context.TrackedEventIdentity = 0;
                context.TrackingState =
                    CharacterFootLandingTrackingState.Empty;
            }
            if (hasCurrentEvent)
                context.ObservedCurrentEventIdentity = currentEventIdentity;
        }

        static void CaptureNextSwing(
            ref CharacterFootLandingContext context,
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionResult diagnostics,
            in CharacterFootMotionSettings settings)
        {
            CharacterFootLandingSnapshot snapshot = context.Snapshot;
            bool validCandidate = step.IsAuthoritative &&
                                  step.HasConsistentLandingEventIdentity &&
                                  (step.IsPreSwing || step.IsSwing) &&
                                  step.TimeToLandingSeconds > 0.000001f &&
                                  step.LandingEventIdentity != 0 &&
                                  step.LandingEventIdentity !=
                                  snapshot.LastLandingEventIdentity;
            if (!validCandidate)
            {
                context.InvalidateCurrent();
                return;
            }
            if (context.NextSwingLanding.HasValue &&
                context.NextSwingLanding.LandingEventIdentity !=
                step.LandingEventIdentity)
            {
                context.TrackedEventIdentity = 0;
                context.ClearNextSwing();
            }
            context.TrackedEventIdentity = step.LandingEventIdentity;
            if (!context.NextSwingLanding.HasValue)
                context.TrackingState = CharacterFootLandingTrackingState.Tracking;
            if (!diagnostics.Accepted ||
                diagnostics.LandingEventIdentity != step.LandingEventIdentity)
            {
                context.InvalidateCurrent();
                return;
            }
            if (context.NextSwingLanding.HasValue)
            {
                Vector3 landingPoint = diagnostics.LandingPoint;
                context.NextSwingPredictionError = Vector3.Distance(
                    context.NextSwingReferencePoint,
                    landingPoint);
                context.NextSwingConstraintWeight = 1f;
                if (Vector3.Distance(
                        landingPoint,
                        context.NextSwingLanding.WorldPoint) <
                    settings.LandingUpdateDistance)
                {
                    context.TrackingState =
                        CharacterFootLandingTrackingState.Accepted;
                    return;
                }
                context.NextSwingLanding =
                    CharacterFootLandingFact.Create(in step, in diagnostics);
                context.TrackingState =
                    CharacterFootLandingTrackingState.Accepted;
                return;
            }
            context.NextSwingLanding =
                CharacterFootLandingFact.Create(in step, in diagnostics);
            context.NextSwingReferencePoint = diagnostics.LandingPoint;
            context.NextSwingPredictionError = 0f;
            context.NextSwingConstraintWeight = 1f;
            context.TrackingState = CharacterFootLandingTrackingState.Accepted;
        }
    }
}
