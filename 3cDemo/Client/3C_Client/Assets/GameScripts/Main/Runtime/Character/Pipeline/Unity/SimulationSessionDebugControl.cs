using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    public enum SimulationSessionDebugCommandKind
    {
        SetRealtime = 0,
        Pause = 1,
        Step = 2,
        SetRatePlayback = 3,
        SetPresentationClock = 4,
        StartRecording = 5,
        StopRecording = 6,
        ReplayRange = 7,
        ResumeFromTick = 8
    }

    public enum SimulationSessionDebugCommandResultCode
    {
        Accepted = 0,
        RejectedTargetMissing = 1,
        RejectedTargetMismatch = 2,
        RejectedAmbiguousTarget = 3,
        RejectedTargetEnded = 4,
        RejectedUnsupportedCapability = 5,
        RejectedTickSystemMissing = 6,
        RejectedInvalidCommand = 7
    }

    public readonly struct SimulationSessionDebugCapabilityDescriptor
    {
        public SimulationSessionDebugCapabilityDescriptor(
            bool supportsTickDrive,
            bool supportsPresentationClock,
            bool isLocalFixedCandidate,
            bool supportsRecording,
            bool supportsReplay,
            bool supportsReplayArtifact,
            bool deterministicReplay)
        {
            SupportsTickDrive = supportsTickDrive;
            SupportsPresentationClock = supportsPresentationClock;
            IsLocalFixedCandidate = isLocalFixedCandidate;
            SupportsRecording = supportsRecording;
            SupportsReplay = supportsReplay;
            SupportsReplayArtifact = supportsReplayArtifact;
            DeterministicReplay = deterministicReplay;
        }

        public bool SupportsTickDrive { get; }
        public bool SupportsPresentationClock { get; }
        public bool IsLocalFixedCandidate { get; }
        public bool SupportsRecording { get; }
        public bool SupportsReplay { get; }
        public bool SupportsReplayArtifact { get; }
        public bool DeterministicReplay { get; }
    }

    public readonly struct SimulationSessionDebugTargetIdentity
    {
        public SimulationSessionDebugTargetIdentity(
            string targetKey,
            string displayName,
            int hostInstanceId,
            SimulationSessionId sessionId,
            SimulationSessionCompositionIdentity compositionIdentity,
            SimulationPipelineIdentity pipeline,
            ProgramCatalogHash programCatalogHash,
            NumericProfileId numericProfileId,
            SimulationComponentIdentity sessionSource,
            SimulationComponentIdentity worldSolver)
        {
            TargetKey = Require(targetKey, nameof(targetKey));
            DisplayName = Require(displayName, nameof(displayName));
            HostInstanceId = hostInstanceId;
            SessionId = sessionId;
            CompositionIdentity = compositionIdentity;
            Pipeline = pipeline;
            ProgramCatalogHash = programCatalogHash;
            NumericProfileId = numericProfileId;
            SessionSource = sessionSource;
            WorldSolver = worldSolver;
        }

        public string TargetKey { get; }
        public string DisplayName { get; }
        public int HostInstanceId { get; }
        public SimulationSessionId SessionId { get; }
        public SimulationSessionCompositionIdentity CompositionIdentity { get; }
        public SimulationPipelineIdentity Pipeline { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public NumericProfileId NumericProfileId { get; }
        public SimulationComponentIdentity SessionSource { get; }
        public SimulationComponentIdentity WorldSolver { get; }

        public static SimulationSessionDebugTargetIdentity Create(
            int hostInstanceId,
            SimulationSessionCompositionDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            string targetKey = string.Join("|",
                hostInstanceId.ToString(),
                descriptor.SessionId.ToString(),
                descriptor.Identity.ToString(),
                descriptor.Pipeline.Hash.ToString(),
                descriptor.ProgramCatalogHash.ToString(),
                descriptor.SessionSource.ToString(),
                descriptor.WorldSolver.ToString());
            string displayName = $"{descriptor.SessionId} | {descriptor.NumericProfileId} | {hostInstanceId}";
            return new SimulationSessionDebugTargetIdentity(
                targetKey,
                displayName,
                hostInstanceId,
                descriptor.SessionId,
                descriptor.Identity,
                descriptor.Pipeline,
                descriptor.ProgramCatalogHash,
                descriptor.NumericProfileId,
                descriptor.SessionSource,
                descriptor.WorldSolver);
        }

        static string Require(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Debug target identity is required.", parameter);
            return value.Trim();
        }
    }

    public readonly struct SimulationSessionDebugCommand
    {
        SimulationSessionDebugCommand(
            SimulationSessionDebugCommandKind kind,
            string targetKey,
            ulong stepCount,
            float rateMultiplier,
            GameplayPresentationDebugClockMode presentationClockMode,
            ulong fromTick,
            ulong toTick,
            ulong sequence)
        {
            Kind = kind;
            TargetKey = targetKey ?? string.Empty;
            StepCount = stepCount;
            RateMultiplier = rateMultiplier;
            PresentationClockMode = presentationClockMode;
            FromTick = fromTick;
            ToTick = toTick;
            Sequence = sequence;
        }

        public SimulationSessionDebugCommandKind Kind { get; }
        public string TargetKey { get; }
        public ulong StepCount { get; }
        public float RateMultiplier { get; }
        public GameplayPresentationDebugClockMode PresentationClockMode { get; }
        public ulong FromTick { get; }
        public ulong ToTick { get; }
        public ulong Sequence { get; }

        public static SimulationSessionDebugCommand SetRealtime(string targetKey) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.SetRealtime, targetKey, 0, 1f, GameplayPresentationDebugClockMode.LivePresentation, 0, 0, 0);

        public static SimulationSessionDebugCommand Pause(string targetKey) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.Pause, targetKey, 0, 1f, GameplayPresentationDebugClockMode.LivePresentation, 0, 0, 0);

        public static SimulationSessionDebugCommand Step(string targetKey, ulong stepCount) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.Step, targetKey, Math.Max(1UL, stepCount), 1f, GameplayPresentationDebugClockMode.LivePresentation, 0, 0, 0);

        public static SimulationSessionDebugCommand SetRatePlayback(string targetKey, float rateMultiplier) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.SetRatePlayback, targetKey, 0, Math.Max(0.01f, rateMultiplier), GameplayPresentationDebugClockMode.LivePresentation, 0, 0, 0);

        public static SimulationSessionDebugCommand SetPresentationClock(string targetKey, GameplayPresentationDebugClockMode mode) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.SetPresentationClock, targetKey, 0, 1f, mode, 0, 0, 0);

        public static SimulationSessionDebugCommand StartRecording(string targetKey) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.StartRecording, targetKey, 0, 1f, GameplayPresentationDebugClockMode.LivePresentation, 0, 0, 0);

        public static SimulationSessionDebugCommand StopRecording(string targetKey) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.StopRecording, targetKey, 0, 1f, GameplayPresentationDebugClockMode.LivePresentation, 0, 0, 0);

        public static SimulationSessionDebugCommand ReplayRange(string targetKey, ulong fromTick, ulong toTick) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.ReplayRange, targetKey, 0, 1f, GameplayPresentationDebugClockMode.LivePresentation, fromTick, toTick, 0);

        public static SimulationSessionDebugCommand ResumeFromTick(string targetKey, ulong tick) =>
            new SimulationSessionDebugCommand(SimulationSessionDebugCommandKind.ResumeFromTick, targetKey, 0, 1f, GameplayPresentationDebugClockMode.LivePresentation, tick, tick, 0);

        public SimulationSessionDebugCommand WithSequence(ulong sequence)
        {
            return new SimulationSessionDebugCommand(
                Kind,
                TargetKey,
                StepCount,
                RateMultiplier,
                PresentationClockMode,
                FromTick,
                ToTick,
                sequence);
        }
    }

    public readonly struct SimulationSessionDebugCommandResult
    {
        public SimulationSessionDebugCommandResult(
            SimulationSessionDebugCommandResultCode code,
            ulong commandSequence,
            string message)
        {
            Code = code;
            CommandSequence = commandSequence;
            Message = message ?? string.Empty;
        }

        public SimulationSessionDebugCommandResultCode Code { get; }
        public ulong CommandSequence { get; }
        public string Message { get; }
        public bool Accepted => Code == SimulationSessionDebugCommandResultCode.Accepted;

        public static SimulationSessionDebugCommandResult AcceptedResult(ulong sequence) =>
            new SimulationSessionDebugCommandResult(SimulationSessionDebugCommandResultCode.Accepted, sequence, string.Empty);

        public static SimulationSessionDebugCommandResult Rejected(
            SimulationSessionDebugCommandResultCode code,
            ulong sequence,
            string message) =>
            new SimulationSessionDebugCommandResult(code, sequence, message);
    }

    public readonly struct SimulationSessionDebugStatusSnapshot
    {
        public SimulationSessionDebugStatusSnapshot(
            SimulationSessionDebugTargetIdentity identity,
            SimulationSessionDebugCapabilityDescriptor capability,
            GameplayTickDriveStatusSnapshot driveStatus,
            bool tickSystemAvailable,
            SimulationSessionLifecycleState lifecycleState,
            ulong latestOuterTick,
            bool recording,
            ulong historyOldestTick,
            ulong historyLatestTick,
            ulong latestCheckpointTick,
            string latestHash,
            SimulationSessionFailure failure)
        {
            Identity = identity;
            Capability = capability;
            DriveStatus = driveStatus;
            TickSystemAvailable = tickSystemAvailable;
            LifecycleState = lifecycleState;
            LatestOuterTick = latestOuterTick;
            Recording = recording;
            HistoryOldestTick = historyOldestTick;
            HistoryLatestTick = historyLatestTick;
            LatestCheckpointTick = latestCheckpointTick;
            LatestHash = latestHash ?? string.Empty;
            Failure = failure;
        }

        public SimulationSessionDebugTargetIdentity Identity { get; }
        public SimulationSessionDebugCapabilityDescriptor Capability { get; }
        public GameplayTickDriveStatusSnapshot DriveStatus { get; }
        public bool TickSystemAvailable { get; }
        public SimulationSessionLifecycleState LifecycleState { get; }
        public ulong LatestOuterTick { get; }
        public bool Recording { get; }
        public ulong HistoryOldestTick { get; }
        public ulong HistoryLatestTick { get; }
        public ulong LatestCheckpointTick { get; }
        public string LatestHash { get; }
        public SimulationSessionFailure Failure { get; }
        public string FailureSummary => Failure?.ToString() ?? string.Empty;
    }

    public interface ISimulationSessionDebugControlPort
    {
        SimulationSessionDebugTargetIdentity Identity { get; }
        SimulationSessionDebugCapabilityDescriptor Capability { get; }
        SimulationSessionDebugStatusSnapshot Status { get; }
        bool TrySubmit(SimulationSessionDebugCommand command, out SimulationSessionDebugCommandResult result);
    }

    public static class LocalSimulationDebugControlService
    {
        static readonly List<ISimulationSessionDebugControlPort> Ports = new List<ISimulationSessionDebugControlPort>();
        static ulong s_CommandSequence;

        public static event Action TargetsChanged;

        public static IReadOnlyList<SimulationSessionDebugStatusSnapshot> CaptureStatusSnapshots()
        {
            var values = new List<SimulationSessionDebugStatusSnapshot>(Ports.Count);
            for (int i = 0; i < Ports.Count; i++)
            {
                ISimulationSessionDebugControlPort port = Ports[i];
                if (port != null)
                    values.Add(port.Status);
            }
            values.Sort((left, right) => string.CompareOrdinal(left.Identity.DisplayName, right.Identity.DisplayName));
            return new ReadOnlyCollection<SimulationSessionDebugStatusSnapshot>(values);
        }

        public static void Register(ISimulationSessionDebugControlPort port)
        {
            if (port == null)
                throw new ArgumentNullException(nameof(port));
            for (int i = 0; i < Ports.Count; i++)
            {
                if (ReferenceEquals(Ports[i], port))
                    return;
                if (string.Equals(Ports[i].Identity.TargetKey, port.Identity.TargetKey, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Debug target '{port.Identity.TargetKey}' is already registered.");
            }
            Ports.Add(port);
            TargetsChanged?.Invoke();
        }

        public static void Unregister(ISimulationSessionDebugControlPort port)
        {
            if (port == null)
                return;
            if (Ports.Remove(port))
                TargetsChanged?.Invoke();
        }

        public static bool TrySubmit(
            string targetKey,
            SimulationSessionDebugCommand command,
            out SimulationSessionDebugCommandResult result)
        {
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                result = SimulationSessionDebugCommandResult.Rejected(
                    SimulationSessionDebugCommandResultCode.RejectedTargetMissing,
                    0,
                    "Debug target key is required.");
                return false;
            }

            ISimulationSessionDebugControlPort match = null;
            int matchCount = 0;
            for (int i = 0; i < Ports.Count; i++)
            {
                ISimulationSessionDebugControlPort port = Ports[i];
                if (port == null || !string.Equals(port.Identity.TargetKey, targetKey, StringComparison.Ordinal))
                    continue;
                match = port;
                matchCount++;
            }

            if (matchCount == 0)
            {
                result = SimulationSessionDebugCommandResult.Rejected(
                    SimulationSessionDebugCommandResultCode.RejectedTargetMissing,
                    0,
                    "Debug target is not active.");
                return false;
            }

            if (matchCount > 1)
            {
                result = SimulationSessionDebugCommandResult.Rejected(
                    SimulationSessionDebugCommandResultCode.RejectedAmbiguousTarget,
                    0,
                    "Debug target key matched more than one active Session.");
                return false;
            }

            s_CommandSequence++;
            bool accepted = match.TrySubmit(command.WithSequence(s_CommandSequence), out result);
            TargetsChanged?.Invoke();
            return accepted;
        }
    }

    sealed class SimulationSessionHostDebugControlPort : ISimulationSessionDebugControlPort
    {
        readonly SimulationSessionHost m_Host;
        readonly SimulationSessionDebugTargetIdentity m_Identity;
        readonly SimulationSessionDebugCapabilityDescriptor m_Capability;

        public SimulationSessionHostDebugControlPort(SimulationSessionHost host, SimulationSessionCompositionDescriptor descriptor)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            m_Host = host;
            m_Identity = SimulationSessionDebugTargetIdentity.Create(host.GetInstanceID(), descriptor);
            bool localFixedCandidate = string.Equals(descriptor.NumericProfileId.Value, "fixed-q32.32", StringComparison.Ordinal) &&
                descriptor.SessionSource.ToString().IndexOf("local", StringComparison.OrdinalIgnoreCase) >= 0;
            m_Capability = new SimulationSessionDebugCapabilityDescriptor(
                true,
                true,
                localFixedCandidate,
                false,
                false,
                false,
                false);
        }

        public SimulationSessionDebugTargetIdentity Identity => m_Identity;
        public SimulationSessionDebugCapabilityDescriptor Capability => m_Capability;
        public SimulationSessionDebugStatusSnapshot Status => BuildStatus();

        public bool TrySubmit(SimulationSessionDebugCommand command, out SimulationSessionDebugCommandResult result)
        {
            if (!string.Equals(command.TargetKey, m_Identity.TargetKey, StringComparison.Ordinal))
            {
                result = SimulationSessionDebugCommandResult.Rejected(
                    SimulationSessionDebugCommandResultCode.RejectedTargetMismatch,
                    command.Sequence,
                    "Command target identity does not match this Session.");
                return false;
            }
            if (m_Host.LifecycleState != SimulationSessionLifecycleState.Active)
            {
                result = SimulationSessionDebugCommandResult.Rejected(
                    SimulationSessionDebugCommandResultCode.RejectedTargetEnded,
                    command.Sequence,
                    "Debug target is not active.");
                return false;
            }
            if (!GameplayTickSystem.IsInitialized)
            {
                result = SimulationSessionDebugCommandResult.Rejected(
                    SimulationSessionDebugCommandResultCode.RejectedTickSystemMissing,
                    command.Sequence,
                    "GameplayTickSystem is not initialized.");
                return false;
            }

            switch (command.Kind)
            {
                case SimulationSessionDebugCommandKind.SetRealtime:
                    return SubmitTickCommand(GameplayTickDriveCommand.SetRealtime(), command.Sequence, out result);
                case SimulationSessionDebugCommandKind.Pause:
                    return SubmitTickCommand(GameplayTickDriveCommand.Pause(), command.Sequence, out result);
                case SimulationSessionDebugCommandKind.Step:
                    return SubmitTickCommand(GameplayTickDriveCommand.Step(command.StepCount), command.Sequence, out result);
                case SimulationSessionDebugCommandKind.SetRatePlayback:
                    return SubmitTickCommand(GameplayTickDriveCommand.SetRatePlayback(command.RateMultiplier), command.Sequence, out result);
                case SimulationSessionDebugCommandKind.SetPresentationClock:
                    return SubmitTickCommand(GameplayTickDriveCommand.SetPresentationClock(command.PresentationClockMode), command.Sequence, out result);
                case SimulationSessionDebugCommandKind.StartRecording:
                case SimulationSessionDebugCommandKind.StopRecording:
                case SimulationSessionDebugCommandKind.ReplayRange:
                case SimulationSessionDebugCommandKind.ResumeFromTick:
                    result = SimulationSessionDebugCommandResult.Rejected(
                        SimulationSessionDebugCommandResultCode.RejectedUnsupportedCapability,
                        command.Sequence,
                        "Local Fixed recording/replay pipeline is not installed on this Session.");
                    return false;
                default:
                    result = SimulationSessionDebugCommandResult.Rejected(
                        SimulationSessionDebugCommandResultCode.RejectedInvalidCommand,
                        command.Sequence,
                        "Debug command is invalid.");
                    return false;
            }
        }

        bool SubmitTickCommand(
            GameplayTickDriveCommand tickCommand,
            ulong sequence,
            out SimulationSessionDebugCommandResult result)
        {
            if (!GameplayTickSystem.EnqueueDriveCommand(tickCommand))
            {
                result = SimulationSessionDebugCommandResult.Rejected(
                    SimulationSessionDebugCommandResultCode.RejectedTickSystemMissing,
                    sequence,
                    "GameplayTickSystem rejected the debug drive command.");
                return false;
            }
            result = SimulationSessionDebugCommandResult.AcceptedResult(sequence);
            return true;
        }

        SimulationSessionDebugStatusSnapshot BuildStatus()
        {
            SimulationSessionDiagnosticsSnapshot diagnostics = m_Host.Diagnostics;
            bool tickSystemAvailable = GameplayTickSystem.IsInitialized;
            GameplayTickDriveStatusSnapshot driveStatus = tickSystemAvailable
                ? GameplayTickSystem.Current.DriveStatus
                : default;
            return new SimulationSessionDebugStatusSnapshot(
                m_Identity,
                m_Capability,
                driveStatus,
                tickSystemAvailable,
                m_Host.LifecycleState,
                diagnostics?.LatestOuterTick ?? 0,
                false,
                0,
                0,
                0,
                string.Empty,
                m_Host.Failure ?? diagnostics?.Failure);
        }
    }
}
