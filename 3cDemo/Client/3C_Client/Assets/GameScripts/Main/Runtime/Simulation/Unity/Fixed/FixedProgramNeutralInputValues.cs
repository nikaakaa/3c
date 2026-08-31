using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using FixedProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using FixedValue = ThirdPersonSimulation.Fixed.SimulationInputValue;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    static class FixedProgramNeutralInputValues
    {
        public static List<FixedValue> Create(FixedProgram program)
        {
            const string prefix = "input:value:";
            var values = new List<FixedValue>();
            for (int i = 0; i < program.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = program.CatalogEntries[i];
                if (entry.Kind != ProgramCatalogEntryKind.InputValue)
                    continue;
                if (!entry.Identity.StartsWith(prefix, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Fixed Program input catalog identity '{entry.Identity}' is invalid.");
                values.Add(CreateValue(program, entry, entry.Identity.Substring(prefix.Length)));
            }
            values.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            return values;
        }

        static FixedValue CreateValue(FixedProgram program, ProgramCatalogEntry entry, string inputId)
        {
            ProgramCatalogField typeField = null;
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                if (string.Equals(entry.Fields[i].Name, "ValueType", StringComparison.Ordinal))
                {
                    typeField = entry.Fields[i];
                    break;
                }
            }
            if (typeField == null || typeField.Kind != ProgramCatalogFieldKind.Constant)
                throw new InvalidOperationException($"Fixed Program input '{inputId}' has no ValueType field.");
            ThirdPersonSimulation.Fixed.ProgramConstant type = program.Constants[typeField.ConstantIndex];
            if (type.Kind != ThirdPersonSimulation.Fixed.ProgramConstantKind.Int32)
                throw new InvalidOperationException($"Fixed Program input '{inputId}' ValueType is not Int32.");
            var kind = (ProgramInputValueKind)type.Int32;
            return kind switch
            {
                ProgramInputValueKind.Boolean => FixedValue.FromBoolean(inputId, false),
                ProgramInputValueKind.Scalar => FixedValue.FromScalar(inputId, FixedScalar.Zero),
                ProgramInputValueKind.Vector2 => FixedValue.FromVector2(inputId, FixedVector2.Zero),
                ProgramInputValueKind.Vector3 => FixedValue.FromVector3(inputId, FixedVector3.Zero),
                ProgramInputValueKind.Yaw => FixedValue.FromYaw(inputId, FixedYaw.Zero),
                ProgramInputValueKind.ActionTargetSnapshot => FixedValue.FromActionTargetSnapshot(inputId,
                    ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot.None),
                _ => throw new InvalidOperationException($"Fixed Program input '{inputId}' has unsupported kind '{kind}'.")
            };
        }
    }
}
