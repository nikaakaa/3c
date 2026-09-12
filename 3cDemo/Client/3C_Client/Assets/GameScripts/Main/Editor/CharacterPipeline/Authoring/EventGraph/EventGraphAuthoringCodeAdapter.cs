using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using BTSMTL.EventGraphs;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ParadoxNotion;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph
{
    public sealed class EventGraphAuthoringCodeAdapter :
        IBtsmtlAuthoringCodeDomainAdapter
    {
        public static readonly EventGraphAuthoringCodeAdapter Instance =
            new EventGraphAuthoringCodeAdapter();

        public string DomainId => "event-graph";

        public bool CanHandle(object root) => root is HostEventGraph;

        public void Emit(
            BtsmtlAuthoringCodeExportContext context,
            object root)
        {
            if (root is not HostEventGraph graph)
            {
                context.ReportError(
                    "event_graph_root_type_invalid",
                    root?.GetType().FullName,
                    "事件图输出适配收到的根对象不是HostEventGraph。");
                return;
            }

            AddUsings(context);
            string graphVariable = context.RegisterObject(
                graph,
                graph.AuthoringId,
                "eventGraph",
                true);
            if (string.IsNullOrEmpty(graphVariable))
                return;

            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Create,
                $"var {graphVariable} = EventGraphAuthoringCode.EnsureRoot<{TypeExpression(graph.GetType())}>(context, {StringLiteral(graph.AuthoringId)}, {StringLiteral(graph.ContentRevision)}, {StringLiteral(graph.name)});");
            EmitCanvas(context, graphVariable, graph);
            EmitVariables(context, graphVariable, graph);

            List<Node> nodes = graph.allNodes?.ToList() ?? new List<Node>();
            var nodeVariables = new Dictionary<Node, string>(ReferenceComparer<Node>.Instance);
            foreach (Node node in nodes)
            {
                if (node == null)
                {
                    context.ReportError(
                        "event_graph_null_node",
                        graph.AuthoringId,
                        "事件图包含空节点，无法完整导出。");
                    continue;
                }
                if (node is MacroInputNode || node is MacroOutputNode)
                {
                    context.ReportError(
                        "event_graph_root_macro_port",
                        node.UID,
                        "Macro输入输出端点不能直接存在于HostEventGraph根图。");
                    continue;
                }
                if (!EventGraphCapabilityCatalog.TryGet(
                        node.GetType(),
                        out EventGraphCapabilityDescriptor capability))
                {
                    context.ReportError(
                        "event_graph_node_unsupported",
                        node.UID,
                        $"事件图节点类型'{node.GetType().FullName}'没有正式输出适配。");
                    continue;
                }
                string nodeVariable = context.RegisterObject(
                    node,
                    node.UID,
                    "node");
                if (!string.IsNullOrEmpty(nodeVariable))
                    nodeVariables.Add(node, nodeVariable);
            }

            foreach (Node node in nodes)
            {
                if (!nodeVariables.TryGetValue(node, out string nodeVariable))
                    continue;
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {nodeVariable} = {graphVariable}.AddAuthoringNode(typeof({TypeExpression(node.GetType())}), {StringLiteral(node.UID)}, {Vector2Literal(node.position)});");
            }

            var macroVariables = new Dictionary<Macro, string>(ReferenceComparer<Macro>.Instance);
            foreach (Node node in nodes)
            {
                if (!nodeVariables.TryGetValue(node, out string nodeVariable))
                    continue;
                EmitNodeConfiguration(
                    context,
                    graphVariable,
                    node,
                    nodeVariable,
                    macroVariables);
            }

            foreach (Connection connection in graph.GetAllConnections())
                EmitConnection(context, graphVariable, connection, nodeVariables);

        }

        static void AddUsings(BtsmtlAuthoringCodeExportContext context)
        {
            context.AddUsing("BTSMTL.EventGraphs");
            context.AddUsing("FlowCanvas");
            context.AddUsing("FlowCanvas.Macros");
            context.AddUsing("FlowCanvas.Nodes");
            context.AddUsing("NodeCanvas.Framework");
            context.AddUsing("ParadoxNotion");
            context.AddUsing("ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph");
            context.AddUsing("UnityEngine");
        }

        static void EmitCanvas(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            HostEventGraph graph)
        {
            if (graph.externalSerializationFile != null)
                context.ReportError(
                    "event_graph_external_serialization_unsupported",
                    graph.AuthoringId,
                    "事件图使用外部序列化文件，当前直接API适配不能完整重建该引用。");
            if (graph.canvasGroups != null && graph.canvasGroups.Count != 0)
                context.ReportError(
                    "event_graph_canvas_groups_unsupported",
                    graph.AuthoringId,
                    "事件图CanvasGroup尚未接入正式直接API，不能省略布局导出。");
            if (!float.IsFinite(graph.zoomFactor) || graph.zoomFactor <= 0f)
            {
                context.ReportError(
                    "event_graph_zoom_invalid",
                    graph.AuthoringId,
                    "事件图Canvas zoom不是有限正数。");
                return;
            }
            if (!IsFinite(graph.translation))
            {
                context.ReportError(
                    "event_graph_translation_invalid",
                    graph.AuthoringId,
                    "事件图Canvas translation包含非法数值。");
                return;
            }
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Configure,
                $"{graphVariable}.ConfigureCanvas({StringLiteral(graph.category)}, {StringLiteral(graph.comments)}, {Vector2Literal(graph.translation)}, {FloatLiteral(graph.zoomFactor)});");
        }

        static void EmitVariables(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            HostEventGraph graph)
        {
            var variableIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (Variable variable in graph.blackboard.variables.Values)
            {
                if (variable == null)
                {
                    context.ReportError(
                        "event_graph_null_variable",
                        graph.AuthoringId,
                        "事件图包含空Blackboard变量，无法完整导出。");
                    continue;
                }
                if (!variable.hasStableIdentity)
                {
                    context.ReportError(
                        "event_graph_variable_identity_missing",
                        variable.name,
                        "事件图变量缺少稳定Variable.ID。");
                    continue;
                }
                if (!variableIds.Add(variable.ID))
                {
                    context.ReportError(
                        "event_graph_variable_identity_duplicate",
                        variable.ID,
                        "事件图Blackboard存在重复Variable.ID。");
                    continue;
                }
                if (!EventGraphValueKinds.IsSupported(variable.varType))
                {
                    context.ReportError(
                        "event_graph_variable_type_unsupported",
                        variable.ID,
                        $"事件图变量类型'{variable.varType?.FullName}'没有直接API输出。");
                    continue;
                }
                if (variable.isPropertyBound)
                {
                    context.ReportError(
                        "event_graph_variable_binding_unsupported",
                        variable.ID,
                        "事件图变量使用了属性绑定，当前直接API适配不能安全重建该外部绑定。");
                    continue;
                }
                string valueLiteral = BtsmtlAuthoringCodeValues.Value(
                    context,
                    variable.value,
                    variable.varType,
                    variable.ID);
                string type = TypeExpression(variable.varType);
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"{graphVariable}.DeclareVariable<{type}>({StringLiteral(variable.ID)}, {StringLiteral(variable.name)}, {valueLiteral});");
                if (variable.isExposedPublic)
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{graphVariable}.ConfigureVariable({StringLiteral(variable.ID)}, true);");
            }
        }

        static void EmitNodeConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            Node node,
            string nodeVariable,
            Dictionary<Macro, string> macroVariables)
        {
            if (!string.IsNullOrEmpty(node.tag) ||
                !string.IsNullOrEmpty(node.comments) ||
                node.isBreakpoint)
            {
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{graphVariable}.ConfigureNodeMetadata({nodeVariable}, {StringLiteral(node.tag)}, {StringLiteral(node.comments)}, {BoolLiteral(node.isBreakpoint)});");
            }

            if (node is EventGraphHostInputNodeMarker)
            {
                string inputId = GetHostInputId(node);
                if (string.IsNullOrWhiteSpace(inputId))
                {
                    context.ReportError(
                        "event_graph_host_input_identity_missing",
                        node.UID,
                        "事件图宿主输入节点缺少稳定输入ID。");
                }
                else
                {
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{graphVariable}.ConfigureHostInput({nodeVariable}, {StringLiteral(inputId)});");
                }
            }

            if (node is EventGraphDeltaNode)
            {
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{graphVariable}.ConfigureHostInput({nodeVariable}, {StringLiteral(EventGraphHostInputIds.DeltaSeconds)});");
            }

            if (node is UpdateEvent update)
            {
                if (update.updateInterval == null ||
                    update.updateInterval.useBlackboard ||
                    !float.IsFinite(update.updateInterval.value) ||
                    update.updateInterval.value != 0f)
                {
                    context.ReportError(
                        "event_graph_update_interval_unsupported",
                        node.UID,
                        "动画同步事件图只支持字面量零Update interval。");
                }
                else
                {
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{graphVariable}.ConfigureUpdateEvent((UpdateEvent){nodeVariable});");
                }
            }

            if (node is Split split)
            {
                int portCount = split.GetOutputFlowPorts().Count();
                if (split.mode != Split.Mode.Instant || portCount < 2)
                {
                    context.ReportError(
                        "event_graph_split_mode_unsupported",
                        node.UID,
                        "事件图只支持至少两个输出的Split.Instant。");
                }
                else
                {
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{graphVariable}.ConfigureInstantSplit((Split){nodeVariable}, {portCount});");
                }
            }

            if (node is Sequence sequence)
            {
                int portCount = sequence.GetOutputFlowPorts().Count();
                if (portCount < 2 || sequence.current < 0 || sequence.current >= portCount)
                {
                    context.ReportError(
                        "event_graph_sequence_configuration_invalid",
                        node.UID,
                        "事件图Sequence端口数量或起始索引无效。");
                }
                else
                {
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{graphVariable}.ConfigureSequence((Sequence){nodeVariable}, {portCount}, {sequence.current.ToString(CultureInfo.InvariantCulture)});");
                }
            }

            if (node is ParameterVariableNode variableNode)
                EmitVariableNodeConfiguration(
                    context,
                    graphVariable,
                    node,
                    nodeVariable,
                    variableNode);

            if (node is MacroNodeWrapper macroNode)
                EmitMacroConfiguration(
                    context,
                    graphVariable,
                    node,
                    nodeVariable,
                    macroNode,
                    macroVariables);

            if (node is FlowNode flowNode)
                EmitValueInputs(context, graphVariable, nodeVariable, flowNode);
        }

        static void EmitVariableNodeConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            Node node,
            string nodeVariable,
            ParameterVariableNode variableNode)
        {
            if (variableNode.parameter == null ||
                !variableNode.parameter.useBlackboard ||
                variableNode.parameter.varRef == null)
            {
                context.ReportError(
                    "event_graph_variable_node_unbound",
                    node.UID,
                    "事件图Get/Set节点没有绑定正式Blackboard变量。");
                return;
            }
            Variable variable = variableNode.parameter.varRef;
            if (node.graph is not HostEventGraph graph ||
                !graph.blackboard.variables.Values.Contains(variable))
            {
                context.ReportError(
                    "event_graph_variable_node_external_binding",
                    node.UID,
                    "事件图Get/Set节点引用了当前图之外的Blackboard变量。");
                return;
            }
            if (variable.varType != variableNode.parameter.varType)
            {
                context.ReportError(
                    "event_graph_variable_node_type_mismatch",
                    node.UID,
                    "事件图Get/Set节点与Variable类型不一致。");
                return;
            }
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Bind,
                $"{graphVariable}.BindVariableNode((ParameterVariableNode){nodeVariable}, {StringLiteral(variable.ID)});");

            if (node is SetVariable<float> floatSet)
                EmitAssignment(
                    context,
                    graphVariable,
                    nodeVariable,
                    "float",
                    floatSet.operation,
                    floatSet.perSecond);
            else if (node is SetVariable<int> intSet)
                EmitAssignment(
                    context,
                    graphVariable,
                    nodeVariable,
                    "int",
                    intSet.operation,
                    intSet.perSecond);
            else if (node is SetVariable<bool> boolSet)
                EmitAssignment(
                    context,
                    graphVariable,
                    nodeVariable,
                    "bool",
                    boolSet.operation,
                    boolSet.perSecond);
            else if (node is SetVariable<Vector2> vector2Set)
                EmitAssignment(
                    context,
                    graphVariable,
                    nodeVariable,
                    "UnityEngine.Vector2",
                    vector2Set.operation,
                    vector2Set.perSecond);
            else if (node is SetVariable<Vector3> vector3Set)
                EmitAssignment(
                    context,
                    graphVariable,
                    nodeVariable,
                    "UnityEngine.Vector3",
                    vector3Set.operation,
                    vector3Set.perSecond);
        }

        static void EmitAssignment(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            string nodeVariable,
            string type,
            AssignOp operation,
            bool perSecond)
        {
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Configure,
                $"{graphVariable}.ConfigureAssignment((SetVariable<{type}>){nodeVariable}, {BtsmtlAuthoringCodeSyntax.EnumLiteral("ParadoxNotion.AssignOp", operation.ToString())}, {BoolLiteral(perSecond)});");
        }

        static void EmitMacroConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            Node node,
            string nodeVariable,
            MacroNodeWrapper macroNode,
            Dictionary<Macro, string> macroVariables)
        {
            Macro macro = macroNode.macro;
            if (macro == null)
            {
                context.ReportError(
                    "event_graph_macro_missing",
                    node.UID,
                    "事件图Macro节点没有Macro资产。");
                return;
            }
            if (macro.usesExternalExecution)
            {
                context.ReportError(
                    "event_graph_macro_external_execution",
                    node.UID,
                    "事件图Macro不能使用外部执行模式。");
                return;
            }
            ValidateMacroInterface(context, node.UID, macro);
            string macroPath = AssetDatabase.GetAssetPath(macro);
            if (string.IsNullOrWhiteSpace(macroPath))
            {
                context.ReportError(
                    "event_graph_macro_reference_not_loadable",
                    node.UID,
                    "事件图Macro不是可由明确资产路径恢复的外部Macro引用。");
                return;
            }
            string macroExpression = BtsmtlAuthoringCodeValues.ExternalAsset(
                context,
                macro,
                typeof(Macro));
            if (macroExpression == "null")
                return;
            if (!macroVariables.TryGetValue(macro, out string macroVariable))
            {
                macroVariable = context.RegisterObject(
                    macro,
                    macroPath,
                    "macro");
                if (string.IsNullOrEmpty(macroVariable))
                    return;
                macroVariables.Add(macro, macroVariable);
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {macroVariable} = {macroExpression} ?? throw new System.InvalidOperationException({StringLiteral($"Event graph Macro '{macroPath}' could not be loaded.")});");
            }
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Configure,
                $"{graphVariable}.ConfigureMacro((MacroNodeWrapper){nodeVariable}, {macroVariable});");
        }

        static void ValidateMacroInterface(
            BtsmtlAuthoringCodeExportContext context,
            string subject,
            Macro macro)
        {
            foreach (DynamicParameterDefinition definition in
                     (macro.inputDefinitions ?? new List<DynamicParameterDefinition>())
                     .Concat(macro.outputDefinitions ?? new List<DynamicParameterDefinition>()))
            {
                if (definition == null ||
                    (definition.type != typeof(Flow) &&
                     !EventGraphValueKinds.IsSupported(definition.type)))
                {
                    context.ReportError(
                        "event_graph_macro_parameter_unsupported",
                        subject,
                        "事件图Macro包含未登记的参数类型。");
                }
            }
        }

        static void EmitValueInputs(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            string nodeVariable,
            FlowNode node)
        {
            foreach (Port port in node.GetAllPorts())
            {
                if (port is ValueInput valueInput)
                {
                    if (valueInput.isConnected)
                    {
                        if (!EventGraphValueKinds.IsSupported(valueInput.type))
                            context.ReportError(
                                "event_graph_value_input_type_unsupported",
                                $"{node.UID}/{port.ID}",
                                "事件图连接值输入的类型不在正式typed合同中。");
                        continue;
                    }
                    if (valueInput.isDefaultValue)
                        continue;
                    if (!EventGraphValueKinds.IsSupported(valueInput.type))
                    {
                        context.ReportError(
                            "event_graph_value_input_unsupported",
                            $"{node.UID}/{port.ID}",
                            "事件图未连接值输入不能按正式直接API完整输出。");
                        continue;
                    }
                    string literal = BtsmtlAuthoringCodeValues.Value(
                        context,
                        valueInput.serializedValue,
                        valueInput.type,
                        $"{node.UID}/{port.ID}");
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{graphVariable}.ConfigureValueInput((FlowNode){nodeVariable}, {StringLiteral(port.ID)}, {literal});");
                }
                else if (port is ValueOutput &&
                         port.isConnected &&
                         !EventGraphValueKinds.IsSupported(port.type))
                {
                    context.ReportError(
                        "event_graph_value_output_type_unsupported",
                        $"{node.UID}/{port.ID}",
                        "事件图连接值输出的类型不在正式typed合同中。");
                }
            }
        }

        static void EmitConnection(
            BtsmtlAuthoringCodeExportContext context,
            string graphVariable,
            Connection connection,
            Dictionary<Node, string> nodeVariables)
        {
            if (connection is not BinderConnection binder)
            {
                context.ReportError(
                    "event_graph_connection_unsupported",
                    connection?.UID,
                    "事件图包含非BinderConnection，不能通过正式直接API重建。");
                return;
            }
            if (binder.sourceNode == null || binder.targetNode == null ||
                !nodeVariables.TryGetValue(binder.sourceNode, out string sourceVariable) ||
                !nodeVariables.TryGetValue(binder.targetNode, out string targetVariable))
            {
                context.ReportError(
                    "event_graph_connection_node_missing",
                    binder.UID,
                    "事件图连接引用了未注册的节点。");
                return;
            }
            Port sourcePort = binder.sourcePort;
            Port targetPort = binder.targetPort;
            if (sourcePort == null || targetPort == null)
            {
                context.ReportError(
                    "event_graph_connection_port_missing",
                    binder.UID,
                    "事件图连接端口无法按稳定ID解析。");
                return;
            }
            if (sourcePort.IsValuePort() &&
                !EventGraphValueKinds.IsSupported(sourcePort.type))
            {
                context.ReportError(
                    "event_graph_connection_value_type_unsupported",
                    binder.UID,
                    "事件图连接值端口类型没有正式输出支持。");
                return;
            }
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Connect,
                $"{graphVariable}.ConnectAuthoringPorts({StringLiteral(binder.UID)}, {sourceVariable}, {StringLiteral(sourcePort.ID)}, {targetVariable}, {StringLiteral(targetPort.ID)});");
        }

        static string GetHostInputId(Node node)
        {
            if (node is EventGraphFloatInputNode floatInput)
                return floatInput.InputId;
            if (node is EventGraphIntInputNode intInput)
                return intInput.InputId;
            if (node is EventGraphBoolInputNode boolInput)
                return boolInput.InputId;
            if (node is EventGraphVector2InputNode vector2Input)
                return vector2Input.InputId;
            if (node is EventGraphVector3InputNode vector3Input)
                return vector3Input.InputId;
            return string.Empty;
        }

        static string TypeExpression(Type type)
        {
            return BtsmtlAuthoringCodeSyntax.TypeName(type);
        }

        static string StringLiteral(string value) =>
            BtsmtlAuthoringCodeSyntax.StringLiteral(value ?? string.Empty);

        static string BoolLiteral(bool value) => value ? "true" : "false";

        static string FloatLiteral(float value) =>
            BtsmtlAuthoringCodeSyntax.FloatLiteral(value);

        static string Vector2Literal(Vector2 value) =>
            $"new UnityEngine.Vector2({FloatLiteral(value.x)}, {FloatLiteral(value.y)})";

        static string Vector3Literal(Vector3 value) =>
            $"new UnityEngine.Vector3({FloatLiteral(value.x)}, {FloatLiteral(value.y)}, {FloatLiteral(value.z)})";

        static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);

        sealed class ReferenceComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static readonly ReferenceComparer<T> Instance =
                new ReferenceComparer<T>();

            public bool Equals(T left, T right) => ReferenceEquals(left, right);

            public int GetHashCode(T value) =>
                RuntimeHelpers.GetHashCode(value);
        }
    }
}
