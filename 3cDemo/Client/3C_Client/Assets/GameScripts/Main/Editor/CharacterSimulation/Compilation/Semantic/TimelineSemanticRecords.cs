using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class TimelineSemanticContentRecord
    {
        internal TimelineSemanticContentRecord(
            TimelineData timeline,
            string route,
            int maxFrame,
            TimelineContentUnit contentUnit,
            IEnumerable<TimelineSemanticTrackRecord> tracks,
            IReadOnlyDictionary<string, TimelineSemanticTreeRecord> treeRecords)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            if (maxFrame < 0)
                throw new ArgumentOutOfRangeException(nameof(maxFrame));
            ContentUnit = contentUnit ?? throw new ArgumentNullException(nameof(contentUnit));
            MaxFrame = maxFrame;
            Tracks = Array.AsReadOnly((tracks ?? Array.Empty<TimelineSemanticTrackRecord>()).ToArray());
            TreeRecords = new ReadOnlyDictionary<string, TimelineSemanticTreeRecord>(
                new Dictionary<string, TimelineSemanticTreeRecord>(treeRecords ?? new Dictionary<string, TimelineSemanticTreeRecord>(), StringComparer.Ordinal));
        }

        public TimelineData Timeline { get; }
        public string Route { get; }
        public int MaxFrame { get; }
        public TimelineContentUnit ContentUnit { get; }
        public IReadOnlyList<TimelineSemanticTrackRecord> Tracks { get; }
        public IReadOnlyDictionary<string, TimelineSemanticTreeRecord> TreeRecords { get; }
    }

    public sealed class TimelineSemanticTreeRecord
    {
        internal TimelineSemanticTreeRecord(TreeClip clip, TimelineRunningTree tree, string route)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public TreeClip Clip { get; }
        public TimelineRunningTree Tree { get; }
        public string Route { get; }
    }

    public sealed class TimelineSemanticTrackRecord
    {
        internal TimelineSemanticTrackRecord(
            Track track,
            int authoringIndex,
            string route,
            IEnumerable<TimelineSemanticClipRecord> clips)
        {
            Track = track ?? throw new ArgumentNullException(nameof(track));
            AuthoringIndex = authoringIndex;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Clips = Array.AsReadOnly((clips ?? Array.Empty<TimelineSemanticClipRecord>()).ToArray());
        }

        public Track Track { get; }
        public int AuthoringIndex { get; }
        public string Route { get; }
        public IReadOnlyList<TimelineSemanticClipRecord> Clips { get; }
    }

    public sealed class TimelineSemanticClipRecord
    {
        internal TimelineSemanticClipRecord(Clip clip, int authoringIndex, string route)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            AuthoringIndex = authoringIndex;
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public Clip Clip { get; }
        public int AuthoringIndex { get; }
        public string Route { get; }
    }
}
