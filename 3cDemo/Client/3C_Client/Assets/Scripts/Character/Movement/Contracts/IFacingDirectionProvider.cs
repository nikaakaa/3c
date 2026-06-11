using UnityEngine;

namespace ThirdPersonMovement
{
    public interface IFacingDirectionProvider
    {
        Vector3 FacingForward { get; }
    }
}
