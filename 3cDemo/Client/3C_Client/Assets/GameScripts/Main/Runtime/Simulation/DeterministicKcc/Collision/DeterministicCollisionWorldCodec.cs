using System;
using System.IO;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public static class DeterministicCollisionWorldCodec
    {
        const uint Magic = 0x57434344;

        public static byte[] Write(DeterministicCollisionWorldArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            using var writer = new CanonicalWriter();
            WritePayload(writer, artifact);
            return writer.ToArray();
        }

        public static DeterministicCollisionWorldArtifact Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != DeterministicCollisionWorldArtifact.ArtifactVersion ||
                !string.Equals(reader.ReadString(), DeterministicCollisionWorldArtifact.ArtifactSchema, StringComparison.Ordinal))
                throw new InvalidDataException("Deterministic Collision World header is invalid.");
            string mapId = reader.ReadString();
            int quantization = reader.ReadInt32();
            var bounds = new DeterministicCollisionBounds(reader.ReadVector3(), reader.ReadVector3());
            int surfaceCount = ReadCount(reader);
            var surfaces = new DeterministicCollisionSurface[surfaceCount];
            for (int i = 0; i < surfaceCount; i++)
                surfaces[i] = new DeterministicCollisionSurface(reader.ReadInt32(), reader.ReadString(), reader.ReadString(), reader.ReadBoolean());
            int vertexCount = ReadCount(reader);
            var vertices = new FixedVector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                vertices[i] = reader.ReadVector3();
            int primitiveCount = ReadCount(reader);
            var primitives = new DeterministicCollisionPrimitive[primitiveCount];
            for (int i = 0; i < primitiveCount; i++)
            {
                int id = reader.ReadInt32();
                DeterministicCollisionPrimitiveKind kind = ReadKind(reader.ReadByte());
                int surfaceId = reader.ReadInt32();
                FixedVector3 a = reader.ReadVector3();
                FixedVector3 b = reader.ReadVector3();
                FixedVector3 c = reader.ReadVector3();
                FixedVector3 normal = reader.ReadVector3();
                FixedScalar distance = reader.ReadScalar();
                var primitiveBounds = new DeterministicCollisionBounds(reader.ReadVector3(), reader.ReadVector3());
                int vertex0 = reader.ReadInt32();
                int vertex1 = reader.ReadInt32();
                int vertex2 = reader.ReadInt32();
                int adjacentPrimitive0 = reader.ReadInt32();
                int adjacentPrimitive1 = reader.ReadInt32();
                int adjacentPrimitive2 = reader.ReadInt32();
                primitives[i] = kind switch
                {
                    DeterministicCollisionPrimitiveKind.Plane => DeterministicCollisionPrimitive.Plane(id, surfaceId, normal, distance, primitiveBounds),
                    DeterministicCollisionPrimitiveKind.Triangle => DeterministicCollisionPrimitive.Triangle(
                        id,
                        surfaceId,
                        vertex0,
                        vertex1,
                        vertex2,
                        a,
                        b,
                        c,
                        adjacentPrimitive0,
                        adjacentPrimitive1,
                        adjacentPrimitive2),
                    DeterministicCollisionPrimitiveKind.Box => DeterministicCollisionPrimitive.Box(id, surfaceId, a, b),
                    _ => throw new InvalidDataException($"Collision primitive kind '{kind}' is unsupported.")
                };
            }
            reader.RequireComplete();
            var artifact = new DeterministicCollisionWorldArtifact(mapId, quantization, bounds, surfaces, vertices, primitives);
            byte[] canonical = Write(artifact);
            if (!BytesEqual(bytes, canonical))
                throw new InvalidDataException("Deterministic Collision World artifact is not canonical.");
            return artifact;
        }

        public static StableHash ComputeContentHash(DeterministicCollisionWorldArtifact artifact)
        {
            using var writer = new CanonicalWriter();
            WritePayload(writer, artifact);
            return writer.ComputeHash();
        }

        static void WritePayload(CanonicalWriter writer, DeterministicCollisionWorldArtifact artifact)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(DeterministicCollisionWorldArtifact.ArtifactVersion);
            writer.WriteString(DeterministicCollisionWorldArtifact.ArtifactSchema);
            writer.WriteString(artifact.MapId);
            writer.WriteInt32(artifact.QuantizationUnitsPerMeter);
            writer.WriteVector3(artifact.Bounds.Minimum);
            writer.WriteVector3(artifact.Bounds.Maximum);
            writer.WriteInt32(artifact.Surfaces.Count);
            for (int i = 0; i < artifact.Surfaces.Count; i++)
            {
                DeterministicCollisionSurface value = artifact.Surfaces[i];
                writer.WriteInt32(value.Id);
                writer.WriteString(value.Identity);
                writer.WriteString(value.MaterialIdentity);
                writer.WriteBoolean(value.Walkable);
            }
            writer.WriteInt32(artifact.Vertices.Count);
            for (int i = 0; i < artifact.Vertices.Count; i++)
                writer.WriteVector3(artifact.Vertices[i]);
            writer.WriteInt32(artifact.Primitives.Count);
            for (int i = 0; i < artifact.Primitives.Count; i++)
            {
                DeterministicCollisionPrimitive value = artifact.Primitives[i];
                writer.WriteInt32(value.Id);
                writer.WriteByte((byte)value.Kind);
                writer.WriteInt32(value.SurfaceId);
                writer.WriteVector3(value.A);
                writer.WriteVector3(value.B);
                writer.WriteVector3(value.C);
                writer.WriteVector3(value.Normal);
                writer.WriteScalar(value.Distance);
                writer.WriteVector3(value.Bounds.Minimum);
                writer.WriteVector3(value.Bounds.Maximum);
                writer.WriteInt32(value.Vertex0);
                writer.WriteInt32(value.Vertex1);
                writer.WriteInt32(value.Vertex2);
                writer.WriteInt32(value.AdjacentPrimitive0);
                writer.WriteInt32(value.AdjacentPrimitive1);
                writer.WriteInt32(value.AdjacentPrimitive2);
            }
        }

        static int ReadCount(CanonicalReader reader)
        {
            int value = reader.ReadInt32();
            if (value < 0 || value > 1000000)
                throw new InvalidDataException($"Collision artifact count '{value}' is invalid.");
            return value;
        }

        static DeterministicCollisionPrimitiveKind ReadKind(byte value)
        {
            if (!Enum.IsDefined(typeof(DeterministicCollisionPrimitiveKind), value))
                throw new InvalidDataException($"Collision primitive kind '{value}' is invalid.");
            return (DeterministicCollisionPrimitiveKind)value;
        }

        static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }
}
