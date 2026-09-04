using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraEffectStackingType : byte
    {
        Replace = 1,
        Add = 2,
        HighestPriority = 3
    }
}
