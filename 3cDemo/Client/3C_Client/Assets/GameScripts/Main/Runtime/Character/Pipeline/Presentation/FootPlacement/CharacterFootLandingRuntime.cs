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
            PromoteLanded(ref projected, in formalFootMotion);
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
            PromoteLanded(ref projected, in formalFootMotion);
            CaptureCurrentContact(
                ref projected,
                in formalFootMotion,
                in landingPrediction);
            CaptureNextSwing(
                ref projected,
                in formalFootMotion,
                in landingPrediction,
                in settings);
            CommitApproach(ref projected, in formalFootMotion);
            return projected.Snapshot;
        }

        internal static void Evaluate(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            context.BeginFrame();
            PromoteLanded(ref context, in formalFootMotion);
            CaptureCurrentContact(
                ref context,
                in formalFootMotion,
                in landingPrediction);
            CaptureNextSwing(
                ref context,
                in formalFootMotion,
                in landingPrediction,
                in settings);
            CommitApproach(ref context, in formalFootMotion);
        }

        static void PromoteLanded(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion)
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

        static void CaptureCurrentContact(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult diagnostics)
        {
            if (diagnostics.StepSource !=
                    CharacterFootLandingStepSource.FormalCurrentContact ||
                !diagnostics.Accepted)
            {
                return;
            }
            AnimationFootMotionEventOccurrence current =
                formalFootMotion.Events.CurrentContact;
            if (!current.IsBound ||
                diagnostics.LandingEventIdentity != current.Identity)
            {
                return;
            }
            if ((context.PromotedLanding.HasValue &&
                 context.PromotedLanding.LandingEventIdentity ==
                     current.Identity) ||
                (context.LastLanding.HasValue &&
                 context.LastLanding.LandingEventIdentity == current.Identity))
            {
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
            if (context.TrackingState ==
                CharacterFootLandingTrackingState.Committed)
            {
                bool retainsCommittedNext = next.IsBound &&
                    context.TrackedEventIdentity == next.Identity &&
                    context.NextSwingLanding.HasValue &&
                    context.NextSwingLanding.LandingEventIdentity == next.Identity;
                if (retainsCommittedNext)
                    return;
                context.TrackedEventIdentity = 0;
                context.ClearNextSwing();
            }
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
            context.TrackingState = CharacterFootLandingTrackingState.Tracking;
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

        static void CommitApproach(
            ref CharacterFootLandingContext context,
            in AnimationFootMotionRuntimeSample formalFootMotion)
        {
            AnimationFootMotionEventFrame events = formalFootMotion.Events;
            if (!events.InApproachContactToLanding)
                return;
            AnimationFootMotionEventOccurrence next = events.NextLanding;
            if (context.TrackingState ==
                    CharacterFootLandingTrackingState.Committed &&
                next.IsBound &&
                context.TrackedEventIdentity == next.Identity &&
                context.NextSwingLanding.HasValue &&
                context.NextSwingLanding.LandingEventIdentity == next.Identity)
            {
                return;
            }
            context.CommitAttempted = true;
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
