using System;
using BTSMTL.EventGraphs;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseNativePoseReadBinding
    {
        internal CharacterPoseNativePoseReadBinding(
            in AnimationPlayerPoseNativeWriteBinding output)
            : this(in output, CharacterPoseSpace.Local)
        {
        }

        internal CharacterPoseNativePoseReadBinding(
            in AnimationPlayerPoseNativeWriteBinding output,
            CharacterPoseSpace space)
        {
            if (output.CompletionIdentity == 0 ||
                space != CharacterPoseSpace.Local &&
                space != CharacterPoseSpace.Component ||
                output.DenseLocalPoses.Length == 0 ||
                output.DenseVelocities.Length != output.DenseLocalPoses.Length ||
                output.PoseParameters.Length == 0 ||
                output.PoseParameterAvailability.Length !=
                    output.PoseParameters.Length ||
                output.Contributions.Length == 0 ||
                output.DenseContributionWeights.Length !=
                    output.Contributions.Length * output.DenseLocalPoses.Length ||
                output.ContributionCount.Length != 1 ||
                output.OutputWeight.Length != 1 ||
                output.LeftFootFeatures.Length != 1 ||
                output.RightFootFeatures.Length != 1 ||
                output.HasFootFeatures.Length != 1 ||
                output.Availability.Length != 1 ||
                output.ContinuityIdentity.Length != 1 ||
                output.Discontinuity.Length != 1 ||
                output.InvalidReason.Length != 1 ||
                output.CompletedAt.Length != 1)
            {
                throw new ArgumentException(
                    "Pose native read binding is invalid.",
                    nameof(output));
            }
            CompletionIdentity = output.CompletionIdentity;
            Space = space;
            DenseLocalPoses = output.DenseLocalPoses;
            DenseVelocities = output.DenseVelocities;
            PoseParameters = output.PoseParameters;
            PoseParameterAvailability = output.PoseParameterAvailability;
            Contributions = output.Contributions;
            DenseContributionWeights = output.DenseContributionWeights;
            ContributionCount = output.ContributionCount;
            OutputWeight = output.OutputWeight;
            LeftFootFeatures = output.LeftFootFeatures;
            RightFootFeatures = output.RightFootFeatures;
            HasFootFeatures = output.HasFootFeatures;
            Availability = output.Availability;
            ContinuityIdentity = output.ContinuityIdentity;
            Discontinuity = output.Discontinuity;
            InvalidReason = output.InvalidReason;
            CompletedAt = output.CompletedAt;
        }

        internal ulong CompletionIdentity { get; }
        internal CharacterPoseSpace Space { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseLocalPoses { get; }
        internal NativeSlice<AnimationBlendBoneVelocity> DenseVelocities { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<byte> PoseParameterAvailability { get; }
        internal NativeSlice<AnimationPrimitivePoseContribution> Contributions { get; }
        internal NativeSlice<float> DenseContributionWeights { get; }
        internal NativeSlice<int> ContributionCount { get; }
        internal NativeSlice<float> OutputWeight { get; }
        internal NativeSlice<AnimationFootFeatureSample> LeftFootFeatures { get; }
        internal NativeSlice<AnimationFootFeatureSample> RightFootFeatures { get; }
        internal NativeSlice<byte> HasFootFeatures { get; }
        internal NativeSlice<AnimationPoseAvailability> Availability { get; }
        internal NativeSlice<ulong> ContinuityIdentity { get; }
        internal NativeSlice<PoseDiscontinuityNative> Discontinuity { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> InvalidReason { get; }
        internal NativeSlice<ulong> CompletedAt { get; }
        internal bool IsValid => CompletionIdentity != 0 &&
            (Space == CharacterPoseSpace.Local ||
             Space == CharacterPoseSpace.Component) &&
            DenseLocalPoses.Length > 0 &&
            DenseVelocities.Length == DenseLocalPoses.Length &&
            PoseParameters.Length > 0 &&
            PoseParameterAvailability.Length == PoseParameters.Length &&
            Contributions.Length > 0 &&
            DenseContributionWeights.Length ==
                Contributions.Length * DenseLocalPoses.Length &&
            ContributionCount.Length == 1 && OutputWeight.Length == 1 &&
            LeftFootFeatures.Length == 1 && RightFootFeatures.Length == 1 &&
            HasFootFeatures.Length == 1 && Availability.Length == 1 &&
            ContinuityIdentity.Length == 1 && Discontinuity.Length == 1 &&
            InvalidReason.Length == 1 && CompletedAt.Length == 1;
    }

    internal abstract class CharacterPoseNativePortValue
    {
        PoseNodeId m_ProducerNodeId;
        ulong m_CompletionIdentity;

        protected CharacterPoseNativePortValue(
            PoseNodeId producerNodeId,
            ulong completionIdentity)
        {
            SetIdentity(producerNodeId, completionIdentity);
        }

        internal PoseNodeId ProducerNodeId => m_ProducerNodeId;
        internal ulong CompletionIdentity => m_CompletionIdentity;
        internal bool IsValid => ProducerNodeId.IsValid && CompletionIdentity != 0;

        protected void SetIdentity(
            PoseNodeId producerNodeId,
            ulong completionIdentity)
        {
            if (!producerNodeId.IsValid || completionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            m_ProducerNodeId = producerNodeId;
            m_CompletionIdentity = completionIdentity;
        }
    }

    internal sealed class CharacterPoseNativeLocalPoseValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeLocalPoseValue(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
            : base(producerNodeId, native.CompletionIdentity)
        {
            ValidateNative(producerNodeId, in native);
            Native = native;
        }

        internal CharacterPoseNativeLocalPoseValue(
            PoseNodeId producerNodeId,
            AnimationPoseValue pose)
            : base(producerNodeId, pose.CompletionIdentity)
        {
            if (!pose.NodeId.IsValid || pose.Availability == AnimationPoseAvailability.Invalid)
                throw new ArgumentException("Pose native local value is invalid.");
            Pose = pose;
        }

        internal CharacterPoseNativePoseReadBinding Native { get; private set; }
        internal AnimationPoseValue Pose { get; private set; }

        internal static CharacterPoseNativeLocalPoseValue Reuse(
            CharacterPoseNativeLocalPoseValue value,
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            if (value == null)
                return new CharacterPoseNativeLocalPoseValue(producerNodeId, in native);
            value.Refresh(producerNodeId, in native);
            return value;
        }

        void Refresh(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            ValidateNative(producerNodeId, in native);
            SetIdentity(producerNodeId, native.CompletionIdentity);
            Native = native;
            Pose = default;
        }

        static void ValidateNative(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            if (!producerNodeId.IsValid || native.CompletionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            if (!native.IsValid ||
                native.Space != CharacterPoseSpace.Local ||
                native.Availability[0] == AnimationPoseAvailability.Invalid)
            {
                throw new ArgumentException(
                    "Pose native local value is invalid.",
                    nameof(native));
            }
        }
    }

    internal sealed class CharacterPoseNativeComponentPoseValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeComponentPoseValue(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
            : base(producerNodeId, native.CompletionIdentity)
        {
            ValidateNative(producerNodeId, in native);
            Native = native;
        }

        internal CharacterPoseNativeComponentPoseValue(
            PoseNodeId producerNodeId,
            AnimationPoseValue pose)
            : base(producerNodeId, pose.CompletionIdentity)
        {
            if (!pose.NodeId.IsValid || pose.Availability == AnimationPoseAvailability.Invalid)
                throw new ArgumentException("Pose native component value is invalid.");
            Pose = pose;
        }

        internal CharacterPoseNativePoseReadBinding Native { get; private set; }
        internal AnimationPoseValue Pose { get; private set; }

        internal static CharacterPoseNativeComponentPoseValue Reuse(
            CharacterPoseNativeComponentPoseValue value,
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            if (value == null)
                return new CharacterPoseNativeComponentPoseValue(producerNodeId, in native);
            value.Refresh(producerNodeId, in native);
            return value;
        }

        void Refresh(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            ValidateNative(producerNodeId, in native);
            SetIdentity(producerNodeId, native.CompletionIdentity);
            Native = native;
            Pose = default;
        }

        static void ValidateNative(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            if (!producerNodeId.IsValid || native.CompletionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            if (!native.IsValid ||
                native.Space != CharacterPoseSpace.Component ||
                native.Availability[0] == AnimationPoseAvailability.Invalid)
            {
                throw new ArgumentException(
                    "Pose native component value is invalid.",
                    nameof(native));
            }
        }
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
            in CharacterPoseNativePoseReadBinding native)
            : base(producerNodeId, native.CompletionIdentity)
        {
            if (!native.IsValid || !native.Discontinuity[0].IsValid)
            {
                throw new ArgumentException(
                    "Pose native discontinuity value is invalid.",
                    nameof(native));
            }
            Native = native.Discontinuity[0];
        }

        internal CharacterPoseNativeDiscontinuityValue(
            PoseNodeId producerNodeId,
            PoseDiscontinuity value)
            : base(producerNodeId, value.CompletionIdentity)
        {
            if (!value.IsValid)
                throw new ArgumentException("Pose native discontinuity value is invalid.");
            Value = value;
        }

        internal PoseDiscontinuityNative Native { get; }
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
