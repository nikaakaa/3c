using System;
using System.Diagnostics;

namespace ThirdPersonPerformance.Instrumentation
{
    public static class PerformanceInstrumentationContextRuntime
    {
        const int DefaultDepth = 32;

        [ThreadStatic]
        static PerformanceInstrumentationContext[] s_Stack;
        [ThreadStatic]
        static PerformanceInstrumentationContext s_Current;
        [ThreadStatic]
        static int s_Depth;
        [ThreadStatic]
        static bool s_Enabled;

        public static PerformanceInstrumentationContext Current => s_Current;

        public static void Prepare(int maxDepth = DefaultDepth)
        {
            if (maxDepth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));
            s_Stack = new PerformanceInstrumentationContext[maxDepth];
            s_Current = default(PerformanceInstrumentationContext);
            s_Depth = 0;
            s_Enabled = true;
        }

        public static void Clear()
        {
            s_Current = default(PerformanceInstrumentationContext);
            s_Depth = 0;
            s_Enabled = false;
            s_Stack = null;
        }

        [Conditional(PerformanceInstrumentationIdentity.Define)]
        public static void BeginFrame(ulong renderFrame)
        {
            if (!s_Enabled)
                return;
            Push();
            s_Current = new PerformanceInstrumentationContext(
                renderFrame,
                PerformanceInstrumentationIdentity.Unavailable,
                PerformanceInstrumentationIdentity.Unavailable,
                PerformanceInstrumentationIdentity.Unavailable,
                PerformanceInstrumentationIdentity.Unavailable,
                PerformanceInstrumentationContextFlags.RenderFrame);
        }

        [Conditional(PerformanceInstrumentationIdentity.Define)]
        public static void BeginLogicTick(ulong logicTick)
        {
            if (!s_Enabled)
                return;
            Push();
            s_Current = new PerformanceInstrumentationContext(
                s_Current.RenderFrame,
                logicTick,
                PerformanceInstrumentationIdentity.Unavailable,
                PerformanceInstrumentationIdentity.Unavailable,
                PerformanceInstrumentationIdentity.Unavailable,
                s_Current.Flags | PerformanceInstrumentationContextFlags.LogicTick);
        }

        [Conditional(PerformanceInstrumentationIdentity.Define)]
        public static void BeginActor(ulong actorId, ulong programIdentity, ulong pipelineIdentity)
        {
            if (!s_Enabled)
                return;
            Push();
            s_Current = new PerformanceInstrumentationContext(
                s_Current.RenderFrame,
                s_Current.LogicTick,
                actorId,
                programIdentity,
                pipelineIdentity,
                s_Current.Flags |
                PerformanceInstrumentationContextFlags.Actor |
                PerformanceInstrumentationContextFlags.Program |
                PerformanceInstrumentationContextFlags.Pipeline);
        }

        [Conditional(PerformanceInstrumentationIdentity.Define)]
        public static void EndFrame()
        {
            if (s_Enabled)
                Pop();
        }

        [Conditional(PerformanceInstrumentationIdentity.Define)]
        public static void EndLogicTick()
        {
            if (s_Enabled)
                Pop();
        }

        [Conditional(PerformanceInstrumentationIdentity.Define)]
        public static void EndActor()
        {
            if (s_Enabled)
                Pop();
        }

        static void Push()
        {
            if (s_Stack == null)
                s_Stack = new PerformanceInstrumentationContext[DefaultDepth];
            if (s_Depth >= s_Stack.Length)
                throw new InvalidOperationException("Performance instrumentation context depth exceeded.");
            s_Stack[s_Depth++] = s_Current;
        }

        static void Pop()
        {
            if (s_Depth <= 0)
                throw new InvalidOperationException("Performance instrumentation context scope is unbalanced.");
            s_Current = s_Stack[--s_Depth];
        }
    }
}
