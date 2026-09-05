using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation
{
    public sealed class OperationControlRuntime<TTarget> : IOperationExecutionLifecycleHost<TTarget>
        where TTarget : struct, IOperationControlTarget<TTarget>
    {
        readonly OperationExecutionTopology m_Topology;
        readonly TTarget m_Target;
        readonly OperationControlCursor<TTarget> m_Cursor;
        readonly OperationStateMachineRuntime<TTarget> m_StateMachine;
        readonly OperationExecutionLifecycleRuntime<TTarget> m_Lifecycle;
        readonly Stack<StateExecutionContext> m_StateExecution = new Stack<StateExecutionContext>();
        readonly int m_MaxExecutionCount;
        int m_ExecutionCount;

        public OperationControlRuntime(OperationExecutionTopology topology, TTarget target, int maxExecutionCount)
        {
            m_Topology = topology ?? throw new ArgumentNullException(nameof(topology));
            if (maxExecutionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExecutionCount));
            m_Target = target;
            m_MaxExecutionCount = maxExecutionCount;
            m_Cursor = new OperationControlCursor<TTarget>(this);
            m_StateMachine = new OperationStateMachineRuntime<TTarget>(this);
            m_Lifecycle = new OperationExecutionLifecycleRuntime<TTarget>(this, m_StateMachine);
        }

        public OperationControlCursor<TTarget> Cursor => m_Cursor;
        public void BeginEvaluation()
        {
            if (m_StateExecution.Count != 0 || m_Lifecycle.HasTransientState)
                throw new InvalidOperationException("Operation control runtime retained transient execution state across evaluations.");
            m_ExecutionCount = 0;
        }

        public OperationExecutionResult Tick(OperationHandle handle) => m_Lifecycle.Tick(handle);

        public OperationExecutionResult TickPersistent(OperationHandle handle) => m_Lifecycle.TickPersistent(handle);

        public OperationStopStatus RequestStop(OperationHandle handle, OperationStopContext context) => m_Lifecycle.RequestStop(handle, context);

        public OperationStopStatus ContinueStop(OperationHandle handle) => m_Lifecycle.ContinueStop(handle);

        public void ForceStop(OperationHandle handle, OperationStopContext context) => m_Lifecycle.ForceStop(handle, context);

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

        public OperationRunnableStatus ReadStatus(OperationHandle handle)
        {
            if (!handle.IsValid)
                return OperationRunnableStatus.Dormant;
            OperationExecutionDescriptor operation = m_Topology.Operation(handle);
            int slot = FindOperationSlot(operation, ProgramStateSemantic.RunnableLifecycle);
            return slot < 0
                ? OperationRunnableStatus.Dormant
                : (OperationRunnableStatus)m_Target.ReadInt32(slot);
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
            return m_StateMachine.PredictCurrentStateRootCompletionTransition(m_StateExecution.Peek().State);
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
                    return m_StateMachine.Tick(operation);
                case SimulationOperationCode.State:
                    return m_StateMachine.TickState(operation);
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
            OperationStopContext pending = m_Lifecycle.ReadStopContext(operation);
            if (pending.IsValid && cursor < children.Count)
            {
                OperationStopStatus stop = RequestStop(children[cursor].Target, pending);
                if (stop == OperationStopStatus.Running)
                    return OperationExecutionResult.Running;
                m_Lifecycle.ClearStopContext(operation);
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
                        m_Lifecycle.WriteStopContext(operation, context);
                        OperationStopStatus stop = RequestStop(edge.Target, context);
                        if (stop == OperationStopStatus.Running)
                            return OperationExecutionResult.Running;
                        m_Lifecycle.ClearStopContext(operation);
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
            OperationStopContext pending = m_Lifecycle.ReadStopContext(operation);
            if (pending.IsValid && cursor >= 0 && cursor < children.Count)
            {
                OperationStopStatus stop = RequestStop(children[cursor].Target, pending);
                if (stop == OperationStopStatus.Running)
                    return OperationExecutionResult.Running;
                m_Lifecycle.ClearStopContext(operation);
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
                    m_Lifecycle.WriteStopContext(operation, context);
                    OperationStopStatus stop = RequestStop(current.Target, context);
                    if (stop == OperationStopStatus.Running)
                        return OperationExecutionResult.Running;
                    m_Lifecycle.ClearStopContext(operation);
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
                    m_Lifecycle.WriteStopContext(operation, context);
                    OperationStopStatus stop = RequestStop(current.Target, context);
                    if (stop == OperationStopStatus.Running)
                        return OperationExecutionResult.Running;
                    m_Lifecycle.ClearStopContext(operation);
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

        OperationExecutionTopology IOperationStateMachineHost<TTarget>.Topology => m_Topology;
        bool IOperationStateMachineHost<TTarget>.DiagnosticsEnabled => m_Target.DiagnosticsEnabled;
        int IOperationStateMachineHost<TTarget>.ReadInt32(int slotIndex) => m_Target.ReadInt32(slotIndex);
        void IOperationStateMachineHost<TTarget>.WriteInt32(int slotIndex, int value) => m_Target.WriteInt32(slotIndex, value);
        string IOperationStateMachineHost<TTarget>.ReadIdentity(int slotIndex) => m_Target.ReadIdentity(slotIndex);
        void IOperationStateMachineHost<TTarget>.WriteIdentity(int slotIndex, string value) => m_Target.WriteIdentity(slotIndex, value);
        bool IOperationStateMachineHost<TTarget>.EvaluateCondition(ProgramControlFlowEdge edge) => EvaluateCondition(edge);
        int IOperationStateMachineHost<TTarget>.RequireOperationSlot(OperationExecutionDescriptor operation, ProgramStateSemantic semantic) => RequireOperationSlot(operation, semantic);
        void IOperationStateMachineHost<TTarget>.ClearStateScope(OperationHandle state) => m_Target.ClearStateScope(m_Topology.Operation(state));
        void IOperationStateMachineHost<TTarget>.NotifyStateLifecycle(OperationExecutionDescriptor machine, OperationHandle state, OperationStateLifecyclePhase phase) => m_Target.NotifyStateLifecycle(machine, state, phase);
        void IOperationStateMachineHost<TTarget>.EmitTrace(OperationExecutionDescriptor operation, string code, OperationControlTraceSeverity severity, string detail) => m_Target.EmitTrace(operation, code, severity, detail);
        string IOperationStateMachineHost<TTarget>.CurrentStateExecutionPath => m_StateExecution.Count == 0 ? string.Empty : m_StateExecution.Peek().Path;
        IDisposable IOperationStateMachineHost<TTarget>.PushStateScope(OperationHandle state, int exitCause) => PushStateExecutionScope(state, exitCause);
        IDisposable IOperationStateMachineHost<TTarget>.PushStateScope(OperationHandle state, int exitCause, bool hasRootCompletedOverride, bool rootCompletedOverride) =>
            hasRootCompletedOverride
                ? PushStateExecutionScope(state, exitCause, rootCompletedOverride)
                : PushStateExecutionScope(state, exitCause);
        OperationControlCursor<TTarget> IOperationExecutionLifecycleHost<TTarget>.Cursor => m_Cursor;
        ulong IOperationExecutionLifecycleHost<TTarget>.ReadUInt64(int slotIndex) => m_Target.ReadUInt64(slotIndex);
        void IOperationExecutionLifecycleHost<TTarget>.WriteUInt64(int slotIndex, ulong value) => m_Target.WriteUInt64(slotIndex, value);
        void IOperationExecutionLifecycleHost<TTarget>.PrepareActivation(OperationExecutionDescriptor operation) => m_Target.PrepareActivation(operation);
        void IOperationExecutionLifecycleHost<TTarget>.ActivateScopes(OperationControlCursor<TTarget> cursor, OperationExecutionDescriptor operation, ulong generation) => m_Target.ActivateScopes(cursor, operation, generation);
        void IOperationExecutionLifecycleHost<TTarget>.CompleteScopes(OperationExecutionDescriptor operation) => m_Target.CompleteScopes(operation);
        void IOperationExecutionLifecycleHost<TTarget>.ResetOperationState(OperationExecutionDescriptor operation) => m_Target.ResetOperationState(operation);
        OperationStopStatus IOperationExecutionLifecycleHost<TTarget>.ContinueLeafStop(OperationControlCursor<TTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => m_Target.ContinueLeafStop(cursor, operation, context);
        void IOperationExecutionLifecycleHost<TTarget>.ForceStopLeaf(OperationControlCursor<TTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => m_Target.ForceStopLeaf(cursor, operation, context);
        OperationExecutionResult IOperationExecutionLifecycleHost<TTarget>.Execute(OperationExecutionDescriptor operation) => Execute(operation);
        void IOperationExecutionLifecycleHost<TTarget>.RequireExecution(OperationHandle handle) => RequireExecution(handle);

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
