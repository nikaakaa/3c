using System;
using System.Collections.Generic;
using System.IO;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Editor;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    [InitializeOnLoad]
    public static class CharacterPresentationReplicationGeneratedSamplingWorkflow
    {
        sealed class Workflow : IDiagnosticSamplingWorkflow
        {
            public string CapabilityId =>
                CharacterPresentationReplicationDiagnosticIdentity.CapabilityId;
            public IReadOnlyList<string> SamplerIds => s_SamplerIds;
            public string SelectedSamplerId => s_SelectedSamplerId;
            public bool IsCapturing => s_Controller != null && !s_Finalizing;
            public bool IsFinalizing => s_Finalizing;
            public bool IsControlledCaptureWindow =>
                IsCapturing && s_ControlledCaptureWindow;
            public bool IsCaptureWindowOpen =>
                IsControlledCaptureWindow && s_CaptureWindowOpen;
            public string CurrentSampleIdentity => s_CurrentSampleIdentity;
            public string LastSavedSampleIdentity => s_LastSavedSampleIdentity;
            public string LastSavedPath => s_LastMainCsvPath;
            public string LastSavedDirectory => s_OutputRoot;
            public string LastManifestPath => s_LastManifestPath;
            public string LastFailure => s_LastFailure;
            public int CapturedFrameCount =>
                s_Controller?.CapturedFrameCount ?? 0;
            public int LastSavedFrameCount => s_LastSavedFrameCount;
            public string GetArtifactPath(string artifactId) =>
                s_LastArtifacts.TryGetValue(
                    artifactId ?? string.Empty,
                    out string path)
                    ? path
                    : string.Empty;
            public void SelectSampler(string samplerId) =>
                Select(samplerId);
            public void Start(in DiagnosticSamplingStartRequest request) =>
                StartCapture(in request);
            public void OpenControlledCaptureWindow() => OpenWindow();
            public void CloseControlledCaptureWindow() => CloseWindow();
            public void StopAndSave() => StopCapture();
        }

        const string StartMenu =
            "Tools/3C/Diagnostics/Character Presentation Replication Sampling/Start";
        const string StopMenu =
            "Tools/3C/Diagnostics/Character Presentation Replication Sampling/Stop and Save";
        const string RevealMenu =
            "Tools/3C/Diagnostics/Character Presentation Replication Sampling/Reveal Last Capture";
        const int DefaultPacketCapacity = 4096;
        const int QueueCapacity = 512;
        const string GameplayLabPlayerActorId = "gameplay-lab-player";
        const string LastManifestPreference =
            "ThirdPerson.Character.PresentationReplicationDiagnostics.LastManifestPath";
        const string LastSampleIdentityPreference =
            "ThirdPerson.Character.PresentationReplicationDiagnostics.LastSampleIdentity";
        static readonly string[] s_SamplerIds =
        {
            CharacterPresentationReplicationDiagnosticIdentity.CoreSamplerId,
            CharacterPresentationReplicationDiagnosticIdentity.FullSamplerId
        };
        static readonly Workflow s_Workflow = new Workflow();
        static CharacterPresentationReplicationGeneratedCaptureController s_Controller;
        static string s_OutputRoot = string.Empty;
        static string s_CurrentSampleIdentity = string.Empty;
        static string s_LastSavedSampleIdentity = string.Empty;
        static string s_LastManifestPath = string.Empty;
        static string s_LastMainCsvPath = string.Empty;
        static string s_LastFailure = string.Empty;
        static readonly Dictionary<string, string> s_LastArtifacts =
            new Dictionary<string, string>(StringComparer.Ordinal);
        static int s_LastSavedFrameCount;
        static bool s_ControlledCaptureWindow;
        static bool s_CaptureWindowOpen;
        static bool s_Finalizing;
        static string s_SelectedSamplerId =
            CharacterPresentationReplicationDiagnosticIdentity.FullSamplerId;
        static string s_ActiveSamplerId = string.Empty;

        static CharacterPresentationReplicationGeneratedSamplingWorkflow()
        {
            DiagnosticSamplingWorkflowRegistry.Register(s_Workflow);
            RestoreLastCapture();
            EditorApplication.update += PollFinalization;
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            EditorApplication.quitting += Cancel;
        }

        public static bool IsCapturing => s_Controller != null && !s_Finalizing;
        public static bool IsFinalizing => s_Finalizing;
        public static bool IsControlledCaptureWindow =>
            IsCapturing && s_ControlledCaptureWindow;
        public static bool IsCaptureWindowOpen =>
            IsControlledCaptureWindow && s_CaptureWindowOpen;
        public static string CurrentSampleIdentity => s_CurrentSampleIdentity;
        public static string LastSavedSampleIdentity => s_LastSavedSampleIdentity;
        public static string LastSavedDirectory => s_OutputRoot;
        public static string LastManifestPath => s_LastManifestPath;
        public static string LastMainCsvPath => s_LastMainCsvPath;
        public static string LastFailure => s_LastFailure;
        public static int CapturedFrameCount => s_Controller?.CapturedFrameCount ?? 0;
        public static int LastSavedFrameCount => s_LastSavedFrameCount;
        public static string SelectedSamplerId => s_SelectedSamplerId;
        public static string GetArtifactPath(string artifactId) =>
            s_LastArtifacts.TryGetValue(
                artifactId ?? string.Empty,
                out string path)
                ? path
                : string.Empty;

        [MenuItem(StartMenu)]
        static void StartFromMenu()
        {
            var request = new DiagnosticSamplingStartRequest(false, 0);
            StartCapture(in request);
        }

        [MenuItem(StartMenu, true)]
        static bool CanStart() =>
            EditorApplication.isPlaying &&
            s_Controller == null &&
            !EditorApplication.isCompiling;

        public static void Select(string samplerId)
        {
            if (s_Controller != null || s_Finalizing)
                throw new InvalidOperationException(
                    "Presentation replication sampler cannot change during capture.");
            if (!string.Equals(
                    samplerId,
                    CharacterPresentationReplicationDiagnosticIdentity.CoreSamplerId,
                    StringComparison.Ordinal) &&
                !string.Equals(
                    samplerId,
                    CharacterPresentationReplicationDiagnosticIdentity.FullSamplerId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Presentation replication sampler is unknown: {samplerId}.",
                    nameof(samplerId));
            }
            s_SelectedSamplerId = samplerId;
        }

        public static void StartCapture(
            in DiagnosticSamplingStartRequest startRequest)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Presentation replication generated sampling requires Play Mode.");
            if (s_Controller != null)
                throw new InvalidOperationException(
                    "Presentation replication generated sampling is already active.");
            if (!AnimationPresentationRuntimeTargetRegistry.TryGet(
                    new ActorId(GameplayLabPlayerActorId),
                    out AnimationPresentationRuntimeTarget target))
            {
                throw new InvalidOperationException(
                    "Gameplay Lab player Animation Presentation target is unavailable.");
            }
            Guid sampleIdentity = Guid.NewGuid();
            DateTime startedUtc = DateTime.UtcNow;
            int packetCapacity = ResolvePacketCapacity(
                startRequest.MinimumEventCount);
            s_OutputRoot = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Diagnostics",
                "GeneratedPresentationSampling",
                $"{startedUtc:yyyyMMdd-HHmmss}-{sampleIdentity:N}"));
            var request = new DiagnosticCaptureStartRequest(
                "presentation-replication/committed-frame",
                CharacterPresentationReplicationDiagnosticEvent.LineageTypeIdentity,
                packetCapacity,
                QueueCapacity,
                DiagnosticBinaryPacketWriter.TransportIdentity,
                "presentation-replication/capture",
                Path.Combine(s_OutputRoot, "capture.packets.bin"),
                Path.Combine(s_OutputRoot, "schema.json"),
                Path.Combine(s_OutputRoot, "runtime.manifest.json"));
            var metadata = new CharacterPresentationReplicationCaptureMetadata(
                sampleIdentity,
                startedUtc,
                target.PresentationIdentity,
                target.RuntimeInstanceId,
                target.HostInstanceId,
                CharacterPresentationReplicationDiagnosticIdentity.ReferenceProfileId);
            var controller =
                new CharacterPresentationReplicationGeneratedCaptureController(
                    target,
                    request,
                    s_SelectedSamplerId,
                    in metadata);
            try
            {
                if (!controller.Start(!startRequest.ControlledCaptureWindow))
                {
                    throw new InvalidOperationException(
                        controller.Failure?.Message ??
                        "Presentation replication generated sampling failed to start.");
                }
                s_Controller = controller;
                s_ActiveSamplerId = s_SelectedSamplerId;
                s_CurrentSampleIdentity = sampleIdentity.ToString("N");
                s_LastFailure = string.Empty;
                s_LastSavedFrameCount = 0;
                s_LastManifestPath = string.Empty;
                s_LastMainCsvPath = string.Empty;
                s_LastArtifacts.Clear();
                s_ControlledCaptureWindow = startRequest.ControlledCaptureWindow;
                s_CaptureWindowOpen = !startRequest.ControlledCaptureWindow;
                Debug.Log(
                    $"Presentation replication generated sampling started: {s_OutputRoot}");
            }
            catch
            {
                controller.Dispose();
                throw;
            }
        }

        static int ResolvePacketCapacity(int minimumEventCount)
        {
            int required = checked(minimumEventCount + 1);
            int capacity = DefaultPacketCapacity;
            while (capacity < required)
                capacity = checked(capacity * 2);
            return capacity;
        }

        static void OpenWindow()
        {
            if (!IsControlledCaptureWindow || s_CaptureWindowOpen)
                throw new InvalidOperationException(
                    "Presentation replication controlled capture window cannot open.");
            s_Controller.Attach();
            s_CaptureWindowOpen = true;
        }

        static void CloseWindow()
        {
            if (!IsControlledCaptureWindow || !s_CaptureWindowOpen)
                throw new InvalidOperationException(
                    "Presentation replication controlled capture window cannot close.");
            s_Controller.Detach();
            s_CaptureWindowOpen = false;
        }

        [MenuItem(StopMenu)]
        static void StopCapture()
        {
            if (!IsCapturing)
                return;
            DiagnosticCaptureStopOutcome outcome =
                DiagnosticCaptureStopOutcome.Completed();
            s_Controller.Stop(in outcome);
            s_Finalizing = true;
        }

        [MenuItem(StopMenu, true)]
        static bool CanStop() => IsCapturing;

        [MenuItem(RevealMenu)]
        static void RevealLastCapture()
        {
            if (!string.IsNullOrEmpty(s_OutputRoot))
                EditorUtility.RevealInFinder(s_OutputRoot);
        }

        [MenuItem(RevealMenu, true)]
        static bool CanRevealLastCapture() => Directory.Exists(s_OutputRoot);

        static void PollFinalization()
        {
            if (!s_Finalizing || s_Controller == null)
                return;
            DiagnosticHostFinalizationResult result;
            try
            {
                bool completed = s_Controller.TryFinalize(
                    s_OutputRoot,
                    out result);
                if (result == null)
                    return;
                if (!completed)
                {
                    CompleteFailure(
                        result.Manifest.Failure?.Message ??
                        "Presentation replication Host finalization failed.");
                    return;
                }
                s_LastManifestPath = Path.Combine(
                    s_OutputRoot,
                    "capability.manifest.json");
                CaptureArtifacts(result.Manifest, s_ActiveSamplerId);
                s_LastSavedSampleIdentity = s_CurrentSampleIdentity;
                s_LastSavedFrameCount = s_Controller.CapturedFrameCount;
                PersistLastCapture();
                CompleteController();
                Debug.Log(
                    $"Presentation replication generated sampling completed: {s_LastManifestPath}");
            }
            catch (Exception exception)
            {
                CompleteFailure(exception.Message);
                Debug.LogException(exception);
            }
        }

        static void CaptureArtifacts(
            DiagnosticCapabilityManifest manifest,
            string samplerId)
        {
            s_LastArtifacts.Clear();
            for (int samplerIndex = 0;
                 samplerIndex < manifest.Samplers.Count;
                 samplerIndex++)
            {
                DiagnosticSamplerManifest sampler = manifest.Samplers[samplerIndex];
                if (!string.Equals(
                        sampler.SamplerId,
                        samplerId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                for (int artifactIndex = 0;
                     artifactIndex < sampler.Artifacts.Count;
                     artifactIndex++)
                {
                    string path = sampler.Artifacts[artifactIndex].Path;
                    string artifactId = artifactIndex == 0
                        ? "main"
                        : Path.GetFileNameWithoutExtension(path);
                    s_LastArtifacts[artifactId] = path;
                }
                break;
            }
            s_LastMainCsvPath = s_LastArtifacts.TryGetValue(
                "main",
                out string main)
                ? main
                : string.Empty;
        }

        static void CompleteFailure(string failure)
        {
            s_LastFailure = failure ?? string.Empty;
            CompleteController();
            Debug.LogError(
                $"Presentation replication generated sampling failed: {s_LastFailure}");
        }

        static void CompleteController()
        {
            s_Controller?.Dispose();
            s_Controller = null;
            s_CurrentSampleIdentity = string.Empty;
            s_ActiveSamplerId = string.Empty;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
            s_Finalizing = false;
        }

        static void RestoreLastCapture()
        {
            string manifestPath = ProjectEditorPreferences.GetString(
                LastManifestPreference,
                string.Empty);
            if (!File.Exists(manifestPath))
                manifestPath = FindLatestManifest();
            if (string.IsNullOrEmpty(manifestPath))
                return;
            try
            {
                byte[] content = File.ReadAllBytes(manifestPath);
                var document = new DiagnosticEncodedDocument(
                    content,
                    DiagnosticArtifactIntegrity.ComputeSha256(content));
                DiagnosticCapabilityManifest manifest =
                    DiagnosticCapabilityCodec.DecodeCapabilityManifest(document);
                if (manifest.Status != DiagnosticCaptureStatus.Completed ||
                    manifest.Samplers.Count != 1)
                {
                    return;
                }
                s_LastManifestPath = Path.GetFullPath(manifestPath);
                s_OutputRoot = Path.GetDirectoryName(s_LastManifestPath);
                string samplerId = manifest.Samplers[0].SamplerId;
                CaptureArtifacts(manifest, samplerId);
                s_SelectedSamplerId = samplerId;
                s_LastSavedSampleIdentity = ProjectEditorPreferences.GetString(
                    LastSampleIdentityPreference,
                    Path.GetFileName(s_OutputRoot));
                s_LastSavedFrameCount = checked((int)manifest.SampleCount);
            }
            catch
            {
                s_LastManifestPath = string.Empty;
                s_LastMainCsvPath = string.Empty;
                s_OutputRoot = string.Empty;
                s_LastArtifacts.Clear();
                s_LastSavedFrameCount = 0;
            }
        }

        static string FindLatestManifest()
        {
            string root = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Diagnostics",
                "GeneratedPresentationSampling"));
            if (!Directory.Exists(root))
                return string.Empty;
            string latest = string.Empty;
            DateTime latestWrite = DateTime.MinValue;
            foreach (string directory in Directory.GetDirectories(root))
            {
                string manifest = Path.Combine(
                    directory,
                    "capability.manifest.json");
                if (!File.Exists(manifest))
                    continue;
                DateTime write = File.GetLastWriteTimeUtc(manifest);
                if (write <= latestWrite)
                    continue;
                latest = manifest;
                latestWrite = write;
            }
            return latest;
        }

        static void PersistLastCapture()
        {
            ProjectEditorPreferences.SetString(
                LastManifestPreference,
                Path.GetFullPath(s_LastManifestPath));
            ProjectEditorPreferences.SetString(
                LastSampleIdentityPreference,
                s_LastSavedSampleIdentity ?? string.Empty);
        }

        static void Cancel()
        {
            if (s_Controller != null && !s_Finalizing)
            {
                DiagnosticCaptureStopOutcome outcome =
                    DiagnosticCaptureStopOutcome.Cancelled();
                s_Controller.Stop(in outcome);
            }
            s_Controller?.Dispose();
            s_Controller = null;
            s_CurrentSampleIdentity = string.Empty;
            s_ActiveSamplerId = string.Empty;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
            s_Finalizing = false;
        }
    }
}
