using System.Reflection;
using NUnit.Framework;
using ThirdPersonCamera;
using ThirdPersonPresentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonPresentation.Tests
{
    public sealed class PresentationTransformInterpolatorTests
    {
        [Test]
        public void UpdateVisualTargetFallsBackToSourceWithoutTickDriver()
        {
            GameObject sourceObject = new GameObject("Source");
            GameObject visualObject = new GameObject("Visual");
            GameObject interpolatorObject = new GameObject("Interpolator");
            try
            {
                sourceObject.transform.SetPositionAndRotation(new Vector3(3f, 0f, 1f), Quaternion.Euler(0f, 45f, 0f));
                PresentationTransformInterpolator interpolator = interpolatorObject.AddComponent<PresentationTransformInterpolator>();
                interpolator.Source = sourceObject.transform;
                interpolator.VisualTarget = visualObject.transform;

                interpolator.UpdateVisualTarget();

                Assert.AreEqual(sourceObject.transform.position, visualObject.transform.position);
                Assert.That(Quaternion.Angle(sourceObject.transform.rotation, visualObject.transform.rotation), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(interpolatorObject);
                Object.DestroyImmediate(visualObject);
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void CaptureSourceSampleDoesNotMoveSourceTransform()
        {
            GameObject sourceObject = new GameObject("Source");
            GameObject visualObject = new GameObject("Visual");
            GameObject interpolatorObject = new GameObject("Interpolator");
            GameObject driverObject = new GameObject("Driver");
            try
            {
                UnitySimulationTickDriver driver = driverObject.AddComponent<UnitySimulationTickDriver>();
                PresentationTransformInterpolator interpolator = interpolatorObject.AddComponent<PresentationTransformInterpolator>();
                interpolator.Source = sourceObject.transform;
                interpolator.VisualTarget = visualObject.transform;
                interpolator.TickDriver = driver;
                interpolator.SnapDistance = 20f;

                sourceObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                interpolator.CaptureSourceSample();
                sourceObject.transform.SetPositionAndRotation(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
                interpolator.CaptureSourceSample();

                PresentationPose resolved = interpolator.ResolvePose(0.5f);

                Assert.AreEqual(new Vector3(10f, 0f, 0f), sourceObject.transform.position);
                Assert.AreEqual(new Vector3(5f, 0f, 0f), resolved.Position);
                Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), resolved.Rotation), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(driverObject);
                Object.DestroyImmediate(interpolatorObject);
                Object.DestroyImmediate(visualObject);
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void UpdateVisualTargetWritesInterpolatedPose()
        {
            GameObject sourceObject = new GameObject("Source");
            GameObject visualObject = new GameObject("Visual");
            GameObject interpolatorObject = new GameObject("Interpolator");
            GameObject driverObject = new GameObject("Driver");
            try
            {
                UnitySimulationTickDriver driver = driverObject.AddComponent<UnitySimulationTickDriver>();
                PresentationTransformInterpolator interpolator = interpolatorObject.AddComponent<PresentationTransformInterpolator>();
                interpolator.Source = sourceObject.transform;
                interpolator.VisualTarget = visualObject.transform;
                interpolator.TickDriver = driver;
                interpolator.SnapDistance = 20f;

                sourceObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                interpolator.CaptureSourceSample();
                sourceObject.transform.SetPositionAndRotation(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
                interpolator.CaptureSourceSample();
                driver.ResetDriver(SimulationTick.Zero);
                driver.Advance(0.75f / 60f);

                interpolator.UpdateVisualTarget();

                Assert.AreEqual(new Vector3(7.5f, 0f, 0f), visualObject.transform.position);
                Assert.AreEqual(new Vector3(10f, 0f, 0f), sourceObject.transform.position);
                Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 67.5f, 0f), visualObject.transform.rotation), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), sourceObject.transform.rotation), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(driverObject);
                Object.DestroyImmediate(interpolatorObject);
                Object.DestroyImmediate(visualObject);
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void UpdateVisualTargetDoesNotWriteSourceWhenTargetIsSource()
        {
            GameObject sourceObject = new GameObject("Source");
            GameObject interpolatorObject = new GameObject("Interpolator");
            try
            {
                sourceObject.transform.position = new Vector3(2f, 0f, 0f);
                PresentationTransformInterpolator interpolator = interpolatorObject.AddComponent<PresentationTransformInterpolator>();
                interpolator.Source = sourceObject.transform;
                interpolator.VisualTarget = sourceObject.transform;

                interpolator.UpdateVisualTarget();

                Assert.AreEqual(new Vector3(2f, 0f, 0f), sourceObject.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(interpolatorObject);
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void CorrectionBlendsVisualTargetTowardCorrectedSource()
        {
            GameObject sourceObject = new GameObject("Source");
            GameObject visualObject = new GameObject("Visual");
            GameObject interpolatorObject = new GameObject("Interpolator");
            try
            {
                sourceObject.transform.SetPositionAndRotation(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
                visualObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                PresentationTransformInterpolator interpolator = interpolatorObject.AddComponent<PresentationTransformInterpolator>();
                interpolator.Source = sourceObject.transform;
                interpolator.VisualTarget = visualObject.transform;

                interpolator.BeginCorrection(PresentationPose.FromTransform(visualObject.transform), 1f);
                interpolator.AdvanceCorrection(0.5f);
                interpolator.UpdateVisualTarget();

                Assert.AreEqual(new Vector3(10f, 0f, 0f), sourceObject.transform.position);
                Assert.AreEqual(new Vector3(5f, 0f, 0f), visualObject.transform.position);
                Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), visualObject.transform.rotation), Is.LessThan(0.001f));

                interpolator.AdvanceCorrection(0.5f);
                interpolator.UpdateVisualTarget();

                Assert.False(interpolator.IsCorrectionActive);
                Assert.AreEqual(new Vector3(10f, 0f, 0f), visualObject.transform.position);
                Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), visualObject.transform.rotation), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(interpolatorObject);
                Object.DestroyImmediate(visualObject);
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void ControllerLateUpdateRefreshesTargetsWhenAutoTickIsDisabled()
        {
            GameObject rigObject = new GameObject("Rig");
            GameObject sourceObject = new GameObject("Source");
            GameObject visualObject = new GameObject("Visual");
            GameObject followObject = new GameObject("CameraFollowTarget");
            GameObject aimObject = new GameObject("CameraAimTarget");
            try
            {
                ThirdPersonCameraController controller = rigObject.AddComponent<ThirdPersonCameraController>();
                PresentationTransformInterpolator interpolator = rigObject.AddComponent<PresentationTransformInterpolator>();
                interpolator.Source = sourceObject.transform;
                interpolator.VisualTarget = visualObject.transform;
                controller.FollowAnchorSource = visualObject.transform;
                controller.CameraFollowTarget = followObject.transform;
                controller.CameraAimTarget = aimObject.transform;
                controller.AutoTick = false;

                sourceObject.transform.position = Vector3.zero;
                interpolator.UpdateVisualTarget();
                InvokeLateUpdate(controller);
                sourceObject.transform.position = new Vector3(4f, 0f, 0f);
                interpolator.UpdateVisualTarget();
                InvokeLateUpdate(controller);

                Assert.AreEqual(new Vector3(4f, 0f, 0f), followObject.transform.position);
                Assert.AreEqual(new Vector3(4f, 0f, 0f), aimObject.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(aimObject);
                Object.DestroyImmediate(followObject);
                Object.DestroyImmediate(visualObject);
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(rigObject);
            }
        }

        static void InvokeLateUpdate(ThirdPersonCameraController controller)
        {
            typeof(ThirdPersonCameraController)
                .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
        }
    }
}
