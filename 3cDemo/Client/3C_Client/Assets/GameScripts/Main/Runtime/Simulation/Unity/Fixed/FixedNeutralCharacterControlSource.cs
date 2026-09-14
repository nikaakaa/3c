using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [DisallowMultipleComponent]
    public sealed class FixedNeutralCharacterControlSource : FixedCharacterControlSource
    {
        public override string SourceIdentity => "neutral-character-inputs/fixed-q32-32";

        public override IUnityFixedCharacterControlSourceRuntime Create(FixedCharacterControlSourceContext context) =>
            new NeutralFixedCharacterSimulationInputAdapter(context.Definition.InputProfile);
    }
}
