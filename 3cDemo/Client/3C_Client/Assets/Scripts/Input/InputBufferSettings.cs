using System;

namespace ThirdPersonInput
{
    public readonly struct InputBufferSettings
    {
        public InputBufferSettings(int attackWindowSteps, int dodgeWindowSteps, int jumpWindowSteps, int interactWindowSteps)
        {
            AttackWindowSteps = ClampWindow(attackWindowSteps);
            DodgeWindowSteps = ClampWindow(dodgeWindowSteps);
            JumpWindowSteps = ClampWindow(jumpWindowSteps);
            InteractWindowSteps = ClampWindow(interactWindowSteps);
        }

        public int AttackWindowSteps { get; }
        public int DodgeWindowSteps { get; }
        public int JumpWindowSteps { get; }
        public int InteractWindowSteps { get; }

        public static InputBufferSettings Default => new InputBufferSettings(6, 4, 4, 2);

        public int GetWindowSteps(InputRequestKind kind)
        {
            switch (kind)
            {
                case InputRequestKind.Attack:
                    return AttackWindowSteps;
                case InputRequestKind.Dodge:
                    return DodgeWindowSteps;
                case InputRequestKind.Jump:
                    return JumpWindowSteps;
                case InputRequestKind.Interact:
                    return InteractWindowSteps;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        static int ClampWindow(int windowSteps)
        {
            return windowSteps < 0 ? 0 : windowSteps;
        }
    }
}
