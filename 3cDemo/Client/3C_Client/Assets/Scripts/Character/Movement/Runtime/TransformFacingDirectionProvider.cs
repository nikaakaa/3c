using UnityEngine;

namespace ThirdPersonMovement
{
    [DisallowMultipleComponent]
    public sealed class TransformFacingDirectionProvider : MonoBehaviour, IFacingDirectionProvider
    {
        [SerializeField] Transform facingRoot;

        public Transform FacingRoot { get => facingRoot; set => facingRoot = value; }
        public Vector3 FacingForward => (facingRoot != null ? facingRoot : transform).forward;

        void Reset()
        {
            facingRoot = transform;
        }
    }
}
