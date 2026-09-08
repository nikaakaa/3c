using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraOverrideTrackSettings
    {
        [SerializeField] CameraOrbitDescriptor m_TopOrbit = new CameraOrbitDescriptor(2f, 0.2f);
        [SerializeField] CameraOrbitDescriptor[] m_Orbits = Array.Empty<CameraOrbitDescriptor>();
        [SerializeField] float[] m_ScreenY = Array.Empty<float>();
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_AimOffset;
        [SerializeField] float m_FieldOfView = 60f;

        public CameraOrbitDescriptor TopOrbit => m_TopOrbit;
        public IReadOnlyList<CameraOrbitDescriptor> Orbits => m_Orbits ?? Array.Empty<CameraOrbitDescriptor>();
        public IReadOnlyList<float> ScreenY => m_ScreenY ?? Array.Empty<float>();
        public Vector3 FollowOffset => m_FollowOffset;
        public Vector3 AimOffset => m_AimOffset;
        public float FieldOfView => m_FieldOfView;

        public CameraOverrideTrackSettings() { }

        public CameraOverrideTrackSettings(
            CameraOrbitDescriptor topOrbit,
            CameraOrbitDescriptor[] orbits,
            float[] screenY,
            Vector3 followOffset,
            Vector3 aimOffset,
            float fieldOfView)
        {
            m_TopOrbit = topOrbit;
            m_Orbits = orbits ?? Array.Empty<CameraOrbitDescriptor>();
            m_ScreenY = screenY ?? Array.Empty<float>();
            m_FollowOffset = followOffset;
            m_AimOffset = aimOffset;
            m_FieldOfView = fieldOfView;
        }

        public void RequireValid(string source)
        {
            if (TopOrbit == null || !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                !float.IsFinite(FollowOffset.x) || !float.IsFinite(FollowOffset.y) ||
                !float.IsFinite(FollowOffset.z) || !float.IsFinite(AimOffset.x) ||
                !float.IsFinite(AimOffset.y) || !float.IsFinite(AimOffset.z))
                throw new InvalidOperationException($"{source} contains invalid Override track settings.");
            TopOrbit.RequireValid($"{source}.TopOrbit");
            for (int i = 0; i < Orbits.Count; i++)
            {
                CameraOrbitDescriptor orbit = Orbits[i];
                if (orbit == null)
                    throw new InvalidOperationException($"{source}.Orbits[{i}] is missing.");
                orbit.RequireValid($"{source}.Orbits[{i}]");
            }
            for (int i = 0; i < ScreenY.Count; i++)
            {
                if (!float.IsFinite(ScreenY[i]) || ScreenY[i] < 0f || ScreenY[i] > 1f)
                    throw new InvalidOperationException($"{source}.ScreenY[{i}] is invalid.");
            }
            if (ScreenY.Count != 0 && ScreenY.Count != Orbits.Count)
                throw new InvalidOperationException($"{source}.ScreenY must match Orbits.");
        }
    }
}
