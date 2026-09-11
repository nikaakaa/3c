using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal enum CharacterPoseIrGraphRole : byte
    {
        Root = 1,
        StateLocal = 2,
        Subgraph = 3,
        LinkedPoseEntry = 4,
        MotionMatchingEntry = 5,
        AnimationLayer = 6,
        ControlRig = 7
    }

    internal enum CharacterPoseNativeNodeRole : byte
    {
        Operation = 1,
        GraphInput = 2,
        GraphOutput = 3,
        Subgraph = 4,
        PoseOutput = 5
    }

    internal abstract class CharacterPoseNodeDefinition<TPayload> :
        CharacterPoseNodeDefinition
        where TPayload : CharacterPoseNodePayload, new()
    {
        public abstract override CharacterPoseNodeKind Kind { get; }
        public override Type PayloadType => typeof(TPayload);
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.Operation;

        public override CharacterPoseIrNode Lower(CharacterPoseCanvasNode node, IReadOnlyList<CharacterPoseIrInput> inputs, string sourcePath)
        {
            if (!(node.Payload is TPayload payload) || node.Kind != Kind)
                throw new InvalidOperationException($"{sourcePath}: payload does not match Node Definition '{Kind}'.");
            Validate(payload, sourcePath);
            return new CharacterPoseIrNode(
                new CharacterPoseIrNodeId(node.NodeId.Value),
                GetSourceMapName(payload),
                payload,
                inputs,
                sourcePath);
        }

        public override CharacterPresentationPoseSourceSlot Source(
            CharacterPoseNodePayload payload) =>
            GetSource(Require(payload));

        public override AnimationChannelId Channel(
            CharacterPoseNodePayload payload) =>
            GetChannel(Require(payload));

        public override PoseParameterId Parameter(
            CharacterPoseNodePayload payload) =>
            GetParameter(Require(payload));

        public override AnimationSelectionAvailabilityPolicy Availability(
            CharacterPoseNodePayload payload,
            bool stateLocal) =>
            GetAvailability(Require(payload), stateLocal);

        public override CharacterAnimationBlendSpaceInputRangePolicy InputRange(
            CharacterPoseNodePayload payload) =>
            GetInputRange(Require(payload));

        public override float Weight(CharacterPoseNodePayload payload) =>
            GetWeight(Require(payload));

        public override CharacterAnimationBoneMaskAsset BoneMask(
            CharacterPoseNodePayload payload) =>
            GetBoneMask(Require(payload));

        public override IReadOnlyList<CharacterPoseParameterPolicy>
            ParameterPolicies(CharacterPoseNodePayload payload) =>
            GetParameterPolicies(Require(payload)) ??
            Array.Empty<CharacterPoseParameterPolicy>();

        public override void RequirePayload(
            CharacterPoseNodePayload payload)
        {
            Require(payload);
        }

        public override void ValidatePayload(
            CharacterPoseNodePayload payload,
            string sourcePath) =>
            Validate(Require(payload), sourcePath);

        public override void ValidateRig(
            CharacterPoseNodePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath) =>
            ValidateRig(
                Require(payload),
                rig ??
                throw new ArgumentNullException(nameof(rig)),
                sourcePath);

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new TPayload();

        public override object ReadField(
            CharacterPoseNodePayload payload,
            string field) =>
            ReadField(Require(payload), field);

        public override IReadOnlyList<CharacterPoseGraphDependency>
            ProjectGraphDependencies(CharacterPoseNodePayload payload) =>
            GetGraphDependencies(Require(payload)) ??
            Array.Empty<CharacterPoseGraphDependency>();

        public override string ProjectChildDocumentId(
            CharacterPoseNodePayload payload) =>
            GetChildDocumentId(Require(payload)) ?? string.Empty;

        public override string SourceMapName(CharacterPoseNodePayload payload) =>
            GetSourceMapName(Require(payload));

        protected virtual CharacterPresentationPoseSourceSlot GetSource(
            TPayload payload) => null;

        protected virtual AnimationChannelId GetChannel(
            TPayload payload) => default;

        protected virtual PoseParameterId GetParameter(
            TPayload payload) => default;

        protected virtual AnimationSelectionAvailabilityPolicy
            GetAvailability(TPayload payload, bool stateLocal) =>
            AnimationSelectionAvailabilityPolicy.RequireSelection;

        protected virtual
            CharacterAnimationBlendSpaceInputRangePolicy
            GetInputRange(TPayload payload) =>
            CharacterAnimationBlendSpaceInputRangePolicy.Clamp;

        protected virtual float GetWeight(TPayload payload) => 1f;

        protected virtual CharacterAnimationBoneMaskAsset
            GetBoneMask(TPayload payload) => null;

        protected virtual IReadOnlyList<CharacterPoseParameterPolicy>
            GetParameterPolicies(TPayload payload) =>
            Array.Empty<CharacterPoseParameterPolicy>();

        protected virtual IReadOnlyList<CharacterPoseGraphDependency>
            GetGraphDependencies(TPayload payload) =>
            Array.Empty<CharacterPoseGraphDependency>();

        protected virtual string GetChildDocumentId(TPayload payload) =>
            string.Empty;

        protected virtual string GetSourceMapName(TPayload payload) =>
            CapabilityIdentity;

        protected virtual object ReadField(
            TPayload payload,
            string field) =>
            throw new InvalidOperationException(
                $"Pose capability '{CapabilityIdentity}' does not declare field '{field}'.");

        protected virtual void Validate(
            TPayload payload,
            string sourcePath)
        {
        }

        protected virtual void ValidateRig(
            TPayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
        }

        TPayload Require(CharacterPoseNodePayload payload) =>
            payload is TPayload typed
                ? typed
                : throw new InvalidOperationException(
                    $"Pose Node Definition '{CapabilityIdentity}' received payload '{payload?.GetType().Name ?? "<null>"}'.");
    }

    internal static class CharacterPoseNodeDefinitionValidation
    {
        public static void Require(
            bool condition,
            string path,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException($"{path}: {message}");
        }

        public static void RequireWeight(float value, string path) =>
            Require(
                float.IsFinite(value) &&
                value >= 0f &&
                value <= 1f,
                path,
                "Pose weight must be finite and in [0, 1].");

        public static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);

        public static bool Finite(Quaternion value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z) &&
            float.IsFinite(value.w);
    }

    internal sealed class
        CharacterProgramParameterInputPoseNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterProgramParameterInputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ProgramParameterInput;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.ProgramParameterInput;

        protected override PoseParameterId GetParameter(
            CharacterProgramParameterInputPosePayload payload) =>
            payload.ParameterId;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterProgramParameterInputPosePayload(
                new PoseParameterId(
                    input.Require<string>("parameter-id")));

        protected override object ReadField(
            CharacterProgramParameterInputPosePayload payload,
            string field) =>
            field == "parameter-id"
                ? payload.ParameterId.Value
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterProgramParameterInputPosePayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.ParameterId.IsValid,
                sourcePath,
                "Parameter identity is missing.");
    }

    internal sealed class
        CharacterActionPlaybackInputPoseNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterActionPlaybackInputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ActionPlaybackInput;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.ActionPlaybackInput;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.AnimationChannel |
                CharacterPoseNodeRuntimeRequirement.ActionPlaybackControl;

        protected override AnimationChannelId GetChannel(
            CharacterActionPlaybackInputPosePayload payload) =>
            payload.AnimationChannelId;

        protected override
            AnimationSelectionAvailabilityPolicy GetAvailability(
                CharacterActionPlaybackInputPosePayload payload,
                bool stateLocal) =>
            AnimationSelectionAvailabilityPolicy.AllowEmpty;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterActionPlaybackInputPosePayload(
                new AnimationChannelId(
                    input.Require<string>(
                        "animation-channel-id")));

        protected override object ReadField(
            CharacterActionPlaybackInputPosePayload payload,
            string field) =>
            field == "animation-channel-id"
                ? payload.AnimationChannelId.Value
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterActionPlaybackInputPosePayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.AnimationChannelId.IsValid,
                sourcePath,
                "Animation Channel identity is missing.");
    }

    internal sealed class
        CharacterSelectedPosePlayerNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterSelectedPosePlayerPayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.SelectedPosePlayer;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.SelectedPosePlayer;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.Player;

        protected override CharacterPresentationPoseSourceSlot GetSource(
            CharacterSelectedPosePlayerPayload payload) =>
            payload.SourceSlot;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterSelectedPosePlayerPayload(
                input.Require<CharacterPresentationPoseSourceSlot>(
                    "pose-source-slot"));

        protected override object ReadField(
            CharacterSelectedPosePlayerPayload payload,
            string field) =>
            field == "pose-source-slot"
                ? payload.SourceSlot
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterSelectedPosePlayerPayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.SourceSlot,
                sourcePath,
                "Selected Pose source binding is incomplete.");
    }

    internal sealed class
        CharacterBlendSpacePlayerPoseNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterBlendSpacePlayerPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.BlendSpacePlayer;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.BlendSpacePlayer;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.Player;
        protected override CharacterPresentationPoseSourceSlot GetSource(
            CharacterBlendSpacePlayerPosePayload payload) =>
            payload.SourceSlot;

        protected override
            CharacterAnimationBlendSpaceInputRangePolicy
            GetInputRange(
                CharacterBlendSpacePlayerPosePayload payload) =>
            payload.InputRangePolicy;

        protected override
            AnimationSelectionAvailabilityPolicy GetAvailability(
                CharacterBlendSpacePlayerPosePayload payload,
                bool stateLocal) =>
            stateLocal
                ? AnimationSelectionAvailabilityPolicy.AllowEmpty
                : AnimationSelectionAvailabilityPolicy
                    .RequireSelection;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterBlendSpacePlayerPosePayload(
                input.Require<CharacterBlendSpacePoseSourceSlot>("pose-source-slot"),
                Enum.Parse<
                    CharacterAnimationBlendSpaceInputRangePolicy>(
                    input.Require<string>(
                        "input-range-policy"),
                    false));

        protected override object ReadField(
            CharacterBlendSpacePlayerPosePayload payload,
            string field) =>
            field switch
            {
                "pose-source-slot" => payload.SourceSlot,
                "input-range-policy" =>
                    payload.InputRangePolicy.ToString(),
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterBlendSpacePlayerPosePayload payload,
            string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(
                payload.SourceSlot,
                sourcePath,
                "Blend Space source slot is missing.");
        }
    }

    internal sealed class
        CharacterClipPlayerPoseNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterClipPlayerPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ClipPlayer;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.ClipPlayer;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.Player |
                CharacterPoseNodeRuntimeRequirement.ClipPlayer;
        protected override CharacterPresentationPoseSourceSlot GetSource(
            CharacterClipPlayerPosePayload payload) =>
            payload.SourceSlot;

        protected override
            AnimationSelectionAvailabilityPolicy GetAvailability(
                CharacterClipPlayerPosePayload payload,
                bool stateLocal) =>
            stateLocal
                ? AnimationSelectionAvailabilityPolicy.AllowEmpty
                : AnimationSelectionAvailabilityPolicy
                    .RequireSelection;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterClipPlayerPosePayload(
                input.Require<CharacterClipPoseSourceSlot>("pose-source-slot"),
                input.Require<float>("play-rate"),
                input.Require<float>("initial-time"),
                input.Require<bool>("loop-animation"),
                Enum.Parse<CharacterClipPlayerClockSource>(
                    input.Require<string>("clock-source"),
                    false));

        protected override object ReadField(
            CharacterClipPlayerPosePayload payload,
            string field) =>
            field switch
            {
                "pose-source-slot" => payload.SourceSlot,
                "play-rate" => payload.PlayRate,
                "initial-time" => payload.InitialTime,
                "loop-animation" => payload.LoopAnimation,
                "clock-source" => payload.ClockSource.ToString(),
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterClipPlayerPosePayload payload,
            string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(
                payload.SourceSlot,
                sourcePath,
                "Clip source slot is missing.");
            CharacterPoseNodeDefinitionValidation.Require(
                float.IsFinite(payload.PlayRate) &&
                payload.PlayRate > 0f,
                sourcePath,
                "Clip play rate must be finite and positive.");
            CharacterPoseNodeDefinitionValidation.Require(
                float.IsFinite(payload.InitialTime) &&
                payload.InitialTime >= 0f,
                sourcePath,
                "Clip initial time must be finite and non-negative.");
            CharacterPoseNodeDefinitionValidation.Require(
                Enum.IsDefined(typeof(CharacterClipPlayerClockSource), payload.ClockSource),
                sourcePath,
                "Clip clock source is invalid.");
        }
    }

    internal sealed class
        CharacterPoseStateMachineNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterPoseStateMachineNodePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.PoseStateMachine;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.PoseStateMachine;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.StateMachine;

        protected override void Validate(
            CharacterPoseStateMachineNodePayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.StateMachine != null,
                sourcePath,
                "Pose StateMachine is missing.");

        protected override IReadOnlyList<CharacterPoseGraphDependency>
            GetGraphDependencies(
                CharacterPoseStateMachineNodePayload payload) =>
            payload.StateMachine?.States
                .Where(value => value?.PoseGraphId.IsValid == true)
                .Select(value => new CharacterPoseGraphDependency(
                    CharacterPoseGraphDependencyKind.StatePose,
                    value.PoseGraphId,
                    value.StateId.Value))
                .ToArray() ?? Array.Empty<CharacterPoseGraphDependency>();

        protected override string GetChildDocumentId(
            CharacterPoseStateMachineNodePayload payload) =>
            payload.StateMachine?.StateMachineId.Value ?? string.Empty;

        public override IReadOnlyList<CharacterPoseResourceSlot>
            ProjectResourceSlots(
                CharacterPoseNodePayload payload) =>
            (payload as CharacterPoseStateMachineNodePayload)?.StateMachine?.Transitions
                .SelectMany(transition => new[]
                {
                    transition?.CustomBlendCurveSlot,
                    transition?.BlendProfileSlot
                })
                .OfType<CharacterPoseResourceSlot>()
                .Distinct()
                .ToArray() ?? Array.Empty<CharacterPoseResourceSlot>();

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPoseStateMachineNodePayload(
                input.RequireStateMachine());
    }

    internal sealed class
        CharacterAnimationSlotPoseNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterAnimationSlotPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.AnimationSlot;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.AnimationSlot;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.AnimationChannel |
                CharacterPoseNodeRuntimeRequirement.Player |
                CharacterPoseNodeRuntimeRequirement.ActionPlaybackControl |
                CharacterPoseNodeRuntimeRequirement.BlendPolicy |
                CharacterPoseNodeRuntimeRequirement.AnimationSlot;

        protected override AnimationChannelId GetChannel(
            CharacterAnimationSlotPosePayload payload) =>
            payload.AnimationChannelId;

        protected override
            AnimationSelectionAvailabilityPolicy GetAvailability(
                CharacterAnimationSlotPosePayload payload,
                bool stateLocal) =>
            payload.SelectionAvailability;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterAnimationSlotPosePayload(
                new AnimationSlotId(
                    input.Require<string>("slot-id")),
                new AnimationChannelId(
                    input.Require<string>(
                        "animation-channel-id")),
                Enum.Parse<
                    AnimationSelectionAvailabilityPolicy>(
                    input.Require<string>(
                        "selection-availability"),
                    false),
                input.Require<CharacterPoseResourceSlot>(
                    "blend-policy"));

        protected override object ReadField(
            CharacterAnimationSlotPosePayload payload,
            string field) =>
            field switch
            {
                "slot-id" => payload.SlotId.Value,
                "animation-channel-id" =>
                    payload.AnimationChannelId.Value,
                "selection-availability" =>
                    payload.SelectionAvailability.ToString(),
                "blend-policy" => payload.BlendPolicySlot,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterAnimationSlotPosePayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.SlotId.IsValid &&
                payload.AnimationChannelId.IsValid &&
                payload.SelectionAvailability ==
                AnimationSelectionAvailabilityPolicy.AllowEmpty &&
                payload.BlendPolicySlot,
                sourcePath,
                "Animation Slot requires identity, channel, AllowEmpty and one Blend Policy.");

        protected override void ValidateRig(
            CharacterAnimationSlotPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                rig.RequireAnimationSlot(payload.SlotId);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{sourcePath}: {exception.Message}",
                    exception);
            }
        }
    }

    internal sealed class CharacterBlendStackPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterBlendStackPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.BlendStack;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.BlendStack;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.PoseSourceSlot |
                CharacterPoseNodeRuntimeRequirement.Player |
                CharacterPoseNodeRuntimeRequirement.BlendPolicy;

        protected override CharacterPresentationPoseSourceSlot GetSource(
            CharacterBlendStackPosePayload payload) =>
            payload.SourceSlot;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterBlendStackPosePayload(
                input.Require<CharacterPresentationPoseSourceSlot>(
                    "pose-source-slot"),
                input.Require<CharacterPoseResourceSlot>(
                    "blend-policy"));

        protected override object ReadField(
            CharacterBlendStackPosePayload payload,
            string field) =>
            field switch
            {
                "pose-source-slot" => payload.SourceSlot,
                "blend-policy" => payload.BlendPolicySlot,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterBlendStackPosePayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.SourceSlot &&
                payload.BlendPolicySlot,
                sourcePath,
                "Blend Stack source binding or Blend Policy is incomplete.");

        protected override void ValidateRig(
            CharacterBlendStackPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
        }
    }

    internal sealed class
        CharacterInertializationPoseNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterInertializationPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.Inertialization;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.Inertialization;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.Inertialization;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterInertializationPosePayload(
                input.Require<CharacterPoseResourceSlot>(
                    "inertialization-policy"));

        protected override object ReadField(
            CharacterInertializationPosePayload payload,
            string field) =>
            field == "inertialization-policy"
                ? payload.PolicySlot
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterInertializationPosePayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.PolicySlot,
                sourcePath,
                "Inertialization Policy is missing.");

        protected override void ValidateRig(
            CharacterInertializationPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
        }
    }

    internal sealed class CharacterBlendPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterBlendPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.BlendPose;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.BlendPose;

        protected override float GetWeight(
            CharacterBlendPosePayload payload) =>
            payload.Weight;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterBlendPosePayload(
                input.Require<float>("weight"));

        protected override object ReadField(
            CharacterBlendPosePayload payload,
            string field) =>
            field == "weight"
                ? payload.Weight
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterBlendPosePayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.RequireWeight(
                payload.Weight,
                sourcePath);
    }

    internal sealed class
        CharacterLayeredBoneBlendPoseNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterLayeredBoneBlendPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.LayeredBoneBlend;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.LayeredBoneBlend;

        protected override float GetWeight(
            CharacterLayeredBoneBlendPosePayload payload) =>
            payload.Weight;

        protected override CharacterAnimationBoneMaskAsset
            GetBoneMask(
                CharacterLayeredBoneBlendPosePayload payload) =>
            null;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterLayeredBoneBlendPosePayload(
                input.Require<CharacterPoseResourceSlot>(
                    "bone-mask"),
                input.Require<CharacterLayeredBoneBlendSpace>(
                    "blend-space"),
                input.Require<float>("weight"));

        protected override object ReadField(
            CharacterLayeredBoneBlendPosePayload payload,
            string field) =>
            field switch
            {
                "bone-mask" => payload.BoneMaskSlot,
                "blend-space" => payload.BlendSpace,
                "weight" => payload.Weight,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterLayeredBoneBlendPosePayload payload,
            string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.RequireWeight(
                payload.Weight,
                sourcePath);
            CharacterPoseNodeDefinitionValidation.Require(
                payload.BoneMaskSlot,
                sourcePath,
                "Layered Bone Blend mask is missing.");
            CharacterPoseNodeDefinitionValidation.Require(
                Enum.IsDefined(
                    typeof(CharacterLayeredBoneBlendSpace),
                    payload.BlendSpace),
                sourcePath,
                "Layered Bone Blend pose space is invalid.");
        }

        protected override void ValidateRig(
            CharacterLayeredBoneBlendPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
        }
    }

    internal sealed class CharacterAdditivePoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterAdditivePosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.AdditivePose;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.AdditivePose;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.Additive;

        protected override float GetWeight(
            CharacterAdditivePosePayload payload) =>
            payload.Weight;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterAdditivePosePayload(
                input.Require<string>("reference-pose-id"),
                Enum.Parse<AdditiveReferenceSpace>(
                    input.Require<string>("reference-space"),
                    false),
                Enum.Parse<AdditiveScalePolicy>(
                    input.Require<string>("scale-policy"),
                    false),
                input.Require<float>("weight"));

        protected override object ReadField(
            CharacterAdditivePosePayload payload,
            string field) =>
            field switch
            {
                "reference-pose-id" =>
                    payload.ReferencePoseId,
                "reference-space" =>
                    payload.ReferenceSpace.ToString(),
                "scale-policy" =>
                    payload.ScalePolicy.ToString(),
                "weight" => payload.Weight,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterAdditivePosePayload payload,
            string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.RequireWeight(
                payload.Weight,
                sourcePath);
            CharacterPoseNodeDefinitionValidation.Require(
                string.Equals(
                    payload.ReferencePoseId,
                    AnimationAdditiveReferencePoseIds.RigReference,
                    StringComparison.Ordinal) &&
                Enum.IsDefined(
                    typeof(AdditiveReferenceSpace),
                    payload.ReferenceSpace) &&
                Enum.IsDefined(
                    typeof(AdditiveScalePolicy),
                    payload.ScalePolicy),
                sourcePath,
                "Additive Pose reference configuration is invalid.");
        }
    }

    internal sealed class
        CharacterPoseParameterResolveNodeDefinition :
            CharacterPoseNodeDefinition<
                CharacterPoseParameterResolvePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.PoseParameterResolve;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.PoseParameterResolve;

        protected override IReadOnlyList<
            CharacterPoseParameterPolicy> GetParameterPolicies(
            CharacterPoseParameterResolvePayload payload) =>
            payload.Policies;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPoseParameterResolvePayload(
                input.Require<CharacterPoseParameterPolicy[]>(
                    "parameter-policies"));

        protected override object ReadField(
            CharacterPoseParameterResolvePayload payload,
            string field) =>
            field == "parameter-policies"
                ? payload.Policies.ToArray()
                : base.ReadField(payload, field);
    }

    internal sealed class CharacterModifyBonePoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterModifyBonePosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ModifyBone;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.ModifyBone;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.ModifyBone;

        protected override void Validate(
            CharacterModifyBonePosePayload payload,
            string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(
                payload.BoneId.IsValid &&
                Enum.IsDefined(
                    typeof(ModifyBoneReferenceSpace),
                    payload.ReferenceSpace) &&
                payload.Operations !=
                ModifyBoneOperationMask.None &&
                CharacterPoseNodeDefinitionValidation.Finite(
                    payload.Position) &&
                CharacterPoseNodeDefinitionValidation.Finite(
                    payload.Rotation) &&
                CharacterPoseNodeDefinitionValidation.Finite(
                    payload.Scale),
                sourcePath,
                "Modify Bone configuration is invalid.");
        }

        protected override void ValidateRig(
            CharacterModifyBonePosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                rig.RequirePhysicalBoneIndex(payload.BoneId);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{sourcePath}: {exception.Message}",
                    exception);
            }
        }

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterModifyBonePosePayload(
                new AnimationBoneId(
                    input.Require<string>("bone-id")),
                Enum.Parse<ModifyBoneReferenceSpace>(
                    input.Require<string>("reference-space"),
                    false),
                Enum.Parse<ModifyBoneOperationMask>(
                    input.Require<string>("operations"),
                    false),
                input.Require<Vector3>("position"),
                input.Require<Quaternion>("rotation")
                    .eulerAngles,
                input.Require<Vector3>("scale"));

        protected override object ReadField(
            CharacterModifyBonePosePayload payload,
            string field) =>
            field switch
            {
                "bone-id" => payload.BoneId.Value,
                "reference-space" =>
                    payload.ReferenceSpace.ToString(),
                "operations" =>
                    payload.Operations.ToString(),
                "position" => payload.Position,
                "rotation" => payload.Rotation,
                "scale" => payload.Scale,
                _ => base.ReadField(payload, field)
            };
    }

    internal sealed class CharacterRootOrientationWarpPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterRootOrientationWarpPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.RootOrientationWarp;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.RootOrientationWarp;
        public override CharacterPoseNodeRuntimeRequirement
            RuntimeRequirements =>
                CharacterPoseNodeRuntimeRequirement.RootOrientationWarp;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterRootOrientationWarpPosePayload(
                input.Require<CharacterPoseResourceSlot>("yaw-curve"));

        protected override object ReadField(
            CharacterRootOrientationWarpPosePayload payload,
            string field) =>
            field == "yaw-curve"
                ? payload.YawCurveSlot
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterRootOrientationWarpPosePayload payload,
            string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(
                payload.YawCurveSlot,
                sourcePath,
                "Root Orientation Warp Yaw Resource Slot is missing.");
        }
    }

    internal sealed class CharacterPoseSubgraphNodeDefinition :
        CharacterPoseNodeDefinition<CharacterPoseSubgraphPayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.PoseSubgraph;
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.Subgraph;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input)
        {
            var subgraph = new CharacterPoseSubgraphReference();
            subgraph.Assign(
                new PoseGraphId(
                    input.Require<string>("graph-id")));
            return new CharacterPoseSubgraphPayload(subgraph);
        }

        protected override object ReadField(
            CharacterPoseSubgraphPayload payload,
            string field) =>
            field == "graph-id"
                ? payload.Subgraph?.PoseGraphId.Value ??
                  string.Empty
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterPoseSubgraphPayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.Subgraph?.PoseGraphId.IsValid == true,
                sourcePath,
                "Pose Subgraph target is missing.");

        protected override IReadOnlyList<CharacterPoseGraphDependency>
            GetGraphDependencies(CharacterPoseSubgraphPayload payload) =>
            payload.Subgraph?.PoseGraphId.IsValid == true
                ? new[]
                {
                    new CharacterPoseGraphDependency(
                        CharacterPoseGraphDependencyKind.Subgraph,
                        payload.Subgraph.PoseGraphId,
                        payload.Subgraph.PoseGraphId.Value)
                }
                : Array.Empty<CharacterPoseGraphDependency>();

    }

    internal sealed class CharacterLocalToComponentPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterLocalToComponentPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.LocalToComponentPose;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.LocalToComponentPose;
    }

    internal sealed class CharacterComponentToLocalPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterComponentToLocalPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ComponentToLocalPose;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.ComponentToLocalPose;
    }

    internal sealed class CharacterGraphInputPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterGraphInputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.GraphInput;
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.GraphInput;
    }

    internal sealed class CharacterGraphOutputPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterGraphOutputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.GraphOutput;
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.GraphOutput;
    }

    internal sealed class CharacterOutputPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterOutputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.OutputPose;
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.PoseOutput;
        public override CharacterPoseOperationCode OperationCode =>
            CharacterPoseOperationCode.OutputPose;
    }

    internal sealed class CharacterPoseIrTopologyCompiler
    {
        public CharacterPoseIrGraph Compile(
            CharacterPoseTypedIrGraph source,
            CharacterPoseIrGraphRole role)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            CharacterPoseCanvasGraph graph = source.Source;
            IReadOnlyDictionary<PoseNodeId, CharacterPoseCanvasNode> nodes =
                source.AuthoredNodes;
            IReadOnlyDictionary<PoseNodeId,
                IReadOnlyList<CharacterPoseCanvasConnection>> incoming =
                source.Incoming;
            List<CharacterPoseCanvasNode> ordered = TopologicalOrder(nodes, incoming);
            ValidateBoundary(role, ordered);
            var loweredNodes = new List<CharacterPoseIrNode>(ordered.Count);
            foreach (CharacterPoseCanvasNode node in ordered)
                loweredNodes.Add(source.RequireNode(node.NodeId));
            CharacterPoseCanvasNode output =
                role != CharacterPoseIrGraphRole.Subgraph &&
                role != CharacterPoseIrGraphRole.LinkedPoseEntry &&
                role != CharacterPoseIrGraphRole.MotionMatchingEntry &&
                role != CharacterPoseIrGraphRole.AnimationLayer &&
                role != CharacterPoseIrGraphRole.ControlRig
                ? ordered.Single(value => value.Kind == CharacterPoseNodeKind.OutputPose)
                : ordered.Single(value => value.Kind == CharacterPoseNodeKind.GraphOutput);
            return new CharacterPoseIrGraph(graph.GraphId, graph.ContentRevision, loweredNodes, new CharacterPoseIrNodeId(output.NodeId.Value));
        }

        static List<CharacterPoseCanvasNode> TopologicalOrder(
            IReadOnlyDictionary<PoseNodeId, CharacterPoseCanvasNode> nodes,
            IReadOnlyDictionary<PoseNodeId,
                IReadOnlyList<CharacterPoseCanvasConnection>> incoming)
        {
            var indegree = incoming.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
                    .Where(edge => !IsTemporalHistoryEdge(nodes, edge))
                    .Select(value => value.SourceNodeId)
                    .Distinct()
                    .Count());
            var outgoing = nodes.Keys.ToDictionary(value => value, _ => new HashSet<PoseNodeId>());
            foreach (KeyValuePair<PoseNodeId,
                         IReadOnlyList<CharacterPoseCanvasConnection>> pair in incoming)
                foreach (CharacterPoseCanvasConnection edge in pair.Value)
                    if (!IsTemporalHistoryEdge(nodes, edge))
                        outgoing[edge.SourceNodeId].Add(pair.Key);
            var ready = new SortedSet<PoseNodeId>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
            var result = new List<CharacterPoseCanvasNode>(nodes.Count);
            while (ready.Count > 0)
            {
                PoseNodeId id = ready.Min;
                ready.Remove(id);
                result.Add(nodes[id]);
                foreach (PoseNodeId target in outgoing[id])
                {
                    indegree[target]--;
                    if (indegree[target] == 0)
                        ready.Add(target);
                }
            }
            if (result.Count != nodes.Count)
                throw new InvalidOperationException("Pose Graph contains a cycle.");
            return result;
        }

        static bool IsTemporalHistoryEdge(
            IReadOnlyDictionary<PoseNodeId, CharacterPoseCanvasNode> nodes,
            CharacterPoseCanvasConnection edge) =>
            ResolvePort(
                nodes[edge.SourceNodeId],
                edge.SourcePortId.Value,
                CharacterPosePortDirection.Output).Kind == CharacterPosePortKind.PoseHistory;

        static CharacterPosePortDefinition ResolvePort(
            CharacterPoseCanvasNode node,
            string portId,
            CharacterPosePortDirection direction) =>
            CharacterPoseAuthoringPortProjection.Require(
                node,
                portId,
                direction);

        static void ValidateBoundary(CharacterPoseIrGraphRole role, IReadOnlyList<CharacterPoseCanvasNode> nodes)
        {
            int rootOutputs = nodes.Count(value => value.Kind == CharacterPoseNodeKind.OutputPose);
            int graphOutputs = nodes.Count(value => value.Kind == CharacterPoseNodeKind.GraphOutput);
            bool graphBoundary =
                role == CharacterPoseIrGraphRole.Subgraph ||
                role == CharacterPoseIrGraphRole.LinkedPoseEntry ||
                role == CharacterPoseIrGraphRole.MotionMatchingEntry ||
                role == CharacterPoseIrGraphRole.AnimationLayer ||
                role == CharacterPoseIrGraphRole.ControlRig;
            if (!graphBoundary && (rootOutputs != 1 || graphOutputs != 0))
                throw new InvalidOperationException("Root and state-local Pose Graphs must contain exactly one Output Pose and no Graph Output.");
            if (graphBoundary && (graphOutputs != 1 || rootOutputs != 0))
                throw new InvalidOperationException("Pose Subgraphs and Linked Pose Entries must contain exactly one Graph Output and no Output Pose.");
        }
    }
}
