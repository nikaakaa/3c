using System;
using ThirdPersonSimulation.DotRecast;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class NavigationSurfaceAsset : ScriptableObject
    {
        [SerializeField] byte[] m_CanonicalArtifact = Array.Empty<byte>();
        [SerializeField] string m_MapId = string.Empty;
        [SerializeField] string m_WorldRevision = string.Empty;
        [SerializeField] string m_GeometryHash = string.Empty;
        [SerializeField] string m_ContentHash = string.Empty;
        [SerializeField] string m_WorldConfigurationHash = string.Empty;
        [SerializeField] string m_DotRecastCommit = string.Empty;

        public string MapId => m_MapId;
        public string WorldRevision => m_WorldRevision;
        public string GeometryHash => m_GeometryHash;
        public string ContentHash => m_ContentHash;
        public string WorldConfigurationHash => m_WorldConfigurationHash;
        public string DotRecastCommit => m_DotRecastCommit;
        public int CanonicalByteLength => m_CanonicalArtifact?.Length ?? 0;

        public byte[] CopyCanonicalArtifact()
        {
            return m_CanonicalArtifact == null ? Array.Empty<byte>() : (byte[])m_CanonicalArtifact.Clone();
        }

        public NavigationSurfaceArtifact Load()
        {
            if (m_CanonicalArtifact == null || m_CanonicalArtifact.Length == 0)
                throw new InvalidOperationException($"Navigation Surface asset '{name}' has no canonical artifact.");
            NavigationSurfaceArtifact artifact = NavigationSurfaceArtifactCodec.Read(m_CanonicalArtifact);
            if (!string.Equals(artifact.MapId, m_MapId, StringComparison.Ordinal) ||
                !string.Equals(artifact.WorldRevision, m_WorldRevision, StringComparison.Ordinal) ||
                !string.Equals(artifact.GeometryHash.Value, m_GeometryHash, StringComparison.Ordinal) ||
                !string.Equals(artifact.ContentHash.Value, m_ContentHash, StringComparison.Ordinal) ||
                !string.Equals(artifact.WorldConfigurationHash.Value, m_WorldConfigurationHash, StringComparison.Ordinal) ||
                !string.Equals(DotRecastSourceIdentity.Commit, m_DotRecastCommit, StringComparison.Ordinal))
                throw new InvalidOperationException($"Navigation Surface asset '{name}' metadata does not match its canonical artifact.");
            return artifact;
        }

#if UNITY_EDITOR
        public void SetCanonicalArtifact(byte[] bytes)
        {
            NavigationSurfaceArtifact artifact = NavigationSurfaceArtifactCodec.Read(bytes);
            byte[] canonical = NavigationSurfaceArtifactCodec.Write(artifact);
            if (!BytesEqual(bytes, canonical))
                throw new InvalidOperationException("Navigation Surface artifact is not in canonical form.");
            m_CanonicalArtifact = (byte[])bytes.Clone();
            m_MapId = artifact.MapId;
            m_WorldRevision = artifact.WorldRevision;
            m_GeometryHash = artifact.GeometryHash.Value;
            m_ContentHash = artifact.ContentHash.Value;
            m_WorldConfigurationHash = artifact.WorldConfigurationHash.Value;
            m_DotRecastCommit = DotRecastSourceIdentity.Commit;
        }

        static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
#endif
    }
}
