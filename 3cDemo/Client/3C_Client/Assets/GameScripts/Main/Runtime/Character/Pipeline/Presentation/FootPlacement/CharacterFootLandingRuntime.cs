using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootLandingRuntime
    {
        internal static CharacterFootLandingSnapshot ProjectBeforePrediction(
            in CharacterFootLifecycleContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion)
        {
            CharacterFootLandingContext projected = context.Landing;
            projected.BeginFrame();
            PrepareCurrentContact(ref projected, in formalFootMotion);
            return projected.Snapshot;
        }

        internal static CharacterFootLandingSnapshot ProjectAfterPrediction(
            in CharacterFootLifecycleContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            CharacterFootLandingContext projected = context.Landing;
            projected.BeginFrame();
            PrepareCurrentContact(ref projected, in formalFootMotion);
            CaptureCurrentContact(
                ref projected,
                in formalFootMotion,
                in landingPrediction);
            CaptureNextSwing(
                ref projected,
                in formalFootMotion,
                in landingPrediction,
                in settings);
            return projected.Snapshot;
        }

        internal static void Evaluate(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            context.BeginFrame();
            PrepareCurrentContact(ref context, in formalFootMotion);
            CaptureCurrentContact(
                ref context,
                in formalFootMotion,
                in landingPrediction);
            CaptureNextSwing(
                ref context,
                in formalFootMotion,
                in landingPrediction,
                in settings);
        }

        static void PrepareCurrentContact(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion)
        {
            AnimationFootMotionEventOccurrence current =
                formalFootMotion.Events.CurrentContact;
            bool hasCurrentEvent = current.IsBound;
            ulong currentEventIdentity = hasCurrentEvent ? current.Identity : 0;
            if (hasCurrentEvent &&
                context.TrackedEventIdentity == currentEventIdentity)
            {
                context.TrackedEventIdentity = 0;
                context.ClearNextSwing();
            }
            if (hasCurrentEvent)
                context.ObservedCurrentEventIdentity = currentEventIdentity;
        }

        static void CaptureCurrentContact(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult diagnostics)
        {
            if (diagnostics.StepSource !=
                CharacterFootLandingStepSource.FormalCurrentContact)
                return;
            AnimationFootMotionEventOccurrence current =
                formalFootMotion.Events.CurrentContact;
            if (!current.IsBound ||
                diagnostics.LandingEventIdentity != current.Identity)
            {
                return;
            }
            context.PlantVerificationAttempted = true;
            if (!diagnostics.Accepted)
            {
                context.PlantVerificationUnavailable = true;
                return;
            }
            context.LastLanding = CharacterFootLandingFact.Create(
                current.Identity,
                in diagnostics);
            context.PromotedLanding = context.LastLanding;
        }

        static void CaptureNextSwing(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult diagnostics,
            in CharacterFootMotionSettings settings)
        {
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
                                  snapshot.LastLandingEventIdentity;
            if (!validCandidate)
            {
                if (!next.IsBound ||
                    context.TrackedEventIdentity != next.Identity)
                {
                    context.TrackedEventIdentity = 0;
                    context.ClearNextSwing();
                }
                else
                {
                    context.RetainTracking();
                }
                return;
            }
            if (context.NextSwingLanding.HasValue &&
                context.NextSwingLanding.LandingEventIdentity != next.Identity)
            {
                context.TrackedEventIdentity = 0;
                context.ClearNextSwing();
            }
            context.TrackedEventIdentity = next.Identity;
            context.NextTrackingState =
                CharacterFootNextLandingTrackingState.Tracking;
            if (!diagnostics.Accepted ||
                diagnostics.LandingEventIdentity != next.Identity)
            {
                context.RetainTracking();
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

    }
}
