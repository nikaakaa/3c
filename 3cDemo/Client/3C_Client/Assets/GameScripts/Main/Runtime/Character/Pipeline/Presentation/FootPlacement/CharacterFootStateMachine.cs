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
        internal ulong ObservedCurrentEventIdentity;
        internal ulong TrackedEventIdentity;
        internal CharacterFootLandingTrackingState LandingTrackingState;

        internal bool HasOutput;
        internal bool HasSwingPath;
        internal bool PlantCycleConsumed;
        internal bool HasContact;
        internal ulong SwingLandingEventIdentity;
        internal ulong ContactEventIdentity;
        internal CharacterFootConstraintState ConstraintState;
        internal CharacterFootLockResponse LockResponse;
        internal Vector3 SwingLandingPoint;
        internal Vector3 SwingResidual;
        internal Vector3 ContactAnchor;
        internal Vector3 EffectiveCorrection;
        internal Vector3 AcquireResidual;
        internal Vector3 ReleaseTargetCorrection;
        internal Vector3 ReleaseResidual;
        internal float ContactProgress;
        internal float ReleaseStartResidual;

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

        internal void ClearConstraint(bool plantCycleConsumed)
        {
            CharacterFootLandingFact lastLanding = LastLanding;
            CharacterFootLandingFact nextSwingLanding = NextSwingLanding;
            CharacterFootLandingFact promotedLanding = PromotedLanding;
            Vector3 nextSwingReferencePoint = NextSwingReferencePoint;
            float nextSwingPredictionError = NextSwingPredictionError;
            float nextSwingConstraintWeight = NextSwingConstraintWeight;
            ulong observedCurrentEventIdentity = ObservedCurrentEventIdentity;
            ulong trackedEventIdentity = TrackedEventIdentity;
            CharacterFootLandingTrackingState landingTrackingState = LandingTrackingState;
            this = default;
            LastLanding = lastLanding;
            NextSwingLanding = nextSwingLanding;
            PromotedLanding = promotedLanding;
            NextSwingReferencePoint = nextSwingReferencePoint;
            NextSwingPredictionError = nextSwingPredictionError;
            NextSwingConstraintWeight = nextSwingConstraintWeight;
            ObservedCurrentEventIdentity = observedCurrentEventIdentity;
            TrackedEventIdentity = trackedEventIdentity;
            LandingTrackingState = landingTrackingState;
            PlantCycleConsumed = plantCycleConsumed;
            ConstraintState = plantCycleConsumed
                ? CharacterFootConstraintState.UnlockedSupport
                : CharacterFootConstraintState.Swing;
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
            in AnimationBiomechanicalStepHeader currentStep,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootStateFrame frame)
        {
            Side = side;
            CurrentStep = currentStep;
            SelectedStep = selectedStep;
            LandingPrediction = landingPrediction;
            Frame = frame;
        }

        internal CharacterFootSide Side { get; }
        internal AnimationBiomechanicalStepHeader CurrentStep { get; }
        internal AnimationBiomechanicalStepHeader SelectedStep { get; }
        internal CharacterFootLandingPredictionResult LandingPrediction { get; }
        internal CharacterFootStateFrame Frame { get; }
    }

    internal static class CharacterFootStateMachine
    {
        const float GeometryEpsilon = 0.0001f;

        internal static CharacterFootLandingSnapshot ProjectLandingBeforePrediction(
            in CharacterFootStateContext context,
            in AnimationBiomechanicalStepHeader currentStep)
        {
            CharacterFootStateContext projected = context;
            projected.BeginFrame();
            PromoteLanded(ref projected, in currentStep);
            return projected.LandingSnapshot;
        }

        internal static CharacterFootLandingSnapshot ProjectLandingAfterPrediction(
            in CharacterFootStateContext context,
            in AnimationBiomechanicalStepHeader currentStep,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootMotionSettings settings)
        {
            CharacterFootStateContext projected = context;
            projected.BeginFrame();
            PromoteLanded(ref projected, in currentStep);
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
            AnimationBiomechanicalStepHeader currentStep = evaluation.CurrentStep;
            AnimationBiomechanicalStepHeader selectedStep = evaluation.SelectedStep;
            CharacterFootLandingPredictionResult landingPrediction =
                evaluation.LandingPrediction;
            CharacterFootStateFrame frame = evaluation.Frame;
            context.BeginFrame();
            PromoteLanded(ref context, in currentStep);
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
            in AnimationBiomechanicalStepHeader step)
        {
            bool hasCurrentEvent = step.IsAuthoritative &&
                                   step.HasConsistentLandingEventIdentity &&
                                   step.LandingEventIdentity != 0;
            ulong currentEventIdentity = hasCurrentEvent ? step.LandingEventIdentity : 0;
            if (context.NextSwingLanding.HasValue)
            {
                ulong acceptedEventIdentity = context.NextSwingLanding.LandingEventIdentity;
                bool completedInPlace = hasCurrentEvent &&
                                        currentEventIdentity == acceptedEventIdentity &&
                                        step.TimeToLandingSeconds <= 0.000001f;
                bool advancedToNextEvent = hasCurrentEvent &&
                                           context.ObservedCurrentEventIdentity == acceptedEventIdentity &&
                                           currentEventIdentity != acceptedEventIdentity;
                if (completedInPlace || advancedToNextEvent)
                {
                    context.LastLanding = context.LandingTrackingState ==
                                          CharacterFootLandingTrackingState.Accepted
                        ? context.NextSwingLanding
                        : default;
                    context.PromotedLanding = context.LastLanding;
                    context.TrackedEventIdentity = 0;
                    context.ClearNextSwingLanding();
                }
            }
            else if (hasCurrentEvent &&
                     step.TimeToLandingSeconds <= 0.000001f &&
                     context.TrackedEventIdentity == currentEventIdentity)
            {
                context.LastLanding = default;
                context.TrackedEventIdentity = 0;
                context.LandingTrackingState = CharacterFootLandingTrackingState.Empty;
            }
            if (hasCurrentEvent)
                context.ObservedCurrentEventIdentity = currentEventIdentity;
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
            float plantConfidence = swing.PlantConfidence;
            Vector3 swingCorrection = ResolveSwingCorrection(frame.AnimatedFoot, in swing);
            if (frame.HardOwnershipLoss)
            {
                context.ClearConstraint(
                    plantConfidence >= AnimationFootConstraintFacts.GroundedMinimumConfidence);
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
                context.EffectiveCorrection = swingCorrection;
            }
            bool preserveOutput = false;
            Vector3 desiredCorrection = swingCorrection;
            switch (context.ConstraintState)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                    ResolveSwingOutput(ref context, in frame, in swing, swingCorrection);
                    preserveOutput = true;
                    ResolveUnconstrained(ref context, in frame, ref desiredCorrection);
                    break;
                case CharacterFootConstraintState.Landing:
                    ResolveLandingIntent(
                        ref context,
                        in frame,
                        swingCorrection,
                        ref desiredCorrection,
                        ref preserveOutput);
                    break;
                case CharacterFootConstraintState.Locked:
                    ResolveContactIntent(
                        ref context,
                        in frame,
                        swingCorrection,
                        ref desiredCorrection,
                        ref preserveOutput);
                    break;
                case CharacterFootConstraintState.Releasing:
                    desiredCorrection = swingCorrection;
                    ResolveReleaseOutput(ref context, in frame, swingCorrection);
                    preserveOutput = true;
                    break;
                default:
                    throw new InvalidOperationException("Foot constraint state is invalid.");
            }
            if (!preserveOutput)
            {
                context.EffectiveCorrection =
                    context.ConstraintState == CharacterFootConstraintState.Locked &&
                    context.LockResponse == CharacterFootLockResponse.FullAnchor
                        ? desiredCorrection
                        : Advance(
                            context.EffectiveCorrection,
                            desiredCorrection,
                            frame.DeltaSeconds,
                            frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            }
            ResolveGroundFloor(ref context, in frame, in swing, swingCorrection);
            ResolveOutputState(ref context, in frame, swingCorrection);
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
            bool hasPath = swing.Accepted && frame.HasLanding &&
                           frame.Landing.LandingEventIdentity == swing.LandingEventIdentity;
            bool revised = hasPath != context.HasSwingPath ||
                           hasPath &&
                           (context.SwingLandingEventIdentity != swing.LandingEventIdentity ||
                            Vector3.Distance(context.SwingLandingPoint, frame.Landing.Point) >
                            frame.Settings.LandingUpdateDistance);
            if (revised)
                context.SwingResidual = context.EffectiveCorrection - swingCorrection;
            context.HasSwingPath = hasPath;
            context.SwingLandingEventIdentity = hasPath ? swing.LandingEventIdentity : 0;
            context.SwingLandingPoint = hasPath ? frame.Landing.Point : default;
            context.SwingResidual = Advance(
                context.SwingResidual,
                default,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            context.EffectiveCorrection = swingCorrection + context.SwingResidual;
            if (swing.Accepted)
            {
                context.EffectiveCorrection = RaiseToFloor(
                    context.EffectiveCorrection,
                    swingCorrection,
                    frame.ComponentUp);
            }
            context.SwingResidual = context.EffectiveCorrection - swingCorrection;
        }

        static void ResolveUnconstrained(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            ref Vector3 desiredCorrection)
        {
            float plantConfidence = frame.SwingMotion.PlantConfidence;
            if (plantConfidence < AnimationFootConstraintFacts.GroundedMinimumConfidence)
            {
                context.PlantCycleConsumed = false;
                context.ConstraintState = CharacterFootConstraintState.Swing;
                return;
            }
            if (context.PlantCycleConsumed)
            {
                context.ConstraintState = CharacterFootConstraintState.UnlockedSupport;
                return;
            }
            context.PlantCycleConsumed = true;
            if (!CanAcquire(in frame))
            {
                context.ConstraintState = CharacterFootConstraintState.UnlockedSupport;
                return;
            }
            Vector3 contactCorrection = ResolveContactCorrection(
                frame.AnimatedFoot,
                frame.Landing.Point);
            float horizontalError = ResolveHorizontalError(contactCorrection, frame.ComponentUp);
            if (horizontalError > frame.Settings.LockDistance)
            {
                context.ConstraintState = CharacterFootConstraintState.UnlockedSupport;
                return;
            }
            context.HasContact = true;
            context.ContactEventIdentity = frame.Landing.LandingEventIdentity;
            context.ContactAnchor = frame.Landing.Point;
            context.ConstraintState = CharacterFootConstraintState.Landing;
            context.LockResponse = CharacterFootLockResponse.None;
            context.EffectiveCorrection = RaiseToFloor(
                context.EffectiveCorrection,
                contactCorrection,
                frame.ComponentUp);
            context.AcquireResidual = context.EffectiveCorrection - contactCorrection;
            context.ContactProgress = 0f;
            context.ReleaseStartResidual = 0f;
            desiredCorrection = contactCorrection;
        }

        static void ResolveLandingIntent(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection,
            ref bool preserveOutput)
        {
            Vector3 contactCorrection = ResolveContactCorrection(
                frame.AnimatedFoot,
                context.ContactAnchor);
            float horizontalError = ResolveHorizontalError(contactCorrection, frame.ComponentUp);
            if (frame.SwingMotion.PlantConfidence <
                    AnimationFootConstraintFacts.GroundedMinimumConfidence ||
                horizontalError > frame.Settings.SlideDistance)
            {
                BeginRelease(ref context, swingCorrection);
                desiredCorrection = swingCorrection;
                preserveOutput = true;
                return;
            }
            context.ContactProgress = Mathf.Max(
                context.ContactProgress,
                ResolvePlantOwnership(frame.SwingMotion.PlantConfidence));
            desiredCorrection = contactCorrection;
            context.EffectiveCorrection = contactCorrection +
                                          context.AcquireResidual *
                                          (1f - context.ContactProgress);
            preserveOutput = true;
            if (context.ContactProgress >= 1f - GeometryEpsilon)
            {
                context.ConstraintState = CharacterFootConstraintState.Locked;
                context.LockResponse = CharacterFootLockResponse.FullAnchor;
                context.EffectiveCorrection = contactCorrection;
            }
        }

        static void ResolveContactIntent(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection,
            ref bool preserveOutput)
        {
            Vector3 fullCorrection = ResolveContactCorrection(
                frame.AnimatedFoot,
                context.ContactAnchor);
            float horizontalError = ResolveHorizontalError(fullCorrection, frame.ComponentUp);
            if (frame.SwingMotion.PlantConfidence <
                    AnimationFootConstraintFacts.LockedMinimumConfidence ||
                horizontalError > frame.Settings.SlideDistance)
            {
                BeginRelease(ref context, swingCorrection);
                desiredCorrection = swingCorrection;
                preserveOutput = true;
                return;
            }
            if (horizontalError > frame.Settings.LockDistance)
            {
                bool enteringSliding =
                    context.LockResponse != CharacterFootLockResponse.Sliding;
                context.LockResponse = CharacterFootLockResponse.Sliding;
                desiredCorrection = ResolveSlidingCorrection(
                    fullCorrection,
                    frame.ComponentUp,
                    horizontalError,
                    frame.Settings);
                preserveOutput = enteringSliding;
            }
            else
            {
                context.LockResponse = CharacterFootLockResponse.FullAnchor;
                desiredCorrection = fullCorrection;
                preserveOutput = false;
            }
        }

        static void ResolveOutputState(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection)
        {
            if (context.ConstraintState != CharacterFootConstraintState.Releasing)
                return;
            if (frame.SwingMotion.PlantConfidence >=
                    AnimationFootConstraintFacts.GroundedMinimumConfidence ||
                Vector3.Distance(context.EffectiveCorrection, swingCorrection) >
                frame.Settings.LandingUpdateDistance)
                return;
            Vector3 outputCorrection = context.EffectiveCorrection;
            context.ClearConstraint(false);
            context.HasOutput = true;
            context.EffectiveCorrection = outputCorrection;
        }

        static void BeginRelease(
            ref CharacterFootStateContext context,
            Vector3 swingCorrection)
        {
            context.ConstraintState = CharacterFootConstraintState.Releasing;
            context.LockResponse = CharacterFootLockResponse.None;
            context.ReleaseTargetCorrection = swingCorrection;
            context.ReleaseResidual = context.EffectiveCorrection - swingCorrection;
            context.ReleaseStartResidual = context.ReleaseResidual.magnitude;
        }

        static void ResolveReleaseOutput(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            Vector3 swingCorrection)
        {
            context.ReleaseResidual +=
                context.ReleaseTargetCorrection - swingCorrection;
            context.ReleaseTargetCorrection = swingCorrection;
            context.ReleaseResidual = Advance(
                context.ReleaseResidual,
                default,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            context.EffectiveCorrection = swingCorrection + context.ReleaseResidual;
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
            float horizontalError = hasContact
                ? Vector3.ProjectOnPlane(
                    context.ContactAnchor - originalSole,
                    frame.ComponentUp.normalized).magnitude
                : 0f;
            float contactOwnership = ResolveContactOwnership(in context);
            CharacterFootSupportEligibility supportEligibility =
                ResolveSupportEligibility(context.ConstraintState);
            float supportWeight = context.ConstraintState switch
            {
                CharacterFootConstraintState.Locked => 1f,
                CharacterFootConstraintState.Releasing => contactOwnership,
                _ => 0f
            };
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
                originalSole + outputCorrection,
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
                hasContact && supportEligibility != CharacterFootSupportEligibility.None
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
                originalSole + outputCorrection,
                originalAnkle + outputCorrection,
                outputCorrection,
                positionWeight,
                in contactReference,
                contactOwnership,
                supportEligibility,
                supportWeight,
                supportWeight,
                horizontalError,
                hasContact ? context.ContactEventIdentity : 0,
                in pelvisReachReference);
        }

        static float ResolveContactOwnership(in CharacterFootStateContext context)
        {
            switch (context.ConstraintState)
            {
                case CharacterFootConstraintState.Landing:
                    return context.ContactProgress;
                case CharacterFootConstraintState.Locked:
                    return 1f;
                case CharacterFootConstraintState.Releasing:
                    if (context.ReleaseStartResidual <= GeometryEpsilon)
                        return 0f;
                    return Mathf.Clamp01(
                        context.ReleaseResidual.magnitude /
                        context.ReleaseStartResidual);
                default:
                    return 0f;
            }
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

        static void ResolveGroundFloor(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            in CharacterFootSwingMotionResult swing,
            Vector3 swingCorrection)
        {
            Vector3 floorCorrection;
            switch (context.ConstraintState)
            {
                case CharacterFootConstraintState.Swing when swing.Accepted:
                case CharacterFootConstraintState.UnlockedSupport when swing.Accepted:
                    floorCorrection = swingCorrection;
                    break;
                case CharacterFootConstraintState.Landing:
                case CharacterFootConstraintState.Locked:
                    floorCorrection = ResolveContactCorrection(
                        frame.AnimatedFoot,
                        context.ContactAnchor);
                    break;
                default:
                    return;
            }
            context.EffectiveCorrection = RaiseToFloor(
                context.EffectiveCorrection,
                floorCorrection,
                frame.ComponentUp);
        }

        static Vector3 RaiseToFloor(
            Vector3 outputCorrection,
            Vector3 floorCorrection,
            Vector3 componentUp)
        {
            Vector3 up = componentUp.normalized;
            float missing = Vector3.Dot(floorCorrection - outputCorrection, up);
            return missing > 0f
                ? outputCorrection + up * missing
                : outputCorrection;
        }

        static float ResolvePlantOwnership(float plantConfidence) =>
            Mathf.InverseLerp(
                AnimationFootConstraintFacts.GroundedMinimumConfidence,
                AnimationFootConstraintFacts.LockedMinimumConfidence,
                plantConfidence);

        static bool CanAcquire(in CharacterFootStateFrame frame) =>
            frame.HasLanding && frame.Landing.LandingEventIdentity != 0;

        static Vector3 ResolveSwingCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            in CharacterFootSwingMotionResult swing) =>
            swing.Accepted
                ? swing.CorrectedAnkle - foot.AnklePosition
                : default;

        static Vector3 ResolveContactCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 contactAnchor) =>
            contactAnchor - ResolveOriginalSole(foot);

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
                !float.IsFinite(frame.SwingMotion.PlantConfidence) ||
                frame.SwingMotion.PlantConfidence < 0f ||
                frame.SwingMotion.PlantConfidence > 1f)
                throw new InvalidOperationException("Foot state frame is invalid.");
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }
}
