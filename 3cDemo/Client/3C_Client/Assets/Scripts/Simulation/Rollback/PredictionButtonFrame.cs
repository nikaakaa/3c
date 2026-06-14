namespace ThirdPersonSimulation
{
    public readonly struct PredictionButtonFrame
    {
        public PredictionButtonFrame(bool pressed, bool held, bool released)
        {
            Pressed = pressed;
            Held = held;
            Released = released;
        }

        public bool Pressed { get; }
        public bool Held { get; }
        public bool Released { get; }

        public static PredictionButtonFrame None => new PredictionButtonFrame(false, false, false);
    }
}
