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
        Restore = 7,
        Replay = 8,
        StartInputRecording = 9,
        StopInputRecording = 10
    }

    public enum BtsmtlScenePlayState : byte
    {
        Idle = 1,
        Checking = 2,
        EnteringPlay = 4,
        Preparing = 5,
        Running = 6,
        Paused = 7,
        Resetting = 8,
        Stopping = 9,
        Faulted = 10
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
            State == BtsmtlScenePlayState.Stopping;
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
            string admissionProfileId,
            string sourceInputRequestId)
        {
            ActorId = actorId;
            SkillId = skillId ?? string.Empty;
            EntryGraphAuthoringId = entryGraphAuthoringId ?? string.Empty;
            AdmissionProfileId = admissionProfileId ?? string.Empty;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
        }

        public string ActorId { get; }
        public string SkillId { get; }
        public string EntryGraphAuthoringId { get; }
        public string AdmissionProfileId { get; }
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
        RejectedSkillAmbiguous = 6
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

    public interface IBtsmtlScenePlayPreviewOperations
    {
        BtsmtlScenePlayStatus Status { get; }
        bool SupportsInputReplay { get; }
        bool SupportsPresentationCheckpointRestore { get; }
        bool IsInputRecording { get; }
        System.Collections.Generic.IReadOnlyList<BtsmtlScenePlaySkillOption> SkillOptions { get; }
        event Action<BtsmtlScenePlayStatus> StatusChanged;
        BtsmtlScenePlayCommandResult Start(BtsmtlScenePlayRequest request);
        BtsmtlScenePlayCommandResult Pause();
        BtsmtlScenePlayCommandResult Resume();
        BtsmtlScenePlayCommandResult Reset();
        BtsmtlScenePlayCommandResult Stop();
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
