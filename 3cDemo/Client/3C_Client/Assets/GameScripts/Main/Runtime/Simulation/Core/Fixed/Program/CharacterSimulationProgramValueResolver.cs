using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    public static class CharacterSimulationProgramValueResolver
    {
        public static SemanticValueKind ResolveLinkedSourceKind(
            CharacterSimulationProgram program,
            ProgramControlFlowEdge edge)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (edge == null || edge.Kind != ProgramControlFlowKind.Value)
                throw new ArgumentException("A Value control-flow edge is required.", nameof(edge));
            if (edge.Source.Value < 0 || edge.Source.Value >= program.Operations.Count ||
                edge.Target.Value < 0 || edge.Target.Value >= program.Operations.Count)
                throw new InvalidOperationException($"Value edge '{edge.Identity}' references an operation outside the Program.");
            SimulationOperation source = program.Operations[edge.Source.Value];
            OperationValuePortDefinition sourcePort = CharacterGameplayValuePortContracts
                .Require(source.Code)
                .RequireSelection(edge.SourcePort);
            return ResolveOutputKind(program, source, sourcePort);
        }

        public static SemanticValueKind ResolveOutputKind(
            CharacterSimulationProgram program,
            SimulationOperation operation,
            OperationValuePortDefinition port)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (port == null)
                throw new ArgumentNullException(nameof(port));
            if (port.Constraint == OperationValuePortConstraint.Fixed)
                return port.FixedKind;
            if (operation.Code == SimulationOperationCode.BlackboardGet)
                return CharacterGameplayValuePortContracts.FromState(program.StateSlots[RequireStateReference(program, operation)].ValueKind);
            if (operation.Code == SimulationOperationCode.Constant && operation.ConstantReferences.Count > 0)
                return FromConstant(program.Constants[operation.ConstantReferences[0]].Kind);
            throw new InvalidOperationException($"Operation '{operation.Handle}' output '{port.Identity}' has no concrete Value kind.");
        }

        public static void RequireInputKind(
            CharacterSimulationProgram program,
            SimulationOperation operation,
            OperationValuePortDefinition port,
            SemanticValueKind kind)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (port == null)
                throw new ArgumentNullException(nameof(port));
            SemanticValueKind expected = port.Constraint == OperationValuePortConstraint.Dynamic
                ? CharacterGameplayValuePortContracts.FromState(program.StateSlots[RequireStateReference(program, operation)].ValueKind)
                : port.Resolve(kind);
            if (!port.Accepts(kind) ||
                (port.Constraint == OperationValuePortConstraint.Dynamic && expected != kind))
                throw new InvalidOperationException($"Operation '{operation.Handle}' input '{port.Identity}' cannot accept Value kind '{kind}'.");
        }

        static int RequireStateReference(CharacterSimulationProgram program, SimulationOperation operation)
        {
            int found = -1;
            for (int i = 0; i < program.References.Count; i++)
            {
                ProgramReference reference = program.References[i];
                if (reference.Kind != ProgramReferenceKind.StateSlot || !reference.SourceOperation.Equals(operation.Handle))
                    continue;
                if (found >= 0)
                    throw new InvalidOperationException($"Operation '{operation.Handle}' has multiple state-slot references for Value resolution.");
                found = reference.TargetIndex;
            }
            if (found < 0)
                throw new InvalidOperationException($"Operation '{operation.Handle}' has no state-slot reference for Value resolution.");
            return found;
        }

        static SemanticValueKind FromConstant(ProgramConstantKind kind)
        {
            return kind switch
            {
                ProgramConstantKind.Boolean => SemanticValueKind.Boolean,
                ProgramConstantKind.Int32 => SemanticValueKind.Int32,
                ProgramConstantKind.UInt64 => SemanticValueKind.UInt64,
                ProgramConstantKind.Scalar => SemanticValueKind.Number,
                ProgramConstantKind.Vector2 => SemanticValueKind.Vector2,
                ProgramConstantKind.Vector3 => SemanticValueKind.Vector3,
                ProgramConstantKind.Yaw => SemanticValueKind.Yaw,
                ProgramConstantKind.String => SemanticValueKind.Identity,
                _ => throw new InvalidOperationException($"Program constant kind '{kind}' cannot enter the Value graph.")
            };
        }
    }
}
