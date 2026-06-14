namespace ThirdPersonSimulation
{
    public interface IPredictionButtonFrameSource
    {
        bool TryReadPredictionButtons(
            out PredictionButtonFrame dodge,
            out PredictionButtonFrame attack,
            out PredictionButtonFrame jump,
            out PredictionButtonFrame interact);
    }
}
