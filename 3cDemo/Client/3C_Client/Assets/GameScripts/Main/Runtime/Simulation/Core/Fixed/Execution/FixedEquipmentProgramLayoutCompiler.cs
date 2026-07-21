using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    static class FixedEquipmentProgramLayoutCompiler
    {
        public static EquipmentProgramLayout Compile(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            bool enabled = program.Manifest.Capabilities.HasGameplayCapability("Equipment");
            return EquipmentProgramLayoutCompiler.Compile(
                enabled,
                program.CatalogEntries,
                program.StateSlots,
                program.References,
                index => Read(program, index));
        }

        static EquipmentCatalogConstant Read(CharacterSimulationProgram program, int index)
        {
            if (index < 0 || index >= program.Constants.Count)
                throw new InvalidOperationException($"Equipment catalog constant '{index}' is outside Program constants.");
            ProgramConstant value = program.Constants[index];
            return value.Kind switch
            {
                ProgramConstantKind.Boolean => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.Boolean, value.Boolean, 0, 0, null),
                ProgramConstantKind.Int32 => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.Int32, false, value.Int32, 0, null),
                ProgramConstantKind.UInt64 => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.UInt64, false, 0, value.UInt64, null),
                ProgramConstantKind.String => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.String, false, 0, 0, value.Text),
                _ => throw new InvalidOperationException($"Equipment catalog constant '{value.Identity}' kind '{value.Kind}' is unsupported.")
            };
        }
    }
}
