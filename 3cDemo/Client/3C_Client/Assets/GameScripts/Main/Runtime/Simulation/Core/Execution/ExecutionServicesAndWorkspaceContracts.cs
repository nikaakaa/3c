using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation
{
    internal enum ActionAdmissionEvaluationMode : byte
    {
        PreviewReplacement = 1,
        CommitActivation = 2
    }

    internal enum ActionAdmissionRejectReason : byte
    {
        None = 0,
        TargetBlocked = 1,
        ActiveSourceNotCancelable = 2,
        SourceActionStillActive = 3,
        TargetSnapshotRequired = 4
    }

    internal readonly struct ActionAdmissionTargetCandidate
    {
        public ActionAdmissionTargetCandidate(string targetId)
        {
            TargetId = targetId ?? string.Empty;
        }

        public string TargetId { get; }
        public bool HasTarget => !string.IsNullOrEmpty(TargetId);
    }

    internal readonly struct ActionAdmissionRequest
    {
        public ActionAdmissionRequest(
            ActionAdmissionProfile targetProfile,
            ActionAdmissionTargetCandidate targetCandidate,
            ActionAdmissionEvaluationMode mode)
        {
            TargetProfile = targetProfile ?? throw new ArgumentNullException(nameof(targetProfile));
            TargetCandidate = targetCandidate;
            Mode = mode;
        }

        public ActionAdmissionProfile TargetProfile { get; }
        public ActionAdmissionTargetCandidate TargetCandidate { get; }
        public ActionAdmissionEvaluationMode Mode { get; }
    }

    internal readonly struct ActionAdmissionDecision
    {
        public ActionAdmissionDecision(
            bool allowed,
            ActionAdmissionRejectReason rejectReason,
            string activeSourceActionId)
        {
            if (allowed && rejectReason != ActionAdmissionRejectReason.None)
                throw new ArgumentException("Allowed Action admission cannot carry a rejection reason.", nameof(rejectReason));
            if (!allowed && rejectReason == ActionAdmissionRejectReason.None)
                throw new ArgumentException("Rejected Action admission requires a reason.", nameof(rejectReason));
            Allowed = allowed;
            RejectReason = rejectReason;
            ActiveSourceActionId = activeSourceActionId ?? string.Empty;
        }

        public bool Allowed { get; }
        public ActionAdmissionRejectReason RejectReason { get; }
        public string ActiveSourceActionId { get; }
    }

    internal interface IActionAdmissionReadPort
    {
        IEnumerable<string> OwnedGameplayTags { get; }
        bool TryGetActiveAction(out string actionId);
        ActionAdmissionProfile RequireActionProfile(string actionId);
        bool TryGetGameplayTagParent(string tag, out string parentTag);
    }

    internal sealed class ActionTagQuery
    {
        public ActionTagQuery(string[] all, string[] any, string[] none)
        {
            All = all ?? Array.Empty<string>();
            Any = any ?? Array.Empty<string>();
            None = none ?? Array.Empty<string>();
        }

        public string[] All { get; }
        public string[] Any { get; }
        public string[] None { get; }
        public bool IsEmpty => All.Length == 0 && Any.Length == 0 && None.Length == 0;
    }

    internal sealed class ActionAdmissionProfile
    {
        public ActionAdmissionProfile(
            string actionId,
            ActionTargetRequirement targetRequirement,
            string[] tags,
            ActionTagQuery block,
            ActionTagQuery cancel)
        {
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            if (!Enum.IsDefined(typeof(ActionTargetRequirement), targetRequirement))
                throw new ArgumentOutOfRangeException(nameof(targetRequirement));
            TargetRequirement = targetRequirement;
            Tags = tags ?? Array.Empty<string>();
            Block = block ?? throw new ArgumentNullException(nameof(block));
            Cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        }

        public string ActionId { get; }
        public ActionTargetRequirement TargetRequirement { get; }
        public string[] Tags { get; }
        public ActionTagQuery Block { get; }
        public ActionTagQuery Cancel { get; }
    }

    internal static class ActionAdmissionProfileCompiler
    {
        const string ActionPrefix = "action:";

        public static ActionAdmissionProfile Compile(ProgramCatalogEntry entry, Func<int, int> readInt32Constant)
        {
            if (entry == null || entry.Kind != ProgramCatalogEntryKind.Action ||
                !entry.Identity.StartsWith(ActionPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException("Action catalog entry is invalid.", nameof(entry));
            }
            if (readInt32Constant == null)
                throw new ArgumentNullException(nameof(readInt32Constant));
            var tags = new List<string>();
            var blockAll = new List<string>();
            var blockAny = new List<string>();
            var blockNone = new List<string>();
            var cancelAll = new List<string>();
            var cancelAny = new List<string>();
            var cancelNone = new List<string>();
            ActionTargetRequirement targetRequirement = default;
            bool hasTargetRequirement = false;
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (string.Equals(field.Name, "TargetRequirement", StringComparison.Ordinal))
                {
                    if (hasTargetRequirement || field.Kind != ProgramCatalogFieldKind.Constant)
                        throw new InvalidOperationException($"Action catalog '{entry.Identity}' has an invalid TargetRequirement field.");
                    int value = readInt32Constant(field.ConstantIndex);
                    targetRequirement = (ActionTargetRequirement)value;
                    if (!Enum.IsDefined(typeof(ActionTargetRequirement), targetRequirement))
                        throw new InvalidOperationException($"Action catalog '{entry.Identity}' has unknown target requirement '{value}'.");
                    hasTargetRequirement = true;
                    continue;
                }
                if (field.Kind != ProgramCatalogFieldKind.Identity || string.IsNullOrWhiteSpace(field.Identity))
                    continue;
                if (field.Name.StartsWith("Tag:", StringComparison.Ordinal))
                    tags.Add(field.Identity);
                else if (field.Name.StartsWith("Block:All:", StringComparison.Ordinal))
                    blockAll.Add(field.Identity);
                else if (field.Name.StartsWith("Block:Any:", StringComparison.Ordinal))
                    blockAny.Add(field.Identity);
                else if (field.Name.StartsWith("Block:None:", StringComparison.Ordinal))
                    blockNone.Add(field.Identity);
                else if (field.Name.StartsWith("Cancel:All:", StringComparison.Ordinal))
                    cancelAll.Add(field.Identity);
                else if (field.Name.StartsWith("Cancel:Any:", StringComparison.Ordinal))
                    cancelAny.Add(field.Identity);
                else if (field.Name.StartsWith("Cancel:None:", StringComparison.Ordinal))
                    cancelNone.Add(field.Identity);
            }
            if (!hasTargetRequirement)
                throw new InvalidOperationException($"Action catalog '{entry.Identity}' has no TargetRequirement field.");
            return new ActionAdmissionProfile(
                entry.Identity.Substring(ActionPrefix.Length),
                targetRequirement,
                tags.ToArray(),
                new ActionTagQuery(blockAll.ToArray(), blockAny.ToArray(), blockNone.ToArray()),
                new ActionTagQuery(cancelAll.ToArray(), cancelAny.ToArray(), cancelNone.ToArray()));
        }
    }

    internal static class GameplayTagSourceIdentity
    {
        public static string ActionInstance(ulong actionInstanceId)
        {
            if (actionInstanceId == 0)
                throw new ArgumentOutOfRangeException(nameof(actionInstanceId));
            return $"action:{actionInstanceId.ToString(CultureInfo.InvariantCulture)}";
        }
    }

    internal sealed class ActionAdmissionControl
    {
        readonly IActionAdmissionReadPort m_Port;
        readonly HashSet<string> m_OwnedTags = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_ActiveSourceTags = new HashSet<string>(StringComparer.Ordinal);

        public ActionAdmissionControl(IActionAdmissionReadPort port)
        {
            m_Port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public ActionAdmissionDecision Evaluate(ActionAdmissionRequest request)
        {
            m_OwnedTags.Clear();
            m_ActiveSourceTags.Clear();
            try
            {
                if (request.TargetProfile.TargetRequirement == ActionTargetRequirement.SnapshotRequired &&
                    !request.TargetCandidate.HasTarget)
                {
                    return Reject(ActionAdmissionRejectReason.TargetSnapshotRequired, string.Empty);
                }
                foreach (string tag in m_Port.OwnedGameplayTags)
                    AddTag(m_OwnedTags, tag);

                bool hasActiveSource = m_Port.TryGetActiveAction(out string activeSourceActionId);
                if (hasActiveSource)
                {
                    ActionAdmissionProfile activeSourceProfile = m_Port.RequireActionProfile(activeSourceActionId);
                    AddTags(m_ActiveSourceTags, activeSourceProfile.Tags);
                }

                if (!request.TargetProfile.Block.IsEmpty && MatchesQuery(request.TargetProfile.Block, m_OwnedTags))
                    return Reject(ActionAdmissionRejectReason.TargetBlocked, activeSourceActionId);

                if (!hasActiveSource)
                    return new ActionAdmissionDecision(true, ActionAdmissionRejectReason.None, string.Empty);

                if (request.Mode == ActionAdmissionEvaluationMode.CommitActivation)
                    return Reject(ActionAdmissionRejectReason.SourceActionStillActive, activeSourceActionId);

                return !request.TargetProfile.Cancel.IsEmpty &&
                       MatchesQuery(request.TargetProfile.Cancel, m_ActiveSourceTags)
                    ? new ActionAdmissionDecision(true, ActionAdmissionRejectReason.None, activeSourceActionId)
                    : Reject(ActionAdmissionRejectReason.ActiveSourceNotCancelable, activeSourceActionId);
            }
            finally
            {
                m_OwnedTags.Clear();
                m_ActiveSourceTags.Clear();
            }
        }

        ActionAdmissionDecision Reject(ActionAdmissionRejectReason reason, string activeSourceActionId)
        {
            return new ActionAdmissionDecision(false, reason, activeSourceActionId);
        }

        bool MatchesQuery(ActionTagQuery query, HashSet<string> owned)
        {
            for (int i = 0; i < query.All.Length; i++)
                if (!HasMatchingTag(owned, query.All[i]))
                    return false;
            bool matchedAny = query.Any.Length == 0;
            for (int i = 0; i < query.Any.Length; i++)
                matchedAny |= HasMatchingTag(owned, query.Any[i]);
            if (!matchedAny)
                return false;
            for (int i = 0; i < query.None.Length; i++)
                if (HasMatchingTag(owned, query.None[i]))
                    return false;
            return true;
        }

        bool HasMatchingTag(HashSet<string> owned, string query)
        {
            foreach (string candidate in owned)
            {
                string current = candidate;
                for (int depth = 0; depth < 64 && !string.IsNullOrEmpty(current); depth++)
                {
                    if (string.Equals(current, query, StringComparison.Ordinal))
                        return true;
                    if (!m_Port.TryGetGameplayTagParent(current, out string parent))
                        break;
                    if (string.Equals(parent, current, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Gameplay tag '{current}' is its own parent.");
                    current = parent;
                }
            }
            return false;
        }

        static void AddTags(HashSet<string> destination, IReadOnlyList<string> tags)
        {
            for (int i = 0; i < tags.Count; i++)
                AddTag(destination, tags[i]);
        }

        static void AddTag(HashSet<string> destination, string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                destination.Add(tag);
        }
    }

    public readonly struct ProgramLayoutIdentity : IEquatable<ProgramLayoutIdentity>
    {
        public ProgramLayoutIdentity(
            ProgramId programId,
            ProgramHash programHash,
            LayoutHash layoutHash,
            OperationSetVersion operationSetVersion,
            SimulationNumericProfile numericProfile)
        {
            if (!programId.IsValid || !programHash.IsValid || !layoutHash.IsValid ||
                !operationSetVersion.IsValid || !numericProfile.IsValid)
            {
                throw new ArgumentException("Program execution services identity is incomplete.");
            }
            ProgramId = programId;
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            OperationSetVersion = operationSetVersion;
            NumericProfile = numericProfile;
        }

        public ProgramId ProgramId { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public bool IsValid =>
            ProgramId.IsValid &&
            ProgramHash.IsValid &&
            LayoutHash.IsValid &&
            OperationSetVersion.IsValid &&
            NumericProfile.IsValid;

        public bool Equals(ProgramLayoutIdentity other) =>
            ProgramId.Equals(other.ProgramId) &&
            ProgramHash.Equals(other.ProgramHash) &&
            LayoutHash.Equals(other.LayoutHash) &&
            OperationSetVersion.Equals(other.OperationSetVersion) &&
            NumericProfile.Equals(other.NumericProfile);

        public override bool Equals(object obj) => obj is ProgramLayoutIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ProgramId, ProgramHash, LayoutHash, OperationSetVersion, NumericProfile);
        public static bool operator ==(ProgramLayoutIdentity left, ProgramLayoutIdentity right) => left.Equals(right);
        public static bool operator !=(ProgramLayoutIdentity left, ProgramLayoutIdentity right) => !left.Equals(right);

        public void Require(ProgramLayoutIdentity actual)
        {
            if (!Equals(actual))
            {
                throw new InvalidOperationException(
                    $"Program execution services identity mismatch: expected '{ProgramId}/{ProgramHash}/{LayoutHash}/{NumericProfile.Id}', received '{actual.ProgramId}/{actual.ProgramHash}/{actual.LayoutHash}/{actual.NumericProfile.Id}'.");
            }
        }
    }

    internal interface IProgramExecutionServices
    {
        ProgramLayoutIdentity Identity { get; }
        OperationExecutionTopology Topology { get; }
        string SourcePath(OperationHandle operation);
        void RequireIdentity(ProgramLayoutIdentity identity);
    }

    internal enum ExecutionWorkspaceScope : byte
    {
        SessionTransaction = 1,
        ActorEvaluation = 2
    }

    internal enum DirtyPageOwnership : byte
    {
        Empty = 0,
        WorkspaceOwned = 1,
        Published = 2,
        Discarded = 3
    }

    internal readonly struct ExecutionWorkspaceLease
    {
        public ExecutionWorkspaceLease(ExecutionWorkspaceScope scope, ulong generation)
        {
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            Scope = scope;
            Generation = generation;
        }

        public ExecutionWorkspaceScope Scope { get; }
        public ulong Generation { get; }
        public bool IsValid => Generation != 0;
    }

    internal sealed class FrozenExecutionBuffer<T> : IReadOnlyList<T>
    {
        readonly T[] m_Values;

        internal FrozenExecutionBuffer(T[] values)
        {
            m_Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public int Count => m_Values.Length;
        public T this[int index] => m_Values[index];
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)m_Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_Values.GetEnumerator();
    }

    internal sealed class ExecutionWorkspaceBuffer<T> : IReadOnlyList<T>
    {
        readonly List<T> m_Values;

        public ExecutionWorkspaceBuffer(int initialCapacity = 0)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            m_Values = new List<T>(initialCapacity);
        }

        public int Count => m_Values.Count;
        public int Capacity => m_Values.Capacity;
        internal List<T> Values => m_Values;
        public T this[int index]
        {
            get => m_Values[index];
            set => m_Values[index] = value;
        }

        public void Add(T value) => m_Values.Add(value);

        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (m_Values.Capacity < capacity)
                m_Values.Capacity = capacity;
        }

        public void Clear() => m_Values.Clear();

        public IEnumerator<T> GetEnumerator() => m_Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_Values.GetEnumerator();

        public FrozenExecutionBuffer<TFrozen> Freeze<TFrozen>(Func<T, TFrozen> freeze)
        {
            if (freeze == null)
                throw new ArgumentNullException(nameof(freeze));
            var values = new TFrozen[m_Values.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = freeze(m_Values[i]);
            return new FrozenExecutionBuffer<TFrozen>(values);
        }
    }

    internal sealed class NestedExecutionWorkspaceBuffer<T>
    {
        readonly List<List<T>> m_Buffers = new List<List<T>>();
        int m_Depth;

        public List<T> Acquire()
        {
            if (m_Depth == m_Buffers.Count)
                m_Buffers.Add(new List<T>(1));
            List<T> buffer = m_Buffers[m_Depth++];
            buffer.Clear();
            return buffer;
        }

        public void Release(List<T> buffer)
        {
            if (m_Depth == 0 || !ReferenceEquals(m_Buffers[m_Depth - 1], buffer))
                throw new InvalidOperationException("Nested execution workspace buffer release order is invalid.");
            buffer.Clear();
            m_Depth--;
        }

        public void Reset()
        {
            if (m_Depth != 0)
                throw new InvalidOperationException("Nested execution workspace buffer still has an active lease.");
            for (int i = 0; i < m_Buffers.Count; i++)
                m_Buffers[i].Clear();
        }
    }

    internal interface IExecutionWorkspaceScratch
    {
        void Reset();
    }

    internal sealed class SessionExecutionWorkspace<TCompletedStep, TActorResult, TActorState, TEgressScratch>
    {
        readonly object m_Gate = new object();
        bool m_InUse;
        ulong m_Generation;

        public ExecutionWorkspaceBuffer<TCompletedStep> CompletedSteps { get; } =
            new ExecutionWorkspaceBuffer<TCompletedStep>();
        public ExecutionWorkspaceBuffer<TActorResult> ActorResults { get; } =
            new ExecutionWorkspaceBuffer<TActorResult>();
        public ExecutionWorkspaceBuffer<TActorState> ActorStates { get; } =
            new ExecutionWorkspaceBuffer<TActorState>();
        public ExecutionWorkspaceBuffer<TEgressScratch> Egress { get; } =
            new ExecutionWorkspaceBuffer<TEgressScratch>();

        public ExecutionWorkspaceLease BeginTransaction()
        {
            lock (m_Gate)
            {
                if (m_InUse)
                    throw new InvalidOperationException("Session execution workspace is already in use.");
                m_InUse = true;
                m_Generation = checked(m_Generation + 1);
                if (m_Generation == 0)
                    throw new OverflowException("Session execution workspace generation overflowed.");
                Reset();
                return new ExecutionWorkspaceLease(ExecutionWorkspaceScope.SessionTransaction, m_Generation);
            }
        }

        public void Require(ExecutionWorkspaceLease lease)
        {
            if (!m_InUse || lease.Scope != ExecutionWorkspaceScope.SessionTransaction || lease.Generation != m_Generation)
                throw new InvalidOperationException("Session execution workspace lease is stale or belongs to another owner.");
        }

        public void EndTransaction(ExecutionWorkspaceLease lease)
        {
            lock (m_Gate)
            {
                Require(lease);
                Reset();
                m_InUse = false;
            }
        }

        void Reset()
        {
            CompletedSteps.Clear();
            ActorResults.Clear();
            ActorStates.Clear();
            Egress.Clear();
        }
    }

    internal sealed class ActorExecutionWorkspace<
        TFact,
        TPresentation,
        TTrace,
        TTimelineSegment,
        TGameplayEffectScratch,
        TMotionScratch>
        where TGameplayEffectScratch : class, IExecutionWorkspaceScratch
        where TMotionScratch : class, IExecutionWorkspaceScratch
    {
        readonly object m_Gate = new object();
        bool m_InUse;
        ulong m_Generation;

        public ActorExecutionWorkspace(
            TGameplayEffectScratch gameplayEffects,
            TMotionScratch motion)
        {
            GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
        }

        public ExecutionWorkspaceBuffer<TFact> Facts { get; } = new ExecutionWorkspaceBuffer<TFact>();
        public ExecutionWorkspaceBuffer<TPresentation> Presentation { get; } =
            new ExecutionWorkspaceBuffer<TPresentation>();
        public ExecutionWorkspaceBuffer<TTrace> Trace { get; } = new ExecutionWorkspaceBuffer<TTrace>();
        public NestedExecutionWorkspaceBuffer<TTimelineSegment> TimelineSegments { get; } =
            new NestedExecutionWorkspaceBuffer<TTimelineSegment>();
        public TGameplayEffectScratch GameplayEffects { get; }
        public TMotionScratch Motion { get; }

        public ExecutionWorkspaceLease BeginEvaluation()
        {
            lock (m_Gate)
            {
                if (m_InUse)
                    throw new InvalidOperationException("Actor execution workspace is already in use.");
                m_InUse = true;
                m_Generation = checked(m_Generation + 1);
                if (m_Generation == 0)
                    throw new OverflowException("Actor execution workspace generation overflowed.");
                Reset();
                return new ExecutionWorkspaceLease(ExecutionWorkspaceScope.ActorEvaluation, m_Generation);
            }
        }

        public void Require(ExecutionWorkspaceLease lease)
        {
            if (!m_InUse || lease.Scope != ExecutionWorkspaceScope.ActorEvaluation || lease.Generation != m_Generation)
                throw new InvalidOperationException("Actor execution workspace lease is stale or belongs to another owner.");
        }

        public void EndEvaluation(ExecutionWorkspaceLease lease)
        {
            lock (m_Gate)
            {
                Require(lease);
                Reset();
                m_InUse = false;
            }
        }

        void Reset()
        {
            Facts.Clear();
            Presentation.Clear();
            Trace.Clear();
            TimelineSegments.Reset();
            GameplayEffects.Reset();
            Motion.Reset();
        }
    }
}
