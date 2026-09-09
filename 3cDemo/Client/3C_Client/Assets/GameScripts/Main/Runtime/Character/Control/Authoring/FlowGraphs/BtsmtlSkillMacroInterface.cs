#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using ParadoxNotion;
using ThirdPersonCharacter.Pipeline;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillMacroInterface
    {
        static readonly Type[] s_ValueTypes =
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(Vector2),
            typeof(Vector3),
            typeof(uint),
            typeof(ulong)
        };

        public static IReadOnlyList<Type> ValueTypes => s_ValueTypes;

        public static void OpenEditor(BtsmtlSkillMacroGraph graph)
        {
            if (!graph)
                throw new ArgumentNullException(nameof(graph));
            BtsmtlSkillMacroInterfaceWindow.Open(graph);
        }

        internal static void ApplyInterfaceMutation(
            BtsmtlSkillMacroGraph graph,
            string title,
            Action mutation)
        {
            var callers = FindCallers(graph).ToList();
            var owners = callers
                .Select(value => (UnityEngine.Object)value.Graph)
                .Append(graph)
                .Distinct()
                .ToArray();
            BtsmtlSkillFlowEditorMutation.Apply(
                graph,
                title,
                () =>
                {
                    mutation();
                    foreach (Caller caller in callers)
                    {
                        caller.Call.GatherPorts();
                        ValidateCall(caller.Call, graph, caller.Path);
                        BtsmtlSkillGraphClosure.Validate(caller.Graph, false);
                    }
                    Validate(graph);
                },
                true,
                owners);
        }

        internal static void RequireParameterChangeAllowed(
            BtsmtlSkillMacroGraph graph,
            string parameterId)
        {
            foreach (Caller caller in FindCallers(graph))
            {
                Port input = caller.Call.GetInputPort(parameterId);
                Port output = caller.Call.GetOutputPort(parameterId);
                if (input?.connections > 0 || output?.connections > 0)
                    throw new InvalidOperationException(
                        $"{caller.Path}: 已连接的Macro参数必须先断开，不能直接修改类型或删除。");
            }
        }

        public static void Initialize(BtsmtlSkillMacroGraph graph)
        {
            if (graph.inputDefinitions.Count != 0 || graph.outputDefinitions.Count != 0)
                throw new InvalidOperationException("技能Macro接口只能初始化一次。");
            graph.inputDefinitions.Add(new DynamicParameterDefinition("执行", typeof(Flow)));
        }

        public static void Validate(BtsmtlSkillMacroGraph graph)
            => Validate(graph.inputDefinitions, graph.outputDefinitions);

        public static void ValidateCall(MacroNodeWrapper call, BtsmtlSkillMacroGraph graph, string path)
        {
            if (call == null || graph == null)
                throw new InvalidOperationException($"{path}: 技能Macro调用目标缺失。");
            Validate(graph);
            var inputIds = new HashSet<string>(StringComparer.Ordinal);
            var outputIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DynamicParameterDefinition definition in graph.inputDefinitions)
            {
                Port port = call.GetInputPort(definition.ID);
                RequirePort(port, definition, true, path);
                inputIds.Add(definition.ID);
            }
            foreach (DynamicParameterDefinition definition in graph.outputDefinitions)
            {
                Port port = call.GetOutputPort(definition.ID);
                RequirePort(port, definition, false, path);
                outputIds.Add(definition.ID);
            }
            foreach (Port port in call.GetInputFlowPorts().Cast<Port>().Concat(call.GetInputValuePorts()))
                if (!inputIds.Contains(port.ID))
                    throw new InvalidOperationException($"{path}: 调用节点包含未登记输入端口'{port.ID}'。");
            foreach (Port port in call.GetOutputFlowPorts().Cast<Port>().Concat(call.GetOutputValuePorts()))
                if (!outputIds.Contains(port.ID))
                    throw new InvalidOperationException($"{path}: 调用节点包含未登记输出端口'{port.ID}'。");
        }

        public static void Validate(IEnumerable<DynamicParameterDefinition> inputs, IEnumerable<DynamicParameterDefinition> outputs)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            int flowInputs = 0;
            foreach (DynamicParameterDefinition input in inputs)
            {
                RequireParameter(input, identities, names);
                if (input.type == typeof(Flow))
                    flowInputs++;
                else
                    RequireValueType(input.type);
            }
            if (flowInputs != 1)
                throw new InvalidOperationException("技能Macro必须有且仅有一个执行入口。");
            names.Clear();
            foreach (DynamicParameterDefinition output in outputs)
            {
                RequireParameter(output, identities, names);
                RequireValueType(output.type);
            }
        }

        static void RequireParameter(DynamicParameterDefinition parameter, HashSet<string> identities, HashSet<string> names)
        {
            if (parameter == null || !parameter.hasStableIdentity || !identities.Add(parameter.ID) ||
                string.IsNullOrWhiteSpace(parameter.name) || !names.Add(parameter.name))
                throw new InvalidOperationException("技能Macro参数必须有独立稳定身份和名称。");
        }

        public static void RequireValueType(Type type)
        {
            if (type != typeof(bool) && type != typeof(int) && type != typeof(float) && type != typeof(string) &&
                type != typeof(Vector2) && type != typeof(Vector3) && type != typeof(uint) && type != typeof(ulong))
                throw new InvalidOperationException($"技能Macro不支持'{type?.FullName}'值参数；执行完成由技能调用状态返回。");
        }

        static void RequirePort(Port port, DynamicParameterDefinition definition, bool input, string path)
        {
            if (port == null ||
                port.IsFlowPort() != (definition.type == typeof(Flow)) ||
                definition.type != typeof(Flow) && port.type != definition.type)
            {
                string direction = input ? "输入" : "输出";
                throw new InvalidOperationException(
                    $"{path}: Macro{direction}端口'{definition.ID}'类型或方向与接口不一致。");
            }
        }

        static IEnumerable<Caller> FindCallers(BtsmtlSkillMacroGraph macro)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterPipelineDefinition"))
            {
                CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (!definition)
                    continue;
                foreach (BtsmtlSkillFlowGraph root in definition.SkillGraphs ?? Array.Empty<BtsmtlSkillFlowGraph>())
                {
                    if (!root)
                        continue;
                    IReadOnlyList<FlowGraph> graphs;
                    try { graphs = BtsmtlSkillGraphClosure.Validate(root, false); }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                    foreach (FlowGraph graph in graphs)
                        foreach (MacroNodeWrapper call in graph.allNodes.OfType<MacroNodeWrapper>())
                            if (ReferenceEquals(call.macro, macro))
                                yield return new Caller(graph, call);
                }
            }
        }

        readonly struct Caller
        {
            internal Caller(FlowGraph graph, MacroNodeWrapper call)
            {
                Graph = graph;
                Call = call;
            }

            internal FlowGraph Graph { get; }
            internal MacroNodeWrapper Call { get; }
            internal string Path => $"{((IBtsmtlSkillFlowGraph)Graph).AuthoringId}/node:{Call.UID}";
        }
    }

    sealed class BtsmtlSkillMacroInterfaceWindow : EditorWindow
    {
        BtsmtlSkillMacroGraph m_Graph;
        Vector2 m_Scroll;

        public static void Open(BtsmtlSkillMacroGraph graph)
        {
            var window = GetWindow<BtsmtlSkillMacroInterfaceWindow>("技能Macro接口");
            window.m_Graph = graph;
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        void OnGUI()
        {
            if (!m_Graph)
            {
                EditorGUILayout.HelpBox("Macro资产已失效。", MessageType.Warning);
                return;
            }
            EditorGUILayout.LabelField(m_Graph.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AssetDatabase.IsMainAsset(m_Graph) ? "Shared Asset" : "Inline");
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            DrawParameters("输入参数", m_Graph.inputDefinitions, true);
            EditorGUILayout.Space(8f);
            DrawParameters("输出参数", m_Graph.outputDefinitions, false);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加值输入"))
                    AddParameter(m_Graph.inputDefinitions, "添加Macro输入");
                if (GUILayout.Button("添加值输出"))
                    AddParameter(m_Graph.outputDefinitions, "添加Macro输出");
            }
        }

        void DrawParameters(string title, List<DynamicParameterDefinition> definitions, bool input)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            for (int index = 0; index < definitions.Count; index++)
            {
                DynamicParameterDefinition definition = definitions[index];
                using var row = new EditorGUILayout.HorizontalScope();
                bool execution = input && definition.type == typeof(Flow);
                using (new EditorGUI.DisabledScope(execution))
                {
                    string name = EditorGUILayout.DelayedTextField(definition.name, GUILayout.Width(150f));
                    int typeIndex = TypeIndex(definition.type);
                    int nextTypeIndex = execution
                        ? typeIndex
                        : EditorGUILayout.Popup(typeIndex, TypeNames(), GUILayout.Width(100f));
                    if (!string.Equals(name, definition.name, StringComparison.Ordinal))
                        Rename(definition, name);
                    if (!execution && nextTypeIndex != typeIndex)
                        ChangeType(definition, s_ValueTypes[nextTypeIndex]);
                }
                if (!execution && GUILayout.Button("删除", GUILayout.Width(48f)))
                    Remove(definitions, definition, input);
            }
        }

        void AddParameter(List<DynamicParameterDefinition> definitions, string title)
        {
            try
            {
                BtsmtlSkillMacroInterface.ApplyInterfaceMutation(
                    m_Graph,
                    title,
                    () => definitions.Add(new DynamicParameterDefinition(
                        Guid.NewGuid().ToString("N"),
                        "值",
                        typeof(float))));
            }
            catch (Exception error) { ShowNotification(new GUIContent(error.Message)); }
        }

        void Rename(DynamicParameterDefinition definition, string name)
        {
            try
            {
                BtsmtlSkillMacroInterface.ApplyInterfaceMutation(
                    m_Graph,
                    "重命名Macro参数",
                    () => definition.name = name);
            }
            catch (Exception error) { ShowNotification(new GUIContent(error.Message)); }
        }

        void ChangeType(DynamicParameterDefinition definition, Type type)
        {
            try
            {
                BtsmtlSkillMacroInterface.RequireParameterChangeAllowed(m_Graph, definition.ID);
                BtsmtlSkillMacroInterface.ApplyInterfaceMutation(
                    m_Graph,
                    "修改Macro参数类型",
                    () => definition.type = type);
            }
            catch (Exception error) { ShowNotification(new GUIContent(error.Message)); }
        }

        void Remove(List<DynamicParameterDefinition> definitions, DynamicParameterDefinition definition, bool input)
        {
            try
            {
                if (input && definition.type == typeof(Flow))
                    throw new InvalidOperationException("技能Macro执行入口不能删除。");
                BtsmtlSkillMacroInterface.RequireParameterChangeAllowed(m_Graph, definition.ID);
                BtsmtlSkillMacroInterface.ApplyInterfaceMutation(
                    m_Graph,
                    "删除Macro参数",
                    () => definitions.Remove(definition));
            }
            catch (Exception error) { ShowNotification(new GUIContent(error.Message)); }
        }

        static int TypeIndex(Type type)
        {
            int index = Array.IndexOf(BtsmtlSkillMacroInterface.ValueTypes.ToArray(), type);
            return index < 0 ? 0 : index;
        }

        static string[] TypeNames() =>
            BtsmtlSkillMacroInterface.ValueTypes.Select(value => value.Name).ToArray();
    }
}
#endif
