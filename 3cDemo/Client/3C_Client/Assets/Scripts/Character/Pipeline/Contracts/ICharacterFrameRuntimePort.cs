namespace ThirdPersonAction
{
    public interface ICharacterFrameRuntimePort : IFullBodySubmissionRuntimePort, IFullBodyOutputRuntimePort
    {
        bool WriteBufferedInputFacts(in CharacterFrameInput input);
    }
}
