using System;
using BTSMTL.EventGraphs;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseNativePoseReadBinding
    {
        readonly NativeSlice<AnimationLocalBonePose> m_DenseLocalPoses;
        readonly NativeSlice<AnimationBlendBoneVelocity> m_DenseVelocities;
        readonly NativeSlice<float> m_PoseParameters;
        readonly NativeSlice<byte> m_PoseParameterAvailability;
        readonly NativeSlice<AnimationPrimitivePoseContribution> m_Contributions;
        readonly NativeSlice<float> m_DenseContributionWeights;
        readonly NativeSlice<int> m_ContributionCount;
        readonly NativeSlice<float> m_OutputWeight;
        readonly NativeSlice<AnimationFootFeatureSample> m_LeftFootFeatures;
        readonly NativeSlice<AnimationFootFeatureSample> m_RightFootFeatures;
        readonly NativeSlice<byte> m_HasFootFeatures;
        readonly NativeSlice<AnimationPoseAvailability> m_Availability;
        readonly NativeSlice<ulong> m_ContinuityIdentity;
        readonly NativeSlice<PoseDiscontinuityNative> m_Discontinuity;
        readonly NativeSlice<AnimationPoseNativeInvalidReason> m_InvalidReason;
        readonly NativeSlice<ulong> m_CompletedAt;

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
            m_DenseLocalPoses = output.DenseLocalPoses;
            m_DenseVelocities = output.DenseVelocities;
            m_PoseParameters = output.PoseParameters;
            m_PoseParameterAvailability = output.PoseParameterAvailability;
            m_Contributions = output.Contributions;
            m_DenseContributionWeights = output.DenseContributionWeights;
            m_ContributionCount = output.ContributionCount;
            m_OutputWeight = output.OutputWeight;
            m_LeftFootFeatures = output.LeftFootFeatures;
            m_RightFootFeatures = output.RightFootFeatures;
            m_HasFootFeatures = output.HasFootFeatures;
            m_Availability = output.Availability;
            m_ContinuityIdentity = output.ContinuityIdentity;
            m_Discontinuity = output.Discontinuity;
            m_InvalidReason = output.InvalidReason;
            m_CompletedAt = output.CompletedAt;
        }

        internal ulong CompletionIdentity { get; }
        internal CharacterPoseSpace Space { get; }
        internal ref readonly NativeSlice<AnimationLocalBonePose> DenseLocalPoses => ref m_DenseLocalPoses;
        internal ref readonly NativeSlice<AnimationBlendBoneVelocity> DenseVelocities => ref m_DenseVelocities;
        internal ref readonly NativeSlice<float> PoseParameters => ref m_PoseParameters;
        internal ref readonly NativeSlice<byte> PoseParameterAvailability => ref m_PoseParameterAvailability;
        internal ref readonly NativeSlice<AnimationPrimitivePoseContribution> Contributions => ref m_Contributions;
        internal ref readonly NativeSlice<float> DenseContributionWeights => ref m_DenseContributionWeights;
        internal ref readonly NativeSlice<int> ContributionCount => ref m_ContributionCount;
        internal ref readonly NativeSlice<float> OutputWeight => ref m_OutputWeight;
        internal ref readonly NativeSlice<AnimationFootFeatureSample> LeftFootFeatures => ref m_LeftFootFeatures;
        internal ref readonly NativeSlice<AnimationFootFeatureSample> RightFootFeatures => ref m_RightFootFeatures;
        internal ref readonly NativeSlice<byte> HasFootFeatures => ref m_HasFootFeatures;
        internal ref readonly NativeSlice<AnimationPoseAvailability> Availability => ref m_Availability;
        internal ref readonly NativeSlice<ulong> ContinuityIdentity => ref m_ContinuityIdentity;
        internal ref readonly NativeSlice<PoseDiscontinuityNative> Discontinuity => ref m_Discontinuity;
        internal ref readonly NativeSlice<AnimationPoseNativeInvalidReason> InvalidReason => ref m_InvalidReason;
        internal ref readonly NativeSlice<ulong> CompletedAt => ref m_CompletedAt;
        internal bool IsValid => CompletionIdentity != 0;
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
        CharacterPoseNativePoseReadBinding m_Native;

        internal CharacterPoseNativeLocalPoseValue(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
            : base(producerNodeId, native.CompletionIdentity)
        {
            ValidateNative(producerNodeId, in native);
            m_Native = native;
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

        internal ref readonly CharacterPoseNativePoseReadBinding Native => ref m_Native;
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
            m_Native = native;
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
        CharacterPoseNativePoseReadBinding m_Native;

        internal CharacterPoseNativeComponentPoseValue(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
            : base(producerNodeId, native.CompletionIdentity)
        {
            ValidateNative(producerNodeId, in native);
            m_Native = native;
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

        internal ref readonly CharacterPoseNativePoseReadBinding Native => ref m_Native;
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
            m_Native = native;
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
                 value.Kind != EventGraphValueKind.Float32 &&
                 value.Kind != EventGraphValueKind.Vector3 &&
                 value.Kind != EventGraphValueKind.Quaternion))
            {
                throw new ArgumentException("Pose native parameter value is invalid.");
            }
            ParameterId = parameterId;
            Value = value;
        }

        internal PoseParameterId ParameterId { get; private set; }
        internal EventGraphValue Value { get; private set; }

        internal static CharacterPoseNativeParameterValue Reuse(
            CharacterPoseNativeParameterValue value,
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            PoseParameterId parameterId,
            EventGraphValue parameterValue)
        {
            if (value == null)
                return new CharacterPoseNativeParameterValue(
                    producerNodeId,
                    completionIdentity,
                    parameterId,
                    parameterValue);
            value.Refresh(
                producerNodeId,
                completionIdentity,
                parameterId,
                parameterValue);
            return value;
        }

        void Refresh(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            PoseParameterId parameterId,
            EventGraphValue parameterValue)
        {
            if (!producerNodeId.IsValid || completionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            if (!parameterId.IsValid ||
                parameterValue.Kind != EventGraphValueKind.Bool &&
                parameterValue.Kind != EventGraphValueKind.Int32 &&
                parameterValue.Kind != EventGraphValueKind.Float32 &&
                parameterValue.Kind != EventGraphValueKind.Vector3 &&
                parameterValue.Kind != EventGraphValueKind.Quaternion)
            {
                throw new ArgumentException("Pose native parameter value is invalid.");
            }
            SetIdentity(producerNodeId, completionIdentity);
            ParameterId = parameterId;
            Value = parameterValue;
        }
    }

    internal sealed class CharacterPoseNativeDiscontinuityValue : CharacterPoseNativePortValue
    {
        internal CharacterPoseNativeDiscontinuityValue(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
            : base(producerNodeId, native.CompletionIdentity)
        {
            ValidateNative(producerNodeId, in native);
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

        internal PoseDiscontinuityNative Native { get; private set; }
        internal PoseDiscontinuity Value { get; private set; }

        internal static CharacterPoseNativeDiscontinuityValue Reuse(
            CharacterPoseNativeDiscontinuityValue value,
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            if (value == null)
                return new CharacterPoseNativeDiscontinuityValue(producerNodeId, in native);
            value.Refresh(producerNodeId, in native);
            return value;
        }

        void Refresh(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            ValidateNative(producerNodeId, in native);
            SetIdentity(producerNodeId, native.CompletionIdentity);
            Native = native.Discontinuity[0];
            Value = default;
        }

        static void ValidateNative(
            PoseNodeId producerNodeId,
            in CharacterPoseNativePoseReadBinding native)
        {
            if (!producerNodeId.IsValid || native.CompletionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            if (!native.IsValid || !native.Discontinuity[0].IsValid)
            {
                throw new ArgumentException(
                    "Pose native discontinuity value is invalid.",
                    nameof(native));
            }
        }
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

        internal ActionAnimationPlaybackCommand Command { get; private set; }

        internal static CharacterPoseNativeActionPlaybackValue Reuse(
            CharacterPoseNativeActionPlaybackValue value,
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            ActionAnimationPlaybackCommand command)
        {
            if (value == null)
                return new CharacterPoseNativeActionPlaybackValue(
                    producerNodeId,
                    completionIdentity,
                    command);
            value.Refresh(producerNodeId, completionIdentity, command);
            return value;
        }

        void Refresh(
            PoseNodeId producerNodeId,
            ulong completionIdentity,
            ActionAnimationPlaybackCommand command)
        {
            if (!producerNodeId.IsValid || completionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            if (!command.IsValid)
                throw new ArgumentException("Pose native action playback value is invalid.");
            SetIdentity(producerNodeId, completionIdentity);
            Command = command;
        }
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

        internal CharacterFullBodyIkGoalSet Value { get; private set; }

        internal static CharacterPoseNativeFullBodyIkGoalsValue Reuse(
            CharacterPoseNativeFullBodyIkGoalsValue value,
            PoseNodeId producerNodeId,
            CharacterFullBodyIkGoalSet goalSet)
        {
            if (value == null)
                return new CharacterPoseNativeFullBodyIkGoalsValue(
                    producerNodeId,
                    goalSet);
            value.Refresh(producerNodeId, goalSet);
            return value;
        }

        void Refresh(
            PoseNodeId producerNodeId,
            CharacterFullBodyIkGoalSet goalSet)
        {
            if (!producerNodeId.IsValid || goalSet.Header.CompletionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            if (!goalSet.IsValid)
                throw new ArgumentException("Pose native Full Body IK goals value is invalid.");
            SetIdentity(producerNodeId, goalSet.Header.CompletionIdentity);
            Value = goalSet;
        }
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

        internal CharacterFullBodyIkGoalContributionHeader Header { get; private set; }
        internal NativeSlice<CharacterFullBodyIkGoal> Goals { get; private set; }

        internal static CharacterPoseNativeGoalContributionValue Reuse(
            CharacterPoseNativeGoalContributionValue value,
            PoseNodeId producerNodeId,
            CharacterFullBodyIkGoalContributionHeader header,
            NativeSlice<CharacterFullBodyIkGoal> goals)
        {
            if (value == null)
                return new CharacterPoseNativeGoalContributionValue(
                    producerNodeId,
                    header,
                    goals);
            value.Refresh(producerNodeId, header, goals);
            return value;
        }

        void Refresh(
            PoseNodeId producerNodeId,
            CharacterFullBodyIkGoalContributionHeader header,
            NativeSlice<CharacterFullBodyIkGoal> goals)
        {
            if (!producerNodeId.IsValid || header.CompletionIdentity == 0)
                throw new ArgumentException("Pose native port value identity is invalid.");
            if (!header.IsValid || goals.Length != header.GoalCount)
                throw new ArgumentException("Pose native goal contribution value is invalid.");
            SetIdentity(producerNodeId, header.CompletionIdentity);
            Header = header;
            Goals = goals;
        }
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
