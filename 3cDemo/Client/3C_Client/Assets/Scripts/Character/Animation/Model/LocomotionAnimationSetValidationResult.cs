using System.Collections.Generic;

namespace ThirdPersonAnimation
{
    public sealed class LocomotionAnimationSetValidationResult
    {
        readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }
    }
}
