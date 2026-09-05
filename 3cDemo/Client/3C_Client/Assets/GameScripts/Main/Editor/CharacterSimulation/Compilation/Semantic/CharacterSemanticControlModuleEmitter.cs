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
            for (int i = 0; i < contract.Parameters.Count; i++)
            {
                CharacterControlParameterDescriptor parameter = contract.Parameters[i];
                fields.Add(m_Builder.ConstantField(source, $"Parameter:{parameter.Id.Value}:ValueKind", parameter.ValueKind));
                fields.Add(m_Builder.ConstantField(source, $"Parameter:{parameter.Id.Value}:NumericValue", parameter.NumericValue));
            }
            for (int i = 0; i < contract.Motions.Count; i++)
            {
                CharacterControlMotionDescriptor motion = contract.Motions[i];
                fields.Add(m_Builder.IdentityField($"Motion:{motion.Binding}:Input", motion.Input.Value));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:MoveSpeed", motion.MoveSpeed));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:TurnSpeedDegrees", motion.TurnSpeedDegrees));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:ExecutionMode", motion.ExecutionMode));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:DurationSeconds", motion.DurationSeconds));
                fields.Add(m_Builder.IdentityField($"Motion:{motion.Binding}:SourceMotion", motion.SourceMotionIdentity));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:DisplacementMode", motion.DisplacementMode));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:Space", motion.Space));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:Priority", motion.Priority));
                fields.Add(m_Builder.ConstantField(source, $"Motion:{motion.Binding}:ConsumeLowerChannels", motion.ConsumeLowerChannels));
            }
            int catalog = m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.ControlModule,
                contract.ModuleId.Value,
                contract.SemanticVersion,
                fields,
                source);
            m_Builder.DeclareSourceMap(ProgramSourceTargetKind.ControlModule, catalog, source);
            for (int i = 0; i < contract.StateFields.Count; i++)
            {
                CharacterControlStateFieldDescriptor field = contract.StateFields[i];
                CharacterSimulationSourceLocation fieldSource = new CharacterSimulationSourceLocation(
                    source.SourceType,
                    source.GraphId,
                    field.Id.Value,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"{source.DisplayPath}/state:{field.Id.Value}",
                    contentHash: source.ContentHash);
                m_Builder.DeclareStateSlot(
                    field.Id.Value,
                    field.ValueKind,
                    ProgramStateOwnerKind.Control,
                    field.Semantic,
                    contract.ModuleId.Value,
                    ProgramSourceTargetKind.ControlState,
                    fieldSource);
            }
            for (int i = 0; i < contract.Transitions.Count; i++)
            {
                CharacterControlTransitionDescriptor transition = contract.Transitions[i];
                CharacterSimulationSourceLocation transitionSource = new CharacterSimulationSourceLocation(
                    source.SourceType,
                    source.GraphId,
                    transition.Id.Value,
                    string.Empty,
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
