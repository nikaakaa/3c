using System;
using System.Collections.Generic;
using System.Linq;
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
    [Serializable] public sealed class CharacterFullBodyIkGoalAssemblerPayload : CharacterPoseNodePayload { public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FullBodyIkGoalAssembler; }

    [Serializable]
    public sealed class CharacterLinkedPoseCallPayload : CharacterPoseNodePayload
    {
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] string m_EntryId = string.Empty;

        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.LinkedPoseCall;
        public LinkedPoseGroupId GroupId => string.IsNullOrWhiteSpace(m_GroupId) ? default : new LinkedPoseGroupId(m_GroupId);
        public LinkedPoseInterfaceId InterfaceId => string.IsNullOrWhiteSpace(m_InterfaceId) ? default : new LinkedPoseInterfaceId(m_InterfaceId);
        public LinkedPoseEntryId EntryId => string.IsNullOrWhiteSpace(m_EntryId) ? default : new LinkedPoseEntryId(m_EntryId);

        public CharacterLinkedPoseCallPayload() { }

        public CharacterLinkedPoseCallPayload(
            LinkedPoseGroupId groupId,
            LinkedPoseInterfaceId interfaceId,
            LinkedPoseEntryId entryId)
        {
            m_GroupId = groupId.IsValid
                ? groupId.Value
                : throw new ArgumentException("Linked Pose Group identity is invalid.", nameof(groupId));
            m_InterfaceId = interfaceId.IsValid
                ? interfaceId.Value
                : throw new ArgumentException("Linked Pose Interface identity is invalid.", nameof(interfaceId));
            m_EntryId = entryId.IsValid
                ? entryId.Value
                : throw new ArgumentException("Linked Pose Entry identity is invalid.", nameof(entryId));
        }
    }
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

    public enum CharacterClipPlayerClockSource : byte
    {
        PresentationDelta = 0,
        CommittedMovement = 1
    }

    [Serializable]
    public sealed class CharacterClipPlayerPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterClipPoseSourceSlot m_SourceSlot;
        [SerializeField] float m_PlayRate = 1f;
        [SerializeField] float m_InitialTime;
        [SerializeField] bool m_LoopAnimation = true;
        [SerializeField] CharacterClipPlayerClockSource m_ClockSource;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ClipPlayer;
        public CharacterClipPoseSourceSlot SourceSlot => m_SourceSlot;
        public float PlayRate => m_PlayRate;
        public float InitialTime => m_InitialTime;
        public bool LoopAnimation => m_LoopAnimation;
        public CharacterClipPlayerClockSource ClockSource => m_ClockSource;
        public CharacterClipPlayerPosePayload() { }
        public CharacterClipPlayerPosePayload(
            CharacterClipPoseSourceSlot sourceSlot,
            float playRate,
            float initialTime,
            bool loopAnimation,
            CharacterClipPlayerClockSource clockSource)
        {
            if (!sourceSlot)
                throw new ArgumentNullException(nameof(sourceSlot));
            if (!float.IsFinite(playRate) || playRate <= 0f || !float.IsFinite(initialTime) || initialTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(playRate));
            if (!Enum.IsDefined(typeof(CharacterClipPlayerClockSource), clockSource))
                throw new ArgumentException("Clip clock binding is invalid.");
            m_SourceSlot = sourceSlot;
            m_PlayRate = playRate;
            m_InitialTime = initialTime;
            m_LoopAnimation = loopAnimation;
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
        [SerializeField] CharacterPoseResourceSlot m_BlendPolicySlot;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.AnimationSlot;
        public AnimationSlotId SlotId => string.IsNullOrWhiteSpace(m_SlotId) ? default : new AnimationSlotId(m_SlotId);
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId) ? default : new AnimationChannelId(m_AnimationChannelId);
        public AnimationSelectionAvailabilityPolicy SelectionAvailability => m_SelectionAvailability;
        public CharacterPoseResourceSlot BlendPolicySlot => m_BlendPolicySlot;
        public CharacterAnimationSlotPosePayload() { }
        public CharacterAnimationSlotPosePayload(AnimationSlotId slotId, AnimationChannelId animationChannelId, AnimationSelectionAvailabilityPolicy availability, CharacterPoseResourceSlot blendPolicySlot)
        {
            m_SlotId = slotId.IsValid ? slotId.Value : throw new ArgumentException("Animation Slot identity is invalid.", nameof(slotId));
            m_AnimationChannelId = animationChannelId.IsValid ? animationChannelId.Value : throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
            m_SelectionAvailability = availability;
            m_BlendPolicySlot = blendPolicySlot ? blendPolicySlot : throw new ArgumentNullException(nameof(blendPolicySlot));
        }
    }

    [Serializable]
    public sealed class CharacterBlendStackPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterMotionMatchingPoseSourceSlot m_SourceSlot;
        [SerializeField] CharacterPoseResourceSlot m_BlendPolicySlot;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.BlendStack;
        public CharacterMotionMatchingPoseSourceSlot SourceSlot => m_SourceSlot;
        public CharacterPoseResourceSlot BlendPolicySlot => m_BlendPolicySlot;
        public CharacterBlendStackPosePayload() { }
        public CharacterBlendStackPosePayload(CharacterMotionMatchingPoseSourceSlot sourceSlot, CharacterPoseResourceSlot blendPolicySlot)
        {
            m_SourceSlot = sourceSlot ? sourceSlot : throw new ArgumentNullException(nameof(sourceSlot));
            m_BlendPolicySlot = blendPolicySlot ? blendPolicySlot : throw new ArgumentNullException(nameof(blendPolicySlot));
        }
    }

    [Serializable]
    public sealed class CharacterInertializationPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseResourceSlot m_PolicySlot;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.Inertialization;
        public CharacterPoseResourceSlot PolicySlot => m_PolicySlot;
        public CharacterInertializationPosePayload() { }
        public CharacterInertializationPosePayload(CharacterPoseResourceSlot policySlot) =>
            m_PolicySlot = policySlot ? policySlot : throw new ArgumentNullException(nameof(policySlot));
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
        [SerializeField] CharacterPoseResourceSlot m_BoneMaskSlot;
        [SerializeField] CharacterLayeredBoneBlendSpace m_BlendSpace = CharacterLayeredBoneBlendSpace.Local;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.LayeredBoneBlend;
        public CharacterPoseResourceSlot BoneMaskSlot => m_BoneMaskSlot;
        public CharacterLayeredBoneBlendSpace BlendSpace => m_BlendSpace;
        public float Weight => m_Weight;
        public CharacterLayeredBoneBlendPosePayload() { }
        public CharacterLayeredBoneBlendPosePayload(
            CharacterPoseResourceSlot boneMaskSlot,
            CharacterLayeredBoneBlendSpace blendSpace,
            float weight)
        {
            m_BoneMaskSlot = boneMaskSlot ? boneMaskSlot : throw new ArgumentNullException(nameof(boneMaskSlot));
            m_BlendSpace = blendSpace;
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
        [SerializeField] CharacterPoseResourceSlot m_YawCurveSlot;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.RootOrientationWarp;
        public CharacterPoseResourceSlot YawCurveSlot => m_YawCurveSlot;
        public CharacterRootOrientationWarpPosePayload() { }
        public CharacterRootOrientationWarpPosePayload(CharacterPoseResourceSlot yawCurveSlot) =>
            m_YawCurveSlot = yawCurveSlot ? yawCurveSlot : throw new ArgumentNullException(nameof(yawCurveSlot));
    }

    [Serializable]
    public sealed class CharacterPoseBoneIkGoalBinding
    {
        [SerializeField] CharacterFullBodyIkEffectorSlot m_EffectorSlot;
        [SerializeField] string m_TargetPoseBoneId = string.Empty;
        [SerializeField] Vector3 m_PositionOffset;
        [SerializeField] Vector3 m_RotationOffsetEuler;
        [SerializeField, Range(0f, 1f)] float m_PositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] float m_RotationWeight = 1f;

        public CharacterFullBodyIkEffectorSlot EffectorSlot => m_EffectorSlot;
        public AnimationBoneId TargetPoseBoneId => string.IsNullOrWhiteSpace(m_TargetPoseBoneId)
            ? default
            : new AnimationBoneId(m_TargetPoseBoneId);
        public Vector3 PositionOffset => m_PositionOffset;
        public Quaternion RotationOffset => Quaternion.Euler(m_RotationOffsetEuler);
        public float PositionWeight => m_PositionWeight;
        public float RotationWeight => m_RotationWeight;

        public CharacterPoseBoneIkGoalBinding() { }

        public CharacterPoseBoneIkGoalBinding(
            CharacterFullBodyIkEffectorSlot effectorSlot,
            AnimationBoneId targetPoseBoneId,
            Vector3 positionOffset,
            Vector3 rotationOffsetEuler,
            float positionWeight,
            float rotationWeight)
        {
            if (effectorSlot < CharacterFullBodyIkEffectorSlot.Body ||
                effectorSlot > CharacterFullBodyIkEffectorSlot.RightFoot)
            {
                throw new ArgumentOutOfRangeException(nameof(effectorSlot));
            }
            m_EffectorSlot = effectorSlot;
            m_TargetPoseBoneId = targetPoseBoneId.IsValid
                ? targetPoseBoneId.Value
                : throw new ArgumentException("Pose Bone IK Goal target is invalid.", nameof(targetPoseBoneId));
            m_PositionOffset = positionOffset;
            m_RotationOffsetEuler = rotationOffsetEuler;
            m_PositionWeight = CharacterBlendPosePayload.RequireWeight(positionWeight);
            m_RotationWeight = CharacterBlendPosePayload.RequireWeight(rotationWeight);
        }
    }

    [Serializable]
    public sealed class CharacterPoseBoneIkGoalsPayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseBoneIkGoalBinding[] m_Bindings = Array.Empty<CharacterPoseBoneIkGoalBinding>();
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseBoneIKGoals;
        public IReadOnlyList<CharacterPoseBoneIkGoalBinding> Bindings => m_Bindings ?? Array.Empty<CharacterPoseBoneIkGoalBinding>();
        public CharacterPoseBoneIkGoalsPayload() { }
        public CharacterPoseBoneIkGoalsPayload(CharacterPoseBoneIkGoalBinding[] bindings) =>
            m_Bindings = bindings ?? Array.Empty<CharacterPoseBoneIkGoalBinding>();
    }

    [Serializable]
    public sealed class CharacterFootPlacementPosePayload : CharacterPoseNodePayload
    {
        [SerializeField] CharacterPoseResourceSlot m_ProfileSlot;
        [SerializeField] CharacterPoseResourceSlot m_CalibrationSlot;
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FootPlacement;
        public CharacterPoseResourceSlot ProfileSlot => m_ProfileSlot;
        public CharacterPoseResourceSlot CalibrationSlot => m_CalibrationSlot;
        public CharacterFootPlacementPosePayload() { }
        public CharacterFootPlacementPosePayload(
            CharacterPoseResourceSlot profileSlot,
            CharacterPoseResourceSlot calibrationSlot)
        {
            m_ProfileSlot = profileSlot ? profileSlot : throw new ArgumentNullException(nameof(profileSlot));
            m_CalibrationSlot = calibrationSlot ? calibrationSlot : throw new ArgumentNullException(nameof(calibrationSlot));
        }
    }

    [Serializable]
    public sealed class CharacterFullBodyIkPosePayload : CharacterPoseNodePayload
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FullBodyIK;
        public CharacterFullBodyIkPosePayload() { }
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

    public static class CharacterLinkedPosePortProjection
    {
        public static CharacterPoseDynamicPort[] CreateCallPorts(
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            LinkedPoseEntryId entryId)
        {
            CharacterLinkedPoseInterfaceEntryDescriptor entry = RequireEntry(
                linkedInterface,
                entryId);
            return CreatePorts(entry, null, false);
        }

        public static CharacterPoseDynamicPort[] CreateGraphInputPorts(
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            LinkedPoseEntryId entryId)
        {
            CharacterLinkedPoseInterfaceEntryDescriptor entry = RequireEntry(
                linkedInterface,
                entryId);
            return CreatePorts(
                entry,
                CharacterPosePortDirection.Input,
                true);
        }

        public static CharacterPoseDynamicPort[] CreateGraphOutputPorts(
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            LinkedPoseEntryId entryId)
        {
            CharacterLinkedPoseInterfaceEntryDescriptor entry = RequireEntry(
                linkedInterface,
                entryId);
            return CreatePorts(
                entry,
                CharacterPosePortDirection.Output,
                true);
        }

        public static void RequireCallMatch(
            CharacterPoseCanvasNode call,
            CharacterLinkedPoseInterfaceAsset linkedInterface)
        {
            if (call?.Kind != CharacterPoseNodeKind.LinkedPoseCall ||
                !call.LinkedPoseGroupId.IsValid ||
                !call.LinkedPoseInterfaceId.IsValid ||
                !call.LinkedPoseEntryId.IsValid ||
                !linkedInterface ||
                call.LinkedPoseInterfaceId != linkedInterface.InterfaceId)
            {
                throw new InvalidOperationException(
                    "Linked Pose Call identity does not match its Interface.");
            }
            CharacterLinkedPoseInterfaceEntryDescriptor entry =
                RequireEntry(linkedInterface, call.LinkedPoseEntryId);
            RequirePorts(
                call.DynamicPorts,
                CreatePorts(entry, null, false),
                $"Linked Pose Call '{call.NodeId}'");
        }

        public static void RequireEntryGraphMatch(
            CharacterPoseCanvasGraph graph,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            LinkedPoseEntryId entryId)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            CharacterLinkedPoseInterfaceEntryDescriptor entry = RequireEntry(
                linkedInterface,
                entryId);
            CharacterPoseCanvasNode graphInput = RequireSingleNode(
                graph,
                CharacterPoseNodeKind.GraphInput);
            CharacterPoseCanvasNode graphOutput = RequireSingleNode(
                graph,
                CharacterPoseNodeKind.GraphOutput);
            RequirePorts(
                graphInput.DynamicPorts,
                CreatePorts(
                    entry,
                    CharacterPosePortDirection.Input,
                    true),
                $"Linked Pose Entry '{entryId}' Graph Input");
            RequirePorts(
                graphOutput.DynamicPorts,
                CreatePorts(
                    entry,
                    CharacterPosePortDirection.Output,
                    true),
                $"Linked Pose Entry '{entryId}' Graph Output");
        }

        static CharacterLinkedPoseInterfaceEntryDescriptor RequireEntry(
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            LinkedPoseEntryId entryId)
        {
            if (!linkedInterface)
                throw new ArgumentNullException(nameof(linkedInterface));
            linkedInterface.RequireValid();
            return linkedInterface.RequireEntry(entryId);
        }

        static CharacterPoseDynamicPort[] CreatePorts(
            CharacterLinkedPoseInterfaceEntryDescriptor entry,
            CharacterPosePortDirection? interfaceDirection,
            bool reverseDirection)
        {
            entry.RequireValid();
            return entry.Ports
                .Where(port =>
                    !interfaceDirection.HasValue ||
                    port.Direction == interfaceDirection.Value)
                .OrderBy(port => port.Order)
                .Select(port => new CharacterPoseDynamicPort(
                    new PosePortId(port.PortId.Value),
                    port.PortId.Value,
                    port.Kind,
                    reverseDirection
                        ? Reverse(port.Direction)
                        : port.Direction,
                    port.Required,
                    port.Order,
                    port.PortId))
                .ToArray();
        }

        static CharacterPosePortDirection Reverse(
            CharacterPosePortDirection direction) =>
            direction == CharacterPosePortDirection.Input
                ? CharacterPosePortDirection.Output
                : CharacterPosePortDirection.Input;

        static void RequirePorts(
            IReadOnlyList<CharacterPoseDynamicPort> actual,
            IReadOnlyList<CharacterPoseDynamicPort> expected,
            string owner)
        {
            CharacterPoseDynamicPort[] ordered = (actual ??
                    Array.Empty<CharacterPoseDynamicPort>())
                .OrderBy(port => port?.Order ?? int.MaxValue)
                .ToArray();
            if (ordered.Length != expected.Count)
                throw new InvalidOperationException(
                    $"{owner} does not exactly cover its Interface ports.");
            var ids = new HashSet<PosePortId>();
            for (int i = 0; i < ordered.Length; i++)
            {
                CharacterPoseDynamicPort value = ordered[i];
                CharacterPoseDynamicPort contract = expected[i];
                if (value == null ||
                    !ids.Add(value.PortId) ||
                    !value.PortId.Equals(contract.PortId) ||
                    value.InterfacePortId != contract.InterfacePortId ||
                    value.Direction != contract.Direction ||
                    value.Kind != contract.Kind ||
                    value.Required != contract.Required ||
                    value.Order != contract.Order)
                {
                    throw new InvalidOperationException(
                        $"{owner} port #{i} does not match its Interface contract.");
                }
            }
        }

        static CharacterPoseCanvasNode RequireSingleNode(
            CharacterPoseCanvasGraph graph,
            CharacterPoseNodeKind kind)
        {
            CharacterPoseCanvasNode[] nodes = graph.Nodes
                .Where(value => value?.Kind == kind)
                .ToArray();
            if (nodes.Length != 1)
                throw new InvalidOperationException(
                    $"Linked Pose Entry Graph '{graph.GraphId}' requires exactly one {kind} node.");
            return nodes[0];
        }
    }

    public static class CharacterPoseSubgraphSignatureValidator
    {
        public static void RequireMatch(
            CharacterPoseCanvasNode callSite,
            CharacterPoseCanvasGraph child)
        {
            if (callSite?.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                callSite.Subgraph == null ||
                child == null ||
                callSite.Subgraph.PoseGraphId != child.GraphId)
            {
                throw new InvalidOperationException("Pose Subgraph call site or child Graph identity is invalid.");
            }

            CharacterPoseCanvasNode graphInput = RequireSingleNode(
                child,
                CharacterPoseNodeKind.GraphInput);
            CharacterPoseCanvasNode graphOutput = RequireSingleNode(
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

        static CharacterPoseCanvasNode RequireSingleNode(
            CharacterPoseCanvasGraph graph,
            CharacterPoseNodeKind kind)
        {
            CharacterPoseCanvasNode[] nodes = graph.Nodes
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
            CharacterPoseCanvasGraph graph,
            CharacterPoseCanvasNode node,
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
