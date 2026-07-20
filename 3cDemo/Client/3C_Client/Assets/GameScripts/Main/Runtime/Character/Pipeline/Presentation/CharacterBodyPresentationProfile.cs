using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterVisualTrajectoryMode : byte
    {
        Direct = 1,
        BoundedCorrection = 2
    }

    [CreateAssetMenu(
        fileName = "CharacterBodyPresentationProfile",
        menuName = "3C/Character/Body Presentation Profile")]
    public sealed class CharacterBodyPresentationProfile : ScriptableObject
    {
        [SerializeField] CharacterVisualTrajectoryMode m_TrajectoryMode = CharacterVisualTrajectoryMode.Direct;
        [SerializeField, Min(0.001f)] float m_PositionHalfLifeSeconds = 0.04f;
        [SerializeField, Min(0.001f)] float m_MaximumHorizontalErrorMeters = 0.18f;
        [SerializeField, Min(0.0001f)] float m_PositionSettleDistanceMeters = 0.005f;
        [SerializeField, Min(0.001f)] float m_YawHalfLifeSeconds = 0.035f;
        [SerializeField, Min(0.01f)] float m_MaximumYawErrorDegrees = 12f;
        [SerializeField, Min(0.001f)] float m_YawSettleDegrees = 0.25f;

        internal CharacterBodyPresentationSettings BuildSettings()
        {
            var settings = new CharacterBodyPresentationSettings(
                m_TrajectoryMode,
                m_PositionHalfLifeSeconds,
                m_MaximumHorizontalErrorMeters,
                m_PositionSettleDistanceMeters,
                m_YawHalfLifeSeconds,
                m_MaximumYawErrorDegrees,
                m_YawSettleDegrees);
            settings.RequireValid(name);
            return settings;
        }
    }

    internal readonly struct CharacterBodyPresentationSettings
    {
        public CharacterBodyPresentationSettings(
            CharacterVisualTrajectoryMode trajectoryMode,
            float positionHalfLifeSeconds,
            float maximumPositionErrorMeters,
            float positionSettleDistanceMeters,
            float yawHalfLifeSeconds,
            float maximumYawErrorDegrees,
            float yawSettleDegrees)
        {
            TrajectoryMode = trajectoryMode;
            PositionHalfLifeSeconds = positionHalfLifeSeconds;
            MaximumPositionErrorMeters = maximumPositionErrorMeters;
            PositionSettleDistanceMeters = positionSettleDistanceMeters;
            YawHalfLifeSeconds = yawHalfLifeSeconds;
            MaximumYawErrorDegrees = maximumYawErrorDegrees;
            YawSettleDegrees = yawSettleDegrees;
        }

        public CharacterVisualTrajectoryMode TrajectoryMode { get; }
        public float PositionHalfLifeSeconds { get; }
        public float MaximumPositionErrorMeters { get; }
        public float PositionSettleDistanceMeters { get; }
        public float YawHalfLifeSeconds { get; }
        public float MaximumYawErrorDegrees { get; }
        public float YawSettleDegrees { get; }

        public void RequireValid(string owner)
        {
            if (TrajectoryMode != CharacterVisualTrajectoryMode.Direct &&
                TrajectoryMode != CharacterVisualTrajectoryMode.BoundedCorrection)
            {
                throw new InvalidOperationException(
                    $"Body Presentation Profile '{owner}' has an unknown trajectory mode.");
            }
            if (TrajectoryMode == CharacterVisualTrajectoryMode.Direct)
                return;
            if (!IsPositiveFinite(PositionHalfLifeSeconds) ||
                !IsPositiveFinite(MaximumPositionErrorMeters) ||
                !IsPositiveFinite(PositionSettleDistanceMeters) ||
                PositionSettleDistanceMeters > MaximumPositionErrorMeters ||
                !IsPositiveFinite(YawHalfLifeSeconds) ||
                !IsPositiveFinite(MaximumYawErrorDegrees) ||
                !IsPositiveFinite(YawSettleDegrees) ||
                YawSettleDegrees > MaximumYawErrorDegrees)
            {
                throw new InvalidOperationException(
                    $"Body Presentation Profile '{owner}' has invalid bounded correction settings.");
            }
        }

        static bool IsPositiveFinite(float value) => float.IsFinite(value) && value > 0f;
    }
}
