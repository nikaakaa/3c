using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvValues;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal enum CharacterFootSwingPathHorizontalAxisState
    {
        Unavailable = 0,
        Available = 1,
        InvalidComponentUp = 2,
        DegenerateAxis = 3
    }

    internal enum CharacterFootActualEnvelopeIntersectionState
    {
        Unavailable = 0,
        InvalidComponentUp = 1,
        DegenerateAxis = 2,
        NoIntersection = 3,
        Unique = 4,
        AmbiguousEnvelopeAtActualFootDistance = 5
    }

    internal enum CharacterFootActualFootAxisRegion
    {
        Unavailable = 0,
        BeforePathStart = 1,
        WithinPathSegment = 2,
        AfterPathEnd = 3
    }

    internal enum CharacterFootActualEnvelopeCounterfactualState
    {
        Unavailable = 0,
        UniqueInCorridor = 1,
        AmbiguousInCorridor = 2,
        OutsideGroundPathCorridor = 3,
        NoIntersection = 4
    }

    internal sealed class CharacterFootActualEnvelopeIntersectionFact
    {
        internal CharacterFootActualEnvelopeIntersectionState State;
        internal float ActualFootHorizontalDistance;
        internal float BaselineHorizontalDistance;
        internal float EnvelopeHorizontalDistance;
        internal CharacterFootActualFootAxisRegion AxisRegion;
        internal float ClosestPathParameter;
        internal float DistanceAlongAxis;
        internal float CrossTrackDistance;
        internal float CorridorRadius;
        internal bool WithinGroundPathCorridor;
        internal int CandidateCount;
        internal float MinimumHeightAlongUp;
        internal float MaximumHeightAlongUp;
        internal float HeightSpan;
        internal bool HasVerticalEdge;
        internal bool HasMultipleHeights;
        internal bool Ambiguous;
        internal CharacterFootActualEnvelopeCounterfactualState
            CounterfactualState;
    }

    [InitializeOnLoad]
    public static class CharacterFootLandingPredictionSampler
    {
        const int MaximumPendingFrameCount = 256;
        const int MaximumQueuedFrameCount = 256;
        const double SamplingStartTimeoutSeconds = 30d;
        const double SamplingFlushIntervalSeconds = 0.5d;
        const float ActualEnvelopeHorizontalEpsilonMeters = 0.001f;
        const float ActualEnvelopeHeightEpsilonMeters = 0.001f;
        const string GameplayLabPlayerActorId = "gameplay-lab-player";
        const string StartMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Start";
        const string StopMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Stop and Save";
        const string GeometryFileName = "ground-path-geometry.csv";
        static readonly string Header = CharacterFootSampleColumns.Schema.Header;
        const string GeometryHeader =
            "SampleIdentity,FrameSequence,CompletionIdentity,Side,GroundPathInputIdentity," +
            "GroundContactIndex,GroundContactSegmentIndex,GroundContactSurfaceIdentity,GroundContactCandidateIdentity," +
            "GroundContactPositionX,GroundContactPositionY,GroundContactPositionZ," +
            "GroundContactNormalX,GroundContactNormalY,GroundContactNormalZ,GroundContactQueryDistance," +
            "GroundEnvelopeVertexIndex,GroundEnvelopeVertexX,GroundEnvelopeVertexY,GroundEnvelopeVertexZ," +
            "GroundSurfaceSegmentIndex,GroundSurfaceIdentity,GroundSurfaceFaceIndex," +
            "GroundSurfaceStartDistance,GroundSurfaceStartHeight,GroundSurfaceEndDistance,GroundSurfaceEndHeight";

        readonly struct SamplingProgramIdentity
        {
            internal SamplingProgramIdentity(
                AnimationPresentationProgramIdentity identity)
                : this(
                    identity.ProjectionRevision,
                    identity.PoseGraphId,
                    identity.PoseGraphRevision,
                    identity.PosePlanHash)
            {
            }

            internal SamplingProgramIdentity(
                string projectionRevision,
                string poseGraphId,
                string poseGraphRevision,
                string posePlanHash)
            {
                if (string.IsNullOrWhiteSpace(projectionRevision) ||
                    string.IsNullOrWhiteSpace(poseGraphId) ||
                    string.IsNullOrWhiteSpace(poseGraphRevision) ||
                    string.IsNullOrWhiteSpace(posePlanHash))
                {
                    throw new ArgumentException(
                        "Foot Landing sampling Program identity is incomplete.");
                }
                ProjectionRevision = projectionRevision.Trim();
                PoseGraphId = poseGraphId.Trim();
                PoseGraphRevision = poseGraphRevision.Trim();
                PosePlanHash = posePlanHash.Trim();
                ProgramIdentity = $"{ProjectionRevision}|{PosePlanHash}";
            }

            internal string ProgramIdentity { get; }
            internal string ProjectionRevision { get; }
            internal string PoseGraphId { get; }
            internal string PoseGraphRevision { get; }
            internal string PosePlanHash { get; }

            internal bool Matches(in SamplingProgramIdentity other) =>
                string.Equals(
                    ProjectionRevision,
                    other.ProjectionRevision,
                    StringComparison.Ordinal) &&
                string.Equals(PoseGraphId, other.PoseGraphId, StringComparison.Ordinal) &&
                string.Equals(
                    PoseGraphRevision,
                    other.PoseGraphRevision,
                    StringComparison.Ordinal) &&
                string.Equals(PosePlanHash, other.PosePlanHash, StringComparison.Ordinal);
        }

        sealed class SamplingSession : IDisposable
        {
            readonly FileStream m_SamplesStream;
            readonly StreamWriter m_SamplesWriter;
            readonly StringBuilder m_SamplesRow = new StringBuilder(4096);
            readonly FileStream m_GeometryStream;
            readonly StreamWriter m_GeometryWriter;
            readonly StringBuilder m_GeometryRow = new StringBuilder(512);
            readonly BlockingCollection<CapturedFrame> m_Queue =
                new BlockingCollection<CapturedFrame>(
                    new ConcurrentQueue<CapturedFrame>(),
                    MaximumQueuedFrameCount);
            readonly Thread m_WriterThread;
            readonly string m_SamplesPartPath;
            readonly string m_GeometryPartPath;
            Exception m_Failure;
            int m_AcceptedFrameCount;
            int m_WrittenFrameCount;
            int m_Disposed;

            internal SamplingSession(in SamplingProgramIdentity program)
            {
                SampleIdentity = Guid.NewGuid();
                StartedUtc = DateTime.UtcNow;
                Program = program;
                string root = ResolveSaveDirectory();
                DirectoryPath = System.IO.Path.Combine(
                    root,
                    $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{SampleIdentity:N}");
                Directory.CreateDirectory(DirectoryPath);
                Path = System.IO.Path.Combine(DirectoryPath, "samples.csv");
                GeometryPath = System.IO.Path.Combine(
                    DirectoryPath,
                    GeometryFileName);
                m_SamplesPartPath = Path + ".part";
                m_GeometryPartPath = GeometryPath + ".part";
                FileStream samplesStream = null;
                StreamWriter samplesWriter = null;
                FileStream geometryStream = null;
                StreamWriter geometryWriter = null;
                try
                {
                    samplesStream = new FileStream(
                        m_SamplesPartPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.Read,
                        65536,
                        FileOptions.SequentialScan);
                    samplesWriter = new StreamWriter(
                        samplesStream,
                        new UTF8Encoding(false));
                    geometryStream = new FileStream(
                        m_GeometryPartPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.Read,
                        65536,
                        FileOptions.SequentialScan);
                    geometryWriter = new StreamWriter(
                        geometryStream,
                        new UTF8Encoding(false));
                    samplesWriter.WriteLine(Header);
                    geometryWriter.WriteLine(GeometryHeader);
                    samplesWriter.Flush();
                    geometryWriter.Flush();
                }
                catch
                {
                    geometryWriter?.Dispose();
                    geometryStream?.Dispose();
                    samplesWriter?.Dispose();
                    samplesStream?.Dispose();
                    throw;
                }
                m_SamplesStream = samplesStream;
                m_SamplesWriter = samplesWriter;
                m_GeometryStream = geometryStream;
                m_GeometryWriter = geometryWriter;
                m_WriterThread = new Thread(WriteLoop)
                {
                    IsBackground = true,
                    Name = $"Foot Landing CSV {SampleIdentity:N}",
                    Priority = System.Threading.ThreadPriority.Normal
                };
                m_WriterThread.Start();
            }

            internal Guid SampleIdentity { get; }
            internal DateTime StartedUtc { get; }
            internal SamplingProgramIdentity Program { get; }
            internal string DirectoryPath { get; }
            internal string Path { get; }
            internal string GeometryPath { get; }
            internal int FrameCount => Volatile.Read(ref m_AcceptedFrameCount);
            internal int WrittenFrameCount => Volatile.Read(ref m_WrittenFrameCount);

            internal void Enqueue(CapturedFrame captured)
            {
                if (captured == null)
                    throw new ArgumentNullException(nameof(captured));
                RequireHealthy();
                if (!m_Queue.TryAdd(captured))
                {
                    throw new InvalidOperationException(
                        "Foot Landing CSV writer queue capacity was exceeded.");
                }
                Interlocked.Increment(ref m_AcceptedFrameCount);
                RequireHealthy();
            }

            internal void RequireHealthy()
            {
                if (Volatile.Read(ref m_Disposed) != 0)
                    throw new ObjectDisposedException(nameof(SamplingSession));
                Exception failure = Volatile.Read(ref m_Failure);
                if (failure != null)
                {
                    throw new IOException(
                        "Foot Landing CSV background writer failed.",
                        failure);
                }
            }

            void WriteLoop()
            {
                long flushIntervalTicks =
                    TimeSpan.FromSeconds(SamplingFlushIntervalSeconds).Ticks;
                long nextFlushTicks = DateTime.UtcNow.Ticks + flushIntervalTicks;
                try
                {
                    foreach (CapturedFrame captured in m_Queue.GetConsumingEnumerable())
                    {
                        Write(captured);
                        Interlocked.Increment(ref m_WrittenFrameCount);
                        long now = DateTime.UtcNow.Ticks;
                        if (now < nextFlushTicks)
                            continue;
                        FlushBuffered();
                        nextFlushTicks = now + flushIntervalTicks;
                    }
                    FlushToDisk();
                }
                catch (Exception exception)
                {
                    Volatile.Write(ref m_Failure, exception);
                }
                finally
                {
                    try
                    {
                        m_GeometryWriter.Dispose();
                        m_SamplesWriter.Dispose();
                    }
                    catch (Exception exception)
                    {
                        Volatile.Write(ref m_Failure, exception);
                    }
                }
            }

            void Write(CapturedFrame captured)
            {
                CharacterFootLandingPredictionDiagnostics frame = captured.Foot;
                FootIkCapture left = captured.Left;
                FootIkCapture right = captured.Right;
                FootStepObservationCapture footStepObservation = captured.FootStepObservation;
                RootHierarchyCapture roots = captured.Roots;
                CharacterFootLandingPredictionFootDiagnostics leftFoot = frame.Left;
                CharacterFootLandingPredictionFootDiagnostics rightFoot = frame.Right;
                WriteSampleRow(
                    this,
                    m_SamplesWriter,
                    m_SamplesRow,
                    in frame,
                    in leftFoot,
                    in left,
                    in footStepObservation,
                    in roots,
                    captured.TargetRuntimeInstanceId,
                    captured.TargetHostInstanceId);
                WriteSampleRow(
                    this,
                    m_SamplesWriter,
                    m_SamplesRow,
                    in frame,
                    in rightFoot,
                    in right,
                    in footStepObservation,
                    in roots,
                    captured.TargetRuntimeInstanceId,
                    captured.TargetHostInstanceId);
                WriteGeometryRows(
                    this,
                    m_GeometryWriter,
                    m_GeometryRow,
                    in frame,
                    in leftFoot);
                WriteGeometryRows(
                    this,
                    m_GeometryWriter,
                    m_GeometryRow,
                    in frame,
                    in rightFoot);
            }

            void FlushBuffered()
            {
                m_SamplesWriter.Flush();
                m_GeometryWriter.Flush();
            }

            void FlushToDisk()
            {
                FlushBuffered();
                m_SamplesStream.Flush(true);
                m_GeometryStream.Flush(true);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref m_Disposed, 1) != 0)
                    return;
                m_Queue.CompleteAdding();
                m_WriterThread.Join();
                m_Queue.Dispose();
                Exception failure = Volatile.Read(ref m_Failure);
                if (failure != null)
                {
                    throw new IOException(
                        "Foot Landing CSV background writer failed.",
                        failure);
                }
                if (WrittenFrameCount != FrameCount)
                {
                    throw new InvalidOperationException(
                        "Foot Landing CSV background writer did not persist every captured frame.");
                }
                if (File.Exists(Path) || File.Exists(GeometryPath))
                    throw new IOException("Foot Landing sealed capture package already exists.");
                File.Move(m_GeometryPartPath, GeometryPath);
                File.Move(m_SamplesPartPath, Path);
            }
        }

        sealed class FinalizationJob
        {
            readonly SamplingSession m_Session;
            readonly Thread m_Thread;
            CharacterFootMotionDiagnosticAnalysis m_Analysis;
            Exception m_Failure;
            int m_Completed;

            internal FinalizationJob(
                SamplingSession session,
                Exception captureFailure,
                int droppedPendingFrameCount)
            {
                m_Session = session ?? throw new ArgumentNullException(nameof(session));
                CaptureFailure = captureFailure;
                DroppedPendingFrameCount = droppedPendingFrameCount;
                m_Thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = $"Foot Landing Finalizer {session.SampleIdentity:N}",
                    Priority = System.Threading.ThreadPriority.BelowNormal
                };
            }

            internal Guid SampleIdentity => m_Session.SampleIdentity;
            internal string SamplesPath => m_Session.Path;
            internal string GeometryPath => m_Session.GeometryPath;
            internal string DirectoryPath => m_Session.DirectoryPath;
            internal int AcceptedFrameCount => m_Session.FrameCount;
            internal int WrittenFrameCount => m_Session.WrittenFrameCount;
            internal int DroppedPendingFrameCount { get; }
            internal Exception CaptureFailure { get; }
            internal bool IsCompleted => Volatile.Read(ref m_Completed) != 0;
            internal Exception Failure => m_Failure;
            internal CharacterFootMotionDiagnosticAnalysis Analysis => m_Analysis;

            internal void Start() => m_Thread.Start();

            internal void Wait() => m_Thread.Join();

            void Run()
            {
                try
                {
                    m_Session.Dispose();
                    if (m_Session.WrittenFrameCount == 0)
                    {
                        if (File.Exists(m_Session.Path))
                            File.Delete(m_Session.Path);
                        if (File.Exists(m_Session.GeometryPath))
                            File.Delete(m_Session.GeometryPath);
                        if (Directory.Exists(m_Session.DirectoryPath) &&
                            !Directory.EnumerateFileSystemEntries(
                                m_Session.DirectoryPath).Any())
                        {
                            Directory.Delete(m_Session.DirectoryPath);
                        }
                    }
                    else
                    {
                        m_Analysis = CharacterFootMotionDiagnosticAnalyzer.Analyze(
                            m_Session.Path);
                    }
                }
                catch (Exception exception)
                {
                    m_Failure = exception;
                }
                finally
                {
                    Volatile.Write(ref m_Completed, 1);
                }
            }
        }

        internal readonly struct FootIkCapture
        {
            internal FootIkCapture(
                CharacterFullBodyIkSolverDiagnostics solver,
                CharacterFullBodyIkEffectorDiagnostics pelvis,
                CharacterFullBodyIkEffectorDiagnostics effector,
                CharacterFullBodyIkLimbDiagnostics limb,
                bool physicalWriteAvailable,
                ulong physicalWriteCompletionIdentity,
                Vector3 physicalAnkleComponentPosition,
                Quaternion physicalAnkleComponentRotation,
                Vector3 physicalPelvisComponentPosition,
                Vector3 physicalPelvisWorldPosition)
            {
                Solver = solver;
                Pelvis = pelvis;
                Effector = effector;
                Limb = limb;
                PhysicalWriteAvailable = physicalWriteAvailable;
                PhysicalWriteCompletionIdentity = physicalWriteCompletionIdentity;
                PhysicalAnkleComponentPosition = physicalAnkleComponentPosition;
                PhysicalAnkleComponentRotation = physicalAnkleComponentRotation;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
                PhysicalPelvisWorldPosition = physicalPelvisWorldPosition;
            }

            internal CharacterFullBodyIkSolverDiagnostics Solver { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Pelvis { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Effector { get; }
            internal CharacterFullBodyIkLimbDiagnostics Limb { get; }
            internal bool SolverAvailable => Solver.IsCompleted;
            internal bool PelvisAvailable => Pelvis.IsAvailable;
            internal bool EffectorAvailable => Effector.IsAvailable;
            internal bool PhysicalWriteAvailable { get; }
            internal ulong PhysicalWriteCompletionIdentity { get; }
            internal Vector3 PhysicalAnkleComponentPosition { get; }
            internal Quaternion PhysicalAnkleComponentRotation { get; }
            internal Vector3 PhysicalPelvisComponentPosition { get; }
            internal Vector3 PhysicalPelvisWorldPosition { get; }
        }

        sealed class PendingFrame
        {
            internal PendingFrame(in CharacterFootLandingPredictionDiagnostics diagnostics)
            {
                Diagnostics = diagnostics;
            }

            internal CharacterFootLandingPredictionDiagnostics Diagnostics { get; }
        }

        internal readonly struct RootHierarchyCapture
        {
            internal RootHierarchyCapture(CharacterRootHierarchyBinding binding)
            {
                if (!binding)
                    throw new ArgumentNullException(nameof(binding));
                LogicRootPosition = binding.LogicRoot.position;
                LogicRootRotation = binding.LogicRoot.rotation;
                VisualRootLocalPosition = binding.VisualRoot.localPosition;
                VisualRootLocalRotation = binding.VisualRoot.localRotation;
                VisualRootWorldPosition = binding.VisualRoot.position;
                VisualRootWorldRotation = binding.VisualRoot.rotation;
                PoseRootLocalPosition = binding.PoseRoot.localPosition;
                PoseRootLocalRotation = binding.PoseRoot.localRotation;
                PoseRootWorldPosition = binding.PoseRoot.position;
                PoseRootWorldRotation = binding.PoseRoot.rotation;
                PoseRootLossyScale = binding.PoseRoot.lossyScale;
            }

            internal Vector3 LogicRootPosition { get; }
            internal Quaternion LogicRootRotation { get; }
            internal Vector3 VisualRootLocalPosition { get; }
            internal Quaternion VisualRootLocalRotation { get; }
            internal Vector3 VisualRootWorldPosition { get; }
            internal Quaternion VisualRootWorldRotation { get; }
            internal Vector3 PoseRootLocalPosition { get; }
            internal Quaternion PoseRootLocalRotation { get; }
            internal Vector3 PoseRootWorldPosition { get; }
            internal Quaternion PoseRootWorldRotation { get; }
            internal Vector3 PoseRootLossyScale { get; }
        }

        readonly struct FootStepObservationCapture
        {
            internal FootStepObservationCapture(
                string sourceIdentity,
                float weight,
                float normalizedTime,
                AnimationFootMotionRuntimeSample left,
                AnimationFootMotionRuntimeSample right)
            {
                if (string.IsNullOrWhiteSpace(sourceIdentity) ||
                    !float.IsFinite(weight) || weight < 0f || weight > 1f ||
                    !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f ||
                    !left.IsValid || !right.IsValid)
                {
                    throw new ArgumentException("Foot Step observation capture is invalid.");
                }
                SourceIdentity = sourceIdentity.Trim();
                Weight = weight;
                NormalizedTime = normalizedTime;
                Left = left;
                Right = right;
                m_IsSpecified = 1;
            }

            readonly byte m_IsSpecified;
            internal string SourceIdentity { get; }
            internal float Weight { get; }
            internal float NormalizedTime { get; }
            internal AnimationFootMotionRuntimeSample Left { get; }
            internal AnimationFootMotionRuntimeSample Right { get; }
            internal bool IsValid => m_IsSpecified != 0;
        }

        sealed class CapturedFrame
        {
            internal CapturedFrame(
                in CharacterFootLandingPredictionDiagnostics foot,
                FootIkCapture left,
                FootIkCapture right,
                FootStepObservationCapture footStepObservation,
                Vector3 physicalPelvisComponentPosition,
                RootHierarchyCapture roots,
                Guid targetRuntimeInstanceId,
                int targetHostInstanceId)
            {
                Foot = foot;
                Left = left;
                Right = right;
                FootStepObservation = footStepObservation;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
                Roots = roots;
                TargetRuntimeInstanceId = targetRuntimeInstanceId;
                TargetHostInstanceId = targetHostInstanceId;
            }

            internal CharacterFootLandingPredictionDiagnostics Foot { get; }
            internal FootIkCapture Left { get; }
            internal FootIkCapture Right { get; }
            internal FootStepObservationCapture FootStepObservation { get; }
            internal Vector3 PhysicalPelvisComponentPosition { get; }
            internal RootHierarchyCapture Roots { get; }
            internal Guid TargetRuntimeInstanceId { get; }
            internal int TargetHostInstanceId { get; }
        }

        static readonly List<PendingFrame> s_PendingFrames =
            new List<PendingFrame>(64);
        static readonly HashSet<Guid> s_ConfiguredTargets = new HashSet<Guid>();
        static readonly Dictionary<Guid, string> s_PoseWatchSignatures =
            new Dictionary<Guid, string>();
        static readonly Guid s_DiagnosticsOwnerId = Guid.NewGuid();

        static bool s_Capturing;
        static bool s_StartPending;
        static bool s_ControlledCaptureWindow;
        static bool s_CaptureWindowOpen;
        static double s_StartDeadline;
        static string s_LastStartFailure = string.Empty;
        static string s_StartWaitReason = string.Empty;
        static string s_LastSavedPath = string.Empty;
        static string s_LastSavedGeometryPath = string.Empty;
        static string s_LastSavedDirectory = string.Empty;
        static string s_LastSavedAnalysisPath = string.Empty;
        static string s_LastSavedDiagnosisDirectory = string.Empty;
        static string s_LastDiagnosticSummary = string.Empty;
        static string s_LastSavedSampleIdentity = string.Empty;
        static string s_LastFinalizationFailure = string.Empty;
        static SamplingSession s_Session;
        static FinalizationJob s_Finalization;
        static int s_DroppedPendingFrameCount;
        static int s_LastSavedFrameCount;
        static int s_LastFactEventCount;
        static int s_LastDiagnosisTargetCount;
        static int s_LastDiagnosisMatchCount;
        static int s_TargetHostInstanceId;
        static int s_TargetRootInstanceId;
        static CharacterRootHierarchyBinding s_TargetRootHierarchy;

        public static bool IsCapturing => s_Capturing;
        public static bool IsStartPending => s_StartPending;
        public static bool IsFinalizing => s_Finalization != null;
        public static bool IsControlledCaptureWindow =>
            s_Capturing && s_ControlledCaptureWindow;
        public static bool IsCaptureWindowOpen =>
            s_Capturing && s_CaptureWindowOpen;
        public static string LastStartFailure => s_LastStartFailure;
        public static string LastSavedPath => s_LastSavedPath;
        public static string LastSavedGeometryPath => s_LastSavedGeometryPath;
        public static string LastSavedDirectory => s_LastSavedDirectory;
        public static string LastSavedAnalysisPath => s_LastSavedAnalysisPath;
        public static string LastSavedDiagnosisDirectory =>
            s_LastSavedDiagnosisDirectory;
        public static string LastDiagnosticSummary => s_LastDiagnosticSummary;
        public static string LastFinalizationFailure => s_LastFinalizationFailure;
        public static string CurrentSampleIdentity =>
            s_Session?.SampleIdentity.ToString("N") ??
            s_Finalization?.SampleIdentity.ToString("N") ??
            string.Empty;
        public static string LastSavedSampleIdentity => s_LastSavedSampleIdentity;
        public static int CapturedFrameCount =>
            s_Session?.FrameCount ??
            s_Finalization?.AcceptedFrameCount ??
            0;
        public static int PendingFrameCount => s_PendingFrames.Count;
        public static int DroppedPendingFrameCount => s_DroppedPendingFrameCount;
        public static int LastSavedFrameCount => s_LastSavedFrameCount;
        public static int LastFactEventCount => s_LastFactEventCount;
        public static int LastDiagnosisTargetCount => s_LastDiagnosisTargetCount;
        public static int LastDiagnosisMatchCount => s_LastDiagnosisMatchCount;

        static CharacterFootLandingPredictionSampler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            s_LastSavedPath = FindLatestSavedPath();
            if (!string.IsNullOrEmpty(s_LastSavedPath))
            {
                s_LastSavedDirectory = System.IO.Path.GetDirectoryName(
                    s_LastSavedPath) ?? string.Empty;
                string geometryPath = System.IO.Path.Combine(
                    s_LastSavedDirectory,
                    GeometryFileName);
                s_LastSavedGeometryPath = File.Exists(geometryPath)
                    ? geometryPath
                    : string.Empty;
                string analysisPath = System.IO.Path.Combine(
                    s_LastSavedDirectory,
                    "diagnoses",
                    CharacterFootDiagnosticStore.ManifestFileName);
                s_LastSavedAnalysisPath = File.Exists(analysisPath)
                    ? analysisPath
                    : string.Empty;
                string diagnosisDirectory = System.IO.Path.Combine(
                    s_LastSavedDirectory,
                    "diagnoses");
                s_LastSavedDiagnosisDirectory = Directory.Exists(
                    diagnosisDirectory)
                    ? diagnosisDirectory
                    : string.Empty;
            }
        }

        public static void StartSampling() => StartSampling(false);

        public static void StartControlledSampling() => StartSampling(true);

        public static void OpenControlledCaptureWindow()
        {
            if (!s_Capturing || !s_ControlledCaptureWindow ||
                s_CaptureWindowOpen)
            {
                throw new InvalidOperationException(
                    "Foot Landing controlled capture window cannot open in the current state.");
            }
            s_CaptureWindowOpen = true;
        }

        public static void CloseControlledCaptureWindow()
        {
            if (!s_Capturing || !s_ControlledCaptureWindow ||
                !s_CaptureWindowOpen)
            {
                throw new InvalidOperationException(
                    "Foot Landing controlled capture window cannot close in the current state.");
            }
            s_CaptureWindowOpen = false;
        }

        static void StartSampling(bool controlledCaptureWindow)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Foot Landing sampling can only start in Play Mode.");
            if (s_Capturing)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already active.");
            if (s_StartPending)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already waiting for the Gameplay Lab player.");
            if (s_Finalization != null)
                throw new InvalidOperationException(
                    "Foot Landing sampling is still finalizing the previous capture.");
            s_PendingFrames.Clear();
            s_DroppedPendingFrameCount = 0;
            s_LastSavedFrameCount = 0;
            s_LastFactEventCount = 0;
            s_LastDiagnosisTargetCount = 0;
            s_LastDiagnosisMatchCount = 0;
            s_LastDiagnosticSummary = string.Empty;
            s_LastFinalizationFailure = string.Empty;
            s_LastStartFailure = string.Empty;
            s_StartWaitReason = string.Empty;
            s_ControlledCaptureWindow = controlledCaptureWindow;
            s_CaptureWindowOpen = !controlledCaptureWindow;
            s_StartPending = true;
            s_StartDeadline = EditorApplication.timeSinceStartup + SamplingStartTimeoutSeconds;
            EditorApplication.update -= PollSamplingStart;
            EditorApplication.update += PollSamplingStart;
            PollSamplingStart();
        }

        static void PollSamplingStart()
        {
            if (!s_StartPending)
            {
                EditorApplication.update -= PollSamplingStart;
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                FailSamplingStart("Gameplay Lab left Play Mode before the player host became available.");
                return;
            }
            try
            {
                if (TryCompleteSamplingStart())
                    return;
            }
            catch (Exception exception)
            {
                FailSamplingStart(exception.Message);
                Debug.LogException(exception);
                return;
            }
            if (EditorApplication.timeSinceStartup >= s_StartDeadline)
                FailSamplingStart(s_StartWaitReason);
        }

        static bool TryCompleteSamplingStart()
        {
            if (!TryBindGameplayLabPlayerTarget())
            {
                s_StartWaitReason = "Gameplay Lab player host did not become available before sampling timed out.";
                return false;
            }
            if (!TryResolveSamplingProgramIdentity(out SamplingProgramIdentity program))
            {
                s_StartWaitReason =
                    "Gameplay Lab player compiled Animation Presentation Program did not become available before sampling timed out.";
                return false;
            }
            s_Capturing = true;
            try
            {
                ConfigureTargets();
                s_Session = new SamplingSession(in program);
                s_LastSavedPath = s_Session.Path;
                s_LastSavedGeometryPath = s_Session.GeometryPath;
                s_LastSavedDirectory = s_Session.DirectoryPath;
                s_LastSavedAnalysisPath = string.Empty;
                s_LastSavedDiagnosisDirectory = string.Empty;
                s_LastSavedSampleIdentity = s_Session.SampleIdentity.ToString("N");
                CharacterFootLandingPredictionDebugRegistry.Published += Capture;
                AnimationPresentationRuntimeTargetRegistry.TargetRegistered += ConfigureTarget;
                AnimationPresentationRuntimeTargetRegistry.TargetUnregistered += RemoveTarget;
                EditorApplication.update += ProcessPendingFrames;
            }
            catch
            {
                CancelSamplingStart();
                throw;
            }
            s_StartPending = false;
            s_StartDeadline = 0d;
            s_StartWaitReason = string.Empty;
            EditorApplication.update -= PollSamplingStart;
            Debug.Log(
                $"Foot Landing sampling started. " +
                $"Sample={s_Session.SampleIdentity:N}, " +
                $"Program={s_Session.Program.ProgramIdentity}, " +
                $"Path={s_Session.Path}");
            return true;
        }

        [MenuItem(StartMenu)]
        static void StartFromMenu() => StartSampling();

        [MenuItem(StartMenu, true)]
        static bool CanStart() =>
            EditorApplication.isPlaying && !s_Capturing && !s_StartPending &&
            s_Finalization == null;

        [MenuItem(StopMenu)]
        static void Stop() => StopAndSave();

        [MenuItem(StopMenu, true)]
        static bool CanStop() => s_Capturing || s_StartPending;

        public static void StopAndSaveSampling() => StopAndSave();

        static void Capture(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (!s_Capturing || !s_CaptureWindowOpen)
                return;
            if (diagnostics.RootInstanceId != s_TargetRootInstanceId)
                return;
            if (s_PendingFrames.Count >= MaximumPendingFrameCount)
            {
                s_PendingFrames.RemoveAt(0);
                s_DroppedPendingFrameCount++;
            }
            s_PendingFrames.Add(new PendingFrame(in diagnostics));
        }

        static void ProcessPendingFrames()
        {
            if (!s_Capturing)
                return;
            try
            {
                ProcessPendingFramesCore();
            }
            catch (Exception exception)
            {
                FailActiveSampling(exception);
            }
        }

        static void ProcessPendingFramesCore()
        {
            ConfigureTargets();
            SamplingSession session = s_Session ?? throw new InvalidOperationException(
                "Foot Landing sampling has no active persistent session.");
            session.RequireHealthy();
            for (int pendingIndex = 0; pendingIndex < s_PendingFrames.Count;)
            {
                PendingFrame pending = s_PendingFrames[pendingIndex];
                CharacterFootLandingPredictionDiagnostics pendingDiagnostics = pending.Diagnostics;
                PendingFrameResolution resolution = TryCaptureCommittedIk(
                    in pendingDiagnostics,
                    out CapturedFrame captured);
                if (resolution == PendingFrameResolution.Waiting)
                {
                    pendingIndex++;
                    continue;
                }
                if (resolution == PendingFrameResolution.Captured)
                    session.Enqueue(captured);
                else
                    s_DroppedPendingFrameCount++;
                s_PendingFrames.RemoveAt(pendingIndex);
            }
            session.RequireHealthy();
        }

        enum PendingFrameResolution : byte
        {
            Waiting,
            Captured,
            Stale
        }

        static PendingFrameResolution TryCaptureCommittedIk(
            in CharacterFootLandingPredictionDiagnostics pending,
            out CapturedFrame captured)
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                AnimationPresentationRuntimeTarget target = targets[targetIndex];
                if (target.HostInstanceId != s_TargetHostInstanceId)
                    continue;
                if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                    continue;
                AnimationFootPlacementRuntimeSnapshot placement = debugView.PosePlan.FootPlacement;
                if (!placement.IsAvailable ||
                    placement.LandingPrediction.RootInstanceId != pending.RootInstanceId)
                {
                    continue;
                }
                if (placement.LandingPrediction.FrameSequence > pending.FrameSequence)
                {
                    captured = default;
                    return PendingFrameResolution.Stale;
                }
                if (placement.LandingPrediction.FrameSequence != pending.FrameSequence ||
                    placement.LandingPrediction.CompletionIdentity != pending.CompletionIdentity)
                {
                    continue;
                }
                captured = new CapturedFrame(
                    in pending,
                    new FootIkCapture(
                        placement.Solver,
                        placement.Pelvis,
                        placement.LeftFoot,
                        placement.LeftLeg,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.LeftPhysicalAnkleComponentPosition,
                        placement.LeftPhysicalAnkleComponentRotation,
                        placement.PhysicalPelvisComponentPosition,
                        placement.PhysicalPelvisWorldPosition),
                    new FootIkCapture(
                        placement.Solver,
                        placement.Pelvis,
                        placement.RightFoot,
                        placement.RightLeg,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.RightPhysicalAnkleComponentPosition,
                        placement.RightPhysicalAnkleComponentRotation,
                        placement.PhysicalPelvisComponentPosition,
                        placement.PhysicalPelvisWorldPosition),
                    CaptureFootStepObservation(debugView.PosePlan),
                    placement.PhysicalPelvisComponentPosition,
                    new RootHierarchyCapture(s_TargetRootHierarchy),
                    target.RuntimeInstanceId,
                    target.HostInstanceId);
                return PendingFrameResolution.Captured;
            }
            captured = default;
            return PendingFrameResolution.Waiting;
        }

        static FootStepObservationCapture CaptureFootStepObservation(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            AnimationFootStepObservationRuntimeSnapshot observation =
                snapshot.FootStepObservation;
            return observation.IsValid
                ? new FootStepObservationCapture(
                    observation.SourceIdentity,
                    observation.SourceWeight,
                    observation.NormalizedTime,
                    observation.Left,
                    observation.Right)
                : default;
        }

        static void ConfigureTargets()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            bool configured = false;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].HostInstanceId != s_TargetHostInstanceId)
                    continue;
                ConfigureTarget(targets[i]);
                configured = true;
            }
            if (!configured)
                throw new InvalidOperationException(
                    "Gameplay Lab player Animation Presentation target is unavailable.");
        }

        static void ConfigureTarget(AnimationPresentationRuntimeTarget target)
        {
            if (!s_Capturing || target == null ||
                target.HostInstanceId != s_TargetHostInstanceId)
                return;
            var targetProgram = new SamplingProgramIdentity(target.ProgramIdentity);
            if (s_Session != null && !s_Session.Program.Matches(in targetProgram))
            {
                throw new InvalidOperationException(
                    "Gameplay Lab player compiled Animation Presentation Program changed during sampling.");
            }
            if (!s_ConfiguredTargets.Contains(target.RuntimeInstanceId))
            {
                target.SetDiagnosticsInterest(
                    s_DiagnosticsOwnerId,
                    AnimationPresentationDiagnosticsInterest.Capture |
                    AnimationPresentationDiagnosticsInterest.OperationDetail);
                s_ConfiguredTargets.Add(target.RuntimeInstanceId);
            }
            if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                return;
            AnimationFootPlacementRuntimeSnapshot footPlacement = debugView.PosePlan.FootPlacement;
            if (footPlacement.IsAvailable &&
                footPlacement.LandingPrediction.RootInstanceId != 0)
            {
                int rootInstanceId = footPlacement.LandingPrediction.RootInstanceId;
                if (s_TargetRootInstanceId != 0 && s_TargetRootInstanceId != rootInstanceId)
                {
                    throw new InvalidOperationException(
                        "Gameplay Lab player Animation Presentation root changed after sampling target binding.");
                }
                s_TargetRootInstanceId = rootInstanceId;
            }
            IReadOnlyList<AnimationPoseWatchIdentity> watches = BuildPoseWatches(debugView.PosePlan);
            string signature = BuildPoseWatchSignature(watches);
            if (string.Equals(
                    s_PoseWatchSignatures.TryGetValue(target.RuntimeInstanceId, out string previous)
                        ? previous
                        : string.Empty,
                    signature,
                    StringComparison.Ordinal))
            {
                return;
            }
            s_PoseWatchSignatures[target.RuntimeInstanceId] = signature;
            target.SetPoseWatchInterests(s_DiagnosticsOwnerId, watches);
        }

        static void RemoveTarget(AnimationPresentationRuntimeTarget target)
        {
            if (target == null)
                return;
            s_ConfiguredTargets.Remove(target.RuntimeInstanceId);
            s_PoseWatchSignatures.Remove(target.RuntimeInstanceId);
        }

        static bool TryBindGameplayLabPlayerTarget()
        {
            int selectedHostInstanceId = 0;
            int selectedRootInstanceId = 0;
            CharacterRootHierarchyBinding selectedRootHierarchy = null;
            CharacterPipelineHost[] hosts = UnityEngine.Object.FindObjectsByType<CharacterPipelineHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < hosts.Length; i++)
            {
                CharacterPipelineHost host = hosts[i];
                if (host == null || !host.VisualRoot ||
                    !string.Equals(host.ActorId, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                if (selectedHostInstanceId != 0)
                    throw new InvalidOperationException(
                        "Gameplay Lab contains multiple gameplay-lab-player hosts.");
                selectedHostInstanceId = host.GetInstanceID();
                selectedRootInstanceId = host.VisualRoot.GetInstanceID();
                selectedRootHierarchy = host.RootHierarchy;
            }
            FixedCharacterHost[] fixedHosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < fixedHosts.Length; i++)
            {
                FixedCharacterHost host = fixedHosts[i];
                if (host == null || !host.RootHierarchy ||
                    !string.Equals(host.ActorId.Value, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                if (selectedHostInstanceId != 0)
                    throw new InvalidOperationException(
                        "Gameplay Lab contains multiple gameplay-lab-player hosts.");
                selectedHostInstanceId = host.GetInstanceID();
                selectedRootInstanceId = host.RootHierarchy.VisualRoot.GetInstanceID();
                selectedRootHierarchy = host.RootHierarchy;
            }
            if (selectedHostInstanceId == 0)
            {
                ResetTargetBinding();
                return false;
            }
            s_TargetHostInstanceId = selectedHostInstanceId;
            s_TargetRootInstanceId = selectedRootInstanceId;
            s_TargetRootHierarchy = selectedRootHierarchy;
            return true;
        }

        static bool TryResolveSamplingProgramIdentity(
            out SamplingProgramIdentity identity)
        {
            identity = default;
            bool found = false;
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                AnimationPresentationRuntimeTarget target = targets[i];
                if (target.HostInstanceId != s_TargetHostInstanceId)
                    continue;
                var candidate = new SamplingProgramIdentity(target.ProgramIdentity);
                if (found && !identity.Matches(in candidate))
                {
                    throw new InvalidOperationException(
                        "Gameplay Lab player exposes multiple compiled Animation Presentation Programs.");
                }
                identity = candidate;
                found = true;
            }
            return found;
        }

        static void ResetTargetBinding()
        {
            s_TargetHostInstanceId = 0;
            s_TargetRootInstanceId = 0;
            s_TargetRootHierarchy = null;
        }

        static IReadOnlyList<AnimationPoseWatchIdentity> BuildPoseWatches(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            var result = new List<AnimationPoseWatchIdentity>(4);
            AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> operations = snapshot.Operations;
            for (int i = 0; i < operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = operations[i];
                if (operation.Code != CharacterPoseOperationCode.FootPlacement &&
                    operation.Code != CharacterPoseOperationCode.FullBodyIK)
                {
                    continue;
                }
                result.Add(new AnimationPoseWatchIdentity(
                    operation.GraphId,
                    snapshot.PoseGraphRevision,
                    operation.NodeId,
                    operation.CallSite));
            }
            return result;
        }

        static string BuildPoseWatchSignature(IReadOnlyList<AnimationPoseWatchIdentity> watches)
        {
            if (watches == null || watches.Count == 0)
                return string.Empty;
            var builder = new StringBuilder(256);
            for (int i = 0; i < watches.Count; i++)
            {
                if (builder.Length != 0)
                    builder.Append('|');
                builder.Append(watches[i]);
            }
            return builder.ToString();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                StopAndSave();
        }

        static void OnBeforeAssemblyReload()
        {
            StopAndSave();
            WaitForFinalization();
        }

        static void OnEditorQuitting()
        {
            StopAndSave();
            WaitForFinalization();
        }

        static void CancelSamplingStart()
        {
            EditorApplication.update -= PollSamplingStart;
            DetachCapture();
            try
            {
                BeginFinalization(null);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            s_StartPending = false;
            s_StartDeadline = 0d;
            s_StartWaitReason = string.Empty;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
            s_PendingFrames.Clear();
            ResetTargetBinding();
        }

        static void DetachCapture()
        {
            CharacterFootLandingPredictionDebugRegistry.Published -= Capture;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered -= ConfigureTarget;
            AnimationPresentationRuntimeTargetRegistry.TargetUnregistered -= RemoveTarget;
            EditorApplication.update -= ProcessPendingFrames;
            RemoveTargetDiagnostics();
            s_Capturing = false;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
        }

        static string StopAndSave()
        {
            if (s_Finalization != null)
                return s_LastSavedPath;
            if (s_StartPending)
            {
                CancelSamplingStart();
                return s_LastSavedPath;
            }
            if (!s_Capturing)
                return s_LastSavedPath;
            Exception failure = null;
            try
            {
                ProcessPendingFramesCore();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                DetachCapture();
                BeginFinalization(failure);
                failure = null;
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
            finally
            {
                s_PendingFrames.Clear();
                ResetTargetBinding();
            }
            if (failure != null)
                Debug.LogException(failure);
            return s_LastSavedPath;
        }

        static void BeginFinalization(Exception captureFailure)
        {
            SamplingSession session = s_Session;
            s_Session = null;
            if (session == null)
                return;
            if (s_Finalization != null)
                throw new InvalidOperationException(
                    "Foot Landing finalization is already active.");
            s_LastSavedPath = session.Path;
            s_LastSavedGeometryPath = session.GeometryPath;
            s_LastSavedDirectory = session.DirectoryPath;
            s_LastSavedSampleIdentity = session.SampleIdentity.ToString("N");
            s_LastSavedAnalysisPath = string.Empty;
            s_LastSavedDiagnosisDirectory = string.Empty;
            s_LastDiagnosticSummary = "Foot Landing finalizing capture package.";
            s_LastFinalizationFailure = string.Empty;
            var job = new FinalizationJob(
                session,
                captureFailure,
                s_DroppedPendingFrameCount);
            s_Finalization = job;
            EditorApplication.update -= PollFinalization;
            EditorApplication.update += PollFinalization;
            job.Start();
            Debug.Log(
                $"Foot Landing sampling stopped and finalization started. " +
                $"Sample={s_LastSavedSampleIdentity}, " +
                $"AcceptedFrames={job.AcceptedFrameCount}, " +
                $"Samples={job.SamplesPath}, Geometry={job.GeometryPath}");
        }

        static void PollFinalization()
        {
            FinalizationJob job = s_Finalization;
            if (job == null)
            {
                EditorApplication.update -= PollFinalization;
                return;
            }
            if (!job.IsCompleted)
                return;
            CompleteFinalization(job);
        }

        static void WaitForFinalization()
        {
            FinalizationJob job = s_Finalization;
            if (job == null)
                return;
            job.Wait();
            CompleteFinalization(job);
        }

        static void CompleteFinalization(FinalizationJob job)
        {
            if (!ReferenceEquals(s_Finalization, job))
                return;
            EditorApplication.update -= PollFinalization;
            s_Finalization = null;
            s_LastSavedFrameCount = job.WrittenFrameCount;
            if (job.Failure != null)
            {
                s_LastFinalizationFailure = job.Failure.Message;
                s_LastDiagnosticSummary =
                    $"Foot Landing finalization failed: {job.Failure.Message}";
                s_LastSavedAnalysisPath = string.Empty;
                s_LastSavedDiagnosisDirectory = string.Empty;
                Debug.LogError(s_LastDiagnosticSummary);
                Debug.LogException(job.Failure);
                return;
            }
            if (job.WrittenFrameCount == 0)
            {
                s_LastSavedPath = string.Empty;
                s_LastSavedGeometryPath = string.Empty;
                s_LastSavedDirectory = string.Empty;
                s_LastSavedAnalysisPath = string.Empty;
                s_LastSavedDiagnosisDirectory = string.Empty;
                s_LastSavedSampleIdentity = string.Empty;
                s_LastDiagnosticSummary =
                    "Foot Landing sampling canceled before any Foot rows were captured.";
                s_LastFactEventCount = 0;
                s_LastDiagnosisTargetCount = 0;
                s_LastDiagnosisMatchCount = 0;
                Debug.Log(s_LastDiagnosticSummary);
                return;
            }
            ApplyAnalysis(job.Analysis);
            if (job.CaptureFailure != null)
            {
                s_LastFinalizationFailure = job.CaptureFailure.Message;
                Debug.LogError(
                    $"Foot Landing capture stopped early; the sealed partial package was analyzed. " +
                    $"Reason={job.CaptureFailure.Message}");
                Debug.LogException(job.CaptureFailure);
            }
            Debug.Log(
                $"Foot Landing sampling finalized {s_LastSavedFrameCount} frames " +
                $"with {job.DroppedPendingFrameCount} dropped pending frames. " +
                $"Sample={s_LastSavedSampleIdentity}, " +
                $"Samples={s_LastSavedPath}, Geometry={s_LastSavedGeometryPath}, " +
                $"Analysis={s_LastSavedAnalysisPath}, " +
                $"Diagnoses={s_LastSavedDiagnosisDirectory}, " +
                $"Summary={s_LastDiagnosticSummary}");
        }

        static void ApplyAnalysis(
            CharacterFootMotionDiagnosticAnalysis analysis)
        {
            s_LastSavedPath = analysis.SamplesPath;
            s_LastSavedGeometryPath = analysis.GeometryPath;
            s_LastSavedDirectory = System.IO.Path.GetDirectoryName(
                analysis.SamplesPath) ?? string.Empty;
            s_LastSavedAnalysisPath = analysis.AnalysisPath;
            s_LastSavedDiagnosisDirectory = analysis.DiagnosisDirectory;
            s_LastDiagnosticSummary = analysis.Summary;
            s_LastFactEventCount = analysis.EventCount;
            s_LastDiagnosisTargetCount = analysis.DiagnosisTargetCount;
            s_LastDiagnosisMatchCount = analysis.DiagnosisMatchCount;
        }

        static void FailActiveSampling(Exception exception)
        {
            string path = s_Session?.Path ?? s_LastSavedPath;
            try
            {
                DetachCapture();
                BeginFinalization(exception);
            }
            catch (Exception finalizationException)
            {
                Debug.LogException(finalizationException);
            }
            finally
            {
                s_PendingFrames.Clear();
                ResetTargetBinding();
            }
            Debug.LogError(
                $"Foot Landing sampling stopped early and is finalizing its completed portion. " +
                $"Samples={path}, Reason={exception.Message}");
        }

        static void FailSamplingStart(string message)
        {
            s_LastStartFailure = string.IsNullOrWhiteSpace(message)
                ? "Foot Landing sampling could not bind the Gameplay Lab player."
                : message;
            CancelSamplingStart();
            Debug.LogError(s_LastStartFailure);
        }

        static void RemoveTargetDiagnostics()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                AnimationPresentationRuntimeTarget target = targets[i];
                if (!s_ConfiguredTargets.Contains(target.RuntimeInstanceId))
                    continue;
                target.RemovePoseWatchInterests(s_DiagnosticsOwnerId);
                target.RemoveDiagnosticsInterest(s_DiagnosticsOwnerId);
            }
            s_ConfiguredTargets.Clear();
            s_PoseWatchSignatures.Clear();
        }

        static string FindLatestSavedPath()
        {
            string directory = ResolveSaveDirectory();
            if (!Directory.Exists(directory))
                return string.Empty;
            string latestPath = string.Empty;
            DateTime latestWriteTime = DateTime.MinValue;
            foreach (string path in Directory.EnumerateFiles(
                         directory,
                         "samples.csv",
                         SearchOption.AllDirectories))
            {
                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime <= latestWriteTime)
                    continue;
                latestWriteTime = writeTime;
                latestPath = path;
            }
            return latestPath;
        }

        static string ResolveSaveDirectory() => Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "Diagnostics",
            "FootPlacementRuns"));

        static void WriteSampleRow(
            SamplingSession session,
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in FootIkCapture ik,
            in FootStepObservationCapture footStepObservation,
            in RootHierarchyCapture roots,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId)
        {
            row.Clear();
            CharacterFootLandingPredictionInputDiagnostics input = frame.Input;
            CharacterFootStepCandidateSelectionDiagnostics stepSelection =
                foot.StepCandidateSelection;
            var identitySource = new CharacterFootIdentityCsvSource(
                session.SampleIdentity.ToString("N"),
                session.StartedUtc.ToString("O", CultureInfo.InvariantCulture),
                session.Program.ProgramIdentity, session.Program.ProjectionRevision,
                session.Program.PoseGraphId, session.Program.PoseGraphRevision,
                session.Program.PosePlanHash, in frame,
                targetRuntimeInstanceId.ToString("N"), targetHostInstanceId,
                in foot, in stepSelection);
            CharacterFootStepCandidateDiagnostics selectedStep =
                stepSelection.SelectedSource ==
                CharacterFootLandingStepSource.FormalNextLanding
                    ? stepSelection.Current
                    : default;
            CharacterFootStepCandidateDiagnostics currentCandidate = stepSelection.Current;
            CharacterFootStepCandidateDiagnostics incomingCandidate = stepSelection.Incoming;
            AnimationFootMotionRuntimeSample observedStep =
                foot.Side == CharacterFootSide.Left
                    ? footStepObservation.Left
                    : footStepObservation.Right;
            bool hasObservedStep = footStepObservation.IsValid && observedStep.IsValid;
            var formalOutputSource = new CharacterFootFormalObservationCsvSource(
                hasObservedStep, footStepObservation.SourceIdentity,
                footStepObservation.Weight, footStepObservation.NormalizedTime,
                in observedStep);
            var outputEvents = new CharacterFootEventCsvSource(hasObservedStep, observedStep.Events);
            CharacterFootStepObservationInputDiagnostics inputObservation =
                input.FootStepObservation;
            AnimationFootMotionRuntimeSample inputObservedStep =
                foot.Side == CharacterFootSide.Left
                    ? inputObservation.Left
                    : inputObservation.Right;
            bool hasInputObservedStep = inputObservation.IsValid && inputObservedStep.IsValid;
            var formalInputObservation = new CharacterFootFormalObservationCsvSource(
                hasInputObservedStep, inputObservation.SourceIdentity,
                inputObservation.SourceWeight, inputObservation.NormalizedTime,
                in inputObservedStep);
            var formalInputSource = new CharacterFootFormalInputCsvSource(
                in formalInputObservation, in inputObservation);
            var inputEvents = new CharacterFootEventCsvSource(hasInputObservedStep, inputObservedStep.Events);
            Vector3 rootLocalLanding = foot.RootLocalLanding;
            CharacterFootPrimarySupportDiagnostics primarySupport = frame.PrimarySupport;
            var landingObservationSource =
                new CharacterFootLandingObservationCsvSource(in foot);
            CharacterFootGroundPathDiagnostics ground = foot.GroundPath;
            CharacterFootSwingMotionDiagnostics motion = foot.FootMotion;
            CharacterFullBodyIkGoal footGoal = foot.Goal;

            Vector3 motionUp =
                motion.PathContinuity.TargetHeightComponentUp.sqrMagnitude > 0.000001f
                ? motion.PathContinuity.TargetHeightComponentUp.normalized
                : default;
            Vector3 groundPathUp = ground.ComponentUp.sqrMagnitude > 0.000001f
                ? ground.ComponentUp.normalized
                : default;
            float originalSoleAlongUp = Vector3.Dot(
                motion.Core.OriginalSole,
                motionUp);
            float baselineSampleAlongUp = Vector3.Dot(
                motion.Core.BaselineSample,
                motionUp);
            float envelopeSampleAlongUp = Vector3.Dot(
                motion.Core.EnvelopeSample,
                motionUp);
            float motionFormalFootHeight = hasInputObservedStep
                ? inputObservedStep.FootHeight
                : 0f;
            float rawFormalTargetHeight =
                envelopeSampleAlongUp + motionFormalFootHeight;
            float envelopeMinimumCorrection =
                envelopeSampleAlongUp - originalSoleAlongUp;
            float builderSelectedCorrection = Mathf.Max(
                0f,
                rawFormalTargetHeight - originalSoleAlongUp);
            bool builderSwingTargetAvailable =
                motion.PathContinuity.PathContinuityEvaluated &&
                motion.PathContinuity.PathAvailableAfter &&
                motion.PathContinuity.PathCurrentLandingEventIdentity ==
                motion.Core.LandingEventIdentity;
            Vector3 builderSwingTargetCorrection =
                builderSwingTargetAvailable
                    ? motion.PathContinuity.PathCurrentTargetCorrection
                    : default;
            CharacterFootActualEnvelopeIntersectionFact actualEnvelope =
                ResolveActualFootEnvelopeIntersection(
                    in ground,
                    in motion,
                    groundPathUp);
            CharacterFootSwingPathHorizontalAxisState horizontalAxisState =
                actualEnvelope.State switch
                {
                    CharacterFootActualEnvelopeIntersectionState.Unavailable =>
                        CharacterFootSwingPathHorizontalAxisState.Unavailable,
                    CharacterFootActualEnvelopeIntersectionState.InvalidComponentUp =>
                        CharacterFootSwingPathHorizontalAxisState.InvalidComponentUp,
                    CharacterFootActualEnvelopeIntersectionState.DegenerateAxis =>
                        CharacterFootSwingPathHorizontalAxisState.DegenerateAxis,
                    _ => CharacterFootSwingPathHorizontalAxisState.Available
                };
            bool actualEnvelopeCorrectionAvailable =
                actualEnvelope.CounterfactualState ==
                CharacterFootActualEnvelopeCounterfactualState
                    .UniqueInCorridor &&
                builderSwingTargetAvailable;
            float actualEnvelopeMinimumCorrection =
                actualEnvelopeCorrectionAvailable
                    ? actualEnvelope.MinimumHeightAlongUp -
                      originalSoleAlongUp
                    : 0f;
            float builderSwingTargetAlongUp =
                builderSwingTargetAvailable
                    ? Vector3.Dot(builderSwingTargetCorrection, motionUp)
                    : 0f;
            float actualEnvelopeAdvanceAboveBuilderTarget =
                actualEnvelopeCorrectionAvailable
                    ? Mathf.Max(
                        0f,
                        actualEnvelopeMinimumCorrection -
                        builderSwingTargetAlongUp)
                    : 0f;
            CharacterFootContactPlanePenetrationAvailability penetrationAvailability =
                ResolvePenetrationAvailability(in frame, in motion, in ik);
            var motionCoreSource = new CharacterFootMotionCoreCsvSource(
                in foot, in motion, baselineSampleAlongUp, envelopeSampleAlongUp,
                motionFormalFootHeight, rawFormalTargetHeight,
                envelopeMinimumCorrection, builderSelectedCorrection,
                builderSwingTargetAvailable, builderSwingTargetCorrection,
                horizontalAxisState, in actualEnvelope,
                actualEnvelopeCorrectionAvailable, actualEnvelopeMinimumCorrection,
                actualEnvelopeAdvanceAboveBuilderTarget, penetrationAvailability);
            CharacterFootSupportTargetDiagnostics selectedTarget = motion.SelectedSupportTarget;
            CharacterFootCurrentSupportDiagnostics currentSupport = foot.CurrentSupport;
            CharacterResolvedFootDiagnostics resolved = foot.Resolved;
            var goalSource = new CharacterFootGoalCsvSource(
                in footGoal, motion.Core.OriginalAnkle, frame.PelvisGoal);
            CharacterFootStrideHipsDiagnostics stride = frame.StrideHips;
            Vector3 expectedPhysicalPelvis = stride.Observation.AnimatedPelvisComponentPosition +
                frame.PelvisGoal.ComponentPosition * frame.PelvisGoal.PositionWeight;
            bool pelvisGoalResidualAvailable = ik.PhysicalWriteAvailable &&
                ik.PhysicalWriteCompletionIdentity == frame.CompletionIdentity &&
                stride.Observation.PoseInputAvailable && frame.PelvisGoal.PositionWeight > 0f;
            float pelvisGoalResidual = pelvisGoalResidualAvailable
                ? Vector3.Distance(ik.PhysicalPelvisComponentPosition, expectedPhysicalPelvis)
                : 0f;
            var pelvisSource = new CharacterFootPelvisCsvSource(
                in stride, frame.PelvisGoal.ComponentPosition,
                ik.PhysicalPelvisComponentPosition, ik.PhysicalPelvisWorldPosition,
                pelvisGoalResidualAvailable, pelvisGoalResidual);

            CharacterFullBodyIkLegPoseDiagnostics legPose = ik.Limb.LegPose;
            Vector3 finalAnkleWorldPosition = default;
            Quaternion finalAnkleWorldRotation = Quaternion.identity;
            CharacterFootPlacementSoleContactPose finalContacts = default;
            if (ik.PhysicalWriteAvailable)
            {
                finalAnkleWorldPosition = TransformComponentPoint(
                    roots,
                    ik.PhysicalAnkleComponentPosition);
                finalAnkleWorldRotation =
                    (roots.PoseRootWorldRotation *
                     ik.PhysicalAnkleComponentRotation).normalized;
                finalContacts = CharacterFootPlacementSoleContactPose.Resolve(
                    foot.SourceAnklePosition,
                    foot.SourceAnkleRotation,
                    foot.SourceHeelPosition,
                    foot.SourceToePosition,
                    finalAnkleWorldPosition,
                    finalAnkleWorldRotation);
            }
            float physicalGoalResidual =
                ik.PhysicalWriteAvailable && legPose.IsAvailable && foot.Goal.PositionWeight > 0f
                    ? Vector3.Distance(
                        ik.PhysicalAnkleComponentPosition,
                        ResolveWeightedAnkleComponentPosition(legPose.OriginalAnkle, in footGoal))
                    : 0f;
            var solverSource = new CharacterFootSolverCsvSource(
                in ik, finalAnkleWorldPosition, finalAnkleWorldRotation,
                finalContacts.HeelPosition, finalContacts.ToePosition, physicalGoalResidual);
            var sampleSource = new CharacterFootSampleCsvSource(
                in identitySource, in selectedStep, in currentCandidate,
                in incomingCandidate, in formalOutputSource, in outputEvents,
                in formalInputSource, in inputEvents, rootLocalLanding, in input,
                in primarySupport, in roots, in landingObservationSource,
                in ground, in motionCoreSource, in motion, in selectedTarget,
                in currentSupport, in resolved, in goalSource, in pelvisSource,
                in solverSource);
            CharacterFootSampleColumns.Schema.Write(row, in sampleSource);
            writer.WriteLine(row);
        }

        static CharacterFootActualEnvelopeIntersectionFact
            ResolveActualFootEnvelopeIntersection(
                in CharacterFootGroundPathDiagnostics ground,
                in CharacterFootSwingMotionDiagnostics motion,
                Vector3 up)
        {
            var result = new CharacterFootActualEnvelopeIntersectionFact
            {
                State = CharacterFootActualEnvelopeIntersectionState.Unavailable
            };
            if (!ground.Accepted ||
                motion.Core.State != CharacterFootSwingMotionState.Accepted ||
                motion.Core.ConstraintState != CharacterFootConstraintState.Swing ||
                ground.EnvelopeVertexCount < 2)
            {
                return result;
            }
            if (!float.IsFinite(up.x) ||
                !float.IsFinite(up.y) ||
                !float.IsFinite(up.z) ||
                up.sqrMagnitude <= 0.000001f)
            {
                result.State =
                    CharacterFootActualEnvelopeIntersectionState.InvalidComponentUp;
                return result;
            }
            Vector3 horizontalAxis = Vector3.ProjectOnPlane(
                ground.NextSwingLanding - ground.LastLanding,
                up);
            if (!float.IsFinite(horizontalAxis.x) ||
                !float.IsFinite(horizontalAxis.y) ||
                !float.IsFinite(horizontalAxis.z) ||
                horizontalAxis.sqrMagnitude <= 0.00000001f)
            {
                result.State =
                    CharacterFootActualEnvelopeIntersectionState.DegenerateAxis;
                return result;
            }
            Vector3 direction = horizontalAxis.normalized;
            float pathLength = horizontalAxis.magnitude;
            Vector3 actualHorizontalOffset = Vector3.ProjectOnPlane(
                motion.Core.OriginalSole - ground.LastLanding,
                up);
            result.ActualFootHorizontalDistance = Vector3.Dot(
                actualHorizontalOffset,
                direction);
            result.BaselineHorizontalDistance = Vector3.Dot(
                motion.Core.BaselineSample - ground.LastLanding,
                direction);
            result.EnvelopeHorizontalDistance = Vector3.Dot(
                motion.Core.EnvelopeSample - ground.LastLanding,
                direction);
            float rawPathParameter =
                result.ActualFootHorizontalDistance / pathLength;
            result.AxisRegion = result.ActualFootHorizontalDistance <
                                -ActualEnvelopeHorizontalEpsilonMeters
                ? CharacterFootActualFootAxisRegion.BeforePathStart
                : result.ActualFootHorizontalDistance >
                  pathLength + ActualEnvelopeHorizontalEpsilonMeters
                    ? CharacterFootActualFootAxisRegion.AfterPathEnd
                    : CharacterFootActualFootAxisRegion.WithinPathSegment;
            result.ClosestPathParameter = Mathf.Clamp01(rawPathParameter);
            result.DistanceAlongAxis =
                result.ClosestPathParameter * pathLength;
            Vector3 closestHorizontalOffset =
                horizontalAxis * result.ClosestPathParameter;
            result.CrossTrackDistance = Vector3.Distance(
                actualHorizontalOffset,
                closestHorizontalOffset);
            result.CorridorRadius = ground.Query.Radius;
            result.WithinGroundPathCorridor =
                float.IsFinite(result.CorridorRadius) &&
                result.CorridorRadius > 0f &&
                result.CrossTrackDistance <=
                result.CorridorRadius + ActualEnvelopeHorizontalEpsilonMeters;
            var heights = new List<float>(ground.EnvelopeVertexCount * 2);
            for (int i = 1; i < ground.EnvelopeVertexCount; i++)
            {
                CharacterFootGroundEnvelopeVertex previous =
                    ground.EnvelopeVertexAt(i - 1);
                CharacterFootGroundEnvelopeVertex current =
                    ground.EnvelopeVertexAt(i);
                float previousDistance = Vector3.Dot(
                    previous.Position - ground.LastLanding,
                    direction);
                float currentDistance = Vector3.Dot(
                    current.Position - ground.LastLanding,
                    direction);
                float minimumDistance = Mathf.Min(
                    previousDistance,
                    currentDistance);
                float maximumDistance = Mathf.Max(
                    previousDistance,
                    currentDistance);
                if (result.ActualFootHorizontalDistance <
                        minimumDistance - ActualEnvelopeHorizontalEpsilonMeters ||
                    result.ActualFootHorizontalDistance >
                        maximumDistance + ActualEnvelopeHorizontalEpsilonMeters)
                {
                    continue;
                }
                float previousHeight = Vector3.Dot(previous.Position, up);
                float currentHeight = Vector3.Dot(current.Position, up);
                float distanceDelta = currentDistance - previousDistance;
                if (Mathf.Abs(distanceDelta) <=
                    ActualEnvelopeHorizontalEpsilonMeters)
                {
                    if (Mathf.Abs(
                            result.ActualFootHorizontalDistance -
                            previousDistance) >
                        ActualEnvelopeHorizontalEpsilonMeters)
                    {
                        continue;
                    }
                    AddUniqueEnvelopeHeight(heights, previousHeight);
                    AddUniqueEnvelopeHeight(heights, currentHeight);
                    if (Mathf.Abs(currentHeight - previousHeight) >
                        ActualEnvelopeHeightEpsilonMeters)
                    {
                        result.HasVerticalEdge = true;
                    }
                    continue;
                }
                float interpolation =
                    (result.ActualFootHorizontalDistance -
                     previousDistance) / distanceDelta;
                AddUniqueEnvelopeHeight(
                    heights,
                    Mathf.Lerp(
                        previousHeight,
                        currentHeight,
                        Mathf.Clamp01(interpolation)));
            }
            if (heights.Count == 0)
            {
                result.State =
                    CharacterFootActualEnvelopeIntersectionState.NoIntersection;
                result.CounterfactualState =
                    result.WithinGroundPathCorridor
                        ? CharacterFootActualEnvelopeCounterfactualState
                            .NoIntersection
                        : CharacterFootActualEnvelopeCounterfactualState
                            .OutsideGroundPathCorridor;
                return result;
            }
            result.CandidateCount = heights.Count;
            result.MinimumHeightAlongUp = heights.Min();
            result.MaximumHeightAlongUp = heights.Max();
            result.HeightSpan = result.MaximumHeightAlongUp -
                                result.MinimumHeightAlongUp;
            result.HasMultipleHeights = heights.Count > 1 &&
                result.HeightSpan > ActualEnvelopeHeightEpsilonMeters;
            result.Ambiguous = result.HasVerticalEdge ||
                               result.HasMultipleHeights;
            result.State = result.Ambiguous
                ? CharacterFootActualEnvelopeIntersectionState
                    .AmbiguousEnvelopeAtActualFootDistance
                : CharacterFootActualEnvelopeIntersectionState.Unique;
            result.CounterfactualState = !result.WithinGroundPathCorridor
                ? CharacterFootActualEnvelopeCounterfactualState
                    .OutsideGroundPathCorridor
                : result.Ambiguous
                    ? CharacterFootActualEnvelopeCounterfactualState
                        .AmbiguousInCorridor
                    : CharacterFootActualEnvelopeCounterfactualState
                        .UniqueInCorridor;
            return result;
        }

        static void AddUniqueEnvelopeHeight(
            List<float> heights,
            float value)
        {
            if (!float.IsFinite(value))
                return;
            for (int i = 0; i < heights.Count; i++)
            {
                if (Mathf.Abs(heights[i] - value) <=
                    ActualEnvelopeHeightEpsilonMeters)
                {
                    return;
                }
            }
            heights.Add(value);
        }

        static void WriteGeometryRows(
            SamplingSession session,
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot)
        {
            CharacterFootGroundPathDiagnostics ground = foot.GroundPath;
            CharacterFootGroundSurfaceDiagnostics surfaces = ground.SurfaceCoverage;
            int rowCount = Math.Max(
                surfaces.Count,
                Math.Max(ground.ContactCount, ground.EnvelopeVertexCount));
            for (int index = 0; index < rowCount; index++)
            {
                row.Clear();
                Add(row, session.SampleIdentity.ToString("N"));
                Add(row, frame.FrameSequence);
                Add(row, frame.CompletionIdentity);
                Add(row, foot.Side.ToString());
                Add(row, ground.InputIdentity);
                bool hasContact = index < ground.ContactCount;
                CharacterFootGroundContact contact = hasContact
                    ? ground.ContactAt(index)
                    : default;
                Add(row, hasContact ? index : -1);
                Add(row, hasContact ? contact.SegmentIndex : -1);
                Add(row, contact.SurfaceIdentity);
                Add(row, contact.CandidateIdentity);
                Add(row, contact.Position);
                Add(row, contact.Normal);
                Add(row, contact.QueryDistance);
                bool hasEnvelopeVertex = index < ground.EnvelopeVertexCount;
                CharacterFootGroundEnvelopeVertex envelopeVertex = hasEnvelopeVertex
                    ? ground.EnvelopeVertexAt(index)
                    : default;
                Add(row, hasEnvelopeVertex ? index : -1);
                Add(row, envelopeVertex.Position);
                bool hasSurface = index < surfaces.Count;
                CharacterFootGroundSurfaceSegment surface = hasSurface
                    ? surfaces.SegmentAt(index)
                    : default;
                Add(row, hasSurface ? index : -1);
                Add(row, surface.SurfaceIdentity);
                Add(row, hasSurface ? surface.FaceIdentity : -1);
                Add(row, surface.Start.x);
                Add(row, surface.Start.y);
                Add(row, surface.End.x);
                Add(row, surface.End.y);
                writer.WriteLine(row);
            }
        }

        static Vector3 ResolveWeightedAnkleComponentPosition(
            Vector3 originalComponentPosition,
            in CharacterFullBodyIkGoal goal)
        {
            return originalComponentPosition +
                   (goal.ComponentPosition - originalComponentPosition) *
                   goal.PositionWeight;
        }

        static Vector3 TransformComponentPoint(
            in RootHierarchyCapture roots,
            Vector3 componentPoint) =>
            roots.PoseRootWorldPosition +
            roots.PoseRootWorldRotation *
            Vector3.Scale(componentPoint, roots.PoseRootLossyScale);

        static CharacterFootContactPlanePenetrationAvailability
            ResolvePenetrationAvailability(
                in CharacterFootLandingPredictionDiagnostics frame,
                in CharacterFootSwingMotionDiagnostics motion,
                in FootIkCapture ik)
        {
            if (!ik.PhysicalWriteAvailable ||
                ik.PhysicalWriteCompletionIdentity != frame.CompletionIdentity)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .FinalPhysicalPoseUnavailable;
            }
            if (motion.Core.ConstraintState != CharacterFootConstraintState.Landing &&
                motion.Core.ConstraintState != CharacterFootConstraintState.Locked)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .ContactLifecycleUnavailable;
            }
            if (!motion.Core.ContactPlaneAvailable)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .ContactPlaneUnavailable;
            }
            if (motion.Core.LandingEventIdentity == 0)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .EventLineageMismatch;
            }
            if (motion.Core.ContactSurfaceIdentity == 0)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .SurfaceLineageMismatch;
            }
            Vector3 normal = motion.Core.ContactPlaneNormal;
            if (!float.IsFinite(normal.x) ||
                !float.IsFinite(normal.y) ||
                !float.IsFinite(normal.z) ||
                normal.sqrMagnitude <= 0.000001f)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .InvalidContactNormal;
            }
            return CharacterFootContactPlanePenetrationAvailability.Available;
        }

    }
}
