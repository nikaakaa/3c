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

    [Flags]
    internal enum CharacterFootPathRevisionReason : byte
    {
        None = 0,
        PathAvailabilityChanged = 1,
        LandingEventChanged = 2,
        LandingPointChanged = 4,
        SwingTargetChanged = 8
    }

    internal readonly struct CharacterFootPathContinuityFact
    {
        internal CharacterFootPathContinuityFact(
            bool evaluated,
            CharacterFootPathRevisionReason revisionReason,
            bool residualRebuilt,
            bool pathAvailableBefore,
            bool pathAvailableAfter,
            ulong previousLandingEventIdentity,
            ulong currentLandingEventIdentity,
            Vector3 previousTargetCorrection,
            Vector3 currentTargetCorrection,
            float landingPointDelta,
            float targetDelta,
            Vector3 residualBeforeRevision,
            Vector3 residualBeforeDecay,
            Vector3 residualAfterDecay,
            float landingUpdateDistance,
            float timeToLandingSeconds,
            float baseHalfLifeSeconds,
            bool deadlineHalfLifeAvailable,
            float deadlineHalfLifeSeconds,
            float appliedHalfLifeSeconds)
        {
            Evaluated = evaluated;
            RevisionReason = revisionReason;
            ResidualRebuilt = residualRebuilt;
            PathAvailableBefore = pathAvailableBefore;
            PathAvailableAfter = pathAvailableAfter;
            PreviousLandingEventIdentity = previousLandingEventIdentity;
            CurrentLandingEventIdentity = currentLandingEventIdentity;
            PreviousTargetCorrection = previousTargetCorrection;
            CurrentTargetCorrection = currentTargetCorrection;
            LandingPointDelta = landingPointDelta;
            TargetDelta = targetDelta;
            ResidualBeforeRevision = residualBeforeRevision;
            ResidualBeforeDecay = residualBeforeDecay;
            ResidualAfterDecay = residualAfterDecay;
            ResidualOutputCorrection =
                currentTargetCorrection + residualAfterDecay;
            LandingUpdateDistance = landingUpdateDistance;
            TimeToLandingSeconds = timeToLandingSeconds;
            BaseHalfLifeSeconds = baseHalfLifeSeconds;
            DeadlineHalfLifeAvailable = deadlineHalfLifeAvailable;
            DeadlineHalfLifeSeconds = deadlineHalfLifeSeconds;
            AppliedHalfLifeSeconds = appliedHalfLifeSeconds;
            StateBefore = default;
            StateAfter = default;
            LockResponseBefore = default;
            LockResponseAfter = default;
            OutputStagesAvailable = false;
            ReleasingCompletedToSwing = false;
            SafetyFloorAvailable = false;
            CorrectionBeforeSafetyFloor = default;
            SafetyFloorMinimumCorrection = default;
            SafetyFloorOutputCorrection = default;
            FinalEffectiveCorrection = default;
            SafetyFloorClamped = false;
            SafetyFloorClampMeters = 0f;
            SafetyFloorClearanceBeforeMeters = 0f;
            SafetyFloorClearanceAfterMeters = 0f;
        }

        CharacterFootPathContinuityFact(
            in CharacterFootPathContinuityFact source,
            CharacterFootConstraintState stateBefore,
            CharacterFootConstraintState stateAfter,
            CharacterFootLockResponse lockResponseBefore,
            CharacterFootLockResponse lockResponseAfter,
            bool safetyFloorAvailable,
            Vector3 correctionBeforeSafetyFloor,
            Vector3 safetyFloorMinimumCorrection,
            Vector3 safetyFloorOutputCorrection,
            Vector3 finalEffectiveCorrection,
            bool safetyFloorClamped,
            float safetyFloorClampMeters,
            float safetyFloorClearanceBeforeMeters,
            float safetyFloorClearanceAfterMeters)
        {
            Evaluated = source.Evaluated;
            RevisionReason = source.RevisionReason;
            ResidualRebuilt = source.ResidualRebuilt;
            PathAvailableBefore = source.PathAvailableBefore;
            PathAvailableAfter = source.PathAvailableAfter;
            PreviousLandingEventIdentity = source.PreviousLandingEventIdentity;
            CurrentLandingEventIdentity = source.CurrentLandingEventIdentity;
            PreviousTargetCorrection = source.PreviousTargetCorrection;
            CurrentTargetCorrection = source.CurrentTargetCorrection;
            LandingPointDelta = source.LandingPointDelta;
            TargetDelta = source.TargetDelta;
            ResidualBeforeRevision = source.ResidualBeforeRevision;
            ResidualBeforeDecay = source.ResidualBeforeDecay;
            ResidualAfterDecay = source.ResidualAfterDecay;
            ResidualOutputCorrection = source.ResidualOutputCorrection;
            LandingUpdateDistance = source.LandingUpdateDistance;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            BaseHalfLifeSeconds = source.BaseHalfLifeSeconds;
            DeadlineHalfLifeAvailable = source.DeadlineHalfLifeAvailable;
            DeadlineHalfLifeSeconds = source.DeadlineHalfLifeSeconds;
            AppliedHalfLifeSeconds = source.AppliedHalfLifeSeconds;
            StateBefore = stateBefore;
            StateAfter = stateAfter;
            LockResponseBefore = lockResponseBefore;
            LockResponseAfter = lockResponseAfter;
            OutputStagesAvailable = true;
            ReleasingCompletedToSwing =
                stateBefore == CharacterFootConstraintState.Releasing &&
                stateAfter == CharacterFootConstraintState.Swing;
            SafetyFloorAvailable = safetyFloorAvailable;
            CorrectionBeforeSafetyFloor = correctionBeforeSafetyFloor;
            SafetyFloorMinimumCorrection = safetyFloorMinimumCorrection;
            SafetyFloorOutputCorrection = safetyFloorOutputCorrection;
            FinalEffectiveCorrection = finalEffectiveCorrection;
            SafetyFloorClamped = safetyFloorClamped;
            SafetyFloorClampMeters = safetyFloorClampMeters;
            SafetyFloorClearanceBeforeMeters = safetyFloorClearanceBeforeMeters;
            SafetyFloorClearanceAfterMeters = safetyFloorClearanceAfterMeters;
        }

        internal bool Evaluated { get; }
        internal CharacterFootPathRevisionReason RevisionReason { get; }
        internal bool ResidualRebuilt { get; }
        internal bool PathAvailableBefore { get; }
        internal bool PathAvailableAfter { get; }
        internal ulong PreviousLandingEventIdentity { get; }
        internal ulong CurrentLandingEventIdentity { get; }
        internal Vector3 PreviousTargetCorrection { get; }
        internal Vector3 CurrentTargetCorrection { get; }
        internal float LandingPointDelta { get; }
        internal float TargetDelta { get; }
        internal Vector3 ResidualBeforeRevision { get; }
        internal Vector3 ResidualBeforeDecay { get; }
        internal Vector3 ResidualAfterDecay { get; }
        internal Vector3 ResidualOutputCorrection { get; }
        internal float LandingUpdateDistance { get; }
        internal float TimeToLandingSeconds { get; }
        internal float BaseHalfLifeSeconds { get; }
        internal bool DeadlineHalfLifeAvailable { get; }
        internal float DeadlineHalfLifeSeconds { get; }
        internal float AppliedHalfLifeSeconds { get; }
        internal CharacterFootConstraintState StateBefore { get; }
        internal CharacterFootConstraintState StateAfter { get; }
        internal CharacterFootLockResponse LockResponseBefore { get; }
        internal CharacterFootLockResponse LockResponseAfter { get; }
        internal bool OutputStagesAvailable { get; }
        internal bool ReleasingCompletedToSwing { get; }
        internal bool SafetyFloorAvailable { get; }
        internal Vector3 CorrectionBeforeSafetyFloor { get; }
        internal Vector3 SafetyFloorMinimumCorrection { get; }
        internal Vector3 SafetyFloorOutputCorrection { get; }
        internal Vector3 FinalEffectiveCorrection { get; }
        internal bool SafetyFloorClamped { get; }
        internal float SafetyFloorClampMeters { get; }
        internal float SafetyFloorClearanceBeforeMeters { get; }
        internal float SafetyFloorClearanceAfterMeters { get; }

        internal CharacterFootPathContinuityFact Complete(
            CharacterFootConstraintState stateBefore,
            CharacterFootConstraintState stateAfter,
            CharacterFootLockResponse lockResponseBefore,
            CharacterFootLockResponse lockResponseAfter,
            bool safetyFloorAvailable,
            Vector3 correctionBeforeSafetyFloor,
            Vector3 safetyFloorMinimumCorrection,
            Vector3 safetyFloorOutputCorrection,
            Vector3 finalEffectiveCorrection,
            bool safetyFloorClamped,
            float safetyFloorClampMeters,
            float safetyFloorClearanceBeforeMeters,
            float safetyFloorClearanceAfterMeters) =>
            new CharacterFootPathContinuityFact(
                in this,
                stateBefore,
                stateAfter,
                lockResponseBefore,
                lockResponseAfter,
                safetyFloorAvailable,
                correctionBeforeSafetyFloor,
                safetyFloorMinimumCorrection,
                safetyFloorOutputCorrection,
                finalEffectiveCorrection,
                safetyFloorClamped,
                safetyFloorClampMeters,
                safetyFloorClearanceBeforeMeters,
                safetyFloorClearanceAfterMeters);

        internal static CharacterFootPathContinuityFact CreateUnevaluated(
            float timeToLandingSeconds,
            CharacterFootMotionSettings settings) =>
            new CharacterFootPathContinuityFact(
                false,
                CharacterFootPathRevisionReason.None,
                false,
                false,
                false,
                0,
                0,
                default,
                default,
                0f,
                0f,
                default,
                default,
                default,
                settings.LandingUpdateDistance,
                timeToLandingSeconds,
                settings.EffectiveCorrectionHalfLifeSeconds,
                false,
                0f,
                settings.EffectiveCorrectionHalfLifeSeconds);
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
        internal int ContactSurfaceIdentity;
        internal CharacterFootConstraintState ConstraintState;
        internal CharacterFootLockResponse LockResponse;
        internal Vector3 SwingLandingPoint;
        internal Vector3 SwingTargetCorrection;
        internal Vector3 SwingResidual;
        internal Vector3 ContactAnchor;
        internal Vector3 ContactNormal;
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
            in CharacterFootCurrentGroundFloorResult currentGroundFloor,
            bool hasContactLanding,
            in CharacterFootGroundPathLanding contactLanding,
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
            CurrentGroundFloor = currentGroundFloor;
            HasContactLanding = hasContactLanding;
            ContactLanding = contactLanding;
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
        internal CharacterFootCurrentGroundFloorResult CurrentGroundFloor { get; }
        internal bool HasContactLanding { get; }
        internal CharacterFootGroundPathLanding ContactLanding { get; }
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
            if (frame.CurrentGroundFloor.Side != evaluation.Side)
                throw new InvalidOperationException(
                    "Current Ground Floor side does not match the evaluation.");
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
                selectedStep.IsValid ? selectedStep.TimeToLandingSeconds : 0f,
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
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            out CharacterFootSwingMotionResult result)
        {
            RequireValid(in frame);
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            float plantConfidence = swing.PlantConfidence;
            Vector3 swingCorrection = ResolveSwingCorrection(frame.AnimatedFoot, in swing);
            CharacterFootConstraintState stateBefore = context.ConstraintState;
            CharacterFootLockResponse lockResponseBefore = context.LockResponse;
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
            CharacterFootPathContinuityFact continuityFact =
                CharacterFootPathContinuityFact.CreateUnevaluated(
                    timeToLandingSeconds,
                    frame.Settings);
            switch (context.ConstraintState)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                    ResolveSwingOutput(
                        ref context,
                        in frame,
                        in swing,
                        swingCorrection,
                        timeToLandingSeconds,
                        out continuityFact);
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
            ResolveOutputState(ref context, in frame, swingCorrection);
            Vector3 correctionBeforeSafetyFloor = context.EffectiveCorrection;
            bool floorResolved = ResolveGroundFloor(
                ref context,
                in frame,
                in swing,
                out bool safetyFloorAvailable,
                out Vector3 safetyFloorMinimumCorrection);
            Vector3 up = frame.ComponentUp.normalized;
            float safetyFloorClampMeters = safetyFloorAvailable
                ? Mathf.Max(
                    0f,
                    Vector3.Dot(
                        context.EffectiveCorrection - correctionBeforeSafetyFloor,
                        up))
                : 0f;
            float safetyFloorClearanceBeforeMeters = safetyFloorAvailable
                ? Vector3.Dot(
                    correctionBeforeSafetyFloor -
                    safetyFloorMinimumCorrection,
                    up)
                : 0f;
            float safetyFloorClearanceAfterMeters = safetyFloorAvailable
                ? Vector3.Dot(
                    context.EffectiveCorrection -
                    safetyFloorMinimumCorrection,
                    up)
                : 0f;
            continuityFact = continuityFact.Complete(
                stateBefore,
                context.ConstraintState,
                lockResponseBefore,
                context.LockResponse,
                floorResolved && safetyFloorAvailable,
                correctionBeforeSafetyFloor,
                safetyFloorAvailable
                    ? safetyFloorMinimumCorrection
                    : default,
                context.EffectiveCorrection,
                context.EffectiveCorrection,
                safetyFloorClampMeters > 0f,
                safetyFloorClampMeters,
                safetyFloorClearanceBeforeMeters,
                safetyFloorClearanceAfterMeters);
            return BuildOutput(
                ref context,
                side,
                in frame,
                in swing,
                desiredCorrection,
                in continuityFact,
                out result);
        }

        static void ResolveSwingOutput(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            in CharacterFootSwingMotionResult swing,
            Vector3 swingCorrection,
            float timeToLandingSeconds,
            out CharacterFootPathContinuityFact continuityFact)
        {
            bool pathAvailableBefore = context.HasSwingPath;
            ulong previousLandingEventIdentity = context.SwingLandingEventIdentity;
            Vector3 previousLandingPoint = context.SwingLandingPoint;
            Vector3 previousTargetCorrection = context.SwingTargetCorrection;
            Vector3 residualBeforeRevision = context.SwingResidual;
            CharacterFootSwingPathReference swingPath =
                swing.SwingPathReference;
            bool hasPath = swing.Accepted &&
                           swingPath.IsAvailable &&
                           swingPath.LandingEventIdentity ==
                           swing.LandingEventIdentity;
            bool comparablePath = pathAvailableBefore && hasPath;
            float landingPointDelta = comparablePath
                ? Vector3.Distance(
                    previousLandingPoint,
                    swingPath.LandingPoint)
                : 0f;
            float targetDelta = comparablePath
                ? Vector3.Distance(previousTargetCorrection, swingCorrection)
                : 0f;
            CharacterFootPathRevisionReason revisionReason =
                CharacterFootPathRevisionReason.None;
            if (hasPath != pathAvailableBefore)
            {
                revisionReason |=
                    CharacterFootPathRevisionReason.PathAvailabilityChanged;
            }
            if (comparablePath &&
                previousLandingEventIdentity != swing.LandingEventIdentity)
                revisionReason |= CharacterFootPathRevisionReason.LandingEventChanged;
            if (comparablePath &&
                landingPointDelta > frame.Settings.LandingUpdateDistance)
                revisionReason |= CharacterFootPathRevisionReason.LandingPointChanged;
            if (comparablePath &&
                targetDelta > frame.Settings.LandingUpdateDistance)
                revisionReason |= CharacterFootPathRevisionReason.SwingTargetChanged;
            bool revised = revisionReason != CharacterFootPathRevisionReason.None;
            if (revised)
                context.SwingResidual = context.EffectiveCorrection - swingCorrection;
            Vector3 residualBeforeDecay = context.SwingResidual;
            context.HasSwingPath = hasPath;
            context.SwingLandingEventIdentity = hasPath ? swing.LandingEventIdentity : 0;
            context.SwingLandingPoint = hasPath
                ? swingPath.LandingPoint
                : default;
            context.SwingTargetCorrection = hasPath ? swingCorrection : default;
            float halfLifeSeconds = ResolveSwingResidualHalfLife(
                context.SwingResidual,
                timeToLandingSeconds,
                frame.Settings,
                out bool deadlineHalfLifeAvailable,
                out float deadlineHalfLifeSeconds);
            context.SwingResidual = Advance(
                context.SwingResidual,
                default,
                frame.DeltaSeconds,
                halfLifeSeconds);
            context.EffectiveCorrection = swingCorrection + context.SwingResidual;
            context.SwingResidual = context.EffectiveCorrection - swingCorrection;
            continuityFact = new CharacterFootPathContinuityFact(
                true,
                revisionReason,
                revised,
                pathAvailableBefore,
                hasPath,
                previousLandingEventIdentity,
                hasPath ? swing.LandingEventIdentity : 0,
                previousTargetCorrection,
                hasPath ? swingCorrection : default,
                landingPointDelta,
                targetDelta,
                residualBeforeRevision,
                residualBeforeDecay,
                context.SwingResidual,
                frame.Settings.LandingUpdateDistance,
                timeToLandingSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds,
                deadlineHalfLifeAvailable,
                deadlineHalfLifeSeconds,
                halfLifeSeconds);
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
                frame.ContactLanding.Point);
            float horizontalError = ResolveHorizontalError(contactCorrection, frame.ComponentUp);
            if (horizontalError > frame.Settings.LockDistance)
            {
                context.ConstraintState = CharacterFootConstraintState.UnlockedSupport;
                return;
            }
            context.HasContact = true;
            context.ContactEventIdentity =
                frame.ContactLanding.LandingEventIdentity;
            context.ContactSurfaceIdentity = frame.ContactLanding.SurfaceIdentity;
            context.ContactAnchor = frame.ContactLanding.Point;
            context.ContactNormal = frame.ContactLanding.Normal;
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
            in CharacterFootPathContinuityFact continuityFact,
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
                swing.SwingPathReference,
                originalSole,
                originalAnkle,
                swing.Distance,
                swing.Progress,
                swing.BaselineSample,
                swing.EnvelopeSample,
                Vector3.Dot(outputCorrection, frame.ComponentUp.normalized),
                swing.LandingPredictionError,
                swing.FormalFootHeight,
                swing.DesiredSoleHeightAlongUp,
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
                desiredCorrection,
                hasContact,
                hasContact ? context.ContactSurfaceIdentity : 0,
                hasContact ? context.ContactNormal : default,
                continuityFact);
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

        static bool ResolveGroundFloor(
            ref CharacterFootStateContext context,
            in CharacterFootStateFrame frame,
            in CharacterFootSwingMotionResult swing,
            out bool safetyFloorAvailable,
            out Vector3 floorCorrection)
        {
            safetyFloorAvailable = false;
            floorCorrection = default;
            switch (context.ConstraintState)
            {
                case CharacterFootConstraintState.Swing when swing.Accepted:
                case CharacterFootConstraintState.UnlockedSupport when swing.Accepted:
                    CharacterFootCurrentGroundFloorResult currentGroundFloor =
                        frame.CurrentGroundFloor;
                    if (!currentGroundFloor.Accepted)
                        return false;
                    safetyFloorAvailable = true;
                    Vector3 up = frame.ComponentUp.normalized;
                    floorCorrection = up * Vector3.Dot(
                        currentGroundFloor.Point -
                        ResolveOriginalSole(frame.AnimatedFoot),
                        up);
                    break;
                case CharacterFootConstraintState.Landing:
                case CharacterFootConstraintState.Locked:
                    floorCorrection = ResolveContactCorrection(
                        frame.AnimatedFoot,
                        context.ContactAnchor);
                    break;
                default:
                    return false;
            }
            context.EffectiveCorrection = RaiseToFloor(
                context.EffectiveCorrection,
                floorCorrection,
                frame.ComponentUp);
            return true;
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
            frame.HasContactLanding &&
            frame.ContactLanding.LandingEventIdentity != 0;

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

        static float ResolveSwingResidualHalfLife(
            Vector3 residual,
            float timeToLandingSeconds,
            CharacterFootMotionSettings settings,
            out bool deadlineHalfLifeAvailable,
            out float deadlineHalfLifeSeconds)
        {
            deadlineHalfLifeAvailable = false;
            deadlineHalfLifeSeconds = 0f;
            float halfLifeSeconds = settings.EffectiveCorrectionHalfLifeSeconds;
            float residualDistance = residual.magnitude;
            if (!float.IsFinite(residualDistance) ||
                residualDistance <= settings.LandingUpdateDistance ||
                !float.IsFinite(timeToLandingSeconds) ||
                timeToLandingSeconds <= 0f)
            {
                return halfLifeSeconds;
            }
            float halfLifeCount = Mathf.Log(
                residualDistance / settings.LandingUpdateDistance,
                2f);
            if (!float.IsFinite(halfLifeCount) || halfLifeCount <= 0f)
                return halfLifeSeconds;
            float candidate = timeToLandingSeconds / halfLifeCount;
            if (!float.IsFinite(candidate) || candidate <= 0f)
                return halfLifeSeconds;
            deadlineHalfLifeAvailable = true;
            deadlineHalfLifeSeconds = candidate;
            return Mathf.Min(halfLifeSeconds, candidate);
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
                frame.SwingMotion.PlantConfidence > 1f ||
                frame.SwingMotion.Accepted !=
                frame.SwingMotion.SwingPathReference.IsAvailable ||
                (frame.SwingMotion.Accepted &&
                 frame.SwingMotion.SwingPathReference.LandingEventIdentity !=
                 frame.SwingMotion.LandingEventIdentity) ||
                frame.CurrentGroundFloor.State !=
                    CharacterFootCurrentGroundFloorState.Rejected &&
                frame.CurrentGroundFloor.State !=
                    CharacterFootCurrentGroundFloorState.Accepted ||
                (frame.SwingMotion.Accepted &&
                 frame.CurrentGroundFloor.RejectReason ==
                 CharacterFootCurrentGroundFloorRejectReason.SwingUnavailable) ||
                (!frame.SwingMotion.Accepted &&
                 frame.CurrentGroundFloor.RejectReason !=
                 CharacterFootCurrentGroundFloorRejectReason.SwingUnavailable) ||
                (frame.CurrentGroundFloor.Accepted &&
                 frame.CurrentGroundFloor.RejectReason !=
                 CharacterFootCurrentGroundFloorRejectReason.None) ||
                (frame.CurrentGroundFloor.State ==
                     CharacterFootCurrentGroundFloorState.Rejected &&
                 frame.CurrentGroundFloor.RejectReason ==
                 CharacterFootCurrentGroundFloorRejectReason.None) ||
                (frame.CurrentGroundFloor.Accepted &&
                 (frame.CurrentGroundFloor.Query.Purpose !=
                      CharacterFootPlacementQueryPurpose.CurrentSwingFloor ||
                  frame.CurrentGroundFloor.SurfaceIdentity == 0 ||
                  !Finite(frame.CurrentGroundFloor.Point) ||
                  !Finite(frame.CurrentGroundFloor.Normal) ||
                  frame.CurrentGroundFloor.Normal.sqrMagnitude <= GeometryEpsilon ||
                  !float.IsFinite(frame.CurrentGroundFloor.Distance) ||
                  frame.CurrentGroundFloor.Distance < 0f)) ||
                (frame.HasContactLanding &&
                 frame.ContactLanding.LandingEventIdentity == 0))
                throw new InvalidOperationException("Foot state frame is invalid.");
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }
}
