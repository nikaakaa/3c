using UnityEngine;
using GoalSource = ThirdPersonCharacter.Pipeline.Editor.CharacterFootGoalCsvSource;
using GoalColumn = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootGoalCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootGoalSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootGoalSample
    {
        internal bool Available;
        internal Vector3 Correction;
        internal Vector3 Position;
        internal Quaternion Rotation;
        internal float PositionWeight;
        internal float RotationWeight;
        internal float PelvisPositionWeight;
        internal float PelvisRotationWeight;
    }

    internal static class CharacterFootGoalColumns
    {
        internal static readonly CharacterFootCsvGroup<GoalSource, CharacterFootGoalSample> Schema =
            new CharacterFootCsvGroup<GoalSource, CharacterFootGoalSample>(
                "Goal", () => new CharacterFootGoalSample(), new GoalColumn[]
                {
                    GoalColumn.Create("FootMotionEncodedGoalAvailable", Codecs.Boolean, Unit.None,
                        (in GoalSource source) => source.Goal.IsValid, (target, value) => target.Available = value),
                    GoalColumn.Create("FootMotionEncodedGoalCorrection", Codecs.Vector, Unit.Metres,
                        (in GoalSource source) => source.Goal.IsValid ? source.Goal.ComponentPosition - source.OriginalAnkle : default, (target, value) => target.Correction = value, "FootMotionEncodedGoalAvailable"),
                    GoalColumn.Create("FinalGoalPosition", Codecs.Vector, Unit.Metres,
                        (in GoalSource source) => source.Goal.ComponentPosition, (target, value) => target.Position = value),
                    GoalColumn.Create("FinalGoalRotation", Codecs.Rotation, Unit.Unitless,
                        (in GoalSource source) => source.Goal.ComponentRotation, (target, value) => target.Rotation = value),
                    GoalColumn.Create("FinalGoalPositionWeight", Codecs.Float32, Unit.Unitless,
                        (in GoalSource source) => source.Goal.PositionWeight, (target, value) => target.PositionWeight = value),
                    GoalColumn.Create("FinalGoalRotationWeight", Codecs.Float32, Unit.Unitless,
                        (in GoalSource source) => source.Goal.RotationWeight, (target, value) => target.RotationWeight = value),
                    GoalColumn.Create("PelvisPositionWeight", Codecs.Float32, Unit.Unitless,
                        (in GoalSource source) => source.PelvisGoal.PositionWeight, (target, value) => target.PelvisPositionWeight = value),
                    GoalColumn.Create("PelvisRotationWeight", Codecs.Float32, Unit.Unitless,
                        (in GoalSource source) => source.PelvisGoal.RotationWeight, (target, value) => target.PelvisRotationWeight = value),
                });
    }
}
