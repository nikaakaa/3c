using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraTargetSlot
    {
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] CameraSpace m_Space = CameraSpace.World;
        [SerializeField] string m_AnchorKey = string.Empty;
        [SerializeField] string m_AimPointKey = string.Empty;
        [SerializeField] string m_PreferredBoneKey = string.Empty;
        [SerializeField] bool m_Required = true;

        public string SlotId => m_SlotId ?? string.Empty;
        public CameraSpace Space => m_Space;
        public string AnchorKey => m_AnchorKey ?? string.Empty;
        public string AimPointKey => m_AimPointKey ?? string.Empty;
        public string PreferredBoneKey => m_PreferredBoneKey ?? string.Empty;
        public bool Required => m_Required;

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(SlotId) || !Enum.IsDefined(typeof(CameraSpace), Space) ||
                string.IsNullOrWhiteSpace(AnchorKey) && string.IsNullOrWhiteSpace(AimPointKey) &&
                string.IsNullOrWhiteSpace(PreferredBoneKey))
                throw new InvalidOperationException($"{source} contains an invalid Camera target slot.");
        }
    }
}
