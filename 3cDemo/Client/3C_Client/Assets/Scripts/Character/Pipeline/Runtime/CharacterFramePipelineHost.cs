using System;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public sealed class CharacterFramePipelineHost
    {
        readonly CharacterFramePipeline pipeline;

        public CharacterFramePipelineHost(
            ICharacterFrameRequestSubmitter requestSubmitter,
            ICharacterFrameOutputSubmitter outputSubmitter)
        {
            pipeline = new CharacterFramePipeline(
                requestSubmitter ?? throw new ArgumentNullException(nameof(requestSubmitter)),
                outputSubmitter ?? throw new ArgumentNullException(nameof(outputSubmitter)));
        }

        public CharacterFrameResult LastFrameResult { get; private set; }

        public bool Tick(
            ICharacterFrameRuntimePort runtime,
            in CharacterFrameInput input,
            out CharacterFrameResult result)
        {
            bool success = pipeline.Tick(runtime, in input, out result);
            LastFrameResult = result;
            return success;
        }

        public CharacterFrameContext BeginFrame(in CharacterFrameInput input)
        {
            return pipeline.BeginFrame(in input);
        }

        public bool RunPhase(
            ICharacterFrameRuntimePort runtime,
            SimulationTickPhase phase,
            ref CharacterFrameContext context,
            out CharacterFrameResult result)
        {
            bool success = pipeline.RunPhase(runtime, phase, ref context, out result);
            LastFrameResult = result;
            return success;
        }
    }
}
