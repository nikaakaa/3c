using System.Collections.Generic;
using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterSimulationSnapshotComparison
    {
        public CharacterSimulationSnapshotComparison(bool matches, string[] differences)
            : this(matches, differences, System.Array.Empty<string>())
        {
        }

        public CharacterSimulationSnapshotComparison(bool matches, string[] differences, string[] presentationDifferences)
        {
            Matches = matches;
            Differences = differences ?? System.Array.Empty<string>();
            PresentationDifferences = presentationDifferences ?? System.Array.Empty<string>();
        }

        public CharacterSimulationSnapshotComparison(string[] differences, string[] presentationDifferences)
        {
            Differences = differences ?? System.Array.Empty<string>();
            PresentationDifferences = presentationDifferences ?? System.Array.Empty<string>();
            Matches = Differences.Count == 0;
        }

        public bool Matches { get; }
        public IReadOnlyList<string> Differences { get; }
        public IReadOnlyList<string> PresentationDifferences { get; }
        public bool HasPresentationDifferences => PresentationDifferences.Count > 0;
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
            List<string> presentationDifferences = new List<string>();
            RollbackCompareScope animationScope = PredictionRollbackScopeResolver.ResolveSnapshotAnimationScope(in expected, in actual);

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
                AddScopedDifference("animationKey", animationScope, differences, presentationDifferences);
            if (Mathf.Abs(expected.AnimationNormalizedTime - actual.AnimationNormalizedTime) > tolerance.AnimationTime)
                AddScopedDifference("animationNormalizedTime", animationScope, differences, presentationDifferences);
            CompareMotionExecutorState(expected.MotionExecutorState, actual.MotionExecutorState, in tolerance, differences);
            CompareRuntimeBlackboard(in expected, in actual, in tolerance, animationScope, differences, presentationDifferences);

            return new CharacterSimulationSnapshotComparison(differences.ToArray(), presentationDifferences.ToArray());
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
            RollbackCompareScope animationScope,
            List<string> differences,
            List<string> presentationDifferences)
        {
            CharacterRuntimeBlackboardSnapshot expectedBlackboard = expected.RuntimeBlackboard;
            CharacterRuntimeBlackboardSnapshot actualBlackboard = actual.RuntimeBlackboard;

            CompareLocomotionFacts(expectedBlackboard.Locomotion, actualBlackboard.Locomotion, in tolerance, differences);
            CompareActionFacts(expectedBlackboard.Action, actualBlackboard.Action, in tolerance, differences);
            CompareAnimationFacts(expectedBlackboard.Animation, actualBlackboard.Animation, in tolerance, animationScope, differences, presentationDifferences);
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
            RollbackCompareScope locomotionPlaybackScope,
            List<string> differences,
            List<string> presentationDifferences)
        {
            if (expected.LocomotionProgress.Phase != actual.LocomotionProgress.Phase)
                AddScopedDifference("blackboard.animation.locomotionPhase", locomotionPlaybackScope, differences, presentationDifferences);
            if (!string.Equals(expected.LocomotionProgress.AliasKey, actual.LocomotionProgress.AliasKey, System.StringComparison.Ordinal))
                AddScopedDifference("blackboard.animation.locomotionAlias", locomotionPlaybackScope, differences, presentationDifferences);
            if (Mathf.Abs(expected.LocomotionProgress.NormalizedTime - actual.LocomotionProgress.NormalizedTime) > tolerance.AnimationTime)
                AddScopedDifference("blackboard.animation.locomotionNormalizedTime", locomotionPlaybackScope, differences, presentationDifferences);
            if (expected.LocomotionProgress.HasValidPlayback != actual.LocomotionProgress.HasValidPlayback)
                AddScopedDifference("blackboard.animation.locomotionHasValidPlayback", locomotionPlaybackScope, differences, presentationDifferences);
            if (expected.LocomotionProgress.IsEnded != actual.LocomotionProgress.IsEnded)
                AddScopedDifference("blackboard.animation.locomotionIsEnded", locomotionPlaybackScope, differences, presentationDifferences);
            if (!string.Equals(expected.LocomotionAnimationName, actual.LocomotionAnimationName, System.StringComparison.Ordinal))
                presentationDifferences.Add("blackboard.animation.locomotionAnimationName");
            if (expected.ActionKey != actual.ActionKey)
                presentationDifferences.Add("blackboard.animation.actionKey");
            if (Mathf.Abs(expected.ActionNormalizedTime - actual.ActionNormalizedTime) > tolerance.AnimationTime)
                presentationDifferences.Add("blackboard.animation.actionNormalizedTime");
            if (expected.ActionHasValidPlayback != actual.ActionHasValidPlayback)
                presentationDifferences.Add("blackboard.animation.actionHasValidPlayback");
            if (expected.ActionIsEnded != actual.ActionIsEnded)
                presentationDifferences.Add("blackboard.animation.actionIsEnded");
            if (!string.Equals(expected.ActionAnimationName, actual.ActionAnimationName, System.StringComparison.Ordinal))
                presentationDifferences.Add("blackboard.animation.actionAnimationName");
            CompareFootPhaseSample(
                expected.CurrentLocomotionFootPhase,
                actual.CurrentLocomotionFootPhase,
                "blackboard.animation.currentFootPhase",
                in tolerance,
                locomotionPlaybackScope,
                differences,
                presentationDifferences);
            CompareFootPhaseSample(
                expected.LastLocomotionExitFootPhase,
                actual.LastLocomotionExitFootPhase,
                "blackboard.animation.lastExitFootPhase",
                in tolerance,
                locomotionPlaybackScope,
                differences,
                presentationDifferences);
        }

        static void CompareFootPhaseSample(
            LocomotionFootPhaseSample expected,
            LocomotionFootPhaseSample actual,
            string prefix,
            in CharacterSimulationSnapshotTolerance tolerance,
            RollbackCompareScope locomotionPlaybackScope,
            List<string> differences,
            List<string> presentationDifferences)
        {
            if (expected.IsValid != actual.IsValid)
                AddScopedDifference($"{prefix}.isValid", locomotionPlaybackScope, differences, presentationDifferences);
            if (expected.Phase != actual.Phase)
                AddScopedDifference($"{prefix}.phase", locomotionPlaybackScope, differences, presentationDifferences);
            if (expected.Gait != actual.Gait)
                AddScopedDifference($"{prefix}.gait", locomotionPlaybackScope, differences, presentationDifferences);
            if (!string.Equals(expected.AliasKey, actual.AliasKey, System.StringComparison.Ordinal))
                AddScopedDifference($"{prefix}.alias", locomotionPlaybackScope, differences, presentationDifferences);
            if (Mathf.Abs(expected.NormalizedTime - actual.NormalizedTime) > tolerance.AnimationTime)
                AddScopedDifference($"{prefix}.normalizedTime", locomotionPlaybackScope, differences, presentationDifferences);
            if (expected.FootPhase != actual.FootPhase)
                AddScopedDifference($"{prefix}.footPhase", locomotionPlaybackScope, differences, presentationDifferences);
            if (expected.SourceStep != actual.SourceStep)
                AddScopedDifference($"{prefix}.sourceStep", locomotionPlaybackScope, differences, presentationDifferences);
        }

        static void AddScopedDifference(
            string difference,
            RollbackCompareScope scope,
            List<string> differences,
            List<string> presentationDifferences)
        {
            switch (scope)
            {
                case RollbackCompareScope.StrictGameplay:
                    differences.Add(difference);
                    break;
                case RollbackCompareScope.PresentationDrift:
                case RollbackCompareScope.PredictiveGameplay:
                    presentationDifferences.Add(difference);
                    break;
            }
        }
    }
}
