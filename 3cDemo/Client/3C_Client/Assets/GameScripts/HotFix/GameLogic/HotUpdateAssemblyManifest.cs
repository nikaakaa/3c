namespace GameLogic
{
    public static class HotUpdateAssemblyManifest
    {
        public const string MainAssemblyName = "GameLogic.dll";

        public static readonly string[] HotUpdateAssemblies =
        {
            "GameBase.dll",
            "GameProto.dll",
            "BattleCore.dll",
            MainAssemblyName
        };
    }
}
