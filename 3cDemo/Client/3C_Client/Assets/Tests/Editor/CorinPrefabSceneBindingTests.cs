using System.Collections.Generic;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class CorinPrefabSceneBindingTests
    {
        const string HumanoidPrefabPath = "Assets/Prefabs/Character/可琳_Humanoid.prefab";

        [Test]
        public void CorinRuntimeControllerAdvancesLocomotionOnlyFrame()
        {
            GameObject instance = InstantiatePrefab(HumanoidPrefabPath);
            try
            {
                CharacterFrameRuntimeController runtime = instance.GetComponentInChildren<CharacterFrameRuntimeController>(true);
                Assert.NotNull(runtime);

                BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero, true);
                Assert.True(runtime.Tick(in input), runtime.LastFramePipelineResult.FailureReason);

                CharacterFrameResult result = runtime.LastFramePipelineResult;
                Assert.True(result.Success, result.FailureReason);
                Assert.AreEqual(CharacterFramePipelineStep.Completed, result.CompletedStep);
                Assert.True(result.Output.HasSubmission);
                Assert.AreEqual(CharacterBodyDomain.Locomotion, result.FramePlan.BaseLayerOwner);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CorinRuntimeControllerAdvancesDodgeAcceptedFrame()
        {
            GameObject instance = InstantiatePrefab(HumanoidPrefabPath);
            try
            {
                CharacterFrameRuntimeController runtime = instance.GetComponentInChildren<CharacterFrameRuntimeController>(true);
                Assert.NotNull(runtime);

                PredictionInputFrame frame = new PredictionInputFrame(
                    new SimulationTick(4),
                    Vector2.up,
                    Vector2.zero,
                    true,
                    new PredictionButtonFrame(true, true, false),
                    PredictionButtonFrame.None,
                    PredictionButtonFrame.None,
                    PredictionButtonFrame.None);
                CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(in frame, 0.02f);
                Assert.True(runtime.Tick(in input), runtime.LastFramePipelineResult.FailureReason);

                CharacterFrameResult result = runtime.LastFramePipelineResult;
                Assert.True(result.Success, result.FailureReason);
                Assert.AreEqual(CharacterBodyDomain.FullBodyAction, result.FramePlan.BaseLayerOwner);
                Assert.False(result.Output.Movement.ExecuteBasicMovement);
                Assert.True(result.Output.Movement.ExecuteActionMovement);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static GameObject InstantiatePrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.NotNull(prefab, assetPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Assert.NotNull(instance, assetPath);
            return instance;
        }
    }
}
