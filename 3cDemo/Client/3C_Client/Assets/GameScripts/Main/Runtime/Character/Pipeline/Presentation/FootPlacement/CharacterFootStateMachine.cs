using System;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterFootLandingTrackingState : byte
    {
        Empty = 0,
        Tracking = 1,
        Accepted = 2
    }

    internal readonly struct CharacterFootLandingFact
    {
        CharacterFootLandingFact(
            ulong landingEventIdentity,
            ulong trajectoryGeneration,
            string futureBodyTranslationSourceIdentity,
            int surfaceIdentity,
            Vector3 worldPoint,
            Vector3 worldNormal)
        {
            HasValue = true;
            LandingEventIdentity = landingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            FutureBodyTranslationSourceIdentity = futureBodyTranslationSourceIdentity;
            SurfaceIdentity = surfaceIdentity;
            WorldPoint = worldPoint;
            WorldNormal = worldNormal;
        }

        internal bool HasValue { get; }
        internal ulong LandingEventIdentity { get; }
        internal ulong TrajectoryGeneration { get; }
        internal string FutureBodyTranslationSourceIdentity { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 WorldPoint { get; }
        internal Vector3 WorldNormal { get; }

        internal CharacterFootGroundPathLanding Resolve() =>
            new CharacterFootGroundPathLanding(
                LandingEventIdentity,
                TrajectoryGeneration,
                FutureBodyTranslationSourceIdentity,
                SurfaceIdentity,
                WorldPoint,
                WorldNormal);

        internal static CharacterFootLandingFact Create(
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionResult diagnostics) =>
            new CharacterFootLandingFact(
                step.LandingEventIdentity,
                diagnostics.TrajectoryGeneration,
                diagnostics.FutureBodyTranslationSourceIdentity,
                diagnostics.SurfaceIdentity,
                diagnostics.LandingPoint,
                diagnostics.LandingNormal);
    }

    internal readonly struct CharacterFootLandingSnapshot
    {
        internal CharacterFootLandingSnapshot(
            CharacterFootLandingTrackingState state,
            ulong eventIdentity,
            bool hasLastLanding,
            CharacterFootGroundPathLanding lastLanding,
            bool hasNextSwingLanding,
            CharacterFootGroundPathLanding nextSwingLanding,
            float nextSwingPredictionError,
            float nextSwingConstraintWeight,
            bool hasPromotedLanding,
            CharacterFootGroundPathLanding promotedLanding)
        {
            State = state;
            EventIdentity = eventIdentity;
            HasLastLanding = hasLastLanding;
            LastLanding = lastLanding;
            HasNextSwingLanding = hasNextSwingLanding;
            NextSwingLanding = nextSwingLanding;
            NextSwingPredictionError = nextSwingPredictionError;
            NextSwingConstraintWeight = nextSwingConstraintWeight;
            HasPromotedLanding = hasPromotedLanding;
            PromotedLanding = promotedLanding;
        }

        internal CharacterFootLandingTrackingState State { get; }
        internal ulong EventIdentity { get; }
        internal bool HasLastLanding { get; }
        internal CharacterFootGroundPathLanding LastLanding { get; }
        internal ulong LastLandingEventIdentity =>
            HasLastLanding ? LastLanding.LandingEventIdentity : 0;
        internal bool HasNextSwingLanding { get; }
        internal CharacterFootGroundPathLanding NextSwingLanding { get; }
        internal float NextSwingPredictionError { get; }
        internal float NextSwingConstraintWeight { get; }
        internal bool HasPromotedLanding { get; }
        internal CharacterFootGroundPathLanding PromotedLanding { get; }

        internal bool TryResolveLanding(
            ulong landingEventIdentity,
            out CharacterFootGroundPathLanding landing)
        {
            if (landingEventIdentity != 0 &&
                HasNextSwingLanding &&
                NextSwingLanding.LandingEventIdentity == landingEventIdentity)
            {
                landing = NextSwingLanding;
                return true;
            }
            if (landingEventIdentity != 0 &&
                HasLastLanding &&
                LastLanding.LandingEventIdentity == landingEventIdentity)
            {
                landing = LastLanding;
                return true;
            }
            landing = default;
            return false;
        }
    }

    internal struct CharacterFootStateContext
    {
        internal CharacterFootLandingFact LastLanding;
        internal CharacterFootLandingFact NextSwingLanding;
        internal CharacterFootLandingFact PromotedLanding;
        internal Vector3 NextSwingReferencePoint;
        internal float NextSwingPredictionError;
        internal float NextSwingConstraintWeight;
        internal ulong TrackedEventIdentity;
        internal CharacterFootLandingTrackingState LandingTrackingState;

        internal bool HasOutput;
        internal bool HasSwingPath;
        internal bool LandingBoundaryConsumed;
        internal bool HasContact;
        internal ulong SwingLandingEventIdentity;
        internal ulong ContactEventIdentity;
        internal CharacterFootConstraintState ConstraintState;
        internal CharacterFootLockResponse LockResponse;
        internal Vector3 SwingTargetCorrection;
        internal Vector3 SwingResidual;
        internal Vector3 ContactAnchor;
        internal Vector3 EffectiveCorrection;
        internal Vector3 AcquireHorizontalResidual;
        internal float AcquireHeightResidual;

        internal CharacterFootLandingSnapshot LandingSnapshot =>
            new CharacterFootLandingSnapshot(
                LandingTrackingState,
                TrackedEventIdentity,
                LastLanding.HasValue,
                LastLanding.HasValue ? LastLanding.Resolve() : default,
                LandingTrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue,
                LandingTrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingLanding.Resolve() : default,
                LandingTrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingPredictionError : 0f,
                LandingTrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingConstraintWeight : 0f,
                PromotedLanding.HasValue,
                PromotedLanding.HasValue ? PromotedLanding.Resolve() : default);

        internal void BeginFrame() => PromotedLanding = default;

        internal void InvalidateCurrentLanding()
        {
            NextSwingPredictionError = 0f;
            NextSwingConstraintWeight = 0f;
            LandingTrackingState = TrackedEventIdentity != 0
                ? CharacterFootLandingTrackingState.Tracking
                : CharacterFootLandingTrackingState.Empty;
        }

        internal void ClearNextSwingLanding()
        {
            NextSwingLanding = default;
            NextSwingReferencePoint = default;
            NextSwingPredictionError = 0f;
            NextSwingConstraintWeight = 0f;
            LandingTrackingState = TrackedEventIdentity != 0
                ? CharacterFootLandingTrackingState.Tracking
                : CharacterFootLandingTrackingState.Empty;
        }

        internal void ClearConstraint()
        {
            CharacterFootLandingFact lastLanding = LastLanding;
            CharacterFootLandingFact nextSwingLanding = NextSwingLanding;
            CharacterFootLandingFact promotedLanding = PromotedLanding;
            Vector3 nextSwingReferencePoint = NextSwingReferencePoint;
            float nextSwingPredictionError = NextSwingPredictionError;
            float nextSwingConstraintWeight = NextSwingConstraintWeight;
            bool landingBoundaryConsumed = LandingBoundaryConsumed;
            ulong trackedEventIdentity = TrackedEventIdentity;
            CharacterFootLandingTrackingState landingTrackingState = LandingTrackingState;
            this = default;
            LastLanding = lastLanding;
            NextSwingLanding = nextSwingLanding;
            PromotedLanding = promotedLanding;
            NextSwingReferencePoint = nextSwingReferencePoint;
            NextSwingPredictionError = nextSwingPredictionError;
            NextSwingConstraintWeight = nextSwingConstraintWeight;
            LandingBoundaryConsumed = landingBoundaryConsumed;
            TrackedEventIdentity = trackedEventIdentity;
            LandingTrackingState = landingTrackingState;
            ConstraintState = CharacterFootConstraintState.Swing;
        }

        internal void ClearContact()
        {
            HasContact = false;
            ContactEventIdentity = 0;
            ContactAnchor = default;
            ConstraintState = CharacterFootConstraintState.Swing;
            LockResponse = CharacterFootLockResponse.None;
        }
    }

    internal readonly struct CharacterFootStateFrame
    {
        internal CharacterFootStateFrame(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in CharacterFootSwingMotionResult swingMotion,
            AnimationFootStepObservationSample footStepObservation,
            bool hasLanding,
            in CharacterFootGroundPathLanding landing,
            bool hardOwnershipLoss,
            float footPlacementWeight,
            Vector3 componentUp,
            float deltaSeconds,
            in CharacterFootMotionSettings settings)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            AnimatedFoot = animatedFoot;
            SwingMotion = swingMotion;
            FootStepObservation = footStepObservation.IsValid
                ? footStepObservation
                : throw new ArgumentException("Formal Foot Step observation is invalid.");
            HasLanding = hasLanding;
            Landing = landing;
            HardOwnershipLoss = hardOwnershipLoss;
            FootPlacementWeight = footPlacementWeight;
            ComponentUp = componentUp;
            DeltaSeconds = deltaSeconds;
            Settings = settings;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterFootPlacementAnimatedFootPose AnimatedFoot { get; }
        internal CharacterFootSwingMotionResult SwingMotion { get; }
        internal AnimationFootStepObservationSample FootStepObservation { get; }
        internal bool HasLanding { get; }
        internal CharacterFootGroundPathLanding Landing { get; }
        internal bool HardOwnershipLoss { get; }
        internal float FootPlacementWeight { get; }
        internal Vector3 ComponentUp { get; }
        internal float DeltaSeconds { get; }
        internal CharacterFootMotionSettings Settings { get; }
    }

    internal readonly struct CharacterFootStateEvaluation
    {
        internal CharacterFootStateEvaluation(
            CharacterFootSide side,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootStateFrame frame)
        {
            Side = side;
            SelectedStep = selectedStep;
            LandingPrediction = landingPrediction;
            Frame = frame;
        }

        internal CharacterFootSide Side { get; }
        internal AnimationBiomechanicalStepHeader SelectedStep { get; }
        internal CharacterFootLandingPredictionResult LandingPrediction { get; }
        internal CharacterFootStateFrame Frame { get; }
    }

    internal static class CharacterFootStateMachine
    {
        const float GeometryEpsilon = 0.0001f;

        internal static CharacterFootLandingSnapshot ProjectLandingBeforePrediction(
            in CharacterFootStateContext context,
            AnimationFootStepObservationSample observation)
        {
            CharacterFootStateContext projected = context;
            projected.BeginFrame();
            PromoteLanded(ref projected, observation);
            return projected.LandingSnapshot;
        }

        internal static CharacterFootLandingSnapshot ProjectLandingAfterPrediction(
            in CharacterFootStateContext context,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings,
            AnimationFootStepObservationSample observation)
        {
            CharacterFootStateContext projected = context;
            projected.BeginFrame();
            PromoteLanded(ref projected, observation);
            CaptureNextSwing(
                ref projected,
                in selectedStep,
                in landingPrediction,
                in settings);
            return projected.LandingSnapshot;
        }

        internal static CharacterResolvedFootResult Evaluate(
            ref CharacterFootStateContext context,
            in CharacterFootStateEvaluation evaluation,
            out CharacterFootSwingMotionResult result)
        {
            AnimationBiomechanicalStepHeader selectedStep = evaluation.SelectedStep;
            CharacterFootLandingPredictionResult landingPrediction =
                evaluation.LandingPrediction;
            CharacterFootStateFrame frame = evaluation.Frame;
            context.BeginFrame();
            PromoteLanded(ref context, frame.FootStepObservation);
            CaptureNextSwing(
                ref context,
                in selectedStep,
                in landingPrediction,
                frame.Settings);
            return Resolve(
                ref context,
                evaluation.Side,
                in frame,
                out result);
        }

        static void PromoteLanded(
            ref CharacterFootStateContext context,
            AnimationFootStepObservationSample observation)
        {
            if (observation.IsValid &&
                observation.LockMode == AnimationFootStepObservationLockMode.Unlocked)
            {
                context.LandingBoundaryConsumed = false;
            }
            bool landingBoundary = observation.IsValid &&
                                   IsLandingBoundary(observation) &&
                                   !context.LandingBoundaryConsumed &&
                                   context.ConstraintState == CharacterFootConstraintState.Swing;
            if (!landingBoundary)
                return;
            context.LandingBoundaryConsumed = true;
            if (context.NextSwingLanding.HasValue)
            {
                context.LastLanding = context.LandingTrackingState ==
                                      CharacterFootLandingTrackingState.Accepted
                    ? context.NextSwingLanding
                    : default;
                context.PromotedLanding = context.LastLanding;
                context.TrackedEventIdentity = 0;
                context.ClearNextSwingLanding();
            }
            else if (context.TrackedEventIdentity != 0)
            {
                context.LastLanding = default;
                context.TrackedEventIdentity = 0;
                context.LandingTrackingState = CharacterFootLandingTrackingState.Empty;
            }
        }

        static void CaptureNextSwing(
            ref CharacterFootStateContext context,
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionResult diagnostics,
            in CharacterFootMotionSettings settings)
        {
            CharacterFootLandingSnapshot snapshot = context.LandingSnapshot;
            bool validCandidate = step.IsAuthoritative &&
                                  step.HasConsistentLandingEventIdentity &&
                                  (step.IsPreSwing || step.IsSwing) &&
                                  step.TimeToLandingSeconds > 0.000001f &&
                                  step.LandingEventIdentity != 0 &&
                                  step.LandingEventIdentity != snapshot.LastLandingEventIdentity;
            if (!validCandidate)
            {
                context.InvalidateCurrentLanding();
                return;
            }
            if (context.NextSwingLanding.HasValue &&
                context.NextSwingLanding.LandingEventIdentity != step.LandingEventIdentity)
            {
                context.TrackedEventIdentity = 0;
                context.ClearNextSwingLanding();
            }
            context.TrackedEventIdentity = step.LandingEventIdentity;
            if (!context.NextSwingLanding.HasValue)
                context.LandingTrackingState = CharacterFootLandingTrackingState.Tracking;
            if (!diagnostics.Accepted || diagnostics.LandingEventIdentity != step.LandingEventIdentity)
            {
                context.InvalidateCurrentLanding();
                return;
            }
            if (context.NextSwingLanding.HasValue)
            {
                Vector3 landingPoint = diagnostics.LandingPoint;
                context.NextSwingPredictionError = Vector3.Distance(
                    context.NextSwingReferencePoint,
                    landingPoint);
                context.NextSwingConstraintWeight = 1f;
                if (Vector3.Distance(landingPoint, context.NextSwingLanding.WorldPoint) <
                    settings.LandingUpdateDistance)
                {
                    context.LandingTrackingState = CharacterFootLandingTrackingState.Accepted;
                    return;
                }
                context.NextSwingLanding = CharacterFootLandingFact.Create(in step, in diagnostics);
                context.LandingTrackingState = CharacterFootLandingTrackingState.Accepted;
                return;
            }
            context.NextSwingLanding = CharacterFootLandingFact.Create(in step, in diagnostics);
            context.NextSwingReferencePoint = diagnostics.LandingPoint;
            context.NextSwingPredictionError = 0f;
            context.NextSwingConstraintWeight = 1f;
            context.LandingTrackingState = CharacterFootLandingTrackingState.Accepted;
        }

        static CharacterResolvedFootResult Resolve(
            ref CharacterFootStateContext context,
            CharacterFootSide side,
            in CharacterFootStateFrame frame,
            out CharacterFootSwingMotionResult result)
        {
            RequireValid(in frame);
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            Vector3 swingCorrection = ResolveSwingCorrection(frame.AnimatedFoot, in swing);
            if (frame.HardOwnershipLoss)
            {
                context.ClearConstraint();
                CharacterFootSwingMotionResult suppressed =
                    CharacterFootSwingMotionBuilder.SuppressUnselected(in swing);
                return BuildOutput(
                    ref context,
                    side,
                    in frame,
                    in suppressed,
                    default,
                    out result);
            }
            if (!context.HasOutput)
            {
                context.HasOutput = true;
                context.EffectiveCorrection = default;
            }
            Vector3 desiredCorrection = swingCorrection;
            switch (context.ConstraintState)
            {
                case CharacterFootConstraintState.Swing:
                    if (!IsLandingBoundary(frame.FootStepObservation) ||
                        !CanAcquire(in context, in frame))
                        ResolveSwingOutput(ref context, in frame, in swing, swingCorrection);
                    ResolveSwingLifecycle(
                        ref context,
                        in frame,
                        ref desiredCorrection);
                    break;
                case CharacterFootConstraintState.Landing:
                    ResolveLandingLifecycle(
                        ref context,
                        in frame,
                        swingCorrection,
                        ref desiredCorrection);
                    break;
                case CharacterFootConstraintState.Locked:
                    ResolveLockedLifecycle(
                        ref context,
                        in frame,
                        swingCorrection,
                        ref desiredCorrection);
                    break;
                case CharacterFootConstraintState.Releasing:
                    ResolveReleaseLifecycle(
                        ref context,
                        in frame,
                        swingCorrection,
                        ref desiredCorrection);
                    break;
                default:
                    throw new InvalidOperationException("Foot constraint state is invalid.");
            }
            return BuildOutput(
                ref context,
                side,
                in frame,
                in swing,
                desiredCorrection,
                out result);
        }

        static void ResolveSwingOutput(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            in CharacterFootSwingMotionResult swing,
            Vector3 swingCorrection)
        {
            bool hasPath = swing.Accepted;
            bool revised = hasPath != context.HasSwingPath ||
                           hasPath &&
                           (context.SwingLandingEventIdentity != swing.LandingEventIdentity ||
                            Vector3.Distance(
                                context.SwingTargetCorrection,
                                swingCorrection) >
                            frame.Settings.LandingUpdateDistance);
            if (revised)
                context.SwingResidual = context.EffectiveCorrection - swingCorrection;
            context.HasSwingPath = hasPath;
            context.SwingLandingEventIdentity = hasPath ? swing.LandingEventIdentity : 0;
            context.SwingTargetCorrection = hasPath ? swingCorrection : default;
            context.SwingResidual = Advance(
                context.SwingResidual,
                default,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            context.EffectiveCorrection = swingCorrection + context.SwingResidual;
            context.SwingResidual = context.EffectiveCorrection - swingCorrection;
        }

        static void ResolveSwingLifecycle(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            ref Vector3 desiredCorrection)
        {
            AnimationFootStepObservationSample observation =
                frame.FootStepObservation;
            bool landingBoundary = IsLandingBoundary(observation);
            if (!landingBoundary || !CanAcquire(in context, in frame))
                return;
            desiredCorrection = ResolveContactTargetCorrection(
                frame.AnimatedFoot,
                frame.Landing.Point,
                frame.ComponentUp,
                frame.FootStepObservation.FootHeight);
            if (!CanAcquireContactTarget(
                    context.EffectiveCorrection,
                    desiredCorrection,
                    frame.ComponentUp,
                    frame.Settings))
            {
                return;
            }
            context.HasContact = true;
            context.ContactEventIdentity = frame.Landing.LandingEventIdentity;
            context.ContactAnchor = frame.Landing.Point;
            context.ConstraintState = CharacterFootConstraintState.Landing;
            context.LockResponse = CharacterFootLockResponse.None;
            Vector3 up = frame.ComponentUp.normalized;
            context.AcquireHeightResidual =
                Vector3.Dot(context.EffectiveCorrection - desiredCorrection, up);
            context.EffectiveCorrection =
                Vector3.ProjectOnPlane(context.EffectiveCorrection, up) +
                up * (Vector3.Dot(desiredCorrection, up) +
                      context.AcquireHeightResidual);
            context.AcquireHorizontalResidual = default;
        }

        static void ResolveLandingLifecycle(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection)
        {
            AnimationFootStepObservationSample observation =
                frame.FootStepObservation;
            if (observation.LockMode ==
                AnimationFootStepObservationLockMode.Unlocked)
            {
                BeginRelease(
                    ref context,
                    in frame,
                    swingCorrection,
                    ref desiredCorrection);
                context.ClearContact();
                return;
            }
            if (observation.LockMode ==
                    AnimationFootStepObservationLockMode.Sliding &&
                context.LockResponse != CharacterFootLockResponse.None)
            {
                BeginRelease(
                    ref context,
                    in frame,
                    swingCorrection,
                    ref desiredCorrection);
                return;
            }
            RefreshContactAnchor(ref context, in frame);
            desiredCorrection = ResolveContactTargetCorrection(
                frame.AnimatedFoot,
                context.ContactAnchor,
                frame.ComponentUp,
                observation.FootHeight);
            if (!CanAcquireContactTarget(
                    context.EffectiveCorrection,
                    desiredCorrection,
                    frame.ComponentUp,
                    frame.Settings))
            {
                BeginRelease(
                    ref context,
                    in frame,
                    swingCorrection,
                    ref desiredCorrection);
                context.ClearContact();
                return;
            }
            if (observation.LockMode ==
                AnimationFootStepObservationLockMode.Sliding)
            {
                Vector3 up = frame.ComponentUp.normalized;
                context.AcquireHeightResidual =
                    Vector3.Dot(context.EffectiveCorrection - desiredCorrection, up);
                context.AcquireHeightResidual = Advance(
                    context.AcquireHeightResidual,
                    0f,
                    frame.DeltaSeconds,
                    frame.Settings.EffectiveCorrectionHalfLifeSeconds);
                context.EffectiveCorrection =
                    Vector3.ProjectOnPlane(context.EffectiveCorrection, up) +
                    up * (Vector3.Dot(desiredCorrection, up) +
                          context.AcquireHeightResidual);
                return;
            }
            ResolveLockedCorrection(
                ref context,
                in frame,
                swingCorrection,
                ref desiredCorrection,
                context.LockResponse == CharacterFootLockResponse.None);
            if (context.LockResponse != CharacterFootLockResponse.FullAnchor ||
                Vector3.Distance(
                    context.EffectiveCorrection,
                    desiredCorrection) > frame.Settings.AnchorClosureDistance)
            {
                return;
            }
            context.ConstraintState = CharacterFootConstraintState.Locked;
        }

        static void ResolveLockedLifecycle(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection)
        {
            AnimationFootStepObservationLockMode mode =
                frame.FootStepObservation.LockMode;
            if (mode == AnimationFootStepObservationLockMode.Unlocked)
            {
                ClearToSwing(ref context, swingCorrection);
                return;
            }
            if (mode == AnimationFootStepObservationLockMode.Sliding)
            {
                BeginRelease(
                    ref context,
                    in frame,
                    swingCorrection,
                    ref desiredCorrection);
                return;
            }
            ResolveLockedCorrection(
                ref context,
                in frame,
                swingCorrection,
                ref desiredCorrection,
                false);
        }

        static void ResolveReleaseLifecycle(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection)
        {
            context.LockResponse = CharacterFootLockResponse.None;
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            ResolveSwingOutput(
                ref context,
                in frame,
                in swing,
                swingCorrection);
            desiredCorrection = context.SwingTargetCorrection;
            if (frame.FootStepObservation.LockMode ==
                AnimationFootStepObservationLockMode.Unlocked)
            {
                context.ClearContact();
            }
        }

        static void BeginRelease(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection)
        {
            context.ConstraintState = CharacterFootConstraintState.Releasing;
            context.LockResponse = CharacterFootLockResponse.None;
            context.AcquireHorizontalResidual = default;
            context.AcquireHeightResidual = 0f;
            context.SwingResidual = context.EffectiveCorrection - swingCorrection;
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            ResolveSwingOutput(
                ref context,
                in frame,
                in swing,
                swingCorrection);
            desiredCorrection = context.SwingTargetCorrection;
        }

        static void ResolveLockedCorrection(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection,
            bool preserveContinuity)
        {
            Vector3 fullCorrection = ResolveContactTargetCorrection(
                frame.AnimatedFoot,
                context.ContactAnchor,
                frame.ComponentUp,
                frame.FootStepObservation.FootHeight);
            float horizontalError = ResolveHorizontalError(
                fullCorrection,
                frame.ComponentUp);
            Vector3 constrainedCorrection;
            if (context.ConstraintState == CharacterFootConstraintState.Locked)
            {
                context.LockResponse = CharacterFootLockResponse.FullAnchor;
                constrainedCorrection = fullCorrection;
            }
            else if (horizontalError > frame.Settings.LockDistance)
            {
                context.LockResponse = CharacterFootLockResponse.Sliding;
                constrainedCorrection = ResolveSlidingCorrection(
                    fullCorrection,
                    frame.ComponentUp,
                    horizontalError,
                    frame.Settings);
            }
            else
            {
                context.LockResponse = CharacterFootLockResponse.FullAnchor;
                constrainedCorrection = fullCorrection;
            }
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 desiredHorizontal = Vector3.ProjectOnPlane(
                constrainedCorrection,
                up);
            desiredCorrection = desiredHorizontal +
                                up * Vector3.Dot(fullCorrection, up);
            if (preserveContinuity)
            {
                context.AcquireHorizontalResidual =
                    Vector3.ProjectOnPlane(
                        context.EffectiveCorrection - desiredCorrection,
                        up);
                context.AcquireHeightResidual =
                    Vector3.Dot(
                        context.EffectiveCorrection - desiredCorrection,
                        up);
            }
            context.AcquireHorizontalResidual = Advance(
                context.AcquireHorizontalResidual,
                default,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            context.AcquireHeightResidual = Advance(
                context.AcquireHeightResidual,
                0f,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            context.EffectiveCorrection =
                desiredCorrection + context.AcquireHorizontalResidual +
                up * context.AcquireHeightResidual;
        }

        static void RefreshContactAnchor(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame)
        {
            if (frame.HasLanding &&
                frame.Landing.LandingEventIdentity ==
                context.ContactEventIdentity)
            {
                context.ContactAnchor = frame.Landing.Point;
            }
        }

        static void ClearToSwing(
            ref CharacterFootStateContext context,
            Vector3 swingCorrection)
        {
            context.ClearConstraint();
            context.HasOutput = true;
            context.EffectiveCorrection = swingCorrection;
        }

        static CharacterResolvedFootResult BuildOutput(
            ref CharacterFootStateContext context,
            CharacterFootSide side,
            in CharacterFootStateFrame frame,
            in CharacterFootSwingMotionResult swing,
            Vector3 desiredCorrection,
            out CharacterFootSwingMotionResult result)
        {
            bool hasContact = context.HasContact;
            Vector3 outputCorrection = context.EffectiveCorrection;
            Vector3 originalSole = ResolveOriginalSole(frame.AnimatedFoot);
            Vector3 originalAnkle = frame.AnimatedFoot.AnklePosition;
            Vector3 finalSole = originalSole + outputCorrection;
            float horizontalError = hasContact
                ? Vector3.ProjectOnPlane(
                    context.ContactAnchor - finalSole,
                    frame.ComponentUp.normalized).magnitude
                : 0f;
            CharacterFootSupportEligibility supportEligibility =
                ResolveSupportEligibility(context.ConstraintState);
            float contactOwnership = hasContact
                ? frame.FootStepObservation.LockWeight
                : 0f;
            float supportIntentWeight =
                frame.FootStepObservation.Support * frame.FootPlacementWeight;
            float anchorDistance = hasContact
                ? Vector3.Distance(finalSole, context.ContactAnchor)
                : frame.Settings.SlideDistance;
            float anchorClosure = supportEligibility !=
                                  CharacterFootSupportEligibility.None
                ? Mathf.InverseLerp(
                    frame.Settings.SlideDistance,
                    frame.Settings.LockDistance,
                    anchorDistance)
                : 0f;
            float supportWeight =
                supportIntentWeight * contactOwnership * anchorClosure;
            float positionWeight = outputCorrection.sqrMagnitude >
                                   GeometryEpsilon * GeometryEpsilon
                ? frame.FootPlacementWeight
                : 0f;
            CharacterFootSwingMotionState outputState = hasContact
                ? CharacterFootSwingMotionState.Accepted
                : swing.State;
            CharacterFootSwingMotionRejectReason rejectReason = hasContact
                ? CharacterFootSwingMotionRejectReason.None
                : swing.RejectReason;
            ulong landingEventIdentity = hasContact
                ? context.ContactEventIdentity
                : swing.LandingEventIdentity;
            result = new CharacterFootSwingMotionResult(
                outputState,
                rejectReason,
                landingEventIdentity,
                swing.GroundPathInputIdentity,
                originalSole,
                originalAnkle,
                swing.Distance,
                swing.Progress,
                swing.BaselineSample,
                swing.EnvelopeSample,
                Vector3.Dot(outputCorrection, frame.ComponentUp.normalized),
                swing.LandingPredictionError,
                swing.LandingConstraintWeight,
                finalSole,
                originalAnkle + outputCorrection,
                positionWeight,
                0f,
                context.ConstraintState,
                context.LockResponse,
                horizontalError,
                contactOwnership,
                supportWeight,
                hasContact ? context.ContactAnchor : default,
                swing.PlantConfidence,
                desiredCorrection);
            var contactReference = hasContact
                ? new CharacterFootContactReference(
                    context.ContactEventIdentity,
                    context.ContactAnchor)
                : default;
            var pelvisReachReference =
                hasContact && supportWeight > GeometryEpsilon
                    ? new CharacterFootPelvisReachReference(
                        context.ContactEventIdentity,
                        context.ContactAnchor)
                    : default;
            return new CharacterResolvedFootResult(
                frame.FrameSequence,
                frame.CompletionIdentity,
                frame.RigId,
                frame.RigRevision,
                side,
                finalSole,
                originalAnkle + outputCorrection,
                outputCorrection,
                positionWeight,
                in contactReference,
                contactOwnership,
                supportEligibility,
                supportWeight,
                supportIntentWeight,
                horizontalError,
                supportWeight > GeometryEpsilon
                    ? context.ContactEventIdentity
                    : 0,
                in pelvisReachReference);
        }

        static CharacterFootSupportEligibility ResolveSupportEligibility(
            CharacterFootConstraintState state) =>
            state switch
            {
                CharacterFootConstraintState.Locked =>
                    CharacterFootSupportEligibility.AcquireAndRetain,
                CharacterFootConstraintState.Releasing =>
                    CharacterFootSupportEligibility.RetainOnly,
                _ => CharacterFootSupportEligibility.None
            };

        static bool CanAcquire(
            in CharacterFootStateContext context,
            in CharacterFootStateFrame frame) =>
            context.PromotedLanding.HasValue &&
            frame.HasLanding &&
            frame.Landing.LandingEventIdentity != 0 &&
            frame.Landing.LandingEventIdentity ==
            context.PromotedLanding.LandingEventIdentity;

        static bool CanAcquireContactTarget(
            Vector3 currentCorrection,
            Vector3 targetCorrection,
            Vector3 componentUp,
            in CharacterFootMotionSettings settings)
        {
            Vector3 up = componentUp.normalized;
            Vector3 remaining = targetCorrection - currentCorrection;
            return Vector3.ProjectOnPlane(remaining, up).magnitude <=
                   settings.SlideDistance &&
                   Mathf.Abs(Vector3.Dot(remaining, up)) <= settings.LockDistance;
        }

        static bool IsLandingBoundary(
            AnimationFootStepObservationSample observation) =>
            observation.LockMode != AnimationFootStepObservationLockMode.Unlocked;

        static Vector3 ResolveSwingCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            in CharacterFootSwingMotionResult swing) =>
            swing.Accepted
                ? swing.CorrectedAnkle - foot.AnklePosition
                : default;

        static Vector3 ResolveContactTargetCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 contactAnchor,
            Vector3 componentUp,
            float formalFootHeight)
        {
            Vector3 up = componentUp.normalized;
            Vector3 correction = contactAnchor - ResolveOriginalSole(foot);
            return Vector3.ProjectOnPlane(correction, up) +
                   up * (Vector3.Dot(correction, up) + formalFootHeight);
        }

        static Vector3 ResolveSlidingCorrection(
            Vector3 fullCorrection,
            Vector3 componentUp,
            float horizontalError,
            CharacterFootMotionSettings settings)
        {
            Vector3 up = componentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(fullCorrection, up);
            float horizontalWeight = Mathf.InverseLerp(
                settings.SlideDistance,
                settings.LockDistance,
                horizontalError);
            return horizontal * horizontalWeight +
                   up * Vector3.Dot(fullCorrection, up);
        }

        static float ResolveHorizontalError(
            Vector3 correction,
            Vector3 componentUp) =>
            Vector3.ProjectOnPlane(correction, componentUp.normalized).magnitude;

        static Vector3 ResolveOriginalSole(
            CharacterFootPlacementAnimatedFootPose foot) =>
            (foot.HeelPosition + foot.ToePosition) * 0.5f;

        static Vector3 Advance(
            Vector3 current,
            Vector3 target,
            float deltaSeconds,
            float halfLifeSeconds)
        {
            if (deltaSeconds <= 0f)
                return current;
            float alpha = 1f - Mathf.Pow(0.5f, deltaSeconds / halfLifeSeconds);
            return Vector3.LerpUnclamped(current, target, alpha);
        }

        static float Advance(
            float current,
            float target,
            float deltaSeconds,
            float halfLifeSeconds)
        {
            if (deltaSeconds <= 0f)
                return current;
            float alpha = 1f - Mathf.Pow(0.5f, deltaSeconds / halfLifeSeconds);
            return Mathf.LerpUnclamped(current, target, alpha);
        }

        static void RequireValid(in CharacterFootStateFrame frame)
        {
            if (frame.FrameSequence == 0 ||
                frame.CompletionIdentity == 0 ||
                frame.RigId.Length == 0 ||
                frame.RigRevision.Length == 0 ||
                !Finite(frame.ComponentUp) ||
                frame.ComponentUp.sqrMagnitude <= GeometryEpsilon ||
                !float.IsFinite(frame.FootPlacementWeight) ||
                frame.FootPlacementWeight < 0f ||
                frame.FootPlacementWeight > 1f ||
                !float.IsFinite(frame.DeltaSeconds) ||
                frame.DeltaSeconds < 0f ||
                !frame.FootStepObservation.IsValid)
                throw new InvalidOperationException("Foot state frame is invalid.");
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }
}
