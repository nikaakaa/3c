using System;
using System.Collections.Generic;
using ThirdPersonDiagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonInput
{
    public sealed class InputRequestBuffer
    {
        readonly List<BufferedInputRequest> requests = new List<BufferedInputRequest>();

        public int Count => requests.Count;
        public IReadOnlyList<BufferedInputRequest> Requests => requests;

        public void AddFromButtonState(InputButtonKind button, InputButtonState state, int currentStep, in InputBufferSettings settings)
        {
            if (!state.Pressed)
                return;

            InputRequestKind kind = ToRequestKind(button);
            AddRequest(kind, button, currentStep, settings.GetWindowSteps(kind));
        }

        public void AddFromButtonState(InputButtonKind button, InputButtonState state, SimulationTick currentTick, in InputBufferSettings settings)
        {
            AddFromButtonState(button, state, currentTick.Value, in settings);
        }

        public BufferedInputRequest AddRequest(InputRequestKind kind, InputButtonKind sourceButton, int originStep, int windowSteps)
        {
            int safeWindow = windowSteps < 0 ? 0 : windowSteps;
            BufferedInputRequest request = new BufferedInputRequest(kind, sourceButton, originStep, originStep + safeWindow);
            requests.Add(request);
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Input,
                RuntimeDiagnosticLogLevel.Info,
                "input-request-added",
                string.Empty,
                string.Empty,
                originStep,
                0,
                $"kind={kind} button={sourceButton} origin={request.OriginStep} expire={request.ExpireStep} window={safeWindow} count={requests.Count}"));
            return request;
        }

        public bool TryPeek(InputRequestKind kind, int currentStep, out BufferedInputRequest request)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                BufferedInputRequest candidate = requests[i];
                if (candidate.Kind == kind && candidate.IsAvailableAt(currentStep))
                {
                    request = candidate;
                    return true;
                }
            }

            request = default;
            return false;
        }

        public bool TryPeek(InputRequestKind kind, SimulationTick currentTick, out BufferedInputRequest request)
        {
            return TryPeek(kind, currentTick.Value, out request);
        }

        public bool TryConsume(InputRequestKind kind, int currentStep, out BufferedInputRequest request)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                BufferedInputRequest candidate = requests[i];
                if (candidate.Kind != kind || !candidate.IsAvailableAt(currentStep))
                    continue;

                candidate.MarkConsumed();
                requests[i] = candidate;
                request = candidate;
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Input,
                    RuntimeDiagnosticLogLevel.Info,
                    "input-request-consumed",
                    string.Empty,
                    string.Empty,
                    currentStep,
                    0,
                    $"kind={candidate.Kind} button={candidate.SourceButton} origin={candidate.OriginStep} expire={candidate.ExpireStep} count={requests.Count}"));
                return true;
            }

            request = default;
            return false;
        }

        public bool TryConsume(InputRequestKind kind, SimulationTick currentTick, out BufferedInputRequest request)
        {
            return TryConsume(kind, currentTick.Value, out request);
        }

        public void RemoveExpired(int currentStep)
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                BufferedInputRequest request = requests[i];
                if (!request.IsExpiredAt(currentStep))
                    continue;

                requests.RemoveAt(i);
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Input,
                    RuntimeDiagnosticLogLevel.Info,
                    "input-request-expired",
                    string.Empty,
                    string.Empty,
                    currentStep,
                    0,
                    $"kind={request.Kind} button={request.SourceButton} origin={request.OriginStep} expire={request.ExpireStep} consumed={request.Consumed} count={requests.Count}"));
            }
        }

        public void RemoveExpired(SimulationTick currentTick)
        {
            RemoveExpired(currentTick.Value);
        }

        public void Clear()
        {
            requests.Clear();
        }

        public static InputRequestKind ToRequestKind(InputButtonKind button)
        {
            switch (button)
            {
                case InputButtonKind.Attack:
                    return InputRequestKind.Attack;
                case InputButtonKind.Dodge:
                    return InputRequestKind.Dodge;
                case InputButtonKind.Jump:
                    return InputRequestKind.Jump;
                case InputButtonKind.Interact:
                    return InputRequestKind.Interact;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }
    }
}
