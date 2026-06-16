using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonAction
{
    public readonly struct CharacterFrameInput
    {
        public CharacterFrameInput(
            int step,
            BasicLocomotionInputSnapshot locomotionInput,
            bool hasBufferedButtonFacts,
            PredictionButtonFrame dodge,
            PredictionButtonFrame attack,
            PredictionButtonFrame jump,
            PredictionButtonFrame interact)
            : this(step, locomotionInput, hasBufferedButtonFacts, dodge, attack, jump, interact, CharacterFrameExternalRequestSubmission.None)
        {
        }

        public CharacterFrameInput(
            int step,
            BasicLocomotionInputSnapshot locomotionInput,
            bool hasBufferedButtonFacts,
            PredictionButtonFrame dodge,
            PredictionButtonFrame attack,
            PredictionButtonFrame jump,
            PredictionButtonFrame interact,
            CharacterFrameExternalRequestSubmission externalRequestSubmission)
        {
            Step = step < 0 ? 0 : step;
            LocomotionInput = SanitizeInput(in locomotionInput);
            HasBufferedButtonFacts = hasBufferedButtonFacts;
            Dodge = dodge;
            Attack = attack;
            Jump = jump;
            Interact = interact;
            ExternalRequestSubmission = externalRequestSubmission;
        }

        public int Step { get; }
        public BasicLocomotionInputSnapshot LocomotionInput { get; }
        public float DeltaTime => LocomotionInput.DeltaTime;
        public bool HasBufferedButtonFacts { get; }
        public PredictionButtonFrame Dodge { get; }
        public PredictionButtonFrame Attack { get; }
        public PredictionButtonFrame Jump { get; }
        public PredictionButtonFrame Interact { get; }
        public CharacterFrameExternalRequestSubmission ExternalRequestSubmission { get; }

        public static CharacterFrameInput FromLocomotionInput(int step, in BasicLocomotionInputSnapshot input)
        {
            return new CharacterFrameInput(
                step,
                input,
                false,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None);
        }

        public static CharacterFrameInput FromPredictionInputFrame(in PredictionInputFrame frame, float deltaTime)
        {
            return new CharacterFrameInput(
                frame.Tick.Value,
                frame.ToLocomotionInput(SanitizeDelta(deltaTime)),
                true,
                frame.Dodge,
                frame.Attack,
                frame.Jump,
                frame.Interact);
        }

        static BasicLocomotionInputSnapshot SanitizeInput(in BasicLocomotionInputSnapshot input)
        {
            return new BasicLocomotionInputSnapshot(
                SanitizeDelta(input.DeltaTime),
                SanitizeVector(input.Move),
                SanitizeVector(input.Look),
                input.RunHeld);
        }

        static float SanitizeDelta(float deltaTime)
        {
            return float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f ? 0f : deltaTime;
        }

        static Vector2 SanitizeVector(Vector2 value)
        {
            value.x = SanitizeAxis(value.x);
            value.y = SanitizeAxis(value.y);
            return value;
        }

        static float SanitizeAxis(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
    }
}
