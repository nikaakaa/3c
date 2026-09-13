using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public sealed class Float32ProgramRuntime
    {
        public const string ComponentId = "thirdperson.simulation.program-runtime.float32";
        public const string SemanticVersion = "1";

        static readonly SimulationProgramRuntimeDescriptor s_Descriptor = BuildDescriptor();
        ReadOnlyCollection<SimulationActorBinding> m_Roster;
        IReadOnlyDictionary<ProgramId, KernelProgramBinding> m_Bindings;
        readonly CharacterControlModuleCatalog m_ControlModules;
        readonly SimulationKernel m_CharacterRuntime;

        Float32ProgramRuntime(
            SimulationProgramCatalog catalog,
            SimulationKernel kernel,
            IEnumerable<SimulationActorBinding> roster,
            IReadOnlyDictionary<ProgramId, KernelProgramBinding> bindings,
            CharacterControlModuleCatalog controlModules)
        {
            Descriptor = s_Descriptor;
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            m_CharacterRuntime = kernel ?? throw new ArgumentNullException(nameof(kernel));
            var values = new List<SimulationActorBinding>(roster ?? throw new ArgumentNullException(nameof(roster)));
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            m_Roster = values.AsReadOnly();
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            m_ControlModules = controlModules ?? throw new ArgumentNullException(nameof(controlModules));
        }

        public static SimulationProgramRuntimeDescriptor DescriptorDefinition => s_Descriptor;
        public SimulationProgramRuntimeDescriptor Descriptor { get; }
        public SimulationProgramCatalog Catalog { get; private set; }
        public IFloat32CharacterDomainRuntime CharacterRuntime => m_CharacterRuntime;
        public CharacterControlModuleCatalog ControlModules => m_ControlModules;
        public IReadOnlyList<SimulationActorBinding> Roster => m_Roster;
        public KernelProgramBinding GetBinding(ProgramId programId)
        {
            if (!m_Bindings.TryGetValue(programId, out KernelProgramBinding binding))
                throw new InvalidOperationException($"Program '{programId}' has no Float32 Kernel binding.");
            return binding;
        }

        public static Float32ProgramRuntime Create(
            IEnumerable<SimulationActorBinding> roster,
            CharacterControlModuleCatalog controlModules)
        {
            if (controlModules == null)
                throw new ArgumentNullException(nameof(controlModules));
            var bindings = roster == null
                ? new List<SimulationActorBinding>()
                : new List<SimulationActorBinding>(roster);
            bindings.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (bindings.Count == 0)
                throw new ArgumentException("Float32 Program Runtime requires an Actor roster.", nameof(roster));

            var programs = new Dictionary<ProgramId, CharacterSimulationProgram>();
            for (int i = 0; i < bindings.Count; i++)
            {
                SimulationActorBinding binding = bindings[i] ??
                    throw new ArgumentException("Float32 Program Runtime roster contains a missing Actor binding.", nameof(roster));
                if (i > 0 && bindings[i - 1].ActorId.Equals(binding.ActorId))
                    throw new ArgumentException($"Float32 Program Runtime roster contains duplicate ActorId '{binding.ActorId}'.", nameof(roster));
                CharacterSimulationProgram program = binding.Program;
                if (program.Manifest.NumericProfile != Float32SimulationNumericProfile.Value)
                    throw new InvalidOperationException($"Actor '{binding.ActorId}' Program is not Float32.");
                if (!program.Manifest.OperationSetVersion.Equals(SimulationKernel.SpecializationManifest.OperationSetVersion))
                    throw new InvalidOperationException($"Actor '{binding.ActorId}' Program operation-set does not match the Float32 Kernel.");
                if (program.ControlModuleBinding.IsValid)
                {
                    ICharacterControlModule module = controlModules.Require(program.ControlModuleBinding);
                    CharacterControlProgramCatalogValidator.ValidateAbilityPrograms(
                        module.Contract,
                        program.AbilityPrograms,
                        program.GraphCallFrames);
                }
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
            var kernel = SimulationKernel.CreateFloat32(controlModules);
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
            return new Float32ProgramRuntime(catalog, kernel, bindings, bindingsByProgram, controlModules);
        }

        internal SimulationProgramCatalog PrepareCatalogAdoption(IReadOnlyList<SimulationActorBinding> bindings)
        {
            return BuildCatalog(bindings, m_ControlModules);
        }

        internal void AdoptPrograms(
            IReadOnlyList<SimulationActorBinding> bindings,
            SimulationProgramCatalog catalog)
        {
            if (bindings == null || catalog == null)
                throw new ArgumentNullException(bindings == null ? nameof(bindings) : nameof(catalog));
            var values = new List<SimulationActorBinding>(bindings);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count != m_Roster.Count)
                throw new InvalidOperationException("Float32 Program adoption changes the locked Actor roster.");
            for (int i = 0; i < values.Count; i++)
            {
                SimulationActorBinding current = m_Roster[i];
                SimulationActorBinding next = values[i];
                if (current.ActorId != next.ActorId || current.ProgramId != next.ProgramId ||
                    !string.Equals(current.WorldBodyBindingId, next.WorldBodyBindingId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Float32 Program adoption changes the locked Actor or World binding.");
                }
                if (!next.LayoutHash.Equals(current.LayoutHash))
                    throw new InvalidOperationException($"Actor '{next.ActorId}' Program LayoutHash changed during adoption.");
            }
            var kernelBindings = new KernelProgramBinding[catalog.Programs.Count];
            var bindingsByProgram = new Dictionary<ProgramId, KernelProgramBinding>();
            for (int i = 0; i < catalog.Programs.Count; i++)
            {
                CharacterSimulationProgram program = catalog.Programs[i];
                KernelProgramBinding binding = new KernelProgramBinding(
                    program,
                    ProgramExecutionLayout.GetOrCreate(program),
                    m_CharacterRuntime);
                kernelBindings[i] = binding;
                bindingsByProgram.Add(program.Manifest.ProgramId, binding);
            }
            m_CharacterRuntime.AdoptPrograms(kernelBindings);
            Catalog = catalog;
            m_Roster = values.AsReadOnly();
            m_Bindings = bindingsByProgram;
        }

        static SimulationProgramCatalog BuildCatalog(
            IReadOnlyList<SimulationActorBinding> bindings,
            CharacterControlModuleCatalog controlModules)
        {
            if (bindings == null || bindings.Count == 0)
                throw new ArgumentException("Float32 Program adoption requires an Actor roster.", nameof(bindings));
            var programs = new Dictionary<ProgramId, CharacterSimulationProgram>();
            var ordered = new List<SimulationActorBinding>(bindings);
            ordered.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < ordered.Count; i++)
            {
                SimulationActorBinding binding = ordered[i] ??
                    throw new ArgumentException("Float32 Program adoption contains a missing Actor binding.", nameof(bindings));
                if (i > 0 && ordered[i - 1].ActorId.Equals(binding.ActorId))
                    throw new ArgumentException($"Float32 Program adoption contains duplicate ActorId '{binding.ActorId}'.", nameof(bindings));
                CharacterSimulationProgram program = binding.Program;
                if (program.Manifest.NumericProfile != Float32SimulationNumericProfile.Value ||
                    !program.Manifest.OperationSetVersion.Equals(SimulationKernel.SpecializationManifest.OperationSetVersion) ||
                    !program.Manifest.ProgramId.Equals(binding.ProgramId) ||
                    !program.ProgramHash.Equals(binding.ProgramHash) ||
                    !program.LayoutHash.Equals(binding.LayoutHash))
                {
                    throw new InvalidOperationException($"Actor '{binding.ActorId}' Program binding is stale or incompatible.");
                }
                if (program.ControlModuleBinding.IsValid)
                {
                    ICharacterControlModule module = controlModules.Require(program.ControlModuleBinding);
                    CharacterControlProgramCatalogValidator.ValidateAbilityPrograms(
                        module.Contract,
                        program.AbilityPrograms,
                        program.GraphCallFrames);
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
            return new SimulationProgramCatalog(programs.Values);
        }

        public SimulationWorldStateSet CreateInitialState(WorldSimulationState worldState)
        {
            if (worldState == null)
                throw new ArgumentNullException(nameof(worldState));
            if (!worldState.NumericProfile.Equals(Catalog.NumericProfile) || worldState.Bodies.Count != m_Roster.Count)
                throw new ArgumentException("Initial World state does not match the Float32 Program Runtime roster.", nameof(worldState));
            var actors = new SimulationActorState[m_Roster.Count];
            for (int i = 0; i < m_Roster.Count; i++)
            {
                if (!worldState.Bodies[i].ActorId.Equals(m_Roster[i].ActorId))
                    throw new ArgumentException("Initial World state Actor order does not match the Float32 Program Runtime roster.", nameof(worldState));
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
            SimulationNumericProfile profile = Float32SimulationNumericProfile.Value;
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
                false,
                specialization.BackendIdentity);
        }
    }
}
