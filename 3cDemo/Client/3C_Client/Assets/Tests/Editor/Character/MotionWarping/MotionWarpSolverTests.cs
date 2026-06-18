using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMotionWarping;
using ThirdPersonMovement;
using UnityEngine;

namespace Tests.Editor.Character.MotionWarping
{
    public sealed class MotionWarpSolverTests
    {
        [Test]
        public void ModelsDoNotExposeRuntimeObjects()
        {
            Type[] modelTypes =
            {
                typeof(MotionWarpPolicy),
                typeof(MotionWarpPayload),
                typeof(MotionWarpTargetSnapshot),
                typeof(MotionWarpRootSnapshot),
                typeof(MotionWarpInput),
                typeof(MotionWarpResult)
            };
            string[] forbidden =
            {
                "UnityEngine.Transform",
                "UnityEngine.GameObject",
                "UnityEngine.Animator",
                "UnityEngine.AnimationClip",
                "UnityEngine.CharacterController",
                "UnityEngine.InputSystem.InputAction"
            };

            foreach (Type modelType in modelTypes)
            {
                FieldInfo[] fields = modelType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in fields)
                    CollectionAssert.DoesNotContain(forbidden, field.FieldType.FullName, $"{modelType.Name}.{field.Name}");
            }
        }

        [Test]
        public void TargetSnapshotDoesNotStoreHistoryOrPredictionState()
        {
            FieldInfo[] fields = typeof(MotionWarpTargetSnapshot).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                Assert.That(field.Name, Does.Not.Contain("history").IgnoreCase);
                Assert.That(field.Name, Does.Not.Contain("prediction").IgnoreCase);
                Assert.That(field.FieldType.FullName, Is.Not.EqualTo("UnityEngine.Transform"));
            }
        }

        [Test]
        public void MissingRequiredTargetReturnsInvalidResult()
        {
            MotionWarpInput input = new MotionWarpInput(
                MotionWarpPolicy.AttackMagnetAndFacingCorrection("attack-magnet", 2f, 0.5f, 30f),
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 7),
                MotionWarpTargetSnapshot.Invalid(new MotionWarpTargetBindingId("lock-on"), 7),
                true,
                7);

            MotionWarpResult result = MotionWarpSolver.Resolve(in input);

            Assert.False(result.IsValid);
            Assert.False(result.HasContribution);
            Assert.AreEqual(MotionWarpFailureReason.TargetMissing, result.FailureReason);
            Assert.AreEqual(Vector3.zero, result.PlanarDelta);
            Assert.AreEqual(0f, result.YawDelta);
        }

        [Test]
        public void InactiveMotionWindowOutputsNoDeltaOrYaw()
        {
            MotionWarpInput input = new MotionWarpInput(
                MotionWarpPolicy.AttackMagnetAndFacingCorrection("attack-magnet", 2f, 0f, 30f),
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 8),
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.right * 4f, Vector3.forward, "test", 8),
                false,
                8);

            MotionWarpResult result = MotionWarpSolver.Resolve(in input);

            Assert.True(result.IsValid);
            Assert.False(result.HasContribution);
            Assert.AreEqual(MotionWarpFailureReason.MotionWindowInactive, result.FailureReason);
            Assert.AreEqual(Vector3.zero, result.PlanarDelta);
            Assert.AreEqual(0f, result.YawDelta);
        }

        [Test]
        public void AttackMagnetOutputsClampedPlanarDelta()
        {
            MotionWarpInput input = new MotionWarpInput(
                MotionWarpPolicy.AttackMagnetAndFacingCorrection("attack-magnet", 2f, 1f, 90f),
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 9),
                MotionWarpTargetSnapshot.Pose("lock-on", new Vector3(3f, 0f, 4f), Vector3.forward, "test", 9),
                true,
                9);

            MotionWarpResult result = MotionWarpSolver.Resolve(in input);

            Assert.True(result.IsValid);
            Assert.True(result.HasContribution);
            Assert.AreEqual(2f, result.PlanarDelta.magnitude, 0.0001f);
            AssertVector3(new Vector3(0.6f, 0f, 0.8f), result.PlanarDelta.normalized);
        }

        [Test]
        public void FacingCorrectionOutputsClampedYawDelta()
        {
            MotionWarpPolicy policy = new MotionWarpPolicy(
                new MotionWarpPolicyId("face-target"),
                false,
                true,
                true,
                false,
                string.Empty,
                MotionWarpAxisMask.Planar,
                MotionWarpRotationPolicy.FaceTargetPosition,
                0f,
                0f,
                30f,
                1f,
                1f);
            MotionWarpInput input = new MotionWarpInput(
                policy,
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 10),
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.right * 5f, Vector3.forward, "test", 10),
                true,
                10);

            MotionWarpResult result = MotionWarpSolver.Resolve(in input);

            Assert.True(result.IsValid);
            Assert.True(result.HasContribution);
            Assert.AreEqual(Vector3.zero, result.PlanarDelta);
            Assert.AreEqual(30f, result.YawDelta, 0.0001f);
        }

        [Test]
        public void MovingTargetOnlyChangesResultThroughCurrentTickSnapshot()
        {
            MotionWarpPolicy policy = MotionWarpPolicy.AttackMagnetAndFacingCorrection("attack-magnet", 5f, 0f, 90f);
            MotionWarpRootSnapshot root = MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 11);
            MotionWarpInput first = new MotionWarpInput(
                policy,
                root,
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.forward * 2f, Vector3.forward, "test", 11),
                true,
                11);
            MotionWarpInput replay = new MotionWarpInput(
                policy,
                root,
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.forward * 2f, Vector3.forward, "test", 11),
                true,
                11);
            MotionWarpInput moved = new MotionWarpInput(
                policy,
                root,
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.right * 2f, Vector3.forward, "test", 12),
                true,
                12);

            MotionWarpResult firstResult = MotionWarpSolver.Resolve(in first);
            MotionWarpResult replayResult = MotionWarpSolver.Resolve(in replay);
            MotionWarpResult movedResult = MotionWarpSolver.Resolve(in moved);

            Assert.AreEqual(firstResult.PlanarDelta, replayResult.PlanarDelta);
            Assert.AreEqual(firstResult.YawDelta, replayResult.YawDelta);
            Assert.AreNotEqual(firstResult.PlanarDelta, movedResult.PlanarDelta);
            Assert.AreEqual(0, typeof(MotionWarpSolver).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length);
        }

        static void AssertVector3(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }

    public sealed class ActionTimelineMotionWarpingTests
    {
        [Test]
        public void MotionClipCarriesWarpPayloadAsIntentOnly()
        {
            MotionWarpPayload payload = MotionWarpPayload.AttackMagnetAndFacingCorrection(
                "attack-magnet",
                "lock-on",
                2f,
                0.5f,
                30f);
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                new ActionStateId("Action.Attack01"),
                12,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                2,
                                8,
                                ActionTimelineClipPayload.Motion(CreateMotionSpec(payload), payload))
                        })
                });

            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 4, 22));

            Assert.True(outcome.HasMotion);
            Assert.True(outcome.MotionSpec.MotionWarpPayload.HasWarp);
            Assert.AreEqual("attack-magnet", outcome.MotionSpec.MotionWarpPayload.Policy.PolicyId.Value);
            Assert.AreEqual("lock-on", outcome.MotionSpec.MotionWarpPayload.TargetBindingId.Value);
        }

        [Test]
        public void ValidatorRejectsRequiredWarpTargetBinding()
        {
            MotionWarpPayload payload = new MotionWarpPayload(
                new MotionWarpPolicy(
                    new MotionWarpPolicyId("attack-magnet"),
                    true,
                    false,
                    true,
                    false,
                    string.Empty,
                    MotionWarpAxisMask.Planar,
                    MotionWarpRotationPolicy.FaceTargetPosition,
                    2f,
                    0f,
                    0f,
                    1f,
                    1f),
                MotionWarpTargetBindingId.None);
            ActionTimelineDefinition timeline = CreateTimeline(payload);

            ActionTimelineValidationResult result = ActionTimelineValidator.Validate(timeline);

            Assert.True(result.HasErrors);
            CollectionAssert.Contains(result.Errors, "clip-motion-warp-target-binding-missing:0:0");
        }

        [Test]
        public void ValidatorRejectsRequiredWarpProfile()
        {
            MotionWarpPayload payload = new MotionWarpPayload(
                new MotionWarpPolicy(
                    new MotionWarpPolicyId("profile-warp"),
                    true,
                    false,
                    false,
                    true,
                    string.Empty,
                    MotionWarpAxisMask.Planar,
                    MotionWarpRotationPolicy.FaceTargetPosition,
                    2f,
                    0f,
                    0f,
                    1f,
                    1f),
                MotionWarpTargetBindingId.None);
            ActionTimelineDefinition timeline = CreateTimeline(payload);

            ActionTimelineValidationResult result = ActionTimelineValidator.Validate(timeline);

            Assert.True(result.HasErrors);
            CollectionAssert.Contains(result.Errors, "clip-motion-warp-profile-missing:0:0");
        }

        [Test]
        public void EvaluatorDoesNotReferenceWarpSolverOrSceneObjects()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts/Character/Action/Timeline/Solver/ActionTimelineEvaluator.cs"),
                Encoding.UTF8);

            Assert.That(source, Does.Not.Contain("MotionWarpSolver"));
            Assert.That(source, Does.Not.Contain("Transform"));
            Assert.That(source, Does.Not.Contain("CharacterController"));
            Assert.That(source, Does.Not.Contain("Animancer"));
            Assert.That(source, Does.Not.Contain("ExecuteActionMovement"));
        }

        static ActionTimelineDefinition CreateTimeline(MotionWarpPayload payload)
        {
            return new ActionTimelineDefinition(
                new ActionStateId("Action.Attack01"),
                10,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                0,
                                6,
                                ActionTimelineClipPayload.Motion(CreateMotionSpec(payload), payload))
                        })
                });
        }

        static ActionMotionSpec CreateMotionSpec(MotionWarpPayload payload)
        {
            return new ActionMotionSpec(
                new ActionStateId("Action.Attack01"),
                new CharacterStateId("Action.Attack01"),
                CharacterStateVariant.None,
                0.4f,
                3f,
                true,
                false,
                Vector3.forward,
                0.1f,
                0,
                payload);
        }
    }

    public sealed class ActionMotionWarpResolveTests
    {
        [Test]
        public void NoWarpPayloadKeepsExistingDistanceOverDurationBehavior()
        {
            ActionMotionSpec spec = CreateMotionSpec(MotionWarpPayload.None, Vector3.forward, 0.1f);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                default,
                CharacterRuntimeActionFacts.Default,
                false);

            ActionMotionResolveResult result = ActionMotionResolver.Resolve(in input);

            Assert.True(result.HasActionMovement);
            Assert.AreEqual(3f * 0.1f / 0.4f, result.MovementCommand.PlanarDistance, 0.0001f);
            Assert.AreEqual(Vector3.forward, result.MovementCommand.WorldDirection);
            Assert.False(result.MovementCommand.HasWarpYaw);
        }

        [Test]
        public void MissingWarpTargetDoesNotFallbackToLockedDirection()
        {
            MotionWarpPayload payload = MotionWarpPayload.AttackMagnetAndFacingCorrection(
                "attack-magnet",
                "lock-on",
                2f,
                0f,
                45f);
            ActionMotionSpec spec = CreateMotionSpec(payload, Vector3.forward, 0.1f);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                default,
                CharacterRuntimeActionFacts.Default,
                false,
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 33),
                MotionWarpTargetSnapshot.Invalid(new MotionWarpTargetBindingId("lock-on"), 33));

            ActionMotionResolveResult result = ActionMotionResolver.Resolve(in input);

            Assert.False(result.HasActionMovement);
            Assert.False(result.MovementCommand.HasMotion);
            Assert.False(result.MotionWarpResult.IsValid);
            Assert.AreEqual(MotionWarpFailureReason.TargetMissing, result.MotionWarpResult.FailureReason);
            Assert.That(result.DiagnosticSummary, Does.Contain("warpInvalid=TargetMissing"));
        }

        [Test]
        public void AttackMagnetAndFacingCorrectionAdaptToActionCommand()
        {
            MotionWarpPayload payload = MotionWarpPayload.AttackMagnetAndFacingCorrection(
                "attack-magnet",
                "lock-on",
                2f,
                1f,
                45f);
            ActionMotionSpec spec = CreateMotionSpec(payload, Vector3.forward, 0.1f);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                default,
                CharacterRuntimeActionFacts.Default,
                false,
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 34),
                MotionWarpTargetSnapshot.Pose("lock-on", new Vector3(3f, 0f, 4f), Vector3.forward, "test", 34));

            ActionMotionResolveResult result = ActionMotionResolver.Resolve(in input);

            Assert.True(result.HasActionMovement);
            Assert.True(result.MotionWarpResult.IsValid);
            Assert.True(result.MotionWarpResult.HasContribution);
            Assert.AreEqual(2f, result.MovementCommand.PlanarDistance, 0.0001f);
            AssertVector3(new Vector3(0.6f, 0f, 0.8f), result.MovementCommand.WorldDirection);
            Assert.True(result.MovementCommand.HasWarpYaw);
            Assert.AreEqual(36.8699f, result.MovementCommand.YawDelta, 0.001f);
        }

        [Test]
        public void FacingOnlyWarpProducesYawOnlyActionCommand()
        {
            MotionWarpPayload payload = new MotionWarpPayload(
                new MotionWarpPolicy(
                    new MotionWarpPolicyId("face-target"),
                    false,
                    true,
                    true,
                    false,
                    string.Empty,
                    MotionWarpAxisMask.Planar,
                    MotionWarpRotationPolicy.FaceTargetPosition,
                    0f,
                    0f,
                    30f,
                    1f,
                    1f),
                new MotionWarpTargetBindingId("lock-on"));
            ActionMotionSpec spec = CreateMotionSpec(payload, Vector3.zero, 0.1f);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                default,
                CharacterRuntimeActionFacts.Default,
                false,
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 35),
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.right * 4f, Vector3.forward, "test", 35));

            ActionMotionResolveResult result = ActionMotionResolver.Resolve(in input);

            Assert.True(result.HasActionMovement);
            Assert.False(result.MovementCommand.HasMovement);
            Assert.True(result.MovementCommand.HasWarpYaw);
            Assert.AreEqual(0f, result.MovementCommand.PlanarDistance);
            Assert.AreEqual(30f, result.MovementCommand.YawDelta, 0.0001f);
        }

        [Test]
        public void MotionWindowInactiveDoesNotReuseBaseDistance()
        {
            MotionWarpPayload payload = MotionWarpPayload.AttackMagnetAndFacingCorrection(
                "attack-magnet",
                "lock-on",
                2f,
                0f,
                45f);
            ActionMotionSpec spec = CreateMotionSpec(payload, Vector3.forward, 0.1f);
            StateTimelineWindowFacts timelineFacts = new StateTimelineWindowFacts(
                CharacterStateIds.Dodge,
                0.2f,
                true,
                0.1f,
                false,
                false,
                false,
                false,
                0,
                0,
                0,
                false,
                string.Empty);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                timelineFacts,
                CharacterRuntimeActionFacts.Default,
                false,
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 36),
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.forward * 4f, Vector3.forward, "test", 36));

            ActionMotionResolveResult result = ActionMotionResolver.Resolve(in input);

            Assert.False(result.HasActionMovement);
            Assert.False(result.MovementCommand.HasMotion);
            Assert.AreEqual(MotionWarpFailureReason.MotionWindowInactive, result.MotionWarpResult.FailureReason);
            Assert.AreEqual(0f, result.MovementCommand.PlanarDistance);
        }

        [Test]
        public void WarpResultIsCarriedByFrameSubmissionBeforeOutputApply()
        {
            MotionWarpPayload payload = MotionWarpPayload.AttackMagnetAndFacingCorrection(
                "attack-magnet",
                "lock-on",
                2f,
                0f,
                45f);
            ActionMotionSpec spec = CreateMotionSpec(payload, Vector3.forward, 0.1f);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                default,
                CharacterRuntimeActionFacts.Default,
                false,
                MotionWarpRootSnapshot.Pose(Vector3.zero, Vector3.forward, 37),
                MotionWarpTargetSnapshot.Pose("lock-on", Vector3.forward * 3f, Vector3.forward, "test", 37));
            ActionMotionResolveResult actionResult = ActionMotionResolver.Resolve(in input);

            CharacterFrameSubmission submission = new CharacterFrameSubmission(
                CharacterFrameSubmissionSource.CharacterRuntimeGraph,
                37,
                default,
                default,
                default,
                default,
                actionResult,
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest),
                StateTimelineFactsTrace.None,
                CharacterStateMachineSnapshot.Inactive,
                false);

            Assert.True(submission.ActionMotionResult.MotionWarpResult.IsValid);
            Assert.True(submission.ActionMotionResult.MotionWarpResult.HasContribution);
            Assert.AreEqual("attack-magnet", submission.ActionMotionResult.MotionWarpResult.PolicyId.Value);
        }

        [Test]
        public void MotionWarpingRuntimeDoesNotAddExecutorOrTransformPaths()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Character");
            string motionWarpingSource = ReadAllSource(Path.Combine(root, "MotionWarping"));
            string locomotionAdapterSource = File.ReadAllText(
                Path.Combine(root, "Movement/Solver/MotionWarping/LocomotionMotionWarpAdapter.cs"),
                Encoding.UTF8);
            string actionTimelineEvaluator = File.ReadAllText(
                Path.Combine(root, "Action/Timeline/Solver/ActionTimelineEvaluator.cs"),
                Encoding.UTF8);

            Assert.That(motionWarpingSource, Does.Not.Contain("CharacterController"));
            Assert.That(motionWarpingSource, Does.Not.Contain("Transform"));
            Assert.That(motionWarpingSource, Does.Not.Contain("Animancer"));
            Assert.That(motionWarpingSource, Does.Not.Contain("ExecuteActionMovement"));
            Assert.That(locomotionAdapterSource, Does.Not.Contain("ThirdPersonAction"));
            Assert.That(locomotionAdapterSource, Does.Not.Contain("ActionLifecycle"));
            Assert.That(actionTimelineEvaluator, Does.Not.Contain("MotionWarpSolver"));
        }

        [Test]
        public void LocomotionAdapterUsesSharedResultWithoutMergingCommandContracts()
        {
            MotionWarpResult result = new MotionWarpResult(
                true,
                true,
                new Vector3(0.25f, 0f, 0.5f),
                12f,
                MotionWarpFailureReason.None,
                new MotionWarpPolicyId("turn-adjust"),
                new MotionWarpTargetBindingId("target"),
                44);

            BasicMovementMotionFacts facts = LocomotionMotionWarpAdapter.ToMotionFacts(
                BasicMovementPhase.MoveLoop,
                "Locomotion.MoveLoop",
                in result);

            Assert.True(facts.HasAnimationMotion);
            Assert.AreEqual(BasicMovementPlanarDeltaSpace.World, facts.PlanarDeltaSpace);
            AssertVector3(new Vector3(0.25f, 0f, 0.5f), facts.LocalPlanarDelta);
            Assert.AreEqual(12f, facts.YawDelta, 0.0001f);
            Assert.AreNotEqual(typeof(MovementCommand), typeof(ActionMovementCommand));
        }

        [Test]
        public void FramePlanStillSuppressesLocomotionWhenActionWarpCandidateWins()
        {
            CharacterFrameArbitrationInput input = new CharacterFrameArbitrationInput(
                BodyOccupancyClaim.CommittedActionFullBody(45),
                CharacterFrameCandidateOutput.Locomotion(true, true, 45),
                CharacterFrameCandidateOutput.CommittedAction(true, true, 45),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, 45),
                45);

            CharacterFramePlan plan = new CharacterFramePlan(DefaultBodyArbiter.Instance.Decide(in input));

            Assert.True(plan.OccupancyDecision.FullBodyClaimAccepted);
            Assert.True(plan.OccupancyDecision.SuppressLocomotionMotion);
            Assert.AreEqual(CharacterBodyDomain.CommittedAction, plan.BaseSlotOwner);
        }

        static ActionMotionSpec CreateMotionSpec(MotionWarpPayload payload, Vector3 lockedDirection, float stateTime)
        {
            return new ActionMotionSpec(
                new ActionStateId("Action.Attack01"),
                new CharacterStateId("Action.Attack01"),
                CharacterStateVariant.None,
                0.4f,
                3f,
                true,
                false,
                lockedDirection,
                stateTime,
                30,
                payload);
        }

        static void AssertVector3(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }

        static string ReadAllSource(string folder)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
                builder.AppendLine(File.ReadAllText(file, Encoding.UTF8));
            return builder.ToString();
        }
    }
}
