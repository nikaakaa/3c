using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal sealed class CharacterPoseSourceModule : IDisposable
    {
        readonly AnimancerComponent m_Animancer;
        readonly AnimancerPoseSamplingBackend m_Backend;
        readonly PhysicalPoseSourceRegistry m_PhysicalSources;
        readonly HashSet<AnimationPhysicalSourceIdentity>
            m_ReleaseValidationIdentities;
        readonly Playable m_PreviousOutputSource;
        readonly float m_PreviousOutputWeight;
        AnimationMixerPlayable m_SourceFanIn;
        bool m_Disposed;

        internal CharacterPoseSourceModule(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig,
            int sourceCapacity,
            int clipCapacity)
        {
            m_Animancer = animancer
                ? animancer
                : throw new ArgumentNullException(nameof(animancer));
            var physicalSources = new PhysicalPoseSourceRegistry(
                sourceCapacity);
            AnimancerPoseSamplingBackend backend = null;
            AnimationMixerPlayable sourceFanIn = default;
            Playable previousOutputSource = default;
            float previousOutputWeight = 1f;
            try
            {
                backend = new AnimancerPoseSamplingBackend(
                    animancer,
                    rigBinding,
                    rig,
                    sourceCapacity,
                    clipCapacity);
                PlayableGraph graph = animancer.Graph.PlayableGraph;
                if (!graph.IsValid())
                {
                    throw new InvalidOperationException(
                        "Animation Pose Graph requires a valid Animancer PlayableGraph.");
                }
                sourceFanIn = AnimationMixerPlayable.Create(
                    graph,
                    checked(sourceCapacity + 1));
                PlayableOutput output = animancer.Graph.Output;
                previousOutputSource = output.GetSourcePlayable();
                previousOutputWeight = output.GetWeight();
                animancer.Graph.InsertOutputPlayable(sourceFanIn);
                sourceFanIn.SetInputWeight(0, 1f);
                output.SetWeight(0f);
            }
            catch
            {
                if (sourceFanIn.IsValid() &&
                    animancer && animancer.IsGraphInitialized)
                {
                    PlayableOutput output = animancer.Graph.Output;
                    if (output.IsOutputValid())
                    {
                        output.SetSourcePlayable(previousOutputSource);
                        output.SetWeight(previousOutputWeight);
                    }
                }
                if (sourceFanIn.IsValid())
                    sourceFanIn.Destroy();
                backend?.Dispose();
                physicalSources.Dispose();
                throw;
            }

            m_Backend = backend;
            m_PhysicalSources = physicalSources;
            m_ReleaseValidationIdentities =
                new HashSet<AnimationPhysicalSourceIdentity>(
                    sourceCapacity);
            m_SourceFanIn = sourceFanIn;
            m_PreviousOutputSource = previousOutputSource;
            m_PreviousOutputWeight = previousOutputWeight;
        }

        internal int Capacity => m_PhysicalSources.Capacity;
        internal bool HasBackendFrame => m_Backend.HasOpenFrame;
        internal bool HasPhysicalFrame => m_PhysicalSources.HasOpenFrame;
        internal int PendingRegistrationCount =>
            m_PhysicalSources.PendingRegistrationCount;

        internal CharacterPoseSourceFrameLease BeginFrame(
            in CharacterPoseFrameLineage lineage) =>
            m_Backend.BeginFrame(in lineage);

        internal void BeginPhysicalFrame() =>
            m_PhysicalSources.BeginFrame();

        internal void BindDemand(
            CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourceDemand demand) =>
            m_Backend.BindDemand(lease, in demand);

        internal CharacterPoseSourceDemand RequireDemand(
            CharacterPoseSourceFrameLease lease) =>
            m_Backend.RequireDemand(lease);

        internal void BindResult(
            CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourceFrameResult result) =>
            m_Backend.BindResult(lease, in result);

        internal void RequirePendingOpen(
            CharacterPoseSourceFrameLease lease) =>
            m_Backend.RequirePendingOpen(lease);

        internal void RequirePendingReady(
            CharacterPoseSourceFrameLease lease) =>
            m_Backend.RequirePendingReady(lease);

        internal bool ContainsCommitted(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId) =>
            m_Backend.ContainsCommitted(sourceId, poseNodeId);

        internal AnimationPhysicalSourceIdentity PrepareAndConnect(
            in AnimationPoseSampleRequest request,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            AnimationPhysicalSourceIdentity physical =
                m_PhysicalSources.Register(
                    request.SourceId,
                    poseNodeId,
                    request.SourceOwnerIndex);
            AnimationPoseSourcePrepareResult prepared =
                m_Backend.PrepareOrUpdate(
                    in request,
                    clipCatalog,
                    in capture,
                    poseNodeId);
            Connect(physical, prepared);
            return physical;
        }

        internal AnimationPhysicalSourceIdentity PrepareAndConnect(
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            AnimationPhysicalSourceIdentity physical =
                m_PhysicalSources.Register(
                    sourceId,
                    poseNodeId,
                    sourceOwnerIndex);
            AnimationPoseSourcePrepareResult prepared =
                m_Backend.PrepareOrUpdate(
                    sourceId,
                    clips,
                    clipCatalog,
                    in capture,
                    poseNodeId);
            Connect(physical, prepared);
            return physical;
        }

        internal void ValidatePhysicalFrame() =>
            m_PhysicalSources.ValidateFrame();

        internal void ValidateFrame(
            CharacterPoseSourceFrameLease lease) =>
            m_Backend.ValidateFrame(lease);

        internal void EnterEvaluateBarrier(
            CharacterPoseSourceFrameLease lease) =>
            m_Backend.EnterEvaluateBarrier(lease);

        internal void CommitFrame(
            CharacterPoseSourceFrameLease lease)
        {
            m_Backend.CommitFrame(lease);
            m_PhysicalSources.CommitFrame();
        }

        internal void DiscardBackendFrame(
            CharacterPoseSourceFrameLease lease) =>
            m_Backend.DiscardFrame(lease);

        internal void DiscardPhysicalFrame() =>
            m_PhysicalSources.DiscardFrame();

        internal AnimationPhysicalSourceIdentity GetPendingRegistration(
            int ordinal) =>
            m_PhysicalSources.GetPendingRegistration(ordinal);

        internal AnimationPhysicalSourceIdentity RequireIdentity(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId) =>
            m_PhysicalSources.RequireIdentity(sourceId, poseNodeId);

        internal AnimationPoseSourceId RequireSourceId(
            AnimationPhysicalSourceIdentity physical) =>
            m_PhysicalSources.RequireSourceId(physical);

        internal PoseNodeId RequirePoseNodeId(
            AnimationPhysicalSourceIdentity physical) =>
            m_PhysicalSources.RequirePoseNodeId(physical);

        internal int RequireSourceOwnerIndex(
            AnimationPhysicalSourceIdentity physical) =>
            m_PhysicalSources.RequireSourceOwnerIndex(physical);

        internal AnimationPhysicalSourceIdentity ValidateRelease(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            AnimationPhysicalSourceIdentity expected)
        {
            AnimationPhysicalSourceIdentity current =
                m_PhysicalSources.RequireIdentity(
                    sourceId,
                    poseNodeId);
            if (expected.IsValid && current != expected)
            {
                throw new InvalidOperationException(
                    "Pose source release physical identity is stale.");
            }
            int port = checked(current.Index.Value + 1);
            if (port <= 0 ||
                port >= m_SourceFanIn.GetInputCount() ||
                !m_Backend.ContainsCommitted(sourceId, poseNodeId) ||
                !m_ReleaseValidationIdentities.Add(current))
            {
                throw new InvalidOperationException(
                    "Pose source release backend identity is not exact.");
            }
            return current;
        }

        internal void ClearReleaseValidation() =>
            m_ReleaseValidationIdentities.Clear();

        internal AnimationPhysicalSourceReleaseToken PrepareRelease(
            AnimationPhysicalSourceIdentity physical,
            AnimationPoseSourceId sourceId) =>
            m_PhysicalSources.PrepareRelease(physical, sourceId);

        internal AnimationPoseSourceReleaseToken StageRelease(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId) =>
            m_Backend.StageRelease(sourceId, poseNodeId);

        internal void Release(
            in AnimationPoseSourceReleaseToken release) =>
            m_Backend.Release(in release);

        internal void ApplyPreparedRelease(
            in AnimationPhysicalSourceReleaseToken release) =>
            m_PhysicalSources.ApplyPreparedRelease(in release);

        internal void Disconnect(
            AnimationPhysicalSourceIdentity physical)
        {
            int port = checked(physical.Index.Value + 1);
            if (m_SourceFanIn.GetInput(port).IsValid())
                m_SourceFanIn.DisconnectInput(port);
            m_SourceFanIn.SetInputWeight(port, 0f);
        }

        internal void BeginReleaseDiagnostics(bool recordDiagnostics) =>
            m_PhysicalSources.BeginReleaseDiagnostics(recordDiagnostics);

        internal void RequireReleaseDiagnosticsCapacity(int required)
        {
            if (required > m_PhysicalSources.ReleaseDiagnosticsCapacity)
            {
                throw new InvalidOperationException(
                    "Animation diagnostics release capacity was exceeded.");
            }
        }

        internal void RecordRelease(
            PoseNodeId poseNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity) =>
            m_PhysicalSources.RecordRelease(
                poseNodeId,
                sourceId,
                completionIdentity);

        internal void CompleteDeferredReleases()
        {
            m_Backend.ExecuteDeferredReleases();
            m_PhysicalSources.CompleteReleaseDiagnostics();
        }

        internal void CancelReleaseDiagnostics() =>
            m_PhysicalSources.CancelReleaseDiagnostics();

        internal CharacterPoseSourceCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
                in CharacterPoseSourceFrameResult sourceFrame) =>
                m_PhysicalSources.CaptureCommittedDiagnostics(
                    in sourceFrame);

        internal ClipSamplePlan RequireDominantClipSample(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            ulong completionIdentity) =>
            m_Backend.RequireDominantClipSample(
                sourceId,
                poseNodeId,
                completionIdentity);

        internal void Clear()
        {
            m_Backend.Clear();
            m_PhysicalSources.Reset();
            for (int port = 1;
                 port < m_SourceFanIn.GetInputCount();
                 port++)
            {
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
            }
            m_ReleaseValidationIdentities.Clear();
        }

        void Connect(
            AnimationPhysicalSourceIdentity physical,
            AnimationPoseSourcePrepareResult prepared)
        {
            int port = checked(physical.Index.Value + 1);
            Playable current = m_SourceFanIn.GetInput(port);
            if (current.IsValid() && current.Equals(prepared.Output))
            {
                m_SourceFanIn.SetInputWeight(port, 1f);
                return;
            }
            if (current.IsValid())
                m_SourceFanIn.DisconnectInput(port);
            m_SourceFanIn.GetGraph().Connect(
                prepared.Output,
                0,
                m_SourceFanIn,
                port);
            m_SourceFanIn.SetInputWeight(port, 1f);
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseSourceModule));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            RequireAlive();
            Exception failure = null;
            DisposeStep(m_Backend.Dispose, ref failure);
            DisposeStep(RestoreOutputAndDestroyFanIn, ref failure);
            DisposeStep(m_PhysicalSources.Dispose, ref failure);
            m_ReleaseValidationIdentities.Clear();
            m_Disposed = true;
            if (failure != null)
                throw failure;
        }

        void RestoreOutputAndDestroyFanIn()
        {
            if (!m_SourceFanIn.IsValid() ||
                !m_Animancer ||
                !m_Animancer.IsGraphInitialized)
            {
                return;
            }
            PlayableOutput output = m_Animancer.Graph.Output;
            if (output.IsOutputValid() &&
                output.GetSourcePlayable().Equals(m_SourceFanIn))
            {
                output.SetSourcePlayable(m_PreviousOutputSource);
                output.SetWeight(m_PreviousOutputWeight);
            }
            m_SourceFanIn.Destroy();
            m_SourceFanIn = default;
        }

        static void DisposeStep(
            Action action,
            ref Exception failure)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }
    }
}
