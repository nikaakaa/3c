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
    }
}
