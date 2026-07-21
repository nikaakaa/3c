using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public static class FixedCharacterPresentationContractAdapter
    {
        public static CharacterPresentationSemanticContract Create(ThirdPersonSimulation.Fixed.CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            return new CharacterPresentationSemanticContract(
                program.Manifest.ProgramId,
                program.Manifest.SourceRevision,
                program.Manifest.SemanticHash,
                program.Producers);
        }
    }
}
