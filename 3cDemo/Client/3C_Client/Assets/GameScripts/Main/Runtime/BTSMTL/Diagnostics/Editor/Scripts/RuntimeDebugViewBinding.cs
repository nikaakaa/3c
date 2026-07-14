using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics.Editor
{
    public enum RuntimeDebugViewKind
    {
        Graph,
        Timeline
    }

    public enum RuntimeDebugViewBindingMode
    {
        None,
        Following,
        Pinned
    }

    enum RuntimeDebugViewBindingStatus
    {
        Ready,
        NoInstance,
        NoSelection,
        MultipleTimelinePlaybacks,
        PinnedInstanceMissing
    }

    public sealed class RuntimeDebugViewBinding
    {
        RuntimeDebugTargetRequest m_Request;
        RuntimeInstanceKey m_SelectedInstance;
        RuntimeDebugViewBindingMode m_Mode = RuntimeDebugViewBindingMode.Following;
        RuntimeDebugTargetResolution m_Resolution;
        RuntimeDebugViewBindingStatus m_Status;
        Guid m_BoundCharacterRuntimeId;

        public RuntimeDebugViewBinding(RuntimeDebugViewKind kind)
        {
            Kind = kind;
        }

        public RuntimeDebugViewKind Kind { get; }
        public RuntimeDebugTargetRequest Request => m_Request;
        public RuntimeDebugViewBindingMode Mode => m_Mode;
        public RuntimeInstanceKey SelectedInstance => m_SelectedInstance;
        public RuntimeDebugTargetResolution Resolution => m_Resolution;
        public bool Following => m_Mode == RuntimeDebugViewBindingMode.Following;
        public bool Pinned => m_Mode == RuntimeDebugViewBindingMode.Pinned;

        public string StatusMessage
        {
            get
            {
                if (!m_Resolution.CanReadSnapshot)
                    return m_Resolution.Message;

                return m_Status switch
                {
                    RuntimeDebugViewBindingStatus.Ready => m_Resolution.Message,
                    RuntimeDebugViewBindingStatus.NoInstance => Kind == RuntimeDebugViewKind.Graph
                        ? "The current target has not executed this Graph."
                        : "The current target has not executed this Timeline.",
                    RuntimeDebugViewBindingStatus.NoSelection => "Enable Follow or select a runtime instance.",
                    RuntimeDebugViewBindingStatus.MultipleTimelinePlaybacks => "Multiple Timeline playbacks are available. Pin one playback.",
                    RuntimeDebugViewBindingStatus.PinnedInstanceMissing => "The pinned runtime instance is absent from this snapshot.",
                    _ => string.Empty
                };
            }
        }

        public void Configure(RuntimeDebugTargetRequest request)
        {
            if (m_Request.Equals(request))
                return;

            m_Request = request;
            m_SelectedInstance = default;
            m_Mode = RuntimeDebugViewBindingMode.Following;
            m_BoundCharacterRuntimeId = Guid.Empty;
            m_Status = RuntimeDebugViewBindingStatus.NoInstance;
        }

        public void Follow()
        {
            m_Mode = RuntimeDebugViewBindingMode.Following;
            m_SelectedInstance = default;
        }

        public void Clear()
        {
            m_Mode = RuntimeDebugViewBindingMode.None;
            m_SelectedInstance = default;
            m_Status = RuntimeDebugViewBindingStatus.NoSelection;
        }

        public bool Pin(RuntimeInstanceKey instance)
        {
            if (!instance.IsValid || (Kind == RuntimeDebugViewKind.Timeline && instance.Kind != RuntimeInstanceKind.TimelinePlayback))
                return false;

            m_Mode = RuntimeDebugViewBindingMode.Pinned;
            m_SelectedInstance = instance;
            return true;
        }

        public void Dispose(RuntimeDebugSession session)
        {
            session?.ReleaseLiveInterest(this);
        }

        public RuntimeDebugTargetResolution Refresh(RuntimeDebugSession session, RuntimeTraceChannel channels)
        {
            if (session == null)
            {
                m_Resolution = new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.NoExactTarget);
                m_SelectedInstance = default;
                return m_Resolution;
            }

            m_Resolution = session.ResolveTarget(m_Request);
            if (!m_Resolution.CanReadSnapshot)
            {
                session.ReleaseLiveInterest(this);
                m_SelectedInstance = default;
                return m_Resolution;
            }

            if (session.CanControlLiveTarget)
                session.EnsureLiveInterest(this, channels);
            else
                session.ReleaseLiveInterest(this);

            RuntimeDebugViewModel view = session.ViewModel;
            if (!view.Valid)
            {
                m_SelectedInstance = default;
                return m_Resolution;
            }

            if (m_BoundCharacterRuntimeId != view.Target.CharacterRuntimeId)
            {
                m_BoundCharacterRuntimeId = view.Target.CharacterRuntimeId;
                m_Mode = RuntimeDebugViewBindingMode.Following;
                m_SelectedInstance = default;
            }

            IReadOnlyList<RuntimeInstanceKey> instances = GetInstances(view);
            if (m_Mode == RuntimeDebugViewBindingMode.Following)
            {
                if (Kind == RuntimeDebugViewKind.Timeline && instances.Count > 1)
                {
                    m_SelectedInstance = default;
                    m_Status = RuntimeDebugViewBindingStatus.MultipleTimelinePlaybacks;
                    return m_Resolution;
                }

                m_SelectedInstance = instances.Count > 0 ? instances[0] : default;
                m_Status = m_SelectedInstance.IsValid
                    ? RuntimeDebugViewBindingStatus.Ready
                    : RuntimeDebugViewBindingStatus.NoInstance;
                return m_Resolution;
            }

            if (m_Mode == RuntimeDebugViewBindingMode.Pinned)
            {
                m_Status = Contains(instances, m_SelectedInstance)
                    ? RuntimeDebugViewBindingStatus.Ready
                    : RuntimeDebugViewBindingStatus.PinnedInstanceMissing;
                return m_Resolution;
            }

            m_Status = RuntimeDebugViewBindingStatus.NoSelection;
            return m_Resolution;
        }

        IReadOnlyList<RuntimeInstanceKey> GetInstances(RuntimeDebugViewModel view)
        {
            return Kind == RuntimeDebugViewKind.Graph
                ? view.GetGraphInstances(m_Request.Source.GraphAuthoringId)
                : view.GetTimelineInstances(m_Request.Source.TimelineAuthoringId);
        }

        static bool Contains(IReadOnlyList<RuntimeInstanceKey> instances, RuntimeInstanceKey value)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].Equals(value))
                    return true;
            }
            return false;
        }
    }
}
