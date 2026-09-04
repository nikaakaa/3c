using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraOrbitComposition
    {
        readonly CameraOrbitPayload[] m_Orbits;

        CameraOrbitComposition(
            float yaw,
            float pitch,
            CameraOrbitPayload[] orbits)
        {
            if (orbits == null || orbits.Length == 0)
                throw new ArgumentException("Camera orbit composition requires orbit data.", nameof(orbits));
            Yaw = yaw;
            Pitch = pitch;
            m_Orbits = orbits;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public IReadOnlyList<CameraOrbitPayload> Orbits => m_Orbits;
        public float CenterRadius => m_Orbits[m_Orbits.Length / 2].Radius;

        public static CameraOrbitComposition Create(
            IReadOnlyList<CameraOrbitPayload> source,
            float centerRadius,
            bool sourceUsesAbsoluteRadius,
            float yaw,
            float pitch)
        {
            if (source == null || source.Count == 0)
                throw new InvalidOperationException("Camera orbit composition has no orbit data.");
            if (!float.IsFinite(centerRadius) || centerRadius <= 0f)
                throw new InvalidOperationException("Camera orbit composition has an invalid center radius.");
            CameraOrbitPayload sourceCenter = source[source.Count / 2];
            if (sourceCenter == null || sourceCenter.Radius <= 0f)
                throw new InvalidOperationException("Camera orbit composition has an invalid source center radius.");
            float scale = sourceUsesAbsoluteRadius
                ? 1f
                : centerRadius / sourceCenter.Radius;
            var orbits = new CameraOrbitPayload[source.Count];
            for (int i = 0; i < orbits.Length; i++)
            {
                CameraOrbitPayload orbit = source[i];
                if (orbit == null)
                    throw new InvalidOperationException($"Camera orbit composition orbit #{i} is missing.");
                orbits[i] = new CameraOrbitPayload(
                    orbit.Height,
                    orbit.Radius * scale,
                    orbit.ScreenY);
            }
            return new CameraOrbitComposition(yaw, pitch, orbits);
        }

        public CameraOrbitComposition WithPose(float yaw, float pitch, float centerRadius)
        {
            if (!float.IsFinite(centerRadius) || centerRadius <= 0f)
                throw new InvalidOperationException("Camera orbit composition has an invalid center radius.");
            return new CameraOrbitComposition(yaw, pitch, ScaleGroup(centerRadius / CenterRadius));
        }

        public CameraOrbitComposition Scale(float scale)
        {
            scale = Mathf.Max(0f, scale);
            return new CameraOrbitComposition(Yaw, Pitch, ScaleGroup(scale));
        }

        public static CameraOrbitComposition Blend(
            CameraOrbitComposition from,
            CameraOrbitComposition to,
            float progress)
        {
            if (from == null || to == null || from.m_Orbits.Length != to.m_Orbits.Length)
                throw new InvalidOperationException("Camera orbit transition changed orbit group capacity.");
            float t = Mathf.Clamp01(progress);
            var orbits = new CameraOrbitPayload[from.m_Orbits.Length];
            for (int i = 0; i < orbits.Length; i++)
            {
                CameraOrbitPayload fromOrbit = from.m_Orbits[i];
                CameraOrbitPayload toOrbit = to.m_Orbits[i];
                orbits[i] = new CameraOrbitPayload(
                    Mathf.LerpUnclamped(fromOrbit.Height, toOrbit.Height, t),
                    Mathf.LerpUnclamped(fromOrbit.Radius, toOrbit.Radius, t),
                    Mathf.LerpUnclamped(fromOrbit.ScreenY, toOrbit.ScreenY, t));
            }
            return new CameraOrbitComposition(
                Mathf.LerpAngle(from.Yaw, to.Yaw, t),
                Mathf.LerpUnclamped(from.Pitch, to.Pitch, t),
                orbits);
        }

        CameraOrbitPayload[] ScaleGroup(float scale)
        {
            var orbits = new CameraOrbitPayload[m_Orbits.Length];
            for (int i = 0; i < orbits.Length; i++)
            {
                CameraOrbitPayload orbit = m_Orbits[i];
                orbits[i] = new CameraOrbitPayload(
                    orbit.Height,
                    orbit.Radius * scale,
                    orbit.ScreenY);
            }
            return orbits;
        }
    }
}
