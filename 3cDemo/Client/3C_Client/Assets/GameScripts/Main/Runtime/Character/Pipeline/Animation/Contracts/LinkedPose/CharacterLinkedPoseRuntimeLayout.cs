using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterLinkedPoseLayoutRange : IEquatable<CharacterLinkedPoseLayoutRange>
    {
        public CharacterLinkedPoseLayoutRange(int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            Offset = offset;
            Count = count;
            _ = checked(offset + count);
        }

        public int Offset { get; }
        public int Count { get; }
        public int End => checked(Offset + Count);

        public void RequireWithin(int capacity, string name)
        {
            if (capacity < 0 || Offset < 0 || Count < 0 || End > capacity)
                throw new InvalidOperationException($"Linked Pose {name} range [{Offset}, {End}) exceeds capacity {capacity}.");
        }

        public bool Equals(CharacterLinkedPoseLayoutRange other) => Offset == other.Offset && Count == other.Count;
        public override bool Equals(object obj) => obj is CharacterLinkedPoseLayoutRange other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Offset, Count);
        public static bool operator ==(CharacterLinkedPoseLayoutRange left, CharacterLinkedPoseLayoutRange right) => left.Equals(right);
        public static bool operator !=(CharacterLinkedPoseLayoutRange left, CharacterLinkedPoseLayoutRange right) => !left.Equals(right);
    }

    public readonly struct CharacterLinkedPoseRuntimeCapacity : IEquatable<CharacterLinkedPoseRuntimeCapacity>
    {
        public CharacterLinkedPoseRuntimeCapacity(
            int operationCount,
            int poseValueCount,
            int goalSetValueCount,
            int playerCount,
            int stateMachineCount,
            int inertializationCount,
            int rootOrientationWarpCount,
            int motionMatchingProviderCount,
            int sourceDemandCount,
            int frameCompletionCount,
            int playerCompletionCount,
            int stageCompletionCount,
            int operationDiagnosticCount,
            int stageDiagnosticCount)
        {
            OperationCount = RequireCount(operationCount, nameof(operationCount));
            PoseValueCount = RequireCount(poseValueCount, nameof(poseValueCount));
            GoalSetValueCount = RequireCount(goalSetValueCount, nameof(goalSetValueCount));
            PlayerCount = RequireCount(playerCount, nameof(playerCount));
            StateMachineCount = RequireCount(stateMachineCount, nameof(stateMachineCount));
            InertializationCount = RequireCount(inertializationCount, nameof(inertializationCount));
            RootOrientationWarpCount = RequireCount(rootOrientationWarpCount, nameof(rootOrientationWarpCount));
            MotionMatchingProviderCount = RequireCount(motionMatchingProviderCount, nameof(motionMatchingProviderCount));
            SourceDemandCount = RequireCount(sourceDemandCount, nameof(sourceDemandCount));
            FrameCompletionCount = RequireCount(frameCompletionCount, nameof(frameCompletionCount));
            PlayerCompletionCount = RequireCount(playerCompletionCount, nameof(playerCompletionCount));
            StageCompletionCount = RequireCount(stageCompletionCount, nameof(stageCompletionCount));
            OperationDiagnosticCount = RequireCount(operationDiagnosticCount, nameof(operationDiagnosticCount));
            StageDiagnosticCount = RequireCount(stageDiagnosticCount, nameof(stageDiagnosticCount));
        }

        public int OperationCount { get; }
        public int PoseValueCount { get; }
        public int GoalSetValueCount { get; }
        public int PlayerCount { get; }
        public int StateMachineCount { get; }
        public int InertializationCount { get; }
        public int RootOrientationWarpCount { get; }
        public int MotionMatchingProviderCount { get; }
        public int SourceDemandCount { get; }
        public int FrameCompletionCount { get; }
        public int PlayerCompletionCount { get; }
        public int StageCompletionCount { get; }
        public int OperationDiagnosticCount { get; }
        public int StageDiagnosticCount { get; }
        public int PlayerDiagnosticCount => PlayerCount;
        public int StateMachineDiagnosticCount => StateMachineCount;
        public int InertializationDiagnosticCount => InertializationCount;
        public int RootOrientationWarpDiagnosticCount => RootOrientationWarpCount;

        public static CharacterLinkedPoseRuntimeCapacity Max(
            in CharacterLinkedPoseRuntimeCapacity left,
            in CharacterLinkedPoseRuntimeCapacity right)
        {
            return new CharacterLinkedPoseRuntimeCapacity(
                Math.Max(left.OperationCount, right.OperationCount),
                Math.Max(left.PoseValueCount, right.PoseValueCount),
                Math.Max(left.GoalSetValueCount, right.GoalSetValueCount),
                Math.Max(left.PlayerCount, right.PlayerCount),
                Math.Max(left.StateMachineCount, right.StateMachineCount),
                Math.Max(left.InertializationCount, right.InertializationCount),
                Math.Max(left.RootOrientationWarpCount, right.RootOrientationWarpCount),
                Math.Max(left.MotionMatchingProviderCount, right.MotionMatchingProviderCount),
                Math.Max(left.SourceDemandCount, right.SourceDemandCount),
                Math.Max(left.FrameCompletionCount, right.FrameCompletionCount),
                Math.Max(left.PlayerCompletionCount, right.PlayerCompletionCount),
                Math.Max(left.StageCompletionCount, right.StageCompletionCount),
                Math.Max(left.OperationDiagnosticCount, right.OperationDiagnosticCount),
                Math.Max(left.StageDiagnosticCount, right.StageDiagnosticCount));
        }

        public bool Equals(CharacterLinkedPoseRuntimeCapacity other) =>
            OperationCount == other.OperationCount &&
            PoseValueCount == other.PoseValueCount &&
            GoalSetValueCount == other.GoalSetValueCount &&
            PlayerCount == other.PlayerCount &&
            StateMachineCount == other.StateMachineCount &&
            InertializationCount == other.InertializationCount &&
            RootOrientationWarpCount == other.RootOrientationWarpCount &&
            MotionMatchingProviderCount == other.MotionMatchingProviderCount &&
            SourceDemandCount == other.SourceDemandCount &&
            FrameCompletionCount == other.FrameCompletionCount &&
            PlayerCompletionCount == other.PlayerCompletionCount &&
            StageCompletionCount == other.StageCompletionCount &&
            OperationDiagnosticCount == other.OperationDiagnosticCount &&
            StageDiagnosticCount == other.StageDiagnosticCount;
        public override bool Equals(object obj) => obj is CharacterLinkedPoseRuntimeCapacity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            HashCode.Combine(
                OperationCount,
                PoseValueCount,
                GoalSetValueCount,
                PlayerCount,
                StateMachineCount,
                InertializationCount,
                RootOrientationWarpCount),
            HashCode.Combine(
                MotionMatchingProviderCount,
                SourceDemandCount,
                FrameCompletionCount,
                PlayerCompletionCount,
                StageCompletionCount,
                OperationDiagnosticCount,
                StageDiagnosticCount));
        public static bool operator ==(CharacterLinkedPoseRuntimeCapacity left, CharacterLinkedPoseRuntimeCapacity right) => left.Equals(right);
        public static bool operator !=(CharacterLinkedPoseRuntimeCapacity left, CharacterLinkedPoseRuntimeCapacity right) => !left.Equals(right);

        static int RequireCount(int value, string name)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public sealed class CharacterLinkedPoseEntryRuntimeLayout
    {
        public CharacterLinkedPoseEntryRuntimeLayout(
            LinkedPoseEntryId entryId,
            int fragmentIndex,
            CharacterLinkedPoseLayoutRange operations,
            CharacterLinkedPoseLayoutRange poseValues,
            CharacterLinkedPoseLayoutRange goalSetValues,
            CharacterLinkedPoseLayoutRange players,
            CharacterLinkedPoseLayoutRange stateMachines,
            CharacterLinkedPoseLayoutRange inertializations,
            CharacterLinkedPoseLayoutRange rootOrientationWarps,
            CharacterLinkedPoseLayoutRange motionMatchingProviders,
            CharacterLinkedPoseLayoutRange sourceDemands,
            CharacterLinkedPoseLayoutRange frameCompletions,
            CharacterLinkedPoseLayoutRange playerCompletions,
            CharacterLinkedPoseLayoutRange stageCompletions,
            CharacterLinkedPoseLayoutRange operationDiagnostics,
            CharacterLinkedPoseLayoutRange stageDiagnostics)
        {
            EntryId = entryId.IsValid ? entryId : throw new ArgumentException("Linked Pose Entry identity is invalid.", nameof(entryId));
            FragmentIndex = fragmentIndex >= 0 ? fragmentIndex : throw new ArgumentOutOfRangeException(nameof(fragmentIndex));
            Operations = operations;
            PoseValues = poseValues;
            GoalSetValues = goalSetValues;
            Players = players;
            StateMachines = stateMachines;
            Inertializations = inertializations;
            RootOrientationWarps = rootOrientationWarps;
            MotionMatchingProviders = motionMatchingProviders;
            SourceDemands = sourceDemands;
            FrameCompletions = frameCompletions;
            PlayerCompletions = playerCompletions;
            StageCompletions = stageCompletions;
            OperationDiagnostics = operationDiagnostics;
            StageDiagnostics = stageDiagnostics;
        }

        public LinkedPoseEntryId EntryId { get; }
        public int FragmentIndex { get; }
        public CharacterLinkedPoseLayoutRange Operations { get; }
        public CharacterLinkedPoseLayoutRange PoseValues { get; }
        public CharacterLinkedPoseLayoutRange GoalSetValues { get; }
        public CharacterLinkedPoseLayoutRange Players { get; }
        public CharacterLinkedPoseLayoutRange StateMachines { get; }
        public CharacterLinkedPoseLayoutRange Inertializations { get; }
        public CharacterLinkedPoseLayoutRange RootOrientationWarps { get; }
        public CharacterLinkedPoseLayoutRange MotionMatchingProviders { get; }
        public CharacterLinkedPoseLayoutRange SourceDemands { get; }
        public CharacterLinkedPoseLayoutRange FrameCompletions { get; }
        public CharacterLinkedPoseLayoutRange PlayerCompletions { get; }
        public CharacterLinkedPoseLayoutRange StageCompletions { get; }
        public CharacterLinkedPoseLayoutRange OperationDiagnostics { get; }
        public CharacterLinkedPoseLayoutRange StageDiagnostics { get; }
        public CharacterLinkedPoseLayoutRange PlayerDiagnostics => Players;
        public CharacterLinkedPoseLayoutRange StateMachineDiagnostics => StateMachines;
        public CharacterLinkedPoseLayoutRange InertializationDiagnostics => Inertializations;
        public CharacterLinkedPoseLayoutRange RootOrientationWarpDiagnostics => RootOrientationWarps;

        internal void RequireWithin(in CharacterLinkedPoseRuntimeCapacity capacity)
        {
            Operations.RequireWithin(capacity.OperationCount, "operation");
            PoseValues.RequireWithin(capacity.PoseValueCount, "Pose value");
            GoalSetValues.RequireWithin(capacity.GoalSetValueCount, "GoalSet value");
            Players.RequireWithin(capacity.PlayerCount, "player");
            StateMachines.RequireWithin(capacity.StateMachineCount, "StateMachine");
            Inertializations.RequireWithin(capacity.InertializationCount, "inertialization");
            RootOrientationWarps.RequireWithin(capacity.RootOrientationWarpCount, "Root Orientation Warp");
            MotionMatchingProviders.RequireWithin(capacity.MotionMatchingProviderCount, "Motion Matching provider");
            SourceDemands.RequireWithin(capacity.SourceDemandCount, "source demand");
            FrameCompletions.RequireWithin(capacity.FrameCompletionCount, "frame completion");
            PlayerCompletions.RequireWithin(capacity.PlayerCompletionCount, "player completion");
            StageCompletions.RequireWithin(capacity.StageCompletionCount, "stage completion");
            OperationDiagnostics.RequireWithin(capacity.OperationDiagnosticCount, "operation diagnostics");
            StageDiagnostics.RequireWithin(capacity.StageDiagnosticCount, "stage diagnostics");
        }
    }

    public sealed class CharacterLinkedPoseImplementationRuntimeLayout
    {
        readonly CharacterLinkedPoseEntryRuntimeLayout[] m_Entries;

        public CharacterLinkedPoseImplementationRuntimeLayout(
            LinkedPoseImplementationId implementationId,
            in CharacterLinkedPoseRuntimeCapacity capacity,
            CharacterLinkedPoseEntryRuntimeLayout[] entries)
        {
            ImplementationId = implementationId.IsValid
                ? implementationId
                : throw new ArgumentException("Linked Pose Implementation identity is invalid.", nameof(implementationId));
            Capacity = capacity;
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            m_Entries = new CharacterLinkedPoseEntryRuntimeLayout[entries.Length];
            Array.Copy(entries, m_Entries, entries.Length);
            RequireValid();
        }

        public LinkedPoseImplementationId ImplementationId { get; }
        public CharacterLinkedPoseRuntimeCapacity Capacity { get; }
        public IReadOnlyList<CharacterLinkedPoseEntryRuntimeLayout> Entries => m_Entries;

        public CharacterLinkedPoseEntryRuntimeLayout RequireEntry(LinkedPoseEntryId entryId)
        {
            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i].EntryId == entryId)
                    return m_Entries[i];
            }
            throw new InvalidOperationException($"Linked Pose Implementation '{ImplementationId}' has no runtime layout for Entry '{entryId}'.");
        }

        public void RequireValid()
        {
            if (!ImplementationId.IsValid || m_Entries == null || m_Entries.Length == 0)
                throw new InvalidOperationException("Linked Pose Implementation runtime layout is incomplete.");
            var ids = new HashSet<LinkedPoseEntryId>();
            CharacterLinkedPoseRuntimeCapacity capacity = Capacity;
            for (int i = 0; i < m_Entries.Length; i++)
            {
                CharacterLinkedPoseEntryRuntimeLayout entry = m_Entries[i];
                if (entry == null || !ids.Add(entry.EntryId))
                    throw new InvalidOperationException($"Linked Pose Implementation '{ImplementationId}' Entry #{i} is missing or duplicated.");
                entry.RequireWithin(in capacity);
            }
            RequireNonOverlapping("operation", value => value.Operations);
            RequireNonOverlapping("Pose value", value => value.PoseValues);
            RequireNonOverlapping("GoalSet value", value => value.GoalSetValues);
            RequireNonOverlapping("player", value => value.Players);
            RequireNonOverlapping("StateMachine", value => value.StateMachines);
            RequireNonOverlapping("inertialization", value => value.Inertializations);
            RequireNonOverlapping("Root Orientation Warp", value => value.RootOrientationWarps);
            RequireNonOverlapping("Motion Matching provider", value => value.MotionMatchingProviders);
            RequireNonOverlapping("source demand", value => value.SourceDemands);
            RequireNonOverlapping("frame completion", value => value.FrameCompletions);
            RequireNonOverlapping("player completion", value => value.PlayerCompletions);
            RequireNonOverlapping("stage completion", value => value.StageCompletions);
            RequireNonOverlapping("operation diagnostics", value => value.OperationDiagnostics);
            RequireNonOverlapping("stage diagnostics", value => value.StageDiagnostics);
            RequirePacked("operation", capacity.OperationCount, value => value.Operations);
            RequirePacked("Pose value", capacity.PoseValueCount, value => value.PoseValues);
            RequirePacked("GoalSet value", capacity.GoalSetValueCount, value => value.GoalSetValues);
            RequirePacked("player", capacity.PlayerCount, value => value.Players);
            RequirePacked("StateMachine", capacity.StateMachineCount, value => value.StateMachines);
            RequirePacked("inertialization", capacity.InertializationCount, value => value.Inertializations);
            RequirePacked("Root Orientation Warp", capacity.RootOrientationWarpCount, value => value.RootOrientationWarps);
            RequirePacked("Motion Matching provider", capacity.MotionMatchingProviderCount, value => value.MotionMatchingProviders);
            RequirePacked("source demand", capacity.SourceDemandCount, value => value.SourceDemands);
            RequirePacked("frame completion", capacity.FrameCompletionCount, value => value.FrameCompletions);
            RequirePacked("player completion", capacity.PlayerCompletionCount, value => value.PlayerCompletions);
            RequirePacked("stage completion", capacity.StageCompletionCount, value => value.StageCompletions);
            RequirePacked("operation diagnostics", capacity.OperationDiagnosticCount, value => value.OperationDiagnostics);
            RequirePacked("stage diagnostics", capacity.StageDiagnosticCount, value => value.StageDiagnostics);
        }

        void RequirePacked(string name, int capacity, Func<CharacterLinkedPoseEntryRuntimeLayout, CharacterLinkedPoseLayoutRange> select)
        {
            int count = 0;
            for (int i = 0; i < m_Entries.Length; i++)
                count = checked(count + select(m_Entries[i]).Count);
            if (count != capacity)
                throw new InvalidOperationException($"Linked Pose Implementation '{ImplementationId}' {name} capacity is not the sum of its Entries.");
        }

        void RequireNonOverlapping(string name, Func<CharacterLinkedPoseEntryRuntimeLayout, CharacterLinkedPoseLayoutRange> select)
        {
            for (int leftIndex = 0; leftIndex < m_Entries.Length; leftIndex++)
            {
                CharacterLinkedPoseLayoutRange left = select(m_Entries[leftIndex]);
                if (left.Count == 0)
                    continue;
                for (int rightIndex = leftIndex + 1; rightIndex < m_Entries.Length; rightIndex++)
                {
                    CharacterLinkedPoseLayoutRange right = select(m_Entries[rightIndex]);
                    if (right.Count != 0 && left.Offset < right.End && right.Offset < left.End)
                    {
                        throw new InvalidOperationException(
                            $"Linked Pose Implementation '{ImplementationId}' Entries '{m_Entries[leftIndex].EntryId}' and '{m_Entries[rightIndex].EntryId}' overlap in {name} layout.");
                    }
                }
            }
        }
    }

    public sealed class CharacterLinkedPoseGroupRuntimeLayout
    {
        readonly CharacterLinkedPoseImplementationRuntimeLayout[] m_Implementations;

        public CharacterLinkedPoseGroupRuntimeLayout(
            LinkedPoseGroupId groupId,
            in CharacterLinkedPoseRuntimeCapacity maximumCapacity,
            CharacterLinkedPoseImplementationRuntimeLayout[] implementations)
        {
            GroupId = groupId.IsValid ? groupId : throw new ArgumentException("Linked Pose Group identity is invalid.", nameof(groupId));
            MaximumCapacity = maximumCapacity;
            if (implementations == null)
                throw new ArgumentNullException(nameof(implementations));
            m_Implementations = new CharacterLinkedPoseImplementationRuntimeLayout[implementations.Length];
            Array.Copy(implementations, m_Implementations, implementations.Length);
            RequireValid();
        }

        public LinkedPoseGroupId GroupId { get; }
        public CharacterLinkedPoseRuntimeCapacity MaximumCapacity { get; }
        public IReadOnlyList<CharacterLinkedPoseImplementationRuntimeLayout> Implementations => m_Implementations;

        public CharacterLinkedPoseImplementationRuntimeLayout RequireImplementation(LinkedPoseImplementationId implementationId)
        {
            for (int i = 0; i < m_Implementations.Length; i++)
            {
                if (m_Implementations[i].ImplementationId == implementationId)
                    return m_Implementations[i];
            }
            throw new InvalidOperationException($"Linked Pose Group '{GroupId}' has no runtime layout for Implementation '{implementationId}'.");
        }

        public void RequireValid()
        {
            if (!GroupId.IsValid || m_Implementations == null || m_Implementations.Length == 0)
                throw new InvalidOperationException("Linked Pose Group runtime layout is incomplete.");
            var ids = new HashSet<LinkedPoseImplementationId>();
            CharacterLinkedPoseRuntimeCapacity derivedMaximum = default;
            for (int i = 0; i < m_Implementations.Length; i++)
            {
                CharacterLinkedPoseImplementationRuntimeLayout implementation = m_Implementations[i];
                implementation?.RequireValid();
                if (implementation == null || !ids.Add(implementation.ImplementationId))
                    throw new InvalidOperationException($"Linked Pose Group '{GroupId}' Implementation #{i} is missing or duplicated.");
                CharacterLinkedPoseRuntimeCapacity capacity = implementation.Capacity;
                derivedMaximum = CharacterLinkedPoseRuntimeCapacity.Max(in derivedMaximum, in capacity);
            }
            if (derivedMaximum != MaximumCapacity)
                throw new InvalidOperationException($"Linked Pose Group '{GroupId}' maximum runtime capacity is stale.");
        }
    }

    public sealed class CharacterLinkedPoseRuntimeLayoutCatalog
    {
        readonly CharacterLinkedPoseGroupRuntimeLayout[] m_Groups;

        public CharacterLinkedPoseRuntimeLayoutCatalog(CharacterLinkedPoseGroupRuntimeLayout[] groups)
        {
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));
            m_Groups = new CharacterLinkedPoseGroupRuntimeLayout[groups.Length];
            Array.Copy(groups, m_Groups, groups.Length);
            var ids = new HashSet<LinkedPoseGroupId>();
            for (int i = 0; i < m_Groups.Length; i++)
            {
                CharacterLinkedPoseGroupRuntimeLayout group = m_Groups[i];
                group?.RequireValid();
                if (group == null || !ids.Add(group.GroupId))
                    throw new InvalidOperationException($"Linked Pose runtime layout Group #{i} is missing or duplicated.");
            }
        }

        public int Count => m_Groups.Length;

        public CharacterLinkedPoseGroupRuntimeLayout RequireGroup(LinkedPoseGroupId groupId)
        {
            for (int i = 0; i < m_Groups.Length; i++)
            {
                if (m_Groups[i].GroupId == groupId)
                    return m_Groups[i];
            }
            throw new InvalidOperationException($"Linked Pose Group '{groupId}' has no runtime layout.");
        }
    }

    public readonly struct CharacterLinkedPoseEntryGenerationHandle
    {
        public CharacterLinkedPoseEntryGenerationHandle(
            in CharacterLinkedPoseGenerationHandle generation,
            CharacterLinkedPoseEntryRuntimeLayout layout)
        {
            if (!generation.IsValid || layout == null)
                throw new ArgumentException("Linked Pose Entry generation handle is incomplete.");
            Generation = generation;
            Layout = layout;
        }

        public CharacterLinkedPoseGenerationHandle Generation { get; }
        public CharacterLinkedPoseEntryRuntimeLayout Layout { get; }
        public LinkedPoseEntryId EntryId => Layout?.EntryId ?? default;
        public int FragmentIndex => Layout?.FragmentIndex ?? -1;
        public bool IsValid => Generation.IsValid && Layout != null && EntryId.IsValid && FragmentIndex >= 0;
    }
}
