using System;
using System.Globalization;

namespace ThirdPerson.Development.Gm
{
    [Flags]
    public enum GmPermission
    {
        None = 0,
        Read = 1
    }

    public enum GmResultCode
    {
        Unspecified,
        Success,
        InvalidRequest,
        UnknownCommand,
        InvalidArguments,
        Unauthorized,
        TargetEnded,
        TargetUnavailable,
        TimedOut,
        VersionMismatch,
        ToolVersionMismatch,
        ExecutionFailed
    }

    public enum GmValueKind
    {
        Text,
        Boolean,
        SignedInteger,
        UnsignedInteger
    }

    [Serializable]
    public sealed class GmCommandArgument
    {
        public string name = string.Empty;
        public string description = string.Empty;
        public bool optional;
    }

    [Serializable]
    public sealed class GmCommandDefinition
    {
        public string id = string.Empty;
        public int version = 1;
        public string description = string.Empty;
        public string usage = string.Empty;
        public GmPermission permission = GmPermission.Read;
        public GmCommandArgument[] arguments = Array.Empty<GmCommandArgument>();
    }

    [Serializable]
    public sealed class GmCommandRequest
    {
        public string requestId = string.Empty;
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string serviceInstanceId = string.Empty;
        public string sessionId = string.Empty;
        public string commandId = string.Empty;
        public int commandVersion;
        public string[] arguments = Array.Empty<string>();
    }

    [Serializable]
    public sealed class GmResultField
    {
        public string name = string.Empty;
        public string label = string.Empty;
        public GmValueKind kind;
        public string value = string.Empty;

        public static GmResultField Text(string name, string label, string value) =>
            Create(name, label, GmValueKind.Text, value);

        public static GmResultField Boolean(string name, string label, bool value) =>
            Create(name, label, GmValueKind.Boolean, value ? "true" : "false");

        public static GmResultField Signed(string name, string label, long value) =>
            Create(name, label, GmValueKind.SignedInteger, value.ToString(CultureInfo.InvariantCulture));

        public static GmResultField Unsigned(string name, string label, ulong value) =>
            Create(name, label, GmValueKind.UnsignedInteger, value.ToString(CultureInfo.InvariantCulture));

        static GmResultField Create(string name, string label, GmValueKind kind, string value) =>
            new GmResultField { name = name, label = label, kind = kind, value = value ?? string.Empty };
    }

    [Serializable]
    public sealed class GmResultSection
    {
        public string title = string.Empty;
        public GmResultField[] fields = Array.Empty<GmResultField>();
    }

    [Serializable]
    public sealed class GmCommandResponse
    {
        public string requestId = string.Empty;
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string serviceInstanceId = string.Empty;
        public string sessionId = string.Empty;
        public GmResultCode code;
        public string completedAtUtc = string.Empty;
        public string message = string.Empty;
        public GmResultSection[] sections = Array.Empty<GmResultSection>();
    }

    [Serializable]
    public sealed class GmServiceDescription
    {
        public int protocolVersion;
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string serviceInstanceId = string.Empty;
        public string sessionId = string.Empty;
        public GmToolIdentity tool = new GmToolIdentity();
        public GmCommandDefinition[] commands = Array.Empty<GmCommandDefinition>();
    }
}
