using BTSMTL.Timeline;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    internal readonly struct TimelineEditorBindingState
    {
        public TimelineEditorBindingState(
            TimelineData timeline,
            UnityEngine.Object serializedOwner,
            string serializedPropertyPath,
            string ownershipLabel,
            UnityEngine.Object sourceGraphWindow,
            UnityEngine.Object sourceGraphOwner,
            TimelineNode sourceNode,
            string sourceNodeGuid,
            string sourceGraphAuthoringId)
        {
            Timeline = timeline;
            SerializedOwner = serializedOwner;
            SerializedPropertyPath = serializedPropertyPath ?? string.Empty;
            OwnershipLabel = ownershipLabel ?? string.Empty;
            SourceGraphWindow = sourceGraphWindow;
            SourceGraphOwner = sourceGraphOwner;
            SourceNode = sourceNode;
            SourceNodeGuid = sourceNodeGuid ?? string.Empty;
            SourceGraphAuthoringId = sourceGraphAuthoringId ?? string.Empty;
        }

        public TimelineData Timeline { get; }
        public UnityEngine.Object SerializedOwner { get; }
        public string SerializedPropertyPath { get; }
        public string OwnershipLabel { get; }
        public UnityEngine.Object SourceGraphWindow { get; }
        public UnityEngine.Object SourceGraphOwner { get; }
        public TimelineNode SourceNode { get; }
        public string SourceNodeGuid { get; }
        public string SourceGraphAuthoringId { get; }
        public bool IsBound => Timeline != null && SerializedOwner != null &&
            !string.IsNullOrEmpty(SerializedPropertyPath);

        public TimelineAsset Document => SerializedOwner as TimelineAsset;

        public string CreateSourceSummary()
        {
            if (!IsBound)
                return "Source: None";
            string ownership = string.IsNullOrWhiteSpace(OwnershipLabel) ? "Timeline" : OwnershipLabel;
            return $"Source: {ownership} / {Timeline.Name}";
        }
    }

    internal sealed class TimelineEditorToolbarControls
    {
        public TimelineEditorToolbarControls(
            VisualElement documentGroup,
            VisualElement workspaceGroup,
            VisualElement statusGroup,
            Toolbar toolbar,
            ToolbarButton backButton,
            ObjectField documentField,
            Label sourceSummary,
            Label status)
        {
            DocumentGroup = documentGroup;
            WorkspaceGroup = workspaceGroup;
            StatusGroup = statusGroup;
            BackButton = backButton;
            DocumentField = documentField;
            SourceSummary = sourceSummary;
            Status = status;
        }

        public VisualElement DocumentGroup { get; }
        public VisualElement WorkspaceGroup { get; }
        public VisualElement StatusGroup { get; }
        public Toolbar Toolbar { get; }
        public ToolbarButton BackButton { get; }
        public ObjectField DocumentField { get; }
        public Label SourceSummary { get; }
        public Label Status { get; }

        public void SetBackVisible(bool visible)
        {
            BackButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetDocumentWithoutNotify(TimelineAsset document)
        {
            DocumentField.SetValueWithoutNotify(document);
        }

        public void SetSourceSummary(string value)
        {
            SourceSummary.text = value;
        }

        public void SetStatus(string value)
        {
            Status.text = value ?? string.Empty;
            Status.tooltip = value ?? string.Empty;
        }
    }

    internal static class TimelineEditorToolbarView
    {
        public static TimelineEditorToolbarControls Create(
            TimelineEditorBindingState binding,
            bool hasNavigation,
            System.Action onBack,
            EventCallback<ChangeEvent<UnityEngine.Object>> onDocumentChanged,
            VisualElement workspaceControls)
        {
            var toolbar = new Toolbar();
            toolbar.AddToClassList("timeline-editor-toolbar");

            var documentGroup = new VisualElement();
            documentGroup.AddToClassList("timeline-editor-toolbar-group");
            documentGroup.style.flexDirection = FlexDirection.Row;
            documentGroup.style.alignItems = Align.Center;

            var backButton = new ToolbarButton(onBack) { text = "‹ Timeline" };
            backButton.style.display = hasNavigation ? DisplayStyle.Flex : DisplayStyle.None;
            documentGroup.Add(backButton);

            var documentField = new ObjectField("Document")
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false
            };
            documentField.style.width = 280f;
            documentField.SetValueWithoutNotify(binding.Document);
            documentField.RegisterValueChangedCallback(onDocumentChanged);
            documentGroup.Add(documentField);

            var sourceSummary = new Label(binding.CreateSourceSummary());
            sourceSummary.style.minWidth = 180f;
            sourceSummary.style.marginLeft = 6f;
            documentGroup.Add(sourceSummary);

            var workspaceGroup = new VisualElement();
            workspaceGroup.AddToClassList("timeline-editor-toolbar-group");
            workspaceGroup.style.flexDirection = FlexDirection.Row;
            workspaceGroup.style.alignItems = Align.Center;
            if (workspaceControls != null)
                workspaceGroup.Add(workspaceControls);

            var statusGroup = new VisualElement();
            statusGroup.AddToClassList("timeline-editor-toolbar-group");
            statusGroup.style.flexDirection = FlexDirection.Row;
            statusGroup.style.flexGrow = 1f;
            statusGroup.style.alignItems = Align.Center;
            var status = new Label("运行控制：Skill Graph / Graph Shell");
            status.style.marginLeft = 6f;
            status.style.flexGrow = 1f;
            status.tooltip = "Timeline 只负责作者编辑；Scene Play、Build、Skill 和运行观察由 Graph Shell 管理。";
            statusGroup.Add(status);

            toolbar.Add(documentGroup);
            toolbar.Add(workspaceGroup);
            toolbar.Add(statusGroup);
            return new TimelineEditorToolbarControls(
                documentGroup,
                workspaceGroup,
                statusGroup,
                toolbar,
                backButton,
                documentField,
                sourceSummary,
                status);
        }
    }
}
