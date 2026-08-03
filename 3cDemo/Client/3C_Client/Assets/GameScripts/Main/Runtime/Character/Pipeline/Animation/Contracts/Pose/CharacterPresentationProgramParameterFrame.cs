using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPresentationProgramParameterFrame
    {
        readonly bool m_HasMotorValues;
        readonly float m_MotorPlanarSpeed;
        readonly float m_MotorLocalVelocityX;
        readonly float m_MotorLocalVelocityY;
        readonly PoseParameterId[] m_DirectIds;
        readonly float[] m_DirectValues;

        CharacterPresentationProgramParameterFrame(
            bool hasMotorValues,
            float motorPlanarSpeed,
            float motorLocalVelocityX,
            float motorLocalVelocityY)
        {
            m_HasMotorValues = hasMotorValues;
            m_MotorPlanarSpeed = motorPlanarSpeed;
            m_MotorLocalVelocityX = motorLocalVelocityX;
            m_MotorLocalVelocityY = motorLocalVelocityY;
            m_DirectIds = null;
            m_DirectValues = null;
        }

        CharacterPresentationProgramParameterFrame(
            PoseParameterId[] directIds,
            float[] directValues)
        {
            m_HasMotorValues = false;
            m_MotorPlanarSpeed = 0f;
            m_MotorLocalVelocityX = 0f;
            m_MotorLocalVelocityY = 0f;
            m_DirectIds = directIds;
            m_DirectValues = directValues;
        }

        internal bool IsValid =>
            m_HasMotorValues ||
            m_DirectIds != null &&
            m_DirectIds.Length > 0 &&
            m_DirectValues != null &&
            m_DirectIds.Length == m_DirectValues.Length;

        internal static CharacterPresentationProgramParameterFrame FromBody(
            in CharacterBodyPresentationFrame bodyFrame)
        {
            if (!bodyFrame.IsValid)
                return default;
            Vector3 localVelocity = Quaternion.Inverse(bodyFrame.VisibleRotation) * bodyFrame.VisibleVelocity;
            return new CharacterPresentationProgramParameterFrame(
                true,
                new Vector2(bodyFrame.VisibleVelocity.x, bodyFrame.VisibleVelocity.z).magnitude,
                localVelocity.x,
                localVelocity.z);
        }

        internal static CharacterPresentationProgramParameterFrame FromFact(
            in CharacterPresentationFactFrame factFrame)
        {
            if (!factFrame.IsValid)
                return default;
            return new CharacterPresentationProgramParameterFrame(
                true,
                factFrame.HorizontalSpeed,
                factFrame.MovementDirection.x * factFrame.HorizontalSpeed,
                factFrame.MovementDirection.y * factFrame.HorizontalSpeed);
        }

        internal static CharacterPresentationProgramParameterFrame FromDirect(
            PoseParameterId xParameterId,
            float x,
            PoseParameterId yParameterId,
            float y)
        {
            return FromDirect(
                yParameterId.IsValid
                    ? new[] { xParameterId, yParameterId }
                    : new[] { xParameterId },
                yParameterId.IsValid
                    ? new[] { x, y }
                    : new[] { x });
        }

        internal static CharacterPresentationProgramParameterFrame FromDirect(
            IReadOnlyList<PoseParameterId> parameterIds,
            IReadOnlyList<float> values)
        {
            if (parameterIds == null || values == null ||
                parameterIds.Count == 0 ||
                parameterIds.Count != values.Count)
            {
                throw new ArgumentException("Direct Presentation Program Parameter fixture is incomplete.");
            }
            var ids = new PoseParameterId[parameterIds.Count];
            var copiedValues = new float[values.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                PoseParameterId id = parameterIds[i];
                float value = values[i];
                if (!Supports(id) || !float.IsFinite(value))
                    throw new ArgumentException("Direct Presentation Program Parameter input is invalid.");
                for (int prior = 0; prior < i; prior++)
                {
                    if (ids[prior].Equals(id))
                        throw new ArgumentException($"Direct Presentation Program Parameter fixture contains duplicate '{id}'.");
                }
                ids[i] = id;
                copiedValues[i] = value;
            }
            return new CharacterPresentationProgramParameterFrame(ids, copiedValues);
        }

        internal float Require(PoseParameterId parameterId)
        {
            if (!parameterId.IsValid)
                throw new ArgumentException("Presentation Program Parameter identity is invalid.", nameof(parameterId));
            if (m_DirectIds != null)
            {
                for (int i = 0; i < m_DirectIds.Length; i++)
                {
                    if (parameterId.Equals(m_DirectIds[i]))
                        return m_DirectValues[i];
                }
            }
            if (m_HasMotorValues)
            {
                if (parameterId.Equals(AnimationPoseParameterIds.MotorPlanarSpeed))
                    return m_MotorPlanarSpeed;
                if (parameterId.Equals(AnimationPoseParameterIds.MotorLocalVelocityX))
                    return m_MotorLocalVelocityX;
                if (parameterId.Equals(AnimationPoseParameterIds.MotorLocalVelocityY))
                    return m_MotorLocalVelocityY;
            }
            throw new InvalidOperationException(
                $"Presentation Program Parameter '{parameterId}' is unavailable in the current frame.");
        }

        internal static bool Supports(PoseParameterId parameterId) =>
            parameterId.Equals(AnimationPoseParameterIds.MotorPlanarSpeed) ||
            parameterId.Equals(AnimationPoseParameterIds.MotorLocalVelocityX) ||
            parameterId.Equals(AnimationPoseParameterIds.MotorLocalVelocityY);
    }
}
