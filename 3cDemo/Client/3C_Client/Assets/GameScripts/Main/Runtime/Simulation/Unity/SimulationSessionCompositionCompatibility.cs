using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public readonly struct SimulationSessionCompatibilityIssue
    {
        public SimulationSessionCompatibilityIssue(string code, string message)
        {
            Code = SimulationIdentityAuthoring.Require(code, nameof(code));
            Message = SimulationIdentityAuthoring.Require(message, nameof(message));
        }

        public string Code { get; }
        public string Message { get; }
        public override string ToString() => $"{Code}: {Message}";
    }

    public sealed class SimulationSessionCompositionCompatibilityReport
    {
        readonly ReadOnlyCollection<SimulationSessionCompatibilityIssue> m_Issues;

        internal SimulationSessionCompositionCompatibilityReport(
            SimulationProgramRuntimeDescriptor programRuntime,
            SimulationExecutionBackendDescriptor backend,
            SimulationPipelineDescriptor pipeline,
            SimulationSessionSourceAuthoringDescriptor source,
            SimulationWorldSolverDefinitionDescriptor solver,
            SimulationPipelineCompilationResult compilation,
            IEnumerable<SimulationSessionCompatibilityIssue> issues)
        {
            ProgramRuntime = programRuntime;
            Backend = backend;
            Pipeline = pipeline;
            Source = source;
            Solver = solver;
            Compilation = compilation;
            m_Issues = new List<SimulationSessionCompatibilityIssue>(
                issues ?? Array.Empty<SimulationSessionCompatibilityIssue>()).AsReadOnly();
        }

        public SimulationProgramRuntimeDescriptor ProgramRuntime { get; }
        public SimulationExecutionBackendDescriptor Backend { get; }
        public SimulationPipelineDescriptor Pipeline { get; }
        public SimulationSessionSourceAuthoringDescriptor Source { get; }
        public SimulationWorldSolverDefinitionDescriptor Solver { get; }
        public SimulationPipelineCompilationResult Compilation { get; }
        public IReadOnlyList<SimulationSessionCompatibilityIssue> Issues => m_Issues;
        public bool IsValid => m_Issues.Count == 0 && Compilation != null && Compilation.IsValid;
        public SimulationPipelineIdentity PipelineIdentity => Compilation?.Plan?.Identity ?? default;
        public StableHash PlanHash => Compilation?.Plan?.PlanHash ?? default;
    }

    public static class SimulationSessionCompositionCompatibility
    {
        public static SimulationSessionCompositionCompatibilityReport Evaluate(
            SimulationSessionCompositionDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            definition.RequireComplete();

            SimulationProgramRuntimeDescriptor program = definition.ProgramRuntime.BuildDescriptor();
            SimulationExecutionBackendDescriptor backend = definition.ExecutionBackend.BuildPortableDescriptor();
            SimulationPipelineDescriptor pipeline = definition.Pipeline.BuildPortableDescriptor();
            SimulationSessionSourceAuthoringDescriptor source = definition.SessionSource.BuildAuthoringDescriptor();
            SimulationWorldSolverDefinitionDescriptor solver = definition.WorldSolver.BuildDescriptor(definition.TickRate);
            var issues = new List<SimulationSessionCompatibilityIssue>();

            RequireEqual(
                source.Source.NumericProfileId.Equals(program.NumericProfileId),
                "source_numeric_profile_mismatch",
                $"Session Source requires '{source.Source.NumericProfileId}', Program Runtime provides '{program.NumericProfileId}'.",
                issues);
            RequireEqual(
                source.Source.TargetAbiVersion.Equals(program.TargetAbiVersion),
                "source_target_abi_mismatch",
                $"Session Source requires ABI '{source.Source.TargetAbiVersion}', Program Runtime provides '{program.TargetAbiVersion}'.",
                issues);
            RequireEqual(
                string.Equals(source.Source.RequiredBackendId, backend.BackendId, StringComparison.Ordinal),
                "source_backend_mismatch",
                $"Session Source requires Backend '{source.Source.RequiredBackendId}', selected '{backend.BackendId}'.",
                issues);
            RequireEqual(
                source.Source.RequiredPipelineId.Equals(pipeline.PipelineId),
                "source_pipeline_mismatch",
                $"Session Source requires Pipeline '{source.Source.RequiredPipelineId}', selected '{pipeline.PipelineId}'.",
                issues);
            RequireEqual(
                solver.NumericProfileId.Equals(program.NumericProfileId),
                "solver_numeric_profile_mismatch",
                $"World Solver requires '{solver.NumericProfileId}', Program Runtime provides '{program.NumericProfileId}'.",
                issues);
            RequireEqual(
                solver.TargetAbiVersion.Equals(program.TargetAbiVersion),
                "solver_target_abi_mismatch",
                $"World Solver requires ABI '{solver.TargetAbiVersion}', Program Runtime provides '{program.TargetAbiVersion}'.",
                issues);
            RequireEqual(
                (solver.Capabilities & source.Source.RequiredSolverCapabilities) == source.Source.RequiredSolverCapabilities,
                "solver_capability_mismatch",
                $"World Solver capabilities '{solver.Capabilities}' do not satisfy Source requirement '{source.Source.RequiredSolverCapabilities}'.",
                issues);

            try
            {
                _ = backend.RequireTarget(
                    program.NumericProfileId,
                    program.TargetAbiVersion,
                    pipeline.SchemaVersion);
            }
            catch (Exception exception)
            {
                issues.Add(new SimulationSessionCompatibilityIssue("backend_target_unsupported", exception.Message));
            }

            SimulationPipelineCompilationResult compilation = null;
            if (issues.Count == 0)
            {
                SimulationPipelinePassFactoryCatalog factories =
                    definition.ExecutionBackend.BuildPortableFactoryCatalog(definition.Pipeline);
                var snapshotCodec = new SimulationComponentIdentity(
                    SimulationComponentRole.SnapshotCodec,
                    "thirdperson.simulation.snapshot-codec.authoring",
                    "1",
                    StableHash.Compute(
                        "simulation-snapshot-codec-authoring/1",
                        program.NumericProfileId.Value,
                        program.TargetAbiVersion.ToString(),
                        backend.Identity.ToString()));
                compilation = SimulationPipelineCompiler.Compile(
                    pipeline,
                    factories,
                    program,
                    WorldCapability.None,
                    backend,
                    source.Source,
                    source.SourcePorts,
                    solver,
                    snapshotCodec,
                    source.Source.ExecutionSupport,
                    false);
            }

            return new SimulationSessionCompositionCompatibilityReport(
                program,
                backend,
                pipeline,
                source,
                solver,
                compilation,
                issues);
        }

        static void RequireEqual(
            bool valid,
            string code,
            string message,
            List<SimulationSessionCompatibilityIssue> issues)
        {
            if (!valid)
                issues.Add(new SimulationSessionCompatibilityIssue(code, message));
        }
    }
}
