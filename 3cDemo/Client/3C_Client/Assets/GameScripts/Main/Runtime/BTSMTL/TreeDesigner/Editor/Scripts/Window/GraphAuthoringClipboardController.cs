using System;
using System.Collections.Generic;
using BTSMTL.Authoring.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace TreeDesigner.Editor
{
    public static class GraphAuthoringClipboardController
    {
        public static void Bind(
            GraphView view,
            Func<string> getDomainId,
            Func<IEnumerable<GraphElement>, string>
                serializeSelection,
            Func<string, bool> canPaste,
            Action<string, string> paste)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (getDomainId == null)
                throw new ArgumentNullException(
                    nameof(getDomainId));
            if (serializeSelection == null)
                throw new ArgumentNullException(
                    nameof(serializeSelection));
            if (canPaste == null)
                throw new ArgumentNullException(nameof(canPaste));
            if (paste == null)
                throw new ArgumentNullException(nameof(paste));

            view.serializeGraphElements = elements =>
            {
                string domainId = RequireDomain(getDomainId());
                string payload =
                    serializeSelection(elements) ?? string.Empty;
                return JsonUtility.ToJson(
                    new GraphAuthoringClipboardEnvelope
                    {
                        domainId = domainId,
                        payload = payload
                    });
            };
            view.canPasteSerializedData = serialized =>
                GraphAuthoringClipboardEnvelope.TryRead(
                    serialized,
                    out GraphAuthoringClipboardEnvelope envelope) &&
                envelope.Allows(
                    RequireDomain(getDomainId())) &&
                canPaste(envelope.payload);
            view.unserializeAndPaste =
                (operationName, serialized) =>
                {
                    string domainId =
                        RequireDomain(getDomainId());
                    if (!GraphAuthoringClipboardEnvelope.TryRead(
                            serialized,
                            out GraphAuthoringClipboardEnvelope
                                envelope) ||
                        !envelope.Allows(domainId))
                    {
                        throw new InvalidOperationException(
                            "Graph Authoring clipboard domain does not match the current document.");
                    }
                    paste(operationName, envelope.payload);
                };
        }

        static string RequireDomain(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    "Graph Authoring clipboard domain is missing.");
            return value;
        }
    }

}
