using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
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
            int m_ConsumedPreparationCount;
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
                m_ConsumedPreparationCount = 0;
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

            internal CharacterPoseSourcePreparation
                ConsumePreparation(
                    CharacterPoseSourceFrameLease lease,
                    in CharacterPoseSourcePreparationView preparations,
                    int index)
            {
                RequireLease(lease);
                CharacterPoseSourcePreparationView expected =
                    m_Demand.Preparations;
                if (!m_HasDemand ||
                    !expected.Matches(in preparations) ||
                    index != m_ConsumedPreparationCount)
                {
                    throw new InvalidOperationException(
                        "Pose Source preparation does not match the Pending demand.");
                }
                CharacterPoseSourcePreparation preparation =
                    preparations.Get(index);
                m_ConsumedPreparationCount++;
                return preparation;
            }

            internal void BindResult(
                CharacterPoseSourceFrameLease lease,
                in CharacterPoseSourceFrameResult result)
            {
                RequireLease(lease);
                if (!m_HasDemand ||
                    m_HasResult ||
                    m_ConsumedPreparationCount !=
                        m_Demand.Preparations.Count ||
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
                m_ConsumedPreparationCount = 0;
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

        sealed class SourceReleaseCompletionPage
        {
            readonly List<ActionBackendReleaseCompletion>
                m_ActionBackend;
            readonly List<AnimationSlotSourceReleaseCompletion>
                m_ActionSlot;
            readonly bool[] m_AcknowledgementMatches;
            int m_ValidatedAcknowledgementCount;
            bool m_AcknowledgementsValidated;

            internal SourceReleaseCompletionPage(int sourceCapacity)
            {
                if (sourceCapacity < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(sourceCapacity));
                }
                int backendCapacity = checked(sourceCapacity * 2);
                m_ActionBackend =
                    new List<ActionBackendReleaseCompletion>(
                        backendCapacity);
                m_ActionSlot =
                    new List<AnimationSlotSourceReleaseCompletion>(
                        sourceCapacity);
                m_AcknowledgementMatches = new bool[backendCapacity];
            }

            internal bool AcknowledgementsValidated =>
                m_AcknowledgementsValidated;

            internal void RequireActionBackendCapacity(
                int additionalCount)
            {
                if (additionalCount < 0 ||
                    checked(m_ActionBackend.Count + additionalCount) >
                    m_ActionBackend.Capacity)
                {
                    throw new InvalidOperationException(
                        "Action backend release completion capacity was exceeded.");
                }
            }

            internal void Record(
                in ActionBackendReleaseCompletion completion)
            {
                if (m_ActionBackend.Count >= m_ActionBackend.Capacity)
                {
                    throw new InvalidOperationException(
                        "Action backend release completion journal capacity was exceeded.");
                }
                m_ActionBackend.Add(completion);
            }

            internal void Record(
                in AnimationSlotSourceReleaseCompletion completion)
            {
                if (m_ActionSlot.Count >= m_ActionSlot.Capacity)
                {
                    throw new InvalidOperationException(
                        "Animation Slot source release completion journal capacity was exceeded.");
                }
                m_ActionSlot.Add(completion);
            }

            internal void CopyActionBackend(
                List<ActionBackendReleaseCompletion> destination)
            {
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                destination.Clear();
                destination.AddRange(m_ActionBackend);
            }

            internal void CopyActionSlot(
                List<AnimationSlotSourceReleaseCompletion> destination)
            {
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                destination.Clear();
                destination.AddRange(m_ActionSlot);
            }

            internal void ValidateAcknowledgements(
                IReadOnlyList<ActionBackendReleaseCompletion>
                    completions)
            {
                if (completions == null)
                    throw new ArgumentNullException(nameof(completions));
                Array.Clear(
                    m_AcknowledgementMatches,
                    0,
                    m_ActionBackend.Count);
                for (int i = 0; i < completions.Count; i++)
                {
                    ActionBackendReleaseCompletion expected =
                        completions[i];
                    int matchIndex = -1;
                    for (int candidateIndex = 0;
                         candidateIndex < m_ActionBackend.Count;
                         candidateIndex++)
                    {
                        ActionBackendReleaseCompletion candidate =
                            m_ActionBackend[candidateIndex];
                        if (!Matches(in candidate, in expected))
                            continue;
                        if (matchIndex >= 0 ||
                            m_AcknowledgementMatches[candidateIndex])
                        {
                            throw new InvalidOperationException(
                                "Action backend release completion is duplicated.");
                        }
                        matchIndex = candidateIndex;
                    }
                    if (matchIndex < 0)
                    {
                        throw new InvalidOperationException(
                            "Action backend release completion acknowledgement is not exact.");
                    }
                    m_AcknowledgementMatches[matchIndex] = true;
                }
                m_ValidatedAcknowledgementCount =
                    m_ActionBackend.Count;
                m_AcknowledgementsValidated = true;
            }

            internal void ApplyAcknowledgements()
            {
                if (!m_AcknowledgementsValidated ||
                    m_ActionBackend.Count !=
                    m_ValidatedAcknowledgementCount)
                {
                    throw new InvalidOperationException(
                        "Action backend release acknowledgements were not validated for the committed frame.");
                }
                int writeIndex = 0;
                for (int readIndex = 0;
                     readIndex < m_ValidatedAcknowledgementCount;
                     readIndex++)
                {
                    if (m_AcknowledgementMatches[readIndex])
                        continue;
                    m_ActionBackend[writeIndex++] =
                        m_ActionBackend[readIndex];
                }
                if (writeIndex < m_ActionBackend.Count)
                {
                    m_ActionBackend.RemoveRange(
                        writeIndex,
                        m_ActionBackend.Count - writeIndex);
                }
                ClearValidatedAcknowledgements();
            }

            internal void ClearActionSlot() => m_ActionSlot.Clear();

            internal void ClearActionBackend() =>
                m_ActionBackend.Clear();

            internal void ClearValidatedAcknowledgements()
            {
                Array.Clear(
                    m_AcknowledgementMatches,
                    0,
                    m_ValidatedAcknowledgementCount);
                m_ValidatedAcknowledgementCount = 0;
                m_AcknowledgementsValidated = false;
            }

            internal void Clear()
            {
                m_ActionBackend.Clear();
                m_ActionSlot.Clear();
                ClearValidatedAcknowledgements();
            }

            static bool Matches(
                in ActionBackendReleaseCompletion left,
                in ActionBackendReleaseCompletion right) =>
                left.RequestIdentity == right.RequestIdentity &&
                left.PlaybackId.Equals(right.PlaybackId) &&
                left.Source.Equals(right.Source) &&
                left.CompletionIdentity == right.CompletionIdentity;
        }

        readonly AnimancerComponent m_Animancer;
        readonly CharacterPoseSourceCatalog m_Catalog;
        readonly CharacterPoseSourceTuningState m_Tuning;
        readonly AnimancerPoseSamplingBackend m_Backend;
        readonly PhysicalPoseSourceRegistry m_PhysicalSources;
        readonly CharacterPoseSourceBindingPage m_BindingPage;
        readonly CharacterPoseSourceUsagePage m_UsagePage;
        readonly ActionPresentationSamplingRuntime m_ActionSampling;
        readonly SourceReleasePage m_ReleasePage;
        readonly SourceReleaseCompletionPage m_ReleaseCompletions;
        readonly HashSet<AnimationPhysicalSourceIdentity>
            m_ReleaseValidationIdentities;
        readonly Playable m_PreviousOutputSource;
        readonly float m_PreviousOutputWeight;
        SourceFramePage m_FramePage;
        ActionPresentationSamplingFrameTransaction m_ActionSamplingFrame;
        AnimationMixerPlayable m_SourceFanIn;
        bool m_Disposed;

        internal CharacterPoseSourceModule(
            AnimancerComponent animancer,
            CharacterPresentationProjection projection,
            ActionAnimationBindingIndex actionBindings,
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
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            m_Catalog = new CharacterPoseSourceCatalog(
                projection,
                clipCapacity);
            m_ActionSampling = new ActionPresentationSamplingRuntime(
                actionBindings ??
                throw new ArgumentNullException(nameof(actionBindings)));
            m_Tuning = new CharacterPoseSourceTuningState(
                projection,
                1);
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
            m_BindingPage = new CharacterPoseSourceBindingPage(
                directBindingCapacity,
                clipBindingCapacity,
                blendSpaceBindingCapacity);
            m_UsagePage =
                new CharacterPoseSourceUsagePage(sourceCapacity);
            m_ReleasePage = new SourceReleasePage(sourceCapacity);
            m_ReleaseCompletions =
                new SourceReleaseCompletionPage(sourceCapacity);
            m_ReleaseValidationIdentities =
                new HashSet<AnimationPhysicalSourceIdentity>(
                    sourceCapacity);
            m_SourceFanIn = sourceFanIn;
            m_PreviousOutputSource = previousOutputSource;
            m_PreviousOutputWeight = previousOutputWeight;
        }

        internal int Capacity => m_PhysicalSources.Capacity;
        internal int ActionSamplingJournalCapacity =>
            m_ActionSampling.JournalCapacity;

        internal void BeginActionSamplingFrame(
            ulong frameIdentity,
            ulong presentationFrame,
            bool captureDiagnostics)
        {
            if (m_ActionSamplingFrame?.IsValid == true)
            {
                throw new InvalidOperationException(
                    "Pose Source Action sampling frame is already open.");
            }
            m_ActionSamplingFrame = m_ActionSampling.BeginFrame(
                frameIdentity,
                presentationFrame,
                captureDiagnostics);
        }

        internal void ProjectActionPresentationSamples(
            CharacterActionPlaybackRuntime actionRuntime,
            CharacterActionPlaybackFrameTransaction actionTransaction,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            RequireActionSamplingFrame(actionTransaction.Identity);
            m_ActionSampling.ProjectPresentationSamples(
                m_ActionSamplingFrame,
                actionRuntime,
                actionTransaction,
                lifecycle,
                presentationSampleTick,
                presentationDeltaSeconds);
        }

        internal void ResolveActionPresentationFrames(
            ulong frameIdentity,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireActionSamplingFrame(frameIdentity);
            m_ActionSampling.ResolvePresentationFrames(
                m_ActionSamplingFrame,
                workspace,
                workspaceLease);
        }

        internal void ValidateActionSamplingFrame(ulong frameIdentity)
        {
            RequireActionSamplingFrame(frameIdentity);
            m_ActionSampling.ValidateFrame(m_ActionSamplingFrame);
        }

        internal void CommitActionSamplingFrame(ulong frameIdentity)
        {
            RequireActionSamplingFrame(frameIdentity);
            m_ActionSampling.SealFrame(m_ActionSamplingFrame);
            m_ActionSamplingFrame = null;
        }

        internal void DiscardActionSamplingFrame(ulong frameIdentity)
        {
            RequireActionSamplingFrame(frameIdentity);
            m_ActionSampling.DiscardFrame(m_ActionSamplingFrame);
            m_ActionSamplingFrame = null;
        }

        internal void BuildCommittedActionTimeSnapshots(
            FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>
                destination) =>
            m_ActionSampling.BuildCommittedTimeSnapshots(destination);

        internal void ResetActionSampling()
        {
            if (m_ActionSamplingFrame?.IsValid == true)
            {
                throw new InvalidOperationException(
                    "Pose Source Action sampling cannot reset during a frame.");
            }
            m_ActionSampling.Reset();
        }

        internal CharacterPoseSourceTuningView RequireTuning(
            ulong generation) =>
            m_Tuning.RequireCommitted(generation);

        internal string PrepareTuningCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation)
        {
            try
            {
                m_Tuning.PrepareCandidate(layout, block, generation);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        internal void CommitTuningCandidate(ulong generation) =>
            m_Tuning.CommitCandidate(generation);

        internal void DiscardTuningCandidate() =>
            m_Tuning.DiscardCandidate();

        internal CharacterPoseSourceFrameLease BeginFrame(
            in CharacterPoseFrameLineage lineage)
        {
            RequireActionSamplingFrame(lineage.FrameIdentity);
            m_Tuning.RequireCommitted(lineage.TuningGeneration);
            m_ReleasePage.RequireEmpty();
            m_ReleaseValidationIdentities.Clear();
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

        void BeginBindingFrame(ulong completionIdentity)
        {
            m_BindingPage.Begin(completionIdentity);
        }

        internal void BindDemand(
            CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourceDemand demand)
        {
            BeginBindingFrame(demand.Lineage.CompletionIdentity);
            m_FramePage.BindDemand(lease, in demand);
        }

        internal CharacterPoseSourceDemand RequireDemand(
            CharacterPoseSourceFrameLease lease) =>
            m_FramePage.RequireDemand(lease);

        internal CharacterPoseSourceFrameResult PrepareFrameResult(
            CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourcePreparedResources preparedResources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSources)
        {
            CharacterPoseSourceDemand demand =
                m_FramePage.RequireDemand(lease);
            var result = new CharacterPoseSourceFrameResult(
                in demand,
                in preparedResources,
                actionSources,
                providerSources);
            m_FramePage.BindResult(lease, in result);
            return result;
        }

        internal CharacterPoseSourcePreparedResources
            RequirePreparedResources(
                CharacterPoseSourceFrameLease lease)
        {
            CharacterPoseSourceDemand demand =
                m_FramePage.RequireDemand(lease);
            CharacterPoseFrameLineage lineage = demand.Lineage;
            return new CharacterPoseSourcePreparedResources(
                in lineage,
                m_BindingPage);
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

        internal AnimationResolvedPoseSourceSample ResolveProviderSample(
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
            var request = new AnimationPoseSampleRequest(
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
            return new AnimationResolvedPoseSourceSample(
                request,
                in sample.LeftFootFeatures,
                in sample.RightFootFeatures,
                sample.HasFootFeatures);
        }

        internal void Prepare(
            CharacterPoseSourceFrameLease lease,
            in CharacterPoseSourcePreparationView preparations,
            int preparationIndex)
        {
            CharacterPoseSourcePreparation preparation =
                m_FramePage.ConsumePreparation(
                    lease,
                    in preparations,
                    preparationIndex);
            if (!preparation.IsValid)
            {
                throw new ArgumentException(
                    "Character Pose source preparation is invalid.",
                    nameof(preparation));
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
        }

        void PrepareActionAndConnect(
            AnimationPoseSampleRequest request,
            AnimationPoseSourceCaptureBinding capture,
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
                    m_Backend.ContainsCommitted(
                        request.SourceId,
                        poseNodeId)
                        ? default
                        : m_Catalog.RequireAction(
                            request.SourceOwnerIndex),
                    in capture,
                    poseNodeId);
            Connect(physical, prepared);
        }

        void PrepareProviderAndConnect(
            AnimationPoseSampleRequest request,
            PresentationPoseSourceSample sample,
            AnimationPoseSourceCaptureBinding capture,
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
                    m_Backend.ContainsCommitted(
                        request.SourceId,
                        poseNodeId)
                        ? default
                        : m_Catalog.RequireMotionMatching(
                            in sample),
                    in capture,
                    poseNodeId);
            Connect(physical, prepared);
        }

        void PrepareDirectAndConnect(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            PresentationPoseSourceSample sample,
            AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                sourceId,
                sourceOwnerIndex,
                clips,
                m_Backend.ContainsCommitted(sourceId, poseNodeId)
                    ? default
                    : m_Catalog.RequireMotionMatching(in sample),
                in capture,
                poseNodeId);
            m_BindingPage.BindDirect(bindingIndex, in binding);
        }

        void PrepareClipAndConnect(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                sourceId,
                sourceOwnerIndex,
                clips,
                m_Backend.ContainsCommitted(sourceId, poseNodeId)
                    ? default
                    : m_Catalog.RequireClip(sourceId),
                in capture,
                poseNodeId);
            m_BindingPage.BindClip(bindingIndex, in binding);
        }

        void PrepareBlendSpaceAndConnect(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            CharacterPoseSourceBinding binding =
                PreparePlayerAndConnect(
                    sourceId,
                    sourceOwnerIndex,
                    clips,
                    m_Backend.ContainsCommitted(
                        sourceId,
                        poseNodeId)
                        ? default
                        : m_Catalog.RequireBlendSpace(bindingIndex),
                    in capture,
                    poseNodeId);
            m_BindingPage.BindBlendSpace(
                bindingIndex,
                in binding);
        }

        CharacterPoseSourceBinding PreparePlayerAndConnect(
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
                clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            if (m_BindingPage.CompletionIdentity == 0 ||
                capture.CompletionIdentity !=
                    m_BindingPage.CompletionIdentity)
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
            return new CharacterPoseSourceBinding(
                physical,
                capture.SourceIndex);
        }

        internal void ValidatePhysicalFrame() =>
            m_PhysicalSources.ValidateFrame();

        internal void ValidateFrame(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireReady(lease);
            m_ReleaseValidationIdentities.Clear();
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

        internal void DiscardFrame(
            CharacterPoseSourceFrameLease lease)
        {
            m_FramePage.RequireOpen(lease);
            Exception failure = null;
            int pendingRegistrationCount = 0;
            DiscardStep(
                () => pendingRegistrationCount =
                    m_PhysicalSources.PendingRegistrationCount,
                ref failure);
            for (int i = pendingRegistrationCount - 1; i >= 0; i--)
            {
                AnimationPhysicalSourceIdentity physical = default;
                DiscardStep(
                    () => physical =
                        m_PhysicalSources.GetPendingRegistration(i),
                    ref failure);
                if (physical.IsValid)
                    DiscardStep(() => Disconnect(physical), ref failure);
            }
            DiscardStep(
                () => m_Backend.DiscardFrame(lease),
                ref failure);
            DiscardStep(
                () => m_FramePage.Discard(lease),
                ref failure);
            DiscardStep(
                m_PhysicalSources.DiscardFrame,
                ref failure);
            m_BindingPage.Clear();
            m_ReleasePage.Clear();
            m_ReleaseValidationIdentities.Clear();
            if (failure != null)
            {
                throw new AggregateException(
                    "Pose Source Pending discard failed.",
                    failure);
            }
        }

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
                !m_Backend.ContainsCommitted(
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
                m_Backend.StageRelease(
                    permission.SourceId,
                    permission.PoseNodeId);
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
            m_Backend.Release(in prepared.BackendRelease);
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
            PoseNodeId poseNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity) =>
            m_PhysicalSources.RecordRelease(
                poseNodeId,
                sourceId,
                completionIdentity);

        internal void CompleteDeferredReleases()
        {
            m_ReleasePage.RequireEmpty();
            m_Backend.ExecuteDeferredReleases();
            m_PhysicalSources.CompleteReleaseDiagnostics();
        }

        internal void CancelReleaseDiagnostics()
        {
            m_ReleaseValidationIdentities.Clear();
            m_PhysicalSources.CancelReleaseDiagnostics();
        }

        internal bool ReleaseAcknowledgementsValidated =>
            m_ReleaseCompletions.AcknowledgementsValidated;

        internal void RequireActionBackendReleaseCompletionCapacity(
            int additionalCount) =>
            m_ReleaseCompletions.RequireActionBackendCapacity(
                additionalCount);

        internal void RecordReleaseCompletion(
            in ActionBackendReleaseCompletion completion) =>
            m_ReleaseCompletions.Record(in completion);

        internal void RecordReleaseCompletion(
            in AnimationSlotSourceReleaseCompletion completion) =>
            m_ReleaseCompletions.Record(in completion);

        internal void CopyActionBackendReleaseCompletions(
            List<ActionBackendReleaseCompletion> destination) =>
            m_ReleaseCompletions.CopyActionBackend(destination);

        internal void CopyActionSlotReleaseCompletions(
            List<AnimationSlotSourceReleaseCompletion> destination) =>
            m_ReleaseCompletions.CopyActionSlot(destination);

        internal void ValidateActionBackendReleaseAcknowledgements(
            IReadOnlyList<ActionBackendReleaseCompletion>
                completions) =>
            m_ReleaseCompletions.ValidateAcknowledgements(completions);

        internal void ApplyActionBackendReleaseAcknowledgements() =>
            m_ReleaseCompletions.ApplyAcknowledgements();

        internal void ClearValidatedReleaseAcknowledgements() =>
            m_ReleaseCompletions.ClearValidatedAcknowledgements();

        internal void ClearActionSlotReleaseCompletions() =>
            m_ReleaseCompletions.ClearActionSlot();

        internal void ClearActionBackendReleaseCompletions() =>
            m_ReleaseCompletions.ClearActionBackend();

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
            m_BindingPage.Clear();
            m_UsagePage.Clear();
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
            m_ReleaseCompletions.Clear();
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
            m_BindingPage.Clear();
            m_UsagePage.Clear();
            m_ReleasePage.Clear();
            m_ReleaseCompletions.Clear();
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

        static void DiscardStep(
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

        void RequireActionSamplingFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0 ||
                m_ActionSamplingFrame?.IsValid != true ||
                m_ActionSamplingFrame.Identity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Pose Source Action sampling frame is stale.");
            }
        }
    }
}
