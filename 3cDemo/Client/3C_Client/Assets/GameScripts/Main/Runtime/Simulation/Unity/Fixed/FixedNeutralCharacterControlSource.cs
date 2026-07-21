using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [DisallowMultipleComponent]
    public sealed class FixedNeutralCharacterControlSource : FixedCharacterControlSource
    {
        public override string SourceIdentity => "neutral-fixed-program-inputs/1";

        public override IUnityFixedCharacterControlSourceRuntime Create(FixedCharacterControlSourceContext context) =>
            new NeutralFixedCharacterSimulationInputAdapter(context.Program);
    }
}
