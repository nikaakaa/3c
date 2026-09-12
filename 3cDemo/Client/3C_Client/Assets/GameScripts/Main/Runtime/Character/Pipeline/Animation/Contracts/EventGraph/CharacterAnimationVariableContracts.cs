using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterAnimationVariableContract
    {
        public const string Version = "character-animation-variable/v1";

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
        public IReadOnlyList<EventGraphVariableDescriptor> Variables => Source.Descriptors;

        public EventGraphVariableDescriptor Require(string variableId) =>
            Source.Require(variableId);

        public bool TryGet(string variableId, out EventGraphVariableDescriptor descriptor)
        {
            try
            {
                descriptor = Source.Require(variableId);
                return true;
            }
            catch (InvalidOperationException)
            {
                descriptor = null;
                return false;
            }
        }
    }

    public sealed class CharacterAnimationVariableFrame
    {
        readonly CharacterAnimationVariableContract m_Contract;

        internal CharacterAnimationVariableFrame(
            ActorId actorId,
            ulong renderFrame,
            SimulationTick simulationTick,
            ulong bodyDiscontinuityGeneration,
            EventGraphVariableFrame values)
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
            Values = values ?? throw new ArgumentNullException(nameof(values));
            m_Contract = new CharacterAnimationVariableContract(values.Contract);
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public SimulationTick SimulationTick { get; }
        public ulong BodyDiscontinuityGeneration { get; }
        public EventGraphVariableFrame Values { get; }
        public CharacterAnimationVariableContract Contract => m_Contract;

        public bool TryRead(string variableId, out EventGraphValue value) =>
            Values.TryRead(variableId, out value);

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

    public sealed class CharacterAnimationVariableUpdateResult
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
                frame ?? throw new ArgumentNullException(nameof(frame)),
                null);

        internal static CharacterAnimationVariableUpdateResult Failed(
            EventGraphExecutionFailure failure) =>
            new CharacterAnimationVariableUpdateResult(
                false,
                null,
                failure ?? throw new ArgumentNullException(nameof(failure)));
    }
}
