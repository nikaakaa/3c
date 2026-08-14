using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct FootPlacementSurface
    {
        internal FootPlacementSurface(Collider collider, Vector3 point, Vector3 normal)
        {
            Collider = collider;
            Transform = collider ? collider.transform : null;
            Point = point;
            Normal = normal;
            LocalPoint = Transform ? Transform.InverseTransformPoint(point) : Vector3.zero;
            LocalNormal = Transform ? Transform.InverseTransformDirection(normal).normalized : Vector3.up;
            Identity = collider ? collider.GetInstanceID() : 0;
        }

        internal Collider Collider { get; }
        internal Transform Transform { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
        internal Vector3 LocalPoint { get; }
        internal Vector3 LocalNormal { get; }
        internal int Identity { get; }
        internal bool IsValid => Collider && Transform && Identity != 0;

        internal FootPlacementSurface Rebuild() =>
            IsValid
                ? new FootPlacementSurface(
                    Collider,
                    Transform.TransformPoint(LocalPoint),
                    Transform.TransformDirection(LocalNormal).normalized)
                : default;
    }
}
