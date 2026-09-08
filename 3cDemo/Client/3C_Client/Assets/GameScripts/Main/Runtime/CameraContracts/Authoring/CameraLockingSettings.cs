using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraLockingSettings
    {
        [SerializeField] bool m_EnableTargetLock;
        [SerializeField] bool m_EnableBossLock;
        [SerializeField] float m_TransitionSeconds = 0.2f;
        [SerializeField] float m_Weight = 1f;

        public bool EnableTargetLock => m_EnableTargetLock;
        public bool EnableBossLock => m_EnableBossLock;
        public float TransitionSeconds => m_TransitionSeconds;
        public float Weight => m_Weight;

        public CameraLockingSettings() { }

        internal CameraLockingSettings(CameraLockingSettings source)
        {
            m_EnableTargetLock = source.EnableTargetLock;
            m_EnableBossLock = source.EnableBossLock;
            m_TransitionSeconds = source.TransitionSeconds;
            m_Weight = source.Weight;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(TransitionSeconds) || TransitionSeconds < 0f ||
                !float.IsFinite(Weight) || Weight < 0f || Weight > 1f)
                throw new InvalidOperationException($"{source} contains invalid Camera locking settings.");
        }
    }
}
