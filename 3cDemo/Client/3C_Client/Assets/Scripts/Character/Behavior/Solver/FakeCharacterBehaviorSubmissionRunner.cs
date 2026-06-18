using System;

namespace ThirdPersonCharacterBehavior
{
    public interface ICharacterBehaviorLeafEvaluator
    {
        void Evaluate(in CharacterBehaviorLeafEvaluationInput input, CharacterBehaviorSubmissionSet submissions);
    }

    public readonly struct CharacterBehaviorLeafEvaluationInput
    {
        public CharacterBehaviorLeafEvaluationInput(
            CharacterBehaviorSubmissionSource source,
            CharacterBehaviorEvaluationPass pass)
        {
            Source = new CharacterBehaviorSubmissionSource(
                source.NodeId,
                source.SourceKind,
                pass,
                source.SourceStep,
                source.SourceOrder);
            Pass = pass;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public CharacterBehaviorEvaluationPass Pass { get; }
        public int SourceStep => Source.SourceStep;
    }

    public sealed class FakeCharacterBehaviorLeafEvaluator : ICharacterBehaviorLeafEvaluator
    {
        readonly string diagnosticCode;

        public FakeCharacterBehaviorLeafEvaluator(string diagnosticCode)
        {
            this.diagnosticCode = diagnosticCode ?? string.Empty;
        }

        public void Evaluate(in CharacterBehaviorLeafEvaluationInput input, CharacterBehaviorSubmissionSet submissions)
        {
            if (submissions == null)
                return;

            submissions.Add(new BehaviorDiagnosticSubmission(
                input.Source,
                diagnosticCode,
                input.Pass.ToString(),
                false));
            submissions.Add(new BehaviorStateWriteSubmission(
                input.Source,
                CharacterBehaviorStateOwner.BehaviorRuntime,
                input.Source.NodeId,
                "last-step",
                input.SourceStep.ToString()));
        }
    }

    public readonly struct FakeCharacterBehaviorLeaf
    {
        public FakeCharacterBehaviorLeaf(
            CharacterBehaviorSubmissionSource source,
            ICharacterBehaviorLeafEvaluator evaluator)
        {
            Source = source;
            Evaluator = evaluator;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public ICharacterBehaviorLeafEvaluator Evaluator { get; }
    }

    public sealed class FakeCharacterBehaviorSubmissionRunner
    {
        readonly FakeCharacterBehaviorLeaf[] leaves;

        public FakeCharacterBehaviorSubmissionRunner(FakeCharacterBehaviorLeaf[] leaves)
        {
            this.leaves = leaves ?? Array.Empty<FakeCharacterBehaviorLeaf>();
        }

        public CharacterBehaviorSubmissionSet Collect(CharacterBehaviorEvaluationPass pass)
        {
            CharacterBehaviorSubmissionSet submissions = new CharacterBehaviorSubmissionSet();
            for (int i = 0; i < leaves.Length; i++)
            {
                FakeCharacterBehaviorLeaf leaf = leaves[i];
                leaf.Evaluator?.Evaluate(
                    new CharacterBehaviorLeafEvaluationInput(leaf.Source, pass),
                    submissions);
            }

            return submissions;
        }
    }
}
