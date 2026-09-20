using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;

namespace BTSMTL.Timeline.Runtime
{
    public static class TimelineRuntimePlaybackSnapshotRegistry
    {
        static readonly Dictionary<RuntimeInstanceKey, TimelineData> s_Snapshots =
            new Dictionary<RuntimeInstanceKey, TimelineData>();

        public static bool Publish(RuntimeInstanceKey playback, TimelineData timeline)
        {
            if (playback.Kind != RuntimeInstanceKind.TimelinePlayback)
                throw new ArgumentException("Timeline playback identity is required.", nameof(playback));
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (s_Snapshots.ContainsKey(playback))
                return false;
            s_Snapshots.Add(playback, timeline.Clone());
            return true;
        }

        public static void Remove(RuntimeInstanceKey playback) => s_Snapshots.Remove(playback);

        public static bool TryGet(RuntimeInstanceKey playback, out TimelineData timeline)
        {
            return s_Snapshots.TryGetValue(playback, out timeline);
        }
    }
}
