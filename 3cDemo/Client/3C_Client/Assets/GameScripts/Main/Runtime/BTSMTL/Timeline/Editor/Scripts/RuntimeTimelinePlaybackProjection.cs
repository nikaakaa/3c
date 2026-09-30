using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics.Editor;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BTSMTL.Timeline.Tree.Editor")]

namespace BTSMTL.Timeline.Editor
{
    internal sealed class RuntimeTimelineObservationBuffer
    {
        internal List<RuntimeDebugEventView> EventBuffer { get; } = new List<RuntimeDebugEventView>();
        internal Dictionary<string, string> ActiveTracks { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        internal Dictionary<string, float> DynamicClipEnds { get; } = new Dictionary<string, float>(StringComparer.Ordinal);
        internal Dictionary<string, string> ActiveClips { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
