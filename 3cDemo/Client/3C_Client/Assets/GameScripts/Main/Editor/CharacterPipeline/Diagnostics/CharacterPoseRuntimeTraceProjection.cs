using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics.Editor;
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

        public CharacterPoseRuntimeTraceProjection(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPresentationProjectionAsset projection)
        {
            m_Asset = asset;
            m_Projection = projection;
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

            var operations =
                new Dictionary<PoseNodeId, AnimationPoseOperationSnapshot>();
            for (int i = 0; i < snapshot.Operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation =
                    snapshot.Operations[i];
                if (string.Equals(
                        operation.GraphId,
                        document.DocumentId,
                        StringComparison.Ordinal))
                {
                    operations[operation.NodeId] = operation;
                }
            }
            return document.Nodes.Select(node =>
            {
                if (!operations.TryGetValue(
                        new PoseNodeId(node.NodeId.Value),
                        out AnimationPoseOperationSnapshot operation))
                {
                    return new GraphAuthoringRuntimeTraceProjection(
                        node.NodeId,
                        "NotCompleted",
                        string.Empty,
                        snapshot.PoseGraphRevision);
                }
                string details = $"{operation.Code} · {operation.InvalidReason} · weight {operation.OutputWeight:0.###}";
                if (TryFindLinkedPoseEntry(
                        in snapshot,
                        in operation,
                        out AnimationLinkedPoseEntryRuntimeSnapshot entry,
                        out CharacterLinkedPoseRuntimeGroupSnapshot group))
                {
                    details +=
                        $" · Linked {entry.GroupId}/{entry.EntryId} call={entry.CallNodeId} " +
                        $"interface={entry.InterfaceId}@{entry.InterfaceSignature} " +
                        $"selector={group.SelectorId} selection={group.SelectionRevision} " +
                        $"implementation={group.ImplementationId}@{group.ImplementationRevision} " +
                        $"content={group.ImplementationContentHash} generation={group.Generation} " +
                        $"reset={group.StateReset} completed={entry.Completed} " +
                        $"sources={entry.SourceDemandCount} " +
                        $"operations={group.ActiveCapacity.OperationCount}/{group.MaximumCapacity.OperationCount}";
                }
                return new GraphAuthoringRuntimeTraceProjection(
                    node.NodeId,
                    operation.Availability.ToString(),
                    details,
                    snapshot.PoseGraphRevision);
            }).ToArray();
        }

        static bool TryFindLinkedPoseEntry(
            in AnimationPresentationRuntimeSnapshot snapshot,
            in AnimationPoseOperationSnapshot operation,
            out AnimationLinkedPoseEntryRuntimeSnapshot entry,
            out CharacterLinkedPoseRuntimeGroupSnapshot group)
        {
            entry = default;
            group = default;
            for (int i = 0; i < snapshot.LinkedPoseEntries.Count; i++)
            {
                AnimationLinkedPoseEntryRuntimeSnapshot candidate = snapshot.LinkedPoseEntries[i];
                bool isCall = candidate.CallNodeId == operation.NodeId;
                bool isFragmentOperation = operation.OperationIndex >= candidate.OperationStart &&
                                           operation.OperationIndex < candidate.OperationStart + candidate.OperationCount;
                if (!isCall && !isFragmentOperation)
                    continue;
                entry = candidate;
                for (int groupIndex = 0; groupIndex < snapshot.LinkedPoseGroups.Count; groupIndex++)
                {
                    CharacterLinkedPoseRuntimeGroupSnapshot candidateGroup = snapshot.LinkedPoseGroups[groupIndex];
                    if (candidateGroup.GroupId == candidate.GroupId)
                    {
                        group = candidateGroup;
                        return true;
                    }
                }
                return false;
            }
            return false;
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

            RuntimeDebugViewModel viewModel =
                RuntimeDebugSession.Shared.ViewModel;
            if (!viewModel.Attached ||
                !AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget target))
            {
                status =
                    "Unavailable: no attached Animation Presentation runtime target.";
                return false;
            }

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

            if (!string.Equals(
                    target.ProjectionRevision,
                    m_Projection.ProjectionRevision,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    snapshot.ProjectionRevision,
                    m_Projection.ProjectionRevision,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    snapshot.PoseGraphId,
                    m_Asset.Graph.GraphId.Value,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    snapshot.PoseGraphRevision,
                    m_Asset.Graph.ContentRevision,
                    StringComparison.Ordinal))
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
            RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
            if (!viewModel.Attached ||
                !AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out target))
            {
                status = "Unavailable: no attached Animation Presentation runtime target.";
                return false;
            }
            status = string.Empty;
            return true;
        }
    }
}
