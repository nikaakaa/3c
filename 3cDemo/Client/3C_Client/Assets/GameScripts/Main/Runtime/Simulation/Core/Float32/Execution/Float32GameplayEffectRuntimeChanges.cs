using System;

namespace ThirdPersonSimulation
{
    internal enum PortableEffectChangeKind : byte
    {
        Lifecycle = 1,
        Attribute = 2,
        Cue = 3,
        Failure = 4
    }

    internal abstract class PortableEffectRuntimeChange
    {
        protected PortableEffectRuntimeChange(PortableEffectChangeKind kind, ulong cursor)
        {
            Kind = kind;
            Cursor = cursor;
        }

        public PortableEffectChangeKind Kind { get; }
        public ulong Cursor { get; }
    }

    internal sealed class PortableEffectLifecycleRuntimeChange : PortableEffectRuntimeChange
    {
        public PortableEffectLifecycleRuntimeChange(
            ulong cursor,
            PortableEffectDefinition definition,
            ulong instanceId,
            SimulationGameplayEffectLifecycleOperation operation,
            SimulationGameplayEffectContext context,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong lifecycleRevision,
            bool instant)
            : base(PortableEffectChangeKind.Lifecycle, cursor)
        {
            Definition = definition;
            InstanceId = instanceId;
            Operation = operation;
            Context = context;
            StartTick = startTick;
            EndTick = endTick;
            StackCount = stackCount;
            LifecycleRevision = lifecycleRevision;
            Instant = instant;
        }

        public PortableEffectDefinition Definition { get; }
        public ulong InstanceId { get; }
        public SimulationGameplayEffectLifecycleOperation Operation { get; }
        public SimulationGameplayEffectContext Context { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public int StackCount { get; }
        public ulong LifecycleRevision { get; }
        public bool Instant { get; }
    }

    internal sealed class PortableAttributeRuntimeChange : PortableEffectRuntimeChange
    {
        public PortableAttributeRuntimeChange(
            ulong cursor,
            PortableAttributeChange value,
            string causeEffectId,
            ulong causeInstanceId,
            SimulationGameplayEffectContext causeContext)
            : base(PortableEffectChangeKind.Attribute, cursor)
        {
            Value = value;
            CauseEffectId = causeEffectId ?? string.Empty;
            CauseInstanceId = causeInstanceId;
            CauseContext = causeContext;
        }

        public PortableAttributeChange Value { get; }
        public string CauseEffectId { get; }
        public ulong CauseInstanceId { get; }
        public SimulationGameplayEffectContext CauseContext { get; }
    }

    internal sealed class PortableCueRuntimeChange : PortableEffectRuntimeChange
    {
        public PortableCueRuntimeChange(ulong cursor, string cueId, PortableCueTrigger trigger, PortableEffectDefinition definition, ulong instanceId, SimulationGameplayEffectContext context)
            : base(PortableEffectChangeKind.Cue, cursor)
        {
            CueId = cueId;
            Trigger = trigger;
            Definition = definition;
            InstanceId = instanceId;
            Context = context;
        }

        public string CueId { get; }
        public PortableCueTrigger Trigger { get; }
        public PortableEffectDefinition Definition { get; }
        public ulong InstanceId { get; }
        public SimulationGameplayEffectContext Context { get; }
    }

    internal sealed class PortableEffectFailureRuntimeChange : PortableEffectRuntimeChange
    {
        public PortableEffectFailureRuntimeChange(ulong cursor, string ownerEffectId, ulong ownerInstanceId, string requestedEffectId, SimulationGameplayEffectApplyResultCode code, string reason)
            : base(PortableEffectChangeKind.Failure, cursor)
        {
            OwnerEffectId = ownerEffectId ?? string.Empty;
            OwnerInstanceId = ownerInstanceId;
            RequestedEffectId = requestedEffectId ?? string.Empty;
            Code = code;
            Reason = reason ?? string.Empty;
        }

        public string OwnerEffectId { get; }
        public ulong OwnerInstanceId { get; }
        public string RequestedEffectId { get; }
        public SimulationGameplayEffectApplyResultCode Code { get; }
        public string Reason { get; }
    }

    internal readonly struct PortableEffectCause
    {
        public PortableEffectCause(PortableEffectDefinition definition, ulong instanceId, SimulationGameplayEffectContext context)
        {
            Definition = definition;
            InstanceId = instanceId;
            Context = context;
        }

        public PortableEffectDefinition Definition { get; }
        public ulong InstanceId { get; }
        public SimulationGameplayEffectContext Context { get; }
    }

}

