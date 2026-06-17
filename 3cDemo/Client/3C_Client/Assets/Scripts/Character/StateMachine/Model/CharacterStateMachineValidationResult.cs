using System.Collections.Generic;
using System.Text;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateMachineValidationResult
    {
        readonly List<string> errors = new List<string>();
        readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool HasErrors => errors.Count > 0;
        public bool HasWarnings => warnings.Count > 0;

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
                errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
                warnings.Add(warning);
        }

        public string DescribeErrors()
        {
            if (!HasErrors)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < errors.Count; i++)
                builder.AppendLine(errors[i]);
            return builder.ToString();
        }

        public string DescribeWarnings()
        {
            if (!HasWarnings)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < warnings.Count; i++)
                builder.AppendLine(warnings[i]);
            return builder.ToString();
        }
    }
}
