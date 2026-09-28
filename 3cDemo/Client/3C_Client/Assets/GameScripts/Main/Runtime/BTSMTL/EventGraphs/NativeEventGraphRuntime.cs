using System;
using NodeCanvas.Framework;
using UnityEngine;

namespace BTSMTL.EventGraphs
{
    public sealed class NativeEventGraphRuntime : IDisposable
    {
        HostEventGraph m_Source;
        EventGraphHostContract m_Contract;
        EventGraphVariableContract m_VariableContract;
        readonly Component m_Agent;
        readonly IBlackboard m_ParentBlackboard;
        HostEventGraph m_Instance;
        EventGraphExecutionFailure m_LastFailure;
        EventGraphInvocationIdentity m_LastInvocation;
        ulong m_ResetGeneration = 1;
        bool m_Started;
        bool m_Faulted;
        bool m_Disposed;

        public NativeEventGraphRuntime(
            HostEventGraph source,
            EventGraphHostContract contract,
            Component agent = null,
            IBlackboard parentBlackboard = null)
        {
            m_Agent = agent;
            m_ParentBlackboard = parentBlackboard;
            ReplaceSource(source, contract, false);
            EnsureInstance();
        }

        public HostEventGraph Source => m_Source;
        public EventGraphHostContract Contract => m_Contract;
        public EventGraphVariableContract VariableContract => m_VariableContract;
        public ulong ResetGeneration => m_ResetGeneration;
        public bool IsStarted => m_Started;
        public bool IsFaulted => m_Faulted;
        public EventGraphExecutionFailure LastFailure => m_LastFailure;

        public NativeEventGraphExecutionResult Execute(
            EventGraphInvocationContext invocation,
            IEventGraphHostContext hostContext)
        {
            RequireAlive();
            if (hostContext == null)
                throw new ArgumentNullException(nameof(hostContext));
            if (!invocation.Identity.IsValid ||
                !invocation.Identity.Equals(hostContext.Invocation.Identity) ||
                !string.Equals(
                    invocation.EventId,
                    hostContext.Invocation.EventId,
                    StringComparison.Ordinal) ||
                invocation.DeltaSeconds != hostContext.Invocation.DeltaSeconds)
            {
                return Fail(
                    invocation,
                    "Event graph host context does not match the invocation.");
            }
            if (!string.Equals(
                    invocation.EventId,
                    m_Contract.UpdateEventId,
                    StringComparison.Ordinal))
            {
                return Fail(
                    invocation,
                    $"Event graph event '{invocation.EventId}' is not declared by host contract '{m_Contract.ContractId}'.");
            }
            if (m_Faulted)
            {
                return Fail(
                    invocation,
                    m_LastFailure?.Message ?? "Event graph runtime is faulted.");
            }
            if (m_LastInvocation.IsValid)
            {
                if (m_LastInvocation.Equals(invocation.Identity))
                    return Fail(invocation, "Event graph invocation was already executed.");
                if (!string.Equals(
                        m_LastInvocation.SourceId,
                        invocation.Identity.SourceId,
                        StringComparison.Ordinal) ||
                    invocation.Identity.Sequence <= m_LastInvocation.Sequence)
                {
                    return Fail(invocation, "Event graph invocation identity is not increasing.");
                }
            }

            try
            {
                EnsureInstance();
                m_Instance.BeginInvocation(m_Contract, hostContext);
                if (!m_Started)
                {
                    m_Instance.StartGraph(
                        m_Agent,
                        m_ParentBlackboard,
                        Graph.UpdateMode.Manual);
                    m_Started = true;
                }
                m_Instance.UpdateGraph(invocation.DeltaSeconds);
                if (m_Instance.ExecutionFailure != null)
                    return Fail(invocation, m_Instance.ExecutionFailure);
                EventGraphVariableFrame frame = m_Instance.CreateVariableFrame(
                    invocation.Identity,
                    m_ResetGeneration);
                m_LastInvocation = invocation.Identity;
                return NativeEventGraphExecutionResult.Success(frame);
            }
            catch (Exception exception)
            {
                return Fail(
                    invocation,
                    new EventGraphExecutionFailure(
                        m_Source.AuthoringId,
                        invocation.EventId,
                        string.Empty,
                        exception.Message,
                        exception));
            }
            finally
            {
                m_Instance?.EndInvocation();
            }
        }

        public void Reset()
        {
            RequireAlive();
            DestroyInstance();
            m_LastFailure = null;
            m_LastInvocation = default;
            m_Faulted = false;
            m_ResetGeneration = checked(m_ResetGeneration + 1);
            if (m_ResetGeneration == 0)
                throw new InvalidOperationException("Event graph reset generation was exhausted.");
        }

        public void ReplaceSource(
            HostEventGraph source,
            EventGraphHostContract contract,
            bool reset = true)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(NativeEventGraphRuntime));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            EventGraphVariableContract variableContract =
                EventGraphAssetValidator.Require(source, contract);
            if (reset)
                DestroyInstance();
            m_Source = source;
            m_Contract = contract;
            m_VariableContract = variableContract;
            m_LastFailure = null;
            m_LastInvocation = default;
            m_Faulted = false;
            if (reset)
            {
                m_ResetGeneration = checked(m_ResetGeneration + 1);
                if (m_ResetGeneration == 0)
                    throw new InvalidOperationException("Event graph reset generation was exhausted.");
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            DestroyInstance();
            m_Source = null;
            m_Contract = null;
            m_VariableContract = null;
            m_LastFailure = null;
            m_LastInvocation = default;
        }

        void EnsureInstance()
        {
            if (m_Instance)
                return;
            try
            {
                m_Instance = Graph.Clone<HostEventGraph>(m_Source, null);
                m_Instance.Initialize(m_Agent, m_ParentBlackboard, true);
                m_Instance.InitializeVariableOutput(m_VariableContract, m_Contract);
            }
            catch
            {
                DestroyInstance();
                throw;
            }
        }

        void DestroyInstance()
        {
            if (!m_Instance)
            {
                m_Started = false;
                return;
            }
            m_Instance.InvalidateVariableOutput();
            try
            {
                if (m_Instance.isRunning)
                    m_Instance.Stop(false);
            }
            finally
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(m_Instance);
                else
                    UnityEngine.Object.DestroyImmediate(m_Instance);
                m_Instance = null;
                m_Started = false;
            }
        }

        NativeEventGraphExecutionResult Fail(
            EventGraphInvocationContext invocation,
            string message) =>
            Fail(
                invocation,
                new EventGraphExecutionFailure(
                    m_Source?.AuthoringId ?? string.Empty,
                    invocation.EventId,
                    string.Empty,
                    message,
                    null));

        NativeEventGraphExecutionResult Fail(
            EventGraphInvocationContext invocation,
            EventGraphExecutionFailure failure)
        {
            m_LastFailure = failure;
            m_Faulted = true;
            DestroyInstance();
            return NativeEventGraphExecutionResult.Failed(failure);
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(NativeEventGraphRuntime));
        }
    }
}
