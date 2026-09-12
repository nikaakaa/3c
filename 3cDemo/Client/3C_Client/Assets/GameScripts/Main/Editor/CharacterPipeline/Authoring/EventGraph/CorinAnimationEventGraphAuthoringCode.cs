using System;
using System.IO;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph
{
    public sealed class CorinAnimationEventGraphAuthoringCode :
        IBtsmtlAuthoringGenerationEntry
    {
        public const string Recipe = "character.animation-event-graph.corin/v1";
        public const string SourcePath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/EventGraph/CorinAnimationEventGraphAuthoringCode.cs";
        public const string OutputPath =
            "Assets/Configs/Character/Corin/Pipeline/Presentation/EventGraphs/CorinAnimationEventGraph.asset";
        public const string GraphIdentity = "character.corin.animation-event-graph";
        public const string ContentRevision = "character.corin.animation-event-graph/v1";

        public string RecipeType => Recipe;
        public string EntryTypeName => typeof(CorinAnimationEventGraphAuthoringCode).FullName;
        public string SourceCodePath =>
            Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                SourcePath.Replace('/', Path.DirectorySeparatorChar));

        public BtsmtlAuthoringGenerationResult Execute(
            BtsmtlAuthoringGenerationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            CharacterPipelineDefinition definition =
                context.ResolveExternalAsset<CharacterPipelineDefinition>(
                    context.DefinitionAssetPath,
                    0L);
            CharacterAnimationPresentationProfile profile =
                definition.AnimationPresentationProfile;
            if (!profile)
                throw new InvalidOperationException(
                    "Corin Animation Event Graph generation requires the Definition Animation Presentation Profile.");
            if (!string.Equals(context.OutputAssetPath, OutputPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Corin Animation Event Graph generation requires output path '{OutputPath}'.");

            CharacterAnimationEventGraph graph =
                EventGraphAuthoringCode.EnsureRoot<CharacterAnimationEventGraph>(
                    context,
                    GraphIdentity,
                    ContentRevision,
                    "CorinAnimationEventGraph");

            graph.AddAuthoringNode<StartEvent>(
                "corin.animation-event-graph.start",
                new Vector2(-720f, 0f));
            UpdateEvent update = graph.AddAuthoringNode<UpdateEvent>(
                "corin.animation-event-graph.update",
                new Vector2(-720f, 280f));

            graph.ConfigureUpdateEvent(update);
            graph.ConfigureCanvas(
                "Animation Event Graph",
                "Corin animation presentation uses Action Playback and source curves as formal inputs.",
                Vector2.zero,
                1f);
            graph.ConfigureAuthoringIdentity(GraphIdentity, ContentRevision);

            Undo.RecordObject(profile, "Bind Corin Animation Event Graph");
            profile.SetEventGraph(graph);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(graph);
            return context.Complete(graph);
        }

    }
}
