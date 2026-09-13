using System;

namespace ThirdPersonSimulation
{
    static class Float32EquipmentProgramLayoutCompiler
    {
        public static EquipmentProgramLayout Compile(
            CharacterSimulationProgram program,
            CharacterEquipmentRuntimeBinding binding)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            bool enabled = program.Manifest.Capabilities.HasGameplayCapability("Equipment");
            if (!enabled)
            {
                if (binding != null)
                    throw new InvalidOperationException("Character Program carries an Equipment binding while Equipment capability is disabled.");
                return EquipmentProgramLayoutCompiler.Compile(
                    false,
                    program.CatalogEntries,
                    program.StateSlots,
                    program.References,
                    program.Producers,
                    index => Read(program, index));
            }
            return EquipmentProgramLayoutCompiler.Compile(
                binding ?? throw new ArgumentNullException(nameof(binding)),
                program.StateSlots,
                program.CatalogEntries,
                program.References,
                program.Producers);
        }

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
                program.Producers,
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
                ProgramConstantKind.Scalar => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.Scalar, false, 0, 0, null, value.Scalar.ToDouble()),
                ProgramConstantKind.Vector2 => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.Vector2, false, 0, 0, null, value.Vector2.X.ToDouble(), value.Vector2.Y.ToDouble()),
                ProgramConstantKind.Vector3 => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.Vector3, false, 0, 0, null, value.Vector3.X.ToDouble(), value.Vector3.Y.ToDouble(), value.Vector3.Z.ToDouble()),
                ProgramConstantKind.Yaw => new EquipmentCatalogConstant(EquipmentCatalogConstantKind.Yaw, false, 0, 0, null, value.Yaw.Degrees.ToDouble()),
                _ => throw new InvalidOperationException($"Equipment catalog constant '{value.Identity}' kind '{value.Kind}' is unsupported.")
            };
        }
    }
}
