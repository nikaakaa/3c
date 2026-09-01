using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class NetworkTestCandidateIdentity
    {
        static readonly Regex LabelPattern = new Regex(
            "^[a-z0-9]+(?:-[a-z0-9]+)*$",
            RegexOptions.CultureInvariant);

        NetworkTestCandidateIdentity(
            string label,
            string candidateId,
            string sourceCommit,
            string sourceTreeHash)
        {
            Label = label;
            CandidateId = candidateId;
            SourceCommit = sourceCommit;
            SourceTreeHash = sourceTreeHash;
        }

        public string Label { get; }
        public string CandidateId { get; }
        public string SourceCommit { get; }
        public string SourceTreeHash { get; }

        public static NetworkTestCandidateIdentity Capture(
            string repositoryRoot,
            string label,
            NetworkTestExternalProcessExecutor processes)
        {
            string normalizedLabel = RequireLabel(label);
            string root = RequireGit(processes, "rev-parse --show-toplevel", repositoryRoot);
            if (!string.Equals(
                    Path.GetFullPath(root),
                    Path.GetFullPath(repositoryRoot),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Network Test Candidate repository root does not match the active Git worktree.");
            }

            string status = RequireGit(processes, "status --porcelain=v1 --untracked-files=all", repositoryRoot);
            if (!string.IsNullOrWhiteSpace(status))
                throw new InvalidOperationException($"Network Test Candidate requires a clean Git worktree.\n{status}");

            string commit = RequireHash(RequireGit(processes, "rev-parse HEAD", repositoryRoot), "commit");
            string tree = RequireHash(RequireGit(processes, "rev-parse HEAD^{tree}", repositoryRoot), "tree");
            return new NetworkTestCandidateIdentity(
                normalizedLabel,
                $"{normalizedLabel}-{commit.Substring(0, 12)}",
                commit,
                tree);
        }

        public void RequireSame(NetworkTestCandidateIdentity other)
        {
            if (other == null ||
                !string.Equals(Label, other.Label, StringComparison.Ordinal) ||
                !string.Equals(CandidateId, other.CandidateId, StringComparison.Ordinal) ||
                !string.Equals(SourceCommit, other.SourceCommit, StringComparison.Ordinal) ||
                !string.Equals(SourceTreeHash, other.SourceTreeHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Network Test Candidate Git identity changed during Build preparation.");
            }
        }

        static string RequireLabel(string value)
        {
            string label = value?.Trim() ?? string.Empty;
            if (label.Length is < 3 or > 48 || !LabelPattern.IsMatch(label))
                throw new ArgumentException("CandidateLabel必须是3到48字符的小写kebab-case。", nameof(value));
            return label;
        }

        static string RequireGit(
            NetworkTestExternalProcessExecutor processes,
            string arguments,
            string repositoryRoot) =>
            processes.Execute("git", arguments, repositoryRoot)
                .RequireSuccess("network-test-candidate-source")
                .Trim();

        static string RequireHash(string value, string name)
        {
            if (value.Length != 40 || !Regex.IsMatch(value, "^[0-9a-f]{40}$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException($"Network Test Candidate Git {name} hash is invalid.");
            return value;
        }
    }
}
