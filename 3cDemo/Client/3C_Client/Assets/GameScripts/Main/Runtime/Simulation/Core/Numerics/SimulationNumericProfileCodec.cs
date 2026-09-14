using System;
using System.IO;

namespace ThirdPersonSimulation
{
    internal static class SimulationNumericProfileCodec
    {
        public static void Write(CanonicalWriter writer, SimulationNumericProfile profile)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (!profile.IsValid)
                throw new ArgumentException("Simulation numeric profile is invalid.", nameof(profile));
            writer.WriteString(profile.Id.Value);
            writer.WriteInt32(profile.AbiVersion.Value);
            writer.WriteInt32(profile.ScalarBits);
            writer.WriteByte((byte)profile.Rounding);
            writer.WriteByte((byte)profile.Overflow);
            writer.WriteBoolean(profile.DeterministicReplay);
        }

        public static SimulationNumericProfile Read(CanonicalReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            return new SimulationNumericProfile(
                new NumericProfileId(reader.ReadString()),
                new TargetAbiVersion(reader.ReadInt32()),
                reader.ReadInt32(),
                ReadEnum<SimulationNumericRoundingMode>(reader.ReadByte()),
                ReadEnum<SimulationNumericOverflowMode>(reader.ReadByte()),
                reader.ReadBoolean());
        }

        static T ReadEnum<T>(byte value) where T : struct
        {
            object candidate = Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), candidate))
                throw new InvalidDataException($"Simulation numeric profile enum '{typeof(T).Name}' value '{value}' is invalid.");
            return (T)candidate;
        }
    }
}
