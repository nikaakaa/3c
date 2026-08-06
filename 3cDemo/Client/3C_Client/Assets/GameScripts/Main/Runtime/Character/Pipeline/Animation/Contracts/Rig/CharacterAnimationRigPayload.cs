using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationPhysicalBonePayload
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] int m_ParentPhysicalIndex = -1;
        [SerializeField] Vector3 m_ReferenceLocalPosition;
        [SerializeField] Quaternion m_ReferenceLocalRotation = Quaternion.identity;
        [SerializeField] Vector3 m_ReferenceLocalScale = Vector3.one;

        public CharacterAnimationPhysicalBonePayload(CharacterAnimationPhysicalBoneDefinition source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_BoneId = source.BoneId.Value;
            m_ParentPhysicalIndex = source.ParentIndex;
            m_ReferenceLocalPosition = source.ReferenceLocalPosition;
            m_ReferenceLocalRotation = source.ReferenceLocalRotation;
            m_ReferenceLocalScale = source.ReferenceLocalScale;
        }

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public int ParentPhysicalIndex => m_ParentPhysicalIndex;
        public Vector3 ReferenceLocalPosition => m_ReferenceLocalPosition;
        public Quaternion ReferenceLocalRotation => m_ReferenceLocalRotation;
        public Vector3 ReferenceLocalScale => m_ReferenceLocalScale;
    }

    [Serializable]
    public sealed class CharacterAnimationVirtualBonePayload
    {
        [SerializeField] string m_VirtualBoneId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] int m_SourcePhysicalBoneIndex = -1;
        [SerializeField] int m_TargetPhysicalBoneIndex = -1;
        [SerializeField] int m_PoseBoneIndex = -1;
        [SerializeField] Vector3 m_ReferenceLocalPosition;
        [SerializeField] Quaternion m_ReferenceLocalRotation = Quaternion.identity;

        public CharacterAnimationVirtualBonePayload(
            CharacterAnimationVirtualBoneDefinition source,
            int sourcePhysicalBoneIndex,
            int targetPhysicalBoneIndex,
            int poseBoneIndex,
            AnimationLocalBonePose referenceLocalPose)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!referenceLocalPose.IsValid)
                throw new ArgumentException("Virtual Bone reference pose is invalid.", nameof(referenceLocalPose));
            m_VirtualBoneId = source.VirtualBoneId.Value;
            m_DisplayName = source.DisplayName;
            m_SourcePhysicalBoneIndex = sourcePhysicalBoneIndex;
            m_TargetPhysicalBoneIndex = targetPhysicalBoneIndex;
            m_PoseBoneIndex = poseBoneIndex;
            m_ReferenceLocalPosition = referenceLocalPose.Position;
            m_ReferenceLocalRotation = referenceLocalPose.Rotation;
        }

        public AnimationBoneId VirtualBoneId => string.IsNullOrWhiteSpace(m_VirtualBoneId) ? default : new AnimationBoneId(m_VirtualBoneId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public int SourcePhysicalBoneIndex => m_SourcePhysicalBoneIndex;
        public int TargetPhysicalBoneIndex => m_TargetPhysicalBoneIndex;
        public int PoseBoneIndex => m_PoseBoneIndex;
        public Vector3 ReferenceLocalPosition => m_ReferenceLocalPosition;
        public Quaternion ReferenceLocalRotation => m_ReferenceLocalRotation;
        public Vector3 ReferenceLocalScale => Vector3.one;
    }

    [Serializable]
    public sealed class CharacterAnimationArmChainPayload
    {
        [SerializeField] int m_ClaviclePhysicalBoneIndex = -1;
        [SerializeField] int m_UpperArmPhysicalBoneIndex = -1;
        [SerializeField] int m_ForearmPhysicalBoneIndex = -1;
        [SerializeField] int m_HandPhysicalBoneIndex = -1;
        [SerializeField] float m_UpperArmLength;
        [SerializeField] float m_ForearmLength;

        public CharacterAnimationArmChainPayload(
            CharacterAnimationRigDefinition rig,
            CharacterAnimationArmChainDefinition source)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_ClaviclePhysicalBoneIndex = source.HasClavicle
                ? rig.RequirePhysicalBoneIndex(source.ClavicleBoneId)
                : -1;
            m_UpperArmPhysicalBoneIndex = rig.RequirePhysicalBoneIndex(source.UpperArmBoneId);
            m_ForearmPhysicalBoneIndex = rig.RequirePhysicalBoneIndex(source.ForearmBoneId);
            m_HandPhysicalBoneIndex = rig.RequirePhysicalBoneIndex(source.HandBoneId);
            m_UpperArmLength = rig.PhysicalBones[m_ForearmPhysicalBoneIndex].ReferenceLocalPosition.magnitude;
            m_ForearmLength = rig.PhysicalBones[m_HandPhysicalBoneIndex].ReferenceLocalPosition.magnitude;
        }

        public int ClaviclePhysicalBoneIndex => m_ClaviclePhysicalBoneIndex;
        public int UpperArmPhysicalBoneIndex => m_UpperArmPhysicalBoneIndex;
        public int ForearmPhysicalBoneIndex => m_ForearmPhysicalBoneIndex;
        public int HandPhysicalBoneIndex => m_HandPhysicalBoneIndex;
        public float UpperArmLength => m_UpperArmLength;
        public float ForearmLength => m_ForearmLength;
        public bool HasClavicle => ClaviclePhysicalBoneIndex >= 0;

        public bool IsValid(int physicalBoneCount)
        {
            if (ClaviclePhysicalBoneIndex < -1 || ClaviclePhysicalBoneIndex >= physicalBoneCount ||
                UpperArmPhysicalBoneIndex < 0 || UpperArmPhysicalBoneIndex >= physicalBoneCount ||
                ForearmPhysicalBoneIndex < 0 || ForearmPhysicalBoneIndex >= physicalBoneCount ||
                HandPhysicalBoneIndex < 0 || HandPhysicalBoneIndex >= physicalBoneCount ||
                !IsPositiveFinite(UpperArmLength) || !IsPositiveFinite(ForearmLength))
                return false;
            var indices = new HashSet<int>
            {
                UpperArmPhysicalBoneIndex,
                ForearmPhysicalBoneIndex,
                HandPhysicalBoneIndex
            };
            return indices.Count == 3 && (!HasClavicle || indices.Add(ClaviclePhysicalBoneIndex));
        }

        static bool IsPositiveFinite(float value) => float.IsFinite(value) && value > 0.0001f;
    }

    [Serializable]
    public sealed class CharacterAnimationLegChainPayload
    {
        [SerializeField] int m_HipPhysicalBoneIndex = -1;
        [SerializeField] int m_KneePhysicalBoneIndex = -1;
        [SerializeField] int m_AnklePhysicalBoneIndex = -1;
        [SerializeField] int m_ToePhysicalBoneIndex = -1;
        [SerializeField] float m_UpperLegLength;
        [SerializeField] float m_LowerLegLength;
        [SerializeField] float m_FootLength;

        public CharacterAnimationLegChainPayload(
            CharacterAnimationRigDefinition rig,
            CharacterAnimationLegChainDefinition source)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_HipPhysicalBoneIndex = rig.RequirePhysicalBoneIndex(source.HipBoneId);
            m_KneePhysicalBoneIndex = rig.RequirePhysicalBoneIndex(source.KneeBoneId);
            m_AnklePhysicalBoneIndex = rig.RequirePhysicalBoneIndex(source.AnkleBoneId);
            m_ToePhysicalBoneIndex = rig.RequirePhysicalBoneIndex(source.ToeBoneId);
            m_UpperLegLength = rig.PhysicalBones[m_KneePhysicalBoneIndex].ReferenceLocalPosition.magnitude;
            m_LowerLegLength = rig.PhysicalBones[m_AnklePhysicalBoneIndex].ReferenceLocalPosition.magnitude;
            m_FootLength = rig.PhysicalBones[m_ToePhysicalBoneIndex].ReferenceLocalPosition.magnitude;
        }

        public int HipPhysicalBoneIndex => m_HipPhysicalBoneIndex;
        public int KneePhysicalBoneIndex => m_KneePhysicalBoneIndex;
        public int AnklePhysicalBoneIndex => m_AnklePhysicalBoneIndex;
        public int ToePhysicalBoneIndex => m_ToePhysicalBoneIndex;
        public float UpperLegLength => m_UpperLegLength;
        public float LowerLegLength => m_LowerLegLength;
        public float FootLength => m_FootLength;
        public float LegLength => UpperLegLength + LowerLegLength;

        public bool IsValid(int physicalBoneCount, int pelvisPhysicalBoneIndex) =>
            HipPhysicalBoneIndex >= 0 && HipPhysicalBoneIndex < physicalBoneCount &&
            KneePhysicalBoneIndex >= 0 && KneePhysicalBoneIndex < physicalBoneCount &&
            AnklePhysicalBoneIndex >= 0 && AnklePhysicalBoneIndex < physicalBoneCount &&
            ToePhysicalBoneIndex >= 0 && ToePhysicalBoneIndex < physicalBoneCount &&
            HipPhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            KneePhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            AnklePhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            ToePhysicalBoneIndex != pelvisPhysicalBoneIndex &&
            AreDistinct() &&
            IsPositiveFinite(UpperLegLength) &&
            IsPositiveFinite(LowerLegLength) &&
            IsPositiveFinite(FootLength);

        bool AreDistinct() =>
            HipPhysicalBoneIndex != KneePhysicalBoneIndex &&
            HipPhysicalBoneIndex != AnklePhysicalBoneIndex &&
            HipPhysicalBoneIndex != ToePhysicalBoneIndex &&
            KneePhysicalBoneIndex != AnklePhysicalBoneIndex &&
            KneePhysicalBoneIndex != ToePhysicalBoneIndex &&
            AnklePhysicalBoneIndex != ToePhysicalBoneIndex;

        static bool IsPositiveFinite(float value) => float.IsFinite(value) && value > 0.0001f;
    }

    [Serializable]
    public sealed class CharacterAnimationRigPayload
    {
        public const string SchemaVersion = "character-animation-rig-payload/v4";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] CharacterAnimationRootBonePolicy m_RootBonePolicy;
        [SerializeField] CharacterAnimationScalePolicy m_ScalePolicy;
        [SerializeField] int m_RootPhysicalBoneIndex = -1;
        [SerializeField] int m_SolverRootPhysicalBoneIndex = -1;
        [SerializeField] int m_PelvisPhysicalBoneIndex = -1;
        [SerializeField] int[] m_OrderedSpinePhysicalBoneIndices = Array.Empty<int>();
        [SerializeField] CharacterAnimationArmChainPayload m_LeftArm;
        [SerializeField] CharacterAnimationArmChainPayload m_RightArm;
        [SerializeField] CharacterAnimationLegChainPayload m_LeftLeg;
        [SerializeField] CharacterAnimationLegChainPayload m_RightLeg;
        [SerializeField] int m_HeadPhysicalBoneIndex = -1;
        [SerializeField] CharacterAnimationPhysicalBonePayload[] m_PhysicalBones = Array.Empty<CharacterAnimationPhysicalBonePayload>();
        [SerializeField] CharacterAnimationVirtualBonePayload[] m_VirtualBones = Array.Empty<CharacterAnimationVirtualBonePayload>();

        public CharacterAnimationRigPayload(CharacterAnimationRigDefinition source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            source.RequireValid();
            m_Schema = SchemaVersion;
            m_RigId = source.RigId;
            m_RigRevision = source.Revision;
            m_RootBonePolicy = source.RootBonePolicy;
            m_ScalePolicy = source.ScalePolicy;
            m_RootPhysicalBoneIndex = source.RequireRootBoneIndex();
            m_SolverRootPhysicalBoneIndex = source.RequirePhysicalBoneIndex(source.SolverRootBoneId);
            m_PelvisPhysicalBoneIndex = source.RequirePhysicalBoneIndex(source.PelvisBoneId);
            m_OrderedSpinePhysicalBoneIndices = new int[source.SpineBoneCount];
            for (int i = 0; i < m_OrderedSpinePhysicalBoneIndices.Length; i++)
                m_OrderedSpinePhysicalBoneIndices[i] = source.RequirePhysicalBoneIndex(source.GetSpineBoneId(i));
            m_LeftArm = new CharacterAnimationArmChainPayload(source, source.LeftArm);
            m_RightArm = new CharacterAnimationArmChainPayload(source, source.RightArm);
            m_LeftLeg = new CharacterAnimationLegChainPayload(source, source.LeftLeg);
            m_RightLeg = new CharacterAnimationLegChainPayload(source, source.RightLeg);
            m_HeadPhysicalBoneIndex = source.HeadBoneId.IsValid
                ? source.RequirePhysicalBoneIndex(source.HeadBoneId)
                : -1;
            m_PhysicalBones = new CharacterAnimationPhysicalBonePayload[source.PhysicalBoneCount];
            for (int i = 0; i < m_PhysicalBones.Length; i++)
                m_PhysicalBones[i] = new CharacterAnimationPhysicalBonePayload(source.PhysicalBones[i]);
            BuildVirtualPayload(source);
            RequireValid();
        }

        public string Schema => m_Schema ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public CharacterAnimationRootBonePolicy RootBonePolicy => m_RootBonePolicy;
        public CharacterAnimationScalePolicy ScalePolicy => m_ScalePolicy;
        public int RootPhysicalBoneIndex => m_RootPhysicalBoneIndex;
        public int SolverRootPhysicalBoneIndex => m_SolverRootPhysicalBoneIndex;
        public int PelvisPhysicalBoneIndex => m_PelvisPhysicalBoneIndex;
        public IReadOnlyList<int> OrderedSpinePhysicalBoneIndices => m_OrderedSpinePhysicalBoneIndices ?? Array.Empty<int>();
        public CharacterAnimationArmChainPayload LeftArm => m_LeftArm;
        public CharacterAnimationArmChainPayload RightArm => m_RightArm;
        public CharacterAnimationLegChainPayload LeftLeg => m_LeftLeg;
        public CharacterAnimationLegChainPayload RightLeg => m_RightLeg;
        public int HeadPhysicalBoneIndex => m_HeadPhysicalBoneIndex;
        public bool HasHead => HeadPhysicalBoneIndex >= 0;
        public IReadOnlyList<CharacterAnimationPhysicalBonePayload> PhysicalBones => m_PhysicalBones ?? Array.Empty<CharacterAnimationPhysicalBonePayload>();
        public IReadOnlyList<CharacterAnimationVirtualBonePayload> VirtualBones => m_VirtualBones ?? Array.Empty<CharacterAnimationVirtualBonePayload>();
        public int PhysicalBoneCount => PhysicalBones.Count;
        public int VirtualBoneCount => VirtualBones.Count;
        public int PoseBoneCount => checked(PhysicalBoneCount + VirtualBoneCount);
        public CharacterPoseBoneCounts BoneCounts => new CharacterPoseBoneCounts(PhysicalBoneCount, VirtualBoneCount);

        public AnimationBoneId GetPoseBoneId(int poseBoneIndex)
        {
            if (poseBoneIndex < 0 || poseBoneIndex >= PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIndex));
            return poseBoneIndex < PhysicalBoneCount
                ? PhysicalBones[poseBoneIndex].BoneId
                : VirtualBones[poseBoneIndex - PhysicalBoneCount].VirtualBoneId;
        }

        public CharacterPoseBoneKind GetPoseBoneKind(int poseBoneIndex)
        {
            if (poseBoneIndex < 0 || poseBoneIndex >= PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIndex));
            return poseBoneIndex < PhysicalBoneCount
                ? CharacterPoseBoneKind.Physical
                : CharacterPoseBoneKind.Virtual;
        }

        public int GetPoseParentIndex(int poseBoneIndex)
        {
            if (poseBoneIndex < 0 || poseBoneIndex >= PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIndex));
            return poseBoneIndex < PhysicalBoneCount
                ? PhysicalBones[poseBoneIndex].ParentPhysicalIndex
                : VirtualBones[poseBoneIndex - PhysicalBoneCount].SourcePhysicalBoneIndex;
        }

        public AnimationLocalBonePose GetReferenceLocalPose(int poseBoneIndex)
        {
            if (poseBoneIndex < 0 || poseBoneIndex >= PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIndex));
            if (poseBoneIndex < PhysicalBoneCount)
            {
                CharacterAnimationPhysicalBonePayload bone = PhysicalBones[poseBoneIndex];
                return new AnimationLocalBonePose(
                    bone.ReferenceLocalPosition,
                    bone.ReferenceLocalRotation,
                    bone.ReferenceLocalScale);
            }
            CharacterAnimationVirtualBonePayload virtualBone = VirtualBones[poseBoneIndex - PhysicalBoneCount];
            return new AnimationLocalBonePose(
                virtualBone.ReferenceLocalPosition,
                virtualBone.ReferenceLocalRotation,
                Vector3.one);
        }

        public int RequirePhysicalBoneIndex(AnimationBoneId boneId)
        {
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                if (PhysicalBones[i].BoneId.Equals(boneId))
                    return i;
            }
            throw new InvalidOperationException($"Compiled Rig '{RigId}' does not contain Physical Bone '{boneId}'.");
        }

        public int RequirePoseBoneIndex(AnimationBoneId boneId)
        {
            for (int i = 0; i < PoseBoneCount; i++)
            {
                if (GetPoseBoneId(i).Equals(boneId))
                    return i;
            }
            throw new InvalidOperationException($"Compiled Rig '{RigId}' does not contain Pose Bone '{boneId}'.");
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(RigId) ||
                string.IsNullOrEmpty(RigRevision) ||
                PhysicalBoneCount == 0 ||
                !Enum.IsDefined(typeof(CharacterAnimationRootBonePolicy), RootBonePolicy) ||
                !Enum.IsDefined(typeof(CharacterAnimationScalePolicy), ScalePolicy) ||
                RootPhysicalBoneIndex < 0 ||
                RootPhysicalBoneIndex >= PhysicalBoneCount ||
                SolverRootPhysicalBoneIndex < 0 ||
                SolverRootPhysicalBoneIndex >= PhysicalBoneCount ||
                PelvisPhysicalBoneIndex < 0 ||
                PelvisPhysicalBoneIndex >= PhysicalBoneCount ||
                OrderedSpinePhysicalBoneIndices.Count == 0 ||
                LeftArm == null ||
                RightArm == null ||
                !LeftArm.IsValid(PhysicalBoneCount) ||
                !RightArm.IsValid(PhysicalBoneCount) ||
                LeftLeg == null ||
                RightLeg == null ||
                !LeftLeg.IsValid(PhysicalBoneCount, PelvisPhysicalBoneIndex) ||
                !RightLeg.IsValid(PhysicalBoneCount, PelvisPhysicalBoneIndex) ||
                HeadPhysicalBoneIndex < -1 ||
                HeadPhysicalBoneIndex >= PhysicalBoneCount)
            {
                throw new InvalidOperationException("Compiled Character Animation Rig payload is invalid.");
            }

            var ids = new HashSet<AnimationBoneId>();
            int rootCount = 0;
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                CharacterAnimationPhysicalBonePayload bone = PhysicalBones[i];
                if (bone == null ||
                    !bone.BoneId.IsValid ||
                    !ids.Add(bone.BoneId) ||
                    bone.ParentPhysicalIndex < -1 ||
                    bone.ParentPhysicalIndex >= i)
                {
                    throw new InvalidOperationException($"Compiled Character Animation Rig Physical Bone #{i} is invalid.");
                }
                if (bone.ParentPhysicalIndex == -1)
                {
                    rootCount++;
                    if (i != RootPhysicalBoneIndex)
                        throw new InvalidOperationException("Compiled Character Animation Rig root index is inconsistent.");
                }
            }
            if (rootCount != 1)
                throw new InvalidOperationException("Compiled Character Animation Rig requires exactly one Physical root Bone.");
            RequireFullBipedSemantics();
            RequireSpineChain();
            RequireSolverRoot();
            RequireArmChain("Left", LeftArm);
            RequireArmChain("Right", RightArm);
            RequireLegChain("Left", LeftLeg);
            RequireLegChain("Right", RightLeg);
            RequireHead();
            Vector3[] referencePositions = BuildReferenceComponentPositions();
            RequireBendPlane("Left Arm", LeftArm.UpperArmPhysicalBoneIndex, LeftArm.ForearmPhysicalBoneIndex, LeftArm.HandPhysicalBoneIndex, referencePositions);
            RequireBendPlane("Right Arm", RightArm.UpperArmPhysicalBoneIndex, RightArm.ForearmPhysicalBoneIndex, RightArm.HandPhysicalBoneIndex, referencePositions);
            RequireBendPlane("Left Leg", LeftLeg.HipPhysicalBoneIndex, LeftLeg.KneePhysicalBoneIndex, LeftLeg.AnklePhysicalBoneIndex, referencePositions);
            RequireBendPlane("Right Leg", RightLeg.HipPhysicalBoneIndex, RightLeg.KneePhysicalBoneIndex, RightLeg.AnklePhysicalBoneIndex, referencePositions);

            for (int i = 0; i < VirtualBoneCount; i++)
            {
                CharacterAnimationVirtualBonePayload bone = VirtualBones[i];
                if (bone == null ||
                    !bone.VirtualBoneId.IsValid ||
                    !ids.Add(bone.VirtualBoneId) ||
                    string.IsNullOrWhiteSpace(bone.DisplayName) ||
                    bone.SourcePhysicalBoneIndex < 0 ||
                    bone.SourcePhysicalBoneIndex >= PhysicalBoneCount ||
                    bone.TargetPhysicalBoneIndex < 0 ||
                    bone.TargetPhysicalBoneIndex >= PhysicalBoneCount ||
                    bone.SourcePhysicalBoneIndex == bone.TargetPhysicalBoneIndex ||
                    bone.PoseBoneIndex != PhysicalBoneCount + i ||
                    !new AnimationLocalBonePose(
                        bone.ReferenceLocalPosition,
                        bone.ReferenceLocalRotation,
                        Vector3.one).IsValid)
                {
                    throw new InvalidOperationException($"Compiled Character Animation Rig Virtual Bone #{i} is invalid.");
                }
            }
        }

        void RequireFullBipedSemantics()
        {
            var indices = new HashSet<int>();
            RequireUniqueSemantic(PelvisPhysicalBoneIndex, indices);
            for (int i = 0; i < OrderedSpinePhysicalBoneIndices.Count; i++)
                RequireUniqueSemantic(OrderedSpinePhysicalBoneIndices[i], indices);
            AddArmSemantics(LeftArm, indices);
            AddArmSemantics(RightArm, indices);
            AddLegSemantics(LeftLeg, indices);
            AddLegSemantics(RightLeg, indices);
            if (HasHead)
                RequireUniqueSemantic(HeadPhysicalBoneIndex, indices);
        }

        void AddArmSemantics(CharacterAnimationArmChainPayload arm, HashSet<int> indices)
        {
            if (arm.HasClavicle)
                RequireUniqueSemantic(arm.ClaviclePhysicalBoneIndex, indices);
            RequireUniqueSemantic(arm.UpperArmPhysicalBoneIndex, indices);
            RequireUniqueSemantic(arm.ForearmPhysicalBoneIndex, indices);
            RequireUniqueSemantic(arm.HandPhysicalBoneIndex, indices);
        }

        void AddLegSemantics(CharacterAnimationLegChainPayload leg, HashSet<int> indices)
        {
            RequireUniqueSemantic(leg.HipPhysicalBoneIndex, indices);
            RequireUniqueSemantic(leg.KneePhysicalBoneIndex, indices);
            RequireUniqueSemantic(leg.AnklePhysicalBoneIndex, indices);
            RequireUniqueSemantic(leg.ToePhysicalBoneIndex, indices);
        }

        static void RequireUniqueSemantic(int index, HashSet<int> indices)
        {
            if (!indices.Add(index))
                throw new InvalidOperationException("Compiled Character Animation Rig contains duplicate full-biped semantic Bones.");
        }

        void RequireSpineChain()
        {
            int parent = PelvisPhysicalBoneIndex;
            for (int i = 0; i < OrderedSpinePhysicalBoneIndices.Count; i++)
            {
                int current = OrderedSpinePhysicalBoneIndices[i];
                RequireParent("Spine", i.ToString(), current, parent);
                parent = current;
            }
        }

        void RequireSolverRoot()
        {
            if (SolverRootPhysicalBoneIndex == PelvisPhysicalBoneIndex)
                return;
            for (int i = 0; i < OrderedSpinePhysicalBoneIndices.Count; i++)
            {
                if (OrderedSpinePhysicalBoneIndices[i] == SolverRootPhysicalBoneIndex)
                    return;
            }
            throw new InvalidOperationException("Compiled Character Animation Rig Solver Root is not the Pelvis or an ordered Spine Bone.");
        }

        void RequireArmChain(string side, CharacterAnimationArmChainPayload arm)
        {
            int upperParent = OrderedSpinePhysicalBoneIndices[OrderedSpinePhysicalBoneIndices.Count - 1];
            if (arm.HasClavicle)
            {
                RequireParent(side, "Clavicle", arm.ClaviclePhysicalBoneIndex, upperParent);
                upperParent = arm.ClaviclePhysicalBoneIndex;
            }
            RequireParent(side, "Upper Arm", arm.UpperArmPhysicalBoneIndex, upperParent);
            RequireParent(side, "Forearm", arm.ForearmPhysicalBoneIndex, arm.UpperArmPhysicalBoneIndex);
            RequireParent(side, "Hand", arm.HandPhysicalBoneIndex, arm.ForearmPhysicalBoneIndex);
        }

        void RequireLegChain(string side, CharacterAnimationLegChainPayload leg)
        {
            RequireParent(side, "Hip", leg.HipPhysicalBoneIndex, PelvisPhysicalBoneIndex);
            RequireParent(side, "Knee", leg.KneePhysicalBoneIndex, leg.HipPhysicalBoneIndex);
            RequireParent(side, "Ankle", leg.AnklePhysicalBoneIndex, leg.KneePhysicalBoneIndex);
            RequireParent(side, "Toe", leg.ToePhysicalBoneIndex, leg.AnklePhysicalBoneIndex);
        }

        void RequireParent(string side, string slot, int childIndex, int parentIndex)
        {
            if (PhysicalBones[childIndex].ParentPhysicalIndex != parentIndex)
            {
                throw new InvalidOperationException(
                    $"Compiled Character Animation Rig {side} {slot} parent chain is invalid.");
            }
        }

        void RequireHead()
        {
            if (!HasHead)
                return;
            int spineEnd = OrderedSpinePhysicalBoneIndices[OrderedSpinePhysicalBoneIndices.Count - 1];
            int current = HeadPhysicalBoneIndex;
            while (current >= 0 && current != spineEnd)
                current = PhysicalBones[current].ParentPhysicalIndex;
            if (current != spineEnd)
                throw new InvalidOperationException("Compiled Character Animation Rig Head is not a descendant of the ordered Spine.");
        }

        Vector3[] BuildReferenceComponentPositions()
        {
            var positions = new Vector3[PhysicalBoneCount];
            var matrices = new Matrix4x4[PhysicalBoneCount];
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                CharacterAnimationPhysicalBonePayload bone = PhysicalBones[i];
                Matrix4x4 local = Matrix4x4.TRS(
                    bone.ReferenceLocalPosition,
                    bone.ReferenceLocalRotation,
                    bone.ReferenceLocalScale);
                matrices[i] = bone.ParentPhysicalIndex >= 0 ? matrices[bone.ParentPhysicalIndex] * local : local;
                positions[i] = matrices[i].GetColumn(3);
            }
            return positions;
        }

        static void RequireBendPlane(string limb, int upper, int middle, int end, Vector3[] positions)
        {
            Vector3 normal = Vector3.Cross(positions[middle] - positions[upper], positions[end] - positions[middle]);
            float area = normal.sqrMagnitude;
            if (float.IsFinite(area) && area > 0.00000001f)
                return;
            throw new InvalidOperationException($"Compiled Character Animation Rig {limb} reference bend plane is degenerate.");
        }

        void BuildVirtualPayload(CharacterAnimationRigDefinition source)
        {
            var counts = new CharacterPoseBoneCounts(source.PhysicalBoneCount, source.VirtualBoneCount);
            var physicalLocalPoses = new NativeArray<AnimationLocalBonePose>(counts.PhysicalBoneCount, Allocator.Temp);
            var physicalParentIndices = new NativeArray<int>(counts.PhysicalBoneCount, Allocator.Temp);
            var descriptors = new NativeArray<CharacterVirtualBoneDescriptor>(counts.VirtualBoneCount, Allocator.Temp);
            var componentScratch = new NativeArray<CharacterComponentBonePose>(counts.PhysicalBoneCount, Allocator.Temp);
            var outputPose = new NativeArray<AnimationLocalBonePose>(counts.PoseBoneCount, Allocator.Temp);
            for (int i = 0; i < counts.PhysicalBoneCount; i++)
            {
                CharacterAnimationPhysicalBoneDefinition bone = source.PhysicalBones[i];
                physicalLocalPoses[i] = new AnimationLocalBonePose(
                    bone.ReferenceLocalPosition,
                    bone.ReferenceLocalRotation,
                    bone.ReferenceLocalScale);
                physicalParentIndices[i] = bone.ParentIndex;
            }
            for (int i = 0; i < counts.VirtualBoneCount; i++)
            {
                CharacterAnimationVirtualBoneDefinition bone = source.VirtualBones[i];
                descriptors[i] = new CharacterVirtualBoneDescriptor(
                    new CharacterPoseBoneRuntimeId(bone.VirtualBoneId),
                    source.RequirePhysicalBoneIndex(bone.SourcePhysicalBoneId),
                    source.RequirePhysicalBoneIndex(bone.TargetPhysicalBoneId),
                    counts.PhysicalBoneCount + i);
            }
            CharacterVirtualBonePoseResult result = CharacterVirtualBonePoseDerivation.Derive(
                counts,
                physicalLocalPoses,
                physicalParentIndices,
                descriptors,
                componentScratch,
                outputPose);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Animation Rig '{source.name}' reference Virtual Bone derivation failed: {result.Failure}.");

            m_VirtualBones = new CharacterAnimationVirtualBonePayload[counts.VirtualBoneCount];
            for (int i = 0; i < counts.VirtualBoneCount; i++)
            {
                CharacterVirtualBoneDescriptor descriptor = descriptors[i];
                m_VirtualBones[i] = new CharacterAnimationVirtualBonePayload(
                    source.VirtualBones[i],
                    descriptor.SourcePhysicalBoneIndex,
                    descriptor.TargetPhysicalBoneIndex,
                    descriptor.PoseBoneIndex,
                    outputPose[descriptor.PoseBoneIndex]);
            }
            outputPose.Dispose();
            componentScratch.Dispose();
            descriptors.Dispose();
            physicalParentIndices.Dispose();
            physicalLocalPoses.Dispose();
        }
    }
}
