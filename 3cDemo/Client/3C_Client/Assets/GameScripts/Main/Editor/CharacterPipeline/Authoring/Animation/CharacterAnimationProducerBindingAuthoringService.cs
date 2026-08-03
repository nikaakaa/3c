using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [Serializable]
        public sealed class CharacterAnimationProducerBindingRequestEntry
        {
            public string timelineAuthoringId = string.Empty;
            public string trackAuthoringId = string.Empty;
            public string sourceAssetPath = string.Empty;
    }

    [Serializable]
    public sealed class CharacterAnimationProducerBindingRequest
    {
        public string fixedProgramWrapperPath = string.Empty;
        public CharacterAnimationProducerBindingRequestEntry[] bindings =
            Array.Empty<CharacterAnimationProducerBindingRequestEntry>();
    }

        public sealed class CharacterAnimationProducerBindingInspectionEntry
        {
            public string producerIdentity = string.Empty;
            public string displayName = string.Empty;
            public string sourceAssetPath = string.Empty;
        public string[] clipPaths = Array.Empty<string>();
    }

    public sealed class CharacterAnimationProducerBindingResult
    {
        public string definitionPath = string.Empty;
        public string profilePath = string.Empty;
        public string projectionPath = string.Empty;
        public string projectionRevision = string.Empty;
        public string float32ProgramWrapperPath = string.Empty;
        public string fixedProgramWrapperPath = string.Empty;
        public CharacterAnimationProducerBindingInspectionEntry[] bindings =
            Array.Empty<CharacterAnimationProducerBindingInspectionEntry>();
        public string[] createdSourcePaths = Array.Empty<string>();
        public bool authoringSucceeded;
        public bool buildSucceeded;
    }

    public static class CharacterAnimationProducerBindingAuthoringService
    {
        sealed class ResolvedBinding
        {
            public CharacterAnimationProducerBindingRequestEntry Request;
            public AnimationProducerAuthoringEntry Producer;
            public string SourcePath;
        }

        sealed class Context
        {
            public CharacterPipelineDefinition Definition;
            public CharacterAnimationPresentationProfile Profile;
            public CharacterAnimationProducerBindingRequest Request;
            public ResolvedBinding[] Bindings;
        }

        public static CharacterAnimationProducerBindingResult Inspect(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            return CreateResult(BuildContext(definition, request), Array.Empty<string>(), false);
        }

        public static CharacterAnimationProducerBindingResult Apply(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            Context context = BuildContext(definition, request);
            var createdPaths = new List<string>();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure Character Animation Producer Bindings");
            try
            {
                for (int i = 0; i < context.Bindings.Length; i++)
                {
                    ResolvedBinding binding = context.Bindings[i];
                    TransitionAssetBase source = AssetDatabase.LoadAssetAtPath<TransitionAssetBase>(binding.SourcePath);
                    if (!source)
                    {
                        source = CreateTimelineSource(binding.Producer, binding.SourcePath);
                        createdPaths.Add(binding.SourcePath);
                    }
                    if (!source.IsValid)
                        throw new InvalidOperationException($"Timeline source '{binding.SourcePath}' is invalid.");
                    CharacterAnimationPresentationAuthoringService.ConfigureTimelineProducerBinding(
                        context.Profile,
                        context.Definition,
                        binding.Producer.ProducerId,
                        source);
                }
                Validate(context);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                return CreateResult(BuildContext(definition, request), createdPaths.ToArray(), false);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                for (int i = 0; i < createdPaths.Count; i++)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(createdPaths[i]))
                        AssetDatabase.DeleteAsset(createdPaths[i]);
                }
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        public static CharacterAnimationProducerBindingResult Build(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            Context context = BuildContext(definition, request);
            Validate(context);
            var buildRequest = new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.Publish,
                new ICharacterSimulationTargetBuildAdapter[]
                {
                    CharacterSimulationTargetCatalog.Float32(definition),
                    new FixedCharacterSimulationTargetBuildAdapter(request.fixedProgramWrapperPath)
                });
            CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(buildRequest);
            if (!result.IsValid)
                throw new InvalidOperationException(string.Join("\n", result.Report.Messages.Select(value => value.ToString())));
            AssetDatabase.SaveAssets();
            CharacterAnimationProducerBindingResult output = CreateResult(
                BuildContext(definition, request),
                Array.Empty<string>(),
                true);
            output.projectionRevision = definition.PresentationProjection
                ? definition.PresentationProjection.ProjectionRevision
                : string.Empty;
            return output;
        }

        static Context BuildContext(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            CharacterAnimationPresentationProfile profile = definition.AnimationPresentationProfile
                ? definition.AnimationPresentationProfile
                : throw new InvalidOperationException($"Character Definition '{definition.name}' has no Animation Presentation Profile.");
            request.fixedProgramWrapperPath = NormalizeAssetPath(request.fixedProgramWrapperPath, ".asset");
            AnimationProducerAuthoringEntry[] producers = CharacterAnimationPresentationAuthoringService
                .DiscoverProducerTracks(definition)
                .OrderBy(value => value.ProgramProducerIdentity, StringComparer.Ordinal)
                .ToArray();
            CharacterAnimationProducerBindingRequestEntry[] requested = request.bindings ??
                Array.Empty<CharacterAnimationProducerBindingRequestEntry>();
            if (requested.Length != producers.Length)
                throw new InvalidOperationException("Producer binding request must cover the complete reachable Animation producer topology.");

            var producerMap = producers.ToDictionary(value => value.ProducerId);
            var identities = new HashSet<AnimationProducerId>();
            var resolved = new ResolvedBinding[requested.Length];
            for (int i = 0; i < requested.Length; i++)
            {
                CharacterAnimationProducerBindingRequestEntry entry = requested[i] ??
                    throw new InvalidOperationException($"Producer binding request #{i} is missing.");
                var producerId = new AnimationProducerId(entry.timelineAuthoringId, entry.trackAuthoringId);
                if (!producerId.IsValid || !identities.Add(producerId) || !producerMap.TryGetValue(producerId, out AnimationProducerAuthoringEntry producer))
                    throw new InvalidOperationException($"Producer binding request #{i} does not identify one reachable producer exactly once.");
                string sourcePath = NormalizeAssetPath(entry.sourceAssetPath, ".asset");
                if (producer.SourceClips.Count == 0)
                    throw new InvalidOperationException($"Timeline producer '{producerId}' has no source clips.");
                resolved[i] = new ResolvedBinding
                {
                    Request = entry,
                    Producer = producer,
                    SourcePath = sourcePath
                };
            }
            Array.Sort(resolved, (left, right) =>
                string.CompareOrdinal(left.Producer.ProgramProducerIdentity, right.Producer.ProgramProducerIdentity));
            return new Context
            {
                Definition = definition,
                Profile = profile,
                Request = request,
                Bindings = resolved
            };
        }

        static TransitionAssetBase CreateTimelineSource(AnimationProducerAuthoringEntry producer, string path)
        {
            string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory) || !AssetDatabase.IsValidFolder(directory))
                throw new InvalidOperationException($"Timeline source directory '{directory}' does not exist.");
            var source = ScriptableObject.CreateInstance<TransitionAsset>();
            source.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(source, path);
            Undo.RegisterCreatedObjectUndo(source, "Create Character Animation Timeline Source");
            if (producer.SourceClips.Count == 1)
            {
                source.Transition = new ClipTransition { Clip = producer.SourceClips[0].Clip };
            }
            else
            {
                var sequence = new TransitionSequence
                {
                    Transitions = producer.SourceClips
                        .Select(value => (ITransition)new ClipTransition { Clip = value.Clip })
                        .ToArray()
                };
                source.Transition = sequence;
            }
            EditorUtility.SetDirty(source);
            return source;
        }

        static void Validate(Context context)
        {
            for (int i = 0; i < context.Bindings.Length; i++)
            {
                ResolvedBinding expected = context.Bindings[i];
                AnimationProducerPresentationBinding actual = context.Profile.FindProducerBinding(expected.Producer.ProducerId);
                if (actual == null)
                    throw new InvalidOperationException($"Producer '{expected.Producer.ProducerId}' has no Action Timeline binding.");
                UnityEngine.Object source = actual.Source;
                if (!source || !string.Equals(AssetDatabase.GetAssetPath(source), expected.SourcePath, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Producer '{expected.Producer.ProducerId}' does not use source '{expected.SourcePath}'.");
            }
            var profileErrors = new List<string>();
            context.Profile.CollectConfigurationErrors(profileErrors);
            if (profileErrors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", profileErrors));
            CharacterPoseGraphValidationReport graphReport = CharacterPresentationPoseGraphValidator.Validate(
                context.Profile.PoseGraph,
                context.Profile.RigDefinition,
                CharacterPoseAuthoringPortProjection.Get,
                context.Bindings.Select(value => value.Producer.AnimationChannelId).Distinct().ToArray());
            if (!graphReport.IsValid)
            {
                var errors = new List<string>();
                graphReport.CopyMessagesTo(errors);
                throw new InvalidOperationException(string.Join("\n", errors));
            }
        }

        static CharacterAnimationProducerBindingResult CreateResult(
            Context context,
            string[] createdPaths,
            bool buildSucceeded)
        {
            return new CharacterAnimationProducerBindingResult
            {
                definitionPath = AssetDatabase.GetAssetPath(context.Definition),
                profilePath = AssetDatabase.GetAssetPath(context.Profile),
                projectionPath = AssetDatabase.GetAssetPath(context.Definition.PresentationProjection),
                projectionRevision = context.Definition.PresentationProjection
                    ? context.Definition.PresentationProjection.ProjectionRevision
                    : string.Empty,
                float32ProgramWrapperPath = AssetDatabase.GetAssetPath(context.Definition.SimulationProgram),
                fixedProgramWrapperPath = context.Request.fixedProgramWrapperPath,
                bindings = context.Bindings.Select(value =>
                {
                    AnimationProducerPresentationBinding binding =
                        context.Profile.FindProducerBinding(value.Producer.ProducerId);
                    UnityEngine.Object source = binding?.Source;
                    return new CharacterAnimationProducerBindingInspectionEntry
                    {
                        producerIdentity = value.Producer.ProgramProducerIdentity,
                        displayName = value.Producer.DisplayName,
                        sourceAssetPath = source ? AssetDatabase.GetAssetPath(source) : string.Empty,
                        clipPaths = value.Producer.SourceClips
                            .Select(clip => clip.Clip ? AssetDatabase.GetAssetPath(clip.Clip) : string.Empty)
                            .ToArray()
                    };
                }).ToArray(),
                createdSourcePaths = createdPaths,
                authoringSucceeded = true,
                buildSucceeded = buildSucceeded
            };
        }

        static string NormalizeAssetPath(string value, string extension)
        {
            string path = (value ?? string.Empty).Trim().Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".."))
                throw new InvalidOperationException($"Asset path '{path}' must be an explicit Assets/...{extension} path.");
            return path;
        }
    }
}
