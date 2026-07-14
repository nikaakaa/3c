using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics.Editor
{
    public enum RuntimeDebugAttachmentState
    {
        Detached,
        Live,
        Frozen,
        CaptureHistory,
        Ended
    }

    public enum RuntimeDebugTargetMatch
    {
        Exact,
        SourceMissing,
        RevisionMismatch
    }

    public enum RuntimeDebugTargetResolutionStatus
    {
        Attached,
        Ended,
        NotPlaying,
        ExplicitHostUnregistered,
        ExplicitHostSourceMissing,
        ExplicitHostRevisionMismatch,
        SourceMissing,
        RevisionMismatch,
        NoExactTarget,
        MultipleExactTargets,
        InvalidSource
    }

    public readonly struct RuntimeDebugTargetRequest : IEquatable<RuntimeDebugTargetRequest>
    {
        public RuntimeDebugTargetRequest(RuntimeSourceElementKey source, string contentHash)
        {
            Source = source;
            ContentHash = contentHash ?? string.Empty;
        }

        public RuntimeSourceElementKey Source { get; }
        public string ContentHash { get; }
        public bool IsValid => Source.IsValid && !string.IsNullOrEmpty(ContentHash);

        public bool Equals(RuntimeDebugTargetRequest other)
        {
            return Source.Equals(other.Source) &&
                   string.Equals(ContentHash, other.ContentHash, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is RuntimeDebugTargetRequest other && Equals(other);
        public override int GetHashCode() => Source.GetHashCode() * 397 ^ (ContentHash?.GetHashCode() ?? 0);
    }

    public readonly struct RuntimeDebugSceneSelection
    {
        public RuntimeDebugSceneSelection(int hostInstanceId)
        {
            HostInstanceId = hostInstanceId;
        }

        public int HostInstanceId { get; }
        public bool HasExplicitHost => HostInstanceId != 0;
    }

    public static class RuntimeDebugSceneSelectionRegistry
    {
        static Func<RuntimeDebugSceneSelection> s_Resolver;

        public static void Register(Func<RuntimeDebugSceneSelection> resolver)
        {
            s_Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public static RuntimeDebugSceneSelection Resolve()
        {
            return s_Resolver != null ? s_Resolver() : default;
        }
    }

    public readonly struct RuntimeDebugTargetCandidate
    {
        public RuntimeDebugTargetCandidate(RuntimeDebugTargetInfo target, RuntimeDebugTargetMatch match)
        {
            Target = target;
            Match = match;
        }

        public RuntimeDebugTargetInfo Target { get; }
        public RuntimeDebugTargetMatch Match { get; }
        public bool IsExact => Match == RuntimeDebugTargetMatch.Exact;
    }

    public readonly struct RuntimeDebugTargetResolution
    {
        readonly IReadOnlyList<RuntimeDebugTargetCandidate> m_Candidates;

        public RuntimeDebugTargetResolution(
            RuntimeDebugTargetResolutionStatus status,
            IReadOnlyList<RuntimeDebugTargetCandidate> candidates = null)
        {
            Status = status;
            m_Candidates = candidates ?? Array.Empty<RuntimeDebugTargetCandidate>();
        }

        public RuntimeDebugTargetResolutionStatus Status { get; }
        public IReadOnlyList<RuntimeDebugTargetCandidate> Candidates => m_Candidates ?? Array.Empty<RuntimeDebugTargetCandidate>();
        public bool CanReadSnapshot => Status == RuntimeDebugTargetResolutionStatus.Attached || Status == RuntimeDebugTargetResolutionStatus.Ended;

        public string Message => Status switch
        {
            RuntimeDebugTargetResolutionStatus.Attached => string.Empty,
            RuntimeDebugTargetResolutionStatus.Ended => "Target ended. Showing frozen history.",
            RuntimeDebugTargetResolutionStatus.NotPlaying => "Enter Play Mode to inspect a runtime target.",
            RuntimeDebugTargetResolutionStatus.ExplicitHostUnregistered => "The selected CharacterPipelineHost is not registered.",
            RuntimeDebugTargetResolutionStatus.ExplicitHostSourceMissing => "The selected CharacterPipelineHost does not contain this authoring source.",
            RuntimeDebugTargetResolutionStatus.ExplicitHostRevisionMismatch => "The selected CharacterPipelineHost was built from a different source revision.",
            RuntimeDebugTargetResolutionStatus.SourceMissing => "The attached trace does not contain this authoring source.",
            RuntimeDebugTargetResolutionStatus.RevisionMismatch => "The attached trace was built from a different source revision.",
            RuntimeDebugTargetResolutionStatus.NoExactTarget => "No registered target exactly matches this authoring source.",
            RuntimeDebugTargetResolutionStatus.MultipleExactTargets => "Multiple registered targets match this authoring source. Choose a target.",
            RuntimeDebugTargetResolutionStatus.InvalidSource => "The current authoring source has no stable identity or content hash.",
            _ => string.Empty
        };
    }
}
