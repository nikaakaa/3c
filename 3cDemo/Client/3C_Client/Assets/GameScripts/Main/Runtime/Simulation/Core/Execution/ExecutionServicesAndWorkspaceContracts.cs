using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ThirdPersonSimulation
{
    internal readonly struct SimulationInputRequestState : IEquatable<SimulationInputRequestState>
    {
        public SimulationInputRequestState(
            string requestId,
            ulong sequence,
            ulong sourceTick,
            ulong expireTick,
            int priority,
            bool consumed = false)
        {
            RequestId = requestId ?? string.Empty;
            Sequence = sequence;
            SourceTick = sourceTick;
            ExpireTick = expireTick;
            Priority = priority;
            Consumed = consumed;
        }

        public string RequestId { get; }
        public ulong Sequence { get; }
        public ulong SourceTick { get; }
        public ulong ExpireTick { get; }
        public int Priority { get; }
        public bool Consumed { get; }
        public bool Equals(SimulationInputRequestState other) =>
            string.Equals(RequestId, other.RequestId, StringComparison.Ordinal) &&
            Sequence == other.Sequence &&
            SourceTick == other.SourceTick &&
            ExpireTick == other.ExpireTick &&
            Priority == other.Priority &&
            Consumed == other.Consumed;
        public bool IsValid => !string.IsNullOrEmpty(RequestId) && Sequence != 0;

        public SimulationInputRequestState Consume() =>
            IsValid
                ? new SimulationInputRequestState(RequestId, Sequence, SourceTick, ExpireTick, Priority, true)
                : this;
    }

    internal static class SimulationInputRequestStateCodec
    {
        public static void Write(CanonicalWriter writer, SimulationInputRequestState value)
        {
            writer.WriteBoolean(value.IsValid);
            if (!value.IsValid)
                return;
            writer.WriteString(value.RequestId);
            writer.WriteUInt64(value.Sequence);
            writer.WriteUInt64(value.SourceTick);
            writer.WriteUInt64(value.ExpireTick);
            writer.WriteInt32(value.Priority);
            writer.WriteBoolean(value.Consumed);
        }

        public static SimulationInputRequestState Read(CanonicalReader reader)
        {
            if (!reader.ReadBoolean())
                return default;
            var value = new SimulationInputRequestState(
                reader.ReadString(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadInt32(),
                reader.ReadBoolean());
            if (!value.IsValid)
                throw new InvalidDataException("Simulation input request state identity is invalid.");
            return value;
        }
    }

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
        TargetSnapshotRequired = 4,
        RequiredTagsMissing = 5,
        ActionCapacityExceeded = 6,
        ReplacementSourceMissing = 7
    }

    internal readonly struct ActionAdmissionActiveAction
    {
        public ActionAdmissionActiveAction(string actionId, ulong instanceId, CharacterSkillId skillId)
        {
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            if (instanceId == 0)
                throw new ArgumentOutOfRangeException(nameof(instanceId));
            InstanceId = instanceId;
            SkillId = skillId;
        }

        public string ActionId { get; }
        public ulong InstanceId { get; }
        public CharacterSkillId SkillId { get; }
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
            ActionAdmissionEvaluationMode mode,
            ulong replacementActionInstanceId = 0,
            ulong executingActionInstanceId = 0)
        {
            TargetProfile = targetProfile ?? throw new ArgumentNullException(nameof(targetProfile));
            TargetCandidate = targetCandidate;
            Mode = mode;
            ReplacementActionInstanceId = replacementActionInstanceId;
            ExecutingActionInstanceId = executingActionInstanceId;
        }

        public ActionAdmissionProfile TargetProfile { get; }
        public ActionAdmissionTargetCandidate TargetCandidate { get; }
        public ActionAdmissionEvaluationMode Mode { get; }
        public ulong ReplacementActionInstanceId { get; }
        public ulong ExecutingActionInstanceId { get; }
    }

    internal readonly struct ActionAdmissionDecision
    {
        public ActionAdmissionDecision(
            bool allowed,
            ActionAdmissionRejectReason rejectReason,
            string activeSourceActionId,
            ulong activeSourceActionInstanceId = 0)
        {
            if (allowed && rejectReason != ActionAdmissionRejectReason.None)
                throw new ArgumentException("Allowed Action admission cannot carry a rejection reason.", nameof(rejectReason));
            if (!allowed && rejectReason == ActionAdmissionRejectReason.None)
                throw new ArgumentException("Rejected Action admission requires a reason.", nameof(rejectReason));
            Allowed = allowed;
            RejectReason = rejectReason;
            ActiveSourceActionId = activeSourceActionId ?? string.Empty;
            ActiveSourceActionInstanceId = activeSourceActionInstanceId;
        }

        public bool Allowed { get; }
        public ActionAdmissionRejectReason RejectReason { get; }
        public string ActiveSourceActionId { get; }
        public ulong ActiveSourceActionInstanceId { get; }
    }

    internal interface IActionAdmissionReadPort
    {
        IEnumerable<string> OwnedGameplayTags { get; }
        int ActionCount { get; }
        bool TryReadActiveAction(int index, out ActionAdmissionActiveAction action);
        ActionAdmissionProfile RequireAdmissionProfile(CharacterSkillId skillId, string actionId);
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
            int maxConcurrentInstances,
            string[] tags,
            ActionTagQuery required,
            ActionTagQuery block,
            ActionTagQuery cancel)
        {
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            if (!Enum.IsDefined(typeof(ActionTargetRequirement), targetRequirement))
                throw new ArgumentOutOfRangeException(nameof(targetRequirement));
            if (maxConcurrentInstances <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentInstances));
            TargetRequirement = targetRequirement;
            MaxConcurrentInstances = maxConcurrentInstances;
            Tags = tags ?? Array.Empty<string>();
            Required = required ?? throw new ArgumentNullException(nameof(required));
            Block = block ?? throw new ArgumentNullException(nameof(block));
            Cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        }

        public string ActionId { get; }
        public ActionTargetRequirement TargetRequirement { get; }
        public int MaxConcurrentInstances { get; }
        public string[] Tags { get; }
        public ActionTagQuery Required { get; }
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
            var requiredAll = new List<string>();
            var requiredAny = new List<string>();
            var requiredNone = new List<string>();
            var blockAll = new List<string>();
            var blockAny = new List<string>();
            var blockNone = new List<string>();
            var cancelAll = new List<string>();
            var cancelAny = new List<string>();
            var cancelNone = new List<string>();
            ActionTargetRequirement targetRequirement = default;
            bool hasTargetRequirement = false;
            int maxConcurrentInstances = 0;
            bool hasMaxConcurrentInstances = false;
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
                if (string.Equals(field.Name, "MaxConcurrentInstances", StringComparison.Ordinal))
                {
                    if (hasMaxConcurrentInstances || field.Kind != ProgramCatalogFieldKind.Constant)
                        throw new InvalidOperationException($"Action catalog '{entry.Identity}' has an invalid MaxConcurrentInstances field.");
                    maxConcurrentInstances = readInt32Constant(field.ConstantIndex);
                    if (maxConcurrentInstances <= 0)
                        throw new InvalidOperationException($"Action catalog '{entry.Identity}' has non-positive MaxConcurrentInstances '{maxConcurrentInstances}'.");
                    hasMaxConcurrentInstances = true;
                    continue;
                }
                if (field.Kind != ProgramCatalogFieldKind.Identity || string.IsNullOrWhiteSpace(field.Identity))
                    continue;
                if (field.Name.StartsWith("Tag:", StringComparison.Ordinal))
                    tags.Add(field.Identity);
                else if (field.Name.StartsWith("Required:All:", StringComparison.Ordinal))
                    requiredAll.Add(field.Identity);
                else if (field.Name.StartsWith("Required:Any:", StringComparison.Ordinal))
                    requiredAny.Add(field.Identity);
                else if (field.Name.StartsWith("Required:None:", StringComparison.Ordinal))
                    requiredNone.Add(field.Identity);
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
            if (!hasMaxConcurrentInstances)
                throw new InvalidOperationException($"Action catalog '{entry.Identity}' has no MaxConcurrentInstances field.");
            return new ActionAdmissionProfile(
                entry.Identity.Substring(ActionPrefix.Length),
                targetRequirement,
                maxConcurrentInstances,
                tags.ToArray(),
                new ActionTagQuery(requiredAll.ToArray(), requiredAny.ToArray(), requiredNone.ToArray()),
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
            return Format("action:", actionInstanceId, CultureInfo.InvariantCulture);
        }

        public static string EffectHandle(ulong effectHandle) =>
            Format("effect:", effectHandle, CultureInfo.CurrentCulture);

        static string Format(string prefix, ulong value, IFormatProvider provider)
        {
            Span<char> characters = stackalloc char[prefix.Length + 20];
            prefix.AsSpan().CopyTo(characters);
            value.TryFormat(characters.Slice(prefix.Length), out int written, provider: provider);
            return new string(characters.Slice(0, prefix.Length + written));
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
                foreach (string tag in m_Port.OwnedGameplayTags)
                    AddTag(m_OwnedTags, tag);

                if (!request.TargetProfile.Required.IsEmpty && !MatchesQuery(request.TargetProfile.Required, m_OwnedTags))
                    return Reject(ActionAdmissionRejectReason.RequiredTagsMissing, string.Empty, 0);

                if (!request.TargetProfile.Block.IsEmpty && MatchesQuery(request.TargetProfile.Block, m_OwnedTags))
                    return Reject(ActionAdmissionRejectReason.TargetBlocked, string.Empty, 0);

                if (request.TargetProfile.TargetRequirement == ActionTargetRequirement.SnapshotRequired &&
                    !request.TargetCandidate.HasTarget)
                {
                    return Reject(ActionAdmissionRejectReason.TargetSnapshotRequired, string.Empty, 0);
                }

                int activeTargetCount = 0;
                ulong activeTargetInstanceId = 0;
                ActionAdmissionActiveAction replacementSource = default;
                bool hasReplacementSource = false;
                for (int index = 0; index < m_Port.ActionCount; index++)
                {
                    if (!m_Port.TryReadActiveAction(index, out ActionAdmissionActiveAction active))
                        continue;
                    if (string.Equals(active.ActionId, request.TargetProfile.ActionId, StringComparison.Ordinal))
                    {
                        activeTargetCount++;
                        activeTargetInstanceId = active.InstanceId;
                    }
                    if (request.ReplacementActionInstanceId == active.InstanceId)
                    {
                        replacementSource = active;
                        hasReplacementSource = true;
                    }
                }

                if (request.ReplacementActionInstanceId != 0)
                {
                    if (!hasReplacementSource)
                        return Reject(ActionAdmissionRejectReason.ReplacementSourceMissing, string.Empty, 0);

                    ActionAdmissionProfile activeSourceProfile = m_Port.RequireAdmissionProfile(replacementSource.SkillId, replacementSource.ActionId);
                    AddTags(m_ActiveSourceTags, activeSourceProfile.Tags);
                    if (request.Mode == ActionAdmissionEvaluationMode.CommitActivation)
                        return Reject(ActionAdmissionRejectReason.SourceActionStillActive, replacementSource.ActionId, replacementSource.InstanceId);
                    return !request.TargetProfile.Cancel.IsEmpty &&
                           MatchesQuery(request.TargetProfile.Cancel, m_ActiveSourceTags)
                        ? new ActionAdmissionDecision(true, ActionAdmissionRejectReason.None, replacementSource.ActionId, replacementSource.InstanceId)
                        : Reject(ActionAdmissionRejectReason.ActiveSourceNotCancelable, replacementSource.ActionId, replacementSource.InstanceId);
                }

                if (activeTargetCount >= request.TargetProfile.MaxConcurrentInstances)
                {
                    if (request.Mode == ActionAdmissionEvaluationMode.PreviewReplacement &&
                        activeTargetCount == 1 &&
                        request.ExecutingActionInstanceId != 0 &&
                        activeTargetInstanceId == request.ExecutingActionInstanceId)
                        return new ActionAdmissionDecision(true, ActionAdmissionRejectReason.None, string.Empty);
                    return Reject(ActionAdmissionRejectReason.ActionCapacityExceeded, request.TargetProfile.ActionId, 0);
                }
                return new ActionAdmissionDecision(true, ActionAdmissionRejectReason.None, string.Empty);
            }
            finally
            {
                m_OwnedTags.Clear();
                m_ActiveSourceTags.Clear();
            }
        }

        ActionAdmissionDecision Reject(
            ActionAdmissionRejectReason reason,
            string activeSourceActionId,
            ulong activeSourceActionInstanceId)
        {
            return new ActionAdmissionDecision(false, reason, activeSourceActionId, activeSourceActionInstanceId);
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

    public readonly struct GameplayAbilityExecutionIdentity : IEquatable<GameplayAbilityExecutionIdentity>
    {
        public GameplayAbilityExecutionIdentity(
            CharacterSkillId abilityId,
            StableHash contentHash,
            StableHash stateSchemaHash,
            OperationSetVersion operationSetVersion,
            SimulationNumericProfile numericProfile)
        {
            if (!abilityId.IsValid || !contentHash.IsValid || !stateSchemaHash.IsValid ||
                !operationSetVersion.IsValid || !numericProfile.IsValid)
            {
                throw new ArgumentException("Ability execution services identity is incomplete.");
            }
            AbilityId = abilityId;
            ContentHash = contentHash;
            StateSchemaHash = stateSchemaHash;
            OperationSetVersion = operationSetVersion;
            NumericProfile = numericProfile;
        }

        public CharacterSkillId AbilityId { get; }
        public StableHash ContentHash { get; }
        public StableHash StateSchemaHash { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public bool IsValid =>
            AbilityId.IsValid &&
            ContentHash.IsValid &&
            StateSchemaHash.IsValid &&
            OperationSetVersion.IsValid &&
            NumericProfile.IsValid;

        public bool Equals(GameplayAbilityExecutionIdentity other) =>
            AbilityId.Equals(other.AbilityId) &&
            ContentHash.Equals(other.ContentHash) &&
            StateSchemaHash.Equals(other.StateSchemaHash) &&
            OperationSetVersion.Equals(other.OperationSetVersion) &&
            NumericProfile.Equals(other.NumericProfile);

        public override bool Equals(object obj) => obj is GameplayAbilityExecutionIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(AbilityId, ContentHash, StateSchemaHash, OperationSetVersion, NumericProfile);
        public static bool operator ==(GameplayAbilityExecutionIdentity left, GameplayAbilityExecutionIdentity right) => left.Equals(right);
        public static bool operator !=(GameplayAbilityExecutionIdentity left, GameplayAbilityExecutionIdentity right) => !left.Equals(right);

        public void Require(GameplayAbilityExecutionIdentity actual)
        {
            if (!Equals(actual))
            {
                throw new InvalidOperationException(
                    $"Ability execution services identity mismatch: expected '{AbilityId}/{ContentHash}/{StateSchemaHash}/{NumericProfile.Id}', received '{actual.AbilityId}/{actual.ContentHash}/{actual.StateSchemaHash}/{actual.NumericProfile.Id}'.");
            }
        }
    }

    internal interface IGameplayAbilityExecutionServices
    {
        GameplayAbilityExecutionIdentity Identity { get; }
        OperationExecutionTopology Topology { get; }
        string SourcePath(OperationHandle operation);
        void RequireIdentity(GameplayAbilityExecutionIdentity identity);
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

    public sealed class NestedExecutionWorkspaceBuffer<T>
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

    internal sealed class SessionExecutionWorkspace<TCompletedStep, TEgressScratch>
    {
        readonly object m_Gate = new object();
        bool m_InUse;
        ulong m_Generation;

        public ExecutionWorkspaceBuffer<TCompletedStep> CompletedSteps { get; } =
            new ExecutionWorkspaceBuffer<TCompletedStep>();
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
            Egress.Clear();
        }
    }

}
