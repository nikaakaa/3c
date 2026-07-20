using System;
using System.IO;
using NUnit.Framework;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Tests
{
    [TestFixture]
    public sealed class ProgramCurveTests
    {
        [Test]
        public void KeysAreStoredInCanonicalTimeOrder()
        {
            var curve = new ProgramCurve(0, 0, new[] { Key(2, 20), Key(0, 0), Key(1, 10) });

            Assert.That(curve.Keys[0].Time, Is.EqualTo(Scalar(0)));
            Assert.That(curve.Keys[1].Time, Is.EqualTo(Scalar(1)));
            Assert.That(curve.Keys[2].Time, Is.EqualTo(Scalar(2)));
        }

        [Test]
        public void DuplicateKeyTimeIsRejected()
        {
            Assert.Throws<ArgumentException>(new Action(() => new ProgramCurve(0, 0, new[] { Key(1, 10), Key(1, 20) })));
        }

        [Test]
        public void EmptyCurveReturnsFallback()
        {
            var curve = new ProgramCurve(0, 0, Array.Empty<ProgramCurveKey>());

            Assert.That(curve.Evaluate(Scalar(5), Scalar(17)), Is.EqualTo(Scalar(17)));
        }

        [Test]
        public void EvaluationClampsToBoundaryKeys()
        {
            var curve = new ProgramCurve(0, 0, new[] { Key(1, 10), Key(3, 30) });

            Assert.That(curve.Evaluate(Scalar(0), Scalar(-1)), Is.EqualTo(Scalar(10)));
            Assert.That(curve.Evaluate(Scalar(4), Scalar(-1)), Is.EqualTo(Scalar(30)));
        }

        [Test]
        public void EvaluationInterpolatesBetweenKeys()
        {
            var curve = new ProgramCurve(0, 0, new[] { Key(0, 0), Key(1, 10) });

            Assert.That(curve.Evaluate(Float32Scalar.FromSingle(0.5f), Scalar(-1)), Is.EqualTo(Scalar(5)));
        }

        [Test]
        public void CodecRoundTripsCanonicalCurve()
        {
            var curve = new ProgramCurve(2, 3, new[]
            {
                new ProgramCurveKey(Scalar(2), Scalar(20), Scalar(1), Scalar(2), Scalar(3), Scalar(4), 5),
                new ProgramCurveKey(Scalar(1), Scalar(10), Scalar(6), Scalar(7), Scalar(8), Scalar(9), 10)
            });

            ProgramCurve decoded = ProgramCurveCodec.Read(ProgramCurveCodec.Write(curve));

            Assert.That(decoded.PreWrapMode, Is.EqualTo(2));
            Assert.That(decoded.PostWrapMode, Is.EqualTo(3));
            Assert.That(decoded.Keys.Count, Is.EqualTo(2));
            AssertKey(decoded.Keys[0], curve.Keys[0]);
            AssertKey(decoded.Keys[1], curve.Keys[1]);
        }

        [Test]
        public void CodecRejectsInvalidHeaderAndTrailingPayload()
        {
            byte[] payload = ProgramCurveCodec.Write(new ProgramCurve(0, 0, new[] { Key(0, 0) }));
            byte[] invalidHeader = (byte[])payload.Clone();
            invalidHeader[0] ^= 0xff;
            byte[] trailing = new byte[payload.Length + 1];
            Buffer.BlockCopy(payload, 0, trailing, 0, payload.Length);

            Assert.Throws<InvalidDataException>(new Action(() => ProgramCurveCodec.Read(invalidHeader)));
            Assert.Throws<InvalidDataException>(new Action(() => ProgramCurveCodec.Read(trailing)));
        }

        static Float32Scalar Scalar(long value) => Float32Scalar.FromInt64(value);

        static ProgramCurveKey Key(long time, long value)
        {
            return new ProgramCurveKey(Scalar(time), Scalar(value), Float32Scalar.Zero, Float32Scalar.Zero, Float32Scalar.Zero, Float32Scalar.Zero, 0);
        }

        static void AssertKey(ProgramCurveKey actual, ProgramCurveKey expected)
        {
            Assert.That(actual.Time, Is.EqualTo(expected.Time));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
            Assert.That(actual.InTangent, Is.EqualTo(expected.InTangent));
            Assert.That(actual.OutTangent, Is.EqualTo(expected.OutTangent));
            Assert.That(actual.InWeight, Is.EqualTo(expected.InWeight));
            Assert.That(actual.OutWeight, Is.EqualTo(expected.OutWeight));
            Assert.That(actual.WeightedMode, Is.EqualTo(expected.WeightedMode));
        }
    }
}
