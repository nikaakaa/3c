using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "SimulationSessionComposition", menuName = "3C/Simulation/Session Composition")]
    public sealed class SimulationSessionCompositionDefinition : ScriptableObject
    {
        [SerializeField] string m_SessionId = string.Empty;
        [SerializeField] string m_WorldId = string.Empty;
        [SerializeField] string m_MapId = string.Empty;
        [SerializeField] string m_WorldRevision = string.Empty;
        [SerializeField] string m_SourceClockId = string.Empty;
        [SerializeField, Min(1)] int m_TickRate;
        [SerializeField] SimulationProgramRuntimeDefinition m_ProgramRuntime;
        [SerializeField] SimulationExecutionBackendDefinition m_ExecutionBackend;
        [SerializeField] SimulationPipelineDefinition m_Pipeline;
        [SerializeField] SimulationSessionSourceDefinition m_SessionSource;
        [SerializeField] SimulationWorldSolverDefinition m_WorldSolver;
        [SerializeField] WorldFeature m_RequiredWorldFeatures;

        public string SessionId => RequireIdentity(m_SessionId, "SessionId");
        public string WorldId => RequireIdentity(m_WorldId, "WorldId");
        public string MapId => RequireIdentity(m_MapId, "MapId");
        public string WorldRevision => RequireIdentity(m_WorldRevision, "WorldRevision");
        public string SourceClockId => RequireIdentity(m_SourceClockId, "SourceClockId");
        public int TickRate => m_TickRate > 0
            ? m_TickRate
            : throw new InvalidOperationException($"Session Composition '{name}' requires an explicit TickRate.");
        public SimulationProgramRuntimeDefinition ProgramRuntime => m_ProgramRuntime;
        public SimulationExecutionBackendDefinition ExecutionBackend => m_ExecutionBackend;
        public SimulationPipelineDefinition Pipeline => m_Pipeline;
        public SimulationSessionSourceDefinition SessionSource => m_SessionSource;
        public SimulationWorldSolverDefinition WorldSolver => m_WorldSolver;
        public WorldFeature RequiredWorldFeatures => m_RequiredWorldFeatures;

        public void RequireComplete()
        {
            _ = SessionId;
            _ = WorldId;
            _ = MapId;
            _ = WorldRevision;
            _ = SourceClockId;
            _ = TickRate;
            if (!m_ProgramRuntime || !m_ExecutionBackend || !m_Pipeline || !m_SessionSource || !m_WorldSolver)
                throw new InvalidOperationException($"Session Composition '{name}' requires all five explicit Definition references.");
        }

        public SimulationSessionCompositionPreparation CreatePreparation(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            RequireComplete();
            return new SimulationSessionCompositionPreparation(this, registrations);
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            string sessionId,
            string worldId,
            string mapId,
            string worldRevision,
            string sourceClockId,
            int tickRate,
            SimulationProgramRuntimeDefinition programRuntime,
            SimulationExecutionBackendDefinition executionBackend,
            SimulationPipelineDefinition pipeline,
            SimulationSessionSourceDefinition sessionSource,
            SimulationWorldSolverDefinition worldSolver,
            WorldFeature requiredWorldFeatures = WorldFeature.None)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_SessionId = RequireIdentity(sessionId, nameof(sessionId));
            m_WorldId = RequireIdentity(worldId, nameof(worldId));
            m_MapId = RequireIdentity(mapId, nameof(mapId));
            m_WorldRevision = RequireIdentity(worldRevision, nameof(worldRevision));
            m_SourceClockId = RequireIdentity(sourceClockId, nameof(sourceClockId));
            m_TickRate = tickRate;
            m_ProgramRuntime = programRuntime ? programRuntime : throw new ArgumentNullException(nameof(programRuntime));
            m_ExecutionBackend = executionBackend ? executionBackend : throw new ArgumentNullException(nameof(executionBackend));
            m_Pipeline = pipeline ? pipeline : throw new ArgumentNullException(nameof(pipeline));
            m_SessionSource = sessionSource ? sessionSource : throw new ArgumentNullException(nameof(sessionSource));
            m_WorldSolver = worldSolver ? worldSolver : throw new ArgumentNullException(nameof(worldSolver));
            m_RequiredWorldFeatures = requiredWorldFeatures;
        }
#endif

        static string RequireIdentity(string value, string field)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException($"Session Composition requires an explicit {field}.")
                : value.Trim();
        }
    }

    public interface ISimulationSessionComposer
    {
        SimulationSessionPreparedRuntime Compose(SimulationSessionCompositionBuildRequest request);
    }

    public interface ISimulationSessionOutputLifecycle
    {
        void BeginLogicTick();
    }

    public sealed class SimulationSessionCompositionBuildRequest
    {
        public SimulationSessionCompositionBuildRequest(
            SimulationSessionCompositionDefinition definition,
            SimulationProgramRuntimeDescriptor programRuntime,
            ISimulationSessionPreparedSource source,
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        }

        public SimulationSessionCompositionDefinition Definition { get; }
        public SimulationProgramRuntimeDescriptor ProgramRuntime { get; }
        public ISimulationSessionPreparedSource Source { get; }
        public IReadOnlyList<ISimulationActorRegistration> Registrations { get; }
    }

    public sealed class SimulationSessionPreparedRuntime
    {
        public SimulationSessionPreparedRuntime(
            SimulationSessionLaunchPlan launchPlan,
            ISimulationSessionRuntimeHandle runtimeHandle,
            ISimulationSessionOutputLifecycle outputLifecycle,
            SimulationTickSourceKind outerTickKind)
        {
            LaunchPlan = launchPlan ?? throw new ArgumentNullException(nameof(launchPlan));
            RuntimeHandle = runtimeHandle ?? throw new ArgumentNullException(nameof(runtimeHandle));
            OutputLifecycle = outputLifecycle ?? throw new ArgumentNullException(nameof(outputLifecycle));
            if (!Enum.IsDefined(typeof(SimulationTickSourceKind), outerTickKind))
                throw new ArgumentOutOfRangeException(nameof(outerTickKind));
            OuterTickKind = outerTickKind;
        }

        public SimulationSessionLaunchPlan LaunchPlan { get; }
        public ISimulationSessionRuntimeHandle RuntimeHandle { get; }
        public ISimulationSessionOutputLifecycle OutputLifecycle { get; }
        public SimulationTickSourceKind OuterTickKind { get; }
    }
}
