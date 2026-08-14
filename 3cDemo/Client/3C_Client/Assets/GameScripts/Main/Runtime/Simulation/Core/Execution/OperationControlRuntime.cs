using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation
{
    public sealed class OperationControlRuntime<TTarget>
        where TTarget : struct, IOperationControlTarget<TTarget>
    {
        readonly OperationExecutionTopology m_Topology;
        readonly TTarget m_Target;
        readonly OperationControlCursor<TTarget> m_Cursor;
        readonly Stack<StateExecutionContext> m_StateExecution = new Stack<StateExecutionContext>();
        readonly HashSet<int> m_ForceStopVisited;
        readonly int m_MaxExecutionCount;
        int m_ForceStopDepth;
        int m_ExecutionCount;

        public OperationControlRuntime(OperationExecutionTopology topology, TTarget target, int maxExecutionCount)
        {
            m_Topology = topology ?? throw new ArgumentNullException(nameof(topology));
            if (maxExecutionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExecutionCount));
            m_Target = target;
            m_MaxExecutionCount = maxExecutionCount;
            m_Cursor = new OperationControlCursor<TTarget>(this);
            m_ForceStopVisited = new HashSet<int>(m_Topology.Operations.Count);
        }

        public OperationControlCursor<TTarget> Cursor => m_Cursor;
        public void BeginEvaluation()
        {
            if (m_StateExecution.Count != 0 || m_ForceStopDepth != 0 || m_ForceStopVisited.Count != 0)
                throw new InvalidOperationException("Operation control runtime retained transient execution state across evaluations.");
            m_ExecutionCount = 0;
        }

        public OperationExecutionResult Tick(OperationHandle handle)
        {
            RequireExecution(handle);
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int lifecycleSlot = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            if (lifecycleSlot < 0)
                return Execute(operation);
            var status = (OperationRunnableStatus)m_Target.ReadInt32(lifecycleSlot);
            if (status == OperationRunnableStatus.Stopping)
                return OperationExecutionResult.Running;
            bool entering = status != OperationRunnableStatus.Running;
            if (entering)
            {
                PrepareActivation(operation);
                ulong generation = IncrementGeneration(operation);
                m_Target.ActivateScopes(m_Cursor, operation, generation);
                m_Target.WriteInt32(lifecycleSlot, (int)OperationRunnableStatus.Running);
                if (m_Target.DiagnosticsEnabled)
                    m_Target.EmitTrace(operation, "operation_enter", OperationControlTraceSeverity.Detail, operation.Code.ToString());
            }
            OperationExecutionResult result = Execute(operation);
            if (result == OperationExecutionResult.Success || result == OperationExecutionResult.Failure)
            {
                m_Target.WriteInt32(
                    lifecycleSlot,
                    result == OperationExecutionResult.Success
                        ? (int)OperationRunnableStatus.Success
                        : (int)OperationRunnableStatus.Failure);
                m_Target.CompleteScopes(operation);
                if (m_Target.DiagnosticsEnabled)
                    m_Target.EmitTrace(operation, "operation_complete", OperationControlTraceSeverity.Detail, result.ToString());
            }
            return result;
        }

        public OperationExecutionResult TickPersistent(OperationHandle handle)
        {
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            if (slot >= 0)
            {
                var status = (OperationRunnableStatus)m_Target.ReadInt32(slot);
                if (status == OperationRunnableStatus.Success)
                    return OperationExecutionResult.Success;
                if (status == OperationRunnableStatus.Failure)
                    return OperationExecutionResult.Failure;
            }
            return Tick(handle);
        }

        public OperationStopStatus RequestStop(OperationHandle handle, OperationStopContext context)
        {
            if (!handle.IsValid)
                return OperationStopStatus.Completed;
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int lifecycle = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            if (lifecycle < 0)
                return OperationStopStatus.Completed;
            var status = (OperationRunnableStatus)m_Target.ReadInt32(lifecycle);
            if (status == OperationRunnableStatus.Dormant ||
                status == OperationRunnableStatus.Success ||
                status == OperationRunnableStatus.Failure)
            {
                m_Target.ResetOperationState(operation);
                return OperationStopStatus.Completed;
            }
            if (status == OperationRunnableStatus.Running)
            {
                m_Target.WriteInt32(lifecycle, (int)OperationRunnableStatus.Stopping);
                WriteStopContext(operation, context);
                if (m_Target.DiagnosticsEnabled)
                    m_Target.EmitTrace(operation, "operation_stop_requested", OperationControlTraceSeverity.Detail, context.Cause.ToString());
            }
            else if (context.IsValid)
            {
                WriteStopContext(operation, context);
            }
            return ContinueStop(handle);
        }

        public OperationStopStatus ContinueStop(OperationHandle handle)
        {
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int lifecycle = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            if (lifecycle < 0 || m_Target.ReadInt32(lifecycle) != (int)OperationRunnableStatus.Stopping)
                return OperationStopStatus.Completed;
            OperationStopContext context = ReadStopContext(operation);
            OperationStopStatus status;
            switch (operation.Code)
            {
                case SimulationOperationCode.State:
                    status = ContinueStateStop(operation, context);
                    break;
                case SimulationOperationCode.StateMachine:
                    status = ContinueStateMachineStop(operation, context);
                    break;
                case SimulationOperationCode.Timeline:
                    status = m_Target.ContinueLeafStop(m_Cursor, operation, context);
                    break;
                default:
                    status = ContinueDirectChildStop(operation, context);
                    break;
            }
            if (status == OperationStopStatus.Running)
                return status;
            m_Target.CompleteScopes(operation);
            if (m_Target.DiagnosticsEnabled)
            {
                m_Target.EmitTrace(
                    operation,
                    "operation_stopped",
                    status == OperationStopStatus.Failed ? OperationControlTraceSeverity.Error : OperationControlTraceSeverity.Detail,
                    context.Cause.ToString());
            }
            m_Target.ResetOperationState(operation);
            return status;
        }

        public void ForceStop(OperationHandle handle, OperationStopContext context)
        {
            bool ownsVisited = m_ForceStopDepth == 0;
            if (ownsVisited)
                m_ForceStopVisited.Clear();
            m_ForceStopDepth++;
            try
            {
                ForceStopCore(handle, context);
            }
            finally
            {
                m_ForceStopDepth--;
                if (ownsVisited)
                    m_ForceStopVisited.Clear();
            }
        }

        public bool IsActive(OperationHandle handle)
        {
            if (!handle.IsValid)
                return false;
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            if (slot < 0)
                return false;
            var status = (OperationRunnableStatus)m_Target.ReadInt32(slot);
            return status == OperationRunnableStatus.Running || status == OperationRunnableStatus.Stopping;
        }

        public bool IsRunning(OperationHandle handle)
        {
            if (!handle.IsValid)
                return false;
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            return slot >= 0 && m_Target.ReadInt32(slot) == (int)OperationRunnableStatus.Running;
        }

        public bool IsStopping(OperationHandle handle)
        {
            if (!handle.IsValid)
                return false;
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            return slot >= 0 && m_Target.ReadInt32(slot) == (int)OperationRunnableStatus.Stopping;
        }

        public ulong ReadGeneration(OperationHandle handle)
        {
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableActivationGeneration);
            return slot < 0 ? 1UL : m_Target.ReadUInt64(slot);
        }

        public bool CurrentStateRootCompleted()
        {
            if (m_StateExecution.Count == 0)
                return false;
            StateExecutionContext context = m_StateExecution.Peek();
            if (context.HasRootCompletedOverride)
                return context.RootCompletedOverride;
            OperationHandle state = context.State;
            ProgramControlFlowEdge root = m_Topology.StateRoot(state);
            if (root == null)
                return false;
            int lifecycle = FindOperationSlot(m_Topology.Operation(root.Target), ProgramStateSemantic.RunnableLifecycle);
            return lifecycle >= 0 && m_Target.ReadInt32(lifecycle) == (int)OperationRunnableStatus.Success;
        }

        public ProgramControlFlowEdge PredictCurrentStateRootCompletionTransition()
        {
            if (m_StateExecution.Count == 0)
                return null;
            OperationHandle state = m_StateExecution.Peek().State;
            using (PushStateExecutionScope(state, -1, true))
                return SelectTransition(state);
        }

        public int CurrentStateExitCause()
        {
            return m_StateExecution.Count == 0 ? -1 : m_StateExecution.Peek().ExitCause;
        }

        public string FindStateExecutionPath(OperationHandle state)
        {
            OperationHandle owner = m_Topology.StateMachineOwner(state);
            if (!owner.IsValid)
                return string.Empty;
            OperationExecutionDescriptor machine = m_Topology.Operation(owner);
            int active = FindOperationSlot(machine, ProgramStateSemantic.StateMachineActive);
            int exiting = FindOperationSlot(machine, ProgramStateSemantic.StateMachineExiting);
            if ((active >= 0 && ParseHandle(m_Target.ReadIdentity(active)).Equals(state)) ||
                (exiting >= 0 && ParseHandle(m_Target.ReadIdentity(exiting)).Equals(state)))
            {
                int path = FindOperationSlot(machine, ProgramStateSemantic.StateMachineExecutionPath);
                return path >= 0 ? m_Target.ReadIdentity(path) : string.Empty;
            }
            return string.Empty;
        }

        public bool TryGetCurrentStateExecutionPath(OperationHandle state, out string path)
        {
            foreach (StateExecutionContext context in m_StateExecution)
            {
                if (!context.State.Equals(state))
                    continue;
                path = context.Path;
                return true;
            }
            path = string.Empty;
            return false;
        }

        public bool IsCurrentStateExecution(OperationHandle state)
        {
            foreach (StateExecutionContext context in m_StateExecution)
            {
                if (context.State.Equals(state))
                    return true;
            }
            return false;
        }

        public IDisposable PushStateExecution(OperationHandle state, int exitCause)
        {
            PushStateExecutionContext(state, exitCause);
            return new StateExecutionDisposable(this);
        }

        StateExecutionScope PushStateExecutionScope(OperationHandle state, int exitCause)
        {
            PushStateExecutionContext(state, exitCause, false, false);
            return new StateExecutionScope(this);
        }

        StateExecutionScope PushStateExecutionScope(
            OperationHandle state,
            int exitCause,
            bool rootCompletedOverride)
        {
            PushStateExecutionContext(state, exitCause, true, rootCompletedOverride);
            return new StateExecutionScope(this);
        }

        void PushStateExecutionContext(OperationHandle state, int exitCause)
        {
            PushStateExecutionContext(state, exitCause, false, false);
        }

        void PushStateExecutionContext(
            OperationHandle state,
            int exitCause,
            bool hasRootCompletedOverride,
            bool rootCompletedOverride)
        {
            string path = FindStateExecutionPath(state);
            m_StateExecution.Push(new StateExecutionContext(
                state,
                exitCause,
                path,
                hasRootCompletedOverride,
                rootCompletedOverride));
        }

        public void RequireExecution(OperationHandle handle)
        {
            m_Topology.RequireOperation(handle);
            m_ExecutionCount++;
            if (m_ExecutionCount > m_MaxExecutionCount)
                throw new InvalidOperationException($"Program exceeded '{m_MaxExecutionCount}' operation evaluations.");
        }

        OperationExecutionResult Execute(OperationExecutionDescriptor operation)
        {
            switch (operation.Code)
            {
                case SimulationOperationCode.Root:
                case SimulationOperationCode.StateOnEnter:
                case SimulationOperationCode.StateOnExit:
                case SimulationOperationCode.TimelineEnter:
                    return TickSingleChild(operation, ProgramControlFlowKind.Child);
                case SimulationOperationCode.Loop:
                    return TickLoop(operation);
                case SimulationOperationCode.Parallel:
                    return TickParallel(operation);
                case SimulationOperationCode.Sequence:
                    return TickSequence(operation);
                case SimulationOperationCode.Selector:
                    return TickSelector(operation);
                case SimulationOperationCode.Succeed:
                    return OperationExecutionResult.Success;
                case SimulationOperationCode.StateMachine:
                    return TickStateMachine(operation);
                case SimulationOperationCode.State:
                    return TickState(operation);
                default:
                    return m_Target.ExecuteLeaf(m_Cursor, operation);
            }
        }

        OperationExecutionResult TickSingleChild(OperationExecutionDescriptor operation, ProgramControlFlowKind kind)
        {
            IReadOnlyList<ProgramControlFlowEdge> children = Edges(operation.Handle, kind);
            if (children.Count > 1)
                throw new InvalidOperationException(
                    $"Single-child operation '{operation.Handle}' ({operation.Code}) has '{children.Count}' child edges.");
            if (children.Count == 0)
                return OperationExecutionResult.Success;
            ProgramControlFlowEdge edge = children[0];
            if (!EvaluateCondition(edge))
                return OperationExecutionResult.Failure;
            return Tick(edge.Target);
        }

        OperationExecutionResult TickLoop(OperationExecutionDescriptor operation)
        {
            IReadOnlyList<ProgramControlFlowEdge> children = Edges(operation.Handle, ProgramControlFlowKind.Child);
            if (children.Count != 1 || !EvaluateCondition(children[0]))
                return OperationExecutionResult.Failure;
            OperationExecutionResult child = Tick(children[0].Target);
            if (operation.Integer0 == 1 && child == OperationExecutionResult.Success)
                return OperationExecutionResult.Success;
            if (operation.Integer0 == 2 && child == OperationExecutionResult.Failure)
                return OperationExecutionResult.Failure;
            return OperationExecutionResult.Running;
        }

        OperationExecutionResult TickSequence(OperationExecutionDescriptor operation)
        {
            IReadOnlyList<ProgramControlFlowEdge> children = Edges(operation.Handle, ProgramControlFlowKind.Child);
            int slot = RequireOperationSlot(operation, ProgramStateSemantic.RunnableChildCursor);
            int cursor = Math.Max(0, m_Target.ReadInt32(slot));
            OperationStopContext pending = ReadStopContext(operation);
            if (pending.IsValid && cursor < children.Count)
            {
                OperationStopStatus stop = RequestStop(children[cursor].Target, pending);
                if (stop == OperationStopStatus.Running)
                    return OperationExecutionResult.Running;
                ClearStopContext(operation);
                return OperationExecutionResult.Failure;
            }
            while (cursor < children.Count)
            {
                ProgramControlFlowEdge edge = children[cursor];
                if (!EvaluateCondition(edge))
                {
                    if (UsesSelfAbort(edge.AbortPolicy) && IsActive(edge.Target))
                    {
                        OperationStopContext context = OperationStopContext.SelfAbort(edge.Target);
                        WriteStopContext(operation, context);
                        OperationStopStatus stop = RequestStop(edge.Target, context);
                        if (stop == OperationStopStatus.Running)
                            return OperationExecutionResult.Running;
                        ClearStopContext(operation);
                        if (stop == OperationStopStatus.Failed)
                            return OperationExecutionResult.Failure;
                    }
                    return OperationExecutionResult.Failure;
                }
                OperationExecutionResult result = Tick(edge.Target);
                if (result == OperationExecutionResult.Running)
                {
                    m_Target.WriteInt32(slot, cursor);
                    return result;
                }
                if (result == OperationExecutionResult.Failure)
                    return result;
                cursor++;
                m_Target.WriteInt32(slot, cursor);
            }
            return OperationExecutionResult.Success;
        }

        OperationExecutionResult TickSelector(OperationExecutionDescriptor operation)
        {
            IReadOnlyList<ProgramControlFlowEdge> children = Edges(operation.Handle, ProgramControlFlowKind.Child);
            int slot = RequireOperationSlot(operation, ProgramStateSemantic.RunnableChildCursor);
            int cursor = m_Target.ReadInt32(slot);
            OperationStopContext pending = ReadStopContext(operation);
            if (pending.IsValid && cursor >= 0 && cursor < children.Count)
            {
                OperationStopStatus stop = RequestStop(children[cursor].Target, pending);
                if (stop == OperationStopStatus.Running)
                    return OperationExecutionResult.Running;
                ClearStopContext(operation);
                m_Target.WriteInt32(slot, -1);
                if (stop == OperationStopStatus.Failed)
                    return OperationExecutionResult.Failure;
                int replacement = FindChildIndex(children, pending.Replacement);
                return TickSelectorFrom(operation, children, slot,
                    pending.Cause == OperationStopCause.LowerPriorityAbort && replacement >= 0 ? replacement : 0);
            }
            if (cursor >= 0 && cursor < children.Count && IsRunning(children[cursor].Target))
            {
                ProgramControlFlowEdge current = children[cursor];
                if (UsesSelfAbort(current.AbortPolicy) && !EvaluateCondition(current))
                {
                    OperationStopContext context = OperationStopContext.SelfAbort(current.Target);
                    WriteStopContext(operation, context);
                    OperationStopStatus stop = RequestStop(current.Target, context);
                    if (stop == OperationStopStatus.Running)
                        return OperationExecutionResult.Running;
                    ClearStopContext(operation);
                    m_Target.WriteInt32(slot, -1);
                    if (stop == OperationStopStatus.Failed)
                        return OperationExecutionResult.Failure;
                    return TickSelectorFrom(operation, children, slot, 0);
                }
                for (int i = 0; i < cursor; i++)
                {
                    if (!UsesLowerPriorityAbort(children[i].AbortPolicy) || !EvaluateCondition(children[i]))
                        continue;
                    OperationStopContext context = OperationStopContext.LowerPriorityAbort(current.Target, children[i].Target);
                    WriteStopContext(operation, context);
                    OperationStopStatus stop = RequestStop(current.Target, context);
                    if (stop == OperationStopStatus.Running)
                        return OperationExecutionResult.Running;
                    ClearStopContext(operation);
                    m_Target.WriteInt32(slot, -1);
                    if (stop == OperationStopStatus.Failed)
                        return OperationExecutionResult.Failure;
                    return TickSelectorFrom(operation, children, slot, i);
                }
                OperationExecutionResult currentResult = Tick(current.Target);
                if (currentResult != OperationExecutionResult.Failure)
                    return currentResult;
                cursor++;
            }
            return TickSelectorFrom(operation, children, slot, cursor < 0 ? 0 : cursor);
        }

        OperationExecutionResult TickParallel(OperationExecutionDescriptor operation)
        {
            IReadOnlyList<ProgramControlFlowEdge> children = Edges(operation.Handle, ProgramControlFlowKind.Child);
            int slot = RequireOperationSlot(operation, ProgramStateSemantic.RunnableChildCursor);
            int completedMask = m_Target.ReadInt32(slot);
            bool running = false;
            for (int i = 0; i < children.Count; i++)
            {
                if (i >= 31)
                    throw new InvalidOperationException($"Parallel operation '{operation.Handle}' exceeds the portable 31-child completion mask.");
                ProgramControlFlowEdge edge = children[i];
                if (!EvaluateCondition(edge))
                {
                    if (IsStopping(edge.Target))
                    {
                        OperationStopStatus pendingStop = ContinueStop(edge.Target);
                        if (pendingStop == OperationStopStatus.Failed)
                            return OperationExecutionResult.Failure;
                        if (pendingStop == OperationStopStatus.Running)
                            running = true;
                    }
                    else if (UsesSelfAbort(edge.AbortPolicy) && IsActive(edge.Target))
                    {
                        OperationStopStatus stop = RequestStop(edge.Target, OperationStopContext.SelfAbort(edge.Target));
                        if (stop == OperationStopStatus.Failed)
                            return OperationExecutionResult.Failure;
                        if (stop == OperationStopStatus.Running)
                            running = true;
                    }
                    completedMask &= ~(1 << i);
                    continue;
                }
                if (operation.Integer0 == 0 && (completedMask & (1 << i)) != 0)
                    continue;
                OperationExecutionResult result = Tick(edge.Target);
                if (result == OperationExecutionResult.Running)
                    running = true;
                else if (operation.Integer0 == 0)
                    completedMask |= 1 << i;
            }
            m_Target.WriteInt32(slot, completedMask);
            return running ? OperationExecutionResult.Running : OperationExecutionResult.Success;
        }

        OperationExecutionResult TickStateMachine(OperationExecutionDescriptor operation)
        {
            int activeSlot = RequireOperationSlot(operation, ProgramStateSemantic.StateMachineActive);
            int pendingSlot = RequireOperationSlot(operation, ProgramStateSemantic.StateMachinePending);
            int exitingSlot = RequireOperationSlot(operation, ProgramStateSemantic.StateMachineExiting);
            int transitionSlot = RequireOperationSlot(operation, ProgramStateSemantic.StateMachineTransition);
            OperationHandle exiting = ParseHandle(m_Target.ReadIdentity(exitingSlot));
            if (exiting.IsValid)
                return ContinueStateTransition(operation, activeSlot, pendingSlot, exitingSlot, transitionSlot, exiting);

            OperationHandle active = ParseHandle(m_Target.ReadIdentity(activeSlot));
            if (!active.IsValid)
            {
                OperationHandle enter = FindOwnedEntry(operation, "AnyState", false);
                IReadOnlyList<ProgramControlFlowEdge> ownerEntries = Edges(operation.Handle, ProgramControlFlowKind.Enter);
                for (int i = 0; i < ownerEntries.Count; i++)
                {
                    if (!string.Equals(ownerEntries[i].SourcePort, "AnyState", StringComparison.Ordinal))
                    {
                        enter = ownerEntries[i].Target;
                        break;
                    }
                }
                ProgramControlFlowEdge initial = SelectTransition(enter);
                if (initial == null || m_Topology.Operation(initial.Target).Code != SimulationOperationCode.State)
                    return OperationExecutionResult.Failure;
                active = initial.Target;
                ActivateState(operation, activeSlot, active);
            }

            OperationHandle anyState = FindOwnedEntry(operation, "AnyState", true);
            ProgramControlFlowEdge transition = anyState.IsValid ? SelectTransition(anyState, active, active) : null;
            if (transition == null)
            {
                OperationExecutionResult stateResult = Tick(active);
                if (stateResult == OperationExecutionResult.Failure)
                    return stateResult;
                transition = SelectTransition(active, default, active);
            }
            if (transition == null)
                return OperationExecutionResult.Running;

            if (m_Target.DiagnosticsEnabled)
            {
                m_Target.EmitTrace(
                    operation,
                    "state_transition_selected",
                    OperationControlTraceSeverity.Detail,
                    $"{transition.Identity}:{FormatHandle(active)}->{FormatHandle(transition.Target)}");
            }
            m_Target.WriteIdentity(exitingSlot, FormatHandle(active));
            m_Target.WriteIdentity(pendingSlot, FormatHandle(transition.Target));
            m_Target.WriteIdentity(transitionSlot, transition.Identity);
            return ContinueStateTransition(operation, activeSlot, pendingSlot, exitingSlot, transitionSlot, active);
        }

        OperationExecutionResult ContinueStateTransition(
            OperationExecutionDescriptor machine,
            int activeSlot,
            int pendingSlot,
            int exitingSlot,
            int transitionSlot,
            OperationHandle exiting)
        {
            OperationHandle target = ParseHandle(m_Target.ReadIdentity(pendingSlot));
            OperationStopContext context = OperationStopContext.StateTransition(exiting, target);
            OperationStopStatus stop = RequestStop(exiting, context);
            if (stop == OperationStopStatus.Running)
                return OperationExecutionResult.Running;
            if (stop == OperationStopStatus.Failed)
                return OperationExecutionResult.Failure;
            m_Target.WriteIdentity(exitingSlot, string.Empty);
            m_Target.WriteIdentity(pendingSlot, string.Empty);
            m_Target.WriteIdentity(transitionSlot, string.Empty);
            if (!target.IsValid || m_Topology.Operation(target).Code == SimulationOperationCode.StateExit)
            {
                m_Target.WriteIdentity(activeSlot, string.Empty);
                ClearStateMachineExecutionPath(machine);
                m_Target.NotifyStateLifecycle(machine, exiting, OperationStateLifecyclePhase.Exited);
                return OperationExecutionResult.Success;
            }
            m_Target.NotifyStateLifecycle(machine, exiting, OperationStateLifecyclePhase.Exited);
            ActivateState(machine, activeSlot, target);
            return OperationExecutionResult.Running;
        }

        OperationExecutionResult TickState(OperationExecutionDescriptor operation)
        {
            int cursorSlot = RequireOperationSlot(operation, ProgramStateSemantic.RunnableChildCursor);
            int phase = m_Target.ReadInt32(cursorSlot);
            if (phase <= 0)
            {
                ProgramControlFlowEdge enter = m_Topology.StateOnEnter(operation.Handle);
                if (enter != null)
                {
                    OperationExecutionResult result = TickInStateContext(operation, enter.Target, -1);
                    if (result != OperationExecutionResult.Success)
                        return result;
                    ForceStop(enter.Target, OperationStopContext.Reset(enter.Target));
                }
                phase = 1;
                m_Target.WriteInt32(cursorSlot, phase);
            }
            ProgramControlFlowEdge root = m_Topology.StateRoot(operation.Handle);
            if (root == null)
                return OperationExecutionResult.Running;
            OperationExecutionResult rootResult;
            using (PushStateExecutionScope(operation.Handle, -1))
                rootResult = TickPersistent(root.Target);
            return rootResult == OperationExecutionResult.Failure
                ? OperationExecutionResult.Failure
                : OperationExecutionResult.Running;
        }

        ProgramControlFlowEdge SelectTransition(
            OperationHandle source,
            OperationHandle excludedTarget = default,
            OperationHandle stateContext = default)
        {
            if (!source.IsValid)
                return null;
            IReadOnlyList<ProgramControlFlowEdge> transitions = Edges(source, ProgramControlFlowKind.Transition);
            StateExecutionScope scope = stateContext.IsValid
                ? PushStateExecutionScope(stateContext, -1)
                : default;
            try
            {
                for (int i = 0; i < transitions.Count; i++)
                {
                    if (excludedTarget.IsValid && transitions[i].Target.Equals(excludedTarget))
                        continue;
                    if (EvaluateCondition(transitions[i]))
                        return transitions[i];
                }
                return null;
            }
            finally
            {
                scope.Dispose();
            }
        }

        OperationStopStatus ContinueDirectChildStop(OperationExecutionDescriptor operation, OperationStopContext context)
        {
            OperationStopStatus aggregate = OperationStopStatus.Completed;
            IReadOnlyList<ProgramControlFlowEdge> children = Edges(operation.Handle, ProgramControlFlowKind.Child);
            for (int i = 0; i < children.Count; i++)
            {
                if (!IsActive(children[i].Target))
                    continue;
                OperationStopStatus status = RequestStop(children[i].Target, context);
                if (status == OperationStopStatus.Failed)
                    return status;
                if (status == OperationStopStatus.Running)
                    aggregate = status;
            }
            return aggregate;
        }

        OperationStopStatus ContinueStateStop(OperationExecutionDescriptor state, OperationStopContext context)
        {
            int cursorSlot = RequireOperationSlot(state, ProgramStateSemantic.RunnableChildCursor);
            int phase = m_Target.ReadInt32(cursorSlot);
            if (phase < 2)
            {
                ProgramControlFlowEdge active = phase <= 0
                    ? m_Topology.StateOnEnter(state.Handle)
                    : m_Topology.StateRoot(state.Handle);
                if (active != null)
                {
                    OperationStopStatus activeStop = RequestStop(active.Target, context);
                    if (activeStop != OperationStopStatus.Completed)
                        return activeStop;
                }
                phase = 2;
                m_Target.WriteInt32(cursorSlot, phase);
            }

            ProgramControlFlowEdge exit = m_Topology.StateOnExit(state.Handle);
            if (exit != null)
            {
                OperationExecutionResult result = TickInStateContext(state, exit.Target, MapExitCause(context.Cause));
                if (result == OperationExecutionResult.Running)
                    return OperationStopStatus.Running;
                if (result == OperationExecutionResult.Failure)
                    return OperationStopStatus.Failed;
                ForceStop(exit.Target, OperationStopContext.Reset(exit.Target));
            }
            m_Target.WriteInt32(cursorSlot, 3);
            m_Target.ClearStateScope(state);
            return OperationStopStatus.Completed;
        }

        OperationStopStatus ContinueStateMachineStop(OperationExecutionDescriptor machine, OperationStopContext context)
        {
            int activeSlot = RequireOperationSlot(machine, ProgramStateSemantic.StateMachineActive);
            int pendingSlot = RequireOperationSlot(machine, ProgramStateSemantic.StateMachinePending);
            int exitingSlot = RequireOperationSlot(machine, ProgramStateSemantic.StateMachineExiting);
            int transitionSlot = RequireOperationSlot(machine, ProgramStateSemantic.StateMachineTransition);
            OperationHandle exiting = ParseHandle(m_Target.ReadIdentity(exitingSlot));
            if (!exiting.IsValid)
                exiting = ParseHandle(m_Target.ReadIdentity(activeSlot));
            m_Target.WriteIdentity(pendingSlot, string.Empty);
            m_Target.WriteIdentity(transitionSlot, string.Empty);
            if (!exiting.IsValid)
            {
                m_Target.WriteIdentity(activeSlot, string.Empty);
                ClearStateMachineExecutionPath(machine);
                return OperationStopStatus.Completed;
            }
            m_Target.WriteIdentity(exitingSlot, FormatHandle(exiting));
            OperationStopStatus stop = RequestStop(exiting, context);
            if (stop != OperationStopStatus.Completed)
                return stop;
            m_Target.WriteIdentity(activeSlot, string.Empty);
            m_Target.WriteIdentity(exitingSlot, string.Empty);
            ClearStateMachineExecutionPath(machine);
            m_Target.NotifyStateLifecycle(machine, exiting, OperationStateLifecyclePhase.Exited);
            return OperationStopStatus.Completed;
        }

        void ForceStopCore(OperationHandle handle, OperationStopContext context)
        {
            if (!handle.IsValid || !m_ForceStopVisited.Add(handle.Value))
                return;
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            bool active = IsActive(handle);
            if (operation.Code == SimulationOperationCode.Timeline)
            {
                m_Target.ForceStopLeaf(m_Cursor, operation, context);
            }
            else if (operation.Code == SimulationOperationCode.StateMachine)
            {
                ForceStopStateMachine(operation, context);
            }
            else if (operation.Code == SimulationOperationCode.State)
            {
                ForceStopState(operation, context);
            }
            else
            {
                IReadOnlyList<ProgramControlFlowEdge> children = Edges(operation.Handle, ProgramControlFlowKind.Child);
                for (int i = 0; i < children.Count; i++)
                {
                    if (IsActive(children[i].Target))
                        ForceStopCore(children[i].Target, context);
                }
            }
            if (active && m_Target.DiagnosticsEnabled)
                m_Target.EmitTrace(operation, "operation_force_stopped", OperationControlTraceSeverity.Detail, context.Cause.ToString());
            m_Target.CompleteScopes(operation);
            m_Target.ResetOperationState(operation);
        }

        void ForceStopState(OperationExecutionDescriptor state, OperationStopContext context)
        {
            IReadOnlyList<ProgramControlFlowEdge> entries = Edges(state.Handle, ProgramControlFlowKind.Enter);
            for (int i = 0; i < entries.Count; i++)
                ForceStopCore(entries[i].Target, context);
            IReadOnlyList<ProgramControlFlowEdge> exits = Edges(state.Handle, ProgramControlFlowKind.Exit);
            for (int i = 0; i < exits.Count; i++)
                ForceStopCore(exits[i].Target, context);
            m_Target.ClearStateScope(state);
        }

        void ForceStopStateMachine(OperationExecutionDescriptor machine, OperationStopContext context)
        {
            int activeSlot = FindOperationSlot(machine, ProgramStateSemantic.StateMachineActive);
            int exitingSlot = FindOperationSlot(machine, ProgramStateSemantic.StateMachineExiting);
            OperationHandle active = activeSlot >= 0 ? ParseHandle(m_Target.ReadIdentity(activeSlot)) : OperationHandle.Invalid;
            OperationHandle exiting = exitingSlot >= 0 ? ParseHandle(m_Target.ReadIdentity(exitingSlot)) : OperationHandle.Invalid;
            if (active.IsValid)
                ForceStopCore(active, context);
            if (exiting.IsValid && !exiting.Equals(active))
                ForceStopCore(exiting, context);
            ClearStateMachineExecutionPath(machine);
        }

        void PrepareActivation(OperationExecutionDescriptor operation)
        {
            m_Target.ResetOperationState(operation);
            int cursor = FindOperationSlot(operation, ProgramStateSemantic.RunnableChildCursor);
            if (cursor >= 0)
                m_Target.WriteInt32(cursor, operation.Code == SimulationOperationCode.Selector ? -1 : 0);
            m_Target.PrepareActivation(operation);
        }

        ulong IncrementGeneration(OperationExecutionDescriptor operation)
        {
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableActivationGeneration);
            if (slot < 0)
                return 1;
            ulong generation = checked(m_Target.ReadUInt64(slot) + 1);
            if (generation == 0)
                generation = 1;
            m_Target.WriteUInt64(slot, generation);
            return generation;
        }

        void ActivateState(OperationExecutionDescriptor machine, int activeSlot, OperationHandle state)
        {
            m_Target.WriteIdentity(activeSlot, FormatHandle(state));
            ulong generation = checked(ReadGeneration(state) + 1);
            if (generation == 0)
                generation = 1;
            string parent = m_StateExecution.Count == 0 ? string.Empty : m_StateExecution.Peek().Path;
            string path = $"{parent}/sm:{machine.Handle.Value.ToString(CultureInfo.InvariantCulture)}/state:{state.Value.ToString(CultureInfo.InvariantCulture)}@{generation.ToString(CultureInfo.InvariantCulture)}";
            int pathSlot = RequireOperationSlot(machine, ProgramStateSemantic.StateMachineExecutionPath);
            m_Target.WriteIdentity(pathSlot, path);
            m_Target.NotifyStateLifecycle(machine, state, OperationStateLifecyclePhase.Entered);
        }

        void ClearStateMachineExecutionPath(OperationExecutionDescriptor machine)
        {
            int slot = FindOperationSlot(machine, ProgramStateSemantic.StateMachineExecutionPath);
            if (slot >= 0)
                m_Target.WriteIdentity(slot, string.Empty);
        }

        OperationExecutionResult TickSelectorFrom(
            OperationExecutionDescriptor operation,
            IReadOnlyList<ProgramControlFlowEdge> children,
            int cursorSlot,
            int start)
        {
            for (int i = Math.Max(0, start); i < children.Count; i++)
            {
                if (!EvaluateCondition(children[i]))
                    continue;
                OperationExecutionResult result = Tick(children[i].Target);
                if (result == OperationExecutionResult.Failure)
                    continue;
                m_Target.WriteInt32(cursorSlot, i);
                return result;
            }
            m_Target.WriteInt32(cursorSlot, -1);
            return OperationExecutionResult.Failure;
        }

        OperationExecutionResult TickInStateContext(OperationExecutionDescriptor state, OperationHandle target, int exitCause)
        {
            using (PushStateExecutionScope(state.Handle, exitCause))
                return Tick(target);
        }

        bool EvaluateCondition(ProgramControlFlowEdge edge)
        {
            bool result = !edge.HasCondition || m_Target.EvaluateCondition(m_Cursor, edge);
            if (edge.HasCondition && m_Target.DiagnosticsEnabled)
            {
                m_Target.EmitTrace(
                    m_Topology.Operation(edge.Source),
                    edge.Kind == ProgramControlFlowKind.Transition
                        ? "state_transition_evaluated"
                        : "condition_graph_evaluated",
                    OperationControlTraceSeverity.Detail,
                    $"{edge.Identity}:{FormatHandle(edge.Source)}->{FormatHandle(edge.Target)}:condition={FormatHandle(edge.Condition)}:result={result}");
            }
            return result;
        }

        OperationHandle FindOwnedEntry(OperationExecutionDescriptor operation, string sourcePort, bool requireSourcePort)
        {
            IReadOnlyList<ProgramControlFlowEdge> edges = Edges(operation.Handle, ProgramControlFlowKind.Enter);
            for (int i = 0; i < edges.Count; i++)
            {
                bool matches = string.Equals(edges[i].SourcePort, sourcePort, StringComparison.Ordinal);
                if (matches || !requireSourcePort && !string.Equals(edges[i].SourcePort, "AnyState", StringComparison.Ordinal))
                    return edges[i].Target;
            }
            return OperationHandle.Invalid;
        }

        IReadOnlyList<ProgramControlFlowEdge> Edges(OperationHandle source, ProgramControlFlowKind kind)
        {
            return m_Topology.Outgoing(source, kind);
        }

        int FindOperationSlot(OperationExecutionDescriptor operation, ProgramStateSemantic semantic)
        {
            return m_Topology.FindOperationStateSlot(operation.Handle, semantic);
        }

        int RequireOperationSlot(OperationExecutionDescriptor operation, ProgramStateSemantic semantic)
        {
            return m_Topology.RequireOperationStateSlot(operation.Handle, semantic);
        }

        void WriteStopContext(OperationExecutionDescriptor operation, OperationStopContext context)
        {
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableStopBarrier);
            if (slot < 0)
                return;
            int replacement = context.Replacement.IsValid ? checked(context.Replacement.Value + 1) : 0;
            if (replacement > 0x007fffff)
                throw new InvalidOperationException($"Stop replacement operation '{context.Replacement}' exceeds the portable barrier range.");
            int encoded = ((int)context.Cause & 0xff) | (replacement << 8);
            m_Target.WriteInt32(slot, encoded);
        }

        OperationStopContext ReadStopContext(OperationExecutionDescriptor operation)
        {
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableStopBarrier);
            if (slot < 0)
                return default;
            int encoded = m_Target.ReadInt32(slot);
            var cause = (OperationStopCause)(encoded & 0xff);
            int replacement = (encoded >> 8) - 1;
            return new OperationStopContext(
                cause,
                operation.Handle,
                replacement >= 0 ? new OperationHandle(replacement) : OperationHandle.Invalid);
        }

        void ClearStopContext(OperationExecutionDescriptor operation)
        {
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableStopBarrier);
            if (slot >= 0)
                m_Target.WriteInt32(slot, 0);
        }

        static int FindChildIndex(IReadOnlyList<ProgramControlFlowEdge> children, OperationHandle target)
        {
            if (!target.IsValid)
                return -1;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].Target.Equals(target))
                    return i;
            }
            return -1;
        }

        static bool UsesSelfAbort(ProgramAbortPolicy policy)
        {
            return policy == ProgramAbortPolicy.Self || policy == ProgramAbortPolicy.Both;
        }

        static bool UsesLowerPriorityAbort(ProgramAbortPolicy policy)
        {
            return policy == ProgramAbortPolicy.LowerPriority || policy == ProgramAbortPolicy.Both;
        }

        static string FormatHandle(OperationHandle value)
        {
            return value.IsValid ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        static OperationHandle ParseHandle(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) && parsed >= 0
                ? new OperationHandle(parsed)
                : OperationHandle.Invalid;
        }

        static int MapExitCause(OperationStopCause cause)
        {
            switch (cause)
            {
                case OperationStopCause.StateTransition: return 0;
                case OperationStopCause.SelfAbort: return 1;
                case OperationStopCause.LowerPriorityAbort: return 2;
                default: return 3;
            }
        }

        readonly struct StateExecutionContext
        {
            public StateExecutionContext(
                OperationHandle state,
                int exitCause,
                string path,
                bool hasRootCompletedOverride,
                bool rootCompletedOverride)
            {
                State = state;
                ExitCause = exitCause;
                Path = path ?? string.Empty;
                HasRootCompletedOverride = hasRootCompletedOverride;
                RootCompletedOverride = rootCompletedOverride;
            }

            public OperationHandle State { get; }
            public int ExitCause { get; }
            public string Path { get; }
            public bool HasRootCompletedOverride { get; }
            public bool RootCompletedOverride { get; }
        }

        readonly struct StateExecutionScope : IDisposable
        {
            readonly OperationControlRuntime<TTarget> m_Owner;

            public StateExecutionScope(OperationControlRuntime<TTarget> owner)
            {
                m_Owner = owner;
            }

            public void Dispose()
            {
                if (m_Owner == null)
                    return;
                m_Owner.m_StateExecution.Pop();
            }
        }

        sealed class StateExecutionDisposable : IDisposable
        {
            OperationControlRuntime<TTarget> m_Owner;

            public StateExecutionDisposable(OperationControlRuntime<TTarget> owner)
            {
                m_Owner = owner;
            }

            public void Dispose()
            {
                if (m_Owner == null)
                    return;
                m_Owner.m_StateExecution.Pop();
                m_Owner = null;
            }
        }
    }
}
