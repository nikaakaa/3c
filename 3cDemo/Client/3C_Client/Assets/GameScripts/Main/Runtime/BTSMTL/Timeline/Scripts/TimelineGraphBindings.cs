using System;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineGraphBinding
    {
        public TimelineGraphBinding(string graphId, string revision)
        {
            GraphId = graphId;
            Revision = revision;
        }

        public string GraphId { get; }
        public string Revision { get; }
    }

    public interface ITimelineGraphBindingSource
    {
        TimelineGraphBinding ResolveTreeClip(Clip clip, TimelineContentClosureBuilder closure);
        TimelineGraphBinding ResolveMarker(TimelineMarker marker, TimelineContentClosureBuilder closure);
    }

#if UNITY_EDITOR
    internal sealed class TimelineAuthoringGraphBindings : ITimelineGraphBindingSource
    {
        internal static readonly TimelineAuthoringGraphBindings Instance = new();

        public TimelineGraphBinding ResolveTreeClip(Clip clip, TimelineContentClosureBuilder closure) =>
            Resolve(((ITimelineTreeClipAuthoringSource)clip).AssetTree, false, clip.ExecutionDomain, $"clip:{clip.AuthoringId}", closure);

        public TimelineGraphBinding ResolveMarker(TimelineMarker marker, TimelineContentClosureBuilder closure) =>
            Resolve(marker.Graph, true, marker.ExecutionDomain,
                $"track:{marker.Track.AuthoringId}/marker:{marker.AuthoringId}", closure);

        static TimelineGraphBinding Resolve(ScriptableObject asset, bool marker, TimelineExecutionDomain domain,
            string sourcePath, TimelineContentClosureBuilder closure)
        {
            if (asset is not ITimelineTreeGraphAsset graph ||
                (marker ? !graph.IsTimelineTrigger : !graph.IsTimelineTree))
                throw new InvalidOperationException($"Timeline '{sourcePath}' requires a {(marker ? "TimelineTrigger" : "TimelineBody")} graph.");
            graph.CollectTimelineContentClosure(closure, sourcePath, domain);
            string identity = "tree:" + graph.AuthoringId;
            for (int i = 0; i < closure.Dependencies.Count; i++)
                if (closure.Dependencies[i].Identity == identity)
                    return new TimelineGraphBinding(identity, closure.Dependencies[i].ContentHash);
            throw new InvalidOperationException($"Timeline graph '{identity}' has no valid content revision: {string.Join(" | ", closure.Errors)}");
        }
    }
#endif
}
