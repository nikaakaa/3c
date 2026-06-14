using UnityEngine;

namespace ThirdPersonInput
{
    public readonly struct InputRequestBufferComponentRestoreState
    {
        public InputRequestBufferComponentRestoreState(int currentStep, InputRequestBufferRestoreState buffer)
        {
            CurrentStep = currentStep < 0 ? 0 : currentStep;
            Buffer = buffer;
        }

        public int CurrentStep { get; }
        public InputRequestBufferRestoreState Buffer { get; }

        public static InputRequestBufferComponentRestoreState Empty =>
            new InputRequestBufferComponentRestoreState(0, InputRequestBufferRestoreState.Empty);
    }

    [DisallowMultipleComponent]
    public sealed class InputRequestBufferComponent : MonoBehaviour
    {
        [SerializeField, Min(0)] int attackWindowSteps = 6;
        [SerializeField, Min(0)] int dodgeWindowSteps = 4;
        [SerializeField, Min(0)] int jumpWindowSteps = 4;
        [SerializeField, Min(0)] int interactWindowSteps = 2;

        readonly InputRequestBuffer buffer = new InputRequestBuffer();
        int currentStep;

        public InputRequestBuffer Buffer => buffer;
        public int CurrentStep => currentStep;
        public InputBufferSettings Settings => new InputBufferSettings(attackWindowSteps, dodgeWindowSteps, jumpWindowSteps, interactWindowSteps);

        public void SetStep(int step)
        {
            currentStep = Mathf.Max(0, step);
            buffer.RemoveExpired(currentStep);
        }

        public void AdvanceStep()
        {
            SetStep(currentStep + 1);
        }

        public void AddButtonState(InputButtonKind button, InputButtonState state)
        {
            InputBufferSettings settings = Settings;
            buffer.AddFromButtonState(button, state, currentStep, in settings);
        }

        public InputRequestBufferComponentRestoreState CaptureRestoreState()
        {
            return new InputRequestBufferComponentRestoreState(currentStep, buffer.CaptureRestoreState());
        }

        public void Restore(in InputRequestBufferComponentRestoreState restoreState)
        {
            currentStep = Mathf.Max(0, restoreState.CurrentStep);
            buffer.Restore(restoreState.Buffer);
            buffer.RemoveExpired(currentStep);
        }

        public void Clear()
        {
            buffer.Clear();
        }
    }
}
