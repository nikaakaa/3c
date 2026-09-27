using System;
using System.Collections.Generic;
using BTSMTL.Authoring.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Authoring.Editor
{
    public interface IGraphAuthoringClipboardCodec
    {
        string Serialize(IGraphAuthoringDocumentProjection document, IReadOnlyList<GraphAuthoringSelection> selection);
        bool CanPaste(IGraphAuthoringDocumentProjection document, string payload);
        void Paste(IGraphAuthoringDocumentProjection document, string operationName, string payload, Vector2 graphPosition);
    }

    public sealed class GraphAuthoringProjectionCanvasBinding
    {
        public GraphAuthoringProjectionCanvasBinding(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringCapabilityCatalog capabilities,
            IGraphAuthoringDomainMutation mutation,
            IGraphAuthoringConnectionPolicy connectionPolicy,
            IGraphAuthoringClipboardCodec clipboard = null,
            bool persistsLayout = true)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            ConnectionPolicy = connectionPolicy ?? throw new ArgumentNullException(nameof(connectionPolicy));
            Clipboard = clipboard;
            PersistsLayout = persistsLayout;
        }

        public IGraphAuthoringDocumentProjection Document { get; }
        public GraphAuthoringCapabilityCatalog Capabilities { get; }
        public IGraphAuthoringDomainMutation Mutation { get; }
        public IGraphAuthoringConnectionPolicy ConnectionPolicy { get; }
        public IGraphAuthoringClipboardCodec Clipboard { get; }
        public bool PersistsLayout { get; }
    }
}
