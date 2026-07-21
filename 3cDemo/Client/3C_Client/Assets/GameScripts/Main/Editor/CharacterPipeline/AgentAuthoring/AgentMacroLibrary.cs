using System;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMacroLibrary
    {
        public bool TryExpand(AgentControllerIntent intent, AgentGraphSnapshot snapshot, out AgentPatchIR patch, AgentCompileReport report)
        {
            patch = null;
            if (intent == null)
            {
                report?.Error("intent", "missing_intent", "AgentControllerIntent 缺失。");
                return false;
            }
            if (!string.Equals(intent.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal) ||
                snapshot == null || !string.Equals(snapshot.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                report?.Error("intent.schemaVersion", "unsupported_schema_version", $"Intent 与 Snapshot 必须使用 {AgentAuthoringSchema.Version}。");
                return false;
            }
            if (!AgentAuthoringSchema.IsDomain(intent.domain) ||
                !string.Equals(intent.domain, snapshot.domain, StringComparison.Ordinal) ||
                !string.Equals(intent.rootIdentity, snapshot.rootIdentity, StringComparison.Ordinal))
            {
                report?.Error("intent.domain", "intent_root_mismatch", "Intent 的 domain/rootIdentity 必须与 v15 Snapshot 完全一致。");
                return false;
            }
            report?.Error(
                "intent.macro",
                "macro_removed_use_typed_patch",
                "Agent v15 不展开带业务默认值的 controller macro。请提交显式 domain-aware AgentPatchIR。");
            return false;
        }
    }
}
