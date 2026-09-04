using System;
using System.Collections.Generic;

namespace ThirdPersonCamera
{
    internal sealed class CameraOrbitGeometry
    {
        readonly CameraOrbitPayload[] m_Orbits;

        CameraOrbitGeometry(CameraOrbitPayload[] orbits)
        {
            if (orbits == null || orbits.Length == 0)
                throw new ArgumentException("Camera orbit geometry requires orbit data.", nameof(orbits));
            m_Orbits = orbits;
        }

        public int Count => m_Orbits.Length;
        public float CenterRadius => m_Orbits[m_Orbits.Length / 2].Radius;
        public CameraOrbitPayload this[int index] => m_Orbits[index];

        public static CameraOrbitGeometry Create(IReadOnlyList<CameraOrbitPayload> source)
        {
            if (source == null || source.Count == 0)
                throw new InvalidOperationException("Camera orbit geometry has no orbit data.");
            var orbits = new CameraOrbitPayload[source.Count];
            for (int i = 0; i < orbits.Length; i++)
            {
                CameraOrbitPayload orbit = source[i];
                if (orbit == null)
                    throw new InvalidOperationException($"Camera orbit geometry orbit #{i} is missing.");
                orbit.RequireValid($"Camera orbit geometry orbit #{i}");
                orbits[i] = new CameraOrbitPayload(
                    orbit.Height,
                    orbit.Radius,
                    orbit.ScreenY);
            }
            return new CameraOrbitGeometry(orbits);
        }

        public static CameraOrbitGeometry Own(CameraOrbitPayload[] orbits) =>
            new CameraOrbitGeometry(orbits);
    }
}
