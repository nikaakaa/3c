using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class Float32SimulationDiagnosticsAggregate : ISimulationDiagnosticsSink
    {
        readonly Dictionary<ActorId, ISimulationDiagnosticsSink> m_ByActor;
        readonly IReadOnlyList<ISimulationDiagnosticsSink> m_Ordered;

        public Float32SimulationDiagnosticsAggregate(IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Diagnostics aggregate requires an Actor roster.", nameof(registrations));
            m_ByActor = new Dictionary<ActorId, ISimulationDiagnosticsSink>();
            var ordered = new List<ISimulationDiagnosticsSink>(registrations.Count);
            for (int i = 0; i < registrations.Count; i++)
            {
                IFloat32SimulationActorRegistration registration = registrations[i] ??
                    throw new ArgumentException("Diagnostics aggregate contains a missing registration.", nameof(registrations));
                if (!m_ByActor.TryAdd(registration.ActorId, registration.SimulationDiagnostics))
                    throw new ArgumentException($"Diagnostics aggregate contains duplicate ActorId '{registration.ActorId}'.", nameof(registrations));
                ordered.Add(registration.SimulationDiagnostics);
            }
            m_Ordered = ordered.AsReadOnly();
        }

        public bool IsEnabled
        {
            get
            {
                for (int i = 0; i < m_Ordered.Count; i++)
                {
                    if (m_Ordered[i].IsEnabled)
                        return true;
                }
                return false;
            }
        }

        public void PublishBoundary(SimulationBoundaryTraceRecord record)
        {
            if (record.ActorId.IsValid)
            {
                GetRequired(record.ActorId).PublishBoundary(record);
                return;
            }
            for (int i = 0; i < m_Ordered.Count; i++)
                m_Ordered[i].PublishBoundary(record);
        }

        public void PublishOperation(SimulationTraceRecord record)
        {
            GetRequired(record.Header.ActorId).PublishOperation(record);
        }

        public void PublishPipeline(SimulationPipelineTraceRecord record)
        {
            for (int i = 0; i < m_Ordered.Count; i++)
                m_Ordered[i].PublishPipeline(record);
        }

        public void PublishModel(SimulationModelTraceRecord record)
        {
            if (record.ActorId.IsValid)
            {
                GetRequired(record.ActorId).PublishModel(record);
                return;
            }
            for (int i = 0; i < m_Ordered.Count; i++)
                m_Ordered[i].PublishModel(record);
        }

        public void PublishWorld(SimulationWorldTraceRecord record)
        {
            GetRequired(record.ActorId).PublishWorld(record);
        }

        ISimulationDiagnosticsSink GetRequired(ActorId actorId)
        {
            if (!m_ByActor.TryGetValue(actorId, out ISimulationDiagnosticsSink adapter))
                throw new InvalidOperationException($"Diagnostics record targets unknown Actor '{actorId}'.");
            return adapter;
        }
    }
}
