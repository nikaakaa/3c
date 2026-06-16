namespace ThirdPersonAction
{
    public interface ICharacterFrameOutputSubmitter
    {
        bool TrySubmitFrameOutput(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            out CharacterFrameSubmission submission);
    }
}
