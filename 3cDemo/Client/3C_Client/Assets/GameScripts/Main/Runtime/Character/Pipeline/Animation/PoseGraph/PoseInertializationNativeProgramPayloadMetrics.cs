using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class PoseInertializationNativeProgramPayloadMetrics
    {
        internal static long CalculateDoublePageResidentPayloadBytes(
            PoseInertializationNativeProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            long pagePayloadBytes = checked(
                PayloadBytes(program.States) +
                PayloadBytes(program.HistoryPoses) +
                PayloadBytes(program.HistoryVelocities) +
                PayloadBytes(program.HistoryParameters) +
                PayloadBytes(program.HistoryParameterAvailability) +
                PayloadBytes(program.HistoryLeftFeet) +
                PayloadBytes(program.HistoryRightFeet) +
                PayloadBytes(program.HistoryHasFeet) +
                PayloadBytes(program.AccumulatorLeftFeet) +
                PayloadBytes(program.AccumulatorRightFeet) +
                PayloadBytes(program.AccumulatorHasFeet) +
                PayloadBytes(program.PositionResiduals) +
                PayloadBytes(program.RotationResiduals) +
                PayloadBytes(program.ScaleResiduals) +
                PayloadBytes(program.LinearVelocityResiduals) +
                PayloadBytes(program.AngularVelocityResiduals) +
                PayloadBytes(program.ScaleVelocityResiduals) +
                PayloadBytes(program.ParameterResiduals));
            return checked(pagePayloadBytes * 2);
        }

        static long PayloadBytes<T>(NativeArray<T> values)
            where T : unmanaged =>
            checked((long)UnsafeUtility.SizeOf<T>() * values.Length);
    }
}
