using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraSpace : byte
    {
        World = 1,
        Core = 2,
        LocalAvatar = 3,
        Camera = 4
    }
}
