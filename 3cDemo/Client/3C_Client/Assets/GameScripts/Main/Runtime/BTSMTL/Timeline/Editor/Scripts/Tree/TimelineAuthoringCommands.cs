using TreeDesigner.Authoring;

namespace BTSMTL.Timeline
{
    public static class TimelineAuthoringCommands
    {
        public static readonly GraphAuthoringCommandId UseInline =
            new GraphAuthoringCommandId(
                "btsmtl.timeline.use-inline");
        public static readonly GraphAuthoringCommandId UseShared =
            new GraphAuthoringCommandId(
                "btsmtl.timeline.use-shared");
    }
}
