using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using TreeDesigner.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPoseRuntimeTraceProjection :
        IGraphAuthoringDomainDiagnostics
    {
        readonly CharacterPresentationPoseGraphAsset m_Asset;
        readonly CharacterPresentationProjectionAsset m_Projection;

        readonly Func<AnimationPresentationRuntimeTarget> m_GetTarget;
        readonly Func<CharacterPoseProgramImage> m_GetPlan;
        readonly Func<string> m_GetCallSite;
        readonly Func<AnimationPresentationRuntimeSnapshot, bool> m_MatchesRevision;

        public CharacterPoseRuntimeTraceProjection(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPresentationProjectionAsset projection,
            Func<AnimationPresentationRuntimeTarget> getTarget,
            Func<AnimationPresentationRuntimeSnapshot, bool> matchesRevision,
            Func<CharacterPoseProgramImage> getPlan,
            Func<string> getCallSite)
        {
            m_Asset = asset;
            m_Projection = projection;
            m_GetTarget = getTarget;
            m_GetPlan = getPlan;
            m_GetCallSite = getCallSite;
            m_MatchesRevision = matchesRevision;
        }

        public IReadOnlyList<GraphAuthoringDiagnosticProjection>
            GetDiagnostics(IGraphAuthoringDocumentProjection document) =>
            Array.Empty<GraphAuthoringDiagnosticProjection>();

        public IReadOnlyList<GraphAuthoringRuntimeTraceProjection>
            GetRuntimeTrace(IGraphAuthoringDocumentProjection document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (!TryGetSnapshot(out AnimationPresentationRuntimeSnapshot snapshot,
                    out string status))
            {
                return document.Nodes.Select(node =>
                        new GraphAuthoringRuntimeTraceProjection(
                            node.NodeId,
                            status,
                            string.Empty,
                            string.Empty))
                    .ToArray();
            }

            CharacterPoseProgramImage plan = m_GetPlan();
            string callSite = m_GetCallSite();
            if (plan == null || callSite == null)
                return document.Nodes.Select(node => new GraphAuthoringRuntimeTraceProjection(
                    node.NodeId, "等待调用位置", string.Empty, snapshot.PoseGraphRevision)).ToArray();
            var operations = new Dictionary<string, List<AnimationPoseOperationSnapshot>>(StringComparer.Ordinal);
            for (int i = 0; i < snapshot.Operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = snapshot.Operations[i];
                if (operation.OperationIndex < 0 || operation.OperationIndex >= plan.SourceMap.Count) continue;
                CharacterPresentationPoseSourceMapEntry source = plan.SourceMap[operation.OperationIndex];
                if (source.NodeId != operation.NodeId || source.GraphId != document.DocumentId ||
                    source.CallSite != callSite || operation.CallSite != callSite) continue;
                if (!operations.TryGetValue(source.AuthorNodeId.Value, out var values))
                    operations.Add(source.AuthorNodeId.Value, values = new List<AnimationPoseOperationSnapshot>());
                values.Add(operation);
            }
            return document.Nodes.Select(node =>
            {
                if (!operations.TryGetValue(node.NodeId.Value, out var values))
                {
                    bool mapped = plan.SourceMap.Any(source => source.GraphId == document.DocumentId &&
                        source.AuthorNodeId.Value == node.NodeId.Value && source.CallSite == callSite);
                    return new GraphAuthoringRuntimeTraceProjection(node.NodeId,
                        mapped ? "未采集运行操作" : "无独立运行操作", string.Empty, snapshot.PoseGraphRevision);
                }
                string details = string.Join("\n", values.Select(operation =>
                    $"#{operation.OperationIndex} {operation.Code} · {operation.InvalidReason} · 权重 {operation.OutputWeight:0.###} · 完成帧 {operation.CompletionIdentity}"));
                string state = string.Join(" / ", values.Select(operation => operation.CompletionIdentity != snapshot.CompletionIdentity
                    ? "等待完成" : operation.Availability switch
                    {
                        AnimationPoseAvailability.Pose => operation.OutputWeight > 0 ? "完成 · 有贡献" : "完成 · 无贡献",
                        AnimationPoseAvailability.NoPose => "完成 · 无姿势",
                        _ => "不可用"
                    }).Distinct());
                return new GraphAuthoringRuntimeTraceProjection(node.NodeId, state, details, snapshot.PoseGraphRevision);
            }).ToArray();
        }

        public bool TryGetSnapshot(
            out AnimationPresentationRuntimeSnapshot snapshot,
            out string status)
        {
            snapshot = default;
            if (!m_Asset || m_Asset.Graph == null || !m_Projection ||
                string.IsNullOrWhiteSpace(m_Projection.ProjectionRevision))
            {
                status =
                    "Unavailable: formal Pose Graph or Presentation Projection is missing.";
                return false;
            }

            if (!TryResolveRuntimeTarget(out AnimationPresentationRuntimeTarget target, out status))
                return false;

            try
            {
                if (!target.TryGetDebugView(
                        out AnimationPresentationDebugView debugView))
                {
                    status =
                        "Unavailable: runtime target has no completed frame snapshot.";
                    return false;
                }
                snapshot = debugView.PosePlan;
            }
            catch (InvalidOperationException)
            {
                status =
                    "Stale: runtime target Projection revision changed.";
                return false;
            }

            if (!m_MatchesRevision(snapshot))
            {
                snapshot = default;
                status =
                    "Stale: runtime Pose Graph or Projection revision does not match this document.";
                return false;
            }

            status = "Ready";
            return true;
        }

        public bool TryGetPosePlanStages(
            out CharacterPosePlanStageSnapshot snapshot,
            out string status)
        {
            snapshot = default;
            if (!TryResolveRuntimeTarget(out AnimationPresentationRuntimeTarget target, out status) ||
                !target.TryGetPosePlanStages(out snapshot))
            {
                if (string.IsNullOrEmpty(status))
                    status = "Unavailable: runtime target has no Pose stage snapshot.";
                return false;
            }
            status = "Ready";
            return true;
        }

        bool TryResolveRuntimeTarget(
            out AnimationPresentationRuntimeTarget target,
            out string status)
        {
            target = null;
            if (!m_Asset || m_Asset.Graph == null || !m_Projection ||
                string.IsNullOrWhiteSpace(m_Projection.ProjectionRevision))
            {
                status = "Unavailable: formal Pose Graph or Presentation Projection is missing.";
                return false;
            }
            target = m_GetTarget();
            if (target == null ||
                !AnimationPresentationRuntimeTargetRegistry.TryGet(target.RuntimeInstanceId, out AnimationPresentationRuntimeTarget current) ||
                !ReferenceEquals(target, current))
            {
                status = "等待运行角色";
                return false;
            }
            status = string.Empty;
            return true;
        }
    }
}
