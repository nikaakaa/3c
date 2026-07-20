using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [DisallowMultipleComponent]
    public sealed class NeutralCharacterControlSource : CharacterControlSource
    {
        public override string SourceIdentity => "neutral-program-inputs/1";

        public override IUnityCharacterSimulationInputAdapter Create(CharacterControlSourceContext context) =>
            new NeutralCharacterSimulationInputAdapter(context.Program);
    }
}
