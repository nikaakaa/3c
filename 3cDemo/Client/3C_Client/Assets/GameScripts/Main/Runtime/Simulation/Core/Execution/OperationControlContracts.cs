using System;

namespace ThirdPersonSimulation
{
    public enum OperationRunnableStatus : byte
    {
        Dormant = 0,
        Running = 1,
        Success = 2,
        Failure = 3,
        Stopping = 4
    }

    public enum OperationExecutionResult : byte
    {
        None = 0,
        Running = 1,
        Success = 2,
        Failure = 3
    }

    public enum OperationStopCause : byte
    {
        None = 0,
        SelfAbort = 1,
        LowerPriorityAbort = 2,
        ParentStop = 3,
        StateTransition = 4,
        Reset = 5,
        Shutdown = 6,
        ActionContextEnded = 7
    }

    public enum OperationStopStatus : byte
    {
        Running = 1,
        Completed = 2,
        Failed = 3
    }

    public enum OperationControlTraceSeverity : byte
    {
        Detail = 1,
        Error = 2
    }

    public enum OperationStateLifecyclePhase : byte
    {
        Entered = 1,
        Exited = 2
    }

    public readonly struct OperationStopContext
    {
        public OperationStopContext(OperationStopCause cause, OperationHandle source, OperationHandle replacement)
        {
            Cause = cause;
            Source = source;
            Replacement = replacement;
        }

        public OperationStopCause Cause { get; }
        public OperationHandle Source { get; }
        public OperationHandle Replacement { get; }
        public bool IsValid => Cause != OperationStopCause.None;

        public static OperationStopContext SelfAbort(OperationHandle source) =>
            new OperationStopContext(OperationStopCause.SelfAbort, source, OperationHandle.Invalid);

        public static OperationStopContext LowerPriorityAbort(OperationHandle source, OperationHandle replacement) =>
            new OperationStopContext(OperationStopCause.LowerPriorityAbort, source, replacement);

        public static OperationStopContext StateTransition(OperationHandle source, OperationHandle replacement) =>
            new OperationStopContext(OperationStopCause.StateTransition, source, replacement);

        public static OperationStopContext ParentStop(OperationHandle source) =>
            new OperationStopContext(OperationStopCause.ParentStop, source, OperationHandle.Invalid);

        public static OperationStopContext Reset(OperationHandle source) =>
            new OperationStopContext(OperationStopCause.Reset, source, OperationHandle.Invalid);

        public static OperationStopContext Shutdown(OperationHandle source) =>
            new OperationStopContext(OperationStopCause.Shutdown, source, OperationHandle.Invalid);

        public static OperationStopContext ActionContextEnded(OperationHandle source) =>
            new OperationStopContext(OperationStopCause.ActionContextEnded, source, OperationHandle.Invalid);
    }

    public interface IOperationControlTarget<TTarget>
        where TTarget : struct, IOperationControlTarget<TTarget>
    {
        bool DiagnosticsEnabled { get; }
        int ReadInt32(int slotIndex);
        void WriteInt32(int slotIndex, int value);
        ulong ReadUInt64(int slotIndex);
        void WriteUInt64(int slotIndex, ulong value);
        string ReadIdentity(int slotIndex);
        void WriteIdentity(int slotIndex, string value);
        bool EvaluateCondition(OperationControlCursor<TTarget> cursor, ProgramControlFlowEdge edge);
        OperationExecutionResult ExecuteLeaf(OperationControlCursor<TTarget> cursor, OperationExecutionDescriptor operation);
        void PrepareActivation(OperationExecutionDescriptor operation);
        void ActivateScopes(OperationControlCursor<TTarget> cursor, OperationExecutionDescriptor operation, ulong generation);
        void CompleteScopes(OperationExecutionDescriptor operation);
        void ClearStateScope(OperationExecutionDescriptor state);
        void ResetOperationState(OperationExecutionDescriptor operation);
        OperationStopStatus ContinueLeafStop(
            OperationControlCursor<TTarget> cursor,
            OperationExecutionDescriptor operation,
            OperationStopContext context);
        void ForceStopLeaf(
            OperationControlCursor<TTarget> cursor,
            OperationExecutionDescriptor operation,
            OperationStopContext context);
        void EmitTrace(
            OperationExecutionDescriptor operation,
            string code,
            OperationControlTraceSeverity severity,
            string detail);
        void NotifyStateLifecycle(
            OperationExecutionDescriptor machine,
            OperationHandle state,
            OperationStateLifecyclePhase phase);
    }

    public readonly struct OperationControlCursor<TTarget>
        where TTarget : struct, IOperationControlTarget<TTarget>
    {
        readonly OperationControlRuntime<TTarget> m_Runtime;

        internal OperationControlCursor(OperationControlRuntime<TTarget> runtime)
        {
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public OperationExecutionResult Tick(OperationHandle operation) => m_Runtime.Tick(operation);
        public OperationExecutionResult TickPersistent(OperationHandle operation) => m_Runtime.TickPersistent(operation);
        public void RequireExecution(OperationHandle operation) => m_Runtime.RequireExecution(operation);
        public OperationStopStatus RequestStop(OperationHandle operation, OperationStopContext context) => m_Runtime.RequestStop(operation, context);
        public OperationStopStatus ContinueStop(OperationHandle operation) => m_Runtime.ContinueStop(operation);
        public void ForceStop(OperationHandle operation, OperationStopContext context) => m_Runtime.ForceStop(operation, context);
        public bool IsActive(OperationHandle operation) => m_Runtime.IsActive(operation);
        public bool IsRunning(OperationHandle operation) => m_Runtime.IsRunning(operation);
        public bool IsStopping(OperationHandle operation) => m_Runtime.IsStopping(operation);
        public ulong ReadGeneration(OperationHandle operation) => m_Runtime.ReadGeneration(operation);
        public bool CurrentStateRootCompleted() => m_Runtime.CurrentStateRootCompleted();
        public ProgramControlFlowEdge PredictCurrentStateRootCompletionTransition() =>
            m_Runtime.PredictCurrentStateRootCompletionTransition();
        public int CurrentStateExitCause() => m_Runtime.CurrentStateExitCause();
        public string FindStateExecutionPath(OperationHandle state) => m_Runtime.FindStateExecutionPath(state);
        public bool TryGetCurrentStateExecutionPath(OperationHandle state, out string path) =>
            m_Runtime.TryGetCurrentStateExecutionPath(state, out path);
        public bool IsCurrentStateExecution(OperationHandle state) => m_Runtime.IsCurrentStateExecution(state);
        public IDisposable PushStateExecution(OperationHandle state, int exitCause) => m_Runtime.PushStateExecution(state, exitCause);
    }
}
