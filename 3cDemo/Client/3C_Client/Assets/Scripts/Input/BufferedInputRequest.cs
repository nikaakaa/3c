namespace ThirdPersonInput
{
    public struct BufferedInputRequest
    {
        public BufferedInputRequest(InputRequestKind kind, InputButtonKind sourceButton, int originStep, int expireStep)
        {
            Kind = kind;
            SourceButton = sourceButton;
            OriginStep = originStep;
            ExpireStep = expireStep < originStep ? originStep : expireStep;
            Consumed = false;
        }

        public InputRequestKind Kind { get; }
        public InputButtonKind SourceButton { get; }
        public int OriginStep { get; }
        public int ExpireStep { get; }
        public bool Consumed { get; private set; }

        public bool IsAvailableAt(int currentStep)
        {
            return !Consumed && currentStep >= OriginStep && currentStep <= ExpireStep;
        }

        public bool IsExpiredAt(int currentStep)
        {
            return currentStep > ExpireStep;
        }

        public void MarkConsumed()
        {
            Consumed = true;
        }
    }
}
