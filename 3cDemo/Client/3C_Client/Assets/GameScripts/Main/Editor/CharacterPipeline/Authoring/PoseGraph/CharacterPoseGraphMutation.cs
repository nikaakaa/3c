using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public enum CharacterPresentationMutationKind : byte
    {
        CreatePoseNode = 1,
        DeletePoseNode = 2,
        SetPoseNodeField = 3,
        AddDynamicPosePort = 4,
        RemoveDynamicPosePort = 5,
        ConnectPosePort = 6,
        DisconnectPosePort = 7,
        MovePoseNode = 8,
        CreatePoseStateMachine = 9,
        CreatePoseState = 10,
        DeletePoseState = 11,
        CreatePoseTransition = 12,
        DeletePoseTransition = 13,
        SetPoseTransitionField = 14,
        SetProfileSourceBinding = 15,
        SetProfilePolicy = 16,
        SetAnimationSlotBinding = 17,
        SetPresentationGraph = 18,
        SetMotionMatchingProfile = 19,
        SetFootPlacementAnalysis = 20,
        CreatePoseGraph = 21,
        DeletePoseGraph = 22,
        SetPoseGraphParameters = 23,
        SetPoseNodeName = 24,
        ConfigurePoseStateMachine = 25,
        RemoveProfileSourceBinding = 26,
        SetProfileProducerBinding = 27,
        RemoveProfileProducerBinding = 28,
        SetPoseStateMachineLayoutElement = 29,
        RemovePoseStateMachineLayoutElement = 30,
        CreatePoseSourceSlot = 31,
        RenamePoseSourceSlot = 32,
        DeletePoseSourceSlot = 33,
        CreateProfileSourceBinding = 34,
        RenameProfileSourceBinding = 35,
        SetPoseStateField = 36,
        CreateLinkedPoseImplementation = 37,
        ConfigureLinkedPoseImplementation = 38,
        RemoveLinkedPoseImplementation = 39,
        SetLinkedPoseGroup = 40,
        RemoveLinkedPoseGroup = 41,
        CreateEquipmentLinkedPoseSelector = 42,
        ConfigureEquipmentLinkedPoseSelector = 43,
        RemoveLinkedPoseSelector = 44,
        SetEquipmentLinkedPoseMapping = 45,
        RemoveEquipmentLinkedPoseMapping = 46,
        CreateLinkedPoseInterface = 47,
        ConfigureLinkedPoseInterface = 48,
        RemoveLinkedPoseInterface = 49,
        ConfigureLinkedPoseCall = 50
    }

    public abstract class CharacterPresentationMutation
    {
        protected CharacterPresentationMutation(CharacterPresentationMutationKind kind, string ownerId)
        {
            Kind = kind;
            OwnerId = RequireIdentity(ownerId, nameof(ownerId));
        }

        public CharacterPresentationMutationKind Kind { get; }
        public string OwnerId { get; }

        protected static string RequireIdentity(string value, string parameterName) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Presentation mutation identity is missing.", parameterName) : value.Trim();
    }

    public sealed class CreatePoseNodeMutation : CharacterPresentationMutation
    {
        public CreatePoseNodeMutation(string graphId, CharacterTypedPoseNode node, Vector2 position)
            : base(CharacterPresentationMutationKind.CreatePoseNode, graphId)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Position = position;
        }
        public CharacterTypedPoseNode Node { get; }
        public Vector2 Position { get; }
    }

    public sealed class DeletePoseNodeMutation : CharacterPresentationMutation
    {
        public DeletePoseNodeMutation(string graphId, PoseNodeId nodeId) : base(CharacterPresentationMutationKind.DeletePoseNode, graphId) => NodeId = nodeId.IsValid ? nodeId : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
        public PoseNodeId NodeId { get; }
    }

    public sealed class SetPoseNodeFieldMutation : CharacterPresentationMutation
    {
        public SetPoseNodeFieldMutation(string graphId, PoseNodeId nodeId, string fieldId, object value) : base(CharacterPresentationMutationKind.SetPoseNodeField, graphId)
        {
            NodeId = nodeId.IsValid ? nodeId : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            FieldId = RequireIdentity(fieldId, nameof(fieldId));
            Value = value;
        }
        public PoseNodeId NodeId { get; }
        public string FieldId { get; }
        public object Value { get; }
    }

    public sealed class AddDynamicPosePortMutation : CharacterPresentationMutation
    {
        public AddDynamicPosePortMutation(string graphId, PoseNodeId nodeId, CharacterPoseDynamicPort port) : base(CharacterPresentationMutationKind.AddDynamicPosePort, graphId)
        {
            NodeId = nodeId.IsValid ? nodeId : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            Port = port ?? throw new ArgumentNullException(nameof(port));
        }
        public PoseNodeId NodeId { get; }
        public CharacterPoseDynamicPort Port { get; }
    }

    public sealed class RemoveDynamicPosePortMutation : CharacterPresentationMutation
    {
        public RemoveDynamicPosePortMutation(string graphId, PoseNodeId nodeId, PosePortId portId) : base(CharacterPresentationMutationKind.RemoveDynamicPosePort, graphId)
        {
            NodeId = nodeId.IsValid ? nodeId : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            PortId = portId.IsValid ? portId : throw new ArgumentException("Pose Port identity is invalid.", nameof(portId));
        }
        public PoseNodeId NodeId { get; }
        public PosePortId PortId { get; }
    }

    public sealed class ConnectPosePortMutation : CharacterPresentationMutation
    {
        public ConnectPosePortMutation(string graphId, string edgeId, PoseNodeId sourceNodeId, PosePortId sourcePortId, PoseNodeId targetNodeId, PosePortId targetPortId) : base(CharacterPresentationMutationKind.ConnectPosePort, graphId)
        {
            EdgeId = RequireIdentity(edgeId, nameof(edgeId));
            SourceNodeId = sourceNodeId.IsValid ? sourceNodeId : throw new ArgumentException("Source node identity is invalid.", nameof(sourceNodeId));
            SourcePortId = sourcePortId.IsValid ? sourcePortId : throw new ArgumentException("Source port identity is invalid.", nameof(sourcePortId));
            TargetNodeId = targetNodeId.IsValid ? targetNodeId : throw new ArgumentException("Target node identity is invalid.", nameof(targetNodeId));
            TargetPortId = targetPortId.IsValid ? targetPortId : throw new ArgumentException("Target port identity is invalid.", nameof(targetPortId));
        }
        public string EdgeId { get; }
        public PoseNodeId SourceNodeId { get; }
        public PosePortId SourcePortId { get; }
        public PoseNodeId TargetNodeId { get; }
        public PosePortId TargetPortId { get; }
    }

    public sealed class DisconnectPosePortMutation : CharacterPresentationMutation
    {
        public DisconnectPosePortMutation(string graphId, string edgeId) : base(CharacterPresentationMutationKind.DisconnectPosePort, graphId) => EdgeId = RequireIdentity(edgeId, nameof(edgeId));
        public string EdgeId { get; }
    }

    public sealed class MovePoseNodeMutation : CharacterPresentationMutation
    {
        public MovePoseNodeMutation(string graphId, PoseNodeId nodeId, Vector2 position) : base(CharacterPresentationMutationKind.MovePoseNode, graphId)
        {
            NodeId = nodeId.IsValid ? nodeId : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            Position = position;
        }
        public PoseNodeId NodeId { get; }
        public Vector2 Position { get; }
    }

    public sealed class CreatePoseGraphMutation : CharacterPresentationMutation
    {
        public CreatePoseGraphMutation(
            string graphAssetId,
            CharacterTypedPoseGraph graph)
            : base(CharacterPresentationMutationKind.CreatePoseGraph, graphAssetId)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public CharacterTypedPoseGraph Graph { get; }
    }

    public sealed class DeletePoseGraphMutation : CharacterPresentationMutation
    {
        public DeletePoseGraphMutation(string graphAssetId, PoseGraphId graphId)
            : base(CharacterPresentationMutationKind.DeletePoseGraph, graphAssetId)
        {
            GraphId = graphId.IsValid
                ? graphId
                : throw new ArgumentException(
                    "Pose Graph identity is invalid.",
                    nameof(graphId));
        }

        public PoseGraphId GraphId { get; }
    }

    public sealed class SetPoseGraphParametersMutation :
        CharacterPresentationMutation
    {
        public SetPoseGraphParametersMutation(
            string graphId,
            CharacterPoseParameterDeclaration[] parameters)
            : base(
                CharacterPresentationMutationKind.SetPoseGraphParameters,
                graphId)
        {
            Parameters = parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
        }

        public IReadOnlyList<CharacterPoseParameterDeclaration> Parameters { get; }
    }

    public sealed class SetPoseNodeNameMutation : CharacterPresentationMutation
    {
        public SetPoseNodeNameMutation(
            string graphId,
            PoseNodeId nodeId,
            string displayName)
            : base(CharacterPresentationMutationKind.SetPoseNodeName, graphId)
        {
            NodeId = nodeId.IsValid
                ? nodeId
                : throw new ArgumentException(
                    "Pose Node identity is invalid.",
                    nameof(nodeId));
            DisplayName = displayName ?? string.Empty;
        }

        public PoseNodeId NodeId { get; }
        public string DisplayName { get; }
    }

    public sealed class CreatePoseStateMachineMutation : CharacterPresentationMutation
    {
        public CreatePoseStateMachineMutation(string graphAssetId, CharacterPoseStateMachineDefinition stateMachine) : base(CharacterPresentationMutationKind.CreatePoseStateMachine, graphAssetId) => StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        public CharacterPoseStateMachineDefinition StateMachine { get; }
    }

    public sealed class CreatePoseStateMutation : CharacterPresentationMutation
    {
        public CreatePoseStateMutation(string stateMachineId, CharacterPoseStateDefinition state) : base(CharacterPresentationMutationKind.CreatePoseState, stateMachineId) => State = state ?? throw new ArgumentNullException(nameof(state));
        public CharacterPoseStateDefinition State { get; }
    }

    public sealed class DeletePoseStateMutation : CharacterPresentationMutation
    {
        public DeletePoseStateMutation(string stateMachineId, PoseStateId stateId) : base(CharacterPresentationMutationKind.DeletePoseState, stateMachineId) => StateId = stateId.IsValid ? stateId : throw new ArgumentException("Pose State identity is invalid.", nameof(stateId));
        public PoseStateId StateId { get; }
    }

    public sealed class SetPoseStateFieldMutation : CharacterPresentationMutation
    {
        public SetPoseStateFieldMutation(
            string stateMachineId,
            PoseStateId stateId,
            string fieldId,
            object value)
            : base(CharacterPresentationMutationKind.SetPoseStateField, stateMachineId)
        {
            StateId = stateId.IsValid
                ? stateId
                : throw new ArgumentException("Pose State identity is invalid.", nameof(stateId));
            FieldId = RequireIdentity(fieldId, nameof(fieldId));
            Value = value;
        }

        public PoseStateId StateId { get; }
        public string FieldId { get; }
        public object Value { get; }
    }

    public sealed class CreatePoseTransitionMutation : CharacterPresentationMutation
    {
        public CreatePoseTransitionMutation(string stateMachineId, CharacterPoseStateTransition transition) : base(CharacterPresentationMutationKind.CreatePoseTransition, stateMachineId) => Transition = transition ?? throw new ArgumentNullException(nameof(transition));
        public CharacterPoseStateTransition Transition { get; }
    }

    public sealed class DeletePoseTransitionMutation : CharacterPresentationMutation
    {
        public DeletePoseTransitionMutation(string stateMachineId, PoseStateTransitionId transitionId) : base(CharacterPresentationMutationKind.DeletePoseTransition, stateMachineId) => TransitionId = transitionId.IsValid ? transitionId : throw new ArgumentException("Pose Transition identity is invalid.", nameof(transitionId));
        public PoseStateTransitionId TransitionId { get; }
    }

    public sealed class SetPoseTransitionFieldMutation : CharacterPresentationMutation
    {
        public SetPoseTransitionFieldMutation(string stateMachineId, PoseStateTransitionId transitionId, string fieldId, object value) : base(CharacterPresentationMutationKind.SetPoseTransitionField, stateMachineId)
        {
            TransitionId = transitionId.IsValid ? transitionId : throw new ArgumentException("Pose Transition identity is invalid.", nameof(transitionId));
            FieldId = RequireIdentity(fieldId, nameof(fieldId));
            Value = value;
        }
        public PoseStateTransitionId TransitionId { get; }
        public string FieldId { get; }
        public object Value { get; }
    }

    public sealed class ConfigurePoseStateMachineMutation :
        CharacterPresentationMutation
    {
        public ConfigurePoseStateMachineMutation(
            string stateMachineId,
            CharacterPoseStateEntry entry,
            CharacterPoseStateAlias[] aliases,
            int maxTransitionsPerFrame)
            : base(
                CharacterPresentationMutationKind.ConfigurePoseStateMachine,
                stateMachineId)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Aliases = aliases ?? Array.Empty<CharacterPoseStateAlias>();
            MaxTransitionsPerFrame = maxTransitionsPerFrame > 0
                ? maxTransitionsPerFrame
                : throw new ArgumentOutOfRangeException(
                    nameof(maxTransitionsPerFrame));
        }

        public CharacterPoseStateEntry Entry { get; }
        public IReadOnlyList<CharacterPoseStateAlias> Aliases { get; }
        public int MaxTransitionsPerFrame { get; }
    }

    public sealed class SetPoseStateMachineLayoutElementMutation :
        CharacterPresentationMutation
    {
        public SetPoseStateMachineLayoutElementMutation(
            string stateMachineId,
            string elementId,
            Vector2 position)
            : base(
                CharacterPresentationMutationKind
                    .SetPoseStateMachineLayoutElement,
                stateMachineId)
        {
            ElementId = RequireIdentity(elementId, nameof(elementId));
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y))
                throw new ArgumentException(
                    "Pose StateMachine layout position must be finite.",
                    nameof(position));
            Position = position;
        }

        public string ElementId { get; }
        public Vector2 Position { get; }
    }

    public sealed class RemovePoseStateMachineLayoutElementMutation :
        CharacterPresentationMutation
    {
        public RemovePoseStateMachineLayoutElementMutation(
            string stateMachineId,
            string elementId)
            : base(
                CharacterPresentationMutationKind
                    .RemovePoseStateMachineLayoutElement,
                stateMachineId)
        {
            ElementId = RequireIdentity(elementId, nameof(elementId));
        }

        public string ElementId { get; }
    }

    public sealed class CreatePoseSourceSlotMutation : CharacterPresentationMutation
    {
        public CreatePoseSourceSlotMutation(
            string graphAssetId,
            CharacterPresentationPoseSourceSlot slot)
            : base(CharacterPresentationMutationKind.CreatePoseSourceSlot, graphAssetId)
        {
            Slot = slot ? slot : throw new ArgumentNullException(nameof(slot));
        }

        public CharacterPresentationPoseSourceSlot Slot { get; }
    }

    public sealed class RenamePoseSourceSlotMutation : CharacterPresentationMutation
    {
        public RenamePoseSourceSlotMutation(
            string graphAssetId,
            CharacterPresentationPoseSourceSlot slot,
            string displayName)
            : base(CharacterPresentationMutationKind.RenamePoseSourceSlot, graphAssetId)
        {
            Slot = slot ? slot : throw new ArgumentNullException(nameof(slot));
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Pose Source Slot name is missing.", nameof(displayName))
                : displayName.Trim();
        }

        public CharacterPresentationPoseSourceSlot Slot { get; }
        public string DisplayName { get; }
    }

    public sealed class DeletePoseSourceSlotMutation : CharacterPresentationMutation
    {
        public DeletePoseSourceSlotMutation(
            string graphAssetId,
            CharacterPresentationPoseSourceSlot slot)
            : base(CharacterPresentationMutationKind.DeletePoseSourceSlot, graphAssetId)
        {
            Slot = slot ? slot : throw new ArgumentNullException(nameof(slot));
        }

        public CharacterPresentationPoseSourceSlot Slot { get; }
    }

    public sealed class CreateProfileSourceBindingMutation : CharacterPresentationMutation
    {
        public CreateProfileSourceBindingMutation(
            string profileId,
            CharacterPresentationPoseSourceBinding binding)
            : base(CharacterPresentationMutationKind.CreateProfileSourceBinding, profileId)
        {
            Binding = binding ? binding : throw new ArgumentNullException(nameof(binding));
        }

        public CharacterPresentationPoseSourceBinding Binding { get; }
    }

    public sealed class SetProfileSourceBindingMutation : CharacterPresentationMutation
    {
        public SetProfileSourceBindingMutation(string profileId, CharacterPresentationPoseSourceBinding binding) : base(CharacterPresentationMutationKind.SetProfileSourceBinding, profileId) => Binding = binding ? binding : throw new ArgumentNullException(nameof(binding));
        public CharacterPresentationPoseSourceBinding Binding { get; }
    }

    public sealed class RenameProfileSourceBindingMutation : CharacterPresentationMutation
    {
        public RenameProfileSourceBindingMutation(
            string profileId,
            CharacterPresentationPoseSourceBinding binding,
            string displayName)
            : base(CharacterPresentationMutationKind.RenameProfileSourceBinding, profileId)
        {
            Binding = binding ? binding : throw new ArgumentNullException(nameof(binding));
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Pose source binding name is missing.", nameof(displayName))
                : displayName.Trim();
        }

        public CharacterPresentationPoseSourceBinding Binding { get; }
        public string DisplayName { get; }
    }

    public sealed class RemoveProfileSourceBindingMutation :
        CharacterPresentationMutation
    {
        public RemoveProfileSourceBindingMutation(
            string profileId,
            CharacterPresentationPoseSourceBinding binding)
            : base(CharacterPresentationMutationKind.RemoveProfileSourceBinding, profileId)
        {
            Binding = binding ? binding : throw new ArgumentNullException(nameof(binding));
        }

        public CharacterPresentationPoseSourceBinding Binding { get; }
    }

    public sealed class SetProfileProducerBindingMutation :
        CharacterPresentationMutation
    {
        public SetProfileProducerBindingMutation(
            string profileId,
            AnimationProducerPresentationBinding binding)
            : base(
                CharacterPresentationMutationKind.SetProfileProducerBinding,
                profileId)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        public AnimationProducerPresentationBinding Binding { get; }
    }

    public sealed class RemoveProfileProducerBindingMutation :
        CharacterPresentationMutation
    {
        public RemoveProfileProducerBindingMutation(
            string profileId,
            AnimationProducerId producerId)
            : base(
                CharacterPresentationMutationKind.RemoveProfileProducerBinding,
                profileId)
        {
            ProducerId = producerId.IsValid
                ? producerId
                : throw new ArgumentException(
                    "Animation Producer identity is invalid.",
                    nameof(producerId));
        }

        public AnimationProducerId ProducerId { get; }
    }

    public sealed class SetProfilePolicyMutation : CharacterPresentationMutation
    {
        public SetProfilePolicyMutation(string profileId, string policyId, object value) : base(CharacterPresentationMutationKind.SetProfilePolicy, profileId)
        {
            PolicyId = RequireIdentity(policyId, nameof(policyId));
            Value = value;
        }
        public string PolicyId { get; }
        public object Value { get; }
    }

    public sealed class SetAnimationSlotBindingMutation : CharacterPresentationMutation
    {
        public SetAnimationSlotBindingMutation(string profileId, PoseNodeId nodeId, AnimationSlotId slotId, AnimationChannelId channelId) : base(CharacterPresentationMutationKind.SetAnimationSlotBinding, profileId)
        {
            NodeId = nodeId.IsValid ? nodeId : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            SlotId = slotId.IsValid ? slotId : throw new ArgumentException("Animation Slot identity is invalid.", nameof(slotId));
            ChannelId = channelId.IsValid ? channelId : throw new ArgumentException("Animation Channel identity is invalid.", nameof(channelId));
        }
        public PoseNodeId NodeId { get; }
        public AnimationSlotId SlotId { get; }
        public AnimationChannelId ChannelId { get; }
    }

    public sealed class SetPresentationGraphMutation : CharacterPresentationMutation
    {
        public SetPresentationGraphMutation(
            string profileId,
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterAnimationRigDefinition rig)
            : base(CharacterPresentationMutationKind.SetPresentationGraph, profileId)
        {
            PoseGraph = poseGraph ? poseGraph : throw new ArgumentNullException(nameof(poseGraph));
            Rig = rig ? rig : throw new ArgumentNullException(nameof(rig));
        }

        public CharacterPresentationPoseGraphAsset PoseGraph { get; }
        public CharacterAnimationRigDefinition Rig { get; }
    }

    public sealed class SetMotionMatchingProfileMutation : CharacterPresentationMutation
    {
        public SetMotionMatchingProfileMutation(
            string profileId,
            CharacterMotionMatchingProfile profile)
            : base(CharacterPresentationMutationKind.SetMotionMatchingProfile, profileId)
        {
            Profile = profile;
        }

        public CharacterMotionMatchingProfile Profile { get; }
    }

    public sealed class SetFootPlacementAnalysisMutation : CharacterPresentationMutation
    {
        public SetFootPlacementAnalysisMutation(
            string profileId,
            CharacterFootPlacementAnalysisMode mode,
            string sourceAssetGuid)
            : base(CharacterPresentationMutationKind.SetFootPlacementAnalysis, profileId)
        {
            Mode = mode;
            SourceAssetGuid = sourceAssetGuid ?? string.Empty;
        }

        public CharacterFootPlacementAnalysisMode Mode { get; }
        public string SourceAssetGuid { get; }
    }

    public sealed class CreateLinkedPoseImplementationMutation :
        CharacterPresentationMutation
    {
        public CreateLinkedPoseImplementationMutation(
            string profileId,
            CharacterLinkedPoseImplementationAsset implementation,
            CharacterPresentationPoseGraphAsset graphOwner)
            : base(
                CharacterPresentationMutationKind.CreateLinkedPoseImplementation,
                profileId)
        {
            Implementation = implementation
                ? implementation
                : throw new ArgumentNullException(nameof(implementation));
            GraphOwner = graphOwner
                ? graphOwner
                : throw new ArgumentNullException(nameof(graphOwner));
        }

        public CharacterLinkedPoseImplementationAsset Implementation { get; }
        public CharacterPresentationPoseGraphAsset GraphOwner { get; }
    }

    public sealed class CreateLinkedPoseInterfaceMutation :
        CharacterPresentationMutation
    {
        public CreateLinkedPoseInterfaceMutation(
            string profileId,
            CharacterLinkedPoseInterfaceAsset linkedInterface)
            : base(
                CharacterPresentationMutationKind.CreateLinkedPoseInterface,
                profileId)
        {
            Interface = linkedInterface
                ? linkedInterface
                : throw new ArgumentNullException(nameof(linkedInterface));
        }

        public CharacterLinkedPoseInterfaceAsset Interface { get; }
    }

    public sealed class ConfigureLinkedPoseInterfaceMutation :
        CharacterPresentationMutation
    {
        public ConfigureLinkedPoseInterfaceMutation(
            string profileId,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            string ownerIdentity,
            string displayName,
            LinkedPoseInterfaceId interfaceId,
            LinkedPoseRevision revision,
            CharacterLinkedPoseInterfaceEntryDescriptor[] entries)
            : base(
                CharacterPresentationMutationKind.ConfigureLinkedPoseInterface,
                profileId)
        {
            Interface = linkedInterface
                ? linkedInterface
                : throw new ArgumentNullException(nameof(linkedInterface));
            OwnerIdentity = RequireIdentity(ownerIdentity, nameof(ownerIdentity));
            DisplayName = RequireIdentity(displayName, nameof(displayName));
            InterfaceId = interfaceId.IsValid
                ? interfaceId
                : throw new ArgumentException(
                    "Linked Pose Interface identity is invalid.",
                    nameof(interfaceId));
            Revision = revision.IsValid
                ? revision
                : throw new ArgumentException(
                    "Linked Pose Interface revision is invalid.",
                    nameof(revision));
            Entries = entries ?? Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>();
        }

        public CharacterLinkedPoseInterfaceAsset Interface { get; }
        public string OwnerIdentity { get; }
        public string DisplayName { get; }
        public LinkedPoseInterfaceId InterfaceId { get; }
        public LinkedPoseRevision Revision { get; }
        public IReadOnlyList<CharacterLinkedPoseInterfaceEntryDescriptor> Entries { get; }
    }

    public sealed class RemoveLinkedPoseInterfaceMutation :
        CharacterPresentationMutation
    {
        public RemoveLinkedPoseInterfaceMutation(
            string profileId,
            CharacterLinkedPoseInterfaceAsset linkedInterface)
            : base(
                CharacterPresentationMutationKind.RemoveLinkedPoseInterface,
                profileId)
        {
            Interface = linkedInterface
                ? linkedInterface
                : throw new ArgumentNullException(nameof(linkedInterface));
        }

        public CharacterLinkedPoseInterfaceAsset Interface { get; }
    }

    public sealed class ConfigureLinkedPoseCallMutation :
        CharacterPresentationMutation
    {
        public ConfigureLinkedPoseCallMutation(
            string graphId,
            PoseNodeId nodeId,
            CharacterLinkedPoseCallPayload payload,
            CharacterPoseDynamicPort[] ports)
            : base(
                CharacterPresentationMutationKind.ConfigureLinkedPoseCall,
                graphId)
        {
            NodeId = nodeId.IsValid
                ? nodeId
                : throw new ArgumentException(
                    "Linked Pose Call node identity is invalid.",
                    nameof(nodeId));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Ports = ports ?? Array.Empty<CharacterPoseDynamicPort>();
        }

        public PoseNodeId NodeId { get; }
        public CharacterLinkedPoseCallPayload Payload { get; }
        public IReadOnlyList<CharacterPoseDynamicPort> Ports { get; }
    }

    public sealed class ConfigureLinkedPoseImplementationMutation :
        CharacterPresentationMutation
    {
        public ConfigureLinkedPoseImplementationMutation(
            string profileId,
            CharacterLinkedPoseImplementationAsset implementation,
            string ownerIdentity,
            string displayName,
            LinkedPoseImplementationId implementationId,
            LinkedPoseRevision revision,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            CharacterLinkedPoseImplementationEntryMutationValue[] entries)
            : base(
                CharacterPresentationMutationKind.ConfigureLinkedPoseImplementation,
                profileId)
        {
            Implementation = implementation
                ? implementation
                : throw new ArgumentNullException(nameof(implementation));
            OwnerIdentity = RequireIdentity(ownerIdentity, nameof(ownerIdentity));
            DisplayName = RequireIdentity(displayName, nameof(displayName));
            ImplementationId = implementationId.IsValid
                ? implementationId
                : throw new ArgumentException(
                    "Linked Pose Implementation identity is invalid.",
                    nameof(implementationId));
            Revision = revision.IsValid
                ? revision
                : throw new ArgumentException(
                    "Linked Pose revision is invalid.",
                    nameof(revision));
            Interface = linkedInterface
                ? linkedInterface
                : throw new ArgumentNullException(nameof(linkedInterface));
            Entries = entries ?? Array.Empty<CharacterLinkedPoseImplementationEntryMutationValue>();
        }

        public CharacterLinkedPoseImplementationAsset Implementation { get; }
        public string OwnerIdentity { get; }
        public string DisplayName { get; }
        public LinkedPoseImplementationId ImplementationId { get; }
        public LinkedPoseRevision Revision { get; }
        public CharacterLinkedPoseInterfaceAsset Interface { get; }
        public IReadOnlyList<CharacterLinkedPoseImplementationEntryMutationValue> Entries { get; }
    }

    public sealed class CharacterLinkedPoseImplementationEntryMutationValue
    {
        public CharacterLinkedPoseImplementationEntryMutationValue(
            LinkedPoseEntryId entryId,
            string graphOwnerIdentity,
            CharacterPresentationPoseGraphAsset graphOwner,
            PoseGraphId graphId)
        {
            EntryId = entryId.IsValid
                ? entryId
                : throw new ArgumentException("Entry identity is invalid.", nameof(entryId));
            GraphOwnerIdentity = string.IsNullOrWhiteSpace(graphOwnerIdentity)
                ? throw new ArgumentException(
                    "Graph owner identity is invalid.",
                    nameof(graphOwnerIdentity))
                : graphOwnerIdentity.Trim();
            GraphOwner = graphOwner
                ? graphOwner
                : throw new ArgumentNullException(nameof(graphOwner));
            GraphId = graphId.IsValid
                ? graphId
                : throw new ArgumentException("Graph identity is invalid.", nameof(graphId));
        }

        public LinkedPoseEntryId EntryId { get; }
        public string GraphOwnerIdentity { get; }
        public CharacterPresentationPoseGraphAsset GraphOwner { get; }
        public PoseGraphId GraphId { get; }
    }

    public sealed class RemoveLinkedPoseImplementationMutation :
        CharacterPresentationMutation
    {
        public RemoveLinkedPoseImplementationMutation(
            string profileId,
            CharacterLinkedPoseImplementationAsset implementation)
            : base(
                CharacterPresentationMutationKind.RemoveLinkedPoseImplementation,
                profileId)
        {
            Implementation = implementation
                ? implementation
                : throw new ArgumentNullException(nameof(implementation));
        }

        public CharacterLinkedPoseImplementationAsset Implementation { get; }
    }

    public sealed class SetLinkedPoseGroupMutation : CharacterPresentationMutation
    {
        public SetLinkedPoseGroupMutation(
            string profileId,
            CharacterLinkedPoseGroupBinding binding)
            : base(CharacterPresentationMutationKind.SetLinkedPoseGroup, profileId)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        public CharacterLinkedPoseGroupBinding Binding { get; }
    }

    public sealed class RemoveLinkedPoseGroupMutation : CharacterPresentationMutation
    {
        public RemoveLinkedPoseGroupMutation(
            string profileId,
            LinkedPoseGroupId groupId)
            : base(CharacterPresentationMutationKind.RemoveLinkedPoseGroup, profileId)
        {
            GroupId = groupId.IsValid
                ? groupId
                : throw new ArgumentException(
                    "Linked Pose Group identity is invalid.",
                    nameof(groupId));
        }

        public LinkedPoseGroupId GroupId { get; }
    }

    public sealed class CreateEquipmentLinkedPoseSelectorMutation :
        CharacterPresentationMutation
    {
        public CreateEquipmentLinkedPoseSelectorMutation(
            string profileId,
            CharacterEquipmentLinkedPoseSelectionBinding selector)
            : base(
                CharacterPresentationMutationKind.CreateEquipmentLinkedPoseSelector,
                profileId)
        {
            Selector = selector
                ? selector
                : throw new ArgumentNullException(nameof(selector));
        }

        public CharacterEquipmentLinkedPoseSelectionBinding Selector { get; }
    }

    public sealed class ConfigureEquipmentLinkedPoseSelectorMutation :
        CharacterPresentationMutation
    {
        public ConfigureEquipmentLinkedPoseSelectorMutation(
            string profileId,
            CharacterEquipmentLinkedPoseSelectionBinding selector,
            LinkedPoseSelectorId selectorId,
            LinkedPoseGroupId groupId,
            EquipmentSlotId slotId,
            LinkedPoseImplementationId emptyImplementationId,
            CharacterEquipmentLinkedPoseMapping[] mappings)
            : base(
                CharacterPresentationMutationKind.ConfigureEquipmentLinkedPoseSelector,
                profileId)
        {
            Selector = selector
                ? selector
                : throw new ArgumentNullException(nameof(selector));
            SelectorId = selectorId.IsValid
                ? selectorId
                : throw new ArgumentException("Selector identity is invalid.", nameof(selectorId));
            GroupId = groupId.IsValid
                ? groupId
                : throw new ArgumentException("Group identity is invalid.", nameof(groupId));
            SlotId = slotId.IsValid
                ? slotId
                : throw new ArgumentException("Equipment Slot identity is invalid.", nameof(slotId));
            EmptyImplementationId = emptyImplementationId.IsValid
                ? emptyImplementationId
                : throw new ArgumentException(
                    "Empty Implementation identity is invalid.",
                    nameof(emptyImplementationId));
            Mappings = mappings ?? Array.Empty<CharacterEquipmentLinkedPoseMapping>();
        }

        public CharacterEquipmentLinkedPoseSelectionBinding Selector { get; }
        public LinkedPoseSelectorId SelectorId { get; }
        public LinkedPoseGroupId GroupId { get; }
        public EquipmentSlotId SlotId { get; }
        public LinkedPoseImplementationId EmptyImplementationId { get; }
        public IReadOnlyList<CharacterEquipmentLinkedPoseMapping> Mappings { get; }
    }

    public sealed class RemoveLinkedPoseSelectorMutation :
        CharacterPresentationMutation
    {
        public RemoveLinkedPoseSelectorMutation(
            string profileId,
            CharacterLinkedPoseSelectorBindingAsset selector)
            : base(CharacterPresentationMutationKind.RemoveLinkedPoseSelector, profileId)
        {
            Selector = selector
                ? selector
                : throw new ArgumentNullException(nameof(selector));
        }

        public CharacterLinkedPoseSelectorBindingAsset Selector { get; }
    }

    public sealed class SetEquipmentLinkedPoseMappingMutation :
        CharacterPresentationMutation
    {
        public SetEquipmentLinkedPoseMappingMutation(
            string profileId,
            CharacterEquipmentLinkedPoseSelectionBinding selector,
            CharacterEquipmentLinkedPoseMapping mapping)
            : base(
                CharacterPresentationMutationKind.SetEquipmentLinkedPoseMapping,
                profileId)
        {
            Selector = selector
                ? selector
                : throw new ArgumentNullException(nameof(selector));
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }

        public CharacterEquipmentLinkedPoseSelectionBinding Selector { get; }
        public CharacterEquipmentLinkedPoseMapping Mapping { get; }
    }

    public sealed class RemoveEquipmentLinkedPoseMappingMutation :
        CharacterPresentationMutation
    {
        public RemoveEquipmentLinkedPoseMappingMutation(
            string profileId,
            CharacterEquipmentLinkedPoseSelectionBinding selector,
            EquipmentId equipmentId)
            : base(
                CharacterPresentationMutationKind.RemoveEquipmentLinkedPoseMapping,
                profileId)
        {
            Selector = selector
                ? selector
                : throw new ArgumentNullException(nameof(selector));
            EquipmentId = equipmentId.IsValid
                ? equipmentId
                : throw new ArgumentException(
                    "Equipment identity is invalid.",
                    nameof(equipmentId));
        }

        public CharacterEquipmentLinkedPoseSelectionBinding Selector { get; }
        public EquipmentId EquipmentId { get; }
    }

    public sealed class CharacterPresentationMutationTransaction
    {
        readonly List<CharacterPresentationMutation> m_Mutations = new List<CharacterPresentationMutation>();

        public CharacterPresentationMutationTransaction(string transactionId, string displayName)
        {
            TransactionId = string.IsNullOrWhiteSpace(transactionId) ? throw new ArgumentException("Presentation transaction identity is missing.", nameof(transactionId)) : transactionId.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Edit Presentation" : displayName.Trim();
        }

        public string TransactionId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<CharacterPresentationMutation> Mutations => m_Mutations;

        public void Add(CharacterPresentationMutation mutation) => m_Mutations.Add(mutation ?? throw new ArgumentNullException(nameof(mutation)));
    }

    public interface ICharacterPresentationMutationOwner
    {
        UnityEngine.Object SerializedOwner { get; }
        CharacterTypedPoseGraph RequirePoseGraph(string graphId);
        void ReplacePoseGraph(CharacterTypedPoseGraph graph);
        void ApplyGraphCatalogMutation(CharacterPresentationMutation mutation);
        void ApplyStateMachineMutation(CharacterPresentationMutation mutation);
        void ApplyProfileMutation(CharacterPresentationMutation mutation);
    }

    public sealed class CharacterPresentationMutationService
    {
        public void Apply(ICharacterPresentationMutationOwner owner, CharacterPresentationMutationTransaction transaction)
        {
            if (owner?.SerializedOwner == null)
                throw new ArgumentNullException(nameof(owner));
            if (transaction == null || transaction.Mutations.Count == 0)
                throw new ArgumentException("Presentation mutation transaction is empty.", nameof(transaction));
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(transaction.DisplayName);
            Undo.RegisterCompleteObjectUndo(owner.SerializedOwner, transaction.DisplayName);
            try
            {
                ApplyWithoutUndo(owner, transaction);
                EditorUtility.SetDirty(owner.SerializedOwner);
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        public void ApplyWithoutUndo(
            ICharacterPresentationMutationOwner owner,
            CharacterPresentationMutationTransaction transaction)
        {
            if (owner?.SerializedOwner == null)
                throw new ArgumentNullException(nameof(owner));
            if (transaction == null || transaction.Mutations.Count == 0)
                throw new ArgumentException(
                    "Presentation mutation transaction is empty.",
                    nameof(transaction));
            foreach (CharacterPresentationMutation mutation in
                     transaction.Mutations.Where(IsGraphCatalogPreMutation))
                owner.ApplyGraphCatalogMutation(mutation);
            foreach (IGrouping<string, CharacterPresentationMutation> group in
                     transaction.Mutations
                         .Where(IsPoseGraphMutation)
                         .GroupBy(value => value.OwnerId, StringComparer.Ordinal))
            {
                CharacterTypedPoseGraph current =
                    owner.RequirePoseGraph(group.Key);
                CharacterTypedPoseGraph next =
                    ApplyPoseGraph(current, group.ToArray());
                owner.ReplacePoseGraph(next);
            }
            foreach (CharacterPresentationMutation mutation in
                     transaction.Mutations.Where(IsGraphCatalogPostMutation))
                owner.ApplyGraphCatalogMutation(mutation);
            foreach (CharacterPresentationMutation mutation in
                     transaction.Mutations.Where(IsStateMachineMutation))
                owner.ApplyStateMachineMutation(mutation);
            foreach (CharacterPresentationMutation mutation in
                     transaction.Mutations.Where(IsProfileMutation))
                owner.ApplyProfileMutation(mutation);
        }

        static CharacterTypedPoseGraph ApplyPoseGraph(CharacterTypedPoseGraph graph, IReadOnlyList<CharacterPresentationMutation> mutations)
        {
            var nodes = graph.Nodes.ToList();
            var edges = graph.Edges.ToList();
            var layout = graph.Layout.ToList();
            bool layoutOnly = mutations.All(value => value is MovePoseNodeMutation);
            CharacterPoseParameterDeclaration[] parameters =
                graph.Parameters.ToArray();
            var appliedFieldNodes = new HashSet<PoseNodeId>();
            foreach (CharacterPresentationMutation mutation in mutations)
            {
                switch (mutation)
                {
                    case CreatePoseNodeMutation create:
                        if (nodes.Any(value => value.NodeId == create.Node.NodeId))
                            throw new InvalidOperationException($"Pose node '{create.Node.NodeId}' already exists.");
                        nodes.Add(create.Node);
                        layout.Add(new CharacterPoseGraphLayoutEntry(create.Node.NodeId, create.Position));
                        break;
                    case DeletePoseNodeMutation delete:
                        RequireNode(nodes, delete.NodeId);
                        nodes.RemoveAll(value => value.NodeId == delete.NodeId);
                        edges.RemoveAll(value => value.SourceNodeId == delete.NodeId || value.TargetNodeId == delete.NodeId);
                        layout.RemoveAll(value => value.NodeId == delete.NodeId);
                        break;
                    case SetPoseNodeFieldMutation set:
                    {
                        if (!appliedFieldNodes.Add(set.NodeId))
                            break;
                        int index = RequireNodeIndex(nodes, set.NodeId);
                        CharacterTypedPoseNode node = nodes[index];
                        SetPoseNodeFieldMutation[] fields = mutations
                            .OfType<SetPoseNodeFieldMutation>()
                            .Where(value => value.NodeId == set.NodeId)
                            .ToArray();
                        nodes[index] = new CharacterTypedPoseNode(node.NodeId, node.DisplayName, CharacterPosePayloadFieldMutation.Set(node.Payload, fields), node.DynamicPorts.ToArray());
                        break;
                    }
                    case ConfigureLinkedPoseCallMutation configure:
                    {
                        int index = RequireNodeIndex(nodes, configure.NodeId);
                        CharacterTypedPoseNode node = nodes[index];
                        if (node.Kind != CharacterPoseNodeKind.LinkedPoseCall)
                            throw new InvalidOperationException(
                                $"Pose node '{configure.NodeId}' is not a Linked Pose Call.");
                        var replacement = new CharacterTypedPoseNode(
                            node.NodeId,
                            node.DisplayName,
                            configure.Payload,
                            configure.Ports.ToArray());
                        IReadOnlyList<CharacterPosePortDefinition> ports =
                            CharacterPoseAuthoringPortProjection.Get(replacement);
                        foreach (CharacterPoseEdge edge in edges.Where(value =>
                                     value.SourceNodeId == configure.NodeId ||
                                     value.TargetNodeId == configure.NodeId))
                        {
                            bool sourceValid = edge.SourceNodeId != configure.NodeId ||
                                ports.Any(value => value.PortId.Equals(edge.SourcePortId) &&
                                                   value.Direction == CharacterPosePortDirection.Output);
                            bool targetValid = edge.TargetNodeId != configure.NodeId ||
                                ports.Any(value => value.PortId.Equals(edge.TargetPortId) &&
                                                   value.Direction == CharacterPosePortDirection.Input);
                            if (!sourceValid || !targetValid)
                                throw new InvalidOperationException(
                                    $"Linked Pose Call '{configure.NodeId}' cannot rebind while edge '{edge.EdgeId}' would become incompatible.");
                        }
                        nodes[index] = replacement;
                        break;
                    }
                    case AddDynamicPosePortMutation add:
                    {
                        int index = RequireNodeIndex(nodes, add.NodeId);
                        CharacterTypedPoseNode node = nodes[index];
                        if (CharacterPoseAuthoringPortProjection
                                .GetFixed(node.Kind)
                                .Any(value =>
                                    value.PortId.Equals(
                                        add.Port.PortId)) ||
                            node.DynamicPorts.Any(value =>
                                value.PortId.Equals(
                                    add.Port.PortId)))
                            throw new InvalidOperationException($"Pose port '{add.Port.PortId}' already exists on '{add.NodeId}'.");
                        nodes[index] = new CharacterTypedPoseNode(node.NodeId, node.DisplayName, node.Payload, node.DynamicPorts.Concat(new[] { add.Port }).OrderBy(value => value.Order).ToArray());
                        break;
                    }
                    case RemoveDynamicPosePortMutation remove:
                    {
                        int index = RequireNodeIndex(nodes, remove.NodeId);
                        CharacterTypedPoseNode node = nodes[index];
                        if (!node.DynamicPorts.Any(value => value.PortId.Equals(remove.PortId)))
                            throw new InvalidOperationException($"Dynamic Pose port '{remove.PortId}' does not exist on '{remove.NodeId}'.");
                        nodes[index] = new CharacterTypedPoseNode(node.NodeId, node.DisplayName, node.Payload, node.DynamicPorts.Where(value => !value.PortId.Equals(remove.PortId)).ToArray());
                        edges.RemoveAll(value => (value.SourceNodeId == remove.NodeId && value.SourcePortId.Equals(remove.PortId)) || (value.TargetNodeId == remove.NodeId && value.TargetPortId.Equals(remove.PortId)));
                        break;
                    }
                    case ConnectPosePortMutation connect:
                        if (edges.Any(value => string.Equals(value.EdgeId, connect.EdgeId, StringComparison.Ordinal)))
                            throw new InvalidOperationException($"Pose edge '{connect.EdgeId}' already exists.");
                        CharacterTypedPoseNode sourceNode = RequireNode(nodes, connect.SourceNodeId);
                        CharacterTypedPoseNode targetNode = RequireNode(nodes, connect.TargetNodeId);
                        CharacterPosePortDefinition sourcePort = RequirePort(sourceNode, connect.SourcePortId);
                        CharacterPosePortDefinition targetPort = RequirePort(targetNode, connect.TargetPortId);
                        if (sourcePort.Direction != CharacterPosePortDirection.Output ||
                            targetPort.Direction != CharacterPosePortDirection.Input ||
                            sourcePort.Kind != targetPort.Kind)
                        {
                            throw new InvalidOperationException(
                                $"Pose edge '{connect.EdgeId}' cannot connect {sourcePort.Kind} '{connect.SourceNodeId}/{connect.SourcePortId}' to {targetPort.Kind} '{connect.TargetNodeId}/{connect.TargetPortId}'.");
                        }
                        if (edges.Any(value => value.TargetNodeId == connect.TargetNodeId &&
                                               value.TargetPortId.Equals(connect.TargetPortId)))
                            throw new InvalidOperationException($"Pose input '{connect.TargetNodeId}/{connect.TargetPortId}' is already connected.");
                        edges.Add(new CharacterPoseEdge(connect.EdgeId, connect.SourceNodeId, connect.SourcePortId, connect.TargetNodeId, connect.TargetPortId));
                        break;
                    case DisconnectPosePortMutation disconnect:
                        if (edges.RemoveAll(value => string.Equals(value.EdgeId, disconnect.EdgeId, StringComparison.Ordinal)) != 1)
                            throw new InvalidOperationException($"Pose edge '{disconnect.EdgeId}' does not exist.");
                        break;
                    case MovePoseNodeMutation move:
                        RequireNode(nodes, move.NodeId);
                        layout.RemoveAll(value => value.NodeId == move.NodeId);
                        layout.Add(new CharacterPoseGraphLayoutEntry(move.NodeId, move.Position));
                        break;
                    case SetPoseGraphParametersMutation setParameters:
                        parameters = setParameters.Parameters.ToArray();
                        break;
                    case SetPoseNodeNameMutation setName:
                    {
                        int index = RequireNodeIndex(nodes, setName.NodeId);
                        CharacterTypedPoseNode node = nodes[index];
                        nodes[index] = new CharacterTypedPoseNode(
                            node.NodeId,
                            setName.DisplayName,
                            node.Payload,
                            node.DynamicPorts.ToArray());
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Mutation '{mutation.Kind}' is not a Pose Graph command.");
                }
            }
            string contentRevision = layoutOnly
                ? graph.ContentRevision
                : Guid.NewGuid().ToString("N");
            return new CharacterTypedPoseGraph(graph.GraphId, contentRevision, parameters, nodes.ToArray(), edges.ToArray(), layout.ToArray());
        }

        static int RequireNodeIndex(IReadOnlyList<CharacterTypedPoseNode> nodes, PoseNodeId nodeId)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].NodeId == nodeId)
                    return i;
            throw new InvalidOperationException($"Pose node '{nodeId}' does not exist.");
        }

        static CharacterTypedPoseNode RequireNode(IReadOnlyList<CharacterTypedPoseNode> nodes, PoseNodeId nodeId) =>
            nodes[RequireNodeIndex(nodes, nodeId)];

        static CharacterPosePortDefinition RequirePort(
            CharacterTypedPoseNode node,
            PosePortId portId) =>
            CharacterPoseAuthoringPortProjection.Get(node)
                .SingleOrDefault(value => value != null && value.PortId.Equals(portId)) ??
            throw new InvalidOperationException(
                $"Pose node '{node.NodeId}' does not declare port '{portId}'.");
        static bool IsGraphCatalogPreMutation(CharacterPresentationMutation value) =>
            value.Kind == CharacterPresentationMutationKind.CreatePoseGraph ||
            value.Kind == CharacterPresentationMutationKind.DeletePoseGraph ||
            value.Kind == CharacterPresentationMutationKind.CreatePoseSourceSlot ||
            value.Kind == CharacterPresentationMutationKind.RenamePoseSourceSlot;

        static bool IsGraphCatalogPostMutation(CharacterPresentationMutation value) =>
            value.Kind == CharacterPresentationMutationKind.DeletePoseSourceSlot;

        static bool IsPoseGraphMutation(CharacterPresentationMutation value) =>
            value.Kind >= CharacterPresentationMutationKind.CreatePoseNode &&
            value.Kind <= CharacterPresentationMutationKind.MovePoseNode ||
            value.Kind == CharacterPresentationMutationKind.SetPoseGraphParameters ||
            value.Kind == CharacterPresentationMutationKind.SetPoseNodeName ||
            value.Kind == CharacterPresentationMutationKind.ConfigureLinkedPoseCall;

        static bool IsStateMachineMutation(CharacterPresentationMutation value) =>
            value.Kind >= CharacterPresentationMutationKind.CreatePoseStateMachine &&
            value.Kind <= CharacterPresentationMutationKind.SetPoseTransitionField ||
            value.Kind == CharacterPresentationMutationKind.SetPoseStateField ||
            value.Kind == CharacterPresentationMutationKind.ConfigurePoseStateMachine ||
            value.Kind == CharacterPresentationMutationKind.SetPoseStateMachineLayoutElement ||
            value.Kind == CharacterPresentationMutationKind.RemovePoseStateMachineLayoutElement;

        static bool IsProfileMutation(CharacterPresentationMutation value) =>
            value.Kind >= CharacterPresentationMutationKind.SetProfileSourceBinding &&
            value.Kind <= CharacterPresentationMutationKind.SetFootPlacementAnalysis ||
            value.Kind >= CharacterPresentationMutationKind.RemoveProfileSourceBinding &&
            value.Kind <= CharacterPresentationMutationKind.RemoveProfileProducerBinding ||
            value.Kind == CharacterPresentationMutationKind.CreateProfileSourceBinding ||
            value.Kind == CharacterPresentationMutationKind.RenameProfileSourceBinding ||
            value.Kind >= CharacterPresentationMutationKind.CreateLinkedPoseImplementation &&
            value.Kind <= CharacterPresentationMutationKind.RemoveEquipmentLinkedPoseMapping ||
            value.Kind >= CharacterPresentationMutationKind.CreateLinkedPoseInterface &&
            value.Kind <= CharacterPresentationMutationKind.RemoveLinkedPoseInterface;
    }

    internal static class CharacterPoseTransitionFieldMutation
    {
        public static CharacterPoseStateTransition Set(
            CharacterPoseStateTransition current,
            string fieldId,
            object value)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            int priority = current.Priority;
            CharacterPoseStateTransitionSource source = current.Source;
            PoseStateId targetStateId = current.TargetStateId;
            CharacterPoseTransitionRuleGraph rule = current.Rule;
            AnimationTransitionBlendLogic blendLogic = current.BlendLogic;
            float durationSeconds = current.DurationSeconds;
            CharacterAnimationBlendMode blendMode = current.BlendMode;
            CharacterAnimationBlendCurveAsset customBlendCurve = current.CustomBlendCurve;
            CharacterAnimationBlendProfile blendProfile = current.BlendProfile;
            switch (fieldId)
            {
                case "source":
                    source = value as CharacterPoseStateTransitionSource ??
                             throw new InvalidOperationException(
                                 "Pose Transition source requires a typed value.");
                    break;
                case "target-state-id":
                    targetStateId = Value(
                        value,
                        text => new PoseStateId(text));
                    break;
                case "priority":
                    priority = Convert.ToInt32(value);
                    break;
                case "rule":
                    rule = value as CharacterPoseTransitionRuleGraph ??
                           throw new InvalidOperationException(
                               "Pose Transition rule requires a typed value.");
                    break;
                case "blend-logic":
                    blendLogic = EnumValue<AnimationTransitionBlendLogic>(value);
                    break;
                case "duration-seconds":
                    durationSeconds = Convert.ToSingle(value);
                    break;
                case "blend-mode":
                    blendMode = EnumValue<CharacterAnimationBlendMode>(value);
                    if (blendMode != CharacterAnimationBlendMode.Custom)
                        customBlendCurve = null;
                    break;
                case "custom-blend-curve":
                    customBlendCurve = value as CharacterAnimationBlendCurveAsset ??
                        throw new InvalidOperationException(
                            "Pose Transition Custom Curve requires a CharacterAnimationBlendCurveAsset.");
                    blendMode = CharacterAnimationBlendMode.Custom;
                    break;
                case "blend-profile":
                    blendProfile = value as CharacterAnimationBlendProfile;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Pose Transition does not declare field '{fieldId}'.");
            }
            return new CharacterPoseStateTransition(
                current.TransitionId,
                source,
                targetStateId,
                priority,
                rule,
                blendLogic,
                durationSeconds,
                blendMode,
                customBlendCurve,
                blendProfile);
        }

        static T Value<T>(object value, Func<string, T> create) =>
            value is T typed
                ? typed
                : create(value?.ToString() ?? string.Empty);

        static T EnumValue<T>(object value) where T : struct =>
            value is T typed
                ? typed
                : Enum.TryParse(value?.ToString(), false, out T parsed)
                    ? parsed
                    : throw new InvalidOperationException(
                        $"Value '{value}' is not a valid {typeof(T).Name}.");
    }

    static class CharacterPosePayloadFieldMutation
    {
        public static CharacterPoseNodePayload Set(
            CharacterPoseNodePayload payload,
            IReadOnlyList<SetPoseNodeFieldMutation> fields)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (fields == null || fields.Count == 0)
                throw new ArgumentException("Pose payload field mutation set is empty.", nameof(fields));
            if (fields.Select(value => value.FieldId).Distinct(StringComparer.Ordinal).Count() != fields.Count)
                throw new InvalidOperationException("Pose payload field mutation set contains duplicate fields.");
            if (payload is CharacterClipPlayerPosePayload clip)
                return SetClip(clip, fields);
            CharacterPoseNodePayload current = payload;
            foreach (SetPoseNodeFieldMutation field in fields)
                current = Set(current, field.FieldId, field.Value);
            return current;
        }

        public static CharacterPoseNodePayload Set(CharacterPoseNodePayload payload, string fieldId, object value)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            return payload switch
            {
                CharacterProgramParameterInputPosePayload current when fieldId == "parameter-id" => new CharacterProgramParameterInputPosePayload(Id<PoseParameterId>(value, text => new PoseParameterId(text))),
                CharacterActionPlaybackInputPosePayload current when fieldId == "animation-channel-id" => new CharacterActionPlaybackInputPosePayload(Id<AnimationChannelId>(value, text => new AnimationChannelId(text))),
                CharacterSelectedPosePlayerPayload current => SetSelected(current, fieldId, value),
                CharacterBlendSpacePlayerPosePayload current => SetBlendSpace(current, fieldId, value),
                CharacterClipPlayerPosePayload current => SetClip(current, fieldId, value),
                CharacterAnimationSlotPosePayload current => SetSlot(current, fieldId, value),
                CharacterBlendStackPosePayload current => SetBlendStack(current, fieldId, value),
                CharacterMotionMatchingPosePayload current => CharacterMotionMatchingPoseFieldMutation.Set(current, fieldId, value),
                CharacterPoseHistoryCollectorPayload current => CharacterMotionMatchingPoseFieldMutation.Set(current, fieldId, value),
                CharacterInertializationPosePayload current when fieldId == "inertialization-policy" => new CharacterInertializationPosePayload(Require<CharacterPoseInertializationPolicy>(value, fieldId)),
                CharacterBlendPosePayload current when fieldId == "weight" => new CharacterBlendPosePayload(Convert.ToSingle(value)),
                CharacterLayeredBoneBlendPosePayload current => SetLayered(current, fieldId, value),
                CharacterAdditivePosePayload current => SetAdditive(current, fieldId, value),
                CharacterPoseParameterResolvePayload current when fieldId == "parameter-policies" => new CharacterPoseParameterResolvePayload(Require<CharacterPoseParameterPolicy[]>(value, fieldId)),
                CharacterModifyBonePosePayload current => SetModifyBone(current, fieldId, value),
                CharacterRootOrientationWarpPosePayload current when fieldId == "yaw-curve" => new CharacterRootOrientationWarpPosePayload(Require<ThirdPersonCharacter.Pipeline.Motion.RootMotion.RootMotionCurveAsset>(value, fieldId)),
                CharacterPoseBoneIkGoalsPayload current when fieldId == "bindings" => new CharacterPoseBoneIkGoalsPayload(Require<CharacterPoseBoneIkGoalBinding[]>(value, fieldId)),
                CharacterFootPlacementPosePayload current => SetFootPlacement(current, fieldId, value),
                CharacterFullBodyIkPosePayload current when fieldId == "profile" => new CharacterFullBodyIkPosePayload(Require<CharacterFullBodyIkProfile>(value, fieldId)),
                CharacterPoseSubgraphPayload current when fieldId == "graph-id" => new CharacterPoseSubgraphPayload(Subgraph(value)),
                _ => throw new InvalidOperationException($"Pose payload '{payload.GetType().Name}' does not declare writable field '{fieldId}'.")
            };
        }

        static CharacterPoseNodePayload SetSelected(CharacterSelectedPosePlayerPayload current, string field, object value) => field switch
        {
            "pose-source-slot" => new CharacterSelectedPosePlayerPayload(
                Require<CharacterMotionMatchingPoseSourceSlot>(value, field)),
            _ => Unknown(current, field)
        };

        static CharacterPoseNodePayload SetBlendSpace(CharacterBlendSpacePlayerPosePayload current, string field, object value) => field switch
        {
            "pose-source-slot" => new CharacterBlendSpacePlayerPosePayload(Require<CharacterBlendSpacePoseSourceSlot>(value, field), current.InputRangePolicy),
            "input-range-policy" => new CharacterBlendSpacePlayerPosePayload(current.SourceSlot, EnumValue<CharacterAnimationBlendSpaceInputRangePolicy>(value)),
            _ => Unknown(current, field)
        };

        static CharacterPoseNodePayload SetClip(CharacterClipPlayerPosePayload current, string field, object value)
        {
            CharacterClipPlayerClockSource clockSource = field == "clock-source"
                ? EnumValue<CharacterClipPlayerClockSource>(value)
                : current.ClockSource;
            return new CharacterClipPlayerPosePayload(
                field == "pose-source-slot" ? Require<CharacterClipPoseSourceSlot>(value, field) : current.SourceSlot,
                field == "play-rate" ? Convert.ToSingle(value) : current.PlayRate,
                field == "initial-time" ? Convert.ToSingle(value) : current.InitialTime,
                clockSource);
        }

        static CharacterPoseNodePayload SetClip(
            CharacterClipPlayerPosePayload current,
            IReadOnlyList<SetPoseNodeFieldMutation> fields)
        {
            var values = fields.ToDictionary(value => value.FieldId, value => value.Value, StringComparer.Ordinal);
            CharacterClipPlayerClockSource clockSource = values.TryGetValue("clock-source", out object clockSourceValue)
                ? EnumValue<CharacterClipPlayerClockSource>(clockSourceValue)
                : current.ClockSource;
            return new CharacterClipPlayerPosePayload(
                values.TryGetValue("pose-source-slot", out object source)
                    ? Require<CharacterClipPoseSourceSlot>(source, "pose-source-slot")
                    : current.SourceSlot,
                values.TryGetValue("play-rate", out object playRate) ? Convert.ToSingle(playRate) : current.PlayRate,
                values.TryGetValue("initial-time", out object initialTime) ? Convert.ToSingle(initialTime) : current.InitialTime,
                clockSource);
        }

        static CharacterPoseNodePayload SetSlot(CharacterAnimationSlotPosePayload current, string field, object value) => new CharacterAnimationSlotPosePayload(
            field == "slot-id" ? Id<AnimationSlotId>(value, text => new AnimationSlotId(text)) : current.SlotId,
            field == "animation-channel-id" ? Id<AnimationChannelId>(value, text => new AnimationChannelId(text)) : current.AnimationChannelId,
            field == "selection-availability" ? EnumValue<AnimationSelectionAvailabilityPolicy>(value) : current.SelectionAvailability,
            field == "blend-policy" ? Require<CharacterAnimationBlendPolicy>(value, field) : current.BlendPolicy);

        static CharacterPoseNodePayload SetBlendStack(CharacterBlendStackPosePayload current, string field, object value) => new CharacterBlendStackPosePayload(
            field == "pose-source-slot" ? Require<CharacterMotionMatchingPoseSourceSlot>(value, field) : current.SourceSlot,
            field == "blend-policy" ? Require<CharacterAnimationBlendPolicy>(value, field) : current.BlendPolicy);

        static CharacterPoseNodePayload SetLayered(CharacterLayeredBoneBlendPosePayload current, string field, object value) => new CharacterLayeredBoneBlendPosePayload(
            field == "bone-mask" ? Require<CharacterAnimationBoneMaskAsset>(value, field) : current.BoneMask,
            field == "weight" ? Convert.ToSingle(value) : current.Weight);

        static CharacterPoseNodePayload SetAdditive(CharacterAdditivePosePayload current, string field, object value) => new CharacterAdditivePosePayload(
            field == "reference-pose-id" ? Convert.ToString(value) : current.ReferencePoseId,
            field == "reference-space" ? EnumValue<AdditiveReferenceSpace>(value) : current.ReferenceSpace,
            field == "scale-policy" ? EnumValue<AdditiveScalePolicy>(value) : current.ScalePolicy,
            field == "weight" ? Convert.ToSingle(value) : current.Weight);

        static CharacterPoseNodePayload SetModifyBone(CharacterModifyBonePosePayload current, string field, object value) => new CharacterModifyBonePosePayload(
            field == "bone-id" ? Id<AnimationBoneId>(value, text => new AnimationBoneId(text)) : current.BoneId,
            field == "reference-space" ? EnumValue<ModifyBoneReferenceSpace>(value) : current.ReferenceSpace,
            field == "operations" ? EnumValue<ModifyBoneOperationMask>(value) : current.Operations,
            field == "position" ? Require<Vector3>(value, field) : current.Position,
            field == "rotation" ? Require<Quaternion>(value, field).eulerAngles : current.Rotation.eulerAngles,
            field == "scale" ? Require<Vector3>(value, field) : current.Scale);

        static CharacterPoseNodePayload SetFootPlacement(CharacterFootPlacementPosePayload current, string field, object value) => new CharacterFootPlacementPosePayload(
            field == "profile" ? Require<CharacterFootPlacementProfile>(value, field) : current.Profile,
            field == "calibration" ? Require<CharacterFootPlacementRigCalibration>(value, field) : current.Calibration);

        static CharacterPoseSubgraphReference Subgraph(object value)
        {
            var result = new CharacterPoseSubgraphReference();
            result.Assign(Id<PoseGraphId>(value, text => new PoseGraphId(text)));
            return result;
        }

        static TId Id<TId>(object value, Func<string, TId> create) => create(Convert.ToString(value));
        static T Require<T>(object value, string field) => value is T typed ? typed : throw new InvalidOperationException($"Pose field '{field}' requires '{typeof(T).Name}'.");
        static T EnumValue<T>(object value) where T : struct => value is T typed ? typed : Enum.Parse<T>(Convert.ToString(value), false);
        static CharacterPoseNodePayload Unknown(CharacterPoseNodePayload payload, string field) => throw new InvalidOperationException($"Pose payload '{payload.GetType().Name}' does not declare writable field '{field}'.");
    }
}
