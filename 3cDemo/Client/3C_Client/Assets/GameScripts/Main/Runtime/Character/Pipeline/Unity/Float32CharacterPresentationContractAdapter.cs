using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public static class Float32CharacterPresentationContractAdapter
    {
        public static CharacterPresentationSemanticContract Create(CharacterSimulationProgram program)
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
