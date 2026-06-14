namespace ThirdPersonSimulation
{
    public readonly struct ReconciledInputFrame
    {
        public ReconciledInputFrame(PredictionInputFrame frame, bool isPrediction)
        {
            Frame = frame;
            IsPrediction = isPrediction;
        }

        public PredictionInputFrame Frame { get; }
        public bool IsPrediction { get; }
    }

    public static class ReconciledInputResolver
    {
        public static ReconciledInputFrame Resolve(
            SimulationTick tick,
            SimulationTick currentTick,
            LatencySimulator remoteSimulator,
            RepeatLastFramePredictionStrategy predictionStrategy)
        {
            if (remoteSimulator.TryGet(tick, currentTick, out PredictionInputFrame realFrame))
            {
                predictionStrategy.RecordFrame(in realFrame);
                return new ReconciledInputFrame(realFrame, false);
            }

            if (predictionStrategy.TryPredict(tick, out PredictionInputFrame predictedFrame))
            {
                return new ReconciledInputFrame(predictedFrame, true);
            }

            return new ReconciledInputFrame(new PredictionInputFrame(
                tick,
                UnityEngine.Vector2.zero,
                UnityEngine.Vector2.zero,
                false,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None), true);
        }
    }
}
