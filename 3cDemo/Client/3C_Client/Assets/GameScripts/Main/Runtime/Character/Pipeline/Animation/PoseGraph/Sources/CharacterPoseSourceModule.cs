using ThirdPersonPerformance.Instrumentation;
using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using Unity.Collections;
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
            int m_ConsumedPreparationCount;
            bool m_HasDemand;
            bool m_HasResult;

            internal bool HasOpenFrame => m_Lease.IsValid;

            internal CharacterPoseSourceFrameLease Begin(
                in CharacterPoseNativeFrameLineage lineage)
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
                m_ConsumedPreparationCount = 0;
                m_HasDemand = false;
                m_HasResult = false;
                return m_Lease;
            }

            internal void BindDemand(
                in CharacterPoseSourceFrameLease lease,
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

            internal static ref readonly CharacterPoseSourceDemand RequireDemand(
                in SourceFramePage page,
                in CharacterPoseSourceFrameLease lease)
            {
                page.RequireLease(lease);
                if (!page.m_HasDemand)
                {
                    throw new InvalidOperationException(
                        "Pose Source Demand is not prepared.");
                }
                return ref page.m_Demand;
            }

            internal ref readonly CharacterPoseSourcePreparation
                ConsumePreparation(
                    in CharacterPoseSourceFrameLease lease,
                    in CharacterPoseSourcePreparationView preparations,
                    int index)
            {
                RequireLease(lease);
                ref readonly CharacterPoseSourcePreparationView expected =
                    ref m_Demand.Preparations;
                if (!m_HasDemand ||
                    !expected.Matches(in preparations) ||
                    index != m_ConsumedPreparationCount)
                {
                    throw new InvalidOperationException(
                        "Pose Source preparation does not match the Pending demand.");
                }
                ref readonly CharacterPoseSourcePreparation preparation =
                    ref preparations.Get(index);
                m_ConsumedPreparationCount++;
                return ref preparation;
            }

            internal void BindResult(
                in CharacterPoseSourceFrameLease lease,
                in CharacterPoseSourceFrameResult result)
            {
                RequireLease(lease);
                if (!m_HasDemand ||
                    m_HasResult ||
                    m_ConsumedPreparationCount !=
                        m_Demand.Preparations.Count ||
                    !result.IsValid ||
                    !m_Demand.Lineage.Matches(in result.Demand.Lineage))
                {
                    throw new ArgumentException(
                        "Pose Source Result does not match the Pending demand.",
                        nameof(result));
                }
                m_Result = result;
                m_HasResult = true;
            }

            internal void RequireOpen(
                in CharacterPoseSourceFrameLease lease) =>
                RequireLease(lease);

            internal void RequireReady(
                in CharacterPoseSourceFrameLease lease)
            {
                RequireLease(lease);
                if (!m_HasDemand ||
                    !m_HasResult ||
                    !m_Result.IsReady ||
                    !m_Demand.Lineage.Matches(in m_Result.Demand.Lineage))
                {
                    throw new InvalidOperationException(
                        "Pose Source Pending page is incomplete.");
                }
            }

            internal void Seal(
                in CharacterPoseSourceFrameLease lease)
            {
                RequireReady(lease);
                Clear();
            }

            internal void Discard(
                in CharacterPoseSourceFrameLease lease)
            {
                RequireLease(lease);
                Clear();
            }

            internal void Clear()
            {
                m_Lease = default;
                m_Demand = default;
                m_Result = default;
                m_ConsumedPreparationCount = 0;
                m_HasDemand = false;
                m_HasResult = false;
            }

            void RequireLease(
                in CharacterPoseSourceFrameLease lease)
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

        struct ReleaseEntry
        {
            internal ulong Generation;
            internal AnimationPhysicalSourceIdentity PhysicalIdentity;
            internal CharacterPoseSourceRetirementPermission Permission;
            internal AnimationPhysicalSourceReleaseToken PhysicalRelease;
            internal AnimationPoseSourceReleaseToken BackendRelease;

            internal bool IsValid =>
                Generation != 0 &&
                PhysicalIdentity.IsValid &&
                Permission.IsValid &&
                PhysicalRelease.IsValid &&
                BackendRelease.IsValid;
        }

        sealed class SourceReleasePage
        {
            readonly ReleaseEntry[] m_Entries;
            int m_Count;
            ulong m_NextGeneration;

            internal SourceReleasePage(int capacity)
            {
                if (capacity < 0)
                    throw new ArgumentOutOfRangeException(nameof(capacity));
                m_Entries = new ReleaseEntry[capacity];
            }

            internal void RequireEmpty()
            {
                if (m_Count != 0)
                {
                    throw new InvalidOperationException(
                        "Pose source release preparations were not fully applied.");
                }
            }

            internal CharacterPoseSourceRetirementHandle Prepare(
                AnimationPhysicalSourceIdentity physicalIdentity,
                in CharacterPoseSourceRetirementPermission permission,
                in AnimationPhysicalSourceReleaseToken physicalRelease,
                in AnimationPoseSourceReleaseToken backendRelease)
            {
                if (m_Count >= m_Entries.Length)
                {
                    throw new InvalidOperationException(
                        "Pose source release preparation capacity was exceeded.");
                }
                int index = FindFree();
                ulong generation = NextGeneration();
                m_Entries[index] = new ReleaseEntry
                {
                    Generation = generation,
                    PhysicalIdentity = physicalIdentity,
                    Permission = permission,
                    PhysicalRelease = physicalRelease,
                    BackendRelease = backendRelease
                };
                m_Count++;
                return new CharacterPoseSourceRetirementHandle(
                    index,
                    generation,
                    in permission);
            }

            internal ReleaseEntry Require(
                in CharacterPoseSourceRetirementHandle retirement)
            {
                if (!retirement.IsValid)
                {
                    throw new ArgumentException(
                        "Pose source release preparation is invalid.",
                        nameof(retirement));
                }
                int index = retirement.Index;
                CharacterPoseSourceRetirementPermission permission =
                    retirement.Permission;
                if ((uint)index >= (uint)m_Entries.Length ||
                    !m_Entries[index].IsValid ||
                    m_Entries[index].Generation !=
                        retirement.Generation ||
                    !m_Entries[index].Permission.Matches(
                        in permission))
                {
                    throw new InvalidOperationException(
                        "Pose source release preparation is stale.");
                }
                return m_Entries[index];
            }

            internal void Complete(
                in CharacterPoseSourceRetirementHandle retirement)
            {
                Require(in retirement);
                m_Entries[retirement.Index] = default;
                m_Count--;
            }

            internal void Clear()
            {
                Array.Clear(m_Entries, 0, m_Entries.Length);
                m_Count = 0;
            }

            int FindFree()
            {
                for (int i = 0; i < m_Entries.Length; i++)
                {
                    if (!m_Entries[i].IsValid)
                        return i;
                }
                throw new InvalidOperationException(
                    "Pose source release preparation capacity was exceeded.");
            }

            ulong NextGeneration()
            {
                m_NextGeneration++;
                if (m_NextGeneration == 0)
                {
                    throw new InvalidOperationException(
                        "Pose source release preparation generation was exhausted.");
                }
                return m_NextGeneration;
            }
        }

        readonly AnimancerComponent m_Animancer;
        readonly CharacterPoseSourceCatalog m_Catalog;
        readonly CharacterPoseFootMotionResolver m_FootMotionResolver;
        readonly CharacterPoseSourceReadinessJournal m_Readiness;
        readonly CharacterPoseSourceBackendSet m_Backends;
        readonly AnimancerPoseSamplingBackend m_NativeClipBackend;
        readonly PhysicalPoseSourceRegistry m_PhysicalSources;
        readonly CharacterPoseSourceBindingPage m_BindingPage;
        readonly CharacterPoseSourceUsagePage m_UsagePage;
        readonly SourceReleasePage m_ReleasePage;
        readonly List<ICharacterPoseSourceRetirementOwner> m_RetirementOwners =
            new List<ICharacterPoseSourceRetirementOwner>();
        readonly HashSet<AnimationPhysicalSourceIdentity>
            m_ReleaseValidationIdentities;
        readonly Playable m_PreviousOutputSource;
        readonly float m_PreviousOutputWeight;
        SourceFramePage m_FramePage;
        AnimationMixerPlayable m_SourceFanIn;
        int m_PreparedSourceCount;
        bool m_Disposed;

        internal CharacterPoseSourceModule(
            AnimancerComponent animancer,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig,
            int sourceCapacity,
            int clipCapacity,
            int directBindingCapacity,
            int clipBindingCapacity,
            int blendSpaceBindingCapacity,
            CharacterAnimationResourceScope resourceScope,
            int parameterCapacity,
            CharacterPoseFootMotionResolver footMotionResolver)
        {
            m_FootMotionResolver = footMotionResolver;
            m_Animancer = animancer
                ? animancer
                : throw new ArgumentNullException(nameof(animancer));
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (resourceScope == null)
                throw new ArgumentNullException(nameof(resourceScope));
            m_Readiness = new CharacterPoseSourceReadinessJournal(
                resourceScope.Store,
                sourceCapacity,
                clipCapacity);
            m_Catalog = new CharacterPoseSourceCatalog(
                clipCapacity);
            var physicalSources = new PhysicalPoseSourceRegistry(
                sourceCapacity);
            AnimancerPoseSamplingBackend nativeClipBackend = null;
            CharacterAclPoseSamplingBackend aclBackend = null;
            CharacterPoseSourceBackendSet backendSet = null;
            AnimationMixerPlayable sourceFanIn = default;
            Playable previousOutputSource = default;
            float previousOutputWeight = 1f;
            try
            {
                nativeClipBackend = new AnimancerPoseSamplingBackend(
                    animancer,
                    rigBinding,
                    rig,
                    sourceCapacity,
                    clipCapacity);
                aclBackend = new CharacterAclPoseSamplingBackend(
                    animancer,
                    rigBinding,
                    rig,
                    sourceCapacity,
                    clipCapacity,
                    resourceScope,
                    parameterCapacity);
                backendSet = new CharacterPoseSourceBackendSet(
                    nativeClipBackend,
                    aclBackend);
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
                if (backendSet != null)
                    backendSet.Dispose();
                else
                {
                    aclBackend?.Dispose();
                    nativeClipBackend?.Dispose();
                }
                physicalSources.Dispose();
                throw;
            }

            m_NativeClipBackend = nativeClipBackend;
            m_Backends = backendSet;
            m_PhysicalSources = physicalSources;
            m_BindingPage = new CharacterPoseSourceBindingPage(
                directBindingCapacity,
                clipBindingCapacity,
                blendSpaceBindingCapacity);
            m_UsagePage =
                new CharacterPoseSourceUsagePage(sourceCapacity);
            m_ReleasePage = new SourceReleasePage(sourceCapacity);
            m_ReleaseValidationIdentities =
                new HashSet<AnimationPhysicalSourceIdentity>(
                    sourceCapacity);
            m_SourceFanIn = sourceFanIn;
            m_PreviousOutputSource = previousOutputSource;
            m_PreviousOutputWeight = previousOutputWeight;
        }

        internal int Capacity => m_PhysicalSources.Capacity;
        internal CharacterPoseSourceFrameLease CurrentLease => m_CurrentLease;

        CharacterPoseSourceFrameLease m_CurrentLease;

        internal CharacterPoseSourceFrameLease BeginFrame(
            in CharacterPoseNativeFrameLineage lineage)
        {
            m_ReleasePage.RequireEmpty();
            m_ReleaseValidationIdentities.Clear();
            CharacterPoseSourceFrameLease lease =
                m_FramePage.Begin(in lineage);
            try
            {
                m_Backends.BeginFrame(lease);
                try
                {
                    m_PhysicalSources.BeginFrame();
                }
                catch
                {
                    m_Backends.DiscardFrame(lease);
                    throw;
                }
                m_Readiness.Clear();
                m_PreparedSourceCount = 0;
                m_CurrentLease = lease;
                return lease;
            }
            catch
            {
                m_FramePage.Discard(lease);
                throw;
            }
        }

        void BeginBindingFrame(ulong completionIdentity)
        {
            m_BindingPage.Begin(completionIdentity);
        }

        internal void BindDemand(
            in CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourceDemand demand)
        {
            BeginBindingFrame(demand.Lineage.CompletionIdentity);
            m_FramePage.BindDemand(lease, in demand);
            m_Readiness.BeginDemand(demand.Lineage.CompletionIdentity);
        }

        internal ref readonly CharacterPoseSourceDemand RequireDemand(
            in CharacterPoseSourceFrameLease lease) =>
            ref SourceFramePage.RequireDemand(in m_FramePage, in lease);

        internal CharacterPoseSourceFrameResult PrepareFrameResult(
            in CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourcePreparedResources preparedResources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSources,
            in CharacterPoseSourceReadinessPageView readinessPage)
        {
            ref readonly CharacterPoseSourceDemand demand =
                ref SourceFramePage.RequireDemand(in m_FramePage, in lease);
            var result = new CharacterPoseSourceFrameResult(
                in demand,
                in preparedResources,
                actionSources,
                providerSources,
                in readinessPage,
                m_PreparedSourceCount > 0);
            m_FramePage.BindResult(lease, in result);
            return result;
        }

        internal CharacterPoseSourceReadinessPageView SealReadiness(
            in CharacterPoseSourcePreparationView preparations)
        {
            return m_Readiness.Seal(
                preparations.CompletionIdentity,
                in preparations);
        }

        internal bool HasPreparedSource => m_PreparedSourceCount > 0;

        internal bool TryDeferSource(
            in CharacterPoseSourceReadinessTarget target)
        {
            return TryDeferSource(in target, out _);
        }

        internal bool TryDeferSource(
            in CharacterPoseSourceReadinessTarget target,
            out CharacterPoseSourceResourceResolution resolution)
        {
            return m_Readiness.TryDefer(in target, out resolution);
        }

        internal CharacterPoseSourcePreparedResources
            RequirePreparedResources(
                in CharacterPoseSourceFrameLease lease)
        {
            ref readonly CharacterPoseSourceDemand demand =
                ref SourceFramePage.RequireDemand(in m_FramePage, in lease);
            ref readonly CharacterPoseNativeFrameLineage lineage = ref demand.Lineage;
            return new CharacterPoseSourcePreparedResources(
                in lineage,
                m_BindingPage);
        }

        internal CharacterPoseSourceBinding PrepareNativeClipPlayer(
            in CharacterPoseSourceFrameLease lease,
            in AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId,
            int bindingIndex)
        {
            RequirePendingOpen(lease);
            if (!sourceId.IsValid || sourceOwnerIndex < 0 ||
                !capture.SourceId.Equals(sourceId) ||
                capture.CompletionIdentity == 0 ||
                !poseNodeId.IsValid || bindingIndex < 0)
            {
                throw new ArgumentException(
                    "Native Clip Player source preparation is invalid.");
            }
            if (m_BindingPage.CompletionIdentity == 0)
                BeginBindingFrame(capture.CompletionIdentity);
            if (m_BindingPage.CompletionIdentity != capture.CompletionIdentity)
                throw new InvalidOperationException(
                    "Native Clip Player source binding frame is stale.");
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                    sourceId,
                    sourceOwnerIndex,
                    clips,
                    m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId)
                        ? default
                        : m_Catalog.Build(clips),
                    in capture,
                    poseNodeId);
            m_BindingPage.BindClip(bindingIndex, in binding);
            m_PreparedSourceCount++;
            return binding;
        }

        internal CharacterPoseSourceBinding PrepareNativeBlendSpacePlayer(
            in CharacterPoseSourceFrameLease lease,
            in AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId,
            int bindingIndex)
        {
            RequirePendingOpen(lease);
            if (!sourceId.IsValid || sourceOwnerIndex < 0 ||
                !capture.SourceId.Equals(sourceId) ||
                capture.CompletionIdentity == 0 ||
                !poseNodeId.IsValid || bindingIndex < 0)
            {
                throw new ArgumentException(
                    "Native Blend Space Player source preparation is invalid.");
            }
            if (m_BindingPage.CompletionIdentity == 0)
                BeginBindingFrame(capture.CompletionIdentity);
            if (m_BindingPage.CompletionIdentity != capture.CompletionIdentity)
                throw new InvalidOperationException(
                    "Native Blend Space Player source binding frame is stale.");
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                    sourceId,
                    sourceOwnerIndex,
                    clips,
                    m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId)
                        ? default
                        : m_Catalog.Build(clips),
                    in capture,
                    poseNodeId);
            m_BindingPage.BindBlendSpace(bindingIndex, in binding);
            m_PreparedSourceCount++;
            return binding;
        }

        internal CharacterPoseSourceBinding PrepareNativeSelectedPosePlayer(
            in CharacterPoseSourceFrameLease lease,
            PresentationPoseSourceSample sample,
            int sourceOwnerIndex,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId,
            int bindingIndex)
        {
            RequirePendingOpen(lease);
            if (sample == null || !sample.IsValid ||
                sample.Availability != PresentationPoseSourceAvailability.Ready ||
                sample.SourceKind != AnimationPoseSourceKind.MotionMatching ||
                sourceOwnerIndex < 0 || !poseNodeId.IsValid || bindingIndex < 0)
            {
                throw new ArgumentException(
                    "Native Selected Pose Player source preparation is invalid.");
            }
            AnimationPoseSourceId sourceId = new AnimationPoseSourceId(
                sample.SourceIndex,
                sample.SourceKind,
                new AnimationPoseSelectionGeneration(
                    sample.SourceGeneration.Value));
            if (!capture.SourceId.Equals(sourceId) ||
                capture.CompletionIdentity == 0)
            {
                throw new ArgumentException(
                    "Native Selected Pose Player source capture is invalid.",
                    nameof(capture));
            }
            if (m_BindingPage.CompletionIdentity == 0)
                BeginBindingFrame(capture.CompletionIdentity);
            if (m_BindingPage.CompletionIdentity != capture.CompletionIdentity)
                throw new InvalidOperationException(
                    "Native Selected Pose Player source binding frame is stale.");
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                    sourceId,
                    sourceOwnerIndex,
                    sample.Clips,
                    m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId)
                        ? default
                        : m_Catalog.Build(sample.Clips),
                    in capture,
                    poseNodeId);
            m_BindingPage.BindDirect(bindingIndex, in binding);
            m_PreparedSourceCount++;
            return binding;
        }

        internal void PrepareNativeActionSource(
            in CharacterPoseSourceFrameLease lease,
            in AnimationPoseSampleRequest request,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            RequirePendingOpen(lease);
            if (!request.IsValid ||
                request.SourceId.SourceKind != AnimationPoseSourceKind.Timeline ||
                !capture.SourceId.Equals(request.SourceId) ||
                capture.CompletionIdentity == 0 ||
                !poseNodeId.IsValid)
            {
                throw new ArgumentException(
                    "Native Action source preparation is invalid.");
            }
            if (m_BindingPage.CompletionIdentity == 0)
                BeginBindingFrame(capture.CompletionIdentity);
            if (m_BindingPage.CompletionIdentity != capture.CompletionIdentity)
                throw new InvalidOperationException(
                    "Native Action source binding frame is stale.");
            PrepareActionAndConnect(
                request,
                capture,
                poseNodeId);
            m_PreparedSourceCount++;
        }

        internal void PrepareNativeProviderSource(
            in CharacterPoseSourceFrameLease lease,
            PresentationPoseSourceSample sample,
            int sourceOwnerIndex,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            RequirePendingOpen(lease);
            if (sample == null || !sample.IsValid ||
                sample.Availability != PresentationPoseSourceAvailability.Ready ||
                sample.SourceKind != AnimationPoseSourceKind.MotionMatching ||
                sourceOwnerIndex < 0 || !poseNodeId.IsValid ||
                capture.CompletionIdentity == 0)
            {
                throw new ArgumentException(
                    "Native Provider source preparation is invalid.");
            }
            AnimationPoseSampleRequest request =
                CreateProviderRequest(sample, sourceOwnerIndex);
            if (!capture.SourceId.Equals(request.SourceId))
                throw new ArgumentException(
                    "Native Provider source capture is invalid.",
                    nameof(capture));
            if (m_BindingPage.CompletionIdentity == 0)
                BeginBindingFrame(capture.CompletionIdentity);
            if (m_BindingPage.CompletionIdentity != capture.CompletionIdentity)
                throw new InvalidOperationException(
                    "Native Provider source binding frame is stale.");
            PrepareProviderAndConnect(
                request,
                sample,
                capture,
                poseNodeId);
            m_PreparedSourceCount++;
        }

        internal void BeginUsage(ulong completionIdentity) =>
            m_UsagePage.Begin(completionIdentity);

        internal void RecordUsage(
            in CharacterPoseSourceUsage usage) =>
            m_UsagePage.Add(in usage);

        internal CharacterPoseSourceUsageView CaptureUsage(
            ulong completionIdentity) =>
            new CharacterPoseSourceUsageView(
                m_UsagePage,
                completionIdentity);

        internal void ClearUsage() => m_UsagePage.Clear();

        internal void RequirePendingOpen(
            in CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireOpen(lease);
            m_Backends.RequireOpenFrame(lease);
        }

        internal void RequirePendingReady(
            in CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireReady(lease);
            m_Backends.RequireOpenFrame(lease);
        }

        internal static AnimationPoseSampleRequest CreateProviderRequest(
            in PresentationPoseSourceSample sample,
            int sourceOwnerIndex)
        {
            if (sample == null || !sample.IsValid ||
                sample.Availability !=
                    PresentationPoseSourceAvailability.Ready ||
                sourceOwnerIndex < 0)
            {
                throw new ArgumentException(
                    "Presentation Pose source sample cannot be lowered.");
            }
            var sourceId = new AnimationPoseSourceId(
                sample.SourceIndex,
                sample.SourceKind,
                new AnimationPoseSelectionGeneration(
                    sample.SourceGeneration.Value));
            PresentationPoseSampleTime time = sample.EffectiveSample;
            return new AnimationPoseSampleRequest(
                sourceId,
                sample.SourcePoseContinuityIdentity,
                sample.FrameSequence,
                sourceOwnerIndex,
                time.SampleTime,
                time.ContinuousTime,
                time.Cycle,
                time.Loop,
                time.TimeScale,
                sample.Clips,
                sample.ParameterPageId,
                sample.PoseParameters,
                sample.PoseParameterAvailability);
        }

        internal void Prepare(
            in CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourcePreparationView preparations,
            int preparationIndex)
        {
            ref readonly CharacterPoseSourcePreparation preparation =
                ref m_FramePage.ConsumePreparation(
                    lease,
                    in preparations,
                    preparationIndex);
            if (!preparation.IsValid)
            {
                throw new ArgumentException(
                    "Character Pose source preparation is invalid.",
                    nameof(preparation));
            }
            CharacterPoseSourceResourceResolution resolution =
                m_Readiness.ResolveCurrent(
                    preparation.Capture.CompletionIdentity,
                    in preparation);
            CharacterPoseSourceReadinessView readiness =
                resolution.ToReadiness(preparation.Capture.CompletionIdentity);
            if (!readiness.IsReady)
            {
                throw new InvalidOperationException(
                    $"Character Pose source preparation resource is '{readiness.Availability}' " +
                    $"with failure '{readiness.ResourceFailureCode}': {readiness.Message}");
            }
            switch (preparation.Kind)
            {
                case CharacterPoseSourcePreparationKind.Action:
                    PrepareActionAndConnect(
                        preparation.Request,
                        preparation.Capture,
                        preparation.PoseNodeId);
                    break;
                case CharacterPoseSourcePreparationKind.Provider:
                    PrepareProviderAndConnect(
                        preparation.Request,
                        preparation.ProviderSample,
                        preparation.Capture,
                        preparation.PoseNodeId);
                    break;
                case CharacterPoseSourcePreparationKind.DirectPlayer:
                    PrepareDirectAndConnect(
                        preparation.BindingIndex,
                        preparation.SourceId,
                        preparation.SourceOwnerIndex,
                        preparation.Clips,
                        preparation.ProviderSample,
                        preparation.Capture,
                        preparation.PoseNodeId);
                    break;
                case CharacterPoseSourcePreparationKind.ClipPlayer:
                    PrepareClipAndConnect(
                        preparation.BindingIndex,
                        preparation.SourceId,
                        preparation.SourceOwnerIndex,
                        preparation.Clips,
                        preparation.Capture,
                        preparation.PoseNodeId);
                    break;
                case CharacterPoseSourcePreparationKind.BlendSpacePlayer:
                    PrepareBlendSpaceAndConnect(
                        preparation.BindingIndex,
                        preparation.SourceId,
                        preparation.SourceOwnerIndex,
                        preparation.Clips,
                        preparation.Capture,
                        preparation.PoseNodeId);
                    break;
                default:
                    throw new InvalidOperationException(
                        "Character Pose source preparation kind is invalid.");
            }
            m_PreparedSourceCount++;
        }

        void PrepareActionAndConnect(
            in AnimationPoseSampleRequest request,
            in AnimationPoseSourceCaptureBinding capture,
            in PoseNodeId poseNodeId)
        {
            CaptureFootMotion(request.SourceId, request.Clips, in capture);
            IAnimationPoseSamplingBackend backend =
                ResolveBackend(request.Clips, request.SourceId, poseNodeId);
            bool committed = m_PhysicalSources.ContainsCommitted(
                request.SourceId,
                poseNodeId);
            AnimationPhysicalSourceIdentity physical =
                RegisterSource(
                    request.SourceId,
                    poseNodeId,
                    request.SourceOwnerIndex,
                    backend,
                    request.Clips,
                    default);
            AnimationPoseSourcePrepareResult prepared =
                backend.PrepareOrUpdate(
                    in request,
                    physical,
                    committed
                        ? default
                        : m_Catalog.Build(request.Clips),
                    in capture,
                    poseNodeId);
            Connect(physical, prepared);
        }

        void PrepareProviderAndConnect(
            in AnimationPoseSampleRequest request,
            PresentationPoseSourceSample sample,
            in AnimationPoseSourceCaptureBinding capture,
            in PoseNodeId poseNodeId)
        {
            CaptureFootMotion(request.SourceId, request.Clips, in capture);
            bool committed = m_PhysicalSources.ContainsCommitted(
                request.SourceId,
                poseNodeId);
            AnimationPhysicalSourceIdentity physical =
                RegisterSource(
                    request.SourceId,
                    poseNodeId,
                    request.SourceOwnerIndex,
                    m_NativeClipBackend,
                    request.Clips,
                    default);
            AnimationPoseSourcePrepareResult prepared =
                m_NativeClipBackend.PrepareOrUpdate(
                    in request,
                    physical,
                    committed
                        ? default
                        : m_Catalog.Build(request.Clips),
                    in capture,
                    poseNodeId);
            Connect(physical, prepared);
        }

        void PrepareDirectAndConnect(
            int bindingIndex,
            in AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            PresentationPoseSourceSample sample,
            in AnimationPoseSourceCaptureBinding capture,
            in PoseNodeId poseNodeId)
        {
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                sourceId,
                sourceOwnerIndex,
                clips,
                m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId)
                    ? default
                    : m_Catalog.Build(clips),
                in capture,
                poseNodeId);
            m_BindingPage.BindDirect(bindingIndex, in binding);
        }

        void PrepareClipAndConnect(
            int bindingIndex,
            in AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture,
            in PoseNodeId poseNodeId)
        {
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                sourceId,
                sourceOwnerIndex,
                clips,
                m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId)
                    ? default
                    : m_Catalog.Build(clips),
                in capture,
                poseNodeId);
            m_BindingPage.BindClip(bindingIndex, in binding);
        }

        void PrepareBlendSpaceAndConnect(
            int bindingIndex,
            in AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture,
            in PoseNodeId poseNodeId)
        {
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                    sourceId,
                    sourceOwnerIndex,
                    clips,
                m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId)
                        ? default
                        : m_Catalog.Build(clips),
                    in capture,
                    poseNodeId);
            m_BindingPage.BindBlendSpace(
                bindingIndex,
                in binding);
        }

        CharacterPoseSourceBinding PreparePlayerAndConnect(
            in AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            CaptureFootMotion(in sourceId, in clips, in capture);
            if (m_BindingPage.CompletionIdentity == 0 ||
                capture.CompletionIdentity !=
                    m_BindingPage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Pose source binding frame is stale.");
            }
            IAnimationPoseSamplingBackend backend =
                ResolveBackend(clips, sourceId, poseNodeId, clipCatalog);
            bool committed = m_PhysicalSources.ContainsCommitted(
                sourceId,
                poseNodeId);
            AnimationPhysicalSourceIdentity physical =
                RegisterSource(
                    sourceId,
                    poseNodeId,
                    sourceOwnerIndex,
                    backend,
                    clips,
                    clipCatalog);
            AnimationPoseSourcePrepareResult prepared =
                backend.PrepareOrUpdate(
                    in sourceId,
                    physical,
                    in clips,
                    committed ? default : clipCatalog,
                    in capture,
                    in poseNodeId);
            if (!prepared.ScalarReadView.IsValid ||
                prepared.ScalarReadView.PhysicalIdentity != physical ||
                prepared.ScalarReadView.SourceId != sourceId)
            {
                throw new InvalidOperationException(
                    "Pose source scalar read view does not match its physical source.");
            }
            CharacterPoseSourceScalarReadView scalarReadView =
                prepared.ScalarReadView;
            Connect(physical, prepared);
            return new CharacterPoseSourceBinding(
                physical,
                capture.SourceIndex,
                in scalarReadView);
        }

        void CaptureFootMotion(
            in AnimationPoseSourceId sourceId,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture)
        {
            int dominant = 0;
            for (int i = 1; i < clips.Count; i++)
            {
                if (clips.ElementAt(i).Weight > clips.ElementAt(dominant).Weight)
                    dominant = i;
            }
            ref readonly ClipSamplePlan clip = ref clips.ElementAt(dominant);
            CharacterPoseFootMotionSource source = m_FootMotionResolver(in sourceId, in clip);
            int cycle = checked((int)Math.Floor(clip.ContinuousClipTime / clip.DurationSeconds));
            NativeSlice<AnimationFootMotionSourceSample> output = capture.FootMotion;
            output[0] = new AnimationFootMotionSourceSample(
                source.SourceNameIndex, source.SourceSampleIdentity,
                clip.ClipBindingIndex, cycle, clip.NormalizedTime,
                source.Observation.Left.Sample(clip.NormalizedTime, cycle, clip.DurationSeconds, clip.IsLooping),
                source.Observation.Right.Sample(clip.NormalizedTime, cycle, clip.DurationSeconds, clip.IsLooping));
        }

        [PerformanceProbe("presentation.animation.source-barrier")]
        internal void EnterEvaluateBarrier(
            in CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireOpen(lease);
            BeginReleaseDiagnostics(false);
            for (int i = 0; i < m_RetirementOwners.Count; i++)
                m_RetirementOwners[i].PrepareRetirements();
            m_PhysicalSources.ValidateFrame();
            m_Backends.ValidateFrame(lease);
            m_Backends.EnterEvaluateBarrier(lease);
        }

        internal void CommitFrame(
            in CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireOpen(lease);
            m_Backends.CommitFrame(lease);
            m_FramePage.Discard(lease);
            m_PhysicalSources.CommitFrame();
            for (int i = 0; i < m_RetirementOwners.Count; i++)
                m_RetirementOwners[i].CommitRetirements();
            CompleteDeferredReleases();
            m_BindingPage.Clear();
            m_PreparedSourceCount = 0;
            m_CurrentLease = default;
        }

        internal void DiscardFrame(
            in CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireOpen(lease);
            for (int i = 0; i < m_RetirementOwners.Count; i++)
                m_RetirementOwners[i].DiscardRetirements();
            Exception failure = null;
            int pendingRegistrationCount = 0;
            try
            {
                pendingRegistrationCount =
                    m_PhysicalSources.PendingRegistrationCount;
            }
            catch (Exception exception)
            {
                RecordDiscardFailure(exception, ref failure);
            }
            for (int i = pendingRegistrationCount - 1; i >= 0; i--)
            {
                AnimationPhysicalSourceIdentity physical = default;
                try
                {
                    physical = m_PhysicalSources.GetPendingRegistration(i);
                }
                catch (Exception exception)
                {
                    RecordDiscardFailure(exception, ref failure);
                }
                if (physical.IsValid)
                {
                    try
                    {
                        Disconnect(physical);
                    }
                    catch (Exception exception)
                    {
                        RecordDiscardFailure(exception, ref failure);
                    }
                }
            }
            try
            {
                m_Backends.DiscardFrame(lease);
            }
            catch (Exception exception)
            {
                RecordDiscardFailure(exception, ref failure);
            }
            try
            {
                m_FramePage.Discard(lease);
            }
            catch (Exception exception)
            {
                RecordDiscardFailure(exception, ref failure);
            }
            try
            {
                m_PhysicalSources.DiscardFrame();
            }
            catch (Exception exception)
            {
                RecordDiscardFailure(exception, ref failure);
            }
            m_BindingPage.Clear();
            m_ReleasePage.Clear();
            m_ReleaseValidationIdentities.Clear();
            m_Readiness.Clear();
            m_PreparedSourceCount = 0;
            m_CurrentLease = default;
            if (failure != null)
            {
                throw new AggregateException(
                    "Pose Source Pending discard failed.",
                    failure);
            }
        }

        internal AnimationPhysicalSourceIdentity RequireIdentity(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId) =>
            m_PhysicalSources.RequireIdentity(sourceId, poseNodeId);

        internal AnimationPoseSourceId RequireSourceId(
            in AnimationPhysicalSourceIdentity physical) =>
            m_PhysicalSources.RequireSourceId(physical);

        internal PoseNodeId RequirePoseNodeId(
            in AnimationPhysicalSourceIdentity physical) =>
            m_PhysicalSources.RequirePoseNodeId(physical);

        internal int RequireSourceOwnerIndex(
            in AnimationPhysicalSourceIdentity physical) =>
            m_PhysicalSources.RequireSourceOwnerIndex(physical);

        internal void RegisterRetirementOwner(ICharacterPoseSourceRetirementOwner owner) =>
            m_RetirementOwners.Add(owner);

        internal void UnregisterRetirementOwner(ICharacterPoseSourceRetirementOwner owner) =>
            m_RetirementOwners.Remove(owner);

        AnimationPhysicalSourceIdentity ValidateRetirement(
            in CharacterPoseSourceRetirementPermission permission)
        {
            if (!permission.IsValid)
            {
                throw new ArgumentException(
                    "Character Pose source retirement permission is invalid.",
                    nameof(permission));
            }
            AnimationPhysicalSourceIdentity current =
                m_PhysicalSources.RequireIdentity(
                    permission.SourceId,
                    permission.PoseNodeId);
            if (permission.ExpectedPhysicalIdentity.IsValid &&
                current != permission.ExpectedPhysicalIdentity)
            {
                throw new InvalidOperationException(
                    "Pose source release physical identity is stale.");
            }
            int port = checked(current.Index.Value + 1);
            if (port <= 0 ||
                port >= m_SourceFanIn.GetInputCount() ||
                !IsCommittedInAnyBackend(
                    permission.SourceId,
                    permission.PoseNodeId) ||
                !m_ReleaseValidationIdentities.Add(current))
            {
                throw new InvalidOperationException(
                    "Pose source release backend identity is not exact.");
            }
            return current;
        }

        internal CharacterPoseSourceRetirementHandle PrepareRetirement(
            in CharacterPoseSourceRetirementPermission permission)
        {
            AnimationPhysicalSourceIdentity physical =
                ValidateRetirement(in permission);
            AnimationPhysicalSourceReleaseToken physicalRelease =
                m_PhysicalSources.PrepareRelease(
                    physical,
                    permission.SourceId);
            AnimationPoseSourceReleaseToken backendRelease =
                RequireCommittedBackend(
                    permission.SourceId,
                    permission.PoseNodeId).StageRelease(
                    permission.SourceId,
                    permission.PoseNodeId,
                    physical);
            return m_ReleasePage.Prepare(
                physical,
                in permission,
                in physicalRelease,
                in backendRelease);
        }

        internal void ApplyRetirement(
            in CharacterPoseSourceRetirementHandle retirement)
        {
            ReleaseEntry prepared =
                m_ReleasePage.Require(in retirement);
            Disconnect(prepared.PhysicalIdentity);
            RequireBackend(prepared.BackendRelease.Backend).Release(
                in prepared.BackendRelease);
            m_PhysicalSources.ApplyPreparedRelease(
                in prepared.PhysicalRelease);
            m_ReleasePage.Complete(in retirement);
        }

        void Disconnect(
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
            in PoseNodeId poseNodeId,
            in AnimationPoseSourceId sourceId,
            ulong completionIdentity) =>
            m_PhysicalSources.RecordRelease(
                poseNodeId,
                sourceId,
                completionIdentity);

        internal void CompleteDeferredReleases()
        {
            m_ReleasePage.RequireEmpty();
            m_Backends.ExecuteDeferredReleases();
            m_PhysicalSources.CompleteReleaseDiagnostics();
        }

        internal void CancelReleaseDiagnostics()
        {
            m_ReleaseValidationIdentities.Clear();
            m_PhysicalSources.CancelReleaseDiagnostics();
        }

        internal ClipSamplePlan RequireDominantClipSample(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId,
            ulong completionIdentity) =>
            RequireCommittedBackend(sourceId, poseNodeId).RequireDominantClipSample(
                sourceId,
                poseNodeId,
                completionIdentity);

        internal void Clear()
        {
            m_Backends.Clear();
            m_FramePage.Clear();
            m_PhysicalSources.Reset();
            m_BindingPage.Clear();
            m_UsagePage.Clear();
            m_Readiness.Clear();
            m_PreparedSourceCount = 0;
            for (int port = 1;
                 port < m_SourceFanIn.GetInputCount();
                 port++)
            {
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
            }
            m_ReleaseValidationIdentities.Clear();
            m_ReleasePage.Clear();
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

        AnimationPhysicalSourceIdentity RegisterSource(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId,
            int sourceOwnerIndex,
            IAnimationPoseSamplingBackend backend,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> catalog)
        {
            CharacterAnimationSamplingBackendKind backendKind =
                backend == m_NativeClipBackend
                    ? CharacterAnimationSamplingBackendKind.NativeClip
                    : CharacterAnimationSamplingBackendKind.Acl;
            int resourceCatalogIndex = backendKind == CharacterAnimationSamplingBackendKind.Acl
                ? RequireResourceCatalogIndex(clips, catalog)
                : -1;
            return m_PhysicalSources.Register(
                sourceId,
                poseNodeId,
                sourceOwnerIndex,
                backendKind,
                resourceCatalogIndex);
        }

        int RequireResourceCatalogIndex(
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> catalog)
        {
            if (clips.Count > 0)
            {
                for (int i = 0; i < clips.Count; i++)
                {
                    ref readonly ClipSamplePlan sample = ref clips.ElementAt(i);
                    if (sample.IsAcl)
                        return sample.ResourceCatalogIndex;
                }
            }
            for (int i = 0; i < catalog.Count; i++)
            {
                ref readonly AnimationPoseSourceClipBinding binding =
                    ref catalog.ElementAt(i);
                if (binding.IsAcl)
                    return binding.ResourceCatalogIndex;
            }
            throw new InvalidOperationException(
                "ACL pose source has no registered resource catalog entry.");
        }

        IAnimationPoseSamplingBackend ResolveBackend(
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId,
            in AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> catalog = default)
        {
            if (m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId))
            {
                AnimationPhysicalSourceIdentity physical =
                    m_PhysicalSources.RequireIdentity(sourceId, poseNodeId);
                return RequireBackend(
                    m_PhysicalSources.RequireBackendKind(physical));
            }
            if (clips.Count > 0)
            {
                bool acl = clips[0].IsAcl;
                for (int i = 1; i < clips.Count; i++)
                {
                    if (clips[i].IsAcl != acl)
                        throw new InvalidOperationException("Animation pose source mixes Native Clip and ACL backends.");
                }
                if (acl)
                    return RequireAclBackend();
                return m_NativeClipBackend;
            }
            if (catalog.Count > 0 && catalog[0].IsAcl)
                return RequireAclBackend();
            if (catalog.Count > 0 && !catalog[0].IsAcl)
                return m_NativeClipBackend;
            throw new InvalidOperationException(
                "Animation pose source has no backend declaration.");
        }

        IAnimationPoseSamplingBackend RequireCommittedBackend(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId)
        {
            AnimationPhysicalSourceIdentity physical =
                m_PhysicalSources.RequireIdentity(sourceId, poseNodeId);
            return RequireBackend(
                m_PhysicalSources.RequireBackendKind(physical));
        }

        bool IsCommittedInAnyBackend(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId poseNodeId) =>
            m_PhysicalSources.ContainsCommitted(sourceId, poseNodeId);

        IAnimationPoseSamplingBackend RequireBackend(
            CharacterAnimationSamplingBackendKind backend)
            => m_Backends.Require(backend);

        IAnimationPoseSamplingBackend RequireAclBackend() =>
            m_Backends.Require(CharacterAnimationSamplingBackendKind.Acl);

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
            DisposeStep(m_Backends.Dispose, ref failure);
            m_FramePage.Clear();
            m_BindingPage.Clear();
            m_UsagePage.Clear();
            m_ReleasePage.Clear();
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

        static void RecordDiscardFailure(
            Exception exception,
            ref Exception failure)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }
    }
}
