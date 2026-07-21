using System;
using ThirdPersonSimulation.DeterministicKcc;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [CreateAssetMenu(fileName = "DeterministicCollisionWorld", menuName = "3C/Simulation/Deterministic Collision World")]
    public sealed class DeterministicCollisionWorldAsset : ScriptableObject
    {
        [SerializeField] string m_MapId;
        [SerializeField] string m_ContentHash;
        [SerializeField] byte[] m_CanonicalBytes = Array.Empty<byte>();

        public string MapId => m_MapId;
        public string ContentHash => m_ContentHash;

        public DeterministicCollisionWorldArtifact Load()
        {
            if (m_CanonicalBytes == null || m_CanonicalBytes.Length == 0)
                throw new InvalidOperationException($"Collision World Asset '{name}' has no canonical artifact.");
            DeterministicCollisionWorldArtifact artifact = DeterministicCollisionWorldCodec.Read((byte[])m_CanonicalBytes.Clone());
            if (!string.Equals(artifact.MapId, m_MapId, StringComparison.Ordinal) ||
                !string.Equals(artifact.ContentHash.Value, m_ContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Collision World Asset '{name}' metadata does not match its canonical artifact.");
            }
            return artifact;
        }

        public void Replace(DeterministicCollisionWorldArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            byte[] bytes = DeterministicCollisionWorldCodec.Write(artifact);
            DeterministicCollisionWorldArtifact canonical = DeterministicCollisionWorldCodec.Read(bytes);
            m_MapId = canonical.MapId;
            m_ContentHash = canonical.ContentHash.Value;
            m_CanonicalBytes = bytes;
        }
    }
}
