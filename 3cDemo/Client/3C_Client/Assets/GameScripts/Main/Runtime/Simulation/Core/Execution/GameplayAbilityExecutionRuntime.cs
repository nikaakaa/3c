using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal static class GameplayAbilityExecutionSlotMap
    {
        public static HashSet<int> Build(
            int operationCount,
            IReadOnlyList<ProgramControlFlowEdge> controlFlow,
            IReadOnlyList<ProgramScopeLayout> scopes,
            GameplayAbilityExecutionCatalog skills,
            IReadOnlyList<ProgramReference> references,
            IReadOnlyList<ProgramStateSlot> stateSlots,
            Func<int, IReadOnlyList<int>> operationStateSlots)
        {
            var result = new HashSet<int>();
            var characterScoped = new HashSet<int>();
            for (int i = 0; i < scopes.Count; i++)
            {
                ProgramScopeLayout scope = scopes[i];
                if (scope.Kind != ProgramScopeKind.Character)
                    continue;
                for (int slotIndex = 0; slotIndex < scope.StateSlots.Count; slotIndex++)
                    characterScoped.Add(scope.StateSlots[slotIndex]);
            }

            var outgoing = new List<int>[operationCount];
            for (int i = 0; i < controlFlow.Count; i++)
            {
                ProgramControlFlowEdge edge = controlFlow[i];
                if (!edge.Source.IsValid || !edge.Target.IsValid)
                    continue;
                List<int> targets = outgoing[edge.Source.Value];
                if (targets == null)
                {
                    targets = new List<int>();
                    outgoing[edge.Source.Value] = targets;
                }
                targets.Add(edge.Target.Value);
            }

            var visited = new bool[operationCount];
            for (int i = 0; i < skills.Bindings.Count; i++)
                VisitSkillOperation(skills.Bindings[i].EntryOperation, outgoing, visited);

            for (int operationIndex = 0; operationIndex < visited.Length; operationIndex++)
            {
                if (!visited[operationIndex])
                    continue;
                IReadOnlyList<int> operationSlots = operationStateSlots(operationIndex);
                for (int slotIndex = 0; slotIndex < operationSlots.Count; slotIndex++)
                    AddSkillExecutionStateSlot(operationSlots[slotIndex], stateSlots, characterScoped, result);
                for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
                {
                    ProgramReference reference = references[referenceIndex];
                    if (!reference.HasSourceOperation ||
                        !reference.SourceOperation.Equals(new OperationHandle(operationIndex)) ||
                        reference.Kind != ProgramReferenceKind.StateSlot)
                        continue;
                    AddSkillExecutionStateSlot(reference.TargetIndex, stateSlots, characterScoped, result);
                }
            }

            for (int scopeIndex = 0; scopeIndex < scopes.Count; scopeIndex++)
            {
                ProgramScopeLayout scope = scopes[scopeIndex];
                if (scope.Kind == ProgramScopeKind.Character ||
                    !scope.OwnerOperation.IsValid ||
                    !visited[scope.OwnerOperation.Value])
                    continue;
                for (int slotIndex = 0; slotIndex < scope.StateSlots.Count; slotIndex++)
                    AddSkillExecutionStateSlot(scope.StateSlots[slotIndex], stateSlots, characterScoped, result);
            }
            return result;
        }

        static void VisitSkillOperation(
            OperationHandle operation,
            List<int>[] outgoing,
            bool[] visited)
        {
            if (!operation.IsValid || operation.Value < 0 || operation.Value >= visited.Length || visited[operation.Value])
                return;
            visited[operation.Value] = true;
            List<int> targets = outgoing[operation.Value];
            if (targets == null)
                return;
            for (int i = 0; i < targets.Count; i++)
                VisitSkillOperation(new OperationHandle(targets[i]), outgoing, visited);
        }

        static void AddSkillExecutionStateSlot(
            int slotIndex,
            IReadOnlyList<ProgramStateSlot> stateSlots,
            HashSet<int> characterScoped,
            HashSet<int> result)
        {
            if (slotIndex < 0 || slotIndex >= stateSlots.Count || characterScoped.Contains(slotIndex))
                return;
            ProgramStateOwnerKind owner = stateSlots[slotIndex].OwnerKind;
            if (owner == ProgramStateOwnerKind.Runnable ||
                owner == ProgramStateOwnerKind.StateMachine ||
                owner == ProgramStateOwnerKind.Timeline ||
                owner == ProgramStateOwnerKind.Blackboard)
                result.Add(slotIndex);
        }
    }

    internal sealed class GameplayAbilityExecutionFrame<TValue>
        where TValue : struct, IEquatable<TValue>
    {
        readonly SortedDictionary<int, TValue> m_Values;

        public GameplayAbilityExecutionFrame(
            CharacterSkillId skillId,
            OperationHandle entryOperation,
            ulong actionInstanceId,
            ulong predictionKey,
            ulong generation,
            Dictionary<int, TValue> values = null)
        {
            if (!skillId.IsValid || !entryOperation.IsValid || actionInstanceId == 0 || predictionKey == 0)
                throw new ArgumentException("Skill execution frame identity is incomplete.");
            SkillId = skillId;
            EntryOperation = entryOperation;
            ActionInstanceId = actionInstanceId;
            PredictionKey = predictionKey;
            Generation = generation;
            m_Values = new SortedDictionary<int, TValue>();
            if (values == null)
                return;
            foreach (KeyValuePair<int, TValue> value in values)
            {
                if (value.Key < 0 || !m_Values.TryAdd(value.Key, value.Value))
                    throw new ArgumentException("Skill execution frame state values are invalid or duplicated.", nameof(values));
            }
        }

        public CharacterSkillId SkillId { get; }
        public OperationHandle EntryOperation { get; }
        public ulong ActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong Generation { get; private set; }
        public IReadOnlyDictionary<int, TValue> Values => m_Values;

        public bool TryGetValue(int slotIndex, out TValue value) => m_Values.TryGetValue(slotIndex, out value);

        public void SetValue(int slotIndex, TValue value) => m_Values[slotIndex] = value;

        public void BindGeneration(ulong generation)
        {
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            if (Generation != 0 && Generation != generation)
                throw new InvalidOperationException($"Skill '{SkillId}' execution frame generation changed.");
            Generation = generation;
        }

        public GameplayAbilityExecutionFrame<TValue> Clone() =>
            new GameplayAbilityExecutionFrame<TValue>(this);

        GameplayAbilityExecutionFrame(GameplayAbilityExecutionFrame<TValue> source)
        {
            SkillId = source.SkillId;
            EntryOperation = source.EntryOperation;
            ActionInstanceId = source.ActionInstanceId;
            PredictionKey = source.PredictionKey;
            Generation = source.Generation;
            m_Values = new SortedDictionary<int, TValue>();
            foreach (KeyValuePair<int, TValue> value in source.m_Values)
                m_Values.Add(value.Key, value.Value);
        }

        public bool Equals(GameplayAbilityExecutionFrame<TValue> other)
        {
            if (other == null || m_Values.Count != other.m_Values.Count)
                return false;
            foreach (KeyValuePair<int, TValue> value in m_Values)
            {
                if (!other.m_Values.TryGetValue(value.Key, out TValue otherValue) ||
                    !value.Value.Equals(otherValue))
                    return false;
            }
            return true;
        }
    }

    internal sealed class GameplayAbilityExecutionAggregate<TValue>
        where TValue : struct, IEquatable<TValue>
    {
        readonly List<GameplayAbilityExecutionFrame<TValue>> m_Frames;

        public GameplayAbilityExecutionAggregate(
            IReadOnlyList<GameplayAbilityExecutionFrame<TValue>> frames = null)
        {
            m_Frames = new List<GameplayAbilityExecutionFrame<TValue>>(frames?.Count ?? 0);
            if (frames != null)
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    GameplayAbilityExecutionFrame<TValue> frame = frames[i];
                    if (frame == null || Find(frame.ActionInstanceId) != null)
                        throw new ArgumentException("Skill execution state frames are invalid or duplicated.", nameof(frames));
                    m_Frames.Add(frame.Clone());
                }
            }
        }

        public IReadOnlyList<GameplayAbilityExecutionFrame<TValue>> Frames => m_Frames;

        public GameplayAbilityExecutionFrame<TValue> Find(ulong actionInstanceId)
        {
            for (int i = 0; i < m_Frames.Count; i++)
                if (m_Frames[i].ActionInstanceId == actionInstanceId)
                    return m_Frames[i];
            return null;
        }

        public void Add(GameplayAbilityExecutionFrame<TValue> frame)
        {
            if (frame == null || Find(frame.ActionInstanceId) != null)
                throw new ArgumentException("Skill execution state frame is invalid or duplicated.", nameof(frame));
            m_Frames.Add(frame);
        }

        public bool Remove(ulong actionInstanceId)
        {
            for (int i = 0; i < m_Frames.Count; i++)
            {
                if (m_Frames[i].ActionInstanceId != actionInstanceId)
                    continue;
                m_Frames.RemoveAt(i);
                return true;
            }
            return false;
        }

        public GameplayAbilityExecutionAggregate<TValue> Clone() =>
            new GameplayAbilityExecutionAggregate<TValue>(m_Frames);

        internal GameplayAbilityExecutionAggregate<TValue> CreateMutableShell()
        {
            var frames = new List<GameplayAbilityExecutionFrame<TValue>>(m_Frames.Count);
            frames.AddRange(m_Frames);
            return Adopt(frames);
        }

        internal GameplayAbilityExecutionAggregate<TValue> CloneForActiveFrameMutation(ulong actionInstanceId)
        {
            int mutableIndex = -1;
            for (int i = 0; i < m_Frames.Count; i++)
            {
                if (m_Frames[i].ActionInstanceId == actionInstanceId)
                {
                    mutableIndex = i;
                    break;
                }
            }
            if (mutableIndex < 0)
                throw new InvalidOperationException($"Skill execution frame '{actionInstanceId}' is absent from the committed aggregate.");
            var frames = new List<GameplayAbilityExecutionFrame<TValue>>(m_Frames.Count);
            for (int i = 0; i < m_Frames.Count; i++)
                frames.Add(i == mutableIndex ? m_Frames[i].Clone() : m_Frames[i]);
            return Adopt(frames);
        }

        internal GameplayAbilityExecutionFrame<TValue> MakeFrameMutable(ulong actionInstanceId)
        {
            for (int i = 0; i < m_Frames.Count; i++)
            {
                if (m_Frames[i].ActionInstanceId != actionInstanceId)
                    continue;
                GameplayAbilityExecutionFrame<TValue> frame = m_Frames[i].Clone();
                m_Frames[i] = frame;
                return frame;
            }
            throw new InvalidOperationException($"Skill execution frame '{actionInstanceId}' is absent from the mutable aggregate.");
        }

        static GameplayAbilityExecutionAggregate<TValue> Adopt(List<GameplayAbilityExecutionFrame<TValue>> frames) =>
            new GameplayAbilityExecutionAggregate<TValue>(frames, true);

        GameplayAbilityExecutionAggregate(List<GameplayAbilityExecutionFrame<TValue>> frames, bool adopted)
        {
            m_Frames = frames;
        }

        public bool Equals(GameplayAbilityExecutionAggregate<TValue> other)
        {
            if (other == null || m_Frames.Count != other.m_Frames.Count)
                return false;
            for (int i = 0; i < m_Frames.Count; i++)
            {
                GameplayAbilityExecutionFrame<TValue> frame = m_Frames[i];
                GameplayAbilityExecutionFrame<TValue> otherFrame = other.Find(frame.ActionInstanceId);
                if (otherFrame == null || !otherFrame.Equals(frame))
                    return false;
            }
            return true;
        }
    }

    internal sealed class GameplayAbilityExecutionManager<TValue>
        where TValue : struct, IEquatable<TValue>
    {
        readonly IGameplayAbilityExecutionStorage<TValue> m_Storage;
        GameplayAbilityExecutionAggregate<TValue> m_States;
        bool m_StatesShared;
        GameplayAbilityExecutionFrame<TValue> m_Active;
        bool m_ActiveFrameCloned;
        readonly Scope m_Scope;

        public GameplayAbilityExecutionManager(IGameplayAbilityExecutionStorage<TValue> storage)
        {
            m_Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            m_Scope = new Scope(this);
        }

        public void BeginEvaluation()
        {
            if (m_Active != null)
                throw new InvalidOperationException("Skill execution state retained an active frame across evaluations.");
        }

        public void EndEvaluation()
        {
            if (m_Active != null)
                throw new InvalidOperationException("Skill execution state has an unclosed frame.");
            m_States = null;
            m_StatesShared = false;
        }

        public IDisposable Enter(AbilityExecutionContext identity)
        {
            if (!identity.IsValid)
                throw new ArgumentException("Skill execution frame identity is incomplete.", nameof(identity));
            if (m_Active != null)
                throw new InvalidOperationException("Skill execution frames cannot be nested.");
            EnsureStates();
            GameplayAbilityExecutionFrame<TValue> frame = m_States.Find(identity.ActionInstanceId);
            if (frame == null)
            {
                EnsureMutableShell();
                frame = new GameplayAbilityExecutionFrame<TValue>(
                    identity.SkillId,
                    identity.EntryOperation,
                    identity.ActionInstanceId,
                    identity.PredictionKey,
                    identity.Generation);
                m_States.Add(frame);
                m_Storage.WriteAggregate(m_States);
                m_ActiveFrameCloned = true;
            }
            else if (frame.SkillId != identity.SkillId ||
                     !frame.EntryOperation.Equals(identity.EntryOperation) ||
                     frame.PredictionKey != identity.PredictionKey ||
                     frame.Generation != 0 && identity.Generation != 0 &&
                     frame.Generation != identity.Generation)
            {
                throw new InvalidOperationException($"Skill execution frame does not match Action instance '{identity.ActionInstanceId}'.");
            }
            m_Active = frame;
            return m_Scope.Begin(frame);
        }

        public bool Remove(ulong actionInstanceId)
        {
            if (m_Active != null && m_Active.ActionInstanceId == actionInstanceId)
                throw new InvalidOperationException("Active Skill execution frame cannot be removed while active.");
            EnsureStates();
            if (m_States.Find(actionInstanceId) == null)
                return false;
            EnsureMutableShell();
            if (!m_States.Remove(actionInstanceId))
                return false;
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        public bool IsActive(ulong actionInstanceId) =>
            m_Active != null && m_Active.ActionInstanceId == actionInstanceId;

        public bool HasFrame(ulong actionInstanceId) =>
            m_Storage.ReadAggregate().Find(actionInstanceId) != null;

        public bool BindGeneration(ulong actionInstanceId, ulong generation)
        {
            if (m_Active == null || m_Active.ActionInstanceId != actionInstanceId)
                return false;
            MakeActiveFrameMutable();
            m_Active.BindGeneration(generation);
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        public bool TryGet(int slotIndex, out TValue value)
        {
            if (!m_Storage.IsAbilityStateSlot(slotIndex))
            {
                value = default;
                return false;
            }
            RequireActiveFrame(slotIndex);
            if (m_Active.TryGetValue(slotIndex, out value))
                return true;
            value = m_Storage.DefaultValue(slotIndex);
            return true;
        }

        public bool TrySet(int slotIndex, TValue value)
        {
            if (!m_Storage.IsAbilityStateSlot(slotIndex))
                return false;
            RequireActiveFrame(slotIndex);
            if (!m_Storage.IsValueValid(slotIndex, value))
                throw new InvalidOperationException($"Skill execution state slot '{slotIndex}' contains a value with the wrong kind.");
            MakeActiveFrameMutable();
            m_Active.SetValue(slotIndex, value);
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        public bool TryReset(int slotIndex)
        {
            if (!m_Storage.IsAbilityStateSlot(slotIndex))
                return false;
            RequireActiveFrame(slotIndex);
            MakeActiveFrameMutable();
            m_Active.SetValue(slotIndex, m_Storage.DefaultValue(slotIndex));
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        void EnsureStates()
        {
            if (m_States != null)
                return;
            m_States = m_Storage.ReadAggregate();
            m_StatesShared = true;
        }

        void EnsureMutableShell()
        {
            EnsureStates();
            if (!m_StatesShared)
                return;
            m_States = m_States.CreateMutableShell();
            m_StatesShared = false;
        }

        void MakeActiveFrameMutable()
        {
            RequireActiveFrame(0);
            EnsureStates();
            if (m_StatesShared)
            {
                m_States = m_States.CloneForActiveFrameMutation(m_Active.ActionInstanceId);
                m_StatesShared = false;
                m_ActiveFrameCloned = true;
            }
            if (!m_ActiveFrameCloned)
            {
                m_Active = m_States.MakeFrameMutable(m_Active.ActionInstanceId);
                m_ActiveFrameCloned = true;
            }
            else
            {
                m_Active = m_States.Find(m_Active.ActionInstanceId);
            }
            if (m_Active == null)
                throw new InvalidOperationException("Skill execution active frame disappeared while preparing state changes.");
        }

        void Exit(GameplayAbilityExecutionFrame<TValue> frame)
        {
            if (!ReferenceEquals(m_Active, frame))
                throw new InvalidOperationException("Skill execution frame scope is unbalanced.");
            if (frame.Generation == 0)
            {
                EnsureMutableShell();
                m_States.Remove(frame.ActionInstanceId);
            }
            m_Storage.WriteAggregate(m_States);
            m_Active = null;
            m_ActiveFrameCloned = false;
        }

        void RequireActiveFrame(int slotIndex)
        {
            if (m_Active == null)
                throw new InvalidOperationException($"Skill execution state slot '{slotIndex}' requires a bound Action instance.");
        }

        sealed class Scope : IDisposable
        {
            readonly GameplayAbilityExecutionManager<TValue> m_Owner;
            GameplayAbilityExecutionFrame<TValue> m_Frame;
            bool m_Disposed;

            public Scope(GameplayAbilityExecutionManager<TValue> owner)
            {
                m_Owner = owner;
            }

            public IDisposable Begin(GameplayAbilityExecutionFrame<TValue> frame)
            {
                m_Frame = frame;
                m_Disposed = false;
                return this;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Owner.Exit(m_Frame);
                m_Frame = null;
            }
        }
    }

    internal sealed class ActionSkillCommitFlow<TTargetSnapshot, TActionState>
        where TTargetSnapshot : struct
        where TActionState : struct
    {
        readonly IActionSkillCommitPort<TTargetSnapshot, TActionState> m_Port;

        public ActionSkillCommitFlow(IActionSkillCommitPort<TTargetSnapshot, TActionState> port)
        {
            m_Port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public ulong Commit(
            ActionSkillActivationRequest<TTargetSnapshot> request,
            ActionAdmissionProfile profile)
        {
            ulong instanceId = m_Port.NextActionInstanceId();
            ulong predictionKey = m_Port.NextPredictionKey();
            TActionState action = m_Port.CreatePredictedAction(request, instanceId, predictionKey);
            try
            {
                m_Port.WriteAction(action);
                m_Port.SetActionTags(instanceId, profile.Tags);
                m_Port.EmitActionFact(request.Source, action);
                return instanceId;
            }
            finally
            {
                m_Port.ClearRequest(request);
            }
        }
    }

    internal sealed class ActionSkillActivationFlow<TTargetSnapshot, TOperation, TActionState>
        where TTargetSnapshot : struct
        where TOperation : class
        where TActionState : struct
    {
        readonly ActionAdmissionControl m_Admission;
        readonly IActionSkillActivationPort<TTargetSnapshot, TOperation> m_Port;
        readonly ActionSkillCommitFlow<TTargetSnapshot, TActionState> m_Commit;

        public ActionSkillActivationFlow(
            ActionAdmissionControl admission,
            IActionSkillActivationPort<TTargetSnapshot, TOperation> port,
            ActionSkillCommitFlow<TTargetSnapshot, TActionState> commit)
        {
            m_Admission = admission ?? throw new ArgumentNullException(nameof(admission));
            m_Port = port ?? throw new ArgumentNullException(nameof(port));
            m_Commit = commit ?? throw new ArgumentNullException(nameof(commit));
        }

        public bool ActivateImmediate(
            ActionSkillActivationCandidate<TTargetSnapshot, TOperation> candidate,
            ActionAdmissionProfile profile)
        {
            TTargetSnapshot targetSnapshot = NormalizeTarget(profile, candidate.TargetSnapshot);
            ActionAdmissionDecision admission = Evaluate(
                profile,
                targetSnapshot,
                ActionAdmissionEvaluationMode.CommitActivation);
            if (!admission.Allowed)
            {
                if (!m_Port.TraceEnabled)
                    return false;
                string detail =
                    $"{profile.ActionId}:{admission.RejectReason}:{admission.ActiveSourceActionId}:{admission.ActiveSourceActionInstanceId}";
                Trace(
                    candidate,
                    "action_activation_rejected",
                    ActionSkillTraceSeverity.Information,
                    detail);
                TraceActionResult(
                    candidate,
                    profile.ActionId,
                    candidate.SkillId,
                    0,
                    InputSequenceFor(candidate),
                    SimulationActionResultKind.Rejected,
                    detail);
                return false;
            }
            if (!TryCreateRequest(candidate, profile, targetSnapshot, out ActionSkillActivationRequest<TTargetSnapshot> request))
            {
                TraceActionResult(
                    candidate,
                    profile.ActionId,
                    candidate.SkillId,
                    0,
                    InputSequenceFor(candidate),
                    SimulationActionResultKind.Rejected,
                    "InputRequestUnavailable");
                return false;
            }
            m_Port.StageRequest(request);
            ulong instanceId = m_Commit.Commit(request, profile);
            TraceActivated(candidate, request, profile, instanceId);
            TraceActionResult(
                candidate,
                request.ActionId,
                request.SkillId,
                instanceId,
                request.InputSequence,
                SimulationActionResultKind.Accepted,
                string.Empty);
            return true;
        }

        public ActionAdmissionDecision Preview(
            TOperation operation,
            ActionAdmissionProfile profile,
            TTargetSnapshot targetSnapshot,
            ulong executingActionInstanceId)
        {
            targetSnapshot = NormalizeTarget(profile, targetSnapshot);
            ActionAdmissionDecision decision = Evaluate(
                profile,
                targetSnapshot,
                ActionAdmissionEvaluationMode.PreviewReplacement,
                executingActionInstanceId: executingActionInstanceId);
            if (m_Port.TraceEnabled)
            {
                m_Port.Trace(
                    operation,
                    "action_admission_preview",
                    ActionSkillTraceSeverity.Detail,
                    $"{profile.ActionId}:{decision.Allowed}:{decision.RejectReason}:{decision.ActiveSourceActionId}:{decision.ActiveSourceActionInstanceId}");
            }
            return decision;
        }

        public bool ActivateFromControl(
            CharacterControlAbilityRequest controlRequest,
            GameplayAbilityExecutionBinding skill,
            ActionAdmissionProfile profile)
        {
            string requestId = string.IsNullOrEmpty(controlRequest.SourceInputRequestId)
                ? skill.SourceInputRequestId
                : controlRequest.SourceInputRequestId;
            bool consumeRequest = string.IsNullOrEmpty(controlRequest.SourceInputRequestId)
                ? skill.ConsumeSourceInputRequest
                : controlRequest.ConsumeSourceInputRequest;
            string targetInputValueId = string.IsNullOrEmpty(controlRequest.TargetInputValueId)
                ? skill.TargetInputValueId
                : controlRequest.TargetInputValueId;
            TTargetSnapshot targetSnapshot = profile.TargetRequirement == ActionTargetRequirement.None ||
                string.IsNullOrEmpty(targetInputValueId)
                ? m_Port.NoneTarget
                : m_Port.ReadTargetSnapshot(targetInputValueId);
            return ActivatePending(
                new ActionSkillActivationCandidate<TTargetSnapshot, TOperation>(
                    skill.SkillId,
                    skill.EntryOperation,
                    skill.ActionContextId,
                    requestId,
                    consumeRequest,
                    string.IsNullOrEmpty(controlRequest.TargetKey) ? skill.TargetKey : controlRequest.TargetKey,
                    targetSnapshot,
                    controlRequest.Source,
                    controlRequest.EquipmentContext,
                    replacementActionInstanceId: controlRequest.ReplacementActionInstanceId,
                    activationEntryId: controlRequest.ActivationEntryId),
                profile);
        }

        public bool TryCommitPendingControl(
            CharacterSkillId skillId,
            ActionAdmissionProfile profile)
        {
            if (!m_Port.TryReadPendingRequest(skillId, out ActionSkillActivationRequest<TTargetSnapshot> request))
                return false;
            if (request.ReplacementActionInstanceId != 0 &&
                !m_Port.IsActionInstanceStopComplete(request.ReplacementActionInstanceId))
                return false;
            ActionAdmissionDecision admission = Evaluate(
                profile,
                request.TargetSnapshot,
                ActionAdmissionEvaluationMode.CommitActivation,
                0);
            if (!admission.Allowed)
            {
                if (admission.RejectReason == ActionAdmissionRejectReason.SourceActionStillActive)
                    return false;
                m_Port.ClearPendingRequest(request);
                if (!m_Port.TraceEnabled)
                    return false;
                string detail =
                    $"{skillId}:{admission.RejectReason}:{admission.ActiveSourceActionId}:{admission.ActiveSourceActionInstanceId}";
                Trace(
                    request.Source,
                    "action_activation_rejected",
                    ActionSkillTraceSeverity.Information,
                    detail);
                m_Port.TraceActionResult(
                    request.Source,
                    profile.ActionId,
                    request.SkillId,
                    0,
                    request.InputSequence,
                    SimulationActionResultKind.Rejected,
                    detail);
                return false;
            }
            ulong instanceId = m_Commit.Commit(request, profile);
            TraceActivated(request.Source, request, profile, instanceId);
            m_Port.TraceActionResult(
                request.Source,
                request.ActionId,
                request.SkillId,
                instanceId,
                request.InputSequence,
                SimulationActionResultKind.Accepted,
                string.Empty);
            return true;
        }

        bool ActivatePending(
            ActionSkillActivationCandidate<TTargetSnapshot, TOperation> candidate,
            ActionAdmissionProfile profile)
        {
            TTargetSnapshot targetSnapshot = NormalizeTarget(profile, candidate.TargetSnapshot);
            ActionAdmissionDecision admission = Evaluate(
                profile,
                targetSnapshot,
                ActionAdmissionEvaluationMode.CommitActivation,
                candidate.ReplacementActionInstanceId);
            bool replacementPending = false;
            if (!admission.Allowed)
            {
                if (admission.RejectReason != ActionAdmissionRejectReason.SourceActionStillActive)
                {
                    if (!m_Port.TraceEnabled)
                        return false;
                    string detail =
                        $"{profile.ActionId}:{admission.RejectReason}:{admission.ActiveSourceActionId}:{admission.ActiveSourceActionInstanceId}";
                    Trace(
                        candidate,
                        "action_activation_rejected",
                        ActionSkillTraceSeverity.Information,
                        detail);
                    TraceActionResult(
                        candidate,
                        profile.ActionId,
                        candidate.SkillId,
                        0,
                        InputSequenceFor(candidate),
                        SimulationActionResultKind.Rejected,
                        detail);
                    return false;
                }
                ActionAdmissionDecision replacement = Evaluate(
                    profile,
                    targetSnapshot,
                    ActionAdmissionEvaluationMode.PreviewReplacement,
                    candidate.ReplacementActionInstanceId);
                if (!replacement.Allowed)
                {
                    if (!m_Port.TraceEnabled)
                        return false;
                    string detail =
                        $"{profile.ActionId}:{replacement.RejectReason}:{replacement.ActiveSourceActionId}:{replacement.ActiveSourceActionInstanceId}";
                    Trace(
                        candidate,
                        "action_replacement_rejected",
                        ActionSkillTraceSeverity.Information,
                        detail);
                    TraceActionResult(
                        candidate,
                        profile.ActionId,
                        candidate.SkillId,
                        0,
                        InputSequenceFor(candidate),
                        SimulationActionResultKind.Rejected,
                        detail);
                    return false;
                }
                m_Port.InterruptActive(
                    candidate.ReplacementActionInstanceId,
                    candidate.Source,
                    "SkillReplacement");
                replacementPending = true;
            }
            if (!TryCreateRequest(candidate, profile, targetSnapshot, out ActionSkillActivationRequest<TTargetSnapshot> request))
            {
                TraceActionResult(
                    candidate,
                    profile.ActionId,
                    candidate.SkillId,
                    0,
                    InputSequenceFor(candidate),
                    SimulationActionResultKind.Rejected,
                    "InputRequestUnavailable");
                return false;
            }
            if (m_Port.HasPendingRequest(profile.ActionId))
                throw new InvalidOperationException($"Action '{profile.ActionId}' already has a pending activation request.");
            m_Port.StageRequest(request);
            return !replacementPending;
        }

        ActionAdmissionDecision Evaluate(
            ActionAdmissionProfile profile,
            TTargetSnapshot targetSnapshot,
            ActionAdmissionEvaluationMode mode,
            ulong replacementActionInstanceId = 0,
            ulong executingActionInstanceId = 0)
        {
            return m_Admission.Evaluate(new ActionAdmissionRequest(
                profile,
                new ActionAdmissionTargetCandidate(m_Port.TargetId(targetSnapshot)),
                mode,
                replacementActionInstanceId,
                executingActionInstanceId));
        }

        bool TryCreateRequest(
            ActionSkillActivationCandidate<TTargetSnapshot, TOperation> candidate,
            ActionAdmissionProfile profile,
            TTargetSnapshot targetSnapshot,
            out ActionSkillActivationRequest<TTargetSnapshot> request)
        {
            ulong inputSequence = m_Port.InputSequence;
            if (!string.IsNullOrEmpty(candidate.SourceInputRequestId))
            {
                if (!m_Port.TryReadInputSequence(candidate.SourceInputRequestId, out inputSequence))
                {
                    if (!m_Port.TraceEnabled)
                    {
                        request = default;
                        return false;
                    }
                    Trace(
                        candidate,
                        "action_request_unavailable",
                        ActionSkillTraceSeverity.Detail,
                        $"{candidate.SourceInputRequestId}:{m_Port.Tick}");
                    request = default;
                    return false;
                }
                if (candidate.ConsumeSourceInputRequest)
                    m_Port.ClearInputRequest(candidate.SourceInputRequestId);
            }
            request = new ActionSkillActivationRequest<TTargetSnapshot>(
                profile.ActionId,
                candidate.SkillId,
                candidate.SkillEntryOperation,
                candidate.ContextId,
                candidate.SourceInputRequestId,
                inputSequence,
                m_Port.Tick,
                candidate.TargetKey,
                targetSnapshot,
                candidate.Source,
                candidate.EquipmentContext,
                candidate.ReplacementActionInstanceId,
                candidate.ActivationEntryId);
            return true;
        }

        TTargetSnapshot NormalizeTarget(
            ActionAdmissionProfile profile,
            TTargetSnapshot targetSnapshot)
        {
            return profile.TargetRequirement == ActionTargetRequirement.None
                ? m_Port.NoneTarget
                : targetSnapshot;
        }

        void Trace(
            ActionSkillActivationCandidate<TTargetSnapshot, TOperation> candidate,
            string code,
            ActionSkillTraceSeverity severity,
            string detail)
        {
            if (!m_Port.TraceEnabled)
                return;
            if (candidate.Operation != null)
                m_Port.Trace(candidate.Operation, code, severity, detail);
            else
                m_Port.Trace(candidate.Source, code, severity, detail);
        }

        void Trace(
            SimulationExecutionSource source,
            string code,
            ActionSkillTraceSeverity severity,
            string detail)
        {
            if (m_Port.TraceEnabled)
                m_Port.Trace(source, code, severity, detail);
        }

        ulong InputSequenceFor(ActionSkillActivationCandidate<TTargetSnapshot, TOperation> candidate)
        {
            if (!string.IsNullOrEmpty(candidate.SourceInputRequestId) &&
                m_Port.TryReadInputSequence(candidate.SourceInputRequestId, out ulong sequence))
                return sequence;
            return m_Port.InputSequence;
        }

        void TraceActionResult(
            ActionSkillActivationCandidate<TTargetSnapshot, TOperation> candidate,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason)
        {
            if (!m_Port.TraceEnabled)
                return;
            if (candidate.Operation != null)
                m_Port.TraceActionResult(
                    candidate.Operation,
                    actionId,
                    skillId,
                    actionInstanceId,
                    inputSequence,
                    result,
                    reason);
            else
                m_Port.TraceActionResult(
                    candidate.Source,
                    actionId,
                    skillId,
                    actionInstanceId,
                    inputSequence,
                    result,
                    reason);
        }

        void TraceActivated(
            ActionSkillActivationCandidate<TTargetSnapshot, TOperation> candidate,
            ActionSkillActivationRequest<TTargetSnapshot> request,
            ActionAdmissionProfile profile,
            ulong instanceId)
        {
            TraceActivated(request.Source, request, profile, instanceId, candidate.Operation);
        }

        void TraceActivated(
            SimulationExecutionSource source,
            ActionSkillActivationRequest<TTargetSnapshot> request,
            ActionAdmissionProfile profile,
            ulong instanceId,
            TOperation operation = null)
        {
            if (!m_Port.TraceEnabled)
                return;
            string detail =
                $"{request.ActionId}:{instanceId}:request={request.SourceInputRequestId}:sequence={request.InputSequence}:requirement={profile.TargetRequirement}:candidate={m_Port.TargetId(request.TargetSnapshot)}:captured={m_Port.TargetId(request.TargetSnapshot)}:captureTick={request.StartTick}:{m_Port.FormatTarget(request.TargetSnapshot)}:equipment={request.EquipmentContext}";
            if (operation != null)
                m_Port.Trace(operation, "action_activated", ActionSkillTraceSeverity.Information, detail);
            else
                m_Port.Trace(source, "action_activated", ActionSkillTraceSeverity.Information, detail);
        }
    }

    internal sealed class AbilityExecution<TActionState>
        where TActionState : struct
    {
        readonly IAbilityLifecyclePort<TActionState> m_Port;

        public AbilityExecution(IAbilityLifecyclePort<TActionState> port)
        {
            m_Port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public bool Submit(
            string contextId,
            int transitionValue,
            string reason,
            SimulationExecutionSource source)
        {
            if (!m_Port.TryFindActive(contextId, out TActionState action))
                return false;
            Resolve(source, action, RequireTransition(transitionValue), reason, 0);
            return true;
        }

        public void ApplyIngress(AbilityLifecycleIngress ingress)
        {
            TActionState match = default;
            int matches = 0;
            foreach (TActionState action in m_Port.ActionStates)
            {
                if (!m_Port.IsActive(action) || !Matches(action, ingress))
                    continue;
                match = action;
                matches++;
            }
            if (matches != 1)
                throw new InvalidOperationException($"Action lifecycle ingress '{ingress.Identity}' matched {matches} active Action instances.");
            Resolve(
                m_Port.Source(match),
                match,
                RequireTransition(ingress.TransitionValue),
                ingress.Reason,
                ingress.SourceTick);
        }

        public void Resolve(
            CharacterSkillId skillId,
            SimulationExecutionSource source,
            AbilityLifecycleTransition transition,
            string reason)
        {
            if (!m_Port.TryFindActive(skillId, out TActionState action))
                return;
            Resolve(source, action, transition, reason, 0);
        }

        public void Resolve(
            TActionState action,
            SimulationExecutionSource source,
            AbilityLifecycleTransition transition,
            string reason)
        {
            if (!m_Port.IsActive(action))
                return;
            Resolve(source, action, transition, reason, 0);
        }

        public void Resolve(
            CharacterSkillId skillId,
            GameplayAbilityExecutionBinding binding,
            string trigger,
            SimulationExecutionSource source,
            AbilityLifecycleTransition defaultTransition,
            string reason,
            string actionWindowType = "")
        {
            if (!m_Port.TryFindActive(skillId, out TActionState action))
                return;
            Resolve(action, binding, trigger, source, defaultTransition, reason, actionWindowType);
        }

        public void Resolve(
            TActionState action,
            GameplayAbilityExecutionBinding binding,
            string trigger,
            SimulationExecutionSource source,
            AbilityLifecycleTransition defaultTransition,
            string reason,
            string actionWindowType = "")
        {
            AbilityLifecycleTransition transition = defaultTransition;
            string resolvedReason = reason ?? string.Empty;
            if (binding != null && binding.TryGetEndRule(trigger, actionWindowType, out GameplayAbilityExecutionEndRule rule))
            {
                transition = RequireTerminalTransition(rule.Transition);
                if (!string.IsNullOrEmpty(rule.Reason) &&
                    (string.IsNullOrEmpty(resolvedReason) ||
                     string.Equals(trigger, GameplayAbilityEndTriggerNames.ExecutionCompleted, StringComparison.Ordinal)))
                    resolvedReason = rule.Reason;
            }
            Resolve(source, action, transition, resolvedReason, 0);
        }

        public void Stop(
            CharacterSkillId skillId,
            CharacterControlAbilityStopMode mode,
            SimulationExecutionSource source,
            string reason)
        {
            if (!m_Port.TryFindActive(skillId, out TActionState action))
                return;
            Resolve(
                source,
                action,
                mode == CharacterControlAbilityStopMode.Force
                    ? AbilityLifecycleTransition.Abort
                    : AbilityLifecycleTransition.Cancel,
                reason,
                0);
        }

        public void Stop(
            TActionState action,
            CharacterControlAbilityStopMode mode,
            SimulationExecutionSource source,
            string reason)
        {
            if (!m_Port.IsActive(action))
                return;
            Resolve(
                source,
                action,
                mode == CharacterControlAbilityStopMode.Force
                    ? AbilityLifecycleTransition.Abort
                    : AbilityLifecycleTransition.Cancel,
                reason,
                0);
        }

        public void Interrupt(
            TActionState action,
            SimulationExecutionSource source,
            string reason)
        {
            Resolve(source, action, AbilityLifecycleTransition.Interrupt, reason, 0);
        }

        void Resolve(
            SimulationExecutionSource source,
            TActionState action,
            AbilityLifecycleTransition transition,
            string reason,
            ulong sourceTick)
        {
            if (!m_Port.IsActive(action))
                throw new InvalidOperationException($"Action '{m_Port.ActionId(action)}/{m_Port.InstanceId(action)}' is not active.");
            using (m_Port.EnterExecution(action))
            {
            AbilityLifecycleState previousState = m_Port.State(action);
            if (transition == AbilityLifecycleTransition.Confirm &&
                previousState != AbilityLifecycleState.Predicted &&
                previousState != AbilityLifecycleState.Corrected)
            {
                throw new InvalidOperationException($"Action '{m_Port.ActionId(action)}/{m_Port.InstanceId(action)}' cannot confirm from '{previousState}'.");
            }

            AbilityLifecyclePhase phase = m_Port.Phase(action);
            AbilityLifecycleState state = previousState;
            string nextReason = reason ?? string.Empty;
            switch (transition)
            {
                case AbilityLifecycleTransition.Confirm:
                    state = AbilityLifecycleState.Confirmed;
                    nextReason = string.Empty;
                    break;
                case AbilityLifecycleTransition.Complete:
                    phase = AbilityLifecyclePhase.Ended;
                    state = AbilityLifecycleState.Ended;
                    break;
                case AbilityLifecycleTransition.Cancel:
                    phase = AbilityLifecyclePhase.Cancel;
                    state = AbilityLifecycleState.Cancelled;
                    break;
                case AbilityLifecycleTransition.Interrupt:
                    phase = AbilityLifecyclePhase.Cancel;
                    state = AbilityLifecycleState.Interrupted;
                    break;
                case AbilityLifecycleTransition.Reject:
                    phase = AbilityLifecyclePhase.Ended;
                    state = AbilityLifecycleState.Rejected;
                    break;
                case AbilityLifecycleTransition.Correct:
                    state = AbilityLifecycleState.Corrected;
                    break;
                case AbilityLifecycleTransition.Abort:
                    phase = AbilityLifecyclePhase.Ended;
                    state = AbilityLifecycleState.Aborted;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transition));
            }

            TActionState next = m_Port.WithLifecycle(
                action,
                new AbilityLifecycleUpdate(
                    phase,
                    state,
                    transition,
                    m_Port.Tick,
                    sourceTick,
                    nextReason));
            m_Port.WriteState(next);
            m_Port.EmitActionFact(source, next);
            SimulationActionResultKind result = ResolveResult(transition);
            if (result != SimulationActionResultKind.None)
            {
                m_Port.TraceActionResult(
                    source,
                    m_Port.ActionId(next),
                    m_Port.SkillId(next),
                    m_Port.InstanceId(next),
                    m_Port.InputSequence(next),
                    result,
                    nextReason);
            }
            if (m_Port.TraceEnabled)
            {
                m_Port.Trace(
                    source,
                    "action_lifecycle",
                    ActionSkillTraceSeverity.Information,
                    $"{m_Port.ActionId(next)}:{m_Port.InstanceId(next)}:{transition}:{m_Port.Reason(next)}:equipment={m_Port.EquipmentContext(next)}",
                    m_Port.SourceGeneration(source));
            }
            }
        }

        bool Matches(TActionState action, AbilityLifecycleIngress ingress) =>
            (ingress.ActionInstanceId == 0 || ingress.ActionInstanceId == m_Port.InstanceId(action)) &&
            (ingress.PredictionKey == 0 || ingress.PredictionKey == m_Port.PredictionKey(action)) &&
            (ingress.InputSequence == 0 || ingress.InputSequence == m_Port.InputSequence(action));

        static AbilityLifecycleTransition RequireTransition(int value)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
                throw new InvalidOperationException($"Action lifecycle transition '{value}' is invalid.");
            var transition = (AbilityLifecycleTransition)(byte)value;
            if (transition < AbilityLifecycleTransition.Confirm || transition > AbilityLifecycleTransition.Abort)
                throw new InvalidOperationException($"Action lifecycle transition '{value}' is invalid.");
            return transition;
        }

        static AbilityLifecycleTransition RequireTerminalTransition(int value)
        {
            AbilityLifecycleTransition transition = RequireTransition(value);
            if (transition != AbilityLifecycleTransition.Complete &&
                transition != AbilityLifecycleTransition.Cancel &&
                transition != AbilityLifecycleTransition.Interrupt &&
                transition != AbilityLifecycleTransition.Abort)
                throw new InvalidOperationException($"Action end rule transition '{value}' is not terminal.");
            return transition;
        }

        static SimulationActionResultKind ResolveResult(AbilityLifecycleTransition transition)
        {
            return transition switch
            {
                AbilityLifecycleTransition.Complete => SimulationActionResultKind.Completed,
                AbilityLifecycleTransition.Cancel => SimulationActionResultKind.Cancelled,
                AbilityLifecycleTransition.Interrupt => SimulationActionResultKind.Interrupted,
                AbilityLifecycleTransition.Reject => SimulationActionResultKind.Rejected,
                AbilityLifecycleTransition.Correct => SimulationActionResultKind.Corrected,
                AbilityLifecycleTransition.Abort => SimulationActionResultKind.Aborted,
                _ => SimulationActionResultKind.None
            };
        }

    }
}
