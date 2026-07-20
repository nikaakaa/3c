using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterPresentationProgramIdentity
    {
        readonly string[] m_ProducerIdentities;

        public CharacterPresentationProgramIdentity(
            string programId,
            string sourceRevision,
            string semanticHash,
            IReadOnlyList<string> producerIdentities)
        {
            ProgramId = Require(programId, nameof(programId));
            SourceRevision = Require(sourceRevision, nameof(sourceRevision));
            SemanticHash = Require(semanticHash, nameof(semanticHash));
            if (producerIdentities == null)
                throw new ArgumentNullException(nameof(producerIdentities));
            m_ProducerIdentities = new string[producerIdentities.Count];
            for (int i = 0; i < m_ProducerIdentities.Length; i++)
                m_ProducerIdentities[i] = Require(producerIdentities[i], $"producerIdentities[{i}]");
        }

        public string ProgramId { get; }
        public string SourceRevision { get; }
        public string SemanticHash { get; }
        public IReadOnlyList<string> ProducerIdentities => m_ProducerIdentities;

        public static CharacterPresentationProgramIdentity From(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            var producers = new string[program.Producers.Count];
            for (int i = 0; i < producers.Length; i++)
                producers[i] = program.Producers[i].Identity;
            return new CharacterPresentationProgramIdentity(
                program.Manifest.ProgramId.Value,
                program.Manifest.SourceRevision.Value,
                program.Manifest.SemanticHash.ToString(),
                producers);
        }

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException($"Presentation Program identity requires '{field}'.", field);
            return value;
        }
    }

    public enum CharacterPresentationProducerKind
    {
        Animation,
        Camera,
        Cue
    }

    [Serializable]
    public sealed class CharacterPresentationProjection
    {
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_ProgramHash = string.Empty;
        [SerializeField] string m_SourceRevision = string.Empty;
        [SerializeField] string m_SemanticHash = string.Empty;
        [SerializeField] string m_NumericProfileId = string.Empty;
        [SerializeField] int m_TargetAbiVersion;
        [SerializeField] TransitionLibraryAsset m_TransitionLibrary;
        [SerializeField] CharacterAnimationLayerDefinition[] m_Layers = Array.Empty<CharacterAnimationLayerDefinition>();
        [SerializeField] CharacterPresentationProducerEntry[] m_Producers = Array.Empty<CharacterPresentationProducerEntry>();

        public string ProgramId => m_ProgramId;
        public string ProgramHash => m_ProgramHash;
        public string SourceRevision => m_SourceRevision;
        public string SemanticHash => m_SemanticHash;
        public string NumericProfileId => m_NumericProfileId;
        public int TargetAbiVersion => m_TargetAbiVersion;
        public TransitionLibraryAsset TransitionLibrary => m_TransitionLibrary;
        public IReadOnlyList<CharacterAnimationLayerDefinition> Layers => m_Layers ?? Array.Empty<CharacterAnimationLayerDefinition>();
        public IReadOnlyList<CharacterPresentationProducerEntry> Producers => m_Producers ?? Array.Empty<CharacterPresentationProducerEntry>();
        public bool IsValid => !string.IsNullOrEmpty(m_ProgramId) &&
                               !string.IsNullOrEmpty(m_ProgramHash) &&
                               !string.IsNullOrEmpty(m_SourceRevision) &&
                               !string.IsNullOrEmpty(m_SemanticHash) &&
                               !string.IsNullOrEmpty(m_NumericProfileId) &&
                               m_TargetAbiVersion > 0 &&
                               m_TransitionLibrary &&
                               Layers.Count > 0;

        public IReadOnlyList<CharacterPresentationProducerEntry> AnimationProducers
        {
            get
            {
                var values = new List<CharacterPresentationProducerEntry>();
                for (int i = 0; i < Producers.Count; i++)
                {
                    if (Producers[i].Kind == CharacterPresentationProducerKind.Animation)
                        values.Add(Producers[i]);
                }
                return values;
            }
        }

        public bool TryGetProducer(string programProducerIdentity, out CharacterPresentationProducerEntry producer)
        {
            for (int i = 0; i < Producers.Count; i++)
            {
                CharacterPresentationProducerEntry candidate = Producers[i];
                if (string.Equals(candidate.ProgramProducerIdentity, programProducerIdentity, StringComparison.Ordinal))
                {
                    producer = candidate;
                    return true;
                }
            }
            producer = null;
            return false;
        }

        public void RequireProgram(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (!IsValid ||
                !string.Equals(m_ProgramId, program.Manifest.ProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(m_ProgramHash, program.ProgramHash.ToString(), StringComparison.Ordinal) ||
                !string.Equals(m_SourceRevision, program.Manifest.SourceRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(m_SemanticHash, program.Manifest.SemanticHash.ToString(), StringComparison.Ordinal) ||
                !string.Equals(m_NumericProfileId, program.Manifest.NumericProfile.Id.Value, StringComparison.Ordinal) ||
                m_TargetAbiVersion != program.Manifest.NumericProfile.AbiVersion.Value)
                throw new InvalidOperationException("Character Presentation Projection does not match the loaded Program artifact.");
        }

        public void RequireSemanticProgram(CharacterPresentationProgramIdentity program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (!IsValid ||
                !string.Equals(m_ProgramId, program.ProgramId, StringComparison.Ordinal) ||
                !string.Equals(m_SourceRevision, program.SourceRevision, StringComparison.Ordinal) ||
                !string.Equals(m_SemanticHash, program.SemanticHash, StringComparison.Ordinal) ||
                Producers.Count != program.ProducerIdentities.Count)
            {
                throw new InvalidOperationException("Character Presentation Projection does not match the loaded semantic Program identity.");
            }
            for (int i = 0; i < Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = Producers[i];
                if (producer == null || producer.ProgramProducerIndex != i ||
                    !string.Equals(producer.ProgramProducerIdentity, program.ProducerIdentities[i], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Character Presentation Projection producer #{i} does not match the loaded semantic Program identity.");
                }
            }
        }

        public static CharacterPresentationProjection Build(
            CharacterSimulationProgram program,
            CharacterAnimationPresentationProfile profile,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>> markerSyncCallSites,
            List<string> errors)
        {
            var projection = new CharacterPresentationProjection();
            if (program == null || profile == null || timelines == null || markerSyncCallSites == null)
            {
                errors?.Add("Character Presentation Projection build input is incomplete.");
                return projection;
            }

            projection.m_ProgramId = program.Manifest.ProgramId.Value;
            projection.m_ProgramHash = program.ProgramHash.ToString();
            projection.m_SourceRevision = program.Manifest.SourceRevision.Value;
            projection.m_SemanticHash = program.Manifest.SemanticHash.ToString();
            projection.m_NumericProfileId = program.Manifest.NumericProfile.Id.Value;
            projection.m_TargetAbiVersion = program.Manifest.NumericProfile.AbiVersion.Value;
            projection.m_TransitionLibrary = profile.TransitionLibrary;
            projection.m_Layers = CopyLayers(profile.Layers);
            ValidateLayers(projection, errors);
            if (!ValidateMarkerSyncAuthoring(program, timelines, markerSyncCallSites, errors))
                return projection;

            var entries = new List<CharacterPresentationProducerEntry>();
            var animationIds = new HashSet<AnimationProducerId>();
            for (int i = 0; i < program.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry entry = BuildProducer(
                    program,
                    program.Producers[i],
                    profile,
                    timelines,
                    errors);
                if (entry == null)
                    continue;
                entries.Add(entry);
                if (entry.Kind == CharacterPresentationProducerKind.Animation)
                    animationIds.Add(entry.ProducerId);
            }
            for (int i = 0; i < profile.ProducerBindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = profile.ProducerBindings[i];
                if (binding != null && binding.ProducerId.IsValid && !animationIds.Contains(binding.ProducerId))
                    errors?.Add($"Animation producer binding '{binding.ProducerId}' is orphaned from the compiled Program.");
            }
            entries.Sort((left, right) => left.ProgramProducerIndex.CompareTo(right.ProgramProducerIndex));
            projection.m_Producers = entries.ToArray();
            return projection;
        }

        static CharacterPresentationProducerEntry BuildProducer(
            CharacterSimulationProgram program,
            ProgramProducer producer,
            CharacterAnimationPresentationProfile profile,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            CharacterPresentationProducerKind? kind = ResolveKind(program, producer, errors);
            ProgramSourceMapEntry source = ResolveSource(program, producer, errors);
            if (!kind.HasValue || source == null)
                return null;

            if (kind.Value != CharacterPresentationProducerKind.Animation)
            {
                CharacterPresentationCameraBinding camera = kind.Value == CharacterPresentationProducerKind.Camera
                    ? BuildCameraBinding(program, producer, source, timelines, errors)
                    : null;
                CharacterPresentationCueBinding cue = kind.Value == CharacterPresentationProducerKind.Cue
                    ? BuildCueBinding(producer, source, timelines, errors)
                    : null;
                if (kind.Value == CharacterPresentationProducerKind.Camera && camera == null ||
                    kind.Value == CharacterPresentationProducerKind.Cue && cue == null)
                    return null;
                return new CharacterPresentationProducerEntry(
                    producer.Index,
                    producer.Identity,
                    kind.Value,
                    string.Empty,
                    string.Empty,
                    producer.LayerId,
                    source.GraphId,
                    source.NodeId,
                    source.TimelineId,
                    ParseTrackId(producer.SourceIdentity),
                    source.DisplayPath,
                    null,
                    camera,
                    cue);
            }

            if (!TryParseAnimationSource(producer.SourceIdentity, out AnimationProducerId producerId) ||
                !string.Equals(source.TimelineId, producerId.TimelineAuthoringId, StringComparison.Ordinal))
            {
                errors?.Add($"Animation producer '{producer.Identity}' has an invalid source identity.");
                return null;
            }
            if (!timelines.TryGetValue(producerId.TimelineAuthoringId, out TimelineData timeline))
            {
                errors?.Add($"Animation producer '{producer.Identity}' Timeline source is absent from the compiler inventory.");
                return null;
            }
            AnimationTrack track = null;
            for (int i = 0; i < timeline.Tracks.Count; i++)
            {
                if (timeline.Tracks[i] is AnimationTrack candidate &&
                    string.Equals(candidate.AuthoringId, producerId.TrackAuthoringId, StringComparison.Ordinal))
                {
                    track = candidate;
                    break;
                }
            }
            if (track == null || !string.Equals(track.LayerId, producer.LayerId, StringComparison.Ordinal))
            {
                errors?.Add($"Animation producer '{producer.Identity}' Track source or Layer binding is invalid.");
                return null;
            }
            AnimationProducerPresentationBinding authoringBinding = profile.FindProducerBinding(producerId);
            if (authoringBinding == null || !authoringBinding.Transition || !authoringBinding.Transition.IsValid)
            {
                errors?.Add($"Animation producer '{producerId}' has no valid Animancer transition binding.");
                return null;
            }
            if (!profile.TransitionLibrary || profile.TransitionLibrary.Library == null ||
                !profile.TransitionLibrary.Library.TryGetTransition(authoringBinding.Transition.Key, out _))
            {
                errors?.Add($"Animation producer '{producerId}' transition is absent from the configured Transition Library.");
                return null;
            }
            if (authoringBinding.Transition.FadeMode == FadeMode.FromStart ||
                authoringBinding.Transition.FadeMode == FadeMode.NormalizedFromStart)
            {
                errors?.Add($"Animation producer '{producerId}' uses unsupported Timeline-owned FadeMode '{authoringBinding.Transition.FadeMode}'.");
                return null;
            }

            var clips = new List<CharacterPresentationAnimationClipBinding>();
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is not BTSMTL.Timeline.AnimationClip clip)
                    continue;
                if (!clip.Clip)
                {
                    errors?.Add($"Animation producer '{producerId}' clip '{clip.AuthoringId}' has no AnimationClip resource.");
                    continue;
                }
                clips.Add(new CharacterPresentationAnimationClipBinding(clip));
            }
            if (clips.Count == 0)
            {
                errors?.Add($"Animation producer '{producerId}' has no compiled AnimationClip binding.");
                return null;
            }
            var animation = new CharacterPresentationAnimationBinding(
                authoringBinding.Transition,
                authoringBinding.Easing,
                track.Name,
                clips.ToArray(),
                AnimationMarkerSyncBinding.Compile(track, timeline));
            return new CharacterPresentationProducerEntry(
                producer.Index,
                producer.Identity,
                kind.Value,
                producerId.TimelineAuthoringId,
                producerId.TrackAuthoringId,
                producer.LayerId,
                source.GraphId,
                source.NodeId,
                source.TimelineId,
                producerId.TrackAuthoringId,
                source.DisplayPath,
                animation,
                null,
                null);
        }

        static CharacterPresentationCameraBinding BuildCameraBinding(
            CharacterSimulationProgram program,
            ProgramProducer producer,
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            if (TryFindSourceClip(source, timelines, out Clip clip))
            {
                if (clip is CameraStateClip state)
                {
                    return CharacterPresentationCameraBinding.State(
                        state.Mode,
                        state.Priority,
                        state.BlendInSeconds,
                        state.BlendOutSeconds,
                        state.TargetKey,
                        state.InterruptPolicy);
                }
                if (clip is CameraCueClip cue)
                {
                    return CharacterPresentationCameraBinding.Cue(
                        cue.CueId,
                        cue.CueKind,
                        cue.CueType,
                        cue.DurationSeconds,
                        cue.Priority);
                }
                if (clip is CameraResponseClip response)
                {
                    return CharacterPresentationCameraBinding.Response(
                        response.LookResponse,
                        response.ManualOrbitWeight,
                        response.PitchResponseWeight,
                        response.YawResponseWeight,
                        response.Priority);
                }
                errors?.Add($"Camera producer '{producer.Identity}' source clip type '{clip.GetType().Name}' is unsupported.");
                return null;
            }

            try
            {
                SimulationOperation operation = RequireProducerOperation(program, producer);
                if (operation.Integer0 != CameraProgramOperationSchema.PayloadVersion)
                    throw new InvalidOperationException(
                        $"payload version '{operation.Integer0}' is unsupported");
                return operation.Code switch
                {
                    SimulationOperationCode.CameraStateRequest => CharacterPresentationCameraBinding.State(
                        (TimelineCameraMode)operation.Integer1,
                        RequireInt32(program, operation, "Priority"),
                        RequireScalar(program, operation, "BlendInSeconds"),
                        RequireScalar(program, operation, "BlendOutSeconds"),
                        RequireString(program, operation, "TargetKey"),
                        (TimelineCameraInterruptPolicy)operation.Flags),
                    SimulationOperationCode.CameraCue => CharacterPresentationCameraBinding.Cue(
                        RequireString(program, operation, "CueId"),
                        (TimelineCameraCueKind)operation.Integer1,
                        RequireString(program, operation, "CueType"),
                        RequireScalar(program, operation, "DurationSeconds"),
                        RequireInt32(program, operation, "Priority")),
                    SimulationOperationCode.CameraResponse => CharacterPresentationCameraBinding.Response(
                        (TimelineCameraLookResponseMode)operation.Integer1,
                        RequireScalar(program, operation, "ManualOrbitWeight"),
                        RequireScalar(program, operation, "PitchResponseWeight"),
                        RequireScalar(program, operation, "YawResponseWeight"),
                        RequireInt32(program, operation, "Priority")),
                    SimulationOperationCode.CameraTarget => CharacterPresentationCameraBinding.Target(
                        RequireString(program, operation, "TargetKey"),
                        RequireString(program, operation, "AnchorKey"),
                        RequireString(program, operation, "AimPointKey"),
                        RequireString(program, operation, "PreferredBoneKey"),
                        RequireInt32(program, operation, "Priority")),
                    _ => throw new InvalidOperationException($"operation '{operation.Code}' is unsupported")
                };
            }
            catch (Exception exception)
            {
                errors?.Add($"Camera producer '{producer.Identity}' Graph payload is invalid: {exception.Message}.");
                return null;
            }
        }

        static SimulationOperation RequireProducerOperation(
            CharacterSimulationProgram program,
            ProgramProducer producer)
        {
            SimulationOperation result = null;
            for (int i = 0; i < program.References.Count; i++)
            {
                ProgramReference reference = program.References[i];
                if (reference.Kind != ProgramReferenceKind.Producer ||
                    reference.TargetIndex != producer.Index ||
                    !reference.HasSourceOperation)
                    continue;
                SimulationOperation candidate = program.Operations[reference.SourceOperation.Value];
                if (!CameraProgramOperationSchema.IsCameraOperation(candidate.Code))
                    continue;
                if (result != null)
                    throw new InvalidOperationException("multiple Camera operations reference the same producer");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException("compiled Camera operation is missing");
        }

        static ProgramConstant RequireConstant(
            CharacterSimulationProgram program,
            SimulationOperation operation,
            string field)
        {
            ProgramConstant result = null;
            string suffix = "/constant/" + field;
            for (int i = 0; i < operation.ConstantReferences.Count; i++)
            {
                ProgramConstant candidate = program.Constants[operation.ConstantReferences[i]];
                if (!candidate.Identity.EndsWith(suffix, StringComparison.Ordinal))
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"field '{field}' is duplicated");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException($"field '{field}' is missing");
        }

        static int RequireInt32(CharacterSimulationProgram program, SimulationOperation operation, string field)
        {
            ProgramConstant value = RequireConstant(program, operation, field);
            if (value.Kind != ProgramConstantKind.Int32)
                throw new InvalidOperationException($"field '{field}' is not Int32");
            return value.Int32;
        }

        static float RequireScalar(CharacterSimulationProgram program, SimulationOperation operation, string field)
        {
            ProgramConstant value = RequireConstant(program, operation, field);
            if (value.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException($"field '{field}' is not Scalar");
            return value.Scalar.ToSingle();
        }

        static string RequireString(CharacterSimulationProgram program, SimulationOperation operation, string field)
        {
            ProgramConstant value = RequireConstant(program, operation, field);
            if (value.Kind != ProgramConstantKind.String)
                throw new InvalidOperationException($"field '{field}' is not String");
            return value.Text;
        }

        static CharacterPresentationCueBinding BuildCueBinding(
            ProgramProducer producer,
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            if (TryFindSourceClip(source, timelines, out Clip clip))
            {
                if (clip is ActionCueClip cue)
                    return new CharacterPresentationCueBinding(cue.CueId, cue.CueType);
                errors?.Add($"Cue producer '{producer.Identity}' source clip type '{clip.GetType().Name}' is unsupported.");
                return null;
            }
            const string cueMarker = ":cue:";
            int marker = producer.Identity.LastIndexOf(cueMarker, StringComparison.Ordinal);
            if (marker >= 0)
            {
                string suffix = producer.Identity.Substring(marker + cueMarker.Length);
                int separator = suffix.IndexOf(':');
                string cueId = separator >= 0 ? suffix.Substring(separator + 1) : suffix;
                if (!string.IsNullOrEmpty(cueId))
                    return new CharacterPresentationCueBinding(cueId, "GameplayEffect");
            }
            errors?.Add($"Cue producer '{producer.Identity}' has no resolvable authoring payload.");
            return null;
        }

        static bool TryFindSourceClip(
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            out Clip clip)
        {
            clip = null;
            if (source == null || string.IsNullOrEmpty(source.TimelineId) || string.IsNullOrEmpty(source.ClipId) ||
                !timelines.TryGetValue(source.TimelineId, out TimelineData timeline))
                return false;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (string.Equals(track.Clips[clipIndex].AuthoringId, source.ClipId, StringComparison.Ordinal))
                    {
                        clip = track.Clips[clipIndex];
                        return true;
                    }
                }
            }
            return false;
        }

        static CharacterPresentationProducerKind? ResolveKind(
            CharacterSimulationProgram program,
            ProgramProducer producer,
            List<string> errors)
        {
            bool animation = false;
            bool camera = false;
            bool cue = false;
            for (int i = 0; i < program.References.Count; i++)
            {
                ProgramReference reference = program.References[i];
                if (reference.Kind != ProgramReferenceKind.Producer ||
                    reference.TargetIndex != producer.Index || !reference.HasSourceOperation)
                    continue;
                switch (program.Operations[reference.SourceOperation.Value].Code)
                {
                    case SimulationOperationCode.TimelineAnimation:
                        animation = true;
                        break;
                    case SimulationOperationCode.TimelineCameraState:
                    case SimulationOperationCode.TimelineCameraCue:
                    case SimulationOperationCode.TimelineCameraResponse:
                    case SimulationOperationCode.CameraStateRequest:
                    case SimulationOperationCode.CameraCue:
                    case SimulationOperationCode.CameraResponse:
                    case SimulationOperationCode.CameraTarget:
                        camera = true;
                        break;
                    case SimulationOperationCode.TimelineCue:
                        cue = true;
                        break;
                }
            }
            int count = (animation ? 1 : 0) + (camera ? 1 : 0) + (cue ? 1 : 0);
            if (count == 0 && string.Equals(producer.LayerId, "Cue", StringComparison.Ordinal))
                return CharacterPresentationProducerKind.Cue;
            if (count != 1)
            {
                errors?.Add($"Presentation producer '{producer.Identity}' has no unique compiled producer kind.");
                return null;
            }
            return animation
                ? CharacterPresentationProducerKind.Animation
                : camera
                    ? CharacterPresentationProducerKind.Camera
                    : CharacterPresentationProducerKind.Cue;
        }

        static ProgramSourceMapEntry ResolveSource(
            CharacterSimulationProgram program,
            ProgramProducer producer,
            List<string> errors)
        {
            ProgramSourceMapEntry source = null;
            int count = 0;
            for (int i = 0; i < program.SourceMap.Count; i++)
            {
                ProgramSourceMapEntry candidate = program.SourceMap[i];
                if (candidate.TargetKind != ProgramSourceTargetKind.Producer || candidate.TargetIndex != producer.Index)
                    continue;
                source = candidate;
                count++;
            }
            if (count == 1)
                return source;
            errors?.Add($"Presentation producer '{producer.Identity}' requires exactly one source-map entry, found {count}.");
            return null;
        }

        static CharacterAnimationLayerDefinition[] CopyLayers(IReadOnlyList<CharacterAnimationLayerDefinition> source)
        {
            var result = new CharacterAnimationLayerDefinition[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                CharacterAnimationLayerDefinition layer = source[i];
                result[i] = layer == null
                    ? null
                    : new CharacterAnimationLayerDefinition(
                        layer.Id,
                        layer.AnimancerLayerIndex,
                        layer.AvatarMask,
                        layer.BlendMode,
                        layer.OutputPolicy);
            }
            return result;
        }

        static void ValidateLayers(CharacterPresentationProjection projection, List<string> errors)
        {
            if (!projection.m_TransitionLibrary)
                errors?.Add("Character Presentation Projection requires a Transition Library.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var indices = new HashSet<int>();
            for (int i = 0; i < projection.m_Layers.Length; i++)
            {
                CharacterAnimationLayerDefinition layer = projection.m_Layers[i];
                if (layer == null || string.IsNullOrEmpty(layer.Id) || !ids.Add(layer.Id) ||
                    layer.AnimancerLayerIndex < 0 || !indices.Add(layer.AnimancerLayerIndex) ||
                    layer.OutputPolicy == AnimationLayerOutputPolicy.Unspecified)
                    errors?.Add($"Character Presentation layer #{i} is invalid or duplicated.");
            }
        }

        static bool TryParseAnimationSource(string sourceIdentity, out AnimationProducerId producerId)
        {
            producerId = default;
            const string timelinePrefix = "timeline:";
            const string trackSeparator = "/track:";
            if (string.IsNullOrEmpty(sourceIdentity) || !sourceIdentity.StartsWith(timelinePrefix, StringComparison.Ordinal))
                return false;
            int separator = sourceIdentity.IndexOf(trackSeparator, timelinePrefix.Length, StringComparison.Ordinal);
            if (separator < 0)
                return false;
            producerId = new AnimationProducerId(
                sourceIdentity.Substring(timelinePrefix.Length, separator - timelinePrefix.Length),
                sourceIdentity.Substring(separator + trackSeparator.Length));
            return producerId.IsValid;
        }

        static string ParseTrackId(string sourceIdentity)
        {
            const string trackSeparator = "/track:";
            int separator = string.IsNullOrEmpty(sourceIdentity)
                ? -1
                : sourceIdentity.IndexOf(trackSeparator, StringComparison.Ordinal);
            return separator < 0 ? string.Empty : sourceIdentity.Substring(separator + trackSeparator.Length);
        }

        static bool ValidateMarkerSyncAuthoring(
            CharacterSimulationProgram program,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>> callSites,
            List<string> errors)
        {
            var inputs = new List<AnimationMarkerSyncAuthoringInput>();
            for (int producerIndex = 0; producerIndex < program.Producers.Count; producerIndex++)
            {
                ProgramProducer producer = program.Producers[producerIndex];
                if (!TryParseAnimationSource(producer.SourceIdentity, out AnimationProducerId producerId) ||
                    !timelines.TryGetValue(producerId.TimelineAuthoringId, out TimelineData timeline))
                    continue;
                AnimationTrack track = null;
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Tracks[trackIndex] is AnimationTrack candidate &&
                        string.Equals(candidate.AuthoringId, producerId.TrackAuthoringId, StringComparison.Ordinal))
                    {
                        track = candidate;
                        break;
                    }
                }
                if (track == null)
                    continue;
                callSites.TryGetValue(producerId.TimelineAuthoringId, out IReadOnlyList<AnimationMarkerSyncCallSite> producerCallSites);
                inputs.Add(new AnimationMarkerSyncAuthoringInput(
                    producer.Identity,
                    timeline,
                    track,
                    producerCallSites ?? Array.Empty<AnimationMarkerSyncCallSite>()));
            }

            var issues = new List<AnimationMarkerSyncAuthoringIssue>();
            AnimationMarkerSyncAuthoring.Validate(inputs, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                AnimationMarkerSyncAuthoringIssue issue = issues[i];
                errors?.Add($"{issue.Code} [{issue.AuthoringPath}]: {issue.Message}");
            }
            return issues.Count == 0;
        }
    }

    [Serializable]
    public sealed class CharacterPresentationProducerEntry
    {
        [SerializeField] int m_ProgramProducerIndex;
        [SerializeField] string m_ProgramProducerIdentity = string.Empty;
        [SerializeField] CharacterPresentationProducerKind m_Kind;
        [SerializeField] string m_TimelineAuthoringId = string.Empty;
        [SerializeField] string m_TrackAuthoringId = string.Empty;
        [SerializeField] string m_LayerId = string.Empty;
        [SerializeField] string m_SourceGraphId = string.Empty;
        [SerializeField] string m_SourceNodeId = string.Empty;
        [SerializeField] string m_SourceTimelineId = string.Empty;
        [SerializeField] string m_SourceTrackId = string.Empty;
        [SerializeField] string m_SourceDisplayPath = string.Empty;
        [SerializeField] CharacterPresentationAnimationBinding m_Animation;
        [SerializeField] CharacterPresentationCameraBinding m_Camera;
        [SerializeField] CharacterPresentationCueBinding m_Cue;

        public CharacterPresentationProducerEntry(
            int programProducerIndex,
            string programProducerIdentity,
            CharacterPresentationProducerKind kind,
            string timelineAuthoringId,
            string trackAuthoringId,
            string layerId,
            string sourceGraphId,
            string sourceNodeId,
            string sourceTimelineId,
            string sourceTrackId,
            string sourceDisplayPath,
            CharacterPresentationAnimationBinding animation,
            CharacterPresentationCameraBinding camera,
            CharacterPresentationCueBinding cue)
        {
            m_ProgramProducerIndex = programProducerIndex;
            m_ProgramProducerIdentity = programProducerIdentity ?? string.Empty;
            m_Kind = kind;
            m_TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            m_TrackAuthoringId = trackAuthoringId ?? string.Empty;
            m_LayerId = layerId ?? string.Empty;
            m_SourceGraphId = sourceGraphId ?? string.Empty;
            m_SourceNodeId = sourceNodeId ?? string.Empty;
            m_SourceTimelineId = sourceTimelineId ?? string.Empty;
            m_SourceTrackId = sourceTrackId ?? string.Empty;
            m_SourceDisplayPath = sourceDisplayPath ?? string.Empty;
            m_Animation = animation;
            m_Camera = camera;
            m_Cue = cue;
        }

        public int ProgramProducerIndex => m_ProgramProducerIndex;
        public string ProgramProducerIdentity => m_ProgramProducerIdentity;
        public CharacterPresentationProducerKind Kind => m_Kind;
        public AnimationProducerId ProducerId => new AnimationProducerId(m_TimelineAuthoringId, m_TrackAuthoringId);
        public string LayerId => m_LayerId;
        public string SourceGraphId => m_SourceGraphId;
        public string SourceNodeId => m_SourceNodeId;
        public string SourceTimelineId => m_SourceTimelineId;
        public string SourceTrackId => m_SourceTrackId;
        public string SourceDisplayPath => m_SourceDisplayPath;
        public CharacterPresentationAnimationBinding Animation => m_Animation;
        public CharacterPresentationCameraBinding Camera => m_Camera;
        public CharacterPresentationCueBinding Cue => m_Cue;
        public int AuthoredClipCount => m_Animation?.Clips.Count ?? 0;
    }

    public enum CharacterPresentationCameraBindingKind
    {
        State,
        Cue,
        Response,
        Target
    }

    [Serializable]
    public sealed class CharacterPresentationCameraBinding
    {
        [SerializeField] CharacterPresentationCameraBindingKind m_Kind;
        [SerializeField] TimelineCameraMode m_Mode;
        [SerializeField] int m_Priority;
        [SerializeField] float m_BlendInSeconds;
        [SerializeField] float m_BlendOutSeconds;
        [SerializeField] string m_TargetKey = string.Empty;
        [SerializeField] TimelineCameraInterruptPolicy m_InterruptPolicy;
        [SerializeField] string m_CueId = string.Empty;
        [SerializeField] TimelineCameraCueKind m_CueKind;
        [SerializeField] string m_CueType = string.Empty;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] TimelineCameraLookResponseMode m_LookResponse;
        [SerializeField] float m_ManualOrbitWeight;
        [SerializeField] float m_PitchResponseWeight;
        [SerializeField] float m_YawResponseWeight;
        [SerializeField] string m_AnchorKey = string.Empty;
        [SerializeField] string m_AimPointKey = string.Empty;
        [SerializeField] string m_PreferredBoneKey = string.Empty;

        public CharacterPresentationCameraBindingKind Kind => m_Kind;
        public TimelineCameraMode Mode => m_Mode;
        public int Priority => m_Priority;
        public float BlendInSeconds => m_BlendInSeconds;
        public float BlendOutSeconds => m_BlendOutSeconds;
        public string TargetKey => m_TargetKey;
        public TimelineCameraInterruptPolicy InterruptPolicy => m_InterruptPolicy;
        public string CueId => m_CueId;
        public TimelineCameraCueKind CueKind => m_CueKind;
        public string CueType => m_CueType;
        public float DurationSeconds => m_DurationSeconds;
        public TimelineCameraLookResponseMode LookResponse => m_LookResponse;
        public float ManualOrbitWeight => m_ManualOrbitWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;
        public string AnchorKey => m_AnchorKey;
        public string AimPointKey => m_AimPointKey;
        public string PreferredBoneKey => m_PreferredBoneKey;

        public static CharacterPresentationCameraBinding State(
            TimelineCameraMode mode,
            int priority,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            TimelineCameraInterruptPolicy interruptPolicy)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.State,
                m_Mode = mode,
                m_Priority = priority,
                m_BlendInSeconds = blendInSeconds,
                m_BlendOutSeconds = blendOutSeconds,
                m_TargetKey = targetKey ?? string.Empty,
                m_InterruptPolicy = interruptPolicy
            };
        }

        public static CharacterPresentationCameraBinding Cue(
            string cueId,
            TimelineCameraCueKind cueKind,
            string cueType,
            float durationSeconds,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Cue,
                m_CueId = cueId ?? string.Empty,
                m_CueKind = cueKind,
                m_CueType = cueType ?? string.Empty,
                m_DurationSeconds = durationSeconds,
                m_Priority = priority
            };
        }

        public static CharacterPresentationCameraBinding Response(
            TimelineCameraLookResponseMode lookResponse,
            float manualOrbitWeight,
            float pitchResponseWeight,
            float yawResponseWeight,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Response,
                m_LookResponse = lookResponse,
                m_ManualOrbitWeight = manualOrbitWeight,
                m_PitchResponseWeight = pitchResponseWeight,
                m_YawResponseWeight = yawResponseWeight,
                m_Priority = priority
            };
        }

        public static CharacterPresentationCameraBinding Target(
            string targetKey,
            string anchorKey,
            string aimPointKey,
            string preferredBoneKey,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Target,
                m_TargetKey = targetKey ?? string.Empty,
                m_AnchorKey = anchorKey ?? string.Empty,
                m_AimPointKey = aimPointKey ?? string.Empty,
                m_PreferredBoneKey = preferredBoneKey ?? string.Empty,
                m_Priority = priority
            };
        }
    }

    [Serializable]
    public sealed class CharacterPresentationCueBinding
    {
        [SerializeField] string m_CueId = string.Empty;
        [SerializeField] string m_CueType = string.Empty;

        public CharacterPresentationCueBinding(string cueId, string cueType)
        {
            m_CueId = cueId ?? string.Empty;
            m_CueType = cueType ?? string.Empty;
        }

        public string CueId => m_CueId;
        public string CueType => m_CueType;
    }

    [Serializable]
    public sealed class CharacterPresentationAnimationBinding
    {
        [SerializeField] TransitionAssetBase m_Transition;
        [SerializeField] Easing.Function m_Easing;
        [SerializeField] string m_TrackName = string.Empty;
        [SerializeField] CharacterPresentationAnimationClipBinding[] m_Clips = Array.Empty<CharacterPresentationAnimationClipBinding>();
        [SerializeField] AnimationMarkerSyncBinding m_MarkerSync = new AnimationMarkerSyncBinding();

        public CharacterPresentationAnimationBinding(
            TransitionAssetBase transition,
            Easing.Function easing,
            string trackName,
            CharacterPresentationAnimationClipBinding[] clips,
            AnimationMarkerSyncBinding markerSync)
        {
            m_Transition = transition;
            m_Easing = easing;
            m_TrackName = trackName ?? string.Empty;
            m_Clips = clips ?? Array.Empty<CharacterPresentationAnimationClipBinding>();
            m_MarkerSync = markerSync ?? throw new ArgumentNullException(nameof(markerSync));
        }

        public TransitionAssetBase Transition => m_Transition;
        public Easing.Function Easing => m_Easing;
        public string TrackName => m_TrackName;
        public IReadOnlyList<CharacterPresentationAnimationClipBinding> Clips => m_Clips ?? Array.Empty<CharacterPresentationAnimationClipBinding>();
        public AnimationMarkerSyncBinding MarkerSync => m_MarkerSync;

        public AnimationProducerSample Sample(
            CharacterPresentationProducerEntry producer,
            AnimationPlaybackId playbackId,
            float sampleTime,
            int cycle)
        {
            var samples = new List<AnimationClipSample>();
            for (int i = 0; i < Clips.Count; i++)
            {
                if (Clips[i].TrySample(sampleTime, cycle, out AnimationClipSample sample))
                    samples.Add(sample);
            }
            return new AnimationProducerSample(
                playbackId,
                producer.LayerId,
                producer.ProgramProducerIdentity,
                producer.SourceGraphId,
                m_TrackName,
                sampleTime,
                cycle,
                samples);
        }

        public bool TrySampleFootPlacement(
            float sampleTime,
            int cycle,
            out AnimationFootPlacementSample footPlacement)
        {
            float totalWeight = 0f;
            float footPlacementWeight = 0f;
            for (int i = 0; i < Clips.Count; i++)
            {
                if (!Clips[i].TrySample(sampleTime, cycle, out AnimationClipSample clip, out AnimationFootPlacementSample sample))
                    continue;
                totalWeight += clip.Weight;
                footPlacementWeight += sample.Weight * clip.Weight;
            }
            if (totalWeight <= 0f)
            {
                footPlacement = default;
                return false;
            }
            footPlacement = new AnimationFootPlacementSample(footPlacementWeight / totalWeight);
            return true;
        }
    }

    [Serializable]
    public sealed class AnimationMarkerSyncMarkerBinding
    {
        [SerializeField] string m_AuthoringId = string.Empty;
        [SerializeField] string m_MarkerId = string.Empty;
        [SerializeField] int m_Frame;
        [SerializeField] float m_TimeSeconds;

        public AnimationMarkerSyncMarkerBinding(string authoringId, string markerId, int frame, float timeSeconds)
        {
            m_AuthoringId = authoringId ?? string.Empty;
            m_MarkerId = markerId ?? string.Empty;
            m_Frame = frame;
            m_TimeSeconds = timeSeconds;
        }

        public string AuthoringId => m_AuthoringId;
        public string MarkerId => m_MarkerId;
        public int Frame => m_Frame;
        public float TimeSeconds => m_TimeSeconds;
    }

    [Serializable]
    public sealed class AnimationMarkerSyncSegmentOccurrence
    {
        [SerializeField] int m_OccurrenceIndex;
        [SerializeField] int m_PreviousMarkerIndex;
        [SerializeField] int m_NextMarkerIndex;
        [SerializeField] string m_PreviousMarkerId = string.Empty;
        [SerializeField] string m_NextMarkerId = string.Empty;
        [SerializeField] float m_StartTimeSeconds;
        [SerializeField] float m_EndTimeSeconds;
        [SerializeField] bool m_Wraps;

        public AnimationMarkerSyncSegmentOccurrence(
            int occurrenceIndex,
            int previousMarkerIndex,
            int nextMarkerIndex,
            string previousMarkerId,
            string nextMarkerId,
            float startTimeSeconds,
            float endTimeSeconds,
            bool wraps)
        {
            m_OccurrenceIndex = occurrenceIndex;
            m_PreviousMarkerIndex = previousMarkerIndex;
            m_NextMarkerIndex = nextMarkerIndex;
            m_PreviousMarkerId = previousMarkerId ?? string.Empty;
            m_NextMarkerId = nextMarkerId ?? string.Empty;
            m_StartTimeSeconds = startTimeSeconds;
            m_EndTimeSeconds = endTimeSeconds;
            m_Wraps = wraps;
        }

        public int OccurrenceIndex => m_OccurrenceIndex;
        public int PreviousMarkerIndex => m_PreviousMarkerIndex;
        public int NextMarkerIndex => m_NextMarkerIndex;
        public string PreviousMarkerId => m_PreviousMarkerId;
        public string NextMarkerId => m_NextMarkerId;
        public float StartTimeSeconds => m_StartTimeSeconds;
        public float EndTimeSeconds => m_EndTimeSeconds;
        public float DurationSeconds => m_EndTimeSeconds - m_StartTimeSeconds;
        public bool Wraps => m_Wraps;
    }

    [Serializable]
    public sealed class AnimationMarkerSyncBinding : ISerializationCallbackReceiver
    {
        [SerializeField] AnimationSyncMode m_Mode = AnimationSyncMode.None;
        [SerializeField] string m_CanonicalGroupId = string.Empty;
        [SerializeField] AnimationMarkerSequenceTopology m_SequenceTopology;
        [SerializeField] AnimationMarkerSyncRole m_SyncRole;
        [SerializeField] int m_DurationFrame;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] AnimationMarkerSyncMarkerBinding[] m_Markers = Array.Empty<AnimationMarkerSyncMarkerBinding>();
        [SerializeField] AnimationMarkerSyncSegmentOccurrence[] m_Segments = Array.Empty<AnimationMarkerSyncSegmentOccurrence>();

        [NonSerialized] Dictionary<string, AnimationMarkerSyncSegmentOccurrence[]> m_Occurrences;

        public AnimationSyncMode Mode => m_Mode;
        public string CanonicalGroupId => m_CanonicalGroupId;
        public AnimationMarkerSequenceTopology SequenceTopology => m_SequenceTopology;
        public AnimationMarkerSyncRole SyncRole => m_SyncRole;
        public int DurationFrame => m_DurationFrame;
        public float DurationSeconds => m_DurationSeconds;
        public IReadOnlyList<AnimationMarkerSyncMarkerBinding> Markers => m_Markers ?? Array.Empty<AnimationMarkerSyncMarkerBinding>();
        public IReadOnlyList<AnimationMarkerSyncSegmentOccurrence> Segments => m_Segments ?? Array.Empty<AnimationMarkerSyncSegmentOccurrence>();
        public bool IsMarkerGroup => m_Mode == AnimationSyncMode.MarkerGroup;

        public static AnimationMarkerSyncBinding Compile(AnimationTrack track, TimelineData timeline)
        {
            if (track == null)
                throw new ArgumentNullException(nameof(track));
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (track.SyncMode == AnimationSyncMode.None)
                return new AnimationMarkerSyncBinding();
            if (track.SyncMode != AnimationSyncMode.MarkerGroup)
                throw new InvalidOperationException($"AnimationTrack '{track.AuthoringId}' has not been migrated to a publishable sync mode.");

            var binding = new AnimationMarkerSyncBinding
            {
                m_Mode = AnimationSyncMode.MarkerGroup,
                m_CanonicalGroupId = AnimationMarkerSyncAuthoring.NormalizeId(track.SyncGroupId),
                m_SequenceTopology = track.SequenceTopology,
                m_SyncRole = track.SyncRole,
                m_DurationFrame = timeline.MaxFrame,
                m_DurationSeconds = timeline.Duration,
                m_Markers = new AnimationMarkerSyncMarkerBinding[track.SyncMarkers.Count]
            };
            for (int i = 0; i < track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = track.SyncMarkers[i];
                binding.m_Markers[i] = new AnimationMarkerSyncMarkerBinding(
                    marker.AuthoringId,
                    AnimationMarkerSyncAuthoring.NormalizeId(marker.MarkerId),
                    marker.Frame,
                    marker.Frame / (float)TimelineUtility.FrameRate);
            }

            int segmentCount = Mathf.Max(0, binding.m_Markers.Length - 1) +
                               (binding.m_SequenceTopology == AnimationMarkerSequenceTopology.Cyclic ? 1 : 0);
            binding.m_Segments = new AnimationMarkerSyncSegmentOccurrence[segmentCount];
            int segmentIndex = 0;
            for (int i = 1; i < binding.m_Markers.Length; i++)
            {
                AnimationMarkerSyncMarkerBinding previous = binding.m_Markers[i - 1];
                AnimationMarkerSyncMarkerBinding next = binding.m_Markers[i];
                binding.m_Segments[segmentIndex] = new AnimationMarkerSyncSegmentOccurrence(
                    segmentIndex,
                    i - 1,
                    i,
                    previous.MarkerId,
                    next.MarkerId,
                    previous.TimeSeconds,
                    next.TimeSeconds,
                    false);
                segmentIndex++;
            }
            if (binding.m_SequenceTopology == AnimationMarkerSequenceTopology.Cyclic)
            {
                AnimationMarkerSyncMarkerBinding previous = binding.m_Markers[binding.m_Markers.Length - 1];
                AnimationMarkerSyncMarkerBinding next = binding.m_Markers[0];
                binding.m_Segments[segmentIndex] = new AnimationMarkerSyncSegmentOccurrence(
                    segmentIndex,
                    binding.m_Markers.Length - 1,
                    0,
                    previous.MarkerId,
                    next.MarkerId,
                    previous.TimeSeconds,
                    binding.m_DurationSeconds + next.TimeSeconds,
                    true);
            }
            binding.RebuildOccurrenceIndex();
            return binding;
        }

        public bool TryGetOccurrences(
            string previousMarkerId,
            string nextMarkerId,
            out AnimationMarkerSyncSegmentOccurrence[] occurrences)
        {
            if (m_Occurrences == null)
                RebuildOccurrenceIndex();
            return m_Occurrences.TryGetValue(
                AnimationMarkerSyncAuthoring.PairKey(previousMarkerId, nextMarkerId),
                out occurrences);
        }

        public bool TryValidate(out string error)
        {
            if (m_Mode == AnimationSyncMode.None)
            {
                if (!string.IsNullOrEmpty(m_CanonicalGroupId) ||
                    m_SequenceTopology != AnimationMarkerSequenceTopology.Unspecified ||
                    m_SyncRole != AnimationMarkerSyncRole.Unspecified ||
                    m_DurationFrame != 0 || m_DurationSeconds != 0f ||
                    Markers.Count != 0 || Segments.Count != 0)
                {
                    error = "None marker sync binding retains compiled marker data.";
                    return false;
                }
                error = string.Empty;
                return true;
            }
            if (m_Mode != AnimationSyncMode.MarkerGroup ||
                string.IsNullOrEmpty(m_CanonicalGroupId) ||
                m_SequenceTopology != AnimationMarkerSequenceTopology.Finite &&
                m_SequenceTopology != AnimationMarkerSequenceTopology.Cyclic ||
                m_SyncRole != AnimationMarkerSyncRole.CanBeLeader &&
                m_SyncRole != AnimationMarkerSyncRole.AlwaysLeader &&
                m_SyncRole != AnimationMarkerSyncRole.AlwaysFollower ||
                m_DurationFrame <= 0 || !float.IsFinite(m_DurationSeconds) || m_DurationSeconds <= 0f ||
                Markers.Count < 2)
            {
                error = "MarkerGroup compiled identity, topology, role, duration or marker count is invalid.";
                return false;
            }
            int expectedSegments = Markers.Count - 1 +
                                   (m_SequenceTopology == AnimationMarkerSequenceTopology.Cyclic ? 1 : 0);
            if (Segments.Count != expectedSegments)
            {
                error = "MarkerGroup compiled segment count does not match its marker topology.";
                return false;
            }
            for (int i = 0; i < Markers.Count; i++)
            {
                AnimationMarkerSyncMarkerBinding marker = Markers[i];
                if (marker == null || string.IsNullOrEmpty(marker.AuthoringId) ||
                    string.IsNullOrEmpty(marker.MarkerId) ||
                    !float.IsFinite(marker.TimeSeconds) || marker.TimeSeconds < 0f ||
                    i > 0 && marker.Frame <= Markers[i - 1].Frame)
                {
                    error = $"MarkerGroup compiled marker #{i} is invalid.";
                    return false;
                }
            }
            for (int i = 0; i < Segments.Count; i++)
            {
                AnimationMarkerSyncSegmentOccurrence segment = Segments[i];
                if (segment == null || segment.OccurrenceIndex != i ||
                    segment.PreviousMarkerIndex < 0 || segment.PreviousMarkerIndex >= Markers.Count ||
                    segment.NextMarkerIndex < 0 || segment.NextMarkerIndex >= Markers.Count ||
                    !float.IsFinite(segment.StartTimeSeconds) || !float.IsFinite(segment.EndTimeSeconds) ||
                    segment.DurationSeconds <= 0f)
                {
                    error = $"MarkerGroup compiled segment #{i} is invalid.";
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            RebuildOccurrenceIndex();
        }

        void RebuildOccurrenceIndex()
        {
            var grouped = new Dictionary<string, List<AnimationMarkerSyncSegmentOccurrence>>(StringComparer.Ordinal);
            AnimationMarkerSyncSegmentOccurrence[] segments = m_Segments ?? Array.Empty<AnimationMarkerSyncSegmentOccurrence>();
            for (int i = 0; i < segments.Length; i++)
            {
                AnimationMarkerSyncSegmentOccurrence segment = segments[i];
                if (segment == null)
                    continue;
                string key = AnimationMarkerSyncAuthoring.PairKey(segment.PreviousMarkerId, segment.NextMarkerId);
                if (!grouped.TryGetValue(key, out List<AnimationMarkerSyncSegmentOccurrence> values))
                {
                    values = new List<AnimationMarkerSyncSegmentOccurrence>();
                    grouped.Add(key, values);
                }
                values.Add(segment);
            }
            m_Occurrences = new Dictionary<string, AnimationMarkerSyncSegmentOccurrence[]>(grouped.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<AnimationMarkerSyncSegmentOccurrence>> pair in grouped)
                m_Occurrences.Add(pair.Key, pair.Value.ToArray());
        }
    }

    [Serializable]
    public sealed class CharacterPresentationAnimationClipBinding
    {
        [SerializeField] string m_ClipAuthoringId = string.Empty;
        [SerializeField] UnityEngine.AnimationClip m_Clip;
        [SerializeField] float m_StartTime;
        [SerializeField] float m_EndTime;
        [SerializeField] float m_ClipInTime;
        [SerializeField] float m_DurationTime;
        [SerializeField] float m_EaseInTime;
        [SerializeField] float m_EaseOutTime;
        [SerializeField] ExtraPolationMode m_Extrapolation;
        [SerializeField] AnimationCurve m_WeightCurve;
        [SerializeField] AnimationCurve m_EaseInCurve;
        [SerializeField] AnimationCurve m_EaseOutCurve;
        [SerializeField] AnimationCurve m_FootPlacementWeightCurve;

        public CharacterPresentationAnimationClipBinding(BTSMTL.Timeline.AnimationClip clip)
        {
            clip.RequireFootPlacementWeightCurve();
            m_ClipAuthoringId = clip.AuthoringId;
            m_Clip = clip.Clip;
            m_StartTime = clip.StartTime;
            m_EndTime = clip.EndTime;
            m_ClipInTime = clip.ClipInTime;
            m_DurationTime = clip.DurationTime;
            m_EaseInTime = clip.EaseInTime;
            m_EaseOutTime = clip.EaseOutTime;
            m_Extrapolation = clip.ExtraPolationMode;
            m_WeightCurve = CopyCurve(clip.WeightCurve);
            m_EaseInCurve = CopyCurve(clip.EaseInCurve);
            m_EaseOutCurve = CopyCurve(clip.EaseOutCurve);
            m_FootPlacementWeightCurve = CopyCurve(clip.FootPlacementCurve);
        }

        public bool TrySample(float timelineTime, int cycle, out AnimationClipSample sample)
        {
            return TrySample(timelineTime, cycle, out sample, out _);
        }

        public bool TrySample(
            float timelineTime,
            int cycle,
            out AnimationClipSample sample,
            out AnimationFootPlacementSample footPlacement)
        {
            sample = default;
            footPlacement = default;
            if (!m_Clip || timelineTime < m_StartTime)
                return false;
            bool hold = timelineTime > m_EndTime && m_Extrapolation == ExtraPolationMode.Hold;
            if (timelineTime > m_EndTime && !hold)
                return false;
            float duration = Mathf.Max(0.0001f, m_DurationTime);
            float selfTime = hold ? m_DurationTime : Mathf.Clamp(timelineTime - m_StartTime, 0f, m_DurationTime);
            float remainTime = Mathf.Max(0f, m_EndTime - timelineTime);
            float normalized = Mathf.Clamp01(selfTime / duration);
            float fadeIn = !hold && m_EaseInTime > 0f && selfTime < m_EaseInTime
                ? Evaluate(m_EaseInCurve, Mathf.Clamp01(selfTime / m_EaseInTime), 1f)
                : 1f;
            float fadeOut = !hold && m_EaseOutTime > 0f && remainTime < m_EaseOutTime
                ? 1f - Evaluate(m_EaseOutCurve, Mathf.Clamp01(1f - remainTime / m_EaseOutTime), 0f)
                : 1f;
            float weight = Mathf.Clamp01(Evaluate(m_WeightCurve, normalized, 1f) * fadeIn * fadeOut);
            if (weight <= 0f)
                return false;
            float clipTime = selfTime + m_ClipInTime;
            bool looping = cycle > 0;
            sample = new AnimationClipSample(
                m_ClipAuthoringId,
                RuntimeSourceElementHandle.Invalid,
                m_Clip,
                clipTime,
                normalized,
                weight,
                looping,
                m_ClipInTime,
                m_DurationTime,
                looping ? clipTime + cycle * m_DurationTime : clipTime);
            footPlacement = new AnimationFootPlacementSample(
                EvaluateRequired(m_FootPlacementWeightCurve, normalized, nameof(m_FootPlacementWeightCurve)));
            return true;
        }

        static float EvaluateRequired(AnimationCurve curve, float time, string field)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"Presentation Projection animation clip requires '{field}'.");
            return curve.Evaluate(time);
        }

        static float Evaluate(AnimationCurve curve, float time, float fallback)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(time) : fallback;
        }

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                return null;
            var result = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return result;
        }
    }
}
