using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationSemanticReader
    {
        readonly CharacterGameplaySemanticIr m_SemanticIr;

        public CharacterPresentationSemanticReader(ValidatedSemanticIrArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            m_SemanticIr = artifact.SemanticIr ?? throw new ArgumentException("Semantic IR artifact has no payload.", nameof(artifact));
            Contract = new CharacterPresentationSemanticContract(
                artifact.Header.ProgramId,
                artifact.Header.SourceRevision,
                artifact.Header.SemanticHash,
                m_SemanticIr.Producers);
        }

        public CharacterPresentationSemanticContract Contract { get; }
        public IReadOnlyList<ProgramProducer> Producers => m_SemanticIr.Producers;

        public CharacterPresentationProducerKind? ResolveKind(ProgramProducer producer, List<string> errors)
        {
            bool animation = false;
            bool camera = false;
            bool cue = false;
            for (int i = 0; i < m_SemanticIr.References.Count; i++)
            {
                ProgramReference reference = m_SemanticIr.References[i];
                if (reference.Kind != ProgramReferenceKind.Producer ||
                    reference.TargetIndex != producer.Index || !reference.HasSourceOperation)
                    continue;
                switch (m_SemanticIr.Operations[reference.SourceOperation.Value].Code)
                {
                    case SimulationOperationCode.TimelineAnimation:
                        animation = true;
                        break;
                    case SimulationOperationCode.TimelineCameraState:
                    case SimulationOperationCode.TimelineCameraCue:
                    case SimulationOperationCode.TimelineCameraResponse:
                    case SimulationOperationCode.CameraStateRequest:
                    case SimulationOperationCode.CameraCue:
                    case SimulationOperationCode.CameraResponse:
                    case SimulationOperationCode.CameraTarget:
                        camera = true;
                        break;
                    case SimulationOperationCode.TimelineCue:
                        cue = true;
                        break;
                }
            }
            int count = (animation ? 1 : 0) + (camera ? 1 : 0) + (cue ? 1 : 0);
            if (count == 0 && string.Equals(producer.LayerId, "Cue", StringComparison.Ordinal))
                return CharacterPresentationProducerKind.Cue;
            if (count != 1)
            {
                errors?.Add($"Presentation producer '{producer.Identity}' has no unique semantic producer kind.");
                return null;
            }
            return animation
                ? CharacterPresentationProducerKind.Animation
                : camera
                    ? CharacterPresentationProducerKind.Camera
                    : CharacterPresentationProducerKind.Cue;
        }

        public ProgramSourceMapEntry ResolveSource(ProgramProducer producer, List<string> errors)
        {
            ProgramSourceMapEntry source = null;
            int count = 0;
            for (int i = 0; i < m_SemanticIr.SourceMap.Count; i++)
            {
                ProgramSourceMapEntry candidate = m_SemanticIr.SourceMap[i];
                if (candidate.TargetKind != ProgramSourceTargetKind.Producer || candidate.TargetIndex != producer.Index)
                    continue;
                source = candidate;
                count++;
            }
            if (count == 1)
                return source;
            errors?.Add($"Presentation producer '{producer.Identity}' requires exactly one semantic source-map entry, found {count}.");
            return null;
        }

        public SemanticOperation RequireProducerOperation(ProgramProducer producer)
        {
            SemanticOperation result = null;
            for (int i = 0; i < m_SemanticIr.References.Count; i++)
            {
                ProgramReference reference = m_SemanticIr.References[i];
                if (reference.Kind != ProgramReferenceKind.Producer ||
                    reference.TargetIndex != producer.Index || !reference.HasSourceOperation)
                    continue;
                SemanticOperation candidate = m_SemanticIr.Operations[reference.SourceOperation.Value];
                if (!CameraProgramOperationSchema.IsCameraOperation(candidate.Code))
                    continue;
                if (result != null)
                    throw new InvalidOperationException("multiple Camera operations reference the same producer");
                result = candidate;
            }
            if (result == null)
                throw new InvalidOperationException("semantic Camera operation is missing");
            CameraProgramOperationSchema.Validate(result, m_SemanticIr.Literals);
            return result;
        }

        public int RequireInt32(SemanticOperation operation, string field)
        {
            SemanticLiteral literal = RequireLiteral(operation, field);
            if (literal.Kind != SemanticLiteralKind.Int32)
                throw new InvalidOperationException($"field '{field}' is not Int32");
            return literal.Int32;
        }

        public float RequireScalar(SemanticOperation operation, string field)
        {
            SemanticLiteral literal = RequireLiteral(operation, field);
            if (literal.Kind != SemanticLiteralKind.Number)
                throw new InvalidOperationException($"field '{field}' is not Number");
            double value = literal.X;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < float.MinValue || value > float.MaxValue)
                throw new InvalidOperationException(
                    $"field '{field}' literal '{literal.Identity}' value '{value:R}' is outside the finite presentation Float range");
            return (float)value;
        }

        public string RequireString(SemanticOperation operation, string field)
        {
            SemanticLiteral literal = RequireLiteral(operation, field);
            if (literal.Kind != SemanticLiteralKind.String)
                throw new InvalidOperationException($"field '{field}' is not String");
            return literal.Text;
        }

        SemanticLiteral RequireLiteral(SemanticOperation operation, string field)
        {
            SemanticLiteral result = null;
            string suffix = "/constant/" + field;
            for (int i = 0; i < operation.LiteralReferences.Count; i++)
            {
                int index = operation.LiteralReferences[i];
                if (index < 0 || index >= m_SemanticIr.Literals.Count)
                    throw new InvalidOperationException($"field '{field}' has an invalid literal index");
                SemanticLiteral candidate = m_SemanticIr.Literals[index];
                if (!candidate.Identity.EndsWith(suffix, StringComparison.Ordinal))
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"field '{field}' is duplicated");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException($"field '{field}' is missing");
        }
    }
}
