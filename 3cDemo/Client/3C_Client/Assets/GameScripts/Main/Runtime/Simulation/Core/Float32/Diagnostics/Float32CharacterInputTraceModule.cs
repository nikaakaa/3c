using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct Float32CharacterInputTraceFrame
    {
        public Float32CharacterInputTraceFrame(
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationInput input)
        {
            if (!actorId.IsValid || !tick.IsValid || input == null ||
                input.NumericProfile != Float32SimulationNumericProfile.Value ||
                input.TickSource.Kind != SimulationTickSourceKind.LocalLogic ||
                input.TickSource.SourceTick == 0 || input.Sequence != input.TickSource.SourceTick)
            {
                throw new ArgumentException("Float32 character input trace frame is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            Input = input;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterSimulationInput Input { get; }
    }

    public sealed class Float32CharacterInputTrace
    {
        readonly IReadOnlyList<Float32CharacterInputTraceFrame> m_Frames;

        public Float32CharacterInputTrace(
            string traceId,
            ActorId actorId,
            IReadOnlyList<Float32CharacterInputTraceFrame> frames)
        {
            if (string.IsNullOrWhiteSpace(traceId) || !actorId.IsValid || frames == null || frames.Count == 0)
                throw new ArgumentException("Float32 input trace identity is incomplete.");
            var values = new List<Float32CharacterInputTraceFrame>(frames);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].ActorId != actorId ||
                    i > 0 && values[i - 1].Tick.Value + 1 != values[i].Tick.Value)
                {
                    throw new ArgumentException("Float32 input trace frames must be contiguous and belong to one Actor.", nameof(frames));
                }
            }
            TraceId = traceId.Trim();
            ActorId = actorId;
            m_Frames = values.AsReadOnly();
        }

        public string TraceId { get; }
        public ActorId ActorId { get; }
        public IReadOnlyList<Float32CharacterInputTraceFrame> Frames => m_Frames;
    }

    public static class Float32CharacterInputTraceModule
    {
        static readonly List<Float32CharacterInputTraceFrame> s_RecordingFrames = new List<Float32CharacterInputTraceFrame>();
        static Float32CharacterInputTraceMode s_Mode;
        static ActorId s_ActorId;
        static Float32CharacterInputTrace s_Replay;
        static Float32CharacterInputTrace s_LastCompletedTrace;
        static int s_ReplayIndex;
        static ulong s_ReplayStartTick;
        static string s_TraceId = string.Empty;

        enum Float32CharacterInputTraceMode : byte
        {
            Idle = 0,
            Recording = 1,
            Replaying = 2
        }

        public static Float32CharacterInputTrace LastCompletedTrace => s_LastCompletedTrace;
        public static bool IsRecording => s_Mode == Float32CharacterInputTraceMode.Recording;
        public static bool IsReplayActive => s_Mode == Float32CharacterInputTraceMode.Replaying;

        public static bool IsReplayInput(CharacterSimulationInput input) =>
            input != null &&
            input.InputSourceIdentity.StartsWith("Float32InputTrace/", StringComparison.Ordinal);

        public static void PrepareRecording(ActorId actorId)
        {
            RequireIdle();
            if (!actorId.IsValid)
                throw new ArgumentException("Float32 recording ActorId is invalid.", nameof(actorId));
            ResetActiveState();
            s_ActorId = actorId;
            s_TraceId = Guid.NewGuid().ToString("N");
            s_Mode = Float32CharacterInputTraceMode.Recording;
        }

        public static Float32CharacterInputTrace StopRecording()
        {
            if (s_Mode != Float32CharacterInputTraceMode.Recording || s_RecordingFrames.Count == 0)
                throw new InvalidOperationException("Float32 input recording has no complete trace.");
            var trace = new Float32CharacterInputTrace(s_TraceId, s_ActorId, s_RecordingFrames);
            s_LastCompletedTrace = trace;
            ResetActiveState();
            return trace;
        }

        public static void PrepareReplay(Float32CharacterInputTrace trace)
        {
            RequireIdle();
            s_Replay = trace ?? throw new ArgumentNullException(nameof(trace));
            s_ActorId = trace.ActorId;
            s_TraceId = trace.TraceId;
            s_ReplayIndex = 0;
            s_ReplayStartTick = 0;
            s_Mode = Float32CharacterInputTraceMode.Replaying;
        }

        public static CharacterSimulationInput Resolve(
            SimulationInputBuildContext context,
            CharacterSimulationInput liveInput)
        {
            if (liveInput == null)
                throw new ArgumentNullException(nameof(liveInput));
            if (context.ActorId != s_ActorId || s_Mode == Float32CharacterInputTraceMode.Idle)
                return liveInput;
            if (s_Mode == Float32CharacterInputTraceMode.Recording)
            {
                if (s_RecordingFrames.Count > 0 &&
                    s_RecordingFrames[^1].Tick.Value + 1 != context.SimulationTick.Value)
                {
                    throw new InvalidOperationException("Float32 input recording Tick continuity changed.");
                }
                s_RecordingFrames.Add(new Float32CharacterInputTraceFrame(context.ActorId, context.SimulationTick, liveInput));
                return liveInput;
            }
            if (s_ReplayIndex >= s_Replay.Frames.Count)
                return liveInput;
            if (s_ReplayStartTick == 0)
                s_ReplayStartTick = context.SimulationTick.Value;
            Float32CharacterInputTraceFrame frame = s_Replay.Frames[s_ReplayIndex];
            CharacterSimulationInput result = Remap(frame, context);
            s_ReplayIndex++;
            if (s_ReplayIndex == s_Replay.Frames.Count)
                ResetActiveState();
            return result;
        }

        public static void Stop()
        {
            ResetActiveState();
        }

        public static void ClearCompletedTrace()
        {
            ResetActiveState();
            s_LastCompletedTrace = null;
        }

        static CharacterSimulationInput Remap(
            Float32CharacterInputTraceFrame frame,
            SimulationInputBuildContext context)
        {
            ulong replayTick = context.SimulationTick.Value;
            var requests = new SimulationInputRequest[frame.Input.Requests.Count];
            for (int i = 0; i < requests.Length; i++)
            {
                SimulationInputRequest request = frame.Input.Requests[i];
                requests[i] = new SimulationInputRequest(
                    request.RequestId,
                    request.Sequence,
                    RemapTick(request.SourceTick, frame.Tick.Value, replayTick),
                    RemapTick(request.ExpireSimulationTick, frame.Tick.Value, replayTick),
                    request.Priority);
            }
            return new CharacterSimulationInput(
                Float32SimulationNumericProfile.Value,
                context.Source,
                $"Float32InputTrace/{s_Replay.TraceId}",
                context.InputSequence,
                frame.Input.Values,
                requests);
        }

        static ulong RemapTick(ulong recorded, ulong recordedFrame, ulong replayFrame)
        {
            if (recorded == 0 || recorded < recordedFrame)
                return recorded;
            return checked(replayFrame + recorded - recordedFrame);
        }

        static void RequireIdle()
        {
            if (s_Mode != Float32CharacterInputTraceMode.Idle)
                throw new InvalidOperationException("Float32 character input trace is already active.");
        }

        static void ResetActiveState()
        {
            s_Mode = Float32CharacterInputTraceMode.Idle;
            s_ActorId = default;
            s_Replay = null;
            s_ReplayIndex = 0;
            s_ReplayStartTick = 0;
            s_TraceId = string.Empty;
            s_RecordingFrames.Clear();
        }
    }
}
