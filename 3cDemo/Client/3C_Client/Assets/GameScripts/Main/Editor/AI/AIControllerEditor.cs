using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Editor;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.AI.Editor
{
    public sealed class AIControllerAuthoringContext : ITreeInspectorBlackboardAuthoringContext
    {
        public AIControllerAuthoringContext(AIControllerDefinition definition)
        {
            Definition = definition;
        }

        public AIControllerDefinition Definition { get; }

        public IReadOnlyList<PipelineBlackboardVariableScope> GetAllowedBlackboardScopes(BaseTree currentTree)
        {
            return new[]
            {
                PipelineBlackboardVariableScope.AIController,
                PipelineBlackboardVariableScope.AITick,
                PipelineBlackboardVariableScope.Graph
            };
        }

        public IEnumerable<BaseTree> GetAdditionalVisibleBlackboardSources(BaseTree currentTree)
        {
            BaseTree root = Definition && Definition.RootTreeAsset ? Definition.RootTreeAsset.Tree : null;
            if (root != null && root != currentTree)
                yield return root;
        }

        public bool IsBlackboardDeclarationTypeAllowed(Type exposedPropertyType, Type valueType)
        {
            return valueType == typeof(bool) ||
                   valueType == typeof(int) ||
                   valueType == typeof(float) ||
                   valueType == typeof(Vector2) ||
                   valueType == typeof(Vector3) ||
                   exposedPropertyType == typeof(AIActorIdExposedProperty) ||
                   exposedPropertyType == typeof(AIActionTargetSnapshotExposedProperty);
        }
    }

    public sealed class AIControllerTreeWindow : BaseTreeWindow
    {
        protected override Type m_TreeInspectorViewType => typeof(AIControllerTreeInspectorView);

        public override void CreateGUI()
        {
            base.CreateGUI();
            titleContent = new GUIContent("AI Controller");
            var identity = new Label();
            identity.style.unityTextAlign = TextAnchor.MiddleRight;
            identity.style.flexGrow = 1f;
            m_NavigationToolbar.Add(identity);
            identity.schedule.Execute(() =>
            {
                var context = AuthoringContext as AIControllerAuthoringContext;
                AIControllerDefinition definition = context?.Definition;
                if (!definition)
                {
                    identity.text = $"Unbound / {Tree?.AuthoringRole}";
                    identity.tooltip = "This AI Tree has no AI Controller Definition authoring context.";
                    return;
                }

                string character = definition.ControlledCharacter ? definition.ControlledCharacter.name : "Missing Character";
                string program = definition.IntentProgram ? "Program Bound" : "Program Missing";
                identity.text = $"{definition.ControllerId} / {character} / {program}";
                identity.tooltip = $"Graph Role: {Tree?.AuthoringRole}";
            }).Every(250);
        }
    }

    public sealed class AIControllerTreeInspectorView : BaseTreeInspectorView
    {
        readonly VisualElement m_ContextPanel;
        readonly ObjectField m_DefinitionField;
        readonly ObjectField m_PerceptionField;
        readonly ObjectField m_CharacterField;
        readonly Label m_InputContract;
        readonly Label m_ProgramStatus;

        public AIControllerTreeInspectorView()
        {
            m_ContextPanel = new VisualElement();
            m_ContextPanel.style.paddingLeft = 6f;
            m_ContextPanel.style.paddingRight = 6f;
            m_ContextPanel.style.paddingTop = 6f;
            m_ContextPanel.style.paddingBottom = 6f;
            m_ContextPanel.Add(new Label("AI Controller Context"));

            m_DefinitionField = CreateReadOnlyField<AIControllerDefinition>("Definition");
            m_PerceptionField = CreateReadOnlyField<AIPerceptionProfile>("Perception");
            m_CharacterField = CreateReadOnlyField<CharacterPipelineDefinition>("Character");
            m_InputContract = new Label();
            m_ProgramStatus = new Label();
            m_ContextPanel.Add(m_DefinitionField);
            m_ContextPanel.Add(m_PerceptionField);
            m_ContextPanel.Add(m_CharacterField);
            m_ContextPanel.Add(m_InputContract);
            m_ContextPanel.Add(m_ProgramStatus);
            Insert(0, m_ContextPanel);
            RefreshContext(null);
        }

        public override void SetAuthoringContext(object authoringContext)
        {
            base.SetAuthoringContext(authoringContext);
            RefreshContext((authoringContext as AIControllerAuthoringContext)?.Definition);
        }

        static ObjectField CreateReadOnlyField<T>(string label) where T : UnityEngine.Object
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(T),
                allowSceneObjects = false
            };
            field.SetEnabled(false);
            return field;
        }

        void RefreshContext(AIControllerDefinition definition)
        {
            m_DefinitionField.value = definition;
            m_PerceptionField.value = definition ? definition.PerceptionProfile : null;
            m_CharacterField.value = definition ? definition.ControlledCharacter : null;
            if (!definition)
            {
                m_InputContract.text = "Character Input: unavailable without Definition context";
                m_ProgramStatus.text = "AI Program: unavailable";
                return;
            }

            CharacterInputProfile input = definition.ControlledCharacter ? definition.ControlledCharacter.InputProfile : null;
            m_InputContract.text = input
                ? $"Character Input: {input.InputValues.Count} values / {input.ActionRequests.Count} requests"
                : "Character Input: missing";
            m_ProgramStatus.text = definition.IntentProgram
                ? $"AI Program: {definition.IntentProgram.ProgramId}"
                : "AI Program: missing";
        }
    }

    public static class AIControllerTreeWindowUtility
    {
        public static BaseTreeWindow Open(AIControllerDefinition definition)
        {
            if (!definition || !definition.RootTreeAsset)
                return null;
            return TreeWindowUtility.OpenTree(
                definition.RootTreeAsset,
                new AIControllerAuthoringContext(definition));
        }
    }

    [CustomEditor(typeof(AIControllerDefinition))]
    public sealed class AIControllerDefinitionEditor : UnityEditor.Editor
    {
        enum ArtifactStatus : byte
        {
            Missing = 1,
            Invalid = 2,
            Unchecked = 3,
            NeedsCompile = 4,
            Ready = 5,
            Stale = 6
        }

        readonly List<string> m_Errors = new List<string>();
        string m_CompileStatus = string.Empty;
        MessageType m_CompileStatusType = MessageType.None;
        ArtifactStatus m_ArtifactStatus;
        string m_ArtifactStatusText = string.Empty;

        void OnEnable()
        {
            RefreshLightweightStatus();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ControllerId"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RootTreeAsset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ControlledCharacter"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PerceptionProfile"));
            bool configurationChanged = EditorGUI.EndChangeCheck();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_IntentProgram"));
            serializedObject.ApplyModifiedProperties();
            if (configurationChanged)
            {
                m_ArtifactStatus = ArtifactStatus.NeedsCompile;
                m_ArtifactStatusText = "AI Controller configuration changed and requires compilation.";
            }

            var definition = (AIControllerDefinition)target;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(definition.RootTreeAsset ? "Open AI Tree" : "Create AI Tree"))
            {
                if (!definition.RootTreeAsset)
                    CreateRootTree(definition);
                AIControllerTreeWindowUtility.Open(definition);
            }
            if (GUILayout.Button("Validate"))
            {
                m_Errors.Clear();
                definition.CollectConfigurationErrors(m_Errors);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Agent Controller"))
                AgentCharacterControllerSynthesisWindow.Open(definition);
            if (GUILayout.Button("Refresh Program Status"))
                RefreshExactStatus(definition);
            EditorGUILayout.EndHorizontal();
            using (new EditorGUI.DisabledScope(!definition.RootTreeAsset || !definition.ControlledCharacter || !definition.PerceptionProfile))
            {
                if (GUILayout.Button("Compile AI Intent Program"))
                {
                    try
                    {
                        AIIntentProgramBuildService.CompileAndPublish(definition);
                        m_CompileStatus = "AI Intent Program compiled and bound.";
                        m_CompileStatusType = MessageType.Info;
                        m_ArtifactStatus = ArtifactStatus.Ready;
                        m_ArtifactStatusText = m_CompileStatus;
                    }
                    catch (Exception exception)
                    {
                        m_CompileStatus = exception.Message;
                        m_CompileStatusType = MessageType.Error;
                        m_ArtifactStatus = ArtifactStatus.NeedsCompile;
                    }
                }
            }
            EditorGUILayout.HelpBox(m_ArtifactStatusText, StatusMessageType(m_ArtifactStatus));
            if (!string.IsNullOrEmpty(m_CompileStatus))
                EditorGUILayout.HelpBox(m_CompileStatus, m_CompileStatusType);
            for (int i = 0; i < m_Errors.Count; i++)
                EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
        }

        void RefreshLightweightStatus()
        {
            AIIntentProgramPublishedStatus published = AIIntentProgramBuildService.InspectPublishedHeader(
                target as AIControllerDefinition,
                out m_ArtifactStatusText);
            m_ArtifactStatus = published switch
            {
                AIIntentProgramPublishedStatus.Missing => ArtifactStatus.Missing,
                AIIntentProgramPublishedStatus.Invalid => ArtifactStatus.Invalid,
                _ => ArtifactStatus.Unchecked
            };
        }

        void RefreshExactStatus(AIControllerDefinition definition)
        {
            bool current = AIIntentProgramBuildService.IsCurrent(definition, out m_ArtifactStatusText);
            m_ArtifactStatus = current
                ? ArtifactStatus.Ready
                : definition && definition.IntentProgram
                    ? ArtifactStatus.Stale
                    : ArtifactStatus.Missing;
        }

        static MessageType StatusMessageType(ArtifactStatus status) => status switch
        {
            ArtifactStatus.Ready => MessageType.Info,
            ArtifactStatus.Invalid => MessageType.Error,
            ArtifactStatus.Missing => MessageType.Warning,
            ArtifactStatus.Stale => MessageType.Warning,
            ArtifactStatus.NeedsCompile => MessageType.Warning,
            _ => MessageType.None
        };

        static void CreateRootTree(AIControllerDefinition definition)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string directory = System.IO.Path.GetDirectoryName(definitionPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("AI Controller Definition must be saved before creating its RootTree.");
            var tree = new AIControllerTree();
            tree.name = definition.name + " AI";
            tree.CheckInit();
            var asset = CreateInstance<BaseTreeAsset>();
            asset.SetTree(tree);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{definition.name}.AIRootTree.asset");
            AssetDatabase.CreateAsset(asset, path);
            Undo.RecordObject(definition, "Bind AI RootTree");
            definition.SetRootTreeAsset(asset);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
        }
    }

    public sealed class AIIntentBindingNodeView : BaseNodeView
    {
        readonly Label m_Binding;

        public AIIntentBindingNodeView(BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow)
        {
            m_Binding = new Label();
            m_Binding.style.unityTextAlign = TextAnchor.MiddleRight;
            m_Binding.style.flexGrow = 1f;
            m_Binding.AddManipulator(new DropdownMenuManipulator(BuildMenu, MouseButton.LeftMouse));
            titleContainer.Add(m_Binding);
            RefreshLabel();
        }

        void BuildMenu(DropdownMenu menu)
        {
            AIControllerDefinition definition = (m_TreeWindow.AuthoringContext as AIControllerAuthoringContext)?.Definition;
            CharacterSimulationProgram program = definition?.ControlledCharacter?.SimulationProgram
                ? definition.ControlledCharacter.SimulationProgram.Load()
                : null;
            if (program == null)
                return;
            ProgramCatalogEntryKind kind = m_Node is SubmitActionRequestNode
                ? ProgramCatalogEntryKind.InputRequest
                : ProgramCatalogEntryKind.InputValue;
            IEnumerable<ProgramCatalogEntry> entries = program.CatalogEntries
                .Where(entry => entry.Kind == kind)
                .Where(entry => m_Node is not WriteActionTargetSnapshotNode || IsActionTarget(program, entry))
                .Where(entry => m_Node is not WriteContinuousInputNode || !IsActionTarget(program, entry))
                .OrderBy(entry => entry.Identity, StringComparer.Ordinal);
            foreach (ProgramCatalogEntry entry in entries)
            {
                string id = StripCatalogPrefix(entry.Identity, kind);
                string label = kind == ProgramCatalogEntryKind.InputRequest
                    ? $"{id} ({RequestTimingClass(program, entry)})"
                    : id;
                menu.AppendAction(label, _ =>
                {
                    m_Node.ApplyModify("Bind AI Intent", () =>
                    {
                        if (m_Node is WriteContinuousInputNode continuous)
                            continuous.ConfigureInput(id, InputPortType(program, entry));
                        else if (m_Node is WriteActionTargetSnapshotNode target)
                            target.ConfigureInput(id);
                        else if (m_Node is SubmitActionRequestNode request)
                            request.ConfigureRequest(id, request.BufferSeconds, request.Priority, request.RepeatPolicy);
                        m_Node.GetNewSerializedTree();
                        RefreshLabel();
                    });
                });
            }
        }

        void RefreshLabel()
        {
            m_Binding.text = m_Node switch
            {
                WriteContinuousInputNode node => node.InputId,
                WriteActionTargetSnapshotNode node => node.InputId,
                SubmitActionRequestNode node => RequestLabel(node),
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(m_Binding.text))
                m_Binding.text = "Select Binding";
        }

        string RequestLabel(SubmitActionRequestNode node)
        {
            if (string.IsNullOrEmpty(node.RequestId))
                return string.Empty;
            AIControllerDefinition definition = (m_TreeWindow.AuthoringContext as AIControllerAuthoringContext)?.Definition;
            CharacterSimulationProgram program = definition?.ControlledCharacter?.SimulationProgram
                ? definition.ControlledCharacter.SimulationProgram.Load()
                : null;
            ProgramCatalogEntry entry = program?.CatalogEntries.FirstOrDefault(value =>
                value.Kind == ProgramCatalogEntryKind.InputRequest &&
                string.Equals(StripCatalogPrefix(value.Identity, value.Kind), node.RequestId, StringComparison.Ordinal));
            return entry != null
                ? $"{node.RequestId} ({RequestTimingClass(program, entry)})"
                : $"{node.RequestId} (Unresolved)";
        }

        static bool IsActionTarget(CharacterSimulationProgram program, ProgramCatalogEntry entry)
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, "ValueType", StringComparison.Ordinal) || field.Kind != ProgramCatalogFieldKind.Constant)
                    continue;
                ProgramConstant value = program.Constants[field.ConstantIndex];
                return value.Kind == ProgramConstantKind.Int32 &&
                       value.Int32 == (int)ProgramInputValueKind.ActionTargetSnapshot;
            }
            return false;
        }

        static Type InputPortType(CharacterSimulationProgram program, ProgramCatalogEntry entry)
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, "ValueType", StringComparison.Ordinal) || field.Kind != ProgramCatalogFieldKind.Constant)
                    continue;
                ProgramConstant value = program.Constants[field.ConstantIndex];
                if (value.Kind != ProgramConstantKind.Int32)
                    break;
                return (ProgramInputValueKind)value.Int32 switch
                {
                    ProgramInputValueKind.Boolean => typeof(BoolPropertyPort),
                    ProgramInputValueKind.Scalar => typeof(FloatPropertyPort),
                    ProgramInputValueKind.Vector2 => typeof(Vector2PropertyPort),
                    ProgramInputValueKind.Vector3 => typeof(Vector3PropertyPort),
                    ProgramInputValueKind.Yaw => typeof(FloatPropertyPort),
                    _ => throw new InvalidOperationException($"Character input '{entry.Identity}' cannot bind Write Continuous Input.")
                };
            }
            throw new InvalidOperationException($"Character input '{entry.Identity}' has no valid ValueType field.");
        }

        static CharacterActionRequestTimingClass RequestTimingClass(
            CharacterSimulationProgram program,
            ProgramCatalogEntry entry)
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, "TimingClass", StringComparison.Ordinal) ||
                    field.Kind != ProgramCatalogFieldKind.Constant)
                    continue;
                ProgramConstant value = program.Constants[field.ConstantIndex];
                object candidate = Enum.ToObject(typeof(CharacterActionRequestTimingClass), value.Int32);
                if (value.Kind == ProgramConstantKind.Int32 &&
                    Enum.IsDefined(typeof(CharacterActionRequestTimingClass), candidate))
                {
                    return (CharacterActionRequestTimingClass)candidate;
                }
                break;
            }
            throw new InvalidOperationException($"Character request '{entry.Identity}' has no valid TimingClass field.");
        }

        static string StripCatalogPrefix(string identity, ProgramCatalogEntryKind kind)
        {
            string prefix = kind == ProgramCatalogEntryKind.InputRequest ? "input:request:" : "input:value:";
            return identity.StartsWith(prefix, StringComparison.Ordinal) ? identity.Substring(prefix.Length) : identity;
        }
    }

    public sealed class AIMemoryNodeView : BaseNodeView
    {
        readonly Label m_Declaration;

        public AIMemoryNodeView(BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow)
        {
            m_Declaration = new Label();
            m_Declaration.style.unityTextAlign = TextAnchor.MiddleRight;
            m_Declaration.style.flexGrow = 1f;
            m_Declaration.AddManipulator(new DropdownMenuManipulator(BuildMenu, MouseButton.LeftMouse));
            titleContainer.Add(m_Declaration);
            RefreshLabel();
        }

        void BuildMenu(DropdownMenu menu)
        {
            IEnumerable<BaseExposedProperty> declarations = m_TreeWindow.GetVisibleExposedProperties()
                .Where(value => value != null && TryGetKind(value, out _))
                .Where(value => value.BlackboardScope == PipelineBlackboardVariableScope.AIController ||
                                value.BlackboardScope == PipelineBlackboardVariableScope.AITick ||
                                value.BlackboardScope == PipelineBlackboardVariableScope.Graph)
                .OrderBy(value => value.BlackboardScope)
                .ThenBy(value => value.Name, StringComparer.Ordinal);
            foreach (BaseExposedProperty declaration in declarations)
            {
                TryGetKind(declaration, out AIMemoryValueKind kind);
                string path = $"{declaration.BlackboardScope}/{declaration.Name}";
                menu.AppendAction(path, _ => Bind(declaration, kind), _ =>
                    string.Equals(CurrentReference.DeclarationOwnerId, declaration.DeclarationOwnerId, StringComparison.Ordinal) &&
                    string.Equals(CurrentReference.DeclarationId, declaration.DeclarationId, StringComparison.Ordinal)
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
            }
        }

        void Bind(BaseExposedProperty declaration, AIMemoryValueKind kind)
        {
            m_Node.ApplyModify("Bind AI Memory", () =>
            {
                if (m_Node is ReadAIMemoryNode read)
                {
                    if (OutputPropertyPorts.TryGetValue("m_Value", out PropertyPortView portView))
                        TreeView.DeleteElements(portView.connections);
                    read.ConfigureAuthoring(declaration, kind);
                    if (OutputPropertyPorts.TryGetValue("m_Value", out portView))
                        portView.SetPropertyPort(read.ValuePort);
                }
                else if (m_Node is WriteAIMemoryNode write)
                {
                    if (InputPropertyPorts.TryGetValue("m_Value", out PropertyPortView portView))
                        TreeView.DeleteElements(portView.connections);
                    write.ConfigureAuthoring(declaration, kind);
                    if (InputPropertyPorts.TryGetValue("m_Value", out portView))
                        portView.SetPropertyPort(write.ValuePort);
                }
                m_Node.GetNewSerializedTree();
                RefreshLabel();
                Refresh();
            });
        }

        PipelineBlackboardVariableReference CurrentReference => m_Node is ReadAIMemoryNode read
            ? read.BlackboardVariable
            : ((WriteAIMemoryNode)m_Node).BlackboardVariable;

        void RefreshLabel()
        {
            m_Declaration.text = CurrentReference.IsValid
                ? CurrentReference.DisplayKey
                : "Select AI Memory";
        }

        static bool TryGetKind(BaseExposedProperty declaration, out AIMemoryValueKind kind)
        {
            kind = declaration switch
            {
                BaseExposedProperty<bool> => AIMemoryValueKind.Boolean,
                BaseExposedProperty<int> => AIMemoryValueKind.Integer,
                BaseExposedProperty<float> => AIMemoryValueKind.Scalar,
                BaseExposedProperty<Vector2> => AIMemoryValueKind.Vector2,
                BaseExposedProperty<Vector3> => AIMemoryValueKind.Vector3,
                AIActorIdExposedProperty => AIMemoryValueKind.ActorId,
                AIActionTargetSnapshotExposedProperty => AIMemoryValueKind.ActionTargetSnapshot,
                _ => default
            };
            return kind != default;
        }
    }
}

namespace TreeDesigner.Editor
{
    public partial class TreeWindowUtilityInstance
    {
        public BaseTreeWindow OpenAIControllerTreeWindow(BaseTree tree = null)
        {
            return TreeWindowUtility.GetWindow<ThirdPersonCharacter.AI.Editor.AIControllerTreeWindow>(tree);
        }
    }
}
