using System.Text;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public static class LocalRollbackSynctestLogFormatter
    {
        public static string FormatPass(in LocalRollbackSynctestResult result)
        {
            return $"[rollback-synctest] PASS restore={result.RestoreTick.Value} end={result.EndTick.Value}";
        }

        public static string FormatFail(in LocalRollbackSynctestResult result)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[rollback-synctest] FAIL");
            if (!string.IsNullOrWhiteSpace(result.FailureReason))
                builder.Append(" reason=").Append(result.FailureReason);
            builder.Append(" restore=").Append(result.RestoreTick.Value);
            builder.Append(" end=").Append(result.EndTick.Value);
            builder.Append(" finalMatches=").Append(result.Comparison.Matches);
            if (result.FirstMismatch.HasMismatch)
            {
                builder.Append(" firstStage=").Append(result.FirstMismatch.Stage);
                builder.Append(" firstTick=").Append(result.FirstMismatch.Tick.Value);
            }
            if (result.Comparison.Differences.Count > 0)
                builder.Append(" differences=").Append(string.Join(",", result.Comparison.Differences));

            return builder.ToString();
        }

        public static string FormatFirstMismatch(in LocalRollbackSynctestResult result)
        {
            LocalRollbackSynctestFirstMismatch mismatch = result.FirstMismatch;
            StringBuilder builder = new StringBuilder(512);
            builder.Append("[rollback-synctest] first-mismatch");
            builder.Append(" stage=").Append(mismatch.Stage);
            builder.Append(" tick=").Append(mismatch.Tick.Value);
            builder.Append(" restore=").Append(result.RestoreTick.Value);
            builder.Append(" end=").Append(result.EndTick.Value);
            builder.Append(" differences=").Append(string.Join(",", mismatch.Comparison.Differences));
            if (mismatch.HasInput)
            {
                PredictionInputFrame input = mismatch.Input;
                builder.Append(" inputMove=").Append(Format(input.Move));
                builder.Append(" inputLook=").Append(Format(input.Look));
                builder.Append(" run=").Append(input.RunHeld);
                builder.Append(" inputCameraBasis=").Append(input.HasCameraBasis).Append('/').Append(input.CameraBasisState.Yaw.ToString("F3"));
                AppendButton(builder, "dodge", input.Dodge);
                AppendButton(builder, "attack", input.Attack);
                AppendButton(builder, "jump", input.Jump);
                AppendButton(builder, "interact", input.Interact);
            }
            else
            {
                builder.Append(" input=none");
            }

            builder.Append(" expected={").Append(DescribeSnapshot(mismatch.Expected)).Append('}');
            builder.Append(" actual={").Append(DescribeSnapshot(mismatch.Actual)).Append('}');
            return builder.ToString();
        }

        public static string DescribeSnapshot(in CharacterSimulationSnapshot snapshot)
        {
            var locomotion = snapshot.RuntimeBlackboard.Locomotion;
            var action = snapshot.RuntimeBlackboard.Action;
            var animation = snapshot.RuntimeBlackboard.Animation;
            var motion = snapshot.MotionExecutorState;
            return
                $"pos={Format(snapshot.Position)} yaw={snapshot.Yaw:F3} cameraBasisYaw={snapshot.CameraBasisState.Yaw:F3} " +
                $"world={Format(snapshot.CurrentWorldDirection)} phase={snapshot.LocomotionPhase} gait={snapshot.LocomotionGait} " +
                $"animKey={snapshot.AnimationKey} animNorm={snapshot.AnimationNormalizedTime:F6} " +
                $"motionSpeed={motion.CurrentSpeed:F3} motionLast={Format(motion.LastWorldDirection)} motionY={motion.VerticalVelocity:F3} motionRoot={motion.HasRootPose}/{Format(motion.RootPosition)}/{motion.RootYaw:F3} " +
                $"bbWorld={Format(locomotion.WorldDirection)} bbMove={locomotion.HasMoveIntent}/{locomotion.MoveStrength:F3} bbStep={locomotion.SourceStep} " +
                $"bbAction={action.Active}/{action.State}/{action.HasMovement}/{Format(action.WorldDirection)}/{action.PlanarDistance:F3}/step={action.SourceStep} " +
                $"bbAnim={animation.LocomotionProgress.Phase}/{animation.LocomotionProgress.AliasKey}/{animation.LocomotionProgress.NormalizedTime:F6}/valid={animation.LocomotionProgress.HasValidPlayback}/ended={animation.LocomotionProgress.IsEnded}/name={animation.LocomotionAnimationName}/step={animation.SourceStep}";
        }

        static void AppendButton(StringBuilder builder, string name, in PredictionButtonFrame button)
        {
            if (!button.Pressed && !button.Held && !button.Released)
                return;

            builder.Append(' ').Append(name).Append('=');
            builder.Append(button.Pressed ? 'P' : '-');
            builder.Append(button.Held ? 'H' : '-');
            builder.Append(button.Released ? 'R' : '-');
        }

        public static string Format(Vector2 value)
        {
            return $"({value.x:F3},{value.y:F3})";
        }

        public static string Format(Vector3 value)
        {
            return $"({value.x:F3},{value.y:F3},{value.z:F3})";
        }
    }
}
