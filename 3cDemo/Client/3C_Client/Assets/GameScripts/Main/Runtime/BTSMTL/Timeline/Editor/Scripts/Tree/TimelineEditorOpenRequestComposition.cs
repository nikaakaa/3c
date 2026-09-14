using System;
using TreeDesigner.Editor;

namespace BTSMTL.Timeline.Editor
{
    public static class TimelineEditorOpenRequestComposition
    {
        public static TimelineEditorOpenRequest Create(
            TimelineData timeline,
            UnityEngine.Object serializedOwner,
            string serializedPropertyPath,
            string ownershipLabel,
            BaseTreeWindow sourceGraphWindow,
            ITimelineEditorRuntimeDebugBinding runtimeDebugBinding = null)
        {
            return new TimelineEditorOpenRequest(
                timeline,
                serializedOwner,
                serializedPropertyPath,
                ownershipLabel,
                runtimeDebugBinding,
                TimelineTreeContractComposition.Create());
        }
    }
}
