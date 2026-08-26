using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.Fixed
{
    public enum FixedCharacterInputTraceMode : byte
    {
        Idle = 0,
        Recording = 1,
        Replaying = 2,
        Completed = 3,
        Faulted = 4
    }

    public readonly struct FixedCharacterInputTraceFrame
    {
        public FixedCharacterInputTraceFrame(
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationInput input)
        {
            if (!actorId.IsValid || !tick.IsValid || input == null ||
                input.NumericProfile != FixedSimulationNumericProfile.Value ||
                input.TickSource.Kind != SimulationTickSourceKind.LocalLogic ||
                string.IsNullOrEmpty(input.TickSource.ClockId) ||
                input.TickSource.SourceTick == 0 ||
                input.Sequence != input.TickSource.SourceTick)
            {
                throw new ArgumentException("Fixed character input trace frame is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            Input = input;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterSimulationInput Input { get; }
    }

    public sealed class FixedCharacterInputTrace
    {
        readonly ReadOnlyCollection<FixedCharacterInputTraceFrame> m_Frames;

        public FixedCharacterInputTrace(
            string traceId,
            ActorId actorId,
            ProgramHash programHash,
            int tickRate,
            IEnumerable<FixedCharacterInputTraceFrame> frames)
        {
            if (string.IsNullOrWhiteSpace(traceId) || !actorId.IsValid || !programHash.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed character input trace identity is incomplete.");
            var values = new List<FixedCharacterInputTraceFrame>(frames ?? throw new ArgumentNullException(nameof(frames)));
            if (values.Count == 0)
                throw new ArgumentException("Fixed character input trace requires at least one frame.", nameof(frames));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].ActorId != actorId ||
                    i > 0 && values[i - 1].Tick.Value + 1 != values[i].Tick.Value)
                {
                    throw new ArgumentException("Fixed character input trace frames must be contiguous and belong to one Actor.", nameof(frames));
                }
            }
            TraceId = traceId.Trim();
            ActorId = actorId;
            ProgramHash = programHash;
            TickRate = tickRate;
            m_Frames = values.AsReadOnly();
        }

        public string TraceId { get; }
        public ActorId ActorId { get; }
        public ProgramHash ProgramHash { get; }
        public int TickRate { get; }
        public IReadOnlyList<FixedCharacterInputTraceFrame> Frames => m_Frames;
    }

    public readonly struct FixedCharacterInputTraceStatus
    {
        public FixedCharacterInputTraceStatus(
            FixedCharacterInputTraceMode mode,
            string traceId,
            string actorId,
            int frameCount,
            int replayedFrameCount,
            string message)
        {
            Mode = mode;
            TraceId = traceId ?? string.Empty;
            ActorId = actorId ?? string.Empty;
            FrameCount = frameCount;
            ReplayedFrameCount = replayedFrameCount;
            Message = message ?? string.Empty;
        }

        public FixedCharacterInputTraceMode Mode { get; }
        public string TraceId { get; }
        public string ActorId { get; }
        public int FrameCount { get; }
        public int ReplayedFrameCount { get; }
        public string Message { get; }
    }

    public static class FixedCharacterInputTraceModule
    {
        static readonly List<FixedCharacterInputTraceFrame> s_RecordingFrames =
            new List<FixedCharacterInputTraceFrame>();

        static FixedCharacterInputTraceMode s_Mode;
        static ActorId s_ActorId;
        static ProgramHash s_ProgramHash;
        static int s_TickRate;
        static FixedCharacterInputTrace s_Replay;
        static ulong s_ReplayStartTick;
        static int s_ReplayIndex;
        static string s_TraceId = string.Empty;
        static string s_Message = string.Empty;

        public static FixedCharacterInputTraceStatus Status => new FixedCharacterInputTraceStatus(
            s_Mode,
            s_TraceId,
            s_ActorId.IsValid ? s_ActorId.Value : string.Empty,
            s_Mode == FixedCharacterInputTraceMode.Recording
                ? s_RecordingFrames.Count
                : s_Replay?.Frames.Count ?? 0,
            s_ReplayIndex,
            s_Message);

        public static void StartRecording(ActorId actorId)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Fixed character input recording ActorId is invalid.", nameof(actorId));
            RequireIdle();
            ResetState();
            s_ActorId = actorId;
            s_TraceId = Guid.NewGuid().ToString("N");
            s_Mode = FixedCharacterInputTraceMode.Recording;
            s_Message = "Waiting for the first canonical Fixed input frame.";
        }

        public static FixedCharacterInputTrace StopRecording()
        {
            if (s_Mode != FixedCharacterInputTraceMode.Recording)
                throw new InvalidOperationException("Fixed character input recording is not active.");
            if (s_RecordingFrames.Count == 0 || !s_ProgramHash.IsValid || s_TickRate <= 0)
                throw new InvalidOperationException("Fixed character input recording has no canonical frames.");
            var trace = new FixedCharacterInputTrace(
                s_TraceId,
                s_ActorId,
                s_ProgramHash,
                s_TickRate,
                s_RecordingFrames);
            ResetState();
            return trace;
        }

        public static void StartReplay(FixedCharacterInputTrace trace)
        {
            if (trace == null)
                throw new ArgumentNullException(nameof(trace));
            RequireIdle();
            ResetState();
            s_Replay = trace;
            s_ActorId = trace.ActorId;
            s_ProgramHash = trace.ProgramHash;
            s_TickRate = trace.TickRate;
            s_TraceId = trace.TraceId;
            s_Mode = FixedCharacterInputTraceMode.Replaying;
            s_Message = "Waiting for the first canonical Fixed input frame.";
        }

        public static void Stop()
        {
            ResetState();
        }

        public static CharacterSimulationInput Resolve(
            FixedCharacterInputBuildContext context,
            ProgramHash programHash,
            CharacterSimulationInput liveInput)
        {
            if (liveInput == null)
                throw new ArgumentNullException(nameof(liveInput));
            if (context.ActorId != s_ActorId || s_Mode == FixedCharacterInputTraceMode.Idle)
                return liveInput;
            try
            {
                return s_Mode switch
                {
                    FixedCharacterInputTraceMode.Recording => Record(context, programHash, liveInput),
                    FixedCharacterInputTraceMode.Replaying => Replay(context, programHash),
                    FixedCharacterInputTraceMode.Completed => HoldLastReplayFrame(context, programHash),
                    FixedCharacterInputTraceMode.Faulted => throw new InvalidOperationException(s_Message),
                    _ => liveInput
                };
            }
            catch (Exception exception)
            {
                s_Mode = FixedCharacterInputTraceMode.Faulted;
                s_Message = exception.Message;
                throw;
            }
        }

        static CharacterSimulationInput Record(
            FixedCharacterInputBuildContext context,
            ProgramHash programHash,
            CharacterSimulationInput liveInput)
        {
            if (s_RecordingFrames.Count == 0)
            {
                s_ProgramHash = programHash;
                s_TickRate = context.TickRate;
            }
            else if (!programHash.Equals(s_ProgramHash) || context.TickRate != s_TickRate ||
                     s_RecordingFrames[s_RecordingFrames.Count - 1].Tick.Value + 1 != context.SimulationTick.Value)
            {
                throw new InvalidOperationException("Fixed character input recording identity or Tick continuity changed.");
            }
            s_RecordingFrames.Add(new FixedCharacterInputTraceFrame(context.ActorId, context.SimulationTick, liveInput));
            s_Message = $"Recorded {s_RecordingFrames.Count} canonical Fixed input frames.";
            return liveInput;
        }

        static CharacterSimulationInput Replay(FixedCharacterInputBuildContext context, ProgramHash programHash)
        {
            ValidateReplayContext(context, programHash);
            if (s_ReplayStartTick == 0)
            {
                if (context.SimulationTick != s_Replay.Frames[0].Tick)
                    throw new InvalidOperationException("Fixed character input replay did not start on the recorded first Tick.");
                s_ReplayStartTick = context.SimulationTick.Value;
            }
            ulong expectedTick = checked(s_ReplayStartTick + (ulong)s_ReplayIndex);
            if (context.SimulationTick.Value != expectedTick)
                throw new InvalidOperationException("Fixed character input replay Tick continuity changed.");
            FixedCharacterInputTraceFrame frame = s_Replay.Frames[s_ReplayIndex];
            CharacterSimulationInput result = Remap(frame, context);
            s_ReplayIndex++;
            if (s_ReplayIndex == s_Replay.Frames.Count)
            {
                s_Mode = FixedCharacterInputTraceMode.Completed;
                s_Message = $"Replayed all {s_ReplayIndex} canonical Fixed input frames.";
            }
            else
            {
                s_Message = $"Replayed {s_ReplayIndex}/{s_Replay.Frames.Count} canonical Fixed input frames.";
            }
            return result;
        }

        static CharacterSimulationInput HoldLastReplayFrame(
            FixedCharacterInputBuildContext context,
            ProgramHash programHash)
        {
            ValidateReplayContext(context, programHash);
            FixedCharacterInputTraceFrame frame = s_Replay.Frames[s_Replay.Frames.Count - 1];
            return new CharacterSimulationInput(
                FixedSimulationNumericProfile.Value,
                context.Source,
                $"FixedInputTrace/{s_Replay.TraceId}",
                context.InputSequence,
                frame.Input.Values,
                Array.Empty<SimulationInputRequest>());
        }

        static CharacterSimulationInput Remap(
            FixedCharacterInputTraceFrame frame,
            FixedCharacterInputBuildContext context)
        {
            var requests = new SimulationInputRequest[frame.Input.Requests.Count];
            for (int i = 0; i < requests.Length; i++)
            {
                SimulationInputRequest request = frame.Input.Requests[i];
                requests[i] = new SimulationInputRequest(
                    request.RequestId,
                    request.Sequence,
                    RemapTick(request.SourceTick, frame.Tick.Value, context.SimulationTick.Value),
                    RemapTick(request.ExpireSimulationTick, frame.Tick.Value, context.SimulationTick.Value),
                    request.Priority);
            }
            return new CharacterSimulationInput(
                FixedSimulationNumericProfile.Value,
                context.Source,
                $"FixedInputTrace/{s_Replay.TraceId}",
                context.InputSequence,
                frame.Input.Values,
                requests);
        }

        static ulong RemapTick(ulong recordedTick, ulong recordedFrameTick, ulong replayFrameTick)
        {
            if (recordedTick == 0)
                return 0;
            if (recordedTick >= recordedFrameTick)
                return checked(replayFrameTick + recordedTick - recordedFrameTick);
            ulong age = recordedFrameTick - recordedTick;
            if (age >= replayFrameTick)
                throw new InvalidOperationException("Fixed character input request predates the replay clock.");
            return replayFrameTick - age;
        }

        static void ValidateReplayContext(FixedCharacterInputBuildContext context, ProgramHash programHash)
        {
            if (!programHash.Equals(s_ProgramHash))
                throw new InvalidOperationException("Fixed character input replay ProgramHash does not match the recording.");
            if (context.TickRate != s_TickRate)
                throw new InvalidOperationException("Fixed character input replay TickRate does not match the recording.");
        }

        static void RequireIdle()
        {
            if (s_Mode != FixedCharacterInputTraceMode.Idle)
                throw new InvalidOperationException($"Fixed character input trace is already {s_Mode}.");
        }

        static void ResetState()
        {
            s_Mode = FixedCharacterInputTraceMode.Idle;
            s_ActorId = default;
            s_ProgramHash = default;
            s_TickRate = 0;
            s_Replay = null;
            s_ReplayStartTick = 0;
            s_ReplayIndex = 0;
            s_TraceId = string.Empty;
            s_Message = string.Empty;
            s_RecordingFrames.Clear();
        }
    }
}
