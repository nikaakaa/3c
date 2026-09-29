using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonGameplay.Tick;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal readonly struct TimelinePresentationExecutionContext
    {
        public TimelinePresentationExecutionContext(
            ulong actionInstanceId,
            SimulationTick tick,
            ActivationId activation)
        {
            ActionInstanceId = actionInstanceId;
            Tick = tick;
            Activation = activation;
        }

        public ulong ActionInstanceId { get; }
        public SimulationTick Tick { get; }
        public ActivationId Activation { get; }
    }

    internal readonly struct TimelinePresentationGraphCameraOutput
    {
        internal TimelinePresentationGraphCameraOutput(TimelineRuntimePresentationFrame frame, string callerId,
            bool marker, int cycle, long time, in Float32PresentationGraphOutputIdentity identity,
            string producer, PresentationCameraRequest activation,
            PresentationCameraRequest retirement, bool retiring)
        {
            Frame = frame;
            CallerId = callerId;
            Marker = marker;
            Cycle = cycle;
            Time = time;
            Identity = identity;
            Producer = producer;
            Activation = activation;
            Retirement = retirement;
            Retiring = retiring;
        }
        internal readonly TimelineRuntimePresentationFrame Frame;
        internal readonly string CallerId;
        internal readonly bool Marker;
        internal readonly int Cycle;
        internal readonly long Time;
        internal readonly Float32PresentationGraphOutputIdentity Identity;
        internal readonly string Producer;
        internal readonly PresentationCameraRequest Activation;
        internal readonly PresentationCameraRequest Retirement;
        internal readonly bool Retiring;
    }
}
