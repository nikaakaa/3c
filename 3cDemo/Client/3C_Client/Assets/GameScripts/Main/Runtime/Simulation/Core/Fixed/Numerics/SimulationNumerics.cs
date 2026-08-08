using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public static class FixedSimulationNumericProfile
    {
        public const int AbiVersion = 7;
        public const int FractionalBits = 32;

        public static SimulationNumericProfile Value { get; } = new SimulationNumericProfile(
            new NumericProfileId("fixed-q32.32"),
            new TargetAbiVersion(AbiVersion),
            64,
            SimulationNumericRoundingMode.FixedNearestEven,
            SimulationNumericOverflowMode.RejectOverflow,
            true);
    }

    public sealed class FixedSimulationTargetManifest
    {
        public FixedSimulationTargetManifest(
            SimulationNumericProfile profile,
            string scalarType,
            string vector2Type,
            string vector3Type,
            string yawType,
            string canonicalCodec,
            SimulationKernelSpecializationManifest kernelSpecialization)
        {
            Profile = profile;
            ScalarType = SimulationIdentity.Require(scalarType, nameof(scalarType));
            Vector2Type = SimulationIdentity.Require(vector2Type, nameof(vector2Type));
            Vector3Type = SimulationIdentity.Require(vector3Type, nameof(vector3Type));
            YawType = SimulationIdentity.Require(yawType, nameof(yawType));
            CanonicalCodec = SimulationIdentity.Require(canonicalCodec, nameof(canonicalCodec));
            KernelSpecialization = kernelSpecialization ?? throw new ArgumentNullException(nameof(kernelSpecialization));
            if (kernelSpecialization.NumericProfile != profile)
                throw new ArgumentException("Numeric Target and Kernel specialization profiles must match.", nameof(kernelSpecialization));
        }

        public SimulationNumericProfile Profile { get; }
        public string ScalarType { get; }
        public string Vector2Type { get; }
        public string Vector3Type { get; }
        public string YawType { get; }
        public string CanonicalCodec { get; }
        public SimulationKernelSpecializationManifest KernelSpecialization { get; }
    }

    public static class FixedSimulationTarget
    {
        static readonly FixedSimulationTargetManifest s_Manifest = new FixedSimulationTargetManifest(
            FixedSimulationNumericProfile.Value,
            nameof(FixedScalar),
            nameof(FixedVector2),
            nameof(FixedVector3),
            nameof(FixedYaw),
            "fixed-q32.32-le/v1",
            SimulationKernel.SpecializationManifest);

        public static FixedSimulationTargetManifest Manifest => s_Manifest;
    }

    public readonly struct FixedScalarConversion
    {
        public FixedScalarConversion(string sourceIdentity, double sourceValue, FixedScalar value)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            SourceValue = sourceValue;
            Value = value;
            AbsoluteError = Math.Abs(sourceValue - value.ToDouble());
        }

        public string SourceIdentity { get; }
        public double SourceValue { get; }
        public FixedScalar Value { get; }
        public double AbsoluteError { get; }
        public bool WasRounded => AbsoluteError > 0d;
    }

    public sealed class SimulationNumericConversionException : Exception
    {
        public SimulationNumericConversionException(string sourceIdentity, double sourceValue, Exception innerException)
            : base($"Fixed conversion failed at '{sourceIdentity}' for value '{sourceValue.ToString("R", CultureInfo.InvariantCulture)}'.", innerException)
        {
            SourceIdentity = sourceIdentity;
            SourceValue = sourceValue;
        }

        public string SourceIdentity { get; }
        public double SourceValue { get; }
    }

    public static class FixedScalarBoundary
    {
        public static FixedScalarConversion LowerAuthoring(double value, string sourceIdentity)
        {
            string identity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            try
            {
                return new FixedScalarConversion(identity, value, FixedScalar.FromDouble(value));
            }
            catch (Exception exception) when (exception is ArgumentOutOfRangeException || exception is OverflowException)
            {
                throw new SimulationNumericConversionException(identity, value, exception);
            }
        }

        public static FixedScalar ConvertExternal(double value, string sourceIdentity)
        {
            string identity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            try
            {
                return FixedScalar.FromDouble(value);
            }
            catch (Exception exception) when (exception is ArgumentOutOfRangeException || exception is OverflowException)
            {
                throw new SimulationNumericConversionException(identity, value, exception);
            }
        }
    }

    public readonly struct FixedScalar : IEquatable<FixedScalar>, IComparable<FixedScalar>
    {
        public const int FractionalBits = FixedSimulationNumericProfile.FractionalBits;
        public const long OneRaw = 1L << FractionalBits;

        FixedScalar(long raw)
        {
            Raw = raw;
        }

        public long Raw { get; }
        public static FixedScalar Zero => new FixedScalar(0L);
        public static FixedScalar One => new FixedScalar(OneRaw);
        public static FixedScalar MinValue => new FixedScalar(long.MinValue);
        public static FixedScalar MaxValue => new FixedScalar(long.MaxValue);

        public static FixedScalar FromRaw(long raw) => new FixedScalar(raw);

        public static FixedScalar FromInt64(long value)
        {
            return new FixedScalar(checked(value * OneRaw));
        }

        public static FixedScalar FromRatio(long numerator, long denominator)
        {
            if (denominator == 0)
                throw new DivideByZeroException();
            return DivideScaled(numerator, denominator);
        }

        public static FixedScalar FromDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            decimal scaled = checked((decimal)value * OneRaw);
            decimal rounded = decimal.Round(scaled, 0, MidpointRounding.ToEven);
            if (rounded < long.MinValue || rounded > long.MaxValue)
                throw new OverflowException($"Simulation Fixed value '{value.ToString("R", CultureInfo.InvariantCulture)}' exceeds Q32.32.");
            return new FixedScalar((long)rounded);
        }

        public static FixedScalar FromSingle(float value) => FromDouble(value);
        public double ToDouble() => Raw / (double)OneRaw;
        public float ToSingle() => (float)ToDouble();
        public int CompareTo(FixedScalar other) => Raw.CompareTo(other.Raw);
        public bool Equals(FixedScalar other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is FixedScalar other && Equals(other);
        public override int GetHashCode() => Raw.GetHashCode();
        public override string ToString() => ToDouble().ToString("R", CultureInfo.InvariantCulture);

        public static FixedScalar Abs(FixedScalar value)
        {
            if (value.Raw == long.MinValue)
                throw new OverflowException("Fixed absolute value overflowed.");
            return new FixedScalar(value.Raw < 0 ? -value.Raw : value.Raw);
        }

        public static FixedScalar Min(FixedScalar left, FixedScalar right) => left <= right ? left : right;
        public static FixedScalar Max(FixedScalar left, FixedScalar right) => left >= right ? left : right;

        public static FixedScalar Clamp(FixedScalar value, FixedScalar minimum, FixedScalar maximum)
        {
            if (minimum > maximum)
                throw new ArgumentException("Minimum exceeds maximum.");
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }

        public static FixedScalar Lerp(FixedScalar from, FixedScalar to, FixedScalar amount)
        {
            return from + (to - from) * Clamp(amount, Zero, One);
        }

        public static FixedScalar Sqrt(FixedScalar value)
        {
            if (value < Zero)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (value == Zero)
                return Zero;
            return new FixedScalar((long)IntegerSquareRootScaled((ulong)value.Raw));
        }

        public ulong CeilingToUInt64()
        {
            if (Raw < 0)
                throw new OverflowException("Negative Fixed value cannot convert to UInt64.");
            ulong whole = (ulong)(Raw >> FractionalBits);
            return (Raw & (OneRaw - 1)) == 0 ? whole : checked(whole + 1UL);
        }

        public int TruncateToInt32()
        {
            long whole = Raw / OneRaw;
            return checked((int)whole);
        }

        public static FixedScalar operator +(FixedScalar left, FixedScalar right) => new FixedScalar(checked(left.Raw + right.Raw));
        public static FixedScalar operator -(FixedScalar left, FixedScalar right) => new FixedScalar(checked(left.Raw - right.Raw));
        public static FixedScalar operator -(FixedScalar value)
        {
            if (value.Raw == long.MinValue)
                throw new OverflowException("Fixed negation overflowed.");
            return new FixedScalar(-value.Raw);
        }
        public static FixedScalar operator *(FixedScalar left, FixedScalar right)
        {
            return MultiplyScaled(left.Raw, right.Raw);
        }
        public static FixedScalar operator /(FixedScalar left, FixedScalar right)
        {
            if (right == Zero)
                throw new DivideByZeroException();
            return DivideScaled(left.Raw, right.Raw);
        }
        public static FixedScalar operator %(FixedScalar left, FixedScalar right)
        {
            if (right == Zero)
                throw new DivideByZeroException();
            return new FixedScalar(left.Raw % right.Raw);
        }

        public static bool operator ==(FixedScalar left, FixedScalar right) => left.Raw == right.Raw;
        public static bool operator !=(FixedScalar left, FixedScalar right) => left.Raw != right.Raw;
        public static bool operator <(FixedScalar left, FixedScalar right) => left.Raw < right.Raw;
        public static bool operator >(FixedScalar left, FixedScalar right) => left.Raw > right.Raw;
        public static bool operator <=(FixedScalar left, FixedScalar right) => left.Raw <= right.Raw;
        public static bool operator >=(FixedScalar left, FixedScalar right) => left.Raw >= right.Raw;

        static FixedScalar MultiplyScaled(long left, long right)
        {
            bool negative = left < 0 != right < 0;
            MultiplyUnsigned(AbsoluteRaw(left), AbsoluteRaw(right), out ulong high, out ulong low);
            if ((high >> FractionalBits) != 0UL)
                throw new OverflowException("Fixed arithmetic overflowed Q32.32.");

            ulong quotient = (high << FractionalBits) | (low >> FractionalBits);
            ulong remainder = low & (OneRaw - 1UL);
            return FromRoundedMagnitude(quotient, remainder, OneRaw, negative);
        }

        static FixedScalar DivideScaled(long numerator, long denominator)
        {
            ulong absoluteNumerator = AbsoluteRaw(numerator);
            ulong absoluteDenominator = AbsoluteRaw(denominator);
            bool negative = numerator < 0 != denominator < 0;
            ulong quotient = 0UL;
            ulong remainder = 0UL;

            for (int bitIndex = 95; bitIndex >= 0; bitIndex--)
            {
                ulong bit = bitIndex >= FractionalBits
                    ? (absoluteNumerator >> (bitIndex - FractionalBits)) & 1UL
                    : 0UL;
                remainder = (remainder << 1) | bit;
                if (remainder < absoluteDenominator)
                    continue;

                remainder -= absoluteDenominator;
                if (bitIndex >= 64)
                    throw new OverflowException("Fixed arithmetic overflowed Q32.32.");
                quotient |= 1UL << bitIndex;
            }

            return FromRoundedMagnitude(quotient, remainder, absoluteDenominator, negative);
        }

        static FixedScalar FromRoundedMagnitude(ulong quotient, ulong remainder, ulong denominator, bool negative)
        {
            ulong half = denominator >> 1;
            bool roundUp = (denominator & 1UL) == 0UL
                ? remainder > half || remainder == half && (quotient & 1UL) != 0UL
                : remainder > half;
            if (roundUp)
            {
                if (quotient == ulong.MaxValue)
                    throw new OverflowException("Fixed arithmetic overflowed Q32.32.");
                quotient++;
            }

            ulong limit = negative ? 0x8000000000000000UL : long.MaxValue;
            if (quotient > limit)
                throw new OverflowException("Fixed arithmetic overflowed Q32.32.");
            if (!negative)
                return new FixedScalar((long)quotient);
            return quotient == 0x8000000000000000UL
                ? new FixedScalar(long.MinValue)
                : new FixedScalar(-(long)quotient);
        }

        static ulong AbsoluteRaw(long value)
        {
            return value < 0
                ? unchecked((ulong)(-(value + 1))) + 1UL
                : (ulong)value;
        }

        static void MultiplyUnsigned(ulong left, ulong right, out ulong high, out ulong low)
        {
            ulong leftLow = (uint)left;
            ulong leftHigh = left >> 32;
            ulong rightLow = (uint)right;
            ulong rightHigh = right >> 32;
            ulong lowProduct = leftLow * rightLow;
            ulong middle = leftHigh * rightLow + (lowProduct >> 32);
            ulong middleLow = (uint)middle;
            ulong middleHigh = middle >> 32;
            middleLow += leftLow * rightHigh;
            high = leftHigh * rightHigh + middleHigh + (middleLow >> 32);
            low = (middleLow << 32) | (uint)lowProduct;
        }

        static ulong IntegerSquareRootScaled(ulong raw)
        {
            ulong root = 0UL;
            ulong remainder = 0UL;
            for (int pairIndex = 47; pairIndex >= 0; pairIndex--)
            {
                int lowBitIndex = pairIndex * 2;
                ulong pair = ShiftedRawBit(raw, lowBitIndex) |
                             (ShiftedRawBit(raw, lowBitIndex + 1) << 1);
                remainder = (remainder << 2) | pair;
                ulong candidate = (root << 2) | 1UL;
                if (remainder >= candidate)
                {
                    remainder -= candidate;
                    root = (root << 1) | 1UL;
                }
                else
                {
                    root <<= 1;
                }
            }
            return root;
        }

        static ulong ShiftedRawBit(ulong raw, int bitIndex)
        {
            if (bitIndex < FractionalBits || bitIndex >= FractionalBits + 64)
                return 0UL;
            return (raw >> (bitIndex - FractionalBits)) & 1UL;
        }
    }

    public readonly struct FixedVector2 : IEquatable<FixedVector2>
    {
        public FixedVector2(FixedScalar x, FixedScalar y)
        {
            X = x;
            Y = y;
        }

        public FixedScalar X { get; }
        public FixedScalar Y { get; }
        public static FixedVector2 Zero => new FixedVector2(FixedScalar.Zero, FixedScalar.Zero);
        public FixedScalar SqrMagnitude => X * X + Y * Y;
        public FixedScalar Magnitude => FixedScalar.Sqrt(SqrMagnitude);
        public FixedVector2 Normalized
        {
            get
            {
                FixedScalar magnitude = Magnitude;
                return magnitude == FixedScalar.Zero ? Zero : new FixedVector2(X / magnitude, Y / magnitude);
            }
        }
        public static FixedScalar Dot(FixedVector2 left, FixedVector2 right) => left.X * right.X + left.Y * right.Y;
        public bool Equals(FixedVector2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is FixedVector2 other && Equals(other);
        public override int GetHashCode() => unchecked(X.GetHashCode() * 397 ^ Y.GetHashCode());
        public override string ToString() => $"({X},{Y})";
        public static FixedVector2 operator +(FixedVector2 left, FixedVector2 right) => new FixedVector2(left.X + right.X, left.Y + right.Y);
        public static FixedVector2 operator -(FixedVector2 left, FixedVector2 right) => new FixedVector2(left.X - right.X, left.Y - right.Y);
        public static FixedVector2 operator -(FixedVector2 value) => new FixedVector2(-value.X, -value.Y);
        public static FixedVector2 operator *(FixedVector2 value, FixedScalar scale) => new FixedVector2(value.X * scale, value.Y * scale);
        public static bool operator ==(FixedVector2 left, FixedVector2 right) => left.Equals(right);
        public static bool operator !=(FixedVector2 left, FixedVector2 right) => !left.Equals(right);
    }

    public readonly struct FixedVector3 : IEquatable<FixedVector3>
    {
        public FixedVector3(FixedScalar x, FixedScalar y, FixedScalar z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public FixedScalar X { get; }
        public FixedScalar Y { get; }
        public FixedScalar Z { get; }
        public static FixedVector3 Zero => new FixedVector3(FixedScalar.Zero, FixedScalar.Zero, FixedScalar.Zero);
        public FixedScalar SqrMagnitude => X * X + Y * Y + Z * Z;
        public FixedScalar Magnitude => FixedScalar.Sqrt(SqrMagnitude);
        public FixedVector3 Normalized
        {
            get
            {
                FixedScalar magnitude = Magnitude;
                return magnitude == FixedScalar.Zero ? Zero : new FixedVector3(X / magnitude, Y / magnitude, Z / magnitude);
            }
        }
        public static FixedScalar Dot(FixedVector3 left, FixedVector3 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        public static FixedVector3 Cross(FixedVector3 left, FixedVector3 right) => new FixedVector3(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);
        public bool Equals(FixedVector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is FixedVector3 other && Equals(other);
        public override int GetHashCode() => unchecked((X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode());
        public override string ToString() => $"({X},{Y},{Z})";
        public static FixedVector3 operator +(FixedVector3 left, FixedVector3 right) => new FixedVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        public static FixedVector3 operator -(FixedVector3 left, FixedVector3 right) => new FixedVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        public static FixedVector3 operator -(FixedVector3 value) => new FixedVector3(-value.X, -value.Y, -value.Z);
        public static FixedVector3 operator *(FixedVector3 value, FixedScalar scale) => new FixedVector3(value.X * scale, value.Y * scale, value.Z * scale);
        public static bool operator ==(FixedVector3 left, FixedVector3 right) => left.Equals(right);
        public static bool operator !=(FixedVector3 left, FixedVector3 right) => !left.Equals(right);
    }

    public readonly struct FixedYaw : IEquatable<FixedYaw>
    {
        static readonly FixedScalar FullTurn = FixedScalar.FromInt64(360);
        static readonly FixedScalar HalfTurn = FixedScalar.FromInt64(180);

        public FixedYaw(FixedScalar degrees)
        {
            Degrees = Normalize(degrees);
        }

        public FixedScalar Degrees { get; }
        public static FixedYaw Zero => new FixedYaw(FixedScalar.Zero);
        public bool Equals(FixedYaw other) => Degrees == other.Degrees;
        public override bool Equals(object obj) => obj is FixedYaw other && Equals(other);
        public override int GetHashCode() => Degrees.GetHashCode();
        public override string ToString() => Degrees.ToString();
        public static bool operator ==(FixedYaw left, FixedYaw right) => left.Equals(right);
        public static bool operator !=(FixedYaw left, FixedYaw right) => !left.Equals(right);

        static FixedScalar Normalize(FixedScalar value)
        {
            FixedScalar normalized = value % FullTurn;
            if (normalized >= HalfTurn)
                normalized -= FullTurn;
            if (normalized < -HalfTurn)
                normalized += FullTurn;
            return normalized;
        }
    }

    public static class FixedAngle
    {
        static readonly long[] AtanRadians =
        {
            3373259426L, 1991351318L, 1052175346L, 534100635L,
            268086748L, 134174063L, 67103403L, 33553749L,
            16777131L, 8388597L, 4194303L, 2097152L,
            1048576L, 524288L, 262144L, 131072L,
            65536L, 32768L, 16384L, 8192L,
            4096L, 2048L, 1024L, 512L,
            256L, 128L, 64L, 32L,
            16L, 8L, 4L, 2L
        };

        const long PiRaw = 13493037705L;
        const long HalfPiRaw = 6746518852L;
        const long CordicGainRaw = 2608131496L;
        static readonly FixedScalar DegreesToRadians = FixedScalar.FromRaw(74961321L);
        static readonly FixedScalar RadiansToDegrees = FixedScalar.FromRaw(246083499208L);

        public static FixedYaw FromPlanarDirection(FixedVector2 direction) => FromPlanarDirection(direction.X, direction.Y);

        public static FixedYaw FromPlanarDirection(FixedScalar x, FixedScalar z)
        {
            if (x == FixedScalar.Zero && z == FixedScalar.Zero)
                return FixedYaw.Zero;
            long vectorX = z.Raw;
            long vectorY = x.Raw;
            long angle = 0L;
            if (vectorX < 0)
            {
                bool positiveY = vectorY >= 0;
                vectorX = checked(-vectorX);
                vectorY = checked(-vectorY);
                angle = positiveY ? PiRaw : -PiRaw;
            }
            for (int i = 0; i < AtanRadians.Length; i++)
            {
                long previousX = vectorX;
                if (vectorY > 0)
                {
                    vectorX = checked(vectorX + (vectorY >> i));
                    vectorY = checked(vectorY - (previousX >> i));
                    angle = checked(angle + AtanRadians[i]);
                }
                else
                {
                    vectorX = checked(vectorX - (vectorY >> i));
                    vectorY = checked(vectorY + (previousX >> i));
                    angle = checked(angle - AtanRadians[i]);
                }
            }
            return new FixedYaw(FixedScalar.FromRaw(angle) * RadiansToDegrees);
        }

        public static FixedScalar Delta(FixedYaw from, FixedYaw to) => new FixedYaw(to.Degrees - from.Degrees).Degrees;

        public static FixedVector3 RotatePlanar(FixedVector3 value, FixedYaw yaw)
        {
            SinCos(yaw, out FixedScalar sine, out FixedScalar cosine);
            return new FixedVector3(
                value.X * cosine + value.Z * sine,
                value.Y,
                -value.X * sine + value.Z * cosine);
        }

        public static void SinCos(FixedYaw yaw, out FixedScalar sine, out FixedScalar cosine)
        {
            long angle = (yaw.Degrees * DegreesToRadians).Raw;
            int sign = 1;
            if (angle > HalfPiRaw)
            {
                angle -= PiRaw;
                sign = -1;
            }
            else if (angle < -HalfPiRaw)
            {
                angle += PiRaw;
                sign = -1;
            }

            long x = CordicGainRaw;
            long y = 0L;
            for (int i = 0; i < AtanRadians.Length; i++)
            {
                long previousX = x;
                if (angle >= 0)
                {
                    x = checked(x - (y >> i));
                    y = checked(y + (previousX >> i));
                    angle -= AtanRadians[i];
                }
                else
                {
                    x = checked(x + (y >> i));
                    y = checked(y - (previousX >> i));
                    angle += AtanRadians[i];
                }
            }
            if (sign < 0)
            {
                x = -x;
                y = -y;
            }
            sine = FixedScalar.FromRaw(y);
            cosine = FixedScalar.FromRaw(x);
        }
    }
}
