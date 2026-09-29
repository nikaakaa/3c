using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseSourceFrameLease
    {
        internal CharacterPoseSourceFrameLease(
            in CharacterPoseNativeFrameLineage lineage)
        {
            if (!lineage.IsOpenValid || lineage.CompletionIdentity != 0)
                throw new ArgumentException(
                    "Pose Source Frame Lease lineage is invalid.",
                    nameof(lineage));
            m_Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        readonly CharacterPoseNativeFrameLineage m_Lineage;
        internal ref readonly CharacterPoseNativeFrameLineage Lineage => ref m_Lineage;
        internal ulong FrameIdentity => Lineage.FrameIdentity;
        internal bool IsValid => m_IsValid;
        internal bool Matches(in CharacterPoseNativeFrameLineage lineage) =>
            m_IsValid &&
            m_Lineage.MatchesIgnoringCompletion(in lineage);
    }

    internal readonly struct CharacterPoseConstraintFrameLease
    {
        internal CharacterPoseConstraintFrameLease(
            in CharacterPoseNativeFrameLineage lineage)
        {
            if (!lineage.IsOpenValid || lineage.CompletionIdentity != 0)
                throw new ArgumentException(
                    "Pose Constraint Frame Lease lineage is invalid.",
                    nameof(lineage));
            m_Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        readonly CharacterPoseNativeFrameLineage m_Lineage;
        internal ref readonly CharacterPoseNativeFrameLineage Lineage => ref m_Lineage;
        internal ulong FrameIdentity => Lineage.FrameIdentity;
        internal ulong PresentationFrame => Lineage.PresentationFrame;
        internal bool IsValid => m_IsValid;
        internal bool Matches(in CharacterPoseNativeFrameLineage lineage) =>
            m_IsValid &&
            m_Lineage.MatchesIgnoringCompletion(in lineage);
    }

    internal readonly struct CharacterPoseSourceDemand
    {
        internal CharacterPoseSourceDemand(
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseSourcePreparationView preparations,
            IReadOnlyList<PoseSourceProviderDemand> providerDemands,
            int actionSourceCount,
            int providerSourceCount)
        {
            if (!lineage.IsValid ||
                !preparations.IsValid ||
                preparations.CompletionIdentity != lineage.CompletionIdentity ||
                providerDemands == null ||
                actionSourceCount < 0 ||
                providerSourceCount < 0)
            {
                throw new ArgumentException(
                    "Character Pose source demand is invalid.");
            }
            for (int i = 0; i < providerDemands.Count; i++)
            {
                PoseSourceProviderDemand demand = providerDemands[i];
                if (!demand.IsValid ||
                    demand.FrameSequence != lineage.PresentationFrame)
                {
                    throw new ArgumentException(
                        "Character Pose provider demand lineage is invalid.");
                }
            }
            Lineage = lineage;
            Preparations = preparations;
            ProviderDemands = providerDemands;
            ActionSourceCount = actionSourceCount;
            ProviderSourceCount = providerSourceCount;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal CharacterPoseSourcePreparationView Preparations { get; }
        internal IReadOnlyList<PoseSourceProviderDemand> ProviderDemands { get; }
        internal int ActionSourceCount { get; }
        internal int ProviderSourceCount { get; }
        internal bool IsValid => m_IsValid && Preparations.IsValid;
    }

    internal readonly struct CharacterPoseSourceBinding
    {
        internal CharacterPoseSourceBinding(
            AnimationPhysicalSourceIdentity physicalIdentity,
            int sourceIndex,
            in CharacterPoseSourceScalarReadView scalarReadView)
        {
            if (!physicalIdentity.IsValid || sourceIndex < 0)
                throw new ArgumentException("Pose source binding is invalid.");
            if (!scalarReadView.IsValid ||
                scalarReadView.PhysicalIdentity != physicalIdentity)
            {
                throw new ArgumentException(
                    "Pose source scalar binding is invalid.");
            }
            PhysicalIdentity = physicalIdentity;
            m_EncodedSourceIndex = checked(sourceIndex + 1);
            ScalarReadView = scalarReadView;
        }

        readonly int m_EncodedSourceIndex;
        internal AnimationPhysicalSourceIdentity PhysicalIdentity { get; }
        internal int SourceIndex => m_EncodedSourceIndex - 1;
        internal CharacterPoseSourceScalarReadView ScalarReadView { get; }
        internal bool IsValid =>
            PhysicalIdentity.IsValid &&
            m_EncodedSourceIndex > 0 &&
            ScalarReadView.IsValid &&
            ScalarReadView.PhysicalIdentity == PhysicalIdentity;
    }

    internal sealed class CharacterPoseSourceBindingPage
    {
        sealed class BindingTable
        {
            internal readonly CharacterPoseSourceBinding[] Values;
            readonly int[] m_WrittenIndices;
            int m_WrittenCount;

            internal BindingTable(int capacity)
            {
                Values = new CharacterPoseSourceBinding[capacity];
                m_WrittenIndices = new int[capacity];
            }

            internal void Bind(int index, in CharacterPoseSourceBinding binding)
            {
                if (!Values[index].PhysicalIdentity.IsValid)
                    m_WrittenIndices[m_WrittenCount++] = index;
                Values[index] = binding;
            }

            internal void Clear()
            {
                for (int i = 0; i < m_WrittenCount; i++)
                    Values[m_WrittenIndices[i]] = default;
                m_WrittenCount = 0;
            }
        }

        readonly BindingTable m_Direct;
        readonly BindingTable m_Clip;
        readonly BindingTable m_BlendSpace;
        ulong m_CompletionIdentity;

        internal CharacterPoseSourceBindingPage(
            int directCapacity,
            int clipCapacity,
            int blendSpaceCapacity)
        {
            m_Direct = new BindingTable(
                RequireCapacity(directCapacity, nameof(directCapacity)));
            m_Clip = new BindingTable(
                RequireCapacity(clipCapacity, nameof(clipCapacity)));
            m_BlendSpace = new BindingTable(
                RequireCapacity(blendSpaceCapacity, nameof(blendSpaceCapacity)));
        }

        internal ulong CompletionIdentity => m_CompletionIdentity;

        internal void Begin(ulong completionIdentity)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            Clear();
            m_CompletionIdentity = completionIdentity;
        }

        internal void BindDirect(int index, in CharacterPoseSourceBinding binding) =>
            Bind(m_Direct, index, in binding);
        internal void BindClip(int index, in CharacterPoseSourceBinding binding) =>
            Bind(m_Clip, index, in binding);
        internal void BindBlendSpace(int index, in CharacterPoseSourceBinding binding) =>
            Bind(m_BlendSpace, index, in binding);
        internal CharacterPoseSourceBinding RequireDirect(int index, ulong completionIdentity) =>
            Require(m_Direct, index, completionIdentity);
        internal CharacterPoseSourceBinding RequireClip(int index, ulong completionIdentity) =>
            Require(m_Clip, index, completionIdentity);
        internal CharacterPoseSourceBinding RequireBlendSpace(int index, ulong completionIdentity) =>
            Require(m_BlendSpace, index, completionIdentity);
        internal bool Matches(ulong completionIdentity) =>
            completionIdentity != 0 && completionIdentity == m_CompletionIdentity;

        internal void Clear()
        {
            m_Direct.Clear();
            m_Clip.Clear();
            m_BlendSpace.Clear();
            m_CompletionIdentity = 0;
        }

        void Bind(
            BindingTable bindings,
            int index,
            in CharacterPoseSourceBinding binding)
        {
            if (m_CompletionIdentity == 0 || !binding.IsValid)
                throw new InvalidOperationException(
                    "Pose source binding page is not open.");
            bindings.Bind(index, in binding);
        }

        CharacterPoseSourceBinding Require(
            BindingTable bindings,
            int index,
            ulong completionIdentity)
        {
            if ((uint)index >= (uint)bindings.Values.Length ||
                !Matches(completionIdentity))
            {
                throw new InvalidOperationException(
                    "Pose source binding request is stale.");
            }
            CharacterPoseSourceBinding binding = bindings.Values[index];
            if (!binding.IsValid)
                throw new InvalidOperationException(
                    "Pose source binding is not prepared.");
            return binding;
        }

        static int RequireCapacity(int capacity, string parameterName)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(parameterName);
            return capacity;
        }
    }

    internal readonly struct CharacterPoseSourcePreparedResources
    {
        internal CharacterPoseSourcePreparedResources(
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseSourceBindingPage bindings)
        {
            if (!lineage.IsValid ||
                bindings == null ||
                !bindings.Matches(lineage.CompletionIdentity))
            {
                throw new ArgumentException(
                    "Character Pose prepared source resources are invalid.");
            }
            Lineage = lineage;
            m_Bindings = bindings;
        }

        readonly CharacterPoseSourceBindingPage m_Bindings;
        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal bool IsValid =>
            Lineage.IsValid &&
            m_Bindings != null &&
            m_Bindings.Matches(Lineage.CompletionIdentity);
        internal CharacterPoseSourceBinding RequireDirectBinding(int index) =>
            m_Bindings.RequireDirect(index, Lineage.CompletionIdentity);
        internal CharacterPoseSourceBinding RequireClipBinding(int index) =>
            m_Bindings.RequireClip(index, Lineage.CompletionIdentity);
        internal CharacterPoseSourceBinding RequireBlendSpaceBinding(int index) =>
            m_Bindings.RequireBlendSpace(index, Lineage.CompletionIdentity);
    }

    internal readonly struct CharacterPoseSourceUsage
    {
        internal CharacterPoseSourceUsage(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            if (!playerNodeId.IsValid || !sourceId.IsValid || completionIdentity == 0)
                throw new ArgumentException("Character Pose source usage is invalid.");
            PlayerNodeId = playerNodeId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
        }

        internal PoseNodeId PlayerNodeId { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal ulong CompletionIdentity { get; }
        internal bool IsValid =>
            PlayerNodeId.IsValid && SourceId.IsValid && CompletionIdentity != 0;
    }

    internal sealed class CharacterPoseSourceUsagePage
    {
        readonly CharacterPoseSourceUsage[] m_Usages;
        ulong m_CompletionIdentity;
        int m_Count;

        internal CharacterPoseSourceUsagePage(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Usages = new CharacterPoseSourceUsage[capacity];
        }

        internal void Begin(ulong completionIdentity)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            Clear();
            m_CompletionIdentity = completionIdentity;
        }

        internal void Add(in CharacterPoseSourceUsage usage)
        {
            if (!usage.IsValid || usage.CompletionIdentity != m_CompletionIdentity)
                throw new InvalidOperationException(
                    "Character Pose source usage does not match the open page.");
            for (int i = 0; i < m_Count; i++)
            {
                CharacterPoseSourceUsage current = m_Usages[i];
                if (current.PlayerNodeId == usage.PlayerNodeId &&
                    current.SourceId.Equals(usage.SourceId))
                    return;
            }
            if (m_Count >= m_Usages.Length)
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan source usage capacity was exceeded.");
            m_Usages[m_Count++] = usage;
        }

        internal bool Matches(ulong completionIdentity) =>
            completionIdentity != 0 && completionIdentity == m_CompletionIdentity;
        internal int RequireCount(ulong completionIdentity)
        {
            RequireOpen(completionIdentity);
            return m_Count;
        }
        internal CharacterPoseSourceUsage Require(int index, ulong completionIdentity)
        {
            RequireOpen(completionIdentity);
            if ((uint)index >= (uint)m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Usages[index];
        }

        internal void Clear()
        {
            Array.Clear(m_Usages, 0, m_Count);
            m_CompletionIdentity = 0;
            m_Count = 0;
        }

        void RequireOpen(ulong completionIdentity)
        {
            if (!Matches(completionIdentity))
                throw new InvalidOperationException(
                    "Character Pose source usage page is stale.");
        }
    }

    internal readonly struct CharacterPoseSourceUsageView
    {
        internal CharacterPoseSourceUsageView(
            CharacterPoseSourceUsagePage page,
            ulong completionIdentity)
        {
            if (page == null || !page.Matches(completionIdentity))
                throw new ArgumentException(
                    "Character Pose source usage view is invalid.");
            m_Page = page;
            CompletionIdentity = completionIdentity;
        }

        readonly CharacterPoseSourceUsagePage m_Page;
        internal ulong CompletionIdentity { get; }
        internal int Count => m_Page.RequireCount(CompletionIdentity);
        internal bool IsValid =>
            m_Page != null && m_Page.Matches(CompletionIdentity);
        internal CharacterPoseSourceUsage Get(int index) =>
            m_Page.Require(index, CompletionIdentity);
    }

    internal enum CharacterPoseSourceFrameOutcome : byte
    {
        AwaitingSample = 1,
        Prepared = 2,
        Invalid = 3
    }

    internal readonly struct CharacterPoseSourceFrameResult
    {
        internal CharacterPoseSourceFrameResult(
            in CharacterPoseSourceDemand demand,
            in CharacterPoseSourcePreparedResources preparedResources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSources,
            in CharacterPoseSourceReadinessPageView readinessPage,
            bool hasCurrentSource)
        {
            if (!demand.IsValid ||
                !preparedResources.IsValid ||
                preparedResources.Lineage != demand.Lineage ||
                !readinessPage.IsValid ||
                readinessPage.CompletionIdentity != demand.Lineage.CompletionIdentity ||
                actionSources == null ||
                providerSources == null ||
                actionSources.Count != demand.ActionSourceCount ||
                providerSources.Count != demand.ProviderSourceCount)
            {
                throw new ArgumentException(
                    "Character Pose source frame does not match its demand.");
            }
            CharacterPoseSourceReadinessView readiness = readinessPage.Current;
            CharacterPoseSourceReadinessView targetReadiness = readinessPage.DeferredTarget;
            if (!readiness.IsValid ||
                readiness.CompletionIdentity != demand.Lineage.CompletionIdentity ||
                !targetReadiness.IsValid ||
                targetReadiness.CompletionIdentity != demand.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose source readiness page does not match its demand.");
            }
            bool pending = readiness.IsPending ||
                !hasCurrentSource && targetReadiness.IsPending;
            bool invalid = readiness.IsInvalid;
            if (!hasCurrentSource && targetReadiness.IsInvalid)
                invalid = true;
            foreach (AnimationResolvedPoseSourceSample sample in actionSources.Values)
            {
                if (sample?.IsValid != true)
                    invalid = true;
            }
            foreach (PresentationPoseSourceSample sample in providerSources.Values)
            {
                if (sample?.IsValid != true ||
                    sample.FrameSequence != demand.Lineage.PresentationFrame ||
                    sample.Availability == PresentationPoseSourceAvailability.Invalid)
                {
                    invalid = true;
                }
                else if (sample.Availability == PresentationPoseSourceAvailability.Pending)
                    pending = true;
            }
            Demand = demand;
            PreparedResources = preparedResources;
            ActionSources = actionSources;
            ProviderSources = providerSources;
            ReadinessPage = readinessPage;
            CurrentReadiness = readiness;
            TargetReadiness = targetReadiness;
            Availability = invalid
                ? PresentationPoseSourceAvailability.Invalid
                : pending
                    ? PresentationPoseSourceAvailability.Pending
                    : PresentationPoseSourceAvailability.Ready;
            Outcome = invalid
                ? CharacterPoseSourceFrameOutcome.Invalid
                : pending
                    ? CharacterPoseSourceFrameOutcome.AwaitingSample
                    : CharacterPoseSourceFrameOutcome.Prepared;
            FailureReason = invalid
                ? readiness.IsInvalid
                    ? readiness.FailureReason
                    : targetReadiness.IsInvalid
                        ? targetReadiness.FailureReason
                        : PresentationPoseSourceFailureReason.SampleInvalid
                : PresentationPoseSourceFailureReason.None;
        }

        internal CharacterPoseSourceDemand Demand { get; }
        internal CharacterPoseSourcePreparedResources PreparedResources { get; }
        internal CharacterPoseNativeFrameLineage Lineage => Demand.Lineage;
        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            AnimationResolvedPoseSourceSample> ActionSources { get; }
        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            PresentationPoseSourceSample> ProviderSources { get; }
        internal CharacterPoseSourceReadinessPageView ReadinessPage { get; }
        internal CharacterPoseSourceReadinessView CurrentReadiness { get; }
        internal CharacterPoseSourceReadinessView TargetReadiness { get; }
        internal PresentationPoseSourceAvailability Availability { get; }
        internal CharacterPoseSourceFrameOutcome Outcome { get; }
        internal PresentationPoseSourceFailureReason FailureReason { get; }
        internal bool IsValid =>
            Demand.IsValid &&
            PreparedResources.IsValid &&
            PreparedResources.Lineage == Demand.Lineage &&
            ActionSources != null &&
            ProviderSources != null &&
            CurrentReadiness.IsValid &&
            TargetReadiness.IsValid &&
            ActionSources.Count == Demand.ActionSourceCount &&
            ProviderSources.Count == Demand.ProviderSourceCount &&
            (Availability == PresentationPoseSourceAvailability.Ready
                ? Outcome == CharacterPoseSourceFrameOutcome.Prepared &&
                  FailureReason == PresentationPoseSourceFailureReason.None
                : Availability == PresentationPoseSourceAvailability.Pending
                    ? Outcome == CharacterPoseSourceFrameOutcome.AwaitingSample &&
                      FailureReason == PresentationPoseSourceFailureReason.None
                    : Availability == PresentationPoseSourceAvailability.Invalid &&
                      Outcome == CharacterPoseSourceFrameOutcome.Invalid &&
                      FailureReason != PresentationPoseSourceFailureReason.None);
        internal bool IsReady =>
            IsValid && Availability == PresentationPoseSourceAvailability.Ready;
    }

    internal readonly struct CharacterPoseSourceCommittedResult
    {
        internal CharacterPoseSourceCommittedResult(
            in CharacterPoseSourceFrameResult sourceFrame)
        {
            if (!sourceFrame.IsReady)
                throw new ArgumentException(
                    "Character Pose source frame cannot publish a committed result.",
                    nameof(sourceFrame));
            Lineage = sourceFrame.Lineage;
            Availability = sourceFrame.Availability;
            Outcome = sourceFrame.Outcome;
            FailureReason = sourceFrame.FailureReason;
        }

        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal PresentationPoseSourceAvailability Availability { get; }
        internal CharacterPoseSourceFrameOutcome Outcome { get; }
        internal PresentationPoseSourceFailureReason FailureReason { get; }
        internal bool IsCommitted =>
            Lineage.IsValid &&
            Availability == PresentationPoseSourceAvailability.Ready &&
            Outcome == CharacterPoseSourceFrameOutcome.Prepared &&
            FailureReason == PresentationPoseSourceFailureReason.None;
    }

    internal enum CharacterPoseOperationOutcome : byte
    {
        None = 0,
        Completed = 1,
        Skipped = 2,
        TypedInvalid = 3
    }

    internal readonly struct CharacterPoseOperationCompletion
    {
        internal CharacterPoseOperationCompletion(
            ulong completionIdentity,
            CharacterPoseOperationOutcome outcome)
        {
            if (completionIdentity == 0 ||
                outcome < CharacterPoseOperationOutcome.Completed ||
                outcome > CharacterPoseOperationOutcome.TypedInvalid)
            {
                throw new ArgumentOutOfRangeException(nameof(outcome));
            }
            CompletionIdentity = completionIdentity;
            Outcome = outcome;
        }

        internal ulong CompletionIdentity { get; }
        internal CharacterPoseOperationOutcome Outcome { get; }
        internal bool IsEmpty =>
            CompletionIdentity == 0 && Outcome == CharacterPoseOperationOutcome.None;
        internal bool IsValid =>
            CompletionIdentity != 0 &&
            Outcome >= CharacterPoseOperationOutcome.Completed &&
            Outcome <= CharacterPoseOperationOutcome.TypedInvalid;
        internal bool Matches(ulong completionIdentity) =>
            IsValid && CompletionIdentity == completionIdentity;
    }

    internal readonly struct CharacterPoseConstraintResult
    {
        internal CharacterPoseConstraintResult(
            in CharacterPoseNativeFrameLineage lineage,
            AnimationPresentationFrameOutcome outcome,
            AnimationPoseAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            int goalCount,
            bool solverProduced,
            in CharacterFullBodyIkResult fullBodyIk)
        {
            Lineage = lineage;
            Outcome = outcome;
            Availability = availability;
            InvalidReason = invalidReason;
            GoalCount = goalCount;
            SolverProduced = solverProduced;
            FullBodyIk = fullBodyIk;
            m_IsValid =
                lineage.IsValid &&
                (outcome == AnimationPresentationFrameOutcome.Committed
                    ? availability == AnimationPoseAvailability.Pose &&
                      invalidReason == AnimationPoseNativeInvalidReason.None &&
                      goalCount >= 0 &&
                      solverProduced &&
                      fullBodyIk.Succeeded
                    : outcome == AnimationPresentationFrameOutcome.TypedInvalid &&
                      availability != AnimationPoseAvailability.Pose &&
                      invalidReason != AnimationPoseNativeInvalidReason.None &&
                      goalCount >= -1 &&
                      (!solverProduced || !fullBodyIk.Succeeded));
        }

        readonly bool m_IsValid;
        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal AnimationPresentationFrameOutcome Outcome { get; }
        internal AnimationPoseAvailability Availability { get; }
        internal AnimationPoseNativeInvalidReason InvalidReason { get; }
        internal int GoalCount { get; }
        internal bool SolverProduced { get; }
        internal CharacterFullBodyIkResult FullBodyIk { get; }
        internal bool IsValid => m_IsValid;
        internal bool IsCompleted =>
            IsValid && Outcome == AnimationPresentationFrameOutcome.Committed;
    }
}
