using System;

namespace ThirdPersonAction
{
    public sealed class CharacterFrameSubmitterGraph : ICharacterFrameRequestSubmitter, ICharacterFrameOutputSubmitter
    {
        readonly ICharacterFrameRequestSubmitter[] requestSubmitters;
        readonly ICharacterFrameOutputSubmitter[] outputSubmitters;

        public CharacterFrameSubmitterGraph(
            ICharacterFrameRequestSubmitter[] requestSubmitters,
            ICharacterFrameOutputSubmitter[] outputSubmitters)
        {
            this.requestSubmitters = requestSubmitters ?? Array.Empty<ICharacterFrameRequestSubmitter>();
            this.outputSubmitters = outputSubmitters ?? Array.Empty<ICharacterFrameOutputSubmitter>();
        }

        public static CharacterFrameSubmitterGraph CreateDefault()
        {
            LocomotionFrameSubmitter locomotionSubmitter = new LocomotionFrameSubmitter();
            FullBodyActionFrameSubmitter actionSubmitter = new FullBodyActionFrameSubmitter();
            return new CharacterFrameSubmitterGraph(
                new ICharacterFrameRequestSubmitter[]
                {
                    locomotionSubmitter,
                    actionSubmitter
                },
                new ICharacterFrameOutputSubmitter[]
                {
                    locomotionSubmitter,
                    actionSubmitter
                });
        }

        public bool TrySubmitFrameRequests(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context)
        {
            for (int i = 0; i < requestSubmitters.Length; i++)
            {
                ICharacterFrameRequestSubmitter submitter = requestSubmitters[i];
                if (submitter == null)
                    continue;

                if (!submitter.TrySubmitFrameRequests(runtime, ref context))
                    return false;
            }

            return true;
        }

        public bool TrySubmitFrameOutput(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            out CharacterFrameSubmission submission)
        {
            for (int i = 0; i < outputSubmitters.Length; i++)
            {
                ICharacterFrameOutputSubmitter submitter = outputSubmitters[i];
                if (submitter == null)
                    continue;

                if (submitter.TrySubmitFrameOutput(runtime, ref context, out submission))
                    return true;

                if (context.CurrentStep == CharacterFramePipelineStep.Failed)
                    return false;
            }

            submission = CharacterFrameSubmission.None(context.Step);
            return false;
        }
    }
}
