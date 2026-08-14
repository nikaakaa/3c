using System;
using TreeDesigner.Editor;

namespace BTSMTL.Timeline.Editor
{
    public interface ITimelineEditorToolCatalogResolver
    {
        TimelineEditorToolCatalog Resolve(BaseTreeWindow sourceGraphWindow);
    }

    public static class TimelineEditorOpenRequestComposition
    {
        static ITimelineEditorToolCatalogResolver s_ToolCatalogResolver;

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
            TimelineEditorToolCatalog toolCatalog = sourceGraphWindow && s_ToolCatalogResolver != null
                ? s_ToolCatalogResolver.Resolve(sourceGraphWindow)
                : TimelineEditorToolComposition.Catalog;
            return new TimelineEditorOpenRequest(
                timeline,
                serializedOwner,
                serializedPropertyPath,
                ownershipLabel,
                runtimeDebugBinding,
                toolCatalog ?? TimelineEditorToolCatalog.Empty);
        }
    }
}
