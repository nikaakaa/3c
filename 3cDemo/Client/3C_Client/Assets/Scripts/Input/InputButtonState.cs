namespace ThirdPersonInput
{
    public readonly struct InputButtonState
    {
        public InputButtonState(bool pressed, bool held, bool released)
        {
            Pressed = pressed;
            Held = held;
            Released = released;
        }

        public bool Pressed { get; }
        public bool Held { get; }
        public bool Released { get; }

        public static InputButtonState FromHeld(bool wasHeld, bool isHeld)
        {
            return new InputButtonState(
                isHeld && !wasHeld,
                isHeld,
                wasHeld && !isHeld);
        }
    }
}
