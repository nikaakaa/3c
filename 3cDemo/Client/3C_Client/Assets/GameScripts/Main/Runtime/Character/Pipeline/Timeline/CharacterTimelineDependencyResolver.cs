using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonGameplay.Tick;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelineDependencyResolver : ITimelineRuntimeDependencyResolver
    {
        readonly Dictionary<string, string> m_GraphRevisions = new(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_CurveRevisions = new(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_CameraEffectRevisions = new(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineRuntimeDependencyHandle> m_Handles = new(StringComparer.Ordinal);
        TimelineRuntimeNumericTarget m_NumericTarget;

        internal void InstallGraphSources(IReadOnlyList<ProgramSourceMapEntry> sources, bool presentationPrograms = false)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                ProgramSourceMapEntry source = sources[i];
                if (string.IsNullOrEmpty(source.GraphId) || string.IsNullOrEmpty(source.ContentHash))
                    continue;
                string identity = $"tree:{source.GraphId}";
                if (m_GraphRevisions.TryGetValue(identity, out string revision) && revision != source.ContentHash)
                    throw new InvalidOperationException($"Compiled Timeline graph '{identity}' has conflicting revisions.");
                m_GraphRevisions[identity] = source.ContentHash;
            }
            InstallGraphCalls(sources, presentationPrograms);
        }

        internal void InstallContent(IEnumerable<TimelineData> timelines, TimelineRuntimeNumericTarget numericTarget)
        {
            m_NumericTarget = numericTarget;
            m_CurveRevisions.Clear();
            m_CameraEffectRevisions.Clear();
            m_Handles.Clear();
            foreach (TimelineData timeline in timelines)
            {
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        if (track.Clips[clipIndex] is CameraEffectClip camera)
                        {
                            if (!camera.Effect)
                                throw new InvalidOperationException($"Timeline camera effect '{camera.AuthoringId}' has no resource.");
                            string identity = $"camera:{TimelineContractKinds.CameraEffectClip}:{camera.Effect.EffectId}";
                            string cameraRevision = SourceContentHasher.Hash(JsonUtility.ToJson(camera.Effect));
                            if (m_CameraEffectRevisions.TryGetValue(identity, out string installed) && installed != cameraRevision)
                                throw new InvalidOperationException($"Timeline camera effect '{identity}' has conflicting revisions.");
                            m_CameraEffectRevisions[identity] = cameraRevision;
                        }
                        if (track.Clips[clipIndex] is not MotionCurveClip motion)
                            continue;
                        if (!motion.SourceCurve || !motion.SourceCurve.TryValidate(out string error))
                            throw new InvalidOperationException($"Timeline motion source '{motion.AuthoringId}' is invalid.");
                        string revision = SourceContentHasher.Hash(JsonUtility.ToJson(motion.SourceCurve));
                        m_CurveRevisions[$"motion-curve:{revision}"] = revision;
                    }
                }
            }
            foreach (string identity in m_GraphRevisions.Keys)
                m_Handles.Add(identity, new TimelineRuntimeDependencyHandle(m_Handles.Count + 1));
            foreach (string identity in m_CurveRevisions.Keys)
                m_Handles.Add(identity, new TimelineRuntimeDependencyHandle(m_Handles.Count + 1));
            foreach (string identity in m_CameraEffectRevisions.Keys)
                m_Handles.Add(identity, new TimelineRuntimeDependencyHandle(m_Handles.Count + 1));
        }

        public bool TryResolve(
            TimelineContentDependency dependency,
            TimelineRuntimeNumericTarget numericTarget,
            out TimelineRuntimeDependencyHandle handle,
            out string error)
        {
            handle = TimelineRuntimeDependencyHandle.Invalid;
            Dictionary<string, string> revisions = dependency.Kind switch
            {
                "timeline.tree" => m_GraphRevisions,
                "timeline.motion-curve" => m_CurveRevisions,
                TimelineContractKinds.CameraEffectClip => m_CameraEffectRevisions,
                _ => null
            };
            if (numericTarget != m_NumericTarget || revisions == null ||
                !revisions.TryGetValue(dependency.Identity, out string revision) ||
                !string.Equals(revision, dependency.ContentHash, StringComparison.Ordinal) ||
                !m_Handles.TryGetValue(dependency.Identity, out handle))
            {
                error = $"Timeline dependency '{dependency.Identity}' has no installed {numericTarget} resource matching revision '{dependency.ContentHash}'.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        sealed class CompiledGraphCall
        {
            internal TimelineGraphBinding Binding;
            internal string[] Dependencies;
        }

        readonly Dictionary<(string Caller, ProgramInvocationCallerKind Kind), CompiledGraphCall> m_GraphCalls = new();

        internal bool TryValidateGraphBindings(TimelineContentUnit content, out string error)
        {
            for (int i = 0; i < content.Dependencies.Count; i++)
            {
                TimelineContentDependency dependency = content.Dependencies[i];
                if (dependency.Kind == "timeline.tree" &&
                    (!m_GraphRevisions.TryGetValue(dependency.Identity, out string revision) || revision != dependency.ContentHash))
                {
                    error = $"图 '{dependency.Identity}' 的当前内容未编译，需要重建并重新预览。";
                    return false;
                }
            }
            for (int i = 0; i < content.Clips.Count; i++)
            {
                TimelineContentClip clip = content.Clips[i];
                if (clip.TrackMuted || clip.ContractKind != TimelineContractKinds.TreeClip)
                    continue;
                ProgramInvocationCallerKind kind = clip.ExecutionPolicy.IsPresentation
                    ? ProgramInvocationCallerKind.PresentationTreeClip : ProgramInvocationCallerKind.TimelineClip;
                if (!TryValidateGraphCall(clip.AuthoringId, kind, clip.GraphBinding, out error))
                    return false;
            }
            for (int i = 0; i < content.Markers.Count; i++)
            {
                TimelineContentMarker marker = content.Markers[i];
                if (marker.TrackMuted)
                    continue;
                ProgramInvocationCallerKind kind = marker.ExecutionPolicy.IsPresentation
                    ? ProgramInvocationCallerKind.PresentationMarker : ProgramInvocationCallerKind.TimelineClip;
                if (!TryValidateGraphCall(marker.MarkerId, kind,
                        new TimelineGraphBinding(marker.GraphId, marker.GraphRevision), out error))
                    return false;
            }
            error = string.Empty;
            return true;
        }

        bool TryValidateGraphCall(string caller, ProgramInvocationCallerKind kind, TimelineGraphBinding binding, out string error)
        {
            if (!m_GraphCalls.TryGetValue((caller, kind), out CompiledGraphCall call) || call.Binding.GraphId != binding.GraphId)
            {
                error = $"Timeline 调用 '{caller}' 的图或执行域绑定发生变化，需要重建并重新预览。";
                return false;
            }
            error = string.Empty;
            return true;
        }

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
