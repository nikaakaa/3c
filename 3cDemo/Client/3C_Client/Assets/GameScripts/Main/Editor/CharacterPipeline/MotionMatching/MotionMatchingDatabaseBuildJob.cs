using System;
using System.IO;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public enum MotionMatchingDatabaseBuildStage : byte
    {
        Preflight = 1,
        Sampling = 2,
        Normalization = 3,
        Index = 4,
        Coverage = 5,
        Publish = 6,
        Complete = 7
    }

    public readonly struct MotionMatchingDatabaseBuildProgress
    {
        public MotionMatchingDatabaseBuildProgress(
            MotionMatchingDatabaseBuildStage stage,
            int completed,
            int total,
            ThirdPersonSimulation.StableHash inputIdentity)
        {
            Stage = stage;
            Completed = completed;
            Total = total;
            InputIdentity = inputIdentity;
        }

        public MotionMatchingDatabaseBuildStage Stage { get; }
        public int Completed { get; }
        public int Total { get; }
        public ThirdPersonSimulation.StableHash InputIdentity { get; }
    }

    public sealed class MotionMatchingDatabaseBuildResult
    {
        public MotionMatchingDatabaseBuildResult(
            bool succeeded,
            bool canceled,
            CharacterMotionMatchingDatabaseArtifact artifact,
            MotionMatchingDatabaseBuildStage finalStage,
            string diagnostic)
        {
            if (succeeded == canceled || succeeded != (artifact != null))
            {
                if (!succeeded && !canceled && artifact == null)
                {
                }
                else
                {
                    throw new ArgumentException("Motion Matching Database Build Result state is inconsistent.");
                }
            }
            Succeeded = succeeded;
            Canceled = canceled;
            Artifact = artifact;
            FinalStage = finalStage;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool Succeeded { get; }
        public bool Canceled { get; }
        public CharacterMotionMatchingDatabaseArtifact Artifact { get; }
        public MotionMatchingDatabaseBuildStage FinalStage { get; }
        public string Diagnostic { get; }
    }

    public sealed class MotionMatchingDatabaseBuildJob : IDisposable
    {
        const int SamplesPerUpdate = 4;
        const int FeaturesPerUpdate = 4;
        const int NodesPerUpdate = 16;
        const int RequirementsPerUpdate = 1;

        static MotionMatchingDatabaseBuildJob s_Active;
        readonly MotionMatchingDatabaseBuildRequest m_Request;
        MotionMatchingDatabaseSampler m_Sampler;
        MotionMatchingSampleAddress[] m_Addresses;
        MotionMatchingSampleBuildRecord[] m_Samples;
        MotionMatchingSegmentPayload[] m_Segments;
        MotionMatchingNormalizationBuildState m_Normalization;
        MotionMatchingSearchIndexBuildState m_Index;
        MotionMatchingCoverageBuildState m_Coverage;
        int m_SampleCursor;
        bool m_Disposed;

        MotionMatchingDatabaseBuildJob(MotionMatchingDatabaseBuildRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            Stage = MotionMatchingDatabaseBuildStage.Preflight;
        }

        public static MotionMatchingDatabaseBuildJob Active => s_Active;
        public MotionMatchingDatabaseBuildRequest Request => m_Request;
        public MotionMatchingDatabaseBuildStage Stage { get; private set; }
        public MotionMatchingDatabaseBuildResult Result { get; private set; }
        public bool IsComplete => Result != null;
        public event Action<MotionMatchingDatabaseBuildProgress> Progress;
        public event Action<MotionMatchingDatabaseBuildJob> Finished;

        public static MotionMatchingDatabaseBuildJob Start(MotionMatchingDatabaseBuildRequest request)
        {
            if (s_Active != null)
                throw new InvalidOperationException("A Motion Matching Database Build job is already active.");
            var job = new MotionMatchingDatabaseBuildJob(request);
            s_Active = job;
            EditorApplication.update += job.Tick;
            AssemblyReloadEvents.beforeAssemblyReload += job.CancelForReload;
            return job;
        }

        public void Cancel()
        {
            if (IsComplete || m_Disposed)
                return;
            Finish(new MotionMatchingDatabaseBuildResult(false, true, null, Stage, "Motion Matching Database Build was canceled."));
        }

        void Tick()
        {
            if (m_Disposed || IsComplete)
                return;
            try
            {
                MotionMatchingDatabaseBuildProgress progress = CurrentProgress();
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Build Motion Matching Database",
                    $"{m_Request.Database.DatabaseId}  {progress.Stage}  {progress.Completed}/{progress.Total}",
                    progress.Total <= 0 ? 0f : progress.Completed / (float)progress.Total))
                {
                    Cancel();
                    return;
                }
                switch (Stage)
                {
                    case MotionMatchingDatabaseBuildStage.Preflight:
                        RunPreflight();
                        break;
                    case MotionMatchingDatabaseBuildStage.Sampling:
                        RunSampling();
                        break;
                    case MotionMatchingDatabaseBuildStage.Normalization:
                        RunNormalization();
                        break;
                    case MotionMatchingDatabaseBuildStage.Index:
                        RunIndex();
                        break;
                    case MotionMatchingDatabaseBuildStage.Coverage:
                        RunCoverage();
                        break;
                    case MotionMatchingDatabaseBuildStage.Publish:
                        RunPublish();
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported Motion Matching Build stage '{Stage}'.");
                }
                if (!IsComplete)
                    Progress?.Invoke(CurrentProgress());
            }
            catch (Exception exception)
            {
                Finish(new MotionMatchingDatabaseBuildResult(false, false, null, Stage, exception.Message));
            }
        }

        void RunPreflight()
        {
            m_Addresses = MotionMatchingDatabaseSampler.CreateAddresses(m_Request);
            m_Samples = new MotionMatchingSampleBuildRecord[m_Addresses.Length];
            m_Sampler = new MotionMatchingDatabaseSampler(m_Request);
            Stage = MotionMatchingDatabaseBuildStage.Sampling;
        }

        void RunSampling()
        {
            int end = Math.Min(m_Addresses.Length, m_SampleCursor + SamplesPerUpdate);
            while (m_SampleCursor < end)
            {
                m_Samples[m_SampleCursor] = m_Sampler.Sample(m_Addresses[m_SampleCursor]);
                m_SampleCursor++;
            }
            if (m_SampleCursor < m_Addresses.Length)
                return;
            m_Sampler.Dispose();
            m_Sampler = null;
            m_Segments = MotionMatchingContinuationCompiler.Compile(m_Request, m_Samples);
            m_Normalization = new MotionMatchingNormalizationBuildState(m_Samples, m_Request.FeatureSchema.DenseFeatureCount);
            Stage = MotionMatchingDatabaseBuildStage.Normalization;
        }

        void RunNormalization()
        {
            m_Normalization.Step(FeaturesPerUpdate);
            if (!m_Normalization.IsComplete)
                return;
            m_Index = new MotionMatchingSearchIndexBuildState(
                m_Samples, m_Normalization.NormalizedFeatures, m_Normalization.Active,
                m_Request.FeatureSchema.DenseFeatureCount, m_Request.SearchPolicy.LeafCapacity,
                m_Request.SearchPolicy.MaximumTreeDepth);
            Stage = MotionMatchingDatabaseBuildStage.Index;
        }

        void RunIndex()
        {
            m_Index.Step(NodesPerUpdate);
            if (!m_Index.IsComplete)
                return;
            m_Coverage = new MotionMatchingCoverageBuildState(m_Request, m_Samples);
            Stage = MotionMatchingDatabaseBuildStage.Coverage;
        }

        void RunCoverage()
        {
            m_Coverage.Step(RequirementsPerUpdate);
            if (m_Coverage.IsComplete)
                Stage = MotionMatchingDatabaseBuildStage.Publish;
        }

        void RunPublish()
        {
            MotionMatchingDatabaseBuildRequest current = MotionMatchingDatabaseBuildRequestFactory.Create(
                m_Request.Profile, m_Request.Database, m_Request.AnalysisSource);
            if (!current.InputSnapshotHash.Equals(m_Request.InputSnapshotHash))
                throw new InvalidOperationException("Motion Matching Database dependencies changed while the Build job was running.");
            CharacterMotionMatchingDatabaseArtifact artifact = MotionMatchingDatabaseArtifactFactory.Create(
                m_Request, m_Samples, m_Segments, m_Normalization, m_Index, m_Coverage.GetSummaries());
            CharacterMotionMatchingDatabaseArtifact published = CharacterMotionMatchingDatabaseArtifactStore.Publish(
                m_Request.Database, artifact, m_Request.CandidatePath);
            Stage = MotionMatchingDatabaseBuildStage.Complete;
            Finish(new MotionMatchingDatabaseBuildResult(true, false, published, Stage, string.Empty));
        }

        MotionMatchingDatabaseBuildProgress CurrentProgress()
        {
            switch (Stage)
            {
                case MotionMatchingDatabaseBuildStage.Preflight:
                    return NewProgress(0, 1);
                case MotionMatchingDatabaseBuildStage.Sampling:
                    return NewProgress(m_SampleCursor, m_Addresses?.Length ?? 0);
                case MotionMatchingDatabaseBuildStage.Normalization:
                    return NewProgress(m_Normalization?.CompletedFeatures ?? 0, m_Normalization?.TotalFeatures ?? 0);
                case MotionMatchingDatabaseBuildStage.Index:
                    return NewProgress(m_Index?.CompletedNodes ?? 0, m_Index?.DiscoveredNodes ?? 0);
                case MotionMatchingDatabaseBuildStage.Coverage:
                    return NewProgress(m_Coverage?.CompletedRequirements ?? 0, m_Coverage?.TotalRequirements ?? 0);
                case MotionMatchingDatabaseBuildStage.Publish:
                    return NewProgress(0, 1);
                default:
                    return NewProgress(1, 1);
            }
        }

        MotionMatchingDatabaseBuildProgress NewProgress(int completed, int total) =>
            new MotionMatchingDatabaseBuildProgress(Stage, completed, total, m_Request.InputSnapshotHash);

        void CancelForReload()
        {
            if (!IsComplete)
                Finish(new MotionMatchingDatabaseBuildResult(false, true, null, Stage, "Motion Matching Database Build stopped for domain reload."));
        }

        void Finish(MotionMatchingDatabaseBuildResult result)
        {
            if (Result != null)
                return;
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Dispose();
            Finished?.Invoke(this);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            EditorApplication.update -= Tick;
            AssemblyReloadEvents.beforeAssemblyReload -= CancelForReload;
            m_Sampler?.Dispose();
            m_Sampler = null;
            EditorUtility.ClearProgressBar();
            if (File.Exists(m_Request.CandidatePath))
                File.Delete(m_Request.CandidatePath);
            if (ReferenceEquals(s_Active, this))
                s_Active = null;
        }
    }
}
