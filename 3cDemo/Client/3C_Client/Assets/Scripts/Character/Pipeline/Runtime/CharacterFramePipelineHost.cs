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

    public sealed class CharacterFrameRuntimeHost
    {
        readonly CharacterFramePipelineHost pipelineHost;

        public CharacterFrameRuntimeHost(
            ICharacterFrameRequestSubmitter requestSubmitter,
            ICharacterFrameOutputSubmitter outputSubmitter)
        {
            pipelineHost = new CharacterFramePipelineHost(
                requestSubmitter ?? throw new ArgumentNullException(nameof(requestSubmitter)),
                outputSubmitter ?? throw new ArgumentNullException(nameof(outputSubmitter)));
        }

        public CharacterFramePipelineHost PipelineHost => pipelineHost;
        public CharacterFrameResult LastFrameResult => pipelineHost.LastFrameResult;

        public bool Tick(
            ICharacterFrameRuntimePort runtime,
            in CharacterFrameInput input,
            out CharacterFrameResult result)
        {
            return pipelineHost.Tick(runtime, in input, out result);
        }

        public CharacterFrameContext BeginFrame(in CharacterFrameInput input)
        {
            return pipelineHost.BeginFrame(in input);
        }

        public bool RunPhase(
            ICharacterFrameRuntimePort runtime,
            SimulationTickPhase phase,
            ref CharacterFrameContext context,
            out CharacterFrameResult result)
        {
            return pipelineHost.RunPhase(runtime, phase, ref context, out result);
        }
    }
}
