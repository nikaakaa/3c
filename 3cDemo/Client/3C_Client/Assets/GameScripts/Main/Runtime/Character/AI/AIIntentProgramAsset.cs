using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.AI
{
    public sealed class AIIntentProgramAsset : ScriptableObject
    {
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_ProgramHash = string.Empty;
        [SerializeField] string m_LayoutHash = string.Empty;
        [SerializeField] string m_SourceRevision = string.Empty;
        [SerializeField] string m_CharacterProgramId = string.Empty;
        [SerializeField] string m_CharacterProgramHash = string.Empty;
        [SerializeField] string m_PerceptionSchemaHash = string.Empty;
        [SerializeField] byte[] m_CanonicalBytes = Array.Empty<byte>();

        public string ProgramId => m_ProgramId ?? string.Empty;
        public string ProgramHash => m_ProgramHash ?? string.Empty;
        public string LayoutHash => m_LayoutHash ?? string.Empty;
        public string SourceRevision => m_SourceRevision ?? string.Empty;
        public string CharacterProgramId => m_CharacterProgramId ?? string.Empty;
        public string CharacterProgramHash => m_CharacterProgramHash ?? string.Empty;
        public string PerceptionSchemaHash => m_PerceptionSchemaHash ?? string.Empty;
        public bool HasCanonicalArtifact => m_CanonicalBytes != null && m_CanonicalBytes.Length != 0;

        public AIIntentProgram Load()
        {
            if (m_CanonicalBytes == null || m_CanonicalBytes.Length == 0)
                throw new InvalidOperationException($"AI Intent Program asset '{name}' has no canonical artifact bytes.");
            AIIntentProgram program = AIIntentProgramCodec.ReadArtifact(m_CanonicalBytes);
            if (!string.Equals(program.ProgramId.Value, ProgramId, StringComparison.Ordinal) ||
                !string.Equals(program.ProgramHash.ToString(), ProgramHash, StringComparison.Ordinal) ||
                !string.Equals(program.LayoutHash.ToString(), LayoutHash, StringComparison.Ordinal) ||
                !string.Equals(program.SemanticIr.SourceRevision, SourceRevision, StringComparison.Ordinal) ||
                !string.Equals(program.CharacterProgramId.Value, CharacterProgramId, StringComparison.Ordinal) ||
                !string.Equals(program.CharacterProgramHash.ToString(), CharacterProgramHash, StringComparison.Ordinal) ||
                !string.Equals(program.PerceptionSchemaHash.ToString(), PerceptionSchemaHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"AI Intent Program asset '{name}' metadata does not match its canonical artifact.");
            }
            return program;
        }

#if UNITY_EDITOR
        public void SetProgram(AIIntentProgram program, byte[] canonicalBytes)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            byte[] exact = canonicalBytes ?? throw new ArgumentNullException(nameof(canonicalBytes));
            AIIntentProgram roundTrip = AIIntentProgramCodec.ReadArtifact(exact);
            if (!roundTrip.ProgramHash.Equals(program.ProgramHash) || !roundTrip.LayoutHash.Equals(program.LayoutHash))
                throw new InvalidOperationException("AI Intent Program artifact round trip changed its identity.");
            m_ProgramId = program.ProgramId.Value;
            m_ProgramHash = program.ProgramHash.ToString();
            m_LayoutHash = program.LayoutHash.ToString();
            m_SourceRevision = program.SemanticIr.SourceRevision;
            m_CharacterProgramId = program.CharacterProgramId.Value;
            m_CharacterProgramHash = program.CharacterProgramHash.ToString();
            m_PerceptionSchemaHash = program.PerceptionSchemaHash.ToString();
            m_CanonicalBytes = (byte[])exact.Clone();
        }
#endif
    }
}
