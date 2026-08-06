using UnityEngine;
using System.Collections;

	namespace RootMotion.FinalIK {

	/// <summary>
	/// %Constraint used for fixing bend direction of 3-segment node chains in a node based %IK solver. 
	/// </summary>
	[System.Serializable]
	public class IKConstraintBend {
		
		#region Main Interface

		/// <summary>
		/// The first bone.
		/// </summary>
		public Transform bone1;
		/// <summary>
		/// The second (bend) bone.
		/// </summary>
		public Transform bone2;
		/// <summary>
		/// The third bone.
		/// </summary>
		public Transform bone3;
		public IndexedBoneHandle bone1Handle = IndexedBoneHandle.Invalid;
		public IndexedBoneHandle bone2Handle = IndexedBoneHandle.Invalid;
		public IndexedBoneHandle bone3Handle = IndexedBoneHandle.Invalid;
		/// <summary>
		/// The bend goal Transform.
		/// </summary>
		public Transform bendGoal;
		
		/// <summary>
		/// The bend direction.
		/// </summary>
		public Vector3 direction = Vector3.right;

		/// <summary>
		/// The bend rotation offset.
		/// </summary>
		public Quaternion rotationOffset;
		
		/// <summary>
		/// The weight. If weight is 1, will override effector rotation and the joint will be rotated at the direction. This enables for direct manipulation of the bend direction independent of effector rotation.
		/// </summary>
		[Range(0f, 1f)]
		public float weight = 0f;
		
		/// <summary>
		/// Determines whether this IKConstraintBend is valid.
		/// </summary>
		public bool IsValid(IKSolverFullBody solver, Warning.Logger logger) {
			if (solver.usesIndexedPoseBackend) {
				if (!bone1Handle.IsValid || !bone2Handle.IsValid || !bone3Handle.IsValid) {
					if (logger != null) logger("Bend Constraint contains an invalid indexed bone reference.");
					return false;
				}
				if (solver.GetPoint(bone1Handle) == null || solver.GetPoint(bone2Handle) == null || solver.GetPoint(bone3Handle) == null) {
					if (logger != null) logger("Bend Constraint indexed bone does not exist in the Node Chain.");
					return false;
				}
				return true;
			}
			if (bone1 == null || bone2 == null || bone3 == null) {
				if (logger != null) logger("Bend Constraint contains a null reference.");
				return false;
			}
			if (solver.GetPoint(bone1) == null) {
				if (logger != null) logger("Bend Constraint is referencing to a bone '" + bone1.name + "' that does not excist in the Node Chain.");
				return false;
			}
			if (solver.GetPoint(bone2) == null) {
				if (logger != null) logger("Bend Constraint is referencing to a bone '" + bone2.name + "' that does not excist in the Node Chain.");
				return false;
			}
			if (solver.GetPoint(bone3) == null) {
				if (logger != null) logger("Bend Constraint is referencing to a bone '" + bone3.name + "' that does not excist in the Node Chain.");
				return false;
			}
			return true;
		}
		
		#endregion Main Interface

		public Vector3 defaultLocalDirection, defaultChildDirection;
        [System.NonSerializedAttribute] public float clampF = 0.505f;
        
        //private IKSolver.Node node1, node2, node3;
        private int chainIndex1;
		private int nodeIndex1;
		private int chainIndex2;
		private int nodeIndex2;
		private int chainIndex3;
		private int nodeIndex3;

		public bool initiated { get; private set; }
		private bool limbOrientationsSet;

		public IKConstraintBend() {}
		
		public IKConstraintBend(Transform bone1, Transform bone2, Transform bone3) {
			SetBones(bone1, bone2, bone3);
		}
		
		public void SetBones(Transform bone1, Transform bone2, Transform bone3) {
			this.bone1 = bone1;
			this.bone2 = bone2;
			this.bone3 = bone3;
		}

		public void SetBones(IndexedBoneHandle bone1, IndexedBoneHandle bone2, IndexedBoneHandle bone3) {
			bone1Handle = bone1;
			bone2Handle = bone2;
			bone3Handle = bone3;
			this.bone1 = null;
			this.bone2 = null;
			this.bone3 = null;
		}
		
		/*
		 * Initiate the constraint and set defaults
		 * */
		public void Initiate(IKSolverFullBody solver) {
			if (solver.usesIndexedPoseBackend) {
				solver.GetChainAndNodeIndexes(bone1Handle, out chainIndex1, out nodeIndex1);
				solver.GetChainAndNodeIndexes(bone2Handle, out chainIndex2, out nodeIndex2);
				solver.GetChainAndNodeIndexes(bone3Handle, out chainIndex3, out nodeIndex3);
			} else {
				solver.GetChainAndNodeIndexes(bone1, out chainIndex1, out nodeIndex1);
				solver.GetChainAndNodeIndexes(bone2, out chainIndex2, out nodeIndex2);
				solver.GetChainAndNodeIndexes(bone3, out chainIndex3, out nodeIndex3);
			}
			IKSolver.Node node1 = solver.GetNode(chainIndex1, nodeIndex1);
			IKSolver.Node node2 = solver.GetNode(chainIndex2, nodeIndex2);
			IKSolver.Node node3 = solver.GetNode(chainIndex3, nodeIndex3);
			Vector3 position1 = solver.ReadComponentPosition(node1);
			Vector3 position2 = solver.ReadComponentPosition(node2);
			Vector3 position3 = solver.ReadComponentPosition(node3);
			Quaternion rotation1 = solver.ReadComponentRotation(node1);
			Quaternion rotation3 = solver.ReadComponentRotation(node3);

			direction = OrthoToBone1(solver, OrthoToLimb(solver, position2 - position1));

			if (!limbOrientationsSet) {
				defaultLocalDirection = Quaternion.Inverse(rotation1) * direction;

				Vector3 defaultNormal = Vector3.Cross((position3 - position1).normalized, direction);
				
				defaultChildDirection = Quaternion.Inverse(rotation3) * defaultNormal;
			}

			initiated = true;
		}

		/*
		 * Make the limb bend towards the specified local directions of the bones
		 * */
		public void SetLimbOrientation(Vector3 upper, Vector3 lower, Vector3 last) {
			if (upper == Vector3.zero) Debug.LogError("Attempting to set limb orientation to Vector3.zero axis");
			if (lower == Vector3.zero) Debug.LogError("Attempting to set limb orientation to Vector3.zero axis");
			if (last == Vector3.zero) Debug.LogError("Attempting to set limb orientation to Vector3.zero axis");
			
			// Default bend direction relative to the first node
			defaultLocalDirection = upper.normalized;
			defaultChildDirection = last.normalized;

			limbOrientationsSet = true;
		}

		/*
		 * Limits the bending joint of the limb to 90 degrees from the default 90 degrees of bend direction
		 * */
		public void LimitBend(IKSolverFullBody solver, float solverWeight, float positionWeight) {
			if (!initiated) return;
			IKSolver.Node node1 = solver.GetNode(chainIndex1, nodeIndex1);
			IKSolver.Node node2 = solver.GetNode(chainIndex2, nodeIndex2);
			IKSolver.Node node3 = solver.GetNode(chainIndex3, nodeIndex3);
			Vector3 position1 = solver.ReadComponentPosition(node1);
			Vector3 position2 = solver.ReadComponentPosition(node2);
			Vector3 position3 = solver.ReadComponentPosition(node3);
			Quaternion rotation1 = solver.ReadComponentRotation(node1);
			Quaternion rotation2 = solver.ReadComponentRotation(node2);
			Quaternion rotation3 = solver.ReadComponentRotation(node3);

			Vector3 normalDirection = rotation1 * -defaultLocalDirection;
			
			Vector3 axis2 = position3 - position2;

			// Clamp the direction from knee/elbow to foot/hand to valid range (90 degrees from right-angledly bent limb)
			bool changed = false;
			Vector3 clampedAxis2 = V3Tools.ClampDirection(axis2, normalDirection, clampF * solverWeight, 0, out changed);

			Quaternion bone3Rotation = rotation3;

			if (changed) {
				Quaternion f = Quaternion.FromToRotation(axis2, clampedAxis2); 
				rotation2 = f * rotation2;
				solver.WriteComponentRotation(node2, rotation2);
			}

			// Rotating bend direction to normal when the limb is stretched out
			if (positionWeight > 0f) {
				Vector3 normal = position2 - position1;
				Vector3 tangent = position3 - position2;

				Vector3.OrthoNormalize(ref normal, ref tangent);
				Quaternion q = Quaternion.FromToRotation(tangent, normalDirection);

				rotation2 = Quaternion.Lerp(rotation2, q * rotation2, positionWeight * solverWeight);
				solver.WriteComponentRotation(node2, rotation2);
			}

			if (changed || positionWeight > 0f) solver.WriteComponentRotation(node3, bone3Rotation);
		}

		/*
		 * Computes the direction from the first node to the second node
		 * */
		public Vector3 GetDir(IKSolverFullBody solver) {
			if (!initiated) return Vector3.zero;

			float w = weight * solver.IKPositionWeight;

			// Apply the bend goal
			if (bendGoal != null) {
				Vector3 b = bendGoal.position - solver.GetNode(chainIndex1, nodeIndex1).solverPosition;
				if (b != Vector3.zero) direction = b;
			}

			if (w >= 1f) return direction.normalized;

			Vector3 solverDirection = solver.GetNode(chainIndex3, nodeIndex3).solverPosition - solver.GetNode(chainIndex1, nodeIndex1).solverPosition;

			// Get rotation from animated limb direction to solver limb direction
			Vector3 position1 = solver.ReadComponentPosition(solver.GetNode(chainIndex1, nodeIndex1));
			Vector3 position2 = solver.ReadComponentPosition(solver.GetNode(chainIndex2, nodeIndex2));
			Vector3 position3 = solver.ReadComponentPosition(solver.GetNode(chainIndex3, nodeIndex3));
			Quaternion f = Quaternion.FromToRotation(position3 - position1, solverDirection);

			Vector3 dir = f * (position2 - position1);

			// Effector rotation
			if (solver.GetNode(chainIndex3, nodeIndex3).effectorRotationWeight > 0f) {
				// Bend direction according to the effector rotation
				Vector3 effectorDirection = -Vector3.Cross(solverDirection, solver.GetNode(chainIndex3, nodeIndex3).solverRotation * defaultChildDirection);
				dir = Vector3.Lerp(dir, effectorDirection, solver.GetNode(chainIndex3, nodeIndex3).effectorRotationWeight);
			}

			// Rotation Offset
			if (rotationOffset != Quaternion.identity) {
				Quaternion toOrtho = Quaternion.FromToRotation(rotationOffset * solverDirection, solverDirection);
				dir = toOrtho * rotationOffset * dir;
			}

			if (w <= 0f) return dir;
			return Vector3.Lerp(dir, direction.normalized, w); 
		}

		/*
		 * Ortho-Normalize a vector to the chain direction
		 * */
		private Vector3 OrthoToLimb(IKSolverFullBody solver, Vector3 tangent) {
			Vector3 normal = solver.GetNode(chainIndex3, nodeIndex3).solverPosition - solver.GetNode(chainIndex1, nodeIndex1).solverPosition;
			Vector3.OrthoNormalize(ref normal, ref tangent);
			return tangent;
		}

		/*
		 * Ortho-Normalize a vector to the first bone direction
		 * */
		private Vector3 OrthoToBone1(IKSolverFullBody solver, Vector3 tangent) {
			Vector3 normal = solver.GetNode(chainIndex2, nodeIndex2).solverPosition - solver.GetNode(chainIndex1, nodeIndex1).solverPosition;
			Vector3.OrthoNormalize(ref normal, ref tangent);
			return tangent;
		}
	}
}
