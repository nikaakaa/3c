namespace ThirdPersonCharacter.Pipeline
{
    public enum CharacterInputSource
    {
        LocalDevice,
        ExternalFacts,
        None
    }

    public enum CharacterMotionAuthority
    {
        LocalSolver,
        ExternalPose,
        None
    }
}
