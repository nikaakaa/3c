using System;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonCharacterBehavior
{
    public sealed class CharacterBehaviorSubmissionTrace
    {
        readonly CharacterBehaviorSourceKind[] sources = new CharacterBehaviorSourceKind[8];
        readonly CharacterBehaviorEvaluationPass[] passes = new CharacterBehaviorEvaluationPass[8];
        int count;

        public int Count => count;

        public void Add(CharacterBehaviorEvaluationPass pass, CharacterBehaviorSourceKind source)
        {
            if (count >= sources.Length)
                return;

            passes[count] = pass;
            sources[count] = source;
            count++;
        }

        public CharacterBehaviorSourceKind SourceAt(int index)
        {
            return index >= 0 && index < count ? sources[index] : CharacterBehaviorSourceKind.None;
        }

        public CharacterBehaviorEvaluationPass PassAt(int index)
        {
            return index >= 0 && index < count ? passes[index] : CharacterBehaviorEvaluationPass.None;
        }

        public static CharacterBehaviorSubmissionTrace Empty => new CharacterBehaviorSubmissionTrace();
    }

    public sealed class CharacterBehaviorSubmissionComposer
    {
        readonly CharacterFrameOutputComposer frameOutputComposer = new CharacterFrameOutputComposer();

        public bool TryCompose(
            CharacterBehaviorSubmissionSet submissions,
            in CharacterFrameContext context,
            out CharacterFrameSubmission submission,
            out string diagnostic)
        {
            submission = CharacterFrameSubmission.None(context.Step);
            diagnostic = string.Empty;
            if (submissions == null)
            {
                diagnostic = "behavior-submissions-missing";
                return false;
            }

            if (!TryGetOutput(
                    submissions,
                    CharacterBehaviorSourceKind.Locomotion,
                    out BehaviorOutputSubmission locomotionOutput))
            {
                diagnostic = "behavior-required-output-missing";
                return false;
            }

            if (!ValidateRequiredOutputs(submissions, out diagnostic))
                return false;

            bool hasCommittedActionOutput = TryGetOutput(
                submissions,
                CharacterBehaviorSourceKind.CommittedAction,
                out BehaviorOutputSubmission committedActionOutput);
            CharacterFrameActionOutputSubmission actionOutput = hasCommittedActionOutput
                ? committedActionOutput.ActionOutput
                : CharacterFrameActionOutputSubmission.None(context.Step);
            ActionMotionResolveResult actionMotionResult = hasCommittedActionOutput
                ? committedActionOutput.ActionMotionResult
                : ActionMotionResolveResult.None(context.Step);
            CharacterFrameArbitrationInput arbitrationInput = BuildArbitrationInput(
                in locomotionOutput,
                hasCommittedActionOutput,
                in committedActionOutput,
                context.Step);
            LocomotionPreemptionFact locomotionPreemption = hasCommittedActionOutput
                ? committedActionOutput.LocomotionPreemption
                : LocomotionPreemptionFact.None;
            submission = new CharacterFrameSubmission(
                CharacterFrameSubmissionSource.CharacterRuntimeGraph,
                context.Step,
                locomotionOutput.LocomotionDecision,
                locomotionOutput.StateDecision,
                locomotionOutput.LocomotionFrame,
                locomotionOutput.StateFrame,
                actionMotionResult,
                context.InputRequest,
                context.ActionDecision,
                context.CurrentTimelineFactsTrace,
                context.PreviousStateSnapshot,
                context.ExitedToLocomotion || actionOutput.ExitedToLocomotion,
                actionOutput,
                arbitrationInput,
                locomotionPreemption);
            CharacterFramePlan plan = frameOutputComposer.CreatePlan(in submission);
            if (!plan.HasPlan)
            {
                diagnostic = "behavior-frame-plan-missing";
                return false;
            }

            return true;
        }

        static bool TryGetOutput(
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSourceKind sourceKind,
            out BehaviorOutputSubmission output)
        {
            output = default;
            if (submissions == null)
                return false;

            for (int i = 0; i < submissions.Outputs.Count; i++)
            {
                BehaviorOutputSubmission candidate = submissions.Outputs[i];
                if (candidate.Source.SourceKind == sourceKind && candidate.HasOutput)
                {
                    output = candidate;
                    return true;
                }
            }

            return false;
        }

        static bool ValidateRequiredOutputs(
            CharacterBehaviorSubmissionSet submissions,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            for (int i = 0; i < submissions.Outputs.Count; i++)
            {
                BehaviorOutputSubmission output = submissions.Outputs[i];
                if (!output.Required)
                    continue;

                if (output.Source.SourceKind == CharacterBehaviorSourceKind.Locomotion &&
                    output.HasOutput)
                {
                    continue;
                }

                diagnostic = output.HasOutput
                    ? $"behavior-required-output-unsupported:{output.Source.SourceKind}"
                    : $"behavior-required-output-empty:{output.Source.SourceKind}";
                return false;
            }

            return true;
        }

        static CharacterFrameArbitrationInput BuildArbitrationInput(
            in BehaviorOutputSubmission locomotionOutput,
            bool hasCommittedActionOutput,
            in BehaviorOutputSubmission committedActionOutput,
            int sourceStep)
        {
            CharacterFrameCandidateOutput locomotionCandidate = CharacterFrameCandidateOutput.Locomotion(
                locomotionOutput.StateFrame.ExecuteBasicMovement,
                locomotionOutput.StateFrame.PresentLocomotionAnimation,
                sourceStep);
            if (!hasCommittedActionOutput)
            {
                return new CharacterFrameArbitrationInput(
                    BodyOccupancyClaim.None(sourceStep),
                    locomotionCandidate,
                    CharacterFrameCandidateOutput.None(CharacterBodyDomain.CommittedAction, sourceStep),
                    CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, sourceStep),
                    sourceStep);
            }

            return new CharacterFrameArbitrationInput(
                committedActionOutput.ArbitrationInput.OccupancyClaim,
                locomotionCandidate,
                committedActionOutput.ArbitrationInput.CommittedActionCandidate,
                committedActionOutput.ArbitrationInput.UpperBodyCandidate,
                sourceStep);
        }
    }
}
