#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using TreeDesigner;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillGraphAuthoringPanel
    {
        static GraphEditor s_Editor;
        static FlowGraph s_Graph;
        static VisualElement s_Panel;
        static IMGUIContainer s_Inspector;
        static string s_NewName = "newVariable";
        static BtsmtlSkillBlackboardValueType s_NewType = BtsmtlSkillBlackboardValueType.Number;
        static PipelineBlackboardVariableScope s_NewScope = PipelineBlackboardVariableScope.Graph;
        static PipelineBlackboardVariableLifetime s_NewLifetime = PipelineBlackboardVariableLifetime.GraphInstance;
        static string s_Message = string.Empty;
        static bool s_Registered;
        static CharacterPipelineDefinition s_Definition;

        public static void SetDefinitionContext(CharacterPipelineDefinition definition)
        {
            s_Definition = definition;
            s_Inspector?.MarkDirtyRepaint();
        }

        [InitializeOnLoadMethod]
        static void Register()
        {
            GraphEditor.onCurrentGraphChanged -= OnCurrentGraphChanged;
            GraphEditor.onCurrentGraphChanged += OnCurrentGraphChanged;
            GraphEditor.onEditorClosed -= Release;
            GraphEditor.onEditorClosed += Release;
            s_Registered = true;
        }

        static void OnCurrentGraphChanged(Graph graph)
        {
            if (graph is BtsmtlSkillFlowGraph || graph is BtsmtlSkillMacroGraph)
            {
                CharacterPipelineDefinition resolved = ResolveDefinition((FlowGraph)graph);
                if (resolved)
                    s_Definition = resolved;
                Bind((FlowGraph)graph);
            }
            else
                ReleasePanel();
        }

        public static void Bind(FlowGraph graph)
        {
            if (graph == null || GraphEditor.current == null)
                return;
            if (!s_Registered)
            {
                GraphEditor.onCurrentGraphChanged += OnCurrentGraphChanged;
                GraphEditor.onEditorClosed += Release;
                s_Registered = true;
            }
            if (s_Editor != GraphEditor.current)
            {
                ReleasePanel();
                s_Editor = GraphEditor.current;
                s_Panel = new VisualElement { name = "btsmtl-skill-authoring-panel" };
                s_Panel.style.paddingLeft = 8;
                s_Panel.style.paddingRight = 8;
                s_Panel.style.paddingTop = 8;
                s_Panel.style.paddingBottom = 8;
                s_Inspector = new IMGUIContainer(DrawInspector);
                s_Panel.Add(s_Inspector);
                s_Editor.SetDomainPanel(s_Panel, 300f);
            }
            s_Graph = graph;
            s_Inspector?.MarkDirtyRepaint();
        }

        static void DrawInspector()
        {
            if (s_Graph is not IBtsmtlSkillFlowGraph authoring)
                return;
            EditorGUILayout.LabelField("Skill Blackboard", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Skill Local", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "外部provider只保存正式owner和声明ID；Character State只读，Ability Attribute写入必须走Gameplay Effect。",
                MessageType.Info);

            s_NewName = EditorGUILayout.TextField("变量名", s_NewName);
            s_NewType = (BtsmtlSkillBlackboardValueType)EditorGUILayout.EnumPopup("类型", s_NewType);
            s_NewScope = (PipelineBlackboardVariableScope)EditorGUILayout.EnumPopup("作用域", s_NewScope);
            s_NewLifetime = (PipelineBlackboardVariableLifetime)EditorGUILayout.EnumPopup("生命周期", s_NewLifetime);
            using (new EditorGUI.DisabledScope(s_Graph.isEditorReadOnly))
            {
                if (GUILayout.Button("创建 Skill Local 变量"))
                    CreateVariable(authoring);
            }

            if (!string.IsNullOrEmpty(s_Message))
                EditorGUILayout.HelpBox(s_Message, MessageType.Warning);

            DrawExternalProviders();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("当前声明", EditorStyles.boldLabel);
            foreach (BtsmtlSkillBlackboardDeclaration declaration in authoring.BlackboardDeclarations)
            {
                if (declaration == null)
                    continue;
                Variable variable = s_Graph.GetGraphSource().localBlackboard.variables.Values
                    .FirstOrDefault(value => value != null && value.ID == declaration.VariableId);
                if (variable == null)
                    continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{variable.name} : {variable.varType.Name}", GUILayout.MinWidth(120f));
                using (new EditorGUI.DisabledScope(s_Graph.isEditorReadOnly))
                {
                    if (GUILayout.Button("Get", GUILayout.Width(42f)))
                        CreateNode(variable, false);
                    if (GUILayout.Button("Set", GUILayout.Width(42f)))
                        CreateNode(variable, true);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField($"{declaration.Scope} / {declaration.Lifetime}", EditorStyles.miniLabel);
            }
        }

        static void DrawExternalProviders()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("External Providers", EditorStyles.boldLabel);
            CharacterPipelineDefinition definition = s_Definition;
            if (!definition)
            {
                EditorGUILayout.HelpBox("当前Skill Graph没有绑定CharacterPipelineDefinition，无法显示外部provider目录。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Character State", EditorStyles.miniLabel);
            DrawProviderButton(
                "读取移动方向与角色朝向夹角",
                typeof(BtsmtlSkillMoveFacingAngleFlowNode),
                node => ((BtsmtlSkillMoveFacingAngleFlowNode)node).Configure(
                    $"control-module:{definition.ControlModuleId}"));
            string controlOwner = $"control-module:{definition.ControlModuleId}";
            DrawProviderButton(
                "Get Movement Position",
                typeof(BtsmtlSkillCharacterStateVector3FlowNode),
                node => ((BtsmtlSkillCharacterStateVector3FlowNode)node).Configure(
                    CharacterStateProviderFields.Position,
                    controlOwner));
            DrawProviderButton(
                "Get Movement Velocity",
                typeof(BtsmtlSkillCharacterStateVector3FlowNode),
                node => ((BtsmtlSkillCharacterStateVector3FlowNode)node).Configure(
                    CharacterStateProviderFields.Velocity,
                    controlOwner));
            DrawProviderButton(
                "Get Vertical Velocity",
                typeof(BtsmtlSkillCharacterStateScalarFlowNode),
                node => ((BtsmtlSkillCharacterStateScalarFlowNode)node).Configure(
                    CharacterStateProviderFields.VerticalVelocity,
                    controlOwner));
            DrawProviderButton(
                "Get Body Yaw",
                typeof(BtsmtlSkillCharacterStateYawFlowNode),
                node => ((BtsmtlSkillCharacterStateYawFlowNode)node).Configure(
                    CharacterStateProviderFields.BodyYaw,
                    controlOwner));
            DrawProviderButton(
                "Get Grounded",
                typeof(BtsmtlSkillCharacterStateBooleanFlowNode),
                node => ((BtsmtlSkillCharacterStateBooleanFlowNode)node).Configure(
                    CharacterStateProviderFields.Grounded,
                    controlOwner));
            EditorGUILayout.LabelField(
                $"只读 C# ControlModule: {definition.ControlModuleId}",
                EditorStyles.miniLabel);

            CharacterInputProfile inputProfile = definition.InputProfile;
            EditorGUILayout.LabelField("Input / TargetData", EditorStyles.miniLabel);
            if (inputProfile)
            {
                for (int i = 0; i < inputProfile.InputValues.Count; i++)
                {
                    CharacterInputValueDefinition input = inputProfile.InputValues[i];
                    if (input == null || string.IsNullOrWhiteSpace(input.InputValueId))
                        continue;
                    Type nodeType = input.ValueType switch
                    {
                        CharacterInputValueType.Bool => typeof(BtsmtlSkillBooleanInputFlowNode),
                        CharacterInputValueType.Float => typeof(BtsmtlSkillScalarInputFlowNode),
                        CharacterInputValueType.Vector2 => typeof(BtsmtlSkillVector2InputFlowNode),
                        _ => null
                    };
                    if (nodeType == null)
                        continue;
                    string inputId = input.InputValueId;
                    DrawProviderButton(
                        $"读取输入 / {inputId}",
                        nodeType,
                        node => ((IBtsmtlSkillInputNode)node).SetInputId(inputId, AssetOwnerId(inputProfile)));
                }
                for (int i = 0; i < inputProfile.ActionRequests.Count; i++)
                {
                    CharacterActionRequestDefinition request = inputProfile.ActionRequests[i];
                    if (request == null || string.IsNullOrWhiteSpace(request.RequestId))
                        continue;
                    string requestId = request.RequestId;
                    DrawProviderButton(
                        $"读取动作请求 / {requestId}",
                        typeof(BtsmtlSkillActionRequestFlowNode),
                        node => ((IBtsmtlSkillInputNode)node).SetInputId(requestId, AssetOwnerId(inputProfile)));
                }
            }
            else
                EditorGUILayout.HelpBox("CharacterInputProfile未配置。", MessageType.Warning);

            CharacterGameplayEffectProfile gameplay = definition.GameplayEffectProfile;
            EditorGUILayout.LabelField("Ability Attribute / GameplayTag", EditorStyles.miniLabel);
            if (!gameplay)
            {
                EditorGUILayout.HelpBox("CharacterGameplayEffectProfile未配置。", MessageType.Warning);
                return;
            }

            string gameplayOwnerId = AssetOwnerId(gameplay);
            for (int i = 0; i < gameplay.AttributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition attribute = gameplay.AttributeDefinitions[i];
                if (!attribute || !attribute.AttributeId.IsValid)
                    continue;
                GameplayAttributeId attributeId = attribute.AttributeId;
                string label = string.IsNullOrWhiteSpace(attribute.DisplayName)
                    ? attribute.name
                    : attribute.DisplayName;
                DrawProviderButton(
                    $"Get Attribute / {label}",
                    typeof(BtsmtlSkillGameplayAttributeFlowNode),
                    node => ((BtsmtlSkillGameplayAttributeFlowNode)node).Configure(attributeId, gameplayOwnerId));
            }

            if (gameplay.TagCatalog)
            {
                for (int i = 0; i < gameplay.TagCatalog.Tags.Count; i++)
                {
                    GameplayTagDefinition tag = gameplay.TagCatalog.Tags[i];
                    if (tag == null || !tag.TagId.IsValid)
                        continue;
                    GameplayTagId tagId = tag.TagId;
                    string label = string.IsNullOrWhiteSpace(tag.DisplayName) ? tagId.Value : tag.DisplayName;
                    DrawProviderButton(
                        $"Get GameplayTag / {label}",
                        typeof(BtsmtlSkillGameplayTagFlowNode),
                        node => ((BtsmtlSkillGameplayTagFlowNode)node).Configure(tagId, gameplayOwnerId));
                }
            }

            for (int i = 0; i < gameplay.EffectDefinitions.Count; i++)
            {
                GameplayEffectDefinition effect = gameplay.EffectDefinitions[i];
                if (!effect)
                    continue;
                GameplayEffectDefinition selected = effect;
                string label = string.IsNullOrWhiteSpace(effect.DisplayName) ? effect.name : effect.DisplayName;
                DrawProviderButton(
                    $"Set Attribute / Apply Effect / {label}",
                    typeof(BtsmtlSkillApplyGameplayEffectFlowNode),
                    node => ((BtsmtlSkillApplyGameplayEffectFlowNode)node).Configure(selected, null, false, gameplayOwnerId));
                DrawProviderButton(
                    $"Set Attribute / Remove Effect / {label}",
                    typeof(BtsmtlSkillRemoveGameplayEffectFlowNode),
                    node => ((BtsmtlSkillRemoveGameplayEffectFlowNode)node).Configure(
                        GameplayEffectRemoveSelector.EffectId,
                        0,
                        selected,
                        null,
                        gameplayOwnerId));
            }
        }

        static void DrawProviderButton(string label, Type nodeType, Action<FlowNode> configure)
        {
            using (new EditorGUI.DisabledScope(s_Graph.isEditorReadOnly || !s_Graph.CanAuthorNodeType(nodeType)))
            {
                if (GUILayout.Button(label))
                    CreateProviderNode(nodeType, configure);
            }
        }

        static void CreateProviderNode(Type nodeType, Action<FlowNode> configure)
        {
            try
            {
                FlowNode created = BtsmtlSkillFlowEditorMutation.Execute(
                    s_Graph,
                    "创建Skill Provider引用",
                    () =>
                    {
                        FlowNode node = (FlowNode)s_Graph.AddNode(nodeType, NextNodePosition());
                        configure(node);
                        return node;
                    });
                GraphEditorUtility.activeElement = created;
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                s_Message = error.Message;
            }
        }

        static Vector2 NextNodePosition()
        {
            int count = s_Graph?.allNodes.Count ?? 0;
            return new Vector2(80f + count % 4 * 240f, 80f + count / 4 * 140f);
        }

        static string AssetOwnerId(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrEmpty(guid) ? string.Empty : $"asset:{guid}";
        }

        static CharacterPipelineDefinition ResolveDefinition(FlowGraph graph)
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterPipelineDefinition");
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!definition)
                    continue;
                IReadOnlyList<BtsmtlSkillFlowGraph> graphs = definition.SkillGraphs;
                for (int graphIndex = 0; graphIndex < graphs.Count; graphIndex++)
                    if (ReferenceEquals(graphs[graphIndex], graph))
                        return definition;
            }
            return null;
        }

        static void CreateVariable(IBtsmtlSkillFlowGraph authoring)
        {
            s_Message = string.Empty;
            if (string.IsNullOrWhiteSpace(s_NewName))
            {
                s_Message = "变量名不能为空。";
                return;
            }
            if (s_Graph.GetGraphSource().localBlackboard.variables.ContainsKey(s_NewName.Trim()))
            {
                s_Message = "变量名已经存在。";
                return;
            }
            if (!TryNormalizeScope(ref s_NewScope, ref s_NewLifetime))
            {
                s_Message = "作用域与生命周期组合无效。";
                return;
            }
            if (!TryGetClrType(s_NewType, out Type type))
            {
                s_Message = "变量类型不受技能Blackboard合同支持。";
                return;
            }
            try
            {
                var declaration = new BtsmtlSkillBlackboardDeclaration(
                    Guid.NewGuid().ToString("N"),
                    s_NewScope,
                    s_NewLifetime);
                BtsmtlSkillBlackboardDeclarations.Add(
                    s_Graph,
                    declaration,
                    s_NewName.Trim(),
                    type,
                    DefaultValue(type));
                s_NewName = "newVariable";
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                s_Message = error.Message;
            }
        }

        static void CreateNode(Variable variable, bool writes)
        {
            if (!BtsmtlSkillFlowEditorMutation.TryGetBlackboardValueType(variable.varType, out BtsmtlSkillBlackboardValueType valueType))
            {
                s_Message = "变量类型不受技能Blackboard节点合同支持。";
                return;
            }
            BtsmtlSkillFlowEditorMutation.CreateBlackboardAccessNode(
                s_Graph,
                variable,
                valueType,
                new Vector2(80f + s_Graph.allNodes.Count % 4 * 220f, 80f + s_Graph.allNodes.Count / 4 * 120f),
                writes);
        }

        static bool TryNormalizeScope(
            ref PipelineBlackboardVariableScope scope,
            ref PipelineBlackboardVariableLifetime lifetime)
        {
            if (scope == PipelineBlackboardVariableScope.Character)
                return false;
            if (scope == PipelineBlackboardVariableScope.State)
                lifetime = PipelineBlackboardVariableLifetime.StateEnterToExit;
            else if (scope == PipelineBlackboardVariableScope.ActionInstance)
                lifetime = PipelineBlackboardVariableLifetime.ActionInstance;
            else if (scope == PipelineBlackboardVariableScope.Frame)
                lifetime = PipelineBlackboardVariableLifetime.Frame;
            else if (scope == PipelineBlackboardVariableScope.Graph &&
                     lifetime != PipelineBlackboardVariableLifetime.Config &&
                     lifetime != PipelineBlackboardVariableLifetime.GraphInstance)
                lifetime = PipelineBlackboardVariableLifetime.GraphInstance;
            return PipelineBlackboardVariablePolicy.IsValid(scope, lifetime);
        }

        static bool TryGetClrType(BtsmtlSkillBlackboardValueType type, out Type clrType)
        {
            clrType = type switch
            {
                BtsmtlSkillBlackboardValueType.Boolean => typeof(bool),
                BtsmtlSkillBlackboardValueType.Integer => typeof(int),
                BtsmtlSkillBlackboardValueType.Number => typeof(float),
                BtsmtlSkillBlackboardValueType.Identity => typeof(string),
                BtsmtlSkillBlackboardValueType.Vector2 => typeof(Vector2),
                BtsmtlSkillBlackboardValueType.Vector3 => typeof(Vector3),
                _ => null
            };
            return clrType != null;
        }

        static object DefaultValue(Type type)
        {
            if (type == typeof(string))
                return string.Empty;
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        static void Release()
        {
            ReleasePanel();
            if (s_Registered)
            {
                GraphEditor.onCurrentGraphChanged -= OnCurrentGraphChanged;
                GraphEditor.onEditorClosed -= Release;
                s_Registered = false;
            }
            s_Editor = null;
            s_Graph = null;
        }

        static void ReleasePanel()
        {
            if (s_Editor != null)
                s_Editor.SetDomainPanel(null, 0f);
            s_Panel?.RemoveFromHierarchy();
            s_Panel = null;
            s_Inspector = null;
        }
    }
}
#endif
