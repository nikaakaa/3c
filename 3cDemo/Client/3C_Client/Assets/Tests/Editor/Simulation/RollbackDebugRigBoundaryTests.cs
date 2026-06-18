using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Animancer;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonPresentation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonSimulation.Tests
{
    public sealed class RollbackDebugRigBoundaryTests
    {
        const string RollbackDebugRigPath = "Assets/Prefabs/Simulation/RollbackDebugRig.prefab";
        const string RollbackDebugRigGuid = "c0d02a2337f24d10953d8de0b5e2de6a";
        const string CorinPrefabPath = "Assets/Prefabs/Character/可琳.prefab";
        const string CorinHumanoidPrefabPath = "Assets/Prefabs/Character/可琳_Humanoid.prefab";
        const string CorinPrefabGuid = "43b770cfc3328e040b7a205c1a61f45b";
        const string PredictionInputFrameSourceGuid = "21976d05acaa4f3e911bd23075f5668a";
        const string PredictionInputRecorderGuid = "f5b7ecfe7dcd45ae963f2a0af520c87a";
        const string SnapshotRecorderGuid = "6ebda9870bd945549299a11bbfb8de1c";
        const string CharacterFrameRollbackSimulationGuid = "73bce0421eee1d84096242add6027c57";
        const string SynctestRunnerGuid = "76dd67150d57413e9cf41bca3e79f6ef";
        const string LatencyRunnerGuid = "d43c4b24d029331458e709b1b3b82db2";
        const string SoakRunnerGuid = "f79063608d784da787c3554c8d0eda2d";
        const string DebugRigInputSourceFileId = "8801000000000000002";
        const string DebugRigInputRecorderFileId = "8801000000000000003";
        const string DebugRigSnapshotRecorderFileId = "8801000000000000004";
        const string DebugRigReplayAdapterFileId = "8801000000000000005";
        const string DebugRigSynctestRunnerFileId = "8801000000000000006";
        const string DebugRigLatencyRunnerFileId = "8801000000000000007";
        const string DebugRigSoakRunnerFileId = "8801000000000000008";
        const string SandboxLocomotionRefFileId = "8802000000000000100";
        const string SandboxRuntimeRefFileId = "8802000000000000102";
        const string SandboxInputBufferRefFileId = "8802000000000000103";
        const string SandboxRequestBufferRefFileId = "8802000000000000104";
        const string SandboxPresentationRefFileId = "8802000000000000105";
        const string SandboxTickDriverFileId = "946519504";
        const string SandboxCameraFileId = "5809153074833929713";

        static readonly string[] CorinPrefabPaths =
        {
            CorinPrefabPath,
            CorinHumanoidPrefabPath
        };

        static readonly Type[] RollbackDebugToolTypes =
        {
            typeof(LocomotionPredictionInputFrameSource),
            typeof(PredictionInputHistoryTickRecorder),
            typeof(LocomotionSnapshotHistoryRecorder),
            typeof(CharacterFrameRollbackSimulation),
            typeof(LocalRollbackSynctestDebugRunner),
            typeof(LocalLatencyReconciliationDebugRunner),
            typeof(LocalRollbackSoakDebugRunner)
        };

        static readonly string[] RollbackDebugToolGuids =
        {
            PredictionInputFrameSourceGuid,
            PredictionInputRecorderGuid,
            SnapshotRecorderGuid,
            CharacterFrameRollbackSimulationGuid,
            SynctestRunnerGuid,
            LatencyRunnerGuid,
            SoakRunnerGuid
        };

        [Test]
        public void CorinPrefabsDoNotCarryRollbackDebugTooling()
        {
            foreach (string path in CorinPrefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.NotNull(prefab, path);

                foreach (Type type in RollbackDebugToolTypes)
                    Assert.That(prefab.GetComponentsInChildren(type, true), Is.Empty, $"{path} {type.Name}");

                string yaml = ReadAssetText(path);
                foreach (string guid in RollbackDebugToolGuids)
                    Assert.That(MonoBehaviourBlocks(yaml, guid), Is.Empty, $"{path} {guid}");

                Assert.That(prefab.GetComponentsInChildren<CharacterFrameRuntimeController>(true), Has.Length.EqualTo(1), path);
            }
        }

        [Test]
        public void RollbackDebugRigPrefabOwnsToolingWithoutGameplayRuntimeDuplication()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RollbackDebugRigPath);
            Assert.NotNull(prefab, RollbackDebugRigPath);

            LocomotionPredictionInputFrameSource inputSource = SingleComponent<LocomotionPredictionInputFrameSource>(prefab);
            PredictionInputHistoryTickRecorder inputRecorder = SingleComponent<PredictionInputHistoryTickRecorder>(prefab);
            LocomotionSnapshotHistoryRecorder snapshotRecorder = SingleComponent<LocomotionSnapshotHistoryRecorder>(prefab);
            CharacterFrameRollbackSimulation simulation = SingleComponent<CharacterFrameRollbackSimulation>(prefab);
            LocalRollbackSynctestDebugRunner synctest = SingleComponent<LocalRollbackSynctestDebugRunner>(prefab);
            LocalLatencyReconciliationDebugRunner latency = SingleComponent<LocalLatencyReconciliationDebugRunner>(prefab);
            LocalRollbackSoakDebugRunner soak = SingleComponent<LocalRollbackSoakDebugRunner>(prefab);

            Assert.AreSame(inputSource, inputRecorder.InputSourceBehaviour);
            Assert.AreSame(inputRecorder, synctest.InputRecorder);
            Assert.AreSame(snapshotRecorder, synctest.SnapshotRecorder);
            Assert.AreSame(simulation, synctest.SimulationBehaviour);
            Assert.AreSame(inputRecorder, latency.InputRecorder);
            Assert.AreSame(snapshotRecorder, latency.SnapshotRecorder);
            Assert.AreSame(simulation, latency.SimulationBehaviour);
            Assert.AreSame(simulation, soak.SimulationBehaviour);
            Assert.That(prefab.GetComponentsInChildren<CharacterFrameRuntimeController>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<CharacterMotionDriver>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<CharacterAnimancerPresenter>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<AnimancerComponent>(true), Is.Empty);
        }

        [Test]
        public void SandboxSceneWiresRollbackDebugRigToCorinRuntime()
        {
            string scene = ReadAssetText("Assets/Scenes/Sandbox.unity");
            Assert.That(scene, Does.Contain($"guid: {RollbackDebugRigGuid}"));
            Assert.That(scene, Does.Not.Contain($"target: {{fileID: 3115813924598397739, guid: {CorinPrefabGuid}"));
            Assert.That(scene, Does.Not.Contain($"target: {{fileID: 7403338803726517453, guid: {CorinPrefabGuid}"));
            Assert.That(scene, Does.Not.Contain($"target: {{fileID: 7555210597970078956, guid: {CorinPrefabGuid}"));
            Assert.That(scene, Does.Not.Contain($"target: {{fileID: 9187719410922289072, guid: {CorinPrefabGuid}"));
            AssertSceneOverride(scene, DebugRigInputSourceFileId, "runtimeController", SandboxRuntimeRefFileId);
            AssertSceneOverride(scene, DebugRigInputSourceFileId, "buttonSourceBehaviour", SandboxRequestBufferRefFileId);
            AssertSceneOverride(scene, DebugRigInputRecorderFileId, "tickDriver", SandboxTickDriverFileId);
            AssertSceneOverride(scene, DebugRigSnapshotRecorderFileId, "tickDriver", SandboxTickDriverFileId);
            AssertSceneOverride(scene, DebugRigSnapshotRecorderFileId, "runtimeController", SandboxRuntimeRefFileId);
            AssertSceneOverride(scene, DebugRigSnapshotRecorderFileId, "inputBufferComponent", SandboxInputBufferRefFileId);
            AssertSceneOverride(scene, DebugRigReplayAdapterFileId, "runtimeController", SandboxRuntimeRefFileId);
            AssertSceneOverride(scene, DebugRigReplayAdapterFileId, "inputBufferComponent", SandboxInputBufferRefFileId);
            AssertSceneOverride(scene, DebugRigSynctestRunnerFileId, "presentationInterpolator", SandboxPresentationRefFileId);
            AssertSceneOverride(scene, DebugRigSynctestRunnerFileId, "cameraController", SandboxCameraFileId);
            AssertSceneOverride(scene, DebugRigLatencyRunnerFileId, "presentationInterpolator", SandboxPresentationRefFileId);
            AssertSceneOverride(scene, DebugRigSoakRunnerFileId, "presentationInterpolator", SandboxPresentationRefFileId);
            AssertSceneOverride(scene, DebugRigSoakRunnerFileId, "cameraController", SandboxCameraFileId);
        }

        [Test]
        public void MissingDebugRigReferencesFailWithoutHierarchyFallback()
        {
            GameObject character = new GameObject("character");
            GameObject rig = new GameObject("debug-rig");
            try
            {
                character.AddComponent<PredictionInputHistoryTickRecorder>();
                character.AddComponent<LocomotionSnapshotHistoryRecorder>();
                character.AddComponent<CharacterFrameRollbackSimulation>();
                LocalRollbackSynctestDebugRunner synctest = rig.AddComponent<LocalRollbackSynctestDebugRunner>();
                LocalLatencyReconciliationDebugRunner latency = rig.AddComponent<LocalLatencyReconciliationDebugRunner>();
                LocalRollbackSoakDebugRunner soak = rig.AddComponent<LocalRollbackSoakDebugRunner>();
                synctest.RunOnKeyDown = false;
                latency.RunOnKeyDown = false;
                soak.RunOnKeyDown = false;

                Assert.False(synctest.RunDebugSynctest());
                Assert.That(synctest.LastResult.FailureReason, Does.Contain("missing recorder or simulation"));
                Assert.False(latency.RunReconciliation());
                Assert.That(string.Join(",", latency.LastResult.Comparison.Differences), Does.Contain("missing recorder or simulation"));
                Assert.False(soak.RunSoak());
                Assert.That(soak.LastResult.FailureReason, Does.Contain("missing simulation"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
                UnityEngine.Object.DestroyImmediate(rig);
            }
        }

        static T SingleComponent<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            Assert.That(components, Has.Length.EqualTo(1), typeof(T).Name);
            return components[0];
        }

        static string ReadAssetText(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, Encoding.UTF8);
        }

        static void AssertSceneOverride(string scene, string targetFileId, string propertyPath, string referenceFileId)
        {
            string targetLine = $"target: {{fileID: {targetFileId}, guid: {RollbackDebugRigGuid}";
            int targetIndex = scene.IndexOf(targetLine, StringComparison.Ordinal);
            while (targetIndex >= 0)
            {
                int nextTargetIndex = scene.IndexOf("    - target:", targetIndex + targetLine.Length, StringComparison.Ordinal);
                if (nextTargetIndex < 0)
                    nextTargetIndex = scene.Length;

                string block = scene.Substring(targetIndex, nextTargetIndex - targetIndex);
                if (block.Contains($"propertyPath: {propertyPath}") &&
                    block.Contains($"objectReference: {{fileID: {referenceFileId}}}"))
                {
                    return;
                }

                targetIndex = scene.IndexOf(targetLine, nextTargetIndex, StringComparison.Ordinal);
            }

            Assert.Fail($"{targetFileId}.{propertyPath} -> {referenceFileId}");
        }

        static int MonoBehaviourBlockCount(string yaml, string scriptGuid)
        {
            int count = 0;
            foreach (Match match in Regex.Matches(yaml, @"--- !u!114 &[^\r\n]+\r?\nMonoBehaviour:\r?\n(?:(?!\r?\n--- !u!).)*", RegexOptions.Singleline))
            {
                if (match.Value.Contains($"guid: {scriptGuid}"))
                    count++;
            }

            return count;
        }

        static string[] MonoBehaviourBlocks(string yaml, string scriptGuid)
        {
            int count = MonoBehaviourBlockCount(yaml, scriptGuid);
            string[] blocks = new string[count];
            int index = 0;
            foreach (Match match in Regex.Matches(yaml, @"--- !u!114 &[^\r\n]+\r?\nMonoBehaviour:\r?\n(?:(?!\r?\n--- !u!).)*", RegexOptions.Singleline))
            {
                if (match.Value.Contains($"guid: {scriptGuid}"))
                    blocks[index++] = match.Value;
            }

            return blocks;
        }
    }
}
