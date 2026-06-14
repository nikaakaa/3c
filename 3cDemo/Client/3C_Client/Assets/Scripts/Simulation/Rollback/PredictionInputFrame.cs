using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct PredictionInputFrame
    {
        public PredictionInputFrame(
            SimulationTick tick,
            Vector2 move,
            Vector2 look,
            bool runHeld,
            PredictionButtonFrame dodge,
            PredictionButtonFrame attack,
            PredictionButtonFrame jump,
            PredictionButtonFrame interact)
            : this(
                tick,
                move,
                look,
                runHeld,
                dodge,
                attack,
                jump,
                interact,
                RollbackCameraBasisState.Default,
                false)
        {
        }

        public PredictionInputFrame(
            SimulationTick tick,
            Vector2 move,
            Vector2 look,
            bool runHeld,
            PredictionButtonFrame dodge,
            PredictionButtonFrame attack,
            PredictionButtonFrame jump,
            PredictionButtonFrame interact,
            RollbackCameraBasisState cameraBasisState)
            : this(
                tick,
                move,
                look,
                runHeld,
                dodge,
                attack,
                jump,
                interact,
                cameraBasisState,
                true)
        {
        }

        PredictionInputFrame(
            SimulationTick tick,
            Vector2 move,
            Vector2 look,
            bool runHeld,
            PredictionButtonFrame dodge,
            PredictionButtonFrame attack,
            PredictionButtonFrame jump,
            PredictionButtonFrame interact,
            RollbackCameraBasisState cameraBasisState,
            bool hasCameraBasis)
        {
            Tick = tick;
            Move = ClampUnit(move);
            Look = ClampUnit(look);
            RunHeld = runHeld;
            Dodge = dodge;
            Attack = attack;
            Jump = jump;
            Interact = interact;
            CameraBasisState = SanitizeCameraBasis(in cameraBasisState);
            HasCameraBasis = hasCameraBasis;
        }

        public SimulationTick Tick { get; }
        public Vector2 Move { get; }
        public Vector2 Look { get; }
        public bool RunHeld { get; }
        public PredictionButtonFrame Dodge { get; }
        public PredictionButtonFrame Attack { get; }
        public PredictionButtonFrame Jump { get; }
        public PredictionButtonFrame Interact { get; }
        public RollbackCameraBasisState CameraBasisState { get; }
        public bool HasCameraBasis { get; }

        public BasicLocomotionInputSnapshot ToLocomotionInput(float deltaTime)
        {
            return new BasicLocomotionInputSnapshot(deltaTime, Move, Look, RunHeld);
        }

        public PredictionInputFrame WithCameraBasis(in RollbackCameraBasisState cameraBasisState)
        {
            return new PredictionInputFrame(
                Tick,
                Move,
                Look,
                RunHeld,
                Dodge,
                Attack,
                Jump,
                Interact,
                cameraBasisState);
        }

        static Vector2 ClampUnit(Vector2 value)
        {
            value.x = Sanitize(value.x);
            value.y = Sanitize(value.y);
            return value.sqrMagnitude > 1f ? value.normalized : value;
        }

        static float Sanitize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            return Mathf.Clamp(value, -1f, 1f);
        }

        static RollbackCameraBasisState SanitizeCameraBasis(in RollbackCameraBasisState cameraBasisState)
        {
            return cameraBasisState.PlanarForward.sqrMagnitude > 0.000001f &&
                   cameraBasisState.PlanarRight.sqrMagnitude > 0.000001f
                ? cameraBasisState
                : RollbackCameraBasisState.Default;
        }
    }

    public static class PredictionInputFrameInputBufferReplay
    {
        public static void WriteToInputBuffer(in PredictionInputFrame frame, InputRequestBufferComponent buffer)
        {
            if (buffer == null)
                return;

            buffer.SetStep(frame.Tick.Value);
            AddButton(frame.Dodge, InputButtonKind.Dodge, buffer);
            AddButton(frame.Attack, InputButtonKind.Attack, buffer);
            AddButton(frame.Jump, InputButtonKind.Jump, buffer);
            AddButton(frame.Interact, InputButtonKind.Interact, buffer);
        }

        static void AddButton(in PredictionButtonFrame frame, InputButtonKind kind, InputRequestBufferComponent buffer)
        {
            buffer.AddButtonState(kind, new InputButtonState(frame.Pressed, frame.Held, frame.Released));
        }
    }
}
