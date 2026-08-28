using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootLandingRuntime
    {
        internal static CharacterFootLandingSnapshot ProjectBeforePrediction(
            in CharacterFootLifecycleContext context,
            in AnimationFootStepObservationSample formalFootMotion)
        {
            CharacterFootLandingContext projected = context.Landing;
            projected.BeginFrame();
            PromoteLanded(ref projected, in formalFootMotion);
            return projected.Snapshot;
        }

        internal static CharacterFootLandingSnapshot ProjectAfterPrediction(
            in CharacterFootLifecycleContext context,
            in AnimationFootStepObservationSample formalFootMotion,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            CharacterFootLandingContext projected = context.Landing;
            projected.BeginFrame();
            PromoteLanded(ref projected, in formalFootMotion);
            CaptureNextSwing(
                ref projected,
                in formalFootMotion,
                in selectedStep,
                in landingPrediction,
                in settings);
            CommitApproach(ref projected, in formalFootMotion);
            return projected.Snapshot;
        }

        internal static void Evaluate(
            ref CharacterFootLandingContext context,
            in AnimationFootStepObservationSample formalFootMotion,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            context.BeginFrame();
            PromoteLanded(ref context, in formalFootMotion);
            CaptureNextSwing(
                ref context,
                in formalFootMotion,
                in selectedStep,
                in landingPrediction,
                in settings);
            CommitApproach(ref context, in formalFootMotion);
        }

        static void PromoteLanded(
            ref CharacterFootLandingContext context,
            in AnimationFootStepObservationSample formalFootMotion)
        {
            AnimationFootMotionEventOccurrence current =
                formalFootMotion.Events.CurrentContact;
            bool hasCurrentEvent = current.IsBound;
            ulong currentEventIdentity = hasCurrentEvent ? current.Identity : 0;
            if (hasCurrentEvent &&
                context.TrackingState ==
                    CharacterFootLandingTrackingState.Committed &&
                context.NextSwingLanding.HasValue &&
                context.NextSwingLanding.LandingEventIdentity ==
                    currentEventIdentity)
            {
                context.LastLanding = context.NextSwingLanding;
                context.PromotedLanding = context.LastLanding;
                context.TrackedEventIdentity = 0;
                context.ClearNextSwing();
            }
            else if (hasCurrentEvent &&
                     context.TrackingState !=
                         CharacterFootLandingTrackingState.Committed &&
                     context.TrackedEventIdentity == currentEventIdentity)
            {
                context.TrackedEventIdentity = 0;
                context.ClearNextSwing();
            }
            if (hasCurrentEvent)
                context.ObservedCurrentEventIdentity = currentEventIdentity;
        }

        static void CaptureNextSwing(
            ref CharacterFootLandingContext context,
            in AnimationFootStepObservationSample formalFootMotion,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult diagnostics,
            in CharacterFootMotionSettings settings)
        {
            if (context.TrackingState ==
                CharacterFootLandingTrackingState.Committed)
            {
                return;
            }
            AnimationFootMotionEventFrame events = formalFootMotion.Events;
            AnimationFootMotionEventOccurrence next = events.NextLanding;
            bool predictivePhase =
                events.Phase == AnimationFootMotionEventPhase.PreSwing ||
                events.Phase == AnimationFootMotionEventPhase.Swing ||
                events.Phase == AnimationFootMotionEventPhase.ApproachContact;
            CharacterFootLandingSnapshot snapshot = context.Snapshot;
            bool validCandidate = next.IsBound &&
                                  predictivePhase &&
                                  events.TimeToLandingSeconds > 0.000001f &&
                                  next.Identity !=
                                  snapshot.LastLandingEventIdentity &&
                                  selectedStep.IsAuthoritative &&
                                  selectedStep.HasConsistentLandingEventIdentity &&
                                  selectedStep.LandingEventIdentity == next.Identity;
            if (!validCandidate)
            {
                context.InvalidateCurrent();
                return;
            }
            if (context.NextSwingLanding.HasValue &&
                context.NextSwingLanding.LandingEventIdentity != next.Identity)
            {
                context.TrackedEventIdentity = 0;
                context.ClearNextSwing();
            }
            context.TrackedEventIdentity = next.Identity;
            context.TrackingState = CharacterFootLandingTrackingState.Tracking;
            if (!diagnostics.Accepted ||
                diagnostics.LandingEventIdentity != next.Identity)
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
                CharacterFootGroundPathLanding previous =
                    context.NextSwingLanding.Resolve();
                bool sameSurface = previous.SurfaceIdentity ==
                                   diagnostics.SurfaceIdentity;
                if (sameSurface &&
                    Vector3.Distance(landingPoint, previous.Point) <
                    settings.LandingAcceptanceDistance)
                {
                    return;
                }
                context.NextSwingLanding = CharacterFootLandingFact.Create(
                    next.Identity,
                    in diagnostics);
                return;
            }
            context.NextSwingLanding = CharacterFootLandingFact.Create(
                next.Identity,
                in diagnostics);
            context.NextSwingReferencePoint = diagnostics.LandingPoint;
            context.NextSwingPredictionError = 0f;
            context.NextSwingConstraintWeight = 1f;
        }

        static void CommitApproach(
            ref CharacterFootLandingContext context,
            in AnimationFootStepObservationSample formalFootMotion)
        {
            AnimationFootMotionEventFrame events = formalFootMotion.Events;
            if (!events.InApproachContactToLanding)
                return;
            context.CommitAttempted = true;
            AnimationFootMotionEventOccurrence next = events.NextLanding;
            bool canCommit = next.IsBound &&
                             context.TrackedEventIdentity == next.Identity &&
                             context.NextSwingLanding.HasValue &&
                             context.NextSwingLanding.LandingEventIdentity ==
                                 next.Identity;
            if (!canCommit)
            {
                context.CommitUnavailable = true;
                return;
            }
            context.TrackingState =
                CharacterFootLandingTrackingState.Committed;
        }
    }
}
