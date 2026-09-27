using System;
using System.Collections.Generic;
using BTSMTL.Authoring.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Authoring.Editor
{
    public abstract class GraphAuthoringDetailsHostView : VisualElement
    {
        protected GraphAuthoringDetailsHostView(bool startsHidden)
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>(
                "VisualTree/GraphAuthoringDetails");
            if (!template)
                throw new InvalidOperationException(
                    "Graph authoring details visual tree is missing.");
            template.CloneTree(this);
            AddToClassList("treeInspector");
            style.display = startsHidden
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            DetailsPage = this.Q("selection-inspector-page") ??
                throw new InvalidOperationException(
                    "Graph authoring details page is missing.");
            DetailsContent =
                this.Q("selection-inspector-container") ??
                throw new InvalidOperationException(
                    "Graph authoring details content is missing.");
            DetailsPage.style.display = DisplayStyle.Flex;
        }

        protected VisualElement DetailsPage { get; }
        protected VisualElement DetailsContent { get; }
    }

    public abstract class GraphAuthoringNavigatorHostView : VisualElement
    {
        protected GraphAuthoringNavigatorHostView(
            string visualTreeName)
        {
            VisualTreeAsset template =
                Resources.Load<VisualTreeAsset>(
                    $"VisualTree/{visualTreeName}");
            if (!template)
                throw new InvalidOperationException(
                    "Graph authoring navigator visual tree is missing.");
            template.CloneTree(this);
            style.flexGrow = 1f;
        }
    }
}
