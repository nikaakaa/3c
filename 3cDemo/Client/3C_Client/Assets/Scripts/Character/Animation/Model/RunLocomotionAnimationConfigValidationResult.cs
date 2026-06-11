using System.Collections.Generic;

namespace ThirdPersonAnimation
{
    public sealed class RunLocomotionAnimationConfigValidationResult
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
            return string.Join("\n", errors);
        }
    }
}
