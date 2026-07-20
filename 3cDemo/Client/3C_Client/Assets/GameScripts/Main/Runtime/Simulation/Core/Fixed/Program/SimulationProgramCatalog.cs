using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class SimulationProgramCatalog
    {
        readonly ReadOnlyCollection<CharacterSimulationProgram> m_Programs;
        readonly Dictionary<ProgramId, CharacterSimulationProgram> m_ById;

        public SimulationProgramCatalog(IEnumerable<CharacterSimulationProgram> programs)
        {
            var values = programs == null ? new List<CharacterSimulationProgram>() : new List<CharacterSimulationProgram>(programs);
            if (values.Count == 0)
                throw new ArgumentException("Simulation Program Catalog cannot be empty.", nameof(programs));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Program Catalog contains a null Program.", nameof(programs));
            }
            values.Sort((left, right) => left.Manifest.ProgramId.CompareTo(right.Manifest.ProgramId));
            m_ById = new Dictionary<ProgramId, CharacterSimulationProgram>();
            var programHashes = new HashSet<ProgramHash>();
            var semanticHashes = new HashSet<SemanticHash>();
            int tickRate = values[0].Manifest.TickRate;
            OperationSetVersion operationSetVersion = values[0].Manifest.OperationSetVersion;
            SimulationNumericProfile numericProfile = values[0].Manifest.NumericProfile;
            WorldCapability requiredWorldCapabilities = WorldCapability.None;
            for (int i = 0; i < values.Count; i++)
            {
                CharacterSimulationProgram program = values[i];
                ProgramId id = program.Manifest.ProgramId;
                if (!id.IsValid || !program.ProgramHash.IsValid || !program.LayoutHash.IsValid)
                    throw new ArgumentException($"Program '{id}' has an incomplete identity.", nameof(programs));
                if (!m_ById.TryAdd(id, program))
                    throw new ArgumentException($"Program Catalog contains duplicate ProgramId '{id}'.", nameof(programs));
                if (!programHashes.Add(program.ProgramHash))
                    throw new ArgumentException($"Program Catalog contains duplicate ProgramHash '{program.ProgramHash}'.", nameof(programs));
                if (!semanticHashes.Add(program.Manifest.SemanticHash))
                    throw new ArgumentException($"Program Catalog contains duplicate SemanticHash '{program.Manifest.SemanticHash}'.", nameof(programs));
                if (program.Manifest.TickRate != tickRate)
                    throw new ArgumentException($"Program '{id}' TickRate does not match Catalog TickRate '{tickRate}'.", nameof(programs));
                if (!program.Manifest.OperationSetVersion.Equals(operationSetVersion))
                    throw new ArgumentException($"Program '{id}' operation-set version does not match the Catalog operation-set version.", nameof(programs));
                if (program.Manifest.NumericProfile != numericProfile)
                    throw new ArgumentException($"Program '{id}' Numeric Profile does not match the Catalog Numeric Profile.", nameof(programs));
                requiredWorldCapabilities |= program.Manifest.Capabilities.RequiredWorldCapabilities;
            }
            m_Programs = values.AsReadOnly();
            TickRate = tickRate;
            OperationSetVersion = operationSetVersion;
            NumericProfile = numericProfile;
            RequiredWorldCapabilities = requiredWorldCapabilities;
            CatalogHash = ComputeHash();
        }

        public IReadOnlyList<CharacterSimulationProgram> Programs => m_Programs;
        public int TickRate { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public WorldCapability RequiredWorldCapabilities { get; }
        public ProgramCatalogHash CatalogHash { get; }

        public CharacterSimulationProgram GetRequired(ProgramId programId)
        {
            if (!m_ById.TryGetValue(programId, out CharacterSimulationProgram program))
                throw new KeyNotFoundException($"Program Catalog does not contain ProgramId '{programId}'.");
            return program;
        }

        public void RequireWorldCapabilities(WorldCapability solverCapabilities)
        {
            WorldCapability missing = RequiredWorldCapabilities & ~solverCapabilities;
            if (missing != WorldCapability.None)
                throw new InvalidOperationException($"World Solver is missing required capabilities '{missing}'.");
        }

        ProgramCatalogHash ComputeHash()
        {
            using var writer = new CanonicalWriter();
            writer.WriteInt32(3);
            writer.WriteInt32(TickRate);
            writer.WriteString(OperationSetVersion.Value);
            SimulationNumericProfileCodec.Write(writer, NumericProfile);
            writer.WriteUInt64((ulong)RequiredWorldCapabilities);
            writer.WriteInt32(m_Programs.Count);
            for (int i = 0; i < m_Programs.Count; i++)
            {
                CharacterSimulationProgram program = m_Programs[i];
                writer.WriteString(program.Manifest.ProgramId.Value);
                writer.WriteString(program.Manifest.SemanticHash.ToString());
                writer.WriteString(program.ProgramHash.ToString());
                writer.WriteString(program.LayoutHash.ToString());
                writer.WriteInt32(program.Manifest.Capabilities.GameplayCapabilities.Count);
                for (int capabilityIndex = 0; capabilityIndex < program.Manifest.Capabilities.GameplayCapabilities.Count; capabilityIndex++)
                    writer.WriteString(program.Manifest.Capabilities.GameplayCapabilities[capabilityIndex]);
                writer.WriteUInt64((ulong)program.Manifest.Capabilities.RequiredWorldCapabilities);
            }
            return new ProgramCatalogHash(writer.ComputeHash());
        }
    }
}

