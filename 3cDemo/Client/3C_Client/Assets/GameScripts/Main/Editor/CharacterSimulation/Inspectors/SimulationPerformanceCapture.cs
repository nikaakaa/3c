using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [InitializeOnLoad]
    static class SimulationPerformanceCapture
    {
        const string PendingKey = "ThirdPerson.PerformanceCapture.Pending";
        const double WarmupSeconds = 3d;
        const double CaptureSeconds = 10d;
        const int SampleCapacity = 1024;
        const string MainThreadMarker = "Main Thread";
        const string IdleMarker = "Idle";
        const string GcAllocatedCounter = "GC Allocated In Frame";

        static readonly string[] s_MarkerNames =
        {
            "ThirdPerson.Gameplay.Input",
            "ThirdPerson.Gameplay.Logic",
            "ThirdPerson.Gameplay.Presentation",
            "ThirdPerson.Session.Input",
            "ThirdPerson.Session.LogicTick",
            "ThirdPerson.Simulation.Pipeline.Transaction",
            "ThirdPerson.Simulation.Pipeline.CheckpointCapture",
            "ThirdPerson.Simulation.Pipeline.Ingress",
            "ThirdPerson.Simulation.Pipeline.Schedule",
            "ThirdPerson.Simulation.Pipeline.Restore",
            "ThirdPerson.Simulation.Pipeline.Evaluate",
            "ThirdPerson.Simulation.Pipeline.WorldResolve",
            "ThirdPerson.Simulation.Pipeline.Finalize",
            "ThirdPerson.Simulation.Pipeline.Egress",
            "ThirdPerson.Simulation.Pipeline.CommitFreeze",
            "ThirdPerson.Simulation.Pipeline.StatePublish",
            "ThirdPerson.Simulation.Pipeline.ExternalCommit",
            "ThirdPerson.Simulation.Pipeline.StepOther",
            "ThirdPerson.Simulation.Kernel.Evaluate",
            "ThirdPerson.Simulation.Kernel.ProgramValidation",
            "ThirdPerson.Simulation.Kernel.Workspace",
            "ThirdPerson.Simulation.Kernel.PendingLease",
            "ThirdPerson.Simulation.Kernel.Finalize",
            "ThirdPerson.Simulation.Kernel.StateCommit",
            "ThirdPerson.Simulation.Kernel.ResultFreeze",
            "ThirdPerson.Simulation.Operation.FrameBegin",
            "ThirdPerson.Simulation.Operation.Setup",
            "ThirdPerson.Simulation.Operation.Ingress",
            "ThirdPerson.Simulation.Operation.GameplayEffectAdvance",
            "ThirdPerson.Simulation.Operation.InputRequestApply",
            "ThirdPerson.Simulation.Operation.TimelineDecision",
            "ThirdPerson.Simulation.Operation.ControlTick",
            "ThirdPerson.Simulation.Operation.MotionResolve",
            "ThirdPerson.Simulation.Operation.BlackboardFinalize",
            "ThirdPerson.Simulation.Operation.FrameComplete",
            "ThirdPerson.Presentation.Animation",
            "ThirdPerson.Presentation.Animation.TransactionBegin",
            "ThirdPerson.Presentation.Animation.Prepare",
            "ThirdPerson.Presentation.Animation.Validate",
            "ThirdPerson.Presentation.Animation.GraphEvaluate",
            "ThirdPerson.Presentation.Animation.PoseGraphExecute",
            "ThirdPerson.Presentation.Animation.FinalWrite",
            "ThirdPerson.Presentation.Animation.PoseGraph.ValueReset",
            "ThirdPerson.Presentation.Animation.PoseGraph.PlayerInput",
            "ThirdPerson.Presentation.Animation.PoseGraph.Slot",
            "ThirdPerson.Presentation.Animation.PoseGraph.State",
            "ThirdPerson.Presentation.Animation.PoseGraph.Inertialization",
            "ThirdPerson.Presentation.Animation.PoseGraph.Blend",
            "ThirdPerson.Presentation.Animation.PoseGraph.Constraint",
            "ThirdPerson.Presentation.Animation.PoseGraph.TwoBoneIK",
            "ThirdPerson.Presentation.Animation.PoseGraph.FootPlacement",
            "ThirdPerson.Presentation.Animation.PoseGraph.SpaceConversion",
            "ThirdPerson.Presentation.Animation.PoseGraph.Output",
            "ThirdPerson.Presentation.Animation.PoseGraph.ValueValidation",
            "ThirdPerson.Presentation.Animation.Seal",
            "ThirdPerson.Presentation.Animation.Diagnostics",
            "ThirdPerson.Presentation.Animation.ActionLifecycle",
            "ThirdPerson.Presentation.Animation.ActionSampling",
            "ThirdPerson.Presentation.Animation.PoseRouting",
            "ThirdPerson.Presentation.Animation.MotionMatching",
            "ThirdPerson.Presentation.Animation.ReleaseProtocol",
            "ThirdPerson.Presentation.Animation.FrameCommit",
            "ThirdPerson.Presentation.Animation.PostCommit",
            "ThirdPerson.Presentation.Body",
            "ThirdPerson.Presentation.Equipment",
            "ThirdPerson.Presentation.FactProjection",
            "ThirdPerson.Presentation.FinalPose",
            "ThirdPerson.Presentation.Camera",
            "FootPlacement.Plan",
            "FootPlacement.Query",
            "FootPlacement.Solve"
        };

        static readonly List<RecorderEntry> s_Recorders = new List<RecorderEntry>();
        static ProfilerRecorder s_GcAllocatedRecorder;
        static bool s_Active;
        static bool s_Recording;
        static bool s_HasGcAllocatedRecorder;
        static double s_WarmupEndsAt;
        static double s_CaptureEndsAt;
        static double s_CaptureStartedAt;
        static int s_CaptureStartFrame;
        static ulong s_CaptureStartLogicTick;

        static SimulationPerformanceCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (SessionState.GetBool(PendingKey, false) && EditorApplication.isPlaying)
                EditorApplication.delayCall += Arm;
        }

        [MenuItem("Tools/3C/Diagnostics/Capture Simulation Performance (10s) %#F9")]
        static void StartCapture()
        {
            if (s_Active || SessionState.GetBool(PendingKey, false))
                throw new InvalidOperationException("A Simulation performance capture is already active.");
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Start gameplay through Launcher before capturing Simulation performance.");
            SessionState.SetBool(PendingKey, true);
            Arm();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && s_Active)
                Abort();
        }

        static void Arm()
        {
            if (s_Active || !EditorApplication.isPlaying)
                return;
            s_Active = true;
            s_WarmupEndsAt = EditorApplication.timeSinceStartup + WarmupSeconds;
            EditorApplication.update += Update;
            Debug.Log("Simulation performance capture armed: 3s warmup, then 10s recording.");
        }

        static void Update()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    Abort();
                    return;
                }
                double now = EditorApplication.timeSinceStartup;
                if (!s_Recording)
                {
                    if (now < s_WarmupEndsAt)
                        return;
                    BeginRecording(now);
                    return;
                }
                if (now >= s_CaptureEndsAt)
                    Complete();
            }
            catch (Exception exception)
            {
                Cleanup();
                Debug.LogException(exception);
            }
        }

        static void BeginRecording(double now)
        {
            if (!ThirdPersonGameplay.Tick.GameplayTickSystem.IsInitialized)
                throw new InvalidOperationException("Simulation performance capture requires an initialized GameplayTickSystem.");
            var options = ProfilerRecorderOptions.StartImmediately |
                          ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
                          ProfilerRecorderOptions.SumAllSamplesInFrame;
            for (int i = 0; i < s_MarkerNames.Length; i++)
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(
                    ProfilerCategory.Scripts,
                    s_MarkerNames[i],
                    SampleCapacity,
                    options);
                s_Recorders.Add(new RecorderEntry(s_MarkerNames[i], recorder));
            }
            s_Recorders.Add(new RecorderEntry(
                MainThreadMarker,
                ProfilerRecorder.StartNew(
                    ProfilerCategory.Internal,
                    MainThreadMarker,
                    SampleCapacity,
                    options)));
            s_Recorders.Add(new RecorderEntry(
                IdleMarker,
                ProfilerRecorder.StartNew(
                    ProfilerCategory.Internal,
                    IdleMarker,
                    SampleCapacity,
                    options)));
            s_GcAllocatedRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory,
                GcAllocatedCounter,
                SampleCapacity,
                options);
            s_HasGcAllocatedRecorder = true;
            s_CaptureStartedAt = now;
            s_CaptureStartFrame = Time.frameCount;
            s_CaptureStartLogicTick = ThirdPersonGameplay.Tick.GameplayTickSystem.Current.LocalLogicTick;
            s_Recording = true;
            s_CaptureEndsAt = now + CaptureSeconds;
        }

        static void Complete()
        {
            double actualCaptureSeconds = Math.Max(double.Epsilon, EditorApplication.timeSinceStartup - s_CaptureStartedAt);
            ulong currentLogicTick = ThirdPersonGameplay.Tick.GameplayTickSystem.IsInitialized
                ? ThirdPersonGameplay.Tick.GameplayTickSystem.Current.LocalLogicTick
                : s_CaptureStartLogicTick;
            if (currentLogicTick < s_CaptureStartLogicTick)
                throw new InvalidOperationException("GameplayTickSystem restarted during Simulation performance capture.");
            long logicTickCount = checked((long)(currentLogicTick - s_CaptureStartLogicTick));
            int presentationFrameCount = Math.Max(0, Time.frameCount - s_CaptureStartFrame);
            var report = new CaptureReport
            {
                capturedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                scene = SceneManager.GetActiveScene().path,
                targetFrameRate = Application.targetFrameRate,
                vSyncCount = QualitySettings.vSyncCount,
                timeScale = Time.timeScale,
                warmupSeconds = WarmupSeconds,
                captureSeconds = actualCaptureSeconds,
                samples = new List<MarkerReport>(s_Recorders.Count),
                gcAllocatedBytesPerFrame = BuildCounterReport(s_GcAllocatedRecorder)
            };
            for (int i = 0; i < s_Recorders.Count; i++)
                report.samples.Add(BuildReport(s_Recorders[i]));
            MarkerReport logic = FindReport(report.samples, "ThirdPerson.Session.LogicTick");
            MarkerReport presentation = FindReport(report.samples, "ThirdPerson.Gameplay.Presentation");
            if (logic == null || logic.invocationCount == 0)
            {
                Cleanup();
                Debug.LogError("Simulation performance capture found no formal Session LogicTick. Start gameplay through Launcher and capture again.");
                return;
            }
            report.logicTicks = logicTickCount;
            report.logicTicksPerSecond = logicTickCount / actualCaptureSeconds;
            report.meanLogicMillisecondsPerTick = logic.meanMillisecondsPerInvocation;
            report.presentationFrames = presentationFrameCount;
            report.presentationFramesPerSecond = presentationFrameCount / actualCaptureSeconds;
            report.samples.Sort((left, right) => right.p95Milliseconds.CompareTo(left.p95Milliseconds));

            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/Performance"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(
                directory,
                $"Simulation-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Cleanup();
            Debug.Log($"Simulation performance capture completed: {path}");
        }

        static MarkerReport FindReport(IReadOnlyList<MarkerReport> reports, string marker)
        {
            for (int i = 0; i < reports.Count; i++)
            {
                if (reports[i].marker == marker)
                    return reports[i];
            }
            return null;
        }

        static MarkerReport BuildReport(RecorderEntry entry)
        {
            entry.Recorder.Stop();
            ProfilerRecorderSample[] samples = entry.Recorder.Valid
                ? entry.Recorder.ToArray()
                : Array.Empty<ProfilerRecorderSample>();
            var values = new List<long>(samples.Length);
            long calls = 0;
            double totalNanoseconds = 0d;
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i].Count <= 0)
                    continue;
                values.Add(samples[i].Value);
                calls += samples[i].Count;
                totalNanoseconds += samples[i].Value;
            }
            values.Sort();
            return new MarkerReport
            {
                marker = entry.Name,
                valid = entry.Recorder.Valid,
                sampledFrames = values.Count,
                invocationCount = calls,
                meanMilliseconds = values.Count == 0 ? 0d : totalNanoseconds / values.Count / 1000000d,
                meanMillisecondsPerInvocation = calls == 0 ? 0d : totalNanoseconds / calls / 1000000d,
                p50Milliseconds = Percentile(values, 0.50d),
                p95Milliseconds = Percentile(values, 0.95d),
                maxMilliseconds = values.Count == 0 ? 0d : values[values.Count - 1] / 1000000d
            };
        }

        static CounterReport BuildCounterReport(ProfilerRecorder recorder)
        {
            recorder.Stop();
            ProfilerRecorderSample[] samples = recorder.Valid
                ? recorder.ToArray()
                : Array.Empty<ProfilerRecorderSample>();
            var values = new List<long>(samples.Length);
            double total = 0d;
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i].Count <= 0)
                    continue;
                values.Add(samples[i].Value);
                total += samples[i].Value;
            }
            values.Sort();
            return new CounterReport
            {
                counter = GcAllocatedCounter,
                unit = "Bytes",
                valid = recorder.Valid,
                sampledFrames = values.Count,
                mean = values.Count == 0 ? 0d : total / values.Count,
                p50 = PercentileRaw(values, 0.50d),
                p95 = PercentileRaw(values, 0.95d),
                max = values.Count == 0 ? 0d : values[values.Count - 1]
            };
        }

        static double Percentile(IReadOnlyList<long> values, double percentile)
        {
            if (values.Count == 0)
                return 0d;
            int index = Math.Max(0, Math.Min(values.Count - 1, (int)Math.Ceiling(values.Count * percentile) - 1));
            return values[index] / 1000000d;
        }

        static double PercentileRaw(IReadOnlyList<long> values, double percentile)
        {
            if (values.Count == 0)
                return 0d;
            int index = Math.Max(0, Math.Min(values.Count - 1, (int)Math.Ceiling(values.Count * percentile) - 1));
            return values[index];
        }

        static void Abort()
        {
            Cleanup();
            Debug.LogWarning("Simulation performance capture aborted before completion.");
        }

        static void Cleanup()
        {
            EditorApplication.update -= Update;
            for (int i = 0; i < s_Recorders.Count; i++)
                s_Recorders[i].Recorder.Dispose();
            s_Recorders.Clear();
            if (s_HasGcAllocatedRecorder)
                s_GcAllocatedRecorder.Dispose();
            s_GcAllocatedRecorder = default;
            s_HasGcAllocatedRecorder = false;
            s_Active = false;
            s_Recording = false;
            s_CaptureStartedAt = 0d;
            s_CaptureStartFrame = 0;
            s_CaptureStartLogicTick = 0UL;
            SessionState.EraseBool(PendingKey);
        }

        sealed class RecorderEntry
        {
            public RecorderEntry(string name, ProfilerRecorder recorder)
            {
                Name = name;
                Recorder = recorder;
            }

            public string Name { get; }
            public ProfilerRecorder Recorder { get; }
        }

        [Serializable]
        sealed class CaptureReport
        {
            public string capturedAtUtc;
            public string unityVersion;
            public string scene;
            public int targetFrameRate;
            public int vSyncCount;
            public float timeScale;
            public double warmupSeconds;
            public double captureSeconds;
            public long logicTicks;
            public double logicTicksPerSecond;
            public double meanLogicMillisecondsPerTick;
            public long presentationFrames;
            public double presentationFramesPerSecond;
            public CounterReport gcAllocatedBytesPerFrame;
            public List<MarkerReport> samples;
        }

        [Serializable]
        sealed class CounterReport
        {
            public string counter;
            public string unit;
            public bool valid;
            public int sampledFrames;
            public double mean;
            public double p50;
            public double p95;
            public double max;
        }

        [Serializable]
        sealed class MarkerReport
        {
            public string marker;
            public bool valid;
            public int sampledFrames;
            public long invocationCount;
            public double meanMilliseconds;
            public double meanMillisecondsPerInvocation;
            public double p50Milliseconds;
            public double p95Milliseconds;
            public double maxMilliseconds;
        }
    }

}
