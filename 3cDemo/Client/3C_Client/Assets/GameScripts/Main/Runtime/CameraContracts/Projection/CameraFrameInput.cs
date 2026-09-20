using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraFrameInput
    {
        public CameraFrameInput(
            Vector3 bodyPosition,
            Quaternion bodyRotation,
            Vector2 lookInput,
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            float ownerTimeScale,
            float localAvatarTimeScale,
            bool ownerTimeScaleAvailable,
            bool localAvatarTimeScaleAvailable,
            bool paused,
            bool resetHistory,
            CameraResetReason resetReason,
            IReadOnlyList<CameraTargetSnapshot> targets)
        {
            BodyPosition = bodyPosition;
            BodyRotation = bodyRotation;
            LookInput = lookInput;
            ScaledDeltaSeconds = RequireDelta(scaledDeltaSeconds, nameof(scaledDeltaSeconds));
            UnscaledDeltaSeconds = RequireDelta(unscaledDeltaSeconds, nameof(unscaledDeltaSeconds));
            PresentationDeltaSeconds = RequireDelta(presentationDeltaSeconds, nameof(presentationDeltaSeconds));
            OwnerTimeScale = RequireScale(ownerTimeScale, nameof(ownerTimeScale));
            LocalAvatarTimeScale = RequireScale(localAvatarTimeScale, nameof(localAvatarTimeScale));
            HasOwnerTimeScale = ownerTimeScaleAvailable;
            HasLocalAvatarTimeScale = localAvatarTimeScaleAvailable;
            Paused = paused;
            ResetHistory = resetHistory;
            if (resetReason != CameraResetReason.None &&
                resetReason != CameraResetReason.Initialization &&
                resetReason != CameraResetReason.BodyCommittedBranchReplacement &&
                resetReason != CameraResetReason.BodySelectedStreamReset &&
                resetReason != CameraResetReason.RuntimeReset)
                throw new ArgumentOutOfRangeException(nameof(resetReason));
            ResetReason = resetReason;
            Targets = targets ?? Array.Empty<CameraTargetSnapshot>();
        }

        public Vector3 BodyPosition { get; }
        public Quaternion BodyRotation { get; }
        public Vector2 LookInput { get; }
        public float ScaledDeltaSeconds { get; }
        public float UnscaledDeltaSeconds { get; }
        public float PresentationDeltaSeconds { get; }
        public float OwnerTimeScale { get; }
        public float LocalAvatarTimeScale { get; }
        public bool HasOwnerTimeScale { get; }
        public bool HasLocalAvatarTimeScale { get; }
        public bool Paused { get; }
        public bool ResetHistory { get; }
        public CameraResetReason ResetReason { get; }
        public IReadOnlyList<CameraTargetSnapshot> Targets { get; }

        public float Delta(CameraTimeDomain domain)
        {
            switch (domain)
            {
                case CameraTimeDomain.PresentationUnscaled:
                    return UnscaledDeltaSeconds;
                case CameraTimeDomain.OwnerScaled:
                    if (!HasOwnerTimeScale)
                        throw new InvalidOperationException("Camera OwnerScaled time requires an OwnerTimeScale input.");
                    return PresentationDeltaSeconds * OwnerTimeScale;
                case CameraTimeDomain.LocalAvatarScaled:
                    if (!HasLocalAvatarTimeScale)
                        throw new InvalidOperationException("Camera LocalAvatarScaled time requires a LocalAvatarTimeScale input.");
                    return PresentationDeltaSeconds * LocalAvatarTimeScale;
                default:
                    return PresentationDeltaSeconds;
            }
        }

        static float RequireDelta(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }

        static float RequireScale(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }
}
