using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedProgramRuntime
    {
        public const string ComponentId = "thirdperson.simulation.program-runtime.fixed-q32.32";
        public const string SemanticVersion = "1";

        static readonly SimulationProgramRuntimeDescriptor s_Descriptor = BuildDescriptor();
        readonly ReadOnlyCollection<SimulationActorBinding> m_Roster;
        readonly IReadOnlyDictionary<ProgramId, KernelProgramBinding> m_Bindings;

        FixedProgramRuntime(
            SimulationProgramCatalog catalog,
            SimulationKernel kernel,
            IEnumerable<SimulationActorBinding> roster,
            IReadOnlyDictionary<ProgramId, KernelProgramBinding> bindings)
        {
            Descriptor = s_Descriptor;
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            var values = new List<SimulationActorBinding>(roster ?? throw new ArgumentNullException(nameof(roster)));
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            m_Roster = values.AsReadOnly();
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public static SimulationProgramRuntimeDescriptor DescriptorDefinition => s_Descriptor;
        public SimulationProgramRuntimeDescriptor Descriptor { get; }
        public SimulationProgramCatalog Catalog { get; }
        public SimulationKernel Kernel { get; }
        public IReadOnlyList<SimulationActorBinding> Roster => m_Roster;
        public KernelProgramBinding GetBinding(ProgramId programId)
        {
            if (!m_Bindings.TryGetValue(programId, out KernelProgramBinding binding))
                throw new InvalidOperationException($"Program '{programId}' has no Fixed Kernel binding.");
            return binding;
        }

        public static FixedProgramRuntime Create(IEnumerable<SimulationActorBinding> roster)
        {
            var bindings = roster == null
                ? new List<SimulationActorBinding>()
                : new List<SimulationActorBinding>(roster);
            bindings.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (bindings.Count == 0)
                throw new ArgumentException("Fixed Program Runtime requires an Actor roster.", nameof(roster));

            var programs = new Dictionary<ProgramId, CharacterSimulationProgram>();
            for (int i = 0; i < bindings.Count; i++)
            {
                SimulationActorBinding binding = bindings[i] ??
                    throw new ArgumentException("Fixed Program Runtime roster contains a missing Actor binding.", nameof(roster));
                if (i > 0 && bindings[i - 1].ActorId.Equals(binding.ActorId))
                    throw new ArgumentException($"Fixed Program Runtime roster contains duplicate ActorId '{binding.ActorId}'.", nameof(roster));
                CharacterSimulationProgram program = binding.Program;
                if (program.Manifest.NumericProfile != FixedSimulationNumericProfile.Value)
                    throw new InvalidOperationException($"Actor '{binding.ActorId}' Program is not Fixed.");
                if (!program.Manifest.OperationSetVersion.Equals(SimulationKernel.SpecializationManifest.OperationSetVersion))
                    throw new InvalidOperationException($"Actor '{binding.ActorId}' Program operation-set does not match the Fixed Kernel.");
                if (!program.Manifest.ProgramId.Equals(binding.ProgramId) ||
                    !program.ProgramHash.Equals(binding.ProgramHash) ||
                    !program.LayoutHash.Equals(binding.LayoutHash))
                {
                    throw new InvalidOperationException($"Actor '{binding.ActorId}' Program binding is stale.");
                }
                if (programs.TryGetValue(program.Manifest.ProgramId, out CharacterSimulationProgram existing))
                {
                    if (!existing.ProgramHash.Equals(program.ProgramHash) || !existing.LayoutHash.Equals(program.LayoutHash))
                        throw new InvalidOperationException($"ProgramId '{program.Manifest.ProgramId}' resolves to multiple Program identities.");
                }
                else
                {
                    programs.Add(program.Manifest.ProgramId, program);
                }
            }

            var catalog = new SimulationProgramCatalog(programs.Values);
            var kernel = SimulationKernel.CreateFixed();
            var kernelBindings = new KernelProgramBinding[catalog.Programs.Count];
            var bindingsByProgram = new Dictionary<ProgramId, KernelProgramBinding>();
            for (int i = 0; i < catalog.Programs.Count; i++)
            {
                CharacterSimulationProgram program = catalog.Programs[i];
                ProgramExecutionLayout layout = ProgramExecutionLayout.GetOrCreate(program);
                var kernelBinding = new KernelProgramBinding(program, layout, kernel);
                kernelBindings[i] = kernelBinding;
                bindingsByProgram.Add(program.Manifest.ProgramId, kernelBinding);
            }
            kernel.BindPrograms(kernelBindings);
            return new FixedProgramRuntime(catalog, kernel, bindings, bindingsByProgram);
        }

        public SimulationWorldStateSet CreateInitialState(WorldSimulationState worldState)
        {
            if (worldState == null)
                throw new ArgumentNullException(nameof(worldState));
            if (!worldState.NumericProfile.Equals(Catalog.NumericProfile) || worldState.Bodies.Count != m_Roster.Count)
                throw new ArgumentException("Initial World state does not match the Fixed Program Runtime roster.", nameof(worldState));
            var actors = new SimulationActorState[m_Roster.Count];
            for (int i = 0; i < m_Roster.Count; i++)
            {
                if (!worldState.Bodies[i].ActorId.Equals(m_Roster[i].ActorId))
                    throw new ArgumentException("Initial World state Actor order does not match the Fixed Program Runtime roster.", nameof(worldState));
                CharacterSimulationProgram program = Catalog.GetRequired(m_Roster[i].ProgramId);
                actors[i] = new SimulationActorState(
                    m_Roster[i].ActorId,
                    CharacterSimulationState.CreateInitial(program));
            }
            return new SimulationWorldStateSet(0, actors, worldState);
        }

        static SimulationProgramRuntimeDescriptor BuildDescriptor()
        {
            SimulationKernelSpecializationManifest specialization = SimulationKernel.SpecializationManifest;
            SimulationNumericProfile profile = FixedSimulationNumericProfile.Value;
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.ProgramRuntime,
                ComponentId,
                SemanticVersion,
                StableHash.Compute(
                    ComponentId,
                    SemanticVersion,
                    profile.Id.Value,
                    profile.AbiVersion.Value.ToString(),
                    specialization.BackendIdentity,
                    specialization.OperationSetVersion.Value));
            return new SimulationProgramRuntimeDescriptor(
                identity,
                profile.Id,
                profile.AbiVersion,
                specialization.OperationSetVersion,
                SimulationPipelineExecutionSupport.Forward |
                SimulationPipelineExecutionSupport.Replay |
                SimulationPipelineExecutionSupport.Restore |
                SimulationPipelineExecutionSupport.Authoritative,
                true,
                specialization.BackendIdentity);
        }
    }
}

