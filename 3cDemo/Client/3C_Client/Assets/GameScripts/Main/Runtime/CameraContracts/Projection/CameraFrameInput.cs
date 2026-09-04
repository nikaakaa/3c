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
            bool paused,
            bool resetHistory,
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
            Paused = paused;
            ResetHistory = resetHistory;
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
        public bool Paused { get; }
        public bool ResetHistory { get; }
        public IReadOnlyList<CameraTargetSnapshot> Targets { get; }

        public float Delta(CameraTimeDomain domain)
        {
            switch (domain)
            {
                case CameraTimeDomain.PresentationUnscaled:
                    return UnscaledDeltaSeconds;
                case CameraTimeDomain.OwnerScaled:
                    return ScaledDeltaSeconds * OwnerTimeScale;
                case CameraTimeDomain.LocalAvatarScaled:
                    return ScaledDeltaSeconds * LocalAvatarTimeScale;
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
