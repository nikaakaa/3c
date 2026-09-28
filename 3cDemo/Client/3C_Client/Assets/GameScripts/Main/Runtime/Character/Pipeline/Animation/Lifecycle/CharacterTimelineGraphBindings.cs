using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed partial class CharacterTimelineDependencyResolver
    {
        sealed class CompiledGraphCall
        {
            internal TimelineGraphBinding Binding;
            internal string[] Dependencies;
        }

        readonly Dictionary<(string Caller, ProgramInvocationCallerKind Kind), CompiledGraphCall> m_GraphCalls = new();

        void InstallGraphCalls(IReadOnlyList<ProgramSourceMapEntry> sources, bool presentationPrograms)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                ProgramSourceMapEntry source = sources[i];
                if (source.TargetKind != ProgramSourceTargetKind.GraphInvocation)
                    continue;
                bool presentation = source.InvocationCallerKind == ProgramInvocationCallerKind.PresentationTreeClip ||
                    source.InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker;
                if (presentationPrograms != presentation ||
                    (!presentation && source.InvocationCallerKind != ProgramInvocationCallerKind.TimelineClip))
                    continue;
                var key = (source.InvocationCallerClipId, source.InvocationCallerKind);
                string identity = "tree:" + source.GraphId;
                if (m_GraphCalls.TryGetValue(key, out CompiledGraphCall existing))
                {
                    if (existing.Binding.GraphId != identity || existing.Binding.Revision != source.ContentHash)
                        throw new InvalidOperationException($"Timeline caller '{key.Item1}' has conflicting compiled graph bindings.");
                    continue;
                }
                var dependencies = new HashSet<string>(StringComparer.Ordinal);
                string prefix = source.SourceInvocationPath + "/";
                for (int j = 0; j < sources.Count; j++)
                {
                    ProgramSourceMapEntry child = sources[j];
                    if (child.TargetKind == ProgramSourceTargetKind.GraphInvocation &&
                        (child.SourceInvocationPath == source.SourceInvocationPath ||
                         child.SourceInvocationPath.StartsWith(prefix, StringComparison.Ordinal)))
                        dependencies.Add("tree:" + child.GraphId);
                }
                var graphIds = new string[dependencies.Count];
                dependencies.CopyTo(graphIds);
                m_GraphCalls.Add(key, new CompiledGraphCall
                {
                    Binding = new TimelineGraphBinding(identity, source.ContentHash),
                    Dependencies = graphIds
                });
            }
        }

        public TimelineGraphBinding ResolveTreeClip(Clip clip, TimelineContentClosureBuilder closure) =>
            ResolveGraphCall(clip.AuthoringId, clip.ExecutionDomain == TimelineExecutionDomain.Presentation
                ? ProgramInvocationCallerKind.PresentationTreeClip : ProgramInvocationCallerKind.TimelineClip,
                "clip:" + clip.AuthoringId, closure);

        public TimelineGraphBinding ResolveMarker(TimelineMarker marker, TimelineContentClosureBuilder closure) =>
            ResolveGraphCall(marker.AuthoringId, marker.ExecutionDomain == TimelineExecutionDomain.Presentation
                ? ProgramInvocationCallerKind.PresentationMarker : ProgramInvocationCallerKind.TimelineClip,
                $"track:{marker.Track.AuthoringId}/marker:{marker.AuthoringId}", closure);

        TimelineGraphBinding ResolveGraphCall(string caller, ProgramInvocationCallerKind kind,
            string sourcePath, TimelineContentClosureBuilder closure)
        {
            if (!m_GraphCalls.TryGetValue((caller, kind), out CompiledGraphCall call))
                throw new InvalidOperationException($"Timeline caller '{caller}' has no installed compiled {kind} graph.");
            for (int i = 0; i < call.Dependencies.Length; i++)
            {
                string identity = call.Dependencies[i];
                closure.AddDependency(identity, "timeline.tree", $"{sourcePath}/graph:{identity}", m_GraphRevisions[identity]);
            }
            return call.Binding;
        }
    }
}
