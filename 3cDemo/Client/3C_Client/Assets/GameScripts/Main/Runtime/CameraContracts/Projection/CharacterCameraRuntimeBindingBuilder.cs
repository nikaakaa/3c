using System;
using System.Collections.Generic;

namespace ThirdPersonCamera
{
    public static class CharacterCameraRuntimeBindingBuilder
    {
        public static CameraBindingPreparationResult Prepare(
            string requestId,
            string requestedSourceId,
            string requestedSourceRevision,
            CharacterCameraProfile profile,
            ICameraRigAdapter rigAdapter,
            IReadOnlyList<CameraTargetBinding> targetBindings,
            ICameraEnvironmentQuery environmentQuery)
        {
            var request = new CameraBindingPreparationRequest(
                requestId,
                requestedSourceId,
                requestedSourceRevision,
                null,
                rigAdapter,
                targetBindings,
                environmentQuery);
            if (!profile)
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Missing,
                    CameraBindingFailureCode.ProjectionMissing,
                    "Camera Profile is missing.");
            }
            CharacterCameraProjectionPayload projection;
            try
            {
                projection = CharacterCameraProjectionBuilder.Build(profile);
            }
            catch (Exception exception)
            {
                return CameraBindingPreparationResult.Failure(
                    request,
                    CameraBindingPreparationStatus.Invalid,
                    CameraBindingFailureCode.ProjectionInvalid,
                    exception.Message);
            }
            request = new CameraBindingPreparationRequest(
                requestId,
                requestedSourceId,
                requestedSourceRevision,
                projection,
                rigAdapter,
                targetBindings,
                environmentQuery);
            return CameraRuntimeBindingPreparation.Prepare(in request);
        }
    }
}
