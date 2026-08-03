using System;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAuthoringOperationException : Exception
    {
        public AgentAuthoringOperationException(
            string code,
            string path,
            string message,
            string suggestion = "")
            : base(message)
        {
            Code = code ?? string.Empty;
            Path = path ?? string.Empty;
            Suggestion = suggestion ?? string.Empty;
        }

        public string Code { get; }
        public string Path { get; }
        public string Suggestion { get; }
    }

    public enum AgentAuthoringAction
    {
        CheckoutDocument,
        RebaseDocument,
        DryRunDocument,
        ApplyDocument,
        Validate
    }

    [Serializable]
    public sealed class AgentAuthoringRequest
    {
        public AgentAuthoringAction action;
        public string domain;
        public string rootAssetPath;
        public string expectedDocumentHash;
        public bool confirmRebase;
    }

    [Serializable]
    public sealed class AgentAuthoringResponse
    {
        public string action;
        public string domain;
        public string rootAssetPath;
        public string rootIdentity;
        public bool success;
        public bool applied;
        public bool saved;
        public string errorCode;
        public string errorMessage;
        public string packagePath;
        public string syncState;
        public string sourceRevision;
        public string editableHash;
        public string contextHash;
        public string documentHash;
        public string planHash;
        public AgentCompileReport report;
    }

    public static class AgentAuthoringActionUtility
    {
        public static string ToProtocolValue(AgentAuthoringAction action)
        {
            switch (action)
            {
                case AgentAuthoringAction.CheckoutDocument:
                    return "checkout_document";
                case AgentAuthoringAction.RebaseDocument:
                    return "rebase_document";
                case AgentAuthoringAction.DryRunDocument:
                    return "dry_run_document";
                case AgentAuthoringAction.ApplyDocument:
                    return "apply_document";
                case AgentAuthoringAction.Validate:
                    return "validate";
                default:
                    return string.Empty;
            }
        }
    }
}
