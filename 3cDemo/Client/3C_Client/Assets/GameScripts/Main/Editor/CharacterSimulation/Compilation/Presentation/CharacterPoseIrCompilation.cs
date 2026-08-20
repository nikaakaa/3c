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
        LinkedPoseEntry = 4
    }

    internal enum CharacterPoseNativeNodeRole : byte
    {
        Operation = 1,
        GraphInput = 2,
        GraphOutput = 3,
        Subgraph = 4,
        PoseOutput = 5
    }

    internal interface ICharacterPoseCompilerHandler
    {
        CharacterPoseNodeKind Kind { get; }
        string BindingId { get; }
        string CapabilityIdentity { get; }
        Type PayloadType { get; }
        CharacterPoseNativeNodeRole NativeRole { get; }
        CharacterPoseExecutionDomain ExecutionDomain { get; }
        CharacterPoseOperationCode Code { get; }
        bool Player { get; }
        bool ActionPlaybackControl { get; }
        bool BlendPolicy { get; }
        bool StateMachine { get; }
        bool AnimationSlot { get; }
        bool Inertialization { get; }
        bool Additive { get; }
        bool ModifyBone { get; }
        bool RootOrientationWarp { get; }
        bool ClipPlayer { get; }
        CharacterPresentationPoseSourceSlot Source(
            CharacterPoseNodePayload payload);
        AnimationChannelId Channel(
            CharacterPoseNodePayload payload);
        PoseParameterId Parameter(
            CharacterPoseNodePayload payload);
        AnimationSelectionAvailabilityPolicy Availability(
            CharacterPoseNodePayload payload,
            bool stateLocal);
        CharacterAnimationBlendSpaceInputRangePolicy InputRange(
            CharacterPoseNodePayload payload);
        float Weight(CharacterPoseNodePayload payload);
        CharacterAnimationBoneMaskAsset BoneMask(
            CharacterPoseNodePayload payload);
        IReadOnlyList<CharacterPoseParameterPolicy>
            ParameterPolicies(CharacterPoseNodePayload payload);
        void RequirePayload(CharacterPoseNodePayload payload);
        void ValidatePayload(
            CharacterPoseNodePayload payload,
            string sourcePath);
        void ValidateRig(
            CharacterPoseNodePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath);
        CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input);
        object ReadField(
            CharacterPoseNodePayload payload,
            string field);
        CharacterPoseIrNode Lower(CharacterTypedPoseNode node, IReadOnlyList<CharacterPoseIrInput> inputs, string sourcePath);
    }

    internal abstract class CharacterPoseCompilerHandler<TPayload> :
        ICharacterPoseCompilerHandler
        where TPayload : CharacterPoseNodePayload, new()
    {
        public abstract CharacterPoseNodeKind Kind { get; }
        public string BindingId =>
            CharacterPoseGraphAuthoringCapabilities
                .Require(Kind).CompilerBindingId;
        public string CapabilityIdentity =>
            CharacterPoseGraphAuthoringCapabilities.Get(Kind).Value;
        public Type PayloadType => typeof(TPayload);
        public virtual CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.Operation;
        public CharacterPoseExecutionDomain ExecutionDomain
        {
            get
            {
                string value = CharacterPoseGraphAuthoringCapabilities
                    .Require(Kind).ExecutionDomainId;
                if (!Enum.TryParse(value, false, out CharacterPoseExecutionDomain result) ||
                    !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), result))
                {
                    throw new InvalidOperationException(
                        $"Pose capability '{CapabilityIdentity}' has invalid execution domain '{value}'.");
                }
                return result;
            }
        }
        public virtual CharacterPoseOperationCode Code => default;
        public virtual bool Player => false;
        public virtual bool ActionPlaybackControl => false;
        public virtual bool BlendPolicy => false;
        public virtual bool StateMachine => false;
        public virtual bool AnimationSlot => false;
        public virtual bool Inertialization => false;
        public virtual bool Additive => false;
        public virtual bool ModifyBone => false;
        public virtual bool RootOrientationWarp => false;
        public virtual bool ClipPlayer => false;

        public CharacterPoseIrNode Lower(CharacterTypedPoseNode node, IReadOnlyList<CharacterPoseIrInput> inputs, string sourcePath)
        {
            if (!(node.Payload is TPayload payload) || node.Kind != Kind)
                throw new InvalidOperationException($"{sourcePath}: payload does not match compiler handler '{Kind}'.");
            Validate(payload, sourcePath);
            return new CharacterPoseIrNode(
                new CharacterPoseIrNodeId(node.NodeId.Value),
                CharacterPoseGraphAuthoringCapabilities.Get(Kind).Value,
                payload,
                inputs,
                sourcePath);
        }

        public CharacterPresentationPoseSourceSlot Source(
            CharacterPoseNodePayload payload) =>
            GetSource(Require(payload));

        public AnimationChannelId Channel(
            CharacterPoseNodePayload payload) =>
            GetChannel(Require(payload));

        public PoseParameterId Parameter(
            CharacterPoseNodePayload payload) =>
            GetParameter(Require(payload));

        public AnimationSelectionAvailabilityPolicy Availability(
            CharacterPoseNodePayload payload,
            bool stateLocal) =>
            GetAvailability(Require(payload), stateLocal);

        public CharacterAnimationBlendSpaceInputRangePolicy InputRange(
            CharacterPoseNodePayload payload) =>
            GetInputRange(Require(payload));

        public float Weight(CharacterPoseNodePayload payload) =>
            GetWeight(Require(payload));

        public CharacterAnimationBoneMaskAsset BoneMask(
            CharacterPoseNodePayload payload) =>
            GetBoneMask(Require(payload));

        public IReadOnlyList<CharacterPoseParameterPolicy>
            ParameterPolicies(CharacterPoseNodePayload payload) =>
            GetParameterPolicies(Require(payload)) ??
            Array.Empty<CharacterPoseParameterPolicy>();

        public void RequirePayload(
            CharacterPoseNodePayload payload)
        {
            Require(payload);
        }

        public void ValidatePayload(
            CharacterPoseNodePayload payload,
            string sourcePath) =>
            Validate(Require(payload), sourcePath);

        public void ValidateRig(
            CharacterPoseNodePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath) =>
            ValidateRig(
                Require(payload),
                rig ??
                throw new ArgumentNullException(nameof(rig)),
                sourcePath);

        public virtual CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new TPayload();

        public object ReadField(
            CharacterPoseNodePayload payload,
            string field) =>
            ReadField(Require(payload), field);

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
                    $"Pose compiler handler '{CapabilityIdentity}' received payload '{payload?.GetType().Name ?? "<null>"}'.");
    }

    internal static class CharacterPoseCompilerHandlerValidation
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
        CharacterProgramParameterInputPoseCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterProgramParameterInputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ProgramParameterInput;
        public override CharacterPoseOperationCode Code =>
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
            CharacterPoseCompilerHandlerValidation.Require(
                payload.ParameterId.IsValid,
                sourcePath,
                "Parameter identity is missing.");
    }

    internal sealed class
        CharacterActionPlaybackInputPoseCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterActionPlaybackInputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ActionPlaybackInput;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.ActionPlaybackInput;
        public override bool ActionPlaybackControl => true;

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
            CharacterPoseCompilerHandlerValidation.Require(
                payload.AnimationChannelId.IsValid,
                sourcePath,
                "Animation Channel identity is missing.");
    }

    internal sealed class
        CharacterSelectedPosePlayerCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterSelectedPosePlayerPayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.SelectedPosePlayer;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.SelectedPosePlayer;
        public override bool Player => true;

        protected override CharacterPresentationPoseSourceSlot GetSource(
            CharacterSelectedPosePlayerPayload payload) =>
            payload.SourceSlot;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterSelectedPosePlayerPayload(
                input.Require<CharacterMotionMatchingPoseSourceSlot>(
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
            CharacterPoseCompilerHandlerValidation.Require(
                payload.SourceSlot,
                sourcePath,
                "Selected Pose source binding is incomplete.");
    }

    internal sealed class
        CharacterBlendSpacePlayerPoseCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterBlendSpacePlayerPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.BlendSpacePlayer;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.BlendSpacePlayer;
        public override bool Player => true;
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
                input.Require<CharacterBlendSpacePoseSourceSlot>(
                    "pose-source-slot"),
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
            string sourcePath) =>
            CharacterPoseCompilerHandlerValidation.Require(
                payload.SourceSlot,
                sourcePath,
                "Blend Space source identity is missing.");
    }

    internal sealed class
        CharacterClipPlayerPoseCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterClipPlayerPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ClipPlayer;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.ClipPlayer;
        public override bool Player => true;
        public override bool ClipPlayer => true;
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
                input.Require<CharacterClipPoseSourceSlot>(
                    "pose-source-slot"),
                input.Require<float>("play-rate"),
                input.Require<float>("initial-time"),
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
                "clock-source" => payload.ClockSource.ToString(),
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterClipPlayerPosePayload payload,
            string sourcePath)
        {
            CharacterPoseCompilerHandlerValidation.Require(
                payload.SourceSlot,
                sourcePath,
                "Clip source identity is missing.");
            CharacterPoseCompilerHandlerValidation.Require(
                float.IsFinite(payload.PlayRate) &&
                payload.PlayRate > 0f,
                sourcePath,
                "Clip play rate must be finite and positive.");
            CharacterPoseCompilerHandlerValidation.Require(
                float.IsFinite(payload.InitialTime) &&
                payload.InitialTime >= 0f,
                sourcePath,
                "Clip initial time must be finite and non-negative.");
            CharacterPoseCompilerHandlerValidation.Require(
                Enum.IsDefined(typeof(CharacterClipPlayerClockSource), payload.ClockSource),
                sourcePath,
                "Clip clock source is invalid.");
        }
    }

    internal sealed class
        CharacterPoseStateMachineNodeCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterPoseStateMachineNodePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.PoseStateMachine;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.PoseStateMachine;
        public override bool StateMachine => true;

        protected override void Validate(
            CharacterPoseStateMachineNodePayload payload,
            string sourcePath) =>
            CharacterPoseCompilerHandlerValidation.Require(
                payload.StateMachine != null,
                sourcePath,
                "Pose StateMachine is missing.");

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPoseStateMachineNodePayload(
                input.RequireStateMachine());
    }

    internal sealed class
        CharacterAnimationSlotPoseCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterAnimationSlotPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.AnimationSlot;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.AnimationSlot;
        public override bool Player => true;
        public override bool ActionPlaybackControl => true;
        public override bool BlendPolicy => true;
        public override bool AnimationSlot => true;

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
                input.Require<CharacterAnimationBlendPolicy>(
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
                "blend-policy" => payload.BlendPolicy,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterAnimationSlotPosePayload payload,
            string sourcePath) =>
            CharacterPoseCompilerHandlerValidation.Require(
                payload.SlotId.IsValid &&
                payload.AnimationChannelId.IsValid &&
                payload.SelectionAvailability ==
                AnimationSelectionAvailabilityPolicy.AllowEmpty &&
                payload.BlendPolicy,
                sourcePath,
                "Animation Slot requires identity, channel, AllowEmpty and one Blend Policy.");

        protected override void ValidateRig(
            CharacterAnimationSlotPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                payload.BlendPolicy.RequireValid(rig);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{sourcePath}: {exception.Message}",
                    exception);
            }
        }
    }

    internal sealed class CharacterBlendStackPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterBlendStackPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.BlendStack;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.BlendStack;
        public override bool Player => true;
        public override bool BlendPolicy => true;

        protected override CharacterPresentationPoseSourceSlot GetSource(
            CharacterBlendStackPosePayload payload) =>
            payload.SourceSlot;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterBlendStackPosePayload(
                input.Require<CharacterMotionMatchingPoseSourceSlot>(
                    "pose-source-slot"),
                input.Require<CharacterAnimationBlendPolicy>(
                    "blend-policy"));

        protected override object ReadField(
            CharacterBlendStackPosePayload payload,
            string field) =>
            field switch
            {
                "pose-source-slot" => payload.SourceSlot,
                "blend-policy" => payload.BlendPolicy,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterBlendStackPosePayload payload,
            string sourcePath) =>
            CharacterPoseCompilerHandlerValidation.Require(
                payload.SourceSlot &&
                payload.BlendPolicy,
                sourcePath,
                "Blend Stack source binding or Blend Policy is incomplete.");

        protected override void ValidateRig(
            CharacterBlendStackPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                payload.BlendPolicy.RequireValid(rig);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{sourcePath}: {exception.Message}",
                    exception);
            }
        }
    }

    internal sealed class
        CharacterInertializationPoseCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterInertializationPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.Inertialization;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.Inertialization;
        public override bool Inertialization => true;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterInertializationPosePayload(
                input.Require<CharacterPoseInertializationPolicy>(
                    "inertialization-policy"));

        protected override object ReadField(
            CharacterInertializationPosePayload payload,
            string field) =>
            field == "inertialization-policy"
                ? payload.Policy
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterInertializationPosePayload payload,
            string sourcePath) =>
            CharacterPoseCompilerHandlerValidation.Require(
                payload.Policy,
                sourcePath,
                "Inertialization Policy is missing.");

        protected override void ValidateRig(
            CharacterInertializationPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                payload.Policy.RequireValid(rig);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{sourcePath}: {exception.Message}",
                    exception);
            }
        }
    }

    internal sealed class CharacterBlendPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterBlendPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.BlendPose;
        public override CharacterPoseOperationCode Code =>
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
            CharacterPoseCompilerHandlerValidation.RequireWeight(
                payload.Weight,
                sourcePath);
    }

    internal sealed class
        CharacterLayeredBoneBlendPoseCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterLayeredBoneBlendPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.LayeredBoneBlend;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.LayeredBoneBlend;

        protected override float GetWeight(
            CharacterLayeredBoneBlendPosePayload payload) =>
            payload.Weight;

        protected override CharacterAnimationBoneMaskAsset
            GetBoneMask(
                CharacterLayeredBoneBlendPosePayload payload) =>
            payload.BoneMask;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterLayeredBoneBlendPosePayload(
                input.Require<CharacterAnimationBoneMaskAsset>(
                    "bone-mask"),
                input.Require<float>("weight"));

        protected override object ReadField(
            CharacterLayeredBoneBlendPosePayload payload,
            string field) =>
            field switch
            {
                "bone-mask" => payload.BoneMask,
                "weight" => payload.Weight,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterLayeredBoneBlendPosePayload payload,
            string sourcePath)
        {
            CharacterPoseCompilerHandlerValidation.RequireWeight(
                payload.Weight,
                sourcePath);
            CharacterPoseCompilerHandlerValidation.Require(
                payload.BoneMask,
                sourcePath,
                "Layered Bone Blend mask is missing.");
        }

        protected override void ValidateRig(
            CharacterLayeredBoneBlendPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                payload.BoneMask.BuildDense(rig);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{sourcePath}: {exception.Message}",
                    exception);
            }
        }
    }

    internal sealed class CharacterAdditivePoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterAdditivePosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.AdditivePose;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.AdditivePose;
        public override bool Additive => true;

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
            CharacterPoseCompilerHandlerValidation.RequireWeight(
                payload.Weight,
                sourcePath);
            CharacterPoseCompilerHandlerValidation.Require(
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
        CharacterPoseParameterResolveCompilerHandler :
            CharacterPoseCompilerHandler<
                CharacterPoseParameterResolvePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.PoseParameterResolve;
        public override CharacterPoseOperationCode Code =>
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

    internal sealed class CharacterModifyBonePoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterModifyBonePosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ModifyBone;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.ModifyBone;
        public override bool ModifyBone => true;

        protected override void Validate(
            CharacterModifyBonePosePayload payload,
            string sourcePath)
        {
            CharacterPoseCompilerHandlerValidation.Require(
                payload.BoneId.IsValid &&
                Enum.IsDefined(
                    typeof(ModifyBoneReferenceSpace),
                    payload.ReferenceSpace) &&
                payload.Operations !=
                ModifyBoneOperationMask.None &&
                CharacterPoseCompilerHandlerValidation.Finite(
                    payload.Position) &&
                CharacterPoseCompilerHandlerValidation.Finite(
                    payload.Rotation) &&
                CharacterPoseCompilerHandlerValidation.Finite(
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

    internal sealed class CharacterRootOrientationWarpPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterRootOrientationWarpPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.RootOrientationWarp;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.RootOrientationWarp;
        public override bool RootOrientationWarp => true;

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterRootOrientationWarpPosePayload(
                input.Require<RootMotionCurveAsset>("yaw-curve"));

        protected override object ReadField(
            CharacterRootOrientationWarpPosePayload payload,
            string field) =>
            field == "yaw-curve"
                ? payload.YawCurve
                : base.ReadField(payload, field);

        protected override void Validate(
            CharacterRootOrientationWarpPosePayload payload,
            string sourcePath)
        {
            CharacterPoseCompilerHandlerValidation.Require(
                payload.YawCurve &&
                payload.YawCurve.TryValidate(out _) &&
                payload.YawCurve.Duration > 0f &&
                payload.YawCurve.LocalYaw != null &&
                payload.YawCurve.LocalYaw.length >= 2 &&
                float.IsFinite(payload.YawCurve.TotalYaw) &&
                Math.Abs(payload.YawCurve.TotalYaw) > 0.001f,
                sourcePath,
                "Root Orientation Warp Yaw profile is invalid.");
        }
    }

    internal sealed class CharacterPoseSubgraphCompilerHandler :
        CharacterPoseCompilerHandler<CharacterPoseSubgraphPayload>
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
            CharacterPoseCompilerHandlerValidation.Require(
                payload.Subgraph?.PoseGraphId.IsValid == true,
                sourcePath,
                "Pose Subgraph target is missing.");
    }

    internal sealed class CharacterLocalToComponentPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterLocalToComponentPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.LocalToComponentPose;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.LocalToComponentPose;
    }

    internal sealed class CharacterComponentToLocalPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterComponentToLocalPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.ComponentToLocalPose;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.ComponentToLocalPose;
    }

    internal sealed class CharacterGraphInputPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterGraphInputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.GraphInput;
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.GraphInput;
    }

    internal sealed class CharacterGraphOutputPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterGraphOutputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.GraphOutput;
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.GraphOutput;
    }

    internal sealed class CharacterOutputPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterOutputPosePayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.OutputPose;
        public override CharacterPoseNativeNodeRole NativeRole =>
            CharacterPoseNativeNodeRole.PoseOutput;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.OutputPose;
    }

    internal sealed class CharacterPoseCompilerHandlerRegistry
    {
        readonly Dictionary<string, ICharacterPoseCompilerHandler>
            m_Handlers =
                new Dictionary<string, ICharacterPoseCompilerHandler>(
                    StringComparer.Ordinal);

        public static CharacterPoseCompilerHandlerRegistry Shared
        {
            get;
        } = new CharacterPoseCompilerHandlerRegistry();

        public IReadOnlyCollection<ICharacterPoseCompilerHandler>
            All => m_Handlers.Values;

        public CharacterPoseCompilerHandlerRegistry()
        {
            IEnumerable<Type> handlerTypes =
                typeof(ICharacterPoseCompilerHandler).Assembly
                    .GetTypes()
                    .Where(type =>
                        !type.IsAbstract &&
                        !type.IsGenericTypeDefinition &&
                        typeof(ICharacterPoseCompilerHandler)
                            .IsAssignableFrom(type))
                    .OrderBy(
                        type => type.FullName,
                        StringComparer.Ordinal);
            foreach (Type type in handlerTypes)
            {
                Register(
                    (ICharacterPoseCompilerHandler)
                    Activator.CreateInstance(type, true));
            }

            IReadOnlyList<GraphAuthoringCapabilityDescriptor>
                capabilities =
                    CharacterPoseGraphAuthoringCapabilities.Catalog
                        .GetDomain(
                            CharacterPoseGraphAuthoringCapabilities
                                .Domain)
                        .Where(value =>
                            value.AuthoringType != null &&
                            typeof(CharacterPoseNodePayload)
                                .IsAssignableFrom(
                                    value.AuthoringType))
                        .ToArray();
            foreach (GraphAuthoringCapabilityDescriptor capability in
                     capabilities)
            {
                if (!m_Handlers.ContainsKey(
                        capability.CompilerBindingId))
                {
                    throw new InvalidOperationException(
                        $"Pose capability '{capability.CapabilityId}' has no compiler handler '{capability.CompilerBindingId}'.");
                }
            }
            if (m_Handlers.Count != capabilities.Count)
            {
                throw new InvalidOperationException(
                    "Pose compiler handlers do not match the formal capability catalog.");
            }
        }

        public ICharacterPoseCompilerHandler Require(
            CharacterPoseNodeKind kind)
        {
            GraphAuthoringCapabilityDescriptor capability =
                CharacterPoseGraphAuthoringCapabilities.Require(kind);
            return m_Handlers.TryGetValue(
                capability.CompilerBindingId,
                out ICharacterPoseCompilerHandler handler)
                ? handler
                : throw new InvalidOperationException(
                    $"Pose capability '{capability.CapabilityId}' has no compiler handler '{capability.CompilerBindingId}'.");
        }

        public ICharacterPoseCompilerHandler RequireCapability(
            string capabilityIdentity)
        {
            if (!CharacterPoseGraphAuthoringCapabilities.Catalog
                    .TryGetByExternalKind(
                        CharacterPoseGraphAuthoringCapabilities
                            .Domain,
                        capabilityIdentity,
                        out GraphAuthoringCapabilityDescriptor
                            capability) ||
                !m_Handlers.TryGetValue(
                    capability.CompilerBindingId,
                    out ICharacterPoseCompilerHandler handler))
            {
                throw new InvalidOperationException(
                    $"Pose IR capability '{capabilityIdentity ?? "<null>"}' has no compiler handler.");
            }
            return handler;
        }

        void Register(ICharacterPoseCompilerHandler handler)
        {
            if (handler == null)
                throw new InvalidOperationException(
                    "Pose compiler handler is missing.");
            GraphAuthoringCapabilityDescriptor capability =
                CharacterPoseGraphAuthoringCapabilities.Require(
                    handler.Kind);
            if (capability.AuthoringType != handler.PayloadType ||
                !string.Equals(
                    capability.CompilerBindingId,
                    handler.BindingId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Pose compiler handler '{handler.GetType().FullName}' does not match capability '{capability.CapabilityId}'.");
            }
            if (!m_Handlers.TryAdd(handler.BindingId, handler))
            {
                throw new InvalidOperationException(
                    $"Pose compiler handler binding '{handler.BindingId}' is registered more than once.");
            }
        }
    }

    internal sealed class CharacterPoseIrCompiler
    {
        readonly CharacterPoseCompilerHandlerRegistry m_Handlers =
            CharacterPoseCompilerHandlerRegistry.Shared;

        public CharacterPoseIrGraph Compile(CharacterTypedPoseGraph graph, CharacterPoseIrGraphRole role)
        {
            if (graph == null || !graph.GraphId.IsValid || string.IsNullOrWhiteSpace(graph.ContentRevision))
                throw new ArgumentException("Typed Pose Graph identity or revision is missing.", nameof(graph));
            Dictionary<PoseNodeId, CharacterTypedPoseNode> nodes = IndexNodes(graph);
            Dictionary<PoseNodeId, List<CharacterPoseEdge>> incoming = BuildIncoming(graph, nodes);
            List<CharacterTypedPoseNode> ordered = TopologicalOrder(nodes, incoming);
            ValidateBoundary(role, ordered);
            var lowered = new List<CharacterPoseIrNode>(ordered.Count);
            foreach (CharacterTypedPoseNode node in ordered)
            {
                string sourcePath = $"pose-graphs/{graph.GraphId.Value}/nodes/{node.NodeId.Value}";
                IReadOnlyList<CharacterPoseIrInput> inputs = BuildInputs(node, incoming[node.NodeId], nodes, sourcePath);
                lowered.Add(m_Handlers.Require(node.Kind).Lower(node, inputs, sourcePath));
            }
            CharacterTypedPoseNode output = role != CharacterPoseIrGraphRole.Subgraph && role != CharacterPoseIrGraphRole.LinkedPoseEntry
                ? ordered.Single(value => value.Kind == CharacterPoseNodeKind.OutputPose)
                : ordered.Single(value => value.Kind == CharacterPoseNodeKind.GraphOutput);
            return new CharacterPoseIrGraph(graph.GraphId, graph.ContentRevision, lowered, new CharacterPoseIrNodeId(output.NodeId.Value));
        }

        static Dictionary<PoseNodeId, CharacterTypedPoseNode> IndexNodes(CharacterTypedPoseGraph graph)
        {
            var result = new Dictionary<PoseNodeId, CharacterTypedPoseNode>();
            foreach (CharacterTypedPoseNode node in graph.Nodes)
            {
                if (node == null || !node.NodeId.IsValid || !result.TryAdd(node.NodeId, node))
                    throw new InvalidOperationException($"pose-graphs/{graph.GraphId.Value}: Pose node identity is missing or duplicated.");
                CharacterPoseGraphAuthoringCapabilities
                    .RequireKind(node.Payload);
                ValidatePorts(node);
            }
            return result;
        }

        static void ValidatePorts(CharacterTypedPoseNode node)
        {
            var ids = new HashSet<string>(
                CharacterPoseAuthoringPortProjection
                    .GetFixed(node.Kind)
                    .Select(value => value.PortId.Value),
                StringComparer.Ordinal);
            foreach (CharacterPoseDynamicPort port in node.DynamicPorts)
            {
                if (port == null || !port.PortId.IsValid || !ids.Add(port.PortId.Value))
                    throw new InvalidOperationException($"Pose node '{node.NodeId}' contains an invalid or duplicate dynamic port.");
            }
        }

        static Dictionary<PoseNodeId, List<CharacterPoseEdge>> BuildIncoming(CharacterTypedPoseGraph graph, IReadOnlyDictionary<PoseNodeId, CharacterTypedPoseNode> nodes)
        {
            var result = nodes.Keys.ToDictionary(value => value, _ => new List<CharacterPoseEdge>());
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseEdge edge in graph.Edges)
            {
                if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeId) || !edgeIds.Add(edge.EdgeId) || !nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId))
                    throw new InvalidOperationException($"pose-graphs/{graph.GraphId.Value}: Pose edge identity or endpoint is invalid.");
                ResolvePort(nodes[edge.SourceNodeId], edge.SourcePortId.Value, CharacterPosePortDirection.Output);
                ResolvePort(nodes[edge.TargetNodeId], edge.TargetPortId.Value, CharacterPosePortDirection.Input);
                result[edge.TargetNodeId].Add(edge);
            }
            return result;
        }

        static List<CharacterTypedPoseNode> TopologicalOrder(
            IReadOnlyDictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            IReadOnlyDictionary<PoseNodeId, List<CharacterPoseEdge>> incoming)
        {
            var indegree = incoming.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
                    .Where(edge => !IsTemporalHistoryEdge(nodes, edge))
                    .Select(value => value.SourceNodeId)
                    .Distinct()
                    .Count());
            var outgoing = nodes.Keys.ToDictionary(value => value, _ => new HashSet<PoseNodeId>());
            foreach (KeyValuePair<PoseNodeId, List<CharacterPoseEdge>> pair in incoming)
                foreach (CharacterPoseEdge edge in pair.Value)
                    if (!IsTemporalHistoryEdge(nodes, edge))
                        outgoing[edge.SourceNodeId].Add(pair.Key);
            var ready = new SortedSet<PoseNodeId>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
            var result = new List<CharacterTypedPoseNode>(nodes.Count);
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
            IReadOnlyDictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            CharacterPoseEdge edge) =>
            ResolvePort(
                nodes[edge.SourceNodeId],
                edge.SourcePortId.Value,
                CharacterPosePortDirection.Output).Kind == CharacterPosePortKind.PoseHistory;

        static IReadOnlyList<CharacterPoseIrInput> BuildInputs(
            CharacterTypedPoseNode target,
            IReadOnlyList<CharacterPoseEdge> incoming,
            IReadOnlyDictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            string sourcePath)
        {
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<CharacterPoseIrInput>();
            foreach (CharacterPoseEdge edge in incoming.OrderBy(value => value.TargetPortId.Value, StringComparer.Ordinal))
            {
                if (!occupied.Add(edge.TargetPortId.Value))
                    throw new InvalidOperationException($"{sourcePath}: input port '{edge.TargetPortId}' has more than one source.");
                CharacterPosePortDefinition targetPort =
                    ResolvePort(
                        target,
                        edge.TargetPortId.Value,
                        CharacterPosePortDirection.Input);
                CharacterPosePortDefinition sourcePort =
                    ResolvePort(
                        nodes[edge.SourceNodeId],
                        edge.SourcePortId.Value,
                        CharacterPosePortDirection.Output);
                if (targetPort.Kind != sourcePort.Kind)
                    throw new InvalidOperationException($"{sourcePath}: edge '{edge.EdgeId}' connects different value kinds.");
                result.Add(new CharacterPoseIrInput(new CharacterPoseIrLinkId(edge.EdgeId), edge.TargetPortId.Value, new CharacterPoseIrNodeId(edge.SourceNodeId.Value), edge.SourcePortId.Value, targetPort.Kind));
            }
            foreach (CharacterPosePortDefinition port in
                     CharacterPoseAuthoringPortProjection.Get(target))
            {
                if (port.Direction ==
                    CharacterPosePortDirection.Input &&
                    port.Required &&
                    !occupied.Contains(port.PortId.Value))
                    throw new InvalidOperationException($"{sourcePath}: required input '{port.PortId}' is not connected.");
            }
            return result;
        }

        static CharacterPosePortDefinition ResolvePort(
            CharacterTypedPoseNode node,
            string portId,
            CharacterPosePortDirection direction) =>
            CharacterPoseAuthoringPortProjection.Require(
                node,
                portId,
                direction);

        static void ValidateBoundary(CharacterPoseIrGraphRole role, IReadOnlyList<CharacterTypedPoseNode> nodes)
        {
            int rootOutputs = nodes.Count(value => value.Kind == CharacterPoseNodeKind.OutputPose);
            int graphOutputs = nodes.Count(value => value.Kind == CharacterPoseNodeKind.GraphOutput);
            bool graphBoundary = role == CharacterPoseIrGraphRole.Subgraph || role == CharacterPoseIrGraphRole.LinkedPoseEntry;
            if (!graphBoundary && (rootOutputs != 1 || graphOutputs != 0))
                throw new InvalidOperationException("Root and state-local Pose Graphs must contain exactly one Output Pose and no Graph Output.");
            if (graphBoundary && (graphOutputs != 1 || rootOutputs != 0))
                throw new InvalidOperationException("Pose Subgraphs and Linked Pose Entries must contain exactly one Graph Output and no Output Pose.");
        }
    }
}
