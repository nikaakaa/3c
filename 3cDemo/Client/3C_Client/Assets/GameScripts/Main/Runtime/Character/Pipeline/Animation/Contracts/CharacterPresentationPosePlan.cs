using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPosePlanPhase : byte
    {
        Selection = 1,
        SourceAndNativePose = 2,
        WorldAwarePostProcess = 3,
        FinalPublication = 4
    }

    public enum CharacterPoseOperationCode : byte
    {
        AnimationSelectionInput = 1,
        MotionMatchingSelectionInput = 2,
        ProgramParameterInput = 3,
        SelectedPosePlayer = 4,
        BlendStack = 5,
        Inertialization = 6,
        BlendPose = 7,
        LayeredBoneBlend = 8,
        AdditivePose = 9,
        PoseParameterResolve = 10,
        ModifyBone = 11,
        FootPlacement = 12,
        OutputPose = 13,
        MarkerSync = 14,
        BlendSpacePlayer = 15
    }

    [Serializable]
    public sealed class CharacterPresentationSelectionInputEntry
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] string m_ProgramProducerId = string.Empty;
        [SerializeField] bool m_MotionMatching;
        [SerializeField] AnimationSelectionAvailabilityPolicy m_Availability;

        public CharacterPresentationSelectionInputEntry(
            int index,
            PoseNodeId nodeId,
            AnimationChannelId animationChannelId,
            string programProducerId,
            bool motionMatching,
            AnimationSelectionAvailabilityPolicy availability)
        {
            if (index < 0 || !nodeId.IsValid || !animationChannelId.IsValid ||
                !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), availability) ||
                motionMatching != !string.IsNullOrWhiteSpace(programProducerId))
                throw new ArgumentException("Compiled Animation Selection input is invalid.");
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_AnimationChannelId = animationChannelId.Value;
            m_ProgramProducerId = programProducerId ?? string.Empty;
            m_MotionMatching = motionMatching;
            m_Availability = availability;
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public AnimationChannelId AnimationChannelId => new AnimationChannelId(m_AnimationChannelId);
        public string ProgramProducerId => m_ProgramProducerId ?? string.Empty;
        public bool MotionMatching => m_MotionMatching;
        public AnimationSelectionAvailabilityPolicy Availability => m_Availability;
    }

    [Serializable]
    public sealed class CharacterPresentationPoseParameterEntry
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] PoseParameterValueType m_ValueType;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] float m_DefaultValue;

        public CharacterPresentationPoseParameterEntry(
            int index,
            PoseParameterId parameterId,
            PoseParameterValueType valueType,
            float defaultValue,
            string unit)
        {
            if (index < 0 || !parameterId.IsValid || !Enum.IsDefined(typeof(PoseParameterValueType), valueType) ||
                !float.IsFinite(defaultValue))
                throw new ArgumentException("Compiled Pose Parameter entry is invalid.");
            m_Index = index;
            m_ParameterId = parameterId.Value;
            m_ValueType = valueType;
            m_Unit = unit?.Trim() ?? string.Empty;
            m_DefaultValue = defaultValue;
        }

        public int Index => m_Index;
        public PoseParameterId ParameterId => new PoseParameterId(m_ParameterId);
        public PoseParameterValueType ValueType => m_ValueType;
        public string Unit => m_Unit ?? string.Empty;
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
            if (index < 0 || !string.Equals(referencePoseId, AnimationAdditiveReferencePoseIds.RigReference, StringComparison.Ordinal) ||
                !Enum.IsDefined(typeof(AdditiveReferenceSpace), space) || !Enum.IsDefined(typeof(AdditiveScalePolicy), scalePolicy) ||
                positions == null || rotations == null || scales == null || positions.Length == 0 ||
                positions.Length != rotations.Length || positions.Length != scales.Length)
                throw new ArgumentException("Compiled Additive reference descriptor is invalid.");
            m_Index = index;
            m_ReferencePoseId = referencePoseId;
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
    public sealed class CharacterPresentationModifyBoneDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] int m_BoneIndex;
        [SerializeField] int m_ParentBoneIndex;
        [SerializeField] ModifyBoneReferenceSpace m_ReferenceSpace;
        [SerializeField] ModifyBoneOperationMask m_Operations;
        [SerializeField] Vector3 m_Position;
        [SerializeField] Quaternion m_Rotation;
        [SerializeField] Vector3 m_Scale;

        public CharacterPresentationModifyBoneDescriptor(int index, int boneIndex, int parentBoneIndex, CharacterPoseNodeDefinition node)
        {
            if (index < 0 || boneIndex < 0 || parentBoneIndex < -1 || node == null)
                throw new ArgumentException("Compiled Modify Bone descriptor is invalid.");
            m_Index = index;
            m_BoneIndex = boneIndex;
            m_ParentBoneIndex = parentBoneIndex;
            m_ReferenceSpace = node.ModifyBoneReferenceSpace;
            m_Operations = node.ModifyBoneOperations;
            m_Position = node.ModifyPosition;
            m_Rotation = node.ModifyRotation;
            m_Scale = node.ModifyScale;
        }

        public int Index => m_Index;
        public int BoneIndex => m_BoneIndex;
        public int ParentBoneIndex => m_ParentBoneIndex;
        public ModifyBoneReferenceSpace ReferenceSpace => m_ReferenceSpace;
        public ModifyBoneOperationMask Operations => m_Operations;
        public Vector3 Position => m_Position;
        public Quaternion Rotation => m_Rotation;
        public Vector3 Scale => m_Scale;
    }

    [Serializable]
    public sealed class CharacterPresentationInertializationRuleDescriptor
    {
        [SerializeField] int m_SourceProgramProducerIndex;
        [SerializeField] int m_TargetProgramProducerIndex;
        [SerializeField] PoseInertializationMode m_Mode;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] int m_CurveIndex = -1;
        [SerializeField] int m_ProfileIndex = -1;
        [SerializeField] PoseParameterInertializationMode[] m_ParameterModes = Array.Empty<PoseParameterInertializationMode>();

        public CharacterPresentationInertializationRuleDescriptor(
            int sourceProgramProducerIndex,
            int targetProgramProducerIndex,
            PoseInertializationMode mode,
            float durationSeconds,
            int curveIndex,
            int profileIndex,
            PoseParameterInertializationMode[] parameterModes)
        {
            if (sourceProgramProducerIndex < 0 || targetProgramProducerIndex < 0 ||
                !Enum.IsDefined(typeof(PoseInertializationMode), mode) ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f ||
                mode == PoseInertializationMode.Inertialize &&
                (durationSeconds <= 0f || curveIndex < 0 || profileIndex < 0) ||
                mode == PoseInertializationMode.HardCut && (curveIndex != -1 || profileIndex != -1) ||
                parameterModes == null || parameterModes.Length == 0)
                throw new ArgumentException("Compiled Inertialization exact rule is invalid.");
            m_SourceProgramProducerIndex = sourceProgramProducerIndex;
            m_TargetProgramProducerIndex = targetProgramProducerIndex;
            m_Mode = mode;
            m_DurationSeconds = durationSeconds;
            m_CurveIndex = curveIndex;
            m_ProfileIndex = profileIndex;
            m_ParameterModes = parameterModes;
        }

        public int SourceProgramProducerIndex => m_SourceProgramProducerIndex;
        public int TargetProgramProducerIndex => m_TargetProgramProducerIndex;
        public PoseInertializationMode Mode => m_Mode;
        public float DurationSeconds => m_DurationSeconds;
        public int CurveIndex => m_CurveIndex;
        public int ProfileIndex => m_ProfileIndex;
        public IReadOnlyList<PoseParameterInertializationMode> ParameterModes => m_ParameterModes ?? Array.Empty<PoseParameterInertializationMode>();
    }

    [Serializable]
    public sealed class CharacterPresentationInertializationDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_InputPlayerNodeId = string.Empty;
        [SerializeField] int m_InputPlayerIndex;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_PolicyRevision = string.Empty;
        [SerializeField] CharacterPresentationInertializationRuleDescriptor[] m_Rules = Array.Empty<CharacterPresentationInertializationRuleDescriptor>();

        public CharacterPresentationInertializationDescriptor(
            int index,
            PoseNodeId nodeId,
            PoseNodeId inputPlayerNodeId,
            int inputPlayerIndex,
            string policyId,
            string policyRevision,
            CharacterPresentationInertializationRuleDescriptor[] rules)
        {
            if (index < 0 || !nodeId.IsValid || !inputPlayerNodeId.IsValid || inputPlayerIndex < 0 ||
                string.IsNullOrWhiteSpace(policyId) || string.IsNullOrWhiteSpace(policyRevision) ||
                rules == null || rules.Length == 0)
                throw new ArgumentException("Compiled Inertialization descriptor is invalid.");
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_InputPlayerNodeId = inputPlayerNodeId.Value;
            m_InputPlayerIndex = inputPlayerIndex;
            m_PolicyId = policyId;
            m_PolicyRevision = policyRevision;
            m_Rules = rules;
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public PoseNodeId InputPlayerNodeId => new PoseNodeId(m_InputPlayerNodeId);
        public int InputPlayerIndex => m_InputPlayerIndex;
        public string PolicyId => m_PolicyId ?? string.Empty;
        public string PolicyRevision => m_PolicyRevision ?? string.Empty;
        public IReadOnlyList<CharacterPresentationInertializationRuleDescriptor> Rules => m_Rules ?? Array.Empty<CharacterPresentationInertializationRuleDescriptor>();
    }

    [Serializable]
    public sealed class CharacterPresentationFootPlacementNodeDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_CalibrationId = string.Empty;
        [SerializeField] string m_CalibrationRevision = string.Empty;

        public CharacterPresentationFootPlacementNodeDescriptor(int index, PoseNodeId nodeId, string calibrationId, string calibrationRevision)
        {
            if (index < 0 || !nodeId.IsValid || string.IsNullOrWhiteSpace(calibrationId) || string.IsNullOrWhiteSpace(calibrationRevision))
                throw new ArgumentException("Compiled Foot Placement node descriptor is invalid.");
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_CalibrationId = calibrationId;
            m_CalibrationRevision = calibrationRevision;
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public string CalibrationId => m_CalibrationId ?? string.Empty;
        public string CalibrationRevision => m_CalibrationRevision ?? string.Empty;
    }

    [Serializable]
    public sealed class CharacterPresentationPoseOperation
    {
        public const int PayloadVersion = 8;

        [SerializeField] int m_Index;
        [SerializeField] CharacterPosePlanPhase m_Phase;
        [SerializeField] CharacterPoseOperationCode m_Code;
        [SerializeField] int m_PayloadVersion = PayloadVersion;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_OutputValueIndex = -1;
        [SerializeField] int m_InputValueIndexA = -1;
        [SerializeField] int m_InputValueIndexB = -1;
        [SerializeField] int m_SelectionInputIndex = -1;
        [SerializeField] int m_MarkerSyncOperationIndex = -1;
        [SerializeField] int m_ParameterIndex = -1;
        [SerializeField] int m_ParameterIndexB = -1;
        [SerializeField] CharacterAnimationBlendSpaceInputRangePolicy m_BlendSpaceInputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        [SerializeField] int m_PlayerIndex = -1;
        [SerializeField] int m_BlendNodeIndex = -1;
        [SerializeField] int m_InertializationIndex = -1;
        [SerializeField] int m_BoneMaskIndex = -1;
        [SerializeField] int m_AdditiveReferenceIndex = -1;
        [SerializeField] int m_ModifyBoneIndex = -1;
        [SerializeField] int m_FootPlacementNodeIndex = -1;
        [SerializeField] float m_Weight = 1f;
        [SerializeField] PoseParameterResolvePolicy[] m_ParameterPolicies = Array.Empty<PoseParameterResolvePolicy>();

        public CharacterPresentationPoseOperation(
            int index,
            CharacterPosePlanPhase phase,
            CharacterPoseOperationCode code,
            PoseNodeId nodeId,
            int outputValueIndex,
            int inputValueIndexA,
            int inputValueIndexB,
            int selectionInputIndex,
            int markerSyncOperationIndex,
            int parameterIndex,
            int parameterIndexB,
            CharacterAnimationBlendSpaceInputRangePolicy blendSpaceInputRangePolicy,
            int playerIndex,
            int blendNodeIndex,
            int inertializationIndex,
            int boneMaskIndex,
            int additiveReferenceIndex,
            int modifyBoneIndex,
            int footPlacementNodeIndex,
            float weight,
            PoseParameterResolvePolicy[] parameterPolicies)
        {
            if (index < 0 || !Enum.IsDefined(typeof(CharacterPosePlanPhase), phase) ||
                !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) || !nodeId.IsValid ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), blendSpaceInputRangePolicy))
                throw new ArgumentException("Compiled Pose Plan operation is invalid.");
            m_Index = index;
            m_Phase = phase;
            m_Code = code;
            m_NodeId = nodeId.Value;
            m_OutputValueIndex = outputValueIndex;
            m_InputValueIndexA = inputValueIndexA;
            m_InputValueIndexB = inputValueIndexB;
            m_SelectionInputIndex = selectionInputIndex;
            m_MarkerSyncOperationIndex = markerSyncOperationIndex;
            m_ParameterIndex = parameterIndex;
            m_ParameterIndexB = parameterIndexB;
            m_BlendSpaceInputRangePolicy = blendSpaceInputRangePolicy;
            m_PlayerIndex = playerIndex;
            m_BlendNodeIndex = blendNodeIndex;
            m_InertializationIndex = inertializationIndex;
            m_BoneMaskIndex = boneMaskIndex;
            m_AdditiveReferenceIndex = additiveReferenceIndex;
            m_ModifyBoneIndex = modifyBoneIndex;
            m_FootPlacementNodeIndex = footPlacementNodeIndex;
            m_Weight = weight;
            m_ParameterPolicies = parameterPolicies ?? Array.Empty<PoseParameterResolvePolicy>();
        }

        public int Index => m_Index;
        public CharacterPosePlanPhase Phase => m_Phase;
        public CharacterPoseOperationCode Code => m_Code;
        public int Version => m_PayloadVersion;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public int OutputValueIndex => m_OutputValueIndex;
        public int InputValueIndexA => m_InputValueIndexA;
        public int InputValueIndexB => m_InputValueIndexB;
        public int SelectionInputIndex => m_SelectionInputIndex;
        public int MarkerSyncOperationIndex => m_MarkerSyncOperationIndex;
        public int ParameterIndex => m_ParameterIndex;
        public int ParameterIndexB => m_ParameterIndexB;
        public CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy => m_BlendSpaceInputRangePolicy;
        public int PlayerIndex => m_PlayerIndex;
        public int BlendNodeIndex => m_BlendNodeIndex;
        public int InertializationIndex => m_InertializationIndex;
        public int BoneMaskIndex => m_BoneMaskIndex;
        public int AdditiveReferenceIndex => m_AdditiveReferenceIndex;
        public int ModifyBoneIndex => m_ModifyBoneIndex;
        public int FootPlacementNodeIndex => m_FootPlacementNodeIndex;
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
    public sealed class CharacterPresentationPosePlan
    {
        public const string SchemaVersion = "character-presentation-pose-plan/v5";
        public const string RuntimeAbi = "character-presentation-pose-runtime/v9";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] string m_RuntimeAbi = RuntimeAbi;
        [SerializeField] string m_PoseGraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] string m_PlanHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] int m_BoneCount;
        [SerializeField] int m_LeftFootBoneIndex = -1;
        [SerializeField] int m_RightFootBoneIndex = -1;
        [SerializeField] CharacterPresentationSelectionInputEntry[] m_SelectionInputs = Array.Empty<CharacterPresentationSelectionInputEntry>();
        [SerializeField] CharacterPresentationPoseParameterEntry[] m_Parameters = Array.Empty<CharacterPresentationPoseParameterEntry>();
        [SerializeField] AnimationBlendNodePayload[] m_BlendNodes = Array.Empty<AnimationBlendNodePayload>();
        [SerializeField] CharacterPresentationInertializationDescriptor[] m_Inertializations = Array.Empty<CharacterPresentationInertializationDescriptor>();
        [SerializeField] CharacterPresentationDenseBoneMask[] m_BoneMasks = Array.Empty<CharacterPresentationDenseBoneMask>();
        [SerializeField] CharacterPresentationAdditiveReferenceDescriptor[] m_AdditiveReferences = Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        [SerializeField] CharacterPresentationModifyBoneDescriptor[] m_ModifyBones = Array.Empty<CharacterPresentationModifyBoneDescriptor>();
        [SerializeField] CharacterPresentationFootPlacementNodeDescriptor[] m_FootPlacementNodes = Array.Empty<CharacterPresentationFootPlacementNodeDescriptor>();
        [SerializeField] CharacterPresentationPoseOperation[] m_Operations = Array.Empty<CharacterPresentationPoseOperation>();
        [SerializeField] CharacterPresentationPoseSourceMapEntry[] m_SourceMap = Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        [SerializeField] int m_SelectionWorkspaceCount;
        [SerializeField] int m_PoseValueWorkspaceCount;
        [SerializeField] int m_ParameterWorkspaceCount;
        [SerializeField] int m_ContributionWorkspaceCount;
        [SerializeField] int m_FrameCacheCount;
        [SerializeField] int m_OutputOperationIndex = -1;

        public CharacterPresentationPosePlan(
            string poseGraphId,
            string contentRevision,
            string planHash,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationSelectionInputEntry[] selectionInputs,
            CharacterPresentationPoseParameterEntry[] parameters,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationInertializationDescriptor[] inertializations,
            CharacterPresentationDenseBoneMask[] boneMasks,
            CharacterPresentationAdditiveReferenceDescriptor[] additiveReferences,
            CharacterPresentationModifyBoneDescriptor[] modifyBones,
            CharacterPresentationFootPlacementNodeDescriptor[] footPlacementNodes,
            CharacterPresentationPoseOperation[] operations,
            CharacterPresentationPoseSourceMapEntry[] sourceMap,
            int selectionWorkspaceCount,
            int poseValueWorkspaceCount,
            int parameterWorkspaceCount,
            int contributionWorkspaceCount,
            int frameCacheCount,
            int outputOperationIndex)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_PoseGraphId = PoseIdentity.Require(poseGraphId, nameof(poseGraphId));
            m_ContentRevision = PoseIdentity.Require(contentRevision, nameof(contentRevision));
            m_PlanHash = PoseIdentity.Require(planHash, nameof(planHash));
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_BoneCount = rig.Bones.Count;
            m_LeftFootBoneIndex = rig.RequireBoneIndex(rig.LeftFootBoneId);
            m_RightFootBoneIndex = rig.RequireBoneIndex(rig.RightFootBoneId);
            m_SelectionInputs = selectionInputs ?? throw new ArgumentNullException(nameof(selectionInputs));
            m_Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            m_BlendNodes = blendNodes ?? throw new ArgumentNullException(nameof(blendNodes));
            m_Inertializations = inertializations ?? throw new ArgumentNullException(nameof(inertializations));
            m_BoneMasks = boneMasks ?? throw new ArgumentNullException(nameof(boneMasks));
            m_AdditiveReferences = additiveReferences ?? throw new ArgumentNullException(nameof(additiveReferences));
            m_ModifyBones = modifyBones ?? throw new ArgumentNullException(nameof(modifyBones));
            m_FootPlacementNodes = footPlacementNodes ?? throw new ArgumentNullException(nameof(footPlacementNodes));
            m_Operations = operations ?? throw new ArgumentNullException(nameof(operations));
            m_SourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
            m_SelectionWorkspaceCount = selectionWorkspaceCount;
            m_PoseValueWorkspaceCount = poseValueWorkspaceCount;
            m_ParameterWorkspaceCount = parameterWorkspaceCount;
            m_ContributionWorkspaceCount = contributionWorkspaceCount;
            m_FrameCacheCount = frameCacheCount;
            m_OutputOperationIndex = outputOperationIndex;
            RequireValid();
        }

        public string PoseGraphId => m_PoseGraphId ?? string.Empty;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public string PlanHash => m_PlanHash ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public int BoneCount => m_BoneCount;
        public int LeftFootBoneIndex => m_LeftFootBoneIndex;
        public int RightFootBoneIndex => m_RightFootBoneIndex;
        public IReadOnlyList<CharacterPresentationSelectionInputEntry> SelectionInputs => m_SelectionInputs ?? Array.Empty<CharacterPresentationSelectionInputEntry>();
        public IReadOnlyList<CharacterPresentationPoseParameterEntry> Parameters => m_Parameters ?? Array.Empty<CharacterPresentationPoseParameterEntry>();
        public IReadOnlyList<AnimationBlendNodePayload> BlendNodes => m_BlendNodes ?? Array.Empty<AnimationBlendNodePayload>();
        public IReadOnlyList<CharacterPresentationInertializationDescriptor> Inertializations => m_Inertializations ?? Array.Empty<CharacterPresentationInertializationDescriptor>();
        public int PlayerCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Operations.Count; i++)
                {
                    CharacterPoseOperationCode code = Operations[i].Code;
                    if (code == CharacterPoseOperationCode.SelectedPosePlayer || code == CharacterPoseOperationCode.BlendStack ||
                        code == CharacterPoseOperationCode.BlendSpacePlayer)
                        count++;
                }
                return count;
            }
        }
        public IReadOnlyList<CharacterPresentationDenseBoneMask> BoneMasks => m_BoneMasks ?? Array.Empty<CharacterPresentationDenseBoneMask>();
        public IReadOnlyList<CharacterPresentationAdditiveReferenceDescriptor> AdditiveReferences => m_AdditiveReferences ?? Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        public IReadOnlyList<CharacterPresentationModifyBoneDescriptor> ModifyBones => m_ModifyBones ?? Array.Empty<CharacterPresentationModifyBoneDescriptor>();
        public IReadOnlyList<CharacterPresentationFootPlacementNodeDescriptor> FootPlacementNodes => m_FootPlacementNodes ?? Array.Empty<CharacterPresentationFootPlacementNodeDescriptor>();
        public IReadOnlyList<CharacterPresentationPoseOperation> Operations => m_Operations ?? Array.Empty<CharacterPresentationPoseOperation>();
        public IReadOnlyList<CharacterPresentationPoseSourceMapEntry> SourceMap => m_SourceMap ?? Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        public int SelectionWorkspaceCount => m_SelectionWorkspaceCount;
        public int PoseValueWorkspaceCount => m_PoseValueWorkspaceCount;
        public int ParameterWorkspaceCount => m_ParameterWorkspaceCount;
        public int ContributionWorkspaceCount => m_ContributionWorkspaceCount;
        public int FrameCacheCount => m_FrameCacheCount;
        public int OutputOperationIndex => m_OutputOperationIndex;

        public int RequireParameterIndex(PoseParameterId parameterId)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].ParameterId.Equals(parameterId))
                    return i;
            }
            throw new InvalidOperationException($"Pose Plan has no Parameter '{parameterId}'.");
        }

        public AnimationBlendNodePayload RequireBlendNode(PoseNodeId nodeId)
        {
            AnimationBlendNodePayload result = null;
            for (int i = 0; i < BlendNodes.Count; i++)
            {
                AnimationBlendNodePayload candidate = BlendNodes[i];
                if (candidate == null || candidate.NodeId != nodeId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Pose Plan duplicates Blend Stack '{nodeId}'.");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException($"Pose Plan has no Blend Stack '{nodeId}'.");
        }

        public void RequireValid()
        {
            if (!string.Equals(m_SchemaVersion, SchemaVersion, StringComparison.Ordinal) ||
                !string.Equals(m_RuntimeAbi, RuntimeAbi, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(PoseGraphId) || string.IsNullOrEmpty(ContentRevision) || string.IsNullOrEmpty(PlanHash) ||
                string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(RigRevision) || BoneCount <= 0 ||
                SelectionInputs.Count == 0 || Operations.Count == 0 || SourceMap.Count != Operations.Count ||
                SelectionWorkspaceCount < SelectionInputs.Count || PoseValueWorkspaceCount <= 0 ||
                ParameterWorkspaceCount < Parameters.Count || ContributionWorkspaceCount <= 0 ||
                FrameCacheCount != Operations.Count || OutputOperationIndex < 0 || OutputOperationIndex >= Operations.Count)
                throw new InvalidOperationException("Character Presentation Pose Plan header or workspace is invalid.");

            var selectionNodes = new HashSet<PoseNodeId>();
            for (int i = 0; i < SelectionInputs.Count; i++)
            {
                CharacterPresentationSelectionInputEntry input = SelectionInputs[i];
                if (input == null || input.Index != i || !selectionNodes.Add(input.NodeId))
                    throw new InvalidOperationException($"Pose Plan Selection input #{i} is invalid or duplicated.");
            }
            var operationNodes = new HashSet<PoseNodeId>();
            var playerIndices = new HashSet<int>();
            var poseValueProducers = new Dictionary<int, CharacterPresentationPoseOperation>();
            int outputCount = 0;
            for (int i = 0; i < Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = Operations[i];
                CharacterPresentationPoseSourceMapEntry source = SourceMap[i];
                if (operation == null || operation.Index != i || operation.Version != CharacterPresentationPoseOperation.PayloadVersion ||
                    !operationNodes.Add(operation.NodeId) || source == null || source.OperationIndex != i || source.NodeId != operation.NodeId)
                    throw new InvalidOperationException($"Pose Plan operation #{i} or source map is invalid.");
                RequirePoseInputPhase(operation, operation.InputValueIndexA, poseValueProducers);
                RequirePoseInputPhase(operation, operation.InputValueIndexB, poseValueProducers);
                if (operation.OutputValueIndex >= 0 &&
                    ((uint)operation.OutputValueIndex >= (uint)PoseValueWorkspaceCount ||
                     !poseValueProducers.TryAdd(operation.OutputValueIndex, operation)))
                {
                    throw new InvalidOperationException($"Pose Plan operation #{i} has an invalid or duplicated Pose output value.");
                }
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != OutputOperationIndex || operation.Phase != CharacterPosePlanPhase.FinalPublication)
                        throw new InvalidOperationException("Pose Plan Output operation boundary is inconsistent.");
                }
                if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer || operation.Code == CharacterPoseOperationCode.BlendStack ||
                    operation.Code == CharacterPoseOperationCode.BlendSpacePlayer)
                {
                    if (operation.PlayerIndex < 0 || !playerIndices.Add(operation.PlayerIndex))
                        throw new InvalidOperationException($"Pose Plan Player operation #{i} has an invalid runtime index.");
                    if (operation.MarkerSyncOperationIndex >= 0 &&
                        (operation.MarkerSyncOperationIndex >= i ||
                         Operations[operation.MarkerSyncOperationIndex].Code != CharacterPoseOperationCode.MarkerSync ||
                         Operations[operation.MarkerSyncOperationIndex].SelectionInputIndex != operation.SelectionInputIndex))
                        throw new InvalidOperationException($"Pose Plan Player operation #{i} has an invalid Marker Sync input.");
                }
            }
            if (outputCount != 1 || FootPlacementNodes.Count > 1 || playerIndices.Count != PlayerCount ||
                playerIndices.Count > 0 && (playerIndices.Min() != 0 || playerIndices.Max() != playerIndices.Count - 1))
                throw new InvalidOperationException("Pose Plan requires one Output and at most one Foot Placement node.");
        }

        public void RequireInertializationValid()
        {
            int operationCount = Operations.Count(value => value.Code == CharacterPoseOperationCode.Inertialization);
            if (operationCount != Inertializations.Count)
                throw new InvalidOperationException("Pose Plan Inertialization operation and descriptor counts are inconsistent.");
            var descriptors = new HashSet<PoseNodeId>();
            for (int index = 0; index < Inertializations.Count; index++)
            {
                CharacterPresentationInertializationDescriptor descriptor = Inertializations[index];
                if (descriptor == null || descriptor.Index != index || !descriptors.Add(descriptor.NodeId) ||
                    string.IsNullOrWhiteSpace(descriptor.PolicyId) || string.IsNullOrWhiteSpace(descriptor.PolicyRevision))
                    throw new InvalidOperationException($"Pose Plan Inertialization descriptor #{index} is invalid or duplicated.");
                CharacterPresentationPoseOperation operation = Operations.SingleOrDefault(value =>
                    value.Code == CharacterPoseOperationCode.Inertialization && value.InertializationIndex == index);
                if (operation == null || operation.NodeId != descriptor.NodeId)
                    throw new InvalidOperationException($"Pose Plan Inertialization descriptor #{index} has no exact operation owner.");
                CharacterPresentationPoseOperation player = Operations.SingleOrDefault(value =>
                    value.Index < operation.Index && value.OutputValueIndex == operation.InputValueIndexA);
                if (player == null ||
                    player.Code != CharacterPoseOperationCode.SelectedPosePlayer && player.Code != CharacterPoseOperationCode.BlendSpacePlayer ||
                    player.NodeId != descriptor.InputPlayerNodeId || player.PlayerIndex != descriptor.InputPlayerIndex)
                {
                    throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' must receive Pose directly from its declared SelectedPosePlayer.");
                }
                var sources = new HashSet<int>();
                var targets = new HashSet<int>();
                var pairs = new HashSet<(int Source, int Target)>();
                for (int ruleIndex = 0; ruleIndex < descriptor.Rules.Count; ruleIndex++)
                {
                    CharacterPresentationInertializationRuleDescriptor rule = descriptor.Rules[ruleIndex];
                    if (rule == null || rule.SourceProgramProducerIndex < 0 || rule.TargetProgramProducerIndex < 0 ||
                        !Enum.IsDefined(typeof(PoseInertializationMode), rule.Mode) ||
                        !float.IsFinite(rule.DurationSeconds) || rule.DurationSeconds < 0f ||
                        rule.Mode == PoseInertializationMode.Inertialize &&
                        (rule.DurationSeconds <= 0f || rule.CurveIndex < 0 || rule.ProfileIndex < 0) ||
                        rule.Mode == PoseInertializationMode.HardCut &&
                        (rule.DurationSeconds != 0f || rule.CurveIndex != -1 || rule.ProfileIndex != -1) ||
                        rule.ParameterModes.Count != Parameters.Count ||
                        !pairs.Add((rule.SourceProgramProducerIndex, rule.TargetProgramProducerIndex)))
                    {
                        throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' exact rule #{ruleIndex} is invalid or duplicated.");
                    }
                    for (int parameter = 0; parameter < rule.ParameterModes.Count; parameter++)
                    {
                        if (!Enum.IsDefined(typeof(PoseParameterInertializationMode), rule.ParameterModes[parameter]))
                            throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' rule #{ruleIndex} parameter filter #{parameter} is invalid.");
                    }
                    sources.Add(rule.SourceProgramProducerIndex);
                    targets.Add(rule.TargetProgramProducerIndex);
                }
                if (!sources.SetEquals(targets) || pairs.Count != checked(sources.Count * sources.Count))
                    throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' does not contain one complete exact endpoint matrix.");
            }
        }

        static void RequirePoseInputPhase(
            CharacterPresentationPoseOperation operation,
            int inputValueIndex,
            IReadOnlyDictionary<int, CharacterPresentationPoseOperation> producers)
        {
            if (inputValueIndex < 0)
                return;
            if (!producers.TryGetValue(inputValueIndex, out CharacterPresentationPoseOperation producer))
                throw new InvalidOperationException($"Pose Plan operation '{operation.NodeId}' reads Pose value '{inputValueIndex}' before it is produced.");
            if (producer.Phase > operation.Phase)
                throw new InvalidOperationException($"Pose Plan operation '{operation.NodeId}' in phase '{operation.Phase}' cannot read later phase '{producer.Phase}' from '{producer.NodeId}'.");
        }
    }
}
