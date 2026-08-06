using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterAnimationRigValidationCode : byte
    {
        ContractInvalid = 1,
        PhysicalBoneInvalid = 2,
        MultipleRoots = 3,
        SemanticBoneInvalid = 4,
        VirtualBoneInvalid = 5,
        SemanticBoneDuplicate = 6,
        LegChainInvalid = 7,
        ArmChainInvalid = 8,
        SpineChainInvalid = 9,
        BendPlaneInvalid = 10
    }

    public sealed class CharacterAnimationRigValidationException : InvalidOperationException
    {
        public CharacterAnimationRigValidationException(
            CharacterAnimationRigValidationCode code,
            string message,
            int boneIndex = -1)
            : base(message)
        {
            Code = code;
            BoneIndex = boneIndex;
        }

        public CharacterAnimationRigValidationCode Code { get; }
        public int BoneIndex { get; }
    }

    public enum CharacterAnimationRootBonePolicy : byte
    {
        ExcludeSourceRoot = 1,
        CaptureSourceRoot = 2
    }

    public enum CharacterAnimationScalePolicy : byte
    {
        PreserveReferenceScale = 1,
        BlendLocalScale = 2
    }

    [Serializable]
    public sealed class CharacterAnimationPhysicalBoneDefinition
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] int m_ParentIndex = -1;
        [SerializeField] Vector3 m_ReferenceLocalPosition;
        [SerializeField] Quaternion m_ReferenceLocalRotation = Quaternion.identity;
        [SerializeField] Vector3 m_ReferenceLocalScale = Vector3.one;

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public int ParentIndex => m_ParentIndex;
        public Vector3 ReferenceLocalPosition => m_ReferenceLocalPosition;
        public Quaternion ReferenceLocalRotation => m_ReferenceLocalRotation;
        public Vector3 ReferenceLocalScale => m_ReferenceLocalScale;

        public CharacterAnimationPhysicalBoneDefinition() { }

        public CharacterAnimationPhysicalBoneDefinition(
            AnimationBoneId boneId,
            int parentIndex,
            Vector3 referenceLocalPosition,
            Quaternion referenceLocalRotation,
            Vector3 referenceLocalScale)
        {
            if (!boneId.IsValid)
                throw new ArgumentException("Animation Bone identity is invalid.", nameof(boneId));
            if (parentIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(parentIndex));
            if (!IsFinite(referenceLocalPosition) || !IsFinite(referenceLocalRotation) || !IsFinite(referenceLocalScale))
                throw new ArgumentException("Animation reference transform is non-finite.");
            m_BoneId = boneId.Value;
            m_ParentIndex = parentIndex;
            m_ReferenceLocalPosition = referenceLocalPosition;
            m_ReferenceLocalRotation = referenceLocalRotation.normalized;
            m_ReferenceLocalScale = referenceLocalScale;
        }

        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        static bool IsFinite(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);
    }

    [Serializable]
    public sealed class CharacterAnimationVirtualBoneDefinition
    {
        [SerializeField] string m_VirtualBoneId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] string m_SourcePhysicalBoneId = string.Empty;
        [SerializeField] string m_TargetPhysicalBoneId = string.Empty;

        public AnimationBoneId VirtualBoneId => string.IsNullOrWhiteSpace(m_VirtualBoneId) ? default : new AnimationBoneId(m_VirtualBoneId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public AnimationBoneId SourcePhysicalBoneId => string.IsNullOrWhiteSpace(m_SourcePhysicalBoneId) ? default : new AnimationBoneId(m_SourcePhysicalBoneId);
        public AnimationBoneId TargetPhysicalBoneId => string.IsNullOrWhiteSpace(m_TargetPhysicalBoneId) ? default : new AnimationBoneId(m_TargetPhysicalBoneId);

        public CharacterAnimationVirtualBoneDefinition() { }

        public CharacterAnimationVirtualBoneDefinition(
            AnimationBoneId virtualBoneId,
            string displayName,
            AnimationBoneId sourcePhysicalBoneId,
            AnimationBoneId targetPhysicalBoneId)
        {
            if (!virtualBoneId.IsValid)
                throw new ArgumentException("Virtual Bone identity is invalid.", nameof(virtualBoneId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Virtual Bone display name is invalid.", nameof(displayName));
            if (!sourcePhysicalBoneId.IsValid)
                throw new ArgumentException("Virtual Bone Source identity is invalid.", nameof(sourcePhysicalBoneId));
            if (!targetPhysicalBoneId.IsValid)
                throw new ArgumentException("Virtual Bone Target identity is invalid.", nameof(targetPhysicalBoneId));
            if (sourcePhysicalBoneId.Equals(targetPhysicalBoneId))
                throw new ArgumentException("Virtual Bone Source and Target must be different.");
            m_VirtualBoneId = virtualBoneId.Value;
            m_DisplayName = displayName.Trim();
            m_SourcePhysicalBoneId = sourcePhysicalBoneId.Value;
            m_TargetPhysicalBoneId = targetPhysicalBoneId.Value;
        }
    }

    [Serializable]
    public sealed class CharacterAnimationArmChainDefinition
    {
        [SerializeField] string m_ClavicleBoneId = string.Empty;
        [SerializeField] string m_UpperArmBoneId = string.Empty;
        [SerializeField] string m_ForearmBoneId = string.Empty;
        [SerializeField] string m_HandBoneId = string.Empty;

        public AnimationBoneId ClavicleBoneId => ToOptionalBoneId(m_ClavicleBoneId);
        public AnimationBoneId UpperArmBoneId => ToBoneId(m_UpperArmBoneId);
        public AnimationBoneId ForearmBoneId => ToBoneId(m_ForearmBoneId);
        public AnimationBoneId HandBoneId => ToBoneId(m_HandBoneId);
        public bool HasClavicle => ClavicleBoneId.IsValid;

        public CharacterAnimationArmChainDefinition() { }

        public CharacterAnimationArmChainDefinition(
            AnimationBoneId clavicleBoneId,
            AnimationBoneId upperArmBoneId,
            AnimationBoneId forearmBoneId,
            AnimationBoneId handBoneId)
        {
            m_ClavicleBoneId = clavicleBoneId.IsValid ? clavicleBoneId.Value : string.Empty;
            m_UpperArmBoneId = RequireBoneId(upperArmBoneId, nameof(upperArmBoneId));
            m_ForearmBoneId = RequireBoneId(forearmBoneId, nameof(forearmBoneId));
            m_HandBoneId = RequireBoneId(handBoneId, nameof(handBoneId));
        }

        public IReadOnlyList<AnimationBoneId> GetRequiredBoneIds() =>
            new[] { UpperArmBoneId, ForearmBoneId, HandBoneId };

        static AnimationBoneId ToOptionalBoneId(string value) =>
            string.IsNullOrWhiteSpace(value) ? default : new AnimationBoneId(value);

        static AnimationBoneId ToBoneId(string value) =>
            string.IsNullOrWhiteSpace(value) ? default : new AnimationBoneId(value);

        static string RequireBoneId(AnimationBoneId boneId, string parameterName) =>
            boneId.IsValid
                ? boneId.Value
                : throw new ArgumentException("Arm chain Bone identity is invalid.", parameterName);
    }

    [Serializable]
    public sealed class CharacterAnimationLegChainDefinition
    {
        [SerializeField] string m_HipBoneId = string.Empty;
        [SerializeField] string m_KneeBoneId = string.Empty;
        [SerializeField] string m_AnkleBoneId = string.Empty;
        [SerializeField] string m_ToeBoneId = string.Empty;

        public AnimationBoneId HipBoneId => ToBoneId(m_HipBoneId);
        public AnimationBoneId KneeBoneId => ToBoneId(m_KneeBoneId);
        public AnimationBoneId AnkleBoneId => ToBoneId(m_AnkleBoneId);
        public AnimationBoneId ToeBoneId => ToBoneId(m_ToeBoneId);

        public CharacterAnimationLegChainDefinition() { }

        public CharacterAnimationLegChainDefinition(
            AnimationBoneId hipBoneId,
            AnimationBoneId kneeBoneId,
            AnimationBoneId ankleBoneId,
            AnimationBoneId toeBoneId)
        {
            m_HipBoneId = RequireBoneId(hipBoneId, nameof(hipBoneId));
            m_KneeBoneId = RequireBoneId(kneeBoneId, nameof(kneeBoneId));
            m_AnkleBoneId = RequireBoneId(ankleBoneId, nameof(ankleBoneId));
            m_ToeBoneId = RequireBoneId(toeBoneId, nameof(toeBoneId));
        }

        public IReadOnlyList<AnimationBoneId> GetBoneIds() =>
            new[] { HipBoneId, KneeBoneId, AnkleBoneId, ToeBoneId };

        static AnimationBoneId ToBoneId(string value) =>
            string.IsNullOrWhiteSpace(value) ? default : new AnimationBoneId(value);

        static string RequireBoneId(AnimationBoneId boneId, string parameterName) =>
            boneId.IsValid
                ? boneId.Value
                : throw new ArgumentException("Leg chain Bone identity is invalid.", parameterName);
    }

    [CreateAssetMenu(fileName = "CharacterAnimationRigDefinition", menuName = "3C/Character/Animation Rig Definition")]
    public sealed class CharacterAnimationRigDefinition : ScriptableObject
    {
        public const string SchemaVersion = "character-animation-rig/v4";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterAnimationPhysicalBoneDefinition[] m_PhysicalBones = Array.Empty<CharacterAnimationPhysicalBoneDefinition>();
        [SerializeField] CharacterAnimationVirtualBoneDefinition[] m_VirtualBones = Array.Empty<CharacterAnimationVirtualBoneDefinition>();
        [SerializeField] CharacterAnimationRootBonePolicy m_RootBonePolicy = CharacterAnimationRootBonePolicy.ExcludeSourceRoot;
        [SerializeField] CharacterAnimationScalePolicy m_ScalePolicy = CharacterAnimationScalePolicy.PreserveReferenceScale;
        [SerializeField] string m_SolverRootBoneId = string.Empty;
        [SerializeField] string m_PelvisBoneId = string.Empty;
        [SerializeField] string[] m_OrderedSpineBoneIds = Array.Empty<string>();
        [SerializeField] CharacterAnimationArmChainDefinition m_LeftArm = new CharacterAnimationArmChainDefinition();
        [SerializeField] CharacterAnimationArmChainDefinition m_RightArm = new CharacterAnimationArmChainDefinition();
        [SerializeField] CharacterAnimationLegChainDefinition m_LeftLeg = new CharacterAnimationLegChainDefinition();
        [SerializeField] CharacterAnimationLegChainDefinition m_RightLeg = new CharacterAnimationLegChainDefinition();
        [SerializeField] string m_HeadBoneId = string.Empty;

        public string Schema => m_Schema ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public IReadOnlyList<CharacterAnimationPhysicalBoneDefinition> PhysicalBones => m_PhysicalBones ?? Array.Empty<CharacterAnimationPhysicalBoneDefinition>();
        public IReadOnlyList<CharacterAnimationVirtualBoneDefinition> VirtualBones => m_VirtualBones ?? Array.Empty<CharacterAnimationVirtualBoneDefinition>();
        public int PhysicalBoneCount => PhysicalBones.Count;
        public int VirtualBoneCount => VirtualBones.Count;
        public int PoseBoneCount => checked(PhysicalBoneCount + VirtualBoneCount);
        public CharacterAnimationRootBonePolicy RootBonePolicy => m_RootBonePolicy;
        public CharacterAnimationScalePolicy ScalePolicy => m_ScalePolicy;
        public AnimationBoneId SolverRootBoneId => ToOptionalBoneId(m_SolverRootBoneId);
        public AnimationBoneId PelvisBoneId => string.IsNullOrWhiteSpace(m_PelvisBoneId) ? default : new AnimationBoneId(m_PelvisBoneId);
        public int SpineBoneCount => m_OrderedSpineBoneIds?.Length ?? 0;
        public CharacterAnimationArmChainDefinition LeftArm => m_LeftArm;
        public CharacterAnimationArmChainDefinition RightArm => m_RightArm;
        public CharacterAnimationLegChainDefinition LeftLeg => m_LeftLeg;
        public CharacterAnimationLegChainDefinition RightLeg => m_RightLeg;
        public AnimationBoneId HeadBoneId => ToOptionalBoneId(m_HeadBoneId);

        public AnimationBoneId GetSpineBoneId(int index)
        {
            if (index < 0 || index >= SpineBoneCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ToOptionalBoneId(m_OrderedSpineBoneIds[index]);
        }

        public void Configure(
            string rigId,
            string revision,
            CharacterAnimationPhysicalBoneDefinition[] physicalBones,
            CharacterAnimationVirtualBoneDefinition[] virtualBones,
            CharacterAnimationRootBonePolicy rootBonePolicy,
            CharacterAnimationScalePolicy scalePolicy,
            AnimationBoneId solverRootBoneId,
            AnimationBoneId pelvisBoneId,
            AnimationBoneId[] orderedSpineBoneIds,
            CharacterAnimationArmChainDefinition leftArm,
            CharacterAnimationArmChainDefinition rightArm,
            CharacterAnimationLegChainDefinition leftLeg,
            CharacterAnimationLegChainDefinition rightLeg,
            AnimationBoneId headBoneId)
        {
            m_Schema = SchemaVersion;
            m_RigId = PoseNodeId.Require(rigId, nameof(rigId));
            m_Revision = PoseNodeId.Require(revision, nameof(revision));
            m_PhysicalBones = physicalBones ?? throw new ArgumentNullException(nameof(physicalBones));
            m_VirtualBones = virtualBones ?? throw new ArgumentNullException(nameof(virtualBones));
            m_RootBonePolicy = rootBonePolicy;
            m_ScalePolicy = scalePolicy;
            m_SolverRootBoneId = RequireBoneId(solverRootBoneId, nameof(solverRootBoneId));
            m_PelvisBoneId = pelvisBoneId.IsValid
                ? pelvisBoneId.Value
                : throw new ArgumentException("Pelvis Bone identity is invalid.", nameof(pelvisBoneId));
            if (orderedSpineBoneIds == null)
                throw new ArgumentNullException(nameof(orderedSpineBoneIds));
            m_OrderedSpineBoneIds = new string[orderedSpineBoneIds.Length];
            for (int i = 0; i < orderedSpineBoneIds.Length; i++)
                m_OrderedSpineBoneIds[i] = RequireBoneId(orderedSpineBoneIds[i], nameof(orderedSpineBoneIds));
            m_LeftArm = leftArm ?? throw new ArgumentNullException(nameof(leftArm));
            m_RightArm = rightArm ?? throw new ArgumentNullException(nameof(rightArm));
            m_LeftLeg = leftLeg ?? throw new ArgumentNullException(nameof(leftLeg));
            m_RightLeg = rightLeg ?? throw new ArgumentNullException(nameof(rightLeg));
            m_HeadBoneId = headBoneId.IsValid ? headBoneId.Value : string.Empty;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(Revision) || PhysicalBoneCount == 0 ||
                !Enum.IsDefined(typeof(CharacterAnimationRootBonePolicy), RootBonePolicy) ||
                !Enum.IsDefined(typeof(CharacterAnimationScalePolicy), ScalePolicy))
            {
                throw new CharacterAnimationRigValidationException(
                    CharacterAnimationRigValidationCode.ContractInvalid,
                    $"Animation Rig '{name}' is incomplete.");
            }
            var ids = new HashSet<AnimationBoneId>();
            int root = -1;
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                CharacterAnimationPhysicalBoneDefinition bone = PhysicalBones[i];
                if (bone == null || !bone.BoneId.IsValid || !ids.Add(bone.BoneId) ||
                    bone.ParentIndex < -1 || bone.ParentIndex >= i)
                {
                    throw new CharacterAnimationRigValidationException(
                        CharacterAnimationRigValidationCode.PhysicalBoneInvalid,
                        $"Animation Rig '{name}' bone #{i} is invalid or not parent-first.",
                        i);
                }
                if (bone.ParentIndex == -1)
                {
                    if (root >= 0)
                    {
                        throw new CharacterAnimationRigValidationException(
                            CharacterAnimationRigValidationCode.MultipleRoots,
                            $"Animation Rig '{name}' contains multiple root bones.",
                            i);
                    }
                    root = i;
                }
            }
            if (root < 0 || !SolverRootBoneId.IsValid || !PelvisBoneId.IsValid || SpineBoneCount == 0 ||
                LeftArm == null || RightArm == null || LeftLeg == null || RightLeg == null)
            {
                throw new CharacterAnimationRigValidationException(
                    CharacterAnimationRigValidationCode.SemanticBoneInvalid,
                    $"Animation Rig '{name}' has incomplete full-biped semantics.");
            }
            RequireSemanticBonesUnique();
            int pelvisIndex = RequirePhysicalBoneIndex(PelvisBoneId);
            int[] spineIndices = RequireSpineValid(pelvisIndex);
            RequireSolverRootValid(pelvisIndex, spineIndices);
            RequireArmChainValid("Left", spineIndices[spineIndices.Length - 1], LeftArm);
            RequireArmChainValid("Right", spineIndices[spineIndices.Length - 1], RightArm);
            RequireLegChainValid("Left", pelvisIndex, LeftLeg);
            RequireLegChainValid("Right", pelvisIndex, RightLeg);
            RequireOptionalHeadValid(spineIndices[spineIndices.Length - 1]);
            Vector3[] referencePositions = BuildReferenceComponentPositions();
            RequireReferenceBendPlane("Left Arm", LeftArm.UpperArmBoneId, LeftArm.ForearmBoneId, LeftArm.HandBoneId, referencePositions);
            RequireReferenceBendPlane("Right Arm", RightArm.UpperArmBoneId, RightArm.ForearmBoneId, RightArm.HandBoneId, referencePositions);
            RequireReferenceBendPlane("Left Leg", LeftLeg.HipBoneId, LeftLeg.KneeBoneId, LeftLeg.AnkleBoneId, referencePositions);
            RequireReferenceBendPlane("Right Leg", RightLeg.HipBoneId, RightLeg.KneeBoneId, RightLeg.AnkleBoneId, referencePositions);
            for (int i = 0; i < VirtualBoneCount; i++)
            {
                CharacterAnimationVirtualBoneDefinition bone = VirtualBones[i];
                if (bone == null ||
                    !bone.VirtualBoneId.IsValid ||
                    !ids.Add(bone.VirtualBoneId) ||
                    string.IsNullOrWhiteSpace(bone.DisplayName) ||
                    !bone.SourcePhysicalBoneId.IsValid ||
                    !bone.TargetPhysicalBoneId.IsValid ||
                    bone.SourcePhysicalBoneId.Equals(bone.TargetPhysicalBoneId) ||
                    FindPhysicalBoneIndex(bone.SourcePhysicalBoneId) < 0 ||
                    FindPhysicalBoneIndex(bone.TargetPhysicalBoneId) < 0)
                {
                    throw new CharacterAnimationRigValidationException(
                        CharacterAnimationRigValidationCode.VirtualBoneInvalid,
                        $"Animation Rig '{name}' Virtual Bone #{i} is invalid.",
                        i);
                }
            }
        }

        public int RequireRootBoneIndex()
        {
            RequireValid();
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                if (PhysicalBones[i].ParentIndex == -1)
                    return i;
            }
            throw new InvalidOperationException($"Animation Rig '{name}' has no root Bone.");
        }

        public int RequirePhysicalBoneIndex(AnimationBoneId boneId)
        {
            int index = FindPhysicalBoneIndex(boneId);
            return index >= 0
                ? index
                : throw new InvalidOperationException($"Animation Rig '{name}' does not contain Physical Bone '{boneId}'.");
        }

        public int RequirePoseBoneIndex(AnimationBoneId boneId)
        {
            int physicalIndex = FindPhysicalBoneIndex(boneId);
            if (physicalIndex >= 0)
                return physicalIndex;
            for (int i = 0; i < VirtualBoneCount; i++)
            {
                if (VirtualBones[i] != null && VirtualBones[i].VirtualBoneId.Equals(boneId))
                    return PhysicalBoneCount + i;
            }
            throw new InvalidOperationException($"Animation Rig '{name}' does not contain Pose Bone '{boneId}'.");
        }

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

        int FindPhysicalBoneIndex(AnimationBoneId boneId)
        {
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                if (PhysicalBones[i] != null && PhysicalBones[i].BoneId.Equals(boneId))
                    return i;
            }
            return -1;
        }

        void RequireSemanticBonesUnique()
        {
            var semanticBones = new HashSet<AnimationBoneId>();
            if (!semanticBones.Add(PelvisBoneId))
                ThrowDuplicateSemantic(PelvisBoneId);
            for (int i = 0; i < SpineBoneCount; i++)
                AddSemanticBone(GetSpineBoneId(i), semanticBones);
            AddArmSemanticBones(LeftArm, semanticBones);
            AddArmSemanticBones(RightArm, semanticBones);
            AddLegSemanticBones(LeftLeg, semanticBones);
            AddLegSemanticBones(RightLeg, semanticBones);
            if (HeadBoneId.IsValid)
                AddSemanticBone(HeadBoneId, semanticBones);
        }

        void AddArmSemanticBones(CharacterAnimationArmChainDefinition arm, HashSet<AnimationBoneId> semanticBones)
        {
            if (arm.HasClavicle)
                AddSemanticBone(arm.ClavicleBoneId, semanticBones);
            IReadOnlyList<AnimationBoneId> ids = arm.GetRequiredBoneIds();
            for (int i = 0; i < ids.Count; i++)
                AddSemanticBone(ids[i], semanticBones);
        }

        void AddLegSemanticBones(
            CharacterAnimationLegChainDefinition leg,
            HashSet<AnimationBoneId> semanticBones)
        {
            IReadOnlyList<AnimationBoneId> ids = leg.GetBoneIds();
            for (int i = 0; i < ids.Count; i++)
            {
                if (!ids[i].IsValid)
                {
                    throw new CharacterAnimationRigValidationException(
                        CharacterAnimationRigValidationCode.SemanticBoneInvalid,
                        $"Animation Rig '{name}' contains an invalid leg Bone identity.");
                }
                if (!semanticBones.Add(ids[i]))
                    ThrowDuplicateSemantic(ids[i]);
            }
        }

        void AddSemanticBone(AnimationBoneId boneId, HashSet<AnimationBoneId> semanticBones)
        {
            if (!boneId.IsValid)
            {
                throw new CharacterAnimationRigValidationException(
                    CharacterAnimationRigValidationCode.SemanticBoneInvalid,
                    $"Animation Rig '{name}' contains an invalid biped Bone identity.");
            }
            if (!semanticBones.Add(boneId))
                ThrowDuplicateSemantic(boneId);
        }

        void ThrowDuplicateSemantic(AnimationBoneId boneId) =>
            throw new CharacterAnimationRigValidationException(
                CharacterAnimationRigValidationCode.SemanticBoneDuplicate,
                $"Animation Rig '{name}' assigns Physical Bone '{boneId}' to multiple semantic slots.");

        void RequireLegChainValid(
            string side,
            int pelvisIndex,
            CharacterAnimationLegChainDefinition leg)
        {
            int hipIndex = RequirePhysicalBoneIndex(leg.HipBoneId);
            int kneeIndex = RequirePhysicalBoneIndex(leg.KneeBoneId);
            int ankleIndex = RequirePhysicalBoneIndex(leg.AnkleBoneId);
            int toeIndex = RequirePhysicalBoneIndex(leg.ToeBoneId);
            RequireDirectParent(side, "Hip", hipIndex, pelvisIndex);
            RequireDirectParent(side, "Knee", kneeIndex, hipIndex);
            RequireDirectParent(side, "Ankle", ankleIndex, kneeIndex);
            RequireDirectParent(side, "Toe", toeIndex, ankleIndex);
            RequireSegmentLength(side, "Upper Leg", kneeIndex);
            RequireSegmentLength(side, "Lower Leg", ankleIndex);
            RequireSegmentLength(side, "Foot", toeIndex);
        }

        int[] RequireSpineValid(int pelvisIndex)
        {
            var indices = new int[SpineBoneCount];
            int parentIndex = pelvisIndex;
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = RequirePhysicalBoneIndex(GetSpineBoneId(i));
                if (PhysicalBones[indices[i]].ParentIndex != parentIndex)
                {
                    throw new CharacterAnimationRigValidationException(
                        CharacterAnimationRigValidationCode.SpineChainInvalid,
                        $"Animation Rig '{name}' Spine #{i} is not a direct child of the previous biped spine Bone.",
                        indices[i]);
                }
                RequirePositiveSegmentLength("Spine", indices[i], CharacterAnimationRigValidationCode.SpineChainInvalid);
                parentIndex = indices[i];
            }
            return indices;
        }

        void RequireSolverRootValid(int pelvisIndex, int[] spineIndices)
        {
            int solverRootIndex = RequirePhysicalBoneIndex(SolverRootBoneId);
            if (solverRootIndex == pelvisIndex || Array.IndexOf(spineIndices, solverRootIndex) >= 0)
                return;
            throw new CharacterAnimationRigValidationException(
                CharacterAnimationRigValidationCode.SemanticBoneInvalid,
                $"Animation Rig '{name}' Solver Root must be the Pelvis or an ordered Spine Bone.",
                solverRootIndex);
        }

        void RequireArmChainValid(string side, int spineEndIndex, CharacterAnimationArmChainDefinition arm)
        {
            int upperArmIndex = RequirePhysicalBoneIndex(arm.UpperArmBoneId);
            int forearmIndex = RequirePhysicalBoneIndex(arm.ForearmBoneId);
            int handIndex = RequirePhysicalBoneIndex(arm.HandBoneId);
            int expectedUpperParent = spineEndIndex;
            if (arm.HasClavicle)
            {
                int clavicleIndex = RequirePhysicalBoneIndex(arm.ClavicleBoneId);
                RequireParent(side, "Clavicle", clavicleIndex, spineEndIndex, CharacterAnimationRigValidationCode.ArmChainInvalid);
                RequirePositiveSegmentLength($"{side} Clavicle", clavicleIndex, CharacterAnimationRigValidationCode.ArmChainInvalid);
                expectedUpperParent = clavicleIndex;
            }
            RequireParent(side, "Upper Arm", upperArmIndex, expectedUpperParent, CharacterAnimationRigValidationCode.ArmChainInvalid);
            RequireParent(side, "Forearm", forearmIndex, upperArmIndex, CharacterAnimationRigValidationCode.ArmChainInvalid);
            RequireParent(side, "Hand", handIndex, forearmIndex, CharacterAnimationRigValidationCode.ArmChainInvalid);
            RequirePositiveSegmentLength($"{side} Upper Arm", forearmIndex, CharacterAnimationRigValidationCode.ArmChainInvalid);
            RequirePositiveSegmentLength($"{side} Forearm", handIndex, CharacterAnimationRigValidationCode.ArmChainInvalid);
        }

        void RequireOptionalHeadValid(int spineEndIndex)
        {
            if (!HeadBoneId.IsValid)
                return;
            int headIndex = RequirePhysicalBoneIndex(HeadBoneId);
            int current = headIndex;
            while (current >= 0 && current != spineEndIndex)
                current = PhysicalBones[current].ParentIndex;
            if (current == spineEndIndex)
                return;
            throw new CharacterAnimationRigValidationException(
                CharacterAnimationRigValidationCode.SemanticBoneInvalid,
                $"Animation Rig '{name}' Head is not a descendant of the ordered Spine.",
                headIndex);
        }

        void RequireParent(string side, string slot, int childIndex, int parentIndex, CharacterAnimationRigValidationCode code)
        {
            if (PhysicalBones[childIndex].ParentIndex == parentIndex)
                return;
            throw new CharacterAnimationRigValidationException(
                code,
                $"Animation Rig '{name}' {side} {slot} parent chain is invalid.",
                childIndex);
        }

        void RequirePositiveSegmentLength(string segment, int childIndex, CharacterAnimationRigValidationCode code)
        {
            float length = PhysicalBones[childIndex].ReferenceLocalPosition.magnitude;
            if (float.IsFinite(length) && length > 0.0001f)
                return;
            throw new CharacterAnimationRigValidationException(
                code,
                $"Animation Rig '{name}' {segment} reference length is invalid.",
                childIndex);
        }

        Vector3[] BuildReferenceComponentPositions()
        {
            var positions = new Vector3[PhysicalBoneCount];
            var matrices = new Matrix4x4[PhysicalBoneCount];
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                CharacterAnimationPhysicalBoneDefinition bone = PhysicalBones[i];
                Matrix4x4 local = Matrix4x4.TRS(
                    bone.ReferenceLocalPosition,
                    bone.ReferenceLocalRotation,
                    bone.ReferenceLocalScale);
                matrices[i] = bone.ParentIndex >= 0 ? matrices[bone.ParentIndex] * local : local;
                positions[i] = matrices[i].GetColumn(3);
            }
            return positions;
        }

        void RequireReferenceBendPlane(
            string limb,
            AnimationBoneId upperBoneId,
            AnimationBoneId middleBoneId,
            AnimationBoneId endBoneId,
            Vector3[] positions)
        {
            int upperIndex = RequirePhysicalBoneIndex(upperBoneId);
            int middleIndex = RequirePhysicalBoneIndex(middleBoneId);
            int endIndex = RequirePhysicalBoneIndex(endBoneId);
            Vector3 normal = Vector3.Cross(
                positions[middleIndex] - positions[upperIndex],
                positions[endIndex] - positions[middleIndex]);
            float area = normal.sqrMagnitude;
            if (float.IsFinite(area) && area > 0.00000001f)
                return;
            throw new CharacterAnimationRigValidationException(
                CharacterAnimationRigValidationCode.BendPlaneInvalid,
                $"Animation Rig '{name}' {limb} reference bend plane is degenerate.",
                middleIndex);
        }

        void RequireDirectParent(string side, string slot, int childIndex, int expectedParentIndex)
        {
            if (PhysicalBones[childIndex].ParentIndex == expectedParentIndex)
                return;
            throw new CharacterAnimationRigValidationException(
                CharacterAnimationRigValidationCode.LegChainInvalid,
                $"Animation Rig '{name}' {side} {slot} is not a direct child of its declared chain parent.",
                childIndex);
        }

        void RequireSegmentLength(string side, string segment, int childIndex)
        {
            float length = PhysicalBones[childIndex].ReferenceLocalPosition.magnitude;
            if (float.IsFinite(length) && length > 0.0001f)
                return;
            throw new CharacterAnimationRigValidationException(
                CharacterAnimationRigValidationCode.LegChainInvalid,
                $"Animation Rig '{name}' {side} {segment} reference length is invalid.",
                childIndex);
        }

        static AnimationBoneId ToOptionalBoneId(string value) =>
            string.IsNullOrWhiteSpace(value) ? default : new AnimationBoneId(value);

        static string RequireBoneId(AnimationBoneId boneId, string parameterName) =>
            boneId.IsValid
                ? boneId.Value
                : throw new ArgumentException("Full-biped Bone identity is invalid.", parameterName);
    }

    [Serializable]
    public sealed class CharacterAnimationBoneWeight
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField, Range(0f, 1f)] float m_Weight;

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public float Weight => m_Weight;

        public CharacterAnimationBoneWeight() { }

        public CharacterAnimationBoneWeight(AnimationBoneId boneId, float weight)
        {
            if (!boneId.IsValid)
                throw new ArgumentException("Animation Bone identity is invalid.", nameof(boneId));
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentOutOfRangeException(nameof(weight));
            m_BoneId = boneId.Value;
            m_Weight = weight;
        }
    }

}
