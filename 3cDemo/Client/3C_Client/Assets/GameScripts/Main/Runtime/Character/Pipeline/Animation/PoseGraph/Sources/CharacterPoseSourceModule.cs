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
        struct SourceFramePage
        {
            CharacterPoseSourceFrameLease m_Lease;
            CharacterPoseSourceDemand m_Demand;
            CharacterPoseSourceFrameResult m_Result;
            bool m_HasDemand;
            bool m_HasResult;

            internal bool HasOpenFrame => m_Lease.IsValid;

            internal CharacterPoseSourceFrameLease Begin(
                in CharacterPoseFrameLineage lineage)
            {
                if (HasOpenFrame)
                {
                    throw new InvalidOperationException(
                        "Pose Source Pending page is already open.");
                }
                m_Lease = new CharacterPoseSourceFrameLease(
                    in lineage);
                m_Demand = default;
                m_Result = default;
                m_HasDemand = false;
                m_HasResult = false;
                return m_Lease;
            }

            internal void BindDemand(
                CharacterPoseSourceFrameLease lease,
                in CharacterPoseSourceDemand demand)
            {
                RequireLease(lease);
                if (m_HasDemand ||
                    !demand.IsValid ||
                    !lease.Matches(demand.Lineage))
                {
                    throw new ArgumentException(
                        "Pose Source Demand does not match the Pending lease.",
                        nameof(demand));
                }
                m_Demand = demand;
                m_HasDemand = true;
            }

            internal CharacterPoseSourceDemand RequireDemand(
                CharacterPoseSourceFrameLease lease)
            {
                RequireLease(lease);
                if (!m_HasDemand)
                {
                    throw new InvalidOperationException(
                        "Pose Source Demand is not prepared.");
                }
                return m_Demand;
            }

            internal void BindResult(
                CharacterPoseSourceFrameLease lease,
                in CharacterPoseSourceFrameResult result)
            {
                RequireLease(lease);
                if (!m_HasDemand ||
                    m_HasResult ||
                    !result.IsReady ||
                    result.Lineage != m_Demand.Lineage)
                {
                    throw new ArgumentException(
                        "Pose Source Result does not match the Pending demand.",
                        nameof(result));
                }
                m_Result = result;
                m_HasResult = true;
            }

            internal void RequireOpen(
                CharacterPoseSourceFrameLease lease) =>
                RequireLease(lease);

            internal void RequireReady(
                CharacterPoseSourceFrameLease lease)
            {
                RequireLease(lease);
                if (!m_HasDemand ||
                    !m_HasResult ||
                    !m_Result.IsReady ||
                    m_Result.Lineage != m_Demand.Lineage)
                {
                    throw new InvalidOperationException(
                        "Pose Source Pending page is incomplete.");
                }
            }

            internal void Seal(
                CharacterPoseSourceFrameLease lease)
            {
                RequireReady(lease);
                Clear();
            }

            internal void Discard(
                CharacterPoseSourceFrameLease lease)
            {
                RequireLease(lease);
                Clear();
            }

            internal void Clear()
            {
                m_Lease = default;
                m_Demand = default;
                m_Result = default;
                m_HasDemand = false;
                m_HasResult = false;
            }

            void RequireLease(
                CharacterPoseSourceFrameLease lease)
            {
                if (!lease.IsValid ||
                    !m_Lease.IsValid ||
                    lease.Lineage != m_Lease.Lineage)
                {
                    throw new InvalidOperationException(
                        "Pose Source Pending lease is stale.");
                }
            }
        }

        internal readonly struct SourceBinding
        {
            internal SourceBinding(
                AnimationPhysicalSourceIdentity physicalIdentity,
                int sourceIndex)
            {
                if (!physicalIdentity.IsValid || sourceIndex < 0)
                {
                    throw new ArgumentException(
                        "Pose source binding is invalid.");
                }
                PhysicalIdentity = physicalIdentity;
                m_EncodedSourceIndex = checked(sourceIndex + 1);
            }

            readonly int m_EncodedSourceIndex;
            internal AnimationPhysicalSourceIdentity PhysicalIdentity
            {
                get;
            }
            internal int SourceIndex => m_EncodedSourceIndex - 1;
            internal bool IsValid =>
                PhysicalIdentity.IsValid &&
                m_EncodedSourceIndex > 0;
        }

        internal readonly struct ReleasePreparation
        {
            internal ReleasePreparation(
                int releaseIndex,
                ulong generation,
                AnimationPhysicalSourceIdentity physicalIdentity,
                AnimationPoseSourceId sourceId,
                PoseNodeId poseNodeId)
            {
                if (releaseIndex < 0 ||
                    generation == 0 ||
                    !physicalIdentity.IsValid ||
                    !sourceId.IsValid ||
                    !poseNodeId.IsValid)
                {
                    throw new ArgumentException(
                        "Pose source release preparation is invalid.");
                }
                m_EncodedReleaseIndex = checked(releaseIndex + 1);
                Generation = generation;
                PhysicalIdentity = physicalIdentity;
                SourceId = sourceId;
                PoseNodeId = poseNodeId;
            }

            readonly int m_EncodedReleaseIndex;
            internal int ReleaseIndex => m_EncodedReleaseIndex - 1;
            internal ulong Generation { get; }
            internal AnimationPhysicalSourceIdentity PhysicalIdentity { get; }
            internal AnimationPoseSourceId SourceId { get; }
            internal PoseNodeId PoseNodeId { get; }
            internal bool IsValid =>
                m_EncodedReleaseIndex > 0 &&
                Generation != 0 &&
                PhysicalIdentity.IsValid &&
                SourceId.IsValid &&
                PoseNodeId.IsValid;
        }

        struct ReleaseEntry
        {
            internal ulong Generation;
            internal AnimationPhysicalSourceIdentity PhysicalIdentity;
            internal AnimationPoseSourceId SourceId;
            internal PoseNodeId PoseNodeId;
            internal AnimationPhysicalSourceReleaseToken PhysicalRelease;
            internal AnimationPoseSourceReleaseToken BackendRelease;

            internal bool IsValid =>
                Generation != 0 &&
                PhysicalIdentity.IsValid &&
                SourceId.IsValid &&
                PoseNodeId.IsValid &&
                PhysicalRelease.IsValid &&
                BackendRelease.IsValid;
        }

        readonly AnimancerComponent m_Animancer;
        readonly AnimancerPoseSamplingBackend m_Backend;
        readonly PhysicalPoseSourceRegistry m_PhysicalSources;
        readonly SourceBinding[] m_DirectBindings;
        readonly SourceBinding[] m_ClipBindings;
        readonly SourceBinding[] m_BlendSpaceBindings;
        readonly ReleaseEntry[] m_ReleasePreparations;
        readonly HashSet<AnimationPhysicalSourceIdentity>
            m_ReleaseValidationIdentities;
        readonly Playable m_PreviousOutputSource;
        readonly float m_PreviousOutputWeight;
        SourceFramePage m_FramePage;
        AnimationMixerPlayable m_SourceFanIn;
        ulong m_BindingCompletionIdentity;
        int m_ReleasePreparationCount;
        ulong m_NextReleasePreparationGeneration;
        bool m_Disposed;

        internal CharacterPoseSourceModule(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig,
            int sourceCapacity,
            int clipCapacity,
            int directBindingCapacity,
            int clipBindingCapacity,
            int blendSpaceBindingCapacity)
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
            m_DirectBindings = new SourceBinding[
                RequireBindingCapacity(
                    directBindingCapacity,
                    nameof(directBindingCapacity))];
            m_ClipBindings = new SourceBinding[
                RequireBindingCapacity(
                    clipBindingCapacity,
                    nameof(clipBindingCapacity))];
            m_BlendSpaceBindings = new SourceBinding[
                RequireBindingCapacity(
                    blendSpaceBindingCapacity,
                    nameof(blendSpaceBindingCapacity))];
            m_ReleasePreparations = new ReleaseEntry[sourceCapacity];
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
            in CharacterPoseFrameLineage lineage)
        {
            if (m_ReleasePreparationCount != 0)
            {
                throw new InvalidOperationException(
                    "Pose source release preparations were not applied.");
            }
            CharacterPoseSourceFrameLease lease =
                m_FramePage.Begin(in lineage);
            try
            {
                m_Backend.BeginFrame(lease);
                try
                {
                    m_PhysicalSources.BeginFrame();
                }
                catch
                {
                    m_Backend.DiscardFrame(lease);
                    throw;
                }
                return lease;
            }
            catch
            {
                m_FramePage.Discard(lease);
                throw;
            }
        }

        internal void BeginBindingFrame(ulong completionIdentity)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(completionIdentity));
            ClearBindings();
            m_BindingCompletionIdentity = completionIdentity;
        }

        internal void BindDemand(
            CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourceDemand demand) =>
            m_FramePage.BindDemand(lease, in demand);

        internal CharacterPoseSourceDemand RequireDemand(
            CharacterPoseSourceFrameLease lease) =>
            m_FramePage.RequireDemand(lease);

        internal CharacterPoseSourceFrameResult PrepareFrameResult(
            CharacterPoseSourceFrameLease lease,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSources)
        {
            CharacterPoseSourceDemand demand =
                m_FramePage.RequireDemand(lease);
            var result = new CharacterPoseSourceFrameResult(
                in demand,
                actionSources,
                providerSources);
            m_FramePage.BindResult(lease, in result);
            return result;
        }

        internal void RequirePendingOpen(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireOpen(lease);
            m_Backend.RequireOpenFrame(lease);
        }

        internal void RequirePendingReady(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireReady(lease);
            m_Backend.RequireOpenFrame(lease);
        }

        internal bool ContainsCommitted(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId) =>
            m_Backend.ContainsCommitted(sourceId, poseNodeId);

        internal void PrepareAndConnect(
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
        }

        internal void PrepareDirectAndConnect(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            m_DirectBindings[bindingIndex] = PreparePlayerAndConnect(
                sourceId,
                sourceOwnerIndex,
                clips,
                clipCatalog,
                in capture,
                poseNodeId);
        }

        internal void PrepareClipAndConnect(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            m_ClipBindings[bindingIndex] = PreparePlayerAndConnect(
                sourceId,
                sourceOwnerIndex,
                clips,
                clipCatalog,
                in capture,
                poseNodeId);
        }

        internal void PrepareBlendSpaceAndConnect(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            m_BlendSpaceBindings[bindingIndex] =
                PreparePlayerAndConnect(
                    sourceId,
                    sourceOwnerIndex,
                    clips,
                    clipCatalog,
                    in capture,
                    poseNodeId);
        }

        SourceBinding PreparePlayerAndConnect(
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            if (m_BindingCompletionIdentity == 0 ||
                capture.CompletionIdentity !=
                    m_BindingCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Pose source binding frame is stale.");
            }
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
            return new SourceBinding(
                physical,
                capture.SourceIndex);
        }

        internal SourceBinding ReadDirectBinding(
            int bindingIndex,
            ulong completionIdentity) =>
            ReadBinding(
                m_DirectBindings,
                bindingIndex,
                completionIdentity);

        internal SourceBinding ReadClipBinding(
            int bindingIndex,
            ulong completionIdentity) =>
            ReadBinding(
                m_ClipBindings,
                bindingIndex,
                completionIdentity);

        internal SourceBinding ReadBlendSpaceBinding(
            int bindingIndex,
            ulong completionIdentity) =>
            ReadBinding(
                m_BlendSpaceBindings,
                bindingIndex,
                completionIdentity);

        internal void ValidatePhysicalFrame() =>
            m_PhysicalSources.ValidateFrame();

        internal void ValidateFrame(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireReady(lease);
            m_Backend.ValidateFrame(lease);
        }

        internal void EnterEvaluateBarrier(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireReady(lease);
            m_Backend.EnterEvaluateBarrier(lease);
        }

        internal void CommitFrame(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireReady(lease);
            m_Backend.CommitFrame(lease);
            m_FramePage.Seal(lease);
            m_PhysicalSources.CommitFrame();
        }

        internal void DiscardBackendFrame(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireOpen(lease);
            m_Backend.DiscardFrame(lease);
            m_FramePage.Discard(lease);
        }

        internal void DiscardPhysicalFrame()
        {
            m_PhysicalSources.DiscardFrame();
            ClearBindings();
            ClearReleasePreparations();
        }

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

        AnimationPhysicalSourceIdentity ValidateRelease(
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

        internal ReleasePreparation PrepareRelease(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            AnimationPhysicalSourceIdentity expected)
        {
            if (m_ReleasePreparationCount >=
                m_ReleasePreparations.Length)
            {
                throw new InvalidOperationException(
                    "Pose source release preparation capacity was exceeded.");
            }
            AnimationPhysicalSourceIdentity physical =
                ValidateRelease(
                    sourceId,
                    poseNodeId,
                    expected);
            AnimationPhysicalSourceReleaseToken physicalRelease =
                m_PhysicalSources.PrepareRelease(
                    physical,
                    sourceId);
            AnimationPoseSourceReleaseToken backendRelease =
                m_Backend.StageRelease(
                    sourceId,
                    poseNodeId);
            int releaseIndex = FindFreeReleasePreparation();
            ulong generation = NextReleasePreparationGeneration();
            m_ReleasePreparations[releaseIndex] = new ReleaseEntry
            {
                Generation = generation,
                PhysicalIdentity = physical,
                SourceId = sourceId,
                PoseNodeId = poseNodeId,
                PhysicalRelease = physicalRelease,
                BackendRelease = backendRelease
            };
            m_ReleasePreparationCount++;
            return new ReleasePreparation(
                releaseIndex,
                generation,
                physical,
                sourceId,
                poseNodeId);
        }

        internal void ApplyPreparedRelease(
            in ReleasePreparation release)
        {
            if (!release.IsValid)
            {
                throw new ArgumentException(
                    "Pose source release preparation is invalid.",
                    nameof(release));
            }
            int releaseIndex = release.ReleaseIndex;
            if ((uint)releaseIndex >=
                    (uint)m_ReleasePreparations.Length ||
                !m_ReleasePreparations[releaseIndex].IsValid ||
                m_ReleasePreparations[releaseIndex].Generation !=
                    release.Generation ||
                m_ReleasePreparations[releaseIndex].PhysicalIdentity !=
                    release.PhysicalIdentity ||
                !m_ReleasePreparations[releaseIndex].SourceId.Equals(
                    release.SourceId) ||
                m_ReleasePreparations[releaseIndex].PoseNodeId !=
                    release.PoseNodeId)
            {
                throw new InvalidOperationException(
                    "Pose source release preparation is stale.");
            }
            ReleaseEntry prepared =
                m_ReleasePreparations[releaseIndex];
            Disconnect(prepared.PhysicalIdentity);
            m_Backend.Release(in prepared.BackendRelease);
            m_PhysicalSources.ApplyPreparedRelease(
                in prepared.PhysicalRelease);
            m_ReleasePreparations[releaseIndex] = default;
            m_ReleasePreparationCount--;
        }

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
            if (m_ReleasePreparationCount != 0)
            {
                throw new InvalidOperationException(
                    "Pose source release preparations were not fully applied.");
            }
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
            m_FramePage.Clear();
            m_PhysicalSources.Reset();
            ClearBindings();
            for (int port = 1;
                 port < m_SourceFanIn.GetInputCount();
                 port++)
            {
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
            }
            m_ReleaseValidationIdentities.Clear();
            ClearReleasePreparations();
        }

        int FindFreeReleasePreparation()
        {
            for (int i = 0; i < m_ReleasePreparations.Length; i++)
            {
                if (!m_ReleasePreparations[i].IsValid)
                    return i;
            }
            throw new InvalidOperationException(
                "Pose source release preparation capacity was exceeded.");
        }

        ulong NextReleasePreparationGeneration()
        {
            m_NextReleasePreparationGeneration++;
            if (m_NextReleasePreparationGeneration == 0)
            {
                throw new InvalidOperationException(
                    "Pose source release preparation generation was exhausted.");
            }
            return m_NextReleasePreparationGeneration;
        }

        void ClearReleasePreparations()
        {
            Array.Clear(
                m_ReleasePreparations,
                0,
                m_ReleasePreparations.Length);
            m_ReleasePreparationCount = 0;
        }

        SourceBinding ReadBinding(
            SourceBinding[] bindings,
            int bindingIndex,
            ulong completionIdentity)
        {
            if ((uint)bindingIndex >= (uint)bindings.Length ||
                completionIdentity == 0 ||
                completionIdentity != m_BindingCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Pose source binding request is stale.");
            }
            return bindings[bindingIndex];
        }

        void ClearBindings()
        {
            Array.Clear(
                m_DirectBindings,
                0,
                m_DirectBindings.Length);
            Array.Clear(
                m_ClipBindings,
                0,
                m_ClipBindings.Length);
            Array.Clear(
                m_BlendSpaceBindings,
                0,
                m_BlendSpaceBindings.Length);
            m_BindingCompletionIdentity = 0;
        }

        static int RequireBindingCapacity(
            int capacity,
            string parameterName)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(parameterName);
            return capacity;
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
            m_FramePage.Clear();
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
