using NUnit.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootLandingPredictorTests
    {
        [Test]
        public void ProjectionConsumesFutureTranslationAndCurrentBodyRotationOnce()
        {
            var body = new CharacterFutureBodyTranslationSample(
                0.25f,
                2f,
                0f,
                0f,
                0f,
                0f,
                0f);

            Vector3 result = CharacterFootLandingPredictor.ProjectRawLanding(
                Vector3.zero,
                Quaternion.Euler(0f, 90f, 0f),
                in body,
                Vector3.forward);

            Assert.That(result.x, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(result.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void QueryIsAnchoredAboveRawLandingAndPointsDown()
        {
            var settings = new CharacterFootLandingPredictionSettings(
                1 << 12,
                16,
                0.08f,
                0.35f,
                0.75f,
                55f,
                2f,
                0.001f,
                60f,
                8f,
                0.05f,
                1f);
            Vector3 raw = new Vector3(1f, 2f, 3f);
            var key = new CharacterFootLandingObservationKey(
                CharacterFootSide.Right,
                CharacterFootPlacementQueryPurpose.FutureLanding,
                1UL,
                1UL,
                0,
                raw,
                Vector3.up,
                "test-profile",
                1UL);

            CharacterFootPlacementQueryRequest query =
                CharacterFootLandingPredictor.BuildQuery(
                    in key,
                    in settings);

            Assert.That(query.FootIndex, Is.EqualTo(1));
            Assert.That(query.Origin, Is.EqualTo(raw + Vector3.up * 0.35f));
            Assert.That(query.Direction, Is.EqualTo(Vector3.down));
            Assert.That(query.Radius, Is.EqualTo(0.08f).Within(0.0001f));
            Assert.That(query.MaximumDistance, Is.EqualTo(1.1f).Within(0.0001f));
        }

        [Test]
        public void QueryMissDoesNotCreateSupport()
        {
            var settings = new CharacterFootLandingPredictionSettings(
                1 << 12,
                16,
                0.08f,
                0.35f,
                0.75f,
                55f,
                2f,
                0.001f,
                60f,
                8f,
                0.05f,
                1f);
            var world = new MissingWorldQuery();
            var pool = new CharacterFootLandingObservationPagePool();

            CharacterFootLandingObservationResult observation =
                CharacterFootLandingPredictor.ResolveObservation(
                CharacterFootSide.Left,
                1UL,
                1UL,
                0,
                Vector3.zero,
                Vector3.up,
                CharacterFootLandingObservationRefreshMode.Thresholded,
                "test-profile",
                in settings,
                world,
                pool,
                null,
                out CharacterFootLandingObservationPage pending);
            CharacterFootLandingQueryResult result = pending.Result;

            Assert.That(result.Accepted, Is.False);
            Assert.That(pending.Query.Purpose, Is.EqualTo(CharacterFootPlacementQueryPurpose.FutureLanding));
            Assert.That(result.Support.SurfaceIdentity, Is.EqualTo(0));
            Assert.That(result.RejectReason, Is.EqualTo(CharacterFootLandingQueryRejectReason.NoHit));
            Assert.That(
                result.SelectionDiagnostics.State,
                Is.EqualTo(
                    CharacterFootLandingQueryCandidateSelectionState.NotExecuted));
            Assert.That(observation.QueryExecutedThisFrame, Is.True);
        }

        [Test]
        public void SwingFormalTargetBelowOriginalSoleMustPreserveSignedCorrection()
        {
            AnimationFootMotionRuntimeSample step = CreateSwingStep();
            CharacterFootGroundPathResult path = CreateAcceptedGroundPath(
                step.LandingEventIdentity,
                0f);
            CharacterFootPlacementAnimatedFootPose animated =
                CreateAnimatedFoot(0.1f);

            CharacterFootSwingMotionResult result =
                CharacterFootSwingMotionBuilder.BuildForSwing(
                    animated,
                    in step,
                    step.LandingEventIdentity,
                    1f,
                    Vector3.up,
                    in path,
                    0f,
                    0f);

            Assert.That(result.Accepted, Is.True);
            Assert.That(
                result.FormalTargetHeightAlongUp -
                Vector3.Dot(result.OriginalSole, Vector3.up),
                Is.EqualTo(-0.1f).Within(0.0001f));
            Assert.That(result.VerticalCorrection, Is.EqualTo(-0.1f).Within(0.0001f));
        }

        [Test]
        public void PreparedPlantActiveMustNotBypassGroundEnvelopeMinimum()
        {
            AnimationFootMotionRuntimeSample step = CreateSwingStep();
            CharacterFootGroundPathResult path = CreateAcceptedGroundPath(
                step.LandingEventIdentity,
                0.2f);
            CharacterFootPlacementAnimatedFootPose animated =
                CreateAnimatedFoot(0f);
            CharacterFootSwingMotionResult swing =
                CharacterFootSwingMotionBuilder.BuildForSwing(
                    animated,
                    in step,
                    step.LandingEventIdentity,
                    1f,
                    Vector3.up,
                    in path,
                    0f,
                    0f);
            CharacterFootGroundPathLanding preparedTarget =
                new CharacterFootGroundPathLanding(
                    step.LandingEventIdentity,
                    1UL,
                    "prepared",
                    20,
                    new Vector3(0f, 0.05f, 0f),
                    Vector3.up);
            CharacterFootStateFrame frame = CreateStateFrame(
                animated,
                in swing,
                in preparedTarget);
            CharacterFootLifecycleContext context = default;
            context.Discrete.State = CharacterFootConstraintState.Swing;
            CharacterFootHardConstraintResult result =
                CharacterFootHardConstraintResolver.Resolve(
                    in context,
                    in frame,
                    Vector3.zero);
            Vector3 envelopeMinimum =
                CharacterFootConstraintMath.ResolvePointMinimumCorrection(
                    animated,
                    swing.EnvelopeSample,
                    Vector3.up);

            Assert.That(result.Available, Is.True);
            Assert.That(result.MinimumCorrection.y, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(result.OutputCorrection.y, Is.GreaterThanOrEqualTo(envelopeMinimum.y));
        }

        [Test]
        public void SwingOutputStepSeparatesSourceCorrectionAndCombinedMotion()
        {
            AnimationFootMotionRuntimeSample step = CreateSwingStep();
            CharacterFootSwingMotionResult sourceOnlyBefore =
                BuildSwingForStep(step, 0f, 0.2f);
            CharacterFootSwingMotionResult sourceOnlyAfter =
                BuildSwingForStep(step, 0.1f, 0.3f);
            CharacterFootSwingMotionResult correctionOnlyBefore =
                BuildSwingForStep(step, 0f, 0.2f);
            CharacterFootSwingMotionResult correctionOnlyAfter =
                BuildSwingForStep(step, 0f, 0.25f);
            CharacterFootSwingMotionResult combinedBefore =
                BuildSwingForStep(step, 0f, 0.2f);
            CharacterFootSwingMotionResult combinedAfter =
                BuildSwingForStep(step, 0.1f, 0.35f);

            Assert.That(
                Vector3.Distance(
                    sourceOnlyAfter.OriginalSole,
                    sourceOnlyBefore.OriginalSole),
                Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(
                Vector3.Distance(
                    sourceOnlyAfter.CorrectedSole - sourceOnlyAfter.OriginalSole,
                    sourceOnlyBefore.CorrectedSole - sourceOnlyBefore.OriginalSole),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                Vector3.Distance(
                    sourceOnlyAfter.CorrectedSole,
                    sourceOnlyBefore.CorrectedSole),
                Is.EqualTo(0.1f).Within(0.0001f));

            Assert.That(
                Vector3.Distance(
                    correctionOnlyAfter.OriginalSole,
                    correctionOnlyBefore.OriginalSole),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                Vector3.Distance(
                    correctionOnlyAfter.CorrectedSole - correctionOnlyAfter.OriginalSole,
                    correctionOnlyBefore.CorrectedSole - correctionOnlyBefore.OriginalSole),
                Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(
                Vector3.Distance(
                    correctionOnlyAfter.CorrectedSole,
                    correctionOnlyBefore.CorrectedSole),
                Is.EqualTo(0.05f).Within(0.0001f));

            Assert.That(
                Vector3.Distance(
                    combinedAfter.OriginalSole,
                    combinedBefore.OriginalSole),
                Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(
                Vector3.Distance(
                    combinedAfter.CorrectedSole - combinedAfter.OriginalSole,
                    combinedBefore.CorrectedSole - combinedBefore.OriginalSole),
                Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(
                Vector3.Distance(
                    combinedAfter.CorrectedSole,
                    combinedBefore.CorrectedSole),
                Is.EqualTo(0.15f).Within(0.0001f));
        }

        [Test]
        public void PlantTargetCorrectionPreservesSignedGapOnBothSides()
        {
            CharacterFootPlacementAnimatedFootPose animated = CreateAnimatedFoot(0f);
            Vector3 aboveTarget = new Vector3(0f, 0.025f, 0f);
            Vector3 belowTarget = new Vector3(0f, -0.025f, 0f);

            Vector3 aboveCorrection =
                CharacterFootConstraintMath.ResolvePointMinimumCorrection(
                    animated,
                    aboveTarget,
                    Vector3.up);
            Vector3 belowCorrection =
                CharacterFootConstraintMath.ResolvePointMinimumCorrection(
                    animated,
                    belowTarget,
                    Vector3.up);

            Assert.That(aboveCorrection.y, Is.EqualTo(0.025f).Within(0.000001f));
            Assert.That(belowCorrection.y, Is.EqualTo(-0.025f).Within(0.000001f));
        }

        [Test]
        public void PredictionInputDistanceExceededRequeriesLateSameEventLanding()
        {
            var settings = new CharacterFootLandingPredictionSettings(
                1 << 12,
                16,
                0.08f,
                0.35f,
                0.75f,
                55f,
                2f,
                0.001f,
                60f,
                8f,
                0.05f,
                1f);
            var world = new RequeryingWorldQuery();
            var pool = new CharacterFootLandingObservationPagePool();

            CharacterFootLandingObservationResult first =
                CharacterFootLandingPredictor.ResolveObservation(
                    CharacterFootSide.Right,
                    7UL,
                    9UL,
                    0,
                    Vector3.zero,
                    Vector3.up,
                    CharacterFootLandingObservationRefreshMode.Thresholded,
                    "test-profile",
                    in settings,
                    world,
                    pool,
                    null,
                    out CharacterFootLandingObservationPage firstPage);
            Assert.That(first.CacheState, Is.EqualTo(CharacterFootLandingObservationCacheState.Queried));
            Assert.That(first.Page.Result.Support.SurfaceIdentity, Is.EqualTo(101));

            Vector3 lateRawLanding = new Vector3(
                settings.PredictionInputAccumulationDistance * 2f,
                0f,
                0f);
            CharacterFootLandingObservationResult second =
                CharacterFootLandingPredictor.ResolveObservation(
                    CharacterFootSide.Right,
                    7UL,
                    9UL,
                    0,
                    lateRawLanding,
                    Vector3.up,
                    CharacterFootLandingObservationRefreshMode.Thresholded,
                    "test-profile",
                    in settings,
                    world,
                    pool,
                    firstPage,
                    out CharacterFootLandingObservationPage secondPage);

            Assert.That(second.QueryReason &
                        CharacterFootLandingObservationQueryReason
                            .PredictionInputDistanceExceeded,
                Is.Not.EqualTo(CharacterFootLandingObservationQueryReason.None));
            Assert.That(second.QueryExecutedThisFrame, Is.True);
            Assert.That(second.CacheState, Is.EqualTo(CharacterFootLandingObservationCacheState.Queried));
            Assert.That(world.QueryCount, Is.EqualTo(2));
            Assert.That(secondPage.Result.Support.SurfaceIdentity, Is.EqualTo(202));
            Assert.That(secondPage.Key.CanonicalRawLanding.x,
                Is.EqualTo(lateRawLanding.x).Within(0.000001f));
        }

        [Test]
        public void ReleasingInterpolationAndLegPoseDiagnosticsRemainAvailable()
        {
            CharacterFullBodyIkLegPoseDiagnostics pose =
                new CharacterFullBodyIkLegPoseDiagnostics(
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, 0.5f, 0f),
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, -0.1f, 0f),
                    new Vector3(0.1f, 1f, 0f),
                    new Vector3(0.1f, 0.5f, 0f),
                    new Vector3(0.1f, 0f, 0f),
                    Vector3.back,
                    35f,
                    40f,
                    0.4f,
                    0.5f,
                    0.6f,
                    0.1f,
                    0.2f,
                    0.3f,
                    0.9f,
                    0.8f,
                    0.7f,
                    true,
                    true,
                    true,
                    CharacterFullBodyIkBendDirectionSource.Animated);
            Assert.That(pose.IsAvailable, Is.True);
            Assert.That(pose.OriginalHip, Is.EqualTo(new Vector3(0f, 1f, 0f)));
            Assert.That(pose.OriginalKnee, Is.EqualTo(new Vector3(0f, 0.5f, 0f)));
            Assert.That(pose.OriginalAnkle, Is.EqualTo(Vector3.zero));
            Assert.That(pose.TargetAnkle, Is.EqualTo(new Vector3(0f, -0.1f, 0f)));
            Assert.That(pose.SolvedHip, Is.EqualTo(new Vector3(0.1f, 1f, 0f)));
            Assert.That(pose.SolvedKnee, Is.EqualTo(new Vector3(0.1f, 0.5f, 0f)));
            Assert.That(pose.SolvedAnkle, Is.EqualTo(new Vector3(0.1f, 0f, 0f)));
            Assert.That(pose.SolvedExtensionRatio, Is.EqualTo(0.6f).Within(0.000001f));
            Assert.That(pose.SolvedCompressionReserve, Is.EqualTo(0.3f).Within(0.000001f));

            AnimationFootMotionRuntimeSample step = CreateSwingStep();
            CharacterFootGroundPathResult path = CreateAcceptedGroundPath(
                step.LandingEventIdentity,
                0.1f);
            CharacterFootPlacementAnimatedFootPose animated = CreateAnimatedFoot(0.1f);
            CharacterFootSwingMotionResult swing =
                CharacterFootSwingMotionBuilder.BuildForSwing(
                    animated,
                    in step,
                    step.LandingEventIdentity,
                    1f,
                    Vector3.up,
                    in path,
                    0f,
                    0f);
            CharacterFootGroundPathLanding releaseLanding =
                new CharacterFootGroundPathLanding(
                    step.LandingEventIdentity,
                    1UL,
                    "release",
                    20,
                    new Vector3(0f, 0.1f, 0f),
                    Vector3.up);
            CharacterFootStateFrame frame = CreateStateFrame(
                animated,
                in swing,
                in releaseLanding);
            CharacterFootSupportTarget support = CreateSupportTarget(
                CharacterFootSupportTargetKind.Releasing,
                CharacterFootSupportPositionSource.ReleasingSwing,
                CharacterFootSupportNormalSource.RetainedContactAnchor);
            CharacterFootStateTarget target = new CharacterFootStateTarget(
                Vector3.zero,
                Vector3.zero,
                CharacterFootInterpolationPolicy.ReleaseResidual,
                false,
                0UL,
                false,
                default,
                CharacterFootPlantTargetKind.None,
                CharacterFootLockResponse.None,
                false,
                true,
                in support,
                true,
                false,
                false,
                false,
                0f,
                default);
            CharacterFootInterpolationState state = default;
            CharacterFootInterpolationResult result =
                CharacterFootInterpolationRuntime.Evaluate(
                    ref state,
                    in target,
                    in frame);

            Assert.That(result.CorrectionResponseFact.Evaluated, Is.True);
            Assert.That(result.PlantFact.Evaluated, Is.False);
            Assert.That(result.SupportTarget.IsValid, Is.True);
        }

        static CharacterFootSwingMotionResult BuildSwingForStep(
            AnimationFootMotionRuntimeSample step,
            float originalSoleHeight,
            float envelopeHeight)
        {
            CharacterFootGroundPathResult path = CreateAcceptedGroundPath(
                step.LandingEventIdentity,
                envelopeHeight);
            CharacterFootPlacementAnimatedFootPose animated =
                CreateAnimatedFoot(originalSoleHeight);
            CharacterFootSwingMotionResult result =
                CharacterFootSwingMotionBuilder.BuildForSwing(
                    animated,
                    in step,
                    step.LandingEventIdentity,
                    1f,
                    Vector3.up,
                    in path,
                    0f,
                    0f);
            Assert.That(result.Accepted, Is.True);
            return result;
        }

        static CharacterFootPlacementAnimatedFootPose CreateAnimatedFoot(float soleHeight)
        {
            Vector3 sole = new Vector3(0f, soleHeight, 0f);
            return new CharacterFootPlacementAnimatedFootPose(
                new Vector3(0f, 0.5f, 0f),
                new Vector3(0f, 0.25f, 0f),
                sole,
                Quaternion.identity,
                sole,
                Quaternion.identity,
                sole,
                Vector3.forward,
                Vector3.up,
                Quaternion.identity,
                Quaternion.identity);
        }

        static AnimationFootMotionRuntimeSample CreateSwingStep()
        {
            AnimationFootMotionEventOccurrence nextLanding =
                new AnimationFootMotionEventOccurrence(
                    1,
                    0,
                    0.5f,
                    1f,
                    Vector3.zero);
            AnimationFootMotionEventFrame events =
                new AnimationFootMotionEventFrame(
                    default,
                    nextLanding,
                    AnimationFootMotionEventPhase.Swing,
                    0.1f,
                    0.5f,
                    0f).Bind(1UL, 1UL, CharacterFootSide.Left);
            return new AnimationFootMotionRuntimeSample(
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                AnimationFootStepObservationLockMode.Unlocked,
                0f,
                0f,
                in events);
        }

        static CharacterFootGroundPathResult CreateAcceptedGroundPath(
            ulong nextLandingEventIdentity,
            float envelopeHeight)
        {
            CharacterFootGroundPathLanding lastLanding =
                new CharacterFootGroundPathLanding(
                    1UL,
                    1UL,
                    "last",
                    10,
                    new Vector3(0f, envelopeHeight, 0f),
                    Vector3.up);
            CharacterFootGroundPathLanding nextLanding =
                new CharacterFootGroundPathLanding(
                    nextLandingEventIdentity,
                    1UL,
                    "next",
                    11,
                    new Vector3(1f, envelopeHeight, 0f),
                    Vector3.up);
            CharacterFootGroundPathInputKey key =
                CharacterFootGroundPathInputBuilder.BuildKey(
                    CharacterFootSide.Left,
                    in lastLanding,
                    in nextLanding,
                    1UL,
                    Vector3.up,
                    "test-profile");
            CharacterFootGroundDetectionSettings settings =
                new CharacterFootGroundDetectionSettings(
                    1,
                    4,
                    4,
                    0.1f,
                    1f,
                    0.45f,
                    0.85f,
                    0.3f);
            Assert.That(
                CharacterFootGroundPathInputBuilder.TryBuild(
                    in key,
                    lastLanding.Point,
                    nextLanding.Point,
                    lastLanding.Normal,
                    nextLanding.Normal,
                    lastLanding.SurfaceIdentity,
                    nextLanding.SurfaceIdentity,
                    Vector3.up,
                    in settings,
                    out CharacterFootGroundPathInput input),
                Is.True);
            CharacterFootGroundPathPage page =
                new CharacterFootGroundPathPage(4);
            Assert.That(
                page.Contacts.SurfaceCoverage.Begin(input.Query, 1UL),
                Is.True);
            Assert.That(
                page.Contacts.SurfaceCoverage.TryAdd(
                    lastLanding.SurfaceIdentity,
                    1,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f)),
                Is.True);
            page.Contacts.SurfaceCoverage.Complete();
            CharacterFootGroundContact contact =
                new CharacterFootGroundContact(
                    0,
                    lastLanding.SurfaceIdentity,
                    1UL,
                    lastLanding.Point,
                    Vector3.up,
                    0f);
            Assert.That(page.Contacts.TryAdd(in contact), Is.True);
            Assert.That(page.Envelope.TryPush(lastLanding.Point), Is.True);
            Assert.That(page.Envelope.TryPush(nextLanding.Point), Is.True);
            page.SetAccepted(1, in input);
            return new CharacterFootGroundPathResult(page, true);
        }

        static CharacterFootStateFrame CreateStateFrame(
            CharacterFootPlacementAnimatedFootPose animated,
            in CharacterFootSwingMotionResult swing,
            in CharacterFootGroundPathLanding preparedTarget) =>
            new CharacterFootStateFrame(
                1UL,
                1UL,
                default,
                default,
                CharacterFootSide.Left,
                animated,
                new Vector3(0f, 0.5f, 0f),
                1f,
                in swing,
                false,
                default,
                true,
                in preparedTarget,
                default,
                default,
                0f,
                0UL,
                CharacterFootGoalOwnershipLossReason.None,
                1f,
                Vector3.up,
                1f / 60f,
                default,
                default,
                1UL,
                default);

        static CharacterFootSupportTarget CreateSupportTarget(
            CharacterFootSupportTargetKind kind,
            CharacterFootSupportPositionSource positionSource,
            CharacterFootSupportNormalSource normalSource) =>
            new CharacterFootSupportTarget(
                1UL,
                1UL,
                CharacterFootSide.Left,
                Vector3.zero,
                Vector3.up,
                20,
                1UL,
                kind,
                positionSource,
                1UL,
                1UL,
                1UL,
                0UL,
                normalSource,
                1UL,
                1UL,
                1UL);

        sealed class MissingWorldQuery : ICharacterFootLandingWorldQuery
        {
            public ulong WorldRevision => 1UL;

            public CharacterFootLandingQueryResult Query(
                in CharacterFootPlacementQueryRequest request) =>
                new CharacterFootLandingQueryResult(
                    CharacterFootLandingQueryRejectReason.NoHit,
                    default,
                    default);
        }

        sealed class RequeryingWorldQuery : ICharacterFootLandingWorldQuery
        {
            public ulong WorldRevision => 1UL;

            public int QueryCount { get; private set; }

            public CharacterFootLandingQueryResult Query(
                in CharacterFootPlacementQueryRequest request)
            {
                QueryCount++;
                int surface = QueryCount == 1 ? 101 : 202;
                Vector3 point = new Vector3(request.Origin.x, 0f, request.Origin.z);
                var support = new CharacterFootLandingSupport(
                    surface,
                    point,
                    Vector3.up,
                    0.1f);
                var selected = new CharacterFootLandingQueryCandidateDiagnostics(
                    surface,
                    point,
                    0.1f);
                var selection = new CharacterFootLandingQuerySelectionDiagnostics(
                    CharacterFootLandingQueryCandidateSelectionState.Selected,
                    1,
                    selected);
                return new CharacterFootLandingQueryResult(
                    CharacterFootLandingQueryRejectReason.None,
                    support,
                    selection);
            }
        }
    }
}
