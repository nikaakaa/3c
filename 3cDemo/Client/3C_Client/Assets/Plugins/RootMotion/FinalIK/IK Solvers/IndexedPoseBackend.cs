using System;
using UnityEngine;

namespace RootMotion.FinalIK {

	public readonly struct IndexedBoneHandle : IEquatable<IndexedBoneHandle> {
		public static IndexedBoneHandle Invalid => new IndexedBoneHandle(-1);

		public IndexedBoneHandle(int index) {
			Index = index;
		}

		public int Index { get; }
		public bool IsValid => Index >= 0;

		public bool Equals(IndexedBoneHandle other) => Index == other.Index;
		public override bool Equals(object obj) => obj is IndexedBoneHandle other && Equals(other);
		public override int GetHashCode() => Index;
		public static bool operator ==(IndexedBoneHandle left, IndexedBoneHandle right) => left.Equals(right);
		public static bool operator !=(IndexedBoneHandle left, IndexedBoneHandle right) => !left.Equals(right);
	}

	public interface IIndexedPoseBackend {
		int BoneCount { get; }
		IndexedBoneHandle GetParent(IndexedBoneHandle bone);
		Vector3 GetComponentPosition(IndexedBoneHandle bone);
		Quaternion GetComponentRotation(IndexedBoneHandle bone);
		Vector3 GetLocalPosition(IndexedBoneHandle bone);
		Quaternion GetLocalRotation(IndexedBoneHandle bone);
		Vector3 GetReferenceComponentPosition(IndexedBoneHandle bone);
		Quaternion GetReferenceComponentRotation(IndexedBoneHandle bone);
		void SetComponentPosition(IndexedBoneHandle bone, Vector3 position);
		void SetComponentRotation(IndexedBoneHandle bone, Quaternion rotation);
		void SetLocalPosition(IndexedBoneHandle bone, Vector3 position);
		void SetLocalRotation(IndexedBoneHandle bone, Quaternion rotation);
		bool IsWritablePhysicalBone(IndexedBoneHandle bone);
	}

	public readonly struct IndexedBipedReferences {
		public IndexedBipedReferences(
			IndexedBoneHandle root,
			IndexedBoneHandle solverRoot,
			IndexedBoneHandle pelvis,
			IndexedBoneHandle[] spine,
			IndexedBoneHandle head,
			IndexedBoneHandle leftClavicle,
			IndexedBoneHandle leftUpperArm,
			IndexedBoneHandle leftForearm,
			IndexedBoneHandle leftHand,
			IndexedBoneHandle rightClavicle,
			IndexedBoneHandle rightUpperArm,
			IndexedBoneHandle rightForearm,
			IndexedBoneHandle rightHand,
			IndexedBoneHandle leftThigh,
			IndexedBoneHandle leftCalf,
			IndexedBoneHandle leftFoot,
			IndexedBoneHandle rightThigh,
			IndexedBoneHandle rightCalf,
			IndexedBoneHandle rightFoot) {
			Root = root;
			SolverRoot = solverRoot;
			Pelvis = pelvis;
			Spine = spine ?? throw new ArgumentNullException(nameof(spine));
			Head = head;
			LeftClavicle = leftClavicle;
			LeftUpperArm = leftUpperArm;
			LeftForearm = leftForearm;
			LeftHand = leftHand;
			RightClavicle = rightClavicle;
			RightUpperArm = rightUpperArm;
			RightForearm = rightForearm;
			RightHand = rightHand;
			LeftThigh = leftThigh;
			LeftCalf = leftCalf;
			LeftFoot = leftFoot;
			RightThigh = rightThigh;
			RightCalf = rightCalf;
			RightFoot = rightFoot;
		}

		public IndexedBoneHandle Root { get; }
		public IndexedBoneHandle SolverRoot { get; }
		public IndexedBoneHandle Pelvis { get; }
		public IndexedBoneHandle[] Spine { get; }
		public IndexedBoneHandle Head { get; }
		public IndexedBoneHandle LeftClavicle { get; }
		public IndexedBoneHandle LeftUpperArm { get; }
		public IndexedBoneHandle LeftForearm { get; }
		public IndexedBoneHandle LeftHand { get; }
		public IndexedBoneHandle RightClavicle { get; }
		public IndexedBoneHandle RightUpperArm { get; }
		public IndexedBoneHandle RightForearm { get; }
		public IndexedBoneHandle RightHand { get; }
		public IndexedBoneHandle LeftThigh { get; }
		public IndexedBoneHandle LeftCalf { get; }
		public IndexedBoneHandle LeftFoot { get; }
		public IndexedBoneHandle RightThigh { get; }
		public IndexedBoneHandle RightCalf { get; }
		public IndexedBoneHandle RightFoot { get; }
	}
}
