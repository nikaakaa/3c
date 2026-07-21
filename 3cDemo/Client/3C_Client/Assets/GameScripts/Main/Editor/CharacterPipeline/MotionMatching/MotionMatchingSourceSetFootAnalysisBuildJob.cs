using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public readonly struct MotionMatchingFootAnalysisBuildProgress
    {
        public MotionMatchingFootAnalysisBuildProgress(CharacterMotionMatchingSourceClipId sourceClipId, int completed, int total)
        {
            SourceClipId = sourceClipId;
            Completed = completed;
            Total = total;
        }

        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public int Completed { get; }
        public int Total { get; }
    }

    public sealed class MotionMatchingSourceSetFootAnalysisBuildRequest
    {
        readonly AnimationClip[] m_TargetClips;

        MotionMatchingSourceSetFootAnalysisBuildRequest(
            CharacterMotionMatchingSourceSet sourceSet,
            CharacterFootPlacementAnalysisSource analysisSource,
            AnimationClip[] targetClips,
            StableHash snapshotHash,
            int readyCount,
            int missingCount,
            int staleCount,
            int estimatedSampleCount)
        {
            SourceSet = sourceSet;
            AnalysisSource = analysisSource;
            m_TargetClips = targetClips;
            SnapshotHash = snapshotHash;
            ReadyCount = readyCount;
            MissingCount = missingCount;
            StaleCount = staleCount;
            EstimatedSampleCount = estimatedSampleCount;
        }

        public CharacterMotionMatchingSourceSet SourceSet { get; }
        public CharacterFootPlacementAnalysisSource AnalysisSource { get; }
        public StableHash SnapshotHash { get; }
        public int ReadyCount { get; }
        public int MissingCount { get; }
        public int StaleCount { get; }
        public int EstimatedSampleCount { get; }
        public int TargetClipCount => m_TargetClips.Length;
        public AnimationClip GetTargetClip(int index) => m_TargetClips[index];

        public static MotionMatchingSourceSetFootAnalysisBuildRequest Create(
            CharacterMotionMatchingSourceSet sourceSet,
            CharacterFootPlacementAnalysisSource analysisSource)
        {
            if (!sourceSet)
                throw new ArgumentNullException(nameof(sourceSet));
            if (!analysisSource)
                throw new ArgumentNullException(nameof(analysisSource));
            sourceSet.RequireValid();
            analysisSource.RequireValid();
            var ordered = sourceSet.SourceClips.OrderBy(value => value.SourceClipId).ToArray();
            var targets = new List<AnimationClip>();
            var snapshotParts = new List<string>
            {
                DependencyHash(sourceSet),
                DependencyHash(analysisSource)
            };
            int ready = 0;
            int missing = 0;
            int stale = 0;
            int estimatedSamples = 0;
            for (int i = 0; i < ordered.Length; i++)
            {
                AnimationClip clip = MotionMatchingSourceClipResolver.Resolve(ordered[i]);
                string dependency = MotionMatchingSourceClipResolver.DependencyHash(clip);
                snapshotParts.Add(ordered[i].SourceClipId.Value);
                snapshotParts.Add(dependency);
                AnimationFootAnalysisArtifactInspection inspection = AnimationFootAnalysisArtifactBuilder.Inspect(clip, analysisSource);
                switch (inspection.Status)
                {
                    case AnimationFootAnalysisArtifactStatus.Ready:
                        ready++;
                        break;
                    case AnimationFootAnalysisArtifactStatus.Missing:
                        missing++;
                        targets.Add(clip);
                        break;
                    case AnimationFootAnalysisArtifactStatus.Stale:
                        stale++;
                        targets.Add(clip);
                        break;
                    default:
                        throw new InvalidOperationException($"Clip '{ordered[i].SourceClipId}' Foot Analysis Artifact is corrupt: {inspection.Error}");
                }
                estimatedSamples += Mathf.Max(2, Mathf.CeilToInt(clip.length * analysisSource.SampleRate));
            }
            return new MotionMatchingSourceSetFootAnalysisBuildRequest(
                sourceSet, analysisSource, targets.ToArray(), StableHash.Compute(snapshotParts.ToArray()),
                ready, missing, stale, estimatedSamples);
        }

        public void RequireUnchanged()
        {
            MotionMatchingSourceSetFootAnalysisBuildRequest current = Create(SourceSet, AnalysisSource);
            if (!SnapshotHash.Equals(current.SnapshotHash))
                throw new InvalidOperationException("Source Set Foot Analysis inputs changed while the job was running.");
        }

        static string DependencyHash(UnityEngine.Object value)
        {
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException($"Foot Analysis input '{value}' is not a persisted asset.");
            return AssetDatabase.GetAssetDependencyHash(path).ToString();
        }
    }

    public sealed class MotionMatchingSourceSetFootAnalysisBuildJob : IDisposable
    {
        static MotionMatchingSourceSetFootAnalysisBuildJob s_Active;
        readonly MotionMatchingSourceSetFootAnalysisBuildRequest m_Request;
        int m_Index;
        bool m_Disposed;

        MotionMatchingSourceSetFootAnalysisBuildJob(MotionMatchingSourceSetFootAnalysisBuildRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public static MotionMatchingSourceSetFootAnalysisBuildJob Active => s_Active;
        public MotionMatchingSourceSetFootAnalysisBuildRequest Request => m_Request;
        public bool IsComplete { get; private set; }
        public bool IsCanceled { get; private set; }
        public Exception Failure { get; private set; }
        public event Action<MotionMatchingFootAnalysisBuildProgress> Progress;
        public event Action<MotionMatchingSourceSetFootAnalysisBuildJob> Finished;

        public static MotionMatchingSourceSetFootAnalysisBuildJob Start(MotionMatchingSourceSetFootAnalysisBuildRequest request)
        {
            if (s_Active != null)
                throw new InvalidOperationException("A Source Set Foot Analysis job is already active.");
            var job = new MotionMatchingSourceSetFootAnalysisBuildJob(request);
            s_Active = job;
            EditorApplication.update += job.Tick;
            AssemblyReloadEvents.beforeAssemblyReload += job.CancelForReload;
            return job;
        }

        public void Cancel()
        {
            if (IsComplete || m_Disposed)
                return;
            IsCanceled = true;
            Complete();
        }

        void Tick()
        {
            if (m_Disposed)
                return;
            try
            {
                if (m_Index >= m_Request.TargetClipCount)
                {
                    m_Request.RequireUnchanged();
                    Complete();
                    return;
                }
                AnimationClip clip = m_Request.GetTargetClip(m_Index);
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Build Source Set Foot Analysis",
                    $"{m_Index + 1}/{m_Request.TargetClipCount}  {clip.name}",
                    m_Request.TargetClipCount == 0 ? 1f : m_Index / (float)m_Request.TargetClipCount))
                {
                    Cancel();
                    return;
                }
                m_Request.RequireUnchanged();
                AnimationFootAnalysisArtifactBuilder.Build(clip, m_Request.AnalysisSource);
                m_Index++;
                Progress?.Invoke(new MotionMatchingFootAnalysisBuildProgress(
                    FindSourceClipId(clip), m_Index, m_Request.TargetClipCount));
            }
            catch (Exception exception)
            {
                Failure = exception;
                Complete();
            }
        }

        CharacterMotionMatchingSourceClipId FindSourceClipId(AnimationClip clip)
        {
            for (int i = 0; i < m_Request.SourceSet.SourceClips.Count; i++)
            {
                CharacterMotionMatchingSourceClipEntry entry = m_Request.SourceSet.SourceClips[i];
                if (MotionMatchingSourceClipResolver.Resolve(entry) == clip)
                    return entry.SourceClipId;
            }
            throw new InvalidOperationException("Completed Foot Analysis Clip no longer belongs to the Source Set.");
        }

        void CancelForReload()
        {
            IsCanceled = true;
            Complete();
        }

        void Complete()
        {
            if (IsComplete)
                return;
            IsComplete = true;
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
            EditorUtility.ClearProgressBar();
            if (ReferenceEquals(s_Active, this))
                s_Active = null;
        }
    }
}
