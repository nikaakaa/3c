using ThirdPersonAction;

namespace ThirdPersonCharacterBehavior
{
    public interface ICharacterBehaviorSubmissionLeaf
    {
        CharacterBehaviorSourceKind SourceKind { get; }

        bool TryRunRequestPass(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace);

        bool TryRunOutputPass(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace);
    }

    public sealed class CharacterBehaviorSubmissionRunner : ICharacterFrameRequestSubmitter, ICharacterFrameOutputSubmitter
    {
        readonly CharacterBehaviorRuntimeDefinition definition;
        readonly ICharacterBehaviorSubmissionLeaf[] leaves;
        readonly CharacterBehaviorSubmissionComposer composer;

        public CharacterBehaviorSubmissionRunner(CharacterBehaviorRuntimeDefinition definition)
            : this(
                definition,
                new LocomotionBehaviorSubmissionLeaf(),
                new CommittedActionBehaviorSubmissionLeaf(),
                new CharacterBehaviorSubmissionComposer())
        {
        }

        public CharacterBehaviorSubmissionRunner(
            CharacterBehaviorRuntimeDefinition definition,
            LocomotionBehaviorSubmissionLeaf locomotionLeaf,
            CommittedActionBehaviorSubmissionLeaf committedActionLeaf,
            CharacterBehaviorSubmissionComposer composer)
            : this(
                definition,
                new ICharacterBehaviorSubmissionLeaf[] { locomotionLeaf, committedActionLeaf },
                composer)
        {
        }

        public CharacterBehaviorSubmissionRunner(
            CharacterBehaviorRuntimeDefinition definition,
            ICharacterBehaviorSubmissionLeaf[] leaves,
            CharacterBehaviorSubmissionComposer composer)
        {
            this.definition = definition;
            this.leaves = leaves ?? System.Array.Empty<ICharacterBehaviorSubmissionLeaf>();
            this.composer = composer;
            LastRequestTrace = CharacterBehaviorSubmissionTrace.Empty;
            LastOutputTrace = CharacterBehaviorSubmissionTrace.Empty;
            LastSubmissions = CharacterBehaviorSubmissionSet.Empty;
        }

        public CharacterBehaviorSubmissionTrace LastRequestTrace { get; private set; }
        public CharacterBehaviorSubmissionTrace LastOutputTrace { get; private set; }
        public CharacterBehaviorSubmissionSet LastSubmissions { get; private set; }

        public bool TrySubmitFrameRequests(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context)
        {
            if (!EnsureDefinition(ref context))
                return false;

            CharacterBehaviorSubmissionSet submissions = new CharacterBehaviorSubmissionSet();
            CharacterBehaviorSubmissionTrace trace = new CharacterBehaviorSubmissionTrace();
            for (int i = 0; i < definition.LeafCount; i++)
            {
                CharacterBehaviorSourceKind leaf = definition.GetLeafAt(i);
                if (!RunRequestLeaf(leaf, runtime, ref context, submissions, trace))
                {
                    LastRequestTrace = trace;
                    LastSubmissions = submissions;
                    return false;
                }
            }

            LastRequestTrace = trace;
            LastSubmissions = submissions;
            return true;
        }

        public bool TrySubmitFrameOutput(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            out CharacterFrameSubmission submission)
        {
            submission = CharacterFrameSubmission.None(context.Step);
            if (!EnsureDefinition(ref context))
                return false;

            CharacterBehaviorSubmissionSet submissions = new CharacterBehaviorSubmissionSet();
            CharacterBehaviorSubmissionTrace trace = new CharacterBehaviorSubmissionTrace();
            for (int i = 0; i < definition.LeafCount; i++)
            {
                CharacterBehaviorSourceKind leaf = definition.GetLeafAt(i);
                if (!RunOutputLeaf(leaf, runtime, ref context, submissions, trace))
                {
                    LastOutputTrace = trace;
                    LastSubmissions = submissions;
                    return false;
                }
            }

            if (!composer.TryCompose(submissions, in context, out submission, out string diagnostic))
            {
                context.MarkFailed(diagnostic);
                LastOutputTrace = trace;
                LastSubmissions = submissions;
                return false;
            }

            context.SetFrameSubmission(in submission);
            LastOutputTrace = trace;
            LastSubmissions = submissions;
            return true;
        }

        bool RunRequestLeaf(
            CharacterBehaviorSourceKind leaf,
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace)
        {
            ICharacterBehaviorSubmissionLeaf submissionLeaf = ResolveLeaf(leaf);
            if (submissionLeaf != null)
                return submissionLeaf.TryRunRequestPass(runtime, ref context, submissions, trace);

            context.MarkFailed($"behavior-entry-leaf-unsupported:{leaf}");
            return false;
        }

        bool RunOutputLeaf(
            CharacterBehaviorSourceKind leaf,
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            CharacterBehaviorSubmissionSet submissions,
            CharacterBehaviorSubmissionTrace trace)
        {
            ICharacterBehaviorSubmissionLeaf submissionLeaf = ResolveLeaf(leaf);
            if (submissionLeaf != null)
                return submissionLeaf.TryRunOutputPass(runtime, ref context, submissions, trace);

            context.MarkFailed($"behavior-entry-leaf-unsupported:{leaf}");
            return false;
        }

        ICharacterBehaviorSubmissionLeaf ResolveLeaf(CharacterBehaviorSourceKind sourceKind)
        {
            for (int i = 0; i < leaves.Length; i++)
            {
                ICharacterBehaviorSubmissionLeaf leaf = leaves[i];
                if (leaf != null && leaf.SourceKind == sourceKind)
                    return leaf;
            }

            return null;
        }

        bool EnsureDefinition(ref CharacterFrameContext context)
        {
            if (definition.IsValid)
                return true;

            context.MarkFailed(definition.Diagnostic);
            return false;
        }
    }
}
