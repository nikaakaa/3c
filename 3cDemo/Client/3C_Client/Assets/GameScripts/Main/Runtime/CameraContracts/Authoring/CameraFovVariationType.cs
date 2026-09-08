using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraFovVariationType : byte
    {
        Absolute = 1,
        Additive = 2,
        Multiplicative = 3
    }
}
