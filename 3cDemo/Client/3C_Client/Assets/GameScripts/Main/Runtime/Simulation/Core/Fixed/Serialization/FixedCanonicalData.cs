using ThirdPersonSimulation;
namespace ThirdPersonSimulation.Fixed
{
    public static class FixedCanonicalData
    {
        public static void WriteScalar(this CanonicalWriter writer, FixedScalar value) => writer.WriteInt64(value.Raw);

        public static void WriteVector2(this CanonicalWriter writer, FixedVector2 value)
        {
            writer.WriteScalar(value.X);
            writer.WriteScalar(value.Y);
        }

        public static void WriteVector3(this CanonicalWriter writer, FixedVector3 value)
        {
            writer.WriteScalar(value.X);
            writer.WriteScalar(value.Y);
            writer.WriteScalar(value.Z);
        }

        public static void WriteYaw(this CanonicalWriter writer, FixedYaw value) => writer.WriteScalar(value.Degrees);
        public static FixedScalar ReadScalar(this CanonicalReader reader) => FixedScalar.FromRaw(reader.ReadInt64());
        public static FixedVector2 ReadVector2(this CanonicalReader reader) => new FixedVector2(reader.ReadScalar(), reader.ReadScalar());
        public static FixedVector3 ReadVector3(this CanonicalReader reader) => new FixedVector3(reader.ReadScalar(), reader.ReadScalar(), reader.ReadScalar());
        public static FixedYaw ReadYaw(this CanonicalReader reader) => new FixedYaw(reader.ReadScalar());
    }
}

