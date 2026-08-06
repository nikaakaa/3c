using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	///  Effector for manipulating node based %IK solvers.
	/// </summary>
	[System.Serializable]
	public class IKEffector {
		
		#region Main Interface

		/// <summary>
		/// Gets the main node.
		/// </summary>
		public IKSolver.Node GetNode(IKSolverFullBody solver) {
			return solver.chain[chainIndex].nodes[nodeIndex];
		}

		/// <summary>
		/// The node transform used by this effector.
		/// </summary>
		public Transform bone;
		public IndexedBoneHandle boneHandle = IndexedBoneHandle.Invalid;
		/// <summary>
		/// The target Transform (optional, you can use just the position and rotation instead).
		/// </summary>
		public Transform target;
		/// <summary>
		/// The position weight.
		/// </summary>
		[Range(0f, 1f)]
		public float positionWeight;
		/// <summary>
		/// The rotation weight.
		/// </summary>
		[Range(0f, 1f)]
		public float rotationWeight;
		/// <summary>
		/// The effector position in world space.
		/// </summary>
		public Vector3 position = Vector3.zero;
		/// <summary>
		/// The effector rotation relative to default rotation in world space.
		/// </summary>
		public Quaternion rotation = Quaternion.identity;
		/// <summary>
		/// The position offset in world space. positionOffset will be reset to Vector3.zero each frame after the solver is complete.
		/// </summary>
		public Vector3 positionOffset;
		/// <summary>
		/// Is this the last effector of a node chain?
		/// </summary>
		public bool isEndEffector { get; private set; }
		/// <summary>
		/// If false, child nodes will be ignored by this effector (if it has any).
		/// </summary>
		public bool effectChildNodes = true;
		/// <summary>
		/// Keeps the node position relative to the triangle defined by the plane bones (applies only to end-effectors).
		/// </summary>
		[Range(0f, 1f)]
		public float maintainRelativePositionWeight;

		/// <summary>
		/// Pins the effector to the animated position of its bone.
		/// </summary>
		public void PinToBone(float positionWeight, float rotationWeight) {
			position = bone.position;
			this.positionWeight = Mathf.Clamp(positionWeight, 0f, 1f);

			rotation = bone.rotation;
			this.rotationWeight = Mathf.Clamp(rotationWeight, 0f, 1f);
		}

		#endregion Main Interface

		public Transform[] childBones = new Transform[0]; // The optional list of other bones that positionOffset and position of this effector will be applied to.
		public IndexedBoneHandle[] childBoneHandles = new IndexedBoneHandle[0];
		public Transform planeBone1; // The first bone defining the parent plane.
		public Transform planeBone2; // The second bone defining the parent plane.
		public Transform planeBone3; // The third bone defining the parent plane.
		public IndexedBoneHandle planeBone1Handle = IndexedBoneHandle.Invalid;
		public IndexedBoneHandle planeBone2Handle = IndexedBoneHandle.Invalid;
		public IndexedBoneHandle planeBone3Handle = IndexedBoneHandle.Invalid;
		public Quaternion planeRotationOffset = Quaternion.identity; // Used by Bend Constraints

		private float posW, rotW;
		private Vector3[] localPositions = new Vector3[0];
		private bool usePlaneNodes;
		private Quaternion animatedPlaneRotation = Quaternion.identity;
		private Vector3 animatedPosition;
		private bool firstUpdate;
		private int chainIndex = -1;
		private int nodeIndex = -1;
		private int plane1ChainIndex;
		private int plane1NodeIndex = -1;
		private int plane2ChainIndex = -1;
		private int plane2NodeIndex = -1;
		private int plane3ChainIndex = -1;
		private int plane3NodeIndex = -1;
		private int[] childChainIndexes = new int[0];
		private int[] childNodeIndexes = new int[0];
		
		public IKEffector() {}
		
		public IKEffector (Transform bone, Transform[] childBones) {
			this.bone = bone;
			this.childBones = childBones;
		}
		
		/*
		 * Determines whether this IKEffector is valid or not.
		 * */
		public bool IsValid(IKSolver solver, ref string message) {
			if (solver.usesIndexedPoseBackend) {
				if (!boneHandle.IsValid || solver.GetPoint(boneHandle) == null) {
					message = "IK Effector indexed bone is invalid or missing from the Node Chain.";
					return false;
				}
				for (int i = 0; i < childBoneHandles.Length; i++) {
					if (!childBoneHandles[i].IsValid || solver.GetPoint(childBoneHandles[i]) == null) {
						message = "IK Effector contains an invalid indexed child bone.";
						return false;
					}
				}
				if (planeBone1Handle.IsValid && solver.GetPoint(planeBone1Handle) == null ||
					planeBone2Handle.IsValid && solver.GetPoint(planeBone2Handle) == null ||
					planeBone3Handle.IsValid && solver.GetPoint(planeBone3Handle) == null) {
					message = "IK Effector contains an indexed plane bone outside the Node Chain.";
					return false;
				}
				return true;
			}

			if (bone == null) {
				message = "IK Effector bone is null.";
				return false;
			}
			
			if (solver.GetPoint(bone) == null) {
				message = "IK Effector is referencing to a bone '" + bone.name + "' that does not excist in the Node Chain.";
				return false;
			}

			foreach (Transform b in childBones) {
				if (b == null) {
					message = "IK Effector contains a null reference.";
					return false;
				}
			}
			
			foreach (Transform b in childBones) {
				if (solver.GetPoint(b) == null) {
					message = "IK Effector is referencing to a bone '" + b.name + "' that does not excist in the Node Chain.";
					return false;
				}
			}

			if (planeBone1 != null && solver.GetPoint(planeBone1) == null) {
				message = "IK Effector is referencing to a bone '" + planeBone1.name + "' that does not excist in the Node Chain.";
				return false;
			}

			if (planeBone2 != null && solver.GetPoint(planeBone2) == null) {
				message = "IK Effector is referencing to a bone '" + planeBone2.name + "' that does not excist in the Node Chain.";
				return false;
			}

			if (planeBone3 != null && solver.GetPoint(planeBone3) == null) {
				message = "IK Effector is referencing to a bone '" + planeBone3.name + "' that does not excist in the Node Chain.";
				return false;
			}

			return true;
		}

		/*
		 * Initiate the effector, set default values
		 * */
		public void Initiate(IKSolverFullBody solver) {
			IKSolver.Point point = solver.usesIndexedPoseBackend ? solver.GetPoint(boneHandle) : solver.GetPoint(bone);
			position = solver.ReadComponentPosition(point);
			rotation = solver.ReadComponentRotation(point);
			animatedPlaneRotation = Quaternion.identity;

			if (solver.usesIndexedPoseBackend) solver.GetChainAndNodeIndexes(boneHandle, out chainIndex, out nodeIndex);
			else solver.GetChainAndNodeIndexes(bone, out chainIndex, out nodeIndex);

			int childCount = solver.usesIndexedPoseBackend ? childBoneHandles.Length : childBones.Length;
			childChainIndexes = new int[childCount];
			childNodeIndexes = new int[childCount];

			for (int i = 0; i < childCount; i++) {
				if (solver.usesIndexedPoseBackend) solver.GetChainAndNodeIndexes(childBoneHandles[i], out childChainIndexes[i], out childNodeIndexes[i]);
				else solver.GetChainAndNodeIndexes(childBones[i], out childChainIndexes[i], out childNodeIndexes[i]);
			}

			localPositions = new Vector3[childCount];

			// Plane nodes
			usePlaneNodes = false;

			bool hasPlane1 = solver.usesIndexedPoseBackend ? planeBone1Handle.IsValid : planeBone1 != null;
			bool hasPlane2 = solver.usesIndexedPoseBackend ? planeBone2Handle.IsValid : planeBone2 != null;
			bool hasPlane3 = solver.usesIndexedPoseBackend ? planeBone3Handle.IsValid : planeBone3 != null;
			if (hasPlane1) {
				if (solver.usesIndexedPoseBackend) solver.GetChainAndNodeIndexes(planeBone1Handle, out plane1ChainIndex, out plane1NodeIndex);
				else solver.GetChainAndNodeIndexes(planeBone1, out plane1ChainIndex, out plane1NodeIndex);

				if (hasPlane2) {
					if (solver.usesIndexedPoseBackend) solver.GetChainAndNodeIndexes(planeBone2Handle, out plane2ChainIndex, out plane2NodeIndex);
					else solver.GetChainAndNodeIndexes(planeBone2, out plane2ChainIndex, out plane2NodeIndex);

					if (hasPlane3) {
						if (solver.usesIndexedPoseBackend) solver.GetChainAndNodeIndexes(planeBone3Handle, out plane3ChainIndex, out plane3NodeIndex);
						else solver.GetChainAndNodeIndexes(planeBone3, out plane3ChainIndex, out plane3NodeIndex);

						usePlaneNodes = true;
					}
				}

				isEndEffector = true;
			} else isEndEffector = false; 
		}
		
		/*
		 * Clear node offset
		 * */
		public void ResetOffset(IKSolverFullBody solver) {
			solver.GetNode(chainIndex, nodeIndex).offset = Vector3.zero;

			for (int i = 0; i < childChainIndexes.Length; i++) {
				solver.GetNode(childChainIndexes[i], childNodeIndexes[i]).offset = Vector3.zero;
			}
		}

		/*
		 * Set the position and rotation to match the target
		 * */
		public void SetToTarget() {
			if (target == null) return;
			position = target.position;
			rotation = target.rotation;
		}
		
		/*
		 * Presolving, applying offset
		 * */
		public void OnPreSolve(IKSolverFullBody solver) {
			positionWeight = Mathf.Clamp(positionWeight, 0f, 1f);
			rotationWeight = Mathf.Clamp(rotationWeight, 0f, 1f);
			maintainRelativePositionWeight = Mathf.Clamp(maintainRelativePositionWeight, 0f, 1f);

			// Calculating weights
			posW = positionWeight * solver.IKPositionWeight;
			rotW = rotationWeight * solver.IKPositionWeight;

			solver.GetNode(chainIndex, nodeIndex).effectorPositionWeight = posW;
			solver.GetNode(chainIndex, nodeIndex).effectorRotationWeight = rotW;

			solver.GetNode(chainIndex, nodeIndex).solverRotation = rotation;

			if (float.IsInfinity(positionOffset.x) ||
				float.IsInfinity(positionOffset.y) ||
				float.IsInfinity(positionOffset.z)
			    ) Debug.LogError("Invalid IKEffector.positionOffset (contains Infinity)! Please make sure not to set IKEffector.positionOffset to infinite values.", bone);

			if (float.IsNaN(positionOffset.x) ||
			    float.IsNaN(positionOffset.y) ||
			    float.IsNaN(positionOffset.z)
			    ) Debug.LogError("Invalid IKEffector.positionOffset (contains NaN)! Please make sure not to set IKEffector.positionOffset to NaN values.", bone);

			if (positionOffset.sqrMagnitude > 10000000000f) Debug.LogError("Additive effector positionOffset detected in Full Body IK (extremely large value). Make sure you are not circularily adding to effector positionOffset each frame.", bone);

			if (float.IsInfinity(position.x) ||
			    float.IsInfinity(position.y) ||
			    float.IsInfinity(position.z)
			    ) Debug.LogError("Invalid IKEffector.position (contains Infinity)!");

			solver.GetNode(chainIndex, nodeIndex).offset += positionOffset * solver.IKPositionWeight;

			if (effectChildNodes && solver.iterations > 0) {
				for (int i = 0; i < childChainIndexes.Length; i++) {
					localPositions[i] = solver.ReadComponentPosition(solver.GetNode(childChainIndexes[i], childNodeIndexes[i])) - solver.ReadComponentPosition(solver.GetNode(chainIndex, nodeIndex));

					solver.GetNode(childChainIndexes[i], childNodeIndexes[i]).offset += positionOffset * solver.IKPositionWeight;
				}
			}

			// Relative to Plane
			if (usePlaneNodes && maintainRelativePositionWeight > 0f) {
				Vector3 plane1 = solver.ReadComponentPosition(solver.GetNode(plane1ChainIndex, plane1NodeIndex));
				Vector3 plane2 = solver.ReadComponentPosition(solver.GetNode(plane2ChainIndex, plane2NodeIndex));
				Vector3 plane3 = solver.ReadComponentPosition(solver.GetNode(plane3ChainIndex, plane3NodeIndex));
				animatedPlaneRotation = Quaternion.LookRotation(plane2 - plane1, plane3 - plane1);
			}

			firstUpdate = true;
		}

		/*
		 * Called after writing the pose
		 * */
		public void OnPostWrite() {
			positionOffset = Vector3.zero;
		}

		/*
		* Rotation of plane nodes in the solver
		* */
		private Quaternion GetPlaneRotation(IKSolverFullBody solver) {
			Vector3 p1 = solver.GetNode(plane1ChainIndex, plane1NodeIndex).solverPosition;
			Vector3 p2 = solver.GetNode(plane2ChainIndex, plane2NodeIndex).solverPosition;
			Vector3 p3 = solver.GetNode(plane3ChainIndex, plane3NodeIndex).solverPosition;

			Vector3 viewingVector = p2 - p1;
			Vector3 upVector = p3 - p1;

			if (viewingVector == Vector3.zero) {
				Warning.Log("Make sure you are not placing 2 or more FBBIK effectors of the same chain to exactly the same position.", bone);
				return Quaternion.identity;
			}
			
			return Quaternion.LookRotation(viewingVector, upVector);
		}

		/*
		 * Manipulating node solverPosition
		 * */
		public void Update(IKSolverFullBody solver) {
			if (firstUpdate) {
				animatedPosition = solver.ReadComponentPosition(solver.GetNode(chainIndex, nodeIndex)) + solver.GetNode(chainIndex, nodeIndex).offset;
				firstUpdate = false;
			}

			solver.GetNode(chainIndex, nodeIndex).solverPosition = Vector3.Lerp(GetPosition(solver, out planeRotationOffset), position, posW);

			// Child nodes
			if (!effectChildNodes) return;

			for (int i = 0; i < childChainIndexes.Length; i++) {
				solver.GetNode(childChainIndexes[i], childNodeIndexes[i]).solverPosition = Vector3.Lerp(solver.GetNode(childChainIndexes[i], childNodeIndexes[i]).solverPosition, solver.GetNode(chainIndex, nodeIndex).solverPosition + localPositions[i], posW);
			}
		}

		/*
		 * Gets the starting position of the iteration
		 * */
		private Vector3 GetPosition(IKSolverFullBody solver, out Quaternion planeRotationOffset) {
			planeRotationOffset = Quaternion.identity;
			if (!isEndEffector) return solver.GetNode(chainIndex, nodeIndex).solverPosition; // non end-effectors are always free

			if (maintainRelativePositionWeight <= 0f) return animatedPosition;

			// Maintain relative position
			Vector3 p = solver.ReadComponentPosition(solver.GetNode(chainIndex, nodeIndex));
			Vector3 dir = p - solver.ReadComponentPosition(solver.GetNode(plane1ChainIndex, plane1NodeIndex));
				
			planeRotationOffset = GetPlaneRotation(solver) * Quaternion.Inverse(animatedPlaneRotation);

			p = solver.GetNode(plane1ChainIndex, plane1NodeIndex).solverPosition + planeRotationOffset * dir;

			// Interpolate the rotation offset
			planeRotationOffset = Quaternion.Lerp(Quaternion.identity, planeRotationOffset, maintainRelativePositionWeight);

			return Vector3.Lerp(animatedPosition, p + solver.GetNode(chainIndex, nodeIndex).offset, maintainRelativePositionWeight);
		}
	}
}
