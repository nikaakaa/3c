using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterSimulationSnapshotComparison
    {
        public CharacterSimulationSnapshotComparison(bool matches, string[] differences)
        {
            Matches = matches;
            Differences = differences ?? System.Array.Empty<string>();
        }

        public bool Matches { get; }
        public IReadOnlyList<string> Differences { get; }
    }

    public readonly struct CharacterSimulationSnapshotTolerance
    {
        public CharacterSimulationSnapshotTolerance(float position, float yaw, float stateTime, float animationTime)
        {
            Position = Mathf.Max(0f, position);
            Yaw = Mathf.Max(0f, yaw);
            StateTime = Mathf.Max(0f, stateTime);
            AnimationTime = Mathf.Max(0f, animationTime);
        }

        public float Position { get; }
        public float Yaw { get; }
        public float StateTime { get; }
        public float AnimationTime { get; }

        public static CharacterSimulationSnapshotTolerance Default =>
            new CharacterSimulationSnapshotTolerance(0.001f, 0.01f, 0.0001f, 0.0001f);
    }

    public static class CharacterSimulationSnapshotComparer
    {
        public static CharacterSimulationSnapshotComparison Compare(
            in CharacterSimulationSnapshot expected,
            in CharacterSimulationSnapshot actual,
            in CharacterSimulationSnapshotTolerance tolerance)
        {
            List<string> differences = new List<string>();

            if (expected.Tick != actual.Tick)
                differences.Add("tick");
            if (Vector3.Distance(expected.Position, actual.Position) > tolerance.Position)
                differences.Add("position");
            if (Mathf.Abs(Mathf.DeltaAngle(expected.Yaw, actual.Yaw)) > tolerance.Yaw)
                differences.Add("yaw");
            CharacterStateMachineSnapshot expectedState = ResolveComparedState(in expected);
            CharacterStateMachineSnapshot actualState = ResolveComparedState(in actual);
            if (expectedState.ActiveState != actualState.ActiveState)
                differences.Add("activeState");
            if (Mathf.Abs(expectedState.StateTime - actualState.StateTime) > tolerance.StateTime)
                differences.Add("stateTime");
            if (expectedState.Variant != actualState.Variant)
                differences.Add("variant");
            if (!string.Equals(expectedState.PendingTransitionPath, actualState.PendingTransitionPath, System.StringComparison.Ordinal))
                differences.Add("pendingTransition");
            if (expected.RunLatchActive != actual.RunLatchActive)
                differences.Add("runLatch");
            if (expected.LastMovingGait != actual.LastMovingGait)
                differences.Add("lastMovingGait");
            if (expected.LocomotionPhase != actual.LocomotionPhase)
                differences.Add("locomotionPhase");
            if (expected.LocomotionGait != actual.LocomotionGait)
                differences.Add("locomotionGait");
            if (!string.Equals(expected.AnimationKey, actual.AnimationKey, System.StringComparison.Ordinal))
                differences.Add("animationKey");
            if (Mathf.Abs(expected.AnimationNormalizedTime - actual.AnimationNormalizedTime) > tolerance.AnimationTime)
                differences.Add("animationNormalizedTime");
            CompareMotionExecutorState(expected.MotionExecutorState, actual.MotionExecutorState, in tolerance, differences);
            CompareRuntimeBlackboard(in expected, in actual, in tolerance, differences);

            return new CharacterSimulationSnapshotComparison(differences.Count == 0, differences.ToArray());
        }

        static CharacterStateMachineSnapshot ResolveComparedState(in CharacterSimulationSnapshot snapshot)
        {
            CharacterStateMachineSnapshot fullBody = snapshot.FullBodyRestoreState.Snapshot;
            return fullBody.ActiveState.IsValid ? fullBody : snapshot.StateMachine;
        }

        static void CompareRuntimeBlackboard(
            in CharacterSimulationSnapshot expected,
            in CharacterSimulationSnapshot actual,
            in CharacterSimulationSnapshotTolerance tolerance,
            List<string> differences)
        {
            CharacterRuntimeBlackboardSnapshot expectedBlackboard = expected.RuntimeBlackboard;
            CharacterRuntimeBlackboardSnapshot actualBlackboard = actual.RuntimeBlackboard;

            CompareLocomotionFacts(expectedBlackboard.Locomotion, actualBlackboard.Locomotion, in tolerance, differences);
            CompareActionFacts(expectedBlackboard.Action, actualBlackboard.Action, in tolerance, differences);
            CompareAnimationFacts(expectedBlackboard.Animation, actualBlackboard.Animation, in tolerance, differences);
        }

        static void CompareMotionExecutorState(
            MotionExecutorRollbackState expected,
            MotionExecutorRollbackState actual,
            in CharacterSimulationSnapshotTolerance tolerance,
            List<string> differences)
        {
            if (Mathf.Abs(expected.CurrentSpeed - actual.CurrentSpeed) > tolerance.Position)
                differences.Add("motionExecutor.currentSpeed");
            if (Vector3.Distance(expected.LastWorldDirection, actual.LastWorldDirection) > tolerance.Position)
                differences.Add("motionExecutor.lastWorldDirection");
            if (Mathf.Abs(expected.VerticalVelocity - actual.VerticalVelocity) > tolerance.Position)
                differences.Add("motionExecutor.verticalVelocity");
            if (expected.HasRootPose != actual.HasRootPose)
                differences.Add("motionExecutor.hasRootPose");
            if (expected.HasRootPose && actual.HasRootPose)
            {
                if (Vector3.Distance(expected.RootPosition, actual.RootPosition) > tolerance.Position)
                    differences.Add("motionExecutor.rootPosition");
                if (Mathf.Abs(Mathf.DeltaAngle(expected.RootYaw, actual.RootYaw)) > tolerance.Yaw)
                    differences.Add("motionExecutor.rootYaw");
            }
        }

        static void CompareLocomotionFacts(
            CharacterRuntimeLocomotionFacts expected,
            CharacterRuntimeLocomotionFacts actual,
            in CharacterSimulationSnapshotTolerance tolerance,
            List<string> differences)
        {
            if (expected.Phase != actual.Phase)
                differences.Add("blackboard.locomotion.phase");
            if (expected.FrameGait != actual.FrameGait)
                differences.Add("blackboard.locomotion.frameGait");
            if (expected.LastMovingGait != actual.LastMovingGait)
                differences.Add("blackboard.locomotion.lastMovingGait");
            if (expected.HasMoveStopEntryGait != actual.HasMoveStopEntryGait)
                differences.Add("blackboard.locomotion.hasMoveStopEntryGait");
            if (expected.MoveStopEntryGait != actual.MoveStopEntryGait)
                differences.Add("blackboard.locomotion.moveStopEntryGait");
            if (expected.RunLatchActive != actual.RunLatchActive)
                differences.Add("blackboard.locomotion.runLatchActive");
            if (Vector3.Distance(expected.WorldDirection, actual.WorldDirection) > tolerance.Position)
                differences.Add("blackboard.locomotion.worldDirection");
            if (expected.HasMoveIntent != actual.HasMoveIntent)
                differences.Add("blackboard.locomotion.hasMoveIntent");
            if (Mathf.Abs(expected.MoveStrength - actual.MoveStrength) > tolerance.AnimationTime)
                differences.Add("blackboard.locomotion.moveStrength");
        }

        static void CompareActionFacts(
            CharacterRuntimeActionFacts expected,
            CharacterRuntimeActionFacts actual,
            in CharacterSimulationSnapshotTolerance tolerance,
            List<string> differences)
        {
            if (expected.Active != actual.Active)
                differences.Add("blackboard.action.active");
            if (expected.State != actual.State)
                differences.Add("blackboard.action.state");
            if (expected.Completed != actual.Completed)
                differences.Add("blackboard.action.completed");
            if (expected.ExitedToLocomotion != actual.ExitedToLocomotion)
                differences.Add("blackboard.action.exitedToLocomotion");
            if (expected.HasMovement != actual.HasMovement)
                differences.Add("blackboard.action.hasMovement");
            if (Vector3.Distance(expected.WorldDirection, actual.WorldDirection) > tolerance.Position)
                differences.Add("blackboard.action.worldDirection");
            if (Mathf.Abs(expected.PlanarDistance - actual.PlanarDistance) > tolerance.Position)
                differences.Add("blackboard.action.planarDistance");
            if (expected.RotateToDirection != actual.RotateToDirection)
                differences.Add("blackboard.action.rotateToDirection");
        }

        static void CompareAnimationFacts(
            CharacterRuntimeAnimationFacts expected,
            CharacterRuntimeAnimationFacts actual,
            in CharacterSimulationSnapshotTolerance tolerance,
            List<string> differences)
        {
            if (expected.LocomotionProgress.Phase != actual.LocomotionProgress.Phase)
                differences.Add("blackboard.animation.locomotionPhase");
            if (!string.Equals(expected.LocomotionProgress.AliasKey, actual.LocomotionProgress.AliasKey, System.StringComparison.Ordinal))
                differences.Add("blackboard.animation.locomotionAlias");
            if (Mathf.Abs(expected.LocomotionProgress.NormalizedTime - actual.LocomotionProgress.NormalizedTime) > tolerance.AnimationTime)
                differences.Add("blackboard.animation.locomotionNormalizedTime");
            if (expected.LocomotionProgress.HasValidPlayback != actual.LocomotionProgress.HasValidPlayback)
                differences.Add("blackboard.animation.locomotionHasValidPlayback");
            if (expected.LocomotionProgress.IsEnded != actual.LocomotionProgress.IsEnded)
                differences.Add("blackboard.animation.locomotionIsEnded");
            if (!string.Equals(expected.LocomotionAnimationName, actual.LocomotionAnimationName, System.StringComparison.Ordinal))
                differences.Add("blackboard.animation.locomotionAnimationName");
            if (expected.ActionKey != actual.ActionKey)
                differences.Add("blackboard.animation.actionKey");
            if (Mathf.Abs(expected.ActionNormalizedTime - actual.ActionNormalizedTime) > tolerance.AnimationTime)
                differences.Add("blackboard.animation.actionNormalizedTime");
            if (expected.ActionHasValidPlayback != actual.ActionHasValidPlayback)
                differences.Add("blackboard.animation.actionHasValidPlayback");
            if (expected.ActionIsEnded != actual.ActionIsEnded)
                differences.Add("blackboard.animation.actionIsEnded");
            if (!string.Equals(expected.ActionAnimationName, actual.ActionAnimationName, System.StringComparison.Ordinal))
                differences.Add("blackboard.animation.actionAnimationName");
        }
    }
}
