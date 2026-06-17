using System.Collections.Generic;
using NUnit.Framework;
using ThirdPersonScene;
using UnityEngine;

namespace ThirdPersonRendering.Tests
{
    public sealed class InteractiveTallGrassTests
    {
        [Test]
        public void DefaultSettingsAreValidForSmallPreviewPatch()
        {
            InteractiveTallGrassSettings settings = InteractiveTallGrassSettings.Default;

            Assert.Greater(settings.AreaSize.x, 0f);
            Assert.Greater(settings.AreaSize.y, 0f);
            Assert.Greater(settings.BladeCount, 0);
            Assert.LessOrEqual(settings.BladeCount, InteractiveTallGrassSettings.MaxBladeCount);
            Assert.Greater(settings.MaxHeight, settings.MinHeight);
            Assert.Greater(settings.MaxWidth, settings.MinWidth);
            Assert.Greater(settings.InteractionRadius, 0f);
        }

        [Test]
        public void InvalidSettingsClampToSafeRanges()
        {
            InteractiveTallGrassSettings settings = new InteractiveTallGrassSettings(
                new Vector2(999f, -5f),
                9999,
                12,
                9f,
                -3f,
                4f,
                -1f,
                new Color(2f, -1f, 0.5f, 3f),
                new Color(-1f, 2f, 0.25f, 2f),
                9f,
                9f,
                99f,
                Vector2.zero,
                99f,
                99f);

            Assert.AreEqual(InteractiveTallGrassSettings.MaxAreaSize, settings.AreaSize.x);
            Assert.AreEqual(InteractiveTallGrassSettings.MinAreaSize, settings.AreaSize.y);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxBladeCount, settings.BladeCount);
            Assert.AreEqual(InteractiveTallGrassSettings.MinBladeHeight, settings.MinHeight);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxBladeHeight, settings.MaxHeight);
            Assert.AreEqual(InteractiveTallGrassSettings.MinBladeWidth, settings.MinWidth);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxBladeWidth, settings.MaxWidth);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxToonStrength, settings.ToonStrength);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxWindStrength, settings.WindStrength);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxWindFrequency, settings.WindFrequency);
            Assert.AreEqual(Vector2.right, settings.WindDirection);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxInteractionRadius, settings.InteractionRadius);
            Assert.AreEqual(InteractiveTallGrassSettings.MaxBendStrength, settings.BendStrength);
            Assert.AreEqual(1f, settings.BaseColor.r);
            Assert.AreEqual(0f, settings.BaseColor.g);
            Assert.AreEqual(1f, settings.BaseColor.a);
        }

        [Test]
        public void SameRandomSeedGeneratesStableBladeDistribution()
        {
            InteractiveTallGrassSettings settings = InteractiveTallGrassSettings.Default;
            IReadOnlyList<InteractiveTallGrassBlade> first = InteractiveTallGrassGenerator.GenerateBlades(settings);
            IReadOnlyList<InteractiveTallGrassBlade> second = InteractiveTallGrassGenerator.GenerateBlades(settings);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Position, second[i].Position);
                Assert.AreEqual(first[i].Height, second[i].Height);
                Assert.AreEqual(first[i].Width, second[i].Width);
                Assert.AreEqual(first[i].YawDegrees, second[i].YawDegrees);
            }
        }

        [Test]
        public void DifferentRandomSeedChangesBladeDistribution()
        {
            InteractiveTallGrassSettings firstSettings = InteractiveTallGrassSettings.Default;
            InteractiveTallGrassSettings secondSettings = new InteractiveTallGrassSettings(
                firstSettings.AreaSize,
                firstSettings.BladeCount,
                firstSettings.RandomSeed + 1,
                firstSettings.MinHeight,
                firstSettings.MaxHeight,
                firstSettings.MinWidth,
                firstSettings.MaxWidth,
                firstSettings.BaseColor,
                firstSettings.TopColor,
                firstSettings.ToonStrength,
                firstSettings.WindStrength,
                firstSettings.WindFrequency,
                firstSettings.WindDirection,
                firstSettings.InteractionRadius,
                firstSettings.BendStrength);

            IReadOnlyList<InteractiveTallGrassBlade> first = InteractiveTallGrassGenerator.GenerateBlades(firstSettings);
            IReadOnlyList<InteractiveTallGrassBlade> second = InteractiveTallGrassGenerator.GenerateBlades(secondSettings);

            Assert.AreNotEqual(first[0].Position, second[0].Position);
        }

        [Test]
        public void MaxBladeCountLimitControlsGeneratedMeshSize()
        {
            InteractiveTallGrassSettings settings = new InteractiveTallGrassSettings(
                new Vector2(5f, 5f),
                9999,
                42,
                1f,
                2f,
                0.1f,
                0.2f,
                Color.green,
                Color.white,
                0.5f,
                0.5f,
                2f,
                Vector2.right,
                1f,
                1f);

            IReadOnlyList<InteractiveTallGrassBlade> blades = InteractiveTallGrassGenerator.GenerateBlades(settings);
            Mesh mesh = InteractiveTallGrassGenerator.BuildMesh(blades);
            try
            {
                Assert.AreEqual(InteractiveTallGrassSettings.MaxBladeCount, blades.Count);
                Assert.AreEqual(InteractiveTallGrassSettings.MaxBladeCount * 8, mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void InteractorWithoutSourceUploadsDisabledInteraction()
        {
            GameObject grass = new GameObject("GrassPreview");
            try
            {
                MeshRenderer renderer = grass.AddComponent<MeshRenderer>();
                InteractiveTallGrassInteractor interactor = grass.AddComponent<InteractiveTallGrassInteractor>();
                interactor.TargetRenderer = renderer;

                interactor.Apply();

                Assert.AreEqual(0f, interactor.LastUploadedPosition.w);
            }
            finally
            {
                Object.DestroyImmediate(grass);
            }
        }

        [Test]
        public void InteractorUploadsSourcePosition()
        {
            GameObject grass = new GameObject("GrassPreview");
            GameObject source = new GameObject("Interactor");
            try
            {
                source.transform.position = new Vector3(1.25f, 0.5f, -2.5f);
                MeshRenderer renderer = grass.AddComponent<MeshRenderer>();
                InteractiveTallGrassInteractor interactor = grass.AddComponent<InteractiveTallGrassInteractor>();
                interactor.TargetRenderer = renderer;
                interactor.InteractionSource = source.transform;

                interactor.Apply();

                Assert.AreEqual(1.25f, interactor.LastUploadedPosition.x);
                Assert.AreEqual(0.5f, interactor.LastUploadedPosition.y);
                Assert.AreEqual(-2.5f, interactor.LastUploadedPosition.z);
                Assert.AreEqual(1f, interactor.LastUploadedPosition.w);
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(grass);
            }
        }

    }
}
