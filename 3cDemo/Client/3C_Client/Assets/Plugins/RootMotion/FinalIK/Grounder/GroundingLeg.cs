using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	public partial class Grounding {

		/// <summary>
		/// The %Grounding %Leg.
		/// </summary>
		public class Leg {

			/// <summary>
			/// Returns true distance from foot to ground is less that maxStep
			/// </summary>
			public bool isGrounded { get; private set; }
			/// <summary>
			/// Gets the current IK position of the foot.
			/// </summary>
			public Vector3 IKPosition { get; private set; }
			/// <summary>
			/// Gets the current rotation offset of the foot.
			/// </summary>
			public Quaternion rotationOffset = Quaternion.identity;
			/// <summary>
			/// Returns true, if the leg is valid and initiated
			/// </summary>
			public bool initiated { get; private set; }
			/// <summary>
			/// The height of foot from ground.
			/// </summary>
			public float heightFromGround { get; private set; }
			/// <summary>
			/// Velocity of the foot
			/// </summary>
			public Vector3 velocity { get; private set; }
			/// <summary>
			/// Gets the foot Transform.
			/// </summary>
			public Transform transform { get; private set; }
			/// <summary>
			/// Gets the current IK offset.
			/// </summary>
			public float IKOffset { get; private set; }

			public bool invertFootCenter;

            public RaycastHit heelHit { get; private set; }
            public RaycastHit capsuleHit { get; private set; }
			public GroundingQueryHit heelQueryHit { get; private set; }
			public GroundingQueryHit toeQueryHit { get; private set; }
			public GroundingQueryHit sideQueryHit { get; private set; }
			public GroundingQueryHit capsuleQueryHit { get; private set; }
			public GroundingQueryHit currentQueryHit {
				get {
					switch (grounding.quality) {
						case Quality.Best:
							return capsuleQueryHit.HasHit ? capsuleQueryHit : heelQueryHit;
						case Quality.Simple:
							if (heelQueryHit.HasHit) return heelQueryHit;
							if (toeQueryHit.HasHit) return toeQueryHit;
							return sideQueryHit;
						default:
							return heelQueryHit;
					}
				}
			}

            /// <summary>
            /// Gets the RaycastHit last used by the Grounder to get ground height at foot position.
            /// </summary>
            public RaycastHit GetHitPoint {
                get
                {
                    if (grounding.quality == Quality.Best) return capsuleHit;
                    return heelHit;
                }
            }

            /// <summary>
            /// Overrides the animated position of the foot.
            /// </summary>
            public void SetFootPosition(Vector3 position)
            {
                doOverrideFootPosition = true;
                overrideFootPosition = position;
            }
            
			private Grounding grounding;
			private float deltaTime;
			private Vector3 lastPosition;
			private Quaternion toHitNormal, r;
			private Vector3 up = Vector3.up;
            private bool doOverrideFootPosition;
            private Vector3 overrideFootPosition;
            private Vector3 transformPosition;
			private int footIndex;
			private bool hasHistory;
			
			// Initiates the Leg
			public void Initiate(Grounding grounding, Transform transform) {
				initiated = false;
				this.grounding = grounding;
				this.transform = transform;
				footIndex = -1;
				up = Vector3.up;
				IKPosition = transform.position;
				rotationOffset = Quaternion.identity;
				
				initiated = true;
				OnEnable();
			}

			internal void Initiate(Grounding grounding, int footIndex) {
				initiated = false;
				this.grounding = grounding;
				this.footIndex = footIndex;
				transform = null;
				up = Vector3.up;
				IKPosition = Vector3.zero;
				rotationOffset = Quaternion.identity;
				hasHistory = false;
				initiated = true;
			}

			// Should be called each time the leg is (re)activated
			public void OnEnable() {
				if (!initiated) return;
				
				lastPosition = transform.position;
				hasHistory = true;
			}

			// Set everything to 0
			public void Reset() {
				lastPosition = transform ? transform.position : IKPosition;
				hasHistory = transform != null;
				IKOffset = 0f;
				IKPosition = lastPosition;
				rotationOffset = Quaternion.identity;
			}

			// Raycasting, processing the leg's position
			public void Process(in GroundingFrameInput frame, in GroundingFootInput foot, IGroundingWorldQueryBackend worldQueryBackend) {
				if (!initiated) return;
				if (grounding.maxStep <= 0) return;
				if (footIndex >= 0 && foot.FootIndex != footIndex) throw new System.ArgumentException("Grounding foot input does not match initiated foot slot.", nameof(foot));

				transformPosition = doOverrideFootPosition ? overrideFootPosition : foot.Ankle.Position;
                doOverrideFootPosition = false;

				deltaTime = frame.DeltaTime;

				up = grounding.up;
				heightFromGround = Mathf.Infinity;
				
				velocity = hasHistory ? (transformPosition - lastPosition) / deltaTime : Vector3.zero;
				lastPosition = transformPosition;
				hasHistory = true;

				Vector3 prediction = velocity * grounding.prediction;
				
				if (grounding.footRadius <= 0) grounding.quality = Grounding.Quality.Fastest;

                isGrounded = false;
				toeQueryHit = default;
				sideQueryHit = default;

                // Raycasting
                switch (grounding.quality)
                {

                    // The fastest, single raycast
                    case Grounding.Quality.Fastest:

						GroundingQueryHit predictedHit = GetRaycastHit(foot.Heel.Position + prediction, GroundingQueryPurpose.Heel, foot.FootIndex, in frame, worldQueryBackend);
						heelQueryHit = predictedHit;
						if (grounding.secondaryPlantQuery) toeQueryHit = GetRaycastHit(foot.Toe.Position + prediction, GroundingQueryPurpose.Toe, foot.FootIndex, in frame, worldQueryBackend);
						heelHit = predictedHit.PhysicsHit;
						SetFootToPoint(predictedHit.Normal, predictedHit.Point);
						if (predictedHit.HasHit) isGrounded = true;
                        break;

                    // Medium, 3 raycasts
                    case Grounding.Quality.Simple:

						heelQueryHit = GetRaycastHit(foot.Heel.Position, GroundingQueryPurpose.Heel, foot.FootIndex, in frame, worldQueryBackend);
						heelHit = heelQueryHit.PhysicsHit;
						toeQueryHit = GetRaycastHit(foot.Toe.Position + prediction, GroundingQueryPurpose.Toe, foot.FootIndex, in frame, worldQueryBackend);
						sideQueryHit = GetRaycastHit(foot.Heel.Position + grounding.rootRight * grounding.footRadius * 0.5f, GroundingQueryPurpose.Side, foot.FootIndex, in frame, worldQueryBackend);

						if (heelQueryHit.HasHit || toeQueryHit.HasHit || sideQueryHit.HasHit) isGrounded = true;

						Vector3 planeNormal = Vector3.Cross(toeQueryHit.Point - heelQueryHit.Point, sideQueryHit.Point - heelQueryHit.Point).normalized;
                        if (Vector3.Dot(planeNormal, up) < 0) planeNormal = -planeNormal;

						SetFootToPlane(planeNormal, heelQueryHit.Point, heelQueryHit.Point);
                        break;

                    // The slowest, raycast and a capsule cast
                    case Grounding.Quality.Best:
						heelQueryHit = GetRaycastHit(foot.Heel.Position, GroundingQueryPurpose.Heel, foot.FootIndex, in frame, worldQueryBackend);
						if (grounding.secondaryPlantQuery) toeQueryHit = GetRaycastHit(foot.Toe.Position + prediction, GroundingQueryPurpose.Toe, foot.FootIndex, in frame, worldQueryBackend);
						capsuleQueryHit = GetCapsuleHit(foot.FootCenter.Position, prediction, foot.FootIndex, in frame, worldQueryBackend);
						heelHit = heelQueryHit.PhysicsHit;
						capsuleHit = capsuleQueryHit.PhysicsHit;

						if (heelQueryHit.HasHit || capsuleQueryHit.HasHit) isGrounded = true;

						SetFootToPlane(capsuleQueryHit.Normal, capsuleQueryHit.Point, heelQueryHit.Point);
                        break;
                }

				float offsetTarget = stepHeightFromGround;
				if (!grounding.rootGrounded) offsetTarget = 0f;

				IKOffset = Interp.LerpValue(IKOffset, offsetTarget, grounding.footSpeed, grounding.footSpeed, deltaTime);
				IKOffset = Mathf.Lerp(IKOffset, offsetTarget, deltaTime * grounding.footSpeed);

				float legHeight = grounding.GetVerticalOffset(transformPosition, grounding.rootPosition);
				float currentMaxOffset = Mathf.Clamp(grounding.maxStep - legHeight, 0f, grounding.maxStep);

				IKOffset = Mathf.Clamp(IKOffset, -currentMaxOffset, IKOffset);

				RotateFoot();

				// Update IK values
				IKPosition = transformPosition - up * IKOffset;

				float rW = grounding.footRotationWeight;
				rotationOffset = rW >= 1? r: Quaternion.Slerp(Quaternion.identity, r, rW);
			}

			// Gets the height from ground clamped between min and max step height
			public float stepHeightFromGround {
				get {
					return Mathf.Clamp(heightFromGround, -grounding.maxStep, grounding.maxStep);
				}
			}

            // Get predicted Capsule hit from the middle of the foot
            private GroundingQueryHit GetCapsuleHit(Vector3 footCenter, Vector3 offsetFromHeel, int queryFootIndex, in GroundingFrameInput frame, IGroundingWorldQueryBackend worldQueryBackend)
            {
                RaycastHit hit = new RaycastHit();
				Vector3 origin = footCenter;

                if (grounding.overstepFallsDown)
                {
                    hit.point = origin - up * grounding.maxStep;
                }
                else
                {
					hit.point = origin - up * grounding.GetVerticalOffset(origin, grounding.rootPosition);
                }
                hit.normal = up;

                // Start point of the capsule
                Vector3 capsuleStart = origin + grounding.maxStep * up;
                // End point of the capsule depending on the foot's velocity.
                Vector3 capsuleEnd = capsuleStart + offsetFromHeel;

				GroundingQueryRequest request = new GroundingQueryRequest(
					GroundingQueryShape.Capsule,
					GroundingQueryPurpose.FootCenter,
					frame.PhysicsScene,
					frame.LayerMask,
					queryFootIndex,
					capsuleStart,
					capsuleEnd,
					-up,
					grounding.footRadius,
					grounding.maxStep * 2f);
				bool hasHit = worldQueryBackend.Query(in request, out GroundingQueryHit queryHit);
				if (hasHit)
                {
					hit = queryHit.PhysicsHit;
                    // Safeguarding from a CapsuleCast bug in Unity that might cause it to return NaN for hit.point when cast against large colliders.
                    if (float.IsNaN(hit.point.x))
                    {
                        hit.point = origin - up * grounding.maxStep * 2f;
                        hit.normal = up;
                    }
                }

                // Since Unity2017 Raycasts will return Vector3.zero when starting from inside a collider
                if (hit.point == Vector3.zero && hit.normal == Vector3.zero)
                {
                    if (grounding.overstepFallsDown)
                    {
                        hit.point = origin - up * grounding.maxStep;
                    }
                    else
                    {
						hit.point = origin - up * grounding.GetVerticalOffset(origin, grounding.rootPosition);
                    }
                }

				return new GroundingQueryHit(hasHit, hit, hasHit ? queryHit.SurfaceIdentity : 0);
            }

            // Get simple Raycast from the heel
            private GroundingQueryHit GetRaycastHit(Vector3 origin, GroundingQueryPurpose purpose, int queryFootIndex, in GroundingFrameInput frame, IGroundingWorldQueryBackend worldQueryBackend)
            {
                RaycastHit hit = new RaycastHit();

                if (grounding.overstepFallsDown)
                {
                    hit.point = origin - up * grounding.maxStep;
                }
                else
                {
					hit.point = origin - up * grounding.GetVerticalOffset(origin, grounding.rootPosition);
                }
                hit.normal = up;

				if (grounding.maxStep <= 0f) return new GroundingQueryHit(false, hit, 0);

				GroundingQueryRequest request = new GroundingQueryRequest(
					GroundingQueryShape.Ray,
					purpose,
					frame.PhysicsScene,
					frame.LayerMask,
					queryFootIndex,
					origin + grounding.maxStep * up,
					Vector3.zero,
					-up,
					0f,
					grounding.maxStep * 2f);
				bool hasHit = worldQueryBackend.Query(in request, out GroundingQueryHit queryHit);
				if (hasHit) hit = queryHit.PhysicsHit;

                // Since Unity2017 Raycasts will return Vector3.zero when starting from inside a collider
                if (hit.point == Vector3.zero && hit.normal == Vector3.zero)
                {
                    if (grounding.overstepFallsDown)
                    {
                        hit.point = origin - up * grounding.maxStep;
                    }
                    else
                    {
						hit.point = origin - up * grounding.GetVerticalOffset(origin, grounding.rootPosition);
                    }
                }

				return new GroundingQueryHit(hasHit, hit, hasHit ? queryHit.SurfaceIdentity : 0);
            }

            // Rotates ground normal with respect to maxFootRotationAngle
            private Vector3 RotateNormal(Vector3 normal) {
				if (grounding.quality == Grounding.Quality.Best) return normal;
				return Vector3.RotateTowards(up, normal, grounding.maxFootRotationAngle * Mathf.Deg2Rad, deltaTime);
			}
			
			// Set foot height from ground relative to a point
			private void SetFootToPoint(Vector3 normal, Vector3 point) {
				toHitNormal = Quaternion.FromToRotation(up, RotateNormal(normal));
				
				heightFromGround = GetHeightFromGround(point);
			}
			
			// Set foot height from ground relative to a plane
			private void SetFootToPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 heelHitPoint) {
				planeNormal = RotateNormal(planeNormal);
				toHitNormal = Quaternion.FromToRotation(up, planeNormal);
				
				Vector3 pointOnPlane = V3Tools.LineToPlane(transformPosition + up * grounding.maxStep, -up, planeNormal, planePoint);
				
				// Get the height offset of the point on the plane
				heightFromGround = GetHeightFromGround(pointOnPlane);
				
				// Making sure the heel doesn't penetrate the ground
				float heelHeight = GetHeightFromGround(heelHitPoint);
				heightFromGround = Mathf.Clamp(heightFromGround, -Mathf.Infinity, heelHeight);
			}

			// Calculate height offset of a point
			private float GetHeightFromGround(Vector3 hitPoint) {
				return grounding.GetVerticalOffset(transformPosition, hitPoint) - rootYOffset;
			}
			
			// Adding ground normal offset to the foot's rotation
			private void RotateFoot() {
				// Getting the full target rotation
				Quaternion rotationOffsetTarget = GetRotationOffsetTarget();
				
				// Slerping the rotation offset
				r = Quaternion.Slerp(r, rotationOffsetTarget, deltaTime * grounding.footRotationSpeed);
			}
			
			// Gets the target hit normal offset as a Quaternion
			private Quaternion GetRotationOffsetTarget() {
				if (grounding.maxFootRotationAngle <= 0f) return Quaternion.identity;
				if (grounding.maxFootRotationAngle >= 180f) return toHitNormal;
				return Quaternion.RotateTowards(Quaternion.identity, toHitNormal, grounding.maxFootRotationAngle);
			}
			
			// The foot's height from ground in the animation
			private float rootYOffset {
				get {
					return grounding.GetVerticalOffset(transformPosition, grounding.rootPosition - up * grounding.heightOffset);
				}
			}		
		}
	}
}
