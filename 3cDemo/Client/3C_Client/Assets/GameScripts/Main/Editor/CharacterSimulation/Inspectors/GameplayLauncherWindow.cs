using System;
using System.IO;
using System.Linq;
using ThirdPerson.ProductStartup;
using ThirdPersonCharacter.Editor.ProductBuild;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonPerformance.Instrumentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public sealed class GameplayLauncherWindow : EditorWindow
    {
        const string BootstrapScene = "Assets/Scenes/Bootstrap.unity";
        static readonly string[] s_FootSamplerLabels = { "Core", "Full" };

        BuildTarget m_BuildTarget;
        string m_ResourcePackageVersion = string.Empty;
        string m_MinimumClientBuildVersion = string.Empty;
        Vector2 m_Scroll;
        string m_DiagnosticSummary = string.Empty;
        double m_AutoSampleStopTime;
        bool m_LastSamplingCapturing;
        bool m_LastSamplingStarting;
        bool m_LastSamplingAnalyzing;
        string m_LastSamplingSavedPath = string.Empty;
        string m_LastSamplingManifestPath = string.Empty;
        string m_LastSamplingDirectory = string.Empty;
        string m_LastSamplingReportPath = string.Empty;
        bool m_LastEditorBusy;
        bool m_LastEditorPlaying;
        bool m_LastEditorCompiling;
        bool m_LastSamplingAvailable;
        bool m_LastAnalysisAvailable;
        string m_LastPerformanceStatus = string.Empty;
        readonly NetworkTestControlCenterGui m_NetworkTestControlCenter = new NetworkTestControlCenterGui();
        [SerializeField]
        string m_SelectedInputTraceId = string.Empty;
        CharacterFixedInputTraceSummary[] m_InputTraces = Array.Empty<CharacterFixedInputTraceSummary>();
        string[] m_InputTraceLabels = Array.Empty<string>();
        int m_SelectedInputTraceIndex = -1;
        string m_LastObservedInputTraceId = string.Empty;
        string m_InputTraceCatalogError = string.Empty;
        string m_SamplingTargetError = string.Empty;

        string SelectedInputTracePath => m_SelectedInputTraceIndex < 0
            ? string.Empty
            : m_InputTraces[m_SelectedInputTraceIndex].Path;

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
            CaptureSamplingUiState();
            m_LastPerformanceStatus = ThirdPersonPerformanceCaptureWorkflow.Status;
            RefreshInputTraces();
        }

        void OnInspectorUpdate()
        {
            string previousTargetError = m_SamplingTargetError;
            m_SamplingTargetError = string.Empty;
            if (EditorApplication.isPlaying && !EditorApplication.isCompiling)
            {
                try { CharacterPoseSamplingTarget.Require("fixed-player"); }
                catch (Exception exception) { m_SamplingTargetError = exception.Message; }
            }
            if (previousTargetError != m_SamplingTargetError)
                Repaint();
            bool capturing = CharacterFootDiagnosticSampling.IsCapturing;
            bool finalizing = CharacterGameplayDiagnosticCapture.IsFinalizing || CharacterFootDiagnosticSampling.IsFinalizing;
            bool analyzing = CharacterFootDiagnosticSampling.IsAnalyzing;
            string samplesPath = CharacterFootDiagnosticSampling.LastSavedPath;
            string manifestPath = CharacterFootDiagnosticSampling.LastManifestPath;
            string sampleDirectory = CharacterFootDiagnosticSampling.LastSavedDirectory;
            string reportPath = CharacterFootDiagnosticSampling.LastReportPath;
            bool editorBusy = IsBusy;
            bool editorPlaying = EditorApplication.isPlaying;
            bool editorCompiling = EditorApplication.isCompiling;
            bool samplingAvailable = CharacterFootDiagnosticSampling.IsAvailable;
            bool analysisAvailable = CharacterFootDiagnosticSampling.IsAnalysisAvailable;
            string performanceStatus = ThirdPersonPerformanceCaptureWorkflow.Status;
            bool networkChanged = m_NetworkTestControlCenter.Poll();
            bool inputTraceChanged = PollInputTraces();
            if (capturing == m_LastSamplingCapturing &&
                finalizing == m_LastSamplingStarting &&
                analyzing == m_LastSamplingAnalyzing &&
                string.Equals(samplesPath, m_LastSamplingSavedPath, StringComparison.Ordinal) &&
                string.Equals(manifestPath, m_LastSamplingManifestPath, StringComparison.Ordinal) &&
                string.Equals(sampleDirectory, m_LastSamplingDirectory, StringComparison.Ordinal) &&
                string.Equals(reportPath, m_LastSamplingReportPath, StringComparison.Ordinal) &&
                editorBusy == m_LastEditorBusy &&
                editorPlaying == m_LastEditorPlaying &&
                editorCompiling == m_LastEditorCompiling &&
                samplingAvailable == m_LastSamplingAvailable &&
                analysisAvailable == m_LastAnalysisAvailable &&
                string.Equals(performanceStatus, m_LastPerformanceStatus, StringComparison.Ordinal) &&
                !inputTraceChanged &&
                !networkChanged)
            {
                return;
            }
            m_LastSamplingCapturing = capturing;
            m_LastSamplingStarting = finalizing;
            m_LastSamplingAnalyzing = analyzing;
            m_LastSamplingSavedPath = samplesPath;
            m_LastSamplingManifestPath = manifestPath;
            m_LastSamplingDirectory = sampleDirectory;
            m_LastSamplingReportPath = reportPath;
            m_LastEditorBusy = editorBusy;
            m_LastEditorPlaying = editorPlaying;
            m_LastEditorCompiling = editorCompiling;
            m_LastSamplingAvailable = samplingAvailable;
            m_LastAnalysisAvailable = analysisAvailable;
            m_LastPerformanceStatus = performanceStatus;
            Repaint();
        }

        void OnDisable()
        {
            EditorApplication.update -= TickAutoSample;
            EditorApplication.delayCall -= RefreshInputTraces;
            if (m_AutoSampleStopTime != 0d &&
                CharacterFootDiagnosticSampling.IsCapturing)
            {
                CharacterGameplayDiagnosticCapture.StopAndSave();
            }
            m_AutoSampleStopTime = 0d;
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            DrawPerformanceCapture();
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
                EditorGUILayout.LabelField("4. 正式启动 / Published Player", EditorStyles.boldLabel);
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
                EditorGUILayout.LabelField("5. 编辑器启动 / Bootstrap Play", EditorStyles.boldLabel);
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

        void DrawFootLandingSampling()
        {
            bool capturing = CharacterFootDiagnosticSampling.IsCapturing;
            bool finalizing = CharacterGameplayDiagnosticCapture.IsFinalizing || CharacterFootDiagnosticSampling.IsFinalizing;
            string samplesPath = CharacterFootDiagnosticSampling.LastSavedPath;
            string manifestPath = CharacterFootDiagnosticSampling.LastManifestPath;
            string sampleDirectory = CharacterFootDiagnosticSampling.LastSavedDirectory;
            bool captureCompilation = CharacterDiagnosticCompilationMode.IsFootCaptureEnabled;
            EditorGUILayout.LabelField(
                "Foot Diagnostic Compilation",
                captureCompilation ? "Capture" : "Disabled");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(IsBusy || captureCompilation))
                {
                    if (GUILayout.Button("Enable Foot Capture"))
                        Execute(CharacterDiagnosticCompilationMode.EnableFootCapture);
                }
                using (new EditorGUI.DisabledScope(
                           IsBusy || !captureCompilation || capturing || finalizing))
                {
                    if (GUILayout.Button("Disable Foot Capture"))
                        Execute(CharacterDiagnosticCompilationMode.DisableFootCapture);
                }
            }
            EditorGUILayout.LabelField(
                "Gameplay Sampling / Animation + Foot",
                capturing ? "Recording" : finalizing ? "Finalizing" : "Idle");
            if (EditorApplication.isCompiling)
                EditorGUILayout.HelpBox("正在编译，完成后采样入口会自动更新。", MessageType.Info);
            else if (!captureCompilation)
                EditorGUILayout.HelpBox(EditorApplication.isPlayingOrWillChangePlaymode
                    ? "采样尚未启用。请先退出 Play，再点 Enable Foot Capture；运行中不能修改编译开关。"
                    : "先点 Enable Foot Capture，等编译完成，再记录输入或启动采样。", MessageType.Info);
            else if (!CharacterGameplayDiagnosticCapture.IsAvailable)
                EditorGUILayout.HelpBox("采样开关已启用，但 Foot 或 Presentation 工作流未注册。请检查 Console 编译错误。", MessageType.Error);
            else if (!string.IsNullOrEmpty(m_SamplingTargetError))
                EditorGUILayout.HelpBox(m_SamplingTargetError, MessageType.Warning);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "先启动 Fixed 会话。采样只在 Play Mode 开始；已完成的采样可在编辑模式下打开目录或分析。",
                    MessageType.None);
            }
            int samplerIndex = string.Equals(
                CharacterFootDiagnosticSampling.SelectedSamplerId,
                CharacterFootDiagnosticSampling.CoreSamplerId,
                StringComparison.Ordinal)
                ? 0
                : 1;
            using (new EditorGUI.DisabledScope(
                       !CharacterGameplayDiagnosticCapture.IsAvailable ||
                       capturing ||
                       finalizing))
            {
                int selectedSamplerIndex = EditorGUILayout.Popup(
                    "Sampler",
                    samplerIndex,
                    s_FootSamplerLabels);
                if (selectedSamplerIndex != samplerIndex)
                {
                    CharacterFootDiagnosticSampling.SelectSampler(
                        selectedSamplerIndex == 0
                            ? CharacterFootDiagnosticSampling.CoreSamplerId
                            : CharacterFootDiagnosticSampling.FullSamplerId);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling ||
                           !EditorApplication.isPlaying ||
                           !string.IsNullOrEmpty(m_SamplingTargetError) ||
                           !CharacterGameplayDiagnosticCapture.IsAvailable ||
                           capturing ||
                           finalizing))
                {
                    if (GUILayout.Button("Start Sampling"))
                        StartManualSample();
                }
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling ||
                           !capturing))
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
                           !string.IsNullOrEmpty(m_SamplingTargetError) ||
                           !CharacterGameplayDiagnosticCapture.IsAvailable ||
                           capturing ||
                           finalizing ||
                           m_AutoSampleStopTime > 0d))
                {
                    if (GUILayout.Button("Auto Sample 8s"))
                        StartAutoSample();
                }
            }
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Foot Diagnosis",
                CharacterFootDiagnosticSampling.IsAnalyzing
                    ? "Analyzing"
                    : "Idle");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           !CharacterFootDiagnosticSampling.IsAnalysisAvailable ||
                           CharacterFootDiagnosticSampling.IsAnalyzing ||
                           finalizing ||
                           !File.Exists(manifestPath)))
                {
                    if (GUILayout.Button("Analyze Last Capture"))
                        ExecuteSampling(
                            CharacterFootDiagnosticSampling
                                .AnalyzeLastCapture);
                }
                using (new EditorGUI.DisabledScope(
                           !CharacterFootDiagnosticSampling.IsAnalysisAvailable ||
                           CharacterFootDiagnosticSampling.IsAnalyzing ||
                           finalizing))
                {
                    if (GUILayout.Button("Analyze Existing Capture"))
                    {
                        string selectedManifest = EditorUtility.OpenFilePanel(
                            "Select Foot Capability Manifest",
                            Path.GetFullPath(Path.Combine(
                                Application.dataPath,
                                "..",
                                "Diagnostics")),
                            "json");
                        if (!string.IsNullOrEmpty(selectedManifest))
                        {
                            ExecuteSampling(() =>
                                CharacterFootDiagnosticSampling
                                    .AnalyzeExistingCapture(
                                        selectedManifest));
                        }
                    }
                }
                using (new EditorGUI.DisabledScope(
                           !File.Exists(
                               CharacterFootDiagnosticSampling
                                   .LastReportPath)))
                {
                    if (GUILayout.Button("Open Last Report"))
                    {
                        ExecuteSampling(
                            CharacterFootDiagnosticSampling.OpenLastReport);
                    }
                }
            }
            if (!string.IsNullOrEmpty(
                    CharacterFootDiagnosticSampling.LastAnalysisFailure))
            {
                EditorGUILayout.HelpBox(
                    CharacterFootDiagnosticSampling.LastAnalysisFailure,
                    MessageType.Error);
            }
            else if (!string.IsNullOrEmpty(
                         CharacterFootDiagnosticSampling.LastReportPath))
            {
                EditorGUILayout.SelectableLabel(
                    CharacterFootDiagnosticSampling.LastReportPath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            if (!string.IsNullOrEmpty(
                    CharacterFootDiagnosticSampling.LastQualityScorePath))
            {
                EditorGUILayout.LabelField("Quality Score");
                EditorGUILayout.SelectableLabel(
                    CharacterFootDiagnosticSampling.LastQualityScorePath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            DrawFixedInputTrace();
            EditorGUILayout.Space(4f);
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
            if (!string.IsNullOrEmpty(manifestPath))
            {
                EditorGUILayout.LabelField("Last Manifest");
                EditorGUILayout.SelectableLabel(
                    manifestPath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        void DrawFixedInputTrace()
        {
            var presentation = CharacterGameplayDiagnosticCapture.Presentation;
            if (presentation != null)
            {
                EditorGUILayout.LabelField("Animation / Action / Camera",
                    presentation.IsCapturing ? "Recording" : presentation.IsFinalizing ? "Finalizing" : "Idle");
                if (!string.IsNullOrEmpty(presentation.LastFailure))
                    EditorGUILayout.HelpBox(presentation.LastFailure, MessageType.Error);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!Directory.Exists(presentation.LastSavedDirectory)))
                        if (GUILayout.Button("Reveal Animation Samples"))
                            EditorUtility.RevealInFinder(presentation.LastSavedDirectory);
                    bool hasAnalysis = DiagnosticAnalysisWorkflowRegistry.TryGet(
                        CharacterGameplayDiagnosticCapture.PresentationCapabilityId, out var analysis);
                    using (new EditorGUI.DisabledScope(!hasAnalysis || analysis.IsAnalyzing ||
                               presentation.IsCapturing || presentation.IsFinalizing || !File.Exists(presentation.LastManifestPath)))
                        if (GUILayout.Button("Analyze Animation Samples"))
                            ExecuteSampling(analysis.AnalyzeLast);
                    using (new EditorGUI.DisabledScope(!hasAnalysis || !File.Exists(analysis.LastReportPath)))
                        if (GUILayout.Button("Open Animation Report"))
                            ExecuteSampling(analysis.OpenLastReport);
                }
            }
            bool recording = CharacterFixedInputTraceWorkflow.IsRecording;
            bool replaying = CharacterFixedInputTraceWorkflow.IsReplaying;
            bool pending = CharacterFixedInputTraceWorkflow.IsPending;
            bool sampling = CharacterGameplayDiagnosticCapture.HasActiveCapture;
            string state = recording
                ? "Recording"
                : replaying
                    ? "Replaying"
                    : pending
                        ? $"Starting {CharacterFixedInputTraceWorkflow.PendingOperation}"
                        : "Idle";
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Fixed Input Trace", state);
            DrawInputTraceSelection(recording, replaying, pending, sampling);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling || recording || replaying || pending || sampling))
                {
                    if (GUILayout.Button("Record Input"))
                        ExecuteSampling(CharacterFixedInputTraceWorkflow.StartRecording);
                }
                using (new EditorGUI.DisabledScope(!recording))
                {
                    if (GUILayout.Button("Stop and Save Input"))
                        ExecuteSampling(() => CharacterFixedInputTraceWorkflow.StopAndSaveRecording());
                }
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling || recording || replaying || pending || sampling ||
                           m_SelectedInputTraceIndex < 0))
                {
                    if (GUILayout.Button("Replay Selected"))
                        ReplaySelectedInputTrace(false);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling || recording || replaying || pending || sampling ||
                           !CharacterGameplayDiagnosticCapture.IsAvailable ||
                           m_SelectedInputTraceIndex < 0))
                {
                    if (GUILayout.Button("Replay Selected + Diagnostics"))
                        ReplaySelectedInputTrace(true);
                }
                using (new EditorGUI.DisabledScope(!replaying && !pending))
                {
                    if (GUILayout.Button("Stop Replay"))
                        ExecuteSampling(CharacterFixedInputTraceWorkflow.Stop);
                }
                if (GUILayout.Button("Reveal Input Traces"))
                    ExecuteSampling(CharacterFixedInputTraceWorkflow.RevealTraceDirectory);
            }
            EditorGUILayout.HelpBox(
                "Replay owns canonical character input only. Camera controls remain live for observation.",
                MessageType.None);
            if (!string.IsNullOrEmpty(CharacterFixedInputTraceWorkflow.LastFailure))
                EditorGUILayout.HelpBox(CharacterFixedInputTraceWorkflow.LastFailure, MessageType.Error);
            else if (!string.IsNullOrEmpty(CharacterFixedInputTraceWorkflow.LastStatus))
                EditorGUILayout.HelpBox(CharacterFixedInputTraceWorkflow.LastStatus, MessageType.Info);
            if (m_SelectedInputTraceIndex >= 0)
            {
                EditorGUILayout.LabelField("Selected Input Trace");
                EditorGUILayout.SelectableLabel(
                    SelectedInputTracePath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        void DrawNetworkTests()
        {
            m_NetworkTestControlCenter.Draw(IsBusy);
        }

        void DrawPerformanceCapture()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("2. 性能诊断 / Performance Capture", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Run the non-elevated Smoke gate, then the exact Replay gate. Capture remains locked until both Completed gates match the selected Scenario and Player; only Capture elevates and starts WPR after Warmup.",
                    MessageType.Info);
                bool toolchainValid = ThirdPersonPerformanceCaptureWorkflow.TryValidateToolchain(out string toolchainStatus);
                EditorGUILayout.LabelField("Toolchain", toolchainValid ? "Configured" : "Invalid");
                if (!toolchainValid)
                    EditorGUILayout.HelpBox(toolchainStatus, MessageType.Warning);
                DrawPerformancePath("Toolchain Definition", ThirdPersonPerformanceCaptureWorkflow.ToolchainPath);
                DrawPerformancePath("Input Recording", SelectedInputTracePath);
                DrawPerformancePath("Scenario", ThirdPersonPerformanceCaptureWorkflow.ScenarioPath);
                DrawPerformancePath("Capture Profile", ThirdPersonPerformanceCaptureWorkflow.CaptureProfilePath);
                DrawPerformancePath("Budget", ThirdPersonPerformanceCaptureWorkflow.BudgetPath);
                DrawPerformancePath("Player", ThirdPersonPerformanceCaptureWorkflow.PlayerManifestPath);
                DrawPerformancePath("Smoke Gate", ThirdPersonPerformanceCaptureWorkflow.LastSmokeManifestPath);
                DrawPerformancePath("Replay Gate", ThirdPersonPerformanceCaptureWorkflow.LastReplayManifestPath);
                DrawPerformancePath("Baseline", ThirdPersonPerformanceCaptureWorkflow.BaselineManifestPath);
                EditorGUILayout.LabelField("Runtime", ThirdPersonPerformanceCaptureWorkflow.FixedRuntimeId);
                EditorGUILayout.LabelField("Run", ThirdPersonPerformanceCaptureWorkflow.Status);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(IsBusy || ThirdPersonPerformanceCaptureWorkflow.IsRunRunning))
                    {
                        if (GUILayout.Button("Configure Toolchain"))
                            SchedulePerformance(ThirdPersonPerformanceCaptureWorkflow.ConfigureToolchain);
                        using (new EditorGUI.DisabledScope(
                                   m_SelectedInputTraceIndex < 0))
                        {
                            if (GUILayout.Button("Publish Scenario"))
                            {
                                string tracePath = SelectedInputTracePath;
                                SchedulePerformance(() => ThirdPersonPerformanceCaptureWorkflow.PublishScenario(
                                    ThirdPersonPerformanceCaptureWorkflow.FixedRuntimeId,
                                    tracePath));
                            }
                        }
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(
                               IsBusy ||
                               ThirdPersonPerformanceCaptureWorkflow.IsRunRunning ||
                               string.IsNullOrEmpty(ThirdPersonPerformanceCaptureWorkflow.ScenarioPath) ||
                               !File.Exists(ThirdPersonPerformanceCaptureWorkflow.ScenarioPath)))
                    {
                        if (GUILayout.Button("Build MarkerOnly Player"))
                        {
                            SchedulePerformance(() => ThirdPersonPerformanceCaptureWorkflow.BuildPlayer(
                                ThirdPersonPerformanceCaptureWorkflow.FixedRuntimeId,
                                PerformanceInstrumentationMode.MarkerOnly));
                        }
                        if (GUILayout.Button("Build Span Player"))
                        {
                            SchedulePerformance(() => ThirdPersonPerformanceCaptureWorkflow.BuildPlayer(
                                ThirdPersonPerformanceCaptureWorkflow.FixedRuntimeId,
                                PerformanceInstrumentationMode.Span));
                        }
                    }
                    using (new EditorGUI.DisabledScope(!ThirdPersonPerformanceCaptureWorkflow.IsRunRunning))
                    {
                        if (GUILayout.Button("Cancel Owned Run"))
                            Execute(ThirdPersonPerformanceCaptureWorkflow.CancelOwnedRun);
                    }
                }
                bool playerReady = !IsBusy && !ThirdPersonPerformanceCaptureWorkflow.IsRunRunning &&
                                   !string.IsNullOrEmpty(ThirdPersonPerformanceCaptureWorkflow.PlayerManifestPath) &&
                                   File.Exists(ThirdPersonPerformanceCaptureWorkflow.PlayerManifestPath);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!playerReady))
                    {
                        if (GUILayout.Button("1. Smoke"))
                            SchedulePerformance(ThirdPersonPerformanceCaptureWorkflow.StartSmoke);
                    }
                    using (new EditorGUI.DisabledScope(
                               !playerReady || !ThirdPersonPerformanceCaptureWorkflow.SmokeGateReady))
                    {
                        if (GUILayout.Button("2. Replay"))
                            SchedulePerformance(ThirdPersonPerformanceCaptureWorkflow.StartReplay);
                    }
                    using (new EditorGUI.DisabledScope(
                               !playerReady || !toolchainValid ||
                               !ThirdPersonPerformanceCaptureWorkflow.SmokeGateReady ||
                               !ThirdPersonPerformanceCaptureWorkflow.ReplayGateReady))
                    {
                        if (GUILayout.Button(string.IsNullOrEmpty(ThirdPersonPerformanceCaptureWorkflow.BaselineManifestPath)
                                ? "3. Capture"
                                : "3. Capture + Compare"))
                            SchedulePerformance(ThirdPersonPerformanceCaptureWorkflow.StartCapture);
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(ThirdPersonPerformanceCaptureWorkflow.IsRunRunning))
                    {
                        if (GUILayout.Button("Select Baseline"))
                            Execute(ThirdPersonPerformanceCaptureWorkflow.SelectBaseline);
                        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(ThirdPersonPerformanceCaptureWorkflow.BaselineManifestPath)))
                        {
                            if (GUILayout.Button("Clear Baseline"))
                                Execute(ThirdPersonPerformanceCaptureWorkflow.ClearBaseline);
                        }
                    }
                    using (new EditorGUI.DisabledScope(!File.Exists(ThirdPersonPerformanceCaptureWorkflow.LastCaptureManifestPath)))
                    {
                        if (GUILayout.Button("Reveal Capture"))
                            Execute(ThirdPersonPerformanceCaptureWorkflow.RevealLastCapture);
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(!File.Exists(ThirdPersonPerformanceCaptureWorkflow.LastCaptureManifestPath)))
                {
                    if (GUILayout.Button("Open Summary"))
                        Execute(ThirdPersonPerformanceCaptureWorkflow.OpenSummary);
                    if (GUILayout.Button("Open Comparison"))
                        Execute(ThirdPersonPerformanceCaptureWorkflow.OpenComparison);
                    if (GUILayout.Button("Open Unity Profiler"))
                        Execute(ThirdPersonPerformanceCaptureWorkflow.OpenUnityProfiler);
                    if (GUILayout.Button("Open WPA"))
                        Execute(ThirdPersonPerformanceCaptureWorkflow.OpenWpa);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(IsBusy || ThirdPersonPerformanceCaptureWorkflow.IsRunRunning || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        if (GUILayout.Button("分析重复采集…"))
                            SchedulePerformance(ThirdPersonPerformanceCaptureWorkflow.SelectAnalysisRequestAndRun);
                    }
                    using (new EditorGUI.DisabledScope(!File.Exists(ThirdPersonPerformanceCaptureWorkflow.LastAnalysisPath)))
                    {
                        if (GUILayout.Button("打开重复分析报告"))
                            Execute(ThirdPersonPerformanceCaptureWorkflow.OpenAnalysis);
                    }
                }
                DrawFootLandingSampling();
            }
        }

        void DrawInputTraceSelection(
            bool recording,
            bool replaying,
            bool pending,
            bool sampling)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           EditorApplication.isCompiling || recording || replaying || pending || sampling ||
                           m_InputTraces.Length == 0))
                {
                    int selected = EditorGUILayout.Popup(
                        "Recording",
                        m_SelectedInputTraceIndex,
                        m_InputTraceLabels);
                    if (selected >= 0 && selected != m_SelectedInputTraceIndex)
                    {
                        m_SelectedInputTraceIndex = selected;
                        m_SelectedInputTraceId = m_InputTraces[selected].TraceId;
                    }
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                {
                    EditorApplication.delayCall -= RefreshInputTraces;
                    EditorApplication.delayCall += RefreshInputTraces;
                }
            }
            if (!string.IsNullOrEmpty(m_InputTraceCatalogError))
                EditorGUILayout.HelpBox(m_InputTraceCatalogError, MessageType.Error);
            else if (m_InputTraces.Length == 0)
                EditorGUILayout.HelpBox("No saved input recordings.", MessageType.Info);
            else if (m_SelectedInputTraceIndex < 0)
                EditorGUILayout.HelpBox("The selected recording is no longer available. Select another recording.", MessageType.Warning);
        }

        void ReplaySelectedInputTrace(bool withDiagnostics)
        {
            string traceId = m_InputTraces[m_SelectedInputTraceIndex].TraceId;
            ExecuteSampling(() =>
            {
                if (withDiagnostics)
                    CharacterFixedInputTraceWorkflow.ReplayTraceWithDiagnostics(traceId);
                else
                    CharacterFixedInputTraceWorkflow.ReplayTrace(traceId);
            });
        }

        bool PollInputTraces()
        {
            string traceId = CharacterFixedInputTraceWorkflow.LastTraceId;
            if (string.Equals(traceId, m_LastObservedInputTraceId, StringComparison.Ordinal))
                return false;
            m_SelectedInputTraceId = traceId;
            RefreshInputTraces();
            return true;
        }

        void RefreshInputTraces()
        {
            m_LastObservedInputTraceId = CharacterFixedInputTraceWorkflow.LastTraceId;
            try
            {
                m_InputTraces = CharacterFixedInputTraceWorkflow.ListTraces().ToArray();
                m_InputTraceLabels = new string[m_InputTraces.Length];
                m_SelectedInputTraceIndex = -1;
                for (int i = 0; i < m_InputTraces.Length; i++)
                {
                    CharacterFixedInputTraceSummary trace = m_InputTraces[i];
                    double seconds = (double)trace.FrameCount / trace.TickRate;
                    m_InputTraceLabels[i] =
                        $"{trace.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} | {seconds:0.0}s | {trace.FrameCount} frames";
                    if (string.Equals(trace.TraceId, m_SelectedInputTraceId, StringComparison.Ordinal))
                        m_SelectedInputTraceIndex = i;
                }
                if (string.IsNullOrEmpty(m_SelectedInputTraceId) && m_InputTraces.Length > 0)
                {
                    m_SelectedInputTraceIndex = 0;
                    m_SelectedInputTraceId = m_InputTraces[0].TraceId;
                }
                m_InputTraceCatalogError = string.Empty;
            }
            catch (Exception exception)
            {
                m_InputTraces = Array.Empty<CharacterFixedInputTraceSummary>();
                m_InputTraceLabels = Array.Empty<string>();
                m_SelectedInputTraceIndex = -1;
                m_InputTraceCatalogError = exception.Message;
            }
            Repaint();
        }

        static void DrawPerformancePath(string label, string path)
        {
            EditorGUILayout.LabelField(label);
            EditorGUILayout.SelectableLabel(
                string.IsNullOrEmpty(path) ? "Not selected" : path,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        static void SchedulePerformance(Action action)
        {
            if (action == null || IsBusy)
                return;
            EditorApplication.delayCall += () => Execute(action);
        }

        void RunProductStartup()
        {
            CommercialStartupConfigurationValidator.ValidateAll();
            EditorPlayModeSceneLaunchResult result = EditorPlayModeSceneLauncher.Start(
                new EditorPlayModeSceneLaunchRequest(BootstrapScene, "product-startup"));
            if (result.Code != EditorPlayModeSceneLaunchResultCode.Started &&
                result.Code != EditorPlayModeSceneLaunchResultCode.Cancelled)
                throw new InvalidOperationException(result.Message);
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
                CharacterGameplayDiagnosticCapture.StartManual();
                m_AutoSampleStopTime =
                    EditorApplication.timeSinceStartup + 8d;
                m_DiagnosticSummary =
                    "Auto sampling 8s... walk the stairs now.";
                EditorApplication.update -= TickAutoSample;
                EditorApplication.update += TickAutoSample;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("3C Launcher", exception.Message, "OK");
            }
        }

        void TickAutoSample()
        {
            if (m_AutoSampleStopTime == 0d)
                return;
            if (EditorApplication.timeSinceStartup < m_AutoSampleStopTime)
            {
                return;
            }
            EditorApplication.update -= TickAutoSample;
            m_AutoSampleStopTime = 0d;
            ExecuteSampling(CharacterGameplayDiagnosticCapture.StopAndSave);
            ShowLastDiagnostics();
            Repaint();
        }

        void ShowLastDiagnostics()
        {
            m_DiagnosticSummary =
                string.IsNullOrEmpty(CharacterFootDiagnosticSampling.LastFailure)
                    ? CharacterFootDiagnosticSampling.LastManifestPath
                    : CharacterFootDiagnosticSampling.LastFailure;
            Debug.Log(
                $"Foot Landing Diagnostics " +
                $"Samples={CharacterFootDiagnosticSampling.LastSavedPath}, " +
                $"Manifest={CharacterFootDiagnosticSampling.LastManifestPath}, " +
                $"Summary={m_DiagnosticSummary}");
            Repaint();
        }

        void StartManualSample()
        {
            m_DiagnosticSummary = string.Empty;
            ExecuteSampling(CharacterGameplayDiagnosticCapture.StartManual);
            Repaint();
        }

        void StopManualSample()
        {
            ExecuteSampling(() =>
            {
                if (CharacterGameplayDiagnosticCapture.OwnsCapture)
                    CharacterGameplayDiagnosticCapture.StopAndSave();
                else
                    CharacterFootDiagnosticSampling.StopAndSaveSampling();
            });
            if (!string.IsNullOrEmpty(CharacterFootDiagnosticSampling.LastSavedPath))
                ShowLastDiagnostics();
        }

        void CaptureSamplingUiState()
        {
            m_LastSamplingCapturing = CharacterFootDiagnosticSampling.IsCapturing;
            m_LastSamplingStarting = CharacterFootDiagnosticSampling.IsFinalizing;
            m_LastSamplingSavedPath = CharacterFootDiagnosticSampling.LastSavedPath;
            m_LastSamplingManifestPath = CharacterFootDiagnosticSampling.LastManifestPath;
            m_LastSamplingDirectory = CharacterFootDiagnosticSampling.LastSavedDirectory;
            m_LastSamplingReportPath = CharacterFootDiagnosticSampling.LastReportPath;
            m_LastEditorBusy = IsBusy;
            m_LastEditorPlaying = EditorApplication.isPlaying;
            m_LastEditorCompiling = EditorApplication.isCompiling;
            m_LastSamplingAvailable = CharacterFootDiagnosticSampling.IsAvailable;
            m_LastAnalysisAvailable = CharacterFootDiagnosticSampling.IsAnalysisAvailable;
        }
    }
}
