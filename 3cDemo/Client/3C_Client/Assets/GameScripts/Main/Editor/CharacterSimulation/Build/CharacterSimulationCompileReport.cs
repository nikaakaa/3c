using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum CharacterSimulationCompileStage
    {
        AuthoringDiscovery,
        SemanticEmission,
        ArtifactValidation,
        TargetLowering,
        PresentationProjection
    }

    public enum CharacterSimulationCompileSeverity
    {
        Information,
        Warning,
        Error
    }

    public sealed class CharacterSimulationCompileMessage
    {
        public CharacterSimulationCompileMessage(CharacterSimulationCompileStage stage, CharacterSimulationCompileSeverity severity, string code, string sourceIdentity, string message)
        {
            Stage = stage;
            Severity = severity;
            Code = code ?? string.Empty;
            SourceIdentity = sourceIdentity ?? string.Empty;
            Message = message ?? string.Empty;
        }
        public CharacterSimulationCompileStage Stage { get; }
        public CharacterSimulationCompileSeverity Severity { get; }
        public string Code { get; }
        public string SourceIdentity { get; }
        public string Message { get; }
        public override string ToString() => $"{Stage} {Severity} {Code} {SourceIdentity}: {Message}";
    }

    public sealed class CharacterSimulationCompileReport
    {
        readonly List<CharacterSimulationCompileMessage> m_Messages = new List<CharacterSimulationCompileMessage>();
        readonly ReadOnlyCollection<CharacterSimulationCompileMessage> m_ReadOnlyMessages;

        public CharacterSimulationCompileReport()
        {
            m_ReadOnlyMessages = m_Messages.AsReadOnly();
        }

        public IReadOnlyList<CharacterSimulationCompileMessage> Messages => m_ReadOnlyMessages;
        public bool IsValid
        {
            get
            {
                for (int i = 0; i < m_Messages.Count; i++)
                {
                    if (m_Messages[i].Severity == CharacterSimulationCompileSeverity.Error)
                        return false;
                }
                return true;
            }
        }

        public void Information(string code, string sourceIdentity, string message) => EmissionInformation(code, sourceIdentity, message);
        public void Warning(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.SemanticEmission, CharacterSimulationCompileSeverity.Warning, code, sourceIdentity, message);
        public void Error(string code, string sourceIdentity, string message) => EmissionError(code, sourceIdentity, message);
        public void DiscoveryInformation(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.AuthoringDiscovery, CharacterSimulationCompileSeverity.Information, code, sourceIdentity, message);
        public void DiscoveryError(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.AuthoringDiscovery, CharacterSimulationCompileSeverity.Error, code, sourceIdentity, message);
        public void EmissionInformation(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.SemanticEmission, CharacterSimulationCompileSeverity.Information, code, sourceIdentity, message);
        public void EmissionError(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.SemanticEmission, CharacterSimulationCompileSeverity.Error, code, sourceIdentity, message);
        public void TargetInformation(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.TargetLowering, CharacterSimulationCompileSeverity.Information, code, sourceIdentity, message);
        public void TargetError(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.TargetLowering, CharacterSimulationCompileSeverity.Error, code, sourceIdentity, message);
        public void PresentationError(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.PresentationProjection, CharacterSimulationCompileSeverity.Error, code, sourceIdentity, message);
        public void ArtifactError(string code, string sourceIdentity, string message) => Add(CharacterSimulationCompileStage.ArtifactValidation, CharacterSimulationCompileSeverity.Error, code, sourceIdentity, message);

        void Add(CharacterSimulationCompileStage stage, CharacterSimulationCompileSeverity severity, string code, string sourceIdentity, string message)
        {
            m_Messages.Add(new CharacterSimulationCompileMessage(stage, severity, code, sourceIdentity, message));
        }
    }

    public sealed class CharacterSemanticFrontendResult
    {
        public CharacterSemanticFrontendResult(
            ThirdPersonSimulation.ValidatedSemanticIrArtifact artifact,
            CharacterAuthoringCompilationModel compilationModel,
            CharacterSimulationCompileReport report)
        {
            Artifact = artifact;
            CompilationModel = compilationModel;
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }
        public ThirdPersonSimulation.ValidatedSemanticIrArtifact Artifact { get; }
        public CharacterAuthoringCompilationModel CompilationModel { get; }
        public CharacterSimulationCompileReport Report { get; }
        public bool IsValid => Artifact != null && CompilationModel != null && Report.IsValid;

        public static CharacterSemanticFrontendResult Failed(CharacterSimulationCompileReport report)
        {
            return new CharacterSemanticFrontendResult(null, null, report);
        }
    }

    public sealed class CharacterSemanticIrArtifactDescriptor
    {
        public CharacterSemanticIrArtifactDescriptor(string path, ThirdPersonSimulation.CharacterGameplaySemanticIrArtifactHeader header)
        {
            Path = path ?? string.Empty;
            Header = header ?? throw new ArgumentNullException(nameof(header));
        }

        public string Path { get; }
        public ThirdPersonSimulation.CharacterGameplaySemanticIrArtifactHeader Header { get; }
    }

    public sealed class CharacterSimulationBuildResult
    {
        public CharacterSimulationBuildResult(
            CharacterSemanticIrArtifactDescriptor artifact,
            IReadOnlyList<CharacterSimulationTargetBuildProduct> targetProducts,
            ThirdPersonCharacter.Pipeline.Animation.CharacterPresentationProjection presentationProjection,
            CharacterSimulationCompileReport report)
        {
            Artifact = artifact;
            TargetProducts = targetProducts ?? Array.Empty<CharacterSimulationTargetBuildProduct>();
            PresentationProjection = presentationProjection;
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public CharacterSemanticIrArtifactDescriptor Artifact { get; }
        public IReadOnlyList<CharacterSimulationTargetBuildProduct> TargetProducts { get; }
        public ThirdPersonCharacter.Pipeline.Animation.CharacterPresentationProjection PresentationProjection { get; }
        public CharacterSimulationCompileReport Report { get; }
        public bool IsValid => Artifact != null && TargetProducts.Count > 0 && PresentationProjection != null && PresentationProjection.IsValid && Report.IsValid;
    }
}
