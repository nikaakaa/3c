using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public interface IAgentConditionTermEmitter
    {
        AgentConditionTermKind Kind { get; }
        bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path);
        AgentConditionTermOutput Emit(
            AgentPatchCompileSession session,
            ConditionRuleGraph graph,
            AgentConditionTermCommand term,
            int index,
            string path);
    }

    public readonly struct AgentConditionTermOutput
    {
        public AgentConditionTermOutput(BaseNode node, PropertyPort port)
        {
            Node = node;
            Port = port;
        }

        public BaseNode Node { get; }
        public PropertyPort Port { get; }
        public bool IsValid => Node != null && Port != null;
    }

    public sealed class AgentConditionRuleBuilder
    {
        readonly Dictionary<AgentConditionTermKind, IAgentConditionTermEmitter> m_Emitters =
            new Dictionary<AgentConditionTermKind, IAgentConditionTermEmitter>();

        public AgentConditionRuleBuilder()
        {
            Register(new MovementCompareEmitter(AgentConditionTermKind.MoveStop, "StopThreshold", CompareNode.CompareType.Less));
            Register(new MovementCompareEmitter(AgentConditionTermKind.MoveHas, "StopThreshold", CompareNode.CompareType.Greater));
            Register(new MovementCompareEmitter(AgentConditionTermKind.MoveRun, "RunThreshold", CompareNode.CompareType.GreaterEqual));
            Register(new WalkRangeEmitter());
            Register(new FacingAngleEmitter());
            Register(new BlackboardBoolEmitter());
            Register(new StateRootCompletedEmitter());
            Register(new ActionRequestEmitter());
            Register(new ActionWindowActiveEmitter());
            Register(new CanActivateActionEmitter());
            Register(new AITargetDistanceCompareBlackboardEmitter());
        }

        public bool Preflight(
            AgentPatchCompileSession session,
            IReadOnlyList<AgentConditionGroupCommand> groups,
            string path)
        {
            bool valid = true;
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                IReadOnlyList<AgentConditionTermCommand> terms = groups[groupIndex].Terms;
                for (int termIndex = 0; termIndex < terms.Count; termIndex++)
                {
                    AgentConditionTermCommand term = terms[termIndex];
                    string termPath = $"{path}.conditionGroups[{groupIndex}].terms[{termIndex}]";
                    if (!m_Emitters.TryGetValue(term.Kind, out IAgentConditionTermEmitter emitter))
                    {
                        session.Report.Error(termPath, "condition_term_unsupported", $"Condition term 没有正式 emitter：{term.Kind}");
                        valid = false;
                        continue;
                    }
                    valid &= emitter.Preflight(session, term, termPath);
                }
            }
            return valid;
        }

        public bool BuildTransitionRule(
            AgentPatchCompileSession session,
            BaseEdge targetEdge,
            AgentEnsureConditionRuleCommand command)
        {
            return BuildRule(session, targetEdge, command.Groups, command.Path);
        }

        public bool BuildFlowRule(
            AgentPatchCompileSession session,
            BaseEdge targetEdge,
            IReadOnlyList<AgentConditionGroupCommand> groups,
            string path)
        {
            return BuildRule(session, targetEdge, groups, path);
        }

        bool BuildRule(
            AgentPatchCompileSession session,
            BaseEdge targetEdge,
            IReadOnlyList<AgentConditionGroupCommand> groups,
            string path)
        {
            ConditionRuleGraph target = targetEdge?.ConditionRuleGraph;
            if (!target)
            {
                session.Report.Error(path, "condition_rule_graph_missing", "目标 ConditionRuleGraph 缺失。");
                return false;
            }

            Clear(target);
            var groupOutputs = new List<AgentConditionTermOutput>();
            int layoutIndex = 0;
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                IReadOnlyList<AgentConditionTermCommand> terms = groups[groupIndex].Terms;
                var termOutputs = new List<AgentConditionTermOutput>();
                for (int termIndex = 0; termIndex < terms.Count; termIndex++)
                {
                    AgentConditionTermCommand term = terms[termIndex];
                    string termPath = $"{path}.conditionGroups[{groupIndex}].terms[{termIndex}]";
                    if (!m_Emitters.TryGetValue(term.Kind, out IAgentConditionTermEmitter emitter))
                    {
                        session.Report.Error(termPath, "condition_term_unsupported", $"Condition term 没有正式 emitter：{term.Kind}");
                        continue;
                    }
                    AgentConditionTermOutput output = emitter.Emit(session, target, term, layoutIndex++, termPath);
                    if (output.IsValid)
                        termOutputs.Add(output);
                }

                AgentConditionTermOutput groupOutput = Combine(
                    session,
                    target,
                    termOutputs,
                    true,
                    $"Group {groupIndex + 1} And",
                    new Vector2(40f, groupIndex * 180f),
                    path);
                if (groupOutput.IsValid)
                    groupOutputs.Add(groupOutput);
            }

            AgentConditionTermOutput combined = Combine(
                session,
                target,
                groupOutputs,
                false,
                "Condition Groups Or",
                new Vector2(260f, 40f),
                path);
            return ConnectResult(session, target, combined, path);
        }

        public ConditionRuleGraph BuildActionExitRule(
            AgentPatchCompileSession session,
            string graphName,
            ActionContextSlot actionContext,
            AgentActionExitRuleKind ruleKind,
            IReadOnlyList<AgentConditionGroupCommand> cancelConditionGroups,
            string path)
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph(graphName);
            var outputs = new List<AgentConditionTermOutput>();

            ActionContextActiveInfoNode activeNode = graph.CreateNode(typeof(ActionContextActiveInfoNode)) as ActionContextActiveInfoNode;
            activeNode.DisplayName = "Action Context Active";
            activeNode.Position = new Vector2(-520f, -120f);
            activeNode.ConfigureAuthoring(actionContext);
            outputs.Add(Output(activeNode, "m_Output"));

            var causeOutputs = new List<AgentConditionTermOutput>();
            if (ruleKind == AgentActionExitRuleKind.Cancel || ruleKind == AgentActionExitRuleKind.Complete)
            {
                causeOutputs.Add(CreateExitCause(graph, StateExitCause.StateTransition, "State Transition Exit", new Vector2(-520f, -20f)));
            }
            else if (ruleKind == AgentActionExitRuleKind.Interrupt)
            {
                causeOutputs.Add(CreateExitCause(graph, StateExitCause.TreeSelfAbort, "Tree Self Abort", new Vector2(-520f, -20f)));
                causeOutputs.Add(CreateExitCause(graph, StateExitCause.TreeLowerPriorityAbort, "Tree Lower Priority Abort", new Vector2(-520f, 60f)));
            }
            else
            {
                causeOutputs.Add(CreateExitCause(graph, StateExitCause.TreeParentStop, "Tree Parent Stop", new Vector2(-520f, -20f)));
            }
            outputs.Add(Combine(session, graph, causeOutputs, false, "Exit Causes Or", new Vector2(-280f, -20f), path));

            if (ruleKind == AgentActionExitRuleKind.Cancel)
            {
                var groupOutputs = new List<AgentConditionTermOutput>();
                int layoutIndex = 2;
                for (int groupIndex = 0; groupIndex < cancelConditionGroups.Count; groupIndex++)
                {
                    IReadOnlyList<AgentConditionTermCommand> terms = cancelConditionGroups[groupIndex].Terms;
                    var termOutputs = new List<AgentConditionTermOutput>();
                    for (int termIndex = 0; termIndex < terms.Count; termIndex++)
                    {
                        AgentConditionTermCommand term = terms[termIndex];
                        string termPath = $"{path}.cancelConditionGroups[{groupIndex}].terms[{termIndex}]";
                        if (!m_Emitters.TryGetValue(term.Kind, out IAgentConditionTermEmitter emitter))
                        {
                            session.Report.Error(termPath, "condition_term_unsupported", $"Condition term 没有正式 emitter：{term.Kind}");
                            continue;
                        }
                        AgentConditionTermOutput output = emitter.Emit(session, graph, term, layoutIndex++, termPath);
                        if (output.IsValid)
                            termOutputs.Add(output);
                    }

                    AgentConditionTermOutput groupOutput = Combine(
                        session,
                        graph,
                        termOutputs,
                        true,
                        $"Cancel Group {groupIndex + 1} And",
                        new Vector2(-120f, 120f + groupIndex * 160f),
                        path);
                    if (groupOutput.IsValid)
                        groupOutputs.Add(groupOutput);
                }

                AgentConditionTermOutput guards = Combine(
                    session,
                    graph,
                    groupOutputs,
                    false,
                    "Cancel Groups Or",
                    new Vector2(120f, 120f),
                    path);
                if (guards.IsValid)
                    outputs.Add(guards);
            }

            AgentConditionTermOutput combined = Combine(
                session,
                graph,
                outputs,
                true,
                "Action Exit And",
                new Vector2(-40f, 20f),
                path);
            ConnectResult(session, graph, combined, path);
            return graph;
        }

        static AgentConditionTermOutput CreateExitCause(
            ConditionRuleGraph graph,
            StateExitCause cause,
            string displayName,
            Vector2 position)
        {
            StateExitCauseInfoNode node = graph.CreateNode(typeof(StateExitCauseInfoNode)) as StateExitCauseInfoNode;
            node.DisplayName = displayName;
            node.Position = position;
            node.ConfigureAuthoring(cause);
            return Output(node, "m_Output");
        }

        void Register(IAgentConditionTermEmitter emitter)
        {
            if (m_Emitters.ContainsKey(emitter.Kind))
                throw new InvalidOperationException($"Duplicate Condition term emitter: {emitter.Kind}");
            m_Emitters.Add(emitter.Kind, emitter);
        }

        static AgentConditionTermOutput Combine(
            AgentPatchCompileSession session,
            ConditionRuleGraph graph,
            IReadOnlyList<AgentConditionTermOutput> outputs,
            bool useAnd,
            string displayName,
            Vector2 position,
            string path)
        {
            if (outputs == null || outputs.Count == 0)
                return default;
            if (outputs.Count == 1)
                return outputs[0];

            AgentConditionTermOutput combined = outputs[0];
            for (int i = 1; i < outputs.Count; i++)
            {
                BaseNode operationNode = graph.CreateNode(useAnd ? typeof(AndNode) : typeof(OrNode));
                operationNode.DisplayName = displayName;
                operationNode.Position = position + new Vector2((i - 1) * 180f, i * 35f);
                Link(session, graph, combined, operationNode, "m_Input1", path);
                Link(session, graph, outputs[i], operationNode, "m_Input2", path);
                combined = Output(operationNode, "m_Output");
            }
            return combined;
        }

        static bool ConnectResult(
            AgentPatchCompileSession session,
            ConditionRuleGraph graph,
            AgentConditionTermOutput output,
            string path)
        {
            ConditionRuleResultNode resultNode = graph.ResultNode;
            if (!output.IsValid || !resultNode || !resultNode.PropertyPortMap.TryGetValue("m_Result", out PropertyPort resultPort))
            {
                session.Report.Error(path, "condition_rule_result_missing", "组合 ConditionRule 缺少 Rule Result。");
                return false;
            }
            graph.LinkProperty(output.Node, resultNode, output.Port, resultPort);
            return true;
        }

        internal static void Link(
            AgentPatchCompileSession session,
            ConditionRuleGraph graph,
            AgentConditionTermOutput output,
            BaseNode target,
            string targetPortId,
            string path)
        {
            if (!output.IsValid || target == null || !target.PropertyPortMap.TryGetValue(targetPortId, out PropertyPort targetPort))
            {
                session.Report.Error(path, "condition_rule_port_missing", $"ConditionRule property port 无法解析：{targetPortId}");
                return;
            }
            graph.LinkProperty(output.Node, target, output.Port, targetPort);
        }

        internal static AgentConditionTermOutput Output(BaseNode node, string portId)
        {
            return node != null && node.PropertyPortMap.TryGetValue(portId, out PropertyPort port)
                ? new AgentConditionTermOutput(node, port)
                : default;
        }

        internal static void ConfigureFloatInputs(CompareNode compare)
        {
            compare.SetPropertyPort("m_InputValue1", typeof(FloatPropertyPort), PortDirection.Input);
            compare.SetPropertyPort("m_InputValue2", typeof(FloatPropertyPort), PortDirection.Input);
        }

        static void Clear(ConditionRuleGraph graph)
        {
            foreach (PropertyEdge edge in graph.PropertyEdges.ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseNode node in graph.Nodes.Where(value => value is not ConditionRuleResultNode).ToList())
                graph.DeleteNode(node);
        }
    }

    public enum AgentActionExitRuleKind
    {
        Cancel,
        Interrupt,
        Abort,
        Complete
    }

    sealed class MovementCompareEmitter : IAgentConditionTermEmitter
    {
        readonly string m_ThresholdKey;
        readonly CompareNode.CompareType m_CompareType;

        public MovementCompareEmitter(AgentConditionTermKind kind, string thresholdKey, CompareNode.CompareType compareType)
        {
            Kind = kind;
            m_ThresholdKey = thresholdKey;
            m_CompareType = compareType;
        }

        public AgentConditionTermKind Kind { get; }

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            bool valid = true;
            if (!session.Resolver.TryResolveInputValue("MoveAxis", out _))
            {
                session.Report.Error(path, "input_not_found", "Condition term 需要当前 InputProfile 的 MoveAxis。");
                valid = false;
            }
            valid &= session.TryResolveBlackboardDeclaration(m_ThresholdKey, typeof(float), path, out _, out _);
            return valid;
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            return EmitCompare(session, graph, index, m_ThresholdKey, m_CompareType, path);
        }

        internal static AgentConditionTermOutput EmitCompare(
            AgentPatchCompileSession session,
            ConditionRuleGraph graph,
            int index,
            string thresholdKey,
            CompareNode.CompareType compareType,
            string path)
        {
            CharacterInputValueInfoNode inputNode = graph.CreateNode(typeof(CharacterInputVector2MagnitudeInfoNode)) as CharacterInputValueInfoNode;
            inputNode.DisplayName = "MoveAxis Magnitude";
            inputNode.Position = new Vector2(-520f, index * 100f);
            inputNode.BindInputValue("MoveAxis");

            PipelineBlackboardFloatInfoNode thresholdNode = graph.CreateNode(typeof(PipelineBlackboardFloatInfoNode)) as PipelineBlackboardFloatInfoNode;
            thresholdNode.DisplayName = thresholdKey;
            thresholdNode.Position = new Vector2(-520f, index * 100f + 45f);
            if (!session.TryResolveBlackboardDeclaration(thresholdKey, typeof(float), path, out _, out BaseExposedProperty declaration))
                return default;
            thresholdNode.ConfigureAuthoring(declaration);

            CompareNode compareNode = graph.CreateNode(typeof(CompareNode)) as CompareNode;
            compareNode.DisplayName = "Compare";
            compareNode.Position = new Vector2(-240f, index * 100f + 20f);
            compareNode.ConfigureAuthoring(compareType);
            AgentConditionRuleBuilder.ConfigureFloatInputs(compareNode);

            graph.LinkProperty(inputNode, compareNode, inputNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue1"]);
            graph.LinkProperty(thresholdNode, compareNode, thresholdNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue2"]);
            return AgentConditionRuleBuilder.Output(compareNode, "m_Result");
        }
    }

    sealed class WalkRangeEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.MoveWalk;

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            bool valid = true;
            if (!session.Resolver.TryResolveInputValue("MoveAxis", out _))
            {
                session.Report.Error(path, "input_not_found", "Condition term 需要当前 InputProfile 的 MoveAxis。");
                valid = false;
            }
            valid &= session.TryResolveBlackboardDeclaration("WalkThreshold", typeof(float), path, out _, out _);
            valid &= session.TryResolveBlackboardDeclaration("RunThreshold", typeof(float), path, out _, out _);
            return valid;
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            AgentConditionTermOutput lower = MovementCompareEmitter.EmitCompare(session, graph, index * 2, "WalkThreshold", CompareNode.CompareType.GreaterEqual, path);
            AgentConditionTermOutput upper = MovementCompareEmitter.EmitCompare(session, graph, index * 2 + 1, "RunThreshold", CompareNode.CompareType.Less, path);
            AndNode walk = graph.CreateNode(typeof(AndNode)) as AndNode;
            walk.DisplayName = "Walk Range";
            walk.Position = new Vector2(-40f, index * 120f);
            AgentConditionRuleBuilder.Link(session, graph, lower, walk, "m_Input1", path);
            AgentConditionRuleBuilder.Link(session, graph, upper, walk, "m_Input2", path);
            return AgentConditionRuleBuilder.Output(walk, "m_Output");
        }
    }

    sealed class FacingAngleEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.TurnFacingAngle;

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            bool valid = true;
            if (!session.Resolver.TryResolveInputValue("MoveAxis", out _))
            {
                session.Report.Error(path, "input_not_found", "Condition term 需要当前 InputProfile 的 MoveAxis。");
                valid = false;
            }
            valid &= session.TryResolveBlackboardDeclaration("MovingTurnAngleThreshold", typeof(float), path, out _, out _);
            return valid;
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            CharacterInputVector2InfoNode inputNode = graph.CreateNode(typeof(CharacterInputVector2InfoNode)) as CharacterInputVector2InfoNode;
            inputNode.DisplayName = "MoveAxis Input Value";
            inputNode.Position = new Vector2(-620f, index * 120f);
            inputNode.BindInputValue("MoveAxis");

            CharacterMoveFacingAngleInfoNode angleNode = graph.CreateNode(typeof(CharacterMoveFacingAngleInfoNode)) as CharacterMoveFacingAngleInfoNode;
            angleNode.DisplayName = "Move Facing Angle";
            angleNode.Position = new Vector2(-400f, index * 120f);

            PipelineBlackboardFloatInfoNode thresholdNode = graph.CreateNode(typeof(PipelineBlackboardFloatInfoNode)) as PipelineBlackboardFloatInfoNode;
            thresholdNode.DisplayName = "MovingTurnAngleThreshold";
            thresholdNode.Position = new Vector2(-400f, index * 120f + 60f);
            if (!session.TryResolveBlackboardDeclaration("MovingTurnAngleThreshold", typeof(float), path, out _, out BaseExposedProperty declaration))
                return default;
            thresholdNode.ConfigureAuthoring(declaration);

            CompareNode compareNode = graph.CreateNode(typeof(CompareNode)) as CompareNode;
            compareNode.DisplayName = "Facing Angle Threshold";
            compareNode.Position = new Vector2(-140f, index * 120f + 25f);
            compareNode.ConfigureAuthoring(CompareNode.CompareType.GreaterEqual);
            AgentConditionRuleBuilder.ConfigureFloatInputs(compareNode);

            graph.LinkProperty(inputNode, angleNode, inputNode.PropertyPortMap["m_Output"], angleNode.PropertyPortMap["m_MoveInput"]);
            graph.LinkProperty(angleNode, compareNode, angleNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue1"]);
            graph.LinkProperty(thresholdNode, compareNode, thresholdNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue2"]);
            return AgentConditionRuleBuilder.Output(compareNode, "m_Result");
        }
    }

    sealed class BlackboardBoolEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.BlackboardBool;

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            return session.TryResolveBlackboardDeclaration(term.BlackboardKey, typeof(bool), path, out _, out _);
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            PipelineBlackboardBoolInfoNode valueNode = graph.CreateNode(typeof(PipelineBlackboardBoolInfoNode)) as PipelineBlackboardBoolInfoNode;
            valueNode.DisplayName = term.BlackboardKey;
            valueNode.Position = new Vector2(-360f, index * 100f);
            if (!session.TryResolveBlackboardDeclaration(term.BlackboardKey, typeof(bool), path, out _, out BaseExposedProperty declaration))
                return default;
            valueNode.ConfigureAuthoring(declaration);
            AgentConditionTermOutput output = AgentConditionRuleBuilder.Output(valueNode, "m_Output");
            if (!term.Negate)
                return output;

            NotNode notNode = graph.CreateNode(typeof(NotNode)) as NotNode;
            notNode.DisplayName = $"Not {term.BlackboardKey}";
            notNode.Position = new Vector2(-120f, index * 100f);
            AgentConditionRuleBuilder.Link(session, graph, output, notNode, "m_Input", path);
            return AgentConditionRuleBuilder.Output(notNode, "m_Output");
        }
    }

    sealed class StateRootCompletedEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.StateRootCompleted;
        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path) => true;

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            StateRootCompletedNode node = graph.CreateNode(typeof(StateRootCompletedNode)) as StateRootCompletedNode;
            node.DisplayName = "State Root Completed";
            node.Position = new Vector2(-360f, index * 100f);
            return AgentConditionRuleBuilder.Output(node, "m_Output");
        }
    }

    sealed class ActionRequestEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.ActionRequest;

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            if (session.Resolver.TryResolveActionRequest(term.Request, out _))
                return true;
            session.Report.Error(path, "request_not_found", $"Action request 未在当前 InputProfile 中找到：{term.Request}");
            return false;
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            if (!session.Resolver.TryResolveActionRequest(term.Request, out _))
            {
                session.Report.Error(path, "request_not_found", $"Action request 未在当前 InputProfile 中找到：{term.Request}");
                return default;
            }
            CharacterActionRequestInfoNode node = graph.CreateNode(typeof(CharacterActionRequestInfoNode)) as CharacterActionRequestInfoNode;
            node.DisplayName = $"Has {term.Request} Request";
            node.Position = new Vector2(-360f, index * 100f);
            node.BindActionRequest(term.Request);
            return AgentConditionRuleBuilder.Output(node, "m_Output");
        }
    }

    sealed class ActionWindowActiveEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.ActionWindowActive;

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            if (!string.IsNullOrWhiteSpace(term.WindowType))
                return true;
            session.Report.Error(path, "window_type_missing", "ActionWindowActive condition 缺少 WindowType。");
            return false;
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            ActionWindowActiveInfoNode node = graph.CreateNode(typeof(ActionWindowActiveInfoNode)) as ActionWindowActiveInfoNode;
            node.DisplayName = $"Window {term.WindowType}";
            node.Position = new Vector2(-360f, index * 100f);
            node.ConfigureAuthoring(term.WindowType);
            return ApplyNegate(session, graph, AgentConditionRuleBuilder.Output(node, "m_Output"), term.Negate, node.DisplayName, index, path);
        }

        internal static AgentConditionTermOutput ApplyNegate(
            AgentPatchCompileSession session,
            ConditionRuleGraph graph,
            AgentConditionTermOutput output,
            bool negate,
            string displayName,
            int index,
            string path)
        {
            if (!negate)
                return output;
            NotNode notNode = graph.CreateNode(typeof(NotNode)) as NotNode;
            notNode.DisplayName = $"Not {displayName}";
            notNode.Position = new Vector2(-120f, index * 100f);
            AgentConditionRuleBuilder.Link(session, graph, output, notNode, "m_Input", path);
            return AgentConditionRuleBuilder.Output(notNode, "m_Output");
        }
    }

    sealed class CanActivateActionEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.CanActivateAction;

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            if (!session.Resolver.TryResolveActionProfile(term.ActionProfile.LogicalId, out ActionProfile profile))
            {
                session.Report.Error(path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{term.ActionProfile.LogicalId}");
                return false;
            }
            if (session.ResolveEffectiveTargetRequirement(profile) == ThirdPersonSimulation.ActionTargetRequirement.None)
            {
                if (string.IsNullOrEmpty(term.TargetSnapshotBlackboardKey))
                    return true;
                session.Report.Error(path, "action_target_snapshot_forbidden", $"ActionProfile '{profile.ActionId}' 使用 None，不得配置 target declaration。");
                return false;
            }
            return session.TryResolveBlackboardDeclaration(
                term.TargetSnapshotBlackboardKey,
                typeof(ActionTargetSnapshot),
                path,
                out _,
                out _);
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            if (!session.Resolver.TryResolveActionProfile(term.ActionProfile.LogicalId, out ActionProfile profile))
            {
                session.Report.Error(path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{term.ActionProfile.LogicalId}");
                return default;
            }
            CanActivateActionInfoNode node = graph.CreateNode(typeof(CanActivateActionInfoNode)) as CanActivateActionInfoNode;
            node.DisplayName = $"Can Activate {profile.ActionId}";
            node.Position = new Vector2(-360f, index * 100f);
            PipelineBlackboardVariableReference target = default;
            if (profile.TargetRequirement != ThirdPersonSimulation.ActionTargetRequirement.None)
            {
                if (!session.TryResolveBlackboardDeclaration(
                        term.TargetSnapshotBlackboardKey,
                        typeof(ActionTargetSnapshot),
                        path,
                        out _,
                        out BaseExposedProperty declaration))
                    return default;
                target = declaration.CreateBlackboardReference();
            }
            node.ConfigureAuthoring(profile, target);
            return ActionWindowActiveEmitter.ApplyNegate(session, graph, AgentConditionRuleBuilder.Output(node, "m_Output"), term.Negate, node.DisplayName, index, path);
        }
    }

    sealed class AITargetDistanceCompareBlackboardEmitter : IAgentConditionTermEmitter
    {
        public AgentConditionTermKind Kind => AgentConditionTermKind.AITargetDistanceCompareBlackboard;

        public bool Preflight(AgentPatchCompileSession session, AgentConditionTermCommand term, string path)
        {
            if (session.Domain != AgentAuthoringSchema.AIControllerDomain)
            {
                session.Report.Error(path, "ai_condition_wrong_domain", "AI target distance condition 只能用于 AIController domain。");
                return false;
            }
            return session.TryResolveBlackboardDeclaration(term.BlackboardKey, typeof(float), path, out _, out _);
        }

        public AgentConditionTermOutput Emit(AgentPatchCompileSession session, ConditionRuleGraph graph, AgentConditionTermCommand term, int index, string path)
        {
            if (!session.TryResolveBlackboardDeclaration(term.BlackboardKey, typeof(float), path, out _, out BaseExposedProperty declaration))
                return default;

            ReadTargetDistanceNode distance = graph.CreateNode(typeof(ReadTargetDistanceNode)) as ReadTargetDistanceNode;
            distance.DisplayName = "Target Distance";
            distance.Position = new Vector2(-520f, index * 100f);

            ReadAIMemoryNode threshold = graph.CreateNode(typeof(ReadAIMemoryNode)) as ReadAIMemoryNode;
            threshold.DisplayName = term.BlackboardKey;
            threshold.Position = new Vector2(-520f, index * 100f + 50f);
            threshold.ConfigureAuthoring(declaration, AIMemoryValueKind.Scalar);
            threshold.RebindReadOnlyViewReferences(graph);

            CompareNode compare = graph.CreateNode(typeof(CompareNode)) as CompareNode;
            compare.DisplayName = "Compare Target Distance";
            compare.Position = new Vector2(-240f, index * 100f + 20f);
            compare.ConfigureAuthoring(term.CompareType);
            AgentConditionRuleBuilder.ConfigureFloatInputs(compare);

            graph.LinkProperty(distance, compare, distance.PropertyPortMap["m_Distance"], compare.PropertyPortMap["m_InputValue1"]);
            graph.LinkProperty(threshold, compare, threshold.PropertyPortMap["m_Value"], compare.PropertyPortMap["m_InputValue2"]);
            AgentConditionTermOutput output = AgentConditionRuleBuilder.Output(compare, "m_Result");
            return ActionWindowActiveEmitter.ApplyNegate(session, graph, output, term.Negate, compare.DisplayName, index, path);
        }

    }
}
