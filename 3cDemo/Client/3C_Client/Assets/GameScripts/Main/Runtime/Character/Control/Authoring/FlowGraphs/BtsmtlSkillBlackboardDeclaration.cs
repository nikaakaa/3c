#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Serializable]
    public sealed class BtsmtlSkillBlackboardDeclaration
    {
        [SerializeField] string m_VariableId;
        [SerializeField] PipelineBlackboardVariableScope m_Scope;
        [SerializeField] PipelineBlackboardVariableLifetime m_Lifetime;
        [SerializeField] string m_Category;
        [SerializeField] PipelineBlackboardInputBinding m_InputBinding;
        [SerializeField] PipelineBlackboardFactProjection m_FactProjection;

        public BtsmtlSkillBlackboardDeclaration(string variableId, PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime, string category = "", PipelineBlackboardInputBinding inputBinding = null,
            PipelineBlackboardFactProjection factProjection = null)
        {
            if (string.IsNullOrWhiteSpace(variableId))
                throw new ArgumentException("黑板声明必须引用稳定变量ID。", nameof(variableId));
            m_VariableId = variableId;
            m_Scope = scope;
            m_Lifetime = lifetime;
            m_Category = category ?? string.Empty;
            m_InputBinding = inputBinding;
            m_FactProjection = factProjection;
        }

        public string VariableId => m_VariableId;
        public PipelineBlackboardVariableScope Scope => m_Scope;
        public PipelineBlackboardVariableLifetime Lifetime => m_Lifetime;
        public string Category => m_Category;
        public PipelineBlackboardInputBinding InputBinding => m_InputBinding != null && m_InputBinding.IsDefined ? m_InputBinding : null;
        public PipelineBlackboardFactProjection FactProjection => m_FactProjection != null && m_FactProjection.IsDefined ? m_FactProjection : null;

        public void Validate(Variable variable)
        {
            if (variable == null || !variable.hasStableIdentity || variable.ID != m_VariableId ||
                variable.isDataBound || variable.isPropertyBound || variable is not ISerializedVariableValue)
                throw new InvalidOperationException("技能黑板变量必须有稳定身份，且不能绑定原生运行getter或对象属性。");
            Type type = variable.varType;
            if (type != typeof(bool) && type != typeof(int) && type != typeof(float) && type != typeof(string) &&
                type != typeof(Vector2) && type != typeof(Vector3) && type != typeof(ActionTargetSnapshot))
                throw new InvalidOperationException($"技能黑板不支持'{type.FullName}'类型。");
            if (m_Scope is PipelineBlackboardVariableScope.AIController or PipelineBlackboardVariableScope.AITick ||
                !PipelineBlackboardVariablePolicy.IsValid(m_Scope, m_Lifetime))
                throw new InvalidOperationException("技能黑板作用域与生命周期不匹配。");
            PipelineBlackboardInputBinding inputBinding = InputBinding;
            PipelineBlackboardFactProjection factProjection = FactProjection;
            if (inputBinding != null && (m_Scope != PipelineBlackboardVariableScope.Character ||
                m_Lifetime != PipelineBlackboardVariableLifetime.Spawn || type != typeof(ActionTargetSnapshot)))
                throw new InvalidOperationException("黑板输入绑定必须使用有效输入ID、ActionTargetSnapshot类型和Character／Spawn作用域。");
            if (factProjection != null && (factProjection.Kind != PipelineBlackboardFactProjectionKind.ActionWindow ||
                type != typeof(bool) || m_Scope != PipelineBlackboardVariableScope.Frame ||
                m_Lifetime != PipelineBlackboardVariableLifetime.Frame || string.IsNullOrWhiteSpace(factProjection.ActionWindowType) ||
                string.IsNullOrWhiteSpace(factProjection.ActionWindowId)))
                throw new InvalidOperationException("动作窗口投射必须使用Frame布尔声明并指定窗口类型与ID。");
        }
    }

    public static class BtsmtlSkillBlackboardDeclarations
    {
        public static Variable Add(FlowGraph graph, BtsmtlSkillBlackboardDeclaration declaration, string name, Type type, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("黑板变量名称不能为空。", nameof(name));
            var owner = (IBtsmtlSkillFlowGraph)graph;
            return BtsmtlSkillFlowEditorMutation.Execute(graph, "新增技能黑板声明", () =>
            {
                var variables = graph.GetGraphSource().localBlackboard.variables;
                if (variables.ContainsKey(name))
                    throw new InvalidOperationException($"黑板变量'{name}'已存在。");
                var variable = (Variable)Activator.CreateInstance(typeof(Variable<>).MakeGenericType(type), name, declaration.VariableId);
                declaration.Validate(variable);
                variable.SetValueBoxed(value == null && type == typeof(string) ? string.Empty : value);
                variables.Add(name, variable);
                owner.SetBlackboardDeclarations(owner.BlackboardDeclarations.Concat(new[] { declaration }));
                return variable;
            });
        }

        public static void Validate(FlowGraph graph, IReadOnlyList<BtsmtlSkillBlackboardDeclaration> declarations)
        {
            var variables = graph.GetGraphSource().localBlackboard.variables;
            var identities = new Dictionary<string, Variable>(StringComparer.Ordinal);
            foreach (var pair in variables)
            {
                Variable variable = pair.Value;
                if (variable == null || !variable.hasStableIdentity || pair.Key != variable.name ||
                    string.IsNullOrWhiteSpace(variable.name) || !identities.TryAdd(variable.ID, variable))
                    throw new InvalidOperationException("技能黑板变量名称、索引或稳定身份无效。");
            }
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (BtsmtlSkillBlackboardDeclaration declaration in declarations)
            {
                if (declaration == null || !declared.Add(declaration.VariableId) || !identities.TryGetValue(declaration.VariableId, out Variable variable))
                    throw new InvalidOperationException("技能黑板元数据重复或没有对应原生变量。");
                declaration.Validate(variable);
            }
            if (declared.Count != identities.Count)
                throw new InvalidOperationException("原生黑板变量缺少明确的BTSMTL作用域声明。");
        }

        public static Variable RequireVariable(FlowGraph graph, string identity) =>
            graph.GetGraphSource().localBlackboard.variables.Values.Single(variable => variable.ID == identity);
    }
}
#endif
