using ThirdPerson.NetworkTest.Contracts;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class NetworkTestProductAdapters
    {
        public static readonly INetworkTestProductBuildAdapter DeterministicRollback = new DeterministicRollbackNetworkTestProductAdapter();

        public static readonly INetworkTestProductBuildAdapter[] All =
        {
            DeterministicRollback
        };
    }
}
