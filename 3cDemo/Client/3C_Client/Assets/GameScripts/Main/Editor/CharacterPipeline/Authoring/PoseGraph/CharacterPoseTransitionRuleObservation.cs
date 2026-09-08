using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using TreeDesigner.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseTransitionRuleObservation
    {
        internal static void Project(CharacterPoseTransitionRuleDocument document, CharacterPoseProgramImage plan,
            string callSite, in AnimationPresentationRuntimeSnapshot snapshot, bool prospective,
            IDictionary<string, GraphAuthoringRuntimeTraceProjection> nodes, IDictionary<string, string> ports, ISet<string> readEdges)
        {
            if (plan == null || callSite == null) return;
            var owner = CharacterPoseGraphAssetMutationOwner.ResolveStateMachineOwner(document.Asset, document.Machine.StateMachineId);
            var callers = plan.SourceMap.Where(source => source.GraphId == owner.Item1.Value &&
                    source.AuthorNodeId.Equals(owner.Item2) && source.CallSite == callSite)
                .Select(source => source.NodeId).ToHashSet();
            var machines = plan.StateMachines.Where(machine => callers.Contains(machine.NodeId) &&
                machine.Transitions.Any(transition => transition.TransitionId.Equals(document.TransitionId) &&
                    transition.Rule.GraphId.Equals(document.Rule.GraphId) && transition.Rule.ContentRevision == document.ContentRevision))
                .Select(machine => machine.NodeId).ToHashSet();
            if (machines.Count == 0) return;
            var rows = new Dictionary<PoseTransitionRuleOperationId, PoseTransitionRuleEvaluationSnapshot>();
            for (int i = 0; i < snapshot.StateMachineRuleEvaluations.Count; i++)
            {
                PoseTransitionRuleEvaluationSnapshot row = snapshot.StateMachineRuleEvaluations[i];
                if (machines.Contains(row.StateMachineNodeId) && row.StateMachineId.Equals(document.Machine.StateMachineId) &&
                    row.TransitionId.Equals(document.TransitionId) && row.Prospective == prospective)
                    rows[row.OperationId] = row;
            }
            foreach (CharacterPoseTransitionRuleOperation operation in document.Rule.Operations)
            {
                string id = operation.OperationId.Value;
                if (!rows.TryGetValue(operation.OperationId, out PoseTransitionRuleEvaluationSnapshot row))
                {
                    nodes[id] = new GraphAuthoringRuntimeTraceProjection(new GraphAuthoringElementId(id), "未采集", string.Empty, document.ContentRevision);
                    continue;
                }
                string value = Format(row);
                nodes[id] = new GraphAuthoringRuntimeTraceProjection(new GraphAuthoringElementId(id), "已求值",
                    value + " · 帧 " + snapshot.CompletionIdentity, document.ContentRevision);
                ports[id + "\0result"] = value;
                if (operation.InputA.IsValid) ports[id + "\0input-a"] = "本次未读取";
                if (operation.InputB.IsValid) ports[id + "\0input-b"] = "本次未读取";
                AddRead(row.ReadInputA, "input-a");
                AddRead(row.ReadInputB, "input-b");

                void AddRead(PoseTransitionRuleOperationId inputId, string portId)
                {
                    if (!inputId.IsValid) return;
                    readEdges.Add(CharacterPoseTransitionRuleDocument.EdgeId(operation.OperationId, portId).Value);
                    ports[id + "\0" + portId] = rows.TryGetValue(inputId, out PoseTransitionRuleEvaluationSnapshot input)
                        ? Format(input) : "已读取 · 值未采集";
                }
            }
        }

        static string Format(in PoseTransitionRuleEvaluationSnapshot row) => row.ValueKind switch
        {
            PoseTransitionRuleValueKind.Bool => row.BoolValue ? "True" : "False",
            PoseTransitionRuleValueKind.Float => row.FloatValue.ToString("G6", CultureInfo.InvariantCulture),
            PoseTransitionRuleValueKind.Enum => row.EnumValue.ToString(CultureInfo.InvariantCulture),
            PoseTransitionRuleValueKind.Identity => row.IdentityValue,
            _ => throw new InvalidOperationException("转换条件结果类型无效。")
        };
    }
}
