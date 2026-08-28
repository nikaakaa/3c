using NUnit.Framework;
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
                0.1f,
                30f,
                8f,
                0.05f,
                1f);
            Vector3 raw = new Vector3(1f, 2f, 3f);
            var key = new CharacterFootLandingObservationKey(
                CharacterFootSide.Right,
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
                0.1f,
                30f,
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
                false,
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
