using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseConstraintTuningView
    {
        internal CharacterPoseConstraintTuningView(ulong generation)
        {
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            Generation = generation;
        }

        internal ulong Generation { get; }
        internal bool IsValid => Generation != 0;
    }

    internal readonly struct CharacterPoseTuningSnapshot
    {
        internal CharacterPoseTuningSnapshot(
            in CharacterPoseProgramTuningView program,
            in CharacterPoseSourceTuningView source,
            in CharacterPoseConstraintTuningView constraint)
        {
            Program = program;
            Source = source;
            Constraint = constraint;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Character Pose tuning snapshot is inconsistent.");
            }
        }

        internal CharacterPoseProgramTuningView Program { get; }
        internal CharacterPoseSourceTuningView Source { get; }
        internal CharacterPoseConstraintTuningView Constraint { get; }
        internal ulong Generation => Program.Generation;
        internal bool IsValid =>
            Program.IsValid &&
            Source.IsValid &&
            Constraint.IsValid &&
            Program.Generation == Source.Generation &&
            Program.Generation == Constraint.Generation;

        internal void RequireGeneration(ulong generation)
        {
            if (!IsValid || Generation != generation)
            {
                throw new InvalidOperationException(
                    "Character Pose tuning snapshot differs from the root frame.");
            }
        }
    }
}
