using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedStateAccessPolicy
    {
        readonly bool[] m_Allowed;

        public FixedStateAccessPolicy(params ProgramStateSemantic[] semantics)
        {
            Array values = Enum.GetValues(typeof(ProgramStateSemantic));
            int maximum = 0;
            for (int i = 0; i < values.Length; i++)
                maximum = Math.Max(maximum, Convert.ToInt32(values.GetValue(i)));
            m_Allowed = new bool[maximum + 1];
            if (semantics == null)
                throw new ArgumentNullException(nameof(semantics));
            for (int i = 0; i < semantics.Length; i++)
                m_Allowed[(int)semantics[i]] = true;
        }

        public bool Allows(ProgramStateSemantic semantic)
        {
            int index = (int)semantic;
            return index >= 0 && index < m_Allowed.Length && m_Allowed[index];
        }
    }

    internal sealed class FixedProgramExecutionServices : IProgramExecutionServices
    {
        readonly ProgramExecutionLayout m_Layout;
        readonly string[] m_OperationSourcePaths;
        readonly IReadOnlyDictionary<int, ProgramCurve> m_ProgramCurves;
        readonly PortableTagQuery[] m_TagQueries;
        readonly SimulationSetByCallerValue[][] m_SetByCallerValues;
        readonly IReadOnlyDictionary<GameplayCueProducerKey, ProgramProducer> m_GameplayCueProducers;
        readonly IReadOnlyDictionary<string, SimulationBlackboardSlotGroup> m_BlackboardGroups;
        readonly IReadOnlyDictionary<ProgramScopeLayout, IReadOnlyList<SimulationBlackboardSlotGroup>> m_ScopeBlackboardGroups;
        readonly ActionAdmissionProfile[] m_ActionProfilesByOperation;
        readonly IReadOnlyDictionary<string, ActionAdmissionProfile> m_ActionProfilesById;

        public FixedProgramExecutionServices(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            OperationExecutionTopology topology,
            string[] operationSourcePaths)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            layout.RequireProgram(program);
            if (topology == null)
                throw new ArgumentNullException(nameof(topology));
            if (operationSourcePaths == null || operationSourcePaths.Length != program.Operations.Count)
                throw new ArgumentException("Program execution services SourceMap index is incomplete.", nameof(operationSourcePaths));

            m_Layout = layout;
            m_OperationSourcePaths = operationSourcePaths;
            Identity = new ProgramLayoutIdentity(
                program.Manifest.ProgramId,
                program.ProgramHash,
                program.LayoutHash,
                program.Manifest.OperationSetVersion,
                program.Manifest.NumericProfile);
            Topology = topology;
            m_ProgramCurves = BuildProgramCurves(program);
            m_TagQueries = BuildTagQueries(program);
            m_SetByCallerValues = BuildSetByCallerValues(program);
            GameplayEffectProgram = new SimulationGameplayEffectProgram(program);
            m_GameplayCueProducers = BuildGameplayCueProducers(program, GameplayEffectProgram);
            BuildActionProfiles(program, layout, out m_ActionProfilesByOperation, out m_ActionProfilesById);
            BuildBlackboardGroups(
                program,
                out m_BlackboardGroups,
                out m_ScopeBlackboardGroups);
            EventSequencePolicy = new FixedStateAccessPolicy(ProgramStateSemantic.FactSequence);
            ControlPolicy = new FixedStateAccessPolicy(
                ProgramStateSemantic.RunnableLifecycle,
                ProgramStateSemantic.RunnableChildCursor,
                ProgramStateSemantic.RunnableStopBarrier,
                ProgramStateSemantic.RunnableActivationGeneration,
                ProgramStateSemantic.LocomotionMotionElapsedTicks,
                ProgramStateSemantic.StateMachineActive,
                ProgramStateSemantic.StateMachinePending,
                ProgramStateSemantic.StateMachineExiting,
                ProgramStateSemantic.StateMachineTransition,
                ProgramStateSemantic.StateMachineExecutionPath);
            ActionPolicy = new FixedStateAccessPolicy(
                ProgramStateSemantic.ActionRequestBuffer,
                ProgramStateSemantic.ActionInstance,
                ProgramStateSemantic.ActionEventSequence);
            InputPolicy = new FixedStateAccessPolicy(ProgramStateSemantic.InputRequestBuffer);
            HandleAllocatorPolicy = new FixedStateAccessPolicy(ProgramStateSemantic.HandleAllocator);
            BlackboardPolicy = new FixedStateAccessPolicy(
                ProgramStateSemantic.BlackboardValue,
                ProgramStateSemantic.BlackboardOwnerToken,
                ProgramStateSemantic.BlackboardLifetime,
                ProgramStateSemantic.BlackboardWriteStamp);
            TimelinePolicy = new FixedStateAccessPolicy(
                ProgramStateSemantic.TimelinePlayback,
                ProgramStateSemantic.TimelineLoop,
                ProgramStateSemantic.TimelineTreeClipCycle,
                ProgramStateSemantic.TimelineRetentionIdentity,
                ProgramStateSemantic.TimelineLogicTime);
            MotionModifierPolicy = new FixedStateAccessPolicy(
                ProgramStateSemantic.MotionWarpActive,
                ProgramStateSemantic.MotionWarpInitialized,
                ProgramStateSemantic.MotionWarpPlaybackGeneration,
                ProgramStateSemantic.MotionWarpActionInstance,
                ProgramStateSemantic.MotionWarpStartBodyPosition,
                ProgramStateSemantic.MotionWarpStartBodyYaw,
                ProgramStateSemantic.MotionWarpSourceWindowStartPosition,
                ProgramStateSemantic.MotionWarpSourceWindowStartYaw,
                ProgramStateSemantic.MotionWarpResolvedTargetPosition,
                ProgramStateSemantic.MotionWarpResolvedTargetYaw,
                ProgramStateSemantic.MotionWarpLimitResult,
                ProgramStateSemantic.MotionWarpPreviousWarpedPosition,
                ProgramStateSemantic.MotionWarpPreviousWarpedYaw,
                ProgramStateSemantic.MotionWarpLastPositionProgress,
                ProgramStateSemantic.MotionWarpLastYawProgress,
                ProgramStateSemantic.MotionWarpSourceOperation);
            EquipmentPolicy = new FixedStateAccessPolicy(
                ProgramStateSemantic.EquipmentAggregate,
                ProgramStateSemantic.EquipmentLocalState);
            Access = new FixedProgramAccess(program, layout, this);
        }

        public FixedProgramAccess Access { get; }
        public ProgramLayoutIdentity Identity { get; }
        public OperationExecutionTopology Topology { get; }
        public SimulationGameplayEffectProgram GameplayEffectProgram { get; }
        public FixedStateAccessPolicy EventSequencePolicy { get; }
        public FixedStateAccessPolicy ControlPolicy { get; }
        public FixedStateAccessPolicy ActionPolicy { get; }
        public FixedStateAccessPolicy InputPolicy { get; }
        public FixedStateAccessPolicy HandleAllocatorPolicy { get; }
        public FixedStateAccessPolicy BlackboardPolicy { get; }
        public FixedStateAccessPolicy TimelinePolicy { get; }
        public FixedStateAccessPolicy MotionModifierPolicy { get; }
        public FixedStateAccessPolicy EquipmentPolicy { get; }

        public string SourcePath(OperationHandle operation)
        {
            if (!operation.IsValid || operation.Value >= m_OperationSourcePaths.Length)
                throw new ArgumentOutOfRangeException(nameof(operation));
            return m_OperationSourcePaths[operation.Value];
        }

        public void RequireIdentity(ProgramLayoutIdentity identity)
        {
            Identity.Require(identity);
        }

        public ProgramCurve RequireTimelineCurve(ProgramConstant constant, string identity)
        {
            if (constant == null)
                throw new ArgumentNullException(nameof(constant));
            if (!m_ProgramCurves.TryGetValue(constant.Index, out ProgramCurve curve))
                throw new InvalidDataException($"Program curve '{identity}' was not compiled into Program execution services.");
            return curve;
        }

        public PortableTagQuery RequireTagQuery(OperationHandle operation)
        {
            if (!operation.IsValid || operation.Value >= m_TagQueries.Length)
                throw new ArgumentOutOfRangeException(nameof(operation));
            return m_TagQueries[operation.Value] ??
                throw new InvalidOperationException($"Operation '{SourcePath(operation)}' has no compiled Gameplay Tag query.");
        }

        public SimulationSetByCallerValue[] SetByCallerValues(OperationHandle operation)
        {
            if (!operation.IsValid || operation.Value >= m_SetByCallerValues.Length)
                throw new ArgumentOutOfRangeException(nameof(operation));
            return m_SetByCallerValues[operation.Value];
        }

        public ActionAdmissionProfile RequireActionProfile(OperationHandle operation)
        {
            if (!operation.IsValid || operation.Value >= m_ActionProfilesByOperation.Length)
                throw new ArgumentOutOfRangeException(nameof(operation));
            return m_ActionProfilesByOperation[operation.Value] ??
                throw new InvalidOperationException($"Operation '{SourcePath(operation)}' has no compiled Action profile.");
        }

        public ActionAdmissionProfile RequireActionProfile(string actionId)
        {
            string identity = SimulationIdentity.Require(actionId, nameof(actionId));
            return m_ActionProfilesById.TryGetValue(identity, out ActionAdmissionProfile profile)
                ? profile
                : throw new InvalidOperationException($"Action profile '{identity}' is absent from the Program catalog.");
        }

        public ProgramProducer RequireGameplayCueProducer(string effectId, string cueId)
        {
            var key = new GameplayCueProducerKey(effectId, cueId);
            if (!m_GameplayCueProducers.TryGetValue(key, out ProgramProducer producer))
                throw new InvalidOperationException($"Gameplay Cue '{effectId}/{cueId}' has no compiled Program producer.");
            return producer;
        }

        public SimulationBlackboardSlotGroup RequireBlackboardGroup(string ownerIdentity)
        {
            if (!m_BlackboardGroups.TryGetValue(ownerIdentity, out SimulationBlackboardSlotGroup group))
                throw new InvalidOperationException($"Blackboard state group '{ownerIdentity}' is incomplete.");
            return group;
        }

        public IReadOnlyList<SimulationBlackboardSlotGroup> BlackboardGroups(ProgramScopeLayout scope)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));
            return m_ScopeBlackboardGroups.TryGetValue(scope, out IReadOnlyList<SimulationBlackboardSlotGroup> groups)
                ? groups
                : Array.Empty<SimulationBlackboardSlotGroup>();
        }

        public bool TryGetInitialBlackboardOwnerToken(int stateSlot, out BlackboardOwnerToken token)
        {
            foreach (SimulationBlackboardSlotGroup group in m_BlackboardGroups.Values)
            {
                if (group.OwnerToken != stateSlot)
                    continue;
                if (group.Scope.Kind == ProgramScopeKind.Character ||
                    group.Scope.Kind == ProgramScopeKind.Graph && group.LifetimeKind == ProgramBlackboardLifetime.Config)
                {
                    token = new BlackboardOwnerToken(group.Scope.Kind, group.CompiledOwnerIndex, 1);
                    return true;
                }
                break;
            }
            token = default;
            return false;
        }

        static IReadOnlyDictionary<int, ProgramCurve> BuildProgramCurves(CharacterSimulationProgram program)
        {
            var result = new Dictionary<int, ProgramCurve>();
            for (int entryIndex = 0; entryIndex < program.CatalogEntries.Count; entryIndex++)
            {
                ProgramCatalogEntry entry = program.CatalogEntries[entryIndex];
                if (entry.Kind != ProgramCatalogEntryKind.TimelineClip &&
                    entry.Kind != ProgramCatalogEntryKind.MotionCurve)
                {
                    continue;
                }
                for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
                {
                    ProgramCatalogField field = entry.Fields[fieldIndex];
                    if (field.Kind != ProgramCatalogFieldKind.Constant)
                        continue;
                    ProgramConstant constant = program.Constants[field.ConstantIndex];
                    if (constant.Kind != ProgramConstantKind.Bytes || result.ContainsKey(constant.Index))
                        continue;
                    byte[] bytes = constant.Bytes.ToArray();
                    if (bytes.Length == 0)
                        throw new InvalidDataException($"Timeline curve '{entry.Identity}/{field.Name}' is empty.");
                    result.Add(constant.Index, ProgramCurveCodec.Read(bytes));
                }
            }
            for (int i = 0; i < program.Constants.Count; i++)
            {
                ProgramConstant constant = program.Constants[i];
                if (constant.Kind != ProgramConstantKind.Bytes || result.ContainsKey(constant.Index) ||
                    !OperationNamedConstantSchema.TryParseIdentity(constant.Identity, out OperationNamedConstant field) ||
                    field != OperationNamedConstant.ActionMotionPositionX && field != OperationNamedConstant.ActionMotionPositionZ)
                    continue;
                byte[] bytes = constant.Bytes.ToArray();
                if (bytes.Length == 0)
                    throw new InvalidDataException($"Program curve '{constant.Identity}' is empty.");
                result.Add(constant.Index, ProgramCurveCodec.Read(bytes));
            }
            return result;
        }

        static void BuildActionProfiles(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            out ActionAdmissionProfile[] byOperation,
            out IReadOnlyDictionary<string, ActionAdmissionProfile> byId)
        {
            var profiles = new Dictionary<string, ActionAdmissionProfile>(StringComparer.Ordinal);
            for (int entryIndex = 0; entryIndex < program.CatalogEntries.Count; entryIndex++)
            {
                ProgramCatalogEntry entry = program.CatalogEntries[entryIndex];
                if (entry.Kind != ProgramCatalogEntryKind.Action)
                    continue;
                ActionAdmissionProfile profile = ActionAdmissionProfileCompiler.Compile(
                    entry,
                    constantIndex => ReadActionTargetRequirement(program, constantIndex));
                if (!profiles.TryAdd(profile.ActionId, profile))
                    throw new InvalidDataException($"Action profile '{profile.ActionId}' is duplicated.");
            }
            byOperation = new ActionAdmissionProfile[program.Operations.Count];
            for (int operationIndex = 0; operationIndex < byOperation.Length; operationIndex++)
            {
                ProgramCatalogEntry entry = layout.FindCatalog(new OperationHandle(operationIndex), ProgramCatalogEntryKind.Action);
                if (entry == null)
                    continue;
                ActionAdmissionProfile profile = ActionAdmissionProfileCompiler.Compile(
                    entry,
                    constantIndex => ReadActionTargetRequirement(program, constantIndex));
                byOperation[operationIndex] = profiles[profile.ActionId];
            }
            byId = profiles;
        }

        static int ReadActionTargetRequirement(CharacterSimulationProgram program, int constantIndex)
        {
            if (constantIndex < 0 || constantIndex >= program.Constants.Count)
                throw new InvalidDataException($"Action target requirement constant '{constantIndex}' is out of range.");
            ProgramConstant constant = program.Constants[constantIndex];
            if (constant.Kind != ProgramConstantKind.Int32)
                throw new InvalidDataException($"Action target requirement constant '{constant.Identity}' has kind '{constant.Kind}'.");
            return constant.Int32;
        }

        static PortableTagQuery[] BuildTagQueries(CharacterSimulationProgram program)
        {
            var result = new PortableTagQuery[program.Operations.Count];
            for (int operationIndex = 0; operationIndex < program.Operations.Count; operationIndex++)
            {
                SimulationOperation operation = program.Operations[operationIndex];
                if (operation.Code != SimulationOperationCode.GameplayEffectMatchTags &&
                    operation.Code != SimulationOperationCode.GameplayEffectRemove)
                    continue;
                var all = new List<string>();
                var any = new List<string>();
                var none = new List<string>();
                for (int i = 0; i < operation.ConstantReferences.Count; i++)
                {
                    ProgramConstant constant = program.Constants[operation.ConstantReferences[i]];
                    if (!OperationNamedConstantSchema.TryGetDynamicField(constant.Identity, out string field))
                        continue;
                    if (!field.StartsWith("Query:", StringComparison.Ordinal))
                        continue;
                    if (constant.Kind != ProgramConstantKind.String)
                        throw new InvalidDataException($"Gameplay Tag query field '{field}' is not String.");
                    if (field.StartsWith("Query:All:", StringComparison.Ordinal))
                        all.Add(constant.Text);
                    else if (field.StartsWith("Query:Any:", StringComparison.Ordinal))
                        any.Add(constant.Text);
                    else if (field.StartsWith("Query:None:", StringComparison.Ordinal))
                        none.Add(constant.Text);
                }
                result[operationIndex] = new PortableTagQuery(all, any, none);
            }
            return result;
        }

        static SimulationSetByCallerValue[][] BuildSetByCallerValues(CharacterSimulationProgram program)
        {
            var result = new SimulationSetByCallerValue[program.Operations.Count][];
            for (int operationIndex = 0; operationIndex < program.Operations.Count; operationIndex++)
            {
                SimulationOperation operation = program.Operations[operationIndex];
                if (operation.Code != SimulationOperationCode.GameplayEffectApply)
                {
                    result[operationIndex] = Array.Empty<SimulationSetByCallerValue>();
                    continue;
                }
                var values = new List<SimulationSetByCallerValue>();
                for (int i = 0; i < operation.ConstantReferences.Count; i++)
                {
                    ProgramConstant constant = program.Constants[operation.ConstantReferences[i]];
                    if (!OperationNamedConstantSchema.TryGetDynamicField(constant.Identity, out string field) ||
                        !field.StartsWith("SetByCaller:", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if (constant.Kind != ProgramConstantKind.Scalar)
                        throw new InvalidDataException($"Gameplay Effect SetByCaller field '{field}' is not Scalar.");
                    string parameterId = field.Substring("SetByCaller:".Length);
                    values.Add(new SimulationSetByCallerValue(parameterId, constant.Scalar));
                }
                values.Sort((left, right) => string.CompareOrdinal(left.ParameterId, right.ParameterId));
                for (int i = 1; i < values.Count; i++)
                {
                    if (string.Equals(values[i - 1].ParameterId, values[i].ParameterId, StringComparison.Ordinal))
                        throw new InvalidDataException($"Gameplay Effect operation '{operation.Handle}' contains duplicate SetByCaller parameter '{values[i].ParameterId}'.");
                }
                result[operationIndex] = values.ToArray();
            }
            return result;
        }

        static IReadOnlyDictionary<GameplayCueProducerKey, ProgramProducer> BuildGameplayCueProducers(
            CharacterSimulationProgram program,
            SimulationGameplayEffectProgram gameplayEffects)
        {
            var result = new Dictionary<GameplayCueProducerKey, ProgramProducer>();
            for (int i = 0; i < program.Producers.Count; i++)
            {
                ProgramProducer producer = program.Producers[i];
                if (producer.ChannelKind != ProgramOutputChannelKind.Presentation ||
                    !gameplayEffects.Effects.ContainsKey(producer.SourceIdentity))
                {
                    continue;
                }
                int separator = producer.Identity.LastIndexOf(':');
                if (separator < 0 || separator == producer.Identity.Length - 1)
                    throw new InvalidDataException($"Gameplay Effect producer '{producer.Identity}' has no Cue identity suffix.");
                var key = new GameplayCueProducerKey(
                    producer.SourceIdentity,
                    producer.Identity.Substring(separator + 1));
                if (result.ContainsKey(key))
                    throw new InvalidDataException($"Gameplay Cue '{key.EffectId}/{key.CueId}' has multiple Program producers.");
                result.Add(key, producer);
            }
            return result;
        }

        readonly struct GameplayCueProducerKey : IEquatable<GameplayCueProducerKey>
        {
            public GameplayCueProducerKey(string effectId, string cueId)
            {
                EffectId = SimulationIdentity.Require(effectId, nameof(effectId));
                CueId = SimulationIdentity.Require(cueId, nameof(cueId));
            }

            public string EffectId { get; }
            public string CueId { get; }

            public bool Equals(GameplayCueProducerKey other) =>
                string.Equals(EffectId, other.EffectId, StringComparison.Ordinal) &&
                string.Equals(CueId, other.CueId, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is GameplayCueProducerKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(EffectId) * 397) ^
                        StringComparer.Ordinal.GetHashCode(CueId);
                }
            }
        }

        static void BuildBlackboardGroups(
            CharacterSimulationProgram program,
            out IReadOnlyDictionary<string, SimulationBlackboardSlotGroup> groups,
            out IReadOnlyDictionary<ProgramScopeLayout, IReadOnlyList<SimulationBlackboardSlotGroup>> scopeGroups)
        {
            var byOwner = new Dictionary<string, SimulationBlackboardSlotGroup>(StringComparer.Ordinal);
            var byScope = new Dictionary<ProgramScopeLayout, IReadOnlyList<SimulationBlackboardSlotGroup>>();
            for (int scopeIndex = 0; scopeIndex < program.Scopes.Count; scopeIndex++)
            {
                ProgramScopeLayout scope = program.Scopes[scopeIndex];
                if (scope.CompiledOwnerIndex != scopeIndex)
                    throw new InvalidOperationException($"Program scope '{scope.Identity}' compiled owner index is not canonical.");
                var owners = new HashSet<string>(StringComparer.Ordinal);
                var values = new List<SimulationBlackboardSlotGroup>();
                for (int slotIndex = 0; slotIndex < scope.StateSlots.Count; slotIndex++)
                {
                    ProgramStateSlot slot = program.StateSlots[scope.StateSlots[slotIndex]];
                    if (slot.Semantic != ProgramStateSemantic.BlackboardValue || !owners.Add(slot.OwnerIdentity))
                        continue;
                    SimulationBlackboardSlotGroup group = BuildBlackboardGroup(
                        program,
                        scope,
                        scope.CompiledOwnerIndex,
                        slot.OwnerIdentity);
                    if (byOwner.ContainsKey(slot.OwnerIdentity))
                        throw new InvalidOperationException($"Blackboard state group '{slot.OwnerIdentity}' belongs to multiple scopes.");
                    byOwner.Add(slot.OwnerIdentity, group);
                    values.Add(group);
                }
                byScope.Add(
                    scope,
                    values.Count == 0
                        ? Array.Empty<SimulationBlackboardSlotGroup>()
                        : Array.AsReadOnly(values.ToArray()));
            }
            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                ProgramStateSlot slot = program.StateSlots[i];
                if (slot.Semantic == ProgramStateSemantic.BlackboardValue && !byOwner.ContainsKey(slot.OwnerIdentity))
                    throw new InvalidOperationException($"Blackboard state group '{slot.OwnerIdentity}' has no compiled scope owner.");
            }
            groups = byOwner;
            scopeGroups = byScope;
        }

        static SimulationBlackboardSlotGroup BuildBlackboardGroup(
            CharacterSimulationProgram program,
            ProgramScopeLayout scope,
            int compiledOwnerIndex,
            string ownerIdentity)
        {
            int value = -1;
            int ownerToken = -1;
            int lifetime = -1;
            int writeStamp = -1;
            for (int i = 0; i < scope.StateSlots.Count; i++)
            {
                int stateSlot = scope.StateSlots[i];
                ProgramStateSlot slot = program.StateSlots[stateSlot];
                if (!string.Equals(slot.OwnerIdentity, ownerIdentity, StringComparison.Ordinal))
                    continue;
                switch (slot.Semantic)
                {
                    case ProgramStateSemantic.BlackboardValue: value = UniqueSlot(value, stateSlot, ownerIdentity); break;
                    case ProgramStateSemantic.BlackboardOwnerToken: ownerToken = UniqueSlot(ownerToken, stateSlot, ownerIdentity); break;
                    case ProgramStateSemantic.BlackboardLifetime: lifetime = UniqueSlot(lifetime, stateSlot, ownerIdentity); break;
                    case ProgramStateSemantic.BlackboardWriteStamp: writeStamp = UniqueSlot(writeStamp, stateSlot, ownerIdentity); break;
                }
            }
            if (value < 0 || ownerToken < 0 || lifetime < 0 || writeStamp < 0)
                throw new InvalidOperationException($"Blackboard state group '{ownerIdentity}' is incomplete.");
            ProgramStateSlot lifetimeSlot = program.StateSlots[lifetime];
            if (lifetimeSlot.DefaultConstantIndex < 0)
                throw new InvalidOperationException($"Blackboard state group '{ownerIdentity}' has no lifetime constant.");
            ProgramConstant lifetimeConstant = program.Constants[lifetimeSlot.DefaultConstantIndex];
            if (lifetimeConstant.Kind != ProgramConstantKind.Int32 ||
                lifetimeConstant.Int32 < byte.MinValue || lifetimeConstant.Int32 > byte.MaxValue ||
                !Enum.IsDefined(typeof(ProgramBlackboardLifetime), (byte)lifetimeConstant.Int32))
            {
                throw new InvalidOperationException($"Blackboard state group '{ownerIdentity}' lifetime is invalid.");
            }
            return new SimulationBlackboardSlotGroup(
                value,
                ownerToken,
                lifetime,
                writeStamp,
                scope,
                compiledOwnerIndex,
                (ProgramBlackboardLifetime)(byte)lifetimeConstant.Int32);
        }

        static int UniqueSlot(int current, int incoming, string ownerIdentity)
        {
            if (current >= 0)
                throw new InvalidOperationException($"Blackboard state group '{ownerIdentity}' contains duplicate semantics.");
            return incoming;
        }
    }

}
