using UnityEngine;

namespace ThirdPersonInput
{
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

        public void Clear()
        {
            buffer.Clear();
        }
    }
}
