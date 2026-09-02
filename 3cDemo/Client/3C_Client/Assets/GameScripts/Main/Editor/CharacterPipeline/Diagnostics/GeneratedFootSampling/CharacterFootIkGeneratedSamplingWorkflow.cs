using System;
using System.IO;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling.Editor
{
    [InitializeOnLoad]
    public static class CharacterFootIkGeneratedSamplingWorkflow
    {
        sealed class Workflow : IDiagnosticSamplingWorkflow
        {
            public string CapabilityId =>
                CharacterFootIkDiagnosticIdentity.CapabilityId;
            public bool IsCapturing =>
                CharacterFootIkGeneratedSamplingWorkflow.IsCapturing;
            public bool IsFinalizing =>
                CharacterFootIkGeneratedSamplingWorkflow.IsFinalizing;
            public bool IsControlledCaptureWindow =>
                CharacterFootIkGeneratedSamplingWorkflow.IsControlledCaptureWindow;
            public bool IsCaptureWindowOpen =>
                CharacterFootIkGeneratedSamplingWorkflow.IsCaptureWindowOpen;
            public string CurrentSampleIdentity =>
                CharacterFootIkGeneratedSamplingWorkflow.CurrentSampleIdentity;
            public string LastSavedSampleIdentity =>
                CharacterFootIkGeneratedSamplingWorkflow.LastSavedSampleIdentity;
            public string LastSavedPath =>
                CharacterFootIkGeneratedSamplingWorkflow.LastMainCsvPath;
            public string LastSavedDirectory =>
                CharacterFootIkGeneratedSamplingWorkflow.LastSavedDirectory;
            public string LastManifestPath =>
                CharacterFootIkGeneratedSamplingWorkflow.LastManifestPath;
            public string LastFailure =>
                CharacterFootIkGeneratedSamplingWorkflow.LastFailure;
            public int CapturedFrameCount =>
                CharacterFootIkGeneratedSamplingWorkflow.CapturedFrameCount;
            public int LastSavedFrameCount =>
                CharacterFootIkGeneratedSamplingWorkflow.LastSavedFrameCount;

            public string GetArtifactPath(string artifactId) =>
                CharacterFootIkGeneratedSamplingWorkflow.GetArtifactPath(
                    artifactId);

            public void Start(bool controlledCaptureWindow) =>
                CharacterFootIkGeneratedSamplingWorkflow.Start(
                    controlledCaptureWindow);

            public void OpenControlledCaptureWindow() =>
                CharacterFootIkGeneratedSamplingWorkflow
                    .OpenControlledCaptureWindow();

            public void CloseControlledCaptureWindow() =>
                CharacterFootIkGeneratedSamplingWorkflow
                    .CloseControlledCaptureWindow();

            public void StopAndSave() =>
                CharacterFootIkGeneratedSamplingWorkflow.StopAndSave();
        }

        const string StartMenu =
            "Tools/3C/Diagnostics/Foot IK Generated Sampling/Start";
        const string StopMenu =
            "Tools/3C/Diagnostics/Foot IK Generated Sampling/Stop and Save";
        const string RevealMenu =
            "Tools/3C/Diagnostics/Foot IK Generated Sampling/Reveal Last Capture";
        const int PacketCapacity = 2048;
        const int QueueCapacity = 512;
        static readonly Workflow s_Workflow = new Workflow();
        static CharacterFootIkGeneratedCaptureController s_Controller;
        static string s_OutputRoot = string.Empty;
        static string s_CurrentSampleIdentity = string.Empty;
        static string s_LastSavedSampleIdentity = string.Empty;
        static string s_LastManifestPath = string.Empty;
        static string s_LastMainCsvPath = string.Empty;
        static string s_LastFailure = string.Empty;
        static readonly System.Collections.Generic.Dictionary<string, string>
            s_LastArtifacts =
                new System.Collections.Generic.Dictionary<string, string>(
                    StringComparer.Ordinal);
        static int s_LastSavedFrameCount;
        static bool s_ControlledCaptureWindow;
        static bool s_CaptureWindowOpen;
        static bool s_Finalizing;

        static CharacterFootIkGeneratedSamplingWorkflow()
        {
            DiagnosticSamplingWorkflowRegistry.Register(s_Workflow);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            EditorApplication.quitting += Cancel;
        }

        public static bool IsCapturing =>
            s_Controller != null && !s_Finalizing;
        public static bool IsFinalizing => s_Finalizing;
        public static bool IsControlledCaptureWindow =>
            IsCapturing && s_ControlledCaptureWindow;
        public static bool IsCaptureWindowOpen =>
            IsControlledCaptureWindow && s_CaptureWindowOpen;
        public static string CurrentSampleIdentity =>
            s_CurrentSampleIdentity;
        public static string LastSavedSampleIdentity =>
            s_LastSavedSampleIdentity;
        public static string LastSavedDirectory =>
            !string.IsNullOrEmpty(s_LastManifestPath)
                ? s_OutputRoot
                : string.Empty;
        public static string LastManifestPath => s_LastManifestPath;
        public static string LastMainCsvPath => s_LastMainCsvPath;
        public static string LastFailure => s_LastFailure;
        public static int CapturedFrameCount =>
            s_Controller?.CapturedFrameCount ?? 0;
        public static int LastSavedFrameCount => s_LastSavedFrameCount;

        public static string GetArtifactPath(string artifactId) =>
            s_LastArtifacts.TryGetValue(
                artifactId ?? string.Empty,
                out string path)
                ? path
                : string.Empty;

        [MenuItem(StartMenu)]
        static void StartFromMenu() => Start(false);

        public static void Start(bool controlledCaptureWindow)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Foot IK generated sampling requires Play Mode.");
            }
            if (s_Controller != null)
            {
                throw new InvalidOperationException(
                    "Foot IK generated sampling is already active.");
            }
            if (AnimationPresentationRuntimeTargetRegistry.Targets.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Foot IK generated sampling requires exactly one Animation Presentation target; found {AnimationPresentationRuntimeTargetRegistry.Targets.Count}.");
            }
            AnimationPresentationRuntimeTarget target =
                AnimationPresentationRuntimeTargetRegistry.Targets[0];
            Guid sampleIdentity = Guid.NewGuid();
            DateTime startedUtc = DateTime.UtcNow;
            s_OutputRoot = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Diagnostics",
                "GeneratedFootSampling",
                $"{startedUtc:yyyyMMdd-HHmmss}-{sampleIdentity:N}"));
            var request = new DiagnosticCaptureStartRequest(
                "presentation-commit/1",
                CharacterFootIkCommitDiagnosticEvent.LineageTypeIdentity,
                PacketCapacity,
                QueueCapacity,
                DiagnosticBinaryPacketWriter.TransportIdentity,
                CharacterFootIkDiagnosticIdentity.FullSamplerId,
                Path.Combine(s_OutputRoot, "capture.packets.bin"),
                Path.Combine(s_OutputRoot, "schema.json"),
                Path.Combine(s_OutputRoot, "runtime.manifest.json"));
            var metadata = new CharacterFootIkCaptureMetadata(
                sampleIdentity,
                startedUtc,
                target.ProgramIdentity,
                target.RuntimeInstanceId,
                target.HostInstanceId);
            var controller = new CharacterFootIkGeneratedCaptureController(
                target,
                request,
                in metadata);
            try
            {
                if (!controller.Start(!controlledCaptureWindow))
                {
                    throw new InvalidOperationException(
                        controller.Failure?.Message ??
                        "Foot IK generated sampling failed to start.");
                }
                s_Controller = controller;
                s_CurrentSampleIdentity = sampleIdentity.ToString("N");
                s_LastFailure = string.Empty;
                s_LastSavedFrameCount = 0;
                s_LastManifestPath = string.Empty;
                s_LastMainCsvPath = string.Empty;
                s_LastArtifacts.Clear();
                s_ControlledCaptureWindow = controlledCaptureWindow;
                s_CaptureWindowOpen = !controlledCaptureWindow;
                Debug.Log(
                    $"Foot IK generated sampling started: {s_OutputRoot}");
            }
            catch
            {
                controller.Dispose();
                throw;
            }
        }

        [MenuItem(StartMenu, true)]
        static bool CanStart() =>
            EditorApplication.isPlaying &&
            s_Controller == null &&
            !EditorApplication.isCompiling;

        public static void OpenControlledCaptureWindow()
        {
            if (!IsControlledCaptureWindow || s_CaptureWindowOpen)
            {
                throw new InvalidOperationException(
                    "Foot IK controlled capture window cannot open.");
            }
            s_Controller.Attach();
            s_CaptureWindowOpen = true;
        }

        public static void CloseControlledCaptureWindow()
        {
            if (!IsControlledCaptureWindow || !s_CaptureWindowOpen)
            {
                throw new InvalidOperationException(
                    "Foot IK controlled capture window cannot close.");
            }
            s_Controller.Detach();
            s_CaptureWindowOpen = false;
        }

        [MenuItem(StopMenu)]
        public static void StopAndSave()
        {
            if (!IsCapturing)
                return;
            DiagnosticCaptureStopOutcome outcome =
                DiagnosticCaptureStopOutcome.Completed();
            s_Controller.Stop(in outcome);
            s_Finalizing = true;
            EditorApplication.update -= PollFinalization;
            EditorApplication.update += PollFinalization;
        }

        [MenuItem(StopMenu, true)]
        static bool CanStop() => IsCapturing;

        [MenuItem(RevealMenu)]
        static void RevealLastCapture()
        {
            if (!string.IsNullOrEmpty(s_LastManifestPath))
                EditorUtility.RevealInFinder(s_LastManifestPath);
        }

        [MenuItem(RevealMenu, true)]
        static bool CanRevealLastCapture() =>
            File.Exists(s_LastManifestPath);

        static void PollFinalization()
        {
            if (!s_Finalizing || s_Controller == null)
            {
                EditorApplication.update -= PollFinalization;
                return;
            }
            DiagnosticHostFinalizationResult result;
            try
            {
                bool completed = s_Controller.TryFinalize(
                    s_OutputRoot,
                    out result);
                if (result == null)
                {
                    if (s_Controller.Failure.HasValue)
                        CompleteFailure(s_Controller.Failure.Value.Message);
                    return;
                }
                if (!completed)
                {
                    CompleteFailure(
                        result.Manifest.Failure?.Message ??
                        "Foot IK Host finalization failed.");
                    return;
                }
                s_LastManifestPath = Path.Combine(
                    s_OutputRoot,
                    "capability.manifest.json");
                CaptureArtifacts(result.Manifest);
                s_LastSavedSampleIdentity = s_CurrentSampleIdentity;
                s_LastSavedFrameCount = checked(
                    (int)(result.Manifest.SampleCount / 2));
                CompleteController();
                Debug.Log(
                    $"Foot IK generated sampling completed: {s_LastManifestPath}");
            }
            catch (Exception exception)
            {
                CompleteFailure(exception.Message);
                Debug.LogException(exception);
            }
        }

        static void CaptureArtifacts(DiagnosticCapabilityManifest manifest)
        {
            for (int samplerIndex = 0;
                 samplerIndex < manifest.Samplers.Count;
                 samplerIndex++)
            {
                DiagnosticSamplerManifest sampler =
                    manifest.Samplers[samplerIndex];
                if (!string.Equals(
                        sampler.SamplerId,
                        CharacterFootIkDiagnosticIdentity.FullSamplerId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                for (int artifactIndex = 0;
                     artifactIndex < sampler.Artifacts.Count;
                     artifactIndex++)
                {
                    string path = sampler.Artifacts[artifactIndex].Path;
                    string name = Path.GetFileName(path);
                    if (name.EndsWith("%2Ffull.csv", StringComparison.Ordinal))
                    {
                        s_LastMainCsvPath = path;
                        s_LastArtifacts.Add("main", path);
                    }
                    else if (name.EndsWith(
                                 ".ground-contacts.csv",
                                 StringComparison.Ordinal))
                    {
                        s_LastArtifacts.Add("ground-contacts", path);
                    }
                    else if (name.EndsWith(
                                 ".ground-envelope.csv",
                                 StringComparison.Ordinal))
                    {
                        s_LastArtifacts.Add("ground-envelope", path);
                    }
                    else if (name.EndsWith(
                                 ".ground-surfaces.csv",
                                 StringComparison.Ordinal))
                    {
                        s_LastArtifacts.Add("ground-surfaces", path);
                    }
                }
            }
            if (s_LastArtifacts.Count != 4)
            {
                throw new InvalidDataException(
                    "Foot IK generated artifact set is incomplete.");
            }
        }

        static void CompleteFailure(string failure)
        {
            s_LastFailure = failure ?? string.Empty;
            CompleteController();
            Debug.LogError(
                $"Foot IK generated sampling failed: {s_LastFailure}");
        }

        static void CompleteController()
        {
            EditorApplication.update -= PollFinalization;
            s_Controller?.Dispose();
            s_Controller = null;
            s_CurrentSampleIdentity = string.Empty;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
            s_Finalizing = false;
        }

        static void Cancel()
        {
            EditorApplication.update -= PollFinalization;
            if (s_Controller != null && !s_Finalizing)
            {
                DiagnosticCaptureStopOutcome outcome =
                    DiagnosticCaptureStopOutcome.Cancelled();
                s_Controller.Stop(in outcome);
            }
            s_Controller?.Dispose();
            s_Controller = null;
            s_CurrentSampleIdentity = string.Empty;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
            s_Finalizing = false;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && IsCapturing)
                StopAndSave();
        }
    }
}
