using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPoseOperationCode : byte
    {
        PoseSlotInput = 1,
        LayeredBoneBlend = 2,
        AdditivePose = 3,
        PoseCurveResolve = 4,
        OutputPose = 5
    }

    [Serializable]
    public sealed class CharacterPresentationPoseSlotProgramEntry
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_PoseSlotId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] PoseSlotOutputPolicy m_OutputPolicy;

        public CharacterPresentationPoseSlotProgramEntry(
            int index,
            PoseSlotId poseSlotId,
            AnimationChannelId animationChannelId,
            PoseSlotOutputPolicy outputPolicy)
        {
            if (index < 0 || !poseSlotId.IsValid || !animationChannelId.IsValid ||
                !Enum.IsDefined(typeof(PoseSlotOutputPolicy), outputPolicy))
            {
                throw new ArgumentException("Compiled Pose Slot entry is invalid.");
            }
            m_Index = index;
            m_PoseSlotId = poseSlotId.Value;
            m_AnimationChannelId = animationChannelId.Value;
            m_OutputPolicy = outputPolicy;
        }

        public int Index => m_Index;
        public PoseSlotId PoseSlotId => new PoseSlotId(m_PoseSlotId);
        public AnimationChannelId AnimationChannelId => new AnimationChannelId(m_AnimationChannelId);
        public PoseSlotOutputPolicy OutputPolicy => m_OutputPolicy;
    }

    [Serializable]
    public sealed class CharacterPresentationPoseParameterProgramEntry
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] float m_DefaultValue;

        public CharacterPresentationPoseParameterProgramEntry(int index, PoseParameterId parameterId, float defaultValue)
        {
            if (index < 0 || !parameterId.IsValid || !float.IsFinite(defaultValue))
                throw new ArgumentException("Compiled Pose Parameter entry is invalid.");
            m_Index = index;
            m_ParameterId = parameterId.Value;
            m_DefaultValue = defaultValue;
        }

        public int Index => m_Index;
        public PoseParameterId ParameterId => new PoseParameterId(m_ParameterId);
        public float DefaultValue => m_DefaultValue;
    }

    [Serializable]
    public sealed class CharacterPresentationDenseBoneMask
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_MaskId = string.Empty;
        [SerializeField] float[] m_Weights = Array.Empty<float>();

        public CharacterPresentationDenseBoneMask(int index, string maskId, float[] weights)
        {
            if (index < 0 || string.IsNullOrWhiteSpace(maskId) || weights == null || weights.Length == 0)
                throw new ArgumentException("Compiled dense Bone Mask is invalid.");
            m_Index = index;
            m_MaskId = maskId.Trim();
            m_Weights = (float[])weights.Clone();
            for (int i = 0; i < m_Weights.Length; i++)
            {
                if (!float.IsFinite(m_Weights[i]) || m_Weights[i] < 0f || m_Weights[i] > 1f)
                    throw new ArgumentOutOfRangeException(nameof(weights));
            }
        }

        public int Index => m_Index;
        public string MaskId => m_MaskId ?? string.Empty;
        public IReadOnlyList<float> Weights => m_Weights ?? Array.Empty<float>();
    }

    [Serializable]
    public sealed class CharacterPresentationAdditiveReferenceDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_ReferencePoseId = string.Empty;
        [SerializeField] AdditiveReferenceSpace m_Space;
        [SerializeField] AdditiveScalePolicy m_ScalePolicy;
        [SerializeField] Vector3[] m_Positions = Array.Empty<Vector3>();
        [SerializeField] Quaternion[] m_Rotations = Array.Empty<Quaternion>();
        [SerializeField] Vector3[] m_Scales = Array.Empty<Vector3>();

        public CharacterPresentationAdditiveReferenceDescriptor(
            int index,
            string referencePoseId,
            AdditiveReferenceSpace space,
            AdditiveScalePolicy scalePolicy,
            Vector3[] positions,
            Quaternion[] rotations,
            Vector3[] scales)
        {
            if (index < 0 || string.IsNullOrWhiteSpace(referencePoseId) ||
                !Enum.IsDefined(typeof(AdditiveReferenceSpace), space) ||
                !Enum.IsDefined(typeof(AdditiveScalePolicy), scalePolicy) ||
                positions == null || rotations == null || scales == null ||
                positions.Length == 0 || positions.Length != rotations.Length || positions.Length != scales.Length)
            {
                throw new ArgumentException("Compiled Additive reference descriptor is invalid.");
            }
            m_Index = index;
            m_ReferencePoseId = referencePoseId.Trim();
            m_Space = space;
            m_ScalePolicy = scalePolicy;
            m_Positions = (Vector3[])positions.Clone();
            m_Rotations = (Quaternion[])rotations.Clone();
            m_Scales = (Vector3[])scales.Clone();
        }

        public int Index => m_Index;
        public string ReferencePoseId => m_ReferencePoseId ?? string.Empty;
        public AdditiveReferenceSpace Space => m_Space;
        public AdditiveScalePolicy ScalePolicy => m_ScalePolicy;
        public IReadOnlyList<Vector3> Positions => m_Positions ?? Array.Empty<Vector3>();
        public IReadOnlyList<Quaternion> Rotations => m_Rotations ?? Array.Empty<Quaternion>();
        public IReadOnlyList<Vector3> Scales => m_Scales ?? Array.Empty<Vector3>();
    }

    [Serializable]
    public sealed class CharacterPresentationPoseOperation
    {
        public const int PayloadVersion = 1;

        [SerializeField] int m_Index;
        [SerializeField] CharacterPoseOperationCode m_Code;
        [SerializeField] int m_PayloadVersion = PayloadVersion;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_OutputPoseValueIndex;
        [SerializeField] int m_InputPoseValueIndexA = -1;
        [SerializeField] int m_InputPoseValueIndexB = -1;
        [SerializeField] int m_PoseSlotIndex = -1;
        [SerializeField] int m_BoneMaskIndex = -1;
        [SerializeField] int m_AdditiveReferenceIndex = -1;
        [SerializeField] float m_Weight = 1f;
        [SerializeField] PoseParameterResolvePolicy[] m_ParameterPolicies = Array.Empty<PoseParameterResolvePolicy>();

        public CharacterPresentationPoseOperation(
            int index,
            CharacterPoseOperationCode code,
            PoseNodeId nodeId,
            int outputPoseValueIndex,
            int inputPoseValueIndexA,
            int inputPoseValueIndexB,
            int poseSlotIndex,
            int boneMaskIndex,
            int additiveReferenceIndex,
            float weight,
            PoseParameterResolvePolicy[] parameterPolicies)
        {
            if (index < 0 || !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) || !nodeId.IsValid ||
                outputPoseValueIndex < 0 || !float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentException("Compiled Pose operation is invalid.");
            m_Index = index;
            m_Code = code;
            m_NodeId = nodeId.Value;
            m_OutputPoseValueIndex = outputPoseValueIndex;
            m_InputPoseValueIndexA = inputPoseValueIndexA;
            m_InputPoseValueIndexB = inputPoseValueIndexB;
            m_PoseSlotIndex = poseSlotIndex;
            m_BoneMaskIndex = boneMaskIndex;
            m_AdditiveReferenceIndex = additiveReferenceIndex;
            m_Weight = weight;
            m_ParameterPolicies = parameterPolicies ?? Array.Empty<PoseParameterResolvePolicy>();
        }

        public int Index => m_Index;
        public CharacterPoseOperationCode Code => m_Code;
        public int Version => m_PayloadVersion;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public int OutputPoseValueIndex => m_OutputPoseValueIndex;
        public int InputPoseValueIndexA => m_InputPoseValueIndexA;
        public int InputPoseValueIndexB => m_InputPoseValueIndexB;
        public int PoseSlotIndex => m_PoseSlotIndex;
        public int BoneMaskIndex => m_BoneMaskIndex;
        public int AdditiveReferenceIndex => m_AdditiveReferenceIndex;
        public float Weight => m_Weight;
        public IReadOnlyList<PoseParameterResolvePolicy> ParameterPolicies => m_ParameterPolicies ?? Array.Empty<PoseParameterResolvePolicy>();
    }

    [Serializable]
    public sealed class CharacterPresentationPoseSourceMapEntry
    {
        [SerializeField] int m_OperationIndex;
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_CallSite = string.Empty;

        public CharacterPresentationPoseSourceMapEntry(int operationIndex, string graphId, PoseNodeId nodeId, string callSite)
        {
            if (operationIndex < 0 || string.IsNullOrWhiteSpace(graphId) || !nodeId.IsValid)
                throw new ArgumentException("Pose operation source map entry is invalid.");
            m_OperationIndex = operationIndex;
            m_GraphId = graphId.Trim();
            m_NodeId = nodeId.Value;
            m_CallSite = callSite ?? string.Empty;
        }

        public int OperationIndex => m_OperationIndex;
        public string GraphId => m_GraphId ?? string.Empty;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public string CallSite => m_CallSite ?? string.Empty;
    }

    [Serializable]
    public sealed class CharacterPresentationPoseProgram
    {
        public const string SchemaVersion = "character-presentation-pose-program/v1";
        public const string RuntimeAbi = "character-presentation-pose-runtime/v1";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] string m_RuntimeAbi = RuntimeAbi;
        [SerializeField] string m_PoseGraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] string m_ProgramHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] int m_BoneCount;
        [SerializeField] int m_LeftFootBoneIndex = -1;
        [SerializeField] int m_RightFootBoneIndex = -1;
        [SerializeField] CharacterPresentationPoseSlotProgramEntry[] m_Slots = Array.Empty<CharacterPresentationPoseSlotProgramEntry>();
        [SerializeField] CharacterPresentationPoseParameterProgramEntry[] m_Parameters = Array.Empty<CharacterPresentationPoseParameterProgramEntry>();
        [SerializeField] CharacterPresentationDenseBoneMask[] m_BoneMasks = Array.Empty<CharacterPresentationDenseBoneMask>();
        [SerializeField] CharacterPresentationAdditiveReferenceDescriptor[] m_AdditiveReferences = Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        [SerializeField] CharacterPresentationPoseOperation[] m_Operations = Array.Empty<CharacterPresentationPoseOperation>();
        [SerializeField] CharacterPresentationPoseSourceMapEntry[] m_SourceMap = Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        [SerializeField] int m_PoseValueWorkspaceCount;
        [SerializeField] int m_ParameterWorkspaceCount;
        [SerializeField] int m_ContributionWorkspaceCount;
        [SerializeField] int m_FrameCacheCount;
        [SerializeField] int m_OutputOperationIndex = -1;

        public CharacterPresentationPoseProgram(
            string poseGraphId,
            string contentRevision,
            string programHash,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationPoseSlotProgramEntry[] slots,
            CharacterPresentationPoseParameterProgramEntry[] parameters,
            CharacterPresentationDenseBoneMask[] boneMasks,
            CharacterPresentationAdditiveReferenceDescriptor[] additiveReferences,
            CharacterPresentationPoseOperation[] operations,
            CharacterPresentationPoseSourceMapEntry[] sourceMap,
            int poseValueWorkspaceCount,
            int parameterWorkspaceCount,
            int contributionWorkspaceCount,
            int frameCacheCount,
            int outputOperationIndex)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_PoseGraphId = PoseSlotId.Require(poseGraphId, nameof(poseGraphId));
            m_ContentRevision = PoseSlotId.Require(contentRevision, nameof(contentRevision));
            m_ProgramHash = PoseSlotId.Require(programHash, nameof(programHash));
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_BoneCount = rig.Bones.Count;
            m_LeftFootBoneIndex = rig.RequireBoneIndex(rig.LeftFootBoneId);
            m_RightFootBoneIndex = rig.RequireBoneIndex(rig.RightFootBoneId);
            m_Slots = slots ?? throw new ArgumentNullException(nameof(slots));
            m_Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            m_BoneMasks = boneMasks ?? throw new ArgumentNullException(nameof(boneMasks));
            m_AdditiveReferences = additiveReferences ?? throw new ArgumentNullException(nameof(additiveReferences));
            m_Operations = operations ?? throw new ArgumentNullException(nameof(operations));
            m_SourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
            m_PoseValueWorkspaceCount = poseValueWorkspaceCount;
            m_ParameterWorkspaceCount = parameterWorkspaceCount;
            m_ContributionWorkspaceCount = contributionWorkspaceCount;
            m_FrameCacheCount = frameCacheCount;
            m_OutputOperationIndex = outputOperationIndex;
            RequireValid();
        }

        public string PoseGraphId => m_PoseGraphId ?? string.Empty;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public string ProgramHash => m_ProgramHash ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public int BoneCount => m_BoneCount;
        public int LeftFootBoneIndex => m_LeftFootBoneIndex;
        public int RightFootBoneIndex => m_RightFootBoneIndex;
        public IReadOnlyList<CharacterPresentationPoseSlotProgramEntry> Slots => m_Slots ?? Array.Empty<CharacterPresentationPoseSlotProgramEntry>();
        public IReadOnlyList<CharacterPresentationPoseParameterProgramEntry> Parameters => m_Parameters ?? Array.Empty<CharacterPresentationPoseParameterProgramEntry>();
        public IReadOnlyList<CharacterPresentationDenseBoneMask> BoneMasks => m_BoneMasks ?? Array.Empty<CharacterPresentationDenseBoneMask>();
        public IReadOnlyList<CharacterPresentationAdditiveReferenceDescriptor> AdditiveReferences => m_AdditiveReferences ?? Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        public IReadOnlyList<CharacterPresentationPoseOperation> Operations => m_Operations ?? Array.Empty<CharacterPresentationPoseOperation>();
        public IReadOnlyList<CharacterPresentationPoseSourceMapEntry> SourceMap => m_SourceMap ?? Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        public int PoseValueWorkspaceCount => m_PoseValueWorkspaceCount;
        public int ParameterWorkspaceCount => m_ParameterWorkspaceCount;
        public int ContributionWorkspaceCount => m_ContributionWorkspaceCount;
        public int FrameCacheCount => m_FrameCacheCount;
        public int OutputOperationIndex => m_OutputOperationIndex;

        public CharacterPresentationPoseSlotProgramEntry RequireSlot(AnimationChannelId channelId)
        {
            CharacterPresentationPoseSlotProgramEntry match = null;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].AnimationChannelId == channelId)
                {
                    if (match != null)
                        throw new InvalidOperationException($"Pose Program duplicates Animation Channel '{channelId}'.");
                    match = Slots[i];
                }
            }
            return match ?? throw new InvalidOperationException($"Pose Program has no Pose Slot for Animation Channel '{channelId}'.");
        }

        public void RequireValid()
        {
            if (!string.Equals(m_SchemaVersion, SchemaVersion, StringComparison.Ordinal) ||
                !string.Equals(m_RuntimeAbi, RuntimeAbi, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(PoseGraphId) || string.IsNullOrEmpty(ContentRevision) || string.IsNullOrEmpty(ProgramHash) ||
                string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(RigRevision) || BoneCount <= 0 ||
                LeftFootBoneIndex < 0 || LeftFootBoneIndex >= BoneCount || RightFootBoneIndex < 0 || RightFootBoneIndex >= BoneCount ||
                Slots.Count == 0 || Operations.Count == 0 || OutputOperationIndex < 0 || OutputOperationIndex >= Operations.Count ||
                PoseValueWorkspaceCount <= 0 || ParameterWorkspaceCount < Parameters.Count || ContributionWorkspaceCount <= 0 || FrameCacheCount <= 0)
            {
                throw new InvalidOperationException("Character Presentation Pose Program header or workspace layout is invalid.");
            }
            var channels = new HashSet<AnimationChannelId>();
            var slots = new HashSet<PoseSlotId>();
            for (int i = 0; i < Slots.Count; i++)
            {
                CharacterPresentationPoseSlotProgramEntry slot = Slots[i];
                if (slot == null || slot.Index != i || !channels.Add(slot.AnimationChannelId) || !slots.Add(slot.PoseSlotId))
                    throw new InvalidOperationException($"Character Presentation Pose Program Slot #{i} is invalid or duplicated.");
            }
            var parameters = new HashSet<PoseParameterId>();
            for (int i = 0; i < Parameters.Count; i++)
            {
                CharacterPresentationPoseParameterProgramEntry parameter = Parameters[i];
                if (parameter == null || parameter.Index != i || !parameter.ParameterId.IsValid ||
                    !float.IsFinite(parameter.DefaultValue) || !parameters.Add(parameter.ParameterId))
                {
                    throw new InvalidOperationException($"Character Presentation Pose Program Parameter #{i} is invalid or duplicated.");
                }
            }
            for (int i = 0; i < BoneMasks.Count; i++)
            {
                CharacterPresentationDenseBoneMask mask = BoneMasks[i];
                if (mask == null || mask.Index != i || mask.Weights.Count != BoneCount)
                    throw new InvalidOperationException($"Character Presentation Pose Program Bone Mask #{i} is invalid.");
            }
            for (int i = 0; i < AdditiveReferences.Count; i++)
            {
                CharacterPresentationAdditiveReferenceDescriptor reference = AdditiveReferences[i];
                if (reference == null || reference.Index != i || string.IsNullOrEmpty(reference.ReferencePoseId) ||
                    !Enum.IsDefined(typeof(AdditiveReferenceSpace), reference.Space) ||
                    !Enum.IsDefined(typeof(AdditiveScalePolicy), reference.ScalePolicy) ||
                    reference.Positions.Count != BoneCount || reference.Rotations.Count != BoneCount ||
                    reference.Scales.Count != BoneCount)
                {
                    throw new InvalidOperationException($"Character Presentation Pose Program Additive reference #{i} is invalid.");
                }
                for (int bone = 0; bone < BoneCount; bone++)
                {
                    if (!IsFinite(reference.Positions[bone]) || !IsFinite(reference.Rotations[bone]) ||
                        Quaternion.Dot(reference.Rotations[bone], reference.Rotations[bone]) <= 0f ||
                        !IsFinite(reference.Scales[bone]) ||
                        reference.ScalePolicy == AdditiveScalePolicy.Multiply && HasZeroComponent(reference.Scales[bone]))
                    {
                        throw new InvalidOperationException($"Character Presentation Pose Program Additive reference #{i} Bone #{bone} is invalid.");
                    }
                }
            }
            if (SourceMap.Count != Operations.Count)
                throw new InvalidOperationException("Character Presentation Pose Program source map count is inconsistent.");
            int outputCount = 0;
            var outputValues = new HashSet<int>();
            for (int i = 0; i < Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = Operations[i];
                if (operation == null || operation.Index != i || operation.Version != CharacterPresentationPoseOperation.PayloadVersion ||
                    !Enum.IsDefined(typeof(CharacterPoseOperationCode), operation.Code) || operation.OutputPoseValueIndex < 0 ||
                    operation.OutputPoseValueIndex >= PoseValueWorkspaceCount || !outputValues.Add(operation.OutputPoseValueIndex))
                {
                    throw new InvalidOperationException($"Character Presentation Pose Program operation #{i} is unsupported or invalid.");
                }
                RequireOperationLayout(operation);
                CharacterPresentationPoseSourceMapEntry source = SourceMap[i];
                if (source == null || source.OperationIndex != i || !source.NodeId.Equals(operation.NodeId) ||
                    string.IsNullOrEmpty(source.GraphId))
                {
                    throw new InvalidOperationException($"Character Presentation Pose Program source map #{i} is invalid.");
                }
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != OutputOperationIndex)
                        throw new InvalidOperationException("Character Presentation Pose Program Output operation index is inconsistent.");
                }
            }
            if (outputCount != 1)
                throw new InvalidOperationException("Character Presentation Pose Program must contain exactly one Output operation.");
        }

        void RequireOperationLayout(CharacterPresentationPoseOperation operation)
        {
            bool inputA = IsPriorValue(operation.InputPoseValueIndexA, operation.OutputPoseValueIndex);
            bool inputB = IsPriorValue(operation.InputPoseValueIndexB, operation.OutputPoseValueIndex);
            bool slot = (uint)operation.PoseSlotIndex < (uint)Slots.Count;
            bool mask = (uint)operation.BoneMaskIndex < (uint)BoneMasks.Count;
            bool additive = (uint)operation.AdditiveReferenceIndex < (uint)AdditiveReferences.Count;
            bool noInputA = operation.InputPoseValueIndexA == -1;
            bool noInputB = operation.InputPoseValueIndexB == -1;
            bool noSlot = operation.PoseSlotIndex == -1;
            bool noMask = operation.BoneMaskIndex == -1;
            bool noAdditive = operation.AdditiveReferenceIndex == -1;
            bool noPolicies = operation.ParameterPolicies.Count == 0;
            bool completePolicies = operation.ParameterPolicies.Count == Parameters.Count;
            if (completePolicies)
            {
                for (int i = 0; i < operation.ParameterPolicies.Count; i++)
                {
                    if (!Enum.IsDefined(typeof(PoseParameterResolvePolicy), operation.ParameterPolicies[i]))
                        completePolicies = false;
                }
            }

            bool valid = operation.Code switch
            {
                CharacterPoseOperationCode.PoseSlotInput => noInputA && noInputB && slot && noMask && noAdditive && noPolicies,
                CharacterPoseOperationCode.LayeredBoneBlend => inputA && inputB && noSlot && mask && noAdditive && completePolicies,
                CharacterPoseOperationCode.AdditivePose => inputA && inputB && noSlot && mask && additive && completePolicies,
                CharacterPoseOperationCode.PoseCurveResolve => inputA && noInputB && noSlot && noMask && noAdditive && completePolicies,
                CharacterPoseOperationCode.OutputPose => inputA && noInputB && noSlot && noMask && noAdditive && noPolicies,
                _ => false
            };
            if (!valid)
                throw new InvalidOperationException($"Character Presentation Pose operation '{operation.NodeId}' has an invalid compiled layout.");
        }

        static bool IsPriorValue(int input, int output) => input >= 0 && input < output;

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);

        static bool HasZeroComponent(Vector3 value) =>
            Mathf.Abs(value.x) <= 0.000001f || Mathf.Abs(value.y) <= 0.000001f || Mathf.Abs(value.z) <= 0.000001f;
    }
}
