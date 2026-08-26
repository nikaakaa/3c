using System;
using System.IO;
using ThirdPerson.ProductStartup;
using ThirdPersonCharacter.Editor.ProductBuild;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public interface IGameplayLabLauncherOperations
    {
        GameplayLabLauncherState ReadState();
        void Open();
        void Play(int variantIndex);
        void SyncAssets();
    }

    public readonly struct GameplayLabLauncherState
    {
        public GameplayLabLauncherState(string[] variantLabels, int selectedVariantIndex)
        {
            VariantLabels = variantLabels ?? throw new ArgumentNullException(nameof(variantLabels));
            if (variantLabels.Length == 0 || selectedVariantIndex < 0 || selectedVariantIndex >= variantLabels.Length)
                throw new ArgumentOutOfRangeException(nameof(selectedVariantIndex));
            SelectedVariantIndex = selectedVariantIndex;
        }

        public string[] VariantLabels { get; }
        public int SelectedVariantIndex { get; }
    }

    public static class GameplayLabLauncherRegistry
    {
        static IGameplayLabLauncherOperations s_Operations;

        public static IGameplayLabLauncherOperations Operations => s_Operations;

        public static void Register(IGameplayLabLauncherOperations operations)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));
            if (s_Operations != null && s_Operations.GetType() != operations.GetType())
                throw new InvalidOperationException("Gameplay Lab launcher operations are already registered.");
            s_Operations = operations;
        }
    }

    public sealed class GameplayLauncherWindow : EditorWindow
    {
        const string BootstrapScene = "Assets/Scenes/Bootstrap.unity";

        BuildTarget m_BuildTarget;
        string m_ResourcePackageVersion = string.Empty;
        string m_MinimumClientBuildVersion = string.Empty;
        string[] m_LabVariantLabels = Array.Empty<string>();
        int m_LabVariantIndex;
        Vector2 m_Scroll;
        string m_DiagnosticSummary = string.Empty;
        double m_AutoSampleStopTime;
        bool m_LastSamplingCapturing;
        bool m_LastSamplingStarting;
        string m_LastSamplingSavedPath = string.Empty;

        [MenuItem("Tools/3C/Launcher", false, -1000)]
        public static void Open()
        {
            GameplayLauncherWindow window = GetWindow<GameplayLauncherWindow>("3C Launcher");
            window.minSize = new Vector2(620f, 640f);
            window.Show();
        }

        void OnEnable()
        {
            m_BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            RefreshGameplayLab();
            CaptureSamplingUiState();
        }

        void OnInspectorUpdate()
        {
            bool capturing = CharacterFootLandingPredictionSampler.IsCapturing;
            bool starting = CharacterFootLandingPredictionSampler.IsStartPending;
            string samplesPath = CharacterFootLandingPredictionSampler.LastSavedPath;
            if (capturing == m_LastSamplingCapturing &&
                starting == m_LastSamplingStarting &&
                string.Equals(samplesPath, m_LastSamplingSavedPath, StringComparison.Ordinal))
            {
                return;
            }
            m_LastSamplingCapturing = capturing;
            m_LastSamplingStarting = starting;
            m_LastSamplingSavedPath = samplesPath;
            Repaint();
        }

        void OnDisable()
        {
            EditorApplication.update -= TickAutoSample;
            if (m_AutoSampleStopTime != 0d &&
                (CharacterFootLandingPredictionSampler.IsStartPending ||
                 CharacterFootLandingPredictionSampler.IsCapturing))
            {
                CharacterFootLandingPredictionSampler.StopAndSaveSampling();
            }
            m_AutoSampleStopTime = 0d;
            GameplayLabFootIkKeyboardRouteDriver.Stop();
        }

        void OnGUI()
        {
            if (m_LabVariantLabels.Length == 0 && GameplayLabLauncherRegistry.Operations != null)
                RefreshGameplayLab();

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            DrawGameplayLab();
            EditorGUILayout.Space(10f);
            DrawNetworkTests();
            EditorGUILayout.Space(10f);
            DrawFormalStartup();
            EditorGUILayout.Space(10f);
            DrawEditorStartup();
            EditorGUILayout.EndScrollView();
        }

        void DrawFormalStartup()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("3. 正式启动 / Published Player", EditorStyles.boldLabel);
                ProductStartupProfile profile = AssetDatabase.LoadAssetAtPath<ProductStartupProfile>(
                    ClientBuildArtifactLayout.ProductStartupProfilePath);
                bool profileValid = DrawProductProfileStatus(profile);
                EditorGUILayout.HelpBox(
                    "Build Content publishes YooAsset files. Build Player embeds Bootstrap and built-in package metadata. Run starts the published Player.",
                    MessageType.Info);

                string clientVersion = profile && profile.TryGetClientBuildVersion(out ClientBuildVersion parsed)
                    ? parsed.ToString()
                    : "Invalid";
                bool buildIdentityValid = TryValidateBuildIdentity(profile, out _);
                EditorGUILayout.LabelField("ClientBuildVersion", clientVersion);
                m_BuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", m_BuildTarget);
                m_ResourcePackageVersion = EditorGUILayout.TextField(
                    "ResourcePackageVersion",
                    m_ResourcePackageVersion);
                m_MinimumClientBuildVersion = EditorGUILayout.TextField(
                    "MinimumClientBuildVersion",
                    m_MinimumClientBuildVersion);
                EditorGUILayout.LabelField("Content", PreviewContentPath());
                EditorGUILayout.LabelField("Player", PreviewPlayerPath(clientVersion));
                using (new EditorGUI.DisabledScope(IsBusy))
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!buildIdentityValid))
                    {
                        if (GUILayout.Button("Build Content"))
                            Execute(() => BuildCommercial(CommercialClientBuildMode.Content));
                    }
                    using (new EditorGUI.DisabledScope(!buildIdentityValid || !profileValid))
                    {
                        if (GUILayout.Button("Build Player"))
                            Execute(() => BuildCommercial(CommercialClientBuildMode.Player));
                        if (GUILayout.Button("Build Content + Player"))
                            Execute(() => BuildCommercial(CommercialClientBuildMode.ContentAndPlayer));
                    }
                    bool publishedPlayerExists = Directory.Exists(PreviewPlayerPath(clientVersion));
                    using (new EditorGUI.DisabledScope(!profileValid || !publishedPlayerExists))
                    {
                        if (GUILayout.Button("Run Published Player"))
                            Execute(RunPublishedPlayer);
                    }
                }
                if (!TryValidateBuildIdentity(profile, out string buildIdentityError))
                    EditorGUILayout.HelpBox(buildIdentityError, MessageType.None);
            }
        }

        void DrawEditorStartup()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("4. 编辑器启动 / Bootstrap Play", EditorStyles.boldLabel);
                ProductStartupProfile profile = AssetDatabase.LoadAssetAtPath<ProductStartupProfile>(
                    ClientBuildArtifactLayout.ProductStartupProfilePath);
                bool profileValid = DrawProductProfileStatus(profile);
                EditorGUILayout.HelpBox(
                    "Editor host, formal path: Bootstrap -> version policy -> cache verification -> Core -> ProductShell -> Auth -> Gameplay preload.",
                    MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Validate Configuration"))
                        Execute(CommercialStartupConfigurationValidator.ValidateAll);
                    using (new EditorGUI.DisabledScope(!profileValid || IsBusy))
                    {
                        if (GUILayout.Button("Play Bootstrap in Editor"))
                            Execute(RunProductStartup);
                    }
                }
            }
        }

        void DrawGameplayLab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("1. 单机 / Gameplay Lab", EditorStyles.boldLabel);
                IGameplayLabLauncherOperations operations = GameplayLabLauncherRegistry.Operations;
                if (operations == null)
                {
                    EditorGUILayout.HelpBox("Gameplay Lab launcher module is not registered.", MessageType.Error);
                    return;
                }
                if (m_LabVariantLabels.Length == 0)
                {
                    EditorGUILayout.HelpBox("Gameplay Lab has no valid Session Variant.", MessageType.Error);
                    if (GUILayout.Button("Refresh"))
                        RefreshGameplayLab();
                    return;
                }

                m_LabVariantIndex = EditorGUILayout.Popup(
                    "Startup Variant",
                    Mathf.Clamp(m_LabVariantIndex, 0, m_LabVariantLabels.Length - 1),
                    m_LabVariantLabels);
                EditorGUILayout.HelpBox("Local Session only. No CDN, Auth, Relay, or remote client is started.", MessageType.Info);
                using (new EditorGUI.DisabledScope(IsBusy))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Scene"))
                        Execute(operations.Open);
                    if (GUILayout.Button("Play Selected Variant"))
                        Execute(() => operations.Play(m_LabVariantIndex));
                    if (GUILayout.Button("Sync Debug Scene Assets"))
                        Execute(() =>
                        {
                            operations.SyncAssets();
                            RefreshGameplayLab();
                        });
                }
                EditorGUILayout.Space(6f);
                DrawFootLandingSampling();
            }
        }

        void DrawFootLandingSampling()
        {
            bool capturing = CharacterFootLandingPredictionSampler.IsCapturing;
            bool starting = CharacterFootLandingPredictionSampler.IsStartPending;
            string samplesPath = CharacterFootLandingPredictionSampler.LastSavedPath;
            string factsPath = CharacterFootLandingPredictionSampler.LastSavedFactsPath;
            string diagnosisDirectory =
                CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory;
            string sampleDirectory = CharacterFootLandingPredictionSampler.LastSavedDirectory;
            EditorGUILayout.LabelField(
                "Foot Landing Sampling",
                capturing ? "Recording" : starting ? "Starting" : "Idle");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling ||
                           !EditorApplication.isPlaying ||
                           capturing ||
                           starting))
                {
                    if (GUILayout.Button("Start Sampling"))
                        StartManualSample();
                }
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling ||
                           !capturing && !starting))
                {
                    if (GUILayout.Button("Stop and Save"))
                        StopManualSample();
                }
                using (new EditorGUI.DisabledScope(
                           string.IsNullOrEmpty(sampleDirectory) ||
                           !Directory.Exists(sampleDirectory)))
                {
                    if (GUILayout.Button("Reveal Sample Folder"))
                        EditorUtility.RevealInFinder(sampleDirectory);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling ||
                           !EditorApplication.isPlaying ||
                           capturing ||
                           starting ||
                           m_AutoSampleStopTime > 0d))
                {
                    if (GUILayout.Button("Auto Sample 8s"))
                        StartAutoSample();
                }
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling ||
                           EditorApplication.isPlayingOrWillChangePlaymode &&
                           !EditorApplication.isPlaying ||
                           capturing ||
                           starting ||
                           GameplayLabFootIkKeyboardRouteDriver.IsActive ||
                           GameplayLabFootIkKeyboardRouteDriver.IsPending ||
                           m_AutoSampleStopTime > 0d))
                {
                    if (GUILayout.Button("Auto Walk Stairs AD Sample"))
                        StartStairSample(GameplayLabFootIkAutomaticRouteMode.StairAdStress);
                }
                using (new EditorGUI.DisabledScope(
                           string.IsNullOrEmpty(diagnosisDirectory) ||
                           !Directory.Exists(diagnosisDirectory)))
                {
                    if (GUILayout.Button("Show Last Diagnoses"))
                        ShowLastDiagnostics();
                }
            }
            using (new EditorGUI.DisabledScope(
                       EditorApplication.isCompiling ||
                       EditorApplication.isPlayingOrWillChangePlaymode &&
                       !EditorApplication.isPlaying ||
                       capturing ||
                       starting ||
                       GameplayLabFootIkKeyboardRouteDriver.IsActive ||
                       GameplayLabFootIkKeyboardRouteDriver.IsPending ||
                       m_AutoSampleStopTime > 0d))
            {
                if (GUILayout.Button("Auto Walk Stairs Straight Sample"))
                    StartStairSample(GameplayLabFootIkAutomaticRouteMode.StairStraight);
            }
            if (GameplayLabFootIkKeyboardRouteDriver.IsActive)
            {
                EditorGUILayout.HelpBox(
                    $"{GameplayLabFootIkKeyboardRouteDriver.Mode} {GameplayLabFootIkKeyboardRouteDriver.PhaseName} lap {GameplayLabFootIkKeyboardRouteDriver.Lap}",
                    MessageType.Info);
            }
            else if (GameplayLabFootIkKeyboardRouteDriver.IsPending)
            {
                EditorGUILayout.HelpBox(
                    $"Starting Gameplay Lab for {GameplayLabFootIkKeyboardRouteDriver.Mode} sample...",
                    MessageType.Info);
            }
            if (!string.IsNullOrEmpty(GameplayLabFootIkKeyboardRouteDriver.LastDiagnosticSummary))
            {
                EditorGUILayout.HelpBox(
                    GameplayLabFootIkKeyboardRouteDriver.LastDiagnosticSummary,
                    MessageType.Info);
            }
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Foot IK Visual Validation",
                CharacterFootPlacementVisualValidation.IsEnabled ? "Enabled" : "Disabled");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(CharacterFootPlacementVisualValidation.IsEnabled))
                {
                    if (GUILayout.Button("Enable Visual Validation"))
                        ExecuteSampling(CharacterFootPlacementVisualValidation.Enable);
                }
                using (new EditorGUI.DisabledScope(!CharacterFootPlacementVisualValidation.IsEnabled))
                {
                    if (GUILayout.Button("Disable Visual Validation"))
                        ExecuteSampling(CharacterFootPlacementVisualValidation.Disable);
                }
            }
            if (CharacterFootPlacementVisualValidation.IsEnabled)
                EditorGUILayout.HelpBox(
                    "Game/Scene View shows accepted landings, Ground Path, original/corrected soles, pelvis/foot goals, FBBIK and final physical bones.",
                    MessageType.Info);
            if (!string.IsNullOrEmpty(m_DiagnosticSummary))
                EditorGUILayout.HelpBox(m_DiagnosticSummary, MessageType.Info);
            if (!string.IsNullOrEmpty(samplesPath))
            {
                EditorGUILayout.LabelField("Last Samples");
                EditorGUILayout.SelectableLabel(
                    samplesPath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            if (!string.IsNullOrEmpty(factsPath))
            {
                EditorGUILayout.LabelField("Last Facts");
                EditorGUILayout.SelectableLabel(
                    factsPath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            if (!string.IsNullOrEmpty(diagnosisDirectory))
            {
                EditorGUILayout.LabelField("Last Diagnoses");
                EditorGUILayout.SelectableLabel(
                    diagnosisDirectory,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        static void DrawNetworkTests()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("2. 双端验证 / Network Test Products", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Build uses each product's explicit scripting backend and Development + StrictMode options. Run only validates and consumes the existing artifacts.",
                    MessageType.Info);
                using (new EditorGUI.DisabledScope(IsBusy))
                {
                    DrawNetworkRow(
                        "Deterministic Rollback",
                        DeterministicRollbackNetworkTestBuildAndRun.Build,
                        DeterministicRollbackNetworkTestBuildAndRun.Run);
                    DrawNetworkRow(
                        "Unity Authority",
                        UnityAuthorityNetworkTestBuildAndRun.Build,
                        UnityAuthorityNetworkTestBuildAndRun.Run);
                    DrawNetworkRow(
                        "DotRecast Authority",
                        DotRecastAuthorityNetworkTestBuildAndRun.Build,
                        DotRecastAuthorityNetworkTestBuildAndRun.Run);
                }
            }
        }

        static void DrawNetworkRow(string label, Action build, Action run)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(190f));
                if (GUILayout.Button("Build Incremental", GUILayout.Width(120f)))
                    Execute(build);
                if (GUILayout.Button("Run", GUILayout.Width(90f)))
                    Execute(run);
            }
        }

        void RunProductStartup()
        {
            CommercialStartupConfigurationValidator.ValidateAll();
            EditorPlayModeSceneLauncher.Play(BootstrapScene);
        }

        void RunPublishedPlayer()
        {
            CommercialStartupConfigurationValidator.ValidateAll();
            ProductStartupProfile profile = AssetDatabase.LoadAssetAtPath<ProductStartupProfile>(
                ClientBuildArtifactLayout.ProductStartupProfilePath);
            if (!profile || !profile.TryGetClientBuildVersion(out ClientBuildVersion clientVersion))
                throw new InvalidOperationException("ProductStartupProfile has no valid ClientBuildVersion.");
            CommercialClientRunWorkflow.Run(m_BuildTarget, clientVersion.ToString());
        }

        void BuildCommercial(CommercialClientBuildMode mode)
        {
            var request = new CommercialClientBuildRequest(
                m_BuildTarget,
                m_ResourcePackageVersion,
                m_MinimumClientBuildVersion,
                mode);
            CommercialClientBuildResult result = CommercialClientBuildWorkflow.Build(request);
            string message = $"Content: {result.ContentPath}\nPlayer: {result.PlayerPath}";
            Debug.Log($"Commercial client build completed. {message}");
            EditorUtility.DisplayDialog("3C Launcher", message, "OK");
        }

        void RefreshGameplayLab()
        {
            IGameplayLabLauncherOperations operations = GameplayLabLauncherRegistry.Operations;
            if (operations == null)
            {
                m_LabVariantLabels = Array.Empty<string>();
                m_LabVariantIndex = 0;
                return;
            }
            try
            {
                GameplayLabLauncherState state = operations.ReadState();
                m_LabVariantLabels = state.VariantLabels;
                m_LabVariantIndex = state.SelectedVariantIndex;
            }
            catch (Exception exception)
            {
                m_LabVariantLabels = Array.Empty<string>();
                m_LabVariantIndex = 0;
                Debug.LogException(exception);
            }
        }

        string PreviewContentPath()
        {
            try
            {
                return ClientBuildArtifactLayout.GetContentVersionRoot(m_BuildTarget, m_ResourcePackageVersion);
            }
            catch
            {
                return "Waiting for ResourcePackageVersion";
            }
        }

        string PreviewPlayerPath(string clientVersion)
        {
            try
            {
                return ClientBuildArtifactLayout.GetPlayerVersionRoot(m_BuildTarget, clientVersion);
            }
            catch
            {
                return "Waiting for a valid ClientBuildVersion";
            }
        }

        bool TryValidateBuildIdentity(ProductStartupProfile profile, out string error)
        {
            if (!profile || !profile.TryGetClientBuildVersion(out ClientBuildVersion clientVersion))
            {
                error = "Content build requires a valid ClientBuildVersion in ProductStartupProfile.";
                return false;
            }
            try
            {
                ClientBuildArtifactLayout.ValidateTarget(m_BuildTarget);
                ClientBuildArtifactLayout.ValidateIdentity(m_ResourcePackageVersion, nameof(m_ResourcePackageVersion));
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            if (!ClientBuildVersion.TryParse(m_MinimumClientBuildVersion, out ClientBuildVersion minimumVersion))
            {
                error = "MinimumClientBuildVersion must be a three-part or four-part non-negative version.";
                return false;
            }
            if (minimumVersion > clientVersion)
            {
                error = "MinimumClientBuildVersion cannot be greater than ClientBuildVersion.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        static bool DrawProductProfileStatus(ProductStartupProfile profile)
        {
            ProductStartupErrorCode errorCode = ProductStartupErrorCode.ProfileMissing;
            string safeError = "ProductStartupProfile is missing.";
            bool valid = profile && profile.TryValidate(out errorCode, out safeError);
            if (!profile)
                EditorGUILayout.HelpBox("ProductStartupProfile is missing.", MessageType.Error);
            else if (!valid)
                EditorGUILayout.HelpBox($"{errorCode}: {safeError}", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("HTTPS ResourceEndpoint and WSS AuthEndpoint are configured.", MessageType.Info);
            return valid;
        }

        static bool IsBusy => EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode;

        static void Execute(Action action)
        {
            if (action == null || IsBusy)
                return;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("3C Launcher", exception.Message, "OK");
            }
        }

        static void ExecuteSampling(Action action)
        {
            if (action == null || EditorApplication.isCompiling)
                return;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("3C Launcher", exception.Message, "OK");
            }
        }

        void StartAutoSample()
        {
            try
            {
                CharacterFootLandingPredictionSampler.StartSampling();
                m_AutoSampleStopTime = -1d;
                m_DiagnosticSummary = "Waiting for Gameplay Lab player...";
                EditorApplication.update -= TickAutoSample;
                EditorApplication.update += TickAutoSample;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("3C Launcher", exception.Message, "OK");
            }
        }

        void StartStairSample(GameplayLabFootIkAutomaticRouteMode mode)
        {
            IGameplayLabLauncherOperations operations = GameplayLabLauncherRegistry.Operations;
            if (operations == null)
            {
                EditorUtility.DisplayDialog("3C Launcher", "Gameplay Lab launcher module is not registered.", "OK");
                return;
            }
            try
            {
                if (EditorApplication.isPlaying)
                {
                    GameplayLabFootIkKeyboardRouteDriver.Start(mode);
                    return;
                }
                GameplayLabFootIkKeyboardRouteDriver.ArmPending(mode);
                operations.Play(m_LabVariantIndex);
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    GameplayLabFootIkKeyboardRouteDriver.ClearPending();
            }
            catch (Exception exception)
            {
                GameplayLabFootIkKeyboardRouteDriver.ClearPending();
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("3C Launcher", exception.Message, "OK");
            }
        }

        void TickAutoSample()
        {
            if (m_AutoSampleStopTime == 0d)
                return;
            if (m_AutoSampleStopTime < 0d)
            {
                if (CharacterFootLandingPredictionSampler.IsStartPending)
                    return;
                if (!CharacterFootLandingPredictionSampler.IsCapturing)
                {
                    EditorApplication.update -= TickAutoSample;
                    m_AutoSampleStopTime = 0d;
                    m_DiagnosticSummary = CharacterFootLandingPredictionSampler.LastStartFailure;
                    Repaint();
                    return;
                }
                m_AutoSampleStopTime = EditorApplication.timeSinceStartup + 8d;
                m_DiagnosticSummary = "Auto sampling 8s... walk the stairs now.";
                Repaint();
                return;
            }
            if (EditorApplication.timeSinceStartup < m_AutoSampleStopTime)
            {
                return;
            }
            EditorApplication.update -= TickAutoSample;
            m_AutoSampleStopTime = 0d;
            GameplayLabFootIkKeyboardRouteDriver.Stop();
            ExecuteSampling(CharacterFootLandingPredictionSampler.StopAndSaveSampling);
            ShowLastDiagnostics();
            Repaint();
        }

        void ShowLastDiagnostics()
        {
            m_DiagnosticSummary =
                CharacterFootLandingPredictionSampler.LastDiagnosticSummary;
            Debug.Log(
                $"Foot Landing Diagnostics " +
                $"Samples={CharacterFootLandingPredictionSampler.LastSavedPath}, " +
                $"Facts={CharacterFootLandingPredictionSampler.LastSavedFactsPath}, " +
                $"Diagnoses={CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory}, " +
                $"Summary={m_DiagnosticSummary}");
            Repaint();
        }

        void StartManualSample()
        {
            m_DiagnosticSummary = string.Empty;
            ExecuteSampling(CharacterFootLandingPredictionSampler.StartSampling);
            Repaint();
        }

        void StopManualSample()
        {
            ExecuteSampling(CharacterFootLandingPredictionSampler.StopAndSaveSampling);
            if (!string.IsNullOrEmpty(CharacterFootLandingPredictionSampler.LastSavedPath))
                ShowLastDiagnostics();
        }

        void CaptureSamplingUiState()
        {
            m_LastSamplingCapturing = CharacterFootLandingPredictionSampler.IsCapturing;
            m_LastSamplingStarting = CharacterFootLandingPredictionSampler.IsStartPending;
            m_LastSamplingSavedPath = CharacterFootLandingPredictionSampler.LastSavedPath;
        }
    }
}
