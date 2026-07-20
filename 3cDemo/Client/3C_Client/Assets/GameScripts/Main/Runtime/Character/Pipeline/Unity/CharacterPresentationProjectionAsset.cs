using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class CharacterPresentationProjectionAsset : ScriptableObject
    {
        [SerializeField] CharacterPresentationProjection m_Projection;

        public string ProgramId => m_Projection?.ProgramId ?? string.Empty;
        public string ProgramHash => m_Projection?.ProgramHash ?? string.Empty;
        public string SourceRevision => m_Projection?.SourceRevision ?? string.Empty;
        public string SemanticHash => m_Projection?.SemanticHash ?? string.Empty;
        public string NumericProfileId => m_Projection?.NumericProfileId ?? string.Empty;
        public int TargetAbiVersion => m_Projection?.TargetAbiVersion ?? 0;

        public CharacterPresentationProjection Load(CharacterSimulationProgram program)
        {
            if (m_Projection == null)
                throw new InvalidOperationException($"Character Presentation Projection asset '{name}' has no compiled projection.");
            m_Projection.RequireProgram(program);
            return m_Projection;
        }

        public CharacterPresentationProjection Load(CharacterPresentationProgramIdentity program)
        {
            if (m_Projection == null)
                throw new InvalidOperationException($"Character Presentation Projection asset '{name}' has no compiled projection.");
            m_Projection.RequireSemanticProgram(program);
            return m_Projection;
        }

#if UNITY_EDITOR
        public CharacterPresentationProjection Inspect(CharacterSimulationProgramAsset program)
        {
            if (m_Projection == null)
                throw new InvalidOperationException($"Character Presentation Projection asset '{name}' has no compiled projection.");
            if (!program)
                throw new ArgumentNullException(nameof(program));
            if (!m_Projection.IsValid ||
                !string.Equals(m_Projection.ProgramId, program.ProgramId, StringComparison.Ordinal) ||
                !string.Equals(m_Projection.ProgramHash, program.ProgramHash, StringComparison.Ordinal) ||
                !string.Equals(m_Projection.SourceRevision, program.SourceRevision, StringComparison.Ordinal) ||
                !string.Equals(m_Projection.SemanticHash, program.SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(m_Projection.NumericProfileId, program.NumericProfileId, StringComparison.Ordinal) ||
                m_Projection.TargetAbiVersion != program.TargetAbiVersion)
                throw new InvalidOperationException("Character Presentation Projection does not match the compiled Program metadata.");
            return m_Projection;
        }

        public void SetCompiledProjection(CharacterPresentationProjection projection)
        {
            if (projection == null || !projection.IsValid)
                throw new ArgumentException("Compiled Character Presentation Projection is invalid.", nameof(projection));
            m_Projection = projection;
        }
#endif
    }
}
