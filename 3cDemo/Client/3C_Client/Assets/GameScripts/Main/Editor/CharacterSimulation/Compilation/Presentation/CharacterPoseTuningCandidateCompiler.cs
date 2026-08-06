using System;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPoseTuningCandidateCompiler
    {
        public static CharacterPoseTuningParameterBlock CompileBlock(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock source,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            layout.RequireValid();
            source.RequireValid(layout);
            if (entry.Interaction != CharacterPoseTuningInteractionPolicy.TunableDefault)
                throw new InvalidOperationException(
                    $"Pose tuning field '{entry.FieldId}' is not a tunable default.");
            if (entry.ValueKind != value.Kind)
                throw new InvalidOperationException(
                    $"Pose tuning field '{entry.FieldId}' received a mismatched value kind.");
            if (!layout.Entries.Contains(entry))
                throw new InvalidOperationException(
                    $"Pose tuning field '{entry.FieldId}' is not part of the active layout.");

            CharacterPoseTuningParameterBlock block = source.Clone();
            switch (entry.ValueKind)
            {
                case CharacterPoseTuningValueKind.Float:
                    block.Floats[entry.ValueIndex] = value.FloatValue;
                    break;
                case CharacterPoseTuningValueKind.Integer:
                    block.Integers[entry.ValueIndex] = value.IntegerValue;
                    break;
                case CharacterPoseTuningValueKind.Boolean:
                    block.Booleans[entry.ValueIndex] = value.BooleanValue ? (byte)1 : (byte)0;
                    break;
                case CharacterPoseTuningValueKind.Enum:
                    block.Enums[entry.ValueIndex] = value.EnumValue;
                    break;
                default:
                    throw new InvalidOperationException("Pose tuning value kind is invalid.");
            }
            block.RequireValid(layout);
            return block;
        }

        public static CharacterPoseTuningCandidate CompileCandidate(
            CharacterPoseTuningTargetIdentity target,
            string sourceAuthoringRevision,
            string candidateRevision,
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock source,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (!string.Equals(target.LayoutHash, layout?.LayoutHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Pose tuning candidate target layout identity does not match the compiled layout.");
            return new CharacterPoseTuningCandidate(
                target,
                sourceAuthoringRevision,
                candidateRevision,
                CompileBlock(layout, source, entry, value));
        }
    }
}
