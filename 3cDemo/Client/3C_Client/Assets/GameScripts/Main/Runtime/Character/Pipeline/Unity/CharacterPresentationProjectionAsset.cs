using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class CharacterPresentationProjectionAsset : ScriptableObject
    {
        [SerializeField] CharacterPresentationProjection m_Projection;

        public string ProgramId => m_Projection?.ProgramId ?? string.Empty;
        public string SourceRevision => m_Projection?.SourceRevision ?? string.Empty;
        public string ProjectionRevision => m_Projection?.ProjectionRevision ?? string.Empty;
        public string SemanticHash => m_Projection?.SemanticHash ?? string.Empty;
        public string ContractHash => m_Projection?.ContractHash ?? string.Empty;

        public CharacterPresentationProjection Load(CharacterPresentationSemanticContract contract)
        {
            if (m_Projection == null)
                throw new InvalidOperationException($"Character Presentation Projection asset '{name}' has no compiled projection.");
            m_Projection.RequireContract(contract);
            m_Projection.RequirePosePayload();
            return m_Projection;
        }

#if UNITY_EDITOR
        public void SetCompiledProjection(CharacterPresentationProjection projection)
        {
            if (projection == null || !projection.IsValid)
                throw new ArgumentException("Compiled Character Presentation Projection is invalid.", nameof(projection));
            m_Projection = projection;
        }
#endif
    }
}
