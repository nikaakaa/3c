using ThirdPersonCharacter.Control.Rules;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public static class CharacterControlRuntimeModuleCatalog
    {
        public static CharacterControlModuleCatalog Create() =>
            CorinCharacterControlModuleCatalog.Create();
    }
}
