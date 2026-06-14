namespace ThirdPersonSimulation
{
    public interface IPredictionInputStrategy
    {
        bool TryPredict(SimulationTick tick, out PredictionInputFrame predictedFrame);
    }

    public sealed class RepeatLastFramePredictionStrategy : IPredictionInputStrategy
    {
        PredictionInputFrame lastFrame;
        bool hasLastFrame;

        public RepeatLastFramePredictionStrategy()
        {
            lastFrame = default;
            hasLastFrame = false;
        }

        public void RecordFrame(in PredictionInputFrame frame)
        {
            lastFrame = frame;
            hasLastFrame = true;
        }

        public bool TryPredict(SimulationTick tick, out PredictionInputFrame predictedFrame)
        {
            if (!hasLastFrame)
            {
                predictedFrame = default;
                return false;
            }

            // Repeat last frame but: do not repeat pressed/released, keep held
            predictedFrame = new PredictionInputFrame(
                tick,
                lastFrame.Move,
                lastFrame.Look,
                lastFrame.RunHeld,
                new PredictionButtonFrame(false, lastFrame.Dodge.Held, false),
                new PredictionButtonFrame(false, lastFrame.Attack.Held, false),
                new PredictionButtonFrame(false, lastFrame.Jump.Held, false),
                new PredictionButtonFrame(false, lastFrame.Interact.Held, false));
            return true;
        }
    }
}
