using System;
using System.Collections.Generic;

namespace ThirdPersonCamera
{
    public enum CameraBindingPreparationStatus : byte
    {
        NotRequired = 0,
        Pending = 1,
        Ready = 2,
        Missing = 3,
        Invalid = 4,
        Failed = 5
    }

    public enum CameraBindingFailureCode : byte
    {
        None = 0,
        RequestInvalid = 1,
        ProjectionMissing = 2,
        ProjectionInvalid = 3,
        RigMissing = 4,
        TargetBindingsMissing = 5,
        TargetBindingInvalid = 6,
        EnvironmentQueryMissing = 7
    }

    public readonly struct CameraBindingPreparationRequest
    {
        public CameraBindingPreparationRequest(
            string requestId,
            string requestedSourceId,
            string requestedSourceRevision,
            CharacterCameraProjectionPayload projection,
            ICameraRigAdapter rigAdapter,
            IReadOnlyList<CameraTargetBinding> targetBindings,
            ICameraEnvironmentQuery environmentQuery)
        {
            RequestId = requestId ?? string.Empty;
            RequestedSourceId = requestedSourceId ?? string.Empty;
            RequestedSourceRevision = requestedSourceRevision ?? string.Empty;
            Projection = projection;
            RigAdapter = rigAdapter;
            TargetBindings = targetBindings;
            EnvironmentQuery = environmentQuery;
        }

        public string RequestId { get; }
        public string RequestedSourceId { get; }
        public string RequestedSourceRevision { get; }
        public CharacterCameraProjectionPayload Projection { get; }
        public ICameraRigAdapter RigAdapter { get; }
        public IReadOnlyList<CameraTargetBinding> TargetBindings { get; }
        public ICameraEnvironmentQuery EnvironmentQuery { get; }
    }

    public sealed class CameraRuntimeBinding
    {
        internal CameraRuntimeBinding(
            string bindingId,
            string requestId,
            string requestedSourceId,
            string requestedSourceRevision,
            CharacterCameraProjectionPayload projection,
            ICameraRigAdapter rigAdapter,
            CameraTargetBinding[] targetBindings,
            ICameraEnvironmentQuery environmentQuery)
        {
            BindingId = bindingId;
            RequestId = requestId;
            RequestedSourceId = requestedSourceId;
            RequestedSourceRevision = requestedSourceRevision;
            Projection = projection;
            RigAdapter = rigAdapter;
            TargetBindings = targetBindings;
            EnvironmentQuery = environmentQuery;
        }

        public string BindingId { get; }
        public string RequestId { get; }
        public string RequestedSourceId { get; }
        public string RequestedSourceRevision { get; }
        public CharacterCameraProjectionPayload Projection { get; }
        public ICameraRigAdapter RigAdapter { get; }
        public IReadOnlyList<CameraTargetBinding> TargetBindings { get; }
        public ICameraEnvironmentQuery EnvironmentQuery { get; }
        public string ProfileId => Projection.ProfileId;
        public string ProfileRevision => Projection.ProfileRevision;

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(BindingId) ||
                string.IsNullOrWhiteSpace(RequestId) ||
                string.IsNullOrWhiteSpace(RequestedSourceId) ||
                string.IsNullOrWhiteSpace(RequestedSourceRevision) ||
                Projection == null ||
                RigAdapter == null ||
                TargetBindings == null ||
                Projection.Collision.Enabled && EnvironmentQuery == null)
            {
                throw new InvalidOperationException("Camera runtime binding is incomplete.");
            }
            Projection.RequireValid();
        }

        public CameraBindingAdoptedResult Adopt(
            string actorId,
            string instanceId)
        {
            if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(instanceId))
            {
                return CameraBindingAdoptedResult.CreateFailed(
                    BindingId,
                    ProfileId,
                    ProfileRevision,
                    "Camera adopted binding requires ActorId and InstanceId.");
            }
            RequireValid();
            return CameraBindingAdoptedResult.CreateAdopted(
                actorId,
                instanceId,
                BindingId,
                ProfileId,
                ProfileRevision);
        }
    }

    public readonly struct CameraBindingPreparationResult
    {
        CameraBindingPreparationResult(
            string requestId,
            string requestedSourceId,
            string requestedSourceRevision,
            CameraBindingPreparationStatus status,
            CameraBindingFailureCode failureCode,
            string failureMessage,
            CameraRuntimeBinding preparedBinding)
        {
            RequestId = requestId ?? string.Empty;
            RequestedSourceId = requestedSourceId ?? string.Empty;
            RequestedSourceRevision = requestedSourceRevision ?? string.Empty;
            Status = status;
            FailureCode = failureCode;
            FailureMessage = failureMessage ?? string.Empty;
            PreparedBinding = preparedBinding;
        }

        public string RequestId { get; }
        public string RequestedSourceId { get; }
        public string RequestedSourceRevision { get; }
        public CameraBindingPreparationStatus Status { get; }
        public CameraBindingFailureCode FailureCode { get; }
        public string FailureMessage { get; }
        public CameraRuntimeBinding PreparedBinding { get; }
        public bool IsReady => Status == CameraBindingPreparationStatus.Ready && PreparedBinding != null;

        internal static CameraBindingPreparationResult Ready(
            CameraBindingPreparationRequest request,
            CameraRuntimeBinding binding) =>
            new CameraBindingPreparationResult(
                request.RequestId,
                request.RequestedSourceId,
                request.RequestedSourceRevision,
                CameraBindingPreparationStatus.Ready,
                CameraBindingFailureCode.None,
                string.Empty,
                binding);

        public static CameraBindingPreparationResult Failure(
            CameraBindingPreparationRequest request,
            CameraBindingPreparationStatus status,
            CameraBindingFailureCode code,
            string message) =>
            new CameraBindingPreparationResult(
                request.RequestId,
                request.RequestedSourceId,
                request.RequestedSourceRevision,
                status,
                code,
                message,
                null);
    }

    public static class CameraRuntimeBindingPreparation
    {
        public static CameraBindingPreparationResult Prepare(
            in CameraBindingPreparationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestId) ||
                string.IsNullOrWhiteSpace(request.RequestedSourceId) ||
                string.IsNullOrWhiteSpace(request.RequestedSourceRevision))
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Invalid,
                    CameraBindingFailureCode.RequestInvalid,
                    "Camera binding request identity is incomplete.");
            }
            if (request.Projection == null)
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Missing,
                    CameraBindingFailureCode.ProjectionMissing,
                    "Camera binding request has no Projection.");
            }
            if (request.RigAdapter == null)
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Missing,
                    CameraBindingFailureCode.RigMissing,
                    "Camera binding request has no Rig Adapter.");
            }
            if (request.TargetBindings == null)
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Missing,
                    CameraBindingFailureCode.TargetBindingsMissing,
                    "Camera binding request has no target binding collection.");
            }
            try
            {
                request.Projection.RequireValid();
            }
            catch (Exception exception)
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Invalid,
                    CameraBindingFailureCode.ProjectionInvalid,
                    exception.Message);
            }
            if (request.Projection.Collision.Enabled && request.EnvironmentQuery == null)
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Missing,
                    CameraBindingFailureCode.EnvironmentQueryMissing,
                    "Camera binding requires an environment query while collision is enabled.");
            }
            var bindings = new CameraTargetBinding[request.TargetBindings.Count];
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < bindings.Length; i++)
            {
                CameraTargetBinding binding = request.TargetBindings[i];
                if (binding == null ||
                    string.IsNullOrWhiteSpace(binding.Key) ||
                    !string.Equals(binding.Key, binding.Key.Trim(), StringComparison.Ordinal) ||
                    binding.Target == null ||
                    !keys.Add(binding.Key))
                {
                    return CameraBindingPreparationResult.Failure(
                        request,
                        CameraBindingPreparationStatus.Invalid,
                        CameraBindingFailureCode.TargetBindingInvalid,
                        $"Camera target binding #{i} is missing, invalid or duplicated.");
                }
                bindings[i] = binding;
            }
            for (int i = 0; i < request.Projection.TargetSlots.Count; i++)
            {
                CameraTargetSlotPayload slot = request.Projection.TargetSlots[i];
                if (slot == null || !slot.Required)
                    continue;
                if (!keys.Contains(slot.AnchorKey) ||
                    !keys.Contains(slot.AimPointKey) ||
                    !keys.Contains(slot.PreferredBoneKey))
                {
                    return CameraBindingPreparationResult.Failure(
                        request,
                        CameraBindingPreparationStatus.Invalid,
                        CameraBindingFailureCode.TargetBindingInvalid,
                        $"Required Camera target slot '{slot?.SlotId ?? "Missing"}' has an unbound point.");
                }
            }
            string bindingId =
                $"camera-binding|{request.Projection.ProfileId}|{request.Projection.ProfileRevision}";
            var prepared = new CameraRuntimeBinding(
                bindingId,
                request.RequestId,
                request.RequestedSourceId,
                request.RequestedSourceRevision,
                request.Projection,
                request.RigAdapter,
                bindings,
                request.EnvironmentQuery);
            return CameraBindingPreparationResult.Ready(request, prepared);
        }
    }

    public readonly struct CameraBindingAdoptedResult
    {
        CameraBindingAdoptedResult(
            bool adopted,
            string actorId,
            string instanceId,
            string bindingId,
            string profileId,
            string profileRevision,
            string failureMessage)
        {
            Adopted = adopted;
            ActorId = actorId ?? string.Empty;
            InstanceId = instanceId ?? string.Empty;
            BindingId = bindingId ?? string.Empty;
            ProfileId = profileId ?? string.Empty;
            ProfileRevision = profileRevision ?? string.Empty;
            FailureMessage = failureMessage ?? string.Empty;
        }

        public bool Adopted { get; }
        public string ActorId { get; }
        public string InstanceId { get; }
        public string BindingId { get; }
        public string ProfileId { get; }
        public string ProfileRevision { get; }
        public string FailureMessage { get; }

        internal static CameraBindingAdoptedResult CreateAdopted(
            string actorId,
            string instanceId,
            string bindingId,
            string profileId,
            string profileRevision) =>
            new CameraBindingAdoptedResult(
                true,
                actorId,
                instanceId,
                bindingId,
                profileId,
                profileRevision,
                string.Empty);

        internal static CameraBindingAdoptedResult CreateFailed(
            string bindingId,
            string profileId,
            string profileRevision,
            string failureMessage) =>
            new CameraBindingAdoptedResult(
                false,
                string.Empty,
                string.Empty,
                bindingId,
                profileId,
                profileRevision,
                failureMessage);
    }
}
