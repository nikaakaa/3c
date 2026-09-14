using System;

namespace ThirdPersonSimulation
{
    internal static class Float32GameplayAbilityExecutionLayoutFactory
    {
        public static GameplayAbilityExecutionLayout Create(Float32GameplayAbilityExecutionData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return new GameplayAbilityExecutionLayout(
                data.AbilityId,
                data.TickRate,
                data.Operations,
                data.Constants,
                data.ConstantInputBindings,
                data.ControlFlow,
                data.References,
                data.GraphCallFrames,
                data.StateSlots,
                data.Scopes,
                data.CatalogEntries,
                data.MotionModifiers,
                data.SourceMap,
                data.Producers,
                data.CatalogIndex,
                data.Topology);
        }
    }
}
