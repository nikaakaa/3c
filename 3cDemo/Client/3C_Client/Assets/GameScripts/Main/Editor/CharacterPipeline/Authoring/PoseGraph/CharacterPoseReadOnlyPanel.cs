using BTSMTL.Authoring.Editor;
using System;
using BTSMTL.Authoring.Graph;
using TreeDesigner.Editor;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    abstract class CharacterPoseReadOnlyPanel :
        IGraphAuthoringReadOnlyPanel
    {
        protected readonly ScrollView Content =
            new ScrollView(ScrollViewMode.Vertical);
        protected IGraphAuthoringDocumentProjection Document;

        protected CharacterPoseReadOnlyPanel()
        {
            Content.AddToClassList("pose-read-only-panel");
        }

        public VisualElement View => Content;

        public virtual void Bind(
            IGraphAuthoringDocumentProjection document)
        {
            Document = document ??
                throw new ArgumentNullException(nameof(document));
        }

        public abstract void Refresh();

        public virtual void Unbind()
        {
            Document = null;
            Content.Clear();
        }

        protected static void AddValue(
            VisualElement parent,
            string label,
            string value)
        {
            var row = new VisualElement();
            row.AddToClassList("pose-applied-row");
            var name = new Label(label);
            name.AddToClassList("pose-applied-label");
            var content = new Label(value ?? string.Empty);
            content.AddToClassList("pose-applied-value");
            row.Add(name);
            row.Add(content);
            parent.Add(row);
        }

        protected static void AddStatus(
            VisualElement parent,
            string status)
        {
            parent.Add(new HelpBox(
                status,
                status.StartsWith("Stale", StringComparison.Ordinal)
                    ? HelpBoxMessageType.Warning
                    : HelpBoxMessageType.Info));
        }
    }

}
