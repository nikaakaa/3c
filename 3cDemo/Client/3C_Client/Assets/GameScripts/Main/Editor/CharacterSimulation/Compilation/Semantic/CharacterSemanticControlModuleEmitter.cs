using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticControlModuleEmitter
    {
        readonly CharacterSimulationProgramBuilder m_Builder;

        public CharacterSemanticControlModuleEmitter(CharacterSimulationProgramBuilder builder)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public int Emit(
            CharacterControlModuleContract contract,
            CharacterSimulationSourceLocation source)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            var fields = new List<ProgramCatalogField>
            {
                m_Builder.ConstantField(source, "SemanticVersion", contract.SemanticVersion),
                m_Builder.IdentityField("InitialState", contract.InitialState.Value)
            };
            int catalog = m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.ControlModule,
                contract.ModuleId.Value,
                contract.SemanticVersion,
                fields,
                source);
            m_Builder.DeclareSourceMap(ProgramSourceTargetKind.ControlModule, catalog, source);
            for (int i = 0; i < contract.Transitions.Count; i++)
            {
                CharacterControlTransitionDescriptor transition = contract.Transitions[i];
                CharacterSimulationSourceLocation transitionSource = new CharacterSimulationSourceLocation(
                    source.SourceType,
                    source.GraphId,
                    string.Empty,
                    transition.Id.Value,
                    string.Empty,
                    string.Empty,
                    $"{source.DisplayPath}/transition:{transition.Id.Value}",
                    contentHash: source.ContentHash);
                m_Builder.DeclareSourceMap(ProgramSourceTargetKind.ControlTransition, i, transitionSource);
            }
            return catalog;
        }
    }
}
