using System;

namespace ThirdPersonSimulation
{
    public sealed class CharacterGameplayEffectRuntimeBinding
    {
        public const int ContractSemanticVersion = 1;
        public const int CatalogFormatVersion = 1;
        public const string CodecIdentity = "character-gameplay-effect-runtime-binding/v1";

        readonly byte[] m_CatalogBytes;

        public CharacterGameplayEffectRuntimeBinding(
            string sourceIdentity,
            StableHash contentRevision,
            int semanticVersion,
            byte[] catalogBytes)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            if (!contentRevision.IsValid)
                throw new ArgumentException("Character Gameplay Effect content revision is required.", nameof(contentRevision));
            if (semanticVersion != ContractSemanticVersion)
                throw new ArgumentOutOfRangeException(nameof(semanticVersion), "Character Gameplay Effect semantic version is unsupported.");
            if (catalogBytes == null || catalogBytes.Length == 0)
                throw new ArgumentException("Character Gameplay Effect catalog bytes are required.", nameof(catalogBytes));
            ContentRevision = contentRevision;
            SemanticVersion = semanticVersion;
            m_CatalogBytes = (byte[])catalogBytes.Clone();
            BindingHash = StableHash.Compute(
                CodecIdentity,
                SourceIdentity,
                ContentRevision.ToString(),
                SemanticVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                SimulationCanonicalPayloadHash.Compute(m_CatalogBytes).ToString());
        }

        public string SourceIdentity { get; }
        public StableHash ContentRevision { get; }
        public int SemanticVersion { get; }
        public StableHash BindingHash { get; }
        public byte[] CatalogBytes => (byte[])m_CatalogBytes.Clone();
    }

    public static class CharacterGameplayEffectRuntimeBindingCodec
    {
        public static byte[] Write(CharacterGameplayEffectRuntimeBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            using var writer = new CanonicalWriter();
            writer.WriteString(CharacterGameplayEffectRuntimeBinding.CodecIdentity);
            writer.WriteString(binding.SourceIdentity);
            writer.WriteString(binding.ContentRevision.ToString());
            writer.WriteInt32(binding.SemanticVersion);
            writer.WriteBytes(binding.CatalogBytes);
            return writer.ToArray();
        }

        public static CharacterGameplayEffectRuntimeBinding Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (!string.Equals(reader.ReadString(), CharacterGameplayEffectRuntimeBinding.CodecIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException("Character Gameplay Effect runtime binding codec identity is unsupported.");
            var binding = new CharacterGameplayEffectRuntimeBinding(
                reader.ReadString(),
                new StableHash(reader.ReadString()),
                reader.ReadInt32(),
                reader.ReadBytes());
            reader.RequireComplete();
            return binding;
        }
    }
}
