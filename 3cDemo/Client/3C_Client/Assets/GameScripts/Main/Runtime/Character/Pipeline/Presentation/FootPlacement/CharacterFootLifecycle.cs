using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootLifecycle
    {
        internal readonly struct Completion
        {
            internal Completion(
                in CharacterFootStateEvaluation evaluation,
                in CharacterFootTransitionDecision preTransition,
                in CharacterFootStateTarget target,
                in CharacterFootInterpolationResult interpolation,
                in CharacterFootSwingMotionResult outputSwing,
                in CharacterFootPlacementRequest request,
                in CharacterFootSwingMotionResult preliminaryMotion,
                in CharacterFootLifecycleTransitionFact lifecycleTransition,
                bool landingCompletionPending,
                in CharacterFootCurrentSupportObservation outputSupport,
                in CharacterFootCurrentSupportObservation stateTargetSupport)
            {
                Evaluation = evaluation;
                PreTransition = preTransition;
                Target = target;
                Interpolation = interpolation;
                OutputSwing = outputSwing;
                Request = request;
                PreliminaryMotion = preliminaryMotion;
                LifecycleTransition = lifecycleTransition;
                LandingCompletionPending = landingCompletionPending;
                OutputSupport = outputSupport;
                StateTargetSupport = stateTargetSupport;
            }

            CharacterFootStateEvaluation Evaluation { get; }
            CharacterFootTransitionDecision PreTransition { get; }
            CharacterFootStateTarget Target { get; }
            CharacterFootInterpolationResult Interpolation { get; }
            CharacterFootSwingMotionResult OutputSwing { get; }
            CharacterFootPlacementRequest Request { get; }
            CharacterFootSwingMotionResult PreliminaryMotion { get; }
            CharacterFootLifecycleTransitionFact LifecycleTransition { get; }
            bool LandingCompletionPending { get; }
            internal CharacterFootCurrentSupportObservation OutputSupport { get; }
            internal CharacterFootCurrentSupportObservation StateTargetSupport { get; }

            internal CharacterResolvedFootResult Complete(
                ref CharacterFootLifecycleContext context,
                bool landingReachAvailable,
                out CharacterFootSwingMotionResult result)
            {
                if (!LandingCompletionPending)
                {
                    result = PreliminaryMotion;
                    CharacterFootPlacementRequest request = Request;
                    return Publish(in request);
                }
                CharacterFootStateFrame frame = Evaluation.Frame;
                CharacterFootInterpolationResult interpolation =
                    Interpolation;
                CharacterFootTransitionDecision preTransition =
                    PreTransition;
                CharacterFootStateTarget target = Target;
                ref readonly CharacterFootPathContinuityFact interpolationContinuity =
                    interpolation.ContinuityFact;
                CharacterFootSwingMotionResult outputSwing = OutputSwing;
                CharacterFootTransitionDecision postTransition =
                    CharacterFootTransitionResolver.ResolvePostInterpolation(
                        in context,
                        in frame,
                        interpolation.Completed,
                        landingReachAvailable);
                CharacterFootTransitionRuntime.Apply(
                    ref context,
                    in postTransition,
                    in frame);
                CharacterFootInterpolationRuntime.ApplyPostTransition(
                    ref context.Interpolation,
                    in postTransition);
                CharacterFootHardConstraintResult hardConstraint =
                    ResolveOutputSupportConstraint(
                        in context, in frame, in interpolation,
                        OutputSupport,
                        context.Interpolation.EffectiveCorrection);
                CharacterFootInterpolationRuntime.ApplyHardConstraint(
                    ref context.Interpolation,
                    in hardConstraint);
                CharacterFootPathContinuityFact continuityFact =
                    CompleteContinuity(
                        in interpolationContinuity,
                        in preTransition,
                        in postTransition,
                        in target,
                        in interpolation,
                        in hardConstraint,
                        frame.ComponentUp);
                CharacterFootLifecycleTransitionFact lifecycleTransition =
                    LifecycleTransition.Complete(
                        in context,
                        in preTransition,
                        in postTransition);
                Vector3 desiredCorrection = ResolveDiagnosticDesiredCorrection(
                    in context,
                    in target,
                    in frame);
                ref readonly CharacterFootSupportIntent supportIntent =
                    target.SupportIntent;
                ref readonly CharacterFootSupportTarget selectedSupportTarget =
                    interpolation.SupportTarget;
                CharacterFootStateEvaluation evaluation = Evaluation;
                CharacterFootPlacementRequest completed = BuildRequest(
                    in context,
                    in evaluation,
                    in outputSwing,
                    desiredCorrection,
                    hardConstraint.OutputCorrection,
                    in selectedSupportTarget,
                    in supportIntent,
                    in continuityFact,
                    in lifecycleTransition,
                    out result);
                return Publish(in completed);
            }
        }

        internal static CharacterFootPlacementRequest Evaluate(
            ref CharacterFootLifecycleContext context,
            in CharacterFootStateEvaluation evaluation,
            out Completion receipt)
        {
            ref readonly CharacterFootStateFrame frame = ref evaluation.Frame;
            ref readonly AnimationFootMotionRuntimeSample formalFootMotion =
                ref evaluation.FormalFootMotion;
            ref readonly CharacterFootLandingPredictionResult landingPrediction =
                ref evaluation.LandingPrediction;
            ref readonly CharacterFootMotionSettings settings = ref frame.Settings;
            CharacterFootLandingRuntime.Evaluate(
                ref context.Landing,
                in formalFootMotion,
                in landingPrediction,
                in settings);
            return Resolve(
                ref context,
                in evaluation,
                out _,
                out receipt);
        }

        static CharacterFootPlacementRequest Resolve(
            ref CharacterFootLifecycleContext context,
            in CharacterFootStateEvaluation evaluation,
            out CharacterFootSwingMotionResult result,
            out Completion receipt)
        {
            ref readonly CharacterFootStateFrame frame = ref evaluation.Frame;
            ref readonly AnimationFootMotionRuntimeSample formalFootMotion =
                ref evaluation.FormalFootMotion;
            ref readonly CharacterFootLandingPredictionResult landingPrediction =
                ref evaluation.LandingPrediction;
            float timeToLandingSeconds = formalFootMotion.HasPredictiveLanding
                ? formalFootMotion.TimeToLandingSeconds : 0f;
            RequireValid(in frame);
            CharacterFootInterpolationRuntime.RebaseOutputWeight(
                ref context.Interpolation, context.PreviousOutputWeight,
                frame.FootPlacementWeight, context.PreviousAnimatedSole);
            context.PreviousOutputWeight = frame.FootPlacementWeight;
            context.PreviousAnimatedSole = CharacterFootConstraintMath.ResolveOriginalSole(
                frame.AnimatedFoot);
            CharacterFootLifecycleTransitionFact lifecycleTransition =
                CharacterFootLifecycleTransitionFact.Begin(
                    in context,
                    in frame);
            CharacterFootTransitionDecision preTransition =
                CharacterFootTransitionResolver.ResolvePreInterpolation(
                    in context,
                    in frame);
            CharacterFootLandingRuntime.CommitCurrentContactVerification(
                ref context.Landing,
                in formalFootMotion,
                in landingPrediction,
                in preTransition);
            CharacterFootTransitionRuntime.Apply(
                ref context,
                in preTransition,
                in frame);
            CharacterFootStateTarget target =
                CharacterFootStateTargetResolver.Resolve(
                    in context,
                    in preTransition,
                    timeToLandingSeconds,
                    in frame);
            CharacterFootCurrentSupportObservation stateTargetSupport = default;
            if (!target.SuppressOutput && target.SupportTargetAvailable &&
                (target.InterpolationPolicy == CharacterFootInterpolationPolicy.ReleaseResidual ||
                 target.InterpolationPolicy == CharacterFootInterpolationPolicy.VerifiedSupport))
            {
                stateTargetSupport = QueryFootSupport(
                    in context, in evaluation, target.Correction, target.SupportTarget);
                target = CharacterFootStateTargetResolver.ConstrainStateTarget(
                    in target, in stateTargetSupport, in frame);
            }
            CharacterFootInterpolationResult interpolation;
            if (!preTransition.SuppressOutput &&
                !target.SupportTargetAvailable)
            {
                interpolation =
                    CharacterFootInterpolationRuntime.AdvanceUnavailable(
                        ref context.Interpolation,
                        in target,
                        in frame);
                ref readonly CharacterFootSwingMotionResult unavailableSwing =
                    ref frame.SwingMotion;
                result = CharacterFootSwingMotionBuilder.SuppressUnselected(
                    in unavailableSwing);
                ref readonly CharacterFootPathContinuityFact unavailableContinuity =
                    interpolation.ContinuityFact;
                result = CharacterFootSwingMotionBuilder.WithPathContinuity(
                    in result,
                    in unavailableContinuity);
                CharacterFootTransitionDecision unavailablePostTransition =
                    default;
                lifecycleTransition = lifecycleTransition.Complete(
                    in context,
                    in preTransition,
                    in unavailablePostTransition);
                result = CharacterFootSwingMotionBuilder.WithLifecycleTransition(
                    in result,
                    in lifecycleTransition);
                CharacterFootResolvedOutcome unavailableOutcome =
                    !frame.CurrentSupport.Available
                        ? CharacterFootResolvedOutcome
                            .CurrentSupportUnavailable
                        : CharacterFootResolvedOutcome
                            .SupportTargetUnavailable;
                CharacterFootPlacementRequest unavailable =
                    BuildUnavailableRequest(in evaluation, unavailableOutcome);
                receipt = new Completion(
                    in evaluation,
                    in preTransition,
                    in target,
                    in interpolation,
                    in result,
                    in unavailable,
                    in result,
                    in lifecycleTransition,
                    false,
                    default,
                    default);
                return unavailable;
            }
            interpolation =
                CharacterFootInterpolationRuntime.Evaluate(
                    ref context.Interpolation,
                    in target,
                    in frame);
            CharacterFootTransitionDecision postTransition =
                CharacterFootTransitionResolver.ResolvePostInterpolation(
                    in context,
                    in frame,
                    interpolation.Completed,
                    false);
            CharacterFootTransitionRuntime.Apply(
                ref context,
                in postTransition,
                in frame);
            CharacterFootInterpolationRuntime.ApplyPostTransition(
                ref context.Interpolation,
                in postTransition);

            ref readonly CharacterFootSwingMotionResult frameSwing = ref frame.SwingMotion;
            CharacterFootSwingMotionResult outputSwing = preTransition.SuppressOutput
                ? CharacterFootSwingMotionBuilder.SuppressUnselected(
                    in frameSwing)
                : frameSwing;
            CharacterFootCurrentSupportObservation outputSupport = preTransition.SuppressOutput
                ? default
                : QueryFootSupport(in context, in evaluation,
                    interpolation.Correction, interpolation.SupportTarget);
            CharacterFootHardConstraintResult hardConstraint =
                preTransition.SuppressOutput
                    ? new CharacterFootHardConstraintResult(
                        false,
                        false,
                        CharacterFootSafetyFloorOwner.None,
                        0,
                        0,
                        default,
                        default,
                        default)
                    : ResolveOutputSupportConstraint(
                        in context, in frame, in interpolation, in outputSupport,
                        context.Interpolation.EffectiveCorrection);
            CharacterFootInterpolationRuntime.ApplyHardConstraint(
                ref context.Interpolation,
                in hardConstraint);
            CharacterFootPathContinuityFact continuityFact =
                interpolation.ContinuityFact;
            lifecycleTransition = lifecycleTransition.Complete(
                in context,
                in preTransition,
                in postTransition);
            if (!preTransition.SuppressOutput)
            {
                continuityFact = CompleteContinuity(
                    in continuityFact,
                    in preTransition,
                    in postTransition,
                    in target,
                    in interpolation,
                    in hardConstraint,
                    frame.ComponentUp);
            }
            Vector3 desiredCorrection = ResolveDiagnosticDesiredCorrection(
                in context,
                in target,
                in frame);
            ref readonly CharacterFootSupportIntent supportIntent = ref target.SupportIntent;
            ref readonly CharacterFootSupportTarget selectedSupportTarget =
                interpolation.SupportTarget;
            CharacterFootPlacementRequest request = BuildRequest(
                in context,
                in evaluation,
                in outputSwing,
                desiredCorrection,
                hardConstraint.OutputCorrection,
                in selectedSupportTarget,
                in supportIntent,
                in continuityFact,
                in lifecycleTransition,
                out result);
            bool landingCompletionPending =
                context.Discrete.State == CharacterFootConstraintState.Landing &&
                interpolation.Completed;
            receipt = new Completion(
                in evaluation,
                in preTransition,
                in target,
                in interpolation,
                in outputSwing,
                in request,
                in result,
                in lifecycleTransition,
                landingCompletionPending,
                in outputSupport,
                in stateTargetSupport);
            return request;
        }

        static Vector3 ResolveDiagnosticDesiredCorrection(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            if (target.InterpolationPolicy == CharacterFootInterpolationPolicy.ReleaseResidual)
                return target.Correction;
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                case CharacterFootConstraintState.Releasing:
                    return CharacterFootConstraintMath.ResolveSwingCorrection(
                        frame.AnimatedFoot,
                        frame.SwingMotion);
                default:
                    return target.Correction;
            }
        }

        static CharacterFootCurrentSupportObservation QueryFootSupport(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateEvaluation evaluation,
            Vector3 correction,
            in CharacterFootSupportTarget support)
        {
            ref readonly CharacterFootStateFrame frame = ref evaluation.Frame;
            ref readonly CharacterFootPlacementAnimatedFootPose foot = ref frame.AnimatedFoot;
            float rotationWeight = context.Contact.HasContact
                ? frame.FootPlacementWeight * frame.LockRequest.Weight : 0f;
            if (!evaluation.Grounded || frame.FootPlacementWeight <= CharacterFootConstraintMath.GeometryEpsilon ||
                !TryResolveFootGoalPose(in foot,
                    CharacterFootConstraintMath.ResolveOriginalSole(foot) + correction,
                    in support, frame.FootPlacementWeight, rotationWeight,
                    out _, out _, out _, out Vector3 ankle, out Quaternion rotation))
                return default;
            CharacterFootPlacementSoleContactPose contacts = foot.ResolveSoleContacts(ankle, rotation);
            return evaluation.SoleSupportQuery.Query(in frame, in contacts);
        }

        static CharacterFootHardConstraintResult ResolveOutputSupportConstraint(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            in CharacterFootInterpolationResult interpolation,
            in CharacterFootCurrentSupportObservation outputSupport,
            Vector3 correction)
        {
            ref readonly CharacterFootSupportTarget selectedTarget =
                ref interpolation.SupportTarget;
            CharacterFootHardConstraintResult constraint = CharacterFootHardConstraintResolver.Resolve(
                in context, in frame, in selectedTarget, correction);
            if (!outputSupport.TryResolveHeightConstraint(
                    out float displacement, out int surfaceIdentity))
                return constraint;
            Vector3 up = frame.ComponentUp.normalized;
            float clearanceCorrection = displacement / frame.FootPlacementWeight;
            Vector3 minimum = interpolation.Correction + up * clearanceCorrection;
            if (constraint.Available && constraint.Owner != CharacterFootSafetyFloorOwner.PlantTarget &&
                Vector3.Dot(constraint.MinimumCorrection - minimum, up) >= 0f)
                return constraint;
            return new CharacterFootHardConstraintResult(
                true, true, CharacterFootSafetyFloorOwner.OutputFootprint,
                surfaceIdentity, 0, correction, minimum,
                CharacterFootConstraintMath.RaiseToMinimum(correction, minimum, up));
        }

        static CharacterFootPathContinuityFact CompleteContinuity(
            in CharacterFootPathContinuityFact fact,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition,
            in CharacterFootStateTarget target,
            in CharacterFootInterpolationResult interpolation,
            in CharacterFootHardConstraintResult hardConstraint,
            Vector3 componentUp)
        {
            bool available = hardConstraint.Resolved && hardConstraint.Available;
            Vector3 up = componentUp.normalized;
            float clampMeters = available
                ? Mathf.Max(
                    0f,
                    Vector3.Dot(
                        hardConstraint.OutputCorrection -
                        hardConstraint.InputCorrection,
                        up))
                : 0f;
            float clearanceBefore = available
                ? Vector3.Dot(
                    hardConstraint.InputCorrection -
                    hardConstraint.MinimumCorrection,
                    up)
                : 0f;
            float clearanceAfter = available
                ? Vector3.Dot(
                    hardConstraint.OutputCorrection -
                    hardConstraint.MinimumCorrection,
                    up)
                : 0f;
            return fact.Complete(
                in preTransition,
                in postTransition,
                in target,
                in interpolation,
                available,
                hardConstraint.Resolved
                    ? hardConstraint.Owner
                    : CharacterFootSafetyFloorOwner.None,
                hardConstraint.Resolved ? hardConstraint.SurfaceIdentity : 0,
                hardConstraint.Resolved ? hardConstraint.PathIdentity : 0,
                hardConstraint.InputCorrection,
                available ? hardConstraint.MinimumCorrection : default,
                hardConstraint.OutputCorrection,
                hardConstraint.OutputCorrection,
                clampMeters > 0f,
                clampMeters,
                clearanceBefore,
                clearanceAfter);
        }

        static CharacterFootPlacementRequest BuildRequest(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateEvaluation evaluation,
            in CharacterFootSwingMotionResult swing,
            Vector3 desiredCorrection,
            Vector3 outputCorrection,
            in CharacterFootSupportTarget supportTarget,
            in CharacterFootSupportIntent supportIntent,
            in CharacterFootPathContinuityFact continuityFact,
            in CharacterFootLifecycleTransitionFact lifecycleTransition,
            out CharacterFootSwingMotionResult result)
        {
            ref readonly CharacterFootStateFrame frame = ref evaluation.Frame;
            bool hasContact = context.Contact.HasContact;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            Vector3 originalAnkle = frame.AnimatedFoot.AnklePosition;
            Vector3 finalSole = originalSole + outputCorrection;
            float rotationWeight = hasContact
                ? frame.FootPlacementWeight * frame.LockRequest.Weight
                : 0f;
            float positionWeight = frame.FootPlacementWeight;
            CharacterFootPlacementAnimatedFootPose animatedFoot =
                frame.AnimatedFoot;
            if (!TryResolveFootGoalPose(
                    in animatedFoot,
                    finalSole,
                    in supportTarget,
                    positionWeight,
                    rotationWeight,
                    out Vector3 effectiveSole,
                    out Vector3 finalAnkle,
                    out Quaternion finalRotation,
                    out Vector3 effectiveAnkle,
                    out Quaternion effectiveRotation))
            {
                result = CharacterFootSwingMotionBuilder.SuppressUnselected(
                    in swing);
                result = CharacterFootSwingMotionBuilder.WithLifecycleTransition(
                    in result,
                    in lifecycleTransition);
                return BuildUnavailableRequest(
                    in evaluation,
                    CharacterFootResolvedOutcome.RotationProjectionUnavailable);
            }
            float horizontalError = hasContact
                ? Vector3.ProjectOnPlane(
                    context.Contact.Anchor - originalSole,
                    frame.ComponentUp.normalized).magnitude
                : 0f;
            float contactOwnership = ResolveContactOwnership(in context);
            bool hasSupportReachReference =
                TryResolveSupportReachReference(
                    in context,
                    in frame,
                    in swing,
                    supportIntent.EventIdentity,
                    out Vector3 supportReachPoint);
            bool ownsSupport = supportIntent.Available &&
                               supportIntent.Weight > 0f &&
                               hasSupportReachReference;
            CharacterFootSupportEligibility supportEligibility = ownsSupport
                ? CharacterFootSupportEligibility.AcquireAndRetain
                : CharacterFootSupportEligibility.None;
            float supportWeight = ownsSupport ? supportIntent.Weight : 0f;
            if (ownsSupport)
            {
                horizontalError = Vector3.ProjectOnPlane(
                    supportReachPoint - originalSole,
                    frame.ComponentUp.normalized).magnitude;
            }
            CharacterFootSwingMotionState outputState = hasContact
                ? CharacterFootSwingMotionState.Accepted
                : swing.State;
            CharacterFootSwingMotionRejectReason rejectReason = hasContact
                ? CharacterFootSwingMotionRejectReason.None
                : swing.RejectReason;
            ulong landingEventIdentity = hasContact
                ? context.Contact.EventIdentity
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
                swing.FormalTargetHeightAlongUp,
                Vector3.Dot(
                    outputCorrection,
                    frame.ComponentUp.normalized),
                swing.LandingPredictionError,
                finalSole,
                finalAnkle,
                positionWeight,
                rotationWeight,
                context.Discrete.State,
                context.Discrete.LockResponse,
                horizontalError,
                contactOwnership,
                supportWeight,
                ownsSupport ? supportReachPoint : default,
                desiredCorrection,
                hasContact,
                hasContact ? context.Contact.SurfaceIdentity : 0,
                hasContact ? context.Contact.Normal : default,
                continuityFact,
                lifecycleTransition: lifecycleTransition);
            var contactReference = hasContact
                ? new CharacterFootContactReference(
                    context.Contact.EventIdentity,
                    context.Contact.Anchor)
                : default;
            var pelvisReachReference =
                supportEligibility != CharacterFootSupportEligibility.None
                    ? new CharacterFootPelvisReachReference(
                        supportIntent.EventIdentity,
                        supportReachPoint)
                    : default;
            bool constrainedContactReach = hasContact &&
                                           (context.Discrete.State ==
                                                CharacterFootConstraintState.Landing ||
                                            context.Discrete.State ==
                                                CharacterFootConstraintState.Locked ||
                                            context.Discrete.State ==
                                                CharacterFootConstraintState.Releasing);
            bool predictedLandingReach =
                (context.Discrete.State ==
                      CharacterFootConstraintState.Swing ||
                  context.Discrete.State ==
                      CharacterFootConstraintState.UnlockedSupport) &&
                swing.Accepted &&
                supportTarget.Kind ==
                CharacterFootSupportTargetKind.SwingGround &&
                supportTarget.PositionEventIdentity ==
                swing.LandingEventIdentity;
            bool landingReachRequested = positionWeight >
                                         CharacterFootConstraintMath
                                             .GeometryEpsilon &&
                                         (constrainedContactReach ||
                                          predictedLandingReach);
            var landingReachRequest = landingReachRequested
                ? new CharacterFootLandingReachRequest(
                    landingEventIdentity,
                    frame.AnimatedHip,
                    effectiveAnkle,
                    frame.LegLength,
                    frame.Settings.MinimumLandingLegCompressionReserve)
                : default;
            var identity = new CharacterFootPlacementIdentity(
                frame.FrameSequence, frame.CompletionIdentity,
                frame.RigId, frame.RigRevision, evaluation.Side);
            var pose = new CharacterFootPlacementPose(
                finalSole, effectiveSole, finalAnkle, finalRotation,
                effectiveAnkle, effectiveRotation, outputCorrection,
                positionWeight, rotationWeight);
            var support = new CharacterFootSupportFacts(
                supportTarget, contactReference, contactOwnership,
                supportEligibility, supportWeight, horizontalError,
                ownsSupport ? supportIntent.EventIdentity : 0,
                pelvisReachReference);
            CharacterFootGoalTarget goalTarget = ResolveGoalTarget(
                in animatedFoot, evaluation.GoalRoot, in pose,
                CharacterFootResolvedOutcome.Ready);
            ref readonly AnimationFootMotionRuntimeSample selectedStep =
                ref evaluation.FormalFootMotion;
            bool landingReachAdmitted = evaluation.Grounded && AdmitLandingReach(
                in selectedStep, context.Discrete.State,
                outputState == CharacterFootSwingMotionState.Accepted,
                landingEventIdentity, positionWeight,
                in contactReference, in landingReachRequest);
            return new CharacterFootPlacementRequest(
                in identity, in pose, in support, in landingReachRequest,
                in goalTarget, CharacterFootResolvedOutcome.Ready,
                landingReachAdmitted, in evaluation.Stride);
        }

        static CharacterResolvedFootResult Publish(in CharacterFootPlacementRequest request) =>
            new CharacterResolvedFootResult(
                in request.Identity, in request.Pose, in request.Support,
                in request.LandingReachRequest, in request.GoalTarget,
                request.Outcome);

        static CharacterFootPlacementRequest BuildUnavailableRequest(
            in CharacterFootStateEvaluation evaluation,
            CharacterFootResolvedOutcome outcome)
        {
            ref readonly CharacterFootStateFrame frame = ref evaluation.Frame;
            ref readonly CharacterFootPlacementAnimatedFootPose foot = ref frame.AnimatedFoot;
            var identity = new CharacterFootPlacementIdentity(
                frame.FrameSequence, frame.CompletionIdentity,
                frame.RigId, frame.RigRevision, evaluation.Side);
            Vector3 sole = (foot.HeelPosition + foot.ToePosition) * 0.5f;
            var pose = new CharacterFootPlacementPose(
                sole, sole, foot.AnklePosition, foot.AnkleRotation,
                foot.AnklePosition, foot.AnkleRotation, default, 0f, 0f);
            CharacterFootGoalTarget goal = ResolveGoalTarget(
                in foot, evaluation.GoalRoot, in pose, outcome);
            return new CharacterFootPlacementRequest(
                in identity, in pose, in default, in default, in goal,
                outcome, false, in evaluation.Stride);
        }

        static bool AdmitLandingReach(
            in AnimationFootMotionRuntimeSample step,
            CharacterFootConstraintState state,
            bool accepted,
            ulong landingEventIdentity,
            float positionWeight,
            in CharacterFootContactReference contact,
            in CharacterFootLandingReachRequest reach)
        {
            if (!reach.IsAvailable ||
                positionWeight <= CharacterPoseConstraintMath.Epsilon ||
                landingEventIdentity != reach.EventIdentity)
                return false;
            if (state == CharacterFootConstraintState.Landing ||
                state == CharacterFootConstraintState.Locked ||
                state == CharacterFootConstraintState.Releasing)
                return contact.IsAvailable;
            return step.IsAuthoritative &&
                   step.HasConsistentLandingEventIdentity &&
                   step.HasPredictiveLanding && accepted &&
                   landingEventIdentity == step.LandingEventIdentity;
        }

        static CharacterFootGoalTarget ResolveGoalTarget(
            in CharacterFootPlacementAnimatedFootPose foot,
            Transform root,
            in CharacterFootPlacementPose pose,
            CharacterFootResolvedOutcome outcome)
        {
            bool hasEffectiveOutput = outcome == CharacterFootResolvedOutcome.Ready &&
                (pose.GoalWeight > CharacterPoseConstraintMath.Epsilon ||
                 pose.RotationWeight > CharacterPoseConstraintMath.Epsilon);
            Vector3 ankle = hasEffectiveOutput ? pose.FinalAnkle : foot.AnklePosition;
            Quaternion rotation = hasEffectiveOutput ? pose.FinalRotation : foot.AnkleRotation;
            float positionWeight = hasEffectiveOutput ? pose.GoalWeight : 0f;
            float rotationWeight = hasEffectiveOutput ? pose.RotationWeight : 0f;
            Vector3 componentPosition = root.InverseTransformPoint(ankle);
            Quaternion componentRotation =
                (Quaternion.Inverse(root.rotation) * rotation).normalized;
            Vector3 effectiveSole = foot.HeelPosition * 0.5f + foot.ToePosition * 0.5f;
            if (positionWeight > 0f)
            {
                Vector3 targetAnkle = root.TransformPoint(componentPosition);
                Vector3 effectiveAnkle = Vector3.LerpUnclamped(
                    foot.AnklePosition, targetAnkle, positionWeight);
                Quaternion targetRotation = (root.rotation * componentRotation).normalized;
                Quaternion effectiveRotation = Quaternion.Slerp(
                    foot.AnkleRotation, targetRotation, rotationWeight).normalized;
                CharacterFootPlacementSoleContactPose contacts =
                    foot.ResolveSoleContacts(effectiveAnkle, effectiveRotation);
                effectiveSole = (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
            }
            return new CharacterFootGoalTarget(
                componentPosition, componentRotation,
                positionWeight, rotationWeight, effectiveSole);
        }

        static bool TryResolveFootGoalPose(
            in CharacterFootPlacementAnimatedFootPose foot,
            Vector3 finalSole,
            in CharacterFootSupportTarget supportTarget,
            float positionWeight,
            float rotationWeight,
            out Vector3 effectiveSole,
            out Vector3 finalAnkle,
            out Quaternion finalRotation,
            out Vector3 effectiveAnkle,
            out Quaternion effectiveRotation)
        {
            effectiveSole = default;
            finalAnkle = default;
            finalRotation = default;
            effectiveAnkle = default;
            effectiveRotation = default;
            if (!supportTarget.IsValid ||
                !CharacterFootConstraintMath.Finite(finalSole) ||
                !float.IsFinite(positionWeight) || positionWeight < 0f ||
                positionWeight > 1f || !float.IsFinite(rotationWeight) ||
                rotationWeight < 0f || rotationWeight > 1f)
            {
                return false;
            }
            Vector3 normal = supportTarget.SupportNormal;
            Vector3 forward = Vector3.ProjectOnPlane(foot.SoleForward, normal);
            if (!CharacterFootConstraintMath.Finite(forward) ||
                forward.sqrMagnitude <=
                CharacterFootConstraintMath.GeometryEpsilon *
                CharacterFootConstraintMath.GeometryEpsilon)
            {
                return false;
            }
            Quaternion soleRotation = Quaternion.LookRotation(
                forward.normalized,
                normal);
            finalRotation = (soleRotation *
                             Quaternion.Inverse(
                                 foot.SoleFrameLocalRotation)).normalized;
            effectiveRotation = Quaternion.Slerp(
                foot.AnkleRotation,
                finalRotation,
                rotationWeight).normalized;
            Quaternion rotationDelta =
                (effectiveRotation * Quaternion.Inverse(foot.AnkleRotation))
                .normalized;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(foot);
            effectiveSole = Vector3.LerpUnclamped(
                originalSole,
                finalSole,
                positionWeight);
            effectiveAnkle = effectiveSole -
                             rotationDelta *
                             (originalSole - foot.AnklePosition);
            finalAnkle = positionWeight >
                         CharacterFootConstraintMath.GeometryEpsilon
                ? foot.AnklePosition +
                  (effectiveAnkle - foot.AnklePosition) / positionWeight
                : foot.AnklePosition;
            return CharacterFootConstraintMath.Finite(finalAnkle) &&
                   CharacterFootConstraintMath.Finite(effectiveAnkle) &&
                   float.IsFinite(finalRotation.x) &&
                   float.IsFinite(finalRotation.y) &&
                   float.IsFinite(finalRotation.z) &&
                   float.IsFinite(finalRotation.w) &&
                   float.IsFinite(effectiveRotation.x) &&
                   float.IsFinite(effectiveRotation.y) &&
                   float.IsFinite(effectiveRotation.z) &&
                   float.IsFinite(effectiveRotation.w);
        }

        static bool TryResolveSupportReachReference(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            in CharacterFootSwingMotionResult swing,
            ulong supportEventIdentity,
            out Vector3 point)
        {
            if (supportEventIdentity != 0 &&
                context.Contact.HasContact &&
                context.Contact.EventIdentity == supportEventIdentity)
            {
                point = context.Contact.Anchor;
                return true;
            }
            if (supportEventIdentity != 0 &&
                context.LandingSnapshot.TryResolveVerifiedLanding(
                    supportEventIdentity,
                    out CharacterFootGroundPathLanding verifiedLanding))
            {
                point = verifiedLanding.Point;
                return true;
            }
            if (supportEventIdentity != 0 &&
                frame.HasContactLanding &&
                frame.ContactLanding.LandingEventIdentity ==
                supportEventIdentity)
            {
                point = frame.ContactLanding.Point;
                return true;
            }
            if (supportEventIdentity != 0 &&
                frame.PreparedPlantActive &&
                frame.PreparedPlantTarget.LandingEventIdentity ==
                supportEventIdentity)
            {
                point = frame.PreparedPlantTarget.Point;
                return true;
            }
            CharacterFootSwingPathReference swingPath =
                swing.SwingPathReference;
            if (supportEventIdentity != 0 &&
                swing.Accepted &&
                swingPath.IsAvailable &&
                swingPath.LandingEventIdentity == supportEventIdentity)
            {
                point = swingPath.LandingPoint;
                return true;
            }
            point = default;
            return false;
        }

        static float ResolveContactOwnership(
            in CharacterFootLifecycleContext context)
        {
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Landing:
                    return context.Interpolation.Progress;
                case CharacterFootConstraintState.Locked:
                    return 1f;
                case CharacterFootConstraintState.Releasing:
                    if (context.Interpolation.StartResidual <=
                        CharacterFootConstraintMath.GeometryEpsilon)
                    {
                        return 0f;
                    }
                    return Mathf.Clamp01(
                        context.Interpolation.Residual.magnitude /
                        context.Interpolation.StartResidual);
                default:
                    return 0f;
            }
        }

        static void RequireValid(in CharacterFootStateFrame frame)
        {
            if (frame.FrameSequence == 0 ||
                frame.CompletionIdentity == 0 ||
                frame.RigId.Length == 0 ||
                frame.RigRevision.Length == 0 ||
                frame.SourceLineage.Length == 0 ||
                frame.ProfileRevision.Length == 0 ||
                frame.WorldRevision == 0 ||
                ((byte)frame.OwnershipLossReason &
                 ~((byte)CharacterFootGoalOwnershipLossReason.Ungrounded |
                   (byte)CharacterFootGoalOwnershipLossReason
                       .SourceLineageInvalidated |
                   (byte)CharacterFootGoalOwnershipLossReason.OutputWeightZero)) != 0 ||
                !CharacterFootConstraintMath.Finite(frame.ComponentUp) ||
                frame.ComponentUp.sqrMagnitude <=
                    CharacterFootConstraintMath.GeometryEpsilon ||
                !CharacterFootConstraintMath.Finite(frame.AnimatedHip) ||
                !float.IsFinite(frame.LegLength) ||
                frame.LegLength <=
                frame.Settings.MinimumLandingLegCompressionReserve ||
                !float.IsFinite(frame.FootPlacementWeight) ||
                frame.FootPlacementWeight < 0f ||
                frame.FootPlacementWeight > 1f ||
                !float.IsFinite(frame.DeltaSeconds) ||
                frame.DeltaSeconds < 0f ||
                !float.IsFinite(frame.FormalSupport) ||
                frame.FormalSupport < 0f ||
                frame.FormalSupport > 1f ||
                frame.SwingMotion.Accepted !=
                frame.SwingMotion.SwingPathReference.IsAvailable ||
                frame.SwingMotion.Accepted &&
                frame.SwingMotion.SwingPathReference.LandingEventIdentity !=
                    frame.SwingMotion.LandingEventIdentity ||
                frame.SwingMotion.Accepted &&
                !float.IsFinite(
                    frame.SwingMotion.FormalTargetHeightAlongUp) ||
                frame.SwingMotion.Accepted &&
                (!frame.GroundPath.Accepted ||
                 frame.GroundPath.InputIdentity != frame.SwingMotion.GroundPathInputIdentity ||
                 frame.GroundPath.NextSwingLandingEventIdentity != frame.SwingMotion.LandingEventIdentity ||
                 frame.GroundPath.Page.Contacts.SurfaceCoverage.WorldRevision != frame.WorldRevision) ||
                frame.HasContactLanding &&
                frame.ContactLanding.LandingEventIdentity == 0 ||
                !frame.CurrentSupport.IsSpecified ||
                frame.CurrentSupport.FrameSequence != frame.FrameSequence ||
                frame.CurrentSupport.CompletionIdentity != frame.CompletionIdentity ||
                frame.CurrentSupport.Side != frame.Side ||
                frame.CurrentSupport.Available &&
                frame.CurrentSupport.Target.WorldRevision != frame.WorldRevision ||
                frame.PreparedPlantActive &&
                (frame.PreparedPlantTarget.LandingEventIdentity == 0 ||
                 !CharacterFootConstraintMath.Finite(
                     frame.PreparedPlantTarget.Point) ||
                 !CharacterFootConstraintMath.Finite(
                     frame.PreparedPlantTarget.Normal)))
            {
                throw new InvalidOperationException(
                    "Foot lifecycle frame is invalid.");
            }
        }
    }
}
