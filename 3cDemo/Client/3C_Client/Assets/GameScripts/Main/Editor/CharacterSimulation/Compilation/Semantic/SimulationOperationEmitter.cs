using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct SimulationConstantInput
    {
        public SimulationConstantInput(string portId, SemanticValueKind kind, object value, CharacterSimulationSourceLocation source)
        {
            PortId = portId;
            Kind = kind;
            Value = value;
            Source = source;
        }

        public string PortId { get; }
        public SemanticValueKind Kind { get; }
        public object Value { get; }
        public CharacterSimulationSourceLocation Source { get; }
    }

    public sealed class SimulationOperationEmitter
    {
        readonly GameplayAbilitySemanticBuilder m_Builder;

        public SimulationOperationEmitter(GameplayAbilitySemanticBuilder builder)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public OperationHandle Emit(
            CharacterSimulationSourceLocation source,
            CharacterSimulationNodeEmission emission,
            IReadOnlyList<SimulationConstantInput> inputs)
        {
            var constants = new List<int>();
            var bindings = new List<(SimulationConstantInput Input, int Constant)>();
            for (int i = 0; i < inputs.Count; i++)
            {
                SimulationConstantInput input = inputs[i];
                int constant = m_Builder.DeclareConstant(input.Source, "default-value", input.Value);
                if (constant >= 0)
                {
                    constants.Add(constant);
                    bindings.Add((input, constant));
                }
            }
            for (int i = 0; i < emission.Constants.Count; i++)
            {
                KeyValuePair<string, object> pair = emission.Constants[i];
                int constant = m_Builder.DeclareConstant(source, pair.Key, pair.Value);
                if (constant >= 0)
                    constants.Add(constant);
            }
            OperationHandle operation = m_Builder.DeclareOperation(
                source,
                emission.Code,
                constants,
                emission.Integer0,
                emission.Integer1,
                emission.Unsigned0,
                default,
                emission.Text0,
                emission.Flags);
            foreach (var binding in bindings)
                m_Builder.DeclareConstantInputBinding(operation, binding.Input.PortId,
                    binding.Constant, binding.Input.Kind, binding.Input.Source);
            return operation;
        }
    }
}
