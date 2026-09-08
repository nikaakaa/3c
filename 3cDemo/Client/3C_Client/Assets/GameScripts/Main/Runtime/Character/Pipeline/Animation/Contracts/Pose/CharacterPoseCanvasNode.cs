using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization.FullSerializer;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseCanvasNode : Node
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField, fsSerializeAsReference] CharacterPoseNodePayload m_Payload;
        [SerializeField] CharacterPoseDynamicPort[] m_DynamicPorts = Array.Empty<CharacterPoseDynamicPort>();

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId)
            ? default
            : new PoseNodeId(m_NodeId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public CharacterPoseNodePayload Payload => m_Payload;
        public CharacterPoseNodeKind Kind =>
            m_Payload?.Kind ??
            throw new InvalidOperationException($"Pose Canvas node '{NodeId}' has no typed payload.");
        public IReadOnlyList<CharacterPoseDynamicPort> DynamicPorts =>
            m_DynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>();

        public override string name
        {
            get => string.IsNullOrWhiteSpace(m_DisplayName) ? Kind.ToString() : m_DisplayName;
            set => m_DisplayName = value ?? string.Empty;
        }

        public override int maxInConnections => -1;
        public override int maxOutConnections => -1;
        public override Type outConnectionType => typeof(CharacterPoseCanvasConnection);
        public override bool allowAsPrime => false;
        public override bool canSelfConnect => false;
        public override Alignment2x2 commentsAlignment => Alignment2x2.Default;
        public override Alignment2x2 iconAlignment => Alignment2x2.Default;

        public T RequirePayload<T>() where T : CharacterPoseNodePayload =>
            m_Payload as T ??
            throw new InvalidOperationException(
                $"Pose Canvas node '{NodeId}' does not own payload '{typeof(T).Name}'.");

        public AnimationChannelId AnimationChannelId => m_Payload switch
        {
            CharacterActionPlaybackInputPosePayload value => value.AnimationChannelId,
            CharacterAnimationSlotPosePayload value => value.AnimationChannelId,
            _ => default
        };
        public PoseParameterId ParameterId =>
            (m_Payload as CharacterProgramParameterInputPosePayload)?.ParameterId ?? default;
        public AnimationSelectionAvailabilityPolicy SelectionAvailability =>
            (m_Payload as CharacterAnimationSlotPosePayload)?.SelectionAvailability ??
            AnimationSelectionAvailabilityPolicy.RequireSelection;
        public CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy =>
            (m_Payload as CharacterBlendSpacePlayerPosePayload)?.InputRangePolicy ??
            CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        public CharacterAnimationBlendPolicy BlendPolicy => m_Payload switch
        {
            CharacterAnimationSlotPosePayload value => value.BlendPolicy,
            CharacterBlendStackPosePayload value => value.BlendPolicy,
            _ => null
        };
        public CharacterPoseInertializationPolicy InertializationPolicy =>
            (m_Payload as CharacterInertializationPosePayload)?.Policy;
        public CharacterAnimationBoneMaskAsset BoneMask =>
            (m_Payload as CharacterLayeredBoneBlendPosePayload)?.BoneMask;
        public float Weight => m_Payload switch
        {
            CharacterBlendPosePayload value => value.Weight,
            CharacterLayeredBoneBlendPosePayload value => value.Weight,
            CharacterAdditivePosePayload value => value.Weight,
            _ => 1f
        };
        public IReadOnlyList<CharacterPoseParameterPolicy> ParameterPolicies =>
            (m_Payload as CharacterPoseParameterResolvePayload)?.Policies ??
            Array.Empty<CharacterPoseParameterPolicy>();
        public string AdditiveReferencePoseId =>
            (m_Payload as CharacterAdditivePosePayload)?.ReferencePoseId ?? string.Empty;
        public AdditiveReferenceSpace AdditiveReferenceSpace =>
            (m_Payload as CharacterAdditivePosePayload)?.ReferenceSpace ??
            global::ThirdPersonCharacter.Pipeline.Animation.AdditiveReferenceSpace.Local;
        public AdditiveScalePolicy AdditiveScalePolicy =>
            (m_Payload as CharacterAdditivePosePayload)?.ScalePolicy ??
            global::ThirdPersonCharacter.Pipeline.Animation.AdditiveScalePolicy.Multiply;
        public AnimationBoneId BoneId =>
            (m_Payload as CharacterModifyBonePosePayload)?.BoneId ?? default;
        public ModifyBoneReferenceSpace ModifyBoneReferenceSpace =>
            (m_Payload as CharacterModifyBonePosePayload)?.ReferenceSpace ??
            global::ThirdPersonCharacter.Pipeline.Animation.ModifyBoneReferenceSpace.Local;
        public ModifyBoneOperationMask ModifyBoneOperations =>
            (m_Payload as CharacterModifyBonePosePayload)?.Operations ??
            ModifyBoneOperationMask.None;
        public Vector3 ModifyPosition =>
            (m_Payload as CharacterModifyBonePosePayload)?.Position ?? Vector3.zero;
        public Quaternion ModifyRotation =>
            (m_Payload as CharacterModifyBonePosePayload)?.Rotation ?? Quaternion.identity;
        public Vector3 ModifyScale =>
            (m_Payload as CharacterModifyBonePosePayload)?.Scale ?? Vector3.one;
        public RootMotionCurveAsset RootOrientationYawCurve =>
            (m_Payload as CharacterRootOrientationWarpPosePayload)?.YawCurve;
        public IReadOnlyList<CharacterPoseBoneIkGoalBinding> PoseBoneIkGoalBindings =>
            (m_Payload as CharacterPoseBoneIkGoalsPayload)?.Bindings ??
            Array.Empty<CharacterPoseBoneIkGoalBinding>();
        public CharacterFootPlacementProfile FootPlacementProfile =>
            (m_Payload as CharacterFootPlacementPosePayload)?.Profile;
        public CharacterFootPlacementRigCalibration FootPlacementCalibration =>
            (m_Payload as CharacterFootPlacementPosePayload)?.Calibration;
        public LinkedPoseGroupId LinkedPoseGroupId =>
            (m_Payload as CharacterLinkedPoseCallPayload)?.GroupId ?? default;
        public LinkedPoseInterfaceId LinkedPoseInterfaceId =>
            (m_Payload as CharacterLinkedPoseCallPayload)?.InterfaceId ?? default;
        public LinkedPoseEntryId LinkedPoseEntryId =>
            (m_Payload as CharacterLinkedPoseCallPayload)?.EntryId ?? default;
        public CharacterPoseSubgraphReference Subgraph =>
            (m_Payload as CharacterPoseSubgraphPayload)?.Subgraph;
        public CharacterPresentationPoseSourceSlot PresentationPoseSourceSlot => m_Payload switch
        {
            CharacterSelectedPosePlayerPayload value => value.SourceSlot,
            CharacterBlendSpacePlayerPosePayload value => value.SourceSlot,
            CharacterClipPlayerPosePayload value => value.SourceSlot,
            CharacterBlendStackPosePayload value => value.SourceSlot,
            _ => null
        };
        public float ClipPlayRate =>
            (m_Payload as CharacterClipPlayerPosePayload)?.PlayRate ?? 1f;
        public float ClipInitialTime =>
            (m_Payload as CharacterClipPlayerPosePayload)?.InitialTime ?? 0f;
        public CharacterClipPlayerClockSource ClipClockSource =>
            (m_Payload as CharacterClipPlayerPosePayload)?.ClockSource ??
            CharacterClipPlayerClockSource.PresentationDelta;
        public CharacterPoseStateMachineDefinition PoseStateMachine =>
            (m_Payload as CharacterPoseStateMachineNodePayload)?.StateMachine;
        public AnimationSlotId AnimationSlotId =>
            (m_Payload as CharacterAnimationSlotPosePayload)?.SlotId ?? default;
        public string AnimationSlotRoutingOwnerId =>
            AnimationSlotId.IsValid ? $"animation-slot/{AnimationSlotId}" : string.Empty;
        public bool AnimationSlotAllowEmpty =>
            m_Payload is CharacterAnimationSlotPosePayload value &&
            value.SelectionAvailability == AnimationSelectionAvailabilityPolicy.AllowEmpty;
        public int AnimationSlotBlendStackCapacity =>
            m_Payload is CharacterAnimationSlotPosePayload value && value.BlendPolicy
                ? value.BlendPolicy.StackPolicy.MaxActiveSourceEntries
                : 0;

        public CharacterPoseCanvasNode() { }

        public CharacterPoseCanvasNode(
            PoseNodeId nodeId,
            string displayName,
            CharacterPoseNodePayload payload,
            CharacterPoseDynamicPort[] dynamicPorts = null,
            Vector2 position = default)
        {
            SetAuthoring(nodeId, displayName, payload, dynamicPorts);
            this.position = position;
        }

        internal void SetAuthoring(
            PoseNodeId nodeId,
            string displayName,
            CharacterPoseNodePayload payload,
            CharacterPoseDynamicPort[] dynamicPorts = null)
        {
            m_NodeId = nodeId.IsValid
                ? nodeId.Value
                : throw new ArgumentException("Pose Canvas node identity is invalid.", nameof(nodeId));
            m_DisplayName = displayName ?? string.Empty;
            m_Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            m_DynamicPorts = dynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>();
        }

        public CharacterPoseCanvasNode CloneAuthoring() =>
            new CharacterPoseCanvasNode(
                NodeId,
                DisplayName,
                Payload,
                DynamicPorts.ToArray(),
                position);

#if UNITY_EDITOR
        protected override void OnNodeInspectorGUI()
        {
            System.Action<CharacterPoseCanvasNode> handler =
                PoseCanvasEditorBridge.InspectorOverride;
            if (handler != null)
            {
                handler(this);
                return;
            }
            base.OnNodeInspectorGUI();
        }
#endif
    }
}
