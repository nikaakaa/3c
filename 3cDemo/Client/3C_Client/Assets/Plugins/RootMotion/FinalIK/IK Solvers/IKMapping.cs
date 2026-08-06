using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	/// Maps a bone or a collection of bones to a node based %IK solver
	/// </summary>
	[System.Serializable]
	public class IKMapping {
		
		#region Main Interface
		
		/// <summary>
		/// Contains mapping information of a single bone
		/// </summary>
		[System.Serializable]
		public class BoneMap {
			/// <summary>
			/// The transform.
			/// </summary>
			public Transform transform;
			public IndexedBoneHandle boneHandle = IndexedBoneHandle.Invalid;
			/// <summary>
			/// The node in %IK Solver.
			/// </summary>
			//public IKSolver.Node node;

			public int chainIndex = -1;
			public int nodeIndex = -1;

			public Vector3 defaultLocalPosition;
			public Quaternion defaultLocalRotation;
			public Vector3 localSwingAxis, localTwistAxis, planePosition, ikPosition;
			public Quaternion defaultLocalTargetRotation;
			private Quaternion maintainRotation;
			public float length;
			public Quaternion animatedRotation;

			private Transform planeBone1, planeBone2, planeBone3;
			private IndexedBoneHandle planeBone1Handle = IndexedBoneHandle.Invalid;
			private IndexedBoneHandle planeBone2Handle = IndexedBoneHandle.Invalid;
			private IndexedBoneHandle planeBone3Handle = IndexedBoneHandle.Invalid;
			private IKSolverFullBody solver;
			private int plane1ChainIndex = -1;
			private int plane1NodeIndex = -1;
			private int plane2ChainIndex = -1;
			private int plane2NodeIndex = -1;
			private int plane3ChainIndex = -1;
			private int plane3NodeIndex = -1;

			//private IKSolver.Node planeNode1, planeNode2, planeNode3;

			public void Initiate(Transform transform, IKSolverFullBody solver) {
				this.transform = transform;
				boneHandle = IndexedBoneHandle.Invalid;
				this.solver = solver;

				solver.GetChainAndNodeIndexes(transform, out chainIndex, out nodeIndex);
				//IKSolver.Point point = solver.GetPoint(transform);
				//this.node = point as IKSolver.Node;
			}

			public void Initiate(IndexedBoneHandle boneHandle, IKSolverFullBody solver) {
				transform = null;
				this.boneHandle = boneHandle;
				this.solver = solver;
				solver.GetChainAndNodeIndexes(boneHandle, out chainIndex, out nodeIndex);
			}

			/// <summary>
			/// Gets the current swing direction of the bone in world space.
			/// </summary>
			public Vector3 swingDirection {
				get {
					return rotation * localSwingAxis;
				}
			}

			public void StoreDefaultLocalState() {
				defaultLocalPosition = localPosition;
				defaultLocalRotation = localRotation;
			}
			
			public void FixTransform(bool position) {
				if (position) localPosition = defaultLocalPosition;
				localRotation = defaultLocalRotation;
			}
			
			#region Reading
			
			/*
			 * Does this bone have a node in the IK Solver?
			 * */
			public bool isNodeBone {
				get {
					return nodeIndex != -1;
					//return node != null;
				}
			}
			
			/*
			 * Calculate length of the bone
			 * */
			public void SetLength(BoneMap nextBone) {
				length = Vector3.Distance(this.position, nextBone.position);
			}
			
			/*
			 * Sets the direction to the swing target in local space
			 * */
			public void SetLocalSwingAxis(BoneMap swingTarget) {
				SetLocalSwingAxis(swingTarget, this);
			}
			
			/*
			 * Sets the direction to the swing target in local space
			 * */
			public void SetLocalSwingAxis(BoneMap bone1, BoneMap bone2) {
				localSwingAxis = Quaternion.Inverse(rotation) * (bone1.position - bone2.position);
			}
			
			/*
			 * Sets the direction to the twist target in local space
			 * */
			public void SetLocalTwistAxis(Vector3 twistDirection, Vector3 normalDirection) {
				Vector3.OrthoNormalize(ref normalDirection, ref twistDirection);
				localTwistAxis = Quaternion.Inverse(rotation) * twistDirection;
			}

			/*
			 * Sets the 3 points defining a plane for this bone
			 * */
			public void SetPlane(IKSolverFullBody solver, Transform planeBone1, Transform planeBone2, Transform planeBone3) {
				this.planeBone1 = planeBone1;
				this.planeBone2 = planeBone2;
				this.planeBone3 = planeBone3;

				solver.GetChainAndNodeIndexes(planeBone1, out plane1ChainIndex, out plane1NodeIndex);
				solver.GetChainAndNodeIndexes(planeBone2, out plane2ChainIndex, out plane2NodeIndex);
				solver.GetChainAndNodeIndexes(planeBone3, out plane3ChainIndex, out plane3NodeIndex);

				//this.planeNode1 = planeNode1;
				//this.planeNode2 = planeNode2;
				//this.planeNode3 = planeNode3;
				
				UpdatePlane(true, true);
			}

			public void SetPlane(IKSolverFullBody solver, IndexedBoneHandle planeBone1, IndexedBoneHandle planeBone2, IndexedBoneHandle planeBone3) {
				planeBone1Handle = planeBone1;
				planeBone2Handle = planeBone2;
				planeBone3Handle = planeBone3;
				this.planeBone1 = null;
				this.planeBone2 = null;
				this.planeBone3 = null;
				solver.GetChainAndNodeIndexes(planeBone1, out plane1ChainIndex, out plane1NodeIndex);
				solver.GetChainAndNodeIndexes(planeBone2, out plane2ChainIndex, out plane2NodeIndex);
				solver.GetChainAndNodeIndexes(planeBone3, out plane3ChainIndex, out plane3NodeIndex);
				UpdatePlane(true, true);
			}
			
			/*
			 * Updates the 3 plane points
			 * */
			public void UpdatePlane(bool rotation, bool position) {
				Quaternion t = lastAnimatedTargetRotation;

				if (rotation) defaultLocalTargetRotation = QuaTools.RotationToLocalSpace(this.rotation, t);
				if (position) planePosition = Quaternion.Inverse(t) * (this.position - planePosition1);
			}
			
			/*
			 * Sets the virtual position for this bone
			 * */
			public void SetIKPosition() {
				ikPosition = position;
			}

			/*
			 * Stores the current rotation for later use.
			 * */
			public void MaintainRotation() {
				maintainRotation = rotation;
			}
			
			#endregion Reading
			
			#region Writing
			
			/*
			 * Moves the bone to its virtual position
			 * */
			public void SetToIKPosition() {
				position = ikPosition;
			}
			
			/*
			 * Moves the bone to the solver position of its node
			 * */
			public void FixToNode(IKSolverFullBody solver, float weight, IKSolver.Node fixNode = null) {
				if (fixNode == null) fixNode = solver.GetNode(chainIndex, nodeIndex);

				if (weight >= 1f) {
					position = fixNode.solverPosition;
					return;
				}

				position = Vector3.Lerp(position, fixNode.solverPosition, weight);
			}
			
			/*
			 * Gets the bone's position relative to its 3 plane nodes
			 * */
			public Vector3 GetPlanePosition(IKSolverFullBody solver) {
				return solver.GetNode(plane1ChainIndex, plane1NodeIndex).solverPosition + (GetTargetRotation(solver) * planePosition);
				//return planeNode1.solverPosition + (targetRotation * planePosition);
			}
			
			/*
			 * Positions the bone relative to its 3 plane nodes
			 * */
			public void PositionToPlane(IKSolverFullBody solver) {
				position = GetPlanePosition(solver);
			}
			
			/*
			 * Rotates the bone relative to its 3 plane nodes
			 * */
			public void RotateToPlane(IKSolverFullBody solver, float weight) {
				Quaternion r = GetTargetRotation(solver) * defaultLocalTargetRotation;

				if (weight >= 1f) {
					rotation = r;
					return;
				}

				rotation = Quaternion.Lerp(rotation, r, weight);
			}

			/*
			 * Swings to the swing target
			 * */
			public void Swing(Vector3 swingTarget, float weight) {
				Swing(swingTarget, position, weight);
			}
			
			/*
			 * Swings to a direction from pos2 to pos1
			 * */
			public void Swing(Vector3 pos1, Vector3 pos2, float weight) {
				Quaternion currentRotation = rotation;
				Quaternion r = Quaternion.FromToRotation(currentRotation * localSwingAxis, pos1 - pos2) * currentRotation;

				if (weight >= 1f) {
					rotation = r;
					return;
				}

				rotation = Quaternion.Lerp(currentRotation, r, weight);
			}
			
			/*
			 * Twists to the twist target
			 * */
			public void Twist(Vector3 twistDirection, Vector3 normalDirection, float weight) {
				Vector3.OrthoNormalize(ref normalDirection, ref twistDirection);

				Quaternion currentRotation = rotation;
				Quaternion r = Quaternion.FromToRotation(currentRotation * localTwistAxis, twistDirection) * currentRotation;

				if (weight >= 1f) {
					rotation = r;
					return;
				}

				rotation = Quaternion.Lerp(currentRotation, r, weight);
			}

			/*
			 * Rotates back to the last animated local rotation
			 * */
			public void RotateToMaintain(float weight) {
				if (weight <= 0f) return;

				rotation = Quaternion.Lerp(rotation, maintainRotation, weight);
			}
			
			/*
			 * Rotates to match the effector rotation
			 * */
			public void RotateToEffector(IKSolverFullBody solver, float weight) {
				if (!isNodeBone) return;
				float w = weight * solver.GetNode(chainIndex, nodeIndex).effectorRotationWeight;
				if (w <= 0f) return;

				if (w >= 1f) {
					rotation = solver.GetNode(chainIndex, nodeIndex).solverRotation;
					return;
				}

				rotation = Quaternion.Lerp(rotation, solver.GetNode(chainIndex, nodeIndex).solverRotation, w);
			}
			
			#endregion Writing
			
			/*
			 * Rotation of plane nodes in the solver
			 * */
			private Quaternion GetTargetRotation(IKSolverFullBody solver) {
				Vector3 p1 = solver.GetNode(plane1ChainIndex, plane1NodeIndex).solverPosition;
				Vector3 p2 = solver.GetNode(plane2ChainIndex, plane2NodeIndex).solverPosition;
				Vector3 p3 = solver.GetNode(plane3ChainIndex, plane3NodeIndex).solverPosition;

				if (p1 == p3) return Quaternion.identity;
				return Quaternion.LookRotation(p2 - p1, p3 - p1);

				//if (planeNode1.solverPosition == planeNode3.solverPosition) return Quaternion.identity;
				//return Quaternion.LookRotation(planeNode2.solverPosition - planeNode1.solverPosition, planeNode3.solverPosition - planeNode1.solverPosition);
			}
			
			/*
			 * Rotation of plane nodes in the animation
			 * */
			private Quaternion lastAnimatedTargetRotation {
				get {
					if (planePosition1 == planePosition3) return Quaternion.identity;
					return Quaternion.LookRotation(planePosition2 - planePosition1, planePosition3 - planePosition1);
				}
			}

			private Vector3 position {
				get { return solver.usesIndexedPoseBackend ? solver.poseBackend.GetComponentPosition(boneHandle) : transform.position; }
				set {
					if (solver.usesIndexedPoseBackend) solver.poseBackend.SetComponentPosition(boneHandle, value);
					else transform.position = value;
				}
			}

			private Quaternion rotation {
				get { return solver.usesIndexedPoseBackend ? solver.poseBackend.GetComponentRotation(boneHandle) : transform.rotation; }
				set {
					if (solver.usesIndexedPoseBackend) solver.poseBackend.SetComponentRotation(boneHandle, value);
					else transform.rotation = value;
				}
			}

			private Vector3 localPosition {
				get { return solver.usesIndexedPoseBackend ? solver.poseBackend.GetLocalPosition(boneHandle) : transform.localPosition; }
				set {
					if (solver.usesIndexedPoseBackend) solver.poseBackend.SetLocalPosition(boneHandle, value);
					else transform.localPosition = value;
				}
			}

			private Quaternion localRotation {
				get { return solver.usesIndexedPoseBackend ? solver.poseBackend.GetLocalRotation(boneHandle) : transform.localRotation; }
				set {
					if (solver.usesIndexedPoseBackend) solver.poseBackend.SetLocalRotation(boneHandle, value);
					else transform.localRotation = value;
				}
			}

			public Vector3 GetComponentPosition() => position;

			public void SetComponentPosition(Vector3 value) => position = value;

			private Vector3 planePosition1 => solver.usesIndexedPoseBackend ? solver.poseBackend.GetComponentPosition(planeBone1Handle) : planeBone1.position;
			private Vector3 planePosition2 => solver.usesIndexedPoseBackend ? solver.poseBackend.GetComponentPosition(planeBone2Handle) : planeBone2.position;
			private Vector3 planePosition3 => solver.usesIndexedPoseBackend ? solver.poseBackend.GetComponentPosition(planeBone3Handle) : planeBone3.position;
		}
		
		/// <summary>
		/// Determines whether this IKMapping is valid.
		/// </summary>
		public virtual bool IsValid(IKSolver solver, ref string message) {
			return true;
		}

		#endregion Main Interface
		
		public virtual void Initiate(IKSolverFullBody solver) {}
		
		protected bool BoneIsValid(Transform bone, IKSolver solver, ref string message, Warning.Logger logger = null) {
			if (bone == null) {
				message = "IKMappingLimb contains a null reference.";
				if (logger != null) logger(message);
				return false;
			}
			if (solver.GetPoint(bone) == null) {
				message = "IKMappingLimb is referencing to a bone '" + bone.name + "' that does not excist in the Node Chain.";
				if (logger != null) logger(message);
				return false;
			}
			return true;
		}

		protected bool BoneIsValid(IndexedBoneHandle bone, IKSolver solver, ref string message, Warning.Logger logger = null) {
			if (!bone.IsValid) {
				message = "IKMapping contains an invalid indexed bone reference.";
				if (logger != null) logger(message);
				return false;
			}
			if (solver.GetPoint(bone) == null) {
				message = "IKMapping references an indexed bone that does not exist in the Node Chain.";
				if (logger != null) logger(message);
				return false;
			}
			return true;
		}

		/*
		 * Interpolates the joint position to match the bone's length
		*/
		protected Vector3 SolveFABRIKJoint(Vector3 pos1, Vector3 pos2, float length) {
			return pos2 + (pos1 - pos2).normalized * length;
		}
	}
}
