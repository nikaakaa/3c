using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraTimeDomain : byte
    {
        PresentationScaled = 1,
        PresentationUnscaled = 2,
        OwnerScaled = 3,
        LocalAvatarScaled = 4
    }
}
