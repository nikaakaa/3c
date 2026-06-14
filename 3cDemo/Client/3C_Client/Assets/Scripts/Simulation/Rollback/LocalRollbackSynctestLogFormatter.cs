using System.Text;
using ThirdPersonAnimation;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public static class LocalRollbackSynctestLogFormatter
    {
        public static string FormatPass(in LocalRollbackSynctestResult result)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[rollback-synctest] PASS restore=").Append(result.RestoreTick.Value);
            builder.Append(" end=").Append(result.EndTick.Value);
            AppendPresentationDifferences(builder, result.Comparison);
            if (result.FirstMismatch.HasPresentationDrift)
            {
                builder.Append(" firstPresentationStage=").Append(result.FirstMismatch.Stage);
                builder.Append(" firstPresentationTick=").Append(result.FirstMismatch.Tick.Value);
                AppendPresentationDifferences(builder, result.FirstMismatch.Comparison, " firstPresentationDifferences=");
            }

            return builder.ToString();
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
                AppendGameplayDifferences(builder, result.FirstMismatch.Comparison, " firstDifferences=");
                AppendPresentationDifferences(builder, result.FirstMismatch.Comparison, " firstPresentationDifferences=");
            }
            AppendGameplayDifferences(builder, result.Comparison);
            AppendPresentationDifferences(builder, result.Comparison);

            return builder.ToString();
        }

        public static string FormatFirstMismatch(in LocalRollbackSynctestResult result)
        {
            LocalRollbackSynctestFirstMismatch mismatch = result.FirstMismatch;
            StringBuilder builder = new StringBuilder(512);
            builder.Append(mismatch.HasMismatch ? "[rollback-synctest] first-mismatch" : "[rollback-synctest] first-presentation-drift");
            builder.Append(" stage=").Append(mismatch.Stage);
            builder.Append(" tick=").Append(mismatch.Tick.Value);
            builder.Append(" restore=").Append(result.RestoreTick.Value);
            builder.Append(" end=").Append(result.EndTick.Value);
            AppendGameplayDifferences(builder, mismatch.Comparison);
            AppendPresentationDifferences(builder, mismatch.Comparison);
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
                $"bbActionAnim={animation.ActionKey}/{animation.ActionNormalizedTime:F6}/valid={animation.ActionHasValidPlayback}/ended={animation.ActionIsEnded}/name={animation.ActionAnimationName} " +
                $"bbFoot={Format(animation.CurrentLocomotionFootPhase)} bbExitFoot={Format(animation.LastLocomotionExitFootPhase)} " +
                $"bbAnim={animation.LocomotionProgress.Phase}/{animation.LocomotionProgress.AliasKey}/{animation.LocomotionProgress.NormalizedTime:F6}/valid={animation.LocomotionProgress.HasValidPlayback}/ended={animation.LocomotionProgress.IsEnded}/name={animation.LocomotionAnimationName}/step={animation.SourceStep}";
        }

        static string Format(LocomotionFootPhaseSample sample)
        {
            return $"{sample.Phase}/{sample.Gait}/{sample.AliasKey}/{sample.FootPhase}/norm={sample.NormalizedTime:F6}/valid={sample.IsValid}/step={sample.SourceStep}";
        }

        static void AppendGameplayDifferences(
            StringBuilder builder,
            in CharacterSimulationSnapshotComparison comparison,
            string label = " differences=")
        {
            if (comparison.Differences.Count > 0)
                builder.Append(label).Append(string.Join(",", comparison.Differences));
        }

        static void AppendPresentationDifferences(StringBuilder builder, in CharacterSimulationSnapshotComparison comparison, string label = " presentationDifferences=")
        {
            if (comparison.PresentationDifferences.Count > 0)
                builder.Append(label).Append(string.Join(",", comparison.PresentationDifferences));
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
