using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class PredictionInputHistoryTickRecorder : MonoBehaviour, ISimulationTickPhaseHandler
    {
        [SerializeField] UnitySimulationTickDriver tickDriver;
        [SerializeField] MonoBehaviour inputSourceBehaviour;
        [SerializeField, Min(1)] int capacity = 120;

        PredictionInputHistory history;
        bool registered;

        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => tickDriver = value; }
        public MonoBehaviour InputSourceBehaviour { get => inputSourceBehaviour; set => inputSourceBehaviour = value; }
        public PredictionInputHistory History => history ?? (history = new PredictionInputHistory(Mathf.Max(1, capacity)));
        public bool IsRegistered => registered;

        void Reset()
        {
            ResolveReferences();
        }

        void OnEnable()
        {
            Register();
        }

        void OnDisable()
        {
            Unregister();
        }

        public bool Register()
        {
            if (registered)
                return true;

            ResolveReferences();
            if (tickDriver == null || !(inputSourceBehaviour is IPredictionInputFrameSource))
                return false;

            tickDriver.Runner.Register(SimulationTickPhase.ReadInput, this);
            registered = true;
            return true;
        }

        public void Unregister()
        {
            if (!registered)
                return;

            if (tickDriver != null)
                tickDriver.Runner.Unregister(SimulationTickPhase.ReadInput, this);

            registered = false;
        }

        public void Tick(SimulationTickPhase phase, in SimulationTickContext context)
        {
            if (phase != SimulationTickPhase.ReadInput)
                return;

            if (!(inputSourceBehaviour is IPredictionInputFrameSource source))
                ResolveReferences();

            if (inputSourceBehaviour is IPredictionInputFrameSource resolvedSource &&
                resolvedSource.TryReadPredictionInput(in context, out PredictionInputFrame frame))
            {
                History.Write(in frame);
            }
        }

        void ResolveReferences()
        {
            if (tickDriver == null)
            {
                tickDriver = GetComponent<UnitySimulationTickDriver>();
                if (tickDriver == null)
                    tickDriver = GetComponentInParent<UnitySimulationTickDriver>();
                if (tickDriver == null)
                    tickDriver = GetComponentInChildren<UnitySimulationTickDriver>(true);
            }

            if (inputSourceBehaviour == null && TryResolveComponentInterface(out IPredictionInputFrameSource _, out MonoBehaviour sourceBehaviour))
                inputSourceBehaviour = sourceBehaviour;
        }

        bool TryResolveComponentInterface<T>(out T service, out MonoBehaviour serviceBehaviour) where T : class
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is T candidate)
                {
                    service = candidate;
                    serviceBehaviour = behaviours[i];
                    return true;
                }
            }

            service = null;
            serviceBehaviour = null;
            return false;
        }
    }
}
