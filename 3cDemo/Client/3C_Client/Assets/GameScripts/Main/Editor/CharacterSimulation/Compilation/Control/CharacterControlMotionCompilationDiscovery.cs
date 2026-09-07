using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterControlMotionCompilationRecord
    {
        internal CharacterControlMotionCompilationRecord(
            CharacterControlMotionDescriptor descriptor,
            CharacterAuthoringGraphOccurrence graph,
            CharacterAuthoringTimelineRecord timeline,
            MotionCurveClip clip)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
        }

        public CharacterControlMotionDescriptor Descriptor { get; }
        public CharacterAuthoringGraphOccurrence Graph { get; }
        public CharacterAuthoringTimelineRecord Timeline { get; }
        public MotionCurveClip Clip { get; }
    }

    public static class CharacterControlMotionCompilationDiscovery
    {
        public static IReadOnlyList<CharacterControlMotionCompilationRecord> Discover(
            IReadOnlyList<CharacterControlMotionDescriptor> descriptors,
            IReadOnlyList<CharacterCompositionRoot> roots,
            CharacterSimulationCompileReport report)
        {
            if (descriptors == null)
                throw new ArgumentNullException(nameof(descriptors));
            if (roots == null)
                throw new ArgumentNullException(nameof(roots));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            var clips = new Dictionary<string, List<(CharacterAuthoringGraphOccurrence Graph, CharacterAuthoringTimelineRecord Timeline, MotionCurveClip Clip)>>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
                Collect(roots[rootIndex].Occurrence);

            var result = new List<CharacterControlMotionCompilationRecord>();
            for (int descriptorIndex = 0; descriptorIndex < descriptors.Count; descriptorIndex++)
            {
                CharacterControlMotionDescriptor descriptor = descriptors[descriptorIndex];
                if (descriptor.DisplacementMode != CharacterControlMotionDisplacementMode.SourceCurve)
                    continue;
                if (!clips.TryGetValue(descriptor.SourceMotionIdentity, out var matches) || matches.Count != 1)
                {
                    report.DiscoveryError(
                        "control_motion_source_ambiguous",
                        descriptor.Binding,
                        $"Control motion '{descriptor.Binding}' source '{descriptor.SourceMotionIdentity}' resolves to {matches?.Count ?? 0} clips.");
                    continue;
                }
                (CharacterAuthoringGraphOccurrence graph, CharacterAuthoringTimelineRecord timeline, MotionCurveClip clip) = matches[0];
                result.Add(new CharacterControlMotionCompilationRecord(descriptor, graph, timeline, clip));
            }
            result.Sort((left, right) => string.CompareOrdinal(left.Descriptor.Binding, right.Descriptor.Binding));
            return new ReadOnlyCollection<CharacterControlMotionCompilationRecord>(result);

            void Collect(CharacterAuthoringGraphOccurrence occurrence)
            {
                if (occurrence == null || !visited.Add(occurrence.Route))
                    return;
                for (int timelineIndex = 0; timelineIndex < occurrence.Timelines.Count; timelineIndex++)
                {
                    CharacterAuthoringTimelineRecord timeline = occurrence.Timelines[timelineIndex];
                    for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                    {
                        TimelineSemanticTrackRecord track = timeline.Tracks[trackIndex];
                        for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                        {
                            if (track.Clips[clipIndex].Clip is not MotionCurveClip clip)
                                continue;
                            string identity = $"timeline:{timeline.Timeline.AuthoringId}/clip:{clip.AuthoringId}";
                            if (!clips.TryGetValue(identity, out var matches))
                            {
                                matches = new List<(CharacterAuthoringGraphOccurrence, CharacterAuthoringTimelineRecord, MotionCurveClip)>();
                                clips.Add(identity, matches);
                            }
                            matches.Add((occurrence, timeline, clip));
                        }
                    }
                }
                for (int referenceIndex = 0; referenceIndex < occurrence.GraphReferences.Count; referenceIndex++)
                    Collect(occurrence.GraphReferences[referenceIndex].Child);
                for (int edgeIndex = 0; edgeIndex < occurrence.Edges.Count; edgeIndex++)
                    Collect(occurrence.Edges[edgeIndex].ConditionGraph);
                for (int edgeIndex = 0; edgeIndex < occurrence.PropertyEdges.Count; edgeIndex++)
                    Collect(occurrence.PropertyEdges[edgeIndex].ConditionGraph);
                for (int timelineIndex = 0; timelineIndex < occurrence.Timelines.Count; timelineIndex++)
                {
                    foreach (CharacterAuthoringGraphOccurrence tree in occurrence.Timelines[timelineIndex].TreeGraphs.Values)
                        Collect(tree);
                }
            }
        }
    }
}
