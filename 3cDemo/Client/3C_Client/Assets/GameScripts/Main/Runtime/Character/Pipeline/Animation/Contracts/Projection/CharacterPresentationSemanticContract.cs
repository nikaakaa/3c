using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterPresentationProducerContractEntry
    {
        public CharacterPresentationProducerContractEntry(ProgramProducer producer)
        {
            if (producer == null)
                throw new ArgumentNullException(nameof(producer));
            if (producer.Index < 0 ||
                string.IsNullOrWhiteSpace(producer.Identity) ||
                !producer.AnimationChannelId.IsValid ||
                string.IsNullOrWhiteSpace(producer.SourceIdentity) ||
                !Enum.IsDefined(typeof(ProgramOutputChannelKind), producer.ChannelKind))
            {
                throw new ArgumentException("Presentation contract producer fields are invalid.", nameof(producer));
            }
            Index = producer.Index;
            Identity = producer.Identity.Trim();
            AnimationChannelId = producer.AnimationChannelId;
            SourceIdentity = producer.SourceIdentity.Trim();
            ChannelKind = producer.ChannelKind;
        }

        public int Index { get; }
        public string Identity { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string SourceIdentity { get; }
        public ProgramOutputChannelKind ChannelKind { get; }
    }

    public sealed class CharacterPresentationSemanticContract
    {
        public const string SchemaVersion = "character-presentation-semantic-contract/v2";

        readonly ReadOnlyCollection<CharacterPresentationProducerContractEntry> m_Producers;

        public CharacterPresentationSemanticContract(
            ProgramId programId,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            IReadOnlyList<ProgramProducer> producers)
        {
            if (!programId.IsValid)
                throw new ArgumentException("Presentation contract ProgramId is invalid.", nameof(programId));
            if (string.IsNullOrEmpty(sourceRevision.Value))
                throw new ArgumentException("Presentation contract SourceRevision is invalid.", nameof(sourceRevision));
            if (!semanticHash.Value.IsValid)
                throw new ArgumentException("Presentation contract SemanticHash is invalid.", nameof(semanticHash));
            if (producers == null)
                throw new ArgumentNullException(nameof(producers));

            ProgramId = programId;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            var entries = new List<CharacterPresentationProducerContractEntry>(producers.Count);
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < producers.Count; i++)
            {
                ProgramProducer producer = producers[i] ?? throw new ArgumentException($"Presentation contract producer #{i} is null.", nameof(producers));
                if (producer.Index != i || !identities.Add(producer.Identity))
                    throw new ArgumentException($"Presentation contract producer #{i} is not canonical.", nameof(producers));
                entries.Add(new CharacterPresentationProducerContractEntry(producer));
            }
            m_Producers = entries.AsReadOnly();
            ContractHash = ComputeHash(programId, sourceRevision, semanticHash, entries);
        }

        public ProgramId ProgramId { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public StableHash ContractHash { get; }
        public IReadOnlyList<CharacterPresentationProducerContractEntry> Producers => m_Producers;

        static StableHash ComputeHash(
            ProgramId programId,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            IReadOnlyList<CharacterPresentationProducerContractEntry> producers)
        {
            var values = new List<string>(5 + producers.Count * 5)
            {
                SchemaVersion,
                programId.Value,
                sourceRevision.Value,
                semanticHash.ToString(),
                producers.Count.ToString(CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < producers.Count; i++)
            {
                CharacterPresentationProducerContractEntry producer = producers[i];
                values.Add(producer.Index.ToString(CultureInfo.InvariantCulture));
                values.Add(producer.Identity);
                values.Add(producer.AnimationChannelId.Value);
                values.Add(producer.SourceIdentity);
                values.Add(((int)producer.ChannelKind).ToString(CultureInfo.InvariantCulture));
            }
            return StableHash.Compute(values.ToArray());
        }
    }
}
