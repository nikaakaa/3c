using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal enum GameplayEffectDurationKind : byte
    {
        Instant = 1,
        Duration = 2,
        Infinite = 3
    }

    internal enum GameplayEffectStackingKind : byte
    {
        None = 0,
        BySource = 1,
        ByTarget = 2
    }

    internal enum GameplayEffectDurationUpdateKind : byte
    {
        Keep = 0,
        Refresh = 1,
        Extend = 2
    }

    internal enum GameplayEffectPeriodUpdateKind : byte
    {
        Keep = 0,
        Reset = 1
    }

    internal enum GameplayEffectOverflowKind : byte
    {
        Reject = 0,
        ReplaceOldest = 1,
        ApplyOverflowEffects = 2
    }

    internal enum GameplayEffectLifecycleKind : byte
    {
        Applied = 1,
        Confirmed = 2,
        Rejected = 3,
        StackChanged = 4,
        Inhibited = 5,
        Resumed = 6,
        PeriodExecuted = 7,
        Removed = 8,
        Expired = 9,
        Corrected = 10,
        Overflow = 11
    }

    internal enum GameplayEffectApplyResultKind : byte
    {
        Applied = 1,
        Rejected = 2,
        MissingDefinition = 3,
        InvalidContext = 4,
        InvalidPrediction = 5,
        MissingParameter = 6,
        UndeclaredParameter = 7,
        RequirementFailed = 8,
        OverflowRejected = 9,
        DefinitionRevisionMismatch = 10
    }

    internal enum GameplayEffectRemoveSelector : byte
    {
        Handle = 0,
        EffectId = 1,
        SourceActor = 2,
        EffectTagQuery = 3
    }

    internal enum GameplayEffectComponentKind : byte
    {
        Modifier = 1,
        GrantedTags = 2,
        TagRequirement = 3,
        AttributeRequirement = 4,
        Execution = 5,
        AdditionalEffects = 6,
        Cue = 7
    }

    internal enum GameplayEffectRequirementPhase : byte
    {
        Application = 1,
        Ongoing = 2,
        Removal = 3
    }

    internal enum GameplayEffectModifierPhase : byte
    {
        BaseValue = 1,
        CurrentValue = 2
    }

    internal enum GameplayEffectAdditionalTrigger : byte
    {
        Applied = 1,
        Period = 2,
        Removed = 3,
        Overflow = 4
    }

    internal enum GameplayEffectCueTrigger : byte
    {
        OnActive = 1,
        WhileActive = 2,
        Executed = 3,
        Removed = 4,
        Expired = 5
    }

    internal readonly struct GameplayEffectComponentDescriptor
    {
        public GameplayEffectComponentDescriptor(
            GameplayEffectComponentKind kind,
            GameplayEffectRequirementPhase requirementPhase = default,
            GameplayEffectModifierPhase modifierPhase = default,
            GameplayEffectCueTrigger cueTrigger = default)
        {
            Kind = kind;
            RequirementPhase = requirementPhase;
            ModifierPhase = modifierPhase;
            CueTrigger = cueTrigger;
        }

        public GameplayEffectComponentKind Kind { get; }
        public GameplayEffectRequirementPhase RequirementPhase { get; }
        public GameplayEffectModifierPhase ModifierPhase { get; }
        public GameplayEffectCueTrigger CueTrigger { get; }
    }

    internal readonly struct GameplayEffectContextIdentity
    {
        public GameplayEffectContextIdentity(
            ActorId sourceActor,
            ActorId targetActor,
            ulong sourceActionInstanceId,
            ulong predictionKey,
            ulong gameplayResultId,
            ulong sourceTick,
            bool predicted)
        {
            if (!sourceActor.IsValid || !targetActor.IsValid || sourceTick == 0)
                throw new ArgumentException("Gameplay Effect context identity is incomplete.");
            if (predicted && (sourceActionInstanceId == 0 || predictionKey == 0))
                throw new ArgumentException("Predicted Gameplay Effect context requires Action and prediction identities.");
            SourceActor = sourceActor;
            TargetActor = targetActor;
            SourceActionInstanceId = sourceActionInstanceId;
            PredictionKey = predictionKey;
            GameplayResultId = gameplayResultId;
            SourceTick = sourceTick;
            Predicted = predicted;
        }

        public ActorId SourceActor { get; }
        public ActorId TargetActor { get; }
        public ulong SourceActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong GameplayResultId { get; }
        public ulong SourceTick { get; }
        public bool Predicted { get; }
        public bool IsValid => SourceActor.IsValid && TargetActor.IsValid && SourceTick != 0;
    }

    internal readonly struct GameplayEffectControlDescriptor
    {
        public GameplayEffectControlDescriptor(
            string effectId,
            uint revision,
            GameplayEffectDurationKind duration,
            GameplayEffectStackingKind stacking,
            int maximumStacks,
            GameplayEffectDurationUpdateKind durationUpdate,
            GameplayEffectPeriodUpdateKind periodUpdate,
            GameplayEffectOverflowKind overflow,
            bool executeOnApplication)
        {
            EffectId = SimulationIdentity.Require(effectId, nameof(effectId));
            if (revision == 0)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (maximumStacks < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumStacks));
            Revision = revision;
            Duration = duration;
            Stacking = stacking;
            MaximumStacks = maximumStacks;
            DurationUpdate = durationUpdate;
            PeriodUpdate = periodUpdate;
            Overflow = overflow;
            ExecuteOnApplication = executeOnApplication;
        }

        public string EffectId { get; }
        public uint Revision { get; }
        public GameplayEffectDurationKind Duration { get; }
        public GameplayEffectStackingKind Stacking { get; }
        public int MaximumStacks { get; }
        public GameplayEffectDurationUpdateKind DurationUpdate { get; }
        public GameplayEffectPeriodUpdateKind PeriodUpdate { get; }
        public GameplayEffectOverflowKind Overflow { get; }
        public bool ExecuteOnApplication { get; }
    }

    internal readonly struct GameplayEffectPreparedSpec<TSpec>
    {
        public GameplayEffectPreparedSpec(
            GameplayEffectControlDescriptor descriptor,
            GameplayEffectContextIdentity context,
            TSpec targetSpec,
            ulong durationTicks,
            ulong periodTicks)
        {
            if (!context.IsValid)
                throw new ArgumentException("Gameplay Effect prepared spec requires a valid context.", nameof(context));
            Descriptor = descriptor;
            Context = context;
            TargetSpec = targetSpec;
            DurationTicks = durationTicks;
            PeriodTicks = periodTicks;
        }

        public GameplayEffectControlDescriptor Descriptor { get; }
        public GameplayEffectContextIdentity Context { get; }
        public TSpec TargetSpec { get; }
        public ulong DurationTicks { get; }
        public ulong PeriodTicks { get; }
    }

    internal readonly struct GameplayEffectApplicationIdentity
    {
        public GameplayEffectApplicationIdentity(
            string effectId,
            ulong authoritativeInstanceId,
            ulong authoritativeLifecycleRevision)
        {
            EffectId = effectId ?? string.Empty;
            AuthoritativeInstanceId = authoritativeInstanceId;
            AuthoritativeLifecycleRevision = authoritativeLifecycleRevision;
        }

        public string EffectId { get; }
        public ulong AuthoritativeInstanceId { get; }
        public ulong AuthoritativeLifecycleRevision { get; }
    }

    internal readonly struct GameplayEffectApplyResult
    {
        public GameplayEffectApplyResult(
            GameplayEffectApplyResultKind kind,
            ulong handle,
            ulong instanceId,
            string reason)
        {
            Kind = kind;
            Handle = handle;
            InstanceId = instanceId;
            Reason = reason ?? string.Empty;
        }

        public GameplayEffectApplyResultKind Kind { get; }
        public ulong Handle { get; }
        public ulong InstanceId { get; }
        public string Reason { get; }
        public bool Succeeded => Kind == GameplayEffectApplyResultKind.Applied;
        public bool AcceptedMutation => Succeeded ||
            Kind == GameplayEffectApplyResultKind.OverflowRejected &&
            string.Equals(Reason, "OverflowEffectsApplied", StringComparison.Ordinal);
    }

    internal readonly struct GameplayEffectRemoveRequest<TTagQuery>
    {
        public GameplayEffectRemoveRequest(
            GameplayEffectRemoveSelector selector,
            ulong handle,
            string effectId,
            ActorId sourceActor,
            TTagQuery tagQuery)
        {
            Selector = selector;
            Handle = handle;
            EffectId = effectId ?? string.Empty;
            SourceActor = sourceActor;
            TagQuery = tagQuery;
        }

        public GameplayEffectRemoveSelector Selector { get; }
        public ulong Handle { get; }
        public string EffectId { get; }
        public ActorId SourceActor { get; }
        public TTagQuery TagQuery { get; }
    }

    internal readonly struct GameplayEffectActiveControlSnapshot
    {
        public GameplayEffectActiveControlSnapshot(
            ulong instanceId,
            ulong startTick,
            ulong endTick,
            ulong nextPeriodTick,
            int stackCount,
            bool inhibited,
            ulong lifecycleRevision)
        {
            InstanceId = instanceId;
            StartTick = startTick;
            EndTick = endTick;
            NextPeriodTick = nextPeriodTick;
            StackCount = stackCount;
            Inhibited = inhibited;
            LifecycleRevision = lifecycleRevision;
        }

        public ulong InstanceId { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public ulong NextPeriodTick { get; }
        public int StackCount { get; }
        public bool Inhibited { get; }
        public ulong LifecycleRevision { get; }
    }

    internal class GameplayEffectActiveControlState<TSpec> : IGameplayEffectActiveControl<TSpec>
    {
        public ulong Handle { get; set; }
        public ulong InstanceId { get; set; }
        public TSpec Spec { get; set; }
        public ulong StartTick { get; set; }
        public ulong EndTick { get; set; }
        public ulong InsertionSequence { get; set; }
        public int StackCount { get; set; }
        public bool Inhibited { get; set; }
        public ulong LifecycleRevision { get; set; }
    }

    internal abstract class GameplayEffectPredictionControlState<TSpec, TAttributeSnapshot> : IGameplayEffectPredictionControl<TSpec>
    {
        public TSpec Spec { get; set; }
        public ulong Handle { get; set; }
        public ulong InstanceId { get; set; }
        public bool CreatedActive { get; set; }
        public bool HasActiveBefore { get; set; }
        public GameplayEffectActiveControlSnapshot ActiveBefore { get; set; }
        public bool Confirmed { get; set; }
        public List<string> CueIds { get; } = new List<string>();
        public SortedDictionary<string, TAttributeSnapshot> Attributes { get; } = new SortedDictionary<string, TAttributeSnapshot>(StringComparer.Ordinal);

        ulong IGameplayEffectPredictionControl<TSpec>.PredictionKey => DescribeContext().PredictionKey;
        ulong IGameplayEffectPredictionControl<TSpec>.SourceActionInstanceId => DescribeContext().SourceActionInstanceId;
        IReadOnlyList<string> IGameplayEffectPredictionControl<TSpec>.CueIds => CueIds;

        protected abstract GameplayEffectContextIdentity DescribeContext();
    }

    internal interface IGameplayEffectActiveControl<TSpec>
    {
        ulong Handle { get; set; }
        ulong InstanceId { get; set; }
        TSpec Spec { get; set; }
        ulong StartTick { get; set; }
        ulong EndTick { get; set; }
        ulong InsertionSequence { get; set; }
        int StackCount { get; set; }
        bool Inhibited { get; set; }
        ulong LifecycleRevision { get; set; }
    }

    internal interface IGameplayEffectPredictionControl<TSpec>
    {
        TSpec Spec { get; }
        ulong PredictionKey { get; }
        ulong SourceActionInstanceId { get; }
        ulong Handle { get; }
        ulong InstanceId { get; }
        bool CreatedActive { get; }
        bool HasActiveBefore { get; }
        GameplayEffectActiveControlSnapshot ActiveBefore { get; }
        bool Confirmed { get; set; }
        IReadOnlyList<string> CueIds { get; }
    }

    internal readonly struct GameplayEffectLifecycleCommand<TApplication>
    {
        public GameplayEffectLifecycleCommand(
            GameplayEffectLifecycleKind kind,
            string effectId,
            ulong instanceId,
            GameplayEffectContextIdentity context,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong lifecycleRevision,
            TApplication authoritativeApplication)
        {
            Kind = kind;
            EffectId = effectId ?? string.Empty;
            InstanceId = instanceId;
            Context = context;
            StartTick = startTick;
            EndTick = endTick;
            StackCount = stackCount;
            LifecycleRevision = lifecycleRevision;
            AuthoritativeApplication = authoritativeApplication;
        }

        public GameplayEffectLifecycleKind Kind { get; }
        public string EffectId { get; }
        public ulong InstanceId { get; }
        public GameplayEffectContextIdentity Context { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public int StackCount { get; }
        public ulong LifecycleRevision { get; }
        public TApplication AuthoritativeApplication { get; }
    }

    internal interface IGameplayEffectControlPort<
        TApplication,
        TSpec,
        TActive,
        TPrediction,
        TTagQuery,
        TSavepoint>
        where TActive : class, IGameplayEffectActiveControl<TSpec>
        where TPrediction : class, IGameplayEffectPredictionControl<TSpec>
    {
        ActorId ActorId { get; }
        ulong Tick { get; }
        bool HasActiveEffects { get; }
        GameplayEffectApplicationIdentity DescribeApplication(TApplication application);
        bool TryPrepare(TApplication application, out GameplayEffectPreparedSpec<TSpec> spec, out GameplayEffectApplyResult failure);
        GameplayEffectPreparedSpec<TSpec> DescribeSpec(TSpec spec);
        int ComponentCount(TSpec spec);
        GameplayEffectComponentDescriptor DescribeComponent(TSpec spec, int componentIndex);
        bool EvaluateTagRequirement(TSpec spec, int componentIndex);
        bool EvaluateAttributeRequirement(TSpec spec, int componentIndex);
        bool MatchesEffectId(TSpec spec, string effectId);
        bool MatchesEffectTagQuery(TSpec spec, TTagQuery tagQuery);
        TActive FindActiveByHandle(ulong handle);
        TActive FindActiveByInstance(ulong instanceId);
        IReadOnlyList<TActive> AcquireActiveEffects();
        void ReleaseActiveEffects(IReadOnlyList<TActive> activeEffects);
        TActive CreateActive(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong handle,
            ulong instanceId,
            ulong startTick,
            ulong endTick,
            ulong insertionSequence,
            ulong lifecycleRevision);
        void AddActive(TActive active);
        void RemoveActive(TActive active);
        void MarkActiveEffectsDirty();
        ulong GetNextPeriod(ulong instanceId);
        void SetNextPeriod(ulong instanceId, ulong tick);
        void DeactivatePersistent(TActive active);
        void ActivateCurrentModifier(TActive active, int componentIndex);
        void ActivateGrantedTags(TActive active);
        void ExecuteNumericComponent(
            GameplayEffectPreparedSpec<TSpec> spec,
            TActive active,
            ulong handle,
            int stackCount,
            int componentIndex);
        int AdditionalEffectCount(TSpec spec, int componentIndex);
        GameplayEffectAdditionalTrigger DescribeAdditionalEffectTrigger(
            TSpec spec,
            int componentIndex,
            int effectIndex);
        TApplication BuildAdditionalApplication(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong instanceId,
            int componentIndex,
            int effectIndex);
        void EmitCue(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong instanceId,
            int componentIndex,
            bool trackPrediction);
        void RegisterCause(ulong handle, GameplayEffectPreparedSpec<TSpec> spec, ulong instanceId);
        void EmitLifecycle(TActive active, GameplayEffectLifecycleKind lifecycle);
        void EmitLifecycle(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong instanceId,
            GameplayEffectLifecycleKind lifecycle,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong revision,
            bool instant);
        void EmitFailure(string ownerEffectId, ulong ownerInstanceId, string requestedEffectId, GameplayEffectApplyResult failure);
        int ChangeCount { get; }
        void TrimChanges(int count);
        void RebuildCauses();
        TSavepoint CreateSavepoint();
        void Restore(TSavepoint savepoint);
        void Release(TSavepoint savepoint);
        bool SavepointIsActive(TSavepoint savepoint);
        ulong CaptureAllocator();
        void RestoreAllocator(ulong value);
        ulong AllocateHandle();
        TPrediction CreatePrediction(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong handle,
            ulong instanceId,
            bool createdActive,
            bool hasActiveBefore,
            GameplayEffectActiveControlSnapshot activeBefore);
        void SetCurrentPrediction(TPrediction prediction);
        void CompletePrediction(TPrediction prediction);
        void CancelPrediction(TPrediction prediction);
        bool TryGetPredictions(ulong predictionKey, out IReadOnlyList<TPrediction> predictions);
        IReadOnlyList<ulong> AcquirePredictionKeys();
        void ReleasePredictionKeys(IReadOnlyList<ulong> keys);
        void AddPrediction(TPrediction prediction);
        void RemovePredictions(ulong predictionKey);
        bool RestorePredictionAttributes(TPrediction prediction);
        void EmitPredictionCueRemoval(TPrediction prediction, string cueId);
        bool TryGetLastLifecycleRevision(ulong instanceId, out ulong revision);
        void SetLastLifecycleRevision(ulong instanceId, ulong revision);
        void MarkJournalDirty();
        bool TryEmitRejectedApplication(TApplication application, GameplayEffectApplyResult failure);
    }

    internal enum GameplayEffectAttributeSnapshotKind : byte
    {
        Source = 1,
        Target = 2
    }

    internal interface IGameplayEffectApplicationAdmissionPort<TApplication, TSpec, TScalar>
        where TApplication : class
        where TSpec : class
    {
        ActorId ActorId { get; }
        bool ContextIsValid(TApplication application);
        ActorId SourceActorId(TApplication application);
        ActorId TargetActorId(TApplication application);
        bool IsPredicted(TApplication application);
        ulong SourceActionInstanceId(TApplication application);
        ulong PredictionKey(TApplication application);
        ulong AuthoritativeInstanceId(TApplication application);
        uint ApplicationDefinitionRevision(TApplication application);
        bool TryCreateSpec(TApplication application, out TSpec spec);
        uint DefinitionRevision(TSpec spec);
        int SuppliedParameterCount(TApplication application);
        string SuppliedParameterId(TApplication application, int index);
        TScalar SuppliedParameterValue(TApplication application, int index);
        bool DeclaresParameter(TSpec spec, string parameterId);
        int RequiredParameterCount(TSpec spec);
        string RequiredParameterId(TSpec spec, int index);
        bool ContainsParameter(TSpec spec, string parameterId);
        void AddParameter(TSpec spec, string parameterId, TScalar value);
        int SuppliedSourceAttributeCount(TApplication application);
        string SuppliedSourceAttributeId(TApplication application, int index);
        TScalar SuppliedSourceAttributeValue(TApplication application, int index);
        string NormalizeAttributeId(string attributeId);
        IEnumerable<string> RequiredSnapshotAttributes(TSpec spec, GameplayEffectAttributeSnapshotKind kind);
        bool TryReadTargetAttribute(string attributeId, out TScalar value);
        void AddSourceAttribute(TSpec spec, string attributeId, TScalar value);
        void AddTargetAttribute(TSpec spec, string attributeId, TScalar value);
        string[] CopyTargetTags();
        int SourceTagCount(TApplication application);
        string SourceTag(TApplication application, int index);
        string NormalizeTag(string tag);
        void SetTargetTags(TSpec spec, string[] tags);
        void SetSourceTags(TSpec spec, string[] tags);
        bool RequiresDuration(TSpec spec);
        bool HasPeriod(TSpec spec);
        bool TryResolveDurationTicks(TSpec spec, out ulong ticks);
        bool TryResolvePeriodTicks(TSpec spec, out ulong ticks);
        void SetDurationTicks(TSpec spec, ulong ticks);
        void SetPeriodTicks(TSpec spec, ulong ticks);
        GameplayEffectPreparedSpec<TSpec> DescribeSpec(TSpec spec);
    }

    internal sealed class GameplayEffectApplicationAdmissionRuntime<TApplication, TSpec, TScalar>
        where TApplication : class
        where TSpec : class
    {
        readonly IGameplayEffectApplicationAdmissionPort<TApplication, TSpec, TScalar> m_Port;
        readonly Dictionary<string, TScalar> m_SuppliedSourceAttributes;

        public GameplayEffectApplicationAdmissionRuntime(
            IGameplayEffectApplicationAdmissionPort<TApplication, TSpec, TScalar> port,
            Dictionary<string, TScalar> suppliedSourceAttributes)
        {
            m_Port = port ?? throw new ArgumentNullException(nameof(port));
            m_SuppliedSourceAttributes = suppliedSourceAttributes ??
                throw new ArgumentNullException(nameof(suppliedSourceAttributes));
        }

        public bool TryPrepare(
            TApplication application,
            out GameplayEffectPreparedSpec<TSpec> prepared,
            out GameplayEffectApplyResult failure)
        {
            prepared = default;
            if (application == null || !m_Port.ContextIsValid(application) ||
                m_Port.TargetActorId(application) != m_Port.ActorId)
            {
                return Fail(application, GameplayEffectApplyResultKind.InvalidContext, "TargetActorMismatch", out failure);
            }
            if (!m_Port.TryCreateSpec(application, out TSpec spec))
                return Fail(application, GameplayEffectApplyResultKind.MissingDefinition, "EffectDefinitionMissing", out failure);
            if (m_Port.ApplicationDefinitionRevision(application) != m_Port.DefinitionRevision(spec))
            {
                return Fail(application, GameplayEffectApplyResultKind.DefinitionRevisionMismatch, "DefinitionRevisionMismatch", out failure);
            }
            if (m_Port.IsPredicted(application) &&
                (m_Port.SourceActionInstanceId(application) == 0 || m_Port.PredictionKey(application) == 0))
            {
                return Fail(application, GameplayEffectApplyResultKind.InvalidPrediction, "PredictionIdentityMissing", out failure);
            }

            for (int i = 0; i < m_Port.SuppliedParameterCount(application); i++)
            {
                string parameterId = m_Port.SuppliedParameterId(application, i);
                if (!m_Port.DeclaresParameter(spec, parameterId))
                {
                    return Fail(
                        application,
                        GameplayEffectApplyResultKind.UndeclaredParameter,
                        $"UndeclaredSetByCaller:{parameterId}",
                        out failure);
                }
                m_Port.AddParameter(spec, parameterId, m_Port.SuppliedParameterValue(application, i));
            }
            for (int i = 0; i < m_Port.RequiredParameterCount(spec); i++)
            {
                string parameterId = m_Port.RequiredParameterId(spec, i);
                if (!m_Port.ContainsParameter(spec, parameterId))
                {
                    return Fail(
                        application,
                        GameplayEffectApplyResultKind.MissingParameter,
                        $"MissingSetByCaller:{parameterId}",
                        out failure);
                }
            }

            m_SuppliedSourceAttributes.Clear();
            try
            {
                for (int i = 0; i < m_Port.SuppliedSourceAttributeCount(application); i++)
                {
                    string attributeId = m_Port.NormalizeAttributeId(m_Port.SuppliedSourceAttributeId(application, i));
                    m_SuppliedSourceAttributes.Add(attributeId, m_Port.SuppliedSourceAttributeValue(application, i));
                }
                bool selfSource = m_Port.SourceActorId(application) == m_Port.TargetActorId(application);
                foreach (string attributeId in m_Port.RequiredSnapshotAttributes(spec, GameplayEffectAttributeSnapshotKind.Source))
                {
                    if (m_SuppliedSourceAttributes.TryGetValue(attributeId, out TScalar supplied))
                        m_Port.AddSourceAttribute(spec, attributeId, supplied);
                    else if (selfSource && m_Port.TryReadTargetAttribute(attributeId, out TScalar current))
                        m_Port.AddSourceAttribute(spec, attributeId, current);
                    else
                    {
                        return Fail(
                            application,
                            GameplayEffectApplyResultKind.InvalidContext,
                            $"SourceAttributeSnapshotMissing:{attributeId}",
                            out failure);
                    }
                }
                foreach (string attributeId in m_Port.RequiredSnapshotAttributes(spec, GameplayEffectAttributeSnapshotKind.Target))
                {
                    if (!m_Port.TryReadTargetAttribute(attributeId, out TScalar current))
                    {
                        return Fail(
                            application,
                            GameplayEffectApplyResultKind.InvalidContext,
                            $"TargetAttributeSnapshotMissing:{attributeId}",
                            out failure);
                    }
                    m_Port.AddTargetAttribute(spec, attributeId, current);
                }

                string[] targetTags = m_Port.CopyTargetTags();
                m_Port.SetTargetTags(spec, targetTags);
                if (m_Port.SourceTagCount(application) > 0)
                {
                    var sourceTags = new string[m_Port.SourceTagCount(application)];
                    for (int i = 0; i < sourceTags.Length; i++)
                        sourceTags[i] = m_Port.NormalizeTag(m_Port.SourceTag(application, i));
                    Array.Sort(sourceTags, StringComparer.Ordinal);
                    m_Port.SetSourceTags(spec, sourceTags);
                }
                else
                {
                    m_Port.SetSourceTags(spec, selfSource ? targetTags : Array.Empty<string>());
                }
            }
            finally
            {
                m_SuppliedSourceAttributes.Clear();
            }

            if (m_Port.RequiresDuration(spec))
            {
                if (!m_Port.TryResolveDurationTicks(spec, out ulong durationTicks))
                    return Fail(application, GameplayEffectApplyResultKind.InvalidContext, "DurationMagnitudeInvalid", out failure);
                m_Port.SetDurationTicks(spec, durationTicks);
            }
            if (m_Port.HasPeriod(spec))
            {
                if (!m_Port.TryResolvePeriodTicks(spec, out ulong periodTicks))
                    return Fail(application, GameplayEffectApplyResultKind.InvalidContext, "PeriodMagnitudeInvalid", out failure);
                m_Port.SetPeriodTicks(spec, periodTicks);
            }

            prepared = m_Port.DescribeSpec(spec);
            failure = default;
            return true;
        }

        bool Fail(
            TApplication application,
            GameplayEffectApplyResultKind kind,
            string reason,
            out GameplayEffectApplyResult failure)
        {
            failure = new GameplayEffectApplyResult(
                kind,
                0,
                application == null ? 0 : m_Port.AuthoritativeInstanceId(application),
                reason);
            return false;
        }
    }
}
