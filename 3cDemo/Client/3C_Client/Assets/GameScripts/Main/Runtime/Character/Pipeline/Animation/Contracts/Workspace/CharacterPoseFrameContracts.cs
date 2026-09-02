using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterPoseFrameLineage :
        IEquatable<CharacterPoseFrameLineage>
    {
        public CharacterPoseFrameLineage(
            ActorId actorId,
            ulong frameIdentity,
            ulong completionIdentity,
            ulong presentationFrame,
            ulong bodyTick,
            string programId,
            string poseGraphId,
            string poseGraphRevision,
            string poseProgramIdentity,
            string projectionRevision,
            string rigId,
            string rigRevision,
            ulong tuningGeneration)
        {
            ActorId = actorId;
            FrameIdentity = frameIdentity;
            CompletionIdentity = completionIdentity;
            PresentationFrame = presentationFrame;
            BodyTick = bodyTick;
            ProgramId = programId ?? string.Empty;
            PoseGraphId = poseGraphId ?? string.Empty;
            PoseGraphRevision = poseGraphRevision ?? string.Empty;
            PoseProgramIdentity = poseProgramIdentity ?? string.Empty;
            ProjectionRevision = projectionRevision ?? string.Empty;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            TuningGeneration = tuningGeneration;
        }

        public ActorId ActorId { get; }
        public ulong FrameIdentity { get; }
        public ulong CompletionIdentity { get; }
        public ulong PresentationFrame { get; }
        public ulong BodyTick { get; }
        public string ProgramId { get; }
        public string PoseGraphId { get; }
        public string PoseGraphRevision { get; }
        public string PoseProgramIdentity { get; }
        public string ProjectionRevision { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public ulong TuningGeneration { get; }
        internal bool IsOpenValid =>
            ActorId.IsValid &&
            FrameIdentity != 0 &&
            PresentationFrame != 0 &&
            BodyTick != 0 &&
            !string.IsNullOrEmpty(ProgramId) &&
            !string.IsNullOrEmpty(PoseGraphId) &&
            !string.IsNullOrEmpty(PoseGraphRevision) &&
            !string.IsNullOrEmpty(PoseProgramIdentity) &&
            !string.IsNullOrEmpty(ProjectionRevision) &&
            !string.IsNullOrEmpty(RigId) &&
            !string.IsNullOrEmpty(RigRevision) &&
            TuningGeneration != 0;
        public bool IsValid => IsOpenValid && CompletionIdentity != 0;

        internal CharacterPoseFrameLineage WithCompletion(
            ulong completionIdentity) =>
            new CharacterPoseFrameLineage(
                ActorId,
                FrameIdentity,
                completionIdentity,
                PresentationFrame,
                BodyTick,
                ProgramId,
                PoseGraphId,
                PoseGraphRevision,
                PoseProgramIdentity,
                ProjectionRevision,
                RigId,
                RigRevision,
                TuningGeneration);

        public bool Equals(CharacterPoseFrameLineage other) =>
            ActorId == other.ActorId &&
            FrameIdentity == other.FrameIdentity &&
            CompletionIdentity == other.CompletionIdentity &&
            PresentationFrame == other.PresentationFrame &&
            BodyTick == other.BodyTick &&
            string.Equals(ProgramId, other.ProgramId, StringComparison.Ordinal) &&
            string.Equals(PoseGraphId, other.PoseGraphId, StringComparison.Ordinal) &&
            string.Equals(PoseGraphRevision, other.PoseGraphRevision, StringComparison.Ordinal) &&
            string.Equals(PoseProgramIdentity, other.PoseProgramIdentity, StringComparison.Ordinal) &&
            string.Equals(ProjectionRevision, other.ProjectionRevision, StringComparison.Ordinal) &&
            string.Equals(RigId, other.RigId, StringComparison.Ordinal) &&
            string.Equals(RigRevision, other.RigRevision, StringComparison.Ordinal) &&
            TuningGeneration == other.TuningGeneration;

        public override bool Equals(object obj) =>
            obj is CharacterPoseFrameLineage other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                ActorId,
                FrameIdentity,
                CompletionIdentity,
                PresentationFrame,
                HashCode.Combine(
                    BodyTick,
                    ProgramId,
                    PoseGraphId,
                    PoseGraphRevision,
                    PoseProgramIdentity,
                    ProjectionRevision,
                    RigId,
                    HashCode.Combine(
                        RigRevision,
                        TuningGeneration)));

        public static bool operator ==(
            CharacterPoseFrameLineage left,
            CharacterPoseFrameLineage right) => left.Equals(right);

        public static bool operator !=(
            CharacterPoseFrameLineage left,
            CharacterPoseFrameLineage right) => !left.Equals(right);
    }

    internal readonly struct CharacterPoseProgramFrameLease
    {
        internal CharacterPoseProgramFrameLease(
            in CharacterPoseFrameLineage lineage)
        {
            if (!lineage.IsOpenValid || lineage.CompletionIdentity != 0)
                throw new ArgumentException("Pose Program Frame Lease lineage is invalid.", nameof(lineage));
            Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal CharacterPoseFrameLineage Lineage { get; }
        internal ulong FrameIdentity => Lineage.FrameIdentity;
        internal bool IsValid => m_IsValid;
        internal bool Matches(CharacterPoseFrameLineage lineage) =>
            m_IsValid &&
            lineage.WithCompletion(0) == Lineage;
    }

    internal readonly struct CharacterPoseSourceFrameLease
    {
        internal CharacterPoseSourceFrameLease(
            in CharacterPoseFrameLineage lineage)
        {
            if (!lineage.IsOpenValid || lineage.CompletionIdentity != 0)
                throw new ArgumentException("Pose Source Frame Lease lineage is invalid.", nameof(lineage));
            Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal CharacterPoseFrameLineage Lineage { get; }
        internal ulong FrameIdentity => Lineage.FrameIdentity;
        internal bool IsValid => m_IsValid;
        internal bool Matches(CharacterPoseFrameLineage lineage) =>
            m_IsValid &&
            lineage.WithCompletion(0) == Lineage;
    }

    internal readonly struct CharacterPoseConstraintFrameLease
    {
        internal CharacterPoseConstraintFrameLease(
            in CharacterPoseFrameLineage lineage)
        {
            if (!lineage.IsOpenValid || lineage.CompletionIdentity != 0)
                throw new ArgumentException("Pose Constraint Frame Lease lineage is invalid.", nameof(lineage));
            Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal CharacterPoseFrameLineage Lineage { get; }
        internal ulong FrameIdentity => Lineage.FrameIdentity;
        internal ulong PresentationFrame => Lineage.PresentationFrame;
        internal bool IsValid => m_IsValid;
        internal bool Matches(CharacterPoseFrameLineage lineage) =>
            m_IsValid &&
            lineage.WithCompletion(0) == Lineage;
    }

    internal readonly struct CharacterFinalPosePublicationFrameLease
    {
        internal CharacterFinalPosePublicationFrameLease(
            in CharacterPoseFrameLineage lineage)
        {
            if (!lineage.IsOpenValid || lineage.CompletionIdentity != 0)
            {
                throw new ArgumentException(
                    "Final Pose Publication Frame Lease lineage is invalid.",
                    nameof(lineage));
            }
            Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal CharacterPoseFrameLineage Lineage { get; }
        internal ulong FrameIdentity => Lineage.FrameIdentity;
        internal bool IsValid => m_IsValid;
        internal bool Matches(CharacterPoseFrameLineage lineage) =>
            m_IsValid &&
            lineage.WithCompletion(0) == Lineage;
    }

    internal readonly struct CharacterPoseSourceDemand
    {
        internal CharacterPoseSourceDemand(
            in CharacterPoseFrameLineage lineage,
            in CharacterPoseSourcePreparationView preparations,
            IReadOnlyList<PoseSourceProviderDemand> providerDemands,
            int actionSourceCount,
            int providerSourceCount)
        {
            if (!lineage.IsValid ||
                !preparations.IsValid ||
                preparations.CompletionIdentity !=
                    lineage.CompletionIdentity ||
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
        internal CharacterPoseFrameLineage Lineage { get; }
        internal CharacterPoseSourcePreparationView Preparations { get; }
        internal IReadOnlyList<PoseSourceProviderDemand> ProviderDemands
        {
            get;
        }
        internal int ActionSourceCount { get; }
        internal int ProviderSourceCount { get; }
        internal bool IsValid =>
            m_IsValid &&
            Preparations.IsValid;
    }

    internal readonly struct CharacterPoseSourceBinding
    {
        internal CharacterPoseSourceBinding(
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
        internal AnimationPhysicalSourceIdentity PhysicalIdentity { get; }
        internal int SourceIndex => m_EncodedSourceIndex - 1;
        internal bool IsValid =>
            PhysicalIdentity.IsValid &&
            m_EncodedSourceIndex > 0;
    }

    internal sealed class CharacterPoseSourceBindingPage
    {
        readonly CharacterPoseSourceBinding[] m_Direct;
        readonly CharacterPoseSourceBinding[] m_Clip;
        readonly CharacterPoseSourceBinding[] m_BlendSpace;
        ulong m_CompletionIdentity;

        internal CharacterPoseSourceBindingPage(
            int directCapacity,
            int clipCapacity,
            int blendSpaceCapacity)
        {
            m_Direct = new CharacterPoseSourceBinding[
                RequireCapacity(
                    directCapacity,
                    nameof(directCapacity))];
            m_Clip = new CharacterPoseSourceBinding[
                RequireCapacity(
                    clipCapacity,
                    nameof(clipCapacity))];
            m_BlendSpace = new CharacterPoseSourceBinding[
                RequireCapacity(
                    blendSpaceCapacity,
                    nameof(blendSpaceCapacity))];
        }

        internal ulong CompletionIdentity => m_CompletionIdentity;

        internal void Begin(ulong completionIdentity)
        {
            if (completionIdentity == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completionIdentity));
            }
            Clear();
            m_CompletionIdentity = completionIdentity;
        }

        internal void BindDirect(
            int index,
            in CharacterPoseSourceBinding binding) =>
            Bind(m_Direct, index, in binding);

        internal void BindClip(
            int index,
            in CharacterPoseSourceBinding binding) =>
            Bind(m_Clip, index, in binding);

        internal void BindBlendSpace(
            int index,
            in CharacterPoseSourceBinding binding) =>
            Bind(m_BlendSpace, index, in binding);

        internal CharacterPoseSourceBinding RequireDirect(
            int index,
            ulong completionIdentity) =>
            Require(m_Direct, index, completionIdentity);

        internal CharacterPoseSourceBinding RequireClip(
            int index,
            ulong completionIdentity) =>
            Require(m_Clip, index, completionIdentity);

        internal CharacterPoseSourceBinding RequireBlendSpace(
            int index,
            ulong completionIdentity) =>
            Require(m_BlendSpace, index, completionIdentity);

        internal bool Matches(ulong completionIdentity) =>
            completionIdentity != 0 &&
            completionIdentity == m_CompletionIdentity;

        internal void Clear()
        {
            Array.Clear(m_Direct, 0, m_Direct.Length);
            Array.Clear(m_Clip, 0, m_Clip.Length);
            Array.Clear(
                m_BlendSpace,
                0,
                m_BlendSpace.Length);
            m_CompletionIdentity = 0;
        }

        void Bind(
            CharacterPoseSourceBinding[] bindings,
            int index,
            in CharacterPoseSourceBinding binding)
        {
            if (m_CompletionIdentity == 0 || !binding.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose source binding page is not open.");
            }
            bindings[index] = binding;
        }

        CharacterPoseSourceBinding Require(
            CharacterPoseSourceBinding[] bindings,
            int index,
            ulong completionIdentity)
        {
            if ((uint)index >= (uint)bindings.Length ||
                !Matches(completionIdentity))
            {
                throw new InvalidOperationException(
                    "Pose source binding request is stale.");
            }
            CharacterPoseSourceBinding binding = bindings[index];
            if (!binding.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose source binding is not prepared.");
            }
            return binding;
        }

        static int RequireCapacity(
            int capacity,
            string parameterName)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(parameterName);
            return capacity;
        }
    }

    internal readonly struct CharacterPoseSourcePreparedResources
    {
        internal CharacterPoseSourcePreparedResources(
            in CharacterPoseFrameLineage lineage,
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
        internal CharacterPoseFrameLineage Lineage { get; }
        internal bool IsValid =>
            Lineage.IsValid &&
            m_Bindings != null &&
            m_Bindings.Matches(Lineage.CompletionIdentity);

        internal CharacterPoseSourceBinding RequireDirectBinding(
            int index) =>
            m_Bindings.RequireDirect(
                index,
                Lineage.CompletionIdentity);

        internal CharacterPoseSourceBinding RequireClipBinding(
            int index) =>
            m_Bindings.RequireClip(
                index,
                Lineage.CompletionIdentity);

        internal CharacterPoseSourceBinding RequireBlendSpaceBinding(
            int index) =>
            m_Bindings.RequireBlendSpace(
                index,
                Lineage.CompletionIdentity);
    }

    internal readonly struct CharacterPoseSourceUsage
    {
        internal CharacterPoseSourceUsage(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            if (!playerNodeId.IsValid ||
                !sourceId.IsValid ||
                completionIdentity == 0)
            {
                throw new ArgumentException(
                    "Character Pose source usage is invalid.");
            }
            PlayerNodeId = playerNodeId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
        }

        internal PoseNodeId PlayerNodeId { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal ulong CompletionIdentity { get; }
        internal bool IsValid =>
            PlayerNodeId.IsValid &&
            SourceId.IsValid &&
            CompletionIdentity != 0;
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
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completionIdentity));
            }
            Clear();
            m_CompletionIdentity = completionIdentity;
        }

        internal void Add(in CharacterPoseSourceUsage usage)
        {
            if (!usage.IsValid ||
                usage.CompletionIdentity != m_CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose source usage does not match the open page.");
            }
            for (int i = 0; i < m_Count; i++)
            {
                CharacterPoseSourceUsage current = m_Usages[i];
                if (current.PlayerNodeId == usage.PlayerNodeId &&
                    current.SourceId.Equals(usage.SourceId))
                {
                    return;
                }
            }
            if (m_Count >= m_Usages.Length)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan source usage capacity was exceeded.");
            }
            m_Usages[m_Count++] = usage;
        }

        internal bool Matches(ulong completionIdentity) =>
            completionIdentity != 0 &&
            completionIdentity == m_CompletionIdentity;

        internal int RequireCount(ulong completionIdentity)
        {
            RequireOpen(completionIdentity);
            return m_Count;
        }

        internal CharacterPoseSourceUsage Require(
            int index,
            ulong completionIdentity)
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
            {
                throw new InvalidOperationException(
                    "Character Pose source usage page is stale.");
            }
        }
    }

    internal readonly struct CharacterPoseSourceUsageView
    {
        internal CharacterPoseSourceUsageView(
            CharacterPoseSourceUsagePage page,
            ulong completionIdentity)
        {
            if (page == null || !page.Matches(completionIdentity))
            {
                throw new ArgumentException(
                    "Character Pose source usage view is invalid.");
            }
            m_Page = page;
            CompletionIdentity = completionIdentity;
        }

        readonly CharacterPoseSourceUsagePage m_Page;
        internal ulong CompletionIdentity { get; }
        internal int Count => m_Page.RequireCount(CompletionIdentity);
        internal bool IsValid =>
            m_Page != null &&
            m_Page.Matches(CompletionIdentity);

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
                PresentationPoseSourceSample> providerSources)
        {
            if (!demand.IsValid ||
                !preparedResources.IsValid ||
                preparedResources.Lineage != demand.Lineage ||
                actionSources == null ||
                providerSources == null ||
                actionSources.Count != demand.ActionSourceCount ||
                providerSources.Count != demand.ProviderSourceCount)
            {
                throw new ArgumentException(
                    "Character Pose source frame does not match its demand.");
            }
            bool pending = false;
            bool invalid = false;
            foreach (AnimationResolvedPoseSourceSample sample in
                     actionSources.Values)
            {
                if (sample?.IsValid != true)
                    invalid = true;
            }
            foreach (PresentationPoseSourceSample sample in
                     providerSources.Values)
            {
                if (sample?.IsValid != true ||
                    sample.FrameSequence !=
                        demand.Lineage.PresentationFrame ||
                    sample.Availability ==
                        PresentationPoseSourceAvailability.Invalid)
                {
                    invalid = true;
                }
                else if (sample.Availability ==
                         PresentationPoseSourceAvailability.Pending)
                {
                    pending = true;
                }
            }
            Demand = demand;
            PreparedResources = preparedResources;
            ActionSources = actionSources;
            ProviderSources = providerSources;
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
                ? PresentationPoseSourceFailureReason.SampleInvalid
                : PresentationPoseSourceFailureReason.None;
        }

        internal CharacterPoseSourceDemand Demand { get; }
        internal CharacterPoseSourcePreparedResources PreparedResources
        {
            get;
        }
        internal CharacterPoseFrameLineage Lineage => Demand.Lineage;
        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            AnimationResolvedPoseSourceSample> ActionSources { get; }
        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            PresentationPoseSourceSample> ProviderSources { get; }
        internal PresentationPoseSourceAvailability Availability { get; }
        internal CharacterPoseSourceFrameOutcome Outcome { get; }
        internal PresentationPoseSourceFailureReason FailureReason { get; }
        internal bool IsValid =>
            Demand.IsValid &&
            PreparedResources.IsValid &&
            PreparedResources.Lineage == Demand.Lineage &&
            ActionSources != null &&
            ProviderSources != null &&
            ActionSources.Count == Demand.ActionSourceCount &&
            ProviderSources.Count == Demand.ProviderSourceCount &&
            (Availability == PresentationPoseSourceAvailability.Ready
                ? Outcome == CharacterPoseSourceFrameOutcome.Prepared &&
                  FailureReason == PresentationPoseSourceFailureReason.None
                : Availability == PresentationPoseSourceAvailability.Pending
                    ? Outcome ==
                      CharacterPoseSourceFrameOutcome.AwaitingSample &&
                      FailureReason == PresentationPoseSourceFailureReason.None
                    : Availability ==
                      PresentationPoseSourceAvailability.Invalid &&
                      Outcome == CharacterPoseSourceFrameOutcome.Invalid &&
                      FailureReason !=
                      PresentationPoseSourceFailureReason.None);
        internal bool IsReady =>
            IsValid &&
            Availability == PresentationPoseSourceAvailability.Ready;
    }

    internal readonly struct CharacterPoseSourceCommittedResult
    {
        internal CharacterPoseSourceCommittedResult(
            in CharacterPoseSourceFrameResult sourceFrame)
        {
            if (!sourceFrame.IsReady)
            {
                throw new ArgumentException(
                    "Character Pose source frame cannot publish a committed result.",
                    nameof(sourceFrame));
            }
            Lineage = sourceFrame.Lineage;
            Availability = sourceFrame.Availability;
            Outcome = sourceFrame.Outcome;
            FailureReason = sourceFrame.FailureReason;
        }

        internal CharacterPoseFrameLineage Lineage { get; }
        internal PresentationPoseSourceAvailability Availability { get; }
        internal CharacterPoseSourceFrameOutcome Outcome { get; }
        internal PresentationPoseSourceFailureReason FailureReason { get; }
        internal bool IsCommitted =>
            Lineage.IsValid &&
            Availability == PresentationPoseSourceAvailability.Ready &&
            Outcome == CharacterPoseSourceFrameOutcome.Prepared &&
            FailureReason == PresentationPoseSourceFailureReason.None;
    }

    internal readonly struct CharacterPoseProgramPrepared
    {
        internal CharacterPoseProgramPrepared(
            in CharacterPoseSourceFrameResult sourceFrame)
        {
            SourceFrame = sourceFrame;
            Lineage = sourceFrame.Lineage;
        }

        internal CharacterPoseSourceFrameResult SourceFrame { get; }
        internal CharacterPoseFrameLineage Lineage { get; }
        internal CharacterPoseSourceFrameOutcome Outcome =>
            SourceFrame.Outcome;
        internal bool IsValid =>
            SourceFrame.IsReady &&
            Outcome == CharacterPoseSourceFrameOutcome.Prepared &&
            SourceFrame.Lineage == Lineage &&
            Lineage.IsValid;
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
            CompletionIdentity == 0 &&
            Outcome == CharacterPoseOperationOutcome.None;
        internal bool IsValid =>
            CompletionIdentity != 0 &&
            Outcome >= CharacterPoseOperationOutcome.Completed &&
            Outcome <= CharacterPoseOperationOutcome.TypedInvalid;
        internal bool Matches(ulong completionIdentity) =>
            IsValid && CompletionIdentity == completionIdentity;
    }

    internal readonly struct CharacterPoseProgramResult
    {
        internal CharacterPoseProgramResult(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationFrameOutcome outcome,
            AnimationPoseAvailability outputAvailability,
            AnimationPoseNativeInvalidReason outputInvalidReason,
            AnimationPoseNativeInvalidReason graphInvalidReason,
            int invalidOperationIndex)
        {
            Lineage = lineage;
            Outcome = outcome;
            OutputAvailability = outputAvailability;
            OutputInvalidReason = outputInvalidReason;
            GraphInvalidReason = graphInvalidReason;
            InvalidOperationIndex = invalidOperationIndex;
            m_IsValid =
                lineage.IsValid &&
                (outcome == AnimationPresentationFrameOutcome.Committed
                    ? outputAvailability == AnimationPoseAvailability.Pose &&
                      outputInvalidReason == AnimationPoseNativeInvalidReason.None &&
                      graphInvalidReason == AnimationPoseNativeInvalidReason.None &&
                      invalidOperationIndex == -1
                    : outcome == AnimationPresentationFrameOutcome.TypedInvalid &&
                      (outputAvailability != AnimationPoseAvailability.Pose ||
                       outputInvalidReason != AnimationPoseNativeInvalidReason.None ||
                       graphInvalidReason != AnimationPoseNativeInvalidReason.None ||
                       invalidOperationIndex >= 0));
        }

        readonly bool m_IsValid;
        internal CharacterPoseFrameLineage Lineage { get; }
        internal AnimationPresentationFrameOutcome Outcome { get; }
        internal AnimationPoseAvailability OutputAvailability { get; }
        internal AnimationPoseNativeInvalidReason OutputInvalidReason { get; }
        internal AnimationPoseNativeInvalidReason GraphInvalidReason { get; }
        internal int InvalidOperationIndex { get; }
        internal bool IsValid => m_IsValid;
        internal bool IsCompleted =>
            IsValid &&
            Outcome == AnimationPresentationFrameOutcome.Committed;
    }

    internal readonly struct CharacterPoseConstraintResult
    {
        internal CharacterPoseConstraintResult(
            in CharacterPoseFrameLineage lineage,
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
        internal CharacterPoseFrameLineage Lineage { get; }
        internal AnimationPresentationFrameOutcome Outcome { get; }
        internal AnimationPoseAvailability Availability { get; }
        internal AnimationPoseNativeInvalidReason InvalidReason { get; }
        internal int GoalCount { get; }
        internal bool SolverProduced { get; }
        internal CharacterFullBodyIkResult FullBodyIk { get; }
        internal bool IsValid => m_IsValid;
        internal bool IsCompleted =>
            IsValid &&
            Outcome == AnimationPresentationFrameOutcome.Committed;
    }

    internal readonly struct CharacterFinalPosePublicationResult
    {
        internal CharacterFinalPosePublicationResult(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationFrameOutcome outcome,
            AnimationFinalPoseWriteOutcome writeOutcome,
            AnimationPoseAvailability availability,
            AnimationPoseNativeInvalidReason outputInvalidReason,
            AnimationPoseNativeInvalidReason graphInvalidReason,
            int invalidOperationIndex,
            ulong appliedCompletionIdentity)
        {
            Lineage = lineage;
            Outcome = outcome;
            WriteOutcome = writeOutcome;
            Availability = availability;
            OutputInvalidReason = outputInvalidReason;
            GraphInvalidReason = graphInvalidReason;
            InvalidOperationIndex = invalidOperationIndex;
            AppliedCompletionIdentity = appliedCompletionIdentity;
            m_IsValid =
                lineage.IsValid &&
                (outcome == AnimationPresentationFrameOutcome.Committed
                    ? writeOutcome == AnimationFinalPoseWriteOutcome.Committed &&
                      availability == AnimationPoseAvailability.Pose &&
                      outputInvalidReason == AnimationPoseNativeInvalidReason.None &&
                      graphInvalidReason == AnimationPoseNativeInvalidReason.None &&
                      invalidOperationIndex == -1 &&
                      appliedCompletionIdentity == lineage.CompletionIdentity
                    : outcome == AnimationPresentationFrameOutcome.TypedInvalid &&
                      writeOutcome == AnimationFinalPoseWriteOutcome.TypedInvalid &&
                      appliedCompletionIdentity == 0 &&
                      (availability != AnimationPoseAvailability.Pose ||
                       outputInvalidReason != AnimationPoseNativeInvalidReason.None ||
                       graphInvalidReason != AnimationPoseNativeInvalidReason.None ||
                       invalidOperationIndex >= 0));
        }

        readonly bool m_IsValid;
        internal CharacterPoseFrameLineage Lineage { get; }
        internal AnimationPresentationFrameOutcome Outcome { get; }
        internal AnimationFinalPoseWriteOutcome WriteOutcome { get; }
        internal AnimationPoseAvailability Availability { get; }
        internal AnimationPoseNativeInvalidReason OutputInvalidReason { get; }
        internal AnimationPoseNativeInvalidReason GraphInvalidReason { get; }
        internal int InvalidOperationIndex { get; }
        internal ulong AppliedCompletionIdentity { get; }
        internal bool IsValid => m_IsValid;
        internal bool IsPublished =>
            IsValid &&
            Outcome == AnimationPresentationFrameOutcome.Committed;
    }

    internal readonly struct CharacterPoseFrameExecutionResult
    {
        internal CharacterPoseFrameExecutionResult(
            in CharacterPoseProgramResult program,
            in CharacterPoseConstraintResult constraint,
            in CharacterFinalPosePublicationResult publication)
        {
            Program = program;
            Constraint = constraint;
            Publication = publication;
            m_IsValid =
                program.IsValid &&
                constraint.IsValid &&
                publication.IsValid &&
                program.Lineage == constraint.Lineage &&
                program.Lineage == publication.Lineage;
        }

        readonly bool m_IsValid;
        internal CharacterPoseProgramResult Program { get; }
        internal CharacterPoseConstraintResult Constraint { get; }
        internal CharacterFinalPosePublicationResult Publication { get; }
        internal CharacterPoseFrameLineage Lineage => Program.Lineage;
        internal bool IsValid => m_IsValid;
        internal bool IsPublished =>
            IsValid &&
            Program.IsCompleted &&
            Constraint.IsCompleted &&
            Publication.IsPublished;
    }
}
