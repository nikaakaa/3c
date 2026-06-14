namespace ThirdPersonSimulation
{
    public readonly struct PredictionHistoryQueryResult
    {
        public PredictionHistoryQueryResult(bool success, SimulationTick tick, string reason)
        {
            Success = success;
            Tick = tick;
            Reason = reason ?? string.Empty;
        }

        public bool Success { get; }
        public SimulationTick Tick { get; }
        public string Reason { get; }

        public static PredictionHistoryQueryResult Ok(SimulationTick tick)
        {
            return new PredictionHistoryQueryResult(true, tick, string.Empty);
        }

        public static PredictionHistoryQueryResult Missing(SimulationTick tick)
        {
            return new PredictionHistoryQueryResult(false, tick, $"missing tick {tick.Value}");
        }
    }
}
