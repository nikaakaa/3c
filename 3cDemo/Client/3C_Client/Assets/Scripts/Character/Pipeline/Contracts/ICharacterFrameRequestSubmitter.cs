namespace ThirdPersonAction
{
    public interface ICharacterFrameRequestSubmitter
    {
        bool TrySubmitFrameRequests(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context);
    }
}
