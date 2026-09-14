using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ThirdPersonGameplay.ScenePlay
{
    public enum BtsmtlScenePlayOperation : byte
    {
        None = 0,
        Start = 1,
        Pause = 2,
        Resume = 3,
        Reset = 4,
        Stop = 5,
        Build = 6,
        Restore = 7,
        Replay = 8,
        StartInputRecording = 9,
        StopInputRecording = 10
    }

    public enum BtsmtlScenePlayState : byte
    {
        Idle = 1,
        Checking = 2,
        NeedsBuild = 3,
        EnteringPlay = 4,
        Preparing = 5,
        Running = 6,
        Paused = 7,
        Resetting = 8,
        Stopping = 9,
        Faulted = 10,
        Building = 11
    }

    public enum BtsmtlScenePlayFailureStage : byte
    {
        None = 0,
        Request = 1,
        Scene = 2,
        Context = 3,
        Product = 4,
        EnterPlay = 5,
        Preparation = 6,
        Target = 7,
        Reset = 8,
        Stop = 9
    }

    public enum BtsmtlScenePlayCommandResultCode : byte
    {
        Accepted = 0,
        Cancelled = 1,
        RejectedBusy = 2,
        RejectedExternalPlay = 3,
        RejectedNotRunning = 4,
        RejectedOwnerMismatch = 5,
        RejectedInvalidRequest = 6,
        RejectedConfiguration = 7,
        Failed = 8
    }

    public readonly struct BtsmtlScenePlayRequestIdentity : IEquatable<BtsmtlScenePlayRequestIdentity>
    {
        public BtsmtlScenePlayRequestIdentity(Guid requestId, string scenePath, string contextId)
        {
            if (requestId == Guid.Empty)
                throw new ArgumentException("Scene Play request identity is required.", nameof(requestId));
            RequestId = requestId;
            ScenePath = Require(scenePath, nameof(scenePath));
            ContextId = Require(contextId, nameof(contextId));
        }

        public Guid RequestId { get; }
        public string ScenePath { get; }
        public string ContextId { get; }

        public bool Equals(BtsmtlScenePlayRequestIdentity other) =>
            RequestId == other.RequestId &&
            string.Equals(ScenePath, other.ScenePath, StringComparison.Ordinal) &&
            string.Equals(ContextId, other.ContextId, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is BtsmtlScenePlayRequestIdentity other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(RequestId, ScenePath, ContextId);

        public static bool operator ==(BtsmtlScenePlayRequestIdentity left, BtsmtlScenePlayRequestIdentity right) =>
            left.Equals(right);

        public static bool operator !=(BtsmtlScenePlayRequestIdentity left, BtsmtlScenePlayRequestIdentity right) =>
            !left.Equals(right);

        public override string ToString() => $"{ScenePath}|{ContextId}|{RequestId:N}";

        static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Scene Play identity must be non-empty and trimmed.", parameterName);
            return value;
        }
    }

    public sealed class BtsmtlScenePlayRequest
    {
        public BtsmtlScenePlayRequest(
            string scenePath,
            string contextId,
            Guid requestId = default,
            bool startPaused = false,
            Action<Scene> prepare = null)
        {
            Identity = new BtsmtlScenePlayRequestIdentity(
                requestId == Guid.Empty ? Guid.NewGuid() : requestId,
                scenePath,
                contextId);
            StartPaused = startPaused;
            Prepare = prepare;
        }

        public BtsmtlScenePlayRequestIdentity Identity { get; }
        public bool StartPaused { get; }
        public Action<Scene> Prepare { get; }
    }

    public readonly struct BtsmtlScenePlayStatus
    {
        public BtsmtlScenePlayStatus(
            BtsmtlScenePlayState state,
            BtsmtlScenePlayOperation operation,
            BtsmtlScenePlayRequestIdentity identity,
            ulong sceneGeneration,
            BtsmtlScenePlayFailureStage failureStage,
            string failureCode,
            string failureMessage)
        {
            State = state;
            Operation = operation;
            Identity = identity;
            SceneGeneration = sceneGeneration;
            FailureStage = failureStage;
            FailureCode = failureCode ?? string.Empty;
            FailureMessage = failureMessage ?? string.Empty;
        }

        public BtsmtlScenePlayState State { get; }
        public BtsmtlScenePlayOperation Operation { get; }
        public BtsmtlScenePlayRequestIdentity Identity { get; }
        public ulong SceneGeneration { get; }
        public BtsmtlScenePlayFailureStage FailureStage { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
        public bool IsActive =>
            State == BtsmtlScenePlayState.EnteringPlay ||
            State == BtsmtlScenePlayState.Preparing ||
            State == BtsmtlScenePlayState.Running ||
            State == BtsmtlScenePlayState.Paused ||
            State == BtsmtlScenePlayState.Resetting ||
            State == BtsmtlScenePlayState.Stopping ||
            State == BtsmtlScenePlayState.Building;
        public bool HasFailure => FailureStage != BtsmtlScenePlayFailureStage.None;

        public static BtsmtlScenePlayStatus Idle => new BtsmtlScenePlayStatus(
            BtsmtlScenePlayState.Idle,
            BtsmtlScenePlayOperation.None,
            default,
            0,
            BtsmtlScenePlayFailureStage.None,
            string.Empty,
            string.Empty);
    }

    public readonly struct BtsmtlScenePlayCommandResult
    {
        public BtsmtlScenePlayCommandResult(
            BtsmtlScenePlayCommandResultCode code,
            BtsmtlScenePlayOperation operation,
            BtsmtlScenePlayStatus status,
            string message)
        {
            Code = code;
            Operation = operation;
            Status = status;
            Message = message ?? string.Empty;
        }

        public BtsmtlScenePlayCommandResultCode Code { get; }
        public BtsmtlScenePlayOperation Operation { get; }
        public BtsmtlScenePlayStatus Status { get; }
        public string Message { get; }
        public bool Accepted => Code == BtsmtlScenePlayCommandResultCode.Accepted;
    }

    public readonly struct BtsmtlScenePlaySkillOption
    {
        public BtsmtlScenePlaySkillOption(
            string actorId,
            string skillId,
            string entryGraphAuthoringId,
            string actionProfileId,
            string sourceInputRequestId)
        {
            ActorId = actorId;
            SkillId = skillId ?? string.Empty;
            EntryGraphAuthoringId = entryGraphAuthoringId ?? string.Empty;
            ActionProfileId = actionProfileId ?? string.Empty;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
        }

        public string ActorId { get; }
        public string SkillId { get; }
        public string EntryGraphAuthoringId { get; }
        public string ActionProfileId { get; }
        public string SourceInputRequestId { get; }
    }

    public enum BtsmtlScenePlaySkillRequestResultCode : byte
    {
        Accepted = 0,
        RejectedNotRunning = 1,
        RejectedActorMissing = 2,
        RejectedSkillMissing = 3,
        RejectedInputRequestMissing = 4,
        RejectedSourceUnavailable = 5,
        RejectedSkillAmbiguous = 6,
        RejectedProgramAdoptionPending = 7
    }

    public readonly struct BtsmtlScenePlaySkillRequestResult
    {
        public BtsmtlScenePlaySkillRequestResult(
            BtsmtlScenePlaySkillRequestResultCode code,
            string actorId,
            string skillId,
            string inputRequestId,
            ulong inputSequence,
            ulong sceneGeneration,
            string message,
            Guid executionBranchId = default,
            ulong checkpointTick = 0)
        {
            Code = code;
            ActorId = actorId;
            SkillId = skillId ?? string.Empty;
            InputRequestId = inputRequestId ?? string.Empty;
            InputSequence = inputSequence;
            SceneGeneration = sceneGeneration;
            Message = message ?? string.Empty;
            ExecutionBranchId = executionBranchId;
            CheckpointTick = checkpointTick;
        }

        public BtsmtlScenePlaySkillRequestResultCode Code { get; }
        public string ActorId { get; }
        public string SkillId { get; }
        public string InputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong SceneGeneration { get; }
        public string Message { get; }
        public Guid ExecutionBranchId { get; }
        public ulong CheckpointTick { get; }
        public bool Accepted => Code == BtsmtlScenePlaySkillRequestResultCode.Accepted;
    }

    public enum BtsmtlScenePlayBuildState : byte
    {
        Idle = 0,
        Building = 1,
        Published = 2,
        Adopted = 3,
        Failed = 4
    }

    public readonly struct BtsmtlScenePlayBuildTargetStatus
    {
        public BtsmtlScenePlayBuildTargetStatus(
            string numericTargetId,
            string programId,
            string sourceRevision,
            string semanticHash,
            string programHash,
            string layoutHash,
            int sourceMapEntryCount,
            string presentationContractHash,
            string presentationProjectionRevision = "")
        {
            NumericTargetId = numericTargetId ?? string.Empty;
            ProgramId = programId ?? string.Empty;
            SourceRevision = sourceRevision ?? string.Empty;
            SemanticHash = semanticHash ?? string.Empty;
            ProgramHash = programHash ?? string.Empty;
            LayoutHash = layoutHash ?? string.Empty;
            SourceMapEntryCount = sourceMapEntryCount;
            PresentationContractHash = presentationContractHash ?? string.Empty;
            PresentationProjectionRevision = presentationProjectionRevision ?? string.Empty;
        }

        public string NumericTargetId { get; }
        public string ProgramId { get; }
        public string SourceRevision { get; }
        public string SemanticHash { get; }
        public string ProgramHash { get; }
        public string LayoutHash { get; }
        public int SourceMapEntryCount { get; }
        public string PresentationContractHash { get; }
        public string PresentationProjectionRevision { get; }
    }

    public enum BtsmtlScenePlayProgramAdoptionStatus : byte
    {
        Applied = 1,
        Deferred = 2,
        Rejected = 3
    }

    public sealed class BtsmtlScenePlayProgramAdoptionReport
    {
        public BtsmtlScenePlayProgramAdoptionReport(
            BtsmtlScenePlayProgramAdoptionStatus status,
            ulong currentProgramEpoch,
            string currentSourceRevision,
            string currentProgramCatalogHash,
            ulong requestedProgramEpoch,
            string requestedSourceRevision,
            string requestedProgramCatalogHash,
            string code,
            string message)
        {
            Status = status;
            CurrentProgramEpoch = currentProgramEpoch;
            CurrentSourceRevision = currentSourceRevision ?? string.Empty;
            CurrentProgramCatalogHash = currentProgramCatalogHash ?? string.Empty;
            RequestedProgramEpoch = requestedProgramEpoch;
            RequestedSourceRevision = requestedSourceRevision ?? string.Empty;
            RequestedProgramCatalogHash = requestedProgramCatalogHash ?? string.Empty;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public BtsmtlScenePlayProgramAdoptionStatus Status { get; }
        public ulong CurrentProgramEpoch { get; }
        public string CurrentSourceRevision { get; }
        public string CurrentProgramCatalogHash { get; }
        public ulong RequestedProgramEpoch { get; }
        public string RequestedSourceRevision { get; }
        public string RequestedProgramCatalogHash { get; }
        public string Code { get; }
        public string Message { get; }
    }

    public readonly struct BtsmtlScenePlayBuildStatus
    {
        public BtsmtlScenePlayBuildStatus(
            BtsmtlScenePlayBuildState state,
            string actorId = "",
            IReadOnlyList<BtsmtlScenePlayBuildTargetStatus> targets = null,
            string message = "",
            ulong requestedProgramEpoch = 0,
            ulong adoptedProgramEpoch = 0,
            BtsmtlScenePlayProgramAdoptionReport adoption = null,
            double buildElapsedSeconds = 0d,
            double adoptionWaitSeconds = 0d)
        {
            State = state;
            ActorId = actorId ?? string.Empty;
            Targets = targets ?? Array.Empty<BtsmtlScenePlayBuildTargetStatus>();
            Message = message ?? string.Empty;
            RequestedProgramEpoch = requestedProgramEpoch;
            AdoptedProgramEpoch = adoptedProgramEpoch;
            Adoption = adoption;
            BuildElapsedSeconds = Math.Max(0d, buildElapsedSeconds);
            AdoptionWaitSeconds = Math.Max(0d, adoptionWaitSeconds);
        }

        public BtsmtlScenePlayBuildState State { get; }
        public string ActorId { get; }
        public IReadOnlyList<BtsmtlScenePlayBuildTargetStatus> Targets { get; }
        public string Message { get; }
        public ulong RequestedProgramEpoch { get; }
        public ulong AdoptedProgramEpoch { get; }
        public BtsmtlScenePlayProgramAdoptionReport Adoption { get; }
        public double BuildElapsedSeconds { get; }
        public double AdoptionWaitSeconds { get; }
        public double TotalElapsedSeconds => BuildElapsedSeconds + AdoptionWaitSeconds;
        public bool IsActive => State == BtsmtlScenePlayBuildState.Building;
        public bool IsPublished => State == BtsmtlScenePlayBuildState.Published;
        public bool IsAdopted => State == BtsmtlScenePlayBuildState.Adopted;
        public bool HasFailure => State == BtsmtlScenePlayBuildState.Failed;

        public static BtsmtlScenePlayBuildStatus Idle =>
            new BtsmtlScenePlayBuildStatus(BtsmtlScenePlayBuildState.Idle);
    }

    public interface IBtsmtlScenePlayPreviewOperations
    {
        BtsmtlScenePlayStatus Status { get; }
        BtsmtlScenePlayBuildStatus BuildStatus { get; }
        bool SupportsInputReplay { get; }
        bool SupportsPresentationCheckpointRestore { get; }
        bool IsInputRecording { get; }
        IReadOnlyList<string> ActorIds { get; }
        System.Collections.Generic.IReadOnlyList<BtsmtlScenePlaySkillOption> SkillOptions { get; }
        event Action<BtsmtlScenePlayStatus> StatusChanged;
        BtsmtlScenePlayCommandResult Start(BtsmtlScenePlayRequest request);
        BtsmtlScenePlayCommandResult Pause();
        BtsmtlScenePlayCommandResult Resume();
        BtsmtlScenePlayCommandResult Reset();
        BtsmtlScenePlayCommandResult Stop();
        BtsmtlScenePlayCommandResult Build(string actorId);
        BtsmtlScenePlayCommandResult StartInputRecording();
        BtsmtlScenePlayCommandResult StopInputRecording();
        BtsmtlScenePlayCommandResult ResumeFromTick(ulong tick);
        BtsmtlScenePlayCommandResult ReplayInputRange(ulong fromTick, ulong toTick);
        BtsmtlScenePlaySkillRequestResult RequestSkill(string actorId, string skillId);
        void AddInterest(Guid ownerId);
        void RemoveInterest(Guid ownerId);
    }

    public static class BtsmtlScenePlayPreviewOperationsRegistry
    {
        static IBtsmtlScenePlayPreviewOperations s_Current;

        public static IBtsmtlScenePlayPreviewOperations Current => s_Current;
        public static event Action Changed;

        public static void Register(IBtsmtlScenePlayPreviewOperations operations)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));
            if (s_Current != null && !ReferenceEquals(s_Current, operations))
                throw new InvalidOperationException(
                    "Scene Play preview operations are already owned by another coordinator.");
            if (ReferenceEquals(s_Current, operations))
                return;
            s_Current = operations;
            Changed?.Invoke();
        }

        public static void Unregister(IBtsmtlScenePlayPreviewOperations operations)
        {
            if (!ReferenceEquals(s_Current, operations))
                return;
            s_Current = null;
            Changed?.Invoke();
        }
    }
}
