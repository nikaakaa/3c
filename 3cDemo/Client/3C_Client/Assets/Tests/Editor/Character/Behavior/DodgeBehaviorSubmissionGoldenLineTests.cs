using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace Tests.Editor.Character.Behavior
{
    public sealed class DodgeBehaviorSubmissionGoldenLineTests
    {
        const float TickInterval = 0.02f;
        const float Tolerance = 0.0001f;

        [Test]
        public void DirectionalDodgeBaselineMapsToBehaviorSubmission()
        {
            DodgeGoldenBaseline baseline = DodgeGoldenBaseline.CaptureAccepted(
                CharacterStateVariant.Directional,
                Vector2.up,
                Vector3.forward,
                true,
                10);
            CharacterBehaviorSubmissionSet submissions = DodgeGoldenLineMapper.Map(in baseline);

            DodgeGoldenLineComparison.AssertEquivalent(in baseline, submissions);

            BehaviorOutputSubmission output = submissions.Outputs[0];
            Assert.False(output.Required);
            Assert.True(output.ActionMotionResult.SetRunLatch);
            Assert.True(output.ActionOutput.HasCommittedActionBranchOutcome);
            CollectionAssert.Contains(output.ActionOutput.ActionTimelineOutcome.ActiveWindowFactIds, "window.action.dodge");
            Assert.AreEqual(1, submissions.MotionChannels.Count);
            Assert.AreEqual(1, submissions.AnimationChannels.Count);
            Assert.AreEqual(1, submissions.WindowFactsChannels.Count);
            Assert.AreEqual(1, submissions.Claims.Count);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, submissions.AnimationChannels[0].AnimationKey);
            Assert.True(submissions.Claims[0].Claim.ClaimsFullBody);
            Assert.AreEqual("cue.dodge.flash", submissions.Cues[0].CueId);
        }

        [Test]
        public void BackstepDodgeBaselineMapsToBehaviorSubmission()
        {
            DodgeGoldenBaseline baseline = DodgeGoldenBaseline.CaptureAccepted(
                CharacterStateVariant.Backstep,
                Vector2.zero,
                Vector3.forward,
                false,
                20);
            CharacterBehaviorSubmissionSet submissions = DodgeGoldenLineMapper.Map(in baseline);

            DodgeGoldenLineComparison.AssertEquivalent(in baseline, submissions);

            BehaviorOutputSubmission output = submissions.Outputs[0];
            Assert.False(output.ActionMotionResult.SetRunLatch);
            Assert.AreEqual(CharacterStateVariant.Backstep, output.ActionMotionResult.Spec.Variant);
            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, output.ActionOutput.AnimationRequest.Key);
            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, submissions.AnimationChannels[0].AnimationKey);
        }

        [Test]
        public void RejectedDodgeRequestDoesNotProduceOutputOrInputConsume()
        {
            DodgeGoldenBaseline baseline = DodgeGoldenBaseline.CaptureRejected(30);
            CharacterBehaviorSubmissionSet submissions = DodgeGoldenLineMapper.Map(in baseline);

            Assert.True(baseline.RequestResult.RequestSubmissions.HasAny);
            Assert.False(baseline.RequestResult.Accepted);
            Assert.AreEqual(1, submissions.Requests.Count);
            Assert.AreEqual(0, submissions.Outputs.Count);
            Assert.AreEqual(1, submissions.Diagnostics.Count);
            Assert.False(submissions.Requests[0].Decision.Accepted);
        }

        [Test]
        public void CompletedDodgeCanRetryWithNewPlaybackIntent()
        {
            DodgeGoldenBaseline first = DodgeGoldenBaseline.CaptureAccepted(
                CharacterStateVariant.Directional,
                Vector2.up,
                Vector3.forward,
                true,
                40);
            CommittedActionRuntimeModule module = first.Module;
            module.CompleteActionLifecycle(first.CompletedMotionResult, false);

            DodgeGoldenBaseline retry = DodgeGoldenBaseline.CaptureAccepted(
                CharacterStateVariant.Directional,
                Vector2.up,
                Vector3.forward,
                true,
                41,
                module);
            CharacterBehaviorSubmissionSet submissions = DodgeGoldenLineMapper.Map(in retry);

            Assert.True(retry.RequestResult.Accepted);
            Assert.True(submissions.Requests[0].Decision.Accepted);
            Assert.AreNotEqual(
                first.Lifecycle.AnimationRequest.ActionPlaybackIntent,
                retry.Lifecycle.AnimationRequest.ActionPlaybackIntent);
            Assert.True(retry.Lifecycle.AnimationRequest.ActionPlaybackIntent.Value > first.Lifecycle.AnimationRequest.ActionPlaybackIntent.Value);
        }

        [Test]
        public void BackstepCompletionRequiresAnimationEndBeforeLifecycleExit()
        {
            DodgeGoldenBaseline baseline = DodgeGoldenBaseline.CaptureAccepted(
                CharacterStateVariant.Backstep,
                Vector2.zero,
                Vector3.forward,
                false,
                50);

            ActionLifecycleFrame lifecycle = baseline.Lifecycle;
            ActionMotionResolveResult completed = baseline.CompletedMotionResult;

            Assert.True(DodgeGoldenBaseline.RequiresAnimationEnd(
                in lifecycle,
                in completed,
                false));
        }

        [Test]
        public void RestoreResumesSameFrameTimingAndSubmission()
        {
            DodgeGoldenBaseline beforeRestore = DodgeGoldenBaseline.CaptureAccepted(
                CharacterStateVariant.Directional,
                Vector2.up,
                Vector3.forward,
                true,
                60);
            CommittedActionRestoreState restoreState = beforeRestore.Module.CaptureRestoreState();

            DodgeGoldenBaseline baselineContinuation = DodgeGoldenBaseline.ContinueFrom(
                beforeRestore.Module,
                beforeRestore.Catalog,
                Vector2.up,
                Vector3.forward,
                true,
                61);
            CommittedActionRuntimeModule restoredModule = new CommittedActionRuntimeModule();
            RestoreActionLifecycleOnly(restoredModule, in restoreState);
            DodgeGoldenBaseline restoredContinuation = DodgeGoldenBaseline.ContinueFrom(
                restoredModule,
                beforeRestore.Catalog,
                Vector2.up,
                Vector3.forward,
                true,
                61);

            CharacterBehaviorSubmissionSet mapped = DodgeGoldenLineMapper.Map(in restoredContinuation);

            DodgeGoldenLineComparison.AssertEquivalent(in baselineContinuation, mapped);
            Assert.AreEqual(
                baselineContinuation.Lifecycle.CommittedActionBranchOutcome.TimelineOutcome.LocalTick,
                restoredContinuation.Lifecycle.CommittedActionBranchOutcome.TimelineOutcome.LocalTick);
        }

        [Test]
        public void MissingMotionFieldReportsFieldLevelDiagnostic()
        {
            DodgeGoldenBaseline baseline = DodgeGoldenBaseline.CaptureAccepted(
                CharacterStateVariant.Directional,
                Vector2.up,
                Vector3.forward,
                true,
                70);
            CharacterBehaviorSubmissionSet submissions = DodgeGoldenLineMapper.Map(in baseline);
            BehaviorOutputSubmission output = submissions.Outputs[0];
            ActionMotionSpec spec = output.ActionMotionResult.Spec;
            ActionMotionSpec missingDistance = new ActionMotionSpec(
                spec.ActionState,
                spec.SourceState,
                spec.Variant,
                spec.Duration,
                0f,
                spec.RotateToDirection,
                spec.SetRunLatchOnComplete,
                spec.LockedWorldDirection,
                spec.StateTime,
                spec.SourceStep);
            ActionMotionResolveResult brokenMotion = new ActionMotionResolveResult(
                missingDistance,
                output.ActionMotionResult.MovementCommand,
                output.ActionMotionResult.HasActionMovement,
                output.ActionMotionResult.ActionCompleted,
                output.ActionMotionResult.SetRunLatch,
                output.ActionMotionResult.SourceStep,
                output.ActionMotionResult.DiagnosticSummary);
            CharacterBehaviorSubmissionSet broken = new CharacterBehaviorSubmissionSet();
            broken.Add(submissions.Requests[0]);
            broken.Add(new BehaviorOutputSubmission(
                output.Source,
                output.LocomotionDecision,
                output.StateDecision,
                output.LocomotionFrame,
                output.StateFrame,
                brokenMotion,
                output.ActionOutput,
                output.ArbitrationInput,
                output.LocomotionPreemption,
                output.Required));

            AssertionException exception = Assert.Throws<AssertionException>(
                () => DodgeGoldenLineComparison.AssertEquivalent(in baseline, broken));

            Assert.That(exception.Message, Does.Contain("motion.distance"));
        }

        [Test]
        public void GoldenLineMapperDoesNotRegisterProductionRuntimeOrSideEffects()
        {
            Type mapperType = typeof(DodgeGoldenLineMapper);
            AssertNoMemberTypeContains(mapperType, "Executor");
            AssertNoMemberTypeContains(mapperType, "Presenter");
            string source = File.ReadAllText(
                "Assets/Tests/Editor/Character/Behavior/DodgeBehaviorSubmissionGoldenLineTests.cs",
                Encoding.UTF8);

            Assert.That(source, Does.Not.Contain("CharacterRuntimeCore" + ".CreateFrameRuntimeHost"));
            Assert.That(source, Does.Not.Contain("CharacterFrame" + "RuntimeHost("));
            Assert.That(source, Does.Not.Contain("FullBody" + "OutputRuntime"));
            Assert.That(source, Does.Not.Contain("Runtime" + "Blackboard"));
            Assert.That(source, Does.Not.Contain("Motion" + "OutputApplier"));
        }

        static void AssertNoMemberTypeContains(Type type, string token)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
                Assert.That(fields[i].FieldType.Name, Does.Not.Contain(token), fields[i].Name);

            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                Assert.That(methods[i].ReturnType.Name, Does.Not.Contain(token), methods[i].Name);
                ParameterInfo[] parameters = methods[i].GetParameters();
                for (int j = 0; j < parameters.Length; j++)
                    Assert.That(parameters[j].ParameterType.Name, Does.Not.Contain(token), methods[i].Name);
            }
        }

        static void RestoreActionLifecycleOnly(CommittedActionRuntimeModule module, in CommittedActionRestoreState restoreState)
        {
            FieldInfo field = typeof(CommittedActionRuntimeModule).GetField(
                "actionLifecycleRuntime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            object lifecycleRuntime = field.GetValue(module);
            Assert.NotNull(lifecycleRuntime);
            MethodInfo method = lifecycleRuntime.GetType().GetMethod(
                "Restore",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method);
            object[] args = { restoreState.Gameplay.ActionLifecycle };
            method.Invoke(lifecycleRuntime, args);
        }

        readonly struct DodgeGoldenBaseline
        {
            DodgeGoldenBaseline(
                CommittedActionRuntimeModule module,
                CharacterActionCatalog catalog,
                CharacterActionRequestSubmissionResult requestResult,
                ActionLifecycleFrame lifecycle,
                ActionMotionResolveResult motionResult,
                ActionMotionResolveResult completedMotionResult,
                CharacterFrameActionOutputSubmission actionOutput,
                CharacterFrameArbitrationInput arbitrationInput,
                string diagnostic)
            {
                Module = module;
                Catalog = catalog;
                RequestResult = requestResult;
                Lifecycle = lifecycle;
                MotionResult = motionResult;
                CompletedMotionResult = completedMotionResult;
                ActionOutput = actionOutput;
                ArbitrationInput = arbitrationInput;
                Diagnostic = diagnostic ?? string.Empty;
            }

            public CommittedActionRuntimeModule Module { get; }
            public CharacterActionCatalog Catalog { get; }
            public CharacterActionRequestSubmissionResult RequestResult { get; }
            public ActionLifecycleFrame Lifecycle { get; }
            public ActionMotionResolveResult MotionResult { get; }
            public ActionMotionResolveResult CompletedMotionResult { get; }
            public CharacterFrameActionOutputSubmission ActionOutput { get; }
            public CharacterFrameArbitrationInput ArbitrationInput { get; }
            public string Diagnostic { get; }
            public bool HasOutput => Lifecycle.HasAction || ActionOutput.HasAnimationRequest || ActionOutput.HasCommittedActionBranchOutcome;

            public static DodgeGoldenBaseline CaptureAccepted(
                CharacterStateVariant variant,
                Vector2 move,
                Vector3 worldDirection,
                bool hasMoveIntentAtCompletion,
                int step,
                CommittedActionRuntimeModule module = null)
            {
                CommittedActionRuntimeModule runtime = module ?? new CommittedActionRuntimeModule();
                CharacterActionCatalog catalog = CreateCatalog();
                CharacterActionRequestSubmissionResult requestResult = ResolveRequest(
                    variant,
                    move,
                    worldDirection,
                    step,
                    catalog,
                    CreateAllowDodgePolicies());
                Assert.True(requestResult.Accepted);

                return CaptureFromResolved(
                    runtime,
                    catalog,
                    in requestResult,
                    move,
                    worldDirection,
                    hasMoveIntentAtCompletion,
                    step);
            }

            public static DodgeGoldenBaseline CaptureRejected(int step)
            {
                CharacterActionCatalog catalog = CreateCatalog();
                CharacterActionRequestSubmissionResult requestResult = ResolveRequest(
                    CharacterStateVariant.Directional,
                    Vector2.up,
                    Vector3.forward,
                    step,
                    catalog,
                    CreateRejectDodgePolicies());
                Assert.False(requestResult.Accepted);

                return new DodgeGoldenBaseline(
                    new CommittedActionRuntimeModule(),
                    catalog,
                    requestResult,
                    ActionLifecycleFrame.None(step),
                    ActionMotionResolveResult.None(step),
                    ActionMotionResolveResult.None(step),
                    CharacterFrameActionOutputSubmission.None(step),
                    CharacterFrameArbitrationInput.None(step),
                    $"request-rejected:{requestResult.Decision.RejectReason}");
            }

            public static DodgeGoldenBaseline ContinueFrom(
                CommittedActionRuntimeModule module,
                CharacterActionCatalog catalog,
                Vector2 move,
                Vector3 worldDirection,
                bool hasMoveIntentAtCompletion,
                int step)
            {
                CharacterActionRequestSubmissionResult noRequest = new CharacterActionRequestSubmissionResult(
                    CharacterInputRequestFact.None(InputRequestKind.Dodge),
                    ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest));
                return CaptureFromResolved(
                    module,
                    catalog,
                    in noRequest,
                    move,
                    worldDirection,
                    hasMoveIntentAtCompletion,
                    step);
            }

            public static bool RequiresAnimationEnd(
                in ActionLifecycleFrame frame,
                in ActionMotionResolveResult result,
                bool hasMoveIntentAtCompletion)
            {
                MethodInfo method = typeof(CommittedActionFrameSubmitter).GetMethod(
                    "RequiresActionAnimationEndBeforeLifecycleExit",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(method);
                object[] args = { frame, result, hasMoveIntentAtCompletion };
                return (bool)method.Invoke(null, args);
            }

            static DodgeGoldenBaseline CaptureFromResolved(
                CommittedActionRuntimeModule module,
                CharacterActionCatalog catalog,
                in CharacterActionRequestSubmissionResult requestResult,
                Vector2 move,
                Vector3 worldDirection,
                bool hasMoveIntentAtCompletion,
                int step)
            {
                ActionLifecycleFrame lifecycle = module.TickActionLifecycle(
                    requestResult.ResolvedAction,
                    in catalog,
                    TickInterval,
                    step);
                ActionMotionResolveResult motion = ResolveMotion(
                    lifecycle.MotionSpec,
                    move,
                    hasMoveIntentAtCompletion);
                ActionMotionResolveResult completed = ResolveMotion(
                    CompletedSpec(lifecycle.MotionSpec),
                    move,
                    hasMoveIntentAtCompletion);
                CharacterFrameActionOutputSubmission actionOutput = BuildActionOutput(
                    in lifecycle,
                    requestResult.Request,
                    step);
                BodyOccupancyClaim claim = ResolveClaim(catalog, lifecycle.ActionState, step);
                CharacterFrameArbitrationInput arbitrationInput = new CharacterFrameArbitrationInput(
                    claim,
                    CharacterFrameCandidateOutput.None(CharacterBodyDomain.Locomotion, step),
                    CharacterFrameCandidateOutput.CommittedAction(
                        motion.HasActionMovement,
                        lifecycle.HasAnimationRequest,
                        step),
                    CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, step),
                    step);

                return new DodgeGoldenBaseline(
                    module,
                    catalog,
                    requestResult,
                    lifecycle,
                    motion,
                    completed,
                    actionOutput,
                    arbitrationInput,
                    motion.DiagnosticSummary);
            }

            static CharacterFrameActionOutputSubmission BuildActionOutput(
                in ActionLifecycleFrame lifecycle,
                in CharacterInputRequestFact request,
                int step)
            {
                MethodInfo method = typeof(CommittedActionFrameSubmitter).GetMethod(
                    "BuildActionOutput",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(method);
                object[] args = { lifecycle, request, step };
                return (CharacterFrameActionOutputSubmission)method.Invoke(null, args);
            }

            static ActionMotionResolveResult ResolveMotion(
                in ActionMotionSpec spec,
                Vector2 move,
                bool hasMoveIntentAtCompletion)
            {
                return ActionMotionResolver.Resolve(new ActionMotionResolveInput(
                    spec,
                    TickInterval,
                    StateTimelineWindowFacts.None(default(CharacterStateId)),
                    CharacterRuntimeActionFacts.Default,
                    hasMoveIntentAtCompletion && move.sqrMagnitude > 0.0001f));
            }

            static ActionMotionSpec CompletedSpec(in ActionMotionSpec spec)
            {
                return new ActionMotionSpec(
                    spec.ActionState,
                    spec.SourceState,
                    spec.Variant,
                    spec.Duration,
                    spec.Distance,
                    spec.RotateToDirection,
                    spec.SetRunLatchOnComplete,
                    spec.LockedWorldDirection,
                    spec.Duration,
                    spec.SourceStep);
            }

            static BodyOccupancyClaim ResolveClaim(CharacterActionCatalog catalog, ActionStateId actionState, int step)
            {
                BodyClaimPolicy policy = CreateBodyClaimPolicy();
                Assert.True(policy.TryResolveClaim(actionState, step, out BodyOccupancyClaim claim));
                return claim;
            }
        }

        static class DodgeGoldenLineMapper
        {
            public static CharacterBehaviorSubmissionSet Map(in DodgeGoldenBaseline baseline)
            {
                CharacterActionRequestSubmissionResult requestResult = baseline.RequestResult;
                CharacterBehaviorSubmissionSet set = new CharacterBehaviorSubmissionSet();
                CharacterBehaviorSubmissionSource requestSource = CharacterBehaviorSubmissionSource.Create(
                    "committed-action.dodge.request",
                    CharacterBehaviorSourceKind.CommittedAction,
                    CharacterBehaviorEvaluationPass.RequestPass,
                    ResolveRequestSourceStep(in requestResult),
                    0);
                CharacterFrameRequestSubmission frameRequest = baseline.RequestResult.RequestSubmissions.HasAny
                    ? baseline.RequestResult.RequestSubmissions.First
                    : default;
                bool hasRequestCandidate = baseline.RequestResult.Request.HasRequest ||
                    baseline.RequestResult.RequestSubmissions.HasAny;
                if (hasRequestCandidate)
                {
                    set.Add(new BehaviorRequestSubmission(
                        requestSource,
                        frameRequest,
                        baseline.RequestResult.Decision,
                        baseline.RequestResult.ResolvedAction,
                        baseline.RequestResult.Accepted ? string.Empty : baseline.Diagnostic));
                }

                if (!baseline.HasOutput)
                {
                    if (!string.IsNullOrWhiteSpace(baseline.Diagnostic))
                        set.Add(new BehaviorDiagnosticSubmission(requestSource, "request-rejected", baseline.Diagnostic, false));
                    return set;
                }

                CharacterBehaviorSubmissionSource outputSource = CharacterBehaviorSubmissionSource.Create(
                    "committed-action.dodge.output",
                    CharacterBehaviorSourceKind.CommittedAction,
                    CharacterBehaviorEvaluationPass.OutputPass,
                    baseline.ActionOutput.Step,
                    0);
                set.Add(new BehaviorOutputSubmission(
                    outputSource,
                    default,
                    default,
                    default,
                    default,
                    baseline.CompletedMotionResult,
                    baseline.ActionOutput,
                    baseline.ArbitrationInput,
                    LocomotionPreemptionFact.None,
                    false));
                ActionTimelineOutcome timeline = baseline.ActionOutput.ActionTimelineOutcome;
                set.Add(new BehaviorMotionChannelSubmission(outputSource, timeline.MotionSpec));
                set.Add(new BehaviorAnimationChannelSubmission(outputSource, timeline.AnimationKey));
                set.Add(new BehaviorWindowFactsChannelSubmission(outputSource, ToFactArray(timeline.ActiveWindowFactIds)));
                set.Add(new BehaviorClaimSubmission(outputSource, baseline.ArbitrationInput.OccupancyClaim));
                for (int i = 0; i < timeline.CueRequests.Count; i++)
                    set.Add(new BehaviorCueSubmission(outputSource, timeline.CueRequests[i].CueId, timeline.LocalTick));
                if (!string.IsNullOrWhiteSpace(baseline.Diagnostic))
                    set.Add(new BehaviorDiagnosticSubmission(outputSource, "action-motion", baseline.Diagnostic, false));

                return set;
            }

            static string[] ToFactArray(System.Collections.Generic.IReadOnlyList<string> facts)
            {
                if (facts == null || facts.Count == 0)
                    return Array.Empty<string>();

                string[] result = new string[facts.Count];
                for (int i = 0; i < facts.Count; i++)
                    result[i] = facts[i];
                return result;
            }
        }

        static class DodgeGoldenLineComparison
        {
            public static void AssertEquivalent(in DodgeGoldenBaseline baseline, CharacterBehaviorSubmissionSet submissions)
            {
                Assert.NotNull(submissions);
                bool hasRequestCandidate = baseline.RequestResult.Request.HasRequest ||
                    baseline.RequestResult.RequestSubmissions.HasAny;
                Assert.AreEqual(hasRequestCandidate ? 1 : 0, submissions.Requests.Count, "request.count");
                if (hasRequestCandidate)
                {
                    BehaviorRequestSubmission request = submissions.Requests[0];
                    Assert.AreEqual(baseline.RequestResult.Accepted, request.Decision.Accepted, "request.accepted");
                    Assert.AreEqual(baseline.RequestResult.Request.RequestKind, request.FrameRequest.RequestFact.RequestKind, "request.kind");
                    Assert.AreEqual(baseline.RequestResult.Request.OriginStep, request.FrameRequest.RequestFact.OriginStep, "request.originStep");
                }

                Assert.AreEqual(1, submissions.Outputs.Count, "output.count");
                BehaviorOutputSubmission output = submissions.Outputs[0];
                AssertMotionEqual(baseline.CompletedMotionResult.Spec, output.ActionMotionResult.Spec);
                AssertClaimEqual(baseline.ArbitrationInput.OccupancyClaim, output.ArbitrationInput.OccupancyClaim);
                Assert.AreEqual(1, submissions.MotionChannels.Count, "motion.channel.count");
                Assert.AreEqual(1, submissions.AnimationChannels.Count, "animation.channel.count");
                Assert.AreEqual(1, submissions.WindowFactsChannels.Count, "window.channel.count");
                Assert.AreEqual(1, submissions.Claims.Count, "claim.channel.count");
                AssertMotionEqual(baseline.ActionOutput.ActionTimelineOutcome.MotionSpec, submissions.MotionChannels[0].MotionSpec);
                Assert.AreEqual(baseline.ActionOutput.ActionTimelineOutcome.AnimationKey, submissions.AnimationChannels[0].AnimationKey, "animation.channel.key");
                CollectionAssert.AreEquivalent(
                    baseline.ActionOutput.ActionTimelineOutcome.ActiveWindowFactIds,
                    submissions.WindowFactsChannels[0].FactIds,
                    "window.channel.facts");
                AssertClaimEqual(baseline.ArbitrationInput.OccupancyClaim, submissions.Claims[0].Claim);
                Assert.AreEqual(baseline.ActionOutput.ConsumeInputRequest, output.ActionOutput.ConsumeInputRequest, "input.consume");
                Assert.AreEqual(baseline.ActionOutput.ConsumedRequestKind, output.ActionOutput.ConsumedRequestKind, "input.kind");
                Assert.AreEqual(baseline.ActionOutput.AnimationRequest.Key, output.ActionOutput.AnimationRequest.Key, "animation.key");
                Assert.AreEqual(
                    baseline.ActionOutput.AnimationRequest.ActionPlaybackIntent,
                    output.ActionOutput.AnimationRequest.ActionPlaybackIntent,
                    "animation.playbackIntent");
                Assert.AreEqual(baseline.CompletedMotionResult.SetRunLatch, output.ActionMotionResult.SetRunLatch, "runLatch");
                CollectionAssert.AreEquivalent(
                    baseline.ActionOutput.ActionTimelineOutcome.ActiveWindowFactIds,
                    output.ActionOutput.ActionTimelineOutcome.ActiveWindowFactIds,
                    "window.facts");
            }

            static void AssertMotionEqual(ActionMotionSpec expected, ActionMotionSpec actual)
            {
                Assert.AreEqual(expected.ActionState, actual.ActionState, "motion.actionState");
                Assert.AreEqual(expected.SourceState, actual.SourceState, "motion.sourceState");
                Assert.AreEqual(expected.Variant, actual.Variant, "motion.variant");
                Assert.That(actual.Duration, Is.EqualTo(expected.Duration).Within(Tolerance), "motion.duration");
                Assert.That(actual.Distance, Is.EqualTo(expected.Distance).Within(Tolerance), "motion.distance");
                Assert.AreEqual(expected.RotateToDirection, actual.RotateToDirection, "motion.rotateToDirection");
                Assert.AreEqual(expected.SetRunLatchOnComplete, actual.SetRunLatchOnComplete, "motion.runLatchOnComplete");
                Assert.That(Vector3.Distance(expected.LockedWorldDirection, actual.LockedWorldDirection), Is.LessThan(Tolerance), "motion.lockedWorldDirection");
            }

            static void AssertClaimEqual(BodyOccupancyClaim expected, BodyOccupancyClaim actual)
            {
                Assert.AreEqual(expected.Domain, actual.Domain, "claim.domain");
                Assert.AreEqual(expected.Kind, actual.Kind, "claim.kind");
                Assert.AreEqual(expected.Channels, actual.Channels, "claim.channels");
                Assert.AreEqual(expected.SourceStep, actual.SourceStep, "claim.sourceStep");
            }
        }

        static CharacterActionRequestSubmissionResult ResolveRequest(
            CharacterStateVariant variant,
            Vector2 move,
            Vector3 worldDirection,
            int step,
            CharacterActionCatalog catalog,
            System.Collections.Generic.IReadOnlyList<ActionInterruptPolicy> policies)
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, step, 4);
            LocomotionDecisionFacts facts = CreateLocomotionFacts(move, worldDirection);
            CommittedActionRequestSubmissionResolverInput input = new CommittedActionRequestSubmissionResolverInput(
                buffer,
                step,
                TickInterval,
                CharacterStateMachineSnapshot.Inactive,
                new BasicLocomotionInputSnapshot(TickInterval, move, Vector2.zero, true),
                move.sqrMagnitude > 0.0001f,
                facts,
                StateTimelineWindowFacts.None(default(CharacterStateId)),
                true,
                catalog,
                0,
                policies);

            return CommittedActionRequestSubmissionResolver.Resolve(in input);
        }

        static int ResolveRequestSourceStep(in CharacterActionRequestSubmissionResult result)
        {
            if (result.Request.HasRequest)
                return result.Request.OriginStep;

            return result.RequestSubmissions.HasAny
                ? result.RequestSubmissions.First.RequestFact.OriginStep
                : 0;
        }

        static CharacterActionCatalog CreateCatalog()
        {
            return new CharacterActionCatalog(new[]
            {
                new CharacterActionDefinition(
                    ActionStateIds.Dodge,
                    ActionRequestType.Dodge,
                    InputRequestKind.Dodge,
                    CharacterStateIds.Dodge,
                    30,
                    20,
                    new DodgeActionVariantDefinition(
                        DodgeActionVariant.Directional,
                        0.02f,
                        4f,
                        true,
                        ActionAnimationKeys.DodgeDirectional),
                    new DodgeActionVariantDefinition(
                        DodgeActionVariant.Backstep,
                        0.02f,
                        3f,
                        false,
                        ActionAnimationKeys.DodgeBackstep),
                    CreateCommittedBranch())
            });
        }

        static CommittedActionBranchDefinition CreateCommittedBranch()
        {
            CommittedActionNodeDefinition directionalCondition = CommittedActionNodeDefinition.ConditionNode(
                "condition.dodge.directional",
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Directional),
                new CommittedActionNodeId("timeline.dodge.directional"));
            CommittedActionNodeDefinition backstepCondition = CommittedActionNodeDefinition.ConditionNode(
                "condition.dodge.backstep",
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Backstep),
                new CommittedActionNodeId("timeline.dodge.backstep"));

            return CommittedActionBranchDefinition.Define(
                "committed-action.dodge",
                ActionStateIds.Dodge,
                CommittedActionNodeDefinition.Selector(
                    "selector.dodge",
                    directionalCondition.NodeId,
                    backstepCondition.NodeId),
                BodyOccupancyClaim.CommittedActionFullBody(0),
                new[]
                {
                    directionalCondition,
                    backstepCondition,
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.dodge.directional",
                        CreateDodgeTimeline(CharacterStateVariant.Directional, ActionAnimationKeys.DodgeDirectional, 4f, true)),
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.dodge.backstep",
                        CreateDodgeTimeline(CharacterStateVariant.Backstep, ActionAnimationKeys.DodgeBackstep, 3f, false))
                });
        }

        static ActionTimelineDefinition CreateDodgeTimeline(
            CharacterStateVariant variant,
            ActionAnimationKey animationKey,
            float distance,
            bool setRunLatch)
        {
            return new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                3,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                3,
                                ActionTimelineClipPayload.Animation(animationKey))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                0,
                                3,
                                ActionTimelineClipPayload.Motion(new ActionMotionSpec(
                                    ActionStateIds.Dodge,
                                    CharacterStateIds.Dodge,
                                    variant,
                                    TickInterval,
                                    distance,
                                    variant == CharacterStateVariant.Directional,
                                    setRunLatch,
                                    Vector3.zero,
                                    0f,
                                    0)))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Hitbox,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.HitboxWindow,
                                0,
                                2,
                                ActionTimelineClipPayload.Fact("window.action.dodge"))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Cue,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Cue,
                                0,
                                0,
                                ActionTimelineClipPayload.Cue("cue.dodge.flash"))
                        })
                });
        }

        static BodyClaimPolicy CreateBodyClaimPolicy()
        {
            return new BodyClaimPolicy(new[]
            {
                new BodyClaimPolicyDefinition(
                    ActionStateIds.Dodge.Value,
                    BodyOccupancyKind.FullBody,
                    CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation)
            });
        }

        static ActionInterruptPolicy[] CreateAllowDodgePolicies()
        {
            return new[]
            {
                new ActionInterruptPolicy(
                    ActionStateIds.None,
                    ActionStateIds.Dodge,
                    0,
                    requestType: ActionRequestType.Dodge)
            };
        }

        static ActionInterruptPolicy[] CreateRejectDodgePolicies()
        {
            return new[]
            {
                new ActionInterruptPolicy(
                    ActionStateIds.None,
                    ActionStateIds.Dodge,
                    999,
                    requestType: ActionRequestType.Dodge)
            };
        }

        static LocomotionDecisionFacts CreateLocomotionFacts(Vector2 move, Vector3 worldDirection)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(move, 0.1f, true);
            Vector3 direction = move.sqrMagnitude > 0.0001f ? worldDirection.normalized : Vector3.zero;
            return new LocomotionDecisionFacts(
                intent,
                intent.HasMoveIntent ? intent.Gait : BasicMovementGait.Walk,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(direction, Vector3.forward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);
        }
    }
}
