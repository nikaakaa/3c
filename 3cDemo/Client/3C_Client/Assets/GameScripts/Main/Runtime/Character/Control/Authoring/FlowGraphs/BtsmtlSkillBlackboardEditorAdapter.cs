#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillBlackboardEditorAdapter
    {
        static readonly (Type Type, string Label, string Name)[] LocalTypes =
        {
            (typeof(bool), "Boolean", "newBoolean"),
            (typeof(int), "Integer", "newInteger"),
            (typeof(float), "Number", "newNumber"),
            (typeof(string), "Identity", "newIdentity"),
            (typeof(Vector2), "Vector2", "newVector2"),
            (typeof(Vector3), "Vector3", "newVector3")
        };

        public static void Draw(FlowGraph graph, IBlackboard blackboard)
        {
            if (!ReferenceEquals(blackboard, graph.blackboard))
                return;
            EditorUtils.Separator();
            EditorGUILayout.LabelField("Skill Providers", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Character State · 只读", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Input / TargetData · 只读", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Ability Attribute / GameplayTag · 通过正式节点访问", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Skill Local · 以下原生变量列表管理", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(graph.isEditorReadOnly))
            {
                if (GUILayout.Button("添加外部 Provider 节点"))
                    BtsmtlSkillProviderNodeMenu.Show(graph);
            }
        }

        public static GenericMenu GetAddVariableMenu(FlowGraph graph, IBlackboard blackboard)
        {
            EnsureLocalBlackboard(graph, blackboard);
            var menu = new GenericMenu();
            for (int i = 0; i < LocalTypes.Length; i++)
            {
                var local = LocalTypes[i];
                menu.AddItem(new GUIContent("Skill Local/" + local.Label), false, () => AddVariable(graph, local.Type, local.Name));
            }
            return menu;
        }

        public static GenericMenu GetVariableMenu(FlowGraph graph, IBlackboard blackboard, Variable variable)
        {
            EnsureLocalBlackboard(graph, blackboard);
            var menu = new GenericMenu();
            if (variable == null || !TryGetDeclaration(graph, variable, out BtsmtlSkillBlackboardDeclaration declaration))
            {
                menu.AddDisabledItem(new GUIContent("Skill declaration is missing"));
                return menu;
            }

            menu.AddDisabledItem(new GUIContent("Type: " + variable.varType.FriendlyName()));
            menu.AddItem(new GUIContent("Create Get Node"), false, () => CreateAccessNode(graph, variable, false));
            menu.AddItem(new GUIContent("Create Set Node"), false, () => CreateAccessNode(graph, variable, true));
            menu.AddSeparator("Skill Scope/");
            AddScopeItem(menu, graph, variable, declaration, "Graph/Config", PipelineBlackboardVariableScope.Graph, PipelineBlackboardVariableLifetime.Config);
            AddScopeItem(menu, graph, variable, declaration, "Graph/Instance", PipelineBlackboardVariableScope.Graph, PipelineBlackboardVariableLifetime.GraphInstance);
            AddScopeItem(menu, graph, variable, declaration, "State", PipelineBlackboardVariableScope.State, PipelineBlackboardVariableLifetime.StateEnterToExit);
            AddScopeItem(menu, graph, variable, declaration, "Action Instance", PipelineBlackboardVariableScope.ActionInstance, PipelineBlackboardVariableLifetime.ActionInstance);
            AddScopeItem(menu, graph, variable, declaration, "Frame", PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame);
            menu.AddSeparator("/");
            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateVariable(graph, variable, declaration));
            AppendTypeMenu(menu, graph, variable);
            menu.AddItem(new GUIContent("Delete Variable"), false, () => DeleteVariable(graph, variable));
            return menu;
        }

        public static void ExecuteMutation(FlowGraph graph, string title, Action mutation)
        {
            try
            {
                BtsmtlSkillFlowEditorMutation.Apply(
                    graph,
                    title,
                    () =>
                    {
                        mutation();
                        ValidateVariableObjects(graph, graph.blackboard.variables.Values);
                    },
                    true,
                    Array.Empty<UnityEngine.Object>(),
                    false);
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }

        public static void ApplyVariableList(
            FlowGraph graph,
            IBlackboard blackboard,
            IReadOnlyList<Variable> variables)
        {
            EnsureLocalBlackboard(graph, blackboard);
            var next = variables.ToDictionary(variable => variable.name, variable => variable);
            bool unchanged = blackboard.variables.Count == next.Count &&
                blackboard.variables.Keys.SequenceEqual(next.Keys) &&
                blackboard.variables.Values.SequenceEqual(next.Values);
            if (unchanged)
            {
                BtsmtlSkillBlackboardDeclarations.Validate(graph, Authoring(graph).BlackboardDeclarations);
                return;
            }

            BtsmtlSkillFlowEditorMutation.Apply(
                graph,
                "更新技能黑板变量",
                () =>
                {
                    blackboard.variables = next;
                    BtsmtlSkillBlackboardDeclarations.Validate(graph, Authoring(graph).BlackboardDeclarations);
                },
                true,
                Array.Empty<UnityEngine.Object>(),
                false);
        }

        static void AddVariable(FlowGraph graph, Type type, string name)
        {
            try
            {
                var declaration = new BtsmtlSkillBlackboardDeclaration(
                    Guid.NewGuid().ToString("N"),
                    PipelineBlackboardVariableScope.Graph,
                    PipelineBlackboardVariableLifetime.GraphInstance);
                BtsmtlSkillBlackboardDeclarations.Add(
                    graph,
                    declaration,
                    NextVariableName(graph.blackboard, name),
                    type,
                    DefaultValue(type));
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }

        static void DuplicateVariable(
            FlowGraph graph,
            Variable source,
            BtsmtlSkillBlackboardDeclaration declaration)
        {
            try
            {
                var copy = new BtsmtlSkillBlackboardDeclaration(
                    Guid.NewGuid().ToString("N"),
                    declaration.Scope,
                    declaration.Lifetime,
                    declaration.Category,
                    declaration.InputBinding,
                    declaration.FactProjection);
                BtsmtlSkillBlackboardDeclarations.Add(
                    graph,
                    copy,
                    NextVariableName(graph.blackboard, source.name),
                    source.varType,
                    source.value);
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }

        static void DeleteVariable(FlowGraph graph, Variable variable)
        {
            if (graph.allNodes.OfType<IBtsmtlSkillBlackboardAccessNode>().Any(node =>
                    node.Variable.DeclarationId == variable.ID))
            {
                GraphEditor.current?.ShowNotification(new GUIContent("该技能黑板变量仍被节点引用，不能删除。"));
                return;
            }

            try
            {
                ExecuteMutation(graph, "删除技能黑板变量", () =>
                {
                    graph.blackboard.RemoveVariable(variable.name);
                    Authoring(graph).SetBlackboardDeclarations(Authoring(graph).BlackboardDeclarations
                        .Where(declaration => declaration.VariableId != variable.ID));
                });
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }

        static void AppendTypeMenu(GenericMenu menu, FlowGraph graph, Variable variable)
        {
            menu.AddSeparator("Skill Type/");
            for (int i = 0; i < LocalTypes.Length; i++)
            {
                var local = LocalTypes[i];
                menu.AddItem(
                    new GUIContent(local.Label),
                    variable.varType == local.Type,
                    () => ChangeType(graph, variable, local.Type));
            }
        }

        static void ChangeType(FlowGraph graph, Variable variable, Type type)
        {
            if (variable.varType == type)
                return;
            if (graph.allNodes.OfType<IBtsmtlSkillBlackboardAccessNode>().Any(node =>
                    node.Variable.DeclarationId == variable.ID))
            {
                GraphEditor.current?.ShowNotification(new GUIContent("该技能黑板变量仍被节点引用，修改类型前请先删除或重建访问节点。"));
                return;
            }

            try
            {
                ExecuteMutation(graph, "修改技能黑板类型", () =>
                {
                    graph.blackboard.ChangeVariableType(variable, type).SetValueBoxed(DefaultValue(type));
                    BtsmtlSkillBlackboardDeclarations.Validate(graph, Authoring(graph).BlackboardDeclarations);
                });
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }

        static void AddScopeItem(
            GenericMenu menu,
            FlowGraph graph,
            Variable variable,
            BtsmtlSkillBlackboardDeclaration declaration,
            string label,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime)
        {
            menu.AddItem(
                new GUIContent(label),
                declaration.Scope == scope && declaration.Lifetime == lifetime,
                () => SetScope(graph, variable, declaration, scope, lifetime));
        }

        static void SetScope(
            FlowGraph graph,
            Variable variable,
            BtsmtlSkillBlackboardDeclaration current,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime)
        {
            if (current.Scope == scope && current.Lifetime == lifetime)
                return;
            try
            {
                ExecuteMutation(graph, "修改技能黑板作用域", () =>
                {
                    var replacement = new BtsmtlSkillBlackboardDeclaration(
                        variable.ID,
                        scope,
                        lifetime,
                        current.Category,
                        current.InputBinding,
                        current.FactProjection);
                    Authoring(graph).SetBlackboardDeclarations(Authoring(graph).BlackboardDeclarations
                        .Select(declaration => declaration.VariableId == variable.ID ? replacement : declaration));
                });
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }

        static void CreateAccessNode(FlowGraph graph, Variable variable, bool writes)
        {
            if (!BtsmtlSkillFlowEditorMutation.TryGetBlackboardValueType(variable.varType, out BtsmtlSkillBlackboardValueType valueType))
            {
                GraphEditor.current?.ShowNotification(new GUIContent("变量类型不受技能Blackboard节点合同支持。"));
                return;
            }
            BtsmtlSkillFlowEditorMutation.CreateBlackboardAccessNode(
                graph,
                variable,
                valueType,
                NextNodePosition(graph),
                writes);
        }

        static bool TryGetDeclaration(
            FlowGraph graph,
            Variable variable,
            out BtsmtlSkillBlackboardDeclaration declaration)
        {
            declaration = Authoring(graph).BlackboardDeclarations.FirstOrDefault(value =>
                value != null && value.VariableId == variable.ID);
            return declaration != null;
        }

        static void ValidateVariableObjects(
            FlowGraph graph,
            IEnumerable<Variable> variables)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Variable variable in variables)
            {
                if (variable == null || string.IsNullOrWhiteSpace(variable.name) ||
                    !variable.hasStableIdentity || !identities.Add(variable.ID) ||
                    !names.Add(variable.name) || variable.isDataBound || variable.isPropertyBound ||
                    variable is not ISerializedVariableValue ||
                    !BtsmtlSkillFlowEditorMutation.TryGetBlackboardValueType(variable.varType, out _))
                    throw new InvalidOperationException("技能黑板变量名称、稳定身份或类型无效。");
                BtsmtlSkillBlackboardDeclaration declaration = Authoring(graph).BlackboardDeclarations
                    .FirstOrDefault(value => value != null && value.VariableId == variable.ID);
                declaration?.Validate(variable);
            }
        }

        static string NextVariableName(IBlackboard blackboard, string seed)
        {
            string name = seed;
            while (blackboard.variables.ContainsKey(name))
                name += ".";
            return name;
        }

        static Vector2 NextNodePosition(FlowGraph graph) =>
            new Vector2(80f + graph.allNodes.Count % 4 * 220f, 80f + graph.allNodes.Count / 4 * 120f);

        static object DefaultValue(Type type) =>
            type == typeof(string) ? string.Empty : Activator.CreateInstance(type);

        static IBtsmtlSkillFlowGraph Authoring(FlowGraph graph) =>
            graph as IBtsmtlSkillFlowGraph ?? throw new InvalidOperationException("技能黑板缺少正式技能图上下文。");

        static void EnsureLocalBlackboard(FlowGraph graph, IBlackboard blackboard)
        {
            if (!ReferenceEquals(blackboard, graph.blackboard))
                throw new InvalidOperationException("Skill Blackboard只能编辑当前技能图的Local声明。");
        }
    }
}
#endif
