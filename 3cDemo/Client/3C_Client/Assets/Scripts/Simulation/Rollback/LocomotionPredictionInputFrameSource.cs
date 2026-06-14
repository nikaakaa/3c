using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class LocomotionPredictionInputFrameSource : MonoBehaviour, IPredictionInputFrameSource, IPredictionInputCameraBasisSource
    {
        [SerializeField] PlayerLocomotionController locomotionController;
        [SerializeField] MonoBehaviour buttonSourceBehaviour;

        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }
        public MonoBehaviour ButtonSourceBehaviour { get => buttonSourceBehaviour; set => buttonSourceBehaviour = value; }

        void Reset()
        {
            ResolveReferences();
        }

        public bool TryReadPredictionInput(in SimulationTickContext context, out PredictionInputFrame frame)
        {
            ResolveReferences();
            if (locomotionController == null || !locomotionController.TryReadInput(context.FixedDeltaSecondsFloat, out BasicLocomotionInputSnapshot input))
            {
                frame = default;
                return false;
            }

            ResolveButtons(out PredictionButtonFrame dodge, out PredictionButtonFrame attack, out PredictionButtonFrame jump, out PredictionButtonFrame interact);
            RollbackCameraBasisState cameraBasisState = CapturePredictionCameraBasis();
            frame = new PredictionInputFrame(
                context.Tick,
                input.Move,
                input.Look,
                input.RunHeld,
                dodge,
                attack,
                jump,
                interact,
                cameraBasisState);
            return true;
        }

        public RollbackCameraBasisState CapturePredictionCameraBasis()
        {
            ResolveReferences();
            return locomotionController != null
                ? locomotionController.CaptureRollbackCameraBasisState()
                : RollbackCameraBasisState.Default;
        }

        void ResolveButtons(
            out PredictionButtonFrame dodge,
            out PredictionButtonFrame attack,
            out PredictionButtonFrame jump,
            out PredictionButtonFrame interact)
        {
            if (buttonSourceBehaviour is IPredictionButtonFrameSource source &&
                source.TryReadPredictionButtons(out dodge, out attack, out jump, out interact))
            {
                return;
            }

            dodge = PredictionButtonFrame.None;
            attack = PredictionButtonFrame.None;
            jump = PredictionButtonFrame.None;
            interact = PredictionButtonFrame.None;
        }

        void ResolveReferences()
        {
            if (locomotionController == null)
            {
                locomotionController = GetComponent<PlayerLocomotionController>();
                if (locomotionController == null)
                    locomotionController = GetComponentInParent<PlayerLocomotionController>();
            }

            if (buttonSourceBehaviour == null && TryResolveComponentInterface(out IPredictionButtonFrameSource _, out MonoBehaviour sourceBehaviour))
            {
                buttonSourceBehaviour = sourceBehaviour;
                return;
            }

            if (buttonSourceBehaviour == null)
            {
                MonoBehaviour[] parentBehaviours = GetComponentsInParent<MonoBehaviour>(true);
                for (int i = 0; i < parentBehaviours.Length; i++)
                {
                    if (parentBehaviours[i] is IPredictionButtonFrameSource)
                    {
                        buttonSourceBehaviour = parentBehaviours[i];
                        return;
                    }
                }
            }

            if (buttonSourceBehaviour == null)
            {
                MonoBehaviour[] childBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < childBehaviours.Length; i++)
                {
                    if (childBehaviours[i] is IPredictionButtonFrameSource)
                    {
                        buttonSourceBehaviour = childBehaviours[i];
                        return;
                    }
                }
            }
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
