using System;
using UnityEngine;

namespace RootMotion.FinalIK {

	public enum GroundingQueryShape {
		Ray,
		Sphere,
		Capsule
	}

	public enum GroundingQueryPurpose {
		Root = 1,
		Heel = 2,
		Toe = 3,
		Side = 4,
		FootCenter = 5,
		FutureLanding = 6,
		GroundEnvelope = 7,
		SwingClearance = 8
	}

	public readonly struct GroundingComponentTransform {
		public GroundingComponentTransform(Vector3 position, Quaternion rotation) {
			Position = position;
			Rotation = rotation;
		}

		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
		public Vector3 Up => Rotation * Vector3.up;
		public Vector3 Right => Rotation * Vector3.right;
		public Vector3 Forward => Rotation * Vector3.forward;
	}

	public readonly struct GroundingFootInput {
		public GroundingFootInput(
			int footIndex,
			GroundingComponentTransform ankle,
			GroundingComponentTransform heel,
			GroundingComponentTransform toe,
			GroundingComponentTransform footCenter,
			float plantWeight = 1f) {
			if (!float.IsFinite(plantWeight) || plantWeight < 0f || plantWeight > 1f) throw new ArgumentOutOfRangeException(nameof(plantWeight));
			FootIndex = footIndex;
			Ankle = ankle;
			Heel = heel;
			Toe = toe;
			FootCenter = footCenter;
			PlantWeight = plantWeight;
		}

		public int FootIndex { get; }
		public GroundingComponentTransform Ankle { get; }
		public GroundingComponentTransform Heel { get; }
		public GroundingComponentTransform Toe { get; }
		public GroundingComponentTransform FootCenter { get; }
		public float PlantWeight { get; }
	}

	public readonly struct GroundingFrameInput {
		public GroundingFrameInput(
			float time,
			float deltaTime,
			PhysicsScene physicsScene,
			int layerMask,
			GroundingComponentTransform root,
			GroundingComponentTransform pelvis,
			GroundingFootInput leftFoot,
			GroundingFootInput rightFoot,
			int footCount) {
			if (footCount < 1 || footCount > 2) throw new ArgumentOutOfRangeException(nameof(footCount));
			Time = time;
			DeltaTime = deltaTime;
			PhysicsScene = physicsScene;
			LayerMask = layerMask;
			Root = root;
			Pelvis = pelvis;
			LeftFoot = leftFoot;
			RightFoot = rightFoot;
			FootCount = footCount;
		}

		public float Time { get; }
		public float DeltaTime { get; }
		public PhysicsScene PhysicsScene { get; }
		public int LayerMask { get; }
		public GroundingComponentTransform Root { get; }
		public GroundingComponentTransform Pelvis { get; }
		public GroundingFootInput LeftFoot { get; }
		public GroundingFootInput RightFoot { get; }
		public int FootCount { get; }

		public GroundingFootInput GetFoot(int index) {
			if (index == 0) return LeftFoot;
			if (index == 1 && FootCount == 2) return RightFoot;
			throw new ArgumentOutOfRangeException(nameof(index));
		}
	}

	public readonly struct GroundingQueryRequest {
		public GroundingQueryRequest(
			GroundingQueryShape shape,
			GroundingQueryPurpose purpose,
			PhysicsScene physicsScene,
			int layerMask,
			int footIndex,
			Vector3 origin,
			Vector3 capsuleEnd,
			Vector3 direction,
			float radius,
			float maxDistance) {
			Shape = shape;
			Purpose = purpose;
			PhysicsScene = physicsScene;
			LayerMask = layerMask;
			FootIndex = footIndex;
			Origin = origin;
			CapsuleEnd = capsuleEnd;
			Direction = direction;
			Radius = radius;
			MaxDistance = maxDistance;
		}

		public GroundingQueryShape Shape { get; }
		public GroundingQueryPurpose Purpose { get; }
		public PhysicsScene PhysicsScene { get; }
		public int LayerMask { get; }
		public int FootIndex { get; }
		public Vector3 Origin { get; }
		public Vector3 CapsuleEnd { get; }
		public Vector3 Direction { get; }
		public float Radius { get; }
		public float MaxDistance { get; }
	}

	public readonly struct GroundingQueryHit {
		public GroundingQueryHit(bool hasHit, RaycastHit physicsHit, int surfaceIdentity) {
			HasHit = hasHit;
			PhysicsHit = physicsHit;
			SurfaceIdentity = surfaceIdentity;
		}

		public bool HasHit { get; }
		public RaycastHit PhysicsHit { get; }
		public int SurfaceIdentity { get; }
		public Vector3 Point => PhysicsHit.point;
		public Vector3 Normal => PhysicsHit.normal;
		public float Distance => PhysicsHit.distance;
	}

	public interface IGroundingWorldQueryBackend {
		bool Query(in GroundingQueryRequest request, out GroundingQueryHit hit);
	}
}
