using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PoseInertializationTemporalOwnerKind : byte
    {
        StateMachineTransition = 1,
        DirectPlayerPolicy = 2
    }

    public enum CharacterPoseOperationCode : byte
    {
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
        BlendSpacePlayer = 15,
        TwoBoneIK = 16,
        SequencePlayer = 17,
        PoseStateMachine = 18,
        StatePoseOutput = 19,
        AnimationSlot = 20,
        ActionPlaybackInput = 21,
        RootOrientationWarp = 22,
        LocalToComponentPose = 23,
        ComponentToLocalPose = 24
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

        public CharacterPresentationModifyBoneDescriptor(
            int index,
            int boneIndex,
            int parentBoneIndex,
            CharacterModifyBonePosePayload payload)
        {
            if (index < 0 || boneIndex < 0 || parentBoneIndex < -1 || payload == null)
                throw new ArgumentException("Compiled Modify Bone descriptor is invalid.");
            m_Index = index;
            m_BoneIndex = boneIndex;
            m_ParentBoneIndex = parentBoneIndex;
            m_ReferenceSpace = payload.ReferenceSpace;
            m_Operations = payload.Operations;
            m_Position = payload.Position;
            m_Rotation = payload.Rotation;
            m_Scale = payload.Scale;
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
    public sealed class CharacterPresentationRootOrientationWarpDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_SequencePlayerIndex = -1;
        [SerializeField] int m_RootPhysicalBoneIndex = -1;
        [SerializeField] float m_Duration;
        [SerializeField] float m_TotalYaw;
        [SerializeField] AnimationCurve m_YawCurve = new AnimationCurve();

        public CharacterPresentationRootOrientationWarpDescriptor(
            int index,
            PoseNodeId nodeId,
            int sequencePlayerIndex,
            int rootPhysicalBoneIndex,
            float duration,
            float totalYaw,
            AnimationCurve yawCurve)
        {
            if (index < 0 || !nodeId.IsValid || sequencePlayerIndex < 0 ||
                rootPhysicalBoneIndex < 0 || !float.IsFinite(duration) || duration <= 0f ||
                !float.IsFinite(totalYaw) || Math.Abs(totalYaw) <= 0.001f ||
                yawCurve == null || yawCurve.length < 2)
            {
                throw new ArgumentException("Compiled Root Orientation Warp descriptor is invalid.");
            }
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_SequencePlayerIndex = sequencePlayerIndex;
            m_RootPhysicalBoneIndex = rootPhysicalBoneIndex;
            m_Duration = duration;
            m_TotalYaw = totalYaw;
            m_YawCurve = new AnimationCurve(yawCurve.keys)
            {
                preWrapMode = yawCurve.preWrapMode,
                postWrapMode = yawCurve.postWrapMode
            };
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public int SequencePlayerIndex => m_SequencePlayerIndex;
        public int RootPhysicalBoneIndex => m_RootPhysicalBoneIndex;
        public float Duration => m_Duration;
        public float TotalYaw => m_TotalYaw;
        public AnimationCurve YawCurve => m_YawCurve;

        public void RequireValid(int sequencePlayerCount, int physicalBoneCount)
        {
            if (Index < 0 || !NodeId.IsValid || SequencePlayerIndex < 0 ||
                SequencePlayerIndex >= sequencePlayerCount || RootPhysicalBoneIndex < 0 ||
                RootPhysicalBoneIndex >= physicalBoneCount || !float.IsFinite(Duration) || Duration <= 0f ||
                !float.IsFinite(TotalYaw) || Math.Abs(TotalYaw) <= 0.001f ||
                YawCurve == null || YawCurve.length < 2 ||
                Math.Abs(YawCurve.keys[0].time) > 0.0001f ||
                Math.Abs(YawCurve.keys[YawCurve.length - 1].time - Duration) > 0.0001f ||
                Math.Abs(YawCurve.Evaluate(Duration) - TotalYaw) > 0.01f)
            {
                throw new InvalidOperationException($"Root Orientation Warp descriptor #{Index} is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterPresentationTwoBoneIkDescriptor
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_RootPhysicalBoneIndex = -1;
        [SerializeField] int m_JointPhysicalBoneIndex = -1;
        [SerializeField] int m_EndPhysicalBoneIndex = -1;
        [SerializeField] int m_EffectorPoseBoneIndex = -1;
        [SerializeField] Vector3 m_EffectorLocalPositionOffset;
        [SerializeField] Quaternion m_EffectorLocalRotationOffset = Quaternion.identity;
        [SerializeField] int m_JointTargetPoseBoneIndex = -1;
        [SerializeField] Vector3 m_JointTargetLocalOffset;
        [SerializeField] CharacterTwoBoneIkEndRotationMode m_EndRotationMode;
        [SerializeField] float m_Weight;

        public CharacterPresentationTwoBoneIkDescriptor(
            PoseNodeId nodeId,
            int rootPhysicalBoneIndex,
            int jointPhysicalBoneIndex,
            int endPhysicalBoneIndex,
            int effectorPoseBoneIndex,
            Vector3 effectorLocalPositionOffset,
            Quaternion effectorLocalRotationOffset,
            int jointTargetPoseBoneIndex,
            Vector3 jointTargetLocalOffset,
            CharacterTwoBoneIkEndRotationMode endRotationMode,
            float weight)
        {
            CharacterTwoBoneIkDescriptor descriptor = new CharacterTwoBoneIkDescriptor(
                new CharacterPoseConstraintId(nodeId.Value),
                rootPhysicalBoneIndex,
                jointPhysicalBoneIndex,
                endPhysicalBoneIndex,
                effectorPoseBoneIndex,
                effectorLocalPositionOffset,
                effectorLocalRotationOffset,
                jointTargetPoseBoneIndex,
                jointTargetLocalOffset,
                endRotationMode,
                weight);
            m_NodeId = nodeId.Value;
            m_RootPhysicalBoneIndex = descriptor.RootPhysicalBoneIndex;
            m_JointPhysicalBoneIndex = descriptor.JointPhysicalBoneIndex;
            m_EndPhysicalBoneIndex = descriptor.EndPhysicalBoneIndex;
            m_EffectorPoseBoneIndex = descriptor.EffectorPoseBoneIndex;
            m_EffectorLocalPositionOffset = descriptor.EffectorLocalPositionOffset;
            m_EffectorLocalRotationOffset = descriptor.EffectorLocalRotationOffset;
            m_JointTargetPoseBoneIndex = descriptor.JointTargetPoseBoneIndex;
            m_JointTargetLocalOffset = descriptor.JointTargetLocalOffset;
            m_EndRotationMode = descriptor.EndRotationMode;
            m_Weight = descriptor.Weight;
        }

        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);

        public CharacterTwoBoneIkDescriptor ToRuntimeDescriptor() => new CharacterTwoBoneIkDescriptor(
            new CharacterPoseConstraintId(m_NodeId),
            m_RootPhysicalBoneIndex,
            m_JointPhysicalBoneIndex,
            m_EndPhysicalBoneIndex,
            m_EffectorPoseBoneIndex,
            m_EffectorLocalPositionOffset,
            m_EffectorLocalRotationOffset,
            m_JointTargetPoseBoneIndex,
            m_JointTargetLocalOffset,
            m_EndRotationMode,
            m_Weight);
    }

    [Serializable]
    public sealed class CharacterPresentationInertializationRuleDescriptor
    {
        [SerializeField] int m_SourceEndpointIndex;
        [SerializeField] int m_TargetEndpointIndex;
        [SerializeField] PoseInertializationMode m_Mode;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] int m_CurveIndex = -1;
        [SerializeField] int m_ProfileIndex = -1;
        [SerializeField] PoseParameterInertializationMode[] m_ParameterModes = Array.Empty<PoseParameterInertializationMode>();

        public CharacterPresentationInertializationRuleDescriptor(
            int sourceEndpointIndex,
            int targetEndpointIndex,
            PoseInertializationMode mode,
            float durationSeconds,
            int curveIndex,
            int profileIndex,
            PoseParameterInertializationMode[] parameterModes)
        {
            if (sourceEndpointIndex < 0 || targetEndpointIndex < 0 ||
                !Enum.IsDefined(typeof(PoseInertializationMode), mode) ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f ||
                mode == PoseInertializationMode.Inertialize &&
                (durationSeconds <= 0f || curveIndex < 0 || profileIndex < 0) ||
                mode == PoseInertializationMode.HardCut && (curveIndex != -1 || profileIndex != -1) ||
                parameterModes == null || parameterModes.Length == 0)
                throw new ArgumentException("Compiled Inertialization exact rule is invalid.");
            m_SourceEndpointIndex = sourceEndpointIndex;
            m_TargetEndpointIndex = targetEndpointIndex;
            m_Mode = mode;
            m_DurationSeconds = durationSeconds;
            m_CurveIndex = curveIndex;
            m_ProfileIndex = profileIndex;
            m_ParameterModes = parameterModes;
        }

        public int SourceEndpointIndex => m_SourceEndpointIndex;
        public int TargetEndpointIndex => m_TargetEndpointIndex;
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
        [SerializeField] PoseInertializationTemporalOwnerKind m_TemporalOwnerKind;
        [SerializeField] string m_InputOwnerNodeId = string.Empty;
        [SerializeField] int m_InputOwnerIndex;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_PolicyRevision = string.Empty;
        [SerializeField] CharacterPresentationInertializationRuleDescriptor[] m_Rules = Array.Empty<CharacterPresentationInertializationRuleDescriptor>();

        public CharacterPresentationInertializationDescriptor(
            int index,
            PoseNodeId nodeId,
            PoseInertializationTemporalOwnerKind temporalOwnerKind,
            PoseNodeId inputOwnerNodeId,
            int inputOwnerIndex,
            string policyId,
            string policyRevision,
            CharacterPresentationInertializationRuleDescriptor[] rules)
        {
            if (index < 0 || !nodeId.IsValid ||
                !Enum.IsDefined(typeof(PoseInertializationTemporalOwnerKind), temporalOwnerKind) ||
                !inputOwnerNodeId.IsValid || inputOwnerIndex < 0 ||
                string.IsNullOrWhiteSpace(policyId) || string.IsNullOrWhiteSpace(policyRevision) ||
                rules == null || rules.Length == 0)
                throw new ArgumentException("Compiled Inertialization descriptor is invalid.");
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_TemporalOwnerKind = temporalOwnerKind;
            m_InputOwnerNodeId = inputOwnerNodeId.Value;
            m_InputOwnerIndex = inputOwnerIndex;
            m_PolicyId = policyId;
            m_PolicyRevision = policyRevision;
            m_Rules = rules;
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public PoseInertializationTemporalOwnerKind TemporalOwnerKind => m_TemporalOwnerKind;
        public PoseNodeId InputOwnerNodeId => new PoseNodeId(m_InputOwnerNodeId);
        public int InputOwnerIndex => m_InputOwnerIndex;
        public string PolicyId => m_PolicyId ?? string.Empty;
        public string PolicyRevision => m_PolicyRevision ?? string.Empty;
        public IReadOnlyList<CharacterPresentationInertializationRuleDescriptor> Rules => m_Rules ?? Array.Empty<CharacterPresentationInertializationRuleDescriptor>();
    }

    [Serializable]
    public sealed class CharacterPresentationFootPlacementNodeDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] CharacterFootPlacementProfile m_Profile;
        [SerializeField] CharacterFootPlacementRigCalibration m_Calibration;
        [SerializeField] string m_CalibrationId = string.Empty;
        [SerializeField] string m_CalibrationRevision = string.Empty;

        public CharacterPresentationFootPlacementNodeDescriptor(
            int index,
            PoseNodeId nodeId,
            CharacterFootPlacementProfile profile,
            CharacterFootPlacementRigCalibration calibration)
        {
            if (index < 0 || !nodeId.IsValid || !profile || !calibration)
                throw new ArgumentException("Compiled Foot Placement node descriptor is invalid.");
            calibration.RequireValid();
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_Profile = profile;
            m_Calibration = calibration;
            m_CalibrationId = calibration.CalibrationId.Value;
            m_CalibrationRevision = calibration.ContentRevision;
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public CharacterFootPlacementProfile Profile => m_Profile;
        public CharacterFootPlacementRigCalibration Calibration => m_Calibration;
        public string CalibrationId => m_CalibrationId ?? string.Empty;
        public string CalibrationRevision => m_CalibrationRevision ?? string.Empty;

        public void RequireValid(string rigId, string rigRevision)
        {
            if (!Profile || !Calibration)
                throw new InvalidOperationException($"Foot Placement descriptor '{NodeId}' references missing authoring assets.");
            Calibration.RequireValid();
            if (!string.Equals(Calibration.RigId, rigId, StringComparison.Ordinal) ||
                !string.Equals(Calibration.RigRevision, rigRevision, StringComparison.Ordinal) ||
                !string.Equals(CalibrationId, Calibration.CalibrationId.Value, StringComparison.Ordinal) ||
                !string.Equals(CalibrationRevision, Calibration.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException($"Foot Placement descriptor '{NodeId}' identity is stale.");
        }
    }

    [Serializable]
    public sealed class CharacterPresentationPoseOperation
    {
        public const int PayloadVersion = 19;

        [SerializeField] int m_Index;
        [SerializeField] CharacterPoseExecutionDomain m_ExecutionDomain;
        [SerializeField] CharacterPoseSpace m_InputPoseSpace;
        [SerializeField] CharacterPoseSpace m_OutputPoseSpace;
        [SerializeField] CharacterPoseOperationCode m_Code;
        [SerializeField] int m_PayloadVersion = PayloadVersion;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_PresentationPoseSourceProviderId =
            string.Empty;
        [SerializeField] int m_PresentationPoseSourceIndex = -1;
        [SerializeField] int m_OutputValueIndex = -1;
        [SerializeField] int m_InputValueIndexA = -1;
        [SerializeField] int m_InputValueIndexB = -1;
        [SerializeField] int m_ControlInputOperationIndex = -1;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] AnimationSelectionAvailabilityPolicy m_SelectionAvailability;
        [SerializeField] int m_ParameterIndex = -1;
        [SerializeField] int m_ParameterIndexB = -1;
        [SerializeField] CharacterAnimationBlendSpaceInputRangePolicy m_BlendSpaceInputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        [SerializeField] int m_PlayerIndex = -1;
        [SerializeField] int m_BlendNodeIndex = -1;
        [SerializeField] int m_InertializationIndex = -1;
        [SerializeField] int m_BoneMaskIndex = -1;
        [SerializeField] int m_AdditiveReferenceIndex = -1;
        [SerializeField] int m_ModifyBoneIndex = -1;
        [SerializeField] int m_RootOrientationWarpIndex = -1;
        [SerializeField] int m_TwoBoneIkIndex = -1;
        [SerializeField] int m_FootPlacementNodeIndex = -1;
        [SerializeField] int m_SequencePlayerIndex = -1;
        [SerializeField] int m_StateMachineIndex = -1;
        [SerializeField] int m_AnimationSlotIndex = -1;
        [SerializeField] float m_Weight = 1f;
        [SerializeField] PoseParameterResolvePolicy[] m_ParameterPolicies = Array.Empty<PoseParameterResolvePolicy>();

        public CharacterPresentationPoseOperation(
            int index,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            CharacterPoseOperationCode code,
            PoseNodeId nodeId,
            PresentationPoseSourceProviderId presentationPoseSourceProviderId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            int outputValueIndex,
            int inputValueIndexA,
            int inputValueIndexB,
            int controlInputOperationIndex,
            AnimationChannelId animationChannelId,
            AnimationSelectionAvailabilityPolicy selectionAvailability,
            int parameterIndex,
            int parameterIndexB,
            CharacterAnimationBlendSpaceInputRangePolicy blendSpaceInputRangePolicy,
            int playerIndex,
            int blendNodeIndex,
            int inertializationIndex,
            int boneMaskIndex,
            int additiveReferenceIndex,
            int modifyBoneIndex,
            int rootOrientationWarpIndex,
            int twoBoneIkIndex,
            int footPlacementNodeIndex,
            int sequencePlayerIndex,
            int stateMachineIndex,
            int animationSlotIndex,
            float weight,
            PoseParameterResolvePolicy[] parameterPolicies)
        {
            if (index < 0 || !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), executionDomain) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), inputPoseSpace) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), outputPoseSpace) ||
                !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) || !nodeId.IsValid ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), blendSpaceInputRangePolicy))
                throw new ArgumentException("Compiled Pose Plan operation is invalid.");
            m_Index = index;
            m_ExecutionDomain = executionDomain;
            m_InputPoseSpace = inputPoseSpace;
            m_OutputPoseSpace = outputPoseSpace;
            m_Code = code;
            m_NodeId = nodeId.Value;
            m_PresentationPoseSourceProviderId =
                presentationPoseSourceProviderId.Value ?? string.Empty;
            m_PresentationPoseSourceIndex = presentationPoseSourceIndex.IsValid
                ? presentationPoseSourceIndex.Value
                : -1;
            m_OutputValueIndex = outputValueIndex;
            m_InputValueIndexA = inputValueIndexA;
            m_InputValueIndexB = inputValueIndexB;
            m_ControlInputOperationIndex = controlInputOperationIndex;
            m_AnimationChannelId = animationChannelId.Value ?? string.Empty;
            m_SelectionAvailability = selectionAvailability;
            m_ParameterIndex = parameterIndex;
            m_ParameterIndexB = parameterIndexB;
            m_BlendSpaceInputRangePolicy = blendSpaceInputRangePolicy;
            m_PlayerIndex = playerIndex;
            m_BlendNodeIndex = blendNodeIndex;
            m_InertializationIndex = inertializationIndex;
            m_BoneMaskIndex = boneMaskIndex;
            m_AdditiveReferenceIndex = additiveReferenceIndex;
            m_ModifyBoneIndex = modifyBoneIndex;
            m_RootOrientationWarpIndex = rootOrientationWarpIndex;
            m_TwoBoneIkIndex = twoBoneIkIndex;
            m_FootPlacementNodeIndex = footPlacementNodeIndex;
            m_SequencePlayerIndex = sequencePlayerIndex;
            m_StateMachineIndex = stateMachineIndex;
            m_AnimationSlotIndex = animationSlotIndex;
            m_Weight = weight;
            m_ParameterPolicies = parameterPolicies ?? Array.Empty<PoseParameterResolvePolicy>();
        }

        public int Index => m_Index;
        public CharacterPoseExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public CharacterPoseSpace InputPoseSpace => m_InputPoseSpace;
        public CharacterPoseSpace OutputPoseSpace => m_OutputPoseSpace;
        public CharacterPoseOperationCode Code => m_Code;
        public int Version => m_PayloadVersion;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public PresentationPoseSourceProviderId PresentationPoseSourceProviderId =>
            string.IsNullOrWhiteSpace(m_PresentationPoseSourceProviderId)
                ? default
                : new PresentationPoseSourceProviderId(
                    m_PresentationPoseSourceProviderId);
        public PresentationPoseSourceIndex PresentationPoseSourceIndex =>
            m_PresentationPoseSourceIndex < 0
                ? default
                : new PresentationPoseSourceIndex(m_PresentationPoseSourceIndex);
        public int OutputValueIndex => m_OutputValueIndex;
        public int InputValueIndexA => m_InputValueIndexA;
        public int InputValueIndexB => m_InputValueIndexB;
        public int ControlInputOperationIndex => m_ControlInputOperationIndex;
        public AnimationChannelId AnimationChannelId =>
            string.IsNullOrWhiteSpace(m_AnimationChannelId)
                ? default
                : new AnimationChannelId(m_AnimationChannelId);
        public AnimationSelectionAvailabilityPolicy SelectionAvailability =>
            m_SelectionAvailability;
        public int ParameterIndex => m_ParameterIndex;
        public int ParameterIndexB => m_ParameterIndexB;
        public CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy => m_BlendSpaceInputRangePolicy;
        public int PlayerIndex => m_PlayerIndex;
        public int BlendNodeIndex => m_BlendNodeIndex;
        public int InertializationIndex => m_InertializationIndex;
        public int BoneMaskIndex => m_BoneMaskIndex;
        public int AdditiveReferenceIndex => m_AdditiveReferenceIndex;
        public int ModifyBoneIndex => m_ModifyBoneIndex;
        public int RootOrientationWarpIndex => m_RootOrientationWarpIndex;
        public int TwoBoneIkIndex => m_TwoBoneIkIndex;
        public int FootPlacementNodeIndex => m_FootPlacementNodeIndex;
        public int SequencePlayerIndex => m_SequencePlayerIndex;
        public int StateMachineIndex => m_StateMachineIndex;
        public int AnimationSlotIndex => m_AnimationSlotIndex;
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
    public sealed class CharacterPresentationPoseStage
    {
        [SerializeField] int m_Index;
        [SerializeField] CharacterPoseExecutionDomain m_ExecutionDomain;
        [SerializeField] CharacterPoseSpace m_InputPoseSpace;
        [SerializeField] CharacterPoseSpace m_OutputPoseSpace;
        [SerializeField] int m_OperationStart;
        [SerializeField] int m_OperationCount;
        [SerializeField] int m_NativeOperationStart;
        [SerializeField] int m_NativeOperationCount;
        [SerializeField] int m_PoseWorkspaceStart;
        [SerializeField] int m_PoseWorkspaceCount;
        [SerializeField] int m_CompletionIndex;
        [SerializeField] int m_DiagnosticIndex;

        public CharacterPresentationPoseStage(
            int index,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            int operationStart,
            int operationCount,
            int nativeOperationStart,
            int nativeOperationCount,
            int poseWorkspaceStart,
            int poseWorkspaceCount)
        {
            if (index < 0 ||
                !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), executionDomain) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), inputPoseSpace) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), outputPoseSpace) ||
                operationStart < 0 || operationCount <= 0 ||
                nativeOperationStart < 0 || nativeOperationCount < 0 ||
                poseWorkspaceStart < 0 || poseWorkspaceCount < 0)
            {
                throw new ArgumentException("Compiled Pose stage is invalid.");
            }
            m_Index = index;
            m_ExecutionDomain = executionDomain;
            m_InputPoseSpace = inputPoseSpace;
            m_OutputPoseSpace = outputPoseSpace;
            m_OperationStart = operationStart;
            m_OperationCount = operationCount;
            m_NativeOperationStart = nativeOperationStart;
            m_NativeOperationCount = nativeOperationCount;
            m_PoseWorkspaceStart = poseWorkspaceStart;
            m_PoseWorkspaceCount = poseWorkspaceCount;
            m_CompletionIndex = index;
            m_DiagnosticIndex = index;
        }

        public int Index => m_Index;
        public CharacterPoseExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public CharacterPoseSpace InputPoseSpace => m_InputPoseSpace;
        public CharacterPoseSpace OutputPoseSpace => m_OutputPoseSpace;
        public int OperationStart => m_OperationStart;
        public int OperationCount => m_OperationCount;
        public int NativeOperationStart => m_NativeOperationStart;
        public int NativeOperationCount => m_NativeOperationCount;
        public int PoseWorkspaceStart => m_PoseWorkspaceStart;
        public int PoseWorkspaceCount => m_PoseWorkspaceCount;
        public int CompletionIndex => m_CompletionIndex;
        public int DiagnosticIndex => m_DiagnosticIndex;
    }

    [Serializable]
    public sealed class CharacterPresentationPosePlan
    {
        public const string SchemaVersion = "character-presentation-pose-plan/v17";
        public const string RuntimeAbi = "character-presentation-pose-runtime/v20";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] string m_RuntimeAbi = RuntimeAbi;
        [SerializeField] string m_PoseGraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] string m_PlanHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] int m_PoseBoneCount;
        [SerializeField] int m_LeftFootBoneIndex = -1;
        [SerializeField] int m_RightFootBoneIndex = -1;
        [SerializeField] CharacterPresentationPoseParameterEntry[] m_Parameters = Array.Empty<CharacterPresentationPoseParameterEntry>();
        [SerializeField] AnimationBlendNodePayload[] m_BlendNodes = Array.Empty<AnimationBlendNodePayload>();
        [SerializeField] CharacterPresentationInertializationDescriptor[] m_Inertializations = Array.Empty<CharacterPresentationInertializationDescriptor>();
        [SerializeField] CharacterPresentationDenseBoneMask[] m_BoneMasks = Array.Empty<CharacterPresentationDenseBoneMask>();
        [SerializeField] CharacterPresentationAdditiveReferenceDescriptor[] m_AdditiveReferences = Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        [SerializeField] CharacterPresentationModifyBoneDescriptor[] m_ModifyBones = Array.Empty<CharacterPresentationModifyBoneDescriptor>();
        [SerializeField] CharacterPresentationRootOrientationWarpDescriptor[] m_RootOrientationWarps = Array.Empty<CharacterPresentationRootOrientationWarpDescriptor>();
        [SerializeField] CharacterPresentationTwoBoneIkDescriptor[] m_TwoBoneIks = Array.Empty<CharacterPresentationTwoBoneIkDescriptor>();
        [SerializeField] CharacterPresentationFootPlacementNodeDescriptor[] m_FootPlacementNodes = Array.Empty<CharacterPresentationFootPlacementNodeDescriptor>();
        [SerializeField] CharacterPresentationSequencePlayerDescriptor[] m_SequencePlayers = Array.Empty<CharacterPresentationSequencePlayerDescriptor>();
        [SerializeField] CharacterPoseStateMachineDescriptor[] m_StateMachines =
            Array.Empty<CharacterPoseStateMachineDescriptor>();
        [SerializeField] CharacterAnimationSlotDescriptor[] m_AnimationSlots =
            Array.Empty<CharacterAnimationSlotDescriptor>();
        [SerializeField] ActionPlaybackInputPlan[] m_ActionPlaybackInputs =
            Array.Empty<ActionPlaybackInputPlan>();
        [SerializeField] CharacterPresentationPoseOperation[] m_Operations = Array.Empty<CharacterPresentationPoseOperation>();
        [SerializeField] CharacterPresentationPoseSourceMapEntry[] m_SourceMap = Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        [SerializeField] CharacterPresentationPoseStage[] m_Stages = Array.Empty<CharacterPresentationPoseStage>();
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
            CharacterPresentationPoseParameterEntry[] parameters,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationInertializationDescriptor[] inertializations,
            CharacterPresentationDenseBoneMask[] boneMasks,
            CharacterPresentationAdditiveReferenceDescriptor[] additiveReferences,
            CharacterPresentationModifyBoneDescriptor[] modifyBones,
            CharacterPresentationRootOrientationWarpDescriptor[] rootOrientationWarps,
            CharacterPresentationTwoBoneIkDescriptor[] twoBoneIks,
            CharacterPresentationFootPlacementNodeDescriptor[] footPlacementNodes,
            CharacterPresentationSequencePlayerDescriptor[] sequencePlayers,
            CharacterPoseStateMachineDescriptor[] stateMachines,
            CharacterAnimationSlotDescriptor[] animationSlots,
            ActionPlaybackInputPlan[] actionPlaybackInputs,
            CharacterPresentationPoseOperation[] operations,
            CharacterPresentationPoseSourceMapEntry[] sourceMap,
            CharacterPresentationPoseStage[] stages,
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
            m_PoseBoneCount = rig.PoseBoneCount;
            m_LeftFootBoneIndex = rig.RequirePhysicalBoneIndex(rig.LeftLeg.AnkleBoneId);
            m_RightFootBoneIndex = rig.RequirePhysicalBoneIndex(rig.RightLeg.AnkleBoneId);
            m_Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            m_BlendNodes = blendNodes ?? throw new ArgumentNullException(nameof(blendNodes));
            m_Inertializations = inertializations ?? throw new ArgumentNullException(nameof(inertializations));
            m_BoneMasks = boneMasks ?? throw new ArgumentNullException(nameof(boneMasks));
            m_AdditiveReferences = additiveReferences ?? throw new ArgumentNullException(nameof(additiveReferences));
            m_ModifyBones = modifyBones ?? throw new ArgumentNullException(nameof(modifyBones));
            m_RootOrientationWarps = rootOrientationWarps ?? throw new ArgumentNullException(nameof(rootOrientationWarps));
            m_TwoBoneIks = twoBoneIks ?? throw new ArgumentNullException(nameof(twoBoneIks));
            m_FootPlacementNodes = footPlacementNodes ?? throw new ArgumentNullException(nameof(footPlacementNodes));
            m_SequencePlayers = sequencePlayers ?? throw new ArgumentNullException(nameof(sequencePlayers));
            m_StateMachines = stateMachines ?? throw new ArgumentNullException(nameof(stateMachines));
            m_AnimationSlots = animationSlots ?? throw new ArgumentNullException(nameof(animationSlots));
            m_ActionPlaybackInputs = actionPlaybackInputs ??
                throw new ArgumentNullException(nameof(actionPlaybackInputs));
            m_Operations = operations ?? throw new ArgumentNullException(nameof(operations));
            m_SourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
            m_Stages = stages ?? throw new ArgumentNullException(nameof(stages));
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
        public int PoseBoneCount => m_PoseBoneCount;
        public int LeftFootBoneIndex => m_LeftFootBoneIndex;
        public int RightFootBoneIndex => m_RightFootBoneIndex;
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
                        code == CharacterPoseOperationCode.BlendSpacePlayer || code == CharacterPoseOperationCode.SequencePlayer ||
                        code == CharacterPoseOperationCode.AnimationSlot)
                        count++;
                }
                return count;
            }
        }
        public IReadOnlyList<CharacterPresentationDenseBoneMask> BoneMasks => m_BoneMasks ?? Array.Empty<CharacterPresentationDenseBoneMask>();
        public IReadOnlyList<CharacterPresentationAdditiveReferenceDescriptor> AdditiveReferences => m_AdditiveReferences ?? Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        public IReadOnlyList<CharacterPresentationModifyBoneDescriptor> ModifyBones => m_ModifyBones ?? Array.Empty<CharacterPresentationModifyBoneDescriptor>();
        public IReadOnlyList<CharacterPresentationRootOrientationWarpDescriptor> RootOrientationWarps => m_RootOrientationWarps ?? Array.Empty<CharacterPresentationRootOrientationWarpDescriptor>();
        public IReadOnlyList<CharacterPresentationTwoBoneIkDescriptor> TwoBoneIks => m_TwoBoneIks ?? Array.Empty<CharacterPresentationTwoBoneIkDescriptor>();
        public IReadOnlyList<CharacterPresentationFootPlacementNodeDescriptor> FootPlacementNodes => m_FootPlacementNodes ?? Array.Empty<CharacterPresentationFootPlacementNodeDescriptor>();
        public IReadOnlyList<CharacterPresentationSequencePlayerDescriptor> SequencePlayers => m_SequencePlayers ?? Array.Empty<CharacterPresentationSequencePlayerDescriptor>();
        public IReadOnlyList<CharacterPoseStateMachineDescriptor> StateMachines =>
            m_StateMachines ?? Array.Empty<CharacterPoseStateMachineDescriptor>();
        public IReadOnlyList<CharacterAnimationSlotDescriptor> AnimationSlots =>
            m_AnimationSlots ?? Array.Empty<CharacterAnimationSlotDescriptor>();
        public IReadOnlyList<ActionPlaybackInputPlan> ActionPlaybackInputs =>
            m_ActionPlaybackInputs ?? Array.Empty<ActionPlaybackInputPlan>();
        public IReadOnlyList<CharacterPresentationPoseOperation> Operations => m_Operations ?? Array.Empty<CharacterPresentationPoseOperation>();
        public IReadOnlyList<CharacterPresentationPoseSourceMapEntry> SourceMap => m_SourceMap ?? Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        public IReadOnlyList<CharacterPresentationPoseStage> Stages => m_Stages ?? Array.Empty<CharacterPresentationPoseStage>();
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
                string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(RigRevision) || PoseBoneCount <= 0 ||
                Operations.Count == 0 || SourceMap.Count != Operations.Count || Stages.Count == 0 ||
                PoseValueWorkspaceCount <= 0 ||
                ParameterWorkspaceCount < Parameters.Count || ContributionWorkspaceCount <= 0 ||
                FrameCacheCount != Operations.Count || OutputOperationIndex < 0 || OutputOperationIndex >= Operations.Count)
                throw new InvalidOperationException("Character Presentation Pose Plan header or workspace is invalid.");

            var actionProducerIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ActionPlaybackInputs.Count; i++)
            {
                ActionPlaybackInputPlan input = ActionPlaybackInputs[i];
                input?.RequireValid();
                if (input == null ||
                    input.Index != i ||
                    !actionProducerIds.Add(input.ProgramProducerId) ||
                    (uint)input.SlotIndex >= (uint)AnimationSlots.Count)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Action Playback input #{i} is invalid or duplicated.");
                }
                CharacterAnimationSlotDescriptor slot = AnimationSlots[input.SlotIndex];
                if (slot.SlotId != input.SlotId ||
                    slot.NodeId != input.SlotNodeId ||
                    slot.AnimationChannelId != input.AnimationChannelId ||
                    slot.ActionPlayer.PlayerIndex != input.ActionPlayerIndex ||
                    slot.ActionPlayer.PlayerNodeId != input.ActionPlayerNodeId)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Action Playback input #{i} does not match its Slot.");
                }
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
                RequirePoseInputDependency(operation, operation.InputValueIndexA, poseValueProducers);
                RequirePoseInputDependency(operation, operation.InputValueIndexB, poseValueProducers);
                if (operation.OutputValueIndex >= 0 &&
                    ((uint)operation.OutputValueIndex >= (uint)PoseValueWorkspaceCount ||
                     !poseValueProducers.TryAdd(operation.OutputValueIndex, operation)))
                {
                    throw new InvalidOperationException($"Pose Plan operation #{i} has an invalid or duplicated Pose output value.");
                }
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != OutputOperationIndex || operation.ExecutionDomain != CharacterPoseExecutionDomain.FinalPublication ||
                        operation.InputPoseSpace != CharacterPoseSpace.Local || operation.OutputPoseSpace != CharacterPoseSpace.Local)
                        throw new InvalidOperationException("Pose Plan Output operation boundary is inconsistent.");
                }
                if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer || operation.Code == CharacterPoseOperationCode.BlendStack ||
                    operation.Code == CharacterPoseOperationCode.BlendSpacePlayer || operation.Code == CharacterPoseOperationCode.SequencePlayer ||
                    operation.Code == CharacterPoseOperationCode.AnimationSlot)
                {
                    if (operation.PlayerIndex < 0 || !playerIndices.Add(operation.PlayerIndex))
                        throw new InvalidOperationException($"Pose Plan Player operation #{i} has an invalid runtime index.");
                }
                if (operation.Code == CharacterPoseOperationCode.SequencePlayer)
                {
                    if ((uint)operation.SequencePlayerIndex >= (uint)SequencePlayers.Count)
                        throw new InvalidOperationException($"Pose Plan Sequence Player operation #{i} has no descriptor.");
                    CharacterPresentationSequencePlayerDescriptor descriptor = SequencePlayers[operation.SequencePlayerIndex];
                    descriptor?.RequireValid();
                    if (descriptor == null || descriptor.Index != operation.SequencePlayerIndex ||
                        descriptor.NodeId != operation.NodeId || descriptor.PlayerIndex != operation.PlayerIndex)
                    {
                        throw new InvalidOperationException($"Pose Plan Sequence Player operation #{i} descriptor ownership is invalid.");
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.RootOrientationWarp)
                {
                    if ((uint)operation.RootOrientationWarpIndex >=
                        (uint)RootOrientationWarps.Count)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Root Orientation Warp operation #{i} has no descriptor.");
                    }
                    CharacterPresentationRootOrientationWarpDescriptor descriptor =
                        RootOrientationWarps[operation.RootOrientationWarpIndex];
                    descriptor?.RequireValid(
                        SequencePlayers.Count,
                        PoseBoneCount);
                    if (descriptor == null ||
                        descriptor.Index != operation.RootOrientationWarpIndex ||
                        descriptor.NodeId != operation.NodeId ||
                        !poseValueProducers.TryGetValue(
                            operation.InputValueIndexA,
                            out CharacterPresentationPoseOperation rootSource) ||
                        rootSource.Code != CharacterPoseOperationCode.SequencePlayer ||
                        rootSource.SequencePlayerIndex != descriptor.SequencePlayerIndex)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Root Orientation Warp operation #{i} descriptor ownership is invalid.");
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.BlendSpacePlayer &&
                    !operation.PresentationPoseSourceIndex.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Blend Space Player operation #{i} has no Presentation Pose source index.");
                }
                if (operation.Code == CharacterPoseOperationCode.PoseStateMachine)
                {
                    if ((uint)operation.StateMachineIndex >= (uint)StateMachines.Count)
                        throw new InvalidOperationException($"Pose Plan StateMachine operation #{i} has no descriptor.");
                    CharacterPoseStateMachineDescriptor descriptor = StateMachines[operation.StateMachineIndex];
                    descriptor?.RequireValid();
                    if (descriptor == null || descriptor.Index != operation.StateMachineIndex ||
                        descriptor.NodeId != operation.NodeId)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan StateMachine operation #{i} descriptor ownership is invalid.");
                    }
                    for (int stateIndex = 0; stateIndex < descriptor.States.Count; stateIndex++)
                    {
                        CharacterPoseStateDescriptor poseState = descriptor.States[stateIndex];
                        int stateEnd = checked(poseState.OperationStart + poseState.OperationCount);
                        if (poseState.OperationStart < 0 || stateEnd > i ||
                            !poseValueProducers.TryGetValue(
                                poseState.OutputPoseValueIndex,
                                out CharacterPresentationPoseOperation stateOutput) ||
                            stateOutput.Code != CharacterPoseOperationCode.StatePoseOutput ||
                            stateOutput.Index < poseState.OperationStart || stateOutput.Index >= stateEnd)
                        {
                            throw new InvalidOperationException(
                                $"Pose Plan StateMachine '{descriptor.NodeId}' State #{stateIndex} output or operation span is invalid.");
                        }
                        for (int usageIndex = 0; usageIndex < poseState.SourceProviders.Count; usageIndex++)
                        {
                            PoseStateSourceProviderPlan usage = poseState.SourceProviders[usageIndex];
                            if (usage == null || usage.StateIndex != stateIndex ||
                                usage.OperationIndex < poseState.OperationStart ||
                                usage.OperationIndex >= stateEnd ||
                                Operations[usage.OperationIndex].PlayerIndex != usage.PlayerIndex ||
                                Operations[usage.OperationIndex].NodeId != usage.PlayerNodeId)
                            {
                                throw new InvalidOperationException(
                                    $"Pose Plan StateMachine '{descriptor.NodeId}' State #{stateIndex} source usage #{usageIndex} is invalid.");
                            }
                        }
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.AnimationSlot)
                {
                    if ((uint)operation.AnimationSlotIndex >= (uint)AnimationSlots.Count)
                        throw new InvalidOperationException($"Pose Plan Animation Slot operation #{i} has no descriptor.");
                    CharacterAnimationSlotDescriptor descriptor = AnimationSlots[operation.AnimationSlotIndex];
                    descriptor?.RequireValid();
                    if (descriptor == null || descriptor.Index != operation.AnimationSlotIndex ||
                        descriptor.NodeId != operation.NodeId ||
                        descriptor.SourceUsage.SourcePoseValueIndex != operation.InputValueIndexA ||
                        descriptor.ActionPlayer.ActionPlaybackOperationIndex != operation.ControlInputOperationIndex ||
                        descriptor.ActionPlayer.PlayerIndex != operation.PlayerIndex ||
                        descriptor.BlendStackWorkspace.BlendNodeIndex != operation.BlendNodeIndex)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot operation #{i} descriptor ownership is invalid.");
                    }
                    if ((uint)operation.ControlInputOperationIndex >= (uint)i ||
                        Operations[operation.ControlInputOperationIndex].Code != CharacterPoseOperationCode.ActionPlaybackInput ||
                        (uint)operation.BlendNodeIndex >= (uint)BlendNodes.Count)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' workspace indices are invalid.");
                    }
                    CharacterPresentationPoseOperation actionInput =
                        Operations[operation.ControlInputOperationIndex];
                    if (actionInput.AnimationChannelId != descriptor.AnimationChannelId ||
                        actionInput.SelectionAvailability != AnimationSelectionAvailabilityPolicy.AllowEmpty)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' Action Playback binding is invalid.");
                    }
                    AnimationBlendNodePayload blendNode = BlendNodes[operation.BlendNodeIndex];
                    if (blendNode == null || blendNode.NodeId != descriptor.NodeId ||
                        blendNode.StackPolicy.MaxActiveSourceEntries != descriptor.BlendStackWorkspace.Capacity)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' BlendStack workspace is invalid.");
                    }
                    if (blendNode.Transitions.Count != descriptor.RequestRoutes.Count)
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' exact route count is inconsistent.");
                    var endpoints = descriptor.Endpoints.ToDictionary(value => value.EndpointId);
                    for (int routeIndex = 0; routeIndex < descriptor.RequestRoutes.Count; routeIndex++)
                    {
                        CharacterAnimationSlotRequestRouteDescriptor route = descriptor.RequestRoutes[routeIndex];
                        CharacterAnimationSlotEndpointDescriptor sourceEndpoint = endpoints[route.SourceEndpointId];
                        CharacterAnimationSlotEndpointDescriptor targetEndpoint = endpoints[route.TargetEndpointId];
                        AnimationBlendTransitionPayload transition = blendNode.RequireTransition(
                            sourceEndpoint.ProgramProducerIndex,
                            sourceEndpoint.SourcePose
                                ? AnimationBlendTransitionEndpointKind.SourcePose
                                : AnimationBlendTransitionEndpointKind.SourceOwner,
                            targetEndpoint.ProgramProducerIndex,
                            targetEndpoint.SourcePose
                                ? AnimationBlendTransitionEndpointKind.SourcePose
                                : AnimationBlendTransitionEndpointKind.SourceOwner);
                        if (transition.BlendLogic != route.BlendLogic ||
                            transition.DurationSeconds != route.DurationSeconds ||
                            transition.CurveIndex != route.CurveIndex ||
                            transition.BlendProfileIndex != route.BlendProfileIndex)
                        {
                            throw new InvalidOperationException(
                                $"Pose Plan Animation Slot '{descriptor.NodeId}' exact route #{routeIndex} does not match its Blend Policy.");
                        }
                    }
                }
            }
            if (SequencePlayers.Count != Operations.Count(value => value.Code == CharacterPoseOperationCode.SequencePlayer) ||
                RootOrientationWarps.Count != Operations.Count(value => value.Code == CharacterPoseOperationCode.RootOrientationWarp) ||
                StateMachines.Count != Operations.Count(value => value.Code == CharacterPoseOperationCode.PoseStateMachine) ||
                AnimationSlots.Count != Operations.Count(value => value.Code == CharacterPoseOperationCode.AnimationSlot) ||
                outputCount != 1 || FootPlacementNodes.Count > 1 || playerIndices.Count != PlayerCount ||
                playerIndices.Count > 0 && (playerIndices.Min() != 0 || playerIndices.Max() != playerIndices.Count - 1))
                throw new InvalidOperationException("Pose Plan requires one Output and at most one Foot Placement node.");
            RequireStagesValid();
            for (int i = 0; i < FootPlacementNodes.Count; i++)
            {
                CharacterPresentationFootPlacementNodeDescriptor descriptor = FootPlacementNodes[i];
                if (descriptor == null || descriptor.Index != i)
                    throw new InvalidOperationException($"Foot Placement descriptor #{i} is invalid.");
                descriptor.RequireValid(RigId, RigRevision);
            }
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
                    !Enum.IsDefined(typeof(PoseInertializationTemporalOwnerKind), descriptor.TemporalOwnerKind) ||
                    !descriptor.InputOwnerNodeId.IsValid || descriptor.InputOwnerIndex < 0 ||
                    string.IsNullOrWhiteSpace(descriptor.PolicyId) || string.IsNullOrWhiteSpace(descriptor.PolicyRevision))
                    throw new InvalidOperationException($"Pose Plan Inertialization descriptor #{index} is invalid or duplicated.");
                CharacterPresentationPoseOperation operation = Operations.SingleOrDefault(value =>
                    value.Code == CharacterPoseOperationCode.Inertialization && value.InertializationIndex == index);
                if (operation == null || operation.NodeId != descriptor.NodeId)
                    throw new InvalidOperationException($"Pose Plan Inertialization descriptor #{index} has no exact operation owner.");
                CharacterPresentationPoseOperation inputOwner = Operations.SingleOrDefault(value =>
                    value.Index < operation.Index && value.OutputValueIndex == operation.InputValueIndexA);
                if (inputOwner == null || inputOwner.NodeId != descriptor.InputOwnerNodeId)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Inertialization '{operation.NodeId}' has no exact direct input owner.");
                }
                HashSet<(int Source, int Target)> expectedPairs;
                if (descriptor.TemporalOwnerKind == PoseInertializationTemporalOwnerKind.StateMachineTransition)
                {
                    if (inputOwner.Code != CharacterPoseOperationCode.PoseStateMachine ||
                        inputOwner.StateMachineIndex != descriptor.InputOwnerIndex ||
                        (uint)descriptor.InputOwnerIndex >= (uint)StateMachines.Count)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Inertialization '{operation.NodeId}' declares a mismatched StateMachine temporal owner.");
                    }
                    expectedPairs = StateMachines[descriptor.InputOwnerIndex].Transitions
                        .Where(value => value.BlendLogic == AnimationTransitionBlendLogic.Inertialization)
                        .Select(value => (value.SourceStateIndex, value.TargetStateIndex))
                        .ToHashSet();
                }
                else
                {
                    if (!IsDirectInertializationPlayer(inputOwner.Code) ||
                        !inputOwner.PresentationPoseSourceIndex.IsValid ||
                        inputOwner.PresentationPoseSourceIndex.Value != descriptor.InputOwnerIndex)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Inertialization '{operation.NodeId}' declares a mismatched direct Player temporal owner.");
                    }
                    expectedPairs = new HashSet<(int Source, int Target)>
                    {
                        (descriptor.InputOwnerIndex, descriptor.InputOwnerIndex)
                    };
                }
                var pairs = new HashSet<(int Source, int Target)>();
                for (int ruleIndex = 0; ruleIndex < descriptor.Rules.Count; ruleIndex++)
                {
                    CharacterPresentationInertializationRuleDescriptor rule = descriptor.Rules[ruleIndex];
                    if (rule == null ||
                        !expectedPairs.Contains((rule.SourceEndpointIndex, rule.TargetEndpointIndex)) ||
                        !Enum.IsDefined(typeof(PoseInertializationMode), rule.Mode) ||
                        !float.IsFinite(rule.DurationSeconds) || rule.DurationSeconds < 0f ||
                        rule.Mode == PoseInertializationMode.Inertialize &&
                        (rule.DurationSeconds <= 0f || rule.CurveIndex < 0 || rule.ProfileIndex < 0) ||
                        rule.Mode == PoseInertializationMode.HardCut &&
                        (rule.DurationSeconds != 0f || rule.CurveIndex != -1 || rule.ProfileIndex != -1) ||
                        rule.ParameterModes.Count != Parameters.Count ||
                        !pairs.Add((rule.SourceEndpointIndex, rule.TargetEndpointIndex)))
                    {
                        throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' exact rule #{ruleIndex} is invalid or duplicated.");
                    }
                    for (int parameter = 0; parameter < rule.ParameterModes.Count; parameter++)
                    {
                        if (!Enum.IsDefined(typeof(PoseParameterInertializationMode), rule.ParameterModes[parameter]))
                            throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' rule #{ruleIndex} parameter filter #{parameter} is invalid.");
                    }
                }
                if (!pairs.SetEquals(expectedPairs))
                    throw new InvalidOperationException(
                        $"Pose Plan Inertialization '{operation.NodeId}' does not contain every exact temporal-owner transition.");
            }
        }

        static bool IsDirectInertializationPlayer(CharacterPoseOperationCode code) =>
            code == CharacterPoseOperationCode.SelectedPosePlayer ||
            code == CharacterPoseOperationCode.BlendSpacePlayer ||
            code == CharacterPoseOperationCode.SequencePlayer;

        void RequireStagesValid()
        {
            int expectedOperationStart = 0;
            int expectedNativeOperationStart = 0;
            int finalStageCount = 0;
            for (int stageIndex = 0; stageIndex < Stages.Count; stageIndex++)
            {
                CharacterPresentationPoseStage stage = Stages[stageIndex];
                if (stage == null || stage.Index != stageIndex ||
                    stage.OperationStart != expectedOperationStart || stage.OperationCount <= 0 ||
                    stage.NativeOperationStart != expectedNativeOperationStart || stage.NativeOperationCount < 0 ||
                    stage.CompletionIndex != stageIndex || stage.DiagnosticIndex != stageIndex ||
                    stage.PoseWorkspaceStart < 0 || stage.PoseWorkspaceCount < 0 ||
                    stage.OperationStart > Operations.Count - stage.OperationCount)
                {
                    throw new InvalidOperationException($"Pose Plan stage #{stageIndex} layout is invalid.");
                }

                int nativeCount = 0;
                int minPoseValue = int.MaxValue;
                int maxPoseValue = -1;
                for (int operationIndex = stage.OperationStart;
                     operationIndex < stage.OperationStart + stage.OperationCount;
                     operationIndex++)
                {
                    CharacterPresentationPoseOperation operation = Operations[operationIndex];
                    if (operation.ExecutionDomain != stage.ExecutionDomain ||
                        operation.OutputPoseSpace != CharacterPoseSpace.None &&
                        operation.OutputPoseSpace != stage.OutputPoseSpace)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan stage #{stageIndex} operation #{operationIndex} domain or Pose space is inconsistent.");
                    }
                    if (CharacterPoseGraphNativeProgram.IsNativePoseOperation(operation.Code))
                        nativeCount++;
                    if (operation.OutputValueIndex < 0)
                        continue;
                    minPoseValue = Math.Min(minPoseValue, operation.OutputValueIndex);
                    maxPoseValue = Math.Max(maxPoseValue, operation.OutputValueIndex);
                }

                int poseStart = maxPoseValue < 0 ? 0 : minPoseValue;
                int poseCount = maxPoseValue < 0 ? 0 : maxPoseValue - minPoseValue + 1;
                if (nativeCount != stage.NativeOperationCount ||
                    poseStart != stage.PoseWorkspaceStart || poseCount != stage.PoseWorkspaceCount)
                {
                    throw new InvalidOperationException($"Pose Plan stage #{stageIndex} workspace layout is inconsistent.");
                }
                if (stage.ExecutionDomain == CharacterPoseExecutionDomain.FinalPublication)
                    finalStageCount++;
                expectedOperationStart += stage.OperationCount;
                expectedNativeOperationStart += stage.NativeOperationCount;
            }

            if (expectedOperationStart != Operations.Count ||
                expectedNativeOperationStart != Operations.Count(value => CharacterPoseGraphNativeProgram.IsNativePoseOperation(value.Code)) ||
                finalStageCount != 1 ||
                Stages[Stages.Count - 1].ExecutionDomain != CharacterPoseExecutionDomain.FinalPublication)
            {
                throw new InvalidOperationException("Pose Plan ordered stage table does not close the operation topology.");
            }
        }

        static void RequirePoseInputDependency(
            CharacterPresentationPoseOperation operation,
            int inputValueIndex,
            IReadOnlyDictionary<int, CharacterPresentationPoseOperation> producers)
        {
            if (inputValueIndex < 0)
                return;
            if (!producers.TryGetValue(inputValueIndex, out CharacterPresentationPoseOperation producer))
                throw new InvalidOperationException($"Pose Plan operation '{operation.NodeId}' reads Pose value '{inputValueIndex}' before it is produced.");
            if (producer.Index >= operation.Index || producer.OutputPoseSpace == CharacterPoseSpace.None ||
                operation.InputPoseSpace == CharacterPoseSpace.None || producer.OutputPoseSpace != operation.InputPoseSpace)
            {
                throw new InvalidOperationException(
                    $"Pose Plan operation '{operation.NodeId}' cannot read {producer.OutputPoseSpace} Pose from '{producer.NodeId}' as {operation.InputPoseSpace} Pose.");
            }
        }
    }
}
