namespace BTSMTL.Timeline
{
    public static class TimelineTreeContractComposition
    {
        public static TimelineContractCatalog Create()
        {
            return new TimelineContractCatalog(new ITimelineContractProvider[]
            {
                AnimationTimelineContracts.Provider,
                MotionCurveTimelineContracts.Provider,
                MotionWarpTimelineContracts.Provider,
                TreeTimelineContracts.Provider,
                ActionCueTimelineContracts.Provider,
                CameraTimelineContracts.Provider,
                ScenePresentationTimelineContracts.Provider
            });
        }
    }
}
