using System.Collections.Generic;
using System.Text;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateMachineValidationResult
    {
        readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
                errors.Add(error);
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
    }
}
