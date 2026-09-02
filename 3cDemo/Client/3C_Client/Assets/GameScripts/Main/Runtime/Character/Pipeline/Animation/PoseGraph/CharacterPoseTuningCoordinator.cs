using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseTuningCoordinator
    {
        readonly CharacterPoseProgramRuntime m_Program;
        readonly CharacterPoseSourceModule m_Source;
        readonly CharacterPoseConstraintRuntime m_Constraints;
        CharacterPoseTuningSnapshot m_Committed;

        internal CharacterPoseTuningCoordinator(
            CharacterPoseProgramRuntime program,
            CharacterPoseSourceModule source,
            CharacterPoseConstraintRuntime constraints,
            ulong initialGeneration)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_Constraints = constraints ??
                throw new ArgumentNullException(nameof(constraints));
            m_Committed = Capture(initialGeneration);
        }

        internal CharacterPoseTuningSnapshot Committed => m_Committed;

        internal string Apply(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong candidateGeneration,
            bool resetOwnerState)
        {
            if (layout == null || block == null)
                return "Pose tuning payload is missing.";
            string sourceError = m_Source.PrepareTuningCandidate(
                layout,
                block,
                candidateGeneration);
            if (!string.IsNullOrEmpty(sourceError))
                return sourceError;
            string programError = m_Program.PrepareTuningCandidate(
                layout,
                block,
                candidateGeneration);
            if (!string.IsNullOrEmpty(programError))
            {
                m_Source.DiscardTuningCandidate();
                return programError;
            }
            string constraintError = m_Constraints.PrepareTuningCandidate(
                layout,
                block,
                candidateGeneration,
                resetOwnerState);
            if (!string.IsNullOrEmpty(constraintError))
            {
                m_Program.DiscardTuningCandidate();
                m_Source.DiscardTuningCandidate();
                return constraintError;
            }
            m_Program.CommitTuningCandidate(candidateGeneration);
            m_Source.CommitTuningCandidate(candidateGeneration);
            m_Constraints.CommitTuningCandidate(candidateGeneration);
            m_Committed = Capture(candidateGeneration);
            return string.Empty;
        }

        CharacterPoseTuningSnapshot Capture(ulong generation)
        {
            CharacterPoseProgramTuningView program =
                m_Program.RequireTuning(generation);
            CharacterPoseSourceTuningView source =
                m_Source.RequireTuning(generation);
            CharacterPoseConstraintTuningView constraint =
                m_Constraints.RequireTuning(generation);
            return new CharacterPoseTuningSnapshot(
                in program,
                in source,
                in constraint);
        }
    }
}
