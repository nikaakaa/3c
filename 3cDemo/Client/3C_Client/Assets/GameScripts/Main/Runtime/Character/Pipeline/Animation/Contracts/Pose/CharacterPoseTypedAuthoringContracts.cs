using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseDynamicPort
    {
        [SerializeField] string m_PortId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] CharacterPosePortKind m_Kind = CharacterPosePortKind.LocalPose;
        [SerializeField] CharacterPosePortDirection m_Direction = CharacterPosePortDirection.Input;
        [SerializeField] bool m_Required = true;
        [SerializeField] int m_Order;
        [SerializeField] string m_InterfacePortId = string.Empty;

        public PosePortId PortId => string.IsNullOrWhiteSpace(m_PortId) ? default : new PosePortId(m_PortId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public CharacterPosePortKind Kind => m_Kind;
        public CharacterPosePortDirection Direction => m_Direction;
        public bool Required => m_Required;
        public int Order => m_Order;
        public PoseInterfacePortId InterfacePortId => string.IsNullOrWhiteSpace(m_InterfacePortId)
            ? default
            : new PoseInterfacePortId(m_InterfacePortId);

        public CharacterPoseDynamicPort() { }

        public CharacterPoseDynamicPort(PosePortId portId, string displayName, CharacterPosePortKind kind, CharacterPosePortDirection direction, bool required, int order, PoseInterfacePortId interfacePortId = default)
        {
            if (!portId.IsValid)
                throw new ArgumentException("Dynamic Pose port identity is invalid.", nameof(portId));
            if (!Enum.IsDefined(typeof(CharacterPosePortKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(CharacterPosePortDirection), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order));
            m_PortId = portId.Value;
            m_DisplayName = displayName ?? string.Empty;
            m_Kind = kind;
            m_Direction = direction;
            m_Required = required;
            m_Order = order;
            m_InterfacePortId = interfacePortId.Value ?? string.Empty;
        }
    }

    [Serializable]
    public abstract class CharacterPoseNodePayload
    {
        public abstract CharacterPoseNodeKind Kind { get; }
    }

    [Serializable] public sealed class CharacterGraphInputPosePayload : CharacterPoseNodePayload { public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.GraphInput; }
    [Serializable] public sealed class CharacterGraphOutputPosePayload : CharacterPoseNodePayload { public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.GraphOutput; }
    [Serializable] public sealed class CharacterOutputPosePayload : CharacterPoseNodePayload { public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.OutputPose; }
    [Serializable] public sealed class CharacterLocalToComponentPosePayload : CharacterPoseNodePayload { public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.LocalToComponentPose; }
    [Serializable] public sealed class CharacterComponentToLocalPosePayload : CharacterPoseNodePayload { public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ComponentToLocalPose; }
    [Serializable]
    public sealed class CharacterActionPlaybackInputPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_AnimationChannelId = string.Empty;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ActionPlaybackInput;
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId) ? default : new AnimationChannelId(m_AnimationChannelId);
        public CharacterActionPlaybackInputPosePayload() { }
        public CharacterActionPlaybackInputPosePayload(AnimationChannelId animationChannelId) => m_AnimationChannelId = animationChannelId.IsValid ? animationChannelId.Value : throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
    }

    [Serializable]
    public sealed class CharacterProgramParameterInputPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_ParameterId = string.Empty;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ProgramParameterInput;
        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public CharacterProgramParameterInputPosePayload() { }
        public CharacterProgramParameterInputPosePayload(PoseParameterId parameterId) => m_ParameterId = parameterId.IsValid ? parameterId.Value : throw new ArgumentException("Pose Parameter identity is invalid.", nameof(parameterId));
    }

    [Serializable]
    public sealed class CharacterSelectedPosePlayerPayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterMotionMatchingPoseSourceSlot m_SourceSlot;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.SelectedPosePlayer;
        public CharacterMotionMatchingPoseSourceSlot SourceSlot => m_SourceSlot;
        public CharacterSelectedPosePlayerPayload() { }
        public CharacterSelectedPosePlayerPayload(CharacterMotionMatchingPoseSourceSlot sourceSlot) =>
            m_SourceSlot = sourceSlot ? sourceSlot : throw new ArgumentNullException(nameof(sourceSlot));
    }

    [Serializable]
    public sealed class CharacterBlendSpacePlayerPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterBlendSpacePoseSourceSlot m_SourceSlot;
        [SerializeField] CharacterAnimationBlendSpaceInputRangePolicy m_InputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.BlendSpacePlayer;
        public CharacterBlendSpacePoseSourceSlot SourceSlot => m_SourceSlot;
        public CharacterAnimationBlendSpaceInputRangePolicy InputRangePolicy => m_InputRangePolicy;
        public CharacterBlendSpacePlayerPosePayload() { }
        public CharacterBlendSpacePlayerPosePayload(CharacterBlendSpacePoseSourceSlot sourceSlot, CharacterAnimationBlendSpaceInputRangePolicy inputRangePolicy)
        {
            m_SourceSlot = sourceSlot ? sourceSlot : throw new ArgumentNullException(nameof(sourceSlot));
            m_InputRangePolicy = inputRangePolicy;
        }
    }

    public enum CharacterSequencePlayerClockSource : byte
    {
        PresentationDelta = 0
    }

    [Serializable]
    public sealed class CharacterSequencePlayerPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterSequencePoseSourceSlot m_SourceSlot;
        [SerializeField] bool m_Loop;
        [SerializeField] float m_PlayRate = 1f;
        [SerializeField] float m_InitialTime;
        [SerializeField] CharacterSequencePlayerClockSource m_ClockSource;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.SequencePlayer;
        public CharacterSequencePoseSourceSlot SourceSlot => m_SourceSlot;
        public bool Loop => m_Loop;
        public float PlayRate => m_PlayRate;
        public float InitialTime => m_InitialTime;
        public CharacterSequencePlayerClockSource ClockSource => m_ClockSource;
        public CharacterSequencePlayerPosePayload() { }
        public CharacterSequencePlayerPosePayload(
            CharacterSequencePoseSourceSlot sourceSlot,
            bool loop,
            float playRate,
            float initialTime,
            CharacterSequencePlayerClockSource clockSource)
        {
            if (!sourceSlot)
                throw new ArgumentNullException(nameof(sourceSlot));
            if (!float.IsFinite(playRate) || playRate <= 0f || !float.IsFinite(initialTime) || initialTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(playRate));
            if (!Enum.IsDefined(typeof(CharacterSequencePlayerClockSource), clockSource))
                throw new ArgumentException("Sequence clock binding is invalid.");
            m_SourceSlot = sourceSlot;
            m_Loop = loop;
            m_PlayRate = playRate;
            m_InitialTime = initialTime;
            m_ClockSource = clockSource;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateMachineNodePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseStateMachineDefinition m_StateMachine;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseStateMachine;
        public CharacterPoseStateMachineDefinition StateMachine => m_StateMachine;
        public CharacterPoseStateMachineNodePayload() { }
        public CharacterPoseStateMachineNodePayload(CharacterPoseStateMachineDefinition stateMachine) => m_StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
    }

    [Serializable]
    public sealed class CharacterAnimationSlotPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] AnimationSelectionAvailabilityPolicy m_SelectionAvailability = AnimationSelectionAvailabilityPolicy.RequireSelection;
        [SerializeField] CharacterAnimationBlendPolicy m_BlendPolicy;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.AnimationSlot;
        public AnimationSlotId SlotId => string.IsNullOrWhiteSpace(m_SlotId) ? default : new AnimationSlotId(m_SlotId);
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId) ? default : new AnimationChannelId(m_AnimationChannelId);
        public AnimationSelectionAvailabilityPolicy SelectionAvailability => m_SelectionAvailability;
        public CharacterAnimationBlendPolicy BlendPolicy => m_BlendPolicy;
        public CharacterAnimationSlotPosePayload() { }
        public CharacterAnimationSlotPosePayload(AnimationSlotId slotId, AnimationChannelId animationChannelId, AnimationSelectionAvailabilityPolicy availability, CharacterAnimationBlendPolicy blendPolicy)
        {
            m_SlotId = slotId.IsValid ? slotId.Value : throw new ArgumentException("Animation Slot identity is invalid.", nameof(slotId));
            m_AnimationChannelId = animationChannelId.IsValid ? animationChannelId.Value : throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
            m_SelectionAvailability = availability;
            m_BlendPolicy = blendPolicy;
        }
    }

    [Serializable]
    public sealed class CharacterBlendStackPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterMotionMatchingPoseSourceSlot m_SourceSlot;
        [SerializeField] CharacterAnimationBlendPolicy m_BlendPolicy;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.BlendStack;
        public CharacterMotionMatchingPoseSourceSlot SourceSlot => m_SourceSlot;
        public CharacterAnimationBlendPolicy BlendPolicy => m_BlendPolicy;
        public CharacterBlendStackPosePayload() { }
        public CharacterBlendStackPosePayload(CharacterMotionMatchingPoseSourceSlot sourceSlot, CharacterAnimationBlendPolicy blendPolicy)
        {
            m_SourceSlot = sourceSlot ? sourceSlot : throw new ArgumentNullException(nameof(sourceSlot));
            m_BlendPolicy = blendPolicy;
        }
    }

    [Serializable]
    public sealed class CharacterInertializationPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseInertializationPolicy m_Policy;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.Inertialization;
        public CharacterPoseInertializationPolicy Policy => m_Policy;
        public CharacterInertializationPosePayload() { }
        public CharacterInertializationPosePayload(CharacterPoseInertializationPolicy policy) => m_Policy = policy;
    }

    [Serializable]
    public sealed class CharacterBlendPosePayload : CharacterPoseNodePayload
    {
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.BlendPose;
        public float Weight => m_Weight;
        public CharacterBlendPosePayload() { }
        public CharacterBlendPosePayload(float weight) => m_Weight = RequireWeight(weight);
        internal static float RequireWeight(float value) => float.IsFinite(value) && value >= 0f && value <= 1f ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    [Serializable]
    public sealed class CharacterLayeredBoneBlendPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterAnimationBoneMaskAsset m_BoneMask;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.LayeredBoneBlend;
        public CharacterAnimationBoneMaskAsset BoneMask => m_BoneMask;
        public float Weight => m_Weight;
        public CharacterLayeredBoneBlendPosePayload() { }
        public CharacterLayeredBoneBlendPosePayload(CharacterAnimationBoneMaskAsset boneMask, float weight)
        {
            m_BoneMask = boneMask;
            m_Weight = CharacterBlendPosePayload.RequireWeight(weight);
        }
    }

    [Serializable]
    public sealed class CharacterAdditivePosePayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_ReferencePoseId = AnimationAdditiveReferencePoseIds.RigReference;
        [SerializeField] AdditiveReferenceSpace m_ReferenceSpace = AdditiveReferenceSpace.Local;
        [SerializeField] AdditiveScalePolicy m_ScalePolicy = AdditiveScalePolicy.Multiply;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.AdditivePose;
        public string ReferencePoseId => m_ReferencePoseId ?? string.Empty;
        public AdditiveReferenceSpace ReferenceSpace => m_ReferenceSpace;
        public AdditiveScalePolicy ScalePolicy => m_ScalePolicy;
        public float Weight => m_Weight;
        public CharacterAdditivePosePayload() { }
        public CharacterAdditivePosePayload(string referencePoseId, AdditiveReferenceSpace referenceSpace, AdditiveScalePolicy scalePolicy, float weight)
        {
            m_ReferencePoseId = PoseIdentity.Require(referencePoseId, nameof(referencePoseId));
            m_ReferenceSpace = referenceSpace;
            m_ScalePolicy = scalePolicy;
            m_Weight = CharacterBlendPosePayload.RequireWeight(weight);
        }
    }

    [Serializable]
    public sealed class CharacterPoseParameterResolvePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseParameterPolicy[] m_Policies = Array.Empty<CharacterPoseParameterPolicy>();
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseParameterResolve;
        public IReadOnlyList<CharacterPoseParameterPolicy> Policies => m_Policies ?? Array.Empty<CharacterPoseParameterPolicy>();
        public CharacterPoseParameterResolvePayload() { }
        public CharacterPoseParameterResolvePayload(CharacterPoseParameterPolicy[] policies) => m_Policies = policies ?? Array.Empty<CharacterPoseParameterPolicy>();
    }

    [Serializable]
    public sealed class CharacterModifyBonePosePayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] ModifyBoneReferenceSpace m_ReferenceSpace = ModifyBoneReferenceSpace.Local;
        [SerializeField] ModifyBoneOperationMask m_Operations;
        [SerializeField] Vector3 m_Position;
        [SerializeField] Vector3 m_RotationEuler;
        [SerializeField] Vector3 m_Scale = Vector3.one;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ModifyBone;
        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public ModifyBoneReferenceSpace ReferenceSpace => m_ReferenceSpace;
        public ModifyBoneOperationMask Operations => m_Operations;
        public Vector3 Position => m_Position;
        public Quaternion Rotation => Quaternion.Euler(m_RotationEuler);
        public Vector3 Scale => m_Scale;
        public CharacterModifyBonePosePayload() { }
        public CharacterModifyBonePosePayload(AnimationBoneId boneId, ModifyBoneReferenceSpace referenceSpace, ModifyBoneOperationMask operations, Vector3 position, Vector3 rotationEuler, Vector3 scale)
        {
            m_BoneId = boneId.IsValid ? boneId.Value : throw new ArgumentException("Animation Bone identity is invalid.", nameof(boneId));
            m_ReferenceSpace = referenceSpace;
            m_Operations = operations;
            m_Position = position;
            m_RotationEuler = rotationEuler;
            m_Scale = scale;
        }
    }

    [Serializable]
    public sealed class CharacterRootOrientationWarpPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] RootMotionCurveAsset m_YawCurve;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.RootOrientationWarp;
        public RootMotionCurveAsset YawCurve => m_YawCurve;
        public CharacterRootOrientationWarpPosePayload() { }
        public CharacterRootOrientationWarpPosePayload(RootMotionCurveAsset yawCurve) =>
            m_YawCurve = yawCurve ? yawCurve : throw new ArgumentNullException(nameof(yawCurve));
    }

    [Serializable]
    public sealed class CharacterTwoBoneIkPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_EndPhysicalBoneId = string.Empty;
        [SerializeField] string m_EffectorPoseBoneId = string.Empty;
        [SerializeField] Vector3 m_EffectorLocalPositionOffset;
        [SerializeField] Vector3 m_EffectorLocalRotationEuler;
        [SerializeField] string m_JointTargetPoseBoneId = string.Empty;
        [SerializeField] Vector3 m_JointTargetLocalOffset;
        [SerializeField] CharacterTwoBoneIkEndRotationMode m_EndRotationMode = CharacterTwoBoneIkEndRotationMode.PreserveInput;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.TwoBoneIK;
        public AnimationBoneId EndPhysicalBoneId => string.IsNullOrWhiteSpace(m_EndPhysicalBoneId) ? default : new AnimationBoneId(m_EndPhysicalBoneId);
        public AnimationBoneId EffectorPoseBoneId => string.IsNullOrWhiteSpace(m_EffectorPoseBoneId) ? default : new AnimationBoneId(m_EffectorPoseBoneId);
        public Vector3 EffectorLocalPositionOffset => m_EffectorLocalPositionOffset;
        public Quaternion EffectorLocalRotationOffset => Quaternion.Euler(m_EffectorLocalRotationEuler);
        public AnimationBoneId JointTargetPoseBoneId => string.IsNullOrWhiteSpace(m_JointTargetPoseBoneId) ? default : new AnimationBoneId(m_JointTargetPoseBoneId);
        public Vector3 JointTargetLocalOffset => m_JointTargetLocalOffset;
        public CharacterTwoBoneIkEndRotationMode EndRotationMode => m_EndRotationMode;
        public float Weight => m_Weight;
        public CharacterTwoBoneIkPosePayload() { }
        public CharacterTwoBoneIkPosePayload(AnimationBoneId endPhysicalBoneId, AnimationBoneId effectorPoseBoneId, Vector3 effectorPosition, Vector3 effectorRotation, AnimationBoneId jointTargetPoseBoneId, Vector3 jointTargetOffset, CharacterTwoBoneIkEndRotationMode endRotationMode, float weight)
        {
            m_EndPhysicalBoneId = endPhysicalBoneId.IsValid ? endPhysicalBoneId.Value : throw new ArgumentException("Two Bone IK end bone is invalid.", nameof(endPhysicalBoneId));
            m_EffectorPoseBoneId = effectorPoseBoneId.IsValid ? effectorPoseBoneId.Value : throw new ArgumentException("Two Bone IK effector bone is invalid.", nameof(effectorPoseBoneId));
            m_EffectorLocalPositionOffset = effectorPosition;
            m_EffectorLocalRotationEuler = effectorRotation;
            m_JointTargetPoseBoneId = jointTargetPoseBoneId.IsValid ? jointTargetPoseBoneId.Value : throw new ArgumentException("Two Bone IK joint target bone is invalid.", nameof(jointTargetPoseBoneId));
            m_JointTargetLocalOffset = jointTargetOffset;
            m_EndRotationMode = endRotationMode;
            m_Weight = CharacterBlendPosePayload.RequireWeight(weight);
        }
    }

    [Serializable]
    public sealed class CharacterFootPlacementPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterFootPlacementProfile m_Profile;
        [SerializeField] CharacterFootPlacementRigCalibration m_Calibration;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FootPlacement;
        public CharacterFootPlacementProfile Profile => m_Profile;
        public CharacterFootPlacementRigCalibration Calibration => m_Calibration;
        public CharacterFootPlacementPosePayload() { }
        public CharacterFootPlacementPosePayload(CharacterFootPlacementProfile profile, CharacterFootPlacementRigCalibration calibration)
        {
            m_Profile = profile;
            m_Calibration = calibration;
        }
    }

    [Serializable]
    public sealed class CharacterPoseSubgraphPayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseSubgraphReference m_Subgraph;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseSubgraph;
        public CharacterPoseSubgraphReference Subgraph => m_Subgraph;
        public CharacterPoseSubgraphPayload() { }
        public CharacterPoseSubgraphPayload(CharacterPoseSubgraphReference subgraph) => m_Subgraph = subgraph ?? throw new ArgumentNullException(nameof(subgraph));
    }

    [Serializable]
    public sealed class CharacterTypedPoseNode
    {
        public const string Schema = "character-pose-node.v1";
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeReference] CharacterPoseNodePayload m_Payload;
        [SerializeField] CharacterPoseDynamicPort[] m_DynamicPorts = Array.Empty<CharacterPoseDynamicPort>();

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public CharacterPoseNodePayload Payload => m_Payload;
        public CharacterPoseNodeKind Kind =>
            m_Payload?.Kind ??
            throw new InvalidOperationException(
                $"Pose node '{NodeId}' has no typed payload.");
        public IReadOnlyList<CharacterPoseDynamicPort> DynamicPorts => m_DynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>();

        public T RequirePayload<T>() where T : CharacterPoseNodePayload =>
            m_Payload as T ?? throw new InvalidOperationException($"Pose node '{NodeId}' does not own payload '{typeof(T).Name}'.");

        public AnimationChannelId AnimationChannelId => m_Payload switch
        {
            CharacterActionPlaybackInputPosePayload value => value.AnimationChannelId,
            CharacterAnimationSlotPosePayload value => value.AnimationChannelId,
            _ => default
        };
        public PoseParameterId ParameterId => (m_Payload as CharacterProgramParameterInputPosePayload)?.ParameterId ?? default;
        public AnimationSelectionAvailabilityPolicy SelectionAvailability => (m_Payload as CharacterAnimationSlotPosePayload)?.SelectionAvailability ?? AnimationSelectionAvailabilityPolicy.RequireSelection;
        public CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy => (m_Payload as CharacterBlendSpacePlayerPosePayload)?.InputRangePolicy ?? CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        public CharacterAnimationBlendPolicy BlendPolicy => m_Payload switch
        {
            CharacterAnimationSlotPosePayload value => value.BlendPolicy,
            CharacterBlendStackPosePayload value => value.BlendPolicy,
            _ => null
        };
        public CharacterPoseInertializationPolicy InertializationPolicy => (m_Payload as CharacterInertializationPosePayload)?.Policy;
        public CharacterAnimationBoneMaskAsset BoneMask => (m_Payload as CharacterLayeredBoneBlendPosePayload)?.BoneMask;
        public float Weight => m_Payload switch
        {
            CharacterBlendPosePayload value => value.Weight,
            CharacterLayeredBoneBlendPosePayload value => value.Weight,
            CharacterAdditivePosePayload value => value.Weight,
            CharacterTwoBoneIkPosePayload value => value.Weight,
            _ => 1f
        };
        public IReadOnlyList<CharacterPoseParameterPolicy> ParameterPolicies => (m_Payload as CharacterPoseParameterResolvePayload)?.Policies ?? Array.Empty<CharacterPoseParameterPolicy>();
        public string AdditiveReferencePoseId => (m_Payload as CharacterAdditivePosePayload)?.ReferencePoseId ?? string.Empty;
        public AdditiveReferenceSpace AdditiveReferenceSpace => (m_Payload as CharacterAdditivePosePayload)?.ReferenceSpace ?? global::ThirdPersonCharacter.Pipeline.Animation.AdditiveReferenceSpace.Local;
        public AdditiveScalePolicy AdditiveScalePolicy => (m_Payload as CharacterAdditivePosePayload)?.ScalePolicy ?? global::ThirdPersonCharacter.Pipeline.Animation.AdditiveScalePolicy.Multiply;
        public AnimationBoneId BoneId => (m_Payload as CharacterModifyBonePosePayload)?.BoneId ?? default;
        public ModifyBoneReferenceSpace ModifyBoneReferenceSpace => (m_Payload as CharacterModifyBonePosePayload)?.ReferenceSpace ?? global::ThirdPersonCharacter.Pipeline.Animation.ModifyBoneReferenceSpace.Local;
        public ModifyBoneOperationMask ModifyBoneOperations => (m_Payload as CharacterModifyBonePosePayload)?.Operations ?? ModifyBoneOperationMask.None;
        public Vector3 ModifyPosition => (m_Payload as CharacterModifyBonePosePayload)?.Position ?? Vector3.zero;
        public Quaternion ModifyRotation => (m_Payload as CharacterModifyBonePosePayload)?.Rotation ?? Quaternion.identity;
        public Vector3 ModifyScale => (m_Payload as CharacterModifyBonePosePayload)?.Scale ?? Vector3.one;
        public RootMotionCurveAsset RootOrientationYawCurve => (m_Payload as CharacterRootOrientationWarpPosePayload)?.YawCurve;
        public AnimationBoneId TwoBoneIkEndPhysicalBoneId => (m_Payload as CharacterTwoBoneIkPosePayload)?.EndPhysicalBoneId ?? default;
        public AnimationBoneId TwoBoneIkEffectorPoseBoneId => (m_Payload as CharacterTwoBoneIkPosePayload)?.EffectorPoseBoneId ?? default;
        public Vector3 TwoBoneIkEffectorLocalPositionOffset => (m_Payload as CharacterTwoBoneIkPosePayload)?.EffectorLocalPositionOffset ?? Vector3.zero;
        public Quaternion TwoBoneIkEffectorLocalRotationOffset => (m_Payload as CharacterTwoBoneIkPosePayload)?.EffectorLocalRotationOffset ?? Quaternion.identity;
        public AnimationBoneId TwoBoneIkJointTargetPoseBoneId => (m_Payload as CharacterTwoBoneIkPosePayload)?.JointTargetPoseBoneId ?? default;
        public Vector3 TwoBoneIkJointTargetLocalOffset => (m_Payload as CharacterTwoBoneIkPosePayload)?.JointTargetLocalOffset ?? Vector3.zero;
        public CharacterTwoBoneIkEndRotationMode TwoBoneIkEndRotationMode => (m_Payload as CharacterTwoBoneIkPosePayload)?.EndRotationMode ?? CharacterTwoBoneIkEndRotationMode.PreserveInput;
        public CharacterFootPlacementProfile FootPlacementProfile => (m_Payload as CharacterFootPlacementPosePayload)?.Profile;
        public CharacterFootPlacementRigCalibration FootPlacementCalibration => (m_Payload as CharacterFootPlacementPosePayload)?.Calibration;
        public CharacterPoseSubgraphReference Subgraph => (m_Payload as CharacterPoseSubgraphPayload)?.Subgraph;
        public CharacterPresentationPoseSourceSlot PresentationPoseSourceSlot => m_Payload switch
        {
            CharacterSelectedPosePlayerPayload value => value.SourceSlot,
            CharacterBlendSpacePlayerPosePayload value => value.SourceSlot,
            CharacterSequencePlayerPosePayload value => value.SourceSlot,
            CharacterBlendStackPosePayload value => value.SourceSlot,
            _ => null
        };
        public bool SequenceLoop => (m_Payload as CharacterSequencePlayerPosePayload)?.Loop ?? false;
        public float SequencePlayRate => (m_Payload as CharacterSequencePlayerPosePayload)?.PlayRate ?? 1f;
        public float SequenceInitialTime => (m_Payload as CharacterSequencePlayerPosePayload)?.InitialTime ?? 0f;
        public CharacterSequencePlayerClockSource SequenceClockSource => (m_Payload as CharacterSequencePlayerPosePayload)?.ClockSource ?? CharacterSequencePlayerClockSource.PresentationDelta;
        public CharacterPoseStateMachineDefinition PoseStateMachine => (m_Payload as CharacterPoseStateMachineNodePayload)?.StateMachine;
        public AnimationSlotId AnimationSlotId => (m_Payload as CharacterAnimationSlotPosePayload)?.SlotId ?? default;
        public string AnimationSlotRoutingOwnerId => AnimationSlotId.IsValid ? $"animation-slot/{AnimationSlotId}" : string.Empty;
        public bool AnimationSlotAllowEmpty => m_Payload is CharacterAnimationSlotPosePayload value && value.SelectionAvailability == AnimationSelectionAvailabilityPolicy.AllowEmpty;
        public int AnimationSlotBlendStackCapacity => m_Payload is CharacterAnimationSlotPosePayload value && value.BlendPolicy ? value.BlendPolicy.StackPolicy.MaxActiveSourceEntries : 0;

        public CharacterTypedPoseNode() { }
        public CharacterTypedPoseNode(PoseNodeId nodeId, string displayName, CharacterPoseNodePayload payload, CharacterPoseDynamicPort[] dynamicPorts = null)
        {
            m_NodeId = nodeId.IsValid ? nodeId.Value : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            m_DisplayName = displayName ?? string.Empty;
            m_Payload = payload ??
                        throw new ArgumentNullException(nameof(payload));
            m_DynamicPorts = dynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>();
        }
    }

    [Serializable]
    public sealed class CharacterPoseGraphLayoutEntry
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] Vector2 m_Position;
        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public Vector2 Position => m_Position;
        public CharacterPoseGraphLayoutEntry() { }
        public CharacterPoseGraphLayoutEntry(PoseNodeId nodeId, Vector2 position)
        {
            m_NodeId = nodeId.IsValid ? nodeId.Value : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            m_Position = position;
        }
    }

    [Serializable]
    public sealed class CharacterTypedPoseGraph
    {
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterPoseParameterDeclaration[] m_Parameters = Array.Empty<CharacterPoseParameterDeclaration>();
        [SerializeField] CharacterTypedPoseNode[] m_Nodes = Array.Empty<CharacterTypedPoseNode>();
        [SerializeField] CharacterPoseEdge[] m_Edges = Array.Empty<CharacterPoseEdge>();
        [SerializeField] CharacterPoseGraphLayoutEntry[] m_Layout = Array.Empty<CharacterPoseGraphLayoutEntry>();

        public PoseGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId) ? default : new PoseGraphId(m_GraphId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public IReadOnlyList<CharacterPoseParameterDeclaration> Parameters => m_Parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
        public IReadOnlyList<CharacterTypedPoseNode> Nodes => m_Nodes ?? Array.Empty<CharacterTypedPoseNode>();
        public IReadOnlyList<CharacterPoseEdge> Edges => m_Edges ?? Array.Empty<CharacterPoseEdge>();
        public IReadOnlyList<CharacterPoseGraphLayoutEntry> Layout => m_Layout ?? Array.Empty<CharacterPoseGraphLayoutEntry>();

        public CharacterTypedPoseGraph() { }

        public CharacterTypedPoseGraph(
            PoseGraphId graphId,
            string contentRevision,
            CharacterPoseParameterDeclaration[] parameters,
            CharacterTypedPoseNode[] nodes,
            CharacterPoseEdge[] edges,
            CharacterPoseGraphLayoutEntry[] layout)
        {
            m_GraphId = graphId.IsValid ? graphId.Value : throw new ArgumentException("Pose Graph identity is invalid.", nameof(graphId));
            m_ContentRevision = PoseIdentity.Require(contentRevision, nameof(contentRevision));
            m_Parameters = parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
            m_Nodes = nodes ?? Array.Empty<CharacterTypedPoseNode>();
            m_Edges = edges ?? Array.Empty<CharacterPoseEdge>();
            m_Layout = layout ?? Array.Empty<CharacterPoseGraphLayoutEntry>();
        }
    }

    public static class CharacterPoseSubgraphSignatureValidator
    {
        public static void RequireMatch(
            CharacterTypedPoseNode callSite,
            CharacterTypedPoseGraph child)
        {
            if (callSite?.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                callSite.Subgraph == null ||
                child == null ||
                callSite.Subgraph.PoseGraphId != child.GraphId)
            {
                throw new InvalidOperationException("Pose Subgraph call site or child Graph identity is invalid.");
            }

            CharacterTypedPoseNode graphInput = RequireSingleNode(
                child,
                CharacterPoseNodeKind.GraphInput);
            CharacterTypedPoseNode graphOutput = RequireSingleNode(
                child,
                CharacterPoseNodeKind.GraphOutput);
            var expected = new Dictionary<PoseInterfacePortId, SignaturePort>();
            AddChildPorts(
                child,
                graphInput,
                CharacterPosePortDirection.Output,
                CharacterPosePortDirection.Input,
                expected);
            AddChildPorts(
                child,
                graphOutput,
                CharacterPosePortDirection.Input,
                CharacterPosePortDirection.Output,
                expected);

            var actual = new HashSet<PoseInterfacePortId>();
            foreach (CharacterPoseDynamicPort port in callSite.DynamicPorts)
            {
                if (port == null || !port.InterfacePortId.IsValid ||
                    !actual.Add(port.InterfacePortId) ||
                    !expected.TryGetValue(port.InterfacePortId, out SignaturePort signature) ||
                    port.Direction != signature.Direction ||
                    port.Kind != signature.Kind ||
                    port.Required != signature.Required)
                {
                    throw new InvalidOperationException(
                        $"Pose Subgraph '{callSite.NodeId}' port '{port?.PortId}' does not match child Graph '{child.GraphId}' interface.");
                }
            }
            if (!actual.SetEquals(expected.Keys))
            {
                throw new InvalidOperationException(
                    $"Pose Subgraph '{callSite.NodeId}' does not exactly cover child Graph '{child.GraphId}' interface.");
            }
        }

        static CharacterTypedPoseNode RequireSingleNode(
            CharacterTypedPoseGraph graph,
            CharacterPoseNodeKind kind)
        {
            CharacterTypedPoseNode[] nodes = graph.Nodes
                .Where(value => value?.Kind == kind)
                .ToArray();
            if (nodes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Pose Subgraph '{graph.GraphId}' requires exactly one {kind} node.");
            }
            return nodes[0];
        }

        static void AddChildPorts(
            CharacterTypedPoseGraph graph,
            CharacterTypedPoseNode node,
            CharacterPosePortDirection childDirection,
            CharacterPosePortDirection callDirection,
            IDictionary<PoseInterfacePortId, SignaturePort> target)
        {
            foreach (CharacterPoseDynamicPort port in node.DynamicPorts)
            {
                if (port == null || port.Direction != childDirection ||
                    !port.InterfacePortId.IsValid ||
                    !target.TryAdd(
                        port.InterfacePortId,
                        new SignaturePort(callDirection, port.Kind, port.Required)))
                {
                    throw new InvalidOperationException(
                        $"Pose Subgraph '{graph.GraphId}' contains an invalid or duplicate interface port.");
                }
            }
        }

        readonly struct SignaturePort
        {
            public SignaturePort(
                CharacterPosePortDirection direction,
                CharacterPosePortKind kind,
                bool required)
            {
                Direction = direction;
                Kind = kind;
                Required = required;
            }

            public CharacterPosePortDirection Direction { get; }
            public CharacterPosePortKind Kind { get; }
            public bool Required { get; }
        }
    }
}
