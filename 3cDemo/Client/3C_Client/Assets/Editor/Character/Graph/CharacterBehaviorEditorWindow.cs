using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior;
using ThirdPersonCharacterBehavior.Authoring;
using ThirdPersonCharacterBehavior.Editor.ActionBranch;
using ThirdPersonCharacterBehavior.Editor.ActionTimeline;
using ThirdPersonCharacterConfig;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacterBehavior.Editor.Graph
{
    public readonly struct CommittedActionLeafCatalogNavigationEntry
    {
        public CommittedActionLeafCatalogNavigationEntry(
            string actionId,
            string displayLabel,
            CharacterActionDefinitionSO definition,
            string diagnostic)
        {
            ActionId = actionId ?? string.Empty;
            DisplayLabel = displayLabel ?? string.Empty;
            Definition = definition;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public string ActionId { get; }
        public string DisplayLabel { get; }
        public CharacterActionDefinitionSO Definition { get; }
        public string Diagnostic { get; }
        public bool IsValid => Definition != null &&
                               !string.IsNullOrWhiteSpace(ActionId) &&
                               string.IsNullOrWhiteSpace(Diagnostic);
    }

    public sealed class CommittedActionLeafCatalogNavigationSnapshot
    {
        readonly CommittedActionLeafCatalogNavigationEntry[] entries;
        readonly CommittedActionLeafCatalogNavigationEntry[] validEntries;
        readonly string[] diagnostics;

        CommittedActionLeafCatalogNavigationSnapshot(
            IReadOnlyList<CommittedActionLeafCatalogNavigationEntry> entries,
            IReadOnlyList<string> diagnostics)
        {
            this.entries = ToArray(entries);
            this.diagnostics = ToArray(diagnostics);
            List<CommittedActionLeafCatalogNavigationEntry> valid = new List<CommittedActionLeafCatalogNavigationEntry>();
            for (int i = 0; i < this.entries.Length; i++)
            {
                if (this.entries[i].IsValid)
                    valid.Add(this.entries[i]);
            }

            validEntries = valid.ToArray();
        }

        public IReadOnlyList<CommittedActionLeafCatalogNavigationEntry> Entries => entries;
        public IReadOnlyList<CommittedActionLeafCatalogNavigationEntry> ValidEntries => validEntries;
        public IReadOnlyList<string> Diagnostics => diagnostics;
        public bool HasErrors => diagnostics.Length > 0;

        public bool TryGetSingleValidEntry(out CommittedActionLeafCatalogNavigationEntry entry)
        {
            if (!HasErrors && validEntries.Length == 1)
            {
                entry = validEntries[0];
                return true;
            }

            entry = default;
            return false;
        }

        public string DescribeDiagnostics()
        {
            return string.Join(Environment.NewLine, diagnostics);
        }

        public static CommittedActionLeafCatalogNavigationSnapshot FromConfig(CharacterConfigSO config)
        {
            if (config == null)
            {
                return new CommittedActionLeafCatalogNavigationSnapshot(
                    Array.Empty<CommittedActionLeafCatalogNavigationEntry>(),
                    new[] { "Character config is missing for CommittedActionLeaf catalog navigation." });
            }

            if (config.ActionCatalog == null)
            {
                return new CommittedActionLeafCatalogNavigationSnapshot(
                    Array.Empty<CommittedActionLeafCatalogNavigationEntry>(),
                    new[] { "Character config action catalog is missing." });
            }

            return FromCatalog(config.ActionCatalog);
        }

        public static CommittedActionLeafCatalogNavigationSnapshot FromCatalog(CharacterActionCatalogSO catalog)
        {
            List<CommittedActionLeafCatalogNavigationEntry> entries =
                new List<CommittedActionLeafCatalogNavigationEntry>();
            List<string> diagnostics = new List<string>();
            if (catalog == null)
            {
                diagnostics.Add("Action catalog is missing.");
                return new CommittedActionLeafCatalogNavigationSnapshot(entries, diagnostics);
            }

            IReadOnlyList<CharacterActionDefinitionSO> definitions = catalog.Definitions;
            if (definitions.Count == 0)
                diagnostics.Add("Action catalog is empty.");

            HashSet<string> actionIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> duplicateIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Count; i++)
            {
                CharacterActionDefinitionSO definition = definitions[i];
                if (definition == null)
                {
                    string diagnostic = $"Catalog entry {i} is missing action definition.";
                    diagnostics.Add(diagnostic);
                    entries.Add(new CommittedActionLeafCatalogNavigationEntry(string.Empty, $"Missing Entry {i}", null, diagnostic));
                    continue;
                }

                string actionId = definition.ActionState.Value;
                string entryDiagnostic = string.Empty;
                if (string.IsNullOrWhiteSpace(actionId))
                {
                    entryDiagnostic = $"Catalog entry {i} action id is missing.";
                    diagnostics.Add(entryDiagnostic);
                }
                else if (!actionIds.Add(actionId) && duplicateIds.Add(actionId))
                {
                    diagnostics.Add($"Catalog duplicates action id '{actionId}'.");
                }

                entries.Add(new CommittedActionLeafCatalogNavigationEntry(
                    actionId,
                    BuildDisplayLabel(actionId, definition),
                    definition,
                    entryDiagnostic));
            }

            return new CommittedActionLeafCatalogNavigationSnapshot(entries, diagnostics);
        }

        static string BuildDisplayLabel(string actionId, CharacterActionDefinitionSO definition)
        {
            string assetName = definition != null ? definition.name : "Missing ActionDefinition";
            return string.IsNullOrWhiteSpace(actionId)
                ? assetName
                : $"{actionId} | {assetName}";
        }

        static T[] ToArray<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<T>();

            T[] array = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
                array[i] = source[i];
            return array;
        }
    }

    public sealed class CharacterBehaviorEditorWindow : EditorWindow
    {
        const string FormalDodgeActionPath = "Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset";
        const string FormalCharacterConfigPath = "Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset";
        const string FormalBehaviorAuthoringPath = "Assets/Configs/3C/Behavior/DefaultCharacterBehaviorAuthoring.asset";
        const string FormalBehaviorRuntimePath = "Assets/Configs/3C/Behavior/DefaultCharacterBehaviorRuntimeDefinition.asset";

        enum SourceMode
        {
            BehaviorSource,
            CommittedActionBranch
        }

        SourceMode mode;
        CharacterConfigSO characterConfig;
        CharacterBehaviorAuthoringAsset behaviorAsset;
        CharacterActionDefinitionSO actionDefinition;
        SerializedObject serializedActionDefinition;
        CommittedActionBranchSerializedAdapter branchAdapter;
        CommittedActionBranchRefPortedGraphAdapter branchGraphAdapter;
        CharacterBehaviorRefPortedGraphView graphView;
        IMGUIContainer branchInspector;
        HelpBox diagnostics;
        ObjectField assetField;
        ObjectField characterConfigField;
        VisualElement actionCatalogNavigationPanel;
        IReadOnlyList<CommittedActionLeafCatalogNavigationEntry> pendingActionNavigationEntries =
            Array.Empty<CommittedActionLeafCatalogNavigationEntry>();
        string selectedBranchNodeId = string.Empty;
        bool reloadScheduled;

        [MenuItem("Tools/3C/Character Behavior Editor")]
        public static void Open()
        {
            CharacterBehaviorEditorWindow window =
                GetWindow<CharacterBehaviorEditorWindow>("Character Behavior Editor");
            window.SetMode(SourceMode.BehaviorSource);
        }

        void OnEnable()
        {
            BuildUi();
        }

        void OnDisable()
        {
            if (graphView != null)
            {
                graphView.NodeSelected -= SelectGraphNode;
                graphView.NodeOpened -= OpenGraphNode;
            }
        }

        void BuildUi()
        {
            if (graphView != null)
            {
                graphView.NodeSelected -= SelectGraphNode;
                graphView.NodeOpened -= OpenGraphNode;
            }

            rootVisualElement.Clear();

            Toolbar toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() => SetMode(SourceMode.BehaviorSource)) { text = "Behavior Source" });
            toolbar.Add(new ToolbarButton(() => SetMode(SourceMode.CommittedActionBranch)) { text = "Committed Branch" });
            assetField = new ObjectField
            {
                allowSceneObjects = false,
                style = { minWidth = 320 }
            };
            assetField.RegisterValueChangedCallback(evt =>
            {
                if (mode == SourceMode.BehaviorSource)
                    behaviorAsset = evt.newValue as CharacterBehaviorAuthoringAsset;
                else
                    actionDefinition = evt.newValue as CharacterActionDefinitionSO;
                ReloadGraph();
            });
            toolbar.Add(assetField);
            characterConfigField = new ObjectField
            {
                label = "Character Config",
                objectType = typeof(CharacterConfigSO),
                allowSceneObjects = false,
                style = { minWidth = 320 }
            };
            characterConfigField.RegisterValueChangedCallback(evt =>
            {
                characterConfig = evt.newValue as CharacterConfigSO;
                HideActionCatalogNavigationPanel();
            });
            toolbar.Add(characterConfigField);
            toolbar.Add(new ToolbarButton(SaveGraph) { text = "Save" });
            toolbar.Add(new ToolbarButton(ValidateGraph) { text = "Validate" });
            toolbar.Add(new ToolbarButton(InitializeBranchTemplate) { text = "Initialize Branch" });
            toolbar.Add(new ToolbarButton(InitializeDodgeBranchTemplate) { text = "Initialize Dodge Template" });
            toolbar.Add(new ToolbarButton(OpenSelectedTimeline) { text = "Open Committed Action Timeline" });
            rootVisualElement.Add(toolbar);

            diagnostics = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            rootVisualElement.Add(diagnostics);

            actionCatalogNavigationPanel = new VisualElement
            {
                style =
                {
                    display = DisplayStyle.None,
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };
            rootVisualElement.Add(actionCatalogNavigationPanel);

            VisualElement content = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1
                }
            };
            rootVisualElement.Add(content);

            branchInspector = new IMGUIContainer(DrawBranchInspector)
            {
                style =
                {
                    width = 340,
                    minWidth = 280,
                    flexShrink = 0
                }
            };
            content.Add(branchInspector);

            graphView = new CharacterBehaviorRefPortedGraphView();
            graphView.NodeSelected += SelectGraphNode;
            graphView.NodeOpened += OpenGraphNode;
            content.Add(graphView);

            ConfigureAssetField();
            ReloadGraph();
        }

        void SetMode(SourceMode nextMode)
        {
            mode = nextMode;
            HideActionCatalogNavigationPanel();
            if (rootVisualElement.childCount == 0)
                BuildUi();
            else
            {
                ConfigureAssetField();
                ReloadGraph();
            }
        }

        void ConfigureAssetField()
        {
            if (assetField == null)
                return;

            if (mode == SourceMode.BehaviorSource)
            {
                assetField.objectType = typeof(CharacterBehaviorAuthoringAsset);
                if (behaviorAsset == null)
                    behaviorAsset = AssetDatabase.LoadAssetAtPath<CharacterBehaviorAuthoringAsset>(FormalBehaviorAuthoringPath);
                assetField.SetValueWithoutNotify(behaviorAsset);
            }
            else
            {
                assetField.objectType = typeof(CharacterActionDefinitionSO);
                if (actionDefinition == null)
                    actionDefinition = AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(FormalDodgeActionPath);
                assetField.SetValueWithoutNotify(actionDefinition);
            }

            ConfigureCharacterConfigField();
        }

        void ConfigureCharacterConfigField()
        {
            if (characterConfigField == null)
                return;

            bool show = mode == SourceMode.BehaviorSource;
            characterConfigField.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
                return;

            if (characterConfig == null)
                characterConfig = AssetDatabase.LoadAssetAtPath<CharacterConfigSO>(FormalCharacterConfigPath);
            characterConfigField.SetValueWithoutNotify(characterConfig);
        }

        void ReloadGraph()
        {
            if (graphView == null)
                return;

            if (mode == SourceMode.BehaviorSource)
            {
                branchAdapter = null;
                branchGraphAdapter = null;
                selectedBranchNodeId = string.Empty;
                graphView.Populate(behaviorAsset);
                SetBranchInspectorVisible(false);
                diagnostics.text = behaviorAsset == null
                    ? $"Formal behavior source authoring asset not found at {FormalBehaviorAuthoringPath}."
                    : $"Editing behavior source topology {AssetDatabase.GetAssetPath(behaviorAsset)} -> {FormalBehaviorRuntimePath}";
                diagnostics.messageType = behaviorAsset == null ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
                return;
            }

            if (actionDefinition == null)
            {
                diagnostics.text = $"Formal Dodge action definition not found at {FormalDodgeActionPath}.";
                diagnostics.messageType = HelpBoxMessageType.Warning;
                graphView.Populate((ICharacterBehaviorRefPortedGraphAdapter)null);
                SetBranchInspectorVisible(true);
                return;
            }

            serializedActionDefinition = new SerializedObject(actionDefinition);
            branchAdapter = new CommittedActionBranchSerializedAdapter(actionDefinition, serializedActionDefinition);
            branchGraphAdapter = new CommittedActionBranchRefPortedGraphAdapter(branchAdapter);
            EnsureBranchSelection();
            graphView.Populate(branchGraphAdapter, selectedBranchNodeId);
            SetBranchInspectorVisible(true);
            branchInspector?.MarkDirtyRepaint();
            diagnostics.text = $"Editing committed action branch {AssetDatabase.GetAssetPath(actionDefinition)}";
            diagnostics.messageType = HelpBoxMessageType.Info;
        }

        void EnsureBranchSelection()
        {
            if (branchAdapter == null)
                return;
            if (!string.IsNullOrWhiteSpace(selectedBranchNodeId) &&
                branchAdapter.TryGetNodeProperty(selectedBranchNodeId, out _, out _))
                return;

            selectedBranchNodeId = string.Empty;
            CommittedActionBranchEditorSnapshot snapshot = branchAdapter.Capture();
            if (!string.IsNullOrWhiteSpace(snapshot.RootNodeId) &&
                branchAdapter.TryGetNodeProperty(snapshot.RootNodeId, out _, out _))
            {
                selectedBranchNodeId = snapshot.RootNodeId;
                return;
            }

            if (branchAdapter.TryGetTimelineNodeId(CommittedActionTimelineVariant.Directional, out string timelineNodeId) ||
                branchAdapter.TryGetTimelineNodeId(CommittedActionTimelineVariant.Backstep, out timelineNodeId))
                selectedBranchNodeId = timelineNodeId;
        }

        void SelectGraphNode(string nodeId)
        {
            if (mode != SourceMode.CommittedActionBranch)
                return;

            selectedBranchNodeId = nodeId ?? string.Empty;
            if (branchAdapter == null ||
                !branchAdapter.TryGetNodeProperty(selectedBranchNodeId, out SerializedProperty node, out _))
                return;

            CommittedActionNodeKind kind = (CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex;
            diagnostics.text = kind == CommittedActionNodeKind.Timeline
                ? $"Selected Timeline node {selectedBranchNodeId}. Use Open Committed Action Timeline to edit it in the dedicated timeline window."
                : $"Selected Branch node {selectedBranchNodeId}.";
            diagnostics.messageType = HelpBoxMessageType.Info;
            branchInspector?.MarkDirtyRepaint();
        }

        void OpenGraphNode(string nodeId)
        {
            if (mode == SourceMode.BehaviorSource)
                OpenBehaviorSourceNode(nodeId);
        }

        bool OpenBehaviorSourceNode(string nodeId)
        {
            if (graphView != null && !graphView.TryGetNodeView(nodeId, out _))
                return false;

            if (!IsCommittedActionLeafNode(behaviorAsset, nodeId))
                return false;

            CommittedActionLeafCatalogNavigationSnapshot snapshot =
                CommittedActionLeafCatalogNavigationSnapshot.FromConfig(characterConfig);
            if (snapshot.HasErrors)
            {
                diagnostics.text = snapshot.DescribeDiagnostics();
                diagnostics.messageType = HelpBoxMessageType.Warning;
                return false;
            }

            if (snapshot.TryGetSingleValidEntry(out CommittedActionLeafCatalogNavigationEntry entry))
                return OpenActionCatalogNavigationEntry(entry);

            ShowActionCatalogNavigationPanel(snapshot);
            diagnostics.text = $"Select committed action from catalog ({snapshot.ValidEntries.Count} actions).";
            diagnostics.messageType = HelpBoxMessageType.Info;
            return false;
        }

        bool OpenActionCatalogNavigationEntry(CommittedActionLeafCatalogNavigationEntry entry)
        {
            if (!entry.IsValid)
            {
                diagnostics.text = string.IsNullOrWhiteSpace(entry.Diagnostic)
                    ? "Selected catalog entry is invalid."
                    : entry.Diagnostic;
                diagnostics.messageType = HelpBoxMessageType.Warning;
                return false;
            }

            actionDefinition = entry.Definition;
            selectedBranchNodeId = string.Empty;
            HideActionCatalogNavigationPanel();
            SetMode(SourceMode.CommittedActionBranch);
            return true;
        }

        void ShowActionCatalogNavigationPanel(CommittedActionLeafCatalogNavigationSnapshot snapshot)
        {
            if (actionCatalogNavigationPanel == null)
                return;

            pendingActionNavigationEntries = snapshot.ValidEntries;
            actionCatalogNavigationPanel.Clear();
            actionCatalogNavigationPanel.style.display = DisplayStyle.Flex;
            actionCatalogNavigationPanel.Add(new Label("Committed Actions")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginRight = 8
                }
            });

            for (int i = 0; i < pendingActionNavigationEntries.Count; i++)
            {
                CommittedActionLeafCatalogNavigationEntry entry = pendingActionNavigationEntries[i];
                Button button = new Button(() => OpenActionCatalogNavigationEntry(entry))
                {
                    text = entry.DisplayLabel,
                    tooltip = AssetDatabase.GetAssetPath(entry.Definition)
                };
                button.style.marginRight = 4;
                actionCatalogNavigationPanel.Add(button);
            }
        }

        void HideActionCatalogNavigationPanel()
        {
            pendingActionNavigationEntries = Array.Empty<CommittedActionLeafCatalogNavigationEntry>();
            if (actionCatalogNavigationPanel == null)
                return;

            actionCatalogNavigationPanel.Clear();
            actionCatalogNavigationPanel.style.display = DisplayStyle.None;
        }

        void SaveGraph()
        {
            if (mode == SourceMode.BehaviorSource)
            {
                if (behaviorAsset == null)
                    return;

                Undo.RecordObject(behaviorAsset, "Character Behavior Editor: Save Source Topology");
                graphView.WriteTo(behaviorAsset);
                EditorUtility.SetDirty(behaviorAsset);
                CharacterBehaviorAuthoringCompilerResult compile =
                    ThirdPersonCharacterBehavior.Authoring.CharacterBehaviorAuthoringCompiler.Compile(behaviorAsset);
                if (!compile.Success)
                {
                    diagnostics.text = string.Join("\n", compile.Errors);
                    diagnostics.messageType = HelpBoxMessageType.Error;
                    return;
                }

                CharacterBehaviorRuntimeDefinitionSO runtimeDefinition =
                    AssetDatabase.LoadAssetAtPath<CharacterBehaviorRuntimeDefinitionSO>(FormalBehaviorRuntimePath);
                if (runtimeDefinition == null)
                {
                    diagnostics.text = $"Formal behavior runtime definition not found at {FormalBehaviorRuntimePath}.";
                    diagnostics.messageType = HelpBoxMessageType.Error;
                    return;
                }

                Undo.RecordObject(runtimeDefinition, "Character Behavior Editor: Compile Runtime Definition");
                runtimeDefinition.SetDefinition(compile.RuntimeDefinition);
                EditorUtility.SetDirty(runtimeDefinition);
                AssetDatabase.SaveAssets();
                diagnostics.text = $"Saved behavior source topology and compiled runtime definition\n{AssetDatabase.GetAssetPath(behaviorAsset)}\n{FormalBehaviorRuntimePath}";
                diagnostics.messageType = HelpBoxMessageType.Info;
                return;
            }

            if (branchAdapter == null)
                return;

            bool saved = branchAdapter.Save(out CharacterActionCatalogValidationResult validation);
            diagnostics.text = saved
                ? $"Saved committed action branch {AssetDatabase.GetAssetPath(actionDefinition)}"
                : validation.DescribeErrors();
            diagnostics.messageType = saved ? HelpBoxMessageType.Info : HelpBoxMessageType.Error;
            ReloadGraph();
        }

        void ValidateGraph()
        {
            if (mode == SourceMode.BehaviorSource)
            {
                diagnostics.text = behaviorAsset == null
                    ? $"Formal behavior source authoring asset not found at {FormalBehaviorAuthoringPath}."
                    : DescribeBehaviorSourceValidation(behaviorAsset);
                diagnostics.messageType = behaviorAsset == null || diagnostics.text.IndexOf("OK", System.StringComparison.Ordinal) < 0
                    ? HelpBoxMessageType.Warning
                    : HelpBoxMessageType.Info;
                return;
            }

            if (actionDefinition == null)
                return;

            CharacterActionCatalogValidationResult validation =
                actionDefinition.Validate(ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default));
            diagnostics.text = validation.HasErrors
                ? validation.DescribeErrors()
                : $"Action branch OK | {AssetDatabase.GetAssetPath(actionDefinition)}";
            diagnostics.messageType = validation.HasErrors ? HelpBoxMessageType.Error : HelpBoxMessageType.Info;
        }

        void OpenSelectedTimeline()
        {
            if (mode != SourceMode.CommittedActionBranch || actionDefinition == null)
                return;

            string timelineNodeId = selectedBranchNodeId;
            if (branchAdapter == null ||
                !branchAdapter.TryGetNodeProperty(timelineNodeId, out SerializedProperty node, out _) ||
                (CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex != CommittedActionNodeKind.Timeline)
            {
                branchAdapter?.TryGetTimelineNodeId(CommittedActionTimelineVariant.Directional, out timelineNodeId);
            }

            CommittedActionTimelineEditorWindow.Open(actionDefinition, timelineNodeId);
        }

        static string DescribeBehaviorSourceValidation(CharacterBehaviorAuthoringAsset asset)
        {
            CharacterBehaviorAuthoringCompilerResult compile =
                ThirdPersonCharacterBehavior.Authoring.CharacterBehaviorAuthoringCompiler.Compile(asset);
            if (!compile.Success)
                return string.Join("\n", compile.Errors);

            return $"Behavior source topology OK | root {compile.RuntimeDefinition.RootId.Value} | leaves {compile.RuntimeDefinition.LeafCount}";
        }

        void InitializeBranchTemplate()
        {
            if (mode != SourceMode.CommittedActionBranch || branchAdapter == null)
                return;

            bool initialized = branchAdapter.InitializeMinimalBranchTemplate(out string diagnostic);
            diagnostics.text = initialized
                ? "Initialized committed action branch root/template."
                : diagnostic;
            diagnostics.messageType = initialized ? HelpBoxMessageType.Info : HelpBoxMessageType.Error;
            selectedBranchNodeId = branchAdapter.Capture().RootNodeId;
            ReloadGraph();
        }

        void InitializeDodgeBranchTemplate()
        {
            if (mode != SourceMode.CommittedActionBranch || branchAdapter == null)
                return;

            bool initialized = branchAdapter.InitializeDodgeBranchTemplate(out string diagnostic);
            diagnostics.text = initialized
                ? "Initialized formal Dodge branch template."
                : diagnostic;
            diagnostics.messageType = initialized ? HelpBoxMessageType.Info : HelpBoxMessageType.Error;
            selectedBranchNodeId = "branch.root.action.dodge";
            ReloadGraph();
        }

        void DrawBranchInspector()
        {
            if (mode != SourceMode.CommittedActionBranch)
                return;

            EditorGUILayout.LabelField("Committed Branch Node", EditorStyles.boldLabel);
            if (branchAdapter == null || actionDefinition == null)
            {
                EditorGUILayout.HelpBox("Select a CharacterActionDefinitionSO.", MessageType.Info);
                return;
            }

            serializedActionDefinition.UpdateIfRequiredOrScript();
            if (!branchAdapter.TryGetBranchProperty(out SerializedProperty branch, out string branchDiagnostic))
            {
                EditorGUILayout.HelpBox(branchDiagnostic, MessageType.Error);
                return;
            }

            string rootNodeId = branch.FindPropertyRelative("rootNodeId").stringValue;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Action Definition", AssetDatabase.GetAssetPath(actionDefinition));
                EditorGUILayout.TextField("Branch Id", branch.FindPropertyRelative("branchId").stringValue);
                EditorGUILayout.TextField("Root Node Id", rootNodeId);
            }

            EditorGUILayout.Space();
            if (string.IsNullOrWhiteSpace(selectedBranchNodeId))
            {
                EditorGUILayout.HelpBox("Select a branch node.", MessageType.Info);
                return;
            }

            if (!branchAdapter.TryGetNodeProperty(selectedBranchNodeId, out SerializedProperty node, out string nodeDiagnostic))
            {
                EditorGUILayout.HelpBox(nodeDiagnostic, MessageType.Warning);
                return;
            }

            string nodeId = node.FindPropertyRelative("nodeId").stringValue;
            CommittedActionNodeKind kind = (CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex;
            bool isRoot = string.Equals(nodeId, rootNodeId, System.StringComparison.Ordinal);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Node Id", nodeId);
                EditorGUILayout.EnumPopup("Kind", kind);
                EditorGUILayout.Toggle("Protected Root", isRoot);
                EditorGUILayout.PropertyField(node.FindPropertyRelative("childNodeIds"), true);
            }

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            switch (kind)
            {
                case CommittedActionNodeKind.Condition:
                    DrawConditionInspector(node);
                    break;
                case CommittedActionNodeKind.Timeline:
                    DrawTimelineInspector(node);
                    break;
                default:
                    EditorGUILayout.HelpBox("Selector/root nodes expose topology and child order here; edges remain edited in the graph.", MessageType.Info);
                    break;
            }

            if (!EditorGUI.EndChangeCheck())
                return;

            serializedActionDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(actionDefinition);
            ScheduleReloadGraph();
        }

        void DrawConditionInspector(SerializedProperty node)
        {
            SerializedProperty condition = node.FindPropertyRelative("condition");
            EditorGUILayout.PropertyField(condition.FindPropertyRelative("kind"));
            EditorGUILayout.PropertyField(condition.FindPropertyRelative("requestKind"));
            EditorGUILayout.PropertyField(condition.FindPropertyRelative("requiredFactId"));
            EditorGUILayout.PropertyField(condition.FindPropertyRelative("expectedVariant"));
        }

        void DrawTimelineInspector(SerializedProperty node)
        {
            SerializedProperty timeline = node.FindPropertyRelative("timeline");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(timeline.FindPropertyRelative("timelineNodeId"));
            EditorGUILayout.PropertyField(timeline.FindPropertyRelative("durationSeconds"));
            EditorGUILayout.PropertyField(timeline.FindPropertyRelative("defaultBodyKind"));
            EditorGUILayout.PropertyField(timeline.FindPropertyRelative("defaultChannels"));
            if (GUILayout.Button("Open Independent Timeline Editor"))
                OpenSelectedTimeline();
        }

        void SetBranchInspectorVisible(bool visible)
        {
            if (branchInspector != null)
                branchInspector.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ScheduleReloadGraph()
        {
            if (reloadScheduled)
                return;

            reloadScheduled = true;
            EditorApplication.delayCall += ReloadGraphFromDelay;
        }

        void ReloadGraphFromDelay()
        {
            reloadScheduled = false;
            if (this != null)
                ReloadGraph();
        }

        internal bool IsBehaviorSourceModeForTests => mode == SourceMode.BehaviorSource;
        internal bool IsCommittedActionBranchModeForTests => mode == SourceMode.CommittedActionBranch;
        internal CharacterBehaviorAuthoringAsset CurrentBehaviorAssetForTests => behaviorAsset;
        internal CharacterActionDefinitionSO CurrentActionDefinitionForTests => actionDefinition;
        internal CharacterConfigSO CurrentCharacterConfigForTests => characterConfig;
        internal int PendingActionNavigationEntryCountForTests => pendingActionNavigationEntries.Count;
        internal string SelectedBranchNodeIdForTests => selectedBranchNodeId;
        internal string DiagnosticsTextForTests => diagnostics != null ? diagnostics.text : string.Empty;

        internal void SetBehaviorAssetForTests(CharacterBehaviorAuthoringAsset asset)
        {
            behaviorAsset = asset;
            SetMode(SourceMode.BehaviorSource);
        }

        internal void SetActionDefinitionForTests(CharacterActionDefinitionSO definition)
        {
            actionDefinition = definition;
            ConfigureAssetField();
            if (mode == SourceMode.CommittedActionBranch)
                ReloadGraph();
        }

        internal void SetCharacterConfigForTests(CharacterConfigSO config)
        {
            characterConfig = config;
            characterConfigField?.SetValueWithoutNotify(characterConfig);
            HideActionCatalogNavigationPanel();
        }

        internal bool OpenBehaviorSourceNodeForTests(string nodeId)
        {
            return OpenBehaviorSourceNode(nodeId);
        }

        internal bool OpenPendingCatalogActionForTests(string actionId)
        {
            for (int i = 0; i < pendingActionNavigationEntries.Count; i++)
            {
                if (string.Equals(pendingActionNavigationEntries[i].ActionId, actionId, StringComparison.Ordinal))
                    return OpenActionCatalogNavigationEntry(pendingActionNavigationEntries[i]);
            }

            return false;
        }

        internal static bool IsCommittedActionLeafNode(CharacterBehaviorAuthoringAsset asset, string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            if (string.Equals(nodeId, "source.committed-action", StringComparison.Ordinal) ||
                nodeId.StartsWith("source.committed-action.", StringComparison.Ordinal))
                return true;

            if (asset == null)
                return false;

            for (int i = 0; i < asset.Nodes.Count; i++)
            {
                CharacterBehaviorAuthoringNode node = asset.Nodes[i];
                if (string.Equals(node.StableId, nodeId, StringComparison.Ordinal))
                    return node.Kind == CharacterBehaviorAuthoringNodeKind.CommittedActionLeaf;
            }

            return false;
        }

        internal static CommittedActionLeafCatalogNavigationSnapshot BuildCatalogNavigationSnapshotForTests(
            CharacterConfigSO config)
        {
            return CommittedActionLeafCatalogNavigationSnapshot.FromConfig(config);
        }
    }
}
