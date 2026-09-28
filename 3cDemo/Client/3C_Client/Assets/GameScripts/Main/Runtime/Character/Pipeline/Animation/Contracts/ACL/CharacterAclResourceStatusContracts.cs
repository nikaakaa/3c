using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterAclDataBlockKind : byte
    {
        Transform = 1,
        Scalar = 2,
        DatabaseHeader = 3,
        BulkMedium = 4,
        BulkLow = 5
    }

    public enum CharacterAclResourceReadiness : byte
    {
        Pending = 1,
        Ready = 2,
        Invalid = 3
    }

    public enum CharacterAclResourceFailureCode : byte
    {
        None = 0,
        SchemaMismatch = 1,
        BackendMismatch = 2,
        MissingRequiredBlock = 3,
        BlockLengthMismatch = 4,
        BlockHashMismatch = 5,
        FormatMismatch = 6,
        RigMismatch = 7,
        BindingMismatch = 8,
        ReferenceMismatch = 9,
        ScalarBindingInvalid = 10,
        PlatformUnsupported = 11,
        NativeUnavailable = 12,
        ContextInvalid = 13,
        CapacityExceeded = 14,
        SourceIdentityMismatch = 15
    }

    public readonly struct CharacterAclResourceReadinessResult
    {
        public CharacterAclResourceReadinessResult(
            CharacterAclResourceReadiness readiness,
            CharacterAclResourceFailureCode failureCode,
            string message)
        {
            Readiness = readiness;
            FailureCode = failureCode;
            Message = message ?? string.Empty;
            if ((byte)readiness < (byte)CharacterAclResourceReadiness.Pending ||
                (byte)readiness > (byte)CharacterAclResourceReadiness.Invalid ||
                (byte)failureCode > (byte)CharacterAclResourceFailureCode.SourceIdentityMismatch ||
                readiness == CharacterAclResourceReadiness.Invalid && failureCode == CharacterAclResourceFailureCode.None ||
                readiness != CharacterAclResourceReadiness.Invalid && failureCode != CharacterAclResourceFailureCode.None)
            {
                throw new ArgumentException("ACL resource readiness result is invalid.");
            }
        }

        public CharacterAclResourceReadiness Readiness { get; }
        public CharacterAclResourceFailureCode FailureCode { get; }
        public string Message { get; }
        public bool IsReady => Readiness == CharacterAclResourceReadiness.Ready;
        public bool IsPending => Readiness == CharacterAclResourceReadiness.Pending;
        public bool IsInvalid => Readiness == CharacterAclResourceReadiness.Invalid;

        public static CharacterAclResourceReadinessResult Ready() =>
            new CharacterAclResourceReadinessResult(
                CharacterAclResourceReadiness.Ready,
                CharacterAclResourceFailureCode.None,
                string.Empty);

        public static CharacterAclResourceReadinessResult Pending() =>
            new CharacterAclResourceReadinessResult(
                CharacterAclResourceReadiness.Pending,
                CharacterAclResourceFailureCode.None,
                string.Empty);

        public static CharacterAclResourceReadinessResult Invalid(
            CharacterAclResourceFailureCode code,
            string message) =>
            new CharacterAclResourceReadinessResult(
                CharacterAclResourceReadiness.Invalid,
                code,
                message);
    }
}
