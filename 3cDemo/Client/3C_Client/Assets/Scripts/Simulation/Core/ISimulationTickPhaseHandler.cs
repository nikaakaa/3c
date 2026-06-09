namespace ThirdPersonSimulation
{
    public interface ISimulationTickPhaseHandler
    {
        void Tick(SimulationTickPhase phase, in SimulationTickContext context);
    }
}
