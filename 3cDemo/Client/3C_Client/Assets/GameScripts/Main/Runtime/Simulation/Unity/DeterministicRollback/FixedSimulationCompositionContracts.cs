using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    public interface IFixedSimulationActorRegistration :
        ISimulationActorRegistration,
        IFixedPublishedActorResultObserver
    {
        ThirdPersonSimulation.Fixed.CharacterSimulationProgram Program { get; }
        ThirdPersonSimulation.Fixed.SimulationActorBinding ProgramIdentity { get; }
        string WorldBodyBindingId { get; }
        ThirdPersonSimulation.Fixed.WorldBodyState InitialBody { get; }
        IRollbackPresentationOutputPort PresentationOutput { get; }
        ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink SimulationDiagnostics { get; }
        void BeginLogicTick();
        void BeginResultCommit(int maximumBodySamples);
        void CompleteResultCommit();
        void AbortResultCommit();
    }

    public interface IDeterministicRollbackSimulationActorRegistration : IFixedSimulationActorRegistration
    {
        IRollbackLocalInputAdapter RollbackInput { get; }
        void BindRuntimeDiagnostics(
            RollbackRuntimeState state,
            RollbackOutputCommitter outputCommitter,
            IRollbackNetworkDiagnosticsSource networkDiagnostics);
    }

    public interface IDeterministicRollbackPreparedSource : ISimulationSessionPreparedSource
    {
        DeterministicRollbackModelDefinition ModelDefinition { get; }
        string LocalPeerId { get; }
        IFixedSimulationSessionRuntimeLauncher RuntimeLauncher { get; }
        IFixedSimulationRestoreSource RestoreSource { get; }
        IFixedSourceEgressOutputPort SourceEgress { get; }
        IFixedSourceEgressOutputPort BindRuntime(
            RollbackRuntimeState state,
            IFixedSimulationSessionSnapshotCodec snapshotCodec,
            DeterministicRollbackModelPolicy policy);
    }

    public interface IDeterministicRollbackPipelineRuntimePackageProvider
    {
        DeterministicRollbackModelPolicy BuildPolicy();
        FixedSimulationPipelineRuntimePackage BuildRuntimePackage(RollbackRuntimeState state);
    }

    internal sealed class FixedSimulationOutputAggregate :
        IRollbackCommitOutputPort,
        ISimulationSessionOutputLifecycle
    {
        readonly Dictionary<ActorId, IFixedSimulationActorRegistration> m_ByActor;
        readonly IFixedSimulationActorRegistration[] m_Ordered;
        readonly int m_MaximumBodySamplesPerActor;

        public FixedSimulationOutputAggregate(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations,
            int maximumBodySamplesPerActor)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Fixed output aggregate requires an Actor roster.", nameof(registrations));
            if (maximumBodySamplesPerActor <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumBodySamplesPerActor));
            m_MaximumBodySamplesPerActor = maximumBodySamplesPerActor;
            m_ByActor = new Dictionary<ActorId, IFixedSimulationActorRegistration>();
            m_Ordered = new IFixedSimulationActorRegistration[registrations.Count];
            for (int i = 0; i < registrations.Count; i++)
            {
                IFixedSimulationActorRegistration registration = registrations[i] ??
                    throw new ArgumentException("Fixed output aggregate contains a missing registration.", nameof(registrations));
                if (!m_ByActor.TryAdd(registration.ActorId, registration))
                    throw new ArgumentException($"Fixed output aggregate contains duplicate ActorId '{registration.ActorId}'.", nameof(registrations));
                m_Ordered[i] = registration;
            }
            Array.Sort(m_Ordered, (left, right) => left.ActorId.CompareTo(right.ActorId));
        }

        public void BeginLogicTick()
        {
            for (int i = 0; i < m_Ordered.Length; i++)
                m_Ordered[i].BeginLogicTick();
        }

        public void BeginCommit()
        {
            for (int i = 0; i < m_Ordered.Length; i++)
            {
                m_Ordered[i].BeginResultCommit(m_MaximumBodySamplesPerActor);
                m_Ordered[i].PresentationOutput.BeginCommit();
            }
        }

        public void CompleteCommit(ulong confirmedTick)
        {
            for (int i = 0; i < m_Ordered.Length; i++)
            {
                m_Ordered[i].PresentationOutput.CompleteCommit(confirmedTick);
                m_Ordered[i].CompleteResultCommit();
            }
        }

        public void AbortCommit()
        {
            for (int i = 0; i < m_Ordered.Length; i++)
            {
                m_Ordered[i].PresentationOutput.AbortCommit();
                m_Ordered[i].AbortResultCommit();
            }
        }

        public void Publish(ThirdPersonSimulation.Fixed.PresentationCommand command) =>
            Route(command.Header.ActorId).PresentationOutput.Publish(command);

        public void Replace(EventId targetEventId, ThirdPersonSimulation.Fixed.PresentationCommand command) =>
            Route(command.Header.ActorId).PresentationOutput.Replace(targetEventId, command);

        void ThirdPersonSimulation.Fixed.ISimulationPresentationOutputPort.Retire(
            ActorId actorId,
            EventId sourceEventId,
            EventId targetEventId) =>
            Route(actorId).PresentationOutput.Retire(actorId, sourceEventId, targetEventId);

        public void ObservePublished(ThirdPersonSimulation.Fixed.SimulationActorTickResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            Route(result.ActorId).ObservePublished(result);
        }

        IFixedSimulationActorRegistration Route(ActorId actorId)
        {
            if (!m_ByActor.TryGetValue(actorId, out IFixedSimulationActorRegistration registration))
                throw new InvalidOperationException($"Fixed output targets unknown Actor '{actorId}'.");
            return registration;
        }
    }

    internal sealed class FixedSimulationDiagnosticsAggregate : ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink
    {
        readonly Dictionary<ActorId, ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink> m_ByActor;
        readonly ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink[] m_Ordered;

        public FixedSimulationDiagnosticsAggregate(IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Fixed diagnostics aggregate requires an Actor roster.", nameof(registrations));
            m_ByActor = new Dictionary<ActorId, ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink>();
            m_Ordered = new ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink[registrations.Count];
            for (int i = 0; i < registrations.Count; i++)
            {
                IFixedSimulationActorRegistration registration = registrations[i] ??
                    throw new ArgumentException("Fixed diagnostics aggregate contains a missing registration.", nameof(registrations));
                if (!m_ByActor.TryAdd(registration.ActorId, registration.SimulationDiagnostics))
                    throw new ArgumentException($"Fixed diagnostics aggregate contains duplicate ActorId '{registration.ActorId}'.", nameof(registrations));
                m_Ordered[i] = registration.SimulationDiagnostics;
            }
        }

        public bool IsEnabled
        {
            get
            {
                for (int i = 0; i < m_Ordered.Length; i++)
                {
                    if (m_Ordered[i].IsEnabled)
                        return true;
                }
                return false;
            }
        }

        public void PublishBoundary(ThirdPersonSimulation.Fixed.SimulationBoundaryTraceRecord record)
        {
            if (record.ActorId.IsValid)
            {
                GetRequired(record.ActorId).PublishBoundary(record);
                return;
            }
            for (int i = 0; i < m_Ordered.Length; i++)
                m_Ordered[i].PublishBoundary(record);
        }

        public void PublishPipeline(ThirdPersonSimulation.Fixed.SimulationPipelineTraceRecord record)
        {
            for (int i = 0; i < m_Ordered.Length; i++)
                m_Ordered[i].PublishPipeline(record);
        }

        public void PublishOperation(ThirdPersonSimulation.Fixed.SimulationTraceRecord record) =>
            GetRequired(record.Header.ActorId).PublishOperation(record);

        public void PublishModel(ThirdPersonSimulation.Fixed.SimulationModelTraceRecord record)
        {
            if (record.ActorId.IsValid)
            {
                GetRequired(record.ActorId).PublishModel(record);
                return;
            }
            for (int i = 0; i < m_Ordered.Length; i++)
                m_Ordered[i].PublishModel(record);
        }

        public void PublishWorld(ThirdPersonSimulation.Fixed.SimulationWorldTraceRecord record) =>
            GetRequired(record.ActorId).PublishWorld(record);

        ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink GetRequired(ActorId actorId)
        {
            if (!m_ByActor.TryGetValue(actorId, out ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink sink))
                throw new InvalidOperationException($"Fixed diagnostics targets unknown Actor '{actorId}'.");
            return sink;
        }
    }
}
