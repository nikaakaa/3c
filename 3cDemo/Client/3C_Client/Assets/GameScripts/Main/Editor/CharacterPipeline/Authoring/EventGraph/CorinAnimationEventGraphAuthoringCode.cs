using System;
using System.IO;
using BTSMTL.EventGraphs;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ParadoxNotion;
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

            Variable<float> actionWeight = graph.DeclareVariable<float>(
                "animation.action-weight",
                "Action Weight",
                1f);
            Variable<float> footPlacementWeight = graph.DeclareVariable<float>(
                "animation.foot-placement-weight",
                "Foot Placement Weight",
                1f);
            StartEvent start = graph.AddAuthoringNode<StartEvent>(
                "corin.animation-event-graph.start",
                new Vector2(-720f, 0f));
            UpdateEvent update = graph.AddAuthoringNode<UpdateEvent>(
                "corin.animation-event-graph.update",
                new Vector2(-720f, 280f));
            Split startSplit = graph.AddAuthoringNode<Split>(
                "corin.animation-event-graph.start-split",
                new Vector2(-440f, 0f));
            Split updateSplit = graph.AddAuthoringNode<Split>(
                "corin.animation-event-graph.update-split",
                new Vector2(-440f, 280f));
            SetVariable<float> setActionWeight =
                graph.AddAuthoringNode<SetVariable<float>>(
                    "corin.animation-event-graph.set-action-weight",
                    new Vector2(-120f, 0f));
            SetVariable<float> setFootPlacementWeight =
                graph.AddAuthoringNode<SetVariable<float>>(
                    "corin.animation-event-graph.set-foot-placement-weight",
                    new Vector2(-120f, 280f));

            graph.ConfigureUpdateEvent(update);
            graph.ConfigureInstantSplit(startSplit, 2);
            graph.ConfigureInstantSplit(updateSplit, 2);
            graph.BindVariableNode(setActionWeight, actionWeight.ID);
            graph.BindVariableNode(setFootPlacementWeight, footPlacementWeight.ID);
            graph.ConfigureAssignment(setActionWeight, AssignOp.Set, false);
            graph.ConfigureAssignment(setFootPlacementWeight, AssignOp.Set, false);
            graph.ConfigureValueInput(setActionWeight, "Value", 1f);
            graph.ConfigureValueInput(setFootPlacementWeight, "Value", 1f);
            graph.ConnectAuthoringPorts(
                "corin.animation-event-graph.start-to-start-split",
                start,
                "Once",
                startSplit,
                "In");
            graph.ConnectAuthoringPorts(
                "corin.animation-event-graph.update-to-update-split",
                update,
                "Out",
                updateSplit,
                "In");
            graph.ConnectAuthoringPorts(
                "corin.animation-event-graph.start-action-weight",
                startSplit,
                "0",
                setActionWeight,
                "In");
            graph.ConnectAuthoringPorts(
                "corin.animation-event-graph.start-foot-placement-weight",
                startSplit,
                "1",
                setFootPlacementWeight,
                "In");
            graph.ConnectAuthoringPorts(
                "corin.animation-event-graph.update-action-weight",
                updateSplit,
                "0",
                setActionWeight,
                "In");
            graph.ConnectAuthoringPorts(
                "corin.animation-event-graph.update-foot-placement-weight",
                updateSplit,
                "1",
                setFootPlacementWeight,
                "In");
            graph.ConfigureCanvas(
                "Animation Event Graph",
                "Corin animation variables are produced by the native presentation event graph.",
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
