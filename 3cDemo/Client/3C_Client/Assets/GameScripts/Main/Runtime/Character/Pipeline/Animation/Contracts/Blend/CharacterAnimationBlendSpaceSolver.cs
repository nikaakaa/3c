using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterAnimationBlendSpaceCompiledPosition
    {
        public CharacterAnimationBlendSpaceCompiledPosition(CharacterAnimationBlendSpaceSampleId sampleId, float x, float y)
        {
            if (!sampleId.IsValid || !float.IsFinite(x) || !float.IsFinite(y))
                throw new ArgumentException("Blend Space compiled position is invalid.");
            SampleId = sampleId;
            X = x;
            Y = y;
        }

        public CharacterAnimationBlendSpaceSampleId SampleId { get; }
        public float X { get; }
        public float Y { get; }
    }

    public enum CharacterAnimationBlendSpaceSolveFailure : byte
    {
        None = 0,
        InvalidParameter = 1,
        InvalidPlan = 2,
        DegenerateResult = 3,
        CapacityExceeded = 4
    }

    public sealed class CharacterAnimationBlendSpaceWeightPage
    {
        readonly CharacterAnimationBlendSpaceSampleId[] m_SampleIds;
        readonly float[] m_Weights;
        readonly float[] m_Scratch;

        public CharacterAnimationBlendSpaceWeightPage(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_SampleIds = new CharacterAnimationBlendSpaceSampleId[capacity];
            m_Weights = new float[capacity];
            m_Scratch = new float[capacity];
        }

        public int Capacity => m_Weights.Length;
        public int Count { get; private set; }
        public CharacterAnimationBlendSpaceSampleId GetSampleId(int index) => index >= 0 && index < Count ? m_SampleIds[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public float GetWeight(int index) => index >= 0 && index < Count ? m_Weights[index] : throw new ArgumentOutOfRangeException(nameof(index));

        internal float[] Scratch => m_Scratch;

        internal void Reset()
        {
            Count = 0;
            Array.Clear(m_Scratch, 0, m_Scratch.Length);
        }

        internal void Add(CharacterAnimationBlendSpaceSampleId sampleId, float weight)
        {
            if (Count >= Capacity || !sampleId.IsValid || !float.IsFinite(weight) || weight <= 0f)
                throw new InvalidOperationException("Blend Space weight page write is invalid.");
            m_SampleIds[Count] = sampleId;
            m_Weights[Count] = weight;
            Count++;
        }
    }

    public sealed class CharacterAnimationBlendSpaceSolverPlan
    {
        readonly CharacterAnimationBlendSpaceCompiledPosition[] m_Positions;
        readonly float[] m_FactorsX;
        readonly float[] m_FactorsY;
        readonly float[] m_Magnitudes;

        public CharacterAnimationBlendSpaceSolverPlan(
            CharacterAnimationBlendSpaceMode mode,
            CharacterAnimationBlendSpaceCompiledPosition[] positions)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpaceMode), mode) || positions == null || positions.Length == 0)
                throw new ArgumentException("Blend Space solver plan is invalid.");
            Mode = mode;
            m_Positions = (CharacterAnimationBlendSpaceCompiledPosition[])positions.Clone();
            Array.Sort(m_Positions, ComparePositions);
            m_FactorsX = new float[m_Positions.Length * m_Positions.Length];
            m_FactorsY = new float[m_FactorsX.Length];
            m_Magnitudes = mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D ? new float[m_Positions.Length] : Array.Empty<float>();
            CompileFactors();
        }

        CharacterAnimationBlendSpaceSolverPlan(
            CharacterAnimationBlendSpaceMode mode,
            CharacterAnimationBlendSpaceCompiledPosition[] positions,
            float[] factorsX,
            float[] factorsY,
            float[] magnitudes)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpaceMode), mode) || positions == null || positions.Length == 0 ||
                factorsX == null || factorsY == null || factorsX.Length != positions.Length * positions.Length || factorsY.Length != factorsX.Length ||
                mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D && (magnitudes == null || magnitudes.Length != positions.Length))
                throw new ArgumentException("Compiled Blend Space solver plan is invalid.");
            Mode = mode;
            m_Positions = (CharacterAnimationBlendSpaceCompiledPosition[])positions.Clone();
            m_FactorsX = (float[])factorsX.Clone();
            m_FactorsY = (float[])factorsY.Clone();
            m_Magnitudes = mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D ? (float[])magnitudes.Clone() : Array.Empty<float>();
        }

        public CharacterAnimationBlendSpaceMode Mode { get; }
        public int SampleCount => m_Positions.Length;
        public CharacterAnimationBlendSpaceCompiledPosition GetPosition(int index) => m_Positions[index];
        public float GetCompiledFactorX(int sourceIndex, int targetIndex) => GetFactorX(sourceIndex, targetIndex);
        public float GetCompiledFactorY(int sourceIndex, int targetIndex) => GetFactorY(sourceIndex, targetIndex);
        public float GetCompiledMagnitude(int index) => Mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D ? GetMagnitude(index) : 0f;

        public static CharacterAnimationBlendSpaceSolverPlan FromCompiled(
            CharacterAnimationBlendSpaceMode mode,
            CharacterAnimationBlendSpaceCompiledPosition[] positions,
            float[] factorsX,
            float[] factorsY,
            float[] magnitudes) => new CharacterAnimationBlendSpaceSolverPlan(mode, positions, factorsX, factorsY, magnitudes);

        internal float GetFactorX(int sourceIndex, int targetIndex) => m_FactorsX[sourceIndex * SampleCount + targetIndex];
        internal float GetFactorY(int sourceIndex, int targetIndex) => m_FactorsY[sourceIndex * SampleCount + targetIndex];
        internal float GetMagnitude(int index) => m_Magnitudes[index];

        void CompileFactors()
        {
            if (Mode == CharacterAnimationBlendSpaceMode.Linear1D)
            {
                for (int i = 1; i < m_Positions.Length; i++)
                {
                    if (m_Positions[i - 1].X >= m_Positions[i].X)
                        throw new ArgumentException("Linear Blend Space positions must be unique.");
                }
                return;
            }
            if (Mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D)
            {
                for (int i = 0; i < SampleCount; i++)
                    m_Magnitudes[i] = Magnitude(m_Positions[i].X, m_Positions[i].Y);
            }
            for (int i = 0; i < SampleCount; i++)
            {
                for (int j = i + 1; j < SampleCount; j++)
                {
                    float x;
                    float y;
                    if (Mode == CharacterAnimationBlendSpaceMode.FreeformCartesian2D)
                    {
                        x = m_Positions[j].X - m_Positions[i].X;
                        y = m_Positions[j].Y - m_Positions[i].Y;
                    }
                    else
                    {
                        float averageMagnitude = (m_Magnitudes[i] + m_Magnitudes[j]) * 0.5f;
                        if (averageMagnitude <= 0f)
                            throw new ArgumentException("Directional Blend Space has multiple zero samples.");
                        x = (m_Magnitudes[j] - m_Magnitudes[i]) / averageMagnitude;
                        y = SignedAngle(m_Positions[i].X, m_Positions[i].Y, m_Positions[j].X, m_Positions[j].Y) * 2f;
                    }
                    float squared = x * x + y * y;
                    if (!float.IsFinite(squared) || squared <= 0f)
                        throw new ArgumentException("Blend Space positions are degenerate.");
                    x /= squared;
                    y /= squared;
                    int forward = i * SampleCount + j;
                    int reverse = j * SampleCount + i;
                    m_FactorsX[forward] = x;
                    m_FactorsY[forward] = y;
                    m_FactorsX[reverse] = -x;
                    m_FactorsY[reverse] = -y;
                }
            }
        }

        static int ComparePositions(CharacterAnimationBlendSpaceCompiledPosition left, CharacterAnimationBlendSpaceCompiledPosition right)
        {
            if (left.X < right.X)
                return -1;
            if (left.X > right.X)
                return 1;
            if (left.Y < right.Y)
                return -1;
            if (left.Y > right.Y)
                return 1;
            return left.SampleId.CompareTo(right.SampleId);
        }

        static float Magnitude(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        internal static float SignedAngle(float ax, float ay, float bx, float by)
        {
            if (ax == 0f && ay == 0f || bx == 0f && by == 0f)
                return 0f;
            return (float)Math.Atan2(ax * by - ay * bx, ax * bx + ay * by);
        }
    }

    public static class CharacterAnimationBlendSpaceWeightEvaluator
    {
        const float MinimumWeight = 0.01f;

        public static bool Evaluate(
            CharacterAnimationBlendSpaceSolverPlan plan,
            float x,
            float y,
            CharacterAnimationBlendSpaceWeightPage output,
            out CharacterAnimationBlendSpaceSolveFailure failure)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Reset();
            if (plan == null || plan.SampleCount == 0)
            {
                failure = CharacterAnimationBlendSpaceSolveFailure.InvalidPlan;
                return false;
            }
            if (!float.IsFinite(x) || !float.IsFinite(y))
            {
                failure = CharacterAnimationBlendSpaceSolveFailure.InvalidParameter;
                return false;
            }
            if (output.Capacity < plan.SampleCount)
            {
                failure = CharacterAnimationBlendSpaceSolveFailure.CapacityExceeded;
                return false;
            }
            float[] weights = output.Scratch;
            bool solved = plan.Mode == CharacterAnimationBlendSpaceMode.Linear1D
                ? SolveLinear(plan, x, weights)
                : SolveGradientBand(plan, x, y, weights);
            if (!solved || !Normalize(plan, weights, output))
            {
                output.Reset();
                failure = CharacterAnimationBlendSpaceSolveFailure.DegenerateResult;
                return false;
            }
            failure = CharacterAnimationBlendSpaceSolveFailure.None;
            return true;
        }

        static bool SolveLinear(CharacterAnimationBlendSpaceSolverPlan plan, float parameter, float[] weights)
        {
            if (plan.SampleCount == 1)
            {
                weights[0] = 1f;
                return true;
            }
            if (parameter <= plan.GetPosition(0).X)
            {
                weights[0] = 1f;
                return true;
            }
            int last = plan.SampleCount - 1;
            if (parameter >= plan.GetPosition(last).X)
            {
                weights[last] = 1f;
                return true;
            }
            for (int i = 1; i < plan.SampleCount; i++)
            {
                float next = plan.GetPosition(i).X;
                if (parameter > next)
                    continue;
                float previous = plan.GetPosition(i - 1).X;
                float t = (parameter - previous) / (next - previous);
                weights[i - 1] = 1f - t;
                weights[i] = t;
                return true;
            }
            return false;
        }

        static bool SolveGradientBand(CharacterAnimationBlendSpaceSolverPlan plan, float x, float y, float[] weights)
        {
            if (plan.SampleCount == 1)
            {
                weights[0] = 1f;
                return true;
            }
            float parameterMagnitude = plan.Mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D
                ? (float)Math.Sqrt(x * x + y * y)
                : 0f;
            for (int i = 0; i < plan.SampleCount; i++)
            {
                CharacterAnimationBlendSpaceCompiledPosition position = plan.GetPosition(i);
                float deltaX;
                float deltaY;
                if (plan.Mode == CharacterAnimationBlendSpaceMode.FreeformCartesian2D)
                {
                    deltaX = x - position.X;
                    deltaY = y - position.Y;
                }
                else
                {
                    deltaX = parameterMagnitude - plan.GetMagnitude(i);
                    deltaY = CharacterAnimationBlendSpaceSolverPlan.SignedAngle(position.X, position.Y, x, y) * 2f;
                }
                float weight = 1f;
                for (int j = 0; j < plan.SampleCount; j++)
                {
                    if (j == i)
                        continue;
                    float factorX = plan.GetFactorX(i, j);
                    float factorY = plan.GetFactorY(i, j);
                    if (plan.Mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D)
                    {
                        float averageMagnitude = (plan.GetMagnitude(j) + plan.GetMagnitude(i)) * 0.5f;
                        if (averageMagnitude <= 0f)
                            return false;
                        factorX *= 1f;
                        float candidate = 1f - (deltaX / averageMagnitude * factorX + deltaY * factorY);
                        if (candidate < weight)
                            weight = candidate;
                    }
                    else
                    {
                        float candidate = 1f - (deltaX * factorX + deltaY * factorY);
                        if (candidate < weight)
                            weight = candidate;
                    }
                }
                weights[i] = weight < MinimumWeight ? 0f : weight;
            }
            return true;
        }

        static bool Normalize(CharacterAnimationBlendSpaceSolverPlan plan, float[] weights, CharacterAnimationBlendSpaceWeightPage output)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (!float.IsFinite(weights[i]) || weights[i] < 0f)
                    return false;
                total += weights[i];
            }
            if (!float.IsFinite(total) || total <= 0f)
                return false;
            for (int i = 0; i < weights.Length; i++)
            {
                float normalized = weights[i] / total;
                if (normalized > 0f)
                    output.Add(plan.GetPosition(i).SampleId, normalized);
            }
            return output.Count > 0;
        }
    }
}
