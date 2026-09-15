using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [DisallowMultipleComponent]
    public sealed class NeutralCharacterControlSource : CharacterControlSource
    {
        public override string SourceIdentity => "neutral-character-inputs/character-control";

        public override IUnityCharacterControlSourceRuntime Create(CharacterControlSourceContext context) =>
            new NeutralCharacterSimulationInputAdapter(context.Definition.InputProfile);
    }
}
