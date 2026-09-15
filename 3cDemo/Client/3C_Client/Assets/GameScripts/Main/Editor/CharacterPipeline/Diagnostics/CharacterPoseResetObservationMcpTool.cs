using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.pose_reset_observation",
        Description = "Observe one explicit Scene Play Actor across two committed Pose frames and a formal Scene Play reset, then compare its FBBIK pose, goals, outputs, bend-history facts, runtime identity and scene generation.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = true,
        BackgroundPollingStatus = true,
        PollAction = "status",
        MaxPollSeconds = 120,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = true,
        OpenWorldHint = false)]
    public static class CharacterPoseResetObservationMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: start or status. Defaults to start.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Exact Scene Play Actor identity. Defaults to gameplay-lab-player.", Required = false)]
            public string actor_id { get; set; }

            [ToolParameter("Stable caller context identity echoed in the result.", Required = false)]
            public string context_id { get; set; }

            [ToolParameter("Stable job identity returned by start; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact active Scene Play scene path. Defaults to Assets/Scenes/GameplayLab/GameplayLab.unity.", Required = false)]
            public string scene_path { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            CharacterPoseResetObservationMcpJobScheduler.Handle(@params);

        internal static Observation Capture(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            AnimationReadOnlyBuffer<AnimationPoseWatchSnapshot> watches =
                snapshot.PoseWatches;
            var pose = new StringBuilder(16384);
            var goals = new StringBuilder(2048);
            var output = new StringBuilder(4096);
            BendFact left = default;
            BendFact right = default;
            ulong resetGeneration = 0;
            int poseCount = 0;
            int goalCount = 0;
            int solverCount = 0;
            int appliedGoalCount = 0;
            int effectorCount = 0;
            for (int watchIndex = 0; watchIndex < watches.Count; watchIndex++)
            {
                AnimationPoseWatchSnapshot watch = watches[watchIndex];
                pose.Append(watch.Identity).Append('|').Append((int)watch.Availability).Append('|');
                AnimationReadOnlyBuffer<CharacterComponentBonePose> componentPoses =
                    snapshot.GetPoseWatchComponentPoses(watchIndex);
                poseCount += componentPoses.Count;
                for (int i = 0; i < componentPoses.Count; i++)
                    Append(pose, componentPoses[i]);
                goals.Append(watch.Identity).Append('|').Append((int)watch.Availability).Append('|');
                AnimationReadOnlyBuffer<CharacterFullBodyIkGoal> goalValues =
                    snapshot.GetPoseWatchFullBodyIkGoals(watchIndex);
                goalCount += goalValues.Count;
                for (int i = 0; i < goalValues.Count; i++)
                    Append(goals, goalValues[i]);
                if (!snapshot.TryGetPoseWatchFullBodyIkSolver(
                        watchIndex,
                        out CharacterFullBodyIkSolverDiagnostics solver))
                    continue;
                solverCount++;
                resetGeneration = solver.BendResetGeneration;
                appliedGoalCount = solver.AppliedGoalCount;
                effectorCount = solver.EffectorCount;
                Append(output, solver);
                AnimationReadOnlyBuffer<CharacterFullBodyIkEffectorDiagnostics> effectors =
                    snapshot.GetPoseWatchFullBodyIkEffectors(watchIndex);
                for (int i = 0; i < effectors.Count; i++)
                    Append(output, effectors[i]);
                AnimationReadOnlyBuffer<CharacterFullBodyIkLimbDiagnostics> limbs =
                    snapshot.GetPoseWatchFullBodyIkLimbs(watchIndex);
                for (int i = 0; i < limbs.Count; i++)
                {
                    CharacterFullBodyIkLimbDiagnostics limb = limbs[i];
                    Append(output, limb);
                    if (limb.Limb == CharacterFullBodyIkLimbSlot.LeftLeg)
                        left = BendFact.From(limb.LegPose);
                    else if (limb.Limb == CharacterFullBodyIkLimbSlot.RightLeg)
                        right = BendFact.From(limb.LegPose);
                }
            }
            if (solverCount != 1 || !left.Available || !right.Available)
                throw new InvalidOperationException(
                    $"Expected one FBBIK solver with both legs; solvers={solverCount}.");
            return new Observation(
                Hash(pose), Hash(goals), Hash(output),
                poseCount, goalCount, appliedGoalCount, effectorCount,
                resetGeneration, left, right);
        }

        internal static void RequireEquivalent(
            Observation created,
            Observation used,
            Observation reset)
        {
            if (created.PoseHash != reset.PoseHash ||
                created.GoalHash != reset.GoalHash ||
                created.OutputHash != reset.OutputHash)
                throw new InvalidOperationException("Created and reset Preview results differ.");
            if (created.Left.HadStable || created.Left.HadApplied ||
                created.Right.HadStable || created.Right.HadApplied ||
                reset.Left.HadStable || reset.Left.HadApplied ||
                reset.Right.HadStable || reset.Right.HadApplied)
                throw new InvalidOperationException("Created or reset solve entered with retained bend history.");
            if (!used.Left.HadStable || !used.Left.HadApplied ||
                !used.Right.HadStable || !used.Right.HadApplied)
                throw new InvalidOperationException("History-used solve did not enter with both bend histories.");
            if (reset.ResetGeneration <= used.ResetGeneration)
                throw new InvalidOperationException("Reset generation did not advance.");
            if (created.Left.Source != reset.Left.Source ||
                created.Right.Source != reset.Right.Source)
                throw new InvalidOperationException("Created and reset direction sources differ.");
        }

        static void Append(StringBuilder value, CharacterComponentBonePose pose)
        {
            Append(value, pose.Position);
            Append(value, pose.Rotation);
            Append(value, pose.Scale);
        }

        static void Append(StringBuilder value, CharacterFullBodyIkGoal goal)
        {
            value.Append((int)goal.Slot).Append('|');
            Append(value, goal.ComponentPosition);
            Append(value, goal.ComponentRotation);
            Append(value, goal.PositionWeight);
            Append(value, goal.RotationWeight);
            value.Append((int)goal.Application).Append('|')
                .Append((int)goal.SourceKind).Append('|')
                .Append(goal.DiagnosticMetadataIndex).Append('|');
        }

        static void Append(StringBuilder value, CharacterFullBodyIkSolverDiagnostics solver)
        {
            value.Append(solver.BackendIdentity).Append('|')
                .Append(solver.RigId).Append('|').Append(solver.RigRevision).Append('|')
                .Append(solver.ProfileId).Append('|').Append(solver.ProfileRevision).Append('|')
                .Append(solver.Iterations).Append('|').Append(solver.FabrikPass ? 1 : 0).Append('|')
                .Append((int)solver.Failure).Append('|').Append(solver.FailedGoalSetIndex).Append('|')
                .Append((int)solver.FailedSlot).Append('|').Append(solver.AppliedGoalCount).Append('|')
                .Append(solver.EffectorCount).Append('|').Append(solver.LimbCount).Append('|');
            Append(value, solver.PelvisPreSolveTranslation);
        }

        static void Append(StringBuilder value, CharacterFullBodyIkEffectorDiagnostics effector)
        {
            value.Append((int)effector.Slot).Append('|')
                .Append((int)effector.SourceKind).Append('|')
                .Append((int)effector.Application).Append('|');
            Append(value, effector.TargetComponentPosition);
            Append(value, effector.TargetComponentRotation);
            Append(value, effector.PositionWeight);
            Append(value, effector.RotationWeight);
            Append(value, effector.SolvedComponentPosition);
            Append(value, effector.SolvedComponentRotation);
            Append(value, effector.PositionResidual);
            Append(value, effector.RotationResidualDegrees);
        }

        static void Append(StringBuilder value, CharacterFullBodyIkLimbDiagnostics limb)
        {
            value.Append((int)limb.Limb).Append('|');
            Append(value, limb.Pull);
            Append(value, limb.Reach);
            Append(value, limb.BendWeight);
            Append(value, limb.BendClamp);
            CharacterFullBodyIkLegPoseDiagnostics leg = limb.LegPose;
            if (!leg.IsAvailable)
                return;
            Append(value, leg.OriginalHip);
            Append(value, leg.OriginalKnee);
            Append(value, leg.OriginalAnkle);
            Append(value, leg.TargetAnkle);
            Append(value, leg.SolvedHip);
            Append(value, leg.SolvedKnee);
            Append(value, leg.SolvedAnkle);
            Append(value, leg.EffectiveBendDirection);
            Append(value, leg.OriginalBendDegrees);
            Append(value, leg.SolvedBendDegrees);
            Append(value, leg.OriginalExtensionRatio);
            Append(value, leg.TargetExtensionRatio);
            Append(value, leg.SolvedExtensionRatio);
            Append(value, leg.OriginalCompressionReserve);
            Append(value, leg.TargetCompressionReserve);
            Append(value, leg.SolvedCompressionReserve);
            Append(value, leg.StabilizationWeight);
        }

        static void Append(StringBuilder value, Vector3 vector)
        {
            Append(value, vector.x);
            Append(value, vector.y);
            Append(value, vector.z);
        }

        static void Append(StringBuilder value, Quaternion rotation)
        {
            Append(value, rotation.x);
            Append(value, rotation.y);
            Append(value, rotation.z);
            Append(value, rotation.w);
        }

        static void Append(StringBuilder value, float number) =>
            value.Append(number.ToString("R", CultureInfo.InvariantCulture)).Append('|');

        static string Hash(StringBuilder value)
        {
            using SHA256 sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(value.ToString());
            byte[] hash = sha.ComputeHash(bytes);
            var result = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        internal readonly struct BendFact
        {
            BendFact(
                bool available,
                bool hadStable,
                bool hadApplied,
                CharacterFullBodyIkBendDirectionSource source)
            {
                Available = available;
                HadStable = hadStable;
                HadApplied = hadApplied;
                Source = source;
            }

            internal bool Available { get; }
            internal bool HadStable { get; }
            internal bool HadApplied { get; }
            internal CharacterFullBodyIkBendDirectionSource Source { get; }

            internal static BendFact From(CharacterFullBodyIkLegPoseDiagnostics pose) =>
                new BendFact(
                    pose.IsAvailable,
                    pose.HadStableBendDirection,
                    pose.HadAppliedBendDirection,
                    pose.BendDirectionSource);

            internal object ToResult() => new
            {
                available = Available,
                had_stable = HadStable,
                had_applied = HadApplied,
                source = Source.ToString()
            };
        }

        internal sealed class Observation
        {
            internal Observation(
                string poseHash,
                string goalHash,
                string outputHash,
                int poseValueCount,
                int goalCount,
                int appliedGoalCount,
                int effectorCount,
                ulong resetGeneration,
                BendFact left,
                BendFact right)
            {
                PoseHash = poseHash;
                GoalHash = goalHash;
                OutputHash = outputHash;
                PoseValueCount = poseValueCount;
                GoalCount = goalCount;
                AppliedGoalCount = appliedGoalCount;
                EffectorCount = effectorCount;
                ResetGeneration = resetGeneration;
                Left = left;
                Right = right;
            }

            internal string PoseHash { get; }
            internal string GoalHash { get; }
            internal string OutputHash { get; }
            internal int PoseValueCount { get; }
            internal int GoalCount { get; }
            internal int AppliedGoalCount { get; }
            internal int EffectorCount { get; }
            internal ulong ResetGeneration { get; }
            internal BendFact Left { get; }
            internal BendFact Right { get; }

            internal object ToResult() => new
            {
                reset_generation = ResetGeneration,
                goal_count = GoalCount,
                applied_goal_count = AppliedGoalCount,
                effector_count = EffectorCount,
                pose_hash = PoseHash,
                goal_hash = GoalHash,
                output_hash = OutputHash,
                left = Left.ToResult(),
                right = Right.ToResult()
            };
        }
    }
}
