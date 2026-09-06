using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationProjectionValidationResult
    {
        internal CharacterPresentationProjectionValidationResult(
            IReadOnlyList<string> diagnostics)
        {
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        internal IReadOnlyList<string> Diagnostics { get; }
        internal bool IsValid => Diagnostics.Count == 0;
    }

    internal static class CharacterPresentationProjectionValidator
    {
        internal static CharacterPresentationProjectionValidationResult Validate(
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract)
        {
            var diagnostics = new List<string>();
            if (projection == null)
            {
                diagnostics.Add("Presentation Projection is missing.");
                return new CharacterPresentationProjectionValidationResult(diagnostics);
            }
            if (contract == null)
            {
                diagnostics.Add("Presentation Projection semantic contract is missing.");
                return new CharacterPresentationProjectionValidationResult(diagnostics);
            }
            if (!projection.IsValid)
                diagnostics.Add("Presentation Projection failed its sealed ABI or capacity validation.");
            if (!string.Equals(
                    projection.ProgramId,
                    contract.ProgramId.Value,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    projection.SourceRevision,
                    contract.SourceRevision.Value,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    projection.SemanticHash,
                    contract.SemanticHash.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    projection.ContractHash,
                    contract.ContractHash.ToString(),
                    StringComparison.Ordinal))
            {
                diagnostics.Add(
                    "Presentation Projection identity does not match the semantic contract.");
            }
            if (projection.Producers.Count != contract.Producers.Count)
            {
                diagnostics.Add(
                    "Presentation Projection producer count does not match the semantic contract.");
            }
            int producerCount = Math.Min(
                projection.Producers.Count,
                contract.Producers.Count);
            for (int i = 0; i < producerCount; i++)
            {
                CharacterPresentationProducerEntry projected = projection.Producers[i];
                CharacterPresentationProducerContractEntry expected = contract.Producers[i];
                if (projected == null ||
                    projected.ProgramProducerIndex != i ||
                    expected.Index != i ||
                    !string.Equals(
                        projected.ProgramProducerIdentity,
                        expected.Identity,
                        StringComparison.Ordinal) ||
                    projected.AnimationChannelId != expected.AnimationChannelId ||
                    !string.Equals(
                        projected.SourceIdentity,
                        expected.SourceIdentity,
                        StringComparison.Ordinal) ||
                    projected.ChannelKind != expected.ChannelKind)
                {
                    diagnostics.Add(
                        $"Presentation Projection producer identity at index {i} does not match the semantic contract.");
                }
            }
            if (diagnostics.Count == 0)
            {
                try
                {
                    projection.RequireContract(contract);
                }
                catch (Exception exception)
                {
                    diagnostics.Add(exception.Message);
                }
            }
            return new CharacterPresentationProjectionValidationResult(diagnostics);
        }
    }
}
