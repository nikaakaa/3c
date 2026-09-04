using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraOrbitComposition
    {
        readonly CameraOrbitGeometry m_FromGeometry;
        readonly CameraOrbitGeometry m_ToGeometry;
        readonly float m_FromRadiusScale;
        readonly float m_ToRadiusScale;
        readonly float m_BlendProgress;

        CameraOrbitComposition(
            float yaw,
            float pitch,
            CameraOrbitGeometry geometry,
            float radiusScale)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            Yaw = yaw;
            Pitch = pitch;
            m_FromGeometry = geometry;
            m_ToGeometry = null;
            m_FromRadiusScale = radiusScale;
            m_ToRadiusScale = 0f;
            m_BlendProgress = 0f;
        }

        CameraOrbitComposition(
            float yaw,
            float pitch,
            CameraOrbitGeometry fromGeometry,
            CameraOrbitGeometry toGeometry,
            float fromRadiusScale,
            float toRadiusScale,
            float blendProgress)
        {
            if (fromGeometry == null || toGeometry == null || fromGeometry.Count != toGeometry.Count)
                throw new InvalidOperationException("Camera orbit composition transition changed orbit group capacity.");
            Yaw = yaw;
            Pitch = pitch;
            m_FromGeometry = fromGeometry;
            m_ToGeometry = toGeometry;
            m_FromRadiusScale = fromRadiusScale;
            m_ToRadiusScale = toRadiusScale;
            m_BlendProgress = blendProgress;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public bool IsValid => m_FromGeometry != null;
        public int Count => m_FromGeometry != null ? m_FromGeometry.Count : 0;
        public float CenterRadius
        {
            get
            {
                if (m_FromGeometry == null)
                    return 0f;
                float fromRadius = m_FromGeometry.CenterRadius * m_FromRadiusScale;
                if (m_ToGeometry == null)
                    return fromRadius;
                float toRadius = m_ToGeometry.CenterRadius * m_ToRadiusScale;
                return Mathf.LerpUnclamped(fromRadius, toRadius, m_BlendProgress);
            }
        }

        public static CameraOrbitComposition Create(
            IReadOnlyList<CameraOrbitPayload> source,
            float centerRadius,
            bool sourceUsesAbsoluteRadius,
            float yaw,
            float pitch) =>
            Create(
                CameraOrbitGeometry.Create(source),
                centerRadius,
                sourceUsesAbsoluteRadius,
                yaw,
                pitch);

        internal static CameraOrbitComposition Create(
            CameraOrbitGeometry geometry,
            float centerRadius,
            bool sourceUsesAbsoluteRadius,
            float yaw,
            float pitch)
        {
            if (geometry == null)
                throw new InvalidOperationException("Camera orbit composition has no orbit geometry.");
            if (!float.IsFinite(centerRadius) || centerRadius <= 0f)
                throw new InvalidOperationException("Camera orbit composition has an invalid center radius.");
            float scale = sourceUsesAbsoluteRadius
                ? 1f
                : centerRadius / geometry.CenterRadius;
            return new CameraOrbitComposition(yaw, pitch, geometry, scale);
        }

        public CameraOrbitComposition WithPose(float yaw, float pitch, float centerRadius)
        {
            if (!IsValid || !float.IsFinite(centerRadius) || centerRadius <= 0f)
                throw new InvalidOperationException("Camera orbit composition has an invalid center radius.");
            if (m_ToGeometry == null)
                return new CameraOrbitComposition(
                    yaw,
                    pitch,
                    m_FromGeometry,
                    centerRadius / m_FromGeometry.CenterRadius);
            float currentCenterRadius = CenterRadius;
            if (!float.IsFinite(currentCenterRadius) || currentCenterRadius <= 0f)
                throw new InvalidOperationException("Camera orbit composition has an invalid blended center radius.");
            float scale = centerRadius / currentCenterRadius;
            return new CameraOrbitComposition(
                yaw,
                pitch,
                m_FromGeometry,
                m_ToGeometry,
                m_FromRadiusScale * scale,
                m_ToRadiusScale * scale,
                m_BlendProgress);
        }

        public CameraOrbitComposition Scale(float scale)
        {
            if (!IsValid)
                throw new InvalidOperationException("Camera orbit composition has no orbit geometry.");
            scale = Mathf.Max(0f, scale);
            if (m_ToGeometry == null)
                return new CameraOrbitComposition(
                    Yaw,
                    Pitch,
                    m_FromGeometry,
                    m_FromRadiusScale * scale);
            return new CameraOrbitComposition(
                Yaw,
                Pitch,
                m_FromGeometry,
                m_ToGeometry,
                m_FromRadiusScale * scale,
                m_ToRadiusScale * scale,
                m_BlendProgress);
        }

        public static CameraOrbitComposition Blend(
            CameraOrbitComposition from,
            CameraOrbitComposition to,
            float progress)
        {
            if (!from.IsValid || !to.IsValid || from.Count != to.Count)
                throw new InvalidOperationException("Camera orbit transition changed orbit group capacity.");
            float t = Mathf.Clamp01(progress);
            float yaw = Mathf.LerpAngle(from.Yaw, to.Yaw, t);
            float pitch = Mathf.LerpUnclamped(from.Pitch, to.Pitch, t);
            if (from.m_ToGeometry == null && to.m_ToGeometry == null)
            {
                if (ReferenceEquals(from.m_FromGeometry, to.m_FromGeometry))
                    return new CameraOrbitComposition(
                        yaw,
                        pitch,
                        from.m_FromGeometry,
                        Mathf.LerpUnclamped(from.m_FromRadiusScale, to.m_FromRadiusScale, t));
                return new CameraOrbitComposition(
                    yaw,
                    pitch,
                    from.m_FromGeometry,
                    to.m_FromGeometry,
                    from.m_FromRadiusScale,
                    to.m_FromRadiusScale,
                    t);
            }

            var orbits = new CameraOrbitPayload[from.Count];
            for (int i = 0; i < orbits.Length; i++)
            {
                from.GetOrbit(i, out float fromHeight, out float fromRadius, out float fromScreenY);
                to.GetOrbit(i, out float toHeight, out float toRadius, out float toScreenY);
                orbits[i] = new CameraOrbitPayload(
                    Mathf.LerpUnclamped(fromHeight, toHeight, t),
                    Mathf.LerpUnclamped(fromRadius, toRadius, t),
                    Mathf.LerpUnclamped(fromScreenY, toScreenY, t));
            }
            return new CameraOrbitComposition(
                yaw,
                pitch,
                CameraOrbitGeometry.Own(orbits),
                1f);
        }

        public void GetOrbit(
            int index,
            out float height,
            out float radius,
            out float screenY)
        {
            if (m_ToGeometry == null)
            {
                CameraOrbitPayload orbit = m_FromGeometry[index];
                height = orbit.Height;
                radius = orbit.Radius * m_FromRadiusScale;
                screenY = orbit.ScreenY;
                return;
            }

            CameraOrbitPayload from = m_FromGeometry[index];
            CameraOrbitPayload to = m_ToGeometry[index];
            height = Mathf.LerpUnclamped(from.Height, to.Height, m_BlendProgress);
            radius = Mathf.LerpUnclamped(
                from.Radius * m_FromRadiusScale,
                to.Radius * m_ToRadiusScale,
                m_BlendProgress);
            screenY = Mathf.LerpUnclamped(from.ScreenY, to.ScreenY, m_BlendProgress);
        }
    }
}
