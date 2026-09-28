using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterAnimationVariableIds
    {
        public const string HorizontalSpeed = "animation.horizontal-speed";
        public const string VerticalSpeed = "animation.vertical-speed";
        public const string MovementDirection = "animation.movement-direction";
        public const string DesiredDirection = "animation.desired-direction";
        public const string HorizontalAcceleration = "animation.horizontal-acceleration";
        public const string FacingError = "animation.facing-error";
        public const string MotionPhase = "animation.motion-phase";
        public const string PreviousPlanarVelocity = "animation.history.previous-planar-velocity";
        public const string HasPreviousSample = "animation.history.has-previous-sample";
    }

    public sealed class CharacterAnimationVariableContract
    {
        public const string Version = "character-animation-variable/v2";

        public CharacterAnimationVariableContract(
            EventGraphVariableContract source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        internal EventGraphVariableContract Source { get; }
        public string GraphId => Source.GraphId;
        public string Revision => Source.Revision;
        public string ContractRevision => Version;
        public string LayoutId => Source.Layout.LayoutId;
        public IReadOnlyList<EventGraphVariableDescriptor> Variables => Source.PublishedDescriptors;

        public EventGraphVariableBinding Bind(string variableId) => Source.Layout.Bind(variableId);

        public EventGraphVariableDescriptor Require(string variableId) =>
            TryGet(variableId, out var descriptor) ? descriptor :
                throw new InvalidOperationException($"Animation variable '{variableId}' is not published by the Character Animation Event Graph.");

        public bool TryGet(string variableId, out EventGraphVariableDescriptor descriptor)
        {
            if (Source.Layout.TryGet(variableId, out var entry))
            {
                descriptor = entry.Descriptor;
                return true;
            }
            descriptor = null;
            return false;
        }
    }

    public readonly struct CharacterAnimationVariableFrame
    {
        readonly CharacterAnimationVariableContract m_Contract;

        internal CharacterAnimationVariableFrame(
            ActorId actorId,
            ulong renderFrame,
            SimulationTick simulationTick,
            ulong bodyDiscontinuityGeneration,
            EventGraphVariableFrame values,
            CharacterAnimationVariableContract contract)
        {
            if (!actorId.IsValid || renderFrame == 0 || !simulationTick.IsValid ||
                bodyDiscontinuityGeneration == 0)
            {
                throw new ArgumentException("Character animation variable frame identity is incomplete.");
            }
            ActorId = actorId;
            RenderFrame = renderFrame;
            SimulationTick = simulationTick;
            BodyDiscontinuityGeneration = bodyDiscontinuityGeneration;
            Values = values.IsValid ? values : throw new ArgumentException("Animation variable values are invalid.", nameof(values));
            m_Contract = contract;
        }

        public bool IsValid => Values.IsValid;
        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public SimulationTick SimulationTick { get; }
        public ulong BodyDiscontinuityGeneration { get; }
        public EventGraphVariableFrame Values { get; }
        public CharacterAnimationVariableContract Contract => m_Contract;

        public bool TryRead(string variableId, out EventGraphValue value) =>
            Values.TryRead(variableId, out value);

        public bool TryRead(EventGraphVariableBinding binding, out EventGraphValue value) =>
            Values.TryRead(binding, out value);

        public EventGraphValue Require(EventGraphVariableBinding binding) => Values.Require(binding);

        public bool TryRead(PoseParameterId parameterId, out EventGraphValue value)
        {
            if (!parameterId.IsValid)
            {
                value = default;
                return false;
            }
            return Values.TryRead(parameterId.Value, out value);
        }

        public EventGraphValue Require(string variableId) =>
            Values.Require(variableId);

        public float RequireFloat(string variableId)
        {
            EventGraphValue value = Require(variableId);
            return value.As<float>();
        }

        public int RequireInt32(string variableId)
        {
            EventGraphValue value = Require(variableId);
            return value.As<int>();
        }

        public bool RequireBool(string variableId)
        {
            EventGraphValue value = Require(variableId);
            return value.As<bool>();
        }
    }

    public readonly struct CharacterAnimationVariableUpdateResult
    {
        internal CharacterAnimationVariableUpdateResult(
            bool succeeded,
            CharacterAnimationVariableFrame frame,
            EventGraphExecutionFailure failure)
        {
            Succeeded = succeeded;
            Frame = frame;
            Failure = failure;
        }

        public bool Succeeded { get; }
        public CharacterAnimationVariableFrame Frame { get; }
        public EventGraphExecutionFailure Failure { get; }

        internal static CharacterAnimationVariableUpdateResult Success(
            CharacterAnimationVariableFrame frame) =>
            new CharacterAnimationVariableUpdateResult(
                true,
                frame.IsValid ? frame : throw new ArgumentException("Animation variable frame is invalid.", nameof(frame)),
                null);

        internal static CharacterAnimationVariableUpdateResult Failed(
            EventGraphExecutionFailure failure) =>
            new CharacterAnimationVariableUpdateResult(
                false,
                default,
                failure ?? throw new ArgumentNullException(nameof(failure)));
    }
}
