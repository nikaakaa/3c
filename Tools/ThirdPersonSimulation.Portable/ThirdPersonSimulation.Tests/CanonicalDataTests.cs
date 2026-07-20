using System;
using System.IO;
using NUnit.Framework;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Tests
{
    [TestFixture]
    public sealed class CanonicalDataTests
    {
        [Test]
        public void PrimitivePayloadRoundTrips()
        {
            using var writer = new CanonicalWriter();
            writer.WriteByte(0xaf);
            writer.WriteBoolean(true);
            writer.WriteBoolean(false);
            writer.WriteInt32(int.MinValue);
            writer.WriteUInt32(uint.MaxValue);
            writer.WriteInt64(long.MinValue);
            writer.WriteUInt64(ulong.MaxValue);
            writer.WriteDouble(123.5d);
            writer.WriteString("角色");
            writer.WriteBytes(new byte[] { 1, 2, 3 });

            var reader = new CanonicalReader(writer.ToArray());

            Assert.That(reader.ReadByte(), Is.EqualTo(0xaf));
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadBoolean(), Is.False);
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MaxValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MinValue));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MaxValue));
            Assert.That(reader.ReadDouble(), Is.EqualTo(123.5d));
            Assert.That(reader.ReadString(), Is.EqualTo("角色"));
            Assert.That(reader.ReadBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(reader.Remaining, Is.Zero);
            Assert.DoesNotThrow(new Action(reader.RequireComplete));
        }

        [Test]
        public void InvalidBooleanIsRejected()
        {
            var reader = new CanonicalReader(new byte[] { 2 });

            Assert.Throws<InvalidDataException>(new Action(() => reader.ReadBoolean()));
        }

        [Test]
        public void NonFiniteDoubleIsRejectedByWriterAndReader()
        {
            using var writer = new CanonicalWriter();

            Assert.Throws<ArgumentOutOfRangeException>(new Action(() => writer.WriteDouble(double.NaN)));
            Assert.Throws<ArgumentOutOfRangeException>(new Action(() => writer.WriteDouble(double.PositiveInfinity)));

            var reader = new CanonicalReader(new byte[] { 0, 0, 0, 0, 0, 0, 240, 127 });
            Assert.Throws<InvalidDataException>(new Action(() => reader.ReadDouble()));
        }

        [Test]
        public void NegativeLengthIsRejected()
        {
            using var writer = new CanonicalWriter();
            writer.WriteInt32(-1);
            var reader = new CanonicalReader(writer.ToArray());

            Assert.Throws<InvalidDataException>(new Action(() => reader.ReadBytes()));
        }

        [Test]
        public void TruncatedPayloadIsRejected()
        {
            using var writer = new CanonicalWriter();
            writer.WriteInt32(2);
            writer.WriteByte(1);
            var reader = new CanonicalReader(writer.ToArray());

            Assert.Throws<EndOfStreamException>(new Action(() => reader.ReadBytes()));
        }

        [Test]
        public void TrailingBytesAreRejected()
        {
            var reader = new CanonicalReader(new byte[] { 1, 0 });
            Assert.That(reader.ReadBoolean(), Is.True);

            Assert.Throws<InvalidDataException>(new Action(reader.RequireComplete));
        }
    }
}
