using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseSourceScalarReadView
    {
        internal CharacterPoseSourceScalarReadView(
            AnimationPhysicalSourceIdentity physicalIdentity,
            in AnimationPoseSourceCaptureBinding capture)
            : this(
                physicalIdentity,
                capture.SourceId,
                capture.CompletionIdentity,
                capture.PoseParameters,
                capture.PoseParameterAvailability)
        {
        }

        internal CharacterPoseSourceScalarReadView(
            AnimationPhysicalSourceIdentity physicalIdentity,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity,
            NativeSlice<float> values,
            NativeSlice<byte> availability)
        {
            if (!physicalIdentity.IsValid || !sourceId.IsValid ||
                completionIdentity == 0 ||
                values.Length == 0 || values.Length != availability.Length)
                throw new ArgumentException("Character Pose source scalar read view is invalid.");
            PhysicalIdentity = physicalIdentity;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
            Values = values;
            Availability = availability;
        }

        internal AnimationPhysicalSourceIdentity PhysicalIdentity { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeSlice<float> Values { get; }
        internal NativeSlice<byte> Availability { get; }
        internal int ParameterCount => Values.Length;
        internal bool IsValid => PhysicalIdentity.IsValid && SourceId.IsValid &&
                                 CompletionIdentity != 0 &&
                                 Values.Length > 0 && Values.Length == Availability.Length;
    }
}
