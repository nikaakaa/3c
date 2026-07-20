namespace ThirdPersonSimulation
{
    public static class Float32CanonicalData
    {
        public static void WriteScalar(this CanonicalWriter writer, Float32Scalar value) => writer.WriteUInt32(value.Bits);

        public static void WriteVector2(this CanonicalWriter writer, Float32Vector2 value)
        {
            writer.WriteScalar(value.X);
            writer.WriteScalar(value.Y);
        }

        public static void WriteVector3(this CanonicalWriter writer, Float32Vector3 value)
        {
            writer.WriteScalar(value.X);
            writer.WriteScalar(value.Y);
            writer.WriteScalar(value.Z);
        }

        public static void WriteYaw(this CanonicalWriter writer, Float32Yaw value) => writer.WriteScalar(value.Degrees);
        public static Float32Scalar ReadScalar(this CanonicalReader reader) => Float32Scalar.FromBits(reader.ReadUInt32());
        public static Float32Vector2 ReadVector2(this CanonicalReader reader) => new Float32Vector2(reader.ReadScalar(), reader.ReadScalar());
        public static Float32Vector3 ReadVector3(this CanonicalReader reader) => new Float32Vector3(reader.ReadScalar(), reader.ReadScalar(), reader.ReadScalar());
        public static Float32Yaw ReadYaw(this CanonicalReader reader) => new Float32Yaw(reader.ReadScalar());
    }
}
