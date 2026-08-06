using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	/// Maps a single bone to a node in %IK Solver
	/// </summary>
	[System.Serializable]
	public class IKMappingBone: IKMapping {
		
		#region Main Interface
		
		/// <summary>
		/// The bone transform.
		/// </summary>
		public Transform bone;
		public IndexedBoneHandle boneHandle = IndexedBoneHandle.Invalid;
		
		/// <summary>
		/// The weight of maintaining the bone's rotation after solver has finished.
		/// </summary>
		[Range(0f, 1f)]
		public float maintainRotationWeight = 1f;
		
		/// <summary>
		/// Determines whether this IKMappingBone is valid.
		/// </summary>
		public override bool IsValid(IKSolver solver, ref string message) {
			if (!base.IsValid(solver, ref message)) return false;
			if (solver.usesIndexedPoseBackend) {
				if (!boneHandle.IsValid) {
					message = "IKMappingBone indexed bone is invalid.";
					return false;
				}
				return true;
			}
			
			if (bone == null) {
				message = "IKMappingBone's bone is null.";
				return false;
			}

			return true;
		}
		
		#endregion Main Interface
		
		private BoneMap boneMap = new BoneMap();
		
		public IKMappingBone() {}
		
		public IKMappingBone(Transform bone) {
			this.bone = bone;
		}

		public void StoreDefaultLocalState() {
			boneMap.StoreDefaultLocalState();
		}
		
		public void FixTransforms() {
			boneMap.FixTransform(false);
		}
		
		/*
		 * Initiating and setting defaults
		 * */
		public override void Initiate(IKSolverFullBody solver) {
			if (boneMap == null) boneMap = new BoneMap();

			if (solver.usesIndexedPoseBackend) boneMap.Initiate(boneHandle, solver);
			else boneMap.Initiate(bone, solver);
		}
		
		/*
		 * Pre-solving
		 * */
		public void ReadPose() {
			boneMap.MaintainRotation();
		}
		
		public void WritePose(float solverWeight) {
			// Rotating back to the last maintained rotation
			boneMap.RotateToMaintain(solverWeight * maintainRotationWeight);
		}
	}
}
