using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPosePublishedProjectionReader
    {
        CharacterSimulationProgramAsset m_Program;
        CharacterPresentationProjectionAsset m_Projection;
        string m_ProgramHash;
        string m_Revision;
        CharacterPresentationProjection m_Value;
        string m_Error;

        internal bool TryRead(CharacterSimulationProgramAsset program, CharacterPresentationProjectionAsset projection,
            out CharacterPresentationProjection value, out string error)
        {
            if (m_Program != program || m_Projection != projection || m_ProgramHash != program.ProgramHash || m_Revision != projection.ProjectionRevision)
            {
                m_Program = program;
                m_Projection = projection;
                m_ProgramHash = program.ProgramHash;
                m_Revision = projection.ProjectionRevision;
                m_Value = null;
                m_Error = null;
                try
                {
                    var contract = Float32CharacterPresentationContractAdapter.Create(program.Load());
                    m_Value = projection.Load(contract);
                }
                catch (Exception exception)
                {
                    m_Error = exception.Message;
                }
            }
            value = m_Value;
            error = m_Error;
            return value != null;
        }
    }
}
