using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal static class ActionSkillExecutionSlotMap
    {
        public static HashSet<int> Build(
            int operationCount,
            IReadOnlyList<ProgramControlFlowEdge> controlFlow,
            IReadOnlyList<ProgramScopeLayout> scopes,
            CharacterSkillProgramCatalog skills,
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
                owner == ProgramStateOwnerKind.MotionModifier ||
                owner == ProgramStateOwnerKind.Blackboard)
                result.Add(slotIndex);
        }
    }

    internal sealed class ActionSkillExecutionFrame<TValue>
        where TValue : struct
    {
        readonly SortedDictionary<int, TValue> m_Values;

        public ActionSkillExecutionFrame(
            CharacterSkillId skillId,
            OperationHandle entryOperation,
            ulong actionInstanceId,
            ulong predictionKey,
            ulong generation,
            IEnumerable<KeyValuePair<int, TValue>> values = null)
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

        public ActionSkillExecutionFrame<TValue> Clone() =>
            new ActionSkillExecutionFrame<TValue>(
                SkillId,
                EntryOperation,
                ActionInstanceId,
                PredictionKey,
                Generation,
                m_Values);
    }

    internal sealed class ActionSkillExecutionAggregate<TValue>
        where TValue : struct
    {
        readonly List<ActionSkillExecutionFrame<TValue>> m_Frames;
        readonly System.Collections.ObjectModel.ReadOnlyCollection<ActionSkillExecutionFrame<TValue>> m_ReadOnlyFrames;

        public ActionSkillExecutionAggregate(
            IEnumerable<ActionSkillExecutionFrame<TValue>> frames = null)
        {
            m_Frames = new List<ActionSkillExecutionFrame<TValue>>();
            if (frames != null)
            {
                foreach (ActionSkillExecutionFrame<TValue> frame in frames)
                {
                    if (frame == null || Find(frame.ActionInstanceId) != null)
                        throw new ArgumentException("Skill execution state frames are invalid or duplicated.", nameof(frames));
                    m_Frames.Add(frame.Clone());
                }
            }
            m_ReadOnlyFrames = m_Frames.AsReadOnly();
        }

        public IReadOnlyList<ActionSkillExecutionFrame<TValue>> Frames => m_ReadOnlyFrames;

        public ActionSkillExecutionFrame<TValue> Find(ulong actionInstanceId)
        {
            for (int i = 0; i < m_Frames.Count; i++)
                if (m_Frames[i].ActionInstanceId == actionInstanceId)
                    return m_Frames[i];
            return null;
        }

        public void Add(ActionSkillExecutionFrame<TValue> frame)
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

        public ActionSkillExecutionAggregate<TValue> Clone() =>
            new ActionSkillExecutionAggregate<TValue>(m_Frames);
    }

    internal sealed class ActionSkillExecutionManager<TValue>
        where TValue : struct
    {
        readonly IActionSkillExecutionStorage<TValue> m_Storage;
        ActionSkillExecutionAggregate<TValue> m_States;
        ActionSkillExecutionFrame<TValue> m_Active;

        public ActionSkillExecutionManager(IActionSkillExecutionStorage<TValue> storage)
        {
            m_Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void BeginEvaluation()
        {
            if (m_Active != null)
                throw new InvalidOperationException("Skill execution state retained an active frame across evaluations.");
            m_States = null;
        }

        public void EndEvaluation()
        {
            if (m_Active != null)
                throw new InvalidOperationException("Skill execution state has an unclosed frame.");
            m_States = null;
        }

        public IDisposable Enter(ActionSkillExecutionIdentity identity)
        {
            if (!identity.IsValid)
                throw new ArgumentException("Skill execution frame identity is incomplete.", nameof(identity));
            if (m_Active != null)
                throw new InvalidOperationException("Skill execution frames cannot be nested.");
            EnsureStates();
            ActionSkillExecutionFrame<TValue> frame = m_States.Find(identity.ActionInstanceId);
            if (frame == null)
            {
                frame = new ActionSkillExecutionFrame<TValue>(
                    identity.SkillId,
                    identity.EntryOperation,
                    identity.ActionInstanceId,
                    identity.PredictionKey,
                    identity.Generation);
                m_States.Add(frame);
                m_Storage.WriteAggregate(m_States);
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
            return new Scope(this, frame);
        }

        public bool Remove(ulong actionInstanceId)
        {
            if (m_Active != null)
                throw new InvalidOperationException("Skill execution frame cannot be removed while active.");
            EnsureStates();
            if (!m_States.Remove(actionInstanceId))
                return false;
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        public bool IsActive(ulong actionInstanceId) =>
            m_Active != null && m_Active.ActionInstanceId == actionInstanceId;

        public bool BindGeneration(ulong actionInstanceId, ulong generation)
        {
            if (m_Active == null || m_Active.ActionInstanceId != actionInstanceId)
                return false;
            m_Active.BindGeneration(generation);
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        public bool TryGet(int slotIndex, out TValue value)
        {
            if (!m_Storage.IsSkillStateSlot(slotIndex))
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
            if (!m_Storage.IsSkillStateSlot(slotIndex))
                return false;
            RequireActiveFrame(slotIndex);
            if (!m_Storage.IsValueValid(slotIndex, value))
                throw new InvalidOperationException($"Skill execution state slot '{slotIndex}' contains a value with the wrong kind.");
            m_Active.SetValue(slotIndex, value);
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        public bool TryReset(int slotIndex)
        {
            if (!m_Storage.IsSkillStateSlot(slotIndex))
                return false;
            RequireActiveFrame(slotIndex);
            m_Active.SetValue(slotIndex, m_Storage.DefaultValue(slotIndex));
            m_Storage.WriteAggregate(m_States);
            return true;
        }

        void EnsureStates()
        {
            if (m_States != null)
                return;
            m_States = m_Storage.ReadAggregate().Clone();
            m_Storage.WriteAggregate(m_States);
        }

        void Exit(ActionSkillExecutionFrame<TValue> frame)
        {
            if (!ReferenceEquals(m_Active, frame))
                throw new InvalidOperationException("Skill execution frame scope is unbalanced.");
            if (frame.Generation == 0)
                m_States.Remove(frame.ActionInstanceId);
            m_Storage.WriteAggregate(m_States);
            m_Active = null;
        }

        void RequireActiveFrame(int slotIndex)
        {
            if (m_Active == null)
                throw new InvalidOperationException($"Skill execution state slot '{slotIndex}' requires a bound Action instance.");
        }

        sealed class Scope : IDisposable
        {
            readonly ActionSkillExecutionManager<TValue> m_Owner;
            readonly ActionSkillExecutionFrame<TValue> m_Frame;
            bool m_Disposed;

            public Scope(ActionSkillExecutionManager<TValue> owner, ActionSkillExecutionFrame<TValue> frame)
            {
                m_Owner = owner;
                m_Frame = frame;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Owner.Exit(m_Frame);
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
                m_Port.ClearRequest(request);
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
                Trace(
                    candidate,
                    "action_activation_rejected",
                    ActionSkillTraceSeverity.Information,
                    $"{profile.ActionId}:{admission.RejectReason}:{admission.ActiveSourceActionId}");
                return false;
            }
            if (!TryCreateRequest(candidate, profile, targetSnapshot, out ActionSkillActivationRequest<TTargetSnapshot> request))
                return false;
            m_Port.StageRequest(request);
            ulong instanceId = m_Commit.Commit(request, profile);
            TraceActivated(candidate, request, profile, instanceId);
            return true;
        }

        public ActionAdmissionDecision Preview(
            TOperation operation,
            ActionAdmissionProfile profile,
            TTargetSnapshot targetSnapshot)
        {
            targetSnapshot = NormalizeTarget(profile, targetSnapshot);
            ActionAdmissionDecision decision = Evaluate(
                profile,
                targetSnapshot,
                ActionAdmissionEvaluationMode.PreviewReplacement);
            if (m_Port.TraceEnabled)
            {
                m_Port.Trace(
                    operation,
                    "action_admission_preview",
                    ActionSkillTraceSeverity.Detail,
                    $"{profile.ActionId}:{decision.Allowed}:{decision.RejectReason}:{decision.ActiveSourceActionId}");
            }
            return decision;
        }

        public bool ActivateFromControl(
            CharacterControlSkillRequest controlRequest,
            CharacterSkillProgramBinding skill,
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
                    default),
                profile);
        }

        public bool TryCommitPendingControl(
            CharacterSkillId skillId,
            ActionAdmissionProfile profile)
        {
            if (!m_Port.TryReadPendingRequest(skillId, out ActionSkillActivationRequest<TTargetSnapshot> request))
                return false;
            ActionAdmissionDecision admission = Evaluate(
                profile,
                request.TargetSnapshot,
                ActionAdmissionEvaluationMode.CommitActivation);
            if (!admission.Allowed)
            {
                if (admission.RejectReason == ActionAdmissionRejectReason.SourceActionStillActive)
                    return false;
                m_Port.ClearPendingRequest(request);
                Trace(
                    request.Source,
                    "action_activation_rejected",
                    ActionSkillTraceSeverity.Information,
                    $"{skillId}:{admission.RejectReason}:{admission.ActiveSourceActionId}");
                return false;
            }
            ulong instanceId = m_Commit.Commit(request, profile);
            TraceActivated(request.Source, request, profile, instanceId);
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
                ActionAdmissionEvaluationMode.CommitActivation);
            bool replacementPending = false;
            if (!admission.Allowed)
            {
                if (admission.RejectReason != ActionAdmissionRejectReason.SourceActionStillActive)
                {
                    Trace(
                        candidate,
                        "action_activation_rejected",
                        ActionSkillTraceSeverity.Information,
                        $"{profile.ActionId}:{admission.RejectReason}:{admission.ActiveSourceActionId}");
                    return false;
                }
                ActionAdmissionDecision replacement = Evaluate(
                    profile,
                    targetSnapshot,
                    ActionAdmissionEvaluationMode.PreviewReplacement);
                if (!replacement.Allowed)
                {
                    Trace(
                        candidate,
                        "action_replacement_rejected",
                        ActionSkillTraceSeverity.Information,
                        $"{profile.ActionId}:{replacement.RejectReason}:{replacement.ActiveSourceActionId}");
                    return false;
                }
                m_Port.InterruptActive(candidate.Source, "SkillReplacement");
                replacementPending = true;
            }
            if (!TryCreateRequest(candidate, profile, targetSnapshot, out ActionSkillActivationRequest<TTargetSnapshot> request))
                return false;
            if (m_Port.HasPendingRequest(profile.ActionId))
                throw new InvalidOperationException($"Action '{profile.ActionId}' already has a pending activation request.");
            m_Port.StageRequest(request);
            return !replacementPending;
        }

        ActionAdmissionDecision Evaluate(
            ActionAdmissionProfile profile,
            TTargetSnapshot targetSnapshot,
            ActionAdmissionEvaluationMode mode)
        {
            return m_Admission.Evaluate(new ActionAdmissionRequest(
                profile,
                new ActionAdmissionTargetCandidate(m_Port.TargetId(targetSnapshot)),
                mode));
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
                candidate.EquipmentContext);
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

    internal sealed class ActionSkillLifecycleFlow<TActionState>
        where TActionState : struct
    {
        readonly IActionSkillLifecyclePort<TActionState> m_Port;

        public ActionSkillLifecycleFlow(IActionSkillLifecyclePort<TActionState> port)
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
            Apply(source, action, RequireTransition(transitionValue), reason, 0);
            return true;
        }

        public void ApplyIngress(ActionSkillLifecycleIngress ingress)
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
            Apply(
                m_Port.Source(match),
                match,
                RequireTransition(ingress.TransitionValue),
                ingress.Reason,
                ingress.SourceTick);
        }

        public void Finish(
            CharacterSkillId skillId,
            SimulationExecutionSource source,
            bool completed,
            string reason)
        {
            if (!m_Port.TryFindActive(skillId, out TActionState action))
                return;
            Apply(
                source,
                action,
                completed ? ActionSkillLifecycleTransition.Complete : ActionSkillLifecycleTransition.Abort,
                reason,
                0);
        }

        public void Stop(
            CharacterSkillId skillId,
            CharacterControlSkillStopMode mode,
            SimulationExecutionSource source,
            string reason)
        {
            if (!m_Port.TryFindActive(skillId, out TActionState action))
                return;
            Apply(
                source,
                action,
                mode == CharacterControlSkillStopMode.Force
                    ? ActionSkillLifecycleTransition.Abort
                    : ActionSkillLifecycleTransition.Cancel,
                reason,
                0);
        }

        public void Interrupt(
            TActionState action,
            SimulationExecutionSource source,
            string reason)
        {
            Apply(source, action, ActionSkillLifecycleTransition.Interrupt, reason, 0);
        }

        void Apply(
            SimulationExecutionSource source,
            TActionState action,
            ActionSkillLifecycleTransition transition,
            string reason,
            ulong sourceTick)
        {
            if (!m_Port.IsActive(action))
                throw new InvalidOperationException($"Action '{m_Port.ActionId(action)}/{m_Port.InstanceId(action)}' is not active.");
            using (m_Port.EnterExecution(action))
            {
            ActionSkillLifecycleState previousState = m_Port.State(action);
            if (transition == ActionSkillLifecycleTransition.Confirm &&
                previousState != ActionSkillLifecycleState.Predicted &&
                previousState != ActionSkillLifecycleState.Corrected)
            {
                throw new InvalidOperationException($"Action '{m_Port.ActionId(action)}/{m_Port.InstanceId(action)}' cannot confirm from '{previousState}'.");
            }

            ActionSkillLifecyclePhase phase = m_Port.Phase(action);
            ActionSkillLifecycleState state = previousState;
            string nextReason = reason ?? string.Empty;
            switch (transition)
            {
                case ActionSkillLifecycleTransition.Confirm:
                    state = ActionSkillLifecycleState.Confirmed;
                    nextReason = string.Empty;
                    break;
                case ActionSkillLifecycleTransition.Complete:
                    phase = ActionSkillLifecyclePhase.Ended;
                    state = ActionSkillLifecycleState.Ended;
                    break;
                case ActionSkillLifecycleTransition.Cancel:
                    phase = ActionSkillLifecyclePhase.Cancel;
                    state = ActionSkillLifecycleState.Cancelled;
                    break;
                case ActionSkillLifecycleTransition.Interrupt:
                    phase = ActionSkillLifecyclePhase.Cancel;
                    state = ActionSkillLifecycleState.Interrupted;
                    break;
                case ActionSkillLifecycleTransition.Reject:
                    phase = ActionSkillLifecyclePhase.Ended;
                    state = ActionSkillLifecycleState.Rejected;
                    break;
                case ActionSkillLifecycleTransition.Correct:
                    state = ActionSkillLifecycleState.Corrected;
                    break;
                case ActionSkillLifecycleTransition.Abort:
                    phase = ActionSkillLifecyclePhase.Ended;
                    state = ActionSkillLifecycleState.Aborted;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transition));
            }

            TActionState next = m_Port.WithLifecycle(
                action,
                new ActionSkillLifecycleUpdate(
                    phase,
                    state,
                    transition,
                    m_Port.Tick,
                    sourceTick,
                    nextReason));
            m_Port.WriteState(next);
            m_Port.EmitActionFact(source, next);
            if (m_Port.TraceEnabled)
            {
                m_Port.Trace(
                    source,
                    "action_lifecycle",
                    ActionSkillTraceSeverity.Information,
                    $"{m_Port.ActionId(next)}:{m_Port.InstanceId(next)}:{transition}:{m_Port.Reason(next)}:equipment={m_Port.EquipmentContext(next)}",
                    m_Port.SourceGeneration(source));
            }
            if (!m_Port.IsActive(next))
                m_Port.ClearTerminalResources(m_Port.InstanceId(next));
            }
        }

        bool Matches(TActionState action, ActionSkillLifecycleIngress ingress) =>
            (ingress.ActionInstanceId == 0 || ingress.ActionInstanceId == m_Port.InstanceId(action)) &&
            (ingress.PredictionKey == 0 || ingress.PredictionKey == m_Port.PredictionKey(action)) &&
            (ingress.InputSequence == 0 || ingress.InputSequence == m_Port.InputSequence(action));

        static ActionSkillLifecycleTransition RequireTransition(int value)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
                throw new InvalidOperationException($"Action lifecycle transition '{value}' is invalid.");
            var transition = (ActionSkillLifecycleTransition)(byte)value;
            if (!Enum.IsDefined(typeof(ActionSkillLifecycleTransition), transition) || transition == ActionSkillLifecycleTransition.None)
                throw new InvalidOperationException($"Action lifecycle transition '{value}' is invalid.");
            return transition;
        }

    }
}

