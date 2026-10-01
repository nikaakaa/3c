using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Collision;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public readonly struct UnitySweepTargetBinding
    {
        public UnitySweepTargetBinding(Collider collider, SweepTargetIdentity identity)
        {
            Collider = collider;
            Identity = identity;
        }
        public Collider Collider { get; }
        public SweepTargetIdentity Identity { get; }
    }

    public sealed class UnitySweepQueryBackend : ISweepQueryBackend<Float32Scalar>
    {
        readonly PhysicsScene m_Scene;
        readonly SweepShape<Float32Scalar>[] m_Shapes;
        readonly Dictionary<int, SweepTargetIdentity> m_Targets;
        readonly Collider[] m_Overlaps;
        readonly RaycastHit[] m_CastHits;
        readonly int m_QueryCapacity;

        public UnitySweepQueryBackend(PhysicsScene scene, ReadOnlySpan<SweepShape<Float32Scalar>> shapes, ReadOnlySpan<UnitySweepTargetBinding> targets, int queryCapacity)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Sweep queries require a valid Physics Scene.", nameof(scene));
            m_Scene = scene;
            m_Shapes = shapes.ToArray();
            m_Targets = new Dictionary<int, SweepTargetIdentity>(targets.Length);
            for (int i = 0; i < targets.Length; i++)
            {
                ref readonly UnitySweepTargetBinding target = ref targets[i];
                if (target.Collider.gameObject.scene.GetPhysicsScene() != scene)
                    throw new ArgumentException("Sweep Collider must belong to the bound Physics Scene.", nameof(targets));
                m_Targets.Add(target.Collider.GetInstanceID(), target.Identity);
            }
            m_QueryCapacity = queryCapacity;
            m_Overlaps = new Collider[queryCapacity + 1];
            m_CastHits = new RaycastHit[queryCapacity + 1];
            Descriptor = new SweepBackendDescriptor(
                "unity-physics/" + Application.unityVersion + "/nonalloc-query-v1",
                Float32SimulationNumericProfile.Value,
                SweepShapeKind.Sphere | SweepShapeKind.Capsule | SweepShapeKind.Box,
                SweepMotionModel.PointPath | SweepMotionModel.ShapeTranslation,
                SweepTargetModel.ScenePose,
                false);
        }

        public PhysicsScene Scene => m_Scene;
        public SweepBackendDescriptor Descriptor { get; }

        public SweepQueryStatus SweepAll(in SweepQuery<Float32Scalar> query, SweepResultBuffer<Float32Scalar> results)
        {
            results.Begin(in query, Descriptor);
            if (query.TargetModel != SweepTargetModel.ScenePose)
                return results.Fail(SweepQueryStatus.UnsupportedMotion, -1, default);
            QueryTriggerInteraction triggers = query.Filter.IncludeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            for (int segmentIndex = 0; segmentIndex < query.Segments.Length; segmentIndex++)
            {
                ref readonly SweepSegment<Float32Scalar> segment = ref query.Segments[segmentIndex];
                ref readonly SweepShape<Float32Scalar> shape = ref m_Shapes[segment.Track.ShapeIndex];
                if (segment.Track.Motion == SweepMotionModel.PointPath)
                {
                    if (shape.Kind != SweepShapeKind.Sphere)
                        return results.Fail(SweepQueryStatus.UnsupportedShape, segmentIndex, default);
                }
                else if (segment.Track.Motion != SweepMotionModel.ShapeTranslation)
                {
                    return results.Fail(SweepQueryStatus.UnsupportedMotion, segmentIndex, default);
                }
                if (shape.Kind != SweepShapeKind.Sphere && !SameOrientation(segment.Start.Pose.Orientation, segment.End.Pose.Orientation))
                    return results.Fail(SweepQueryStatus.UnsupportedMotion, segmentIndex, default);

                Vector3 start = ToUnity(segment.Start.Pose.Position);
                Quaternion orientation = ToUnity(segment.Start.Pose.Orientation);
                Vector3 capsuleOffset = orientation * Vector3.up * shape.HalfLength.Value;
                int overlapCount;
                switch (shape.Kind)
                {
                    case SweepShapeKind.Sphere:
                        overlapCount = m_Scene.OverlapSphere(start, shape.Radius.Value, m_Overlaps, query.Filter.LayerMask, triggers);
                        break;
                    case SweepShapeKind.Capsule:
                        overlapCount = m_Scene.OverlapCapsule(start - capsuleOffset, start + capsuleOffset, shape.Radius.Value, m_Overlaps, query.Filter.LayerMask, triggers);
                        break;
                    case SweepShapeKind.Box:
                        overlapCount = m_Scene.OverlapBox(start, ToUnity(shape.HalfExtents), m_Overlaps, orientation, query.Filter.LayerMask, triggers);
                        break;
                    default:
                        return results.Fail(SweepQueryStatus.UnsupportedShape, segmentIndex, default);
                }
                if (overlapCount > m_QueryCapacity)
                    return results.Fail(SweepQueryStatus.CapacityExceeded, segmentIndex, default);
                for (int i = 0; i < overlapCount; i++)
                {
                    int colliderId = m_Overlaps[i].GetInstanceID();
                    if (!m_Targets.TryGetValue(colliderId, out SweepTargetIdentity target))
                        return results.Fail(SweepQueryStatus.UnmappedCollider, segmentIndex, default, colliderId);
                    if (target.TargetId == query.Filter.ExcludedTargetId)
                        continue;
                    var candidate = new SweepCandidate<Float32Scalar>(
                        target, segment.Track.Id, shape.Id, segmentIndex, segment.Track.Motion,
                        SweepContactKind.InitialOverlap, SweepContactFields.Time,
                        segment.Start.Time, Float32Scalar.Zero, query.TargetPoseTime, default, default);
                    if (!results.Add(in candidate))
                        return results.Fail(SweepQueryStatus.CapacityExceeded, segmentIndex, target);
                }
                Vector3 delta = ToUnity(segment.End.Pose.Position) - start;
                float distance = delta.magnitude;
                if (distance == 0f)
                    continue;
                Vector3 direction = delta / distance;
                int castCount;
                switch (shape.Kind)
                {
                    case SweepShapeKind.Sphere:
                        castCount = m_Scene.SphereCast(start, shape.Radius.Value, direction, m_CastHits, distance, query.Filter.LayerMask, triggers);
                        break;
                    case SweepShapeKind.Capsule:
                        castCount = m_Scene.CapsuleCast(start - capsuleOffset, start + capsuleOffset, shape.Radius.Value, direction, m_CastHits, distance, query.Filter.LayerMask, triggers);
                        break;
                    default:
                        castCount = m_Scene.BoxCast(start, ToUnity(shape.HalfExtents), direction, m_CastHits, orientation, distance, query.Filter.LayerMask, triggers);
                        break;
                }
                if (castCount > m_QueryCapacity)
                    return results.Fail(SweepQueryStatus.CapacityExceeded, segmentIndex, default);
                for (int i = 0; i < castCount; i++)
                {
                    ref readonly RaycastHit hit = ref m_CastHits[i];
                    int colliderId = hit.collider.GetInstanceID();
                    if (!m_Targets.TryGetValue(colliderId, out SweepTargetIdentity target))
                        return results.Fail(SweepQueryStatus.UnmappedCollider, segmentIndex, default, colliderId);
                    if (target.TargetId == query.Filter.ExcludedTargetId)
                        continue;
                    Float32Scalar fraction = Float32Scalar.FromSingle(hit.distance / distance);
                    Float32Scalar time = segment.Start.Time + (segment.End.Time - segment.Start.Time) * fraction;
                    SweepContactFields fields = SweepContactFields.Time;
                    Vector3 normal = hit.normal;
                    bool hasContact = normal.sqrMagnitude > 0f;
                    if (hasContact)
                        fields |= SweepContactFields.Point | SweepContactFields.Normal;
                    var candidate = new SweepCandidate<Float32Scalar>(
                        target, segment.Track.Id, shape.Id, segmentIndex, segment.Track.Motion,
                        SweepContactKind.Sweep, fields, time, fraction, query.TargetPoseTime,
                        hasContact ? FromUnity(hit.point) : default, hasContact ? FromUnity(normal) : default);
                    if (!results.Add(in candidate))
                        return results.Fail(SweepQueryStatus.CapacityExceeded, segmentIndex, target);
                }
            }
            return results.Complete();
        }

        static bool SameOrientation(SweepQuaternion<Float32Scalar> left, SweepQuaternion<Float32Scalar> right) =>
            left.X == right.X && left.Y == right.Y && left.Z == right.Z && left.W == right.W ||
            left.X == -right.X && left.Y == -right.Y && left.Z == -right.Z && left.W == -right.W;

        static Vector3 ToUnity(SweepVector3<Float32Scalar> value) => new Vector3(value.X.Value, value.Y.Value, value.Z.Value);
        static Quaternion ToUnity(SweepQuaternion<Float32Scalar> value) => new Quaternion(value.X.Value, value.Y.Value, value.Z.Value, value.W.Value);
        static SweepVector3<Float32Scalar> FromUnity(Vector3 value) => new SweepVector3<Float32Scalar>(Float32Scalar.FromSingle(value.x), Float32Scalar.FromSingle(value.y), Float32Scalar.FromSingle(value.z));
    }
}
