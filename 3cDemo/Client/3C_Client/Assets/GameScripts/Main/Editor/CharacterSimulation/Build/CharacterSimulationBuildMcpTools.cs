using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using BTSMTL.Timeline.Unity;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    [McpForUnityTool("timeline.build_float32_program", Description = "Build and publish the Float32 Program wrapper for one exact TimelineAsset path through the shared Semantic IR pipeline.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = false)]
    public static class BuildTimelineFloat32ProgramMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Assets/... path to one TimelineAsset required for start.", Required = false)]
            public string timeline_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return CharacterSimulationBuildMcpJobScheduler.Handle(
                @params,
                CharacterSimulationBuildKind.TimelineFloat32);
        }
    }

    [McpForUnityTool("timeline.build_fixed_program", Description = "Build and publish the Fixed Program wrapper for one exact TimelineAsset and one exact wrapper destination through the shared Semantic IR pipeline.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = false)]
    public static class BuildTimelineFixedProgramMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Assets/... path to one TimelineAsset required for start.", Required = false)]
            public string timeline_asset_path { get; set; }

            [ToolParameter("Exact Assets/... .asset destination for the Fixed Timeline Program wrapper required for start.", Required = false)]
            public string wrapper_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return CharacterSimulationBuildMcpJobScheduler.Handle(
                @params,
                CharacterSimulationBuildKind.TimelineFixed);
        }
    }

    public static class TimelineSimulationBuildBatchEntryPoint
    {
        public static void BuildFloat32()
        {
            Run("timeline.build_float32_program", false);
        }

        public static void BuildFixed()
        {
            Run("timeline.build_fixed_program", true);
        }

        public static void BuildPanelExampleScene()
        {
            const string programPath = "Assets/Configs/Timeline/ScenePresentation/Generated/CorinPanelExpandTimeline.TimelineProgram.asset";
            const string scenePath = "Assets/Scenes/Timeline/CorinPanelExpandTimelineScene.unity";
            try
            {
                TimelineSimulationProgramAsset program = AssetDatabase.LoadAssetAtPath<TimelineSimulationProgramAsset>(programPath);
                if (!program)
                    throw new InvalidOperationException($"Timeline Program asset '{programPath}' is missing.");
                EnsureFolder("Assets/Scenes", "Timeline");
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateCamera();
                CreateLight();
                GameObject canvas = CreateCanvas();
                ScenePresentationPanelTarget left = CreatePanel(canvas.transform, "LeftPanel", "scene.panel.left", new Vector2(-120f, 0f), new Color(0.15f, 0.45f, 1f, 0f));
                ScenePresentationPanelTarget right = CreatePanel(canvas.transform, "RightPanel", "scene.panel.right", new Vector2(120f, 0f), new Color(1f, 0.35f, 0.15f, 0f));
                GameObject hostObject = new GameObject("TimelineHost");
                hostObject.SetActive(false);
                ScenePresentationTimelineHost host = hostObject.AddComponent<ScenePresentationTimelineHost>();
                ConfigureHost(host, program, left, right);
                hostObject.SetActive(true);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                    throw new InvalidOperationException($"Timeline sample scene '{scenePath}' could not be saved.");
                AssetDatabase.SaveAssets();
                Debug.Log($"Timeline sample scene published: {scenePath}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void PreparePanelExampleTimeline()
        {
            const string timelinePath = "Assets/Configs/Timeline/ScenePresentation/CorinPanelExpandTimeline.asset";
            try
            {
                TimelineAsset asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
                if (!asset || asset.Data == null)
                    throw new InvalidOperationException($"Timeline asset '{timelinePath}' is missing.");
                TreeTrack track = asset.Data.Tracks.OfType<TreeTrack>().SingleOrDefault();
                if (track == null)
                {
                    TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
                    asset.Data.AddTrack(typeof(TreeTrack), catalog);
                    track = asset.Data.Tracks.OfType<TreeTrack>().Single();
                    TreeClip clip = asset.Data.AddClip(catalog, track, 0) as TreeClip;
                    clip.SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                }
                EnsurePanelVisibilityBindings(asset);
                Debug.Log($"Timeline condition TreeClip prepared: {timelinePath}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void EnsurePanelVisibilityBindings(TimelineAsset asset)
        {
            TimelineData timeline = asset.Data;
            if (!timeline.TryGetExternalBinding("leftVisible", out _))
                timeline.AddExternalBinding("leftVisible", "Left Visible", "scene.presentation.panel", TimelineBindingValueKind.Boolean, TimelineBindingAccess.Write, TimelineBindingLifetime.Tick, "visible");
            if (!timeline.TryGetExternalBinding("rightVisible", out _))
                timeline.AddExternalBinding("rightVisible", "Right Visible", "scene.presentation.panel", TimelineBindingValueKind.Boolean, TimelineBindingAccess.Write, TimelineBindingLifetime.Tick, "visible");
            ScenePresentationParameterTrack track = timeline.Tracks.OfType<ScenePresentationParameterTrack>().Single();
            TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
            if (!track.Clips.OfType<ScenePresentationParameterCurveClip>().Any(value => string.Equals(value.ParameterBindingId, "leftVisible", StringComparison.Ordinal)))
            {
                ScenePresentationParameterCurveClip clip = timeline.AddClip(catalog, track, 0) as ScenePresentationParameterCurveClip;
                clip.StartFrame = 0;
                clip.EndFrame = 60;
                clip.ConfigureBindings("leftPanel", "leftVisible", AnimationCurve.Constant(0f, 1f, 1f));
            }
            if (!track.Clips.OfType<ScenePresentationParameterCurveClip>().Any(value => string.Equals(value.ParameterBindingId, "rightVisible", StringComparison.Ordinal)))
            {
                ScenePresentationParameterCurveClip clip = timeline.AddClip(catalog, track, 0) as ScenePresentationParameterCurveClip;
                clip.StartFrame = 0;
                clip.EndFrame = 60;
                clip.ConfigureBindings("rightPanel", "rightVisible", AnimationCurve.Constant(0f, 1f, 1f));
            }
            timeline.Init();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        public static void RunPanelExampleScene()
        {
            const string scenePath = "Assets/Scenes/Timeline/CorinPanelExpandTimelineScene.unity";
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ScenePresentationTimelineHost host = UnityEngine.Object.FindObjectOfType<ScenePresentationTimelineHost>();
                ScenePresentationPanelTarget[] targets = UnityEngine.Object.FindObjectsOfType<ScenePresentationPanelTarget>();
                if (!scene.IsValid() || !host || targets.Length != 2)
                    throw new InvalidOperationException($"Timeline sample scene '{scenePath}' has an incomplete runtime binding.");
                host.PrepareAndStart();
                int frameCount = 0;
                void Tick()
                {
                    host.Advance(1f / 60f);
                    frameCount++;
                    if (frameCount < 90)
                        return;
                    EditorApplication.update -= Tick;
                    FixedTimelineSimulationProgramAsset fixedAsset = AssetDatabase.LoadAssetAtPath<FixedTimelineSimulationProgramAsset>("Assets/Configs/Timeline/ScenePresentation/Generated/CorinPanelExpandTimeline.FixedTimelineProgram.asset");
                    if (!fixedAsset)
                        throw new InvalidOperationException("Fixed Timeline sample Program asset is missing.");
                    var fixedPlayback = new FixedTimelinePlayback(fixedAsset.Load());
                    try
                    {
                        fixedPlayback.Prepare(
                            new FixedTimelineBindingSet(new[]
                            {
                                new FixedTimelineTargetBinding("leftPanel", targets[0]),
                                new FixedTimelineTargetBinding("rightPanel", targets[1])
                            }),
                            new[]
                            {
                                new TimelineCallBinding("leftPanel", TimelineBindingValue.Target(targets[0].TargetIdentity)),
                                new TimelineCallBinding("rightPanel", TimelineBindingValue.Target(targets[1].TargetIdentity))
                            },
                            new FixedTimelineCall("scene.presentation.corin.fixed", "scene.panel.expand", 2UL));
                        fixedPlayback.Start();
                        for (int i = 0; i < 90; i++)
                            fixedPlayback.Advance(FixedScalar.FromRatio(1, 60));
                        bool valid = host.Status == Float32TimelinePlaybackStatus.Completed &&
                                     fixedPlayback.Status == FixedTimelinePlaybackStatus.Completed &&
                                     targets.All(target => target.OpenAmount > 0.99f && target.Visible);
                        TimelineSimulationProgramAsset floatAsset = AssetDatabase.LoadAssetAtPath<TimelineSimulationProgramAsset>("Assets/Configs/Timeline/ScenePresentation/Generated/CorinPanelExpandTimeline.TimelineProgram.asset");
                        if (!floatAsset)
                            throw new InvalidOperationException("Float32 Timeline sample Program asset is missing.");
                        bool lifecycleValid = RunIndependentLifecycleEvidence(floatAsset.Load());
                        bool runValid = valid && lifecycleValid;
                        Debug.Log($"Timeline sample scene run: floatStatus={host.Status}, fixedStatus={fixedPlayback.Status}, targets={targets.Length}, left={targets[0].OpenAmount:0.###}, right={targets[1].OpenAmount:0.###}, lifecycle={lifecycleValid}, valid={runValid}");
                        EditorApplication.Exit(runValid ? 0 : 1);
                    }
                    finally
                    {
                        fixedPlayback.Dispose();
                    }
                }
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static bool RunIndependentLifecycleEvidence(ThirdPersonSimulation.CharacterSimulationProgram program)
        {
            var firstLeft = new TimelineScenePresentationTarget("scene.panel.concurrent.left.a");
            var firstRight = new TimelineScenePresentationTarget("scene.panel.concurrent.right.a");
            var secondLeft = new TimelineScenePresentationTarget("scene.panel.concurrent.left.b");
            var secondRight = new TimelineScenePresentationTarget("scene.panel.concurrent.right.b");
            var first = new Float32TimelinePlayback(program);
            var second = new Float32TimelinePlayback(program);
            bool concurrent = false;
            bool firstCompleted = false;
            bool secondForceStopped = false;
            string firstIdentity = string.Empty;
            string secondIdentity = string.Empty;
            TimelinePlaybackObservation firstObservation = default;
            TimelinePlaybackObservation secondObservation = default;
            try
            {
                first.Prepare(
                    CreateFloat32BindingSet(firstLeft, firstRight),
                    CreateTargetCallBindings(firstLeft, firstRight),
                    new Float32TimelineCall("scene.presentation.concurrent.a", "scene.panel.expand", 11UL));
                second.Prepare(
                    CreateFloat32BindingSet(secondLeft, secondRight),
                    CreateTargetCallBindings(secondLeft, secondRight),
                    new Float32TimelineCall("scene.presentation.concurrent.b", "scene.panel.expand", 12UL));
                first.Start();
                second.Start();
                firstIdentity = FormatTimelineIdentity(first.ExecutionIdentity);
                secondIdentity = FormatTimelineIdentity(second.ExecutionIdentity);
                for (int i = 0; i < 30; i++)
                {
                    first.Advance(Float32Scalar.FromSingle(1f / 60f));
                    second.Advance(Float32Scalar.FromSingle(1f / 60f));
                }
                second.ForceStop();
                concurrent = first.Status == Float32TimelinePlaybackStatus.Running &&
                             second.Status == Float32TimelinePlaybackStatus.Stopped;
                for (int i = 0; i < 90; i++)
                    first.Advance(Float32Scalar.FromSingle(1f / 60f));
                firstCompleted = first.Status == Float32TimelinePlaybackStatus.Completed &&
                                 firstLeft.LastSamples.Count > 0 &&
                                 firstRight.LastSamples.Count > 0;
                secondForceStopped = second.Status == Float32TimelinePlaybackStatus.Stopped;
                firstObservation = first.Observation;
                secondObservation = second.Observation;
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }

            bool invalidBindingRejected = false;
            using (var invalid = new Float32TimelinePlayback(program))
            {
                try
                {
                    var suppliedLeft = new TimelineScenePresentationTarget("scene.panel.invalid.supplied.left");
                    var suppliedRight = new TimelineScenePresentationTarget("scene.panel.invalid.supplied.right");
                    invalid.Prepare(
                        CreateFloat32BindingSet(suppliedLeft, suppliedRight),
                        new[]
                        {
                            new TimelineCallBinding("leftPanel", TimelineBindingValue.Target("scene.panel.invalid.call.left")),
                            new TimelineCallBinding("rightPanel", TimelineBindingValue.Target("scene.panel.invalid.call.right"))
                        },
                        new Float32TimelineCall("scene.presentation.invalid", "scene.panel.expand", 13UL));
                }
                catch (InvalidOperationException)
                {
                    invalidBindingRejected = true;
                }
            }

            bool valid = concurrent && firstCompleted && secondForceStopped && invalidBindingRejected;
            Debug.Log(
                $"Timeline independent lifecycle evidence: concurrent={concurrent}, first={firstIdentity}, second={secondIdentity}, firstState={firstObservation.State}, secondState={secondObservation.State}, root={firstObservation.RootIdentity}, entry={firstObservation.EntryIdentity}, content={firstObservation.ContentIdentity}, bindings={string.Join(",", firstObservation.BindingIds)}, targets={firstLeft.TargetIdentity}|{firstRight.TargetIdentity}|{secondLeft.TargetIdentity}|{secondRight.TargetIdentity}, forceStopped={secondForceStopped}, invalidBindingRejected={invalidBindingRejected}, valid={valid}");
            return valid;
        }

        static string FormatTimelineIdentity(TimelineExecutionIdentity identity)
        {
            return $"{identity.OwnerIdentity}/{identity.CallIdentity}/{identity.InstanceId}";
        }

        static Float32TimelineBindingSet CreateFloat32BindingSet(
            ITimelineScenePresentationSink left,
            ITimelineScenePresentationSink right)
        {
            return new Float32TimelineBindingSet(new[]
            {
                new Float32TimelineTargetBinding("leftPanel", left),
                new Float32TimelineTargetBinding("rightPanel", right)
            });
        }

        static TimelineCallBinding[] CreateTargetCallBindings(
            ITimelineScenePresentationSink left,
            ITimelineScenePresentationSink right)
        {
            return new[]
            {
                new TimelineCallBinding("leftPanel", TimelineBindingValue.Target(left.TargetIdentity)),
                new TimelineCallBinding("rightPanel", TimelineBindingValue.Target(right.TargetIdentity))
            };
        }

        static void Run(string toolName, bool fixedTarget)
        {
            try
            {
                var parameters = new JObject
                {
                    ["timeline_asset_path"] = RequiredArgument("-timeline_asset_path")
                };
                if (fixedTarget)
                    parameters["wrapper_asset_path"] = RequiredArgument("-wrapper_asset_path");
                object response = fixedTarget
                    ? CharacterSimulationBuildMcpBridge.BuildTimelineFixed(parameters)
                    : CharacterSimulationBuildMcpBridge.BuildTimelineFloat32(parameters);
                Debug.Log($"{toolName}: {JsonConvert.SerializeObject(response, Formatting.None)}");
                EditorApplication.Exit(response is IMcpResponse result && result.Success ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static string RequiredArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++)
                if (string.Equals(arguments[i], name, StringComparison.Ordinal))
                    return string.IsNullOrWhiteSpace(arguments[i + 1])
                        ? throw new InvalidOperationException($"Command line argument '{name}' is empty.")
                        : arguments[i + 1];
            throw new InvalidOperationException($"Command line argument '{name}' is required.");
        }

        static GameObject CreateCanvas()
        {
            GameObject canvasObject = new GameObject("TimelineCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvasObject;
        }

        static ScenePresentationPanelTarget CreatePanel(
            Transform parent,
            string name,
            string targetIdentity,
            Vector2 position,
            Color color)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100f, 100f);
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            ScenePresentationPanelTarget target = panelObject.AddComponent<ScenePresentationPanelTarget>();
            var serialized = new SerializedObject(target);
            serialized.FindProperty("m_TargetIdentity").stringValue = targetIdentity;
            serialized.FindProperty("m_Graphic").objectReferenceValue = image;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }

        static void ConfigureHost(
            ScenePresentationTimelineHost host,
            TimelineSimulationProgramAsset program,
            ScenePresentationPanelTarget left,
            ScenePresentationPanelTarget right)
        {
            var serialized = new SerializedObject(host);
            serialized.FindProperty("m_ProgramAsset").objectReferenceValue = program;
            serialized.FindProperty("m_OwnerIdentity").stringValue = "scene.presentation.corin";
            serialized.FindProperty("m_CallIdentity").stringValue = "scene.panel.expand";
            serialized.FindProperty("m_InstanceId").ulongValue = 1UL;
            SerializedProperty bindings = serialized.FindProperty("m_TargetBindings");
            bindings.arraySize = 2;
            SerializedProperty leftBinding = bindings.GetArrayElementAtIndex(0);
            leftBinding.FindPropertyRelative("m_BindingId").stringValue = "leftPanel";
            leftBinding.FindPropertyRelative("m_Target").objectReferenceValue = left;
            SerializedProperty rightBinding = bindings.GetArrayElementAtIndex(1);
            rightBinding.FindPropertyRelative("m_BindingId").stringValue = "rightPanel";
            rightBinding.FindPropertyRelative("m_Target").objectReferenceValue = right;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;
            cameraObject.GetComponent<Camera>().orthographic = true;
            cameraObject.GetComponent<Camera>().orthographicSize = 4f;
        }

        static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }

    static class CharacterSimulationBuildMcpBridge
    {
        static bool s_Building;

        public static object BuildTimelineFloat32(JObject parameters)
        {
            object validation = ValidateTimelineRequest(
                parameters,
                new HashSet<string>(StringComparer.Ordinal) { "timeline_asset_path" },
                false,
                out TimelineAsset timeline,
                out string timelinePath,
                out _);
            if (validation != null)
                return validation;
            if (!TryEnter(out object busy))
                return busy;
            try
            {
                ICharacterSimulationTargetBuildAdapter target =
                    CharacterSimulationTargetCatalog.Float32(timeline);
                TimelineSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                    new TimelineSimulationBuildRequest(
                        timeline,
                        CharacterSimulationBuildPublicationMode.Publish,
                        new[] { target }));
                if (!result.IsValid)
                    return TimelineBuildFailure(timelinePath, target.UnityWrapperDestination, result);
                TimelineSimulationProgramAsset wrapper =
                    AssetDatabase.LoadAssetAtPath<TimelineSimulationProgramAsset>(target.UnityWrapperDestination);
                if (!wrapper)
                {
                    return new ErrorResponse(
                        "timeline_float32_wrapper_missing_after_build",
                        new { timelineAssetPath = timelinePath, wrapperAssetPath = target.UnityWrapperDestination });
                }
                return new SuccessResponse(
                    "Exact Float32 Timeline Program was published.",
                    CreateTimelineResponse(
                        timelinePath,
                        target.UnityWrapperDestination,
                        wrapper.NumericProfileId,
                        wrapper.TargetAbiVersion,
                        wrapper.ProgramId,
                        wrapper.SourceRevision,
                        wrapper.SemanticHash,
                        wrapper.ProgramHash,
                        wrapper.LayoutHash,
                        wrapper.CanonicalBytesHash,
                        wrapper.CanonicalByteLength,
                        result));
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "timeline_build_exception",
                    new { timelineAssetPath = timelinePath, message = exception.Message });
            }
            finally
            {
                s_Building = false;
            }
        }

        public static object BuildTimelineFixed(JObject parameters)
        {
            object validation = ValidateTimelineRequest(
                parameters,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "timeline_asset_path",
                    "wrapper_asset_path"
                },
                true,
                out TimelineAsset timeline,
                out string timelinePath,
                out string wrapperPath);
            if (validation != null)
                return validation;
            if (!TryEnter(out object busy))
                return busy;
            try
            {
                ICharacterSimulationTargetBuildAdapter target =
                    new FixedCharacterSimulationTargetBuildAdapter(
                        wrapperPath,
                        SimulationProgramRootKind.Timeline);
                TimelineSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                    new TimelineSimulationBuildRequest(
                        timeline,
                        CharacterSimulationBuildPublicationMode.Publish,
                        new[] { target }));
                if (!result.IsValid)
                    return TimelineBuildFailure(timelinePath, wrapperPath, result);
                FixedTimelineSimulationProgramAsset wrapper =
                    AssetDatabase.LoadAssetAtPath<FixedTimelineSimulationProgramAsset>(wrapperPath);
                if (!wrapper)
                {
                    return new ErrorResponse(
                        "timeline_fixed_wrapper_missing_after_build",
                        new { timelineAssetPath = timelinePath, wrapperAssetPath = wrapperPath });
                }
                return new SuccessResponse(
                    "Exact Fixed Timeline Program was published.",
                    CreateTimelineResponse(
                        timelinePath,
                        wrapperPath,
                        ThirdPersonSimulation.Fixed.FixedSimulationNumericProfile.Value.Id.Value,
                        ThirdPersonSimulation.Fixed.FixedSimulationNumericProfile.Value.AbiVersion.Value,
                        wrapper.ProgramId,
                        wrapper.SourceRevision,
                        wrapper.SemanticHash,
                        wrapper.ProgramHash,
                        wrapper.LayoutHash,
                        wrapper.CanonicalBytesHash,
                        wrapper.CanonicalByteLength,
                        result));
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "timeline_build_exception",
                    new
                    {
                        timelineAssetPath = timelinePath,
                        wrapperAssetPath = wrapperPath,
                        message = exception.Message
                    });
            }
            finally
            {
                s_Building = false;
            }
        }

        static object ValidateTimelineRequest(
            JObject parameters,
            HashSet<string> allowed,
            bool requiresWrapper,
            out TimelineAsset timeline,
            out string timelinePath,
            out string wrapperPath)
        {
            timeline = null;
            timelinePath = string.Empty;
            wrapperPath = string.Empty;
            if (parameters == null)
                return new ErrorResponse("request_missing");
            string unknown = parameters.Properties()
                .Select(property => property.Name)
                .FirstOrDefault(name => !allowed.Contains(name));
            if (!string.IsNullOrEmpty(unknown))
                return new ErrorResponse("unknown_parameter", new { parameter = unknown });
            if (!TryGetExactAssetPath(parameters, "timeline_asset_path", out timelinePath))
                return new ErrorResponse("timeline_asset_path_required");
            timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (!timeline || !string.Equals(AssetDatabase.GetAssetPath(timeline), timelinePath, StringComparison.Ordinal))
            {
                return new ErrorResponse(
                    "timeline_asset_not_found",
                    new { timelineAssetPath = timelinePath });
            }
            if (!requiresWrapper)
                return null;
            if (!TryGetExactAssetPath(parameters, "wrapper_asset_path", out wrapperPath))
                return new ErrorResponse("wrapper_asset_path_required", new { timelineAssetPath = timelinePath });
            return null;
        }

        static bool TryGetExactAssetPath(
            JObject parameters,
            string key,
            out string path)
        {
            path = string.Empty;
            JToken token = parameters[key];
            if (token?.Type != JTokenType.String)
                return false;
            path = token.Value<string>();
            return !string.IsNullOrWhiteSpace(path) &&
                   path.StartsWith("Assets/", StringComparison.Ordinal) &&
                   path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                   path.IndexOf('\\') < 0 &&
                   path.IndexOf("/../", StringComparison.Ordinal) < 0 &&
                   path.IndexOf("/./", StringComparison.Ordinal) < 0 &&
                   path.IndexOf("//", StringComparison.Ordinal) < 0;
        }

        static bool TryEnter(out object error)
        {
            error = null;
            if (s_Building)
            {
                error = new ErrorResponse("character_build_already_running");
                return false;
            }
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                Application.isPlaying)
            {
                error = new ErrorResponse(
                    "unity_editor_busy",
                    new
                    {
                        isCompiling = EditorApplication.isCompiling,
                        isUpdating = EditorApplication.isUpdating,
                        isPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode
                    });
                return false;
            }
            s_Building = true;
            return true;
        }

        static object TimelineBuildFailure(
            string timelinePath,
            string wrapperPath,
            TimelineSimulationBuildResult result)
        {
            return new ErrorResponse(
                "timeline_build_failed",
                new
                {
                    timelineAssetPath = timelinePath,
                    wrapperAssetPath = wrapperPath,
                    diagnostics = Messages(result)
                });
        }

        static object CreateTimelineResponse(
            string timelinePath,
            string wrapperPath,
            string numericProfileId,
            int targetAbiVersion,
            string programId,
            string sourceRevision,
            string semanticHash,
            string programHash,
            string layoutHash,
            string canonicalBytesHash,
            int canonicalByteLength,
            TimelineSimulationBuildResult result)
        {
            SimulationProgramRootDescriptor root = result.Artifact.Header.Root;
            return new
            {
                timelineAssetPath = timelinePath,
                wrapperAssetPath = wrapperPath,
                numericProfileId,
                targetAbiVersion,
                programId,
                sourceRevision,
                semanticHash,
                programHash,
                layoutHash,
                canonicalBytesHash,
                canonicalByteLength,
                rootKind = root.Kind.ToString(),
                rootIdentity = root.RootIdentity,
                entryIdentity = root.EntryIdentity,
                contentIdentity = root.ContentIdentity,
                diagnostics = Messages(result)
            };
        }

        static object[] Messages(TimelineSimulationBuildResult result)
        {
            return Messages(result?.Report);
        }

        static object[] Messages(CharacterSimulationCompileReport report)
        {
            if (report == null)
                return Array.Empty<object>();
            return report.Messages
                .Select(message => (object)new
                {
                    stage = message.Stage.ToString(),
                    severity = message.Severity.ToString(),
                    message.Code,
                    message.SourceIdentity,
                    message.Message
                })
                .ToArray();
        }
    }
}
