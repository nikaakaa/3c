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
                ReadRounding(reader.ReadByte()),
                ReadOverflow(reader.ReadByte()),
                reader.ReadBoolean());
        }

        public static void Skip(CanonicalReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            ArraySegment<byte> identity = reader.ReadUtf8Segment();
            if (identity.Count == 0)
                throw new InvalidDataException("Simulation numeric profile identity is invalid.");
            new TargetAbiVersion(reader.ReadInt32());
            reader.ReadInt32();
            ReadRounding(reader.ReadByte());
            ReadOverflow(reader.ReadByte());
            reader.ReadBoolean();
        }

        static SimulationNumericRoundingMode ReadRounding(byte value)
        {
            var candidate = (SimulationNumericRoundingMode)value;
            if (candidate != SimulationNumericRoundingMode.Ieee754NearestEven &&
                candidate != SimulationNumericRoundingMode.FixedNearestEven)
                throw new InvalidDataException($"Simulation numeric profile enum '{nameof(SimulationNumericRoundingMode)}' value '{value}' is invalid.");
            return candidate;
        }

        static SimulationNumericOverflowMode ReadOverflow(byte value)
        {
            var candidate = (SimulationNumericOverflowMode)value;
            if (candidate != SimulationNumericOverflowMode.RejectNonFinite &&
                candidate != SimulationNumericOverflowMode.RejectOverflow)
                throw new InvalidDataException($"Simulation numeric profile enum '{nameof(SimulationNumericOverflowMode)}' value '{value}' is invalid.");
            return candidate;
        }
    }
}
