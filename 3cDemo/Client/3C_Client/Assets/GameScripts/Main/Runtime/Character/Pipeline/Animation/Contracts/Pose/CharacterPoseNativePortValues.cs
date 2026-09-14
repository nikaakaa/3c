using System;
using BTSMTL.EventGraphs;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal abstract class CharacterPoseNativePortValue
    {
        protected CharacterPoseNativePortValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity)
        {
            if (!producerNodeId.IsValid || completionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            ProducerNodeId = producerNodeId;
            CompletionIdentity = completionIdentity;
        }

        internal PoseNodeId ProducerNodeId { get; }
        internal ulong CompletionIdentity { get; }
        internal bool IsValid => ProducerNodeId.IsValid && CompletionIdentity != 0;
    }

    internal sealed class CharacterPoseNativeLocalPoseValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeLocalPoseValue(
            PoseNodeId producerNodeId,
            AnimationPoseValue pose)
            : base(producerNodeId, pose.CompletionIdentity)
        {
            if (!pose.NodeId.IsValid || pose.Availability == AnimationPoseAvailability.Invalid)
                throw new ArgumentException("Pose native local value is invalid.");
            Pose = pose;
        }

        internal AnimationPoseValue Pose { get; }
    }

    internal sealed class CharacterPoseNativeComponentPoseValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeComponentPoseValue(
            PoseNodeId producerNodeId,
            AnimationPoseValue pose)
            : base(producerNodeId, pose.CompletionIdentity)
        {
            if (!pose.NodeId.IsValid || pose.Availability == AnimationPoseAvailability.Invalid)
                throw new ArgumentException("Pose native component value is invalid.");
            Pose = pose;
        }

        internal AnimationPoseValue Pose { get; }
    }

    internal sealed class CharacterPoseNativeParameterValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeParameterValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            PoseParameterId parameterId,
            EventGraphValue value)
            : base(producerNodeId, completionIdentity)
        {
            if (!parameterId.IsValid ||
                (value.Kind != EventGraphValueKind.Bool &&
                 value.Kind != EventGraphValueKind.Int32 &&
                 value.Kind != EventGraphValueKind.Float32))
            {
                throw new ArgumentException("Pose native parameter value is invalid.");
            }
            ParameterId = parameterId;
            Value = value;
        }

        internal PoseParameterId ParameterId { get; }
        internal EventGraphValue Value { get; }
    }

    internal sealed class CharacterPoseNativeDiscontinuityValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeDiscontinuityValue(
            PoseNodeId producerNodeId,
            PoseDiscontinuity value)
            : base(producerNodeId, value.CompletionIdentity)
        {
            if (!value.IsValid)
                throw new ArgumentException("Pose native discontinuity value is invalid.");
            Value = value;
        }

        internal PoseDiscontinuity Value { get; }
    }

    internal sealed class CharacterPoseNativeActionPlaybackValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeActionPlaybackValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            ActionAnimationPlaybackCommand command)
            : base(producerNodeId, completionIdentity)
        {
            if (!command.IsValid)
                throw new ArgumentException("Pose native action playback value is invalid.");
            Command = command;
        }

        internal ActionAnimationPlaybackCommand Command { get; }
    }

    internal sealed class CharacterPoseNativeFullBodyIkGoalsValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeFullBodyIkGoalsValue(
            PoseNodeId producerNodeId,
            CharacterFullBodyIkGoalSet value)
            : base(producerNodeId, value.Header.CompletionIdentity)
        {
            if (!value.IsValid)
                throw new ArgumentException("Pose native Full Body IK goals value is invalid.");
            Value = value;
        }

        internal CharacterFullBodyIkGoalSet Value { get; }
    }

    internal sealed class CharacterPoseNativeGoalContributionValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeGoalContributionValue(
            PoseNodeId producerNodeId,
            CharacterFullBodyIkGoalContributionHeader header,
            NativeSlice<CharacterFullBodyIkGoal> goals)
            : base(producerNodeId, header.CompletionIdentity)
        {
            if (!header.IsValid || goals.Length != header.GoalCount)
                throw new ArgumentException("Pose native goal contribution value is invalid.");
            Header = header;
            Goals = goals;
        }

        internal CharacterFullBodyIkGoalContributionHeader Header { get; }
        internal NativeSlice<CharacterFullBodyIkGoal> Goals { get; }
    }

    internal sealed class CharacterPoseNativeHistoryValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeHistoryValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            CharacterPoseHistoryReadView value)
            : base(producerNodeId, completionIdentity)
        {
            if (!value.IsValid)
                throw new ArgumentException("Pose native history value is invalid.");
            Value = value;
        }

        internal CharacterPoseHistoryReadView Value { get; }
    }

    internal sealed class CharacterPoseNativeTrajectoryValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeTrajectoryValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            CharacterMotionMatchingTrajectoryReadView value)
            : base(producerNodeId, completionIdentity)
        {
            if (!value.IsValid)
                throw new ArgumentException("Pose native trajectory value is invalid.");
            Value = value;
        }

        internal CharacterMotionMatchingTrajectoryReadView Value { get; }
    }

    internal sealed class CharacterPoseNativeFactsValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeFactsValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            in CharacterPresentationFactFrame value)
            : base(producerNodeId, completionIdentity)
        {
            if (!value.IsValid)
                throw new ArgumentException("Pose native facts value is invalid.");
            Value = value;
        }

        internal CharacterPresentationFactFrame Value { get; }
    }

    internal sealed class CharacterPoseNativeMotionMatchingBindingValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeMotionMatchingBindingValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            CharacterMotionMatchingBinding value)
            : base(producerNodeId, completionIdentity)
        {
            if (!value || !value.BindingId.IsValid)
                throw new ArgumentException("Pose native Motion Matching binding value is invalid.");
            Value = value;
        }

        internal CharacterMotionMatchingBinding Value { get; }
    }
}
