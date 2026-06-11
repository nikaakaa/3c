using NUnit.Framework;
using ThirdPersonPresentation;
using UnityEngine;

namespace ThirdPersonPresentation.Tests
{
    public sealed class PresentationTransformResolverTests
    {
        [Test]
        public void ResolveInterpolatesPositionBetweenPreviousAndCurrentPose()
        {
            PresentationPose result = PresentationTransformResolver.Resolve(
                new PresentationPose(Vector3.zero, Quaternion.identity),
                new PresentationPose(new Vector3(10f, 0f, 0f), Quaternion.identity),
                0.25f,
                true,
                20f);

            Assert.AreEqual(new Vector3(2.5f, 0f, 0f), result.Position);
        }

        [Test]
        public void ResolveInterpolatesRotationBetweenPreviousAndCurrentPose()
        {
            Quaternion currentRotation = Quaternion.Euler(0f, 90f, 0f);

            PresentationPose result = PresentationTransformResolver.Resolve(
                new PresentationPose(Vector3.zero, Quaternion.identity),
                new PresentationPose(Vector3.zero, currentRotation),
                0.5f,
                true,
                20f);

            Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), result.Rotation), Is.LessThan(0.001f));
        }

        [Test]
        public void ResolveClampsAlphaIntoRange()
        {
            PresentationPose previous = new PresentationPose(Vector3.zero, Quaternion.identity);
            PresentationPose current = new PresentationPose(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

            PresentationPose negative = PresentationTransformResolver.Resolve(previous, current, -2f, true, 20f);
            PresentationPose above = PresentationTransformResolver.Resolve(previous, current, 2f, true, 20f);

            Assert.AreEqual(previous.Position, negative.Position);
            Assert.AreEqual(current.Position, above.Position);
            Assert.That(Quaternion.Angle(previous.Rotation, negative.Rotation), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(current.Rotation, above.Rotation), Is.LessThan(0.001f));
        }

        [Test]
        public void ResolveSnapsToCurrentPoseWithoutPreviousSample()
        {
            PresentationPose current = new PresentationPose(new Vector3(4f, 1f, -2f), Quaternion.Euler(0f, 30f, 0f));

            PresentationPose result = PresentationTransformResolver.Resolve(default, current, 0.5f, false, 20f);

            Assert.AreEqual(current.Position, result.Position);
            Assert.That(Quaternion.Angle(current.Rotation, result.Rotation), Is.LessThan(0.001f));
        }

        [Test]
        public void ResolveSnapsToCurrentPoseAfterTeleport()
        {
            PresentationPose current = new PresentationPose(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

            PresentationPose result = PresentationTransformResolver.Resolve(
                new PresentationPose(Vector3.zero, Quaternion.identity),
                current,
                0.5f,
                true,
                3f);

            Assert.AreEqual(current.Position, result.Position);
            Assert.That(Quaternion.Angle(current.Rotation, result.Rotation), Is.LessThan(0.001f));
        }
    }
}
