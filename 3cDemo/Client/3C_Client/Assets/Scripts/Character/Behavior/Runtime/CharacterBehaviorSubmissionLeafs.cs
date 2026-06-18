using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonCharacterBehavior
{
    public sealed class LocomotionBehaviorSubmissionLeaf : ICharacterBehaviorSubmissionLeaf
    {
        // 临时 delegate 边界：typed submission 覆盖完整 request/output 后删除旧 submitter。
        readonly LocomotionFrameSubmitter submitter = new LocomotionFrameSubmitter();

        public CharacterBehaviorSourceKind SourceKind => CharacterBehaviorSourceKind.Locomotion;

        public bool TryRunRequestPass(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace)
        {
            trace.Add(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSourceKind.Locomotion);
            bool success = submitter.TrySubmitFrameRequests(runtime, ref context);
            if (success)
            {
                submissions.Add(new BehaviorDiagnosticSubmission(
                    Source(CharacterBehaviorEvaluationPass.RequestPass, context.Step),
                    "locomotion-request-context",
                    $"locomotion request context ready step={context.Step}",
                    false));
            }

            return success;
        }

        public bool TryRunOutputPass(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace)
        {
            trace.Add(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSourceKind.Locomotion);
            bool success = submitter.TrySubmitFrameOutput(runtime, ref context, out _);
            if (context.CurrentStep == CharacterFramePipelineStep.Failed)
                return false;

            submissions.Add(new BehaviorOutputSubmission(
                Source(CharacterBehaviorEvaluationPass.OutputPass, context.Step),
                context.LocomotionDecision,
                context.StateDecision,
                context.LocomotionFrame,
                context.StateFrame,
                ActionMotionResolveResult.None(context.Step),
                CharacterFrameActionOutputSubmission.None(context.Step),
                CharacterFrameArbitrationInput.None(context.Step),
                LocomotionPreemptionFact.None,
                true));
            return success || context.HasLocomotionDecision;
        }

        static CharacterBehaviorSubmissionSource Source(CharacterBehaviorEvaluationPass pass, int step)
        {
            return CharacterBehaviorSubmissionSource.Create(
                "behavior.locomotion",
                CharacterBehaviorSourceKind.Locomotion,
                pass,
                step,
                0);
        }
    }

    public sealed class CommittedActionBehaviorSubmissionLeaf : ICharacterBehaviorSubmissionLeaf
    {
        // 临时 delegate 边界：typed submission 覆盖完整 request/output 后删除旧 submitter。
        readonly CommittedActionFrameSubmitter submitter = new CommittedActionFrameSubmitter();

        public CharacterBehaviorSourceKind SourceKind => CharacterBehaviorSourceKind.CommittedAction;

        public bool TryRunRequestPass(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace)
        {
            trace.Add(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSourceKind.CommittedAction);
            if (!context.HasLocomotionDecision)
            {
                context.MarkFailed("behavior-action-request-context-missing");
                return false;
            }

            bool success = submitter.TrySubmitFrameRequests(runtime, ref context);
            CharacterFrameRequestSubmission request = context.RequestSubmissions.HasAny
                ? context.RequestSubmissions.First
                : default;
            submissions.Add(new BehaviorRequestSubmission(
                Source(CharacterBehaviorEvaluationPass.RequestPass, context.Step),
                request,
                context.ActionDecision,
                context.ResolvedAction,
                context.ActionDecision.Accepted ? string.Empty : context.ActionDecision.RejectReason.ToString()));
            return success;
        }

        public bool TryRunOutputPass(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace)
        {
            trace.Add(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSourceKind.CommittedAction);
            if (!context.StateDecision.HasStateFrame)
            {
                context.MarkFailed("behavior-action-output-context-missing");
                return false;
            }

            bool success = submitter.TrySubmitFrameOutput(runtime, ref context, out CharacterFrameSubmission submission);
            if (!success)
                return false;

            CharacterBehaviorSubmissionSource source = Source(CharacterBehaviorEvaluationPass.OutputPass, context.Step);
            CharacterFrameArbitrationInput actionOnlyInput = BuildActionOnlyArbitrationInput(
                in submission,
                context.Step);
            CharacterFrameActionOutputSubmission actionOutput = submission.ActionOutput;
            BehaviorOutputSubmission output = new BehaviorOutputSubmission(
                source,
                default,
                default,
                default,
                default,
                submission.ActionMotionResult,
                actionOutput,
                actionOnlyInput,
                submission.LocomotionPreemption,
                false);
            if (output.HasOutput)
                submissions.Add(output);
            AddActionChannelSubmissions(submissions, source, in actionOutput);
            if (!string.IsNullOrWhiteSpace(submission.Diagnostics.ActionMotionDiagnosticSummary))
            {
                submissions.Add(new BehaviorDiagnosticSubmission(
                    source,
                    "action-motion",
                    submission.Diagnostics.ActionMotionDiagnosticSummary,
                    false));
            }

            return true;
        }

        static CharacterFrameArbitrationInput BuildActionOnlyArbitrationInput(
            in CharacterFrameSubmission submission,
            int sourceStep)
        {
            CommittedActionBranchOutcome branchOutcome = submission.ActionOutput.CommittedActionBranchOutcome;
            BodyOccupancyClaim claim = branchOutcome.BodyClaim.HasClaim
                ? branchOutcome.BodyClaim
                : submission.ArbitrationInput.OccupancyClaim;
            CharacterFrameCandidateOutput actionCandidate = branchOutcome.Candidate.HasAnyCandidate
                ? branchOutcome.Candidate
                : submission.ArbitrationInput.CommittedActionCandidate;
            return new CharacterFrameArbitrationInput(
                claim,
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.Locomotion, sourceStep),
                actionCandidate,
                submission.ArbitrationInput.UpperBodyCandidate,
                sourceStep);
        }

        static void AddActionChannelSubmissions(
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionSource source,
            in CharacterFrameActionOutputSubmission actionOutput)
        {
            CommittedActionBranchOutcome branchOutcome = actionOutput.CommittedActionBranchOutcome;
            ActionTimelineOutcome timeline = branchOutcome.TimelineOutcome;
            if (timeline.HasMotion)
                submissions.Add(new BehaviorMotionChannelSubmission(source, timeline.MotionSpec));
            if (timeline.HasAnimation)
                submissions.Add(new BehaviorAnimationChannelSubmission(source, timeline.AnimationKey));
            if (timeline.ActiveWindowFactIds.Count > 0)
            {
                string[] facts = new string[timeline.ActiveWindowFactIds.Count];
                for (int i = 0; i < facts.Length; i++)
                    facts[i] = timeline.ActiveWindowFactIds[i];
                submissions.Add(new BehaviorWindowFactsChannelSubmission(source, facts));
            }
            if (branchOutcome.BodyClaim.HasClaim)
                submissions.Add(new BehaviorClaimSubmission(source, branchOutcome.BodyClaim));
            for (int i = 0; i < timeline.CueRequests.Count; i++)
                submissions.Add(new BehaviorCueSubmission(source, timeline.CueRequests[i].CueId, timeline.LocalTick));
            if (branchOutcome.HasDiagnostic)
            {
                submissions.Add(new BehaviorDiagnosticSubmission(
                    source,
                    "committed-action-branch",
                    branchOutcome.Diagnostic,
                    false));
            }
        }

        static CharacterBehaviorSubmissionSource Source(CharacterBehaviorEvaluationPass pass, int step)
        {
            return CharacterBehaviorSubmissionSource.Create(
                "behavior.committed-action",
                CharacterBehaviorSourceKind.CommittedAction,
                pass,
                step,
                1);
        }
    }
}
