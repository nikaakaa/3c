using System;
using ThirdPersonAction;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacterBehavior.Editor.ActionTimeline
{
    public sealed class CommittedActionTimelineEditorWindow : EditorWindow
    {
        const string FormalDodgeActionPath = "Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset";

        CharacterActionDefinitionSO actionDefinition;
        SerializedObject serializedActionDefinition;
        CommittedActionTimelineSerializedAdapter timelineAdapter;
        HelpBox diagnostics;
        ObjectField assetField;
        ObjectField previewTargetField;
        CommittedActionRefPortedTimelineView timelineView;
        GameObject previewTarget;
        double lastPreviewTime;
        float previewFrame;
        string selectedTimelineNodeId = string.Empty;

        [MenuItem("Tools/3C/Committed Action Timeline Editor")]
        public static void Open()
        {
            GetWindow<CommittedActionTimelineEditorWindow>("Committed Action Timeline Editor");
        }

        public static void Open(CharacterActionDefinitionSO actionDefinition, string timelineNodeId)
        {
            CommittedActionTimelineEditorWindow window =
                GetWindow<CommittedActionTimelineEditorWindow>("Committed Action Timeline Editor");
            window.SetTarget(actionDefinition, timelineNodeId);
        }

        void OnEnable()
        {
            BuildUi();
        }

        void OnDisable()
        {
            timelineView?.SuspendScenePreview();
        }

        void BuildUi()
        {
            rootVisualElement.Clear();

            Toolbar toolbar = new Toolbar();
            assetField = new ObjectField
            {
                objectType = typeof(CharacterActionDefinitionSO),
                allowSceneObjects = false,
                style = { minWidth = 280 }
            };
            assetField.RegisterValueChangedCallback(evt =>
            {
                actionDefinition = evt.newValue as CharacterActionDefinitionSO;
                Reload();
            });
            toolbar.Add(assetField);
            previewTargetField = new ObjectField
            {
                label = "Preview Target",
                objectType = typeof(GameObject),
                allowSceneObjects = true,
                tooltip = "Scene Preview Target",
                style = { minWidth = 320 }
            };
            previewTargetField.RegisterValueChangedCallback(evt =>
            {
                previewTarget = evt.newValue as GameObject;
                timelineView?.SetScenePreviewTarget(previewTarget);
            });
            previewTargetField.SetValueWithoutNotify(previewTarget);
            toolbar.Add(previewTargetField);
            toolbar.Add(new ToolbarButton(BindSelectedPreviewTarget) { text = "Bind Selection" });
            toolbar.Add(new ToolbarButton(OpenFormalDodge) { text = "Open Dodge" });
            toolbar.Add(new ToolbarButton(Save) { text = "Save" });
            toolbar.Add(new ToolbarButton(ValidateFormalConfig) { text = "Validate" });
            rootVisualElement.Add(toolbar);

            diagnostics = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            rootVisualElement.Add(diagnostics);

            timelineView = new CommittedActionRefPortedTimelineView();
            rootVisualElement.Add(timelineView);

            OpenFormalDodge();
        }

        void Update()
        {
            if (timelineView == null || !timelineView.IsPreviewPlaying)
            {
                lastPreviewTime = EditorApplication.timeSinceStartup;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float delta = Mathf.Max(0f, (float)(now - lastPreviewTime));
            lastPreviewTime = now;
            previewFrame += delta * 30f * timelineView.PreviewSpeed;
            if (previewFrame > timelineView.MaxPreviewFrame)
                previewFrame = 0f;
            timelineView.SetPreviewFrame(Mathf.FloorToInt(previewFrame));
            Repaint();
        }

        void OpenFormalDodge()
        {
            actionDefinition = AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(FormalDodgeActionPath);
            selectedTimelineNodeId = string.Empty;
            if (assetField != null)
                assetField.SetValueWithoutNotify(actionDefinition);
            Reload();
        }

        void SetTarget(CharacterActionDefinitionSO targetActionDefinition, string timelineNodeId)
        {
            actionDefinition = targetActionDefinition;
            selectedTimelineNodeId = timelineNodeId ?? string.Empty;
            if (assetField != null)
                assetField.SetValueWithoutNotify(actionDefinition);
            Reload();
        }

        void Reload()
        {
            if (actionDefinition == null)
            {
                diagnostics.text = $"Formal Dodge action definition not found at {FormalDodgeActionPath}.";
                diagnostics.messageType = HelpBoxMessageType.Warning;
                timelineAdapter = null;
                timelineView.Populate((CommittedActionTimelineSerializedAdapter)null);
                timelineView.DisposeScenePreview();
                return;
            }

            timelineAdapter = string.IsNullOrWhiteSpace(selectedTimelineNodeId)
                ? new CommittedActionTimelineSerializedAdapter(actionDefinition)
                : new CommittedActionTimelineSerializedAdapter(actionDefinition, new SerializedObject(actionDefinition), selectedTimelineNodeId);
            serializedActionDefinition = timelineAdapter.SerializedObject;
            diagnostics.text = string.IsNullOrWhiteSpace(selectedTimelineNodeId)
                ? $"Editing formal Dodge config {AssetDatabase.GetAssetPath(actionDefinition)}"
                : $"Editing timeline node {selectedTimelineNodeId} | {AssetDatabase.GetAssetPath(actionDefinition)}";
            diagnostics.messageType = HelpBoxMessageType.Info;
            timelineView.Populate(timelineAdapter);
            ApplyPreviewTargetToView();
            previewFrame = 0f;
            lastPreviewTime = EditorApplication.timeSinceStartup;
        }

        void BindSelectedPreviewTarget()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                CommittedActionTimelinePreviewLogger.Warning("Bind Selection failed: no active scene GameObject selected");
                diagnostics.text = "Select a scene character GameObject, then click Bind Selection.";
                diagnostics.messageType = HelpBoxMessageType.Warning;
                return;
            }

            if (previewTargetField != null)
                previewTargetField.SetValueWithoutNotify(selected);
            previewTarget = selected;
            timelineView?.SetScenePreviewTarget(selected);
            diagnostics.text = $"Preview target bound: {selected.name}";
            diagnostics.messageType = HelpBoxMessageType.Info;
        }

        void ApplyPreviewTargetToView()
        {
            if (previewTargetField != null)
                previewTargetField.SetValueWithoutNotify(previewTarget);
            timelineView?.SetScenePreviewTarget(previewTarget);
        }

        void Save()
        {
            if (timelineAdapter == null)
                return;

            bool saved = timelineAdapter.Save(out CommittedActionTimelineEditorValidationResult validation);
            ReportValidation($"Saved {AssetDatabase.GetAssetPath(actionDefinition)}", validation, saved);
            timelineView.Populate(timelineAdapter);
        }

        void ValidateFormalConfig()
        {
            if (timelineAdapter == null)
                return;

            serializedActionDefinition.ApplyModifiedProperties();
            ReportValidation(
                $"Validated {AssetDatabase.GetAssetPath(actionDefinition)}",
                CommittedActionTimelineEditorValidator.Validate(timelineAdapter),
                true);
        }

        void ReportValidation(
            string prefix,
            CommittedActionTimelineEditorValidationResult editorValidation,
            bool saveSucceeded)
        {
            ActionTimelineCompileContext compileContext = ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default);
            CharacterActionCatalogValidationResult validation = actionDefinition.Validate(in compileContext);
            CharacterActionDefinition definition = actionDefinition.ToDefinition(in compileContext);
            bool hasBranch = definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch);
            bool success = saveSucceeded && !validation.HasErrors && !editorValidation.HasErrors && hasBranch && branch.CanEvaluate;
            string detail = success
                ? $"Action.Dodge OK | selector {branch.RootNode.NodeId.Value} | nodes {branch.Nodes.Count + 1}"
                : string.Join(Environment.NewLine, editorValidation.Errors);
            if (!hasBranch)
                detail = string.IsNullOrWhiteSpace(detail) ? "Dodge committed action branch is missing." : $"{detail}\nDodge committed action branch is missing.";
            diagnostics.text = $"{prefix}\n{detail}";
            diagnostics.messageType = success ? HelpBoxMessageType.Info : HelpBoxMessageType.Error;
        }
    }
}
