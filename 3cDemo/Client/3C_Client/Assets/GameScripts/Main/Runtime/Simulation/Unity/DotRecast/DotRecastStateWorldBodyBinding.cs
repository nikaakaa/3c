using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecast;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [DisallowMultipleComponent]
    public sealed class DotRecastStateWorldBodyBinding : Float32WorldBodyBinding
    {
        [SerializeField] Vector3 m_InitialPosition;
        [SerializeField] float m_InitialYawDegrees;
        [SerializeField] bool m_InitialGrounded = true;
        [SerializeField] float m_ContactRadius;
        [SerializeField] float m_ContactHeight;
        [SerializeField] float m_ContactSkinWidth;

        public ActorContactShape ContactShape => BuildContactShape();

        protected override void RequireImplementationValid()
        {
            if (!IsFinite(m_InitialPosition.x) || !IsFinite(m_InitialPosition.y) ||
                !IsFinite(m_InitialPosition.z) || !IsFinite(m_InitialYawDegrees) ||
                !IsFinite(m_ContactRadius) || !IsFinite(m_ContactHeight) || !IsFinite(m_ContactSkinWidth))
            {
                throw new InvalidOperationException($"DotRecast World body binding '{BindingId}' contains a non-finite initial body.");
            }
            BuildContactShape();
        }

        protected override WorldBodyState BuildInitialBody(ActorId actorId)
        {
            return new WorldBodyState(
                actorId,
                new Float32Vector3(
                    Float32ScalarBoundary.ConvertExternal(m_InitialPosition.x, $"{BindingId}/initial-position-x"),
                    Float32ScalarBoundary.ConvertExternal(m_InitialPosition.y, $"{BindingId}/initial-position-y"),
                    Float32ScalarBoundary.ConvertExternal(m_InitialPosition.z, $"{BindingId}/initial-position-z")),
                new Float32Yaw(Float32ScalarBoundary.ConvertExternal(
                    Mathf.DeltaAngle(0f, m_InitialYawDegrees),
                    $"{BindingId}/initial-yaw")),
                Float32Vector3.Zero,
                Float32Scalar.Zero,
                m_InitialGrounded,
                WorldCollisionSummary.None);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        ActorContactShape BuildContactShape()
        {
            return new ActorContactShape(
                Float32ScalarBoundary.ConvertExternal(m_ContactRadius, $"{BindingId}/contact-radius"),
                Float32ScalarBoundary.ConvertExternal(m_ContactHeight, $"{BindingId}/contact-height"),
                Float32ScalarBoundary.ConvertExternal(m_ContactSkinWidth, $"{BindingId}/contact-skin-width"));
        }
    }
}
