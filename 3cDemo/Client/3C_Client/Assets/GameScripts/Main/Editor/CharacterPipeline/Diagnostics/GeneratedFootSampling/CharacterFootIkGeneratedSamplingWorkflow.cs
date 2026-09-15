using System;
using System.IO;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using ThirdPersonCharacter.Editor;
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
            public System.Collections.Generic.IReadOnlyList<string>
                SamplerIds => s_SamplerIds;
            public string SelectedSamplerId => s_SelectedSamplerId;
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

            public void SelectSampler(string samplerId) =>
                CharacterFootIkGeneratedSamplingWorkflow.SelectSampler(
                    samplerId);

            public void Start(in DiagnosticSamplingStartRequest request) =>
                CharacterFootIkGeneratedSamplingWorkflow.Start(
                    in request);

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
        const int DefaultPacketCapacity = 2048;
        const int QueueCapacity = 512;
        const string GameplayLabPlayerActorId = "gameplay-lab-player";
        const string LastManifestPreference =
            "ThirdPerson.Character.FootDiagnostics.LastManifestPath";
        const string LastSampleIdentityPreference =
            "ThirdPerson.Character.FootDiagnostics.LastSampleIdentity";
        static readonly string[] s_SamplerIds =
        {
            CharacterFootIkDiagnosticIdentity.CoreSamplerId,
            CharacterFootIkDiagnosticIdentity.FullSamplerId
        };
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
        static string s_SelectedSamplerId =
            CharacterFootIkDiagnosticIdentity.FullSamplerId;
        static string s_ActiveSamplerId = string.Empty;

        static CharacterFootIkGeneratedSamplingWorkflow()
        {
            DiagnosticSamplingWorkflowRegistry.Register(s_Workflow);
            RestoreLastCapture();
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
            Start(in request);
        }

        public static void SelectSampler(string samplerId)
        {
            if (s_Controller != null || s_Finalizing)
            {
                throw new InvalidOperationException(
                    "Foot IK sampler cannot change during capture.");
            }
            if (!string.Equals(
                    samplerId,
                    CharacterFootIkDiagnosticIdentity.CoreSamplerId,
                    StringComparison.Ordinal) &&
                !string.Equals(
                    samplerId,
                    CharacterFootIkDiagnosticIdentity.FullSamplerId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Foot IK sampler is unknown: {samplerId}.",
                    nameof(samplerId));
            }
            s_SelectedSamplerId = samplerId;
        }

        public static void Start(in DiagnosticSamplingStartRequest startRequest)
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
                "GeneratedFootSampling",
                $"{startedUtc:yyyyMMdd-HHmmss}-{sampleIdentity:N}"));
            var request = new DiagnosticCaptureStartRequest(
                "presentation-commit/1",
                CharacterFootIkCommitDiagnosticEvent.LineageTypeIdentity,
                packetCapacity,
                QueueCapacity,
                DiagnosticBinaryPacketWriter.TransportIdentity,
                s_SelectedSamplerId,
                Path.Combine(s_OutputRoot, "capture.packets.bin"),
                Path.Combine(s_OutputRoot, "schema.json"),
                Path.Combine(s_OutputRoot, "runtime.manifest.json"));
            var metadata = new CharacterFootIkCaptureMetadata(
                sampleIdentity,
                startedUtc,
                target.PresentationIdentity,
                target.RuntimeInstanceId,
                target.HostInstanceId);
            var controller = new CharacterFootIkGeneratedCaptureController(
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
                        "Foot IK generated sampling failed to start.");
                }
                s_Controller = controller;
                s_ActiveSamplerId = s_SelectedSamplerId;
                s_CurrentSampleIdentity = sampleIdentity.ToString("N");
                s_LastFailure = string.Empty;
                s_LastSavedFrameCount = 0;
                s_LastManifestPath = string.Empty;
                s_LastMainCsvPath = string.Empty;
                s_LastArtifacts.Clear();
                s_ControlledCaptureWindow =
                    startRequest.ControlledCaptureWindow;
                s_CaptureWindowOpen =
                    !startRequest.ControlledCaptureWindow;
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

        static int ResolvePacketCapacity(int minimumEventCount)
        {
            int required = checked(minimumEventCount * 2 + 1);
            int capacity = DefaultPacketCapacity;
            while (capacity < required)
                capacity = checked(capacity * 2);
            return capacity;
        }

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
            if (!string.IsNullOrEmpty(s_OutputRoot))
                EditorUtility.RevealInFinder(s_OutputRoot);
        }

        [MenuItem(RevealMenu, true)]
        static bool CanRevealLastCapture() =>
            Directory.Exists(s_OutputRoot);

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
                CaptureArtifacts(result.Manifest, s_ActiveSamplerId);
                s_LastSavedSampleIdentity = s_CurrentSampleIdentity;
                s_LastSavedFrameCount = checked(
                    (int)(result.Manifest.SampleCount / 2));
                PersistLastCapture();
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

        static void CaptureArtifacts(
            DiagnosticCapabilityManifest manifest,
            string samplerId)
        {
            for (int samplerIndex = 0;
                 samplerIndex < manifest.Samplers.Count;
                 samplerIndex++)
            {
                DiagnosticSamplerManifest sampler =
                    manifest.Samplers[samplerIndex];
                if (!string.Equals(
                        sampler.SamplerId,
                        samplerId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                if (sampler.Artifacts.Count == 0)
                    continue;
                s_LastMainCsvPath = sampler.Artifacts[0].Path;
                s_LastArtifacts.Add("main", s_LastMainCsvPath);
                for (int artifactIndex = 1;
                     artifactIndex < sampler.Artifacts.Count;
                     artifactIndex++)
                {
                    string path = sampler.Artifacts[artifactIndex].Path;
                    string name = Path.GetFileName(path);
                    if (name.EndsWith(
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
            int expectedArtifactCount = string.Equals(
                samplerId,
                CharacterFootIkDiagnosticIdentity.FullSamplerId,
                StringComparison.Ordinal)
                ? 4
                : 1;
            if (s_LastArtifacts.Count != expectedArtifactCount)
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
                s_LastArtifacts.Clear();
                string samplerId = manifest.Samplers[0].SamplerId;
                CaptureArtifacts(manifest, samplerId);
                s_SelectedSamplerId = samplerId;
                s_LastSavedSampleIdentity = ProjectEditorPreferences.GetString(
                    LastSampleIdentityPreference,
                    Path.GetFileName(s_OutputRoot));
                s_LastSavedFrameCount = checked(
                    (int)(manifest.SampleCount / 2));
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
                "GeneratedFootSampling"));
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
            s_ActiveSamplerId = string.Empty;
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
