using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics
{
    public sealed class RuntimeDiagnosticsContext
    {
        readonly Stack<RuntimeInstanceKey> m_InstanceStack = new Stack<RuntimeInstanceKey>();
        readonly Dictionary<RuntimeInstanceKey, RuntimeSourceElementHandle> m_InstanceSources = new Dictionary<RuntimeInstanceKey, RuntimeSourceElementHandle>();
        ulong m_Sequence;
        ulong m_LogicTick;
        ulong m_PresentationFrame;

        public RuntimeDiagnosticsContext(
            Guid characterRuntimeId,
            Guid sessionId,
            RuntimeProgramRevision revision,
            IDebugSourceMap sourceMap,
            RuntimeDiagnosticsStore store)
        {
            if (characterRuntimeId == Guid.Empty)
                throw new ArgumentException("Character runtime identity is required.", nameof(characterRuntimeId));
            if (sessionId == Guid.Empty)
                throw new ArgumentException("Runtime diagnostics session identity is required.", nameof(sessionId));
            CharacterRuntimeId = characterRuntimeId;
            SessionId = sessionId;
            Revision = revision;
            SourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
            Store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Guid CharacterRuntimeId { get; }
        public Guid SessionId { get; }
        public RuntimeProgramRevision Revision { get; }
        public IDebugSourceMap SourceMap { get; }
        public RuntimeDiagnosticsStore Store { get; }
        public ulong LogicTick => m_LogicTick;
        public ulong PresentationFrame => m_PresentationFrame;
        public RuntimeInstanceKey CurrentRuntimeInstance => m_InstanceStack.Count > 0
            ? m_InstanceStack.Peek()
            : RuntimeInstanceKey.Character(CharacterRuntimeId);

        public void BeginLogicTick(ulong localLogicTick)
        {
            m_LogicTick = localLogicTick;
        }

        public void BeginPresentationFrame(ulong presentationFrame)
        {
            m_PresentationFrame = presentationFrame;
        }

        public bool ShouldPublish(RuntimeTraceChannel channel, RuntimeTraceEventKind kind) => Store.ShouldPublish(channel, kind);

        public RuntimeSourceElementHandle ResolveSourceHandle(RuntimeSourceElementKey source)
        {
            if (!SourceMap.TryGetHandle(source, out RuntimeSourceElementHandle handle))
                throw new InvalidOperationException($"Runtime diagnostics source is absent from the exact Source Map: {source.Kind}/{source.GraphAuthoringId}/{source.ElementAuthoringId}/{source.TimelineAuthoringId}/{source.TrackAuthoringId}/{source.ClipAuthoringId}.");
            return handle;
        }

        public void PushRuntimeInstance(RuntimeInstanceKey instance)
        {
            if (!instance.IsValid || instance.CharacterRuntimeId != CharacterRuntimeId)
                throw new InvalidOperationException("Runtime diagnostics instance does not belong to this Character target.");
            m_InstanceStack.Push(instance);
        }

        public void PopRuntimeInstance(RuntimeInstanceKey instance)
        {
            if (m_InstanceStack.Count == 0 || !m_InstanceStack.Peek().Equals(instance))
                throw new InvalidOperationException("Runtime diagnostics instance stack is unbalanced.");
            m_InstanceStack.Pop();
        }

        public bool Publish(
            RuntimeTraceChannel channel,
            RuntimeTraceDomain domain,
            RuntimeTraceEventKind kind,
            RuntimeSourceElementKey source,
            RuntimeInstanceKey runtimeInstance,
            RuntimeTracePayload payload)
        {
            if (!Store.ShouldPublish(channel, kind))
                return false;
            RuntimeSourceElementHandle handle = ResolveSourceHandle(source);
            if (runtimeInstance.IsValid)
                m_InstanceSources[runtimeInstance] = handle;
            return Publish(channel, domain, kind, handle, runtimeInstance, payload);
        }

        public bool Publish(
            RuntimeTraceChannel channel,
            RuntimeTraceDomain domain,
            RuntimeTraceEventKind kind,
            RuntimeSourceElementHandle source,
            RuntimeInstanceKey runtimeInstance,
            RuntimeTracePayload payload)
        {
            if (!Store.ShouldPublish(channel, kind))
                return false;
            if (!source.IsValid && runtimeInstance.IsValid)
                m_InstanceSources.TryGetValue(runtimeInstance, out source);
            m_Sequence++;
            if (m_Sequence == 0)
                m_Sequence++;
            ulong position = domain == RuntimeTraceDomain.Presentation ? m_PresentationFrame : m_LogicTick;
            Store.Publish(new RuntimeTraceEvent(
                SessionId,
                Revision,
                domain,
                channel,
                position,
                m_Sequence,
                runtimeInstance.IsValid ? runtimeInstance : CurrentRuntimeInstance,
                source,
                kind,
                payload));
            return true;
        }

        public bool PublishTarget(RuntimeTraceEventKind kind, RuntimeTracePayload payload)
        {
            if (!Store.ShouldPublish(RuntimeTraceChannel.Graph, kind))
                return false;
            return Publish(
                RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Lifecycle,
                kind,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(CharacterRuntimeId),
                payload);
        }
    }
}
