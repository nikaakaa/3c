using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentTimelineDocumentFragments
    {
        public static bool IsDefinitionFragment(string path)
        {
            return path.StartsWith("editable/timelines/", StringComparison.Ordinal) &&
                   (path.EndsWith("/timeline.json", StringComparison.Ordinal) ||
                    path.EndsWith("/curves.json", StringComparison.Ordinal));
        }

        public static bool TryDiscoverNew(
            IReadOnlyDictionary<string, JToken> candidates,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string directory in candidates.Keys
                         .Select(path => path.Substring(0, path.LastIndexOf('/')))
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string timelinePath = directory + "/timeline.json";
                string curvesPath = directory + "/curves.json";
                if (!candidates.TryGetValue(timelinePath, out JToken timelineToken) ||
                    !candidates.TryGetValue(curvesPath, out JToken curvesToken))
                {
                    report.Error(directory, "timeline_new_pair_incomplete", "新增Timeline必须同时提供同目录timeline.json与curves.json。");
                    valid = false;
                    continue;
                }
                if (!AgentAuthoringDocumentCodec.TryConvertToken(timelineToken, timelinePath, report, out AgentPackageTimelineFile timeline) ||
                    !AgentAuthoringDocumentCodec.TryConvertToken(curvesToken, curvesPath, report, out AgentPackageCurvesFile curves))
                {
                    valid = false;
                    continue;
                }
                bool localTimeline = IsLocal(timeline.id);
                bool localCallSite = timeline.callSites?.Count == 1 &&
                                     timeline.callSites[0] != null &&
                                     IsLocal(timeline.callSites[0].nodeAuthoringId) &&
                                     !string.IsNullOrWhiteSpace(timeline.callSites[0].graphPath);
                bool localContents = timeline.sections != null &&
                    timeline.sections.All(section => section != null && IsLocal(section.sectionAuthoringId)) &&
                    timeline.tracks != null && timeline.tracks.All(track =>
                        track != null &&
                        IsLocal(track.trackAuthoringId) &&
                        track.clips != null &&
                        track.clips.All(clip => clip != null && IsLocal(clip.clipAuthoringId)));
                string expectedDirectory = $"editable/timelines/{AgentAuthoringPackageMapper.Segment(timeline.id)}";
                if (!localTimeline ||
                    !localCallSite ||
                    !localContents ||
                    !string.Equals(curves.timelineId, timeline.id, StringComparison.Ordinal) ||
                    !string.Equals(directory, expectedDirectory, StringComparison.Ordinal))
                {
                    report.Error(
                        timelinePath,
                        "timeline_new_pair_invalid",
                        "新增Timeline必须使用canonical local identity目录、唯一local TimelineNode调用点、local Section/Track/Clip，并保持curves timelineId一致。");
                    valid = false;
                    continue;
                }
                result.Add(timelinePath);
                result.Add(curvesPath);
            }
            discovered = result;
            return valid;
        }

        static bool IsLocal(string identity)
        {
            return !string.IsNullOrWhiteSpace(identity) &&
                   identity.StartsWith("local:", StringComparison.Ordinal) &&
                   identity.Length > "local:".Length &&
                   identity.Substring("local:".Length).All(character =>
                       char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }
    }
}
