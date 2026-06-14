namespace ThirdPersonSimulation
{
    public interface IPredictionInputFrameSource
    {
        bool TryReadPredictionInput(in SimulationTickContext context, out PredictionInputFrame frame);
    }

    public interface IPredictionInputCameraBasisSource
    {
        RollbackCameraBasisState CapturePredictionCameraBasis();
    }
}
