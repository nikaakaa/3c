using System;
using TreeDesigner.Editor;

namespace BTSMTL.Timeline.Editor
{
    public interface ITimelineEditorMarkerTopologyResolver
    {
        ITimelineAnimationMarkerSyncAuthoringContext Resolve(BaseTreeWindow sourceGraphWindow);
    }

    public interface ITimelineEditorToolCatalogResolver
    {
        TimelineEditorToolCatalog Resolve(BaseTreeWindow sourceGraphWindow);
    }

    public static class TimelineEditorOpenRequestComposition
    {
        static ITimelineEditorMarkerTopologyResolver s_MarkerTopologyResolver;
        static ITimelineEditorToolCatalogResolver s_ToolCatalogResolver;

        public static void SetMarkerTopologyResolver(ITimelineEditorMarkerTopologyResolver resolver)
        {
            s_MarkerTopologyResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public static void SetToolCatalogResolver(ITimelineEditorToolCatalogResolver resolver)
        {
            s_ToolCatalogResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public static TimelineEditorOpenRequest Create(
            TimelineData timeline,
            UnityEngine.Object serializedOwner,
            string serializedPropertyPath,
            string ownershipLabel,
            BaseTreeWindow sourceGraphWindow,
            ITimelineEditorRuntimeDebugBinding runtimeDebugBinding = null)
        {
            ITimelineAnimationMarkerSyncAuthoringContext topology =
                sourceGraphWindow && s_MarkerTopologyResolver != null
                    ? s_MarkerTopologyResolver.Resolve(sourceGraphWindow)
                    : null;
            TimelineEditorToolCatalog toolCatalog = sourceGraphWindow && s_ToolCatalogResolver != null
                ? s_ToolCatalogResolver.Resolve(sourceGraphWindow)
                : TimelineEditorToolComposition.Catalog;
            return new TimelineEditorOpenRequest(
                timeline,
                serializedOwner,
                serializedPropertyPath,
                ownershipLabel,
                topology,
                runtimeDebugBinding,
                toolCatalog ?? TimelineEditorToolCatalog.Empty);
        }
    }
}
