namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public readonly struct GameplayLabFootIkKeyboardMove
    {
        public const float DeadZone = 0.35f;

        public GameplayLabFootIkKeyboardMove(bool a, bool d, bool w, bool s)
        {
            A = a;
            D = d;
            W = w;
            S = s;
        }

        public bool A { get; }
        public bool D { get; }
        public bool W { get; }
        public bool S { get; }

        public static GameplayLabFootIkKeyboardMove FromCameraRelative(float x, float y)
        {
            bool a = x <= -DeadZone;
            bool d = x >= DeadZone;
            bool w = y >= DeadZone;
            bool s = y <= -DeadZone;
            if (a && d)
            {
                a = x < 0f;
                d = !a;
            }
            if (w && s)
            {
                w = y > 0f;
                s = !w;
            }
            return new GameplayLabFootIkKeyboardMove(a, d, w, s);
        }
    }
}
