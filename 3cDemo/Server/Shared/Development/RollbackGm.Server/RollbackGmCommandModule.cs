namespace ThirdPerson.Development.Gm.Rollback;

public static class RollbackGmCommandModule
{
    public static GmCommandRegistry CreateRegistry(IRollbackGmQuerySource source)
    {
        var registry = new GmCommandRegistry();
        registry.Register(new HelpGmCommand(registry));
        registry.Register(new SessionInfoGmCommand(source));
        registry.Register(new ActorListGmCommand(source));
        registry.Register(new RuntimeStatusGmCommand(source));
        registry.Seal();
        return registry;
    }
}
