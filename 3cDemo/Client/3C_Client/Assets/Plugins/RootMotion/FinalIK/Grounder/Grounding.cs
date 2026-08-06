using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	/// Foot placement system.
	/// </summary>
	[System.Serializable]
	public partial class Grounding {

        #region Main Interface

        /// <summary>
        /// The raycasting quality. Fastest is a single raycast per foot, Simple is three raycasts, Best is one raycast and a capsule cast per foot.
        /// </summary>
        [System.Serializable]
		public enum Quality {
			Fastest,
			Simple,
			Best
		}

		/// <summary>
		/// Layers to ground the character to. Make sure to exclude the layer of the character controller.
		/// </summary>
		[Tooltip("Layers to ground the character to. Make sure to exclude the layer of the character controller.")]
		public LayerMask layers;
		/// <summary>
		/// Max step height. Maximum vertical distance of Grounding from the root of the character.
		/// </summary>
		[Tooltip("Max step height. Maximum vertical distance of Grounding from the root of the character.")]
		public float maxStep = 0.5f;
		/// <summary>
		/// The height offset of the root.
		/// </summary>
		[Tooltip("The height offset of the root.")]
		public float heightOffset;
		/// <summary>
		/// The speed of moving the feet up/down.
		/// </summary>
		[Tooltip("The speed of moving the feet up/down.")]
		public float footSpeed = 2.5f;
		/// <summary>
		/// CapsuleCast radius. Should match approximately with the size of the feet.
		/// </summary>
		[Tooltip("CapsuleCast radius. Should match approximately with the size of the feet.")]
		public float footRadius = 0.15f;
		/// <summary>
		/// Offset of the foot center along character forward axis.
		/// </summary>
		[Tooltip("Offset of the foot center along character forward axis.")]
		[HideInInspector] public float footCenterOffset; // TODO make visible in inspector if Grounder Visualization is finished.
		/// <summary>
		/// Amount of velocity based prediction of the foot positions.
		/// </summary>
		[Tooltip("Amount of velocity based prediction of the foot positions.")]
		public float prediction = 0.05f;
		/// <summary>
		/// Weight of rotating the feet to the ground normal offset.
		/// </summary>
		[Tooltip("Weight of rotating the feet to the ground normal offset.")]
		[Range(0f, 1f)]
		public float footRotationWeight = 1f;
		/// <summary>
		/// Speed of slerping the feet to their grounded rotations.
		/// </summary>
		[Tooltip("Speed of slerping the feet to their grounded rotations.")]
		public float footRotationSpeed = 7f;
		/// <summary>
		/// Max Foot Rotation Angle, Max angular offset from the foot's rotation (Reasonable range: 0-90 degrees).
		/// </summary>
		[Tooltip("Max Foot Rotation Angle. Max angular offset from the foot's rotation.")]
		[Range(0f, 90f)]
		public float maxFootRotationAngle = 45f;
		/// <summary>
		/// If true, solver will rotate with the character root so the character can be grounded for example to spherical planets. 
		/// For performance reasons leave this off unless needed.
		/// </summary>
		[Tooltip("If true, solver will rotate with the character root so the character can be grounded for example to spherical planets. For performance reasons leave this off unless needed.")]
		public bool rotateSolver;
		/// <summary>
		/// The speed of moving the character up/down.
		/// </summary>
		[Tooltip("The speed of moving the character up/down.")]
		public float pelvisSpeed = 5f;
		/// <summary>
		/// Used for smoothing out vertical pelvis movement (range 0 - 1).
		/// </summary>
		[Tooltip("Used for smoothing out vertical pelvis movement (range 0 - 1).")]
		[Range(0f, 1f)]
		public float pelvisDamper;
		/// <summary>
		/// The weight of lowering the pelvis to the lowest foot.
		/// </summary>
		[Tooltip("The weight of lowering the pelvis to the lowest foot.")]
		public float lowerPelvisWeight = 1f;
		/// <summary>
		/// The weight of lifting the pelvis to the highest foot. This is useful when you don't want the feet to go too high relative to the body when crouching.
		/// </summary>
		[Tooltip("The weight of lifting the pelvis to the highest foot. This is useful when you don't want the feet to go too high relative to the body when crouching.")]
		public float liftPelvisWeight;
		/// <summary>
		/// The radius of the spherecast from the root that determines whether the character root is grounded.
		/// </summary>
		[Tooltip("The radius of the spherecast from the root that determines whether the character root is grounded.")]
		public float rootSphereCastRadius = 0.1f;
        /// <summary>
        /// If false, keeps the foot that is over a ledge at the root level. If true, lowers the overstepping foot and body by the 'Max Step' value.
        /// </summary>
        [Tooltip("If false, keeps the foot that is over a ledge at the root level. If true, lowers the overstepping foot and body by the 'Max Step' value.")]
		public bool overstepFallsDown = true;
		public bool secondaryPlantQuery;
		/// <summary>
		/// The raycasting quality. Fastest is a single raycast per foot, Simple is three raycasts, Best is one raycast and a capsule cast per foot.
		/// </summary>
		[Tooltip("The raycasting quality. Fastest is a single raycast per foot, Simple is three raycasts, Best is one raycast and a capsule cast per foot.")]
		public Quality quality = Quality.Best;

		/// <summary>
		/// The %Grounding legs.
		/// </summary>
		public Leg[] legs { get; private set; }
		/// <summary>
		/// The %Grounding pelvis.
		/// </summary>
		public Pelvis pelvis { get; private set; }
		/// <summary>
		/// Gets a value indicating whether any of the legs are grounded
		/// </summary>
		public bool isGrounded { get; private set; }
		/// <summary>
		/// The root Transform
		/// </summary>
		public Transform root { get; private set; }
		/// <summary>
		/// Ground height at the root position.
		/// </summary>
		public RaycastHit rootHit { get; private set; }
		public GroundingQueryHit rootQueryHit { get; private set; }
		/// <summary>
		/// Is the RaycastHit from the root grounded?
		/// </summary>
		public bool rootGrounded {
			get {
				return rootQueryHit.HasHit && rootQueryHit.Distance < maxStep * 2f;
			}
		}

        // For overriding ray/capsule/sphere casting functions
        public delegate bool OnRaycastDelegate(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public OnRaycastDelegate Raycast = Physics.Raycast;

        public delegate bool OnCapsuleCastDelegate(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public OnCapsuleCastDelegate CapsuleCast = Physics.CapsuleCast;

        public delegate bool OnSphereCastDelegate(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public OnSphereCastDelegate SphereCast = Physics.SphereCast;

        /// <summary>
        /// Raycasts or sphereCasts to find the root ground point. Distance of the Ray/Sphere cast is maxDistanceMlp x maxStep. Use this instead of rootHit if the Grounder is weighed out/disabled and not updated.
        /// </summary>
        public RaycastHit GetRootHit(float maxDistanceMlp = 10f) {
			GroundingFrameInput frame = BuildVendorFrame();
			return GetRootHit(in frame, vendorBackend, maxDistanceMlp).PhysicsHit;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Grounding"/> is valid.
		/// </summary>
		public bool IsValid(ref string errorMessage) {
			if (root == null && !explicitInputMode) {
				errorMessage = "Root transform is null. Can't initiate Grounding.";
				return false;
			}
			if (legs == null) {
				errorMessage = "Grounding legs is null. Can't initiate Grounding.";
				return false;
			}
			if (pelvis == null) {
				errorMessage = "Grounding pelvis is null. Can't initiate Grounding.";
				return false;
			}
			
			if (legs.Length == 0) {
				errorMessage = "Grounding has 0 legs. Can't initiate Grounding.";
				return false;
			}
			return true;
		}
		
		/// <summary>
		/// Initiate the %Grounding as an integrated solver by providing the root Transform, leg solvers, pelvis Transform and spine solver.
		/// </summary>
		public void Initiate(Transform root, Transform[] feet) {
			this.root = root;
			explicitInputMode = false;
			initiated = false;

			rootHit = new RaycastHit();
			rootQueryHit = new GroundingQueryHit();
			if (vendorBackend == null) vendorBackend = new VendorGroundingWorldQueryBackend(this);

			// Constructing Legs
			if (legs == null) legs = new Leg[feet.Length];
			if (legs.Length != feet.Length) legs = new Leg[feet.Length];
			for (int i = 0; i < feet.Length; i++) if (legs[i] == null) legs[i] = new Leg();
			
			// Constructing pelvis
			if (pelvis == null) pelvis = new Pelvis();
			
			string errorMessage = string.Empty;
			if (!IsValid(ref errorMessage)) {
				Warning.Log(errorMessage, root, false);
				return;
			}
			
			// Initiate solvers only if application is playing
			if (Application.isPlaying) {
				for (int i = 0; i < feet.Length; i++) legs[i].Initiate(this, feet[i]);
				pelvis.Initiate(this);
				
				initiated = true;
			}
		}

		public void Initiate(int footCount) {
			if (footCount < 1 || footCount > 2) throw new System.ArgumentOutOfRangeException(nameof(footCount));
			root = null;
			explicitInputMode = true;
			initiated = false;
			rootHit = new RaycastHit();
			rootQueryHit = new GroundingQueryHit();
			if (legs == null || legs.Length != footCount) legs = new Leg[footCount];
			for (int i = 0; i < footCount; i++) {
				if (legs[i] == null) legs[i] = new Leg();
				legs[i].Initiate(this, i);
			}
			if (pelvis == null) pelvis = new Pelvis();
			pelvis.Initiate(this);
			initiated = true;
		}

		/// <summary>
		/// Updates the Grounding.
		/// </summary>
		public void Update() {
			GroundingFrameInput frame = BuildVendorFrame();
			Update(in frame, vendorBackend);
		}

		public void Update(in GroundingFrameInput frame, IGroundingWorldQueryBackend worldQueryBackend) {
			if (!initiated) return;
			if (worldQueryBackend == null) throw new System.ArgumentNullException(nameof(worldQueryBackend));
			if (frame.FootCount != legs.Length) throw new System.ArgumentException("Grounding frame foot count does not match initiated legs.", nameof(frame));
			if (frame.DeltaTime <= 0f || !float.IsFinite(frame.DeltaTime)) throw new System.ArgumentOutOfRangeException(nameof(frame), "Grounding frame delta must be finite and positive.");
			currentFrame = frame;
			hasCurrentFrame = true;

			if (frame.LayerMask == 0) LogWarning("Grounding layers are set to nothing. Please add a ground layer.");

			maxStep = Mathf.Clamp(maxStep, 0f, maxStep);
			footRadius = Mathf.Clamp(footRadius, 0.0001f, maxStep);
			pelvisDamper = Mathf.Clamp(pelvisDamper, 0f, 1f);
			rootSphereCastRadius = Mathf.Clamp(rootSphereCastRadius, 0.0001f, rootSphereCastRadius);
			maxFootRotationAngle = Mathf.Clamp(maxFootRotationAngle, 0f, 90f);
			prediction = Mathf.Clamp(prediction, 0f, prediction);
			footSpeed = Mathf.Clamp(footSpeed, 0f, footSpeed);

			// Root hit
			rootQueryHit = GetRootHit(in frame, worldQueryBackend, 10f);
			rootHit = rootQueryHit.PhysicsHit;

			float lowestOffset = Mathf.NegativeInfinity;
			float highestOffset = Mathf.Infinity;
			isGrounded = false;

			// Process legs
			for (int i = 0; i < legs.Length; i++) {
				Leg leg = legs[i];
				GroundingFootInput foot = frame.GetFoot(i);
				leg.Process(in frame, in foot, worldQueryBackend);

				if (leg.IKOffset > lowestOffset) lowestOffset = leg.IKOffset;
				if (leg.IKOffset < highestOffset) highestOffset = leg.IKOffset;

				if (leg.isGrounded) isGrounded = true;
			}

            // Precess pelvis
            lowestOffset = Mathf.Max(lowestOffset, 0f);
            highestOffset = Mathf.Min(highestOffset, 0f);
			pelvis.Process(-lowestOffset * lowerPelvisWeight, -highestOffset * liftPelvisWeight, isGrounded, in frame);
		}

		// Calculate the normal of the plane defined by leg positions, so we know how to rotate the body
		public Vector3 GetLegsPlaneNormal() {
			if (!initiated) return Vector3.up;

            Vector3 _up = up;
            Vector3 normal = _up;

			// Go through all the legs, rotate the normal by its offset
			for (int i = 0; i < legs.Length; i++) {
				// Direction from the root to the leg
				Vector3 legDirection = legs[i].IKPosition - rootPosition;

                // Find the tangent
				Vector3 legNormal = _up;
				Vector3 legTangent = legDirection;
				Vector3.OrthoNormalize(ref legNormal, ref legTangent);
				
                // Find the rotation offset from the tangent to the direction
                Quaternion fromTo = Quaternion.FromToRotation(legTangent, legDirection);
                
                // Rotate the normal
                normal = fromTo * normal;
			}
			
			return normal;
		}

		// Set everything to 0
		public void Reset() {
			if (!Application.isPlaying && !explicitInputMode) return;
			pelvis.Reset();
			foreach (Leg leg in legs) leg.Reset();
		}

		#endregion Main Interface
		
		private bool initiated;
		private bool explicitInputMode;
		private bool hasCurrentFrame;
		private GroundingFrameInput currentFrame;
		private VendorGroundingWorldQueryBackend vendorBackend;

		// Logs the warning if no other warning has beed logged in this session.
		public void LogWarning(string message) {
			Warning.Log(message, root);
		}
		
		// The up vector in solver rotation space.
		public Vector3 up {
			get {
				return (useRootRotation? rootUp: Vector3.up);
			}
		}

		internal Vector3 rootPosition => hasCurrentFrame ? currentFrame.Root.Position : root.position;
		internal Quaternion rootRotation => hasCurrentFrame ? currentFrame.Root.Rotation : root.rotation;
		internal Vector3 rootUp => hasCurrentFrame ? currentFrame.Root.Up : root.up;
		internal Vector3 rootRight => hasCurrentFrame ? currentFrame.Root.Right : root.right;
		internal Vector3 rootForward => hasCurrentFrame ? currentFrame.Root.Forward : root.forward;
		
		// Gets the vertical offset between two vectors in solver rotation space
		public float GetVerticalOffset(Vector3 p1, Vector3 p2) {
			if (useRootRotation) {
				Vector3 v = Quaternion.Inverse(rootRotation) * (p1 - p2);
				return v.y;
			}
			
			return p1.y - p2.y;
		}
		
		// Flattens a vector to ground plane in solver rotation space
		public Vector3 Flatten(Vector3 v) {
			if (useRootRotation) {
				Vector3 tangent = v;
				Vector3 normal = rootUp;
				Vector3.OrthoNormalize(ref normal, ref tangent);
				return Vector3.Project(v, tangent);
			}
			
			v.y = 0;
			return v;
		}
		
		// Determines whether to use root rotation as solver rotation
		private bool useRootRotation {
			get {
				if (!rotateSolver) return false;
				if (rootUp == Vector3.up) return false;
				return true;
			}
		}

		public Vector3 GetFootCenterOffset() {
			return rootForward * footRadius + rootForward * footCenterOffset;
		}

		private GroundingQueryHit GetRootHit(in GroundingFrameInput frame, IGroundingWorldQueryBackend backend, float maxDistanceMlp) {
			Vector3 solverUp = up;
			Vector3 legsCenter = Vector3.zero;
			for (int i = 0; i < frame.FootCount; i++) legsCenter += frame.GetFoot(i).Ankle.Position;
			legsCenter /= frame.FootCount;
			float distanceMultiplier = maxDistanceMlp + 1f;
			RaycastHit fallback = new RaycastHit {
				point = legsCenter - solverUp * maxStep * 10f,
				normal = solverUp,
				distance = maxStep * distanceMultiplier
			};
			if (maxStep <= 0f) return new GroundingQueryHit(false, fallback, 0);
			GroundingQueryRequest request = new GroundingQueryRequest(
				quality == Quality.Best ? GroundingQueryShape.Sphere : GroundingQueryShape.Ray,
				GroundingQueryPurpose.Root,
				frame.PhysicsScene,
				frame.LayerMask,
				-1,
				legsCenter + solverUp * maxStep,
				Vector3.zero,
				-solverUp,
				quality == Quality.Best ? rootSphereCastRadius : 0f,
				maxStep * distanceMultiplier);
			return backend.Query(in request, out GroundingQueryHit hit) ? hit : new GroundingQueryHit(false, fallback, 0);
		}

		private GroundingFrameInput BuildVendorFrame() {
			if (root == null || legs == null || legs.Length == 0) return default;
			GroundingComponentTransform rootTransform = new GroundingComponentTransform(root.position, root.rotation);
			GroundingFootInput left = BuildVendorFootInput(0);
			GroundingFootInput right = legs.Length > 1 ? BuildVendorFootInput(1) : default;
			return new GroundingFrameInput(
				Time.time,
				Mathf.Max(Time.deltaTime, 0.000001f),
				Physics.defaultPhysicsScene,
				layers,
				rootTransform,
				rootTransform,
				left,
				right,
				legs.Length);
		}

		private GroundingFootInput BuildVendorFootInput(int index) {
			Transform foot = legs[index].transform;
			GroundingComponentTransform ankle = new GroundingComponentTransform(foot.position, foot.rotation);
			Vector3 centerOffset = GetFootCenterOffset();
			if (legs[index].invertFootCenter) centerOffset = -centerOffset;
			GroundingComponentTransform center = new GroundingComponentTransform(foot.position + centerOffset, foot.rotation);
			return new GroundingFootInput(index, ankle, ankle, center, center);
		}

		private sealed class VendorGroundingWorldQueryBackend : IGroundingWorldQueryBackend {
			readonly Grounding grounding;

			public VendorGroundingWorldQueryBackend(Grounding grounding) {
				this.grounding = grounding;
			}

			public bool Query(in GroundingQueryRequest request, out GroundingQueryHit hit) {
				bool hasHit;
				RaycastHit physicsHit;
				switch (request.Shape) {
					case GroundingQueryShape.Ray:
						hasHit = grounding.Raycast(request.Origin, request.Direction, out physicsHit, request.MaxDistance, request.LayerMask, QueryTriggerInteraction.Ignore);
						break;
					case GroundingQueryShape.Sphere:
						hasHit = grounding.SphereCast(request.Origin, request.Radius, request.Direction, out physicsHit, request.MaxDistance, request.LayerMask, QueryTriggerInteraction.Ignore);
						break;
					case GroundingQueryShape.Capsule:
						hasHit = grounding.CapsuleCast(request.Origin, request.CapsuleEnd, request.Radius, request.Direction, out physicsHit, request.MaxDistance, request.LayerMask, QueryTriggerInteraction.Ignore);
						break;
					default:
						throw new System.ArgumentOutOfRangeException();
				}
				hit = new GroundingQueryHit(hasHit, physicsHit, hasHit && physicsHit.collider ? physicsHit.collider.GetInstanceID() : 0);
				return hasHit;
			}
		}
	}
}


