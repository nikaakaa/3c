using System.Collections.Concurrent;
using Fantasy;

namespace FrameSyncLiveSmoke;

public sealed class FrameSyncLiveSmokeClientProbe
{
    readonly ConcurrentQueue<G2C_FrameSyncHandshakeResponse> handshakeResponses = new();
    readonly ConcurrentQueue<G2C_FrameSyncInputAck> inputAcks = new();
    readonly ConcurrentQueue<G2C_FrameSyncConfirmedInputSet> confirmedInputSets = new();
    readonly ConcurrentQueue<G2C_FrameSyncCorrection> corrections = new();
    readonly ConcurrentQueue<G2C_FrameSyncDiagnostic> diagnostics = new();

    public FrameSyncLiveSmokeClientProbe(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public int HandshakeResponseCount => handshakeResponses.Count;
    public int InputAckCount => inputAcks.Count;
    public int ConfirmedInputSetCount => confirmedInputSets.Count;
    public int CorrectionCount => corrections.Count;
    public int DiagnosticCount => diagnostics.Count;
    public IReadOnlyCollection<G2C_FrameSyncHandshakeResponse> HandshakeResponses => handshakeResponses.ToArray();
    public IReadOnlyCollection<G2C_FrameSyncInputAck> InputAcks => inputAcks.ToArray();
    public IReadOnlyCollection<G2C_FrameSyncConfirmedInputSet> ConfirmedInputSets => confirmedInputSets.ToArray();
    public IReadOnlyCollection<G2C_FrameSyncCorrection> Corrections => corrections.ToArray();
    public IReadOnlyCollection<G2C_FrameSyncDiagnostic> Diagnostics => diagnostics.ToArray();

    public void Add(G2C_FrameSyncHandshakeResponse message)
    {
        handshakeResponses.Enqueue(Clone(message));
    }

    public void Add(G2C_FrameSyncInputAck message)
    {
        inputAcks.Enqueue(Clone(message));
    }

    public void Add(G2C_FrameSyncConfirmedInputSet message)
    {
        confirmedInputSets.Enqueue(Clone(message));
    }

    public void Add(G2C_FrameSyncCorrection message)
    {
        corrections.Enqueue(Clone(message));
    }

    public void Add(G2C_FrameSyncDiagnostic message)
    {
        diagnostics.Enqueue(Clone(message));
    }

    static G2C_FrameSyncHandshakeResponse Clone(G2C_FrameSyncHandshakeResponse value)
    {
        G2C_FrameSyncHandshakeResponse clone = new()
        {
            ErrorCode = value.ErrorCode,
            Accepted = value.Accepted,
            AssignedPlayerId = value.AssignedPlayerId,
            AssignedUnitId = value.AssignedUnitId
        };

        foreach (int reason in value.FailureReasons)
            clone.FailureReasons.Add(reason);

        return clone;
    }

    static G2C_FrameSyncInputAck Clone(G2C_FrameSyncInputAck value)
    {
        return new G2C_FrameSyncInputAck
        {
            Tick = value.Tick,
            PlayerId = value.PlayerId,
            UnitId = value.UnitId,
            LocalInputSequence = value.LocalInputSequence,
            ServerSequence = value.ServerSequence,
            Accepted = value.Accepted
        };
    }

    static G2C_FrameSyncConfirmedInputSet Clone(G2C_FrameSyncConfirmedInputSet value)
    {
        G2C_FrameSyncConfirmedInputSet clone = new()
        {
            Tick = value.Tick,
            ServerSequence = value.ServerSequence,
            ProtocolVersion = value.ProtocolVersion,
            ConfigHash = value.ConfigHash,
            ConfirmedTick = value.ConfirmedTick
        };

        foreach (FrameSyncInputFrameMessage input in value.Inputs)
            clone.Inputs.Add(Clone(input));

        foreach (FrameSyncConfirmedInputDiagnosticMessage diagnostic in value.Diagnostics)
            clone.Diagnostics.Add(new FrameSyncConfirmedInputDiagnosticMessage
            {
                Kind = diagnostic.Kind,
                Tick = diagnostic.Tick,
                PlayerId = diagnostic.PlayerId,
                UnitId = diagnostic.UnitId,
                LocalInputSequence = diagnostic.LocalInputSequence,
                Message = diagnostic.Message
            });

        return clone;
    }

    static G2C_FrameSyncCorrection Clone(G2C_FrameSyncCorrection value)
    {
        return new G2C_FrameSyncCorrection
        {
            Tick = value.Tick,
            RestoreTick = value.RestoreTick,
            ExpectedChecksum = value.ExpectedChecksum,
            ActualChecksum = value.ActualChecksum,
            Reason = value.Reason
        };
    }

    static G2C_FrameSyncDiagnostic Clone(G2C_FrameSyncDiagnostic value)
    {
        return new G2C_FrameSyncDiagnostic
        {
            Kind = value.Kind,
            Tick = value.Tick,
            PlayerId = value.PlayerId,
            UnitId = value.UnitId,
            DiagnosticMessage = value.DiagnosticMessage
        };
    }

    static FrameSyncInputFrameMessage Clone(FrameSyncInputFrameMessage input)
    {
        FrameSyncInputFrameMessage clone = new()
        {
            Tick = input.Tick,
            PlayerId = input.PlayerId,
            UnitId = input.UnitId,
            LocalInputSequence = input.LocalInputSequence,
            MoveIntent = Clone(input.MoveIntent),
            MoveIntentSpace = input.MoveIntentSpace,
            LookIntent = Clone(input.LookIntent),
            LookIntentSpace = input.LookIntentSpace,
            RunHeld = input.RunHeld,
            Dodge = Clone(input.Dodge),
            Attack = Clone(input.Attack),
            Jump = Clone(input.Jump),
            Interact = Clone(input.Interact),
            TargetIntent = Clone(input.TargetIntent),
            CameraBasis = Clone(input.CameraBasis),
            HasCameraBasis = input.HasCameraBasis
        };

        foreach (FrameSyncActionRequestMessage request in input.ActionRequests)
        {
            clone.ActionRequests.Add(new FrameSyncActionRequestMessage
            {
                Kind = request.Kind,
                StableActionId = request.StableActionId,
                RequestSequence = request.RequestSequence,
                Button = Clone(request.Button),
                TargetIntent = Clone(request.TargetIntent)
            });
        }

        return clone;
    }

    static FrameSyncVector2 Clone(FrameSyncVector2? value)
    {
        return new FrameSyncVector2 { X = value?.X ?? 0f, Y = value?.Y ?? 0f };
    }

    static FrameSyncVector3 Clone(FrameSyncVector3? value)
    {
        return new FrameSyncVector3 { X = value?.X ?? 0f, Y = value?.Y ?? 0f, Z = value?.Z ?? 0f };
    }

    static FrameSyncButtonFactMessage Clone(FrameSyncButtonFactMessage? value)
    {
        return new FrameSyncButtonFactMessage
        {
            Pressed = value?.Pressed ?? false,
            Held = value?.Held ?? false,
            Released = value?.Released ?? false
        };
    }

    static FrameSyncTargetIntentMessage Clone(FrameSyncTargetIntentMessage? value)
    {
        return new FrameSyncTargetIntentMessage
        {
            TargetId = value?.TargetId ?? 0,
            AimIntent = Clone(value?.AimIntent)
        };
    }

    static FrameSyncCameraBasisMessage Clone(FrameSyncCameraBasisMessage? value)
    {
        return new FrameSyncCameraBasisMessage
        {
            PlanarForward = Clone(value?.PlanarForward),
            PlanarRight = Clone(value?.PlanarRight),
            Yaw = value?.Yaw ?? 0f
        };
    }
}

public static class FrameSyncLiveSmokeProbe
{
    static readonly ConcurrentDictionary<long, FrameSyncLiveSmokeClientProbe> probes = new();

    public static FrameSyncLiveSmokeClientProbe Register(long sessionRuntimeId, string name)
    {
        FrameSyncLiveSmokeClientProbe probe = new(name);
        probes[sessionRuntimeId] = probe;
        return probe;
    }

    public static void Unregister(long sessionRuntimeId)
    {
        probes.TryRemove(sessionRuntimeId, out _);
    }

    public static void Add(long sessionRuntimeId, G2C_FrameSyncHandshakeResponse message)
    {
        if (probes.TryGetValue(sessionRuntimeId, out FrameSyncLiveSmokeClientProbe? probe))
            probe.Add(message);
    }

    public static void Add(long sessionRuntimeId, G2C_FrameSyncInputAck message)
    {
        if (probes.TryGetValue(sessionRuntimeId, out FrameSyncLiveSmokeClientProbe? probe))
            probe.Add(message);
    }

    public static void Add(long sessionRuntimeId, G2C_FrameSyncConfirmedInputSet message)
    {
        if (probes.TryGetValue(sessionRuntimeId, out FrameSyncLiveSmokeClientProbe? probe))
            probe.Add(message);
    }

    public static void Add(long sessionRuntimeId, G2C_FrameSyncCorrection message)
    {
        if (probes.TryGetValue(sessionRuntimeId, out FrameSyncLiveSmokeClientProbe? probe))
            probe.Add(message);
    }

    public static void Add(long sessionRuntimeId, G2C_FrameSyncDiagnostic message)
    {
        if (probes.TryGetValue(sessionRuntimeId, out FrameSyncLiveSmokeClientProbe? probe))
            probe.Add(message);
    }
}
